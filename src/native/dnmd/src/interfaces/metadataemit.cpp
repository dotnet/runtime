#include "metadataemit.hpp"
#include "importhelpers.hpp"
#include "signatures.hpp"
#include "pal.hpp"
#include <limits>
#include <fstream>
#include <stack>
#include <algorithm>
#include <utility>
#include <cstring>

#define RETURN_IF_FAILED(exp) \
{ \
    hr = (exp); \
    if (FAILED(hr)) \
    { \
        return hr; \
    } \
}

#define MD_MODULE_TOKEN TokenFromRid(1, mdtModule)
#define MD_GLOBAL_PARENT_TOKEN TokenFromRid(1, mdtTypeDef)

namespace
{
    void SplitTypeName(
        char* typeName,
        char const** nspace,
        char const** name)
    {
        // Search for the last delimiter.
        char* pos = std::strrchr(typeName, '.');
        if (pos == nullptr)
        {
            // No namespace is indicated by an empty string.
            *nspace = "";
            *name = typeName;
        }
        else
        {
            *pos = '\0';
            *nspace = typeName;
            *name = pos + 1;
        }
    }
}

HRESULT MetadataEmit::SetModuleProps(
        LPCWSTR     szName)
{
    // If the name is null, we have nothing to do.
    // COMPAT-BREAK: CoreCLR would still record the token in the EncLog in this case.
    if (szName == nullptr)
        return S_OK;

    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;
    
    mdcursor_t c;
    uint32_t count;
    if (!md_create_cursor(MetaData(), mdtid_Module, &c, &count))
    {
        if (md_append_row(MetaData(), mdtid_Module, &c))
        {
            md_commit_row_add(c);
        }
        else
        {
            return E_FAIL;
        }
    }

    // Search for a file name in the provided path
    // and use that as the module name.
    char* modulePath = cvt;
    std::size_t len = std::strlen(modulePath);
    char const* start = modulePath;
    for (char const* p = modulePath + len - 1; p >= modulePath; p--)
    {
        if (*p == '\\' || *p == '/')
        {
            start = p + 1;
            break;
        }
    }
    
    if (1 != md_set_column_value_as_utf8(c, mdtModule_Name, 1, &start))
        return E_FAIL;

    // TODO: Record ENC Log.

    return S_OK;
}

HRESULT MetadataEmit::Save(
        LPCWSTR     szFile,
        DWORD       dwSaveFlags)
{
    if (dwSaveFlags != 0)
        return E_INVALIDARG;

    pal::StringConvert<WCHAR, char> cvt(szFile);
    if (!cvt.Success())
        return E_INVALIDARG;

    size_t saveSize;
    md_write_to_buffer(MetaData(), nullptr, &saveSize);
    std::unique_ptr<uint8_t[]> buffer { new uint8_t[saveSize] };
    if (!md_write_to_buffer(MetaData(), buffer.get(), &saveSize))
        return E_FAIL;

    std::FILE* file = std::fopen(cvt, "wb");
    if (file == nullptr)
    {
        return E_FAIL;
    }

    size_t totalSaved = 0;
    while (totalSaved < saveSize)
    {
        totalSaved += std::fwrite(buffer.get(), sizeof(uint8_t), saveSize - totalSaved, file);
        if (ferror(file) != 0)
        {
            std::fclose(file);
            return E_FAIL;
        }
    }

    if (std::fclose(file) == EOF)
    {
        return E_FAIL;
    }

    return S_OK;
}

HRESULT MetadataEmit::SaveToStream(
        IStream     *pIStream,
        DWORD       dwSaveFlags)
{
    HRESULT hr;
    if (dwSaveFlags != 0)
        return E_INVALIDARG;

    size_t saveSize;
    md_write_to_buffer(MetaData(), nullptr, &saveSize);
    std::unique_ptr<uint8_t[]> buffer { new uint8_t[saveSize] };
    md_write_to_buffer(MetaData(), buffer.get(), &saveSize);

    size_t totalSaved = 0;
    while (totalSaved < saveSize)
    {
        ULONG numBytesToWrite = (ULONG)std::min(saveSize, (size_t)std::numeric_limits<ULONG>::max());
        RETURN_IF_FAILED(pIStream->Write((char const*)buffer.get() + totalSaved, numBytesToWrite, nullptr));
        totalSaved += numBytesToWrite;
    }

    return pIStream->Write(buffer.get(), (ULONG)saveSize, nullptr);
}

HRESULT MetadataEmit::GetSaveSize(
        CorSaveSize fSave,
        DWORD       *pdwSaveSize)
{
    // TODO: Do we want to support different save modes (as specified through dispenser options)?
    // If so, we'll need to handle that here in addition to the ::Save* methods.
    UNREFERENCED_PARAMETER(fSave);
    size_t saveSize;
    md_write_to_buffer(MetaData(), nullptr, &saveSize);
    if (saveSize > std::numeric_limits<DWORD>::max())
        return CLDB_E_TOO_BIG;
    *pdwSaveSize = (DWORD)saveSize;
    return S_OK;
}

HRESULT MetadataEmit::DefineTypeDef(
        LPCWSTR     szTypeDef,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[],
        mdTypeDef   *ptd)
{
    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_TypeDef, &c))
        return E_FAIL;
    
    pal::StringConvert<WCHAR, char> cvt(szTypeDef);
    if (!cvt.Success())
        return E_INVALIDARG;
    
    // TODO: Check for duplicate type definitions

    char const* ns;
    char const* name;
    SplitTypeName(cvt, &ns, &name);
    if (1 != md_set_column_value_as_utf8(c, mdtTypeDef_TypeNamespace, 1, &ns))
        return E_FAIL;
    if (1 != md_set_column_value_as_utf8(c, mdtTypeDef_TypeName, 1, &name))
        return E_FAIL;
    
    // TODO: Handle reserved flags
    uint32_t flags = (uint32_t)dwTypeDefFlags;
    if (1 != md_set_column_value_as_constant(c, mdtTypeDef_Flags, 1, &flags))
        return E_FAIL;
    
    if (1 != md_set_column_value_as_token(c, mdtTypeDef_Extends, 1, &tkExtends))
        return E_FAIL;
    
    mdcursor_t fieldCursor;
    uint32_t numFields;
    if (!md_create_cursor(MetaData(), mdtid_Field, &fieldCursor, &numFields))
    {
        mdToken nilField = mdFieldDefNil;
        if (1 != md_set_column_value_as_token(c, mdtTypeDef_FieldList, 1, &nilField))
            return E_FAIL;
    }
    else
    {
        md_cursor_move(&fieldCursor, numFields);
        if (1 != md_set_column_value_as_cursor(c, mdtTypeDef_FieldList, 1, &fieldCursor))
            return E_FAIL;
    }
    
    mdcursor_t methodCursor;
    uint32_t numMethods;
    if (!md_create_cursor(MetaData(), mdtid_MethodDef, &methodCursor, &numMethods))
    {
        mdToken nilMethod = mdMethodDefNil;
        if (1 != md_set_column_value_as_token(c, mdtTypeDef_MethodList, 1, &nilMethod))
            return E_FAIL;
    }
    else
    {
        md_cursor_move(&methodCursor, numMethods);
        if (1 != md_set_column_value_as_cursor(c, mdtTypeDef_MethodList, 1, &methodCursor))
            return E_FAIL;
    }
    
    size_t i = 0;

    if (rtkImplements != nullptr)
    {
        for (mdToken currentImplementation = rtkImplements[i]; currentImplementation != mdTokenNil; currentImplementation = rtkImplements[++i])
        {
            md_added_row_t interfaceImpl;
            if (!md_append_row(MetaData(), mdtid_InterfaceImpl, &interfaceImpl))
                return E_FAIL;

            if (1 != md_set_column_value_as_cursor(interfaceImpl, mdtInterfaceImpl_Class, 1, &c))
                return E_FAIL;

            if (1 != md_set_column_value_as_token(interfaceImpl, mdtInterfaceImpl_Interface, 1, &currentImplementation))
                return E_FAIL;
        }
    }
    
    // TODO: Update Enc Log

    if (!md_cursor_to_token(c, ptd))
        return E_FAIL;
    
    return S_OK;
}

HRESULT MetadataEmit::DefineNestedType(
        LPCWSTR     szTypeDef,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[],
        mdTypeDef   tdEncloser,
        mdTypeDef   *ptd)
{
    HRESULT hr;

    if (TypeFromToken(tdEncloser) != mdtTypeDef || IsNilToken(tdEncloser))
        return E_INVALIDARG;

    if (IsTdNested(dwTypeDefFlags))
        return E_INVALIDARG;

    RETURN_IF_FAILED(DefineTypeDef(szTypeDef, dwTypeDefFlags, tkExtends, rtkImplements, ptd));

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_NestedClass, &c))
        return E_FAIL;

    if (1 != md_set_column_value_as_token(c, mdtNestedClass_NestedClass, 1, ptd))
        return E_FAIL;
    
    if (1 != md_set_column_value_as_token(c, mdtNestedClass_EnclosingClass, 1, &tdEncloser))
        return E_FAIL;

    // TODO: Update ENC log
    return S_OK;
}

HRESULT MetadataEmit::SetHandler(
        IUnknown    *pUnk)
{
    // The this implementation of MetadataEmit doesn't ever remap tokens,
    // so this method (which is for registering a callback for when tokens are remapped)
    // is a no-op.
    UNREFERENCED_PARAMETER(pUnk);
    return S_OK;
}

HRESULT MetadataEmit::DefineMethod(
        mdTypeDef       td,
        LPCWSTR         szName,
        DWORD           dwMethodFlags,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG           cbSigBlob,
        ULONG           ulCodeRVA,
        DWORD           dwImplFlags,
        mdMethodDef     *pmd)
{
    if (TypeFromToken(td) != mdtTypeDef)
        return E_INVALIDARG;

    mdcursor_t type;
    if (!md_token_to_cursor(MetaData(), td, &type))
        return CLDB_E_FILE_CORRUPT;

    md_added_row_t newMethod;
    if (!md_add_new_row_to_list(type, mdtTypeDef_MethodList, &newMethod))
        return E_FAIL;

    pal::StringConvert<WCHAR, char> cvt(szName);

    char const* name = cvt;
    if (1 != md_set_column_value_as_utf8(newMethod, mdtMethodDef_Name, 1, &name))
        return E_FAIL;
    
    uint32_t flags = dwMethodFlags;
    if (1 != md_set_column_value_as_constant(newMethod, mdtMethodDef_Flags, 1, &flags))
        return E_FAIL;
    
    uint32_t sigLength = cbSigBlob;
    if (1 != md_set_column_value_as_blob(newMethod, mdtMethodDef_Signature, 1, &pvSigBlob, &sigLength))
        return E_FAIL;
    
    uint32_t implFlags = dwImplFlags;
    if (1 != md_set_column_value_as_constant(newMethod, mdtMethodDef_ImplFlags, 1, &implFlags))
        return E_FAIL;
    
    uint32_t rva = ulCodeRVA;
    if (1 != md_set_column_value_as_constant(newMethod, mdtMethodDef_Rva, 1, &rva))
        return E_FAIL;

    if (!md_cursor_to_token(newMethod, pmd))
        return CLDB_E_FILE_CORRUPT;
    
    // TODO: Update ENC log
    return S_OK;
}

HRESULT MetadataEmit::DefineMethodImpl(
        mdTypeDef   td,
        mdToken     tkBody,
        mdToken     tkDecl)
{
    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_MethodImpl, &c))
        return E_FAIL;

    if (1 != md_set_column_value_as_token(c, mdtMethodImpl_Class, 1, &td))
        return E_FAIL;
    
    if (1 != md_set_column_value_as_token(c, mdtMethodImpl_MethodBody, 1, &tkBody))
        return E_FAIL;

    if (1 != md_set_column_value_as_token(c, mdtMethodImpl_MethodDeclaration, 1, &tkDecl))
        return E_FAIL;

    // TODO: Update ENC log
    return S_OK;
}

HRESULT MetadataEmit::DefineTypeRefByName(
        mdToken     tkResolutionScope,
        LPCWSTR     szName,
        mdTypeRef   *ptr)
{
    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_TypeRef, &c))
        return E_FAIL;

    if (1 != md_set_column_value_as_token(c, mdtTypeRef_ResolutionScope, 1, &tkResolutionScope))
        return E_FAIL;
    
    pal::StringConvert<WCHAR, char> cv(szName);

    if (!cv.Success())
        return E_FAIL;

    char const* ns;
    char const* name;
    SplitTypeName(cv, &ns, &name);

    if (1 != md_set_column_value_as_utf8(c, mdtTypeRef_TypeNamespace, 1, &ns))
        return E_FAIL;
    if (1 != md_set_column_value_as_utf8(c, mdtTypeRef_TypeName, 1, &name))
        return E_FAIL;

    if (!md_cursor_to_token(c, ptr))
        return E_FAIL;
    
    // TODO: Update ENC log
    return S_OK;
}

HRESULT MetadataEmit::DefineImportType(
        IMetaDataAssemblyImport *pAssemImport,
        void const *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *pImport,
        mdTypeDef   tdImport,
        IMetaDataAssemblyEmit *pAssemEmit,
        mdTypeRef   *ptr)
{
    HRESULT hr;
    dncp::com_ptr<IDNMDOwner> assemImport{};

    if (pAssemImport != nullptr)
        RETURN_IF_FAILED(pAssemImport->QueryInterface(IID_IDNMDOwner, (void**)&assemImport));

    dncp::com_ptr<IDNMDOwner> assemEmit{};
    if (pAssemEmit != nullptr)
        RETURN_IF_FAILED(pAssemEmit->QueryInterface(IID_IDNMDOwner, (void**)&assemEmit));

    if (pImport == nullptr)
        return E_INVALIDARG;
    
    dncp::com_ptr<IDNMDOwner> import{};
    RETURN_IF_FAILED(pImport->QueryInterface(IID_IDNMDOwner, (void**)&import));

    mdcursor_t originalTypeDef;
    if (!md_token_to_cursor(import->MetaData(), tdImport, &originalTypeDef))
        return CLDB_E_FILE_CORRUPT;
    
    mdcursor_t importedTypeDef;

    RETURN_IF_FAILED(ImportReferenceToTypeDef(
        originalTypeDef,
        assemImport->MetaData(),
        { reinterpret_cast<uint8_t const*>(pbHashValue), cbHashValue },
        assemEmit->MetaData(),
        MetaData(),
        false,
        [](mdcursor_t){},
        &importedTypeDef
    ));

    if (!md_cursor_to_token(importedTypeDef, ptr))
        return E_FAIL;
    
    return S_OK;
}

HRESULT MetadataEmit::DefineMemberRef(
        mdToken     tkImport,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMemberRef *pmr)
{
    if (IsNilToken(tkImport))
        tkImport = MD_GLOBAL_PARENT_TOKEN;
    
    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;
    char const* name = cvt;

    // TODO: Check for duplicates

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_MemberRef, &c))
        return E_FAIL;
    
    if (1 != md_set_column_value_as_token(c, mdtMemberRef_Class, 1, &tkImport))
        return E_FAIL;
    
    if (1 != md_set_column_value_as_utf8(c, mdtMemberRef_Name, 1, &name))
        return E_FAIL;
    
    uint8_t const* sig = (uint8_t const*)pvSigBlob;
    uint32_t sigLength = cbSigBlob;
    if (1 != md_set_column_value_as_blob(c, mdtMemberRef_Signature, 1, &sig, &sigLength))
        return E_FAIL;
    
    if (!md_cursor_to_token(c, pmr))
        return E_FAIL;
    
    // TODO: Update EncLog
    return S_OK;
}

HRESULT MetadataEmit::DefineImportMember(
        IMetaDataAssemblyImport *pAssemImport,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *pImport,
        mdToken     mbMember,
        IMetaDataAssemblyEmit *pAssemEmit,
        mdToken     tkParent,
        mdMemberRef *pmr)
{
    return ::DefineImportMember(
        this,
        pAssemImport,
        pbHashValue,
        cbHashValue,
        pImport,
        mbMember,
        pAssemEmit,
        tkParent,
        pmr);
}

namespace
{
    HRESULT AddMethodSemantic(mdhandle_t md, mdcursor_t parent, CorMethodSemanticsAttr semantic, mdMethodDef method)
    {
        md_added_row_t addMethodSemantic;
        if (!md_append_row(md, mdtid_MethodSemantics, &addMethodSemantic))
            return E_FAIL;
        
        if (1 != md_set_column_value_as_cursor(addMethodSemantic, mdtMethodSemantics_Association, 1, &parent))
            return E_FAIL;
        
        uint32_t semantics = semantic;
        if (1 != md_set_column_value_as_constant(addMethodSemantic, mdtMethodSemantics_Semantics, 1, &semantics))
            return E_FAIL;
        
        if (1 != md_set_column_value_as_token(addMethodSemantic, mdtMethodSemantics_Method, 1, &method))
            return E_FAIL;
        
        // TODO: Update EncLog
        return S_OK;
    }
    
    HRESULT DeleteParentedToken(mdhandle_t md, mdToken parent, mdtable_id_t childTable, col_index_t parentColumn)
    {
        mdcursor_t c;
        uint32_t count;
        if (!md_create_cursor(md, childTable, &c, &count))
            return CLDB_E_RECORD_NOTFOUND;
        
        if (!md_find_row_from_cursor(c, parentColumn, parent, &c))
            return CLDB_E_RECORD_NOTFOUND;

        mdToken nilParent = mdFieldDefNil;
        if (1 != md_set_column_value_as_token(c, mdtFieldMarshal_Parent, 1, &nilParent))
            return E_FAIL;
        
        mdcursor_t parentCursor;
        if (!md_token_to_cursor(md, parent, &parentCursor))
            return CLDB_E_FILE_CORRUPT;
        // TODO: Update EncLog
        return S_OK;
    }

    HRESULT RemoveFlag(mdhandle_t md, mdToken tk, col_index_t flagsColumn, uint32_t flagToRemove)
    {
        // TODO: Update EncLog
        mdcursor_t c;
        if (!md_token_to_cursor(md, tk, &c))
            return CLDB_E_FILE_CORRUPT;

        uint32_t flags;
        if (1 != md_get_column_value_as_constant(c, flagsColumn, 1, &flags))
            return E_FAIL;
        
        flags &= ~flagToRemove;
        if (1 != md_set_column_value_as_constant(c, flagsColumn, 1, &flags))
            return E_FAIL;

        // TODO: Update EncLog
        return S_OK;
    }

    HRESULT AddFlag(mdhandle_t md, mdToken tk, col_index_t flagsColumn, uint32_t flagToAdd)
    {
        // TODO: Update EncLog
        mdcursor_t c;
        if (!md_token_to_cursor(md, tk, &c))
            return CLDB_E_FILE_CORRUPT;

        uint32_t flags;
        if (1 != md_get_column_value_as_constant(c, flagsColumn, 1, &flags))
            return E_FAIL;
        
        flags |= flagToAdd;
        if (1 != md_set_column_value_as_constant(c, flagsColumn, 1, &flags))
            return E_FAIL;

        // TODO: Update EncLog
        return S_OK;
    }

    template<typename T>
    HRESULT FindOrCreateParentedRow(mdhandle_t md, mdToken parent, mdtable_id_t childTable, col_index_t parentCol, T const& setTableData)
    {
        HRESULT hr;
        mdcursor_t c;
        md_added_row_t addedRow;
        uint32_t count;
        if (!md_create_cursor(md, childTable, &c, &count)
            || !md_find_row_from_cursor(c, parentCol, parent, &c))
        {
            // TODO: Update EncLog
            if (!md_append_row(md, childTable, &addedRow))
                return E_FAIL;
            
            if (1 != md_set_column_value_as_token(addedRow, parentCol, 1, &parent))
                return E_FAIL;
            c = addedRow;
        }
        RETURN_IF_FAILED(setTableData(c));

        return S_OK;
    }
}

HRESULT MetadataEmit::DefineEvent(
        mdTypeDef   td,
        LPCWSTR     szEvent,
        DWORD       dwEventFlags,
        mdToken     tkEventType,
        mdMethodDef mdAddOn,
        mdMethodDef mdRemoveOn,
        mdMethodDef mdFire,
        mdMethodDef rmdOtherMethods[],
        mdEvent     *pmdEvent)
{
    assert(TypeFromToken(td) == mdtTypeDef && td != mdTypeDefNil);
    assert(IsNilToken(tkEventType) || TypeFromToken(tkEventType) == mdtTypeDef ||
                TypeFromToken(tkEventType) == mdtTypeRef || TypeFromToken(tkEventType) == mdtTypeSpec);
    assert(TypeFromToken(mdAddOn) == mdtMethodDef && mdAddOn != mdMethodDefNil);
    assert(TypeFromToken(mdRemoveOn) == mdtMethodDef && mdRemoveOn != mdMethodDefNil);
    assert(IsNilToken(mdFire) || TypeFromToken(mdFire) == mdtMethodDef);
    assert(szEvent && pmdEvent);

    pal::StringConvert<WCHAR, char> cvt(szEvent);
    if (!cvt.Success())
        return E_INVALIDARG;
    
    char const* name = cvt;

    return FindOrCreateParentedRow(MetaData(), td, mdtid_EventMap, mdtEventMap_Parent, [=](mdcursor_t c)
    {
        HRESULT hr;
        // TODO: Check for duplicates
        md_added_row_t addedEvent;
        if (!md_add_new_row_to_list(c, mdtEventMap_EventList, &addedEvent))
            return E_FAIL;
        
        if (1 != md_set_column_value_as_utf8(addedEvent, mdtEvent_Name, 1, &name))
            return E_FAIL;
        
        uint32_t flags = dwEventFlags;
        if (1 != md_set_column_value_as_constant(addedEvent, mdtEvent_EventFlags, 1, &flags))
            return E_FAIL;
        
        if (1 != md_set_column_value_as_token(addedEvent, mdtEvent_EventType, 1, &tkEventType))
            return E_FAIL;

        if (mdAddOn != mdMethodDefNil)
        {
            RETURN_IF_FAILED(AddMethodSemantic(MetaData(), addedEvent, msAddOn, mdAddOn));
        }

        if (mdRemoveOn != mdMethodDefNil)
        {
            RETURN_IF_FAILED(AddMethodSemantic(MetaData(), addedEvent, msRemoveOn, mdRemoveOn));
        }

        if (mdFire != mdMethodDefNil)
        {
            RETURN_IF_FAILED(AddMethodSemantic(MetaData(), addedEvent, msFire, mdFire));
        }

        if (rmdOtherMethods != nullptr)
        {
            for (size_t i = 0; !IsNilToken(rmdOtherMethods[i]); i++)
            {
                RETURN_IF_FAILED(AddMethodSemantic(MetaData(), addedEvent, msOther, rmdOtherMethods[i]));
            }
        }

        if (!md_cursor_to_token(addedEvent, pmdEvent))
            return E_FAIL;

        // TODO: Update EncLog
        return S_OK;
    });
}

HRESULT MetadataEmit::SetClassLayout(
        mdTypeDef   td,
        DWORD       dwPackSize,
        COR_FIELD_OFFSET rFieldOffsets[],
        ULONG       ulClassSize)
{
    HRESULT hr;
    assert(TypeFromToken(td) == mdtTypeDef);

    if (rFieldOffsets != nullptr)
    {
        for (size_t i = 0; rFieldOffsets[i].ridOfField != mdFieldDefNil; ++i)
        {
            if (rFieldOffsets[i].ulOffset != UINT32_MAX)
            {
                mdToken field = TokenFromRid(rFieldOffsets[i].ridOfField, mdtFieldDef);
                uint32_t offset = rFieldOffsets[i].ulOffset;
                RETURN_IF_FAILED(FindOrCreateParentedRow(MetaData(), field, mdtid_FieldLayout, mdtFieldLayout_Field, [=](mdcursor_t c)
                {
                    if (1 != md_set_column_value_as_constant(c, mdtFieldLayout_Offset, 1, &offset))
                        return E_FAIL;
                    
                    return S_OK;
                }));
            }
        }
    }

    RETURN_IF_FAILED(FindOrCreateParentedRow(MetaData(), td, mdtid_ClassLayout, mdtClassLayout_Parent, [=](mdcursor_t c)
    {
        uint32_t packSize = (uint32_t)dwPackSize;
        if (1 != md_set_column_value_as_constant(c, mdtClassLayout_PackingSize, 1, &packSize))
            return E_FAIL;

        uint32_t classSize = (uint32_t)ulClassSize;
        if (1 != md_set_column_value_as_constant(c, mdtClassLayout_ClassSize, 1, &classSize))
            return E_FAIL;
        
        return S_OK;
    }));

    return S_OK;
}

HRESULT MetadataEmit::DeleteClassLayout(
        mdTypeDef   td)
{
    assert(TypeFromToken(td) == mdtTypeDef);
    HRESULT hr;
    mdcursor_t c;
    uint32_t count;
    if (!md_create_cursor(MetaData(), mdtid_ClassLayout, &c, &count))
        return CLDB_E_RECORD_NOTFOUND;
    
    if (!md_find_row_from_cursor(c, mdtClassLayout_Parent, td, &c))
        return CLDB_E_RECORD_NOTFOUND;
    
    RETURN_IF_FAILED(DeleteParentedToken(MetaData(), td, mdtid_ClassLayout, mdtClassLayout_Parent));

    // Now that we've deleted the class layout entry,
    // we need to delete the field layout entries for the fields of the type.
    mdcursor_t type;
    if (!md_token_to_cursor(MetaData(), td, &type))
        return CLDB_E_FILE_CORRUPT;

    mdcursor_t field;
    uint32_t fieldCount;
    if (!md_get_column_value_as_range(type, mdtTypeDef_FieldList, &field, &fieldCount))
        return S_OK;
    
    for (uint32_t i = 0; i < fieldCount; ++i, md_cursor_next(&field))
    {
        mdcursor_t resolvedField;
        if (!md_resolve_indirect_cursor(field, &resolvedField))
            return E_FAIL;

        mdToken fieldToken;
        if (!md_cursor_to_token(resolvedField, &fieldToken))
            return E_FAIL;

        hr = DeleteParentedToken(MetaData(), fieldToken, mdtid_FieldLayout, mdtFieldLayout_Field);

        // If we couldn't find the field layout entry, that's fine.
        // If we hit another error, return that error.
        if (hr == CLDB_E_RECORD_NOTFOUND)
            continue;
        RETURN_IF_FAILED(hr);
    }

    return S_OK;
}

HRESULT MetadataEmit::SetFieldMarshal(
        mdToken     tk,
        PCCOR_SIGNATURE pvNativeType,
        ULONG       cbNativeType)
{
    mdcursor_t parent;
    if (!md_token_to_cursor(MetaData(), tk, &parent))
        return CLDB_E_FILE_CORRUPT;
    
    col_index_t col = TypeFromToken(tk) == mdtFieldDef ? mdtField_Flags : mdtParam_Flags;
    uint32_t flagToAdd = TypeFromToken(tk) == mdtFieldDef ? (uint32_t)fdHasFieldMarshal : (uint32_t)pdHasFieldMarshal;
    uint32_t flags;
    if (1 != md_get_column_value_as_constant(parent, col, 1, &flags))
        return E_FAIL;
    
    flags |= flagToAdd;
    if (1 != md_set_column_value_as_constant(parent, col, 1, &flags))
        return E_FAIL;

    FindOrCreateParentedRow(MetaData(), tk, mdtid_FieldMarshal, mdtFieldMarshal_Parent, [=](mdcursor_t c)
    {
        uint8_t const* sig = (uint8_t const*)pvNativeType;
        uint32_t sigLength = cbNativeType;
        if (1 != md_set_column_value_as_blob(c, mdtFieldMarshal_NativeType, 1, &sig, &sigLength))
            return E_FAIL;

        return S_OK;
    });

    // TODO: Update EncLog
    return S_OK;
}

HRESULT MetadataEmit::DeleteFieldMarshal(
        mdToken     tk)
{
    HRESULT hr;
    assert(TypeFromToken(tk) == mdtFieldDef || TypeFromToken(tk) == mdtParamDef);
    assert(!IsNilToken(tk));

    RETURN_IF_FAILED(DeleteParentedToken(
        MetaData(),
        tk,
        mdtid_FieldMarshal,
        mdtFieldMarshal_Parent));

    RETURN_IF_FAILED(RemoveFlag(
        MetaData(),
        tk,
        TypeFromToken(tk) == mdtFieldDef ? mdtField_Flags : mdtParam_Flags,
        TypeFromToken(tk) == mdtFieldDef ? (uint32_t)fdHasFieldMarshal : (uint32_t)pdHasFieldMarshal));
    return S_OK;
}

HRESULT MetadataEmit::DefinePermissionSet(
        mdToken     tk,
        DWORD       dwAction,
        void const  *pvPermission,
        ULONG       cbPermission,
        mdPermission *ppm)
{
    // TODO: Check for duplicates
    assert(TypeFromToken(tk) == mdtTypeDef || TypeFromToken(tk) == mdtMethodDef ||
             TypeFromToken(tk) == mdtAssembly);

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_DeclSecurity, &c))
        return E_FAIL;

    if (1 != md_set_column_value_as_token(c, mdtDeclSecurity_Parent, 1, &tk))
        return E_FAIL;

    if (TypeFromToken(tk) == mdtTypeDef
        || TypeFromToken(tk) == mdtMethodDef)
    {
        uint32_t flagToAdd = TypeFromToken(tk) == mdtTypeDef ? (uint32_t)tdHasSecurity : (uint32_t)mdHasSecurity;
        col_index_t flagsCol = TypeFromToken(tk) == mdtTypeDef ? mdtTypeDef_Flags : mdtMethodDef_Flags;

        mdcursor_t parent;
        if (1 != md_get_column_value_as_cursor(c, mdtDeclSecurity_Parent, 1, &parent))
            return E_FAIL;

        uint32_t flags;
        if (1 != md_get_column_value_as_constant(parent, flagsCol, 1, &flags))
            return E_FAIL;

        flags |= flagToAdd;

        if (1 != md_set_column_value_as_constant(parent, flagsCol, 1, &flags))
            return E_FAIL;
        // TODO: Update EncLog
    }

    uint32_t action = dwAction;
    if (1 != md_set_column_value_as_constant(c, mdtDeclSecurity_Action, 1, &action))
        return E_FAIL;
    
    uint8_t const* permission = (uint8_t const*)pvPermission;
    uint32_t permissionLength = cbPermission;
    if (1 != md_set_column_value_as_blob(c, mdtDeclSecurity_PermissionSet, 1, &permission, &permissionLength))
        return E_FAIL;

    if (!md_cursor_to_token(c, ppm))
        return E_FAIL;
    
    // TODO: Update EncLog
    return S_OK;
}

HRESULT MetadataEmit::SetRVA(
        mdMethodDef md,
        ULONG       ulRVA)
{
    mdcursor_t method;
    if (!md_token_to_cursor(MetaData(), md, &method))
        return CLDB_E_FILE_CORRUPT;
    
    uint32_t rva = ulRVA;
    if (1 != md_set_column_value_as_constant(method, mdtMethodDef_Rva, 1, &rva))
        return E_FAIL;

    // TODO: Update EncLog
    return S_OK;
}

HRESULT MetadataEmit::GetTokenFromSig(
        PCCOR_SIGNATURE pvSig,
        ULONG       cbSig,
        mdSignature *pmsig)
{
    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_StandAloneSig, &c))
        return E_FAIL;

    uint32_t sigLength = cbSig;
    if (1 != md_set_column_value_as_blob(c, mdtStandAloneSig_Signature, 1, &pvSig, &sigLength))
        return E_FAIL;

    if (!md_cursor_to_token(c, pmsig))
        return CLDB_E_FILE_CORRUPT;

    // TODO: Update EncLog
    return S_OK;
}

HRESULT MetadataEmit::DefineModuleRef(
        LPCWSTR     szName,
        mdModuleRef *pmur)
{
    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_ModuleRef, &c))
        return E_FAIL;

    pal::StringConvert<WCHAR, char> cvt(szName);
    char const* name = cvt;

    if (1 != md_set_column_value_as_utf8(c, mdtModuleRef_Name, 1, &name))
        return E_FAIL;
    
    if (!md_cursor_to_token(c, pmur))
        return CLDB_E_FILE_CORRUPT;

    // TODO: Update EncLog
    return S_OK;
}


HRESULT MetadataEmit::SetParent(
        mdMemberRef mr,
        mdToken     tk)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), mr, &c))
        return CLDB_E_FILE_CORRUPT;
    
    if (1 != md_set_column_value_as_token(c, mdtMemberRef_Class, 1, &tk))
        return E_FAIL;
    
    // TODO: Update EncLog
    return S_OK;
}

HRESULT MetadataEmit::GetTokenFromTypeSpec(
        PCCOR_SIGNATURE pvSig,
        ULONG       cbSig,
        mdTypeSpec *ptypespec)
{
    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_TypeSpec, &c))
        return E_FAIL;

    uint32_t sigLength = cbSig;
    if (1 != md_set_column_value_as_blob(c, mdtTypeSpec_Signature, 1, &pvSig, &sigLength))
        return E_FAIL;

    if (!md_cursor_to_token(c, ptypespec))
        return CLDB_E_FILE_CORRUPT;

    // TODO: Update EncLog
    return S_OK;
}

HRESULT MetadataEmit::SaveToMemory(
        void        *pbData,
        ULONG       cbData)
{
    size_t saveSize = cbData;
    return md_write_to_buffer(MetaData(), (uint8_t*)pbData, &saveSize) ? S_OK : E_OUTOFMEMORY;
}

HRESULT MetadataEmit::DefineUserString(
        LPCWSTR szString,
        ULONG       cchString,
        mdString    *pstk)
{
    std::unique_ptr<char16_t[]> pString{ new char16_t[cchString + 1] };
    std::memcpy(pString.get(), szString, cchString * sizeof(char16_t));
    pString[cchString] = u'\0';

    mduserstringcursor_t c = md_add_userstring_to_heap(MetaData(), pString.get());

    if (c == 0)
        return E_FAIL;
    
    if ((c & 0xff000000) != 0)
        return META_E_STRINGSPACE_FULL;

    *pstk = TokenFromRid((mdString)c, mdtString);
    return S_OK;
}

HRESULT MetadataEmit::DeleteToken(
        mdToken     tkObj)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), tkObj, &c))
        return E_INVALIDARG;
    
    char const* deletedName = COR_DELETED_NAME_A;
    switch (TypeFromToken(tkObj))
    {
        case mdtTypeDef:
        {
            if (1 != md_set_column_value_as_utf8(c, mdtTypeDef_TypeName, 1, &deletedName))
                return E_FAIL;
            return AddFlag(MetaData(), tkObj, mdtTypeDef_Flags, tdSpecialName | tdRTSpecialName);
        }
        case mdtMethodDef:
        {
            if (1 != md_set_column_value_as_utf8(c, mdtMethodDef_Name, 1, &deletedName))
                return E_FAIL;
            return AddFlag(MetaData(), tkObj, mdtMethodDef_Flags, mdSpecialName | mdRTSpecialName);
        }
        case mdtFieldDef:
        {
            if (1 != md_set_column_value_as_utf8(c, mdtField_Name, 1, &deletedName))
                return E_FAIL;
            return AddFlag(MetaData(), tkObj, mdtField_Flags, fdSpecialName | fdRTSpecialName);
        }
        case mdtEvent:
        {
            if (1 != md_set_column_value_as_utf8(c, mdtEvent_Name, 1, &deletedName))
                return E_FAIL;
            return AddFlag(MetaData(), tkObj, mdtEvent_EventFlags, evSpecialName | evRTSpecialName);
        }
        case mdtProperty:
        {
            if (1 != md_set_column_value_as_utf8(c, mdtProperty_Name, 1, &deletedName))
                return E_FAIL;
            return AddFlag(MetaData(), tkObj, mdtProperty_Flags, prSpecialName | prRTSpecialName);
        }
        case mdtExportedType:
        {
            if (1 != md_set_column_value_as_utf8(c, mdtExportedType_TypeName, 1, &deletedName))
                return E_FAIL;
            return S_OK;
        }
        case mdtCustomAttribute:
        {
            mdToken parent;
            if (1 != md_get_column_value_as_token(c, mdtCustomAttribute_Parent, 1, &parent))
                return E_FAIL;
            
            // Change the parent to the nil token.
            parent = TokenFromRid(mdTokenNil, TypeFromToken(parent));

            if (1 != md_set_column_value_as_token(c, mdtCustomAttribute_Parent, 1, &parent))
                return E_FAIL;
            
            return S_OK;
        }
        case mdtGenericParam:
        {
            mdToken parent;
            if (1 != md_get_column_value_as_token(c, mdtGenericParam_Owner, 1, &parent))
                return E_FAIL;
            
            // Change the parent to the nil token.
            parent = TokenFromRid(mdTokenNil, TypeFromToken(parent));

            if (1 != md_set_column_value_as_token(c, mdtGenericParam_Owner, 1, &parent))
                return E_FAIL;
            
            return S_OK;
        }
        case mdtGenericParamConstraint:
        {
            mdToken parent = mdGenericParamNil;
            if (1 != md_set_column_value_as_token(c, mdtGenericParamConstraint_Owner, 1, &parent))
                return E_FAIL;
            
            return S_OK;
        }
        case mdtPermission:
        {
            mdToken parent;
            if (1 != md_get_column_value_as_token(c, mdtDeclSecurity_Parent, 1, &parent))
                return E_FAIL;
            
            // Change the parent to the nil token.
            mdToken originalParent = parent;
            parent = TokenFromRid(mdTokenNil, TypeFromToken(parent));

            if (1 != md_set_column_value_as_token(c, mdtDeclSecurity_Parent, 1, &parent))
                return E_FAIL;
            
            if (TypeFromToken(originalParent) == mdtAssembly)
            {
                // There is no HasSecurity flag for an assembly, so we're done.
                return S_OK;
            }

            mdcursor_t permissions;
            uint32_t numPermissions;
            if (!md_create_cursor(MetaData(), mdtid_DeclSecurity, &permissions, &numPermissions))
                return E_FAIL;

            // If we have no more permissions for this parent, remove the HasSecurity bit.
            // Since we just need to know if there's any matching row and we don't need a range of rows,
            // we can use find_row instead of find_range.
            if (!md_find_row_from_cursor(permissions, mdtDeclSecurity_Parent, originalParent, &permissions))
            {
                return RemoveFlag(
                    MetaData(),
                    originalParent,
                    TypeFromToken(originalParent) == mdtTypeDef ? mdtTypeDef_Flags : mdtMethodDef_Flags,
                    TypeFromToken(originalParent) == mdtTypeDef ? (uint32_t)tdHasSecurity : (uint32_t)mdHasSecurity);
            }

            return S_OK;
        }
        default:
            break;
    }
    return E_INVALIDARG;
}

HRESULT MetadataEmit::SetMethodProps(
        mdMethodDef md,
        DWORD       dwMethodFlags,
        ULONG       ulCodeRVA,
        DWORD       dwImplFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), md, &c))
        return CLDB_E_FILE_CORRUPT;
    
    if (dwMethodFlags != std::numeric_limits<DWORD>::max())
    {
        // TODO: Strip the reserved flags from user input and preserve the existing reserved flags.
        uint32_t flags = dwMethodFlags;
        if (1 != md_set_column_value_as_constant(c, mdtMethodDef_Flags, 1, &flags))
            return E_FAIL;
    }
    
    if (ulCodeRVA != std::numeric_limits<ULONG>::max())
    {
        uint32_t rva = ulCodeRVA;
        if (1 != md_set_column_value_as_constant(c, mdtMethodDef_Rva, 1, &rva))
            return E_FAIL;
    }
    
    if (dwImplFlags != std::numeric_limits<DWORD>::max())
    {
        uint32_t implFlags = dwImplFlags;
        if (1 != md_set_column_value_as_constant(c, mdtMethodDef_ImplFlags, 1, &implFlags))
            return E_FAIL;
    }
    
    // TODO: Update EncLog
    return S_OK;
}

HRESULT MetadataEmit::SetTypeDefProps(
        mdTypeDef   td,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[])
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), td, &c))
        return CLDB_E_FILE_CORRUPT;
    
    if (dwTypeDefFlags != std::numeric_limits<DWORD>::max())
    {
        // TODO: Strip the reserved flags from user input and preserve the existing reserved flags.
        uint32_t flags = dwTypeDefFlags;
        if (1 != md_set_column_value_as_constant(c, mdtTypeDef_Flags, 1, &flags))
            return E_FAIL;
    }

    if (tkExtends != std::numeric_limits<uint32_t>::max())
    {
        if (IsNilToken(tkExtends))
            tkExtends = mdTypeDefNil;
        
        if (1 != md_set_column_value_as_token(c, mdtTypeDef_Extends, 1, &tkExtends))
            return E_FAIL;
    }

    if (rtkImplements)
    {
        // First null-out the Class columns of the current implementations.
        // We can't delete here as we hand out tokens into this table to the caller.
        // This would be much more efficient if we could delete rows, as nulling out the parent will almost assuredly make the column
        // unsorted.
        mdcursor_t interfaceImplCursor;
        uint32_t numInterfaceImpls;
        if (md_create_cursor(MetaData(), mdtid_InterfaceImpl, &interfaceImplCursor, &numInterfaceImpls)
            && md_find_range_from_cursor(interfaceImplCursor, mdtInterfaceImpl_Class, RidFromToken(td), &interfaceImplCursor, &numInterfaceImpls) != MD_RANGE_NOT_FOUND)
        {
            for (uint32_t i = 0; i < numInterfaceImpls; ++i)
            {
                mdToken parent;
                if (1 != md_get_column_value_as_token(interfaceImplCursor, mdtInterfaceImpl_Class, 1, &parent))
                    return E_FAIL;
                
                // If getting a range was unsupported, then we're doing a whole table scan here.
                // In that case, we can't assume that we've already validated the parent.
                // Update it here.
                if (parent == td)
                {
                    mdToken newParent = mdTypeDefNil;
                    if (1 != md_set_column_value_as_token(interfaceImplCursor, mdtInterfaceImpl_Class, 1, &newParent))
                        return E_FAIL;
                }
            }
        }

        size_t implIndex = 0;
        mdToken currentImplementation = rtkImplements[implIndex];
        do
        {
            md_added_row_t interfaceImpl;
            if (!md_append_row(MetaData(), mdtid_InterfaceImpl, &interfaceImpl))
                return E_FAIL;
            
            if (1 != md_set_column_value_as_cursor(interfaceImpl, mdtInterfaceImpl_Class, 1, &c))
                return E_FAIL;
            
            if (1 != md_set_column_value_as_token(interfaceImpl, mdtInterfaceImpl_Interface, 1, &currentImplementation))
                return E_FAIL;
        } while ((currentImplementation = rtkImplements[++implIndex]) != mdTokenNil);
    }

    // TODO: Update EncLog
    return S_OK;
}

namespace
{
    // Set all rows in the MethodSemantic table with a matching Association column of parent to the nil token of parent's table.
    HRESULT RemoveSemantics(mdhandle_t md, mdToken parent, CorMethodSemanticsAttr semantic)
    {
        mdcursor_t c;
        uint32_t count;
        if (!md_create_cursor(md, mdtid_MethodSemantics, &c, &count))
            return CLDB_E_RECORD_NOTFOUND;
        
        md_range_result_t result = md_find_range_from_cursor(c, mdtMethodSemantics_Association, parent, &c, &count);
        if (result == MD_RANGE_NOT_FOUND)
            return S_OK;
        
        for (uint32_t i = 0; i < count; ++i, md_cursor_next(&c))
        {
            mdToken association;
            if (1 != md_get_column_value_as_token(c, mdtMethodSemantics_Association, 1, &association))
                return E_FAIL;
            
            uint32_t recordSemantic;
            if (1 != md_get_column_value_as_constant(c, mdtMethodSemantics_Semantics, 1, &recordSemantic))
                return E_FAIL;

            if (association == parent && recordSemantic == (uint32_t)semantic)
            {
                association = TokenFromRid(mdTokenNil, TypeFromToken(association));
                if (1 != md_set_column_value_as_token(c, mdtMethodSemantics_Association, 1, &association))
                    return E_FAIL;
            }
        }

        return S_OK;
    }
}

HRESULT MetadataEmit::SetEventProps(
        mdEvent     ev,
        DWORD       dwEventFlags,
        mdToken     tkEventType,
        mdMethodDef mdAddOn,
        mdMethodDef mdRemoveOn,
        mdMethodDef mdFire,
        mdMethodDef rmdOtherMethods[])
{
    HRESULT hr;
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), ev, &c))
        return CLDB_E_FILE_CORRUPT;
    
    if (dwEventFlags != std::numeric_limits<DWORD>::max())
    {
        uint32_t eventFlags = dwEventFlags;
        if (1 != md_set_column_value_as_constant(c, mdtEvent_EventFlags, 1, &eventFlags))
            return E_FAIL;
    }

    if (!IsNilToken(tkEventType))
    {
        if (1 != md_set_column_value_as_token(c, mdtEvent_EventType, 1, &tkEventType))
            return E_FAIL;
    }

    if (!IsNilToken(mdAddOn))
    {
        RemoveSemantics(MetaData(), ev, msAddOn);
        RETURN_IF_FAILED(AddMethodSemantic(MetaData(), c, msAddOn, mdAddOn));
    }

    if (!IsNilToken(mdRemoveOn))
    {
        RemoveSemantics(MetaData(), ev, msRemoveOn);
        RETURN_IF_FAILED(AddMethodSemantic(MetaData(), c, msRemoveOn, mdRemoveOn));
    }

    if (!IsNilToken(mdFire))
    {
        RemoveSemantics(MetaData(), ev, msFire);
        RETURN_IF_FAILED(AddMethodSemantic(MetaData(), c, msFire, mdFire));
    }

    if (rmdOtherMethods)
    {
        RemoveSemantics(MetaData(), ev, msOther);
        for (size_t i = 0; rmdOtherMethods[i] != mdMethodDefNil; ++i)
        {
            RETURN_IF_FAILED(AddMethodSemantic(MetaData(), c, msOther, rmdOtherMethods[i]));
        }
    }

    // TODO: Update EncLog

    return S_OK;
}

HRESULT MetadataEmit::SetPermissionSetProps(
        mdToken     tk,
        DWORD       dwAction,
        void const  *pvPermission,
        ULONG       cbPermission,
        mdPermission *ppm)
{
    assert(TypeFromToken(tk) == mdtTypeDef || TypeFromToken(tk) == mdtMethodDef ||
        TypeFromToken(tk) == mdtAssembly);

    if (dwAction == UINT32_MAX || dwAction == 0 || dwAction > dclMaximumValue)
        return E_INVALIDARG;

    mdcursor_t c;
    uint32_t count;
    if (!md_create_cursor(MetaData(), mdtid_DeclSecurity, &c, &count))
        return CLDB_E_RECORD_NOTFOUND;
    
    if (!md_find_row_from_cursor(c, mdtDeclSecurity_Parent, tk, &c))
        return CLDB_E_RECORD_NOTFOUND;

    uint32_t action = dwAction;
    if (1 != md_set_column_value_as_constant(c, mdtDeclSecurity_Action, 1, &action))
        return E_FAIL;
    
    uint8_t const* permission = (uint8_t const*)pvPermission;
    uint32_t permissionLength = cbPermission;
    if (1 != md_set_column_value_as_blob(c, mdtDeclSecurity_PermissionSet, 1, &permission, &permissionLength))
        return E_FAIL;

    if (!md_cursor_to_token(c, ppm))
        return CLDB_E_FILE_CORRUPT;

    // TODO: Update EncLog
    return S_OK;
}

HRESULT MetadataEmit::DefinePinvokeMap(
        mdToken     tk,
        DWORD       dwMappingFlags,
        LPCWSTR     szImportName,
        mdModuleRef mrImportDLL)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), tk, &c))
        return CLDB_E_FILE_CORRUPT;
    
    if (TypeFromToken(tk) == mdtMethodDef)
    {
        AddFlag(MetaData(), tk, mdtMethodDef_Flags, mdPinvokeImpl);
    }
    else if (TypeFromToken(tk) == mdtFieldDef)
    {
        AddFlag(MetaData(), tk, mdtField_Flags, fdPinvokeImpl);
    }
    // TODO: check for duplicates

    // If we found a duplicate and ENC is on, update.
    // If we found a duplicate and ENC is off, fail.
    // Otherwise, we need to make a new row
    mdcursor_t row_to_edit;
    md_added_row_t added_row_wrapper;

    // TODO: We don't expose tokens for the ImplMap table, so as long as we aren't generating ENC deltas
    // we can insert in-place.
    if (!md_append_row(MetaData(), mdtid_ImplMap, &row_to_edit))
        return E_FAIL;
    added_row_wrapper = md_added_row_t(row_to_edit);

    if (1 != md_set_column_value_as_token(row_to_edit, mdtImplMap_MemberForwarded, 1, &tk))
        return E_FAIL;
    
    if (dwMappingFlags == std::numeric_limits<uint32_t>::max())
    {
        // Unspecified by the user, set to the default.
        dwMappingFlags = 0;
    }

    uint32_t mappingFlags = dwMappingFlags;
    if (1 != md_set_column_value_as_constant(row_to_edit, mdtImplMap_MappingFlags, 1, &mappingFlags))
        return E_FAIL;
    
    pal::StringConvert<WCHAR, char> cvt(szImportName);
    char const* name = cvt;
    if (1 != md_set_column_value_as_utf8(row_to_edit, mdtImplMap_ImportName, 1, &name))
        return E_FAIL;
    
    if (IsNilToken(mrImportDLL))
    {
        // TODO: If the token is nil, create a module ref to "" (if it doesn't exist) and use that.
    }

    if (1 != md_set_column_value_as_token(row_to_edit, mdtImplMap_ImportScope, 1, &mrImportDLL))
        return E_FAIL;
    
    // TODO: Update EncLog
    return S_OK;
}

HRESULT MetadataEmit::SetPinvokeMap(
        mdToken     tk,
        DWORD       dwMappingFlags,
        LPCWSTR     szImportName,
        mdModuleRef mrImportDLL)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), tk, &c))
        return CLDB_E_FILE_CORRUPT;

    mdcursor_t implMapCursor;
    uint32_t numImplMaps;
    if (!md_create_cursor(MetaData(), mdtid_ImplMap, &implMapCursor, &numImplMaps))
        return E_FAIL;

    mdcursor_t row_to_edit;
    if (!md_find_row_from_cursor(implMapCursor, mdtImplMap_MemberForwarded, tk, &row_to_edit))
        return CLDB_E_RECORD_NOTFOUND;
    
    if (dwMappingFlags != std::numeric_limits<uint32_t>::max())
    {
        uint32_t mappingFlags = dwMappingFlags;
        if (1 != md_set_column_value_as_constant(row_to_edit, mdtImplMap_MappingFlags, 1, &mappingFlags))
            return E_FAIL;
    }
    
    if (szImportName != nullptr)
    {
        pal::StringConvert<WCHAR, char> cvt(szImportName);
        char const* name = cvt;
        if (1 != md_set_column_value_as_utf8(row_to_edit, mdtImplMap_ImportName, 1, &name))
            return E_FAIL;
    }
    
    if (1 != md_set_column_value_as_token(row_to_edit, mdtImplMap_ImportScope, 1, &mrImportDLL))
        return E_FAIL;
    
    // TODO: Update EncLog
    return S_OK;
}

HRESULT MetadataEmit::DeletePinvokeMap(
        mdToken     tk)
{
    HRESULT hr;
    assert(TypeFromToken(tk) == mdtFieldDef || TypeFromToken(tk) == mdtMethodDef);
    assert(!IsNilToken(tk));

    RETURN_IF_FAILED(DeleteParentedToken(
        MetaData(),
        tk,
        mdtid_ImplMap,
        mdtImplMap_MemberForwarded));
    
    RETURN_IF_FAILED(RemoveFlag(
        MetaData(),
        tk,
        TypeFromToken(tk) == mdtFieldDef ? mdtField_Flags : mdtMethodDef_Flags,
        TypeFromToken(tk) == mdtFieldDef ? (uint32_t)fdPinvokeImpl : (uint32_t)mdPinvokeImpl));
    
    return S_OK;
}


HRESULT MetadataEmit::DefineCustomAttribute(
        mdToken     tkOwner,
        mdToken     tkCtor,
        void const  *pCustomAttribute,
        ULONG       cbCustomAttribute,
        mdCustomAttribute *pcv)
{
    if (TypeFromToken(tkOwner) == mdtCustomAttribute)
        return E_INVALIDARG;

    if (IsNilToken(tkOwner)
        || IsNilToken(tkCtor)
        || (TypeFromToken(tkCtor) != mdtMethodDef
            && TypeFromToken(tkCtor) != mdtMemberRef) )
    {
        return E_INVALIDARG;
    }

    // TODO: Recognize pseudoattributes and handle them appropriately.

    // We hand out tokens here, so we can't move rows to keep the parent column sorted.
    md_added_row_t new_row;
    if (!md_append_row(MetaData(), mdtid_CustomAttribute, &new_row))
        return E_FAIL;
    
    if (1 != md_set_column_value_as_token(new_row, mdtCustomAttribute_Parent, 1, &tkOwner))
        return E_FAIL;
    
    if (1 != md_set_column_value_as_token(new_row, mdtCustomAttribute_Type, 1, &tkCtor))
        return E_FAIL;
    
    uint8_t const* pCustomAttributeBlob = (uint8_t const*)pCustomAttribute;
    uint32_t customAttributeBlobLen = cbCustomAttribute;
    if (1 != md_set_column_value_as_blob(new_row, mdtCustomAttribute_Value, 1, &pCustomAttributeBlob, &customAttributeBlobLen))
        return E_FAIL;
    
    if (!md_cursor_to_token(new_row, pcv))
        return CLDB_E_FILE_CORRUPT;
    
    // TODO: Update EncLog
    return S_OK;
}

HRESULT MetadataEmit::SetCustomAttributeValue(
        mdCustomAttribute pcv,
        void const  *pCustomAttribute,
        ULONG       cbCustomAttribute)
{
    if (TypeFromToken(pcv) != mdtCustomAttribute)
        return E_INVALIDARG;
    
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), pcv, &c))
        return CLDB_E_FILE_CORRUPT;

    uint8_t const* pCustomAttributeBlob = (uint8_t const*)pCustomAttribute;
    uint32_t customAttributeBlobLen = cbCustomAttribute;
    if (1 != md_set_column_value_as_blob(c, mdtCustomAttribute_Value, 1, &pCustomAttributeBlob, &customAttributeBlobLen))
        return E_FAIL;
    
    // TODO: Update EncLog
    return S_OK;
}

namespace
{
    // Determine the blob size base of the ELEMENT_TYPE_* associated with the blob.
    // This cannot be a table lookup because ELEMENT_TYPE_STRING is an unicode string.
    uint32_t GetSizeOfConstantBlob(
        int32_t  type,
        void const* pValue,
        uint32_t  strLen)
    {
        uint32_t size = 0;

        switch (type)
        {
        case ELEMENT_TYPE_BOOLEAN:
            size = sizeof(bool);
            break;
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
            size = sizeof(uint8_t);
            break;
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
            size = sizeof(uint16_t);
            break;
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_R4:
            size = sizeof(uint32_t);

            break;

        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R8:
            size = sizeof(uint64_t);
            break;

        case ELEMENT_TYPE_STRING:
            if (pValue == 0)
                size = 0;
            else
            if (strLen != (uint32_t) -1)
                size = strLen * sizeof(WCHAR);
            else
                size = (uint32_t)(sizeof(WCHAR) * PAL_wcslen((LPWSTR)pValue));
            break;

        case ELEMENT_TYPE_CLASS:
            // The only legal value is a null pointer, and on 32 bit platforms we've already
            // stored 32 bits, so we will use just 32 bits of null.  If the type is
            // E_T_CLASS, the caller should know that the value is always null anyway.
            size = sizeof(uint32_t);
            break;
        default:
            assert(!"Not a valid type to specify default value!");
            break;
        }
        return size;
    }
}

HRESULT MetadataEmit::DefineField(
        mdTypeDef   td,
        LPCWSTR     szName,
        DWORD       dwFieldFlags,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdFieldDef  *pmd)
{
    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;
    
    char const* name = cvt;

    md_added_row_t c;
    mdcursor_t typeDef;
    if (!md_token_to_cursor(MetaData(), td, &typeDef))
        return CLDB_E_FILE_CORRUPT;
    
    if (!md_add_new_row_to_list(typeDef, mdtTypeDef_FieldList, &c))
        return E_FAIL;
    
    if (1 != md_set_column_value_as_utf8(c, mdtField_Name, 1, &name))
        return E_FAIL;
    
    bool hasConstant = false;
    // See if there is a Constant.
    if ((dwCPlusTypeFlag != ELEMENT_TYPE_VOID && dwCPlusTypeFlag != ELEMENT_TYPE_END &&
         dwCPlusTypeFlag != UINT32_MAX) &&
        (pValue || (pValue == 0 && (dwCPlusTypeFlag == ELEMENT_TYPE_STRING ||
                                    dwCPlusTypeFlag == ELEMENT_TYPE_CLASS))))
    {
        hasConstant = true;
    }

    if (dwFieldFlags != std::numeric_limits<DWORD>::max())
    {
        // TODO: Handle reserved flags
        uint32_t fieldFlags = dwFieldFlags;

        // If the field name has the special name for enum fields,
        // set the special name and RTSpecialName flags.
        // COMPAT: CoreCLR does not check if the field is actually in an enum type.
        if (strcmp(name, COR_ENUM_FIELD_NAME) == 0)
        {
            fieldFlags |= fdRTSpecialName | fdSpecialName;
        }
        if (1 != md_set_column_value_as_constant(c, mdtField_Flags, 1, &fieldFlags))
            return E_FAIL;
    }
    else
    {
        uint32_t fieldFlags = 0;

        // If the field name has the special name for enum fields,
        // set the special name and RTSpecialName flags.
        // COMPAT: CoreCLR does not check if the field is actually in an enum type.
        if (strcmp(name, COR_ENUM_FIELD_NAME) == 0)
        {
            fieldFlags |= fdRTSpecialName | fdSpecialName;
        }
        if (1 != md_set_column_value_as_constant(c, mdtField_Flags, 1, &fieldFlags))
            return E_FAIL;
    }

    uint8_t const* sig = (uint8_t const*)pvSigBlob;
    uint32_t sigLength = cbSigBlob;
    if (sigLength != 0)
    {
        if (1 != md_set_column_value_as_blob(c, mdtField_Signature, 1, &sig, &sigLength))
            return E_FAIL;
    }

    if (hasConstant)
    {
        md_added_row_t constant;
        if (!md_append_row(MetaData(), mdtid_Constant, &constant))
            return E_FAIL;
        
        if (1 != md_set_column_value_as_cursor(constant, mdtConstant_Parent, 1, &c))
            return E_FAIL;
        
        uint32_t type = dwCPlusTypeFlag;
        if (1 != md_set_column_value_as_constant(constant, mdtConstant_Type, 1, &type))
            return E_FAIL;
        
        uint64_t defaultConstantValue = 0;
        uint8_t const* pConstantValue = (uint8_t const*)pValue;
        if (pConstantValue == nullptr)
            pConstantValue = (uint8_t const*)&defaultConstantValue;
        
        uint32_t constantSize = GetSizeOfConstantBlob(dwCPlusTypeFlag, pConstantValue, cchValue);
        if (1 != md_set_column_value_as_blob(constant, mdtConstant_Value, 1, &pConstantValue, &constantSize))
            return E_FAIL;
    
    }

    if (!md_cursor_to_token(c, pmd))
        return CLDB_E_FILE_CORRUPT;
    
    // TODO: Update EncLog
    return S_OK;
}

HRESULT MetadataEmit::DefineProperty(
        mdTypeDef   td,
        LPCWSTR     szProperty,
        DWORD       dwPropFlags,
        PCCOR_SIGNATURE pvSig,
        ULONG       cbSig,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdMethodDef mdSetter,
        mdMethodDef mdGetter,
        mdMethodDef rmdOtherMethods[],
        mdProperty  *pmdProp)
{
    return FindOrCreateParentedRow(
        MetaData(),
        td,
        mdtid_PropertyMap,
        mdtPropertyMap_Parent,
        [=] (mdcursor_t map)
        {
            HRESULT hr;
            md_added_row_t c;
            if (!md_add_new_row_to_list(map, mdtPropertyMap_Parent, &c))
                return E_FAIL;
            
            pal::StringConvert<WCHAR, char> cvt(szProperty);
            if (!cvt.Success())
                return E_INVALIDARG;
            
            char const* name = cvt;
            if (1 != md_set_column_value_as_utf8(c, mdtProperty_Name, 1, &name))
                return E_FAIL;


            if (pvSig != nullptr)
            {
                uint8_t const* sig = (uint8_t const*)pvSig;
                uint32_t sigLength = cbSig;
                if (1 != md_set_column_value_as_blob(c, mdtProperty_Type, 1, &sig, &sigLength))
                    return E_FAIL;
            }

            uint32_t propFlags = (uint32_t)dwPropFlags;
            if (propFlags != std::numeric_limits<uint32_t>::max())
            {
                propFlags &= ~prReservedMask;  
            }
            else
            {
                propFlags = 0;
            }

            bool hasConstant = false;
            // See if there is a Constant.
            if ((dwCPlusTypeFlag != ELEMENT_TYPE_VOID && dwCPlusTypeFlag != ELEMENT_TYPE_END &&
                dwCPlusTypeFlag != UINT32_MAX) &&
                (pValue || (pValue == 0 && (dwCPlusTypeFlag == ELEMENT_TYPE_STRING ||
                                            dwCPlusTypeFlag == ELEMENT_TYPE_CLASS))))
            {
                if (propFlags == std::numeric_limits<uint32_t>::max())
                    propFlags = 0;
                propFlags |= prHasDefault;
                hasConstant = true;
            }

            if (1 != md_set_column_value_as_constant(c, mdtProperty_Flags, 1, &propFlags))
                return E_FAIL;

            if (mdGetter != mdMethodDefNil)
            {
                RETURN_IF_FAILED(AddMethodSemantic(MetaData(), c, msGetter, mdGetter));
            }

            if (mdSetter != mdMethodDefNil)
            {
                RETURN_IF_FAILED(AddMethodSemantic(MetaData(), c, msSetter, mdSetter));
            }
        
            if (rmdOtherMethods)
            {
                for (size_t i = 0; RidFromToken(rmdOtherMethods[i]) != mdTokenNil; ++i)
                {
                    RETURN_IF_FAILED(AddMethodSemantic(MetaData(), c, msOther, rmdOtherMethods[i]));
                }
            }
            
            if (hasConstant)
            {
                md_added_row_t constant;
                if (!md_append_row(MetaData(), mdtid_Constant, &constant))
                    return E_FAIL;
                
                if (1 != md_set_column_value_as_cursor(constant, mdtConstant_Parent, 1, &c))
                    return E_FAIL;
                
                uint32_t type = dwCPlusTypeFlag;
                if (1 != md_set_column_value_as_constant(constant, mdtConstant_Type, 1, &type))
                    return E_FAIL;
                
                uint64_t defaultConstantValue = 0;
                uint8_t const* pConstantValue = (uint8_t const*)pValue;
                if (pConstantValue == nullptr)
                    pConstantValue = (uint8_t const*)&defaultConstantValue;
                
                uint32_t constantSize = GetSizeOfConstantBlob(dwCPlusTypeFlag, pConstantValue, cchValue);
                if (1 != md_set_column_value_as_blob(constant, mdtConstant_Value, 1, &pConstantValue, &constantSize))
                    return E_FAIL;
            }

            if (!md_cursor_to_token(c, pmdProp))
                return CLDB_E_FILE_CORRUPT;
            
            // TODO: Update EncLog

            return S_OK;
        }
    );
}

HRESULT MetadataEmit::DefineParam(
        mdMethodDef md,
        ULONG       ulParamSeq,
        LPCWSTR     szName,
        DWORD       dwParamFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdParamDef  *ppd)
{
    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;
    
    char const* name = cvt;

    md_added_row_t c;
    mdcursor_t method;
    if (!md_token_to_cursor(MetaData(), md, &method))
        return CLDB_E_FILE_CORRUPT;
    
    if (!md_add_new_row_to_sorted_list(method, mdtMethodDef_ParamList, mdtParam_Sequence, (uint32_t)ulParamSeq, &c))
        return E_FAIL;
    
    if (1 != md_set_column_value_as_utf8(c, mdtParam_Name, 1, &name))
        return E_FAIL;
    
    bool hasConstant = false;
    // See if there is a Constant.
    if ((dwCPlusTypeFlag != ELEMENT_TYPE_VOID && dwCPlusTypeFlag != ELEMENT_TYPE_END &&
         dwCPlusTypeFlag != UINT32_MAX) &&
        (pValue || (pValue == 0 && (dwCPlusTypeFlag == ELEMENT_TYPE_STRING ||
                                    dwCPlusTypeFlag == ELEMENT_TYPE_CLASS))))
    {
        hasConstant = true;
    }

    if (dwParamFlags != std::numeric_limits<DWORD>::max())
    {
        // TODO: Handle reserved flags
        uint32_t flags = dwParamFlags;

        if (1 != md_set_column_value_as_constant(c, mdtParam_Flags, 1, &flags))
            return E_FAIL;
    }
    else
    {
        uint32_t flags = 0;
        if (1 != md_set_column_value_as_constant(c, mdtParam_Flags, 1, &flags))
            return E_FAIL;
    }

    if (hasConstant)
    {
        md_added_row_t constant;
        if (!md_append_row(MetaData(), mdtid_Constant, &constant))
            return E_FAIL;
        
        if (1 != md_set_column_value_as_cursor(constant, mdtConstant_Parent, 1, &c))
            return E_FAIL;
        
        uint32_t type = dwCPlusTypeFlag;
        if (1 != md_set_column_value_as_constant(constant, mdtConstant_Type, 1, &type))
            return E_FAIL;
        
        uint64_t defaultConstantValue = 0;
        uint8_t const* pConstantValue = (uint8_t const*)pValue;
        if (pConstantValue == nullptr)
            pConstantValue = (uint8_t const*)&defaultConstantValue;
        
        uint32_t constantSize = GetSizeOfConstantBlob(dwCPlusTypeFlag, pConstantValue, cchValue);
        if (1 != md_set_column_value_as_blob(constant, mdtConstant_Value, 1, &pConstantValue, &constantSize))
            return E_FAIL;
    
    }

    if (!md_cursor_to_token(c, ppd))
        return CLDB_E_FILE_CORRUPT;
    
    // TODO: Update EncLog
    return S_OK;
}

HRESULT MetadataEmit::SetFieldProps(
        mdFieldDef  fd,
        DWORD       dwFieldFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), fd, &c))
        return CLDB_E_FILE_CORRUPT;
    
    bool hasConstant = false;
    // See if there is a Constant.
    if ((dwCPlusTypeFlag != ELEMENT_TYPE_VOID && dwCPlusTypeFlag != ELEMENT_TYPE_END &&
         dwCPlusTypeFlag != UINT32_MAX) &&
        (pValue || (pValue == 0 && (dwCPlusTypeFlag == ELEMENT_TYPE_STRING ||
                                    dwCPlusTypeFlag == ELEMENT_TYPE_CLASS))))
    {
        hasConstant = true;
    }

    if (dwFieldFlags != std::numeric_limits<DWORD>::max())
    {
        // TODO: Handle reserved flags
        uint32_t fieldFlags = dwFieldFlags;
        if (1 != md_set_column_value_as_constant(c, mdtField_Flags, 1, &fieldFlags))
            return E_FAIL;
    }

    if (hasConstant)
    {
        // Create or update the Constant record that points to this field.
        return FindOrCreateParentedRow(MetaData(), fd, mdtid_Constant, mdtConstant_Parent, [=](mdcursor_t constant)
        {        
            uint32_t type = dwCPlusTypeFlag;
            if (1 != md_set_column_value_as_constant(constant, mdtConstant_Type, 1, &type))
                return E_FAIL;
            
            uint64_t defaultConstantValue = 0;
            uint8_t const* pConstantValue = (uint8_t const*)pValue;
            if (pConstantValue == nullptr)
                pConstantValue = (uint8_t const*)&defaultConstantValue;
            
            uint32_t constantSize = GetSizeOfConstantBlob(dwCPlusTypeFlag, pConstantValue, cchValue);
            if (1 != md_set_column_value_as_blob(constant, mdtConstant_Value, 1, &pConstantValue, &constantSize))
                return E_FAIL;

            return S_OK;
        });
    }
    return S_OK;
}

HRESULT MetadataEmit::SetPropertyProps(
        mdProperty  pr,
        DWORD       dwPropFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdMethodDef mdSetter,
        mdMethodDef mdGetter,
        mdMethodDef rmdOtherMethods[])
{
    HRESULT hr;
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), pr, &c))
        return CLDB_E_FILE_CORRUPT;

    if (dwPropFlags != std::numeric_limits<DWORD>::max())
    {
        dwPropFlags &= ~prReservedMask;  
    }

    bool hasConstant = false;
    // See if there is a Constant.
    if ((dwCPlusTypeFlag != ELEMENT_TYPE_VOID && dwCPlusTypeFlag != ELEMENT_TYPE_END &&
        dwCPlusTypeFlag != UINT32_MAX) &&
        (pValue || (pValue == 0 && (dwCPlusTypeFlag == ELEMENT_TYPE_STRING ||
                                    dwCPlusTypeFlag == ELEMENT_TYPE_CLASS))))
    {
        if (dwPropFlags == std::numeric_limits<DWORD>::max())
            dwPropFlags = 0;
        dwPropFlags |= prHasDefault;
        hasConstant = true;
    }

    if (dwPropFlags != std::numeric_limits<DWORD>::max())
    {
        // TODO: Preserve reserved flags
        uint32_t flags = dwPropFlags;
        if (1 != md_set_column_value_as_constant(c, mdtProperty_Flags, 1, &flags))
            return E_FAIL;   
    }

    if (mdGetter != mdMethodDefNil)
    {
        RETURN_IF_FAILED(RemoveSemantics(MetaData(), pr, msGetter));
        RETURN_IF_FAILED(AddMethodSemantic(MetaData(), c, msGetter, mdGetter));
    }

    if (mdSetter != mdMethodDefNil)
    {
        RETURN_IF_FAILED(RemoveSemantics(MetaData(), pr, msSetter));
        RETURN_IF_FAILED(AddMethodSemantic(MetaData(), c, msSetter, mdSetter));
    }

    if (rmdOtherMethods)
    {
        RETURN_IF_FAILED(RemoveSemantics(MetaData(), pr, msOther));
        for (size_t i = 0; RidFromToken(rmdOtherMethods[i]) != mdTokenNil; ++i)
        {
            RETURN_IF_FAILED(AddMethodSemantic(MetaData(), c, msOther, rmdOtherMethods[i]));
        }
    }
    
    if (hasConstant)
    {
        // Create or update the Constant record that points to this property.
        return FindOrCreateParentedRow(MetaData(), pr, mdtid_Constant, mdtConstant_Parent, [=](mdcursor_t constant)
        {        
            uint32_t type = dwCPlusTypeFlag;
            if (1 != md_set_column_value_as_constant(constant, mdtConstant_Type, 1, &type))
                return E_FAIL;
            
            uint64_t defaultConstantValue = 0;
            uint8_t const* pConstantValue = (uint8_t const*)pValue;
            if (pConstantValue == nullptr)
                pConstantValue = (uint8_t const*)&defaultConstantValue;
            
            uint32_t constantSize = GetSizeOfConstantBlob(dwCPlusTypeFlag, pConstantValue, cchValue);
            if (1 != md_set_column_value_as_blob(constant, mdtConstant_Value, 1, &pConstantValue, &constantSize))
                return E_FAIL;

            return S_OK;
        });
    }

    // TODO: Update EncLog

    return S_OK;
}

HRESULT MetadataEmit::SetParamProps(
        mdParamDef  pd,
        LPCWSTR     szName,
        DWORD       dwParamFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), pd, &c))
        return CLDB_E_FILE_CORRUPT;
    
    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;
    
    char const* name = cvt;
    if (1 != md_set_column_value_as_utf8(c, mdtParam_Name, 1, &name))
        return E_FAIL;
    
    bool hasConstant = false;
    // See if there is a Constant.
    if ((dwCPlusTypeFlag != ELEMENT_TYPE_VOID && dwCPlusTypeFlag != ELEMENT_TYPE_END &&
         dwCPlusTypeFlag != UINT32_MAX) &&
        (pValue || (pValue == 0 && (dwCPlusTypeFlag == ELEMENT_TYPE_STRING ||
                                    dwCPlusTypeFlag == ELEMENT_TYPE_CLASS))))
    {
        hasConstant = true;
    }

    if (dwParamFlags != std::numeric_limits<DWORD>::max())
    {
        // TODO: Handle reserved flags
        uint32_t flags = dwParamFlags;
        if (1 != md_set_column_value_as_constant(c, mdtParam_Flags, 1, &flags))
            return E_FAIL;
    }

    if (hasConstant)
    {
        // Create or update the Constant record that points to this field.
        return FindOrCreateParentedRow(MetaData(), pd, mdtid_Constant, mdtConstant_Parent, [=](mdcursor_t constant)
        {        
            uint32_t type = dwCPlusTypeFlag;
            if (1 != md_set_column_value_as_constant(constant, mdtConstant_Type, 1, &type))
                return E_FAIL;
            
            uint64_t defaultConstantValue = 0;
            uint8_t const* pConstantValue = (uint8_t const*)pValue;
            if (pConstantValue == nullptr)
                pConstantValue = (uint8_t const*)&defaultConstantValue;
            
            uint32_t constantSize = GetSizeOfConstantBlob(dwCPlusTypeFlag, pConstantValue, cchValue);
            if (1 != md_set_column_value_as_blob(constant, mdtConstant_Value, 1, &pConstantValue, &constantSize))
                return E_FAIL;

            return S_OK;
        });
    }

    return S_OK;
}


HRESULT MetadataEmit::DefineSecurityAttributeSet(
        mdToken     tkObj,
        COR_SECATTR rSecAttrs[],
        ULONG       cSecAttrs,
        ULONG       *pulErrorAttr)
{
    // Not implemented in CoreCLR
    UNREFERENCED_PARAMETER(tkObj);
    UNREFERENCED_PARAMETER(rSecAttrs);
    UNREFERENCED_PARAMETER(cSecAttrs);
    UNREFERENCED_PARAMETER(pulErrorAttr);
    return E_NOTIMPL;
}

HRESULT MetadataEmit::ApplyEditAndContinue(
        IUnknown    *pImport)
{
    HRESULT hr;
    dncp::com_ptr<IDNMDOwner> delta;
    RETURN_IF_FAILED(pImport->QueryInterface(IID_IDNMDOwner, (void**)&delta));

    if (!md_apply_delta(MetaData(), delta->MetaData()))
        return E_INVALIDARG;

    // TODO: Reset and copy EncLog from delta metadata to this metadata.
    return S_OK;
}

HRESULT MetadataEmit::TranslateSigWithScope(
        IMetaDataAssemblyImport *pAssemImport,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *import,
        PCCOR_SIGNATURE pbSigBlob,
        ULONG       cbSigBlob,
        IMetaDataAssemblyEmit *pAssemEmit,
        IMetaDataEmit *emit,
        PCOR_SIGNATURE pvTranslatedSig,
        ULONG       cbTranslatedSigMax,
        ULONG       *pcbTranslatedSig)
{
    HRESULT hr;
    dncp::com_ptr<IDNMDOwner> assemImport{};

    if (pAssemImport != nullptr)
        RETURN_IF_FAILED(pAssemImport->QueryInterface(IID_IDNMDOwner, (void**)&assemImport));

    dncp::com_ptr<IDNMDOwner> assemEmit{};
    if (pAssemEmit != nullptr)
        RETURN_IF_FAILED(pAssemEmit->QueryInterface(IID_IDNMDOwner, (void**)&assemEmit));

    if (import == nullptr)
        return E_INVALIDARG;
    
    dncp::com_ptr<IDNMDOwner> moduleImport{};
    RETURN_IF_FAILED(import->QueryInterface(IID_IDNMDOwner, (void**)&moduleImport));
    
    dncp::com_ptr<IDNMDOwner> moduleEmit{};
    RETURN_IF_FAILED(emit->QueryInterface(IID_IDNMDOwner, (void**)&moduleEmit));
    
    malloc_span<uint8_t> translatedSig;
    RETURN_IF_FAILED(ImportSignatureIntoModule(
        assemImport->MetaData(),
        moduleImport->MetaData(),
        { reinterpret_cast<uint8_t const*>(pbHashValue), cbHashValue },
        assemEmit->MetaData(),
        moduleEmit->MetaData(),
        { pbSigBlob, cbSigBlob },
        [](mdcursor_t){},
        translatedSig));
    
    std::copy_n(translatedSig.begin(), std::min(translatedSig.size(), (size_t)cbTranslatedSigMax), (uint8_t*)pvTranslatedSig);

    *pcbTranslatedSig = (ULONG)translatedSig.size();
    return translatedSig.size() > cbTranslatedSigMax ? CLDB_S_TRUNCATION : S_OK;
}

HRESULT MetadataEmit::SetMethodImplFlags(
        mdMethodDef md,
        DWORD       dwImplFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), md, &c))
        return E_INVALIDARG;
    
    uint32_t flags = (uint32_t)dwImplFlags;
    if (1 != md_set_column_value_as_constant(c, mdtMethodDef_ImplFlags, 1, &flags))
        return E_FAIL;

    // TODO: Update ENC log
    return S_OK;
}

HRESULT MetadataEmit::SetFieldRVA(
        mdFieldDef  fd,
        ULONG       ulRVA)
{
    uint32_t rva = (uint32_t)ulRVA;

    HRESULT hr = FindOrCreateParentedRow(MetaData(), fd, mdtid_FieldRva, mdtFieldRva_Field, [=](mdcursor_t c)
    {
        if (1 != md_set_column_value_as_constant(c, mdtFieldRva_Rva, 1, &rva))
            return E_FAIL;

        return S_OK;
    });

    RETURN_IF_FAILED(hr);

    mdcursor_t field;
    if (!md_token_to_cursor(MetaData(), fd, &field))
        return E_INVALIDARG;

    uint32_t flags;
    if (1 != md_get_column_value_as_constant(field, mdtField_Flags, 1, &flags))
        return CLDB_E_FILE_CORRUPT;

    flags |= fdHasFieldRVA;
    if (1 != md_set_column_value_as_constant(field, mdtField_Flags, 1, &flags))
        return E_FAIL;

    // TODO: Update ENC log
    
    return S_OK;
}

HRESULT MetadataEmit::Merge(
        IMetaDataImport *pImport,
        IMapToken   *pHostMapToken,
        IUnknown    *pHandler)
{
    // Not Implemented in CoreCLR
    UNREFERENCED_PARAMETER(pImport);
    UNREFERENCED_PARAMETER(pHostMapToken);
    UNREFERENCED_PARAMETER(pHandler);
    return E_NOTIMPL;
}

HRESULT MetadataEmit::MergeEnd()
{
    // Not Implemented in CoreCLR
    return E_NOTIMPL;
}

HRESULT MetadataEmit::DefineMethodSpec(
        mdToken     tkParent,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMethodSpec *pmi)
{
    if (TypeFromToken(tkParent) != mdtMethodDef && TypeFromToken(tkParent) != mdtMemberRef)
        return META_E_BAD_INPUT_PARAMETER;
    
    if (cbSigBlob == 0 || pvSigBlob == nullptr || pmi == nullptr)
        return META_E_BAD_INPUT_PARAMETER;

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_MethodSpec, &c))
        return E_FAIL;

    if (1 != md_set_column_value_as_token(c, mdtMethodSpec_Method, 1, &tkParent))
        return E_FAIL;

    uint32_t sigLength = cbSigBlob;
    if (1 != md_set_column_value_as_blob(c, mdtMethodSpec_Instantiation, 1, &pvSigBlob, &sigLength))
        return E_FAIL;

    if (!md_cursor_to_token(c, pmi))
        return CLDB_E_FILE_CORRUPT;

    // TODO: Update EncLog
    return S_OK;
}

// TODO: Add EnC mode support to the emit implementation.
// Maybe we can do a layering model where we have a base emit implementation that doesn't support EnC,
// and then a wrapper that does?
HRESULT MetadataEmit::GetDeltaSaveSize(
        CorSaveSize fSave,
        DWORD       *pdwSaveSize)
{
    UNREFERENCED_PARAMETER(fSave);
    UNREFERENCED_PARAMETER(pdwSaveSize);
    return META_E_NOT_IN_ENC_MODE;
}

HRESULT MetadataEmit::SaveDelta(
        LPCWSTR     szFile,
        DWORD       dwSaveFlags)
{
    UNREFERENCED_PARAMETER(szFile);
    UNREFERENCED_PARAMETER(dwSaveFlags);
    return META_E_NOT_IN_ENC_MODE;
}

HRESULT MetadataEmit::SaveDeltaToStream(
        IStream     *pIStream,
        DWORD       dwSaveFlags)
{
    UNREFERENCED_PARAMETER(pIStream);
    UNREFERENCED_PARAMETER(dwSaveFlags);
    return META_E_NOT_IN_ENC_MODE;
}

HRESULT MetadataEmit::SaveDeltaToMemory(
        void        *pbData,
        ULONG       cbData)
{
    UNREFERENCED_PARAMETER(pbData);
    UNREFERENCED_PARAMETER(cbData);
    return META_E_NOT_IN_ENC_MODE;
}

HRESULT MetadataEmit::DefineGenericParam(
        mdToken      tk,
        ULONG        ulParamSeq,
        DWORD        dwParamFlags,
        LPCWSTR      szname,
        DWORD        reserved,
        mdToken      rtkConstraints[],
        mdGenericParam *pgp)
{
    if (reserved != 0)
        return META_E_BAD_INPUT_PARAMETER;
    
    if (TypeFromToken(tk) != mdtMethodDef && TypeFromToken(tk) != mdtTypeDef)
        return META_E_BAD_INPUT_PARAMETER;
    
    // TODO: Check for duplicates

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_GenericParam, &c))
        return E_FAIL;
    
    if (1 != md_set_column_value_as_token(c, mdtGenericParam_Owner, 1, &tk))
        return E_FAIL;
    
    uint32_t paramSeq = ulParamSeq;
    if (1 != md_set_column_value_as_constant(c, mdtGenericParam_Number, 1, &paramSeq))
        return E_FAIL;
    
    uint32_t flags = dwParamFlags;
    if (1 != md_set_column_value_as_constant(c, mdtGenericParam_Flags, 1, &flags))
        return E_FAIL;
    
    if (szname != nullptr)
    {
        pal::StringConvert<WCHAR, char> cvt(szname);
        if (!cvt.Success())
            return E_INVALIDARG;

        char const* name = cvt;
        if (1 != md_set_column_value_as_utf8(c, mdtGenericParam_Name, 1, &name))
            return E_FAIL;
    }
    else
    {
        char const* name = nullptr;
        if (1 != md_set_column_value_as_utf8(c, mdtGenericParam_Name, 1, &name))
            return E_FAIL;
    }

    if (rtkConstraints != nullptr)
    {
        for (size_t i = 0; RidFromToken(rtkConstraints[i]) != mdTokenNil; i++)
        {
            md_added_row_t added_row;
            if (!md_append_row(MetaData(), mdtid_GenericParamConstraint, &added_row))
                return E_FAIL;
            
            if (1 != md_set_column_value_as_cursor(added_row, mdtGenericParamConstraint_Owner, 1, &c))
                return E_FAIL;
            
            if (1 != md_set_column_value_as_token(added_row, mdtGenericParamConstraint_Constraint, 1, &rtkConstraints[i]))
                return E_FAIL;
            
            // TODO: Update EncLog
        }
    }

    // TODO: Update EncLog
    if (!md_cursor_to_token(c, pgp))
        return CLDB_E_FILE_CORRUPT;
    
    return S_OK;
}

HRESULT MetadataEmit::SetGenericParamProps(
        mdGenericParam gp,
        DWORD        dwParamFlags,
        LPCWSTR      szName,
        DWORD        reserved,
        mdToken      rtkConstraints[])
{
    if (reserved != 0)
        return META_E_BAD_INPUT_PARAMETER;
    
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), gp, &c))
        return E_INVALIDARG;
    
    uint32_t flags = dwParamFlags;
    if (1 != md_set_column_value_as_constant(c, mdtGenericParam_Flags, 1, &flags))
        return E_FAIL;
    
    if (szName != nullptr)
    {
        pal::StringConvert<WCHAR, char> cvt(szName);
        if (!cvt.Success())
            return E_INVALIDARG;

        char const* name = cvt;
        if (1 != md_set_column_value_as_utf8(c, mdtGenericParam_Name, 1, &name))
            return E_FAIL;
    }

    if (rtkConstraints != nullptr)
    {
        // Delete all existing constraints
        mdcursor_t constraint;
        uint32_t count;
        if (!md_create_cursor(MetaData(), mdtid_GenericParamConstraint, &constraint, &count))
            return E_FAIL;
        
        md_range_result_t result = md_find_range_from_cursor(constraint, mdtGenericParamConstraint_Owner, gp, &constraint, &count);
        if (result != MD_RANGE_NOT_FOUND)
        {
            for (uint32_t i = 0; i < count; ++i, md_cursor_next(&constraint))
            {
                mdToken parent;
                if (1 != md_get_column_value_as_token(constraint, mdtGenericParamConstraint_Owner, 1, &parent))
                    return E_FAIL;
                
                if (parent == gp)
                {
                    parent = mdGenericParamNil;
                    if (1 != md_set_column_value_as_token(constraint, mdtGenericParamConstraint_Owner, 1, &parent))
                        return E_FAIL;
                }
            }
        }

        for (size_t i = 0; RidFromToken(rtkConstraints[i]) != mdTokenNil; i++)
        {
            md_added_row_t added_row;
            if (!md_append_row(MetaData(), mdtid_GenericParamConstraint, &added_row))
                return E_FAIL;
            
            if (1 != md_set_column_value_as_cursor(added_row, mdtGenericParamConstraint_Owner, 1, &c))
                return E_FAIL;
            
            if (1 != md_set_column_value_as_token(added_row, mdtGenericParamConstraint_Constraint, 1, &rtkConstraints[i]))
                return E_FAIL;
            
            // TODO: Update EncLog
        }
    }

    // TODO: Update EncLog
    
    return S_OK;
}

HRESULT MetadataEmit::ResetENCLog()
{
    return META_E_NOT_IN_ENC_MODE;
}

HRESULT MetadataEmit::DefineAssembly(
        void const  *pbPublicKey,
        ULONG       cbPublicKey,
        ULONG       ulHashAlgId,
        LPCWSTR     szName,
        ASSEMBLYMETADATA const *pMetaData,
        DWORD       dwAssemblyFlags,
        mdAssembly  *pma)
{
    if (szName == nullptr || pMetaData == nullptr || pma == nullptr)
        return E_INVALIDARG;

    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;
    
    mdcursor_t c;
    uint32_t count;
    if (!md_create_cursor(MetaData(), mdtid_Assembly, &c, &count))
    {
        if (md_append_row(MetaData(), mdtid_Assembly, &c))
        {
            md_commit_row_add(c);
        }
        else
        {
            return E_FAIL;
        }
    }

    uint32_t assemblyFlags = dwAssemblyFlags;
    if (cbPublicKey != 0)
    {
        assemblyFlags |= afPublicKey;
    }
    
    const uint8_t* publicKey = (const uint8_t*)pbPublicKey;
    if (publicKey != nullptr)
    {
        uint32_t publicKeyLength = cbPublicKey;
        if (1 != md_set_column_value_as_blob(c, mdtAssembly_PublicKey, 1, &publicKey, &publicKeyLength))
            return E_FAIL;
    }
    else
    {
        uint32_t publicKeyLength = 0;
        if (1 != md_set_column_value_as_blob(c, mdtAssembly_PublicKey, 1, &publicKey, &publicKeyLength))
            return E_FAIL;
    }
    
    if (1 != md_set_column_value_as_constant(c, mdtAssembly_Flags, 1, &assemblyFlags))
        return E_FAIL;

    char const* name = cvt;
    if (1 != md_set_column_value_as_utf8(c, mdtAssembly_Name, 1, &name))
        return E_FAIL;
    
    uint32_t hashAlgId = ulHashAlgId;
    if (1 != md_set_column_value_as_constant(c, mdtAssembly_HashAlgId, 1, &hashAlgId))
        return E_FAIL;

    uint32_t majorVersion = pMetaData->usMajorVersion != std::numeric_limits<uint16_t>::max() ? pMetaData->usMajorVersion : 0;
    if (1 != md_set_column_value_as_constant(c, mdtAssembly_MajorVersion, 1, &majorVersion))
        return E_FAIL;

    uint32_t minorVersion = pMetaData->usMinorVersion != std::numeric_limits<uint16_t>::max() ? pMetaData->usMinorVersion : 0;
    if (1 != md_set_column_value_as_constant(c, mdtAssembly_MinorVersion, 1, &minorVersion))
        return E_FAIL;
    
    uint32_t buildNumber = pMetaData->usBuildNumber != std::numeric_limits<uint16_t>::max() ? pMetaData->usBuildNumber : 0;
    if (1 != md_set_column_value_as_constant(c, mdtAssembly_BuildNumber, 1, &buildNumber))
        return E_FAIL;
    
    uint32_t revisionNumber = pMetaData->usRevisionNumber != std::numeric_limits<uint16_t>::max() ? pMetaData->usRevisionNumber : 0;
    if (1 != md_set_column_value_as_constant(c, mdtAssembly_RevisionNumber, 1, &revisionNumber))
        return E_FAIL;
    
    if (pMetaData->szLocale != nullptr)
    {
        pal::StringConvert<WCHAR, char> cvtLocale(pMetaData->szLocale);
        if (!cvtLocale.Success())
            return E_INVALIDARG;

        char const* locale = cvtLocale;
        if (1 != md_set_column_value_as_utf8(c, mdtAssembly_Culture, 1, &locale))
            return E_FAIL;
    }
    else
    {
        char const* locale = nullptr;
        if (1 != md_set_column_value_as_utf8(c, mdtAssembly_Culture, 1, &locale))
            return E_FAIL;
    }

    if (!md_cursor_to_token(c, pma))
        return E_FAIL;

    // TODO: Update ENC Log

    return S_OK;
}

HRESULT MetadataEmit::DefineAssemblyRef(
        void const  *pbPublicKeyOrToken,
        ULONG       cbPublicKeyOrToken,
        LPCWSTR     szName,
        ASSEMBLYMETADATA const *pMetaData,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwAssemblyRefFlags,
        mdAssemblyRef *pmdar)
{
    if (szName == nullptr || pMetaData == nullptr || pmdar == nullptr)
        return E_INVALIDARG;

    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;
    
    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_AssemblyRef, &c))
        return E_FAIL;
    
    const uint8_t* publicKey = (const uint8_t*)pbPublicKeyOrToken;
    if (publicKey != nullptr)
    {
        uint32_t publicKeyLength = cbPublicKeyOrToken;
        if (1 != md_set_column_value_as_blob(c, mdtAssemblyRef_PublicKeyOrToken, 1, &publicKey, &publicKeyLength))
            return E_FAIL;
    }
    else
    {
        uint32_t publicKeyLength = 0;
        if (1 != md_set_column_value_as_blob(c, mdtAssemblyRef_PublicKeyOrToken, 1, &publicKey, &publicKeyLength))
            return E_FAIL;
    }

    if (pbHashValue != nullptr)
    {
        uint8_t const* hashValue = (uint8_t const*)pbHashValue;
        uint32_t hashValueLength = cbHashValue;
        if (1 != md_set_column_value_as_blob(c, mdtAssemblyRef_HashValue, 1, &hashValue, &hashValueLength))
            return E_FAIL;
    }
    else
    {
        uint8_t const* hashValue = nullptr;
        uint32_t hashValueLength = 0;
        if (1 != md_set_column_value_as_blob(c, mdtAssemblyRef_HashValue, 1, &hashValue, &hashValueLength))
            return E_FAIL;
    }
    
    uint32_t assemblyFlags = PrepareForSaving(dwAssemblyRefFlags);
    if (1 != md_set_column_value_as_constant(c, mdtAssemblyRef_Flags, 1, &assemblyFlags))
        return E_FAIL;

    char const* name = cvt;
    if (1 != md_set_column_value_as_utf8(c, mdtAssemblyRef_Name, 1, &name))
        return E_FAIL;

    uint32_t majorVersion = pMetaData->usMajorVersion != std::numeric_limits<uint16_t>::max() ? pMetaData->usMajorVersion : 0;
    if (1 != md_set_column_value_as_constant(c, mdtAssemblyRef_MajorVersion, 1, &majorVersion))
        return E_FAIL;
    
    uint32_t minorVersion = pMetaData->usMinorVersion != std::numeric_limits<uint16_t>::max() ? pMetaData->usMinorVersion : 0;
    if (1 != md_set_column_value_as_constant(c, mdtAssemblyRef_MinorVersion, 1, &minorVersion))
        return E_FAIL;
    
    uint32_t buildNumber = pMetaData->usBuildNumber != std::numeric_limits<uint16_t>::max() ? pMetaData->usBuildNumber : 0;
    if (1 != md_set_column_value_as_constant(c, mdtAssemblyRef_BuildNumber, 1, &buildNumber))
        return E_FAIL;
    
    uint32_t revisionNumber = pMetaData->usRevisionNumber != std::numeric_limits<uint16_t>::max() ? pMetaData->usRevisionNumber : 0;
    if (1 != md_set_column_value_as_constant(c, mdtAssemblyRef_RevisionNumber, 1, &revisionNumber))
        return E_FAIL;
    
    if (pMetaData->szLocale != nullptr)
    {
        pal::StringConvert<WCHAR, char> cvtLocale(pMetaData->szLocale);
        if (!cvtLocale.Success())
            return E_INVALIDARG;

        char const* locale = cvtLocale;
        if (1 != md_set_column_value_as_utf8(c, mdtAssemblyRef_Culture, 1, &locale))
            return E_FAIL;
    }
    else
    {
        char const* locale = nullptr;
        if (1 != md_set_column_value_as_utf8(c, mdtAssemblyRef_Culture, 1, &locale))
            return E_FAIL;
    }

    if (!md_cursor_to_token(c, pmdar))
        return E_FAIL;

    // TODO: Update ENC Log

    return S_OK;
}

HRESULT MetadataEmit::DefineFile(
        LPCWSTR     szName,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwFileFlags,
        mdFile      *pmdf)
{
    
    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;

    md_added_row_t c;
    
    if (!md_append_row(MetaData(), mdtid_File, &c))
        return E_FAIL;

    char const* name = cvt;

    if (1 != md_set_column_value_as_utf8(c, mdtFile_Name, 1, &name))
        return E_FAIL;
    
    if (pbHashValue != nullptr)
    {
        uint8_t const* hashValue = (uint8_t const*)pbHashValue;
        uint32_t hashValueLength = cbHashValue;
        if (1 != md_set_column_value_as_blob(c, mdtFile_HashValue, 1, &hashValue, &hashValueLength))
            return E_FAIL;
    }
    else
    {
        uint8_t const* hashValue = nullptr;
        uint32_t hashValueLength = 0;
        if (1 != md_set_column_value_as_blob(c, mdtFile_HashValue, 1, &hashValue, &hashValueLength))
            return E_FAIL;
    }

    uint32_t fileFlags = dwFileFlags != std::numeric_limits<uint32_t>::max() ? dwFileFlags : 0;
    if (1 != md_set_column_value_as_constant(c, mdtFile_Flags, 1, &fileFlags))
        return E_FAIL;

    if (!md_cursor_to_token(c, pmdf))
        return E_FAIL;
    
    // TODO: Update ENC Log
    return S_OK;
}

HRESULT MetadataEmit::DefineExportedType(
        LPCWSTR     szName,
        mdToken     tkImplementation,
        mdTypeDef   tkTypeDef,
        DWORD       dwExportedTypeFlags,
        mdExportedType   *pmdct)
{
    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_ExportedType, &c))
        return E_FAIL;

    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;
    
    // TODO: check for duplicates
    char const* ns;
    char const* name;
    SplitTypeName(cvt, &ns, &name);
    if (1 != md_set_column_value_as_utf8(c, mdtExportedType_TypeNamespace, 1, &ns))
        return E_FAIL;
    if (1 != md_set_column_value_as_utf8(c, mdtExportedType_TypeName, 1, &name))
        return E_FAIL;
    
    if (!IsNilToken(tkImplementation))
    {
        if (1 != md_set_column_value_as_token(c, mdtExportedType_Implementation, 1, &tkImplementation))
            return E_FAIL;
    }
    else
    {
        // COMPAT: When the implementation column isn't defined, it is defaulted to the 0 value.
        // For the Implementation coded index, the nil File token is the 0 value;
        mdToken nilToken = mdFileNil;
        if (1 != md_set_column_value_as_token(c, mdtExportedType_Implementation, 1, &nilToken))
            return E_FAIL;
    }

    if (!IsNilToken(tkTypeDef))
    {
        if (1 != md_set_column_value_as_constant(c, mdtExportedType_TypeDefId, 1, &tkTypeDef))
            return E_FAIL;
    }
    else
    {
        mdToken nilToken = 0;
        if (1 != md_set_column_value_as_constant(c, mdtExportedType_TypeDefId, 1, &nilToken))
            return E_FAIL;
    }

    uint32_t exportedTypeFlags = dwExportedTypeFlags != std::numeric_limits<uint32_t>::max() ? dwExportedTypeFlags : 0;
    if (1 != md_set_column_value_as_constant(c, mdtExportedType_Flags, 1, &exportedTypeFlags))
        return E_FAIL;

    if (!md_cursor_to_token(c, pmdct))
        return E_FAIL;
    
    // TODO: Update ENC Log
    return S_OK;
}

HRESULT MetadataEmit::DefineManifestResource(
        LPCWSTR     szName,
        mdToken     tkImplementation,
        DWORD       dwOffset,
        DWORD       dwResourceFlags,
        mdManifestResource  *pmdmr)
{
    // TODO: check for duplicates
    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_ManifestResource, &c))
        return E_FAIL;
    
    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;
    
    char const* name = cvt;
    if (1 != md_set_column_value_as_utf8(c, mdtManifestResource_Name, 1, &name))
        return E_FAIL;
    
    if (!IsNilToken(tkImplementation))
    {
        if (1 != md_set_column_value_as_token(c, mdtManifestResource_Implementation, 1, &tkImplementation))
            return E_FAIL;
    }
    else
    {
        // COMPAT: When the implementation column isn't defined, it is defaulted to the 0 value.
        // For the Implementation coded index, the nil File token is the 0 value;
        mdToken nilToken = mdFileNil;
        if (1 != md_set_column_value_as_token(c, mdtManifestResource_Implementation, 1, &nilToken))
            return E_FAIL;
    }

    uint32_t offset = dwOffset != std::numeric_limits<uint32_t>::max() ? dwOffset : 0;
    if (1 != md_set_column_value_as_constant(c, mdtManifestResource_Offset, 1, &offset))
        return E_FAIL;

    uint32_t resourceFlags = dwResourceFlags != std::numeric_limits<uint32_t>::max() ? dwResourceFlags : 0;
    if (1 != md_set_column_value_as_constant(c, mdtManifestResource_Flags, 1, &resourceFlags))
        return E_FAIL;

    if (!md_cursor_to_token(c, pmdmr))
        return E_FAIL;
    
    // TODO: Update ENC Log
    return S_OK;
}

HRESULT MetadataEmit::SetAssemblyProps(
        mdAssembly  pma,
        void const  *pbPublicKey,
        ULONG       cbPublicKey,
        ULONG       ulHashAlgId,
        LPCWSTR     szName,
        ASSEMBLYMETADATA const *pMetaData,
        DWORD       dwAssemblyFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), pma, &c))
        return E_INVALIDARG;
    
    uint32_t assemblyFlags = dwAssemblyFlags;
    if (cbPublicKey != 0)
    {
        assemblyFlags |= afPublicKey;
    }

    const uint8_t* publicKey = (const uint8_t*)pbPublicKey;
    if (publicKey != nullptr)
    {
        uint32_t publicKeyLength = cbPublicKey;
        if (1 != md_set_column_value_as_blob(c, mdtAssembly_PublicKey, 1, &publicKey, &publicKeyLength))
            return E_FAIL;
    }

    if (1 != md_set_column_value_as_constant(c, mdtAssembly_Flags, 1, &assemblyFlags))
        return E_FAIL;
    
    if (szName != nullptr)
    {
        pal::StringConvert<WCHAR, char> cvt(szName);
        if (!cvt.Success())
            return E_INVALIDARG;
        
        char const* name = cvt;
        if (1 != md_set_column_value_as_utf8(c, mdtAssembly_Name, 1, &name))
            return E_FAIL;
    }

    if (ulHashAlgId != std::numeric_limits<uint32_t>::max())
    {
        uint32_t hashAlgId = ulHashAlgId;
        if (1 != md_set_column_value_as_constant(c, mdtAssembly_HashAlgId, 1, &hashAlgId))
            return E_FAIL;
    }

    if (pMetaData->usMajorVersion != std::numeric_limits<uint16_t>::max())
    {
        uint32_t majorVersion = pMetaData->usMajorVersion;
        if (1 != md_set_column_value_as_constant(c, mdtAssembly_MajorVersion, 1, &majorVersion))
            return E_FAIL;
    }

    if (pMetaData->usMinorVersion != std::numeric_limits<uint16_t>::max())
    {
        uint32_t minorVersion = pMetaData->usMinorVersion;
        if (1 != md_set_column_value_as_constant(c, mdtAssembly_MinorVersion, 1, &minorVersion))
            return E_FAIL;
    }

    if (pMetaData->usBuildNumber != std::numeric_limits<uint16_t>::max())
    {
        uint32_t buildNumber = pMetaData->usBuildNumber;
        if (1 != md_set_column_value_as_constant(c, mdtAssembly_BuildNumber, 1, &buildNumber))
            return E_FAIL;
    }

    if (pMetaData->usRevisionNumber != std::numeric_limits<uint16_t>::max())
    {
        uint32_t revisionNumber = pMetaData->usRevisionNumber;
        if (1 != md_set_column_value_as_constant(c, mdtAssembly_RevisionNumber, 1, &revisionNumber))
            return E_FAIL;
    }

    if (pMetaData->szLocale != nullptr)
    {
        pal::StringConvert<WCHAR, char> cvtLocale(pMetaData->szLocale);
        if (!cvtLocale.Success())
            return E_INVALIDARG;

        char const* locale = cvtLocale;
        if (1 != md_set_column_value_as_utf8(c, mdtAssembly_Culture, 1, &locale))
            return E_FAIL;
    }

    // TODO: Update ENC Log

    return S_OK;
}

HRESULT MetadataEmit::SetAssemblyRefProps(
        mdAssemblyRef ar,
        void const  *pbPublicKeyOrToken,
        ULONG       cbPublicKeyOrToken,
        LPCWSTR     szName,
        ASSEMBLYMETADATA const *pMetaData,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwAssemblyRefFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), ar, &c))
        return E_INVALIDARG;
    
    uint32_t assemblyFlags = dwAssemblyRefFlags;
    if (cbPublicKeyOrToken != 0)
    {
        assemblyFlags |= afPublicKey;
    }

    const uint8_t* publicKey = (const uint8_t*)pbPublicKeyOrToken;
    if (publicKey != nullptr)
    {
        uint32_t publicKeyLength = cbPublicKeyOrToken;
        if (1 != md_set_column_value_as_blob(c, mdtAssemblyRef_PublicKeyOrToken, 1, &publicKey, &publicKeyLength))
            return E_FAIL;
    }

    if (pbHashValue != nullptr)
    {
        uint8_t const* hashValue = (uint8_t const*)pbHashValue;
        uint32_t hashValueLength = cbHashValue;
        if (1 != md_set_column_value_as_blob(c, mdtAssemblyRef_HashValue, 1, &hashValue, &hashValueLength))
            return E_FAIL;
    }

    if (1 != md_set_column_value_as_constant(c, mdtAssemblyRef_Flags, 1, &assemblyFlags))
        return E_FAIL;
    
    if (szName != nullptr)
    {
        pal::StringConvert<WCHAR, char> cvt(szName);
        if (!cvt.Success())
            return E_INVALIDARG;
        
        char const* name = cvt;
        if (1 != md_set_column_value_as_utf8(c, mdtAssemblyRef_Name, 1, &name))
            return E_FAIL;
    }

    if (pMetaData->usMajorVersion != std::numeric_limits<uint16_t>::max())
    {
        uint32_t majorVersion = pMetaData->usMajorVersion;
        if (1 != md_set_column_value_as_constant(c, mdtAssemblyRef_MajorVersion, 1, &majorVersion))
            return E_FAIL;
    }

    if (pMetaData->usMinorVersion != std::numeric_limits<uint16_t>::max())
    {
        uint32_t minorVersion = pMetaData->usMinorVersion;
        if (1 != md_set_column_value_as_constant(c, mdtAssemblyRef_MinorVersion, 1, &minorVersion))
            return E_FAIL;
    }

    if (pMetaData->usBuildNumber != std::numeric_limits<uint16_t>::max())
    {
        uint32_t buildNumber = pMetaData->usBuildNumber;
        if (1 != md_set_column_value_as_constant(c, mdtAssemblyRef_BuildNumber, 1, &buildNumber))
            return E_FAIL;
    }

    if (pMetaData->usRevisionNumber != std::numeric_limits<uint16_t>::max())
    {
        uint32_t revisionNumber = pMetaData->usRevisionNumber;
        if (1 != md_set_column_value_as_constant(c, mdtAssemblyRef_RevisionNumber, 1, &revisionNumber))
            return E_FAIL;
    }

    if (pMetaData->szLocale != nullptr)
    {
        pal::StringConvert<WCHAR, char> cvtLocale(pMetaData->szLocale);
        if (!cvtLocale.Success())
            return E_INVALIDARG;

        char const* locale = cvtLocale;
        if (1 != md_set_column_value_as_utf8(c, mdtAssemblyRef_Culture, 1, &locale))
            return E_FAIL;
    }

    // TODO: Update ENC Log

    return S_OK;
}

HRESULT MetadataEmit::SetFileProps(
        mdFile      file,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwFileFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), file, &c))
        return E_INVALIDARG;

    if (pbHashValue != nullptr)
    {
        uint8_t const* hashValue = (uint8_t const*)pbHashValue;
        uint32_t hashValueLength = cbHashValue;
        if (1 != md_set_column_value_as_blob(c, mdtFile_HashValue, 1, &hashValue, &hashValueLength))
            return E_FAIL;
    }

    if (dwFileFlags != std::numeric_limits<uint32_t>::max())
    {
        uint32_t fileFlags = dwFileFlags;
        if (1 != md_set_column_value_as_constant(c, mdtFile_Flags, 1, &fileFlags))
            return E_FAIL;
    }

    // TODO: Update ENC Log

    return S_OK;
}

HRESULT MetadataEmit::SetExportedTypeProps(
        mdExportedType   ct,
        mdToken     tkImplementation,
        mdTypeDef   tkTypeDef,
        DWORD       dwExportedTypeFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), ct, &c))
        return E_INVALIDARG;
    
    if (!IsNilToken(tkImplementation))
    {
        if (1 != md_set_column_value_as_token(c, mdtExportedType_Implementation, 1, &tkImplementation))
            return E_FAIL;
    }

    if (!IsNilToken(tkTypeDef))
    {
        if (1 != md_set_column_value_as_token(c, mdtExportedType_TypeDefId, 1, &tkTypeDef))
            return E_FAIL;
    }

    if (dwExportedTypeFlags != std::numeric_limits<uint32_t>::max())
    {
        uint32_t exportedTypeFlags = dwExportedTypeFlags;
        if (1 != md_set_column_value_as_constant(c, mdtExportedType_Flags, 1, &exportedTypeFlags))
            return E_FAIL;
    }

    // TODO: Update ENC Log

    return S_OK;
}

HRESULT MetadataEmit::SetManifestResourceProps(
        mdManifestResource  mr,
        mdToken     tkImplementation,
        DWORD       dwOffset,
        DWORD       dwResourceFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), mr, &c))
        return E_INVALIDARG;
    
    if (!IsNilToken(tkImplementation))
    {
        if (1 != md_set_column_value_as_token(c, mdtManifestResource_Implementation, 1, &tkImplementation))
            return E_FAIL;
    }

    if (dwOffset != std::numeric_limits<uint32_t>::max())
    {
        uint32_t offset = dwOffset;
        if (1 != md_set_column_value_as_constant(c, mdtManifestResource_Offset, 1, &offset))
            return E_FAIL;
    }

    if (dwResourceFlags != std::numeric_limits<uint32_t>::max())
    {
        uint32_t resourceFlags = dwResourceFlags;
        if (1 != md_set_column_value_as_constant(c, mdtManifestResource_Flags, 1, &resourceFlags))
            return E_FAIL;
    }

    // TODO: Update ENC Log

    return S_OK;
}
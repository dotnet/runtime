#include <cassert>

#include "pal.hpp"
#include "impl.hpp"

#define MD_MODULE_TOKEN TokenFromRid(1, mdtModule)
#define MD_GLOBAL_PARENT_TOKEN TokenFromRid(1, mdtTypeDef)

#define ToHCORENUMImpl(hcorenum) (reinterpret_cast<HCORENUMImpl*>(hcorenum))

#define RETURN_IF_FAILED(exp) \
{ \
    hr = (exp); \
    if (FAILED(hr)) \
    { \
        return hr; \
    } \
}

void STDMETHODCALLTYPE MetadataImportRO::CloseEnum(HCORENUM hEnum)
{
    HCORENUMImpl* impl = ToHCORENUMImpl(hEnum);
    if (impl != nullptr)
        HCORENUMImpl::Destroy(impl);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::CountEnum(HCORENUM hEnum, ULONG* pulCount)
{
    if (pulCount == nullptr)
        return E_INVALIDARG;

    HCORENUMImpl* enumImpl = ToHCORENUMImpl(hEnum);
    *pulCount = enumImpl->Count();
    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::ResetEnum(HCORENUM hEnum, ULONG ulPos)
{
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(hEnum);
    if (enumImpl == nullptr)
        return S_OK;
    return enumImpl->Reset(ulPos);
}

namespace
{
    HRESULT CreateEnumTokens(
        mdhandle_t mdhandle,
        mdtable_id_t mdtid,
        HCORENUMImpl** pEnumImpl)
    {
        HRESULT hr;
        mdcursor_t cursor;
        uint32_t rows;
        if (!md_create_cursor(mdhandle, mdtid, &cursor, &rows))
            return E_INVALIDARG;

        HCORENUMImpl* enumImpl;
        RETURN_IF_FAILED(HCORENUMImpl::CreateTableEnum(1, &enumImpl));
        HCORENUMImpl::InitTableEnum(*enumImpl, cursor, rows);
        *pEnumImpl = enumImpl;
        return S_OK;
    }

    struct TokenRangeFilter
    {
        col_index_t FilterColumn;
        LPCWSTR Value;
    };

    HRESULT CreateEnumTokenRange(
        mdhandle_t mdhandle,
        mdToken token,
        col_index_t column,
        _In_opt_ TokenRangeFilter const* filter,
        HCORENUMImpl** pEnumImpl)
    {
        HRESULT hr;
        mdcursor_t cursor;
        if (!md_token_to_cursor(mdhandle, token, &cursor))
            return CLDB_E_INDEX_NOTFOUND;

        mdcursor_t begin;
        uint32_t count;
        if (!md_get_column_value_as_range(cursor, column, &begin, &count))
            return CLDB_E_FILE_CORRUPT;

        HCORENUMImpl* enumImpl;
        if (filter == nullptr || filter->Value == nullptr)
        {
            RETURN_IF_FAILED(HCORENUMImpl::CreateTableEnum(1, &enumImpl));
            HCORENUMImpl::InitTableEnum(*enumImpl, begin, count);
        }
        else
        {
            assert(filter != nullptr && filter->Value != nullptr);
            pal::StringConvert<WCHAR, char> cvt{ filter->Value };
            if (!cvt.Success())
                return E_INVALIDARG;

            char const* toMatch;
            mdToken matchedTk;
            RETURN_IF_FAILED(HCORENUMImpl::CreateDynamicEnum(&enumImpl));

            HCORENUMImpl_ptr cleanup{ enumImpl };

            mdcursor_t curr = begin;
            for (uint32_t i = 0; i < count; ++i)
            {
                if (1 != md_get_column_value_as_utf8(curr, filter->FilterColumn, 1, &toMatch))
                    return CLDB_E_FILE_CORRUPT;

                if (0 == ::strcmp(toMatch, cvt))
                {
                    (void)md_cursor_to_token(curr, &matchedTk);
                    RETURN_IF_FAILED(HCORENUMImpl::AddToDynamicEnum(*enumImpl, matchedTk));
                }
                (void)md_cursor_next(&curr);
            }

            enumImpl = cleanup.release();
        }

        *pEnumImpl = enumImpl;
        return S_OK;
    }

    HRESULT CopyToStringOutput(
        LPCWSTR str,
        uint32_t strCharCount,
        _Out_writes_to_opt_(cchBuffer, *pchBuffer)
        LPWSTR  szBuffer,
        ULONG   cchBuffer,
        ULONG* pchBuffer) noexcept
    {
        assert(str != nullptr && pchBuffer != nullptr);

        *pchBuffer = strCharCount;
        if (cchBuffer > 0 && strCharCount > 0)
        {
            uint32_t toCopyWChar = cchBuffer < strCharCount ? cchBuffer : strCharCount;
            assert(szBuffer != nullptr);
            ::memcpy(szBuffer, str, toCopyWChar * sizeof(WCHAR));
            if (cchBuffer < strCharCount)
            {
                ::memset(&szBuffer[cchBuffer - 1], 0, sizeof(WCHAR)); // Ensure null terminator
                return CLDB_S_TRUNCATION;
            }
        }

        return S_OK;
    }

    HRESULT ConstructTypeName(
        char const* nspace,
        char const* name,
        malloc_ptr& mem)
    {
        char* buffer;
        size_t nspaceLen = nspace == nullptr ? 0 : ::strlen(nspace);
        size_t nameLen = name == nullptr ? 0 : ::strlen(name);
        size_t bufferLength = nspaceLen + nameLen + 1 + 1; // +1 for type delim and +1 for null.

        mem.reset(::malloc(bufferLength * sizeof(*buffer)));
        if (mem == nullptr)
            return E_OUTOFMEMORY;

        buffer = (char*)mem.get();
        buffer[0] = '\0';

        if (nspaceLen > 0)
        {
            ::strcat_s(buffer, bufferLength, nspace);
            ::strcat_s(buffer, bufferLength, ".");
        }

        if (nameLen > 0)
            ::strcat_s(buffer, bufferLength, name);

        return S_OK;
    }
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumTypeDefs(
    HCORENUM* phEnum,
    mdTypeDef rTypeDefs[],
    ULONG cMax,
    ULONG* pcTypeDefs)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        mdcursor_t cursor;
        uint32_t rows;
        if (!md_create_cursor(_md_ptr.get(), mdtid_TypeDef, &cursor, &rows))
            return E_INVALIDARG;

        // From ECMA-335, section II.22.37:
        //  "The first row of the TypeDef table represents the pseudo class that acts as parent for functions
        //  and variables defined at module scope."
        // Based on the above we always skip the first row.
        rows--;
        (void)md_cursor_next(&cursor);

        RETURN_IF_FAILED(HCORENUMImpl::CreateTableEnum(1, &enumImpl));
        HCORENUMImpl::InitTableEnum(*enumImpl, cursor, rows);
        *phEnum = enumImpl;
    }

    return enumImpl->ReadTokens(rTypeDefs, cMax, pcTypeDefs);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumInterfaceImpls(
    HCORENUM* phEnum,
    mdTypeDef td,
    mdInterfaceImpl rImpls[],
    ULONG cMax,
    ULONG* pcImpls)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        mdcursor_t cursor;
        uint32_t rows;
        if (!md_create_cursor(_md_ptr.get(), mdtid_InterfaceImpl, &cursor, &rows))
            return E_INVALIDARG;

        uint32_t id = RidFromToken(td);
        if (!md_find_range_from_cursor(cursor, mdtInterfaceImpl_Class, id, &cursor, &rows))
            return CLDB_E_FILE_CORRUPT;

        RETURN_IF_FAILED(HCORENUMImpl::CreateTableEnum(1, &enumImpl));
        HCORENUMImpl::InitTableEnum(*enumImpl, cursor, rows);
        *phEnum = enumImpl;
    }

    return enumImpl->ReadTokens(rImpls, cMax, pcImpls);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumTypeRefs(
    HCORENUM* phEnum,
    mdTypeRef rTypeRefs[],
    ULONG cMax,
    ULONG* pcTypeRefs)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        RETURN_IF_FAILED(CreateEnumTokens(_md_ptr.get(), mdtid_TypeRef, &enumImpl));
        *phEnum = enumImpl;
    }
    return enumImpl->ReadTokens(rTypeRefs, cMax, pcTypeRefs);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::FindTypeDefByName(
    LPCWSTR     szTypeDef,
    mdToken     tkEnclosingClass,
    mdTypeDef* ptd)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetScopeProps(
    _Out_writes_to_opt_(cchName, *pchName)
    LPWSTR      szName,
    ULONG       cchName,
    ULONG* pchName,
    GUID* pmvid)
{
    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), MD_MODULE_TOKEN, &cursor))
        return CLDB_E_INDEX_NOTFOUND;

    char const* name;
    if (1 != md_get_column_value_as_utf8(cursor, mdtModule_Name, 1, &name)
        || 1 != md_get_column_value_as_guid(cursor, mdtModule_Mvid, 1, pmvid))
    {
        return CLDB_E_FILE_CORRUPT;
    }

    uint32_t written;
    pal::StringConvert<char, WCHAR> cvt{ name };
    if (!cvt.Success()
        || !cvt.CopyTo(szName, cchName, &written))
    {
        return E_INVALIDARG;
    }

    *pchName = cvt.Length();
    return (written < cvt.Length())
        ? CLDB_S_TRUNCATION
        : S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetModuleFromScope(
    mdModule* pmd)
{
    if (pmd == nullptr)
        return E_POINTER;

    *pmd = TokenFromRid(1, mdtModule);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetTypeDefProps(
    mdTypeDef   td,
    _Out_writes_to_opt_(cchTypeDef, *pchTypeDef)
    LPWSTR      szTypeDef,
    ULONG       cchTypeDef,
    ULONG* pchTypeDef,
    DWORD* pdwTypeDefFlags,
    mdToken* ptkExtends)
{
    if (TypeFromToken(td) != mdtTypeDef)
        return E_INVALIDARG;

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), td, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    if (1 != md_get_column_value_as_constant(cursor, mdtTypeDef_Flags, 1, (uint32_t*)pdwTypeDefFlags)
        || 1 != md_get_column_value_as_token(cursor, mdtTypeDef_Extends, 1, ptkExtends))
    {
        return CLDB_E_FILE_CORRUPT;
    }

    if (*ptkExtends == mdTypeDefNil)
        *ptkExtends = mdTypeRefNil;

    char const* name;
    char const* nspace;
    if (1 != md_get_column_value_as_utf8(cursor, mdtTypeDef_TypeName, 1, &name)
        || 1 != md_get_column_value_as_utf8(cursor, mdtTypeDef_TypeNamespace, 1, &nspace))
    {
        return CLDB_E_FILE_CORRUPT;
    }

    HRESULT hr;
    malloc_ptr mem;
    RETURN_IF_FAILED(ConstructTypeName(nspace, name, mem));

    uint32_t written;
    pal::StringConvert<char, WCHAR> cvt{ (char const*)mem.get() };
    if (!cvt.Success()
        || !cvt.CopyTo(szTypeDef, cchTypeDef, &written))
    {
        return E_INVALIDARG;
    }

    *pchTypeDef = cvt.Length();
    return (written < cvt.Length())
        ? CLDB_S_TRUNCATION
        : S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetInterfaceImplProps(
    mdInterfaceImpl iiImpl,
    mdTypeDef* pClass,
    mdToken* ptkIface)
{
    if (TypeFromToken(iiImpl) != mdtInterfaceImpl)
        return E_INVALIDARG;

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), iiImpl, &cursor))
        return CLDB_E_INDEX_NOTFOUND;

    if (1 != md_get_column_value_as_token(cursor, mdtInterfaceImpl_Class, 1, pClass)
        || 1 != md_get_column_value_as_token(cursor, mdtInterfaceImpl_Interface, 1, ptkIface))
    {
        return CLDB_E_FILE_CORRUPT;
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetTypeRefProps(
    mdTypeRef   tr,
    mdToken* ptkResolutionScope,
    _Out_writes_to_opt_(cchName, *pchName)
    LPWSTR      szName,
    ULONG       cchName,
    ULONG* pchName)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::ResolveTypeRef(mdTypeRef tr, REFIID riid, IUnknown** ppIScope, mdTypeDef* ptd)
{
    // Requires VM knowledge
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumMembers(
    HCORENUM* phEnum,
    mdTypeDef   cl,
    mdToken     rMembers[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        if (TypeFromToken(cl) != mdtTypeDef)
            return E_INVALIDARG;

        mdcursor_t cursor;
        if (!md_token_to_cursor(_md_ptr.get(), cl, &cursor))
            return CLDB_E_INDEX_NOTFOUND;

        mdcursor_t methodList;
        uint32_t methodListCount;
        mdcursor_t fieldList;
        uint32_t fieldListCount;
        if (!md_get_column_value_as_range(cursor, mdtTypeDef_FieldList, &fieldList, &fieldListCount)
            || !md_get_column_value_as_range(cursor, mdtTypeDef_MethodList, &methodList, &methodListCount))
        {
            return CLDB_E_FILE_CORRUPT;
        }

        RETURN_IF_FAILED(HCORENUMImpl::CreateTableEnum(2, &enumImpl));
        HCORENUMImpl::InitTableEnum(enumImpl[0], methodList, methodListCount);
        HCORENUMImpl::InitTableEnum(enumImpl[1], fieldList, fieldListCount);
        *phEnum = enumImpl;
    }

    return enumImpl->ReadTokens(rMembers, cMax, pcTokens);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumMembersWithName(
    HCORENUM* phEnum,
    mdTypeDef   cl,
    LPCWSTR     szName,
    mdToken     rMembers[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        // If name is null, defer to the EnumMembers() API.
        if (szName == nullptr)
            return EnumMembers(phEnum, cl, rMembers, cMax, pcTokens);

        if (TypeFromToken(cl) != mdtTypeDef)
            return E_INVALIDARG;

        mdcursor_t cursor;
        if (!md_token_to_cursor(_md_ptr.get(), cl, &cursor))
            return CLDB_E_INDEX_NOTFOUND;

        mdcursor_t methodList;
        uint32_t methodListCount;
        mdcursor_t fieldList;
        uint32_t fieldListCount;
        if (!md_get_column_value_as_range(cursor, mdtTypeDef_FieldList, &fieldList, &fieldListCount)
            || !md_get_column_value_as_range(cursor, mdtTypeDef_MethodList, &methodList, &methodListCount))
        {
            return CLDB_E_FILE_CORRUPT;
        }

        assert(szName != nullptr);
        pal::StringConvert<WCHAR, char> cvt{ szName };
        if (!cvt.Success())
            return E_INVALIDARG;

        char const* toMatch;
        mdToken matchedTk;
        RETURN_IF_FAILED(HCORENUMImpl::CreateDynamicEnum(&enumImpl));

        HCORENUMImpl_ptr cleanup{ enumImpl };

        // Iterate the Type's methods
        for (uint32_t i = 0; i < methodListCount; ++i)
        {
            if (1 != md_get_column_value_as_utf8(methodList, mdtMethodDef_Name, 1, &toMatch))
                return CLDB_E_FILE_CORRUPT;

            if (0 == ::strcmp(toMatch, cvt))
            {
                (void)md_cursor_to_token(methodList, &matchedTk);
                RETURN_IF_FAILED(HCORENUMImpl::AddToDynamicEnum(*enumImpl, matchedTk));
            }
            (void)md_cursor_next(&methodList);
        }

        // Iterate the Type's fields
        for (uint32_t i = 0; i < fieldListCount; ++i)
        {
            if (1 != md_get_column_value_as_utf8(fieldList, mdtField_Name, 1, &toMatch))
                return CLDB_E_FILE_CORRUPT;

            if (0 == ::strcmp(toMatch, cvt))
            {
                (void)md_cursor_to_token(fieldList, &matchedTk);
                RETURN_IF_FAILED(HCORENUMImpl::AddToDynamicEnum(*enumImpl, matchedTk));
            }
            (void)md_cursor_next(&fieldList);
        }

        *phEnum = cleanup.release();
    }

    return enumImpl->ReadTokens(rMembers, cMax, pcTokens);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumMethods(
    HCORENUM* phEnum,
    mdTypeDef   cl,
    mdMethodDef rMethods[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    return EnumMethodsWithName(phEnum, cl, nullptr, rMethods, cMax, pcTokens);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumMethodsWithName(
    HCORENUM* phEnum,
    mdTypeDef   cl,
    LPCWSTR     szName,
    mdMethodDef rMethods[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        if (TypeFromToken(cl) != mdtTypeDef)
            return E_INVALIDARG;

        TokenRangeFilter filter{ mdtMethodDef_Name, szName };
        RETURN_IF_FAILED(CreateEnumTokenRange(_md_ptr.get(), cl, mdtTypeDef_MethodList, &filter, &enumImpl));
        *phEnum = enumImpl;
    }
    return enumImpl->ReadTokens(rMethods, cMax, pcTokens);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumFields(
    HCORENUM* phEnum,
    mdTypeDef   cl,
    mdFieldDef  rFields[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    return EnumFieldsWithName(phEnum, cl, nullptr, rFields, cMax, pcTokens);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumFieldsWithName(
    HCORENUM* phEnum,
    mdTypeDef   cl,
    LPCWSTR     szName,
    mdFieldDef  rFields[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        if (TypeFromToken(cl) != mdtTypeDef)
            return E_INVALIDARG;

        TokenRangeFilter filter{ mdtField_Name, szName };
        RETURN_IF_FAILED(CreateEnumTokenRange(_md_ptr.get(), cl, mdtTypeDef_FieldList, &filter, &enumImpl));
        *phEnum = enumImpl;
    }
    return enumImpl->ReadTokens(rFields, cMax, pcTokens);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumParams(
    HCORENUM* phEnum,
    mdMethodDef mb,
    mdParamDef  rParams[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        if (TypeFromToken(mb) != mdtMethodDef)
            return E_INVALIDARG;

        RETURN_IF_FAILED(CreateEnumTokenRange(_md_ptr.get(), mb, mdtMethodDef_ParamList, nullptr, &enumImpl));
        *phEnum = enumImpl;
    }
    return enumImpl->ReadTokens(rParams, cMax, pcTokens);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumMemberRefs(
    HCORENUM* phEnum,
    mdToken     tkParent,
    mdMemberRef rMemberRefs[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumMethodImpls(
    HCORENUM* phEnum,
    mdTypeDef   td,
    mdToken     rMethodBody[],
    mdToken     rMethodDecl[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumPermissionSets(
    HCORENUM* phEnum,
    mdToken     tk,
    DWORD       dwActions,
    mdPermission rPermission[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        CorTokenType type = (CorTokenType)TypeFromToken(tk);
        if (type != mdtTypeDef
            && type != mdtMethodDef
            && type != mdtAssembly)
        {
            *pcTokens = 0;
            return S_FALSE;
        }

        mdcursor_t cursor;
        uint32_t count;
        if (!md_create_cursor(_md_ptr.get(), mdtid_DeclSecurity, &cursor, &count))
            return CLDB_E_FILE_CORRUPT;

        if (!IsNilToken(tk))
        {
            if (!md_find_range_from_cursor(cursor, mdtDeclSecurity_Parent, tk, &cursor, &count))
                return CLDB_E_RECORD_NOTFOUND;
        }

        if (IsDclActionNil(dwActions))
        {
            RETURN_IF_FAILED(HCORENUMImpl::CreateTableEnum(1, &enumImpl));
            HCORENUMImpl::InitTableEnum(*enumImpl, cursor, count);
        }
        else
        {
            uint32_t action;
            mdToken toAdd;
            RETURN_IF_FAILED(HCORENUMImpl::CreateDynamicEnum(&enumImpl));

            HCORENUMImpl_ptr cleanup{ enumImpl };
            for (uint32_t i = 0; i < count; ++i)
            {
                if (1 == md_get_column_value_as_constant(cursor, mdtDeclSecurity_Action, 1, &action)
                    && action == dwActions)
                {
                    // If we read from the cursor it must be valid.
                    (void)md_cursor_to_token(cursor, &toAdd);
                    RETURN_IF_FAILED(HCORENUMImpl::AddToDynamicEnum(*enumImpl, toAdd));
                }

                if (!md_cursor_next(&cursor))
                    return CLDB_E_RECORD_NOTFOUND;
            }
            enumImpl = cleanup.release();
        }
        *phEnum = enumImpl;
    }

    return enumImpl->ReadTokens(rPermission, cMax, pcTokens);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::FindMember(
    mdTypeDef   td,
    LPCWSTR     szName,
    PCCOR_SIGNATURE pvSigBlob,
    ULONG       cbSigBlob,
    mdToken* pmb)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::FindMethod(
    mdTypeDef   td,
    LPCWSTR     szName,
    PCCOR_SIGNATURE pvSigBlob,
    ULONG       cbSigBlob,
    mdMethodDef* pmb)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::FindField(
    mdTypeDef   td,
    LPCWSTR     szName,
    PCCOR_SIGNATURE pvSigBlob,
    ULONG       cbSigBlob,
    mdFieldDef* pmb)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::FindMemberRef(
    mdTypeRef   td,
    LPCWSTR     szName,
    PCCOR_SIGNATURE pvSigBlob,
    ULONG       cbSigBlob,
    mdMemberRef* pmr)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetMethodProps(
    mdMethodDef mb,
    mdTypeDef* pClass,
    _Out_writes_to_opt_(cchMethod, *pchMethod)
    LPWSTR      szMethod,
    ULONG       cchMethod,
    ULONG* pchMethod,
    DWORD* pdwAttr,
    PCCOR_SIGNATURE* ppvSigBlob,
    ULONG* pcbSigBlob,
    ULONG* pulCodeRVA,
    DWORD* pdwImplFlags)
{
    mdcursor_t cursor;
    if (TypeFromToken(mb) != mdtMethodDef)
        return E_INVALIDARG;

    if (!md_token_to_cursor(_md_ptr.get(), mb, &cursor))
        return CLDB_E_INDEX_NOTFOUND;

    uint32_t attrs;
    uint32_t rva;
    uint32_t implFlags;
    if (1 != md_get_column_value_as_constant(cursor, mdtMethodDef_Flags, 1, &attrs)
        || 1 != md_get_column_value_as_constant(cursor, mdtMethodDef_Rva, 1, &rva)
        || 1 != md_get_column_value_as_constant(cursor, mdtMethodDef_ImplFlags, 1, &implFlags))
    {
        return CLDB_E_FILE_CORRUPT;
    }

    *pdwAttr = attrs;
    *pulCodeRVA = rva;
    *pdwImplFlags = implFlags;

    uint8_t const* sig;
    uint32_t sigLen;
    if (1 != md_get_column_value_as_blob(cursor, mdtMethodDef_Signature, 1, &sig, &sigLen))
        return CLDB_E_FILE_CORRUPT;

    *ppvSigBlob = sig;
    *pcbSigBlob = sigLen;

    char const* name;
    if (1 != md_get_column_value_as_utf8(cursor, mdtMethodDef_Name, 1, &name))
        return CLDB_E_FILE_CORRUPT;

    uint32_t written;
    pal::StringConvert<char, WCHAR> cvt{ name };
    if (!cvt.Success()
        || !cvt.CopyTo(szMethod, cchMethod, &written))
    {
        return E_INVALIDARG;
    }

    *pchMethod = written;
    return (written < cvt.Length())
        ? CLDB_S_TRUNCATION
        : S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetMemberRefProps(
    mdMemberRef mr,
    mdToken* ptk,
    _Out_writes_to_opt_(cchMember, *pchMember)
    LPWSTR      szMember,
    ULONG       cchMember,
    ULONG* pchMember,
    PCCOR_SIGNATURE* ppvSigBlob,
    ULONG* pbSig)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumProperties(
    HCORENUM* phEnum,
    mdTypeDef   td,
    mdProperty  rProperties[],
    ULONG       cMax,
    ULONG* pcProperties)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        if (TypeFromToken(td) != mdtTypeDef)
            return E_INVALIDARG;

        // Create cursor for PropertyMap table
        mdcursor_t propertyMap;
        uint32_t propertyMapCount;
        if (!md_create_cursor(_md_ptr.get(), mdtid_PropertyMap, &propertyMap, &propertyMapCount))
            return E_INVALIDARG;

        // Find the entry in the PropertyMap table and then
        // resolve the column to the range in the Property table.
        mdcursor_t typedefPropMap;
        mdcursor_t propertyList;
        uint32_t propertyListCount;
        if (!md_find_row_from_cursor(propertyMap, mdtPropertyMap_Parent, RidFromToken(td), &typedefPropMap)
            || !md_get_column_value_as_range(typedefPropMap, mdtPropertyMap_PropertyList, &propertyList, &propertyListCount))
        {
            return CLDB_E_FILE_CORRUPT;
        }

        RETURN_IF_FAILED(HCORENUMImpl::CreateTableEnum(1, &enumImpl));
        HCORENUMImpl::InitTableEnum(*enumImpl, propertyList, propertyListCount);
        *phEnum = enumImpl;
    }

    return enumImpl->ReadTokens(rProperties, cMax, pcProperties);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumEvents(
    HCORENUM* phEnum,
    mdTypeDef   td,
    mdEvent     rEvents[],
    ULONG       cMax,
    ULONG* pcEvents)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        if (TypeFromToken(td) != mdtTypeDef)
            return E_INVALIDARG;

        // Create cursor for EventMap table
        mdcursor_t eventMap;
        uint32_t eventMapCount;
        if (!md_create_cursor(_md_ptr.get(), mdtid_EventMap, &eventMap, &eventMapCount))
            return CLDB_E_FILE_CORRUPT;

        // Find the entry in the EventMap table and then
        // resolve the column to the range in the Event table.
        mdcursor_t typedefEventMap;
        mdcursor_t eventList;
        uint32_t eventListCount;
        if (!md_find_row_from_cursor(eventMap, mdtEventMap_Parent, RidFromToken(td), &typedefEventMap)
            || !md_get_column_value_as_range(typedefEventMap, mdtEventMap_EventList, &eventList, &eventListCount))
        {
            return CLDB_E_FILE_CORRUPT;
        }

        RETURN_IF_FAILED(HCORENUMImpl::CreateTableEnum(1, &enumImpl));
        HCORENUMImpl::InitTableEnum(*enumImpl, eventList, eventListCount);
        *phEnum = enumImpl;
    }

    return enumImpl->ReadTokens(rEvents, cMax, pcEvents);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetEventProps(
    mdEvent     ev,
    mdTypeDef* pClass,
    LPCWSTR     szEvent,
    ULONG       cchEvent,
    ULONG* pchEvent,
    DWORD* pdwEventFlags,
    mdToken* ptkEventType,
    mdMethodDef* pmdAddOn,
    mdMethodDef* pmdRemoveOn,
    mdMethodDef* pmdFire,
    mdMethodDef rmdOtherMethod[],
    ULONG       cMax,
    ULONG* pcOtherMethod)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumMethodSemantics(
    HCORENUM* phEnum,
    mdMethodDef mb,
    mdToken     rEventProp[],
    ULONG       cMax,
    ULONG* pcEventProp)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        if (TypeFromToken(mb) != mdtMethodDef)
            return E_INVALIDARG;

        mdcursor_t cursor;
        uint32_t count;
        if (!md_create_cursor(_md_ptr.get(), mdtid_MethodSemantics, &cursor, &count))
            return CLDB_E_FILE_CORRUPT;

        RETURN_IF_FAILED(HCORENUMImpl::CreateDynamicEnum(&enumImpl));

        HCORENUMImpl_ptr cleanup{ enumImpl };

        // Read in for matching in bulk
        mdToken toMatch[64];
        mdToken matchedTk;
        uint32_t i = 0;
        while (i < count)
        {
            int32_t read = md_get_column_value_as_token(cursor, mdtMethodSemantics_Method, ARRAYSIZE(toMatch), toMatch);
            if (read == 0)
                break;

            assert(read >= 0);
            for (int32_t j = 0; j < read; ++j)
            {
                if (toMatch[j] == mb)
                {
                    if (1 != md_get_column_value_as_token(cursor, mdtMethodSemantics_Association, 1, &matchedTk))
                        return CLDB_E_FILE_CORRUPT;
                    RETURN_IF_FAILED(HCORENUMImpl::AddToDynamicEnum(*enumImpl, matchedTk));
                }
                (void)md_cursor_next(&cursor);
            }
            i += read;
        }
        *phEnum = cleanup.release();
    }

    return enumImpl->ReadTokens(rEventProp, cMax, pcEventProp);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetMethodSemantics(
    mdMethodDef mb,
    mdToken     tkEventProp,
    DWORD* pdwSemanticsFlags)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetClassLayout(
    mdTypeDef   td,
    DWORD* pdwPackSize,
    COR_FIELD_OFFSET rFieldOffset[],
    ULONG       cMax,
    ULONG* pcFieldOffset,
    ULONG* pulClassSize)
{
    if (TypeFromToken(td) != mdtTypeDef)
        return E_INVALIDARG;

    mdcursor_t begin;
    uint32_t unused;
    mdcursor_t entry;
    bool foundLayout = false;
    if (!md_create_cursor(_md_ptr.get(), mdtid_ClassLayout, &begin, &unused)
        || !md_find_row_from_cursor(begin, mdtClassLayout_Parent, RidFromToken(td), &entry))
    {
        *pcFieldOffset = 0;
        *pulClassSize = 0;
    }
    else
    {
        foundLayout = true;
        // Acquire the packing and class sizes for the type and cursor to the typedef entry.
        if (1 != md_get_column_value_as_constant(entry, mdtClassLayout_PackingSize, 1, (uint32_t*)pdwPackSize)
            || 1 != md_get_column_value_as_constant(entry, mdtClassLayout_ClassSize, 1, (uint32_t*)pulClassSize))
        {
            return CLDB_E_FILE_CORRUPT;
        }
    }

    mdcursor_t typeEntry;
    if (!md_token_to_cursor(_md_ptr.get(), td, &typeEntry))
        return CLDB_E_RECORD_NOTFOUND;

    // Get the list of field data
    mdcursor_t fieldList;
    uint32_t fieldListCount;
    if (!md_get_column_value_as_range(typeEntry, mdtTypeDef_FieldList, &fieldList, &fieldListCount))
        return CLDB_E_FILE_CORRUPT;

    *pcFieldOffset = fieldListCount;
    if (fieldListCount > 0)
    {
        // It is possible the table is empty and can therefore fail. This API permits this
        // behavior and sets the offset as -1 if this occurs.
        mdcursor_t fieldLayoutBegin;
        if (!md_create_cursor(_md_ptr.get(), mdtid_FieldLayout, &fieldLayoutBegin, &unused))
            ::memset(&fieldLayoutBegin, 0, sizeof(fieldLayoutBegin));

        uint32_t readIn = 0;
        for (uint32_t i = 0; i < cMax; ++i)
        {
            COR_FIELD_OFFSET& offset = rFieldOffset[i];
            if (!md_cursor_to_token(fieldList, &offset.ridOfField))
                return CLDB_E_FILE_CORRUPT;

            // See above comment about empty FieldLayout table.
            offset.ulOffset = (ULONG)-1;
            mdcursor_t fieldLayoutRow;
            if (md_find_row_from_cursor(fieldLayoutBegin, mdtFieldLayout_Field, RidFromToken(offset.ridOfField), &fieldLayoutRow))
            {
                (void)md_get_column_value_as_constant(fieldLayoutRow, mdtFieldLayout_Offset, 1, (uint32_t*)&offset.ulOffset);
                foundLayout = true;
            }

            readIn++;
            if (readIn >= fieldListCount)
                break;

            if (!md_cursor_next(&fieldList))
                return CLDB_E_FILE_CORRUPT;
        }
    }

    if (!foundLayout)
        return CLDB_E_RECORD_NOTFOUND;

    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetFieldMarshal(
    mdToken     tk,
    PCCOR_SIGNATURE* ppvNativeType,
    ULONG* pcbNativeType)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetRVA(
    mdToken     tk,
    ULONG* pulCodeRVA,
    DWORD* pdwImplFlags)
{
    uint32_t codeRVA = 0;
    uint32_t implFlags = 0;

    mdcursor_t cursor;
    if (TypeFromToken(tk) == mdtMethodDef)
    {
        if (!md_token_to_cursor(_md_ptr.get(), tk, &cursor))
            return CLDB_E_RECORD_NOTFOUND;

        if (1 != md_get_column_value_as_constant(cursor, mdtMethodDef_Rva, 1, &codeRVA)
            || 1 != md_get_column_value_as_constant(cursor, mdtMethodDef_ImplFlags, 1, &implFlags))
        {
            return CLDB_E_FILE_CORRUPT;
        }
    }
    else
    {
        if (TypeFromToken(tk) != mdtFieldDef)
            return E_INVALIDARG;

        uint32_t unused;
        mdcursor_t fieldRvaRow;
        if (!md_create_cursor(_md_ptr.get(), mdtid_FieldRva, &cursor, &unused)
            || !md_find_row_from_cursor(cursor, mdtFieldRva_Field, RidFromToken(tk), &fieldRvaRow))
        {
            return CLDB_E_RECORD_NOTFOUND;
        }

        if (1 != md_get_column_value_as_constant(fieldRvaRow, mdtFieldRva_Rva, 1, &codeRVA))
            return CLDB_E_FILE_CORRUPT;
    }

    if (pulCodeRVA != nullptr)
        *pulCodeRVA = (ULONG)codeRVA;

    if (pdwImplFlags != nullptr)
        *pdwImplFlags = (DWORD)implFlags;

    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetPermissionSetProps(
    mdPermission pm,
    DWORD* pdwAction,
    void const** ppvPermission,
    ULONG* pcbPermission)
{
    if (TypeFromToken(pm) != mdtPermission)
        return E_INVALIDARG;

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), pm, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    if (pdwAction != nullptr
        && 1 != md_get_column_value_as_constant(cursor, mdtDeclSecurity_Action, 1, (uint32_t*)pdwAction))
    {
        return CLDB_E_FILE_CORRUPT;
    }

    if (ppvPermission != nullptr
        && 1 != md_get_column_value_as_blob(cursor, mdtDeclSecurity_PermissionSet, 1, (uint8_t const**)ppvPermission, (uint32_t*)pcbPermission))
    {
        return CLDB_E_FILE_CORRUPT;
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetSigFromToken(
    mdSignature mdSig,
    PCCOR_SIGNATURE* ppvSig,
    ULONG* pcbSig)
{
    if (TypeFromToken(mdSig) != mdtSignature || ppvSig == nullptr || pcbSig == nullptr)
        return E_INVALIDARG;

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), mdSig, &cursor))
        return CLDB_E_INDEX_NOTFOUND;

    return (1 == md_get_column_value_as_blob(cursor, mdtStandAloneSig_Signature, 1, ppvSig, (uint32_t*)pcbSig))
        ? S_OK
        : COR_E_BADIMAGEFORMAT;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetModuleRefProps(
    mdModuleRef mur,
    _Out_writes_to_opt_(cchName, *pchName)
    LPWSTR      szName,
    ULONG       cchName,
    ULONG* pchName)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumModuleRefs(
    HCORENUM* phEnum,
    mdModuleRef rModuleRefs[],
    ULONG       cMax,
    ULONG* pcModuleRefs)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        RETURN_IF_FAILED(CreateEnumTokens(_md_ptr.get(), mdtid_ModuleRef, &enumImpl));
        *phEnum = enumImpl;
    }
    return enumImpl->ReadTokens(rModuleRefs, cMax, pcModuleRefs);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetTypeSpecFromToken(
    mdTypeSpec typespec,
    PCCOR_SIGNATURE* ppvSig,
    ULONG* pcbSig)
{
    if (TypeFromToken(typespec) != mdtTypeSpec)
        return E_INVALIDARG;

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), typespec, &cursor)
        || 1 != md_get_column_value_as_blob(cursor, mdtTypeSpec_Signature, 1, ppvSig, (uint32_t*)pcbSig))
    {
        return CLDB_E_RECORD_NOTFOUND;
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetNameFromToken(            // Not Recommended! May be removed!
    mdToken     tk,
    MDUTF8CSTR* pszUtf8NamePtr)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumUnresolvedMethods(
    HCORENUM* phEnum,
    mdToken     rMethods[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    // IMetaDataEmit only
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetUserString(
    mdString    stk,
    _Out_writes_to_opt_(cchString, *pchString)
    LPWSTR      szString,
    ULONG       cchString,
    ULONG* pchString)
{
    if (TypeFromToken(stk) != mdtString || pchString == nullptr)
        return E_INVALIDARG;

    mduserstringcursor_t cursor = RidFromToken(stk);
    mduserstring_t string;
    uint32_t offset;
    if (!md_walk_user_string_heap(_md_ptr.get(), &cursor, 1, &string, &offset))
        return CLDB_E_INDEX_NOTFOUND;

    // Strings in #US should have a trailing single byte.
    if (string.str_bytes % sizeof(WCHAR) == 0)
        return CLDB_E_FILE_CORRUPT;

    // Compute the string size in characters
    uint32_t retCharLen = (string.str_bytes - 1) / sizeof(WCHAR);
    return CopyToStringOutput(string.str, retCharLen, szString, cchString, pchString);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetPinvokeMap(
    mdToken     tk,
    DWORD* pdwMappingFlags,
    _Out_writes_to_opt_(cchImportName, *pchImportName)
    LPWSTR      szImportName,
    ULONG       cchImportName,
    ULONG* pchImportName,
    mdModuleRef* pmrImportDLL)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumSignatures(
    HCORENUM* phEnum,
    mdSignature rSignatures[],
    ULONG       cMax,
    ULONG* pcSignatures)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        RETURN_IF_FAILED(CreateEnumTokens(_md_ptr.get(), mdtid_StandAloneSig, &enumImpl));
        *phEnum = enumImpl;
    }
    return enumImpl->ReadTokens(rSignatures, cMax, pcSignatures);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumTypeSpecs(
    HCORENUM* phEnum,
    mdTypeSpec  rTypeSpecs[],
    ULONG       cMax,
    ULONG* pcTypeSpecs)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        RETURN_IF_FAILED(CreateEnumTokens(_md_ptr.get(), mdtid_TypeSpec, &enumImpl));
        *phEnum = enumImpl;
    }
    return enumImpl->ReadTokens(rTypeSpecs, cMax, pcTypeSpecs);
}

namespace
{
    HRESULT EnumNonEmptyUserStrings(
        _In_ mdhandle_t handle,
        _Inout_ HCORENUMImpl& enumImpl)
    {
        HRESULT hr;

        mduserstring_t us[8];
        uint32_t offsets[ARRAYSIZE(us)];

        mduserstringcursor_t cursor = 0;
        for (;;)
        {
            int32_t count = md_walk_user_string_heap(handle, &cursor, ARRAYSIZE(us), us, offsets);
            if (count == 0)
                break;

            for (int32_t j = 0; j < count; ++j)
            {
                if (us[j].str_bytes == 0)
                    continue;

                RETURN_IF_FAILED(HCORENUMImpl::AddToDynamicEnum(enumImpl, RidToToken(offsets[j], mdtString)));
            }
        }

        return S_OK;
    }
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumUserStrings(
    HCORENUM* phEnum,
    mdString    rStrings[],
    ULONG       cMax,
    ULONG* pcStrings)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        RETURN_IF_FAILED(HCORENUMImpl::CreateDynamicEnum(&enumImpl));

        HCORENUMImpl_ptr cleanup{ enumImpl };
        RETURN_IF_FAILED(EnumNonEmptyUserStrings(_md_ptr.get(), *enumImpl));

        *phEnum = cleanup.release();
    }

    return enumImpl->ReadTokens(rStrings, cMax, pcStrings);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetParamForMethodIndex(
    mdMethodDef md,
    ULONG       ulParamSeq,
    mdParamDef* ppd)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumCustomAttributes(
    HCORENUM* phEnum,
    mdToken     tk,
    mdToken     tkType,
    mdCustomAttribute rCustomAttributes[],
    ULONG       cMax,
    ULONG* pcCustomAttributes)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetCustomAttributeProps(
    mdCustomAttribute cv,
    mdToken* ptkObj,
    mdToken* ptkType,
    void const** ppBlob,
    ULONG* pcbSize)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::FindTypeRef(
    mdToken     tkResolutionScope,
    LPCWSTR     szName,
    mdTypeRef* ptr)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetMemberProps(
    mdToken     mb,
    mdTypeDef* pClass,
    _Out_writes_to_opt_(cchMember, *pchMember)
    LPWSTR      szMember,
    ULONG       cchMember,
    ULONG* pchMember,
    DWORD* pdwAttr,
    PCCOR_SIGNATURE* ppvSigBlob,
    ULONG* pcbSigBlob,
    ULONG* pulCodeRVA,
    DWORD* pdwImplFlags,
    DWORD* pdwCPlusTypeFlag,
    UVCP_CONSTANT* ppValue,
    ULONG* pcchValue)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetFieldProps(
    mdFieldDef  mb,
    mdTypeDef* pClass,
    _Out_writes_to_opt_(cchField, *pchField)
    LPWSTR      szField,
    ULONG       cchField,
    ULONG* pchField,
    DWORD* pdwAttr,
    PCCOR_SIGNATURE* ppvSigBlob,
    ULONG* pcbSigBlob,
    DWORD* pdwCPlusTypeFlag,
    UVCP_CONSTANT* ppValue,
    ULONG* pcchValue)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetPropertyProps(
    mdProperty  prop,
    mdTypeDef* pClass,
    LPCWSTR     szProperty,
    ULONG       cchProperty,
    ULONG* pchProperty,
    DWORD* pdwPropFlags,
    PCCOR_SIGNATURE* ppvSig,
    ULONG* pbSig,
    DWORD* pdwCPlusTypeFlag,
    UVCP_CONSTANT* ppDefaultValue,
    ULONG* pcchDefaultValue,
    mdMethodDef* pmdSetter,
    mdMethodDef* pmdGetter,
    mdMethodDef rmdOtherMethod[],
    ULONG       cMax,
    ULONG* pcOtherMethod)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetParamProps(
    mdParamDef  tk,
    mdMethodDef* pmd,
    ULONG* pulSequence,
    _Out_writes_to_opt_(cchName, *pchName)
    LPWSTR      szName,
    ULONG       cchName,
    ULONG* pchName,
    DWORD* pdwAttr,
    DWORD* pdwCPlusTypeFlag,
    UVCP_CONSTANT* ppValue,
    ULONG* pcchValue)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetCustomAttributeByName(
    mdToken     tkObj,
    LPCWSTR     szName,
    const void** ppData,
    ULONG* pcbData)
{
    return E_NOTIMPL;
}

BOOL STDMETHODCALLTYPE MetadataImportRO::IsValidToken(
    mdToken     tk)
{
    // If we can create a cursor, the token is valid.
    mdcursor_t cursor;
    return md_token_to_cursor(_md_ptr.get(), tk, &cursor)
        ? TRUE
        : FALSE;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetNestedClassProps(
    mdTypeDef   tdNestedClass,
    mdTypeDef* ptdEnclosingClass)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetNativeCallConvFromSig(
    void const* pvSig,
    ULONG       cbSig,
    ULONG* pCallConv)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::IsGlobal(
    mdToken     pd,
    int* pbGlobal)
{
    if (IsValidToken(pd) != TRUE)
        return E_INVALIDARG;

    mdToken parent;
    mdcursor_t cursor;
    switch (TypeFromToken(pd))
    {
    case mdtTypeDef:
        *pbGlobal = (pd == MD_GLOBAL_PARENT_TOKEN);
        break;
    case mdtFieldDef:
    case mdtMethodDef:
    case mdtEvent:
    case mdtProperty:
        if (!md_token_to_cursor(_md_ptr.get(), pd, &cursor))
            return CLDB_E_RECORD_NOTFOUND;
        if (!md_find_token_of_range_element(cursor, &parent))
            return CLDB_E_FILE_CORRUPT;
        *pbGlobal = (parent == MD_GLOBAL_PARENT_TOKEN);
        break;
    default:
        *pbGlobal = FALSE;
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumGenericParams(
    HCORENUM* phEnum,
    mdToken      tk,
    mdGenericParam rGenericParams[],
    ULONG       cMax,
    ULONG* pcGenericParams)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetGenericParamProps(
    mdGenericParam gp,
    ULONG* pulParamSeq,
    DWORD* pdwParamFlags,
    mdToken* ptOwner,
    DWORD* reserved,
    _Out_writes_to_opt_(cchName, *pchName)
    LPWSTR       wzname,
    ULONG        cchName,
    ULONG* pchName)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetMethodSpecProps(
    mdMethodSpec mi,
    mdToken* tkParent,
    PCCOR_SIGNATURE* ppvSigBlob,
    ULONG* pcbSigBlob)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumGenericParamConstraints(
    HCORENUM* phEnum,
    mdGenericParam tk,
    mdGenericParamConstraint rGenericParamConstraints[],
    ULONG       cMax,
    ULONG* pcGenericParamConstraints)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetGenericParamConstraintProps(
    mdGenericParamConstraint gpc,
    mdGenericParam* ptGenericParam,
    mdToken* ptkConstraintType)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetPEKind(
    DWORD* pdwPEKind,
    DWORD* pdwMAchine)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetVersionString(
    _Out_writes_to_opt_(ccBufSize, *pccBufSize)
    LPWSTR      pwzBuf,
    DWORD       ccBufSize,
    DWORD* pccBufSize)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumMethodSpecs(
    HCORENUM* phEnum,
    mdToken      tk,
    mdMethodSpec rMethodSpecs[],
    ULONG       cMax,
    ULONG* pcMethodSpecs)
{
    return E_NOTIMPL;
}

#ifdef DNMD_BUILD_SHARED
#ifdef _MSC_VER
#define DNMD_EXPORT __declspec(dllexport)
#else
#define DNMD_EXPORT __attribute__((__visibility__("default")))
#endif // !_MSC_VER
#endif // DNMD_BUILD_SHARED
#include "metadataimport.hpp"
#include "dnmd_interfaces.hpp"
#include "signatures.hpp"
#include "../hcorenum.hpp"

#include <cassert>

// C++ lifetime wrapper for HCORENUMImpl memory
struct HCORENUMImplInPlaceDeleter
{
    using pointer = HCORENUMImpl*;
    void operator()(HCORENUMImpl* mem)
    {
        HCORENUMImpl::DestroyInAllocatedMemory(mem);
    }
};

using HCORENUMImplInPlace_ptr = std::unique_ptr<HCORENUMImpl, HCORENUMImplInPlaceDeleter>;

static_assert(sizeof(HCORENUMImpl) <= sizeof(HENUMInternal), "HCORENUMImpl must fit in HENUMInternal");

#define MD_MODULE_TOKEN TokenFromRid(1, mdtModule)
#define MD_GLOBAL_PARENT_TOKEN TokenFromRid(1, mdtTypeDef)
#define ToHCORENUMImpl(henumInternal) (reinterpret_cast<HCORENUMImpl*>(henumInternal))
#define ToHENUMInternal(hcorenumImpl) (reinterpret_cast<HENUMInternal*>(hcorenumImpl))
#define RETURN_IF_FAILED(exp) \
{ \
    hr = (exp); \
    if (FAILED(hr)) \
    { \
        return hr; \
    } \
}

namespace
{
    int HexDigit(BYTE c)
    {
        if (c >= '0' && c <= '9')
            return c - '0';
        if (c >= 'a' && c <= 'f')
            return c - 'a' + 10;
        if (c >= 'A' && c <= 'F')
            return c - 'A' + 10;
        return -1;
    }

    bool ParseHex(BYTE const* chars, size_t length, uint32_t* value)
    {
        uint32_t parsed = 0;
        for (size_t i = 0; i < length; ++i)
        {
            int digit = HexDigit(chars[i]);
            if (digit < 0)
                return false;
            parsed = (parsed << 4) | static_cast<uint32_t>(digit);
        }
        *value = parsed;
        return true;
    }

    bool ParseGuid(BYTE const* chars, GUID* guid)
    {
        if (chars[8] != '-' || chars[13] != '-' || chars[18] != '-' || chars[23] != '-')
            return false;

        uint32_t value;
        if (!ParseHex(chars, 8, &value))
            return false;
        guid->Data1 = value;
        if (!ParseHex(chars + 9, 4, &value))
            return false;
        guid->Data2 = static_cast<uint16_t>(value);
        if (!ParseHex(chars + 14, 4, &value))
            return false;
        guid->Data3 = static_cast<uint16_t>(value);

        for (size_t i = 0; i < 8; ++i)
        {
            size_t offset = i < 2 ? 19 + 2 * i : 24 + 2 * (i - 2);
            if (!ParseHex(chars + offset, 2, &value))
                return false;
            guid->Data4[i] = static_cast<uint8_t>(value);
        }
        return true;
    }

    HRESULT CreateEnumTokenRangeForSortedTableKey(
        mdhandle_t mdhandle,
        mdtable_id_t table,
        col_index_t keyColumn,
        mdToken token,
        HCORENUMImpl* pEnumImpl)
    {
        HRESULT hr;
        mdcursor_t cursor;
        uint32_t tableCount;
        if (!md_create_cursor(mdhandle, table, &cursor, &tableCount))
        {
            HCORENUMImpl::CreateDynamicEnumInAllocatedMemory(pEnumImpl);
            return S_OK;
        }
        mdcursor_t begin;
        uint32_t count;
        md_range_result_t result = md_find_range_from_cursor(cursor, keyColumn, token, &begin, &count);

        if (result == MD_RANGE_NOT_FOUND)
        {
            HCORENUMImpl::CreateDynamicEnumInAllocatedMemory(pEnumImpl);
            return S_OK;
        }
        else if (result == MD_RANGE_FOUND)
        {
            HCORENUMImpl::CreateTableEnumInAllocatedMemory(1, pEnumImpl);
            HCORENUMImpl::InitTableEnum(*pEnumImpl, 0, begin, count);
            return S_OK;
        }
        else
        {
            // Unsorted so we need to search across the entire table
            HCORENUMImpl::CreateDynamicEnumInAllocatedMemory(pEnumImpl);
            HCORENUMImplInPlace_ptr implCleanup{ pEnumImpl };
            mdcursor_t curr = cursor;
            uint32_t currCount = tableCount;

            // Read in for matching in bulk
            mdToken matchedGroup[64];
            uint32_t i = 0;
            while (i < currCount)
            {
                int32_t read = md_get_many_rows_column_value_as_token(curr, keyColumn, ARRAY_SIZE(matchedGroup), matchedGroup);
                if (read <= 0)
                    return CLDB_E_FILE_CORRUPT;

                for (int32_t j = 0; j < read; ++j)
                {
                    if (matchedGroup[j] == token)
                    {
                        mdToken matchedTk;
                        if (!md_cursor_to_token(curr, &matchedTk))
                            return CLDB_E_FILE_CORRUPT;
                        RETURN_IF_FAILED(HCORENUMImpl::AddToDynamicEnum(*pEnumImpl, matchedTk));
                    }
                    (void)md_cursor_next(&curr);
                }
                i += read;
            }
            implCleanup.release();
            return S_OK;
        }
    }
}

STDMETHODIMP_(ULONG) InternalMetadataImportRO::GetCountWithTokenKind(
    DWORD       tkKind)
{
    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(m_handle.get(), (mdtable_id_t)(tkKind >> 24), &cursor, &count))
        return 0;

    return count;
}


STDMETHODIMP InternalMetadataImportRO::EnumTypeDefInit(
    HENUMInternal *phEnum)
{
    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(m_handle.get(), mdtid_TypeDef, &cursor, &count))
        return CLDB_E_FILE_CORRUPT;

    HCORENUMImpl* enumImpl = ToHCORENUMImpl(phEnum);
    HCORENUMImpl::CreateTableEnumInAllocatedMemory(1, enumImpl);

    // Skip the first row (TypeDef 0x02000001)
    // We don't want to return the global module type def.
    md_cursor_move(&cursor, 1);

    HCORENUMImpl::InitTableEnum(*enumImpl, 0, cursor, count - 1);

    return S_OK;
}


STDMETHODIMP InternalMetadataImportRO::EnumMethodImplInit(
    mdTypeDef       td,
    HENUMInternal   *phEnumBody,
    HENUMInternal   *phEnumDecl)
{
    // COMPAT, the RO version of this API does not return the decl tokens
    // and it returns the MethodImpl tokens in the body enum.
    if (TypeFromToken(td) != mdtTypeDef)
        return E_INVALIDARG;

    HCORENUMImpl::CreateDynamicEnumInAllocatedMemory(ToHCORENUMImpl(phEnumDecl));
    HCORENUMImpl* enumBody = ToHCORENUMImpl(phEnumBody);
    HCORENUMImpl::CreateTableEnumInAllocatedMemory(1, enumBody);
    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(m_handle.get(), mdtid_MethodImpl, &cursor, &count))
    {
        HCORENUMImpl::InitTableEnum(*enumBody, 0, cursor, 0);
        return S_OK;
    }

    return CreateEnumTokenRangeForSortedTableKey(m_handle.get(), mdtid_MethodImpl, mdtMethodImpl_Class, td, enumBody);
}
STDMETHODIMP_(ULONG) InternalMetadataImportRO::EnumMethodImplGetCount(
    HENUMInternal   *phEnumBody,
    HENUMInternal   *phEnumDecl)
{
    UNREFERENCED_PARAMETER(phEnumDecl);
    return EnumGetCount(phEnumBody);
}
STDMETHODIMP_(void) InternalMetadataImportRO::EnumMethodImplReset(
    HENUMInternal   *phEnumBody,
    HENUMInternal   *phEnumDecl)
{
    ToHCORENUMImpl(phEnumBody)->Reset(0);
    ToHCORENUMImpl(phEnumDecl)->Reset(0);
}
STDMETHODIMP InternalMetadataImportRO::EnumMethodImplNext(
    HENUMInternal   *phEnumBody,
    HENUMInternal   *phEnumDecl,
    mdToken         *ptkBody,
    mdToken         *ptkDecl)
{
    UNREFERENCED_PARAMETER(phEnumDecl);
    HRESULT hr;
    ULONG numTokens = 0;
    mdToken implRecord;
    RETURN_IF_FAILED(ToHCORENUMImpl(phEnumBody)->ReadTokens(&implRecord, 1, &numTokens));
    if (numTokens == 0)
        return S_FALSE;

    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), implRecord, &c))
        return CLDB_E_FILE_CORRUPT;

    if (!md_get_column_value_as_token(c, mdtMethodImpl_MethodBody, ptkBody))
        return CLDB_E_FILE_CORRUPT;

    if (!md_get_column_value_as_token(c, mdtMethodImpl_MethodDeclaration, ptkDecl))
        return CLDB_E_FILE_CORRUPT;
    return S_OK;
}
STDMETHODIMP_(void) InternalMetadataImportRO::EnumMethodImplClose(
    HENUMInternal   *phEnumBody,
    HENUMInternal   *phEnumDecl)
{
    HCORENUMImpl::Destroy(ToHCORENUMImpl(phEnumBody));
    HCORENUMImpl::Destroy(ToHCORENUMImpl(phEnumDecl));
}

STDMETHODIMP InternalMetadataImportRO::EnumGlobalFunctionsInit(
    HENUMInternal   *phEnum)
{
    mdcursor_t globalType;
    if (!md_token_to_cursor(m_handle.get(), MD_GLOBAL_PARENT_TOKEN, &globalType))
        return CLDB_E_FILE_CORRUPT;

    mdcursor_t cursor;
    uint32_t count;
    if (!md_get_column_value_as_range(globalType, mdtTypeDef_MethodList, &cursor, &count))
        return CLDB_E_FILE_CORRUPT;

    HCORENUMImpl* impl = ToHCORENUMImpl(phEnum);
    HCORENUMImpl::CreateTableEnumInAllocatedMemory(1, impl);

    HCORENUMImpl::InitTableEnum(*impl, 0, cursor, count);
    return S_OK;
}
STDMETHODIMP InternalMetadataImportRO::EnumGlobalFieldsInit(
    HENUMInternal   *phEnum)
{
    mdcursor_t globalType;
    if (!md_token_to_cursor(m_handle.get(), MD_GLOBAL_PARENT_TOKEN, &globalType))
        return CLDB_E_FILE_CORRUPT;

    mdcursor_t cursor;
    uint32_t count;
    if (!md_get_column_value_as_range(globalType, mdtTypeDef_FieldList, &cursor, &count))
        return CLDB_E_FILE_CORRUPT;

    HCORENUMImpl* impl = ToHCORENUMImpl(phEnum);
    HCORENUMImpl::CreateTableEnumInAllocatedMemory(1, impl);

    HCORENUMImpl::InitTableEnum(*impl, 0, cursor, count);
    return S_OK;
}
STDMETHODIMP InternalMetadataImportRO::EnumInit(
    DWORD       tkKind,
    mdToken     tkParent,
    HENUMInternal *phEnum)
{
    switch (tkKind)
    {
        case mdtMethodDef:
        {
            mdcursor_t cursor;
            uint32_t count;
            if (!md_token_to_cursor(m_handle.get(), tkParent, &cursor))
                return CLDB_E_FILE_CORRUPT;
            if (!md_get_column_value_as_range(cursor, mdtTypeDef_MethodList, &cursor, &count))
                return CLDB_E_FILE_CORRUPT;

            HCORENUMImpl* impl = ToHCORENUMImpl(phEnum);
            HCORENUMImpl::CreateTableEnumInAllocatedMemory(1, impl);

            HCORENUMImpl::InitTableEnum(*impl, 0, cursor, count);
            return S_OK;
        }
        case mdtFieldDef:
        {
            mdcursor_t cursor;
            uint32_t count;
            if (!md_token_to_cursor(m_handle.get(), tkParent, &cursor))
                return CLDB_E_FILE_CORRUPT;
            if (!md_get_column_value_as_range(cursor, mdtTypeDef_FieldList, &cursor, &count))
                return CLDB_E_FILE_CORRUPT;

            HCORENUMImpl* impl = ToHCORENUMImpl(phEnum);
            HCORENUMImpl::CreateTableEnumInAllocatedMemory(1, impl);

            HCORENUMImpl::InitTableEnum(*impl, 0, cursor, count);
            return S_OK;
        }
        case mdtGenericParam:
        {
            HCORENUMImpl* impl = ToHCORENUMImpl(phEnum);

            return CreateEnumTokenRangeForSortedTableKey(m_handle.get(), mdtid_GenericParam, mdtGenericParam_Owner, tkParent, impl);
        }
        case mdtGenericParamConstraint:
        {
            HCORENUMImpl* impl = ToHCORENUMImpl(phEnum);

            return CreateEnumTokenRangeForSortedTableKey(m_handle.get(), mdtid_GenericParamConstraint, mdtGenericParamConstraint_Owner, tkParent, impl);
        }
        case mdtInterfaceImpl:
        {
            HCORENUMImpl* impl = ToHCORENUMImpl(phEnum);
            return CreateEnumTokenRangeForSortedTableKey(m_handle.get(), mdtid_InterfaceImpl, mdtInterfaceImpl_Class, tkParent, impl);
        }
        case mdtProperty:
        {
            HCORENUMImpl* impl = ToHCORENUMImpl(phEnum);
            HCORENUMImpl::CreateTableEnumInAllocatedMemory(1, impl);

            mdcursor_t cursor;
            uint32_t count;
            mdcursor_t propertyMap;
            if (!md_create_cursor(m_handle.get(), mdtid_PropertyMap, &cursor, &count)
                || !md_find_row_from_cursor(cursor, mdtPropertyMap_Parent, tkParent, &propertyMap))
            {
                HCORENUMImpl::InitTableEnum(*impl, 0, cursor, 0);
                return S_OK;
            }

            mdcursor_t props;
            uint32_t numProps;
            if (!md_get_column_value_as_range(propertyMap, mdtPropertyMap_PropertyList, &props, &numProps))
                return CLDB_E_FILE_CORRUPT;

            HCORENUMImpl::InitTableEnum(*impl, 0, props, numProps);
            return S_OK;
        }
        case mdtEvent:
        {
            HCORENUMImpl* impl = ToHCORENUMImpl(phEnum);
            HCORENUMImpl::CreateTableEnumInAllocatedMemory(1, impl);


            mdcursor_t cursor;
            uint32_t count;
            mdcursor_t eventMap;
            if (!md_create_cursor(m_handle.get(), mdtid_EventMap, &cursor, &count)
                || !md_find_row_from_cursor(cursor, mdtEventMap_Parent, RidFromToken(tkParent), &eventMap))
            {
                HCORENUMImpl::InitTableEnum(*impl, 0, cursor, 0);
                return S_OK;
            }

            mdcursor_t events;
            uint32_t numEvents;
            if (!md_get_column_value_as_range(eventMap, mdtEventMap_EventList, &events, &numEvents))
                return CLDB_E_FILE_CORRUPT;

            HCORENUMImpl::InitTableEnum(*impl, 0, events, numEvents);
            return S_OK;
        }
        case mdtParamDef:
        {
            mdcursor_t cursor;
            uint32_t count;
            if (!md_token_to_cursor(m_handle.get(), tkParent, &cursor))
                return CLDB_E_FILE_CORRUPT;
            if (!md_get_column_value_as_range(cursor, mdtMethodDef_ParamList, &cursor, &count))
                return CLDB_E_FILE_CORRUPT;

            HCORENUMImpl* impl = ToHCORENUMImpl(phEnum);
            HCORENUMImpl::CreateTableEnumInAllocatedMemory(1, impl);

            HCORENUMImpl::InitTableEnum(*impl, 0, cursor, count);
            return S_OK;
        }
        case mdtCustomAttribute:
        {
            HCORENUMImpl* impl = ToHCORENUMImpl(phEnum);
            return CreateEnumTokenRangeForSortedTableKey(m_handle.get(), mdtid_CustomAttribute, mdtCustomAttribute_Parent, tkParent, impl);
        }
        case mdtAssemblyRef:
        case mdtFile:
        case mdtExportedType:
        case mdtManifestResource:
        case mdtModuleRef:
        case mdtMethodImpl:
        {
            assert(IsNilToken(tkParent));
            return EnumAllInit(tkKind, phEnum);
        }
        default:
        {
            assert(false);
            return E_NOTIMPL;
        }
    }
}

STDMETHODIMP InternalMetadataImportRO::EnumAllInit(
    DWORD       tkKind,
    HENUMInternal *phEnum)
{
    HCORENUMImpl* impl = ToHCORENUMImpl(phEnum);
    HCORENUMImpl::CreateTableEnumInAllocatedMemory(1, impl);

    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(m_handle.get(), (mdtable_id_t)(tkKind >> 24), &cursor, &count))
    {
        HCORENUMImpl::InitTableEnum(*impl, 0, cursor, 0);
        return S_OK;
    }

    HCORENUMImpl::InitTableEnum(*impl, 0, cursor, count);
    return S_OK;
}
STDMETHODIMP_(bool) InternalMetadataImportRO::EnumNext(
    HENUMInternal *phEnum,
    mdToken     *ptk)
{
    HCORENUMImpl* impl = ToHCORENUMImpl(phEnum);
    ULONG numTokens = 0;
    return impl->ReadTokens(ptk, 1, &numTokens) == S_OK && numTokens == 1;
}
STDMETHODIMP_(ULONG) InternalMetadataImportRO::EnumGetCount(
    HENUMInternal *phEnum)
{
    return ToHCORENUMImpl(phEnum)->Count();
}
STDMETHODIMP_(void) InternalMetadataImportRO::EnumReset(
    HENUMInternal *phEnum)
{
    ToHCORENUMImpl(phEnum)->Reset(0);
}
STDMETHODIMP_(void) InternalMetadataImportRO::EnumClose(
    HENUMInternal *phEnum)
{
    HCORENUMImpl::DestroyInAllocatedMemory(ToHCORENUMImpl(phEnum));
}


STDMETHODIMP InternalMetadataImportRO::EnumCustomAttributeByNameInit(
    mdToken     tkParent,
    LPCSTR      szName,
    HENUMInternal *phEnum)
{
    HRESULT hr;
    HCORENUMImpl* impl = ToHCORENUMImpl(phEnum);
    HCORENUMImpl::CreateDynamicEnumInAllocatedMemory(impl);

    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(m_handle.get(), mdtid_CustomAttribute, &cursor, &count))
        return S_OK;

    mdcursor_t attributes;
    uint32_t numAttributes;
    md_range_result_t result = md_find_range_from_cursor(cursor, mdtCustomAttribute_Parent, tkParent, &attributes, &numAttributes);
    if (result == MD_RANGE_NOT_FOUND)
        return S_OK;

    bool checkParent = false;
    if (result == MD_RANGE_NOT_SUPPORTED)
    {
        attributes = cursor;
        numAttributes = count;
        checkParent = true;
    }

    HCORENUMImplInPlace_ptr implCleanup{ impl };
    for (uint32_t i = 0; i < numAttributes; i++, md_cursor_next(&attributes))
    {
        mdToken caToken;
        if (!md_cursor_to_token(attributes, &caToken))
            return CLDB_E_FILE_CORRUPT;

        if (checkParent)
        {
            mdToken parent;
            if (!md_get_column_value_as_token(attributes, mdtCustomAttribute_Parent, &parent))
                return CLDB_E_FILE_CORRUPT;

            if (parent != tkParent)
                continue;
        }

        LPCSTR pNamespace;
        LPCSTR pName;
        RETURN_IF_FAILED(GetNameOfCustomAttribute(caToken, &pNamespace, &pName));

        // PERF: Avoid constructing the full type name and instead compare the namespace and name separately
        // with the input name.
        // This removes a heap allocation.
        size_t namespaceLen = strlen(pNamespace);

        // If szName == $"{pNamespace}.{pName}", then it's a match.
        // This is safe because if strcmp(pNamespace, szName) == 0, then szName[namespaceLen] == '\0', so we don't read past the end of the buffer.
        if (strncmp(szName, pNamespace, namespaceLen) == 0
            && szName[namespaceLen] == '.'
            && strcmp(szName + namespaceLen + 1, pName) == 0)
        {
            RETURN_IF_FAILED(HCORENUMImpl::AddToDynamicEnum(*impl, caToken));
        }
    }
    implCleanup.release();

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetParentToken(
    mdToken     tkChild,
    mdToken     *ptkParent)
{
    mdcursor_t cursor;
    if (!md_token_to_cursor(m_handle.get(), tkChild, &cursor))
        return CLDB_E_FILE_CORRUPT;

    switch (TypeFromToken(tkChild))
    {
    case mdtTypeDef:
        {
            mdcursor_t nestedclass;
            uint32_t count;
            // If tkChild isn't a nested type, then *ptkParent has to be left unchanged! (callers depend on that)
            if (!md_create_cursor(m_handle.get(), mdtid_NestedClass, &nestedclass, &count))
                return S_OK;
            if (!md_find_row_from_cursor(nestedclass, mdtNestedClass_NestedClass, RidFromToken(tkChild), &nestedclass))
                return S_OK;
            if (!md_get_column_value_as_token(nestedclass, mdtNestedClass_EnclosingClass, ptkParent))
                return CLDB_E_FILE_CORRUPT;

            return S_OK;
        }
    case mdtMethodSpec:
        if (!md_get_column_value_as_token(cursor, mdtMethodSpec_Method, ptkParent))
            return CLDB_E_FILE_CORRUPT;
        return S_OK;

    case mdtMethodDef:
    case mdtFieldDef:
    case mdtParamDef:
    case mdtEvent:
    case mdtProperty:
        if (!md_find_token_of_range_element(cursor, ptkParent))
            return CLDB_E_FILE_CORRUPT;
        return S_OK;
    case mdtMemberRef:
        if (!md_get_column_value_as_token(cursor, mdtMemberRef_Class, ptkParent))
            return CLDB_E_FILE_CORRUPT;
        return S_OK;

    case mdtCustomAttribute:
        if (!md_get_column_value_as_token(cursor, mdtCustomAttribute_Parent, ptkParent))
            return CLDB_E_FILE_CORRUPT;
        return S_OK;
    }
    return S_OK;
}


STDMETHODIMP InternalMetadataImportRO::GetCustomAttributeProps(
    mdCustomAttribute at,
    mdToken     *ptkType)
{
    mdcursor_t cursor;
    if (!md_token_to_cursor(m_handle.get(), at, &cursor))
        return CLDB_E_FILE_CORRUPT;

    if (!md_get_column_value_as_token(cursor, mdtCustomAttribute_Type, ptkType))
        return CLDB_E_FILE_CORRUPT;

    return S_OK;
}
STDMETHODIMP InternalMetadataImportRO::GetCustomAttributeAsBlob(
    mdCustomAttribute cv,
    void const  **ppBlob,
    ULONG       *pcbSize)
{
    mdcursor_t cursor;
    if (!md_token_to_cursor(m_handle.get(), cv, &cursor))
        return CLDB_E_FILE_CORRUPT;

    uint8_t const* blob;
    uint32_t size;
    if (!md_get_column_value_as_blob(cursor, mdtCustomAttribute_Value, &blob, &size))
        return CLDB_E_FILE_CORRUPT;

    *ppBlob = blob;
    *pcbSize = size;

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetScopeProps(
    LPCSTR      *pszName,
    GUID        *pmvid)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), MD_MODULE_TOKEN, &c))
        return CLDB_E_FILE_CORRUPT;

    if (pszName != nullptr
        && !md_get_column_value_as_utf8(c, mdtModule_Name, pszName))
        return CLDB_E_FILE_CORRUPT;

    if (pmvid != nullptr
        && !md_get_column_value_as_guid(c, mdtModule_Mvid, (mdguid_t*)pmvid))
        return CLDB_E_FILE_CORRUPT;

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::FindParamOfMethod(
    mdMethodDef md,
    ULONG       iSeq,
    mdParamDef  *pparamdef)
{
    mdcursor_t method;
    if (!md_token_to_cursor(m_handle.get(), md, &method))
        return CLDB_E_FILE_CORRUPT;

    mdcursor_t paramList;
    uint32_t count;
    if (!md_get_column_value_as_range(method, mdtMethodDef_ParamList, &paramList, &count))
        return CLDB_E_FILE_CORRUPT;

    for (size_t i = 0; i < count; i++, md_cursor_next(&paramList))
    {
        mdcursor_t param;
        if (!md_resolve_indirect_cursor(paramList, &param))
            return CLDB_E_FILE_CORRUPT;
        uint32_t seq;
        if (!md_get_column_value_as_constant(param, mdtParam_Sequence, &seq))
            return CLDB_E_FILE_CORRUPT;

        if (seq == iSeq)
        {
            if (!md_cursor_to_token(param, pparamdef))
                return CLDB_E_FILE_CORRUPT;
            return S_OK;
        }
    }
    return CLDB_E_RECORD_NOTFOUND;
}

STDMETHODIMP InternalMetadataImportRO::GetNameOfTypeDef(
    mdTypeDef   classdef,
    LPCSTR      *pszname,
    LPCSTR      *psznamespace)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), classdef, &c))
        return CLDB_E_FILE_CORRUPT;

    if (pszname != nullptr
        && !md_get_column_value_as_utf8(c, mdtTypeDef_TypeName, pszname))
        return CLDB_E_FILE_CORRUPT;

    if (psznamespace != nullptr
        && !md_get_column_value_as_utf8(c, mdtTypeDef_TypeNamespace, psznamespace))
        return CLDB_E_FILE_CORRUPT;

    return S_OK;
}
STDMETHODIMP InternalMetadataImportRO::GetIsDualOfTypeDef(
    mdTypeDef   classdef,
    ULONG       *pDual)
{
    ULONG iFace = 0;
    HRESULT hr;

    hr = GetIfaceTypeOfTypeDef(classdef, &iFace);
    if (hr == S_OK)
        *pDual = (iFace == ifDual);
    else
        *pDual = 1;

    return hr;
}
STDMETHODIMP InternalMetadataImportRO::GetIfaceTypeOfTypeDef(
    mdTypeDef   classdef,
    ULONG       *pIface)
{
    HRESULT hr;
    const void* blob;
    ULONG size;
    RETURN_IF_FAILED(GetCustomAttributeByName(classdef, INTEROP_INTERFACETYPE_TYPE, &blob, &size));
    if (size < 5)
        return CLDB_E_FILE_CORRUPT;
    if (*(uint32_t*)blob != 0x1)
        return META_E_CA_INVALID_BLOB;

    *pIface = ((uint8_t*)blob + 4)[0];
    if (*pIface > ifLast)
        *pIface = ifDual;
    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetNameOfMethodDef(
    mdMethodDef md,
    LPCSTR     *pszName)
{
    // Force inline calls to avoid https://developercommunity.visualstudio.com/t/Bad-quality-AMD64-bad-codegen---Storing/10764816
#ifdef _MSC_VER
    [[msvc::forceinline_calls]]
#endif
    {
        mdcursor_t c;
        if (!md_token_to_cursor(m_handle.get(), md, &c))
            return CLDB_E_FILE_CORRUPT;

        if (!md_get_column_value_as_utf8(c, mdtMethodDef_Name, pszName))
            return CLDB_E_FILE_CORRUPT;

        return S_OK;
    }
}

STDMETHODIMP InternalMetadataImportRO::GetNameAndSigOfMethodDef(
    mdMethodDef      methoddef,
    PCCOR_SIGNATURE *ppvSigBlob,
    ULONG           *pcbSigBlob,
    LPCSTR          *pszName)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), methoddef, &c))
        return CLDB_E_FILE_CORRUPT;

    if (!md_get_column_value_as_utf8(c, mdtMethodDef_Name, pszName))
        return CLDB_E_FILE_CORRUPT;

    uint8_t const* blob;
    uint32_t size;
    if (!md_get_column_value_as_blob(c, mdtMethodDef_Signature, &blob, &size))
        return CLDB_E_FILE_CORRUPT;

    *ppvSigBlob = blob;
    *pcbSigBlob = size;

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetNameOfFieldDef(
    mdFieldDef fd,
    LPCSTR    *pszName)
{
    // Force inline calls to avoid https://developercommunity.visualstudio.com/t/Bad-quality-AMD64-bad-codegen---Storing/10764816
#ifdef _MSC_VER
    [[msvc::forceinline_calls]]
#endif
    {
        mdcursor_t c;
        if (!md_token_to_cursor(m_handle.get(), fd, &c))
            return CLDB_E_FILE_CORRUPT;

        if (!md_get_column_value_as_utf8(c, mdtField_Name, pszName))
            return CLDB_E_FILE_CORRUPT;

        return S_OK;
    }
}

STDMETHODIMP InternalMetadataImportRO::GetNameOfTypeRef(
    mdTypeRef   classref,
    LPCSTR      *psznamespace,
    LPCSTR      *pszname)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), classref, &c))
        return CLDB_E_FILE_CORRUPT;

    if (!md_get_column_value_as_utf8(c, mdtTypeRef_TypeName, pszname))
        return CLDB_E_FILE_CORRUPT;

    if (!md_get_column_value_as_utf8(c, mdtTypeRef_TypeNamespace, psznamespace))
        return CLDB_E_FILE_CORRUPT;

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetResolutionScopeOfTypeRef(
    mdTypeRef classref,
    mdToken  *ptkResolutionScope)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), classref, &c))
        return CLDB_E_FILE_CORRUPT;

    if (!md_get_column_value_as_token(c, mdtTypeRef_ResolutionScope, ptkResolutionScope))
        return CLDB_E_FILE_CORRUPT;

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::FindTypeRefByName(
    LPCSTR      szNamespace,
    LPCSTR      szName,
    mdToken     tkResolutionScope,
    mdTypeRef   *ptk)
{
    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(m_handle.get(), mdtid_TypeRef, &cursor, &count))
        return CLDB_E_RECORD_NOTFOUND;

    bool scopeIsSet = !IsNilToken(tkResolutionScope);
    mdToken resMaybe;
    char const* str;
    for (uint32_t i = 0; i < count; (void)md_cursor_next(&cursor), ++i)
    {
        if (!md_get_column_value_as_token(cursor, mdtTypeRef_ResolutionScope, &resMaybe))
            return CLDB_E_FILE_CORRUPT;

        // See if the Resolution scopes match.
        if ((IsNilToken(resMaybe) && scopeIsSet)    // User didn't state scope.
            || resMaybe != tkResolutionScope)       // Match user scope.
        {
            continue;
        }

        if (!md_get_column_value_as_utf8(cursor, mdtTypeRef_TypeNamespace, &str))
            return CLDB_E_FILE_CORRUPT;

        if (0 != ::strcmp(szNamespace, str))
            continue;

        if (!md_get_column_value_as_utf8(cursor, mdtTypeRef_TypeName, &str))
            return CLDB_E_FILE_CORRUPT;

        if (0 == ::strcmp(szName, str))
        {
            (void)md_cursor_to_token(cursor, ptk);
            return S_OK;
        }
    }

    // Not found.
    return CLDB_E_RECORD_NOTFOUND;
}

STDMETHODIMP InternalMetadataImportRO::GetTypeDefProps(
    mdTypeDef   classdef,
    DWORD       *pdwAttr,
    mdToken     *ptkExtends)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), classdef, &c))
        return CLDB_E_FILE_CORRUPT;

    if (pdwAttr != nullptr)
    {
        uint32_t attr;
        if (!md_get_column_value_as_constant(c, mdtTypeDef_Flags, &attr))
            return CLDB_E_FILE_CORRUPT;

        *pdwAttr = attr;
    }

    if (ptkExtends != nullptr
        && !md_get_column_value_as_token(c, mdtTypeDef_Extends, ptkExtends))
        return CLDB_E_FILE_CORRUPT;

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetItemGuid(
    mdToken     tkObj,
    CLSID       *pGuid)
{
    if (pGuid == nullptr)
        return E_INVALIDARG;

    BYTE const* blob = nullptr;
    ULONG blobLength = 0;
    HRESULT hr = GetCustomAttributeByName(tkObj, INTEROP_GUID_TYPE, (const void**)&blob, &blobLength);
    if (hr == S_FALSE)
    {
        *pGuid = GUID_NULL;
        return S_FALSE;
    }
    if (FAILED(hr))
        return hr;

    // The attribute stores a 36-byte GUID string after its prolog and serialized length.
    if (blob == nullptr || blobLength != 41 || blob[0] != 1 || blob[1] != 0
        || blob[2] != 36 || blob[39] != 0 || blob[40] != 0)
        return E_INVALIDARG;

    GUID parsed{};
    if (!ParseGuid(blob + 3, &parsed))
        return E_INVALIDARG;
    *pGuid = parsed;
    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetNestedClassProps(
    mdTypeDef   tkNestedClass,
    mdTypeDef   *ptkEnclosingClass)
{
    if (TypeFromToken(tkNestedClass) != mdtTypeDef)
        return E_INVALIDARG;

    mdcursor_t cursor;
    uint32_t count;
    mdcursor_t nestedClassRow;
    if (!md_create_cursor(m_handle.get(), mdtid_NestedClass, &cursor, &count)
        || !md_find_row_from_cursor(cursor, mdtNestedClass_NestedClass, RidFromToken(tkNestedClass), &nestedClassRow))
    {
        return CLDB_E_RECORD_NOTFOUND;
    }

    mdTypeDef enclosed;
    if (!md_get_column_value_as_token(nestedClassRow, mdtNestedClass_EnclosingClass, &enclosed))
        return CLDB_E_FILE_CORRUPT;

    *ptkEnclosingClass = enclosed;
    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetCountNestedClasses(
    mdTypeDef   tkEnclosingClass,
    ULONG      *pcNestedClassesCount)
{
    if (TypeFromToken(tkEnclosingClass) != mdtTypeDef)
        return E_INVALIDARG;

    mdcursor_t cursor;
    uint32_t count;
    mdcursor_t nestedClassRowStart;
    uint32_t nestedClassRowCount;
    if (!md_create_cursor(m_handle.get(), mdtid_NestedClass, &cursor, &count))
    {
        return CLDB_E_RECORD_NOTFOUND;
    }

    md_range_result_t result = md_find_range_from_cursor(cursor, mdtNestedClass_EnclosingClass, RidFromToken(tkEnclosingClass), &nestedClassRowStart, &nestedClassRowCount);
    if (result == MD_RANGE_NOT_FOUND)
    {
        return CLDB_E_RECORD_NOTFOUND;
    }
    else if (result == MD_RANGE_NOT_SUPPORTED)
    {
        nestedClassRowCount = 0;
        for (uint32_t i = 0; i < count; i++, md_cursor_next(&cursor))
        {
            mdToken enclosingClass;
            if (!md_get_column_value_as_token(cursor, mdtNestedClass_EnclosingClass, &enclosingClass))
                return CLDB_E_FILE_CORRUPT;

            if (enclosingClass == tkEnclosingClass)
                nestedClassRowCount++;
        }

    }

    *pcNestedClassesCount = nestedClassRowCount;
    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetNestedClasses(
    mdTypeDef   tkEnclosingClass,
    mdTypeDef   *rNestedClasses,
    ULONG       ulNestedClasses,
    ULONG      *pcNestedClasses)
{
    if (TypeFromToken(tkEnclosingClass) != mdtTypeDef)
        return E_INVALIDARG;

    mdcursor_t cursor;
    uint32_t count;
    mdcursor_t nestedClassRowStart;
    uint32_t nestedClassRowCount;
    if (!md_create_cursor(m_handle.get(), mdtid_NestedClass, &cursor, &count))
    {
        return CLDB_E_RECORD_NOTFOUND;
    }

    md_range_result_t result = md_find_range_from_cursor(cursor, mdtNestedClass_EnclosingClass, RidFromToken(tkEnclosingClass), &nestedClassRowStart, &nestedClassRowCount);
    if (result == MD_RANGE_NOT_FOUND)
    {
        return CLDB_E_RECORD_NOTFOUND;
    }
    else if (result == MD_RANGE_NOT_SUPPORTED)
    {
        nestedClassRowCount = 0;
        for (uint32_t i = 0; i < count; i++, md_cursor_next(&cursor))
        {
            mdToken enclosingClass;
            if (!md_get_column_value_as_token(cursor, mdtNestedClass_EnclosingClass, &enclosingClass))
                return CLDB_E_FILE_CORRUPT;

            if (enclosingClass == tkEnclosingClass)
            {
                if (!md_get_column_value_as_token(cursor, mdtNestedClass_NestedClass, &rNestedClasses[nestedClassRowCount++]))
                    return CLDB_E_FILE_CORRUPT;

                if (nestedClassRowCount == ulNestedClasses)
                    break;
            }
        }

        *pcNestedClasses = nestedClassRowCount;
        return S_OK;
    }

    int32_t numReadRows = md_get_many_rows_column_value_as_token(nestedClassRowStart, mdtNestedClass_NestedClass, std::min((uint32_t)ulNestedClasses, nestedClassRowCount), rNestedClasses);

    if (numReadRows == -1)
        return CLDB_E_FILE_CORRUPT;

    *pcNestedClasses = (uint32_t)numReadRows;

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetModuleRefProps(
    mdModuleRef mur,
    LPCSTR      *pszName)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), mur, &c))
        return CLDB_E_FILE_CORRUPT;

    if (!md_get_column_value_as_utf8(c, mdtModuleRef_Name, pszName))
        return CLDB_E_FILE_CORRUPT;

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetSigOfMethodDef(
    mdMethodDef       tkMethodDef,
    ULONG *           pcbSigBlob,
    PCCOR_SIGNATURE * ppSig)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), tkMethodDef, &c))
        return CLDB_E_FILE_CORRUPT;

    uint8_t const* sig;
    uint32_t sigLength;
    if (!md_get_column_value_as_blob(c, mdtMethodDef_Signature, &sig, &sigLength))
        return CLDB_E_FILE_CORRUPT;

    *ppSig = sig;
    *pcbSigBlob = sigLength;

    return S_OK;
}
STDMETHODIMP InternalMetadataImportRO::GetSigOfFieldDef(
    mdFieldDef        tkFieldDef,
    ULONG *           pcbSigBlob,
    PCCOR_SIGNATURE * ppSig)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), tkFieldDef, &c))
        return CLDB_E_FILE_CORRUPT;

    uint8_t const* sig;
    uint32_t sigLength;
    if (!md_get_column_value_as_blob(c, mdtField_Signature, &sig, &sigLength))
        return CLDB_E_FILE_CORRUPT;

    *ppSig = sig;
    *pcbSigBlob = sigLength;

    return S_OK;
}
STDMETHODIMP InternalMetadataImportRO::GetSigFromToken(
    mdToken           tk,
    ULONG *           pcbSig,
    PCCOR_SIGNATURE * ppSig)
{
    col_index_t targetColumn;
    switch (TypeFromToken(tk))
    {
        case mdtSignature:
            targetColumn = mdtStandAloneSig_Signature;
            break;
        case mdtTypeSpec:
            targetColumn = mdtTypeSpec_Signature;
            break;
        case mdtMethodDef:
            targetColumn = mdtMethodDef_Signature;
            break;
        case mdtFieldDef:
            targetColumn = mdtField_Signature;
            break;
        default:
            *pcbSig = 0;
            return META_E_INVALID_TOKEN_TYPE;
    }

    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), tk, &c))
        return CLDB_E_FILE_CORRUPT;

    uint8_t const* sig;
    uint32_t sigLength;
    if (!md_get_column_value_as_blob(c, targetColumn, &sig, &sigLength))
        return CLDB_E_FILE_CORRUPT;

    *ppSig = sig;
    *pcbSig = sigLength;

    return S_OK;
}



STDMETHODIMP InternalMetadataImportRO::GetMethodDefProps(
    mdMethodDef md,
    DWORD      *pdwFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), md, &c))
        return CLDB_E_FILE_CORRUPT;

    uint32_t flags;
    if (!md_get_column_value_as_constant(c, mdtMethodDef_Flags, &flags))
        return CLDB_E_FILE_CORRUPT;

    *pdwFlags = flags;

    return S_OK;
}


STDMETHODIMP InternalMetadataImportRO::GetMethodImplProps(
    mdToken     tk,
    ULONG       *pulCodeRVA,
    DWORD       *pdwImplFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), tk, &c))
        return CLDB_E_FILE_CORRUPT;

    if (pulCodeRVA != nullptr)
    {
        uint32_t rva;
        if (!md_get_column_value_as_constant(c, mdtMethodDef_Rva, &rva))
            return CLDB_E_FILE_CORRUPT;

        *pulCodeRVA = rva;
    }

    if (pdwImplFlags != nullptr)
    {
        uint32_t flags;
        if (!md_get_column_value_as_constant(c, mdtMethodDef_ImplFlags, &flags))
            return CLDB_E_FILE_CORRUPT;

        *pdwImplFlags = flags;
    }

    return S_OK;
}


STDMETHODIMP InternalMetadataImportRO::GetFieldRVA(
    mdFieldDef  fd,
    ULONG       *pulCodeRVA)
{
    mdcursor_t cursor;
    uint32_t count;
    mdcursor_t fieldRvaRow;
    if (!md_create_cursor(m_handle.get(), mdtid_FieldRva, &cursor, &count)
        || !md_find_row_from_cursor(cursor, mdtFieldRva_Field, RidFromToken(fd), &fieldRvaRow))
        return CLDB_E_RECORD_NOTFOUND;

    uint32_t rva;
    if (!md_get_column_value_as_constant(fieldRvaRow, mdtFieldRva_Rva, &rva))
        return CLDB_E_FILE_CORRUPT;

    *pulCodeRVA = rva;

    return S_OK;
}


STDMETHODIMP InternalMetadataImportRO::GetFieldDefProps(
    mdFieldDef fd,
    DWORD     *pdwFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), fd, &c))
        return CLDB_E_FILE_CORRUPT;

    uint32_t flags;
    if (!md_get_column_value_as_constant(c, mdtField_Flags, &flags))
        return CLDB_E_FILE_CORRUPT;

    *pdwFlags = flags;

    return S_OK;
}

namespace
{
    HRESULT FillMDDefaultValue(
        BYTE        bType,
        void const *pValue,
        ULONG       cbValue,
        MDDefaultValue  *pMDDefaultValue)
    {
        HRESULT     hr = S_OK;

        pMDDefaultValue->m_bType = bType;
        pMDDefaultValue->m_cbSize = cbValue;
        switch (bType)
        {
        case ELEMENT_TYPE_BOOLEAN:
            if (cbValue < 1)
            {
                return CLDB_E_FILE_CORRUPT;
            }
            pMDDefaultValue->m_bValue = *((BYTE *) pValue);
            break;
        case ELEMENT_TYPE_I1:
            if (cbValue < 1)
            {
                return CLDB_E_FILE_CORRUPT;
            }
            pMDDefaultValue->m_cValue = *((CHAR *) pValue);
            break;
        case ELEMENT_TYPE_U1:
            if (cbValue < 1)
            {
                return CLDB_E_FILE_CORRUPT;
            }
            pMDDefaultValue->m_byteValue = *((BYTE *) pValue);
            break;
        case ELEMENT_TYPE_I2:
            if (cbValue < 2)
            {
                return CLDB_E_FILE_CORRUPT;
            }
            pMDDefaultValue->m_sValue = *(int16_t*)(pValue);
            break;
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_CHAR:
            if (cbValue < 2)
            {
                return CLDB_E_FILE_CORRUPT;
            }
            pMDDefaultValue->m_usValue = *(uint16_t*)(pValue);
            break;
        case ELEMENT_TYPE_I4:
            if (cbValue < 4)
            {
                return CLDB_E_FILE_CORRUPT;
            }
            pMDDefaultValue->m_lValue = *(int32_t*)(pValue);
            break;
        case ELEMENT_TYPE_U4:
            if (cbValue < 4)
            {
                return CLDB_E_FILE_CORRUPT;
            }
            pMDDefaultValue->m_ulValue = *(uint32_t*)(pValue);
            break;
        case ELEMENT_TYPE_R4:
            {
                if (cbValue < 4)
                {
                    return CLDB_E_FILE_CORRUPT;
                }
                pMDDefaultValue->m_fltValue = *(float*)(pValue);
            }
            break;
        case ELEMENT_TYPE_R8:
            {
                if (cbValue < 8)
                {
                    return CLDB_E_FILE_CORRUPT;
                }
                pMDDefaultValue->m_dblValue = *(double*)(pValue);
            }
            break;
        case ELEMENT_TYPE_STRING:
            if (cbValue == 0)
                pValue = NULL;
            pMDDefaultValue->m_wzValue = (LPWSTR) pValue;
            break;
        case ELEMENT_TYPE_CLASS:
            //
            // There is only a 4-byte quantity in the MetaData, and it must always
            // be zero.  So, we load an INT32 and zero-extend it to be pointer-sized.
            //
            if (cbValue < 4)
            {
                return CLDB_E_FILE_CORRUPT;
            }
            pMDDefaultValue->m_unkValue = (IUnknown *)(UINT_PTR)(*(int32_t*)pValue);
            if (pMDDefaultValue->m_unkValue != NULL)
            {
                _ASSERTE(!"Non-NULL objectref's are not supported as default values!");
                return CLDB_E_FILE_CORRUPT;
            }
            break;
        case ELEMENT_TYPE_I8:
            if (cbValue < 8)
            {
                return CLDB_E_FILE_CORRUPT;
            }
            pMDDefaultValue->m_llValue = *(int64_t*)(pValue);
            break;
        case ELEMENT_TYPE_U8:
            if (cbValue < 8)
            {
                return CLDB_E_FILE_CORRUPT;
            }
            pMDDefaultValue->m_ullValue = *(uint64_t*)(pValue);
            break;
        case ELEMENT_TYPE_VOID:
            break;
        default:
            return CLDB_E_FILE_CORRUPT;
            break;
        }
        return hr;
    }
}

STDMETHODIMP InternalMetadataImportRO::GetDefaultValue(
    mdToken     tk,
    MDDefaultValue *pDefaultValue)
{
    assert(pDefaultValue);

    HRESULT     hr;

    mdcursor_t constantTable;
    uint32_t constantTableLength;
    if (!md_create_cursor(m_handle.get(), mdtid_Constant, &constantTable, &constantTableLength))
        return CLDB_E_FILE_CORRUPT;

    mdcursor_t constant;
    if (!md_find_row_from_cursor(constantTable, mdtConstant_Parent, tk, &constant))
    {
        pDefaultValue->m_bType = ELEMENT_TYPE_VOID;
        return S_OK;
    }

    uint32_t type;
    if (!md_get_column_value_as_constant(constant, mdtConstant_Type, &type))
        return CLDB_E_FILE_CORRUPT;

    // get the value blob
    uint8_t const* value;
    uint32_t valueLength;
    if (!md_get_column_value_as_blob(constant, mdtConstant_Value, &value, &valueLength))
        return CLDB_E_FILE_CORRUPT;

    hr = FillMDDefaultValue((BYTE)type, value, valueLength, pDefaultValue);
    return hr;
}


STDMETHODIMP InternalMetadataImportRO::GetDispIdOfMemberDef(
    mdToken     tk,
    ULONG       *pDispid)
{
    HRESULT     hr;                     // A result.
    const BYTE  *pBlob;                 // Blob with dispid.
    ULONG       cbBlob;                 // Length of blob.

    // Get the DISPID, if any.
    assert(pDispid);

    *pDispid = static_cast<ULONG>(-1);
    hr = GetCustomAttributeByName(tk, INTEROP_DISPID_TYPE, (const void**)&pBlob, &cbBlob);
    if (hr == S_OK)
    {
        if (cbBlob < 8)
            return META_E_CA_INVALID_BLOB;
        *pDispid = ((uint32_t*)pBlob)[1];
    }
    return hr;
}


STDMETHODIMP InternalMetadataImportRO::GetTypeOfInterfaceImpl(
    mdInterfaceImpl iiImpl,
    mdToken        *ptkType)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), iiImpl, &c))
        return CLDB_E_FILE_CORRUPT;

    if (!md_get_column_value_as_token(c, mdtInterfaceImpl_Interface, ptkType))
        return CLDB_E_FILE_CORRUPT;

    return S_OK;
}

namespace
{
    HRESULT FindTypeDefByName(
        InternalMetadataImportRO* importer,
        char const* nspace,
        char const* name,
        mdToken tkEnclosingClass,
        mdTypeDef* ptd)
    {
        assert(importer != nullptr && nspace != nullptr && name != nullptr && ptd != nullptr);
        *ptd = mdTypeDefNil;

        HRESULT hr;

        // If the caller supplied a TypeRef scope, we need to walk until we find
        // a TypeDef scope we can use to look up the inner definition.
        if (TypeFromToken(tkEnclosingClass) == mdtTypeRef)
        {
            mdcursor_t typeRefCursor;
            if (!md_token_to_cursor(importer->MetaData(), tkEnclosingClass, &typeRefCursor))
                return CLDB_E_RECORD_NOTFOUND;

            uint32_t typeRefScope;
            char const* typeRefNspace;
            char const* typeRefName;
            if (!md_get_column_value_as_token(typeRefCursor, mdtTypeRef_ResolutionScope, &typeRefScope)
                || !md_get_column_value_as_utf8(typeRefCursor, mdtTypeRef_TypeNamespace, &typeRefNspace)
                || !md_get_column_value_as_utf8(typeRefCursor, mdtTypeRef_TypeName, &typeRefName))
            {
                return CLDB_E_FILE_CORRUPT;
            }

            if (tkEnclosingClass == typeRefScope
                && 0 == ::strcmp(name, typeRefName)
                && 0 == ::strcmp(nspace, typeRefNspace))
            {
                // This defensive workaround works around a feature of DotFuscator that adds a bad TypeRef
                // which causes tools like ILDASM to crash. The TypeRef's parent is set to itself
                // which causes this function to recurse infinitely.
                return CLDB_E_FILE_CORRUPT;
            }

            // Update tkEnclosingClass to TypeDef
            RETURN_IF_FAILED(FindTypeDefByName(
                importer,
                typeRefNspace,
                typeRefName,
                (TypeFromToken(typeRefScope) == mdtTypeRef) ? typeRefScope : mdTokenNil,
                &tkEnclosingClass));
            assert(TypeFromToken(tkEnclosingClass) == mdtTypeDef);
        }

        mdcursor_t cursor;
        uint32_t count;
        if (!md_create_cursor(importer->MetaData(), mdtid_TypeDef, &cursor, &count))
            return CLDB_E_RECORD_NOTFOUND;

        uint32_t flags;
        char const* str;
        mdToken tk;
        mdToken tmpTk;
        for (uint32_t i = 0; i < count; (void)md_cursor_next(&cursor), ++i)
        {
            if (!md_get_column_value_as_constant(cursor, mdtTypeDef_Flags, &flags))
                return CLDB_E_FILE_CORRUPT;

            // Use XOR to handle the following in a single expression:
            //  - The class is Nested and EnclosingClass passed is nil
            //      or
            //  - The class is not Nested and EnclosingClass passed in is not nil
            if (!(IsTdNested(flags) ^ IsNilToken(tkEnclosingClass)))
                continue;

            // Filter to enclosing class
            if (!IsNilToken(tkEnclosingClass))
            {
                assert(TypeFromToken(tkEnclosingClass) == mdtTypeDef);
                (void)md_cursor_to_token(cursor, &tk);
                hr = importer->GetNestedClassProps(tk, &tmpTk);

                // Skip this type if it doesn't have an enclosing class
                // or its enclosing doesn't match the filter.
                if (FAILED(hr) || tmpTk != tkEnclosingClass)
                    continue;
            }

            if (!md_get_column_value_as_utf8(cursor, mdtTypeDef_TypeNamespace, &str))
                return CLDB_E_FILE_CORRUPT;

            if (0 != ::strcmp(nspace, str))
                continue;

            if (!md_get_column_value_as_utf8(cursor, mdtTypeDef_TypeName, &str))
                return CLDB_E_FILE_CORRUPT;

            if (0 == ::strcmp(name, str))
            {
                (void)md_cursor_to_token(cursor, ptd);
                return S_OK;
            }
        }
        return CLDB_E_RECORD_NOTFOUND;
    }
}


STDMETHODIMP InternalMetadataImportRO::FindTypeDef(
    LPCSTR      szNamespace,
    LPCSTR      szName,
    mdToken     tkEnclosingClass,
    mdTypeDef   *ptypedef)
{
    return FindTypeDefByName(this, szNamespace, szName, tkEnclosingClass, ptypedef);
}


STDMETHODIMP InternalMetadataImportRO::GetNameAndSigOfMemberRef(
    mdMemberRef      memberref,
    PCCOR_SIGNATURE *ppvSigBlob,
    ULONG           *pcbSigBlob,
    LPCSTR          *pszName)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), memberref, &c))
        return CLDB_E_FILE_CORRUPT;

    if (ppvSigBlob != nullptr)
    {
        uint8_t const* sig;
        uint32_t sigLength;
        if (!md_get_column_value_as_blob(c, mdtMemberRef_Signature, &sig, &sigLength))
            return CLDB_E_FILE_CORRUPT;

        *ppvSigBlob = sig;
        *pcbSigBlob = sigLength;
    }

    if (!md_get_column_value_as_utf8(c, mdtMemberRef_Name, pszName))
        return CLDB_E_FILE_CORRUPT;

    return S_OK;
}


STDMETHODIMP InternalMetadataImportRO::GetParentOfMemberRef(
    mdMemberRef memberref,
    mdToken    *ptkParent)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), memberref, &c))
        return CLDB_E_FILE_CORRUPT;

    if (!md_get_column_value_as_token(c, mdtMemberRef_Class, ptkParent))
        return CLDB_E_FILE_CORRUPT;

    return S_OK;
}
STDMETHODIMP InternalMetadataImportRO::GetParamDefProps(
    mdParamDef paramdef,
    USHORT    *pusSequence,
    DWORD     *pdwAttr,
    LPCSTR    *pszName)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), paramdef, &c))
        return CLDB_E_FILE_CORRUPT;

    if (pusSequence != nullptr)
    {
        uint32_t sequence;
        if (!md_get_column_value_as_constant(c, mdtParam_Sequence, &sequence))
            return CLDB_E_FILE_CORRUPT;

        *pusSequence = (USHORT)sequence;
    }

    if (pdwAttr != nullptr)
    {
        uint32_t flags;
        if (!md_get_column_value_as_constant(c, mdtParam_Flags, &flags))
            return CLDB_E_FILE_CORRUPT;

        *pdwAttr = flags;
    }

    if (!md_get_column_value_as_utf8(c, mdtParam_Name, pszName))
        return CLDB_E_FILE_CORRUPT;

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetPropertyInfoForMethodDef(
    mdMethodDef md,
    mdProperty  *ppd,
    LPCSTR      *pName,
    ULONG       *pSemantic)
{
    mdcursor_t c;
    uint32_t count;
    if (!md_create_cursor(m_handle.get(), mdtid_MethodSemantics, &c, &count))
        return CLDB_E_FILE_CORRUPT;

    mdcursor_t semantics;
    if (!md_find_row_from_cursor(c, mdtMethodSemantics_Method, RidFromToken(md), &semantics))
        return CLDB_E_FILE_CORRUPT;

    mdToken association;
    if (!md_get_column_value_as_token(semantics, mdtMethodSemantics_Association, &association))
        return CLDB_E_FILE_CORRUPT;

    if (TypeFromToken(association) != mdtProperty)
        return S_FALSE;

    if (ppd)
        *ppd = association;

    mdcursor_t prop;
    if (!md_token_to_cursor(m_handle.get(), association, &prop))
        return CLDB_E_FILE_CORRUPT;

    if (pName)
    {
        if (!md_get_column_value_as_utf8(prop, mdtProperty_Name, pName))
            return CLDB_E_FILE_CORRUPT;
    }

    if (pSemantic)
    {
        uint32_t flags;
        if (!md_get_column_value_as_constant(semantics, mdtMethodSemantics_Semantics, &flags))
            return CLDB_E_FILE_CORRUPT;

        *pSemantic = flags;
    }

    return S_OK;
}


STDMETHODIMP InternalMetadataImportRO::GetClassPackSize(
    mdTypeDef   td,
    ULONG       *pdwPackSize)
{
    if (TypeFromToken(td) != mdtTypeDef)
        return E_INVALIDARG;

    mdcursor_t begin;
    uint32_t count;
    mdcursor_t entry;
    if (!md_create_cursor(m_handle.get(), mdtid_ClassLayout, &begin, &count)
        || !md_find_row_from_cursor(begin, mdtClassLayout_Parent, RidFromToken(td), &entry))
    {
        return CLDB_E_RECORD_NOTFOUND;
    }
    uint32_t packSize;
    // Acquire the packing and class sizes for the type and cursor to the typedef entry.
    if (!md_get_column_value_as_constant(entry, mdtClassLayout_PackingSize, &packSize))
    {
        return CLDB_E_FILE_CORRUPT;
    }

    *pdwPackSize = packSize;
    return S_OK;
}
STDMETHODIMP InternalMetadataImportRO::GetClassTotalSize(
    mdTypeDef   td,
    ULONG       *pdwClassSize)
{
    if (TypeFromToken(td) != mdtTypeDef)
        return E_INVALIDARG;

    mdcursor_t begin;
    uint32_t count;
    mdcursor_t entry;
    if (!md_create_cursor(m_handle.get(), mdtid_ClassLayout, &begin, &count)
        || !md_find_row_from_cursor(begin, mdtClassLayout_Parent, RidFromToken(td), &entry))
    {
        return CLDB_E_RECORD_NOTFOUND;
    }

    uint32_t classSize;
    // Acquire the packing and class sizes for the type and cursor to the typedef entry.
    if (!md_get_column_value_as_constant(entry, mdtClassLayout_ClassSize, &classSize))
    {
        return CLDB_E_FILE_CORRUPT;
    }
    *pdwClassSize = classSize;
    return S_OK;
}
STDMETHODIMP InternalMetadataImportRO::GetClassLayoutInit(
    mdTypeDef   td,
    MD_CLASS_LAYOUT *pLayout)
{
    if (TypeFromToken(td) != mdtTypeDef)
        return E_INVALIDARG;

    mdcursor_t typeEntry;
    if (!md_token_to_cursor(m_handle.get(), td, &typeEntry))
        return CLDB_E_RECORD_NOTFOUND;

    // Get the list of field data
    mdcursor_t fieldList;
    uint32_t fieldListCount;
    if (!md_get_column_value_as_range(typeEntry, mdtTypeDef_FieldList, &fieldList, &fieldListCount))
        return CLDB_E_FILE_CORRUPT;

    mdToken firstField;
    if (!md_cursor_to_token(fieldList, &firstField))
    {
        mdcursor_t fieldTable;
        uint32_t fieldCount = 0;
        // If the image has fields, we need to put the next (non-existent) row value
        // here for compat. If there are no fields, it needs to be 0.
        (void)md_create_cursor(m_handle.get(), mdtid_Field, &fieldTable, &fieldCount);
        pLayout->m_ridFieldCur = fieldCount;
        pLayout->m_ridFieldEnd = fieldCount;
        return S_OK;
    }

    pLayout->m_ridFieldCur = RidFromToken(firstField);
    pLayout->m_ridFieldEnd = pLayout->m_ridFieldCur + fieldListCount;
    return S_OK;
}
STDMETHODIMP InternalMetadataImportRO::GetClassLayoutNext(
    MD_CLASS_LAYOUT *pLayout,
    mdFieldDef  *pfd,
    ULONG       *pulOffset)
{
    mdcursor_t fieldLayout;
    uint32_t fieldLayoutCount;
    if (!md_create_cursor(m_handle.get(), mdtid_FieldLayout, &fieldLayout, &fieldLayoutCount))
    {
        *pfd = mdFieldDefNil;
        return S_FALSE;
    }

    for (; pLayout->m_ridFieldCur < pLayout->m_ridFieldEnd; pLayout->m_ridFieldCur++)
    {
        mdcursor_t field;
        if (!md_token_to_cursor(m_handle.get(), TokenFromRid(pLayout->m_ridFieldCur, (mdtid_FieldPtr << 24)), &field)
            && !md_token_to_cursor(m_handle.get(), TokenFromRid(pLayout->m_ridFieldCur, mdtFieldDef), &field))
            return CLDB_E_FILE_CORRUPT;

        if (!md_resolve_indirect_cursor(field, &field))
            return CLDB_E_FILE_CORRUPT;

        if (md_find_row_from_cursor(fieldLayout, mdtFieldLayout_Field, pLayout->m_ridFieldCur, &fieldLayout))
        {
            uint32_t offset;
            if (!md_get_column_value_as_constant(fieldLayout, mdtFieldLayout_Offset, &offset))
                return CLDB_E_FILE_CORRUPT;
            *pulOffset = offset;

            if (!md_cursor_to_token(field, pfd))
                return CLDB_E_FILE_CORRUPT;

            pLayout->m_ridFieldCur++;
            return S_OK;
        }
    }
    *pfd = mdFieldDefNil;
    return S_FALSE;
}


STDMETHODIMP InternalMetadataImportRO::GetFieldMarshal(
    mdFieldDef  fd,
    PCCOR_SIGNATURE *pSigNativeType,
    ULONG       *pcbNativeType)
{
    mdcursor_t c;
    uint32_t count;
    if (!md_create_cursor(m_handle.get(), mdtid_FieldMarshal, &c, &count))
        return CLDB_E_RECORD_NOTFOUND;

    mdcursor_t field;
    if (!md_find_row_from_cursor(c, mdtFieldMarshal_Parent, fd, &field))
        return CLDB_E_RECORD_NOTFOUND;

    uint8_t const* sig;
    uint32_t sigLength;
    if (!md_get_column_value_as_blob(field, mdtFieldMarshal_NativeType, &sig, &sigLength))
        return CLDB_E_FILE_CORRUPT;

    *pSigNativeType = sig;
    *pcbNativeType = sigLength;

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::FindProperty(
    mdTypeDef   td,
    LPCSTR      szPropName,
    mdProperty  *pProp)
{
    mdcursor_t propertyMap;
    uint32_t propertyMapCount;
    if (!md_create_cursor(m_handle.get(), mdtid_PropertyMap, &propertyMap, &propertyMapCount))
        return CLDB_E_FILE_CORRUPT;

    if (!md_find_row_from_cursor(propertyMap, mdtPropertyMap_Parent, RidFromToken(td), &propertyMap))
        return CLDB_E_FILE_CORRUPT;

    mdcursor_t property;
    uint32_t numProperties;
    if (!md_get_column_value_as_range(propertyMap, mdtPropertyMap_PropertyList, &property, &numProperties))
        return CLDB_E_FILE_CORRUPT;

    for (uint32_t i = 0; i < numProperties; i++, md_cursor_next(&property))
    {
        mdcursor_t prop;
        if (!md_resolve_indirect_cursor(property, &prop))
            return CLDB_E_FILE_CORRUPT;

        LPCSTR name;
        if (!md_get_column_value_as_utf8(prop, mdtProperty_Name, &name))
            return CLDB_E_FILE_CORRUPT;

        if (strcmp(name, szPropName) == 0)
        {
            if (!md_cursor_to_token(prop, pProp))
                return CLDB_E_FILE_CORRUPT;

            return S_OK;
        }
    }
    return CLDB_E_RECORD_NOTFOUND;
}

STDMETHODIMP InternalMetadataImportRO::GetPropertyProps(
    mdProperty  prop,
    LPCSTR      *szProperty,
    DWORD       *pdwPropFlags,
    PCCOR_SIGNATURE *ppvSig,
    ULONG       *pcbSig)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), prop, &c))
        return CLDB_E_FILE_CORRUPT;

    if (ppvSig != nullptr)
    {
        uint8_t const* sig;
        uint32_t sigLength;
        if (!md_get_column_value_as_blob(c, mdtProperty_Type, &sig, &sigLength))
            return CLDB_E_FILE_CORRUPT;

        *ppvSig = sig;
        if (pcbSig != nullptr)
            *pcbSig = sigLength;
    }


    if (szProperty != nullptr
        && !md_get_column_value_as_utf8(c, mdtProperty_Name, szProperty))
        return CLDB_E_FILE_CORRUPT;

    if (pdwPropFlags != nullptr)
    {
        uint32_t flags;
        if (!md_get_column_value_as_constant(c, mdtProperty_Flags, &flags))
            return CLDB_E_FILE_CORRUPT;

        *pdwPropFlags = flags;
    }

    return S_OK;
}


STDMETHODIMP InternalMetadataImportRO::FindEvent(
    mdTypeDef   td,
    LPCSTR      szEventName,
    mdEvent     *pEvent)
{
    mdcursor_t eventMap;
    uint32_t eventMapCount;
    if (!md_create_cursor(m_handle.get(), mdtid_EventMap, &eventMap, &eventMapCount))
        return CLDB_E_FILE_CORRUPT;

    if (!md_find_row_from_cursor(eventMap, mdtEventMap_Parent, RidFromToken(td), &eventMap))
        return CLDB_E_FILE_CORRUPT;

    mdcursor_t event;
    uint32_t numEvents;
    if (!md_get_column_value_as_range(eventMap, mdtEventMap_EventList, &event, &numEvents))
        return CLDB_E_FILE_CORRUPT;

    for (uint32_t i = 0; i < numEvents; i++, md_cursor_next(&event))
    {
        mdcursor_t evt;
        if (!md_resolve_indirect_cursor(event, &evt))
            return CLDB_E_FILE_CORRUPT;

        LPCSTR name;
        if (!md_get_column_value_as_utf8(evt, mdtEvent_Name, &name))
            return CLDB_E_FILE_CORRUPT;

        if (strcmp(name, szEventName) == 0)
        {
            if (!md_cursor_to_token(evt, pEvent))
                return CLDB_E_FILE_CORRUPT;

            return S_OK;
        }
    }
    return CLDB_E_RECORD_NOTFOUND;
}

STDMETHODIMP InternalMetadataImportRO::GetEventProps(
    mdEvent     ev,
    LPCSTR      *pszEvent,
    DWORD       *pdwEventFlags,
    mdToken     *ptkEventType)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), ev, &c))
        return CLDB_E_FILE_CORRUPT;

    if (pdwEventFlags != nullptr)
    {
        uint32_t flags;
        if (!md_get_column_value_as_constant(c, mdtEvent_EventFlags, &flags))
            return CLDB_E_FILE_CORRUPT;

        *pdwEventFlags = flags;
    }

    if (pszEvent != nullptr
        && !md_get_column_value_as_utf8(c, mdtEvent_Name, pszEvent))
        return CLDB_E_FILE_CORRUPT;

    if (ptkEventType != nullptr
        && !md_get_column_value_as_token(c, mdtEvent_EventType, ptkEventType))
        return CLDB_E_FILE_CORRUPT;

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::FindAssociate(
    mdToken     evprop,
    DWORD       associate,
    mdMethodDef *pmd)
{
    mdcursor_t c;
    if (!md_create_cursor(m_handle.get(), mdtid_MethodSemantics, &c, NULL))
        return CLDB_E_FILE_CORRUPT;

    uint32_t numAssociatedMethods;
    md_range_result_t result = md_find_range_from_cursor(c, mdtMethodSemantics_Association, evprop, &c, &numAssociatedMethods);
    assert(result != MD_RANGE_NOT_SUPPORTED);
    if (result == MD_RANGE_NOT_FOUND)
        return CLDB_E_RECORD_NOTFOUND;

    bool checkParent = result == MD_RANGE_NOT_SUPPORTED;

    for (uint32_t i = 0; i < numAssociatedMethods; i++, md_cursor_next(&c))
    {
        if (checkParent)
        {
            mdToken parent;
            if (!md_get_column_value_as_token(c, mdtMethodSemantics_Association, &parent))
                return CLDB_E_FILE_CORRUPT;

            if (parent != evprop)
                continue;
        }

        uint32_t semantics;
        if (!md_get_column_value_as_constant(c, mdtMethodSemantics_Semantics, &semantics))
            return CLDB_E_FILE_CORRUPT;

        if (associate == semantics)
        {
            if (!md_get_column_value_as_token(c, mdtMethodSemantics_Method, pmd))
                return CLDB_E_FILE_CORRUPT;
            return S_OK;
        }
    }

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::EnumAssociateInit(
    mdToken     evprop,
    HENUMInternal *phEnum)
{
    HCORENUMImpl* impl = ToHCORENUMImpl(phEnum);
    return CreateEnumTokenRangeForSortedTableKey(m_handle.get(), mdtid_MethodSemantics, mdtMethodSemantics_Association, evprop, impl);
}

STDMETHODIMP InternalMetadataImportRO::GetAllAssociates(
    HENUMInternal *phEnum,
    ASSOCIATE_RECORD *pAssociateRec,
    ULONG       cAssociateRec)
{
    uint32_t count = EnumGetCount(phEnum);
    if (count != cAssociateRec)
        return E_INVALIDARG;

    HCORENUMImpl* impl = ToHCORENUMImpl(phEnum);
    for (uint32_t i = 0; i < cAssociateRec; i++)
    {
        mdToken tok;
        ULONG numRead;
        impl->ReadTokens(&tok, 1, &numRead);
        if (numRead != 1)
            return E_FAIL;

        mdcursor_t c;
        if (!md_token_to_cursor(m_handle.get(), tok, &c))
            return CLDB_E_FILE_CORRUPT;

        uint32_t semantics;
        if (!md_get_column_value_as_constant(c, mdtMethodSemantics_Semantics, &semantics))
            return CLDB_E_FILE_CORRUPT;

        pAssociateRec[i].m_dwSemantics = semantics;

        if (!md_get_column_value_as_token(c, mdtMethodSemantics_Method, &pAssociateRec[i].m_memberdef))
            return CLDB_E_FILE_CORRUPT;
    }

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetPermissionSetProps(
    mdPermission pm,
    DWORD       *pdwAction,
    void const  **ppvPermission,
    ULONG       *pcbPermission)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), pm, &c))
        return CLDB_E_FILE_CORRUPT;

    uint32_t action;
    if (!md_get_column_value_as_constant(c, mdtDeclSecurity_Action, &action))
        return CLDB_E_FILE_CORRUPT;

    *pdwAction = action;

    uint8_t const* permission;
    uint32_t permissionLength;
    if (!md_get_column_value_as_blob(c, mdtDeclSecurity_PermissionSet, &permission, &permissionLength))
        return CLDB_E_FILE_CORRUPT;

    *ppvPermission = permission;
    *pcbPermission = permissionLength;

    return S_OK;
}


STDMETHODIMP InternalMetadataImportRO::GetUserString(
    mdString stk,
    ULONG   *pchString,
    LPCWSTR *pwszUserString)
{
    if (TypeFromToken(stk) != mdtString || pchString == nullptr)
        return E_INVALIDARG;

    mduserstringcursor_t cursor = RidFromToken(stk);
    mduserstring_t string;
    uint32_t offset;
    if (!md_walk_user_string_heap(m_handle.get(), &cursor, &string, &offset))
        return CLDB_E_INDEX_NOTFOUND;

        // Strings in #US should have a trailing single byte.
    if (string.str_bytes % sizeof(WCHAR) == 0)
        return CLDB_E_FILE_CORRUPT;

    *pchString = string.str_bytes / sizeof(WCHAR);
    *pwszUserString = (LPCWSTR)string.str;
    return S_OK;
}


STDMETHODIMP InternalMetadataImportRO::GetPinvokeMap(
    mdToken     tk,
    DWORD       *pdwMappingFlags,
    LPCSTR      *pszImportName,
    mdModuleRef *pmrImportDLL)
{
    if (TypeFromToken(tk) != mdtMethodDef && TypeFromToken(tk) != mdtFieldDef)
        return E_INVALIDARG;

    mdcursor_t cursor;
    uint32_t count;
    mdcursor_t implRow;
    if (!md_create_cursor(m_handle.get(), mdtid_ImplMap, &cursor, &count)
        || !md_find_row_from_cursor(cursor, mdtImplMap_MemberForwarded, tk, &implRow))
    {
        return CLDB_E_RECORD_NOTFOUND;
    }

    if (pdwMappingFlags != nullptr)
    {
        uint32_t flags;
        if (!md_get_column_value_as_constant(implRow, mdtImplMap_MappingFlags, &flags))
            return CLDB_E_FILE_CORRUPT;
        *pdwMappingFlags = flags;
    }

    if (pmrImportDLL != nullptr
        && !md_get_column_value_as_token(implRow, mdtImplMap_ImportScope, pmrImportDLL))
        return CLDB_E_FILE_CORRUPT;

    if (pszImportName != nullptr
        && !md_get_column_value_as_utf8(implRow, mdtImplMap_ImportName, pszImportName))
        return CLDB_E_FILE_CORRUPT;
    return S_OK;
}


STDMETHODIMP InternalMetadataImportRO::ConvertTextSigToComSig(
    BOOL        fCreateTrIfNotFound,
    LPCSTR      pSignature,
    CQuickBytes *pqbNewSig,
    ULONG       *pcbCount)
{
    UNREFERENCED_PARAMETER(fCreateTrIfNotFound);
    UNREFERENCED_PARAMETER(pSignature);
    UNREFERENCED_PARAMETER(pqbNewSig);
    UNREFERENCED_PARAMETER(pcbCount);
    // Not implemented in CoreCLR.
    return E_NOTIMPL;
}


STDMETHODIMP InternalMetadataImportRO::GetAssemblyProps(
    mdAssembly  mda,
    const void  **ppbPublicKey,
    ULONG       *pcbPublicKey,
    ULONG       *pulHashAlgId,
    LPCSTR      *pszName,
    AssemblyMetaDataInternal *pMetaData,
    DWORD       *pdwAssemblyFlags)
{
    // Get properties from Assembly table.
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), mda, &c))
        return CLDB_E_FILE_CORRUPT;

    if (pulHashAlgId != nullptr)
    {
        uint32_t hashAlgId;
        if (!md_get_column_value_as_constant(c, mdtAssembly_HashAlgId, &hashAlgId))
            return CLDB_E_FILE_CORRUPT;

        *pulHashAlgId = hashAlgId;
    }

    if (pdwAssemblyFlags != nullptr)
    {
        uint32_t flags;
        if (!md_get_column_value_as_constant(c, mdtAssembly_Flags, &flags))
            return CLDB_E_FILE_CORRUPT;

        uint8_t const* publicKey;
        uint32_t publicKeyLength;
        if (!md_get_column_value_as_blob(c, mdtAssembly_PublicKey, &publicKey, &publicKeyLength))
            return CLDB_E_FILE_CORRUPT;

        if (publicKeyLength != 0)
            flags |= afPublicKey;

        *pdwAssemblyFlags = flags;
    }

    if (ppbPublicKey != nullptr)
    {
        uint8_t const* publicKey;
        uint32_t publicKeyLength;
        if (!md_get_column_value_as_blob(c, mdtAssembly_PublicKey, &publicKey, &publicKeyLength))
            return CLDB_E_FILE_CORRUPT;

        *ppbPublicKey = publicKey;
        *pcbPublicKey = publicKeyLength;
    }

    if (pszName != nullptr
        && !md_get_column_value_as_utf8(c, mdtAssembly_Name, pszName))
        return CLDB_E_FILE_CORRUPT;

    if (pMetaData)
    {
        uint32_t majorVersion;
        if (!md_get_column_value_as_constant(c, mdtAssembly_MajorVersion, &majorVersion))
            return CLDB_E_FILE_CORRUPT;

        uint32_t minorVersion;
        if (!md_get_column_value_as_constant(c, mdtAssembly_MinorVersion, &minorVersion))
            return CLDB_E_FILE_CORRUPT;

        uint32_t buildNumber;
        if (!md_get_column_value_as_constant(c, mdtAssembly_BuildNumber, &buildNumber))
            return CLDB_E_FILE_CORRUPT;

        uint32_t revisionNumber;
        if (!md_get_column_value_as_constant(c, mdtAssembly_RevisionNumber, &revisionNumber))
            return CLDB_E_FILE_CORRUPT;

        LPCSTR locale;
        if (!md_get_column_value_as_utf8(c, mdtAssembly_Culture, &locale))
            return CLDB_E_FILE_CORRUPT;

        pMetaData->usMajorVersion = (USHORT)majorVersion;
        pMetaData->usMinorVersion = (USHORT)minorVersion;
        pMetaData->usBuildNumber = (USHORT)buildNumber;
        pMetaData->usRevisionNumber = (USHORT)revisionNumber;
        pMetaData->szLocale = locale;
    }

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetAssemblyRefProps(
    mdAssemblyRef mdar,
    const void  **ppbPublicKeyOrToken,
    ULONG       *pcbPublicKeyOrToken,
    LPCSTR      *pszName,
    AssemblyMetaDataInternal *pMetaData,
    const void  **ppbHashValue,
    ULONG       *pcbHashValue,
    DWORD       *pdwAssemblyRefFlags)
{
    // Get properties from AssemblyRef table.
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), mdar, &c))
        return CLDB_E_FILE_CORRUPT;

    if (pdwAssemblyRefFlags != nullptr)
    {
        uint32_t flags;
        if (!md_get_column_value_as_constant(c, mdtAssemblyRef_Flags, &flags))
            return CLDB_E_FILE_CORRUPT;

        *pdwAssemblyRefFlags = flags;
    }

    if (ppbPublicKeyOrToken != nullptr)
    {
        uint8_t const* publicKeyOrToken;
        uint32_t publicKeyOrTokenLength;
        if (!md_get_column_value_as_blob(c, mdtAssemblyRef_PublicKeyOrToken, &publicKeyOrToken, &publicKeyOrTokenLength))
            return CLDB_E_FILE_CORRUPT;

        *ppbPublicKeyOrToken = publicKeyOrToken;
        *pcbPublicKeyOrToken = publicKeyOrTokenLength;
    }

    if (pszName != nullptr
        && !md_get_column_value_as_utf8(c, mdtAssemblyRef_Name, pszName))
        return CLDB_E_FILE_CORRUPT;

    if (pMetaData)
    {
        uint32_t majorVersion;
        if (!md_get_column_value_as_constant(c, mdtAssemblyRef_MajorVersion, &majorVersion))
            return CLDB_E_FILE_CORRUPT;

        uint32_t minorVersion;
        if (!md_get_column_value_as_constant(c, mdtAssemblyRef_MinorVersion, &minorVersion))
            return CLDB_E_FILE_CORRUPT;

        uint32_t buildNumber;
        if (!md_get_column_value_as_constant(c, mdtAssemblyRef_BuildNumber, &buildNumber))
            return CLDB_E_FILE_CORRUPT;

        uint32_t revisionNumber;
        if (!md_get_column_value_as_constant(c, mdtAssemblyRef_RevisionNumber, &revisionNumber))
            return CLDB_E_FILE_CORRUPT;

        LPCSTR locale;
        if (!md_get_column_value_as_utf8(c, mdtAssemblyRef_Culture, &locale))
            return CLDB_E_FILE_CORRUPT;

        pMetaData->usMajorVersion = (USHORT)majorVersion;
        pMetaData->usMinorVersion = (USHORT)minorVersion;
        pMetaData->usBuildNumber = (USHORT)buildNumber;
        pMetaData->usRevisionNumber = (USHORT)revisionNumber;
        pMetaData->szLocale = locale;
    }

    if (ppbHashValue != nullptr)
    {
        uint8_t const* hashValue;
        uint32_t hashValueLength;
        if (!md_get_column_value_as_blob(c, mdtAssemblyRef_HashValue, &hashValue, &hashValueLength))
            return CLDB_E_FILE_CORRUPT;

        *ppbHashValue = hashValue;
        *pcbHashValue = hashValueLength;
    }

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetFileProps(
    mdFile      mdf,
    LPCSTR      *pszName,
    const void  **ppbHashValue,
    ULONG       *pcbHashValue,
    DWORD       *pdwFileFlags)
{
    // Get properties from File table.
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), mdf, &c))
        return CLDB_E_FILE_CORRUPT;

    if (pdwFileFlags != nullptr)
    {
        uint32_t flags;
        if (!md_get_column_value_as_constant(c, mdtFile_Flags, &flags))
            return CLDB_E_FILE_CORRUPT;

        *pdwFileFlags = flags;
    }

    if (pszName != nullptr
        && !md_get_column_value_as_utf8(c, mdtFile_Name, pszName))
        return CLDB_E_FILE_CORRUPT;

    if (ppbHashValue != nullptr)
    {
        uint8_t const* hashValue;
        uint32_t hashValueLength;
        if (!md_get_column_value_as_blob(c, mdtFile_HashValue, &hashValue, &hashValueLength))
            return CLDB_E_FILE_CORRUPT;

        *ppbHashValue = hashValue;
        *pcbHashValue = hashValueLength;
    }

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetExportedTypeProps(
    mdExportedType   mdct,
    LPCSTR      *pszNamespace,
    LPCSTR      *pszName,
    mdToken     *ptkImplementation,
    mdTypeDef   *ptkTypeDef,
    DWORD       *pdwExportedTypeFlags)
{
    // Get properties from ExportedType table.
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), mdct, &c))
        return CLDB_E_FILE_CORRUPT;

    if (pdwExportedTypeFlags != nullptr)
    {
        uint32_t flags;
        if (!md_get_column_value_as_constant(c, mdtExportedType_Flags, &flags))
            return CLDB_E_FILE_CORRUPT;

        *pdwExportedTypeFlags = flags;
    }

    if (pszNamespace != nullptr
        && !md_get_column_value_as_utf8(c, mdtExportedType_TypeNamespace, pszNamespace))
        return CLDB_E_FILE_CORRUPT;

    if (pszName != nullptr
        && !md_get_column_value_as_utf8(c, mdtExportedType_TypeName, pszName))
        return CLDB_E_FILE_CORRUPT;

    if (ptkImplementation != nullptr
        && !md_get_column_value_as_token(c, mdtExportedType_Implementation, ptkImplementation))
        return CLDB_E_FILE_CORRUPT;

    if (ptkTypeDef != nullptr)
    {
        uint32_t typeDefId;
        if (!md_get_column_value_as_constant(c, mdtExportedType_TypeDefId, &typeDefId))
            return CLDB_E_FILE_CORRUPT;

        *ptkTypeDef = typeDefId;
    }

    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetManifestResourceProps(
    mdManifestResource  mdmr,
    LPCSTR      *pszName,
    mdToken     *ptkImplementation,
    DWORD       *pdwOffset,
    DWORD       *pdwResourceFlags)
{
    // Get properties from ManifestResource table.
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), mdmr, &c))
        return CLDB_E_FILE_CORRUPT;

    if (pdwResourceFlags != nullptr)
    {
        uint32_t flags;
        if (!md_get_column_value_as_constant(c, mdtManifestResource_Flags, &flags))
            return CLDB_E_FILE_CORRUPT;

        *pdwResourceFlags = flags;
    }

    if (pszName != nullptr
        && !md_get_column_value_as_utf8(c, mdtManifestResource_Name, pszName))
        return CLDB_E_FILE_CORRUPT;

    if (ptkImplementation != nullptr
        && !md_get_column_value_as_token(c, mdtManifestResource_Implementation, ptkImplementation))
        return CLDB_E_FILE_CORRUPT;

    if (pdwOffset != nullptr)
    {
        uint32_t offset;
        if (!md_get_column_value_as_constant(c, mdtManifestResource_Offset, &offset))
            return CLDB_E_FILE_CORRUPT;

        *pdwOffset = offset;
    }

    return S_OK;
}
STDMETHODIMP InternalMetadataImportRO::FindExportedTypeByName(
    LPCSTR      szNamespace,
    LPCSTR      szName,
    mdExportedType   tkEnclosingType,
    mdExportedType   *pmct)
{
    if (szName == nullptr)
        return E_INVALIDARG;

    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(m_handle.get(), mdtid_ExportedType, &cursor, &count))
        return CLDB_E_RECORD_NOTFOUND;

    for (uint32_t i = 0; i < count; md_cursor_next(&cursor), i++)
    {
        mdToken implementation;
        if (!md_get_column_value_as_token(cursor, mdtExportedType_Implementation, &implementation))
            return CLDB_E_FILE_CORRUPT;

        // Handle the case of nested vs. non-nested classes
        if (TypeFromToken(implementation) == CorTokenType::mdtExportedType && !IsNilToken(implementation))
        {
            // Current ExportedType being looked at is a nested type, so
            // comparing the implementation token.
            if (implementation != tkEnclosingType)
                continue;
        }
        else if (TypeFromToken(tkEnclosingType) == mdtExportedType
                && !IsNilToken(tkEnclosingType))
        {
            // ExportedType passed in is nested but the current ExportedType is not.
            continue;
        }

        char const* recordNspace;
        if (!md_get_column_value_as_utf8(cursor, mdtExportedType_TypeNamespace, &recordNspace))
            return CLDB_E_FILE_CORRUPT;

        if (::strcmp(recordNspace, szNamespace) != 0)
            continue;

        char const* recordName;
        if (!md_get_column_value_as_utf8(cursor, mdtExportedType_TypeName, &recordName))
            return CLDB_E_FILE_CORRUPT;

        if (::strcmp(recordName, szName) != 0)
            continue;

        if (!md_cursor_to_token(cursor, pmct))
            return CLDB_E_FILE_CORRUPT;
        return S_OK;
    }
    return CLDB_E_RECORD_NOTFOUND;
}
STDMETHODIMP InternalMetadataImportRO::FindManifestResourceByName(
    LPCSTR      szName,
    mdManifestResource *pmmr)
{
    if (szName == nullptr)
        return E_INVALIDARG;

    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(m_handle.get(), mdtid_ManifestResource, &cursor, &count))
        return CLDB_E_RECORD_NOTFOUND;

    for (uint32_t i = 0; i < count; md_cursor_next(&cursor), i++)
    {
        mdManifestResource token;
        if (!md_cursor_to_token(cursor, &token))
            return CLDB_E_FILE_CORRUPT;

        char const* name;
        if (!md_get_column_value_as_utf8(cursor, mdtManifestResource_Name, &name))
            return CLDB_E_FILE_CORRUPT;

        if (::strcmp(name, szName) == 0)
        {
            *pmmr = token;
            return S_OK;
        }
    }
    return CLDB_E_RECORD_NOTFOUND;
}
STDMETHODIMP InternalMetadataImportRO::GetAssemblyFromScope(
    mdAssembly  *ptkAssembly)
{
    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(m_handle.get(), mdtid_Assembly, &cursor, &count))
        return CLDB_E_RECORD_NOTFOUND;
    if (!md_cursor_to_token(cursor, ptkAssembly))
        return CLDB_E_FILE_CORRUPT;
    return S_OK;
}
namespace
{
    // See TypeSpec definition at II.23.2.14
    HRESULT ExtractTypeDefRefFromSpec(uint8_t const* specBlob, uint32_t specBlobLen, mdToken& tk)
    {
        assert(specBlob != nullptr);
        if (specBlobLen == 0)
            return COR_E_BADIMAGEFORMAT;

        PCCOR_SIGNATURE sig = specBlob;
        PCCOR_SIGNATURE sigEnd = specBlob + specBlobLen;

        ULONG data;
        sig += CorSigUncompressData(sig, &data);

        while (sig < sigEnd
            && (CorIsModifierElementType((CorElementType)data)
                || data == ELEMENT_TYPE_GENERICINST))
        {
            sig += CorSigUncompressData(sig, &data);
        }

        if (sig >= sigEnd)
            return COR_E_BADIMAGEFORMAT;

        if (data == ELEMENT_TYPE_VALUETYPE || data == ELEMENT_TYPE_CLASS)
        {
            if (mdTokenNil == CorSigUncompressToken(sig, &tk))
                return COR_E_BADIMAGEFORMAT;
            return S_OK;
        }

        tk = mdTokenNil;
        return S_FALSE;
    }

    HRESULT ResolveTypeDefRefSpecToName(mdcursor_t cursor, char const** nspace, char const** name)
    {
        assert(nspace != nullptr && name != nullptr);

        HRESULT hr;
        mdToken typeTk;
        if (!md_cursor_to_token(cursor, &typeTk))
            return E_FAIL;

        uint8_t const* specBlob;
        uint32_t specBlobLen;
        uint32_t tokenType = TypeFromToken(typeTk);
        while (tokenType == mdtTypeSpec)
        {
            if (!md_get_column_value_as_blob(cursor, mdtTypeSpec_Signature, &specBlob, &specBlobLen))
                return CLDB_E_FILE_CORRUPT;

            RETURN_IF_FAILED(ExtractTypeDefRefFromSpec(specBlob, specBlobLen, typeTk));
            if (typeTk == mdTokenNil)
                return S_FALSE;

            if (!md_token_to_cursor(md_extract_handle_from_cursor(cursor), typeTk, &cursor))
                return CLDB_E_FILE_CORRUPT;
            tokenType = TypeFromToken(typeTk);
        }

        switch (tokenType)
        {
        case mdtTypeDef:
            return (md_get_column_value_as_utf8(cursor, mdtTypeDef_TypeNamespace, nspace)
                && md_get_column_value_as_utf8(cursor, mdtTypeDef_TypeName, name))
                ? S_OK
                : CLDB_E_FILE_CORRUPT;
        case mdtTypeRef:
            return (md_get_column_value_as_utf8(cursor, mdtTypeRef_TypeNamespace, nspace)
                && md_get_column_value_as_utf8(cursor, mdtTypeRef_TypeName, name))
                ? S_OK
                : CLDB_E_FILE_CORRUPT;
        default:
            assert(!"Unexpected token in ResolveTypeDefRefSpecToName");
            return E_FAIL;
        }
    }
}

STDMETHODIMP InternalMetadataImportRO::GetCustomAttributeByName(
    mdToken     tkObj,
    LPCSTR     szName,
    const void  **ppData,
    ULONG       *pcbData)
{
    if (szName == nullptr)
        return E_INVALIDARG;

    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(m_handle.get(), mdtid_CustomAttribute, &cursor, &count))
        return CLDB_E_RECORD_NOTFOUND;

    mdcursor_t custAttrCurr;
    uint32_t custAttrCount;
    md_range_result_t result = md_find_range_from_cursor(cursor, mdtCustomAttribute_Parent, tkObj, &custAttrCurr, &custAttrCount);
    assert(result != MD_RANGE_NOT_SUPPORTED);
    if (result == MD_RANGE_NOT_FOUND)
    {
        if (ppData != nullptr)
        {
            *ppData = nullptr;
            *pcbData = 0;
        }
        return S_FALSE;
    }

    HRESULT hr;
    char const* nspace;
    char const* name;

    bool checkParent = result == MD_RANGE_NOT_SUPPORTED;
    mdcursor_t type;
    mdcursor_t tgtType;
    mdToken typeTk;
    size_t len;
    char const* curr;
    for (uint32_t i = 0; i < custAttrCount; (void)md_cursor_next(&custAttrCurr), ++i)
    {
        if (checkParent)
        {
            mdToken parent;
            if (!md_get_column_value_as_token(custAttrCurr, mdtCustomAttribute_Parent, &parent))
                return CLDB_E_FILE_CORRUPT;

            if (parent != tkObj)
                continue;
        }

        if (!md_get_column_value_as_cursor(custAttrCurr, mdtCustomAttribute_Type, &type))
            return CLDB_E_FILE_CORRUPT;

        // Cursor was returned so must be valid.
        (void)md_cursor_to_token(type, &typeTk);

        // Resolve the cursor based on its type.
        switch (TypeFromToken(typeTk))
        {
        case mdtMethodDef:
            if (!md_find_cursor_of_range_element(type, &tgtType))
                return CLDB_E_FILE_CORRUPT;
            break;
        case mdtMemberRef:
            if (!md_get_column_value_as_cursor(type, mdtMemberRef_Class, &tgtType))
                return CLDB_E_FILE_CORRUPT;
            break;
        default:
            assert(!"Unexpected token in GetCustomAttributeByName");
            return COR_E_BADIMAGEFORMAT;
        }

        if (SUCCEEDED(hr = ResolveTypeDefRefSpecToName(tgtType, &nspace, &name)))
        {
            curr = szName;
            if (nspace[0] != '\0')
            {
                len = ::strlen(nspace);
                if (0 != ::strncmp(szName, nspace, len))
                    continue;
                curr += len;

                // Check for overrun and next character
                size_t szNameLen = ::strlen(szName);
                if (szNameLen <= len || curr[0] != '.')
                    continue;
                curr += 1;
            }

            if (0 == ::strcmp(curr, name))
            {
                if (ppData != nullptr)
                {
                    uint8_t const* data;
                    uint32_t dataLen;
                    if (!md_get_column_value_as_blob(custAttrCurr, mdtCustomAttribute_Value, &data, &dataLen))
                        return CLDB_E_FILE_CORRUPT;
                    *ppData = data;
                    *pcbData = dataLen;
                }
                return S_OK;
            }
        }
        RETURN_IF_FAILED(hr);
    }

    if (ppData)
    {
        *ppData = nullptr;
        *pcbData = 0;
    }
    return S_FALSE;
}

STDMETHODIMP InternalMetadataImportRO::GetTypeSpecFromToken(
    mdTypeSpec typespec,
    PCCOR_SIGNATURE *ppvSig,
    ULONG       *pcbSig)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), typespec, &c))
        return CLDB_E_FILE_CORRUPT;
    uint8_t const* sig;
    uint32_t sigLength;
    if (!md_get_column_value_as_blob(c, mdtTypeSpec_Signature, &sig, &sigLength))
        return CLDB_E_FILE_CORRUPT;
    *ppvSig = sig;
    *pcbSig = sigLength;
    return S_OK;
}
STDMETHODIMP InternalMetadataImportRO::SetUserContextData(
    IUnknown    *pIUnk)
{
    UNREFERENCED_PARAMETER(pIUnk);
    // Unimplemented in CoreCLR
    return E_NOTIMPL;
}
STDMETHODIMP_(BOOL) InternalMetadataImportRO::IsValidToken(
    mdToken     tk)
{
    mdcursor_t c;
    return md_token_to_cursor(m_handle.get(), tk, &c);
}
STDMETHODIMP InternalMetadataImportRO::TranslateSigWithScope(
    IMDInternalImport *pAssemImport,
    const void  *pbHashValue,
    ULONG       cbHashValue,
    PCCOR_SIGNATURE pbSigBlob,
    ULONG       cbSigBlob,
    IMetaDataAssemblyEmit *pAssemEmit,
    IMetaDataEmit *emit,
    CQuickBytes *pqkSigEmit,
    ULONG       *pcbSig)
{
    UNREFERENCED_PARAMETER(pAssemImport);
    UNREFERENCED_PARAMETER(pbHashValue);
    UNREFERENCED_PARAMETER(cbHashValue);
    UNREFERENCED_PARAMETER(pbSigBlob);
    UNREFERENCED_PARAMETER(cbSigBlob);
    UNREFERENCED_PARAMETER(pAssemEmit);
    UNREFERENCED_PARAMETER(emit);
    UNREFERENCED_PARAMETER(pqkSigEmit);
    UNREFERENCED_PARAMETER(pcbSig);

    // Requires Emit support
    return E_NOTIMPL;
}
STDMETHODIMP_(IMetaModelCommon*) InternalMetadataImportRO::GetMetaModelCommon()
{
    // Unused in the import-only paths in CoreCLR,
    // so defer implementation for now.
    return nullptr;
}
STDMETHODIMP_(IUnknown *) InternalMetadataImportRO::GetCachedPublicInterface(BOOL fWithLock)
{
    UNREFERENCED_PARAMETER(fWithLock);
    return nullptr;
}
__checkReturn
STDMETHODIMP InternalMetadataImportRO::SetCachedPublicInterface(IUnknown *pUnk)
{
    UNREFERENCED_PARAMETER(pUnk);
    return CLDB_E_FILE_CORRUPT;
}
STDMETHODIMP_(minipal_rwlock*) InternalMetadataImportRO::GetReaderWriterLock()
{
    return nullptr;
}
__checkReturn
STDMETHODIMP InternalMetadataImportRO::SetReaderWriterLock(minipal_rwlock * pLock)
{
    UNREFERENCED_PARAMETER(pLock);
    return S_OK;
}
STDMETHODIMP_(mdModule) InternalMetadataImportRO::GetModuleFromScope()
{
    return MD_MODULE_TOKEN;
}

namespace
{
    template<typename TComparer>
    STDMETHODIMP FindMethodDef(
        mdhandle_t handle,
        mdTypeDef   classdef,
        LPCSTR      szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        TComparer   comparer,
        mdMethodDef *pmd)
    {
        if (TypeFromToken(classdef) != mdtTypeDef && classdef != mdTokenNil)
            return E_INVALIDARG;

        if (classdef == mdTypeDefNil || classdef == mdTokenNil)
            classdef = MD_GLOBAL_PARENT_TOKEN;

        mdcursor_t typedefCursor;
        if (!md_token_to_cursor(handle, classdef, &typedefCursor))
            return CLDB_E_INDEX_NOTFOUND;

        mdcursor_t methodCursor;
        uint32_t count;
        if (!md_get_column_value_as_range(typedefCursor, mdtTypeDef_MethodList, &methodCursor, &count))
            return CLDB_E_FILE_CORRUPT;

        inline_span<uint8_t> methodDefSig;
        if (cbSigBlob != 0)
            GetMethodDefSigFromMethodRefSig({(uint8_t*)pvSigBlob, (size_t)cbSigBlob}, methodDefSig);

        for (uint32_t i = 0; i < count; (void)md_cursor_next(&methodCursor), ++i)
        {
            mdcursor_t method;
            if (!md_resolve_indirect_cursor(methodCursor, &method))
                return CLDB_E_FILE_CORRUPT;

            char const* methodName;
            if (!md_get_column_value_as_utf8(method, mdtMethodDef_Name, &methodName))
                return CLDB_E_FILE_CORRUPT;
            if (::strcmp(methodName, szName) != 0)
                continue;

            if (cbSigBlob != 0)
            {
                uint8_t const* sig;
                uint32_t sigLen;
                if (!md_get_column_value_as_blob(method, mdtMethodDef_Signature, &sig, &sigLen))
                    return CLDB_E_FILE_CORRUPT;
                if (sigLen != methodDefSig.size()
                    || (comparer(sig, sigLen, methodDefSig.data(), (DWORD)methodDefSig.size()) == FALSE))
                {
                    continue;
                }
            }

            // PERF: Read the flags at the end. Even though the flag check is cheaper than
            // the strcmp, we'll almost never hit this code path as "Private scope" is almost never used.
            // As a result, the extra memory read of the flags is an additional cost that we can avoid
            // in the "negative" case.
            uint32_t flags;
            if (!md_get_column_value_as_constant(method, mdtMethodDef_Flags, &flags))
                return CLDB_E_FILE_CORRUPT;

            // Ignore PrivateScope methods. By the spec, they can only be referred to by a MethodDef token
            // and cannot be discovered in any other way.
            if (IsMdPrivateScope(flags))
                continue;

            if (!md_cursor_to_token(method, pmd))
                return CLDB_E_FILE_CORRUPT;
            return S_OK;
        }
        return CLDB_E_RECORD_NOTFOUND;
    }

    BOOL CompareSignatures(PCCOR_SIGNATURE sig1, DWORD sig1Length, PCCOR_SIGNATURE sig2, DWORD sig2Length)
    {
        if (sig1Length != sig2Length || memcmp(sig1, sig2, sig2Length))
            return FALSE;
        else
            return TRUE;
    }
}

STDMETHODIMP InternalMetadataImportRO::FindMethodDef(
    mdTypeDef   classdef,
    LPCSTR      szName,
    PCCOR_SIGNATURE pvSigBlob,
    ULONG       cbSigBlob,
    mdMethodDef *pmd)
{
   return ::FindMethodDef(
    m_handle.get(),
    classdef,
    szName,
    pvSigBlob,
    cbSigBlob,
    CompareSignatures,
    pmd);
}

STDMETHODIMP InternalMetadataImportRO::FindMethodDefUsingCompare(
    mdTypeDef   classdef,
    LPCSTR      szName,
    PCCOR_SIGNATURE pvSigBlob,
    ULONG       cbSigBlob,
    PSIGCOMPARE pSignatureCompare,
    void*       pSignatureArgs,
    mdMethodDef *pmd)
{
    return ::FindMethodDef(
        m_handle.get(),
        classdef,
        szName,
        pvSigBlob,
        cbSigBlob,
        [pSignatureCompare, pSignatureArgs](PCCOR_SIGNATURE sig1, DWORD sig1Length, PCCOR_SIGNATURE sig2, DWORD sig2Length)
        {
            if (pSignatureCompare == nullptr)
            {
                return 0;
            }
            return pSignatureCompare(sig1, sig1Length, sig2, sig2Length, pSignatureArgs);
        },
        pmd);
}

STDMETHODIMP InternalMetadataImportRO::GetFieldOffset(
    mdFieldDef  fd,
    ULONG       *pulOffset)
{
    mdcursor_t fieldLayout;
    uint32_t fieldLayoutCount;
    if (!md_create_cursor(m_handle.get(), mdtid_FieldLayout, &fieldLayout, &fieldLayoutCount))
        return S_FALSE;

    mdcursor_t field;
    if (!md_token_to_cursor(m_handle.get(), fd, &field))
        return CLDB_E_FILE_CORRUPT;

    if (!md_find_row_from_cursor(fieldLayout, mdtFieldLayout_Field, RidFromToken(fd), &fieldLayout))
        return S_FALSE;

    uint32_t offset;
    if (!md_get_column_value_as_constant(fieldLayout, mdtFieldLayout_Offset, &offset))
        return CLDB_E_FILE_CORRUPT;
    *pulOffset = offset;
    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetMethodSpecProps(
    mdMethodSpec ms,
    mdToken *tkParent,
    PCCOR_SIGNATURE *ppvSigBlob,
    ULONG       *pcbSigBlob)
{
    // Get MethodSpec props from MethodSpec table.
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), ms, &c))
        return CLDB_E_FILE_CORRUPT;

    if (!md_get_column_value_as_token(c, mdtMethodSpec_Method, tkParent))
        return CLDB_E_FILE_CORRUPT;

    uint8_t const* sig;
    uint32_t sigLength;

    if (!md_get_column_value_as_blob(c, mdtMethodSpec_Instantiation, &sig, &sigLength))
        return CLDB_E_FILE_CORRUPT;

    *ppvSigBlob = sig;
    *pcbSigBlob = sigLength;

    return S_OK;
}
STDMETHODIMP InternalMetadataImportRO::GetTableInfoWithIndex(
    ULONG      index,
    void       **pTable,
    void       **pTableSize)
{
    UNREFERENCED_PARAMETER(index);
    UNREFERENCED_PARAMETER(pTable);
    UNREFERENCED_PARAMETER(pTableSize);

    // Requires exposing table info
    // Unused by CoreCLR
    return E_NOTIMPL;
}
STDMETHODIMP InternalMetadataImportRO::ApplyEditAndContinue(
    void        *pDeltaMD,
    ULONG       cbDeltaMD,
    IMDInternalImport **ppv)
{
    UNREFERENCED_PARAMETER(pDeltaMD);
    UNREFERENCED_PARAMETER(cbDeltaMD);
    UNREFERENCED_PARAMETER(ppv);
    // Requires Emit support
    return E_NOTIMPL;
}


STDMETHODIMP InternalMetadataImportRO::GetGenericParamProps(
    mdGenericParam rd,
    ULONG* pulSequence,
    DWORD* pdwAttr,
    mdToken *ptOwner,
    DWORD *reserved,
    LPCSTR *szName)
{
    UNREFERENCED_PARAMETER(reserved);
    // Get props from GenericParam table.
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), rd, &c))
        return CLDB_E_FILE_CORRUPT;

    if (pulSequence != nullptr)
    {
        uint32_t sequence;
        if (!md_get_column_value_as_constant(c, mdtGenericParam_Number, &sequence))
            return CLDB_E_FILE_CORRUPT;
        *pulSequence = sequence;
    }

    if (pdwAttr != nullptr)
    {
        uint32_t flags;
        if (!md_get_column_value_as_constant(c, mdtGenericParam_Flags, &flags))
            return CLDB_E_FILE_CORRUPT;
        *pdwAttr = flags;
    }

    if (ptOwner != nullptr
        && !md_get_column_value_as_token(c, mdtGenericParam_Owner, ptOwner))
        return CLDB_E_FILE_CORRUPT;

    if (szName != nullptr
        && !md_get_column_value_as_utf8(c, mdtGenericParam_Name, szName))
        return CLDB_E_FILE_CORRUPT;

    return S_OK;
}
STDMETHODIMP InternalMetadataImportRO::GetGenericParamConstraintProps(
    mdGenericParamConstraint rd,
    mdGenericParam *ptGenericParam,
    mdToken      *ptkConstraintType)
{
    // Get props from GenericParamConstraint table.
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), rd, &c))
        return CLDB_E_FILE_CORRUPT;

    if (ptGenericParam != nullptr
        && !md_get_column_value_as_token(c, mdtGenericParamConstraint_Owner, ptGenericParam))
        return CLDB_E_FILE_CORRUPT;

    if (ptkConstraintType != nullptr
        && !md_get_column_value_as_token(c, mdtGenericParamConstraint_Constraint, ptkConstraintType))
        return CLDB_E_FILE_CORRUPT;

    return S_OK;
}



STDMETHODIMP InternalMetadataImportRO::GetVersionString(
    LPCSTR      *pVer)
{
    char const* versionString = md_get_version_string(m_handle.get());
    if (versionString == nullptr)
        versionString = "";

    *pVer = versionString;
    return S_OK;
}

STDMETHODIMP InternalMetadataImportRO::GetTypeDefRefTokenInTypeSpec(
    mdTypeSpec  tkTypeSpec,
    mdToken    *tkEnclosedToken)
{
    mdcursor_t spec;
    if (!md_token_to_cursor(m_handle.get(), tkTypeSpec, &spec))
        return CLDB_E_FILE_CORRUPT;

    uint8_t const* specData;
    uint32_t specLen;
    if (!md_get_column_value_as_blob(spec, mdtTypeSpec_Signature, &specData, &specLen))
        return CLDB_E_FILE_CORRUPT;

    return ExtractTypeDefRefFromSpec(specData, specLen, *tkEnclosedToken);
}

STDMETHODIMP_(DWORD) InternalMetadataImportRO::GetMetadataStreamVersion()
{
    // We only support the V1.0 or V2.0 version of the metadata format,
    // and V1 is forward compatible with V2, so we always can say that
    // the metadata is in the V2 format.
    return 0x20000;
}

STDMETHODIMP InternalMetadataImportRO::GetNameOfCustomAttribute(
    mdCustomAttribute mdAttribute,
    LPCSTR          *pszNamespace,
    LPCSTR          *pszName)
{
    mdcursor_t c;
    if (!md_token_to_cursor(m_handle.get(), mdAttribute, &c))
        return CLDB_E_FILE_CORRUPT;

    mdcursor_t attrConstructor;
    if (!md_get_column_value_as_cursor(c, mdtCustomAttribute_Type, &attrConstructor))
        return CLDB_E_FILE_CORRUPT;

    mdToken ctorToken;
    if (!md_cursor_to_token(attrConstructor, &ctorToken))
        return CLDB_E_FILE_CORRUPT;

    mdcursor_t type;
    switch (TypeFromToken(ctorToken))
    {
        case mdtMethodDef:
            if (!md_find_cursor_of_range_element(attrConstructor, &type))
                return CLDB_E_FILE_CORRUPT;
            break;
        case mdtMemberRef:
            if (!md_get_column_value_as_cursor(attrConstructor, mdtMemberRef_Class, &type))
                return CLDB_E_FILE_CORRUPT;
            break;
        default:
            return COR_E_BADIMAGEFORMAT;
    }

    return ResolveTypeDefRefSpecToName(type, pszNamespace, pszName);
}

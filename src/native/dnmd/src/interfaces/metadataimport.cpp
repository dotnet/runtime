#include <cassert>

#include "pal.hpp"
#include "metadataimportro.hpp"
#include "hcorenum.hpp"
#include "signatures.hpp"
#include <cstring>

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

mdhandle_t MetadataImportRO::MetaData()
{
    return _md_ptr.get();
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
    *pulCount = enumImpl == nullptr
        ? 0
        : enumImpl->Count();
    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::ResetEnum(HCORENUM hEnum, ULONG ulPos)
{
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(hEnum);
    return enumImpl == nullptr
        ? S_OK
        : enumImpl->Reset(ulPos);
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
            return CLDB_E_RECORD_NOTFOUND;

        HCORENUMImpl* enumImpl;
        RETURN_IF_FAILED(HCORENUMImpl::CreateTableEnum(1, &enumImpl));
        HCORENUMImpl::InitTableEnum(*enumImpl, 0, cursor, rows);
        *pEnumImpl = enumImpl;
        return S_OK;
    }

    struct TokenRangeFilter final
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
            HCORENUMImpl::InitTableEnum(*enumImpl, 0, begin, count);
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
                mdcursor_t target;
                if (!md_resolve_indirect_cursor(curr, &target))
                    return CLDB_E_FILE_CORRUPT;
                if (1 != md_get_column_value_as_utf8(target, filter->FilterColumn, 1, &toMatch))
                    return CLDB_E_FILE_CORRUPT;

                if (0 == ::strcmp(toMatch, cvt))
                {
                    (void)md_cursor_to_token(target, &matchedTk);
                    RETURN_IF_FAILED(HCORENUMImpl::AddToDynamicEnum(*enumImpl, matchedTk));
                }
                (void)md_cursor_next(&curr);
            }

            enumImpl = cleanup.release();
        }

        *pEnumImpl = enumImpl;
        return S_OK;
    }

    HRESULT CreateEnumTokenRangeForSortedTableKey(
        mdhandle_t mdhandle,
        mdtable_id_t table,
        col_index_t keyColumn,
        mdToken token,
        HCORENUMImpl** pEnumImpl)
    {
        HRESULT hr;
        mdcursor_t cursor;
        uint32_t tableCount;
        if (!md_create_cursor(mdhandle, table, &cursor, &tableCount))
            return CLDB_E_INDEX_NOTFOUND;

        mdcursor_t begin;
        uint32_t count;
        md_range_result_t result = md_find_range_from_cursor(cursor, keyColumn, token, &begin, &count);

        if (result == MD_RANGE_NOT_FOUND)
        {
            return HCORENUMImpl::CreateDynamicEnum(pEnumImpl);
        }
        else if (result == MD_RANGE_FOUND)
        {
            HCORENUMImpl* enumImpl;
            RETURN_IF_FAILED(HCORENUMImpl::CreateTableEnum(1, &enumImpl));
            HCORENUMImpl::InitTableEnum(*enumImpl, 0, begin, count);
            *pEnumImpl = enumImpl;
            return S_OK;
        }
        else
        {
            // Unsorted so we need to search across the entire table
            HCORENUMImpl* enumImpl;
            RETURN_IF_FAILED(HCORENUMImpl::CreateDynamicEnum(&enumImpl));
            HCORENUMImpl_ptr cleanup{ enumImpl };
            mdcursor_t curr = cursor;
            uint32_t currCount = tableCount;

            // Read in for matching in bulk
            mdToken matchedGroup[64];
            uint32_t i = 0;
            while (i < currCount)
            {
                int32_t read = md_get_column_value_as_token(curr, keyColumn, ARRAY_SIZE(matchedGroup), matchedGroup);
                if (read == 0)
                    break;

                assert(read > 0);
                for (int32_t j = 0; j < read; ++j)
                {
                    if (matchedGroup[j] == token)
                    {
                        mdToken matchedTk;
                        if (!md_cursor_to_token(curr, &matchedTk))
                            return CLDB_E_FILE_CORRUPT;
                        RETURN_IF_FAILED(HCORENUMImpl::AddToDynamicEnum(*enumImpl, matchedTk));
                    }
                    (void)md_cursor_next(&curr);
                }
                i += read;
            }

            *pEnumImpl = cleanup.release();
            return S_OK;
        }
    }

    HRESULT ConvertAndReturnStringOutput(
        _In_z_ char const* str,
        _Out_writes_to_opt_(cchBuffer, *pchBuffer)
        WCHAR* szBuffer,
        ULONG cchBuffer,
        ULONG* pchBuffer)
    {
        // Handle empty string.
        if (str[0] == '\0')
        {
            if (szBuffer != nullptr)
                ::memset(&szBuffer[0], 0, sizeof(*szBuffer));
            if (pchBuffer != nullptr)
                *pchBuffer = 0;
            return S_OK;
        }

        HRESULT hr = pal::ConvertUtf8ToUtf16(str, szBuffer, cchBuffer, (uint32_t*)pchBuffer);
        if (FAILED(hr))
        {
            if (hr == E_NOT_SUFFICIENT_BUFFER
                && szBuffer != nullptr
                && cchBuffer > 0)
            {
                ::memset(&szBuffer[cchBuffer - 1], 0, sizeof(*szBuffer));
                return CLDB_S_TRUNCATION;
            }
            return E_INVALIDARG;
        }
        return S_OK;
    }

    HRESULT ConstructTypeName(
        char const* nspace,
        char const* name,
        malloc_ptr<char>& mem)
    {
        char* buffer;
        size_t nspaceLen = nspace == nullptr ? 0 : ::strlen(nspace);
        size_t nameLen = name == nullptr ? 0 : ::strlen(name);
        size_t bufferLength = nspaceLen + nameLen + 1 + 1; // +1 for type delim and +1 for null.

        mem.reset((char*)::malloc(bufferLength * sizeof(*buffer)));
        if (mem == nullptr)
            return E_OUTOFMEMORY;

        buffer = mem.get();
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

    void SplitTypeName(
        char* typeName,
        char const** nspace,
        char const** name)
    {
        // Search for the last delimiter.
        char* pos = ::strrchr(typeName, '.');
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

    // Starting from the supplied cursor, find and then enumerate
    // the range of rows in "lookupRange" with the given "lookupTk"
    // value. When a value is found, the supplied type instance will
    // be used to call back to the caller.
    //
    // Example of type instance:
    //   struct Operation
    //   {
    //       bool operator()(mdcursor_t cursor)
    //       {
    //           return stopEnumeration; // Return true to stop, false to continue.
    //       }
    //   };
    template<typename T>
    void EnumTableRange(
        mdcursor_t begin,
        uint32_t count,
        col_index_t lookupRange,
        mdToken lookupTk,
        T& op)
    {
        mdcursor_t curr;
        uint32_t currCount;
        md_range_result_t result = md_find_range_from_cursor(begin, lookupRange, lookupTk, &curr, &currCount);
        if (result == MD_RANGE_FOUND)
        {
            // Table is sorted and subset found
            for (uint32_t i = 0; i < currCount; ++i)
            {
                if (op(curr))
                    return;
                (void)md_cursor_next(&curr);
            }
        }
        else if (result == MD_RANGE_NOT_SUPPORTED)
        {
            // Cannot get a range on this table so we need to search across the entire table
            curr = begin;
            currCount = count;

            // Read in for matching in bulk
            mdToken matchedGroup[64];
            uint32_t i = 0;
            while (i < currCount)
            {
                int32_t read = md_get_column_value_as_token(curr, lookupRange, ARRAY_SIZE(matchedGroup), matchedGroup);
                if (read == 0)
                    break;

                assert(read > 0);
                for (int32_t j = 0; j < read; ++j)
                {
                    if (matchedGroup[j] == lookupTk)
                    {
                        if (op(curr))
                            return;
                    }
                    (void)md_cursor_next(&curr);
                }
                i += read;
            }
        }
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
            return CLDB_E_RECORD_NOTFOUND;

        // From ECMA-335, section II.22.37:
        //  "The first row of the TypeDef table represents the pseudo class that acts as parent for functions
        //  and variables defined at module scope."
        // Based on the above we always skip the first row.
        rows--;
        (void)md_cursor_next(&cursor);

        RETURN_IF_FAILED(HCORENUMImpl::CreateTableEnum(1, &enumImpl));
        HCORENUMImpl::InitTableEnum(*enumImpl, 0, cursor, rows);
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
            return CLDB_E_RECORD_NOTFOUND;

        RETURN_IF_FAILED(CreateEnumTokenRangeForSortedTableKey(_md_ptr.get(), mdtid_InterfaceImpl, mdtInterfaceImpl_Class, td, &enumImpl));
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

namespace
{
    HRESULT FindTypeDefByName(
        MetadataImportRO* importer,
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
            if (1 != md_get_column_value_as_token(typeRefCursor, mdtTypeRef_ResolutionScope, 1, &typeRefScope)
                || 1 != md_get_column_value_as_utf8(typeRefCursor, mdtTypeRef_TypeNamespace, 1, &typeRefNspace)
                || 1 != md_get_column_value_as_utf8(typeRefCursor, mdtTypeRef_TypeName, 1, &typeRefName))
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
            if (1 != md_get_column_value_as_constant(cursor, mdtTypeDef_Flags, 1, &flags))
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

            if (1 != md_get_column_value_as_utf8(cursor, mdtTypeDef_TypeNamespace, 1, &str))
                return CLDB_E_FILE_CORRUPT;

            if (0 != ::strcmp(nspace, str))
                continue;

            if (1 != md_get_column_value_as_utf8(cursor, mdtTypeDef_TypeName, 1, &str))
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

HRESULT STDMETHODCALLTYPE MetadataImportRO::FindTypeDefByName(
    LPCWSTR     szTypeDef,
    mdToken     tkEnclosingClass,
    mdTypeDef* ptd)
{
    if (szTypeDef == nullptr || ptd == nullptr)
        return E_INVALIDARG;

    // Check the enclosing token is either valid or nil.
    if (TypeFromToken(tkEnclosingClass) != mdtTypeDef
        && TypeFromToken(tkEnclosingClass) != mdtTypeRef
        && TypeFromToken(tkEnclosingClass) != mdtModule
        && !IsNilToken(tkEnclosingClass))
    {
        return E_INVALIDARG;
    }
    else if (tkEnclosingClass == MD_MODULE_TOKEN)
    {
        // Module scope is the same as no scope
        tkEnclosingClass = mdTokenNil;
    }

    pal::StringConvert<WCHAR, char> cvt{ szTypeDef };
    if (!cvt.Success())
        return E_INVALIDARG;

    char const* nspace;
    char const* name;
    SplitTypeName(cvt, &nspace, &name);
    return ::FindTypeDefByName(this, nspace, name, tkEnclosingClass, ptd);
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

    GUID mvid;
    if (1 != md_get_column_value_as_guid(cursor, mdtModule_Mvid, 1, reinterpret_cast<mdguid_t*>(&mvid)))
        return CLDB_E_FILE_CORRUPT;
    *pmvid = mvid;

    char const* name;
    if (1 != md_get_column_value_as_utf8(cursor, mdtModule_Name, 1, &name))
        return CLDB_E_FILE_CORRUPT;
    return ConvertAndReturnStringOutput(name, szName, cchName, pchName);
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

    uint32_t flags;
    if (1 != md_get_column_value_as_constant(cursor, mdtTypeDef_Flags, 1, &flags))
        return CLDB_E_FILE_CORRUPT;
    *pdwTypeDefFlags = flags;

    mdToken extends;
    if (1 != md_get_column_value_as_token(cursor, mdtTypeDef_Extends, 1, &extends))
        return CLDB_E_FILE_CORRUPT;
    *ptkExtends = extends == mdTypeDefNil
        ? mdTypeRefNil
        : extends;

    char const* name;
    char const* nspace;
    if (1 != md_get_column_value_as_utf8(cursor, mdtTypeDef_TypeName, 1, &name)
        || 1 != md_get_column_value_as_utf8(cursor, mdtTypeDef_TypeNamespace, 1, &nspace))
    {
        return CLDB_E_FILE_CORRUPT;
    }

    HRESULT hr;
    malloc_ptr<char> mem;
    RETURN_IF_FAILED(ConstructTypeName(nspace, name, mem));
    return ConvertAndReturnStringOutput(mem.get(), szTypeDef, cchTypeDef, pchTypeDef);
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

    mdTypeDef type;
    if (1 != md_get_column_value_as_token(cursor, mdtInterfaceImpl_Class, 1, &type))
        return CLDB_E_FILE_CORRUPT;
    *pClass = type;

    mdToken iface;
    if (1 != md_get_column_value_as_token(cursor, mdtInterfaceImpl_Interface, 1, &iface))
        return CLDB_E_FILE_CORRUPT;
    *ptkIface = iface;

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
    if (TypeFromToken(tr) != mdtTypeRef)
        return E_INVALIDARG;

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), tr, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    mdToken resScope;
    if (1 != md_get_column_value_as_token(cursor, mdtTypeRef_ResolutionScope, 1, &resScope))
        return CLDB_E_FILE_CORRUPT;
    *ptkResolutionScope = resScope;

    char const* name;
    char const* nspace;
    if (1 != md_get_column_value_as_utf8(cursor, mdtTypeRef_TypeName, 1, &name)
        || 1 != md_get_column_value_as_utf8(cursor, mdtTypeRef_TypeNamespace, 1, &nspace))
    {
        return CLDB_E_FILE_CORRUPT;
    }

    HRESULT hr;
    malloc_ptr<char> mem;
    RETURN_IF_FAILED(ConstructTypeName(nspace, name, mem));
    return ConvertAndReturnStringOutput(mem.get(), szName, cchName, pchName);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::ResolveTypeRef(mdTypeRef tr, REFIID riid, IUnknown** ppIScope, mdTypeDef* ptd)
{
    UNREFERENCED_PARAMETER(tr);
    UNREFERENCED_PARAMETER(riid);
    UNREFERENCED_PARAMETER(ppIScope);
    UNREFERENCED_PARAMETER(ptd);

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
        HCORENUMImpl::InitTableEnum(*enumImpl, 0, methodList, methodListCount);
        HCORENUMImpl::InitTableEnum(*enumImpl, 1, fieldList, fieldListCount);
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
            mdcursor_t methodCursor;
            if (!md_resolve_indirect_cursor(methodList, &methodCursor))
                return CLDB_E_FILE_CORRUPT;
            if (1 != md_get_column_value_as_utf8(methodCursor, mdtMethodDef_Name, 1, &toMatch))
                return CLDB_E_FILE_CORRUPT;

            if (0 == ::strcmp(toMatch, cvt))
            {
                (void)md_cursor_to_token(methodCursor, &matchedTk);
                RETURN_IF_FAILED(HCORENUMImpl::AddToDynamicEnum(*enumImpl, matchedTk));
            }
            (void)md_cursor_next(&methodList);
        }

        // Iterate the Type's fields
        for (uint32_t i = 0; i < fieldListCount; ++i)
        {
            mdcursor_t fieldCursor;
            if (!md_resolve_indirect_cursor(fieldList, &fieldCursor))
                return CLDB_E_FILE_CORRUPT;
            if (1 != md_get_column_value_as_utf8(fieldCursor, mdtField_Name, 1, &toMatch))
                return CLDB_E_FILE_CORRUPT;

            if (0 == ::strcmp(toMatch, cvt))
            {
                (void)md_cursor_to_token(fieldCursor, &matchedTk);
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
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        mdcursor_t cursor;
        uint32_t count;
        if (!md_create_cursor(_md_ptr.get(), mdtid_MemberRef, &cursor, &count))
            return CLDB_E_RECORD_NOTFOUND;

        RETURN_IF_FAILED(HCORENUMImpl::CreateDynamicEnum(&enumImpl));

        HCORENUMImpl_ptr cleanup{ enumImpl };

        // Read in for matching in bulk
        mdToken toMatch[64];
        mdToken matchedTk;
        uint32_t i = 0;
        while (i < count)
        {
            int32_t read = md_get_column_value_as_token(cursor, mdtMemberRef_Class, ARRAY_SIZE(toMatch), toMatch);
            if (read == 0)
                break;

            assert(read > 0);
            for (int32_t j = 0; j < read; ++j)
            {
                if (toMatch[j] == tkParent)
                {
                    (void)md_cursor_to_token(cursor, &matchedTk);
                    RETURN_IF_FAILED(HCORENUMImpl::AddToDynamicEnum(*enumImpl, matchedTk));
                }
                (void)md_cursor_next(&cursor);
            }
            i += read;
        }
        *phEnum = cleanup.release();
    }
    return enumImpl->ReadTokens(rMemberRefs, cMax, pcTokens);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumMethodImpls(
    HCORENUM* phEnum,
    mdTypeDef   td,
    mdToken     rMethodBody[],
    mdToken     rMethodDecl[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        if (TypeFromToken(td) != mdtTypeDef)
            return E_INVALIDARG;

        mdcursor_t cursor;
        uint32_t count;
        if (!md_create_cursor(_md_ptr.get(), mdtid_MethodImpl, &cursor, &count))
            return CLDB_E_RECORD_NOTFOUND;

        RETURN_IF_FAILED(HCORENUMImpl::CreateDynamicEnum(&enumImpl, 2));
        HCORENUMImpl_ptr cleanup{ enumImpl };

        struct _Finder
        {
            HCORENUMImpl& EnumImpl;
            mdToken Body;
            mdToken Decl;
            HRESULT hr;
            HRESULT Result; // Result of the operation

            bool operator()(mdcursor_t c)
            {
                if (1 != md_get_column_value_as_token(c, mdtMethodImpl_MethodBody, 1, &Body)
                    || 1 != md_get_column_value_as_token(c, mdtMethodImpl_MethodDeclaration, 1, &Decl))
                {
                    Result = CLDB_E_FILE_CORRUPT;
                    return true;
                }

                if (FAILED(hr = HCORENUMImpl::AddToDynamicEnum(EnumImpl, Body))
                    || FAILED(hr = HCORENUMImpl::AddToDynamicEnum(EnumImpl, Decl)))
                {
                    Result = hr;
                    return true;
                }

                return false;
            }
        } finder{ *enumImpl, mdTokenNil, mdTokenNil, S_OK, S_OK };

        EnumTableRange(cursor, count, mdtMethodImpl_Class, td, finder);
        RETURN_IF_FAILED(finder.Result);

        *phEnum = cleanup.release();
    }
    return enumImpl->ReadTokenPairs(rMethodBody, rMethodDecl, cMax, pcTokens);
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
            return CLDB_E_RECORD_NOTFOUND;

        if (!IsNilToken(tk))
        {
            if (md_find_range_from_cursor(cursor, mdtDeclSecurity_Parent, tk, &cursor, &count) == MD_RANGE_NOT_FOUND)
                return CLDB_E_RECORD_NOTFOUND;
        }

        if (IsNilToken(tk) && IsDclActionNil(dwActions))
        {
            RETURN_IF_FAILED(HCORENUMImpl::CreateTableEnum(1, &enumImpl));
            HCORENUMImpl::InitTableEnum(*enumImpl, 0, cursor, count);
        }
        else
        {
            uint32_t action;
            mdToken parent;
            mdToken toAdd;
            RETURN_IF_FAILED(HCORENUMImpl::CreateDynamicEnum(&enumImpl));

            HCORENUMImpl_ptr cleanup{ enumImpl };
            for (uint32_t i = 0; i < count; ++i)
            {
                if ((IsDclActionNil(dwActions)
                        || (1 == md_get_column_value_as_constant(cursor, mdtDeclSecurity_Action, 1, &action)
                            && action == dwActions))
                    && (IsNilToken(tk)
                        || (1 == md_get_column_value_as_token(cursor, mdtDeclSecurity_Parent, 1, &parent)
                            && parent == tk)))
                {
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
    HRESULT hr = FindMethod(td, szName, pvSigBlob, cbSigBlob, (mdMethodDef*)pmb);
    if (hr == CLDB_E_RECORD_NOTFOUND)
        hr = FindField(td, szName, pvSigBlob, cbSigBlob, (mdFieldDef*)pmb);
    return hr;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::FindMethod(
    mdTypeDef   td,
    LPCWSTR     szName,
    PCCOR_SIGNATURE pvSigBlob,
    ULONG       cbSigBlob,
    mdMethodDef* pmb)
{
    if (TypeFromToken(td) != mdtTypeDef && td != mdTokenNil)
        return E_INVALIDARG;

    if (td == mdTypeDefNil || td == mdTokenNil)
        td = MD_GLOBAL_PARENT_TOKEN;

    mdcursor_t typedefCursor;
    if (!md_token_to_cursor(_md_ptr.get(), td, &typedefCursor))
        return CLDB_E_INDEX_NOTFOUND;

    mdcursor_t methodCursor;
    uint32_t count;
    if (!md_get_column_value_as_range(typedefCursor, mdtTypeDef_MethodList, &methodCursor, &count))
        return CLDB_E_FILE_CORRUPT;

    malloc_span<uint8_t> methodDefSig;
    try
    {
        methodDefSig = GetMethodDefSigFromMethodRefSig({ (uint8_t*)pvSigBlob, (size_t)cbSigBlob });    
    }
    catch (std::exception const&)
    {
        return E_INVALIDARG;
    }

    pal::StringConvert<WCHAR, char> cvt{ szName };
    if (!cvt.Success())
        return E_INVALIDARG;

    for (uint32_t i = 0; i < count; (void)md_cursor_next(&methodCursor), ++i)
    {
        mdcursor_t target;
        if (!md_resolve_indirect_cursor(methodCursor, &target))
            return CLDB_E_FILE_CORRUPT;
        uint32_t flags;
        if (1 != md_get_column_value_as_constant(target, mdtMethodDef_Flags, 1, &flags))
            return CLDB_E_FILE_CORRUPT;

        // Ignore PrivateScope methods. By the spec, they can only be referred to by a MethodDef token
        // and cannot be discovered in any other way.
        if (IsMdPrivateScope(flags))
            continue;

        char const* methodName;
        if (1 != md_get_column_value_as_utf8(target, mdtMethodDef_Name, 1, &methodName))
            return CLDB_E_FILE_CORRUPT;
        if (::strncmp(methodName, cvt, cvt.Length()) != 0)
            continue;

        if (pvSigBlob != nullptr)
        {
            uint8_t const* sig;
            uint32_t sigLen;
            if (1 != md_get_column_value_as_blob(target, mdtMethodDef_Signature, 1, &sig, &sigLen))
                return CLDB_E_FILE_CORRUPT;
            if (sigLen != methodDefSig.size()
                || ::memcmp(methodDefSig, sig, sigLen) != 0)
            {
                continue;
            }
        }
        if (!md_cursor_to_token(target, pmb))
            return CLDB_E_FILE_CORRUPT;
        return S_OK;
    }
    return CLDB_E_RECORD_NOTFOUND;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::FindField(
    mdTypeDef   td,
    LPCWSTR     szName,
    PCCOR_SIGNATURE pvSigBlob,
    ULONG       cbSigBlob,
    mdFieldDef* pmb)
{
    if (TypeFromToken(td) != mdtTypeDef && td != mdTokenNil)
        return E_INVALIDARG;

    if (td == mdTypeDefNil || td == mdTokenNil)
        td = MD_GLOBAL_PARENT_TOKEN;

    mdcursor_t typedefCursor;
    if (!md_token_to_cursor(_md_ptr.get(), td, &typedefCursor))
        return CLDB_E_INDEX_NOTFOUND;

    mdcursor_t fieldCursor;
    uint32_t count;
    if (!md_get_column_value_as_range(typedefCursor, mdtTypeDef_FieldList, &fieldCursor, &count))
        return CLDB_E_FILE_CORRUPT;

    pal::StringConvert<WCHAR, char> cvt{ szName };
    if (!cvt.Success())
        return E_INVALIDARG;

    for (uint32_t i = 0; i < count; (void)md_cursor_next(&fieldCursor), ++i)
    {
        mdcursor_t target;
        if (!md_resolve_indirect_cursor(fieldCursor, &target))
            return CLDB_E_FILE_CORRUPT;
        uint32_t flags;
        if (1 != md_get_column_value_as_constant(target, mdtField_Flags, 1, &flags))
            return CLDB_E_FILE_CORRUPT;

        // Ignore PrivateScope fields. By the spec, they can only be referred to by a FieldDef token
        // and cannot be discovered in any other way.
        if (IsFdPrivateScope(flags))
            continue;

        char const* name;
        if (1 != md_get_column_value_as_utf8(target, mdtField_Name, 1, &name))
            return CLDB_E_FILE_CORRUPT;
        if (::strncmp(name, cvt, cvt.Length()) != 0)
            continue;

        if (pvSigBlob != nullptr)
        {
            uint8_t const* sig;
            uint32_t sigLen;
            if (1 != md_get_column_value_as_blob(target, mdtField_Signature, 1, &sig, &sigLen))
                return CLDB_E_FILE_CORRUPT;
            if (cbSigBlob != sigLen
                || ::memcmp(pvSigBlob, sig, sigLen) != 0)
            {
                continue;
            }
        }
        if (!md_cursor_to_token(target, pmb))
            return CLDB_E_FILE_CORRUPT;
        return S_OK;
    }
    return CLDB_E_RECORD_NOTFOUND;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::FindMemberRef(
    mdTypeRef   td,
    LPCWSTR     szName,
    PCCOR_SIGNATURE pvSigBlob,
    ULONG       cbSigBlob,
    mdMemberRef* pmr)
{
    if (TypeFromToken(td) != mdtTypeRef
        && TypeFromToken(td) != mdtMethodDef
        && TypeFromToken(td) != mdtModuleRef
        && TypeFromToken(td) != mdtTypeDef
        && TypeFromToken(td) != mdtTypeSpec)
    {
        return E_INVALIDARG;
    }

    if (szName == nullptr || pmr == nullptr)
        return CLDB_E_RECORD_NOTFOUND;

    if (IsNilToken(td))
        td = MD_GLOBAL_PARENT_TOKEN;

    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(_md_ptr.get(), mdtid_MemberRef, &cursor, &count))
        return CLDB_E_FILE_CORRUPT;

    for (uint32_t i = 0; i < count; (void)md_cursor_next(&cursor), ++i)
    {
        mdToken refParent;
        if (1 != md_get_column_value_as_token(cursor, mdtMemberRef_Class, 1, &refParent))
            return CLDB_E_FILE_CORRUPT;

        if (refParent != td)
            continue;

        if (szName != nullptr)
        {
            char const* name;
            if (1 != md_get_column_value_as_utf8(cursor, mdtMemberRef_Name, 1, &name))
                return CLDB_E_FILE_CORRUPT;
            pal::StringConvert<WCHAR, char> cvt{ szName };
            if (!cvt.Success())
                return E_INVALIDARG;
            if (::strncmp(name, cvt, cvt.Length()) != 0)
                continue;
        }

        if (pvSigBlob != nullptr)
        {
            uint8_t const* sig;
            uint32_t sigLen;
            if (1 != md_get_column_value_as_blob(cursor, mdtMemberRef_Signature, 1, &sig, &sigLen))
                return CLDB_E_FILE_CORRUPT;
            if (cbSigBlob != sigLen
                || ::memcmp(pvSigBlob, sig, sigLen) != 0)
            {
                continue;
            }
        }
        if (!md_cursor_to_token(cursor, pmr))
            return CLDB_E_FILE_CORRUPT;
        return S_OK;
    }
    return CLDB_E_RECORD_NOTFOUND;
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
    if (TypeFromToken(mb) != mdtMethodDef)
        return E_INVALIDARG;

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), mb, &cursor))
        return CLDB_E_INDEX_NOTFOUND;

    mdTypeDef classDef;
    if (!md_find_token_of_range_element(cursor, &classDef))
        return CLDB_E_RECORD_NOTFOUND;
    *pClass = classDef;

    uint32_t attrs;
    if (1 != md_get_column_value_as_constant(cursor, mdtMethodDef_Flags, 1, &attrs))
        return CLDB_E_FILE_CORRUPT;
    *pdwAttr = attrs;

    uint32_t rva;
    if (1 != md_get_column_value_as_constant(cursor, mdtMethodDef_Rva, 1, &rva))
        return CLDB_E_FILE_CORRUPT;
    *pulCodeRVA = rva;

    uint32_t implFlags;
    if (1 != md_get_column_value_as_constant(cursor, mdtMethodDef_ImplFlags, 1, &implFlags))
        return CLDB_E_FILE_CORRUPT;
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
    return ConvertAndReturnStringOutput(name, szMethod, cchMethod, pchMethod);
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
    if (TypeFromToken(mr) != mdtMemberRef)
        return E_INVALIDARG;

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), mr, &cursor))
        return CLDB_E_INDEX_NOTFOUND;

    mdToken type;
    if (1 != md_get_column_value_as_token(cursor, mdtMemberRef_Class, 1, &type))
        return CLDB_E_FILE_CORRUPT;

    *ptk = type;

    uint8_t const* sig;
    uint32_t sigLen;
    if (1 != md_get_column_value_as_blob(cursor, mdtMemberRef_Signature, 1, &sig, &sigLen))
        return CLDB_E_FILE_CORRUPT;

    *ppvSigBlob = sig;
    *pbSig = sigLen;

    char const* name;
    if (1 != md_get_column_value_as_utf8(cursor, mdtMemberRef_Name, 1, &name))
        return CLDB_E_FILE_CORRUPT;
    return ConvertAndReturnStringOutput(name, szMember, cchMember, pchMember);
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
            return CLDB_E_RECORD_NOTFOUND;

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
        HCORENUMImpl::InitTableEnum(*enumImpl, 0, propertyList, propertyListCount);
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
            return CLDB_E_RECORD_NOTFOUND;

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
        HCORENUMImpl::InitTableEnum(*enumImpl, 0, eventList, eventListCount);
        *phEnum = enumImpl;
    }
    return enumImpl->ReadTokens(rEvents, cMax, pcEvents);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetEventProps(
    mdEvent     ev,
    mdTypeDef* pClass,
    // Should be defined as _Out_writes_to_opt_(cchEvent, *pchEvent) and non-const. Mistake from initial release.
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
    if (TypeFromToken(ev) != mdtEvent)
        return E_INVALIDARG;

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), ev, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    mdTypeDef classDef;
    if (!md_find_token_of_range_element(cursor, &classDef))
        return CLDB_E_RECORD_NOTFOUND;
    *pClass = classDef;

    uint32_t flags;
    if (1 != md_get_column_value_as_constant(cursor, mdtEvent_EventFlags, 1, &flags))
        return CLDB_E_FILE_CORRUPT;
    *pdwEventFlags = flags;

    mdToken type;
    if (1 != md_get_column_value_as_token(cursor, mdtEvent_EventType, 1, &type))
        return CLDB_E_FILE_CORRUPT;
    *ptkEventType = type;

    mdcursor_t methodSemCursor;
    uint32_t methodSemCount;
    if (!md_create_cursor(_md_ptr.get(), mdtid_MethodSemantics, &methodSemCursor, &methodSemCount))
        return CLDB_E_RECORD_NOTFOUND;

    struct _Finder
    {
        mdMethodDef AddOn;
        mdMethodDef RemoveOn;
        mdMethodDef Fire;
        mdMethodDef* Other;
        uint32_t const OtherLen;
        uint32_t OtherCount;
        HRESULT Result; // Result of the operation

        bool operator()(mdcursor_t c)
        {
            mdMethodDef tk;
            uint32_t semantics;
            if (1 != md_get_column_value_as_token(c, mdtMethodSemantics_Method, 1, &tk)
                || 1 != md_get_column_value_as_constant(c, mdtMethodSemantics_Semantics, 1, &semantics))
            {
                Result = CLDB_E_FILE_CORRUPT;
                return true; // Failure detected, so stop.
            }
            switch (semantics)
            {
            case msAddOn: AddOn = tk;
                break;
            case msRemoveOn: RemoveOn = tk;
                break;
            case msFire: Fire = tk;
                break;
            case msOther:
                if (OtherCount < OtherLen)
                    Other[OtherCount] = tk;
                OtherCount++;
                break;
            default:
                assert(!"Unknown semantic");
            }
            return false;
        }
    } finder{ mdMethodDefNil, mdMethodDefNil, mdMethodDefNil, rmdOtherMethod, cMax, 0, S_OK };

    EnumTableRange(methodSemCursor, methodSemCount, mdtMethodSemantics_Association, ev, finder);

    HRESULT hr;
    RETURN_IF_FAILED(finder.Result);

    *pmdAddOn = finder.AddOn;
    *pmdRemoveOn = finder.RemoveOn;
    *pmdFire = finder.Fire;
    *pcOtherMethod = finder.OtherCount;

    char const* name;
    if (1 != md_get_column_value_as_utf8(cursor, mdtEvent_Name, 1, &name))
        return CLDB_E_FILE_CORRUPT;
    // The const_cast<> is needed because the signature incorrectly expresses the
    // desired semantics. This has been wrong since .NET Framework 1.0.
    return ConvertAndReturnStringOutput(name, const_cast<WCHAR*>(szEvent), cchEvent, pchEvent);
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
            return CLDB_E_RECORD_NOTFOUND;

        RETURN_IF_FAILED(HCORENUMImpl::CreateDynamicEnum(&enumImpl));

        HCORENUMImpl_ptr cleanup{ enumImpl };

        // Read in for matching in bulk
        mdToken toMatch[64];
        mdToken matchedTk;
        uint32_t i = 0;
        while (i < count)
        {
            int32_t read = md_get_column_value_as_token(cursor, mdtMethodSemantics_Method, ARRAY_SIZE(toMatch), toMatch);
            if (read == 0)
                break;

            assert(read > 0);
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
    if (TypeFromToken(mb) != mdtMethodDef || pdwSemanticsFlags == nullptr)
        return E_INVALIDARG;

    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(_md_ptr.get(), mdtid_MethodSemantics, &cursor, &count))
        return CLDB_E_RECORD_NOTFOUND;

    struct _Finder
    {
        mdMethodDef const MethodDef; // Look for this methoddef
        uint32_t Value; // Value to acquire
        HRESULT Result; // Result of the operation

        bool operator()(mdcursor_t c)
        {
            mdToken matchedTk;
            if (1 == md_get_column_value_as_token(c, mdtMethodSemantics_Method, 1, &matchedTk)
                && MethodDef == matchedTk)
            {
                // Found result, stop iterating
                Result = (1 == md_get_column_value_as_constant(c, mdtMethodSemantics_Semantics, 1, &Value))
                    ? S_OK
                    : CLDB_E_FILE_CORRUPT;
                return true;
            }
            return false;
        }
    } finder{ mb, 0, CLDB_E_RECORD_NOTFOUND };

    EnumTableRange(cursor, count, mdtMethodSemantics_Association, tkEventProp, finder);

    HRESULT hr;
    RETURN_IF_FAILED(finder.Result);

    *pdwSemanticsFlags = finder.Value;
    return S_OK;
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
    uint32_t count;
    mdcursor_t entry;
    bool foundLayout = false;
    if (!md_create_cursor(_md_ptr.get(), mdtid_ClassLayout, &begin, &count)
        || !md_find_row_from_cursor(begin, mdtClassLayout_Parent, RidFromToken(td), &entry))
    {
        *pdwPackSize = 0;
        *pulClassSize = 0;
    }
    else
    {
        foundLayout = true;

        uint32_t packSize;
        uint32_t classSize;
        // Acquire the packing and class sizes for the type and cursor to the typedef entry.
        if (1 != md_get_column_value_as_constant(entry, mdtClassLayout_PackingSize, 1, &packSize)
            || 1 != md_get_column_value_as_constant(entry, mdtClassLayout_ClassSize, 1, &classSize))
        {
            return CLDB_E_FILE_CORRUPT;
        }

        *pdwPackSize = packSize;
        *pulClassSize = classSize;
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
        if (!md_create_cursor(_md_ptr.get(), mdtid_FieldLayout, &fieldLayoutBegin, &count))
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
    if (TypeFromToken(tk) != mdtParamDef && TypeFromToken(tk) != mdtFieldDef)
        return E_INVALIDARG;

    mdcursor_t cursor;
    uint32_t count;
    mdcursor_t fieldMarshalRow;
    if (!md_create_cursor(_md_ptr.get(), mdtid_FieldMarshal, &cursor, &count)
        || !md_find_row_from_cursor(cursor, mdtFieldMarshal_Parent, tk, &fieldMarshalRow))
    {
        return CLDB_E_RECORD_NOTFOUND;
    }

    uint8_t const* sig;
    uint32_t sigLen;
    if (1 != md_get_column_value_as_blob(fieldMarshalRow, mdtFieldMarshal_NativeType, 1, &sig, &sigLen))
        return CLDB_E_FILE_CORRUPT;

    *ppvNativeType = (PCCOR_SIGNATURE)sig;
    *pcbNativeType = sigLen;
    return S_OK;
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

        uint32_t count;
        mdcursor_t fieldRvaRow;
        if (!md_create_cursor(_md_ptr.get(), mdtid_FieldRva, &cursor, &count)
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

    uint8_t const* sig;
    uint32_t sigLen;
    if (1 != md_get_column_value_as_blob(cursor, mdtStandAloneSig_Signature, 1, &sig, &sigLen))
        return CLDB_E_FILE_CORRUPT;
    *ppvSig = (PCCOR_SIGNATURE)sig;
    *pcbSig = sigLen;

    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetModuleRefProps(
    mdModuleRef mur,
    _Out_writes_to_opt_(cchName, *pchName)
    LPWSTR      szName,
    ULONG       cchName,
    ULONG* pchName)
{
    if (TypeFromToken(mur) != mdtModuleRef)
        return E_INVALIDARG;

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), mur, &cursor))
        return CLDB_E_INDEX_NOTFOUND;

    char const* name;
    if (1 != md_get_column_value_as_utf8(cursor, mdtModuleRef_Name, 1, &name))
        return CLDB_E_FILE_CORRUPT;
    return ConvertAndReturnStringOutput(name, szName, cchName, pchName);
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
    if (pszUtf8NamePtr == nullptr)
        return E_INVALIDARG;

    col_index_t col_idx;
    switch (TypeFromToken(tk))
    {
    case mdtModule:
        col_idx = mdtModule_Name;
        break;
    case mdtTypeRef:
        col_idx = mdtTypeRef_TypeName;
        break;
    case mdtTypeDef:
        col_idx = mdtTypeDef_TypeName;
        break;
    case mdtFieldDef:
        col_idx = mdtField_Name;
        break;
    case mdtMethodDef:
        col_idx = mdtMethodDef_Name;
        break;
    case mdtParamDef:
        col_idx = mdtParam_Name;
        break;
    case mdtMemberRef:
        col_idx = mdtMemberRef_Name;
        break;
    case mdtEvent:
        col_idx = mdtEvent_Name;
        break;
    case mdtProperty:
        col_idx = mdtProperty_Name;
        break;
    case mdtModuleRef:
        col_idx = mdtModuleRef_Name;
        break;
    default:
        return E_INVALIDARG;
    }

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), tk, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

     if (1 != md_get_column_value_as_utf8(cursor, col_idx, 1, pszUtf8NamePtr))
        return CLDB_E_FILE_CORRUPT;

     return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumUnresolvedMethods(
    HCORENUM* phEnum,
    mdToken     rMethods[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    UNREFERENCED_PARAMETER(phEnum);
    UNREFERENCED_PARAMETER(rMethods);
    UNREFERENCED_PARAMETER(cMax);
    UNREFERENCED_PARAMETER(pcTokens);

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
    if (!md_walk_user_string_heap(_md_ptr.get(), &cursor, &string, &offset))
        return CLDB_E_INDEX_NOTFOUND;

    // Strings in #US should have a trailing single byte.
    if (string.str_bytes % sizeof(WCHAR) == 0)
        return CLDB_E_FILE_CORRUPT;

    // Compute the string size in characters
    uint32_t retCharLen = (string.str_bytes - 1) / sizeof(WCHAR);
    *pchString = retCharLen;
    if (cchString > 0 && szString != nullptr)
    {
        uint32_t toCopyWChar = cchString < retCharLen ? cchString : retCharLen;
        ::memcpy(szString, string.str, toCopyWChar * sizeof(WCHAR));
        if (cchString < retCharLen)
        {
            ::memset(&szString[toCopyWChar - 1], 0, sizeof(WCHAR)); // Ensure null terminator
            return CLDB_S_TRUNCATION;
        }
    }

    return S_OK;
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
    if (TypeFromToken(tk) != mdtMethodDef && TypeFromToken(tk) != mdtFieldDef)
        return E_INVALIDARG;

    mdcursor_t cursor;
    uint32_t count;
    mdcursor_t implRow;
    if (!md_create_cursor(_md_ptr.get(), mdtid_ImplMap, &cursor, &count)
        || !md_find_row_from_cursor(cursor, mdtImplMap_MemberForwarded, tk, &implRow))
    {
        return CLDB_E_RECORD_NOTFOUND;
    }

    uint32_t flags;
    if (1 != md_get_column_value_as_constant(implRow, mdtImplMap_MappingFlags, 1, &flags))
        return CLDB_E_FILE_CORRUPT;
    *pdwMappingFlags = flags;

    mdModuleRef token;
    if (1 != md_get_column_value_as_token(implRow, mdtImplMap_ImportScope, 1, &token))
        return CLDB_E_FILE_CORRUPT;
    *pmrImportDLL = token;

    char const* importName;
    if (1 != md_get_column_value_as_utf8(implRow, mdtImplMap_ImportName, 1, &importName))
        return CLDB_E_FILE_CORRUPT;
    return ConvertAndReturnStringOutput(importName, szImportName, cchImportName, pchImportName);
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
        mduserstring_t us;
        uint32_t offset;
        mduserstringcursor_t cursor = 0;
        for (;;)
        {
            if (!md_walk_user_string_heap(_md_ptr.get(), &cursor, &us, &offset))
                break;

            // Ignore strings that are of zero length.
            if (us.str_bytes == 0)
                continue;

            // Add mdtString token types to the enumeration.
            RETURN_IF_FAILED(HCORENUMImpl::AddToDynamicEnum(*enumImpl, RidToToken(offset, mdtString)));
        }

        *phEnum = cleanup.release();
    }
    return enumImpl->ReadTokens(rStrings, cMax, pcStrings);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetParamForMethodIndex(
    mdMethodDef md,
    ULONG       ulParamSeq,
    mdParamDef* ppd)
{
    if (TypeFromToken(md) != mdtMethodDef || ulParamSeq == UINT32_MAX || ppd == nullptr)
        return E_INVALIDARG;

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), md, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    mdcursor_t curr;
    uint32_t count;
    if (!md_get_column_value_as_range(cursor, mdtMethodDef_ParamList, &curr, &count))
        return CLDB_E_FILE_CORRUPT;

    uint32_t seqMaybe;
    for (uint32_t i = 0; i < count; ++i)
    {
        mdcursor_t target;
        if (!md_resolve_indirect_cursor(curr, &target))
            return CLDB_E_FILE_CORRUPT;
        if (1 != md_get_column_value_as_constant(target, mdtParam_Sequence, 1, &seqMaybe))
            return CLDB_E_FILE_CORRUPT;

        if (ulParamSeq == seqMaybe)
        {
            (void)md_cursor_to_token(target, ppd);
            return S_OK;
        }

        // If the requested sequence value is less than what is returned,
        // we are done. Param sequences numbers are ordered - see II.22.33.
        if (ulParamSeq < seqMaybe)
            break;

        (void)md_cursor_next(&curr);
    }

    return CLDB_E_RECORD_NOTFOUND;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumCustomAttributes(
    HCORENUM* phEnum,
    mdToken     tk,
    mdToken     tkType,
    mdCustomAttribute rCustomAttributes[],
    ULONG       cMax,
    ULONG* pcCustomAttributes)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        mdcursor_t cursor;
        uint32_t count;
        if (!md_create_cursor(_md_ptr.get(), mdtid_CustomAttribute, &cursor, &count))
            return CLDB_E_RECORD_NOTFOUND;

        mdcursor_t curr;
        uint32_t currCount;
        if (IsNilToken(tk))
        {
            // Caller is looking across all attributes
            assert(IsNilToken(tkType)); // Ignoring type filter
            RETURN_IF_FAILED(HCORENUMImpl::CreateTableEnum(1, &enumImpl));
            HCORENUMImpl::InitTableEnum(*enumImpl, 0, cursor, count);
        }
        else
        {
            md_range_result_t result = md_find_range_from_cursor(cursor, mdtCustomAttribute_Parent, tk, &curr, &currCount);
            if (IsNilToken(tkType) && result != MD_RANGE_NOT_SUPPORTED)
            {
                // Caller is looking for all associated attributes and we got a table range.
                if (result == MD_RANGE_FOUND)
                {
                    RETURN_IF_FAILED(HCORENUMImpl::CreateTableEnum(1, &enumImpl));
                    HCORENUMImpl::InitTableEnum(*enumImpl, 0, curr, currCount);
                }
                else if (result == MD_RANGE_NOT_FOUND)
                {
                    // If there are no tokens found, create an empty enumeration.
                    RETURN_IF_FAILED(HCORENUMImpl::CreateDynamicEnum(&enumImpl));
                }
            }
            else
            {
                RETURN_IF_FAILED(HCORENUMImpl::CreateDynamicEnum(&enumImpl));

                HCORENUMImpl_ptr cleanup{ enumImpl };

                // We need to search across the entire table as we couldn't get a range.
                curr = cursor;
                currCount = count;

                // Read in for matching in bulk
                mdToken toMatch[64];
                mdToken matchedTk;
                uint32_t i = 0;
                while (i < currCount)
                {
                    int32_t read = md_get_column_value_as_token(curr, mdtCustomAttribute_Parent, ARRAY_SIZE(toMatch), toMatch);
                    if (read == 0)
                        break;

                    assert(read > 0);
                    for (int32_t j = 0; j < read; ++j)
                    {
                        if (toMatch[j] == tkType)
                        {
                            (void)md_cursor_to_token(curr, &matchedTk);
                            RETURN_IF_FAILED(HCORENUMImpl::AddToDynamicEnum(*enumImpl, matchedTk));
                        }
                        (void)md_cursor_next(&curr);
                    }
                    i += read;
                }
                enumImpl = cleanup.release();
            }
        }
        *phEnum = enumImpl;
    }
    return enumImpl->ReadTokens(rCustomAttributes, cMax, pcCustomAttributes);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetCustomAttributeProps(
    mdCustomAttribute cv,
    mdToken* ptkObj,
    mdToken* ptkType,
    void const** ppBlob,
    ULONG* pcbSize)
{
    if (TypeFromToken(cv) != mdtCustomAttribute)
        return E_INVALIDARG;

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), cv, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    mdToken obj;
    if (1 != md_get_column_value_as_token(cursor, mdtCustomAttribute_Parent, 1, &obj))
        return CLDB_E_FILE_CORRUPT;
    *ptkObj = obj;

    mdToken type;
    if (1 != md_get_column_value_as_token(cursor, mdtCustomAttribute_Type, 1, &type))
        return CLDB_E_FILE_CORRUPT;
    *ptkType = type;

    uint8_t const* blob;
    uint32_t blobLen;
    if (1 != md_get_column_value_as_blob(cursor, mdtCustomAttribute_Value, 1, &blob, &blobLen))
        return CLDB_E_FILE_CORRUPT;
    *ppBlob = blob;
    *pcbSize = blobLen;

    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::FindTypeRef(
    mdToken     tkResolutionScope,
    LPCWSTR     szName,
    mdTypeRef* ptr)
{
    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(_md_ptr.get(), mdtid_TypeRef, &cursor, &count))
        return CLDB_E_RECORD_NOTFOUND;

    pal::StringConvert<WCHAR, char> cvt{ szName };
    if (!cvt.Success())
        return E_INVALIDARG;

    char const* nspace;
    char const* name;
    SplitTypeName(cvt, &nspace, &name);

    bool scopeIsSet = !IsNilToken(tkResolutionScope);
    mdToken resMaybe;
    char const* str;
    for (uint32_t i = 0; i < count; (void)md_cursor_next(&cursor), ++i)
    {
        if (1 != md_get_column_value_as_token(cursor, mdtTypeRef_ResolutionScope, 1, &resMaybe))
            return CLDB_E_FILE_CORRUPT;

        // See if the Resolution scopes match.
        if ((IsNilToken(resMaybe) && scopeIsSet)    // User didn't state scope.
            || resMaybe != tkResolutionScope)       // Match user scope.
        {
            continue;
        }

        if (1 != md_get_column_value_as_utf8(cursor, mdtTypeRef_TypeNamespace, 1, &str))
            return CLDB_E_FILE_CORRUPT;

        if (0 != ::strcmp(nspace, str))
            continue;

        if (1 != md_get_column_value_as_utf8(cursor, mdtTypeRef_TypeName, 1, &str))
            return CLDB_E_FILE_CORRUPT;

        if (0 == ::strcmp(name, str))
        {
            (void)md_cursor_to_token(cursor, ptr);
            return S_OK;
        }
    }

    // Not found.
    return CLDB_E_RECORD_NOTFOUND;
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
    if (TypeFromToken(mb) == mdtMethodDef)
    {
        return GetMethodProps(
            mb,
            pClass,
            szMember,
            cchMember,
            pchMember,
            pdwAttr,
            ppvSigBlob,
            pcbSigBlob,
            pulCodeRVA,
            pdwImplFlags);
    }

    if (TypeFromToken(mb) == mdtFieldDef)
    {
        return GetFieldProps(
            mb,
            pClass,
            szMember,
            cchMember,
            pchMember,
            pdwAttr,
            ppvSigBlob,
            pcbSigBlob,
            pdwCPlusTypeFlag,
            ppValue,
            pcchValue);
    }

    return E_INVALIDARG;
}

namespace
{
    HRESULT FindConstant(
        mdhandle_view ptr,
        uint32_t lookupValue,
        DWORD& cnstCorType,
        UVCP_CONSTANT& cnst,
        ULONG& cnstLen)
    {
        assert(ptr != nullptr);

        mdcursor_t constantCursor;
        uint32_t constantCount;
        mdcursor_t constantPropCursor;
        uint32_t corType;
        uint8_t const* defaultValue;
        uint32_t defaultValueLen;
        if (!md_create_cursor(ptr.get(), mdtid_Constant, &constantCursor, &constantCount)
            || !md_find_row_from_cursor(constantCursor, mdtConstant_Parent, lookupValue, &constantPropCursor))
        {
            corType = ELEMENT_TYPE_VOID;
            defaultValue = nullptr;
            defaultValueLen = 0;
        }
        else
        {
            if (1 != md_get_column_value_as_constant(constantPropCursor, mdtConstant_Type, 1, &corType))
                return CLDB_E_FILE_CORRUPT;
            if (1 != md_get_column_value_as_blob(constantPropCursor, mdtConstant_Value, 1, &defaultValue, &defaultValueLen))
                return CLDB_E_FILE_CORRUPT;
        }

        cnstCorType = corType;
        cnst = (UVCP_CONSTANT)defaultValue;
        cnstLen = corType == ELEMENT_TYPE_STRING
            ? defaultValueLen / sizeof(WCHAR)
            : 0; // Only string return a non-zero length for the constant value.
        return S_OK;
    }
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
    if (TypeFromToken(mb) != mdtFieldDef)
        return E_INVALIDARG;

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), mb, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    mdTypeDef classDef;
    if (!md_find_token_of_range_element(cursor, &classDef))
        return CLDB_E_RECORD_NOTFOUND;
    *pClass = classDef;

    uint32_t flags;
    if (1 != md_get_column_value_as_constant(cursor, mdtField_Flags, 1, &flags))
        return CLDB_E_FILE_CORRUPT;
    *pdwAttr = flags;

    uint8_t const* sig;
    uint32_t sigLen;
    if (1 != md_get_column_value_as_blob(cursor, mdtField_Signature, 1, &sig, &sigLen))
        return CLDB_E_FILE_CORRUPT;
    *ppvSigBlob = (PCCOR_SIGNATURE)sig;
    *pcbSigBlob = sigLen;

    HRESULT hr;
    RETURN_IF_FAILED(FindConstant(_md_ptr, mb, *pdwCPlusTypeFlag, *ppValue, *pcchValue));

    char const* name;
    if (1 != md_get_column_value_as_utf8(cursor, mdtField_Name, 1, &name))
        return CLDB_E_FILE_CORRUPT;
    return ConvertAndReturnStringOutput(name, szField, cchField, pchField);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetPropertyProps(
    mdProperty  prop,
    mdTypeDef* pClass,
    // Should be defined as _Out_writes_to_opt_(cchProperty, *pchProperty) and non-const. Mistake from initial release.
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
    if (TypeFromToken(prop) != mdtProperty)
        return E_INVALIDARG;

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), prop, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    mdTypeDef classDef;
    if (!md_find_token_of_range_element(cursor, &classDef))
        return CLDB_E_RECORD_NOTFOUND;
    *pClass = classDef;

    uint32_t flags;
    if (1 != md_get_column_value_as_constant(cursor, mdtProperty_Flags, 1, &flags))
        return CLDB_E_FILE_CORRUPT;
    *pdwPropFlags = flags;

    uint8_t const* sig;
    uint32_t sigLen;
    if (1 != md_get_column_value_as_blob(cursor, mdtProperty_Type, 1, &sig, &sigLen))
        return CLDB_E_FILE_CORRUPT;
    *ppvSig = sig;
    *pbSig = sigLen;

    HRESULT hr;
    RETURN_IF_FAILED(FindConstant(_md_ptr, prop, *pdwCPlusTypeFlag, *ppDefaultValue, *pcchDefaultValue));

    mdcursor_t methodSemCursor;
    uint32_t methodSemCount;
    if (!md_create_cursor(_md_ptr.get(), mdtid_MethodSemantics, &methodSemCursor, &methodSemCount))
        return CLDB_E_RECORD_NOTFOUND;

    struct _Finder
    {
        mdMethodDef Setter;
        mdMethodDef Getter;
        mdMethodDef* Other;
        uint32_t const OtherLen;
        uint32_t OtherCount;
        HRESULT Result; // Result of the operation

        bool operator()(mdcursor_t c)
        {
            mdMethodDef tk;
            uint32_t semantics;
            if (1 != md_get_column_value_as_token(c, mdtMethodSemantics_Method, 1, &tk)
                || 1 != md_get_column_value_as_constant(c, mdtMethodSemantics_Semantics, 1, &semantics))
            {
                Result = CLDB_E_FILE_CORRUPT;
                return true; // Failure detected, so stop.
            }
            switch (semantics)
            {
            case msSetter: Setter = tk;
                break;
            case msGetter: Getter = tk;
                break;
            case msOther:
                if (OtherCount < OtherLen)
                    Other[OtherCount] = tk;
                OtherCount++;
                break;
            default:
                assert(!"Unknown semantic");
            }
            return false;
        }
    } finder{ mdMethodDefNil, mdMethodDefNil, rmdOtherMethod, cMax, 0, S_OK };

    EnumTableRange(methodSemCursor, methodSemCount, mdtMethodSemantics_Association, prop, finder);

    RETURN_IF_FAILED(finder.Result);

    *pmdSetter = finder.Setter;
    *pmdGetter = finder.Getter;
    *pcOtherMethod = finder.OtherCount;

    char const* name;
    if (1 != md_get_column_value_as_utf8(cursor, mdtProperty_Name, 1, &name))
        return CLDB_E_FILE_CORRUPT;

    // The const_cast<> is needed because the signature incorrectly expresses the
    // desired semantics. This has been wrong since .NET Framework 1.0.
    return ConvertAndReturnStringOutput(name, const_cast<WCHAR*>(szProperty), cchProperty, pchProperty);
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
    if (TypeFromToken(tk) != mdtParamDef)
        return E_INVALIDARG;

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), tk, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    mdMethodDef methodDef;
    if (!md_find_token_of_range_element(cursor, &methodDef))
        return CLDB_E_FILE_CORRUPT;
    *pmd = methodDef;

    uint32_t seq;
    if (1 != md_get_column_value_as_constant(cursor, mdtParam_Sequence, 1, &seq))
        return CLDB_E_FILE_CORRUPT;
    *pulSequence = seq;

    uint32_t flags;
    if (1 != md_get_column_value_as_constant(cursor, mdtParam_Flags, 1, &flags))
        return CLDB_E_FILE_CORRUPT;
    *pdwAttr = flags;

    HRESULT hr;
    RETURN_IF_FAILED(FindConstant(_md_ptr, tk, *pdwCPlusTypeFlag, *ppValue, *pcchValue));

    char const* name;
    if (1 != md_get_column_value_as_utf8(cursor, mdtParam_Name, 1, &name))
        return CLDB_E_FILE_CORRUPT;
    return ConvertAndReturnStringOutput(name, szName, cchName, pchName);
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
            if (1 != md_get_column_value_as_blob(cursor, mdtTypeSpec_Signature, 1, &specBlob, &specBlobLen))
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
            return (1 == md_get_column_value_as_utf8(cursor, mdtTypeDef_TypeNamespace, 1, nspace)
                && 1 == md_get_column_value_as_utf8(cursor, mdtTypeDef_TypeName, 1, name))
                ? S_OK
                : CLDB_E_FILE_CORRUPT;
        case mdtTypeRef:
            return (1 == md_get_column_value_as_utf8(cursor, mdtTypeRef_TypeNamespace, 1, nspace)
                && 1 == md_get_column_value_as_utf8(cursor, mdtTypeRef_TypeName, 1, name))
                ? S_OK
                : CLDB_E_FILE_CORRUPT;
        default:
            assert(!"Unexpected token in ResolveTypeDefRefSpecToName");
            return E_FAIL;
        }
    }
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetCustomAttributeByName(
    mdToken     tkObj,
    LPCWSTR     szName,
    void const** ppData,
    ULONG* pcbData)
{
    if (szName == nullptr || ppData == nullptr || pcbData == nullptr)
        return E_INVALIDARG;

    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(_md_ptr.get(), mdtid_CustomAttribute, &cursor, &count))
        return S_FALSE; // If no custom attributes are defined, treat it the same as if the attribute is not found.

    char buffer[1024];
    pal::StringConvert<WCHAR, char> cvt{ szName, buffer };
    if (!cvt.Success())
        return E_INVALIDARG;

    struct
    {
        HRESULT hr;
        void const** ppData;
        ULONG* pcbData;
        pal::StringConvert<WCHAR, char> const& cvt;

        bool operator()(mdcursor_t custAttrCurr)
        {
            char const* nspace;
            char const* name;

            mdcursor_t type;
            mdcursor_t tgtType;
            mdToken typeTk;
            size_t len;
            char const* curr;

            if (1 != md_get_column_value_as_cursor(custAttrCurr, mdtCustomAttribute_Type, 1, &type))
            {
                hr = CLDB_E_FILE_CORRUPT;
                return true;
            }

            // Cursor was returned so must be valid.
            (void)md_cursor_to_token(type, &typeTk);

            // Resolve the cursor based on its type.
            switch (TypeFromToken(typeTk))
            {
            case mdtMethodDef:
                if (!md_find_cursor_of_range_element(type, &tgtType))
                {
                    hr = CLDB_E_FILE_CORRUPT;
                    return true;
                }
                break;
            case mdtMemberRef:
                if (1 != md_get_column_value_as_cursor(type, mdtMemberRef_Class, 1, &tgtType))
                {
                    hr = CLDB_E_FILE_CORRUPT;
                    return true;
                }
                break;
            default:
                assert(!"Unexpected token in GetCustomAttributeByName");
                {
                    hr = COR_E_BADIMAGEFORMAT;
                    return true;
                }
            }

            if (FAILED(hr = ResolveTypeDefRefSpecToName(tgtType, &nspace, &name)))
            {
                return true;
            }
            else
            {
                hr = S_FALSE;
                curr = cvt;
                if (nspace[0] != '\0')
                {
                    len = ::strlen(nspace);
                    if (0 != ::strncmp(cvt, nspace, len))
                        return false;
                    curr += len;

                    // Check for overrun and next character
                    if (cvt.Length() <= len || curr[0] != '.')
                        return false;
                    curr += 1;
                }

                if (0 == ::strcmp(curr, name))
                {
                    uint8_t const* data;
                    uint32_t dataLen;
                    if (1 != md_get_column_value_as_blob(custAttrCurr, mdtCustomAttribute_Value, 1, &data, &dataLen))
                    {
                        hr = CLDB_E_FILE_CORRUPT;
                        return true;
                    }
                    *ppData = data;
                    *pcbData = dataLen;
                    hr = S_OK;
                    return true;
                }
            }
            return false;
        }
    } finder {S_FALSE, ppData, pcbData, cvt};

    EnumTableRange(cursor, count, mdtCustomAttribute_Parent, tkObj, finder);

    if (finder.hr != S_OK)
    {
        *ppData = nullptr;
        *pcbData = 0;
    }
    return finder.hr;
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
    if (TypeFromToken(tdNestedClass) != mdtTypeDef)
        return E_INVALIDARG;

    mdcursor_t cursor;
    uint32_t count;
    mdcursor_t nestedClassRow;
    if (!md_create_cursor(_md_ptr.get(), mdtid_NestedClass, &cursor, &count)
        || !md_find_row_from_cursor(cursor, mdtNestedClass_NestedClass, RidFromToken(tdNestedClass), &nestedClassRow))
    {
        return CLDB_E_RECORD_NOTFOUND;
    }

    mdTypeDef enclosed;
    if (1 != md_get_column_value_as_token(nestedClassRow, mdtNestedClass_EnclosingClass, 1, &enclosed))
        return CLDB_E_FILE_CORRUPT;

    *ptdEnclosingClass = enclosed;
    return S_OK;
}

namespace
{
    uint32_t const FoundValue = 0;
    uint32_t const InvalidReadCount = (uint32_t)-1;

    struct ReadSigContext
    {
        mdhandle_t Handle;
        CorPinvokeMap PinvokeCallConv;
    };

    // II.23.2.8 TypeDefOrRefOrSpecEncoded as potential calling convention
    uint32_t ReadTypeDefOrRefOrSpecEncodedAsCallConv(PCCOR_SIGNATURE sig, ReadSigContext& cxt)
    {
        mdToken tk;
        uint32_t readIn = CorSigUncompressToken(sig, &tk);
        if (IsNilToken(tk) || TypeFromToken(tk) == mdtTypeSpec)
            return readIn;

        // See if this token is a calling convention.
        uint32_t tkType = TypeFromToken(tk);
        if (tkType != mdtTypeRef && tkType != mdtTypeDef)
            return readIn;

        mdcursor_t cursor;
        if (!md_token_to_cursor(cxt.Handle, tk, &cursor))
            return InvalidReadCount;

        col_index_t colNspace;
        col_index_t colName;
        if (tkType == mdtTypeRef)
        {
            colNspace = mdtTypeRef_TypeNamespace;
            colName = mdtTypeRef_TypeName;
        }
        else
        {
            assert(tkType == mdtTypeDef);
            colNspace = mdtTypeDef_TypeNamespace;
            colName = mdtTypeDef_TypeName;
        }

        char const* nspace;
        if (1 != md_get_column_value_as_utf8(cursor, colNspace, 1, &nspace))
            return InvalidReadCount;

        if (0 == ::strcmp(nspace, CMOD_CALLCONV_NAMESPACE) || 0 == ::strcmp(nspace, CMOD_CALLCONV_NAMESPACE_OLD))
        {
            char const* name;
            if (1 != md_get_column_value_as_utf8(cursor, colName, 1, &name))
                return InvalidReadCount;

            if (0 == ::strcmp(name, CMOD_CALLCONV_NAME_CDECL))
            {
                cxt.PinvokeCallConv = pmCallConvCdecl;
                return FoundValue;
            }
            if (0 == ::strcmp(name, CMOD_CALLCONV_NAME_STDCALL))
            {
                cxt.PinvokeCallConv = pmCallConvStdcall;
                return FoundValue;
            }
            if (0 == ::strcmp(name, CMOD_CALLCONV_NAME_THISCALL))
            {
                cxt.PinvokeCallConv = pmCallConvThiscall;
                return FoundValue;
            }
            if (0 == ::strcmp(name, CMOD_CALLCONV_NAME_FASTCALL))
            {
                cxt.PinvokeCallConv = pmCallConvFastcall;
                return FoundValue;
            }
        }
        return readIn;
    }

    // Handles processing the following metadata signature rules.
    //
    // S := MethodDefSig | MethodRefSig | StandAloneMethodSig
    // II.23.2.1  MethodDefSig : = (DEFAULT | VARARG | (GENERIC GenParamCount)) ParamCount RetType Param*
    // II.23.2.2  MethodRefSig : = (DEFAULT | (GENERIC GenParamCount)) ParamCount RetType Param*
    //  | VARARG ParamCount RetType Param* (SENTINEL Param+)?
    // II.23.2.3  StandAloneMethodSig : = (DEFAULT | STDCALL | THISCALL | FASTCALL) ParamCount RetType Param*
    //  | (VARARG | C) ParamCount RetType Param* (SENTINEL Param+)?
    //
    // II.23.2.11 RetType := CustomMod* (BYREF? Type | TYPEDBYREF | VOID)
    // II.23.2.10 Param := CustomMod* (BYREF? Type | TYPEDBYREF)
    // II.23.2.7  CustomMod := (CMOD_OPT | CMOD_REQD) TypeDefOrRefOrSpecEncoded
    // II.23.2.12 Type := BOOLEAN | CHAR | I1 | U1 | I2 | U2 | I4 | U4 | I8 | U8 | R4 | R8 | I | U
    //  | ARRAY Type ArrayShape
    //  | CLASS TypeDefOrRefOrSpecEncoded
    //  | FNPTR MethodDefSig
    //  | FNPTR MethodRefSig
    //  | GENERICINST (CLASS | VALUETYPE) TypeDefOrRefOrSpecEncoded GenArgCount Type*
    //  | MVAR Number
    //  | OBJECT
    //  | PTR CustomMod* Type
    //  | PTR CustomMod* VOID
    //  | STRING
    //  | SZARRAY CustomMod* Type
    //  | VALUETYPE TypeDefOrRefOrSpecEncoded
    //  | VAR Number
    // II.23.2.13 ArrayShape := Rank NumSizes Size* NumLoBounds LoBound*
    //
    // II.23.2.8  TypeDefOrRefOrSpecEncoded := mdToken
    // GenArgCount := uint32
    // ParamCount := uint32
    // Number := uint32
    // Rank := uint32
    // NumSizes := uint32
    // Size := uint32
    // NumLoBounds := uint32
    // LoBound := int32
    uint32_t ReadCorType_CallConv(PCCOR_SIGNATURE sig, PCCOR_SIGNATURE sigEnd, ReadSigContext& cxt)
    {
#define RETURN_IF_NOT_ADVANCE(stmt) \
{\
    if (sigCurr >= sigEnd) return InvalidReadCount; \
    cnt = stmt; \
    if (cnt <= FoundValue) return cnt; \
    sigCurr += cnt; \
}

        assert(sig != nullptr && sigEnd != nullptr);

        PCCOR_SIGNATURE sigCurr = sig;

        // Process modifiers (e.g., PTR or BYREF) and VARARG sentinel values - see MethodDefSig and MethodRefSig.
        CorElementType corType = CorSigUncompressElementType(sigCurr);
        while (CorIsModifierElementType(corType) || corType == ELEMENT_TYPE_SENTINEL)
            corType = CorSigUncompressElementType(sigCurr);

        // Read in CorType
        uint32_t cnt = 0;
        ULONG data;
        ULONG tmp;
        int32_t stmp;
        mdToken tk;
        switch (corType)
        {
        case ELEMENT_TYPE_SZARRAY:
            RETURN_IF_NOT_ADVANCE(ReadCorType_CallConv(sigCurr, sigEnd, cxt)); // CustomMod* Type
            break;
        case ELEMENT_TYPE_VAR:
        case ELEMENT_TYPE_MVAR:
            RETURN_IF_NOT_ADVANCE(CorSigUncompressData(sigCurr, &data)); // Number
            break;
        case ELEMENT_TYPE_GENERICINST:
            RETURN_IF_NOT_ADVANCE(ReadCorType_CallConv(sigCurr, sigEnd, cxt)); // (CLASS | VALUETYPE) TypeDefOrRefOrSpecEncoded
            RETURN_IF_NOT_ADVANCE(CorSigUncompressData(sigCurr, &data)); // GenArgCount
            for (uint32_t i = 0; i < data; ++i)
                RETURN_IF_NOT_ADVANCE(ReadCorType_CallConv(sigCurr, sigEnd, cxt)); // Type*
            break;
        case ELEMENT_TYPE_FNPTR: // MethodDefSig | MethodRefSig
            RETURN_IF_NOT_ADVANCE(CorSigUncompressData(sigCurr, &data)); // Metadata calling convention
            RETURN_IF_NOT_ADVANCE(CorSigUncompressData(sigCurr, &data)); // ParamCount
            RETURN_IF_NOT_ADVANCE(ReadCorType_CallConv(sigCurr, sigEnd, cxt)); // RetType
            for (uint32_t i = 0; i < data; ++i)
                RETURN_IF_NOT_ADVANCE(ReadCorType_CallConv(sigCurr, sigEnd, cxt)); // Type*
            break;
        case ELEMENT_TYPE_ARRAY:
            RETURN_IF_NOT_ADVANCE(ReadCorType_CallConv(sigCurr, sigEnd, cxt)); // Type
            RETURN_IF_NOT_ADVANCE(CorSigUncompressData(sigCurr, &data)); // Rank
            RETURN_IF_NOT_ADVANCE(CorSigUncompressData(sigCurr, &data)); // NumSizes
            for (uint32_t i = 0; i < data; ++i)
                RETURN_IF_NOT_ADVANCE(CorSigUncompressData(sigCurr, &tmp)); // Size*
            RETURN_IF_NOT_ADVANCE(CorSigUncompressData(sigCurr, &data)); // NumLoBounds
            for (uint32_t i = 0; i < data; ++i)
                RETURN_IF_NOT_ADVANCE(CorSigUncompressSignedInt(sigCurr, &stmp)); // LoBounds*
            break;
        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
            RETURN_IF_NOT_ADVANCE(CorSigUncompressToken(sigCurr, &tk)); // TypeDefOrRefOrSpecEncoded
            break;
        case ELEMENT_TYPE_CMOD_REQD:
        case ELEMENT_TYPE_CMOD_OPT:
            RETURN_IF_NOT_ADVANCE(ReadTypeDefOrRefOrSpecEncodedAsCallConv(sigCurr, cxt)); // TypeDefOrRefOrSpecEncoded
            RETURN_IF_NOT_ADVANCE(ReadCorType_CallConv(sigCurr, sigEnd, cxt)); // (Target) Type
            break;
        default:
            break;
        }

        if (sigCurr > sigEnd)
            return InvalidReadCount;

        return (uint32_t)(sigCurr - sig);
#undef RETURN_IF_NOT_ADVANCE
    }
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetNativeCallConvFromSig(
    void const* pvSig,
    ULONG       cbSig,
    ULONG* pCallConv)
{
    if (cbSig == 0 || pCallConv == nullptr)
        return E_INVALIDARG;

    PCCOR_SIGNATURE sig = (PCCOR_SIGNATURE)pvSig;
    PCCOR_SIGNATURE sigEnd = sig + cbSig;
    ULONG callConv; // Metadata callconv position value, not the expected return type.

    // Signature processing specified in ECMA-335 sections:
    //  - II.23.2.1 MethodDefSig
    //  - II.23.2.2 MethodRefSig
    //  - II.23.2.3 StandAloneMethodSig - Primary signature containing native calling conventions
    //
    uint32_t cnt = CorSigUncompressData(sig, &callConv);
    if (cnt == InvalidReadCount)
        return CORSEC_E_INVALID_IMAGE_FORMAT;

    PCCOR_SIGNATURE sigTypeArgs = sig + cnt;
    PCCOR_SIGNATURE sigArgCount = sigTypeArgs;

    // Check for generic signature, defined in II.23.2.1.
    if (callConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        ULONG typeArgs;
        cnt = CorSigUncompressData(sigTypeArgs, &typeArgs);
        if (cnt == InvalidReadCount)
            return CORSEC_E_INVALID_IMAGE_FORMAT;
        sigArgCount = sigTypeArgs + cnt;
    }

    // Read in signature arg count.
    assert(sigArgCount < sigEnd);
    ULONG argCount;
    cnt = CorSigUncompressData(sigArgCount, &argCount);
    if (cnt == InvalidReadCount)
        return CORSEC_E_INVALID_IMAGE_FORMAT;

    ReadSigContext cxt{ _md_ptr.get(), pmCallConvWinapi };

    PCCOR_SIGNATURE sigRetType = sigArgCount + cnt;
    assert(sigRetType < sigEnd);
    // Process the return type - II.23.2.11.
    cnt = ReadCorType_CallConv(sigRetType, sigEnd, cxt);
    if (cnt == InvalidReadCount)
        return CORSEC_E_INVALID_IMAGE_FORMAT;

    // Check if calling convention was found. If not
    // found, continue looking on the arguments.
    if (cnt != 0)
    {
        PCCOR_SIGNATURE sigArgs = sigRetType + cnt;
        PCCOR_SIGNATURE sigCurrArg = sigArgs;
        // Process arguments - II.23.2.10.
        for (uint32_t i = 0; i < argCount; ++i)
        {
            assert(sigCurrArg < sigEnd);

            cnt = ReadCorType_CallConv(sigCurrArg, sigEnd, cxt);
            if (cnt == InvalidReadCount)
                return CORSEC_E_INVALID_IMAGE_FORMAT;

            if (cnt == FoundValue)
                goto Done;

            // Move to the next argument
            sigCurrArg = sigCurrArg + cnt;
        }
    }
Done:
    *pCallConv = cxt.PinvokeCallConv;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::IsGlobal(
    mdToken     pd,
    int* pbGlobal)
{
    if (IsValidToken(pd) != TRUE)
        return E_INVALIDARG;

    mdToken parent;
    mdcursor_t cursor;
    BOOL result;
    switch (TypeFromToken(pd))
    {
    case mdtTypeDef:
        result = pd == MD_GLOBAL_PARENT_TOKEN ? TRUE : FALSE;
        break;
    case mdtFieldDef:
    case mdtMethodDef:
    case mdtEvent:
    case mdtProperty:
        if (!md_token_to_cursor(_md_ptr.get(), pd, &cursor))
            return CLDB_E_RECORD_NOTFOUND;
        if (!md_find_token_of_range_element(cursor, &parent))
            return CLDB_E_FILE_CORRUPT;
        result = parent == MD_GLOBAL_PARENT_TOKEN ? TRUE : FALSE;
        break;
    default:
        result = FALSE;
    }

    *pbGlobal = result;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumGenericParams(
    HCORENUM* phEnum,
    mdToken      tk,
    mdGenericParam rGenericParams[],
    ULONG       cMax,
    ULONG* pcGenericParams)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        mdcursor_t cursor;
        uint32_t count;
        if (!md_create_cursor(_md_ptr.get(), mdtid_GenericParam, &cursor, &count))
            return CLDB_E_RECORD_NOTFOUND;

        RETURN_IF_FAILED(HCORENUMImpl::CreateDynamicEnum(&enumImpl));
        HCORENUMImpl_ptr cleanup{ enumImpl };

        struct _Finder
        {
            HCORENUMImpl& EnumImpl;
            mdToken Token;
            HRESULT hr;
            HRESULT Result; // Result of the operation

            bool operator()(mdcursor_t c)
            {
                (void)md_cursor_to_token(c, &Token);
                if (FAILED(hr = HCORENUMImpl::AddToDynamicEnum(EnumImpl, Token)))
                {
                    Result = hr;
                    return true;
                }

                return false;
            }
        } finder{ *enumImpl, mdTokenNil, S_OK, S_OK };

        EnumTableRange(cursor, count, mdtGenericParam_Owner, tk, finder);
        RETURN_IF_FAILED(finder.Result);

        *phEnum = cleanup.release();
    }
    return enumImpl->ReadTokens(rGenericParams, cMax, pcGenericParams);
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
    UNREFERENCED_PARAMETER(reserved);

    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), gp, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    if (1 != md_get_column_value_as_token(cursor, mdtGenericParam_Owner, 1, ptOwner))
        return CLDB_E_FILE_CORRUPT;

    uint32_t sequenceNumber;
    if (1 != md_get_column_value_as_constant(cursor, mdtGenericParam_Number, 1, &sequenceNumber))
        return CLDB_E_FILE_CORRUPT;
    *pulParamSeq = sequenceNumber;

    uint32_t paramFlags;
    if (1 != md_get_column_value_as_constant(cursor, mdtGenericParam_Flags, 1, &paramFlags))
        return CLDB_E_FILE_CORRUPT;
    *pdwParamFlags = paramFlags;

    char const* name;
    if (1 != md_get_column_value_as_utf8(cursor, mdtGenericParam_Name, 1, &name))
        return CLDB_E_FILE_CORRUPT;

    return ConvertAndReturnStringOutput(name, wzname, cchName, pchName);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetMethodSpecProps(
    mdMethodSpec mi,
    mdToken* tkParent,
    PCCOR_SIGNATURE* ppvSigBlob,
    ULONG* pcbSigBlob)
{
    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), mi, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    if (1 != md_get_column_value_as_token(cursor, mdtMethodSpec_Method, 1, tkParent))
        return CLDB_E_FILE_CORRUPT;

    uint8_t const* sig;
    uint32_t sigLen;
    if (1 != md_get_column_value_as_blob(cursor, mdtMethodSpec_Instantiation, 1, &sig, &sigLen))
        return CLDB_E_FILE_CORRUPT;

    *ppvSigBlob = (PCCOR_SIGNATURE)sig;
    *pcbSigBlob = sigLen;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumGenericParamConstraints(
    HCORENUM* phEnum,
    mdGenericParam tk,
    mdGenericParamConstraint rGenericParamConstraints[],
    ULONG       cMax,
    ULONG* pcGenericParamConstraints)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        mdcursor_t cursor;
        uint32_t count;
        if (!md_create_cursor(_md_ptr.get(), mdtid_GenericParamConstraint, &cursor, &count))
            return CLDB_E_RECORD_NOTFOUND;

        RETURN_IF_FAILED(HCORENUMImpl::CreateDynamicEnum(&enumImpl));
        HCORENUMImpl_ptr cleanup{ enumImpl };

        struct _Finder
        {
            HCORENUMImpl& EnumImpl;
            mdToken Token;
            HRESULT hr;
            HRESULT Result; // Result of the operation

            bool operator()(mdcursor_t c)
            {
                (void)md_cursor_to_token(c, &Token);
                if (FAILED(hr = HCORENUMImpl::AddToDynamicEnum(EnumImpl, Token)))
                {
                    Result = hr;
                    return true;
                }

                return false;
            }
        } finder{ *enumImpl, mdTokenNil, S_OK, S_OK };

        EnumTableRange(cursor, count, mdtGenericParamConstraint_Owner, tk, finder);
        RETURN_IF_FAILED(finder.Result);

        *phEnum = cleanup.release();
    }
    return enumImpl->ReadTokens(rGenericParamConstraints, cMax, pcGenericParamConstraints);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetGenericParamConstraintProps(
    mdGenericParamConstraint gpc,
    mdGenericParam* ptGenericParam,
    mdToken* ptkConstraintType)
{
    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), gpc, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    if (1 != md_get_column_value_as_token(cursor, mdtGenericParamConstraint_Owner, 1, ptGenericParam))
        return CLDB_E_FILE_CORRUPT;

    if (1 != md_get_column_value_as_token(cursor, mdtGenericParamConstraint_Constraint, 1, ptkConstraintType))
        return CLDB_E_FILE_CORRUPT;

    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetPEKind(
    DWORD* pdwPEKind,
    DWORD* pdwMAchine)
{
    UNREFERENCED_PARAMETER(pdwPEKind);
    UNREFERENCED_PARAMETER(pdwMAchine);

    // Requires PE data to be available.
    // This implementation only has the metadata tables.
    // It does not have any information about the PE envelope.
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetVersionString(
    _Out_writes_to_opt_(ccBufSize, *pccBufSize)
    LPWSTR      pwzBuf,
    DWORD       ccBufSize,
    DWORD* pccBufSize)
{
    char const* versionString = md_get_version_string(_md_ptr.get());
    if (versionString == nullptr)
        versionString = "";
    return ConvertAndReturnStringOutput(versionString, pwzBuf, ccBufSize, pccBufSize);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumMethodSpecs(
    HCORENUM* phEnum,
    mdToken      tk,
    mdMethodSpec rMethodSpecs[],
    ULONG       cMax,
    ULONG* pcMethodSpecs)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        mdcursor_t cursor;
        uint32_t count;
        if (!md_create_cursor(_md_ptr.get(), mdtid_MethodSpec, &cursor, &count))
            return CLDB_E_RECORD_NOTFOUND;

        RETURN_IF_FAILED(HCORENUMImpl::CreateDynamicEnum(&enumImpl));
        HCORENUMImpl_ptr cleanup{ enumImpl };

        struct _Finder
        {
            HCORENUMImpl& EnumImpl;
            mdToken Token;
            HRESULT hr;
            HRESULT Result; // Result of the operation

            bool operator()(mdcursor_t c)
            {
                (void)md_cursor_to_token(c, &Token);
                if (FAILED(hr = HCORENUMImpl::AddToDynamicEnum(EnumImpl, Token)))
                {
                    Result = hr;
                    return true;
                }

                return false;
            }
        } finder{ *enumImpl, mdTokenNil, S_OK, S_OK };

        EnumTableRange(cursor, count, mdtMethodSpec_Method, tk, finder);
        RETURN_IF_FAILED(finder.Result);

        *phEnum = cleanup.release();
    }
    return enumImpl->ReadTokens(rMethodSpecs, cMax, pcMethodSpecs);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetAssemblyProps(
    mdAssembly  mda,
    void const  **ppbPublicKey,
    ULONG       *pcbPublicKey,
    ULONG       *pulHashAlgId,
    _Out_writes_to_opt_(cchName, *pchName)
    LPWSTR      szName,
    ULONG       cchName,
    ULONG       *pchName,
    ASSEMBLYMETADATA *pMetaData,
    DWORD       *pdwAssemblyFlags)
{
    mdcursor_t cursor;

    if (!md_token_to_cursor(_md_ptr.get(), mda, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    if (ppbPublicKey != nullptr)
    {
        uint8_t const* publicKey;
        uint32_t publicKeySize;
        if (1 != md_get_column_value_as_blob(cursor, mdtAssembly_PublicKey, 1, &publicKey, &publicKeySize))
            return CLDB_E_FILE_CORRUPT;

        *ppbPublicKey = publicKey;
        *pcbPublicKey = publicKeySize;
    }

    if (pulHashAlgId != nullptr)
    {
        uint32_t hashAlgId;
        if (1 != md_get_column_value_as_constant(cursor, mdtAssembly_HashAlgId, 1, &hashAlgId))
            return CLDB_E_FILE_CORRUPT;
        *pulHashAlgId = hashAlgId;
    }

    if (pMetaData != nullptr)
    {
        uint32_t majorVersion;
        uint32_t minorVersion;
        uint32_t buildNumber;
        uint32_t patchNumber;
        if (1 != md_get_column_value_as_constant(cursor, mdtAssembly_MajorVersion, 1, &majorVersion))
            return CLDB_E_FILE_CORRUPT;

        if (1 != md_get_column_value_as_constant(cursor, mdtAssembly_MinorVersion, 1, &minorVersion))
            return CLDB_E_FILE_CORRUPT;

        if (1 != md_get_column_value_as_constant(cursor, mdtAssembly_BuildNumber, 1, &buildNumber))
            return CLDB_E_FILE_CORRUPT;

        if (1 != md_get_column_value_as_constant(cursor, mdtAssembly_RevisionNumber, 1, &patchNumber))
            return CLDB_E_FILE_CORRUPT;

        pMetaData->usMajorVersion = static_cast<uint16_t>(majorVersion);
        pMetaData->usMinorVersion = static_cast<uint16_t>(minorVersion);
        pMetaData->usBuildNumber = static_cast<uint16_t>(buildNumber);
        pMetaData->usRevisionNumber = static_cast<uint16_t>(patchNumber);

        char const* culture;
        if (1 != md_get_column_value_as_utf8(cursor, mdtAssembly_Culture, 1, &culture))
            return CLDB_E_FILE_CORRUPT;

        HRESULT hr;
        RETURN_IF_FAILED(ConvertAndReturnStringOutput(culture, pMetaData->szLocale, pMetaData->cbLocale, &pMetaData->cbLocale));

        // We do not read the AssemblyOS or AssemblyProcessor tables to fill these arrays since neither .NET Framework or CoreCLR do.
        pMetaData->ulProcessor = 0;
        pMetaData->ulOS = 0;
    }

    if (pdwAssemblyFlags != nullptr)
    {
        uint32_t assemblyFlags;
        if (1 != md_get_column_value_as_constant(cursor, mdtAssembly_Flags, 1, &assemblyFlags))
            return CLDB_E_FILE_CORRUPT;

        uint8_t const* publicKey;
        uint32_t publicKeySize;
        if (1 != md_get_column_value_as_blob(cursor, mdtAssembly_PublicKey, 1, &publicKey, &publicKeySize))
            return CLDB_E_FILE_CORRUPT;
        if (publicKeySize != 0)
            assemblyFlags |= afPublicKey;

        *pdwAssemblyFlags = assemblyFlags;
    }

    if (szName != nullptr || pchName != nullptr)
    {
        char const* name;
        if (1 != md_get_column_value_as_utf8(cursor, mdtAssembly_Name, 1, &name))
            return CLDB_E_FILE_CORRUPT;

        return ConvertAndReturnStringOutput(name, szName, cchName, pchName);
    }
    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetAssemblyRefProps(
    mdAssemblyRef mdar,
    void const  **ppbPublicKeyOrToken,
    ULONG       *pcbPublicKeyOrToken,
    _Out_writes_to_opt_(cchName, *pchName)
    LPWSTR      szName,
    ULONG       cchName,
    ULONG       *pchName,
    ASSEMBLYMETADATA *pMetaData,
    void const  **ppbHashValue,
    ULONG       *pcbHashValue,
    DWORD       *pdwAssemblyRefFlags)
{
    mdcursor_t cursor;

    if (!md_token_to_cursor(_md_ptr.get(), mdar, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    if (ppbPublicKeyOrToken != nullptr)
    {
        uint8_t const* publicKeyOrToken;
        uint32_t publicKeyOrTokenSize;
        if (1 != md_get_column_value_as_blob(cursor, mdtAssemblyRef_PublicKeyOrToken, 1, &publicKeyOrToken, &publicKeyOrTokenSize))
            return CLDB_E_FILE_CORRUPT;

        *ppbPublicKeyOrToken = publicKeyOrToken;
        *pcbPublicKeyOrToken = publicKeyOrTokenSize;
    }

    if (ppbHashValue != nullptr)
    {
        uint8_t const* hashValue;
        uint32_t hashValueSize;
        if (1 != md_get_column_value_as_blob(cursor, mdtAssemblyRef_HashValue, 1, &hashValue, &hashValueSize))
            return CLDB_E_FILE_CORRUPT;
        *ppbHashValue = hashValue;
        *pcbHashValue = hashValueSize;
    }

    if (pMetaData != nullptr)
    {
        uint32_t majorVersion;
        uint32_t minorVersion;
        uint32_t buildNumber;
        uint32_t patchNumber;
        if (1 != md_get_column_value_as_constant(cursor, mdtAssemblyRef_MajorVersion, 1, &majorVersion))
            return CLDB_E_FILE_CORRUPT;

        if (1 != md_get_column_value_as_constant(cursor, mdtAssemblyRef_MinorVersion, 1, &minorVersion))
            return CLDB_E_FILE_CORRUPT;

        if (1 != md_get_column_value_as_constant(cursor, mdtAssemblyRef_BuildNumber, 1, &buildNumber))
            return CLDB_E_FILE_CORRUPT;

        if (1 != md_get_column_value_as_constant(cursor, mdtAssemblyRef_RevisionNumber, 1, &patchNumber))
            return CLDB_E_FILE_CORRUPT;

        pMetaData->usMajorVersion = static_cast<uint16_t>(majorVersion);
        pMetaData->usMinorVersion = static_cast<uint16_t>(minorVersion);
        pMetaData->usBuildNumber = static_cast<uint16_t>(buildNumber);
        pMetaData->usRevisionNumber = static_cast<uint16_t>(patchNumber);

        char const* culture;
        if (1 != md_get_column_value_as_utf8(cursor, mdtAssemblyRef_Culture, 1, &culture))
            return CLDB_E_FILE_CORRUPT;

        HRESULT hr;
        RETURN_IF_FAILED(ConvertAndReturnStringOutput(culture, pMetaData->szLocale, pMetaData->cbLocale, &pMetaData->cbLocale));

        // We do not read the AssemblyRefOS or AssemblyRefProcessor tables to fill these arrays since neither .NET Framework or CoreCLR do.
        pMetaData->ulProcessor = 0;
        pMetaData->ulOS = 0;
    }

    if (pdwAssemblyRefFlags != nullptr)
    {
        uint32_t assemblyRefFlags;
        if (1 != md_get_column_value_as_constant(cursor, mdtAssemblyRef_Flags, 1, &assemblyRefFlags))
            return CLDB_E_FILE_CORRUPT;

        *pdwAssemblyRefFlags = assemblyRefFlags;
    }

    if (szName != nullptr || pchName != nullptr)
    {
        char const* name;
        if (1 != md_get_column_value_as_utf8(cursor, mdtAssemblyRef_Name, 1, &name))
            return CLDB_E_FILE_CORRUPT;

        return ConvertAndReturnStringOutput(name, szName, cchName, pchName);
    }
    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetFileProps(
    mdFile      mdf,
    _Out_writes_to_opt_(cchName, *pchName)
    LPWSTR      szName,
    ULONG       cchName,
    ULONG       *pchName,
    void const  **ppbHashValue,
    ULONG       *pcbHashValue,
    DWORD       *pdwFileFlags)
{
    mdcursor_t cursor;

    if (!md_token_to_cursor(_md_ptr.get(), mdf, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    if (ppbHashValue != nullptr)
    {
        uint8_t const* hashValue;
        uint32_t hashValueSize;
        if (1 != md_get_column_value_as_blob(cursor, mdtFile_HashValue, 1, &hashValue, &hashValueSize))
            return CLDB_E_FILE_CORRUPT;
        *ppbHashValue = hashValue;
        *pcbHashValue = hashValueSize;
    }

    if (pdwFileFlags != nullptr)
    {
        uint32_t fileFlags;
        if (1 != md_get_column_value_as_constant(cursor, mdtFile_Flags, 1, &fileFlags))
            return CLDB_E_FILE_CORRUPT;

        *pdwFileFlags = fileFlags;
    }

    if (szName != nullptr || pchName != nullptr)
    {
        char const* name;
        if (1 != md_get_column_value_as_utf8(cursor, mdtFile_Name, 1, &name))
            return CLDB_E_FILE_CORRUPT;

        return ConvertAndReturnStringOutput(name, szName, cchName, pchName);
    }
    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetExportedTypeProps(
    mdExportedType   mdct,
    _Out_writes_to_opt_(cchName, *pchName)
    LPWSTR      szName,
    ULONG       cchName,
    ULONG       *pchName,
    mdToken     *ptkImplementation,
    mdTypeDef   *ptkTypeDef,
    DWORD       *pdwExportedTypeFlags)
{
    mdcursor_t cursor;

    if (!md_token_to_cursor(_md_ptr.get(), mdct, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    if (ptkImplementation != nullptr)
    {
        if (1 != md_get_column_value_as_token(cursor, mdtExportedType_Implementation, 1, ptkImplementation))
            return CLDB_E_FILE_CORRUPT;
    }

    if (ptkTypeDef != nullptr)
    {
        // This column points into the TypeDef table of another module,
        // so it isn't a true column reference here.
        if (1 != md_get_column_value_as_constant(cursor, mdtExportedType_TypeDefId, 1, ptkTypeDef))
            return CLDB_E_FILE_CORRUPT;
    }

    if (pdwExportedTypeFlags != nullptr)
    {
        uint32_t exportedTypeFlags;
        if (1 != md_get_column_value_as_constant(cursor, mdtExportedType_Flags, 1, &exportedTypeFlags))
            return CLDB_E_FILE_CORRUPT;

        *pdwExportedTypeFlags = exportedTypeFlags;
    }

    if (szName != nullptr || pchName != nullptr)
    {
        char const* name;
        if (1 != md_get_column_value_as_utf8(cursor, mdtExportedType_TypeName, 1, &name))
            return CLDB_E_FILE_CORRUPT;

        char const* nspace;
        if (1 != md_get_column_value_as_utf8(cursor, mdtExportedType_TypeNamespace, 1, &nspace))
            return CLDB_E_FILE_CORRUPT;

        HRESULT hr;
        malloc_ptr<char> mem;
        RETURN_IF_FAILED(ConstructTypeName(nspace, name, mem));

        return ConvertAndReturnStringOutput((char*)mem.get(), szName, cchName, pchName);
    }
    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetManifestResourceProps(
    mdManifestResource  mdmr,
    _Out_writes_to_opt_(cchName, *pchName)
    LPWSTR      szName,
    ULONG       cchName,
    ULONG       *pchName,
    mdToken     *ptkImplementation,
    DWORD       *pdwOffset,
    DWORD       *pdwResourceFlags)
{
    mdcursor_t cursor;

    if (!md_token_to_cursor(_md_ptr.get(), mdmr, &cursor))
        return CLDB_E_RECORD_NOTFOUND;

    if (ptkImplementation != nullptr)
    {
        if (1 != md_get_column_value_as_token(cursor, mdtManifestResource_Implementation, 1, ptkImplementation))
            return CLDB_E_FILE_CORRUPT;
    }

    if (pdwOffset != nullptr)
    {
        uint32_t offset;
        if (1 != md_get_column_value_as_constant(cursor, mdtManifestResource_Offset, 1, &offset))
            return CLDB_E_FILE_CORRUPT;

        *pdwOffset = offset;
    }

    if (pdwResourceFlags != nullptr)
    {
        uint32_t resourceFlags;
        if (1 != md_get_column_value_as_constant(cursor, mdtManifestResource_Flags, 1, &resourceFlags))
            return CLDB_E_FILE_CORRUPT;

        *pdwResourceFlags = resourceFlags;
    }
    if (szName != nullptr || pchName != nullptr)
    {
        char const* name;
        if (1 != md_get_column_value_as_utf8(cursor, mdtManifestResource_Name, 1, &name))
            return CLDB_E_FILE_CORRUPT;

        return ConvertAndReturnStringOutput(name, szName, cchName, pchName);
    }
    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumAssemblyRefs(
    HCORENUM    *phEnum,
    mdAssemblyRef rAssemblyRefs[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        RETURN_IF_FAILED(CreateEnumTokens(_md_ptr.get(), mdtid_AssemblyRef, &enumImpl));
        *phEnum = enumImpl;
    }
    return enumImpl->ReadTokens(rAssemblyRefs, cMax, pcTokens);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumFiles(
    HCORENUM    *phEnum,
    mdFile      rFiles[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        RETURN_IF_FAILED(CreateEnumTokens(_md_ptr.get(), mdtid_File, &enumImpl));
        *phEnum = enumImpl;
    }
    return enumImpl->ReadTokens(rFiles, cMax, pcTokens);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumExportedTypes(
    HCORENUM    *phEnum,
    mdExportedType   rExportedTypes[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        RETURN_IF_FAILED(CreateEnumTokens(_md_ptr.get(), mdtid_ExportedType, &enumImpl));
        *phEnum = enumImpl;
    }
    return enumImpl->ReadTokens(rExportedTypes, cMax, pcTokens);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumManifestResources(
    HCORENUM    *phEnum,
    mdManifestResource  rManifestResources[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToHCORENUMImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        RETURN_IF_FAILED(CreateEnumTokens(_md_ptr.get(), mdtid_ManifestResource, &enumImpl));
        *phEnum = enumImpl;
    }
    return enumImpl->ReadTokens(rManifestResources, cMax, pcTokens);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetAssemblyFromScope(
    mdAssembly  *ptkAssembly)
{
    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(_md_ptr.get(), mdtid_Assembly, &cursor, &count))
        return CLDB_E_RECORD_NOTFOUND;
    if (!md_cursor_to_token(cursor, ptkAssembly))
        return CLDB_E_FILE_CORRUPT;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::FindExportedTypeByName(
    LPCWSTR     szName,
    mdToken     mdtExportedType,
    mdExportedType   *ptkExportedType)
{
    if (szName == nullptr)
        return E_INVALIDARG;

    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(_md_ptr.get(), mdtid_ExportedType, &cursor, &count))
        return CLDB_E_RECORD_NOTFOUND;

    pal::StringConvert<WCHAR, char> cvt{ szName };
    if (!cvt.Success())
        return E_INVALIDARG;
    char const* nspace;
    char const* name;
    SplitTypeName(cvt, &nspace, &name);

    for (uint32_t i = 0; i < count; md_cursor_next(&cursor), i++)
    {
        mdToken implementation;
        if (1 != md_get_column_value_as_token(cursor, mdtExportedType_Implementation, 1, &implementation))
            return CLDB_E_FILE_CORRUPT;

        // Handle the case of nested vs. non-nested classes
        if (TypeFromToken(implementation) == CorTokenType::mdtExportedType && !IsNilToken(implementation))
        {
            // Current ExportedType being looked at is a nested type, so
            // comparing the implementation token.
            if (implementation != mdtExportedType)
                continue;
        }
        else if (TypeFromToken(mdtExportedType) == mdtExportedType
                && !IsNilToken(mdtExportedType))
        {
            // ExportedType passed in is nested but the current ExportedType is not.
            continue;
        }

        char const* recordNspace;
        if (1 != md_get_column_value_as_utf8(cursor, mdtExportedType_TypeNamespace, 1, &recordNspace))
            return CLDB_E_FILE_CORRUPT;

        if (::strcmp(nspace, recordNspace) != 0)
            continue;

        char const* recordName;
        if (1 != md_get_column_value_as_utf8(cursor, mdtExportedType_TypeName, 1, &recordName))
            return CLDB_E_FILE_CORRUPT;

        if (::strcmp(name, recordName) != 0)
            continue;

        if (!md_cursor_to_token(cursor, ptkExportedType))
            return CLDB_E_FILE_CORRUPT;
        return S_OK;
    }
    return CLDB_E_RECORD_NOTFOUND;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::FindManifestResourceByName(
    LPCWSTR     szName,
    mdManifestResource *ptkManifestResource)
{
    if (szName == nullptr)
        return E_INVALIDARG;

    mdcursor_t cursor;
    uint32_t count;
    if (!md_create_cursor(_md_ptr.get(), mdtid_ManifestResource, &cursor, &count))
        return CLDB_E_RECORD_NOTFOUND;

    pal::StringConvert<WCHAR, char> cvt{ szName };
    if (!cvt.Success())
        return E_INVALIDARG;

    for (uint32_t i = 0; i < count; md_cursor_next(&cursor), i++)
    {
        mdManifestResource token;
        if (!md_cursor_to_token(cursor, &token))
            return CLDB_E_FILE_CORRUPT;

        char const* name;
        if (1 != md_get_column_value_as_utf8(cursor, mdtManifestResource_Name, 1, &name))
            return CLDB_E_FILE_CORRUPT;

        if (::strncmp(name, cvt, cvt.Length()) == 0)
        {
            *ptkManifestResource = token;
            return S_OK;
        }
    }
    return CLDB_E_RECORD_NOTFOUND;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::FindAssembliesByName(
    LPCWSTR  szAppBase,
    LPCWSTR  szPrivateBin,
    LPCWSTR  szAssemblyName,
    IUnknown *ppIUnk[],
    ULONG    cMax,
    ULONG    *pcAssemblies)
{
    UNREFERENCED_PARAMETER(szAppBase);
    UNREFERENCED_PARAMETER(szPrivateBin);
    UNREFERENCED_PARAMETER(szAssemblyName);
    UNREFERENCED_PARAMETER(ppIUnk);
    UNREFERENCED_PARAMETER(cMax);
    UNREFERENCED_PARAMETER(pcAssemblies);
    // Requires VM knowledge and is only supported in .NET Framework.
    return E_NOTIMPL;
}
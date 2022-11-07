#include <cassert>

#include "impl.hpp"

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
    // Represents a singly linked list enumerator
    struct HCORENUMImpl
    {
        // Cursors for table walking
        mdcursor_t current;
        mdcursor_t start;
        uint32_t readIn;
        uint32_t total;

        // Cursor for user string heap walking
        mduserstringcursor_t us_current;

        HCORENUMImpl* next;
    };

    HCORENUMImpl* ToEnumImpl(HCORENUM hEnum) noexcept
    {
        return reinterpret_cast<HCORENUMImpl*>(hEnum);
    }

    HCORENUM ToEnum(HCORENUMImpl* enumImpl) noexcept
    {
        return reinterpret_cast<HCORENUM>(enumImpl);
    }

    HRESULT CreateHCORENUMImpl(_In_ size_t count, _Out_ HCORENUMImpl** pEnumImpl) noexcept
    {
        HCORENUMImpl* enumImpl;
        enumImpl = (HCORENUMImpl*)::malloc(sizeof(*enumImpl) * count);
        if (enumImpl == nullptr)
            return E_OUTOFMEMORY;

        HCORENUMImpl* prev = enumImpl;
        prev->next = nullptr;
        for (size_t i = 1; i < count; ++i)
        {
            prev->next = &enumImpl[i];
            prev = prev->next;
            prev->next = nullptr;
        }

        *pEnumImpl = enumImpl;
        return S_OK;
    }

    void InitHCORENUMImpl(_Inout_ HCORENUMImpl* enumImpl, _In_ mduserstringcursor_t cursor, _In_ uint32_t count) noexcept
    {
        ::memset(enumImpl, 0, sizeof(*enumImpl));
        enumImpl->total = count;
        enumImpl->us_current = cursor;
    }

    void InitHCORENUMImpl(_Inout_ HCORENUMImpl* enumImpl, _In_ mdcursor_t cursor, _In_ uint32_t rows) noexcept
    {
        enumImpl->current = cursor;
        enumImpl->start = cursor;
        enumImpl->readIn = 0;
        enumImpl->total = rows;
        enumImpl->us_current = (mduserstringcursor_t)~0; // Used create an invalid cursor.
    }

    void DestroyHCORENUM(HCORENUM hEnum) noexcept
    {
        ::free(ToEnumImpl(hEnum));
    }
}

void STDMETHODCALLTYPE MetadataImportRO::CloseEnum(HCORENUM hEnum)
{
    DestroyHCORENUM(hEnum);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::CountEnum(HCORENUM hEnum, ULONG* pulCount)
{
    if (pulCount == nullptr)
        return E_INVALIDARG;

    HCORENUMImpl* enumImpl = ToEnumImpl(hEnum);

    // Accumulate all tables in the enumerator
    uint32_t count = 0;
    while (enumImpl != nullptr)
    {
        count += enumImpl->total;
        enumImpl = enumImpl->next;
    }

    *pulCount = count;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::ResetEnum(HCORENUM hEnum, ULONG ulPos)
{
    HCORENUMImpl* enumImpl = ToEnumImpl(hEnum);
    if (enumImpl == nullptr)
        return S_OK;

    mdcursor_t newStart;
    uint32_t newReadIn;
    bool reset = false;
    while (enumImpl != nullptr)
    {
        newStart = enumImpl->start;
        if (reset)
        {
            // Reset the enumerator state
            newReadIn = 0;
        }
        else if (ulPos < enumImpl->total)
        {
            // The current enumerator contains the position
            if (!md_cursor_move(&newStart, ulPos))
                return E_INVALIDARG;
            newReadIn = ulPos;
            reset = true;
        }
        else
        {
            // The current enumerator is consumed based on position
            ulPos -= enumImpl->total;
            if (!md_cursor_move(&newStart, enumImpl->total))
                return E_INVALIDARG;
            newReadIn = enumImpl->total;
        }

        enumImpl->current = newStart;
        enumImpl->readIn = newReadIn;
        enumImpl = enumImpl->next;
    }

    return S_OK;
}

namespace
{
    HRESULT ReadFromEnum(
        HCORENUMImpl* enumImpl,
        mdToken rTokens[],
        ULONG cMax,
        ULONG* pcTokens)
    {
        assert(enumImpl != nullptr && rTokens != nullptr);

        uint32_t count = 0;
        for (uint32_t i = 0; i < cMax; ++i)
        {
            // Check if all values have been read.
            while (enumImpl->readIn == enumImpl->total)
            {
                enumImpl = enumImpl->next;
                // Check next link in enumerator list
                if (enumImpl == nullptr)
                    goto Done;
            }

            if (!md_cursor_to_token(enumImpl->current, &rTokens[count]))
                break;
            count++;

            if (!md_cursor_next(&enumImpl->current))
                break;
            enumImpl->readIn++;
        }
Done:
        if (pcTokens != nullptr)
            *pcTokens = count;

        return S_OK;
    }

    HRESULT EnumTokens(
        mdhandle_t mdhandle,
        mdtable_id_t mdtid,
        HCORENUM* phEnum,
        mdToken rTokens[],
        ULONG cMax,
        ULONG* pcTokens)
    {
        HRESULT hr;
        HCORENUMImpl* enumImpl = ToEnumImpl(*phEnum);
        if (enumImpl == nullptr)
        {
            mdcursor_t cursor;
            uint32_t rows;
            if (!md_create_cursor(mdhandle, mdtid, &cursor, &rows))
                return E_INVALIDARG;

            RETURN_IF_FAILED(CreateHCORENUMImpl(1, &enumImpl));
            InitHCORENUMImpl(enumImpl, cursor, rows);
            *phEnum = enumImpl;
        }

        return ReadFromEnum(enumImpl, rTokens, cMax, pcTokens);
    }

    HRESULT EnumTokenRange(
        mdhandle_t mdhandle,
        mdToken token,
        col_index_t column,
        HCORENUM* phEnum,
        mdToken rTokens[],
        ULONG cMax,
        ULONG* pcTokens)
    {
        HRESULT hr;
        HCORENUMImpl* enumImpl = ToEnumImpl(*phEnum);
        if (enumImpl == nullptr)
        {
            mdcursor_t cursor;
            if (!md_token_to_cursor(mdhandle, token, &cursor))
                return CLDB_E_INDEX_NOTFOUND;

            mdcursor_t begin;
            uint32_t count;
            if (!md_get_column_value_as_range(cursor, column, &begin, &count))
                return CLDB_E_FILE_CORRUPT;

            RETURN_IF_FAILED(CreateHCORENUMImpl(1, &enumImpl));
            InitHCORENUMImpl(enumImpl, begin, count);
            *phEnum = enumImpl;
        }

        return ReadFromEnum(enumImpl, rTokens, cMax, pcTokens);
    }
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumTypeDefs(
    HCORENUM* phEnum,
    mdTypeDef rTypeDefs[],
    ULONG cMax,
    ULONG* pcTypeDefs)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToEnumImpl(*phEnum);
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

        RETURN_IF_FAILED(CreateHCORENUMImpl(1, &enumImpl));
        InitHCORENUMImpl(enumImpl, cursor, rows);
        *phEnum = enumImpl;
    }

    return ReadFromEnum(enumImpl, rTypeDefs, cMax, pcTypeDefs);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumInterfaceImpls(
    HCORENUM* phEnum,
    mdTypeDef td,
    mdInterfaceImpl rImpls[],
    ULONG cMax,
    ULONG* pcImpls)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToEnumImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        mdcursor_t cursor;
        uint32_t rows;
        if (!md_create_cursor(_md_ptr.get(), mdtid_InterfaceImpl, &cursor, &rows))
            return E_INVALIDARG;

        uint32_t id = RidFromToken(td);
        if (!md_find_range_from_cursor(cursor, mdtInterfaceImpl_Class, id, &cursor, &rows))
            return CLDB_E_FILE_CORRUPT;

        RETURN_IF_FAILED(CreateHCORENUMImpl(1, &enumImpl));
        InitHCORENUMImpl(enumImpl, cursor, rows);
        *phEnum = enumImpl;
    }

    return ReadFromEnum(enumImpl, rImpls, cMax, pcImpls);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumTypeRefs(
    HCORENUM* phEnum,
    mdTypeRef rTypeRefs[],
    ULONG cMax,
    ULONG* pcTypeRefs)
{
    return EnumTokens(_md_ptr.get(), mdtid_TypeRef, phEnum, rTypeRefs, cMax, pcTypeRefs);
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
    return E_NOTIMPL;
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
    return E_NOTIMPL;
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
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumMembers(
    HCORENUM* phEnum,
    mdTypeDef   cl,
    mdToken     rMembers[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    if (TypeFromToken(cl) != mdtTypeDef)
        return E_INVALIDARG;

    HRESULT hr;
    HCORENUMImpl* enumImpl = ToEnumImpl(*phEnum);
    if (enumImpl == nullptr)
    {
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

        RETURN_IF_FAILED(CreateHCORENUMImpl(2, &enumImpl));
        InitHCORENUMImpl(&enumImpl[0], methodList, methodListCount);
        InitHCORENUMImpl(&enumImpl[1], fieldList, fieldListCount);
        *phEnum = enumImpl;
    }

    return ReadFromEnum(enumImpl, rMembers, cMax, pcTokens);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumMembersWithName(
    HCORENUM* phEnum,
    mdTypeDef   cl,
    LPCWSTR     szName,
    mdToken     rMembers[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumMethods(
    HCORENUM* phEnum,
    mdTypeDef   cl,
    mdMethodDef rMethods[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    if (TypeFromToken(cl) != mdtTypeDef)
        return E_INVALIDARG;

    return EnumTokenRange(_md_ptr.get(), cl, mdtTypeDef_MethodList, phEnum, rMethods, cMax, pcTokens);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumMethodsWithName(
    HCORENUM* phEnum,
    mdTypeDef   cl,
    LPCWSTR     szName,
    mdMethodDef rMethods[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumFields(
    HCORENUM* phEnum,
    mdTypeDef   cl,
    mdFieldDef  rFields[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    if (TypeFromToken(cl) != mdtTypeDef)
        return E_INVALIDARG;

    return EnumTokenRange(_md_ptr.get(), cl, mdtTypeDef_FieldList, phEnum, rFields, cMax, pcTokens);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumFieldsWithName(
    HCORENUM* phEnum,
    mdTypeDef   cl,
    LPCWSTR     szName,
    mdFieldDef  rFields[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    return E_NOTIMPL;
}


HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumParams(
    HCORENUM* phEnum,
    mdMethodDef mb,
    mdParamDef  rParams[],
    ULONG       cMax,
    ULONG* pcTokens)
{
    if (TypeFromToken(mb) != mdtMethodDef)
        return E_INVALIDARG;

    return EnumTokenRange(_md_ptr.get(), mb, mdtMethodDef_ParamList, phEnum, rParams, cMax, pcTokens);
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
    return E_NOTIMPL;
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
    return E_NOTIMPL;
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
    if (TypeFromToken(td) != mdtTypeDef)
        return E_INVALIDARG;

    HRESULT hr;
    HCORENUMImpl* enumImpl = ToEnumImpl(*phEnum);
    if (enumImpl == nullptr)
    {
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

        RETURN_IF_FAILED(CreateHCORENUMImpl(1, &enumImpl));
        InitHCORENUMImpl(enumImpl, propertyList, propertyListCount);
        *phEnum = enumImpl;
    }

    return ReadFromEnum(enumImpl, rProperties, cMax, pcProperties);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumEvents(
    HCORENUM* phEnum,
    mdTypeDef   td,
    mdEvent     rEvents[],
    ULONG       cMax,
    ULONG* pcEvents)
{
    if (TypeFromToken(td) != mdtTypeDef)
        return E_INVALIDARG;

    HRESULT hr;
    HCORENUMImpl* enumImpl = ToEnumImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        // Create cursor for EventMap table
        mdcursor_t eventMap;
        uint32_t eventMapCount;
        if (!md_create_cursor(_md_ptr.get(), mdtid_EventMap, &eventMap, &eventMapCount))
            return E_INVALIDARG;

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

        RETURN_IF_FAILED(CreateHCORENUMImpl(1, &enumImpl));
        InitHCORENUMImpl(enumImpl, eventList, eventListCount);
        *phEnum = enumImpl;
    }

    return ReadFromEnum(enumImpl, rEvents, cMax, pcEvents);
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
    return E_NOTIMPL;
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
            return CLDB_E_RECORD_NOTFOUND;

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
    return E_NOTIMPL;
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
    return EnumTokens(_md_ptr.get(), mdtid_ModuleRef, phEnum, rModuleRefs, cMax, pcModuleRefs);
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

    // Set the return as soon as we know.
    uint32_t retCharLen = (string.str_bytes - 1) / sizeof(WCHAR);
    *pchString = retCharLen;

    if (cchString > 0 && retCharLen > 0)
    {
        uint32_t toCopyWChar = cchString < retCharLen ? cchString : retCharLen;
        assert(szString != nullptr);
        memcpy(szString, string.str, toCopyWChar * sizeof(WCHAR));
        if (cchString < retCharLen)
        {
            ::memset(&szString[cchString - 1], 0, sizeof(WCHAR)); // Ensure null terminator
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
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumSignatures(
    HCORENUM* phEnum,
    mdSignature rSignatures[],
    ULONG       cMax,
    ULONG* pcSignatures)
{
    return EnumTokens(_md_ptr.get(), mdtid_StandAloneSig, phEnum, rSignatures, cMax, pcSignatures);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumTypeSpecs(
    HCORENUM* phEnum,
    mdTypeSpec  rTypeSpecs[],
    ULONG       cMax,
    ULONG* pcTypeSpecs)
{
    return EnumTokens(_md_ptr.get(), mdtid_TypeSpec, phEnum, rTypeSpecs, cMax, pcTypeSpecs);
}

namespace
{
    uint32_t EnumNonEmptyUserStrings(
        _In_ mdhandle_t handle,
        _Inout_ mduserstringcursor_t& cursor,
        _Out_writes_opt_(cMax) mdString rStrings[],
        _In_ ULONG cMax)
    {
        assert(cMax != 0);
        mduserstring_t us[8];
        uint32_t offsets[ARRAYSIZE(us)];

        // Initialize buffer if provided.
        if (rStrings != nullptr)
            ::memset(rStrings, 0, cMax * sizeof(rStrings[0]));

        uint32_t bulkRead;
        uint32_t i = 0;
        while (i < cMax)
        {
            bulkRead = ARRAYSIZE(us) < (cMax - i) ? ARRAYSIZE(us) : (cMax - i);
            int32_t count = md_walk_user_string_heap(handle, &cursor, bulkRead, us, offsets);
            if (count == 0)
                break;

            for (int32_t j = 0; j < count; ++j)
            {
                if (us[j].str_bytes == 0)
                    continue;

                if (rStrings != nullptr)
                    rStrings[i] = RidToToken(offsets[j], mdtString);

                i++;
            }
        }

        return i;
    }
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumUserStrings(
    HCORENUM* phEnum,
    mdString    rStrings[],
    ULONG       cMax,
    ULONG* pcStrings)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToEnumImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        mduserstringcursor_t cursor = 0;
        uint32_t count = EnumNonEmptyUserStrings(_md_ptr.get(), cursor, nullptr, UINT32_MAX);

        RETURN_IF_FAILED(CreateHCORENUMImpl(1, &enumImpl));
        cursor = 0;
        InitHCORENUMImpl(enumImpl, cursor, count);
        *phEnum = enumImpl;
    }

    *pcStrings = (cMax > 0)
        ? EnumNonEmptyUserStrings(_md_ptr.get(), enumImpl->us_current, rStrings, cMax)
        : 0;
    return S_OK;
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
    return E_NOTIMPL;
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

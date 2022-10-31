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
    struct HCORENUMImpl
    {
        mdcursor_t current;
        mdcursor_t start;
        uint32_t readIn;
        uint32_t total;
    };

    HCORENUMImpl* ToEnumImpl(HCORENUM hEnum) noexcept
    {
        return reinterpret_cast<HCORENUMImpl*>(hEnum);
    }

    HCORENUM ToEnum(HCORENUMImpl* enumImpl) noexcept
    {
        return reinterpret_cast<HCORENUM>(enumImpl);
    }

    HRESULT CreateHCORENUMImpl(_In_ mdcursor_t cursor, _In_ uint32_t rows, _Out_ HCORENUMImpl** pEnumImpl) noexcept
    {
        HCORENUMImpl* enumImpl;
        enumImpl = (HCORENUMImpl*)::malloc(sizeof(*enumImpl));
        if (enumImpl == nullptr)
            return E_OUTOFMEMORY;

        // Copy over cursor to new allocation
        enumImpl->current = cursor;
        enumImpl->start = cursor;
        enumImpl->readIn = 0;
        enumImpl->total = rows;

        *pEnumImpl = enumImpl;
        return S_OK;
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
    *pulCount = (enumImpl != nullptr)
        ? enumImpl->total
        : 0;

    return S_OK;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::ResetEnum(HCORENUM hEnum, ULONG ulPos)
{
    HCORENUMImpl* enumImpl = ToEnumImpl(hEnum);
    if (enumImpl == nullptr)
        return S_OK;

    mdcursor_t newStart = enumImpl->start;
    if (!md_cursor_move(&newStart, ulPos))
        return E_INVALIDARG;

    // Reset the enum state
    enumImpl->current = newStart;
    enumImpl->readIn = ulPos;
    return S_OK;
}

namespace
{
    HRESULT EnumerateTokens(
        HCORENUMImpl* enumImpl,
        mdToken rTokens[],
        ULONG cMax,
        ULONG* pcTokens)
    {
        assert(enumImpl != nullptr && rTokens != nullptr);

        uint32_t count = 0;
        for (uint32_t i = 0; i < cMax; ++i)
        {
            if (!md_cursor_to_token(enumImpl->current, &rTokens[count]))
                break;
            count++;

            if (!md_cursor_next(&enumImpl->current))
                break;
            enumImpl->readIn++;
        }

        if (pcTokens != nullptr)
            *pcTokens = count;

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
    HCORENUMImpl* enumImpl = ToEnumImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        mdcursor_t cursor;
        uint32_t rows;
        if (!md_create_cursor(_md_ptr.get(), mdtid_TypeDef, &cursor, &rows))
            return E_INVALIDARG;

        RETURN_IF_FAILED(CreateHCORENUMImpl(cursor, rows, &enumImpl));
        *phEnum = enumImpl;
    }

    return EnumerateTokens(enumImpl, rTypeDefs, cMax, pcTypeDefs);
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumInterfaceImpls(
    HCORENUM* phEnum,
    mdTypeDef td,
    mdInterfaceImpl rImpls[],
    ULONG cMax,
    ULONG* pcImpls)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumTypeRefs(
    HCORENUM* phEnum,
    mdTypeRef rTypeRefs[],
    ULONG cMax,
    ULONG* pcTypeRefs)
{
    HRESULT hr;
    HCORENUMImpl* enumImpl = ToEnumImpl(*phEnum);
    if (enumImpl == nullptr)
    {
        mdcursor_t cursor;
        uint32_t rows;
        if (!md_create_cursor(_md_ptr.get(), mdtid_TypeRef, &cursor, &rows))
            return E_INVALIDARG;

        RETURN_IF_FAILED(CreateHCORENUMImpl(cursor, rows, &enumImpl));
        *phEnum = enumImpl;
    }

    return EnumerateTokens(enumImpl, rTypeRefs, cMax, pcTypeRefs);
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
    mdcursor_t cursor;
    if (!md_token_to_cursor(_md_ptr.get(), iiImpl, &cursor))
        return CLDB_E_INDEX_NOTFOUND;

    if (!md_get_column_value_as_token(cursor, mdtInterfaceImpl_Class, 1, pClass)
        || !md_get_column_value_as_token(cursor, mdtInterfaceImpl_Interface, 1, ptkIface))
        return E_FAIL;

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
    return E_NOTIMPL;
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
    return E_NOTIMPL;
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
    return E_NOTIMPL;
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
    return E_NOTIMPL;
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
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumEvents(
    HCORENUM* phEnum,
    mdTypeDef   td,
    mdEvent     rEvents[],
    ULONG       cMax,
    ULONG* pcEvents)
{
    return E_NOTIMPL;
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
    return E_NOTIMPL;
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
    return E_NOTIMPL;
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
    return E_NOTIMPL;
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
    ULONG       cmax,
    ULONG* pcModuleRefs)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::GetTypeSpecFromToken(
    mdTypeSpec typespec,
    PCCOR_SIGNATURE* ppvSig,
    ULONG* pcbSig)
{
    return E_NOTIMPL;
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
    return E_NOTIMPL;
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
    ULONG       cmax,
    ULONG* pcSignatures)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumTypeSpecs(
    HCORENUM* phEnum,
    mdTypeSpec  rTypeSpecs[],
    ULONG       cmax,
    ULONG* pcTypeSpecs)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE MetadataImportRO::EnumUserStrings(
    HCORENUM* phEnum,
    mdString    rStrings[],
    ULONG       cmax,
    ULONG* pcStrings)
{
    return E_NOTIMPL;
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

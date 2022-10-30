#ifndef _SRC_INTERFACES_IMPL_HPP_
#define _SRC_INTERFACES_IMPL_HPP_

#include <cstdint>
#include <atomic>

#include <external/cor.h>
#include <external/corhdr.h>

#include "dnmd.hpp"

class MetadataImportRO : IMetaDataImport2
{
    std::atomic_uint32_t _refCount;
    mdhandle_ptr _md_ptr;

public:
    MetadataImportRO(mdhandle_ptr md_ptr)
        : _refCount{ 1 }
        , _md_ptr{ std::move(md_ptr) }
    { }

    virtual ~MetadataImportRO() = default;

public: // IMetaDataImport
    STDMETHOD_(void, CloseEnum)(HCORENUM hEnum);
    STDMETHOD(CountEnum)(HCORENUM hEnum, ULONG *pulCount);
    STDMETHOD(ResetEnum)(HCORENUM hEnum, ULONG ulPos);
    STDMETHOD(EnumTypeDefs)(HCORENUM *phEnum, mdTypeDef rTypeDefs[],
                            ULONG cMax, ULONG *pcTypeDefs);
    STDMETHOD(EnumInterfaceImpls)(HCORENUM *phEnum, mdTypeDef td,
                            mdInterfaceImpl rImpls[], ULONG cMax,
                            ULONG* pcImpls);
    STDMETHOD(EnumTypeRefs)(HCORENUM *phEnum, mdTypeRef rTypeRefs[],
                            ULONG cMax, ULONG* pcTypeRefs);

    STDMETHOD(FindTypeDefByName)(
        LPCWSTR     szTypeDef,
        mdToken     tkEnclosingClass,
        mdTypeDef   *ptd);

    STDMETHOD(GetScopeProps)(
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,
        ULONG       cchName,
        ULONG       *pchName,
        GUID        *pmvid);

    STDMETHOD(GetModuleFromScope)(
        mdModule    *pmd);

    STDMETHOD(GetTypeDefProps)(
        mdTypeDef   td,
      _Out_writes_to_opt_(cchTypeDef, *pchTypeDef)
        LPWSTR      szTypeDef,
        ULONG       cchTypeDef,
        ULONG       *pchTypeDef,
        DWORD       *pdwTypeDefFlags,
        mdToken     *ptkExtends);

    STDMETHOD(GetInterfaceImplProps)(
        mdInterfaceImpl iiImpl,
        mdTypeDef   *pClass,
        mdToken     *ptkIface);

    STDMETHOD(GetTypeRefProps)(
        mdTypeRef   tr,
        mdToken     *ptkResolutionScope,
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,
        ULONG       cchName,
        ULONG       *pchName);

    STDMETHOD(ResolveTypeRef)(mdTypeRef tr, REFIID riid, IUnknown **ppIScope, mdTypeDef *ptd);

    STDMETHOD(EnumMembers)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        mdToken     rMembers[],
        ULONG       cMax,
        ULONG       *pcTokens);

    STDMETHOD(EnumMembersWithName)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        LPCWSTR     szName,
        mdToken     rMembers[],
        ULONG       cMax,
        ULONG       *pcTokens);

    STDMETHOD(EnumMethods)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        mdMethodDef rMethods[],
        ULONG       cMax,
        ULONG       *pcTokens);

    STDMETHOD(EnumMethodsWithName)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        LPCWSTR     szName,
        mdMethodDef rMethods[],
        ULONG       cMax,
        ULONG       *pcTokens);

    STDMETHOD(EnumFields)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        mdFieldDef  rFields[],
        ULONG       cMax,
        ULONG       *pcTokens);

    STDMETHOD(EnumFieldsWithName)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        LPCWSTR     szName,
        mdFieldDef  rFields[],
        ULONG       cMax,
        ULONG       *pcTokens);


    STDMETHOD(EnumParams)(
        HCORENUM    *phEnum,
        mdMethodDef mb,
        mdParamDef  rParams[],
        ULONG       cMax,
        ULONG       *pcTokens);

    STDMETHOD(EnumMemberRefs)(
        HCORENUM    *phEnum,
        mdToken     tkParent,
        mdMemberRef rMemberRefs[],
        ULONG       cMax,
        ULONG       *pcTokens);

    STDMETHOD(EnumMethodImpls)(
        HCORENUM    *phEnum,
        mdTypeDef   td,
        mdToken     rMethodBody[],
        mdToken     rMethodDecl[],
        ULONG       cMax,
        ULONG       *pcTokens);

    STDMETHOD(EnumPermissionSets)(
        HCORENUM    *phEnum,
        mdToken     tk,
        DWORD       dwActions,
        mdPermission rPermission[],
        ULONG       cMax,
        ULONG       *pcTokens);

    STDMETHOD(FindMember)(
        mdTypeDef   td,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdToken     *pmb);

    STDMETHOD(FindMethod)(
        mdTypeDef   td,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMethodDef *pmb);

    STDMETHOD(FindField)(
        mdTypeDef   td,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdFieldDef  *pmb);

    STDMETHOD(FindMemberRef)(
        mdTypeRef   td,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMemberRef *pmr);

    STDMETHOD (GetMethodProps)(
        mdMethodDef mb,
        mdTypeDef   *pClass,
      _Out_writes_to_opt_(cchMethod, *pchMethod)
        LPWSTR      szMethod,
        ULONG       cchMethod,
        ULONG       *pchMethod,
        DWORD       *pdwAttr,
        PCCOR_SIGNATURE *ppvSigBlob,
        ULONG       *pcbSigBlob,
        ULONG       *pulCodeRVA,
        DWORD       *pdwImplFlags);

    STDMETHOD(GetMemberRefProps)(
        mdMemberRef mr,
        mdToken     *ptk,
      _Out_writes_to_opt_(cchMember, *pchMember)
        LPWSTR      szMember,
        ULONG       cchMember,
        ULONG       *pchMember,
        PCCOR_SIGNATURE *ppvSigBlob,
        ULONG       *pbSig);

    STDMETHOD(EnumProperties)(
        HCORENUM    *phEnum,
        mdTypeDef   td,
        mdProperty  rProperties[],
        ULONG       cMax,
        ULONG       *pcProperties);

    STDMETHOD(EnumEvents)(
        HCORENUM    *phEnum,
        mdTypeDef   td,
        mdEvent     rEvents[],
        ULONG       cMax,
        ULONG       *pcEvents);

    STDMETHOD(GetEventProps)(
        mdEvent     ev,
        mdTypeDef   *pClass,
        LPCWSTR     szEvent,
        ULONG       cchEvent,
        ULONG       *pchEvent,
        DWORD       *pdwEventFlags,
        mdToken     *ptkEventType,
        mdMethodDef *pmdAddOn,
        mdMethodDef *pmdRemoveOn,
        mdMethodDef *pmdFire,
        mdMethodDef rmdOtherMethod[],
        ULONG       cMax,
        ULONG       *pcOtherMethod);

    STDMETHOD(EnumMethodSemantics)(
        HCORENUM    *phEnum,
        mdMethodDef mb,
        mdToken     rEventProp[],
        ULONG       cMax,
        ULONG       *pcEventProp);

    STDMETHOD(GetMethodSemantics)(
        mdMethodDef mb,
        mdToken     tkEventProp,
        DWORD       *pdwSemanticsFlags);

    STDMETHOD(GetClassLayout) (
        mdTypeDef   td,
        DWORD       *pdwPackSize,
        COR_FIELD_OFFSET rFieldOffset[],
        ULONG       cMax,
        ULONG       *pcFieldOffset,
        ULONG       *pulClassSize);

    STDMETHOD(GetFieldMarshal) (
        mdToken     tk,
        PCCOR_SIGNATURE *ppvNativeType,
        ULONG       *pcbNativeType);

    STDMETHOD(GetRVA)(
        mdToken     tk,
        ULONG       *pulCodeRVA,
        DWORD       *pdwImplFlags);

    STDMETHOD(GetPermissionSetProps) (
        mdPermission pm,
        DWORD       *pdwAction,
        void const  **ppvPermission,
        ULONG       *pcbPermission);

    STDMETHOD(GetSigFromToken)(
        mdSignature mdSig,
        PCCOR_SIGNATURE *ppvSig,
        ULONG       *pcbSig);

    STDMETHOD(GetModuleRefProps)(
        mdModuleRef mur,
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,
        ULONG       cchName,
        ULONG       *pchName);

    STDMETHOD(EnumModuleRefs)(
        HCORENUM    *phEnum,
        mdModuleRef rModuleRefs[],
        ULONG       cmax,
        ULONG       *pcModuleRefs);

    STDMETHOD(GetTypeSpecFromToken)(
        mdTypeSpec typespec,
        PCCOR_SIGNATURE *ppvSig,
        ULONG       *pcbSig);

    STDMETHOD(GetNameFromToken)(            // Not Recommended! May be removed!
        mdToken     tk,
        MDUTF8CSTR  *pszUtf8NamePtr);

    STDMETHOD(EnumUnresolvedMethods)(
        HCORENUM    *phEnum,
        mdToken     rMethods[],
        ULONG       cMax,
        ULONG       *pcTokens);

    STDMETHOD(GetUserString)(
        mdString    stk,
      _Out_writes_to_opt_(cchString, *pchString)
        LPWSTR      szString,
        ULONG       cchString,
        ULONG       *pchString);

    STDMETHOD(GetPinvokeMap)(
        mdToken     tk,
        DWORD       *pdwMappingFlags,
      _Out_writes_to_opt_(cchImportName, *pchImportName)
        LPWSTR      szImportName,
        ULONG       cchImportName,
        ULONG       *pchImportName,
        mdModuleRef *pmrImportDLL);

    STDMETHOD(EnumSignatures)(
        HCORENUM    *phEnum,
        mdSignature rSignatures[],
        ULONG       cmax,
        ULONG       *pcSignatures);

    STDMETHOD(EnumTypeSpecs)(
        HCORENUM    *phEnum,
        mdTypeSpec  rTypeSpecs[],
        ULONG       cmax,
        ULONG       *pcTypeSpecs);

    STDMETHOD(EnumUserStrings)(
        HCORENUM    *phEnum,
        mdString    rStrings[],
        ULONG       cmax,
        ULONG       *pcStrings);

    STDMETHOD(GetParamForMethodIndex)(
        mdMethodDef md,
        ULONG       ulParamSeq,
        mdParamDef  *ppd);

    STDMETHOD(EnumCustomAttributes)(
        HCORENUM    *phEnum,
        mdToken     tk,
        mdToken     tkType,
        mdCustomAttribute rCustomAttributes[],
        ULONG       cMax,
        ULONG       *pcCustomAttributes);

    STDMETHOD(GetCustomAttributeProps)(
        mdCustomAttribute cv,
        mdToken     *ptkObj,
        mdToken     *ptkType,
        void const  **ppBlob,
        ULONG       *pcbSize);

    STDMETHOD(FindTypeRef)(
        mdToken     tkResolutionScope,
        LPCWSTR     szName,
        mdTypeRef   *ptr);

    STDMETHOD(GetMemberProps)(
        mdToken     mb,
        mdTypeDef   *pClass,
      _Out_writes_to_opt_(cchMember, *pchMember)
        LPWSTR      szMember,
        ULONG       cchMember,
        ULONG       *pchMember,
        DWORD       *pdwAttr,
        PCCOR_SIGNATURE *ppvSigBlob,
        ULONG       *pcbSigBlob,
        ULONG       *pulCodeRVA,
        DWORD       *pdwImplFlags,
        DWORD       *pdwCPlusTypeFlag,
        UVCP_CONSTANT *ppValue,
        ULONG       *pcchValue);

    STDMETHOD(GetFieldProps)(
        mdFieldDef  mb,
        mdTypeDef   *pClass,
      _Out_writes_to_opt_(cchField, *pchField)
        LPWSTR      szField,
        ULONG       cchField,
        ULONG       *pchField,
        DWORD       *pdwAttr,
        PCCOR_SIGNATURE *ppvSigBlob,
        ULONG       *pcbSigBlob,
        DWORD       *pdwCPlusTypeFlag,
        UVCP_CONSTANT *ppValue,
        ULONG       *pcchValue);

    STDMETHOD(GetPropertyProps)(
        mdProperty  prop,
        mdTypeDef   *pClass,
        LPCWSTR     szProperty,
        ULONG       cchProperty,
        ULONG       *pchProperty,
        DWORD       *pdwPropFlags,
        PCCOR_SIGNATURE *ppvSig,
        ULONG       *pbSig,
        DWORD       *pdwCPlusTypeFlag,
        UVCP_CONSTANT *ppDefaultValue,
        ULONG       *pcchDefaultValue,
        mdMethodDef *pmdSetter,
        mdMethodDef *pmdGetter,
        mdMethodDef rmdOtherMethod[],
        ULONG       cMax,
        ULONG       *pcOtherMethod);

    STDMETHOD(GetParamProps)(
        mdParamDef  tk,
        mdMethodDef *pmd,
        ULONG       *pulSequence,
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,
        ULONG       cchName,
        ULONG       *pchName,
        DWORD       *pdwAttr,
        DWORD       *pdwCPlusTypeFlag,
        UVCP_CONSTANT *ppValue,
        ULONG       *pcchValue);

    STDMETHOD(GetCustomAttributeByName)(
        mdToken     tkObj,
        LPCWSTR     szName,
        const void  **ppData,
        ULONG       *pcbData);

    STDMETHOD_(BOOL, IsValidToken)(
        mdToken     tk);

    STDMETHOD(GetNestedClassProps)(
        mdTypeDef   tdNestedClass,
        mdTypeDef   *ptdEnclosingClass);

    STDMETHOD(GetNativeCallConvFromSig)(
        void const  *pvSig,
        ULONG       cbSig,
        ULONG       *pCallConv);

    STDMETHOD(IsGlobal)(
        mdToken     pd,
        int         *pbGlobal);

public: // IMetaDataImport2
    STDMETHOD(EnumGenericParams)(
        HCORENUM    *phEnum,
        mdToken      tk,
        mdGenericParam rGenericParams[],
        ULONG       cMax,
        ULONG       *pcGenericParams);

    STDMETHOD(GetGenericParamProps)(
        mdGenericParam gp,
        ULONG        *pulParamSeq,
        DWORD        *pdwParamFlags,
        mdToken      *ptOwner,
        DWORD       *reserved,
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR       wzname,
        ULONG        cchName,
        ULONG        *pchName);

    STDMETHOD(GetMethodSpecProps)(
        mdMethodSpec mi,
        mdToken *tkParent,
        PCCOR_SIGNATURE *ppvSigBlob,
        ULONG       *pcbSigBlob);

    STDMETHOD(EnumGenericParamConstraints)(
        HCORENUM    *phEnum,
        mdGenericParam tk,
        mdGenericParamConstraint rGenericParamConstraints[],
        ULONG       cMax,
        ULONG       *pcGenericParamConstraints);

    STDMETHOD(GetGenericParamConstraintProps)(
        mdGenericParamConstraint gpc,
        mdGenericParam *ptGenericParam,
        mdToken      *ptkConstraintType);

    STDMETHOD(GetPEKind)(
        DWORD* pdwPEKind,
        DWORD* pdwMAchine);

    STDMETHOD(GetVersionString)(
      _Out_writes_to_opt_(ccBufSize, *pccBufSize)
        LPWSTR      pwzBuf,
        DWORD       ccBufSize,
        DWORD       *pccBufSize);

    STDMETHOD(EnumMethodSpecs)(
        HCORENUM    *phEnum,
        mdToken      tk,
        mdMethodSpec rMethodSpecs[],
        ULONG       cMax,
        ULONG       *pcMethodSpecs);

public: // IUnknown
    virtual HRESULT STDMETHODCALLTYPE QueryInterface(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject)
    {
        if (ppvObject == nullptr)
            return E_POINTER;

        if (riid == IID_IUnknown)
        {
            *ppvObject = static_cast<IUnknown*>(this);
        }
        else if (riid == IID_IMetaDataImport ||riid == IID_IMetaDataImport2)
        {
            *ppvObject = static_cast<IMetaDataImport2*>(this);
        }
        else
        {
            *ppvObject = nullptr;
            return E_NOINTERFACE;
        }

        (void)AddRef();
        return S_OK;
    }

    virtual ULONG STDMETHODCALLTYPE AddRef(void)
    {
        return ++_refCount;
    }

    virtual ULONG STDMETHODCALLTYPE Release(void)
    {
        uint32_t c = --_refCount;
        if (c == 0)
            delete this;
        return c;
    }
};

#endif // _SRC_INTERFACES_IMPL_HPP_
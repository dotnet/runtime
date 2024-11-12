#ifndef _SRC_INTERFACES_METADATAIMPORTRO_HPP_
#define _SRC_INTERFACES_METADATAIMPORTRO_HPP_

#include <internal/dnmd_platform.hpp>
#include "tearoffbase.hpp"
#include "controllingiunknown.hpp"
#include "dnmdowner.hpp"

#include <cor.h>
#include <corhdr.h>

#include <cstdint>

class MetadataImportRO final : public TearOffBase<IMetaDataImport2, IMetaDataAssemblyImport>
{
    mdhandle_view _md_ptr;

protected:
    virtual bool TryGetInterfaceOnThis(REFIID riid, void** ppvObject) override
    {
        assert(riid != IID_IUnknown);
        if (riid == IID_IMetaDataImport || riid == IID_IMetaDataImport2)
        {
            *ppvObject = static_cast<IMetaDataImport2*>(this);
            return true;
        }
        if (riid == IID_IMetaDataAssemblyImport)
        {
            *ppvObject = static_cast<IMetaDataAssemblyImport*>(this);
            return true;
        }
        return false;
    }

public:
    MetadataImportRO(IUnknown* controllingUnknown, mdhandle_view md_ptr)
        : TearOffBase(controllingUnknown)
        , _md_ptr{ md_ptr }
    { }

    virtual ~MetadataImportRO() = default;

    mdhandle_t MetaData();

public: // IMetaDataImport
    STDMETHOD_(void, CloseEnum)(HCORENUM hEnum) override;
    STDMETHOD(CountEnum)(HCORENUM hEnum, ULONG *pulCount) override;
    STDMETHOD(ResetEnum)(HCORENUM hEnum, ULONG ulPos) override;
    STDMETHOD(EnumTypeDefs)(HCORENUM *phEnum, mdTypeDef rTypeDefs[],
                            ULONG cMax, ULONG *pcTypeDefs) override;
    STDMETHOD(EnumInterfaceImpls)(HCORENUM *phEnum, mdTypeDef td,
                            mdInterfaceImpl rImpls[], ULONG cMax,
                            ULONG* pcImpls) override;
    STDMETHOD(EnumTypeRefs)(HCORENUM *phEnum, mdTypeRef rTypeRefs[],
                            ULONG cMax, ULONG* pcTypeRefs) override;

    STDMETHOD(FindTypeDefByName)(
        LPCWSTR     szTypeDef,
        mdToken     tkEnclosingClass,
        mdTypeDef   *ptd) override;

    STDMETHOD(GetScopeProps)(
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,
        ULONG       cchName,
        ULONG       *pchName,
        GUID        *pmvid) override;

    STDMETHOD(GetModuleFromScope)(
        mdModule    *pmd) override;

    STDMETHOD(GetTypeDefProps)(
        mdTypeDef   td,
      _Out_writes_to_opt_(cchTypeDef, *pchTypeDef)
        LPWSTR      szTypeDef,
        ULONG       cchTypeDef,
        ULONG       *pchTypeDef,
        DWORD       *pdwTypeDefFlags,
        mdToken     *ptkExtends) override;

    STDMETHOD(GetInterfaceImplProps)(
        mdInterfaceImpl iiImpl,
        mdTypeDef   *pClass,
        mdToken     *ptkIface) override;

    STDMETHOD(GetTypeRefProps)(
        mdTypeRef   tr,
        mdToken     *ptkResolutionScope,
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,
        ULONG       cchName,
        ULONG       *pchName) override;

    STDMETHOD(ResolveTypeRef)(mdTypeRef tr, REFIID riid, IUnknown **ppIScope, mdTypeDef *ptd) override;

    STDMETHOD(EnumMembers)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        mdToken     rMembers[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(EnumMembersWithName)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        LPCWSTR     szName,
        mdToken     rMembers[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(EnumMethods)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        mdMethodDef rMethods[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(EnumMethodsWithName)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        LPCWSTR     szName,
        mdMethodDef rMethods[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(EnumFields)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        mdFieldDef  rFields[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(EnumFieldsWithName)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        LPCWSTR     szName,
        mdFieldDef  rFields[],
        ULONG       cMax,
        ULONG       *pcTokens) override;


    STDMETHOD(EnumParams)(
        HCORENUM    *phEnum,
        mdMethodDef mb,
        mdParamDef  rParams[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(EnumMemberRefs)(
        HCORENUM    *phEnum,
        mdToken     tkParent,
        mdMemberRef rMemberRefs[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(EnumMethodImpls)(
        HCORENUM    *phEnum,
        mdTypeDef   td,
        mdToken     rMethodBody[],
        mdToken     rMethodDecl[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(EnumPermissionSets)(
        HCORENUM    *phEnum,
        mdToken     tk,
        DWORD       dwActions,
        mdPermission rPermission[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(FindMember)(
        mdTypeDef   td,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdToken     *pmb) override;

    STDMETHOD(FindMethod)(
        mdTypeDef   td,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMethodDef *pmb) override;

    STDMETHOD(FindField)(
        mdTypeDef   td,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdFieldDef  *pmb) override;

    STDMETHOD(FindMemberRef)(
        mdTypeRef   td,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMemberRef *pmr) override;

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
        DWORD       *pdwImplFlags) override;

    STDMETHOD(GetMemberRefProps)(
        mdMemberRef mr,
        mdToken     *ptk,
      _Out_writes_to_opt_(cchMember, *pchMember)
        LPWSTR      szMember,
        ULONG       cchMember,
        ULONG       *pchMember,
        PCCOR_SIGNATURE *ppvSigBlob,
        ULONG       *pbSig) override;

    STDMETHOD(EnumProperties)(
        HCORENUM    *phEnum,
        mdTypeDef   td,
        mdProperty  rProperties[],
        ULONG       cMax,
        ULONG       *pcProperties) override;

    STDMETHOD(EnumEvents)(
        HCORENUM    *phEnum,
        mdTypeDef   td,
        mdEvent     rEvents[],
        ULONG       cMax,
        ULONG       *pcEvents) override;

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
        ULONG       *pcOtherMethod) override;

    STDMETHOD(EnumMethodSemantics)(
        HCORENUM    *phEnum,
        mdMethodDef mb,
        mdToken     rEventProp[],
        ULONG       cMax,
        ULONG       *pcEventProp) override;

    STDMETHOD(GetMethodSemantics)(
        mdMethodDef mb,
        mdToken     tkEventProp,
        DWORD       *pdwSemanticsFlags) override;

    STDMETHOD(GetClassLayout) (
        mdTypeDef   td,
        DWORD       *pdwPackSize,
        COR_FIELD_OFFSET rFieldOffset[],
        ULONG       cMax,
        ULONG       *pcFieldOffset,
        ULONG       *pulClassSize) override;

    STDMETHOD(GetFieldMarshal) (
        mdToken     tk,
        PCCOR_SIGNATURE *ppvNativeType,
        ULONG       *pcbNativeType) override;

    STDMETHOD(GetRVA)(
        mdToken     tk,
        ULONG       *pulCodeRVA,
        DWORD       *pdwImplFlags) override;

    STDMETHOD(GetPermissionSetProps) (
        mdPermission pm,
        DWORD       *pdwAction,
        void const  **ppvPermission,
        ULONG       *pcbPermission) override;

    STDMETHOD(GetSigFromToken)(
        mdSignature mdSig,
        PCCOR_SIGNATURE *ppvSig,
        ULONG       *pcbSig) override;

    STDMETHOD(GetModuleRefProps)(
        mdModuleRef mur,
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,
        ULONG       cchName,
        ULONG       *pchName) override;

    STDMETHOD(EnumModuleRefs)(
        HCORENUM    *phEnum,
        mdModuleRef rModuleRefs[],
        ULONG       cMax,
        ULONG       *pcModuleRefs) override;

    STDMETHOD(GetTypeSpecFromToken)(
        mdTypeSpec typespec,
        PCCOR_SIGNATURE *ppvSig,
        ULONG       *pcbSig) override;

    STDMETHOD(GetNameFromToken)(            // Not Recommended! May be removed!
        mdToken     tk,
        MDUTF8CSTR  *pszUtf8NamePtr) override;

    STDMETHOD(EnumUnresolvedMethods)(
        HCORENUM    *phEnum,
        mdToken     rMethods[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(GetUserString)(
        mdString    stk,
      _Out_writes_to_opt_(cchString, *pchString)
        LPWSTR      szString,
        ULONG       cchString,
        ULONG       *pchString) override;

    STDMETHOD(GetPinvokeMap)(
        mdToken     tk,
        DWORD       *pdwMappingFlags,
      _Out_writes_to_opt_(cchImportName, *pchImportName)
        LPWSTR      szImportName,
        ULONG       cchImportName,
        ULONG       *pchImportName,
        mdModuleRef *pmrImportDLL) override;

    STDMETHOD(EnumSignatures)(
        HCORENUM    *phEnum,
        mdSignature rSignatures[],
        ULONG       cMax,
        ULONG       *pcSignatures) override;

    STDMETHOD(EnumTypeSpecs)(
        HCORENUM    *phEnum,
        mdTypeSpec  rTypeSpecs[],
        ULONG       cMax,
        ULONG       *pcTypeSpecs) override;

    STDMETHOD(EnumUserStrings)(
        HCORENUM    *phEnum,
        mdString    rStrings[],
        ULONG       cMax,
        ULONG       *pcStrings) override;

    STDMETHOD(GetParamForMethodIndex)(
        mdMethodDef md,
        ULONG       ulParamSeq,
        mdParamDef  *ppd) override;

    STDMETHOD(EnumCustomAttributes)(
        HCORENUM    *phEnum,
        mdToken     tk,
        mdToken     tkType,
        mdCustomAttribute rCustomAttributes[],
        ULONG       cMax,
        ULONG       *pcCustomAttributes) override;

    STDMETHOD(GetCustomAttributeProps)(
        mdCustomAttribute cv,
        mdToken     *ptkObj,
        mdToken     *ptkType,
        void const  **ppBlob,
        ULONG       *pcbSize) override;

    STDMETHOD(FindTypeRef)(
        mdToken     tkResolutionScope,
        LPCWSTR     szName,
        mdTypeRef   *ptr) override;

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
        ULONG       *pcchValue) override;

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
        ULONG       *pcchValue) override;

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
        ULONG       *pcOtherMethod) override;

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
        ULONG       *pcchValue) override;

    STDMETHOD(GetCustomAttributeByName)(
        mdToken     tkObj,
        LPCWSTR     szName,
        void const**  ppData,
        ULONG       *pcbData) override;

    STDMETHOD_(BOOL, IsValidToken)(
        mdToken     tk) override;

    STDMETHOD(GetNestedClassProps)(
        mdTypeDef   tdNestedClass,
        mdTypeDef   *ptdEnclosingClass) override;

    STDMETHOD(GetNativeCallConvFromSig)(
        void const  *pvSig,
        ULONG       cbSig,
        ULONG       *pCallConv) override;

    STDMETHOD(IsGlobal)(
        mdToken     pd,
        int         *pbGlobal) override;

public: // IMetaDataImport2
    STDMETHOD(EnumGenericParams)(
        HCORENUM    *phEnum,
        mdToken      tk,
        mdGenericParam rGenericParams[],
        ULONG       cMax,
        ULONG       *pcGenericParams) override;

    STDMETHOD(GetGenericParamProps)(
        mdGenericParam gp,
        ULONG        *pulParamSeq,
        DWORD        *pdwParamFlags,
        mdToken      *ptOwner,
        DWORD       *reserved,
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR       wzname,
        ULONG        cchName,
        ULONG        *pchName) override;

    STDMETHOD(GetMethodSpecProps)(
        mdMethodSpec mi,
        mdToken *tkParent,
        PCCOR_SIGNATURE *ppvSigBlob,
        ULONG       *pcbSigBlob) override;

    STDMETHOD(EnumGenericParamConstraints)(
        HCORENUM    *phEnum,
        mdGenericParam tk,
        mdGenericParamConstraint rGenericParamConstraints[],
        ULONG       cMax,
        ULONG       *pcGenericParamConstraints) override;

    STDMETHOD(GetGenericParamConstraintProps)(
        mdGenericParamConstraint gpc,
        mdGenericParam *ptGenericParam,
        mdToken      *ptkConstraintType) override;

    STDMETHOD(GetPEKind)(
        DWORD* pdwPEKind,
        DWORD* pdwMAchine) override;

    STDMETHOD(GetVersionString)(
      _Out_writes_to_opt_(ccBufSize, *pccBufSize)
        LPWSTR      pwzBuf,
        DWORD       ccBufSize,
        DWORD       *pccBufSize) override;

    STDMETHOD(EnumMethodSpecs)(
        HCORENUM    *phEnum,
        mdToken      tk,
        mdMethodSpec rMethodSpecs[],
        ULONG       cMax,
        ULONG       *pcMethodSpecs) override;

public: // IMetaDataAssemblyImport
    STDMETHOD(GetAssemblyProps)(
        mdAssembly  mda,
        void const** ppbPublicKey,
        ULONG* pcbPublicKey,
        ULONG* pulHashAlgId,
        _Out_writes_to_opt_(cchName, *pchName) LPWSTR  szName,
        ULONG       cchName,
        ULONG* pchName,
        ASSEMBLYMETADATA* pMetaData,
        DWORD* pdwAssemblyFlags) override;

    STDMETHOD(GetAssemblyRefProps)(
        mdAssemblyRef mdar,
        void const** ppbPublicKeyOrToken,
        ULONG* pcbPublicKeyOrToken,
        _Out_writes_to_opt_(cchName, *pchName)LPWSTR szName,
        ULONG       cchName,
        ULONG* pchName,
        ASSEMBLYMETADATA* pMetaData,
        void const** ppbHashValue,
        ULONG* pcbHashValue,
        DWORD* pdwAssemblyRefFlags) override;

    STDMETHOD(GetFileProps)(
        mdFile      mdf,
        _Out_writes_to_opt_(cchName, *pchName) LPWSTR      szName,
        ULONG       cchName,
        ULONG* pchName,
        void const** ppbHashValue,
        ULONG* pcbHashValue,
        DWORD* pdwFileFlags) override;

    STDMETHOD(GetExportedTypeProps)(
        mdExportedType   mdct,
        _Out_writes_to_opt_(cchName, *pchName) LPWSTR      szName,
        ULONG       cchName,
        ULONG* pchName,
        mdToken* ptkImplementation,
        mdTypeDef* ptkTypeDef,
        DWORD* pdwExportedTypeFlags) override;

    STDMETHOD(GetManifestResourceProps)(
        mdManifestResource  mdmr,
        _Out_writes_to_opt_(cchName, *pchName)LPWSTR      szName,
        ULONG       cchName,
        ULONG* pchName,
        mdToken* ptkImplementation,
        DWORD* pdwOffset,
        DWORD* pdwResourceFlags) override;

    STDMETHOD(EnumAssemblyRefs)(
        HCORENUM* phEnum,
        mdAssemblyRef rAssemblyRefs[],
        ULONG       cMax,
        ULONG* pcTokens) override;

    STDMETHOD(EnumFiles)(
        HCORENUM* phEnum,
        mdFile      rFiles[],
        ULONG       cMax,
        ULONG* pcTokens) override;

    STDMETHOD(EnumExportedTypes)(
        HCORENUM* phEnum,
        mdExportedType   rExportedTypes[],
        ULONG       cMax,
        ULONG* pcTokens) override;

    STDMETHOD(EnumManifestResources)(
        HCORENUM* phEnum,
        mdManifestResource  rManifestResources[],
        ULONG       cMax,
        ULONG* pcTokens) override;

    STDMETHOD(GetAssemblyFromScope)(
        mdAssembly* ptkAssembly) override;

    STDMETHOD(FindExportedTypeByName)(
        LPCWSTR     szName,
        mdToken     mdtExportedType,
        mdExportedType* ptkExportedType) override;

    STDMETHOD(FindManifestResourceByName)(
        LPCWSTR     szName,
        mdManifestResource* ptkManifestResource) override;

    STDMETHOD(FindAssembliesByName)(
        LPCWSTR  szAppBase,
        LPCWSTR  szPrivateBin,
        LPCWSTR  szAssemblyName,
        IUnknown* ppIUnk[],
        ULONG    cMax,
        ULONG* pcAssemblies) override;
};

#endif // _SRC_INTERFACES_METADATAIMPORTRO_HPP_
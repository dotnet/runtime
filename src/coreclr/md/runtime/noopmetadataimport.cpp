// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// NoopMetadataImport implementation.

#include "stdafx.h"

#ifdef FEATURE_METADATA_IN_VM

// This importer is only used to satisfy the symbol binder's non-null parameter.
// It is intentionally inert so we do not materialize the module's real public importer.
#ifdef _DEBUG
#define NOOPMD_NYI(name)                                                                    \
    do {                                                                                    \
        _ASSERTE_MSG(false,                                                                 \
            "NoopMetadataImport: unexpected call to " #name ". The binder should not "    \
            "be asking this importer for metadata.");                                      \
        return E_NOTIMPL;                                                                   \
    } while (0)
#else
#define NOOPMD_NYI(name)  return E_NOTIMPL
#endif

namespace
{
class NoopMetadataImport final : public IMetaDataImport2
{
public:
    // IUnknown
    STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject) override
    {
        LIMITED_METHOD_CONTRACT;

        if (ppvObject == NULL)
            return E_POINTER;

        *ppvObject = NULL;

        // IMetaDataImport2 derives from IMetaDataImport derives from IUnknown -- a
        // single-inheritance chain -- so one pointer, the IMetaDataImport2*, is the
        // layout-compatible answer for all three IIDs.
        if (riid == IID_IUnknown ||
            riid == IID_IMetaDataImport ||
            riid == IID_IMetaDataImport2)
        {
            *ppvObject = static_cast<IMetaDataImport2*>(this);
            return S_OK;
        }

        return E_NOINTERFACE;
    }
    STDMETHOD_(ULONG, AddRef)() override
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }
    STDMETHOD_(ULONG, Release)() override
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    // IMetaDataImport / IMetaDataImport2 -- every method asserts in debug and
    // returns a failure or empty result in release.
    STDMETHOD_(void, CloseEnum)(HCORENUM /*hEnum*/) override
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE_MSG(false, "NoopMetadataImport::CloseEnum NYI");
    }
    STDMETHOD(CountEnum)(HCORENUM hEnum, ULONG* pulCount) override { NOOPMD_NYI(CountEnum); }
    STDMETHOD(ResetEnum)(HCORENUM hEnum, ULONG ulPos) override { NOOPMD_NYI(ResetEnum); }
    STDMETHOD(EnumTypeDefs)(HCORENUM* phEnum, mdTypeDef rTypeDefs[], ULONG cMax, ULONG* pcTypeDefs) override { NOOPMD_NYI(EnumTypeDefs); }
    STDMETHOD(EnumInterfaceImpls)(HCORENUM* phEnum, mdTypeDef td, mdInterfaceImpl rImpls[], ULONG cMax, ULONG* pcImpls) override { NOOPMD_NYI(EnumInterfaceImpls); }
    STDMETHOD(EnumTypeRefs)(HCORENUM* phEnum, mdTypeRef rTypeRefs[], ULONG cMax, ULONG* pcTypeRefs) override { NOOPMD_NYI(EnumTypeRefs); }
    STDMETHOD(FindTypeDefByName)(LPCWSTR szTypeDef, mdToken tkEnclosingClass, mdTypeDef* ptd) override { NOOPMD_NYI(FindTypeDefByName); }
    STDMETHOD(GetScopeProps)(LPWSTR szName, ULONG cchName, ULONG* pchName, GUID* pmvid) override { NOOPMD_NYI(GetScopeProps); }
    STDMETHOD(GetModuleFromScope)(mdModule* pmd) override { NOOPMD_NYI(GetModuleFromScope); }
    STDMETHOD(GetTypeDefProps)(mdTypeDef td, LPWSTR szTypeDef, ULONG cchTypeDef, ULONG* pchTypeDef, DWORD* pdwTypeDefFlags, mdToken* ptkExtends) override { NOOPMD_NYI(GetTypeDefProps); }
    STDMETHOD(GetInterfaceImplProps)(mdInterfaceImpl iiImpl, mdTypeDef* pClass, mdToken* ptkIface) override { NOOPMD_NYI(GetInterfaceImplProps); }
    STDMETHOD(GetTypeRefProps)(mdTypeRef tr, mdToken* ptkResolutionScope, LPWSTR szName, ULONG cchName, ULONG* pchName) override { NOOPMD_NYI(GetTypeRefProps); }
    STDMETHOD(ResolveTypeRef)(mdTypeRef tr, REFIID riid, IUnknown** ppIScope, mdTypeDef* ptd) override { NOOPMD_NYI(ResolveTypeRef); }
    STDMETHOD(EnumMembers)(HCORENUM* phEnum, mdTypeDef cl, mdToken rMembers[], ULONG cMax, ULONG* pcTokens) override { NOOPMD_NYI(EnumMembers); }
    STDMETHOD(EnumMembersWithName)(HCORENUM* phEnum, mdTypeDef cl, LPCWSTR szName, mdToken rMembers[], ULONG cMax, ULONG* pcTokens) override { NOOPMD_NYI(EnumMembersWithName); }
    STDMETHOD(EnumMethods)(HCORENUM* phEnum, mdTypeDef cl, mdMethodDef rMethods[], ULONG cMax, ULONG* pcTokens) override { NOOPMD_NYI(EnumMethods); }
    STDMETHOD(EnumMethodsWithName)(HCORENUM* phEnum, mdTypeDef cl, LPCWSTR szName, mdMethodDef rMethods[], ULONG cMax, ULONG* pcTokens) override { NOOPMD_NYI(EnumMethodsWithName); }
    STDMETHOD(EnumFields)(HCORENUM* phEnum, mdTypeDef cl, mdFieldDef rFields[], ULONG cMax, ULONG* pcTokens) override { NOOPMD_NYI(EnumFields); }
    STDMETHOD(EnumFieldsWithName)(HCORENUM* phEnum, mdTypeDef cl, LPCWSTR szName, mdFieldDef rFields[], ULONG cMax, ULONG* pcTokens) override { NOOPMD_NYI(EnumFieldsWithName); }
    STDMETHOD(EnumParams)(HCORENUM* phEnum, mdMethodDef mb, mdParamDef rParams[], ULONG cMax, ULONG* pcTokens) override { NOOPMD_NYI(EnumParams); }
    STDMETHOD(EnumMemberRefs)(HCORENUM* phEnum, mdToken tkParent, mdMemberRef rMemberRefs[], ULONG cMax, ULONG* pcTokens) override { NOOPMD_NYI(EnumMemberRefs); }
    STDMETHOD(EnumMethodImpls)(HCORENUM* phEnum, mdTypeDef td, mdToken rMethodBody[], mdToken rMethodDecl[], ULONG cMax, ULONG* pcTokens) override { NOOPMD_NYI(EnumMethodImpls); }
    STDMETHOD(EnumPermissionSets)(HCORENUM* phEnum, mdToken tk, DWORD dwActions, mdPermission rPermission[], ULONG cMax, ULONG* pcTokens) override { NOOPMD_NYI(EnumPermissionSets); }
    STDMETHOD(FindMember)(mdTypeDef td, LPCWSTR szName, PCCOR_SIGNATURE pvSigBlob, ULONG cbSigBlob, mdToken* pmb) override { NOOPMD_NYI(FindMember); }
    STDMETHOD(FindMethod)(mdTypeDef td, LPCWSTR szName, PCCOR_SIGNATURE pvSigBlob, ULONG cbSigBlob, mdMethodDef* pmb) override { NOOPMD_NYI(FindMethod); }
    STDMETHOD(FindField)(mdTypeDef td, LPCWSTR szName, PCCOR_SIGNATURE pvSigBlob, ULONG cbSigBlob, mdFieldDef* pmb) override { NOOPMD_NYI(FindField); }
    STDMETHOD(FindMemberRef)(mdTypeRef td, LPCWSTR szName, PCCOR_SIGNATURE pvSigBlob, ULONG cbSigBlob, mdMemberRef* pmr) override { NOOPMD_NYI(FindMemberRef); }
    STDMETHOD(GetMethodProps)(mdMethodDef mb, mdTypeDef* pClass, LPWSTR szMethod, ULONG cchMethod, ULONG* pchMethod, DWORD* pdwAttr, PCCOR_SIGNATURE* ppvSigBlob, ULONG* pcbSigBlob, ULONG* pulCodeRVA, DWORD* pdwImplFlags) override { NOOPMD_NYI(GetMethodProps); }
    STDMETHOD(GetMemberRefProps)(mdMemberRef mr, mdToken* ptk, LPWSTR szMember, ULONG cchMember, ULONG* pchMember, PCCOR_SIGNATURE* ppvSigBlob, ULONG* pbSig) override { NOOPMD_NYI(GetMemberRefProps); }
    STDMETHOD(EnumProperties)(HCORENUM* phEnum, mdTypeDef td, mdProperty rProperties[], ULONG cMax, ULONG* pcProperties) override { NOOPMD_NYI(EnumProperties); }
    STDMETHOD(EnumEvents)(HCORENUM* phEnum, mdTypeDef td, mdEvent rEvents[], ULONG cMax, ULONG* pcEvents) override { NOOPMD_NYI(EnumEvents); }
    STDMETHOD(GetEventProps)(mdEvent ev, mdTypeDef* pClass, LPCWSTR szEvent, ULONG cchEvent, ULONG* pchEvent, DWORD* pdwEventFlags, mdToken* ptkEventType, mdMethodDef* pmdAddOn, mdMethodDef* pmdRemoveOn, mdMethodDef* pmdFire, mdMethodDef rmdOtherMethod[], ULONG cMax, ULONG* pcOtherMethod) override { NOOPMD_NYI(GetEventProps); }
    STDMETHOD(EnumMethodSemantics)(HCORENUM* phEnum, mdMethodDef mb, mdToken rEventProp[], ULONG cMax, ULONG* pcEventProp) override { NOOPMD_NYI(EnumMethodSemantics); }
    STDMETHOD(GetMethodSemantics)(mdMethodDef mb, mdToken tkEventProp, DWORD* pdwSemanticsFlags) override { NOOPMD_NYI(GetMethodSemantics); }
    STDMETHOD(GetClassLayout)(mdTypeDef td, DWORD* pdwPackSize, COR_FIELD_OFFSET rFieldOffset[], ULONG cMax, ULONG* pcFieldOffset, ULONG* pulClassSize) override { NOOPMD_NYI(GetClassLayout); }
    STDMETHOD(GetFieldMarshal)(mdToken tk, PCCOR_SIGNATURE* ppvNativeType, ULONG* pcbNativeType) override { NOOPMD_NYI(GetFieldMarshal); }
    STDMETHOD(GetRVA)(mdToken tk, ULONG* pulCodeRVA, DWORD* pdwImplFlags) override { NOOPMD_NYI(GetRVA); }
    STDMETHOD(GetPermissionSetProps)(mdPermission pm, DWORD* pdwAction, void const** ppvPermission, ULONG* pcbPermission) override { NOOPMD_NYI(GetPermissionSetProps); }
    STDMETHOD(GetModuleRefProps)(mdModuleRef mur, LPWSTR szName, ULONG cchName, ULONG* pchName) override { NOOPMD_NYI(GetModuleRefProps); }
    STDMETHOD(EnumModuleRefs)(HCORENUM* phEnum, mdModuleRef rModuleRefs[], ULONG cmax, ULONG* pcModuleRefs) override { NOOPMD_NYI(EnumModuleRefs); }
    STDMETHOD(GetTypeSpecFromToken)(mdTypeSpec typespec, PCCOR_SIGNATURE* ppvSig, ULONG* pcbSig) override { NOOPMD_NYI(GetTypeSpecFromToken); }
    STDMETHOD(GetNameFromToken)(mdToken tk, MDUTF8CSTR* pszUtf8NamePtr) override { NOOPMD_NYI(GetNameFromToken); }
    STDMETHOD(EnumUnresolvedMethods)(HCORENUM* phEnum, mdToken rMethods[], ULONG cMax, ULONG* pcTokens) override { NOOPMD_NYI(EnumUnresolvedMethods); }
    STDMETHOD(GetUserString)(mdString stk, LPWSTR szString, ULONG cchString, ULONG* pchString) override { NOOPMD_NYI(GetUserString); }
    STDMETHOD(GetPinvokeMap)(mdToken tk, DWORD* pdwMappingFlags, LPWSTR szImportName, ULONG cchImportName, ULONG* pchImportName, mdModuleRef* pmrImportDLL) override { NOOPMD_NYI(GetPinvokeMap); }
    STDMETHOD(EnumSignatures)(HCORENUM* phEnum, mdSignature rSignatures[], ULONG cmax, ULONG* pcSignatures) override { NOOPMD_NYI(EnumSignatures); }
    STDMETHOD(EnumTypeSpecs)(HCORENUM* phEnum, mdTypeSpec rTypeSpecs[], ULONG cmax, ULONG* pcTypeSpecs) override { NOOPMD_NYI(EnumTypeSpecs); }
    STDMETHOD(EnumUserStrings)(HCORENUM* phEnum, mdString rStrings[], ULONG cmax, ULONG* pcStrings) override { NOOPMD_NYI(EnumUserStrings); }
    STDMETHOD(GetParamForMethodIndex)(mdMethodDef md, ULONG ulParamSeq, mdParamDef* ppd) override { NOOPMD_NYI(GetParamForMethodIndex); }
    STDMETHOD(EnumCustomAttributes)(HCORENUM* phEnum, mdToken tk, mdToken tkType, mdCustomAttribute rCustomAttributes[], ULONG cMax, ULONG* pcCustomAttributes) override { NOOPMD_NYI(EnumCustomAttributes); }
    STDMETHOD(GetCustomAttributeProps)(mdCustomAttribute cv, mdToken* ptkObj, mdToken* ptkType, void const** ppBlob, ULONG* pcbSize) override { NOOPMD_NYI(GetCustomAttributeProps); }
    STDMETHOD(FindTypeRef)(mdToken tkResolutionScope, LPCWSTR szName, mdTypeRef* ptr) override { NOOPMD_NYI(FindTypeRef); }
    STDMETHOD(GetMemberProps)(mdToken mb, mdTypeDef* pClass, LPWSTR szMember, ULONG cchMember, ULONG* pchMember, DWORD* pdwAttr, PCCOR_SIGNATURE* ppvSigBlob, ULONG* pcbSigBlob, ULONG* pulCodeRVA, DWORD* pdwImplFlags, DWORD* pdwCPlusTypeFlag, UVCP_CONSTANT* ppValue, ULONG* pcchValue) override { NOOPMD_NYI(GetMemberProps); }
    STDMETHOD(GetFieldProps)(mdFieldDef mb, mdTypeDef* pClass, LPWSTR szField, ULONG cchField, ULONG* pchField, DWORD* pdwAttr, PCCOR_SIGNATURE* ppvSigBlob, ULONG* pcbSigBlob, DWORD* pdwCPlusTypeFlag, UVCP_CONSTANT* ppValue, ULONG* pcchValue) override { NOOPMD_NYI(GetFieldProps); }
    STDMETHOD(GetPropertyProps)(mdProperty prop, mdTypeDef* pClass, LPCWSTR szProperty, ULONG cchProperty, ULONG* pchProperty, DWORD* pdwPropFlags, PCCOR_SIGNATURE* ppvSig, ULONG* pbSig, DWORD* pdwCPlusTypeFlag, UVCP_CONSTANT* ppDefaultValue, ULONG* pcchDefaultValue, mdMethodDef* pmdSetter, mdMethodDef* pmdGetter, mdMethodDef rmdOtherMethod[], ULONG cMax, ULONG* pcOtherMethod) override { NOOPMD_NYI(GetPropertyProps); }
    STDMETHOD(GetParamProps)(mdParamDef tk, mdMethodDef* pmd, ULONG* pulSequence, LPWSTR szName, ULONG cchName, ULONG* pchName, DWORD* pdwAttr, DWORD* pdwCPlusTypeFlag, UVCP_CONSTANT* ppValue, ULONG* pcchValue) override { NOOPMD_NYI(GetParamProps); }
    STDMETHOD(GetCustomAttributeByName)(mdToken tkObj, LPCWSTR szName, const void** ppData, ULONG* pcbData) override { NOOPMD_NYI(GetCustomAttributeByName); }
    STDMETHOD_(BOOL, IsValidToken)(mdToken /*tk*/) override
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE_MSG(false, "NoopMetadataImport::IsValidToken NYI");
        return FALSE;
    }
    STDMETHOD(GetNestedClassProps)(mdTypeDef tdNestedClass, mdTypeDef* ptdEnclosingClass) override { NOOPMD_NYI(GetNestedClassProps); }
    STDMETHOD(GetNativeCallConvFromSig)(void const* pvSig, ULONG cbSig, ULONG* pCallConv) override { NOOPMD_NYI(GetNativeCallConvFromSig); }
    STDMETHOD(IsGlobal)(mdToken pd, int* pbGlobal) override { NOOPMD_NYI(IsGlobal); }
    STDMETHOD(GetSigFromToken)(mdSignature mdSig, PCCOR_SIGNATURE* ppvSig, ULONG* pcbSig) override { NOOPMD_NYI(GetSigFromToken); }

    // IMetaDataImport2
    STDMETHOD(EnumGenericParams)(HCORENUM* phEnum, mdToken tk, mdGenericParam rGenericParams[], ULONG cMax, ULONG* pcGenericParams) override { NOOPMD_NYI(EnumGenericParams); }
    STDMETHOD(GetGenericParamProps)(mdGenericParam gp, ULONG* pulParamSeq, DWORD* pdwParamFlags, mdToken* ptOwner, DWORD* reserved, LPWSTR wzname, ULONG cchName, ULONG* pchName) override { NOOPMD_NYI(GetGenericParamProps); }
    STDMETHOD(GetMethodSpecProps)(mdMethodSpec mi, mdToken* tkParent, PCCOR_SIGNATURE* ppvSigBlob, ULONG* pcbSigBlob) override { NOOPMD_NYI(GetMethodSpecProps); }
    STDMETHOD(EnumGenericParamConstraints)(HCORENUM* phEnum, mdGenericParam tk, mdGenericParamConstraint rGenericParamConstraints[], ULONG cMax, ULONG* pcGenericParamConstraints) override { NOOPMD_NYI(EnumGenericParamConstraints); }
    STDMETHOD(GetGenericParamConstraintProps)(mdGenericParamConstraint gpc, mdGenericParam* ptGenericParam, mdToken* ptkConstraintType) override { NOOPMD_NYI(GetGenericParamConstraintProps); }
    STDMETHOD(GetPEKind)(DWORD* pdwPEKind, DWORD* pdwMachine) override { NOOPMD_NYI(GetPEKind); }
    STDMETHOD(GetVersionString)(LPWSTR pwzBuf, DWORD ccBufSize, DWORD* pccBufSize) override { NOOPMD_NYI(GetVersionString); }
    STDMETHOD(EnumMethodSpecs)(HCORENUM* phEnum, mdToken tk, mdMethodSpec rMethodSpecs[], ULONG cMax, ULONG* pcMethodSpecs) override { NOOPMD_NYI(EnumMethodSpecs); }
};

NoopMetadataImport g_NoopMetadataImport;
} // namespace

IMetaDataImport2* GetNoopMetaDataImport2()
{
    LIMITED_METHOD_CONTRACT;

    return &g_NoopMetadataImport;
}

#endif // FEATURE_METADATA_IN_VM

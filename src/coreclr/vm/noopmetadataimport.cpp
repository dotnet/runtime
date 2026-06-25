// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// NoopMetadataImport implementation. See noopmetadataimport.h for design.

#include "common.h"

#ifndef DACCESS_COMPILE

#include "noopmetadataimport.h"

// Every method asserts in debug so an unexpected call (a future reader consumer
// that walks constant/local signatures, or a diasymreader change) is caught
// immediately; release builds return E_NOTIMPL.
#ifdef _DEBUG
#define NOOPMD_NYI(name)                                                                \
    do {                                                                                \
        _ASSERTE_MSG(false,                                                             \
            "NoopMetadataImport: unexpected call to " #name ". This importer is "       \
            "handed to Microsoft.DiaSymReader.Native only to satisfy the binder; the "  \
            "reader is expected to resolve sequence points without calling back into "  \
            "it. A call here means that assumption no longer holds.");                  \
        return E_NOTIMPL;                                                               \
    } while (0)
#else
#define NOOPMD_NYI(name)  return E_NOTIMPL
#endif

// ---------------------------------------------------------------------------
// Singleton accessor
// ---------------------------------------------------------------------------

IUnknown* NoopMetadataImport::GetInstance()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Lazily created once and intentionally leaked for the process lifetime, so
    // there is no static-initialization ordering or teardown to reason about.
    static NoopMetadataImport* volatile s_pInstance = NULL;

    NoopMetadataImport* pInstance = VolatileLoad(&s_pInstance);
    if (pInstance == NULL)
    {
        NoopMetadataImport* pNew = new (nothrow) NoopMetadataImport();
        if (pNew == NULL)
            return NULL;

        if (InterlockedCompareExchangeT(&s_pInstance, pNew, (NoopMetadataImport*)NULL) != NULL)
            delete pNew; // lost the race; another thread published first

        pInstance = VolatileLoad(&s_pInstance);
    }

    return static_cast<IMetaDataImport2*>(pInstance);
}

// ---------------------------------------------------------------------------
// IUnknown
// ---------------------------------------------------------------------------

STDMETHODIMP NoopMetadataImport::QueryInterface(REFIID riid, void** ppvObject)
{
    LIMITED_METHOD_CONTRACT;

    if (ppvObject == NULL)
        return E_POINTER;

    *ppvObject = NULL;

    // IMetaDataImport2 derives from IMetaDataImport derives from IUnknown -- a
    // single-inheritance chain -- so one pointer, the IMetaDataImport2*, is the
    // layout-compatible answer for all three IIDs. IID_IGetIMDInternalImport (the
    // public->internal back-conversion the runtime uses to push a module to RW
    // metadata) is intentionally not exposed.
    if (riid == IID_IUnknown ||
        riid == IID_IMetaDataImport ||
        riid == IID_IMetaDataImport2)
    {
        *ppvObject = static_cast<IMetaDataImport2*>(this);
        AddRef();
        return S_OK;
    }

    return E_NOINTERFACE;
}

// The singleton lives for the process, so refcounting is a no-op: returning a
// constant tells COM callers the object is never destroyed.
STDMETHODIMP_(ULONG) NoopMetadataImport::AddRef()
{
    LIMITED_METHOD_CONTRACT;
    return 1;
}

STDMETHODIMP_(ULONG) NoopMetadataImport::Release()
{
    LIMITED_METHOD_CONTRACT;
    return 1;
}

// ---------------------------------------------------------------------------
// Fail-fast stubs for every metadata method.
// ---------------------------------------------------------------------------

STDMETHODIMP_(void) NoopMetadataImport::CloseEnum(HCORENUM /*hEnum*/) { LIMITED_METHOD_CONTRACT; _ASSERTE_MSG(false, "NoopMetadataImport::CloseEnum NYI"); }
STDMETHODIMP NoopMetadataImport::CountEnum(HCORENUM, ULONG*) { NOOPMD_NYI(CountEnum); }
STDMETHODIMP NoopMetadataImport::ResetEnum(HCORENUM, ULONG) { NOOPMD_NYI(ResetEnum); }
STDMETHODIMP NoopMetadataImport::EnumTypeDefs(HCORENUM*, mdTypeDef[], ULONG, ULONG*) { NOOPMD_NYI(EnumTypeDefs); }
STDMETHODIMP NoopMetadataImport::EnumInterfaceImpls(HCORENUM*, mdTypeDef, mdInterfaceImpl[], ULONG, ULONG*) { NOOPMD_NYI(EnumInterfaceImpls); }
STDMETHODIMP NoopMetadataImport::EnumTypeRefs(HCORENUM*, mdTypeRef[], ULONG, ULONG*) { NOOPMD_NYI(EnumTypeRefs); }
STDMETHODIMP NoopMetadataImport::FindTypeDefByName(LPCWSTR, mdToken, mdTypeDef*) { NOOPMD_NYI(FindTypeDefByName); }
STDMETHODIMP NoopMetadataImport::GetScopeProps(LPWSTR, ULONG, ULONG*, GUID*) { NOOPMD_NYI(GetScopeProps); }
STDMETHODIMP NoopMetadataImport::GetModuleFromScope(mdModule*) { NOOPMD_NYI(GetModuleFromScope); }
STDMETHODIMP NoopMetadataImport::GetTypeDefProps(mdTypeDef, LPWSTR, ULONG, ULONG*, DWORD*, mdToken*) { NOOPMD_NYI(GetTypeDefProps); }
STDMETHODIMP NoopMetadataImport::GetInterfaceImplProps(mdInterfaceImpl, mdTypeDef*, mdToken*) { NOOPMD_NYI(GetInterfaceImplProps); }
STDMETHODIMP NoopMetadataImport::GetTypeRefProps(mdTypeRef, mdToken*, LPWSTR, ULONG, ULONG*) { NOOPMD_NYI(GetTypeRefProps); }
STDMETHODIMP NoopMetadataImport::ResolveTypeRef(mdTypeRef, REFIID, IUnknown**, mdTypeDef*) { NOOPMD_NYI(ResolveTypeRef); }
STDMETHODIMP NoopMetadataImport::EnumMembers(HCORENUM*, mdTypeDef, mdToken[], ULONG, ULONG*) { NOOPMD_NYI(EnumMembers); }
STDMETHODIMP NoopMetadataImport::EnumMembersWithName(HCORENUM*, mdTypeDef, LPCWSTR, mdToken[], ULONG, ULONG*) { NOOPMD_NYI(EnumMembersWithName); }
STDMETHODIMP NoopMetadataImport::EnumMethods(HCORENUM*, mdTypeDef, mdMethodDef[], ULONG, ULONG*) { NOOPMD_NYI(EnumMethods); }
STDMETHODIMP NoopMetadataImport::EnumMethodsWithName(HCORENUM*, mdTypeDef, LPCWSTR, mdMethodDef[], ULONG, ULONG*) { NOOPMD_NYI(EnumMethodsWithName); }
STDMETHODIMP NoopMetadataImport::EnumFields(HCORENUM*, mdTypeDef, mdFieldDef[], ULONG, ULONG*) { NOOPMD_NYI(EnumFields); }
STDMETHODIMP NoopMetadataImport::EnumFieldsWithName(HCORENUM*, mdTypeDef, LPCWSTR, mdFieldDef[], ULONG, ULONG*) { NOOPMD_NYI(EnumFieldsWithName); }
STDMETHODIMP NoopMetadataImport::EnumParams(HCORENUM*, mdMethodDef, mdParamDef[], ULONG, ULONG*) { NOOPMD_NYI(EnumParams); }
STDMETHODIMP NoopMetadataImport::EnumMemberRefs(HCORENUM*, mdToken, mdMemberRef[], ULONG, ULONG*) { NOOPMD_NYI(EnumMemberRefs); }
STDMETHODIMP NoopMetadataImport::EnumMethodImpls(HCORENUM*, mdTypeDef, mdToken[], mdToken[], ULONG, ULONG*) { NOOPMD_NYI(EnumMethodImpls); }
STDMETHODIMP NoopMetadataImport::EnumPermissionSets(HCORENUM*, mdToken, DWORD, mdPermission[], ULONG, ULONG*) { NOOPMD_NYI(EnumPermissionSets); }
STDMETHODIMP NoopMetadataImport::FindMember(mdTypeDef, LPCWSTR, PCCOR_SIGNATURE, ULONG, mdToken*) { NOOPMD_NYI(FindMember); }
STDMETHODIMP NoopMetadataImport::FindMethod(mdTypeDef, LPCWSTR, PCCOR_SIGNATURE, ULONG, mdMethodDef*) { NOOPMD_NYI(FindMethod); }
STDMETHODIMP NoopMetadataImport::FindField(mdTypeDef, LPCWSTR, PCCOR_SIGNATURE, ULONG, mdFieldDef*) { NOOPMD_NYI(FindField); }
STDMETHODIMP NoopMetadataImport::FindMemberRef(mdTypeRef, LPCWSTR, PCCOR_SIGNATURE, ULONG, mdMemberRef*) { NOOPMD_NYI(FindMemberRef); }
STDMETHODIMP NoopMetadataImport::GetMethodProps(mdMethodDef, mdTypeDef*, LPWSTR, ULONG, ULONG*, DWORD*, PCCOR_SIGNATURE*, ULONG*, ULONG*, DWORD*) { NOOPMD_NYI(GetMethodProps); }
STDMETHODIMP NoopMetadataImport::GetMemberRefProps(mdMemberRef, mdToken*, LPWSTR, ULONG, ULONG*, PCCOR_SIGNATURE*, ULONG*) { NOOPMD_NYI(GetMemberRefProps); }
STDMETHODIMP NoopMetadataImport::EnumProperties(HCORENUM*, mdTypeDef, mdProperty[], ULONG, ULONG*) { NOOPMD_NYI(EnumProperties); }
STDMETHODIMP NoopMetadataImport::EnumEvents(HCORENUM*, mdTypeDef, mdEvent[], ULONG, ULONG*) { NOOPMD_NYI(EnumEvents); }
STDMETHODIMP NoopMetadataImport::GetEventProps(mdEvent, mdTypeDef*, LPCWSTR, ULONG, ULONG*, DWORD*, mdToken*, mdMethodDef*, mdMethodDef*, mdMethodDef*, mdMethodDef[], ULONG, ULONG*) { NOOPMD_NYI(GetEventProps); }
STDMETHODIMP NoopMetadataImport::EnumMethodSemantics(HCORENUM*, mdMethodDef, mdToken[], ULONG, ULONG*) { NOOPMD_NYI(EnumMethodSemantics); }
STDMETHODIMP NoopMetadataImport::GetMethodSemantics(mdMethodDef, mdToken, DWORD*) { NOOPMD_NYI(GetMethodSemantics); }
STDMETHODIMP NoopMetadataImport::GetClassLayout(mdTypeDef, DWORD*, COR_FIELD_OFFSET[], ULONG, ULONG*, ULONG*) { NOOPMD_NYI(GetClassLayout); }
STDMETHODIMP NoopMetadataImport::GetFieldMarshal(mdToken, PCCOR_SIGNATURE*, ULONG*) { NOOPMD_NYI(GetFieldMarshal); }
STDMETHODIMP NoopMetadataImport::GetRVA(mdToken, ULONG*, DWORD*) { NOOPMD_NYI(GetRVA); }
STDMETHODIMP NoopMetadataImport::GetPermissionSetProps(mdPermission, DWORD*, void const**, ULONG*) { NOOPMD_NYI(GetPermissionSetProps); }
STDMETHODIMP NoopMetadataImport::GetModuleRefProps(mdModuleRef, LPWSTR, ULONG, ULONG*) { NOOPMD_NYI(GetModuleRefProps); }
STDMETHODIMP NoopMetadataImport::EnumModuleRefs(HCORENUM*, mdModuleRef[], ULONG, ULONG*) { NOOPMD_NYI(EnumModuleRefs); }
STDMETHODIMP NoopMetadataImport::GetTypeSpecFromToken(mdTypeSpec, PCCOR_SIGNATURE*, ULONG*) { NOOPMD_NYI(GetTypeSpecFromToken); }
STDMETHODIMP NoopMetadataImport::GetNameFromToken(mdToken, MDUTF8CSTR*) { NOOPMD_NYI(GetNameFromToken); }
STDMETHODIMP NoopMetadataImport::EnumUnresolvedMethods(HCORENUM*, mdToken[], ULONG, ULONG*) { NOOPMD_NYI(EnumUnresolvedMethods); }
STDMETHODIMP NoopMetadataImport::GetUserString(mdString, LPWSTR, ULONG, ULONG*) { NOOPMD_NYI(GetUserString); }
STDMETHODIMP NoopMetadataImport::GetPinvokeMap(mdToken, DWORD*, LPWSTR, ULONG, ULONG*, mdModuleRef*) { NOOPMD_NYI(GetPinvokeMap); }
STDMETHODIMP NoopMetadataImport::EnumSignatures(HCORENUM*, mdSignature[], ULONG, ULONG*) { NOOPMD_NYI(EnumSignatures); }
STDMETHODIMP NoopMetadataImport::EnumTypeSpecs(HCORENUM*, mdTypeSpec[], ULONG, ULONG*) { NOOPMD_NYI(EnumTypeSpecs); }
STDMETHODIMP NoopMetadataImport::EnumUserStrings(HCORENUM*, mdString[], ULONG, ULONG*) { NOOPMD_NYI(EnumUserStrings); }
STDMETHODIMP NoopMetadataImport::GetParamForMethodIndex(mdMethodDef, ULONG, mdParamDef*) { NOOPMD_NYI(GetParamForMethodIndex); }
STDMETHODIMP NoopMetadataImport::EnumCustomAttributes(HCORENUM*, mdToken, mdToken, mdCustomAttribute[], ULONG, ULONG*) { NOOPMD_NYI(EnumCustomAttributes); }
STDMETHODIMP NoopMetadataImport::GetCustomAttributeProps(mdCustomAttribute, mdToken*, mdToken*, void const**, ULONG*) { NOOPMD_NYI(GetCustomAttributeProps); }
STDMETHODIMP NoopMetadataImport::FindTypeRef(mdToken, LPCWSTR, mdTypeRef*) { NOOPMD_NYI(FindTypeRef); }
STDMETHODIMP NoopMetadataImport::GetMemberProps(mdToken, mdTypeDef*, LPWSTR, ULONG, ULONG*, DWORD*, PCCOR_SIGNATURE*, ULONG*, ULONG*, DWORD*, DWORD*, UVCP_CONSTANT*, ULONG*) { NOOPMD_NYI(GetMemberProps); }
STDMETHODIMP NoopMetadataImport::GetFieldProps(mdFieldDef, mdTypeDef*, LPWSTR, ULONG, ULONG*, DWORD*, PCCOR_SIGNATURE*, ULONG*, DWORD*, UVCP_CONSTANT*, ULONG*) { NOOPMD_NYI(GetFieldProps); }
STDMETHODIMP NoopMetadataImport::GetPropertyProps(mdProperty, mdTypeDef*, LPCWSTR, ULONG, ULONG*, DWORD*, PCCOR_SIGNATURE*, ULONG*, DWORD*, UVCP_CONSTANT*, ULONG*, mdMethodDef*, mdMethodDef*, mdMethodDef[], ULONG, ULONG*) { NOOPMD_NYI(GetPropertyProps); }
STDMETHODIMP NoopMetadataImport::GetParamProps(mdParamDef, mdMethodDef*, ULONG*, LPWSTR, ULONG, ULONG*, DWORD*, DWORD*, UVCP_CONSTANT*, ULONG*) { NOOPMD_NYI(GetParamProps); }
STDMETHODIMP NoopMetadataImport::GetCustomAttributeByName(mdToken, LPCWSTR, const void**, ULONG*) { NOOPMD_NYI(GetCustomAttributeByName); }

STDMETHODIMP_(BOOL) NoopMetadataImport::IsValidToken(mdToken /*tk*/)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE_MSG(false, "NoopMetadataImport::IsValidToken NYI");
    return FALSE;
}

STDMETHODIMP NoopMetadataImport::GetNestedClassProps(mdTypeDef, mdTypeDef*) { NOOPMD_NYI(GetNestedClassProps); }
STDMETHODIMP NoopMetadataImport::GetNativeCallConvFromSig(void const*, ULONG, ULONG*) { NOOPMD_NYI(GetNativeCallConvFromSig); }
STDMETHODIMP NoopMetadataImport::IsGlobal(mdToken, int*) { NOOPMD_NYI(IsGlobal); }
STDMETHODIMP NoopMetadataImport::GetSigFromToken(mdSignature, PCCOR_SIGNATURE*, ULONG*) { NOOPMD_NYI(GetSigFromToken); }

// IMetaDataImport2
STDMETHODIMP NoopMetadataImport::EnumGenericParams(HCORENUM*, mdToken, mdGenericParam[], ULONG, ULONG*) { NOOPMD_NYI(EnumGenericParams); }
STDMETHODIMP NoopMetadataImport::GetGenericParamProps(mdGenericParam, ULONG*, DWORD*, mdToken*, DWORD*, LPWSTR, ULONG, ULONG*) { NOOPMD_NYI(GetGenericParamProps); }
STDMETHODIMP NoopMetadataImport::GetMethodSpecProps(mdMethodSpec, mdToken*, PCCOR_SIGNATURE*, ULONG*) { NOOPMD_NYI(GetMethodSpecProps); }
STDMETHODIMP NoopMetadataImport::EnumGenericParamConstraints(HCORENUM*, mdGenericParam, mdGenericParamConstraint[], ULONG, ULONG*) { NOOPMD_NYI(EnumGenericParamConstraints); }
STDMETHODIMP NoopMetadataImport::GetGenericParamConstraintProps(mdGenericParamConstraint, mdGenericParam*, mdToken*) { NOOPMD_NYI(GetGenericParamConstraintProps); }
STDMETHODIMP NoopMetadataImport::GetPEKind(DWORD*, DWORD*) { NOOPMD_NYI(GetPEKind); }
STDMETHODIMP NoopMetadataImport::GetVersionString(LPWSTR, DWORD, DWORD*) { NOOPMD_NYI(GetVersionString); }
STDMETHODIMP NoopMetadataImport::EnumMethodSpecs(HCORENUM*, mdToken, mdMethodSpec[], ULONG, ULONG*) { NOOPMD_NYI(EnumMethodSpecs); }

#endif // !DACCESS_COMPILE

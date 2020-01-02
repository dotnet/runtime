// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Header: AssemblySpec.cpp
**
** Purpose: Implements Assembly binding class
**
**


**
===========================================================*/

#include "common.h"

#include <stdlib.h>

#include "assemblyspec.hpp"
#include "eeconfig.h"
#include "strongnameinternal.h"
#include "strongnameholders.h"
#include "eventtrace.h"

#ifdef FEATURE_COMINTEROP
#include "clrprivbinderutil.h"
#include "winrthelpers.h"
#endif

#include "../binder/inc/bindertracing.h"

#ifdef _DEBUG
// This debug-only wrapper for LookupAssembly is solely for the use of postconditions and
// assertions. The problem is that the real LookupAssembly can throw an OOM
// simply because it can't allocate scratch space. For the sake of asserting,
// we can treat those as successful lookups.
BOOL UnsafeVerifyLookupAssembly(AssemblySpecBindingCache *pCache, AssemblySpec *pSpec, DomainAssembly *pComparator)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FORBID_FAULT;

    BOOL result = FALSE;

    EX_TRY
    {
        SCAN_IGNORE_FAULT; // Won't go away: This wrapper exists precisely to turn an OOM here into something our postconditions can deal with.
        result = (pComparator == pCache->LookupAssembly(pSpec));
    }
    EX_CATCH
    {
        Exception *ex = GET_EXCEPTION();

        result = ex->IsTransient();
    }
    EX_END_CATCH(SwallowAllExceptions)

    return result;

}
#endif

#ifdef _DEBUG
// This debug-only wrapper for LookupFile is solely for the use of postconditions and
// assertions. The problem is that the real LookupFile can throw an OOM
// simply because it can't allocate scratch space. For the sake of asserting,
// we can treat those as successful lookups.
BOOL UnsafeVerifyLookupFile(AssemblySpecBindingCache *pCache, AssemblySpec *pSpec, PEAssembly *pComparator)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FORBID_FAULT;

    BOOL result = FALSE;

    EX_TRY
    {
        SCAN_IGNORE_FAULT; // Won't go away: This wrapper exists precisely to turn an OOM here into something our postconditions can deal with.
        result = pCache->LookupFile(pSpec)->Equals(pComparator);
    }
    EX_CATCH
    {
        Exception *ex = GET_EXCEPTION();

        result = ex->IsTransient();
    }
    EX_END_CATCH(SwallowAllExceptions)

    return result;

}

#endif

#ifdef _DEBUG

// This debug-only wrapper for Contains is solely for the use of postconditions and
// assertions. The problem is that the real Contains can throw an OOM
// simply because it can't allocate scratch space. For the sake of asserting,
// we can treat those as successful lookups.
BOOL UnsafeContains(AssemblySpecBindingCache *pCache, AssemblySpec *pSpec)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FORBID_FAULT;

    BOOL result = FALSE;

    EX_TRY
    {
        SCAN_IGNORE_FAULT; // Won't go away: This wrapper exists precisely to turn an OOM here into something our postconditions can deal with.
        result = pCache->Contains(pSpec);
    }
    EX_CATCH
    {
        Exception *ex = GET_EXCEPTION();

        result = ex->IsTransient();
    }
    EX_END_CATCH(SwallowAllExceptions)

    return result;

}
#endif



AssemblySpecHash::~AssemblySpecHash()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    PtrHashMap::PtrIterator i = m_map.begin();
    while (!i.end())
    {
        AssemblySpec *s = (AssemblySpec*) i.GetValue();
        if (m_pHeap != NULL)
            s->~AssemblySpec();
        else
            delete s;

        ++i;
    }
}

// Check assembly name for invalid characters
// Return value:
//      TRUE: If no invalid characters were found, or if the assembly name isn't set
//      FALSE: If invalid characters were found
// This is needed to prevent security loopholes with ':', '/' and '\' in the assembly name
BOOL AssemblySpec::IsValidAssemblyName()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    if (GetName())
    {
        SString ssAssemblyName(SString::Utf8, GetName());
        for (SString::Iterator i = ssAssemblyName.Begin(); i[0] != W('\0'); i++) {
            switch (i[0]) {
                case W(':'):
                case W('\\'):
                case W('/'):
                    return FALSE;

                default:
                    break;
            }
        }
    }
    return TRUE;
}

HRESULT AssemblySpec::InitializeSpecInternal(mdToken kAssemblyToken,
                                  IMDInternalImport *pImport,
                                  DomainAssembly *pStaticParent,
                                  BOOL fAllowAllocation)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        if (fAllowAllocation) {GC_TRIGGERS;} else {GC_NOTRIGGER;};
        if (fAllowAllocation) {INJECT_FAULT(COMPlusThrowOM());} else {FORBID_FAULT;};
        NOTHROW;
        MODE_ANY;
        PRECONDITION(pImport->IsValidToken(kAssemblyToken));
        PRECONDITION(TypeFromToken(kAssemblyToken) == mdtAssembly
                     || TypeFromToken(kAssemblyToken) == mdtAssemblyRef);
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    EX_TRY
    {
        IfFailThrow(BaseAssemblySpec::Init(kAssemblyToken,pImport));

        if (IsContentType_WindowsRuntime())
        {
            if (!fAllowAllocation)
            {   // We don't support this because we must be able to allocate in order to
                // extract embedded type names for the native image scenario. Currently,
                // the only caller of this method with fAllowAllocation == FALSE is
                // Module::GetAssemblyIfLoaded, and since this method will only check the
                // assembly spec cache, and since we can't cache WinRT assemblies, this
                // limitation should have no negative impact.
                IfFailThrow(E_FAIL);
            }

            // Extract embedded content, if present (currently used for embedded WinRT type names).
            ParseEncodedName();
        }

        // For static binds, we cannot reference a weakly named assembly from a strong named one.
        // (Note that this constraint doesn't apply to dynamic binds which is why this check is
        // not farther down the stack.)
        if (pStaticParent != NULL)
        {
            // We dont validate this for CoreCLR as there is no good use-case for this scenario.

            SetParentAssembly(pStaticParent);
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
} // AssemblySpec::InitializeSpecInternal



void AssemblySpec::InitializeSpec(PEAssembly * pFile)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pFile));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    ReleaseHolder<IMDInternalImport> pImport(pFile->GetMDImportWithRef());
    mdAssembly a;
    IfFailThrow(pImport->GetAssemblyFromScope(&a));

    InitializeSpec(a, pImport, NULL);

#ifdef FEATURE_COMINTEROP
    if (IsContentType_WindowsRuntime())
    {
        LPCSTR  szNamespace;
        LPCSTR  szTypeName;
        SString ssFakeNameSpaceAllocationBuffer;
        IfFailThrow(::GetFirstWinRTTypeDef(pImport, &szNamespace, &szTypeName, pFile->GetPath(), &ssFakeNameSpaceAllocationBuffer));

        SetWindowsRuntimeType(szNamespace, szTypeName);

        // pFile is not guaranteed to stay around (it might be unloaded with the AppDomain), we have to copy the type name
        CloneFields(WINRT_TYPE_NAME_OWNED);
    }
#endif //FEATURE_COMINTEROP

    // Set the binding context for the AssemblySpec
    ICLRPrivBinder* pCurrentBinder = GetBindingContext();
    ICLRPrivBinder* pExpectedBinder = pFile->GetBindingContext();
    if (pCurrentBinder == NULL)
    {
        // We should aways having the binding context in the PEAssembly. The only exception to this are the following:
        //
        // 1) when we are here during EEStartup and loading mscorlib.dll.
        // 2) We are dealing with dynamic assemblies
        _ASSERTE((pExpectedBinder != NULL) || pFile->IsSystem() || pFile->IsDynamic());
        SetBindingContext(pExpectedBinder);
    }
}

#ifndef CROSSGEN_COMPILE

// This uses thread storage to allocate space. Please use Checkpoint and release it.
HRESULT AssemblySpec::InitializeSpec(StackingAllocator* alloc, ASSEMBLYNAMEREF* pName,
                                  BOOL fParse /*=TRUE*/)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(alloc));
        PRECONDITION(CheckPointer(pName));
        PRECONDITION(IsProtectedByGCFrame(pName));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Simple name
    if ((*pName)->GetSimpleName() != NULL) {
        WCHAR* pString;
        int    iString;
        ((STRINGREF) (*pName)->GetSimpleName())->RefInterpretGetStringValuesDangerousForGC(&pString, &iString);
        DWORD lgth = WszWideCharToMultiByte(CP_UTF8, 0, pString, iString, NULL, 0, NULL, NULL);
        if (lgth + 1 < lgth)
            ThrowHR(E_INVALIDARG);
        LPSTR lpName = (LPSTR) alloc->Alloc(S_UINT32(lgth) + S_UINT32(1));
        WszWideCharToMultiByte(CP_UTF8, 0, pString, iString,
                               lpName, lgth+1, NULL, NULL);
        lpName[lgth] = '\0';
        // Calling Init here will trash the cached lpName in AssemblySpec, but lpName is still needed by ParseName
        // call below.
        SetName(lpName);
    }
    else
    {
        // Ensure we always have an assembly simple name.
        LPSTR lpName = (LPSTR) alloc->Alloc(S_UINT32(1));
        lpName[0] = '\0';
        SetName(lpName);
    }

    if (fParse) {
        HRESULT hr = ParseName();
        // Sometimes Fusion flags invalid characters in the name, sometimes it doesn't
        // depending on where the invalid characters are
        // We want to Raise the assembly resolve event on all invalid characters
        // but calling ParseName before checking for invalid characters gives Fusion a chance to
        // parse the rest of the name (to get a public key token, etc.)
        if ((hr == FUSION_E_INVALID_NAME) || (!IsValidAssemblyName())) {
            // This is the only case where we do not throw on an error
            // We don't want to throw so as to give the caller a chance to call RaiseAssemblyResolveEvent
            // The only caller that cares is System.Reflection.Assembly.InternalLoad which calls us through
            // AssemblyNameNative::Init
            return FUSION_E_INVALID_NAME;
        }
        else
            IfFailThrow(hr);
    }
    else {
        AssemblyMetaDataInternal asmInfo;
        // Flags
        DWORD dwFlags = (*pName)->GetFlags();

        // Version
        VERSIONREF version = (VERSIONREF) (*pName)->GetVersion();
        if(version == NULL) {
            asmInfo.usMajorVersion = (USHORT)-1;
            asmInfo.usMinorVersion = (USHORT)-1;
            asmInfo.usBuildNumber = (USHORT)-1;
            asmInfo.usRevisionNumber = (USHORT)-1;
        }
        else {
            asmInfo.usMajorVersion = (USHORT)version->GetMajor();
            asmInfo.usMinorVersion = (USHORT)version->GetMinor();
            asmInfo.usBuildNumber = (USHORT)version->GetBuild();
            asmInfo.usRevisionNumber = (USHORT)version->GetRevision();
        }

        asmInfo.szLocale = 0;
        asmInfo.ulOS = 0;
        asmInfo.rOS = 0;
        asmInfo.ulProcessor = 0;
        asmInfo.rProcessor = 0;

        if ((*pName)->GetCultureInfo() != NULL)
        {
            struct _gc {
                OBJECTREF   cultureinfo;
                STRINGREF   pString;
            } gc;

            gc.cultureinfo = (*pName)->GetCultureInfo();
            gc.pString = NULL;

            GCPROTECT_BEGIN(gc);

            MethodDescCallSite getName(METHOD__CULTURE_INFO__GET_NAME, &gc.cultureinfo);

            ARG_SLOT args[] = {
                ObjToArgSlot(gc.cultureinfo)
            };
            gc.pString = getName.Call_RetSTRINGREF(args);
            if (gc.pString != NULL) {
                WCHAR* pString;
                int    iString;
                gc.pString->RefInterpretGetStringValuesDangerousForGC(&pString, &iString);
                DWORD lgth = WszWideCharToMultiByte(CP_UTF8, 0, pString, iString, NULL, 0, NULL, NULL);
                S_UINT32 lengthWillNull = S_UINT32(lgth) + S_UINT32(1);
                LPSTR lpLocale = (LPSTR) alloc->Alloc(lengthWillNull);
                if (lengthWillNull.IsOverflow())
                {
                    COMPlusThrowHR(COR_E_OVERFLOW);
                }
                WszWideCharToMultiByte(CP_UTF8, 0, pString, iString,
                                       lpLocale, lengthWillNull.Value(), NULL, NULL);
                lpLocale[lgth] = '\0';
                asmInfo.szLocale = lpLocale;
            }
            GCPROTECT_END();
        }

        // Strong name
        DWORD cbPublicKeyOrToken=0;
        BYTE* pbPublicKeyOrToken=NULL;
        // Note that we prefer to take a public key token if present,
        // even if flags indicate a full public key
        if ((*pName)->GetPublicKeyToken() != NULL) {
            dwFlags &= ~afPublicKey;
            PBYTE  pArray = NULL;
            pArray = (*pName)->GetPublicKeyToken()->GetDirectPointerToNonObjectElements();
            cbPublicKeyOrToken = (*pName)->GetPublicKeyToken()->GetNumComponents();
            pbPublicKeyOrToken = pArray;
        }
        else if ((*pName)->GetPublicKey() != NULL) {
            dwFlags |= afPublicKey;
            PBYTE  pArray = NULL;
            pArray = (*pName)->GetPublicKey()->GetDirectPointerToNonObjectElements();
            cbPublicKeyOrToken = (*pName)->GetPublicKey()->GetNumComponents();
            pbPublicKeyOrToken = pArray;
        }
        BaseAssemblySpec::Init(GetName(),&asmInfo,pbPublicKeyOrToken,cbPublicKeyOrToken,dwFlags);
    }

    CloneFieldsToStackingAllocator(alloc);

    // Extract embedded WinRT name, if present.
    ParseEncodedName();

    return S_OK;
}

void AssemblySpec::AssemblyNameInit(ASSEMBLYNAMEREF* pAsmName, PEImage* pImageInfo)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        PRECONDITION(IsProtectedByGCFrame (pAsmName));
    }
    CONTRACTL_END;

    struct _gc {
        OBJECTREF CultureInfo;
        STRINGREF Locale;
        OBJECTREF Version;
        U1ARRAYREF PublicKeyOrToken;
        STRINGREF Name;
        STRINGREF CodeBase;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    if ((m_context.usMajorVersion != (USHORT) -1) &&
        (m_context.usMinorVersion != (USHORT) -1)) {

        MethodTable* pVersion = MscorlibBinder::GetClass(CLASS__VERSION);

        // version
        gc.Version = AllocateObject(pVersion);

        // BaseAssemblySpec and AssemblyName properties store uint16 components for the version. Version and AssemblyVersion
        // store int32 or uint32. When the former are initialized from the latter, the components are truncated to uint16 size.
        // When the latter are initialized from the former, they are zero-extended to int32 size. For uint16 components, the max
        // value is used to indicate an unspecified component. For int32 components, -1 is used. Since we're initializing a
        // Version from an assembly version, map the uint16 unspecified value to the int32 size.
        int componentCount = 2;
        if (m_context.usBuildNumber != (USHORT)-1)
        {
            ++componentCount;
            if (m_context.usRevisionNumber != (USHORT)-1)
            {
                ++componentCount;
            }
        }
        switch (componentCount)
        {
            case 2:
            {
                // Call Version(int, int) because Version(int, int, int, int) does not allow passing the unspecified value -1
                MethodDescCallSite ctorMethod(METHOD__VERSION__CTOR_Ix2);
                ARG_SLOT VersionArgs[] =
                {
                    ObjToArgSlot(gc.Version),
                    (ARG_SLOT) m_context.usMajorVersion,
                    (ARG_SLOT) m_context.usMinorVersion
                };
                ctorMethod.Call(VersionArgs);
                break;
            }

            case 3:
            {
                // Call Version(int, int, int) because Version(int, int, int, int) does not allow passing the unspecified value -1
                MethodDescCallSite ctorMethod(METHOD__VERSION__CTOR_Ix3);
                ARG_SLOT VersionArgs[] =
                {
                    ObjToArgSlot(gc.Version),
                    (ARG_SLOT) m_context.usMajorVersion,
                    (ARG_SLOT) m_context.usMinorVersion,
                    (ARG_SLOT) m_context.usBuildNumber
                };
                ctorMethod.Call(VersionArgs);
                break;
            }

            default:
            {
                // Call Version(int, int, int, int)
                _ASSERTE(componentCount == 4);
                MethodDescCallSite ctorMethod(METHOD__VERSION__CTOR_Ix4);
                ARG_SLOT VersionArgs[] =
                {
                    ObjToArgSlot(gc.Version),
                    (ARG_SLOT) m_context.usMajorVersion,
                    (ARG_SLOT) m_context.usMinorVersion,
                    (ARG_SLOT) m_context.usBuildNumber,
                    (ARG_SLOT) m_context.usRevisionNumber
                };
                ctorMethod.Call(VersionArgs);
                break;
            }
        }
    }

    // cultureinfo
    if (m_context.szLocale) {

        MethodTable* pCI = MscorlibBinder::GetClass(CLASS__CULTURE_INFO);
        gc.CultureInfo = AllocateObject(pCI);

        gc.Locale = StringObject::NewString(m_context.szLocale);

        MethodDescCallSite strCtor(METHOD__CULTURE_INFO__STR_CTOR);

        ARG_SLOT args[2] =
        {
            ObjToArgSlot(gc.CultureInfo),
            ObjToArgSlot(gc.Locale)
        };

        strCtor.Call(args);
    }

    // public key or token byte array
    if (m_pbPublicKeyOrToken)
    {
        gc.PublicKeyOrToken = (U1ARRAYREF)AllocatePrimitiveArray(ELEMENT_TYPE_U1, m_cbPublicKeyOrToken);
        memcpyNoGCRefs(gc.PublicKeyOrToken->m_Array, m_pbPublicKeyOrToken, m_cbPublicKeyOrToken);
    }

    // simple name
    if(GetName())
        gc.Name = StringObject::NewString(GetName());

    if (GetCodeBase())
        gc.CodeBase = StringObject::NewString(GetCodeBase());

    BOOL fPublicKey = m_dwFlags & afPublicKey;

    ULONG hashAlgId=0;
    if (pImageInfo != NULL)
    {
        if(!pImageInfo->GetMDImport()->IsValidToken(TokenFromRid(1, mdtAssembly)))
        {
            ThrowHR(COR_E_BADIMAGEFORMAT);
        }
        IfFailThrow(pImageInfo->GetMDImport()->GetAssemblyProps(TokenFromRid(1, mdtAssembly), NULL, NULL, &hashAlgId, NULL, NULL, NULL));
    }

    MethodDescCallSite init(METHOD__ASSEMBLY_NAME__CTOR);

    ARG_SLOT MethodArgs[] =
    {
        ObjToArgSlot(*pAsmName),
        ObjToArgSlot(gc.Name),
        fPublicKey ? ObjToArgSlot(gc.PublicKeyOrToken) :
        (ARG_SLOT) NULL, // public key
        fPublicKey ? (ARG_SLOT) NULL :
        ObjToArgSlot(gc.PublicKeyOrToken), // public key token
        ObjToArgSlot(gc.Version),
        ObjToArgSlot(gc.CultureInfo),
        (ARG_SLOT) hashAlgId,
        (ARG_SLOT) 1, // AssemblyVersionCompatibility.SameMachine
        ObjToArgSlot(gc.CodeBase),
        (ARG_SLOT) m_dwFlags,
        (ARG_SLOT) NULL // key pair
    };

    init.Call(MethodArgs);

    // Only set the processor architecture if we're looking at a newer binary that has
    // that information in the PE, and we're not looking at a reference assembly.
    if(pImageInfo && !pImageInfo->HasV1Metadata() && !pImageInfo->IsReferenceAssembly())
    {
        DWORD dwMachine, dwKind;

        pImageInfo->GetPEKindAndMachine(&dwMachine,&dwKind);

        MethodDescCallSite setPA(METHOD__ASSEMBLY_NAME__SET_PROC_ARCH_INDEX);

        ARG_SLOT PAMethodArgs[] = {
            ObjToArgSlot(*pAsmName),
            (ARG_SLOT)dwMachine,
            (ARG_SLOT)dwKind
        };

        setPA.Call(PAMethodArgs);
    }

    GCPROTECT_END();
}

// This uses thread storage to allocate space. Please use Checkpoint and release it.
void AssemblySpec::SetCodeBase(StackingAllocator* alloc, STRINGREF *pCodeBase)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pCodeBase));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Codebase
    if (pCodeBase != NULL && *pCodeBase != NULL) {
        WCHAR* pString;
        int    iString;
        (*pCodeBase)->RefInterpretGetStringValuesDangerousForGC(&pString, &iString);

        DWORD dwCodeBase = (DWORD) iString+1;
        m_wszCodeBase = new (alloc) WCHAR[dwCodeBase];
        memcpy((void*)m_wszCodeBase, pString, dwCodeBase * sizeof(WCHAR));
    }
}

#endif // CROSSGEN_COMPILE

// Check if the supplied assembly's public key matches up with the one in the Spec, if any
// Throws an appropriate exception in case of a mismatch
void AssemblySpec::MatchPublicKeys(Assembly *pAssembly)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Check that the public keys are the same as in the AR.
    if (!IsStrongNamed())
        return;

    const void *pbPublicKey;
    DWORD cbPublicKey;
    pbPublicKey = pAssembly->GetPublicKey(&cbPublicKey);
    if (cbPublicKey == 0)
        ThrowHR(FUSION_E_PRIVATE_ASM_DISALLOWED);

    if (IsAfPublicKey(m_dwFlags))
    {
        if ((m_cbPublicKeyOrToken != cbPublicKey) ||
            memcmp(m_pbPublicKeyOrToken, pbPublicKey, m_cbPublicKeyOrToken))
        {
            ThrowHR(FUSION_E_REF_DEF_MISMATCH);
        }
    }
    else
    {
        // Ref has a token
        StrongNameBufferHolder<BYTE> pbStrongNameToken;
        DWORD cbStrongNameToken;

        IfFailThrow(StrongNameTokenFromPublicKey((BYTE*)pbPublicKey,
            cbPublicKey,
            &pbStrongNameToken,
            &cbStrongNameToken));

        if ((m_cbPublicKeyOrToken != cbStrongNameToken) ||
            memcmp(m_pbPublicKeyOrToken, pbStrongNameToken, cbStrongNameToken))
        {
            ThrowHR(FUSION_E_REF_DEF_MISMATCH);
        }
    }
}

Assembly *AssemblySpec::LoadAssembly(FileLoadLevel targetLevel, BOOL fThrowOnFileNotFound)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    DomainAssembly * pDomainAssembly = LoadDomainAssembly(targetLevel, fThrowOnFileNotFound);
    if (pDomainAssembly == NULL) {
        _ASSERTE(!fThrowOnFileNotFound);
        return NULL;
    }
    return pDomainAssembly->GetAssembly();
}

// Returns a BOOL indicating if the two Binder references point to the same
// binder instance.
BOOL AreSameBinderInstance(ICLRPrivBinder *pBinderA, ICLRPrivBinder *pBinderB)
{
    LIMITED_METHOD_CONTRACT;

    BOOL fIsSameInstance = (pBinderA == pBinderB);

    if (!fIsSameInstance && (pBinderA != NULL) && (pBinderB != NULL))
    {
        // Get the ID for the first binder
        UINT_PTR binderIDA = 0, binderIDB = 0;
        HRESULT hr = pBinderA->GetBinderID(&binderIDA);
        if (SUCCEEDED(hr))
        {
            // Get the ID for the second binder
            hr = pBinderB->GetBinderID(&binderIDB);
            if (SUCCEEDED(hr))
            {
                fIsSameInstance = (binderIDA == binderIDB);
            }
        }
    }

    return fIsSameInstance;
}

ICLRPrivBinder* AssemblySpec::GetBindingContextFromParentAssembly(AppDomain *pDomain)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pDomain != NULL);
    }
    CONTRACTL_END;

    ICLRPrivBinder *pParentAssemblyBinder = NULL;
    DomainAssembly *pParentDomainAssembly = GetParentAssembly();

    if(pParentDomainAssembly != NULL)
    {
        // Get the PEAssembly associated with the parent's domain assembly
        PEAssembly *pParentPEAssembly = pParentDomainAssembly->GetFile();

        // ICLRPrivAssembly implements ICLRPrivBinder and thus, "is a" binder in a manner of semantics.
        pParentAssemblyBinder = pParentPEAssembly->GetBindingContext();
    }

    if (GetPreferFallbackLoadContextBinder())
    {
        // If we have been asked to use the fallback load context binder (currently only supported for AssemblyLoadContext.LoadFromAssemblyName),
        // then pretend we do not have any binder yet available.
        _ASSERTE(GetFallbackLoadContextBinderForRequestingAssembly() != NULL);
        pParentAssemblyBinder = NULL;
    }

    if (pParentAssemblyBinder == NULL)
    {
        // If the parent assembly binder is not available, then we maybe dealing with one of the following
        // assembly scenarios:
        //
        // 1) Domain Neutral assembly
        // 2) Entrypoint assembly
        // 3) AssemblyLoadContext.LoadFromAssemblyName
        //
        // For (1) and (2), we will need to bind against the DefaultContext binder (aka TPA Binder). This happens
        // below if we do not find the parent assembly binder.
        //
        // For (3), fetch the fallback load context binder reference.

        pParentAssemblyBinder = GetFallbackLoadContextBinderForRequestingAssembly();
    }

    if (pParentAssemblyBinder != NULL)
    {
        CLRPrivBinderCoreCLR *pTPABinder = pDomain->GetTPABinderContext();
        if (AreSameBinderInstance(pTPABinder, pParentAssemblyBinder))
        {
            // If the parent assembly is a platform (TPA) assembly, then its binding context will always be the TPABinder context. In
            // such case, we will return the default context for binding to allow the bind to go
            // via the custom binder context, if it was overridden. If it was not overridden, then we will get the expected
            // TPABinder context anyways.
            //
            // Get the reference to the default binding context (this could be the TPABinder context or custom AssemblyLoadContext)
            pParentAssemblyBinder = static_cast<ICLRPrivBinder*>(pDomain->GetTPABinderContext());
        }
    }

#if defined(FEATURE_COMINTEROP)
    if (!IsContentType_WindowsRuntime() && (pParentAssemblyBinder != NULL))
    {
        CLRPrivBinderWinRT *pWinRTBinder = pDomain->GetWinRtBinder();
        if (AreSameBinderInstance(pWinRTBinder, pParentAssemblyBinder))
        {
            // We could be here when a non-WinRT assembly load is triggerred by a winmd (e.g. System.Runtime being loaded due to
            // types being referenced from Windows.Foundation.Winmd).
            //
            // If the AssemblySpec does not correspond to WinRT type but our parent assembly binder is a WinRT binder,
            // then such an assembly will not be found by the binder.
            // In such a case, the parent binder should be the fallback binder for the WinRT assembly if one exists.
            ICLRPrivBinder* pParentWinRTBinder = pParentAssemblyBinder;
            pParentAssemblyBinder = NULL;
            ReleaseHolder<ICLRPrivAssemblyID_WinRT> assembly;
            if (SUCCEEDED(pParentWinRTBinder->QueryInterface<ICLRPrivAssemblyID_WinRT>(&assembly)))
            {
                pParentAssemblyBinder = dac_cast<PTR_CLRPrivAssemblyWinRT>(assembly.GetValue())->GetFallbackBinder();

                // The fallback binder should not be a WinRT binder.
                _ASSERTE(!AreSameBinderInstance(pWinRTBinder, pParentAssemblyBinder));
            }
        }
    }
#endif // defined(FEATURE_COMINTEROP)

    if (!pParentAssemblyBinder)
    {
        // We can be here when loading assemblies via the host (e.g. ICLRRuntimeHost2::ExecuteAssembly) or dealing with assemblies
        // whose parent is a domain neutral assembly (see comment above for details).
        //
        // In such a case, the parent assembly (semantically) is CoreLibrary and thus, the default binding context should be
        // used as the parent assembly binder.
        pParentAssemblyBinder = static_cast<ICLRPrivBinder*>(pDomain->GetTPABinderContext());
    }

    return pParentAssemblyBinder;
}

DomainAssembly *AssemblySpec::LoadDomainAssembly(FileLoadLevel targetLevel,
                                                 BOOL fThrowOnFileNotFound)
{
    CONTRACT(DomainAssembly *)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION((!fThrowOnFileNotFound && CheckPointer(RETVAL, NULL_OK))
                      || CheckPointer(RETVAL));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    ETWOnStartup (LoaderCatchCall_V1, LoaderCatchCallEnd_V1);
    AppDomain* pDomain = GetAppDomain();

    DomainAssembly* pAssembly = nullptr;
    if (CanUseWithBindingCache())
    {
        pAssembly = pDomain->FindCachedAssembly(this);
    }

    if (pAssembly)
    {
        BinderTracing::AssemblyBindOperation bindOperation(this);
        bindOperation.SetResult(pAssembly->GetFile(), true /*cached*/);

        pDomain->LoadDomainFile(pAssembly, targetLevel);
        RETURN pAssembly;
    }

    PEAssemblyHolder pFile(pDomain->BindAssemblySpec(this, fThrowOnFileNotFound));
    if (pFile == NULL)
        RETURN NULL;

    pAssembly = pDomain->LoadDomainAssembly(this, pFile, targetLevel);

    RETURN pAssembly;
}

/* static */
Assembly *AssemblySpec::LoadAssembly(LPCSTR pSimpleName,
                                     AssemblyMetaDataInternal* pContext,
                                     const BYTE * pbPublicKeyOrToken,
                                     DWORD cbPublicKeyOrToken,
                                     DWORD dwFlags)
{
    CONTRACT(Assembly *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pSimpleName));
        POSTCONDITION(CheckPointer(RETVAL));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    AssemblySpec spec;
    IfFailThrow(spec.Init(pSimpleName, pContext,
                          pbPublicKeyOrToken, cbPublicKeyOrToken, dwFlags));

    RETURN spec.LoadAssembly(FILE_LOADED);
}

/* static */
Assembly *AssemblySpec::LoadAssembly(LPCWSTR pFilePath)
{
    CONTRACT(Assembly *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pFilePath));
        POSTCONDITION(CheckPointer(RETVAL));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    AssemblySpec spec;
    spec.SetCodeBase(pFilePath);
    RETURN spec.LoadAssembly(FILE_LOADED);
}

HRESULT AssemblySpec::CheckFriendAssemblyName()
{
    WRAPPER_NO_CONTRACT;

    // Version, Culture, Architecture, and publickeytoken are not permitted
    if ((m_context.usMajorVersion != (USHORT) -1) ||
        (m_context.szLocale != NULL) ||
        (IsAfPA_Specified(m_dwFlags)) ||
        (IsStrongNamed() && !HasPublicKey()))
    {
        return META_E_CA_BAD_FRIENDS_ARGS;
    }
    else
    {
        return S_OK;
    }
}

HRESULT AssemblySpec::EmitToken(
    IMetaDataAssemblyEmit *pEmit,
    mdAssemblyRef *pToken,
    BOOL fUsePublicKeyToken, /*=TRUE*/
    BOOL fMustBeBindable /*=FALSE*/)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        MODE_ANY;
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    EX_TRY
    {
        SmallStackSString ssName;
        fMustBeBindable ? GetEncodedName(ssName) : GetName(ssName);

        ASSEMBLYMETADATA AMD;

        AMD.usMajorVersion = m_context.usMajorVersion;
        AMD.usMinorVersion = m_context.usMinorVersion;
        AMD.usBuildNumber = m_context.usBuildNumber;
        AMD.usRevisionNumber = m_context.usRevisionNumber;

        if (m_context.szLocale) {
            AMD.cbLocale = MultiByteToWideChar(CP_UTF8, 0, m_context.szLocale, -1, NULL, 0);
            if(AMD.cbLocale==0)
                IfFailGo(HRESULT_FROM_GetLastError());
            AMD.szLocale = (LPWSTR) alloca(AMD.cbLocale * sizeof(WCHAR) );
            if(MultiByteToWideChar(CP_UTF8, 0, m_context.szLocale, -1, AMD.szLocale, AMD.cbLocale)==0)
                IfFailGo(HRESULT_FROM_GetLastError());
        }
        else {
            AMD.cbLocale = 0;
            AMD.szLocale = NULL;
        }

        // If we've been asked to emit a public key token in the reference but we've
        // been given a public key then we need to generate the token now.
        if (m_cbPublicKeyOrToken && fUsePublicKeyToken && IsAfPublicKey(m_dwFlags)) {
            StrongNameBufferHolder<BYTE> pbPublicKeyToken;
            DWORD cbPublicKeyToken;
            IfFailThrow(StrongNameTokenFromPublicKey(m_pbPublicKeyOrToken,
                m_cbPublicKeyOrToken,
                &pbPublicKeyToken,
                &cbPublicKeyToken));

            hr = pEmit->DefineAssemblyRef(pbPublicKeyToken,
                                          cbPublicKeyToken,
                                          ssName.GetUnicode(),
                                          &AMD,
                                          NULL,
                                          0,
                                          m_dwFlags & ~afPublicKey,
                                          pToken);
        }
        else {
            hr = pEmit->DefineAssemblyRef(m_pbPublicKeyOrToken,
                                          m_cbPublicKeyOrToken,
                                          ssName.GetUnicode(),
                                          &AMD,
                                          NULL,
                                          0,
                                          m_dwFlags,
                                          pToken);
        }

        hr = S_OK;
    ErrExit:
        ;
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

//===========================================================================================
// Constructs an AssemblySpec for the given IAssemblyName. Recognizes IAssemblyName objects
// that were built from WinRT AssemblySpec objects, extracts the encoded type name, and sets
// the type namespace and class name properties appropriately.

void AssemblySpec::ParseEncodedName()
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END

#ifdef FEATURE_COMINTEROP
    if (IsContentType_WindowsRuntime())
    {
        StackSString ssEncodedName(SString::Utf8, m_pAssemblyName);
        ssEncodedName.Normalize();

        SString::Iterator itBang = ssEncodedName.Begin();
        if (ssEncodedName.Find(itBang, SL(W("!"))))
        {
            StackSString ssAssemblyName(ssEncodedName, ssEncodedName.Begin(), itBang - ssEncodedName.Begin());
            StackSString ssTypeName(ssEncodedName, ++itBang, ssEncodedName.End() - itBang);
            SetName(ssAssemblyName);
            SetWindowsRuntimeType(ssTypeName);
        }
    }
#endif
}

void AssemblySpec::SetWindowsRuntimeType(
    LPCUTF8 szNamespace,
    LPCUTF8 szClassName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
#ifdef FEATURE_COMINTEROP
    // Release already allocated string
    if (m_ownedFlags & WINRT_TYPE_NAME_OWNED)
    {
        if (m_szWinRtTypeNamespace != nullptr)
            delete [] m_szWinRtTypeNamespace;
        if (m_szWinRtTypeClassName != nullptr)
            delete [] m_szWinRtTypeClassName;
    }
    m_szWinRtTypeNamespace = szNamespace;
    m_szWinRtTypeClassName = szClassName;

    m_ownedFlags &= ~WINRT_TYPE_NAME_OWNED;
#else
    // Classic (non-phone) CoreCLR does not support WinRT interop; this should never be called with a non-empty type name
    _ASSERTE((szNamespace == NULL) && (szClassName == NULL));
#endif
}

void AssemblySpec::SetWindowsRuntimeType(
    SString const & _ssTypeName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Release already allocated string
    if (m_ownedFlags & WINRT_TYPE_NAME_OWNED)
    {
        if (m_szWinRtTypeNamespace != nullptr)
            delete[] m_szWinRtTypeNamespace;
        if (m_szWinRtTypeClassName != nullptr)
            delete[] m_szWinRtTypeClassName;
        m_ownedFlags &= ~WINRT_TYPE_NAME_OWNED;
    }

    SString ssTypeName;
    _ssTypeName.ConvertToUTF8(ssTypeName);

    LPUTF8 szTypeName = (LPUTF8)ssTypeName.GetUTF8NoConvert();
    ns::SplitInline(szTypeName, m_szWinRtTypeNamespace, m_szWinRtTypeClassName);
    m_ownedFlags &= ~WINRT_TYPE_NAME_OWNED;
    // Make a copy of the type name strings
    CloneFields(WINRT_TYPE_NAME_OWNED);
}


AssemblySpecBindingCache::AssemblySpecBindingCache()
{
    LIMITED_METHOD_CONTRACT;
}

AssemblySpecBindingCache::~AssemblySpecBindingCache()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    Clear();
}

void AssemblySpecBindingCache::Clear()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    PtrHashMap::PtrIterator i = m_map.begin();
    while (!i.end())
    {
        AssemblyBinding *b = (AssemblyBinding*) i.GetValue();
        if (m_pHeap == NULL)
            delete b;
        else
            b->~AssemblyBinding();

        ++i;
    }

    m_map.Clear();
}

void AssemblySpecBindingCache::Init(CrstBase *pCrst, LoaderHeap *pHeap)
{
    WRAPPER_NO_CONTRACT;

    LockOwner lock = {pCrst, IsOwnerOfCrst};
    m_map.Init(INITIAL_ASM_SPEC_HASH_SIZE, CompareSpecs, TRUE, &lock);
    m_pHeap = pHeap;
}

AssemblySpecBindingCache::AssemblyBinding* AssemblySpecBindingCache::LookupInternal(AssemblySpec* pSpec, BOOL fThrow)
{
    CONTRACTL
    {
        if (fThrow)
        {
            THROWS;
            GC_TRIGGERS;
            INJECT_FAULT(COMPlusThrowOM(););
        }
        else
        {
            GC_NOTRIGGER;
            NOTHROW;
            FORBID_FAULT;
        }
        MODE_ANY;
        PRECONDITION(pSpec != NULL);
    }
    CONTRACTL_END;

    UPTR key = (UPTR)pSpec->Hash();
    UPTR lookupKey = key;

    // On CoreCLR, we will use the BinderID as the key
    ICLRPrivBinder *pBinderContextForLookup = NULL;
    AppDomain *pSpecDomain = pSpec->GetAppDomain();
    bool fGetBindingContextFromParent = true;

    // Check if the AssemblySpec already has specified its binding context. This will be set for assemblies that are
    // attempted to be explicitly bound using AssemblyLoadContext LoadFrom* methods.
    if(!pSpec->IsAssemblySpecForMscorlib())
        pBinderContextForLookup = pSpec->GetBindingContext();
    else
    {
        // For System.Private.Corelib Binding context is either not set or if set then it should be TPA
        _ASSERTE(pSpec->GetBindingContext() == NULL || pSpec->GetBindingContext() == pSpecDomain->GetTPABinderContext());
    }

    if (pBinderContextForLookup != NULL)
    {
        // We are working with the actual binding context in which the assembly was expected to be loaded.
        // Thus, we don't need to get it from the parent assembly.
        fGetBindingContextFromParent = false;
    }

    if (fGetBindingContextFromParent)
    {
        // MScorlib does not have a binding context associated with it and its lookup will only be done
        // using its AssemblySpec hash.
        if (!pSpec->IsAssemblySpecForMscorlib())
        {
            pBinderContextForLookup = pSpec->GetBindingContextFromParentAssembly(pSpecDomain);
            pSpec->SetBindingContext(pBinderContextForLookup);
        }
    }

    if (pBinderContextForLookup)
    {
        UINT_PTR binderID = 0;
        HRESULT hr = pBinderContextForLookup->GetBinderID(&binderID);
        _ASSERTE(SUCCEEDED(hr));
        lookupKey = key^binderID;
    }

    AssemblyBinding* pEntry = (AssemblyBinding *)m_map.LookupValue(lookupKey, pSpec);

    // Reset the binding context if one was originally never present in the AssemblySpec and we didnt find any entry
    // in the cache.
    if (fGetBindingContextFromParent)
    {
        if (pEntry == (AssemblyBinding *) INVALIDENTRY)
        {
            pSpec->SetBindingContext(NULL);
        }
    }

    return pEntry;
}

BOOL AssemblySpecBindingCache::Contains(AssemblySpec *pSpec)
{
    WRAPPER_NO_CONTRACT;
    return (LookupInternal(pSpec, TRUE) != (AssemblyBinding *) INVALIDENTRY);
}

DomainAssembly *AssemblySpecBindingCache::LookupAssembly(AssemblySpec *pSpec,
                                                         BOOL fThrow /*=TRUE*/)
{
    CONTRACT(DomainAssembly *)
    {
        INSTANCE_CHECK;
        if (fThrow) {
            GC_TRIGGERS;
            THROWS;
            INJECT_FAULT(COMPlusThrowOM(););
        }
        else {
            GC_NOTRIGGER;
            NOTHROW;
            FORBID_FAULT;
        }
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    AssemblyBinding *entry = (AssemblyBinding *) INVALIDENTRY;

    entry = LookupInternal(pSpec, fThrow);

    if (entry == (AssemblyBinding *) INVALIDENTRY)
        RETURN NULL;
    else
    {
        if ((entry->GetAssembly() == NULL) && fThrow)
        {
            // May be either unloaded, or an exception occurred.
            entry->ThrowIfError();
        }

        RETURN entry->GetAssembly();
    }
}

PEAssembly *AssemblySpecBindingCache::LookupFile(AssemblySpec *pSpec, BOOL fThrow /*=TRUE*/)
{
    CONTRACT(PEAssembly *)
    {
        INSTANCE_CHECK;
        if (fThrow) {
            GC_TRIGGERS;
            THROWS;
            INJECT_FAULT(COMPlusThrowOM(););
        }
        else {
            GC_NOTRIGGER;
            NOTHROW;
            FORBID_FAULT;
        }
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    AssemblyBinding *entry = (AssemblyBinding *) INVALIDENTRY;
    entry = LookupInternal(pSpec, fThrow);

    if (entry == (AssemblyBinding *) INVALIDENTRY)
        RETURN NULL;
    else
    {
        if (fThrow && (entry->GetFile() == NULL))
        {
            CONSISTENCY_CHECK(entry->IsError());
            entry->ThrowIfError();
        }

        RETURN entry->GetFile();
    }
}


class AssemblyBindingHolder
{
public:
    AssemblyBindingHolder()
    {
        LIMITED_METHOD_CONTRACT;
        m_entry = NULL;
        m_pHeap = NULL;
    }

    AssemblySpecBindingCache::AssemblyBinding *CreateAssemblyBinding(LoaderHeap *pHeap)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            INJECT_FAULT(COMPlusThrowOM(););
        }
        CONTRACTL_END

        m_pHeap = pHeap;
        if (pHeap)
        {
            m_entry = new (m_amTracker.Track(pHeap->AllocMem(S_SIZE_T(sizeof(AssemblySpecBindingCache::AssemblyBinding))))) AssemblySpecBindingCache::AssemblyBinding;
        }
        else
        {
            m_entry = new AssemblySpecBindingCache::AssemblyBinding;
        }
        return m_entry;
    }

    ~AssemblyBindingHolder()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            FORBID_FAULT;
        }
        CONTRACTL_END

        if (m_entry)
        {
            if (m_pHeap)
            {
                // just call destructor - m_amTracker will delete the memory for m_entry itself.
                m_entry->~AssemblyBinding();
            }
            else
            {
                delete m_entry;
            }
        }
    }

    void SuppressRelease()
    {
        LIMITED_METHOD_CONTRACT;
        m_entry = NULL;
        m_pHeap = NULL;
        m_amTracker.SuppressRelease();
    }

    AllocMemTracker *GetPamTracker()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_amTracker;
    }



private:
    AssemblySpecBindingCache::AssemblyBinding *m_entry;
    LoaderHeap                                *m_pHeap;
    AllocMemTracker                            m_amTracker;
};

// NOTE ABOUT STATE OF CACHE ENTRIES:
//
// A cache entry can be in one of 4 states:
// 1. Empty (no entry)
// 2. File (a PEAssembly has been bound, but not yet an Assembly)
// 3. Assembly (Both a PEAssembly & Assembly are available.)
// 4. Error (an error has occurred)
//
// The legal state transitions are:
// 1 -> any
// 2 -> 3
// 2 -> 4


BOOL AssemblySpecBindingCache::StoreAssembly(AssemblySpec *pSpec, DomainAssembly *pAssembly)
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(UnsafeContains(this, pSpec));
        POSTCONDITION(UnsafeVerifyLookupAssembly(this, pSpec, pAssembly));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    UPTR key = (UPTR)pSpec->Hash();

    // On CoreCLR, we will use the BinderID as the key
    ICLRPrivBinder* pBinderContextForLookup = pAssembly->GetFile()->GetBindingContext();

    _ASSERTE(pBinderContextForLookup || pAssembly->GetFile()->IsSystem());
    if (pBinderContextForLookup)
    {
        UINT_PTR binderID = 0;
        HRESULT hr = pBinderContextForLookup->GetBinderID(&binderID);
        _ASSERTE(SUCCEEDED(hr));
        key = key^binderID;

        if (!pSpec->GetBindingContext())
        {
            pSpec->SetBindingContext(pBinderContextForLookup);
        }
    }

    AssemblyBinding *entry = (AssemblyBinding *) m_map.LookupValue(key, pSpec);

    if (entry == (AssemblyBinding *) INVALIDENTRY)
    {
        AssemblyBindingHolder abHolder;

        LoaderHeap* pHeap = m_pHeap;
        if (pAssembly->IsCollectible())
        {
            pHeap = pAssembly->GetLoaderAllocator()->GetHighFrequencyHeap();
        }

        entry = abHolder.CreateAssemblyBinding(pHeap);
        entry->Init(pSpec,pAssembly->GetFile(),pAssembly,NULL,pHeap, abHolder.GetPamTracker());

        m_map.InsertValue(key, entry);

        abHolder.SuppressRelease();

        STRESS_LOG2(LF_CLASSLOADER,LL_INFO10,"StoreFile (StoreAssembly): Add cached entry (%p) with PEFile %p",entry,pAssembly->GetFile());
        RETURN TRUE;
    }
    else
    {
        if (!entry->IsError())
        {
            if (entry->GetAssembly() != NULL)
            {
                // OK if this is a duplicate
                if (entry->GetAssembly() == pAssembly)
                    RETURN TRUE;
            }
            else
            {
                // OK if we have have a matching PEAssembly
                if (entry->GetFile() != NULL
                    && pAssembly->GetFile()->Equals(entry->GetFile()))
                {
                    entry->SetAssembly(pAssembly);
                    RETURN TRUE;
                }
            }
        }

        // Invalid cache transition (see above note about state transitions)
        RETURN FALSE;
    }
}

// Note that this routine may be called outside a lock, so may be racing with another thread.
// Returns TRUE if add was successful - if FALSE is returned, caller should honor current
// cached value to ensure consistency.

BOOL AssemblySpecBindingCache::StoreFile(AssemblySpec *pSpec, PEAssembly *pFile)
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION((!RETVAL) || (UnsafeContains(this, pSpec) && UnsafeVerifyLookupFile(this, pSpec, pFile)));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    UPTR key = (UPTR)pSpec->Hash();

    // On CoreCLR, we will use the BinderID as the key
    ICLRPrivBinder* pBinderContextForLookup = pFile->GetBindingContext();

    _ASSERTE(pBinderContextForLookup || pFile->IsSystem());
    if (pBinderContextForLookup)
    {
        UINT_PTR binderID = 0;
        HRESULT hr = pBinderContextForLookup->GetBinderID(&binderID);
        _ASSERTE(SUCCEEDED(hr));
        key = key^binderID;

        if (!pSpec->GetBindingContext())
        {
            pSpec->SetBindingContext(pBinderContextForLookup);
        }
    }

    AssemblyBinding *entry = (AssemblyBinding *) m_map.LookupValue(key, pSpec);

    if (entry == (AssemblyBinding *) INVALIDENTRY)
    {
        AssemblyBindingHolder abHolder;

        LoaderHeap* pHeap = m_pHeap;

#ifndef CROSSGEN_COMPILE
        if (pBinderContextForLookup != NULL)
        {
            LoaderAllocator* pLoaderAllocator = NULL;

            // Assemblies loaded with AssemblyLoadContext need to use a different heap if
            // marked as collectible
            if (SUCCEEDED(pBinderContextForLookup->GetLoaderAllocator((LPVOID*)&pLoaderAllocator)))
            {
                _ASSERTE(pLoaderAllocator != NULL);
                pHeap = pLoaderAllocator->GetHighFrequencyHeap();
            }
        }
#endif // !CROSSGEN_COMPILE

        entry = abHolder.CreateAssemblyBinding(pHeap);

        entry->Init(pSpec,pFile,NULL,NULL,pHeap, abHolder.GetPamTracker());

        m_map.InsertValue(key, entry);
        abHolder.SuppressRelease();

        STRESS_LOG2(LF_CLASSLOADER,LL_INFO10,"StoreFile: Add cached entry (%p) with PEFile %p\n", entry, pFile);

        RETURN TRUE;
    }
    else
    {
        if (!entry->IsError())
        {
            // OK if this is a duplicate
            if (entry->GetFile() != NULL
                && pFile->Equals(entry->GetFile()))
                RETURN TRUE;
        }
        else
        if (entry->IsPostBindError())
        {
            // Another thread has reported what's going to happen later.
            entry->ThrowIfError();

        }
        STRESS_LOG2(LF_CLASSLOADER,LL_INFO10,"Incompatible cached entry found (%p) when adding PEFile %p\n", entry, pFile);
        // Invalid cache transition (see above note about state transitions)
        RETURN FALSE;
    }
}

BOOL AssemblySpecBindingCache::StoreException(AssemblySpec *pSpec, Exception* pEx)
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        DISABLED(POSTCONDITION(UnsafeContains(this, pSpec))); //<TODO>@todo: Getting violations here - StoreExceptions could happen anywhere so this is possibly too aggressive.</TODO>
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    UPTR key = (UPTR)pSpec->Hash();

    AssemblyBinding *entry = LookupInternal(pSpec, TRUE);
    if (entry == (AssemblyBinding *) INVALIDENTRY)
    {
        // TODO: Merge this with the failure lookup in the binder
        //
        // Since no entry was found for this assembly in any binding context, save the failure
        // in the TPABinder context
        ICLRPrivBinder* pBinderToSaveException = NULL;
        pBinderToSaveException = pSpec->GetBindingContext();
        if (pBinderToSaveException == NULL)
        {
            if (!pSpec->IsAssemblySpecForMscorlib())
            {
                pBinderToSaveException = pSpec->GetBindingContextFromParentAssembly(pSpec->GetAppDomain());
                UINT_PTR binderID = 0;
                HRESULT hr = pBinderToSaveException->GetBinderID(&binderID);
                _ASSERTE(SUCCEEDED(hr));
                key = key^binderID;
            }
        }
    }

    if (entry == (AssemblyBinding *) INVALIDENTRY) {
        AssemblyBindingHolder abHolder;
        entry = abHolder.CreateAssemblyBinding(m_pHeap);

        entry->Init(pSpec,NULL,NULL,pEx,m_pHeap, abHolder.GetPamTracker());

        m_map.InsertValue(key, entry);
        abHolder.SuppressRelease();

        STRESS_LOG2(LF_CLASSLOADER,LL_INFO10,"StoreFile (StoreException): Add cached entry (%p) with exception %p",entry,pEx);
        RETURN TRUE;
    }
    else
    {
        // OK if this is a duplicate
        if (entry->IsError())
        {
            if (entry->GetHR() == pEx->GetHR())
                RETURN TRUE;
        }
        else
        {
            // OK to transition to error if we don't have an Assembly yet
            if (entry->GetAssembly() == NULL)
            {
                entry->InitException(pEx);
                RETURN TRUE;
            }
        }

        // Invalid cache transition (see above note about state transitions)
        RETURN FALSE;
    }
}

BOOL AssemblySpecBindingCache::RemoveAssembly(DomainAssembly* pAssembly)
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pAssembly != NULL);
    }
    CONTRACT_END;
    BOOL result = FALSE;
    PtrHashMap::PtrIterator i = m_map.begin();
    while (!i.end())
    {
        AssemblyBinding* entry = (AssemblyBinding*)i.GetValue();
        if (entry->GetAssembly() == pAssembly)
        {
            UPTR key = i.GetKey();
            m_map.DeleteValue(key, entry);

            if (m_pHeap == NULL)
                delete entry;
            else
                entry->~AssemblyBinding();

            result = TRUE;
        }
        ++i;
    }

    RETURN result;
}

/* static */
BOOL AssemblySpecHash::CompareSpecs(UPTR u1, UPTR u2)
{
    // the same...
    WRAPPER_NO_CONTRACT;
    return AssemblySpecBindingCache::CompareSpecs(u1,u2);
}

/* static */
BOOL AssemblySpecBindingCache::CompareSpecs(UPTR u1, UPTR u2)
{
    WRAPPER_NO_CONTRACT;
    AssemblySpec *a1 = (AssemblySpec *) (u1 << 1);
    AssemblySpec *a2 = (AssemblySpec *) u2;

    return a1->CompareEx(a2);
}

/* static */
BOOL DomainAssemblyCache::CompareBindingSpec(UPTR spec1, UPTR spec2)
{
    WRAPPER_NO_CONTRACT;

    AssemblySpec* pSpec1 = (AssemblySpec*) (spec1 << 1);
    AssemblyEntry* pEntry2 = (AssemblyEntry*) spec2;

    return pSpec1->CompareEx(&pEntry2->spec);
}

DomainAssemblyCache::AssemblyEntry* DomainAssemblyCache::LookupEntry(AssemblySpec* pSpec)
{
    CONTRACT (DomainAssemblyCache::AssemblyEntry*)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END

    DWORD hashValue = pSpec->Hash();

    LPVOID pResult = m_Table.LookupValue(hashValue, pSpec);
    if(pResult == (LPVOID) INVALIDENTRY)
        RETURN NULL;
    else
        RETURN (AssemblyEntry*) pResult;
}

VOID DomainAssemblyCache::InsertEntry(AssemblySpec* pSpec, LPVOID pData1, LPVOID pData2/*=NULL*/)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    LPVOID ptr = LookupEntry(pSpec);
    if(ptr == NULL) {

        BaseDomain::CacheLockHolder lh(m_pDomain);

        ptr = LookupEntry(pSpec);
        if(ptr == NULL) {
            AllocMemTracker amTracker;
            AllocMemTracker *pamTracker = &amTracker;

            AssemblyEntry* pEntry = (AssemblyEntry*) pamTracker->Track( m_pDomain->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(AssemblyEntry))) );
            new (&pEntry->spec) AssemblySpec ();

            pEntry->spec.CopyFrom(pSpec);
            pEntry->spec.CloneFieldsToLoaderHeap(AssemblySpec::ALL_OWNED, m_pDomain->GetLowFrequencyHeap(), pamTracker);

            // Clear the parent assembly, it is not needed for the AssemblySpec in the cache entry and it could contain stale
            // pointer when the parent was a collectible assembly that was collected.
            pEntry->spec.SetParentAssembly(NULL);

            pEntry->pData[0] = pData1;
            pEntry->pData[1] = pData2;
            DWORD hashValue = pEntry->Hash();
            m_Table.InsertValue(hashValue, pEntry);

            pamTracker->SuppressRelease();
        }
        // lh goes out of scope here
    }
#ifdef _DEBUG
    else {
        _ASSERTE(pData1 == ((AssemblyEntry*) ptr)->pData[0]);
        _ASSERTE(pData2 == ((AssemblyEntry*) ptr)->pData[1]);
    }
#endif

}




DomainAssembly * AssemblySpec::GetParentAssembly()
{
    LIMITED_METHOD_CONTRACT;
    return m_pParentAssembly;
}

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

#ifdef FEATURE_FUSION
#include "actasm.h"
#include "appctx.h"
#endif
#include "assemblyspec.hpp"
#include "security.h"
#include "eeconfig.h"
#include "strongname.h"
#include "strongnameholders.h"
#ifdef FEATURE_FUSION
#include "assemblysink.h"
#include "dbglog.h"
#include "bindinglog.hpp"
#include "assemblyfilehash.h"
#endif
#include "mdaassistants.h"
#include "eventtrace.h"

#ifdef FEATURE_COMINTEROP
#include "clrprivbinderutil.h"
#include "winrthelpers.h"
#endif

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
                                  BOOL fIntrospectionOnly, 
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
        PRECONDITION(pStaticParent == NULL || !(pStaticParent->IsIntrospectionOnly() && !fIntrospectionOnly));   //Something's wrong if an introspection assembly loads an assembly for execution.
    }
    CONTRACTL_END;
    
    HRESULT hr = S_OK;
    
    EX_TRY
    {
        // We also did this check as a precondition as we should have prevented this structurally - but just 
        // in case, make sure retail stops us from proceeding further.
        if (pStaticParent != NULL && pStaticParent->IsIntrospectionOnly() && !fIntrospectionOnly)
        {
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
        }
        
        // Normalize this boolean as it tends to be used for comparisons
        m_fIntrospectionOnly = !!fIntrospectionOnly;

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
#if !defined(FEATURE_CORECLR)        
            // It is OK for signed assemblies to reference WinRT assemblies (.winmd files) that are not signed
            if (!IsContentType_WindowsRuntime() && pStaticParent->GetFile()->IsStrongNamed() && !IsStrongNamed())
            {
                ThrowHR(FUSION_E_PRIVATE_ASM_DISALLOWED);
            }
#endif // !defined(FEATURE_CORECLR)
            
            SetParentAssembly(pStaticParent);
        }
    }
    EX_CATCH_HRESULT(hr);
    
    return hr;
} // AssemblySpec::InitializeSpecInternal

#ifdef FEATURE_FUSION
void AssemblySpec::InitializeSpec(IAssemblyName *pName,
                                  DomainAssembly *pStaticParent /*=NULL*/ ,
                                  BOOL fIntrospectionOnly /*=FALSE*/  )
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Normalize this boolean as it tends to be used for comparisons
    m_fIntrospectionOnly = !!fIntrospectionOnly;
    IfFailThrow(Init(pName));

    // For static binds, we cannot reference a strongly named assembly from a weakly named one.
    // (Note that this constraint doesn't apply to dynamic binds which is why this check is
    // not farther down the stack.)

    if (pStaticParent != NULL) {
        if (pStaticParent->GetFile()->IsStrongNamed() && !IsStrongNamed())
        {
            EEFileLoadException::Throw(this, FUSION_E_PRIVATE_ASM_DISALLOWED);
        }
        SetParentAssembly(pStaticParent);
    }

    // Extract embedded WinRT name, if present.
    ParseEncodedName();
}
#endif //FEATURE_FUSION

#ifdef FEATURE_MIXEDMODE
void AssemblySpec::InitializeSpec(HMODULE hMod,
                                  BOOL fIntrospectionOnly /*=FALSE*/)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Normalize this boolean as it tends to be used for comparisons
    m_fIntrospectionOnly = !!fIntrospectionOnly;

    PEDecoder pe(hMod);

    if (!pe.CheckILFormat())
    {
        StackSString path;
        PEImage::GetPathFromDll(hMod, path);
        EEFileLoadException::Throw(path, COR_E_BADIMAGEFORMAT);
    }

    COUNT_T size;
    const void *data = pe.GetMetadata(&size);   
    SafeComHolder<IMDInternalImport> pImport;
    IfFailThrow(GetMetaDataInternalInterface((void *) data, size, ofRead, 
                                             IID_IMDInternalImport,
                                             (void **) &pImport));

    mdAssembly a;
    if (FAILED(pImport->GetAssemblyFromScope(&a)))
        ThrowHR(COR_E_ASSEMBLYEXPECTED);

    InitializeSpec(a, pImport, NULL, fIntrospectionOnly);
}
#endif //FEATURE_MIXEDMODE

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

    InitializeSpec(a, pImport, NULL, pFile->IsIntrospectionOnly());
    
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

#if defined(FEATURE_CORECLR)
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
#endif // defined(FEATURE_CORECLR)
}

#ifndef CROSSGEN_COMPILE

// This uses thread storage to allocate space. Please use Checkpoint and release it.
#ifdef FEATURE_FUSION
HRESULT AssemblySpec::InitializeSpec(StackingAllocator* alloc, ASSEMBLYNAMEREF* pName, 
                                  BOOL fParse /*=TRUE*/, BOOL fIntrospectionOnly /*=FALSE*/)
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
        m_pAssemblyName = lpName;
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
        // Flags
        m_dwFlags = (*pName)->GetFlags();
    
        // Version
        VERSIONREF version = (VERSIONREF) (*pName)->GetVersion();
        if(version == NULL) {
            m_context.usMajorVersion = (USHORT)-1;
            m_context.usMinorVersion = (USHORT)-1;
            m_context.usBuildNumber = (USHORT)-1;
            m_context.usRevisionNumber = (USHORT)-1;
        }
        else {
            m_context.usMajorVersion = (USHORT)version->GetMajor();
            m_context.usMinorVersion = (USHORT)version->GetMinor();
            m_context.usBuildNumber = (USHORT)version->GetBuild();
            m_context.usRevisionNumber = (USHORT)version->GetRevision();
        }

        m_context.szLocale = 0;

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
                LPSTR lpLocale = (LPSTR) alloc->Alloc(S_UINT32(lgth) + S_UINT32(1));
                WszWideCharToMultiByte(CP_UTF8, 0, pString, iString,
                                       lpLocale, lgth+1, NULL, NULL);
                lpLocale[lgth] = '\0';
                m_context.szLocale = lpLocale;
            }
            GCPROTECT_END();
        }

        // Strong name
        // Note that we prefer to take a public key token if present,
        // even if flags indicate a full public key
        if ((*pName)->GetPublicKeyToken() != NULL) {
            m_dwFlags &= ~afPublicKey;
            PBYTE  pArray = NULL;
            pArray = (*pName)->GetPublicKeyToken()->GetDirectPointerToNonObjectElements();
            m_cbPublicKeyOrToken = (*pName)->GetPublicKeyToken()->GetNumComponents();
            m_pbPublicKeyOrToken = new (alloc) BYTE[m_cbPublicKeyOrToken];
            memcpy(m_pbPublicKeyOrToken, pArray, m_cbPublicKeyOrToken);
        }
        else if ((*pName)->GetPublicKey() != NULL) {
            m_dwFlags |= afPublicKey;
            PBYTE  pArray = NULL;
            pArray = (*pName)->GetPublicKey()->GetDirectPointerToNonObjectElements();
            m_cbPublicKeyOrToken = (*pName)->GetPublicKey()->GetNumComponents();
            m_pbPublicKeyOrToken = new (alloc) BYTE[m_cbPublicKeyOrToken]; 
            memcpy(m_pbPublicKeyOrToken, pArray, m_cbPublicKeyOrToken);
        }
    }

    // Hash for control 
    // <TODO>@TODO cts, can we use unsafe in this case!!!</TODO>
    if ((*pName)->GetHashForControl() != NULL)
        SetHashForControl((*pName)->GetHashForControl()->GetDataPtr(), 
                          (*pName)->GetHashForControl()->GetNumComponents(), 
                          (*pName)->GetHashAlgorithmForControl());

    // Normalize this boolean as it tends to be used for comparisons
    m_fIntrospectionOnly = !!fIntrospectionOnly;

    // Extract embedded WinRT name, if present.
    ParseEncodedName();

    return S_OK;
}

#else // FEATURE_FUSION
HRESULT AssemblySpec::InitializeSpec(StackingAllocator* alloc, ASSEMBLYNAMEREF* pName, 
                                  BOOL fParse /*=TRUE*/, BOOL fIntrospectionOnly /*=FALSE*/)
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

    // Hash for control 
    // <TODO>@TODO cts, can we use unsafe in this case!!!</TODO>
    if ((*pName)->GetHashForControl() != NULL)
        SetHashForControl((*pName)->GetHashForControl()->GetDataPtr(), 
                          (*pName)->GetHashForControl()->GetNumComponents(), 
                          (*pName)->GetHashAlgorithmForControl());

    // Normalize this boolean as it tends to be used for comparisons
    m_fIntrospectionOnly = !!fIntrospectionOnly;

    // Extract embedded WinRT name, if present.
    ParseEncodedName();

    return S_OK;
}
#endif // FEATURE_FUSION

void AssemblySpec::AssemblyNameInit(ASSEMBLYNAMEREF* pAsmName, PEImage* pImageInfo)
{
    CONTRACTL 
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        SO_INTOLERANT;
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


        MethodDescCallSite ctorMethod(METHOD__VERSION__CTOR);
            
        ARG_SLOT VersionArgs[5] =
        {
            ObjToArgSlot(gc.Version),
            (ARG_SLOT) m_context.usMajorVersion,      
            (ARG_SLOT) m_context.usMinorVersion,
            (ARG_SLOT) m_context.usBuildNumber,
            (ARG_SLOT) m_context.usRevisionNumber,
        };
        ctorMethod.Call(VersionArgs);
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
        Security::CopyEncodingToByteArray((BYTE*) m_pbPublicKeyOrToken,
                                          m_cbPublicKeyOrToken,
                                          (OBJECTREF*) &gc.PublicKeyOrToken);

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

    MethodDescCallSite init(METHOD__ASSEMBLY_NAME__INIT);
    
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

#ifdef FEATURE_FUSION

/* static */
void AssemblySpec::DemandFileIOPermission(PEAssembly *pFile)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pFile));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // should have already checked permission if the codebase is set
    if (!GetCodeBase()) { 

        if (pFile->IsBindingCodeBase()) {
            if (pFile->IsSourceDownloadCache()) {
                StackSString check;
                pFile->GetCodeBase(check);

                DemandFileIOPermission(check, FALSE, FILE_WEBPERM);
            }
            else
                DemandFileIOPermission(pFile->GetPath(), TRUE, FILE_READANDPATHDISC);
        }
    }
}

STDAPI RuntimeCheckLocationAccess(LPCWSTR wszLocation)
{

    if (GetThread()==NULL)
        return S_FALSE;

    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(wszLocation));
    }
    CONTRACTL_END;
    OVERRIDE_LOAD_LEVEL_LIMIT(FILE_ACTIVE);
    HRESULT hr=S_OK;
    DWORD dwDemand = 0;

    if (SString::_wcsnicmp(wszLocation, W("file"), 4))
        dwDemand = AssemblySpec::FILE_WEBPERM;
    else
        dwDemand = AssemblySpec::FILE_READANDPATHDISC;

    EX_TRY
    {
        AssemblySpec::DemandFileIOPermission(wszLocation,
                                             FALSE,
                                             dwDemand);
    }
    EX_CATCH_HRESULT(hr);
    return hr;

}

/* static */
void AssemblySpec::DemandFileIOPermission(LPCWSTR wszCodeBase,
                                          BOOL fHavePath,
                                          DWORD dwDemandFlag)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(wszCodeBase));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    GCX_COOP();
        
    MethodDescCallSite demandPermission(METHOD__ASSEMBLY__DEMAND_PERMISSION);

    STRINGREF codeBase = NULL;
    GCPROTECT_BEGIN(codeBase);
            
    codeBase = StringObject::NewString(wszCodeBase);
    ARG_SLOT args[3] = 
    {
        ObjToArgSlot(codeBase),
        BoolToArgSlot(fHavePath),
        dwDemandFlag
    };
    demandPermission.Call(args);
    GCPROTECT_END();
}

BOOL AssemblySpec::FindAssemblyFile(AppDomain* pAppDomain, BOOL fThrowOnFileNotFound,
                                    IAssembly** ppIAssembly, IHostAssembly **ppIHostAssembly, IBindResult** ppNativeFusionAssembly,
                                    IFusionBindLog** ppFusionLog, HRESULT *pHRBindResult, StackCrawlMark *pCallerStackMark /* = NULL */,
                                    AssemblyLoadSecurity *pLoadSecurity /* = NULL */)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pAppDomain));
        PRECONDITION(CheckPointer(pHRBindResult));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    GCX_PREEMP();

    IApplicationContext *pFusionContext = pAppDomain->GetFusionContext();

    AssemblySink* pSink = pAppDomain->AllocateAssemblySink(this);
    SafeComHolderPreemp<IAssemblyBindSink> sinkholder(pSink);

    BOOL fSuppressSecurityChecks = pLoadSecurity != NULL && pLoadSecurity->m_fSuppressSecurityChecks;

    if (!GetCodeBase() && !fSuppressSecurityChecks)
        pSink->RequireCodebaseSecurityCheck();

    BOOL fIsWellKnown = FALSE;
    HRESULT hr = S_OK;

    IfFailGo(AssemblySpec::LoadAssembly(pFusionContext,
                                    pSink,
                                    ppIAssembly,
                                    ppIHostAssembly,
                                    ppNativeFusionAssembly,
                                    IsIntrospectionOnly(),
                                    fSuppressSecurityChecks));

    // Host should have already done appropriate permission demand
    if (!(*ppIHostAssembly)) {
        DWORD dwLocation;
        IfFailGo((*ppIAssembly)->GetAssemblyLocation(&dwLocation));

        fIsWellKnown = (dwLocation == ASMLOC_UNKNOWN);

        // check if it was cached, where a codebase had originally loaded it
        if (pSink->DoCodebaseSecurityCheck() &&
            !fSuppressSecurityChecks &&
            (dwLocation & ASMLOC_CODEBASE_HINT)) {
            if ((dwLocation & ASMLOC_LOCATION_MASK) == ASMLOC_DOWNLOAD_CACHE) {
                StackSString codeBase;
                SafeComHolderPreemp<IAssemblyName> pNameDef;
                
                // <TODO>We could be caching the IAssemblyName and codebase</TODO>
                IfFailGo((*ppIAssembly)->GetAssemblyNameDef(&pNameDef));

                FusionBind::GetAssemblyNameStringProperty(pNameDef, ASM_NAME_CODEBASE_URL, codeBase);

                DemandFileIOPermission(codeBase, FALSE, FILE_WEBPERM);
            }
            else if ((dwLocation & ASMLOC_LOCATION_MASK) != ASMLOC_GAC) {
                StackSString path;
                FusionBind::GetAssemblyManifestModulePath((*ppIAssembly), path);
                
                DemandFileIOPermission(path, TRUE, FILE_READANDPATHDISC);
            }
        }

        // Verify control hash
        if (m_HashForControl.GetSize() > 0) {
            StackSString path;
            
            FusionBind::GetAssemblyManifestModulePath((*ppIAssembly), path);
            
            AssemblyFileHash fileHash;
            IfFailGo(fileHash.SetFileName(path));
            IfFailGo(fileHash.CalculateHash(m_dwHashAlg));
            
            if (!m_HashForControl.Equals(fileHash.GetHash(), fileHash.GetHashSize()))
                IfFailGo(FUSION_E_REF_DEF_MISMATCH);
        }
    }

#ifdef MDA_SUPPORTED
    MdaLoadFromContext* pProbe = MDA_GET_ASSISTANT(LoadFromContext);
    if (pProbe) {
        pProbe->NowLoading(ppIAssembly, pCallerStackMark);
    }
#endif

    *ppFusionLog = pSink->m_pFusionLog;
    if (*ppFusionLog)
        (*ppFusionLog)->AddRef();
    return fIsWellKnown;

 ErrExit:
    {
        
        *pHRBindResult = hr;

        if (fThrowOnFileNotFound || (!Assembly::FileNotFound(hr)))
            EEFileLoadException::Throw(this, pSink->m_pFusionLog, hr);
    }

    return FALSE;
}
#endif // FEATURE_FUSION

void AssemblySpec::MatchRetargetedPublicKeys(Assembly *pAssembly)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pAssembly));
    }
    CONTRACTL_END;
#ifdef FEATURE_FUSION
    GCX_PREEMP();

    // Manually apply fusion policy to obtain retargeted public key
    SafeComHolderPreemp<IAssemblyName> pRequestedAssemblyName(NULL);
    SafeComHolderPreemp<IAssemblyName> pPostPolicyAssemblyName(NULL);
    IfFailThrow(CreateFusionName(&pRequestedAssemblyName));
    HRESULT hr = PreBindAssembly(GetAppDomain()->GetFusionContext(),
                                 pRequestedAssemblyName,
                                 NULL, // pAsmParent
                                 &pPostPolicyAssemblyName,
                                 NULL  // pvReserved
                                 );
    if (SUCCEEDED(hr)
        || (FAILED(hr) && (hr == FUSION_E_REF_DEF_MISMATCH))) {
        IAssemblyName *pResultAssemblyName = pAssembly->GetFusionAssemblyName();
        if (pResultAssemblyName
            && pPostPolicyAssemblyName
            && pResultAssemblyName->IsEqual(pPostPolicyAssemblyName, ASM_CMPF_PUBLIC_KEY_TOKEN) == S_OK)
            return;
    }
#endif // FEATURE_FUSION
    ThrowHR(FUSION_E_REF_DEF_MISMATCH);
}


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
    if (IsStrongNamed()) {

        const void *pbPublicKey;
        DWORD cbPublicKey;
        pbPublicKey = pAssembly->GetPublicKey(&cbPublicKey);
        if (cbPublicKey == 0)
            ThrowHR(FUSION_E_PRIVATE_ASM_DISALLOWED);

        if (m_dwFlags & afPublicKey) {
            if ((m_cbPublicKeyOrToken != cbPublicKey) ||
                memcmp(m_pbPublicKeyOrToken, pbPublicKey, m_cbPublicKeyOrToken))
                return MatchRetargetedPublicKeys(pAssembly);
        }

        // Ref has a token
        else {
            StrongNameBufferHolder<BYTE> pbStrongNameToken;
            DWORD cbStrongNameToken;

            if (!StrongNameTokenFromPublicKey((BYTE*) pbPublicKey,
                                              cbPublicKey,
                                              &pbStrongNameToken,
                                              &cbStrongNameToken))
                ThrowHR(StrongNameErrorInfo());
            if ((m_cbPublicKeyOrToken != cbStrongNameToken) ||
                memcmp(m_pbPublicKeyOrToken,
                       pbStrongNameToken,
                       cbStrongNameToken)) {
                return MatchRetargetedPublicKeys(pAssembly);
            }
        }
    }
}


PEAssembly *AssemblySpec::ResolveAssemblyFile(AppDomain *pDomain, BOOL fPreBind)
{
    CONTRACT(PEAssembly *)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    // No assembly resolve on codebase binds
    if (GetName() == NULL)
        RETURN NULL;

    Assembly *pAssembly = pDomain->RaiseAssemblyResolveEvent(this, IsIntrospectionOnly(), fPreBind);

    if (pAssembly != NULL) {
#ifdef FEATURE_FUSION
        if (!IsIntrospectionOnly() && IsLoggingNeeded()) {
            BinderLogging::BindingLog::CacheResultOfAssemblyResolveEvent(pDomain->GetFusionContext(), GetParentLoadContext(), pAssembly);
        }
#endif
        PEAssembly *pFile = pAssembly->GetManifestFile();
        pFile->AddRef();

        RETURN pFile;
    }

    RETURN NULL;
}


Assembly *AssemblySpec::LoadAssembly(FileLoadLevel targetLevel, AssemblyLoadSecurity *pLoadSecurity, BOOL fThrowOnFileNotFound, BOOL fRaisePrebindEvents, StackCrawlMark *pCallerStackMark)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
 
    DomainAssembly * pDomainAssembly = LoadDomainAssembly(targetLevel, pLoadSecurity, fThrowOnFileNotFound, fRaisePrebindEvents, pCallerStackMark);
    if (pDomainAssembly == NULL) {
        _ASSERTE(!fThrowOnFileNotFound);
        return NULL;
    }
    return pDomainAssembly->GetAssembly();
}

#if defined(FEATURE_CORECLR)
// Returns a BOOL indicating if the two Binder references point to the same
// binder instance.
BOOL AreSameBinderInstance(ICLRPrivBinder *pBinderA, ICLRPrivBinder *pBinderB)
{
    LIMITED_METHOD_CONTRACT;
    
    BOOL fIsSameInstance = FALSE;
    
    if ((pBinderA != NULL) && (pBinderB != NULL))
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
#endif // defined(FEATURE_CORECLR)

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
        
#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)
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
                pParentAssemblyBinder = static_cast<ICLRPrivBinder*>(pDomain->GetFusionContext());
            }
        }
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)
    }
       
#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

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
            // then such an assembly will not be found by the binder. In such a case, we reset our binder reference.
            pParentAssemblyBinder = NULL;
        }
    }
#endif // defined(FEATURE_COMINTEROP)
    
    if (!pParentAssemblyBinder)
    {
        // We can be here when loading assemblies via the host (e.g. ICLRRuntimeHost2::ExecuteAssembly) or when attempting
        // to load assemblies via custom AssemblyLoadContext implementation. 
        //
        // In such a case, the parent assembly (semantically) is mscorlib and thus, the default binding context should be 
        // used as the parent assembly binder.
        pParentAssemblyBinder = static_cast<ICLRPrivBinder*>(pDomain->GetFusionContext());
    }
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)
    
    return pParentAssemblyBinder;
}

DomainAssembly *AssemblySpec::LoadDomainAssembly(FileLoadLevel targetLevel,
                                                 AssemblyLoadSecurity *pLoadSecurity,
                                                 BOOL fThrowOnFileNotFound,
                                                 BOOL fRaisePrebindEvents,
                                                 StackCrawlMark *pCallerStackMark)
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

#ifndef FEATURE_CORECLR
    // Event Tracing for Windows is used to log data for performance and functional testing purposes.
    // The events in this function are used to help measure the performance of assembly loading as a whole for dynamic loads.

    // Special-purpose holder structure to ensure the LoaderPhaseEnd ETW event is fired when returning from function.
    struct ETWLoaderPhaseHolder
    {
        StackSString ETWCodeBase, ETWAssemblyName;

        DWORD _dwAppDomainId;
        BOOL initialized;

        ETWLoaderPhaseHolder()
            : _dwAppDomainId(ETWAppDomainIdNotAvailable)
            , initialized(FALSE)
        { }

        void Init(DWORD dwAppDomainId, LPCWSTR wszCodeBase, LPCSTR szAssemblyName)
        {
            _dwAppDomainId = dwAppDomainId;

            EX_TRY
            {
                if (wszCodeBase != NULL)
                {
                    ETWCodeBase.Append(wszCodeBase);
                    ETWCodeBase.Normalize(); // Ensures that the later cast to LPCWSTR does not throw.
                }
            }
            EX_CATCH
            {
                ETWCodeBase.Clear();
            }
            EX_END_CATCH(RethrowTransientExceptions)            

            EX_TRY
            {
                if (szAssemblyName != NULL)
                {
                    ETWAssemblyName.AppendUTF8(szAssemblyName);
                    ETWAssemblyName.Normalize(); // Ensures that the later cast to LPCWSTR does not throw.
                }
            }
            EX_CATCH
            {
                ETWAssemblyName.Clear();
            }
            EX_END_CATCH(RethrowTransientExceptions)            

            FireEtwLoaderPhaseStart(_dwAppDomainId, ETWLoadContextNotAvailable, ETWFieldUnused, ETWLoaderDynamicLoad, ETWCodeBase.IsEmpty() ? NULL : (LPCWSTR)ETWCodeBase, ETWAssemblyName.IsEmpty() ? NULL : (LPCWSTR)ETWAssemblyName, GetClrInstanceId());

            initialized = TRUE;
        }

        ~ETWLoaderPhaseHolder()
        {
            if (initialized)
            {
                FireEtwLoaderPhaseEnd(_dwAppDomainId, ETWLoadContextNotAvailable, ETWFieldUnused, ETWLoaderDynamicLoad, ETWCodeBase.IsEmpty() ? NULL : (LPCWSTR)ETWCodeBase, ETWAssemblyName.IsEmpty() ? NULL : (LPCWSTR)ETWAssemblyName, GetClrInstanceId());
            }
        }
    };

    ETWLoaderPhaseHolder loaderPhaseHolder;
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_PRIVATEBINDING_KEYWORD)) {
#ifdef  FEATURE_FUSION
        loaderPhaseHolder.Init(pDomain->GetId().m_dwId, m_wszCodeBase, m_pAssemblyName);
#else
        loaderPhaseHolder.Init(pDomain->GetId().m_dwId, NULL, NULL);
#endif    
    }
#endif // FEATURE_CORECLR

    DomainAssembly *pAssembly = nullptr;

#ifdef FEATURE_HOSTED_BINDER
    ICLRPrivBinder * pBinder = GetHostBinder();
    
    // If no binder was explicitly set, check if parent assembly has a binder.
    if (pBinder == nullptr)
    {
        pBinder = GetBindingContextFromParentAssembly(pDomain);
    }

#ifdef FEATURE_APPX_BINDER
    // If no explicit or parent binder, check domain.
    if (pBinder == nullptr && AppX::IsAppXProcess())
    {
        pBinder = pDomain->GetCurrentLoadContextHostBinder();
    }
#endif

    if (pBinder != nullptr)
    {
        ReleaseHolder<ICLRPrivAssembly> pPrivAssembly;
        HRESULT hrCachedResult;
        if (SUCCEEDED(pBinder->FindAssemblyBySpec(GetAppDomain(), this, &hrCachedResult, &pPrivAssembly)) &&
            SUCCEEDED(hrCachedResult))
        {
            pAssembly = pDomain->FindAssembly(pPrivAssembly);
        }
    }
#endif
    if ((pAssembly == nullptr) && CanUseWithBindingCache())
    {
        pAssembly = pDomain->FindCachedAssembly(this);
    }

    if (pAssembly)
    {
        pDomain->LoadDomainFile(pAssembly, targetLevel);
        RETURN pAssembly;
    }

#ifdef FEATURE_REFLECTION_ONLY_LOAD
    if (IsIntrospectionOnly() && (GetCodeBase() == NULL))
    {
        SafeComHolder<IAssemblyName> pIAssemblyName;
        IfFailThrow(CreateFusionName(&pIAssemblyName));

        // Note: We do not support introspection-only collectible assemblies (yet)
        AppDomain::AssemblyIterator i = pDomain->IterateAssembliesEx(
            (AssemblyIterationFlags)(kIncludeLoaded | kIncludeIntrospection | kExcludeCollectible));
        CollectibleAssemblyHolder<DomainAssembly *> pCachedDomainAssembly;

        while (i.Next(pCachedDomainAssembly.This()))
        {
            _ASSERTE(!pCachedDomainAssembly->IsCollectible());
            IAssemblyName * pCachedAssemblyName = pCachedDomainAssembly->GetAssembly()->GetFusionAssemblyName();
            if (S_OK == pCachedAssemblyName->IsEqual(pIAssemblyName, ASM_CMPF_IL_ALL))
            {
                RETURN pCachedDomainAssembly;
            }
        }
    }
#endif // FEATURE_REFLECTION_ONLY_LOAD

    PEAssemblyHolder pFile(pDomain->BindAssemblySpec(this, fThrowOnFileNotFound, fRaisePrebindEvents, pCallerStackMark, pLoadSecurity));
    if (pFile == NULL)
        RETURN NULL;

    pAssembly = pDomain->LoadDomainAssembly(this, pFile, targetLevel, pLoadSecurity);

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

#ifndef  FEATURE_FUSION  
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
#endif //FEATURE_FUSION

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
        PRECONDITION(HasUniqueIdentity() || AppDomain::GetCurrentDomain()->IsCompilationDomain());
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
            if (!StrongNameTokenFromPublicKey(m_pbPublicKeyOrToken,
                                              m_cbPublicKeyOrToken,
                                              &pbPublicKeyToken,
                                              &cbPublicKeyToken)) {
                IfFailGo(StrongNameErrorInfo());
            }

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

void AssemblySpecBindingCache::OnAppDomainUnload()
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
        b->OnAppDomainUnload();

        ++i;
    }
}

void AssemblySpecBindingCache::Init(CrstBase *pCrst, LoaderHeap *pHeap)
{
    WRAPPER_NO_CONTRACT;

    LockOwner lock = {pCrst, IsOwnerOfCrst};
    m_map.Init(INITIAL_ASM_SPEC_HASH_SIZE, CompareSpecs, TRUE, &lock);
    m_pHeap = pHeap;
}

#if defined(FEATURE_CORECLR)
AssemblySpecBindingCache::AssemblyBinding* AssemblySpecBindingCache::GetAssemblyBindingEntryForAssemblySpec(AssemblySpec* pSpec, BOOL fThrow)
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

    AssemblyBinding* pEntry = (AssemblyBinding *) INVALIDENTRY;
    UPTR key = (UPTR)pSpec->Hash();
    
    // On CoreCLR, we will use the BinderID as the key 
    ICLRPrivBinder *pBinderContextForLookup = NULL;
    AppDomain *pSpecDomain = pSpec->GetAppDomain();
    bool fGetBindingContextFromParent = true;
    
    // Check if the AssemblySpec already has specified its binding context. This will be set for assemblies that are
    // attempted to be explicitly bound using AssemblyLoadContext LoadFrom* methods.
    pBinderContextForLookup = pSpec->GetBindingContext();
    if (pBinderContextForLookup != NULL)
    {
        // We are working with the actual binding context in which the assembly was expected to be loaded.
        // Thus, we dont need to get it from the parent assembly.
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

    UPTR lookupKey = key;
    if (pBinderContextForLookup)
    {
        UINT_PTR binderID = 0;
        HRESULT hr = pBinderContextForLookup->GetBinderID(&binderID);
        _ASSERTE(SUCCEEDED(hr));
        lookupKey = key^binderID;
    }
    
    pEntry = (AssemblyBinding *) m_map.LookupValue(lookupKey, pSpec);
    if (pEntry == (AssemblyBinding *) INVALIDENTRY)
    {
        // We didnt find the AssemblyBinding entry against the binder of the parent assembly.
        // It is possible that the AssemblySpec corresponds to a TPA assembly, so try the lookup
        // against the TPABinder context.
        ICLRPrivBinder* pTPABinderContext = pSpecDomain->GetTPABinderContext();
        if ((pTPABinderContext != NULL) && !AreSameBinderInstance(pTPABinderContext, pBinderContextForLookup))
        {
            UINT_PTR tpaBinderID = 0;
            HRESULT hr = pTPABinderContext->GetBinderID(&tpaBinderID);
            _ASSERTE(SUCCEEDED(hr));
            lookupKey = key^tpaBinderID;
            
            // Set the binding context in AssemblySpec to be TPABinder
            // as that will be used in the Lookup operation below.
            if (fGetBindingContextFromParent)
            {
                pSpec->SetBindingContext(pTPABinderContext);
            }
            
            pEntry = (AssemblyBinding *) m_map.LookupValue(lookupKey, pSpec);
        }
    }
    
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
#endif // defined(FEATURE_CORECLR)

BOOL AssemblySpecBindingCache::Contains(AssemblySpec *pSpec)
{
    WRAPPER_NO_CONTRACT;

#if !defined(FEATURE_CORECLR)
    DWORD key = pSpec->Hash();
    AssemblyBinding *entry = (AssemblyBinding *) m_map.LookupValue(key, pSpec);
    return (entry != (AssemblyBinding *) INVALIDENTRY);
#else // defined(FEATURE_CORECLR)    
    return (GetAssemblyBindingEntryForAssemblySpec(pSpec, TRUE) != (AssemblyBinding *) INVALIDENTRY);
#endif // !defined(FEATURE_CORECLR)    
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
    
#if !defined(FEATURE_CORECLR)    
    DWORD key = pSpec->Hash();
    entry = (AssemblyBinding *) m_map.LookupValue(key, pSpec);
#else // defined(FEATURE_CORECLR)
    entry = GetAssemblyBindingEntryForAssemblySpec(pSpec, fThrow);
#endif // !defined(FEATURE_CORECLR)

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
    
#if !defined(FEATURE_CORECLR)    
    DWORD key = pSpec->Hash();
    entry = (AssemblyBinding *) m_map.LookupValue(key, pSpec);
#else // defined(FEATURE_CORECLR)
    entry = GetAssemblyBindingEntryForAssemblySpec(pSpec, fThrow);
#endif // !defined(FEATURE_CORECLR)
    
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
#ifdef FEATURE_HOSTED_BINDER
        // Host binder based assembly spec's cannot currently be safely inserted into caches.
        PRECONDITION(pSpec->GetHostBinder() == nullptr);
#endif // FEATURE_HOSTED_BINDER
        POSTCONDITION(UnsafeContains(this, pSpec));
        POSTCONDITION(UnsafeVerifyLookupAssembly(this, pSpec, pAssembly));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    UPTR key = (UPTR)pSpec->Hash();

#if defined(FEATURE_CORECLR)
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
#endif // defined(FEATURE_CORECLR)
    
    AssemblyBinding *entry = (AssemblyBinding *) m_map.LookupValue(key, pSpec);

    if (entry == (AssemblyBinding *) INVALIDENTRY)
    {
        AssemblyBindingHolder abHolder;
        entry = abHolder.CreateAssemblyBinding(m_pHeap);

        entry->Init(pSpec,pAssembly->GetFile(),pAssembly,NULL,m_pHeap, abHolder.GetPamTracker());

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
#ifdef FEATURE_HOSTED_BINDER
        // Host binder based assembly spec's cannot currently be safely inserted into caches.
        PRECONDITION(pSpec->GetHostBinder() == nullptr);
#endif // FEATURE_HOSTED_BINDER
        POSTCONDITION((!RETVAL) || (UnsafeContains(this, pSpec) && UnsafeVerifyLookupFile(this, pSpec, pFile)));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    UPTR key = (UPTR)pSpec->Hash();

#if defined(FEATURE_CORECLR)
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
#endif // defined(FEATURE_CORECLR)

    AssemblyBinding *entry = (AssemblyBinding *) m_map.LookupValue(key, pSpec);

    if (entry == (AssemblyBinding *) INVALIDENTRY)
    {
        AssemblyBindingHolder abHolder;
        entry = abHolder.CreateAssemblyBinding(m_pHeap);

        entry->Init(pSpec,pFile,NULL,NULL,m_pHeap, abHolder.GetPamTracker());

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
#ifdef FEATURE_HOSTED_BINDER
        // Host binder based assembly spec's cannot currently be safely inserted into caches.
        PRECONDITION(pSpec->GetHostBinder() == nullptr);
#endif // FEATURE_HOSTED_BINDER
        DISABLED(POSTCONDITION(UnsafeContains(this, pSpec))); //<TODO>@todo: Getting violations here - StoreExceptions could happen anywhere so this is possibly too aggressive.</TODO>
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    UPTR key = (UPTR)pSpec->Hash();

#if !defined(FEATURE_CORECLR)    
    AssemblyBinding *entry = (AssemblyBinding *) m_map.LookupValue(key, pSpec);
#else // defined(FEATURE_CORECLR)
    AssemblyBinding *entry = GetAssemblyBindingEntryForAssemblySpec(pSpec, TRUE);
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
#endif // defined(FEATURE_CORECLR)

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

#if defined(FEATURE_HOSTED_BINDER) && defined(FEATURE_APPX_BINDER)
    _ASSERTE(a1->GetAppDomain() == a2->GetAppDomain());
    if (a1->GetAppDomain()->HasLoadContextHostBinder())
        return (CLRPrivBinderUtil::CompareHostBinderSpecs(a1,a2));
#endif

    if ((!a1->CompareEx(a2)) ||
        (a1->IsIntrospectionOnly() != a2->IsIntrospectionOnly()))
        return FALSE;
    return TRUE;  
}



/* static */
BOOL DomainAssemblyCache::CompareBindingSpec(UPTR spec1, UPTR spec2)
{
    WRAPPER_NO_CONTRACT;

    AssemblySpec* pSpec1 = (AssemblySpec*) (spec1 << 1);
    AssemblyEntry* pEntry2 = (AssemblyEntry*) spec2;

#if defined(FEATURE_HOSTED_BINDER) && defined(FEATURE_FUSION)
    AssemblySpec* pSpec2 = &pEntry2->spec;
    _ASSERTE(pSpec1->GetAppDomain() == pSpec2->GetAppDomain());
    if (pSpec1->GetAppDomain()->HasLoadContextHostBinder())
        return (CLRPrivBinderUtil::CompareHostBinderSpecs(pSpec1,pSpec2));
#endif


    if ((!pSpec1->CompareEx(&pEntry2->spec)) ||
        (pSpec1->IsIntrospectionOnly() != pEntry2->spec.IsIntrospectionOnly()))
        return FALSE;

    return TRUE;
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

#ifdef FEATURE_FUSION

IAssembly * AssemblySpec::GetParentIAssembly()
{
    LIMITED_METHOD_CONTRACT;
    if(m_pParentAssembly)
        return m_pParentAssembly->GetFile()->GetFusionAssembly();

    return NULL;
}

LPCVOID AssemblySpec::GetParentAssemblyPtr()
{
    LIMITED_METHOD_CONTRACT;
    if(m_pParentAssembly)
    {
#ifdef FEATURE_HOSTED_BINDER
        if (m_pParentAssembly->GetFile()->HasHostAssembly())
            return m_pParentAssembly->GetFile()->GetHostAssembly();
        else
#endif
            return m_pParentAssembly->GetFile()->GetFusionAssembly();
    }
    return NULL;
}

#endif //FEATURE_FUSION



DomainAssembly * AssemblySpec::GetParentAssembly()
{
    LIMITED_METHOD_CONTRACT;
    return m_pParentAssembly;
}

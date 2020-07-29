// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// AssemblyBinder.cpp
//


//
// Implements the AssemblyBinder class
//
// ============================================================

#include "assemblybinder.hpp"
#include "assemblyname.hpp"
#include "assembly.hpp"
#include "applicationcontext.hpp"
#include "bindertracing.h"
#include "loadcontext.hpp"
#include "bindresult.inl"
#include "failurecache.hpp"
#include "utils.hpp"
#include "variables.hpp"
#include "stringarraylist.h"

#define APP_DOMAIN_LOCKED_UNLOCKED        0x02
#define APP_DOMAIN_LOCKED_CONTEXT         0x04

#ifndef IMAGE_FILE_MACHINE_ARM64
#define IMAGE_FILE_MACHINE_ARM64             0xAA64  // ARM64 Little-Endian
#endif

BOOL IsCompilationProcess();

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
#include "clrprivbindercoreclr.h"
#include "clrprivbinderassemblyloadcontext.h"
// Helper function in the VM, invoked by the Binder, to invoke the host assembly resolver
extern HRESULT RuntimeInvokeHostAssemblyResolver(INT_PTR pManagedAssemblyLoadContextToBindWithin,
                                                IAssemblyName *pIAssemblyName, CLRPrivBinderCoreCLR *pTPABinder,
                                                BINDER_SPACE::AssemblyName *pAssemblyName, ICLRPrivAssembly **ppLoadedAssembly);

#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

namespace BINDER_SPACE
{
    namespace
    {
        //
        // This defines the assembly equivalence relation
        //
        bool IsCompatibleAssemblyVersion(/* in */ AssemblyName *pRequestedName,
                                         /* in */ AssemblyName *pFoundName)
        {
            AssemblyVersion *pRequestedVersion = pRequestedName->GetVersion();
            AssemblyVersion *pFoundVersion = pFoundName->GetVersion();

            if (!pRequestedVersion->HasMajor())
            {
                // An unspecified requested version component matches any value for the same component in the found version,
                // regardless of lesser-order version components
                return true;
            }
            if (!pFoundVersion->HasMajor() || pRequestedVersion->GetMajor() > pFoundVersion->GetMajor())
            {
                // - A specific requested version component does not match an unspecified value for the same component in
                //   the found version, regardless of lesser-order version components
                // - Or, the requested version is greater than the found version
                return false;
            }
            if (pRequestedVersion->GetMajor() < pFoundVersion->GetMajor())
            {
                // The requested version is less than the found version
                return true;
            }

            if (!pRequestedVersion->HasMinor())
            {
                return true;
            }
            if (!pFoundVersion->HasMinor() || pRequestedVersion->GetMinor() > pFoundVersion->GetMinor())
            {
                return false;
            }
            if (pRequestedVersion->GetMinor() < pFoundVersion->GetMinor())
            {
                return true;
            }

            if (!pRequestedVersion->HasBuild())
            {
                return true;
            }
            if (!pFoundVersion->HasBuild() || pRequestedVersion->GetBuild() > pFoundVersion->GetBuild())
            {
                return false;
            }
            if (pRequestedVersion->GetBuild() < pFoundVersion->GetBuild())
            {
                return true;
            }

            if (!pRequestedVersion->HasRevision())
            {
                return true;
            }
            if (!pFoundVersion->HasRevision() || pRequestedVersion->GetRevision() > pFoundVersion->GetRevision())
            {
                return false;
            }
            return true;
        }

        HRESULT URLToFullPath(PathString &assemblyPath)
        {
            HRESULT hr = S_OK;

            SString::Iterator pos = assemblyPath.Begin();
            if (assemblyPath.MatchCaseInsensitive(pos, g_BinderVariables->httpURLPrefix))
            {
                // HTTP downloads are unsupported
                hr = FUSION_E_CODE_DOWNLOAD_DISABLED;
            }
            else
            {
                SString fullAssemblyPath;
                WCHAR *pwzFullAssemblyPath = fullAssemblyPath.OpenUnicodeBuffer(MAX_LONGPATH);
                DWORD dwCCFullAssemblyPath = MAX_LONGPATH + 1; // SString allocates extra byte for null.

                MutateUrlToPath(assemblyPath);

                dwCCFullAssemblyPath = WszGetFullPathName(assemblyPath.GetUnicode(),
                                                          dwCCFullAssemblyPath,
                                                          pwzFullAssemblyPath,
                                                          NULL);
                if (dwCCFullAssemblyPath > MAX_LONGPATH)
                {
                    fullAssemblyPath.CloseBuffer();
                    pwzFullAssemblyPath = fullAssemblyPath.OpenUnicodeBuffer(dwCCFullAssemblyPath - 1);
                    dwCCFullAssemblyPath = WszGetFullPathName(assemblyPath.GetUnicode(),
                                                              dwCCFullAssemblyPath,
                                                              pwzFullAssemblyPath,
                                                              NULL);
                }
                fullAssemblyPath.CloseBuffer(dwCCFullAssemblyPath);

                if (dwCCFullAssemblyPath == 0)
                {
                    hr = HRESULT_FROM_GetLastError();
                }
                else
                {
                    assemblyPath.Set(fullAssemblyPath);
                }
            }

            return hr;
        }

#ifndef CROSSGEN_COMPILE
        HRESULT CreateImageAssembly(IMDInternalImport       *pIMetaDataAssemblyImport,
                                    PEKIND                   PeKind,
                                    PEImage                 *pPEImage,
                                    PEImage                 *pNativePEImage,
                                    BindResult              *pBindResult)
        {
            HRESULT hr = S_OK;
            ReleaseHolder<Assembly> pAssembly;
            PathString asesmblyPath;

            SAFE_NEW(pAssembly, Assembly);
            IF_FAIL_GO(pAssembly->Init(pIMetaDataAssemblyImport,
                                       PeKind,
                                       pPEImage,
                                       pNativePEImage,
                                       asesmblyPath,
                                       FALSE /* fIsInGAC */));

            pBindResult->SetResult(pAssembly);
            pBindResult->SetIsFirstRequest(TRUE);

        Exit:
            return hr;
        }
#endif // !CROSSGEN_COMPILE
    };

    /* static */
    HRESULT AssemblyBinder::Startup()
    {
        STATIC_CONTRACT_NOTHROW;

        HRESULT hr = S_OK;

        // This should only be called once
        _ASSERTE(g_BinderVariables == NULL);
        g_BinderVariables = new Variables();
        IF_FAIL_GO(g_BinderVariables->Init());

    Exit:
        return hr;
    }


    HRESULT AssemblyBinder::TranslatePEToArchitectureType(DWORD  *pdwPAFlags, PEKIND *PeKind)
    {
        HRESULT hr = S_OK;

        _ASSERTE(pdwPAFlags != NULL);
        _ASSERTE(PeKind != NULL);

        CorPEKind CLRPeKind = (CorPEKind) pdwPAFlags[0];
        DWORD dwImageType = pdwPAFlags[1];

        *PeKind = peNone;

        if(CLRPeKind == peNot)
        {
            // Not a PE. Shouldn't ever get here.
            IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_BAD_FORMAT));
        }
        else
        {
            if ((CLRPeKind & peILonly) && !(CLRPeKind & pe32Plus) &&
                !(CLRPeKind & pe32BitRequired) && dwImageType == IMAGE_FILE_MACHINE_I386)
            {
                // Processor-agnostic (MSIL)
                *PeKind = peMSIL;
            }
            else if (CLRPeKind & pe32Plus)
            {
                // 64-bit
                if (CLRPeKind & pe32BitRequired)
                {
                    // Invalid
                    IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_BAD_FORMAT));
                }

                // Regardless of whether ILONLY is set or not, the architecture
                // is the machine type.
                if(dwImageType == IMAGE_FILE_MACHINE_ARM64)
                    *PeKind = peARM64;
                else if (dwImageType == IMAGE_FILE_MACHINE_AMD64)
                    *PeKind = peAMD64;
                else
                {
                    // We don't support other architectures
                    IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_BAD_FORMAT));
                }
            }
            else
            {
                // 32-bit, non-agnostic
                if(dwImageType == IMAGE_FILE_MACHINE_I386)
                    *PeKind = peI386;
                else if(dwImageType == IMAGE_FILE_MACHINE_ARMNT)
                    *PeKind = peARM;
                else
                {
                    // Not supported
                    IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_BAD_FORMAT));
                }
            }
        }

    Exit:
        return hr;
    }

    // See code:BINDER_SPACE::AssemblyBinder::GetAssembly for info on fNgenExplicitBind
    // and fExplicitBindToNativeImage, and see code:CEECompileInfo::LoadAssemblyByPath
    // for an example of how they're used.
    HRESULT AssemblyBinder::BindAssembly(/* in */  ApplicationContext  *pApplicationContext,
                                         /* in */  AssemblyName        *pAssemblyName,
                                         /* in */  LPCWSTR              szCodeBase,
                                         /* in */  PEAssembly          *pParentAssembly,
                                         /* in */  BOOL                 fNgenExplicitBind,
                                         /* in */  BOOL                 fExplicitBindToNativeImage,
                                         /* in */  bool                 excludeAppPaths,
                                         /* out */ Assembly           **ppAssembly)
    {
        HRESULT hr = S_OK;
        LONG kContextVersion = 0;
        BindResult bindResult;

        // Tracing happens outside the binder lock to avoid calling into managed code within the lock
        BinderTracing::ResolutionAttemptedOperation tracer{pAssemblyName, pApplicationContext->GetBinderID(), 0 /*managedALC*/, hr};

#ifndef CROSSGEN_COMPILE
    Retry:
        {
            // Lock the binding application context
            CRITSEC_Holder contextLock(pApplicationContext->GetCriticalSectionCookie());
#endif

            if (szCodeBase == NULL)
            {
                IF_FAIL_GO(BindByName(pApplicationContext,
                                      pAssemblyName,
                                      false, // skipFailureCaching
                                      false, // skipVersionCompatibilityCheck
                                      excludeAppPaths,
                                      &bindResult));
            }
            else
            {
                PathString assemblyPath(szCodeBase);

                // Convert URL to full path and block HTTP downloads
                IF_FAIL_GO(URLToFullPath(assemblyPath));
                BOOL fDoNgenExplicitBind = fNgenExplicitBind;

                // Only use explicit ngen binding in the new coreclr path-based binding model
                if (!pApplicationContext->IsTpaListProvided())
                {
                    fDoNgenExplicitBind = FALSE;
                }

                IF_FAIL_GO(BindWhereRef(pApplicationContext,
                                        assemblyPath,
                                        fDoNgenExplicitBind,
                                        fExplicitBindToNativeImage,
                                        excludeAppPaths,
                                        &bindResult));
            }

            // Remember the post-bind version
            kContextVersion = pApplicationContext->GetVersion();

#ifndef CROSSGEN_COMPILE
        } // lock(pApplicationContext)
#endif

    Exit:
        tracer.TraceBindResult(bindResult);

        if (bindResult.HaveResult())
        {
#ifndef CROSSGEN_COMPILE
            BindResult hostBindResult;

            hr = RegisterAndGetHostChosen(pApplicationContext,
                                          kContextVersion,
                                          &bindResult,
                                          &hostBindResult);

            if (hr == S_FALSE)
            {
                // Another bind interfered. We need to retry the entire bind.
                // This by design loops as long as needed because by construction we eventually
                // will succeed or fail the bind.
                bindResult.Reset();
                goto Retry;
            }
            else if (hr == S_OK)
            {
                *ppAssembly = hostBindResult.GetAsAssembly(TRUE /* fAddRef */);
            }
#else // CROSSGEN_COMPILE

            *ppAssembly = bindResult.GetAsAssembly(TRUE /* fAddRef */);

#endif // CROSSGEN_COMPILE
        }

        return hr;
    }

    /* static */
    HRESULT AssemblyBinder::BindToSystem(SString   &systemDirectory,
                                         Assembly **ppSystemAssembly,
                                         bool       fBindToNativeImage)
    {
        // Indirect check that binder was initialized.
        _ASSERTE(g_BinderVariables != NULL);

        HRESULT hr = S_OK;

        _ASSERTE(ppSystemAssembly != NULL);

        ReleaseHolder<Assembly> pSystemAssembly;

        // At run-time, System.Private.CoreLib.dll is expected to be the NI image.
        // System.Private.CoreLib.dll is expected to be found at one of the following locations:
        //   * Non-single-file app: In systemDirectory, beside coreclr.dll
        //   * Framework-dependent single-file app: In systemDirectory, beside coreclr.dll
        //   * Self-contained single-file app: Within the single-file bundle.
        //
        //   CoreLib path (sCoreLib):
        //   * Absolute path when looking for a file on disk
        //   * Bundle-relative path when looking within the single-file bundle.

        StackSString sCoreLibName(CoreLibName_IL_W);
        StackSString sCoreLib;
        BinderTracing::PathSource pathSource = BinderTracing::PathSource::Bundle;
        BundleFileLocation bundleFileLocation = Bundle::ProbeAppBundle(sCoreLibName, /*pathIsBundleRelative */ true);
        if (!bundleFileLocation.IsValid())
        {
            pathSource = BinderTracing::PathSource::ApplicationAssemblies;
        }
        sCoreLib.Set(systemDirectory);
        CombinePath(sCoreLib, sCoreLibName, sCoreLib);

        IF_FAIL_GO(AssemblyBinder::GetAssembly(sCoreLib,
                                               TRUE /* fIsInGAC */,
                                               fBindToNativeImage,
                                               &pSystemAssembly,
                                               NULL /* szMDAssemblyPath */,
                                               bundleFileLocation));
        BinderTracing::PathProbed(sCoreLib, pathSource, hr);

        *ppSystemAssembly = pSystemAssembly.Extract();

    Exit:
        return hr;
    }


    /* static */
    HRESULT AssemblyBinder::BindToSystemSatellite(SString& systemDirectory,
        SString& simpleName,
        SString& cultureName,
        Assembly** ppSystemAssembly)
    {
        // Indirect check that binder was initialized.
        _ASSERTE(g_BinderVariables != NULL);

        HRESULT hr = S_OK;

        _ASSERTE(ppSystemAssembly != NULL);

        // Satellite assembly's relative path
        StackSString relativePath;

        // append culture name
        if (!cultureName.IsEmpty())
        {
            CombinePath(relativePath, cultureName, relativePath);
        }

        // append satellite assembly's simple name
        CombinePath(relativePath, simpleName, relativePath);

        // append extension
        relativePath.Append(W(".dll"));

        // Satellite assembly's path:
        //   * Absolute path when looking for a file on disk
        //   * Bundle-relative path when looking within the single-file bundle.
        StackSString sCoreLibSatellite;

        BinderTracing::PathSource pathSource = BinderTracing::PathSource::Bundle;
        BundleFileLocation bundleFileLocation = Bundle::ProbeAppBundle(relativePath, /*pathIsBundleRelative */ true);
        if (!bundleFileLocation.IsValid())
        {
            sCoreLibSatellite.Set(systemDirectory);
            pathSource = BinderTracing::PathSource::ApplicationAssemblies;
        }
        CombinePath(sCoreLibSatellite, relativePath, sCoreLibSatellite);

        ReleaseHolder<Assembly> pSystemAssembly;
        IF_FAIL_GO(AssemblyBinder::GetAssembly(sCoreLibSatellite,
                                               TRUE /* fIsInGAC */,
                                               FALSE /* fExplicitBindToNativeImage */,
                                               &pSystemAssembly,
                                               NULL /* szMDAssemblyPath */,
                                               bundleFileLocation));
        BinderTracing::PathProbed(sCoreLibSatellite, pathSource, hr);

        *ppSystemAssembly = pSystemAssembly.Extract();

    Exit:
        return hr;
    }

    /* static */
    HRESULT AssemblyBinder::BindByName(ApplicationContext *pApplicationContext,
                                       AssemblyName       *pAssemblyName,
                                       bool                skipFailureCaching,
                                       bool                skipVersionCompatibilityCheck,
                                       bool                excludeAppPaths,
                                       BindResult         *pBindResult)
    {
        HRESULT hr = S_OK;
        PathString assemblyDisplayName;

        // Look for already cached binding failure (ignore PA, every PA will lock the context)
        pAssemblyName->GetDisplayName(assemblyDisplayName,
                                      AssemblyName::INCLUDE_VERSION);

        hr = pApplicationContext->GetFailureCache()->Lookup(assemblyDisplayName);
        if (FAILED(hr))
        {
            if ((hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND)) && skipFailureCaching)
            {
                // Ignore pre-existing transient bind error (re-bind will succeed)
                pApplicationContext->GetFailureCache()->Remove(assemblyDisplayName);
            }

            goto LogExit;
        }
        else if (hr == S_FALSE)
        {
            // workaround: Special case for byte arrays. Rerun the bind to create binding log.
            pAssemblyName->SetIsDefinition(TRUE);
            hr = S_OK;
        }

        if (!Assembly::IsValidArchitecture(pAssemblyName->GetArchitecture()))
        {
            // Assembly reference contains wrong architecture
            IF_FAIL_GO(FUSION_E_INVALID_NAME);
        }

        IF_FAIL_GO(BindLocked(pApplicationContext,
                              pAssemblyName,
                              skipVersionCompatibilityCheck,
                              excludeAppPaths,
                              pBindResult));

        if (!pBindResult->HaveResult())
        {
            // Behavior rules are clueless now
            IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND));
        }

    Exit:
        if (FAILED(hr))
        {
            if (skipFailureCaching)
            {
                if (hr != HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
                {
                    // Cache non-transient bind error for byte-array
                    hr = S_FALSE;
                }
                else
                {
                    // Ignore transient bind error (re-bind will succeed)
                    goto LogExit;
                }
            }

            hr = pApplicationContext->AddToFailureCache(assemblyDisplayName, hr);
        }

    LogExit:
        return hr;
    }

    /* static */
    // See code:BINDER_SPACE::AssemblyBinder::GetAssembly for info on fNgenExplicitBind
    // and fExplicitBindToNativeImage, and see code:CEECompileInfo::LoadAssemblyByPath
    // for an example of how they're used.
    HRESULT AssemblyBinder::BindWhereRef(ApplicationContext *pApplicationContext,
                                         PathString         &assemblyPath,
                                         BOOL                fNgenExplicitBind,
                                         BOOL                fExplicitBindToNativeImage,
                                         bool                excludeAppPaths,
                                         BindResult         *pBindResult)
    {
        HRESULT hr = S_OK;

        ReleaseHolder<Assembly> pAssembly;
        BindResult lockedBindResult;

        // Look for already cached binding failure
        hr = pApplicationContext->GetFailureCache()->Lookup(assemblyPath);
        if (FAILED(hr))
        {
            goto LogExit;
        }

        // If we return this assembly, then it is guaranteed to be not in GAC
        // Design decision. For now, keep the V2 model of Fusion being oblivious of the strong name.
        // Security team did not see any security concern with interpreting the version information.
        IF_FAIL_GO(GetAssembly(assemblyPath,
                               FALSE /* fIsInGAC */,

                               // Pass through caller's intent of whether to bind to the
                               // NI using an explicit path to the NI that was
                               // specified.  Generally only NGEN PDB generation has
                               // this TRUE.
                               fExplicitBindToNativeImage,
                               &pAssembly,
                               NULL /* szMDAssemblyPath */,
                               Bundle::ProbeAppBundle(assemblyPath)));

        AssemblyName *pAssemblyName;
        pAssemblyName = pAssembly->GetAssemblyName();

        if (!fNgenExplicitBind)
        {
            IF_FAIL_GO(BindLocked(pApplicationContext,
                                  pAssemblyName,
                                  false, // skipVersionCompatibilityCheck
                                  excludeAppPaths,
                                  &lockedBindResult));
            if (lockedBindResult.HaveResult())
            {
                pBindResult->SetResult(&lockedBindResult);
                GO_WITH_HRESULT(S_OK);
            }
        }

        hr = S_OK;
        pBindResult->SetResult(pAssembly);

    Exit:

        if (FAILED(hr))
        {
            // Always cache binding failures
            hr = pApplicationContext->AddToFailureCache(assemblyPath, hr);
        }

    LogExit:
        return hr;
    }

    /* static */
    HRESULT AssemblyBinder::BindLocked(ApplicationContext *pApplicationContext,
                                       AssemblyName       *pAssemblyName,
                                       bool                skipVersionCompatibilityCheck,
                                       bool                excludeAppPaths,
                                       BindResult         *pBindResult)
    {
        HRESULT hr = S_OK;

        bool isTpaListProvided = pApplicationContext->IsTpaListProvided();
#ifndef CROSSGEN_COMPILE
        ContextEntry *pContextEntry = NULL;
        hr = FindInExecutionContext(pApplicationContext, pAssemblyName, &pContextEntry);

        // Add the attempt to the bind result on failure / not found. On success, it will be added after the version check.
        if (FAILED(hr) || pContextEntry == NULL)
            pBindResult->SetAttemptResult(hr, pContextEntry);

        IF_FAIL_GO(hr);
        if (pContextEntry != NULL)
        {
            if (!skipVersionCompatibilityCheck)
            {
                // Can't give higher version than already bound
                bool isCompatible = IsCompatibleAssemblyVersion(pAssemblyName, pContextEntry->GetAssemblyName());
                hr = isCompatible ? S_OK : FUSION_E_APP_DOMAIN_LOCKED;
                pBindResult->SetAttemptResult(hr, pContextEntry);

                // TPA binder returns FUSION_E_REF_DEF_MISMATCH for incompatible version
                if (hr == FUSION_E_APP_DOMAIN_LOCKED && isTpaListProvided)
                    hr = FUSION_E_REF_DEF_MISMATCH;
            }
            else
            {
                pBindResult->SetAttemptResult(hr, pContextEntry);
            }

            IF_FAIL_GO(hr);

            pBindResult->SetResult(pContextEntry);
        }
        else
#endif // !CROSSGEN_COMPILE
        if (isTpaListProvided)
        {
            // BindByTpaList handles setting attempt results on the bind result
            hr = BindByTpaList(pApplicationContext,
                                     pAssemblyName,
                                     excludeAppPaths,
                                     pBindResult);
            if (SUCCEEDED(hr) && pBindResult->HaveResult())
            {
                bool isCompatible = IsCompatibleAssemblyVersion(pAssemblyName, pBindResult->GetAssemblyName());
                hr = isCompatible ? S_OK : FUSION_E_APP_DOMAIN_LOCKED;
                pBindResult->SetAttemptResult(hr, pBindResult->GetAsAssembly());

                // TPA binder returns FUSION_E_REF_DEF_MISMATCH for incompatible version
                if (hr == FUSION_E_APP_DOMAIN_LOCKED && isTpaListProvided)
                    hr = FUSION_E_REF_DEF_MISMATCH;
            }

            if (FAILED(hr))
            {
                pBindResult->SetNoResult();
            }
            IF_FAIL_GO(hr);
        }
    Exit:
        return hr;
    }

#ifndef CROSSGEN_COMPILE
    /* static */
    HRESULT AssemblyBinder::FindInExecutionContext(ApplicationContext  *pApplicationContext,
                                                   AssemblyName        *pAssemblyName,
                                                   ContextEntry       **ppContextEntry)
    {
        HRESULT hr = S_OK;

        _ASSERTE(pApplicationContext != NULL);
        _ASSERTE(pAssemblyName != NULL);
        _ASSERTE(ppContextEntry != NULL);

        ExecutionContext *pExecutionContext = pApplicationContext->GetExecutionContext();
        ContextEntry *pContextEntry = pExecutionContext->Lookup(pAssemblyName);

        // Set any found context entry. It is up to the caller to check the returned HRESULT
        // for errors due to validation
        *ppContextEntry = pContextEntry;
        if (pContextEntry != NULL)
        {
            AssemblyName *pContextName = pContextEntry->GetAssemblyName();
            if (pAssemblyName->GetIsDefinition() &&
                (pContextName->GetArchitecture() != pAssemblyName->GetArchitecture()))
            {
                return FUSION_E_APP_DOMAIN_LOCKED;
            }
        }

        return pContextEntry != NULL ? S_OK : S_FALSE;
    }

#endif //CROSSGEN_COMPILE

    //
    // Tests whether a candidate assembly's name matches the requested.
    // This does not do a version check.  The binder applies version policy
    // further up the stack once it gets a successful bind.
    //
    BOOL TestCandidateRefMatchesDef(AssemblyName *pRequestedAssemblyName,
                                    AssemblyName *pBoundAssemblyName,
                                    BOOL tpaListAssembly)
    {
        DWORD dwIncludeFlags = AssemblyName::INCLUDE_DEFAULT;

        if (!tpaListAssembly)
        {
            SString &culture = pRequestedAssemblyName->GetCulture();
            if (culture.IsEmpty() || culture.EqualsCaseInsensitive(g_BinderVariables->cultureNeutral))
            {
                dwIncludeFlags |= AssemblyName::EXCLUDE_CULTURE;
            }
        }

        if (pRequestedAssemblyName->GetArchitecture() != peNone)
        {
            dwIncludeFlags |= AssemblyName::INCLUDE_ARCHITECTURE;
        }

        return pBoundAssemblyName->Equals(pRequestedAssemblyName, dwIncludeFlags);
    }

    namespace
    {
        HRESULT BindSatelliteResourceFromBundle(
            AssemblyName*          pRequestedAssemblyName,
            SString               &relativePath,
            BindResult*            pBindResult)
        {
            HRESULT hr = S_OK;

            BundleFileLocation bundleFileLocation = Bundle::ProbeAppBundle(relativePath, /* pathIsBundleRelative */ true);
            if (!bundleFileLocation.IsValid())
            {
                return hr;
            }

            ReleaseHolder<Assembly> pAssembly;
            hr = AssemblyBinder::GetAssembly(relativePath,
                                             FALSE /* fIsInGAC */,
                                             FALSE /* fExplicitBindToNativeImage */,
                                             &pAssembly,
                                             NULL,  // szMDAssemblyPath
                                             bundleFileLocation);

            BinderTracing::PathProbed(relativePath, BinderTracing::PathSource::Bundle, hr);

            // Missing files are okay and expected when probing
            if (hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
            {
                return S_OK;
            }

            pBindResult->SetAttemptResult(hr, pAssembly);
            if (FAILED(hr))
                return hr;

            AssemblyName* pBoundAssemblyName = pAssembly->GetAssemblyName();
            if (TestCandidateRefMatchesDef(pRequestedAssemblyName, pBoundAssemblyName, false /*tpaListAssembly*/))
            {
                pBindResult->SetResult(pAssembly);
                hr = S_OK;
            }
            else
            {
                hr = FUSION_E_REF_DEF_MISMATCH;
            }

            pBindResult->SetAttemptResult(hr, pAssembly);
            return hr;
        }

        HRESULT BindSatelliteResourceByProbingPaths(
            const StringArrayList    *pResourceRoots,
            AssemblyName             *pRequestedAssemblyName,
            SString                  &relativePath,
            BindResult               *pBindResult,
            BinderTracing::PathSource pathSource)
        {
            HRESULT hr = S_OK;

            for (UINT i = 0; i < pResourceRoots->GetCount(); i++)
            {
                ReleaseHolder<Assembly> pAssembly;
                SString &wszBindingPath = (*pResourceRoots)[i];
                SString fileName(wszBindingPath);
                CombinePath(fileName, relativePath, fileName);

                hr = AssemblyBinder::GetAssembly(fileName,
                                                 FALSE /* fIsInGAC */,
                                                 FALSE /* fExplicitBindToNativeImage */,
                                                 &pAssembly);
                BinderTracing::PathProbed(fileName, pathSource, hr);

                // Missing files are okay and expected when probing
                if (hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
                {
                    continue;
                }

                pBindResult->SetAttemptResult(hr, pAssembly);
                if (FAILED(hr))
                    return hr;

                AssemblyName *pBoundAssemblyName = pAssembly->GetAssemblyName();
                if (TestCandidateRefMatchesDef(pRequestedAssemblyName, pBoundAssemblyName, false /*tpaListAssembly*/))
                {
                    pBindResult->SetResult(pAssembly);
                    hr = S_OK;
                }
                else
                {
                    hr = FUSION_E_REF_DEF_MISMATCH;
                }

                pBindResult->SetAttemptResult(hr, pAssembly);
                return hr;
            }

            // Up-stack expects S_OK when we don't find any candidate assemblies and no fatal error occurred (ie, no S_FALSE)
            return  S_OK;
        }

        HRESULT BindSatelliteResource(
            ApplicationContext* pApplicationContext,
            AssemblyName* pRequestedAssemblyName,
            BindResult* pBindResult)
        {
            // Satellite resource probing strategy is to look:
            //   * First within the single-file bundle
            //   * Then under each of the Platform Resource Roots
            //   * Then under each of the App Paths.
            //
            // During each search, if we find a platform resource file with matching file name, but whose ref-def didn't match,
            // fall back to application resource lookup to handle case where a user creates resources with the same
            // names as platform ones.

            HRESULT hr = S_OK;
            SString& simpleNameRef = pRequestedAssemblyName->GetSimpleName();
            SString& cultureRef = pRequestedAssemblyName->GetCulture();

            _ASSERTE(!cultureRef.IsEmpty() && !cultureRef.EqualsCaseInsensitive(g_BinderVariables->cultureNeutral));

            ReleaseHolder<Assembly> pAssembly;
            SString fileName;
            CombinePath(fileName, cultureRef, fileName);
            CombinePath(fileName, simpleNameRef, fileName);
            fileName.Append(W(".dll"));

            hr = BindSatelliteResourceFromBundle(pRequestedAssemblyName, fileName, pBindResult);

            if (pBindResult->HaveResult() || (FAILED(hr) && hr != FUSION_E_CONFIGURATION_ERROR))
            {
                return hr;
            }

            hr = BindSatelliteResourceByProbingPaths(pApplicationContext->GetPlatformResourceRoots(),
                                                     pRequestedAssemblyName,
                                                     fileName,
                                                     pBindResult,
                                                     BinderTracing::PathSource::PlatformResourceRoots);

            if (pBindResult->HaveResult() || (FAILED(hr) && hr != FUSION_E_CONFIGURATION_ERROR))
            {
                return hr;
            }

            hr = BindSatelliteResourceByProbingPaths(pApplicationContext->GetAppPaths(),
                                                     pRequestedAssemblyName,
                                                     fileName,
                                                     pBindResult,
                                                     BinderTracing::PathSource::AppPaths);

            return hr;
        }

        HRESULT BindAssemblyByProbingPaths(
            const StringArrayList   *pBindingPaths,
            AssemblyName            *pRequestedAssemblyName,
            bool                    useNativeImages,
            Assembly                **ppAssembly)
        {
            SString &simpleName = pRequestedAssemblyName->GetSimpleName();
            BinderTracing::PathSource pathSource = useNativeImages ? BinderTracing::PathSource::AppNativeImagePaths : BinderTracing::PathSource::AppPaths;
            // Loop through the binding paths looking for a matching assembly
            for (DWORD i = 0; i < pBindingPaths->GetCount(); i++)
            {
                HRESULT hr;
                ReleaseHolder<Assembly> pAssembly;
                LPCWSTR wszBindingPath = (*pBindingPaths)[i];

                PathString fileNameWithoutExtension(wszBindingPath);
                CombinePath(fileNameWithoutExtension, simpleName, fileNameWithoutExtension);

                // Look for a matching dll first
                PathString fileName(fileNameWithoutExtension);
                fileName.Append(useNativeImages ? W(".ni.dll") : W(".dll"));
                hr = AssemblyBinder::GetAssembly(fileName,
                                                 FALSE, // fIsInGAC
                                                 useNativeImages, // fExplicitBindToNativeImage
                                                 &pAssembly);
                BinderTracing::PathProbed(fileName, pathSource, hr);

                if (FAILED(hr))
                {
                    fileName.Set(fileNameWithoutExtension);
                    fileName.Append(useNativeImages ? W(".ni.exe") : W(".exe"));
                    hr = AssemblyBinder::GetAssembly(fileName,
                                                     FALSE, // fIsInGAC
                                                     useNativeImages, // fExplicitBindToNativeImage
                                                     &pAssembly);
                    BinderTracing::PathProbed(fileName, pathSource, hr);
                }

                // Since we're probing, file not founds are ok and we should just try another
                // probing path
                if (hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
                {
                    continue;
                }

                // Set any found assembly. It is up to the caller to check the returned HRESULT for errors due to validation
                *ppAssembly = pAssembly.Extract();
                if (FAILED(hr))
                    return hr;

                // We found a candidate.
                //
                // Below this point, we either establish that the ref-def matches, or
                // we fail the bind.

                // Compare requested AssemblyName with that from the candidate assembly
                if (!TestCandidateRefMatchesDef(pRequestedAssemblyName, pAssembly->GetAssemblyName(), false /*tpaListAssembly*/))
                    return FUSION_E_REF_DEF_MISMATCH;

                return S_OK;
            }

            return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
        }
    }

    /*
     * BindByTpaList is the entry-point for the custom binding algorithm in CoreCLR.
     *
     * The search for assemblies will proceed in the following order:
     *
     * If this application is a single-file bundle, the meta-data contained in the bundle
     * will be probed to find the requested assembly. If the assembly is not found,
     * The list of platform assemblies (TPAs) are considered next.
     *
     * Platform assemblies are specified as a list of files.  This list is the only set of
     * assemblies that we will load as platform.  They can be specified as IL or NIs.
     *
     * Resources for platform assemblies are located by probing starting at the Platform Resource Roots,
     * a set of folders configured by the host.
     *
     * If a requested assembly identity cannot be found in the TPA list or the resource roots,
     * it is considered an application assembly.  We probe for application assemblies in one of two
     * sets of paths: the AppNiPaths, a list of paths containing native images, and the AppPaths, a
     * list of paths containing IL files and satellite resource folders.
     *
     */
    /* static */
    HRESULT AssemblyBinder::BindByTpaList(ApplicationContext  *pApplicationContext,
                                          AssemblyName        *pRequestedAssemblyName,
                                          bool                 excludeAppPaths,
                                          BindResult          *pBindResult)
    {
        HRESULT hr = S_OK;

        SString &culture = pRequestedAssemblyName->GetCulture();
        bool fPartialMatchOnTpa = false;

        if (!culture.IsEmpty() && !culture.EqualsCaseInsensitive(g_BinderVariables->cultureNeutral))
        {
            IF_FAIL_GO(BindSatelliteResource(pApplicationContext, pRequestedAssemblyName, pBindResult));
        }
        else
        {
            ReleaseHolder<Assembly> pTPAAssembly;
            SString& simpleName = pRequestedAssemblyName->GetSimpleName();

            // Is assembly in the bundle?
            // Single-file bundle contents take precedence over TPA.
            // The list of bundled assemblies is contained in the bundle manifest, and NOT in the TPA.
            // Therefore the bundle is first probed using the assembly's simple name.
            // If found, the assembly is loaded from the bundle.
            if (Bundle::AppIsBundle())
            {
                // Search Assembly.ni.dll, then Assembly.dll
                // The Assembly.ni.dll paths are rare, and intended for supporting managed C++ R2R assemblies.
                SString candidates[] = { W(".ni.dll"),  W(".dll") };

                // Loop through the binding paths looking for a matching assembly
                for (int i = 0; i < 2; i++)
                {
                    SString assemblyFileName(simpleName);
                    assemblyFileName.Append(candidates[i]);

                    SString assemblyFilePath(Bundle::AppBundle->BasePath());
                    assemblyFilePath.Append(assemblyFileName);

                    BundleFileLocation bundleFileLocation = Bundle::ProbeAppBundle(assemblyFileName, /* pathIsBundleRelative */ true);
                    if (bundleFileLocation.IsValid())
                    {
                        hr = GetAssembly(assemblyFilePath,
                                         TRUE,  // fIsInGAC
                                         FALSE, // fExplicitBindToNativeImage
                                         &pTPAAssembly,
                                         NULL,  // szMDAssemblyPath
                                         bundleFileLocation);

                        BinderTracing::PathProbed(assemblyFilePath, BinderTracing::PathSource::Bundle, hr);

                        if (hr != HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
                        {
                            // Any other error is fatal
                            IF_FAIL_GO(hr);

                            if (TestCandidateRefMatchesDef(pRequestedAssemblyName, pTPAAssembly->GetAssemblyName(), true /*tpaListAssembly*/))
                            {
                                // We have found the requested assembly match in the bundle with validation of the full-qualified name.
                                // Bind to it.
                                pBindResult->SetResult(pTPAAssembly);
                                GO_WITH_HRESULT(S_OK);
                            }
                        }
                    }
                }
            }

            // Is assembly on TPA list?
            SimpleNameToFileNameMap * tpaMap = pApplicationContext->GetTpaList();
            const SimpleNameToFileNameMapEntry *pTpaEntry = tpaMap->LookupPtr(simpleName.GetUnicode());
            if (pTpaEntry != nullptr)
            {
                if (pTpaEntry->m_wszNIFileName != nullptr)
                {
                    SString fileName(pTpaEntry->m_wszNIFileName);

                    hr = GetAssembly(fileName,
                                     TRUE,  // fIsInGAC
                                     TRUE,  // fExplicitBindToNativeImage
                                     &pTPAAssembly);
                    BinderTracing::PathProbed(fileName, BinderTracing::PathSource::ApplicationAssemblies, hr);
                }
                else
                {
                    _ASSERTE(pTpaEntry->m_wszILFileName != nullptr);
                    SString fileName(pTpaEntry->m_wszILFileName);

                    hr = GetAssembly(fileName,
                                     TRUE,  // fIsInGAC
                                     FALSE, // fExplicitBindToNativeImage
                                     &pTPAAssembly);
                    BinderTracing::PathProbed(fileName, BinderTracing::PathSource::ApplicationAssemblies, hr);
                }

                pBindResult->SetAttemptResult(hr, pTPAAssembly);

                // On file not found, simply fall back to app path probing
                if (hr != HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
                {
                    // Any other error is fatal
                    IF_FAIL_GO(hr);

                    if (TestCandidateRefMatchesDef(pRequestedAssemblyName, pTPAAssembly->GetAssemblyName(), true /*tpaListAssembly*/))
                    {
                        // We have found the requested assembly match on TPA with validation of the full-qualified name. Bind to it.
                        pBindResult->SetResult(pTPAAssembly);
                        pBindResult->SetAttemptResult(S_OK, pTPAAssembly);
                        GO_WITH_HRESULT(S_OK);
                    }
                    else
                    {
                        // We found the assembly on TPA but it didn't match the RequestedAssembly assembly-name. In this case, lets proceed to see if we find the requested
                        // assembly in the App paths.
                        pBindResult->SetAttemptResult(FUSION_E_REF_DEF_MISMATCH, pTPAAssembly);
                        fPartialMatchOnTpa = true;
                    }
                }

                // We either didn't find a candidate, or the ref-def failed.  Either way; fall back to app path probing.
            }

            if (!excludeAppPaths)
            {
                // Probe AppNiPaths first, then AppPaths
                ReleaseHolder<Assembly> pAssembly;
                hr = BindAssemblyByProbingPaths(pApplicationContext->GetAppNiPaths(),
                                                pRequestedAssemblyName,
                                                true, // useNativeImages
                                                &pAssembly);
                if (hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
                {
                    hr = BindAssemblyByProbingPaths(pApplicationContext->GetAppPaths(),
                                                    pRequestedAssemblyName,
                                                    false, // useNativeImages
                                                    &pAssembly);
                }

                pBindResult->SetAttemptResult(hr, pAssembly);
                if (hr != HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
                {
                    IF_FAIL_GO(hr);

                    // At this point, we have found an assembly with the expected name in the App paths. If this was also found on TPA,
                    // make sure that the app assembly has the same fullname (excluding version) as the TPA version. If it does, then
                    // we should bind to the TPA assembly. If it does not, then bind to the app assembly since it has a different fullname than the
                    // TPA assembly.
                    if (fPartialMatchOnTpa)
                    {
                        if (TestCandidateRefMatchesDef(pAssembly->GetAssemblyName(), pTPAAssembly->GetAssemblyName(), true /*tpaListAssembly*/))
                        {
                            // Fullname (SimpleName+Culture+PKT) matched for TPA and app assembly - so bind to TPA instance.
                            pBindResult->SetResult(pTPAAssembly);
                            pBindResult->SetAttemptResult(hr, pTPAAssembly);
                            GO_WITH_HRESULT(S_OK);
                        }
                        else
                        {
                            // Fullname (SimpleName+Culture+PKT) did not match for TPA and app assembly - so bind to app instance.
                            pBindResult->SetResult(pAssembly);
                            GO_WITH_HRESULT(S_OK);
                        }
                    }
                    else
                    {
                        // We didn't see this assembly on TPA - so simply bind to the app instance.
                        pBindResult->SetResult(pAssembly);
                        GO_WITH_HRESULT(S_OK);
                    }
                }
            }
        }

        // Couldn't find a matching assembly in any of the probing paths
        // Return S_FALSE here. BindByName will interpret a successful HRESULT
        // and lack of BindResult as a failure to find a matching assembly.
        hr = S_FALSE;

    Exit:
        return hr;
    }

    /* static */
    HRESULT AssemblyBinder::GetAssembly(SString            &assemblyPath,
                                        BOOL               fIsInGAC,

                                        // When binding to the native image, should we
                                        // assume assemblyPath explicitly specifies that
                                        // NI?  (If not, infer the path to the NI
                                        // implicitly.)
                                        BOOL               fExplicitBindToNativeImage,

                                        Assembly           **ppAssembly,

                                        // If assemblyPath refers to a native image without metadata,
                                        // szMDAssemblyPath gives the alternative file to get metadata.
                                        LPCTSTR            szMDAssemblyPath,
                                        BundleFileLocation bundleFileLocation)
    {
        HRESULT hr = S_OK;

        _ASSERTE(ppAssembly != NULL);

        ReleaseHolder<Assembly> pAssembly;
        ReleaseHolder<IMDInternalImport> pIMetaDataAssemblyImport;
        DWORD dwPAFlags[2];
        PEKIND PeKind = peNone;
        PEImage *pPEImage = NULL;
        PEImage *pNativePEImage = NULL;

        // Allocate assembly object
        SAFE_NEW(pAssembly, Assembly);

        // Obtain assembly meta data
        {
            LPCTSTR szAssemblyPath = const_cast<LPCTSTR>(assemblyPath.GetUnicode());

            hr = BinderAcquirePEImage(szAssemblyPath, &pPEImage, &pNativePEImage, fExplicitBindToNativeImage, bundleFileLocation);
            IF_FAIL_GO(hr);

            // If we found a native image, it might be an MSIL assembly masquerading as an native image
            // as a fallback mechanism for when the Triton tool chain wasn't able to generate a native image.
            // In that case it will not have a native header, so just treat it like the MSIL assembly it is.
            if (pNativePEImage)
            {
                BOOL hasHeader = TRUE;
                IF_FAIL_GO(BinderHasNativeHeader(pNativePEImage, &hasHeader));
                if (!hasHeader)
                {
                    BinderReleasePEImage(pPEImage);
                    BinderReleasePEImage(pNativePEImage);

                    hr = BinderAcquirePEImage(szAssemblyPath, &pPEImage, &pNativePEImage, false, bundleFileLocation);
                    IF_FAIL_GO(hr);
                }
            }

            if (pNativePEImage)
                hr = BinderAcquireImport(pNativePEImage, &pIMetaDataAssemblyImport, dwPAFlags, TRUE);
            else
                hr = BinderAcquireImport(pPEImage, &pIMetaDataAssemblyImport, dwPAFlags, FALSE);

            IF_FAIL_GO(hr);

            if (pIMetaDataAssemblyImport == NULL && pNativePEImage != NULL)
            {
                IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND));
            }

            IF_FAIL_GO(TranslatePEToArchitectureType(dwPAFlags, &PeKind));
        }

        // Initialize assembly object
        IF_FAIL_GO(pAssembly->Init(pIMetaDataAssemblyImport,
                                   PeKind,
                                   pPEImage,
                                   pNativePEImage,
                                   assemblyPath,
                                   fIsInGAC));

        // We're done
        *ppAssembly = pAssembly.Extract();

    Exit:

        BinderReleasePEImage(pPEImage);
        BinderReleasePEImage(pNativePEImage);

        // Normalize file not found
        if ((FAILED(hr)) && IsFileNotFound(hr))
        {
            hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
        }

        return hr;
    }

#ifndef CROSSGEN_COMPILE

    /* static */
    HRESULT AssemblyBinder::Register(ApplicationContext *pApplicationContext,
                                     BindResult         *pBindResult)
    {
        HRESULT hr = S_OK;

        _ASSERTE(!pBindResult->GetIsContextBound());

        pApplicationContext->IncrementVersion();

        // Register the bindResult in the ExecutionContext only if we dont have it already.
        // This method is invoked under a lock (by its caller), so we are thread safe.
        ContextEntry *pContextEntry = NULL;
        hr = FindInExecutionContext(pApplicationContext, pBindResult->GetAssemblyName(), &pContextEntry);
        if (SUCCEEDED(hr))
        {
            if (pContextEntry == NULL)
            {
                ExecutionContext *pExecutionContext = pApplicationContext->GetExecutionContext();
                IF_FAIL_GO(pExecutionContext->Register(pBindResult));
            }
            else
            {
                // Update the BindResult with the contents of the ContextEntry we found
                pBindResult->SetResult(pContextEntry);
            }
        }

    Exit:
        return hr;
    }

    /* static */
    HRESULT AssemblyBinder::RegisterAndGetHostChosen(ApplicationContext *pApplicationContext,
                                                     LONG                kContextVersion,
                                                     BindResult         *pBindResult,
                                                     BindResult         *pHostBindResult)
    {
        HRESULT hr = S_OK;

        _ASSERTE(pBindResult != NULL);
        _ASSERTE(pBindResult->HaveResult());
        _ASSERTE(pHostBindResult != NULL);

        if (!pBindResult->GetIsContextBound())
        {
            pHostBindResult->SetResult(pBindResult);

            {
                // Lock the application context
                CRITSEC_Holder contextLock(pApplicationContext->GetCriticalSectionCookie());

                // Only perform costly validation if other binds succeded before us
                if (kContextVersion != pApplicationContext->GetVersion())
                {
                    IF_FAIL_GO(AssemblyBinder::OtherBindInterfered(pApplicationContext,
                                                                   pBindResult));

                    if (hr == S_FALSE)
                    {
                        // Another bind interfered
                        GO_WITH_HRESULT(hr);
                    }
                }

                // No bind interfered, we can now register
                IF_FAIL_GO(Register(pApplicationContext,
                                    pHostBindResult));
            }
        }
        else
        {
            // No work required. Return the input
            pHostBindResult->SetResult(pBindResult);
        }

    Exit:
        return hr;
    }

    /* static */
    HRESULT AssemblyBinder::OtherBindInterfered(ApplicationContext *pApplicationContext,
                                                BindResult         *pBindResult)
    {
        HRESULT hr = S_FALSE;
        AssemblyName *pAssemblyName = pBindResult->GetAssemblyName();
        PathString assemblyDisplayName;

        _ASSERTE(pAssemblyName != NULL);

        // Look for already cached binding failure (ignore PA, every PA will lock the context)
        pAssemblyName->GetDisplayName(assemblyDisplayName, AssemblyName::INCLUDE_VERSION);
        hr = pApplicationContext->GetFailureCache()->Lookup(assemblyDisplayName);

        if (hr == S_OK)
        {
            ContextEntry *pContextEntry = NULL;

            hr = FindInExecutionContext(pApplicationContext, pAssemblyName, &pContextEntry);

            if (SUCCEEDED(hr) && (pContextEntry == NULL))
            {
                // We can accept this bind in the domain
                GO_WITH_HRESULT(S_OK);
            }
        }

        // Some other bind interfered
        GO_WITH_HRESULT(S_FALSE);

    Exit:
        return hr;
    }

#endif //CROSSGEN_COMPILE

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
HRESULT AssemblyBinder::BindUsingHostAssemblyResolver(/* in */ INT_PTR pManagedAssemblyLoadContextToBindWithin,
                                                      /* in */ AssemblyName       *pAssemblyName,
                                                      /* in */ IAssemblyName      *pIAssemblyName,
                                                      /* in */ CLRPrivBinderCoreCLR *pTPABinder,
                                                      /* out */ Assembly           **ppAssembly)
{
    HRESULT hr = E_FAIL;

    _ASSERTE(pManagedAssemblyLoadContextToBindWithin != NULL);

    // RuntimeInvokeHostAssemblyResolver will perform steps 2-4 of CLRPrivBinderAssemblyLoadContext::BindAssemblyByName.
    ICLRPrivAssembly *pLoadedAssembly = NULL;
    hr = RuntimeInvokeHostAssemblyResolver(pManagedAssemblyLoadContextToBindWithin, pIAssemblyName,
                                           pTPABinder, pAssemblyName, &pLoadedAssembly);
    if (SUCCEEDED(hr))
    {
        _ASSERTE(pLoadedAssembly != NULL);
        *ppAssembly = static_cast<Assembly *>(pLoadedAssembly);
    }

    return hr;
}

/* static */
HRESULT AssemblyBinder::BindUsingPEImage(/* in */  ApplicationContext *pApplicationContext,
                                         /* in */  BINDER_SPACE::AssemblyName *pAssemblyName,
                                         /* in */  PEImage            *pPEImage,
                                         /* in */  PEKIND              peKind,
                                         /* in */  IMDInternalImport  *pIMetaDataAssemblyImport,
                                         /* [retval] [out] */  Assembly **ppAssembly)
{
    HRESULT hr = E_FAIL;

    // Indirect check that binder was initialized.
    _ASSERTE(g_BinderVariables != NULL);

    LONG kContextVersion = 0;
    BindResult bindResult;

    // Prepare binding data
    *ppAssembly = NULL;

    // Tracing happens outside the binder lock to avoid calling into managed code within the lock
    BinderTracing::ResolutionAttemptedOperation tracer{pAssemblyName, pApplicationContext->GetBinderID(), 0 /*managedALC*/, hr};

    // Attempt the actual bind (eventually more than once)
Retry:
    bool mvidMismatch = false;
    {
        // Lock the application context
        CRITSEC_Holder contextLock(pApplicationContext->GetCriticalSectionCookie());

        // Attempt uncached bind and register stream if possible
        // We skip version compatibility check - so assemblies with same simple name will be reported
        // as a successful bind. Below we compare MVIDs in that case instead (which is a more precise equality check).
        hr = BindByName(pApplicationContext,
                        pAssemblyName,
                        true,  // skipFailureCaching
                        true,  // skipVersionCompatibilityCheck
                        false, // excludeAppPaths
                        &bindResult);

        if (hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
        {
            IF_FAIL_GO(CreateImageAssembly(pIMetaDataAssemblyImport,
                                           peKind,
                                           pPEImage,
                                           NULL,
                                           &bindResult));
        }
        else if (hr == S_OK)
        {
            if (bindResult.HaveResult())
            {
                // Attempt was made to load an assembly that has the same name as a previously loaded one. Since same name
                // does not imply the same assembly, we will need to check the MVID to confirm it is the same assembly as being
                // requested.
                //
                GUID incomingMVID;
                ZeroMemory(&incomingMVID, sizeof(GUID));

                // If we cannot get MVID, then err on side of caution and fail the
                // load.
                IF_FAIL_GO(pIMetaDataAssemblyImport->GetScopeProps(NULL, &incomingMVID));

                GUID boundMVID;
                ZeroMemory(&boundMVID, sizeof(GUID));

                // If we cannot get MVID, then err on side of caution and fail the
                // load.
                IF_FAIL_GO(bindResult.GetAsAssembly()->GetMVID(&boundMVID));

                mvidMismatch = incomingMVID != boundMVID;
                if (mvidMismatch)
                {
                    // MVIDs do not match, so fail the load.
                    IF_FAIL_GO(COR_E_FILELOAD);
                }

                // MVIDs match - request came in for the same assembly that was previously loaded.
                // Let it through...
            }
        }

        // Remember the post-bind version of the context
        kContextVersion = pApplicationContext->GetVersion();

    } // lock(pApplicationContext)

    if (bindResult.HaveResult())
    {
        BindResult hostBindResult;

        // This has to happen outside the binder lock as it can cause new binds
        IF_FAIL_GO(RegisterAndGetHostChosen(pApplicationContext,
                                            kContextVersion,
                                            &bindResult,
                                            &hostBindResult));

        if (hr == S_FALSE)
        {
            tracer.TraceBindResult(bindResult);

            // Another bind interfered. We need to retry entire bind.
            // This by design loops as long as needed because by construction we eventually
            // will succeed or fail the bind.
            bindResult.Reset();
            goto Retry;
        }
        else if (hr == S_OK)
        {
            *ppAssembly = hostBindResult.GetAsAssembly(TRUE /* fAddRef */);
        }
    }

Exit:
    tracer.TraceBindResult(bindResult, mvidMismatch);
    return hr;
}
#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
};



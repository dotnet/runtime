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

#include "common.h"
#include "assemblybindercommon.hpp"
#include "assemblyname.hpp"
#include "assembly.hpp"
#include "applicationcontext.hpp"
#include "bindertracing.h"
#include "loadcontext.hpp"
#include "bindresult.inl"
#include "failurecache.hpp"
#include "utils.hpp"
#include "stringarraylist.h"
#include "configuration.h"

#if !defined(DACCESS_COMPILE)
#include "defaultassemblybinder.h"
// Helper function in the VM, invoked by the Binder, to invoke the host assembly resolver
extern HRESULT RuntimeInvokeHostAssemblyResolver(INT_PTR pManagedAssemblyLoadContextToBindWithin,
                                                 BINDER_SPACE::AssemblyName *pAssemblyName,
                                                 DefaultAssemblyBinder *pDefaultBinder,
                                                 BINDER_SPACE::Assembly **ppLoadedAssembly);

#endif // !defined(DACCESS_COMPILE)

STDAPI BinderAcquirePEImage(LPCTSTR            szAssemblyPath,
    PEImage** ppPEImage,
    BundleFileLocation bundleFileLocation);

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

        HRESULT CreateImageAssembly(PEImage                 *pPEImage,
                                    BindResult              *pBindResult)
        {
            HRESULT hr = S_OK;
            ReleaseHolder<Assembly> pAssembly;

            SAFE_NEW(pAssembly, Assembly);
            IF_FAIL_GO(pAssembly->Init(pPEImage, /* fIsInTPA */ FALSE ));

            pBindResult->SetResult(pAssembly);
            pBindResult->SetIsFirstRequest(TRUE);

        Exit:
            return hr;
        }
    };

    HRESULT AssemblyBinderCommon::TranslatePEToArchitectureType(DWORD  *pdwPAFlags, PEKIND *PeKind)
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

    HRESULT AssemblyBinderCommon::BindAssembly(/* in */  AssemblyBinder      *pBinder,
                                               /* in */  AssemblyName        *pAssemblyName,
                                               /* in */  bool                 excludeAppPaths,
                                               /* out */ Assembly           **ppAssembly)
    {
        HRESULT hr = S_OK;
        LONG kContextVersion = 0;
        BindResult bindResult;
        ApplicationContext* pApplicationContext = pBinder->GetAppContext();

        // Tracing happens outside the binder lock to avoid calling into managed code within the lock
        BinderTracing::ResolutionAttemptedOperation tracer{pAssemblyName, pBinder, 0 /*managedALC*/, hr};

    Retry:
        {
            // Lock the binding application context
            CRITSEC_Holder contextLock(pApplicationContext->GetCriticalSectionCookie());

            _ASSERTE(pAssemblyName != NULL);
            IF_FAIL_GO(BindByName(pApplicationContext,
                                    pAssemblyName,
                                    false, // skipFailureCaching
                                    false, // skipVersionCompatibilityCheck
                                    excludeAppPaths,
                                    &bindResult));

            // Remember the post-bind version
            kContextVersion = pApplicationContext->GetVersion();

        } // lock(pApplicationContext)

    Exit:
        tracer.TraceBindResult(bindResult);

        if (bindResult.HaveResult())
        {
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
                *ppAssembly = hostBindResult.GetAssembly(TRUE /* fAddRef */);
            }
        }

        return hr;
    }

    /* static */
    HRESULT AssemblyBinderCommon::BindToSystem(SString   &systemDirectory,
                                               Assembly **ppSystemAssembly)
    {
        HRESULT hr = S_OK;

        _ASSERTE(ppSystemAssembly != NULL);

        ReleaseHolder<Assembly> pSystemAssembly;

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

        hr = AssemblyBinderCommon::GetAssembly(sCoreLib,
                                         TRUE /* fIsInTPA */,
                                         &pSystemAssembly,
                                         bundleFileLocation);

        BinderTracing::PathProbed(sCoreLib, pathSource, hr);

        if (hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
        {
            // Try to find corelib in the TPA
            StackSString sCoreLibSimpleName(CoreLibName_W);
            StackSString sTrustedPlatformAssemblies = Configuration::GetKnobStringValue(W("TRUSTED_PLATFORM_ASSEMBLIES"));
            sTrustedPlatformAssemblies.Normalize();

            bool found = false;
            for (SString::Iterator i = sTrustedPlatformAssemblies.Begin(); i != sTrustedPlatformAssemblies.End(); )
            {
                SString fileName;
                SString simpleName;
                bool isNativeImage = false;
                HRESULT pathResult = S_OK;
                IF_FAIL_GO(pathResult = GetNextTPAPath(sTrustedPlatformAssemblies, i, /*dllOnly*/ true, fileName, simpleName, isNativeImage));
                if (pathResult == S_FALSE)
                {
                    break;
                }

                if (simpleName.EqualsCaseInsensitive(sCoreLibSimpleName))
                {
                    sCoreLib = fileName;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                GO_WITH_HRESULT(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND));
            }

            hr = AssemblyBinderCommon::GetAssembly(sCoreLib,
                TRUE /* fIsInTPA */,
                &pSystemAssembly,
                bundleFileLocation);

            BinderTracing::PathProbed(sCoreLib, BinderTracing::PathSource::ApplicationAssemblies, hr);
        }

        IF_FAIL_GO(hr);

        *ppSystemAssembly = pSystemAssembly.Extract();

    Exit:
        return hr;
    }


    /* static */
    HRESULT AssemblyBinderCommon::BindToSystemSatellite(SString& systemDirectory,
        SString& simpleName,
        SString& cultureName,
        Assembly** ppSystemAssembly)
    {
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
        IF_FAIL_GO(AssemblyBinderCommon::GetAssembly(sCoreLibSatellite,
                                               TRUE /* fIsInTPA */,
                                               &pSystemAssembly,
                                               bundleFileLocation));
        BinderTracing::PathProbed(sCoreLibSatellite, pathSource, hr);

        *ppSystemAssembly = pSystemAssembly.Extract();

    Exit:
        return hr;
    }

    /* static */
    HRESULT AssemblyBinderCommon::BindByName(ApplicationContext *pApplicationContext,
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

        if (!IsValidArchitecture(pAssemblyName->GetArchitecture()))
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
    HRESULT AssemblyBinderCommon::BindLocked(ApplicationContext *pApplicationContext,
                                             AssemblyName       *pAssemblyName,
                                             bool                skipVersionCompatibilityCheck,
                                             bool                excludeAppPaths,
                                             BindResult         *pBindResult)
    {
        HRESULT hr = S_OK;

        bool isTpaListProvided = pApplicationContext->IsTpaListProvided();
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
                pBindResult->SetAttemptResult(hr, pBindResult->GetAssembly());

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

    /* static */
    HRESULT AssemblyBinderCommon::FindInExecutionContext(ApplicationContext  *pApplicationContext,
                                                         AssemblyName        *pAssemblyName,
                                                         ContextEntry       **ppContextEntry)
    {
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
            if (pRequestedAssemblyName->IsNeutralCulture())
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
            hr = AssemblyBinderCommon::GetAssembly(relativePath,
                                                   FALSE /* fIsInTPA */,
                                                   &pAssembly,
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

                hr = AssemblyBinderCommon::GetAssembly(fileName,
                                                       FALSE /* fIsInTPA */,
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
            const SString& simpleNameRef = pRequestedAssemblyName->GetSimpleName();
            SString& cultureRef = pRequestedAssemblyName->GetCulture();

            _ASSERTE(!pRequestedAssemblyName->IsNeutralCulture());

            ReleaseHolder<Assembly> pAssembly;
            SString fileName;
            CombinePath(fileName, cultureRef, fileName);
            CombinePath(fileName, simpleNameRef, fileName);
            fileName.Append(W(".dll"));

            hr = BindSatelliteResourceFromBundle(pRequestedAssemblyName, fileName, pBindResult);

            if (pBindResult->HaveResult() || FAILED(hr))
            {
                return hr;
            }

            hr = BindSatelliteResourceByProbingPaths(pApplicationContext->GetPlatformResourceRoots(),
                                                     pRequestedAssemblyName,
                                                     fileName,
                                                     pBindResult,
                                                     BinderTracing::PathSource::PlatformResourceRoots);

            if (pBindResult->HaveResult() || FAILED(hr))
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
            Assembly                **ppAssembly)
        {
            const SString &simpleName = pRequestedAssemblyName->GetSimpleName();
            BinderTracing::PathSource pathSource = BinderTracing::PathSource::AppPaths;
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
                fileName.Append(W(".dll"));
                hr = AssemblyBinderCommon::GetAssembly(fileName,
                                                 FALSE, // fIsInTPA
                                                 &pAssembly);
                BinderTracing::PathProbed(fileName, pathSource, hr);

                if (FAILED(hr))
                {
                    fileName.Set(fileNameWithoutExtension);
                    fileName.Append(W(".exe"));
                    hr = AssemblyBinderCommon::GetAssembly(fileName,
                                                     FALSE, // fIsInTPA
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
     * it is considered an application assembly.  We probe for application assemblies in the
     * AppPaths, a list of paths containing IL files and satellite resource folders.
     *
     */
    /* static */
    HRESULT AssemblyBinderCommon::BindByTpaList(ApplicationContext  *pApplicationContext,
                                                AssemblyName        *pRequestedAssemblyName,
                                                bool                 excludeAppPaths,
                                                BindResult          *pBindResult)
    {
        HRESULT hr = S_OK;

        bool fPartialMatchOnTpa = false;

        if (!pRequestedAssemblyName->IsNeutralCulture())
        {
            IF_FAIL_GO(BindSatelliteResource(pApplicationContext, pRequestedAssemblyName, pBindResult));
        }
        else
        {
            ReleaseHolder<Assembly> pTPAAssembly;
            const SString& simpleName = pRequestedAssemblyName->GetSimpleName();

            // Is assembly in the bundle?
            // Single-file bundle contents take precedence over TPA.
            // The list of bundled assemblies is contained in the bundle manifest, and NOT in the TPA.
            // Therefore the bundle is first probed using the assembly's simple name.
            // If found, the assembly is loaded from the bundle.
            if (Bundle::AppIsBundle())
            {
                // Search Assembly.ni.dll, then Assembly.dll
                // The Assembly.ni.dll paths are rare, and intended for supporting managed C++ R2R assemblies.
                const WCHAR* const candidates[] = { W(".ni.dll"),  W(".dll") };

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
                                         TRUE,  // fIsInTPA
                                         &pTPAAssembly,
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
                                     TRUE,  // fIsInTPA
                                     &pTPAAssembly);
                    BinderTracing::PathProbed(fileName, BinderTracing::PathSource::ApplicationAssemblies, hr);
                }
                else
                {
                    _ASSERTE(pTpaEntry->m_wszILFileName != nullptr);
                    SString fileName(pTpaEntry->m_wszILFileName);

                    hr = GetAssembly(fileName,
                                     TRUE,  // fIsInTPA
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
                // Probe AppPaths
                ReleaseHolder<Assembly> pAssembly;
                hr = BindAssemblyByProbingPaths(pApplicationContext->GetAppPaths(),
                                                pRequestedAssemblyName,
                                                &pAssembly);

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
    HRESULT AssemblyBinderCommon::GetAssembly(SString            &assemblyPath,
                                              BOOL               fIsInTPA,
                                              Assembly           **ppAssembly,
                                              BundleFileLocation bundleFileLocation)
    {
        HRESULT hr = S_OK;

        _ASSERTE(ppAssembly != NULL);

        ReleaseHolder<Assembly> pAssembly;
        PEImage *pPEImage = NULL;

        // Allocate assembly object
        SAFE_NEW(pAssembly, Assembly);

        // Obtain assembly meta data
        {
            LPCTSTR szAssemblyPath = const_cast<LPCTSTR>(assemblyPath.GetUnicode());

            hr = BinderAcquirePEImage(szAssemblyPath, &pPEImage, bundleFileLocation);
            IF_FAIL_GO(hr);
        }

        // Initialize assembly object
        IF_FAIL_GO(pAssembly->Init(pPEImage, fIsInTPA));

        // We're done
        *ppAssembly = pAssembly.Extract();

    Exit:

        SAFE_RELEASE(pPEImage);

        // Normalize file not found
        if ((FAILED(hr)) && IsFileNotFound(hr))
        {
            hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
        }

        return hr;
    }


    /* static */
    HRESULT AssemblyBinderCommon::Register(ApplicationContext *pApplicationContext,
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
    HRESULT AssemblyBinderCommon::RegisterAndGetHostChosen(ApplicationContext *pApplicationContext,
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
                    IF_FAIL_GO(AssemblyBinderCommon::OtherBindInterfered(pApplicationContext,
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
    HRESULT AssemblyBinderCommon::OtherBindInterfered(ApplicationContext *pApplicationContext,
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


#if !defined(DACCESS_COMPILE)
HRESULT AssemblyBinderCommon::BindUsingHostAssemblyResolver(/* in */ INT_PTR pManagedAssemblyLoadContextToBindWithin,
                                                            /* in */ AssemblyName       *pAssemblyName,
                                                            /* in */ DefaultAssemblyBinder *pDefaultBinder,
                                                            /* out */ Assembly           **ppAssembly)
{
    HRESULT hr = E_FAIL;

    _ASSERTE(pManagedAssemblyLoadContextToBindWithin != NULL);

    // RuntimeInvokeHostAssemblyResolver will perform steps 2-4 of CustomAssemblyBinder::BindAssemblyByName.
    BINDER_SPACE::Assembly *pLoadedAssembly = NULL;
    hr = RuntimeInvokeHostAssemblyResolver(pManagedAssemblyLoadContextToBindWithin,
                                           pAssemblyName, pDefaultBinder, &pLoadedAssembly);
    if (SUCCEEDED(hr))
    {
        _ASSERTE(pLoadedAssembly != NULL);
        *ppAssembly = pLoadedAssembly;
    }

    return hr;
}

/* static */
HRESULT AssemblyBinderCommon::BindUsingPEImage(/* in */  AssemblyBinder* pBinder,
                                               /* in */  BINDER_SPACE::AssemblyName *pAssemblyName,
                                               /* in */  PEImage            *pPEImage,
                                               /* [retval] [out] */  Assembly **ppAssembly)
{
    HRESULT hr = E_FAIL;

    LONG kContextVersion = 0;
    BindResult bindResult;

    // Prepare binding data
    *ppAssembly = NULL;
    ApplicationContext* pApplicationContext = pBinder->GetAppContext();

    // Tracing happens outside the binder lock to avoid calling into managed code within the lock
    BinderTracing::ResolutionAttemptedOperation tracer{pAssemblyName, pBinder, 0 /*managedALC*/, hr};

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
            IF_FAIL_GO(CreateImageAssembly(pPEImage, &bindResult));
        }
        else if (hr == S_OK)
        {
            if (bindResult.HaveResult())
            {
                // Attempt was made to load an assembly that has the same name as a previously loaded one. Since same name
                // does not imply the same assembly, we will need to check the MVID to confirm it is the same assembly as being
                // requested.

                GUID incomingMVID;
                GUID boundMVID;

                // GetMVID can throw exception
                EX_TRY
                {
                    pPEImage->GetMVID(&incomingMVID);
                    bindResult.GetAssembly()->GetPEImage()->GetMVID(&boundMVID);
                }
                EX_CATCH
                {
                    hr = GET_EXCEPTION()->GetHR();
                    goto Exit;
                }
                EX_END_CATCH(SwallowAllExceptions);


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
            *ppAssembly = hostBindResult.GetAssembly(TRUE /* fAddRef */);
        }
    }

Exit:
    tracer.TraceBindResult(bindResult, mvidMismatch);
    return hr;
}

HRESULT AssemblyBinderCommon::CreateDefaultBinder(DefaultAssemblyBinder** ppDefaultBinder)
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        if (ppDefaultBinder != NULL)
        {
            NewHolder<DefaultAssemblyBinder> pBinder;
            SAFE_NEW(pBinder, DefaultAssemblyBinder);

            BINDER_SPACE::ApplicationContext* pApplicationContext = pBinder->GetAppContext();
            hr = pApplicationContext->Init();
            if (SUCCEEDED(hr))
            {
                pBinder->SetManagedAssemblyLoadContext(NULL);
                *ppDefaultBinder = pBinder.Extract();
            }
        }
    }
    EX_CATCH_HRESULT(hr);

Exit:
    return hr;
}

BOOL AssemblyBinderCommon::IsValidArchitecture(PEKIND kArchitecture)
{
    if ((kArchitecture == peMSIL) || (kArchitecture == peNone))
        return TRUE;

    PEKIND processArchitecture =
#if defined(TARGET_X86)
        peI386;
#elif defined(TARGET_AMD64)
        peAMD64;
#elif defined(TARGET_ARM)
        peARM;
#elif defined(TARGET_ARM64)
        peARM64;
#else
        peMSIL;
#endif

    return (kArchitecture == processArchitecture);
}

#endif // !defined(DACCESS_COMPILE)
};



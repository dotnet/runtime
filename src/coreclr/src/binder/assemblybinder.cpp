//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
#include "loadcontext.hpp"
#include "bindresult.inl"
#include "failurecache.hpp"
#ifdef FEATURE_VERSIONING_LOG
#include "bindinglog.hpp"
#endif // FEATURE_VERSIONING_LOG
#include "compatibility.hpp"
#include "utils.hpp"
#include "variables.hpp"
#include "stringarraylist.h"

#include "strongname.h"
#ifdef FEATURE_VERSIONING_LOG
#include "../dlls/mscorrc/fusres.h"
#endif // FEATURE_VERSIONING_LOG

#define APP_DOMAIN_LOCKED_INSPECTION_ONLY 0x01
#define APP_DOMAIN_LOCKED_UNLOCKED        0x02
#define APP_DOMAIN_LOCKED_CONTEXT         0x04

#define BIND_BEHAVIOR_STATIC            0
#define BIND_BEHAVIOR_ORDER_INDEPENDENT 1
#define BIND_BEHAVIOR_BEST_MATCH        2

#ifndef IMAGE_FILE_MACHINE_ARM64
#define IMAGE_FILE_MACHINE_ARM64             0xAA64  // ARM64 Little-Endian
#endif

BOOL IsCompilationProcess();

#ifdef FEATURE_LEGACYNETCF
extern BOOL RuntimeIsLegacyNetCF(DWORD adid);
#endif

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE) && !defined(MDILNIGEN)
#include "clrprivbindercoreclr.h"
#include "clrprivbinderassemblyloadcontext.h"
// Helper function in the VM, invoked by the Binder, to invoke the host assembly resolver
extern HRESULT RuntimeInvokeHostAssemblyResolver(CLRPrivBinderAssemblyLoadContext *pLoadContextToBindWithin, IAssemblyName *pIAssemblyName, ICLRPrivAssembly **ppLoadedAssembly);

// Helper to check if we have a host assembly resolver set
extern BOOL RuntimeCanUseAppPathAssemblyResolver(DWORD adid);

#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE) && !defined(MDILNIGEN)

namespace BINDER_SPACE
{
    typedef enum
    {
        kVersionIgnore,
        kVersionExact,
        kVersionServiceRollForward,
        kVersionFeatureRollForward,
        kVersionFeatureExact,
        kVersionFeatureHighest,
        kVersionFeatureLowestHigher,
        kVersionFeatureHighestLower
    } VersionMatchMode;

    namespace
    {
        BOOL fAssemblyBinderInitialized = FALSE;

        //
        // This defines the assembly equivalence relation
        //
        HRESULT IsValidAssemblyVersion(/* in */ AssemblyName *pRequestedName,
                                       /* in */ AssemblyName *pFoundName,
                                       /* in */ ApplicationContext *pApplicationContext,
                                       /* in */ BOOL fBeingBoundToPlatformAssembly)
        {
            HRESULT hr = S_OK;
            BINDER_LOG_ENTER(W("IsValidAssemblyVersion"));
            AssemblyVersion *pRequestedVersion = pRequestedName->GetVersion();
            AssemblyVersion *pFoundVersion = pFoundName->GetVersion();

            bool fWindowsPhone7 = false;
#ifdef FEATURE_LEGACYNETCF
            fWindowsPhone7 = RuntimeIsLegacyNetCF(pApplicationContext->GetAppDomainId()) == TRUE;
#endif

            //
            // If the AssemblyRef has no version and we're not binding to a platform assembly,
            // we can skip version checking and allow the bind
            //
            //
            // Windows Phone 7 Quirk:
            //
            // NetCF allows partial binds to platform assemblies.  If we are running a
            // Mango application, skip the version check if no Ref version is provided,
            // since there are apps in the Marketplace that do Assembly.Load("System")
            //
            if (((fWindowsPhone7 && fBeingBoundToPlatformAssembly) || !fBeingBoundToPlatformAssembly) && !pRequestedName->HaveAssemblyVersion())
                
            {
                return hr;
            }
            

            //
            // Windows Phone 7 Quirk:
            //
            // NetCF does not respect version numbers when binding unsigned assemblies.  If you ask
            // for a version you can get any version back, including an unversioned assembly.
            //
            if (!fWindowsPhone7 || pRequestedName->IsStronglyNamed())
            {
                //
                // This if condition is paired with the one above that checks for pRequestedName
                // not having an assembly version.  If we didn't exit in the above if condition,
                // and satisfy this one's requirements, we're in a situation where the assembly
                // Ref has a version, but the Def doesn't, which cannot succeed a bind
                //
                if (!fBeingBoundToPlatformAssembly
                    && pRequestedName->HaveAssemblyVersion()
                    && !pFoundName->HaveAssemblyVersion())
                {
                    hr = FUSION_E_APP_DOMAIN_LOCKED;
                }
                else if (pRequestedVersion->IsEqualFeatureVersion(pFoundVersion))
                {
                    // Now service version matters
                    if (pRequestedVersion->IsLargerServiceVersion(pFoundVersion))
                    {
                        hr = FUSION_E_APP_DOMAIN_LOCKED;
                    }
                }
                else if (pRequestedVersion->IsLargerFeatureVersion(pFoundVersion))
                {
                    hr = FUSION_E_APP_DOMAIN_LOCKED;
                }
            }
            
            if (pApplicationContext->IsTpaListProvided() && hr == FUSION_E_APP_DOMAIN_LOCKED)
            {
                // For our new binding models, use a more descriptive error code than APP_DOMAIN_LOCKED for bind
                // failures.
                hr = FUSION_E_REF_DEF_MISMATCH;
            }

            BINDER_LOG_LEAVE_HR(W("IsValidAssemblyVersion"), hr)
            return hr;
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
                PathString fullAssemblyPath;
                WCHAR *pwzFullAssemblyPath = fullAssemblyPath.OpenUnicodeBuffer(MAX_PATH);
                DWORD dwCCFullAssemblyPath = MAX_PATH + 1; // SString allocates extra byte for null.

                MutateUrlToPath(assemblyPath);

                dwCCFullAssemblyPath = WszGetFullPathName(assemblyPath.GetUnicode(),
                                                          dwCCFullAssemblyPath,
                                                          pwzFullAssemblyPath,
                                                          NULL);
                fullAssemblyPath.CloseBuffer(dwCCFullAssemblyPath);

                if ((dwCCFullAssemblyPath == 0) || (dwCCFullAssemblyPath > (MAX_PATH + 1)))
                {
                    hr = HRESULT_FROM_GetLastError();
                }
                else
                {
                    assemblyPath.Set(fullAssemblyPath);
                }

                // Now turn this path into our canonical representation
                CanonicalizePath(assemblyPath);
            }

            return hr;
        }


#ifdef FEATURE_VERSIONING_LOG
        //
        // This function outputs the current binding result
        // and flushes the bind log.
        //
        HRESULT LogBindResult(ApplicationContext *pApplicationContext,
                              HRESULT             hrLog,
                              BindResult         *pBindResult)
        {
            HRESULT hr = S_OK;
            BindingLog *pBindingLog = pApplicationContext->GetBindingLog();

            if (!pBindingLog->CanLog())
            {
                // For non-logging, return the bind result
                hr = hrLog;
                goto Exit;
            }

            IF_FAIL_GO(pBindingLog->LogHR(hrLog));

            if ((hrLog == S_OK) && pBindResult->HaveResult())
            {
                IF_FAIL_GO(pBindingLog->LogResult(pBindResult));
            }

            IF_FAIL_GO(pBindingLog->Flush());

            // For failure-free logging, return the bind result
            hr = hrLog;

        Exit:
            // SilverLight does not propagate binding log; therefore kill the information here.
            pApplicationContext->ClearBindingLog();
            return hr;
        }

        inline UINT GetLockedContextEntry(BOOL fInspectionOnly)
        {
            if (fInspectionOnly)
                return ID_FUSLOG_BINDING_LOCKED_ASSEMBLY_INS_CONTEXT;
            else
                return ID_FUSLOG_BINDING_LOCKED_ASSEMBLY_EXE_CONTEXT;
        }

        inline UINT GetLockedEntry(BOOL fInspectionOnly)
        {
            if (fInspectionOnly)
                return ID_FUSLOG_BINDING_LOCKED_MT_INS_LOCKED_ENTRY;
            else
                return ID_FUSLOG_BINDING_LOCKED_MT_EXE_LOCKED_ENTRY;
        }

        inline UINT GetLocalizedEntry(BOOL fInspectionOnly)
        {
            if (fInspectionOnly)
                return ID_FUSLOG_BINDING_LOCKED_MT_INS_LOCALI_ENTRY;
            else
                return ID_FUSLOG_BINDING_LOCKED_MT_EXE_LOCALI_ENTRY;
        }

        inline UINT GetCBaseEntry(BOOL fInspectionOnly)
        {
            if (fInspectionOnly)
                return ID_FUSLOG_BINDING_LOCKED_MT_INS_CBASE_ENTRY;
            else
                return ID_FUSLOG_BINDING_LOCKED_MT_EXE_CBASE_ENTRY;
        }

        HRESULT LogAppDomainLocked(ApplicationContext *pApplicationContext,
                                   DWORD               dwLockedReason,
                                   AssemblyName       *pAssemblyName = NULL)
        {
            HRESULT hr = S_OK;
            BindingLog *pBindingLog = pApplicationContext->GetBindingLog();

            if (pBindingLog->CanLog())
            {
                PathString info;
                PathString format;
                BOOL fInspectionOnly = FALSE;

                if ((dwLockedReason & APP_DOMAIN_LOCKED_INSPECTION_ONLY) != 0)
                {
                    dwLockedReason &= ~APP_DOMAIN_LOCKED_INSPECTION_ONLY;
                    fInspectionOnly = TRUE;
                }

                switch (dwLockedReason)
                {
                case APP_DOMAIN_LOCKED_UNLOCKED:
                {
                    IF_FAIL_GO(info.
                               LoadResourceAndReturnHR(CCompRC::Debugging,
                                                       ID_FUSLOG_BINDING_LOCKED_UNLOCKED));
                }
                break;
                case APP_DOMAIN_LOCKED_CONTEXT:
                {
                    PathString displayName;

                    _ASSERTE(pAssemblyName != NULL);

                    IF_FAIL_GO(format.
                               LoadResourceAndReturnHR(CCompRC::Debugging,
                                                       GetLockedContextEntry(fInspectionOnly)));

                    pAssemblyName->GetDisplayName(displayName,
                                                  AssemblyName::INCLUDE_VERSION |
                                                  AssemblyName::INCLUDE_ARCHITECTURE);

                    info.Printf(format.GetUnicode(), displayName.GetUnicode());
                }
                break;
                default:
                    _ASSERTE(0);
                    IF_FAIL_GO(E_INVALIDARG);
                    break;
                }

                IF_FAIL_GO(pBindingLog->Log(info));
            }

        Exit:
            return hr;
        }

        HRESULT LogBindBehavior(ApplicationContext *pApplicationContext,
                                DWORD               dwBindBehavior)
        {
            HRESULT hr = S_OK;
            BindingLog *pBindingLog = pApplicationContext->GetBindingLog();

            if (pBindingLog->CanLog())
            {
                PathString bindBehavior;
                UINT uiBindBehavior = 0;

                switch (dwBindBehavior)
                {
                case BIND_BEHAVIOR_STATIC:
                    uiBindBehavior = ID_FUSLOG_BINDING_BEHAVIOR_STATIC;
                    break;
                case BIND_BEHAVIOR_ORDER_INDEPENDENT:
                    uiBindBehavior = ID_FUSLOG_BINDING_BEHAVIOR_ORDER_INDEPENDENT;
                    break;
                case BIND_BEHAVIOR_BEST_MATCH:
                    uiBindBehavior = ID_FUSLOG_BINDING_BEHAVIOR_BEST_MATCH;
                    break;
                default:
                    _ASSERTE(0);
                    IF_FAIL_GO(E_INVALIDARG);
                    break;
                }

                IF_FAIL_GO(bindBehavior.LoadResourceAndReturnHR(CCompRC::Debugging, uiBindBehavior));
                IF_FAIL_GO(pBindingLog->Log(bindBehavior.GetUnicode()));
            }

        Exit:
            return hr;
        }

        HRESULT LogAssemblyNameWhereRef(ApplicationContext *pApplicationContext,
                                        Assembly           *pAssembly)
        {
            HRESULT hr = S_OK;
            BindingLog *pBindingLog = pApplicationContext->GetBindingLog();

            if (pBindingLog->CanLog())
            {
                PathString info;

                IF_FAIL_GO(info.
                           LoadResourceAndReturnHR(CCompRC::Debugging, ID_FUSLOG_BINDING_LOG_WHERE_REF_NAME));
                IF_FAIL_GO(pBindingLog->LogAssemblyName(info.GetUnicode(),
                                                        pAssembly->GetAssemblyName()));
            }
            
        Exit:
            return hr;
        }

        HRESULT LogConfigurationError(ApplicationContext *pApplicationContext,
                                      AssemblyName       *pCulturedManifestName,
                                      AssemblyName       *pLocalPathAssemblyName)
        {
            HRESULT hr = S_OK;
            BindingLog *pBindingLog = pApplicationContext->GetBindingLog();

            if (pBindingLog->CanLog())
            {
                PathString tmp;
                PathString culturedManifestDisplayName;
                PathString localPathDisplayName;
                PathString info;

                IF_FAIL_GO(tmp.
                           LoadResourceAndReturnHR(CCompRC::Debugging, ID_FUSLOG_BINDING_LOG_ERRONOUS_MANIFEST_ENTRY));

                pCulturedManifestName->GetDisplayName(culturedManifestDisplayName,
                                                      AssemblyName::INCLUDE_VERSION |
                                                      AssemblyName::INCLUDE_ARCHITECTURE);
                pLocalPathAssemblyName->GetDisplayName(localPathDisplayName,
                                                       AssemblyName::INCLUDE_VERSION |
                                                       AssemblyName::INCLUDE_ARCHITECTURE);
                
                info.Printf(tmp.GetUnicode(),
                            culturedManifestDisplayName.GetUnicode(), 
                            localPathDisplayName.GetUnicode());
                IF_FAIL_GO(pBindingLog->Log(info.GetUnicode()));
            }

        Exit:
            return hr;
        }

        HRESULT LogPathAttempt(ApplicationContext *pApplicationContext,
                               PathString         &assemblyPath)
        {
            HRESULT hr = S_OK;
            BindingLog *pBindingLog = pApplicationContext->GetBindingLog();

            if (pBindingLog->CanLog())
            {
                PathString tmp;
                PathString info;

                IF_FAIL_GO(tmp.LoadResourceAndReturnHR(CCompRC::Debugging,
                                                       ID_FUSLOG_BINDING_LOG_PATH_ATTEMPT));
                info.Printf(tmp.GetUnicode(), assemblyPath.GetUnicode());

                IF_FAIL_GO(pBindingLog->Log(info));
            }

        Exit:
            return hr;
        }
#endif // FEATURE_VERSIONING_LOG

#ifndef CROSSGEN_COMPILE
        HRESULT CreateImageAssembly(IMDInternalImport       *pIMetaDataAssemblyImport,
                                    PEKIND                   PeKind,
                                    PEImage                 *pPEImage,
                                    PEImage                 *pNativePEImage,
                                    BOOL                     fInspectionOnly,
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
                                       fInspectionOnly,
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
        HRESULT hr = S_OK;
 
       if (!BINDER_SPACE::fAssemblyBinderInitialized)
        {
            g_BinderVariables = new Variables();
            IF_FAIL_GO(g_BinderVariables->Init());

            // Setup Debug log
            BINDER_LOG_STARTUP();

            // We're done
            BINDER_SPACE::fAssemblyBinderInitialized = TRUE;
        }

    Exit:
        return hr;
    }


    HRESULT AssemblyBinder::TranslatePEToArchitectureType(DWORD  *pdwPAFlags, PEKIND *PeKind)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("TranslatePEToArchitectureType"))

        _ASSERTE(pdwPAFlags != NULL);
        _ASSERTE(PeKind != NULL);

        CorPEKind CLRPeKind = (CorPEKind) pdwPAFlags[0];
        DWORD dwImageType = pdwPAFlags[1];

        *PeKind = peNone;

        if(CLRPeKind == peNot) 
        {
            // Not a PE. Shouldn't ever get here.
            BINDER_LOG(W("Not a PE!"));
            IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_BAD_FORMAT));
        }
        else 
        {
            if ((CLRPeKind & peILonly) && !(CLRPeKind & pe32Plus) &&
                !(CLRPeKind & pe32BitRequired) && dwImageType == IMAGE_FILE_MACHINE_I386) 
            {
                // Processor-agnostic (MSIL)
                BINDER_LOG(W("Processor-agnostic (MSIL)"));
                *PeKind = peMSIL;
            }
            else if (CLRPeKind & pe32Plus) 
            {
                // 64-bit
                if (CLRPeKind & pe32BitRequired) 
                {
                    // Invalid
                    BINDER_LOG(W("CLRPeKind & pe32BitRequired is true"));
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
                    BINDER_LOG(W("Unknown architecture"));
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
                    BINDER_LOG(W("32-bit, non-agnostic"));
                    IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_BAD_FORMAT));
                }
            }
        }

    Exit:
        BINDER_LOG_LEAVE_HR(W("TranslatePEToArchitectureType"), hr);
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
                                         /* out */ Assembly           **ppAssembly)
    {
        HRESULT hr = S_OK;
        LONG kContextVersion = 0;
        BindResult bindResult;
        
        BINDER_LOG_ENTER(W("AssemblyBinder::BindAssembly"));

#ifndef CROSSGEN_COMPILE
    Retry:
       {
            // Lock the binding application context
            CRITSEC_Holder contextLock(pApplicationContext->GetCriticalSectionCookie());
#endif

            if (szCodeBase == NULL)
            {
#ifdef FEATURE_VERSIONING_LOG
                // Log bind
                IF_FAIL_GO(BindingLog::CreateInContext(pApplicationContext,
                                                       pAssemblyName,
                                                       pParentAssembly));
#endif // FEATURE_VERSIONING_LOG


                hr = BindByName(pApplicationContext,
                                      pAssemblyName,
                                      BIND_CACHE_FAILURES,
                                      &bindResult);
                IF_FAIL_GO(hr);
            }
            else
            {
                PathString assemblyPath(szCodeBase);

#ifdef FEATURE_VERSIONING_LOG
                // Log bind
                IF_FAIL_GO(BindingLog::CreateInContext(pApplicationContext,
                                                       assemblyPath,
                                                       pParentAssembly));
#endif // FEATURE_VERSIONING_LOG

                // Convert URL to full path and block HTTP downloads
                IF_FAIL_GO(URLToFullPath(assemblyPath));
                BOOL fDoNgenExplicitBind = fNgenExplicitBind;
                
                // Only use explicit ngen binding in the new coreclr path-based binding model
                if(!pApplicationContext->IsTpaListProvided())
                {
                    fDoNgenExplicitBind = FALSE;
                }
                
                IF_FAIL_GO(BindWhereRef(pApplicationContext,
                                        assemblyPath,
                                        fDoNgenExplicitBind,
                                        fExplicitBindToNativeImage,
                                        &bindResult));
            }

            // Remember the post-bind version
            kContextVersion = pApplicationContext->GetVersion();

        Exit:
#ifdef FEATURE_VERSIONING_LOG
            hr = LogBindResult(pApplicationContext, hr, &bindResult);
#else // FEATURE_VERSIONING_LOG
            ;
#endif // FEATURE_VERSIONING_LOG

#ifndef CROSSGEN_COMPILE
        } // lock(pApplicationContext)
#endif

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

        BINDER_LOG_LEAVE_HR(W("AssemblyBinder::BindAssembly"), hr);
        return hr;
    }

    /* static */
    HRESULT AssemblyBinder::BindToSystem(SString   &systemDirectory,
                                         Assembly **ppSystemAssembly,
                                         bool fBindToNativeImage)
    {
        _ASSERTE(BINDER_SPACE::fAssemblyBinderInitialized == TRUE);

        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("AssemblyBinder:BindToSystem"));

        _ASSERTE(ppSystemAssembly != NULL);

        StackSString sMscorlibDir(systemDirectory);
        ReleaseHolder<Assembly> pSystemAssembly;

        if(!sMscorlibDir.EndsWith(DIRECTORY_SEPARATOR_CHAR_W))
        {
            sMscorlibDir.Append(DIRECTORY_SEPARATOR_CHAR_W);
        }

        StackSString sMscorlib;

        // At run-time, mscorlib.ni.dll is typically always available, and
        // mscorlib.dll is typically not.  So check for the NI first.
        sMscorlib = sMscorlibDir;
        sMscorlib.Append(W("mscorlib.ni.dll"));
        if (!fBindToNativeImage || FAILED(AssemblyBinder::GetAssembly(sMscorlib,
                                               FALSE /* fInspectionOnly */,
                                               TRUE /* fIsInGAC */,
                                               TRUE /* fExplicitBindToNativeImage */,
                                               &pSystemAssembly)))
        {
            // If mscorlib.ni.dll is unavailable, look for mscorlib.dll instead
            sMscorlib = sMscorlibDir;
            sMscorlib.Append(W("mscorlib.dll"));
            IF_FAIL_GO(AssemblyBinder::GetAssembly(sMscorlib,
                                                   FALSE /* fInspectionOnly */,
                                                   TRUE /* fIsInGAC */,
                                                   FALSE /* fExplicitBindToNativeImage */,
                                                   &pSystemAssembly));
        }

        *ppSystemAssembly = pSystemAssembly.Extract();

    Exit:
        BINDER_LOG_LEAVE_HR(W("AssemblyBinder::BindToSystem"), hr);
        return hr;
    }


    /* static */
    HRESULT AssemblyBinder::BindToSystemSatellite(SString   &systemDirectory,
                                                  SString   &simpleName,
                                                  SString   &cultureName,
                                                  Assembly **ppSystemAssembly)
    {
        _ASSERTE(BINDER_SPACE::fAssemblyBinderInitialized == TRUE);

        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("AssemblyBinder:BindToSystemSatellite"));

        _ASSERTE(ppSystemAssembly != NULL);

        StackSString sMscorlibSatellite(systemDirectory);
        ReleaseHolder<Assembly> pSystemAssembly;

        // append culture name
        if (!cultureName.IsEmpty())
        {
            CombinePath(sMscorlibSatellite, cultureName, sMscorlibSatellite);
        }

        // append satellite assembly's simple name
        CombinePath(sMscorlibSatellite, simpleName, sMscorlibSatellite);

        // append extension
        sMscorlibSatellite.Append(W(".dll"));

        IF_FAIL_GO(AssemblyBinder::GetAssembly(sMscorlibSatellite,
                                               FALSE /* fInspectionOnly */,
                                               TRUE /* fIsInGAC */,
                                               FALSE /* fExplicitBindToNativeImage */,
                                               &pSystemAssembly));

        *ppSystemAssembly = pSystemAssembly.Extract();

    Exit:
        BINDER_LOG_LEAVE_HR(W("AssemblyBinder::BindToSystemSatellite"), hr);
        return hr;
    }

    /* static */
    HRESULT AssemblyBinder::GetAssemblyFromImage(PEImage   *pPEImage,
                                                 PEImage   *pNativePEImage,
                                                 Assembly **ppAssembly)
    {
        _ASSERTE(BINDER_SPACE::fAssemblyBinderInitialized == TRUE);

        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("AssemblyBinder::GetAssemblyFromImage"));
        
        _ASSERTE(pPEImage != NULL);
        _ASSERTE(ppAssembly != NULL);

        ReleaseHolder<Assembly> pAssembly;
        ReleaseHolder<IMDInternalImport> pIMetaDataAssemblyImport;
        DWORD dwPAFlags[2];
        PEKIND PeKind = peNone;

        SAFE_NEW(pAssembly, Assembly);
        if(pNativePEImage)
        {
            IF_FAIL_GO(BinderAcquireImport(pNativePEImage, &pIMetaDataAssemblyImport, dwPAFlags, TRUE));
        }
        else
        {
            IF_FAIL_GO(BinderAcquireImport(pPEImage, &pIMetaDataAssemblyImport, dwPAFlags, FALSE));
        }
        IF_FAIL_GO(TranslatePEToArchitectureType(dwPAFlags, &PeKind));
        IF_FAIL_GO(pAssembly->Init(pIMetaDataAssemblyImport,
                                   PeKind,
                                   pPEImage,
                                   pNativePEImage,
                                   g_BinderVariables->emptyString,
                                   FALSE /* fInspectionOnly */,
                                   FALSE /* fIsInGAC */));

        // TODO: Is this correct?
        pAssembly->SetIsByteArray(TRUE);

        *ppAssembly = pAssembly.Extract();

    Exit:
        BINDER_LOG_LEAVE_HR(W("AssemblyBinder::GetAssemblyFromImage"), hr);
        return hr;
    }

#ifndef CROSSGEN_COMPILE
    /* static */
    HRESULT AssemblyBinder::PreBindByteArray(ApplicationContext *pApplicationContext,
                                             PEImage            *pPEImage,
                                             BOOL                fInspectionOnly)
    {
        _ASSERTE(BINDER_SPACE::fAssemblyBinderInitialized == TRUE);

        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("AssemblyBinder::PreBindByteArray"));

        ReleaseHolder<AssemblyName> pAssemblyName;
        BOOL fNeedHostRegister = FALSE;
        LONG kContextVersion = 0;
        ReleaseHolder<IMDInternalImport> pIMetaDataAssemblyImport;
        DWORD dwPAFlags[2];
        PEKIND PeKind = peNone;
        BindResult bindResult;

        // Prepare binding data
        SAFE_NEW(pAssemblyName, AssemblyName);
        IF_FAIL_GO(BinderAcquireImport(pPEImage, &pIMetaDataAssemblyImport, dwPAFlags, FALSE));
        IF_FAIL_GO(TranslatePEToArchitectureType(dwPAFlags, &PeKind));
        IF_FAIL_GO(pAssemblyName->Init(pIMetaDataAssemblyImport, PeKind));
        pAssemblyName->SetIsDefinition(TRUE);

        // Validate architecture
        if (!fInspectionOnly && !Assembly::IsValidArchitecture(pAssemblyName->GetArchitecture()))
        {
            IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_BAD_FORMAT));
        }

        // Attempt the actual bind (eventually more than once)
    Retry:
        {
            // Lock the application context
            CRITSEC_Holder contextLock(pApplicationContext->GetCriticalSectionCookie());

            // Attempt uncached bind and register stream if possible
            if (!fInspectionOnly &&
                 ((hr = BindByName(pApplicationContext,
                                   pAssemblyName,
                                   BIND_CACHE_FAILURES | BIND_CACHE_RERUN_BIND,
                                   &bindResult)) == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
                )
            {
                if ((fInspectionOnly && !bindResult.HaveResult()) ||
                    (bindResult.GetRetargetedAssemblyName() == NULL))
                {
                    IF_FAIL_GO(CreateImageAssembly(pIMetaDataAssemblyImport,
                                                   PeKind,
                                                   pPEImage,
                                                   NULL,
                                                   fInspectionOnly,
                                                   &bindResult));
                    if (fInspectionOnly)
                    {
                        // For inspection-only, we do not share the map.
                        IF_FAIL_GO(Register(pApplicationContext, fInspectionOnly, &bindResult));
                    }
                    else
                    {
                        // Remember the post-bind version of the context
                        kContextVersion = pApplicationContext->GetVersion();
                        fNeedHostRegister = TRUE;
                    }
                }
            }
        } // lock(pApplicationContext)

        if (fNeedHostRegister)
        {
            BindResult hostBindResult;

            // This has to happen outside the binder lock as it can cause new binds
            IF_FAIL_GO(RegisterAndGetHostChosen(pApplicationContext,
                                                kContextVersion,
                                                &bindResult,
                                                &hostBindResult));

            if (hr == S_FALSE)
            {
                // Another bind interfered. We need to retry entire bind.
                // This by design loops as long as needed because by construction we eventually
                // will succeed or fail the bind.
                bindResult.Reset();
                goto Retry;
            }
        }
    
        // Ignore bind errors here because we need to attempt by-name load to get log entry.
        GO_WITH_HRESULT(S_OK);

    Exit:
        BINDER_LOG_LEAVE_HR(W("AssemblyBinder::PreBindByteArray"), hr);
        return hr;
    }

#endif //CROSSGEN_COMPILE

    /* static */
    HRESULT AssemblyBinder::BindByName(ApplicationContext *pApplicationContext,
                                       AssemblyName       *pAssemblyName,
                                       DWORD               dwBindFlags,
                                       BindResult         *pBindResult)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("AssemblyBinder::BindByName"));
        PathString assemblyDisplayName;
        ReleaseHolder<AssemblyName> pRetargetedAssemblyName;
        BOOL fIsRetargeted = FALSE;

        // Apply retargeting
        IF_FAIL_GO(Compatibility::Retarget(pAssemblyName, &pRetargetedAssemblyName, &fIsRetargeted));

        // Look for already cached binding failure (ignore PA, every PA will lock the context)
        pRetargetedAssemblyName->GetDisplayName(assemblyDisplayName,
                                                AssemblyName::INCLUDE_VERSION);

        hr = pApplicationContext->GetFailureCache()->Lookup(assemblyDisplayName);
        if (FAILED(hr))
        {
            if ((hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND)) && RerunBind(dwBindFlags))
            {
                // Ignore pre-existing transient bind error (re-bind will succeed)
                pApplicationContext->GetFailureCache()->Remove(assemblyDisplayName);
            }

            goto LogExit;
        }
        else if (hr == S_FALSE)
        {
            // workaround: Special case for byte arrays. Rerun the bind to create binding log.
            pRetargetedAssemblyName->SetIsDefinition(TRUE);
            hr = S_OK;
        }

        if (!Assembly::IsValidArchitecture(pRetargetedAssemblyName->GetArchitecture()))
        {
            // Assembly reference contains wrong architecture
            IF_FAIL_GO(FUSION_E_INVALID_NAME);
        }

       IF_FAIL_GO(BindLocked(pApplicationContext,
                              pRetargetedAssemblyName,
                              dwBindFlags,
                              pBindResult));

        if (!pBindResult->HaveResult())
        {
            // Behavior rules are clueless now
            IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND));
        }

    Exit:
        if (FAILED(hr) && CacheBindFailures(dwBindFlags))
        {
            if (RerunBind(dwBindFlags))
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

        BINDER_LOG_LEAVE_HR(W("AssemblyBinder::BindByName"), hr);
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
                                         BindResult         *pBindResult)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("AssemblyBinder::BindWhereRef"));

        ReleaseHolder<Assembly> pAssembly;
        ReleaseHolder<AssemblyName> pRetargetedAssemblyName;
        BOOL fIsRetargeted = FALSE;
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
                               FALSE /* fInspectionOnly */,
                               FALSE /* fIsInGAC */,
                               
                               // Pass through caller's intent of whether to bind to the
                               // NI using an explicit path to the NI that was
                               // specified.  Generally only NGEN PDB generation has
                               // this TRUE.
                               fExplicitBindToNativeImage,
                               &pAssembly));
#ifdef FEATURE_VERSIONING_LOG
        IF_FAIL_GO(LogAssemblyNameWhereRef(pApplicationContext, pAssembly));
#endif // FEATURE_VERSIONING_LOG

        AssemblyName *pAssemblyName;
        pAssemblyName = pAssembly->GetAssemblyName();

        IF_FAIL_GO(Compatibility::Retarget(pAssemblyName, &pRetargetedAssemblyName, &fIsRetargeted));

        if (!fNgenExplicitBind)
        {
            IF_FAIL_GO(BindLockedOrService(pApplicationContext,
                                           pRetargetedAssemblyName,
                                           &lockedBindResult));
            if (lockedBindResult.HaveResult())
            {
                pBindResult->SetResult(&lockedBindResult);
                GO_WITH_HRESULT(S_OK);
            }

            else if (fIsRetargeted)
            {
                // We did not find retargeted assembly.
                // Check that assembly we have does match (must have retargetable property set).
                ReleaseHolder<AssemblyName> pClonedAssemblyName;

                IF_FAIL_GO(pRetargetedAssemblyName->Clone(&pClonedAssemblyName));
                pClonedAssemblyName->SetIsRetargetable(TRUE);

                if (!pClonedAssemblyName->Equals(pAssemblyName, AssemblyName::INCLUDE_VERSION))
                {
                    IF_FAIL_GO(FUSION_E_REF_DEF_MISMATCH);
                }
            }
        }

        hr = S_OK;
        pAssembly->SetIsDynamicBind(TRUE);
        pBindResult->SetResult(pAssembly);

    Exit:

        if (FAILED(hr))
        {
            // Always cache binding failures
            hr = pApplicationContext->AddToFailureCache(assemblyPath, hr);
        }

    LogExit:

        BINDER_LOG_LEAVE_HR(W("AssemblyBinder::BindWhereRef"), hr);
        return hr;
    }

    /* static */
    HRESULT AssemblyBinder::BindLocked(ApplicationContext *pApplicationContext,
                                       AssemblyName       *pAssemblyName,
                                       DWORD               dwBindFlags,
                                       BindResult         *pBindResult)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("AssemblyBinder::BindLocked"));
        
        BOOL fIgnoreDynamicBinds = IgnoreDynamicBinds(dwBindFlags);
        
#ifndef CROSSGEN_COMPILE
        ContextEntry *pContextEntry = NULL;
        IF_FAIL_GO(FindInExecutionContext(pApplicationContext, pAssemblyName, &pContextEntry));
        if (pContextEntry != NULL)
        {
            if (fIgnoreDynamicBinds && pContextEntry->GetIsDynamicBind())
            {
                // Dynamic binds need to be always considered a failure for binding closures
                IF_FAIL_GO(FUSION_E_APP_DOMAIN_LOCKED);
            }
#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE) && !defined(MDILNIGEN)
            else if (IgnoreRefDefMatch(dwBindFlags))
            {
                // Skip RefDef matching if we have been asked to.
            }
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE) && !defined(MDILNIGEN)            
            else
            {
                // Can't give higher serciving than already bound
                IF_FAIL_GO(IsValidAssemblyVersion(pAssemblyName,
                                                  pContextEntry->GetAssemblyName(),
                                                  pApplicationContext,
                                                  !!pContextEntry->GetIsInGAC()));
            }
            
            pBindResult->SetResult(pContextEntry);
        }
        else
#endif // !CROSSGEN_COMPILE
        if (pApplicationContext->IsTpaListProvided())
        {
            bool fUnifiedAppAssemblyToPlatform = false;
            IF_FAIL_GO(BindByTpaList(pApplicationContext,
                                     pAssemblyName,
                                     FALSE /*fInspectionOnly*/,
                                     pBindResult,
                                     &fUnifiedAppAssemblyToPlatform));
            if (pBindResult->HaveResult())
            {
                bool fBeingBoundToPlatformAssembly = !!pBindResult->GetIsInGAC();
                if (fUnifiedAppAssemblyToPlatform)
                {
                    // Pretend as if we have bound to an app assembly since the RequestedAssemblyName came in for an app
                    // assembly and thus, may not contain a version number.
                    fBeingBoundToPlatformAssembly = false;
                }
                
                hr = IsValidAssemblyVersion(pAssemblyName, pBindResult->GetAssemblyName(), pApplicationContext, fBeingBoundToPlatformAssembly);
                if (FAILED(hr))
                {
                    pBindResult->SetNoResult();                    
                }
            }
        }
    Exit:
        BINDER_LOG_LEAVE_HR(W("AssemblyBinder::BindLocked"), hr);
        return hr;
    }

    /* static */
    HRESULT AssemblyBinder::BindLockedOrService(ApplicationContext *pApplicationContext,
                                                AssemblyName       *pAssemblyName,
                                                BindResult         *pBindResult)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("AssemblyBinder::BindLockedOrService"));

        BindResult lockedBindResult;

        IF_FAIL_GO(BindLocked(pApplicationContext,
                              pAssemblyName,
                              0 /*  Do not IgnoreDynamicBinds */,
                              &lockedBindResult));

        if (lockedBindResult.HaveResult())
        {
            // Locked Bind succeeded
            pBindResult->SetResult(&lockedBindResult);
        }
        else
        {
            pBindResult->SetNoResult();
        }

    Exit:
        BINDER_LOG_LEAVE_HR(W("AssemblyBinder::BindLockedOrService"), hr);
        return hr;
    }

#ifndef CROSSGEN_COMPILE
    /* static */
    HRESULT AssemblyBinder::FindInExecutionContext(ApplicationContext  *pApplicationContext,
                                                   AssemblyName        *pAssemblyName,
                                                   ContextEntry       **ppContextEntry)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("AssemblyBinder::FindInExecutionContext"));

        _ASSERTE(pApplicationContext != NULL);
        _ASSERTE(pAssemblyName != NULL);
        _ASSERTE(ppContextEntry != NULL);

        ExecutionContext *pExecutionContext = pApplicationContext->GetExecutionContext();
        ContextEntry *pContextEntry = pExecutionContext->Lookup(pAssemblyName);

        if (pContextEntry != NULL)
        {
            AssemblyName *pContextName = pContextEntry->GetAssemblyName();

#ifdef FEATURE_VERSIONING_LOG
            // First-time requests are considered unlocked, everything else is locked
            DWORD dwLockedReason = (pContextEntry->GetIsFirstRequest() ?
                                    APP_DOMAIN_LOCKED_UNLOCKED : APP_DOMAIN_LOCKED_CONTEXT);

            IF_FAIL_GO(LogAppDomainLocked(pApplicationContext, dwLockedReason, pContextName));
            pContextEntry->SetIsFirstRequest(FALSE);
#endif // FEATURE_VERSIONING_LOG

            if (pAssemblyName->GetIsDefinition() &&
                (pContextName->GetArchitecture() != pAssemblyName->GetArchitecture()))
            {
                IF_FAIL_GO(FUSION_E_APP_DOMAIN_LOCKED);
            }
        }

        *ppContextEntry = pContextEntry;

    Exit:
        BINDER_LOG_LEAVE_HR(W("AssemblyBinder::FindInExecutionContext"), hr);
        return hr;
    }

#endif //CROSSGEN_COMPILE

    //
    // Tests whether a candidate assembly's name matches the requested.
    // This does not do a version check.  The binder applies version policy
    // further up the stack once it gets a successful bind.
    //
    BOOL TestCandidateRefMatchesDef(ApplicationContext *pApplicationContext,
                                    AssemblyName *pRequestedAssemblyName,
                                    AssemblyName *pBoundAssemblyName,
                                    BOOL tpaListAssembly)
    {
        DWORD dwIncludeFlags = AssemblyName::INCLUDE_DEFAULT;

        bool fWindowsPhone7 = false;
#ifdef FEATURE_LEGACYNETCF
        fWindowsPhone7 = RuntimeIsLegacyNetCF(pApplicationContext->GetAppDomainId()) == TRUE;
#endif
        //
        // Windows Phone 7 Quirk:
        //
        // NetCF allows partial binds to platform assemblies.  If we are running a
        // Mango application, skip the PKT check if no Ref version is provided,
        // since there are apps in the Marketplace that do Assembly.Load("System")
        //
        if (!tpaListAssembly || (fWindowsPhone7 && tpaListAssembly))
        {
            dwIncludeFlags |= AssemblyName::EXCLUDE_PUBLIC_KEY_TOKEN_IF_MISSING;

            //
            // On Windows Phone 7, exclude culture comparisons when requesting an uncultured
            // assembly for app compat reasons (there are main app assemblies with spurious cultures)
            //
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

        BINDER_LOG_ASSEMBLY_NAME(W("pBoundAssemblyName"), pBoundAssemblyName);

        return pBoundAssemblyName->Equals(pRequestedAssemblyName, dwIncludeFlags);
    }
    
    HRESULT BindSatelliteResourceByResourceRoots(ApplicationContext  *pApplicationContext,
                                          StringArrayList     *pResourceRoots,
                                          AssemblyName        *pRequestedAssemblyName,
                                          BOOL                 fInspectionOnly,
                                          BindResult          *pBindResult)
    {
        HRESULT hr = S_OK;

        SString &simpleNameRef = pRequestedAssemblyName->GetSimpleName();
        SString &cultureRef = pRequestedAssemblyName->GetCulture();
        
        _ASSERTE(!cultureRef.IsEmpty() && !cultureRef.EqualsCaseInsensitive(g_BinderVariables->cultureNeutral));
        
        for (UINT i = 0; i < pResourceRoots->GetCount(); i++)
        {
            ReleaseHolder<Assembly> pAssembly;
            SString &wszBindingPath = (*pResourceRoots)[i];
            SString fileName(wszBindingPath);

            CombinePath(fileName, cultureRef, fileName);
            CombinePath(fileName, simpleNameRef, fileName);
            fileName.Append(W(".dll"));

            hr = AssemblyBinder::GetAssembly(fileName,
                                             fInspectionOnly,
                                             FALSE /* fIsInGAC */,
                                             FALSE /* fExplicitBindToNativeImage */,
                                             &pAssembly);

            // Missing files are okay and expected when probing
            if (hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
            {
                continue;
            }
            
            IF_FAIL_GO(hr);

            AssemblyName *pBoundAssemblyName = pAssembly->GetAssemblyName();
            if (TestCandidateRefMatchesDef(pApplicationContext, pRequestedAssemblyName, pBoundAssemblyName, false /*tpaListAssembly*/))
            {
                pBindResult->SetResult(pAssembly);
                GO_WITH_HRESULT(S_OK);
            }

#ifdef FEATURE_VERSIONING_LOG
            // Log the candidates we throw out for diagnostics
            IF_FAIL_GO(LogConfigurationError(pApplicationContext,
                                             pRequestedAssemblyName,
                                             pBoundAssemblyName));
#endif // FEATURE_VERSIONING_LOG
            
            IF_FAIL_GO(FUSION_E_REF_DEF_MISMATCH);
        }

        // Up-stack expects S_OK when we don't find any candidate assemblies and no fatal error occured (ie, no S_FALSE)
        hr = S_OK;
    Exit:
        return hr;
    }
    
    /*
     * BindByTpaList is the entry-point for the custom binding algorithm on the Phone.
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
                                          BOOL                 fInspectionOnly,
                                          BindResult          *pBindResult,
                                          bool                *pfUnifiedAppAssemblyToPlatform)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("AssemblyBinder::BindByTpaList"));

        SString &culture = pRequestedAssemblyName->GetCulture();
        bool fPartialMatchOnTpa = false;
        
        // Did we unify an app assembly (with same SimpleName+Culture+PKT) to a platform (TPA) assembly?
        *pfUnifiedAppAssemblyToPlatform = false;
        
        if (!culture.IsEmpty() && !culture.EqualsCaseInsensitive(g_BinderVariables->cultureNeutral))
        {
            //
            // Satellite resource probing strategy is to look under each of the Platform Resource Roots
            // followed by App Paths.
            //
            
            hr = BindSatelliteResourceByResourceRoots(pApplicationContext, 
                                                            pApplicationContext->GetPlatformResourceRoots(), 
                                                            pRequestedAssemblyName, 
                                                            fInspectionOnly, 
                                                            pBindResult);
            
            // We found a platform resource file with matching file name, but whose ref-def didn't match.  Fall
            // back to application resource lookup to handle case where a user creates resources with the same
            // names as platform ones.
            if (hr != FUSION_E_CONFIGURATION_ERROR)
            {
                IF_FAIL_GO(hr);
            }
            
            if (!pBindResult->HaveResult())
            {
                IF_FAIL_GO(BindSatelliteResourceByResourceRoots(pApplicationContext, 
                                                                pApplicationContext->GetAppPaths(), 
                                                                pRequestedAssemblyName, 
                                                                fInspectionOnly, 
                                                                pBindResult));
            }
        }
        else
        {
            // Is assembly on TPA list?
            SString &simpleName = pRequestedAssemblyName->GetSimpleName();
            SimpleNameToFileNameMap * tpaMap = pApplicationContext->GetTpaList();
            const SimpleNameToFileNameMapEntry *pTpaEntry = tpaMap->LookupPtr(simpleName.GetUnicode());
            ReleaseHolder<Assembly> pTPAAssembly;
            if (pTpaEntry != nullptr)
            {
                if (pTpaEntry->m_wszNIFileName != nullptr)
                {
                    SString fileName(pTpaEntry->m_wszNIFileName);

                    // A GetAssembly overload perhaps, or just another parameter to the existing method
                    hr = GetAssembly(fileName,
                                        fInspectionOnly,
                                        TRUE, /* fIsInGAC */
                                        TRUE /* fExplicitBindToNativeImage */,
                                        &pTPAAssembly
                                        );
                }
                else
                {
                    _ASSERTE(pTpaEntry->m_wszILFileName != nullptr);
                    SString fileName(pTpaEntry->m_wszILFileName);
                    
                    hr = GetAssembly(fileName,
                                        fInspectionOnly,
                                        TRUE, /* fIsInGAC */
                                        FALSE /* fExplicitBindToNativeImage */,
                                        &pTPAAssembly);
                }

                // On file not found, simply fall back to app path probing
                if (hr != HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
                {
                    // Any other error is fatal
                    IF_FAIL_GO(hr);
                    
                    if (TestCandidateRefMatchesDef(pApplicationContext, pRequestedAssemblyName, pTPAAssembly->GetAssemblyName(), true /*tpaListAssembly*/))
                    {
                        // We have found the requested assembly match on TPA with validation of the full-qualified name. Bind to it.
                        pBindResult->SetResult(pTPAAssembly);
                        GO_WITH_HRESULT(S_OK);
                    }
                    else
                    {
                        // We found the assembly on TPA but it didnt match the RequestedAssembly assembly-name. In this case, lets proceed to see if we find the requested
                        // assembly in the App paths.
                        fPartialMatchOnTpa = true;
                    }
                }
                
                // We either didn't find a candidate, or the ref-def failed.  Either way; fall back to app path probing.
            }

            bool fUseAppPathsBasedResolver = true;
            
#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE) && !defined(MDILNIGEN)           
            // If Host Assembly Resolver is specified, then we will use that as the override for the default resolution mechanism (that uses AppPath probing).
            if (!RuntimeCanUseAppPathAssemblyResolver(pApplicationContext->GetAppDomainId()))
            {
                fUseAppPathsBasedResolver = false;
            }
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE) && !defined(MDILNIGEN)
             
            // This loop executes twice max.  First time through we probe AppNiPaths, the second time we probe AppPaths
            bool parseNiPaths = true;
            while (fUseAppPathsBasedResolver)
            {
                StringArrayList *pBindingPaths = parseNiPaths ? pApplicationContext->GetAppNiPaths() : pApplicationContext->GetAppPaths();
                
                // Loop through the binding paths looking for a matching assembly
                for (DWORD i = 0; i < pBindingPaths->GetCount(); i++)
                {
                    ReleaseHolder<Assembly> pAssembly;
                    LPCWSTR wszBindingPath = (*pBindingPaths)[i];
                    
                    SString &simpleName = pRequestedAssemblyName->GetSimpleName();

                    // Look for a matching dll first
                    hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
                    
                    {
                        SString fileName(wszBindingPath);
                        CombinePath(fileName, simpleName, fileName);
                        if (parseNiPaths)
                        {
                            fileName.Append(W(".ni.dll"));
                            hr = GetAssembly(fileName,
                                            fInspectionOnly,
                                            FALSE, /* fIsInGAC */
                                            TRUE /* fExplicitBindToNativeImage */,
                                            &pAssembly);
                        }
                        else
                        {
                            if (FAILED(hr))
                            {
                                fileName.Append(W(".dll"));

                                hr = GetAssembly(fileName,
                                                fInspectionOnly,
                                                FALSE, /* fIsInGAC */
                                                FALSE /* fExplicitBindToNativeImage */,
                                                &pAssembly);
                            }
                        }
                    }

                    if (FAILED(hr))
                    {
                        SString fileName(wszBindingPath);
                        CombinePath(fileName, simpleName, fileName);

                        if (parseNiPaths)
                        {
                            fileName.Append(W(".ni.exe"));
                            hr = GetAssembly(fileName,
                                            fInspectionOnly,
                                            FALSE, /* fIsInGAC */
                                            TRUE /* fExplicitBindToNativeImage */,
                                            &pAssembly);
                        }
                        else
                        {
                            if (FAILED(hr))
                            {
                                fileName.Append(W(".exe"));

                                hr = GetAssembly(fileName,
                                                fInspectionOnly,
                                                FALSE, /* fIsInGAC */
                                                FALSE /* fExplicitBindToNativeImage */,
                                                &pAssembly);
                            }
                        }
                    }
                    
                    // Since we're probing, file not founds are ok and we should just try another
                    // probing path
                    if (hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
                    {
                        continue;
                    }
                    IF_FAIL_GO(hr);

                    // We found a candidate.  
                    //
                    // Below this point, we either establish that the ref-def matches, or
                    // we fail the bind.

                    // Compare requested AssemblyName with that from the candidate assembly 
                    if (TestCandidateRefMatchesDef(pApplicationContext, pRequestedAssemblyName, pAssembly->GetAssemblyName(), false /*tpaListAssembly*/))
                    {
                        // At this point, we have found an assembly with the expected name in the App paths. If this was also found on TPA,
                        // make sure that the app assembly has the same fullname (excluding version) as the TPA version. If it does, then
                        // we should bind to the TPA assembly. If it does not, then bind to the app assembly since it has a different fullname than the 
                        // TPA assembly.
                        if (fPartialMatchOnTpa)
                        {
                            if (TestCandidateRefMatchesDef(pApplicationContext, pAssembly->GetAssemblyName(), pTPAAssembly->GetAssemblyName(), true /*tpaListAssembly*/))
                            {
                                // Fullname (SimpleName+Culture+PKT) matched for TPA and app assembly - so bind to TPA instance.
                                *pfUnifiedAppAssemblyToPlatform = true;
                                pBindResult->SetResult(pTPAAssembly);
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
                            // We didnt see this assembly on TPA - so simply bind to the app instance.
                            pBindResult->SetResult(pAssembly);
                            GO_WITH_HRESULT(S_OK);
                        }
                    }

        #ifdef FEATURE_VERSIONING_LOG
                    // Log the candidates we throw out for diagnostics
                    IF_FAIL_GO(LogConfigurationError(pApplicationContext,
                                                     pRequestedAssemblyName,
                                                     pAssembly->GetAssemblyName()));
        #endif // FEATURE_VERSIONING_LOG
        
                    IF_FAIL_GO(FUSION_E_REF_DEF_MISMATCH);

                }
                
                if (!parseNiPaths)
                {
                    break;
                }
                
                parseNiPaths = false;
            }
        }
        
        // Couldn't find a matching assembly in any of the probing paths
        // Return S_OK here.  BindByName will interpret a lack of BindResult
        // as a failure to find a matching assembly.  Other callers of this
        // function, such as BindLockedOrService will interpret as deciding
        // not to override an explicit bind with a probed assembly
        hr = S_OK;
        
    Exit:
        BINDER_LOG_LEAVE_HR(W("AssemblyBinder::BindByTpaList"), hr);
        return hr;  
    }
    
    /* static */
    HRESULT AssemblyBinder::GetAssembly(SString     &assemblyPath,
                                        BOOL         fInspectionOnly,
                                        BOOL         fIsInGAC,
                                        
                                        // When binding to the native image, should we
                                        // assume assemblyPath explicitly specifies that
                                        // NI?  (If not, infer the path to the NI
                                        // implicitly.)
                                        BOOL         fExplicitBindToNativeImage,
                                        
                                        Assembly   **ppAssembly,

                                        // If assemblyPath refers to a native image without metadata,
                                        // szMDAssemblyPath gives the alternative file to get metadata.
                                        LPCTSTR      szMDAssemblyPath)
    {
        HRESULT hr = S_OK;

        BINDER_LOG_ENTER(W("Assembly::GetAssembly"));
        BINDER_LOG_STRING(W("assemblyPath"), assemblyPath);

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

            BINDER_LOG_ENTER(W("BinderAcquirePEImage"));
            hr = BinderAcquirePEImage(szAssemblyPath, &pPEImage, &pNativePEImage, fExplicitBindToNativeImage);
            BINDER_LOG_LEAVE_HR(W("BinderAcquirePEImage"), hr);
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
                     pPEImage = pNativePEImage;
                     pNativePEImage = NULL;
                }
            }

            BINDER_LOG_ENTER(W("BinderAcquireImport"));
            if(pNativePEImage)
                hr = BinderAcquireImport(pNativePEImage, &pIMetaDataAssemblyImport, dwPAFlags, TRUE);
            else
                hr = BinderAcquireImport(pPEImage, &pIMetaDataAssemblyImport, dwPAFlags, FALSE);

            BINDER_LOG_LEAVE_HR(W("BinderAcquireImport"), hr);
            IF_FAIL_GO(hr);

            if (pIMetaDataAssemblyImport == NULL && pNativePEImage != NULL)
            {
                // The native image doesn't contain metadata. Currently this is only supported for Windows.ni.winmd.
                // While loading Windows.winmd, CLRPrivBinderWinRT::GetAssemblyAndTryFindNativeImage should have passed
                // in a non-NULL szMDAssemblyPath, where we can load metadata from. If szMDAssemblyPath is NULL,
                // it indicates that the app is trying to load a non-WinMD assembly named Windows (it's possible
                // that the app happens to contain an assembly Windows.dll). To handle this case properly, we
                // return a file-not-found error, so the caller can continues it's search.
                if (szMDAssemblyPath == NULL)
                {
                    IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND));
                }
                else
                {
                    BINDER_LOG_ENTER(W("BinderAcquirePEImage"));
                    hr = BinderAcquirePEImage(szMDAssemblyPath, &pPEImage, NULL, FALSE);
                    BINDER_LOG_LEAVE_HR(W("BinderAcquirePEImage"), hr);
                    IF_FAIL_GO(hr);

                    BINDER_LOG_ENTER(W("BinderAcquireImport"));
                    hr = BinderAcquireImport(pPEImage, &pIMetaDataAssemblyImport, dwPAFlags, FALSE);
                    BINDER_LOG_LEAVE_HR(W("BinderAcquireImport"), hr);
                    IF_FAIL_GO(hr);
                }
            }

            IF_FAIL_GO(TranslatePEToArchitectureType(dwPAFlags, &PeKind));
        }

       // Initialize assembly object
        IF_FAIL_GO(pAssembly->Init(pIMetaDataAssemblyImport,
                                   PeKind,
                                   pPEImage,
                                   pNativePEImage,
                                   assemblyPath,
                                   fInspectionOnly,
                                   fIsInGAC));

        // We're done
        *ppAssembly = pAssembly.Extract();

    Exit:

        BinderReleasePEImage(pPEImage);
        BinderReleasePEImage(pNativePEImage);
        
        BINDER_LOG_LEAVE_HR(W("AssemblyBinder::GetAssembly"), hr);

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
                                     BOOL                fInspectionOnly,
                                     BindResult         *pBindResult)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("AssemblyBinder::Register"));

        if (!pBindResult->GetIsContextBound())
        {
            pApplicationContext->IncrementVersion();

            if (fInspectionOnly)
            {
                InspectionContext *pInspectionContext = pApplicationContext->GetInspectionContext();
                IF_FAIL_GO(pInspectionContext->Register(pBindResult));
            }
            else
            {
                // Register the bindResult in the ExecutionContext only if we dont have it already.
                // This method is invoked under a lock (by its caller), so we are thread safe.
                ContextEntry *pContextEntry = NULL;
                hr = FindInExecutionContext(pApplicationContext, pBindResult->GetAssemblyName(), &pContextEntry);
                if (hr == S_OK)
                {
                    if (pContextEntry == NULL)
                    {
                        ExecutionContext *pExecutionContext = pApplicationContext->GetExecutionContext();
                        IF_FAIL_GO(pExecutionContext->Register(pBindResult));
                    }
                    else
                    {
                        // The dynamic binds are compiled in CoreCLR, but they are not supported. They are only reachable by internal API Assembly.Load(byte[]) that nobody should be calling.
                        // This code path does not handle dynamic binds correctly (and is not expected to). We do not expect to come here for dynamic binds.

                        _ASSERTE(!pContextEntry->GetIsDynamicBind());
                        
                        // Update the BindResult with the contents of the ContextEntry we found
                        pBindResult->SetResult(pContextEntry);
                    }
                }
            }
        }

    Exit:
        BINDER_LOG_LEAVE_HR(W("AssemblyBinder::Register"), hr);
        return hr;
    }

    /* static */
    HRESULT AssemblyBinder::RegisterAndGetHostChosen(ApplicationContext *pApplicationContext,
                                                     LONG                kContextVersion,
                                                     BindResult         *pBindResult,
                                                     BindResult         *pHostBindResult)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("AssemblyBinder::RegisterHostChosen"));

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
                                    FALSE /* fInspectionOnly */,
                                    pHostBindResult));
            }
        }
        else
        {
            // No work required. Return the input
            pHostBindResult->SetResult(pBindResult);
        }

    Exit:
        BINDER_LOG_LEAVE_HR(W("AssemblyBinder::RegisterHostChosen"), hr);
        return hr;
    }

    /* static */
    HRESULT AssemblyBinder::OtherBindInterfered(ApplicationContext *pApplicationContext,
                                                BindResult         *pBindResult)
    {
        HRESULT hr = S_FALSE;
        BINDER_LOG_ENTER(W("AssemblyBinder::OtherBindInterfered"));
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

            if ((hr == S_OK) && (pContextEntry == NULL))
            {
                // We can accept this bind in the domain
                GO_WITH_HRESULT(S_OK);
            }
        }

        // Some other bind interfered
        GO_WITH_HRESULT(S_FALSE);

    Exit:
        BINDER_LOG_LEAVE_HR(W("AssemblyBinder::OtherBindInterfered"), hr);
        return hr;
    }

#endif //CROSSGEN_COMPILE

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE) && !defined(MDILNIGEN)
HRESULT AssemblyBinder::BindUsingHostAssemblyResolver (/* in */ CLRPrivBinderAssemblyLoadContext *pLoadContextToBindWithin,
                                                       /* in */ AssemblyName       *pAssemblyName,
                                                      /* in */ IAssemblyName      *pIAssemblyName,
                                                      /* out */ Assembly           **ppAssembly)
{
    HRESULT hr = E_FAIL;
    BINDER_LOG_ENTER(W("AssemblyBinder::BindUsingHostAssemblyResolver"));
    
    _ASSERTE(pLoadContextToBindWithin != NULL);
    
    // Get the application context within which the assembly will be bound and loaded
    ApplicationContext *pApplicationContext = pLoadContextToBindWithin->GetAppContext();
    _ASSERTE(pApplicationContext != NULL);
    
    // Call into the VM to use the HostAssemblyResolver and load the assembly
    ICLRPrivAssembly *pLoadedAssembly = NULL;
    hr = RuntimeInvokeHostAssemblyResolver(pLoadContextToBindWithin, pIAssemblyName, &pLoadedAssembly);
    if (SUCCEEDED(hr))
    {
        _ASSERTE(pLoadedAssembly != NULL);
        *ppAssembly = static_cast<Assembly *>(pLoadedAssembly);
    }

    BINDER_LOG_LEAVE_HR(W("AssemblyBinder::BindUsingHostAssemblyResolver"), hr);
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
    BINDER_LOG_ENTER(W("AssemblyBinder::BindUsingPEImage"));
    
    _ASSERTE(BINDER_SPACE::fAssemblyBinderInitialized == TRUE);

    LONG kContextVersion = 0;
    BindResult bindResult;

    // Prepare binding data
    *ppAssembly = NULL;
    
    // Attempt the actual bind (eventually more than once)
Retry:
    {
        // Lock the application context
        CRITSEC_Holder contextLock(pApplicationContext->GetCriticalSectionCookie());
        
        // Attempt uncached bind and register stream if possible
        hr = BindByName(pApplicationContext,
                               pAssemblyName,
                               BIND_CACHE_FAILURES|BIND_CACHE_RERUN_BIND|BIND_IGNORE_REFDEF_MATCH,
                               &bindResult);
        
        if (hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
        {
            IF_FAIL_GO(CreateImageAssembly(pIMetaDataAssemblyImport,
                                           peKind,
                                           pPEImage,
                                           NULL,
                                           FALSE,
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
                
                if (incomingMVID != boundMVID)
                {
                    // MVIDs do not match, so fail the load.
                    IF_FAIL_GO(COR_E_FILELOAD);
                }
                
                // MVIDs match - request came in for the same assembly that was previously loaded.
                // Let is through...
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
        
    BINDER_LOG_LEAVE_HR(W("AssemblyBinder::BindUsingPEImage"), hr);
    return hr;
}
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE) && !defined(MDILNIGEN)
};



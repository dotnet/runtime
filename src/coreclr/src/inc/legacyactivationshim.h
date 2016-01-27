// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// LegacyActivationShim.h
//
// This file allows simple migration from .NET Runtime v2 Host Activation APIs
// to the .NET Runtime v4 Host Activation APIs through simple shim functions.

#ifndef __LEGACYACTIVATIONSHIM_H__
#define __LEGACYACTIVATIONSHIM_H__

#pragma warning(push)
#pragma warning(disable:4127) // warning C4127: conditional expression is constant
                              // caused by IfHrFailRet's while(0) code.
#pragma warning(disable:4917) // a GUID can only be associated with a class, interface or namespace 
#pragma warning(disable:4191) // 'reinterpret_cast' : unsafe conversion from 'FARPROC' to 'XXX' 

#ifdef _MANAGED
// We are compiling Managed C++, switch to native code then (and store current managed/native status on the stack)
#pragma managed(push, off)
#endif //_MANAGED

#include "mscoree.h"
#include "metahost.h"

#include "wchar.h"

#include "corerror.h"

// To minimize how much we perturb sources that we are included in, we make sure that
// all macros we define/redefine are restored at the end of the header.
#pragma push_macro("IfHrFailRet")
#pragma push_macro("IfHrFailRetFALSE")
#pragma push_macro("IfHrFailRetVOID")

// ---IfHrFailRet------------------------------------------------------------------------------------
#undef IfHrFailRet
#undef IfHrFailRetFALSE
#undef IfHrFailRetVOID
#define IfHrFailRet(EXPR) do { hr = (EXPR); if(FAILED(hr)) { return (hr); } } while (0)
#define IfHrFailRetFALSE(EXPR) do { HRESULT _hr_ = (EXPR); if(FAILED(_hr_)) { return false; } } while (0)
#define IfHrFailRetVOID(EXPR) do { HRESULT _hr_ = (EXPR); if(FAILED(_hr_)) { return; } } while (0)

#include "legacyactivationshimutil.h"

// Use of deprecated APIs within LegacyActivationShim namespace will result in C4996 that we will
// disable for our own use.
#pragma warning(push)
#pragma warning(disable:4996)
// ---LEGACYACTIVATONSHIM NAMESPACE----------------------------------------------------------------
namespace LegacyActivationShim
{
    // ---HELPERS----------------------------------------------------------------------------------
#define GET_CLRMETAHOST(x) \
    ICLRMetaHost *x = NULL; \
    IfHrFailRet(Util::GetCLRMetaHost(&x))

#define GET_CLRMETAHOSTPOLICY(x) \
    ICLRMetaHostPolicy*x = NULL; \
    IfHrFailRet(Util::GetCLRMetaHostPolicy(&x))

#define GET_CLRINFO(x) \
    ICLRRuntimeInfo *x = NULL; \
    IfHrFailRet(Util::GetCLRRuntimeInfo(&x))

#define LEGACY_API_PASS_THROUGH_STATIC(_name, _ret_type, _ret_value, _sig, _args)       \
    {                                                                                   \
        hr = S_OK;                                                                      \
        _ret_value = ::_name _args;                                                     \
    }

#define LEGACY_API_PASS_THROUGH_STATIC_VOIDRET(_name, _sig, _args)                      \
    {                                                                                   \
        ::_name _args;                                                                  \
    }

#define LEGACY_API_PASS_THROUGH_DELAYLOAD(_name, _ret_type, _ret_value, _sig, _args)    \
    {                                                                                   \
        typedef _ret_type __stdcall t_FN _sig;                                          \
        Util::MscoreeFunctor<t_FN> FN;                                                  \
        if (SUCCEEDED(hr = FN.Init(#_name))) {                                          \
            _ret_value = FN()_args;                                                     \
        }                                                                               \
    }

#define LEGACY_API_PASS_THROUGH_DELAYLOAD_VOIDRET(_name, _sig, _args)                   \
    {                                                                                   \
        typedef void __stdcall t_FN _sig;                                               \
        Util::MscoreeFunctor<t_FN> FN;                                                  \
        if (SUCCEEDED(FN.Init(#_name))) {                                          \
            FN()_args;                                                                  \
        }                                                                               \
    }

#ifndef LEGACY_ACTIVATION_SHIM_DELAY_LOAD
#define CALL_LEGACY_API(_name, _sig, _args)                                             \
    LEGACY_API_PASS_THROUGH_STATIC(_name, HRESULT, hr, _sig, _args)
#define CALL_LEGACY_API_VOIDRET(_name, _sig, _args)                                     \
    LEGACY_API_PASS_THROUGH_STATIC_VOIDRET(_name, _sig, _args)
#else
#define CALL_LEGACY_API(_name, _sig, _args)                                             \
    LEGACY_API_PASS_THROUGH_DELAYLOAD(_name, HRESULT, hr, _sig, _args)
#define CALL_LEGACY_API_VOIDRET(_name, _sig, _args)                                     \
    LEGACY_API_PASS_THROUGH_DELAYLOAD_VOIDRET(_name, _sig, _args)
#endif

    // ---LEGACY SHIM FUNCTIONS--------------------------------------------------------------------

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT GetCORSystemDirectory(
        __out_ecount(cchBuffer) LPWSTR pBuffer, 
        __in  DWORD  cchBuffer, 
        __out DWORD *pdwLength)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs())
        {
            DWORD dwLengthDummy = cchBuffer;
            if (pdwLength == NULL)
                pdwLength = &dwLengthDummy;
            else
                *pdwLength = cchBuffer;

            GET_CLRINFO(pInfo);
            IfHrFailRet(pInfo->GetRuntimeDirectory(pBuffer, pdwLength));
        }
        else
        {
            CALL_LEGACY_API(GetCORSystemDirectory,
                            (LPWSTR pBuffer, 
                             DWORD  cchBuffer, 
                             DWORD *pdwLength),
                            (pBuffer,
                             cchBuffer,
                             pdwLength));
        }

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT GetCORVersion(
        __out_ecount(cchBuffer) LPWSTR pbBuffer, 
        __in  DWORD  cchBuffer, 
        __out DWORD *pdwLength)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs())
        {
            DWORD dwLengthDummy = cchBuffer;
            if (pdwLength == NULL)
                pdwLength = &dwLengthDummy;
            else
                *pdwLength = cchBuffer;

            GET_CLRINFO(pInfo);
            IfHrFailRet(pInfo->GetVersionString(pbBuffer, pdwLength));
        }
        else
        {
            CALL_LEGACY_API(GetCORVersion,
                            (LPWSTR pbBuffer,
                             DWORD  cchBuffer,
                             DWORD *pdwLength),
                            (pbBuffer,
                             cchBuffer,
                             pdwLength));
                            
        }

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT GetFileVersion(
        __in  LPCWSTR szFileName, 
        __out_ecount(cchBuffer) LPWSTR  szBuffer, 
        __in  DWORD   cchBuffer, 
        __out DWORD  *pdwLength)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs())
        {
            DWORD dwLengthDummy = cchBuffer;
            if (pdwLength == NULL)
                pdwLength = &dwLengthDummy;
            else
                *pdwLength = cchBuffer;

            GET_CLRMETAHOST(pMH);
            IfHrFailRet(pMH->GetVersionFromFile(szFileName, szBuffer, pdwLength));
        }
        else
        {
            CALL_LEGACY_API(GetFileVersion,
                            (LPCWSTR szFileName, 
                             LPWSTR  szBuffer, 
                             DWORD   cchBuffer, 
                             DWORD  *pdwLength),
                            (szFileName,
                             szBuffer,
                             cchBuffer,
                             pdwLength));
        }

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT GetCORRequiredVersion(
        __out_ecount(cchBuffer) LPWSTR pBuffer, 
        __in  DWORD  cchBuffer, 
        __out DWORD *pdwLength)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs())
        {
            DWORD dwLengthDummy = cchBuffer;
            if (pdwLength == NULL)
                pdwLength = &dwLengthDummy;
            else
                *pdwLength = cchBuffer;

            IfHrFailRet(Util::GetConfigImageVersion(pBuffer, pdwLength));
        }
        else
        {
            CALL_LEGACY_API(GetCORRequiredVersion,
                            (LPWSTR pBuffer, 
                             DWORD  cchBuffer, 
                             DWORD *pdwLength),
                            (pBuffer,
                             cchBuffer,
                             pdwLength));
        }

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    // This API is the one exception that we don't have fully equivalent functionality for
    // in the new APIs. Specifically, we do not have the runtimeInfoFlags equivalent that
    // allows platform differentiation. As such, we just send the call to the legacy API,
    // which does not bind (thankfully) and so we do not cap this specific API to Whidbey.
    inline
    HRESULT GetRequestedRuntimeInfo(
        __in_opt  LPCWSTR pExe,
        __in_opt  LPCWSTR pwszVersion,
        __in_opt  LPCWSTR pConfigurationFile,
        __in  DWORD startupFlags,
        __in  DWORD runtimeInfoFlags,
        __out_ecount(dwDirectory) LPWSTR pDirectory,
        __in  DWORD dwDirectory,
        __out DWORD  *pdwDirectoryLength, 
        __out_ecount(cchBuffer) LPWSTR pVersion,
        __in  DWORD cchBuffer,
        __out DWORD  *pdwLength)
    {
        HRESULT hr = S_OK;

        CALL_LEGACY_API(GetRequestedRuntimeInfo,
                        (LPCWSTR  pExe, 
                         LPCWSTR  pwszVersion, 
                         LPCWSTR  pConfigurationFile, 
                         DWORD    startupFlags, 
                         DWORD    runtimeInfoFlags, 
                         LPWSTR   pDirectory, 
                         DWORD    dwDirectory, 
                         DWORD   *pdwDirectoryLength, 
                         LPWSTR   pVersion, 
                         DWORD    cchBuffer, 
                         DWORD   *pdwLength),
                        (pExe,
                         pwszVersion,
                         pConfigurationFile,
                         startupFlags,
                         runtimeInfoFlags,
                         pDirectory,
                         dwDirectory,
                         pdwDirectoryLength, 
                         pVersion,
                         cchBuffer,
                         pdwLength));

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT GetRequestedRuntimeVersion(
        __in  LPWSTR pExe,
        __out_ecount(cchBuffer) LPWSTR pVersion,
        __in  DWORD cchBuffer,
        __out DWORD *pdwLength)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs())
        {
            DWORD dwLengthDummy = cchBuffer;
            if (pdwLength == NULL)
                pdwLength = &dwLengthDummy;
            else
                *pdwLength = cchBuffer;

            GET_CLRMETAHOSTPOLICY(pMHP);
            ICLRRuntimeInfo *pInfo = NULL;
            IfHrFailRet(pMHP->GetRequestedRuntime(
                METAHOST_POLICY_USE_PROCESS_IMAGE_PATH,
                pExe,
                NULL, // config stream
                pVersion,
                pdwLength, 
                NULL, // image version str
                NULL, // image version len
                NULL,
                IID_ICLRRuntimeInfo,
                reinterpret_cast<LPVOID*>(&pInfo)));// ppRuntime
            Util::ReleaseHolder<ICLRRuntimeInfo*> hInfo(pInfo);
        }
        else
        {
            CALL_LEGACY_API(GetRequestedRuntimeVersion,
                            (LPWSTR pExe,
                             LPWSTR pVersion,
                             DWORD cchBuffer,
                             DWORD *pdwLength),
                            (pExe,
                             pVersion,
                             cchBuffer,
                             pdwLength));
        }

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT CorBindToRuntimeHost(
        LPCWSTR pwszVersion,
        LPCWSTR pwszBuildFlavor,
        LPCWSTR pwszHostConfigFile,
        VOID* pReserved,
        DWORD startupFlags,
        REFCLSID rclsid,
        REFIID riid,
        LPVOID FAR *ppv)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs())
        {
            IStream *pConfigStream = NULL;
            Util::ReleaseHolder<IStream*> hConfigStream;
            if (pwszHostConfigFile != NULL)
            {
                IfHrFailRet(Util::CreateIStreamFromFile(pwszHostConfigFile, &pConfigStream));
                hConfigStream.Assign(pConfigStream);
            }

            WCHAR wszVersionLocal[512];
            DWORD cchVersionLocal = 512;
            if (pwszVersion != NULL)
                wcsncpy_s(&wszVersionLocal[0], cchVersionLocal, pwszVersion, _TRUNCATE);

            ICLRRuntimeInfo *pInfo = NULL;
            IfHrFailRet(Util::GetCLRRuntimeInfo(
                &pInfo,
                NULL,
                pConfigStream,
                pwszVersion == NULL ? NULL : &wszVersionLocal[0],
                pwszVersion == NULL ? NULL : &cchVersionLocal));

            // We're intentionally ignoring the HRESULT return value, since CorBindToRuntimeEx
            // always ignored these flags when a runtime had already been bound, and we need
            // to emulate that behavior for when multiple calls to CorBindToRuntimeEx are made
            // but with different startup flags (ICLRRuntimeInfo::SetDefaultStartupFlags will
            // return E_INVALIDARG in the case that the runtime has already been started with
            // different flags).
            Util::AddStartupFlags(pInfo, pwszBuildFlavor, startupFlags, pwszHostConfigFile);

            IfHrFailRet(pInfo->GetInterface(rclsid, riid, ppv));
        }
        else
        {
            CALL_LEGACY_API(CorBindToRuntimeHost,
                            (LPCWSTR pwszVersion,
                             LPCWSTR pwszBuildFlavor,
                             LPCWSTR pwszHostConfigFile,
                             VOID* pReserved,
                             DWORD startupFlags,
                             REFCLSID rclsid,
                             REFIID riid,
                             LPVOID FAR *ppv),
                            (pwszVersion,
                             pwszBuildFlavor,
                             pwszHostConfigFile,
                             pReserved,
                             startupFlags,
                             rclsid,
                             riid,
                             ppv));
        }

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT CorBindToRuntimeEx(
        LPCWSTR pwszVersion,
        LPCWSTR pwszBuildFlavor,
        DWORD startupFlags,
        REFCLSID rclsid,
        REFIID riid,
        LPVOID* ppv)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs())
        {
            WCHAR wszVersionLocal[512];
            DWORD cchVersionLocal = 512;
            if (pwszVersion != NULL)
                wcsncpy_s(&wszVersionLocal[0], cchVersionLocal, pwszVersion, _TRUNCATE);

            ICLRRuntimeInfo *pInfo = NULL;
            IfHrFailRet(Util::GetCLRRuntimeInfo(
                &pInfo,
                NULL, // exe path
                NULL, // config stream
                pwszVersion == NULL ? NULL : &wszVersionLocal[0],
                pwszVersion == NULL ? NULL : &cchVersionLocal));

            // We're intentionally ignoring the HRESULT return value, since CorBindToRuntimeEx
            // always ignored these flags when a runtime had already been bound, and we need
            // to emulate that behavior for when multiple calls to CorBindToRuntimeEx are made
            // but with different startup flags (ICLRRuntimeInfo::SetDefaultStartupFlags will
            // return E_INVALIDARG in the case that the runtime has already been started with
            // different flags).
            Util::AddStartupFlags(pInfo, pwszBuildFlavor, startupFlags, NULL);

            IfHrFailRet(pInfo->GetInterface(rclsid, riid, ppv));
        }
        else
        {
            CALL_LEGACY_API(CorBindToRuntimeEx,
                            (LPCWSTR pwszVersion,
                             LPCWSTR pwszBuildFlavor,
                             DWORD startupFlags,
                             REFCLSID rclsid,
                             REFIID riid,
                             LPVOID* ppv),
                            (pwszVersion,
                             pwszBuildFlavor,
                             startupFlags,
                             rclsid,
                             riid,
                             ppv));
        }

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT CorBindToRuntimeByCfg(
        IStream* pCfgStream,
        DWORD reserved,
        DWORD startupFlags,
        REFCLSID rclsid,
        REFIID riid,
        LPVOID* ppv)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs())
        {
            // The legacy CorBindToRuntimeByCfg picks up startup flags from both the config stream and
            // application config file if it is present. For simplicity, we ignore the app config here.
            ICLRRuntimeInfo *pInfo = NULL;
            IfHrFailRet(Util::GetCLRRuntimeInfo(
                &pInfo,
                NULL, // exe path
                pCfgStream));

            // We're intentionally ignoring the HRESULT return value, since CorBindToRuntimeEx
            // always ignored these flags when a runtime had already been bound, and we need
            // to emulate that behavior for when multiple calls to CorBindToRuntimeEx are made
            // but with different startup flags (ICLRRuntimeInfo::SetDefaultStartupFlags will
            // return E_INVALIDARG in the case that the runtime has already been started with
            // different flags).
            Util::AddStartupFlags(pInfo, NULL, startupFlags, NULL);

            IfHrFailRet(pInfo->GetInterface(rclsid, riid, ppv));
        }
        else
        {
            CALL_LEGACY_API(CorBindToRuntimeByCfg,
                            (IStream* pCfgStream,
                             DWORD reserved,
                             DWORD startupFlags,
                             REFCLSID rclsid,
                             REFIID riid,
                             LPVOID* ppv),
                            (pCfgStream,
                             reserved,
                             startupFlags,
                             rclsid,
                             riid,
                             ppv));
        }

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT CorBindToRuntime(
        LPCWSTR pwszVersion,
        LPCWSTR pwszBuildFlavor,
        REFCLSID rclsid,
        REFIID riid,
        LPVOID* ppv)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs())
        {
            WCHAR wszVersionLocal[512];
            DWORD cchVersionLocal = 512;
            if (pwszVersion != NULL)
                wcsncpy_s(&wszVersionLocal[0], cchVersionLocal, pwszVersion, _TRUNCATE);

            ICLRRuntimeInfo *pInfo = NULL;
            IfHrFailRet(Util::GetCLRRuntimeInfo(
                &pInfo,
                NULL, // exe path
                NULL, // config stream
                pwszVersion == NULL ? NULL : &wszVersionLocal[0],
                pwszVersion == NULL ? NULL : &cchVersionLocal));

            // CorBindToRuntime has its special default flags
            //
            // We're intentionally ignoring the HRESULT return value, since CorBindToRuntimeEx
            // always ignored these flags when a runtime had already been bound, and we need
            // to emulate that behavior for when multiple calls to CorBindToRuntimeEx are made
            // but with different startup flags (ICLRRuntimeInfo::SetDefaultStartupFlags will
            // return E_INVALIDARG in the case that the runtime has already been started with
            // different flags).
            Util::AddStartupFlags(pInfo, NULL, STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN_HOST, NULL);

            IfHrFailRet(pInfo->GetInterface(rclsid, riid, ppv));
        }
        else
        {
            CALL_LEGACY_API(CorBindToRuntime,
                            (LPCWSTR pwszVersion,
                             LPCWSTR pwszBuildFlavor,
                             REFCLSID rclsid,
                             REFIID riid,
                             LPVOID* ppv),
                            (pwszVersion,
                             pwszBuildFlavor,
                             rclsid,
                             riid,
                             ppv));
        }

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT CorBindToCurrentRuntime(
        LPCWSTR pwszFileName,
        REFCLSID rclsid,
        REFIID riid,
        LPVOID FAR *ppv)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs())
        {
            ICLRRuntimeInfo *pInfo = NULL;
            IfHrFailRet(Util::GetCLRRuntimeInfo(
                &pInfo,
                pwszFileName));

            IfHrFailRet(pInfo->GetInterface(rclsid, riid, ppv));
        }
        else
        {
            CALL_LEGACY_API(CorBindToCurrentRuntime,
                            (LPCWSTR pwszFileName,
                             REFCLSID rclsid,
                             REFIID riid,
                             LPVOID FAR *ppv),
                            (pwszFileName,
                             rclsid,
                             riid,
                             ppv));
        }

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT ClrCreateManagedInstance(
        LPCWSTR pTypeName,
        REFIID riid,
        void **ppObject)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs())
        {
            GET_CLRINFO(pInfo);
            HRESULT (STDMETHODCALLTYPE *pfnClrCreateManagedInstance)(LPCWSTR typeName, REFIID riid, void ** ppv) = NULL;
            IfHrFailRet(pInfo->GetProcAddress("ClrCreateManagedInstance", (LPVOID *)&pfnClrCreateManagedInstance));
            IfHrFailRet(pfnClrCreateManagedInstance(pTypeName, riid, ppObject));
        }
        else
        {
            CALL_LEGACY_API(ClrCreateManagedInstance,
                            (LPCWSTR pTypeName,
                             REFIID riid,
                             void **ppObject),
                            (pTypeName,
                             riid,
                             ppObject));
        }

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT LoadLibraryShim(
        LPCWSTR szDllName,
        LPCWSTR szVersion,
        LPVOID pvReserved,
        HMODULE *phModDll)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs())
        {
            Util::ReleaseHolder<ICLRRuntimeInfo*> hInfo;
            ICLRRuntimeInfo *pInfo = NULL;

            // Semantics of LoadLibraryShim is that a non-null version must match exactly.
            if (szVersion != NULL)
            {
                GET_CLRMETAHOST(pMH);
                IfHrFailRet(pMH->GetRuntime(szVersion, IID_ICLRRuntimeInfo, reinterpret_cast<LPVOID*>(&pInfo)));
                hInfo.Assign(pInfo);
            }
            else
            {
                IfHrFailRet(Util::GetCLRRuntimeInfo(&pInfo));
            }
            IfHrFailRet(pInfo->LoadLibrary(szDllName, phModDll));
        }
        else
        {
            CALL_LEGACY_API(LoadLibraryShim,
                            (LPCWSTR szDllName,
                             LPCWSTR szVersion,
                             LPVOID pvReserved,
                             HMODULE *phModDll),
                            (szDllName,
                             szVersion,
                             pvReserved,
                             phModDll));
        }

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT CallFunctionShim(
        LPCWSTR szDllName,
        LPCSTR szFunctionName,
        LPVOID lpvArgument1,
        LPVOID lpvArgument2,
        LPCWSTR szVersion,
        LPVOID pvReserved)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs())
        {
            HMODULE hMod = NULL;
            HRESULT (__stdcall * pfn)(LPVOID,LPVOID) = NULL;

            // Load library
            IfHrFailRet(LegacyActivationShim::LoadLibraryShim(szDllName, szVersion, pvReserved, &hMod));

            // NOTE: Legacy CallFunctionShim does not release HMODULE, leak to maintain compat
            // Util::HMODULEHolder hModHolder(hMod);
            
            // Find function.
            pfn = (HRESULT (__stdcall *)(LPVOID,LPVOID))GetProcAddress(hMod, szFunctionName);
            if (pfn == NULL)
                return HRESULT_FROM_WIN32(GetLastError());
            
            // Call it.
            return pfn(lpvArgument1, lpvArgument2);
        }
        else
        {
            CALL_LEGACY_API(CallFunctionShim,
                            (LPCWSTR szDllName,
                             LPCSTR szFunctionName,
                             LPVOID lpvArgument1,
                             LPVOID lpvArgument2,
                             LPCWSTR szVersion,
                             LPVOID pvReserved),
                            (szDllName,
                             szFunctionName,
                             lpvArgument1,
                             lpvArgument2,
                             szVersion,
                             pvReserved));
        }

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT GetRealProcAddress(
        LPCSTR pwszProcName, 
        VOID **ppv)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs())
        {
            GET_CLRINFO(pInfo);
            IfHrFailRet(pInfo->GetProcAddress(pwszProcName, ppv));
        }
        else
        {
            CALL_LEGACY_API(GetRealProcAddress,
                            (LPCSTR pwszProcName, 
                             VOID **ppv),
                            (pwszProcName,
                             ppv));
        }

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    inline
    void CorExitProcess(
        int exitCode)
    {
#ifndef LEGACY_ACTIVATION_SHIM_DELAY_LOAD
        ::CorExitProcess(exitCode);
#else
        typedef void __stdcall t_CorExitProcess(
            int exitCode);

        Util::MscoreeFunctor<t_CorExitProcess> FN;
        if (FAILED(FN.Init("CorExitProcess")))
            return;

        FN()(exitCode);
#endif
    }

// Define this method only if it is not yet defined as macro (see ndp\clr\src\inc\UtilCode.h).
#ifndef LoadStringRC
    // --------------------------------------------------------------------------------------------
    inline
    HRESULT LoadStringRC(
        UINT   nResourceID, 
        __out_ecount(nMax) LPWSTR szBuffer,
        int    nMax, 
        int    fQuiet)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs())
        {
            GET_CLRINFO(pInfo);
            DWORD cchMax = static_cast<DWORD>(nMax);
            IfHrFailRet(pInfo->LoadErrorString(nResourceID, szBuffer, &cchMax, -1));
        }
        else
        {
            CALL_LEGACY_API(LoadStringRC,
                            (UINT   nResourceID, 
                             LPWSTR szBuffer,
                             int    nMax, 
                             int    fQuiet),
                            (nResourceID,
                             szBuffer,
                             nMax,
                             fQuiet));
        }

        return hr;
    }
#endif //LoadStringRC

// Define this method only if it is not yet defined as macro (see ndp\clr\src\inc\UtilCode.h).
#if !defined(LoadStringRCEx) && !defined(FEATURE_CORESYSTEM)
    // --------------------------------------------------------------------------------------------
    inline
    HRESULT LoadStringRCEx(
        LCID lcid,
        UINT   nResourceID, 
        __out_ecount(nMax) LPWSTR szBuffer,
        int    nMax, 
        int    fQuiet, 
        int *pcwchUsed)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs())
        {
            GET_CLRINFO(pInfo);
            DWORD cchUsed = static_cast<DWORD>(nMax);
            IfHrFailRet(pInfo->LoadErrorString(nResourceID, szBuffer, &cchUsed, lcid));
            *pcwchUsed = cchUsed;
        }
        else
        {
            CALL_LEGACY_API(LoadStringRCEx,
                            (LCID lcid,
                             UINT   nResourceID, 
                             LPWSTR szBuffer,
                             int    nMax, 
                             int    fQuiet, 
                             int *pcwchUsed),
                            (lcid,
                             nResourceID,
                             szBuffer,
                             nMax,
                             fQuiet,
                             pcwchUsed));
        }

        return hr;
    }
#endif //LoadStringRCEx

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT LockClrVersion(
        FLockClrVersionCallback hostCallback,
        FLockClrVersionCallback *pBeginHostSetup,
        FLockClrVersionCallback *pEndHostSetup)
    {
        HRESULT hr = S_OK;

        CALL_LEGACY_API(LockClrVersion,
                        (FLockClrVersionCallback hostCallback,
                         FLockClrVersionCallback *pBeginHostSetup,
                         FLockClrVersionCallback *pEndHostSetup),
                        (hostCallback,
                         pBeginHostSetup,
                         pEndHostSetup));

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT CreateDebuggingInterfaceFromVersion(
        int        nDebuggerVersion, 
        LPCWSTR szDebuggeeVersion,
        IUnknown ** ppCordb)
    {
        HRESULT hr = S_OK;

        CALL_LEGACY_API(CreateDebuggingInterfaceFromVersion,
                        (int        nDebuggerVersion, 
                         LPCWSTR szDebuggeeVersion,
                         IUnknown ** ppCordb),
                        (nDebuggerVersion,
                         szDebuggeeVersion,
                         ppCordb));

        return hr;
    }

    // --------------------------------------------------------------------------------------------
    inline
    HRESULT GetVersionFromProcess(
        __in  HANDLE hProcess,
        __out_ecount(cchBuffer) LPWSTR pVersion,
        __in  DWORD cchBuffer,
        __out DWORD *pdwLength)
    {
        HRESULT hr = S_OK;

        CALL_LEGACY_API(GetVersionFromProcess,
                        (HANDLE hProcess,
                         LPWSTR pVersion,
                         DWORD cchBuffer,
                         DWORD *pdwLength),
                        (hProcess,
                         pVersion,
                         cchBuffer,
                         pdwLength));

        return hr;
    }

    // --------------------------------------------------------------------------------------------
// CoInitializeEE is declared in cor.h, define it only if explicitly requested
#ifdef LEGACY_ACTIVATION_SHIM_DEFINE_CoInitializeEE
    inline 
    HRESULT CoInitializeEE(DWORD flags)
    {
        HRESULT hr = S_OK;
        
        if (Util::HasNewActivationAPIs())
        {
            GET_CLRINFO(pInfo);
            HRESULT (* pfnCoInitializeEE)(DWORD);
            IfHrFailRet(pInfo->GetProcAddress("CoInitializeEE", (LPVOID *)&pfnCoInitializeEE));
            return (*pfnCoInitializeEE)(flags);
        }
        else
        {
            CALL_LEGACY_API(CoInitializeEE,
                            (DWORD flags),
                            (flags));
        }
        
        return hr;
    }

    inline 
    VOID CoUninitializeEE(BOOL flags)
    {
        if (Util::HasNewActivationAPIs())
        {
            ICLRRuntimeInfo *pInfo = NULL;
            if (FAILED(Util::GetCLRRuntimeInfo(&pInfo)))
                return;

            VOID (* pfnCoUninitializeEE)(BOOL);
            if (FAILED(pInfo->GetProcAddress("CoUninitializeEE", (LPVOID *)&pfnCoUninitializeEE)))
                return;

            (*pfnCoUninitializeEE)(flags);
        }
        else
        {
            CALL_LEGACY_API_VOIDRET(CoUninitializeEE,
                                    (BOOL flags),
                                    (flags));
        }
    }

    inline 
    HRESULT CoInitializeCor(DWORD flags)
    {
        HRESULT hr = S_OK;
        
        if (Util::HasNewActivationAPIs())
        {
            GET_CLRINFO(pInfo);
            HRESULT (* pfnCoInitializeCor)(DWORD);
            IfHrFailRet(pInfo->GetProcAddress("CoInitializeCor", (LPVOID *)&pfnCoInitializeCor));
            return (*pfnCoInitializeCor)(flags);
        }
        else
        {
            CALL_LEGACY_API(CoInitializeCor,
                            (DWORD flags),
                            (flags));
        }
        
        return hr;
    }

    inline 
    VOID CoUninitializeCor()
    {
        if (Util::HasNewActivationAPIs())
        {
            ICLRRuntimeInfo *pInfo = NULL;
            if (FAILED(Util::GetCLRRuntimeInfo(&pInfo)))
                return;

            VOID (* pfnCoUninitializeCor)();
            if (FAILED(pInfo->GetProcAddress("CoUninitializeCor", (LPVOID *)&pfnCoUninitializeCor)))
                return;

            (*pfnCoUninitializeCor)();
        }
        else
        {
            CALL_LEGACY_API_VOIDRET(CoUninitializeCor,
                                    (VOID),
                                    ());
        }
    }

#endif //LEGACY_ACTIVATION_SHIM_DEFINE_CoInitializeEE
    
    // --------------------------------------------------------------------------------------------
// CoEEShutDownCOM is declared in cor.h, define it only if explicitly requested
#ifdef LEGACY_ACTIVATION_SHIM_DEFINE_CoEEShutDownCOM
    inline 
    void CoEEShutDownCOM()
    {
        if (Util::HasNewActivationAPIs())
        {
            ICLRRuntimeInfo *pInfo = NULL;
            IfHrFailRetVOID(Util::GetCLRRuntimeInfo(&pInfo));
            void (* pfnCoEEShutDownCOM)();
            IfHrFailRetVOID(pInfo->GetProcAddress("CoEEShutDownCOM", (LPVOID *)&pfnCoEEShutDownCOM));
            (*pfnCoEEShutDownCOM)();
        }
        else
        {
            CALL_LEGACY_API_VOIDRET(CoEEShutDownCOM, 
                            (), 
                            ());
        }
        
        return;
    }
#endif //LEGACY_ACTIVATION_SHIM_DEFINE_CoEEShutDownCOM
    
    // ---StrongName Function Helpers--------------------------------------------------------------
#if !defined(LEGACY_ACTIVATION_SHIM_DELAY_LOAD) && defined(__STRONG_NAME_H)
#define LEGACY_STRONGNAME_API_PASS_THROUGH(_name, _ret_type, _ret_value, _sig, _args)   \
    LEGACY_API_PASS_THROUGH_STATIC(_name, _ret_type, _ret_value, _sig, _args)
#define LEGACY_STRONGNAME_API_PASS_THROUGH_VOIDRET(_name, _sig, _args)   \
    LEGACY_API_PASS_THROUGH_STATIC_VOIDRET(_name, _sig, _args)
#else //defined(LEGACY_ACTIVATION_SHIM_DELAY_LOAD) || !defined(__STRONG_NAME_H)
#define LEGACY_STRONGNAME_API_PASS_THROUGH(_name, _ret_type, _ret_value, _sig, _args)   \
    LEGACY_API_PASS_THROUGH_DELAYLOAD(_name, _ret_type, _ret_value, _sig, _args)
#define LEGACY_STRONGNAME_API_PASS_THROUGH_VOIDRET(_name, _sig, _args)   \
    LEGACY_API_PASS_THROUGH_DELAYLOAD_VOIDRET(_name, _sig, _args)
#endif //defined(LEGACY_ACTIVATION_SHIM_DELAY_LOAD) || !defined(__STRONG_NAME_H)

// Defines a method that just delegates a call to the right runtime, this one is for SN APIs that 
// return HRESULT.
#define PASS_THROUGH_IMPL_HRESULT(_name, _signature, _args)     \
    inline                                                      \
    HRESULT _name##_signature                                   \
    {                                                           \
        HRESULT hr = S_OK;                                      \
        if (Util::HasNewActivationAPIs())                       \
        {                                                       \
            ICLRStrongName *pSN = NULL;                         \
            IfHrFailRet(Util::GetCLRStrongName(&pSN));          \
            IfHrFailRet(pSN->_name _args);                      \
        }                                                       \
        else                                                    \
        {                                                       \
            LEGACY_STRONGNAME_API_PASS_THROUGH(                 \
                _name, HRESULT, hr, _signature, _args);         \
            IfHrFailRet(hr);                                    \
        }                                                       \
        return hr;                                              \
    }

// Defines a method that just delegates a call to the right runtime, this one is for SN APIs that 
// return BOOL.
#define PASS_THROUGH_IMPL_BOOLEAN(_name, _signature, _args)     \
    inline                                                      \
    BOOL _name##_signature                                      \
    {                                                           \
        HRESULT hr = S_OK;                                      \
        if (Util::HasNewActivationAPIs())                       \
        {                                                       \
            ICLRStrongName *pSN = NULL;                         \
            IfHrFailRetFALSE(Util::GetCLRStrongName(&pSN));     \
            IfHrFailRetFALSE(pSN->_name _args);                 \
            return TRUE;                                        \
        }                                                       \
        else                                                    \
        {                                                       \
            BOOL fResult = TRUE;                                \
            LEGACY_STRONGNAME_API_PASS_THROUGH(                 \
                _name, BOOL, fResult, _signature, _args);       \
            IfHrFailRetFALSE(hr);                               \
            return fResult;                                     \
        }                                                       \
    }

// Defines a method that just delegates a call to the right runtime, this one is for SN APIs that 
// return VOID.
#define PASS_THROUGH_IMPL_VOID(_name, _signature, _args)        \
    inline                                                      \
    VOID _name##_signature                                      \
    {                                                           \
        HRESULT hr = S_OK;                                      \
        if (Util::HasNewActivationAPIs())                       \
        {                                                       \
            ICLRStrongName *pSN = NULL;                         \
            IfHrFailRetVOID(Util::GetCLRStrongName(&pSN));      \
            IfHrFailRetVOID(pSN->_name _args);                  \
            return;                                             \
        }                                                       \
        else                                                    \
        {                                                       \
            LEGACY_STRONGNAME_API_PASS_THROUGH_VOIDRET(         \
                _name, _signature, _args);                      \
            IfHrFailRetVOID(hr);                                \
            return;                                             \
        }                                                       \
    }

    // ---StrongName functions---------------------------------------------------------------------

PASS_THROUGH_IMPL_HRESULT(GetHashFromAssemblyFile,
                         (LPCSTR pszFilePath, unsigned int *piHashAlg, BYTE *pbHash, DWORD cchHash, DWORD *pchHash),
                         (pszFilePath, piHashAlg, pbHash, cchHash, pchHash));

PASS_THROUGH_IMPL_HRESULT(GetHashFromAssemblyFileW,
                         (LPCWSTR pwzFilePath, unsigned int *piHashAlg, BYTE *pbHash, DWORD cchHash, DWORD *pchHash),
                         (pwzFilePath, piHashAlg, pbHash, cchHash, pchHash));

PASS_THROUGH_IMPL_HRESULT(GetHashFromBlob,
                         (BYTE *pbBlob, DWORD cchBlob, unsigned int *piHashAlg, BYTE *pbHash, DWORD cchHash, DWORD *pchHash),
                         (pbBlob, cchBlob, piHashAlg, pbHash, cchHash, pchHash));

PASS_THROUGH_IMPL_HRESULT(GetHashFromFile,
                         (LPCSTR pszFilePath, unsigned int *piHashAlg, BYTE *pbHash, DWORD cchHash, DWORD *pchHash),
                         (pszFilePath, piHashAlg, pbHash, cchHash, pchHash));

PASS_THROUGH_IMPL_HRESULT(GetHashFromFileW,
                         (LPCWSTR pwzFilePath, unsigned int *piHashAlg, BYTE *pbHash, DWORD cchHash, DWORD *pchHash),
                         (pwzFilePath, piHashAlg, pbHash, cchHash, pchHash));

PASS_THROUGH_IMPL_HRESULT(GetHashFromHandle,
                         (HANDLE hFile, unsigned int *piHashAlg, BYTE *pbHash, DWORD cchHash, DWORD *pchHash),
                         (hFile, piHashAlg, pbHash, cchHash, pchHash));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameCompareAssemblies,
                         (LPCWSTR pwzAssembly1, LPCWSTR pwzAssembly2, DWORD *pdwResult),
                         (pwzAssembly1, pwzAssembly2, pdwResult));

PASS_THROUGH_IMPL_VOID(StrongNameFreeBuffer,
                      (BYTE *pbMemory),
                      (pbMemory));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameGetBlob,
                         (LPCWSTR pwzFilePath, BYTE *pbBlob, DWORD *pcbBlob),
                         (pwzFilePath, pbBlob, pcbBlob));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameGetBlobFromImage,
                         (BYTE *pbBase, DWORD dwLength, BYTE *pbBlob, DWORD *pcbBlob),
                         (pbBase, dwLength, pbBlob, pcbBlob));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameGetPublicKey,
                         (LPCWSTR pwzKeyContainer, BYTE *pbKeyBlob, ULONG cbKeyBlob, BYTE **ppbPublicKeyBlob, ULONG *pcbPublicKeyBlob),
                         (pwzKeyContainer, pbKeyBlob, cbKeyBlob, ppbPublicKeyBlob, pcbPublicKeyBlob));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameHashSize,
                         (ULONG ulHashAlg, DWORD *pcbSize),
                         (ulHashAlg, pcbSize));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameKeyDelete,
                         (LPCWSTR pwzKeyContainer),
                         (pwzKeyContainer));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameKeyGen,
                         (LPCWSTR pwzKeyContainer, DWORD dwFlags, BYTE **ppbKeyBlob, ULONG *pcbKeyBlob),
                         (pwzKeyContainer, dwFlags, ppbKeyBlob, pcbKeyBlob));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameKeyGenEx,
                         (LPCWSTR pwzKeyContainer, DWORD dwFlags, DWORD dwKeySize, BYTE **ppbKeyBlob, ULONG *pcbKeyBlob),
                         (pwzKeyContainer, dwFlags, dwKeySize, ppbKeyBlob, pcbKeyBlob));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameKeyInstall,
                         (LPCWSTR pwzKeyContainer, BYTE *pbKeyBlob, ULONG cbKeyBlob),
                         (pwzKeyContainer, pbKeyBlob, cbKeyBlob));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameSignatureGeneration,
                         (LPCWSTR pwzFilePath, LPCWSTR pwzKeyContainer, BYTE *pbKeyBlob, ULONG cbKeyBlob, BYTE **ppbSignatureBlob, ULONG *pcbSignatureBlob),
                         (pwzFilePath, pwzKeyContainer, pbKeyBlob, cbKeyBlob, ppbSignatureBlob, pcbSignatureBlob));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameSignatureGenerationEx,
                         (LPCWSTR wszFilePath, LPCWSTR wszKeyContainer, BYTE *pbKeyBlob, ULONG cbKeyBlob, BYTE **ppbSignatureBlob, ULONG *pcbSignatureBlob, DWORD dwFlags),
                         (wszFilePath, wszKeyContainer, pbKeyBlob, cbKeyBlob, ppbSignatureBlob, pcbSignatureBlob, dwFlags));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameSignatureSize,
                         (BYTE *pbPublicKeyBlob, ULONG cbPublicKeyBlob, DWORD *pcbSize),
                         (pbPublicKeyBlob, cbPublicKeyBlob, pcbSize));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameSignatureVerification,
                         (LPCWSTR  pwzFilePath, DWORD dwInFlags, DWORD *pdwOutFlags),
                         (pwzFilePath, dwInFlags, pdwOutFlags));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameSignatureVerificationEx,
                         (LPCWSTR pwzFilePath, BOOLEAN fForceVerification, BOOLEAN *pfWasVerified),
                         (pwzFilePath, fForceVerification, pfWasVerified));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameSignatureVerificationFromImage,
                         (BYTE *pbBase, DWORD dwLength, DWORD dwInFlags, DWORD *pdwOutFlags),
                         (pbBase, dwLength, dwInFlags, pdwOutFlags));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameTokenFromAssembly,
                         (LPCWSTR pwzFilePath, BYTE **ppbStrongNameToken, ULONG *pcbStrongNameToken),
                         (pwzFilePath, ppbStrongNameToken, pcbStrongNameToken));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameTokenFromAssemblyEx,
                         (LPCWSTR pwzFilePath, BYTE **ppbStrongNameToken, ULONG *pcbStrongNameToken, BYTE **ppbPublicKeyBlob, ULONG *pcbPublicKeyBlob),
                         (pwzFilePath, ppbStrongNameToken, pcbStrongNameToken, ppbPublicKeyBlob, pcbPublicKeyBlob));

PASS_THROUGH_IMPL_BOOLEAN(StrongNameTokenFromPublicKey,
                         (BYTE *pbPublicKeyBlob, ULONG cbPublicKeyBlob, BYTE **ppbStrongNameToken, ULONG *pcbStrongNameToken),
                         (pbPublicKeyBlob, cbPublicKeyBlob, ppbStrongNameToken, pcbStrongNameToken));

#undef PASS_THROUGH_IMPL_HRESULT
#undef PASS_THROUGH_IMPL_BOOLEAN
#undef PASS_THROUGH_IMPL_VOID

// Defines a method that just delegates a call to the right runtime, this one is for SN APIs that 
// return BOOLEAN.
#define WRAP_HRESULT_IMPL_BOOLEAN(_WrapperName, _name, _signature, _args)               \
    inline                                                                              \
    HRESULT _WrapperName##_signature                                                    \
    {                                                                                   \
        HRESULT hr = S_OK;                                                              \
        if (Util::HasNewActivationAPIs())                                               \
        {                                                                               \
            ICLRStrongName *pSN = NULL;                                                 \
            IfHrFailRet(Util::GetCLRStrongName(&pSN));                                  \
            return pSN->_name _args;                                                    \
        }                                                                               \
        else                                                                            \
        {                                                                               \
            typedef BOOL __stdcall t_FN _signature;                                     \
            Util::MscoreeFunctor<t_FN> FN;                                              \
            IfHrFailRet(FN.Init(#_name));                                               \
            if ((FN() _args))                                                           \
            {                                                                           \
                return S_OK;                                                            \
            }                                                                           \
            else                                                                        \
            {   /*@TODO: Static bind version, if necessary*/                            \
                typedef DWORD __stdcall t_FNStrongNameErrorInfo(void);                  \
                Util::MscoreeFunctor<t_FNStrongNameErrorInfo> FNStrongNameErrorInfo;    \
                IfHrFailRet(FNStrongNameErrorInfo.Init("StrongNameErrorInfo"));         \
                HRESULT hrResult = (HRESULT)FNStrongNameErrorInfo() ();                 \
                if (SUCCEEDED(hrResult))                                                \
                {                                                                       \
                    hrResult = E_FAIL;                                                  \
                }                                                                       \
                return hrResult;                                                        \
            }                                                                           \
        }                                                                               \
    }

WRAP_HRESULT_IMPL_BOOLEAN(StrongNameHashSize_HRESULT, 
                          StrongNameHashSize, 
                          (ULONG ulHashAlg, DWORD *pcbSize), 
                          (ulHashAlg, pcbSize));

WRAP_HRESULT_IMPL_BOOLEAN(StrongNameTokenFromPublicKey_HRESULT, 
                          StrongNameTokenFromPublicKey, 
                          (BYTE *pbPublicKeyBlob, ULONG cbPublicKeyBlob, BYTE **ppbStrongNameToken, ULONG *pcbStrongNameToken), 
                          (pbPublicKeyBlob, cbPublicKeyBlob, ppbStrongNameToken, pcbStrongNameToken));

WRAP_HRESULT_IMPL_BOOLEAN(StrongNameSignatureSize_HRESULT, 
                          StrongNameSignatureSize, 
                          (BYTE *pbPublicKeyBlob, ULONG cbPublicKeyBlob, DWORD *pcbSize), 
                          (pbPublicKeyBlob, cbPublicKeyBlob, pcbSize));

WRAP_HRESULT_IMPL_BOOLEAN(StrongNameGetPublicKey_HRESULT, 
                          StrongNameGetPublicKey, 
                          (LPCWSTR pwzKeyContainer, BYTE *pbKeyBlob, ULONG cbKeyBlob, BYTE **ppbPublicKeyBlob, ULONG *pcbPublicKeyBlob), 
                          (pwzKeyContainer, pbKeyBlob, cbKeyBlob, ppbPublicKeyBlob, pcbPublicKeyBlob));

WRAP_HRESULT_IMPL_BOOLEAN(StrongNameKeyInstall_HRESULT, 
                          StrongNameKeyInstall, 
                          (LPCWSTR pwzKeyContainer, BYTE *pbKeyBlob, ULONG cbKeyBlob), 
                          (pwzKeyContainer, pbKeyBlob, cbKeyBlob));

WRAP_HRESULT_IMPL_BOOLEAN(StrongNameSignatureGeneration_HRESULT, 
                          StrongNameSignatureGeneration, 
                          (LPCWSTR pwzFilePath, LPCWSTR pwzKeyContainer, BYTE *pbKeyBlob, ULONG cbKeyBlob, BYTE **ppbSignatureBlob, ULONG *pcbSignatureBlob), 
                          (pwzFilePath, pwzKeyContainer, pbKeyBlob, cbKeyBlob, ppbSignatureBlob, pcbSignatureBlob));

WRAP_HRESULT_IMPL_BOOLEAN(StrongNameKeyGen_HRESULT, 
                          StrongNameKeyGen, 
                          (LPCWSTR pwzKeyContainer, DWORD dwFlags, BYTE **ppbKeyBlob, ULONG *pcbKeyBlob), 
                          (pwzKeyContainer, dwFlags, ppbKeyBlob, pcbKeyBlob));

#undef WRAP_HRESULT_IMPL_BOOLEAN

// Defines a method that just delegates a call to the right runtime, this one is for ICLRStrongName2 
// APIs that return BOOLEAN.
#define WRAP_HRESULT_IMPL_BOOLEAN(_WrapperName, _name, _signature, _args)               \
    inline                                                                              \
    HRESULT _WrapperName##_signature                                                    \
    {                                                                                   \
        HRESULT hr = S_OK;                                                              \
        if (Util::HasNewActivationAPIs())                                               \
        {                                                                               \
            ICLRStrongName2 *pSN = NULL;                                                \
            IfHrFailRet(Util::GetCLRStrongName2(&pSN));                                 \
            return pSN->_name _args;                                                    \
        }                                                                               \
        else                                                                            \
        {                                                                               \
            return E_FAIL;                                                              \
        }                                                                               \
    }


WRAP_HRESULT_IMPL_BOOLEAN(StrongNameGetPublicKeyEx_HRESULT, 
                          StrongNameGetPublicKeyEx, 
                          (LPCWSTR pwzKeyContainer, BYTE *pbKeyBlob, ULONG cbKeyBlob, BYTE **ppbPublicKeyBlob, ULONG *pcbPublicKeyBlob, ULONG uHashAlgId, ULONG uReserved), 
                          (pwzKeyContainer, pbKeyBlob, cbKeyBlob, ppbPublicKeyBlob, pcbPublicKeyBlob, uHashAlgId, uReserved));

#undef WRAP_HRESULT_IMPL_BOOLEAN

    inline
    HRESULT ClrCoCreateInstance(
      REFCLSID rclsid,
      LPUNKNOWN pUnkOuter,
      DWORD dwClsContext,
      REFIID riid,
      LPVOID * ppv)
    {
        HRESULT hr = S_OK;

        if (Util::HasNewActivationAPIs() /*&& Util::IsCLSIDHostedByClr(rclsid)*/)
        {
            GET_CLRINFO(pInfo);
            IfHrFailRet(pInfo->GetInterface(rclsid, riid, ppv));
        }
        else
        {
            IfHrFailRet(::CoCreateInstance(rclsid, pUnkOuter, dwClsContext, riid, ppv));
        }

        return hr;
    }
}; // namespace LegacyActivationShim
#pragma warning(pop) // Revert C4996 status

#undef LEGACY_API_PASS_THROUGH_STATIC
#undef LEGACY_API_PASS_THROUGH_STATIC_VOIDRET
#undef LEGACY_API_PASS_THROUGH_DELAYLOAD
#undef LEGACY_API_PASS_THROUGH_DELAYLOAD_VOIDRET
#undef CALL_LEGACY_API
#undef LEGACY_STRONGNAME_API_PASS_THROUGH
#undef LEGACY_STRONGNAME_API_PASS_THROUGH_VOIDRET

#undef LEGACY_ACTIVATION_SHIM_DEFAULT_PRODUCT_VER_HELPER_L
#undef LEGACY_ACTIVATION_SHIM_DEFAULT_PRODUCT_VER_STR_L

#pragma pop_macro("IfHrFailRetVOID")
#pragma pop_macro("IfHrFailRetFALSE")
#pragma pop_macro("IfHrFailRet")

#ifdef _MANAGED
// We are compiling Managed C++, restore previous managed/native status from the stack
#pragma managed(pop)
#endif //_MANAGED

#pragma warning(pop)

#endif // __LEGACYACTIVATIONSHIM_H__

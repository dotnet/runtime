// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



/*============================================================
**
** Header:  FusionBind.hpp
**
** Purpose: Implements FusionBind (loader domain) architecture
**
**
===========================================================*/
#ifndef _FUSIONBIND_H
#define _FUSIONBIND_H

#ifndef FEATURE_FUSION
#error FEATURE_FUSION is not enabled, please do not include fusionbind.h
#endif

#include <fusion.h>
#include <fusionpriv.h>
#include "metadata.h"
#include "fusionsink.h"
#include "utilcode.h"
#include "loaderheap.h"
#include "fusionsetup.h"
#include "sstring.h"
#include "ex.h"
#ifdef PAL_STDCPP_COMPAT
#include <type_traits>
#else
#include "clr_std/type_traits"
#endif

#include "binderngen.h"
#include "clrprivbinding.h"

class FusionBind
{
public:

    //****************************************************************************************
    //

    static HRESULT GetVersion(__out_ecount(*pdwVersion) LPWSTR pVersion, __inout DWORD* pdwVersion);
  
    
    //****************************************************************************************
    //
    // Creates a fusion context for the application domain. All ApplicationContext properties
    // must be set in the AppDomain store prior to this call. Any changes or additions to the
    // AppDomain store are ignored.
    static HRESULT CreateFusionContext(LPCWSTR pzName, IApplicationContext** ppFusionContext);


    //****************************************************************************************
    //
    // Loads an environmental value into the fusion context
    static HRESULT AddEnvironmentProperty(__in LPCWSTR variable, 
                                          __in LPCWSTR pProperty, 
                                          IApplicationContext* pFusionContext);

    //****************************************************************************************
    //
    static HRESULT SetupFusionContext(LPCWSTR szAppBase,
                                      LPCWSTR szPrivateBin,
                                      IApplicationContext** ppFusionContext);

    // Starts remote load of an assembly. The thread is parked on 
    // an event waiting for fusion to report success or failure.
    static HRESULT RemoteLoad(IApplicationContext * pFusionContext, 
                              FusionSink* pSink, 
                              IAssemblyName *pName, 
                              IAssembly *pParentAssembly,
                              LPCWSTR pCodeBase,
                              IAssembly** ppIAssembly,
                              IHostAssembly** ppIHostAssembly,
                              IBindResult** ppNativeFusionAssembly,
                              BOOL fForIntrospectionOnly,
                              BOOL fSuppressSecurityChecks);

    static HRESULT RemoteLoadModule(IApplicationContext * pFusionContext, 
                                    IAssemblyModuleImport* pModule, 
                                    FusionSink *pSink,
                                    IAssemblyModuleImport** pResult);

    static BOOL VerifyBindingStringW(LPCWSTR pwStr) {
        WRAPPER_NO_CONTRACT;
        if (wcschr(pwStr, '\\') ||
            wcschr(pwStr, '/') ||
            wcschr(pwStr, ':'))
            return FALSE;

        return TRUE;
    }

    static HRESULT VerifyBindingString(LPCSTR pName) {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            INJECT_FAULT(return E_OUTOFMEMORY;);
        }
        CONTRACTL_END;

        DWORD dwStrLen = WszMultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, pName, -1, NULL, NULL);
        CQuickBytes qb;
        LPWSTR pwStr = (LPWSTR) qb.AllocNoThrow(dwStrLen*sizeof(WCHAR));
        if (!pwStr)
            return E_OUTOFMEMORY;
        
        if(!WszMultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, pName, -1, pwStr, dwStrLen))
            return HRESULT_FROM_GetLastError();

        if (VerifyBindingStringW(pwStr))
            return S_OK;
        else
            return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
    }

    static void GetAssemblyManifestModulePath(IAssembly *pFusionAssembly, SString &result)
    {
        CONTRACTL
        {
            THROWS;
            INJECT_FAULT(ThrowOutOfMemory());
        }
        CONTRACTL_END;

        DWORD dwSize = 0;
        LPWSTR buffer = NULL;
        COUNT_T allocation = result.GetUnicodeAllocation();
        if (allocation > 0) {
            // pass in the buffer if we got one
            dwSize = allocation + 1;
            buffer = result.OpenUnicodeBuffer(allocation);
        }
        HRESULT hr = pFusionAssembly->GetManifestModulePath(buffer, &dwSize);
        if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
        {
            if (buffer != NULL) 
                result.CloseBuffer(0);
            buffer = result.OpenUnicodeBuffer(dwSize-1);
            hr = pFusionAssembly->GetManifestModulePath(buffer, &dwSize);
        }
        if (buffer != NULL)
            result.CloseBuffer((SUCCEEDED(hr) && dwSize >= 1) ? (dwSize-1) : 0);
        IfFailThrow(hr);
    }

    static SString& GetAssemblyNameDisplayName(
        IAssemblyName *pName,
        SString &result,
        DWORD flags = 0 /* default */)
    {
        CONTRACTL
        {
            GC_NOTRIGGER;
            THROWS;
            INJECT_FAULT(ThrowOutOfMemory());
        }
        CONTRACTL_END;

        DWORD dwSize = 0;
        LPWSTR buffer = NULL;
        COUNT_T allocation = result.GetUnicodeAllocation();
        if (allocation > 0)
        {
            // pass in the buffer if we got one
            dwSize = allocation + 1;
            buffer = result.OpenUnicodeBuffer(allocation);
        }

        HRESULT hr = pName->GetDisplayName(buffer, &dwSize, flags);
        if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
        {
            if (buffer != NULL) 
                result.CloseBuffer(0);
            buffer = result.OpenUnicodeBuffer(dwSize-1);
            hr = pName->GetDisplayName(buffer, &dwSize, flags);
        }

        if (buffer != NULL)
        {
            result.CloseBuffer((SUCCEEDED(hr) && dwSize >= 1) ? (dwSize-1) : 0);
        }

        IfFailThrow(hr);
        return result;
    }

    static BOOL GetAssemblyNameStringProperty(IAssemblyName *pName, DWORD property, SString &result)
    {
        CONTRACTL
        {
            THROWS;
            INJECT_FAULT(ThrowOutOfMemory());
        }
        CONTRACTL_END;

        DWORD dwSize = 0;
        LPWSTR buffer = NULL;
        COUNT_T allocation = result.GetUnicodeAllocation();
        if (allocation > 0) {
            // pass in the buffer if we got one
            dwSize = (allocation + 1) * sizeof(WCHAR);
            buffer = result.OpenUnicodeBuffer(allocation);
        }
        HRESULT hr = pName->GetProperty(property, (LPVOID)buffer, &dwSize);
        if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
        {
            if (buffer != NULL) 
                result.CloseBuffer(0);
            buffer = result.OpenUnicodeBuffer(dwSize/sizeof(WCHAR) - 1);
            hr = pName->GetProperty(property, (LPVOID)buffer, &dwSize);
        }
        if (buffer != NULL)
            result.CloseBuffer((SUCCEEDED(hr) && dwSize >= sizeof(WCHAR)) ? (dwSize/sizeof(WCHAR)-1) : 0);
        if (hr == HRESULT_FROM_WIN32(ERROR_NOT_FOUND))
        {
            return FALSE;
        }
        IfFailThrow(hr);
              
        return TRUE;
    }

    static BOOL GetApplicationContextStringProperty(IApplicationContext *pContext, 
                                                    LPCWSTR property, SString &result)
    {
        CONTRACTL
        {
            THROWS;
            INJECT_FAULT(ThrowOutOfMemory());
        }
        CONTRACTL_END;

        DWORD dwSize = 0;
        LPWSTR buffer = NULL;
        COUNT_T allocation = result.GetUnicodeAllocation();
        if (allocation > 0) {
            // pass in the buffer if we got one
            dwSize = (allocation + 1) * sizeof(WCHAR);
            buffer = result.OpenUnicodeBuffer(allocation);
        }
        HRESULT hr = pContext->Get(property, (LPVOID)buffer, &dwSize, 0);
        if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
        {
            if (buffer != NULL) 
                result.CloseBuffer(0);
            buffer = result.OpenUnicodeBuffer(dwSize/sizeof(WCHAR) - 1);
            hr = pContext->Get(property, (LPVOID)buffer, &dwSize, 0);
        }
        if (buffer != NULL)
            result.CloseBuffer((SUCCEEDED(hr) && dwSize >= sizeof(WCHAR)) ? (dwSize/sizeof(WCHAR)-1) : 0);
        if (hr == HRESULT_FROM_WIN32(ERROR_NOT_FOUND))
        {
            return FALSE;
        }
        IfFailThrow(hr);

        return TRUE;
    }

    static BOOL GetApplicationContextDWORDProperty(IApplicationContext *pContext, 
                                                   LPCWSTR property, DWORD *result)
    {
        CONTRACTL
        {
            THROWS;
            INJECT_FAULT(return E_OUTOFMEMORY;);
        }
        CONTRACTL_END;

        DWORD dwSize = sizeof(DWORD);
        HRESULT hr = pContext->Get(property, result, &dwSize, 0);
        if (hr == HRESULT_FROM_WIN32(ERROR_NOT_FOUND))
            return FALSE;

        IfFailThrow(hr);
        
        return TRUE;
    }

    static void SetApplicationContextStringProperty(IApplicationContext *pContext, LPCWSTR property, 
                                                    SString &value)
    {
        CONTRACTL
        {
            THROWS;
            INJECT_FAULT(ThrowOutOfMemory());
        }
        CONTRACTL_END;

        IfFailThrow(pContext->Set(property, (void *) value.GetUnicode(), 
                                  (value.GetCount()+1)*sizeof(WCHAR), 0));
    }

    static void SetApplicationContextDWORDProperty(IApplicationContext *pContext, LPCWSTR property, 
                                                   DWORD value)
    {
        CONTRACTL
        {
            THROWS;
            INJECT_FAULT(ThrowOutOfMemory());
        }
        CONTRACTL_END;

        IfFailThrow(pContext->Set(property, &value, sizeof(value), 0));
    }
};

#endif


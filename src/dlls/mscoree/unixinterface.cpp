//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


//*****************************************************************************
// unixinterface.cpp
//
// Implementation for the interface exposed by the libcoreclr.so on Unix
//

//*****************************************************************************

#include "stdafx.h"
#include <utilcode.h>
#include <corhost.h>

#define ASSERTE_ALL_BUILDS(expr) _ASSERTE_ALL_BUILDS(__FILE__, (expr))

// Holder for const wide strings
typedef NewArrayHolder<const WCHAR> ConstWStringHolder;

// Holder for array of wide strings
class ConstWStringArrayHolder : public NewArrayHolder<LPCWSTR>
{
    int m_cElements;

public:
    ConstWStringArrayHolder() :
        NewArrayHolder<LPCWSTR>(),
        m_cElements(0)
    {
    }

    void Set(LPCWSTR* value, int cElements)   
    {                                             
        NewArrayHolder<LPCWSTR>::operator=(value);                  
        m_cElements = cElements;
    }                                             

    ~ConstWStringArrayHolder()
    {
        for (int i = 0; i < m_cElements; i++)
        {
            delete [] this->m_value[i];
        }
    }
};

// Convert 8 bit string to unicode
static LPCWSTR StringToUnicode(LPCSTR str)
{
    int length = MultiByteToWideChar(CP_ACP, 0, str, -1, NULL, 0);
    ASSERTE_ALL_BUILDS(length != 0);

    LPWSTR result = new (nothrow) WCHAR[length];
    ASSERTE_ALL_BUILDS(result != NULL);
    
    length = MultiByteToWideChar(CP_ACP, 0, str, length, result, length);
    ASSERTE_ALL_BUILDS(length != 0);

    return result;
}

// Convert 8 bit string array to unicode string array
static LPCWSTR* StringArrayToUnicode(int argc, LPCSTR* argv)
{
    LPCWSTR* argvW = nullptr;
    
    if (argc > 0)
    {
        argvW = new (nothrow) LPCWSTR[argc];
        ASSERTE_ALL_BUILDS(argvW != 0);
        
        for (int i = 0; i < argc; i++)
        {
            argvW[i] = StringToUnicode(argv[i]);
        }
    }

    return argvW;
}

//
// Execute a managed assembly with given arguments
//
// Parameters:
//  exePath                 - Absolute path of the executable that invoked the ExecuteAssembly
//  coreClrPath             - Absolute path of the libcoreclr.so
//  appDomainFriendlyName   - Friendly name of the app domain that will be created to execute the assembly
//  propertyCount           - Number of properties (elements of the following two arguments)
//  propertyKeys            - Keys of properties of the app domain
//  propertyValues          - Values of properties of the app domain
//  argc                    - Number of arguments passed to the executed assembly
//  argv                    - Array of arguments passed to the executed assembly
//  managedAssemblyPath     - Path of the managed assembly to execute
//  exitCode                - Exit code returned by the executed assembly
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
extern "C"
HRESULT ExecuteAssembly(
            LPCSTR exePath,
            LPCSTR coreClrPath,
            LPCSTR appDomainFriendlyName,
            int propertyCount,
            LPCSTR* propertyKeys,
            LPCSTR* propertyValues,
            int argc,
            LPCSTR* argv,
            LPCSTR managedAssemblyPath,
            DWORD* exitCode)
{
    *exitCode = 0;

    DWORD error = PAL_InitializeCoreCLR(exePath, coreClrPath, true);
    HRESULT hr = HRESULT_FROM_WIN32(error);

    // If PAL initialization failed, then we should return right away and avoid
    // calling any other APIs because they can end up calling into the PAL layer again.
    if (FAILED(hr))
    {
        return hr;
    }

    ReleaseHolder<ICLRRuntimeHost2> host;

    hr = CorHost2::CreateObject(IID_ICLRRuntimeHost2, (void**)&host);
    IfFailRet(hr);

    hr = host->SetStartupFlags((STARTUP_FLAGS)
                               (STARTUP_FLAGS::STARTUP_LOADER_OPTIMIZATION_SINGLE_DOMAIN |
                                STARTUP_FLAGS::STARTUP_SINGLE_APPDOMAIN));
    IfFailRet(hr);

    hr = host->Start();
    IfFailRet(hr);
    
    ConstWStringHolder appDomainFriendlyNameW = StringToUnicode(appDomainFriendlyName);
    
    ConstWStringArrayHolder propertyKeysW;
    propertyKeysW.Set(StringArrayToUnicode(propertyCount, propertyKeys), propertyCount);
    
    ConstWStringArrayHolder propertyValuesW;
    propertyValuesW.Set(StringArrayToUnicode(propertyCount, propertyValues), propertyCount);

    DWORD domainId;

    hr = host->CreateAppDomainWithManager(
        appDomainFriendlyNameW,
        // Flags:
        // APPDOMAIN_ENABLE_PLATFORM_SPECIFIC_APPS
        // - By default CoreCLR only allows platform neutral assembly to be run. To allow
        //   assemblies marked as platform specific, include this flag
        //
        // APPDOMAIN_ENABLE_PINVOKE_AND_CLASSIC_COMINTEROP
        // - Allows sandboxed applications to make P/Invoke calls and use COM interop
        //
        // APPDOMAIN_SECURITY_SANDBOXED
        // - Enables sandboxing. If not set, the app is considered full trust
        //
        // APPDOMAIN_IGNORE_UNHANDLED_EXCEPTION
        // - Prevents the application from being torn down if a managed exception is unhandled
        //
        APPDOMAIN_ENABLE_PLATFORM_SPECIFIC_APPS |
        APPDOMAIN_ENABLE_PINVOKE_AND_CLASSIC_COMINTEROP,
        NULL,                    // Name of the assembly that contains the AppDomainManager implementation
        NULL,                    // The AppDomainManager implementation type name
        propertyCount,
        propertyKeysW,
        propertyValuesW,
        &domainId);
    IfFailRet(hr);

    ConstWStringArrayHolder argvW;
    argvW.Set(StringArrayToUnicode(argc, argv), argc);
    
    ConstWStringHolder managedAssemblyPathW = StringToUnicode(managedAssemblyPath);

    hr = host->ExecuteAssembly(domainId, managedAssemblyPathW, argc, argvW, exitCode);
    IfFailRet(hr);

    hr = host->UnloadAppDomain(domainId,
                               true); // Wait until done
    IfFailRet(hr);

    hr = host->Stop();

    // The PAL_Terminate is not called here since it would terminate the current process.

    return hr;
}

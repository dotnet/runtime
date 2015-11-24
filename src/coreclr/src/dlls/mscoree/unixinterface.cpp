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

typedef int (STDMETHODCALLTYPE *HostMain)(
    const int argc,
    const wchar_t** argv
    );

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

static void ExtractStartupFlagsAndConvertToUnicode(
    const char** propertyKeys,
    const char** propertyValues,
    int* propertyCountRef,
    STARTUP_FLAGS* startupFlagsRef,
    LPCWSTR** propertyKeysWRef,
    LPCWSTR** propertyValuesWRef)
{
    int propertyCount = *propertyCountRef;

    LPCWSTR* propertyKeysW = new (nothrow) LPCWSTR[propertyCount];
    ASSERTE_ALL_BUILDS(propertyKeysW != nullptr);

    LPCWSTR* propertyValuesW = new (nothrow) LPCWSTR[propertyCount];
    ASSERTE_ALL_BUILDS(propertyValuesW != nullptr);

    // Extract the startup flags from the properties, and convert the remaining properties into unicode
    STARTUP_FLAGS startupFlags =
        static_cast<STARTUP_FLAGS>(
            STARTUP_FLAGS::STARTUP_LOADER_OPTIMIZATION_SINGLE_DOMAIN |
            STARTUP_FLAGS::STARTUP_SINGLE_APPDOMAIN);
    int propertyCountW = 0;
    for (int propertyIndex = 0; propertyIndex < propertyCount; ++propertyIndex)
    {
        auto SetFlagValue = [&](STARTUP_FLAGS flag)
        {
            if (strcmp(propertyValues[propertyIndex], "0") == 0)
            {
                startupFlags = static_cast<STARTUP_FLAGS>(startupFlags & ~flag);
            }
            else if (strcmp(propertyValues[propertyIndex], "1") == 0)
            {
                startupFlags = static_cast<STARTUP_FLAGS>(startupFlags | flag);
            }
        };

        if (strcmp(propertyKeys[propertyIndex], "CONCURRENT_GC") == 0)
        {
            SetFlagValue(STARTUP_CONCURRENT_GC);
        }
        else if (strcmp(propertyKeys[propertyIndex], "SERVER_GC") == 0)
        {
            SetFlagValue(STARTUP_SERVER_GC);
        }
        else if (strcmp(propertyKeys[propertyIndex], "HOARD_GC_VM") == 0)
        {
            SetFlagValue(STARTUP_HOARD_GC_VM);
        }
        else if (strcmp(propertyKeys[propertyIndex], "TRIM_GC_COMMIT") == 0)
        {
            SetFlagValue(STARTUP_TRIM_GC_COMMIT);
        }
        else
        {
            // This is not a CoreCLR startup flag, convert it to unicode and preserve it for returning
            propertyKeysW[propertyCountW] = StringToUnicode(propertyKeys[propertyIndex]);
            propertyValuesW[propertyCountW] = StringToUnicode(propertyValues[propertyIndex]);
            ++propertyCountW;
        }
    }

    for (int propertyIndex = propertyCountW; propertyIndex < propertyCount; ++propertyIndex)
    {
        propertyKeysW[propertyIndex] = nullptr;
        propertyValuesW[propertyIndex] = nullptr;
    }

    *propertyCountRef = propertyCountW;
    *startupFlagsRef = startupFlags;
    *propertyKeysWRef = propertyKeysW;
    *propertyValuesWRef = propertyValuesW;
}

//
// Initialize the CoreCLR. Creates and starts CoreCLR host and creates an app domain
//
// Parameters:
//  exePath                 - Absolute path of the executable that invoked the ExecuteAssembly
//  appDomainFriendlyName   - Friendly name of the app domain that will be created to execute the assembly
//  propertyCount           - Number of properties (elements of the following two arguments)
//  propertyKeys            - Keys of properties of the app domain
//  propertyValues          - Values of properties of the app domain
//  hostHandle              - Output parameter, handle of the created host
//  domainId                - Output parameter, id of the created app domain 
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
extern "C"
int coreclr_initialize(
            const char* exePath,
            const char* appDomainFriendlyName,
            int propertyCount,
            const char** propertyKeys,
            const char** propertyValues,
            void** hostHandle,
            unsigned int* domainId)
{
    HRESULT hr;
#ifdef FEATURE_PAL
    DWORD error = PAL_InitializeCoreCLR(exePath);
    hr = HRESULT_FROM_WIN32(error);

    // If PAL initialization failed, then we should return right away and avoid
    // calling any other APIs because they can end up calling into the PAL layer again.
    if (FAILED(hr))
    {
        return hr;
    }
#endif

    ReleaseHolder<ICLRRuntimeHost2> host;

    hr = CorHost2::CreateObject(IID_ICLRRuntimeHost2, (void**)&host);
    IfFailRet(hr);

    ConstWStringHolder appDomainFriendlyNameW = StringToUnicode(appDomainFriendlyName);

    STARTUP_FLAGS startupFlags;
    LPCWSTR* propertyKeysWTemp;
    LPCWSTR* propertyValuesWTemp;
    ExtractStartupFlagsAndConvertToUnicode(
        propertyKeys,
        propertyValues,
        &propertyCount,
        &startupFlags,
        &propertyKeysWTemp,
        &propertyValuesWTemp);
    
    ConstWStringArrayHolder propertyKeysW;
    propertyKeysW.Set(propertyKeysWTemp, propertyCount);
    
    ConstWStringArrayHolder propertyValuesW;
    propertyValuesW.Set(propertyValuesWTemp, propertyCount);

    hr = host->SetStartupFlags(startupFlags);
    IfFailRet(hr);

    hr = host->Start();
    IfFailRet(hr);

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
        APPDOMAIN_ENABLE_PINVOKE_AND_CLASSIC_COMINTEROP |
        APPDOMAIN_DISABLE_TRANSPARENCY_ENFORCEMENT,
        NULL,                    // Name of the assembly that contains the AppDomainManager implementation
        NULL,                    // The AppDomainManager implementation type name
        propertyCount,
        propertyKeysW,
        propertyValuesW,
        (DWORD *)domainId);

    if (SUCCEEDED(hr))
    {
        host.SuppressRelease();
        *hostHandle = host;
    }

    return hr;
}

//
// Shutdown CoreCLR. It unloads the app domain and stops the CoreCLR host.
//
// Parameters:
//  hostHandle              - Handle of the host
//  domainId                - Id of the domain 
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
extern "C"
int coreclr_shutdown(
            void* hostHandle,
            unsigned int domainId)
{
    ReleaseHolder<ICLRRuntimeHost2> host(reinterpret_cast<ICLRRuntimeHost2*>(hostHandle));
    HRESULT hr = host->UnloadAppDomain(domainId,
                                       true); // Wait until done
    IfFailRet(hr);

    hr = host->Stop();

    // The PAL_Terminate is not called here since it would terminate the current process.

    return hr;
}

//
// Create a native callable delegate for a managed method.
//
// Parameters:
//  hostHandle              - Handle of the host
//  domainId                - Id of the domain 
//  entryPointAssemblyName  - Name of the assembly which holds the custom entry point
//  entryPointTypeName      - Name of the type which holds the custom entry point
//  entryPointMethodName    - Name of the method which is the custom entry point
//  delegate                - Output parameter, the function stores a pointer to the delegate at the specified address
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
extern "C"
int coreclr_create_delegate(
            void* hostHandle,
            unsigned int domainId,
            const char* entryPointAssemblyName,
            const char* entryPointTypeName,
            const char* entryPointMethodName,
            void** delegate)
{
    ICLRRuntimeHost2* host = reinterpret_cast<ICLRRuntimeHost2*>(hostHandle);

    ConstWStringHolder entryPointAssemblyNameW = StringToUnicode(entryPointAssemblyName);
    ConstWStringHolder entryPointTypeNameW = StringToUnicode(entryPointTypeName);
    ConstWStringHolder entryPointMethodNameW = StringToUnicode(entryPointMethodName);

    HRESULT hr = host->CreateDelegate(
                            domainId,
                            entryPointAssemblyNameW,
                            entryPointTypeNameW,
                            entryPointMethodNameW,
                            (INT_PTR*)delegate);
    
    return hr;
}

//
// Execute a managed assembly with given arguments
//
// Parameters:
//  hostHandle              - Handle of the host
//  domainId                - Id of the domain 
//  argc                    - Number of arguments passed to the executed assembly
//  argv                    - Array of arguments passed to the executed assembly
//  managedAssemblyPath     - Path of the managed assembly to execute (or NULL if using a custom entrypoint).
//  exitCode                - Exit code returned by the executed assembly
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
extern "C"
int coreclr_execute_assembly(
            void* hostHandle,
            unsigned int domainId,
            int argc,
            const char** argv,
            const char* managedAssemblyPath,
            unsigned int* exitCode)
{
    if (exitCode == NULL)
    {
        return HRESULT_FROM_WIN32(ERROR_INVALID_PARAMETER);
    }
    *exitCode = -1;

    ICLRRuntimeHost2* host = reinterpret_cast<ICLRRuntimeHost2*>(hostHandle);

    ConstWStringArrayHolder argvW;
    argvW.Set(StringArrayToUnicode(argc, argv), argc);
    
    ConstWStringHolder managedAssemblyPathW = StringToUnicode(managedAssemblyPath);

    HRESULT hr = host->ExecuteAssembly(domainId, managedAssemblyPathW, argc, argvW, (DWORD *)exitCode);
    IfFailRet(hr);

    return hr;
}

#ifdef PLATFORM_UNIX
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
//  managedAssemblyPath     - Path of the managed assembly to execute (or NULL if using a custom entrypoint).
//  enntyPointAssemblyName  - Name of the assembly which holds the custom entry point (or NULL to use managedAssemblyPath).
//  entryPointTypeName      - Name of the type which holds the custom entry point (or NULL to use managedAssemblyPath).
//  entryPointMethodName    - Name of the method which is the custom entry point (or NULL to use managedAssemblyPath).
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
            LPCSTR entryPointAssemblyName,
            LPCSTR entryPointTypeName,
            LPCSTR entryPointMethodName,
            DWORD* exitCode)
{
    if (exitCode == NULL)
    {
        return HRESULT_FROM_WIN32(ERROR_INVALID_PARAMETER);
    }
    *exitCode = -1;

    ReleaseHolder<ICLRRuntimeHost2> host; //(reinterpret_cast<ICLRRuntimeHost2*>(hostHandle));
    DWORD domainId;

    HRESULT hr = coreclr_initialize(exePath, appDomainFriendlyName, propertyCount, propertyKeys, propertyValues, &host, &domainId);
    IfFailRet(hr);

    ConstWStringArrayHolder argvW;
    argvW.Set(StringArrayToUnicode(argc, argv), argc);
    
    if (entryPointAssemblyName == NULL || entryPointTypeName == NULL || entryPointMethodName == NULL)
    {
        ConstWStringHolder managedAssemblyPathW = StringToUnicode(managedAssemblyPath);

        hr = host->ExecuteAssembly(domainId, managedAssemblyPathW, argc, argvW, exitCode);
        IfFailRet(hr);
    }
    else
    {
        ConstWStringHolder entryPointAssemblyNameW = StringToUnicode(entryPointAssemblyName);
        ConstWStringHolder entryPointTypeNameW = StringToUnicode(entryPointTypeName);
        ConstWStringHolder entryPointMethodNameW = StringToUnicode(entryPointMethodName);

        HostMain pHostMain;

        hr = host->CreateDelegate(
            domainId,
            entryPointAssemblyNameW,
            entryPointTypeNameW,
            entryPointMethodNameW,
            (INT_PTR*)&pHostMain);
        
        IfFailRet(hr);

        *exitCode = pHostMain(argc, argvW);
    }

    hr = coreclr_shutdown(host, domainId);

    return hr;
}
#endif

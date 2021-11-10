// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//*****************************************************************************
// unixinterface.cpp
//
// Implementation for the interface exposed by libcoreclr.so
//

//*****************************************************************************

#include "stdafx.h"
#include <utilcode.h>
#include <corhost.h>
#include <configuration.h>
#ifdef FEATURE_GDBJIT
#include "../../vm/gdbjithelpers.h"
#endif // FEATURE_GDBJIT
#include "bundle.h"
#include "pinvokeoverride.h"

#define ASSERTE_ALL_BUILDS(expr) _ASSERTE_ALL_BUILDS(__FILE__, (expr))

// Holder for const wide strings
typedef NewArrayHolder<const WCHAR> ConstWStringHolder;

// Specifies whether coreclr is embedded or standalone
extern bool g_coreclr_embedded;

// Specifies whether hostpolicy is embedded in executable or standalone
extern bool g_hostpolicy_embedded;

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
    int length = MultiByteToWideChar(CP_UTF8, 0, str, -1, NULL, 0);
    ASSERTE_ALL_BUILDS(length != 0);

    LPWSTR result = new (nothrow) WCHAR[length];
    ASSERTE_ALL_BUILDS(result != NULL);

    length = MultiByteToWideChar(CP_UTF8, 0, str, -1, result, length);
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

static void InitializeStartupFlags(STARTUP_FLAGS* startupFlagsRef)
{
    STARTUP_FLAGS startupFlags = static_cast<STARTUP_FLAGS>(
            STARTUP_FLAGS::STARTUP_LOADER_OPTIMIZATION_SINGLE_DOMAIN |
            STARTUP_FLAGS::STARTUP_SINGLE_APPDOMAIN);

    if (Configuration::GetKnobBooleanValue(W("System.GC.Concurrent"), CLRConfig::UNSUPPORTED_gcConcurrent))
    {
        startupFlags = static_cast<STARTUP_FLAGS>(startupFlags | STARTUP_CONCURRENT_GC);
    }
    if (Configuration::GetKnobBooleanValue(W("System.GC.Server"), CLRConfig::UNSUPPORTED_gcServer))
    {
        startupFlags = static_cast<STARTUP_FLAGS>(startupFlags | STARTUP_SERVER_GC);
    }
    if (Configuration::GetKnobBooleanValue(W("System.GC.RetainVM"), CLRConfig::UNSUPPORTED_GCRetainVM))
    {
        startupFlags = static_cast<STARTUP_FLAGS>(startupFlags | STARTUP_HOARD_GC_VM);
    }

    *startupFlagsRef = startupFlags;
}

static void ConvertConfigPropertiesToUnicode(
    const char** propertyKeys,
    const char** propertyValues,
    int propertyCount,
    LPCWSTR** propertyKeysWRef,
    LPCWSTR** propertyValuesWRef,
    BundleProbeFn** bundleProbe,
    PInvokeOverrideFn** pinvokeOverride,
    bool* hostPolicyEmbedded)
{
    LPCWSTR* propertyKeysW = new (nothrow) LPCWSTR[propertyCount];
    ASSERTE_ALL_BUILDS(propertyKeysW != nullptr);

    LPCWSTR* propertyValuesW = new (nothrow) LPCWSTR[propertyCount];
    ASSERTE_ALL_BUILDS(propertyValuesW != nullptr);

    for (int propertyIndex = 0; propertyIndex < propertyCount; ++propertyIndex)
    {
        propertyKeysW[propertyIndex] = StringToUnicode(propertyKeys[propertyIndex]);
        propertyValuesW[propertyIndex] = StringToUnicode(propertyValues[propertyIndex]);

        if (strcmp(propertyKeys[propertyIndex], "BUNDLE_PROBE") == 0)
        {
            // If this application is a single-file bundle, the bundle-probe callback 
            // is passed in as the value of "BUNDLE_PROBE" property (encoded as a string).
            *bundleProbe = (BundleProbeFn*)_wcstoui64(propertyValuesW[propertyIndex], nullptr, 0);
        }
        else if (strcmp(propertyKeys[propertyIndex], "PINVOKE_OVERRIDE") == 0)
        {
            // If host provides a PInvoke override (typically in a single-file bundle),
            // the override callback is passed in as the value of "PINVOKE_OVERRIDE" property (encoded as a string).
            *pinvokeOverride = (PInvokeOverrideFn*)_wcstoui64(propertyValuesW[propertyIndex], nullptr, 0);
        }
        else if (strcmp(propertyKeys[propertyIndex], "HOSTPOLICY_EMBEDDED") == 0)
        {
            // The HOSTPOLICY_EMBEDDED property indicates if the executable has hostpolicy statically linked in
            *hostPolicyEmbedded = (wcscmp(propertyValuesW[propertyIndex], W("true")) == 0);
        }
    }

    *propertyKeysWRef = propertyKeysW;
    *propertyValuesWRef = propertyValuesW;
}

#ifdef FEATURE_GDBJIT
GetInfoForMethodDelegate getInfoForMethodDelegate = NULL;
extern "C" int coreclr_create_delegate(void*, unsigned int, const char*, const char*, const char*, void**);
#endif //FEATURE_GDBJIT

//
// Initialize the CoreCLR. Creates and starts CoreCLR host and creates an app domain
//
// Parameters:
//  exePath                 - Absolute path of the executable that invoked the ExecuteAssembly (the native host application)
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
DLLEXPORT
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

    LPCWSTR* propertyKeysW;
    LPCWSTR* propertyValuesW;
    BundleProbeFn* bundleProbe = nullptr;
    bool hostPolicyEmbedded = false;
    PInvokeOverrideFn* pinvokeOverride = nullptr;

    ConvertConfigPropertiesToUnicode(
        propertyKeys,
        propertyValues,
        propertyCount,
        &propertyKeysW,
        &propertyValuesW,
        &bundleProbe,
        &pinvokeOverride,
        &hostPolicyEmbedded);

#ifdef TARGET_UNIX
    DWORD error = PAL_InitializeCoreCLR(exePath, g_coreclr_embedded);
    hr = HRESULT_FROM_WIN32(error);

    // If PAL initialization failed, then we should return right away and avoid
    // calling any other APIs because they can end up calling into the PAL layer again.
    if (FAILED(hr))
    {
        return hr;
    }
#endif

    g_hostpolicy_embedded = hostPolicyEmbedded;

    if (pinvokeOverride != nullptr)
    {
        PInvokeOverride::SetPInvokeOverride(pinvokeOverride, PInvokeOverride::Source::RuntimeConfiguration);
    }

    ReleaseHolder<ICLRRuntimeHost4> host;

    hr = CorHost2::CreateObject(IID_ICLRRuntimeHost4, (void**)&host);
    IfFailRet(hr);

    ConstWStringHolder appDomainFriendlyNameW = StringToUnicode(appDomainFriendlyName);

    if (bundleProbe != nullptr)
    {
        static Bundle bundle(exePath, bundleProbe);
        Bundle::AppBundle = &bundle;
    }

    // This will take ownership of propertyKeysWTemp and propertyValuesWTemp
    Configuration::InitializeConfigurationKnobs(propertyCount, propertyKeysW, propertyValuesW);

    STARTUP_FLAGS startupFlags;
    InitializeStartupFlags(&startupFlags);

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
#ifdef FEATURE_GDBJIT
        HRESULT createDelegateResult;
        createDelegateResult = coreclr_create_delegate(*hostHandle,
                                                       *domainId,
                                                       "SOS.NETCore",
                                                       "SOS.SymbolReader",
                                                       "GetInfoForMethod",
                                                       (void**)&getInfoForMethodDelegate);

#if defined(_DEBUG)
        if (!SUCCEEDED(createDelegateResult))
        {
            fprintf(stderr,
                    "Can't create delegate for 'SOS.SymbolReader.GetInfoForMethod' "
                    "method - status: 0x%08x\n", createDelegateResult);
        }
#endif // _DEBUG

#endif
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
DLLEXPORT
int coreclr_shutdown(
            void* hostHandle,
            unsigned int domainId)
{
    ReleaseHolder<ICLRRuntimeHost4> host(reinterpret_cast<ICLRRuntimeHost4*>(hostHandle));

    HRESULT hr = host->UnloadAppDomain(domainId, true); // Wait until done
    IfFailRet(hr);

    hr = host->Stop();

#ifdef TARGET_UNIX
    PAL_Shutdown();
#endif

    return hr;
}

//
// Shutdown CoreCLR. It unloads the app domain and stops the CoreCLR host.
//
// Parameters:
//  hostHandle              - Handle of the host
//  domainId                - Id of the domain
//  latchedExitCode         - Latched exit code after domain unloaded
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
extern "C"
DLLEXPORT
int coreclr_shutdown_2(
            void* hostHandle,
            unsigned int domainId,
            int* latchedExitCode)
{
    ReleaseHolder<ICLRRuntimeHost4> host(reinterpret_cast<ICLRRuntimeHost4*>(hostHandle));

    HRESULT hr = host->UnloadAppDomain2(domainId, true, latchedExitCode); // Wait until done
    IfFailRet(hr);

    hr = host->Stop();

#ifdef TARGET_UNIX
    PAL_Shutdown();
#endif

    return hr;
}

//
// Create a native callable function pointer for a managed method.
//
// Parameters:
//  hostHandle              - Handle of the host
//  domainId                - Id of the domain
//  entryPointAssemblyName  - Name of the assembly which holds the custom entry point
//  entryPointTypeName      - Name of the type which holds the custom entry point
//  entryPointMethodName    - Name of the method which is the custom entry point
//  delegate                - Output parameter, the function stores a native callable function pointer to the delegate at the specified address
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
extern "C"
DLLEXPORT
int coreclr_create_delegate(
            void* hostHandle,
            unsigned int domainId,
            const char* entryPointAssemblyName,
            const char* entryPointTypeName,
            const char* entryPointMethodName,
            void** delegate)
{
    ICLRRuntimeHost4* host = reinterpret_cast<ICLRRuntimeHost4*>(hostHandle);

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
DLLEXPORT
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

    ICLRRuntimeHost4* host = reinterpret_cast<ICLRRuntimeHost4*>(hostHandle);

    ConstWStringArrayHolder argvW;
    argvW.Set(StringArrayToUnicode(argc, argv), argc);

    ConstWStringHolder managedAssemblyPathW = StringToUnicode(managedAssemblyPath);

    HRESULT hr = host->ExecuteAssembly(domainId, managedAssemblyPathW, argc, argvW, (DWORD *)exitCode);
    IfFailRet(hr);

    return hr;
}

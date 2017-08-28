//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


//
// A simple CoreCLR host that runs on CoreSystem.
//

#include "windows.h"
#include <HString.h>
#include <WinString.h>
#include "UWPHost.h"

const DWORD DefaultADID = 1;

bool UWPHost::m_Inited = false;
CRITICAL_SECTION UWPHost::m_critSec;

extern "C" BOOLEAN WINAPI DllMain(HINSTANCE hDllHandle, DWORD nReason, LPVOID Reserved)
{
    switch (nReason)
    {
        case DLL_PROCESS_ATTACH:
            InitializeCriticalSection(&UWPHost::m_critSec);
            break;
        case DLL_PROCESS_DETACH:
            DeleteCriticalSection(&UWPHost::m_critSec);
            break;
    }
    return TRUE;
}

class CritSecHolder
{
private:
    CRITICAL_SECTION* value;
public:
    CritSecHolder(CRITICAL_SECTION* data)
    {
        value = data;
        EnterCriticalSection(value);
    }
    ~CritSecHolder()
    {
        LeaveCriticalSection(value);
    }
};

HRESULT UWPHost::LoadAndStartCoreCLRRuntime()
{
    HRESULT hr = S_OK;
    HostEnvironment hostEnvironment;

    IfFailRet(hostEnvironment.Initialize());

    if (!hostEnvironment.CanDebugUWPApps())
    {
        return E_FAIL;
    }

    if (!hostEnvironment.IsCoreCLRLoaded())
    {
        return E_FAIL;
    }

    // 
    // Setup arguments for the runtime
    //

    wchar_t *appPath = (wchar_t *)hostEnvironment.GetPackageRoot();
    wchar_t *appPaths = (wchar_t *)hostEnvironment.GetAppPaths();

    // Construct native search directory paths
    StringBuffer sbNativeDllSearchDirs;
    wchar_t * coreCLRInstallPath = hostEnvironment.GetCoreCLRInstallPath();
    sbNativeDllSearchDirs.Append(coreCLRInstallPath, wcslen(coreCLRInstallPath));
    sbNativeDllSearchDirs.Append(L";", 1);
    sbNativeDllSearchDirs.Append(appPaths, wcslen(appPaths));
    const wchar_t *nativeDllSearchDirs = sbNativeDllSearchDirs.CStr();

    // Check for app local WinMetadata
    wchar_t appLocalWinMetadata[MAX_PATH] = L"";
    wcscpy_s(appLocalWinMetadata, appPath);
    wcscat_s(appLocalWinMetadata, MAX_PATH, L"\\WinMetadata");
    DWORD dwFileAttributes = ::GetFileAttributes(appLocalWinMetadata);
    bool appLocalWinMetadataExists = dwFileAttributes != INVALID_FILE_ATTRIBUTES && (dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY);
    if (!appLocalWinMetadataExists)
    {
        memset(appLocalWinMetadata,0,MAX_PATH*sizeof(wchar_t));
    }

    // Start the CoreCLR
    m_CLRRuntimeHost = hostEnvironment.GetCLRRuntimeHost(hr);
    if (!m_CLRRuntimeHost) {
        return hr;
    }

    {
        CritSecHolder guard(&UWPHost::m_critSec);
        if(m_Inited)
        {
            return hr;                
        }

        // Default startup flags
        hr = m_CLRRuntimeHost->SetStartupFlags((STARTUP_FLAGS)
            (STARTUP_FLAGS::STARTUP_LOADER_OPTIMIZATION_SINGLE_DOMAIN | 
            STARTUP_FLAGS::STARTUP_SINGLE_APPDOMAIN|
            STARTUP_FLAGS::STARTUP_CONCURRENT_GC|
            STARTUP_FLAGS::STARTUP_APPX_APP_MODEL)
            ); 

        // 
        // Authenticate with either
        // CORECLR_HOST_AUTHENTICATION_KEY  or
        // CORECLR_HOST_AUTHENTICATION_KEY_NONGEN  
        //
        IfFailRet(m_CLRRuntimeHost->Authenticate(CORECLR_HOST_AUTHENTICATION_KEY)); 

        IfFailRet(m_CLRRuntimeHost->Start());

        //-------------------------------------------------------------
    
        // Create an AppDomain
        //
        // Allowed property names:
        // APPBASE
        // - The base path of the application from which the exe and other assemblies will be loaded
        //
        // TRUSTED_PLATFORM_ASSEMBLIES
        // - The list of complete paths to each of the fully trusted assemblies
        //
        // APP_PATHS
        // - The list of paths which will be probed by the assembly loader
        //
        // NATIVE_DLL_SEARCH_DIRECTORIES
        // - The list of paths that will be probed for native DLLs called by PInvoke
        //
        // APP_NI_PATHS
        // - The list of paths which will be probed for NGEN images by the assembly loader
        //
        // PLATFORM_RESOURCE_ROOTS
        // - The list of paths which will be probed for loading the satellite assemblies
        //
        // APP_LOCAL_WINMETADATA
        // - The path where the app-local .winmd file can be found.
        //
        // N.B.: that we're not passing APP_NI_PATHS, since UWP F5 is not expected to have native images and 
        // relying on the following **ASSUMPTION**:
        // fallback is supposed to have the native images in the app package root, 
        // and as with the same assembly file name of the IL image
        // 
        const wchar_t *property_keys[] = { 
            L"TRUSTED_PLATFORM_ASSEMBLIES",
            L"APP_PATHS",
            L"NATIVE_DLL_SEARCH_DIRECTORIES",
            L"APP_LOCAL_WINMETADATA"
        };
        const wchar_t *property_values[] = { 
            // TRUSTED_PLATFORM_ASSEMBLIES
            hostEnvironment.GetTpaList(),
            // APP_PATHS
            appPaths,
            // NATIVE_DLL_SEARCH_DIRECTORIES
            nativeDllSearchDirs,
            // APP_LOCAL_WINMETADATA
            appLocalWinMetadata
        };


        DWORD domainId;

        hr = m_CLRRuntimeHost->CreateAppDomainWithManager(
            L"CoreCLR_UWP_Domain",   // The friendly name of the AppDomain
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
            NULL,                // Name of the assembly that contains the AppDomainManager implementation
            NULL,                    // The AppDomainManager implementation type name
            sizeof(property_keys)/sizeof(wchar_t*),  // The number of properties
            property_keys,
            property_values,
            &domainId);
    
        IfFailRet(hr);
        m_Inited = true;
    }
    return hr;
} // End of UWPHost::LoadAndStartCoreCLRRuntime()


HRESULT ExecuteAssembly(_In_z_ wchar_t *entryPointAssemblyFileName, int argc, LPCWSTR* pArgv, DWORD *pExitCode)
{

    HRESULT hr = S_OK;
    
    UWPHost uwpHost;
    IfFailRet(uwpHost.LoadAndStartCoreCLRRuntime());
    
    DWORD domainId;
    ICLRRuntimeHost2 *host = uwpHost.GetCLRRuntimeHost();
    IfFailGo(host->GetCurrentAppDomainId(&domainId));

    // Execute the assembly in question
    IfFailGo(host->ExecuteAssembly(domainId, entryPointAssemblyFileName, argc, pArgv, pExitCode));

    // Unload the AppDomain
    hr = host->UnloadAppDomain(
        domainId, 
        true);                          // Wait until done

    IfFailGo(hr);

    // Stop the runtime
    hr = host->Stop();
    // Release the reference to the host
    host->Release();

    return hr;
ErrExit:
    if (host != nullptr)
    {
        host->Stop();
        host->Release();
    }
    return hr;
}

HRESULT STDMETHODCALLTYPE DllGetActivationFactory(HSTRING activatableClassId, IActivationFactory** ppFactory)
{
    HRESULT hr = S_OK;
    UWPHost uwpHost;
    hr = uwpHost.LoadAndStartCoreCLRRuntime();
    if (FAILED(hr) && (hr != HOST_E_INVALIDOPERATION))
    {
        return hr;
    }

    ICLRRuntimeHost2 *host = uwpHost.GetCLRRuntimeHost();
    hr = host->DllGetActivationFactory(DefaultADID, WindowsGetStringRawBuffer(activatableClassId,NULL), ppFactory);
    host->Release();

    return hr;
}

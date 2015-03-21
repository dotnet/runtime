//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


//
// .A simple CoreCLR host that runs on CoreSystem.
//

#include "windows.h"
#include <stdio.h>
#include "mscoree.h"
#include <Logger.h>
#include "palclr.h"

// The name of the CoreCLR native runtime DLL.
static const wchar_t *coreCLRDll = W("CoreCLR.dll");

// The location where CoreCLR is expected to be installed. If CoreCLR.dll isn't
//  found in the same directory as the host, it will be looked for here.
static const wchar_t *coreCLRInstallDirectory = W("%windir%\\system32\\");

// Dynamically expanding string buffer to hold TPA list
class StringBuffer {
    wchar_t* m_buffer;
    size_t m_capacity;
    size_t m_length;

    StringBuffer(const StringBuffer&);
    StringBuffer& operator =(const StringBuffer&);

public:
    StringBuffer() : m_capacity(0), m_buffer(0), m_length(0) {
    }

    ~StringBuffer() {
        delete[] m_buffer;
    }

    const wchar_t* CStr() const {
        return m_buffer;
    }

    void Append(const wchar_t* str, size_t strLen) {
        if (!m_buffer) {
            m_buffer = new wchar_t[4096];
            m_capacity = 4096;
        }
        if (m_length + strLen + 1 > m_capacity) {
            size_t newCapacity = m_capacity * 2;
            wchar_t* newBuffer = new wchar_t[newCapacity];
            wcsncpy_s(newBuffer, newCapacity, m_buffer, m_length);
            delete[] m_buffer;
            m_buffer = newBuffer;
            m_capacity = newCapacity;
        }
        wcsncpy_s(m_buffer + m_length, m_capacity - m_length, str, strLen);
        m_length += strLen;
    }
};

// Encapsulates the environment that CoreCLR will run in, including the TPALIST
class HostEnvironment 
{
    // The path to this module
    wchar_t m_hostPath[MAX_PATH];

    // The path to the directory containing this module
    wchar_t m_hostDirectoryPath[MAX_PATH];

    // The name of this module, without the path
    wchar_t *m_hostExeName;

    // The list of paths to the assemblies that will be trusted by CoreCLR
    StringBuffer m_tpaList;

    ICLRRuntimeHost2* m_CLRRuntimeHost;

    HMODULE m_coreCLRModule;

    Logger *m_log;


    // Attempts to load CoreCLR.dll from the given directory.
    // On success pins the dll, sets m_coreCLRDirectoryPath and returns the HMODULE.
    // On failure returns nullptr.
    HMODULE TryLoadCoreCLR(const wchar_t* directoryPath) {

        wchar_t coreCLRPath[MAX_PATH];
        wcscpy_s(coreCLRPath, directoryPath);
        wcscat_s(coreCLRPath, coreCLRDll);

        *m_log << W("Attempting to load: ") << coreCLRPath << Logger::endl;

        HMODULE result = ::LoadLibraryExW(coreCLRPath, NULL, 0);
        if (!result) {
            *m_log << W("Failed to load: ") << coreCLRPath << Logger::endl;
            *m_log << W("Error code: ") << GetLastError() << Logger::endl;
            return nullptr;
        }

        // Pin the module - CoreCLR.dll does not support being unloaded.
        HMODULE dummy_coreCLRModule;
        if (!::GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_PIN, coreCLRPath, &dummy_coreCLRModule)) {
            *m_log << W("Failed to pin: ") << coreCLRPath << Logger::endl;
            return nullptr;
        }

        wchar_t coreCLRLoadedPath[MAX_PATH];
        ::GetModuleFileNameW(result, coreCLRLoadedPath, MAX_PATH);

        *m_log << W("Loaded: ") << coreCLRLoadedPath << Logger::endl;

        return result;
    }

public:
    // The path to the directory that CoreCLR is in
    wchar_t m_coreCLRDirectoryPath[MAX_PATH];

    HostEnvironment(Logger *logger) 
        : m_log(logger), m_CLRRuntimeHost(nullptr) {

            // Discover the path to this exe's module. All other files are expected to be in the same directory.
            DWORD thisModuleLength = ::GetModuleFileNameW(::GetModuleHandleW(nullptr), m_hostPath, MAX_PATH);

            // Search for the last backslash in the host path.
            int lastBackslashIndex;
            for (lastBackslashIndex = thisModuleLength-1; lastBackslashIndex >= 0; lastBackslashIndex--) {
                if (m_hostPath[lastBackslashIndex] == W('\\')) {
                    break;
                }
            }

            // Copy the directory path
            ::wcsncpy_s(m_hostDirectoryPath, m_hostPath, lastBackslashIndex + 1);

            // Save the exe name
            m_hostExeName = m_hostPath + lastBackslashIndex + 1;

            *m_log << W("Host directory: ")  << m_hostDirectoryPath << Logger::endl;

            // Check for %CORE_ROOT% and try to load CoreCLR.dll from it if it is set
            wchar_t coreRoot[MAX_PATH];
            size_t outSize;
			m_coreCLRModule = NULL; // Initialize this here since we don't call TryLoadCoreCLR if CORE_ROOT is unset.
            if (_wgetenv_s(&outSize, coreRoot, MAX_PATH, W("CORE_ROOT")) == 0 && outSize > 0)
            {
                wcscat_s(coreRoot, MAX_PATH, W("\\"));
                m_coreCLRModule = TryLoadCoreCLR(coreRoot);
            }
            else
            {
                *m_log << W("CORE_ROOT not set; skipping") << Logger::endl;
                *m_log << W("You can set the environment variable CORE_ROOT to point to the path") << Logger::endl;
                *m_log << W("where CoreCLR.dll lives to help CoreRun.exe find it.") << Logger::endl;
            }

            // Try to load CoreCLR from the directory that coreRun is in
            if (!m_coreCLRModule) {
                m_coreCLRModule = TryLoadCoreCLR(m_hostDirectoryPath);
            }

            if (!m_coreCLRModule) {

                // Failed to load. Try to load from the well-known location.
                wchar_t coreCLRInstallPath[MAX_PATH];
                ::ExpandEnvironmentStringsW(coreCLRInstallDirectory, coreCLRInstallPath, MAX_PATH);
                m_coreCLRModule = TryLoadCoreCLR(coreCLRInstallPath);

            }

            if (m_coreCLRModule) {

                // Save the directory that CoreCLR was found in
                DWORD modulePathLength = ::GetModuleFileNameW(m_coreCLRModule, m_coreCLRDirectoryPath, MAX_PATH);

                // Search for the last backslash and terminate it there to keep just the directory path with trailing slash
                for (lastBackslashIndex = modulePathLength-1; lastBackslashIndex >= 0; lastBackslashIndex--) {
                    if (m_coreCLRDirectoryPath[lastBackslashIndex] == W('\\')) {
                        m_coreCLRDirectoryPath[lastBackslashIndex + 1] = W('\0');
                        break;
                    }
                }

            } else {
                *m_log << W("Unable to load ") << coreCLRDll << Logger::endl;
            }
    }

    ~HostEnvironment() {
        if(m_coreCLRModule) {
            // Free the module. This is done for completeness, but in fact CoreCLR.dll 
            // was pinned earlier so this call won't actually free it. The pinning is
            // done because CoreCLR does not support unloading.
            ::FreeLibrary(m_coreCLRModule);
        }
    }

    bool TPAListContainsFile(wchar_t* fileNameWithoutExtension, wchar_t** rgTPAExtensions, int countExtensions)
    {
        if (!m_tpaList.CStr()) return false;

        for (int iExtension = 0; iExtension < countExtensions; iExtension++)
        {
            wchar_t fileName[MAX_PATH];
            wcscpy_s(fileName, MAX_PATH, W("\\")); // So that we don't match other files that end with the current file name
            wcscat_s(fileName, MAX_PATH, fileNameWithoutExtension);
            wcscat_s(fileName, MAX_PATH, rgTPAExtensions[iExtension] + 1);
            wcscat_s(fileName, MAX_PATH, W(";")); // So that we don't match other files that begin with the current file name

            if (wcsstr(m_tpaList.CStr(), fileName))
            {
                return true;
            }
        }
        return false;
    }

    void RemoveExtensionAndNi(wchar_t* fileName)
    {
        // Remove extension, if it exists
        wchar_t* extension = wcsrchr(fileName, W('.')); 
        if (extension != NULL)
        {
            extension[0] = W('\0');

            // Check for .ni
            size_t len = wcslen(fileName);
            if (len > 3 &&
                fileName[len - 1] == W('i') &&
                fileName[len - 2] == W('n') &&
                fileName[len - 3] == W('.') )
            {
                fileName[len - 3] = W('\0');
            }
        }
    }

    void AddFilesFromDirectoryToTPAList(wchar_t* targetPath, wchar_t** rgTPAExtensions, int countExtensions)
    {
        *m_log << W("Adding assemblies from ") << targetPath << W(" to the TPA list") << Logger::endl;
        wchar_t assemblyPath[MAX_PATH];

        for (int iExtension = 0; iExtension < countExtensions; iExtension++)
        {
            wcscpy_s(assemblyPath, MAX_PATH, targetPath);

            const size_t dirLength = wcslen(targetPath);
            wchar_t* const fileNameBuffer = assemblyPath + dirLength;
            const size_t fileNameBufferSize = MAX_PATH - dirLength;

            wcscat_s(assemblyPath, rgTPAExtensions[iExtension]);
            WIN32_FIND_DATA data;
            HANDLE findHandle = FindFirstFile(assemblyPath, &data);

            if (findHandle != INVALID_HANDLE_VALUE) {
                do {
                    if (!(data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)) {
                        // It seems that CoreCLR doesn't always use the first instance of an assembly on the TPA list (ni's may be preferred
                        // over il, even if they appear later). So, only include the first instance of a simple assembly name to allow
                        // users the opportunity to override Framework assemblies by placing dlls in %CORE_LIBRARIES%

                        // ToLower for case-insensitive comparisons
                        wchar_t* fileNameChar = data.cFileName;
                        while (*fileNameChar)
                        {
                            *fileNameChar = towlower(*fileNameChar);
                            fileNameChar++;
                        }

                        // Remove extension
                        wchar_t fileNameWithoutExtension[MAX_PATH];
                        wcscpy_s(fileNameWithoutExtension, MAX_PATH, data.cFileName);

                        RemoveExtensionAndNi(fileNameWithoutExtension);

                        // Add to the list if not already on it
                        if (!TPAListContainsFile(fileNameWithoutExtension, rgTPAExtensions, countExtensions))
                        {
                            const size_t fileLength = wcslen(data.cFileName);
                            const size_t assemblyPathLength = dirLength + fileLength;
                            wcsncpy_s(fileNameBuffer, fileNameBufferSize, data.cFileName, fileLength);
                            m_tpaList.Append(assemblyPath, assemblyPathLength);
                            m_tpaList.Append(W(";"), 1);
                        }
                        else
                        {
                            *m_log << W("Not adding ") << targetPath << data.cFileName << W(" to the TPA list because another file with the same name is already present on the list") << Logger::endl;
                        }
                    }
                } while (0 != FindNextFile(findHandle, &data));

                FindClose(findHandle);
            }
        }
    }

    // Returns the semicolon-separated list of paths to runtime dlls that are considered trusted.
    // On first call, scans the coreclr directory for dlls and adds them all to the list.
    const wchar_t * GetTpaList() {
        if (!m_tpaList.CStr()) {
            wchar_t *rgTPAExtensions[] = {
                        W("*.ni.dll"),		// Probe for .ni.dll first so that it's preferred if ni and il coexist in the same dir
                        W("*.dll"),
                        W("*.ni.exe"),
                        W("*.exe"),
                        W("*.ni.winmd")
                        W("*.winmd")
                        };

            // Add files from %CORE_LIBRARIES% if specified
            wchar_t coreLibraries[MAX_PATH];
            size_t outSize;
            if (_wgetenv_s(&outSize, coreLibraries, MAX_PATH, W("CORE_LIBRARIES")) == 0 && outSize > 0)
            {
                wcscat_s(coreLibraries, MAX_PATH, W("\\"));
                AddFilesFromDirectoryToTPAList(coreLibraries, rgTPAExtensions, _countof(rgTPAExtensions));
            }
            else
            {
                *m_log << W("CORE_LIBRARIES not set; skipping") << Logger::endl;
                *m_log << W("You can set the environment variable CORE_LIBRARIES to point to a") << Logger::endl;
                *m_log << W("path containing additional platform assemblies,") << Logger::endl;
            }

            AddFilesFromDirectoryToTPAList(m_coreCLRDirectoryPath, rgTPAExtensions, _countof(rgTPAExtensions));
        }

        return m_tpaList.CStr();
    }

    // Returns the path to the host module
    const wchar_t * GetHostPath() {
        return m_hostPath;
    }

    // Returns the path to the host module
    const wchar_t * GetHostExeName() {
        return m_hostExeName;
    }

    // Returns the ICLRRuntimeHost2 instance, loading it from CoreCLR.dll if necessary, or nullptr on failure.
    ICLRRuntimeHost2* GetCLRRuntimeHost() {
        if (!m_CLRRuntimeHost) {

            if (!m_coreCLRModule) {
                *m_log << W("Unable to load ") << coreCLRDll << Logger::endl;
                return nullptr;
            }

            *m_log << W("Finding GetCLRRuntimeHost(...)") << Logger::endl;

            FnGetCLRRuntimeHost pfnGetCLRRuntimeHost = 
                (FnGetCLRRuntimeHost)::GetProcAddress(m_coreCLRModule, "GetCLRRuntimeHost");

            if (!pfnGetCLRRuntimeHost) {
                *m_log << W("Failed to find function GetCLRRuntimeHost in ") << coreCLRDll << Logger::endl;
                return nullptr;
            }

            *m_log << W("Calling GetCLRRuntimeHost(...)") << Logger::endl;

            HRESULT hr = pfnGetCLRRuntimeHost(IID_ICLRRuntimeHost2, (IUnknown**)&m_CLRRuntimeHost);
            if (FAILED(hr)) {
                *m_log << W("Failed to get ICLRRuntimeHost2 interface. ERRORCODE: ") << Logger::hresult << hr << Logger::endl;
                return nullptr;
            }
        }

        return m_CLRRuntimeHost;
    }


};

bool TryRun(const int argc, const wchar_t* argv[], Logger &log, const bool verbose, const bool waitForDebugger, DWORD &exitCode)
{

    // Assume failure
    exitCode = -1;

    HostEnvironment hostEnvironment(&log);

    //-------------------------------------------------------------

    // Find the specified exe. This is done using LoadLibrary so that
    // the OS library search semantics are used to find it.

    const wchar_t* exeName = argc > 0 ? argv[0] : nullptr;
    if(exeName == nullptr)
    {
        log << W("No exename specified.") << Logger::endl;
        return false;
    }

    wchar_t appPath[MAX_PATH] = W("");
    wchar_t appNiPath[MAX_PATH * 2] = W("");
    wchar_t managedAssemblyFullName[MAX_PATH] = W("");
    wchar_t appLocalWinmetadata[MAX_PATH] = W("");
    
    wchar_t* filePart = NULL;
    
    if (!::GetFullPathName(exeName, MAX_PATH, appPath, &filePart)) {
        log << W("Failed to get full path: ") << exeName << Logger::endl;
        log << W("Error code: ") << GetLastError() << Logger::endl;
        return false;
    } 

    wcscpy_s(managedAssemblyFullName, appPath);

    *(filePart) = W('\0');

    log << W("Loading: ") << managedAssemblyFullName << Logger::endl;
   
    wcscpy_s(appLocalWinmetadata, appPath);
    wcscat(appLocalWinmetadata, W("\\WinMetadata"));
   
    DWORD dwAttrib = ::GetFileAttributes(appLocalWinmetadata);
    bool appLocalWinMDexists = dwAttrib != INVALID_FILE_ATTRIBUTES && (dwAttrib & FILE_ATTRIBUTE_DIRECTORY);

    if (!appLocalWinMDexists) {
        wcscpy_s(appLocalWinmetadata, W(""));
    }
    wcscpy_s(appNiPath, appPath);
    wcscat(appNiPath, W("NI"));
    wcscat_s(appNiPath, MAX_PATH * 2, W(";"));
    wcscat_s(appNiPath, MAX_PATH * 2, appPath);

    // Construct native search directory paths
    wchar_t nativeDllSearchDirs[MAX_PATH * 3];

    wcscpy_s(nativeDllSearchDirs, appPath);
    wchar_t coreLibraries[MAX_PATH];
    size_t outSize;
    if (_wgetenv_s(&outSize, coreLibraries, MAX_PATH, W("CORE_LIBRARIES")) == 0 && outSize > 0)
    {
        wcscat_s(nativeDllSearchDirs, MAX_PATH * 3, W(";"));
        wcscat_s(nativeDllSearchDirs, MAX_PATH * 3, coreLibraries);
    }
    wcscat_s(nativeDllSearchDirs, MAX_PATH * 3, W(";"));
    wcscat_s(nativeDllSearchDirs, MAX_PATH * 3, hostEnvironment.m_coreCLRDirectoryPath);

    // Start the CoreCLR

    ICLRRuntimeHost2 *host = hostEnvironment.GetCLRRuntimeHost();
    if (!host) {
        return false;
    }

    HRESULT hr;

    log << W("Setting ICLRRuntimeHost2 startup flags") << Logger::endl;

    // Default startup flags
    hr = host->SetStartupFlags((STARTUP_FLAGS)
        (STARTUP_FLAGS::STARTUP_LOADER_OPTIMIZATION_SINGLE_DOMAIN | 
        STARTUP_FLAGS::STARTUP_SINGLE_APPDOMAIN)); 
    if (FAILED(hr)) {
        log << W("Failed to set startup flags. ERRORCODE: ") << Logger::hresult << hr << Logger::endl;
        return false;
    }

    log << W("Starting ICLRRuntimeHost2") << Logger::endl;

    hr = host->Start();
    if (FAILED(hr)) {
        log << W("Failed to start CoreCLR. ERRORCODE: ") << Logger::hresult << hr << Logger:: endl;
        return false;
    }

    //-------------------------------------------------------------

    // Create an AppDomain

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
    // APP_NI_PATHS
    // - The list of additional paths that the assembly loader will probe for ngen images
    //
    // NATIVE_DLL_SEARCH_DIRECTORIES
    // - The list of paths that will be probed for native DLLs called by PInvoke
    //
    const wchar_t *property_keys[] = { 
        W("TRUSTED_PLATFORM_ASSEMBLIES"),
        W("APP_PATHS"),
        W("APP_NI_PATHS"),
        W("NATIVE_DLL_SEARCH_DIRECTORIES"),
        W("AppDomainCompatSwitch"),
        W("APP_LOCAL_WINMETADATA")
    };
    const wchar_t *property_values[] = { 
        // TRUSTED_PLATFORM_ASSEMBLIES
        hostEnvironment.GetTpaList(),
        // APP_PATHS
        appPath,
        // APP_NI_PATHS
        appNiPath,
        // NATIVE_DLL_SEARCH_DIRECTORIES
        nativeDllSearchDirs,
        // AppDomainCompatSwitch
        W("UseLatestBehaviorWhenTFMNotSpecified"),
        // APP_LOCAL_WINMETADATA
        appLocalWinmetadata
    };
     
    log << W("Creating an AppDomain") << Logger::endl;
    for (int idx = 0; idx < sizeof(property_keys) / sizeof(wchar_t*); idx++)
    {
        log << property_keys[idx] << W("=") << property_values[idx] << Logger::endl;
    }

    DWORD domainId;

    hr = host->CreateAppDomainWithManager(
        hostEnvironment.GetHostExeName(),   // The friendly name of the AppDomain
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

    if (FAILED(hr)) {
        log << W("Failed call to CreateAppDomainWithManager. ERRORCODE: ") << Logger::hresult << hr << Logger::endl;
        return false;
    }

    if(waitForDebugger)
    {
        if(!IsDebuggerPresent())
        {
            log << W("Waiting for the debugger to attach. Press any key to continue ...") << Logger::endl;
            getchar();
            if (IsDebuggerPresent())
            {
                log << "Debugger is attached." << Logger::endl;
            }
            else
            {
                log << "Debugger failed to attach." << Logger::endl;
            }
        }
    }

    hr = host->ExecuteAssembly(domainId, managedAssemblyFullName, argc-1, (argc-1)?&(argv[1]):NULL, &exitCode);
    if (FAILED(hr)) {
        log << W("Failed call to ExecuteAssembly. ERRORCODE: ") << Logger::hresult << hr << Logger::endl;
        return false;
    }

    log << W("App exit value = ") << exitCode << Logger::endl;


    //-------------------------------------------------------------

    // Unload the AppDomain

    log << W("Unloading the AppDomain") << Logger::endl;

    hr = host->UnloadAppDomain(
        domainId, 
        true);                          // Wait until done

    if (FAILED(hr)) {
        log << W("Failed to unload the AppDomain. ERRORCODE: ") << Logger::hresult << hr << Logger::endl;
        return false;
    }


    //-------------------------------------------------------------

    // Stop the host

    log << W("Stopping the host") << Logger::endl;

    hr = host->Stop();

    if (FAILED(hr)) {
        log << W("Failed to stop the host. ERRORCODE: ") << Logger::hresult << hr << Logger::endl;
        return false;
    }

    //-------------------------------------------------------------

    // Release the reference to the host

    log << W("Releasing ICLRRuntimeHost2") << Logger::endl;

    host->Release();

    return true;

}

void showHelp() {
    ::wprintf(
        W("Runs executables on CoreCLR\r\n")
        W("\r\n")
        W("USAGE: coreRun [/d] [/v] Managed.exe\r\n")
        W("\r\n")
        W("  where Managed.exe is a managed executable built for CoreCLR\r\n")
        W("        /v causes verbose output to be written to the console\r\n")
        W("        /d causes coreRun to wait for a debugger to attach before\r\n")
        W("         launching Managed.exe\r\n")
        W("\r\n")
        W("  CoreCLR is searched for in %%core_root%%, then in the directory\r\n")
        W("  that coreRun.exe is in, then finally in %s.\r\n"),
        coreCLRInstallDirectory
        );
}

int __cdecl wmain(const int argc, const wchar_t* argv[])
{
    // Parse the options from the command line

    bool verbose = false;
    bool waitForDebugger = false;
    bool helpRequested = false;
    int newArgc = argc - 1;
    const wchar_t **newArgv = argv + 1;

    auto stringsEqual = [](const wchar_t * const a, const wchar_t * const b) -> bool {
        return ::_wcsicmp(a, b) == 0;
    };

    auto tryParseOption = [&](const wchar_t* arg) -> bool {
        if ( stringsEqual(arg, W("/v")) || stringsEqual(arg, W("-v")) ) {
                verbose = true;
                return true;
        } else if ( stringsEqual(arg, W("/d")) || stringsEqual(arg, W("-d")) ) {
                waitForDebugger = true;
                return true;
        } else if ( stringsEqual(arg, W("/?")) || stringsEqual(arg, W("-?")) ) {
            helpRequested = true;
            return true;
        } else {
            return false;
        }
    };

    while (newArgc > 0 && tryParseOption(newArgv[0])) {
        newArgc--;
        newArgv++;
    }

    if (argc < 2 || helpRequested || newArgc==0) {
        showHelp();
        return -1;
    } else {
        Logger log;
        if (verbose) {
            log.Enable();
        } else {
            log.Disable();
        }

        DWORD exitCode;
        auto success = TryRun(newArgc, newArgv, log, verbose, waitForDebugger, exitCode);

        log << W("Execution ") << (success ? W("succeeded") : W("failed")) << Logger::endl;

        return exitCode;
    }
}

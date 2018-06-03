//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// SuperPMI-Shim-Collector.cpp - Shim that collects and yields .mc (method context) files.
//----------------------------------------------------------

#include "standardpch.h"
#include "coreclrcallbacks.h"
#include "icorjitcompiler.h"
#include "runtimedetails.h"
#include "errorhandling.h"
#include "logging.h"
#include "spmiutil.h"
#include "jithost.h"

// Assumptions:
// -We'll never be unloaded - we leak memory and have no facility to unload libraries
// -printf output to console is okay

HMODULE        g_hRealJit           = 0; // We leak this currently (could do the proper shutdown in process_detach)
WCHAR*         g_realJitPath        = nullptr; // We leak this (could do the proper shutdown in process_detach)
WCHAR*         g_logPath            = nullptr; // Again, we leak this one too...
WCHAR*         g_dataFileName       = nullptr; // We leak this
char*          g_logFilePath        = nullptr; // We *don't* leak this, hooray!
WCHAR*         g_HomeDirectory      = nullptr;
WCHAR*         g_DefaultRealJitPath = nullptr;
MethodContext* g_globalContext      = nullptr;

void SetDefaultPaths()
{
    if (g_HomeDirectory == nullptr)
    {
        g_HomeDirectory = GetEnvironmentVariableWithDefaultW(W("HOME"), W("."));
    }

    if (g_DefaultRealJitPath == nullptr)
    {
        size_t len           = wcslen(g_HomeDirectory) + 1 + wcslen(DEFAULT_REAL_JIT_NAME_W) + 1;
        g_DefaultRealJitPath = new WCHAR[len];
        wcscpy_s(g_DefaultRealJitPath, len, g_HomeDirectory);
        wcscat_s(g_DefaultRealJitPath, len, DIRECTORY_SEPARATOR_STR_W);
        wcscat_s(g_DefaultRealJitPath, len, DEFAULT_REAL_JIT_NAME_W);
    }
}

void SetLibName()
{
    if (g_realJitPath == nullptr)
    {
        g_realJitPath = GetEnvironmentVariableWithDefaultW(W("SuperPMIShimPath"), g_DefaultRealJitPath);
    }
}

void SetLogPath()
{
    if (g_logPath == nullptr)
    {
        g_logPath = GetEnvironmentVariableWithDefaultW(W("SuperPMIShimLogPath"), g_HomeDirectory);
    }
}

void SetLogPathName()
{
    // NOTE: under PAL, we don't get the command line, so we depend on the random number generator to give us a unique
    // filename.
    WCHAR* OriginalExecutableName =
        GetCommandLineW(); // TODO-Cleanup: not cool to write to the process view of commandline....
    size_t len            = wcslen(OriginalExecutableName);
    WCHAR* ExecutableName = new WCHAR[len + 1];
    wcscpy_s(ExecutableName, len + 1, OriginalExecutableName);
    ExecutableName[len] = W('\0');
    WCHAR* quote1       = NULL;

    // if there are any quotes in filename convert them to spaces.
    while ((quote1 = wcsstr(ExecutableName, W("\""))) != NULL)
        *quote1 = W(' ');

    // remove any illegal or annoying characters from file name by converting them to underscores
    while ((quote1 = wcspbrk(ExecutableName, W("=<>:\"/\\|?! *.,"))) != NULL)
        *quote1 = W('_');

    const WCHAR* DataFileExtension       = W(".mc");
    size_t       ExecutableNameLength    = wcslen(ExecutableName);
    size_t       DataFileExtensionLength = wcslen(DataFileExtension);
    size_t       logPathLength           = wcslen(g_logPath);

    size_t dataFileNameLength = logPathLength + 1 + ExecutableNameLength + 1 + DataFileExtensionLength + 1;

    const size_t MaxAcceptablePathLength =
        MAX_PATH - 20; // subtract 20 to leave buffer, for possible random number addition
    if (dataFileNameLength >= MaxAcceptablePathLength)
    {
        // The path name is too long; creating the file will fail. This can happen because we use the command line,
        // which for ngen includes lots of environment variables, for example.

        // Assume (!) the extra space is all in the ExecutableName, so shorten that.
        ExecutableNameLength -= dataFileNameLength - MaxAcceptablePathLength;

        dataFileNameLength = MaxAcceptablePathLength;
    }

// Always add a random number, just in case the above doesn't give us a unique filename.
#ifdef FEATURE_PAL
    unsigned __int64 randNumber       = 0;
    const size_t     RandNumberLength = sizeof(randNumber) * 2 + 1; // 16 hex digits + null
    WCHAR            RandNumberString[RandNumberLength];
    PAL_Random(&randNumber, sizeof(randNumber));
    swprintf_s(RandNumberString, RandNumberLength, W("%016llX"), randNumber);
#else  // !FEATURE_PAL
    unsigned int randNumber       = 0;
    const size_t RandNumberLength = sizeof(randNumber) * 2 + 1; // 8 hex digits + null
    WCHAR        RandNumberString[RandNumberLength];
    rand_s(&randNumber);
    swprintf_s(RandNumberString, RandNumberLength, W("%08X"), randNumber);
#endif // !FEATURE_PAL

    dataFileNameLength += RandNumberLength - 1;

    // Construct the full pathname we're going to use.
    g_dataFileName    = new WCHAR[dataFileNameLength];
    g_dataFileName[0] = 0;
    wcsncat_s(g_dataFileName, dataFileNameLength, g_logPath, logPathLength);
    wcsncat_s(g_dataFileName, dataFileNameLength, DIRECTORY_SEPARATOR_STR_W, 1);
    wcsncat_s(g_dataFileName, dataFileNameLength, ExecutableName, ExecutableNameLength);

    if (RandNumberLength > 0)
    {
        wcsncat_s(g_dataFileName, dataFileNameLength, RandNumberString, RandNumberLength);
    }

    wcsncat_s(g_dataFileName, dataFileNameLength, DataFileExtension, DataFileExtensionLength);
}

// TODO: this only works for ANSI file paths...
void SetLogFilePath()
{
    if (g_logFilePath == nullptr)
    {
        // If the environment variable isn't set, we don't enable file logging
        g_logFilePath = GetEnvironmentVariableWithDefaultA("SuperPMIShimLogFilePath", nullptr);
    }
}

extern "C" BOOL
#ifndef FEATURE_PAL
    APIENTRY
#endif // !FEATURE_PAL
    DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
        case DLL_PROCESS_ATTACH:
#ifdef FEATURE_PAL
            if (0 != PAL_InitializeDLL())
            {
                fprintf(stderr, "Error: Fail to PAL_InitializeDLL\n");
                exit(1);
            }
#endif // FEATURE_PAL

            Logger::Initialize();
            SetLogFilePath();
            Logger::OpenLogFile(g_logFilePath);
            break;

        case DLL_PROCESS_DETACH:
            Logger::Shutdown();

            delete[] g_logFilePath;
            g_logFilePath = nullptr;

            break;

        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
            break;
    }
    return TRUE;
}

// Exported via def file
extern "C" void __stdcall jitStartup(ICorJitHost* host)
{
    SetDefaultPaths();
    SetLibName();

    // Load Library
    if (g_hRealJit == 0)
    {
        g_hRealJit = ::LoadLibraryW(g_realJitPath);
        if (g_hRealJit == 0)
        {
            LogError("jitStartup() - LoadLibrary failed to load '%ws' (0x%08x)", g_realJitPath, ::GetLastError());
            return;
        }
    }

    // Get the required entrypoint
    PjitStartup pnjitStartup = (PjitStartup)::GetProcAddress(g_hRealJit, "jitStartup");
    if (pnjitStartup == nullptr)
    {
        // This portion of the interface is not used by the JIT under test.
        return;
    }

    g_globalContext = new MethodContext();
    g_ourJitHost    = new JitHost(host, g_globalContext);
    pnjitStartup(g_ourJitHost);
}

// Exported via def file
extern "C" ICorJitCompiler* __stdcall getJit()
{
    DWORD             dwRetVal = 0;
    PgetJit           pngetJit;
    interceptor_ICJC* pJitInstance = nullptr;
    ICorJitCompiler*  tICJI        = nullptr;

    SetDefaultPaths();
    SetLibName();
    SetLogPath();
    SetLogPathName();

    // Load Library
    if (g_hRealJit == 0)
    {
        g_hRealJit = ::LoadLibraryW(g_realJitPath);
        if (g_hRealJit == 0)
        {
            LogError("getJit() - LoadLibrary failed to load '%ws' (0x%08x)", g_realJitPath, ::GetLastError());
            return nullptr;
        }
    }

    // get the required entrypoints
    pngetJit = (PgetJit)::GetProcAddress(g_hRealJit, "getJit");
    if (pngetJit == 0)
    {
        LogError("getJit() - GetProcAddress 'getJit' failed (0x%08x)", ::GetLastError());
        return nullptr;
    }

    tICJI = pngetJit();
    if (tICJI == nullptr)
    {
        LogError("getJit() - pngetJit gave us null");
        return nullptr;
    }

    pJitInstance                           = new interceptor_ICJC();
    pJitInstance->original_ICorJitCompiler = tICJI;

    // create our datafile
    pJitInstance->hFile = CreateFileW(g_dataFileName, GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,
                                      FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (pJitInstance->hFile == INVALID_HANDLE_VALUE)
    {
        LogError("Couldn't open file '%ws': error %d", g_dataFileName, GetLastError());
    }

    return pJitInstance;
}

// Exported via def file
extern "C" void __stdcall sxsJitStartup(CoreClrCallbacks const& original_cccallbacks)
{
    PsxsJitStartup pnsxsJitStartup;

    SetDefaultPaths();
    SetLibName();

    // Load Library
    if (g_hRealJit == 0)
    {
        g_hRealJit = ::LoadLibraryW(g_realJitPath);
        if (g_hRealJit == 0)
        {
            LogError("sxsJitStartup() - LoadLibrary failed to load '%ws' (0x%08x)", g_realJitPath, ::GetLastError());
            return;
        }
    }

    // get entry point
    pnsxsJitStartup = (PsxsJitStartup)::GetProcAddress(g_hRealJit, "sxsJitStartup");
    if (pnsxsJitStartup == 0)
    {
        LogError("sxsJitStartup() - GetProcAddress 'sxsJitStartup' failed (0x%08x)", ::GetLastError());
        return;
    }

    // Setup CoreClrCallbacks and call sxsJitStartup
    original_CoreClrCallbacks                             = new CoreClrCallbacks();
    original_CoreClrCallbacks->m_hmodCoreCLR              = original_cccallbacks.m_hmodCoreCLR;
    original_CoreClrCallbacks->m_pfnIEE                   = original_cccallbacks.m_pfnIEE;
    original_CoreClrCallbacks->m_pfnGetCORSystemDirectory = original_cccallbacks.m_pfnGetCORSystemDirectory;
    original_CoreClrCallbacks->m_pfnGetCLRFunction        = original_cccallbacks.m_pfnGetCLRFunction;

    CoreClrCallbacks* temp = new CoreClrCallbacks();

    temp->m_hmodCoreCLR              = original_cccallbacks.m_hmodCoreCLR;
    temp->m_pfnIEE                   = IEE_t;
    temp->m_pfnGetCORSystemDirectory = original_cccallbacks.m_pfnGetCORSystemDirectory;
    temp->m_pfnGetCLRFunction        = GetCLRFunction;

    pnsxsJitStartup(*temp);
}

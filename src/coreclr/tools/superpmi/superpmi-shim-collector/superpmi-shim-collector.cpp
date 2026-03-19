// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// SuperPMI-Shim-Collector.cpp - Shim that collects and yields .mc (method context) files.
//----------------------------------------------------------

#include "standardpch.h"
#include "icorjitcompiler.h"
#include "runtimedetails.h"
#include "errorhandling.h"
#include "logging.h"
#include "spmiutil.h"
#include "jithost.h"

// Assumptions:
// -We'll never be unloaded - we leak memory and have no facility to unload libraries
// -printf output to console is okay

HMODULE        g_hRealJit = 0;    // We leak this currently (could do the proper shutdown in process_detach)
std::string    g_realJitPath{""}; // Destructable objects will be cleaned up and won't leak
std::string    g_logPath{""};
std::string    g_HomeDirectory{""};
std::string    g_DefaultRealJitPath{""};
std::string    g_dataFileName{""};
MethodContext* g_globalContext    = nullptr;
bool           g_initialized      = false;
char*          g_collectionFilter = nullptr;

// RAII holder for logger
// Global deconstructors are unreliable. We only use it for superpmi shim.
class LoggerHolder
{
public:
    LoggerHolder()
    {
        Logger::Initialize();
        // If the environment variable isn't set, we don't enable file logging
        const char* logFilePath = GetEnvWithDefault("SuperPMIShimLogFilePath", nullptr);
        if (logFilePath)
        {
            Logger::OpenLogFile(logFilePath);
        }
    }

    ~LoggerHolder()
    {
        Logger::Shutdown();
    }
} loggerHolder;

void SetDefaultPaths()
{
    if (g_HomeDirectory.empty())
    {
        g_HomeDirectory = GetEnvWithDefault("HOME", ".");
    }

    if (g_DefaultRealJitPath.empty())
    {
        g_DefaultRealJitPath = g_HomeDirectory + DIRECTORY_SEPARATOR_CHAR_A + DEFAULT_REAL_JIT_NAME_A;
    }
}

void SetLibName()
{
    if (g_realJitPath.empty())
    {
        g_realJitPath = GetEnvWithDefault("SuperPMIShimPath", g_DefaultRealJitPath.c_str());
    }
}

void SetLogPath()
{
    if (g_logPath.empty())
    {
        g_logPath = GetEnvWithDefault("SuperPMIShimLogPath", g_HomeDirectory.c_str());
    }
}

void SetLogPathName()
{
    g_dataFileName = GetResultFileName(g_logPath, GetProcessCommandLine(), ".mc");
}

void SetCollectionFilter()
{
    g_collectionFilter = GetEnvironmentVariableWithDefaultA("SuperPMIShimFilter", nullptr);

    if (g_collectionFilter != nullptr)
    {
        fprintf(stderr, "*** SPMI filter '%s'\n", g_collectionFilter);
    }
}

void InitializeShim()
{
    if (g_initialized)
    {
        return;
    }

#ifdef HOST_UNIX
    // Register signal handlers for the shim so we can handle committing collections on JIT segfaults.
    PAL_SetInitializeDLLFlags(PAL_INITIALIZE_REGISTER_SIGNALS);
    if (0 != PAL_InitializeDLL())
    {
        fprintf(stderr, "Error: Fail to PAL_InitializeDLL\n");
        exit(1);
    }
#endif // HOST_UNIX

#ifdef HOST_WINDOWS
    // Assertions will be sent to stderr instead of a pop-up dialog.
    _CrtSetReportMode(_CRT_ASSERT, _CRTDBG_MODE_FILE);
    _CrtSetReportFile(_CRT_ASSERT, _CRTDBG_FILE_STDERR);
#endif // HOST_WINDOWS

    g_initialized = true;
}

extern "C"
#ifdef HOST_UNIX
    DLLEXPORT // For Win32 PAL LoadLibrary emulation
#endif
        BOOL
        DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
        case DLL_PROCESS_ATTACH:
            InitializeShim();
            break;

        case DLL_PROCESS_DETACH:
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
            break;
    }
    return TRUE;
}

extern "C" DLLEXPORT void jitStartup(ICorJitHost* host)
{
    // crossgen2 doesn't invoke DllMain on Linux/Mac (under PAL), so optionally do initialization work here.
    InitializeShim();

    SetDefaultPaths();
    SetLibName();
    SetDebugDumpVariables();
    SetCollectionFilter();

    if (!LoadRealJitLib(g_hRealJit, g_realJitPath))
    {
        return;
    }

    // Get the required entrypoint
    PjitStartup pnjitStartup = (PjitStartup)GET_PROC_ADDRESS(g_hRealJit, "jitStartup");
    if (pnjitStartup == nullptr)
    {
        // This portion of the interface is not used by the JIT under test.
        return;
    }

    g_globalContext = new MethodContext();
    g_ourJitHost    = new JitHost(host, g_globalContext);
    pnjitStartup(g_ourJitHost);
}

extern "C" DLLEXPORT ICorJitCompiler* getJit()
{
    DWORD             dwRetVal = 0;
    PgetJit           pngetJit;
    interceptor_ICJC* pJitInstance = nullptr;
    ICorJitCompiler*  tICJI        = nullptr;

    SetDefaultPaths();
    SetLibName();
    SetLogPath();
    SetLogPathName();
    SetDebugDumpVariables();

    if (!LoadRealJitLib(g_hRealJit, g_realJitPath))
    {
        return nullptr;
    }

    // get the required entrypoints
    pngetJit = (PgetJit)GET_PROC_ADDRESS(g_hRealJit, "getJit");
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

#ifdef TARGET_WINDOWS
    pJitInstance->currentOs = CORINFO_WINNT;
#elif defined(TARGET_OSX)
    pJitInstance->currentOs = CORINFO_APPLE;
#elif defined(TARGET_UNIX)
    pJitInstance->currentOs = CORINFO_UNIX;
#else
#error No target os defined
#endif

    // create our datafile
    pJitInstance->hFile = CreateFileA(g_dataFileName.c_str(), GENERIC_READ | GENERIC_WRITE, 0, NULL,
                                      CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (pJitInstance->hFile == INVALID_HANDLE_VALUE)
    {
        LogError("Couldn't open file '%s': error %d", g_dataFileName.c_str(), GetLastError());
    }

    return pJitInstance;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// SuperPMI-Shim.cpp - thin shim for the jit
//----------------------------------------------------------

#include "standardpch.h"
#include "superpmi-shim-counter.h"
#include "runtimedetails.h"
#include "icorjitcompiler.h"
#include "errorhandling.h"
#include "logging.h"
#include "spmiutil.h"
#include "jithost.h"

HMODULE               g_hRealJit      = 0; // We leak this currently (could do the proper shutdown in process_detach)
std::string           g_realJitPath   = ""; // std::string will be cleaned up and won't leak
std::string           g_logPath            = "";
std::string           g_logFilePath        = "";
std::string           g_HomeDirectory      = "";
std::string           g_DefaultRealJitPath = "";
MethodCallSummarizer* g_globalContext      = nullptr;

void SetDefaultPaths()
{
    if (g_HomeDirectory.empty())
    {
        g_HomeDirectory = GetEnvWithDefault("HOME", ".");
    }

    if (g_DefaultRealJitPath.empty())
    {
        g_DefaultRealJitPath = g_HomeDirectory + DIRECTORY_SEPARATOR_STR_A + DEFAULT_REAL_JIT_NAME_A;
    }
}

void SetLibName()
{
    if (g_realJitPath.empty())
    {
        g_realJitPath = GetEnvWithDefault("SuperPMIShimPath", g_DefaultRealJitPath);
    }
}

void SetLogPath()
{
    if (g_logPath.empty())
    {
        g_logPath = GetEnvWithDefault("SuperPMIShimLogPath", g_HomeDirectory);
    }
}

void SetLogFilePath()
{
    if (g_logFilePath.empty())
    {
        // If the environment variable isn't set, we don't enable file logging
        g_logFilePath = GetEnvWithDefault("SuperPMIShimLogFilePath", nullptr);
    }
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
#ifdef HOST_UNIX
            if (0 != PAL_InitializeDLL())
            {
                fprintf(stderr, "Error: Fail to PAL_InitializeDLL\n");
                exit(1);
            }
#endif // HOST_UNIX

            Logger::Initialize();
            SetLogFilePath();
            Logger::OpenLogFile(g_logFilePath.c_str());
            break;

        case DLL_PROCESS_DETACH:
            Logger::Shutdown();

            if (g_globalContext != nullptr)
            {
                g_globalContext->SaveTextFile();
            }

            break;

        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
            break;
    }
    return TRUE;
}

extern "C" DLLEXPORT void jitStartup(ICorJitHost* host)
{
    SetDefaultPaths();
    SetLibName();
    SetDebugDumpVariables();

    if (!LoadRealJitLib(g_hRealJit, g_realJitPath.c_str()))
    {
        return;
    }

    // Get the required entrypoint
    PjitStartup pnjitStartup = (PjitStartup)::GetProcAddress(g_hRealJit, "jitStartup");
    if (pnjitStartup == nullptr)
    {
        // This portion of the interface is not used by the JIT under test.
        g_ourJitHost = nullptr;
        return;
    }

    g_ourJitHost = new JitHost(host);

    if (g_globalContext == nullptr)
    {
        SetLogPath();
        g_globalContext = new MethodCallSummarizer(g_logPath);
    }

    g_ourJitHost->setMethodCallSummarizer(g_globalContext);

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
    SetDebugDumpVariables();

    if (!LoadRealJitLib(g_hRealJit, g_realJitPath.c_str()))
    {
        return nullptr;
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

    if (g_globalContext == nullptr)
    {
        SetLogPath();
        g_globalContext = new MethodCallSummarizer(g_logPath);
    }
    pJitInstance->mcs = g_globalContext;
    return pJitInstance;
}

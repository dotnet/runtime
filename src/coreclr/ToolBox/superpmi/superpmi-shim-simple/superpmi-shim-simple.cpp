// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// SuperPMI-Shim.cpp - thin shim for the jit
//----------------------------------------------------------

#include "standardpch.h"
#include "superpmi-shim-simple.h"
#include "runtimedetails.h"
#include "icorjitcompiler.h"
#include "errorhandling.h"
#include "logging.h"
#include "spmiutil.h"
#include "jithost.h"

HMODULE g_hRealJit           = 0;       // We leak this currently (could do the proper shutdown in process_detach)
WCHAR*  g_realJitPath        = nullptr; // We leak this (could do the proper shutdown in process_detach)
char*   g_logFilePath        = nullptr; // We *don't* leak this, hooray!
WCHAR*  g_HomeDirectory      = nullptr;
WCHAR*  g_DefaultRealJitPath = nullptr;

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

// TODO: this only works for ANSI file paths...
void SetLogFilePath()
{
    if (g_logFilePath == nullptr)
    {
        // If the environment variable isn't set, we don't enable file logging
        g_logFilePath = GetEnvironmentVariableWithDefaultA("SuperPMIShimLogFilePath", nullptr);
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

extern "C" DLLEXPORT void __stdcall jitStartup(ICorJitHost* host)
{
    SetDefaultPaths();
    SetLibName();
    SetDebugDumpVariables();

    if (!LoadRealJitLib(g_hRealJit, g_realJitPath))
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
    pnjitStartup(g_ourJitHost);
}

extern "C" DLLEXPORT ICorJitCompiler* __stdcall getJit()
{
    DWORD             dwRetVal = 0;
    PgetJit           pngetJit;
    interceptor_ICJC* pJitInstance = nullptr;
    ICorJitCompiler*  tICJI        = nullptr;

    SetDefaultPaths();
    SetLibName();
    SetDebugDumpVariables();

    if (!LoadRealJitLib(g_hRealJit, g_realJitPath))
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
    return pJitInstance;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/**
 *
 * AutoTrace: This infrastructure is used to run automated testing of Diagnostic Server based tracing via
 * EventPipe.  The feature itself is enabled via the feature flag FEATURE_AUTO_TRACE.
 * 
 * Two environment variables dictate behavior:
 * - COMPlus_AutoTrace_N_Tracers: a number in [0,64] where 0 will disable the feature
 * - COMPlus_AutoTrace_Command: The path to an executable to be invoked.  Typically this will be a "run.sh|cmd".
 *  > (NB: you should `cd` into the directory you intend to execute `COMPlus_AutoTrace_Command` from as the first line of the script.)
 * 
 * Once turned on, AutoTrace will run the specified command `COMPlus_AutoTrace_N_Tracers` times.  There is an event that will pause execution
 * of the runtime until all the tracers have attached.  Once all the tracers are attached, execution will continue normally.
 * 
 * This logic is easily modified to accommodate testing other mechanisms related to the Diagnostic Server.
 * 
 */

#include "common.h" // Required for pre-compiled header

#ifdef FEATURE_AUTO_TRACE
#ifdef FEATURE_PAL
#include "pal.h"
#endif // FEATURE_PAL

HANDLE auto_trace_event;
static size_t g_n_tracers = 1;
static const WCHAR* command_format = W("%hs -p %d");
static WCHAR* command = nullptr;

void auto_trace_init()
{
    char *nAutoTracersValue = getenv("COMPlus_AutoTrace_N_Tracers");
    if (nAutoTracersValue != NULL)
    {
        g_n_tracers = strtoul(nAutoTracersValue, NULL, 10);
    }

    // Get the command to run auto-trace.  Note that the `-p <pid>` option
    // will be automatically added for you
    char *commandTextValue = getenv("COMPlus_AutoTrace_Command");
    if (commandTextValue != NULL)
    {
        DWORD currentProcessId = GetCurrentProcessId();
        command = new WCHAR[8192];
        _snwprintf_s(command, 8192, _TRUNCATE, command_format, commandTextValue, currentProcessId);
    }
    else
    {
        // we don't have anything to run, just set
        // n tracers to 0...
        g_n_tracers = 0;
    }

    auto_trace_event = CreateEventW(
        /* lpEventAttributes = */ NULL,
        /* bManualReset      = */ FALSE,
        /* bInitialState     = */ FALSE,
        /* lpName            = */ nullptr
    );
}

void auto_trace_launch_internal()
{
    DWORD currentProcessId = GetCurrentProcessId();
    STARTUPINFO si;
    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(STARTUPINFO);
#ifndef FEATURE_PAL
    si.dwFlags = STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_HIDE;
#endif
    
    PROCESS_INFORMATION result;

    BOOL code = CreateProcessW(
        /* lpApplicationName    = */ nullptr,
        /* lpCommandLine        = */ command,
        /* lpCommandLine        = */ nullptr,
        /* lpThreadAttributes   = */ nullptr,
        /* bInheritHandles      = */ false,
        /* dwCreationFlags      = */ CREATE_NEW_CONSOLE,
        /* lpEnvironment        = */ nullptr,
        /* lpCurrentDirectory   = */ nullptr,
        /* lpStartupInfo        = */ &si,
        /* lpProcessInformation = */ &result
    );
}

void auto_trace_launch()
{
    for (int i = 0; i < g_n_tracers; ++i)
    {
        auto_trace_launch_internal();
    }
    delete[] command;
}

void auto_trace_wait()
{
    if (g_n_tracers > 0)
        WaitForSingleObject(auto_trace_event, INFINITE);
}

void auto_trace_signal()
{
    #ifdef SetEvent
    #undef SetEvent
    #endif
    static size_t nCalls = 0;
    if (++nCalls == g_n_tracers)
        SetEvent(auto_trace_event);
}

#endif // FEATURE_AUTO_TRACE

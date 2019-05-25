#include "common.h" // Required for pre-compiled header

#ifdef FEATURE_AUTO_TRACE
#ifdef FEATURE_PAL
#include "pal.h"
#endif // FEATURE_PAL

HANDLE auto_trace_event;
static size_t g_n_tracers = 1;
#ifdef __apple__
static const WCHAR* command_format = L"%hs -p %d";
#else
static const WCHAR* command_format = u"%hs -p %d";
#endif // __apple__
static WCHAR* command = nullptr;

void auto_trace_init()
{
    char *nAutoTracersValue = getenv("N_AUTO_TRACERS");
    if (nAutoTracersValue != NULL)
    {
        g_n_tracers = strtoul(nAutoTracersValue, NULL, 10);
    }

    // Get the command to run auto-trace.  Note that the `-p <pid>` option
    // will be automatically added for you
    char *commandTextValue = getenv("AUTO_TRACE_CMD");
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
    fprintf(stdout, "%d", nCalls);
    if (++nCalls == g_n_tracers)
        SetEvent(auto_trace_event);
}

#endif // FEATURE_AUTO_TRACE

#include "common.h" // Required for pre-compiled header

#ifdef FEATURE_AUTO_TRACE

#include "pal.h"

HANDLE auto_trace_event;

void auto_trace_init()
{
    auto_trace_event = CreateEventW(
        /* lpEventAttributes = */ NULL,
        /* bManualReset      = */ FALSE,
        /* bInitialState     = */ FALSE,
        /* lpName            = */ nullptr
    );
}

void auto_trace_launch()
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
    #ifdef FEATURE_PAL
    const char16_t* commandFormat = u"/Users/andrewau/git/diagnostics/src/Tools/dotnet-trace/run.sh collect --providers Microsoft-Windows-DotNETRuntime:FFFFFFFFFFFFFFBF -p %d";
    #else
    const char16_t* commandFormat = u"C:\\Windows\\System32\\cmd.exe /c run.cmd collect --providers Microsoft-Windows-DotNETRuntime:FFFFFFFFFFFFFFBF -p %d";
    #endif
    size_t len = wcslen(commandFormat) + 10 + 1;
    char16_t* command = new char16_t[len];
    _snwprintf_s(command, len, _TRUNCATE, commandFormat, currentProcessId);
    const char16_t* currentDirectory = u"C:\\Dev\\diagnostics\\src\\Tools\\dotnet-trace";

    BOOL code = CreateProcessW(
        /* lpApplicationName    = */ nullptr,
        /* lpCommandLine        = */ command,
        /* lpCommandLine        = */ nullptr,
        /* lpThreadAttributes   = */ nullptr,
        /* bInheritHandles      = */ false,
        /* dwCreationFlags      = */ CREATE_NEW_CONSOLE,
        /* lpEnvironment        = */ nullptr,
        /* lpCurrentDirectory   = */ currentDirectory,
        /* lpStartupInfo        = */ &si,
        /* lpProcessInformation = */ &result
    );
    delete[] command;
}

void auto_trace_wait()
{
    WaitForSingleObject(auto_trace_event, INFINITE);
}

void auto_trace_signal()
{
    #ifdef SetEvent
    #undef SetEvent
    #endif
    SetEvent(auto_trace_event);    
}

#endif // FEATURE_AUTO_TRACE
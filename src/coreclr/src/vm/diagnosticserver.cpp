// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "diagnosticserver.h"
#include "ipcstreamfactory.h"
#include "eventpipeprotocolhelper.h"
#include "dumpdiagnosticprotocolhelper.h"
#include "profilerdiagnosticprotocolhelper.h"
#include "processdiagnosticsprotocolhelper.h"
#include "diagnosticsprotocol.h"

#ifdef TARGET_UNIX
#include "pal.h"
#endif // TARGET_UNIX

#ifdef FEATURE_AUTO_TRACE
#include "autotrace.h"
#endif

#ifdef FEATURE_PERFTRACING

Volatile<bool> DiagnosticServer::s_shuttingDown(false);
CLREventStatic *DiagnosticServer::s_ResumeRuntimeStartupEvent = nullptr;

DWORD WINAPI DiagnosticServer::DiagnosticsServerThread(LPVOID)
{
    CONTRACTL
    {
#ifndef DEBUG
        NOTHROW;
#endif
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(s_shuttingDown || IpcStreamFactory::HasActiveConnections());
    }
    CONTRACTL_END;

    if (!IpcStreamFactory::HasActiveConnections())
    {
        STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_ERROR, "Diagnostics IPC listener was undefined\n");
        return 1;
    }

    ErrorCallback LoggingCallback = [](const char *szMessage, uint32_t code) {
        STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_WARNING, "warning (%d): %s.\n", code, szMessage);
    };

#ifndef DEBUG
    EX_TRY
    {
#endif
        while (!s_shuttingDown)
        {
            IpcStream *pStream = IpcStreamFactory::GetNextAvailableStream(LoggingCallback);

            if (pStream == nullptr)
                continue;
#ifdef FEATURE_AUTO_TRACE
            auto_trace_signal();
#endif
            DiagnosticsIpc::IpcMessage message;
            if (!message.Initialize(pStream))
            {
                DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, CORDIAGIPC_E_BAD_ENCODING);
                delete pStream;
                continue;
            }

            if (::strcmp((char *)message.GetHeader().Magic, (char *)DiagnosticsIpc::DotnetIpcMagic_V1.Magic) != 0)
            {
                DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, CORDIAGIPC_E_UNKNOWN_MAGIC);
                delete pStream;
                continue;
            }

            STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_INFO10, "DiagnosticServer - received IPC message with command set (%d) and command id (%d)\n", message.GetHeader().CommandSet, message.GetHeader().CommandId);

            switch ((DiagnosticsIpc::DiagnosticServerCommandSet)message.GetHeader().CommandSet)
            {
            case DiagnosticsIpc::DiagnosticServerCommandSet::EventPipe:
                EventPipeProtocolHelper::HandleIpcMessage(message, pStream);
                break;

            case DiagnosticsIpc::DiagnosticServerCommandSet::Dump:
                DumpDiagnosticProtocolHelper::HandleIpcMessage(message, pStream);
                break;

            case DiagnosticsIpc::DiagnosticServerCommandSet::Process:
                ProcessDiagnosticsProtocolHelper::HandleIpcMessage(message,pStream);
                break;

#ifdef FEATURE_PROFAPI_ATTACH_DETACH
            case DiagnosticsIpc::DiagnosticServerCommandSet::Profiler:
                ProfilerDiagnosticProtocolHelper::HandleIpcMessage(message, pStream);
                break;
#endif // FEATURE_PROFAPI_ATTACH_DETACH

            default:
                STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_WARNING, "Received unknown request type (%d)\n", message.GetHeader().CommandSet);
                DiagnosticsIpc::IpcMessage::SendErrorMessage(pStream, CORDIAGIPC_E_UNKNOWN_COMMAND);
                delete pStream;
                break;
            }
        }
#ifndef DEBUG
    }
    EX_CATCH
    {
        STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_ERROR, "Exception caught in diagnostic thread. Leaving thread now.\n");
    }
    EX_END_CATCH(SwallowAllExceptions);
#endif

    return 0;
}

bool DiagnosticServer::Initialize()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // COMPlus_EnableDiagnostics==0 disables diagnostics so we don't create the diagnostics pipe/socket or diagnostics server thread
    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableDiagnostics) == 0)
    {
        return true;
    }

    bool fSuccess = false;

    EX_TRY
    {
        auto ErrorCallback = [](const char *szMessage, uint32_t code) {
            STRESS_LOG2(
                LF_DIAGNOSTICS_PORT,                                  // facility
                LL_ERROR,                                             // level
                "Failed to create diagnostic IPC: error (%d): %s.\n", // msg
                code,                                                 // data1
                szMessage);                                           // data2
        };

        NewArrayHolder<char> address = nullptr;
        CLRConfigStringHolder wAddress = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_DOTNET_DiagnosticsMonitorAddress);
        int nCharactersWritten = 0;
        if (wAddress != nullptr)
        {
            // By default, opts in to Pause on Start
            s_ResumeRuntimeStartupEvent = new CLREventStatic();
            s_ResumeRuntimeStartupEvent->CreateManualEvent(false);

            nCharactersWritten = WideCharToMultiByte(CP_UTF8, 0, wAddress, -1, NULL, 0, NULL, NULL);
            if (nCharactersWritten != 0)
            {
                address = new char[nCharactersWritten];
                nCharactersWritten = WideCharToMultiByte(CP_UTF8, 0, wAddress, -1, address, nCharactersWritten, NULL, NULL);
                assert(nCharactersWritten != 0);
            }

            // Create the client mode connection
            fSuccess &= IpcStreamFactory::CreateClient(address, ErrorCallback);
        }

        fSuccess &= IpcStreamFactory::CreateServer(nullptr, ErrorCallback);

        if (IpcStreamFactory::HasActiveConnections())
        {
#ifdef FEATURE_AUTO_TRACE
            auto_trace_init();
            auto_trace_launch();
#endif
            DWORD dwThreadId = 0;
            HANDLE hServerThread = ::CreateThread( // TODO: Is it correct to have this "lower" level call here?
                nullptr,                     // no security attribute
                0,                           // default stack size
                DiagnosticsServerThread,     // thread proc
                nullptr,                     // thread parameter
                0,                           // not suspended
                &dwThreadId);                // returns thread ID

            if (hServerThread == NULL)
            {
                IpcStreamFactory::CloseConnections();

                // Failed to create IPC thread.
                STRESS_LOG1(
                    LF_DIAGNOSTICS_PORT,                                 // facility
                    LL_ERROR,                                            // level
                    "Failed to create diagnostic server thread (%d).\n", // msg
                    ::GetLastError());                                   // data1
            }
            else
            {
                ::CloseHandle(hServerThread);

#ifdef FEATURE_AUTO_TRACE
                auto_trace_wait();
#endif
                fSuccess = true;
            }
        }
    }
    EX_CATCH
    {
        // TODO: Should we log anything here?
    }
    EX_END_CATCH(SwallowAllExceptions);

    return fSuccess;
}

bool DiagnosticServer::Shutdown()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    bool fSuccess = false;

    s_shuttingDown = true;

    EX_TRY
    {
        if (IpcStreamFactory::HasActiveConnections())
        {
            auto ErrorCallback = [](const char *szMessage, uint32_t code) {
                STRESS_LOG2(
                    LF_DIAGNOSTICS_PORT,                                  // facility
                    LL_ERROR,                                             // level
                    "Failed to close diagnostic IPC: error (%d): %s.\n",  // msg
                    code,                                                 // data1
                    szMessage);                                           // data2
            };

            IpcStreamFactory::Shutdown(ErrorCallback);
        }
        fSuccess = true;
    }
    EX_CATCH
    {
        fSuccess = false;
        // TODO: Should we log anything here?
    }
    EX_END_CATCH(SwallowAllExceptions);

    return fSuccess;
}

// This method will block runtime bring-up IFF DOTNET_DiagnosticsMonitorAddress != nullptr and DOTNET_DiagnosticsMonitorPauseOnStart!=0 (it's default state)
// The s_ResumeRuntimeStartupEvent event will be signaled when the Diagnostics Monitor uses the ResumeRuntime Diagnostics IPC Command
void DiagnosticServer::PauseForDiagnosticsMonitor()
{
    CONTRACTL
    {
      THROWS;
      GC_NOTRIGGER;
      MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    CLRConfigStringHolder pDotnetDiagnosticsMonitorAddress = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_DOTNET_DiagnosticsMonitorAddress);
    if (pDotnetDiagnosticsMonitorAddress != nullptr)
    {
        DWORD dwDotnetDiagnosticsMonitorPauseOnStart = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_DOTNET_DiagnosticsMonitorPauseOnStart);
        if (dwDotnetDiagnosticsMonitorPauseOnStart != 0)
        {
            _ASSERTE(s_ResumeRuntimeStartupEvent != nullptr && s_ResumeRuntimeStartupEvent->IsValid());
            STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_ALWAYS, "The runtime has been configured to pause during startup and is awaiting a Diagnostics IPC ResumeStartup command.");
            const DWORD dwFiveSecondWait = s_ResumeRuntimeStartupEvent->Wait(5000, false);
            if (dwFiveSecondWait == WAIT_TIMEOUT)
            {
                wprintf(W("The runtime has been configured to pause during startup and is awaiting a Diagnostics IPC ResumeStartup command from a server at '%s'.\n"), (LPWSTR)pDotnetDiagnosticsMonitorAddress);
                fflush(stdout);
                STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_ALWAYS, "The runtime has been configured to pause during startup and is awaiting a Diagnostics IPC ResumeStartup command and has waited 5 seconds.");
                const DWORD dwWait = s_ResumeRuntimeStartupEvent->Wait(INFINITE, false);
            }
        }
    }
    // allow wait failures to fall through and the runtime to continue coming up
}

void DiagnosticServer::ResumeRuntimeStartup()
{
    LIMITED_METHOD_CONTRACT;
    if (s_ResumeRuntimeStartupEvent != nullptr && s_ResumeRuntimeStartupEvent->IsValid())
        s_ResumeRuntimeStartupEvent->Set();
}

#endif // FEATURE_PERFTRACING

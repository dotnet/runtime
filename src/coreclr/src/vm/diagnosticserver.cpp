// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "diagnosticserver.h"
#include "eventpipeprotocolhelper.h"
#include "diagnosticprotocolhelper.h"

#ifdef FEATURE_PAL
#include "pal.h"
#endif // FEATURE_PAL

#ifdef FEATURE_PERFTRACING

IpcStream::DiagnosticsIpc *DiagnosticServer::s_pIpc = nullptr;

static DWORD WINAPI DiagnosticsServerThread(LPVOID lpThreadParameter)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(lpThreadParameter != nullptr);
    }
    CONTRACTL_END;

    auto pIpc = reinterpret_cast<IpcStream::DiagnosticsIpc *>(lpThreadParameter);
    if (pIpc == nullptr)
    {
        STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_ERROR, "Diagnostics IPC listener was undefined\n");
        return 1;
    }

    ErrorCallback LoggingCallback = [](const char *szMessage, uint32_t code) {
        STRESS_LOG2(LF_DIAGNOSTICS_PORT, LL_WARNING, "warning (%d): %s.\n", code, szMessage);
    };

    EX_TRY
    {
        while (true)
        {
            // FIXME: Ideally this would be something like a std::shared_ptr
            IpcStream *pStream = pIpc->Accept(LoggingCallback);
            if (pStream == nullptr)
                continue;

            // TODO: Read operation should happen in a loop.
            uint32_t nNumberOfBytesRead = 0;
            MessageHeader header;
            bool fSuccess = pStream->Read(&header, sizeof(header), nNumberOfBytesRead);
            if (!fSuccess || nNumberOfBytesRead != sizeof(header))
            {
                delete pStream;
                continue;
            }

            switch (header.RequestType)
            {
            case DiagnosticMessageType::StopEventPipeTracing:
                EventPipeProtocolHelper::StopTracing(pStream);
                break;

            case DiagnosticMessageType::CollectEventPipeTracing:
                EventPipeProtocolHelper::CollectTracing(pStream);
                break;

#ifdef FEATURE_PAL
            case DiagnosticMessageType::GenerateCoreDump:
                DiagnosticProtocolHelper::GenerateCoreDump(pStream);
                break;
#endif

#ifdef FEATURE_PROFAPI_ATTACH_DETACH
            case DiagnosticMessageType::AttachProfiler:
                DiagnosticProtocolHelper::AttachProfiler(pStream);
                break;
#endif // FEATURE_PROFAPI_ATTACH_DETACH

            default:
                STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_WARNING, "Received unknown request type (%d)\n", header.RequestType);
                delete pStream;
                break;
            }
        }
    }
    EX_CATCH
    {
        STRESS_LOG0(LF_DIAGNOSTICS_PORT, LL_ERROR, "Exception caught in diagnostic thread. Leaving thread now.\n");
        _ASSERTE(!"Hit an error in the diagnostic server thread\n.");
    }
    EX_END_CATCH(SwallowAllExceptions);

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

        // TODO: Should we handle/assert that (s_pIpc == nullptr)?
        s_pIpc = IpcStream::DiagnosticsIpc::Create(
            "dotnetcore-diagnostic", ErrorCallback);

        if (s_pIpc != nullptr)
        {
            DWORD dwThreadId = 0;
            HANDLE hThread = ::CreateThread( // TODO: Is it correct to have this "lower" level call here?
                nullptr,                     // no security attribute
                0,                           // default stack size
                DiagnosticsServerThread,     // thread proc
                (LPVOID)s_pIpc,              // thread parameter
                0,                           // not suspended
                &dwThreadId);                // returns thread ID

            if (hThread == nullptr)
            {
                // Failed to create IPC thread.
                STRESS_LOG1(
                    LF_DIAGNOSTICS_PORT,                                 // facility
                    LL_ERROR,                                            // level
                    "Failed to create diagnostic server thread (%d).\n", // msg
                    ::GetLastError());                                   // data1
            }
            else
            {
                // FIXME: Maybe hold on to the thread to abort/cleanup at exit?
                ::CloseHandle(hThread);

                // TODO: Add error handling?
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

    EX_TRY
    {
        if (s_pIpc != nullptr)
        {
            auto ErrorCallback = [](const char *szMessage, uint32_t code) {
                STRESS_LOG2(
                    LF_DIAGNOSTICS_PORT,                                  // facility
                    LL_ERROR,                                             // level
                    "Failed to unlink diagnostic IPC: error (%d): %s.\n", // msg
                    code,                                                 // data1
                    szMessage);                                           // data2
            };
            s_pIpc->Unlink(ErrorCallback);
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

#endif // FEATURE_PERFTRACING

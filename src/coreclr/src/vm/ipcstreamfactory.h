// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __IPC_STREAM_FACTORY_H__
#define __IPC_STREAM_FACTORY_H__

#ifdef FEATURE_PERFTRACING

#include "diagnosticsipc.h"

class IpcStreamFactory
{
public:
    // forward declare
    struct DiagnosticPort;

    enum class DiagnosticPortType : uint8_t
    {
        LISTEN  = 0,
        CONNECT = 1
    };

    enum class DiagnosticPortSuspendMode : uint8_t
    {
        NOSUSPEND = 0,
        SUSPEND   = 1
    };

    struct DiagnosticPortBuilder
    {
        LPSTR Path = nullptr;
        DiagnosticPortType Type = DiagnosticPortType::CONNECT;
        DiagnosticPortSuspendMode SuspendMode = DiagnosticPortSuspendMode::SUSPEND;

        DiagnosticPortBuilder WithPath(LPSTR path) { Path = path; return *this; }
        DiagnosticPortBuilder WithType(DiagnosticPortType type) { Type = type; return *this; }
        DiagnosticPortBuilder WithSuspendMode(DiagnosticPortSuspendMode mode) { SuspendMode = mode; return *this; }
        DiagnosticPortBuilder WithTag(LPSTR tag)
        {
            // check if port type
            if (_stricmp(tag, "listen") == 0)
                return WithType(DiagnosticPortType::LISTEN);

            if (_stricmp(tag, "connect") == 0)
                return WithType(DiagnosticPortType::CONNECT);

            // check if suspendmode tag
            if (_stricmp(tag, "nosuspend") == 0)
                return WithSuspendMode(DiagnosticPortSuspendMode::NOSUSPEND);

            if (_stricmp(tag, "suspend") == 0)
                return WithSuspendMode(DiagnosticPortSuspendMode::SUSPEND);

            // don't mutate if it's not a valid option
            STRESS_LOG1(LF_DIAGNOSTICS_PORT, LL_INFO10, "IpcStreamFactory::DiagnosticPortBuilder::WithTag - Unknown tag '%s'.\n", tag);
            return *this;
        }
    };

    struct DiagnosticPort
    {
    public:
        DiagnosticPort(IpcStream::DiagnosticsIpc *pIpc, DiagnosticPortBuilder builder) :
            SuspendMode(builder.SuspendMode),
            _pIpc(pIpc),
            _pStream(nullptr),
            _type(builder.Type)
        { }

        const DiagnosticPortSuspendMode SuspendMode;

        // Will be false until ResumeRuntime command is sent on this connection
        bool HasResumedRuntime = false;

        // returns a pollable handle and performs any preparation required
        // e.g., as a side-effect, will connect and advertise on reverse connections
        virtual bool GetIpcPollHandle(IpcStream::DiagnosticsIpc::IpcPollHandle *pIpcPollHandle, ErrorCallback callback = nullptr) = 0;

        // Returns the signaled stream in a usable state
        virtual IpcStream *GetConnectedStream(ErrorCallback callback = nullptr) = 0;

        // Resets the connection in the event of a hangup
        virtual void Reset(ErrorCallback callback = nullptr) = 0;

        // closes the underlying connections
        // only performs minimal cleanup if isShutdown==true
        void Close(bool isShutdown = false, ErrorCallback callback = nullptr)
        {
            if (_pIpc != nullptr)
                _pIpc->Close(isShutdown, callback);
            if (_pStream != nullptr && !isShutdown)
                _pStream->Close(callback);
        }

    protected:
        IpcStream::DiagnosticsIpc *_pIpc;
        IpcStream *_pStream;
        DiagnosticPortType _type;
    };

    struct ConnectDiagnosticPort : public DiagnosticPort
    {
        ConnectDiagnosticPort(IpcStream::DiagnosticsIpc *pIpc, DiagnosticPortBuilder builder) : DiagnosticPort(pIpc, builder) { }

        // returns a pollable handle and performs any preparation required
        bool GetIpcPollHandle(IpcStream::DiagnosticsIpc::IpcPollHandle *pIpcPollHandle, ErrorCallback callback = nullptr) override;

        // Returns the signaled stream in a usable state
        IpcStream *GetConnectedStream(ErrorCallback callback = nullptr) override;

        // Resets the connection in the event of a hangup
        void Reset(ErrorCallback callback = nullptr) override;
    };

    struct ListenDiagnosticPort : public DiagnosticPort
    {
        ListenDiagnosticPort(IpcStream::DiagnosticsIpc *pIpc, DiagnosticPortBuilder builder) : DiagnosticPort(pIpc, builder) { }

        // returns a pollable handle and performs any preparation required
        bool GetIpcPollHandle(IpcStream::DiagnosticsIpc::IpcPollHandle *pIpcPollHandle, ErrorCallback callback = nullptr) override;

        // Returns the signaled stream in a usable state
        IpcStream *GetConnectedStream(ErrorCallback callback = nullptr) override;

        // Resets the connection in the event of a hangup
        void Reset(ErrorCallback callback = nullptr) override;
    };

    static bool Configure(ErrorCallback callback = nullptr);
    static IpcStream *GetNextAvailableStream(ErrorCallback = nullptr);
    static void ResumeCurrentPort();
    static bool AnySuspendedPorts();
    static bool HasActivePorts();
    static void ClosePorts(ErrorCallback callback = nullptr);
    static void Shutdown(ErrorCallback callback = nullptr);
private:
    static bool BuildAndAddPort(DiagnosticPortBuilder builder, ErrorCallback callback = nullptr);
    static CQuickArrayList<DiagnosticPort*> s_rgpDiagnosticPorts;
    static Volatile<bool> s_isShutdown;
    // set this in GetNextAvailableStream, and then expose a callback that 
    // allows us to track which connections have sent their ResumeRuntime commands
    static DiagnosticPort *s_currentPort;

    // Polling timeout semantics
    // If client connection is opted in
    //   and connection succeeds => set timeout to infinite
    //   and connection fails => set timeout to minimum and scale by falloff factor
    // else => set timeout to -1 (infinite)
    //
    // If an agent closes its socket while we're still connected,
    // Poll will return and let us know which connection hung up
    static int32_t GetNextTimeout(int32_t currentTimeoutMs);
    constexpr static float s_pollTimeoutFalloffFactor = 1.25;
    constexpr static int32_t s_pollTimeoutInfinite = -1;
    constexpr static int32_t s_pollTimeoutMinMs = 10;
    constexpr static int32_t s_pollTimeoutMaxMs = 500;
};

#endif // FEATURE_PERFTRACING

#endif // __IPC_STREAM_FACTORY_H__ 

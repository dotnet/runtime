// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __IPC_STREAM_FACTORY_H__
#define __IPC_STREAM_FACTORY_H__

#ifdef FEATURE_PERFTRACING

#include "diagnosticsipc.h"

class IpcStreamFactory
{
public:
    struct ConnectionState
    {
    public:
        ConnectionState(IpcStream::DiagnosticsIpc *pIpc) :
            _pIpc(pIpc),
            _pStream(nullptr)
        { }

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
    };

    struct ClientConnectionState : public ConnectionState
    {
        ClientConnectionState(IpcStream::DiagnosticsIpc *pIpc) : ConnectionState(pIpc) { }

        // returns a pollable handle and performs any preparation required
        bool GetIpcPollHandle(IpcStream::DiagnosticsIpc::IpcPollHandle *pIpcPollHandle, ErrorCallback callback = nullptr) override;

        // Returns the signaled stream in a usable state
        IpcStream *GetConnectedStream(ErrorCallback callback = nullptr) override;

        // Resets the connection in the event of a hangup
        void Reset(ErrorCallback callback = nullptr) override;
    };

    struct ServerConnectionState : public ConnectionState
    {
        ServerConnectionState(IpcStream::DiagnosticsIpc *pIpc) : ConnectionState(pIpc) { }

        // returns a pollable handle and performs any preparation required
        bool GetIpcPollHandle(IpcStream::DiagnosticsIpc::IpcPollHandle *pIpcPollHandle, ErrorCallback callback = nullptr) override;

        // Returns the signaled stream in a usable state
        IpcStream *GetConnectedStream(ErrorCallback callback = nullptr) override;

        // Resets the connection in the event of a hangup
        void Reset(ErrorCallback callback = nullptr) override;
    };

    static bool CreateServer(const char *const pIpcName, ErrorCallback = nullptr);
    static bool CreateClient(const char *const pIpcName, ErrorCallback = nullptr);
    static IpcStream *GetNextAvailableStream(ErrorCallback = nullptr);
    static bool HasActiveConnections();
    static void CloseConnections(ErrorCallback callback = nullptr);
    static void Shutdown(ErrorCallback callback = nullptr);
private:
    static CQuickArrayList<ConnectionState*> s_rgpConnectionStates;
    static Volatile<bool> s_isShutdown;

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
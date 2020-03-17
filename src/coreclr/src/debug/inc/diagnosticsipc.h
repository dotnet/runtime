// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __DIAGNOSTICS_IPC_H__
#define __DIAGNOSTICS_IPC_H__

#include <stdint.h>

#ifdef TARGET_UNIX
  struct sockaddr_un;
#else
  #include <Windows.h>
#endif /* TARGET_UNIX */

typedef void (*ErrorCallback)(const char *szMessage, uint32_t code);

class IpcStream final
{
public:
    ~IpcStream();
    bool Read(void *lpBuffer, const uint32_t nBytesToRead, uint32_t &nBytesRead) const;
    bool Write(const void *lpBuffer, const uint32_t nBytesToWrite, uint32_t &nBytesWritten) const;
    bool Flush() const;

    // Poll
    // Paramters:
    // - IpcStream **pStreams: Array of pointers to IpcStreams to poll
    // - uint32_t nStreams: The number of streams to poll
    // - int32_t timeoutMs: The timeout in milliseconds for the poll (-1 == infinite)
    // - IpcStream **pStream: OUT PARAMETER nullptr for timeout or error, signalled stream for successful poll
    // Returns:
    // int32_t: -1 on error, 0 on timeout, >0 on successful poll
    // - if ppStream is != nullptr and -1 is returned, that connection was hungup and it shouldn't be treated as an error
    static int32_t Poll(IpcStream **ppStreams, uint32_t nStreams, int32_t timeoutMs, IpcStream **ppStream, ErrorCallback callback = nullptr);

    class DiagnosticsIpc final
    {
    public:
        enum ConnectionMode
        {
            CLIENT,
            SERVER
        };

        ConnectionMode mode;

        ~DiagnosticsIpc();

        //! Creates an IPC object
        static DiagnosticsIpc *Create(const char *const pIpcName, ConnectionMode mode, ErrorCallback callback = nullptr);

        //! Enables the underlaying IPC implementation to accept connection.
        IpcStream *Accept(bool shouldBlock, ErrorCallback callback = nullptr) const;

        IpcStream *Connect(ErrorCallback callback = nullptr);

        //! Closes an open IPC.
        void Close(ErrorCallback callback = nullptr);

    private:

#ifdef TARGET_UNIX
        const int _serverSocket;
        sockaddr_un *const _pServerAddress;
        bool _isClosed;

        DiagnosticsIpc(const int serverSocket, sockaddr_un *const pServerAddress, ConnectionMode mode = ConnectionMode::SERVER);

        //! Used to unlink the socket so it can be removed from the filesystem
        //! when the last reference to it is closed.
        void Unlink(ErrorCallback callback = nullptr);
#else
        static const uint32_t MaxNamedPipeNameLength = 256;
        char _pNamedPipeName[MaxNamedPipeNameLength]; // https://docs.microsoft.com/en-us/windows/desktop/api/winbase/nf-winbase-createnamedpipea

        DiagnosticsIpc(const char(&namedPipeName)[MaxNamedPipeNameLength], ConnectionMode mode = ConnectionMode::SERVER);
#endif /* TARGET_UNIX */

        DiagnosticsIpc() = delete;
        DiagnosticsIpc(const DiagnosticsIpc &src) = delete;
        DiagnosticsIpc(DiagnosticsIpc &&src) = delete;
        DiagnosticsIpc &operator=(const DiagnosticsIpc &rhs) = delete;
        DiagnosticsIpc &&operator=(DiagnosticsIpc &&rhs) = delete;
    };

private:
#ifdef TARGET_UNIX
    int _clientSocket = -1;
    int _serverSocket = -1;
    IpcStream(int clientSocket, int serverSocket, DiagnosticsIpc::ConnectionMode mode = DiagnosticsIpc::ConnectionMode::SERVER)
        : _clientSocket(clientSocket), _serverSocket(serverSocket), _mode(mode) {}
#else
    HANDLE _hPipe = INVALID_HANDLE_VALUE;
    OVERLAPPED _oOverlap = {};
    IpcStream(HANDLE hPipe, DiagnosticsIpc::ConnectionMode mode = DiagnosticsIpc::ConnectionMode::SERVER);
#endif /* TARGET_UNIX */

    DiagnosticsIpc::ConnectionMode _mode;

    IpcStream() = delete;
    IpcStream(const IpcStream &src) = delete;
    IpcStream(IpcStream &&src) = delete;
    IpcStream &operator=(const IpcStream &rhs) = delete;
    IpcStream &&operator=(IpcStream &&rhs) = delete;
};

#endif // __DIAGNOSTICS_IPC_H__

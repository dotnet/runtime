// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __DIAGNOSTICS_IPC_H__
#define __DIAGNOSTICS_IPC_H__

#include <stdint.h>

#ifdef FEATURE_PAL
  struct sockaddr_un;
#else
  #include <Windows.h>
#endif /* FEATURE_PAL */

typedef void (*ErrorCallback)(const char *szMessage, uint32_t code);

class IpcStream final
{
public:
    ~IpcStream();
    bool Read(void *lpBuffer, const uint32_t nBytesToRead, uint32_t &nBytesRead) const;
    bool Write(const void *lpBuffer, const uint32_t nBytesToWrite, uint32_t &nBytesWritten) const;
    bool Flush() const;

    class DiagnosticsIpc final
    {
    public:
        ~DiagnosticsIpc();

        //! Creates an IPC object
        static DiagnosticsIpc *Create(const char *const pIpcName, ErrorCallback callback = nullptr);

        //! Enables the underlaying IPC implementation to accept connection.
        IpcStream *Accept(ErrorCallback callback = nullptr) const;

        //! Closes an open IPC.
        void Close(ErrorCallback callback = nullptr);

    private:

#ifdef FEATURE_PAL
        const int _serverSocket;
        sockaddr_un *const _pServerAddress;
        bool _isClosed;

        DiagnosticsIpc(const int serverSocket, sockaddr_un *const pServerAddress);

        //! Used to unlink the socket so it can be removed from the filesystem
        //! when the last reference to it is closed.
        void Unlink(ErrorCallback callback = nullptr);
#else
        static const uint32_t MaxNamedPipeNameLength = 256;
        char _pNamedPipeName[MaxNamedPipeNameLength]; // https://docs.microsoft.com/en-us/windows/desktop/api/winbase/nf-winbase-createnamedpipea

        DiagnosticsIpc(const char(&namedPipeName)[MaxNamedPipeNameLength]);
#endif /* FEATURE_PAL */

        DiagnosticsIpc() = delete;
        DiagnosticsIpc(const DiagnosticsIpc &src) = delete;
        DiagnosticsIpc(DiagnosticsIpc &&src) = delete;
        DiagnosticsIpc &operator=(const DiagnosticsIpc &rhs) = delete;
        DiagnosticsIpc &&operator=(DiagnosticsIpc &&rhs) = delete;
    };

private:
#ifdef FEATURE_PAL
    int _clientSocket = -1;
    IpcStream(int clientSocket) : _clientSocket(clientSocket) {}
#else
    HANDLE _hPipe = INVALID_HANDLE_VALUE;
    IpcStream(HANDLE hPipe) : _hPipe(hPipe) {}
#endif /* FEATURE_PAL */

    IpcStream() = delete;
    IpcStream(const IpcStream &src) = delete;
    IpcStream(IpcStream &&src) = delete;
    IpcStream &operator=(const IpcStream &rhs) = delete;
    IpcStream &&operator=(IpcStream &&rhs) = delete;
};

#endif // __DIAGNOSTICS_IPC_H__

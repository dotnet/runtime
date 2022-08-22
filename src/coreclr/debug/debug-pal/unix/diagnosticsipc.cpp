// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <pal.h>
#include <pal_assert.h>
#include <unistd.h>
#include <fcntl.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <sys/stat.h>
#include "diagnosticsipc.h"
#include "processdescriptor.h"

#if __GNUC__
    #include <poll.h>
#else
    #include <sys/poll.h>
#endif // __GNUC__

IpcStream::DiagnosticsIpc::DiagnosticsIpc(const int serverSocket, sockaddr_un *const pServerAddress, ConnectionMode mode) :
    mode(mode),
    _serverSocket(serverSocket),
    _pServerAddress(new sockaddr_un),
    _isClosed(false),
    _isListening(false)
{
    _ASSERTE(_pServerAddress != nullptr);
    _ASSERTE(pServerAddress != nullptr);

    if (_pServerAddress == nullptr || pServerAddress == nullptr)
        return;
    memcpy(_pServerAddress, pServerAddress, sizeof(sockaddr_un));
}

IpcStream::DiagnosticsIpc::~DiagnosticsIpc()
{
    Close();
    delete _pServerAddress;
}

IpcStream::DiagnosticsIpc *IpcStream::DiagnosticsIpc::Create(const char *const pIpcName, ConnectionMode mode, ErrorCallback callback)
{
    sockaddr_un serverAddress{};
    serverAddress.sun_family = AF_UNIX;

    if (pIpcName != nullptr)
    {
        int chars = snprintf(serverAddress.sun_path, sizeof(serverAddress.sun_path), "%s", pIpcName);
        _ASSERTE(chars > 0 && (unsigned int)chars < sizeof(serverAddress.sun_path));
    }
    else
    {
        // generate the default socket name in TMP Path
        const ProcessDescriptor pd = ProcessDescriptor::FromCurrentProcess();
        PAL_GetTransportName(
            sizeof(serverAddress.sun_path),
            serverAddress.sun_path,
            "dotnet-diagnostic",
            pd.m_Pid,
            pd.m_ApplicationGroupId,
            "socket");
    }

    if (mode == ConnectionMode::CONNECT)
        return new IpcStream::DiagnosticsIpc(-1, &serverAddress, ConnectionMode::CONNECT);

#if defined(__APPLE__) || defined(__FreeBSD__)
    mode_t prev_mask = umask(~(S_IRUSR | S_IWUSR)); // This will set the default permission bit to 600
#endif // __APPLE__

    const int serverSocket = ::socket(AF_UNIX, SOCK_STREAM, 0);
    if (serverSocket == -1)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
#if defined(__APPLE__) || defined(__FreeBSD__)
        umask(prev_mask);
#endif // __APPLE__
        _ASSERTE(!"Failed to create diagnostics IPC socket.");
        return nullptr;
    }

#if !(defined(__APPLE__) || defined(__FreeBSD__))
    if (fchmod(serverSocket, S_IRUSR | S_IWUSR) == -1)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
        _ASSERTE(!"Failed to set permissions on diagnostics IPC socket.");
        return nullptr;
    }
#endif // __APPLE__

    const int fSuccessBind = ::bind(serverSocket, (sockaddr *)&serverAddress, sizeof(serverAddress));
    if (fSuccessBind == -1)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
        _ASSERTE(fSuccessBind != -1);

        const int fSuccessClose = ::close(serverSocket);
        _ASSERTE(fSuccessClose != -1);

#if defined(__APPLE__) || defined(__FreeBSD__)
        umask(prev_mask);
#endif // __APPLE__

        return nullptr;
    }

#if defined(__APPLE__) || defined(__FreeBSD__)
    umask(prev_mask);
#endif // __APPLE__

    return new IpcStream::DiagnosticsIpc(serverSocket, &serverAddress, mode);
}

bool IpcStream::DiagnosticsIpc::Listen(ErrorCallback callback)
{
    _ASSERTE(mode == ConnectionMode::LISTEN);
    if (mode != ConnectionMode::LISTEN)
    {
        if (callback != nullptr)
            callback("Cannot call Listen on a client connection", -1);
        return false;
    }

    if (_isListening)
        return true;

    const int fSuccessfulListen = ::listen(_serverSocket, /* backlog */ 255);
    if (fSuccessfulListen == -1)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
        _ASSERTE(fSuccessfulListen != -1);

        const int fSuccessUnlink = ::unlink(_pServerAddress->sun_path);
        _ASSERTE(fSuccessUnlink != -1);

        const int fSuccessClose = ::close(_serverSocket);
        _ASSERTE(fSuccessClose != -1);
        return false;
    }
    else
    {
        _isListening = true;
        return true;
    }
}

IpcStream *IpcStream::DiagnosticsIpc::Accept(ErrorCallback callback)
{
    _ASSERTE(mode == ConnectionMode::LISTEN);
    _ASSERTE(_isListening);

    sockaddr_un from;
    socklen_t fromlen = sizeof(from);
    const int clientSocket = ::accept(_serverSocket, (sockaddr *)&from, &fromlen);
    if (clientSocket == -1)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
        return nullptr;
    }

    return new IpcStream(clientSocket, mode);
}

IpcStream *IpcStream::DiagnosticsIpc::Connect(ErrorCallback callback)
{
    _ASSERTE(mode == ConnectionMode::CONNECT);

    sockaddr_un clientAddress{};
    clientAddress.sun_family = AF_UNIX;
    const int clientSocket = ::socket(AF_UNIX, SOCK_STREAM, 0);
    if (clientSocket == -1)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
        return nullptr;
    }

    // We don't expect this to block since this is a Unix Domain Socket.  `connect` may block until the
    // TCP handshake is complete for TCP/IP sockets, but UDS don't use TCP.  `connect` will return even if
    // the server hasn't called `accept`.
    if (::connect(clientSocket, (struct sockaddr *)_pServerAddress, sizeof(*_pServerAddress)) < 0)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);

        const bool fCloseSuccess = ::close(clientSocket) == 0;
        if (!fCloseSuccess && callback != nullptr)
            callback(strerror(errno), errno);
        return nullptr;
    }

    return new IpcStream(clientSocket, ConnectionMode::CONNECT);
}

int32_t IpcStream::DiagnosticsIpc::Poll(IpcPollHandle *rgIpcPollHandles, uint32_t nHandles, int32_t timeoutMs, ErrorCallback callback)
{
    // prepare the pollfd structs
    pollfd *pollfds = new pollfd[nHandles];
    for (uint32_t i = 0; i < nHandles; i++)
    {
        rgIpcPollHandles[i].revents = 0; // ignore any values in revents
        int fd = -1;
        if (rgIpcPollHandles[i].pIpc != nullptr)
        {
            // SERVER
            _ASSERTE(rgIpcPollHandles[i].pIpc->mode == ConnectionMode::LISTEN);
            fd = rgIpcPollHandles[i].pIpc->_serverSocket;
        }
        else
        {
            // CLIENT
            _ASSERTE(rgIpcPollHandles[i].pStream != nullptr);
            fd = rgIpcPollHandles[i].pStream->_clientSocket;
        }

        pollfds[i].fd = fd;
        pollfds[i].events = POLLIN;
    }

    int retval = poll(pollfds, nHandles, timeoutMs);

    // Check results
    if (retval < 0)
    {
        //     If poll() returns with an error, including one due to an interrupted call, the fds
        //  array will be unmodified and the global variable errno will be set to indicate the error.
        // - POLL(2)
        if (callback != nullptr)
            callback(strerror(errno), errno);
        delete[] pollfds;
        return -1;
    }
    else if (retval == 0)
    {
        // we timed out
        delete[] pollfds;
        return 0;
    }

    for (uint32_t i = 0; i < nHandles; i++)
    {
        if (pollfds[i].revents != 0)
        {
            if (callback != nullptr)
                callback("IpcStream::DiagnosticsIpc::Poll - poll revents", (uint32_t)pollfds[i].revents);
            // error check FIRST
            if (pollfds[i].revents & POLLHUP)
            {
                // check for hangup first because a closed socket
                // will technically meet the requirements for POLLIN
                // i.e., a call to recv/read won't block
                rgIpcPollHandles[i].revents = (uint8_t)PollEvents::HANGUP;
            }
            else if ((pollfds[i].revents & (POLLERR|POLLNVAL)))
            {
                if (callback != nullptr)
                    callback("Poll error", (uint32_t)pollfds[i].revents);
                rgIpcPollHandles[i].revents = (uint8_t)PollEvents::ERR;
            }
            else if (pollfds[i].revents & (POLLIN|POLLPRI))
            {
                rgIpcPollHandles[i].revents = (uint8_t)PollEvents::SIGNALED;
            }
            else
            {
                rgIpcPollHandles[i].revents = (uint8_t)PollEvents::UNKNOWN;
                if (callback != nullptr)
                    callback("unknown poll response", (uint32_t)pollfds[i].revents);
            }
        }
    }

    delete[] pollfds;
    return 1;
}

void IpcStream::DiagnosticsIpc::Close(bool isShutdown, ErrorCallback callback)
{
    if (_isClosed)
        return;
    _isClosed = true;

    if (_serverSocket != -1)
    {
        // only close the socket if not shutting down, let the OS handle it in that case
        if (!isShutdown && ::close(_serverSocket) == -1)
        {
            if (callback != nullptr)
                callback(strerror(errno), errno);
            _ASSERTE(!"Failed to close unix domain socket.");
        }

        // N.B. - it is safe to unlink the unix domain socket file while the server
        // is still alive:
        // "The usual UNIX close-behind semantics apply; the socket can be unlinked
        // at any time and will be finally removed from the file system when the last
        // reference to it is closed." - unix(7) man page
        Unlink(callback);
    }
}

// This helps remove the socket from the filesystem when the runtime exits.
// See: http://man7.org/linux/man-pages/man7/unix.7.html#NOTES
void IpcStream::DiagnosticsIpc::Unlink(ErrorCallback callback)
{
    const int fSuccessUnlink = ::unlink(_pServerAddress->sun_path);
    if (fSuccessUnlink == -1)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
        _ASSERTE(!"Failed to unlink server address.");
    }
}

IpcStream::~IpcStream()
{
    Close();
}

void IpcStream::Close(ErrorCallback)
{
    if (_clientSocket != -1)
    {
        Flush();

        const int fSuccessClose = ::close(_clientSocket);
        _ASSERTE(fSuccessClose != -1);
        _clientSocket = -1;
    }
}

bool IpcStream::Read(void *lpBuffer, const uint32_t nBytesToRead, uint32_t &nBytesRead, const int32_t timeoutMs)
{
    _ASSERTE(lpBuffer != nullptr);

    if (timeoutMs != InfiniteTimeout)
    {
        pollfd pfd;
        pfd.fd = _clientSocket;
        pfd.events = POLLIN;
        int retval = poll(&pfd, 1, timeoutMs);
        if (retval <= 0 || !(pfd.revents & POLLIN))
        {
            // timeout or error
            return false;
        }
        // else fallthrough
    }

    uint8_t *lpBufferCursor = (uint8_t*)lpBuffer;
    ssize_t currentBytesRead = 0;
    ssize_t totalBytesRead = 0;
    bool fSuccess = true;
    while (fSuccess && nBytesToRead - totalBytesRead > 0)
    {
        currentBytesRead = ::recv(_clientSocket, lpBufferCursor, nBytesToRead - totalBytesRead, 0);
        fSuccess = currentBytesRead != 0;
        if (!fSuccess)
            break;
        totalBytesRead += currentBytesRead;
        lpBufferCursor += currentBytesRead;
    }

    if (!fSuccess)
    {
        // TODO: Add error handling.
    }

    nBytesRead = static_cast<uint32_t>(totalBytesRead);
    return fSuccess;
}

bool IpcStream::Write(const void *lpBuffer, const uint32_t nBytesToWrite, uint32_t &nBytesWritten, const int32_t timeoutMs)
{
    _ASSERTE(lpBuffer != nullptr);

    if (timeoutMs != InfiniteTimeout)
    {
        pollfd pfd;
        pfd.fd = _clientSocket;
        pfd.events = POLLOUT;
        int retval = poll(&pfd, 1, timeoutMs);
        if (retval <= 0 || !(pfd.revents & POLLOUT))
        {
            // timeout or error
            return false;
        }
        // else fallthrough
    }

    uint8_t *lpBufferCursor = (uint8_t*)lpBuffer;
    ssize_t currentBytesWritten = 0;
    ssize_t totalBytesWritten = 0;
    bool fSuccess = true;
    while (fSuccess && nBytesToWrite - totalBytesWritten > 0)
    {
        currentBytesWritten = ::send(_clientSocket, lpBufferCursor, nBytesToWrite - totalBytesWritten, 0);
        fSuccess = currentBytesWritten != -1;
        if (!fSuccess)
            break;
        lpBufferCursor += currentBytesWritten;
        totalBytesWritten += currentBytesWritten;
    }

    if (!fSuccess)
    {
        // TODO: Add error handling.
    }

    nBytesWritten = static_cast<uint32_t>(totalBytesWritten);
    return fSuccess;
}

bool IpcStream::Flush() const
{
    // fsync - http://man7.org/linux/man-pages/man2/fsync.2.html ???
    return true;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <pal.h>
#include <pal_assert.h>
#include <unistd.h>
#include <fcntl.h>
#include <sys/socket.h>
#include <sys/poll.h>
#include <sys/un.h>
#include <sys/stat.h>
#include "diagnosticsipc.h"
#include "processdescriptor.h"

IpcStream::DiagnosticsIpc::DiagnosticsIpc(const int serverSocket, sockaddr_un *const pServerAddress, ConnectionMode mode) :
    mode(mode),
    _serverSocket(serverSocket),
    _pServerAddress(new sockaddr_un),
    _isClosed(false)
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

    if (mode == ConnectionMode::CLIENT)
        return new IpcStream::DiagnosticsIpc(-1, &serverAddress, ConnectionMode::CLIENT);

#ifdef __APPLE__
    mode_t prev_mask = umask(~(S_IRUSR | S_IWUSR)); // This will set the default permission bit to 600
#endif // __APPLE__

    const int serverSocket = ::socket(AF_UNIX, SOCK_STREAM, 0);
    if (serverSocket == -1)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
#ifdef __APPLE__
        umask(prev_mask);
#endif // __APPLE__
        _ASSERTE(!"Failed to create diagnostics IPC socket.");
        return nullptr;
    }

#ifndef __APPLE__
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

#ifdef __APPLE__
        umask(prev_mask);
#endif // __APPLE__

        return nullptr;
    }

    const int fSuccessfulListen = ::listen(serverSocket, /* backlog */ 255);
    if (fSuccessfulListen == -1)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
        _ASSERTE(fSuccessfulListen != -1);

        const int fSuccessUnlink = ::unlink(serverAddress.sun_path);
        _ASSERTE(fSuccessUnlink != -1);

        const int fSuccessClose = ::close(serverSocket);
        _ASSERTE(fSuccessClose != -1);
#ifdef __APPLE__
        umask(prev_mask);
#endif // __APPLE__
        return nullptr;
    }

#ifdef __APPLE__
    umask(prev_mask);
#endif // __APPLE__

    return new IpcStream::DiagnosticsIpc(serverSocket, &serverAddress, mode);
}

IpcStream *IpcStream::DiagnosticsIpc::Connect(ErrorCallback callback)
{
    sockaddr_un clientAddress{};
    clientAddress.sun_family = AF_UNIX;
    const int clientSocket = ::socket(AF_UNIX, SOCK_STREAM, 0);
    if (clientSocket == -1)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
        return nullptr;
        // TODO: unlinks?
    }

    if (::connect(clientSocket, (struct sockaddr *)_pServerAddress, sizeof(*_pServerAddress)) < 0)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
        return nullptr;
        // TODO: Anything else?
    }

    return new IpcStream(clientSocket, -1, ConnectionMode::CLIENT);
}

IpcStream *IpcStream::DiagnosticsIpc::Accept(bool shouldBlock, ErrorCallback callback) const
{
    sockaddr_un from;
    socklen_t fromlen = sizeof(from);
    const int clientSocket = shouldBlock ? ::accept(_serverSocket, (sockaddr *)&from, &fromlen) : -1;
    if (shouldBlock && clientSocket == -1)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
        return nullptr;
    }

    return new IpcStream(clientSocket, _serverSocket);
}

IpcStream *IpcStream::Select(IpcStream **pStreams, uint32_t nStreams, ErrorCallback callback)
{
    pollfd *pollfds = new pollfd[nStreams];
    for (uint32_t i = 0; i < nStreams; i++)
    {
        int fd = -1;
        if (pStreams[i]->_mode == DiagnosticsIpc::ConnectionMode::SERVER && pStreams[i]->_clientSocket == -1)
        {
            fd = pStreams[i]->_serverSocket;
        }
        else
        {
            fd = pStreams[i]->_clientSocket;
        }

        pollfds[i].fd = fd;
        pollfds[i].events = POLLIN;
    }

    int retval = poll(pollfds, nStreams, -1); // -1 = infinite
    
    if (retval <= 0)
    {
        for (uint32_t i = 0; i < nStreams; i++)
        {
            if ((pollfds[i].revents & POLLERR) && callback != nullptr)
                callback(strerror(errno), errno);
        }
        delete[] pollfds;
        return nullptr;
    }

    IpcStream *pStream = nullptr;
    for (uint32_t i = 0; i < nStreams; i++)
    {
        if (pollfds[i].revents != 0)
        {
            bool needToAccept = pStreams[i]->_mode == DiagnosticsIpc::ConnectionMode::SERVER && pStreams[i]->_clientSocket == -1;
            if (pollfds[i].revents & POLLIN)
            {
                if (needToAccept)
                {
                    sockaddr_un from;
                    socklen_t fromlen = sizeof(from);
                    const int clientSocket = ::accept(pStreams[i]->_serverSocket, (sockaddr *)&from, &fromlen);
                    if (clientSocket == -1)
                    {
                        if (callback != nullptr)
                            callback(strerror(errno), errno);
                        delete[] pollfds;
                        return nullptr;
                    }
                    pStream = new IpcStream(clientSocket, pStreams[i]->_serverSocket, pStreams[i]->_mode);
                }
                else
                {
                    pStream = pStreams[i];
                }
                break;
            }
        }
        else
        {
            if ((pollfds[i].revents & POLLERR) && callback != nullptr)
            {
                callback("POLLERR", POLLERR);
            }
            else if ((pollfds[i].revents & POLLNVAL) && callback != nullptr)
            {
                callback("POLLNVAL", POLLNVAL);
            }

            delete[] pollfds;
            return nullptr;
        }
    }

    delete[] pollfds;
    return pStream;
}

void IpcStream::DiagnosticsIpc::Close(ErrorCallback callback)
{
    if (_isClosed)
        return;
    _isClosed = true;

    if (_serverSocket != -1)
    {
        if (::close(_serverSocket) == -1)
        {
            if (callback != nullptr)
                callback(strerror(errno), errno);
            _ASSERTE(!"Failed to close unix domain socket.");
        }

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
    if (_clientSocket != -1)
    {
        Flush();

        const int fSuccessClose = ::close(_clientSocket);
        _ASSERTE(fSuccessClose != -1);
    }
}

bool IpcStream::Read(void *lpBuffer, const uint32_t nBytesToRead, uint32_t &nBytesRead) const
{
    _ASSERTE(lpBuffer != nullptr);

    const ssize_t ssize = ::recv(_clientSocket, lpBuffer, nBytesToRead, 0);
    const bool fSuccess = ssize != -1;

    if (!fSuccess)
    {
        // TODO: Add error handling.
    }

    nBytesRead = static_cast<uint32_t>(ssize);
    return fSuccess;
}

bool IpcStream::Write(const void *lpBuffer, const uint32_t nBytesToWrite, uint32_t &nBytesWritten) const
{
    _ASSERTE(lpBuffer != nullptr);

    const ssize_t ssize = ::send(_clientSocket, lpBuffer, nBytesToWrite, 0);
    const bool fSuccess = ssize != -1;

    if (!fSuccess)
    {
        // TODO: Add error handling.
    }

    nBytesWritten = static_cast<uint32_t>(ssize);
    return fSuccess;
}

bool IpcStream::Flush() const
{
    // fsync - http://man7.org/linux/man-pages/man2/fsync.2.html ???
    return true;
}

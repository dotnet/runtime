// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <pal.h>
#include <pal_assert.h>
#include <unistd.h>
#include <fcntl.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <sys/stat.h>
#include "diagnosticsipc.h"
#include "processdescriptor.h"

IpcStream::DiagnosticsIpc::DiagnosticsIpc(const int serverSocket, sockaddr_un *const pServerAddress) :
    _serverSocket(serverSocket),
    _pServerAddress(new sockaddr_un),
    _isUnlinked(false)
{
    _ASSERTE(_pServerAddress != nullptr);
    _ASSERTE(_serverSocket != -1);
    _ASSERTE(pServerAddress != nullptr);

    if (_pServerAddress == nullptr || pServerAddress == nullptr)
        return;
    memcpy(_pServerAddress, pServerAddress, sizeof(sockaddr_un));
}

IpcStream::DiagnosticsIpc::~DiagnosticsIpc()
{
    if (_serverSocket != -1)
    {
        const int fSuccessClose = ::close(_serverSocket);
        _ASSERTE(fSuccessClose != -1); // TODO: Add error handling?

        Unlink();

        delete _pServerAddress;
    }
}

IpcStream::DiagnosticsIpc *IpcStream::DiagnosticsIpc::Create(const char *const pIpcName, ErrorCallback callback)
{
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
    sockaddr_un serverAddress{};
    serverAddress.sun_family = AF_UNIX;
    const ProcessDescriptor pd = ProcessDescriptor::FromCurrentProcess();
    PAL_GetTransportName(
        sizeof(serverAddress.sun_path),
        serverAddress.sun_path,
        pIpcName,
        pd.m_Pid,
        pd.m_ApplicationGroupId,
        "socket");

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

    return new IpcStream::DiagnosticsIpc(serverSocket, &serverAddress);
}

IpcStream *IpcStream::DiagnosticsIpc::Accept(ErrorCallback callback) const
{
    sockaddr_un from;
    socklen_t fromlen = sizeof(from);
    const int clientSocket = ::accept(_serverSocket, (sockaddr *)&from, &fromlen);
    if (clientSocket == -1)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
        return nullptr;
    }

    return new IpcStream(clientSocket);
}

//! This helps remove the socket from the filesystem when the runtime exits.
//! From: http://man7.org/linux/man-pages/man7/unix.7.html#NOTES
//!   Binding to a socket with a filename creates a socket in the
//!   filesystem that must be deleted by the caller when it is no longer
//!   needed (using unlink(2)).  The usual UNIX close-behind semantics
//!   apply; the socket can be unlinked at any time and will be finally
//!   removed from the filesystem when the last reference to it is closed.
void IpcStream::DiagnosticsIpc::Unlink(ErrorCallback callback)
{
    if (_isUnlinked)
        return;
    _isUnlinked = true;

    const int fSuccessUnlink = ::unlink(_pServerAddress->sun_path);
    if (fSuccessUnlink == -1)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
        _ASSERTE(fSuccessUnlink == 0);
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <pal.h>
#include <pal_assert.h>
#include <new>
#include <unistd.h>
#include <fcntl.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/un.h>
#include "diagnosticsipc.h"
#include "processdescriptor.h"

IpcStream::DiagnosticsIpc::DiagnosticsIpc(const int serverSocket, sockaddr_un *const pServerAddress) :
    _serverSocket(serverSocket),
    _pServerAddress(new (std::nothrow) sockaddr_un)
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

        const int fSuccessUnlink = ::unlink(_pServerAddress->sun_path);
        _ASSERTE(fSuccessUnlink != -1); // TODO: Add error handling?

        delete _pServerAddress;
    }
}

IpcStream::DiagnosticsIpc *IpcStream::DiagnosticsIpc::Create(const char *const pIpcName, ErrorCallback callback)
{
    const int serverSocket = ::socket(AF_UNIX, SOCK_STREAM, 0);
    if (serverSocket == -1)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
        _ASSERTE(serverSocket != -1);
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

    const int fSuccessBind = ::bind(serverSocket, (sockaddr *)&serverAddress, sizeof(serverAddress));
    if (fSuccessBind == -1)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
        _ASSERTE(fSuccessBind != -1);
        return nullptr;
    }

    return new IpcStream::DiagnosticsIpc(serverSocket, &serverAddress);
}

IpcStream *IpcStream::DiagnosticsIpc::Accept(ErrorCallback callback) const
{
    if (::listen(_serverSocket, /* backlog */ 255) == -1)
        return nullptr;
    sockaddr_un from;
    socklen_t fromlen = sizeof(from);
    const int clientSocket = ::accept(_serverSocket, (sockaddr *)&from, &fromlen);
    if (clientSocket == -1)
    {
        if (callback != nullptr)
            callback(strerror(errno), errno);
        return nullptr;
    }

    auto pIpcStream = new (std::nothrow) IpcStream(clientSocket);
    if (pIpcStream == nullptr && callback != nullptr)
        callback("Failed to allocate an IpcStream object.", 1);
    return pIpcStream;
}

IpcStream::~IpcStream()
{
    if (_clientSocket != -1)
    {
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

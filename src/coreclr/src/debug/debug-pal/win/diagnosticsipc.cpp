// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <assert.h>
#include <stdio.h>
#include "diagnosticsipc.h"

IpcStream::DiagnosticsIpc::DiagnosticsIpc(const char(&namedPipeName)[MaxNamedPipeNameLength])
{
    memcpy(_pNamedPipeName, namedPipeName, sizeof(_pNamedPipeName));
}

IpcStream::DiagnosticsIpc::~DiagnosticsIpc()
{
}

IpcStream::DiagnosticsIpc *IpcStream::DiagnosticsIpc::Create(const char *const pIpcName, ErrorCallback callback)
{
    assert(pIpcName != nullptr);
    if (pIpcName == nullptr)
        return nullptr;

    char namedPipeName[MaxNamedPipeNameLength]{};
    const int nCharactersWritten = sprintf_s(
        namedPipeName,
        sizeof(namedPipeName),
        "\\\\.\\pipe\\%s-%d",
        pIpcName,
        ::GetCurrentProcessId());

    if (nCharactersWritten == -1)
    {
        if (callback != nullptr)
            callback("Failed to generate the named pipe name", nCharactersWritten);
        assert(nCharactersWritten != -1);
        return nullptr;
    }

    return new IpcStream::DiagnosticsIpc(namedPipeName);
}

IpcStream *IpcStream::DiagnosticsIpc::Accept(ErrorCallback callback) const
{
    const uint32_t nInBufferSize = 16 * 1024;
    const uint32_t nOutBufferSize = 16 * 1024;
    HANDLE hPipe = ::CreateNamedPipeA(
        _pNamedPipeName,                                            // pipe name
        PIPE_ACCESS_DUPLEX,                                         // read/write access
        PIPE_TYPE_BYTE | PIPE_WAIT | PIPE_REJECT_REMOTE_CLIENTS,    // message type pipe, message-read and blocking mode
        PIPE_UNLIMITED_INSTANCES,                                   // max. instances
        nOutBufferSize,                                             // output buffer size
        nInBufferSize,                                              // input buffer size
        0,                                                          // default client time-out
        NULL);                                                      // default security attribute

    if (hPipe == INVALID_HANDLE_VALUE)
    {
        if (callback != nullptr)
            callback("Failed to create an instance of a named pipe.", ::GetLastError());
        return nullptr;
    }

    const BOOL fSuccess = ::ConnectNamedPipe(hPipe, NULL) != 0;
    const DWORD errorCode = ::GetLastError();
    if (!fSuccess && (errorCode != ERROR_PIPE_CONNECTED))
    {
        if (callback != nullptr)
            callback("Failed to wait for a client process to connect.", errorCode);
        return nullptr;
    }

    return new IpcStream(hPipe);
}

void IpcStream::DiagnosticsIpc::Unlink(ErrorCallback)
{
}

IpcStream::~IpcStream()
{
    if (_hPipe != INVALID_HANDLE_VALUE)
    {
        Flush();

        const BOOL fSuccessDisconnectNamedPipe = ::DisconnectNamedPipe(_hPipe);
        assert(fSuccessDisconnectNamedPipe != 0);

        const BOOL fSuccessCloseHandle = ::CloseHandle(_hPipe);
        assert(fSuccessCloseHandle != 0);
    }
}

bool IpcStream::Read(void *lpBuffer, const uint32_t nBytesToRead, uint32_t &nBytesRead) const
{
    assert(lpBuffer != nullptr);

    DWORD nNumberOfBytesRead = 0;
    const bool fSuccess = ::ReadFile(
        _hPipe,                 // handle to pipe
        lpBuffer,               // buffer to receive data
        nBytesToRead,           // size of buffer
        &nNumberOfBytesRead,    // number of bytes read
        NULL) != 0;             // not overlapped I/O

    if (!fSuccess)
    {
        // TODO: Add error handling.
    }

    nBytesRead = static_cast<uint32_t>(nNumberOfBytesRead);
    return fSuccess;
}

bool IpcStream::Write(const void *lpBuffer, const uint32_t nBytesToWrite, uint32_t &nBytesWritten) const
{
    assert(lpBuffer != nullptr);

    DWORD nNumberOfBytesWritten = 0;
    const bool fSuccess = ::WriteFile(
        _hPipe,                 // handle to pipe
        lpBuffer,               // buffer to write from
        nBytesToWrite,          // number of bytes to write
        &nNumberOfBytesWritten, // number of bytes written
        NULL) != 0;             // not overlapped I/O

    if (!fSuccess)
    {
        // TODO: Add error handling.
    }

    nBytesWritten = static_cast<uint32_t>(nNumberOfBytesWritten);
    return fSuccess;
}

bool IpcStream::Flush() const
{
    const bool fSuccess = ::FlushFileBuffers(_hPipe) != 0;
    if (!fSuccess)
    {
        // TODO: Add error handling.
    }
    return fSuccess;
}

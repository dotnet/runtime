// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <assert.h>
#include <stdio.h>
#include <stdlib.h>
#include "diagnosticsipc.h"

IpcStream::DiagnosticsIpc::DiagnosticsIpc(const char(&namedPipeName)[MaxNamedPipeNameLength])
{
    memcpy(_pNamedPipeName, namedPipeName, sizeof(_pNamedPipeName));
}

IpcStream::DiagnosticsIpc::~DiagnosticsIpc()
{
    Close();
}

IpcStream::DiagnosticsIpc *IpcStream::DiagnosticsIpc::Create(const char *const pIpcName, ErrorCallback callback)
{
    char namedPipeName[MaxNamedPipeNameLength]{};
    int nCharactersWritten = -1;

    if (pIpcName != nullptr)
    {
        nCharactersWritten = sprintf_s(
            namedPipeName,
            sizeof(namedPipeName),
            "\\\\.\\pipe\\%s",
            pIpcName);
    }
    else
    {
        nCharactersWritten = sprintf_s(
            namedPipeName,
            sizeof(namedPipeName),
            "\\\\.\\pipe\\dotnet-diagnostic-%d",
            ::GetCurrentProcessId());
    }

    if (nCharactersWritten == -1)
    {
        if (callback != nullptr)
            callback("Failed to generate the named pipe name", nCharactersWritten);
        assert(nCharactersWritten != -1);
        return nullptr;
    }

    return new IpcStream::DiagnosticsIpc(namedPipeName);
}

IpcStream *IpcStream::DiagnosticsIpc::Accept(bool shouldBlock, ErrorCallback callback) const
{
    const uint32_t nInBufferSize = 16 * 1024;
    const uint32_t nOutBufferSize = 16 * 1024;
    HANDLE hPipe = ::CreateNamedPipeA(
        _pNamedPipeName,                                            // pipe name
        PIPE_ACCESS_DUPLEX,                                         // read/write access
        PIPE_TYPE_BYTE | 
        (shouldBlock ? PIPE_WAIT : PIPE_NOWAIT) | 
        PIPE_REJECT_REMOTE_CLIENTS,                                 // message type pipe, message-read and blocking mode
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
    if (!fSuccess)
    {
        const DWORD errorCode = ::GetLastError();
        switch (errorCode)
        {
            case ERROR_PIPE_LISTENING:
                // Occurs when there isn't a pending client and we're
                // in PIPE_NOWAIT mode
            case ERROR_PIPE_CONNECTED:
                // Occurs when a client connects before the function is called.
                // In this case, there is a connection between client and
                // server, even though the function returned zero.
                break;

            default:
                if (callback != nullptr)
                    callback("A client process failed to connect.", errorCode);
                ::CloseHandle(hPipe);
                return nullptr;
        }
    }

    return new IpcStream(hPipe);
}

IpcStream *IpcStream::DiagnosticsIpc::Connect(const char *const pIpcName, ErrorCallback callback)
{
    DiagnosticsIpc *diagnosticsIpc = DiagnosticsIpc::Create(pIpcName, callback);
    const uint32_t nInBufferSize = 16 * 1024;
    const uint32_t nOutBufferSize = 16 * 1024;
    HANDLE hPipe = ::CreateFileA( 
        diagnosticsIpc->_pNamedPipeName,    // pipe name 
        GENERIC_READ |                      // read and write access 
        GENERIC_WRITE, 
        0,                                  // no sharing 
        NULL,                               // default security attributes
        OPEN_EXISTING,                      // opens existing pipe 
        0,                                  // default attributes 
        NULL);                              // no template file

    if (hPipe == INVALID_HANDLE_VALUE)
    {
        if (callback != nullptr)
            callback("Failed to connect to named pipe.", ::GetLastError());
        return nullptr;
    }

    return new IpcStream(hPipe, ConnectionMode::CLIENT);
}

void IpcStream::DiagnosticsIpc::Close(ErrorCallback)
{
}

IpcStream::~IpcStream()
{
    if (_hPipe != INVALID_HANDLE_VALUE)
    {
        Flush();

        if (_mode == ConnectionMode::SERVER)
        {
            const BOOL fSuccessDisconnectNamedPipe = ::DisconnectNamedPipe(_hPipe);
            assert(fSuccessDisconnectNamedPipe != 0);
        }

        const BOOL fSuccessCloseHandle = ::CloseHandle(_hPipe);
        assert(fSuccessCloseHandle != 0);
    }
}

IpcStream *IpcStream::Select(IpcStream **pStreams, uint32_t nStreams, ErrorCallback callback)
{
    // load up an array of handles
    HANDLE *pHandles = new HANDLE[nStreams];
    for (uint32_t i = 0; i < nStreams; i++)
        pHandles[i] = pStreams[i]->_hPipe;

    // call wait for multiple obj
    DWORD dwWait = WaitForMultipleObjects(
        nStreams,       // count
        pHandles,       // handles
        false,          // Don't wait all
        INFINITE);      // wait infinitely

    // determine which of the streams signaled
    DWORD index = dwWait - WAIT_OBJECT_0;
    if (index < 0 || index > (nStreams - 1))
    {
        if (callback != nullptr)
            callback("Failed to select to named pipe.", ::GetLastError());
        return nullptr;
    }

    // set that stream's mode to blocking
    bool result = SetNamedPipeHandleState(
        pHandles[index],                // handle
        PIPE_READMODE_BYTE | PIPE_WAIT, // read mode and wait mode
        NULL,                           // no collecting
        NULL);                          // no collecting

    // cleanup and return that stream
    delete pHandles;
    return pStreams[index];
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

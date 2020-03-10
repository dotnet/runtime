// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <assert.h>
#include <stdio.h>
#include <stdlib.h>
#include "diagnosticsipc.h"

#define _ASSERTE assert

IpcStream::DiagnosticsIpc::DiagnosticsIpc(const char(&namedPipeName)[MaxNamedPipeNameLength], ConnectionMode mode)
    : mode(mode)
{
    memcpy(_pNamedPipeName, namedPipeName, sizeof(_pNamedPipeName));
}

IpcStream::DiagnosticsIpc::~DiagnosticsIpc()
{
    Close();
}

IpcStream::DiagnosticsIpc *IpcStream::DiagnosticsIpc::Create(const char *const pIpcName, ConnectionMode mode, ErrorCallback callback)
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

    if (mode == ConnectionMode::CLIENT)
    {
        // TODO: block here till the socket exists?
    }

    if (nCharactersWritten == -1)
    {
        if (callback != nullptr)
            callback("Failed to generate the named pipe name", nCharactersWritten);
        _ASSERTE(nCharactersWritten != -1);
        return nullptr;
    }

    return new IpcStream::DiagnosticsIpc(namedPipeName, mode);
}

IpcStream *IpcStream::DiagnosticsIpc::Accept(bool shouldBlock, ErrorCallback callback) const
{
    _ASSERTE(mode == ConnectionMode::SERVER);
    if (mode != ConnectionMode::SERVER)
    {
        if (callback != nullptr)
            callback("Cannot call accept on a client connection", 0);
        return nullptr;
    }

    const uint32_t nInBufferSize = 16 * 1024;
    const uint32_t nOutBufferSize = 16 * 1024;
    HANDLE hPipe = ::CreateNamedPipeA(
        _pNamedPipeName,                                            // pipe name
        PIPE_ACCESS_DUPLEX |
        FILE_FLAG_OVERLAPPED,                                       // read/write access
        PIPE_TYPE_BYTE | 
        PIPE_WAIT | 
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

    // TODO: Find a better way to do this than
    // mixing abstractions
    IpcStream *pStream = new IpcStream(hPipe, mode);

    BOOL fSuccess = ::ConnectNamedPipe(hPipe, &pStream->_oOverlap) != 0;
    if (!fSuccess)
    {
        const DWORD errorCode = ::GetLastError();
        switch (errorCode)
        {
            case ERROR_PIPE_LISTENING:
                // Occurs when there isn't a pending client and we're
                // in PIPE_NOWAIT mode
            case ERROR_IO_PENDING:
                if (shouldBlock)
                {
                    fSuccess = GetOverlappedResult(pStream->_hPipe,
                                                   &pStream->_oOverlap,
                                                   NULL,
                                                   true);
                }
            case ERROR_PIPE_CONNECTED:
                // Occurs when a client connects before the function is called.
                // In this case, there is a connection between client and
                // server, even though the function returned zero.
                break;

            default:
                if (callback != nullptr)
                    callback("A client process failed to connect.", errorCode);
                ::CloseHandle(hPipe);
                delete pStream;
                return nullptr;
        }
    }

    return pStream;
}

IpcStream *IpcStream::DiagnosticsIpc::Connect(ErrorCallback callback)
{
    _ASSERTE(mode == ConnectionMode::CLIENT);
    if (mode != ConnectionMode::CLIENT)
    {
        if (callback != nullptr)
            callback("Cannot call connect on a client connection", 0);
        return nullptr;
    }

    HANDLE hPipe = ::CreateFileA( 
        _pNamedPipeName,                    // pipe name 
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

    return new IpcStream(hPipe, mode);
}

void IpcStream::DiagnosticsIpc::Close(ErrorCallback)
{
}

IpcStream::IpcStream(HANDLE hPipe, DiagnosticsIpc::ConnectionMode mode) :
    _hPipe(hPipe), 
    _mode(mode) 
{
    if (_mode == DiagnosticsIpc::ConnectionMode::SERVER)
        _oOverlap.hEvent = CreateEvent(NULL, true, false, NULL);
}

IpcStream::~IpcStream()
{
    if (_hPipe != INVALID_HANDLE_VALUE)
    {
        Flush();

        if (_mode == DiagnosticsIpc::ConnectionMode::SERVER)
        {
            const BOOL fSuccessDisconnectNamedPipe = ::DisconnectNamedPipe(_hPipe);
            _ASSERTE(fSuccessDisconnectNamedPipe != 0);
        }

        const BOOL fSuccessCloseHandle = ::CloseHandle(_hPipe);
        _ASSERTE(fSuccessCloseHandle != 0);
    }
}

IpcStream *IpcStream::Select(IpcStream **pStreams, uint32_t nStreams, ErrorCallback callback)
{
    // load up an array of handles
    HANDLE *pHandles = new HANDLE[nStreams];
    for (uint32_t i = 0; i < nStreams; i++)
    {
        if (pStreams[i]->_mode == DiagnosticsIpc::ConnectionMode::SERVER)
        {
            pHandles[i] = pStreams[i]->_oOverlap.hEvent;
        }
        else
        {
            pHandles[i] = pStreams[i]->_hPipe;
        }
    }

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
        delete pHandles;
        return nullptr;
    }

    if (pStreams[index]->_mode == IpcStream::DiagnosticsIpc::ConnectionMode::SERVER)
    {
        // set that stream's mode to blocking
        bool result = SetNamedPipeHandleState(
            pHandles[index],                // handle
            PIPE_READMODE_BYTE | PIPE_WAIT, // read mode and wait mode
            NULL,                           // no collecting
            NULL);                          // no collecting
        if (!result)
        {
            if (callback != nullptr)
                callback("Failed to convert handle to wait mode", ::GetLastError());
            delete pHandles;
            return nullptr;
        }
    }

    // cleanup and return that stream
    delete pHandles;
    return pStreams[index];
}

bool IpcStream::Read(void *lpBuffer, const uint32_t nBytesToRead, uint32_t &nBytesRead) const
{
    _ASSERTE(lpBuffer != nullptr);

    DWORD nNumberOfBytesRead = 0;
    LPOVERLAPPED overlap = (_mode == DiagnosticsIpc::ConnectionMode::SERVER) ? 
        const_cast<LPOVERLAPPED>(&_oOverlap) :
        NULL;
    bool fSuccess = ::ReadFile(
        _hPipe,                 // handle to pipe
        lpBuffer,               // buffer to receive data
        nBytesToRead,           // size of buffer
        &nNumberOfBytesRead,    // number of bytes read
        overlap) != 0;          // not overlapped I/O

    if (!fSuccess)
    {
        DWORD dwError = GetLastError();
        if (dwError == ERROR_IO_PENDING)
        {
            fSuccess = GetOverlappedResult(_hPipe,
                                           overlap,
                                           &nNumberOfBytesRead,
                                           true) != 0;
        }
        // TODO: Add error handling.
    }

    nBytesRead = static_cast<uint32_t>(nNumberOfBytesRead);
    return fSuccess;
}

bool IpcStream::Write(const void *lpBuffer, const uint32_t nBytesToWrite, uint32_t &nBytesWritten) const
{
    _ASSERTE(lpBuffer != nullptr);

    DWORD nNumberOfBytesWritten = 0;
    LPOVERLAPPED overlap = (_mode == DiagnosticsIpc::ConnectionMode::SERVER) ? 
        const_cast<LPOVERLAPPED>(&_oOverlap) :
        NULL;
    bool fSuccess = ::WriteFile(
        _hPipe,                 // handle to pipe
        lpBuffer,               // buffer to write from
        nBytesToWrite,          // number of bytes to write
        &nNumberOfBytesWritten, // number of bytes written
        overlap) != 0;          // not overlapped I/O

    if (!fSuccess)
    {
        DWORD dwError = GetLastError();
        if (dwError == ERROR_IO_PENDING)
        {
            fSuccess = GetOverlappedResult(_hPipe,
                                           overlap,
                                           &nNumberOfBytesWritten,
                                           true) != 0;
        }
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

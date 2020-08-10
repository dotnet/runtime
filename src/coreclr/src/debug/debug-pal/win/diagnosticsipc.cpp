// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include <stdio.h>
#include <stdlib.h>
#include "diagnosticsipc.h"

#define _ASSERTE assert

IpcStream::DiagnosticsIpc::DiagnosticsIpc(const char(&namedPipeName)[MaxNamedPipeNameLength], ConnectionMode mode) : 
    mode(mode),
    _isListening(false)
{
    memcpy(_pNamedPipeName, namedPipeName, sizeof(_pNamedPipeName));
    memset(&_oOverlap, 0, sizeof(OVERLAPPED));
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

    if (nCharactersWritten == -1)
    {
        if (callback != nullptr)
            callback("Failed to generate the named pipe name", nCharactersWritten);
        _ASSERTE(nCharactersWritten != -1);
        return nullptr;
    }

    return new IpcStream::DiagnosticsIpc(namedPipeName, mode);
}

bool IpcStream::DiagnosticsIpc::Listen(ErrorCallback callback)
{
    _ASSERTE(mode == ConnectionMode::SERVER);
    if (mode != ConnectionMode::SERVER)
    {
        if (callback != nullptr)
            callback("Cannot call Listen on a client connection", -1);
        return false;
    }

    if (_isListening)
        return true;

    const uint32_t nInBufferSize = 16 * 1024;
    const uint32_t nOutBufferSize = 16 * 1024;
    _hPipe = ::CreateNamedPipeA(
        _pNamedPipeName,                                            // pipe name
        PIPE_ACCESS_DUPLEX |                                        // read/write access
        FILE_FLAG_OVERLAPPED,                                       // async listening
        PIPE_TYPE_BYTE | PIPE_WAIT | PIPE_REJECT_REMOTE_CLIENTS,    // message type pipe, message-read and blocking mode
        PIPE_UNLIMITED_INSTANCES,                                   // max. instances
        nOutBufferSize,                                             // output buffer size
        nInBufferSize,                                              // input buffer size
        0,                                                          // default client time-out
        NULL);                                                      // default security attribute

    if (_hPipe == INVALID_HANDLE_VALUE)
    {
        if (callback != nullptr)
            callback("Failed to create an instance of a named pipe.", ::GetLastError());
        return false;
    }

    HANDLE hOverlapEvent = CreateEvent(NULL, true, false, NULL);
    if (hOverlapEvent == NULL)
    {
        if (callback != nullptr)
            callback("Failed to create overlap event", ::GetLastError());
        ::CloseHandle(_hPipe);
        _hPipe = INVALID_HANDLE_VALUE;
        return false;
    }
    _oOverlap.hEvent = hOverlapEvent;

    BOOL fSuccess = ::ConnectNamedPipe(_hPipe, &_oOverlap) != 0;
    if (!fSuccess)
    {
        const DWORD errorCode = ::GetLastError();
        switch (errorCode)
        {
            case ERROR_IO_PENDING:
                // There was a pending connection that can be waited on (will happen in poll)
            case ERROR_PIPE_CONNECTED:
                // Occurs when a client connects before the function is called.
                // In this case, there is a connection between client and
                // server, even though the function returned zero.
                break;

            default:
                if (callback != nullptr)
                    callback("A client process failed to connect.", errorCode);
                ::CloseHandle(_hPipe);
                _hPipe = INVALID_HANDLE_VALUE;
                ::CloseHandle(_oOverlap.hEvent);
                _oOverlap.hEvent = INVALID_HANDLE_VALUE;
                memset(&_oOverlap, 0, sizeof(OVERLAPPED)); // clear the overlapped objects state
                return false;
        }
    }

    _isListening = true;
    return true;
}

IpcStream *IpcStream::DiagnosticsIpc::Accept(ErrorCallback callback)
{
    _ASSERTE(_isListening);
    _ASSERTE(mode == ConnectionMode::SERVER);

    DWORD dwDummy = 0;
    bool fSuccess = GetOverlappedResult(
        _hPipe,     // handle
        &_oOverlap, // overlapped
        &dwDummy,   // throw-away dword
        true);      // wait till event signals

    if (!fSuccess)
    {
        if (callback != nullptr)
            callback("Failed to GetOverlappedResults for NamedPipe server", ::GetLastError());
        return nullptr;
    }

    // create new IpcStream using handle and reset the Server object so it can listen again
    IpcStream *pStream = new IpcStream(_hPipe, ConnectionMode::SERVER);

    // reset the server
    _hPipe = INVALID_HANDLE_VALUE;
    _isListening = false;
    ::CloseHandle(_oOverlap.hEvent);
    memset(&_oOverlap, 0, sizeof(OVERLAPPED)); // clear the overlapped objects state
    fSuccess = Listen(callback);
    if (!fSuccess)
    {
        delete pStream;
        return nullptr;
    }

    return pStream;
}

IpcStream *IpcStream::DiagnosticsIpc::Connect(ErrorCallback callback)
{
    _ASSERTE(mode == ConnectionMode::CLIENT);
    if (mode != ConnectionMode::CLIENT)
    {
        if (callback != nullptr)
            callback("Cannot call connect on a server connection", 0);
        return nullptr;
    }

    HANDLE hPipe = ::CreateFileA( 
        _pNamedPipeName,                    // pipe name 
        PIPE_ACCESS_DUPLEX,                 // read/write access
        0,                                  // no sharing 
        NULL,                               // default security attributes
        OPEN_EXISTING,                      // opens existing pipe 
        FILE_FLAG_OVERLAPPED,               // Overlapped
        NULL);                              // no template file

    if (hPipe == INVALID_HANDLE_VALUE)
    {
        if (callback != nullptr)
            callback("Failed to connect to named pipe.", ::GetLastError());
        return nullptr;
    }

    return new IpcStream(hPipe, mode);
}

void IpcStream::DiagnosticsIpc::Close(bool isShutdown, ErrorCallback callback)
{
    // don't attempt cleanup on shutdown and let the OS handle it
    if (isShutdown)
    {
        if (callback != nullptr)
            callback("Closing without cleaning underlying handles", 100);
        return;
    }

    if (_hPipe != INVALID_HANDLE_VALUE)
    {
        if (mode == DiagnosticsIpc::ConnectionMode::SERVER)
        {
            const BOOL fSuccessDisconnectNamedPipe = ::DisconnectNamedPipe(_hPipe);
            _ASSERTE(fSuccessDisconnectNamedPipe != 0);
            if (fSuccessDisconnectNamedPipe != 0 && callback != nullptr)
                callback("Failed to disconnect NamedPipe", ::GetLastError());
        }

        const BOOL fSuccessCloseHandle = ::CloseHandle(_hPipe);
        _ASSERTE(fSuccessCloseHandle != 0);
        if (fSuccessCloseHandle != 0 && callback != nullptr)
            callback("Failed to close pipe handle", ::GetLastError());
    }

    if (_oOverlap.hEvent != INVALID_HANDLE_VALUE)
    {
        const BOOL fSuccessCloseEvent = ::CloseHandle(_oOverlap.hEvent);
        _ASSERTE(fSuccessCloseEvent != 0);
        if (fSuccessCloseEvent != 0 && callback != nullptr)
            callback("Failed to close overlap event handle", ::GetLastError());
    }
}

IpcStream::IpcStream(HANDLE hPipe, DiagnosticsIpc::ConnectionMode mode) :
    _hPipe(hPipe), 
    _mode(mode) 
{
    memset(&_oOverlap, 0, sizeof(OVERLAPPED));
    _oOverlap.hEvent = CreateEvent(NULL, true, false, NULL);
}

IpcStream::~IpcStream()
{
    Close();
}

void IpcStream::Close(ErrorCallback callback)
{
    if (_hPipe != INVALID_HANDLE_VALUE)
    {
        Flush();

        if (_mode == DiagnosticsIpc::ConnectionMode::SERVER)
        {
            const BOOL fSuccessDisconnectNamedPipe = ::DisconnectNamedPipe(_hPipe);
            _ASSERTE(fSuccessDisconnectNamedPipe != 0);
            if (fSuccessDisconnectNamedPipe != 0 && callback != nullptr)
                callback("Failed to disconnect NamedPipe", ::GetLastError());
        }

        const BOOL fSuccessCloseHandle = ::CloseHandle(_hPipe);
        _ASSERTE(fSuccessCloseHandle != 0);
        if (fSuccessCloseHandle != 0 && callback != nullptr)
            callback("Failed to close pipe handle", ::GetLastError());
    }

    if (_oOverlap.hEvent != INVALID_HANDLE_VALUE)
    {
        const BOOL fSuccessCloseEvent = ::CloseHandle(_oOverlap.hEvent);
        _ASSERTE(fSuccessCloseEvent != 0);
        if (fSuccessCloseEvent != 0 && callback != nullptr)
            callback("Failed to close overlapped event handle", ::GetLastError());
    }
}

int32_t IpcStream::DiagnosticsIpc::Poll(IpcPollHandle *rgIpcPollHandles, uint32_t nHandles, int32_t timeoutMs, ErrorCallback callback)
{
    // load up an array of handles
    HANDLE *pHandles = new HANDLE[nHandles];
    for (uint32_t i = 0; i < nHandles; i++)
    {
        rgIpcPollHandles[i].revents = 0; // ignore any inputs on revents
        if (rgIpcPollHandles[i].pIpc != nullptr)
        {
            // SERVER
            _ASSERTE(rgIpcPollHandles[i].pIpc->mode == DiagnosticsIpc::ConnectionMode::SERVER);
            pHandles[i] = rgIpcPollHandles[i].pIpc->_oOverlap.hEvent;
        }
        else
        {
            // CLIENT
            bool fSuccess = false;
            DWORD dwDummy = 0;
            if (!rgIpcPollHandles[i].pStream->_isTestReading)
            {
                // check for data by doing an asynchronous 0 byte read.
                // This will signal if the pipe closes (hangup) or the server
                // sends new data
                fSuccess = ::ReadFile(
                    rgIpcPollHandles[i].pStream->_hPipe,      // handle
                    nullptr,                                    // null buffer
                    0,                                          // read 0 bytes
                    &dwDummy,                                   // dummy variable
                    &rgIpcPollHandles[i].pStream->_oOverlap); // overlap object to use
                rgIpcPollHandles[i].pStream->_isTestReading = true;
                if (!fSuccess)
                {
                    DWORD error = ::GetLastError();
                    switch (error)
                    {
                        case ERROR_IO_PENDING:
                            pHandles[i] = rgIpcPollHandles[i].pStream->_oOverlap.hEvent;
                            break;
                        case ERROR_PIPE_NOT_CONNECTED:
                            // hangup
                            rgIpcPollHandles[i].revents = (uint8_t)PollEvents::HANGUP;
                            delete[] pHandles;
                            return -1;
                        default:
                            if (callback != nullptr)
                                callback("0 byte async read on client connection failed", error);
                            delete[] pHandles;
                            return -1;
                    }
                }
                else
                {
                    // there's already data to be read
                    pHandles[i] = rgIpcPollHandles[i].pStream->_oOverlap.hEvent;
                }
            }
            else
            {
                pHandles[i] = rgIpcPollHandles[i].pStream->_oOverlap.hEvent;
            }
        }
    }

    // call wait for multiple obj
    DWORD dwWait = WaitForMultipleObjects(
        nHandles,       // count
        pHandles,       // handles
        false,          // Don't wait-all
        timeoutMs);
    
    if (dwWait == WAIT_TIMEOUT)
    {
        // we timed out
        delete[] pHandles;
        return 0;
    }

    if (dwWait == WAIT_FAILED)
    {
        // we errored
        if (callback != nullptr)
            callback("WaitForMultipleObjects failed", ::GetLastError());
        delete[] pHandles;
        return -1;
    }

    // determine which of the streams signaled
    DWORD index = dwWait - WAIT_OBJECT_0;
    // error check the index
    if (index < 0 || index > (nHandles - 1))
    {
        // check if we abandoned something
        DWORD abandonedIndex = dwWait - WAIT_ABANDONED_0;
        if (abandonedIndex > 0 || abandonedIndex < (nHandles - 1))
        {
            rgIpcPollHandles[abandonedIndex].revents = (uint8_t)IpcStream::DiagnosticsIpc::PollEvents::HANGUP;
            delete[] pHandles;
            return -1;
        }
        else
        {
            if (callback != nullptr)
                callback("WaitForMultipleObjects failed", ::GetLastError());
            delete[] pHandles;
            return -1;
        }
    }

    // Set revents depending on what signaled the stream
    if (rgIpcPollHandles[index].pIpc == nullptr)
    {
        // CLIENT
        // check if the connection got hung up
        DWORD dwDummy = 0;
        bool fSuccess = GetOverlappedResult(rgIpcPollHandles[index].pStream->_hPipe,
                                            &rgIpcPollHandles[index].pStream->_oOverlap,
                                            &dwDummy,
                                            true);
        rgIpcPollHandles[index].pStream->_isTestReading = false;
        if (!fSuccess)
        {
            DWORD error = ::GetLastError();
            if (error == ERROR_PIPE_NOT_CONNECTED)
                rgIpcPollHandles[index].revents = (uint8_t)IpcStream::DiagnosticsIpc::PollEvents::HANGUP;
            else
            {
                if (callback != nullptr)
                    callback("Client connection error", -1);
                rgIpcPollHandles[index].revents = (uint8_t)IpcStream::DiagnosticsIpc::PollEvents::ERR;
                delete[] pHandles;
                return -1;
            }
        }
        else
        {
            rgIpcPollHandles[index].revents = (uint8_t)IpcStream::DiagnosticsIpc::PollEvents::SIGNALED;
        }
    }
    else
    {
        // SERVER
        rgIpcPollHandles[index].revents = (uint8_t)IpcStream::DiagnosticsIpc::PollEvents::SIGNALED;
    }

    delete[] pHandles;
    return 1;
}

bool IpcStream::Read(void *lpBuffer, const uint32_t nBytesToRead, uint32_t &nBytesRead, const int32_t timeoutMs)
{
    _ASSERTE(lpBuffer != nullptr);

    DWORD nNumberOfBytesRead = 0;
    LPOVERLAPPED overlap = &_oOverlap;
    bool fSuccess = ::ReadFile(
        _hPipe,                 // handle to pipe
        lpBuffer,               // buffer to receive data
        nBytesToRead,           // size of buffer
        &nNumberOfBytesRead,    // number of bytes read
        overlap) != 0;          // overlapped I/O

    if (!fSuccess)
    {
        if (timeoutMs == InfiniteTimeout)
        {
            fSuccess = GetOverlappedResult(_hPipe,
                                           overlap,
                                           &nNumberOfBytesRead,
                                           true) != 0;
        }
        else
        {
            DWORD dwError = GetLastError();
            if (dwError == ERROR_IO_PENDING)
            {
                DWORD dwWait = WaitForSingleObject(_oOverlap.hEvent, (DWORD)timeoutMs);
                if (dwWait == WAIT_OBJECT_0)
                {
                    // get the result
                    fSuccess = GetOverlappedResult(_hPipe,
                                                   overlap,
                                                   &nNumberOfBytesRead,
                                                   true) != 0;
                }
                else
                {
                    // cancel IO and ensure the cancel happened
                    if (CancelIo(_hPipe))
                    {
                        // check if the async write beat the cancellation
                        fSuccess = GetOverlappedResult(_hPipe, overlap, &nNumberOfBytesRead, true) != 0;
                    }
                }
            }
        }
        // TODO: Add error handling.
    }

    nBytesRead = static_cast<uint32_t>(nNumberOfBytesRead);
    return fSuccess;
}

bool IpcStream::Write(const void *lpBuffer, const uint32_t nBytesToWrite, uint32_t &nBytesWritten, const int32_t timeoutMs)
{
    _ASSERTE(lpBuffer != nullptr);

    DWORD nNumberOfBytesWritten = 0;
    LPOVERLAPPED overlap = &_oOverlap;
    bool fSuccess = ::WriteFile(
        _hPipe,                 // handle to pipe
        lpBuffer,               // buffer to write from
        nBytesToWrite,          // number of bytes to write
        &nNumberOfBytesWritten, // number of bytes written
        overlap) != 0;          // overlapped I/O

    if (!fSuccess)
    {
        DWORD dwError = GetLastError();
        if (dwError == ERROR_IO_PENDING)
        {
            if (timeoutMs == InfiniteTimeout)
            {
                // if we're waiting infinitely, don't bother with extra kernel call
                fSuccess = GetOverlappedResult(_hPipe,
                                               overlap,
                                               &nNumberOfBytesWritten,
                                                true) != 0;
            }
            else
            {
                DWORD dwWait = WaitForSingleObject(_oOverlap.hEvent, (DWORD)timeoutMs);
                if (dwWait == WAIT_OBJECT_0)
                {
                    // get the result
                    fSuccess = GetOverlappedResult(_hPipe,
                                                   overlap,
                                                   &nNumberOfBytesWritten,
                                                   true) != 0;
                }
                else
                {
                    // cancel IO and ensure the cancel happened
                    if (CancelIo(_hPipe))
                    {
                        // check if the async write beat the cancellation
                        fSuccess = GetOverlappedResult(_hPipe, overlap, &nNumberOfBytesWritten, true) != 0;
                    }
                }
            }
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <windows.h>
#include <stdio.h>
#include <wchar.h>
#include <assert.h>
#include "twowaypipe.h"

#define _ASSERTE assert

// This file contains implementation of a simple IPC mechanism - bidirectional named pipe.
// It is implemented on top of two one-directional names pipes (fifos on UNIX)


// Creates a server side of the pipe.
// Id is used to create pipes names and uniquely identify the pipe on the machine.
// true - success, false - failure (use GetLastError() for more details)
bool TwoWayPipe::CreateServer(const ProcessDescriptor& pd)
{
    _ASSERTE(m_state == NotInitialized);
    if (m_state != NotInitialized)
        return false;

    m_inboundPipe = CreateOneWayPipe(pd.m_Pid, true);
    if (m_inboundPipe == INVALID_HANDLE_VALUE)
    {
        return false;
    }

    m_outboundPipe = CreateOneWayPipe(pd.m_Pid, false);
    if (m_outboundPipe == INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_inboundPipe);
        m_inboundPipe = INVALID_HANDLE_VALUE;
        return false;
    }

    m_state = Created;
    return true;
}


// Connects to a previously opened server side of the pipe.
// Id is used to locate the pipe on the machine.
// true - success, false - failure (use GetLastError() for more details)
bool TwoWayPipe::Connect(const ProcessDescriptor& pd)
{
    _ASSERTE(m_state == NotInitialized);
    if (m_state != NotInitialized)
        return false;

    m_inboundPipe = OpenOneWayPipe(pd.m_Pid, true);
    if (m_inboundPipe == INVALID_HANDLE_VALUE)
    {
        return false;
    }

    m_outboundPipe = OpenOneWayPipe(pd.m_Pid, false);
    if (m_outboundPipe == INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_inboundPipe);
        m_inboundPipe = INVALID_HANDLE_VALUE;
        return false;
    }

    m_state = ClientConnected;
    return true;

}

// Waits for incoming client connections, assumes GetState() == Created
// true - success, false - failure (use GetLastError() for more details)
bool TwoWayPipe::WaitForConnection()
{
    _ASSERTE(m_state == Created);
    if (m_state != Created)
        return false;

    if (!ConnectNamedPipe(m_inboundPipe, NULL))
    {
        auto error = GetLastError();
        if (error != ERROR_PIPE_CONNECTED)
            return false;
    }

    if (!ConnectNamedPipe(m_outboundPipe, NULL))
    {
        auto error = GetLastError();
        if (error != ERROR_PIPE_CONNECTED)
            return false;
    }

    m_state = ServerConnected;
    return true;
}

// Reads data from pipe. Returns number of bytes read or a negative number in case of an error.
// use GetLastError() for more details
int TwoWayPipe::Read(void *buffer, DWORD bufferSize)
{
    _ASSERTE(m_state == ServerConnected || m_state == ClientConnected);
    DWORD bytesRead;
    BOOL ok = ReadFile(m_inboundPipe, buffer, bufferSize, &bytesRead, NULL);

    if (ok)
    {
        return (int)bytesRead;
    }
    else
    {
        return -1;
    }
}

// Writes data to pipe. Returns number of bytes written or a negative number in case of an error.
// use GetLastError() for more details
int TwoWayPipe::Write(const void *data, DWORD dataSize)
{
    _ASSERTE(m_state == ServerConnected || m_state == ClientConnected);
    DWORD bytesWritten;
    BOOL ok = WriteFile(m_outboundPipe, data, dataSize, &bytesWritten, NULL);

    if (ok)
    {
        FlushFileBuffers(m_outboundPipe);
        return (int)bytesWritten;
    }
    else
    {
        return -1;
    }
}

// Disconnect server or client side of the pipe.
// true - success, false - failure (use GetLastError() for more details)
bool TwoWayPipe::Disconnect()
{
    if (m_state == ServerConnected)
    {
        DisconnectNamedPipe(m_outboundPipe);
        DisconnectNamedPipe(m_inboundPipe);
        CloseHandle(m_outboundPipe);
        m_outboundPipe = INVALID_HANDLE_VALUE;
        CloseHandle(m_inboundPipe);
        m_inboundPipe = INVALID_HANDLE_VALUE;
        m_state = NotInitialized;
        return true;
    }
    else if (m_state == ClientConnected)
    {
        CloseHandle(m_outboundPipe);
        m_outboundPipe = INVALID_HANDLE_VALUE;
        CloseHandle(m_inboundPipe);
        m_inboundPipe = INVALID_HANDLE_VALUE;
        m_state = NotInitialized;
        return true;
    }
    else
    {
        // nothign to do
        return true;
    }
}

#define PIPE_NAME_FORMAT_STR L"\\\\.\\pipe\\clr-debug-pipe-%d-%s"

// Connects to a one sided pipe previously created by CreateOneWayPipe.
// In order to successfully connect id and inbound flag should be the same.
HANDLE TwoWayPipe::OpenOneWayPipe(DWORD id, bool inbound)
{
    WCHAR fullName[MAX_PATH];
    // "in" and "out" are deliberately switched because we're opening a client side connection
    int chars = swprintf_s(fullName, MAX_PATH, PIPE_NAME_FORMAT_STR, id, inbound ? L"out" : L"in");
    _ASSERTE(chars > 0);

    HANDLE handle = CreateFileW(
        fullName,
        inbound ? GENERIC_READ : GENERIC_WRITE,
        0,              // no sharing
        NULL,           // default security attributes
        OPEN_EXISTING,  // opens existing pipe
        0,              // default attributes
        NULL);          // no template file

    return handle;
}


// Creates a one way pipe, id and inboud flag are used for naming.
// Created pipe is supposed to be connected to by OpenOneWayPipe.
HANDLE TwoWayPipe::CreateOneWayPipe(DWORD id, bool inbound)
{
    WCHAR fullName[MAX_PATH];
    int chars = swprintf_s(fullName, MAX_PATH, PIPE_NAME_FORMAT_STR, id, inbound ? L"in" : L"out");
    _ASSERTE(chars > 0);

    HANDLE handle = CreateNamedPipeW(fullName,
        (inbound ? PIPE_ACCESS_INBOUND : PIPE_ACCESS_OUTBOUND) | FILE_FLAG_FIRST_PIPE_INSTANCE,
        PIPE_TYPE_BYTE | PIPE_WAIT | PIPE_REJECT_REMOTE_CLIENTS,
        1,    // max number of instances
        4000, //in buffer size
        4000, //out buffer size
        0,    // default timeout
        NULL); // default security

    return handle;
}


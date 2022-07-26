// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <pal.h>
#include <unistd.h>
#include <fcntl.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <limits.h>
#include <pal_assert.h>
#include "twowaypipe.h"

// Creates a server side of the pipe.
// Id is used to create pipes names and uniquely identify the pipe on the machine.
// true - success, false - failure (use GetLastError() for more details)
bool TwoWayPipe::CreateServer(const ProcessDescriptor& pd)
{
    _ASSERTE(m_state == NotInitialized);
    if (m_state != NotInitialized)
        return false;

    PAL_GetTransportPipeName(m_inPipeName, pd.m_Pid, pd.m_ApplicationGroupId, "in");
    PAL_GetTransportPipeName(m_outPipeName, pd.m_Pid, pd.m_ApplicationGroupId, "out");

    unlink(m_inPipeName);

    if (mkfifo(m_inPipeName, S_IRWXU) == -1)
    {
        return false;
    }


    unlink(m_outPipeName);

    if (mkfifo(m_outPipeName, S_IRWXU) == -1)
    {
        unlink(m_inPipeName);
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

    //"in" and "out" are switched deliberately, because we're on the client
    PAL_GetTransportPipeName(m_inPipeName, pd.m_Pid, pd.m_ApplicationGroupId, "out");
    PAL_GetTransportPipeName(m_outPipeName, pd.m_Pid, pd.m_ApplicationGroupId, "in");

    // Pipe opening order is reversed compared to WaitForConnection()
    // in order to avoid deadlock.
    m_outboundPipe = open(m_outPipeName, O_WRONLY);
    if (m_outboundPipe == INVALID_PIPE)
    {
        return false;
    }

    m_inboundPipe = open(m_inPipeName, O_RDONLY);
    if (m_inboundPipe == INVALID_PIPE)
    {
        close(m_outboundPipe);
        m_outboundPipe = INVALID_PIPE;
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

    m_inboundPipe = open(m_inPipeName, O_RDONLY);
    if (m_inboundPipe == INVALID_PIPE)
    {
        return false;
    }

    m_outboundPipe = open(m_outPipeName, O_WRONLY);
    if (m_outboundPipe == INVALID_PIPE)
    {
        close(m_inboundPipe);
        m_inboundPipe = INVALID_PIPE;
        return false;
    }

    m_state = ServerConnected;
    return true;
}

// Reads data from pipe. Returns number of bytes read or a negative number in case of an error.
// use GetLastError() for more details
// UNIXTODO - mjm 9/6/15 - does not set last error on failure
int TwoWayPipe::Read(void *buffer, DWORD bufferSize)
{
    _ASSERTE(m_state == ServerConnected || m_state == ClientConnected);

    int totalBytesRead = 0;
    int bytesRead;
    int cb = bufferSize;

    while ((bytesRead = (int)read(m_inboundPipe, buffer, cb)) > 0)
    {
        totalBytesRead += bytesRead;
        _ASSERTE(totalBytesRead <= (int)bufferSize);
        if (totalBytesRead >= (int)bufferSize)
        {
            break;
        }

        buffer = (char*)buffer + bytesRead;
        cb -= bytesRead;
    }

    return bytesRead == -1 ? -1 : totalBytesRead;
}

// Writes data to pipe. Returns number of bytes written or a negative number in case of an error.
// use GetLastError() for more details
// UNIXTODO - mjm 9/6/15 - does not set last error on failure
int TwoWayPipe::Write(const void *data, DWORD dataSize)
{
    _ASSERTE(m_state == ServerConnected || m_state == ClientConnected);

    int totalBytesWritten = 0;
    int bytesWritten;
    int cb = dataSize;

    while ((bytesWritten = (int)write(m_outboundPipe, data, cb)) > 0)
    {
        totalBytesWritten += bytesWritten;
        _ASSERTE(totalBytesWritten <= (int)dataSize);
        if (totalBytesWritten >= (int)dataSize)
        {
            break;
        }

        data = (char*)data + bytesWritten;
        cb -= bytesWritten;
    }

    return bytesWritten == -1 ? -1 : totalBytesWritten;
}

// Disconnect server or client side of the pipe.
// true - success, false - failure (use GetLastError() for more details)
bool TwoWayPipe::Disconnect()
{
    // IMPORTANT NOTE: This function must not call any signal unsafe functions
    // since it is called from signal handlers.
    // That includes ASSERT and TRACE macros.

    if (m_state == ServerConnected || m_state == Created)
    {
        unlink(m_inPipeName);
        unlink(m_outPipeName);
    }

    m_state = NotInitialized;
    return true;
}

// Used by debugger side (RS) to cleanup the target (LS) named pipes
// and semaphores when the debugger detects the debuggee process  exited.
void TwoWayPipe::CleanupTargetProcess()
{
    unlink(m_inPipeName);
    unlink(m_outPipeName);
}

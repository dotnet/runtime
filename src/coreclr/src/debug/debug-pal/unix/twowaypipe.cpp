// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <pal.h>

#include <unistd.h>
#include <fcntl.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <limits.h>
#include <pal_assert.h>

#include "twowaypipe.h"

static const char* PipeNameFormat = "/tmp/clr-debug-pipe-%d-%llu-%s";

static void GetPipeName(char *name, DWORD id, const char *suffix)
{
    UINT64 disambiguationKey;
    BOOL ret = GetProcessIdDisambiguationKey(id, &disambiguationKey);

    // If GetProcessIdDisambiguationKey failed for some reason, it should set the value 
    // to 0. We expect that anyone else making the pipe name will also fail and thus will
    // also try to use 0 as the value.
    _ASSERTE(ret == TRUE || disambiguationKey == 0);

    int chars = _snprintf(name, PATH_MAX, PipeNameFormat, id, disambiguationKey, suffix);
    _ASSERTE(chars > 0 && chars < PATH_MAX);
}

// Creates a server side of the pipe. 
// Id is used to create pipes names and uniquely identify the pipe on the machine. 
// true - success, false - failure (use GetLastError() for more details)
bool TwoWayPipe::CreateServer(DWORD id)
{
    _ASSERTE(m_state == NotInitialized);
    if (m_state != NotInitialized)
        return false;

    m_id = id;
    char inPipeName[PATH_MAX];
    char outPipeName[PATH_MAX];
    GetPipeName(inPipeName, id, "in");
    GetPipeName(outPipeName, id, "out");

    if (mkfifo(inPipeName, S_IRWXU) == -1)
    {
        return false;
    }

    if (mkfifo(outPipeName, S_IRWXU) == -1)
    {
        unlink(inPipeName);
        return false;
    }

    m_state = Created;
    return true;
}

// Connects to a previously opened server side of the pipe.
// Id is used to locate the pipe on the machine. 
// true - success, false - failure (use GetLastError() for more details)
bool TwoWayPipe::Connect(DWORD id)
{
    _ASSERTE(m_state == NotInitialized);
    if (m_state != NotInitialized)
        return false;

    m_id = id;
    char inPipeName[PATH_MAX];
    char outPipeName[PATH_MAX];
    //"in" and "out" are switched deliberately, because we're on the client
    GetPipeName(inPipeName, id, "out");
    GetPipeName(outPipeName, id, "in");

    // Pipe opening order is reversed compared to WaitForConnection()
    // in order to avaid deadlock.
    m_outboundPipe = open(outPipeName, O_WRONLY);
    if (m_outboundPipe == INVALID_PIPE)
    {
        return false;
    }

    m_inboundPipe = open(inPipeName, O_RDONLY);
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

    char inPipeName[PATH_MAX];
    char outPipeName[PATH_MAX];
    GetPipeName(inPipeName, m_id, "in");
    GetPipeName(outPipeName, m_id, "out");

    m_inboundPipe = open(inPipeName, O_RDONLY);
    if (m_inboundPipe == INVALID_PIPE)
    {
        return false;
    }

    m_outboundPipe = open(outPipeName, O_WRONLY);
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
        _ASSERTE(totalBytesRead <= bufferSize);
        if (totalBytesRead >= bufferSize)
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
        _ASSERTE(totalBytesWritten <= dataSize);
        if (totalBytesWritten >= dataSize)
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
    if (m_outboundPipe != INVALID_PIPE && m_outboundPipe != 0)
    {
        close(m_outboundPipe);
        m_outboundPipe = INVALID_PIPE;
    }

    if (m_inboundPipe != INVALID_PIPE && m_inboundPipe != 0)
    {
        close(m_inboundPipe);
        m_inboundPipe = INVALID_PIPE;
    }

    if (m_state == ServerConnected || m_state == Created)
    {
        char inPipeName[PATH_MAX];
        GetPipeName(inPipeName, m_id, "in");
        unlink(inPipeName);

        char outPipeName[PATH_MAX];
        GetPipeName(outPipeName, m_id, "out");
        unlink(outPipeName);
    }

    m_state = NotInitialized;
    return true;
}

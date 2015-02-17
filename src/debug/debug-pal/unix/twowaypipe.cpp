//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include <unistd.h>
#include <fcntl.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <stdio.h>
#include <limits.h>

#include "windefs.h"
#include "twowaypipe.h"

#define PIPE_NAME_FORMAT_STR "/tmp/clr-debug-pipe-%d-%s"

static void GetPipeName(char *name, DWORD id, const char *suffix)
{
    int chars = snprintf(name, PATH_MAX, PIPE_NAME_FORMAT_STR, id, suffix);
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

    //TODO: REVIEW if S_IRWXU | S_IRWXG is the right access level in prof use
    if (mkfifo(inPipeName, S_IRWXU | S_IRWXG) == -1)
    {
        return false;
    }

    if (mkfifo(outPipeName, S_IRWXU | S_IRWXG) == -1)
    {
        remove(inPipeName);
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
int TwoWayPipe::Read(void *buffer, DWORD bufferSize)
{
    _ASSERTE(m_state == ServerConnected || m_state == ClientConnected);
    return (int)read(m_inboundPipe, buffer, bufferSize);
}

// Writes data to pipe. Returns number of bytes written or a negative number in case of an error.
// use GetLastError() for more details
int TwoWayPipe::Write(const void *data, DWORD dataSize)
{
    _ASSERTE(m_state == ServerConnected || m_state == ClientConnected);
    return (int)write(m_outboundPipe, data, dataSize);
}

// Disconnect server or client side of the pipe.
// true - success, false - failure (use GetLastError() for more details)
bool TwoWayPipe::Disconnect()
{

    if (m_outboundPipe != INVALID_PIPE)
    {
        close(m_outboundPipe);
        m_outboundPipe = INVALID_PIPE;
    }

    if (m_inboundPipe != INVALID_PIPE)
    {
        close(m_inboundPipe);
        m_inboundPipe = INVALID_PIPE;
    }    

    if (m_state == ServerConnected || m_state == Created)
    {

        char inPipeName[PATH_MAX];
        GetPipeName(inPipeName, m_id, "in");
        remove(inPipeName);

        char outPipeName[PATH_MAX];
        GetPipeName(outPipeName, m_id, "out");
        remove(outPipeName);
    }

    m_state = NotInitialized;
    return true;
}


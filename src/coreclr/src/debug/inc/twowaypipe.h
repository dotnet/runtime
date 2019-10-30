// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef TwoWayPipe_H
#define TwoWayPipe_H

#include "processdescriptor.h"

#ifdef FEATURE_PAL
#define INVALID_PIPE -1
#else
#define INVALID_PIPE INVALID_HANDLE_VALUE
#endif

// This file contains definition of a simple IPC mechanism - bidirectional named pipe.
// It is implemented on top of two one-directional names pipes (fifos on UNIX)

// One Windows it is possible to ask OS to create a bidirectional pipe, but it is not the case on UNIX.
// In order to unify implementation we use two pipes on all systems.

// This all methods of this class are *NOT* thread safe: it is assumed the caller provides synchronization at a higher level.
class TwoWayPipe
{
public:
    enum State
    {
        NotInitialized,   // Object didn't create or connect to any pipes.
        Created,          // Server side of the pipe has been created, but didn't bind it to a client.
        ServerConnected,  // Server side of the pipe is connected to a client 
        ClientConnected,  // Client side of the pipe is connected to a server.
    };

    TwoWayPipe()
        :m_state(NotInitialized),
        m_inboundPipe(INVALID_PIPE),
        m_outboundPipe(INVALID_PIPE)
    {}


    ~TwoWayPipe()
    {
        Disconnect();
    }

    // Creates a server side of the pipe. 
    // pd is used to create pipes names and uniquely identify the pipe on the machine. 
    // true - success, false - failure (use GetLastError() for more details)
    bool CreateServer(const ProcessDescriptor& pd);

    // Connects to a previously opened server side of the pipe.
    // pd is used to locate the pipe on the machine. 
    // true - success, false - failure (use GetLastError() for more details)
    bool Connect(const ProcessDescriptor& pd);

    // Waits for incoming client connections, assumes GetState() == Created
    // true - success, false - failure (use GetLastError() for more details)
    bool WaitForConnection();

    // Reads data from pipe. Returns number of bytes read or a negative number in case of an error.
    // use GetLastError() for more details
    int Read(void *buffer, DWORD bufferSize);

    // Writes data to pipe. Returns number of bytes written or a negative number in case of an error.
    // use GetLastError() for more details
    int Write(const void *data, DWORD dataSize);

    // Disconnects server or client side of the pipe.
    // true - success, false - failure (use GetLastError() for more details)
    bool Disconnect();

    State GetState()
    {
        return m_state;
    }

    // Used by debugger side (RS) to cleanup the target (LS) named pipes 
    // and semaphores when the debugger detects the debuggee process  exited.
    void CleanupTargetProcess();

private:

    State m_state;

#ifdef FEATURE_PAL

    int m_inboundPipe, m_outboundPipe;      // two one sided pipes used for communication
    char m_inPipeName[MAX_DEBUGGER_TRANSPORT_PIPE_NAME_LENGTH];   // filename of the inbound pipe
    char m_outPipeName[MAX_DEBUGGER_TRANSPORT_PIPE_NAME_LENGTH];  // filename of the outbound pipe

#else
    // Connects to a one sided pipe previously created by CreateOneWayPipe.
    // In order to successfully connect id and inbound flag should be the same.
    HANDLE OpenOneWayPipe(DWORD id, bool inbound);
   
    // Creates a one way pipe, id and inboud flag are used for naming.
    // Created pipe is supposed to be connected to by OpenOneWayPipe.
    HANDLE CreateOneWayPipe(DWORD id, bool inbound);

    HANDLE m_inboundPipe, m_outboundPipe; //two one sided pipes used for communication
#endif //FEATURE_PAL
};

#endif //TwoWayPipe_H

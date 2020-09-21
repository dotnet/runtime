// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <windows.h>
#include "twowaypipe.h"

// This file contains a dummy implementation of the simple IPC mechanism - bidirectional named pipe.
// It is used for the cross OS DBI where IPC is not supported.


// Creates a server side of the pipe.
// Id is used to create pipes names and uniquely identify the pipe on the machine.
// true - success, false - failure (use GetLastError() for more details)
bool TwoWayPipe::CreateServer(const ProcessDescriptor& pd)
{
    return false;
}


// Connects to a previously opened server side of the pipe.
// Id is used to locate the pipe on the machine.
// true - success, false - failure (use GetLastError() for more details)
bool TwoWayPipe::Connect(const ProcessDescriptor& pd)
{
    return false;
}

// Waits for incoming client connections, assumes GetState() == Created
// true - success, false - failure (use GetLastError() for more details)
bool TwoWayPipe::WaitForConnection()
{
    return false;
}

// Reads data from pipe. Returns number of bytes read or a negative number in case of an error.
// use GetLastError() for more details
int TwoWayPipe::Read(void *buffer, DWORD bufferSize)
{
    return -1;
}

// Writes data to pipe. Returns number of bytes written or a negative number in case of an error.
// use GetLastError() for more details
int TwoWayPipe::Write(const void *data, DWORD dataSize)
{
    return -1;
}

// Disconnect server or client side of the pipe.
// true - success, false - failure (use GetLastError() for more details)
bool TwoWayPipe::Disconnect()
{
    return false;
}

// Connects to a one sided pipe previously created by CreateOneWayPipe.
// In order to successfully connect id and inbound flag should be the same.
HANDLE TwoWayPipe::OpenOneWayPipe(DWORD id, bool inbound)
{
    return NULL;
}


// Creates a one way pipe, id and inboud flag are used for naming.
// Created pipe is supposed to be connected to by OpenOneWayPipe.
HANDLE TwoWayPipe::CreateOneWayPipe(DWORD id, bool inbound)
{
    return NULL;
}

// Used by debugger side (RS) to cleanup the target (LS) named pipes
// and semaphores when the debugger detects the debuggee process  exited.
void TwoWayPipe::CleanupTargetProcess()
{
}

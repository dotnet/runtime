// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Pipes
{
    [Serializable]
    internal enum PipeState
    {
        // Waiting to connect is the state before a live connection has been established. For named pipes, the 
        // transition from Waiting to Connect to Connected occurs after an explicit request to connect. For 
        // anonymous pipes this occurs as soon as both pipe handles are created (as soon as the anonymous pipe 
        // server ctor has completed).
        WaitingToConnect = 0,

        // For named pipes: the state we're in after calling Connect. For anonymous pipes: occurs as soon as 
        // both handles are created.
        Connected = 1,

        // It’s detected that the other side has broken the connection. Note that this effect isn’t immediate; we 
        // only detect this on the subsequent Win32 call, as indicated by the following error codes: 
        // ERROR_BROKEN_PIPE, ERROR_PIPE_NOT_CONNECTED.
        // A side can cause the connection to break in the following ways:
        //    - Named server calls Disconnect
        //    - One side calls Close/Dispose
        //    - One side closes the handle
        Broken = 2,

        // Valid only for named servers. The server transitions to this state immediately after Disconnect is called. 
        Disconnected = 3,

        // Close/Disposed are the same state. The Close method calls Dispose; both of these close the pipe handle
        // and perform other cleanup. The pipe object is no longer usable after this has been called.
        Closed = 4,
    }
}

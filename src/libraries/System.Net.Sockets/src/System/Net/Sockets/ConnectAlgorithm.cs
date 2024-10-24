// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Sockets
{
    // Defines constants used by the Socket.Shutdown method.
    public enum ConnectAlgorithm
    {
        // defaul mechanism e.g. sequential processing
        Default = 0,

        // use a Happy Eyeballs-like algorithm to connect.
        Parallel = 1,
    }
}

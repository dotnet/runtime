// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;

namespace System.Net.Sockets
{
    public enum SocketError : int
    {
        Success = 0,
        SocketError = (-1),
        Interrupted = (10000 + 4),
        AccessDenied = (10000 + 13),
        Fault = (10000 + 14),
        InvalidArgument = (10000 + 22),
        TooManyOpenSockets = (10000 + 24),

        // Windows Sockets definitions of regular Berkeley error constants
        WouldBlock = (10000 + 35),
        InProgress = (10000 + 36),
        AlreadyInProgress = (10000 + 37),
        NotSocket = (10000 + 38),
        DestinationAddressRequired = (10000 + 39),
        MessageSize = (10000 + 40),
        ProtocolType = (10000 + 41),
        ProtocolOption = (10000 + 42),
        ProtocolNotSupported = (10000 + 43),
        SocketNotSupported = (10000 + 44),
        OperationNotSupported = (10000 + 45),
        ProtocolFamilyNotSupported = (10000 + 46),
        AddressFamilyNotSupported = (10000 + 47),
        AddressAlreadyInUse = (10000 + 48),
        AddressNotAvailable = (10000 + 49),
        NetworkDown = (10000 + 50),
        NetworkUnreachable = (10000 + 51),
        NetworkReset = (10000 + 52),
        ConnectionAborted = (10000 + 53),
        ConnectionReset = (10000 + 54),
        NoBufferSpaceAvailable = (10000 + 55),
        IsConnected = (10000 + 56),
        NotConnected = (10000 + 57),
        Shutdown = (10000 + 58),
        TimedOut = (10000 + 60),
        ConnectionRefused = (10000 + 61),
        HostDown = (10000 + 64),
        HostUnreachable = (10000 + 65),
        ProcessLimit = (10000 + 67),

        // Extended Windows Sockets error constant definitions
        SystemNotReady = (10000 + 91),
        VersionNotSupported = (10000 + 92),
        NotInitialized = (10000 + 93),
        Disconnecting = (10000 + 101),
        TypeNotFound = (10000 + 109),
        HostNotFound = (10000 + 1001),
        TryAgain = (10000 + 1002),
        NoRecovery = (10000 + 1003),
        NoData = (10000 + 1004),

        // OS dependent errors
        IOPending = (int)Interop.Winsock.ERROR_IO_PENDING,
        OperationAborted = (int)Interop.Winsock.ERROR_OPERATION_ABORTED,
    }
}

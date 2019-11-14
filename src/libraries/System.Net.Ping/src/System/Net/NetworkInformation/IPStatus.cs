// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    // TODO #3562 - properly name constants below from Win32 SDK (i.e. 11000 + k).

    public enum IPStatus
    {
        Success = 0,
        //BufferTooSmall = 11000 + 1,

        DestinationNetworkUnreachable = 11000 + 2,
        DestinationHostUnreachable = 11000 + 3,
        DestinationProtocolUnreachable = 11000 + 4,
        DestinationPortUnreachable = 11000 + 5,
        DestinationProhibited = 11000 + 4,

        NoResources = 11000 + 6,
        BadOption = 11000 + 7,
        HardwareError = 11000 + 8,
        PacketTooBig = 11000 + 9,
        TimedOut = 11000 + 10,
        //  BadRequest = 11000 + 11,
        BadRoute = 11000 + 12,

        TtlExpired = 11000 + 13,
        TtlReassemblyTimeExceeded = 11000 + 14,

        ParameterProblem = 11000 + 15,
        SourceQuench = 11000 + 16,
        //OptionTooBig = 11000 + 17,
        BadDestination = 11000 + 18,

        DestinationUnreachable = 11000 + 40,
        TimeExceeded = 11000 + 41,
        BadHeader = 11000 + 42,
        UnrecognizedNextHeader = 11000 + 43,
        IcmpError = 11000 + 44,
        DestinationScopeMismatch = 11000 + 45,
        Unknown = -1,
    }
}

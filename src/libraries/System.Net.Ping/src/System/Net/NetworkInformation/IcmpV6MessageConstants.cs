// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Represents the "type" field in ICMPv6 headers.
    /// </summary>
    internal enum IcmpV6MessageType : byte
    {
        DestinationUnreachable = 1,
        PacketTooBig = 2,
        TimeExceeded = 3,
        ParameterProblem = 4,
        EchoRequest = 128,
        EchoReply = 129
    }

    /// <summary>
    /// Represents the "code" field in ICMPv6 headers whose type is DestinationUnreachable.
    /// </summary>
    internal enum IcmpV6DestinationUnreachableCode : byte
    {
        NoRouteToDestination = 0,
        CommunicationAdministrativelyProhibited = 1,
        BeyondScopeOfSourceAddress = 2,
        AddressUnreachable = 3,
        PortUnreachable = 4,
        SourceAddressFailedPolicy = 5,
        RejectRouteToDestination = 6,
        SourceRoutingHeaderError = 7
    }

    /// <summary>
    /// Represents the "code" field in ICMPv6 headers whose type is TimeExceeded.
    /// </summary>
    internal enum IcmpV6TimeExceededCode : byte
    {
        HopLimitExceeded = 0,
        FragmentReassemblyTimeExceeded = 1
    }

    /// <summary>
    /// Represents the "code" field in ICMPv6 headers whose type is ParameterProblem.
    /// </summary>
    internal enum IcmpV6ParameterProblemCode : byte
    {
        ErroneousHeaderField = 0,
        UnrecognizedNextHeader = 1,
        UnrecognizedIpv6Option = 2
    }

    internal static class IcmpV6MessageConstants
    {
        public static IPStatus MapV6TypeToIPStatus(byte type, byte code)
        {
            return (IcmpV6MessageType)type switch
            {
                IcmpV6MessageType.EchoReply => IPStatus.Success,
                IcmpV6MessageType.DestinationUnreachable => (IcmpV6DestinationUnreachableCode)code switch
                {
                    IcmpV6DestinationUnreachableCode.NoRouteToDestination => IPStatus.BadRoute,
                    IcmpV6DestinationUnreachableCode.SourceRoutingHeaderError => IPStatus.BadHeader,
                    _ => IPStatus.DestinationUnreachable,
                },
                IcmpV6MessageType.PacketTooBig => IPStatus.PacketTooBig,
                IcmpV6MessageType.TimeExceeded => (IcmpV6TimeExceededCode)code switch
                {
                    IcmpV6TimeExceededCode.FragmentReassemblyTimeExceeded => IPStatus.TtlReassemblyTimeExceeded,
                    _ => IPStatus.TtlExpired,
                },
                IcmpV6MessageType.ParameterProblem => (IcmpV6ParameterProblemCode)code switch
                {
                    IcmpV6ParameterProblemCode.ErroneousHeaderField => IPStatus.BadHeader,
                    IcmpV6ParameterProblemCode.UnrecognizedNextHeader => IPStatus.UnrecognizedNextHeader,
                    IcmpV6ParameterProblemCode.UnrecognizedIpv6Option => IPStatus.BadOption,
                    _ => IPStatus.ParameterProblem,
                },
                _ => IPStatus.Unknown,
            };
        }
    }
}

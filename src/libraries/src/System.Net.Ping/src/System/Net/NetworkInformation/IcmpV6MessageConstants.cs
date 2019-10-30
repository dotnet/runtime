// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        RejectRouteToDesitnation = 6,
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
            switch ((IcmpV6MessageType)type)
            {
                case IcmpV6MessageType.EchoReply:
                    return IPStatus.Success;

                case IcmpV6MessageType.DestinationUnreachable:
                    switch ((IcmpV6DestinationUnreachableCode)code)
                    {
                        case IcmpV6DestinationUnreachableCode.NoRouteToDestination:
                            return IPStatus.BadRoute;
                        case IcmpV6DestinationUnreachableCode.SourceRoutingHeaderError:
                            return IPStatus.BadHeader;
                        default:
                            return IPStatus.DestinationUnreachable;
                    }

                case IcmpV6MessageType.PacketTooBig:
                    return IPStatus.PacketTooBig;

                case IcmpV6MessageType.TimeExceeded:
                    switch ((IcmpV6TimeExceededCode)code)
                    {
                        case IcmpV6TimeExceededCode.HopLimitExceeded:
                            return IPStatus.TimeExceeded;
                        case IcmpV6TimeExceededCode.FragmentReassemblyTimeExceeded:
                            return IPStatus.TtlReassemblyTimeExceeded;
                        default:
                            return IPStatus.TimeExceeded;
                    }

                case IcmpV6MessageType.ParameterProblem:
                    switch ((IcmpV6ParameterProblemCode)code)
                    {
                        case IcmpV6ParameterProblemCode.ErroneousHeaderField:
                            return IPStatus.BadHeader;
                        case IcmpV6ParameterProblemCode.UnrecognizedNextHeader:
                            return IPStatus.UnrecognizedNextHeader;
                        case IcmpV6ParameterProblemCode.UnrecognizedIpv6Option:
                            return IPStatus.BadOption;
                        default:
                            return IPStatus.ParameterProblem;
                    }

                default:
                    return IPStatus.Unknown;
            }
        }
    }
}

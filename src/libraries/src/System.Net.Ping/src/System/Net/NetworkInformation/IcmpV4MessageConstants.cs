// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Represents the "type" field in ICMPv4 headers.
    /// </summary>
    internal enum IcmpV4MessageType : byte
    {
        EchoReply = 0,
        DestinationUnreachable = 3,
        SourceQuench = 4,
        RedirectMessage = 5,
        EchoRequest = 8,
        RouterAdvertisement = 9,
        RouterSolicitation = 10,
        TimeExceeded = 11,
        ParameterProblemBadIPHeader = 12,
        Timestamp = 13,
        TimestampReply = 14,
        InformationRequest = 15,
        InformationReply = 16,
        AddressMaskRequest = 17,
        AddressMaskReply = 18,
        Traceroute = 30
    }

    /// <summary>
    /// Represents the "code" field in ICMPv4 headers whose type is DestinationUnreachable.
    /// </summary>
    internal enum IcmpV4DestinationUnreachableCode : byte
    {
        DestinationNetworkUnreachable = 0,
        DestinationHostUnreachable = 1,
        DestinationProtocolUnreachable = 2,
        DestinationPortUnreachable = 3,
        FragmentationRequiredAndDFFlagSet = 4,
        SourceRouteFailed = 5,
        DestinationNetworkUnknown = 6,
        DestinationHostUnknown = 7,
        SourceHostIsolated = 8,
        NetworkAdministrativelyProhibited = 9,
        HostAdministrativelyProhibited = 10,
        NetworkUnreachableForTos = 11,
        HostUnreachableForTos = 12,
        CommunicationAdministrativelyProhibited = 13,
        HostPrecedenceViolation = 14,
        PrecedenceCutoffInEffect = 15,
    }

    internal static class IcmpV4MessageConstants
    {
        public static IPStatus MapV4TypeToIPStatus(int type, int code)
        {
            switch ((IcmpV4MessageType)type)
            {
                case IcmpV4MessageType.EchoReply:
                    return IPStatus.Success;

                case IcmpV4MessageType.DestinationUnreachable:
                    switch ((IcmpV4DestinationUnreachableCode)code)
                    {
                        case IcmpV4DestinationUnreachableCode.DestinationNetworkUnreachable:
                            return IPStatus.DestinationNetworkUnreachable;
                        case IcmpV4DestinationUnreachableCode.DestinationHostUnreachable:
                            return IPStatus.DestinationHostUnreachable;
                        case IcmpV4DestinationUnreachableCode.DestinationProtocolUnreachable:
                            return IPStatus.DestinationProtocolUnreachable;
                        case IcmpV4DestinationUnreachableCode.DestinationPortUnreachable:
                            return IPStatus.DestinationPortUnreachable;
                        default:
                            return IPStatus.DestinationUnreachable;
                    }

                case IcmpV4MessageType.SourceQuench:
                    return IPStatus.SourceQuench;

                case IcmpV4MessageType.TimeExceeded:
                    return IPStatus.TimeExceeded;

                case IcmpV4MessageType.ParameterProblemBadIPHeader:
                    return IPStatus.BadHeader;

                default:
                    return IPStatus.Unknown;
            }
        }
    }
}

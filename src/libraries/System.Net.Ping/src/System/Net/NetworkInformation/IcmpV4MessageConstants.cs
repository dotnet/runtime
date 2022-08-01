// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            return (IcmpV4MessageType)type switch
            {
                IcmpV4MessageType.EchoReply => IPStatus.Success,
                IcmpV4MessageType.DestinationUnreachable => (IcmpV4DestinationUnreachableCode)code switch
                {
                    IcmpV4DestinationUnreachableCode.DestinationNetworkUnreachable => IPStatus.DestinationNetworkUnreachable,
                    IcmpV4DestinationUnreachableCode.DestinationHostUnreachable => IPStatus.DestinationHostUnreachable,
                    IcmpV4DestinationUnreachableCode.DestinationProtocolUnreachable => IPStatus.DestinationProtocolUnreachable,
                    IcmpV4DestinationUnreachableCode.DestinationPortUnreachable => IPStatus.DestinationPortUnreachable,
                    _ => IPStatus.DestinationUnreachable,
                },
                IcmpV4MessageType.SourceQuench => IPStatus.SourceQuench,
                IcmpV4MessageType.TimeExceeded => IPStatus.TtlExpired,
                IcmpV4MessageType.ParameterProblemBadIPHeader => IPStatus.BadHeader,
                _ => IPStatus.Unknown,
            };
        }
    }
}

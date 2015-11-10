// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    internal static class Icmpv6MessageConstants
    {
        // Message Types
        internal const int DestinationUnreachable = 1;
        internal const int PacketTooBig = 2;
        internal const int TimeExceeded = 3;
        internal const int ParameterProblem = 4;
        internal const int EchoRequest = 128;
        internal const int EchoReply = 129;

        // DestinationUnreachable Codes
        internal const int NoRouteToDestination = 0;
        internal const int CommunicationAdministrativelyProhibited = 1;
        internal const int BeyondScopeOfSourceAddress = 2;
        internal const int AddressUnreachable = 3;
        internal const int PortUnreachable = 4;
        internal const int SourceAddressFailedPolicy = 5;
        internal const int RejectRouteToDesitnation = 6;
        internal const int SourceRoutingHeaderError = 7;

        // TimeExceeded Codes
        internal const int HopLimitExceeded = 0;
        internal const int FragmentReassemblyTimeExceeded = 1;

        // ParameterProblem Codes
        internal const int ErroneousHeaderField = 0;
        internal const int UnrecognizedNextHeader = 1;
        internal const int UnrecognizedIpv6Option = 2;

        public static IPStatus MapV6TypeToIPStatus(byte type, byte code)
        {
            switch (type)
            {
                case EchoReply:
                    return IPStatus.Success;
                case DestinationUnreachable:
                    switch (code)
                    {
                        case NoRouteToDestination:
                            return IPStatus.BadRoute;
                        case SourceRoutingHeaderError:
                            return IPStatus.BadHeader;
                        default:
                            return IPStatus.Unknown;
                    }
                case PacketTooBig:
                    return IPStatus.PacketTooBig;
                case TimeExceeded:
                    switch (code)
                    {
                        case HopLimitExceeded:
                            return IPStatus.TimeExceeded;
                        case FragmentReassemblyTimeExceeded:
                            return IPStatus.TtlReassemblyTimeExceeded;
                        default:
                            return IPStatus.Unknown;
                    }
                case ParameterProblem:
                    switch (code)
                    {
                        case ErroneousHeaderField:
                            return IPStatus.BadHeader;
                        case UnrecognizedNextHeader:
                            return IPStatus.UnrecognizedNextHeader;
                        case UnrecognizedIpv6Option:
                            return IPStatus.BadOption;
                        default:
                            return IPStatus.Unknown;
                    }
                default:
                    return IPStatus.Unknown;
            }
        }
    }
}

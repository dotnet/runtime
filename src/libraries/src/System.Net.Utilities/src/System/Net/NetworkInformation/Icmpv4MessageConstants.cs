// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    internal static class Icmpv4MessageConstants
    {
        // Message Types
        internal const int EchoReply = 0;
        internal const int DestinationUnreachable = 3;
        internal const int SourceQuench = 4;
        internal const int RedirectMessage = 5;
        internal const int EchoRequest = 8;
        internal const int RouterAdvertisement = 9;
        internal const int RouterSolicitation = 10;
        internal const int TimeExceeded = 11;
        internal const int ParameterProblemBadIPHeader = 12;
        internal const int Timestamp = 13;
        internal const int TimestampReply = 14;
        internal const int InformationRequest = 15;
        internal const int InformationReply = 16;
        internal const int AddressMaskRequest = 17;
        internal const int AddressMaskReply = 18;
        internal const int Traceroute = 30;

        // DestinationUnreachable Codes
        internal const int DestinationNetworkUnreachable = 0;
        internal const int DestinationHostUnreachable = 1;
        internal const int DestinationProtocolUnreachable = 2;
        internal const int DestinationPortUnreachable = 3;
        internal const int FragmentationRequiredAndDFFlagSet = 4;
        internal const int SourceRouteFailed = 5;
        internal const int DestinationNetworkUnknown = 6;
        internal const int DestinationHostUnknown = 7;
        internal const int SourceHostIsolated = 8;
        internal const int NetworkAdministrativelyProhibited = 9;
        internal const int HostAdministrativelyProhibited = 10;
        internal const int NetworkUnreachableForTos = 11;
        internal const int HostUnreachableForTos = 12;
        internal const int CommunicationAdministrativelyProhibited = 13;
        internal const int HostPrecedenceViolation = 14;
        internal const int PrecedenceCutoffInEffect = 15;

        public static IPStatus MapV4TypeToIPStatus(int type, int code)
        {
            switch (type)
            {
                case EchoReply:
                    return IPStatus.Success;
                case DestinationUnreachable:
                    switch (code)
                    {
                        case DestinationNetworkUnreachable:
                            return IPStatus.DestinationNetworkUnreachable;
                        case DestinationHostUnreachable:
                            return IPStatus.DestinationHostUnreachable;
                        case DestinationProtocolUnreachable:
                            return IPStatus.DestinationProtocolUnreachable;
                        case DestinationPortUnreachable:
                            return IPStatus.DestinationPortUnreachable;
                        default:
                            return IPStatus.Unknown;
                    }
                case SourceQuench:
                    return IPStatus.SourceQuench;
                case ParameterProblemBadIPHeader:
                    return IPStatus.BadHeader;
                default:
                    return IPStatus.Unknown;
            }
        }
    }
}

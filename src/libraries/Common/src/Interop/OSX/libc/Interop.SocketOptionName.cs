// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal static partial class libc
    {
        public const int SO_DEBUG = 0x0001;
        public const int SO_REUSEADDR = 0x0004;
        public const int SO_TYPE = 0x1008;
        public const int SO_ERROR = 0x1007;
        public const int SO_DONTROUTE = 0x0010;
        public const int SO_BROADCAST = 0x0020;
        public const int SO_SNDBUF = 0x1001;
        public const int SO_RCVBUF = 0x1002;
        public const int SO_KEEPALIVE = 0x0008;
        public const int SO_OOBINLINE = 0x0100;
        public const int SO_LINGER = 0x0080;
        public const int SO_RCVLOWAT = 0x1004;
        public const int SO_SNDLOWAT = 0x1003;
        public const int SO_RCVTIMEO = 0x1006;
        public const int SO_SNDTIMEO = 0x1005;
        public const int SO_ACCEPTCONN = 0x0002;

        public const int IP_TOS = 3;
        public const int IP_TTL = 4;
        public const int IP_HDRINCL = 2;
        public const int IP_OPTIONS = 1;
        public const int IP_PKTINFO = 26;
        public const int IP_MULTICAST_IF = 9;
        public const int IP_MULTICAST_TTL = 10;
        public const int IP_MULTICAST_LOOP = 11;
        public const int IP_ADD_MEMBERSHIP = 12;
        public const int IP_DROP_MEMBERSHIP = 13;
        public const int IP_UNBLOCK_SOURCE = 73;
        public const int IP_BLOCK_SOURCE = 72;
        public const int IP_ADD_SOURCE_MEMBERSHIP = 70;
        public const int IP_DROP_SOURCE_MEMBERSHIP = 71;

        public const int IPV6_V6ONLY = 27;
        public const int IPV6_RECVPKTINFO = 61;
        public const int IPV6_PKTINFO = 46;

        public const int TCP_NODELAY = 0x1;
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    // NOTE: this is only correct for Linux on x86-64
    internal static partial class libc
    {
        public const int SO_DEBUG = 1;
        public const int SO_REUSEADDR = 2;
        public const int SO_TYPE = 3;
        public const int SO_ERROR = 4;
        public const int SO_DONTROUTE = 5;
        public const int SO_BROADCAST = 6;
        public const int SO_SNDBUF = 7;
        public const int SO_RCVBUF = 8;
        public const int SO_KEEPALIVE = 9;
        public const int SO_OOBINLINE = 10;
        public const int SO_LINGER = 13;
        public const int SO_RCVLOWAT = 18;
        public const int SO_SNDLOWAT = 19;
        public const int SO_RCVTIMEO = 20;
        public const int SO_SNDTIMEO = 21;
        public const int SO_ACCEPTCONN = 30;

        public const int IP_TOS = 1;
        public const int IP_TTL = 2;
        public const int IP_HDRINCL = 3;
        public const int IP_OPTIONS = 4;
        public const int IP_PKTINFO = 8;
        public const int IP_MULTICAST_IF = 32;
        public const int IP_MULTICAST_TTL = 33;
        public const int IP_MULTICAST_LOOP = 34;
        public const int IP_ADD_MEMBERSHIP = 35;
        public const int IP_DROP_MEMBERSHIP = 36;
        public const int IP_UNBLOCK_SOURCE = 37;
        public const int IP_BLOCK_SOURCE = 38;
        public const int IP_ADD_SOURCE_MEMBERSHIP = 39;
        public const int IP_DROP_SOURCE_MEMBERSHIP = 40;

        public const int IPV6_V6ONLY = 26;
        public const int IPV6_RECVPKTINFO = 49;
        public const int IPV6_PKTINFO = 50;

        public const int TCP_NODELAY = 1;
    }
}

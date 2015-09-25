// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal static partial class libc
    {
        public const int SOL_SOCKET = 0xffff;

        public const int IPPROTO_IP = 0;
        public const int IPPROTO_TCP = 6;
        public const int IPPROTO_UDP = 17;
        public const int IPPROTO_IPV6 = 41;
    }
}

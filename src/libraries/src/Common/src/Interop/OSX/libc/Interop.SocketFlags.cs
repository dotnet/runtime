// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal static partial class libc
    {
        public const int MSG_OOB = 0x01;       // Process out-of-band data
        public const int MSG_PEEK = 0x02;      // Peek at incoming messages.
        public const int MSG_DONTROUTE = 0x04; // Don't use local routing.
        public const int MSG_CTRUNC = 0x20;    // Control data lost before delivery.
        public const int MSG_TRUNC = 0x10;     // Data lost before delivery.
    }
}

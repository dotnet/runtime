// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal static partial class libc
    {
        // NOTE: this is only correct for 64-bit platforms!
        public struct timeval
        {
            public long tv_sec;
            public int tv_usec;

            public timeval(int microseconds)
            {
                tv_sec = microseconds / 1000000;
                tv_usec = microseconds % 1000000;
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal static partial class libc
    {
        public struct linger
        {
            public int l_onoff;  // Set to non-zero to linger on close.
            public int l_linger; // Number of seconds to linger.
        }
    }
}

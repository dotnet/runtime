// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

internal static partial class Interop
{
    internal static partial class libc
    {
#pragma warning disable 169,649
        public struct timespec
        {
            public long tv_sec;
            public long tv_nsec;
        }
#pragma warning restore 169, 649
    }
}

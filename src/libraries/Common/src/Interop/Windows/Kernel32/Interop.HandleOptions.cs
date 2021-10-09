// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal static partial class HandleOptions
        {
            internal const int DUPLICATE_SAME_ACCESS = 2;
            internal const int STILL_ACTIVE = 0x00000103;
            internal const int TOKEN_ADJUST_PRIVILEGES = 0x20;
        }
    }
}

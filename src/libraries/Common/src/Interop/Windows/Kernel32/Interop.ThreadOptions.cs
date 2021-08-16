// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal static partial class ThreadOptions
        {
            internal const int THREAD_SET_INFORMATION       = 0x0020;
            internal const int THREAD_QUERY_INFORMATION     = 0x0040;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal static partial class GenericOperations
        {
            internal const int GENERIC_READ = unchecked((int)0x80000000);
            internal const int GENERIC_WRITE = 0x40000000;
        }
    }
}

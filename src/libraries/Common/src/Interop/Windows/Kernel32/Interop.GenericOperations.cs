// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal partial class GenericOperations
        {
            internal const int GENERIC_READ = unchecked((int)0x80000000);
            internal const int GENERIC_WRITE = 0x40000000;
        }
    }
}

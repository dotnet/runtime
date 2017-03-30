// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal partial class SecurityOptions
        {
            internal const int SECURITY_SQOS_PRESENT = 0x00100000;
            internal const int SECURITY_ANONYMOUS = 0 << 16;
            internal const int SECURITY_IDENTIFICATION = 1 << 16;
            internal const int SECURITY_IMPERSONATION = 2 << 16;
            internal const int SECURITY_DELEGATION = 3 << 16;
        }
    }
}

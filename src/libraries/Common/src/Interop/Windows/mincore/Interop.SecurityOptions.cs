// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal partial class Interop
{
    internal partial class mincore
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal const int WAIT_TIMEOUT = 0x00000102;
        internal const int WAIT_OBJECT_0 = 0x00000000;
        internal const int WAIT_ABANDONED = 0x00000080;
    }
}

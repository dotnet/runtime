// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class Netapi32
    {
        [Flags]
        internal enum DS_DOMAINTRUST_FLAG : uint
        {
            DS_DOMAIN_IN_FOREST = 0x0001,
            DS_DOMAIN_DIRECT_OUTBOUND = 0x0002,
            DS_DOMAIN_TREE_ROOT = 0x0004,
            DS_DOMAIN_PRIMARY = 0x0008,
            DS_DOMAIN_NATIVE_MODE = 0x0010,
            DS_DOMAIN_DIRECT_INBOUND = 0x0020
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal enum FINDEX_SEARCH_OPS : uint
        {
            FindExSearchNameMatch = 0x0u,
            FindExSearchLimitToDirectories = 0x1u,
            FindExSearchLimitToDevices = 0x2u,
            FindExSearchMaxSearchOp = 0x3u,
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
internal partial class Interop
{
    internal partial class Kernel32
    {
        internal enum GET_FILEEX_INFO_LEVELS : uint
        {
            GetFileExInfoStandard = 0x0u,
            GetFileExMaxInfoLevel = 0x1u,
        }
    }
}

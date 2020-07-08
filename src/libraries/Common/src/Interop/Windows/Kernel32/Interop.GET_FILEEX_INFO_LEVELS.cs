// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal enum GET_FILEEX_INFO_LEVELS : uint
        {
            GetFileExInfoStandard = 0x0u,
            GetFileExMaxInfoLevel = 0x1u,
        }
    }
}

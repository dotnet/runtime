// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
#if NET
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    internal static partial class Version
    {
        internal static partial class FileVersionInfoType
        {
            internal const int FILE_VER_GET_LOCALISED = 0x1;
            internal const int FILE_VER_GET_NEUTRAL = 0x2;
        }
    }
}

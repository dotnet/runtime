// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
#if NET
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    internal static partial class SspiCli
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct TOKEN_SOURCE
        {
            internal fixed byte SourceName[TOKEN_SOURCE_LENGTH];
            internal LUID SourceIdentifier;

            internal const int TOKEN_SOURCE_LENGTH = 8;
        }
    }
}

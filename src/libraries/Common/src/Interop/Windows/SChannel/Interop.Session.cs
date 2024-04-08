// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class SChannel
    {
        // schannel.h;

        public const int SCHANNEL_SESSION = 3;   // session control

        // Session structure.
        [StructLayout(LayoutKind.Sequential)]
        public struct SCHANNEL_SESSION_TOKEN
        {
            public uint dwTokenType;            // SCHANNEL_SESSION
            public uint dwFlags;
        }

        public const int SSL_SESSION_ENABLE_RECONNECTS = 1;
        public const int SSL_SESSION_DISABLE_RECONNECTS = 2;
    }
}

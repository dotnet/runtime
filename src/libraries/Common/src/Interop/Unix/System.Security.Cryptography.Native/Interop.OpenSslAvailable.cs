// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static class OpenSslNoInit
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_OpenSslAvailable")]
        private static extern int OpenSslAvailable();

        private static readonly Lazy<bool> s_openSslAvailable =
            new Lazy<bool>(() => OpenSslAvailable() != 0);

        internal static bool OpenSslIsAvailable => s_openSslAvailable.Value;
    }
}

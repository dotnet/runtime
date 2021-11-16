// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class OpenSsl
    {
        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_OpenSslVersionNumber")]
        internal static partial long OpenSslVersionNumber();
    }
}

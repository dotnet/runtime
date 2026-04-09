// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [LibraryImport(Libraries.BCrypt)]
        internal static unsafe partial NTSTATUS BCryptGenerateSymmetricKey(
            SafeBCryptAlgorithmHandle hAlgorithm,
            out SafeBCryptKeyHandle phKey,
            IntPtr pbKeyObject,
            int cbKeyObject,
            byte* pbSecret,
            int cbSecret,
            uint dwFlags);

        [LibraryImport(Libraries.BCrypt)]
        internal static unsafe partial NTSTATUS BCryptGenerateSymmetricKey(
            nuint hAlgorithm,
            out SafeBCryptKeyHandle phKey,
            IntPtr pbKeyObject,
            int cbKeyObject,
            byte* pbSecret,
            int cbSecret,
            uint dwFlags);
    }
}

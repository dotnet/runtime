// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [GeneratedDllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
        internal static unsafe partial NTSTATUS BCryptHash(nuint hAlgorithm, byte* pbSecret, int cbSecret, byte* pbInput, int cbInput, byte* pbOutput, int cbOutput);
    }
}

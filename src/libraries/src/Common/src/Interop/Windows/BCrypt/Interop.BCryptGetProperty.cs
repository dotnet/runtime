// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class BCrypt
    {
        [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
        internal static extern unsafe NTSTATUS BCryptGetProperty(SafeBCryptHandle hObject, string pszProperty, void* pbOutput, int cbOutput, out int pcbResult, int dwFlags);
    }
}


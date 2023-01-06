// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Interop.Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool GetTokenInformation(
            SafeTokenHandle TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass,
            void* TokenInformation,
            uint TokenInformationLength,
            out uint ReturnLength);
    }
}

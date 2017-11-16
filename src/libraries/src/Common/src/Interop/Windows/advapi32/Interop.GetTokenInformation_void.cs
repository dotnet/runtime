// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Interop.Libraries.Advapi32, SetLastError = true)]
        internal unsafe static extern bool GetTokenInformation(
            SafeAccessTokenHandle TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass,
            void* TokenInformation,
            uint TokenInformationLength,
            out uint ReturnLength);
    }
}

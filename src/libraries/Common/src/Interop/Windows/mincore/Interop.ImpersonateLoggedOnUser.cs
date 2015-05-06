// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class mincore
    {
        [DllImport(Interop.Libraries.SecurityBase, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool ImpersonateLoggedOnUser(SafeAccessTokenHandle userToken);
    }
}

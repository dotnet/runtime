// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NCrypt
    {
        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern ErrorCode NCryptOpenStorageProvider(out SafeNCryptProviderHandle phProvider, string pszProviderName, int dwFlags);
    }
}

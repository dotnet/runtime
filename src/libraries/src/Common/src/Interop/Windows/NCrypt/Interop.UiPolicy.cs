// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class NCrypt
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct NCRYPT_UI_POLICY
        {
            public int dwVersion;
            public CngUIProtectionLevels dwFlags;
            public IntPtr pszCreationTitle;
            public IntPtr pszFriendlyName;
            public IntPtr pszDescription;
        }
    }
}

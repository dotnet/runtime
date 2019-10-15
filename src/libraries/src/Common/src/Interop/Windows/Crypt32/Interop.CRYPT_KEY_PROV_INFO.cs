// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPT_KEY_PROV_INFO
        {
            internal IntPtr pwszContainerName;
            internal IntPtr pwszProvName;
            internal int dwProvType;
            internal int dwFlags;
            internal int cProvParam;
            internal IntPtr rgProvParam;
            internal CryptKeySpec dwKeySpec;
        }
    }
}

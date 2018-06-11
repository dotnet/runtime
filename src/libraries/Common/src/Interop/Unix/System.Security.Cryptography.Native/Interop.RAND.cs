// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        internal unsafe static bool GetRandomBytes(byte* pbBuffer, int count)
        {
            Debug.Assert(count >= 0);

            return CryptoNative_GetRandomBytes(pbBuffer, count);
        }

        [DllImport(Libraries.CryptoNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private unsafe static extern bool CryptoNative_GetRandomBytes(byte* buf, int num);
    }
}

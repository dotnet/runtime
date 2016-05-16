// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        internal static unsafe bool GetRandomBytes(byte[] buf, int num)
        {
            Debug.Assert(buf != null);
            Debug.Assert(num >= 0 && num <= buf.Length);

            fixed (byte* pBuf = buf)
            {
                return GetRandomBytes(pBuf, num);
            }
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetRandomBytes")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe extern bool GetRandomBytes(byte* buf, int num);
    }
}

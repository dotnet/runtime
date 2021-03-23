// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        private delegate int NegativeSizeReadMethod<in THandle>(THandle handle, byte[]? buf, int cBuf);
        private static byte[] GetDynamicBuffer<THandle>(NegativeSizeReadMethod<THandle> method, THandle handle)
        {
            int negativeSize = method(handle, null, 0);
            if (negativeSize > 0)
                throw new CryptographicException();

            byte[] bytes = new byte[-negativeSize];
            int ret = method(handle, bytes, bytes.Length);
            if (ret != 1)
                throw new CryptographicException();

            return bytes;
        }
    }
}

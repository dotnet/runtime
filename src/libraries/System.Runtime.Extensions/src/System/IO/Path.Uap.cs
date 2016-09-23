// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Windows.Storage.Streams;
using Windows.Security.Cryptography;

namespace System.IO
{
    public static partial class Path
    {
        private static unsafe void GetCryptoRandomBytes(byte* bytes, int byteCount)
        {
            Debug.Assert(bytes != null);
            Debug.Assert(byteCount >= 0);

            byte[] arr;
            IBuffer buffer = CryptographicBuffer.GenerateRandom((uint)byteCount);
            CryptographicBuffer.CopyToByteArray(buffer, out arr);

            Debug.Assert(arr.Length == byteCount);

            Marshal.Copy(arr, 0, new IntPtr(bytes), byteCount);
        }
    }
}

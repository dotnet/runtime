// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Windows.Storage.Streams;
using Windows.Security.Cryptography;

namespace System.IO
{
    public static partial class Path
    {
        private static byte[] CreateCryptoRandomByteArray(int byteLength)
        {
            byte[] arr;
            IBuffer buffer = CryptographicBuffer.GenerateRandom((uint)byteLength);
            CryptographicBuffer.CopyToByteArray(buffer, out arr);
            return arr;
        }
    }
}

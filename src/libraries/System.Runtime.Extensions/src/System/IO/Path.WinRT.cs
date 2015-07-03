// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

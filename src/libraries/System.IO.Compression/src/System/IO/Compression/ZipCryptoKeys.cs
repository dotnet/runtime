// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal readonly struct ZipCryptoKeys
    {
        internal readonly uint Key0;
        internal readonly uint Key1;
        internal readonly uint Key2;

        internal ZipCryptoKeys(uint key0, uint key1, uint key2)
        {
            Key0 = key0;
            Key1 = key1;
            Key2 = key2;
        }
    }
}

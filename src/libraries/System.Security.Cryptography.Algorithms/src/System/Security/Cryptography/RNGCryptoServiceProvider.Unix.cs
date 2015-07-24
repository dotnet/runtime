// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography
{
    internal sealed class RNGCryptoServiceProvider : RandomNumberGenerator
    {
        public sealed override unsafe void GetBytes(byte[] data)
        {
            ValidateGetBytesArgs(data);
            if (data.Length > 0)
            {
                fixed (byte* buf = data)
                {
                    if (Interop.libcrypto.RAND_pseudo_bytes(buf, data.Length) == -1)
                    {
                        throw Interop.libcrypto.CreateOpenSslCryptographicException();
                    }
                }
            }
        }
    }
}

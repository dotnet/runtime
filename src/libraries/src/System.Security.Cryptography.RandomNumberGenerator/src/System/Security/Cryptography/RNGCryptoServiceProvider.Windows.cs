// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

using Internal.NativeCrypto;

namespace System.Security.Cryptography
{
    internal sealed class RNGCryptoServiceProvider : RandomNumberGenerator
    {
        public sealed override void GetBytes(byte[] data)
        {
            ValidateGetBytesArgs(data);
            if (data.Length > 0)
            {
                Cng.BCryptGenRandom(data);
            }
        }
    }
}


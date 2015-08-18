// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public sealed partial class ECDsaCng : ECDsa
    {
        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            return CngAsymmetricAlgorithmCore.HashData(data, offset, count, hashAlgorithm);
        }

        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            return CngAsymmetricAlgorithmCore.HashData(data, hashAlgorithm);
        }
    }
}


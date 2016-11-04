// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public abstract class AsymmetricSignatureFormatter
    {
        protected AsymmetricSignatureFormatter() {}

        public abstract void SetKey(AsymmetricAlgorithm key);
        public abstract void SetHashAlgorithm(string strName);

        public virtual byte[] CreateSignature(HashAlgorithm hash)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));

            SetHashAlgorithm(hash.ToAlgorithmName());
            return CreateSignature(hash.Hash);
        }

        public abstract byte[] CreateSignature(byte[] rgbHash);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    [UnsupportedOSPlatform("browser")]
    public abstract class AsymmetricSignatureFormatter
    {
        protected AsymmetricSignatureFormatter() { }

        public abstract void SetKey(AsymmetricAlgorithm key);
        public abstract void SetHashAlgorithm(string strName);

        public virtual byte[] CreateSignature(HashAlgorithm hash)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));

            SetHashAlgorithm(hash.ToAlgorithmName()!);
            Debug.Assert(hash.Hash != null);
            return CreateSignature(hash.Hash);
        }

        public abstract byte[] CreateSignature(byte[] rgbHash);
    }
}

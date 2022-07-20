// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    public abstract class AsymmetricSignatureDeformatter
    {
        protected AsymmetricSignatureDeformatter() { }

        public abstract void SetKey(AsymmetricAlgorithm key);
        public abstract void SetHashAlgorithm(string strName);

        public virtual bool VerifySignature(HashAlgorithm hash, byte[] rgbSignature)
        {
            ArgumentNullException.ThrowIfNull(hash);

            SetHashAlgorithm(hash.ToAlgorithmName()!);
            Debug.Assert(hash.Hash != null);
            return VerifySignature(hash.Hash, rgbSignature);
        }

        public abstract bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);
    }
}

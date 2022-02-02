// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public abstract class AsymmetricKeyExchangeDeformatter
    {
        protected AsymmetricKeyExchangeDeformatter() { }
        public abstract string? Parameters { get; set; }
        public abstract void SetKey(AsymmetricAlgorithm key);
        public abstract byte[] DecryptKeyExchange(byte[] rgb);
    }
}

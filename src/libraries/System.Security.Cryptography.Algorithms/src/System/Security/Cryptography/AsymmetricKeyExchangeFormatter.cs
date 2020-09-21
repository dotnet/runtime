// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    [UnsupportedOSPlatform("browser")]
    public abstract class AsymmetricKeyExchangeFormatter
    {
        protected AsymmetricKeyExchangeFormatter() { }

        public abstract string? Parameters { get; }

        public abstract void SetKey(AsymmetricAlgorithm key);
        public abstract byte[] CreateKeyExchange(byte[] data);

        // For .NET Framework compat, keep this even though symAlgType is not used.
        public abstract byte[] CreateKeyExchange(byte[] data, Type? symAlgType);
    }
}

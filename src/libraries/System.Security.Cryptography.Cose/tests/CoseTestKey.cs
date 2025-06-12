// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Security.Cryptography.Cose.Tests
{
    public sealed class CoseTestKey : IDisposable
    {
        public string Id { get; }

        public IDisposable Key { get; }
        public CoseSigner Signer { get; }
        public CoseKey CoseKey { get; }

        public CoseTestKey(string keyId, IDisposable key, CoseKey coseKey)
        {
            Id = keyId;

            Key = key;
            CoseKey = coseKey;
            Signer = new(coseKey, protectedHeaders: new CoseHeaderMap { [CoseHeaderLabel.KeyIdentifier] = CoseHeaderValue.FromBytes(Encoding.UTF8.GetBytes(keyId)) });
        }
        public void Dispose()
        {
            Key.Dispose();
        }

        public override string ToString() => $"KeyId={Id}";
    }
}

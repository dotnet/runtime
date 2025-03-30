// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public class SlhDsaOpenSslConstructionTests : SlhDsaConstructionTestsBase
    {
        protected override SlhDsa GenerateKey(SlhDsaAlgorithm algorithm)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.SlhDsaGenerateKey(algorithm.Name, ReadOnlySpan<byte>.Empty);
            return new SlhDsaOpenSsl(key);
        }

        protected override SlhDsa ImportSlhDsaPrivateSeed(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> seed)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.SlhDsaGenerateKey(algorithm.Name, seed);
            return new SlhDsaOpenSsl(key);
        }

        protected override SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: false);
            return new SlhDsaOpenSsl(key);
        }

        protected override SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: true);
            return new SlhDsaOpenSsl(key);
        }
    }
}

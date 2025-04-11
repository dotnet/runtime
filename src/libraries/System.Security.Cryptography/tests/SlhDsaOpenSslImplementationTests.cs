// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    [ConditionalClass(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
    public sealed class SlhDsaOpenSslImplementationTests : SlhDsaImplementationTestsBase
    {
        protected override SlhDsa GenerateKey(SlhDsaAlgorithm algorithm)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.SlhDsaGenerateKey(algorithm.Name);
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    [ConditionalClass(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
    public sealed class SlhDsaDefaultImplementationTests : SlhDsaImplementationTestsBase
    {
        protected override SlhDsa GenerateKey(SlhDsaAlgorithm algorithm) => SlhDsa.GenerateKey(algorithm);
        protected override SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => SlhDsa.ImportSlhDsaPublicKey(algorithm, source);
        protected override SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => SlhDsa.ImportSlhDsaSecretKey(algorithm, source);
    }
}

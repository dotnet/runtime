// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    public class MLKemImplementationTests : MLKemBaseTests
    {
        public override MLKem GenerateKey(MLKemAlgorithm algorithm)
        {
            return MLKem.GenerateKey(algorithm);
        }

        public override MLKem ImportPrivateSeed(MLKemAlgorithm algorithm, ReadOnlySpan<byte> seed)
        {
            return MLKem.ImportPrivateSeed(algorithm, seed);
        }

        public override MLKem ImportDecapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            return MLKem.ImportDecapsulationKey(algorithm, source);
        }

        public override MLKem ImportEncapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            return MLKem.ImportEncapsulationKey(algorithm, source);
        }
    }
}

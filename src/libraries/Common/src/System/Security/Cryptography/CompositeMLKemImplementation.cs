// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal sealed partial class CompositeMLKemImplementation : CompositeMLKem
    {
        internal static partial bool IsAlgorithmSupportedImpl(CompositeMLKemAlgorithm algorithm);

        internal static partial CompositeMLKem GenerateKeyImpl(CompositeMLKemAlgorithm algorithm);

        internal static partial CompositeMLKem ImportEncapsulationKeyImpl(CompositeMLKemAlgorithm algorithm, ReadOnlySpan<byte> source);

        internal static partial CompositeMLKem ImportDecapsulationKeyImpl(CompositeMLKemAlgorithm algorithm, ReadOnlySpan<byte> source);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable IDE0060 // Remove unused parameter

namespace System.Security.Cryptography
{
    internal sealed partial class CompositeMLKemImplementation : CompositeMLKem
    {
        public CompositeMLKemImplementation(CompositeMLKemAlgorithm algorithm)
            : base(algorithm)
        {
            throw new PlatformNotSupportedException();
        }

        internal static partial bool IsAlgorithmSupportedImpl(CompositeMLKemAlgorithm algorithm) => false;

        internal static partial CompositeMLKem GenerateKeyImpl(CompositeMLKemAlgorithm algorithm) =>
            throw new PlatformNotSupportedException();

        internal static partial CompositeMLKem ImportEncapsulationKeyImpl(CompositeMLKemAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        internal static partial CompositeMLKem ImportDecapsulationKeyImpl(CompositeMLKemAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret) =>
            throw new PlatformNotSupportedException();

        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret) =>
            throw new PlatformNotSupportedException();

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten) =>
            throw new PlatformNotSupportedException();

        protected override int ExportEncapsulationKeyCore(Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        protected override int ExportDecapsulationKeyCore(Span<byte> destination) =>
            throw new PlatformNotSupportedException();
    }
}

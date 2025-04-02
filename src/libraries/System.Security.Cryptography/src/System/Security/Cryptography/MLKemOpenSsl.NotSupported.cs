// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    public sealed partial class MLKemOpenSsl : MLKem
    {
#pragma warning disable CA1822 // Member does not access instance data and can be marked static
        private partial void Initialize(SafeEvpPKeyHandle upRefHandle)
#pragma warning restore CA1822
        {
            throw new PlatformNotSupportedException();
        }

        private static partial MLKemAlgorithm AlgorithmFromHandle(SafeEvpPKeyHandle pkeyHandle, out SafeEvpPKeyHandle upRefHandle)
        {
            throw new PlatformNotSupportedException();
        }

        public partial SafeEvpPKeyHandle DuplicateKeyHandle()
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override void ExportPrivateSeedCore(Span<byte> destination)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override void ExportDecapsulationKeyCore(Span<byte> destination)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override void ExportEncapsulationKeyCore(Span<byte> destination)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }
    }
}

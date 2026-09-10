// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    internal sealed class CompositeMLKemMockImplementation : CompositeMLKem
    {
        internal static CompositeMLKemMockImplementation Create(CompositeMLKemAlgorithm algorithm) =>
            new CompositeMLKemMockImplementation(algorithm);

        public CompositeMLKemMockImplementation(CompositeMLKemAlgorithm algorithm)
            : base(algorithm)
        {
        }

        internal delegate void EncapsulateAction(Span<byte> ciphertext, Span<byte> sharedSecret);
        internal delegate void DecapsulateAction(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret);
        internal delegate int ExportFunc(Span<byte> destination);
        internal delegate bool TryExportFunc(Span<byte> destination, out int written);
        internal delegate void DisposeAction(bool disposing);

        public int EncapsulateCoreCallCount;
        public int DecapsulateCoreCallCount;
        public int ExportEncapsulationKeyCoreCallCount;
        public int ExportDecapsulationKeyCoreCallCount;
        public int TryExportPkcs8PrivateKeyCoreCallCount;
        public int DisposeCallCount;

        public EncapsulateAction EncapsulateCoreHook { get; set; } = (_, _) => Assert.Fail();
        public DecapsulateAction DecapsulateCoreHook { get; set; } = (_, _) => Assert.Fail();
        public ExportFunc ExportEncapsulationKeyCoreHook { get; set; } = _ => { Assert.Fail(); return 0; };
        public ExportFunc ExportDecapsulationKeyCoreHook { get; set; } = _ => { Assert.Fail(); return 0; };
        public TryExportFunc TryExportPkcs8PrivateKeyCoreHook { get; set; } = (Span<byte> destination, out int bytesWritten) => { Assert.Fail(); bytesWritten = 0; return false; };
        public DisposeAction DisposeHook { get; set; } = _ => { };

        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            EncapsulateCoreCallCount++;
            EncapsulateCoreHook(ciphertext, sharedSecret);
        }

        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            DecapsulateCoreCallCount++;
            DecapsulateCoreHook(ciphertext, sharedSecret);
        }

        protected override int ExportEncapsulationKeyCore(Span<byte> destination)
        {
            ExportEncapsulationKeyCoreCallCount++;
            return ExportEncapsulationKeyCoreHook(destination);
        }

        protected override int ExportDecapsulationKeyCore(Span<byte> destination)
        {
            ExportDecapsulationKeyCoreCallCount++;
            return ExportDecapsulationKeyCoreHook(destination);
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            TryExportPkcs8PrivateKeyCoreCallCount++;
            return TryExportPkcs8PrivateKeyCoreHook(destination, out bytesWritten);
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCallCount++;
            DisposeHook(disposing);
        }

        public void AddLengthAssertion()
        {
            EncapsulateAction oldEncapsulateCoreHook = EncapsulateCoreHook;
            EncapsulateCoreHook = (Span<byte> ciphertext, Span<byte> sharedSecret) =>
            {
                oldEncapsulateCoreHook(ciphertext, sharedSecret);
                Assert.Equal(Algorithm.CiphertextSizeInBytes, ciphertext.Length);
                Assert.Equal(Algorithm.SharedSecretSizeInBytes, sharedSecret.Length);
            };

            DecapsulateAction oldDecapsulateCoreHook = DecapsulateCoreHook;
            DecapsulateCoreHook = (ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret) =>
            {
                oldDecapsulateCoreHook(ciphertext, sharedSecret);
                Assert.Equal(Algorithm.CiphertextSizeInBytes, ciphertext.Length);
                Assert.Equal(Algorithm.SharedSecretSizeInBytes, sharedSecret.Length);
            };

            ExportFunc oldExportEncapsulationKeyCoreHook = ExportEncapsulationKeyCoreHook;
            ExportEncapsulationKeyCoreHook = (Span<byte> destination) =>
            {
                int ret = oldExportEncapsulationKeyCoreHook(destination);
                AssertExtensions.GreaterThanOrEqualTo(
                    destination.Length,
                    CompositeMLKemTestData.ExpectedEncapsulationKeySizeLowerBound(Algorithm));
                return ret;
            };

            ExportFunc oldExportDecapsulationKeyCoreHook = ExportDecapsulationKeyCoreHook;
            ExportDecapsulationKeyCoreHook = (Span<byte> destination) =>
            {
                int ret = oldExportDecapsulationKeyCoreHook(destination);
                AssertExtensions.GreaterThanOrEqualTo(
                    destination.Length,
                    CompositeMLKemTestData.ExpectedDecapsulationKeySizeLowerBound(Algorithm));
                return ret;
            };

            TryExportFunc oldTryExportPkcs8PrivateKeyCoreHook = TryExportPkcs8PrivateKeyCoreHook;
            TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                bool ret = oldTryExportPkcs8PrivateKeyCoreHook(destination, out bytesWritten);
                AssertExtensions.GreaterThanOrEqualTo(
                    destination.Length,
                    CompositeMLKemTestData.ExpectedDecapsulationKeySizeLowerBound(Algorithm));
                return ret;
            };
        }

        public void AddDestinationBufferIsSameAssertion(ReadOnlyMemory<byte> buffer)
        {
            ExportFunc oldExportEncapsulationKeyCoreHook = ExportEncapsulationKeyCoreHook;
            ExportEncapsulationKeyCoreHook = (Span<byte> destination) =>
            {
                int ret = oldExportEncapsulationKeyCoreHook(destination);
                AssertExtensions.Same(buffer.Span, destination);
                return ret;
            };

            ExportFunc oldExportDecapsulationKeyCoreHook = ExportDecapsulationKeyCoreHook;
            ExportDecapsulationKeyCoreHook = (Span<byte> destination) =>
            {
                int ret = oldExportDecapsulationKeyCoreHook(destination);
                AssertExtensions.Same(buffer.Span, destination);
                return ret;
            };
        }

        public void AddCiphertextBufferIsSameAssertion(ReadOnlyMemory<byte> buffer)
        {
            EncapsulateAction oldEncapsulateCoreHook = EncapsulateCoreHook;
            EncapsulateCoreHook = (Span<byte> ciphertext, Span<byte> sharedSecret) =>
            {
                oldEncapsulateCoreHook(ciphertext, sharedSecret);
                AssertExtensions.Same(buffer.Span, ciphertext);
            };

            DecapsulateAction oldDecapsulateCoreHook = DecapsulateCoreHook;
            DecapsulateCoreHook = (ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret) =>
            {
                oldDecapsulateCoreHook(ciphertext, sharedSecret);
                AssertExtensions.Same(buffer.Span, ciphertext);
            };
        }

        public void AddSharedSecretBufferIsSameAssertion(ReadOnlyMemory<byte> buffer)
        {
            EncapsulateAction oldEncapsulateCoreHook = EncapsulateCoreHook;
            EncapsulateCoreHook = (Span<byte> ciphertext, Span<byte> sharedSecret) =>
            {
                oldEncapsulateCoreHook(ciphertext, sharedSecret);
                AssertExtensions.Same(buffer.Span, sharedSecret);
            };

            DecapsulateAction oldDecapsulateCoreHook = DecapsulateCoreHook;
            DecapsulateCoreHook = (ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret) =>
            {
                oldDecapsulateCoreHook(ciphertext, sharedSecret);
                AssertExtensions.Same(buffer.Span, sharedSecret);
            };
        }

        public void AddFillDestination(byte b)
        {
            EncapsulateAction oldEncapsulateCoreHook = EncapsulateCoreHook;
            EncapsulateCoreHook = (Span<byte> ciphertext, Span<byte> sharedSecret) =>
            {
                oldEncapsulateCoreHook(ciphertext, sharedSecret);
                ciphertext.Fill(b);
                sharedSecret.Fill(b);
            };

            DecapsulateAction oldDecapsulateCoreHook = DecapsulateCoreHook;
            DecapsulateCoreHook = (ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret) =>
            {
                oldDecapsulateCoreHook(ciphertext, sharedSecret);
                sharedSecret.Fill(b);
            };

            ExportFunc oldExportEncapsulationKeyCoreHook = ExportEncapsulationKeyCoreHook;
            ExportEncapsulationKeyCoreHook = (Span<byte> destination) =>
            {
                _ = oldExportEncapsulationKeyCoreHook(destination);
                destination.Fill(b);
                return destination.Length;
            };

            ExportFunc oldExportDecapsulationKeyCoreHook = ExportDecapsulationKeyCoreHook;
            ExportDecapsulationKeyCoreHook = (Span<byte> destination) =>
            {
                _ = oldExportDecapsulationKeyCoreHook(destination);
                destination.Fill(b);
                return destination.Length;
            };

            TryExportFunc oldTryExportPkcs8PrivateKeyCoreHook = TryExportPkcs8PrivateKeyCoreHook;
            TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                bool ret = oldTryExportPkcs8PrivateKeyCoreHook(destination, out bytesWritten);
                destination.Fill(b);
                return ret;
            };
        }

        public void SetNoOpHooks()
        {
            EncapsulateCoreHook = (_, _) => { };
            DecapsulateCoreHook = (_, _) => { };
            ExportEncapsulationKeyCoreHook = destination => destination.Length;
            ExportDecapsulationKeyCoreHook = destination => destination.Length;
            TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                bytesWritten = destination.Length;
                return true;
            };
        }

        public void AddFillDestination(byte[] fillContents)
        {
            ExportFunc oldExportEncapsulationKeyCoreHook = ExportEncapsulationKeyCoreHook;
            ExportEncapsulationKeyCoreHook = (Span<byte> destination) =>
            {
                _ = oldExportEncapsulationKeyCoreHook(destination);

                if (fillContents.AsSpan().TryCopyTo(destination))
                {
                    return fillContents.Length;
                }

                return 0;
            };

            ExportFunc oldExportDecapsulationKeyCoreHook = ExportDecapsulationKeyCoreHook;
            ExportDecapsulationKeyCoreHook = (Span<byte> destination) =>
            {
                _ = oldExportDecapsulationKeyCoreHook(destination);

                if (fillContents.AsSpan().TryCopyTo(destination))
                {
                    return fillContents.Length;
                }

                return 0;
            };

            TryExportFunc oldTryExportPkcs8PrivateKeyCoreHook = TryExportPkcs8PrivateKeyCoreHook;
            TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                _ = oldTryExportPkcs8PrivateKeyCoreHook(destination, out _);

                if (fillContents.AsSpan().TryCopyTo(destination))
                {
                    bytesWritten = fillContents.Length;
                    return true;
                }

                bytesWritten = 0;
                return false;
            };
        }
    }
}

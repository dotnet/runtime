// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    internal sealed class CompositeMLDsaMockImplementation : CompositeMLDsa
    {
        internal static CompositeMLDsaMockImplementation Create(CompositeMLDsaAlgorithm algorithm) =>
            new CompositeMLDsaMockImplementation(algorithm);

        public CompositeMLDsaMockImplementation(CompositeMLDsaAlgorithm algorithm)
            : base(algorithm)
        {
        }

        internal delegate int SignDataFunc(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination);
        internal delegate bool VerifyDataFunc(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature);
        internal delegate bool TryExportFunc(Span<byte> destination, out int bytesWritten);
        internal delegate void DisposeAction(bool disposing);

        public int SignDataCoreCallCount = 0;
        public int VerifyDataCoreCallCount = 0;
        public int TryExportCompositeMLDsaPublicKeyCoreCallCount = 0;
        public int TryExportCompositeMLDsaPrivateKeyCoreCallCount = 0;
        public int DisposeCallCount = 0;

        public SignDataFunc SignDataCoreHook { get; set; } = (_, _, _) => { Assert.Fail(); return 0; };
        public VerifyDataFunc VerifyDataCoreHook { get; set; } = (_, _, _) => { Assert.Fail(); return false; };
        public TryExportFunc TryExportCompositeMLDsaPublicKeyCoreHook { get; set; } = (_, out bytesWritten) => { Assert.Fail(); bytesWritten = 0; return false; };
        public TryExportFunc TryExportCompositeMLDsaPrivateKeyCoreHook { get; set; } = (_, out bytesWritten) => { Assert.Fail(); bytesWritten = 0; return false; };
        public DisposeAction DisposeHook { get; set; } = _ => { };

        protected override int SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination)
        {
            SignDataCoreCallCount++;
            return SignDataCoreHook(data, context, destination);
        }

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature)
        {
            VerifyDataCoreCallCount++;
            return VerifyDataCoreHook(data, context, signature);
        }

        protected override bool TryExportCompositeMLDsaPublicKeyCore(Span<byte> destination, out int bytesWritten)
        {
            TryExportCompositeMLDsaPublicKeyCoreCallCount++;
            return TryExportCompositeMLDsaPublicKeyCoreHook(destination, out bytesWritten);
        }

        protected override bool TryExportCompositeMLDsaPrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            TryExportCompositeMLDsaPrivateKeyCoreCallCount++;
            return TryExportCompositeMLDsaPrivateKeyCoreHook(destination, out bytesWritten);
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCallCount++;
            DisposeHook(disposing);
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            throw new NotImplementedException();
        }

        public void AddLengthAssertion()
        {
            SignDataFunc oldTrySignDataCoreHook = SignDataCoreHook;
            SignDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                int ret = oldTrySignDataCoreHook(data, context, destination);
                AssertExtensions.LessThanOrEqualTo(
                    32 + CompositeMLDsaTestHelpers.MLDsaAlgorithms[Algorithm].SignatureSizeInBytes, // randomizer + mldsaSig
                    destination.Length);
                return ret;
            };

            VerifyDataFunc oldVerifyDataCoreHook = VerifyDataCoreHook;
            VerifyDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            {
                bool ret = oldVerifyDataCoreHook(data, context, signature);
                AssertExtensions.LessThanOrEqualTo(
                    32 + CompositeMLDsaTestHelpers.MLDsaAlgorithms[Algorithm].SignatureSizeInBytes, // randomizer + mldsaSig
                    signature.Length);
                return ret;
            };

            TryExportFunc oldTryExportCompositeMLDsaPublicKeyCoreHook = TryExportCompositeMLDsaPublicKeyCoreHook;
            TryExportCompositeMLDsaPublicKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                bool ret = oldTryExportCompositeMLDsaPublicKeyCoreHook(destination, out bytesWritten);
                AssertExtensions.LessThanOrEqualTo(
                    CompositeMLDsaTestHelpers.MLDsaAlgorithms[Algorithm].PublicKeySizeInBytes,
                    destination.Length);
                return ret;
            };

            TryExportFunc oldTryExportCompositeMLDsaPrivateKeyCoreHook = TryExportCompositeMLDsaPrivateKeyCoreHook;
            TryExportCompositeMLDsaPrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                bool ret = oldTryExportCompositeMLDsaPrivateKeyCoreHook(destination, out bytesWritten);
                AssertExtensions.LessThanOrEqualTo(
                    CompositeMLDsaTestHelpers.MLDsaAlgorithms[Algorithm].PrivateSeedSizeInBytes,
                    destination.Length);
                return ret;
            };
        }

        public void AddDestinationBufferIsSameAssertion(ReadOnlyMemory<byte> buffer)
        {
            SignDataFunc oldTrySignDataCoreHook = SignDataCoreHook;
            SignDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                int ret = oldTrySignDataCoreHook(data, context, destination);
                AssertExtensions.Same(buffer.Span, destination);
                return ret;
            };

            TryExportFunc oldTryExportCompositeMLDsaPublicKeyCoreHook = TryExportCompositeMLDsaPublicKeyCoreHook;
            TryExportCompositeMLDsaPublicKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                bool ret = oldTryExportCompositeMLDsaPublicKeyCoreHook(destination, out bytesWritten);
                AssertExtensions.Same(buffer.Span, destination);
                return ret;
            };

            TryExportFunc oldTryExportCompositeMLDsaPrivateKeyCoreHook = TryExportCompositeMLDsaPrivateKeyCoreHook;
            TryExportCompositeMLDsaPrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                bool ret = oldTryExportCompositeMLDsaPrivateKeyCoreHook(destination, out bytesWritten);
                AssertExtensions.Same(buffer.Span, destination);
                return ret;
            };
        }

        public void AddContextBufferIsSameAssertion(ReadOnlyMemory<byte> buffer)
        {
            SignDataFunc oldTrySignDataCoreHook = SignDataCoreHook;
            SignDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                int ret = oldTrySignDataCoreHook(data, context, destination);
                AssertExtensions.Same(buffer.Span, context);
                return ret;
            };

            VerifyDataFunc oldVerifyDataCoreHook = VerifyDataCoreHook;
            VerifyDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            {
                bool ret = oldVerifyDataCoreHook(data, context, signature);
                AssertExtensions.Same(buffer.Span, context);
                return ret;
            };
        }

        public void AddSignatureBufferIsSameAssertion(ReadOnlyMemory<byte> buffer)
        {
            VerifyDataFunc oldVerifyDataCoreHook = VerifyDataCoreHook;
            VerifyDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            {
                bool ret = oldVerifyDataCoreHook(data, context, signature);
                AssertExtensions.Same(buffer.Span, signature);
                return ret;
            };
        }

        public void AddDataBufferIsSameAssertion(ReadOnlyMemory<byte> buffer)
        {
            SignDataFunc oldTrySignDataCoreHook = SignDataCoreHook;
            SignDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                int ret = oldTrySignDataCoreHook(data, context, destination);
                AssertExtensions.Same(buffer.Span, data);
                return ret;
            };

            VerifyDataFunc oldVerifyDataCoreHook = VerifyDataCoreHook;
            VerifyDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            {
                bool ret = oldVerifyDataCoreHook(data, context, signature);
                AssertExtensions.Same(buffer.Span, data);
                return ret;
            };
        }

        public void AddFillDestination(byte b)
        {
            SignDataFunc oldTrySignDataCoreHook = SignDataCoreHook;
            SignDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                _ = oldTrySignDataCoreHook(data, context, destination);
                destination.Fill(b);
                return destination.Length;
            };

            TryExportFunc oldExportCompositeMLDsaPublicKeyCoreHook = TryExportCompositeMLDsaPublicKeyCoreHook;
            TryExportCompositeMLDsaPublicKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                _ = oldExportCompositeMLDsaPublicKeyCoreHook(destination, out _);
                destination.Fill(b);
                bytesWritten = destination.Length;
                return true;
            };

            TryExportFunc oldExportCompositeMLDsaPrivateKeyCoreHook = TryExportCompositeMLDsaPrivateKeyCoreHook;
            TryExportCompositeMLDsaPrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                _ = oldExportCompositeMLDsaPrivateKeyCoreHook(destination, out _);
                destination.Fill(b);
                bytesWritten = destination.Length;
                return true;
            };
        }

        public void AddFillDestination(byte[] fillContents)
        {
            SignDataFunc oldTrySignDataCoreHook = SignDataCoreHook;
            SignDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                _ = oldTrySignDataCoreHook(data, context, destination);

                if (fillContents.AsSpan().TryCopyTo(destination))
                {
                    return fillContents.Length;
                }

                return 0;
            };

            TryExportFunc oldExportCompositeMLDsaPublicKeyCoreHook = TryExportCompositeMLDsaPublicKeyCoreHook;
            TryExportCompositeMLDsaPublicKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                _ = oldExportCompositeMLDsaPublicKeyCoreHook(destination, out _);

                if (fillContents.AsSpan().TryCopyTo(destination))
                {
                    bytesWritten = fillContents.Length;
                    return true;
                }

                bytesWritten = 0;
                return false;
            };

            TryExportFunc oldExportCompositeMLDsaPrivateKeyCoreHook = TryExportCompositeMLDsaPrivateKeyCoreHook;
            TryExportCompositeMLDsaPrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                _ = oldExportCompositeMLDsaPrivateKeyCoreHook(destination, out _);

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

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
        internal delegate int ExportFunc(Span<byte> destination);
        internal delegate bool TryExportFunc(Span<byte> destination, out int written);
        internal delegate void DisposeAction(bool disposing);

        public int SignDataCoreCallCount = 0;
        public int VerifyDataCoreCallCount = 0;
        public int ExportCompositeMLDsaPublicKeyCoreCallCount = 0;
        public int ExportCompositeMLDsaPrivateKeyCoreCallCount = 0;
        public int TryExportPkcs8PrivateKeyCoreCallCount = 0;
        public int DisposeCallCount = 0;

        public SignDataFunc SignDataCoreHook { get; set; } = (_, _, _) => { Assert.Fail(); return 0; };
        public VerifyDataFunc VerifyDataCoreHook { get; set; } = (_, _, _) => { Assert.Fail(); return false; };
        public ExportFunc ExportCompositeMLDsaPublicKeyCoreHook { get; set; } = _ => { Assert.Fail(); return 0; };
        public ExportFunc ExportCompositeMLDsaPrivateKeyCoreHook { get; set; } = _ => { Assert.Fail(); return 0; };
        public TryExportFunc TryExportPkcs8PrivateKeyCoreHook { get; set; } = (destination, out bytesWritten) => { Assert.Fail(); bytesWritten = 0; return false; };
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

        protected override int ExportCompositeMLDsaPublicKeyCore(Span<byte> destination)
        {
            ExportCompositeMLDsaPublicKeyCoreCallCount++;
            return ExportCompositeMLDsaPublicKeyCoreHook(destination);
        }

        protected override int ExportCompositeMLDsaPrivateKeyCore(Span<byte> destination)
        {
            ExportCompositeMLDsaPrivateKeyCoreCallCount++;
            return ExportCompositeMLDsaPrivateKeyCoreHook(destination);
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCallCount++;
            DisposeHook(disposing);
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            TryExportPkcs8PrivateKeyCoreCallCount++;
            return TryExportPkcs8PrivateKeyCoreHook(destination, out bytesWritten);
        }

        public void AddLengthAssertion()
        {
            SignDataFunc oldTrySignDataCoreHook = SignDataCoreHook;
            SignDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                int ret = oldTrySignDataCoreHook(data, context, destination);
                AssertExtensions.LessThanOrEqualTo(
                    CompositeMLDsaTestHelpers.MLDsaAlgorithms[Algorithm].SignatureSizeInBytes, // mldsaSig
                    destination.Length);
                return ret;
            };

            VerifyDataFunc oldVerifyDataCoreHook = VerifyDataCoreHook;
            VerifyDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            {
                bool ret = oldVerifyDataCoreHook(data, context, signature);
                AssertExtensions.LessThanOrEqualTo(
                    CompositeMLDsaTestHelpers.MLDsaAlgorithms[Algorithm].SignatureSizeInBytes, // mldsaSig
                    signature.Length);
                return ret;
            };

            ExportFunc oldExportCompositeMLDsaPublicKeyCoreHook = ExportCompositeMLDsaPublicKeyCoreHook;
            ExportCompositeMLDsaPublicKeyCoreHook = (Span<byte> destination) =>
            {
                int ret = oldExportCompositeMLDsaPublicKeyCoreHook(destination);
                AssertExtensions.GreaterThanOrEqualTo(
                    destination.Length,
                    CompositeMLDsaTestHelpers.ExpectedPublicKeySizeLowerBound(Algorithm));
                return ret;
            };

            ExportFunc oldExportCompositeMLDsaPrivateKeyCoreHook = ExportCompositeMLDsaPrivateKeyCoreHook;
            ExportCompositeMLDsaPrivateKeyCoreHook = (Span<byte> destination) =>
            {
                int ret = oldExportCompositeMLDsaPrivateKeyCoreHook(destination);
                AssertExtensions.GreaterThanOrEqualTo(
                    destination.Length,
                    CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeLowerBound(Algorithm));
                return ret;
            };

            TryExportFunc oldTryExportPkcs8PrivateKeyCoreHook = TryExportPkcs8PrivateKeyCoreHook;
            TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                bool ret = oldTryExportPkcs8PrivateKeyCoreHook(destination, out bytesWritten);
                AssertExtensions.GreaterThanOrEqualTo(
                    destination.Length,
                    CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeLowerBound(Algorithm));
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

            ExportFunc oldExportCompositeMLDsaPublicKeyCoreHook = ExportCompositeMLDsaPublicKeyCoreHook;
            ExportCompositeMLDsaPublicKeyCoreHook = (Span<byte> destination) =>
            {
                int ret = oldExportCompositeMLDsaPublicKeyCoreHook(destination);
                AssertExtensions.Same(buffer.Span, destination);
                return ret;
            };

            ExportFunc oldExportCompositeMLDsaPrivateKeyCoreHook = ExportCompositeMLDsaPrivateKeyCoreHook;
            ExportCompositeMLDsaPrivateKeyCoreHook = (Span<byte> destination) =>
            {
                int ret = oldExportCompositeMLDsaPrivateKeyCoreHook(destination);
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

            ExportFunc oldExportCompositeMLDsaPublicKeyCoreHook = ExportCompositeMLDsaPublicKeyCoreHook;
            ExportCompositeMLDsaPublicKeyCoreHook = (Span<byte> destination) =>
            {
                _ = oldExportCompositeMLDsaPublicKeyCoreHook(destination);
                destination.Fill(b);
                return destination.Length;
            };

            ExportFunc oldExportCompositeMLDsaPrivateKeyCoreHook = ExportCompositeMLDsaPrivateKeyCoreHook;
            ExportCompositeMLDsaPrivateKeyCoreHook = (Span<byte> destination) =>
            {
                _ = oldExportCompositeMLDsaPrivateKeyCoreHook(destination);
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

            ExportFunc oldExportCompositeMLDsaPublicKeyCoreHook = ExportCompositeMLDsaPublicKeyCoreHook;
            ExportCompositeMLDsaPublicKeyCoreHook = (Span<byte> destination) =>
            {
                _ = oldExportCompositeMLDsaPublicKeyCoreHook(destination);

                if (fillContents.AsSpan().TryCopyTo(destination))
                {
                    return fillContents.Length;
                }

                return 0;
            };

            ExportFunc oldExportCompositeMLDsaPrivateKeyCoreHook = ExportCompositeMLDsaPrivateKeyCoreHook;
            ExportCompositeMLDsaPrivateKeyCoreHook = (Span<byte> destination) =>
            {
                _ = oldExportCompositeMLDsaPrivateKeyCoreHook(destination);

                if (fillContents.AsSpan().TryCopyTo(destination))
                {
                    return fillContents.Length;
                }

                return 0;
            };

            TryExportFunc oldTryExportPkcs8PrivateKeyCoreHook = TryExportPkcs8PrivateKeyCoreHook;
            TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                bool ret = oldTryExportPkcs8PrivateKeyCoreHook(destination, out int localBytesWritten);

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

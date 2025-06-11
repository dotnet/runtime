// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    internal sealed class MLDsaTestImplementation : MLDsa
    {
        internal delegate void ExportAction(Span<byte> destination);
        internal delegate bool TryExportFunc(Span<byte> destination, out int bytesWritten);
        internal delegate void SignAction(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination);
        internal delegate bool VerifyFunc(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature);

        internal int VerifyDataCoreCallCount = 0;
        internal int SignDataCoreCallCount = 0;
        internal int ExportMLDsaPrivateSeedCoreCallCount = 0;
        internal int ExportMLDsaPublicKeyCoreCallCount = 0;
        internal int ExportMLDsaSecretKeyCoreCallCount = 0;
        internal int TryExportPkcs8PrivateKeyCoreCallCount = 0;
        internal int DisposeCallCount = 0;

        internal ExportAction ExportMLDsaPrivateSeedHook { get; set; }
        internal ExportAction ExportMLDsaPublicKeyHook { get; set; }
        internal ExportAction ExportMLDsaSecretKeyHook { get; set; }
        internal TryExportFunc TryExportPkcs8PrivateKeyHook { get; set; }
        internal SignAction SignDataHook { get; set; }
        internal VerifyFunc VerifyDataHook { get; set; }
        internal Action<bool> DisposeHook { get; set; }

        private MLDsaTestImplementation(MLDsaAlgorithm algorithm) : base(algorithm)
        {
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCallCount++;
            DisposeHook(disposing);
        }

        protected override void ExportMLDsaPrivateSeedCore(Span<byte> destination)
        {
            ExportMLDsaPrivateSeedCoreCallCount++;
            ExportMLDsaPrivateSeedHook(destination);
        }

        protected override void ExportMLDsaPublicKeyCore(Span<byte> destination)
        {
            ExportMLDsaPublicKeyCoreCallCount++;
            ExportMLDsaPublicKeyHook(destination);
        }

        protected override void ExportMLDsaSecretKeyCore(Span<byte> destination)
        {
            ExportMLDsaSecretKeyCoreCallCount++;
            ExportMLDsaSecretKeyHook(destination);
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            TryExportPkcs8PrivateKeyCoreCallCount++;
            return TryExportPkcs8PrivateKeyHook(destination, out bytesWritten);
        }

        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination)
        {
            SignDataCoreCallCount++;
            SignDataHook(data, context, destination);
        }

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature)
        {
            VerifyDataCoreCallCount++;
            return VerifyDataHook(data, context, signature);
        }

        internal static MLDsaTestImplementation CreateOverriddenCoreMethodsFail(MLDsaAlgorithm algorithm)
        {
            return new MLDsaTestImplementation(algorithm)
            {
                ExportMLDsaPrivateSeedHook = _ => Assert.Fail(),
                ExportMLDsaPublicKeyHook = _ => Assert.Fail(),
                ExportMLDsaSecretKeyHook = _ => Assert.Fail(),
                SignDataHook = (_, _, _) => Assert.Fail(),
                VerifyDataHook = (_, _, _) => { Assert.Fail(); return false; },
                DisposeHook = _ => { },

                TryExportPkcs8PrivateKeyHook = (_, out bytesWritten) =>
                {
                    Assert.Fail();
                    bytesWritten = 0;
                    return false;
                },
            };
        }

        internal static MLDsaTestImplementation CreateNoOp(MLDsaAlgorithm algorithm)
        {
            return new MLDsaTestImplementation(algorithm)
            {
                ExportMLDsaPrivateSeedHook = d => d.Clear(),
                ExportMLDsaPublicKeyHook = d => d.Clear(),
                ExportMLDsaSecretKeyHook = d => d.Clear(),
                SignDataHook = (data, context, destination) => destination.Clear(),
                VerifyDataHook = (data, context, signature) => false,
                DisposeHook = _ => { },

                TryExportPkcs8PrivateKeyHook = (Span<byte> destination, out int bytesWritten) =>
                {
                    destination.Clear();
                    bytesWritten = destination.Length;
                    return true;
                },
            };
        }

        internal static MLDsaTestImplementation Wrap(MLDsa other)
        {
            return new MLDsaTestImplementation(other.Algorithm)
            {
                ExportMLDsaPrivateSeedHook = d => other.ExportMLDsaPrivateSeed(d),
                ExportMLDsaPublicKeyHook = d => other.ExportMLDsaPublicKey(d),
                ExportMLDsaSecretKeyHook = d => other.ExportMLDsaSecretKey(d),
                SignDataHook = (data, context, destination) => other.SignData(data, destination, context),
                VerifyDataHook = (data, context, signature) => other.VerifyData(data, signature, context),
                DisposeHook = _ => other.Dispose(),

                TryExportPkcs8PrivateKeyHook =
                    (Span<byte> destination, out int bytesWritten) =>
                        other.TryExportPkcs8PrivateKey(destination, out bytesWritten),
            };
        }

        public void AddLengthAssertion()
        {
            ExportAction oldExportMLDsaPrivateSeedHook = ExportMLDsaPrivateSeedHook;
            ExportMLDsaPrivateSeedHook = (Span<byte> destination) =>
            {
                oldExportMLDsaPrivateSeedHook(destination);
                Assert.Equal(Algorithm.PrivateSeedSizeInBytes, destination.Length);
            };

            ExportAction oldExportMLDsaPublicKeyHook = ExportMLDsaPublicKeyHook;
            ExportMLDsaPublicKeyHook = (Span<byte> destination) =>
            {
                oldExportMLDsaPublicKeyHook(destination);
                Assert.Equal(Algorithm.PublicKeySizeInBytes, destination.Length);
            };

            ExportAction oldExportMLDsaSecretKeyHook = ExportMLDsaSecretKeyHook;
            ExportMLDsaSecretKeyHook = (Span<byte> destination) =>
            {
                oldExportMLDsaSecretKeyHook(destination);
                Assert.Equal(Algorithm.SecretKeySizeInBytes, destination.Length);
            };

            SignAction oldSignDataHook = SignDataHook;
            SignDataHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                oldSignDataHook(data, context, destination);
                Assert.Equal(Algorithm.SignatureSizeInBytes, destination.Length);
            };

            VerifyFunc oldVerifyDataHook = VerifyDataHook;
            VerifyDataHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            {
                bool ret = oldVerifyDataHook(data, context, signature);
                Assert.Equal(Algorithm.SignatureSizeInBytes, signature.Length);
                return ret;
            };
        }

        public void AddDestinationBufferIsSameAssertion(ReadOnlyMemory<byte> buffer)
        {
            ExportAction oldExportMLDsaPrivateSeedHook = ExportMLDsaPrivateSeedHook;
            ExportMLDsaPrivateSeedHook = (Span<byte> destination) =>
            {
                oldExportMLDsaPrivateSeedHook(destination);
                AssertExtensions.Same(buffer.Span, destination);
            };

            ExportAction oldExportMLDsaPublicKeyHook = ExportMLDsaPublicKeyHook;
            ExportMLDsaPublicKeyHook = (Span<byte> destination) =>
            {
                oldExportMLDsaPublicKeyHook(destination);
                AssertExtensions.Same(buffer.Span, destination);
            };

            ExportAction oldExportMLDsaSecretKeyHook = ExportMLDsaSecretKeyHook;
            ExportMLDsaSecretKeyHook = (Span<byte> destination) =>
            {
                oldExportMLDsaSecretKeyHook(destination);
                AssertExtensions.Same(buffer.Span, destination);
            };

            SignAction oldSignDataHook = SignDataHook;
            SignDataHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                oldSignDataHook(data, context, destination);
                AssertExtensions.Same(buffer.Span, destination);
            };

            TryExportFunc oldTryExportPkcs8PrivateKeyHook = TryExportPkcs8PrivateKeyHook;
            TryExportPkcs8PrivateKeyHook = (Span<byte> destination, out int bytesWritten) =>
            {
                bool ret = oldTryExportPkcs8PrivateKeyHook(destination, out bytesWritten);
                AssertExtensions.Same(buffer.Span, destination);
                return ret;
            };
        }

        public void AddContextBufferIsSameAssertion(ReadOnlyMemory<byte> buffer)
        {
            SignAction oldSignDataHook = SignDataHook;
            SignDataHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                oldSignDataHook(data, context, destination);
                AssertExtensions.Same(buffer.Span, context);
            };

            VerifyFunc oldVerifyDataHook = VerifyDataHook;
            VerifyDataHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            {
                bool ret = oldVerifyDataHook(data, context, signature);
                AssertExtensions.Same(buffer.Span, context);
                return ret;
            };
        }

        public void AddSignatureBufferIsSameAssertion(ReadOnlyMemory<byte> buffer)
        {
            VerifyFunc oldVerifyDataHook = VerifyDataHook;
            VerifyDataHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            {
                bool ret = oldVerifyDataHook(data, context, signature);
                AssertExtensions.Same(buffer.Span, signature);
                return ret;
            };
        }

        public void AddDataBufferIsSameAssertion(ReadOnlyMemory<byte> buffer)
        {
            SignAction oldSignDataHook = SignDataHook;
            SignDataHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                oldSignDataHook(data, context, destination);
                AssertExtensions.Same(buffer.Span, data);
            };

            VerifyFunc oldVerifyDataHook = VerifyDataHook;
            VerifyDataHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            {
                bool ret = oldVerifyDataHook(data, context, signature);
                AssertExtensions.Same(buffer.Span, data);
                return ret;
            };
        }

        public void AddFillDestination(byte b)
        {
            ExportAction oldExportMLDsaPrivateSeedHook = ExportMLDsaPrivateSeedHook;
            ExportMLDsaPrivateSeedHook = (Span<byte> destination) =>
            {
                oldExportMLDsaPrivateSeedHook(destination);
                destination.Fill(b);
            };

            ExportAction oldExportMLDsaPublicKeyHook = ExportMLDsaPublicKeyHook;
            ExportMLDsaPublicKeyHook = (Span<byte> destination) =>
            {
                oldExportMLDsaPublicKeyHook(destination);
                destination.Fill(b);
            };

            ExportAction oldExportMLDsaSecretKeyHook = ExportMLDsaSecretKeyHook;
            ExportMLDsaSecretKeyHook = (Span<byte> destination) =>
            {
                oldExportMLDsaSecretKeyHook(destination);
                destination.Fill(b);
            };

            SignAction oldSignDataHook = SignDataHook;
            SignDataHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                oldSignDataHook(data, context, destination);
                destination.Fill(b);
            };

            TryExportFunc oldTryExportPkcs8PrivateKeyHook = TryExportPkcs8PrivateKeyHook;
            TryExportPkcs8PrivateKeyHook = (Span<byte> destination, out int bytesWritten) =>
            {
                _ = oldTryExportPkcs8PrivateKeyHook(destination, out bytesWritten);
                destination.Fill(b);
                bytesWritten = destination.Length;
                return true;
            };
        }
    }
}

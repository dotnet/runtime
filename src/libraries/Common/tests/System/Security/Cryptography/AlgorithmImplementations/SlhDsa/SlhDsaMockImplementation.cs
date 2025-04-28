// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    /// <summary>
    /// Instrumented derived class for testing the base functionality of <see cref="SlhDsa"/>.
    /// </summary>
    internal sealed class SlhDsaMockImplementation : SlhDsa
    {
        /// <summary>
        /// Creates an instance of SlhDsaTestImplementation with all virtual methods overridden with a call to Assert.Fail
        /// except Dispose.
        /// </summary>
        /// <param name="algorithm">Specifies the algorithm used for the test implementation.</param>
        /// <returns>Returns a configured instance of SlhDsaTestImplementation.</returns>
        internal static SlhDsaMockImplementation Create(SlhDsaAlgorithm algorithm) =>
            new SlhDsaMockImplementation(algorithm);

        public SlhDsaMockImplementation(SlhDsaAlgorithm algorithm)
            : base(algorithm)
        {
        }

        public delegate void ExportSlhDsaPublicKeyCoreAction(Span<byte> s);
        public delegate void ExportSlhDsaSecretKeyCoreAction(Span<byte> s);
        public delegate void SignDataCoreAction(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> s);
        public delegate bool VerifyDataCoreFunc(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature);
        public delegate void DisposeAction(bool disposing);

        public int VerifyDataCoreCallCount = 0;
        public int SignDataCoreCallCount = 0;
        public int ExportSlhDsaPublicKeyCoreCallCount = 0;
        public int ExportSlhDsaSecretKeyCoreCallCount = 0;
        public int DisposeCallCount = 0;

        public ExportSlhDsaPublicKeyCoreAction ExportSlhDsaPublicKeyCoreHook { get; set; } = _ => Assert.Fail();
        public ExportSlhDsaSecretKeyCoreAction ExportSlhDsaSecretKeyCoreHook { get; set; } = _ => Assert.Fail();
        public SignDataCoreAction SignDataCoreHook { get; set; } = (_, _, _) => Assert.Fail();
        public VerifyDataCoreFunc VerifyDataCoreHook { get; set; } = (_, _, _) => { Assert.Fail(); return false; };
        public DisposeAction DisposeHook { get; set; } = _ => { };

        protected override void ExportSlhDsaPublicKeyCore(Span<byte> destination)
        {
            ExportSlhDsaPublicKeyCoreCallCount++;
            ExportSlhDsaPublicKeyCoreHook(destination);
        }

        protected override void ExportSlhDsaSecretKeyCore(Span<byte> destination)
        {
            ExportSlhDsaSecretKeyCoreCallCount++;
            ExportSlhDsaSecretKeyCoreHook(destination);
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCallCount++;
            DisposeHook(disposing);
        }

        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination)
        {
            SignDataCoreCallCount++;
            SignDataCoreHook(data, context, destination);
        }

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature)
        {
            VerifyDataCoreCallCount++;
            return VerifyDataCoreHook(data, context, signature);
        }

        public void AddLengthAssertion()
        {
            ExportSlhDsaPublicKeyCoreAction oldExportSlhDsaPublicKeyCoreHook = ExportSlhDsaPublicKeyCoreHook;
            ExportSlhDsaPublicKeyCoreHook = (Span<byte> destination) =>
            {
                oldExportSlhDsaPublicKeyCoreHook(destination);
                Assert.Equal(Algorithm.PublicKeySizeInBytes, destination.Length);
            };

            ExportSlhDsaSecretKeyCoreAction oldExportSlhDsaSecretKeyCoreHook = ExportSlhDsaSecretKeyCoreHook;
            ExportSlhDsaSecretKeyCoreHook = (Span<byte> destination) =>
            {
                oldExportSlhDsaSecretKeyCoreHook(destination);
                Assert.Equal(Algorithm.SecretKeySizeInBytes, destination.Length);
            };

            SignDataCoreAction oldSignDataCoreHook = SignDataCoreHook;
            SignDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                oldSignDataCoreHook(data, context, destination);
                Assert.Equal(Algorithm.SignatureSizeInBytes, destination.Length);
            };

            VerifyDataCoreFunc oldVerifyDataCoreHook = VerifyDataCoreHook;
            VerifyDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            {
                bool ret = oldVerifyDataCoreHook(data, context, signature);
                Assert.Equal(Algorithm.SignatureSizeInBytes, signature.Length);
                return ret;
            };
        }

        public void AddDestinationBufferIsSameAssertion(ReadOnlyMemory<byte> buffer)
        {
            ExportSlhDsaPublicKeyCoreAction oldExportSlhDsaPublicKeyCoreHook = ExportSlhDsaPublicKeyCoreHook;
            ExportSlhDsaPublicKeyCoreHook = (Span<byte> destination) =>
            {
                oldExportSlhDsaPublicKeyCoreHook(destination);
                AssertExtensions.Same(buffer.Span, destination);
            };

            ExportSlhDsaSecretKeyCoreAction oldExportSlhDsaSecretKeyCoreHook = ExportSlhDsaSecretKeyCoreHook;
            ExportSlhDsaSecretKeyCoreHook = (Span<byte> destination) =>
            {
                oldExportSlhDsaSecretKeyCoreHook(destination);
                AssertExtensions.Same(buffer.Span, destination);
            };

            SignDataCoreAction oldSignDataCoreHook = SignDataCoreHook;
            SignDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                oldSignDataCoreHook(data, context, destination);
                AssertExtensions.Same(buffer.Span, destination);
            };
        }

        public void AddContextBufferIsSameAssertion(ReadOnlyMemory<byte> buffer)
        {
            SignDataCoreAction oldSignDataCoreHook = SignDataCoreHook;
            SignDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                oldSignDataCoreHook(data, context, destination);
                AssertExtensions.Same(buffer.Span, context);
            };

            VerifyDataCoreFunc oldVerifyDataCoreHook = VerifyDataCoreHook;
            VerifyDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            {
                bool ret = oldVerifyDataCoreHook(data, context, signature);
                AssertExtensions.Same(buffer.Span, context);
                return ret;
            };
        }

        public void AddSignatureBufferIsSameAssertion(ReadOnlyMemory<byte> buffer)
        {
            VerifyDataCoreFunc oldVerifyDataCoreHook = VerifyDataCoreHook;
            VerifyDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            {
                bool ret = oldVerifyDataCoreHook(data, context, signature);
                AssertExtensions.Same(buffer.Span, signature);
                return ret;
            };
        }

        public void AddDataBufferIsSameAssertion(ReadOnlyMemory<byte> buffer)
        {
            SignDataCoreAction oldSignDataCoreHook = SignDataCoreHook;
            SignDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                oldSignDataCoreHook(data, context, destination);
                AssertExtensions.Same(buffer.Span, data);
            };

            VerifyDataCoreFunc oldVerifyDataCoreHook = VerifyDataCoreHook;
            VerifyDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            {
                bool ret = oldVerifyDataCoreHook(data, context, signature);
                AssertExtensions.Same(buffer.Span, data);
                return ret;
            };
        }

        public void AddFillDestination(byte b)
        {
            ExportSlhDsaPublicKeyCoreAction oldExportSlhDsaPublicKeyCoreHook = ExportSlhDsaPublicKeyCoreHook;
            ExportSlhDsaPublicKeyCoreHook = (Span<byte> destination) =>
            {
                oldExportSlhDsaPublicKeyCoreHook(destination);
                destination.Fill(b);
            };

            ExportSlhDsaSecretKeyCoreAction oldExportSlhDsaSecretKeyCoreHook = ExportSlhDsaSecretKeyCoreHook;
            ExportSlhDsaSecretKeyCoreHook = (Span<byte> destination) =>
            {
                oldExportSlhDsaSecretKeyCoreHook(destination);
                destination.Fill(b);
            };

            SignDataCoreAction oldSignDataCoreHook = SignDataCoreHook;
            SignDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                oldSignDataCoreHook(data, context, destination);
                destination.Fill(b);
            };
        }
    }
}

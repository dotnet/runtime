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
        /// Creates an instance of SlhDsaTestImplementation with all virtual methods overridden with a call to Assert.Fail.
        /// </summary>
        /// <param name="algorithm">Specifies the algorithm used for the test implementation.</param>
        /// <returns>Returns a configured instance of SlhDsaTestImplementation.</returns>
        internal static SlhDsaMockImplementation CreateOverriddenCoreMethodsFail(SlhDsaAlgorithm algorithm)
        {
            return new SlhDsaMockImplementation(algorithm)
            {
                ExportSlhDsaPublicKeyCoreHook = _ => Assert.Fail(),
                ExportSlhDsaSecretKeyCoreHook = _ => Assert.Fail(),
                TryExportPkcs8PrivateKeyCoreHook = (destination, out bytesWritten) =>
                {
                    Assert.Fail();
                    bytesWritten = 0;
                    return false;
                },
                SignDataCoreHook = (_, _, _) => Assert.Fail(),
                VerifyDataCoreHook = (_, _, _) => { Assert.Fail(); return default; },
            };
        }

        public SlhDsaMockImplementation(SlhDsaAlgorithm algorithm)
            : base(algorithm)
        {
        }

        internal delegate void ExportSlhDsaPublicKeyCoreAction(Span<byte> s);
        public ExportSlhDsaPublicKeyCoreAction ExportSlhDsaPublicKeyCoreHook { get; set; } = _ => { };
        internal delegate void ExportSlhDsaSecretKeyCoreAction(Span<byte> s);
        public ExportSlhDsaSecretKeyCoreAction ExportSlhDsaSecretKeyCoreHook { get; set; } = _ => { };
        internal delegate bool TryExportPkcs8PrivateKeyCoreFunc(Span<byte> destination, out int bytesWritten);
        public TryExportPkcs8PrivateKeyCoreFunc TryExportPkcs8PrivateKeyCoreHook { get; set; } = (_, out bytesWritten) => { bytesWritten = 0; return false; };
        internal delegate void SignDataCoreAction(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> s);
        public SignDataCoreAction SignDataCoreHook { get; set; } = (_, _, _) => { };
        internal delegate bool VerifyDataCoreFunc(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature);
        public VerifyDataCoreFunc VerifyDataCoreHook { get; set; } = (_, _, _) => false;
        internal delegate void DisposeAction(bool disposing);
        public DisposeAction DisposeHook { get; set; } = _ => { };

        protected override void ExportSlhDsaPublicKeyCore(Span<byte> destination) => ExportSlhDsaPublicKeyCoreHook(destination);

        protected override void ExportSlhDsaSecretKeyCore(Span<byte> destination) => ExportSlhDsaSecretKeyCoreHook(destination);
        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten) => TryExportPkcs8PrivateKeyCoreHook(destination, out bytesWritten);

        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) => SignDataCoreHook(data, context, destination);

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) => VerifyDataCoreHook(data, context, signature);

        protected override void Dispose(bool disposing) => DisposeHook(disposing);
    }
}

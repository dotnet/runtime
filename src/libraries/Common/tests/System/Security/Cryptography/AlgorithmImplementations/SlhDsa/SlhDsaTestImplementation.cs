// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    /// <summary>
    /// Instrumented derived class for testing the base functionality of <see cref="SlhDsa"/>.
    /// </summary>
    internal sealed class SlhDsaTestImplementation : SlhDsa
    {
        /// <summary>
        /// Creates an instance of SlhDsaTestImplementation with all virtual methods overridden with a call to Assert.Fail.
        /// </summary>
        /// <param name="algorithm">Specifies the algorithm used for the test implementation.</param>
        /// <returns>Returns a configured instance of SlhDsaTestImplementation.</returns>
        internal static SlhDsaTestImplementation CreateOverriddenCoreMethodsFail(SlhDsaAlgorithm algorithm)
        {
            return new SlhDsaTestImplementation(algorithm)
            {
                ExportSlhDsaPrivateSeedCoreHook = _ => Assert.Fail(),
                ExportSlhDsaPublicKeyCoreHook = _ => Assert.Fail(),
                ExportSlhDsaSecretKeyCoreHook = _ => Assert.Fail(),
                SignDataCoreHook = (_, _, _) => Assert.Fail(),
                VerifyDataCoreHook = (_, _, _) => { Assert.Fail(); return default; },
            };
        }

        public SlhDsaTestImplementation(SlhDsaAlgorithm algorithm)
            : base(algorithm)
        {
        }

        public Action<Span<byte>> ExportSlhDsaPrivateSeedCoreHook { get; set; } = _ => { };
        protected override void ExportSlhDsaPrivateSeedCore(Span<byte> destination) => ExportSlhDsaPrivateSeedCoreHook(destination);

        public Action<Span<byte>> ExportSlhDsaPublicKeyCoreHook { get; set; } = _ => { };
        protected override void ExportSlhDsaPublicKeyCore(Span<byte> destination) => ExportSlhDsaPublicKeyCoreHook(destination);

        public Action<Span<byte>> ExportSlhDsaSecretKeyCoreHook { get; set; } = _ => { };
        protected override void ExportSlhDsaSecretKeyCore(Span<byte> destination) => ExportSlhDsaSecretKeyCoreHook(destination);

        public Action<ReadOnlySpan<byte>, ReadOnlySpan<byte>, Span<byte>> SignDataCoreHook { get; set; } = (_, _, _) => { };
        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) => SignDataCoreHook(data, context, destination);

        public Func<ReadOnlySpan<byte>, ReadOnlySpan<byte>, ReadOnlySpan<byte>, bool> VerifyDataCoreHook { get; set; } = (_, _, _) => false;
        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) => VerifyDataCoreHook(data, context, signature);

        public Action<bool> DisposeHook { get; set; } = _ => { };
        protected override void Dispose(bool disposing) => DisposeHook(disposing);
    }
}

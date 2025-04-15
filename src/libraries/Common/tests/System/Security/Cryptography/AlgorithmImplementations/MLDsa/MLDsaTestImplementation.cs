// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    internal sealed class MLDsaTestImplementation : MLDsa
    {
        internal delegate void ExportAction(Span<byte> destination);
        internal delegate void SignAction(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination);
        internal delegate bool VerifyFunc(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature);

        internal ExportAction ExportMLDsaPrivateSeedHook { get; set; }
        internal ExportAction ExportMLDsaPublicKeyHook { get; set; }
        internal ExportAction ExportMLDsaSecretKeyHook { get; set; }
        internal SignAction SignDataHook { get; set; }
        internal VerifyFunc VerifyDataHook { get; set; }
        internal Action<bool> DisposeHook { get; set; } = _ => { };

        private MLDsaTestImplementation(MLDsaAlgorithm algorithm) : base(algorithm)
        {
        }

        protected override void Dispose(bool disposing) => DisposeHook(disposing);

        protected override void ExportMLDsaPrivateSeedCore(Span<byte> destination) => ExportMLDsaPrivateSeedHook(destination);
        protected override void ExportMLDsaPublicKeyCore(Span<byte> destination) => ExportMLDsaPublicKeyHook(destination);
        protected override void ExportMLDsaSecretKeyCore(Span<byte> destination) => ExportMLDsaSecretKeyHook(destination);

        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            SignDataHook(data, context, destination);

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            VerifyDataHook(data, context, signature);

        internal static MLDsaTestImplementation CreateOverriddenCoreMethodsFail(MLDsaAlgorithm algorithm)
        {
            return new MLDsaTestImplementation(algorithm)
            {
                ExportMLDsaPrivateSeedHook = _ => Assert.Fail(),
                ExportMLDsaPublicKeyHook = _ => Assert.Fail(),
                ExportMLDsaSecretKeyHook = _ => Assert.Fail(),
                SignDataHook = (_, _, _) => Assert.Fail(),
                VerifyDataHook = (_, _, _) => { Assert.Fail(); return false; },
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
                VerifyDataHook = (data, context, signature) => signature.IndexOfAnyExcept((byte)0) == -1,
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
            };
        }
    }
}

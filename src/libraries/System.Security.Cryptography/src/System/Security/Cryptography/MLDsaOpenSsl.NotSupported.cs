// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    public sealed partial class MLDsaOpenSsl : MLDsa
    {
        private static partial MLDsaAlgorithm AlgorithmFromHandle(
            SafeEvpPKeyHandle pkeyHandle,
            out SafeEvpPKeyHandle upRefHandle,
            out bool hasSeed,
            out bool hasPrivateKey)
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

        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override void SignPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, Span<byte> destination)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override bool VerifyPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, ReadOnlySpan<byte> signature)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override void SignMuCore(ReadOnlySpan<byte> externalMu, Span<byte> destination)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override bool VerifyMuCore(ReadOnlySpan<byte> externalMu, ReadOnlySpan<byte> signature)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override void ExportMLDsaPublicKeyCore(Span<byte> destination)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override void ExportMLDsaPrivateKeyCore(Span<byte> destination)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override void ExportMLDsaPrivateSeedCore(Span<byte> destination)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }
    }
}

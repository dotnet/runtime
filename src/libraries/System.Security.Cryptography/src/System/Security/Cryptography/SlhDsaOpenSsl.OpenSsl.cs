// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class SlhDsaOpenSsl
    {
        public partial SafeEvpPKeyHandle DuplicateKeyHandle()
        {
            ThrowIfDisposed();
            return _key.DuplicateHandle();
        }

        private static partial SlhDsaAlgorithm AlgorithmFromHandle(SafeEvpPKeyHandle pkeyHandle, out SafeEvpPKeyHandle upRefHandle)
        {
            ArgumentNullException.ThrowIfNull(pkeyHandle);

            if (pkeyHandle.IsInvalid)
            {
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(pkeyHandle));
            }

            upRefHandle = pkeyHandle.DuplicateHandle();

            try
            {
                Interop.Crypto.PalSlhDsaAlgorithmId slhDsaAlgorithmId = Interop.Crypto.GetSlhDsaAlgorithmId(upRefHandle);

                switch (slhDsaAlgorithmId)
                {
                    case Interop.Crypto.PalSlhDsaAlgorithmId.SlhDsaSha2_128s:
                        return SlhDsaAlgorithm.SlhDsaSha2_128s;
                    case Interop.Crypto.PalSlhDsaAlgorithmId.SlhDsaShake128s:
                        return SlhDsaAlgorithm.SlhDsaShake128s;
                    case Interop.Crypto.PalSlhDsaAlgorithmId.SlhDsaSha2_128f:
                        return SlhDsaAlgorithm.SlhDsaSha2_128f;
                    case Interop.Crypto.PalSlhDsaAlgorithmId.SlhDsaShake128f:
                        return SlhDsaAlgorithm.SlhDsaShake128f;
                    case Interop.Crypto.PalSlhDsaAlgorithmId.SlhDsaSha2_192s:
                        return SlhDsaAlgorithm.SlhDsaSha2_192s;
                    case Interop.Crypto.PalSlhDsaAlgorithmId.SlhDsaShake192s:
                        return SlhDsaAlgorithm.SlhDsaShake192s;
                    case Interop.Crypto.PalSlhDsaAlgorithmId.SlhDsaSha2_192f:
                        return SlhDsaAlgorithm.SlhDsaSha2_192f;
                    case Interop.Crypto.PalSlhDsaAlgorithmId.SlhDsaShake192f:
                        return SlhDsaAlgorithm.SlhDsaShake192f;
                    case Interop.Crypto.PalSlhDsaAlgorithmId.SlhDsaSha2_256s:
                        return SlhDsaAlgorithm.SlhDsaSha2_256s;
                    case Interop.Crypto.PalSlhDsaAlgorithmId.SlhDsaShake256s:
                        return SlhDsaAlgorithm.SlhDsaShake256s;
                    case Interop.Crypto.PalSlhDsaAlgorithmId.SlhDsaSha2_256f:
                        return SlhDsaAlgorithm.SlhDsaSha2_256f;
                    case Interop.Crypto.PalSlhDsaAlgorithmId.SlhDsaShake256f:
                        return SlhDsaAlgorithm.SlhDsaShake256f;
                    default:
                        throw new CryptographicException(SR.Cryptography_SlhDsaInvalidAlgorithmHandle);
                }
            }
            catch
            {
                upRefHandle.Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _key.Dispose();
            }
        }

        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            Interop.Crypto.SlhDsaSignPure(_key, data, context, destination);

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            Interop.Crypto.SlhDsaVerifyPure(_key, data, context, signature);

        protected override void SignPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, Span<byte> destination) =>
            Helpers.SlhDsaPreHash(
                hash,
                context,
                hashAlgorithmOid,
                _key,
                destination,
                static (key, encodedMessage, destination) =>
                {
                    Interop.Crypto.SlhDsaSignPreEncoded(key, encodedMessage, destination);
                    return true;
                });

        protected override bool VerifyPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, ReadOnlySpan<byte> signature) =>
            Helpers.SlhDsaPreHash(
                hash,
                context,
                hashAlgorithmOid,
                _key,
                signature,
                static (key, encodedMessage, signature) => Interop.Crypto.SlhDsaVerifyPreEncoded(key, encodedMessage, signature));

        protected override void ExportSlhDsaPublicKeyCore(Span<byte> destination) =>
            Interop.Crypto.SlhDsaExportPublicKey(_key, destination);

        protected override void ExportSlhDsaSecretKeyCore(Span<byte> destination) =>
            Interop.Crypto.SlhDsaExportSecretKey(_key, destination);
    }
}

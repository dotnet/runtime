// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography.Cose
{
    public sealed class CoseSignature
    {
        private readonly byte[] _encodedBodyProtectedHeaders;
        internal readonly byte[] _encodedSignProtectedHeaders;
        internal readonly byte[] _signature;
        private CoseMultiSignMessage? _message;

        public CoseHeaderMap ProtectedHeaders { get; }
        public CoseHeaderMap UnprotectedHeaders { get; }


        internal CoseSignature(CoseMultiSignMessage message, CoseHeaderMap protectedHeaders, CoseHeaderMap unprotectedHeaders, byte[] encodedBodyProtectedHeaders, byte[] encodedSignProtectedHeaders, byte[] signature)
            : this(protectedHeaders, unprotectedHeaders, encodedBodyProtectedHeaders, encodedSignProtectedHeaders, signature)
        {
            Message = message;
        }

        internal CoseSignature(CoseHeaderMap protectedHeaders, CoseHeaderMap unprotectedHeaders, byte[] encodedBodyProtectedHeaders, byte[] encodedSignProtectedHeaders, byte[] signature)
        {
            ProtectedHeaders = protectedHeaders;
            UnprotectedHeaders = unprotectedHeaders;
            _encodedBodyProtectedHeaders = encodedBodyProtectedHeaders;
            _encodedSignProtectedHeaders = encodedSignProtectedHeaders;
            _signature = signature;
        }
        internal CoseMultiSignMessage Message
        {
            get
            {
                Debug.Assert(_message != null);
                return _message;
            }
            set
            {
                _message = value;
            }
        }

        public bool VerifyEmbedded(AsymmetricAlgorithm key, ReadOnlySpan<byte> associatedData)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (Message.IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasDetached);
            }

            return VerifyCore(key, Message.Content.Value.Span, null, associatedData, CoseHelpers.GetKeyType(key));
        }

        public bool VerifyEmbedded(AsymmetricAlgorithm key, byte[]? associatedData = null)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (Message.IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasDetached);
            }

            return VerifyCore(key, Message.Content.Value.Span, null, associatedData, CoseHelpers.GetKeyType(key));
        }

        public bool VerifyDetached(AsymmetricAlgorithm key, byte[] detachedContent, byte[]? associatedData = null)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (detachedContent is null)
            {
                throw new ArgumentNullException(nameof(detachedContent));
            }

            if (!Message.IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasEmbedded);
            }

            return VerifyCore(key, detachedContent, null, associatedData, CoseHelpers.GetKeyType(key));
        }

        public bool VerifyDetached(AsymmetricAlgorithm key, ReadOnlySpan<byte> detachedContent, ReadOnlySpan<byte> associatedData = default)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (!Message.IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasEmbedded);
            }

            return VerifyCore(key, detachedContent, null, associatedData, CoseHelpers.GetKeyType(key));
        }

        public bool VerifyDetached(AsymmetricAlgorithm key, Stream detachedContent, ReadOnlySpan<byte> associatedData = default)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (detachedContent is null)
            {
                throw new ArgumentNullException(nameof(detachedContent));
            }

            if (!detachedContent.CanRead)
            {
                throw new ArgumentException(SR.Sign1ArgumentStreamNotReadable, nameof(detachedContent));
            }

            if (!detachedContent.CanSeek)
            {
                throw new ArgumentException(SR.Sign1ArgumentStreamNotSeekable, nameof(detachedContent));
            }

            if (!Message.IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasEmbedded);
            }

            return VerifyCore(key, default, detachedContent, associatedData, CoseHelpers.GetKeyType(key));
        }

        public Task<bool> VerifyDetachedAsync(AsymmetricAlgorithm key, Stream detachedContent, ReadOnlyMemory<byte> associatedData = default, CancellationToken cancellationToken = default)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (detachedContent is null)
            {
                throw new ArgumentNullException(nameof(detachedContent));
            }

            if (!detachedContent.CanRead)
            {
                throw new ArgumentException(SR.Sign1ArgumentStreamNotReadable, nameof(detachedContent));
            }

            if (!detachedContent.CanSeek)
            {
                throw new ArgumentException(SR.Sign1ArgumentStreamNotSeekable, nameof(detachedContent));
            }

            if (!Message.IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasEmbedded);
            }

            return VerifyAsyncCore(key, detachedContent, associatedData, CoseHelpers.GetKeyType(key), cancellationToken);
        }

        private async Task<bool> VerifyAsyncCore(AsymmetricAlgorithm key, Stream content, ReadOnlyMemory<byte> associatedData, KeyType keyType, CancellationToken cancellationToken)
        {
            ReadOnlyMemory<byte> encodedAlg = CoseHelpers.GetCoseAlgorithmFromProtectedHeaders(ProtectedHeaders);

            int? nullableAlg = CoseHelpers.DecodeCoseAlgorithmHeader(encodedAlg);
            if (nullableAlg == null)
            {
                throw new CryptographicException(SR.Sign1VerifyAlgHeaderWasIncorrect);
            }

            HashAlgorithmName hashAlgorithm = CoseHelpers.GetHashAlgorithmFromCoseAlgorithmAndKeyType(nullableAlg.Value, keyType, out RSASignaturePadding? padding);

            using (IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithm))
            {
                int bufferLength = CoseMessage.ComputeToBeSignedEncodedSize(
                    SigStructureContext.Signature,
                    _encodedBodyProtectedHeaders.Length,
                    _encodedSignProtectedHeaders.Length,
                    associatedData.Length,
                    contentLength: 0);
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferLength);

                try
                {
                    await CoseMessage.AppendToBeSignedAsync(buffer, hasher, SigStructureContext.Signature, _encodedBodyProtectedHeaders, _encodedSignProtectedHeaders, associatedData, content, cancellationToken).ConfigureAwait(false);
                    return VerifyHash(key, hasher, hashAlgorithm, keyType, padding);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
                }
            }
        }

        private bool VerifyCore(AsymmetricAlgorithm key, ReadOnlySpan<byte> contentBytes, Stream? contentStream, ReadOnlySpan<byte> associatedData, KeyType keyType)
        {
            ReadOnlyMemory<byte> encodedAlg = CoseHelpers.GetCoseAlgorithmFromProtectedHeaders(ProtectedHeaders);
            int? nullableAlg = CoseHelpers.DecodeCoseAlgorithmHeader(encodedAlg);
            if (nullableAlg == null)
            {
                throw new CryptographicException(SR.Sign1VerifyAlgHeaderWasIncorrect);
            }

            HashAlgorithmName hashAlgorithm = CoseHelpers.GetHashAlgorithmFromCoseAlgorithmAndKeyType(nullableAlg.Value, keyType, out RSASignaturePadding? padding);
            using (IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithm))
            {
                int bufferLength = CoseMessage.ComputeToBeSignedEncodedSize(
                    SigStructureContext.Signature,
                    _encodedBodyProtectedHeaders.Length,
                    _encodedSignProtectedHeaders.Length,
                    associatedData.Length,
                    contentLength: 0);
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferLength);

                try
                {
                    CoseMessage.AppendToBeSigned(buffer, hasher, SigStructureContext.Signature, _encodedBodyProtectedHeaders, _encodedSignProtectedHeaders, associatedData, contentBytes, contentStream);
                    return VerifyHash(key, hasher, hashAlgorithm, keyType, padding);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
                }
            }
        }

        private bool VerifyHash(AsymmetricAlgorithm key, IncrementalHash hasher, HashAlgorithmName hashAlgorithm, KeyType keyType, RSASignaturePadding? padding)
        {
#if NETCOREAPP
            Debug.Assert(hasher.HashLengthInBytes <= 512 / 8); // largest hash we can get (SHA512).
            Span<byte> hash = stackalloc byte[hasher.HashLengthInBytes];
            hasher.GetHashAndReset(hash);
#else
            byte[] hash = hasher.GetHashAndReset();
#endif
            if (keyType == KeyType.ECDsa)
            {
                var ecdsa = (ECDsa)key;
                return ecdsa.VerifyHash(hash, _signature);
            }
            else
            {
                Debug.Assert(keyType == KeyType.RSA);
                Debug.Assert(padding != null);
                var rsa = (RSA)key;
                return rsa.VerifyHash(hash, _signature, hashAlgorithm, padding);
            }
        }
    }
}

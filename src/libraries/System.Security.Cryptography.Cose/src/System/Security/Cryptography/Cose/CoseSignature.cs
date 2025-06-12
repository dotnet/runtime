// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography.Cose
{
    /// <summary>
    /// Represents a COSE_Signature that carries one signature and information about that signature associated with a <see cref="CoseMultiSignMessage"/>.
    /// </summary>
    public sealed class CoseSignature
    {
        internal readonly byte[] _encodedSignProtectedHeaders;
        internal readonly byte[] _signature;
        private CoseMultiSignMessage? _message;

        /// <summary>
        /// Gets the protected header parameters associated with this instance.
        /// </summary>
        /// <value>A collection of protected header parameters associated with this instance.</value>
        public CoseHeaderMap ProtectedHeaders { get; }

        /// <summary>
        /// Gets the unprotected header parameters associated with this instance.
        /// </summary>
        /// <value>A collection of unprotected header parameters associated with this instance.</value>
        public CoseHeaderMap UnprotectedHeaders { get; }

        /// <summary>
        /// Gets the raw bytes of the protected header parameters associated with this instance.
        /// </summary>
        /// <value>A region of memory that contains the raw bytes of the protected header parameters associated with this instance.</value>
        public ReadOnlyMemory<byte> RawProtectedHeaders => _encodedSignProtectedHeaders;

        /// <summary>
        /// Gets the digital signature.
        /// </summary>
        /// <value>A region of memory that contains the digital signature.</value>
        public ReadOnlyMemory<byte> Signature => _signature;

        internal CoseSignature(CoseMultiSignMessage message, CoseHeaderMap protectedHeaders, CoseHeaderMap unprotectedHeaders, byte[] encodedSignProtectedHeaders, byte[] signature)
            : this(protectedHeaders, unprotectedHeaders, encodedSignProtectedHeaders, signature)
        {
            Message = message;
        }

        internal CoseSignature(CoseHeaderMap protectedHeaders, CoseHeaderMap unprotectedHeaders, byte[] encodedSignProtectedHeaders, byte[] signature)
        {
            ProtectedHeaders = protectedHeaders;
            UnprotectedHeaders = unprotectedHeaders;
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

        /// <summary>
        /// Verifies that the signature is valid for the message's content using the specified key.
        /// </summary>
        /// <param name="key">The private key used to sign the content.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must match the value provided during signing.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is of an unsupported type.</exception>
        /// <exception cref="InvalidOperationException">The content is detached from the associated message, use an overload that accepts a detached content.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <see cref="CoseMessage.ProtectedHeaders"/> does not have a value for the <see cref="CoseHeaderLabel.Algorithm"/> header.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was incorrectly formatted.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was not one of the values supported by this implementation.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header doesn't match with the algorithms supported by the specified <paramref name="key"/>.
        ///   </para>
        /// </exception>
        /// <seealso cref="VerifyDetached(AsymmetricAlgorithm, ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
        /// <seealso cref="CoseMessage.Content"/>
        public bool VerifyEmbedded(AsymmetricAlgorithm key, ReadOnlySpan<byte> associatedData)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (Message.IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasDetached);
            }

            CoseAlgorithm coseAlgorithm = GetCoseAlgorithmFromProtectedHeaders();
            CoseKey coseKey = CoseKey.FromUntrustedAlgorithmAndKey(coseAlgorithm, key);

            return VerifyCore(coseKey, Message.Content.Value.Span, null, associatedData);
        }

        /// <summary>
        /// Verifies that the signature is valid for the message's content using the specified key.
        /// </summary>
        /// <param name="key">The private key used to sign the content.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must match the value provided during signing.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is of an unsupported type.</exception>
        /// <exception cref="InvalidOperationException">The content is detached from the associated message, use an overload that accepts a detached content.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <see cref="CoseMessage.ProtectedHeaders"/> does not have a value for the <see cref="CoseHeaderLabel.Algorithm"/> header.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was incorrectly formatted.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was not one of the values supported by this implementation.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header doesn't match with the algorithms supported by the specified <paramref name="key"/>.
        ///   </para>
        /// </exception>
        /// <seealso cref="VerifyDetached(CoseKey, ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
        /// <seealso cref="CoseMessage.Content"/>
        public bool VerifyEmbedded(CoseKey key, ReadOnlySpan<byte> associatedData = default)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (Message.IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasDetached);
            }

            return VerifyCore(key, Message.Content.Value.Span, null, associatedData);
        }

        /// <summary>
        /// Verifies that the signature is valid for the message's content using the specified key.
        /// </summary>
        /// <param name="key">The private key used to sign the content.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must match the value provided during signing.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is of an unsupported type.</exception>
        /// <exception cref="InvalidOperationException">The content is detached from the associated message, use an overload that accepts a detached content.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <see cref="CoseMessage.ProtectedHeaders"/> does not have a value for the <see cref="CoseHeaderLabel.Algorithm"/> header.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was incorrectly formatted.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was not one of the values supported by this implementation.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header doesn't match with the algorithms supported by the specified <paramref name="key"/>.
        ///   </para>
        /// </exception>
        /// <seealso cref="VerifyDetached(AsymmetricAlgorithm, byte[], byte[])"/>
        /// <seealso cref="CoseMessage.Content"/>
        public bool VerifyEmbedded(AsymmetricAlgorithm key, byte[]? associatedData = null)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (Message.IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasDetached);
            }

            CoseAlgorithm coseAlgorithm = GetCoseAlgorithmFromProtectedHeaders();
            CoseKey coseKey = CoseKey.FromUntrustedAlgorithmAndKey(coseAlgorithm, key);

            return VerifyCore(coseKey, Message.Content.Value.Span, null, associatedData);
        }

        /// <summary>
        /// Verifies that the signature is valid for the message's content using the specified key.
        /// </summary>
        /// <param name="key">The private key used to sign the content.</param>
        /// <param name="detachedContent">The content that was previously signed.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must match the value provided during signing.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="detachedContent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is of an unsupported type.</exception>
        /// <exception cref="InvalidOperationException">The content is embedded on the associated message, use an overload that uses embedded content.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <see cref="CoseMessage.ProtectedHeaders"/> does not have a value for the <see cref="CoseHeaderLabel.Algorithm"/> header.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was incorrectly formatted.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was not one of the values supported by this implementation.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header doesn't match with the algorithms supported by the specified <paramref name="key"/>.
        ///   </para>
        /// </exception>
        /// <seealso cref="VerifyEmbedded(AsymmetricAlgorithm, byte[])"/>
        /// <seealso cref="CoseMessage.Content"/>
        public bool VerifyDetached(AsymmetricAlgorithm key, byte[] detachedContent, byte[]? associatedData = null)
        {
            ArgumentNullException.ThrowIfNull(key);

            ArgumentNullException.ThrowIfNull(detachedContent);

            if (!Message.IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasEmbedded);
            }

            CoseAlgorithm coseAlgorithm = GetCoseAlgorithmFromProtectedHeaders();
            CoseKey coseKey = CoseKey.FromUntrustedAlgorithmAndKey(coseAlgorithm, key);

            return VerifyCore(coseKey, detachedContent, null, associatedData);
        }

        /// <summary>
        /// Verifies that the signature is valid for the message's content using the specified key.
        /// </summary>
        /// <param name="key">The private key used to sign the content.</param>
        /// <param name="detachedContent">The content that was previously signed.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must match the value provided during signing.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is of an unsupported type.</exception>
        /// <exception cref="InvalidOperationException">The content is embedded on the associated message, use an overload that uses embedded content.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <see cref="CoseMessage.ProtectedHeaders"/> does not have a value for the <see cref="CoseHeaderLabel.Algorithm"/> header.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was incorrectly formatted.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was not one of the values supported by this implementation.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header doesn't match with the algorithms supported by the specified <paramref name="key"/>.
        ///   </para>
        /// </exception>
        /// <seealso cref="VerifyEmbedded(AsymmetricAlgorithm, ReadOnlySpan{byte})"/>
        /// <seealso cref="CoseMessage.Content"/>
        public bool VerifyDetached(AsymmetricAlgorithm key, ReadOnlySpan<byte> detachedContent, ReadOnlySpan<byte> associatedData = default)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (!Message.IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasEmbedded);
            }

            CoseAlgorithm coseAlgorithm = GetCoseAlgorithmFromProtectedHeaders();
            CoseKey coseKey = CoseKey.FromUntrustedAlgorithmAndKey(coseAlgorithm, key);

            return VerifyCore(coseKey, detachedContent, null, associatedData);
        }

        /// <summary>
        /// Verifies that the signature is valid for the message's content using the specified key.
        /// </summary>
        /// <param name="key">The private key used to sign the content.</param>
        /// <param name="detachedContent">The content that was previously signed.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must match the value provided during signing.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is of an unsupported type.</exception>
        /// <exception cref="InvalidOperationException">The content is embedded on the associated message, use an overload that uses embedded content.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <see cref="CoseMessage.ProtectedHeaders"/> does not have a value for the <see cref="CoseHeaderLabel.Algorithm"/> header.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was incorrectly formatted.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was not one of the values supported by this implementation.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header doesn't match with the algorithms supported by the specified <paramref name="key"/>.
        ///   </para>
        /// </exception>
        /// <seealso cref="VerifyEmbedded(CoseKey, ReadOnlySpan{byte})"/>
        /// <seealso cref="CoseMessage.Content"/>
        public bool VerifyDetached(CoseKey key, ReadOnlySpan<byte> detachedContent, ReadOnlySpan<byte> associatedData = default)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (!Message.IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasEmbedded);
            }

            return VerifyCore(key, detachedContent, null, associatedData);
        }

        /// <summary>
        /// Verifies that the signature is valid for the message's content using the specified key.
        /// </summary>
        /// <param name="key">The private key used to sign the content.</param>
        /// <param name="detachedContent">The content that was previously signed.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must match the value provided during signing.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="detachedContent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="key"/> is of an unsupported type.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="detachedContent"/> does not support reading or seeking.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">The content is embedded on the associated message, use an overload that uses embedded content.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <see cref="CoseMessage.ProtectedHeaders"/> does not have a value for the <see cref="CoseHeaderLabel.Algorithm"/> header.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was incorrectly formatted.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was not one of the values supported by this implementation.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header doesn't match with the algorithms supported by the specified <paramref name="key"/>.
        ///   </para>
        /// </exception>
        /// <seealso cref="VerifyDetachedAsync(AsymmetricAlgorithm, Stream, ReadOnlyMemory{byte}, CancellationToken)"/>
        /// <seealso cref="CoseMessage.Content"/>
        public bool VerifyDetached(AsymmetricAlgorithm key, Stream detachedContent, ReadOnlySpan<byte> associatedData = default)
        {
            ArgumentNullException.ThrowIfNull(key);

            ArgumentNullException.ThrowIfNull(detachedContent);

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

            CoseAlgorithm coseAlgorithm = GetCoseAlgorithmFromProtectedHeaders();
            CoseKey coseKey = CoseKey.FromUntrustedAlgorithmAndKey(coseAlgorithm, key);

            return VerifyCore(coseKey, default, detachedContent, associatedData);
        }

        /// <summary>
        /// Verifies that the signature is valid for the message's content using the specified key.
        /// </summary>
        /// <param name="key">The private key used to sign the content.</param>
        /// <param name="detachedContent">The content that was previously signed.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must match the value provided during signing.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="detachedContent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="key"/> is of an unsupported type.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="detachedContent"/> does not support reading or seeking.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">The content is embedded on the associated message, use an overload that uses embedded content.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <see cref="CoseMessage.ProtectedHeaders"/> does not have a value for the <see cref="CoseHeaderLabel.Algorithm"/> header.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was incorrectly formatted.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was not one of the values supported by this implementation.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header doesn't match with the algorithms supported by the specified <paramref name="key"/>.
        ///   </para>
        /// </exception>
        /// <seealso cref="CoseMessage.Content"/>
        public bool VerifyDetached(CoseKey key, Stream detachedContent, ReadOnlySpan<byte> associatedData = default)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(detachedContent);

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

            return VerifyCore(key, default, detachedContent, associatedData);
        }

        /// <summary>
        /// Asynchronously verifies that the signature is valid for the message's content using the specified key.
        /// </summary>
        /// <param name="key">The private key used to sign the content.</param>
        /// <param name="detachedContent">The content that was previously signed.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must match the value provided during signing.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A task whose <see cref="Task{TResult}"/> property is <see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="detachedContent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="key"/> is of an unsupported type.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="detachedContent"/> does not support reading or seeking.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">The content is embedded on the associated message, use an overload that uses embedded content.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <see cref="CoseMessage.ProtectedHeaders"/> does not have a value for the <see cref="CoseHeaderLabel.Algorithm"/> header.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was incorrectly formatted.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was not one of the values supported by this implementation.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header doesn't match with the algorithms supported by the specified <paramref name="key"/>.
        ///   </para>
        /// </exception>
        /// <seealso cref="VerifyDetached(AsymmetricAlgorithm, Stream, ReadOnlySpan{byte})"/>
        /// <seealso cref="CoseMessage.Content"/>
        public Task<bool> VerifyDetachedAsync(AsymmetricAlgorithm key, Stream detachedContent, ReadOnlyMemory<byte> associatedData = default, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(detachedContent);

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

            CoseAlgorithm coseAlgorithm = GetCoseAlgorithmFromProtectedHeaders();
            CoseKey coseKey = CoseKey.FromUntrustedAlgorithmAndKey(coseAlgorithm, key);

            return VerifyAsyncCore(coseKey, detachedContent, associatedData, cancellationToken);
        }

        /// <summary>
        /// Asynchronously verifies that the signature is valid for the message's content using the specified key.
        /// </summary>
        /// <param name="key">The private key used to sign the content.</param>
        /// <param name="detachedContent">The content that was previously signed.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must match the value provided during signing.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A task whose <see cref="Task{TResult}"/> property is <see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="detachedContent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="key"/> is of an unsupported type.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="detachedContent"/> does not support reading or seeking.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">The content is embedded on the associated message, use an overload that uses embedded content.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <see cref="CoseMessage.ProtectedHeaders"/> does not have a value for the <see cref="CoseHeaderLabel.Algorithm"/> header.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was incorrectly formatted.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header was not one of the values supported by this implementation.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm protected header doesn't match with the algorithms supported by the specified <paramref name="key"/>.
        ///   </para>
        /// </exception>
        /// <seealso cref="VerifyDetached(CoseKey, Stream, ReadOnlySpan{byte})"/>
        /// <seealso cref="CoseMessage.Content"/>
        public Task<bool> VerifyDetachedAsync(CoseKey key, Stream detachedContent, ReadOnlyMemory<byte> associatedData = default, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(detachedContent);

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

            return VerifyAsyncCore(key, detachedContent, associatedData, cancellationToken);
        }

        private async Task<bool> VerifyAsyncCore(CoseKey key, Stream content, ReadOnlyMemory<byte> associatedData, CancellationToken cancellationToken)
        {
            using (ToBeSignedBuilder toBeSignedBuilder = key.CreateToBeSignedBuilder())
            {
                int bufferLength = CoseMessage.ComputeToBeSignedEncodedSize(
                    SigStructureContext.Signature,
                    Message.RawProtectedHeaders.Length,
                    _encodedSignProtectedHeaders.Length,
                    associatedData.Length,
                    contentLength: 0);
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferLength);

                try
                {
                    await CoseMessage.AppendToBeSignedAsync(buffer, toBeSignedBuilder, SigStructureContext.Signature, Message.RawProtectedHeaders, _encodedSignProtectedHeaders, associatedData, content, cancellationToken).ConfigureAwait(false);

                    bool retVal = false;
                    toBeSignedBuilder.WithDataAndResetAfterOperation(buffer, (buff, toBeSigned) =>
                    {
                        retVal = key.Verify(toBeSigned, _signature);
                    });

                    return retVal;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
                }
            }
        }

        private bool VerifyCore(CoseKey key, ReadOnlySpan<byte> contentBytes, Stream? contentStream, ReadOnlySpan<byte> associatedData)
        {
            using (ToBeSignedBuilder toBeSignedBuilder = key.CreateToBeSignedBuilder())
            {
                int bufferLength = CoseMessage.ComputeToBeSignedEncodedSize(
                    SigStructureContext.Signature,
                    Message.RawProtectedHeaders.Length,
                    _encodedSignProtectedHeaders.Length,
                    associatedData.Length,
                    contentLength: 0);
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferLength);

                try
                {
                    CoseMessage.AppendToBeSigned(buffer, toBeSignedBuilder, SigStructureContext.Signature, Message.RawProtectedHeaders.Span, _encodedSignProtectedHeaders, associatedData, contentBytes, contentStream);

                    bool retVal = false;
                    toBeSignedBuilder.WithDataAndResetAfterOperation(buffer, (buff, toBeSigned) =>
                    {
                        retVal = key.Verify(toBeSigned, _signature);
                    });

                    return retVal;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
                }
            }
        }

        private CoseAlgorithm GetCoseAlgorithmFromProtectedHeaders()
        {
            ReadOnlyMemory<byte> encodedAlg = CoseHelpers.GetCoseAlgorithmFromProtectedHeaders(ProtectedHeaders);

            CoseAlgorithm? nullableAlg = CoseHelpers.DecodeCoseAlgorithmHeader(encodedAlg);
            if (nullableAlg == null)
            {
                throw new CryptographicException(SR.Sign1VerifyAlgHeaderWasIncorrect);
            }

            return nullableAlg.Value;
        }
    }
}

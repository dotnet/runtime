// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Cbor;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography.Cose
{
    /// <summary>
    /// Represents a single-signature COSE_Sign1 message.
    /// </summary>
    public sealed class CoseSign1Message : CoseMessage
    {
        internal const int Sign1ArrayLength = 4;
        private const int Sign1SizeOfCborTag = 1;
        private readonly byte[] _signature;

        /// <summary>
        /// Gets the digital signature.
        /// </summary>
        /// <value>A region of memory that contains the digital signature.</value>
        public ReadOnlyMemory<byte> Signature => _signature;

        internal CoseSign1Message(CoseHeaderMap protectedHeader, CoseHeaderMap unprotectedHeader, byte[]? content, byte[] signature, byte[] protectedHeaderAsBstr, bool isTagged)
            : base(protectedHeader, unprotectedHeader, content, protectedHeaderAsBstr, isTagged)
        {
            _signature = signature;
        }

        /// <summary>
        /// Signs the specified content and encodes it as a COSE_Sign1 message with detached content.
        /// </summary>
        /// <param name="detachedContent">The content to sign.</param>
        /// <param name="signer">The signer information used to sign <paramref name="detachedContent"/>.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
        /// <returns>The encoded message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="detachedContent"/> or <paramref name="signer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     The <see cref="CoseSigner.ProtectedHeaders"/> and <see cref="CoseSigner.UnprotectedHeaders"/> collections in <paramref name="signer"/> have one or more labels in common.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     One or more of the labels specified in a <see cref="CoseHeaderLabel.CriticalHeaders"/> header is missing.
        ///   </para>
        /// </exception>
        public static byte[] SignDetached(byte[] detachedContent, CoseSigner signer, byte[]? associatedData = null)
        {
            if (detachedContent is null)
                throw new ArgumentNullException(nameof(detachedContent));

            if (signer is null)
                throw new ArgumentNullException(nameof(signer));

            return SignCore(detachedContent.AsSpan(), null, signer, associatedData, isDetached: true);
        }

        /// <summary>
        /// Signs the specified content and encodes it as a COSE_Sign1 message with embedded content.
        /// </summary>
        /// <param name="embeddedContent">The content to sign and to include in the message.</param>
        /// <param name="signer">The signer information used to sign <paramref name="embeddedContent"/>.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
        /// <returns>The encoded message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="embeddedContent"/> or <paramref name="signer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     The <see cref="CoseSigner.ProtectedHeaders"/> and <see cref="CoseSigner.UnprotectedHeaders"/> collections in <paramref name="signer"/> have one or more labels in common.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     One or more of the labels specified in a <see cref="CoseHeaderLabel.CriticalHeaders"/> header is missing.
        ///   </para>
        /// </exception>
        public static byte[] SignEmbedded(byte[] embeddedContent, CoseSigner signer, byte[]? associatedData = null)
        {
            if (embeddedContent is null)
                throw new ArgumentNullException(nameof(embeddedContent));

            if (signer is null)
                throw new ArgumentNullException(nameof(signer));

            return SignCore(embeddedContent.AsSpan(), null, signer, associatedData, isDetached: false);
        }

        /// <summary>
        /// Signs the specified content and encodes it as a COSE_Sign1 message with detached content.
        /// </summary>
        /// <param name="detachedContent">The content to sign.</param>
        /// <param name="signer">The signer information used to sign <paramref name="detachedContent"/>.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
        /// <returns>The encoded message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="signer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     The <see cref="CoseSigner.ProtectedHeaders"/> and <see cref="CoseSigner.UnprotectedHeaders"/> collections in <paramref name="signer"/> have one or more labels in common.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     One or more of the labels specified in a <see cref="CoseHeaderLabel.CriticalHeaders"/> header is missing.
        ///   </para>
        /// </exception>
        public static byte[] SignDetached(ReadOnlySpan<byte> detachedContent, CoseSigner signer, ReadOnlySpan<byte> associatedData = default)
        {
            if (signer is null)
                throw new ArgumentNullException(nameof(signer));

            return SignCore(detachedContent, null, signer, associatedData, isDetached: true);
        }

        /// <summary>
        /// Signs the specified content and encodes it as a COSE_Sign1 message with embedded content.
        /// </summary>
        /// <param name="embeddedContent">The content to sign and to include in the message.</param>
        /// <param name="signer">The signer information used to sign <paramref name="embeddedContent"/>.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
        /// <returns>The encoded message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="signer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     The <see cref="CoseSigner.ProtectedHeaders"/> and <see cref="CoseSigner.UnprotectedHeaders"/> collections in <paramref name="signer"/> have one or more labels in common.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     One or more of the labels specified in a <see cref="CoseHeaderLabel.CriticalHeaders"/> header is missing.
        ///   </para>
        /// </exception>
        public static byte[] SignEmbedded(ReadOnlySpan<byte> embeddedContent, CoseSigner signer, ReadOnlySpan<byte> associatedData = default)
        {
            if (signer is null)
                throw new ArgumentNullException(nameof(signer));

            return SignCore(embeddedContent, null, signer, associatedData, isDetached: false);
        }

        /// <summary>
        /// Signs the specified content and encodes it as a COSE_Sign1 message with detached content.
        /// </summary>
        /// <param name="detachedContent">The content to sign.</param>
        /// <param name="signer">The signer information used to sign <paramref name="detachedContent"/>.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
        /// <returns>The encoded message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="detachedContent"/> or <paramref name="signer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="detachedContent"/> does not support reading or seeking.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The <see cref="CoseSigner.ProtectedHeaders"/> and <see cref="CoseSigner.UnprotectedHeaders"/> collections in <paramref name="signer"/> have one or more labels in common.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     One or more of the labels specified in a <see cref="CoseHeaderLabel.CriticalHeaders"/> header is missing.
        ///   </para>
        /// </exception>
        public static byte[] SignDetached(Stream detachedContent, CoseSigner signer, ReadOnlySpan<byte> associatedData = default)
        {
            if (detachedContent is null)
                throw new ArgumentNullException(nameof(detachedContent));

            if (signer is null)
                throw new ArgumentNullException(nameof(signer));

            if (!detachedContent.CanRead)
                throw new ArgumentException(SR.Sign1ArgumentStreamNotReadable, nameof(detachedContent));

            if (!detachedContent.CanSeek)
                throw new ArgumentException(SR.Sign1ArgumentStreamNotSeekable, nameof(detachedContent));

            return SignCore(default, detachedContent, signer, associatedData, isDetached: true);
        }

        internal static byte[] SignCore(ReadOnlySpan<byte> contentBytes, Stream? contentStream, CoseSigner signer, ReadOnlySpan<byte> associatedData, bool isDetached)
        {
            Debug.Assert(contentStream == null || (isDetached && contentBytes.Length == 0));

            ValidateBeforeSign(signer);

            int expectedSize = ComputeEncodedSize(signer, contentBytes.Length, isDetached);
            var buffer = new byte[expectedSize];

            int bytesWritten = CreateCoseSign1Message(contentBytes, contentStream, buffer, signer, associatedData, isDetached);
            Debug.Assert(expectedSize == bytesWritten);

            return buffer;
        }

        /// <summary>
        /// Asynchronously signs the specified content and encodes it as a COSE_Sign1 message with detached content.
        /// </summary>
        /// <param name="detachedContent">The content to sign.</param>
        /// <param name="signer">The signer information used to sign <paramref name="detachedContent"/>.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A task that represents the asynchronous operation. The value of its <see cref="Task{T}.Result"/> property contains the encoded message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="detachedContent"/> or <paramref name="signer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="detachedContent"/> does not support reading or seeking.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The <see cref="CoseSigner.ProtectedHeaders"/> and <see cref="CoseSigner.UnprotectedHeaders"/> collections in <paramref name="signer"/> have one or more labels in common.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     One or more of the labels specified in a <see cref="CoseHeaderLabel.CriticalHeaders"/> header is missing.
        ///   </para>
        /// </exception>
        public static Task<byte[]> SignDetachedAsync(Stream detachedContent, CoseSigner signer, ReadOnlyMemory<byte> associatedData = default, CancellationToken cancellationToken = default)
        {
            if (detachedContent is null)
                throw new ArgumentNullException(nameof(detachedContent));

            if (signer is null)
                throw new ArgumentNullException(nameof(signer));

            if (!detachedContent.CanRead)
                throw new ArgumentException(SR.Sign1ArgumentStreamNotReadable, nameof(detachedContent));

            if (!detachedContent.CanSeek)
                throw new ArgumentException(SR.Sign1ArgumentStreamNotSeekable, nameof(detachedContent));

            ValidateBeforeSign(signer);

            int expectedSize = ComputeEncodedSize(signer, contentLength: 0, isDetached: true);
            return SignAsyncCore(expectedSize, detachedContent, signer, associatedData, cancellationToken);
        }

        private static async Task<byte[]> SignAsyncCore(int expectedSize, Stream content, CoseSigner signer, ReadOnlyMemory<byte> associatedData, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[expectedSize];
            int bytesWritten = await CreateCoseSign1MessageAsync(content, buffer, signer, associatedData, cancellationToken).ConfigureAwait(false);

            Debug.Assert(buffer.Length == bytesWritten);
            return buffer;
        }

        /// <summary>
        /// Attempts to sign the specified content and encode it as a COSE_Sign1 message with detached content into the specified buffer.
        /// </summary>
        /// <param name="detachedContent">The content to sign.</param>
        /// <param name="destination">The buffer in which to write the encoded bytes.</param>
        /// <param name="signer">The signer information used to sign <paramref name="detachedContent"/>.</param>
        /// <param name="bytesWritten">On success, receives the number of bytes written to <paramref name="destination"/>.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
        /// <returns><see langword="true"/> if <paramref name="destination"/> had sufficient length to receive the encoded message; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="signer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     The <see cref="CoseSigner.ProtectedHeaders"/> and <see cref="CoseSigner.UnprotectedHeaders"/> collections in <paramref name="signer"/> have one or more labels in common.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     One or more of the labels specified in a <see cref="CoseHeaderLabel.CriticalHeaders"/> header is missing.
        ///   </para>
        /// </exception>
        public static bool TrySignDetached(ReadOnlySpan<byte> detachedContent, Span<byte> destination, CoseSigner signer, out int bytesWritten, ReadOnlySpan<byte> associatedData = default)
            => TrySign(detachedContent, destination, signer, out bytesWritten, associatedData, isDetached: true);

        /// <summary>
        /// Attempts to sign the specified content and encode it as a COSE_Sign1 message with embedded content into the specified buffer.
        /// </summary>
        /// <param name="embeddedContent">The content to sign and to include in the message.</param>
        /// <param name="destination">The buffer in which to write the encoded bytes.</param>
        /// <param name="signer">The signer information used to sign <paramref name="embeddedContent"/>.</param>
        /// <param name="bytesWritten">On success, receives the number of bytes written to <paramref name="destination"/>.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
        /// <returns><see langword="true"/> if <paramref name="destination"/> had sufficient length to receive the encoded message; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="signer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     The <see cref="CoseSigner.ProtectedHeaders"/> and <see cref="CoseSigner.UnprotectedHeaders"/> collections in <paramref name="signer"/> have one or more labels in common.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     One or more of the labels specified in a <see cref="CoseHeaderLabel.CriticalHeaders"/> header is missing.
        ///   </para>
        /// </exception>
        public static bool TrySignEmbedded(ReadOnlySpan<byte> embeddedContent, Span<byte> destination, CoseSigner signer, out int bytesWritten, ReadOnlySpan<byte> associatedData = default)
            => TrySign(embeddedContent, destination, signer, out bytesWritten, associatedData, isDetached: false);

        private static bool TrySign(ReadOnlySpan<byte> content, Span<byte> destination, CoseSigner signer, out int bytesWritten, ReadOnlySpan<byte> associatedData, bool isDetached)
        {
            if (signer is null)
                throw new ArgumentNullException(nameof(signer));

            ValidateBeforeSign(signer);

            int expectedSize = ComputeEncodedSize(signer, content.Length, isDetached);
            if (expectedSize > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = CreateCoseSign1Message(content, null, destination, signer, associatedData, isDetached);
            Debug.Assert(expectedSize == bytesWritten);

            return true;
        }

        internal static void ValidateBeforeSign(CoseSigner signer)
        {
            if (ContainDuplicateLabels(signer._protectedHeaders, signer._unprotectedHeaders))
            {
                throw new ArgumentException(SR.Sign1SignHeaderDuplicateLabels, nameof(signer));
            }

            if (MissingCriticalHeaders(signer._protectedHeaders, out string? labelName))
            {
                throw new ArgumentException(SR.Format(SR.CriticalHeaderMissing, labelName), nameof(signer));
            }
        }

        private static int CreateCoseSign1Message(ReadOnlySpan<byte> contentBytes, Stream? contentStream, Span<byte> buffer, CoseSigner signer, ReadOnlySpan<byte> associatedData, bool isDetached)
        {
            var writer = new CborWriter();
            writer.WriteTag(Sign1Tag);
            writer.WriteStartArray(Sign1ArrayLength);

            int protectedMapBytesWritten = CoseHelpers.WriteHeaderMap(buffer, writer, signer._protectedHeaders, isProtected: true, signer._algHeaderValueToSlip);
            // We're going to use the encoded protected headers again after this step (for the toBeSigned construction),
            // so don't overwrite them yet.
            CoseHelpers.WriteHeaderMap(buffer.Slice(protectedMapBytesWritten), writer, signer._unprotectedHeaders, isProtected: false, null);

            CoseHelpers.WriteContent(writer, contentBytes, isDetached);

            using (IncrementalHash hasher = IncrementalHash.CreateHash(signer.HashAlgorithm))
            {
                AppendToBeSigned(buffer, hasher, SigStructureContext.Signature1, buffer.Slice(0, protectedMapBytesWritten), ReadOnlySpan<byte>.Empty, associatedData, contentBytes, contentStream);
                CoseHelpers.WriteSignature(buffer, hasher, writer, signer);
            }

            writer.WriteEndArray();
            return writer.Encode(buffer);
        }

        private static async Task<int> CreateCoseSign1MessageAsync(Stream content, byte[] buffer, CoseSigner signer, ReadOnlyMemory<byte> associatedData, CancellationToken cancellationToken)
        {
            var writer = new CborWriter();
            writer.WriteTag(Sign1Tag);
            writer.WriteStartArray(Sign1ArrayLength);

            int protectedMapBytesWritten = CoseHelpers.WriteHeaderMap(buffer, writer, signer._protectedHeaders, isProtected: true, signer._algHeaderValueToSlip);
            // We're going to use the encoded protected headers again after this step (for the toBeSigned construction),
            // so don't overwrite them yet.
            CoseHelpers.WriteHeaderMap(buffer.AsSpan(protectedMapBytesWritten), writer, signer._unprotectedHeaders, isProtected: false, null);
            CoseHelpers.WriteContent(writer, default, isDetached: true);

            using (IncrementalHash hasher = IncrementalHash.CreateHash(signer.HashAlgorithm))
            {
                await AppendToBeSignedAsync(buffer, hasher, SigStructureContext.Signature1, buffer.AsMemory(0, protectedMapBytesWritten), ReadOnlyMemory<byte>.Empty, associatedData, content, cancellationToken).ConfigureAwait(false);
                CoseHelpers.WriteSignature(buffer, hasher, writer, signer);
            }

            writer.WriteEndArray();
            return writer.Encode(buffer);
        }

        /// <summary>
        /// Verifies that the signature is valid for the content using the specified key.
        /// </summary>
        /// <param name="key">The public key that is associated with the private key that was used to sign the content.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must match the value provided during signing.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is of an unsupported type.</exception>
        /// <exception cref="InvalidOperationException">The content is detached from this message, use an overload that accepts a detached content.</exception>
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
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasDetached);
            }

            return VerifyCore(key, _content, null, associatedData, CoseHelpers.GetKeyType(key));
        }

        /// <summary>
        /// Verifies that the signature is valid for the content using the specified key.
        /// </summary>
        /// <param name="key">The public key that is associated with the private key that was used to sign the content.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must match the value provided during signing.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is of an unsupported type.</exception>
        /// <exception cref="InvalidOperationException">The content is detached from this message, use an overload that accepts a detached content.</exception>        /// <exception cref="CryptographicException">
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
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasDetached);
            }

            return VerifyCore(key, _content, null, associatedData, CoseHelpers.GetKeyType(key));
        }

        /// <summary>
        /// Verifies that the signature is valid for the content using the specified key.
        /// </summary>
        /// <param name="key">The public key that is associated with the private key that was used to sign the content.</param>
        /// <param name="detachedContent">The content that was previously signed.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must match the value provided during signing.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="detachedContent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is of an unsupported type.</exception>
        /// <exception cref="InvalidOperationException">The content is embedded on this message, use an overload that uses embedded content.</exception>
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
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (detachedContent is null)
            {
                throw new ArgumentNullException(nameof(detachedContent));
            }

            if (!IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasEmbedded);
            }

            return VerifyCore(key, detachedContent, null, associatedData, CoseHelpers.GetKeyType(key));
        }

        /// <summary>
        /// Verifies that the signature is valid for the content using the specified key.
        /// </summary>
        /// <param name="key">The public key that is associated with the private key that was used to sign the content.</param>
        /// <param name="detachedContent">The content that was previously signed.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must match the value provided during signing.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is of an unsupported type.</exception>
        /// <exception cref="InvalidOperationException">The content is embedded on this message, use an overload that uses embedded content.</exception>
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
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (!IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasEmbedded);
            }

            return VerifyCore(key, detachedContent, null, associatedData, CoseHelpers.GetKeyType(key));
        }

        /// <summary>
        /// Verifies that the signature is valid for the content using the specified key.
        /// </summary>
        /// <param name="key">The public key that is associated with the private key that was used to sign the content.</param>
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
        /// <exception cref="InvalidOperationException">The content is embedded on this message, use an overload that uses embedded content.</exception>
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

            if (!IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasEmbedded);
            }

            return VerifyCore(key, default, detachedContent, associatedData, CoseHelpers.GetKeyType(key));
        }

        private bool VerifyCore(AsymmetricAlgorithm key, ReadOnlySpan<byte> contentBytes, Stream? contentStream, ReadOnlySpan<byte> associatedData, KeyType keyType)
        {
            Debug.Assert(contentStream == null || contentBytes.Length == 0);
            ReadOnlyMemory<byte> encodedAlg = CoseHelpers.GetCoseAlgorithmFromProtectedHeaders(ProtectedHeaders);

            int? nullableAlg = CoseHelpers.DecodeCoseAlgorithmHeader(encodedAlg);
            if (nullableAlg == null)
            {
                throw new CryptographicException(SR.Sign1VerifyAlgHeaderWasIncorrect);
            }

            HashAlgorithmName hashAlgorithm = CoseHelpers.GetHashAlgorithmFromCoseAlgorithmAndKeyType(nullableAlg.Value, keyType, out RSASignaturePadding? padding);
            using (IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithm))
            {
                int bufferLength = ComputeToBeSignedEncodedSize(
                    SigStructureContext.Signature1,
                    _protectedHeaderAsBstr.Length,
                    signProtectedLength: 0,
                    associatedData.Length,
                    contentLength: 0);
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferLength);

                try
                {
                    AppendToBeSigned(buffer, hasher, SigStructureContext.Signature1, _protectedHeaderAsBstr, ReadOnlySpan<byte>.Empty, associatedData, contentBytes, contentStream);
                    return VerifyHash(key, hasher, hashAlgorithm, keyType, padding);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
                }
            }
        }

        /// <summary>
        /// Asynchronously verifies that the signature is valid for the content using the specified key.
        /// </summary>
        /// <param name="key">The public key that is associated with the private key that was used to sign the content.</param>
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
        /// <exception cref="InvalidOperationException">The content is embedded on this message, use an overload that uses embedded content.</exception>
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

            if (!IsDetached)
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
                int bufferLength = ComputeToBeSignedEncodedSize(
                    SigStructureContext.Signature1,
                    _protectedHeaderAsBstr.Length,
                    signProtectedLength: 0,
                    associatedData.Length,
                    contentLength: 0);
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferLength);

                await AppendToBeSignedAsync(buffer, hasher, SigStructureContext.Signature1, _protectedHeaderAsBstr, ReadOnlyMemory<byte>.Empty, associatedData, content, cancellationToken).ConfigureAwait(false);
                bool retVal = VerifyHash(key, hasher, hashAlgorithm, keyType, padding);

                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);

                return retVal;
            }
        }

        private bool VerifyHash(AsymmetricAlgorithm key, IncrementalHash hasher, HashAlgorithmName hashAlgorithm, KeyType keyType, RSASignaturePadding? padding)
        {
#if NETSTANDARD2_0 || NETFRAMEWORK
            byte[] hash = hasher.GetHashAndReset();
#else
            Debug.Assert(hasher.HashLengthInBytes <= 512 / 8); // largest hash we can get (SHA512).
            Span<byte> hash = stackalloc byte[hasher.HashLengthInBytes];
            hasher.GetHashAndReset(hash);
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

        private static int ComputeEncodedSize(CoseSigner signer, int contentLength, bool isDetached)
        {
            // tag + array(4) + encoded protected header map + unprotected header map + content + signature.
            int encodedSize = Sign1SizeOfCborTag + CoseHelpers.SizeOfArrayOfLessThan24 +
                CoseHelpers.GetByteStringEncodedSize(CoseHeaderMap.ComputeEncodedSize(signer._protectedHeaders, signer._algHeaderValueToSlip)) +
                CoseHeaderMap.ComputeEncodedSize(signer._unprotectedHeaders);

            if (isDetached)
            {
                encodedSize += CoseHelpers.SizeOfNull;
            }
            else
            {
                encodedSize += CoseHelpers.GetByteStringEncodedSize(contentLength);
            }

            encodedSize += CoseHelpers.GetByteStringEncodedSize(CoseHelpers.ComputeSignatureSize(signer));

            return encodedSize;
        }

        /// <summary>
        /// Calculates the number of bytes produced by encoding this message.
        /// </summary>
        /// <returns>The number of bytes produced by encoding this message.</returns>
        public override int GetEncodedLength() =>
            CoseHelpers.GetCoseSignEncodedLengthMinusSignature(_isTagged, Sign1SizeOfCborTag, _protectedHeaderAsBstr.Length, UnprotectedHeaders, _content) +
            CoseHelpers.GetByteStringEncodedSize(_signature.Length);

        /// <summary>
        /// Attempts to encode this message into the specified buffer.
        /// </summary>
        /// <param name="destination">The buffer in which to write the encoded value.</param>
        /// <param name="bytesWritten">On success, receives the number of bytes written to <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="destination"/> had sufficient length to receive the value; otherwise, <see langword="false"/>.</returns>
        /// <remarks>Use <see cref="GetEncodedLength()"/> to determine how many bytes result in encoding this message.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="CoseMessage.ProtectedHeaders"/> and <see cref="CoseMessage.UnprotectedHeaders"/> collections have one or more labels in common.</exception>
        /// <seealso cref="GetEncodedLength()"/>
        public override bool TryEncode(Span<byte> destination, out int bytesWritten)
        {
            if (ContainDuplicateLabels(ProtectedHeaders, UnprotectedHeaders))
            {
                throw new InvalidOperationException(SR.Sign1SignHeaderDuplicateLabels);
            }

            if (destination.Length < GetEncodedLength())
            {
                bytesWritten = 0;
                return false;
            }

            var writer = new CborWriter();

            if (_isTagged)
            {
                writer.WriteTag(Sign1Tag);
            }

            writer.WriteStartArray(Sign1ArrayLength);

            writer.WriteByteString(_protectedHeaderAsBstr);

            CoseHelpers.WriteHeaderMap(destination, writer, UnprotectedHeaders, isProtected: false, null);

            CoseHelpers.WriteContent(writer, Content.GetValueOrDefault().Span, !Content.HasValue);
            writer.WriteByteString(_signature);

            writer.WriteEndArray();

            bytesWritten = writer.Encode(destination);
            Debug.Assert(bytesWritten == GetEncodedLength());

            return true;
        }
    }
}

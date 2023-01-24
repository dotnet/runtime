// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Formats.Cbor;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography.Cose
{
    /// <summary>
    /// Represents a multiple signature COSE_Sign message.
    /// </summary>
    public sealed class CoseMultiSignMessage : CoseMessage
    {
        internal const int MultiSignArrayLength = 4;
        private const int MultiSignSizeOfCborTag = 2;
        internal const int CoseSignatureArrayLength = 3;

        private readonly List<CoseSignature> _signatures;

        /// <summary>
        /// Gets a read-only collection of signatures associated with this message.
        /// </summary>
        /// <value>A read-only collection of signatures associated with this message.</value>
        public ReadOnlyCollection<CoseSignature> Signatures { get; }

        internal CoseMultiSignMessage(CoseHeaderMap protectedHeader, CoseHeaderMap unprotectedHeader, byte[]? content, List<CoseSignature> signatures, byte[] encodedProtectedHeader, bool isTagged)
            : base(protectedHeader, unprotectedHeader, content, encodedProtectedHeader, isTagged)
        {
            foreach (CoseSignature s in signatures)
            {
                s.Message = this;
            }

            Signatures = new ReadOnlyCollection<CoseSignature>(signatures);
            _signatures = signatures;
        }

        /// <summary>
        /// Signs the specified content and encodes it as a COSE_Sign message with detached content.
        /// </summary>
        /// <param name="detachedContent">The content to sign.</param>
        /// <param name="signer">The signer information used to sign <paramref name="detachedContent"/>.</param>
        /// <param name="protectedHeaders">The protected header parameters to append to the message's content layer.</param>
        /// <param name="unprotectedHeaders">The unprotected header parameters to append to the message's content layer.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
        /// <returns>The encoded message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="detachedContent"/> or <paramref name="signer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     The <paramref name="protectedHeaders"/> and <paramref name="unprotectedHeaders"/> collections have one or more labels in common.
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
        public static byte[] SignDetached(byte[] detachedContent, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, byte[]? associatedData = null)
        {
            if (detachedContent is null)
                throw new ArgumentNullException(nameof(detachedContent));

            return SignCore(detachedContent, null, signer, protectedHeaders, unprotectedHeaders, associatedData, isDetached: true);
        }

        /// <summary>
        /// Signs the specified content and encodes it as a COSE_Sign message with embedded content.
        /// </summary>
        /// <param name="embeddedContent">The content to sign and to include in the message.</param>
        /// <param name="signer">The signer information used to sign <paramref name="embeddedContent"/>.</param>
        /// <param name="protectedHeaders">The protected header parameters to append to the message's content layer.</param>
        /// <param name="unprotectedHeaders">The unprotected header parameters to append to the message's content layer.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
        /// <returns>The encoded message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="embeddedContent"/> or <paramref name="signer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     The <paramref name="protectedHeaders"/> and <paramref name="unprotectedHeaders"/> collections have one or more labels in common.
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
        public static byte[] SignEmbedded(byte[] embeddedContent, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, byte[]? associatedData = null)
        {
            if (embeddedContent is null)
                throw new ArgumentNullException(nameof(embeddedContent));

            return SignCore(embeddedContent, null, signer, protectedHeaders, unprotectedHeaders, associatedData, isDetached: false);
        }

        /// <summary>
        /// Signs the specified content and encodes it as a COSE_Sign message with detached content.
        /// </summary>
        /// <param name="detachedContent">The content to sign.</param>
        /// <param name="signer">The signer information used to sign <paramref name="detachedContent"/>.</param>
        /// <param name="protectedHeaders">The protected header parameters to append to the message's content layer.</param>
        /// <param name="unprotectedHeaders">The unprotected header parameters to append to the message's content layer.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
        /// <returns>The encoded message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="signer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     The <paramref name="protectedHeaders"/> and <paramref name="unprotectedHeaders"/> collections have one or more labels in common.
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
        public static byte[] SignDetached(ReadOnlySpan<byte> detachedContent, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, ReadOnlySpan<byte> associatedData = default)
            => SignCore(detachedContent, null, signer, protectedHeaders, unprotectedHeaders, associatedData, isDetached: true);


        /// <summary>
        /// Signs the specified content and encodes it as a COSE_Sign message with detached content.
        /// </summary>
        /// <param name="embeddedContent">The content to sign and to include in the message.</param>
        /// <param name="signer">The signer information used to sign <paramref name="embeddedContent"/>.</param>
        /// <param name="protectedHeaders">The protected header parameters to append to the message's content layer.</param>
        /// <param name="unprotectedHeaders">The unprotected header parameters to append to the message's content layer.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
        /// <returns>The encoded message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="signer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     The <paramref name="protectedHeaders"/> and <paramref name="unprotectedHeaders"/> collections have one or more labels in common.
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
        public static byte[] SignEmbedded(ReadOnlySpan<byte> embeddedContent, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, ReadOnlySpan<byte> associatedData = default)
            => SignCore(embeddedContent, null, signer, protectedHeaders, unprotectedHeaders, associatedData, isDetached: false);

        /// <summary>
        /// Signs the specified content and encodes it as a COSE_Sign message with detached content.
        /// </summary>
        /// <param name="detachedContent">The content to sign.</param>
        /// <param name="signer">The signer information used to sign <paramref name="detachedContent"/>.</param>
        /// <param name="protectedHeaders">The protected header parameters to append to the message's content layer.</param>
        /// <param name="unprotectedHeaders">The unprotected header parameters to append to the message's content layer.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
        /// <returns>The encoded message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="detachedContent"/> or <paramref name="signer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="detachedContent"/> does not support reading or seeking.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The <paramref name="protectedHeaders"/> and <paramref name="unprotectedHeaders"/> collections have one or more labels in common.
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
        /// <seealso cref="SignDetachedAsync"/>
        public static byte[] SignDetached(Stream detachedContent, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, ReadOnlySpan<byte> associatedData = default)
        {
            if (detachedContent is null)
                throw new ArgumentNullException(nameof(detachedContent));

            if (!detachedContent.CanRead)
                throw new ArgumentException(SR.Sign1ArgumentStreamNotReadable, nameof(detachedContent));

            if (!detachedContent.CanSeek)
                throw new ArgumentException(SR.Sign1ArgumentStreamNotSeekable, nameof(detachedContent));

            return SignCore(default, detachedContent, signer, protectedHeaders, unprotectedHeaders, associatedData, isDetached: true);
        }

        private static byte[] SignCore(
            ReadOnlySpan<byte> content,
            Stream? contentStream,
            CoseSigner signer,
            CoseHeaderMap? protectedHeaders,
            CoseHeaderMap? unprotectedHeaders,
            ReadOnlySpan<byte> associatedData,
            bool isDetached)
        {
            if (signer is null)
                throw new ArgumentNullException(nameof(signer));

            ValidateBeforeSign(signer, protectedHeaders, unprotectedHeaders);

            int expectedSize = ComputeEncodedSize(signer, protectedHeaders, unprotectedHeaders, content.Length, isDetached);
            var buffer = new byte[expectedSize];

            int bytesWritten = CreateCoseMultiSignMessage(content, contentStream, buffer, signer, protectedHeaders, unprotectedHeaders, associatedData, isDetached);
            Debug.Assert(expectedSize == bytesWritten);

            return buffer;
        }

        /// <summary>
        /// Asynchronously signs the specified content and encodes it as a COSE_Sign message with detached content.
        /// </summary>
        /// <param name="detachedContent">The content to sign.</param>
        /// <param name="signer">The signer information used to sign <paramref name="detachedContent"/>.</param>
        /// <param name="protectedHeaders">The protected header parameters to append to the message's content layer.</param>
        /// <param name="unprotectedHeaders">The unprotected header parameters to append to the message's content layer.</param>
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
        ///     The <paramref name="protectedHeaders"/> and <paramref name="unprotectedHeaders"/> collections have one or more labels in common.
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
        public static Task<byte[]> SignDetachedAsync(
            Stream detachedContent,
            CoseSigner signer,
            CoseHeaderMap? protectedHeaders = null,
            CoseHeaderMap? unprotectedHeaders = null,
            ReadOnlyMemory<byte> associatedData = default,
            CancellationToken cancellationToken = default)
        {
            if (detachedContent is null)
                throw new ArgumentNullException(nameof(detachedContent));

            if (signer is null)
                throw new ArgumentNullException(nameof(signer));

            if (!detachedContent.CanRead)
                throw new ArgumentException(SR.Sign1ArgumentStreamNotReadable, nameof(detachedContent));

            if (!detachedContent.CanSeek)
                throw new ArgumentException(SR.Sign1ArgumentStreamNotSeekable, nameof(detachedContent));

            ValidateBeforeSign(signer, protectedHeaders, unprotectedHeaders);

            int expectedSize = ComputeEncodedSize(signer, protectedHeaders, unprotectedHeaders, contentLength: 0, isDetached: true);
            return SignAsyncCore(expectedSize, detachedContent, signer, protectedHeaders, unprotectedHeaders, associatedData, cancellationToken);
        }

        private static async Task<byte[]> SignAsyncCore(
            int expectedSize,
            Stream content,
            CoseSigner signer,
            CoseHeaderMap? protectedHeaders,
            CoseHeaderMap? unprotectedHeaders,
            ReadOnlyMemory<byte> associatedData,
            CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[expectedSize];
            int bytesWritten = await CreateCoseMultiSignMessageAsync(content, buffer, signer, protectedHeaders, unprotectedHeaders, associatedData, cancellationToken).ConfigureAwait(false);

            Debug.Assert(buffer.Length == bytesWritten);
            return buffer;
        }

        /// <summary>
        /// Attempts to sign the specified content and encode it as a COSE_Sign message with detached content into the specified buffer.
        /// </summary>
        /// <param name="detachedContent">The content to sign.</param>
        /// <param name="destination">The buffer in which to write the encoded bytes.</param>
        /// <param name="signer">The signer information used to sign <paramref name="detachedContent"/>.</param>
        /// <param name="bytesWritten">On success, receives the number of bytes written to <paramref name="destination"/>.</param>
        /// <param name="protectedHeaders">The protected header parameters to append to the message's content layer.</param>
        /// <param name="unprotectedHeaders">The unprotected header parameters to append to the message's content layer.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
        /// <returns><see langword="true"/> if <paramref name="destination"/> had sufficient length to receive the encoded message; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="signer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     The <paramref name="protectedHeaders"/> and <paramref name="unprotectedHeaders"/> collections have one or more labels in common.
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
        public static bool TrySignDetached(ReadOnlySpan<byte> detachedContent, Span<byte> destination, CoseSigner signer, out int bytesWritten, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, ReadOnlySpan<byte> associatedData = default)
            => TrySign(detachedContent, destination, signer, protectedHeaders, unprotectedHeaders, out bytesWritten, associatedData, isDetached: true);

        /// <summary>
        /// Signs the specified content and encodes it as a COSE_Sign message with embedded content.
        /// </summary>
        /// <param name="embeddedContent">The content to sign and to include in the message.</param>
        /// <param name="destination">The buffer in which to write the encoded bytes.</param>
        /// <param name="signer">The signer information used to sign <paramref name="embeddedContent"/>.</param>
        /// <param name="bytesWritten">On success, receives the number of bytes written to <paramref name="destination"/>.</param>
        /// <param name="protectedHeaders">The protected header parameters to append to the message's content layer.</param>
        /// <param name="unprotectedHeaders">The unprotected header parameters to append to the message's content layer.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
        /// <returns><see langword="true"/> if <paramref name="destination"/> had sufficient length to receive the encoded message; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="signer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     The <paramref name="protectedHeaders"/> and <paramref name="unprotectedHeaders"/> collections have one or more labels in common.
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
        public static bool TrySignEmbedded(ReadOnlySpan<byte> embeddedContent, Span<byte> destination, CoseSigner signer, out int bytesWritten, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, ReadOnlySpan<byte> associatedData = default)
            => TrySign(embeddedContent, destination, signer, protectedHeaders, unprotectedHeaders, out bytesWritten, associatedData, isDetached: false);

        private static bool TrySign(ReadOnlySpan<byte> content, Span<byte> destination, CoseSigner signer, CoseHeaderMap? protectedHeaders, CoseHeaderMap? unprotectedHeaders, out int bytesWritten, ReadOnlySpan<byte> associatedData, bool isDetached)
        {
            if (signer is null)
                throw new ArgumentNullException(nameof(signer));

            ValidateBeforeSign(signer, protectedHeaders, unprotectedHeaders);

            int expectedSize = ComputeEncodedSize(signer, protectedHeaders, unprotectedHeaders, content.Length, isDetached);
            if (expectedSize > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = CreateCoseMultiSignMessage(content, null, destination, signer, protectedHeaders, unprotectedHeaders, associatedData, isDetached);
            Debug.Assert(expectedSize == bytesWritten);

            return true;
        }

        private static int CreateCoseMultiSignMessage(
            ReadOnlySpan<byte> content,
            Stream? contentStream,
            Span<byte> buffer,
            CoseSigner signer,
            CoseHeaderMap? protectedHeaders,
            CoseHeaderMap? unprotectedHeaders,
            ReadOnlySpan<byte> associatedData,
            bool isDetached)
        {
            var writer = new CborWriter();
            writer.WriteTag(MultiSignTag);
            writer.WriteStartArray(MultiSignArrayLength);

            int protectedMapBytesWritten = CoseHelpers.WriteHeaderMap(buffer, writer, protectedHeaders, isProtected: true, null);
            // We're going to use the encoded protected headers again after this step (for the toBeSigned construction),
            // so don't overwrite them yet.
            CoseHelpers.WriteHeaderMap(buffer.Slice(protectedMapBytesWritten), writer, unprotectedHeaders, isProtected: false, null);

            CoseHelpers.WriteContent(writer, content, isDetached);

            WriteCoseSignaturesArray(writer, signer, buffer.Slice(protectedMapBytesWritten), buffer.Slice(0, protectedMapBytesWritten), associatedData, content, contentStream);

            writer.WriteEndArray();
            return writer.Encode(buffer);
        }

        private static async Task<int> CreateCoseMultiSignMessageAsync(
            Stream content,
            byte[] buffer,
            CoseSigner signer,
            CoseHeaderMap? protectedHeaders,
            CoseHeaderMap? unprotectedHeaders,
            ReadOnlyMemory<byte> associatedData,
            CancellationToken cancellationToken)
        {
            var writer = new CborWriter();
            writer.WriteTag(MultiSignTag);
            writer.WriteStartArray(MultiSignArrayLength);

            int protectedMapBytesWritten = CoseHelpers.WriteHeaderMap(buffer, writer, protectedHeaders, isProtected: true, null);
            // We're going to use the encoded protected headers again after this step (for the toBeSigned construction),
            // so don't overwrite them yet.
            CoseHelpers.WriteHeaderMap(buffer.AsSpan(protectedMapBytesWritten), writer, unprotectedHeaders, isProtected: false, null);
            CoseHelpers.WriteContent(writer, default, isDetached: true);

            await WriteSignatureAsync(writer, signer, buffer, buffer.AsMemory(0, protectedMapBytesWritten), associatedData, content, cancellationToken).ConfigureAwait(false);

            writer.WriteEndArray();
            return writer.Encode(buffer);
        }

        private static void ValidateBeforeSign(CoseSigner signer, CoseHeaderMap? protectedHeaders, CoseHeaderMap? unprotectedHeaders)
        {
            if (ContainDuplicateLabels(signer._protectedHeaders, signer._unprotectedHeaders))
            {
                throw new ArgumentException(SR.Sign1SignHeaderDuplicateLabels, nameof(signer));
            }

            if (ContainDuplicateLabels(protectedHeaders, unprotectedHeaders))
            {
                throw new ArgumentException(SR.Sign1SignHeaderDuplicateLabels);
            }

            if (MissingCriticalHeaders(signer._protectedHeaders, out string? labelName))
            {
                throw new ArgumentException(SR.Format(SR.CriticalHeaderMissing, labelName), nameof(signer));
            }

            if (MissingCriticalHeaders(protectedHeaders, out labelName))
            {
                throw new ArgumentException(SR.Format(SR.CriticalHeaderMissing, labelName), nameof(protectedHeaders));
            }
        }

        private static void WriteCoseSignaturesArray(
            CborWriter writer,
            CoseSigner signer,
            Span<byte> buffer,
            ReadOnlySpan<byte> bodyProtected,
            ReadOnlySpan<byte> associatedData,
            ReadOnlySpan<byte> content,
            Stream? contentStream)
        {
            writer.WriteStartArray(1);
            writer.WriteStartArray(CoseSignatureArrayLength);

            int signProtectedBytesWritten = CoseHelpers.WriteHeaderMap(buffer, writer, signer._protectedHeaders, isProtected: true, signer._algHeaderValueToSlip);

            CoseHelpers.WriteHeaderMap(buffer.Slice(signProtectedBytesWritten), writer, signer.UnprotectedHeaders, isProtected: false, null);

            using (IncrementalHash hasher = IncrementalHash.CreateHash(signer.HashAlgorithm))
            {
                AppendToBeSigned(buffer, hasher, SigStructureContext.Signature, bodyProtected, buffer.Slice(0, signProtectedBytesWritten), associatedData, content, contentStream);
                CoseHelpers.WriteSignature(buffer, hasher, writer, signer);
            }

            writer.WriteEndArray();
            writer.WriteEndArray();
        }

        private static async Task WriteSignatureAsync(
            CborWriter writer,
            CoseSigner signer,
            byte[] buffer,
            ReadOnlyMemory<byte> bodyProtected,
            ReadOnlyMemory<byte> associatedData,
            Stream contentStream,
            CancellationToken cancellationToken)
        {
            int start = bodyProtected.Length;

            writer.WriteStartArray(1);
            writer.WriteStartArray(CoseSignatureArrayLength);

            int signProtectedBytesWritten = CoseHelpers.WriteHeaderMap(buffer.AsSpan(start), writer, signer._protectedHeaders, isProtected: true, signer._algHeaderValueToSlip);

            CoseHelpers.WriteHeaderMap(buffer.AsSpan(start + signProtectedBytesWritten), writer, signer.UnprotectedHeaders, isProtected: false, null);

            HashAlgorithmName hashAlgorithm = signer.HashAlgorithm;
            using (IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithm))
            {
                // We can use the whole buffer at this point as the space for bodyProtected and signProtected is consumed.
                await AppendToBeSignedAsync(buffer, hasher, SigStructureContext.Signature, bodyProtected, buffer.AsMemory(start, signProtectedBytesWritten), associatedData, contentStream, cancellationToken).ConfigureAwait(false);
                CoseHelpers.WriteSignature(buffer, hasher, writer, signer);
            }

            writer.WriteEndArray();
            writer.WriteEndArray();
        }

        private static int ComputeEncodedSize(CoseSigner signer, CoseHeaderMap? protectedHeaders, CoseHeaderMap? unprotectedHeaders, int contentLength, bool isDetached)
        {
            // tag + array(4) + encoded protected header map + unprotected header map + content + [+COSE_Signature].
            int encodedSize = MultiSignSizeOfCborTag + CoseHelpers.SizeOfArrayOfLessThan24;

            int protectedHeadersSize = CoseHeaderMap.ComputeEncodedSize(protectedHeaders);
            if (protectedHeadersSize > 1)
            {
                encodedSize += CoseHelpers.GetByteStringEncodedSize(protectedHeadersSize);
            }
            else
            {
                Debug.Assert(protectedHeadersSize == 1);
                encodedSize += protectedHeadersSize;
            }

            encodedSize += CoseHeaderMap.ComputeEncodedSize(unprotectedHeaders);

            if (isDetached)
            {
                encodedSize += CoseHelpers.SizeOfNull;
            }
            else
            {
                encodedSize += CoseHelpers.GetByteStringEncodedSize(contentLength);
            }

            encodedSize += CoseHelpers.SizeOfArrayOfLessThan24;
            encodedSize += CoseHelpers.SizeOfArrayOfLessThan24;
            encodedSize += CoseHelpers.GetByteStringEncodedSize(CoseHeaderMap.ComputeEncodedSize(signer._protectedHeaders, signer._algHeaderValueToSlip));
            encodedSize += CoseHeaderMap.ComputeEncodedSize(signer._unprotectedHeaders);
            encodedSize += CoseHelpers.GetByteStringEncodedSize(CoseHelpers.ComputeSignatureSize(signer));

            return encodedSize;
        }

        /// <summary>
        /// Calculates the number of bytes produced by encoding this message.
        /// </summary>
        /// <returns>The number of bytes produced by encoding this message.</returns>
        public override int GetEncodedLength()
        {
            int encodedLength = CoseHelpers.GetCoseSignEncodedLengthMinusSignature(_isTagged, MultiSignSizeOfCborTag, _protectedHeaderAsBstr.Length, UnprotectedHeaders, _content);
            encodedLength += CoseHelpers.GetIntegerEncodedSize(Signatures.Count);

            foreach (CoseSignature signature in Signatures)
            {
                encodedLength += CoseHelpers.SizeOfArrayOfLessThan24;
                encodedLength += CoseHelpers.GetByteStringEncodedSize(signature._encodedSignProtectedHeaders.Length);
                encodedLength += CoseHeaderMap.ComputeEncodedSize(signature.UnprotectedHeaders);
                encodedLength += CoseHelpers.GetByteStringEncodedSize(signature._signature.Length);
            }

            return encodedLength;
        }

        /// <summary>
        /// Attempts to encode this message into the specified buffer.
        /// </summary>
        /// <param name="destination">The buffer in which to write the encoded value.</param>
        /// <param name="bytesWritten">On success, receives the number of bytes written to <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="destination"/> had sufficient length to receive the value; otherwise, <see langword="false"/>.</returns>
        /// <remarks>Use <see cref="GetEncodedLength()"/> to determine how many bytes result in encoding this message.</remarks>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///     The <see cref="CoseMessage.ProtectedHeaders"/> and <see cref="CoseMessage.UnprotectedHeaders"/> collections have one or more labels in common.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The message does not contain at least one signature.
        ///   </para>
        /// </exception>
        /// <seealso cref="GetEncodedLength()"/>
        public override bool TryEncode(Span<byte> destination, out int bytesWritten)
        {
            if (ContainDuplicateLabels(ProtectedHeaders, UnprotectedHeaders))
            {
                throw new InvalidOperationException(SR.Sign1SignHeaderDuplicateLabels);
            }

            if (Signatures.Count < 1)
            {
                throw new InvalidOperationException(SR.MultiSignMessageMustCarryAtLeastOneSignature);
            }

            if (destination.Length < GetEncodedLength())
            {
                bytesWritten = 0;
                return false;
            }

            var writer = new CborWriter();

            if (_isTagged)
            {
                writer.WriteTag(MultiSignTag);
            }

            writer.WriteStartArray(MultiSignArrayLength);

            writer.WriteByteString(_protectedHeaderAsBstr);

            CoseHelpers.WriteHeaderMap(destination, writer, UnprotectedHeaders, isProtected: false, null);

            CoseHelpers.WriteContent(writer, Content.GetValueOrDefault().Span, !Content.HasValue);

            writer.WriteStartArray(Signatures.Count);
            foreach (CoseSignature signature in Signatures)
            {
                if (ContainDuplicateLabels(signature.ProtectedHeaders, signature.UnprotectedHeaders))
                {
                    throw new InvalidOperationException(SR.Sign1SignHeaderDuplicateLabels);
                }

                writer.WriteStartArray(CoseSignatureArrayLength);
                writer.WriteByteString(signature._encodedSignProtectedHeaders);

                CoseHelpers.WriteHeaderMap(destination, writer, signature.UnprotectedHeaders, false, null);

                writer.WriteByteString(signature._signature);
                writer.WriteEndArray();
            }

            writer.WriteEndArray();
            writer.WriteEndArray();

            bytesWritten = writer.Encode(destination);
            Debug.Assert(bytesWritten == GetEncodedLength());

            return true;
        }

        /// <summary>
        /// Adds a signature for the content embedded in this message.
        /// </summary>
        /// <param name="signer">The signer information used to sign the content.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
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
        /// <exception cref="InvalidOperationException">The content is detached from this message, use an overload that accepts a detached content.</exception>
        public void AddSignatureForEmbedded(CoseSigner signer, byte[]? associatedData = null)
            => AddSignatureForEmbedded(signer, associatedData.AsSpan());

        /// <summary>
        /// Adds a signature for the content embedded in this message.
        /// </summary>
        /// <param name="signer">The signer information used to sign the content.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
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
        /// <exception cref="InvalidOperationException">The content is detached from this message, use an overload that accepts a detached content.</exception>
        public void AddSignatureForEmbedded(CoseSigner signer, ReadOnlySpan<byte> associatedData)
        {
            if (signer == null)
            {
                throw new ArgumentNullException(nameof(signer));
            }

            if (IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasDetached);
            }

            AddSignatureCore(_content, null, signer, associatedData);
        }

        /// <summary>
        /// Adds a signature for the specified content to this message.
        /// </summary>
        /// <param name="detachedContent">The content to sign.</param>
        /// <param name="signer">The signer information used to sign the content.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
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
        /// <exception cref="InvalidOperationException">The content is embedded on this message, use an overload that uses embedded content.</exception>
        public void AddSignatureForDetached(byte[] detachedContent, CoseSigner signer, byte[]? associatedData = null)
        {
            if (detachedContent == null)
            {
                throw new ArgumentNullException(nameof(detachedContent));
            }

            AddSignatureForDetached(detachedContent.AsSpan(), signer, associatedData);
        }

        /// <summary>
        /// Adds a signature for the specified content to this message.
        /// </summary>
        /// <param name="detachedContent">The content to sign.</param>
        /// <param name="signer">The signer information used to sign the content.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
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
        /// <exception cref="InvalidOperationException">The content is embedded on this message, use an overload that uses embedded content.</exception>
        public void AddSignatureForDetached(ReadOnlySpan<byte> detachedContent, CoseSigner signer, ReadOnlySpan<byte> associatedData = default)
        {
            if (signer == null)
            {
                throw new ArgumentNullException(nameof(signer));
            }

            if (!IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasEmbedded);
            }

            AddSignatureCore(detachedContent, null, signer, associatedData);
        }

        /// <summary>
        /// Adds a signature for the specified content to this message.
        /// </summary>
        /// <param name="detachedContent">The content to sign.</param>
        /// <param name="signer">The signer information used to sign the content.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
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
        /// <exception cref="InvalidOperationException">The content is embedded on this message, use an overload that uses embedded content.</exception>
        public void AddSignatureForDetached(Stream detachedContent, CoseSigner signer, ReadOnlySpan<byte> associatedData = default)
        {
            if (detachedContent == null)
            {
                throw new ArgumentNullException(nameof(detachedContent));
            }

            if (!IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasEmbedded);
            }

            AddSignatureCore(default, detachedContent, signer, associatedData);
        }

        private void AddSignatureCore(ReadOnlySpan<byte> contentBytes, Stream? contentStream, CoseSigner signer, ReadOnlySpan<byte> associatedData)
        {
            ValidateBeforeSign(signer, null, null);

            CoseHeaderMap signProtectedHeaders = signer.ProtectedHeaders;
            int? algHeaderValueToSlip = signer._algHeaderValueToSlip;
            int signProtectedEncodedLength = CoseHeaderMap.ComputeEncodedSize(signProtectedHeaders, algHeaderValueToSlip);

            int toBeSignedLength = ComputeToBeSignedEncodedSize(
                SigStructureContext.Signature,
                _protectedHeaderAsBstr.Length,
                signProtectedEncodedLength,
                associatedData.Length,
                contentLength: 0);

            byte[] buffer = ArrayPool<byte>.Shared.Rent(Math.Max(toBeSignedLength, CoseHelpers.ComputeSignatureSize(signer)));

            try
            {
                Span<byte> bufferSpan = buffer;
                int bytesWritten = CoseHeaderMap.Encode(signProtectedHeaders, bufferSpan, isProtected: true, algHeaderValueToSlip);
                byte[] encodedSignProtected = bufferSpan.Slice(0, bytesWritten).ToArray();

                using (IncrementalHash hasher = IncrementalHash.CreateHash(signer.HashAlgorithm))
                {
                    AppendToBeSigned(bufferSpan, hasher, SigStructureContext.Signature, _protectedHeaderAsBstr, encodedSignProtected, associatedData, contentBytes, contentStream);
                    bytesWritten = CoseHelpers.SignHash(signer, hasher, buffer);

                    byte[] signature = bufferSpan.Slice(0, bytesWritten).ToArray();
                    _signatures.Add(new CoseSignature(this, signProtectedHeaders, signer.UnprotectedHeaders, encodedSignProtected, signature));
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }

        /// <summary>
        /// Asynchronously adds a signature for the specified content to this message.
        /// </summary>
        /// <param name="detachedContent">The content to sign.</param>
        /// <param name="signer">The signer information used to sign the content.</param>
        /// <param name="associatedData">The extra data associated with the signature, which must also be provided during verification.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
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
        /// <exception cref="InvalidOperationException">The content is embedded on this message, use an overload that uses embedded content.</exception>
        public Task AddSignatureForDetachedAsync(Stream detachedContent, CoseSigner signer, ReadOnlyMemory<byte> associatedData = default, CancellationToken cancellationToken = default)
        {
            if (detachedContent == null)
            {
                throw new ArgumentNullException(nameof(detachedContent));
            }

            if (!IsDetached)
            {
                throw new InvalidOperationException(SR.ContentWasEmbedded);
            }

            return AddSignatureCoreAsync(detachedContent, signer, associatedData, cancellationToken);
        }

        private async Task AddSignatureCoreAsync(Stream content, CoseSigner signer, ReadOnlyMemory<byte> associatedData, CancellationToken cancellationToken)
        {
            ValidateBeforeSign(signer, null, null);

            CoseHeaderMap signProtectedHeaders = signer.ProtectedHeaders;
            int? algHeaderValueToSlip = signer._algHeaderValueToSlip;
            int signProtectedEncodedLength = CoseHeaderMap.ComputeEncodedSize(signProtectedHeaders, algHeaderValueToSlip);

            int toBeSignedLength = ComputeToBeSignedEncodedSize(
                SigStructureContext.Signature,
                _protectedHeaderAsBstr.Length,
                signProtectedEncodedLength,
                associatedData.Length,
                contentLength: 0);

            byte[] buffer = ArrayPool<byte>.Shared.Rent(Math.Max(toBeSignedLength, CoseHelpers.ComputeSignatureSize(signer)));

            int bytesWritten = CoseHeaderMap.Encode(signProtectedHeaders, buffer, isProtected: true, algHeaderValueToSlip);
            byte[] encodedSignProtected = buffer.AsSpan(0, bytesWritten).ToArray();

            using (IncrementalHash hasher = IncrementalHash.CreateHash(signer.HashAlgorithm))
            {
                await AppendToBeSignedAsync(buffer, hasher, SigStructureContext.Signature, _protectedHeaderAsBstr, encodedSignProtected, associatedData, content, cancellationToken).ConfigureAwait(false);
                bytesWritten = CoseHelpers.SignHash(signer, hasher, buffer);

                byte[] signature = buffer.AsSpan(0, bytesWritten).ToArray();
                _signatures.Add(new CoseSignature(this, signProtectedHeaders, signer.UnprotectedHeaders, encodedSignProtected, signature));
            }

            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }

        /// <summary>
        /// Removes the specified signature from the message.
        /// </summary>
        /// <param name="signature">The signature to remove from the message.</param>
        /// <exception cref="ArgumentNullException"><paramref name="signature"/> is <see langword="null"/>.</exception>
        /// <seealso cref="Signatures"/>
        public void RemoveSignature(CoseSignature signature)
        {
            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature));
            }

            _signatures.Remove(signature);
        }

        /// <summary>
        /// Removes the signature at the specified index from the message.
        /// </summary>
        /// <param name="index">The zero-based index of the signature to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <para>
        ///     <paramref name="index"/> is less than 0.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="index"/> is equal to or greater than the number of elements in <see cref="Signatures"/>.
        ///   </para>
        /// </exception>
        /// <seealso cref="Signatures"/>
        public void RemoveSignature(int index)
            => _signatures.RemoveAt(index);
    }
}

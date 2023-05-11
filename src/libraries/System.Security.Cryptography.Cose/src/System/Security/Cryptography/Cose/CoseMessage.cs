// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Cbor;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography.Cose
{
    /// <summary>
    /// Represents a COSE message as described in RFC 8152.
    /// </summary>
    public abstract class CoseMessage
    {
        private const string SigStructureContextSign = "Signature";
        private const string SigStructureContextSign1 = "Signature1";
        internal static readonly int SizeOfSigStructureCtxSign = CoseHelpers.GetTextStringEncodedSize(SigStructureContextSign);
        internal static readonly int SizeOfSigStructureCtxSign1 = CoseHelpers.GetTextStringEncodedSize(SigStructureContextSign1);

        // COSE tags https://datatracker.ietf.org/doc/html/rfc8152#page-8 Table 1.
        internal const CborTag Sign1Tag = (CborTag)18;
        internal const CborTag MultiSignTag = (CborTag)98;

        internal readonly byte[]? _content;
        internal readonly byte[] _protectedHeaderAsBstr;
        internal readonly bool _isTagged;

        private readonly CoseHeaderMap _protectedHeaders;
        private readonly CoseHeaderMap _unprotectedHeaders;

        /// <summary>
        /// Gets the protected header parameters associated with this message.
        /// </summary>
        /// <value>A collection of protected header parameters associated with this message.</value>
        public CoseHeaderMap ProtectedHeaders => _protectedHeaders;

        /// <summary>
        /// Gets the unprotected header parameters associated with this message.
        /// </summary>
        /// <value>A collection of unprotected header parameters associated with this message.</value>
        public CoseHeaderMap UnprotectedHeaders => _unprotectedHeaders;

        /// <summary>
        /// Gets the raw bytes of the protected header parameters associated with this message.
        /// </summary>
        /// <value>A region of memory that contains the raw bytes of the protected header parameters associated with this message.</value>
        public ReadOnlyMemory<byte> RawProtectedHeaders => _protectedHeaderAsBstr;

        internal CoseMessage(CoseHeaderMap protectedHeader, CoseHeaderMap unprotectedHeader, byte[]? content, byte[] encodedProtectedHeader, bool isTagged)
        {
            _content = content;
            _protectedHeaderAsBstr = encodedProtectedHeader;
            _protectedHeaders = protectedHeader;
            _unprotectedHeaders = unprotectedHeader;
            _isTagged = isTagged;
        }

        /// <summary>
        /// Gets the content of this message or <see langword="null"/> if the content was detached from the message.
        /// </summary>
        /// <value>A region of memory that contains the content of this message or <see langword="null"/> if the content was detached from the message.</value>
        // Sign and MAC also refer to the content as payload.
        // Encrypt also refers to the content as cyphertext.
        public ReadOnlyMemory<byte>? Content
        {
            get
            {
                if (IsDetached)
                {
                    return null;
                }

                return _content;
            }
        }

        [MemberNotNullWhen(false, nameof(Content))]
        internal bool IsDetached => _content == null;

        /// <summary>
        /// Decodes a CBOR payload as a COSE_Sign1 message.
        /// </summary>
        /// <param name="cborPayload">The sequence of bytes to decode.</param>
        /// <returns>The decoded message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="cborPayload"/> is <see langword="null"/>.</exception>
        /// <exception cref="CryptographicException"><paramref name="cborPayload"/> could not be decoded as a COSE_Sign1 message.</exception>
        public static CoseSign1Message DecodeSign1(byte[] cborPayload)
        {
            if (cborPayload is null)
                throw new ArgumentNullException(nameof(cborPayload));

            return DecodeCoseSign1Core(new CborReader(cborPayload));
        }

        /// <summary>
        /// Decodes a CBOR payload as a COSE_Sign1 message.
        /// </summary>
        /// <param name="cborPayload">The sequence of CBOR-encoded bytes to decode.</param>
        /// <returns>The decoded message.</returns>
        /// <exception cref="CryptographicException"><paramref name="cborPayload"/> could not be decoded as a COSE_Sign1 message.</exception>
        public static CoseSign1Message DecodeSign1(ReadOnlySpan<byte> cborPayload)
        {
            unsafe
            {
                fixed (byte* ptr = &MemoryMarshal.GetReference(cborPayload))
                {
                    using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, cborPayload.Length))
                    {
                        return DecodeCoseSign1Core(new CborReader(manager.Memory));
                    }
                }
            }
        }

        private static CoseSign1Message DecodeCoseSign1Core(CborReader reader)
        {
            try
            {
                CborTag? tag = DecodeTag(reader);
                if (tag != null && tag != Sign1Tag)
                {
                    throw new CryptographicException(SR.Format(SR.DecodeSign1IncorrectTag, tag));
                }

                ReadOnlyMemory<byte> coseSignArray = reader.ReadEncodedValue();

                if (reader.BytesRemaining != 0)
                {
                    throw new CryptographicException(SR.Format(SR.DecodeErrorWhileDecoding, SR.DecodeMessageContainedTrailingData));
                }

                reader = new CborReader(coseSignArray);

                int? arrayLength = reader.ReadStartArray();
                if (arrayLength.HasValue ? arrayLength != CoseSign1Message.Sign1ArrayLength :
                    HasIndefiniteLengthArrayIncorrectLength(coseSignArray, CoseSign1Message.Sign1ArrayLength))
                {
                    throw new CryptographicException(SR.Format(SR.DecodeErrorWhileDecoding, SR.DecodeSign1ArrayLengthMustBeFour));
                }

                var protectedHeader = new CoseHeaderMap();
                DecodeProtectedBucket(reader, protectedHeader, out byte[] protectedHeaderAsBstr);

                var unprotectedHeader = new CoseHeaderMap();
                DecodeUnprotectedBucket(reader, unprotectedHeader);

                if (ContainDuplicateLabels(protectedHeader, unprotectedHeader))
                {
                    throw new CryptographicException(SR.Sign1SignHeaderDuplicateLabels);
                }

                byte[]? payload = DecodePayload(reader);
                byte[] signature = DecodeSignature(reader);
                reader.ReadEndArray();

                Debug.Assert(reader.BytesRemaining == 0);

                return new CoseSign1Message(protectedHeader, unprotectedHeader, payload, signature, protectedHeaderAsBstr, tag.HasValue);
            }
            catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
            {
                throw new CryptographicException(SR.DecodeErrorWhileDecodingSeeInnerEx, ex);
            }
        }

        /// <summary>
        /// Decodes a CBOR payload as a COSE_Sign message.
        /// </summary>
        /// <param name="cborPayload">The sequence of bytes to decode.</param>
        /// <returns>The decoded message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="cborPayload"/> is <see langword="null"/>.</exception>
        /// <exception cref="CryptographicException"><paramref name="cborPayload"/> could not be decoded as a COSE_Sign message.</exception>
        public static CoseMultiSignMessage DecodeMultiSign(byte[] cborPayload)
        {
            if (cborPayload is null)
                throw new ArgumentNullException(nameof(cborPayload));

            return DecodeCoseMultiSignCore(new CborReader(cborPayload));
        }

        /// <summary>
        /// Decodes a CBOR payload as a COSE_Sign message.
        /// </summary>
        /// <param name="cborPayload">The sequence of bytes to decode.</param>
        /// <returns>The decoded message.</returns>
        /// <exception cref="CryptographicException"><paramref name="cborPayload"/> could not be decoded as a COSE_Sign message.</exception>
        public static CoseMultiSignMessage DecodeMultiSign(ReadOnlySpan<byte> cborPayload)
        {
            unsafe
            {
                fixed (byte* ptr = &MemoryMarshal.GetReference(cborPayload))
                {
                    using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, cborPayload.Length))
                    {
                        return DecodeCoseMultiSignCore(new CborReader(manager.Memory));
                    }
                }
            }
        }

        private static CoseMultiSignMessage DecodeCoseMultiSignCore(CborReader reader)
        {
            try
            {
                CborTag? tag = DecodeTag(reader);
                if (tag != null && tag != MultiSignTag)
                {
                    throw new CryptographicException(SR.Format(SR.DecodeMultiSignIncorrectTag, tag));
                }

                ReadOnlyMemory<byte> coseSignArray = reader.ReadEncodedValue();

                if (reader.BytesRemaining != 0)
                {
                    throw new CryptographicException(SR.Format(SR.DecodeErrorWhileDecoding, SR.DecodeMessageContainedTrailingData));
                }

                reader = new CborReader(coseSignArray);

                int? arrayLength = reader.ReadStartArray();
                if (arrayLength.HasValue ? arrayLength != CoseMultiSignMessage.MultiSignArrayLength :
                    HasIndefiniteLengthArrayIncorrectLength(coseSignArray, CoseMultiSignMessage.MultiSignArrayLength))
                {
                    throw new CryptographicException(SR.Format(SR.DecodeErrorWhileDecoding, SR.DecodeMultiSignArrayLengthMustBeFour));
                }

                var protectedHeaders = new CoseHeaderMap();
                DecodeProtectedBucket(reader, protectedHeaders, out byte[] encodedProtectedHeaders);

                var unprotectedHeaders = new CoseHeaderMap();
                DecodeUnprotectedBucket(reader, unprotectedHeaders);

                if (ContainDuplicateLabels(protectedHeaders, unprotectedHeaders))
                {
                    throw new CryptographicException(SR.Sign1SignHeaderDuplicateLabels);
                }

                byte[]? payload = DecodePayload(reader);
                List<CoseSignature> signatures = DecodeCoseSignaturesArray(reader);

                reader.ReadEndArray();
                Debug.Assert(reader.BytesRemaining == 0);

                return new CoseMultiSignMessage(protectedHeaders, unprotectedHeaders, payload, signatures, encodedProtectedHeaders, tag.HasValue);
            }
            catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
            {
                throw new CryptographicException(SR.DecodeErrorWhileDecodingSeeInnerEx, ex);
            }
        }

        private static CborTag? DecodeTag(CborReader reader)
        {
            return reader.PeekState() switch
            {
                CborReaderState.Tag => reader.ReadTag(),
                _ => null
            };
        }

        private static void DecodeProtectedBucket(CborReader reader, CoseHeaderMap headerParameters, out byte[] protectedHeaderAsBstr)
        {
            protectedHeaderAsBstr = reader.ReadByteString();
            if (protectedHeaderAsBstr.Length == 0)
            {
                return;
            }

            var protectedHeaderReader = new CborReader(protectedHeaderAsBstr);
            DecodeBucket(protectedHeaderReader, headerParameters);

            if (MissingCriticalHeaders(headerParameters, out string? labelName))
            {
                throw new CryptographicException(SR.Format(SR.CriticalHeaderMissing, labelName));
            }

            headerParameters.IsReadOnly = true;

            if (protectedHeaderReader.BytesRemaining != 0)
            {
                throw new CryptographicException(SR.Format(SR.DecodeErrorWhileDecoding, SR.DecodeSign1EncodedProtectedMapIncorrect));
            }
        }

        private static void DecodeUnprotectedBucket(CborReader reader, CoseHeaderMap headerParameters)
        {
            DecodeBucket(reader, headerParameters);
        }

        private static void DecodeBucket(CborReader reader, CoseHeaderMap headerParameters)
        {
            int? length = reader.ReadStartMap();
            for (int i = 0; i < length; i++)
            {
                CoseHeaderLabel label = reader.PeekState() switch
                {
                    CborReaderState.UnsignedInteger or CborReaderState.NegativeInteger => new CoseHeaderLabel(reader.ReadInt32()),
                    CborReaderState.TextString => new CoseHeaderLabel(reader.ReadTextString()),
                    _ => throw new CryptographicException(SR.Format(SR.DecodeErrorWhileDecoding, SR.DecodeSign1MapLabelWasIncorrect))
                };

                CoseHeaderValue value = CoseHeaderValue.FromEncodedValue(reader.ReadEncodedValue().Span);
                headerParameters.Add(label, value);
            }
            reader.ReadEndMap();
        }

        private static byte[]? DecodePayload(CborReader reader)
        {
            CborReaderState state = reader.PeekState();
            if (state == CborReaderState.Null)
            {
                reader.ReadNull();
                return null;
            }
            if (state == CborReaderState.ByteString)
            {
                return reader.ReadByteString();
            }

            throw new CryptographicException(SR.Format(SR.DecodeErrorWhileDecoding, SR.DecodeSign1PayloadWasIncorrect));
        }

        private static byte[] DecodeSignature(CborReader reader)
        {
            return reader.ReadByteString();
        }

        private static List<CoseSignature> DecodeCoseSignaturesArray(CborReader reader)
        {
            int? signaturesLength = reader.ReadStartArray();
            List<CoseSignature> signatures = new List<CoseSignature>(signaturesLength.GetValueOrDefault());

            while (reader.PeekState() == CborReaderState.StartArray)
            {
                CoseSignature signature = DecodeCoseSignature(reader.ReadEncodedValue());
                signatures.Add(signature);
            }

            reader.ReadEndArray();

            if (signatures.Count < 1)
            {
                throw new CryptographicException(SR.Format(SR.DecodeErrorWhileDecoding, SR.MultiSignMessageMustCarryAtLeastOneSignature));
            }

            return signatures;
        }

        private static CoseSignature DecodeCoseSignature(ReadOnlyMemory<byte> coseSignature)
        {
            var reader = new CborReader(coseSignature);
            int? length = reader.ReadStartArray();

            if (length.HasValue ? length != CoseMultiSignMessage.CoseSignatureArrayLength :
                HasIndefiniteLengthArrayIncorrectLength(coseSignature, CoseMultiSignMessage.CoseSignatureArrayLength))
            {
                throw new CryptographicException(SR.Format(SR.DecodeErrorWhileDecoding, SR.DecodeCoseSignatureMustBeArrayOfThree));
            }

            var protectedHeaders = new CoseHeaderMap();
            DecodeProtectedBucket(reader, protectedHeaders, out byte[] signProtected);

            var unprotectedHeaders = new CoseHeaderMap();
            DecodeUnprotectedBucket(reader, unprotectedHeaders);

            if (ContainDuplicateLabels(protectedHeaders, unprotectedHeaders))
            {
                throw new CryptographicException(SR.Sign1SignHeaderDuplicateLabels);
            }

            byte[] signatureBytes = DecodeSignature(reader);
            reader.ReadEndArray();

            return new CoseSignature(protectedHeaders, unprotectedHeaders, signProtected, signatureBytes);
        }

        private static bool HasIndefiniteLengthArrayIncorrectLength(ReadOnlyMemory<byte> encodedArray, int expectedLength)
        {
            var reader = new CborReader(encodedArray);
            reader.ReadStartArray();
            int count = 0;

            while (reader.PeekState() != CborReaderState.EndArray)
            {
                reader.SkipValue();
                count++;

                if (count > expectedLength)
                {
                    return true;
                }
            }

            bool retVal = count != expectedLength;
            reader.ReadEndArray();
            Debug.Assert(reader.BytesRemaining == 0);

            return retVal;
        }

        internal static void AppendToBeSigned(
            Span<byte> buffer,
            IncrementalHash hasher,
            SigStructureContext context,
            ReadOnlySpan<byte> bodyProtected,
            ReadOnlySpan<byte> signProtected,
            ReadOnlySpan<byte> associatedData,
            ReadOnlySpan<byte> contentBytes,
            Stream? contentStream)
        {
            int bytesWritten = CreateToBeSigned(buffer, context, bodyProtected, signProtected, associatedData, ReadOnlySpan<byte>.Empty);
            bytesWritten -= 1; // Trim the empty bstr content, it is just a placeholder.

            hasher.AppendData(buffer.Slice(0, bytesWritten));

            if (contentStream == null)
            {
                // content length
                CoseHelpers.WriteByteStringLength(hasher, (ulong)contentBytes.Length);

                //content
                hasher.AppendData(contentBytes);
            }
            else
            {
                // content length
                CoseHelpers.WriteByteStringLength(hasher, (ulong)(contentStream.Length - contentStream.Position));

                //content
                byte[] contentBuffer = ArrayPool<byte>.Shared.Rent(4096);
                int bytesRead;

                try
                {
                    while ((bytesRead = contentStream.Read(contentBuffer, 0, contentBuffer.Length)) > 0)
                    {
                        hasher.AppendData(contentBuffer, 0, bytesRead);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(contentBuffer, clearArray: true);
                }
            }
        }

        internal static async Task AppendToBeSignedAsync(
            byte[] buffer,
            IncrementalHash hasher,
            SigStructureContext context,
            ReadOnlyMemory<byte> bodyProtected,
            ReadOnlyMemory<byte> signProtected,
            ReadOnlyMemory<byte> associatedData,
            Stream content,
            CancellationToken cancellationToken)
        {
            int bytesWritten = CreateToBeSigned(buffer, context, bodyProtected.Span, signProtected.Span, associatedData.Span, ReadOnlySpan<byte>.Empty);
            bytesWritten -= 1; // Trim the empty bstr content, it is just a placeholder.

            hasher.AppendData(buffer, 0, bytesWritten);

            //content length
            CoseHelpers.WriteByteStringLength(hasher, (ulong)(content.Length - content.Position));

            // content
            byte[] contentBuffer = ArrayPool<byte>.Shared.Rent(4096);
            int bytesRead;
#if NETSTANDARD2_0 || NETFRAMEWORK
            while ((bytesRead = await content.ReadAsync(contentBuffer, 0, contentBuffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
#else
            while ((bytesRead = await content.ReadAsync(contentBuffer, cancellationToken).ConfigureAwait(false)) > 0)
#endif
            {
                hasher.AppendData(contentBuffer, 0, bytesRead);
            }

            ArrayPool<byte>.Shared.Return(contentBuffer, clearArray: true);
        }

        internal static int CreateToBeSigned(Span<byte> destination, SigStructureContext context, ReadOnlySpan<byte> bodyProtected, ReadOnlySpan<byte> signProtected, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> content)
        {
            var writer = new CborWriter();
            if (context == SigStructureContext.Signature)
            {
                writer.WriteStartArray(5);
                writer.WriteTextString(SigStructureContextSign);
                writer.WriteByteString(bodyProtected);
                writer.WriteByteString(signProtected);
            }
            else
            {
                Debug.Assert(context == SigStructureContext.Signature1);
                Debug.Assert(signProtected.Length == 0);
                writer.WriteStartArray(4);
                writer.WriteTextString(SigStructureContextSign1);
                writer.WriteByteString(bodyProtected);
            }

            writer.WriteByteString(associatedData);
            writer.WriteByteString(content);
            writer.WriteEndArray();

            int bytesWritten = writer.Encode(destination);
            Debug.Assert(bytesWritten == ComputeToBeSignedEncodedSize(context, bodyProtected.Length, signProtected.Length, associatedData.Length, content.Length));

            return bytesWritten;
        }

        internal static int ComputeToBeSignedEncodedSize(SigStructureContext context, int bodyProtectedLength, int signProtectedLength, int associatedDataLength, int contentLength)
        {
            int encodedSize = CoseHelpers.SizeOfArrayOfLessThan24 +
                CoseHelpers.GetByteStringEncodedSize(bodyProtectedLength) +
                CoseHelpers.GetByteStringEncodedSize(associatedDataLength) +
                CoseHelpers.GetByteStringEncodedSize(contentLength);

            if (context == SigStructureContext.Signature)
            {
                encodedSize += SizeOfSigStructureCtxSign +
                    CoseHelpers.GetByteStringEncodedSize(signProtectedLength);
            }
            else
            {
                Debug.Assert(context == SigStructureContext.Signature1);
                Debug.Assert(signProtectedLength == 0);
                encodedSize += SizeOfSigStructureCtxSign1;
            }

            return encodedSize;
        }

        // Validate duplicate labels https://datatracker.ietf.org/doc/html/rfc8152#section-3.
        internal static bool ContainDuplicateLabels(CoseHeaderMap? protectedHeaders, CoseHeaderMap? unprotectedHeaders)
        {
            if (protectedHeaders == null || unprotectedHeaders == null)
            {
                return false;
            }

            foreach (KeyValuePair<CoseHeaderLabel, CoseHeaderValue> kvp in protectedHeaders)
            {
                if (unprotectedHeaders.ContainsKey(kvp.Key))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool MissingCriticalHeaders(CoseHeaderMap? protectedHeders, out string? labelName)
        {
            if (protectedHeders == null ||
                !protectedHeders.TryGetValue(CoseHeaderLabel.CriticalHeaders, out CoseHeaderValue critHeaderValue))
            {
                labelName = null;
                return false;
            }

            var reader = new CborReader(critHeaderValue.EncodedValue);
            int length = reader.ReadStartArray().GetValueOrDefault();
            Debug.Assert(length > 0);

            for (int i = 0; i < length; i++)
            {
                CoseHeaderLabel label = reader.PeekState() switch
                {
                    CborReaderState.UnsignedInteger or CborReaderState.NegativeInteger => new CoseHeaderLabel(reader.ReadInt32()),
                    CborReaderState.TextString => new CoseHeaderLabel(reader.ReadTextString()),
                    _ => throw new CryptographicException(SR.CriticalHeadersLabelWasIncorrect)
                };

                if (!protectedHeders.ContainsKey(label))
                {
                    labelName = label.LabelName;
                    return true;
                }
            }

            labelName = null;
            return false;
        }

        /// <summary>
        /// Encodes this message as CBOR.
        /// </summary>
        /// <returns>The message encoded as CBOR.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="ProtectedHeaders"/> and <see cref="UnprotectedHeaders"/> collections have one or more labels in common.</exception>
        public byte[] Encode()
        {
            byte[] buffer = new byte[GetEncodedLength()];
            int bytesWritten = Encode(buffer);
            Debug.Assert(bytesWritten == buffer.Length);

            return buffer;
        }

        /// <summary>
        /// Encodes this message as CBOR.
        /// </summary>
        /// <param name="destination">The buffer in which to write the encoded value.</param>
        /// <returns>The number of bytes written to <paramref name="destination"/>.</returns>
        /// <remarks>Use <see cref="GetEncodedLength()"/> to determine how many bytes result in encoding this message.</remarks>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too small to hold the value.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="ProtectedHeaders"/> and <see cref="UnprotectedHeaders"/> collections have one or more labels in common.</exception>
        /// <seealso cref="GetEncodedLength()"/>
        public int Encode(Span<byte> destination)
        {
            if (!TryEncode(destination, out int bytesWritten))
            {
                throw new ArgumentException(SR.Argument_EncodeDestinationTooSmall, nameof(destination));
            }

            return bytesWritten;
        }

        /// <summary>
        /// When overriden in a derived class, attempts to encode this message into the specified buffer.
        /// </summary>
        /// <param name="destination">The buffer in which to write the encoded value.</param>
        /// <param name="bytesWritten">On success, receives the number of bytes written to <paramref name="destination"/>. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true"/> if <paramref name="destination"/> had sufficient length to receive the value; otherwise, <see langword="false"/>.</returns>
        /// <remarks>Use <see cref="GetEncodedLength()"/> to determine how many bytes result in encoding this message.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="ProtectedHeaders"/> and <see cref="UnprotectedHeaders"/> collections have one or more labels in common.</exception>
        /// <seealso cref="GetEncodedLength()"/>
        public abstract bool TryEncode(Span<byte> destination, out int bytesWritten);

        /// <summary>
        /// When overriden in a derived class, calculates the number of bytes produced by encoding this <see cref="CoseMessage"/>.
        /// </summary>
        /// <returns>The number of bytes produced by encoding this message.</returns>
        public abstract int GetEncodedLength();
    }
}

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
    public abstract class CoseMessage
    {
        private const string SigStructureContextSign = "Signature";
        private const string SigStructureContextSign1 = "Signature1";
        internal static readonly int SizeOfSigStructureCtxSign = CoseHelpers.GetTextStringEncodedSize(SigStructureContextSign);
        internal static readonly int SizeOfSigStructureCtxSign1 = CoseHelpers.GetTextStringEncodedSize(SigStructureContextSign1);

        // COSE tags https://datatracker.ietf.org/doc/html/rfc8152#page-8 Table 1.
        internal const CborTag Sign1Tag = (CborTag)18;
        internal const CborTag MultiSignTag = (CborTag)98;

        internal byte[]? _content;
        internal byte[] _protectedHeaderAsBstr;
        internal bool _isTagged;

        private CoseHeaderMap _protectedHeaders;
        private CoseHeaderMap _unprotectedHeaders;
        public CoseHeaderMap ProtectedHeaders => _protectedHeaders;
        public CoseHeaderMap UnprotectedHeaders => _unprotectedHeaders;

        internal CoseMessage(CoseHeaderMap protectedHeader, CoseHeaderMap unprotectedHeader, byte[]? content, byte[] encodedProtectedHeader, bool isTagged)
        {
            _content = content;
            _protectedHeaderAsBstr = encodedProtectedHeader;
            _protectedHeaders = protectedHeader;
            _unprotectedHeaders = unprotectedHeader;
            _isTagged = isTagged;
        }

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

        public static CoseSign1Message DecodeSign1(byte[] cborPayload)
        {
            if (cborPayload is null)
                throw new ArgumentNullException(nameof(cborPayload));

            return DecodeCoseSign1Core(new CborReader(cborPayload));
        }

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

                int? arrayLength = reader.ReadStartArray();
                if (arrayLength != 4)
                {
                    throw new CryptographicException(SR.Format(SR.DecodeErrorWhileDecoding, SR.DecodeSign1ArrayLengthMustBeFour));
                }

                var protectedHeader = new CoseHeaderMap();
                DecodeProtectedBucket(reader, protectedHeader, out byte[] protectedHeaderAsBstr);

                var unprotectedHeader = new CoseHeaderMap();
                DecodeUnprotectedBucket(reader, unprotectedHeader);

                ThrowIfDuplicateLabels(protectedHeader, unprotectedHeader);

                byte[]? payload = DecodePayload(reader);
                byte[] signature = DecodeSignature(reader);
                reader.ReadEndArray();

                if (reader.BytesRemaining != 0)
                {
                    throw new CryptographicException(SR.Format(SR.DecodeErrorWhileDecoding, SR.DecodeSign1MessageContainedTrailingData));
                }

                return new CoseSign1Message(protectedHeader, unprotectedHeader, payload, signature, protectedHeaderAsBstr, tag.HasValue);
            }
            catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
            {
                throw new CryptographicException(SR.DecodeErrorWhileDecodingSeeInnerEx, ex);
            }
        }

        public static CoseMultiSignMessage DecodeMultiSign(byte[] cborPayload)
        {
            if (cborPayload is null)
                throw new ArgumentNullException(nameof(cborPayload));

            return DecodeCoseMultiSignCore(new CborReader(cborPayload));
        }

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

                int? arrayLength = reader.ReadStartArray();
                if (arrayLength != 4)
                {
                    throw new CryptographicException(SR.Format(SR.DecodeErrorWhileDecoding, SR.DecodeSign1ArrayLengthMustBeFour));
                }

                var protectedHeaders = new CoseHeaderMap();
                DecodeProtectedBucket(reader, protectedHeaders, out byte[] encodedProtectedHeaders);

                var unprotectedHeaders = new CoseHeaderMap();
                DecodeUnprotectedBucket(reader, unprotectedHeaders);

                ThrowIfDuplicateLabels(protectedHeaders, unprotectedHeaders);

                byte[]? payload = DecodePayload(reader);
                List<CoseSignature> signatures = DecodeCoseSignaturesArray(reader, encodedProtectedHeaders);

                reader.ReadEndArray();

                if (reader.BytesRemaining != 0)
                {
                    throw new CryptographicException(SR.Format(SR.DecodeErrorWhileDecoding, SR.DecodeSign1MessageContainedTrailingData));
                }

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
            ThrowIfMissingCriticalHeaders(headerParameters);
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

        private static List<CoseSignature> DecodeCoseSignaturesArray(CborReader reader, byte[] bodyProtected)
        {
            int? signaturesLength = reader.ReadStartArray();

            if (signaturesLength.GetValueOrDefault() < 1)
            {
                throw new CryptographicException(SR.Format(SR.DecodeErrorWhileDecoding, SR.MultiSignMessageMustCarryAtLeastOneSignature));
            }

            List<CoseSignature> signatures = new List<CoseSignature>(signaturesLength!.Value);

            for (int i = 0; i < signaturesLength; i++)
            {
                int? length = reader.ReadStartArray();

                if (length != CoseMultiSignMessage.CoseSignatureArrayLength)
                {
                    throw new CryptographicException(SR.Format(SR.DecodeErrorWhileDecoding, SR.DecodeCoseSignatureMustBeArrayOfThree));
                }

                var protectedHeaders = new CoseHeaderMap();
                DecodeProtectedBucket(reader, protectedHeaders, out byte[] signProtected);

                var unprotectedHeaders = new CoseHeaderMap();
                DecodeUnprotectedBucket(reader, unprotectedHeaders);

                ThrowIfDuplicateLabels(protectedHeaders, unprotectedHeaders);

                byte[] signatureBytes = DecodeSignature(reader);

                signatures.Add(new CoseSignature(protectedHeaders, unprotectedHeaders, bodyProtected, signProtected, signatureBytes));

                reader.ReadEndArray();
            }
            reader.ReadEndArray();

            return signatures;
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
        internal static void ThrowIfDuplicateLabels(CoseHeaderMap? protectedHeaders, CoseHeaderMap? unprotectedHeaders)
        {
            if (protectedHeaders == null || unprotectedHeaders == null)
            {
                return;
            }

            foreach (KeyValuePair<CoseHeaderLabel, CoseHeaderValue> kvp in protectedHeaders)
            {
                if (unprotectedHeaders.ContainsKey(kvp.Key))
                {
                    throw new CryptographicException(SR.Sign1SignHeaderDuplicateLabels);
                }
            }
        }

        internal static void ThrowIfMissingCriticalHeaders(CoseHeaderMap? protectedHeders)
        {
            if (protectedHeders == null ||
                !protectedHeders.TryGetValue(CoseHeaderLabel.CriticalHeaders, out CoseHeaderValue critHeaderValue))
            {
                return;
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
                    throw new CryptographicException(SR.Format(SR.CriticalHeaderMissing, label.LabelName));
                }
            }
        }

        public byte[] Encode()
        {
            byte[] buffer = new byte[GetEncodedLength()];
            int bytesWritten = Encode(buffer);
            Debug.Assert(bytesWritten == buffer.Length);

            return buffer;
        }

        public int Encode(Span<byte> destination)
        {
            if (!TryEncode(destination, out int bytesWritten))
            {
                throw new ArgumentException(SR.Argument_EncodeDestinationTooSmall, nameof(destination));
            }

            return bytesWritten;
        }

        public abstract bool TryEncode(Span<byte> destination, out int bytesWritten);

        public abstract int GetEncodedLength();
    }
}

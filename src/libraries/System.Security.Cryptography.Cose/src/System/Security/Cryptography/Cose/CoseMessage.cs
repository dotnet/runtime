// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Cbor;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography.Cose
{
    public abstract class CoseMessage
    {
        private const byte EmptyStringByte = 0xa0;
        internal const int SizeOfArrayOfFour = 1;

        // COSE tags https://datatracker.ietf.org/doc/html/rfc8152#page-8 Table 1.
        internal const CborTag Sign1Tag = (CborTag)18;

        internal byte[]? _content;
        internal byte[] _signature;
        internal byte[] _protectedHeaderAsBstr;

        private CoseHeaderMap _protectedHeaders;
        private CoseHeaderMap _unprotectedHeaders;
        public CoseHeaderMap ProtectedHeaders => _protectedHeaders;
        public CoseHeaderMap UnprotectedHeaders => _unprotectedHeaders;

        internal CoseMessage(CoseHeaderMap protectedHeader, CoseHeaderMap unprotectedHeader, byte[]? content, byte[] signature, byte[] encodedProtectedHeader)
        {
            _content = content;
            _signature = signature;
            _protectedHeaderAsBstr = encodedProtectedHeader;
            _protectedHeaders = protectedHeader;
            _unprotectedHeaders = unprotectedHeader;
        }

        // Sign and MAC also refer to the content as payload.
        // Encrypt also refers to the content as cyphertext.
        public ReadOnlyMemory<byte>? Content
        {
            get
            {
                if (_content  != null)
                {
                    return _content;
                }

                return null;
            }
        }

        public static CoseSign1Message DecodeSign1(byte[] cborPayload!!)
            => DecodeCoseSign1Core(new CborReader(cborPayload));

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
                    throw new CryptographicException(SR.Format(SR.DecodeSign1ErrorWhileDecoding, SR.DecodeSign1ArrayLengthMustBeFour));
                }

                var protectedHeader = new CoseHeaderMap();
                DecodeProtectedBucket(reader, protectedHeader, out byte[] protectedHeaderAsBstr);
                protectedHeader.IsReadOnly = true;

                var unprotectedHeader = new CoseHeaderMap();
                DecodeUnprotectedBucket(reader, unprotectedHeader);

                ThrowIfDuplicateLabels(protectedHeader, unprotectedHeader);

                byte[]? payload = DecodePayload(reader);
                byte[] signature = DecodeSignature(reader);
                reader.ReadEndArray();

                if (reader.BytesRemaining != 0)
                {
                    throw new CryptographicException(SR.Format(SR.DecodeSign1ErrorWhileDecoding, SR.DecodeSign1MesageContainedTrailingData));
                }

                return new CoseSign1Message(protectedHeader, unprotectedHeader, payload, signature, protectedHeaderAsBstr);
            }
            catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
            {
                throw new CryptographicException(SR.DecodeSign1ErrorWhileDecodingSeeInnerEx, ex);
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
                throw new CryptographicException(SR.Format(SR.DecodeSign1ErrorWhileDecoding, SR.DecodeSign1EncodedProtectedMapIncorrect));
            }
            else if (protectedHeaderAsBstr.Length == 1 && protectedHeaderAsBstr[0] == EmptyStringByte)
            {
                return;
            }

            var protectedHeaderReader = new CborReader(protectedHeaderAsBstr);
            DecodeBucket(protectedHeaderReader, headerParameters);

            if (protectedHeaderReader.BytesRemaining != 0)
            {
                throw new CryptographicException(SR.Format(SR.DecodeSign1ErrorWhileDecoding, SR.DecodeSign1EncodedProtectedMapIncorrect));
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
                    _ => throw new CryptographicException(SR.Format(SR.DecodeSign1ErrorWhileDecoding, SR.DecodeSign1MapLabelWasIncorrect))
                };
                headerParameters.SetEncodedValue(label, reader.ReadEncodedValue());
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

            throw new CryptographicException(SR.Format(SR.DecodeSign1ErrorWhileDecoding, SR.DecodeSign1PayloadWasIncorrect));
        }

        private static byte[] DecodeSignature(CborReader reader)
        {
            return reader.ReadByteString();
        }

        internal static void AppendToBeSigned(Span<byte> buffer, IncrementalHash hasher, string context, ReadOnlySpan<byte> encodedProtectedHeader, ReadOnlySpan<byte> contentBytes, Stream? contentStream, HashAlgorithmName hashAlgorithm)
        {
            int bytesWritten = CreateToBeSigned(buffer, context, encodedProtectedHeader, ReadOnlySpan<byte>.Empty);
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

        internal static async Task AppendToBeSignedAsync(byte[] buffer, IncrementalHash hasher, string context, ReadOnlyMemory<byte> encodedProtectedHeader, Stream content, HashAlgorithmName hashAlgorithm, CancellationToken cancellationToken)
        {
            int bytesWritten = CreateToBeSigned(buffer, context, encodedProtectedHeader.Span, ReadOnlySpan<byte>.Empty);
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

        internal static int CreateToBeSigned(Span<byte> destination, string context, ReadOnlySpan<byte> encodedProtectedHeader, ReadOnlySpan<byte> content)
        {
            var writer = new CborWriter();
            writer.WriteStartArray(4);
            writer.WriteTextString(context); // context
            writer.WriteByteString(encodedProtectedHeader); // body_protected
            writer.WriteByteString(Span<byte>.Empty); // external_aad
            writer.WriteByteString(content); // content
            writer.WriteEndArray();

            return writer.Encode(destination);
        }

        internal static int ComputeToBeSignedEncodedSize(string context, ReadOnlySpan<byte> encodedProtectedHeader, ReadOnlySpan<byte> content)
            => SizeOfArrayOfFour +
            CoseHelpers.GetTextStringEncodedSize(context) +
            CoseHelpers.GetByteStringEncodedSize(encodedProtectedHeader.Length) +
            CoseHelpers.GetByteStringEncodedSize(Span<byte>.Empty.Length) +
            CoseHelpers.GetByteStringEncodedSize(content.Length);

        // Validate duplicate labels https://datatracker.ietf.org/doc/html/rfc8152#section-3.
        internal static void ThrowIfDuplicateLabels(CoseHeaderMap? protectedHeaders, CoseHeaderMap? unprotectedHeaders)
        {
            if (protectedHeaders == null || unprotectedHeaders == null)
            {
                return;
            }

            foreach ((CoseHeaderLabel Label, ReadOnlyMemory<byte>) header in protectedHeaders)
            {
                if (unprotectedHeaders.TryGetEncodedValue(header.Label, out _))
                {
                    throw new CryptographicException(SR.Sign1SignHeaderDuplicateLabels);
                }
            }
        }

        internal enum KeyType
        {
            ECDsa,
            RSA,
        }

        internal static KeyType GetKeyType(AsymmetricAlgorithm key)
        {
            return key switch
            {
                ECDsa => KeyType.ECDsa,
                RSA => KeyType.RSA,
                _ => throw new CryptographicException(SR.Format(SR.Sign1UnsupportedKey, key.GetType()))
            };
        }
    }
}

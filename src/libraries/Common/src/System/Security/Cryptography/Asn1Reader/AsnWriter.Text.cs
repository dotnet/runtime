// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    internal sealed partial class AsnWriter
    {
        /// <summary>
        ///   Write the provided string using the specified encoding type using the UNIVERSAL
        ///   tag corresponding to the encoding type.
        /// </summary>
        /// <param name="encodingType">
        ///   The <see cref="UniversalTagNumber"/> corresponding to the encoding to use.
        /// </param>
        /// <param name="str">The string to write.</param>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is <c>null</c></exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a restricted character string encoding type --OR--
        ///   <paramref name="encodingType"/> is a restricted character string encoding type that is not
        ///   currently supported by this method
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="WriteCharacterString(Asn1Tag,UniversalTagNumber,string)"/>
        public void WriteCharacterString(UniversalTagNumber encodingType, string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            WriteCharacterString(encodingType, str.AsSpan());
        }

        /// <summary>
        ///   Write the provided string using the specified encoding type using the UNIVERSAL
        ///   tag corresponding to the encoding type.
        /// </summary>
        /// <param name="encodingType">
        ///   The <see cref="UniversalTagNumber"/> corresponding to the encoding to use.
        /// </param>
        /// <param name="str">The string to write.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a restricted character string encoding type --OR--
        ///   <paramref name="encodingType"/> is a restricted character string encoding type that is not
        ///   currently supported by this method
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="WriteCharacterString(Asn1Tag,UniversalTagNumber,ReadOnlySpan{char})"/>
        public void WriteCharacterString(UniversalTagNumber encodingType, ReadOnlySpan<char> str)
        {
            Text.Encoding encoding = AsnCharacterStringEncodings.GetEncoding(encodingType);

            WriteCharacterStringCore(new Asn1Tag(encodingType), encoding, str);
        }

        /// <summary>
        ///   Write the provided string using the specified encoding type using the specified
        ///   tag corresponding to the encoding type.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <param name="encodingType">
        ///   The <see cref="UniversalTagNumber"/> corresponding to the encoding to use.
        /// </param>
        /// <param name="str">The string to write.</param>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is <c>null</c></exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a restricted character string encoding type --OR--
        ///   <paramref name="encodingType"/> is a restricted character string encoding type that is not
        ///   currently supported by this method
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteCharacterString(Asn1Tag tag, UniversalTagNumber encodingType, string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            WriteCharacterString(tag, encodingType, str.AsSpan());
        }

        /// <summary>
        ///   Write the provided string using the specified encoding type using the specified
        ///   tag corresponding to the encoding type.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <param name="encodingType">
        ///   The <see cref="UniversalTagNumber"/> corresponding to the encoding to use.
        /// </param>
        /// <param name="str">The string to write.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a restricted character string encoding type --OR--
        ///   <paramref name="encodingType"/> is a restricted character string encoding type that is not
        ///   currently supported by this method
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteCharacterString(Asn1Tag tag, UniversalTagNumber encodingType, ReadOnlySpan<char> str)
        {
            CheckUniversalTag(tag, encodingType);

            Text.Encoding encoding = AsnCharacterStringEncodings.GetEncoding(encodingType);
            WriteCharacterStringCore(tag, encoding, str);
        }

        // T-REC-X.690-201508 sec 8.23
        private void WriteCharacterStringCore(Asn1Tag tag, Text.Encoding encoding, ReadOnlySpan<char> str)
        {
            int size = -1;

            // T-REC-X.690-201508 sec 9.2
            if (RuleSet == AsnEncodingRules.CER)
            {
                // TODO: Split this for netstandard vs netcoreapp for span?.
                unsafe
                {
                    fixed (char* strPtr = &MemoryMarshal.GetReference(str))
                    {
                        size = encoding.GetByteCount(strPtr, str.Length);

                        // If it exceeds the primitive segment size, use the constructed encoding.
                        if (size > AsnReader.MaxCERSegmentSize)
                        {
                            WriteConstructedCerCharacterString(tag, encoding, str, size);
                            return;
                        }
                    }
                }
            }

            // TODO: Split this for netstandard vs netcoreapp for span?.
            unsafe
            {
                fixed (char* strPtr = &MemoryMarshal.GetReference(str))
                {
                    if (size < 0)
                    {
                        size = encoding.GetByteCount(strPtr, str.Length);
                    }

                    // Clear the constructed tag, if present.
                    WriteTag(tag.AsPrimitive());
                    WriteLength(size);
                    Span<byte> dest = _buffer.AsSpan(_offset, size);

                    fixed (byte* destPtr = &MemoryMarshal.GetReference(dest))
                    {
                        int written = encoding.GetBytes(strPtr, str.Length, destPtr, dest.Length);

                        if (written != size)
                        {
                            Debug.Fail($"Encoding produced different answer for GetByteCount ({size}) and GetBytes ({written})");
                            throw new InvalidOperationException();
                        }
                    }

                    _offset += size;
                }
            }
        }

        private void WriteConstructedCerCharacterString(Asn1Tag tag, Text.Encoding encoding, ReadOnlySpan<char> str, int size)
        {
            Debug.Assert(size > AsnReader.MaxCERSegmentSize);

            byte[] tmp;

            // TODO: Split this for netstandard vs netcoreapp for span?.
            var localPool = ArrayPool<byte>.Shared;
            unsafe
            {
                fixed (char* strPtr = &MemoryMarshal.GetReference(str))
                {
                    tmp = localPool.Rent(size);

                    fixed (byte* destPtr = tmp)
                    {
                        int written = encoding.GetBytes(strPtr, str.Length, destPtr, tmp.Length);

                        if (written != size)
                        {
                            Debug.Fail(
                                $"Encoding produced different answer for GetByteCount ({size}) and GetBytes ({written})");
                            throw new InvalidOperationException();
                        }
                    }
                }
            }

            WriteConstructedCerOctetString(tag, tmp.AsSpan(0, size));
            Array.Clear(tmp, 0, size);
            localPool.Return(tmp);
        }
    }
}

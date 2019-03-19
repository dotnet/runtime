// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Numerics;

namespace System.Security.Cryptography.Asn1
{
    internal sealed partial class AsnWriter
    {
        /// <summary>
        ///   Write an Object Identifier with a specified tag.
        /// </summary>
        /// <param name="oid">The object identifier to write.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="oid"/> is <c>null</c>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="oid"/>.<see cref="Oid.Value"/> is not a valid dotted decimal
        ///   object identifier
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteObjectIdentifier(Oid oid)
        {
            if (oid == null)
                throw new ArgumentNullException(nameof(oid));

            CheckDisposed();

            if (oid.Value == null)
                throw new CryptographicException(SR.Argument_InvalidOidValue);

            WriteObjectIdentifier(oid.Value);
        }

        /// <summary>
        ///   Write an Object Identifier with a specified tag.
        /// </summary>
        /// <param name="oidValue">The object identifier to write.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="oidValue"/> is <c>null</c>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="oidValue"/> is not a valid dotted decimal
        ///   object identifier
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteObjectIdentifier(string oidValue)
        {
            if (oidValue == null)
                throw new ArgumentNullException(nameof(oidValue));

            WriteObjectIdentifier(oidValue.AsSpan());
        }

        /// <summary>
        ///   Write an Object Identifier with tag UNIVERSAL 6.
        /// </summary>
        /// <param name="oidValue">The object identifier to write.</param>
        /// <exception cref="CryptographicException">
        ///   <paramref name="oidValue"/> is not a valid dotted decimal
        ///   object identifier
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteObjectIdentifier(ReadOnlySpan<char> oidValue)
        {
            WriteObjectIdentifierCore(Asn1Tag.ObjectIdentifier, oidValue);
        }

        /// <summary>
        ///   Write an Object Identifier with a specified tag.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <param name="oid">The object identifier to write.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="oid"/> is <c>null</c>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="oid"/>.<see cref="Oid.Value"/> is not a valid dotted decimal
        ///   object identifier
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteObjectIdentifier(Asn1Tag tag, Oid oid)
        {
            if (oid == null)
                throw new ArgumentNullException(nameof(oid));

            CheckUniversalTag(tag, UniversalTagNumber.ObjectIdentifier);
            CheckDisposed();

            if (oid.Value == null)
                throw new CryptographicException(SR.Argument_InvalidOidValue);

            WriteObjectIdentifier(tag, oid.Value);
        }

        /// <summary>
        ///   Write an Object Identifier with a specified tag.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <param name="oidValue">The object identifier to write.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="oidValue"/> is <c>null</c>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="oidValue"/> is not a valid dotted decimal
        ///   object identifier
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteObjectIdentifier(Asn1Tag tag, string oidValue)
        {
            if (oidValue == null)
                throw new ArgumentNullException(nameof(oidValue));

            WriteObjectIdentifier(tag, oidValue.AsSpan());
        }

        /// <summary>
        ///   Write an Object Identifier with a specified tag.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <param name="oidValue">The object identifier to write.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="oidValue"/> is not a valid dotted decimal
        ///   object identifier
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteObjectIdentifier(Asn1Tag tag, ReadOnlySpan<char> oidValue)
        {
            CheckUniversalTag(tag, UniversalTagNumber.ObjectIdentifier);

            WriteObjectIdentifierCore(tag.AsPrimitive(), oidValue);
        }


        // T-REC-X.690-201508 sec 8.19
        private void WriteObjectIdentifierCore(Asn1Tag tag, ReadOnlySpan<char> oidValue)
        {
            CheckDisposed();

            // T-REC-X.690-201508 sec 8.19.4
            // The first character is in { 0, 1, 2 }, the second will be a '.', and a third (digit)
            // will also exist.
            if (oidValue.Length < 3)
                throw new CryptographicException(SR.Argument_InvalidOidValue);
            if (oidValue[1] != '.')
                throw new CryptographicException(SR.Argument_InvalidOidValue);

            // The worst case is "1.1.1.1.1", which takes 4 bytes (5 components, with the first two condensed)
            // Longer numbers get smaller: "2.1.127" is only 2 bytes. (81d (0x51) and 127 (0x7F))
            // So length / 2 should prevent any reallocations.
            var localPool = ArrayPool<byte>.Shared;
            byte[] tmp = localPool.Rent(oidValue.Length / 2);
            int tmpOffset = 0;

            try
            {
                int firstComponent;

                switch (oidValue[0])
                {
                    case '0':
                        firstComponent = 0;
                        break;
                    case '1':
                        firstComponent = 1;
                        break;
                    case '2':
                        firstComponent = 2;
                        break;
                    default:
                        throw new CryptographicException(SR.Argument_InvalidOidValue);
                }

                // The first two components are special:
                // ITU X.690 8.19.4:
                //   The numerical value of the first subidentifier is derived from the values of the first two
                //   object identifier components in the object identifier value being encoded, using the formula:
                //       (X*40) + Y
                //   where X is the value of the first object identifier component and Y is the value of the
                //   second object identifier component.
                //       NOTE - This packing of the first two object identifier components recognizes that only
                //          three values are allocated from the root node, and at most 39 subsequent values from
                //          nodes reached by X = 0 and X = 1.

                // skip firstComponent and the trailing .
                ReadOnlySpan<char> remaining = oidValue.Slice(2);

                BigInteger subIdentifier = ParseSubIdentifier(ref remaining);
                subIdentifier += 40 * firstComponent;

                int localLen = EncodeSubIdentifier(tmp.AsSpan(tmpOffset), ref subIdentifier);
                tmpOffset += localLen;

                while (!remaining.IsEmpty)
                {
                    subIdentifier = ParseSubIdentifier(ref remaining);
                    localLen = EncodeSubIdentifier(tmp.AsSpan(tmpOffset), ref subIdentifier);
                    tmpOffset += localLen;
                }

                Debug.Assert(!tag.IsConstructed);
                WriteTag(tag);
                WriteLength(tmpOffset);
                Buffer.BlockCopy(tmp, 0, _buffer, _offset, tmpOffset);
                _offset += tmpOffset;
            }
            finally
            {
                Array.Clear(tmp, 0, tmpOffset);
                localPool.Return(tmp);
            }
        }

        private static BigInteger ParseSubIdentifier(ref ReadOnlySpan<char> oidValue)
        {
            int endIndex = oidValue.IndexOf('.');

            if (endIndex == -1)
            {
                endIndex = oidValue.Length;
            }
            else if (endIndex == 0 || endIndex == oidValue.Length - 1)
            {
                throw new CryptographicException(SR.Argument_InvalidOidValue);
            }

            // The following code is equivalent to
            // BigInteger.TryParse(temp, NumberStyles.None, CultureInfo.InvariantCulture, out value)
            // TODO: Split this for netstandard vs netcoreapp for span-perf?.
            BigInteger value = BigInteger.Zero;

            for (int position = 0; position < endIndex; position++)
            {
                if (position > 0 && value == 0)
                {
                    // T-REC X.680-201508 sec 12.26
                    throw new CryptographicException(SR.Argument_InvalidOidValue);
                }

                value *= 10;
                value += AtoI(oidValue[position]);
            }

            oidValue = oidValue.Slice(Math.Min(oidValue.Length, endIndex + 1));
            return value;
        }

        private static int AtoI(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }

            throw new CryptographicException(SR.Argument_InvalidOidValue);
        }

        // ITU-T-X.690-201508 sec 8.19.5
        private static int EncodeSubIdentifier(Span<byte> dest, ref BigInteger subIdentifier)
        {
            Debug.Assert(dest.Length > 0);

            if (subIdentifier.IsZero)
            {
                dest[0] = 0;
                return 1;
            }

            BigInteger unencoded = subIdentifier;
            int idx = 0;

            do
            {
                BigInteger cur = unencoded & 0x7F;
                byte curByte = (byte)cur;

                if (subIdentifier != unencoded)
                {
                    curByte |= 0x80;
                }

                unencoded >>= 7;
                dest[idx] = curByte;
                idx++;
            }
            while (unencoded != BigInteger.Zero);

            Reverse(dest.Slice(0, idx));
            return idx;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    public class UniqueId
    {
        private long _idLow;
        private long _idHigh;
        private string? _s;
        private const int guidLength = 16;
        private const int uuidLength = 45;

        private static ReadOnlySpan<short> Char2val =>
        [
            /*    0-15 */
                              0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
            /*   16-31 */
                              0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
            /*   32-47 */
                              0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
            /*   48-63 */
                              0x000, 0x010, 0x020, 0x030, 0x040, 0x050, 0x060, 0x070, 0x080, 0x090, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
            /*   64-79 */
                              0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
            /*   80-95 */
                              0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
            /*  96-111 */
                              0x100, 0x0A0, 0x0B0, 0x0C0, 0x0D0, 0x0E0, 0x0F0, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
            /* 112-127 */
                              0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,

            /*    0-15 */
                              0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
            /*   16-31 */
                              0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
            /*   32-47 */
                              0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
            /*   48-63 */
                              0x000, 0x001, 0x002, 0x003, 0x004, 0x005, 0x006, 0x007, 0x008, 0x009, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
            /*   64-79 */
                              0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
            /*   80-95 */
                              0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
            /*  96-111 */
                              0x100, 0x00A, 0x00B, 0x00C, 0x00D, 0x00E, 0x00F, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
            /* 112-127 */
                              0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
        ];

        public UniqueId() : this(Guid.NewGuid())
        {
        }

        public UniqueId(Guid guid) : this(guid.ToByteArray())
        {
        }

        public UniqueId(byte[] guid) : this(guid, 0)
        {
        }

        public UniqueId(byte[] guid, int offset)
        {
            ArgumentNullException.ThrowIfNull(guid);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > guid.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, guid.Length));
            if (guidLength > guid.Length - offset)
                throw new ArgumentException(SR.Format(SR.XmlArrayTooSmallInput, guidLength), nameof(guid));

            ReadOnlySpan<byte> source = guid.AsSpan(offset, guidLength);
            _idLow = BinaryPrimitives.ReadInt64LittleEndian(source);
            _idHigh = BinaryPrimitives.ReadInt64LittleEndian(source.Slice(8));
        }

        public UniqueId(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.Length == 0)
                throw new FormatException(SR.XmlInvalidUniqueId);
            Parse(value);
            _s = value;
        }

        public UniqueId(char[] chars, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(chars);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, chars.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > chars.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, chars.Length - offset));
            if (count == 0)
                throw new FormatException(SR.XmlInvalidUniqueId);
            Parse(chars.AsSpan(offset, count));
            if (!IsGuid)
            {
                _s = new string(chars, offset, count);
            }
        }


        public int CharArrayLength
        {
            get
            {
                if (_s != null)
                    return _s.Length;

                return uuidLength;
            }
        }

        private static int Decode(ReadOnlySpan<short> char2val, char ch1, char ch2)
        {
            if ((ch1 | ch2) >= 0x80)
                return 0x100;

            return char2val[ch1] | char2val[0x80 + ch2];
        }

        private static void Encode(byte b, Span<char> chars, int offset)
        {
            chars[offset] = HexConverter.ToCharLower(b >> 4);
            chars[offset + 1] = HexConverter.ToCharLower(b);
        }

        public bool IsGuid => ((_idLow | _idHigh) != 0);

        private void Parse(ReadOnlySpan<char> chars)
        {
            //           1         2         3         4
            // 012345678901234567890123456789012345678901234
            // urn:uuid:xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

            if (chars.Length != uuidLength ||
                chars[0] != 'u' || chars[1] != 'r' || chars[2] != 'n' || chars[3] != ':' ||
                chars[4] != 'u' || chars[5] != 'u' || chars[6] != 'i' || chars[7] != 'd' || chars[8] != ':' ||
                chars[17] != '-' || chars[22] != '-' || chars[27] != '-' || chars[32] != '-')
            {
                return;
            }

            Span<byte> bytes = stackalloc byte[guidLength];
            ReadOnlySpan<short> char2val = Char2val;

            //   0         1         2         3         4
            //   012345678901234567890123456789012345678901234
            //   urn:uuid:aabbccdd-eeff-gghh-0011-223344556677
            //
            //   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5
            //   ddccbbaaffeehhgg0011223344556677

            int i;
            int j = 0;
            i = Decode(char2val, chars[15], chars[16]); bytes[0] = (byte)i; j |= i;
            i = Decode(char2val, chars[13], chars[14]); bytes[1] = (byte)i; j |= i;
            i = Decode(char2val, chars[11], chars[12]); bytes[2] = (byte)i; j |= i;
            i = Decode(char2val, chars[9], chars[10]); bytes[3] = (byte)i; j |= i;
            i = Decode(char2val, chars[20], chars[21]); bytes[4] = (byte)i; j |= i;
            i = Decode(char2val, chars[18], chars[19]); bytes[5] = (byte)i; j |= i;
            i = Decode(char2val, chars[25], chars[26]); bytes[6] = (byte)i; j |= i;
            i = Decode(char2val, chars[23], chars[24]); bytes[7] = (byte)i; j |= i;
            i = Decode(char2val, chars[28], chars[29]); bytes[8] = (byte)i; j |= i;
            i = Decode(char2val, chars[30], chars[31]); bytes[9] = (byte)i; j |= i;
            i = Decode(char2val, chars[33], chars[34]); bytes[10] = (byte)i; j |= i;
            i = Decode(char2val, chars[35], chars[36]); bytes[11] = (byte)i; j |= i;
            i = Decode(char2val, chars[37], chars[38]); bytes[12] = (byte)i; j |= i;
            i = Decode(char2val, chars[39], chars[40]); bytes[13] = (byte)i; j |= i;
            i = Decode(char2val, chars[41], chars[42]); bytes[14] = (byte)i; j |= i;
            i = Decode(char2val, chars[43], chars[44]); bytes[15] = (byte)i; j |= i;

            if (j >= 0x100)
                return;

            _idLow = BinaryPrimitives.ReadInt64LittleEndian(bytes);
            _idHigh = BinaryPrimitives.ReadInt64LittleEndian(bytes.Slice(8));
        }

        public int ToCharArray(char[] chars, int offset)
        {
            ArgumentNullException.ThrowIfNull(chars);

            int count = CharArrayLength;

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, chars.Length));

            if (count > chars.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(chars), SR.Format(SR.XmlArrayTooSmallOutput, count));

            ToSpan(chars.AsSpan(offset, count));
            return count;
        }

        private void ToSpan(Span<char> chars)
        {
            if (_s != null)
            {
                _s.CopyTo(chars);
            }
            else
            {
                Span<byte> bytes = stackalloc byte[guidLength];
                BinaryPrimitives.WriteInt64LittleEndian(bytes, _idLow);
                BinaryPrimitives.WriteInt64LittleEndian(bytes.Slice(8), _idHigh);

                // Force a single bounds check up front so the indexed writes below are bounds-check-free.
                chars = chars.Slice(0, uuidLength);
                chars[0] = 'u';
                chars[1] = 'r';
                chars[2] = 'n';
                chars[3] = ':';
                chars[4] = 'u';
                chars[5] = 'u';
                chars[6] = 'i';
                chars[7] = 'd';
                chars[8] = ':';
                chars[17] = '-';
                chars[22] = '-';
                chars[27] = '-';
                chars[32] = '-';

                Encode(bytes[0], chars, 15);
                Encode(bytes[1], chars, 13);
                Encode(bytes[2], chars, 11);
                Encode(bytes[3], chars, 9);
                Encode(bytes[4], chars, 20);
                Encode(bytes[5], chars, 18);
                Encode(bytes[6], chars, 25);
                Encode(bytes[7], chars, 23);
                Encode(bytes[8], chars, 28);
                Encode(bytes[9], chars, 30);
                Encode(bytes[10], chars, 33);
                Encode(bytes[11], chars, 35);
                Encode(bytes[12], chars, 37);
                Encode(bytes[13], chars, 39);
                Encode(bytes[14], chars, 41);
                Encode(bytes[15], chars, 43);
            }
        }

        public bool TryGetGuid(out Guid guid)
        {
            byte[] buffer = new byte[guidLength];
            if (!TryGetGuid(buffer, 0))
            {
                guid = Guid.Empty;
                return false;
            }

            guid = new Guid(buffer);
            return true;
        }

        public bool TryGetGuid(byte[] buffer, int offset)
        {
            if (!IsGuid)
                return false;

            ArgumentNullException.ThrowIfNull(buffer);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, buffer.Length));

            if (guidLength > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(buffer), SR.Format(SR.XmlArrayTooSmallOutput, guidLength));

            Span<byte> destination = buffer.AsSpan(offset, guidLength);
            BinaryPrimitives.WriteInt64LittleEndian(destination, _idLow);
            BinaryPrimitives.WriteInt64LittleEndian(destination.Slice(8), _idHigh);

            return true;
        }

        public override string ToString() =>
            _s ??= string.Create(CharArrayLength, this, (destination, thisRef) => thisRef.ToSpan(destination));

        public static bool operator ==(UniqueId? id1, UniqueId? id2)
        {
            if (object.ReferenceEquals(id1, id2))
                return true;

            if (id1 is null || id2 is null)
                return false;

            if (id1.IsGuid && id2.IsGuid)
            {
                return id1._idLow == id2._idLow && id1._idHigh == id2._idHigh;
            }

            return id1.ToString() == id2.ToString();
        }

        public static bool operator !=(UniqueId? id1, UniqueId? id2)
        {
            return !(id1 == id2);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return this == (obj as UniqueId);
        }

        public override int GetHashCode()
        {
            if (IsGuid)
            {
                long hash = (_idLow ^ _idHigh);
                return ((int)(hash >> 32)) ^ ((int)hash);
            }
            else
            {
                return ToString().GetHashCode();
            }
        }

    }
}

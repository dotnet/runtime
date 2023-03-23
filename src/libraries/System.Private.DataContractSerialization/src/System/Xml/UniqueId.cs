// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        private static ReadOnlySpan<short> Char2val => new short[256]
        {
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
        };

        public UniqueId() : this(Guid.NewGuid())
        {
        }

        public UniqueId(Guid guid) : this(guid.ToByteArray())
        {
        }

        public UniqueId(byte[] guid) : this(guid, 0)
        {
        }

        public unsafe UniqueId(byte[] guid, int offset)
        {
            ArgumentNullException.ThrowIfNull(guid);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > guid.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, guid.Length));
            if (guidLength > guid.Length - offset)
                throw new ArgumentException(SR.Format(SR.XmlArrayTooSmallInput, guidLength), nameof(guid));
            fixed (byte* pb = &guid[offset])
            {
                _idLow = UnsafeGetInt64(pb);
                _idHigh = UnsafeGetInt64(&pb[8]);
            }
        }

        public unsafe UniqueId(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.Length == 0)
                throw new FormatException(SR.XmlInvalidUniqueId);
            fixed (char* pch = value)
            {
                UnsafeParse(pch, value.Length);
            }
            _s = value;
        }

        public unsafe UniqueId(char[] chars, int offset, int count)
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
            fixed (char* pch = &chars[offset])
            {
                UnsafeParse(pch, count);
            }
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

        private unsafe int UnsafeDecode(short* char2val, char ch1, char ch2)
        {
            if ((ch1 | ch2) >= 0x80)
                return 0x100;

            return char2val[ch1] | char2val[0x80 + ch2];
        }

        private static unsafe void UnsafeEncode(byte b, char* pch)
        {
            pch[0] = HexConverter.ToCharLower(b >> 4);
            pch[1] = HexConverter.ToCharLower(b);
        }

        public bool IsGuid => ((_idLow | _idHigh) != 0);

        private unsafe void UnsafeParse(char* chars, int charCount)
        {
            //           1         2         3         4
            // 012345678901234567890123456789012345678901234
            // urn:uuid:xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

            if (charCount != uuidLength ||
                chars[0] != 'u' || chars[1] != 'r' || chars[2] != 'n' || chars[3] != ':' ||
                chars[4] != 'u' || chars[5] != 'u' || chars[6] != 'i' || chars[7] != 'd' || chars[8] != ':' ||
                chars[17] != '-' || chars[22] != '-' || chars[27] != '-' || chars[32] != '-')
            {
                return;
            }

            byte* bytes = stackalloc byte[guidLength];

            int i = 0;
            int j = 0;
            fixed (short* ps = &Char2val[0])
            {
                short* _char2val = ps;

                //   0         1         2         3         4
                //   012345678901234567890123456789012345678901234
                //   urn:uuid:aabbccdd-eeff-gghh-0011-223344556677
                //
                //   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5
                //   ddccbbaaffeehhgg0011223344556677

                i = UnsafeDecode(_char2val, chars[15], chars[16]); bytes[0] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[13], chars[14]); bytes[1] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[11], chars[12]); bytes[2] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[9], chars[10]); bytes[3] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[20], chars[21]); bytes[4] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[18], chars[19]); bytes[5] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[25], chars[26]); bytes[6] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[23], chars[24]); bytes[7] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[28], chars[29]); bytes[8] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[30], chars[31]); bytes[9] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[33], chars[34]); bytes[10] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[35], chars[36]); bytes[11] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[37], chars[38]); bytes[12] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[39], chars[40]); bytes[13] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[41], chars[42]); bytes[14] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[43], chars[44]); bytes[15] = (byte)i; j |= i;

                if (j >= 0x100)
                    return;

                _idLow = UnsafeGetInt64(bytes);
                _idHigh = UnsafeGetInt64(&bytes[8]);
            }
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

        private unsafe void ToSpan(Span<char> chars)
        {
            if (_s != null)
            {
                _s.CopyTo(chars);
            }
            else
            {
                byte* bytes = stackalloc byte[guidLength];
                UnsafeSetInt64(_idLow, bytes);
                UnsafeSetInt64(_idHigh, &bytes[8]);

                fixed (char* _pch = &chars[0])
                {
                    char* pch = _pch;
                    pch[0] = 'u';
                    pch[1] = 'r';
                    pch[2] = 'n';
                    pch[3] = ':';
                    pch[4] = 'u';
                    pch[5] = 'u';
                    pch[6] = 'i';
                    pch[7] = 'd';
                    pch[8] = ':';
                    pch[17] = '-';
                    pch[22] = '-';
                    pch[27] = '-';
                    pch[32] = '-';

                    UnsafeEncode(bytes[0], &pch[15]);
                    UnsafeEncode(bytes[1], &pch[13]);
                    UnsafeEncode(bytes[2], &pch[11]);
                    UnsafeEncode(bytes[3], &pch[9]);
                    UnsafeEncode(bytes[4], &pch[20]);
                    UnsafeEncode(bytes[5], &pch[18]);
                    UnsafeEncode(bytes[6], &pch[25]);
                    UnsafeEncode(bytes[7], &pch[23]);
                    UnsafeEncode(bytes[8], &pch[28]);
                    UnsafeEncode(bytes[9], &pch[30]);
                    UnsafeEncode(bytes[10], &pch[33]);
                    UnsafeEncode(bytes[11], &pch[35]);
                    UnsafeEncode(bytes[12], &pch[37]);
                    UnsafeEncode(bytes[13], &pch[39]);
                    UnsafeEncode(bytes[14], &pch[41]);
                    UnsafeEncode(bytes[15], &pch[43]);
                }
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

        public unsafe bool TryGetGuid(byte[] buffer, int offset)
        {
            if (!IsGuid)
                return false;

            ArgumentNullException.ThrowIfNull(buffer);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, buffer.Length));

            if (guidLength > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(buffer), SR.Format(SR.XmlArrayTooSmallOutput, guidLength));

            fixed (byte* pb = &buffer[offset])
            {
                UnsafeSetInt64(_idLow, pb);
                UnsafeSetInt64(_idHigh, &pb[8]);
            }

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

        private unsafe long UnsafeGetInt64(byte* pb)
        {
            int idLow = UnsafeGetInt32(pb);
            int idHigh = UnsafeGetInt32(&pb[4]);
            return (((long)idHigh) << 32) | ((uint)idLow);
        }

        private unsafe int UnsafeGetInt32(byte* pb)
        {
            int value = pb[3];
            value <<= 8;
            value |= pb[2];
            value <<= 8;
            value |= pb[1];
            value <<= 8;
            value |= pb[0];
            return value;
        }

        private unsafe void UnsafeSetInt64(long value, byte* pb)
        {
            UnsafeSetInt32((int)value, pb);
            UnsafeSetInt32((int)(value >> 32), &pb[4]);
        }

        private unsafe void UnsafeSetInt32(int value, byte* pb)
        {
            pb[0] = (byte)value;
            value >>= 8;
            pb[1] = (byte)value;
            value >>= 8;
            pb[2] = (byte)value;
            value >>= 8;
            pb[3] = (byte)value;
        }
    }
}

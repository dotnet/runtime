// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization; //For SR
using System.Text;

namespace System.Text
{
    internal sealed class Base64Encoding : Encoding
    {
        private static ReadOnlySpan<byte> Char2val => // 128
        [
            /*    0-15 */ 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            /*   16-31 */ 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            /*   32-47 */ 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,   62, 0xFF, 0xFF, 0xFF,   63,
            /*   48-63 */   52,   53,   54,   55,   56,   57,   58,   59,   60,   61, 0xFF, 0xFF, 0xFF,   64, 0xFF, 0xFF,
            /*   64-79 */ 0xFF,    0,    1,    2,    3,    4,    5,    6,    7,    8,    9,   10,   11,   12,   13,   14,
            /*   80-95 */   15,   16,   17,   18,   19,   20,   21,   22,   23,   24,   25, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            /*  96-111 */ 0xFF,   26,   27,   28,   29,   30,   31,   32,   33,   34,   35,   36,   37,   38,   39,   40,
            /* 112-127 */   41,   42,   43,   44,   45,   46,   47,   48,   49,   50,   51, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        ];

        private const string Val2Char = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        private static ReadOnlySpan<byte> Val2byte => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"u8;

        public override int GetMaxByteCount(int charCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(charCount);
            if ((charCount % 4) != 0)
                throw new FormatException(SR.Format(SR.XmlInvalidBase64Length, charCount.ToString()));
            return charCount / 4 * 3;
        }

        private static bool IsValidLeadBytes(int v1, int v2, int v3, int v4)
        {
            // First two chars of a four char base64 sequence can't be ==, and must be valid
            return ((v1 | v2) < 64) && ((v3 | v4) != 0xFF);
        }

        private static bool IsValidTailBytes(int v3, int v4)
        {
            // If the third char is = then the fourth char must be =
            return !(v3 == 64 && v4 != 64);
        }

        public override unsafe int GetByteCount(char[] chars, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(chars);

            ArgumentOutOfRangeException.ThrowIfNegative(index);
            if (index > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(index), SR.Format(SR.OffsetExceedsBufferSize, chars.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > chars.Length - index)
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, chars.Length - index));

            if (count == 0)
                return 0;
            if ((count % 4) != 0)
                throw new FormatException(SR.Format(SR.XmlInvalidBase64Length, count.ToString()));
            fixed (byte* _char2val = &Char2val[0])
            {
                fixed (char* _chars = &chars[index])
                {
                    int totalCount = 0;
                    char* pch = _chars;
                    char* pchMax = _chars + count;
                    while (pch < pchMax)
                    {
                        Debug.Assert(pch + 4 <= pchMax);
                        char pch0 = pch[0];
                        char pch1 = pch[1];
                        char pch2 = pch[2];
                        char pch3 = pch[3];

                        if ((pch0 | pch1 | pch2 | pch3) >= 128)
                            throw new FormatException(SR.Format(SR.XmlInvalidBase64Sequence, new string(pch, 0, 4), index + (int)(pch - _chars)));

                        // xx765432 xx107654 xx321076 xx543210
                        // 76543210 76543210 76543210
                        int v1 = _char2val[pch0];
                        int v2 = _char2val[pch1];
                        int v3 = _char2val[pch2];
                        int v4 = _char2val[pch3];

                        if (!IsValidLeadBytes(v1, v2, v3, v4) || !IsValidTailBytes(v3, v4))
                            throw new FormatException(SR.Format(SR.XmlInvalidBase64Sequence, new string(pch, 0, 4), index + (int)(pch - _chars)));

                        int byteCount = (v4 != 64 ? 3 : (v3 != 64 ? 2 : 1));
                        totalCount += byteCount;
                        pch += 4;
                    }
                    return totalCount;
                }
            }
        }

        public override unsafe int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            ArgumentNullException.ThrowIfNull(chars);
            ArgumentOutOfRangeException.ThrowIfNegative(charIndex);
            if (charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex), SR.Format(SR.OffsetExceedsBufferSize, chars.Length));

            ArgumentOutOfRangeException.ThrowIfNegative(charCount);
            if (charCount > chars.Length - charIndex)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.Format(SR.SizeExceedsRemainingBufferSpace, chars.Length - charIndex));

            ArgumentNullException.ThrowIfNull(bytes);
            ArgumentOutOfRangeException.ThrowIfNegative(byteIndex);
            if (byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.Format(SR.OffsetExceedsBufferSize, bytes.Length));

            if (charCount == 0)
                return 0;
            if ((charCount % 4) != 0)
                throw new FormatException(SR.Format(SR.XmlInvalidBase64Length, charCount.ToString()));
            fixed (byte* _char2val = &Char2val[0])
            {
                fixed (char* _chars = &chars[charIndex])
                {
                    fixed (byte* _bytes = &bytes[byteIndex])
                    {
                        char* pch = _chars;
                        char* pchMax = _chars + charCount;
                        byte* pb = _bytes;
                        byte* pbMax = _bytes + bytes.Length - byteIndex;
                        while (pch < pchMax)
                        {
                            Debug.Assert(pch + 4 <= pchMax);
                            char pch0 = pch[0];
                            char pch1 = pch[1];
                            char pch2 = pch[2];
                            char pch3 = pch[3];

                            if ((pch0 | pch1 | pch2 | pch3) >= 128)
                                throw new FormatException(SR.Format(SR.XmlInvalidBase64Sequence, new string(pch, 0, 4), charIndex + (int)(pch - _chars)));
                            // xx765432 xx107654 xx321076 xx543210
                            // 76543210 76543210 76543210

                            int v1 = _char2val[pch0];
                            int v2 = _char2val[pch1];
                            int v3 = _char2val[pch2];
                            int v4 = _char2val[pch3];

                            if (!IsValidLeadBytes(v1, v2, v3, v4) || !IsValidTailBytes(v3, v4))
                                throw new FormatException(SR.Format(SR.XmlInvalidBase64Sequence, new string(pch, 0, 4), charIndex + (int)(pch - _chars)));

                            int byteCount = (v4 != 64 ? 3 : (v3 != 64 ? 2 : 1));
                            if (pb + byteCount > pbMax)
                                throw new ArgumentException(SR.XmlArrayTooSmall, nameof(bytes));

                            pb[0] = (byte)((v1 << 2) | ((v2 >> 4) & 0x03));
                            if (byteCount > 1)
                            {
                                pb[1] = (byte)((v2 << 4) | ((v3 >> 2) & 0x0F));
                                if (byteCount > 2)
                                {
                                    pb[2] = (byte)((v3 << 6) | ((v4 >> 0) & 0x3F));
                                }
                            }
                            pb += byteCount;
                            pch += 4;
                        }
                        return (int)(pb - _bytes);
                    }
                }
            }
        }

        public unsafe int GetBytes(byte[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            ArgumentNullException.ThrowIfNull(chars);
            ArgumentOutOfRangeException.ThrowIfNegative(charIndex);
            if (charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex), SR.Format(SR.OffsetExceedsBufferSize, chars.Length));

            ArgumentOutOfRangeException.ThrowIfNegative(charCount);
            if (charCount > chars.Length - charIndex)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.Format(SR.SizeExceedsRemainingBufferSpace, chars.Length - charIndex));

            ArgumentNullException.ThrowIfNull(bytes);
            ArgumentOutOfRangeException.ThrowIfNegative(byteIndex);
            if (byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.Format(SR.OffsetExceedsBufferSize, bytes.Length));

            if (charCount == 0)
                return 0;
            if ((charCount % 4) != 0)
                throw new FormatException(SR.Format(SR.XmlInvalidBase64Length, charCount.ToString()));
            fixed (byte* _char2val = &Char2val[0])
            {
                fixed (byte* _chars = &chars[charIndex])
                {
                    fixed (byte* _bytes = &bytes[byteIndex])
                    {
                        byte* pch = _chars;
                        byte* pchMax = _chars + charCount;
                        byte* pb = _bytes;
                        byte* pbMax = _bytes + bytes.Length - byteIndex;
                        while (pch < pchMax)
                        {
                            Debug.Assert(pch + 4 <= pchMax);
                            byte pch0 = pch[0];
                            byte pch1 = pch[1];
                            byte pch2 = pch[2];
                            byte pch3 = pch[3];
                            if ((pch0 | pch1 | pch2 | pch3) >= 128)
                                throw new FormatException(SR.Format(SR.XmlInvalidBase64Sequence, "?", charIndex + (int)(pch - _chars)));
                            // xx765432 xx107654 xx321076 xx543210
                            // 76543210 76543210 76543210

                            int v1 = _char2val[pch0];
                            int v2 = _char2val[pch1];
                            int v3 = _char2val[pch2];
                            int v4 = _char2val[pch3];

                            if (!IsValidLeadBytes(v1, v2, v3, v4) || !IsValidTailBytes(v3, v4))
                                throw new FormatException(SR.Format(SR.XmlInvalidBase64Sequence, "?", charIndex + (int)(pch - _chars)));

                            int byteCount = (v4 != 64 ? 3 : (v3 != 64 ? 2 : 1));
                            if (pb + byteCount > pbMax)
                                throw new ArgumentException(SR.XmlArrayTooSmall, nameof(bytes));

                            pb[0] = (byte)((v1 << 2) | ((v2 >> 4) & 0x03));
                            if (byteCount > 1)
                            {
                                pb[1] = unchecked((byte)((v2 << 4) | ((v3 >> 2) & 0x0F)));
                                if (byteCount > 2)
                                {
                                    pb[2] = unchecked((byte)((v3 << 6) | ((v4 >> 0) & 0x3F)));
                                }
                            }
                            pb += byteCount;
                            pch += 4;
                        }
                        return (int)(pb - _bytes);
                    }
                }
            }
        }
        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0 || byteCount > int.MaxValue / 4 * 3 - 2)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.Format(SR.ValueMustBeInRange, 0, int.MaxValue / 4 * 3 - 2));
            return ((byteCount + 2) / 3) * 4;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return GetMaxCharCount(count);
        }

        public override unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            ArgumentOutOfRangeException.ThrowIfNegative(byteIndex);
            if (byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.Format(SR.OffsetExceedsBufferSize, bytes.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(byteCount);
            if (byteCount > bytes.Length - byteIndex)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.Format(SR.SizeExceedsRemainingBufferSpace, bytes.Length - byteIndex));

            int charCount = GetCharCount(bytes, byteIndex, byteCount);
            ArgumentNullException.ThrowIfNull(chars);
            ArgumentOutOfRangeException.ThrowIfNegative(charIndex);
            if (charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex), SR.Format(SR.OffsetExceedsBufferSize, chars.Length));
            if (charCount < 0 || charCount > chars.Length - charIndex)
                throw new ArgumentException(SR.XmlArrayTooSmall, nameof(chars));

            // We've computed exactly how many chars there are and verified that
            // there's enough space in the char buffer, so we can proceed without
            // checking the charCount.

            if (byteCount > 0)
            {
                fixed (char* _val2char = Val2Char)
                {
                    fixed (byte* _bytes = &bytes[byteIndex])
                    {
                        fixed (char* _chars = &chars[charIndex])
                        {
                            byte* pb = _bytes;
                            byte* pbMax = pb + byteCount - 3;
                            char* pch = _chars;

                            // Convert chunks of 3 bytes to 4 chars
                            while (pb <= pbMax)
                            {
                                // 76543210 76543210 76543210
                                // xx765432 xx107654 xx321076 xx543210

                                // Inspect the code carefully before you change this
                                pch[0] = _val2char[(pb[0] >> 2)];
                                pch[1] = _val2char[((pb[0] & 0x03) << 4) | (pb[1] >> 4)];
                                pch[2] = _val2char[((pb[1] & 0x0F) << 2) | (pb[2] >> 6)];
                                pch[3] = _val2char[pb[2] & 0x3F];

                                pb += 3;
                                pch += 4;
                            }

                            // Handle 1 or 2 trailing bytes
                            if (pb - pbMax == 2)
                            {
                                // 1 trailing byte
                                // 76543210 xxxxxxxx xxxxxxxx
                                // xx765432 xx10xxxx xxxxxxxx xxxxxxxx
                                pch[0] = _val2char[(pb[0] >> 2)];
                                pch[1] = _val2char[((pb[0] & 0x03) << 4)];
                                pch[2] = '=';
                                pch[3] = '=';
                            }
                            else if (pb - pbMax == 1)
                            {
                                // 2 trailing bytes
                                // 76543210 76543210 xxxxxxxx
                                // xx765432 xx107654 xx3210xx xxxxxxxx
                                pch[0] = _val2char[(pb[0] >> 2)];
                                pch[1] = _val2char[((pb[0] & 0x03) << 4) | (pb[1] >> 4)];
                                pch[2] = _val2char[((pb[1] & 0x0F) << 2)];
                                pch[3] = '=';
                            }
                            else
                            {
                                // 0 trailing bytes
                                Debug.Assert(pb - pbMax == 3);
                            }
                        }
                    }
                }
            }

            return charCount;
        }

        public unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount, byte[] chars, int charIndex)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            ArgumentOutOfRangeException.ThrowIfNegative(byteIndex);
            if (byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.Format(SR.OffsetExceedsBufferSize, bytes.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(byteCount);
            if (byteCount > bytes.Length - byteIndex)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.Format(SR.SizeExceedsRemainingBufferSpace, bytes.Length - byteIndex));

            int charCount = GetCharCount(bytes, byteIndex, byteCount);
            ArgumentNullException.ThrowIfNull(chars);
            ArgumentOutOfRangeException.ThrowIfNegative(charIndex);
            if (charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex), SR.Format(SR.OffsetExceedsBufferSize, chars.Length));

            if (charCount < 0 || charCount > chars.Length - charIndex)
                throw new ArgumentException(SR.XmlArrayTooSmall, nameof(chars));

            // We've computed exactly how many chars there are and verified that
            // there's enough space in the char buffer, so we can proceed without
            // checking the charCount.

            if (byteCount > 0)
            {
                fixed (byte* _val2byte = &Val2byte[0])
                {
                    fixed (byte* _bytes = &bytes[byteIndex])
                    {
                        fixed (byte* _chars = &chars[charIndex])
                        {
                            byte* pb = _bytes;
                            byte* pbMax = pb + byteCount - 3;
                            byte* pch = _chars;

                            // Convert chunks of 3 bytes to 4 chars
                            while (pb <= pbMax)
                            {
                                // 76543210 76543210 76543210
                                // xx765432 xx107654 xx321076 xx543210

                                // Inspect the code carefully before you change this
                                pch[0] = _val2byte[(pb[0] >> 2)];
                                pch[1] = _val2byte[((pb[0] & 0x03) << 4) | (pb[1] >> 4)];
                                pch[2] = _val2byte[((pb[1] & 0x0F) << 2) | (pb[2] >> 6)];
                                pch[3] = _val2byte[pb[2] & 0x3F];

                                pb += 3;
                                pch += 4;
                            }

                            // Handle 1 or 2 trailing bytes
                            if (pb - pbMax == 2)
                            {
                                // 1 trailing byte
                                // 76543210 xxxxxxxx xxxxxxxx
                                // xx765432 xx10xxxx xxxxxxxx xxxxxxxx
                                pch[0] = _val2byte[(pb[0] >> 2)];
                                pch[1] = _val2byte[((pb[0] & 0x03) << 4)];
                                pch[2] = (byte)'=';
                                pch[3] = (byte)'=';
                            }
                            else if (pb - pbMax == 1)
                            {
                                // 2 trailing bytes
                                // 76543210 76543210 xxxxxxxx
                                // xx765432 xx107654 xx3210xx xxxxxxxx
                                pch[0] = _val2byte[(pb[0] >> 2)];
                                pch[1] = _val2byte[((pb[0] & 0x03) << 4) | (pb[1] >> 4)];
                                pch[2] = _val2byte[((pb[1] & 0x0F) << 2)];
                                pch[3] = (byte)'=';
                            }
                            else
                            {
                                // 0 trailing bytes
                                Debug.Assert(pb - pbMax == 3);
                            }
                        }
                    }
                }
            }

            return charCount;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text;

namespace System.Text
{
    internal sealed class OSEncoder : Encoder
    {
        private char _charLeftOver;
        private readonly Encoding _encoding;

        private const char NULL_CHAR = (char)0;

        internal OSEncoder(Encoding encoding)
        {
            _encoding = encoding;
            Reset();
        }

        public override void Reset()
        {
            _charLeftOver = NULL_CHAR;
        }

        public override unsafe int GetByteCount(char[] chars, int index, int count, bool flush)
        {
            ArgumentNullException.ThrowIfNull(chars);

            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            if (chars.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(chars), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (chars.Length == 0 && (_charLeftOver == NULL_CHAR || !flush))
                return 0;

            fixed (char* pChar = chars)
            {
                char dummyChar;
                char* pBuffer = pChar == null ? &dummyChar : pChar + index;
                return GetByteCount(pBuffer, count, flush);
            }
        }

        private unsafe int ConvertWithLeftOverChar(char* chars, int count, byte* bytes, int byteCount)
        {
            Debug.Assert(_charLeftOver != NULL_CHAR);
            char* pTempBuffer = stackalloc char[2];
            pTempBuffer[0] = _charLeftOver;

            int index = 0;

            if (count > 0 && char.IsLowSurrogate(chars[0]))
            {
                pTempBuffer[1] = chars[0];
                index++;
            }

            int result = OSEncoding.WideCharToMultiByte(_encoding.CodePage, pTempBuffer, index + 1, bytes, byteCount);

            if (count - index > 0)
                result += OSEncoding.WideCharToMultiByte(
                                        _encoding.CodePage, chars + index,
                                        count - index,
                                        bytes == null ? null : bytes + result,
                                        bytes == null ? 0 : byteCount - result);

            return result;
        }

        public override unsafe int GetByteCount(char* chars, int count, bool flush)
        {
            ArgumentNullException.ThrowIfNull(chars);

            ArgumentOutOfRangeException.ThrowIfNegative(count);

            bool excludeLastChar = count > 0 && !flush && char.IsHighSurrogate(chars[count - 1]);

            if (excludeLastChar)
                count--;

            if (_charLeftOver == NULL_CHAR)
            {
                if (count <= 0)
                    return 0;

                return OSEncoding.WideCharToMultiByte(_encoding.CodePage, chars, count, null, 0);
            }

            // we have left over character
            if (count == 0 && !excludeLastChar && !flush)
                return 0;

            return ConvertWithLeftOverChar(chars, count, null, 0);
        }

        public override unsafe int GetBytes(char[] chars, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex, bool flush)
        {
            ArgumentNullException.ThrowIfNull(chars);
            ArgumentNullException.ThrowIfNull(bytes);

            ArgumentOutOfRangeException.ThrowIfNegative(charIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(charCount);

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);

            if (bytes.Length == 0)
                return 0;

            if (charCount == 0 && (_charLeftOver == NULL_CHAR || !flush))
                return 0;

            fixed (char* pChars = chars)
            fixed (byte* pBytes = &bytes[0])
            {
                char dummyChar;
                char* pBuffer = pChars == null ? &dummyChar : pChars + charIndex;

                return GetBytes(pBuffer, charCount, pBytes + byteIndex, bytes.Length - byteIndex, flush);
            }
        }

        public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, bool flush)
        {
            ArgumentNullException.ThrowIfNull(chars);
            ArgumentNullException.ThrowIfNull(bytes);

            ArgumentOutOfRangeException.ThrowIfNegative(byteCount);
            ArgumentOutOfRangeException.ThrowIfNegative(charCount);

            if (byteCount == 0)
                return 0;

            char lastChar = charCount > 0 && !flush && char.IsHighSurrogate(chars[charCount - 1]) ? chars[charCount - 1] : NULL_CHAR;

            if (lastChar != NULL_CHAR)
                charCount--;

            if (_charLeftOver == NULL_CHAR)
            {
                if (charCount <= 0)
                {
                    _charLeftOver = lastChar;
                    return 0;
                }

                int result = OSEncoding.WideCharToMultiByte(_encoding.CodePage, chars, charCount, bytes, byteCount);
                _charLeftOver = lastChar;
                return result;
            }

            // we have left over character
            if (charCount == 0 && lastChar == NULL_CHAR && !flush)
                return 0;

            int res = ConvertWithLeftOverChar(chars, charCount, bytes, byteCount);
            _charLeftOver = lastChar;
            return res;
        }

        public override unsafe void Convert(char[] chars, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex, int byteCount, bool flush,
                                              out int charsUsed, out int bytesUsed, out bool completed)
        {
            ArgumentNullException.ThrowIfNull(chars);
            ArgumentNullException.ThrowIfNull(bytes);

            ArgumentOutOfRangeException.ThrowIfNegative(charIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(charCount);

            ArgumentOutOfRangeException.ThrowIfNegative(byteIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(byteCount);

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (bytes.Length == 0 || (chars.Length == 0 && (_charLeftOver == NULL_CHAR || !flush)))
            {
                bytesUsed = 0;
                charsUsed = 0;
                completed = false;
                return;
            }

            fixed (char* pChars = chars)
            fixed (byte* pBytes = &bytes[0])
            {
                char dummyChar;
                char* pBuffer = pChars == null ? &dummyChar : pChars + charIndex;

                Convert(pBuffer, charCount, pBytes + byteIndex, byteCount, flush, out charsUsed, out bytesUsed, out completed);
            }
        }

        public override unsafe void Convert(char* chars, int charCount,
                                              byte* bytes, int byteCount, bool flush,
                                              out int charsUsed, out int bytesUsed, out bool completed)
        {
            ArgumentNullException.ThrowIfNull(chars);
            ArgumentNullException.ThrowIfNull(bytes);

            ArgumentOutOfRangeException.ThrowIfNegative(charCount);
            ArgumentOutOfRangeException.ThrowIfNegative(byteCount);

            int count = charCount;
            while (count > 0)
            {
                int returnedByteCount = GetByteCount(chars, count, flush);
                if (returnedByteCount <= byteCount)
                    break;

                count /= 2;
            }

            if (count > 0)
            {
                // note GetBytes can change the _charLeftOver state
                bytesUsed = GetBytes(chars, count, bytes, byteCount, flush);
                charsUsed = count;
                completed = _charLeftOver == NULL_CHAR && charCount == count;
                return;
            }

            bytesUsed = 0;
            charsUsed = 0;
            completed = false;
        }
    }
}

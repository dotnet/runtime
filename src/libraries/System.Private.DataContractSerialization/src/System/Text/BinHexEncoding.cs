// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime;

namespace System.Text
{
    internal class BinHexEncoding : Encoding
    {
        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ValueMustBeNonNegative);
            if ((charCount % 2) != 0)
                throw new FormatException(SR.Format(SR.XmlInvalidBinHexLength, charCount.ToString()));
            return charCount / 2;
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return GetMaxByteCount(count);
        }

        public unsafe override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (chars == null)
                throw new ArgumentNullException(nameof(chars));
            if (charIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(charIndex), SR.ValueMustBeNonNegative);
            if (charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex), SR.Format(SR.OffsetExceedsBufferSize, chars.Length));
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ValueMustBeNonNegative);
            if (charCount > chars.Length - charIndex)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.Format(SR.SizeExceedsRemainingBufferSpace, chars.Length - charIndex));
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (byteIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.ValueMustBeNonNegative);
            if (byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.Format(SR.OffsetExceedsBufferSize, bytes.Length));
            int byteCount = GetByteCount(chars, charIndex, charCount);
            if (byteCount < 0 || byteCount > bytes.Length - byteIndex)
                throw new ArgumentException(SR.XmlArrayTooSmall, nameof(bytes));
            if (charCount > 0)
            {
                if (!HexConverter.TryDecodeFromUtf16(chars.AsSpan(charIndex, charCount), bytes.AsSpan(byteIndex, byteCount), out int charsProcessed))
                {
                    int error = charsProcessed + charIndex;
                    throw new FormatException(SR.Format(SR.XmlInvalidBinHexSequence, new string(chars, error, 2), error));
                }
            }
            return byteCount;
        }
#if NO
        public override Encoder GetEncoder()
        {
            return new BufferedEncoder(this, 2);
        }
#endif
        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0 || byteCount > int.MaxValue / 2)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.Format(SR.ValueMustBeInRange, 0, int.MaxValue / 2));
            return byteCount * 2;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return GetMaxCharCount(count);
        }

        public unsafe override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (byteIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.ValueMustBeNonNegative);
            if (byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.Format(SR.OffsetExceedsBufferSize, bytes.Length));
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.ValueMustBeNonNegative);
            if (byteCount > bytes.Length - byteIndex)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.Format(SR.SizeExceedsRemainingBufferSpace, bytes.Length - byteIndex));
            int charCount = GetCharCount(bytes, byteIndex, byteCount);
            if (chars == null)
                throw new ArgumentNullException(nameof(chars));
            if (charIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(charIndex), SR.ValueMustBeNonNegative);
            if (charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex), SR.Format(SR.OffsetExceedsBufferSize, chars.Length));
            if (charCount < 0 || charCount > chars.Length - charIndex)
                throw new ArgumentException(SR.XmlArrayTooSmall, nameof(chars));
            if (byteCount > 0)
            {
                HexConverter.EncodeToUtf16(bytes.AsSpan(byteIndex, byteCount), chars.AsSpan(charIndex));
            }
            return charCount;
        }
    }
}

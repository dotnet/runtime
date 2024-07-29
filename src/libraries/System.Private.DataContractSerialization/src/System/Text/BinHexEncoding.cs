// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime;

namespace System.Text
{
    internal sealed class BinHexEncoding : Encoding
    {
        public override int GetMaxByteCount(int charCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(charCount);
            if ((charCount % 2) != 0)
                throw new FormatException(SR.Format(SR.XmlInvalidBinHexLength, charCount.ToString()));
            return charCount / 2;
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return GetMaxByteCount(count);
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
            if (byteCount > 0)
            {
                HexConverter.EncodeToUtf16(bytes.AsSpan(byteIndex, byteCount), chars.AsSpan(charIndex));
            }
            return charCount;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text
{
    //
    // Latin1Encoding is a simple override to optimize the GetString version of Latin1Encoding.
    // because of the best fit cases we can't do this when encoding the string, only when decoding
    //
    internal partial class Latin1Encoding : Encoding
    {
        // Used by Encoding.Latin1 for lazy initialization
        // The initialization code will not be run until a static member of the class is referenced
        internal static readonly Latin1EncodingSealed s_default = new Latin1EncodingSealed();

        // We only use the best-fit table, of which ASCII is a superset for us.
        public Latin1Encoding() : base(Encoding.ISO_8859_1)
        {
        }

        public override ReadOnlySpan<byte> Preamble => default;

        // Default fallback that we'll use.
        internal override void SetDefaultFallbacks()
        {
            // We use best-fit mappings by default when encoding.
            encoderFallback = EncoderLatin1BestFitFallback.SingletonInstance;
            decoderFallback = DecoderFallback.ReplacementFallback;
        }

        /*
         * GetByteCount - Each Latin-1 char narrows to exactly one byte,
         * but fallback mechanism must be consulted for non-Latin-1 chars.
         */

        public override unsafe int GetByteCount(char* chars, int count)
        {
            if (chars is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chars);
            }

            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            return GetByteCountCommon(chars, count);
        }

        public override unsafe int GetByteCount(char[] chars, int index, int count)
        {
            if (chars is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chars, ExceptionResource.ArgumentNull_Array);
            }

            if ((index | count) < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException((index < 0) ? ExceptionArgument.index : ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (chars.Length - index < count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.chars, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
            }

            fixed (char* pChars = chars)
            {
                return GetByteCountCommon(pChars + index, count);
            }
        }

        public override unsafe int GetByteCount(ReadOnlySpan<char> chars)
        {
            // It's ok for us to pass null pointers down to the workhorse below.

            fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
            {
                return GetByteCountCommon(charsPtr, chars.Length);
            }
        }

        public override unsafe int GetByteCount(string s)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }

            fixed (char* pChars = s)
            {
                return GetByteCountCommon(pChars, s.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe int GetByteCountCommon(char* pChars, int charCount)
        {
            // Common helper method for all non-EncoderNLS entry points to GetByteCount.
            // A modification of this method should be copied in to each of the supported encodings: ASCII, UTF8, UTF16, UTF32.

            Debug.Assert(charCount >= 0, "Caller shouldn't specify negative length buffer.");
            Debug.Assert(pChars is not null || charCount == 0, "Input pointer shouldn't be null if non-zero length specified.");

            // First call into the fast path.
            // Don't bother providing a fallback mechanism; our fast path doesn't use it.

            int totalByteCount = GetByteCountFast(pChars, charCount, fallback: null, out int charsConsumed);

            if (charsConsumed != charCount)
            {
                // If there's still data remaining in the source buffer, go down the fallback path.
                // We need to check for integer overflow since the fallback could change the required
                // output count in unexpected ways.

                totalByteCount += GetByteCountWithFallback(pChars, charCount, charsConsumed);
                if (totalByteCount < 0)
                {
                    ThrowConversionOverflow();
                }
            }

            return totalByteCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // called directly by GetByteCountCommon
        private protected sealed override unsafe int GetByteCountFast(char* pChars, int charsLength, EncoderFallback? fallback, out int charsConsumed)
        {
            // Can we short-circuit the entire calculation? If so, the output byte count
            // will exactly match the input char count. Otherwise we need to walk the
            // entire input and find the index of the first non-Latin-1 char.

            int byteCount = charsLength;

            if (!FallbackSupportsFastGetByteCount(fallback))
            {
                // Unrecognized fallback mechanism - count chars manually.

                byteCount = (int)Latin1Utility.GetIndexOfFirstNonLatin1Char(pChars, (uint)charsLength);
            }

            charsConsumed = byteCount;
            return byteCount;
        }

        public override int GetMaxByteCount(int charCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(charCount);

            // Characters would be # of characters + 1 in case high surrogate is ? * max fallback
            long byteCount = (long)charCount + 1;

            if (EncoderFallback.MaxCharCount > 1)
                byteCount *= EncoderFallback.MaxCharCount;

            // 1 to 1 for most characters.  Only surrogates with fallbacks have less.

            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ArgumentOutOfRange_GetByteCountOverflow);
            return (int)byteCount;
        }

        /*
         * GetBytes - Each Latin-1 char narrows to exactly one byte,
         * but fallback mechanism must be consulted for non-Latin-1 chars.
         */

        public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
        {
            if (chars is null || bytes is null)
            {
                ThrowHelper.ThrowArgumentNullException(
                    argument: (chars is null) ? ExceptionArgument.chars : ExceptionArgument.bytes,
                    resource: ExceptionResource.ArgumentNull_Array);
            }

            if ((charCount | byteCount) < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    argument: (charCount < 0) ? ExceptionArgument.charCount : ExceptionArgument.byteCount,
                    resource: ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            return GetBytesCommon(chars, charCount, bytes, byteCount);
        }

        public override unsafe int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (chars is null || bytes is null)
            {
                ThrowHelper.ThrowArgumentNullException(
                    argument: (chars is null) ? ExceptionArgument.chars : ExceptionArgument.bytes,
                    resource: ExceptionResource.ArgumentNull_Array);
            }

            if ((charIndex | charCount) < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    argument: (charIndex < 0) ? ExceptionArgument.charIndex : ExceptionArgument.charCount,
                    resource: ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (chars!.Length - charIndex < charCount)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.chars, ExceptionResource.ArgumentOutOfRange_IndexCount);
            }

            if ((uint)byteIndex > bytes!.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.byteIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            fixed (char* pChars = chars)
            fixed (byte* pBytes = bytes)
            {
                return GetBytesCommon(pChars + charIndex, charCount, pBytes + byteIndex, bytes.Length - byteIndex);
            }
        }

        public override unsafe int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
        {
            // It's ok for us to operate on null / empty spans.

            fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
            fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
            {
                return GetBytesCommon(charsPtr, chars.Length, bytesPtr, bytes.Length);
            }
        }

        /// <inheritdoc/>
        public override unsafe bool TryGetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, out int bytesWritten)
        {
            fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
            fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
            {
                int written = GetBytesCommon(charsPtr, chars.Length, bytesPtr, bytes.Length, throwForDestinationOverflow: false);
                if (written >= 0)
                {
                    bytesWritten = written;
                    return true;
                }

                bytesWritten = 0;
                return false;
            }
        }

        public override unsafe int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (s is null || bytes is null)
            {
                ThrowHelper.ThrowArgumentNullException(
                    argument: (s is null) ? ExceptionArgument.s : ExceptionArgument.bytes,
                    resource: ExceptionResource.ArgumentNull_Array);
            }

            if ((charIndex | charCount) < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    argument: (charIndex < 0) ? ExceptionArgument.charIndex : ExceptionArgument.charCount,
                    resource: ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (s!.Length - charIndex < charCount)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.s, ExceptionResource.ArgumentOutOfRange_IndexCount);
            }

            if ((uint)byteIndex > bytes!.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.byteIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            fixed (char* pChars = s)
            fixed (byte* pBytes = bytes)
            {
                return GetBytesCommon(pChars + charIndex, charCount, pBytes + byteIndex, bytes.Length - byteIndex);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe int GetBytesCommon(char* pChars, int charCount, byte* pBytes, int byteCount, bool throwForDestinationOverflow = true)
        {
            // Common helper method for all non-EncoderNLS entry points to GetBytes.
            // A modification of this method should be copied in to each of the supported encodings: ASCII, UTF8, UTF16, UTF32.

            Debug.Assert(charCount >= 0, "Caller shouldn't specify negative length buffer.");
            Debug.Assert(pChars is not null || charCount == 0, "Input pointer shouldn't be null if non-zero length specified.");
            Debug.Assert(byteCount >= 0, "Caller shouldn't specify negative length buffer.");
            Debug.Assert(pBytes is not null || byteCount == 0, "Input pointer shouldn't be null if non-zero length specified.");

            // First call into the fast path.

            int bytesWritten = GetBytesFast(pChars, charCount, pBytes, byteCount, out int charsConsumed);

            if (charsConsumed == charCount)
            {
                // All elements converted - return immediately.

                return bytesWritten;
            }
            else
            {
                // Simple narrowing conversion couldn't operate on entire buffer - invoke fallback.

                return GetBytesWithFallback(pChars, charCount, pBytes, byteCount, charsConsumed, bytesWritten, throwForDestinationOverflow);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // called directly by GetBytesCommon
        private protected sealed override unsafe int GetBytesFast(char* pChars, int charsLength, byte* pBytes, int bytesLength, out int charsConsumed)
        {
            int bytesWritten = (int)Latin1Utility.NarrowUtf16ToLatin1(pChars, pBytes, (uint)Math.Min(charsLength, bytesLength));

            charsConsumed = bytesWritten;
            return bytesWritten;
        }


        /*
         * GetCharCount - Each byte widens to exactly one char, preserving count.
         * We never consult the fallback mechanism during decoding.
         */

        public override unsafe int GetCharCount(byte* bytes, int count)
        {
            if (bytes is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes);
            }

            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            return count;
        }

        public override int GetCharCount(byte[] bytes)
        {
            if (bytes is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes);
            }

            return bytes.Length;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            if (bytes is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes, ExceptionResource.ArgumentNull_Array);
            }

            if ((index | count) < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException((index < 0) ? ExceptionArgument.index : ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (bytes.Length - index < count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.bytes, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
            }

            return count;
        }

        public override int GetCharCount(ReadOnlySpan<byte> bytes)
        {
            return bytes.Length;
        }

        private protected override unsafe int GetCharCountFast(byte* pBytes, int bytesLength, DecoderFallback? fallback, out int bytesConsumed)
        {
            // We never consult the fallback mechanism during GetChars.
            // A single byte is always widened to a single char, so we'll return
            // the byte count as the final char count.

            bytesConsumed = bytesLength;
            return bytesLength;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            // Unlike GetMaxByteCount, there won't be any pending surrogates for bytes -> chars conversions.

            if (byteCount < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.byteCount, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            return byteCount;
        }

        /*
         * GetChars - Each byte widens to exactly one char, preserving count.
         * We never consult the fallback mechanism during decoding.
         */

        public override unsafe int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        {
            if (bytes is null || chars is null)
            {
                ThrowHelper.ThrowArgumentNullException(
                    argument: (bytes is null) ? ExceptionArgument.bytes : ExceptionArgument.chars,
                    resource: ExceptionResource.ArgumentNull_Array);
            }

            if ((byteCount | charCount) < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    argument: (byteCount < 0) ? ExceptionArgument.byteCount : ExceptionArgument.charCount,
                    resource: ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            return GetCharsCommon(bytes, byteCount, chars, charCount);
        }

        public override unsafe char[] GetChars(byte[] bytes)
        {
            if (bytes is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes);
            }

            if (bytes.Length == 0)
            {
                return Array.Empty<char>();
            }

            // Since we're going to fill the entire char[] buffer, we could consider GC.AllocateUninitializedArray.

            char[] chars = new char[bytes.Length];

            fixed (byte* pBytes = bytes)
            fixed (char* pChars = chars)
            {
                GetCharsCommon(pBytes, bytes.Length, pChars, chars.Length);
            }

            return chars;
        }

        public override unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            if (bytes is null || chars is null)
            {
                ThrowHelper.ThrowArgumentNullException(
                    argument: (bytes is null) ? ExceptionArgument.bytes : ExceptionArgument.chars,
                    resource: ExceptionResource.ArgumentNull_Array);
            }

            if ((byteIndex | byteCount) < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    argument: (byteIndex < 0) ? ExceptionArgument.byteIndex : ExceptionArgument.byteCount,
                    resource: ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (bytes.Length - byteIndex < byteCount)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.bytes, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
            }

            if ((uint)charIndex > (uint)chars.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.charIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            fixed (byte* pBytes = bytes)
            fixed (char* pChars = chars)
            {
                return GetCharsCommon(pBytes + byteIndex, byteCount, pChars + charIndex, chars.Length - charIndex);
            }
        }

        public override unsafe char[] GetChars(byte[] bytes, int index, int count)
        {
            if (bytes is null)
            {
                ThrowHelper.ThrowArgumentNullException(
                    argument: ExceptionArgument.bytes,
                    resource: ExceptionResource.ArgumentNull_Array);
            }

            if ((index | count) < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    argument: (index < 0) ? ExceptionArgument.index : ExceptionArgument.count,
                    resource: ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (bytes.Length - index < count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.bytes, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
            }

            // Since we're going to fill the entire char[] buffer, we could consider GC.AllocateUninitializedArray.

            char[] chars = new char[count];

            fixed (byte* pBytes = bytes)
            fixed (char* pChars = chars)
            {
                GetCharsCommon(pBytes + index, count, pChars, chars.Length);
            }

            return chars;
        }

        public override unsafe int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars)
        {
            // It's ok for us to pass null pointers down to the workhorse below.

            fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
            fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
            {
                return GetCharsCommon(bytesPtr, bytes.Length, charsPtr, chars.Length);
            }
        }

        /// <inheritdoc/>
        public override unsafe bool TryGetChars(ReadOnlySpan<byte> bytes, Span<char> chars, out int charsWritten)
        {
            if (bytes.Length <= chars.Length)
            {
                fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
                fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
                {
                    charsWritten = GetCharsCommon(bytesPtr, bytes.Length, charsPtr, chars.Length);
                    return true;
                }
            }

            charsWritten = 0;
            return false;
        }

        public override unsafe string GetString(byte[] bytes)
        {
            if (bytes is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes);
            }

            string result = string.FastAllocateString(bytes.Length);
            fixed (byte* pBytes = bytes)
            fixed (char* pChars = result)
            {
                GetCharsCommon(pBytes, bytes.Length, pChars, result.Length);
            }
            return result;
        }

        public override unsafe string GetString(byte[] bytes, int index, int count)
        {
            if (bytes is null)
            {
                ThrowHelper.ThrowArgumentNullException(
                    argument: ExceptionArgument.bytes,
                    resource: ExceptionResource.ArgumentNull_Array);
            }

            if ((index | count) < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    argument: (index < 0) ? ExceptionArgument.index : ExceptionArgument.count,
                    resource: ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (bytes.Length - index < count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.bytes, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
            }

            string result = string.FastAllocateString(count);
            fixed (byte* pBytes = bytes)
            fixed (char* pChars = result)
            {
                GetCharsCommon(pBytes + index, count, pChars, count);
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe int GetCharsCommon(byte* pBytes, int byteCount, char* pChars, int charCount)
        {
            // Common helper method for all non-DecoderNLS entry points to GetChars.
            // A modification of this method should be copied in to each of the supported encodings: ASCII, UTF8, UTF16, UTF32.

            Debug.Assert(byteCount >= 0, "Caller shouldn't specify negative length buffer.");
            Debug.Assert(pBytes is not null || byteCount == 0, "Input pointer shouldn't be null if non-zero length specified.");
            Debug.Assert(charCount >= 0, "Caller shouldn't specify negative length buffer.");
            Debug.Assert(pChars is not null || charCount == 0, "Input pointer shouldn't be null if non-zero length specified.");

            // If we already know ahead of time that the destination buffer isn't large enough to hold
            // the widened data, fail immediately.

            if (byteCount > charCount)
            {
                ThrowCharsOverflow();
            }

            Latin1Utility.WidenLatin1ToUtf16(pBytes, pChars, (uint)byteCount);
            return byteCount;
        }

        // called by the fallback mechanism
        private protected sealed override unsafe int GetCharsFast(byte* pBytes, int bytesLength, char* pChars, int charsLength, out int bytesConsumed)
        {
            int charsWritten = Math.Min(bytesLength, charsLength);
            Latin1Utility.WidenLatin1ToUtf16(pBytes, pChars, (uint)charsWritten);

            bytesConsumed = charsWritten;
            return charsWritten;
        }

        public override Decoder GetDecoder()
        {
            return new DecoderNLS(this);
        }

        public override Encoder GetEncoder()
        {
            return new EncoderNLS(this);
        }

        //
        // Beginning of methods used by shared fallback logic.
        //

        internal sealed override bool TryGetByteCount(Rune value, out int byteCount)
        {
            // We can only process U+0000..U+00FF.
            // Everything else must go through the fallback mechanism.

            if (value.Value <= byte.MaxValue)
            {
                byteCount = 1;
                return true;
            }
            else
            {
                byteCount = default;
                return false;
            }
        }

        internal sealed override OperationStatus EncodeRune(Rune value, Span<byte> bytes, out int bytesWritten)
        {
            // We can only process U+0000..U+00FF.
            // Everything else must go through the fallback mechanism.

            if (value.Value <= byte.MaxValue)
            {
                if (!bytes.IsEmpty)
                {
                    bytes[0] = (byte)value.Value;
                    bytesWritten = 1;
                    return OperationStatus.Done;
                }
                else
                {
                    bytesWritten = 0;
                    return OperationStatus.DestinationTooSmall;
                }
            }
            else
            {
                bytesWritten = 0;
                return OperationStatus.InvalidData;
            }
        }

        internal sealed override OperationStatus DecodeFirstRune(ReadOnlySpan<byte> bytes, out Rune value, out int bytesConsumed)
        {
            if (!bytes.IsEmpty)
            {
                // Latin-1 byte
                byte b = bytes[0];
                value = new Rune(b);
                bytesConsumed = 1;
                return OperationStatus.Done;
            }
            else
            {
                // No data to decode
                value = Rune.ReplacementChar;
                bytesConsumed = 0;
                return OperationStatus.NeedMoreData;
            }
        }

        //
        // End of methods used by shared fallback logic.
        //

        // True if and only if the encoding only uses single byte code points.  (Ie, ASCII, 1252, etc)
        public override bool IsSingleByte => true;

        public override bool IsAlwaysNormalized(NormalizationForm form)
        {
            // Latin-1 contains precomposed characters, so normal for Form C.
            // Since some are composed, not normal for D & KD.
            // Also some letters like 0x00A8 (spacing diarisis) have compatibility decompositions, so false for KD & KC.

            // Only true for form C.
            return form == NormalizationForm.FormC;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool FallbackSupportsFastGetByteCount(EncoderFallback? fallback)
        {
            // If the caller didn't provide a fallback mechanism, they wanted us
            // to suppress short-circuiting the operation.

            if (fallback is null)
            {
                return false;
            }

            // If we're using the default best-fit fallback mechanism, we know that
            // any non-Latin-1 char will get replaced with a Latin-1 byte.

            if (fallback is EncoderLatin1BestFitFallback)
            {
                return true;
            }

            // If we're using a replacement fallback, and if the replacement fallback
            // replaces non-Latin-1 chars with Latin-1 bytes, then we know that
            // the number of output bytes will match the number of input chars.

            if (fallback is EncoderReplacementFallback replacementFallback
                && replacementFallback.MaxCharCount == 1
                && replacementFallback.DefaultString[0] <= byte.MaxValue)
            {
                return true;
            }

            // Otherwise we don't know the fallback behavior when it sees non-Latin-1
            // chars, so we can't assume the number of output bytes will match the
            // number of input chars.

            return false;
        }
    }
}

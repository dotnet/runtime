// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text
{
    // ASCIIEncoding
    //
    // Note that ASCIIEncoding is optimized with no best fit and ? for fallback.
    // It doesn't come in other flavors.
    //
    // Note: ASCIIEncoding is the only encoding that doesn't do best fit (windows has best fit).
    //
    // Note: IsAlwaysNormalized remains false because 1/2 the code points are unassigned, so they'd
    //       use fallbacks, and we cannot guarantee that fallbacks are normalized.

    public partial class ASCIIEncoding : Encoding
    {
        // This specialized sealed type has two benefits:
        // 1) it allows for devirtualization (see https://github.com/dotnet/coreclr/pull/9230), and
        // 2) it allows us to provide highly optimized implementations of certain routines because
        //    we can make assumptions about the fallback mechanisms in use (in particular, always
        //    replace with "?").
        //
        // (We don't take advantage of #2 yet, but we can do so in the future because the implementation
        // of cloning below allows us to make assumptions about the behaviors of the sealed type.)
        internal sealed class ASCIIEncodingSealed : ASCIIEncoding
        {
            public override object Clone()
            {
                // The base implementation of Encoding.Clone calls object.MemberwiseClone and marks the new object mutable.
                // We don't want to do this because it violates the invariants we have set for the sealed type.
                // Instead, we'll create a new instance of the base ASCIIEncoding type and mark it mutable.

                return new ASCIIEncoding()
                {
                    IsReadOnly = false
                };
            }
        }

        // Used by Encoding.ASCII for lazy initialization
        // The initialization code will not be run until a static member of the class is referenced
        internal static readonly ASCIIEncodingSealed s_default = new ASCIIEncodingSealed();

        public ASCIIEncoding() : base(Encoding.CodePageASCII)
        {
        }

        internal sealed override void SetDefaultFallbacks()
        {
            // For ASCIIEncoding we just use default replacement fallback
            this.encoderFallback = EncoderFallback.ReplacementFallback;
            this.decoderFallback = DecoderFallback.ReplacementFallback;
        }

        // WARNING: GetByteCount(string chars), GetBytes(string chars,...), and GetString(byte[] byteIndex...)
        // WARNING: have different variable names than EncodingNLS.cs, so this can't just be cut & pasted,
        // WARNING: or it'll break VB's way of calling these.
        //
        // The following methods are copied from EncodingNLS.cs.
        // Unfortunately EncodingNLS.cs is internal and we're public, so we have to re-implement them here.
        // These should be kept in sync for the following classes:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        // Returns the number of bytes required to encode a range of characters in
        // a character array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetByteCount(char[] chars, int index, int count)
        {
            // Validate input parameters

            if (chars is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chars, ExceptionResource.ArgumentNull_Array);
            }

            if ((index | count) < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException((index < 0) ? ExceptionArgument.index : ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (chars!.Length - index < count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.chars, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
            }

            fixed (char* pChars = chars)
            {
                return GetByteCountCommon(pChars + index, count);
            }
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetByteCount(string chars)
        {
            // Validate input parameters

            if (chars is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chars);
            }

            fixed (char* pChars = chars)
            {
                return GetByteCountCommon(pChars, chars!.Length);
            }
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [CLSCompliant(false)]
        public override unsafe int GetByteCount(char* chars, int count)
        {
            // Validate Parameters

            if (chars == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chars);
            }

            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            return GetByteCountCommon(chars, count);
        }

        public override unsafe int GetByteCount(ReadOnlySpan<char> chars)
        {
            // It's ok for us to pass null pointers down to the workhorse below.

            fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
            {
                return GetByteCountCommon(charsPtr, chars.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe int GetByteCountCommon(char* pChars, int charCount)
        {
            // Common helper method for all non-EncoderNLS entry points to GetByteCount.
            // A modification of this method should be copied in to each of the supported encodings: ASCII, UTF8, UTF16, UTF32.

            Debug.Assert(charCount >= 0, "Caller shouldn't specify negative length buffer.");
            Debug.Assert(pChars != null || charCount == 0, "Input pointer shouldn't be null if non-zero length specified.");

            // First call into the fast path.

            int totalByteCount = GetByteCountFast(pChars, charCount, EncoderFallback, out int charsConsumed);

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
            // First: Can we short-circuit the entire calculation?
            // If an EncoderReplacementFallback is in use, all non-ASCII chars
            // (including surrogate halves) are replaced with the default string.
            // If the default string consists of a single ASCII value, then we
            // know there's a 1:1 char->byte transcoding in all cases.

            int byteCount = charsLength;

            if (!(fallback is EncoderReplacementFallback replacementFallback
                && replacementFallback.MaxCharCount == 1
                && replacementFallback.DefaultString[0] <= 0x7F))
            {
                // Unrecognized fallback mechanism - count chars manually.

                byteCount = (int)ASCIIUtility.GetIndexOfFirstNonAsciiChar(pChars, (uint)charsLength);
            }

            charsConsumed = byteCount;
            return byteCount;
        }

        // Parent method is safe.
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        public override unsafe int GetBytes(string chars, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex)
        {
            // Validate Parameters

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

        // Encodes a range of characters in a character array into a range of bytes
        // in a byte array. An exception occurs if the byte array is not large
        // enough to hold the complete encoding of the characters. The
        // GetByteCount method can be used to determine the exact number of
        // bytes that will be produced for a given range of characters.
        // Alternatively, the GetMaxByteCount method can be used to
        // determine the maximum number of bytes that will be produced for a given
        // number of characters, regardless of the actual character values.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetBytes(char[] chars, int charIndex, int charCount,
                                               byte[] bytes, int byteIndex)
        {
            // Validate parameters

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

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [CLSCompliant(false)]
        public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
        {
            // Validate Parameters

            if (chars == null || bytes == null)
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

        public override unsafe int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
        {
            // It's ok for us to operate on null / empty spans.

            fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
            fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
            {
                return GetBytesCommon(charsPtr, chars.Length, bytesPtr, bytes.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe int GetBytesCommon(char* pChars, int charCount, byte* pBytes, int byteCount)
        {
            // Common helper method for all non-EncoderNLS entry points to GetBytes.
            // A modification of this method should be copied in to each of the supported encodings: ASCII, UTF8, UTF16, UTF32.

            Debug.Assert(charCount >= 0, "Caller shouldn't specify negative length buffer.");
            Debug.Assert(pChars != null || charCount == 0, "Input pointer shouldn't be null if non-zero length specified.");
            Debug.Assert(byteCount >= 0, "Caller shouldn't specify negative length buffer.");
            Debug.Assert(pBytes != null || byteCount == 0, "Input pointer shouldn't be null if non-zero length specified.");

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

                return GetBytesWithFallback(pChars, charCount, pBytes, byteCount, charsConsumed, bytesWritten);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // called directly by GetBytesCommon
        private protected sealed override unsafe int GetBytesFast(char* pChars, int charsLength, byte* pBytes, int bytesLength, out int charsConsumed)
        {
            int bytesWritten = (int)ASCIIUtility.NarrowUtf16ToAscii(pChars, pBytes, (uint)Math.Min(charsLength, bytesLength));

            charsConsumed = bytesWritten;
            return bytesWritten;
        }

        private protected sealed override unsafe int GetBytesWithFallback(ReadOnlySpan<char> chars, int originalCharsLength, Span<byte> bytes, int originalBytesLength, EncoderNLS? encoder)
        {
            // We special-case EncoderReplacementFallback if it's telling us to write a single ASCII char,
            // since we believe this to be relatively common and we can handle it more efficiently than
            // the base implementation.

            if (((encoder is null) ? this.EncoderFallback : encoder.Fallback) is EncoderReplacementFallback replacementFallback
                && replacementFallback.MaxCharCount == 1
                && replacementFallback.DefaultString[0] <= 0x7F)
            {
                byte replacementByte = (byte)replacementFallback.DefaultString[0];

                int numElementsToConvert = Math.Min(chars.Length, bytes.Length);
                int idx = 0;

                fixed (char* pChars = &MemoryMarshal.GetReference(chars))
                fixed (byte* pBytes = &MemoryMarshal.GetReference(bytes))
                {
                    // In a loop, replace the non-convertible data, then bulk-convert as much as we can.

                    while (idx < numElementsToConvert)
                    {
                        pBytes[idx++] = replacementByte;

                        if (idx < numElementsToConvert)
                        {
                            idx += (int)ASCIIUtility.NarrowUtf16ToAscii(&pChars[idx], &pBytes[idx], (uint)(numElementsToConvert - idx));
                        }

                        Debug.Assert(idx <= numElementsToConvert, "Somehow went beyond bounds of source or destination buffer?");
                    }
                }

                // Slice off how much we consumed / wrote.

                chars = chars.Slice(numElementsToConvert);
                bytes = bytes.Slice(numElementsToConvert);
            }

            // If we couldn't go through our fast fallback mechanism, or if we still have leftover
            // data because we couldn't consume everything in the loop above, we need to go down the
            // slow fallback path.

            if (chars.IsEmpty)
            {
                return originalBytesLength - bytes.Length; // total number of bytes written
            }
            else
            {
                return base.GetBytesWithFallback(chars, originalCharsLength, bytes, originalBytesLength, encoder);
            }
        }

        // Returns the number of characters produced by decoding a range of bytes
        // in a byte array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetCharCount(byte[] bytes, int index, int count)
        {
            // Validate Parameters

            if (bytes is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes, ExceptionResource.ArgumentNull_Array);
            }

            if ((index | count) < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException((index < 0) ? ExceptionArgument.index : ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (bytes!.Length - index < count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.bytes, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
            }

            fixed (byte* pBytes = bytes)
            {
                return GetCharCountCommon(pBytes + index, count);
            }
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [CLSCompliant(false)]
        public override unsafe int GetCharCount(byte* bytes, int count)
        {
            // Validate Parameters

            if (bytes == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes, ExceptionResource.ArgumentNull_Array);
            }

            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            return GetCharCountCommon(bytes, count);
        }

        public override unsafe int GetCharCount(ReadOnlySpan<byte> bytes)
        {
            // It's ok for us to pass null pointers down to the workhorse routine.

            fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
            {
                return GetCharCountCommon(bytesPtr, bytes.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe int GetCharCountCommon(byte* pBytes, int byteCount)
        {
            // Common helper method for all non-DecoderNLS entry points to GetCharCount.
            // A modification of this method should be copied in to each of the supported encodings: ASCII, UTF8, UTF16, UTF32.

            Debug.Assert(byteCount >= 0, "Caller shouldn't specify negative length buffer.");
            Debug.Assert(pBytes != null || byteCount == 0, "Input pointer shouldn't be null if non-zero length specified.");

            // First call into the fast path.

            int totalCharCount = GetCharCountFast(pBytes, byteCount, DecoderFallback, out int bytesConsumed);

            if (bytesConsumed != byteCount)
            {
                // If there's still data remaining in the source buffer, go down the fallback path.
                // We need to check for integer overflow since the fallback could change the required
                // output count in unexpected ways.

                totalCharCount += GetCharCountWithFallback(pBytes, byteCount, bytesConsumed);
                if (totalCharCount < 0)
                {
                    ThrowConversionOverflow();
                }
            }

            return totalCharCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // called directly by GetCharCountCommon
        private protected sealed override unsafe int GetCharCountFast(byte* pBytes, int bytesLength, DecoderFallback? fallback, out int bytesConsumed)
        {
            // First: Can we short-circuit the entire calculation?
            // If a DecoderReplacementFallback is in use, all non-ASCII bytes are replaced with
            // the default string. If the default string consists of a single BMP value, then we
            // know there's a 1:1 byte->char transcoding in all cases.

            int charCount = bytesLength;

            if (!(fallback is DecoderReplacementFallback replacementFallback) || replacementFallback.MaxCharCount != 1)
            {
                // Unrecognized fallback mechanism - count bytes manually.

                charCount = (int)ASCIIUtility.GetIndexOfFirstNonAsciiByte(pBytes, (uint)bytesLength);
            }

            bytesConsumed = charCount;
            return charCount;
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                              char[] chars, int charIndex)
        {
            // Validate Parameters

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

            if (bytes!.Length - byteIndex < byteCount)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.bytes, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
            }

            if ((uint)charIndex > (uint)chars!.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.charIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            fixed (byte* pBytes = bytes)
            fixed (char* pChars = chars)
            {
                return GetCharsCommon(pBytes + byteIndex, byteCount, pChars + charIndex, chars.Length - charIndex);
            }
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [CLSCompliant(false)]
        public override unsafe int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        {
            // Validate Parameters

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

        public override unsafe int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars)
        {
            // It's ok for us to pass null pointers down to the workhorse below.

            fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
            fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
            {
                return GetCharsCommon(bytesPtr, bytes.Length, charsPtr, chars.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe int GetCharsCommon(byte* pBytes, int byteCount, char* pChars, int charCount)
        {
            // Common helper method for all non-DecoderNLS entry points to GetChars.
            // A modification of this method should be copied in to each of the supported encodings: ASCII, UTF8, UTF16, UTF32.

            Debug.Assert(byteCount >= 0, "Caller shouldn't specify negative length buffer.");
            Debug.Assert(pBytes != null || byteCount == 0, "Input pointer shouldn't be null if non-zero length specified.");
            Debug.Assert(charCount >= 0, "Caller shouldn't specify negative length buffer.");
            Debug.Assert(pChars != null || charCount == 0, "Input pointer shouldn't be null if non-zero length specified.");

            // First call into the fast path.

            int charsWritten = GetCharsFast(pBytes, byteCount, pChars, charCount, out int bytesConsumed);

            if (bytesConsumed == byteCount)
            {
                // All elements converted - return immediately.

                return charsWritten;
            }
            else
            {
                // Simple narrowing conversion couldn't operate on entire buffer - invoke fallback.

                return GetCharsWithFallback(pBytes, byteCount, pChars, charCount, bytesConsumed, charsWritten);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // called directly by GetCharsCommon
        private protected sealed override unsafe int GetCharsFast(byte* pBytes, int bytesLength, char* pChars, int charsLength, out int bytesConsumed)
        {
            int charsWritten = (int)ASCIIUtility.WidenAsciiToUtf16(pBytes, pChars, (uint)Math.Min(bytesLength, charsLength));

            bytesConsumed = charsWritten;
            return charsWritten;
        }

        private protected sealed override unsafe int GetCharsWithFallback(ReadOnlySpan<byte> bytes, int originalBytesLength, Span<char> chars, int originalCharsLength, DecoderNLS? decoder)
        {
            // We special-case DecoderReplacementFallback if it's telling us to write a single BMP char,
            // since we believe this to be relatively common and we can handle it more efficiently than
            // the base implementation.

            if ((decoder is null ? DecoderFallback : decoder.Fallback) is DecoderReplacementFallback replacementFallback
                && replacementFallback.MaxCharCount == 1)
            {
                char replacementChar = replacementFallback.DefaultString[0];

                int numElementsToConvert = Math.Min(bytes.Length, chars.Length);
                int idx = 0;

                fixed (byte* pBytes = &MemoryMarshal.GetReference(bytes))
                fixed (char* pChars = &MemoryMarshal.GetReference(chars))
                {
                    // In a loop, replace the non-convertible data, then bulk-convert as much as we can.

                    while (idx < numElementsToConvert)
                    {
                        pChars[idx++] = replacementChar;

                        if (idx < numElementsToConvert)
                        {
                            idx += (int)ASCIIUtility.WidenAsciiToUtf16(&pBytes[idx], &pChars[idx], (uint)(numElementsToConvert - idx));
                        }

                        Debug.Assert(idx <= numElementsToConvert, "Somehow went beyond bounds of source or destination buffer?");
                    }
                }

                // Slice off how much we consumed / wrote.

                bytes = bytes.Slice(numElementsToConvert);
                chars = chars.Slice(numElementsToConvert);
            }

            // If we couldn't go through our fast fallback mechanism, or if we still have leftover
            // data because we couldn't consume everything in the loop above, we need to go down the
            // slow fallback path.

            if (bytes.IsEmpty)
            {
                return originalCharsLength - chars.Length; // total number of chars written
            }
            else
            {
                return base.GetCharsWithFallback(bytes, originalBytesLength, chars, originalCharsLength, decoder);
            }
        }

        // Returns a string containing the decoded representation of a range of
        // bytes in a byte array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe string GetString(byte[] bytes, int byteIndex, int byteCount)
        {
            // Validate Parameters

            if (bytes is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes, ExceptionResource.ArgumentNull_Array);
            }

            if ((byteIndex | byteCount) < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    argument: (byteIndex < 0) ? ExceptionArgument.byteIndex : ExceptionArgument.byteCount,
                    resource: ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (bytes!.Length - byteIndex < byteCount)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.bytes, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
            }

            // Avoid problems with empty input buffer
            if (byteCount == 0)
                return string.Empty;

            fixed (byte* pBytes = bytes)
            {
                return string.CreateStringFromEncoding(pBytes + byteIndex, byteCount, this);
            }
        }

        //
        // End of standard methods copied from EncodingNLS.cs
        //

        //
        // Beginning of methods used by shared fallback logic.
        //

        internal sealed override bool TryGetByteCount(Rune value, out int byteCount)
        {
            if (value.IsAscii)
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
            if (value.IsAscii)
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
                byte b = bytes[0];
                if (b <= 0x7F)
                {
                    // ASCII byte

                    value = new Rune(b);
                    bytesConsumed = 1;
                    return OperationStatus.Done;
                }
                else
                {
                    // Non-ASCII byte

                    value = Rune.ReplacementChar;
                    bytesConsumed = 1;
                    return OperationStatus.InvalidData;
                }
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

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount),
                     SR.ArgumentOutOfRange_NeedNonNegNum);

            // Characters would be # of characters + 1 in case high surrogate is ? * max fallback
            long byteCount = (long)charCount + 1;

            if (EncoderFallback.MaxCharCount > 1)
                byteCount *= EncoderFallback.MaxCharCount;

            // 1 to 1 for most characters.  Only surrogates with fallbacks have less.

            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ArgumentOutOfRange_GetByteCountOverflow);
            return (int)byteCount;
        }


        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount),
                     SR.ArgumentOutOfRange_NeedNonNegNum);

            // Just return length, SBCS stay the same length because they don't map to surrogate
            long charCount = (long)byteCount;

            // 1 to 1 for most characters.  Only surrogates with fallbacks have less, unknown fallbacks could be longer.
            if (DecoderFallback.MaxCharCount > 1)
                charCount *= DecoderFallback.MaxCharCount;

            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.ArgumentOutOfRange_GetCharCountOverflow);

            return (int)charCount;
        }

        // True if and only if the encoding only uses single byte code points.  (Ie, ASCII, 1252, etc)

        public override bool IsSingleByte => true;

        public override Decoder GetDecoder() => new DecoderNLS(this);


        public override Encoder GetEncoder() => new EncoderNLS(this);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The worker functions in this file was optimized for performance. If you make changes
// you should use care to consider all of the interesting cases.

// The code of all worker functions in this file is written twice: Once as a slow loop, and the
// second time as a fast loop. The slow loops handles all special cases, throws exceptions, etc.
// The fast loops attempts to blaze through as fast as possible with optimistic range checks,
// processing multiple characters at a time, and falling back to the slow loop for all special cases.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;

namespace System.Text
{
    // Encodes text into and out of UTF-8.  UTF-8 is a way of writing
    // Unicode characters with variable numbers of bytes per character,
    // optimized for the lower 127 ASCII characters.  It's an efficient way
    // of encoding US English in an internationalizable way.
    //
    // Don't override IsAlwaysNormalized because it is just a Unicode Transformation and could be confused.
    //
    // The UTF-8 byte order mark is simply the Unicode byte order mark
    // (0xFEFF) written in UTF-8 (0xEF 0xBB 0xBF).  The byte order mark is
    // used mostly to distinguish UTF-8 text from other encodings, and doesn't
    // switch the byte orderings.

    public partial class UTF8Encoding : Encoding
    {
        /*
            bytes   bits    UTF-8 representation
            -----   ----    -----------------------------------
            1        7      0vvvvvvv
            2       11      110vvvvv 10vvvvvv
            3       16      1110vvvv 10vvvvvv 10vvvvvv
            4       21      11110vvv 10vvvvvv 10vvvvvv 10vvvvvv
            -----   ----    -----------------------------------

            Surrogate:
            Real Unicode value = (HighSurrogate - 0xD800) * 0x400 + (LowSurrogate - 0xDC00) + 0x10000
        */

        private const int UTF8_CODEPAGE = 65001;

        /// <summary>
        /// Transcoding to UTF-8 bytes from UTF-16 input chars will result in a maximum 3:1 expansion.
        /// </summary>
        /// <remarks>
        /// Supplementary code points are expanded to UTF-8 from UTF-16 at a 4:2 ratio,
        /// so 3:1 is still the correct value for maximum expansion.
        /// </remarks>
        private const int MaxUtf8BytesPerChar = 3;


        // Used by Encoding.UTF8 for lazy initialization
        // The initialization code will not be run until a static member of the class is referenced
        internal static readonly UTF8EncodingSealed s_default = new UTF8EncodingSealed(encoderShouldEmitUTF8Identifier: true);

        internal static ReadOnlySpan<byte> PreambleSpan => new byte[3] { 0xEF, 0xBB, 0xBF }; // uses C# compiler's optimization for static byte[] data

        // Yes, the idea of emitting U+FEFF as a UTF-8 identifier has made it into
        // the standard.
        private readonly bool _emitUTF8Identifier;

        private readonly bool _isThrowException;


        public UTF8Encoding() :
            base(UTF8_CODEPAGE)
        {
        }


        public UTF8Encoding(bool encoderShouldEmitUTF8Identifier) :
            this()
        {
            _emitUTF8Identifier = encoderShouldEmitUTF8Identifier;
        }


        public UTF8Encoding(bool encoderShouldEmitUTF8Identifier, bool throwOnInvalidBytes) :
            this(encoderShouldEmitUTF8Identifier)
        {
            _isThrowException = throwOnInvalidBytes;

            // Encoding's constructor already did this, but it'll be wrong if we're throwing exceptions
            if (_isThrowException)
                SetDefaultFallbacks();
        }

        internal sealed override void SetDefaultFallbacks()
        {
            // For UTF-X encodings, we use a replacement fallback with an empty string
            if (_isThrowException)
            {
                this.encoderFallback = EncoderFallback.ExceptionFallback;
                this.decoderFallback = DecoderFallback.ExceptionFallback;
            }
            else
            {
                this.encoderFallback = new EncoderReplacementFallback("\xFFFD");
                this.decoderFallback = new DecoderReplacementFallback("\xFFFD");
            }
        }


        // WARNING: GetByteCount(string chars)
        // WARNING: has different variable names than EncodingNLS.cs, so this can't just be cut & pasted,
        // WARNING: otherwise it'll break VB's way of declaring these.
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

            if (chars.Length - index < count)
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
                return GetByteCountCommon(pChars, chars.Length);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // called directly by GetCharCountCommon
        private protected sealed override unsafe int GetByteCountFast(char* pChars, int charsLength, EncoderFallback? fallback, out int charsConsumed)
        {
            // The number of UTF-8 code units may exceed the number of UTF-16 code units,
            // so we'll need to check for overflow before casting to Int32.

            char* ptrToFirstInvalidChar = Utf16Utility.GetPointerToFirstInvalidChar(pChars, charsLength, out long utf8CodeUnitCountAdjustment, out _);

            int tempCharsConsumed = (int)(ptrToFirstInvalidChar - pChars);
            charsConsumed = tempCharsConsumed;

            long totalUtf8Bytes = tempCharsConsumed + utf8CodeUnitCountAdjustment;
            if ((ulong)totalUtf8Bytes > int.MaxValue)
            {
                ThrowConversionOverflow();
            }

            return (int)totalUtf8Bytes;
        }

        // Parent method is safe.
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        public override unsafe int GetBytes(string s, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex)
        {
            // Validate Parameters

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

            if (s.Length - charIndex < charCount)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.s, ExceptionResource.ArgumentOutOfRange_IndexCount);
            }

            if ((uint)byteIndex > bytes.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.byteIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            fixed (char* pChars = s)
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

            if (chars.Length - charIndex < charCount)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.chars, ExceptionResource.ArgumentOutOfRange_IndexCount);
            }

            if ((uint)byteIndex > bytes.Length)
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
            // We don't care about the exact OperationStatus value returned by the workhorse routine; we only
            // care if the workhorse was able to consume the entire input payload. If we're unable to do so,
            // we'll handle the remainder in the fallback routine.

            Utf8Utility.TranscodeToUtf8(pChars, charsLength, pBytes, bytesLength, out char* pInputBufferRemaining, out byte* pOutputBufferRemaining);

            charsConsumed = (int)(pInputBufferRemaining - pChars);
            return (int)(pOutputBufferRemaining - pBytes);
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

            if (bytes.Length - index < count)
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

        // WARNING:  If we throw an error, then System.Resources.ResourceReader calls this method.
        //           So if we're really broken, then that could also throw an error... recursively.
        //           So try to make sure GetChars can at least process all uses by
        //           System.Resources.ResourceReader!
        //
        // Note:  We throw exceptions on individually encoded surrogates and other non-shortest forms.
        //        If exceptions aren't turned on, then we drop all non-shortest &individual surrogates.
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
            // We don't care about the exact OperationStatus value returned by the workhorse routine; we only
            // care if the workhorse was able to consume the entire input payload. If we're unable to do so,
            // we'll handle the remainder in the fallback routine.

            Utf8Utility.TranscodeToUtf16(pBytes, bytesLength, pChars, charsLength, out byte* pInputBufferRemaining, out char* pOutputBufferRemaining);

            bytesConsumed = (int)(pInputBufferRemaining - pBytes);
            return (int)(pOutputBufferRemaining - pChars);
        }

        private protected sealed override unsafe int GetCharsWithFallback(ReadOnlySpan<byte> bytes, int originalBytesLength, Span<char> chars, int originalCharsLength, DecoderNLS? decoder)
        {
            // We special-case DecoderReplacementFallback if it's telling us to write a single U+FFFD char,
            // since we believe this to be relatively common and we can handle it more efficiently than
            // the base implementation.

            if (((decoder is null) ? this.DecoderFallback : decoder.Fallback) is DecoderReplacementFallback replacementFallback
                && replacementFallback.MaxCharCount == 1
                && replacementFallback.DefaultString[0] == UnicodeUtility.ReplacementChar)
            {
                // Don't care about the exact OperationStatus, just how much of the payload we were able
                // to process.

                Utf8.ToUtf16(bytes, chars, out int bytesRead, out int charsWritten, replaceInvalidSequences: true, isFinalBlock: decoder is null || decoder.MustFlush);

                // Slice off how much we consumed / wrote.

                bytes = bytes.Slice(bytesRead);
                chars = chars.Slice(charsWritten);
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

        public override unsafe string GetString(byte[] bytes, int index, int count)
        {
            // Validate Parameters

            if (bytes is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes, ExceptionResource.ArgumentNull_Array);
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

            // Avoid problems with empty input buffer
            if (count == 0)
                return string.Empty;

            fixed (byte* pBytes = bytes)
            {
                return string.CreateStringFromEncoding(pBytes + index, count, this);
            }
        }

        //
        // End of standard methods copied from EncodingNLS.cs
        //

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe int GetCharCountCommon(byte* pBytes, int byteCount)
        {
            // Common helper method for all non-DecoderNLS entry points to GetCharCount.
            // A modification of this method should be copied in to each of the supported encodings: ASCII, UTF8, UTF16, UTF32.

            Debug.Assert(byteCount >= 0, "Caller shouldn't specify negative length buffer.");
            Debug.Assert(pBytes != null || byteCount == 0, "Input pointer shouldn't be null if non-zero length specified.");

            // First call into the fast path.
            // Don't bother providing a fallback mechanism; our fast path doesn't use it.

            int totalCharCount = GetCharCountFast(pBytes, byteCount, fallback: null, out int bytesConsumed);

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
            // The number of UTF-16 code units will never exceed the number of UTF-8 code units,
            // so the addition at the end of this method will not overflow.

            byte* ptrToFirstInvalidByte = Utf8Utility.GetPointerToFirstInvalidByte(pBytes, bytesLength, out int utf16CodeUnitCountAdjustment, out _);

            int tempBytesConsumed = (int)(ptrToFirstInvalidByte - pBytes);
            bytesConsumed = tempBytesConsumed;

            return tempBytesConsumed + utf16CodeUnitCountAdjustment;
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
            // All well-formed Rune instances can be converted to 1..4 UTF-8 code units.

            byteCount = value.Utf8SequenceLength;
            return true;
        }

        internal sealed override OperationStatus EncodeRune(Rune value, Span<byte> bytes, out int bytesWritten)
        {
            // All well-formed Rune instances can be encoded as 1..4 UTF-8 code units.
            // If there's an error, it's because the destination was too small.

            return value.TryEncodeToUtf8(bytes, out bytesWritten) ? OperationStatus.Done : OperationStatus.DestinationTooSmall;
        }

        internal sealed override OperationStatus DecodeFirstRune(ReadOnlySpan<byte> bytes, out Rune value, out int bytesConsumed)
        {
            return Rune.DecodeFromUtf8(bytes, out value, out bytesConsumed);
        }

        //
        // End of methods used by shared fallback logic.
        //

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount),
                     SR.ArgumentOutOfRange_NeedNonNegNum);

            // Characters would be # of characters + 1 in case left over high surrogate is ? * max fallback
            long byteCount = (long)charCount + 1;

            if (EncoderFallback.MaxCharCount > 1)
                byteCount *= EncoderFallback.MaxCharCount;

            byteCount *= MaxUtf8BytesPerChar;

            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ArgumentOutOfRange_GetByteCountOverflow);

            return (int)byteCount;
        }


        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount),
                     SR.ArgumentOutOfRange_NeedNonNegNum);

            // Figure out our length, 1 char per input byte + 1 char if 1st byte is last byte of 4 byte surrogate pair
            long charCount = ((long)byteCount + 1);

            // Non-shortest form would fall back, so get max count from fallback.
            // So would 11... followed by 11..., so you could fall back every byte
            if (DecoderFallback.MaxCharCount > 1)
            {
                charCount *= DecoderFallback.MaxCharCount;
            }

            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.ArgumentOutOfRange_GetCharCountOverflow);

            return (int)charCount;
        }


        public override byte[] GetPreamble()
        {
            if (_emitUTF8Identifier)
            {
                // Allocate new array to prevent users from modifying it.
                return new byte[3] { 0xEF, 0xBB, 0xBF };
            }
            else
                return Array.Empty<byte>();
        }

        public override ReadOnlySpan<byte> Preamble =>
            GetType() != typeof(UTF8Encoding) ? new ReadOnlySpan<byte>(GetPreamble()) : // in case a derived UTF8Encoding overrode GetPreamble
            _emitUTF8Identifier ? PreambleSpan :
            default;

        public override bool Equals([NotNullWhen(true)] object? value)
        {
            if (value is UTF8Encoding that)
            {
                return (_emitUTF8Identifier == that._emitUTF8Identifier) &&
                       (EncoderFallback.Equals(that.EncoderFallback)) &&
                       (DecoderFallback.Equals(that.DecoderFallback));
            }
            return false;
        }


        public override int GetHashCode()
        {
            // Not great distribution, but this is relatively unlikely to be used as the key in a hashtable.
            return this.EncoderFallback.GetHashCode() + this.DecoderFallback.GetHashCode() +
                   UTF8_CODEPAGE + (_emitUTF8Identifier ? 1 : 0);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// The worker functions in this file was optimized for performance. If you make changes
// you should use care to consider all of the interesting cases.

// The code of all worker functions in this file is written twice: Once as as a slow loop, and the
// second time as a fast loop. The slow loops handles all special cases, throws exceptions, etc.
// The fast loops attempts to blaze through as fast as possible with optimistic range checks,
// processing multiple characters at a time, and falling back to the slow loop for all special cases.

// This define can be used to turn off the fast loops. Useful for finding whether
// the problem is fastloop-specific.
#define FASTLOOP

namespace System.Text
{
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;

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

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public class UTF8Encoding : Encoding
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

        private const int UTF8_CODEPAGE=65001;
        
        // Used by Encoding.UTF8 for lazy initialization
        // The initialization code will not be run until a static member of the class is referenced
        internal static readonly UTF8Encoding s_default = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        // Yes, the idea of emitting U+FEFF as a UTF-8 identifier has made it into
        // the standard.
        private bool emitUTF8Identifier = false;

        private bool isThrowException = false;


        public UTF8Encoding(): this(false)
        {
        }


        public UTF8Encoding(bool encoderShouldEmitUTF8Identifier):
            this(encoderShouldEmitUTF8Identifier, false)
        {
        }


        public UTF8Encoding(bool encoderShouldEmitUTF8Identifier, bool throwOnInvalidBytes):
            base(UTF8_CODEPAGE)
        {
            this.emitUTF8Identifier = encoderShouldEmitUTF8Identifier;
            this.isThrowException = throwOnInvalidBytes;

            // Encoding's constructor already did this, but it'll be wrong if we're throwing exceptions
            if (this.isThrowException)
                SetDefaultFallbacks();
        }

        internal override void SetDefaultFallbacks()
        {
            // For UTF-X encodings, we use a replacement fallback with an empty string
            if (this.isThrowException)
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

        // NOTE: Many methods in this class forward to EncodingForwarder for
        // validating arguments/wrapping the unsafe methods in this class 
        // which do the actual work. That class contains
        // shared logic for doing this which is used by
        // ASCIIEncoding, EncodingNLS, UnicodeEncoding, UTF32Encoding,
        // UTF7Encoding, and UTF8Encoding.
        // The reason the code is separated out into a static class, rather
        // than a base class which overrides all of these methods for us
        // (which is what EncodingNLS is for internal Encodings) is because
        // that's really more of an implementation detail so it's internal.
        // At the same time, C# doesn't allow a public class subclassing an
        // internal/private one, so we end up having to re-override these
        // methods in all of the public Encodings + EncodingNLS.

        // Returns the number of bytes required to encode a range of characters in
        // a character array.

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return EncodingForwarder.GetByteCount(this, chars, index, count);
        }

        public override int GetByteCount(String chars)
        {
            return EncodingForwarder.GetByteCount(this, chars);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        [System.Runtime.InteropServices.ComVisible(false)]
        public override unsafe int GetByteCount(char* chars, int count)
        {
            return EncodingForwarder.GetByteCount(this, chars, count);
        }

        public override int GetBytes(String s, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex)
        {
            return EncodingForwarder.GetBytes(this, s, charIndex, charCount, bytes, byteIndex);
        }

        // Encodes a range of characters in a character array into a range of bytes
        // in a byte array. An exception occurs if the byte array is not large
        // enough to hold the complete encoding of the characters. The
        // GetByteCount method can be used to determine the exact number of
        // bytes that will be produced for a given range of characters.
        // Alternatively, the GetMaxByteCount method can be used to
        // determine the maximum number of bytes that will be produced for a given
        // number of characters, regardless of the actual character values.

        public override int GetBytes(char[] chars, int charIndex, int charCount,
                                               byte[] bytes, int byteIndex)
        {
            return EncodingForwarder.GetBytes(this, chars, charIndex, charCount, bytes, byteIndex);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        [System.Runtime.InteropServices.ComVisible(false)]
        public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
        {
            return EncodingForwarder.GetBytes(this, chars, charCount, bytes, byteCount);
        }

        // Returns the number of characters produced by decoding a range of bytes
        // in a byte array.

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return EncodingForwarder.GetCharCount(this, bytes, index, count);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        [System.Runtime.InteropServices.ComVisible(false)]
        public override unsafe int GetCharCount(byte* bytes, int count)
        {
            return EncodingForwarder.GetCharCount(this, bytes, count);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                              char[] chars, int charIndex)
        {
            return EncodingForwarder.GetChars(this, bytes, byteIndex, byteCount, chars, charIndex);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        [System.Runtime.InteropServices.ComVisible(false)]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        {
            return EncodingForwarder.GetChars(this, bytes, byteCount, chars, charCount);
        }

        // Returns a string containing the decoded representation of a range of
        // bytes in a byte array.

        [System.Runtime.InteropServices.ComVisible(false)]
        public override String GetString(byte[] bytes, int index, int count)
        {
            return EncodingForwarder.GetString(this, bytes, index, count);
        }
        
        // End of overridden methods which use EncodingForwarder

        // To simplify maintenance, the structure of GetByteCount and GetBytes should be
        // kept the same as much as possible
        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetByteCount(char *chars, int count, EncoderNLS baseEncoder)
        {
            // For fallback we may need a fallback buffer.
            // We wait to initialize it though in case we don't have any broken input unicode
            EncoderFallbackBuffer fallbackBuffer = null;
            char *pSrc = chars;
            char *pEnd = pSrc+count;

            // Start by assuming we have as many as count
            int byteCount = count;

            int ch = 0;

            if (baseEncoder != null) {
                UTF8Encoder encoder = (UTF8Encoder)baseEncoder;
                ch = encoder.surrogateChar;

                // We mustn't have left over fallback data when counting
                if (encoder.InternalHasFallbackBuffer)
                {
                    fallbackBuffer = encoder.FallbackBuffer;
                    if (fallbackBuffer.Remaining > 0)
                        throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty",
                        this.EncodingName, encoder.Fallback.GetType()));

                    // Set our internal fallback interesting things.
                    fallbackBuffer.InternalInitialize(chars, pEnd, encoder, false);
                }
            }

            for (;;) {
                // SLOWLOOP: does all range checks, handles all special cases, but it is slow
                if (pSrc >= pEnd) {
                
                    if (ch == 0) {
                        // Unroll any fallback that happens at the end
                        ch = fallbackBuffer != null ? fallbackBuffer.InternalGetNextChar() : 0;
                        if (ch > 0) {
                            byteCount++;
                            goto ProcessChar;
                        }
                    } else {
                        // Case of surrogates in the fallback.
                        if (fallbackBuffer != null && fallbackBuffer.bFallingBack) {
                            Contract.Assert(ch >= 0xD800 && ch <= 0xDBFF,
                                "[UTF8Encoding.GetBytes]expected high surrogate, not 0x" + ((int)ch).ToString("X4", CultureInfo.InvariantCulture));
                            
                            ch = fallbackBuffer.InternalGetNextChar();
                            byteCount++;
                            
                            if (InRange(ch, CharUnicodeInfo.LOW_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END)) {
                                ch = 0xfffd;
                                byteCount++;
                                goto EncodeChar;
                            } else if (ch > 0){
                                goto ProcessChar;
                            } else {
                                byteCount--; // ignore last one.
                                break;
                            }
                        }
                    }
                
                    if (ch <= 0) {
                        break;
                    }
                    if (baseEncoder != null && !baseEncoder.MustFlush) {
                        break;
                    }

                    // attempt to encode the partial surrogate (will fallback or ignore it), it'll also subtract 1.
                    byteCount++;
                    goto EncodeChar;
                }

                if (ch > 0) {
                    Contract.Assert(ch >= 0xD800 && ch <= 0xDBFF,
                        "[UTF8Encoding.GetBytes]expected high surrogate, not 0x" + ((int)ch).ToString("X4", CultureInfo.InvariantCulture));

                    // use separate helper variables for local contexts so that the jit optimizations
                    // won't get confused about the variable lifetimes
                    int cha = *pSrc;

                    // count the pending surrogate
                    byteCount++;

                    // In previous byte, we encountered a high surrogate, so we are expecting a low surrogate here.
                    // if (IsLowSurrogate(cha)) {
                    if (InRange(cha, CharUnicodeInfo.LOW_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END)) {
                        // Don't need a real # because we're just counting, anything > 0x7ff ('cept surrogate) will do.
                        ch = 0xfffd;
//                        ch = cha + (ch << 10) +
//                            (0x10000
//                            - CharUnicodeInfo.LOW_SURROGATE_START
//                            - (CharUnicodeInfo.HIGH_SURROGATE_START << 10) );

                        // Use this next char
                        pSrc++;
                    }
                    // else ch is still high surrogate and encoding will fail (so don't add count)

                    // attempt to encode the surrogate or partial surrogate
                    goto EncodeChar;
                }

                // If we've used a fallback, then we have to check for it
                if (fallbackBuffer != null)
                {
                    ch = fallbackBuffer.InternalGetNextChar();
                    if (ch > 0)
                    {
                        // We have an extra byte we weren't expecting.
                        byteCount++;
                        goto ProcessChar;
                    }
                }

                // read next char. The JIT optimization seems to be getting confused when
                // compiling "ch = *pSrc++;", so rather use "ch = *pSrc; pSrc++;" instead
                ch = *pSrc;
                pSrc++;

            ProcessChar:
                // if (IsHighSurrogate(ch)) {
                if (InRange(ch, CharUnicodeInfo.HIGH_SURROGATE_START, CharUnicodeInfo.HIGH_SURROGATE_END)) {
                    // we will count this surrogate next time around
                    byteCount--;
                    continue;
                }
                // either good char or partial surrogate

            EncodeChar:
                // throw exception on partial surrogate if necessary
                // if (IsLowSurrogate(ch) || IsHighSurrogate(ch))
                if (InRange(ch, CharUnicodeInfo.HIGH_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END))
                {
                    // Lone surrogates aren't allowed
                    // Have to make a fallback buffer if we don't have one
                    if (fallbackBuffer == null)
                    {
                        // wait on fallbacks if we can
                        // For fallback we may need a fallback buffer
                        if (baseEncoder == null)
                            fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = baseEncoder.FallbackBuffer;

                        // Set our internal fallback interesting things.
                        fallbackBuffer.InternalInitialize(chars, chars + count, baseEncoder, false);
                    }

                    // Do our fallback.  Actually we already know its a mixed up surrogate,
                    // so the ref pSrc isn't gonna do anything.
                    fallbackBuffer.InternalFallback(unchecked((char)ch), ref pSrc);

                    // Ignore it if we don't throw (we had preallocated this ch)
                    byteCount--;
                    ch = 0;
                    continue;
                }

                // Count them
                if (ch > 0x7F) {
                    if (ch > 0x7FF) {
                        // the extra surrogate byte was compensated by the second surrogate character
                        // (2 surrogates make 4 bytes.  We've already counted 2 bytes, 1 per char)
                        byteCount++;
                    }
                    byteCount++;
                }

#if BIT64
                // check for overflow
                if (byteCount < 0) {
                    break;
                }
#endif

#if FASTLOOP
                // If still have fallback don't do fast loop
                if (fallbackBuffer != null && (ch = fallbackBuffer.InternalGetNextChar()) != 0)
                {
                    // We're reserving 1 byte for each char by default
                    byteCount++;
                    goto ProcessChar;
                }

                int availableChars = PtrDiff(pEnd, pSrc);

                // don't fall into the fast decoding loop if we don't have enough characters
                if (availableChars <= 13) {
                    // try to get over the remainder of the ascii characters fast though
                    char* pLocalEnd = pEnd; // hint to get pLocalEnd enregistered
                    while (pSrc < pLocalEnd) {
                        ch = *pSrc;
                        pSrc++;
                        if (ch > 0x7F)
                            goto ProcessChar;
                    }
                    
                    // we are done
                    break;
                }

#if BIT64
                // make sure that we won't get a silent overflow inside the fast loop
                // (Fall out to slow loop if we have this many characters)
                availableChars &= 0x0FFFFFFF;
#endif

                // To compute the upper bound, assume that all characters are ASCII characters at this point,
                //  the boundary will be decreased for every non-ASCII character we encounter
                // Also, we need 3 + 4 chars reserve for the unrolled ansi decoding loop and for decoding of surrogates
                char *pStop = pSrc + availableChars - (3 + 4);

                while (pSrc < pStop) {
                    ch = *pSrc;
                    pSrc++;

                    if (ch > 0x7F)                                                  // Not ASCII
                    {
                        if (ch > 0x7FF)                                             // Not 2 Byte
                        {
                            if ((ch & 0xF800) == 0xD800)                            // See if its a Surrogate
                                goto LongCode;
                            byteCount++;
                        }
                        byteCount ++;
                    }

                    // get pSrc aligned
                    if ((unchecked((int)pSrc) & 0x2) != 0) {
                        ch = *pSrc;
                        pSrc++;
                        if (ch > 0x7F)                                              // Not ASCII
                        {
                            if (ch > 0x7FF)                                         // Not 2 Byte
                            {
                                if ((ch & 0xF800) == 0xD800)                        // See if its a Surrogate
                                    goto LongCode;
                                byteCount++;
                            }
                            byteCount ++;
                        }
                    }

                    // Run 2 * 4 characters at a time!
                    while (pSrc < pStop) {
                        ch = *(int*)pSrc;
                        int chc = *(int*)(pSrc+2);
                        if (((ch | chc) & unchecked((int)0xFF80FF80)) != 0)         // See if not ASCII
                        {
                            if (((ch | chc) & unchecked((int)0xF800F800)) != 0)     // See if not 2 Byte
                            {
                                goto LongCodeWithMask;
                            }


                            if ((ch & unchecked((int)0xFF800000)) != 0)             // Actually 0x07800780 is all we care about (4 bits)
                                byteCount++;
                            if ((ch & unchecked((int)0xFF80)) != 0)
                                byteCount++;
                            if ((chc & unchecked((int)0xFF800000)) != 0)
                                byteCount++;
                            if ((chc & unchecked((int)0xFF80)) != 0)
                                byteCount++;
                        }
                        pSrc += 4;

                        ch = *(int*)pSrc;
                        chc = *(int*)(pSrc+2);
                        if (((ch | chc) & unchecked((int)0xFF80FF80)) != 0)         // See if not ASCII
                        {
                            if (((ch | chc) & unchecked((int)0xF800F800)) != 0)     // See if not 2 Byte
                            {
                                goto LongCodeWithMask;
                            }

                            if ((ch & unchecked((int)0xFF800000)) != 0)
                                byteCount++;
                            if ((ch & unchecked((int)0xFF80)) != 0)
                                byteCount++;
                            if ((chc & unchecked((int)0xFF800000)) != 0)
                                byteCount++;
                            if ((chc & unchecked((int)0xFF80)) != 0)
                                byteCount++;
                        }
                        pSrc += 4;
                    }
                    break;

                LongCodeWithMask:
#if BIGENDIAN
                    // be careful about the sign extension
                    ch = (int)(((uint)ch) >> 16);
#else // BIGENDIAN
                    ch = (char)ch;
#endif // BIGENDIAN
                    pSrc++;

                    if (ch <= 0x7F) {
                        continue;
                    }

                LongCode:
                    // use separate helper variables for slow and fast loop so that the jit optimizations
                    // won't get confused about the variable lifetimes
                    if (ch > 0x7FF) {
                        // if (IsLowSurrogate(ch) || IsHighSurrogate(ch))
                        if (InRange(ch, CharUnicodeInfo.HIGH_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END)) {
                            // 4 byte encoding - high surrogate + low surrogate

                            int chd = *pSrc;
                            if (
                                // !IsHighSurrogate(ch) // low without high -> bad
                                ch > CharUnicodeInfo.HIGH_SURROGATE_END ||
                                // !IsLowSurrogate(chd) // high not followed by low -> bad
                                !InRange(chd, CharUnicodeInfo.LOW_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END) )
                            {
                                // Back up and drop out to slow loop to figure out error
                                pSrc--;
                                break;
                            }
                            pSrc++;

                            // byteCount - this byte is compensated by the second surrogate character
                        }
                        byteCount++;
                    }
                    byteCount++;

                    // byteCount - the last byte is already included
                }
#endif // FASTLOOP

                // no pending char at this point
                ch = 0;
            }

#if BIT64
            // check for overflow
            if (byteCount < 0) {
                throw new ArgumentException(
                        Environment.GetResourceString("Argument_ConversionOverflow"));
            }
#endif

            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[UTF8Encoding.GetByteCount]Expected Empty fallback buffer");

            return byteCount;
        }

        // diffs two char pointers using unsigned arithmetic. The unsigned arithmetic
        // is good enough for us, and it tends to generate better code than the signed
        // arithmetic generated by default
        [System.Security.SecurityCritical]  // auto-generated
        unsafe private static int PtrDiff(char *a, char* b)
        {
            return (int)(((uint)((byte*)a - (byte*)b)) >> 1);
        }

        // byte* flavor just for parity
        [System.Security.SecurityCritical]  // auto-generated
        unsafe private static int PtrDiff(byte* a, byte* b)
        {
            return (int)(a - b);
        }

        private static bool InRange(int ch, int start, int end)
        {
            return (uint)(ch - start) <= (uint)(end - start);
        }

        // Our workhorse
        // Note:  We ignore mismatched surrogates, unless the exception flag is set in which case we throw
        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetBytes(char* chars, int charCount,
                                                byte* bytes, int byteCount, EncoderNLS baseEncoder)
        {
            Contract.Assert(chars!=null, "[UTF8Encoding.GetBytes]chars!=null");
            Contract.Assert(byteCount >=0, "[UTF8Encoding.GetBytes]byteCount >=0");
            Contract.Assert(charCount >=0, "[UTF8Encoding.GetBytes]charCount >=0");
            Contract.Assert(bytes!=null, "[UTF8Encoding.GetBytes]bytes!=null");

            UTF8Encoder encoder = null;

            // For fallback we may need a fallback buffer.
            // We wait to initialize it though in case we don't have any broken input unicode
            EncoderFallbackBuffer fallbackBuffer = null;
            char *pSrc = chars;
            byte *pTarget = bytes;

            char *pEnd = pSrc+charCount;
            byte *pAllocatedBufferEnd = pTarget+byteCount;

            int ch = 0;

            // assume that JIT will enregister pSrc, pTarget and ch

            if (baseEncoder != null) {
                encoder = (UTF8Encoder)baseEncoder;
                ch = encoder.surrogateChar;

                // We mustn't have left over fallback data when counting
                if (encoder.InternalHasFallbackBuffer)
                {
                    // We always need the fallback buffer in get bytes so we can flush any remaining ones if necessary
                    fallbackBuffer = encoder.FallbackBuffer;
                    if (fallbackBuffer.Remaining > 0 && encoder.m_throwOnOverflow)
                        throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty",
                        this.EncodingName, encoder.Fallback.GetType()));

                    // Set our internal fallback interesting things.
                    fallbackBuffer.InternalInitialize(chars, pEnd, encoder, true);
                }
            }

            for (;;) {
                // SLOWLOOP: does all range checks, handles all special cases, but it is slow

                if (pSrc >= pEnd) {
                    
                    if (ch == 0) {
                        // Check if there's anthing left to get out of the fallback buffer
                        ch = fallbackBuffer != null ? fallbackBuffer.InternalGetNextChar() : 0;
                        if (ch > 0) {
                            goto ProcessChar;
                        }
                    } else {
                        // Case of leftover surrogates in the fallback buffer
                        if (fallbackBuffer != null && fallbackBuffer.bFallingBack) {
                            Contract.Assert(ch >= 0xD800 && ch <= 0xDBFF,
                                "[UTF8Encoding.GetBytes]expected high surrogate, not 0x" + ((int)ch).ToString("X4", CultureInfo.InvariantCulture));
                            
                            int cha = ch;
                            
                            ch = fallbackBuffer.InternalGetNextChar();
                            
                            if (InRange(ch, CharUnicodeInfo.LOW_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END)) {
                                ch = ch + (cha << 10) + (0x10000 - CharUnicodeInfo.LOW_SURROGATE_START - (CharUnicodeInfo.HIGH_SURROGATE_START << 10));
                                goto EncodeChar;
                            } else if (ch > 0){
                                goto ProcessChar;
                            } else {
                                break;
                            }
                        }
                    }
                    
                    // attempt to encode the partial surrogate (will fail or ignore)
                    if (ch > 0 && (encoder == null || encoder.MustFlush))
                        goto EncodeChar;

                    // We're done
                    break;
                }

                if (ch > 0) {
                    // We have a high surrogate left over from a previous loop.
                    Contract.Assert(ch >= 0xD800 && ch <= 0xDBFF,
                        "[UTF8Encoding.GetBytes]expected high surrogate, not 0x" + ((int)ch).ToString("X4", CultureInfo.InvariantCulture));

                    // use separate helper variables for local contexts so that the jit optimizations
                    // won't get confused about the variable lifetimes
                    int cha = *pSrc;

                    // In previous byte, we encountered a high surrogate, so we are expecting a low surrogate here.
                    // if (IsLowSurrogate(cha)) {
                    if (InRange(cha, CharUnicodeInfo.LOW_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END)) {
                        ch = cha + (ch << 10) +
                            (0x10000
                            - CharUnicodeInfo.LOW_SURROGATE_START
                            - (CharUnicodeInfo.HIGH_SURROGATE_START << 10) );

                        pSrc++;
                    }
                    // else ch is still high surrogate and encoding will fail

                    // attempt to encode the surrogate or partial surrogate
                    goto EncodeChar;
                }

                // If we've used a fallback, then we have to check for it
                if (fallbackBuffer != null)
                {
                    ch = fallbackBuffer.InternalGetNextChar();
                    if (ch > 0) goto ProcessChar;
                }

                // read next char. The JIT optimization seems to be getting confused when
                // compiling "ch = *pSrc++;", so rather use "ch = *pSrc; pSrc++;" instead
                ch = *pSrc;
                pSrc++;

            ProcessChar:
                // if (IsHighSurrogate(ch)) {
                if (InRange(ch, CharUnicodeInfo.HIGH_SURROGATE_START, CharUnicodeInfo.HIGH_SURROGATE_END)) {
                    continue;
                }
                // either good char or partial surrogate

            EncodeChar:
                // throw exception on partial surrogate if necessary
                // if (IsLowSurrogate(ch) || IsHighSurrogate(ch))
                if (InRange(ch, CharUnicodeInfo.HIGH_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END))
                {
                    // Lone surrogates aren't allowed, we have to do fallback for them
                    // Have to make a fallback buffer if we don't have one
                    if (fallbackBuffer == null)
                    {
                        // wait on fallbacks if we can
                        // For fallback we may need a fallback buffer
                        if (baseEncoder == null)
                            fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = baseEncoder.FallbackBuffer;

                        // Set our internal fallback interesting things.
                        fallbackBuffer.InternalInitialize(chars, pEnd, baseEncoder, true);
                    }

                    // Do our fallback.  Actually we already know its a mixed up surrogate,
                    // so the ref pSrc isn't gonna do anything.
                    fallbackBuffer.InternalFallback(unchecked((char)ch), ref pSrc);

                    // Ignore it if we don't throw
                    ch = 0;
                    continue;
                }

                // Count bytes needed
                int bytesNeeded = 1;
                if (ch > 0x7F) {
                    if (ch > 0x7FF) {
                        if (ch > 0xFFFF) {
                            bytesNeeded++;  // 4 bytes (surrogate pair)
                        }
                        bytesNeeded++;      // 3 bytes (800-FFFF)
                    }
                    bytesNeeded++;          // 2 bytes (80-7FF)
                }

                if (pTarget > pAllocatedBufferEnd - bytesNeeded) {
                    // Left over surrogate from last time will cause pSrc == chars, so we'll throw
                    if (fallbackBuffer != null && fallbackBuffer.bFallingBack)
                    {
                        fallbackBuffer.MovePrevious();              // Didn't use this fallback char
                        if (ch > 0xFFFF)
                            fallbackBuffer.MovePrevious();          // Was surrogate, didn't use 2nd part either
                    }
                    else
                    {
                        pSrc--;                                     // Didn't use this char
                        if (ch > 0xFFFF)
                            pSrc--;                                 // Was surrogate, didn't use 2nd part either
                    }
                    Contract.Assert(pSrc >= chars || pTarget == bytes,
                        "[UTF8Encoding.GetBytes]Expected pSrc to be within buffer or to throw with insufficient room.");
                    ThrowBytesOverflow(encoder, pTarget == bytes);  // Throw if we must
                    ch = 0;                                         // Nothing left over (we backed up to start of pair if supplimentary)
                    break;
                }

                if (ch <= 0x7F) {
                    *pTarget = (byte)ch;
                }
                else {
                    // use separate helper variables for local contexts so that the jit optimizations
                    // won't get confused about the variable lifetimes
                    int chb;
                    if (ch <= 0x7FF) {
                        // 2 byte encoding
                        chb = (byte)(unchecked((sbyte)0xC0) | (ch >> 6));
                    }
                    else
                    {
                        if (ch <= 0xFFFF) {
                            chb = (byte)(unchecked((sbyte)0xE0) | (ch >> 12));
                        }
                        else
                        {
                            *pTarget = (byte)(unchecked((sbyte)0xF0) | (ch >> 18));
                            pTarget++;

                            chb = unchecked((sbyte)0x80) | (ch >> 12) & 0x3F;
                        }
                        *pTarget = (byte)chb;
                        pTarget++;

                        chb = unchecked((sbyte)0x80) | (ch >> 6) & 0x3F;
                    }
                    *pTarget = (byte)chb;
                    pTarget++;

                    *pTarget = (byte)(unchecked((sbyte)0x80) | ch & 0x3F);
                }
                pTarget++;


#if FASTLOOP
                // If still have fallback don't do fast loop
                if (fallbackBuffer != null && (ch = fallbackBuffer.InternalGetNextChar()) != 0)
                    goto ProcessChar;

                int availableChars = PtrDiff(pEnd, pSrc);
                int availableBytes = PtrDiff(pAllocatedBufferEnd, pTarget);

                // don't fall into the fast decoding loop if we don't have enough characters
                // Note that if we don't have enough bytes, pStop will prevent us from entering the fast loop.
                if (availableChars <= 13) {
                    // we are hoping for 1 byte per char
                    if (availableBytes < availableChars) {
                        // not enough output room.  no pending bits at this point
                        ch = 0;
                        continue;
                    }
                    
                    // try to get over the remainder of the ascii characters fast though
                    char* pLocalEnd = pEnd; // hint to get pLocalEnd enregistered
                    while (pSrc < pLocalEnd) {
                        ch = *pSrc;
                        pSrc++;

                        // Not ASCII, need more than 1 byte per char
                        if (ch > 0x7F)
                            goto ProcessChar;

                        *pTarget = (byte)ch;
                        pTarget++;
                    }
                    // we are done, let ch be 0 to clear encoder
                    ch = 0;
                    break;
                }

                // we need at least 1 byte per character, but Convert might allow us to convert
                // only part of the input, so try as much as we can.  Reduce charCount if necessary
                if (availableBytes < availableChars)
                {
                    availableChars = availableBytes;
                }

                // FASTLOOP:
                // - optimistic range checks
                // - fallbacks to the slow loop for all special cases, exception throwing, etc.

                // To compute the upper bound, assume that all characters are ASCII characters at this point,
                //  the boundary will be decreased for every non-ASCII character we encounter
                // Also, we need 5 chars reserve for the unrolled ansi decoding loop and for decoding of surrogates
                // If there aren't enough bytes for the output, then pStop will be <= pSrc and will bypass the loop.
                char *pStop = pSrc + availableChars - 5;

                while (pSrc < pStop) {
                    ch = *pSrc;
                    pSrc++;

                    if (ch > 0x7F) {
                        goto LongCode;
                    }
                    *pTarget = (byte)ch;
                    pTarget++;

                    // get pSrc aligned
                    if ((unchecked((int)pSrc) & 0x2) != 0) {
                        ch = *pSrc;
                        pSrc++;
                        if (ch > 0x7F) {
                            goto LongCode;
                        }
                        *pTarget = (byte)ch;
                        pTarget++;
                    }

                    // Run 4 characters at a time!
                    while (pSrc < pStop) {
                        ch = *(int*)pSrc;
                        int chc = *(int*)(pSrc+2);
                        if (((ch | chc) & unchecked((int)0xFF80FF80)) != 0) {
                            goto LongCodeWithMask;
                        }

                        // Unfortunately, this is endianess sensitive
#if BIGENDIAN
                        *pTarget = (byte)(ch>>16);
                        *(pTarget+1) = (byte)ch;
                        pSrc += 4;
                        *(pTarget+2) = (byte)(chc>>16);
                        *(pTarget+3) = (byte)chc;
                        pTarget += 4;
#else // BIGENDIAN
                        *pTarget = (byte)ch;
                        *(pTarget+1) = (byte)(ch>>16);
                        pSrc += 4;
                        *(pTarget+2) = (byte)chc;
                        *(pTarget+3) = (byte)(chc>>16);
                        pTarget += 4;
#endif // BIGENDIAN
                    }
                    continue;

                LongCodeWithMask:
#if BIGENDIAN
                    // be careful about the sign extension
                    ch = (int)(((uint)ch) >> 16);
#else // BIGENDIAN
                    ch = (char)ch;
#endif // BIGENDIAN
                    pSrc++;

                    if (ch > 0x7F) {
                        goto LongCode;
                    }
                    *pTarget = (byte)ch;
                    pTarget++;
                    continue;

                LongCode:
                    // use separate helper variables for slow and fast loop so that the jit optimizations
                    // won't get confused about the variable lifetimes
                    int chd;
                    if (ch <= 0x7FF) {
                        // 2 byte encoding
                        chd = unchecked((sbyte)0xC0) | (ch >> 6);
                    }
                    else {
                        // if (!IsLowSurrogate(ch) && !IsHighSurrogate(ch))
                        if (!InRange(ch, CharUnicodeInfo.HIGH_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END)) {
                            // 3 byte encoding
                            chd = unchecked((sbyte)0xE0) | (ch >> 12);
                        }
                        else
                        {
                            // 4 byte encoding - high surrogate + low surrogate
                            // if (!IsHighSurrogate(ch))
                            if (ch > CharUnicodeInfo.HIGH_SURROGATE_END) {
                                // low without high -> bad, try again in slow loop
                                pSrc -= 1;
                                break;
                            }

                            chd = *pSrc;
                            pSrc++;

                            // if (!IsLowSurrogate(chd)) {
                            if (!InRange(chd, CharUnicodeInfo.LOW_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END)) {
                                // high not followed by low -> bad, try again in slow loop
                                pSrc -= 2;
                                break;
                            }

                            ch = chd + (ch << 10) +
                                (0x10000
                                - CharUnicodeInfo.LOW_SURROGATE_START
                                - (CharUnicodeInfo.HIGH_SURROGATE_START << 10) );

                            *pTarget = (byte)(unchecked((sbyte)0xF0) | (ch >> 18));
                            // pStop - this byte is compensated by the second surrogate character
                            // 2 input chars require 4 output bytes.  2 have been anticipated already
                            // and 2 more will be accounted for by the 2 pStop-- calls below.
                            pTarget++;                           

                            chd = unchecked((sbyte)0x80) | (ch >> 12) & 0x3F;
                        }
                        *pTarget = (byte)chd;
                        pStop--;                    // 3 byte sequence for 1 char, so need pStop-- and the one below too.
                        pTarget++;

                        chd = unchecked((sbyte)0x80) | (ch >> 6) & 0x3F;
                    }
                    *pTarget = (byte)chd;
                    pStop--;                        // 2 byte sequence for 1 char so need pStop--.
                    pTarget++;

                    *pTarget = (byte)(unchecked((sbyte)0x80) | ch & 0x3F);
                    // pStop - this byte is already included
                    pTarget++;
                }

                Contract.Assert(pTarget <= pAllocatedBufferEnd, "[UTF8Encoding.GetBytes]pTarget <= pAllocatedBufferEnd");

#endif // FASTLOOP

                // no pending char at this point
                ch = 0;
            }

            // Do we have to set the encoder bytes?
            if (encoder != null)
            {
                Contract.Assert(!encoder.MustFlush || ch == 0,
                    "[UTF8Encoding.GetBytes] Expected no mustflush or 0 leftover ch " + ch.ToString("X2", CultureInfo.InvariantCulture));

                encoder.surrogateChar = ch;
                encoder.m_charsUsed = (int)(pSrc - chars);
            }

            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0 ||
                baseEncoder == null || !baseEncoder.m_throwOnOverflow,
                "[UTF8Encoding.GetBytes]Expected empty fallback buffer if not converting");

            return (int)(pTarget - bytes);
        }


        // These are bitmasks used to maintain the state in the decoder. They occupy the higher bits
        // while the actual character is being built in the lower bits. They are shifted together
        // with the actual bits of the character.

        // bits 30 & 31 are used for pending bits fixup
        private const int FinalByte         = 1 << 29;
        private const int SupplimentarySeq  = 1 << 28;
        private const int ThreeByteSeq      = 1 << 27;

        // Note:  We throw exceptions on individually encoded surrogates and other non-shortest forms.
        //        If exceptions aren't turned on, then we drop all non-shortest &individual surrogates.
        //
        // To simplify maintenance, the structure of GetCharCount and GetChars should be
        // kept the same as much as possible
        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetCharCount(byte* bytes, int count, DecoderNLS baseDecoder)
        {
            Contract.Assert(count >=0, "[UTF8Encoding.GetCharCount]count >=0");
            Contract.Assert(bytes!=null, "[UTF8Encoding.GetCharCount]bytes!=null");

            // Initialize stuff
            byte *pSrc = bytes;
            byte *pEnd = pSrc+count;

            // Start by assuming we have as many as count, charCount always includes the adjustment
            // for the character being decoded
            int charCount = count;
            int ch = 0;
            DecoderFallbackBuffer fallback = null;

            if (baseDecoder != null) {
                UTF8Decoder decoder = (UTF8Decoder)baseDecoder;
                ch = decoder.bits;
                charCount -= (ch >> 30);        // Adjust char count for # of expected bytes and expected output chars.

                // Shouldn't have anything in fallback buffer for GetCharCount
                // (don't have to check m_throwOnOverflow for count)
                Contract.Assert(!decoder.InternalHasFallbackBuffer || decoder.FallbackBuffer.Remaining == 0,
                    "[UTF8Encoding.GetCharCount]Expected empty fallback buffer at start");
            }

            for (;;)
            {
                // SLOWLOOP: does all range checks, handles all special cases, but it is slow

                if (pSrc >= pEnd) {
                    break;
                }

                if (ch == 0) {
                    // no pending bits
                    goto ReadChar;
                }

                // read next byte. The JIT optimization seems to be getting confused when
                // compiling "ch = *pSrc++;", so rather use "ch = *pSrc; pSrc++;" instead
                int cha = *pSrc;
                pSrc++;

                // we are expecting to see trailing bytes like 10vvvvvv
                if ((cha & unchecked((sbyte)0xC0)) != 0x80) {
                    // This can be a valid starting byte for another UTF8 byte sequence, so let's put
                    // the current byte back, and try to see if this is a valid byte for another UTF8 byte sequence
                    pSrc--;
                    charCount += (ch >> 30);
                    goto InvalidByteSequence;
                }

                // fold in the new byte
                ch = (ch << 6) | (cha & 0x3F);

                if ((ch & FinalByte) == 0) {
                    Contract.Assert( (ch & (SupplimentarySeq | ThreeByteSeq)) != 0,
                        "[UTF8Encoding.GetChars]Invariant volation");

                    if ((ch & SupplimentarySeq) != 0) {
                        if ((ch & (FinalByte >> 6)) != 0) {
                            // this is 3rd byte (of 4 byte supplimentary) - nothing to do
                            continue;
                        }

                        // 2nd byte, check for non-shortest form of supplimentary char and the valid
                        // supplimentary characters in range 0x010000 - 0x10FFFF at the same time
                        if (!InRange(ch & 0x1F0, 0x10, 0x100)) {
                            goto InvalidByteSequence;
                        }
                    }
                    else {
                        // Must be 2nd byte of a 3-byte sequence
                        // check for non-shortest form of 3 byte seq
                        if ((ch & (0x1F << 5)) == 0 ||                  // non-shortest form
                            (ch & (0xF800 >> 6) ) == (0xD800 >> 6))     // illegal individually encoded surrogate
                        {
                            goto InvalidByteSequence;
                        }
                    }
                    continue;
                }

                // ready to punch

                // adjust for surrogates in non-shortest form
                if ((ch & (SupplimentarySeq | 0x1F0000)) == SupplimentarySeq) {
                    charCount--;
                }
                goto EncodeChar;

            InvalidByteSequence:
                // this code fragment should be close to the gotos referencing it
                // Have to do fallback for invalid bytes
                if (fallback == null)
                {
                    if (baseDecoder == null)
                        fallback = this.decoderFallback.CreateFallbackBuffer();
                    else
                        fallback = baseDecoder.FallbackBuffer;
                    fallback.InternalInitialize(bytes, null);
                }
                charCount += FallbackInvalidByteSequence(pSrc, ch, fallback);

                ch = 0;
                continue;

            ReadChar:
                ch = *pSrc;
                pSrc++;

            ProcessChar:
                if (ch > 0x7F) {
                    // If its > 0x7F, its start of a new multi-byte sequence

                    // Long sequence, so unreserve our char.
                    charCount--;

                    // bit 6 has to be non-zero for start of multibyte chars.
                    if ((ch & 0x40) == 0) {
                        // Unexpected trail byte
                        goto InvalidByteSequence;
                    }

                    // start a new long code
                    if ((ch & 0x20) != 0) {
                        if ((ch & 0x10) != 0) {
                            // 4 byte encoding - supplimentary character (2 surrogates)

                            ch &= 0x0F;

                            // check that bit 4 is zero and the valid supplimentary character
                            // range 0x000000 - 0x10FFFF at the same time
                            if (ch > 0x04) {
                                ch |= 0xf0;
                                goto InvalidByteSequence;
                            }

                            // Add bit flags so that when we check new characters & rotate we'll be flagged correctly.
                            // Final byte flag, count fix if we don't make final byte & supplimentary sequence flag.
                            ch |= (FinalByte >> 3*6) |  // Final byte is 3 more bytes from now
                                  (1 << 30) |           // If it dies on next byte we'll need an extra char
                                  (3 << (30-2*6)) |     // If it dies on last byte we'll need to subtract a char
                                (SupplimentarySeq) | (SupplimentarySeq >> 6) |
                                (SupplimentarySeq >> 2*6) | (SupplimentarySeq >> 3*6);

                            // Our character count will be 2 characters for these 4 bytes, so subtract another char
                            charCount--;
                        }
                        else {
                            // 3 byte encoding
                            // Add bit flags so that when we check new characters & rotate we'll be flagged correctly.
                            ch = (ch & 0x0F) | ( (FinalByte >> 2*6) | (1 << 30) |
                                (ThreeByteSeq) | (ThreeByteSeq >> 6) | (ThreeByteSeq >> 2*6) );

                            // We'll expect 1 character for these 3 bytes, so subtract another char.
                            charCount--;
                        }
                    }
                    else {
                        // 2 byte encoding

                        ch &= 0x1F;

                        // check for non-shortest form
                        if (ch <= 1) {
                            ch |= 0xc0;
                            goto InvalidByteSequence;
                        }

                        // Add bit flags so we'll be flagged correctly
                        ch |= (FinalByte >> 6);
                    }
                    continue;
                }

            EncodeChar:

#if FASTLOOP
                int availableBytes = PtrDiff(pEnd, pSrc);

                // don't fall into the fast decoding loop if we don't have enough bytes
                if (availableBytes <= 13) {
                    // try to get over the remainder of the ascii characters fast though
                    byte* pLocalEnd = pEnd; // hint to get pLocalEnd enregistered
                    while (pSrc < pLocalEnd) {
                        ch = *pSrc;
                        pSrc++;

                        if (ch > 0x7F)
                            goto ProcessChar;
                    }
                    // we are done
                    ch = 0;
                    break;
                }

                // To compute the upper bound, assume that all characters are ASCII characters at this point,
                //  the boundary will be decreased for every non-ASCII character we encounter
                // Also, we need 7 chars reserve for the unrolled ansi decoding loop and for decoding of multibyte sequences
                byte *pStop = pSrc + availableBytes - 7;

                while (pSrc < pStop) {
                    ch = *pSrc;
                    pSrc++;

                    if (ch > 0x7F) {
                        goto LongCode;
                    }

                    // get pSrc 2-byte aligned
                    if ((unchecked((int)pSrc) & 0x1) != 0) {
                        ch = *pSrc;
                        pSrc++;
                        if (ch > 0x7F) {
                            goto LongCode;
                        }
                    }

                    // get pSrc 4-byte aligned
                    if ((unchecked((int)pSrc) & 0x2) != 0) {
                        ch = *(ushort*)pSrc;
                        if ((ch & 0x8080) != 0) {
                            goto LongCodeWithMask16;
                        }
                        pSrc += 2;
                    }

                    // Run 8 + 8 characters at a time!
                    while (pSrc < pStop) {
                        ch = *(int*)pSrc;
                        int chb = *(int*)(pSrc+4);
                        if (((ch | chb) & unchecked((int)0x80808080)) != 0) {
                            goto LongCodeWithMask32;
                        }
                        pSrc += 8;

                        // This is a really small loop - unroll it
                        if (pSrc >= pStop)
                            break;

                        ch = *(int*)pSrc;
                        chb = *(int*)(pSrc+4);
                        if (((ch | chb) & unchecked((int)0x80808080)) != 0) {
                            goto LongCodeWithMask32;
                        }
                        pSrc += 8;
                    }
                    break;

#if BIGENDIAN
                LongCodeWithMask32:
                    // be careful about the sign extension
                    ch = (int)(((uint)ch) >> 16);
                LongCodeWithMask16:
                    ch = (int)(((uint)ch) >> 8);
#else // BIGENDIAN
                LongCodeWithMask32:
                LongCodeWithMask16:
                    ch &= 0xFF;
#endif // BIGENDIAN
                    pSrc++;
                    if (ch <= 0x7F) {
                        continue;
                    }

                LongCode:
                    int chc = *pSrc;
                    pSrc++;

                    if (
                        // bit 6 has to be zero
                        (ch & 0x40) == 0 ||
                        // we are expecting to see trailing bytes like 10vvvvvv
                        (chc & unchecked((sbyte)0xC0)) != 0x80)
                    {
                        goto BadLongCode;
                    }

                    chc &= 0x3F;

                    // start a new long code
                    if ((ch & 0x20) != 0) {

                        // fold the first two bytes together
                        chc |= (ch & 0x0F) << 6;

                        if ((ch & 0x10) != 0) {
                            // 4 byte encoding - surrogate
                            ch = *pSrc;
                            if (
                                // check that bit 4 is zero, the non-shortest form of surrogate
                                // and the valid surrogate range 0x000000 - 0x10FFFF at the same time
                                !InRange(chc >> 4, 0x01, 0x10) ||
                                // we are expecting to see trailing bytes like 10vvvvvv
                                (ch & unchecked((sbyte)0xC0)) != 0x80 )
                            {
                                goto BadLongCode;
                            }

                            chc = (chc << 6) | (ch & 0x3F);

                            ch = *(pSrc+1);
                            // we are expecting to see trailing bytes like 10vvvvvv
                            if ((ch & unchecked((sbyte)0xC0)) != 0x80) {
                                goto BadLongCode;
                            }
                            pSrc += 2;

                            // extra byte
                            charCount--;
                        }
                        else {
                            // 3 byte encoding
                            ch = *pSrc;
                            if (
                                // check for non-shortest form of 3 byte seq
                                (chc & (0x1F << 5)) == 0 ||
                                // Can't have surrogates here.
                                (chc & (0xF800 >> 6) ) == (0xD800 >> 6) ||
                                // we are expecting to see trailing bytes like 10vvvvvv
                                (ch & unchecked((sbyte)0xC0)) != 0x80 )
                            {
                                goto BadLongCode;
                            }
                            pSrc++;

                            // extra byte
                            charCount--;
                        }
                    }
                    else {
                        // 2 byte encoding

                        // check for non-shortest form
                        if ((ch & 0x1E) == 0) {
                            goto BadLongCode;
                        }
                    }

                    // extra byte
                    charCount--;
                }
#endif // FASTLOOP

                // no pending bits at this point
                ch = 0;
                continue;

            BadLongCode:
                pSrc -= 2;
                ch = 0;
                continue;
            }

            // May have a problem if we have to flush
            if (ch != 0)
            {
                // We were already adjusting for these, so need to unadjust
                charCount += (ch >> 30);
                if (baseDecoder == null || baseDecoder.MustFlush)
                {
                    // Have to do fallback for invalid bytes
                    if (fallback == null)
                    {
                        if (baseDecoder == null)
                            fallback = this.decoderFallback.CreateFallbackBuffer();
                        else
                            fallback = baseDecoder.FallbackBuffer;
                        fallback.InternalInitialize(bytes, null);
                    }
                    charCount += FallbackInvalidByteSequence(pSrc, ch, fallback);
                }
            }

            // Shouldn't have anything in fallback buffer for GetCharCount
            // (don't have to check m_throwOnOverflow for count)
            Contract.Assert(fallback == null || fallback.Remaining == 0,
                "[UTF8Encoding.GetCharCount]Expected empty fallback buffer at end");

            return charCount;
        }

        // WARNING:  If we throw an error, then System.Resources.ResourceReader calls this method.
        //           So if we're really broken, then that could also throw an error... recursively.
        //           So try to make sure GetChars can at least process all uses by
        //           System.Resources.ResourceReader!
        //
        // Note:  We throw exceptions on individually encoded surrogates and other non-shortest forms.
        //        If exceptions aren't turned on, then we drop all non-shortest &individual surrogates.
        //
        // To simplify maintenance, the structure of GetCharCount and GetChars should be
        // kept the same as much as possible
        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetChars(byte* bytes, int byteCount,
                                                char* chars, int charCount, DecoderNLS baseDecoder)
        {
            Contract.Assert(chars!=null, "[UTF8Encoding.GetChars]chars!=null");
            Contract.Assert(byteCount >=0, "[UTF8Encoding.GetChars]count >=0");
            Contract.Assert(charCount >=0, "[UTF8Encoding.GetChars]charCount >=0");
            Contract.Assert(bytes!=null, "[UTF8Encoding.GetChars]bytes!=null");

            byte *pSrc = bytes;
            char *pTarget = chars;

            byte *pEnd = pSrc+byteCount;
            char *pAllocatedBufferEnd = pTarget+charCount;

            int ch = 0;

            DecoderFallbackBuffer fallback = null;
            if (baseDecoder != null) {
                UTF8Decoder decoder = (UTF8Decoder)baseDecoder;
                ch = decoder.bits;

                // Shouldn't have anything in fallback buffer for GetChars
                // (don't have to check m_throwOnOverflow for chars, we always use all or none so always should be empty)
                Contract.Assert(!decoder.InternalHasFallbackBuffer || decoder.FallbackBuffer.Remaining == 0,
                    "[UTF8Encoding.GetChars]Expected empty fallback buffer at start");
            }

            for (;;)
            {
                // SLOWLOOP: does all range checks, handles all special cases, but it is slow

                if (pSrc >= pEnd) {
                    break;
                }

                if (ch == 0) {
                    // no pending bits
                    goto ReadChar;
                }

                // read next byte. The JIT optimization seems to be getting confused when
                // compiling "ch = *pSrc++;", so rather use "ch = *pSrc; pSrc++;" instead
                int cha = *pSrc;
                pSrc++;

                // we are expecting to see trailing bytes like 10vvvvvv
                if ((cha & unchecked((sbyte)0xC0)) != 0x80) {
                    // This can be a valid starting byte for another UTF8 byte sequence, so let's put
                    // the current byte back, and try to see if this is a valid byte for another UTF8 byte sequence
                    pSrc--;
                    goto InvalidByteSequence;
                }

                // fold in the new byte
                ch = (ch << 6) | (cha & 0x3F);

                if ((ch & FinalByte) == 0) {
                    // Not at last byte yet
                    Contract.Assert( (ch & (SupplimentarySeq | ThreeByteSeq)) != 0,
                        "[UTF8Encoding.GetChars]Invariant volation");

                    if ((ch & SupplimentarySeq) != 0) {
                        // Its a 4-byte supplimentary sequence
                        if ((ch & (FinalByte >> 6)) != 0) {
                            // this is 3rd byte of 4 byte sequence - nothing to do
                            continue;
                        }

                        // 2nd byte of 4 bytes
                        // check for non-shortest form of surrogate and the valid surrogate
                        // range 0x000000 - 0x10FFFF at the same time
                        if (!InRange(ch & 0x1F0, 0x10, 0x100)) {
                            goto InvalidByteSequence;
                        }
                    }
                    else {
                        // Must be 2nd byte of a 3-byte sequence
                        // check for non-shortest form of 3 byte seq
                        if ((ch & (0x1F << 5)) == 0 ||                  // non-shortest form
                            (ch & (0xF800 >> 6) ) == (0xD800 >> 6))     // illegal individually encoded surrogate
                        {
                            goto InvalidByteSequence;
                        }
                    }
                    continue;
                }

                // ready to punch

                // surrogate in shortest form?
                // Might be possible to get rid of this?  Already did non-shortest check for 4-byte sequence when reading 2nd byte?
                if ((ch & (SupplimentarySeq | 0x1F0000)) > SupplimentarySeq) {
                    // let the range check for the second char throw the exception
                    if (pTarget < pAllocatedBufferEnd) {
                        *pTarget = (char)( ((ch >> 10) & 0x7FF) +
                            unchecked((short)((CharUnicodeInfo.HIGH_SURROGATE_START - (0x10000 >> 10)))) );
                        pTarget++;

                        ch = (ch & 0x3FF) +
                            unchecked((int)(CharUnicodeInfo.LOW_SURROGATE_START));
                    }
                }

                goto EncodeChar;

            InvalidByteSequence:
                // this code fragment should be close to the gotos referencing it
                // Have to do fallback for invalid bytes
                if (fallback == null)
                {
                    if (baseDecoder == null)
                        fallback = this.decoderFallback.CreateFallbackBuffer();
                    else
                        fallback = baseDecoder.FallbackBuffer;
                    fallback.InternalInitialize(bytes, pAllocatedBufferEnd);
                }
                // This'll back us up the appropriate # of bytes if we didn't get anywhere
                if (!FallbackInvalidByteSequence(ref pSrc, ch, fallback, ref pTarget))
                {
                    // Ran out of buffer space
                    // Need to throw an exception?
                    Contract.Assert(pSrc >= bytes || pTarget == chars,
                        "[UTF8Encoding.GetChars]Expected to throw or remain in byte buffer after fallback");
                    fallback.InternalReset();
                    ThrowCharsOverflow(baseDecoder, pTarget == chars);
                    ch = 0;
                    break;
                }
                Contract.Assert(pSrc >= bytes, 
                    "[UTF8Encoding.GetChars]Expected invalid byte sequence to have remained within the byte array");
                ch = 0;
                continue;

            ReadChar:
                ch = *pSrc;
                pSrc++;

            ProcessChar:
                if (ch > 0x7F) {
                    // If its > 0x7F, its start of a new multi-byte sequence

                    // bit 6 has to be non-zero
                    if ((ch & 0x40) == 0) {
                        goto InvalidByteSequence;
                    }

                    // start a new long code
                    if ((ch & 0x20) != 0) {
                        if ((ch & 0x10) != 0) {
                            // 4 byte encoding - supplimentary character (2 surrogates)

                            ch &= 0x0F;

                            // check that bit 4 is zero and the valid supplimentary character
                            // range 0x000000 - 0x10FFFF at the same time
                            if (ch > 0x04) {
                                ch |= 0xf0;
                                goto InvalidByteSequence;
                            }

                            ch |= (FinalByte >> 3*6) | (1 << 30) | (3 << (30-2*6)) |
                                (SupplimentarySeq) | (SupplimentarySeq >> 6) |
                                (SupplimentarySeq >> 2*6) | (SupplimentarySeq >> 3*6);
                        }
                        else {
                            // 3 byte encoding
                            ch = (ch & 0x0F) | ( (FinalByte >> 2*6) | (1 << 30) |
                                (ThreeByteSeq) | (ThreeByteSeq >> 6) | (ThreeByteSeq >> 2*6) );
                        }
                    }
                    else {
                        // 2 byte encoding

                        ch &= 0x1F;

                        // check for non-shortest form
                        if (ch <= 1) {
                            ch |= 0xc0;
                            goto InvalidByteSequence;
                        }

                        ch |= (FinalByte >> 6);
                    }
                    continue;
                }

            EncodeChar:
                // write the pending character
                if (pTarget >= pAllocatedBufferEnd)
                {
                    // Fix chars so we make sure to throw if we didn't output anything
                    ch &= 0x1fffff;
                    if (ch > 0x7f)
                    {
                        if (ch > 0x7ff)
                        {
                            if (ch >= CharUnicodeInfo.LOW_SURROGATE_START &&
                                ch <= CharUnicodeInfo.LOW_SURROGATE_END)
                            {
                                pSrc--;     // It was 4 bytes
                                pTarget--;  // 1 was stored already, but we can't remember 1/2, so back up
                            }
                            else if (ch > 0xffff)
                            {
                                pSrc--;     // It was 4 bytes, nothing was stored
                            }
                            pSrc--;         // It was at least 3 bytes
                        }
                        pSrc--;             // It was at least 2 bytes
                    }
                    pSrc--;

                    // Throw that we don't have enough room (pSrc could be < chars if we had started to process
                    // a 4 byte sequence alredy)
                    Contract.Assert(pSrc >= bytes || pTarget == chars,
                        "[UTF8Encoding.GetChars]Expected pSrc to be within input buffer or throw due to no output]");
                    ThrowCharsOverflow(baseDecoder, pTarget == chars);

                    // Don't store ch in decoder, we already backed up to its start
                    ch = 0;

                    // Didn't throw, just use this buffer size.
                    break;
                }
                *pTarget = (char)ch;
                pTarget++;

#if FASTLOOP
                int availableChars = PtrDiff(pAllocatedBufferEnd, pTarget);
                int availableBytes = PtrDiff(pEnd, pSrc);

                // don't fall into the fast decoding loop if we don't have enough bytes
                // Test for availableChars is done because pStop would be <= pTarget.
                if (availableBytes <= 13) {
                    // we may need as many as 1 character per byte
                    if (availableChars < availableBytes) {
                        // not enough output room.  no pending bits at this point
                        ch = 0;
                        continue;
                    }

                    // try to get over the remainder of the ascii characters fast though
                    byte* pLocalEnd = pEnd; // hint to get pLocalEnd enregistered
                    while (pSrc < pLocalEnd) {
                        ch = *pSrc;
                        pSrc++;

                        if (ch > 0x7F)
                            goto ProcessChar;

                        *pTarget = (char)ch;
                        pTarget++;
                    }
                    // we are done
                    ch = 0;
                    break;
                }

                // we may need as many as 1 character per byte, so reduce the byte count if necessary.
                // If availableChars is too small, pStop will be before pTarget and we won't do fast loop.
                if (availableChars < availableBytes) {
                    availableBytes = availableChars;
                }

                // To compute the upper bound, assume that all characters are ASCII characters at this point,
                //  the boundary will be decreased for every non-ASCII character we encounter
                // Also, we need 7 chars reserve for the unrolled ansi decoding loop and for decoding of multibyte sequences
                char *pStop = pTarget + availableBytes - 7;

                while (pTarget < pStop) {
                    ch = *pSrc;
                    pSrc++;

                    if (ch > 0x7F) {
                        goto LongCode;
                    }
                    *pTarget = (char)ch;
                    pTarget++;

                    // get pSrc to be 2-byte aligned
                    if ((unchecked((int)pSrc) & 0x1) != 0) {
                        ch = *pSrc;
                        pSrc++;
                        if (ch > 0x7F) {
                            goto LongCode;
                        }
                        *pTarget = (char)ch;
                        pTarget++;
                    }

                    // get pSrc to be 4-byte aligned
                    if ((unchecked((int)pSrc) & 0x2) != 0) {
                        ch = *(ushort*)pSrc;
                        if ((ch & 0x8080) != 0) {
                            goto LongCodeWithMask16;
                        }

                        // Unfortunately, this is endianess sensitive
#if BIGENDIAN
                        *pTarget = (char)((ch >> 8) & 0x7F);
                        pSrc += 2;
                        *(pTarget+1) = (char)(ch & 0x7F);
                        pTarget += 2;
#else // BIGENDIAN
                        *pTarget = (char)(ch & 0x7F);
                        pSrc += 2;
                        *(pTarget+1) = (char)((ch >> 8) & 0x7F);
                        pTarget += 2;
#endif // BIGENDIAN
                    }

                    // Run 8 characters at a time!
                    while (pTarget < pStop) {
                        ch = *(int*)pSrc;
                        int chb = *(int*)(pSrc+4);
                        if (((ch | chb) & unchecked((int)0x80808080)) != 0) {
                            goto LongCodeWithMask32;
                        }

                        // Unfortunately, this is endianess sensitive
#if BIGENDIAN
                        *pTarget = (char)((ch >> 24) & 0x7F);
                        *(pTarget+1) = (char)((ch >> 16) & 0x7F);
                        *(pTarget+2) = (char)((ch >> 8) & 0x7F);
                        *(pTarget+3) = (char)(ch & 0x7F);
                        pSrc += 8;
                        *(pTarget+4) = (char)((chb >> 24) & 0x7F);
                        *(pTarget+5) = (char)((chb >> 16) & 0x7F);
                        *(pTarget+6) = (char)((chb >> 8) & 0x7F);
                        *(pTarget+7) = (char)(chb & 0x7F);
                        pTarget += 8;
#else // BIGENDIAN
                        *pTarget = (char)(ch & 0x7F);
                        *(pTarget+1) = (char)((ch >> 8) & 0x7F);
                        *(pTarget+2) = (char)((ch >> 16) & 0x7F);
                        *(pTarget+3) = (char)((ch >> 24) & 0x7F);
                        pSrc += 8;
                        *(pTarget+4) = (char)(chb & 0x7F);
                        *(pTarget+5) = (char)((chb >> 8) & 0x7F);
                        *(pTarget+6) = (char)((chb >> 16) & 0x7F);
                        *(pTarget+7) = (char)((chb >> 24) & 0x7F);
                        pTarget += 8;
#endif // BIGENDIAN
                    }
                    break;

#if BIGENDIAN
                LongCodeWithMask32:
                    // be careful about the sign extension
                    ch = (int)(((uint)ch) >> 16);
                LongCodeWithMask16:
                    ch = (int)(((uint)ch) >> 8);
#else // BIGENDIAN
                LongCodeWithMask32:
                LongCodeWithMask16:
                    ch &= 0xFF;
#endif // BIGENDIAN
                    pSrc++;
                    if (ch <= 0x7F) {
                        *pTarget = (char)ch;
                        pTarget++;
                        continue;
                    }

                LongCode:
                    int chc = *pSrc;
                    pSrc++;

                    if (
                        // bit 6 has to be zero
                        (ch & 0x40) == 0 ||
                        // we are expecting to see trailing bytes like 10vvvvvv
                        (chc & unchecked((sbyte)0xC0)) != 0x80)
                    {
                        goto BadLongCode;
                    }

                    chc &= 0x3F;

                    // start a new long code
                    if ((ch & 0x20) != 0) {

                        // fold the first two bytes together
                        chc |= (ch & 0x0F) << 6;

                        if ((ch & 0x10) != 0) {
                            // 4 byte encoding - surrogate
                            ch = *pSrc;
                            if (
                                // check that bit 4 is zero, the non-shortest form of surrogate
                                // and the valid surrogate range 0x000000 - 0x10FFFF at the same time
                                !InRange(chc >> 4, 0x01, 0x10) ||
                                // we are expecting to see trailing bytes like 10vvvvvv
                                (ch & unchecked((sbyte)0xC0)) != 0x80 )
                            {
                                goto BadLongCode;
                            }

                            chc = (chc << 6) | (ch & 0x3F);

                            ch = *(pSrc+1);
                            // we are expecting to see trailing bytes like 10vvvvvv
                            if ((ch & unchecked((sbyte)0xC0)) != 0x80) {
                                goto BadLongCode;
                            }
                            pSrc += 2;

                            ch = (chc << 6) | (ch & 0x3F);

                            *pTarget = (char)( ((ch >> 10) & 0x7FF) +
                                unchecked((short)(CharUnicodeInfo.HIGH_SURROGATE_START - (0x10000 >> 10))) );
                            pTarget++;

                            ch = (ch & 0x3FF) +
                                unchecked((short)(CharUnicodeInfo.LOW_SURROGATE_START));

                            // extra byte, we're already planning 2 chars for 2 of these bytes,
                            // but the big loop is testing the target against pStop, so we need
                            // to subtract 2 more or we risk overrunning the input.  Subtract 
                            // one here and one below.
                            pStop--;
                        }
                        else {
                            // 3 byte encoding
                            ch = *pSrc;
                            if (
                                // check for non-shortest form of 3 byte seq
                                (chc & (0x1F << 5)) == 0 ||
                                // Can't have surrogates here.
                                (chc & (0xF800 >> 6) ) == (0xD800 >> 6) ||
                                // we are expecting to see trailing bytes like 10vvvvvv
                                (ch & unchecked((sbyte)0xC0)) != 0x80 )
                            {
                                goto BadLongCode;
                            }
                            pSrc++;

                            ch = (chc << 6) | (ch & 0x3F);

                            // extra byte, we're only expecting 1 char for each of these 3 bytes,
                            // but the loop is testing the target (not source) against pStop, so
                            // we need to subtract 2 more or we risk overrunning the input.
                            // Subtract 1 here and one more below
                            pStop--;
                        }
                    }
                    else {
                        // 2 byte encoding

                        ch &= 0x1F;

                        // check for non-shortest form
                        if (ch <= 1) {
                            goto BadLongCode;
                        }
                        ch = (ch << 6) | chc;
                    }

                    *pTarget = (char)ch;
                    pTarget++;

                    // extra byte, we're only expecting 1 char for each of these 2 bytes,
                    // but the loop is testing the target (not source) against pStop.
                    // subtract an extra count from pStop so that we don't overrun the input.
                    pStop--;
                }
#endif // FASTLOOP

                Contract.Assert(pTarget <= pAllocatedBufferEnd, "[UTF8Encoding.GetChars]pTarget <= pAllocatedBufferEnd");

                // no pending bits at this point
                ch = 0;
                continue;

            BadLongCode:
                pSrc -= 2;
                ch = 0;
                continue;
            }

            if (ch != 0 && (baseDecoder == null || baseDecoder.MustFlush))
            {
                // Have to do fallback for invalid bytes
                if (fallback == null)
                {
                    if (baseDecoder == null)
                        fallback = this.decoderFallback.CreateFallbackBuffer();
                    else
                        fallback = baseDecoder.FallbackBuffer;
                    fallback.InternalInitialize(bytes, pAllocatedBufferEnd);
                }

                // This'll back us up the appropriate # of bytes if we didn't get anywhere
                if (!FallbackInvalidByteSequence(ref pSrc, ch, fallback, ref pTarget))
                {
                    Contract.Assert(pSrc >= bytes || pTarget == chars,
                        "[UTF8Encoding.GetChars]Expected to throw or remain in byte buffer while flushing");

                    // Ran out of buffer space
                    // Need to throw an exception?
                    fallback.InternalReset();
                    ThrowCharsOverflow(baseDecoder, pTarget == chars);
                }
                Contract.Assert(pSrc >= bytes, 
                    "[UTF8Encoding.GetChars]Expected flushing invalid byte sequence to have remained within the byte array");                
                ch = 0;
            }

            if (baseDecoder != null)
            {
                UTF8Decoder decoder = (UTF8Decoder)baseDecoder;

                // If we're storing flush data we expect all bits to be used or else
                // we're stuck in the middle of a conversion
                Contract.Assert(!baseDecoder.MustFlush || ch == 0 || !baseDecoder.m_throwOnOverflow,
                    "[UTF8Encoding.GetChars]Expected no must flush or no left over bits or no throw on overflow.");

                // Remember our leftover bits.
                decoder.bits = ch;

                baseDecoder.m_bytesUsed = (int)(pSrc - bytes);
            }

            // Shouldn't have anything in fallback buffer for GetChars
            // (don't have to check m_throwOnOverflow for chars)
            Contract.Assert(fallback == null || fallback.Remaining == 0,
                "[UTF8Encoding.GetChars]Expected empty fallback buffer at end");

            return PtrDiff(pTarget, chars);
        }

        // During GetChars we had an invalid byte sequence
        // pSrc is backed up to the start of the bad sequence if we didn't have room to
        // fall it back.  Otherwise pSrc remains wher it is.
        [System.Security.SecurityCritical]  // auto-generated
        private unsafe bool FallbackInvalidByteSequence(
            ref byte* pSrc, int ch, DecoderFallbackBuffer fallback, ref char* pTarget)
        {
            // Get our byte[]
            byte *pStart = pSrc;
            byte[] bytesUnknown = GetBytesUnknown(ref pStart, ch);

            // Do the actual fallback
            if (!fallback.InternalFallback(bytesUnknown, pSrc, ref pTarget))
            {
                // Oops, it failed, back up to pStart
                pSrc = pStart;
                return false;
            }

            // It worked
            return true;
        }

        // During GetCharCount we had an invalid byte sequence
        // pSrc is used to find the index that points to the invalid bytes,
        // however the byte[] contains the fallback bytes (in case the index is -1)
        [System.Security.SecurityCritical]  // auto-generated
        private unsafe int FallbackInvalidByteSequence(
            byte* pSrc, int ch, DecoderFallbackBuffer fallback)
        {
            // Get our byte[]
            byte[] bytesUnknown = GetBytesUnknown(ref pSrc, ch);

            // Do the actual fallback
            int count = fallback.InternalFallback(bytesUnknown, pSrc);

            // # of fallback chars expected.
            // Note that we only get here for "long" sequences, and have already unreserved
            // the count that we prereserved for the input bytes
            return count;
        }

        // Note that some of these bytes may have come from a previous fallback, so we cannot
        // just decrement the pointer and use the values we read.  In those cases we have 
        // to regenerate the original values.
        [System.Security.SecurityCritical]  // auto-generated
        private unsafe byte[] GetBytesUnknown(ref byte* pSrc, int ch)
        {
            // Get our byte[]
            byte[] bytesUnknown = null;

            // See if it was a plain char
            // (have to check >= 0 because we have all sorts of wierd bit flags)
            if (ch < 0x100 && ch >= 0)
            {
                pSrc--;
                bytesUnknown = new byte[] { unchecked((byte)ch) };
            }
            // See if its an unfinished 2 byte sequence
            else if ((ch & (SupplimentarySeq | ThreeByteSeq)) == 0)
            {
                pSrc--;
                bytesUnknown = new byte[] { unchecked((byte)((ch & 0x1F )| 0xc0)) };
            }
            // So now we're either 2nd byte of 3 or 4 byte sequence or
            // we hit a non-trail byte or we ran out of space for 3rd byte of 4 byte sequence
            // 1st check if its a 4 byte sequence
            else if ((ch & SupplimentarySeq) != 0)
            {
                //  3rd byte of 4 byte sequence?
                if ((ch & (FinalByte >> 6)) != 0)
                {
                    // 3rd byte of 4 byte sequence
                    pSrc-=3;
                    bytesUnknown = new byte[] {
                        unchecked((byte)(((ch >> 12) & 0x07) | 0xF0)),
                        unchecked((byte)(((ch >> 6) & 0x3F) | 0x80)),
                        unchecked((byte)(((ch) & 0x3F) | 0x80)) };
                }
                else if ((ch & (FinalByte >> 12)) != 0)
                {
                    // 2nd byte of a 4 byte sequence
                    pSrc-=2;
                    bytesUnknown = new byte[] {
                        unchecked((byte)(((ch >> 6) & 0x07) | 0xF0)),
                        unchecked((byte)(((ch) & 0x3F) | 0x80)) };
                }
                else
                {
                    // 4th byte of a 4 byte sequence
                    pSrc--;
                    bytesUnknown = new byte[] { unchecked((byte)(((ch) & 0x07) | 0xF0))};
                }
            }
            else
            {
                // 2nd byte of 3 byte sequence?
                if ((ch & (FinalByte >> 6)) != 0)
                {
                    // So its 2nd byte of a 3 byte sequence
                    pSrc-=2;
                    bytesUnknown = new byte[] {
                        unchecked((byte)(((ch >> 6) & 0x0F) | 0xE0)), unchecked ((byte)(((ch) & 0x3F) | 0x80)) };
                }
                else
                {
                    // 1st byte of a 3 byte sequence
                    pSrc--;
                    bytesUnknown = new byte[] { unchecked((byte)(((ch) & 0x0F) | 0xE0))};
                }
            }

            return bytesUnknown;
        }


        public override Decoder GetDecoder() {
            return new UTF8Decoder(this);
        }


        public override Encoder GetEncoder() {
            return new UTF8Encoder(this);
        }


        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
               throw new ArgumentOutOfRangeException("charCount",
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            // Characters would be # of characters + 1 in case left over high surrogate is ? * max fallback
            long byteCount = (long)charCount + 1;

            if (EncoderFallback.MaxCharCount > 1)
                byteCount *= EncoderFallback.MaxCharCount;

            // Max 3 bytes per char.  (4 bytes per 2 chars for surrogates)
            byteCount *= 3;

            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException("charCount", Environment.GetResourceString("ArgumentOutOfRange_GetByteCountOverflow"));

            return (int)byteCount;
        }


        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
               throw new ArgumentOutOfRangeException("byteCount",
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            // Figure out our length, 1 char per input byte + 1 char if 1st byte is last byte of 4 byte surrogate pair
            long charCount = ((long)byteCount + 1);

            // Non-shortest form would fall back, so get max count from fallback.
            // So would 11... followed by 11..., so you could fall back every byte
            if (DecoderFallback.MaxCharCount > 1)
            {
                charCount *= DecoderFallback.MaxCharCount;
            }

            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException("byteCount", Environment.GetResourceString("ArgumentOutOfRange_GetCharCountOverflow"));

            return (int)charCount;
        }


        public override byte[] GetPreamble()
        {
            if (emitUTF8Identifier) {
                // Allocate new array to prevent users from modifying it.
                return new byte[3] { 0xEF, 0xBB, 0xBF };
            }
            else
                return EmptyArray<Byte>.Value;
        }


        public override bool Equals(Object value) {
            UTF8Encoding that = value as UTF8Encoding;
            if (that != null) {
                return (emitUTF8Identifier == that.emitUTF8Identifier) &&
//                       (isThrowException == that.isThrowException) && // Same as encoder/decoderfallbacks being exception
                       (EncoderFallback.Equals(that.EncoderFallback)) &&
                       (DecoderFallback.Equals(that.DecoderFallback));
            }
            return (false);
        }


        public override int GetHashCode() {
            //Not great distribution, but this is relatively unlikely to be used as the key in a hashtable.
            return this.EncoderFallback.GetHashCode() + this.DecoderFallback.GetHashCode() +
                   UTF8_CODEPAGE + (emitUTF8Identifier?1:0);
        }

        [Serializable]
        internal class UTF8Encoder : EncoderNLS, ISerializable
        {
            // We must save a high surrogate value until the next call, looking
            // for a low surrogate value.
            internal int surrogateChar;

            public UTF8Encoder(UTF8Encoding encoding) : base(encoding)
            {
                // base calls reset
            }

            // Constructor called by serialization, have to handle deserializing from Everett
            internal UTF8Encoder(SerializationInfo info, StreamingContext context)
            {
                // Any info?
                if (info==null) throw new ArgumentNullException("info");
                Contract.EndContractBlock();

                // Get common info
                this.m_encoding = (Encoding)info.GetValue("encoding", typeof(Encoding));

                // SurrogateChar happens to mean the same thing
                this.surrogateChar = (int)info.GetValue("surrogateChar", typeof(int));

                try 
                {
                    this.m_fallback = (EncoderFallback) info.GetValue("m_fallback", typeof(EncoderFallback));
                } 
                catch (SerializationException)
                {
                    this.m_fallback = null;
                }
            }

            // ISerializable implementation, get data for this object
            [System.Security.SecurityCritical]  // auto-generated_required
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                // Any info?
                if (info==null) throw new ArgumentNullException("info");
                Contract.EndContractBlock();

                // Save Whidbey data
                // Just need Everett maxCharSize (BaseCodePageEncoding) or m_maxByteSize (MLangBaseCodePageEncoding)
                info.AddValue("encoding", this.m_encoding);
                info.AddValue("surrogateChar", this.surrogateChar);

                info.AddValue("m_fallback", this.m_fallback);

                // Extra stuff for Everett that Whidbey doesn't use
                info.AddValue("storedSurrogate", this.surrogateChar > 0 ? true : false);
                info.AddValue("mustFlush", false);  // Everett doesn't actually use this either, but it accidently serialized it!
            }

            public override void Reset()

            {
                this.surrogateChar = 0;
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();                
            }

            // Anything left in our encoder?
            internal override bool HasState
            {
                get
                {
                    return (this.surrogateChar != 0);
                }
            }
        }

        [Serializable]
        internal class UTF8Decoder : DecoderNLS, ISerializable
        {
            // We'll need to remember the previous information. See the comments around definition
            // of FinalByte for details.
            internal int bits;

            public UTF8Decoder(UTF8Encoding encoding) : base(encoding)
            {
                // base calls reset
            }

            // Constructor called by serialization, have to handle deserializing from Everett
            internal UTF8Decoder(SerializationInfo info, StreamingContext context)
            {
                // Any info?
                if (info==null) throw new ArgumentNullException("info");
                Contract.EndContractBlock();

                // Get common info
                this.m_encoding = (Encoding)info.GetValue("encoding", typeof(Encoding));

                try
                {
                    // Get whidbey version of bits
                    this.bits = (int)info.GetValue("wbits", typeof(int));
                    this.m_fallback = (DecoderFallback) info.GetValue("m_fallback", typeof(DecoderFallback));
                }
                catch (SerializationException)
                {
                    // Everett calls bits bits instead of wbits, so this is Everett
                    this.bits = 0;
                    this.m_fallback = null;
                }
            }

            // ISerializable implementation, get data for this object
            [System.Security.SecurityCritical]  // auto-generated_required
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                // Any info?
                if (info==null) throw new ArgumentNullException("info");
                Contract.EndContractBlock();

                // Save new Whidbey data
                info.AddValue("encoding", this.m_encoding);
                info.AddValue("wbits", this.bits);          // Special whidbey bits name
                info.AddValue("m_fallback", this.m_fallback);  

                // Everett has extra stuff, we set it all to 0 in case this deserializes in Everett
                info.AddValue("bits", (int)0);
                info.AddValue("trailCount", (int)0);
                info.AddValue("isSurrogate", false);
                info.AddValue("byteSequence", (int)0);
            }

            public override void Reset()
            {
                this.bits = 0;
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();
            }

            // Anything left in our decoder?
            internal override bool HasState
            {
                get
                {
                    return (this.bits != 0);
                }
            }
        }
    }
}

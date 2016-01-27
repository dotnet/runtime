// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text
{
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;

    // ASCIIEncoding
    //
    // Note that ASCIIEncoding is optomized with no best fit and ? for fallback.
    // It doesn't come in other flavors.
    //
    // Note: ASCIIEncoding is the only encoding that doesn't do best fit (windows has best fit).
    //
    // Note: IsAlwaysNormalized remains false because 1/2 the code points are unassigned, so they'd
    //       use fallbacks, and we cannot guarantee that fallbacks are normalized.
    //

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public class ASCIIEncoding : Encoding
    {

        public ASCIIEncoding() : base(Encoding.CodePageASCII)
        {
        }

        internal override void SetDefaultFallbacks()
        {
            // For ASCIIEncoding we just use default replacement fallback
            this.encoderFallback = EncoderFallback.ReplacementFallback;
            this.decoderFallback = DecoderFallback.ReplacementFallback;
        }

        //
        // WARNING: GetByteCount(string chars), GetBytes(string chars,...), and GetString(byte[] byteIndex...)
        // WARNING: have different variable names than EncodingNLS.cs, so this can't just be cut & pasted,
        // WARNING: or it'll break VB's way of calling these.
        //
        // The following methods are copied from EncodingNLS.cs.
        // Unfortunately EncodingNLS.cs is internal and we're public, so we have to reimpliment them here.
        // These should be kept in sync for the following classes:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        //

        // Returns the number of bytes required to encode a range of characters in
        // a character array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override unsafe int GetByteCount(char[] chars, int index, int count)
        {
            // Validate input parameters
            if (chars == null)
                throw new ArgumentNullException("chars",
                      Environment.GetResourceString("ArgumentNull_Array"));

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index<0 ? "index" : "count"),
                      Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));

            if (chars.Length - index < count)
                throw new ArgumentOutOfRangeException("chars",
                      Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            Contract.EndContractBlock();

            // If no input, return 0, avoid fixed empty array problem
            if (chars.Length == 0)
                return 0;

            // Just call the pointer version
            fixed (char* pChars = chars)
                return GetByteCount(pChars + index, count, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override unsafe int GetByteCount(String chars)
        {
            // Validate input
            if (chars==null)
                throw new ArgumentNullException("chars");
            Contract.EndContractBlock();

            fixed (char* pChars = chars)
                return GetByteCount(pChars, chars.Length, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        [System.Runtime.InteropServices.ComVisible(false)]
        public override unsafe int GetByteCount(char* chars, int count)
        {
            // Validate Parameters
            if (chars == null)
                throw new ArgumentNullException("chars",
                    Environment.GetResourceString("ArgumentNull_Array"));

            if (count < 0)
                throw new ArgumentOutOfRangeException("count",
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            // Call it with empty encoder
            return GetByteCount(chars, count, null);
        }

        // Parent method is safe.
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override unsafe int GetBytes(String chars, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex)
        {
            if (chars == null || bytes == null)
                throw new ArgumentNullException((chars == null ? "chars" : "bytes"),
                      Environment.GetResourceString("ArgumentNull_Array"));

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex<0 ? "charIndex" : "charCount"),
                      Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException("chars",
                      Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException("byteIndex",
                    Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();

            int byteCount = bytes.Length - byteIndex;

            // Fixed doesn't like empty byte arrays
            if (bytes.Length == 0)
                bytes = new byte[1];

            fixed (char* pChars = chars)
                fixed ( byte* pBytes = bytes)
                    return GetBytes(pChars + charIndex, charCount,
                                    pBytes + byteIndex, byteCount, null);
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

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override unsafe int GetBytes(char[] chars, int charIndex, int charCount,
                                               byte[] bytes, int byteIndex)
        {
            // Validate parameters
            if (chars == null || bytes == null)
                throw new ArgumentNullException((chars == null ? "chars" : "bytes"),
                      Environment.GetResourceString("ArgumentNull_Array"));

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex<0 ? "charIndex" : "charCount"),
                      Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException("chars",
                      Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException("byteIndex",
                     Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();

            // If nothing to encode return 0, avoid fixed problem
            if (chars.Length == 0)
                return 0;

            // Just call pointer version
            int byteCount = bytes.Length - byteIndex;

            // Fixed doesn't like empty byte arrays
            if (bytes.Length == 0)
                bytes = new byte[1];

            fixed (char* pChars = chars)
                fixed (byte* pBytes = bytes)
                    // Remember that byteCount is # to decode, not size of array.
                    return GetBytes(pChars + charIndex, charCount,
                                    pBytes + byteIndex, byteCount, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        [System.Runtime.InteropServices.ComVisible(false)]
        public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
        {
            // Validate Parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? "bytes" : "chars",
                    Environment.GetResourceString("ArgumentNull_Array"));

            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount<0 ? "charCount" : "byteCount"),
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            return GetBytes(chars, charCount, bytes, byteCount, null);
        }

        // Returns the number of characters produced by decoding a range of bytes
        // in a byte array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override unsafe int GetCharCount(byte[] bytes, int index, int count)
        {
            // Validate Parameters
            if (bytes == null)
                throw new ArgumentNullException("bytes",
                    Environment.GetResourceString("ArgumentNull_Array"));

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index<0 ? "index" : "count"),
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));

            if (bytes.Length - index < count)
                throw new ArgumentOutOfRangeException("bytes",
                    Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            Contract.EndContractBlock();

            // If no input just return 0, fixed doesn't like 0 length arrays
            if (bytes.Length == 0)
                return 0;

            // Just call pointer version
            fixed (byte* pBytes = bytes)
                return GetCharCount(pBytes + index, count, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        [System.Runtime.InteropServices.ComVisible(false)]
        public override unsafe int GetCharCount(byte* bytes, int count)
        {
            // Validate Parameters
            if (bytes == null)
                throw new ArgumentNullException("bytes",
                    Environment.GetResourceString("ArgumentNull_Array"));

            if (count < 0)
                throw new ArgumentOutOfRangeException("count",
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            return GetCharCount(bytes, count, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                              char[] chars, int charIndex)
        {
            // Validate Parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? "bytes" : "chars",
                    Environment.GetResourceString("ArgumentNull_Array"));

            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex<0 ? "byteIndex" : "byteCount"),
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));

            if ( bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException("bytes",
                    Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));

            if (charIndex < 0 || charIndex > chars.Length)
                throw new ArgumentOutOfRangeException("charIndex",
                    Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();

            // If no input, return 0 & avoid fixed problem
            if (bytes.Length == 0)
                return 0;

            // Just call pointer version
            int charCount = chars.Length - charIndex;

            // Fixed doesn't like empty char arrays
            if (chars.Length == 0)
                chars = new char[1];

            fixed (byte* pBytes = bytes)
                fixed (char* pChars = chars)
                    // Remember that charCount is # to decode, not size of array
                    return GetChars(pBytes + byteIndex, byteCount,
                                    pChars + charIndex, charCount, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        [System.Runtime.InteropServices.ComVisible(false)]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        {
            // Validate Parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? "bytes" : "chars",
                    Environment.GetResourceString("ArgumentNull_Array"));

            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount<0 ? "charCount" : "byteCount"),
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            return GetChars(bytes, byteCount, chars, charCount, null);
        }

        // Returns a string containing the decoded representation of a range of
        // bytes in a byte array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override unsafe String GetString(byte[] bytes, int byteIndex, int byteCount)
        {
            // Validate Parameters
            if (bytes == null)
                throw new ArgumentNullException("bytes",
                    Environment.GetResourceString("ArgumentNull_Array"));

            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex < 0 ? "byteIndex" : "byteCount"),
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));


            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException("bytes",
                    Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            Contract.EndContractBlock();

            // Avoid problems with empty input buffer
            if (bytes.Length == 0) return String.Empty;

            fixed (byte* pBytes = bytes)
                return String.CreateStringFromEncoding(
                    pBytes + byteIndex, byteCount, this);
        }

        //
        // End of standard methods copied from EncodingNLS.cs
        //

        // GetByteCount
        // Note: We start by assuming that the output will be the same as count.  Having
        // an encoder or fallback may change that assumption
        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetByteCount(char* chars, int charCount, EncoderNLS encoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            Contract.Assert(charCount >= 0, "[ASCIIEncoding.GetByteCount]count is negative");
            Contract.Assert(chars != null, "[ASCIIEncoding.GetByteCount]chars is null");

            // Assert because we shouldn't be able to have a null encoder.
            Contract.Assert(encoderFallback != null, "[ASCIIEncoding.GetByteCount]Attempting to use null fallback encoder");

            char charLeftOver = (char)0;
            EncoderReplacementFallback fallback = null;

            // Start by assuming default count, then +/- for fallback characters
            char* charEnd = chars + charCount;

            // For fallback we may need a fallback buffer, we know we aren't default fallback.
            EncoderFallbackBuffer fallbackBuffer = null;

            if (encoder != null)
            {
                charLeftOver = encoder.charLeftOver;
                Contract.Assert(charLeftOver == 0 || Char.IsHighSurrogate(charLeftOver),
                    "[ASCIIEncoding.GetByteCount]leftover character should be high surrogate");

                fallback = encoder.Fallback as EncoderReplacementFallback;

                // We mustn't have left over fallback data when counting
                if (encoder.InternalHasFallbackBuffer)
                {
                    // We always need the fallback buffer in get bytes so we can flush any remaining ones if necessary
                    fallbackBuffer = encoder.FallbackBuffer;
                    if (fallbackBuffer.Remaining > 0 && encoder.m_throwOnOverflow)
                        throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty",
                        this.EncodingName, encoder.Fallback.GetType()));

                    // Set our internal fallback interesting things.
                    fallbackBuffer.InternalInitialize(chars, charEnd, encoder, false);
                }

                // Verify that we have no fallbackbuffer, for ASCII its always empty, so just assert
                Contract.Assert(!encoder.m_throwOnOverflow || !encoder.InternalHasFallbackBuffer ||
                    encoder.FallbackBuffer.Remaining == 0,
                    "[ASCIICodePageEncoding.GetByteCount]Expected empty fallback buffer");
//                if (encoder.InternalHasFallbackBuffer && encoder.FallbackBuffer.Remaining > 0)
//                    throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty",
//                    this.EncodingName, encoder.Fallback.GetType()));
            }
            else
            {
                fallback = this.EncoderFallback as EncoderReplacementFallback;
            }

            // If we have an encoder AND we aren't using default fallback,
            // then we may have a complicated count.
            if (fallback != null && fallback.MaxCharCount == 1)
            {
                // Replacement fallback encodes surrogate pairs as two ?? (or two whatever), so return size is always
                // same as input size.
                // Note that no existing SBCS code pages map code points to supplimentary characters, so this is easy.

                // We could however have 1 extra byte if the last call had an encoder and a funky fallback and
                // if we don't use the funky fallback this time.

                // Do we have an extra char left over from last time?
                if (charLeftOver > 0)
                    charCount++;

                return (charCount);
            }

            // Count is more complicated if you have a funky fallback
            // For fallback we may need a fallback buffer, we know we're not default fallback
            int byteCount = 0;

            // We may have a left over character from last time, try and process it.
            if (charLeftOver > 0)
            {
                Contract.Assert(Char.IsHighSurrogate(charLeftOver), "[ASCIIEncoding.GetByteCount]leftover character should be high surrogate");
                Contract.Assert(encoder != null, "[ASCIIEncoding.GetByteCount]Expected encoder");

                // Since left over char was a surrogate, it'll have to be fallen back.
                // Get Fallback
                fallbackBuffer = encoder.FallbackBuffer;
                fallbackBuffer.InternalInitialize(chars, charEnd, encoder, false);

                // This will fallback a pair if *chars is a low surrogate
                fallbackBuffer.InternalFallback(charLeftOver, ref chars);
            }

            // Now we may have fallback char[] already from the encoder

            // Go ahead and do it, including the fallback.
            char ch;
            while ((ch = (fallbackBuffer == null) ? '\0' : fallbackBuffer.InternalGetNextChar()) != 0 ||
                    chars < charEnd)
            {

                // First unwind any fallback
                if (ch == 0)
                {
                    // No fallback, just get next char
                    ch = *chars;
                    chars++;
                }

                // Check for fallback, this'll catch surrogate pairs too.
                // no chars >= 0x80 are allowed.
                if (ch > 0x7f)
                {
                    if (fallbackBuffer == null)
                    {
                        // Initialize the buffer
                        if (encoder == null)
                            fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = encoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(charEnd - charCount, charEnd, encoder, false);
                    }

                    // Get Fallback
                    fallbackBuffer.InternalFallback(ch, ref chars);
                    continue;
                }

                // We'll use this one
                byteCount++;
            }

            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[ASCIIEncoding.GetByteCount]Expected Empty fallback buffer");

            return byteCount;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetBytes(char* chars, int charCount,
                                                byte* bytes, int byteCount, EncoderNLS encoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            Contract.Assert(bytes != null, "[ASCIIEncoding.GetBytes]bytes is null");
            Contract.Assert(byteCount >= 0, "[ASCIIEncoding.GetBytes]byteCount is negative");
            Contract.Assert(chars != null, "[ASCIIEncoding.GetBytes]chars is null");
            Contract.Assert(charCount >= 0, "[ASCIIEncoding.GetBytes]charCount is negative");

            // Assert because we shouldn't be able to have a null encoder.
            Contract.Assert(encoderFallback != null, "[ASCIIEncoding.GetBytes]Attempting to use null encoder fallback");

            // Get any left over characters
            char charLeftOver = (char)0;
            EncoderReplacementFallback fallback = null;

            // For fallback we may need a fallback buffer, we know we aren't default fallback.
            EncoderFallbackBuffer fallbackBuffer = null;

            // prepare our end
            char* charEnd = chars + charCount;
            byte* byteStart = bytes;
            char* charStart = chars;

            if (encoder != null)
            {
                charLeftOver = encoder.charLeftOver;
                fallback = encoder.Fallback as EncoderReplacementFallback;

                // We mustn't have left over fallback data when counting
                if (encoder.InternalHasFallbackBuffer)
                {
                    // We always need the fallback buffer in get bytes so we can flush any remaining ones if necessary
                    fallbackBuffer = encoder.FallbackBuffer;
                    if (fallbackBuffer.Remaining > 0 && encoder.m_throwOnOverflow)
                        throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty",
                        this.EncodingName, encoder.Fallback.GetType()));

                    // Set our internal fallback interesting things.
                    fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, true);
                }

                Contract.Assert(charLeftOver == 0 || Char.IsHighSurrogate(charLeftOver),
                    "[ASCIIEncoding.GetBytes]leftover character should be high surrogate");

                // Verify that we have no fallbackbuffer, for ASCII its always empty, so just assert
                Contract.Assert(!encoder.m_throwOnOverflow || !encoder.InternalHasFallbackBuffer ||
                    encoder.FallbackBuffer.Remaining == 0,
                    "[ASCIICodePageEncoding.GetBytes]Expected empty fallback buffer");
//                if (encoder.m_throwOnOverflow && encoder.InternalHasFallbackBuffer &&
//                  encoder.FallbackBuffer.Remaining > 0)
//                  throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty",
//                        this.EncodingName, encoder.Fallback.GetType()));
            }
            else
            {
                fallback = this.EncoderFallback as EncoderReplacementFallback;
            }


            // See if we do the fast default or slightly slower fallback
            if (fallback != null && fallback.MaxCharCount == 1)
            {
                // Fast version
                char cReplacement = fallback.DefaultString[0];

                // Check for replacements in range, otherwise fall back to slow version.
                if (cReplacement <= (char)0x7f)
                {
                    // We should have exactly as many output bytes as input bytes, unless there's a left
                    // over character, in which case we may need one more.
                    // If we had a left over character will have to add a ?  (This happens if they had a funky
                    // fallback last time, but not this time.) (We can't spit any out though
                    // because with fallback encoder each surrogate is treated as a seperate code point)
                    if (charLeftOver > 0)
                    {
                        // Have to have room
                        // Throw even if doing no throw version because this is just 1 char,
                        // so buffer will never be big enough
                        if (byteCount == 0)
                            ThrowBytesOverflow(encoder, true);

                        // This'll make sure we still have more room and also make sure our return value is correct.
                        *(bytes++) = (byte)cReplacement;
                        byteCount--;                // We used one of the ones we were counting.
                    }

                    // This keeps us from overrunning our output buffer
                    if (byteCount < charCount)
                    {
                        // Throw or make buffer smaller?
                        ThrowBytesOverflow(encoder, byteCount < 1);

                        // Just use what we can
                        charEnd = chars + byteCount;
                    }

                    // We just do a quick copy
                    while (chars < charEnd)
                    {
                        char ch2 = *(chars++);
                        if (ch2 >= 0x0080) *(bytes++) = (byte)cReplacement;
                        else *(bytes++) = unchecked((byte)(ch2));
                    }

                    // Clear encoder
                    if (encoder != null)
                    {
                        encoder.charLeftOver = (char)0;
                        encoder.m_charsUsed = (int)(chars-charStart);
                    }

                    return (int)(bytes - byteStart);
                }
            }

            // Slower version, have to do real fallback.

            // prepare our end
            byte* byteEnd = bytes + byteCount;

            // We may have a left over character from last time, try and process it.
            if (charLeftOver > 0)
            {
                // Initialize the buffer
                Contract.Assert(encoder != null,
                    "[ASCIIEncoding.GetBytes]Expected non null encoder if we have surrogate left over");
                fallbackBuffer = encoder.FallbackBuffer;
                fallbackBuffer.InternalInitialize(chars, charEnd, encoder, true);

                // Since left over char was a surrogate, it'll have to be fallen back.
                // Get Fallback
                // This will fallback a pair if *chars is a low surrogate
                fallbackBuffer.InternalFallback(charLeftOver, ref chars);
            }

            // Now we may have fallback char[] already from the encoder

            // Go ahead and do it, including the fallback.
            char ch;
            while ((ch = (fallbackBuffer == null) ? '\0' : fallbackBuffer.InternalGetNextChar()) != 0 ||
                    chars < charEnd)
            {
                // First unwind any fallback
                if (ch == 0)
                {
                    // No fallback, just get next char
                    ch = *chars;
                    chars++;
                }

                // Check for fallback, this'll catch surrogate pairs too.
                // All characters >= 0x80 must fall back.
                if (ch > 0x7f)
                {
                    // Initialize the buffer
                    if (fallbackBuffer == null)
                    {
                        if (encoder == null)
                            fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = encoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(charEnd - charCount, charEnd, encoder, true);
                    }

                    // Get Fallback
                    fallbackBuffer.InternalFallback(ch, ref chars);

                    // Go ahead & continue (& do the fallback)
                    continue;
                }

                // We'll use this one
                // Bounds check
                if (bytes >= byteEnd)
                {
                    // didn't use this char, we'll throw or use buffer
                    if (fallbackBuffer == null || fallbackBuffer.bFallingBack == false)
                    {
                        Contract.Assert(chars > charStart || bytes == byteStart,
                            "[ASCIIEncoding.GetBytes]Expected chars to have advanced already.");
                        chars--;                                        // don't use last char
                    }
                    else
                        fallbackBuffer.MovePrevious();

                    // Are we throwing or using buffer?
                    ThrowBytesOverflow(encoder, bytes == byteStart);    // throw?
                    break;                                              // don't throw, stop
                }

                // Go ahead and add it
                *bytes = unchecked((byte)ch);
                bytes++;
            }

            // Need to do encoder stuff
            if (encoder != null)
            {
                // Fallback stuck it in encoder if necessary, but we have to clear MustFlush cases
                if (fallbackBuffer != null && !fallbackBuffer.bUsedEncoder)
                    // Clear it in case of MustFlush
                    encoder.charLeftOver = (char)0;

                // Set our chars used count
                encoder.m_charsUsed = (int)(chars - charStart);
            }

            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0 ||
                (encoder != null && !encoder.m_throwOnOverflow ),
                "[ASCIIEncoding.GetBytes]Expected Empty fallback buffer at end");

            return (int)(bytes - byteStart);
        }

        // This is internal and called by something else,
        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetCharCount(byte* bytes, int count, DecoderNLS decoder)
        {
            // Just assert, we're called internally so these should be safe, checked already
            Contract.Assert(bytes != null, "[ASCIIEncoding.GetCharCount]bytes is null");
            Contract.Assert(count >= 0, "[ASCIIEncoding.GetCharCount]byteCount is negative");

            // ASCII doesn't do best fit, so don't have to check for it, find out which decoder fallback we're using
            DecoderReplacementFallback fallback = null;

            if (decoder == null)
                fallback = this.DecoderFallback as DecoderReplacementFallback;
            else
            {
                fallback = decoder.Fallback as DecoderReplacementFallback;
                Contract.Assert(!decoder.m_throwOnOverflow || !decoder.InternalHasFallbackBuffer ||
                    decoder.FallbackBuffer.Remaining == 0,
                    "[ASCIICodePageEncoding.GetCharCount]Expected empty fallback buffer");
            }

            if (fallback != null && fallback.MaxCharCount == 1)
            {
                // Just return length, SBCS stay the same length because they don't map to surrogate
                // pairs and we don't have a decoder fallback.

                return count;
            }

            // Only need decoder fallback buffer if not using default replacement fallback, no best fit for ASCII
            DecoderFallbackBuffer fallbackBuffer = null;

            // Have to do it the hard way.
            // Assume charCount will be == count
            int charCount = count;
            byte[] byteBuffer = new byte[1];

            // Do it our fast way
            byte* byteEnd = bytes + count;

            // Quick loop
            while (bytes < byteEnd)
            {
                // Faster if don't use *bytes++;
                byte b = *bytes;
                bytes++;

                // If unknown we have to do fallback count
                if (b >= 0x80)
                {
                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.DecoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(byteEnd - count, null);
                    }

                    // Use fallback buffer
                    byteBuffer[0] = b;
                    charCount--;            // Have to unreserve the one we already allocated for b
                    charCount += fallbackBuffer.InternalFallback(byteBuffer, bytes);
                }
            }

            // Fallback buffer must be empty
            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[ASCIIEncoding.GetCharCount]Expected Empty fallback buffer");

            // Converted sequence is same length as input
            return charCount;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetChars(byte* bytes, int byteCount,
                                                char* chars, int charCount, DecoderNLS decoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            Contract.Assert(bytes != null, "[ASCIIEncoding.GetChars]bytes is null");
            Contract.Assert(byteCount >= 0, "[ASCIIEncoding.GetChars]byteCount is negative");
            Contract.Assert(chars != null, "[ASCIIEncoding.GetChars]chars is null");
            Contract.Assert(charCount >= 0, "[ASCIIEncoding.GetChars]charCount is negative");

            // Do it fast way if using ? replacement fallback
            byte* byteEnd = bytes + byteCount;
            byte* byteStart = bytes;
            char* charStart = chars;

            // Note: ASCII doesn't do best fit, but we have to fallback if they use something > 0x7f
            // Only need decoder fallback buffer if not using ? fallback.
            // ASCII doesn't do best fit, so don't have to check for it, find out which decoder fallback we're using
            DecoderReplacementFallback fallback = null;

            if (decoder == null)
                fallback = this.DecoderFallback as DecoderReplacementFallback;
            else
            {
                fallback = decoder.Fallback as DecoderReplacementFallback;
                Contract.Assert(!decoder.m_throwOnOverflow || !decoder.InternalHasFallbackBuffer ||
                    decoder.FallbackBuffer.Remaining == 0,
                    "[ASCIICodePageEncoding.GetChars]Expected empty fallback buffer");
            }

            if (fallback != null && fallback.MaxCharCount == 1)
            {
                // Try it the fast way
                char replacementChar = fallback.DefaultString[0];

                // Need byteCount chars, otherwise too small buffer
                if (charCount < byteCount)
                {
                    // Need at least 1 output byte, throw if must throw
                    ThrowCharsOverflow(decoder, charCount < 1);

                    // Not throwing, use what we can
                    byteEnd = bytes + charCount;
                }

                // Quick loop, just do '?' replacement because we don't have fallbacks for decodings.
                while (bytes < byteEnd)
                {
                    byte b = *(bytes++);
                    if (b >= 0x80)
                        // This is an invalid byte in the ASCII encoding.
                        *(chars++) = replacementChar;
                    else
                        *(chars++) = unchecked((char)b);
                }

                // bytes & chars used are the same
                if (decoder != null)
                    decoder.m_bytesUsed = (int)(bytes - byteStart);
                return (int)(chars - charStart);
            }

            // Slower way's going to need a fallback buffer
            DecoderFallbackBuffer fallbackBuffer = null;
            byte[] byteBuffer = new byte[1];
            char*   charEnd = chars + charCount;

            // Not quite so fast loop
            while (bytes < byteEnd)
            {
                // Faster if don't use *bytes++;
                byte b = *(bytes);
                bytes++;

                if (b >= 0x80)
                {
                    // This is an invalid byte in the ASCII encoding.
                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.DecoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(byteEnd - byteCount, charEnd);
                    }

                    // Use fallback buffer
                    byteBuffer[0] = b;

                    // Note that chars won't get updated unless this succeeds
                    if (!fallbackBuffer.InternalFallback(byteBuffer, bytes, ref chars))
                    {
                        // May or may not throw, but we didn't get this byte
                        Contract.Assert(bytes > byteStart || chars == charStart,
                            "[ASCIIEncoding.GetChars]Expected bytes to have advanced already (fallback case)");
                        bytes--;                                            // unused byte
                        fallbackBuffer.InternalReset();                     // Didn't fall this back
                        ThrowCharsOverflow(decoder, chars == charStart);    // throw?
                        break;                                              // don't throw, but stop loop
                    }
                }
                else
                {
                    // Make sure we have buffer space
                    if (chars >= charEnd)
                    {
                        Contract.Assert(bytes > byteStart || chars == charStart,
                            "[ASCIIEncoding.GetChars]Expected bytes to have advanced already (normal case)");
                        bytes--;                                            // unused byte
                        ThrowCharsOverflow(decoder, chars == charStart);    // throw?
                        break;                                              // don't throw, but stop loop
                    }

                    *(chars) = unchecked((char)b);
                    chars++;
                }
            }

            // Might have had decoder fallback stuff.
            if (decoder != null)
                decoder.m_bytesUsed = (int)(bytes - byteStart);

            // Expect Empty fallback buffer for GetChars
            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[ASCIIEncoding.GetChars]Expected Empty fallback buffer");

            return (int)(chars - charStart);
        }


        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
               throw new ArgumentOutOfRangeException("charCount",
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            // Characters would be # of characters + 1 in case high surrogate is ? * max fallback
            long byteCount = (long)charCount + 1;

            if (EncoderFallback.MaxCharCount > 1)
                byteCount *= EncoderFallback.MaxCharCount;

            // 1 to 1 for most characters.  Only surrogates with fallbacks have less.

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

            // Just return length, SBCS stay the same length because they don't map to surrogate
            long charCount = (long)byteCount;

            // 1 to 1 for most characters.  Only surrogates with fallbacks have less, unknown fallbacks could be longer.
            if (DecoderFallback.MaxCharCount > 1)
                charCount *= DecoderFallback.MaxCharCount;

            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException("byteCount", Environment.GetResourceString("ArgumentOutOfRange_GetCharCountOverflow"));

            return (int)charCount;
        }

        // True if and only if the encoding only uses single byte code points.  (Ie, ASCII, 1252, etc)

        [System.Runtime.InteropServices.ComVisible(false)]
        public override bool IsSingleByte
        {
            get
            {
                return true;
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public override Decoder GetDecoder()
        {
            return new DecoderNLS(this);
        }


        [System.Runtime.InteropServices.ComVisible(false)]
        public override Encoder GetEncoder()
        {
            return new EncoderNLS(this);
        }
    }
}

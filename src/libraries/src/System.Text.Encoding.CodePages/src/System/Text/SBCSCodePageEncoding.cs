// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Security;

namespace System.Text
{
    [Serializable]
    internal class SBCSCodePageEncoding : BaseCodePageEncoding
    {
        // Pointers to our memory section parts
        [SecurityCritical]
        private unsafe char* _mapBytesToUnicode = null;      // char 256
        [SecurityCritical]
        private unsafe byte* _mapUnicodeToBytes = null;      // byte 65536

        private const char UNKNOWN_CHAR = (char)0xFFFD;

        // byteUnknown is used for default fallback only
        private byte _byteUnknown;
        private char _charUnknown;

        [System.Security.SecurityCritical]  // auto-generated
        public SBCSCodePageEncoding(int codePage) : this(codePage, codePage)
        {
        }

        [System.Security.SecurityCritical]  // auto-generated
        public SBCSCodePageEncoding(int codePage, int dataCodePage) : base(codePage, dataCodePage)
        {
        }

        // Method assumes that memory pointer is aligned
        private static unsafe void ZeroMemAligned(byte* buffer, int count)
        {
            long* pLong = (long*)buffer;
            long* pLongEnd = (long*)(buffer + count - sizeof(long));

            while (pLong < pLongEnd)
            {
                *pLong = 0;
                pLong++;
            }

            byte* pByte = (byte*)pLong;
            byte* pEnd = buffer + count;

            while (pByte < pEnd)
            {
                *pByte = 0;
                pByte++;
            }
        }

        // We have a managed code page entry, so load our tables
        // SBCS data section looks like:
        //
        // char[256]  - what each byte maps to in unicode.  No support for surrogates. 0 is undefined code point
        //              (except 0 for byte 0 is expected to be a real 0)
        //
        // byte/char* - Data for best fit (unicode->bytes), again no best fit for Unicode
        //              1st WORD is Unicode // of 1st character position
        //              Next bytes are best fit byte for that position.  Position is incremented after each byte
        //              byte < 0x20 means skip the next n positions.  (Where n is the byte #)
        //              byte == 1 means that next word is another unicode code point #
        //              byte == 0 is unknown.  (doesn't override initial WCHAR[256] table!
        [System.Security.SecurityCritical]  // auto-generated
        protected override unsafe void LoadManagedCodePage()
        {
            fixed (byte* pBytes = m_codePageHeader)
            {
                CodePageHeader* pCodePage = (CodePageHeader*)pBytes;
                // Should be loading OUR code page
                Debug.Assert(pCodePage->CodePage == dataTableCodePage,
                    "[SBCSCodePageEncoding.LoadManagedCodePage]Expected to load data table code page");

                // Make sure we're really a 1 byte code page
                if (pCodePage->ByteCount != 1)
                    throw new NotSupportedException(SR.Format(SR.NotSupported_NoCodepageData, CodePage));

                // Remember our unknown bytes & chars
                _byteUnknown = (byte)pCodePage->ByteReplace;
                _charUnknown = pCodePage->UnicodeReplace;

                // Get our mapped section 65536 bytes for unicode->bytes, 256 * 2 bytes for bytes->unicode
                // Plus 4 byte to remember CP # when done loading it. (Don't want to get IA64 or anything out of alignment)
                const int UnicodeToBytesMappingSize = 65536;
                const int BytesToUnicodeMappingSize = 256 * 2;
                const int CodePageNumberSize = 4;
                int bytesToAllocate = UnicodeToBytesMappingSize + BytesToUnicodeMappingSize + CodePageNumberSize + iExtraBytes;
                byte* pNativeMemory = GetNativeMemory(bytesToAllocate);
                ZeroMemAligned(pNativeMemory, bytesToAllocate);

                char* mapBytesToUnicode = (char*)pNativeMemory;
                byte* mapUnicodeToBytes = (byte*)(pNativeMemory + 256 * 2);

                // Need to read our data file and fill in our section.
                // WARNING: Multiple code pieces could do this at once (so we don't have to lock machine-wide)
                //          so be careful here.  Only stick legal values in here, don't stick temporary values.

                // Read our data file and set mapBytesToUnicode and mapUnicodeToBytes appropriately
                // First table is just all 256 mappings

                byte[] buffer = new byte[256 * sizeof(char)];
                lock (s_streamLock)
                {
                    s_codePagesEncodingDataStream.Seek(m_firstDataWordOffset, SeekOrigin.Begin);
                    s_codePagesEncodingDataStream.Read(buffer, 0, buffer.Length);
                }

                fixed (byte* pBuffer = buffer)
                {
                    char* pTemp = (char*)pBuffer;
                    for (int b = 0; b < 256; b++)
                    {
                        // Don't want to force 0's to map Unicode wrong.  0 byte == 0 unicode already taken care of
                        if (pTemp[b] != 0 || b == 0)
                        {
                            mapBytesToUnicode[b] = pTemp[b];

                            if (pTemp[b] != UNKNOWN_CHAR)
                                mapUnicodeToBytes[pTemp[b]] = (byte)b;
                        }
                        else
                        {
                            mapBytesToUnicode[b] = UNKNOWN_CHAR;
                        }
                    }
                }
                
                _mapBytesToUnicode = mapBytesToUnicode;
                _mapUnicodeToBytes = mapUnicodeToBytes;
            }
        }

        // Private object for locking instead of locking on a public type for SQL reliability work.
        private static Object s_InternalSyncObject;
        private static Object InternalSyncObject
        {
            get
            {
                if (s_InternalSyncObject == null)
                {
                    Object o = new Object();
                    Interlocked.CompareExchange<Object>(ref s_InternalSyncObject, o, null);
                }
                return s_InternalSyncObject;
            }
        }

        // Read in our best fit table
        [System.Security.SecurityCritical]  // auto-generated
        protected unsafe override void ReadBestFitTable()
        {
            // Lock so we don't confuse ourselves.
            lock (InternalSyncObject)
            {
                // If we got a best fit array already, then don't do this
                if (arrayUnicodeBestFit == null)
                {
                    //
                    // Read in Best Fit table.
                    //

                    // First check the SBCS->Unicode best fit table, which starts right after the
                    // 256 word data table.  This table looks like word, word where 1st word is byte and 2nd
                    // word is replacement for that word.  It ends when byte == 0.
                    byte[] buffer = new byte[m_dataSize - 512];
                    lock (s_streamLock)
                    {
                        s_codePagesEncodingDataStream.Seek(m_firstDataWordOffset + 512, SeekOrigin.Begin);
                        s_codePagesEncodingDataStream.Read(buffer, 0, buffer.Length);
                    }

                    fixed (byte* pBuffer = buffer)
                    {
                        byte* pData = pBuffer;
                        // Need new best fit array
                        char[] arrayTemp = new char[256];
                        for (int i = 0; i < 256; i++)
                            arrayTemp[i] = _mapBytesToUnicode[i];

                        // See if our words are zero
                        ushort byteTemp;
                        while ((byteTemp = *((ushort*)pData)) != 0)
                        {
                            Debug.Assert(arrayTemp[byteTemp] == UNKNOWN_CHAR, String.Format(CultureInfo.InvariantCulture,
                                "[SBCSCodePageEncoding::ReadBestFitTable] Expected unallocated byte (not 0x{2:X2}) for best fit byte at 0x{0:X2} for code page {1}",
                                byteTemp, CodePage, (int)arrayTemp[byteTemp]));
                            pData += 2;

                            arrayTemp[byteTemp] = *((char*)pData);
                            pData += 2;
                        }

                        // Remember our new array
                        arrayBytesBestFit = arrayTemp;

                        // It was on 0, it needs to be on next byte
                        pData += 2;
                        byte* pUnicodeToSBCS = pData;

                        // Now count our characters from our Unicode->SBCS best fit table,
                        // which is right after our 256 byte data table
                        int iBestFitCount = 0;

                        // Now do the UnicodeToBytes Best Fit mapping (this is the one we normally think of when we say "best fit")
                        // pData should be pointing at the first data point for Bytes->Unicode table
                        int unicodePosition = *((ushort*)pData);
                        pData += 2;

                        while (unicodePosition < 0x10000)
                        {
                            // Get the next byte
                            byte input = *pData;
                            pData++;

                            // build our table:
                            if (input == 1)
                            {
                                // Use next 2 bytes as our byte position
                                unicodePosition = *((ushort*)pData);
                                pData += 2;
                            }
                            else if (input < 0x20 && input > 0 && input != 0x1e)
                            {
                                // Advance input characters
                                unicodePosition += input;
                            }
                            else
                            {
                                // Use this character if it isn't zero
                                if (input > 0)
                                    iBestFitCount++;

                                // skip this unicode position in any case
                                unicodePosition++;
                            }
                        }

                        // Make an array for our best fit data
                        arrayTemp = new char[iBestFitCount * 2];

                        // Now actually read in the data
                        // reset pData should be pointing at the first data point for Bytes->Unicode table
                        pData = pUnicodeToSBCS;
                        unicodePosition = *((ushort*)pData);
                        pData += 2;
                        iBestFitCount = 0;

                        while (unicodePosition < 0x10000)
                        {
                            // Get the next byte
                            byte input = *pData;
                            pData++;

                            // build our table:
                            if (input == 1)
                            {
                                // Use next 2 bytes as our byte position
                                unicodePosition = *((ushort*)pData);
                                pData += 2;
                            }
                            else if (input < 0x20 && input > 0 && input != 0x1e)
                            {
                                // Advance input characters
                                unicodePosition += input;
                            }
                            else
                            {
                                // Check for escape for glyph range
                                if (input == 0x1e)
                                {
                                    // Its an escape, so just read next byte directly
                                    input = *pData;
                                    pData++;
                                }

                                // 0 means just skip me
                                if (input > 0)
                                {
                                    // Use this character
                                    arrayTemp[iBestFitCount++] = (char)unicodePosition;
                                    // Have to map it to Unicode because best fit will need unicode value of best fit char.
                                    arrayTemp[iBestFitCount++] = _mapBytesToUnicode[input];

                                    // This won't work if it won't round trip.
                                    Debug.Assert(arrayTemp[iBestFitCount - 1] != (char)0,
                                        String.Format(CultureInfo.InvariantCulture,
                                        "[SBCSCodePageEncoding.ReadBestFitTable] No valid Unicode value {0:X4} for round trip bytes {1:X4}, encoding {2}",
                                        (int)_mapBytesToUnicode[input], (int)input, CodePage));
                                }
                                unicodePosition++;
                            }
                        }

                        // Remember it
                        arrayUnicodeBestFit = arrayTemp;
                    } // Fixed()
                }
            }
        }

        // GetByteCount
        // Note: We start by assuming that the output will be the same as count.  Having
        // an encoder or fallback may change that assumption
        [System.Security.SecurityCritical]  // auto-generated
        public override unsafe int GetByteCount(char* chars, int count, EncoderNLS encoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            Debug.Assert(count >= 0, "[SBCSCodePageEncoding.GetByteCount]count is negative");
            Debug.Assert(chars != null, "[SBCSCodePageEncoding.GetByteCount]chars is null");

            // Assert because we shouldn't be able to have a null encoder.
            Debug.Assert(EncoderFallback != null, "[SBCSCodePageEncoding.GetByteCount]Attempting to use null fallback");

            CheckMemorySection();

            // Need to test fallback
            EncoderReplacementFallback fallback = null;

            // Get any left over characters
            char charLeftOver = (char)0;
            if (encoder != null)
            {
                charLeftOver = encoder.charLeftOver;
                Debug.Assert(charLeftOver == 0 || Char.IsHighSurrogate(charLeftOver),
                    "[SBCSCodePageEncoding.GetByteCount]leftover character should be high surrogate");
                fallback = encoder.Fallback as EncoderReplacementFallback;

                // Verify that we have no fallbackbuffer, actually for SBCS this is always empty, so just assert
                Debug.Assert(!encoder.m_throwOnOverflow || !encoder.InternalHasFallbackBuffer ||
                    encoder.FallbackBuffer.Remaining == 0,
                    "[SBCSCodePageEncoding.GetByteCount]Expected empty fallback buffer at start");
            }
            else
            {
                // If we aren't using default fallback then we may have a complicated count.
                fallback = EncoderFallback as EncoderReplacementFallback;
            }

            if ((fallback != null && fallback.MaxCharCount == 1)/* || bIsBestFit*/)
            {
                // Replacement fallback encodes surrogate pairs as two ?? (or two whatever), so return size is always
                // same as input size.
                // Note that no existing SBCS code pages map code points to supplementary characters, so this is easy.

                // We could however have 1 extra byte if the last call had an encoder and a funky fallback and
                // if we don't use the funky fallback this time.

                // Do we have an extra char left over from last time?
                if (charLeftOver > 0)
                    count++;

                return (count);
            }

            // It had a funky fallback, so it's more complicated
            // May need buffer later
            EncoderFallbackBuffer fallbackBuffer = null;

            // prepare our end
            int byteCount = 0;
            char* charEnd = chars + count;

            EncoderFallbackBufferHelper fallbackHelper = new EncoderFallbackBufferHelper(fallbackBuffer);

            // We may have a left over character from last time, try and process it.
            if (charLeftOver > 0)
            {
                // Since leftover char was a surrogate, it'll have to be fallen back.
                // Get fallback
                Debug.Assert(encoder != null, "[SBCSCodePageEncoding.GetByteCount]Expect to have encoder if we have a charLeftOver");
                fallbackBuffer = encoder.FallbackBuffer;
                fallbackHelper = new EncoderFallbackBufferHelper(fallbackBuffer);
                fallbackHelper.InternalInitialize(chars, charEnd, encoder, false);

                // This will fallback a pair if *chars is a low surrogate
                fallbackHelper.InternalFallback(charLeftOver, ref chars);
            }

            // Now we may have fallback char[] already from the encoder

            // Go ahead and do it, including the fallback.
            char ch;
            while ((ch = (fallbackBuffer == null) ? '\0' : fallbackHelper.InternalGetNextChar()) != 0 || chars < charEnd)
            {
                // First unwind any fallback
                if (ch == 0)
                {
                    // No fallback, just get next char
                    ch = *chars;
                    chars++;
                }

                // get byte for this char
                byte bTemp = _mapUnicodeToBytes[ch];

                // Check for fallback, this'll catch surrogate pairs too.
                if (bTemp == 0 && ch != (char)0)
                {
                    if (fallbackBuffer == null)
                    {
                        // Create & init fallback buffer
                        if (encoder == null)
                            fallbackBuffer = EncoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = encoder.FallbackBuffer;

                        fallbackHelper = new EncoderFallbackBufferHelper(fallbackBuffer);

                        // chars has moved so we need to remember figure it out so Exception fallback
                        // index will be correct
                        fallbackHelper.InternalInitialize(charEnd - count, charEnd, encoder, false);
                    }

                    // Get Fallback
                    fallbackHelper.InternalFallback(ch, ref chars);
                    continue;
                }

                // We'll use this one
                byteCount++;
            }

            Debug.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[SBCSEncoding.GetByteCount]Expected Empty fallback buffer at end");

            return (int)byteCount;
        }

        [System.Security.SecurityCritical]  // auto-generated
        public override unsafe int GetBytes(char* chars, int charCount,
                                                byte* bytes, int byteCount, EncoderNLS encoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            Debug.Assert(bytes != null, "[SBCSCodePageEncoding.GetBytes]bytes is null");
            Debug.Assert(byteCount >= 0, "[SBCSCodePageEncoding.GetBytes]byteCount is negative");
            Debug.Assert(chars != null, "[SBCSCodePageEncoding.GetBytes]chars is null");
            Debug.Assert(charCount >= 0, "[SBCSCodePageEncoding.GetBytes]charCount is negative");

            // Assert because we shouldn't be able to have a null encoder.
            Debug.Assert(EncoderFallback != null, "[SBCSCodePageEncoding.GetBytes]Attempting to use null encoder fallback");

            CheckMemorySection();

            // Need to test fallback
            EncoderReplacementFallback fallback = null;

            // Get any left over characters
            char charLeftOver = (char)0;
            if (encoder != null)
            {
                charLeftOver = encoder.charLeftOver;
                Debug.Assert(charLeftOver == 0 || Char.IsHighSurrogate(charLeftOver),
                    "[SBCSCodePageEncoding.GetBytes]leftover character should be high surrogate");
                fallback = encoder.Fallback as EncoderReplacementFallback;

                // Verify that we have no fallbackbuffer, for SBCS its always empty, so just assert
                Debug.Assert(!encoder.m_throwOnOverflow || !encoder.InternalHasFallbackBuffer ||
                    encoder.FallbackBuffer.Remaining == 0,
                    "[SBCSCodePageEncoding.GetBytes]Expected empty fallback buffer at start");
                //                if (encoder.m_throwOnOverflow && encoder.InternalHasFallbackBuffer &&
                //                    encoder.FallbackBuffer.Remaining > 0)
                //                    throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty",
                //                        EncodingName, encoder.Fallback.GetType()));
            }
            else
            {
                // If we aren't using default fallback then we may have a complicated count.
                fallback = EncoderFallback as EncoderReplacementFallback;
            }

            // prepare our end
            char* charEnd = chars + charCount;
            byte* byteStart = bytes;
            char* charStart = chars;

            // See if we do the fast default or slightly slower fallback
            if (fallback != null && fallback.MaxCharCount == 1)
            {
                // Make sure our fallback character is valid first
                byte bReplacement = _mapUnicodeToBytes[fallback.DefaultString[0]];

                // Check for replacements in range, otherwise fall back to slow version.
                if (bReplacement != 0)
                {
                    // We should have exactly as many output bytes as input bytes, unless there's a leftover
                    // character, in which case we may need one more.

                    // If we had a leftover character we will have to add a ?  (This happens if they had a funky
                    // fallback last time, but not this time. We can't spit any out though,
                    // because with fallback encoder each surrogate is treated as a separate code point)
                    if (charLeftOver > 0)
                    {
                        // Have to have room
                        // Throw even if doing no throw version because this is just 1 char,
                        // so buffer will never be big enough
                        if (byteCount == 0)
                            ThrowBytesOverflow(encoder, true);

                        // This'll make sure we still have more room and also make sure our return value is correct.
                        *(bytes++) = bReplacement;
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

                    // Simple way
                    while (chars < charEnd)
                    {
                        char ch2 = *chars;
                        chars++;

                        byte bTemp = _mapUnicodeToBytes[ch2];

                        // Check for fallback
                        if (bTemp == 0 && ch2 != (char)0)
                            *bytes = bReplacement;
                        else
                            *bytes = bTemp;

                        bytes++;
                    }

                    // Clear encoder
                    if (encoder != null)
                    {
                        encoder.charLeftOver = (char)0;
                        encoder.m_charsUsed = (int)(chars - charStart);
                    }
                    return (int)(bytes - byteStart);
                }
            }

            // Slower version, have to do real fallback.

            // For fallback we may need a fallback buffer, we know we aren't default fallback
            EncoderFallbackBuffer fallbackBuffer = null;

            // prepare our end
            byte* byteEnd = bytes + byteCount;

            EncoderFallbackBufferHelper fallbackHelper = new EncoderFallbackBufferHelper(fallbackBuffer);

            // We may have a left over character from last time, try and process it.
            if (charLeftOver > 0)
            {
                // Since left over char was a surrogate, it'll have to be fallen back.
                // Get Fallback
                Debug.Assert(encoder != null, "[SBCSCodePageEncoding.GetBytes]Expect to have encoder if we have a charLeftOver");
                fallbackBuffer = encoder.FallbackBuffer;
                fallbackHelper = new EncoderFallbackBufferHelper(fallbackBuffer);


                fallbackHelper.InternalInitialize(chars, charEnd, encoder, true);

                // This will fallback a pair if *chars is a low surrogate
                fallbackHelper.InternalFallback(charLeftOver, ref chars);
                if (fallbackBuffer.Remaining > byteEnd - bytes)
                {
                    // Throw it, if we don't have enough for this we never will
                    ThrowBytesOverflow(encoder, true);
                }
            }

            // Now we may have fallback char[] already from the encoder fallback above

            // Go ahead and do it, including the fallback.
            char ch;
            while ((ch = (fallbackBuffer == null) ? '\0' : fallbackHelper.InternalGetNextChar()) != 0 ||
                    chars < charEnd)
            {
                // First unwind any fallback
                if (ch == 0)
                {
                    // No fallback, just get next char
                    ch = *chars;
                    chars++;
                }

                // get byte for this char
                byte bTemp = _mapUnicodeToBytes[ch];

                // Check for fallback, this'll catch surrogate pairs too.
                if (bTemp == 0 && ch != (char)0)
                {
                    // Get Fallback
                    if (fallbackBuffer == null)
                    {
                        // Create & init fallback buffer
                        if (encoder == null)
                            fallbackBuffer = EncoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = encoder.FallbackBuffer;

                        fallbackHelper = new EncoderFallbackBufferHelper(fallbackBuffer);

                        // chars has moved so we need to remember figure it out so Exception fallback
                        // index will be correct
                        fallbackHelper.InternalInitialize(charEnd - charCount, charEnd, encoder, true);
                    }

                    // Make sure we have enough room.  Each fallback char will be 1 output char
                    // (or recursion exception will be thrown)
                    fallbackHelper.InternalFallback(ch, ref chars);
                    if (fallbackBuffer.Remaining > byteEnd - bytes)
                    {
                        // Didn't use this char, reset it
                        Debug.Assert(chars > charStart, "[SBCSCodePageEncoding.GetBytes]Expected chars to have advanced (fallback)");
                        chars--;
                        fallbackHelper.InternalReset();

                        // Throw it & drop this data
                        ThrowBytesOverflow(encoder, chars == charStart);
                        break;
                    }
                    continue;
                }

                // We'll use this one
                // Bounds check
                if (bytes >= byteEnd)
                {
                    // didn't use this char, we'll throw or use buffer
                    Debug.Assert(fallbackBuffer == null || fallbackHelper.bFallingBack == false, "[SBCSCodePageEncoding.GetBytes]Expected to NOT be falling back");
                    if (fallbackBuffer == null || fallbackHelper.bFallingBack == false)
                    {
                        Debug.Assert(chars > charStart, "[SBCSCodePageEncoding.GetBytes]Expected chars to have advanced (normal)");
                        chars--;                                        // don't use last char
                    }
                    ThrowBytesOverflow(encoder, chars == charStart);    // throw ?
                    break;                                              // don't throw, stop
                }

                // Go ahead and add it
                *bytes = bTemp;
                bytes++;
            }

            // encoder stuff if we have one
            if (encoder != null)
            {
                // Fallback stuck it in encoder if necessary, but we have to clear MustFlush cases
                if (fallbackBuffer != null && !fallbackHelper.bUsedEncoder)
                    // Clear it in case of MustFlush
                    encoder.charLeftOver = (char)0;

                // Set our chars used count
                encoder.m_charsUsed = (int)(chars - charStart);
            }

            // Expect Empty fallback buffer for SBCS
            Debug.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0, "[SBCSEncoding.GetBytes]Expected Empty fallback buffer at end");

            return (int)(bytes - byteStart);
        }

        // This is internal and called by something else,
        [System.Security.SecurityCritical]  // auto-generated
        public override unsafe int GetCharCount(byte* bytes, int count, DecoderNLS decoder)
        {
            // Just assert, we're called internally so these should be safe, checked already
            Debug.Assert(bytes != null, "[SBCSCodePageEncoding.GetCharCount]bytes is null");
            Debug.Assert(count >= 0, "[SBCSCodePageEncoding.GetCharCount]byteCount is negative");

            CheckMemorySection();

            // See if we have best fit
            bool bUseBestFit = false;

            // Only need decoder fallback buffer if not using default replacement fallback or best fit fallback.
            DecoderReplacementFallback fallback = null;

            if (decoder == null)
            {
                fallback = DecoderFallback as DecoderReplacementFallback;
                bUseBestFit = DecoderFallback is InternalDecoderBestFitFallback;
            }
            else
            {
                fallback = decoder.Fallback as DecoderReplacementFallback;
                bUseBestFit = decoder.Fallback is InternalDecoderBestFitFallback;
                Debug.Assert(!decoder.m_throwOnOverflow || !decoder.InternalHasFallbackBuffer ||
                    decoder.FallbackBuffer.Remaining == 0,
                    "[SBCSCodePageEncoding.GetChars]Expected empty fallback buffer at start");
            }

            if (bUseBestFit || (fallback != null && fallback.MaxCharCount == 1))
            {
                // Just return length, SBCS stay the same length because they don't map to surrogate
                // pairs and we don't have a decoder fallback.
                return count;
            }

            // Might need one of these later
            DecoderFallbackBuffer fallbackBuffer = null;
            DecoderFallbackBufferHelper fallbackHelper = new DecoderFallbackBufferHelper(fallbackBuffer);

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
                char c;
                c = _mapBytesToUnicode[*bytes];
                bytes++;

                // If unknown we have to do fallback count
                if (c == UNKNOWN_CHAR)
                {
                    // Must have a fallback buffer
                    if (fallbackBuffer == null)
                    {
                        // Need to adjust count so we get real start
                        if (decoder == null)
                            fallbackBuffer = DecoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;

                        fallbackHelper = new DecoderFallbackBufferHelper(fallbackBuffer);

                        fallbackHelper.InternalInitialize(byteEnd - count, null);
                    }

                    // Use fallback buffer
                    byteBuffer[0] = *(bytes - 1);
                    charCount--;                            // We'd already reserved one for *(bytes-1)
                    charCount += fallbackHelper.InternalFallback(byteBuffer, bytes);
                }
            }

            // Fallback buffer must be empty
            Debug.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[SBCSEncoding.GetCharCount]Expected Empty fallback buffer at end");

            // Converted sequence is same length as input
            return charCount;
        }

        [System.Security.SecurityCritical]  // auto-generated
        public override unsafe int GetChars(byte* bytes, int byteCount,
                                                char* chars, int charCount, DecoderNLS decoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            Debug.Assert(bytes != null, "[SBCSCodePageEncoding.GetChars]bytes is null");
            Debug.Assert(byteCount >= 0, "[SBCSCodePageEncoding.GetChars]byteCount is negative");
            Debug.Assert(chars != null, "[SBCSCodePageEncoding.GetChars]chars is null");
            Debug.Assert(charCount >= 0, "[SBCSCodePageEncoding.GetChars]charCount is negative");

            CheckMemorySection();

            // See if we have best fit
            bool bUseBestFit = false;

            // Do it fast way if using ? replacement or best fit fallbacks
            byte* byteEnd = bytes + byteCount;
            byte* byteStart = bytes;
            char* charStart = chars;

            // Only need decoder fallback buffer if not using default replacement fallback or best fit fallback.
            DecoderReplacementFallback fallback = null;

            if (decoder == null)
            {
                fallback = DecoderFallback as DecoderReplacementFallback;
                bUseBestFit = DecoderFallback is InternalDecoderBestFitFallback;
            }
            else
            {
                fallback = decoder.Fallback as DecoderReplacementFallback;
                bUseBestFit = decoder.Fallback is InternalDecoderBestFitFallback;
                Debug.Assert(!decoder.m_throwOnOverflow || !decoder.InternalHasFallbackBuffer ||
                    decoder.FallbackBuffer.Remaining == 0,
                    "[SBCSCodePageEncoding.GetChars]Expected empty fallback buffer at start");
            }

            if (bUseBestFit || (fallback != null && fallback.MaxCharCount == 1))
            {
                // Try it the fast way
                char replacementChar;
                if (fallback == null)
                    replacementChar = '?';  // Best fit always has ? for fallback for SBCS
                else
                    replacementChar = fallback.DefaultString[0];

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
                    char c;
                    if (bUseBestFit)
                    {
                        if (arrayBytesBestFit == null)
                        {
                            ReadBestFitTable();
                        }
                        c = arrayBytesBestFit[*bytes];
                    }
                    else
                        c = _mapBytesToUnicode[*bytes];
                    bytes++;

                    if (c == UNKNOWN_CHAR)
                        // This is an invalid byte in the ASCII encoding.
                        *chars = replacementChar;
                    else
                        *chars = c;
                    chars++;
                }

                // bytes & chars used are the same
                if (decoder != null)
                    decoder.m_bytesUsed = (int)(bytes - byteStart);
                return (int)(chars - charStart);
            }

            // Slower way's going to need a fallback buffer
            DecoderFallbackBuffer fallbackBuffer = null;
            byte[] byteBuffer = new byte[1];
            char* charEnd = chars + charCount;

            DecoderFallbackBufferHelper fallbackHelper = new DecoderFallbackBufferHelper(
                decoder != null ? decoder.FallbackBuffer : DecoderFallback.CreateFallbackBuffer());

            // Not quite so fast loop
            while (bytes < byteEnd)
            {
                // Faster if don't use *bytes++;
                char c = _mapBytesToUnicode[*bytes];
                bytes++;

                // See if it was unknown
                if (c == UNKNOWN_CHAR)
                {
                    // Make sure we have a fallback buffer
                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = DecoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;

                        fallbackHelper = new DecoderFallbackBufferHelper(fallbackBuffer);

                        fallbackHelper.InternalInitialize(byteEnd - byteCount, charEnd);
                    }

                    // Use fallback buffer
                    Debug.Assert(bytes > byteStart,
                        "[SBCSCodePageEncoding.GetChars]Expected bytes to have advanced already (unknown byte)");
                    byteBuffer[0] = *(bytes - 1);
                    // Fallback adds fallback to chars, but doesn't increment chars unless the whole thing fits.
                    if (!fallbackHelper.InternalFallback(byteBuffer, bytes, ref chars))
                    {
                        // May or may not throw, but we didn't get this byte
                        bytes--;                                            // unused byte
                        fallbackHelper.InternalReset();                     // Didn't fall this back
                        ThrowCharsOverflow(decoder, bytes == byteStart);    // throw?
                        break;                                              // don't throw, but stop loop
                    }
                }
                else
                {
                    // Make sure we have buffer space
                    if (chars >= charEnd)
                    {
                        Debug.Assert(bytes > byteStart,
                            "[SBCSCodePageEncoding.GetChars]Expected bytes to have advanced already (known byte)");
                        bytes--;                                            // unused byte
                        ThrowCharsOverflow(decoder, bytes == byteStart);    // throw?
                        break;                                              // don't throw, but stop loop
                    }

                    *(chars) = c;
                    chars++;
                }
            }

            // Might have had decoder fallback stuff.
            if (decoder != null)
                decoder.m_bytesUsed = (int)(bytes - byteStart);

            // Expect Empty fallback buffer for GetChars
            Debug.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[SBCSEncoding.GetChars]Expected Empty fallback buffer at end");

            return (int)(chars - charStart);
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ArgumentOutOfRange_NeedNonNegNum);
            Contract.EndContractBlock();

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
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.ArgumentOutOfRange_NeedNonNegNum);
            Contract.EndContractBlock();

            // Just return length, SBCS stay the same length because they don't map to surrogate
            long charCount = (long)byteCount;

            // 1 to 1 for most characters.  Only surrogates with fallbacks have less, unknown fallbacks could be longer.
            if (DecoderFallback.MaxCharCount > 1)
                charCount *= DecoderFallback.MaxCharCount;

            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.ArgumentOutOfRange_GetCharCountOverflow);

            return (int)charCount;
        }

        // True if and only if the encoding only uses single byte code points.  (i.e. ASCII, 1252, etc)
        public override bool IsSingleByte
        {
            get
            {
                return true;
            }
        }
    }
}

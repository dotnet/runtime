// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if FEATURE_CODEPAGES_FILE // requires BaseCodePageEncooding
namespace System.Text
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Text;
    using System.Threading;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Security.Permissions;

    // SBCSCodePageEncoding
    [Serializable]
    internal class SBCSCodePageEncoding : BaseCodePageEncoding, ISerializable
    {
        // Pointers to our memory section parts
        [NonSerialized]
        [SecurityCritical]
        unsafe char* mapBytesToUnicode = null;      // char 256
        [NonSerialized]
        [SecurityCritical]
        unsafe byte* mapUnicodeToBytes = null;      // byte 65536
        [NonSerialized]
        [SecurityCritical]
        unsafe int*  mapCodePageCached = null;      // to remember which CP is cached

        const char UNKNOWN_CHAR=(char)0xFFFD;

        // byteUnknown is used for default fallback only
        [NonSerialized]
        byte  byteUnknown;
        [NonSerialized]
        char  charUnknown;

        [System.Security.SecurityCritical]  // auto-generated
        public SBCSCodePageEncoding(int codePage) : this(codePage, codePage)
        {
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal SBCSCodePageEncoding(int codePage, int dataCodePage) : base(codePage, dataCodePage)
        {
        }

        // Constructor called by serialization.
        // Note:  We use the base GetObjectData however
        [System.Security.SecurityCritical]  // auto-generated
        internal SBCSCodePageEncoding(SerializationInfo info, StreamingContext context) : base(0)
        {
            // Actually this can't ever get called, CodePageEncoding is our proxy
            Contract.Assert(false, "Didn't expect to make it to SBCSCodePageEncoding serialization constructor");
            throw new ArgumentNullException("this");
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
            // Should be loading OUR code page
            Contract.Assert(pCodePage->CodePage == this.dataTableCodePage,
                "[SBCSCodePageEncoding.LoadManagedCodePage]Expected to load data table code page");

            // Make sure we're really a 1 byte code page
            if (pCodePage->ByteCount != 1)
                throw new NotSupportedException(
                    Environment.GetResourceString("NotSupported_NoCodepageData", CodePage));

            // Remember our unknown bytes & chars
            byteUnknown = (byte)pCodePage->ByteReplace;
            charUnknown = pCodePage->UnicodeReplace;

            // Get our mapped section 65536 bytes for unicode->bytes, 256 * 2 bytes for bytes->unicode
            // Plus 4 byte to remember CP # when done loading it. (Don't want to get IA64 or anything out of alignment)
            byte *pMemorySection = GetSharedMemory(65536*1 + 256*2 + 4 + iExtraBytes);

            mapBytesToUnicode = (char*)pMemorySection;
            mapUnicodeToBytes = (byte*)(pMemorySection + 256 * 2);
            mapCodePageCached = (int*)(pMemorySection + 256 * 2 + 65536 * 1 + iExtraBytes);

            // If its cached (& filled in) we don't have to do anything else
            if (*mapCodePageCached != 0)
            {
                Contract.Assert(*mapCodePageCached == this.dataTableCodePage,
                    "[DBCSCodePageEncoding.LoadManagedCodePage]Expected mapped section cached page to be same as data table code page.  Cached : " +
                    *mapCodePageCached + " Expected:" + this.dataTableCodePage);

                if (*mapCodePageCached != this.dataTableCodePage)
                    throw new OutOfMemoryException(
                        Environment.GetResourceString("Arg_OutOfMemoryException"));

                // If its cached (& filled in) we don't have to do anything else
                return;
            }

            // Need to read our data file and fill in our section.
            // WARNING: Multiple code pieces could do this at once (so we don't have to lock machine-wide)
            //          so be careful here.  Only stick legal values in here, don't stick temporary values.

            // Read our data file and set mapBytesToUnicode and mapUnicodeToBytes appropriately
            // First table is just all 256 mappings
            char* pTemp = (char*)&(pCodePage->FirstDataWord);
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

            // We're done with our mapped section, set our flag so others don't have to rebuild table.
            *mapCodePageCached = this.dataTableCodePage;
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
            lock(InternalSyncObject)
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
                    byte* pData = (byte*)&(pCodePage->FirstDataWord);
                    pData += 512;

                    // Need new best fit array
                    char[] arrayTemp = new char[256];
                    for (int i = 0; i < 256; i++)
                        arrayTemp[i] = mapBytesToUnicode[i];

                    // See if our words are zero
                    ushort byteTemp;
                    while ((byteTemp = *((ushort*)pData)) != 0)
                    {

                        Contract.Assert(arrayTemp[byteTemp] == UNKNOWN_CHAR, String.Format(CultureInfo.InvariantCulture,
                            "[SBCSCodePageEncoding::ReadBestFitTable] Expected unallocated byte (not 0x{2:X2}) for best fit byte at 0x{0:X2} for code page {1}",
                            byteTemp, CodePage, (int)arrayTemp[byteTemp]));
                        pData += 2;

                        arrayTemp[byteTemp] = *((char*)pData);
                        pData += 2;
                    }

                    // Remember our new array
                    arrayBytesBestFit = arrayTemp;

                    // It was on 0, it needs to be on next byte
                    pData+=2;
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
                            pData+=2;
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
                    arrayTemp = new char[iBestFitCount*2];

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
                            pData+=2;
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
                                arrayTemp[iBestFitCount++] = mapBytesToUnicode[input];

                                // This won't work if it won't round trip.
                                Contract.Assert(arrayTemp[iBestFitCount-1] != (char)0,
                                    String.Format(CultureInfo.InvariantCulture,
                                    "[SBCSCodePageEncoding.ReadBestFitTable] No valid Unicode value {0:X4} for round trip bytes {1:X4}, encoding {2}",
                                    (int)mapBytesToUnicode[input], (int)input, CodePage));
                            }
                            unicodePosition++;
                        }
                    }

                    // Remember it
                    arrayUnicodeBestFit = arrayTemp;
                }
            }
        }

        // GetByteCount
        // Note: We start by assuming that the output will be the same as count.  Having
        // an encoder or fallback may change that assumption
        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetByteCount(char* chars, int count, EncoderNLS encoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            Contract.Assert(count >= 0, "[SBCSCodePageEncoding.GetByteCount]count is negative");
            Contract.Assert(chars != null, "[SBCSCodePageEncoding.GetByteCount]chars is null");

            // Assert because we shouldn't be able to have a null encoder.
            Contract.Assert(encoderFallback != null, "[SBCSCodePageEncoding.GetByteCount]Attempting to use null fallback");

            CheckMemorySection();

            // Need to test fallback
            EncoderReplacementFallback fallback = null;

            // Get any left over characters
            char charLeftOver = (char)0;
            if (encoder != null)
            {
                charLeftOver = encoder.charLeftOver;
                Contract.Assert(charLeftOver == 0 || Char.IsHighSurrogate(charLeftOver),
                    "[SBCSCodePageEncoding.GetByteCount]leftover character should be high surrogate");
                fallback = encoder.Fallback as EncoderReplacementFallback;

                // Verify that we have no fallbackbuffer, actually for SBCS this is always empty, so just assert
                Contract.Assert(!encoder.m_throwOnOverflow || !encoder.InternalHasFallbackBuffer ||
                    encoder.FallbackBuffer.Remaining == 0,
                    "[SBCSCodePageEncoding.GetByteCount]Expected empty fallback buffer at start");
            }
            else
            {
                // If we aren't using default fallback then we may have a complicated count.
                fallback = this.EncoderFallback as EncoderReplacementFallback;
            }

            if ((fallback != null && fallback.MaxCharCount == 1)/* || bIsBestFit*/)
            {
                // Replacement fallback encodes surrogate pairs as two ?? (or two whatever), so return size is always
                // same as input size.
                // Note that no existing SBCS code pages map code points to supplimentary characters, so this is easy.

                // We could however have 1 extra byte if the last call had an encoder and a funky fallback and
                // if we don't use the funky fallback this time.

                // Do we have an extra char left over from last time?
                if (charLeftOver > 0)
                    count++;

                return (count);
            }

            // It had a funky fallback, so its more complicated
            // Need buffer maybe later
            EncoderFallbackBuffer fallbackBuffer = null;

            // prepare our end
            int byteCount = 0;
            char* charEnd = chars + count;

            // We may have a left over character from last time, try and process it.
            if (charLeftOver > 0)
            {
                // Since left over char was a surrogate, it'll have to be fallen back.
                // Get Fallback
                Contract.Assert(encoder != null, "[SBCSCodePageEncoding.GetByteCount]Expect to have encoder if we have a charLeftOver");
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

                // get byte for this char
                byte bTemp = mapUnicodeToBytes[ch];

                // Check for fallback, this'll catch surrogate pairs too.
                if (bTemp == 0 && ch != (char)0)
                {
                    if (fallbackBuffer == null)
                    {
                        // Create & init fallback buffer
                        if (encoder == null)
                            fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = encoder.FallbackBuffer;

                        // chars has moved so we need to remember figure it out so Exception fallback
                        // index will be correct
                        fallbackBuffer.InternalInitialize(charEnd - count, charEnd, encoder, false);
                    }

                    // Get Fallback
                    fallbackBuffer.InternalFallback(ch, ref chars);
                    continue;
                }

                // We'll use this one
                byteCount++;
            }

            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[SBCSEncoding.GetByteCount]Expected Empty fallback buffer at end");

            return (int)byteCount;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetBytes(char* chars, int charCount,
                                                byte* bytes, int byteCount, EncoderNLS encoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            Contract.Assert(bytes != null, "[SBCSCodePageEncoding.GetBytes]bytes is null");
            Contract.Assert(byteCount >= 0, "[SBCSCodePageEncoding.GetBytes]byteCount is negative");
            Contract.Assert(chars != null, "[SBCSCodePageEncoding.GetBytes]chars is null");
            Contract.Assert(charCount >= 0, "[SBCSCodePageEncoding.GetBytes]charCount is negative");

            // Assert because we shouldn't be able to have a null encoder.
            Contract.Assert(encoderFallback != null, "[SBCSCodePageEncoding.GetBytes]Attempting to use null encoder fallback");

            CheckMemorySection();

            // Need to test fallback
            EncoderReplacementFallback fallback = null;

            // Get any left over characters
            char charLeftOver = (char)0;
            if (encoder != null)
            {
                charLeftOver = encoder.charLeftOver;
                Contract.Assert(charLeftOver == 0 || Char.IsHighSurrogate(charLeftOver),
                    "[SBCSCodePageEncoding.GetBytes]leftover character should be high surrogate");
                fallback = encoder.Fallback as EncoderReplacementFallback;

                // Verify that we have no fallbackbuffer, for SBCS its always empty, so just assert
                Contract.Assert(!encoder.m_throwOnOverflow || !encoder.InternalHasFallbackBuffer ||
                    encoder.FallbackBuffer.Remaining == 0,
                    "[SBCSCodePageEncoding.GetBytes]Expected empty fallback buffer at start");
//                if (encoder.m_throwOnOverflow && encoder.InternalHasFallbackBuffer &&
//                    encoder.FallbackBuffer.Remaining > 0)
//                    throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty",
//                        this.EncodingName, encoder.Fallback.GetType()));
            }
            else
            {
                // If we aren't using default fallback then we may have a complicated count.
                fallback = this.EncoderFallback as EncoderReplacementFallback;
            }

            // prepare our end
            char* charEnd = chars + charCount;
            byte* byteStart = bytes;
            char* charStart = chars;

            // See if we do the fast default or slightly slower fallback
            if (fallback != null && fallback.MaxCharCount == 1)
            {
                // Make sure our fallback character is valid first
                byte bReplacement = mapUnicodeToBytes[fallback.DefaultString[0]];

                // Check for replacements in range, otherwise fall back to slow version.
                if (bReplacement != 0)
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

                        byte bTemp = mapUnicodeToBytes[ch2];

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
                        encoder.m_charsUsed = (int)(chars-charStart);
                    }
                    return (int)(bytes - byteStart);
                }
            }

            // Slower version, have to do real fallback.

            // For fallback we may need a fallback buffer, we know we aren't default fallback
            EncoderFallbackBuffer fallbackBuffer = null;

            // prepare our end
            byte* byteEnd = bytes + byteCount;

            // We may have a left over character from last time, try and process it.
            if (charLeftOver > 0)
            {
                // Since left over char was a surrogate, it'll have to be fallen back.
                // Get Fallback
                Contract.Assert(encoder != null, "[SBCSCodePageEncoding.GetBytes]Expect to have encoder if we have a charLeftOver");
                fallbackBuffer = encoder.FallbackBuffer;
                fallbackBuffer.InternalInitialize(chars, charEnd, encoder, true);

                // This will fallback a pair if *chars is a low surrogate
                fallbackBuffer.InternalFallback(charLeftOver, ref chars);
                if (fallbackBuffer.Remaining > byteEnd - bytes)
                {
                    // Throw it, if we don't have enough for this we never will
                    ThrowBytesOverflow(encoder, true);
                }
            }

            // Now we may have fallback char[] already from the encoder fallback above

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

                // get byte for this char
                byte bTemp = mapUnicodeToBytes[ch];

                // Check for fallback, this'll catch surrogate pairs too.
                if (bTemp == 0 && ch != (char)0)
                {
                    // Get Fallback
                    if ( fallbackBuffer == null )
                    {
                        // Create & init fallback buffer
                        if (encoder == null)
                            fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = encoder.FallbackBuffer;
                        // chars has moved so we need to remember figure it out so Exception fallback
                        // index will be correct
                        fallbackBuffer.InternalInitialize(charEnd - charCount, charEnd, encoder, true);
                    }

                    // Make sure we have enough room.  Each fallback char will be 1 output char
                    // (or recursion exception will be thrown)
                    fallbackBuffer.InternalFallback(ch, ref chars);
                    if (fallbackBuffer.Remaining > byteEnd - bytes)
                    {
                        // Didn't use this char, reset it
                        Contract.Assert(chars > charStart,
                            "[SBCSCodePageEncoding.GetBytes]Expected chars to have advanced (fallback)");
                        chars--;
                        fallbackBuffer.InternalReset();

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
                    Contract.Assert(fallbackBuffer == null || fallbackBuffer.bFallingBack == false,
                        "[SBCSCodePageEncoding.GetBytes]Expected to NOT be falling back");
                    if (fallbackBuffer == null || fallbackBuffer.bFallingBack == false)
                    {
                        Contract.Assert(chars > charStart,
                            "[SBCSCodePageEncoding.GetBytes]Expected chars to have advanced (normal)");                        
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
                if (fallbackBuffer != null && !fallbackBuffer.bUsedEncoder)
                    // Clear it in case of MustFlush
                    encoder.charLeftOver = (char)0;

                // Set our chars used count
                encoder.m_charsUsed = (int)(chars - charStart);
            }

            // Expect Empty fallback buffer for SBCS
            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[SBCSEncoding.GetBytes]Expected Empty fallback buffer at end");

            return (int)(bytes - byteStart);
        }

        // This is internal and called by something else,
        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetCharCount(byte* bytes, int count, DecoderNLS decoder)
        {
            // Just assert, we're called internally so these should be safe, checked already
            Contract.Assert(bytes != null, "[SBCSCodePageEncoding.GetCharCount]bytes is null");
            Contract.Assert(count >= 0, "[SBCSCodePageEncoding.GetCharCount]byteCount is negative");

            CheckMemorySection();

            // See if we have best fit
            bool bUseBestFit = false;
            
            // Only need decoder fallback buffer if not using default replacement fallback or best fit fallback.
            DecoderReplacementFallback fallback = null;

            if (decoder == null)
            {
                fallback = this.DecoderFallback as DecoderReplacementFallback;
                bUseBestFit = this.DecoderFallback.IsMicrosoftBestFitFallback;                
            }
            else
            {
                fallback = decoder.Fallback as DecoderReplacementFallback;
                bUseBestFit = decoder.Fallback.IsMicrosoftBestFitFallback;
                Contract.Assert(!decoder.m_throwOnOverflow || !decoder.InternalHasFallbackBuffer ||
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
                c = mapBytesToUnicode[*bytes];
                bytes++;

                // If unknown we have to do fallback count
                if (c == UNKNOWN_CHAR)
                {
                    // Must have a fallback buffer
                    if (fallbackBuffer == null)
                    {
                        // Need to adjust count so we get real start
                        if (decoder == null)
                            fallbackBuffer = this.DecoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(byteEnd - count, null);
                    }

                    // Use fallback buffer
                    byteBuffer[0] = *(bytes - 1);
                    charCount--;                            // We'd already reserved one for *(bytes-1)
                    charCount += fallbackBuffer.InternalFallback(byteBuffer, bytes);
                }
            }

            // Fallback buffer must be empty
            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[SBCSEncoding.GetCharCount]Expected Empty fallback buffer at end");

            // Converted sequence is same length as input
            return charCount;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetChars(byte* bytes, int byteCount,
                                                char* chars, int charCount, DecoderNLS decoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            Contract.Assert(bytes != null, "[SBCSCodePageEncoding.GetChars]bytes is null");
            Contract.Assert(byteCount >= 0, "[SBCSCodePageEncoding.GetChars]byteCount is negative");
            Contract.Assert(chars != null, "[SBCSCodePageEncoding.GetChars]chars is null");
            Contract.Assert(charCount >= 0, "[SBCSCodePageEncoding.GetChars]charCount is negative");

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
                fallback = this.DecoderFallback as DecoderReplacementFallback;
                bUseBestFit = this.DecoderFallback.IsMicrosoftBestFitFallback;                
            }
            else
            {
                fallback = decoder.Fallback as DecoderReplacementFallback;
                bUseBestFit = decoder.Fallback.IsMicrosoftBestFitFallback;
                Contract.Assert(!decoder.m_throwOnOverflow || !decoder.InternalHasFallbackBuffer ||
                    decoder.FallbackBuffer.Remaining == 0,
                    "[SBCSCodePageEncoding.GetChars]Expected empty fallback buffer at start");
            }

            if (bUseBestFit || (fallback != null && fallback.MaxCharCount == 1))
            {
                // Try it the fast way
                char replacementChar;
                if (fallback == null)
                    replacementChar = '?';  // Best fit alwasy has ? for fallback for SBCS
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
                        c = mapBytesToUnicode[*bytes];
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
            char*   charEnd = chars + charCount;

            // Not quite so fast loop
            while (bytes < byteEnd)
            {
                // Faster if don't use *bytes++;
                char c = mapBytesToUnicode[*bytes];
                bytes++;

                // See if it was unknown
                if (c == UNKNOWN_CHAR)
                {
                    // Make sure we have a fallback buffer
                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.DecoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(byteEnd - byteCount, charEnd);
                    }

                    // Use fallback buffer
                    Contract.Assert(bytes > byteStart,
                        "[SBCSCodePageEncoding.GetChars]Expected bytes to have advanced already (unknown byte)");
                    byteBuffer[0] = *(bytes - 1);
                    // Fallback adds fallback to chars, but doesn't increment chars unless the whole thing fits.
                    if (!fallbackBuffer.InternalFallback(byteBuffer, bytes, ref chars))
                    {
                        // May or may not throw, but we didn't get this byte
                        bytes--;                                            // unused byte
                        fallbackBuffer.InternalReset();                     // Didn't fall this back
                        ThrowCharsOverflow(decoder, bytes == byteStart);    // throw?
                        break;                                              // don't throw, but stop loop
                    }
                }
                else
                {
                    // Make sure we have buffer space
                    if (chars >= charEnd)
                    {
                        Contract.Assert(bytes > byteStart,
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
            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[SBCSEncoding.GetChars]Expected Empty fallback buffer at end");

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
        public override bool IsSingleByte
        {
            get
            {
                return true;
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public override bool IsAlwaysNormalized(NormalizationForm form)
        {
            // Most of these code pages could be decomposed or have compatibility mappings for KC, KD, & D
            // additionally the allow unassigned forms and IDNA wouldn't work either, so C is our choice.
            if (form == NormalizationForm.FormC)
            {
                // Form C is only true for some code pages.  They have to have all 256 code points assigned
                // and not map to unassigned or combinable code points.
                switch (CodePage)
                {
                    // Return true for some code pages.
                    case 1252:     // (Latin I - ANSI)
                    case 1250:     // (Eastern Europe - ANSI)
                    case 1251:     // (Cyrillic - ANSI)
                    case 1254:     // (Turkish - ANSI)
                    case 1256:     // (Arabic - ANSI)
                    case 28591:    // (ISO 8859-1 Latin I)
                    case 437:      // (United States - OEM)
                    case 737:      // (Greek (aka 437G) - OEM)
                    case 775:      // (Baltic - OEM)
                    case 850:      // (Multilingual (Latin I) - OEM)
                    case 852:      // (Slovak (Latin II) - OEM)
                    case 855:      // (Cyrillic - OEM)
                    case 858:      // (Multilingual (Latin I) - OEM + Euro)
                    case 860:      // (Portuguese - OEM)
                    case 861:      // (Icelandic - OEM)
                    case 862:      // (Hebrew - OEM)
                    case 863:      // (Canadian French - OEM)
                    case 865:      // (Nordic - OEM)
                    case 866:      // (Russian - OEM)
                    case 869:      // (Modern Greek - OEM)
                    case 10007:    // (Cyrillic - MAC)
                    case 10017:    // (Ukraine - MAC)
                    case 10029:    // (Latin II - MAC)
                    case 28592:    // (ISO 8859-2 Eastern Europe)
                    case 28594:    // (ISO 8859-4 Baltic)
                    case 28595:    // (ISO 8859-5 Cyrillic)
                    case 28599:    // (ISO 8859-9 Latin Alphabet No.5)
                    case 28603:    // (ISO/IEC 8859-13:1998 (Lithuanian))
                    case 28605:    // (ISO 8859-15 Latin 9 (IBM923=IBM819+Euro))
                    case 037:      // (IBM EBCDIC U.S./Canada)
                    case 500:      // (IBM EBCDIC International)
                    case 870:      // (IBM EBCDIC Latin-2 Multilingual/ROECE)
                    case 1026:     // (IBM EBCDIC Latin-5 Turkey)
                    case 1047:     // (IBM Latin-1/Open System)
                    case 1140:     // (IBM EBCDIC U.S./Canada (037+Euro))
                    case 1141:     // (IBM EBCDIC Germany (20273(IBM273)+Euro))
                    case 1142:     // (IBM EBCDIC Denmark/Norway (20277(IBM277+Euro))
                    case 1143:     // (IBM EBCDIC Finland/Sweden (20278(IBM278)+Euro))
                    case 1144:     // (IBM EBCDIC Italy (20280(IBM280)+Euro))
                    case 1145:     // (IBM EBCDIC Latin America/Spain (20284(IBM284)+Euro))
                    case 1146:     // (IBM EBCDIC United Kingdom (20285(IBM285)+Euro))
                    case 1147:     // (IBM EBCDIC France (20297(IBM297+Euro))
                    case 1148:     // (IBM EBCDIC International (500+Euro))
                    case 1149:     // (IBM EBCDIC Icelandic (20871(IBM871+Euro))
                    case 20273:    // (IBM EBCDIC Germany)
                    case 20277:    // (IBM EBCDIC Denmark/Norway)
                    case 20278:    // (IBM EBCDIC Finland/Sweden)
                    case 20280:    // (IBM EBCDIC Italy)
                    case 20284:    // (IBM EBCDIC Latin America/Spain)
                    case 20285:    // (IBM EBCDIC United Kingdom)
                    case 20297:    // (IBM EBCDIC France)
                    case 20871:    // (IBM EBCDIC Icelandic)
                    case 20880:    // (IBM EBCDIC Cyrillic)
                    case 20924:    // (IBM Latin-1/Open System (IBM924=IBM1047+Euro))
                    case 21025:    // (IBM EBCDIC Cyrillic (Serbian, Bulgarian))
                    case 720:      // (Arabic - Transparent ASMO)
                    case 20866:    // (Russian - KOI8)
                    case 21866:    // (Ukrainian - KOI8-U)
                        return true;        
                }
            }

            // False for IDNA and unknown
            return false;
        }        
    }
}
#endif // FEATURE_CODEPAGES_FILE

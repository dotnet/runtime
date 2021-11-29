// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//
//  Ported to managed code from c_gb18030.c and related gb18030 dll files
//
//
//  Abstract:
//
//      Managed implementation of GB18030-2000 (code page 54936) ported from implementation in c_g18030.dll
//      If you find a bug here you should check there (in windows) as well and visa versa.
//      This file contains functions to convert GB18030-2000 (code page 54936) into Unicode, and vice versa.
//
//  Notes:
//      GB18030-2000 (aka GBK2K) is designed to be mostly compatible with GBK (codepage 936),
//      while supports the full range of Unicode code points (BMP + 16 supplementary planes).
//
//      The structure for GB18030 is:
//          * Single byte:
//              0x00 ~ 0x7f
//          * Two-byte:
//              0x81 ~ 0xfe, 0x40 ~ 0x7e    (leading byte, trailing byte)
//              0x81 ~ 0xfe, 0x80 ~ 0xfe    (leading byte, trailing byte)
//          * Four-byte:
//              0x81 ~ 0xfe, 0x30 ~ 0x39, 0x81 ~ 0xfe, 0x30 ~ 0x39.
//              The surrogate pair will be encoded from 0x90, 0x30, 0x81, 0x30
//
//      The BMP range is fully supported in GB18030 using 1-byte, 2-byte and 4-byte sequences.
//      In valid 4-byte GB18030, there are two gaps that can not be mapped to Unicode characters.
//          0x84, 0x31, 0xa5, 0x30 (just after the GB18030 bytes for U+FFFF(*)) ~ 0x8f, 0x39, 0xfe, 0x39 (just before the first GB18030 bytes for U+D800,U+DC00)
//          0xe3, 0x32, 0x9a, 0x36 (just after the GB18030 bytes for U+DBFF U+DFFF(**)) ~ 0xfe, 0x39, 0xfe, 0x39
//
//
//          Note1: U+FFFF = 0x84, 0x31, 0xa4, 0x39
//          Note2: U+DBFF U+DFFF = 0xe3, 0x32, 0x9a, 0x35
//
//      Tables used in GB18030Encoding:
//
//          Our data is similar to the 936 Code Page, so we start from there to build our tables.  We build the
//          normal double byte mapUnicodeToBytes and mapBytesToUnicode tables by applying differences from 936.
//          We also build a map4BytesToUnicode table and a mapUnicodeTo4BytesFlags
//
//          * mapUnicodeTo4BytesFlags
//              This is an array of bytes, so we have to do a / 8 and << %8 to check the appropriate bit (see Is4Byte())
//              If the bit is set its true.
//
//              true    - If set/true this is a 4 byte code.  The value in mapUnicodeToBytes will be the 4 byte offset
//              false   - If cleared/false this is a 1 or 2 byte code.  The value in mapUnicodeToBytes will be the 2 bytes.
//
//          * mapUnicodeToBytes
//              Contains either the 2 byte value of double byte GB18030 or the 4 byte offset for 4 byte GB18030,
//              depending on the value of the flag in mapUnicodeTo4BytesFlags
//
//          * mapBytesToUnicode
//              mapBytesToUnicode maps 2 byte GB 18030 to Unicode like other DBCS code pages.
//
//          * map4BytesToUnicode
//              map4BytesToUnicode is indexed by the 4 byte offset and contains the unicode value for each 4 byte offset
//
//
//      4 Byte sequences
//          We generally use the offset for the 4 byte sequence, such as:
//
//          The index value is the offset of the 4-byte GB18030.
//
//          4-byte GB18030      Index value
//          ==============      ===========
//          81,30,81,30         0
//          81,30,81,31         1
//          81,30,81,32         2
//          ...                 ...
//
//          The value of map4BytesToUnicode contains the Unicode codepoint for the offset of the
//          corresponding 4-byte GB18030.
//
//          E.g. map4BytesToUnicode[0] = 0x0080.  This means that GB18030 0x81, 0x30, 0x81, 0x30 will be converted to Unicode U+0800.
//
//      4 Byte Surrogate Sequences
//          Those work similarly to the normal 4 byte sequences, but start at a different offset
//
//  We don't override IsAlwaysNormalized because GB18030 covers all of the unicode space, so isn't guaranteed to be normal.
//

using System;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Globalization;

namespace System.Text
{
    /*=================================GB18030Encoding============================
    **
    ** This is used to support GB18030-2000 encoding (code page 54936).
    **
    ==============================================================================*/

    internal sealed class GB18030Encoding : DBCSCodePageEncoding
    {
        // This is the table of 4 byte conversions.
        private const int GBLast4ByteCode = 0x99FB;
        internal unsafe char* map4BytesToUnicode = null;       // new char[GBLast4ByteCode + 1]; // Need to map all 4 byte sequences to Unicode
        internal unsafe byte* mapUnicodeTo4BytesFlags = null;  // new byte[0x10000 / 8];         // Need 1 bit for each code point to say if its 4 byte or not

        private const int GB18030 = 54936;

        // First and last character of surrogate range as offset from 4 byte GB18030 GB81308130
        private const int GBSurrogateOffset = 0x2E248;      // GB90308130
        private const int GBLastSurrogateOffset = 0x12E247; // GBE3329A35

        // We have to load the 936 code page tables, so impersonate 936 as our base
        internal GB18030Encoding()
            // For GB18030Encoding just use default replacement fallbacks because its only for bad surrogates
            : base(GB18030, 936, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback)
        {
        }

        // This loads our base 936 code page and then applies the changes from the tableUnicodeToGBDiffs table.
        // See table comments for table format.
        protected override unsafe void LoadManagedCodePage()
        {
            // Use base code page loading algorithm.
            iExtraBytes = (GBLast4ByteCode + 1) * 2 + 0x10000 / 8;

            // Load most of our code page
            base.LoadManagedCodePage();

            // Point to our new data sections
            byte* pNativeMemory = (byte*)safeNativeMemoryHandle!.DangerousGetHandle();
            mapUnicodeTo4BytesFlags = pNativeMemory + 65536 * 2 * 2;
            map4BytesToUnicode = (char*)(pNativeMemory + 65536 * 2 * 2 + 0x10000 / 8);

            // Once we've done our base LoadManagedCodePage, we'll have to add our fixes
            char unicodeCount = (char)0;
            ushort count4Byte = 0;
            for (int index = 0; index < _tableUnicodeToGBDiffs.Length; index++)
            {
                ushort data = _tableUnicodeToGBDiffs[index];

                // Check high bit
                if ((data & 0x8000) != 0)
                {
                    // Make be exact value
                    if (data > 0x9000 && data != 0xD1A6)
                    {
                        // It was an exact value (gb18040[data] = unicode)
                        mapBytesToUnicode[data] = unicodeCount;
                        mapUnicodeToBytes[unicodeCount] = data;
                        unicodeCount++;
                    }
                    else
                    {
                        // It was a CP 936 compatible data, that table's already loaded, just increment our pointer
                        unicodeCount += unchecked((char)(data & 0x7FFF));
                    }
                }
                else
                {
                    // It was GB 18030 4 byte data, next <data> characters are 4 byte sequences.
                    while (data > 0)
                    {
                        Debug.Assert(count4Byte <= GBLast4ByteCode,
                            "[GB18030Encoding.LoadManagedCodePage] Found too many 4 byte codes in data table.");

                        // Set the 4 byte -> Unicode value
                        map4BytesToUnicode[count4Byte] = unicodeCount;
                        // Set the unicode -> 4 bytes value, including flag that its a 4 byte sequence
                        mapUnicodeToBytes[unicodeCount] = count4Byte;
                        // Set the flag saying its a 4 byte sequence
                        mapUnicodeTo4BytesFlags[unicodeCount / 8] |= unchecked((byte)(1 << (unicodeCount % 8)));
                        unchecked
                        {
                            unicodeCount++;
                        }
                        count4Byte++;
                        data--;
                    }
                }
            }

            // unicodeCount should've wrapped back to 0
            Debug.Assert(unicodeCount == 0,
                "[GB18030Encoding.LoadManagedCodePage] Expected unicodeCount to wrap around to 0 as all chars were processed");

            // We should've read in GBLast4ByteCode 4 byte sequences
            Debug.Assert(count4Byte == GBLast4ByteCode + 1,
                $"[GB18030Encoding.LoadManagedCodePage] Expected 0x99FB to be last 4 byte offset, found 0x{count4Byte:X4}");
        }

        // Is4Byte
        // Checks the 4 byte table and returns true if this is a 4 byte code.
        // Its a 4 byte code if the flag is set in mapUnicodeTo4BytesFlags
        internal unsafe bool Is4Byte(char charTest)
        {
            // See what kind it is
            byte b4Byte = mapUnicodeTo4BytesFlags[charTest / 8];
            return (b4Byte != 0 && (b4Byte & (1 << (charTest % 8))) != 0);
        }

        // GetByteCount
        public override unsafe int GetByteCount(char* chars, int count, EncoderNLS? encoder)
        {
            // Just call GetBytes() with null bytes
            return GetBytes(chars, count, null, 0, encoder);
        }

        public override unsafe int GetBytes(char* chars, int charCount,
                                                byte* bytes, int byteCount, EncoderNLS? encoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            // We'll allow null bytes as a count
            //            Debug.Assert(bytes != null, "[GB18030Encoding.GetBytes]bytes is null");
            Debug.Assert(byteCount >= 0, "[GB18030Encoding.GetBytes]byteCount is negative");
            Debug.Assert(chars != null, "[GB18030Encoding.GetBytes]chars is null");
            Debug.Assert(charCount >= 0, "[GB18030Encoding.GetBytes]charCount is negative");

            // Assert because we shouldn't be able to have a null encoder.
            Debug.Assert(EncoderFallback != null, "[GB18030Encoding.GetBytes]Attempting to use null encoder fallback");

            // Get any left over characters
            char charLeftOver = (char)0;
            if (encoder != null)
                charLeftOver = encoder.charLeftOver;

            // prepare our helpers
            EncodingByteBuffer buffer = new EncodingByteBuffer(this, encoder, bytes, byteCount, chars, charCount);

        // Try again if we were MustFlush
        TryAgain:

            // Go ahead and do it, including the fallback.
            while (buffer.MoreData)
            {
                // Get next char
                char ch = buffer.GetNextChar();

                // Have to check for charLeftOver
                if (charLeftOver != 0)
                {
                    Debug.Assert(char.IsHighSurrogate(charLeftOver),
                        $"[GB18030Encoding.GetBytes] leftover character should be high surrogate, not 0x{(int)charLeftOver:X4}");

                    // If our next char isn't a low surrogate, then we need to do fallback.
                    if (!char.IsLowSurrogate(ch))
                    {
                        // No low surrogate, fallback high surrogate & try this one again
                        buffer.MovePrevious(false);                  // (Ignoring this character, don't throw)
                        if (!buffer.Fallback(charLeftOver))
                        {
                            charLeftOver = (char)0;
                            break;
                        }
                        charLeftOver = (char)0;
                        continue;
                    }
                    else
                    {
                        // Next is a surrogate, add it as surrogate pair

                        // Need 4 bytes for surrogates
                        // Get our offset
                        int offset = ((charLeftOver - 0xd800) << 10) + (ch - 0xdc00);

                        byte byte4 = (byte)((offset % 0x0a) + 0x30);
                        offset /= 0x0a;
                        byte byte3 = (byte)((offset % 0x7e) + 0x81);
                        offset /= 0x7e;
                        byte byte2 = (byte)((offset % 0x0a) + 0x30);
                        offset /= 0x0a;
                        Debug.Assert(offset < 0x6f,
                            $"[GB18030Encoding.GetBytes](1) Expected offset < 0x6f, not 0x{offset:X2}");

                        charLeftOver = (char)0;
                        if (!buffer.AddByte((byte)(offset + 0x90), byte2, byte3, byte4))
                        {
                            // Didn't work, need to back up for both surrogates (AddByte already backed up one)
                            buffer.MovePrevious(false);             // (don't throw)
                            break;
                        }
                    }
                    charLeftOver = '\0';
                }
                // ASCII's easiest
                else if (ch <= 0x7f)
                {
                    // Need a byte
                    if (!buffer.AddByte((byte)ch))
                        break;
                }
                // See if its a surrogate pair
                else if (char.IsHighSurrogate(ch))
                {
                    // Remember it for next time
                    charLeftOver = ch;
                }
                else if (char.IsLowSurrogate(ch))
                {
                    // Low surrogates should've been found already
                    if (!buffer.Fallback(ch))
                        break;
                }
                else
                {
                    // Not surrogate or ASCII, get value
                    ushort iBytes = mapUnicodeToBytes[ch];

                    // See what kind it is
                    if (Is4Byte(ch))
                    {
                        //
                        // This Unicode character will be converted to four-byte GB18030.
                        //
                        // Need 4 bytes
                        byte byte4 = (byte)((iBytes % 0x0a) + 0x30);
                        iBytes /= 0x0a;
                        byte byte3 = (byte)((iBytes % 0x7e) + 0x81);
                        iBytes /= 0x7e;
                        byte byte2 = (byte)((iBytes % 0x0a) + 0x30);
                        iBytes /= 0x0a;
                        Debug.Assert(iBytes < 0x7e,
                            $"[GB18030Encoding.GetBytes]Expected iBytes < 0x7e, not 0x{iBytes:X2}");
                        if (!buffer.AddByte((byte)(iBytes + 0x81), byte2, byte3, byte4))
                            break;
                    }
                    else
                    {
                        // Its 2 byte, use it
                        if (!buffer.AddByte(unchecked((byte)(iBytes >> 8)), unchecked((byte)(iBytes & 0xff))))
                            break;
                    }
                }
            }

            // Do we need to flush our charLeftOver?
            if ((encoder == null || encoder.MustFlush) && (charLeftOver > 0))
            {
                // Fall it back
                buffer.Fallback(charLeftOver);
                charLeftOver = (char)0;
                goto TryAgain;
            }

            // Fallback stuck it in encoder if necessary, but we have to clear MustFlash cases
            // (Check bytes != null, don't clear it if we're just counting)
            if (encoder != null)
            {
                // Remember our charLeftOver
                if (bytes != null)
                    encoder.charLeftOver = charLeftOver;

                encoder.m_charsUsed = buffer.CharsUsed;
            }

            // Return our length
            return buffer.Count;
        }

        // Helper methods
        internal bool IsGBLeadByte(short ch)
        {
            // return true if we're in the lead byte range
            return ((ch) >= 0x81 && (ch) <= 0xfe);
        }

        internal bool IsGBTwoByteTrailing(short ch)
        {
            // Return true if we are in range for the trailing byte of a 2 byte sequence
            return (((ch) >= 0x40 && (ch) <= 0x7e) ||
                    ((ch) >= 0x80 && (ch) <= 0xfe));
        }

        internal bool IsGBFourByteTrailing(short ch)
        {
            // Return true if we are in range for the trailing byte of a 4 byte sequence
            return ((ch) >= 0x30 && (ch) <= 0x39);
        }

        internal int GetFourBytesOffset(short offset1, short offset2, short offset3, short offset4)
        {
            return ((offset1 - 0x81) * 0x0a * 0x7e * 0x0a +
                    (offset2 - 0x30) * 0x7e * 0x0a +
                    (offset3 - 0x81) * 0x0a +
                     offset4 - 0x30);
        }

        // This is internal and called by something else,
        public override unsafe int GetCharCount(byte* bytes, int count, DecoderNLS? baseDecoder)
        {
            // Just call GetChars() with null chars to count
            return GetChars(bytes, count, null, 0, baseDecoder);
        }

        public override unsafe int GetChars(byte* bytes, int byteCount,
                                                char* chars, int charCount, DecoderNLS? baseDecoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            // We'll allow null chars as a count
            Debug.Assert(bytes != null, "[GB18030Encoding.GetChars]bytes is null");
            Debug.Assert(byteCount >= 0, "[GB18030Encoding.GetChars]byteCount is negative");
            //            Debug.Assert(chars != null, "[GB18030Encoding.GetChars]chars is null");
            Debug.Assert(charCount >= 0, "[GB18030Encoding.GetChars]charCount is negative");

            // Fix our decoder
            GB18030Decoder? decoder = (GB18030Decoder?)baseDecoder;

            // Get our info.
            EncodingCharBuffer buffer = new EncodingCharBuffer(this, decoder, chars, charCount, bytes, byteCount);

            // Need temp bytes because we can't muss up decoder
            short byte1 = -1;
            short byte2 = -1;
            short byte3 = -1;
            short byte4 = -1;

            // See if there was anything to get out of the decoder
            if (decoder != null && decoder.bLeftOver1 != -1)
            {
                // Need temp bytes because we can't muss up decoder
                byte1 = decoder.bLeftOver1;
                byte2 = decoder.bLeftOver2;
                byte3 = decoder.bLeftOver3;
                byte4 = decoder.bLeftOver4;

                // Loop because we might have too many in buffer
                // This could happen if we are working on a 4 byte sequence, but it isn't valid.
                while (byte1 != -1)
                {
                    // If its not a lead byte, use ? or its value, then scoot them down & try again
                    // This could happen if we previously had a bad 4 byte sequence and this is a trail byte
                    if (!IsGBLeadByte(byte1))
                    {
                        // This is either a ? or ASCII, need 1 char output
                        if (byte1 <= 0x7f)
                        {
                            if (!buffer.AddChar((char)byte1))      // Its ASCII
                                break;
                        }
                        else
                        {
                            if (!buffer.Fallback((byte)byte1))     // Not a valid byte
                                break;
                        }

                        byte1 = byte2;
                        byte2 = byte3;
                        byte3 = byte4;
                        byte4 = -1;
                        continue;
                    }

                    // Read in more bytes as needed
                    while (byte2 == -1 ||
                           (IsGBFourByteTrailing(byte2) && byte4 == -1))
                    {
                        // Do we have room?
                        if (!buffer.MoreData)
                        {
                            // No input left to read, do we have to flush?
                            if (!decoder.MustFlush)
                            {
                                // Don't stick stuff in decoder when counting
                                if (chars != null)
                                {
                                    // Don't have to flush, won't have any chars
                                    // Decoder is correct, just return
                                    decoder.bLeftOver1 = byte1;
                                    decoder.bLeftOver2 = byte2;
                                    decoder.bLeftOver3 = byte3;
                                    decoder.bLeftOver4 = byte4;
                                }

                                decoder.m_bytesUsed = buffer.BytesUsed;
                                return buffer.Count;
                            }

                            // We'll have to flush, add a ? and scoot them down to try again
                            // We could be trying for a 4 byte sequence but byte 3 could be ascii and should be spit out
                            // Breaking will do this because we have zeros
                            break;
                        }

                        // Read them in
                        if (byte2 == -1) byte2 = buffer.GetNextByte();
                        else if (byte3 == -1) byte3 = buffer.GetNextByte();
                        else byte4 = buffer.GetNextByte();
                    }

                    // Now we have our 2 or 4 bytes
                    if (IsGBTwoByteTrailing(byte2))
                    {
                        //
                        // The trailing byte is a GB18030 two-byte sequence trailing byte.
                        //
                        int iTwoBytes = byte1 << 8;
                        iTwoBytes |= unchecked((byte)byte2);
                        if (!buffer.AddChar(mapBytesToUnicode[iTwoBytes], 2))
                            break;

                        // We're done with it
                        byte1 = -1;
                        byte2 = -1;
                    }
                    else if (IsGBFourByteTrailing(byte2) &&
                             IsGBLeadByte(byte3) &&
                             IsGBFourByteTrailing(byte4))
                    {
                        //
                        // Four-byte GB18030
                        //

                        int sFourBytesOffset = GetFourBytesOffset(
                            byte1, byte2, byte3, byte4);

                        // What kind is it?
                        if (sFourBytesOffset <= GBLast4ByteCode)
                        {
                            //
                            // The Unicode will be in the BMP range.
                            //
                            if (!buffer.AddChar(map4BytesToUnicode[sFourBytesOffset], 4))
                                break;
                        }
                        else if (sFourBytesOffset >= GBSurrogateOffset &&
                                 sFourBytesOffset <= GBLastSurrogateOffset)
                        {
                            //
                            // This will be converted to a surrogate pair, need another char
                            //

                            // Use our surrogate
                            sFourBytesOffset -= GBSurrogateOffset;
                            if (!buffer.AddChar(unchecked((char)(0xd800 + (sFourBytesOffset / 0x400))),
                                                unchecked((char)(0xdc00 + (sFourBytesOffset % 0x400))), 4))
                                break;
                        }
                        else
                        {
                            // Real GB18030 codepoint, but can't be mapped to unicode
                            // We already checked our buffer space.
                            // Do fallback here if we implement decoderfallbacks.
                            if (!buffer.Fallback((byte)byte1, (byte)byte2, (byte)byte3, (byte)byte4))
                                break;
                        }

                        // We're done with this one
                        byte1 = -1;
                        byte2 = -1;
                        byte3 = -1;
                        byte4 = -1;
                    }
                    else
                    {
                        // Not a valid sequence, use '?' for 1st byte & scoot them all down 1
                        if (!buffer.Fallback((byte)byte1))
                            break;

                        // Move all bytes down 1
                        byte1 = byte2;
                        byte2 = byte3;
                        byte3 = byte4;
                        byte4 = -1;
                    }
                }
            }

            // Loop, just do '?' replacement because we don't have fallbacks for decodings.
            while (buffer.MoreData)
            {
                byte ch = buffer.GetNextByte();

                // ASCII case is easy
                if (ch <= 0x7f)
                {
                    // ASCII, have room?
                    if (!buffer.AddChar((char)ch))
                        break;              // No room in convert buffer, so stop
                }
                // See if its a lead byte
                else if (IsGBLeadByte(ch))
                {
                    // ch is a lead byte, have room for more?
                    if (buffer.MoreData)
                    {
                        byte ch2 = buffer.GetNextByte();
                        if (IsGBTwoByteTrailing(ch2))
                        {
                            //
                            // The trailing byte is a GB18030 two-byte sequence trailing byte.
                            //

                            //
                            // Two-byte GB18030
                            //
                            int iTwoBytes = ch << 8;
                            iTwoBytes |= ch2;
                            if (!buffer.AddChar(mapBytesToUnicode[iTwoBytes], 2))
                                break;
                        }
                        else if (IsGBFourByteTrailing(ch2))
                        {
                            // Do we have room for Four Byte Sequence? (already have 1 byte)
                            if (buffer.EvenMoreData(2))
                            {
                                // Is it a valid 4 byte sequence?
                                byte ch3 = buffer.GetNextByte();
                                byte ch4 = buffer.GetNextByte();
                                if (IsGBLeadByte(ch3) &&
                                    IsGBFourByteTrailing(ch4))
                                {
                                    //
                                    // Four-byte GB18030
                                    //
                                    int sFourBytesOffset = GetFourBytesOffset(ch, ch2, ch3, ch4);

                                    // What kind is it?
                                    // We'll be at least 1 BMP char or a '?' char.

                                    if (sFourBytesOffset <= GBLast4ByteCode)
                                    {
                                        //
                                        // The Unicode will be in the BMP range.
                                        //
                                        if (!buffer.AddChar(map4BytesToUnicode[sFourBytesOffset], 4))
                                            break;
                                    }
                                    else if (sFourBytesOffset >= GBSurrogateOffset &&
                                             sFourBytesOffset <= GBLastSurrogateOffset)
                                    {
                                        //
                                        // This will be converted to a surrogate pair, need another char
                                        //

                                        // Use our surrogate
                                        sFourBytesOffset -= GBSurrogateOffset;
                                        if (!buffer.AddChar(unchecked((char)(0xd800 + (sFourBytesOffset / 0x400))),
                                                             unchecked((char)(0xdc00 + (sFourBytesOffset % 0x400))), 4))
                                            break;
                                    }
                                    else
                                    {
                                        // Real GB18030 codepoint, but can't be mapped to unicode
                                        if (!buffer.Fallback(ch, ch2, ch3, ch4))
                                            break;
                                    }
                                }
                                else
                                {
                                    // Not a valid 2 or 4 byte sequence, use '?' for ch and try other 3 again
                                    buffer.AdjustBytes(-3);
                                    if (!buffer.Fallback(ch))
                                        break;
                                }
                            }
                            else
                            {
                                // No room for 4 bytes, have 2 already, may be one more
                                // Lead byte but no place to stick it
                                if (decoder != null && !decoder.MustFlush)
                                {
                                    // (make sure not to set decoder if counting, so check chars)
                                    if (chars != null)
                                    {
                                        // We'll be able to stick the remainder in the decoder
                                        byte1 = ch;
                                        byte2 = ch2;

                                        if (buffer.MoreData)
                                            byte3 = buffer.GetNextByte();
                                        else
                                            byte3 = -1;

                                        byte4 = -1;
                                    }
                                    break;
                                }

                                // Won't go in decoder, we'll use '?' for it.
                                if (!buffer.Fallback(ch, ch2))
                                    break;
                            }
                        }
                        else
                        {
                            // Unknown byte sequence, fall back lead byte and try 2nd one again
                            buffer.AdjustBytes(-1);
                            if (!buffer.Fallback(ch))
                                break;
                        }
                    }
                    else
                    {
                        // Lead byte but don't know about trail byte
                        // (make sure not to set decoder if counting, so check bytes)
                        if (decoder != null && !decoder.MustFlush)
                        {
                            // We'll be able to stick it in the decoder
                            // (don't actually do it when counting though)
                            if (chars != null)
                            {
                                byte1 = ch;
                                byte2 = -1;
                                byte3 = -1;
                                byte4 = -1;
                            }
                            break;
                        }

                        if (!buffer.Fallback(ch))
                            break;
                    }
                }
                else
                {
                    // Not ASCII and not a lead byte, we'll use '?' for it if we have room
                    if (!buffer.Fallback(ch))
                        break;
                }
            }

            // Need to flush the decoder if necessary
            // (make sure not to set decoder if counting, so check bytes)
            if (decoder != null)
            {
                if (chars != null)
                {
                    decoder.bLeftOver1 = byte1;
                    decoder.bLeftOver2 = byte2;
                    decoder.bLeftOver3 = byte3;
                    decoder.bLeftOver4 = byte4;
                }
                decoder.m_bytesUsed = buffer.BytesUsed;
            }

            // Return the # of characters we found
            return buffer.Count;
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            // Characters would be # of characters + 1 in case high surrogate is ? * max fallback
            long byteCount = (long)charCount + 1;

            if (EncoderFallback.MaxCharCount > 1)
                byteCount *= EncoderFallback.MaxCharCount;

            // We could have 4 bytes for each char, no extra for surrogates because 18030 can do whole unicode range.
            byteCount *= 4;

            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ArgumentOutOfRange_GetByteCountOverflow);

            return (int)byteCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            // Just return length, we could have a single char for each byte + whatever extra our decoder could do to us.
            // If decoder is messed up it could spit out 3 ?s.
            long charCount = ((long)byteCount) + 3;

            // Take fallback size into consideration
            if (DecoderFallback.MaxCharCount > 1)
                charCount *= DecoderFallback.MaxCharCount;

            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.ArgumentOutOfRange_GetCharCountOverflow);

            return (int)charCount;
        }

        public override Decoder GetDecoder()
        {
            return new GB18030Decoder(this);
        }

        internal sealed class GB18030Decoder : DecoderNLS
        {
            internal short bLeftOver1 = -1;
            internal short bLeftOver2 = -1;
            internal short bLeftOver3 = -1;
            internal short bLeftOver4 = -1;

            internal GB18030Decoder(EncodingNLS encoding) : base(encoding)
            {
                // DecoderNLS Calls reset
            }

            public override void Reset()
            {
                bLeftOver1 = -1;
                bLeftOver2 = -1;
                bLeftOver3 = -1;
                bLeftOver4 = -1;
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();
            }

            // Anything left in our decoder?
            internal override bool HasState
            {
                get
                {
                    return (bLeftOver1 >= 0);
                }
            }
        }

        // tableUnicodeToGBDiffs
        //
        // This compressed data enumerates the differences between gb18030 and the 936 code page as follows:
        //
        // <count> & 0x8000 == 0x8000      The next count <count> characters are identical to 936 characters.
        // <count> & 0x8000 == 0x0000      The next count <count> characters are 4 byte gb18030 characters.
        // Except for:
        // <count> >= 0x9000 && <count> != 0xD1A6     This character is this 2 byte GB18030 value.
        //
        private readonly ushort[] _tableUnicodeToGBDiffs =
        {
            0x8080, // U+0000 - U+007F (  128 chars) use CP 936 conversion.
            0x0024, // U+0080 - U+00A3 (   36 chars) are GB18030 81 30 81 30 - 81 30 84 35 (offset 0000 - 0023)
            0x8001, // U+00A4 - U+00A4 (    1 chars) use CP 936 conversion.
            0x0002, // U+00A5 - U+00A6 (    2 chars) are GB18030 81 30 84 36 - 81 30 84 37 (offset 0024 - 0025)
            0x8002, // U+00A7 - U+00A8 (    2 chars) use CP 936 conversion.
            0x0007, // U+00A9 - U+00AF (    7 chars) are GB18030 81 30 84 38 - 81 30 85 34 (offset 0026 - 002C)
            0x8002, // U+00B0 - U+00B1 (    2 chars) use CP 936 conversion.
            0x0005, // U+00B2 - U+00B6 (    5 chars) are GB18030 81 30 85 35 - 81 30 85 39 (offset 002D - 0031)
            0x8001, // U+00B7 - U+00B7 (    1 chars) use CP 936 conversion.
            0x001F, // U+00B8 - U+00D6 (   31 chars) are GB18030 81 30 86 30 - 81 30 89 30 (offset 0032 - 0050)
            0x8001, // U+00D7 - U+00D7 (    1 chars) use CP 936 conversion.
            0x0008, // U+00D8 - U+00DF (    8 chars) are GB18030 81 30 89 31 - 81 30 89 38 (offset 0051 - 0058)
            0x8002, // U+00E0 - U+00E1 (    2 chars) use CP 936 conversion.
            0x0006, // U+00E2 - U+00E7 (    6 chars) are GB18030 81 30 89 39 - 81 30 8A 34 (offset 0059 - 005E)
            0x8003, // U+00E8 - U+00EA (    3 chars) use CP 936 conversion.
            0x0001, // U+00EB - U+00EB (    1 chars) are GB18030 81 30 8A 35 - 81 30 8A 35 (offset 005F - 005F)
            0x8002, // U+00EC - U+00ED (    2 chars) use CP 936 conversion.
            0x0004, // U+00EE - U+00F1 (    4 chars) are GB18030 81 30 8A 36 - 81 30 8A 39 (offset 0060 - 0063)
            0x8002, // U+00F2 - U+00F3 (    2 chars) use CP 936 conversion.
            0x0003, // U+00F4 - U+00F6 (    3 chars) are GB18030 81 30 8B 30 - 81 30 8B 32 (offset 0064 - 0066)
            0x8001, // U+00F7 - U+00F7 (    1 chars) use CP 936 conversion.
            0x0001, // U+00F8 - U+00F8 (    1 chars) are GB18030 81 30 8B 33 - 81 30 8B 33 (offset 0067 - 0067)
            0x8002, // U+00F9 - U+00FA (    2 chars) use CP 936 conversion.
            0x0001, // U+00FB - U+00FB (    1 chars) are GB18030 81 30 8B 34 - 81 30 8B 34 (offset 0068 - 0068)
            0x8001, // U+00FC - U+00FC (    1 chars) use CP 936 conversion.
            0x0004, // U+00FD - U+0100 (    4 chars) are GB18030 81 30 8B 35 - 81 30 8B 38 (offset 0069 - 006C)
            0x8001, // U+0101 - U+0101 (    1 chars) use CP 936 conversion.
            0x0011, // U+0102 - U+0112 (   17 chars) are GB18030 81 30 8B 39 - 81 30 8D 35 (offset 006D - 007D)
            0x8001, // U+0113 - U+0113 (    1 chars) use CP 936 conversion.
            0x0007, // U+0114 - U+011A (    7 chars) are GB18030 81 30 8D 36 - 81 30 8E 32 (offset 007E - 0084)
            0x8001, // U+011B - U+011B (    1 chars) use CP 936 conversion.
            0x000F, // U+011C - U+012A (   15 chars) are GB18030 81 30 8E 33 - 81 30 8F 37 (offset 0085 - 0093)
            0x8001, // U+012B - U+012B (    1 chars) use CP 936 conversion.
            0x0018, // U+012C - U+0143 (   24 chars) are GB18030 81 30 8F 38 - 81 30 92 31 (offset 0094 - 00AB)
            0x8001, // U+0144 - U+0144 (    1 chars) use CP 936 conversion.
            0x0003, // U+0145 - U+0147 (    3 chars) are GB18030 81 30 92 32 - 81 30 92 34 (offset 00AC - 00AE)
            0x8001, // U+0148 - U+0148 (    1 chars) use CP 936 conversion.
            0x0004, // U+0149 - U+014C (    4 chars) are GB18030 81 30 92 35 - 81 30 92 38 (offset 00AF - 00B2)
            0x8001, // U+014D - U+014D (    1 chars) use CP 936 conversion.
            0x001D, // U+014E - U+016A (   29 chars) are GB18030 81 30 92 39 - 81 30 95 37 (offset 00B3 - 00CF)
            0x8001, // U+016B - U+016B (    1 chars) use CP 936 conversion.
            0x0062, // U+016C - U+01CD (   98 chars) are GB18030 81 30 95 38 - 81 30 9F 35 (offset 00D0 - 0131)
            0x8001, // U+01CE - U+01CE (    1 chars) use CP 936 conversion.
            0x0001, // U+01CF - U+01CF (    1 chars) are GB18030 81 30 9F 36 - 81 30 9F 36 (offset 0132 - 0132)
            0x8001, // U+01D0 - U+01D0 (    1 chars) use CP 936 conversion.
            0x0001, // U+01D1 - U+01D1 (    1 chars) are GB18030 81 30 9F 37 - 81 30 9F 37 (offset 0133 - 0133)
            0x8001, // U+01D2 - U+01D2 (    1 chars) use CP 936 conversion.
            0x0001, // U+01D3 - U+01D3 (    1 chars) are GB18030 81 30 9F 38 - 81 30 9F 38 (offset 0134 - 0134)
            0x8001, // U+01D4 - U+01D4 (    1 chars) use CP 936 conversion.
            0x0001, // U+01D5 - U+01D5 (    1 chars) are GB18030 81 30 9F 39 - 81 30 9F 39 (offset 0135 - 0135)
            0x8001, // U+01D6 - U+01D6 (    1 chars) use CP 936 conversion.
            0x0001, // U+01D7 - U+01D7 (    1 chars) are GB18030 81 30 A0 30 - 81 30 A0 30 (offset 0136 - 0136)
            0x8001, // U+01D8 - U+01D8 (    1 chars) use CP 936 conversion.
            0x0001, // U+01D9 - U+01D9 (    1 chars) are GB18030 81 30 A0 31 - 81 30 A0 31 (offset 0137 - 0137)
            0x8001, // U+01DA - U+01DA (    1 chars) use CP 936 conversion.
            0x0001, // U+01DB - U+01DB (    1 chars) are GB18030 81 30 A0 32 - 81 30 A0 32 (offset 0138 - 0138)
            0x8001, // U+01DC - U+01DC (    1 chars) use CP 936 conversion.
            0x001C, // U+01DD - U+01F8 (   28 chars) are GB18030 81 30 A0 33 - 81 30 A3 30 (offset 0139 - 0154)
            0xA8BF, // U+01F9 is non-936 GB18030 value A8 BF.
            0x0057, // U+01FA - U+0250 (   87 chars) are GB18030 81 30 A3 31 - 81 30 AB 37 (offset 0155 - 01AB)
            0x8001, // U+0251 - U+0251 (    1 chars) use CP 936 conversion.
            0x000F, // U+0252 - U+0260 (   15 chars) are GB18030 81 30 AB 38 - 81 30 AD 32 (offset 01AC - 01BA)
            0x8001, // U+0261 - U+0261 (    1 chars) use CP 936 conversion.
            0x0065, // U+0262 - U+02C6 (  101 chars) are GB18030 81 30 AD 33 - 81 30 B7 33 (offset 01BB - 021F)
            0x8001, // U+02C7 - U+02C7 (    1 chars) use CP 936 conversion.
            0x0001, // U+02C8 - U+02C8 (    1 chars) are GB18030 81 30 B7 34 - 81 30 B7 34 (offset 0220 - 0220)
            0x8003, // U+02C9 - U+02CB (    3 chars) use CP 936 conversion.
            0x000D, // U+02CC - U+02D8 (   13 chars) are GB18030 81 30 B7 35 - 81 30 B8 37 (offset 0221 - 022D)
            0x8001, // U+02D9 - U+02D9 (    1 chars) use CP 936 conversion.
            0x00B7, // U+02DA - U+0390 (  183 chars) are GB18030 81 30 B8 38 - 81 30 CB 30 (offset 022E - 02E4)
            0x8011, // U+0391 - U+03A1 (   17 chars) use CP 936 conversion.
            0x0001, // U+03A2 - U+03A2 (    1 chars) are GB18030 81 30 CB 31 - 81 30 CB 31 (offset 02E5 - 02E5)
            0x8007, // U+03A3 - U+03A9 (    7 chars) use CP 936 conversion.
            0x0007, // U+03AA - U+03B0 (    7 chars) are GB18030 81 30 CB 32 - 81 30 CB 38 (offset 02E6 - 02EC)
            0x8011, // U+03B1 - U+03C1 (   17 chars) use CP 936 conversion.
            0x0001, // U+03C2 - U+03C2 (    1 chars) are GB18030 81 30 CB 39 - 81 30 CB 39 (offset 02ED - 02ED)
            0x8007, // U+03C3 - U+03C9 (    7 chars) use CP 936 conversion.
            0x0037, // U+03CA - U+0400 (   55 chars) are GB18030 81 30 CC 30 - 81 30 D1 34 (offset 02EE - 0324)
            0x8001, // U+0401 - U+0401 (    1 chars) use CP 936 conversion.
            0x000E, // U+0402 - U+040F (   14 chars) are GB18030 81 30 D1 35 - 81 30 D2 38 (offset 0325 - 0332)
            0x8040, // U+0410 - U+044F (   64 chars) use CP 936 conversion.
            0x0001, // U+0450 - U+0450 (    1 chars) are GB18030 81 30 D2 39 - 81 30 D2 39 (offset 0333 - 0333)
            0x8001, // U+0451 - U+0451 (    1 chars) use CP 936 conversion.
            0x1BBE, // U+0452 - U+200F ( 7102 chars) are GB18030 81 30 D3 30 - 81 36 A5 31 (offset 0334 - 1EF1)
            0x8001, // U+2010 - U+2010 (    1 chars) use CP 936 conversion.
            0x0002, // U+2011 - U+2012 (    2 chars) are GB18030 81 36 A5 32 - 81 36 A5 33 (offset 1EF2 - 1EF3)
            0x8004, // U+2013 - U+2016 (    4 chars) use CP 936 conversion.
            0x0001, // U+2017 - U+2017 (    1 chars) are GB18030 81 36 A5 34 - 81 36 A5 34 (offset 1EF4 - 1EF4)
            0x8002, // U+2018 - U+2019 (    2 chars) use CP 936 conversion.
            0x0002, // U+201A - U+201B (    2 chars) are GB18030 81 36 A5 35 - 81 36 A5 36 (offset 1EF5 - 1EF6)
            0x8002, // U+201C - U+201D (    2 chars) use CP 936 conversion.
            0x0007, // U+201E - U+2024 (    7 chars) are GB18030 81 36 A5 37 - 81 36 A6 33 (offset 1EF7 - 1EFD)
            0x8002, // U+2025 - U+2026 (    2 chars) use CP 936 conversion.
            0x0009, // U+2027 - U+202F (    9 chars) are GB18030 81 36 A6 34 - 81 36 A7 32 (offset 1EFE - 1F06)
            0x8001, // U+2030 - U+2030 (    1 chars) use CP 936 conversion.
            0x0001, // U+2031 - U+2031 (    1 chars) are GB18030 81 36 A7 33 - 81 36 A7 33 (offset 1F07 - 1F07)
            0x8002, // U+2032 - U+2033 (    2 chars) use CP 936 conversion.
            0x0001, // U+2034 - U+2034 (    1 chars) are GB18030 81 36 A7 34 - 81 36 A7 34 (offset 1F08 - 1F08)
            0x8001, // U+2035 - U+2035 (    1 chars) use CP 936 conversion.
            0x0005, // U+2036 - U+203A (    5 chars) are GB18030 81 36 A7 35 - 81 36 A7 39 (offset 1F09 - 1F0D)
            0x8001, // U+203B - U+203B (    1 chars) use CP 936 conversion.
            0x0070, // U+203C - U+20AB (  112 chars) are GB18030 81 36 A8 30 - 81 36 B3 31 (offset 1F0E - 1F7D)
            0xA2E3, // U+20AC is non-936 GB18030 value A2 E3.
            0x0056, // U+20AD - U+2102 (   86 chars) are GB18030 81 36 B3 32 - 81 36 BB 37 (offset 1F7E - 1FD3)
            0x8001, // U+2103 - U+2103 (    1 chars) use CP 936 conversion.
            0x0001, // U+2104 - U+2104 (    1 chars) are GB18030 81 36 BB 38 - 81 36 BB 38 (offset 1FD4 - 1FD4)
            0x8001, // U+2105 - U+2105 (    1 chars) use CP 936 conversion.
            0x0003, // U+2106 - U+2108 (    3 chars) are GB18030 81 36 BB 39 - 81 36 BC 31 (offset 1FD5 - 1FD7)
            0x8001, // U+2109 - U+2109 (    1 chars) use CP 936 conversion.
            0x000C, // U+210A - U+2115 (   12 chars) are GB18030 81 36 BC 32 - 81 36 BD 33 (offset 1FD8 - 1FE3)
            0x8001, // U+2116 - U+2116 (    1 chars) use CP 936 conversion.
            0x000A, // U+2117 - U+2120 (   10 chars) are GB18030 81 36 BD 34 - 81 36 BE 33 (offset 1FE4 - 1FED)
            0x8001, // U+2121 - U+2121 (    1 chars) use CP 936 conversion.
            0x003E, // U+2122 - U+215F (   62 chars) are GB18030 81 36 BE 34 - 81 36 C4 35 (offset 1FEE - 202B)
            0x800C, // U+2160 - U+216B (   12 chars) use CP 936 conversion.
            0x0004, // U+216C - U+216F (    4 chars) are GB18030 81 36 C4 36 - 81 36 C4 39 (offset 202C - 202F)
            0x800A, // U+2170 - U+2179 (   10 chars) use CP 936 conversion.
            0x0016, // U+217A - U+218F (   22 chars) are GB18030 81 36 C5 30 - 81 36 C7 31 (offset 2030 - 2045)
            0x8004, // U+2190 - U+2193 (    4 chars) use CP 936 conversion.
            0x0002, // U+2194 - U+2195 (    2 chars) are GB18030 81 36 C7 32 - 81 36 C7 33 (offset 2046 - 2047)
            0x8004, // U+2196 - U+2199 (    4 chars) use CP 936 conversion.
            0x006E, // U+219A - U+2207 (  110 chars) are GB18030 81 36 C7 34 - 81 36 D2 33 (offset 2048 - 20B5)
            0x8001, // U+2208 - U+2208 (    1 chars) use CP 936 conversion.
            0x0006, // U+2209 - U+220E (    6 chars) are GB18030 81 36 D2 34 - 81 36 D2 39 (offset 20B6 - 20BB)
            0x8001, // U+220F - U+220F (    1 chars) use CP 936 conversion.
            0x0001, // U+2210 - U+2210 (    1 chars) are GB18030 81 36 D3 30 - 81 36 D3 30 (offset 20BC - 20BC)
            0x8001, // U+2211 - U+2211 (    1 chars) use CP 936 conversion.
            0x0003, // U+2212 - U+2214 (    3 chars) are GB18030 81 36 D3 31 - 81 36 D3 33 (offset 20BD - 20BF)
            0x8001, // U+2215 - U+2215 (    1 chars) use CP 936 conversion.
            0x0004, // U+2216 - U+2219 (    4 chars) are GB18030 81 36 D3 34 - 81 36 D3 37 (offset 20C0 - 20C3)
            0x8001, // U+221A - U+221A (    1 chars) use CP 936 conversion.
            0x0002, // U+221B - U+221C (    2 chars) are GB18030 81 36 D3 38 - 81 36 D3 39 (offset 20C4 - 20C5)
            0x8004, // U+221D - U+2220 (    4 chars) use CP 936 conversion.
            0x0002, // U+2221 - U+2222 (    2 chars) are GB18030 81 36 D4 30 - 81 36 D4 31 (offset 20C6 - 20C7)
            0x8001, // U+2223 - U+2223 (    1 chars) use CP 936 conversion.
            0x0001, // U+2224 - U+2224 (    1 chars) are GB18030 81 36 D4 32 - 81 36 D4 32 (offset 20C8 - 20C8)
            0x8001, // U+2225 - U+2225 (    1 chars) use CP 936 conversion.
            0x0001, // U+2226 - U+2226 (    1 chars) are GB18030 81 36 D4 33 - 81 36 D4 33 (offset 20C9 - 20C9)
            0x8005, // U+2227 - U+222B (    5 chars) use CP 936 conversion.
            0x0002, // U+222C - U+222D (    2 chars) are GB18030 81 36 D4 34 - 81 36 D4 35 (offset 20CA - 20CB)
            0x8001, // U+222E - U+222E (    1 chars) use CP 936 conversion.
            0x0005, // U+222F - U+2233 (    5 chars) are GB18030 81 36 D4 36 - 81 36 D5 30 (offset 20CC - 20D0)
            0x8004, // U+2234 - U+2237 (    4 chars) use CP 936 conversion.
            0x0005, // U+2238 - U+223C (    5 chars) are GB18030 81 36 D5 31 - 81 36 D5 35 (offset 20D1 - 20D5)
            0x8001, // U+223D - U+223D (    1 chars) use CP 936 conversion.
            0x000A, // U+223E - U+2247 (   10 chars) are GB18030 81 36 D5 36 - 81 36 D6 35 (offset 20D6 - 20DF)
            0x8001, // U+2248 - U+2248 (    1 chars) use CP 936 conversion.
            0x0003, // U+2249 - U+224B (    3 chars) are GB18030 81 36 D6 36 - 81 36 D6 38 (offset 20E0 - 20E2)
            0x8001, // U+224C - U+224C (    1 chars) use CP 936 conversion.
            0x0005, // U+224D - U+2251 (    5 chars) are GB18030 81 36 D6 39 - 81 36 D7 33 (offset 20E3 - 20E7)
            0x8001, // U+2252 - U+2252 (    1 chars) use CP 936 conversion.
            0x000D, // U+2253 - U+225F (   13 chars) are GB18030 81 36 D7 34 - 81 36 D8 36 (offset 20E8 - 20F4)
            0x8002, // U+2260 - U+2261 (    2 chars) use CP 936 conversion.
            0x0002, // U+2262 - U+2263 (    2 chars) are GB18030 81 36 D8 37 - 81 36 D8 38 (offset 20F5 - 20F6)
            0x8004, // U+2264 - U+2267 (    4 chars) use CP 936 conversion.
            0x0006, // U+2268 - U+226D (    6 chars) are GB18030 81 36 D8 39 - 81 36 D9 34 (offset 20F7 - 20FC)
            0x8002, // U+226E - U+226F (    2 chars) use CP 936 conversion.
            0x0025, // U+2270 - U+2294 (   37 chars) are GB18030 81 36 D9 35 - 81 36 DD 31 (offset 20FD - 2121)
            0x8001, // U+2295 - U+2295 (    1 chars) use CP 936 conversion.
            0x0003, // U+2296 - U+2298 (    3 chars) are GB18030 81 36 DD 32 - 81 36 DD 34 (offset 2122 - 2124)
            0x8001, // U+2299 - U+2299 (    1 chars) use CP 936 conversion.
            0x000B, // U+229A - U+22A4 (   11 chars) are GB18030 81 36 DD 35 - 81 36 DE 35 (offset 2125 - 212F)
            0x8001, // U+22A5 - U+22A5 (    1 chars) use CP 936 conversion.
            0x0019, // U+22A6 - U+22BE (   25 chars) are GB18030 81 36 DE 36 - 81 36 E1 30 (offset 2130 - 2148)
            0x8001, // U+22BF - U+22BF (    1 chars) use CP 936 conversion.
            0x0052, // U+22C0 - U+2311 (   82 chars) are GB18030 81 36 E1 31 - 81 36 E9 32 (offset 2149 - 219A)
            0x8001, // U+2312 - U+2312 (    1 chars) use CP 936 conversion.
            0x014D, // U+2313 - U+245F (  333 chars) are GB18030 81 36 E9 33 - 81 37 8C 35 (offset 219B - 22E7)
            0x800A, // U+2460 - U+2469 (   10 chars) use CP 936 conversion.
            0x000A, // U+246A - U+2473 (   10 chars) are GB18030 81 37 8C 36 - 81 37 8D 35 (offset 22E8 - 22F1)
            0x8028, // U+2474 - U+249B (   40 chars) use CP 936 conversion.
            0x0064, // U+249C - U+24FF (  100 chars) are GB18030 81 37 8D 36 - 81 37 97 35 (offset 22F2 - 2355)
            0x804C, // U+2500 - U+254B (   76 chars) use CP 936 conversion.
            0x0004, // U+254C - U+254F (    4 chars) are GB18030 81 37 97 36 - 81 37 97 39 (offset 2356 - 2359)
            0x8024, // U+2550 - U+2573 (   36 chars) use CP 936 conversion.
            0x000D, // U+2574 - U+2580 (   13 chars) are GB18030 81 37 98 30 - 81 37 99 32 (offset 235A - 2366)
            0x800F, // U+2581 - U+258F (   15 chars) use CP 936 conversion.
            0x0003, // U+2590 - U+2592 (    3 chars) are GB18030 81 37 99 33 - 81 37 99 35 (offset 2367 - 2369)
            0x8003, // U+2593 - U+2595 (    3 chars) use CP 936 conversion.
            0x000A, // U+2596 - U+259F (   10 chars) are GB18030 81 37 99 36 - 81 37 9A 35 (offset 236A - 2373)
            0x8002, // U+25A0 - U+25A1 (    2 chars) use CP 936 conversion.
            0x0010, // U+25A2 - U+25B1 (   16 chars) are GB18030 81 37 9A 36 - 81 37 9C 31 (offset 2374 - 2383)
            0x8002, // U+25B2 - U+25B3 (    2 chars) use CP 936 conversion.
            0x0008, // U+25B4 - U+25BB (    8 chars) are GB18030 81 37 9C 32 - 81 37 9C 39 (offset 2384 - 238B)
            0x8002, // U+25BC - U+25BD (    2 chars) use CP 936 conversion.
            0x0008, // U+25BE - U+25C5 (    8 chars) are GB18030 81 37 9D 30 - 81 37 9D 37 (offset 238C - 2393)
            0x8002, // U+25C6 - U+25C7 (    2 chars) use CP 936 conversion.
            0x0003, // U+25C8 - U+25CA (    3 chars) are GB18030 81 37 9D 38 - 81 37 9E 30 (offset 2394 - 2396)
            0x8001, // U+25CB - U+25CB (    1 chars) use CP 936 conversion.
            0x0002, // U+25CC - U+25CD (    2 chars) are GB18030 81 37 9E 31 - 81 37 9E 32 (offset 2397 - 2398)
            0x8002, // U+25CE - U+25CF (    2 chars) use CP 936 conversion.
            0x0012, // U+25D0 - U+25E1 (   18 chars) are GB18030 81 37 9E 33 - 81 37 A0 30 (offset 2399 - 23AA)
            0x8004, // U+25E2 - U+25E5 (    4 chars) use CP 936 conversion.
            0x001F, // U+25E6 - U+2604 (   31 chars) are GB18030 81 37 A0 31 - 81 37 A3 31 (offset 23AB - 23C9)
            0x8002, // U+2605 - U+2606 (    2 chars) use CP 936 conversion.
            0x0002, // U+2607 - U+2608 (    2 chars) are GB18030 81 37 A3 32 - 81 37 A3 33 (offset 23CA - 23CB)
            0x8001, // U+2609 - U+2609 (    1 chars) use CP 936 conversion.
            0x0036, // U+260A - U+263F (   54 chars) are GB18030 81 37 A3 34 - 81 37 A8 37 (offset 23CC - 2401)
            0x8001, // U+2640 - U+2640 (    1 chars) use CP 936 conversion.
            0x0001, // U+2641 - U+2641 (    1 chars) are GB18030 81 37 A8 38 - 81 37 A8 38 (offset 2402 - 2402)
            0x8001, // U+2642 - U+2642 (    1 chars) use CP 936 conversion.
            0x083E, // U+2643 - U+2E80 ( 2110 chars) are GB18030 81 37 A8 39 - 81 38 FD 38 (offset 2403 - 2C40)
            0xFE50, // U+2E81 is non-936 GB18030 value FE 50.
            0x0002, // U+2E82 - U+2E83 (    2 chars) are GB18030 81 38 FD 39 - 81 38 FE 30 (offset 2C41 - 2C42)
            0xFE54, // U+2E84 is non-936 GB18030 value FE 54.
            0x0003, // U+2E85 - U+2E87 (    3 chars) are GB18030 81 38 FE 31 - 81 38 FE 33 (offset 2C43 - 2C45)
            0xFE57, // U+2E88 is non-936 GB18030 value FE 57.
            0x0002, // U+2E89 - U+2E8A (    2 chars) are GB18030 81 38 FE 34 - 81 38 FE 35 (offset 2C46 - 2C47)
            0xFE58, // U+2E8B is non-936 GB18030 value FE 58.
            0xFE5D, // U+2E8C is non-936 GB18030 value FE 5D.
            0x000A, // U+2E8D - U+2E96 (   10 chars) are GB18030 81 38 FE 36 - 81 39 81 35 (offset 2C48 - 2C51)
            0xFE5E, // U+2E97 is non-936 GB18030 value FE 5E.
            0x000F, // U+2E98 - U+2EA6 (   15 chars) are GB18030 81 39 81 36 - 81 39 83 30 (offset 2C52 - 2C60)
            0xFE6B, // U+2EA7 is non-936 GB18030 value FE 6B.
            0x0002, // U+2EA8 - U+2EA9 (    2 chars) are GB18030 81 39 83 31 - 81 39 83 32 (offset 2C61 - 2C62)
            0xFE6E, // U+2EAA is non-936 GB18030 value FE 6E.
            0x0003, // U+2EAB - U+2EAD (    3 chars) are GB18030 81 39 83 33 - 81 39 83 35 (offset 2C63 - 2C65)
            0xFE71, // U+2EAE is non-936 GB18030 value FE 71.
            0x0004, // U+2EAF - U+2EB2 (    4 chars) are GB18030 81 39 83 36 - 81 39 83 39 (offset 2C66 - 2C69)
            0xFE73, // U+2EB3 is non-936 GB18030 value FE 73.
            0x0002, // U+2EB4 - U+2EB5 (    2 chars) are GB18030 81 39 84 30 - 81 39 84 31 (offset 2C6A - 2C6B)
            0xFE74, // U+2EB6 is non-936 GB18030 value FE 74.
            0xFE75, // U+2EB7 is non-936 GB18030 value FE 75.
            0x0003, // U+2EB8 - U+2EBA (    3 chars) are GB18030 81 39 84 32 - 81 39 84 34 (offset 2C6C - 2C6E)
            0xFE79, // U+2EBB is non-936 GB18030 value FE 79.
            0x000E, // U+2EBC - U+2EC9 (   14 chars) are GB18030 81 39 84 35 - 81 39 85 38 (offset 2C6F - 2C7C)
            0xFE84, // U+2ECA is non-936 GB18030 value FE 84.
            0x0125, // U+2ECB - U+2FEF (  293 chars) are GB18030 81 39 85 39 - 81 39 A3 31 (offset 2C7D - 2DA1)
            0xA98A, // U+2FF0 is non-936 GB18030 value A9 8A.
            0xA98B, // U+2FF1 is non-936 GB18030 value A9 8B.
            0xA98C, // U+2FF2 is non-936 GB18030 value A9 8C.
            0xA98D, // U+2FF3 is non-936 GB18030 value A9 8D.
            0xA98E, // U+2FF4 is non-936 GB18030 value A9 8E.
            0xA98F, // U+2FF5 is non-936 GB18030 value A9 8F.
            0xA990, // U+2FF6 is non-936 GB18030 value A9 90.
            0xA991, // U+2FF7 is non-936 GB18030 value A9 91.
            0xA992, // U+2FF8 is non-936 GB18030 value A9 92.
            0xA993, // U+2FF9 is non-936 GB18030 value A9 93.
            0xA994, // U+2FFA is non-936 GB18030 value A9 94.
            0xA995, // U+2FFB is non-936 GB18030 value A9 95.
            0x0004, // U+2FFC - U+2FFF (    4 chars) are GB18030 81 39 A3 32 - 81 39 A3 35 (offset 2DA2 - 2DA5)
            0x8004, // U+3000 - U+3003 (    4 chars) use CP 936 conversion.
            0x0001, // U+3004 - U+3004 (    1 chars) are GB18030 81 39 A3 36 - 81 39 A3 36 (offset 2DA6 - 2DA6)
            0x8013, // U+3005 - U+3017 (   19 chars) use CP 936 conversion.
            0x0005, // U+3018 - U+301C (    5 chars) are GB18030 81 39 A3 37 - 81 39 A4 31 (offset 2DA7 - 2DAB)
            0x8002, // U+301D - U+301E (    2 chars) use CP 936 conversion.
            0x0002, // U+301F - U+3020 (    2 chars) are GB18030 81 39 A4 32 - 81 39 A4 33 (offset 2DAC - 2DAD)
            0x8009, // U+3021 - U+3029 (    9 chars) use CP 936 conversion.
            0x0014, // U+302A - U+303D (   20 chars) are GB18030 81 39 A4 34 - 81 39 A6 33 (offset 2DAE - 2DC1)
            0xA989, // U+303E is non-936 GB18030 value A9 89.
            0x0002, // U+303F - U+3040 (    2 chars) are GB18030 81 39 A6 34 - 81 39 A6 35 (offset 2DC2 - 2DC3)
            0x8053, // U+3041 - U+3093 (   83 chars) use CP 936 conversion.
            0x0007, // U+3094 - U+309A (    7 chars) are GB18030 81 39 A6 36 - 81 39 A7 32 (offset 2DC4 - 2DCA)
            0x8004, // U+309B - U+309E (    4 chars) use CP 936 conversion.
            0x0002, // U+309F - U+30A0 (    2 chars) are GB18030 81 39 A7 33 - 81 39 A7 34 (offset 2DCB - 2DCC)
            0x8056, // U+30A1 - U+30F6 (   86 chars) use CP 936 conversion.
            0x0005, // U+30F7 - U+30FB (    5 chars) are GB18030 81 39 A7 35 - 81 39 A7 39 (offset 2DCD - 2DD1)
            0x8003, // U+30FC - U+30FE (    3 chars) use CP 936 conversion.
            0x0006, // U+30FF - U+3104 (    6 chars) are GB18030 81 39 A8 30 - 81 39 A8 35 (offset 2DD2 - 2DD7)
            0x8025, // U+3105 - U+3129 (   37 chars) use CP 936 conversion.
            0x00F6, // U+312A - U+321F (  246 chars) are GB18030 81 39 A8 36 - 81 39 C1 31 (offset 2DD8 - 2ECD)
            0x800A, // U+3220 - U+3229 (   10 chars) use CP 936 conversion.
            0x0007, // U+322A - U+3230 (    7 chars) are GB18030 81 39 C1 32 - 81 39 C1 38 (offset 2ECE - 2ED4)
            0x8001, // U+3231 - U+3231 (    1 chars) use CP 936 conversion.
            0x0071, // U+3232 - U+32A2 (  113 chars) are GB18030 81 39 C1 39 - 81 39 CD 31 (offset 2ED5 - 2F45)
            0x8001, // U+32A3 - U+32A3 (    1 chars) use CP 936 conversion.
            0x00EA, // U+32A4 - U+338D (  234 chars) are GB18030 81 39 CD 32 - 81 39 E4 35 (offset 2F46 - 302F)
            0x8002, // U+338E - U+338F (    2 chars) use CP 936 conversion.
            0x000C, // U+3390 - U+339B (   12 chars) are GB18030 81 39 E4 36 - 81 39 E5 37 (offset 3030 - 303B)
            0x8003, // U+339C - U+339E (    3 chars) use CP 936 conversion.
            0x0002, // U+339F - U+33A0 (    2 chars) are GB18030 81 39 E5 38 - 81 39 E5 39 (offset 303C - 303D)
            0x8001, // U+33A1 - U+33A1 (    1 chars) use CP 936 conversion.
            0x0022, // U+33A2 - U+33C3 (   34 chars) are GB18030 81 39 E6 30 - 81 39 E9 33 (offset 303E - 305F)
            0x8001, // U+33C4 - U+33C4 (    1 chars) use CP 936 conversion.
            0x0009, // U+33C5 - U+33CD (    9 chars) are GB18030 81 39 E9 34 - 81 39 EA 32 (offset 3060 - 3068)
            0x8001, // U+33CE - U+33CE (    1 chars) use CP 936 conversion.
            0x0002, // U+33CF - U+33D0 (    2 chars) are GB18030 81 39 EA 33 - 81 39 EA 34 (offset 3069 - 306A)
            0x8002, // U+33D1 - U+33D2 (    2 chars) use CP 936 conversion.
            0x0002, // U+33D3 - U+33D4 (    2 chars) are GB18030 81 39 EA 35 - 81 39 EA 36 (offset 306B - 306C)
            0x8001, // U+33D5 - U+33D5 (    1 chars) use CP 936 conversion.
            0x0071, // U+33D6 - U+3446 (  113 chars) are GB18030 81 39 EA 37 - 81 39 F5 39 (offset 306D - 30DD)
            0xFE56, // U+3447 is non-936 GB18030 value FE 56.
            0x002B, // U+3448 - U+3472 (   43 chars) are GB18030 81 39 F6 30 - 81 39 FA 32 (offset 30DE - 3108)
            0xFE55, // U+3473 is non-936 GB18030 value FE 55.
            0x012A, // U+3474 - U+359D (  298 chars) are GB18030 81 39 FA 33 - 82 30 9A 30 (offset 3109 - 3232)
            0xFE5A, // U+359E is non-936 GB18030 value FE 5A.
            0x006F, // U+359F - U+360D (  111 chars) are GB18030 82 30 9A 31 - 82 30 A5 31 (offset 3233 - 32A1)
            0xFE5C, // U+360E is non-936 GB18030 value FE 5C.
            0x000B, // U+360F - U+3619 (   11 chars) are GB18030 82 30 A5 32 - 82 30 A6 32 (offset 32A2 - 32AC)
            0xFE5B, // U+361A is non-936 GB18030 value FE 5B.
            0x02FD, // U+361B - U+3917 (  765 chars) are GB18030 82 30 A6 33 - 82 30 F2 37 (offset 32AD - 35A9)
            0xFE60, // U+3918 is non-936 GB18030 value FE 60.
            0x0055, // U+3919 - U+396D (   85 chars) are GB18030 82 30 F2 38 - 82 30 FB 32 (offset 35AA - 35FE)
            0xFE5F, // U+396E is non-936 GB18030 value FE 5F.
            0x0060, // U+396F - U+39CE (   96 chars) are GB18030 82 30 FB 33 - 82 31 86 38 (offset 35FF - 365E)
            0xFE62, // U+39CF is non-936 GB18030 value FE 62.
            0xFE65, // U+39D0 is non-936 GB18030 value FE 65.
            0x000E, // U+39D1 - U+39DE (   14 chars) are GB18030 82 31 86 39 - 82 31 88 32 (offset 365F - 366C)
            0xFE63, // U+39DF is non-936 GB18030 value FE 63.
            0x0093, // U+39E0 - U+3A72 (  147 chars) are GB18030 82 31 88 33 - 82 31 96 39 (offset 366D - 36FF)
            0xFE64, // U+3A73 is non-936 GB18030 value FE 64.
            0x00DA, // U+3A74 - U+3B4D (  218 chars) are GB18030 82 31 97 30 - 82 31 AC 37 (offset 3700 - 37D9)
            0xFE68, // U+3B4E is non-936 GB18030 value FE 68.
            0x011F, // U+3B4F - U+3C6D (  287 chars) are GB18030 82 31 AC 38 - 82 31 C9 34 (offset 37DA - 38F8)
            0xFE69, // U+3C6E is non-936 GB18030 value FE 69.
            0x0071, // U+3C6F - U+3CDF (  113 chars) are GB18030 82 31 C9 35 - 82 31 D4 37 (offset 38F9 - 3969)
            0xFE6A, // U+3CE0 is non-936 GB18030 value FE 6A.
            0x0375, // U+3CE1 - U+4055 (  885 chars) are GB18030 82 31 D4 38 - 82 32 AF 32 (offset 396A - 3CDE)
            0xFE6F, // U+4056 is non-936 GB18030 value FE 6F.
            0x0108, // U+4057 - U+415E (  264 chars) are GB18030 82 32 AF 33 - 82 32 C9 36 (offset 3CDF - 3DE6)
            0xFE70, // U+415F is non-936 GB18030 value FE 70.
            0x01D7, // U+4160 - U+4336 (  471 chars) are GB18030 82 32 C9 37 - 82 32 F8 37 (offset 3DE7 - 3FBD)
            0xFE72, // U+4337 is non-936 GB18030 value FE 72.
            0x0074, // U+4338 - U+43AB (  116 chars) are GB18030 82 32 F8 38 - 82 33 86 33 (offset 3FBE - 4031)
            0xFE78, // U+43AC is non-936 GB18030 value FE 78.
            0x0004, // U+43AD - U+43B0 (    4 chars) are GB18030 82 33 86 34 - 82 33 86 37 (offset 4032 - 4035)
            0xFE77, // U+43B1 is non-936 GB18030 value FE 77.
            0x002B, // U+43B2 - U+43DC (   43 chars) are GB18030 82 33 86 38 - 82 33 8B 30 (offset 4036 - 4060)
            0xFE7A, // U+43DD is non-936 GB18030 value FE 7A.
            0x00F8, // U+43DE - U+44D5 (  248 chars) are GB18030 82 33 8B 31 - 82 33 A3 38 (offset 4061 - 4158)
            0xFE7B, // U+44D6 is non-936 GB18030 value FE 7B.
            0x0175, // U+44D7 - U+464B (  373 chars) are GB18030 82 33 A3 39 - 82 33 C9 31 (offset 4159 - 42CD)
            0xFE7D, // U+464C is non-936 GB18030 value FE 7D.
            0x0014, // U+464D - U+4660 (   20 chars) are GB18030 82 33 C9 32 - 82 33 CB 31 (offset 42CE - 42E1)
            0xFE7C, // U+4661 is non-936 GB18030 value FE 7C.
            0x00C1, // U+4662 - U+4722 (  193 chars) are GB18030 82 33 CB 32 - 82 33 DE 34 (offset 42E2 - 43A2)
            0xFE80, // U+4723 is non-936 GB18030 value FE 80.
            0x0005, // U+4724 - U+4728 (    5 chars) are GB18030 82 33 DE 35 - 82 33 DE 39 (offset 43A3 - 43A7)
            0xFE81, // U+4729 is non-936 GB18030 value FE 81.
            0x0052, // U+472A - U+477B (   82 chars) are GB18030 82 33 DF 30 - 82 33 E7 31 (offset 43A8 - 43F9)
            0xFE82, // U+477C is non-936 GB18030 value FE 82.
            0x0010, // U+477D - U+478C (   16 chars) are GB18030 82 33 E7 32 - 82 33 E8 37 (offset 43FA - 4409)
            0xFE83, // U+478D is non-936 GB18030 value FE 83.
            0x01B9, // U+478E - U+4946 (  441 chars) are GB18030 82 33 E8 38 - 82 34 96 38 (offset 440A - 45C2)
            0xFE85, // U+4947 is non-936 GB18030 value FE 85.
            0x0032, // U+4948 - U+4979 (   50 chars) are GB18030 82 34 96 39 - 82 34 9B 38 (offset 45C3 - 45F4)
            0xFE86, // U+497A is non-936 GB18030 value FE 86.
            0x0002, // U+497B - U+497C (    2 chars) are GB18030 82 34 9B 39 - 82 34 9C 30 (offset 45F5 - 45F6)
            0xFE87, // U+497D is non-936 GB18030 value FE 87.
            0x0004, // U+497E - U+4981 (    4 chars) are GB18030 82 34 9C 31 - 82 34 9C 34 (offset 45F7 - 45FA)
            0xFE88, // U+4982 is non-936 GB18030 value FE 88.
            0xFE89, // U+4983 is non-936 GB18030 value FE 89.
            0x0001, // U+4984 - U+4984 (    1 chars) are GB18030 82 34 9C 35 - 82 34 9C 35 (offset 45FB - 45FB)
            0xFE8A, // U+4985 is non-936 GB18030 value FE 8A.
            0xFE8B, // U+4986 is non-936 GB18030 value FE 8B.
            0x0014, // U+4987 - U+499A (   20 chars) are GB18030 82 34 9C 36 - 82 34 9E 35 (offset 45FC - 460F)
            0xFE8D, // U+499B is non-936 GB18030 value FE 8D.
            0x0003, // U+499C - U+499E (    3 chars) are GB18030 82 34 9E 36 - 82 34 9E 38 (offset 4610 - 4612)
            0xFE8C, // U+499F is non-936 GB18030 value FE 8C.
            0x0016, // U+49A0 - U+49B5 (   22 chars) are GB18030 82 34 9E 39 - 82 34 A1 30 (offset 4613 - 4628)
            0xFE8F, // U+49B6 is non-936 GB18030 value FE 8F.
            0xFE8E, // U+49B7 is non-936 GB18030 value FE 8E.
            0x02BF, // U+49B8 - U+4C76 (  703 chars) are GB18030 82 34 A1 31 - 82 34 E7 33 (offset 4629 - 48E7)
            0xFE96, // U+4C77 is non-936 GB18030 value FE 96.
            0x0027, // U+4C78 - U+4C9E (   39 chars) are GB18030 82 34 E7 34 - 82 34 EB 32 (offset 48E8 - 490E)
            0xFE93, // U+4C9F is non-936 GB18030 value FE 93.
            0xFE94, // U+4CA0 is non-936 GB18030 value FE 94.
            0xFE95, // U+4CA1 is non-936 GB18030 value FE 95.
            0xFE97, // U+4CA2 is non-936 GB18030 value FE 97.
            0xFE92, // U+4CA3 is non-936 GB18030 value FE 92.
            0x006F, // U+4CA4 - U+4D12 (  111 chars) are GB18030 82 34 EB 33 - 82 34 F6 33 (offset 490F - 497D)
            0xFE98, // U+4D13 is non-936 GB18030 value FE 98.
            0xFE99, // U+4D14 is non-936 GB18030 value FE 99.
            0xFE9A, // U+4D15 is non-936 GB18030 value FE 9A.
            0xFE9B, // U+4D16 is non-936 GB18030 value FE 9B.
            0xFE9C, // U+4D17 is non-936 GB18030 value FE 9C.
            0xFE9D, // U+4D18 is non-936 GB18030 value FE 9D.
            0xFE9E, // U+4D19 is non-936 GB18030 value FE 9E.
            0x0094, // U+4D1A - U+4DAD (  148 chars) are GB18030 82 34 F6 34 - 82 35 87 31 (offset 497E - 4A11)
            0xFE9F, // U+4DAE is non-936 GB18030 value FE 9F.
            0x0051, // U+4DAF - U+4DFF (   81 chars) are GB18030 82 35 87 32 - 82 35 8F 32 (offset 4A12 - 4A62)
            0xD1A6, // U+4E00 - U+9FA5 (20902 chars) use CP 936 conversion.
            0x385A, // U+9FA6 - U+D7FF (14426 chars) are GB18030 82 35 8F 33 - 83 36 C7 38 (offset 4A63 - 82BC)
            0x8F6C, // U+D800 - U+E76B ( 3948 chars) use CP 936 conversion.
            0x0001, // U+E76C - U+E76C (    1 chars) are GB18030 83 36 C7 39 - 83 36 C7 39 (offset 82BD - 82BD)
            0x805B, // U+E76D - U+E7C7 (   91 chars) use CP 936 conversion.
            0x0001, // U+E7C8 - U+E7C8 (    1 chars) are GB18030 83 36 C8 30 - 83 36 C8 30 (offset 82BE - 82BE)
            0x801E, // U+E7C9 - U+E7E6 (   30 chars) use CP 936 conversion.
            0x000D, // U+E7E7 - U+E7F3 (   13 chars) are GB18030 83 36 C8 31 - 83 36 C9 33 (offset 82BF - 82CB)
            0x8021, // U+E7F4 - U+E814 (   33 chars) use CP 936 conversion.
            0x0001, // U+E815 - U+E815 (    1 chars) are GB18030 83 36 C9 34 - 83 36 C9 34 (offset 82CC - 82CC)
            0x8003, // U+E816 - U+E818 (    3 chars) use CP 936 conversion.
            0x0005, // U+E819 - U+E81D (    5 chars) are GB18030 83 36 C9 35 - 83 36 C9 39 (offset 82CD - 82D1)
            0x8001, // U+E81E - U+E81E (    1 chars) use CP 936 conversion.
            0x0007, // U+E81F - U+E825 (    7 chars) are GB18030 83 36 CA 30 - 83 36 CA 36 (offset 82D2 - 82D8)
            0x8001, // U+E826 - U+E826 (    1 chars) use CP 936 conversion.
            0x0004, // U+E827 - U+E82A (    4 chars) are GB18030 83 36 CA 37 - 83 36 CB 30 (offset 82D9 - 82DC)
            0x8002, // U+E82B - U+E82C (    2 chars) use CP 936 conversion.
            0x0004, // U+E82D - U+E830 (    4 chars) are GB18030 83 36 CB 31 - 83 36 CB 34 (offset 82DD - 82E0)
            0x8002, // U+E831 - U+E832 (    2 chars) use CP 936 conversion.
            0x0008, // U+E833 - U+E83A (    8 chars) are GB18030 83 36 CB 35 - 83 36 CC 32 (offset 82E1 - 82E8)
            0x8001, // U+E83B - U+E83B (    1 chars) use CP 936 conversion.
            0x0007, // U+E83C - U+E842 (    7 chars) are GB18030 83 36 CC 33 - 83 36 CC 39 (offset 82E9 - 82EF)
            0x8001, // U+E843 - U+E843 (    1 chars) use CP 936 conversion.
            0x0010, // U+E844 - U+E853 (   16 chars) are GB18030 83 36 CD 30 - 83 36 CE 35 (offset 82F0 - 82FF)
            0x8002, // U+E854 - U+E855 (    2 chars) use CP 936 conversion.
            0x000E, // U+E856 - U+E863 (   14 chars) are GB18030 83 36 CE 36 - 83 36 CF 39 (offset 8300 - 830D)
            0x8001, // U+E864 - U+E864 (    1 chars) use CP 936 conversion.
            0x10C7, // U+E865 - U+F92B ( 4295 chars) are GB18030 83 36 D0 30 - 84 30 85 34 (offset 830E - 93D4)
            0x8001, // U+F92C - U+F92C (    1 chars) use CP 936 conversion.
            0x004C, // U+F92D - U+F978 (   76 chars) are GB18030 84 30 85 35 - 84 30 8D 30 (offset 93D5 - 9420)
            0x8001, // U+F979 - U+F979 (    1 chars) use CP 936 conversion.
            0x001B, // U+F97A - U+F994 (   27 chars) are GB18030 84 30 8D 31 - 84 30 8F 37 (offset 9421 - 943B)
            0x8001, // U+F995 - U+F995 (    1 chars) use CP 936 conversion.
            0x0051, // U+F996 - U+F9E6 (   81 chars) are GB18030 84 30 8F 38 - 84 30 97 38 (offset 943C - 948C)
            0x8001, // U+F9E7 - U+F9E7 (    1 chars) use CP 936 conversion.
            0x0009, // U+F9E8 - U+F9F0 (    9 chars) are GB18030 84 30 97 39 - 84 30 98 37 (offset 948D - 9495)
            0x8001, // U+F9F1 - U+F9F1 (    1 chars) use CP 936 conversion.
            0x001A, // U+F9F2 - U+FA0B (   26 chars) are GB18030 84 30 98 38 - 84 30 9B 33 (offset 9496 - 94AF)
            0x8004, // U+FA0C - U+FA0F (    4 chars) use CP 936 conversion.
            0x0001, // U+FA10 - U+FA10 (    1 chars) are GB18030 84 30 9B 34 - 84 30 9B 34 (offset 94B0 - 94B0)
            0x8001, // U+FA11 - U+FA11 (    1 chars) use CP 936 conversion.
            0x0001, // U+FA12 - U+FA12 (    1 chars) are GB18030 84 30 9B 35 - 84 30 9B 35 (offset 94B1 - 94B1)
            0x8002, // U+FA13 - U+FA14 (    2 chars) use CP 936 conversion.
            0x0003, // U+FA15 - U+FA17 (    3 chars) are GB18030 84 30 9B 36 - 84 30 9B 38 (offset 94B2 - 94B4)
            0x8001, // U+FA18 - U+FA18 (    1 chars) use CP 936 conversion.
            0x0006, // U+FA19 - U+FA1E (    6 chars) are GB18030 84 30 9B 39 - 84 30 9C 34 (offset 94B5 - 94BA)
            0x8003, // U+FA1F - U+FA21 (    3 chars) use CP 936 conversion.
            0x0001, // U+FA22 - U+FA22 (    1 chars) are GB18030 84 30 9C 35 - 84 30 9C 35 (offset 94BB - 94BB)
            0x8002, // U+FA23 - U+FA24 (    2 chars) use CP 936 conversion.
            0x0002, // U+FA25 - U+FA26 (    2 chars) are GB18030 84 30 9C 36 - 84 30 9C 37 (offset 94BC - 94BD)
            0x8003, // U+FA27 - U+FA29 (    3 chars) use CP 936 conversion.
            0x0406, // U+FA2A - U+FE2F ( 1030 chars) are GB18030 84 30 9C 38 - 84 31 85 37 (offset 94BE - 98C3)
            0x8002, // U+FE30 - U+FE31 (    2 chars) use CP 936 conversion.
            0x0001, // U+FE32 - U+FE32 (    1 chars) are GB18030 84 31 85 38 - 84 31 85 38 (offset 98C4 - 98C4)
            0x8012, // U+FE33 - U+FE44 (   18 chars) use CP 936 conversion.
            0x0004, // U+FE45 - U+FE48 (    4 chars) are GB18030 84 31 85 39 - 84 31 86 32 (offset 98C5 - 98C8)
            0x800A, // U+FE49 - U+FE52 (   10 chars) use CP 936 conversion.
            0x0001, // U+FE53 - U+FE53 (    1 chars) are GB18030 84 31 86 33 - 84 31 86 33 (offset 98C9 - 98C9)
            0x8004, // U+FE54 - U+FE57 (    4 chars) use CP 936 conversion.
            0x0001, // U+FE58 - U+FE58 (    1 chars) are GB18030 84 31 86 34 - 84 31 86 34 (offset 98CA - 98CA)
            0x800E, // U+FE59 - U+FE66 (   14 chars) use CP 936 conversion.
            0x0001, // U+FE67 - U+FE67 (    1 chars) are GB18030 84 31 86 35 - 84 31 86 35 (offset 98CB - 98CB)
            0x8004, // U+FE68 - U+FE6B (    4 chars) use CP 936 conversion.
            0x0095, // U+FE6C - U+FF00 (  149 chars) are GB18030 84 31 86 36 - 84 31 95 34 (offset 98CC - 9960)
            0x805E, // U+FF01 - U+FF5E (   94 chars) use CP 936 conversion.
            0x0081, // U+FF5F - U+FFDF (  129 chars) are GB18030 84 31 95 35 - 84 31 A2 33 (offset 9961 - 99E1)
            0x8006, // U+FFE0 - U+FFE5 (    6 chars) use CP 936 conversion.
            0x001A, // U+FFE6 - U+FFFF (   26 chars) are GB18030 84 31 A2 34 - 84 31 A4 39 (offset 99E2 - 99FB)
        };
    }
}

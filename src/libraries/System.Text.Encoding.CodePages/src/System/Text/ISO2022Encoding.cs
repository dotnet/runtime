// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
//  Ported to managed code from c_is2022.c and related iso 2022 dll files from mlang
//
//  Abstract:
//
//      Managed implementation of ISO 2022 code pages, ported from the implementation in c_is2022.dll
//      This code should be kept in sync with the other implementations
//      This encoding wraps the basic encodings in code that adds the shift in/out wrapper methods
//
//  Notes:
//
// IsAlwaysNormalized ???
// Regarding Normalization for ISO-2022-JP (50220, 50221, 50222), its the same rules as EUCJP
//  Forms KC & KD are precluded because of things like halfwidth Katakana that has compatibility mappings
//  Form D is precluded because of 0x00a8, which changes to space + dieresis.
// 
// Note: I think that IsAlwaysNormalized should probably return true for form C for Japanese 20932 based CPs.
//
// For ISO-2022-KR
//  Never normalized, C & D (& therefore KC & KD) are precluded because of Hangul syllables and combined characters.
//
// IsAlwaysNormalized ???
// Regarding Normalization for ISO-2022-CN (50227, 50229) & HZ-GB2312 (52936) I think is similar to the Japanese case.
//  Forms KC & KD are precluded because of things like halfwidth Katakana that has compatibility mappings
//  Form D is precluded because of 0x00a8, which changes to space + dieresis.
//
// Note: I think that IsAlwaysNormalized should probably return true for form C for Chinese 20936 based CPs.
//

using System.Globalization;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using System;
using System.Security;
using System.Runtime.CompilerServices;

namespace System.Text
{
    /*=================================ISO2022Encoding============================
    **
    ** This is used to support ISO 2022 encodings that use shift/escape sequences.
    **
    ==============================================================================*/

    internal class ISO2022Encoding : DBCSCodePageEncoding
    {
        private const byte SHIFT_OUT = (byte)0x0E;
        private const byte SHIFT_IN = (byte)0x0F;
        private const byte ESCAPE = 0x1B;
        private const byte LEADBYTE_HALFWIDTH = 0x10;

        // We have to load the 936 code page tables, so impersonate 936 as our base
        // This pretends to be other code pages as far as memory sections are concerned.
        internal ISO2022Encoding(int codePage) : base(codePage, s_tableBaseCodePages[codePage % 10])
        {
        }

        private static int[] s_tableBaseCodePages =
        {
            932,    // 50220  ISO-2022-JP, No halfwidth Katakana, convert to full width
            932,    // 50221  ISO-2022-JP, Use escape sequence for half width Katakana
            932,    // 50222  ISO-2022-JP, Use shift-in/shift-out for half width Katakana
            0,
            0,
            949,    // 50225  ISO-2022-KR, Korean
            936,    // 52936  HZ-GB2312, 936 might be better source
            0, //20936,    // 50227  ISO-2022-CN, Note: This is just the same as CP 936 in Everett.
            0,
            // 50229 is currently unsupported, CP 20000 is currently not built in .nlp file
            0, //20000,    // 50229  ISO-2022-CN, ModeCNS11643_1
            0, //20000,    // 50229  ISO-2022-CN, ModeCNS11643_2
            0         //                     ModeASCII
        };

        internal enum ISO2022Modes
        {
            ModeHalfwidthKatakana = 0,
            ModeJIS0208 = 1,
            ModeKR = 5,
            ModeHZ = 6,
            ModeGB2312 = 7,
            ModeCNS11643_1 = 9,
            ModeCNS11643_2 = 10,
            ModeASCII = 11,

            ModeIncompleteEscape = -1,
            ModeInvalidEscape = -2,
            ModeNOOP = -3
        }

        // Clean up characters for ISO2022 code pages, etc.
        // ISO2022 (50220, 50221, 50222)
        // GB-HZ (52936)
        protected override bool CleanUpBytes(ref int bytes)
        {
            switch (CodePage)
            {
                // 932 based code pages
                case 50220:
                case 50221:
                case 50222:
                    {
                        if (bytes >= 0x100)
                        {
                            // map extended char (0xfa40-0xfc4b) to a special range
                            // (ported from mlang)
                            if (bytes >= 0xfa40 && bytes <= 0xfc4b)
                            {
                                if (bytes >= 0xfa40 && bytes <= 0xfa5b)
                                {
                                    if (bytes <= 0xfa49)
                                        bytes = bytes - 0x0b51;
                                    else if (bytes >= 0xfa4a && bytes <= 0xfa53)
                                        bytes = bytes - 0x072f6;
                                    else if (bytes >= 0xfa54 && bytes <= 0xfa57)
                                        bytes = bytes - 0x0b5b;
                                    else if (bytes == 0xfa58)
                                        bytes = 0x878a;
                                    else if (bytes == 0xfa59)
                                        bytes = 0x8782;
                                    else if (bytes == 0xfa5a)
                                        bytes = 0x8784;
                                    else if (bytes == 0xfa5b)
                                        bytes = 0x879a;
                                }
                                else if (bytes >= 0xfa5c && bytes <= 0xfc4b)
                                {
                                    byte tc = unchecked((byte)bytes);
                                    if (tc < 0x5c)
                                        bytes = bytes - 0x0d5f;
                                    else if (tc >= 0x80 && tc <= 0x9B)
                                        bytes = bytes - 0x0d1d;
                                    else
                                        bytes = bytes - 0x0d1c;
                                }
                            }

                            // Convert 932 code page to 20932 like code page range
                            // (also ported from mlang)
                            byte bLead = unchecked((byte)(bytes >> 8));
                            byte bTrail = unchecked((byte)bytes);

                            bLead -= ((bLead > (byte)0x9f) ? (byte)0xb1 : (byte)0x71);
                            bLead = (byte)((bLead << 1) + 1);
                            if (bTrail > (byte)0x9e)
                            {
                                bTrail -= (byte)0x7e;
                                bLead++;
                            }
                            else
                            {
                                if (bTrail > (byte)0x7e)
                                    bTrail--;
                                bTrail -= (byte)0x1f;
                            }

                            bytes = ((int)bLead) << 8 | (int)bTrail;
                            // Don't step out of our allocated lead byte area.
                            // All DBCS lead and trail bytes should be >= 0x21 and <= 0x7e
                            // This is commented out because Everett/Mlang had illegal PUA
                            // mappings to ISO2022 code pages that we're maintaining.
                            //                        if ((bytes & 0xFF00) < 0x2100 || (bytes & 0xFF00) > 0x7e00 ||
                            //                          (bytes & 0xFF) < 0x21 || (bytes & 0xFF) > 0x7e)
                            //                        return false;
                        }
                        else
                        {
                            // Adjust 1/2 Katakana
                            if (bytes >= 0xa1 && bytes <= 0xdf)
                                bytes += (LEADBYTE_HALFWIDTH << 8) - 0x80;

                            // 0x81-0x9f and 0xe0-0xfc CP 932
                            // 0x8e and 0xa1-0xfe      CP 20932 (we don't use 8e though)
                            // b0-df is 1/2 Katakana
                            if (bytes >= 0x81 &&
                                (bytes <= 0x9f ||
                                 (bytes >= 0xe0 && bytes <= 0xfc)))
                            {
                                // Don't do lead bytes, we use escape sequences instead.
                                return false;
                            }
                        }
                        break;
                    }
                case 50225:
                    {
                        // For 50225 since we don't rely on lead byte marks, return false and don't add them,
                        // esp. since we're only a 7 bit code page.
                        if (bytes >= 0x80 && bytes <= 0xff)
                            return false;

                        // Ignore characters out of range (a1-7f)
                        if (bytes >= 0x100 &&
                            ((bytes & 0xff) < 0xa1 || (bytes & 0xff) == 0xff ||
                             (bytes & 0xff00) < 0xa100 || (bytes & 0xff00) == 0xff00))
                            return false;

                        // May as well get them into our 7 bit range
                        bytes &= 0x7f7f;

                        break;
                    }
                case 52936:
                    {
                        // Since we don't rely on lead byte marks for 52936, get rid of them so we
                        // don't end up with extra weird fffe mappings.
                        if (bytes >= 0x81 && bytes <= 0xfe)
                            return false;

                        break;
                    }
            }

            return true;
        }

        // GetByteCount
        public override unsafe int GetByteCount(char* chars, int count, EncoderNLS baseEncoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            Debug.Assert(count >= 0, "[ISO2022Encoding.GetByteCount]count is negative");
            Debug.Assert(chars != null, "[ISO2022Encoding.GetByteCount]chars is null");

            // Just call GetBytes with null byte* to get count
            return GetBytes(chars, count, null, 0, baseEncoder);
        }

        public override unsafe int GetBytes(char* chars, int charCount,
                                                byte* bytes, int byteCount, EncoderNLS baseEncoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            Debug.Assert(chars != null, "[ISO2022Encoding.GetBytes]chars is null");
            Debug.Assert(byteCount >= 0, "[ISO2022Encoding.GetBytes]byteCount is negative");
            Debug.Assert(charCount >= 0, "[ISO2022Encoding.GetBytes]charCount is negative");

            // Assert because we shouldn't be able to have a null encoder.
            Debug.Assert(EncoderFallback != null, "[ISO2022Encoding.GetBytes]Attempting to use null encoder fallback");

            // Fix our encoder
            ISO2022Encoder encoder = (ISO2022Encoder)baseEncoder;

            // Our return value
            int iCount = 0;

            switch (CodePage)
            {
                case 50220:
                case 50221:
                case 50222:
                    iCount = GetBytesCP5022xJP(chars, charCount, bytes, byteCount, encoder);
                    break;
                case 50225:
                    iCount = GetBytesCP50225KR(chars, charCount, bytes, byteCount, encoder);
                    break;
                // Everett had 50227 the same as 936
                /*              case 50227:
                                    iCount = GetBytesCP50227CN( chars, charCount, bytes, byteCount, encoder );
                                    break;
                */
                case 52936:
                    iCount = GetBytesCP52936(chars, charCount, bytes, byteCount, encoder);
                    break;
            }

            return iCount;
        }

        // This is internal and called by something else,
        public override unsafe int GetCharCount(byte* bytes, int count, DecoderNLS baseDecoder)
        {
            // Just assert, we're called internally so these should be safe, checked already
            Debug.Assert(bytes != null, "[ISO2022Encoding.GetCharCount]bytes is null");
            Debug.Assert(count >= 0, "[ISO2022Encoding.GetCharCount]byteCount is negative");

            // Just call getChars with null char* to get count
            return GetChars(bytes, count, null, 0, baseDecoder);
        }

        public override unsafe int GetChars(byte* bytes, int byteCount,
                                                char* chars, int charCount, DecoderNLS baseDecoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            Debug.Assert(bytes != null, "[ISO2022Encoding.GetChars]bytes is null");
            Debug.Assert(byteCount >= 0, "[ISO2022Encoding.GetChars]byteCount is negative");
            Debug.Assert(charCount >= 0, "[ISO2022Encoding.GetChars]charCount is negative");

            // Fix our decoder
            ISO2022Decoder decoder = (ISO2022Decoder)baseDecoder;
            int iCount = 0;

            switch (CodePage)
            {
                case 50220:
                case 50221:
                case 50222:
                    iCount = GetCharsCP5022xJP(bytes, byteCount, chars, charCount, decoder);
                    break;
                case 50225:
                    iCount = GetCharsCP50225KR(bytes, byteCount, chars, charCount, decoder);
                    break;
                // Currently 50227 is the same as 936
                //                case 50227:
                //                  iCount = GetCharsCP50227CN( bytes, byteCount, chars, charCount, decoder);
                //                break;
                case 52936:
                    iCount = GetCharsCP52936(bytes, byteCount, chars, charCount, decoder);
                    break;
                default:
                    Debug.Fail("[ISO2022Encoding.GetChars] had unexpected code page");
                    break;
            }

            return iCount;
        }

        // ISO 2022 Code pages for JP.
        //  50220 - No halfwidth Katakana, convert to full width
        //  50221 - Use escape sequence for half width Katakana
        //  50222 - Use shift-in/shift-out for half width Katakana
        //
        // These are the JIS code pages, superset of ISO-2022 / ISO-2022-JP-1
        //  0E          Shift Out (following bytes are Katakana)
        //  0F          Shift In  (back to "normal" behavior)
        //  21-7E       Byte ranges (1 or 2 bytes)
        //  <ESC> $ @   To Double Byte 0208 Mode (actually older code page, but subset of 0208)
        //  <ESC> $ B   To Double Byte 0208 Mode (duplicate)
        //  <ESC> $ ( D To Double Byte 0212 Mode (previously we misinterpreted this)
        //  <ESC> $ I   To half width Katakana
        //  <ESC> ( J   To JIS-Roman
        //  <ESC> ( H   To JIS-Roman (swedish character set)
        //  <ESC> ( B   To ASCII
        //  <ESC> & @   Alternate lead in to <ESC> $ B so just ignore it.
        //
        // So in Katakana mode we add 0x8e as a lead byte and use CP 20932 to convert it
        // In ASCII mode we just spit out the single byte.
        // In Roman mode we should change 0x5c (\) -> Yen sign and 0x7e (~) to Overline, however
        //      we didn't in mLang, otherwise roman is like ASCII.
        // In 0208 double byte mode we have to |= with 0x8080 and use CP 20932 to convert it.
        // In 0212 double byte mode we have to |= with 0x8000 and use CP 20932 to convert it.
        //
        // Note that JIS Shift In/Shift Out is different than the other ISO2022 encodings.  For JIS
        // Shift out always shifts to half-width Katakana.  Chinese encodings use designator sequences
        // instead of escape sequences and shift out to the designated sequence or back in to ASCII.
        //
        // When decoding JIS 0208, MLang used a '*' (0x2a) character in JIS 0208 mode to map the trailing byte
        // to halfwidth katakana.  I found no description of that behavior, however that block of 0208 is
        // undefined, so we maintain that behavior when decoding.  We will never generate characters using
        // that technique, but the decoder will process them.
        //
        private unsafe int GetBytesCP5022xJP(char* chars, int charCount,
                                                  byte* bytes, int byteCount, ISO2022Encoder encoder)
        {
            // prepare our helpers
            EncodingByteBuffer buffer = new EncodingByteBuffer(this, encoder, bytes, byteCount, chars, charCount);

            // Get our mode
            ISO2022Modes currentMode = ISO2022Modes.ModeASCII;      // Mode
            ISO2022Modes shiftInMode = ISO2022Modes.ModeASCII;      // Mode that shift in will go back to (only used by CP 50222)

            // Check our encoder
            if (encoder != null)
            {
                char charLeftOver = encoder.charLeftOver;

                currentMode = encoder.currentMode;
                shiftInMode = encoder.shiftInOutMode;

                // We may have a left over character from last time, try and process it.
                if (charLeftOver > 0)
                {
                    Debug.Assert(char.IsHighSurrogate(charLeftOver), "[ISO2022Encoding.GetBytesCP5022xJP]leftover character should be high surrogate");

                    // It has to be a high surrogate, which we don't support, so it has to be a fallback
                    buffer.Fallback(charLeftOver);
                }
            }

            while (buffer.MoreData)
            {
                // Get our char
                char ch = buffer.GetNextChar();

                // Get our bytes
                ushort iBytes = mapUnicodeToBytes[ch];

            StartConvert:
                // Check for halfwidth bytes
                byte bLeadByte = (byte)(iBytes >> 8);
                byte bTrailByte = (byte)(iBytes & 0xff);

                if (bLeadByte == LEADBYTE_HALFWIDTH)
                {
                    // Its Halfwidth Katakana
                    if (CodePage == 50220)
                    {
                        // CodePage 50220 doesn't use halfwidth Katakana, convert to fullwidth
                        // See if its out of range, fallback if so, throws if recursive fallback
                        if (bTrailByte < 0x21 || bTrailByte >= 0x21 + s_HalfToFullWidthKanaTable.Length)
                        {
                            buffer.Fallback(ch);
                            continue;
                        }

                        // Get the full width katakana char to use.
                        iBytes = unchecked((ushort)(s_HalfToFullWidthKanaTable[bTrailByte - 0x21] & 0x7F7F));

                        // May have to do all sorts of fun stuff for mode, go back to start convert
                        goto StartConvert;
                    }

                    // Can use halfwidth Katakana, make sure we're in right mode

                    // Make sure we're in right mode
                    if (currentMode != ISO2022Modes.ModeHalfwidthKatakana)
                    {
                        // 50222 or 50221, either shift in/out or escape to get to Katakana mode
                        if (CodePage == 50222)
                        {
                            // Shift Out
                            if (!buffer.AddByte(SHIFT_OUT))
                                break;  // convert out of space, stop

                            // Don't change modes until after AddByte in case it fails for convert
                            // We get to shift out to Katakana, make sure we'll go back to the right mode
                            // (This ends up always being ASCII)
                            shiftInMode = currentMode;
                            currentMode = ISO2022Modes.ModeHalfwidthKatakana;
                        }
                        else
                        {
                            // 50221 does halfwidth katakana by escape sequence
                            Debug.Assert(CodePage == 50221, "[ISO2022Encoding.GetBytesCP5022xJP]Expected Code Page 50221");

                            // Add our escape sequence
                            if (!buffer.AddByte(ESCAPE, unchecked((byte)'('), unchecked((byte)'I')))
                                break;  // convert out of space, stop

                            currentMode = ISO2022Modes.ModeHalfwidthKatakana;
                        }
                    }

                    // We know we're in Katakana mode now, so add it.
                    // Go ahead and add the Katakana byte.  Our table tail bytes are 0x80 too big.
                    if (!buffer.AddByte(unchecked((byte)(bTrailByte & 0x7F))))
                        break;  // convert out of space, stop

                    // Done with this one
                    continue;
                }
                else if (bLeadByte != 0)
                {
                    //
                    //  It's a double byte character.
                    //

                    // If we're CP 50222 we may have to shift in from Katakana mode first
                    if (CodePage == 50222 && currentMode == ISO2022Modes.ModeHalfwidthKatakana)
                    {
                        // Shift In
                        if (!buffer.AddByte(SHIFT_IN))
                            break;    // convert out of space, stop

                        // Need to shift in from katakana.  (Still might not be right, but won't be shifted out anyway)
                        currentMode = shiftInMode;
                    }

                    // Make sure we're in the right mode (JIS 0208 or JIS 0212)
                    // Note: Right now we don't use JIS 0212.  Also this table would be wrong

                    // Its JIS extension 0208
                    if (currentMode != ISO2022Modes.ModeJIS0208)
                    {
                        // Escape sequence, we can fail after this, mode will be correct for convert
                        if (!buffer.AddByte(ESCAPE, unchecked((byte)'$'), unchecked((byte)'B')))
                            break;  // Convert out of space, stop

                        currentMode = ISO2022Modes.ModeJIS0208;
                    }

                    // Add our double bytes
                    if (!buffer.AddByte(unchecked((byte)(bLeadByte)), unchecked((byte)(bTrailByte))))
                        break; // Convert out of space, stop
                    continue;
                }
                else if (iBytes != 0 || ch == 0)
                {
                    // Single byte Char
                    // If we're CP 50222 we may have to shift in from Katakana mode first
                    if (CodePage == 50222 && currentMode == ISO2022Modes.ModeHalfwidthKatakana)
                    {
                        // Shift IN
                        if (!buffer.AddByte(SHIFT_IN))
                            break; // convert ran out of room

                        // Need to shift in from katakana.  (Still might not be right, but won't be shifted out anyway)
                        currentMode = shiftInMode;
                    }

                    // Its a single byte character, switch to ASCII if we have to
                    if (currentMode != ISO2022Modes.ModeASCII)
                    {
                        if (!buffer.AddByte(ESCAPE, unchecked((byte)'('), unchecked((byte)'B')))
                            break; // convert ran out of room

                        currentMode = ISO2022Modes.ModeASCII;
                    }

                    // Add the ASCII char
                    if (!buffer.AddByte(bTrailByte))
                        break; // convert had no room left
                    continue;
                }

                // Its unknown, do fallback, throws if recursive (knows because we called InternalGetNextChar)
                buffer.Fallback(ch);
            }

            // Switch back to ASCII if MustFlush or no encoder
            if (currentMode != ISO2022Modes.ModeASCII &&
                (encoder == null || encoder.MustFlush))
            {
                // If we're CP 50222 we may have to shift in from Katakana mode first
                if (CodePage == 50222 && currentMode == ISO2022Modes.ModeHalfwidthKatakana)
                {
                    // Shift IN, only shift mode if necessary.
                    if (buffer.AddByte(SHIFT_IN))
                        // Need to shift in from katakana.  (Still might not be right, but won't be shifted out anyway)
                        currentMode = shiftInMode;
                    else
                        // If not successful, convert will maintain state for next time, also
                        // AddByte will have decremented our char count, however we need it to remain the same
                        buffer.GetNextChar();
                }

                // switch back to ASCII to finish neatly
                if (currentMode != ISO2022Modes.ModeASCII &&
                    (CodePage != 50222 || currentMode != ISO2022Modes.ModeHalfwidthKatakana))
                {
                    // only shift if it was successful
                    if (buffer.AddByte(ESCAPE, unchecked((byte)'('), unchecked((byte)'B')))
                        currentMode = ISO2022Modes.ModeASCII;
                    else
                        // If not successful, convert will maintain state for next time, also
                        // AddByte will have decremented our char count, however we need it to remain the same
                        buffer.GetNextChar();
                }
            }

            // Remember our encoder state
            if (bytes != null && encoder != null)
            {
                // This is ASCII if we had to flush
                encoder.currentMode = currentMode;
                encoder.shiftInOutMode = shiftInMode;

                if (!buffer.fallbackBufferHelper.bUsedEncoder)
                {
                    encoder.charLeftOver = (char)0;
                }

                encoder.m_charsUsed = buffer.CharsUsed;
            }

            // Return our length
            return buffer.Count;
        }

        // ISO 2022 Code pages for Korean - CP 50225
        //
        // CP 50225 has Shift In/Shift Out codes, and a single designator sequence that is supposed
        // to appear once in the file, at the beginning of a line, before any multibyte code points.
        // So we stick the designator at the beginning of the output.
        //
        // These are the KR code page codes for ISO-2022-KR
        //  0E          Shift Out (following bytes are double byte)
        //  0F          Shift In  (back to ASCII behavior)
        //  21-7E       Byte ranges (1 or 2 bytes)
        //  <ESC> $)C   Double byte ISO-2022-KR designator
        //
        // Note that this encoding is a little different than other encodings.  The <esc>$)C sequence
        // should only appear once per file.  (Actually I saw another spec/rfc that said at the beginning
        // of each line, but it shouldn't really matter.)
        //
        // During decoding Mlang accepted ' ', '\t, and '\n' as their respective characters, even if
        // it was in double byte mode.  We maintain that behavior, although I couldn't find a reference or
        // reason for that behavior.  We never generate data using that shortcut.
        //
        // Also Mlang always assumed KR mode, even if the designator wasn't found yet, so we do that as
        // well.  So basically we just ignore <ESC>$)C when decoding.
        //
        private unsafe int GetBytesCP50225KR(char* chars, int charCount,
                                                    byte* bytes, int byteCount, ISO2022Encoder encoder)
        {
            // prepare our helpers
            EncodingByteBuffer buffer = new EncodingByteBuffer(this, encoder, bytes, byteCount, chars, charCount);

            // Get our mode
            ISO2022Modes currentMode = ISO2022Modes.ModeASCII;      // Mode
            ISO2022Modes shiftOutMode = ISO2022Modes.ModeASCII;     // ModeKR if already stamped lead bytes

            // Check our encoder
            if (encoder != null)
            {
                // May have leftover stuff
                char charLeftOver = encoder.charLeftOver;
                currentMode = encoder.currentMode;
                shiftOutMode = encoder.shiftInOutMode;

                // We may have a l left over character from last time, try and process it.
                if (charLeftOver > 0)
                {
                    Debug.Assert(char.IsHighSurrogate(charLeftOver), "[ISO2022Encoding.GetBytesCP50225KR]leftover character should be high surrogate");

                    // It has to be a high surrogate, which we don't support, so it has to be a fallback
                    buffer.Fallback(charLeftOver);
                }
            }

            while (buffer.MoreData)
            {
                // Get our data
                char ch = buffer.GetNextChar();

                // Get our bytes
                ushort iBytes = mapUnicodeToBytes[ch];

                // Check for double byte bytes
                byte bLeadByte = (byte)(iBytes >> 8);
                byte bTrailByte = (byte)(iBytes & 0xff);

                if (bLeadByte != 0)
                {
                    //
                    //  It's a double byte character.
                    //

                    // If we haven't done our Korean designator, then do so, if we have any input
                    if (shiftOutMode != ISO2022Modes.ModeKR)
                    {
                        // Add our code page designator sequence
                        if (!buffer.AddByte(ESCAPE, unchecked((byte)'$'), unchecked((byte)')'), unchecked((byte)'C')))
                            break; // No room during convert.

                        shiftOutMode = ISO2022Modes.ModeKR;
                    }

                    // May have to switch to ModeKR first
                    if (currentMode != ISO2022Modes.ModeKR)
                    {
                        if (!buffer.AddByte(SHIFT_OUT))
                            break; // No convert room

                        currentMode = ISO2022Modes.ModeKR;
                    }

                    // Add the bytes
                    if (!buffer.AddByte(bLeadByte, bTrailByte))
                        break; // no convert room
                    continue;
                }
                else if (iBytes != 0 || ch == 0)
                {
                    // Its a single byte character, switch to ASCII if we have to
                    if (currentMode != ISO2022Modes.ModeASCII)
                    {
                        if (!buffer.AddByte(SHIFT_IN))
                            break;

                        currentMode = ISO2022Modes.ModeASCII;
                    }

                    // Add the ASCII char
                    if (!buffer.AddByte(bTrailByte))
                        break;
                    continue;
                }

                // Its unknown, do fallback, throws if recursive (knows because we called InternalGetNextChar)
                buffer.Fallback(ch);
            }

            // Switch back to ASCII if MustFlush or no encoder
            if (currentMode != ISO2022Modes.ModeASCII &&
                (encoder == null || encoder.MustFlush))
            {
                // Get back to ASCII to be safe.  Only do it if it success.
                if (buffer.AddByte(SHIFT_IN))
                    currentMode = ISO2022Modes.ModeASCII;
                else
                    // If not successful, convert will maintain state for next time, also
                    // AddByte will have decremented our char count, however we need it to remain the same
                    buffer.GetNextChar();
            }

            // Remember our encoder state
            if (bytes != null && encoder != null)
            {
                // If we didn't use the encoder, then there's no chars left over
                if (!buffer.fallbackBufferHelper.bUsedEncoder)
                {
                    encoder.charLeftOver = (char)0;
                }

                // This is ASCII if we had to flush
                encoder.currentMode = currentMode;

                // We don't use shift out mode, but if we've flushed we need to reset it so it doesn't 
                // get output again.
                if (!encoder.MustFlush || encoder.charLeftOver != (char)0)
                {
                    // We should be not flushing or converting
                    Debug.Assert(!encoder.MustFlush || !encoder.m_throwOnOverflow,
                        "[ISO2022Encoding.GetBytesCP50225KR]Expected no left over data or not flushing or not converting");
                    encoder.shiftInOutMode = shiftOutMode;
                }
                else
                    encoder.shiftInOutMode = ISO2022Modes.ModeASCII;

                encoder.m_charsUsed = buffer.CharsUsed;
            }

            // Return our length
            return buffer.Count;
        }

        // CP52936 is HZ Encoding
        // HZ Encoding has 4 shift sequences:
        // ~~       '~' (\u7e)
        // ~}       shift into 1 byte mode,
        // ~{       shift into 2 byte GB 2312-80
        // ~<NL>    Maintain 2 byte mode across new lines (ignore both ~ and <NL> characters)
        //          (This is for mailers that restrict to 70 or 80 or whatever character lines)
        //
        // According to comment in mlang, lead & trail byte ranges are described in RFC 1843
        // RFC 1843 => valid HZ code range: leading byte 0x21 - 0x77, 2nd byte 0x21 - 0x7e
        // Our 936 code points are or'd with 0x8080, so lead byte 0xa1 - 0xf7, trail byte 0xa1 - 0xfe
        //
        // This encoding is designed for transmission by e-mail and news.  No bytes should have high bit set.
        // (all bytes <= 0x7f)
        private unsafe int GetBytesCP52936(char* chars, int charCount,
                                           byte* bytes, int byteCount, ISO2022Encoder encoder)
        {
            // prepare our helpers
            EncodingByteBuffer buffer = new EncodingByteBuffer(this, encoder, bytes, byteCount, chars, charCount);

            // Mode
            ISO2022Modes currentMode = ISO2022Modes.ModeASCII;

            // Check our encoder
            if (encoder != null)
            {
                char charLeftOver = encoder.charLeftOver;
                currentMode = encoder.currentMode;

                // We may have a left over character from last time, try and process it.
                if (charLeftOver > 0)
                {
                    Debug.Assert(char.IsHighSurrogate(charLeftOver), "[ISO2022Encoding.GetBytesCP52936]leftover character should be high surrogate");

                    // It has to be a high surrogate, which we don't support, so it has to be a fallback
                    buffer.Fallback(charLeftOver);
                }
            }

            while (buffer.MoreData)
            {
                // Get our char
                char ch = buffer.GetNextChar();

                // Get our bytes
                ushort sChar = mapUnicodeToBytes[ch];
                if (sChar == 0 && ch != 0)
                {
                    // Wasn't a legal byte sequence, its a surrogate or fallback
                    // Throws if recursive (knows because we called InternalGetNextChar)
                    buffer.Fallback(ch);

                    // Done with our char, now process fallback
                    continue;
                }

                // Check for halfwidth bytes
                byte bLeadByte = (byte)(sChar >> 8);
                byte bTrailByte = (byte)(sChar & 0xff);

                // If its a double byte, it has to fit in the lead byte 0xa1 - 0xf7, trail byte 0xa1 - 0xfe range
                // (including the 0x8080 that our codepage or's to the value)
                if ((bLeadByte != 0 &&
                     (bLeadByte < 0xa1 || bLeadByte > 0xf7 || bTrailByte < 0xa1 || bTrailByte > 0xfe)) ||
                    (bLeadByte == 0 && bTrailByte > 0x80 && bTrailByte != 0xff))
                {
                    // Illegal character, in 936 code page, but not in HZ subset, get fallback for it
                    buffer.Fallback(ch);
                    continue;
                }

                // sChar is now either ASCII or has an 0x8080 mask
                if (bLeadByte != 0)
                {
                    // Its a double byte mode
                    if (currentMode != ISO2022Modes.ModeHZ)
                    {
                        // Need to add the double byte mode marker
                        if (!buffer.AddByte((byte)'~', (byte)'{', 2))
                            break;                                      // Stop if no buffer space in convert

                        currentMode = ISO2022Modes.ModeHZ;
                    }

                    // Go ahead and add the 2 bytes
                    if (!buffer.AddByte(unchecked((byte)(bLeadByte & 0x7f)), unchecked((byte)(bTrailByte & 0x7f))))
                        break;                                      // Stop if no buffer space in convert
                }
                else
                {
                    // Its supposed to be ASCII
                    if (currentMode != ISO2022Modes.ModeASCII)
                    {
                        // Need to add the ASCII mode marker
                        // Will have 1 more byte (or 2 if ~)
                        if (!buffer.AddByte((byte)'~', (byte)'}', bTrailByte == '~' ? 2 : 1))
                            break;

                        currentMode = ISO2022Modes.ModeASCII;
                    }

                    // If its a '~' we'll need an extra one
                    if (bTrailByte == '~')
                    {
                        // Need to add the extra ~
                        if (!buffer.AddByte((byte)'~', 1))
                            break;
                    }

                    // Need to add the character
                    if (!buffer.AddByte(bTrailByte))
                        break;
                }
            }

            // Add ASCII shift out if we're at end of decoder
            if (currentMode != ISO2022Modes.ModeASCII &&
                (encoder == null || encoder.MustFlush))
            {
                // Need to add the ASCII mode marker
                // Only turn off other mode if this works
                if (buffer.AddByte((byte)'~', (byte)'}'))
                    currentMode = ISO2022Modes.ModeASCII;
                else
                    // If not successful, convert will maintain state for next time, also
                    // AddByte will have decremented our char count, however we need it to remain the same
                    buffer.GetNextChar();
            }

            // Need to remember our mode
            if (encoder != null && bytes != null)
            {
                // This is ASCII if we had to flush
                encoder.currentMode = currentMode;

                if (!buffer.fallbackBufferHelper.bUsedEncoder)
                {
                    encoder.charLeftOver = (char)0;
                }

                encoder.m_charsUsed = buffer.CharsUsed;
            }

            // Return our length
            return buffer.Count;
        }

        private unsafe int GetCharsCP5022xJP(byte* bytes, int byteCount,
                                                  char* chars, int charCount, ISO2022Decoder decoder)
        {
            // Get our info.
            EncodingCharBuffer buffer = new EncodingCharBuffer(this, decoder, chars, charCount, bytes, byteCount);

            // No mode information yet
            ISO2022Modes currentMode = ISO2022Modes.ModeASCII;      // Our current Mode
            ISO2022Modes shiftInMode = ISO2022Modes.ModeASCII;      // Mode that we'll shift in to
            byte[] escapeBytes = new byte[4];
            int escapeCount = 0;

            if (decoder != null)
            {
                currentMode = decoder.currentMode;
                shiftInMode = decoder.shiftInOutMode;

                // See if we have leftover decoder buffer to use
                // Load our bytesLeftOver
                escapeCount = decoder.bytesLeftOverCount;

                // Don't want to mess up decoder if we're counting or throw an exception
                for (int i = 0; i < escapeCount; i++)
                    escapeBytes[i] = decoder.bytesLeftOver[i];
            }

            // Do this until the end
            while (buffer.MoreData || escapeCount > 0)
            {
                byte ch;

                if (escapeCount > 0)
                {
                    // Get more escape sequences if necessary
                    if (escapeBytes[0] == ESCAPE)
                    {
                        // Stop if no more input
                        if (!buffer.MoreData)
                        {
                            if (decoder != null && !decoder.MustFlush)
                                break;
                        }
                        else
                        {
                            // Add it to the sequence we can check
                            escapeBytes[escapeCount++] = buffer.GetNextByte();

                            // We have an escape sequence
                            ISO2022Modes modeReturn =
                                CheckEscapeSequenceJP(escapeBytes, escapeCount);

                            if (modeReturn != ISO2022Modes.ModeInvalidEscape)
                            {
                                if (modeReturn != ISO2022Modes.ModeIncompleteEscape)
                                {
                                    // Processed escape correctly
                                    escapeCount = 0;

                                    // We're now this mode
                                    currentMode = shiftInMode = modeReturn;
                                }

                                // Either way, continue to get next escape or real byte
                                continue;
                            }
                        }
                        // If ModeInvalidEscape, or no input & must flush, then fall through to add escape.
                    }

                    // Read next escape byte and move them down one.
                    ch = DecrementEscapeBytes(ref escapeBytes, ref escapeCount);
                }
                else
                {
                    // Get our next byte
                    ch = buffer.GetNextByte();

                    if (ch == ESCAPE)
                    {
                        // We'll have an escape sequence, use it if we don't have one buffered already
                        if (escapeCount == 0)
                        {
                            // Start this new escape sequence
                            escapeBytes[0] = ch;
                            escapeCount = 1;
                            continue;
                        }

                        // Flush the previous escape sequence, then reuse this escape byte
                        buffer.AdjustBytes(-1);
                    }
                }

                if (ch == SHIFT_OUT)
                {
                    shiftInMode = currentMode;
                    currentMode = ISO2022Modes.ModeHalfwidthKatakana;
                    continue;
                }
                else if (ch == SHIFT_IN)
                {
                    currentMode = shiftInMode;
                    continue;
                }

                // Get our full character
                ushort iBytes = ch;
                bool b2Bytes = false;

                if (currentMode == ISO2022Modes.ModeJIS0208)
                {
                    //
                    //  To handle errors, we need to check:
                    //    1. if trailbyte is there
                    //    2. if code is valid
                    //
                    if (escapeCount > 0)
                    {
                        // Let another escape fall through
                        if (escapeBytes[0] != ESCAPE)
                        {
                            // Move them down one & get the next data
                            iBytes <<= 8;
                            iBytes |= DecrementEscapeBytes(ref escapeBytes, ref escapeCount);
                            b2Bytes = true;
                        }
                    }
                    else if (buffer.MoreData)
                    {
                        iBytes <<= 8;
                        iBytes |= buffer.GetNextByte();
                        b2Bytes = true;
                    }
                    else
                    {
                        // Not enough input, use decoder if possible
                        if (decoder == null || decoder.MustFlush)
                        {
                            // No decoder, do fallback for this byte
                            buffer.Fallback(ch);
                            break;
                        }

                        // Stick it in the decoder if we're not counting
                        if (chars != null)
                        {
                            escapeBytes[0] = ch;
                            escapeCount = 1;
                        }
                        break;
                    }

                    // MLang treated JIS 0208 '*' lead byte like a single halfwidth katakana
                    // escape, so use 0x8e00 as katakana lead byte and keep same trail byte.
                    // 0x2a lead byte range is normally unused in JIS 0208, so shouldn't have
                    // any weird compatibility issues.
                    if ((b2Bytes == true) && ((iBytes & 0xff00) == 0x2a00))
                    {
                        iBytes = (ushort)(iBytes & 0xff);
                        iBytes |= (LEADBYTE_HALFWIDTH << 8);   // Put us in the halfwidth katakana range
                    }
                }
                else if (iBytes >= 0xA1 && iBytes <= 0xDF)
                {
                    // Everett accidentally mapped Katakana like shift-jis (932),
                    // even though this is a 7 bit code page.  We keep that mapping
                    iBytes |= (LEADBYTE_HALFWIDTH << 8);    // Map to halfwidth katakana range
                    iBytes &= 0xff7f;                       // remove extra 0x80
                }
                else if (currentMode == ISO2022Modes.ModeHalfwidthKatakana)
                {
                    // Add 0x10 lead byte that our encoding expects for Katakana:
                    iBytes |= (LEADBYTE_HALFWIDTH << 8);
                }

                // We have an iBytes to try to convert.
                char c = mapBytesToUnicode[iBytes];

                // See if it was unknown
                if (c == UNKNOWN_CHAR_FLAG && iBytes != 0)
                {
                    // Have to do fallback
                    if (b2Bytes)
                    {
                        if (!buffer.Fallback((byte)(iBytes >> 8), (byte)iBytes))
                            break;
                    }
                    else
                    {
                        if (!buffer.Fallback(ch))
                            break;
                    }
                }
                else
                {
                    // If we were JIS 0208, then we consumed an extra byte
                    if (!buffer.AddChar(c, b2Bytes ? 2 : 1))
                        break;
                }
            }

            // Make sure our decoder state matches our mode, if not counting
            if (chars != null && decoder != null)
            {
                // Remember it if we don't flush
                if (!decoder.MustFlush || escapeCount != 0)
                {
                    // Either not flushing or had state (from convert)
                    Debug.Assert(!decoder.MustFlush || !decoder.m_throwOnOverflow,
                        "[ISO2022Encoding.GetCharsCP5022xJP]Expected no state or not converting or not flushing");

                    decoder.currentMode = currentMode;
                    decoder.shiftInOutMode = shiftInMode;

                    // Remember escape buffer
                    decoder.bytesLeftOverCount = escapeCount;
                    decoder.bytesLeftOver = escapeBytes;
                }
                else
                {
                    // We flush, clear buffer
                    decoder.currentMode = ISO2022Modes.ModeASCII;
                    decoder.shiftInOutMode = ISO2022Modes.ModeASCII;
                    decoder.bytesLeftOverCount = 0;
                    // Slightly different if counting/not counting
                }

                decoder.m_bytesUsed = buffer.BytesUsed;
            }

            // Return # of characters we found
            return buffer.Count;
        }

        // We know we have an escape sequence, so check it starting with the byte after the escape
        private ISO2022Modes CheckEscapeSequenceJP(byte[] bytes, int escapeCount)
        {
            // Have an escape sequence
            if (bytes[0] != ESCAPE)
                return ISO2022Modes.ModeInvalidEscape;

            if (escapeCount < 3)
                return ISO2022Modes.ModeIncompleteEscape;

            if (bytes[1] == '(')
            {
                if (bytes[2] == 'B')       // <esc>(B
                {
                    return ISO2022Modes.ModeASCII;
                }
                else if (bytes[2] == 'H')  // <esc>(H
                {
                    // Actually this is supposed to be Swedish
                    // We treat it like ASCII though.
                    return ISO2022Modes.ModeASCII;
                }
                else if (bytes[2] == 'J')  // <esc>(J
                {
                    // Actually this is supposed to be Roman
                    // 2 characters are different, but historically we treat it as ascii
                    return ISO2022Modes.ModeASCII;
                }
                else if (bytes[2] == 'I')  // <esc>(I
                {
                    return ISO2022Modes.ModeHalfwidthKatakana;
                }
            }
            else if (bytes[1] == '$')
            {
                if (bytes[2] == '@' ||   // <esc>$@
                    bytes[2] == 'B')     // <esc>$B
                {
                    return ISO2022Modes.ModeJIS0208;
                }
                else
                {
                    // Looking for <esc>$(D
                    if (escapeCount < 4)
                        return ISO2022Modes.ModeIncompleteEscape;

                    if (bytes[2] == '(' && bytes[3] == 'D') // <esc>$(D
                    {
                        // Mlang treated 0208 like 0212 even though that's wrong
                        return ISO2022Modes.ModeJIS0208;
                    }
                }
            }
            else if (bytes[1] == '&')
            {
                if (bytes[2] == '@')            // <esc>&@
                {
                    // Ignore ESC & @ (prefix to <esc>$B)
                    return ISO2022Modes.ModeNOOP;
                }
            }

            // If we get here we fell through and have an invalid/unknown escape sequence
            return ISO2022Modes.ModeInvalidEscape;
        }

        private byte DecrementEscapeBytes(ref byte[] bytes, ref int count)
        {
            Debug.Assert(count > 0, "[ISO2022Encoding.DecrementEscapeBytes]count > 0");

            // Decrement our count
            count--;

            // Remember the first one
            byte returnValue = bytes[0];

            // Move them down one.
            for (int i = 0; i < count; i++)
            {
                bytes[i] = bytes[i + 1];
            }

            // Clear out the last byte
            bytes[count] = 0;

            // Return the old 1st byte
            return returnValue;
        }

        // Note that in DBCS mode mlang passed through ' ', '\t' and '\n' as SBCS characters
        // probably to allow mailer formatting without too much extra work.
        private unsafe int GetCharsCP50225KR(byte* bytes, int byteCount,
                                                   char* chars, int charCount, ISO2022Decoder decoder)
        {
            // Get our info.
            EncodingCharBuffer buffer = new EncodingCharBuffer(this, decoder, chars, charCount, bytes, byteCount);

            // No mode information yet
            ISO2022Modes currentMode = ISO2022Modes.ModeASCII;      // Our current Mode

            byte[] escapeBytes = new byte[4];
            int escapeCount = 0;

            if (decoder != null)
            {
                currentMode = decoder.currentMode;

                // See if we have leftover decoder buffer to use
                // Load our bytesLeftOver
                escapeCount = decoder.bytesLeftOverCount;

                // Don't want to mess up decoder if we're counting or throw an exception
                for (int i = 0; i < escapeCount; i++)
                    escapeBytes[i] = decoder.bytesLeftOver[i];
            }

            // Do this until the end, just do '?' replacement because we don't have fallbacks for decodings.
            while (buffer.MoreData || escapeCount > 0)
            {
                byte ch;

                if (escapeCount > 0)
                {
                    // Get more escape sequences if necessary
                    if (escapeBytes[0] == ESCAPE)
                    {
                        // Stop if no more input
                        if (!buffer.MoreData)
                        {
                            if (decoder != null && !decoder.MustFlush)
                                break;
                        }
                        else
                        {
                            // Add it to the sequence we can check
                            escapeBytes[escapeCount++] = buffer.GetNextByte();

                            // We have an escape sequence
                            ISO2022Modes modeReturn =
                                CheckEscapeSequenceKR(escapeBytes, escapeCount);

                            if (modeReturn != ISO2022Modes.ModeInvalidEscape)
                            {
                                if (modeReturn != ISO2022Modes.ModeIncompleteEscape)
                                {
                                    // Processed escape correctly, no effect (we know about KR mode)
                                    escapeCount = 0;
                                }

                                // Either way, continue to get next escape or real byte
                                continue;
                            }
                        }
                        // If ModeInvalidEscape, or no input & must flush, then fall through to add escape.
                    }

                    // Still have something left over in escape buffer
                    // Get it and move them down one
                    ch = DecrementEscapeBytes(ref escapeBytes, ref escapeCount);
                }
                else
                {
                    // Get our next byte
                    ch = buffer.GetNextByte();

                    if (ch == ESCAPE)
                    {
                        // We'll have an escape sequence, use it if we don't have one buffered already
                        if (escapeCount == 0)
                        {
                            // Start this new escape sequence
                            escapeBytes[0] = ch;
                            escapeCount = 1;
                            continue;
                        }

                        // Flush previous escape sequence, then reuse this escape byte
                        buffer.AdjustBytes(-1);
                    }
                }

                if (ch == SHIFT_OUT)
                {
                    currentMode = ISO2022Modes.ModeKR;
                    continue;
                }
                else if (ch == SHIFT_IN)
                {
                    currentMode = ISO2022Modes.ModeASCII;
                    continue;
                }

                // Get our full character
                ushort iBytes = ch;
                bool b2Bytes = false;

                // MLANG was passing through ' ', '\t' and '\n', so we do so as well, but I don't see that in the RFC.
                if (currentMode == ISO2022Modes.ModeKR && ch != ' ' && ch != '\t' && ch != '\n')
                {
                    //
                    //  To handle errors, we need to check:
                    //    1. if trailbyte is there
                    //    2. if code is valid
                    //
                    if (escapeCount > 0)
                    {
                        // Let another escape fall through
                        if (escapeBytes[0] != ESCAPE)
                        {
                            // Move them down one & get the next data
                            iBytes <<= 8;
                            iBytes |= DecrementEscapeBytes(ref escapeBytes, ref escapeCount);
                            b2Bytes = true;
                        }
                    }
                    else if (buffer.MoreData)
                    {
                        iBytes <<= 8;
                        iBytes |= buffer.GetNextByte();
                        b2Bytes = true;
                    }
                    else
                    {
                        // Not enough input, use decoder if possible
                        if (decoder == null || decoder.MustFlush)
                        {
                            // No decoder, do fallback for lonely 1st byte
                            buffer.Fallback(ch);
                            break;
                        }

                        // Stick it in the decoder if we're not counting
                        if (chars != null)
                        {
                            escapeBytes[0] = ch;
                            escapeCount = 1;
                        }
                        break;
                    }
                }

                // We have a iBytes to try to convert.
                char c = mapBytesToUnicode[iBytes];

                // See if it was unknown
                if (c == UNKNOWN_CHAR_FLAG && iBytes != 0)
                {
                    // Have to do fallback
                    if (b2Bytes)
                    {
                        if (!buffer.Fallback((byte)(iBytes >> 8), (byte)iBytes))
                            break;
                    }
                    else
                    {
                        if (!buffer.Fallback(ch))
                            break;
                    }
                }
                else
                {
                    if (!buffer.AddChar(c, b2Bytes ? 2 : 1))
                        break;
                }
            }

            // Make sure our decoder state matches our mode, if not counting
            if (chars != null && decoder != null)
            {
                // Remember it if we don't flush
                if (!decoder.MustFlush || escapeCount != 0)
                {
                    // Either not flushing or had state (from convert)
                    Debug.Assert(!decoder.MustFlush || !decoder.m_throwOnOverflow,
                        "[ISO2022Encoding.GetCharsCP50225KR]Expected no state or not converting or not flushing");

                    decoder.currentMode = currentMode;

                    // Remember escape buffer
                    decoder.bytesLeftOverCount = escapeCount;
                    decoder.bytesLeftOver = escapeBytes;
                }
                else
                {
                    // We flush, clear buffer
                    decoder.currentMode = ISO2022Modes.ModeASCII;
                    decoder.shiftInOutMode = ISO2022Modes.ModeASCII;
                    decoder.bytesLeftOverCount = 0;
                }

                decoder.m_bytesUsed = buffer.BytesUsed;
            }

            // Return # of characters we found
            return buffer.Count;
        }

        // We know we have an escape sequence, so check it starting with the byte after the escape
        private ISO2022Modes CheckEscapeSequenceKR(byte[] bytes, int escapeCount)
        {
            // Have an escape sequence
            if (bytes[0] != ESCAPE)
                return ISO2022Modes.ModeInvalidEscape;

            if (escapeCount < 4)
                return ISO2022Modes.ModeIncompleteEscape;

            if (bytes[1] == '$' && bytes[2] == ')' && bytes[3] == 'C') // <esc>$)C
                return ISO2022Modes.ModeKR;

            // If we get here we fell through and have an invalid/unknown escape sequence
            return ISO2022Modes.ModeInvalidEscape;
        }

        // CP52936 is HZ Encoding
        // HZ Encoding has 4 shift sequences:
        // ~~       '~' (\u7e)
        // ~}       shift into 1 byte mode,
        // ~{       shift into 2 byte GB 2312-80
        // ~<NL>    Maintain 2 byte mode across new lines (ignore both ~ and <NL> characters)
        //          (This is for mailers that restrict to 70 or 80 or whatever character lines)
        //
        // According to comment in mlang, lead & trail byte ranges are described in RFC 1843
        // RFC 1843 => valid HZ code range: leading byte 0x21 - 0x77, 2nd byte 0x21 - 0x7e
        // Our 936 code points are or'd with 0x8080, so lead byte 0xa1 - 0xf7, trail byte 0xa1 - 0xfe
        //
        // This encoding is designed for transmission by e-mail and news.  No bytes should have high bit set.
        // (all bytes <= 0x7f)
        private unsafe int GetCharsCP52936(byte* bytes, int byteCount,
                                                char* chars, int charCount, ISO2022Decoder decoder)
        {
            Debug.Assert(byteCount >= 0, "[ISO2022Encoding.GetCharsCP52936]count >=0");
            Debug.Assert(bytes != null, "[ISO2022Encoding.GetCharsCP52936]bytes!=null");

            // Get our info.
            EncodingCharBuffer buffer = new EncodingCharBuffer(this, decoder, chars, charCount, bytes, byteCount);

            // No mode information yet
            ISO2022Modes currentMode = ISO2022Modes.ModeASCII;
            int byteLeftOver = -1;
            bool bUsedDecoder = false;

            if (decoder != null)
            {
                currentMode = decoder.currentMode;
                // See if we have leftover decoder buffer to use
                // Don't want to mess up decoder if we're counting or throw an exception
                if (decoder.bytesLeftOverCount != 0)
                {
                    // Load our bytesLeftOver
                    byteLeftOver = decoder.bytesLeftOver[0];
                }
            }

            // Do this until the end, just do '?' replacement because we don't have fallbacks for decodings.
            while (buffer.MoreData || byteLeftOver >= 0)
            {
                byte ch;

                // May have a left over byte
                if (byteLeftOver >= 0)
                {
                    ch = (byte)byteLeftOver;
                    byteLeftOver = -1;
                }
                else
                {
                    ch = buffer.GetNextByte();
                }

                // We're in escape mode
                if (ch == '~')
                {
                    // Next char is type of switch
                    if (!buffer.MoreData)
                    {
                        // We don't have anything left, it'll be in decoder or a ?
                        // don't fail if we are allowing overflows
                        if (decoder == null || decoder.MustFlush)
                        {
                            // We'll be a '?'
                            buffer.Fallback(ch);
                            // break if we fail & break if we don't (because !MoreData)
                            // Add succeeded, continue
                            break;
                        }

                        // Stick it in decoder
                        if (decoder != null)
                            decoder.ClearMustFlush();

                        if (chars != null)
                        {
                            decoder.bytesLeftOverCount = 1;
                            decoder.bytesLeftOver[0] = (byte)'~';
                            bUsedDecoder = true;
                        }
                        break;
                    }

                    // What type is it?, get 2nd byte
                    ch = buffer.GetNextByte();

                    if (ch == '~' && currentMode == ISO2022Modes.ModeASCII)
                    {
                        // Its just a ~~ replacement for ~, add it
                        if (!buffer.AddChar((char)ch, 2))
                            // Add failed, break for converting
                            break;

                        // Add succeeded, continue
                        continue;
                    }
                    else if (ch == '{')
                    {
                        // Switching to Double Byte mode
                        currentMode = ISO2022Modes.ModeHZ;
                        continue;
                    }
                    else if (ch == '}')
                    {
                        // Switching to ASCII mode
                        currentMode = ISO2022Modes.ModeASCII;
                        continue;
                    }
                    else if (ch == '\n')
                    {
                        // Ignore ~\n sequence
                        continue;
                    }
                    else
                    {
                        // Unknown escape, back up and try the '~' as a "normal" byte or lead byte
                        buffer.AdjustBytes(-1);
                        ch = (byte)'~';
                    }
                }

                // go ahead and add our data
                if (currentMode != ISO2022Modes.ModeASCII)
                {
                    // Should be ModeHZ
                    Debug.Assert(currentMode == ISO2022Modes.ModeHZ, "[ISO2022Encoding.GetCharsCP52936]Expected ModeHZ");
                    char cm;

                    // Everett allowed characters < 0x20 to be passed as if they were ASCII
                    if (ch < 0x20)
                    {
                        // Emit it as ASCII
                        goto STOREASCII;
                    }

                    // Its multibyte, should have another byte
                    if (!buffer.MoreData)
                    {
                        // No bytes left
                        // don't fail if we are allowing overflows
                        if (decoder == null || decoder.MustFlush)
                        {
                            // Not enough bytes, fallback lead byte
                            buffer.Fallback(ch);

                            // Break if we fail & break because !MoreData
                            break;
                        }

                        if (decoder != null)
                            decoder.ClearMustFlush();

                        // Stick it in decoder
                        if (chars != null)
                        {
                            decoder.bytesLeftOverCount = 1;
                            decoder.bytesLeftOver[0] = ch;
                            bUsedDecoder = true;
                        }
                        break;
                    }

                    // Everett uses space as an escape character for single SBCS bytes
                    byte ch2 = buffer.GetNextByte();
                    ushort iBytes = (ushort)(ch << 8 | ch2);

                    if (ch == ' ' && ch2 != 0)
                    {
                        // Get next char and treat it like ASCII (Everett treated space like an escape
                        // allowing the next char to be just ascii)
                        cm = (char)ch2;
                        goto STOREMULTIBYTE;
                    }

                    // Bytes should be in range: lead byte 0x21-0x77, trail byte: 0x21 - 0x7e
                    if ((ch < 0x21 || ch > 0x77 || ch2 < 0x21 || ch2 > 0x7e) &&
                    // Everett allowed high bit mappings for same characters (but only if both bits set)
                        (ch < 0xa1 || ch > 0xf7 || ch2 < 0xa1 || ch2 > 0xfe))
                    {
                        // For some reason Everett allowed XX20 to become unicode 3000... (ideo sp)
                        if (ch2 == 0x20 && 0x21 <= ch && ch <= 0x7d)
                        {
                            iBytes = 0x2121;
                            goto MULTIBYTE;
                        }

                        // Illegal char, use fallback.  If lead byte is 0 have to do it special and do it first
                        if (!buffer.Fallback((byte)(iBytes >> 8), (byte)(iBytes)))
                            break;
                        continue;
                    }

                MULTIBYTE:
                    iBytes |= 0x8080;
                    // Look up the multibyte char to stick it in our data

                    // We have a iBytes to try to convert.
                    cm = mapBytesToUnicode[iBytes];

                STOREMULTIBYTE:

                    // See if it was unknown
                    if (cm == UNKNOWN_CHAR_FLAG && iBytes != 0)
                    {
                        // Fall back the unknown stuff
                        if (!buffer.Fallback((byte)(iBytes >> 8), (byte)(iBytes)))
                            break;
                        continue;
                    }

                    if (!buffer.AddChar(cm, 2))
                        break;              // convert ran out of buffer, stop
                    continue;
                }

            // Just ASCII
            // We allow some chars > 7f because Everett did, so we have to look them up.
            STOREASCII:
                char c = mapBytesToUnicode[ch];

                // Check if it was unknown
                if ((c == UNKNOWN_CHAR_FLAG || c == 0) && (ch != 0))
                {
                    // fallback the unknown bytes
                    if (!buffer.Fallback((byte)ch))
                        break;
                    continue;
                }

                // Go ahead and add our ASCII character
                if (!buffer.AddChar(c))
                    break;                  // convert ran out of buffer, stop
            }

            // Need to remember our state, IF we're not counting
            if (chars != null && decoder != null)
            {
                if (!bUsedDecoder)
                {
                    // If we didn't use it, clear the byte left over
                    decoder.bytesLeftOverCount = 0;
                }

                if (decoder.MustFlush && decoder.bytesLeftOverCount == 0)
                {
                    decoder.currentMode = ISO2022Modes.ModeASCII;
                }
                else
                {
                    // Either not flushing or had state (from convert)
                    Debug.Assert(!decoder.MustFlush || !decoder.m_throwOnOverflow,
                        "[ISO2022Encoding.GetCharsCP52936]Expected no state or not converting or not flushing");

                    decoder.currentMode = currentMode;
                }
                decoder.m_bytesUsed = buffer.BytesUsed;
            }

            // Return # of characters we found
            return buffer.Count;
        }

        // Note: These all end up with 1/2 bytes of average byte count, so unless we're 1 we're always
        // charCount/2 bytes too big.
        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            // Characters would be # of characters + 1 in case high surrogate is ? * max fallback
            long byteCount = (long)charCount + 1;

            if (EncoderFallback.MaxCharCount > 1)
                byteCount *= EncoderFallback.MaxCharCount;

            // Start with just generic DBCS values (sort of).
            int perChar = 2;
            int extraStart = 0;
            int extraEnd = 0;

            switch (CodePage)
            {
                case 50220:
                case 50221:
                    // 2 bytes per char + 3 bytes switch to JIS 0208 or 1 byte + 3 bytes switch to 1 byte CP
                    perChar = 5;        // 5 max (4.5 average)
                    extraEnd = 3;       // 3 bytes to shift back to ASCII
                    break;
                case 50222:
                    // 2 bytes per char + 3 bytes switch to JIS 0208 or 1 byte + 3 bytes switch to 1 byte CP
                    perChar = 5;        // 5 max (4.5 average)
                    extraEnd = 4;       // 1 byte to shift from Katakana -> DBCS, 3 bytes to shift back to ASCII from DBCS
                    break;
                case 50225:
                    // 2 bytes per char + 1 byte SO, or 1 byte per char + 1 byte SI.
                    perChar = 3;        // 3 max, (2.5 average)
                    extraStart = 4;     // EUC-KR marker appears at beginning of file.
                    extraEnd = 1;       // 1 byte to shift back to ascii if necessary.
                    break;
                case 52936:
                    // 2 bytes per char + 2 byte shift, or 1 byte + 1 byte shift
                    // Worst case: left over surrogate with no low surrogate is extra ?, could have to switch to ASCII, then could have HZ and flush to ASCII mode
                    perChar = 4;        // 4 max, (3.5 average if every other char is HZ/ASCII)
                    extraEnd = 2;       // 2 if we have to shift back to ASCII
                    break;
            }

            // Return our surrogate and End plus perChar for each char.
            byteCount *= perChar;
            byteCount += extraStart + extraEnd;

            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ArgumentOutOfRange_GetByteCountOverflow);

            return (int)byteCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            int perChar = 1;
            int extraDecoder = 1;

            switch (CodePage)
            {
                case 50220:
                case 50221:
                case 50222:
                case 50225:
                    perChar = 1;        // Worst case all ASCII
                    extraDecoder = 3;   // Could have left over 3 chars of 4 char escape sequence, that all become ?
                    break;
                case 52936:
                    perChar = 1;        // Worst case all ASCII
                    extraDecoder = 1;   // sequences are 2 chars, so if next one is illegal, then previous 1 could be ?
                    break;
            }

            // Figure out our length, perchar * char + whatever extra our decoder could do to us.
            long charCount = ((long)byteCount * perChar) + extraDecoder;

            // Just in case we have to fall back unknown ones.
            if (DecoderFallback.MaxCharCount > 1)
                charCount *= DecoderFallback.MaxCharCount;

            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.ArgumentOutOfRange_GetCharCountOverflow);

            return (int)charCount;
        }

        public override Encoder GetEncoder()
        {
            return new ISO2022Encoder(this);
        }

        public override Decoder GetDecoder()
        {
            return new ISO2022Decoder(this);
        }

        internal class ISO2022Encoder : System.Text.EncoderNLS
        {
            internal ISO2022Modes currentMode;
            internal ISO2022Modes shiftInOutMode;

            internal ISO2022Encoder(EncodingNLS encoding) : base(encoding)
            {
                // base calls reset
            }

            public override void Reset()
            {
                // Reset
                currentMode = ISO2022Modes.ModeASCII;
                shiftInOutMode = ISO2022Modes.ModeASCII;
                charLeftOver = (char)0;
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();
            }

            // Anything left in our encoder?
            internal override bool HasState
            {
                get
                {
                    // Don't check shift-out mode, it may be ascii (JP) or not (KR)
                    return (charLeftOver != (char)0 ||
                            currentMode != ISO2022Modes.ModeASCII);
                }
            }
        }

        internal class ISO2022Decoder : System.Text.DecoderNLS
        {
            internal byte[] bytesLeftOver;
            internal int bytesLeftOverCount;
            internal ISO2022Modes currentMode;
            internal ISO2022Modes shiftInOutMode;

            internal ISO2022Decoder(EncodingNLS encoding) : base(encoding)
            {
                // base calls reset
            }

            public override void Reset()
            {
                // Reset
                bytesLeftOverCount = 0;
                bytesLeftOver = new byte[4];
                currentMode = ISO2022Modes.ModeASCII;
                shiftInOutMode = ISO2022Modes.ModeASCII;
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();
            }

            // Anything left in our decoder?
            internal override bool HasState
            {
                get
                {
                    // If we have bytes left over or not shifted back to ASCII then we have a problem
                    return (bytesLeftOverCount != 0 ||
                            currentMode != ISO2022Modes.ModeASCII);
                }
            }
        }

        private static ushort[] s_HalfToFullWidthKanaTable =
        {
            0xa1a3, // 0x8ea1 : Halfwidth Ideographic Period
            0xa1d6, // 0x8ea2 : Halfwidth Opening Corner Bracket
            0xa1d7, // 0x8ea3 : Halfwidth Closing Corner Bracket
            0xa1a2, // 0x8ea4 : Halfwidth Ideographic Comma
            0xa1a6, // 0x8ea5 : Halfwidth Katakana Middle Dot
            0xa5f2, // 0x8ea6 : Halfwidth Katakana Wo
            0xa5a1, // 0x8ea7 : Halfwidth Katakana Small A
            0xa5a3, // 0x8ea8 : Halfwidth Katakana Small I
            0xa5a5, // 0x8ea9 : Halfwidth Katakana Small U
            0xa5a7, // 0x8eaa : Halfwidth Katakana Small E
            0xa5a9, // 0x8eab : Halfwidth Katakana Small O
            0xa5e3, // 0x8eac : Halfwidth Katakana Small Ya
            0xa5e5, // 0x8ead : Halfwidth Katakana Small Yu
            0xa5e7, // 0x8eae : Halfwidth Katakana Small Yo
            0xa5c3, // 0x8eaf : Halfwidth Katakana Small Tu
            0xa1bc, // 0x8eb0 : Halfwidth Katakana-Hiragana Prolonged Sound Mark
            0xa5a2, // 0x8eb1 : Halfwidth Katakana A
            0xa5a4, // 0x8eb2 : Halfwidth Katakana I
            0xa5a6, // 0x8eb3 : Halfwidth Katakana U
            0xa5a8, // 0x8eb4 : Halfwidth Katakana E
            0xa5aa, // 0x8eb5 : Halfwidth Katakana O
            0xa5ab, // 0x8eb6 : Halfwidth Katakana Ka
            0xa5ad, // 0x8eb7 : Halfwidth Katakana Ki
            0xa5af, // 0x8eb8 : Halfwidth Katakana Ku
            0xa5b1, // 0x8eb9 : Halfwidth Katakana Ke
            0xa5b3, // 0x8eba : Halfwidth Katakana Ko
            0xa5b5, // 0x8ebb : Halfwidth Katakana Sa
            0xa5b7, // 0x8ebc : Halfwidth Katakana Si
            0xa5b9, // 0x8ebd : Halfwidth Katakana Su
            0xa5bb, // 0x8ebe : Halfwidth Katakana Se
            0xa5bd, // 0x8ebf : Halfwidth Katakana So
            0xa5bf, // 0x8ec0 : Halfwidth Katakana Ta
            0xa5c1, // 0x8ec1 : Halfwidth Katakana Ti
            0xa5c4, // 0x8ec2 : Halfwidth Katakana Tu
            0xa5c6, // 0x8ec3 : Halfwidth Katakana Te
            0xa5c8, // 0x8ec4 : Halfwidth Katakana To
            0xa5ca, // 0x8ec5 : Halfwidth Katakana Na
            0xa5cb, // 0x8ec6 : Halfwidth Katakana Ni
            0xa5cc, // 0x8ec7 : Halfwidth Katakana Nu
            0xa5cd, // 0x8ec8 : Halfwidth Katakana Ne
            0xa5ce, // 0x8ec9 : Halfwidth Katakana No
            0xa5cf, // 0x8eca : Halfwidth Katakana Ha
            0xa5d2, // 0x8ecb : Halfwidth Katakana Hi
            0xa5d5, // 0x8ecc : Halfwidth Katakana Hu
            0xa5d8, // 0x8ecd : Halfwidth Katakana He
            0xa5db, // 0x8ece : Halfwidth Katakana Ho
            0xa5de, // 0x8ecf : Halfwidth Katakana Ma
            0xa5df, // 0x8ed0 : Halfwidth Katakana Mi
            0xa5e0, // 0x8ed1 : Halfwidth Katakana Mu
            0xa5e1, // 0x8ed2 : Halfwidth Katakana Me
            0xa5e2, // 0x8ed3 : Halfwidth Katakana Mo
            0xa5e4, // 0x8ed4 : Halfwidth Katakana Ya
            0xa5e6, // 0x8ed5 : Halfwidth Katakana Yu
            0xa5e8, // 0x8ed6 : Halfwidth Katakana Yo
            0xa5e9, // 0x8ed7 : Halfwidth Katakana Ra
            0xa5ea, // 0x8ed8 : Halfwidth Katakana Ri
            0xa5eb, // 0x8ed9 : Halfwidth Katakana Ru
            0xa5ec, // 0x8eda : Halfwidth Katakana Re
            0xa5ed, // 0x8edb : Halfwidth Katakana Ro
            0xa5ef, // 0x8edc : Halfwidth Katakana Wa
            0xa5f3, // 0x8edd : Halfwidth Katakana N
            0xa1ab, // 0x8ede : Halfwidth Katakana Voiced Sound Mark
            0xa1ac  // 0x8edf : Halfwidth Katakana Semi-Voiced Sound Mark
        };
    }
}



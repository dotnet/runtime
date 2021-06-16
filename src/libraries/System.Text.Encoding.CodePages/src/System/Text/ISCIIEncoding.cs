// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ISCIIEncoding
//
//  Ported from windows c_iscii.  If you find bugs here, there are likely similar
//  bugs in the windows version

using System.Diagnostics;
using System.Runtime.Serialization;

namespace System.Text
{
    // Encodes text into and out of the ISCII encodings.
    // ISCII contains characters to encode indic scripts by mapping indic scripts
    // to the same code page.  This works because they are all related scripts.
    // ISCII provides a "font" selection method to switch between the appropriate
    // fonts to display the other scripts.  All ISCII characters are above the
    // ASCII range to provide ASCII compatibility.
    //
    // IsAlwaysNormalized() isn't overridden
    // We don't override IsAlwaysNormalized() because it is false for all forms (like base implementation)
    //      Forms C & KC have things like 0933 + 093C == composed 0934, so they aren't normalized
    //      Forms D & KD have things like 0934, which decomposes to 0933 + 093C, so not normal.
    //      Form IDNA has the above problems plus case mapping, so false (like most encodings)
    //
    internal sealed class ISCIIEncoding : EncodingNLS, ISerializable
    {
        // Constants
        private const int CodeDevanagari = 2;    // 0x42 57002
        private const int CodePunjabi = 11;   // 0x4b 57011 Punjabi (Gurmukhi)

        // Ranges
        private const int MultiByteBegin = 0xa0;     // Beginning of MultiByte space in ISCII
        private const int IndicBegin = 0x0901;   // Beginning of Unicode Indic script code points
        private const int IndicEnd = 0x0d6f;   // End of Unicode Indic Script code points

        // ISCII Control Values
        private const byte ControlATR = 0xef;     // Attribute (ATR) code
        private const byte ControlCodePageStart = 0x40;  // Start of code page range

        // Interesting ISCII characters
        private const byte Virama = 0xe8;
        private const byte Nukta = 0xe9;
        private const byte DevenagariExt = 0xf0;

        // Interesting Unicode characters
        private const char ZWNJ = (char)0x200c;
        private const char ZWJ = (char)0x200d;

        // Code Page
        private readonly int _defaultCodePage;

        public ISCIIEncoding(int codePage) : base(codePage)
        {
            // Set our code page (subtracting windows code page # offset)
            _defaultCodePage = codePage - 57000;

            // Legal windows code pages are between Devanagari and Punjabi
            Debug.Assert(_defaultCodePage >= CodeDevanagari && _defaultCodePage <= CodePunjabi,
                "[ISCIIEncoding] Code page (" + codePage + " isn't supported by ISCIIEncoding!");

            // This shouldn't really be possible
            if (_defaultCodePage < CodeDevanagari || _defaultCodePage > CodePunjabi)
                throw new ArgumentException(SR.Format(SR.Argument_CodepageNotSupported, codePage), nameof(codePage));
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        // Our MaxByteCount is 4 times the input size.  That could be because
        // the first input character could be in the wrong code page ("font") and
        // then that character could also be encoded in 2 code points
        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            // Characters would be # of characters + 1 in case high surrogate is ? * max fallback
            long byteCount = (long)charCount + 1;

            if (EncoderFallback.MaxCharCount > 1)
                byteCount *= EncoderFallback.MaxCharCount;

            // 4 Time input because 1st input could require code page change and also that char could require 2 code points
            byteCount *= 4;

            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ArgumentOutOfRange_GetByteCountOverflow);

            return (int)byteCount;
        }

        // Our MaxCharCount is the same as the byteCount.  There are a few sequences
        // where 2 (or more) bytes could become 2 chars, but that's still 1 to 1.
        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            // Our MaxCharCount is the same as the byteCount.  There are a few sequences
            // where 2 (or more) bytes could become 2 chars, but that's still 1 to 1.
            // Also could have 1 in decoder if we're waiting to see if next char's a nukta.
            long charCount = ((long)byteCount + 1);

            // Some code points are undefined so we could fall back.
            if (DecoderFallback.MaxCharCount > 1)
                charCount *= DecoderFallback.MaxCharCount;

            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.ArgumentOutOfRange_GetCharCountOverflow);

            return (int)charCount;
        }

        // Our workhorse version
        public override unsafe int GetByteCount(char* chars, int count, EncoderNLS? baseEncoder)
        {
            // Use null pointer to ask GetBytes for count
            return GetBytes(chars, count, null, 0, baseEncoder);
        }

        // Workhorse
        public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, EncoderNLS? baseEncoder)
        {
            // Allow null bytes for counting
            Debug.Assert(chars != null, "[ISCIIEncoding.GetBytes]chars!=null");
            //            Debug.Assert(bytes != null, "[ISCIIEncoding.GetBytes]bytes!=null");
            Debug.Assert(charCount >= 0, "[ISCIIEncoding.GetBytes]charCount >=0");
            Debug.Assert(byteCount >= 0, "[ISCIIEncoding.GetBytes]byteCount >=0");

            // Need the ISCII Encoder
            ISCIIEncoder? encoder = (ISCIIEncoder?)baseEncoder;

            // prepare our helpers
            EncodingByteBuffer buffer = new EncodingByteBuffer(this, encoder, bytes, byteCount, chars, charCount);

            int currentCodePage = _defaultCodePage;
            bool bLastVirama = false;

            // Use encoder info if available
            if (encoder != null)
            {
                // Remember our old state
                currentCodePage = encoder.currentCodePage;
                bLastVirama = encoder.bLastVirama;

                // If we have a high surrogate left over, then fall it back
                if (encoder.charLeftOver > 0)
                {
                    buffer.Fallback(encoder.charLeftOver);
                    bLastVirama = false;        // Redundant
                }
            }

            while (buffer.MoreData)
            {
                // Get our data
                char ch = buffer.GetNextChar();

                // See if its a Multi Byte Character
                if (ch < MultiByteBegin)
                {
                    // Its a boring low character, add it.
                    if (!buffer.AddByte((byte)ch))
                        break;
                    bLastVirama = false;
                    continue;
                }

                // See if its outside of the Indic script range
                if ((ch < IndicBegin) || (ch > IndicEnd))
                {
                    // See if its a ZWJ or ZWNJ and if we has bLastVirama;
                    if (bLastVirama && (ch == ZWNJ || ch == ZWJ))
                    {
                        // It was a bLastVirama and ZWNJ || ZWJ
                        if (ch == ZWNJ)
                        {
                            if (!buffer.AddByte(Virama))
                                break;
                        }
                        else // ZWJ
                        {
                            if (!buffer.AddByte(Nukta))
                                break;
                        }

                        // bLastVirama now counts as false
                        bLastVirama = false;
                        continue;
                    }

                    // Have to do our fallback
                    //
                    // Note that this will fallback 2 chars if this is a high surrogate.
                    // Throws if recursive (knows because we called InternalGetNextChar)
                    buffer.Fallback(ch);
                    bLastVirama = false;
                    continue;
                }

                // Its in the Unicode Indic script range
                int indicInfo = s_UnicodeToIndicChar[ch - IndicBegin];
                byte byteIndic = (byte)indicInfo;
                int indicScript = (0x000f & (indicInfo >> 8));
                int indicTwoBytes = (0xf000 & indicInfo);

                // If IndicInfo is 0 then have to do fallback
                if (indicInfo == 0)
                {
                    // Its some Unicode character we don't have indic for.
                    // Have to do our fallback
                    // Add Fallback Count
                    // Note that chars was preincremented, and GetEncoderFallbackString might add an extra
                    // if chars != charEnd and there's a surrogate.
                    // Throws if recursive (knows because we called InternalGetNextChar)
                    buffer.Fallback(ch);

                    bLastVirama = false;
                    continue;
                }

                // See if our code page ("font" in ISCII spec) has to change
                // (This if doesn't add character, just changes character set)
                Debug.Assert(indicScript != 0, "[ISCIIEncoding.GetBytes]expected an indic script value");
                if (indicScript != currentCodePage)
                {
                    // It changed, spit out the ATR
                    if (!buffer.AddByte(ControlATR, (byte)(indicScript | ControlCodePageStart)))
                        break;

                    // Now spit out the new code page (& remember it) (do this afterwards in case AddByte failed)
                    currentCodePage = indicScript;

                    // We only know how to map from Unicode to pages from Devanagari to Punjabi (2 to 11)
                    Debug.Assert(currentCodePage >= CodeDevanagari && currentCodePage <= CodePunjabi,
                        "[ISCIIEncoding.GetBytes]Code page (" + currentCodePage + " shouldn't appear in ISCII from Unicode table!");
                }

                // Safe to add our byte now
                if (!buffer.AddByte(byteIndic, indicTwoBytes != 0 ? 1 : 0))
                    break;

                // Remember if this one was a Virama
                bLastVirama = (byteIndic == Virama);

                // Some characters need extra bytes
                if (indicTwoBytes != 0)
                {
                    // This one needs another byte
                    Debug.Assert((indicTwoBytes >> 12) > 0 && (indicTwoBytes >> 12) <= 3,
                        "[ISCIIEncoding.GetBytes]Expected indicTwoBytes from 1-3, not " + (indicTwoBytes >> 12));

                    // Already did buffer checking, but...
                    if (!buffer.AddByte(s_SecondIndicByte[indicTwoBytes >> 12]))
                        break;
                }
            }

            // May need to switch back to our default code page
            if (currentCodePage != _defaultCodePage && (encoder == null || encoder.MustFlush))
            {
                // It changed, spit out the ATR
                if (buffer.AddByte(ControlATR, (byte)(_defaultCodePage | ControlCodePageStart)))
                    currentCodePage = _defaultCodePage;
                else
                    // If not successful, convert will maintain state for next time, also
                    // AddByte will have decremented our char count, however we need it to remain the same
                    buffer.GetNextChar();
                bLastVirama = false;
            }

            // Make sure we remember our state if necessary
            // Note that we don't care about flush because Virama and code page
            // changes are legal at the end.
            // Don't set encoder if we're just counting
            if (encoder != null && bytes != null)
            {
                // Clear Encoder if necessary.
                if (!buffer.fallbackBufferHelper.bUsedEncoder)
                {
                    encoder.charLeftOver = (char)0;
                }

                // Remember our code page/virama state
                encoder.currentCodePage = currentCodePage;
                encoder.bLastVirama = bLastVirama;

                // How many chars were used?
                encoder.m_charsUsed = buffer.CharsUsed;
            }

            // Return our length
            return buffer.Count;
        }

        // Workhorse
        public override unsafe int GetCharCount(byte* bytes, int count, DecoderNLS? baseDecoder)
        {
            // Just call GetChars with null chars saying we want count
            return GetChars(bytes, count, null, 0, baseDecoder);
        }

        // For decoding, the following interesting rules apply:
        // Virama followed by another Virama or Nukta becomes Virama + ZWNJ or Virama + ZWJ
        // ATR is followed by a byte to switch code pages ("fonts")
        // Devenagari F0, B8 -> \u0952
        // Devenagari F0, BF -> \u0970
        // Some characters followed by E9 become a different character instead.
        public override unsafe int GetChars(byte* bytes, int byteCount,
                                                char* chars, int charCount, DecoderNLS? baseDecoder)
        {
            // Just need to ASSERT, this is called by something else internal that checked parameters already
            // Allow null chars for counting
            Debug.Assert(bytes != null, "[ISCIIEncoding.GetChars]bytes is null");
            Debug.Assert(byteCount >= 0, "[ISCIIEncoding.GetChars]byteCount is negative");
            //            Debug.Assert(chars != null, "[ISCIIEncoding.GetChars]chars is null");
            Debug.Assert(charCount >= 0, "[ISCIIEncoding.GetChars]charCount is negative");

            // Need the ISCII Decoder
            ISCIIDecoder? decoder = (ISCIIDecoder?)baseDecoder;

            // Get our info.
            EncodingCharBuffer buffer = new EncodingCharBuffer(this, decoder, chars, charCount, bytes, byteCount);

            int currentCodePage = _defaultCodePage;
            bool bLastATR = false;
            bool bLastVirama = false;
            bool bLastDevenagariStressAbbr = false;
            char cLastCharForNextNukta = '\0';
            char cLastCharForNoNextNukta = '\0';

            // See if there's anything in our decoder
            if (decoder != null)
            {
                currentCodePage = decoder.currentCodePage;
                bLastATR = decoder.bLastATR;
                bLastVirama = decoder.bLastVirama;
                bLastDevenagariStressAbbr = decoder.bLastDevenagariStressAbbr;
                cLastCharForNextNukta = decoder.cLastCharForNextNukta;
                cLastCharForNoNextNukta = decoder.cLastCharForNoNextNukta;
            }

            bool bLastSpecial = bLastVirama | bLastATR | bLastDevenagariStressAbbr |
                (cLastCharForNextNukta != '\0');

            // Get our current code page index (some code pages are dups)
            int currentCodePageIndex = -1;
            Debug.Assert(currentCodePage >= CodeDevanagari && currentCodePage <= CodePunjabi,
                "[ISCIIEncoding.GetChars]Decoder code page must be >= Devanagari and <= Punjabi, not " + currentCodePage);

            if (currentCodePage >= CodeDevanagari && currentCodePage <= CodePunjabi)
            {
                currentCodePageIndex = s_IndicMappingIndex[currentCodePage];
            }

            // Loop through our input
            while (buffer.MoreData)
            {
                byte b = buffer.GetNextByte();

                // See if last one was special
                if (bLastSpecial)
                {
                    // Now it won't be
                    bLastSpecial = false;

                    // One and only one of our flags should be set
                    Debug.Assert(((bLastVirama ? 1 : 0) + (bLastATR ? 1 : 0) +
                               (bLastDevenagariStressAbbr ? 1 : 0) +
                               ((cLastCharForNextNukta > 0) ? 1 : 0)) == 1,
                        $"[ISCIIEncoding.GetChars]Special cases require 1 and only 1 special case flag: LastATR {bLastATR} Dev. {bLastDevenagariStressAbbr} Nukta {cLastCharForNextNukta}");
                    // If the last one was an ATR, then we'll have to do ATR stuff
                    if (bLastATR)
                    {
                        // We only support Devanagari - Punjabi
                        if (b >= (0x40 | CodeDevanagari) && b <= (0x40 | CodePunjabi))
                        {
                            // Remember the code page
                            currentCodePage = b & 0xf;
                            currentCodePageIndex = s_IndicMappingIndex[currentCodePage];
                            // No longer last ATR
                            bLastATR = false;
                            continue;
                        }

                        // Change back to default?
                        if (b == 0x40)
                        {
                            currentCodePage = _defaultCodePage;
                            currentCodePageIndex = -1;

                            if (currentCodePage >= CodeDevanagari && currentCodePage <= CodePunjabi)
                            {
                                currentCodePageIndex = s_IndicMappingIndex[currentCodePage];
                            }
                            // No longer last ATR
                            bLastATR = false;
                            continue;
                        }

                        // We don't support Roman
                        if (b == 0x41)
                        {
                            currentCodePage = _defaultCodePage;
                            currentCodePageIndex = -1;

                            if (currentCodePage >= CodeDevanagari && currentCodePage <= CodePunjabi)
                            {
                                currentCodePageIndex = s_IndicMappingIndex[currentCodePage];
                            }

                            // Even though we don't know how to support Roman, windows didn't add a ? so we don't either.
                            // No longer last ATR
                            bLastATR = false;
                            continue;
                        }

                        // Other code pages & ATR codes not supported, fallback the ATR
                        // If fails, decrements the buffer, which is OK, we remember ATR state.
                        if (!buffer.Fallback(ControlATR))
                            break;

                        // No longer last ATR (fell back)
                        bLastATR = false;

                        // we know we can't have any of these other modes
                        Debug.Assert(bLastVirama == false, "[ISCIIEncoding.GetChars] Expected no bLastVirama in bLastATR mode");
                        Debug.Assert(bLastDevenagariStressAbbr == false, "[ISCIIEncoding.GetChars] Expected no bLastDevenagariStressAbbr in bLastATR mode");
                        Debug.Assert(cLastCharForNextNukta == (char)0, "[ISCIIEncoding.GetChars] Expected no cLastCharForNextNukta in bLastATR mode");
                        Debug.Assert(cLastCharForNoNextNukta == (char)0, "[ISCIIEncoding.GetChars] Expected no cLastCharForNoNextNukta in bLastATR mode");
                        // Keep processing this byte
                    }
                    else if (bLastVirama)
                    {
                        // If last was Virama, then we might need ZWNJ or ZWJ instead
                        if (b == Virama)
                        {
                            // If no room, then stop
                            if (!buffer.AddChar(ZWNJ))
                                break;
                            bLastVirama = false;
                            continue;
                        }
                        if (b == Nukta)
                        {
                            // If no room, then stop
                            if (!buffer.AddChar(ZWJ))
                                break;
                            bLastVirama = false;
                            continue;
                        }

                        // No longer in this mode, fall through to handle character
                        // (Virama itself was added when flag was set last iteration)
                        bLastVirama = false;

                        // We know we can't have any of these other modes
                        Debug.Assert(bLastATR == false, "[ISCIIEncoding.GetChars] Expected no bLastATR in bLastVirama mode");
                        Debug.Assert(bLastDevenagariStressAbbr == false, "[ISCIIEncoding.GetChars] Expected no bLastDevenagariStressAbbr in bLastVirama mode");
                        Debug.Assert(cLastCharForNextNukta == (char)0, "[ISCIIEncoding.GetChars] Expected no cLastCharForNextNukta in bLastVirama mode");
                        Debug.Assert(cLastCharForNoNextNukta == (char)0, "[ISCIIEncoding.GetChars] Expected no cLastCharForNoNextNukta in bLastVirama mode");
                    }
                    else if (bLastDevenagariStressAbbr)
                    {
                        // Last byte was an 0xf0 (ext).
                        // If current is b8 or bf, then we have 952 or 970.  Otherwise fallback
                        if (b == 0xb8)
                        {
                            // It was a 0xb8
                            if (!buffer.AddChar('\x0952'))         // Devanagari stress sign anudatta
                                break;
                            bLastDevenagariStressAbbr = false;
                            continue;
                        }

                        if (b == 0xbf)
                        {
                            // It was a 0xbf
                            if (!buffer.AddChar('\x0970'))         // Devanagari abbr. sign
                                break;
                            bLastDevenagariStressAbbr = false;
                            continue;
                        }

                        // Wasn't an expected pattern, do fallback for f0 (ext)
                        // if fails, fallback will back up our buffer
                        if (!buffer.Fallback(DevenagariExt))
                            break;

                        // Keep processing this byte (turn off mode)
                        // (last character was added when mode was set)
                        bLastDevenagariStressAbbr = false;

                        Debug.Assert(bLastATR == false, "[ISCIIEncoding.GetChars] Expected no bLastATR in bLastDevenagariStressAbbr mode");
                        Debug.Assert(bLastVirama == false, "[ISCIIEncoding.GetChars] Expected no bLastVirama in bLastDevenagariStressAbbr mode");
                        Debug.Assert(cLastCharForNextNukta == (char)0, "[ISCIIEncoding.GetChars] Expected no cLastCharForNextNukta in bLastDevenagariStressAbbr mode");
                        Debug.Assert(cLastCharForNoNextNukta == (char)0, "[ISCIIEncoding.GetChars] Expected no cLastCharForNoNextNukta in bLastDevenagariStressAbbr mode");
                    }
                    else
                    {
                        // We were checking for next char being a nukta
                        Debug.Assert(cLastCharForNextNukta > 0 && cLastCharForNoNextNukta > 0,
                            "[ISCIIEncoding.GetChars]No other special case found, but cLastCharFor(No)NextNukta variable(s) aren't set.");

                        // We'll either add combined char or last char
                        if (b == Nukta)
                        {
                            // We combine nukta with previous char
                            if (!buffer.AddChar(cLastCharForNextNukta))
                                break;

                            // Done already
                            cLastCharForNextNukta = cLastCharForNoNextNukta = '\0';
                            continue;
                        }

                        // No Nukta, just add last character and keep processing current byte
                        if (!buffer.AddChar(cLastCharForNoNextNukta))
                            break;

                        // Keep processing this byte, turn off mode.
                        cLastCharForNextNukta = cLastCharForNoNextNukta = '\0';

                        Debug.Assert(bLastATR == false, "[ISCIIEncoding.GetChars] Expected no bLastATR in cLastCharForNextNukta mode");
                        Debug.Assert(bLastVirama == false, "[ISCIIEncoding.GetChars] Expected no bLastVirama in cLastCharForNextNukta mode");
                        Debug.Assert(bLastDevenagariStressAbbr == false, "[ISCIIEncoding.GetChars] Expected no bLastDevenagariStressAbbr in cLastCharForNextNukta mode");
                    }
                }

                // Now bLastSpecial should be false and all flags false.
                Debug.Assert(!bLastSpecial && !bLastDevenagariStressAbbr && !bLastVirama && !bLastATR &&
                          cLastCharForNextNukta == '\0',
                          "[ISCIIEncoding.GetChars]No special state for last code point should exist at this point.");

                // If its a simple byte, just add it
                if (b < MultiByteBegin)
                {
                    if (!buffer.AddChar((char)b))
                        break;
                    continue;
                }

                // See if its an ATR marker
                if (b == ControlATR)
                {
                    bLastATR = bLastSpecial = true;
                    continue;
                }

                Debug.Assert(currentCodePageIndex != -1, "[ISCIIEncoding.GetChars]Expected valid currentCodePageIndex != -1");
                char ch = s_IndicMapping[currentCodePageIndex, 0, b - MultiByteBegin];
                char cAlt = s_IndicMapping[currentCodePageIndex, 1, b - MultiByteBegin];

                // If no 2nd char, just add it, also lonely Nuktas get added as well.
                if (cAlt == 0 || b == Nukta)
                {
                    // If it was an unknown character do fallback

                    // ? if not known.
                    if (ch == 0)
                    {
                        // Fallback the unknown byte
                        if (!buffer.Fallback(b))
                            break;
                    }
                    else
                    {
                        // Add the known character
                        if (!buffer.AddChar(ch))
                            break;
                    }
                    continue;
                }

                // if b == Virama set last Virama so we can do ZWJ or ZWNJ next time if needed.
                if (b == Virama)
                {
                    // Add Virama
                    if (!buffer.AddChar(ch))
                        break;
                    bLastVirama = bLastSpecial = true;
                    continue;
                }

                // See if its one that changes with a Nukta
                if ((cAlt & 0xF000) == 0)
                {
                    // It could change if next char is a nukta
                    bLastSpecial = true;
                    cLastCharForNextNukta = cAlt;
                    cLastCharForNoNextNukta = ch;
                    continue;
                }

                // We must be the Devenagari special case for F0, B8 & F0, BF
                Debug.Assert(currentCodePage == CodeDevanagari && b == DevenagariExt,
                    $"[ISCIIEncoding.GetChars] Devenagari special case must {DevenagariExt} not {b} or in Devanagari code page {CodeDevanagari} not {currentCodePage}.");
                bLastDevenagariStressAbbr = bLastSpecial = true;
            }

            // If we don't have a decoder, or if we had to flush, then we need to get rid
            // of last ATR, LastNoNextNukta and LastDevenagariExt.
            if (decoder == null || decoder.MustFlush)
            {
                // If these fail (because of Convert with insufficient buffer), then they'll turn off MustFlush as well.
                if (bLastATR)
                {
                    // Have to add ATR fallback
                    if (buffer.Fallback(ControlATR))
                        bLastATR = false;
                    else
                        // If not successful, convert will maintain state for next time, also
                        // AddChar will have decremented our byte count, however we need it to remain the same
                        buffer.GetNextByte();
                }
                else if (bLastDevenagariStressAbbr)
                {
                    // Have to do fallback for DevenagariExt
                    if (buffer.Fallback(DevenagariExt))
                        bLastDevenagariStressAbbr = false;
                    else
                        // If not successful, convert will maintain state for next time, also
                        // AddChar will have decremented our byte count, however we need it to remain the same
                        buffer.GetNextByte();
                }
                else if (cLastCharForNoNextNukta != '\0')
                {
                    // Have to add our last char because there was no next nukta
                    if (buffer.AddChar(cLastCharForNoNextNukta))
                        cLastCharForNoNextNukta = cLastCharForNextNukta = '\0';
                    else
                        // If not successful, convert will maintain state for next time, also
                        // AddChar will have decremented our byte count, however we need it to remain the same
                        buffer.GetNextByte();
                }
                // LastVirama is unimportant for flushing decoder.
            }

            // Remember any left over stuff
            // (only remember if we aren't counting)
            if (decoder != null && chars != null)
            {
                // If not flushing or have state (from convert) then need to remember state
                if (!decoder.MustFlush ||
                    cLastCharForNoNextNukta != '\0' || bLastATR || bLastDevenagariStressAbbr)
                {
                    // Either not flushing or had state (from convert)
                    Debug.Assert(!decoder.MustFlush || !decoder.m_throwOnOverflow,
                        "[ISCIIEncoding.GetChars]Expected no state or not converting or not flushing");
                    decoder.currentCodePage = currentCodePage;
                    decoder.bLastVirama = bLastVirama;
                    decoder.bLastATR = bLastATR;
                    decoder.bLastDevenagariStressAbbr = bLastDevenagariStressAbbr;
                    decoder.cLastCharForNextNukta = cLastCharForNextNukta;
                    decoder.cLastCharForNoNextNukta = cLastCharForNoNextNukta;
                }
                else
                {
                    decoder.currentCodePage = _defaultCodePage;
                    decoder.bLastVirama = false;
                    decoder.bLastATR = false;
                    decoder.bLastDevenagariStressAbbr = false;
                    decoder.cLastCharForNextNukta = '\0';
                    decoder.cLastCharForNoNextNukta = '\0';
                }
                decoder.m_bytesUsed = buffer.BytesUsed;
            }
            // Otherwise we already did fallback and added extra things

            // Return the # of characters we found
            return buffer.Count;
        }

        public override Decoder GetDecoder()
        {
            return new ISCIIDecoder(this);
        }

        public override Encoder GetEncoder()
        {
            return new ISCIIEncoder(this);
        }

        public override int GetHashCode()
        {
            //Not great distribution, but this is relatively unlikely to be used as the key in a hashtable.
            return _defaultCodePage + EncoderFallback.GetHashCode() + DecoderFallback.GetHashCode();
        }

        internal sealed class ISCIIEncoder : EncoderNLS
        {
            // Need to remember the default code page (for HasState)
            internal int defaultCodePage;

            // Need a place for the current code page
            internal int currentCodePage;

            // Was the last character a virama?  (Because ZWJ and ZWNJ are different then)
            internal bool bLastVirama;

            public ISCIIEncoder(EncodingNLS encoding) : base(encoding)
            {
                currentCodePage = defaultCodePage = encoding.CodePage - 57000;
                // base calls reset
            }

            // Warning: If you're decoding mixed encoding files or something, this could be confusing
            //          We don't always force back to base encoding mapping, so if you reset where do you restart?
            public override void Reset()
            {
                bLastVirama = false;
                charLeftOver = (char)0;
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();
            }

            // Anything left in our encoder?
            // Encoder not only has to get rid of left over characters, but it has to switch back to the current code page.
            internal override bool HasState
            {
                get
                {
                    return (charLeftOver != (char)0 || currentCodePage != defaultCodePage);
                }
            }
        }

        internal sealed class ISCIIDecoder : DecoderNLS
        {
            // Need a place to store any our current code page and last ATR flag
            internal int currentCodePage;
            internal bool bLastATR;
            internal bool bLastVirama;
            internal bool bLastDevenagariStressAbbr;
            internal char cLastCharForNextNukta;
            internal char cLastCharForNoNextNukta;

            public ISCIIDecoder(EncodingNLS encoding) : base(encoding)
            {
                currentCodePage = encoding.CodePage - 57000;
                // base calls reset
            }

            // Warning: If you're decoding mixed encoding files or something, this could be confusing
            //          We don't always force back to base encoding mapping, so if you reset where do you restart?
            public override void Reset()
            {
                bLastATR = false;
                bLastVirama = false;
                bLastDevenagariStressAbbr = false;
                cLastCharForNextNukta = '\0';
                cLastCharForNoNextNukta = '\0';
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();
            }

            // Anything left in our decoder?
            internal override bool HasState
            {
                get
                {
                    return (cLastCharForNextNukta != '\0' || cLastCharForNoNextNukta != '\0' ||
                            bLastATR || bLastDevenagariStressAbbr);
                }
            }
        }

        //
        // ISCII Tables
        //
        // From Windows ISCII\tables.c
        //

        ////////////////////////////////////////////////////////////////////////////
        //
        //  Char to Byte
        //
        //  0xXYZZ  Where Y is the code page "font" part and ZZ is the byte character
        //          The high X bits also reference the SecondIndicByte table if an
        //          extra byte is needed.
        //  0x0000  For undefined characters
        //
        //  This is valid for values IndicBegin to IndicEnd
        //
        // WARNING: When this was copied from windows, the ? characters (0x003F) were
        // searched/replaced with 0x0000.
        //
        ////////////////////////////////////////////////////////////////////////////

        private static readonly int[] s_UnicodeToIndicChar =
        {
            0x02a1,  // U+0901 : Devanagari Sign Candrabindu
            0x02a2,  // U+0902 : Devanagari Sign Anusvara
            0x02a3,  // U+0903 : Devanagari Sign Visarga
            0x0000,  // U+0904 : Undefined
            0x02a4,  // U+0905 : Devanagari Letter A
            0x02a5,  // U+0906 : Devanagari Letter Aa
            0x02a6,  // U+0907 : Devanagari Letter I
            0x02a7,  // U+0908 : Devanagari Letter Ii
            0x02a8,  // U+0909 : Devanagari Letter U
            0x02a9,  // U+090a : Devanagari Letter Uu
            0x02aa,  // U+090b : Devanagari Letter Vocalic R
            0x12a6,  // U+090c : Devanagari Letter Vocalic L
            0x02ae,  // U+090d : Devanagari Letter Candra E
            0x02ab,  // U+090e : Devanagari Letter Short E
            0x02ac,  // U+090f : Devanagari Letter E
            0x02ad,  // U+0910 : Devanagari Letter Ai
            0x02b2,  // U+0911 : Devanagari Letter Candra O
            0x02af,  // U+0912 : Devanagari Letter Short O
            0x02b0,  // U+0913 : Devanagari Letter O
            0x02b1,  // U+0914 : Devanagari Letter Au
            0x02b3,  // U+0915 : Devanagari Letter Ka
            0x02b4,  // U+0916 : Devanagari Letter Kha
            0x02b5,  // U+0917 : Devanagari Letter Ga
            0x02b6,  // U+0918 : Devanagari Letter Gha
            0x02b7,  // U+0919 : Devanagari Letter Nga
            0x02b8,  // U+091a : Devanagari Letter Ca
            0x02b9,  // U+091b : Devanagari Letter Cha
            0x02ba,  // U+091c : Devanagari Letter Ja
            0x02bb,  // U+091d : Devanagari Letter Jha
            0x02bc,  // U+091e : Devanagari Letter Nya
            0x02bd,  // U+091f : Devanagari Letter Tta
            0x02be,  // U+0920 : Devanagari Letter Ttha
            0x02bf,  // U+0921 : Devanagari Letter Dda
            0x02c0,  // U+0922 : Devanagari Letter Ddha
            0x02c1,  // U+0923 : Devanagari Letter Nna
            0x02c2,  // U+0924 : Devanagari Letter Ta
            0x02c3,  // U+0925 : Devanagari Letter Tha
            0x02c4,  // U+0926 : Devanagari Letter Da
            0x02c5,  // U+0927 : Devanagari Letter Dha
            0x02c6,  // U+0928 : Devanagari Letter Na
            0x02c7,  // U+0929 : Devanagari Letter Nnna
            0x02c8,  // U+092a : Devanagari Letter Pa
            0x02c9,  // U+092b : Devanagari Letter Pha
            0x02ca,  // U+092c : Devanagari Letter Ba
            0x02cb,  // U+092d : Devanagari Letter Bha
            0x02cc,  // U+092e : Devanagari Letter Ma
            0x02cd,  // U+092f : Devanagari Letter Ya
            0x02cf,  // U+0930 : Devanagari Letter Ra
            0x02d0,  // U+0931 : Devanagari Letter Rra
            0x02d1,  // U+0932 : Devanagari Letter La
            0x02d2,  // U+0933 : Devanagari Letter Lla
            0x02d3,  // U+0934 : Devanagari Letter Llla
            0x02d4,  // U+0935 : Devanagari Letter Va
            0x02d5,  // U+0936 : Devanagari Letter Sha
            0x02d6,  // U+0937 : Devanagari Letter Ssa
            0x02d7,  // U+0938 : Devanagari Letter Sa
            0x02d8,  // U+0939 : Devanagari Letter Ha
            0x0000,  // U+093a : Undefined
            0x0000,  // U+093b : Undefined
            0x02e9,  // U+093c : Devanagari Sign Nukta
            0x12ea,  // U+093d : Devanagari Sign Avagraha
            0x02da,  // U+093e : Devanagari Vowel Sign Aa
            0x02db,  // U+093f : Devanagari Vowel Sign I
            0x02dc,  // U+0940 : Devanagari Vowel Sign Ii
            0x02dd,  // U+0941 : Devanagari Vowel Sign U
            0x02de,  // U+0942 : Devanagari Vowel Sign Uu
            0x02df,  // U+0943 : Devanagari Vowel Sign Vocalic R
            0x12df,  // U+0944 : Devanagari Vowel Sign Vocalic Rr
            0x02e3,  // U+0945 : Devanagari Vowel Sign Candra E
            0x02e0,  // U+0946 : Devanagari Vowel Sign Short E
            0x02e1,  // U+0947 : Devanagari Vowel Sign E
            0x02e2,  // U+0948 : Devanagari Vowel Sign Ai
            0x02e7,  // U+0949 : Devanagari Vowel Sign Candra O
            0x02e4,  // U+094a : Devanagari Vowel Sign Short O
            0x02e5,  // U+094b : Devanagari Vowel Sign O
            0x02e6,  // U+094c : Devanagari Vowel Sign Au
            0x02e8,  // U+094d : Devanagari Sign Virama
            0x0000,  // U+094e : Undefined
            0x0000,  // U+094f : Undefined
            0x12a1,  // U+0950 : Devanagari Om
            0x0000,  // U+0951 : Devanagari Stress Sign Udatta
            0x22f0,  // U+0952 : Devanagari Stress Sign Anudatta
            0x0000,  // U+0953 : Devanagari Grave Accent
            0x0000,  // U+0954 : Devanagari Acute Accent
            0x0000,  // U+0955 : Undefined
            0x0000,  // U+0956 : Undefined
            0x0000,  // U+0957 : Undefined
            0x12b3,  // U+0958 : Devanagari Letter Qa
            0x12b4,  // U+0959 : Devanagari Letter Khha
            0x12b5,  // U+095a : Devanagari Letter Ghha
            0x12ba,  // U+095b : Devanagari Letter Za
            0x12bf,  // U+095c : Devanagari Letter Dddha
            0x12c0,  // U+095d : Devanagari Letter Rha
            0x12c9,  // U+095e : Devanagari Letter Fa
            0x02ce,  // U+095f : Devanagari Letter Yya
            0x12aa,  // U+0960 : Devanagari Letter Vocalic Rr
            0x12a7,  // U+0961 : Devanagari Letter Vocalic Ll
            0x12db,  // U+0962 : Devanagari Vowel Sign Vocalic L
            0x12dc,  // U+0963 : Devanagari Vowel Sign Vocalic Ll
            0x02ea,  // U+0964 : Devanagari Danda
            0x0000,  // U+0965 : Devanagari Double Danda
            0x02f1,  // U+0966 : Devanagari Digit Zero
            0x02f2,  // U+0967 : Devanagari Digit One
            0x02f3,  // U+0968 : Devanagari Digit Two
            0x02f4,  // U+0969 : Devanagari Digit Three
            0x02f5,  // U+096a : Devanagari Digit Four
            0x02f6,  // U+096b : Devanagari Digit Five
            0x02f7,  // U+096c : Devanagari Digit Six
            0x02f8,  // U+096d : Devanagari Digit Seven
            0x02f9,  // U+096e : Devanagari Digit Eight
            0x02fa,  // U+096f : Devanagari Digit Nine
            0x32f0,  // U+0970 : Devanagari Abbreviation Sign
            0x0000,  // U+0971 : Undefined
            0x0000,  // U+0972 : Undefined
            0x0000,  // U+0973 : Undefined
            0x0000,  // U+0974 : Undefined
            0x0000,  // U+0975 : Undefined
            0x0000,  // U+0976 : Undefined
            0x0000,  // U+0977 : Undefined
            0x0000,  // U+0978 : Undefined
            0x0000,  // U+0979 : Undefined
            0x0000,  // U+097a : Undefined
            0x0000,  // U+097b : Undefined
            0x0000,  // U+097c : Undefined
            0x0000,  // U+097d : Undefined
            0x0000,  // U+097e : Undefined
            0x0000,  // U+097f : Undefined
            0x0000,  // U+0980 : Undefined
            0x03a1,  // U+0981 : Bengali Sign Candrabindu
            0x03a2,  // U+0982 : Bengali Sign Anusvara
            0x03a3,  // U+0983 : Bengali Sign Visarga
            0x0000,  // U+0984 : Undefined
            0x03a4,  // U+0985 : Bengali Letter A
            0x03a5,  // U+0986 : Bengali Letter Aa
            0x03a6,  // U+0987 : Bengali Letter I
            0x03a7,  // U+0988 : Bengali Letter Ii
            0x03a8,  // U+0989 : Bengali Letter U
            0x03a9,  // U+098a : Bengali Letter Uu
            0x03aa,  // U+098b : Bengali Letter Vocalic R
            0x13a6,  // U+098c : Bengali Letter Vocalic L
            0x0000,  // U+098d : Undefined
            0x0000,  // U+098e : Undefined
            0x03ab,  // U+098f : Bengali Letter E
            0x03ad,  // U+0990 : Bengali Letter Ai
            0x0000,  // U+0991 : Undefined
            0x0000,  // U+0992 : Undefined
            0x03af,  // U+0993 : Bengali Letter O
            0x03b1,  // U+0994 : Bengali Letter Au
            0x03b3,  // U+0995 : Bengali Letter Ka
            0x03b4,  // U+0996 : Bengali Letter Kha
            0x03b5,  // U+0997 : Bengali Letter Ga
            0x03b6,  // U+0998 : Bengali Letter Gha
            0x03b7,  // U+0999 : Bengali Letter Nga
            0x03b8,  // U+099a : Bengali Letter Ca
            0x03b9,  // U+099b : Bengali Letter Cha
            0x03ba,  // U+099c : Bengali Letter Ja
            0x03bb,  // U+099d : Bengali Letter Jha
            0x03bc,  // U+099e : Bengali Letter Nya
            0x03bd,  // U+099f : Bengali Letter Tta
            0x03be,  // U+09a0 : Bengali Letter Ttha
            0x03bf,  // U+09a1 : Bengali Letter Dda
            0x03c0,  // U+09a2 : Bengali Letter Ddha
            0x03c1,  // U+09a3 : Bengali Letter Nna
            0x03c2,  // U+09a4 : Bengali Letter Ta
            0x03c3,  // U+09a5 : Bengali Letter Tha
            0x03c4,  // U+09a6 : Bengali Letter Da
            0x03c5,  // U+09a7 : Bengali Letter Dha
            0x03c6,  // U+09a8 : Bengali Letter Na
            0x0000,  // U+09a9 : Undefined
            0x03c8,  // U+09aa : Bengali Letter Pa
            0x03c9,  // U+09ab : Bengali Letter Pha
            0x03ca,  // U+09ac : Bengali Letter Ba
            0x03cb,  // U+09ad : Bengali Letter Bha
            0x03cc,  // U+09ae : Bengali Letter Ma
            0x03cd,  // U+09af : Bengali Letter Ya
            0x03cf,  // U+09b0 : Bengali Letter Ra
            0x0000,  // U+09b1 : Undefined
            0x03d1,  // U+09b2 : Bengali Letter La
            0x0000,  // U+09b3 : Undefined
            0x0000,  // U+09b4 : Undefined
            0x0000,  // U+09b5 : Undefined
            0x03d5,  // U+09b6 : Bengali Letter Sha
            0x03d6,  // U+09b7 : Bengali Letter Ssa
            0x03d7,  // U+09b8 : Bengali Letter Sa
            0x03d8,  // U+09b9 : Bengali Letter Ha
            0x0000,  // U+09ba : Undefined
            0x0000,  // U+09bb : Undefined
            0x03e9,  // U+09bc : Bengali Sign Nukta
            0x0000,  // U+09bd : Undefined
            0x03da,  // U+09be : Bengali Vowel Sign Aa
            0x03db,  // U+09bf : Bengali Vowel Sign I
            0x03dc,  // U+09c0 : Bengali Vowel Sign Ii
            0x03dd,  // U+09c1 : Bengali Vowel Sign U
            0x03de,  // U+09c2 : Bengali Vowel Sign Uu
            0x03df,  // U+09c3 : Bengali Vowel Sign Vocalic R
            0x13df,  // U+09c4 : Bengali Vowel Sign Vocalic Rr
            0x0000,  // U+09c5 : Undefined
            0x0000,  // U+09c6 : Undefined
            0x03e0,  // U+09c7 : Bengali Vowel Sign E
            0x03e2,  // U+09c8 : Bengali Vowel Sign Ai
            0x0000,  // U+09c9 : Undefined
            0x0000,  // U+09ca : Undefined
            0x03e4,  // U+09cb : Bengali Vowel Sign O
            0x03e6,  // U+09cc : Bengali Vowel Sign Au
            0x03e8,  // U+09cd : Bengali Sign Virama
            0x0000,  // U+09ce : Undefined
            0x0000,  // U+09cf : Undefined
            0x0000,  // U+09d0 : Undefined
            0x0000,  // U+09d1 : Undefined
            0x0000,  // U+09d2 : Undefined
            0x0000,  // U+09d3 : Undefined
            0x0000,  // U+09d4 : Undefined
            0x0000,  // U+09d5 : Undefined
            0x0000,  // U+09d6 : Undefined
            0x0000,  // U+09d7 : Bengali Au Length Mark
            0x0000,  // U+09d8 : Undefined
            0x0000,  // U+09d9 : Undefined
            0x0000,  // U+09da : Undefined
            0x0000,  // U+09db : Undefined
            0x13bf,  // U+09dc : Bengali Letter Rra
            0x13c0,  // U+09dd : Bengali Letter Rha
            0x0000,  // U+09de : Undefined
            0x03ce,  // U+09df : Bengali Letter Yya
            0x13aa,  // U+09e0 : Bengali Letter Vocalic Rr
            0x13a7,  // U+09e1 : Bengali Letter Vocalic Ll
            0x13db,  // U+09e2 : Bengali Vowel Sign Vocalic L
            0x13dc,  // U+09e3 : Bengali Vowel Sign Vocalic Ll
            0x0000,  // U+09e4 : Undefined
            0x0000,  // U+09e5 : Undefined
            0x03f1,  // U+09e6 : Bengali Digit Zero
            0x03f2,  // U+09e7 : Bengali Digit One
            0x03f3,  // U+09e8 : Bengali Digit Two
            0x03f4,  // U+09e9 : Bengali Digit Three
            0x03f5,  // U+09ea : Bengali Digit Four
            0x03f6,  // U+09eb : Bengali Digit Five
            0x03f7,  // U+09ec : Bengali Digit Six
            0x03f8,  // U+09ed : Bengali Digit Seven
            0x03f9,  // U+09ee : Bengali Digit Eight
            0x03fa,  // U+09ef : Bengali Digit Nine
            0x0000,  // U+09f0 : Bengali Letter Ra With Middle Diagonal
            0x0000,  // U+09f1 : Bengali Letter Ra With Lower Diagonal
            0x0000,  // U+09f2 : Bengali Rupee Mark
            0x0000,  // U+09f3 : Bengali Rupee Sign
            0x0000,  // U+09f4 : Bengali Currency Numerator One
            0x0000,  // U+09f5 : Bengali Currency Numerator Two
            0x0000,  // U+09f6 : Bengali Currency Numerator Three
            0x0000,  // U+09f7 : Bengali Currency Numerator Four
            0x0000,  // U+09f8 : Bengali Currency Numerator One Less Than The Denominator
            0x0000,  // U+09f9 : Bengali Currency Denominator Sixteen
            0x0000,  // U+09fa : Bengali Isshar
            0x0000,  // U+09fb : Undefined
            0x0000,  // U+09fc : Undefined
            0x0000,  // U+09fd : Undefined
            0x0000,  // U+09fe : Undefined
            0x0000,  // U+09ff : Undefined
            0x0000,  // U+0a00 : Undefined
            0x0000,  // U+0a01 : Undefined
            0x0ba2,  // U+0a02 : Gurmukhi Sign Bindi
            0x0000,  // U+0a03 : Undefined
            0x0000,  // U+0a04 : Undefined
            0x0ba4,  // U+0a05 : Gurmukhi Letter A
            0x0ba5,  // U+0a06 : Gurmukhi Letter Aa
            0x0ba6,  // U+0a07 : Gurmukhi Letter I
            0x0ba7,  // U+0a08 : Gurmukhi Letter Ii
            0x0ba8,  // U+0a09 : Gurmukhi Letter U
            0x0ba9,  // U+0a0a : Gurmukhi Letter Uu
            0x0000,  // U+0a0b : Undefined
            0x0000,  // U+0a0c : Undefined
            0x0000,  // U+0a0d : Undefined
            0x0000,  // U+0a0e : Undefined
            0x0bab,  // U+0a0f : Gurmukhi Letter Ee
            0x0bad,  // U+0a10 : Gurmukhi Letter Ai
            0x0000,  // U+0a11 : Undefined
            0x0000,  // U+0a12 : Undefined
            0x0bb0,  // U+0a13 : Gurmukhi Letter Oo
            0x0bb1,  // U+0a14 : Gurmukhi Letter Au
            0x0bb3,  // U+0a15 : Gurmukhi Letter Ka
            0x0bb4,  // U+0a16 : Gurmukhi Letter Kha
            0x0bb5,  // U+0a17 : Gurmukhi Letter Ga
            0x0bb6,  // U+0a18 : Gurmukhi Letter Gha
            0x0bb7,  // U+0a19 : Gurmukhi Letter Nga
            0x0bb8,  // U+0a1a : Gurmukhi Letter Ca
            0x0bb9,  // U+0a1b : Gurmukhi Letter Cha
            0x0bba,  // U+0a1c : Gurmukhi Letter Ja
            0x0bbb,  // U+0a1d : Gurmukhi Letter Jha
            0x0bbc,  // U+0a1e : Gurmukhi Letter Nya
            0x0bbd,  // U+0a1f : Gurmukhi Letter Tta
            0x0bbe,  // U+0a20 : Gurmukhi Letter Ttha
            0x0bbf,  // U+0a21 : Gurmukhi Letter Dda
            0x0bc0,  // U+0a22 : Gurmukhi Letter Ddha
            0x0bc1,  // U+0a23 : Gurmukhi Letter Nna
            0x0bc2,  // U+0a24 : Gurmukhi Letter Ta
            0x0bc3,  // U+0a25 : Gurmukhi Letter Tha
            0x0bc4,  // U+0a26 : Gurmukhi Letter Da
            0x0bc5,  // U+0a27 : Gurmukhi Letter Dha
            0x0bc6,  // U+0a28 : Gurmukhi Letter Na
            0x0000,  // U+0a29 : Undefined
            0x0bc8,  // U+0a2a : Gurmukhi Letter Pa
            0x0bc9,  // U+0a2b : Gurmukhi Letter Pha
            0x0bca,  // U+0a2c : Gurmukhi Letter Ba
            0x0bcb,  // U+0a2d : Gurmukhi Letter Bha
            0x0bcc,  // U+0a2e : Gurmukhi Letter Ma
            0x0bcd,  // U+0a2f : Gurmukhi Letter Ya
            0x0bcf,  // U+0a30 : Gurmukhi Letter Ra
            0x0000,  // U+0a31 : Undefined
            0x0bd1,  // U+0a32 : Gurmukhi Letter La
            0x0bd2,  // U+0a33 : Gurmukhi Letter Lla
            0x0000,  // U+0a34 : Undefined
            0x0bd4,  // U+0a35 : Gurmukhi Letter Va
            0x0bd5,  // U+0a36 : Gurmukhi Letter Sha
            0x0000,  // U+0a37 : Undefined
            0x0bd7,  // U+0a38 : Gurmukhi Letter Sa
            0x0bd8,  // U+0a39 : Gurmukhi Letter Ha
            0x0000,  // U+0a3a : Undefined
            0x0000,  // U+0a3b : Undefined
            0x0be9,  // U+0a3c : Gurmukhi Sign Nukta
            0x0000,  // U+0a3d : Undefined
            0x0bda,  // U+0a3e : Gurmukhi Vowel Sign Aa
            0x0bdb,  // U+0a3f : Gurmukhi Vowel Sign I
            0x0bdc,  // U+0a40 : Gurmukhi Vowel Sign Ii
            0x0bdd,  // U+0a41 : Gurmukhi Vowel Sign U
            0x0bde,  // U+0a42 : Gurmukhi Vowel Sign Uu
            0x0000,  // U+0a43 : Undefined
            0x0000,  // U+0a44 : Undefined
            0x0000,  // U+0a45 : Undefined
            0x0000,  // U+0a46 : Undefined
            0x0be0,  // U+0a47 : Gurmukhi Vowel Sign Ee
            0x0be2,  // U+0a48 : Gurmukhi Vowel Sign Ai
            0x0000,  // U+0a49 : Undefined
            0x0000,  // U+0a4a : Undefined
            0x0be4,  // U+0a4b : Gurmukhi Vowel Sign Oo
            0x0be6,  // U+0a4c : Gurmukhi Vowel Sign Au
            0x0be8,  // U+0a4d : Gurmukhi Sign Virama
            0x0000,  // U+0a4e : Undefined
            0x0000,  // U+0a4f : Undefined
            0x0000,  // U+0a50 : Undefined
            0x0000,  // U+0a51 : Undefined
            0x0000,  // U+0a52 : Undefined
            0x0000,  // U+0a53 : Undefined
            0x0000,  // U+0a54 : Undefined
            0x0000,  // U+0a55 : Undefined
            0x0000,  // U+0a56 : Undefined
            0x0000,  // U+0a57 : Undefined
            0x0000,  // U+0a58 : Undefined
            0x1bb4,  // U+0a59 : Gurmukhi Letter Khha
            0x1bb5,  // U+0a5a : Gurmukhi Letter Ghha
            0x1bba,  // U+0a5b : Gurmukhi Letter Za
            0x1bc0,  // U+0a5c : Gurmukhi Letter Rra
            0x0000,  // U+0a5d : Undefined
            0x1bc9,  // U+0a5e : Gurmukhi Letter Fa
            0x0000,  // U+0a5f : Undefined
            0x0000,  // U+0a60 : Undefined
            0x0000,  // U+0a61 : Undefined
            0x0000,  // U+0a62 : Undefined
            0x0000,  // U+0a63 : Undefined
            0x0000,  // U+0a64 : Undefined
            0x0000,  // U+0a65 : Undefined
            0x0bf1,  // U+0a66 : Gurmukhi Digit Zero
            0x0bf2,  // U+0a67 : Gurmukhi Digit One
            0x0bf3,  // U+0a68 : Gurmukhi Digit Two
            0x0bf4,  // U+0a69 : Gurmukhi Digit Three
            0x0bf5,  // U+0a6a : Gurmukhi Digit Four
            0x0bf6,  // U+0a6b : Gurmukhi Digit Five
            0x0bf7,  // U+0a6c : Gurmukhi Digit Six
            0x0bf8,  // U+0a6d : Gurmukhi Digit Seven
            0x0bf9,  // U+0a6e : Gurmukhi Digit Eight
            0x0bfa,  // U+0a6f : Gurmukhi Digit Nine
            0x0000,  // U+0a70 : Gurmukhi Tippi
            0x0000,  // U+0a71 : Gurmukhi Addak
            0x0000,  // U+0a72 : Gurmukhi Iri
            0x0000,  // U+0a73 : Gurmukhi Ura
            0x0000,  // U+0a74 : Gurmukhi Ek Onkar
            0x0000,  // U+0a75 : Undefined
            0x0000,  // U+0a76 : Undefined
            0x0000,  // U+0a77 : Undefined
            0x0000,  // U+0a78 : Undefined
            0x0000,  // U+0a79 : Undefined
            0x0000,  // U+0a7a : Undefined
            0x0000,  // U+0a7b : Undefined
            0x0000,  // U+0a7c : Undefined
            0x0000,  // U+0a7d : Undefined
            0x0000,  // U+0a7e : Undefined
            0x0000,  // U+0a7f : Undefined
            0x0000,  // U+0a80 : Undefined
            0x0aa1,  // U+0a81 : Gujarati Sign Candrabindu
            0x0aa2,  // U+0a82 : Gujarati Sign Anusvara
            0x0aa3,  // U+0a83 : Gujarati Sign Visarga
            0x0000,  // U+0a84 : Undefined
            0x0aa4,  // U+0a85 : Gujarati Letter A
            0x0aa5,  // U+0a86 : Gujarati Letter Aa
            0x0aa6,  // U+0a87 : Gujarati Letter I
            0x0aa7,  // U+0a88 : Gujarati Letter Ii
            0x0aa8,  // U+0a89 : Gujarati Letter U
            0x0aa9,  // U+0a8a : Gujarati Letter Uu
            0x0aaa,  // U+0a8b : Gujarati Letter Vocalic R
            0x0000,  // U+0a8c : Undefined
            0x0aae,  // U+0a8d : Gujarati Vowel Candra E
            0x0000,  // U+0a8e : Undefined
            0x0aab,  // U+0a8f : Gujarati Letter E
            0x0aad,  // U+0a90 : Gujarati Letter Ai
            0x0ab2,  // U+0a91 : Gujarati Vowel Candra O
            0x0000,  // U+0a92 : Undefined
            0x0ab0,  // U+0a93 : Gujarati Letter O
            0x0ab1,  // U+0a94 : Gujarati Letter Au
            0x0ab3,  // U+0a95 : Gujarati Letter Ka
            0x0ab4,  // U+0a96 : Gujarati Letter Kha
            0x0ab5,  // U+0a97 : Gujarati Letter Ga
            0x0ab6,  // U+0a98 : Gujarati Letter Gha
            0x0ab7,  // U+0a99 : Gujarati Letter Nga
            0x0ab8,  // U+0a9a : Gujarati Letter Ca
            0x0ab9,  // U+0a9b : Gujarati Letter Cha
            0x0aba,  // U+0a9c : Gujarati Letter Ja
            0x0abb,  // U+0a9d : Gujarati Letter Jha
            0x0abc,  // U+0a9e : Gujarati Letter Nya
            0x0abd,  // U+0a9f : Gujarati Letter Tta
            0x0abe,  // U+0aa0 : Gujarati Letter Ttha
            0x0abf,  // U+0aa1 : Gujarati Letter Dda
            0x0ac0,  // U+0aa2 : Gujarati Letter Ddha
            0x0ac1,  // U+0aa3 : Gujarati Letter Nna
            0x0ac2,  // U+0aa4 : Gujarati Letter Ta
            0x0ac3,  // U+0aa5 : Gujarati Letter Tha
            0x0ac4,  // U+0aa6 : Gujarati Letter Da
            0x0ac5,  // U+0aa7 : Gujarati Letter Dha
            0x0ac6,  // U+0aa8 : Gujarati Letter Na
            0x0000,  // U+0aa9 : Undefined
            0x0ac8,  // U+0aaa : Gujarati Letter Pa
            0x0ac9,  // U+0aab : Gujarati Letter Pha
            0x0aca,  // U+0aac : Gujarati Letter Ba
            0x0acb,  // U+0aad : Gujarati Letter Bha
            0x0acc,  // U+0aae : Gujarati Letter Ma
            0x0acd,  // U+0aaf : Gujarati Letter Ya
            0x0acf,  // U+0ab0 : Gujarati Letter Ra
            0x0000,  // U+0ab1 : Undefined
            0x0ad1,  // U+0ab2 : Gujarati Letter La
            0x0ad2,  // U+0ab3 : Gujarati Letter Lla
            0x0000,  // U+0ab4 : Undefined
            0x0ad4,  // U+0ab5 : Gujarati Letter Va
            0x0ad5,  // U+0ab6 : Gujarati Letter Sha
            0x0ad6,  // U+0ab7 : Gujarati Letter Ssa
            0x0ad7,  // U+0ab8 : Gujarati Letter Sa
            0x0ad8,  // U+0ab9 : Gujarati Letter Ha
            0x0000,  // U+0aba : Undefined
            0x0000,  // U+0abb : Undefined
            0x0ae9,  // U+0abc : Gujarati Sign Nukta
            0x1aea,  // U+0abd : Gujarati Sign Avagraha
            0x0ada,  // U+0abe : Gujarati Vowel Sign Aa
            0x0adb,  // U+0abf : Gujarati Vowel Sign I
            0x0adc,  // U+0ac0 : Gujarati Vowel Sign Ii
            0x0add,  // U+0ac1 : Gujarati Vowel Sign U
            0x0ade,  // U+0ac2 : Gujarati Vowel Sign Uu
            0x0adf,  // U+0ac3 : Gujarati Vowel Sign Vocalic R
            0x1adf,  // U+0ac4 : Gujarati Vowel Sign Vocalic Rr
            0x0ae3,  // U+0ac5 : Gujarati Vowel Sign Candra E
            0x0000,  // U+0ac6 : Undefined
            0x0ae0,  // U+0ac7 : Gujarati Vowel Sign E
            0x0ae2,  // U+0ac8 : Gujarati Vowel Sign Ai
            0x0ae7,  // U+0ac9 : Gujarati Vowel Sign Candra O
            0x0000,  // U+0aca : Undefined
            0x0ae4,  // U+0acb : Gujarati Vowel Sign O
            0x0ae6,  // U+0acc : Gujarati Vowel Sign Au
            0x0ae8,  // U+0acd : Gujarati Sign Virama
            0x0000,  // U+0ace : Undefined
            0x0000,  // U+0acf : Undefined
            0x1aa1,  // U+0ad0 : Gujarati Om
            0x0000,  // U+0ad1 : Undefined
            0x0000,  // U+0ad2 : Undefined
            0x0000,  // U+0ad3 : Undefined
            0x0000,  // U+0ad4 : Undefined
            0x0000,  // U+0ad5 : Undefined
            0x0000,  // U+0ad6 : Undefined
            0x0000,  // U+0ad7 : Undefined
            0x0000,  // U+0ad8 : Undefined
            0x0000,  // U+0ad9 : Undefined
            0x0000,  // U+0ada : Undefined
            0x0000,  // U+0adb : Undefined
            0x0000,  // U+0adc : Undefined
            0x0000,  // U+0add : Undefined
            0x0000,  // U+0ade : Undefined
            0x0000,  // U+0adf : Undefined
            0x1aaa,  // U+0ae0 : Gujarati Letter Vocalic Rr
            0x0000,  // U+0ae1 : Undefined
            0x0000,  // U+0ae2 : Undefined
            0x0000,  // U+0ae3 : Undefined
            0x0000,  // U+0ae4 : Undefined
            0x0000,  // U+0ae5 : Undefined
            0x0af1,  // U+0ae6 : Gujarati Digit Zero
            0x0af2,  // U+0ae7 : Gujarati Digit One
            0x0af3,  // U+0ae8 : Gujarati Digit Two
            0x0af4,  // U+0ae9 : Gujarati Digit Three
            0x0af5,  // U+0aea : Gujarati Digit Four
            0x0af6,  // U+0aeb : Gujarati Digit Five
            0x0af7,  // U+0aec : Gujarati Digit Six
            0x0af8,  // U+0aed : Gujarati Digit Seven
            0x0af9,  // U+0aee : Gujarati Digit Eight
            0x0afa,  // U+0aef : Gujarati Digit Nine
            0x0000,  // U+0af0 : Undefined
            0x0000,  // U+0af1 : Undefined
            0x0000,  // U+0af2 : Undefined
            0x0000,  // U+0af3 : Undefined
            0x0000,  // U+0af4 : Undefined
            0x0000,  // U+0af5 : Undefined
            0x0000,  // U+0af6 : Undefined
            0x0000,  // U+0af7 : Undefined
            0x0000,  // U+0af8 : Undefined
            0x0000,  // U+0af9 : Undefined
            0x0000,  // U+0afa : Undefined
            0x0000,  // U+0afb : Undefined
            0x0000,  // U+0afc : Undefined
            0x0000,  // U+0afd : Undefined
            0x0000,  // U+0afe : Undefined
            0x0000,  // U+0aff : Undefined
            0x0000,  // U+0b00 : Undefined
            0x07a1,  // U+0b01 : Oriya Sign Candrabindu
            0x07a2,  // U+0b02 : Oriya Sign Anusvara
            0x07a3,  // U+0b03 : Oriya Sign Visarga
            0x0000,  // U+0b04 : Undefined
            0x07a4,  // U+0b05 : Oriya Letter A
            0x07a5,  // U+0b06 : Oriya Letter Aa
            0x07a6,  // U+0b07 : Oriya Letter I
            0x07a7,  // U+0b08 : Oriya Letter Ii
            0x07a8,  // U+0b09 : Oriya Letter U
            0x07a9,  // U+0b0a : Oriya Letter Uu
            0x07aa,  // U+0b0b : Oriya Letter Vocalic R
            0x17a6,  // U+0b0c : Oriya Letter Vocalic L
            0x0000,  // U+0b0d : Undefined
            0x0000,  // U+0b0e : Undefined
            0x07ab,  // U+0b0f : Oriya Letter E
            0x07ad,  // U+0b10 : Oriya Letter Ai
            0x0000,  // U+0b11 : Undefined
            0x0000,  // U+0b12 : Undefined
            0x07b0,  // U+0b13 : Oriya Letter O
            0x07b1,  // U+0b14 : Oriya Letter Au
            0x07b3,  // U+0b15 : Oriya Letter Ka
            0x07b4,  // U+0b16 : Oriya Letter Kha
            0x07b5,  // U+0b17 : Oriya Letter Ga
            0x07b6,  // U+0b18 : Oriya Letter Gha
            0x07b7,  // U+0b19 : Oriya Letter Nga
            0x07b8,  // U+0b1a : Oriya Letter Ca
            0x07b9,  // U+0b1b : Oriya Letter Cha
            0x07ba,  // U+0b1c : Oriya Letter Ja
            0x07bb,  // U+0b1d : Oriya Letter Jha
            0x07bc,  // U+0b1e : Oriya Letter Nya
            0x07bd,  // U+0b1f : Oriya Letter Tta
            0x07be,  // U+0b20 : Oriya Letter Ttha
            0x07bf,  // U+0b21 : Oriya Letter Dda
            0x07c0,  // U+0b22 : Oriya Letter Ddha
            0x07c1,  // U+0b23 : Oriya Letter Nna
            0x07c2,  // U+0b24 : Oriya Letter Ta
            0x07c3,  // U+0b25 : Oriya Letter Tha
            0x07c4,  // U+0b26 : Oriya Letter Da
            0x07c5,  // U+0b27 : Oriya Letter Dha
            0x07c6,  // U+0b28 : Oriya Letter Na
            0x0000,  // U+0b29 : Undefined
            0x07c8,  // U+0b2a : Oriya Letter Pa
            0x07c9,  // U+0b2b : Oriya Letter Pha
            0x07ca,  // U+0b2c : Oriya Letter Ba
            0x07cb,  // U+0b2d : Oriya Letter Bha
            0x07cc,  // U+0b2e : Oriya Letter Ma
            0x07cd,  // U+0b2f : Oriya Letter Ya
            0x07cf,  // U+0b30 : Oriya Letter Ra
            0x0000,  // U+0b31 : Undefined
            0x07d1,  // U+0b32 : Oriya Letter La
            0x07d2,  // U+0b33 : Oriya Letter Lla
            0x0000,  // U+0b34 : Undefined
            0x0000,  // U+0b35 : Undefined
            0x07d5,  // U+0b36 : Oriya Letter Sha
            0x07d6,  // U+0b37 : Oriya Letter Ssa
            0x07d7,  // U+0b38 : Oriya Letter Sa
            0x07d8,  // U+0b39 : Oriya Letter Ha
            0x0000,  // U+0b3a : Undefined
            0x0000,  // U+0b3b : Undefined
            0x07e9,  // U+0b3c : Oriya Sign Nukta
            0x17ea,  // U+0b3d : Oriya Sign Avagraha
            0x07da,  // U+0b3e : Oriya Vowel Sign Aa
            0x07db,  // U+0b3f : Oriya Vowel Sign I
            0x07dc,  // U+0b40 : Oriya Vowel Sign Ii
            0x07dd,  // U+0b41 : Oriya Vowel Sign U
            0x07de,  // U+0b42 : Oriya Vowel Sign Uu
            0x07df,  // U+0b43 : Oriya Vowel Sign Vocalic R
            0x0000,  // U+0b44 : Undefined
            0x0000,  // U+0b45 : Undefined
            0x0000,  // U+0b46 : Undefined
            0x07e0,  // U+0b47 : Oriya Vowel Sign E
            0x07e2,  // U+0b48 : Oriya Vowel Sign Ai
            0x0000,  // U+0b49 : Undefined
            0x0000,  // U+0b4a : Undefined
            0x07e4,  // U+0b4b : Oriya Vowel Sign O
            0x07e6,  // U+0b4c : Oriya Vowel Sign Au
            0x07e8,  // U+0b4d : Oriya Sign Virama
            0x0000,  // U+0b4e : Undefined
            0x0000,  // U+0b4f : Undefined
            0x0000,  // U+0b50 : Undefined
            0x0000,  // U+0b51 : Undefined
            0x0000,  // U+0b52 : Undefined
            0x0000,  // U+0b53 : Undefined
            0x0000,  // U+0b54 : Undefined
            0x0000,  // U+0b55 : Undefined
            0x0000,  // U+0b56 : Oriya Ai Length Mark
            0x0000,  // U+0b57 : Oriya Au Length Mark
            0x0000,  // U+0b58 : Undefined
            0x0000,  // U+0b59 : Undefined
            0x0000,  // U+0b5a : Undefined
            0x0000,  // U+0b5b : Undefined
            0x17bf,  // U+0b5c : Oriya Letter Rra
            0x17c0,  // U+0b5d : Oriya Letter Rha
            0x0000,  // U+0b5e : Undefined
            0x07ce,  // U+0b5f : Oriya Letter Yya
            0x17aa,  // U+0b60 : Oriya Letter Vocalic Rr
            0x17a7,  // U+0b61 : Oriya Letter Vocalic Ll
            0x0000,  // U+0b62 : Undefined
            0x0000,  // U+0b63 : Undefined
            0x0000,  // U+0b64 : Undefined
            0x0000,  // U+0b65 : Undefined
            0x07f1,  // U+0b66 : Oriya Digit Zero
            0x07f2,  // U+0b67 : Oriya Digit One
            0x07f3,  // U+0b68 : Oriya Digit Two
            0x07f4,  // U+0b69 : Oriya Digit Three
            0x07f5,  // U+0b6a : Oriya Digit Four
            0x07f6,  // U+0b6b : Oriya Digit Five
            0x07f7,  // U+0b6c : Oriya Digit Six
            0x07f8,  // U+0b6d : Oriya Digit Seven
            0x07f9,  // U+0b6e : Oriya Digit Eight
            0x07fa,  // U+0b6f : Oriya Digit Nine
            0x0000,  // U+0b70 : Oriya Isshar
            0x0000,  // U+0b71 : Undefined
            0x0000,  // U+0b72 : Undefined
            0x0000,  // U+0b73 : Undefined
            0x0000,  // U+0b74 : Undefined
            0x0000,  // U+0b75 : Undefined
            0x0000,  // U+0b76 : Undefined
            0x0000,  // U+0b77 : Undefined
            0x0000,  // U+0b78 : Undefined
            0x0000,  // U+0b79 : Undefined
            0x0000,  // U+0b7a : Undefined
            0x0000,  // U+0b7b : Undefined
            0x0000,  // U+0b7c : Undefined
            0x0000,  // U+0b7d : Undefined
            0x0000,  // U+0b7e : Undefined
            0x0000,  // U+0b7f : Undefined
            0x0000,  // U+0b80 : Undefined
            0x0000,  // U+0b81 : Undefined
            0x04a2,  // U+0b82 : Tamil Sign Anusvara
            0x04a3,  // U+0b83 : Tamil Sign Visarga
            0x0000,  // U+0b84 : Undefined
            0x04a4,  // U+0b85 : Tamil Letter A
            0x04a5,  // U+0b86 : Tamil Letter Aa
            0x04a6,  // U+0b87 : Tamil Letter I
            0x04a7,  // U+0b88 : Tamil Letter Ii
            0x04a8,  // U+0b89 : Tamil Letter U
            0x04a9,  // U+0b8a : Tamil Letter Uu
            0x0000,  // U+0b8b : Undefined
            0x0000,  // U+0b8c : Undefined
            0x0000,  // U+0b8d : Undefined
            0x0000,  // U+0b8e : Tamil Letter E
            0x04ab,  // U+0b8f : Tamil Letter Ee
            0x04ad,  // U+0b90 : Tamil Letter Ai
            0x0000,  // U+0b91 : Undefined
            0x04af,  // U+0b92 : Tamil Letter O
            0x04b0,  // U+0b93 : Tamil Letter Oo
            0x04b1,  // U+0b94 : Tamil Letter Au
            0x04b3,  // U+0b95 : Tamil Letter Ka
            0x0000,  // U+0b96 : Undefined
            0x0000,  // U+0b97 : Undefined
            0x0000,  // U+0b98 : Undefined
            0x04b7,  // U+0b99 : Tamil Letter Nga
            0x04b8,  // U+0b9a : Tamil Letter Ca
            0x0000,  // U+0b9b : Undefined
            0x04ba,  // U+0b9c : Tamil Letter Ja
            0x0000,  // U+0b9d : Undefined
            0x04bc,  // U+0b9e : Tamil Letter Nya
            0x04bd,  // U+0b9f : Tamil Letter Tta
            0x0000,  // U+0ba0 : Undefined
            0x0000,  // U+0ba1 : Undefined
            0x0000,  // U+0ba2 : Undefined
            0x04c1,  // U+0ba3 : Tamil Letter Nna
            0x04c2,  // U+0ba4 : Tamil Letter Ta
            0x0000,  // U+0ba5 : Undefined
            0x0000,  // U+0ba6 : Undefined
            0x0000,  // U+0ba7 : Undefined
            0x04c6,  // U+0ba8 : Tamil Letter Na
            0x04c7,  // U+0ba9 : Tamil Letter Nnna
            0x04c8,  // U+0baa : Tamil Letter Pa
            0x0000,  // U+0bab : Undefined
            0x0000,  // U+0bac : Undefined
            0x0000,  // U+0bad : Undefined
            0x04cc,  // U+0bae : Tamil Letter Ma
            0x04cd,  // U+0baf : Tamil Letter Ya
            0x04cf,  // U+0bb0 : Tamil Letter Ra
            0x04d0,  // U+0bb1 : Tamil Letter Rra
            0x04d1,  // U+0bb2 : Tamil Letter La
            0x04d2,  // U+0bb3 : Tamil Letter Lla
            0x04d3,  // U+0bb4 : Tamil Letter Llla
            0x04d4,  // U+0bb5 : Tamil Letter Va
            0x0000,  // U+0bb6 : Undefined
            0x04d5,  // U+0bb7 : Tamil Letter Ssa
            0x04d7,  // U+0bb8 : Tamil Letter Sa
            0x04d8,  // U+0bb9 : Tamil Letter Ha
            0x0000,  // U+0bba : Undefined
            0x0000,  // U+0bbb : Undefined
            0x0000,  // U+0bbc : Undefined
            0x0000,  // U+0bbd : Undefined
            0x04da,  // U+0bbe : Tamil Vowel Sign Aa
            0x04db,  // U+0bbf : Tamil Vowel Sign I
            0x04dc,  // U+0bc0 : Tamil Vowel Sign Ii
            0x04dd,  // U+0bc1 : Tamil Vowel Sign U
            0x04de,  // U+0bc2 : Tamil Vowel Sign Uu
            0x0000,  // U+0bc3 : Undefined
            0x0000,  // U+0bc4 : Undefined
            0x0000,  // U+0bc5 : Undefined
            0x04e0,  // U+0bc6 : Tamil Vowel Sign E
            0x04e1,  // U+0bc7 : Tamil Vowel Sign Ee
            0x04e2,  // U+0bc8 : Tamil Vowel Sign Ai
            0x0000,  // U+0bc9 : Undefined
            0x04e4,  // U+0bca : Tamil Vowel Sign O
            0x04e5,  // U+0bcb : Tamil Vowel Sign Oo
            0x04e6,  // U+0bcc : Tamil Vowel Sign Au
            0x04e8,  // U+0bcd : Tamil Sign Virama
            0x0000,  // U+0bce : Undefined
            0x0000,  // U+0bcf : Undefined
            0x0000,  // U+0bd0 : Undefined
            0x0000,  // U+0bd1 : Undefined
            0x0000,  // U+0bd2 : Undefined
            0x0000,  // U+0bd3 : Undefined
            0x0000,  // U+0bd4 : Undefined
            0x0000,  // U+0bd5 : Undefined
            0x0000,  // U+0bd6 : Undefined
            0x0000,  // U+0bd7 : Tamil Au Length Mark
            0x0000,  // U+0bd8 : Undefined
            0x0000,  // U+0bd9 : Undefined
            0x0000,  // U+0bda : Undefined
            0x0000,  // U+0bdb : Undefined
            0x0000,  // U+0bdc : Undefined
            0x0000,  // U+0bdd : Undefined
            0x0000,  // U+0bde : Undefined
            0x0000,  // U+0bdf : Undefined
            0x0000,  // U+0be0 : Undefined
            0x0000,  // U+0be1 : Undefined
            0x0000,  // U+0be2 : Undefined
            0x0000,  // U+0be3 : Undefined
            0x0000,  // U+0be4 : Undefined
            0x0000,  // U+0be5 : Undefined
            0x0000,  // U+0be6 : Undefined
            0x04f2,  // U+0be7 : Tamil Digit One
            0x04f3,  // U+0be8 : Tamil Digit Two
            0x04f4,  // U+0be9 : Tamil Digit Three
            0x04f5,  // U+0bea : Tamil Digit Four
            0x04f6,  // U+0beb : Tamil Digit Five
            0x04f7,  // U+0bec : Tamil Digit Six
            0x04f8,  // U+0bed : Tamil Digit Seven
            0x04f9,  // U+0bee : Tamil Digit Eight
            0x04fa,  // U+0bef : Tamil Digit Nine
            0x0000,  // U+0bf0 : Tamil Number Ten
            0x0000,  // U+0bf1 : Tamil Number One Hundred
            0x0000,  // U+0bf2 : Tamil Number One Thousand
            0x0000,  // U+0bf3 : Undefined
            0x0000,  // U+0bf4 : Undefined
            0x0000,  // U+0bf5 : Undefined
            0x0000,  // U+0bf6 : Undefined
            0x0000,  // U+0bf7 : Undefined
            0x0000,  // U+0bf8 : Undefined
            0x0000,  // U+0bf9 : Undefined
            0x0000,  // U+0bfa : Undefined
            0x0000,  // U+0bfb : Undefined
            0x0000,  // U+0bfc : Undefined
            0x0000,  // U+0bfd : Undefined
            0x0000,  // U+0bfe : Undefined
            0x0000,  // U+0bff : Undefined
            0x0000,  // U+0c00 : Undefined
            0x05a1,  // U+0c01 : Telugu Sign Candrabindu
            0x05a2,  // U+0c02 : Telugu Sign Anusvara
            0x05a3,  // U+0c03 : Telugu Sign Visarga
            0x0000,  // U+0c04 : Undefined
            0x05a4,  // U+0c05 : Telugu Letter A
            0x05a5,  // U+0c06 : Telugu Letter Aa
            0x05a6,  // U+0c07 : Telugu Letter I
            0x05a7,  // U+0c08 : Telugu Letter Ii
            0x05a8,  // U+0c09 : Telugu Letter U
            0x05a9,  // U+0c0a : Telugu Letter Uu
            0x05aa,  // U+0c0b : Telugu Letter Vocalic R
            0x15a6,  // U+0c0c : Telugu Letter Vocalic L
            0x0000,  // U+0c0d : Undefined
            0x05ab,  // U+0c0e : Telugu Letter E
            0x05ac,  // U+0c0f : Telugu Letter Ee
            0x05ad,  // U+0c10 : Telugu Letter Ai
            0x0000,  // U+0c11 : Undefined
            0x05af,  // U+0c12 : Telugu Letter O
            0x05b0,  // U+0c13 : Telugu Letter Oo
            0x05b1,  // U+0c14 : Telugu Letter Au
            0x05b3,  // U+0c15 : Telugu Letter Ka
            0x05b4,  // U+0c16 : Telugu Letter Kha
            0x05b5,  // U+0c17 : Telugu Letter Ga
            0x05b6,  // U+0c18 : Telugu Letter Gha
            0x05b7,  // U+0c19 : Telugu Letter Nga
            0x05b8,  // U+0c1a : Telugu Letter Ca
            0x05b9,  // U+0c1b : Telugu Letter Cha
            0x05ba,  // U+0c1c : Telugu Letter Ja
            0x05bb,  // U+0c1d : Telugu Letter Jha
            0x05bc,  // U+0c1e : Telugu Letter Nya
            0x05bd,  // U+0c1f : Telugu Letter Tta
            0x05be,  // U+0c20 : Telugu Letter Ttha
            0x05bf,  // U+0c21 : Telugu Letter Dda
            0x05c0,  // U+0c22 : Telugu Letter Ddha
            0x05c1,  // U+0c23 : Telugu Letter Nna
            0x05c2,  // U+0c24 : Telugu Letter Ta
            0x05c3,  // U+0c25 : Telugu Letter Tha
            0x05c4,  // U+0c26 : Telugu Letter Da
            0x05c5,  // U+0c27 : Telugu Letter Dha
            0x05c6,  // U+0c28 : Telugu Letter Na
            0x0000,  // U+0c29 : Undefined
            0x05c8,  // U+0c2a : Telugu Letter Pa
            0x05c9,  // U+0c2b : Telugu Letter Pha
            0x05ca,  // U+0c2c : Telugu Letter Ba
            0x05cb,  // U+0c2d : Telugu Letter Bha
            0x05cc,  // U+0c2e : Telugu Letter Ma
            0x05cd,  // U+0c2f : Telugu Letter Ya
            0x05cf,  // U+0c30 : Telugu Letter Ra
            0x05d0,  // U+0c31 : Telugu Letter Rra
            0x05d1,  // U+0c32 : Telugu Letter La
            0x05d2,  // U+0c33 : Telugu Letter Lla
            0x0000,  // U+0c34 : Undefined
            0x05d4,  // U+0c35 : Telugu Letter Va
            0x05d5,  // U+0c36 : Telugu Letter Sha
            0x05d6,  // U+0c37 : Telugu Letter Ssa
            0x05d7,  // U+0c38 : Telugu Letter Sa
            0x05d8,  // U+0c39 : Telugu Letter Ha
            0x0000,  // U+0c3a : Undefined
            0x0000,  // U+0c3b : Undefined
            0x0000,  // U+0c3c : Undefined
            0x0000,  // U+0c3d : Undefined
            0x05da,  // U+0c3e : Telugu Vowel Sign Aa
            0x05db,  // U+0c3f : Telugu Vowel Sign I
            0x05dc,  // U+0c40 : Telugu Vowel Sign Ii
            0x05dd,  // U+0c41 : Telugu Vowel Sign U
            0x05de,  // U+0c42 : Telugu Vowel Sign Uu
            0x05df,  // U+0c43 : Telugu Vowel Sign Vocalic R
            0x15df,  // U+0c44 : Telugu Vowel Sign Vocalic Rr
            0x0000,  // U+0c45 : Undefined
            0x05e0,  // U+0c46 : Telugu Vowel Sign E
            0x05e1,  // U+0c47 : Telugu Vowel Sign Ee
            0x05e2,  // U+0c48 : Telugu Vowel Sign Ai
            0x0000,  // U+0c49 : Undefined
            0x05e4,  // U+0c4a : Telugu Vowel Sign O
            0x05e5,  // U+0c4b : Telugu Vowel Sign Oo
            0x05e6,  // U+0c4c : Telugu Vowel Sign Au
            0x05e8,  // U+0c4d : Telugu Sign Virama
            0x0000,  // U+0c4e : Undefined
            0x0000,  // U+0c4f : Undefined
            0x0000,  // U+0c50 : Undefined
            0x0000,  // U+0c51 : Undefined
            0x0000,  // U+0c52 : Undefined
            0x0000,  // U+0c53 : Undefined
            0x0000,  // U+0c54 : Undefined
            0x0000,  // U+0c55 : Telugu Length Mark
            0x0000,  // U+0c56 : Telugu Ai Length Mark
            0x0000,  // U+0c57 : Undefined
            0x0000,  // U+0c58 : Undefined
            0x0000,  // U+0c59 : Undefined
            0x0000,  // U+0c5a : Undefined
            0x0000,  // U+0c5b : Undefined
            0x0000,  // U+0c5c : Undefined
            0x0000,  // U+0c5d : Undefined
            0x0000,  // U+0c5e : Undefined
            0x0000,  // U+0c5f : Undefined
            0x15aa,  // U+0c60 : Telugu Letter Vocalic Rr
            0x15a7,  // U+0c61 : Telugu Letter Vocalic Ll
            0x0000,  // U+0c62 : Undefined
            0x0000,  // U+0c63 : Undefined
            0x0000,  // U+0c64 : Undefined
            0x0000,  // U+0c65 : Undefined
            0x05f1,  // U+0c66 : Telugu Digit Zero
            0x05f2,  // U+0c67 : Telugu Digit One
            0x05f3,  // U+0c68 : Telugu Digit Two
            0x05f4,  // U+0c69 : Telugu Digit Three
            0x05f5,  // U+0c6a : Telugu Digit Four
            0x05f6,  // U+0c6b : Telugu Digit Five
            0x05f7,  // U+0c6c : Telugu Digit Six
            0x05f8,  // U+0c6d : Telugu Digit Seven
            0x05f9,  // U+0c6e : Telugu Digit Eight
            0x05fa,  // U+0c6f : Telugu Digit Nine
            0x0000,  // U+0c70 : Undefined
            0x0000,  // U+0c71 : Undefined
            0x0000,  // U+0c72 : Undefined
            0x0000,  // U+0c73 : Undefined
            0x0000,  // U+0c74 : Undefined
            0x0000,  // U+0c75 : Undefined
            0x0000,  // U+0c76 : Undefined
            0x0000,  // U+0c77 : Undefined
            0x0000,  // U+0c78 : Undefined
            0x0000,  // U+0c79 : Undefined
            0x0000,  // U+0c7a : Undefined
            0x0000,  // U+0c7b : Undefined
            0x0000,  // U+0c7c : Undefined
            0x0000,  // U+0c7d : Undefined
            0x0000,  // U+0c7e : Undefined
            0x0000,  // U+0c7f : Undefined
            0x0000,  // U+0c80 : Undefined
            0x0000,  // U+0c81 : Undefined
            0x08a2,  // U+0c82 : Kannada Sign Anusvara
            0x08a3,  // U+0c83 : Kannada Sign Visarga
            0x0000,  // U+0c84 : Undefined
            0x08a4,  // U+0c85 : Kannada Letter A
            0x08a5,  // U+0c86 : Kannada Letter Aa
            0x08a6,  // U+0c87 : Kannada Letter I
            0x08a7,  // U+0c88 : Kannada Letter Ii
            0x08a8,  // U+0c89 : Kannada Letter U
            0x08a9,  // U+0c8a : Kannada Letter Uu
            0x08aa,  // U+0c8b : Kannada Letter Vocalic R
            0x18a6,  // U+0c8c : Kannada Letter Vocalic L
            0x0000,  // U+0c8d : Undefined
            0x08ab,  // U+0c8e : Kannada Letter E
            0x08ac,  // U+0c8f : Kannada Letter Ee
            0x08ad,  // U+0c90 : Kannada Letter Ai
            0x0000,  // U+0c91 : Undefined
            0x08af,  // U+0c92 : Kannada Letter O
            0x08b0,  // U+0c93 : Kannada Letter Oo
            0x08b1,  // U+0c94 : Kannada Letter Au
            0x08b3,  // U+0c95 : Kannada Letter Ka
            0x08b4,  // U+0c96 : Kannada Letter Kha
            0x08b5,  // U+0c97 : Kannada Letter Ga
            0x08b6,  // U+0c98 : Kannada Letter Gha
            0x08b7,  // U+0c99 : Kannada Letter Nga
            0x08b8,  // U+0c9a : Kannada Letter Ca
            0x08b9,  // U+0c9b : Kannada Letter Cha
            0x08ba,  // U+0c9c : Kannada Letter Ja
            0x08bb,  // U+0c9d : Kannada Letter Jha
            0x08bc,  // U+0c9e : Kannada Letter Nya
            0x08bd,  // U+0c9f : Kannada Letter Tta
            0x08be,  // U+0ca0 : Kannada Letter Ttha
            0x08bf,  // U+0ca1 : Kannada Letter Dda
            0x08c0,  // U+0ca2 : Kannada Letter Ddha
            0x08c1,  // U+0ca3 : Kannada Letter Nna
            0x08c2,  // U+0ca4 : Kannada Letter Ta
            0x08c3,  // U+0ca5 : Kannada Letter Tha
            0x08c4,  // U+0ca6 : Kannada Letter Da
            0x08c5,  // U+0ca7 : Kannada Letter Dha
            0x08c6,  // U+0ca8 : Kannada Letter Na
            0x0000,  // U+0ca9 : Undefined
            0x08c8,  // U+0caa : Kannada Letter Pa
            0x08c9,  // U+0cab : Kannada Letter Pha
            0x08ca,  // U+0cac : Kannada Letter Ba
            0x08cb,  // U+0cad : Kannada Letter Bha
            0x08cc,  // U+0cae : Kannada Letter Ma
            0x08cd,  // U+0caf : Kannada Letter Ya
            0x08cf,  // U+0cb0 : Kannada Letter Ra
            0x08d0,  // U+0cb1 : Kannada Letter Rra
            0x08d1,  // U+0cb2 : Kannada Letter La
            0x08d2,  // U+0cb3 : Kannada Letter Lla
            0x0000,  // U+0cb4 : Undefined
            0x08d4,  // U+0cb5 : Kannada Letter Va
            0x08d5,  // U+0cb6 : Kannada Letter Sha
            0x08d6,  // U+0cb7 : Kannada Letter Ssa
            0x08d7,  // U+0cb8 : Kannada Letter Sa
            0x08d8,  // U+0cb9 : Kannada Letter Ha
            0x0000,  // U+0cba : Undefined
            0x0000,  // U+0cbb : Undefined
            0x0000,  // U+0cbc : Undefined
            0x0000,  // U+0cbd : Undefined
            0x08da,  // U+0cbe : Kannada Vowel Sign Aa
            0x08db,  // U+0cbf : Kannada Vowel Sign I
            0x08dc,  // U+0cc0 : Kannada Vowel Sign Ii
            0x08dd,  // U+0cc1 : Kannada Vowel Sign U
            0x08de,  // U+0cc2 : Kannada Vowel Sign Uu
            0x08df,  // U+0cc3 : Kannada Vowel Sign Vocalic R
            0x18df,  // U+0cc4 : Kannada Vowel Sign Vocalic Rr
            0x0000,  // U+0cc5 : Undefined
            0x08e0,  // U+0cc6 : Kannada Vowel Sign E
            0x08e1,  // U+0cc7 : Kannada Vowel Sign Ee
            0x08e2,  // U+0cc8 : Kannada Vowel Sign Ai
            0x0000,  // U+0cc9 : Undefined
            0x08e4,  // U+0cca : Kannada Vowel Sign O
            0x08e5,  // U+0ccb : Kannada Vowel Sign Oo
            0x08e6,  // U+0ccc : Kannada Vowel Sign Au
            0x08e8,  // U+0ccd : Kannada Sign Virama
            0x0000,  // U+0cce : Undefined
            0x0000,  // U+0ccf : Undefined
            0x0000,  // U+0cd0 : Undefined
            0x0000,  // U+0cd1 : Undefined
            0x0000,  // U+0cd2 : Undefined
            0x0000,  // U+0cd3 : Undefined
            0x0000,  // U+0cd4 : Undefined
            0x0000,  // U+0cd5 : Kannada Length Mark
            0x0000,  // U+0cd6 : Kannada Ai Length Mark
            0x0000,  // U+0cd7 : Undefined
            0x0000,  // U+0cd8 : Undefined
            0x0000,  // U+0cd9 : Undefined
            0x0000,  // U+0cda : Undefined
            0x0000,  // U+0cdb : Undefined
            0x0000,  // U+0cdc : Undefined
            0x0000,  // U+0cdd : Undefined
            0x18c9,  // U+0cde : Kannada Letter Fa
            0x0000,  // U+0cdf : Undefined
            0x18aa,  // U+0ce0 : Kannada Letter Vocalic Rr
            0x18a7,  // U+0ce1 : Kannada Letter Vocalic Ll
            0x0000,  // U+0ce2 : Undefined
            0x0000,  // U+0ce3 : Undefined
            0x0000,  // U+0ce4 : Undefined
            0x0000,  // U+0ce5 : Undefined
            0x08f1,  // U+0ce6 : Kannada Digit Zero
            0x08f2,  // U+0ce7 : Kannada Digit One
            0x08f3,  // U+0ce8 : Kannada Digit Two
            0x08f4,  // U+0ce9 : Kannada Digit Three
            0x08f5,  // U+0cea : Kannada Digit Four
            0x08f6,  // U+0ceb : Kannada Digit Five
            0x08f7,  // U+0cec : Kannada Digit Six
            0x08f8,  // U+0ced : Kannada Digit Seven
            0x08f9,  // U+0cee : Kannada Digit Eight
            0x08fa,  // U+0cef : Kannada Digit Nine
            0x0000,  // U+0cf0 : Undefined
            0x0000,  // U+0cf1 : Undefined
            0x0000,  // U+0cf2 : Undefined
            0x0000,  // U+0cf3 : Undefined
            0x0000,  // U+0cf4 : Undefined
            0x0000,  // U+0cf5 : Undefined
            0x0000,  // U+0cf6 : Undefined
            0x0000,  // U+0cf7 : Undefined
            0x0000,  // U+0cf8 : Undefined
            0x0000,  // U+0cf9 : Undefined
            0x0000,  // U+0cfa : Undefined
            0x0000,  // U+0cfb : Undefined
            0x0000,  // U+0cfc : Undefined
            0x0000,  // U+0cfd : Undefined
            0x0000,  // U+0cfe : Undefined
            0x0000,  // U+0cff : Undefined
            0x0000,  // U+0d00 : Undefined
            0x0000,  // U+0d01 : Undefined
            0x09a2,  // U+0d02 : Malayalam Sign Anusvara
            0x09a3,  // U+0d03 : Malayalam Sign Visarga
            0x0000,  // U+0d04 : Undefined
            0x09a4,  // U+0d05 : Malayalam Letter A
            0x09a5,  // U+0d06 : Malayalam Letter Aa
            0x09a6,  // U+0d07 : Malayalam Letter I
            0x09a7,  // U+0d08 : Malayalam Letter Ii
            0x09a8,  // U+0d09 : Malayalam Letter U
            0x09a9,  // U+0d0a : Malayalam Letter Uu
            0x09aa,  // U+0d0b : Malayalam Letter Vocalic R
            0x19a6,  // U+0d0c : Malayalam Letter Vocalic L
            0x0000,  // U+0d0d : Undefined
            0x09ab,  // U+0d0e : Malayalam Letter E
            0x09ac,  // U+0d0f : Malayalam Letter Ee
            0x09ad,  // U+0d10 : Malayalam Letter Ai
            0x0000,  // U+0d11 : Undefined
            0x09af,  // U+0d12 : Malayalam Letter O
            0x09b0,  // U+0d13 : Malayalam Letter Oo
            0x09b1,  // U+0d14 : Malayalam Letter Au
            0x09b3,  // U+0d15 : Malayalam Letter Ka
            0x09b4,  // U+0d16 : Malayalam Letter Kha
            0x09b5,  // U+0d17 : Malayalam Letter Ga
            0x09b6,  // U+0d18 : Malayalam Letter Gha
            0x09b7,  // U+0d19 : Malayalam Letter Nga
            0x09b8,  // U+0d1a : Malayalam Letter Ca
            0x09b9,  // U+0d1b : Malayalam Letter Cha
            0x09ba,  // U+0d1c : Malayalam Letter Ja
            0x09bb,  // U+0d1d : Malayalam Letter Jha
            0x09bc,  // U+0d1e : Malayalam Letter Nya
            0x09bd,  // U+0d1f : Malayalam Letter Tta
            0x09be,  // U+0d20 : Malayalam Letter Ttha
            0x09bf,  // U+0d21 : Malayalam Letter Dda
            0x09c0,  // U+0d22 : Malayalam Letter Ddha
            0x09c1,  // U+0d23 : Malayalam Letter Nna
            0x09c2,  // U+0d24 : Malayalam Letter Ta
            0x09c3,  // U+0d25 : Malayalam Letter Tha
            0x09c4,  // U+0d26 : Malayalam Letter Da
            0x09c5,  // U+0d27 : Malayalam Letter Dha
            0x09c6,  // U+0d28 : Malayalam Letter Na
            0x0000,  // U+0d29 : Undefined
            0x09c8,  // U+0d2a : Malayalam Letter Pa
            0x09c9,  // U+0d2b : Malayalam Letter Pha
            0x09ca,  // U+0d2c : Malayalam Letter Ba
            0x09cb,  // U+0d2d : Malayalam Letter Bha
            0x09cc,  // U+0d2e : Malayalam Letter Ma
            0x09cd,  // U+0d2f : Malayalam Letter Ya
            0x09cf,  // U+0d30 : Malayalam Letter Ra
            0x09d0,  // U+0d31 : Malayalam Letter Rra
            0x09d1,  // U+0d32 : Malayalam Letter La
            0x09d2,  // U+0d33 : Malayalam Letter Lla
            0x09d3,  // U+0d34 : Malayalam Letter Llla
            0x09d4,  // U+0d35 : Malayalam Letter Va
            0x09d5,  // U+0d36 : Malayalam Letter Sha
            0x09d6,  // U+0d37 : Malayalam Letter Ssa
            0x09d7,  // U+0d38 : Malayalam Letter Sa
            0x09d8,  // U+0d39 : Malayalam Letter Ha
            0x0000,  // U+0d3a : Undefined
            0x0000,  // U+0d3b : Undefined
            0x0000,  // U+0d3c : Undefined
            0x0000,  // U+0d3d : Undefined
            0x09da,  // U+0d3e : Malayalam Vowel Sign Aa
            0x09db,  // U+0d3f : Malayalam Vowel Sign I
            0x09dc,  // U+0d40 : Malayalam Vowel Sign Ii
            0x09dd,  // U+0d41 : Malayalam Vowel Sign U
            0x09de,  // U+0d42 : Malayalam Vowel Sign Uu
            0x09df,  // U+0d43 : Malayalam Vowel Sign Vocalic R
            0x0000,  // U+0d44 : Undefined
            0x0000,  // U+0d45 : Undefined
            0x09e0,  // U+0d46 : Malayalam Vowel Sign E
            0x09e1,  // U+0d47 : Malayalam Vowel Sign Ee
            0x09e2,  // U+0d48 : Malayalam Vowel Sign Ai
            0x0000,  // U+0d49 : Undefined
            0x09e4,  // U+0d4a : Malayalam Vowel Sign O
            0x09e5,  // U+0d4b : Malayalam Vowel Sign Oo
            0x09e6,  // U+0d4c : Malayalam Vowel Sign Au
            0x09e8,  // U+0d4d : Malayalam Sign Virama
            0x0000,  // U+0d4e : Undefined
            0x0000,  // U+0d4f : Undefined
            0x0000,  // U+0d50 : Undefined
            0x0000,  // U+0d51 : Undefined
            0x0000,  // U+0d52 : Undefined
            0x0000,  // U+0d53 : Undefined
            0x0000,  // U+0d54 : Undefined
            0x0000,  // U+0d55 : Undefined
            0x0000,  // U+0d56 : Undefined
            0x0000,  // U+0d57 : Malayalam Au Length Mark
            0x0000,  // U+0d58 : Undefined
            0x0000,  // U+0d59 : Undefined
            0x0000,  // U+0d5a : Undefined
            0x0000,  // U+0d5b : Undefined
            0x0000,  // U+0d5c : Undefined
            0x0000,  // U+0d5d : Undefined
            0x0000,  // U+0d5e : Undefined
            0x0000,  // U+0d5f : Undefined
            0x19aa,  // U+0d60 : Malayalam Letter Vocalic Rr
            0x19a7,  // U+0d61 : Malayalam Letter Vocalic Ll
            0x0000,  // U+0d62 : Undefined
            0x0000,  // U+0d63 : Undefined
            0x0000,  // U+0d64 : Undefined
            0x0000,  // U+0d65 : Undefined
            0x09f1,  // U+0d66 : Malayalam Digit Zero
            0x09f2,  // U+0d67 : Malayalam Digit One
            0x09f3,  // U+0d68 : Malayalam Digit Two
            0x09f4,  // U+0d69 : Malayalam Digit Three
            0x09f5,  // U+0d6a : Malayalam Digit Four
            0x09f6,  // U+0d6b : Malayalam Digit Five
            0x09f7,  // U+0d6c : Malayalam Digit Six
            0x09f8,  // U+0d6d : Malayalam Digit Seven
            0x09f9,  // U+0d6e : Malayalam Digit Eight
            0x09fa   // U+0d6f : Malayalam Digit Nine
        };

        ////////////////////////////////////////////////////////////////////////////
        // SecondIndicByte
        //
        // This is used if the UnicodeToIndic table 4 high bits are set, this is
        // the value of the second Indic byte when applicable.
        ////////////////////////////////////////////////////////////////////////////
        private static readonly byte[] s_SecondIndicByte =
        {
            0x00,
            0xe9,
            0xb8,             // U+0952 == 0xf0_0xb8
            0xbf              // U+0970 == 0xf0_0xbf
        };

        ////////////////////////////////////////////////////////////////////////////
        // IndicMapping
        //
        // This table maps the 10 indic code pages to their unicode counterparts.
        // There are 0x60 characters in each table.  The tables are in pairs of 2
        // (1st char, 2nd char) and there are 10 tables (1 for each code page "font")
        ////////////////////////////////////////////////////////////////////////////
        private static readonly int[] s_IndicMappingIndex =
        {
            -1,       //  0 DEF 0X40 Default        // Not a real code page
            -1,       //  1 RMN 0X41 Roman          // Transliteration not supported
            0,        //  2 DEV 0X42 Devanagari
            1,        //  3 BNG 0X43 Bengali
            2,        //  4 TML 0X44 Tamil
            3,        //  5 TLG 0X45 Telugu
            1,        //  6 ASM 0X46 Assamese (Bengali) - Reuses table 1
            4,        //  7 ORI 0X47 Oriya
            5,        //  8 KND 0X48 Kannada
            6,        //  9 MLM 0X49 Malayalam
            7,        // 10 GJR 0X4A Gujarati
            8         // 11 PNJ 0X4B Punjabi (Gurmukhi)
        };

        ////////////////////////////////////////////////////////////////////////////
        // IndicMapping
        //
        // This table contains 9 tables for the 10 indic code pages to their unicode counterparts.
        // There are 0x60 characters in each table.  The tables are in pairs of 2
        // (1st char, 2nd char) and there are 10 tables (1 for each code page "font")
        //
        // The first index is the table index (from the IndicMappingIndex table),
        // the 2nd the byte index, the third the character index.
        //
        // For byte 0 a 0x0000 value indicates an unknown character
        // For byte 1 a 0 value indicates no special attributes.
        // For byte 1, 200C & 200D are Virama, Nukta special cases
        // For byte 1, B8BF is Devanagari stress & abbreviation sign special cases
        //
        // WARNING: When copying these from windows, ? 0x003F were changed to 0x0000.
        //
        ////////////////////////////////////////////////////////////////////////////
        // char[codePageMapIndex][byte][character]
        private static readonly char[,,] s_IndicMapping =
        {
            {
                ////////////////////////////////////////////////////////////////////////////
                //
                //  Devanagari  Table 0, Code Page (2, 0x42, 57002)
                //
                ////////////////////////////////////////////////////////////////////////////

                // Default Unicode Char
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0000', '\x0901', '\x0902', '\x0903', '\x0905', '\x0906', '\x0907', '\x0908',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0909', '\x090a', '\x090b', '\x090e', '\x090f', '\x0910', '\x090d', '\x0912',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0913', '\x0914', '\x0911', '\x0915', '\x0916', '\x0917', '\x0918', '\x0919',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x091a', '\x091b', '\x091c', '\x091d', '\x091e', '\x091f', '\x0920', '\x0921',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x0922', '\x0923', '\x0924', '\x0925', '\x0926', '\x0927', '\x0928', '\x0929',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x092a', '\x092b', '\x092c', '\x092d', '\x092e', '\x092f', '\x095f', '\x0930',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x0931', '\x0932', '\x0933', '\x0934', '\x0935', '\x0936', '\x0937', '\x0938',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x0939', '\x0000', '\x093e', '\x093f', '\x0940', '\x0941', '\x0942', '\x0943',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x0946', '\x0947', '\x0948', '\x0945', '\x094a', '\x094b', '\x094c', '\x0949',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x094d', '\x093c', '\x0964', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\x0000', '\x0966', '\x0967', '\x0968', '\x0969', '\x096a', '\x096b', '\x096c',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x096d', '\x096e', '\x096f', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'
                },

                // Alternate Unicode Char & Flags
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0', '\x0950',    '\x0',    '\x0',    '\x0',    '\x0', '\x090c', '\x0961',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0',    '\x0', '\x0960',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0',    '\x0',    '\x0', '\x0958', '\x0959', '\x095a',    '\x0',    '\x0',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x0',    '\x0', '\x095b',    '\x0',    '\x0',    '\x0',    '\x0', '\x095c',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x095d',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x0', '\x095e',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x0',    '\x0',    '\x0', '\x0962', '\x0963',    '\x0',    '\x0', '\x0944',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x200C', '\x200D', '\x093d',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\xB8BF',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0'
                }
            },

            {
                ////////////////////////////////////////////////////////////////////////////
                //
                //  Bengali & Assemese Table 1', Code Pages   (3, '43', 57003 & 6', '46', 57006)
                //
                ////////////////////////////////////////////////////////////////////////////

                // Default Unicode Char
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0000', '\x0981', '\x0982', '\x0983', '\x0985', '\x0986', '\x0987', '\x0988',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0989', '\x098a', '\x098b', '\x098f', '\x098f', '\x0990', '\x0990', '\x0993',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0993', '\x0994', '\x0994', '\x0995', '\x0996', '\x0997', '\x0998', '\x0999',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x099a', '\x099b', '\x099c', '\x099d', '\x099e', '\x099f', '\x09a0', '\x09a1',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x09a2', '\x09a3', '\x09a4', '\x09a5', '\x09a6', '\x09a7', '\x09a8', '\x09a8',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x09aa', '\x09ab', '\x09ac', '\x09ad', '\x09ae', '\x09af', '\x09df', '\x09b0',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x09b0', '\x09b2', '\x09b2', '\x09b2', '\x09ac', '\x09b6', '\x09b7', '\x09b8',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x09b9', '\x0000', '\x09be', '\x09bf', '\x09c0', '\x09c1', '\x09c2', '\x09c3',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x09c7', '\x09c7', '\x09c8', '\x09c8', '\x09cb', '\x09cb', '\x09cc', '\x09cc',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x09cd', '\x09bc', '\x002e', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\x0000', '\x09e6', '\x09e7', '\x09e8', '\x09e9', '\x09ea', '\x09eb', '\x09ec',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x09ed', '\x09ee', '\x09ef', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'
                },

                // Alternate Unicode Char & Flags
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                          '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0', '\x098c', '\x09e1',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                          '\x0',    '\x0', '\x09e0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                          '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                          '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0', '\x09dc',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                       '\x09dd',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                          '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                          '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                          '\x0',    '\x0',    '\x0', '\x09e2', '\x09e3',    '\x0',    '\x0', '\x09c4',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                          '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                       '\x200C', '\x200D',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                          '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                          '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0'
                }
            },

            {
                ////////////////////////////////////////////////////////////////////////////
                //
                //  Tamil   Table 2', Code Page   (4, '44', 57004)
                //
                ////////////////////////////////////////////////////////////////////////////

                // Default Unicode Char
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0000', '\x0000', '\x0b82', '\x0b83', '\x0b85', '\x0b86', '\x0b87', '\x0b88',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0b89', '\x0b8a', '\x0000', '\x0b8f', '\x0b8f', '\x0b90', '\x0b90', '\x0b92',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0b93', '\x0b94', '\x0b94', '\x0b95', '\x0b95', '\x0b95', '\x0b95', '\x0b99',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x0b9a', '\x0b9a', '\x0b9c', '\x0b9c', '\x0b9e', '\x0b9f', '\x0b9f', '\x0b9f',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x0b9f', '\x0ba3', '\x0ba4', '\x0ba4', '\x0ba4', '\x0ba4', '\x0ba8', '\x0ba9',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x0baa', '\x0baa', '\x0baa', '\x0baa', '\x0bae', '\x0baf', '\x0baf', '\x0bb0',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x0bb1', '\x0bb2', '\x0bb3', '\x0bb4', '\x0bb5', '\x0bb7', '\x0bb7', '\x0bb8',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x0bb9', '\x0000', '\x0bbe', '\x0bbf', '\x0bc0', '\x0bc1', '\x0bc2', '\x0000',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x0bc6', '\x0bc7', '\x0bc8', '\x0bc8', '\x0bca', '\x0bcb', '\x0bcc', '\x0bcc',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x0bcd', '\x0000', '\x002e', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\x0000', '\x0030', '\x0be7', '\x0be8', '\x0be9', '\x0bea', '\x0beb', '\x0bec',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x0bed', '\x0bee', '\x0bef', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'
                },

                // Alternate Unicode Char & Flags
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x200C', '\x200D',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0'
                }
            },

            {
                ////////////////////////////////////////////////////////////////////////////
                //
                //  Telugu    Table 3', Code Page   (5, '45', 57005)
                //
                ////////////////////////////////////////////////////////////////////////////

                // Default Unicode Char
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0000', '\x0c01', '\x0c02', '\x0c03', '\x0c05', '\x0c06', '\x0c07', '\x0c08',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0c09', '\x0c0a', '\x0c0b', '\x0c0e', '\x0c0f', '\x0c10', '\x0c10', '\x0c12',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0c13', '\x0c14', '\x0c14', '\x0c15', '\x0c16', '\x0c17', '\x0c18', '\x0c19',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x0c1a', '\x0c1b', '\x0c1c', '\x0c1d', '\x0c1e', '\x0c1f', '\x0c20', '\x0c21',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x0c22', '\x0c23', '\x0c24', '\x0c25', '\x0c26', '\x0c27', '\x0c28', '\x0c28',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x0c2a', '\x0c2b', '\x0c2c', '\x0c2d', '\x0c2e', '\x0c2f', '\x0c2f', '\x0c30',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x0c31', '\x0c32', '\x0c33', '\x0c33', '\x0c35', '\x0c36', '\x0c37', '\x0c38',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x0c39', '\x0000', '\x0c3e', '\x0c3f', '\x0c40', '\x0c41', '\x0c42', '\x0c43',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x0c46', '\x0c47', '\x0c48', '\x0c48', '\x0c4a', '\x0c4b', '\x0c4c', '\x0c4c',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x0c4d', '\x0000', '\x002e', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\x0000', '\x0c66', '\x0c67', '\x0c68', '\x0c69', '\x0c6a', '\x0c6b', '\x0c6c',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x0c6d', '\x0c6e', '\x0c6f', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'
                },

                // Alternate Unicode Char & Flags
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0', '\x0c0c', '\x0c61',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0',    '\x0', '\x0c60',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0', '\x0c44',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x200C', '\x200D',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0'
                }
            },

            {
                ////////////////////////////////////////////////////////////////////////////
                //
                //  Oriya   Table 4', Code Page   (7, '47', 57007)
                //
                ////////////////////////////////////////////////////////////////////////////

                // Default Unicode Char
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0000', '\x0b01', '\x0b02', '\x0b03', '\x0b05', '\x0b06', '\x0b07', '\x0b08',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0b09', '\x0b0a', '\x0b0b', '\x0b0f', '\x0b0f', '\x0b10', '\x0b10', '\x0b10',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0b13', '\x0b14', '\x0b14', '\x0b15', '\x0b16', '\x0b17', '\x0b18', '\x0b19',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x0b1a', '\x0b1b', '\x0b1c', '\x0b1d', '\x0b1e', '\x0b1f', '\x0b20', '\x0b21',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x0b22', '\x0b23', '\x0b24', '\x0b25', '\x0b26', '\x0b27', '\x0b28', '\x0b28',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x0b2a', '\x0b2b', '\x0b2c', '\x0b2d', '\x0b2e', '\x0b2f', '\x0b5f', '\x0b30',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x0b30', '\x0b32', '\x0b33', '\x0b33', '\x0b2c', '\x0b36', '\x0b37', '\x0b38',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x0b39', '\x0000', '\x0b3e', '\x0b3f', '\x0b40', '\x0b41', '\x0b42', '\x0b43',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x0b47', '\x0b47', '\x0b48', '\x0b48', '\x0b4b', '\x0b4b', '\x0b4c', '\x0b4c',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x0b4d', '\x0b3c', '\x002e', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\x0000', '\x0b66', '\x0b67', '\x0b68', '\x0b69', '\x0b6a', '\x0b6b', '\x0b6c',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x0b6d', '\x0b6e', '\x0b6f', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'
                },

                // Alternate Unicode Char & Flags
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0', '\x0c0c', '\x0c61',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0',    '\x0', '\x0c60',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0', '\x0b5c',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x0b5d',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0', '\x0c44',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x200C', '\x200D', '\x0b3d',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0'
                }
            },

            {
                ////////////////////////////////////////////////////////////////////////////
                //
                //  Kannada     Table 5', Code Page   (8, '48', 57008)
                //
                ////////////////////////////////////////////////////////////////////////////

                // Default Unicode Char
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0000', '\x0000', '\x0c82', '\x0c83', '\x0c85', '\x0c86', '\x0c87', '\x0c88',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0c89', '\x0c8a', '\x0c8b', '\x0c8e', '\x0c8f', '\x0c90', '\x0c90', '\x0c92',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0c93', '\x0c94', '\x0c94', '\x0c95', '\x0c96', '\x0c97', '\x0c98', '\x0c99',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x0c9a', '\x0c9b', '\x0c9c', '\x0c9d', '\x0c9e', '\x0c9f', '\x0ca0', '\x0ca1',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x0ca2', '\x0ca3', '\x0ca4', '\x0ca5', '\x0ca6', '\x0ca7', '\x0ca8', '\x0ca8',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x0caa', '\x0cab', '\x0cac', '\x0cad', '\x0cae', '\x0caf', '\x0caf', '\x0cb0',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x0cb1', '\x0cb2', '\x0cb3', '\x0cb3', '\x0cb5', '\x0cb6', '\x0cb7', '\x0cb8',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x0cb9', '\x0000', '\x0cbe', '\x0cbf', '\x0cc0', '\x0cc1', '\x0cc2', '\x0cc3',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x0cc6', '\x0cc7', '\x0cc8', '\x0cc8', '\x0cca', '\x0ccb', '\x0ccc', '\x0ccc',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x0ccd', '\x0000', '\x002e', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\x0000', '\x0ce6', '\x0ce7', '\x0ce8', '\x0ce9', '\x0cea', '\x0ceb', '\x0cec',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x0ced', '\x0cee', '\x0cef', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'
                },

                // Alternate Unicode Char & Flags
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0', '\x0c8c', '\x0ce1',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0',    '\x0', '\x0ce0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x0', '\x0cde',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0', '\x0cc4',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x200C', '\x200D',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0'
                }
            },

            {
                ////////////////////////////////////////////////////////////////////////////
                //
                //  Malayalam    Table 6', Code Page   (9, '49', 57009)
                //
                ////////////////////////////////////////////////////////////////////////////

                // Default Unicode Char
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0000', '\x0000', '\x0d02', '\x0d03', '\x0d05', '\x0d06', '\x0d07', '\x0d08',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0d09', '\x0d0a', '\x0d0b', '\x0d0e', '\x0d0f', '\x0d10', '\x0d10', '\x0d12',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0d13', '\x0d14', '\x0d14', '\x0d15', '\x0d16', '\x0d17', '\x0d18', '\x0d19',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x0d1a', '\x0d1b', '\x0d1c', '\x0d1d', '\x0d1e', '\x0d1f', '\x0d20', '\x0d21',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x0d22', '\x0d23', '\x0d24', '\x0d25', '\x0d26', '\x0d27', '\x0d28', '\x0d28',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x0d2a', '\x0d2b', '\x0d2c', '\x0d2d', '\x0d2e', '\x0d2f', '\x0d2f', '\x0d30',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x0d31', '\x0d32', '\x0d33', '\x0d34', '\x0d35', '\x0d36', '\x0d37', '\x0d38',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x0d39', '\x0000', '\x0d3e', '\x0d3f', '\x0d40', '\x0d41', '\x0d42', '\x0d43',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x0d46', '\x0d47', '\x0d48', '\x0d48', '\x0d4a', '\x0d4b', '\x0d4c', '\x0d4c',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x0d4d', '\x0000', '\x002e', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\x0000', '\x0d66', '\x0d67', '\x0d68', '\x0d69', '\x0d6a', '\x0d6b', '\x0d6c',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x0d6d', '\x0d6e', '\x0d6f', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'
                },

                // Alternate Unicode Char & Flags
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0', '\x0d0c', '\x0d61',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0',    '\x0', '\x0d60',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x200C', '\x200D',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0'
                }
            },

            {
                ////////////////////////////////////////////////////////////////////////////
                //
                //  Gujarati   Table 7', Code Page (10', '4a', 57010)
                //
                ////////////////////////////////////////////////////////////////////////////

                // Default Unicode Char
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0000', '\x0a81', '\x0a82', '\x0a83', '\x0a85', '\x0a86', '\x0a87', '\x0a88',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0a89', '\x0a8a', '\x0a8b', '\x0a8f', '\x0a8f', '\x0a90', '\x0a8d', '\x0a8d',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0a93', '\x0a94', '\x0a91', '\x0a95', '\x0a96', '\x0a97', '\x0a98', '\x0a99',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x0a9a', '\x0a9b', '\x0a9c', '\x0a9d', '\x0a9e', '\x0a9f', '\x0aa0', '\x0aa1',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x0aa2', '\x0aa3', '\x0aa4', '\x0aa5', '\x0aa6', '\x0aa7', '\x0aa8', '\x0aa8',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x0aaa', '\x0aab', '\x0aac', '\x0aad', '\x0aae', '\x0aaf', '\x0aaf', '\x0ab0',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x0ab0', '\x0ab2', '\x0ab3', '\x0ab3', '\x0ab5', '\x0ab6', '\x0ab7', '\x0ab8',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x0ab9', '\x0000', '\x0abe', '\x0abf', '\x0ac0', '\x0ac1', '\x0ac2', '\x0ac3',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x0ac7', '\x0ac7', '\x0ac8', '\x0ac5', '\x0acb', '\x0acb', '\x0acc', '\x0ac9',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x0acd', '\x0abc', '\x002e', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\x0000', '\x0ae6', '\x0ae7', '\x0ae8', '\x0ae9', '\x0aea', '\x0aeb', '\x0aec',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x0aed', '\x0aee', '\x0aef', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'
                },

                // Alternate Unicode Char & Flags
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0', '\x0ad0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0',    '\x0', '\x0ae0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0', '\x0ac4',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x200C', '\x200D', '\x0abd',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0'
                }
            },

            {
                ////////////////////////////////////////////////////////////////////////////
                //
                //  Punjabi (Gurmukhi)    Table 8', Code Page (11', '4b', 57011)
                //
                ////////////////////////////////////////////////////////////////////////////

                // Default Unicode Char
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0000', '\x0000', '\x0a02', '\x0000', '\x0a05', '\x0a06', '\x0a07', '\x0a08',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0a09', '\x0a0a', '\x0000', '\x0a0f', '\x0a0f', '\x0a10', '\x0a10', '\x0a10',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0a13', '\x0a14', '\x0a14', '\x0a15', '\x0a16', '\x0a17', '\x0a18', '\x0a19',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x0a1a', '\x0a1b', '\x0a1c', '\x0a1d', '\x0a1e', '\x0a1f', '\x0a20', '\x0a21',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x0a22', '\x0a23', '\x0a24', '\x0a25', '\x0a26', '\x0a27', '\x0a28', '\x0a28',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x0a2a', '\x0a2b', '\x0a2c', '\x0a2d', '\x0a2e', '\x0a2f', '\x0a2f', '\x0a30',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x0a30', '\x0a32', '\x0a33', '\x0a33', '\x0a35', '\x0a36', '\x0a36', '\x0a38',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x0a39', '\x0000', '\x0a3e', '\x0a3f', '\x0a40', '\x0a41', '\x0a42', '\x0000',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x0a47', '\x0a47', '\x0a48', '\x0a48', '\x0a4b', '\x0a4b', '\x0a4c', '\x0a4c',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x0a4d', '\x0a3c', '\x002e', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\x0000', '\x0a66', '\x0a67', '\x0a68', '\x0a69', '\x0a6a', '\x0a6b', '\x0a6c',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x0a6d', '\x0a6e', '\x0a6f', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'
                },

                // Alternate Unicode Char & Flags
                {
                    //       a0,       a1,       a2,       a3,       a4,       a5,       a6,       a7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       a8,       a9,       aa,       ab,       ac,       ad,       ae,       af,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       b0,       b1,       b2,       b3,       b4,       b5,       b6,       b7,
                    '\x0',    '\x0',    '\x0',    '\x0', '\x0a59', '\x0a5a',    '\x0',    '\x0',
                    //       b8,       b9,       ba,       bb,       bc,       bd,       be,       bf,
                    '\x0',    '\x0', '\x0a5b',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       c0,       c1,       c2,       c3,       c4,       c5,       c6,       c7,
                    '\x0a5c',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       c8,       c9,       ca,       cb,       cc,       cd,       ce,       cf,
                    '\x0', '\x0a5e',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d0,       d1,       d2,       d3,       d4,       d5,       d6,       d7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       d8,       d9,       da,       db,       dc,       dd,       de,       df,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       e0,       e1,       e2,       e3,       e4,       e5,       e6,       e7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       e8,       e9,       ea,       eb,       ec,       ed,       ee,       ef,
                    '\x200C', '\x200D',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f0,       f1,       f2,       f3,       f4,       f5,       f6,       f7,
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',
                    //       f8,       f9,       fa,       fb,       fc,       fd,       fe,       ff
                    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0',    '\x0'
                }
            }
        };
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Don't override IsAlwaysNormalized because it is just a Unicode Transformation and could be confused.
//

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Text
{
    public class UTF7Encoding : Encoding
    {
        private const string base64Chars =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        // 0123456789111111111122222222223333333333444444444455555555556666
        //              012345678901234567890123456789012345678901234567890123

        // These are the characters that can be directly encoded in UTF7.
        private const string directChars =
            "\t\n\r '(),-./0123456789:?ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        // These are the characters that can be optionally directly encoded in UTF7.
        private const string optionalChars =
            "!\"#$%&*;<=>@[]^_`{|}";

#pragma warning disable SYSLIB0001
        // Used by Encoding.UTF7 for lazy initialization
        // The initialization code will not be run until a static member of the class is referenced
        internal static readonly UTF7Encoding s_default = new UTF7Encoding();
#pragma warning restore SYSLIB0001

        // The set of base 64 characters.
        private byte[] _base64Bytes;
        // The decoded bits for every base64 values. This array has a size of 128 elements.
        // The index is the code point value of the base 64 characters.  The value is -1 if
        // the code point is not a valid base 64 character.  Otherwise, the value is a value
        // from 0 ~ 63.
        private sbyte[] _base64Values;
        // The array to decide if a Unicode code point below 0x80 can be directly encoded in UTF7.
        // This array has a size of 128.
        private bool[] _directEncode;

        private readonly bool _allowOptionals;

        private const int UTF7_CODEPAGE = 65000;

        [Obsolete(Obsoletions.SystemTextEncodingUTF7Message, DiagnosticId = Obsoletions.SystemTextEncodingUTF7DiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public UTF7Encoding()
            : this(false)
        {
        }

        [Obsolete(Obsoletions.SystemTextEncodingUTF7Message, DiagnosticId = Obsoletions.SystemTextEncodingUTF7DiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public UTF7Encoding(bool allowOptionals)
            : base(UTF7_CODEPAGE) // Set the data item.
        {
            // Allowing optionals?
            _allowOptionals = allowOptionals;

            // Make our tables
            MakeTables();
        }

        [MemberNotNull(nameof(_base64Bytes))]
        [MemberNotNull(nameof(_base64Values))]
        [MemberNotNull(nameof(_directEncode))]
        private void MakeTables()
        {
            // Build our tables
            _base64Bytes = new byte[64];
            for (int i = 0; i < 64; i++) _base64Bytes[i] = (byte)base64Chars[i];
            _base64Values = new sbyte[128];
            for (int i = 0; i < 128; i++) _base64Values[i] = -1;
            for (int i = 0; i < 64; i++) _base64Values[_base64Bytes[i]] = (sbyte)i;
            _directEncode = new bool[128];
            int count = directChars.Length;
            for (int i = 0; i < count; i++)
            {
                _directEncode[directChars[i]] = true;
            }

            if (_allowOptionals)
            {
                count = optionalChars.Length;
                for (int i = 0; i < count; i++)
                {
                    _directEncode[optionalChars[i]] = true;
                }
            }
        }

        // We go ahead and set this because Encoding expects it, however nothing can fall back in UTF7.
        internal sealed override void SetDefaultFallbacks()
        {
            // UTF7 had an odd decoderFallback behavior, and the Encoder fallback
            // is irrelevant because we encode surrogates individually and never check for unmatched ones
            // (so nothing can fallback during encoding)
            this.encoderFallback = new EncoderReplacementFallback(string.Empty);
            this.decoderFallback = new DecoderUTF7Fallback();
        }

        public override bool Equals([NotNullWhen(true)] object? value)
        {
            if (value is UTF7Encoding that)
            {
                return (_allowOptionals == that._allowOptionals) &&
                       (EncoderFallback.Equals(that.EncoderFallback)) &&
                       (DecoderFallback.Equals(that.DecoderFallback));
            }
            return false;
        }

        // Compared to all the other encodings, variations of UTF7 are unlikely

        public override int GetHashCode()
        {
            return this.CodePage + this.EncoderFallback.GetHashCode() + this.DecoderFallback.GetHashCode();
        }

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

        public override unsafe int GetByteCount(char[] chars!!, int index, int count)
        {
            // Validate input parameters
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (chars.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(chars), SR.ArgumentOutOfRange_IndexCountBuffer);

            // If no input, return 0, avoid fixed empty array problem
            if (count == 0)
                return 0;

            // Just call the pointer version
            fixed (char* pChars = chars)
                return GetByteCount(pChars + index, count, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetByteCount(string s)
        {
            // Validate input
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }

            fixed (char* pChars = s)
                return GetByteCount(pChars, s.Length, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [CLSCompliant(false)]
        public override unsafe int GetByteCount(char* chars!!, int count)
        {
            // Validate Parameters
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            // Call it with empty encoder
            return GetByteCount(chars, count, null);
        }

        // Parent method is safe.
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        public override unsafe int GetBytes(string s!!, int charIndex, int charCount,
                                            byte[] bytes!!, int byteIndex)
        {
            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException(charIndex < 0 ? nameof(charIndex) : nameof(charCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (s.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(s), SR.ArgumentOutOfRange_IndexCount);

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);

            int byteCount = bytes.Length - byteIndex;

            fixed (char* pChars = s) fixed (byte* pBytes = &MemoryMarshal.GetReference((Span<byte>)bytes))
                return GetBytes(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, null);
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

        public override unsafe int GetBytes(char[] chars!!, int charIndex, int charCount,
                                            byte[] bytes!!, int byteIndex)
        {
            // Validate parameters
            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException(charIndex < 0 ? nameof(charIndex) : nameof(charCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);

            // If nothing to encode return 0, avoid fixed problem
            if (charCount == 0)
                return 0;

            // Just call pointer version
            int byteCount = bytes.Length - byteIndex;

            fixed (char* pChars = chars) fixed (byte* pBytes = &MemoryMarshal.GetReference((Span<byte>)bytes))
                // Remember that byteCount is # to decode, not size of array.
                return GetBytes(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [CLSCompliant(false)]
        public override unsafe int GetBytes(char* chars!!, int charCount, byte* bytes!!, int byteCount)
        {
            // Validate Parameters
            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException(charCount < 0 ? nameof(charCount) : nameof(byteCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            return GetBytes(chars, charCount, bytes, byteCount, null);
        }

        // Returns the number of characters produced by decoding a range of bytes
        // in a byte array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetCharCount(byte[] bytes!!, int index, int count)
        {
            // Validate Parameters
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            // If no input just return 0, fixed doesn't like 0 length arrays.
            if (count == 0)
                return 0;

            // Just call pointer version
            fixed (byte* pBytes = bytes)
                return GetCharCount(pBytes + index, count, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [CLSCompliant(false)]
        public override unsafe int GetCharCount(byte* bytes!!, int count)
        {
            // Validate Parameters
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            return GetCharCount(bytes, count, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetChars(byte[] bytes!!, int byteIndex, int byteCount,
                                            char[] chars!!, int charIndex)
        {
            // Validate Parameters
            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException(byteIndex < 0 ? nameof(byteIndex) : nameof(byteCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (charIndex < 0 || charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);

            // If no input, return 0 & avoid fixed problem
            if (byteCount == 0)
                return 0;

            // Just call pointer version
            int charCount = chars.Length - charIndex;

            fixed (byte* pBytes = bytes) fixed (char* pChars = &MemoryMarshal.GetReference((Span<char>)chars))
                // Remember that charCount is # to decode, not size of array
                return GetChars(pBytes + byteIndex, byteCount, pChars + charIndex, charCount, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [CLSCompliant(false)]
        public override unsafe int GetChars(byte* bytes!!, int byteCount, char* chars!!, int charCount)
        {
            // Validate Parameters
            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException(charCount < 0 ? nameof(charCount) : nameof(byteCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            return GetChars(bytes, byteCount, chars, charCount, null);
        }

        // Returns a string containing the decoded representation of a range of
        // bytes in a byte array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe string GetString(byte[] bytes!!, int index, int count)
        {
            // Validate Parameters
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            // Avoid problems with empty input buffer
            if (count == 0) return string.Empty;

            fixed (byte* pBytes = bytes)
                return string.CreateStringFromEncoding(
                    pBytes + index, count, this);
        }

        //
        // End of standard methods copied from EncodingNLS.cs
        //
        internal sealed override unsafe int GetByteCount(char* chars, int count, EncoderNLS? baseEncoder)
        {
            Debug.Assert(chars != null, "[UTF7Encoding.GetByteCount]chars!=null");
            Debug.Assert(count >= 0, "[UTF7Encoding.GetByteCount]count >=0");

            // Just call GetBytes with bytes == null
            return GetBytes(chars, count, null, 0, baseEncoder);
        }

        internal sealed override unsafe int GetBytes(
            char* chars, int charCount, byte* bytes, int byteCount, EncoderNLS? baseEncoder)
        {
            Debug.Assert(byteCount >= 0, "[UTF7Encoding.GetBytes]byteCount >=0");
            Debug.Assert(chars != null, "[UTF7Encoding.GetBytes]chars!=null");
            Debug.Assert(charCount >= 0, "[UTF7Encoding.GetBytes]charCount >=0");

            // Get encoder info
            UTF7Encoding.Encoder? encoder = (UTF7Encoding.Encoder?)baseEncoder;

            // Default bits & count
            int bits = 0;
            int bitCount = -1;

            // prepare our helpers
            Encoding.EncodingByteBuffer buffer = new Encoding.EncodingByteBuffer(
                this, encoder, bytes, byteCount, chars, charCount);

            if (encoder != null)
            {
                bits = encoder.bits;
                bitCount = encoder.bitCount;

                // May have had too many left over
                while (bitCount >= 6)
                {
                    bitCount -= 6;
                    // If we fail we'll never really have enough room
                    if (!buffer.AddByte(_base64Bytes[(bits >> bitCount) & 0x3F]))
                        ThrowBytesOverflow(encoder, buffer.Count == 0);
                }
            }

            while (buffer.MoreData)
            {
                char currentChar = buffer.GetNextChar();

                if (currentChar < 0x80 && _directEncode[currentChar])
                {
                    if (bitCount >= 0)
                    {
                        if (bitCount > 0)
                        {
                            // Try to add the next byte
                            if (!buffer.AddByte(_base64Bytes[bits << 6 - bitCount & 0x3F]))
                                break;                                          // Stop here, didn't throw

                            bitCount = 0;
                        }

                        // Need to get emit '-' and our char, 2 bytes total
                        if (!buffer.AddByte((byte)'-'))
                            break;                                          // Stop here, didn't throw

                        bitCount = -1;
                    }

                    // Need to emit our char
                    if (!buffer.AddByte((byte)currentChar))
                        break;                                          // Stop here, didn't throw
                }
                else if (bitCount < 0 && currentChar == '+')
                {
                    if (!buffer.AddByte((byte)'+', (byte)'-'))
                        break;                                          // Stop here, didn't throw
                }
                else
                {
                    if (bitCount < 0)
                    {
                        // Need to emit a + and 12 bits (3 bytes)
                        // Only 12 of the 16 bits will be emitted this time, the other 4 wait 'til next time
                        if (!buffer.AddByte((byte)'+'))
                            break;                                          // Stop here, didn't throw

                        // We're now in bit mode, but haven't stored data yet
                        bitCount = 0;
                    }

                    // Add our bits
                    bits = bits << 16 | currentChar;
                    bitCount += 16;

                    while (bitCount >= 6)
                    {
                        bitCount -= 6;
                        if (!buffer.AddByte(_base64Bytes[(bits >> bitCount) & 0x3F]))
                        {
                            bitCount += 6;                              // We didn't use these bits
                            buffer.GetNextChar();                       // We're processing this char still, but AddByte
                                                                        // --'d it when we ran out of space
                            break;                                      // Stop here, not enough room for bytes
                        }
                    }

                    if (bitCount >= 6)
                        break;                  // Didn't have room to encode enough bits
                }
            }

            // Now if we have bits left over we have to encode them.
            // MustFlush may have been cleared by encoding.ThrowBytesOverflow earlier if converting
            if (bitCount >= 0 && (encoder == null || encoder.MustFlush))
            {
                // Do we have bits we have to stick in?
                if (bitCount > 0)
                {
                    if (buffer.AddByte(_base64Bytes[(bits << (6 - bitCount)) & 0x3F]))
                    {
                        // Emitted spare bits, 0 bits left
                        bitCount = 0;
                    }
                }

                // If converting and failed bitCount above, then we'll fail this too
                if (buffer.AddByte((byte)'-'))
                {
                    // turned off bit mode';
                    bits = 0;
                    bitCount = -1;
                }
                else
                    // If not successful, convert will maintain state for next time, also
                    // AddByte will have decremented our char count, however we need it to remain the same
                    buffer.GetNextChar();
            }

            // Do we have an encoder we're allowed to use?
            // bytes == null if counting, so don't use encoder then
            if (bytes != null && encoder != null)
            {
                // We already cleared bits & bitcount for mustflush case
                encoder.bits = bits;
                encoder.bitCount = bitCount;
                encoder._charsUsed = buffer.CharsUsed;
            }

            return buffer.Count;
        }

        internal sealed override unsafe int GetCharCount(byte* bytes, int count, DecoderNLS? baseDecoder)
        {
            Debug.Assert(count >= 0, "[UTF7Encoding.GetCharCount]count >=0");
            Debug.Assert(bytes != null, "[UTF7Encoding.GetCharCount]bytes!=null");

            // Just call GetChars with null char* to do counting
            return GetChars(bytes, count, null, 0, baseDecoder);
        }

        internal sealed override unsafe int GetChars(
            byte* bytes, int byteCount, char* chars, int charCount, DecoderNLS? baseDecoder)
        {
            Debug.Assert(byteCount >= 0, "[UTF7Encoding.GetChars]byteCount >=0");
            Debug.Assert(bytes != null, "[UTF7Encoding.GetChars]bytes!=null");
            Debug.Assert(charCount >= 0, "[UTF7Encoding.GetChars]charCount >=0");

            // Might use a decoder
            UTF7Encoding.Decoder? decoder = (UTF7Encoding.Decoder?)baseDecoder;

            // Get our output buffer info.
            Encoding.EncodingCharBuffer buffer = new Encoding.EncodingCharBuffer(
                this, decoder, chars, charCount, bytes, byteCount);

            // Get decoder info
            int bits = 0;
            int bitCount = -1;
            bool firstByte = false;
            if (decoder != null)
            {
                bits = decoder.bits;
                bitCount = decoder.bitCount;
                firstByte = decoder.firstByte;

                Debug.Assert(!firstByte || decoder.bitCount <= 0,
                    "[UTF7Encoding.GetChars]If remembered bits, then first byte flag shouldn't be set");
            }

            // We may have had bits in the decoder that we couldn't output last time, so do so now
            if (bitCount >= 16)
            {
                // Check our decoder buffer
                if (!buffer.AddChar((char)((bits >> (bitCount - 16)) & 0xFFFF)))
                    ThrowCharsOverflow(decoder, true);  // Always throw, they need at least 1 char even in Convert

                // Used this one, clean up extra bits
                bitCount -= 16;
            }

            // Loop through the input
            while (buffer.MoreData)
            {
                byte currentByte = buffer.GetNextByte();
                int c;

                if (bitCount >= 0)
                {
                    //
                    // Modified base 64 encoding.
                    //
                    sbyte v;
                    if (currentByte < 0x80 && ((v = _base64Values[currentByte]) >= 0))
                    {
                        firstByte = false;
                        bits = (bits << 6) | ((byte)v);
                        bitCount += 6;
                        if (bitCount >= 16)
                        {
                            c = (bits >> (bitCount - 16)) & 0xFFFF;
                            bitCount -= 16;
                        }
                        // If not enough bits just continue
                        else continue;
                    }
                    else
                    {
                        // If it wasn't a base 64 byte, everything's going to turn off base 64 mode
                        bitCount = -1;

                        if (currentByte != '-')
                        {
                            // >= 0x80 (because of 1st if statemtn)
                            // We need this check since the _base64Values[b] check below need b <= 0x7f.
                            // This is not a valid base 64 byte.  Terminate the shifted-sequence and
                            // emit this byte.

                            // not in base 64 table
                            // According to the RFC 1642 and the example code of UTF-7
                            // in Unicode 2.0, we should just zero-extend the invalid UTF7 byte

                            // Chars won't be updated unless this works, try to fallback
                            if (!buffer.Fallback(currentByte))
                                break;                                          // Stop here, didn't throw

                            // Used that byte, we're done with it
                            continue;
                        }

                        //
                        // The encoding for '+' is "+-".
                        //
                        if (firstByte) c = '+';
                        // We just turn it off if not emitting a +, so we're done.
                        else continue;
                    }
                    //
                    // End of modified base 64 encoding block.
                    //
                }
                else if (currentByte == '+')
                {
                    //
                    // Found the start of a modified base 64 encoding block or a plus sign.
                    //
                    bitCount = 0;
                    firstByte = true;
                    continue;
                }
                else
                {
                    // Normal character
                    if (currentByte >= 0x80)
                    {
                        // Try to fallback
                        if (!buffer.Fallback(currentByte))
                            break;                                          // Stop here, didn't throw

                        // Done falling back
                        continue;
                    }

                    // Use the normal character
                    c = currentByte;
                }

                if (c >= 0)
                {
                    // Check our buffer
                    if (!buffer.AddChar((char)c))
                    {
                        // No room.  If it was a plain char we'll try again later.
                        // Note, we'll consume this byte and stick it in decoder, even if we can't output it
                        if (bitCount >= 0)                                  // Can we rememmber this byte (char)
                        {
                            buffer.AdjustBytes(+1);                         // Need to readd the byte that AddChar subtracted when it failed
                            bitCount += 16;                                 // We'll still need that char we have in our bits
                        }
                        break;                                              // didn't throw, stop
                    }
                }
            }

            // Stick stuff in the decoder if we can (chars == null if counting, so don't store decoder)
            if (chars != null && decoder != null)
            {
                // MustFlush?  (Could've been cleared by ThrowCharsOverflow if Convert & didn't reach end of buffer)
                if (decoder.MustFlush)
                {
                    // RFC doesn't specify what would happen if we have non-0 leftover bits, we just drop them
                    decoder.bits = 0;
                    decoder.bitCount = -1;
                    decoder.firstByte = false;
                }
                else
                {
                    decoder.bits = bits;
                    decoder.bitCount = bitCount;
                    decoder.firstByte = firstByte;
                }
                decoder._bytesUsed = buffer.BytesUsed;
            }
            // else ignore any hanging bits.

            // Return our count
            return buffer.Count;
        }

        public override System.Text.Decoder GetDecoder()
        {
            return new UTF7Encoding.Decoder(this);
        }

        public override System.Text.Encoder GetEncoder()
        {
            return new UTF7Encoding.Encoder(this);
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount),
                     SR.ArgumentOutOfRange_NeedNonNegNum);

            // Suppose that every char can not be direct-encoded, we know that
            // a byte can encode 6 bits of the Unicode character.  And we will
            // also need two extra bytes for the shift-in ('+') and shift-out ('-') mark.
            // Therefore, the max byte should be:
            // byteCount = 2 + Math.Ceiling((double)charCount * 16 / 6);
            // That is always <= 2 + 3 * charCount;
            // Longest case is alternating encoded, direct, encoded data for 5 + 1 + 5... bytes per char.
            // UTF7 doesn't have left over surrogates, but if no input we may need an output - to turn off
            // encoding if MustFlush is true.

            // Its easiest to think of this as 2 bytes to turn on/off the base64 mode, then 3 bytes per char.
            // 3 bytes is 18 bits of encoding, which is more than we need, but if its direct encoded then 3
            // bytes allows us to turn off and then back on base64 mode if necessary.

            // Note that UTF7 encoded surrogates individually and isn't worried about mismatches, so all
            // code points are encodable int UTF7.
            long byteCount = (long)charCount * 3 + 2;

            // check for overflow
            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ArgumentOutOfRange_GetByteCountOverflow);

            return (int)byteCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount),
                     SR.ArgumentOutOfRange_NeedNonNegNum);

            // Worst case is 1 char per byte.  Minimum 1 for left over bits in case decoder is being flushed
            // Also note that we ignore extra bits (per spec), so UTF7 doesn't have unknown in this direction.
            int charCount = byteCount;
            if (charCount == 0) charCount = 1;

            return charCount;
        }

        // Of all the amazing things... This MUST be Decoder so that our com name
        // for System.Text.Decoder doesn't change
        private sealed class Decoder : DecoderNLS
        {
            /*private*/
            internal int bits;
            /*private*/
            internal int bitCount;
            /*private*/
            internal bool firstByte;

            public Decoder(UTF7Encoding encoding) : base(encoding)
            {
                // base calls reset
            }

            public override void Reset()
            {
                this.bits = 0;
                this.bitCount = -1;
                this.firstByte = false;
                if (_fallbackBuffer != null)
                    _fallbackBuffer.Reset();
            }

            // Anything left in our encoder?
            internal override bool HasState =>
                // NOTE: This forces the last -, which some encoder might not encode.  If we
                // don't see it we don't think we're done reading.
                this.bitCount != -1;
        }

        // Of all the amazing things... This MUST be Encoder so that our com name
        // for System.Text.Encoder doesn't change
        private sealed class Encoder : EncoderNLS
        {
            /*private*/
            internal int bits;
            /*private*/
            internal int bitCount;

            public Encoder(UTF7Encoding encoding) : base(encoding)
            {
                // base calls reset
            }

            public override void Reset()
            {
                this.bitCount = -1;
                this.bits = 0;
                if (_fallbackBuffer != null)
                    _fallbackBuffer.Reset();
            }

            // Anything left in our encoder?
            internal override bool HasState => this.bits != 0 || this.bitCount != -1;
        }

        // Preexisting UTF7 behavior for bad bytes was just to spit out the byte as the next char
        // and turn off base64 mode if it was in that mode.  We still exit the mode, but now we fallback.
        private sealed class DecoderUTF7Fallback : DecoderFallback
        {
            // Default replacement fallback uses no best fit and ? replacement string

            public override DecoderFallbackBuffer CreateFallbackBuffer() =>
                new DecoderUTF7FallbackBuffer();

            // Maximum number of characters that this instance of this fallback could return
            public override int MaxCharCount => 1; // returns 1 char per bad byte

            public override bool Equals([NotNullWhen(true)] object? value) => value is DecoderUTF7Fallback;

            public override int GetHashCode() => 984;
        }

        private sealed class DecoderUTF7FallbackBuffer : DecoderFallbackBuffer
        {
            // Store our default string
            private char cFallback;
            private int iCount = -1;
            private int iSize;

            // Fallback Methods
            public override bool Fallback(byte[] bytesUnknown, int index)
            {
                // We expect no previous fallback in our buffer
                Debug.Assert(iCount < 0, "[DecoderUTF7FallbackBuffer.Fallback] Can't have recursive fallbacks");
                Debug.Assert(bytesUnknown.Length == 1, "[DecoderUTF7FallbackBuffer.Fallback] Only possible fallback case should be 1 unknown byte");

                // Go ahead and get our fallback
                cFallback = (char)bytesUnknown[0];

                // Any of the fallback characters can be handled except for 0
                if (cFallback == 0)
                {
                    return false;
                }

                iCount = iSize = 1;

                return true;
            }

            public override char GetNextChar()
            {
                if (iCount-- > 0)
                    return cFallback;

                // Note: this means that 0 in UTF7 stream will never be emitted.
                return (char)0;
            }

            public override bool MovePrevious()
            {
                if (iCount >= 0)
                {
                    iCount++;
                }

                // return true if we were allowed to do this
                return iCount >= 0 && iCount <= iSize;
            }

            // Return # of chars left in this fallback
            public override int Remaining => (iCount > 0) ? iCount : 0;

            // Clear the buffer
            public override unsafe void Reset()
            {
                iCount = -1;
                byteStart = null;
            }

            // This version just counts the fallback and doesn't actually copy anything.
            internal override unsafe int InternalFallback(byte[] bytes, byte* pBytes)
            // Right now this has both bytes and bytes[], since we might have extra bytes, hence the
            // array, and we might need the index, hence the byte*
            {
                // We expect no previous fallback in our buffer
                Debug.Assert(iCount < 0, "[DecoderUTF7FallbackBuffer.InternalFallback] Can't have recursive fallbacks");
                if (bytes.Length != 1)
                {
                    throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);
                }

                // Can't fallback a byte 0, so return for that case, 1 otherwise.
                return bytes[0] == 0 ? 0 : 1;
            }
        }
    }
}

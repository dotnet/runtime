// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Buffers;
using System.Diagnostics;
using System.Threading;

namespace System.Text
{
    public abstract class EncoderFallback
    {
        private static EncoderFallback? s_replacementFallback; // Default fallback, uses no best fit & "?"
        private static EncoderFallback? s_exceptionFallback;

        // Get each of our generic fallbacks.

        public static EncoderFallback ReplacementFallback
        {
            get
            {
                if (s_replacementFallback == null)
                    Interlocked.CompareExchange<EncoderFallback?>(ref s_replacementFallback, new EncoderReplacementFallback(), null);

                return s_replacementFallback!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
            }
        }


        public static EncoderFallback ExceptionFallback
        {
            get
            {
                if (s_exceptionFallback == null)
                    Interlocked.CompareExchange<EncoderFallback?>(ref s_exceptionFallback, new EncoderExceptionFallback(), null);

                return s_exceptionFallback!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
            }
        }

        // Fallback
        //
        // Return the appropriate unicode string alternative to the character that need to fall back.
        // Most implementations will be:
        //      return new MyCustomEncoderFallbackBuffer(this);

        public abstract EncoderFallbackBuffer CreateFallbackBuffer();

        // Maximum number of characters that this instance of this fallback could return

        public abstract int MaxCharCount { get; }
    }


    public abstract class EncoderFallbackBuffer
    {
        // Most implementations will probably need an implementation-specific constructor

        // Public methods that cannot be overridden that let us do our fallback thing
        // These wrap the internal methods so that we can check for people doing stuff that is incorrect

        public abstract bool Fallback(char charUnknown, int index);

        public abstract bool Fallback(char charUnknownHigh, char charUnknownLow, int index);

        // Get next character

        public abstract char GetNextChar();

        // Back up a character

        public abstract bool MovePrevious();

        // How many chars left in this fallback?

        public abstract int Remaining { get; }

        // Not sure if this should be public or not.
        // Clear the buffer

        public virtual void Reset()
        {
            while (GetNextChar() != (char)0) ;
        }

        // Internal items to help us figure out what we're doing as far as error messages, etc.
        // These help us with our performance and messages internally
        internal unsafe char* charStart;
        internal unsafe char* charEnd;
        internal EncoderNLS? encoder; // TODO: MAKE ME PRIVATE
        internal bool setEncoder;
        internal bool bUsedEncoder;
        internal bool bFallingBack = false;
        internal int iRecursionCount = 0;
        private const int iMaxRecursion = 250;
        private Encoding? encoding;
        private int originalCharCount;

        // Internal Reset
        // For example, what if someone fails a conversion and wants to reset one of our fallback buffers?
        internal unsafe void InternalReset()
        {
            charStart = null;
            bFallingBack = false;
            iRecursionCount = 0;
            Reset();
        }

        // Set the above values
        // This can't be part of the constructor because EncoderFallbacks would have to know how to implement these.
        internal unsafe void InternalInitialize(char* charStart, char* charEnd, EncoderNLS? encoder, bool setEncoder)
        {
            this.charStart = charStart;
            this.charEnd = charEnd;
            this.encoder = encoder;
            this.setEncoder = setEncoder;
            this.bUsedEncoder = false;
            this.bFallingBack = false;
            this.iRecursionCount = 0;
        }

        internal static EncoderFallbackBuffer CreateAndInitialize(Encoding encoding, EncoderNLS? encoder, int originalCharCount)
        {
            // The original char count is only used for keeping track of what 'index' value needs
            // to be passed to the abstract Fallback method. The index value is calculated by subtracting
            // 'chars.Length' (where chars is expected to be the entire remaining input buffer)
            // from the 'originalCharCount' value specified here.

            EncoderFallbackBuffer fallbackBuffer = (encoder is null) ? encoding.EncoderFallback.CreateFallbackBuffer() : encoder.FallbackBuffer;

            fallbackBuffer.encoding = encoding;
            fallbackBuffer.encoder = encoder;
            fallbackBuffer.originalCharCount = originalCharCount;

            return fallbackBuffer;
        }

        internal char InternalGetNextChar()
        {
            char ch = GetNextChar();
            bFallingBack = (ch != 0);
            if (ch == 0) iRecursionCount = 0;
            return ch;
        }

        private bool InternalFallback(ReadOnlySpan<char> chars, out int charsConsumed)
        {
            Debug.Assert(!chars.IsEmpty, "Caller shouldn't invoke this if there's no data to fall back.");

            // First, try falling back a single BMP character or a standalone low surrogate.
            // If the first char is a high surrogate, we'll try to combine it with the next
            // char in the input sequence.

            char firstChar = chars[0];
            char secondChar = default;

            if (!chars.IsEmpty)
            {
                firstChar = chars[0];

                if (1 < (uint)chars.Length)
                {
                    secondChar = chars[1];
                }
            }

            // Ask the subclassed type to initiate fallback logic.

            int index = originalCharCount - chars.Length;

            if (!char.IsSurrogatePair(firstChar, secondChar))
            {
                // This code path is also used when 'firstChar' is a standalone surrogate or
                // if it's a high surrogate at the end of the input buffer.

                charsConsumed = 1;
                return Fallback(firstChar, index);
            }
            else
            {
                charsConsumed = 2;
                return Fallback(firstChar, secondChar, index);
            }
        }

        internal int InternalFallbackGetByteCount(ReadOnlySpan<char> chars, out int charsConsumed)
        {
            int bytesWritten = 0;

            if (InternalFallback(chars, out charsConsumed))
            {
                // There's data in the fallback buffer - pull it out now.

                bytesWritten = DrainRemainingDataForGetByteCount();
            }

            return bytesWritten;
        }

        internal bool TryInternalFallbackGetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, out int charsConsumed, out int bytesWritten)
        {
            if (InternalFallback(chars, out charsConsumed))
            {
                // There's data in the fallback buffer - pull it out now.

                return TryDrainRemainingDataForGetBytes(bytes, out bytesWritten);
            }
            else
            {
                // There's no data in the fallback buffer.

                bytesWritten = 0;
                return true; // true = didn't run out of space in destination buffer
            }
        }

        internal bool TryDrainRemainingDataForGetBytes(Span<byte> bytes, out int bytesWritten)
        {
            int originalBytesLength = bytes.Length;

            Debug.Assert(encoding != null);
            Rune thisRune;
            while ((thisRune = GetNextRune()).Value != 0)
            {
                switch (encoding.EncodeRune(thisRune, bytes, out int bytesWrittenJustNow))
                {
                    case OperationStatus.Done:

                        bytes = bytes.Slice(bytesWrittenJustNow);
                        continue;

                    case OperationStatus.DestinationTooSmall:

                        // Since we're not consuming the Rune we just read, back up as many chars as necessary
                        // to undo the read we just performed, then report to our caller that we ran out of space.

                        for (int i = 0; i < thisRune.Utf16SequenceLength; i++)
                        {
                            MovePrevious();
                        }

                        bytesWritten = originalBytesLength - bytes.Length;
                        return false; // ran out of destination buffer

                    case OperationStatus.InvalidData:

                        // We can't fallback the fallback. We can't make forward progress, so report to our caller
                        // that something went terribly wrong. The error message contains the fallback char that
                        // couldn't be converted. (Ideally we'd provide the first char that originally triggered
                        // the fallback, but it's complicated to keep this state around, and a fallback producing
                        // invalid data should be a very rare occurrence.)

                        ThrowLastCharRecursive(thisRune.Value);
                        break; // will never be hit; call above throws

                    default:

                        Debug.Fail("Unexpected return value.");
                        break;
                }
            }

            bytesWritten = originalBytesLength - bytes.Length;
            return true; // finished successfully
        }

        internal int DrainRemainingDataForGetByteCount()
        {
            int totalByteCount = 0;

            Debug.Assert(encoding != null);
            Rune thisRune;
            while ((thisRune = GetNextRune()).Value != 0)
            {
                if (!encoding.TryGetByteCount(thisRune, out int byteCountThisIteration))
                {
                    // We can't fallback the fallback. We can't make forward progress, so report to our caller
                    // that something went terribly wrong. The error message contains the fallback char that
                    // couldn't be converted. (Ideally we'd provide the first char that originally triggered
                    // the fallback, but it's complicated to keep this state around, and a fallback producing
                    // invalid data should be a very rare occurrence.)

                    ThrowLastCharRecursive(thisRune.Value);
                }

                Debug.Assert(byteCountThisIteration >= 0, "Encoding shouldn't have returned a negative byte count.");

                // We need to check for overflow while tallying the fallback byte count.

                totalByteCount += byteCountThisIteration;
                if (totalByteCount < 0)
                {
                    InternalReset();
                    Encoding.ThrowConversionOverflow();
                }
            }

            return totalByteCount;
        }

        private Rune GetNextRune()
        {
            char firstChar = GetNextChar();
            if (Rune.TryCreate(firstChar, out Rune value) || Rune.TryCreate(firstChar, GetNextChar(), out value))
            {
                return value;
            }

            throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);
        }

        // Fallback the current character using the remaining buffer and encoder if necessary
        // This can only be called by our encodings (other have to use the public fallback methods), so
        // we can use our EncoderNLS here too.
        // setEncoder is true if we're calling from a GetBytes method, false if we're calling from a GetByteCount
        //
        // Note that this could also change the contents of this.encoder, which is the same
        // object that the caller is using, so the caller could mess up the encoder for us
        // if they aren't careful.
        internal unsafe virtual bool InternalFallback(char ch, ref char* chars)
        {
            // Shouldn't have null charStart
            Debug.Assert(charStart != null,
                "[EncoderFallback.InternalFallbackBuffer]Fallback buffer is not initialized");

            // Get our index, remember chars was preincremented to point at next char, so have to -1
            int index = (int)(chars - charStart) - 1;

            // See if it was a high surrogate
            if (char.IsHighSurrogate(ch))
            {
                // See if there's a low surrogate to go with it
                if (chars >= this.charEnd)
                {
                    // Nothing left in input buffer
                    // No input, return 0 if mustflush is false
                    if (this.encoder != null && !this.encoder.MustFlush)
                    {
                        // Done, nothing to fallback
                        if (this.setEncoder)
                        {
                            bUsedEncoder = true;
                            this.encoder._charLeftOver = ch;
                        }
                        bFallingBack = false;
                        return false;
                    }
                }
                else
                {
                    // Might have a low surrogate
                    char cNext = *chars;
                    if (char.IsLowSurrogate(cNext))
                    {
                        // If already falling back then fail
                        if (bFallingBack && iRecursionCount++ > iMaxRecursion)
                            ThrowLastCharRecursive(char.ConvertToUtf32(ch, cNext));

                        // Next is a surrogate, add it as surrogate pair, and increment chars
                        chars++;
                        bFallingBack = Fallback(ch, cNext, index);
                        return bFallingBack;
                    }
                    // Next isn't a low surrogate, just fallback the high surrogate
                }
            }

            // If already falling back then fail
            if (bFallingBack && iRecursionCount++ > iMaxRecursion)
                ThrowLastCharRecursive((int)ch);

            // Fall back our char
            bFallingBack = Fallback(ch, index);

            return bFallingBack;
        }

        // private helper methods
        internal void ThrowLastCharRecursive(int charRecursive)
        {
            // Throw it, using our complete character
            throw new ArgumentException(
                SR.Format(SR.Argument_RecursiveFallback,
                    charRecursive), "chars");
        }
    }
}

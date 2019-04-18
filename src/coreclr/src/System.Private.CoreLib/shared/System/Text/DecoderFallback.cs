// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace System.Text
{
    public abstract class DecoderFallback
    {
        private static DecoderFallback? s_replacementFallback; // Default fallback, uses no best fit & "?"
        private static DecoderFallback? s_exceptionFallback;

        public static DecoderFallback ReplacementFallback =>
            s_replacementFallback ?? Interlocked.CompareExchange(ref s_replacementFallback, new DecoderReplacementFallback(), null) ?? s_replacementFallback!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761


        public static DecoderFallback ExceptionFallback =>
            s_exceptionFallback ?? Interlocked.CompareExchange<DecoderFallback?>(ref s_exceptionFallback, new DecoderExceptionFallback(), null) ?? s_exceptionFallback!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761

        // Fallback
        //
        // Return the appropriate unicode string alternative to the character that need to fall back.
        // Most implementations will be:
        //      return new MyCustomDecoderFallbackBuffer(this);

        public abstract DecoderFallbackBuffer CreateFallbackBuffer();

        // Maximum number of characters that this instance of this fallback could return

        public abstract int MaxCharCount { get; }
    }


    public abstract class DecoderFallbackBuffer
    {
        // Most implementations will probably need an implementation-specific constructor

        // internal methods that cannot be overridden that let us do our fallback thing
        // These wrap the internal methods so that we can check for people doing stuff that's incorrect

        public abstract bool Fallback(byte[] bytesUnknown, int index);

        // Get next character

        public abstract char GetNextChar();

        // Back up a character

        public abstract bool MovePrevious();

        // How many chars left in this fallback?

        public abstract int Remaining { get; }

        // Clear the buffer

        public virtual void Reset()
        {
            while (GetNextChar() != (char)0) ;
        }

        // Internal items to help us figure out what we're doing as far as error messages, etc.
        // These help us with our performance and messages internally
        internal unsafe byte* byteStart;
        internal unsafe char* charEnd;

        internal Encoding? _encoding;
        internal DecoderNLS? _decoder;
        private int _originalByteCount;

        // Internal Reset
        internal unsafe void InternalReset()
        {
            byteStart = null;
            Reset();
        }

        // Set the above values
        // This can't be part of the constructor because DecoderFallbacks would have to know how to implement these.
        internal unsafe void InternalInitialize(byte* byteStart, char* charEnd)
        {
            this.byteStart = byteStart;
            this.charEnd = charEnd;
        }

        internal static DecoderFallbackBuffer CreateAndInitialize(Encoding encoding, DecoderNLS? decoder, int originalByteCount)
        {
            // The original byte count is only used for keeping track of what 'index' value needs
            // to be passed to the abstract Fallback method. The index value is calculated by subtracting
            // 'bytes.Length' (where bytes is expected to be the entire remaining input buffer)
            // from the 'originalByteCount' value specified here.

            DecoderFallbackBuffer fallbackBuffer = (decoder is null) ? encoding.DecoderFallback.CreateFallbackBuffer() : decoder.FallbackBuffer;

            fallbackBuffer._encoding = encoding;
            fallbackBuffer._decoder = decoder;
            fallbackBuffer._originalByteCount = originalByteCount;

            return fallbackBuffer;
        }

        // Fallback the current byte by sticking it into the remaining char buffer.
        // This can only be called by our encodings (other have to use the public fallback methods), so
        // we can use our DecoderNLS here too (except we don't).
        // Returns true if we are successful, false if we can't fallback the character (no buffer space)
        // So caller needs to throw buffer space if return false.
        // Right now this has both bytes and bytes[], since we might have extra bytes, hence the
        // array, and we might need the index, hence the byte*
        // Don't touch ref chars unless we succeed
        internal unsafe virtual bool InternalFallback(byte[] bytes, byte* pBytes, ref char* chars)
        {
            Debug.Assert(byteStart != null, "[DecoderFallback.InternalFallback]Used InternalFallback without calling InternalInitialize");

            // See if there's a fallback character and we have an output buffer then copy our string.
            if (this.Fallback(bytes, (int)(pBytes - byteStart - bytes.Length)))
            {
                // Copy the chars to our output
                char ch;
                char* charTemp = chars;
                bool bHighSurrogate = false;
                while ((ch = GetNextChar()) != 0)
                {
                    // Make sure no mixed up surrogates
                    if (char.IsSurrogate(ch))
                    {
                        if (char.IsHighSurrogate(ch))
                        {
                            // High Surrogate
                            if (bHighSurrogate)
                                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);
                            bHighSurrogate = true;
                        }
                        else
                        {
                            // Low surrogate
                            if (bHighSurrogate == false)
                                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);
                            bHighSurrogate = false;
                        }
                    }

                    if (charTemp >= charEnd)
                    {
                        // No buffer space
                        return false;
                    }

                    *(charTemp++) = ch;
                }

                // Need to make sure that bHighSurrogate isn't true
                if (bHighSurrogate)
                    throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);

                // Now we aren't going to be false, so its OK to update chars
                chars = charTemp;
            }

            return true;
        }

        // This version just counts the fallback and doesn't actually copy anything.
        internal unsafe virtual int InternalFallback(byte[] bytes, byte* pBytes)
        // Right now this has both bytes and bytes[], since we might have extra bytes, hence the
        // array, and we might need the index, hence the byte*
        {
            Debug.Assert(byteStart != null, "[DecoderFallback.InternalFallback]Used InternalFallback without calling InternalInitialize");

            // See if there's a fallback character and we have an output buffer then copy our string.
            if (this.Fallback(bytes, (int)(pBytes - byteStart - bytes.Length)))
            {
                int count = 0;

                char ch;
                bool bHighSurrogate = false;
                while ((ch = GetNextChar()) != 0)
                {
                    // Make sure no mixed up surrogates
                    if (char.IsSurrogate(ch))
                    {
                        if (char.IsHighSurrogate(ch))
                        {
                            // High Surrogate
                            if (bHighSurrogate)
                                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);
                            bHighSurrogate = true;
                        }
                        else
                        {
                            // Low surrogate
                            if (bHighSurrogate == false)
                                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);
                            bHighSurrogate = false;
                        }
                    }

                    count++;
                }

                // Need to make sure that bHighSurrogate isn't true
                if (bHighSurrogate)
                    throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);

                return count;
            }

            // If no fallback return 0
            return 0;
        }

        internal int InternalFallbackGetCharCount(ReadOnlySpan<byte> remainingBytes, int fallbackLength)
        {
            return (Fallback(remainingBytes.Slice(0, fallbackLength).ToArray(), index: _originalByteCount - remainingBytes.Length))
                ? DrainRemainingDataForGetCharCount()
                : 0;
        }

        internal bool TryInternalFallbackGetChars(ReadOnlySpan<byte> remainingBytes, int fallbackLength, Span<char> chars, out int charsWritten)
        {
            if (Fallback(remainingBytes.Slice(0, fallbackLength).ToArray(), index: _originalByteCount - remainingBytes.Length))
            {
                return TryDrainRemainingDataForGetChars(chars, out charsWritten);
            }
            else
            {
                // Return true because we weren't asked to write anything, so this is a "success" in the sense that
                // the output buffer was large enough to hold the desired 0 chars of output.

                charsWritten = 0;
                return true;
            }
        }

        private Rune GetNextRune()
        {
            // Call GetNextChar() and try treating it as a non-surrogate character.
            // If that fails, call GetNextChar() again and attempt to treat the two chars
            // as a surrogate pair. If that still fails, throw an exception since the fallback
            // mechanism is giving us a bad replacement character.

            Rune rune;
            char ch = GetNextChar();
            if (!Rune.TryCreate(ch, out rune) && !Rune.TryCreate(ch, GetNextChar(), out rune))
            {
                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);
            }

            return rune;
        }

        internal int DrainRemainingDataForGetCharCount()
        {
            int totalCharCount = 0;

            Rune thisRune;
            while ((thisRune = GetNextRune()).Value != 0)
            {
                // We need to check for overflow while tallying the fallback char count.

                totalCharCount += thisRune.Utf16SequenceLength;
                if (totalCharCount < 0)
                {
                    InternalReset();
                    Encoding.ThrowConversionOverflow();
                }
            }

            return totalCharCount;
        }

        internal bool TryDrainRemainingDataForGetChars(Span<char> chars, out int charsWritten)
        {
            int originalCharCount = chars.Length;

            Rune thisRune;
            while ((thisRune = GetNextRune()).Value != 0)
            {
                if (thisRune.TryEncodeToUtf16(chars, out int charsWrittenJustNow))
                {
                    chars = chars.Slice(charsWrittenJustNow);
                    continue;
                }
                else
                {
                    InternalReset();
                    charsWritten = default;
                    return false;
                }
            }

            charsWritten = originalCharCount - chars.Length;
            return true;
        }

        // private helper methods
        internal void ThrowLastBytesRecursive(byte[] bytesUnknown)
        {
            bytesUnknown = bytesUnknown ?? Array.Empty<byte>();

            // Create a string representation of our bytes.
            StringBuilder strBytes = new StringBuilder(bytesUnknown.Length * 3);
            int i;
            for (i = 0; i < bytesUnknown.Length && i < 20; i++)
            {
                if (strBytes.Length > 0)
                    strBytes.Append(' ');
                strBytes.AppendFormat(CultureInfo.InvariantCulture, "\\x{0:X2}", bytesUnknown[i]);
            }
            // In case the string's really long
            if (i == 20)
                strBytes.Append(" ...");

            // Throw it, using our complete bytes
            throw new ArgumentException(
                SR.Format(SR.Argument_RecursiveFallbackBytes,
                    strBytes.ToString()), nameof(bytesUnknown));
        }
    }
}

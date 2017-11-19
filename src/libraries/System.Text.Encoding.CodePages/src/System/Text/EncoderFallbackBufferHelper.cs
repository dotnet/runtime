// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text
{
    internal struct EncoderFallbackBufferHelper
    {
        public unsafe EncoderFallbackBufferHelper(EncoderFallbackBuffer fallbackBuffer)
        {
            _fallbackBuffer = fallbackBuffer;
            bFallingBack = bUsedEncoder = setEncoder = false;
            iRecursionCount = 0;
            charEnd = charStart = null;
            encoder = null;
        }
        // Internal items to help us figure out what we're doing as far as error messages, etc.
        // These help us with our performance and messages internally
        internal unsafe char* charStart;
        internal unsafe char* charEnd;
        internal EncoderNLS encoder;
        internal bool setEncoder;
        internal bool bUsedEncoder;
        internal bool bFallingBack;
        internal int iRecursionCount;
        private const int iMaxRecursion = 250;
        private EncoderFallbackBuffer _fallbackBuffer;

        // Internal Reset
        // For example, what if someone fails a conversion and wants to reset one of our fallback buffers?
        internal unsafe void InternalReset()
        {
            charStart = null;
            bFallingBack = false;
            iRecursionCount = 0;
            _fallbackBuffer.Reset();
        }

        // Set the above values
        // This can't be part of the constructor because EncoderFallbacks would have to know how to implement these.
        internal unsafe void InternalInitialize(char* _charStart, char* _charEnd, EncoderNLS _encoder, bool _setEncoder)
        {
            charStart = _charStart;
            charEnd = _charEnd;
            encoder = _encoder;
            setEncoder = _setEncoder;
            bUsedEncoder = false;
            bFallingBack = false;
            iRecursionCount = 0;
        }

        internal char InternalGetNextChar()
        {
            char ch = _fallbackBuffer.GetNextChar();
            bFallingBack = (ch != 0);
            if (ch == 0) iRecursionCount = 0;
            return ch;
        }

        // Fallback the current character using the remaining buffer and encoder if necessary
        // This can only be called by our encodings (other have to use the public fallback methods), so
        // we can use our EncoderNLS here too.
        // setEncoder is true if we're calling from a GetBytes method, false if we're calling from a GetByteCount
        //
        // Note that this could also change the contents of encoder, which is the same
        // object that the caller is using, so the caller could mess up the encoder for us
        // if they aren't careful.
        internal unsafe bool InternalFallback(char ch, ref char* chars)
        {
            // Shouldn't have null charStart
            Debug.Assert(charStart != null, "[EncoderFallback.InternalFallbackBuffer]Fallback buffer is not initialized");

            // Get our index, remember chars was preincremented to point at next char, so have to decrement
            int index = (int)(chars - charStart) - 1;

            // See if it was a high surrogate
            if (Char.IsHighSurrogate(ch))
            {
                // See if there's a low surrogate to go with it
                if (chars >= charEnd)
                {
                    // Nothing left in input buffer
                    // No input, return 0 if mustflush is false
                    if (encoder != null && !encoder.MustFlush)
                    {
                        // Done, nothing to fallback
                        if (setEncoder)
                        {
                            bUsedEncoder = true;
                            encoder.charLeftOver = ch;
                        }
                        bFallingBack = false;
                        return false;
                    }
                }
                else
                {
                    // Might have a low surrogate
                    char cNext = *chars;
                    if (Char.IsLowSurrogate(cNext))
                    {
                        // If already falling back then fail
                        if (bFallingBack && iRecursionCount++ > iMaxRecursion)
                            ThrowLastCharRecursive(Char.ConvertToUtf32(ch, cNext));

                        // Next is a surrogate, add it as surrogate pair, and increment chars
                        chars++;
                        bFallingBack = _fallbackBuffer.Fallback(ch, cNext, index);
                        return bFallingBack;
                    }
                    // Next isn't a low surrogate, just fallback the high surrogate
                }
            }

            // If already falling back then fail
            if (bFallingBack && iRecursionCount++ > iMaxRecursion)
                ThrowLastCharRecursive((int)ch);

            // Fall back our char
            bFallingBack = _fallbackBuffer.Fallback(ch, index);

            return bFallingBack;
        }

        // private helper methods
        internal void ThrowLastCharRecursive(int charRecursive)
        {
            // Throw it, using our complete character
            throw new ArgumentException(SR.Format(SR.Argument_RecursiveFallback, charRecursive), "chars");
        }
    }
}


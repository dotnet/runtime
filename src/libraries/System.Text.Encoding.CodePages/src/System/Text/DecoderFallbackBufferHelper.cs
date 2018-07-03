// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text
{
    internal unsafe struct DecoderFallbackBufferHelper
    {
        // Internal items to help us figure out what we're doing as far as error messages, etc.
        // These help us with our performance and messages internally
        internal unsafe byte* byteStart;
        internal unsafe char* charEnd; private DecoderFallbackBuffer _fallbackBuffer;

        public DecoderFallbackBufferHelper(DecoderFallbackBuffer fallbackBuffer)
        {
            _fallbackBuffer = fallbackBuffer;
            byteStart = null;
            charEnd = null;
        }

        internal unsafe void InternalReset()
        {
            byteStart = null;
            _fallbackBuffer.Reset();
        }

        internal void InternalInitialize(byte* _byteStart, char* _charEnd)
        {
            byteStart = _byteStart;
            charEnd = _charEnd;
        }

        // Fallback the current byte by sticking it into the remaining char buffer.
        // This can only be called by our encodings (other have to use the public fallback methods), so
        // we can use our DecoderNLS here too (except we don't).
        // Returns true if we are successful, false if we can't fallback the character (no buffer space)
        // So the caller needs to throw buffer space if the method returns false.
        // Right now this has both bytes and bytes[], since we might have extra bytes, hence the
        // array, and we might need the index, hence the byte*
        // Don't touch ref chars unless we succeed
        internal bool InternalFallback(byte[] bytes, byte* pBytes, ref char* chars)
        {
            Debug.Assert(byteStart != null, "[DecoderFallback.InternalFallback]Used InternalFallback without calling InternalInitialize");

            // See if there's a fallback character and we have an output buffer then copy our string.
            if (_fallbackBuffer.Fallback(bytes, (int)(pBytes - byteStart - bytes.Length)))
            {
                // Copy the chars to our output
                char ch;
                char* charTemp = chars;
                bool bHighSurrogate = false;
                while ((ch = _fallbackBuffer.GetNextChar()) != 0)
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
        internal unsafe int InternalFallback(byte[] bytes, byte* pBytes)
        // Right now this has both bytes and bytes[], since we might have extra bytes, hence the
        // array, and we might need the index, hence the byte*
        {
            Debug.Assert(byteStart != null, "[DecoderFallback.InternalFallback]Used InternalFallback without calling InternalInitialize");

            // See if there's a fallback character and we have an output buffer then copy our string.
            if (_fallbackBuffer.Fallback(bytes, (int)(pBytes - byteStart - bytes.Length)))
            {
                int count = 0;

                char ch;
                bool bHighSurrogate = false;
                while ((ch = _fallbackBuffer.GetNextChar()) != 0)
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
    }
}

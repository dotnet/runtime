// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This is used internally to create best fit behavior as per the original windows best fit behavior.
//

using System.Diagnostics;
using System.Globalization;

namespace System.Text
{
    internal sealed class EncoderLatin1BestFitFallback : EncoderFallback
    {
        // Provides access to the singleton instance of this fallback mechanism
        internal static readonly EncoderLatin1BestFitFallback SingletonInstance = new EncoderLatin1BestFitFallback();

        private EncoderLatin1BestFitFallback()
        {
        }

        public override EncoderFallbackBuffer CreateFallbackBuffer() =>
            new EncoderLatin1BestFitFallbackBuffer();

        // Maximum number of characters that this instance of this fallback could return
        public override int MaxCharCount => 1;
    }

    internal sealed partial class EncoderLatin1BestFitFallbackBuffer : EncoderFallbackBuffer
    {
        // Our variables
        private char _cBestFit;
        private int _iCount = -1;
        private int _iSize;

        // Fallback methods
        public override bool Fallback(char charUnknown, int index)
        {
            // If we had a buffer already we're being recursive, throw, it's probably at the suspect
            // character in our array.
            // Shouldn't be able to get here for all of our code pages, table would have to be messed up.
            Debug.Assert(_iCount < 1, $"[EncoderLatin1BestFitFallbackBuffer.Fallback(non surrogate)] Fallback char {(int)_cBestFit:X4} caused recursive fallback");

            _iCount = _iSize = 1;
            _cBestFit = TryBestFit(charUnknown);
            if (_cBestFit == '\0')
                _cBestFit = '?';

            return true;
        }

        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
        {
            // Double check input surrogate pair
            if (!char.IsHighSurrogate(charUnknownHigh))
                throw new ArgumentOutOfRangeException(nameof(charUnknownHigh),
                    SR.Format(SR.ArgumentOutOfRange_Range,
                    0xD800, 0xDBFF));

            if (!char.IsLowSurrogate(charUnknownLow))
                throw new ArgumentOutOfRangeException(nameof(charUnknownLow),
                    SR.Format(SR.ArgumentOutOfRange_Range,
                    0xDC00, 0xDFFF));

            // If we had a buffer already we're being recursive, throw, it's probably at the suspect
            // character in our array.  0 is processing last character, < 0 is not falling back
            // Shouldn't be able to get here, table would have to be messed up.
            Debug.Assert(_iCount < 1, $"[EncoderLatin1BestFitFallbackBuffer.Fallback(surrogate)] Fallback char {(int)_cBestFit:X4} caused recursive fallback");

            // Go ahead and get our fallback, surrogates don't have best fit
            _cBestFit = '?';
            _iCount = _iSize = 2;

            return true;
        }

        // Default version is overridden in EncoderReplacementFallback.cs
        public override char GetNextChar()
        {
            // We want it to get < 0 because == 0 means that the current/last character is a fallback
            // and we need to detect recursion.  We could have a flag but we already have this counter.
            _iCount--;

            // Do we have anything left? 0 is now last fallback char, negative is nothing left
            if (_iCount < 0)
                return '\0';

            // Need to get it out of the buffer.
            // Make sure it didn't wrap from the fast count-- path
            if (_iCount == int.MaxValue)
            {
                _iCount = -1;
                return '\0';
            }

            // Return the best fit character
            return _cBestFit;
        }

        public override bool MovePrevious()
        {
            // Exception fallback doesn't have anywhere to back up to.
            if (_iCount >= 0)
                _iCount++;

            // Return true if we could do it.
            return _iCount >= 0 && _iCount <= _iSize;
        }

        // How many characters left to output?
        public override int Remaining => (_iCount > 0) ? _iCount : 0;

        // Clear the buffer
        public override unsafe void Reset()
        {
            _iCount = -1;
            charStart = null;
            bFallingBack = false;
        }

        // private helper methods
        private static char TryBestFit(char cUnknown)
        {
            // Need to figure out our best fit character, low is beginning of array, high is 1 AFTER end of array
            int lowBound = 0;
            Debug.Assert(s_arrayCharBestFit != null);
            int highBound = s_arrayCharBestFit.Length;
            int index;

            // Binary search the array
            int iDiff;
            while ((iDiff = (highBound - lowBound)) > 6)
            {
                // Look in the middle, which is complicated by the fact that we have 2 #s for each pair,
                // so we don't want index to be odd because we want to be on word boundaries.
                // Also note that index can never == highBound (because diff is rounded down)
                index = ((iDiff / 2) + lowBound) & 0xFFFE;

                char cTest = s_arrayCharBestFit[index];
                if (cTest == cUnknown)
                {
                    // We found it
                    Debug.Assert(index + 1 < s_arrayCharBestFit.Length,
                        "[EncoderLatin1BestFitFallbackBuffer.TryBestFit]Expected replacement character at end of array");
                    return s_arrayCharBestFit[index + 1];
                }
                else if (cTest < cUnknown)
                {
                    // We weren't high enough
                    lowBound = index;
                }
                else
                {
                    // We weren't low enough
                    highBound = index;
                }
            }

            for (index = lowBound; index < highBound; index += 2)
            {
                if (s_arrayCharBestFit[index] == cUnknown)
                {
                    // We found it
                    Debug.Assert(index + 1 < s_arrayCharBestFit.Length,
                        "[EncoderLatin1BestFitFallbackBuffer.TryBestFit]Expected replacement character at end of array");
                    return s_arrayCharBestFit[index + 1];
                }
            }

            // Char wasn't in our table
            return '\0';
        }
    }
}

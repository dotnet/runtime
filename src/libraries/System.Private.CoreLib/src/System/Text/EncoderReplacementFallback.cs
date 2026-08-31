// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text
{
    public sealed class EncoderReplacementFallback : EncoderFallback
    {
        internal static readonly EncoderReplacementFallback s_default = new EncoderReplacementFallback();

        // Our variables
        private readonly string _strDefault;

        // Construction.  Default replacement fallback uses no best fit and ? replacement string
        public EncoderReplacementFallback() : this("?")
        {
        }

        public EncoderReplacementFallback(string replacement)
        {
            ArgumentNullException.ThrowIfNull(replacement);

            // Make sure it doesn't have bad surrogate pairs
            bool bFoundHigh = false;
            foreach (char c in replacement)
            {
                // Found a surrogate?
                if (char.IsSurrogate(c))
                {
                    // High or Low?
                    if (char.IsHighSurrogate(c))
                    {
                        // if already had a high one, stop
                        if (bFoundHigh)
                            break;  // break & throw at the bFoundHIgh below
                        bFoundHigh = true;
                    }
                    else
                    {
                        // Low, did we have a high?
                        if (!bFoundHigh)
                        {
                            // Didn't have one, make if fail when we stop
                            bFoundHigh = true;
                            break;
                        }

                        // Clear flag
                        bFoundHigh = false;
                    }
                }
                // If last was high we're in trouble (not surrogate so not low surrogate, so break)
                else if (bFoundHigh)
                    break;
            }
            if (bFoundHigh)
                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, nameof(replacement));

            _strDefault = replacement;
        }

        public string DefaultString => _strDefault;

        public override EncoderFallbackBuffer CreateFallbackBuffer() =>
            new EncoderReplacementFallbackBuffer(this);

        // Maximum number of characters that this instance of this fallback could return
        public override int MaxCharCount => _strDefault.Length;

        public override bool Equals([NotNullWhen(true)] object? value) =>
            value is EncoderReplacementFallback that &&
            _strDefault == that._strDefault;

        public override int GetHashCode() => _strDefault.GetHashCode();
    }

    public sealed class EncoderReplacementFallbackBuffer : EncoderFallbackBuffer
    {
        // Store our default string
        private readonly string _strDefault;
        private int _fallbackCount = -1;
        private int _fallbackIndex = -1;

        // Construction
        public EncoderReplacementFallbackBuffer(EncoderReplacementFallback fallback)
        {
            // 2X in case we're a surrogate pair
            _strDefault = fallback.DefaultString + fallback.DefaultString;
        }

        // Fallback Methods
        public override bool Fallback(char charUnknown, int index)
        {
            // If we had a buffer already we're being recursive, throw, it's probably at the suspect
            // character in our array.
            if (_fallbackCount >= 1)
            {
                // If we're recursive we may still have something in our buffer that makes this a surrogate
                if (char.IsHighSurrogate(charUnknown) && _fallbackCount >= 0 &&
                    char.IsLowSurrogate(_strDefault[_fallbackIndex + 1]))
                    ThrowLastCharRecursive(char.ConvertToUtf32(charUnknown, _strDefault[_fallbackIndex + 1]));

                // Nope, just one character
                ThrowLastCharRecursive(unchecked((int)charUnknown));
            }

            // Go ahead and get our fallback
            // Divide by 2 because we aren't a surrogate pair
            _fallbackCount = _strDefault.Length / 2;
            _fallbackIndex = -1;

            return _fallbackCount != 0;
        }

        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
        {
            // Double check input surrogate pair
            if (!char.IsHighSurrogate(charUnknownHigh))
                throw new ArgumentOutOfRangeException(nameof(charUnknownHigh),
                    SR.Format(SR.ArgumentOutOfRange_Range, 0xD800, 0xDBFF));

            if (!char.IsLowSurrogate(charUnknownLow))
                throw new ArgumentOutOfRangeException(nameof(charUnknownLow),
                    SR.Format(SR.ArgumentOutOfRange_Range, 0xDC00, 0xDFFF));

            // If we had a buffer already we're being recursive, throw, it's probably at the suspect
            // character in our array.
            if (_fallbackCount >= 1)
                ThrowLastCharRecursive(char.ConvertToUtf32(charUnknownHigh, charUnknownLow));

            // Go ahead and get our fallback
            _fallbackCount = _strDefault.Length;
            _fallbackIndex = -1;

            return _fallbackCount != 0;
        }

        public override char GetNextChar()
        {
            // We want it to get < 0 because == 0 means that the current/last character is a fallback
            // and we need to detect recursion.  We could have a flag but we already have this counter.
            _fallbackCount--;
            _fallbackIndex++;

            // Do we have anything left? 0 is now last fallback char, negative is nothing left
            if (_fallbackCount < 0)
                return '\0';

            // Need to get it out of the buffer.
            // Make sure it didn't wrap from the fast count-- path
            if (_fallbackCount == int.MaxValue)
            {
                _fallbackCount = -1;
                return '\0';
            }

            // Now make sure its in the expected range
            Debug.Assert(_fallbackIndex < _strDefault.Length && _fallbackIndex >= 0,
                            "Index exceeds buffer range");

            return _strDefault[_fallbackIndex];
        }

        public override bool MovePrevious()
        {
            // Back up one, only if we just processed the last character (or earlier)
            if (_fallbackCount >= -1 && _fallbackIndex >= 0)
            {
                _fallbackIndex--;
                _fallbackCount++;
                return true;
            }

            // Return false 'cause we couldn't do it.
            return false;
        }

        // How many characters left to output?
        public override int Remaining =>
                // Our count is 0 for 1 character left.
                (_fallbackCount < 0) ? 0 : _fallbackCount;

        // Clear the buffer
        public override unsafe void Reset()
        {
            _fallbackCount = -1;
            _fallbackIndex = 0;
            charStart = null;
            bFallingBack = false;
        }
    }
}

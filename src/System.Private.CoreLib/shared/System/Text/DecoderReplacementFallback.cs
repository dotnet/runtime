// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text
{
    public sealed class DecoderReplacementFallback : DecoderFallback
    {
        // Our variables
        private string _strDefault;

        // Construction.  Default replacement fallback uses no best fit and ? replacement string
        public DecoderReplacementFallback() : this("?")
        {
        }

        public DecoderReplacementFallback(string replacement)
        {
            if (replacement == null)
                throw new ArgumentNullException(nameof(replacement));

            // Make sure it doesn't have bad surrogate pairs
            bool bFoundHigh = false;
            for (int i = 0; i < replacement.Length; i++)
            {
                // Found a surrogate?
                if (char.IsSurrogate(replacement, i))
                {
                    // High or Low?
                    if (char.IsHighSurrogate(replacement, i))
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
                throw new ArgumentException(SR.Format(SR.Argument_InvalidCharSequenceNoIndex, nameof(replacement)));

            _strDefault = replacement;
        }

        public string DefaultString
        {
            get
            {
                return _strDefault;
            }
        }

        public override DecoderFallbackBuffer CreateFallbackBuffer()
        {
            return new DecoderReplacementFallbackBuffer(this);
        }

        // Maximum number of characters that this instance of this fallback could return
        public override int MaxCharCount
        {
            get
            {
                return _strDefault.Length;
            }
        }

        public override bool Equals(object? value)
        {
            if (value is DecoderReplacementFallback that)
            {
                return _strDefault == that._strDefault;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _strDefault.GetHashCode();
        }
    }



    public sealed class DecoderReplacementFallbackBuffer : DecoderFallbackBuffer
    {
        // Store our default string
        private string _strDefault;
        private int _fallbackCount = -1;
        private int _fallbackIndex = -1;

        // Construction
        public DecoderReplacementFallbackBuffer(DecoderReplacementFallback fallback)
        {
            // TODO-NULLABLE: NullReferenceException (fallback)
            _strDefault = fallback.DefaultString;
        }

        // Fallback Methods
        public override bool Fallback(byte[] bytesUnknown, int index)
        {
            // We expect no previous fallback in our buffer
            // We can't call recursively but others might (note, we don't test on last char!!!)
            if (_fallbackCount >= 1)
            {
                ThrowLastBytesRecursive(bytesUnknown);
            }

            // Go ahead and get our fallback
            if (_strDefault.Length == 0)
                return false;

            _fallbackCount = _strDefault.Length;
            _fallbackIndex = -1;

            return true;
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
        public override int Remaining
        {
            get
            {
                // Our count is 0 for 1 character left.
                return (_fallbackCount < 0) ? 0 : _fallbackCount;
            }
        }

        // Clear the buffer
        public override unsafe void Reset()
        {
            _fallbackCount = -1;
            _fallbackIndex = -1;
            byteStart = null;
        }

        // This version just counts the fallback and doesn't actually copy anything.
        internal unsafe override int InternalFallback(byte[] bytes, byte* pBytes)
        // Right now this has both bytes and bytes[], since we might have extra bytes, hence the
        // array, and we might need the index, hence the byte*
        {
            // return our replacement string Length
            return _strDefault.Length;
        }
    }
}


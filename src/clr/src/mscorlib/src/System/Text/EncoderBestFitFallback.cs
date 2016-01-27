// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This is used internally to create best fit behavior as per the original windows best fit behavior.
//
namespace System.Text
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Diagnostics.Contracts;

    [Serializable]
    internal class InternalEncoderBestFitFallback : EncoderFallback
    {
        // Our variables
        internal Encoding encoding = null;
        internal char[]   arrayBestFit = null;

        internal InternalEncoderBestFitFallback(Encoding encoding)
        {
            // Need to load our replacement characters table.
            this.encoding = encoding;
            this.bIsMicrosoftBestFitFallback = true;
        }

        public override EncoderFallbackBuffer CreateFallbackBuffer()
        {
            return new InternalEncoderBestFitFallbackBuffer(this);
        }

        // Maximum number of characters that this instance of this fallback could return
        public override int MaxCharCount
        {
            get
            {
                return 1;
            }
        }

        public override bool Equals(Object value)
        {
            InternalEncoderBestFitFallback that = value as InternalEncoderBestFitFallback;
            if (that != null)
            {
                return (this.encoding.CodePage == that.encoding.CodePage);
            }
            return (false);
        }

        public override int GetHashCode()
        {
            return this.encoding.CodePage;
        }
    }

    internal sealed class InternalEncoderBestFitFallbackBuffer : EncoderFallbackBuffer
    {
        // Our variables
        private char                    cBestFit = '\0';
        private InternalEncoderBestFitFallback  oFallback;
        private int                     iCount = -1;
        private int                     iSize;

        // Private object for locking instead of locking on a public type for SQL reliability work.
        private static Object s_InternalSyncObject;
        private static Object InternalSyncObject
        {
            get
            {
                if (s_InternalSyncObject == null)
                {
                    Object o = new Object();
                    Interlocked.CompareExchange<Object>(ref s_InternalSyncObject, o, null);
                }
                return s_InternalSyncObject;
            }
        }

        // Constructor
        public InternalEncoderBestFitFallbackBuffer(InternalEncoderBestFitFallback fallback)
        {
            this.oFallback = fallback;

            if (oFallback.arrayBestFit == null)
            {
                // Lock so we don't confuse ourselves.
                lock(InternalSyncObject)
                {
                    // Double check before we do it again.
                    if (oFallback.arrayBestFit == null)
                        oFallback.arrayBestFit = fallback.encoding.GetBestFitUnicodeToBytesData();
                }
            }
        }

        // Fallback methods
        public override bool Fallback(char charUnknown, int index)
        {
            // If we had a buffer already we're being recursive, throw, it's probably at the suspect
            // character in our array.
            // Shouldn't be able to get here for all of our code pages, table would have to be messed up.
            Contract.Assert(iCount < 1, "[InternalEncoderBestFitFallbackBuffer.Fallback(non surrogate)] Fallback char " + ((int)cBestFit).ToString("X4", CultureInfo.InvariantCulture) + " caused recursive fallback");

            iCount = iSize = 1;
            cBestFit = TryBestFit(charUnknown);
            if (cBestFit == '\0')
                cBestFit = '?';

            return true;
        }

        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
        {
            // Double check input surrogate pair
            if (!Char.IsHighSurrogate(charUnknownHigh))
                throw new ArgumentOutOfRangeException("charUnknownHigh",
                    Environment.GetResourceString("ArgumentOutOfRange_Range",
                    0xD800, 0xDBFF));

            if (!Char.IsLowSurrogate(charUnknownLow))
                throw new ArgumentOutOfRangeException("CharUnknownLow",
                    Environment.GetResourceString("ArgumentOutOfRange_Range",
                    0xDC00, 0xDFFF));
            Contract.EndContractBlock();

            // If we had a buffer already we're being recursive, throw, it's probably at the suspect
            // character in our array.  0 is processing last character, < 0 is not falling back
            // Shouldn't be able to get here, table would have to be messed up.
            Contract.Assert(iCount < 1, "[InternalEncoderBestFitFallbackBuffer.Fallback(surrogate)] Fallback char " + ((int)cBestFit).ToString("X4", CultureInfo.InvariantCulture) + " caused recursive fallback");

            // Go ahead and get our fallback, surrogates don't have best fit
            cBestFit = '?';
            iCount = iSize = 2;

            return true;
        }

        // Default version is overridden in EncoderReplacementFallback.cs
        public override char GetNextChar()
        {
            // We want it to get < 0 because == 0 means that the current/last character is a fallback
            // and we need to detect recursion.  We could have a flag but we already have this counter.
            iCount--;
            
            // Do we have anything left? 0 is now last fallback char, negative is nothing left
            if (iCount < 0)
                return '\0';

            // Need to get it out of the buffer.
            // Make sure it didn't wrap from the fast count-- path
            if (iCount == int.MaxValue)
            {
                iCount = -1;
                return '\0';
            }

            // Return the best fit character
            return cBestFit;
        }

        public override bool MovePrevious()
        {
            // Exception fallback doesn't have anywhere to back up to.
            if (iCount >= 0)
                iCount++;

            // Return true if we could do it.
            return (iCount >= 0 && iCount <= iSize);
        }


        // How many characters left to output?
        public override int Remaining
        {
            get
            {
                return (iCount > 0) ? iCount : 0;
            }
        }

        // Clear the buffer
        [System.Security.SecuritySafeCritical] // overrides public transparent member
        public override unsafe void Reset()
        {
            iCount = -1;
            charStart = null;
            bFallingBack = false;
        }

        // private helper methods
        private char TryBestFit(char cUnknown)
        {
            // Need to figure out our best fit character, low is beginning of array, high is 1 AFTER end of array
            int lowBound = 0;
            int highBound = oFallback.arrayBestFit.Length;
            int index;

            // Binary search the array
            int iDiff;
            while ((iDiff = (highBound - lowBound)) > 6)
            {
                // Look in the middle, which is complicated by the fact that we have 2 #s for each pair,
                // so we don't want index to be odd because we want to be on word boundaries.
                // Also note that index can never == highBound (because diff is rounded down)
                index = ((iDiff / 2) + lowBound) & 0xFFFE;

                char cTest = oFallback.arrayBestFit[index];
                if (cTest == cUnknown)
                {
                    // We found it
                    Contract.Assert(index + 1 < oFallback.arrayBestFit.Length,
                        "[InternalEncoderBestFitFallbackBuffer.TryBestFit]Expected replacement character at end of array");
                    return oFallback.arrayBestFit[index + 1];
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
                if (oFallback.arrayBestFit[index] == cUnknown)
                {
                    // We found it
                    Contract.Assert(index + 1 < oFallback.arrayBestFit.Length,
                        "[InternalEncoderBestFitFallbackBuffer.TryBestFit]Expected replacement character at end of array");
                    return oFallback.arrayBestFit[index + 1];
                }
            }

            // Char wasn't in our table
            return '\0';
        }
    }
}


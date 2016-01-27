// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Threading;
using System.Diagnostics.Contracts;

namespace System.Text
{
    [Serializable]
    public abstract class EncoderFallback
    {
// disable csharp compiler warning #0414: field assigned unused value
#pragma warning disable 0414
        internal bool                 bIsMicrosoftBestFitFallback = false;
#pragma warning restore 0414

        private static volatile EncoderFallback replacementFallback; // Default fallback, uses no best fit & "?"
        private static volatile EncoderFallback exceptionFallback;

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

        // Get each of our generic fallbacks.

        public static EncoderFallback ReplacementFallback
        {
            get
            {
                if (replacementFallback == null)
                    lock(InternalSyncObject)
                        if (replacementFallback == null)
                            replacementFallback = new EncoderReplacementFallback();

                return replacementFallback;
            }
        }


        public static EncoderFallback ExceptionFallback
        {
            get
            {
                if (exceptionFallback == null)
                    lock(InternalSyncObject)
                        if (exceptionFallback == null)
                            exceptionFallback = new EncoderExceptionFallback();

                return exceptionFallback;
            }
        }

        // Fallback
        //
        // Return the appropriate unicode string alternative to the character that need to fall back.
        // Most implimentations will be:
        //      return new MyCustomEncoderFallbackBuffer(this);

        public abstract EncoderFallbackBuffer CreateFallbackBuffer();

        // Maximum number of characters that this instance of this fallback could return

        public abstract int MaxCharCount { get; }
    }


    public abstract class EncoderFallbackBuffer
    {
        // Most implementations will probably need an implemenation-specific constructor

        // Public methods that cannot be overriden that let us do our fallback thing
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
            while (GetNextChar() != (char)0);
        }

        // Internal items to help us figure out what we're doing as far as error messages, etc.
        // These help us with our performance and messages internally
        [SecurityCritical]
        internal    unsafe char*   charStart;
        [SecurityCritical]
        internal    unsafe char*   charEnd;
        internal    EncoderNLS     encoder;
        internal    bool           setEncoder;
        internal    bool           bUsedEncoder;
        internal    bool           bFallingBack = false;
        internal    int            iRecursionCount = 0;
        private const int          iMaxRecursion = 250;

        // Internal Reset
        // For example, what if someone fails a conversion and wants to reset one of our fallback buffers?
        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe void InternalReset()
        {
            charStart = null;
            bFallingBack = false;
            iRecursionCount = 0;
            Reset();
        }

        // Set the above values
        // This can't be part of the constructor because EncoderFallbacks would have to know how to impliment these.
        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe void InternalInitialize(char* charStart, char* charEnd, EncoderNLS encoder, bool setEncoder)
        {
            this.charStart = charStart;
            this.charEnd = charEnd;
            this.encoder = encoder;
            this.setEncoder = setEncoder;
            this.bUsedEncoder = false;
            this.bFallingBack = false;
            this.iRecursionCount = 0;
        }

        internal char InternalGetNextChar()
        {
            char ch = GetNextChar();
            bFallingBack = (ch != 0);
            if (ch == 0) iRecursionCount = 0;
            return ch;
        }

        // Fallback the current character using the remaining buffer and encoder if necessary
        // This can only be called by our encodings (other have to use the public fallback methods), so
        // we can use our EncoderNLS here too.
        // setEncoder is true if we're calling from a GetBytes method, false if we're calling from a GetByteCount
        //
        // Note that this could also change the contents of this.encoder, which is the same
        // object that the caller is using, so the caller could mess up the encoder for us
        // if they aren't careful.
        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe virtual bool InternalFallback(char ch, ref char* chars)
        {
            // Shouldn't have null charStart
            Contract.Assert(charStart != null,
                "[EncoderFallback.InternalFallbackBuffer]Fallback buffer is not initialized");

            // Get our index, remember chars was preincremented to point at next char, so have to -1
            int index = (int)(chars - charStart) - 1;

            // See if it was a high surrogate
            if (Char.IsHighSurrogate(ch))
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
                            this.encoder.charLeftOver = ch;
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
                Environment.GetResourceString("Argument_RecursiveFallback",
                    charRecursive), "chars");
        }

    }
}

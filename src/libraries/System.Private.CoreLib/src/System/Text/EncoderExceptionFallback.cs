// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace System.Text
{
    public sealed class EncoderExceptionFallback : EncoderFallback
    {
        internal static readonly EncoderExceptionFallback s_default = new EncoderExceptionFallback();

        // Construction
        public EncoderExceptionFallback()
        {
        }

        public override EncoderFallbackBuffer CreateFallbackBuffer() =>
            new EncoderExceptionFallbackBuffer();

        // Maximum number of characters that this instance of this fallback could return
        public override int MaxCharCount => 0;

        public override bool Equals([NotNullWhen(true)] object? value) =>
            value is EncoderExceptionFallback;

        public override int GetHashCode() => 654;
    }


    public sealed class EncoderExceptionFallbackBuffer : EncoderFallbackBuffer
    {
        public EncoderExceptionFallbackBuffer() { }
        public override bool Fallback(char charUnknown, int index)
        {
            // Fall back our char
            throw new EncoderFallbackException(
                SR.Format(SR.Argument_InvalidCodePageConversionIndex, (int)charUnknown, index), charUnknown, index);
        }

        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
        {
            if (!char.IsHighSurrogate(charUnknownHigh))
            {
                throw new ArgumentOutOfRangeException(nameof(charUnknownHigh),
                    SR.Format(SR.ArgumentOutOfRange_Range, 0xD800, 0xDBFF));
            }
            if (!char.IsLowSurrogate(charUnknownLow))
            {
                throw new ArgumentOutOfRangeException(nameof(charUnknownLow),
                    SR.Format(SR.ArgumentOutOfRange_Range, 0xDC00, 0xDFFF));
            }

            int iTemp = char.ConvertToUtf32(charUnknownHigh, charUnknownLow);

            // Fall back our char
            throw new EncoderFallbackException(
                SR.Format(SR.Argument_InvalidCodePageConversionIndex, iTemp, index), charUnknownHigh, charUnknownLow, index);
        }

        public override char GetNextChar() => (char)0;

        // Exception fallback doesn't have anywhere to back up to.
        public override bool MovePrevious() => false;

        // Exceptions are always empty
        public override int Remaining => 0;
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class EncoderFallbackException : ArgumentException
    {
        private readonly char _charUnknown;
        private readonly char _charUnknownHigh;
        private readonly char _charUnknownLow;
        private readonly int _index;

        public EncoderFallbackException()
            : base(SR.Arg_ArgumentException)
        {
            HResult = HResults.COR_E_ARGUMENT;
        }

        public EncoderFallbackException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_ARGUMENT;
        }

        public EncoderFallbackException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_ARGUMENT;
        }

        internal EncoderFallbackException(
            string? message, char charUnknown, int index) : base(message)
        {
            _charUnknown = charUnknown;
            _index = index;
        }

        internal EncoderFallbackException(
            string message, char charUnknownHigh, char charUnknownLow, int index) : base(message)
        {
            if (!char.IsHighSurrogate(charUnknownHigh))
            {
                throw new ArgumentOutOfRangeException(nameof(charUnknownHigh),
                    SR.Format(SR.ArgumentOutOfRange_Range, 0xD800, 0xDBFF));
            }
            if (!char.IsLowSurrogate(charUnknownLow))
            {
                throw new ArgumentOutOfRangeException(nameof(CharUnknownLow),
                    SR.Format(SR.ArgumentOutOfRange_Range, 0xDC00, 0xDFFF));
            }

            _charUnknownHigh = charUnknownHigh;
            _charUnknownLow = charUnknownLow;
            _index = index;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private EncoderFallbackException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }

        public char CharUnknown => _charUnknown;

        public char CharUnknownHigh => _charUnknownHigh;

        public char CharUnknownLow => _charUnknownLow;

        public int Index => _index;

        // Return true if the unknown character is a surrogate pair.
        public bool IsUnknownSurrogate() => _charUnknownHigh != '\0';
    }
}

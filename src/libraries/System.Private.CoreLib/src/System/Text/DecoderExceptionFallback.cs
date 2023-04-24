// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;

namespace System.Text
{
    public sealed class DecoderExceptionFallback : DecoderFallback
    {
        internal static readonly DecoderExceptionFallback s_default = new DecoderExceptionFallback();

        public override DecoderFallbackBuffer CreateFallbackBuffer() =>
            new DecoderExceptionFallbackBuffer();

        // Maximum number of characters that this instance of this fallback could return
        public override int MaxCharCount => 0;

        public override bool Equals([NotNullWhen(true)] object? value) =>
            value is DecoderExceptionFallback;

        public override int GetHashCode() => 879;
    }


    public sealed class DecoderExceptionFallbackBuffer : DecoderFallbackBuffer
    {
        public override bool Fallback(byte[] bytesUnknown, int index)
        {
            Throw(bytesUnknown, index);
            return true;
        }

        public override char GetNextChar() => (char)0;

        // Exception fallback doesn't have anywhere to back up to.
        public override bool MovePrevious() => false;

        // Exceptions are always empty
        public override int Remaining => 0;

        [DoesNotReturn]
        private static void Throw(byte[] bytesUnknown, int index)
        {
            bytesUnknown ??= Array.Empty<byte>();

            // Create a string representation of our bytes.
            StringBuilder strBytes = new StringBuilder(bytesUnknown.Length * 4);

            const int MaxLength = 20;
            for (int i = 0; i < bytesUnknown.Length && i < MaxLength; i++)
            {
                strBytes.Append($"[{bytesUnknown[i]:X2}]");
            }

            // In case the string's really long
            if (bytesUnknown.Length > MaxLength)
            {
                strBytes.Append(" ...");
            }

            // Known index
            throw new DecoderFallbackException(
                SR.Format(SR.Argument_InvalidCodePageBytesIndex,
                   strBytes, index), bytesUnknown, index);
        }
    }

    // Exception for decoding unknown byte sequences.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class DecoderFallbackException : ArgumentException
    {
        private readonly byte[]? _bytesUnknown;
        private readonly int _index;

        public DecoderFallbackException()
            : base(SR.Arg_ArgumentException)
        {
            HResult = HResults.COR_E_ARGUMENT;
        }

        public DecoderFallbackException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_ARGUMENT;
        }

        public DecoderFallbackException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_ARGUMENT;
        }

        public DecoderFallbackException(string? message, byte[]? bytesUnknown, int index)
            : base(message)
        {
            _bytesUnknown = bytesUnknown;
            _index = index;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private DecoderFallbackException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }

        public byte[]? BytesUnknown => _bytesUnknown;

        public int Index => _index;
    }
}

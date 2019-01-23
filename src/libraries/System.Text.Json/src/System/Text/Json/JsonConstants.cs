// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    internal static class JsonConstants
    {
        public const byte OpenBrace = (byte)'{';
        public const byte CloseBrace = (byte)'}';
        public const byte OpenBracket = (byte)'[';
        public const byte CloseBracket = (byte)']';
        public const byte Space = (byte)' ';
        public const byte CarriageReturn = (byte)'\r';
        public const byte LineFeed = (byte)'\n';
        public const byte Tab = (byte)'\t';
        public const byte ListSeparator = (byte)',';
        public const byte KeyValueSeperator = (byte)':';
        public const byte Quote = (byte)'"';
        public const byte BackSlash = (byte)'\\';
        public const byte Slash = (byte)'/';
        public const byte BackSpace = (byte)'\b';
        public const byte FormFeed = (byte)'\f';
        public const byte Asterisk = (byte)'*';

        public static ReadOnlySpan<byte> TrueValue => new byte[] { (byte)'t', (byte)'r', (byte)'u', (byte)'e' };
        public static ReadOnlySpan<byte> FalseValue => new byte[] { (byte)'f', (byte)'a', (byte)'l', (byte)'s', (byte)'e' };
        public static ReadOnlySpan<byte> NullValue => new byte[] { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };

        // Used to search for the end of a number
        public static ReadOnlySpan<byte> Delimiters => new byte[] { ListSeparator, CloseBrace, CloseBracket, Space, LineFeed, CarriageReturn, Tab, Slash };

        // Explicitly skipping ReverseSolidus since that is handled separately
        public static ReadOnlySpan<byte> EscapableChars => new byte[] { Quote, (byte)'n', (byte)'r', (byte)'t', Slash, (byte)'u', (byte)'b', (byte)'f' };

        public const int SpacesPerIndent = 2;
        public const int MaxWriterDepth = 1_000;
        public const int RemoveFlagsBitMask = 0x7FFFFFFF;

        public const int StackallocThreshold = 256;

        // In the worst case, an ASCII character represented as a single utf-8 byte could expand 6x when escaped.
        // For example: '+' becomes '\u0043'
        // Escaping surrogate pairs (represented by 3 or 4 utf-8 bytes) would expand to 12 bytes (which is still <= 6x).
        // The same factor applies to utf-16 characters.
        public const int MaxExpansionFactorWhileEscaping = 6;

        public const int MaxTokenSize = 2_000_000_000 / MaxExpansionFactorWhileEscaping;  // 357_913_941 bytes
        public const int MaxCharacterTokenSize = 2_000_000_000 / MaxExpansionFactorWhileEscaping; // 357_913_941 characters

        public const int MaximumFormatInt64Length = 20;   // 19 + sign (i.e. -9223372036854775808)
        public const int MaximumFormatUInt64Length = 20;  // i.e. 18446744073709551615
        public const int MaximumFormatDoubleLength = 128;  // default (i.e. 'G'), using 128 (rather than say 32) to be future-proof.
        public const int MaximumFormatSingleLength = 128;  // default (i.e. 'G'), using 128 (rather than say 32) to be future-proof.
        public const int MaximumFormatDecimalLength = 31; // default (i.e. 'G')
        public const int MaximumFormatGuidLength = 36;    // default (i.e. 'D'), 8 + 4 + 4 + 4 + 12 + 4 for the hyphens (e.g. 094ffa0a-0442-494d-b452-04003fa755cc)
        public const int MaximumFormatDateTimeLength = 27;    // StandardFormat 'O', e.g. 2017-06-12T05:30:45.7680000
        public const int MaximumFormatDateTimeOffsetLength = 33;  // StandardFormat 'O', e.g. 2017-06-12T05:30:45.7680000-07:00

        internal const char ScientificNotationFormat = 'e';

        // Encoding Helpers
        public const char HighSurrogateStart = '\ud800';
        public const char HighSurrogateEnd = '\udbff';
        public const char LowSurrogateStart = '\udc00';
        public const char LowSurrogateEnd = '\udfff';

        public const int UnicodePlane01StartValue = 0x10000;
        public const int HighSurrogateStartValue = 0xD800;
        public const int HighSurrogateEndValue = 0xDBFF;
        public const int LowSurrogateStartValue = 0xDC00;
        public const int LowSurrogateEndValue = 0xDFFF;
        public const int BitShiftBy10 = 0x400;
    }
}

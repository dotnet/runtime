// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Buffers
{
    /// <summary>
    /// Represents a standard formatting string without using an actual String. A StandardFormat consists of a character (such as 'G', 'D' or 'X')
    /// and an optional precision ranging from 0..99, or the special value NoPrecision.
    /// </summary>
    public readonly struct StandardFormat : IEquatable<StandardFormat>
    {
        /// <summary>
        /// Precision values for format that don't use a precision, or for when the precision is to be unspecified.
        /// </summary>
        public const byte NoPrecision = byte.MaxValue;

        /// <summary>
        /// The maximum valid precision value.
        /// </summary>
        public const byte MaxPrecision = 99;

        private readonly byte _format;
        private readonly byte _precision;

        /// <summary>
        /// The character component of the format.
        /// </summary>
        public char Symbol => (char)_format;

        /// <summary>
        /// The precision component of the format. Ranges from 0..9 or the special value NoPrecision.
        /// </summary>
        public byte Precision => _precision;

        /// <summary>
        /// true if Precision is a value other than NoPrecision
        /// </summary>
        public bool HasPrecision => _precision != NoPrecision;

        /// <summary>
        /// true if the StandardFormat == default(StandardFormat)
        /// </summary>
        public bool IsDefault => _format == 0 && _precision == 0;

        /// <summary>
        /// Create a StandardFormat.
        /// </summary>
        /// <param name="symbol">A type-specific formatting character such as 'G', 'D' or 'X'</param>
        /// <param name="precision">An optional precision ranging from 0..9 or the special value NoPrecision (the default)</param>
        public StandardFormat(char symbol, byte precision = NoPrecision)
        {
            if (precision != NoPrecision && precision > MaxPrecision)
                ThrowHelper.ThrowArgumentOutOfRangeException_PrecisionTooLarge();
            if (symbol != (byte)symbol)
                ThrowHelper.ThrowArgumentOutOfRangeException_SymbolDoesNotFit();

            _format = (byte)symbol;
            _precision = precision;
        }

        /// <summary>
        /// Converts a character to a StandardFormat using the NoPrecision precision.
        /// </summary>
        public static implicit operator StandardFormat(char symbol) => new StandardFormat(symbol);

        /// <summary>
        /// Converts a <see cref="ReadOnlySpan{Char}"/> into a StandardFormat
        /// </summary>
        public static StandardFormat Parse([StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format)
        {
            ParseHelper(format, out StandardFormat standardFormat, throws: true);

            return standardFormat;
        }

        /// <summary>
        /// Converts a classic .NET format string into a StandardFormat
        /// </summary>
        public static StandardFormat Parse([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => format == null ? default : Parse(format.AsSpan());

        /// <summary>
        /// Tries to convert a <see cref="ReadOnlySpan{Char}"/> into a StandardFormat. A return value indicates whether the conversion succeeded or failed.
        /// </summary>
        public static bool TryParse([StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format, out StandardFormat result)
        {
            return ParseHelper(format, out result);
        }

        private static bool ParseHelper(ReadOnlySpan<char> format, out StandardFormat standardFormat, bool throws = false)
        {
            standardFormat = default;

            if (format.Length == 0)
                return true;

            char symbol = format[0];
            byte precision;
            if (format.Length == 1)
            {
                precision = NoPrecision;
            }
            else
            {
                uint parsedPrecision = 0;
                for (int srcIndex = 1; srcIndex < format.Length; srcIndex++)
                {
                    uint digit = format[srcIndex] - 48u; // '0'
                    if (digit > 9)
                    {
                        return throws ? throw new FormatException(SR.Format(SR.Argument_CannotParsePrecision, MaxPrecision)) : false;
                    }
                    parsedPrecision = parsedPrecision * 10 + digit;
                    if (parsedPrecision > MaxPrecision)
                    {
                        return throws ? throw new FormatException(SR.Format(SR.Argument_PrecisionTooLarge, MaxPrecision)) : false;
                    }
                }

                precision = (byte)parsedPrecision;
            }

            standardFormat = new StandardFormat(symbol, precision);
            return true;
        }

        /// <summary>
        /// Returns true if both the Symbol and Precision are equal.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is StandardFormat other && Equals(other);

        /// <summary>
        /// Compute a hash code.
        /// </summary>
        public override int GetHashCode() => _format.GetHashCode() ^ _precision.GetHashCode();

        /// <summary>
        /// Returns true if both the Symbol and Precision are equal.
        /// </summary>
        public bool Equals(StandardFormat other) => _format == other._format && _precision == other._precision;

        /// <summary>
        /// Returns the format in classic .NET format.
        /// </summary>
        public override string ToString()
        {
            Span<char> buffer = stackalloc char[FormatStringLength];
            int charsWritten = Format(buffer);
            return new string(buffer.Slice(0, charsWritten));
        }

        /// <summary>The exact buffer length required by <see cref="Format"/>.</summary>
        internal const int FormatStringLength = 3;

        /// <summary>
        /// Formats the format in classic .NET format.
        /// </summary>
        internal int Format(Span<char> destination)
        {
            Debug.Assert(destination.Length == FormatStringLength);

            int count = 0;
            char symbol = Symbol;

            if (symbol != default &&
                (uint)destination.Length == FormatStringLength) // to eliminate bounds checks
            {
                destination[0] = symbol;
                count = 1;

                uint precision = Precision;
                if (precision != NoPrecision)
                {
                    // Note that Precision is stored as a byte, so in theory it could contain
                    // values > MaxPrecision (99).  But all supported mechanisms for creating a
                    // StandardFormat limit values to being <= MaxPrecision, so the only way a value
                    // could be larger than that is if unsafe code or the equivalent were used
                    // to force a larger invalid value in, in which case we don't need to
                    // guarantee such an invalid value is properly roundtripped through here;
                    // we just need to make sure things aren't corrupted further.

                    if (precision >= 10)
                    {
                        uint div;
                        (div, precision) = Math.DivRem(precision, 10);
                        destination[1] = (char)('0' + div % 10);
                        count = 2;
                    }

                    destination[count] = (char)('0' + precision);
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Returns true if both the Symbol and Precision are equal.
        /// </summary>
        public static bool operator ==(StandardFormat left, StandardFormat right) => left.Equals(right);

        /// <summary>
        /// Returns false if both the Symbol and Precision are equal.
        /// </summary>
        public static bool operator !=(StandardFormat left, StandardFormat right) => !left.Equals(right);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Text
{
    /// <summary>
    /// Represents a Unicode scalar value ([ U+0000..U+D7FF ], inclusive; or [ U+E000..U+10FFFF ], inclusive).
    /// </summary>
    /// <remarks>
    /// This type's constructors and conversion operators validate the input, so consumers can call the APIs
    /// assuming that the underlying <see cref="Rune"/> instance is well-formed.
    /// </remarks>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly struct Rune : IComparable<Rune>, IEquatable<Rune>
    {
        private const byte IsWhiteSpaceFlag = 0x80;
        private const byte IsLetterOrDigitFlag = 0x40;
        private const byte UnicodeCategoryMask = 0x1F;

        // Contains information about the ASCII character range [ U+0000..U+007F ], with:
        // - 0x80 bit if set means 'is whitespace'
        // - 0x40 bit if set means 'is letter or digit'
        // - 0x20 bit is reserved for future use
        // - bottom 5 bits are the UnicodeCategory of the character
        private static ReadOnlySpan<byte> AsciiCharInfo => new byte[]
        {
            0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x8E, 0x8E, 0x8E, 0x8E, 0x8E, 0x0E, 0x0E,
            0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E,
            0x8B, 0x18, 0x18, 0x18, 0x1A, 0x18, 0x18, 0x18, 0x14, 0x15, 0x18, 0x19, 0x18, 0x13, 0x18, 0x18,
            0x48, 0x48, 0x48, 0x48, 0x48, 0x48, 0x48, 0x48, 0x48, 0x48, 0x18, 0x18, 0x19, 0x19, 0x19, 0x18,
            0x18, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40,
            0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x14, 0x18, 0x15, 0x1B, 0x12,
            0x1B, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41,
            0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x14, 0x19, 0x15, 0x19, 0x0E
        };

        private readonly uint _value;

        /// <summary>
        /// Creates a <see cref="Rune"/> from the provided UTF-16 code unit.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="ch"/> represents a UTF-16 surrogate code point
        /// U+D800..U+DFFF, inclusive.
        /// </exception>
        public Rune(char ch)
        {
            uint expanded = ch;
            if (UnicodeUtility.IsSurrogateCodePoint(expanded))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.ch);
            }
            _value = expanded;
        }

        /// <summary>
        /// Creates a <see cref="Rune"/> from the provided Unicode scalar value.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="value"/> does not represent a value Unicode scalar value.
        /// </exception>
        public Rune(int value)
            : this((uint)value)
        {
        }

        /// <summary>
        /// Creates a <see cref="Rune"/> from the provided Unicode scalar value.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="value"/> does not represent a value Unicode scalar value.
        /// </exception>
        [CLSCompliant(false)]
        public Rune(uint value)
        {
            if (!UnicodeUtility.IsValidUnicodeScalar(value))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
            }
            _value = value;
        }

        // non-validating ctor
        private Rune(uint scalarValue, bool unused)
        {
            UnicodeDebug.AssertIsValidScalar(scalarValue);
            _value = scalarValue;
        }

        public static bool operator ==(Rune left, Rune right) => (left._value == right._value);

        public static bool operator !=(Rune left, Rune right) => (left._value != right._value);

        public static bool operator <(Rune left, Rune right) => (left._value < right._value);

        public static bool operator <=(Rune left, Rune right) => (left._value <= right._value);

        public static bool operator >(Rune left, Rune right) => (left._value > right._value);

        public static bool operator >=(Rune left, Rune right) => (left._value >= right._value);

        // Operators below are explicit because they may throw.

        public static explicit operator Rune(char ch) => new Rune(ch);

        [CLSCompliant(false)]
        public static explicit operator Rune(uint value) => new Rune(value);

        public static explicit operator Rune(int value) => new Rune(value);

        // Displayed as "'<char>' (U+XXXX)"; e.g., "'e' (U+0065)"
        private string DebuggerDisplay => FormattableString.Invariant($"U+{_value:X4} '{(IsValid(_value) ? ToString() : "\uFFFD")}'");

        /// <summary>
        /// Returns true if and only if this scalar value is ASCII ([ U+0000..U+007F ])
        /// and therefore representable by a single UTF-8 code unit.
        /// </summary>
        public bool IsAscii => UnicodeUtility.IsAsciiCodePoint(_value);

        /// <summary>
        /// Returns true if and only if this scalar value is within the BMP ([ U+0000..U+FFFF ])
        /// and therefore representable by a single UTF-16 code unit.
        /// </summary>
        public bool IsBmp => UnicodeUtility.IsBmpCodePoint(_value);

        /// <summary>
        /// Returns the Unicode plane (0 to 16, inclusive) which contains this scalar.
        /// </summary>
        public int Plane => UnicodeUtility.GetPlane(_value);

        /// <summary>
        /// A <see cref="Rune"/> instance that represents the Unicode replacement character U+FFFD.
        /// </summary>
        public static Rune ReplacementChar => UnsafeCreate(UnicodeUtility.ReplacementChar);

        /// <summary>
        /// Returns the length in code units (<see cref="Char"/>) of the
        /// UTF-16 sequence required to represent this scalar value.
        /// </summary>
        /// <remarks>
        /// The return value will be 1 or 2.
        /// </remarks>
        public int Utf16SequenceLength => UnicodeUtility.GetUtf16SequenceLength(_value);

        /// <summary>
        /// Returns the length in code units of the
        /// UTF-8 sequence required to represent this scalar value.
        /// </summary>
        /// <remarks>
        /// The return value will be 1 through 4, inclusive.
        /// </remarks>
        public int Utf8SequenceLength => UnicodeUtility.GetUtf8SequenceLength(_value);

        /// <summary>
        /// Returns the Unicode scalar value as an integer.
        /// </summary>
        public int Value => (int)_value;

        private static Rune ChangeCaseCultureAware(Rune rune, TextInfo textInfo, bool toUpper)
        {
            Debug.Assert(!GlobalizationMode.Invariant, "This should've been checked by the caller.");
            Debug.Assert(textInfo != null, "This should've been checked by the caller.");

            Span<char> original = stackalloc char[2]; // worst case scenario = 2 code units (for a surrogate pair)
            Span<char> modified = stackalloc char[2]; // case change should preserve UTF-16 code unit count

            int charCount = rune.EncodeToUtf16(original);
            original = original.Slice(0, charCount);
            modified = modified.Slice(0, charCount);

            if (toUpper)
            {
                textInfo.ChangeCaseToUpper(original, modified);
            }
            else
            {
                textInfo.ChangeCaseToLower(original, modified);
            }

            // We use simple case folding rules, which disallows moving between the BMP and supplementary
            // planes when performing a case conversion. The helper methods which reconstruct a Rune
            // contain debug asserts for this condition.

            if (rune.IsBmp)
            {
                return UnsafeCreate(modified[0]);
            }
            else
            {
                return UnsafeCreate(UnicodeUtility.GetScalarFromUtf16SurrogatePair(modified[0], modified[1]));
            }
        }

        public int CompareTo(Rune other) => this._value.CompareTo(other._value);

        // returns the number of chars written
        private int EncodeToUtf16(Span<char> destination)
        {
            Debug.Assert(destination.Length >= Utf16SequenceLength, "Caller should've provided a large enough buffer.");
            bool success = TryEncode(destination, out int charsWritten);
            Debug.Assert(success, "TryEncode should never fail given a large enough buffer.");
            return charsWritten;
        }

        public override bool Equals(object obj) => (obj is Rune other) && this.Equals(other);

        public bool Equals(Rune other) => (this == other);

        public override int GetHashCode() => Value;

        /// <summary>
        /// Gets the <see cref="Rune"/> which begins at index <paramref name="index"/> in
        /// string <paramref name="input"/>.
        /// </summary>
        /// <remarks>
        /// Throws if <paramref name="input"/> is null, if <paramref name="index"/> is out of range, or
        /// if <paramref name="index"/> does not reference the start of a valid scalar value within <paramref name="input"/>.
        /// </remarks>
        public static Rune GetRuneAt(string input, int index)
        {
            int runeValue = ReadRuneFromString(input, index);
            if (runeValue < 0)
            {
                ThrowHelper.ThrowArgumentException_CannotExtractScalar(ExceptionArgument.index);
            }

            return UnsafeCreate((uint)runeValue);
        }

        /// <summary>
        /// Returns <see langword="true"/> iff <paramref name="value"/> is a valid Unicode scalar
        /// value, i.e., is in [ U+0000..U+D7FF ], inclusive; or [ U+E000..U+10FFFF ], inclusive.
        /// </summary>
        public static bool IsValid(int value) => IsValid((uint)value);

        /// <summary>
        /// Returns <see langword="true"/> iff <paramref name="value"/> is a valid Unicode scalar
        /// value, i.e., is in [ U+0000..U+D7FF ], inclusive; or [ U+E000..U+10FFFF ], inclusive.
        /// </summary>
        [CLSCompliant(false)]
        public static bool IsValid(uint value) => UnicodeUtility.IsValidUnicodeScalar(value);

        // returns a negative number on failure
        internal static int ReadFirstRuneFromUtf16Buffer(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty)
            {
                return -1;
            }

            // Optimistically assume input is within BMP.

            uint returnValue = input[0];
            if (UnicodeUtility.IsSurrogateCodePoint(returnValue))
            {
                if (!UnicodeUtility.IsHighSurrogateCodePoint(returnValue))
                {
                    return -1;
                }

                // Treat 'returnValue' as the high surrogate.

                if (1 >= (uint)input.Length)
                {
                    return -1; // not an argument exception - just a "bad data" failure
                }

                uint potentialLowSurrogate = input[1];
                if (!UnicodeUtility.IsLowSurrogateCodePoint(potentialLowSurrogate))
                {
                    return -1;
                }

                returnValue = UnicodeUtility.GetScalarFromUtf16SurrogatePair(returnValue, potentialLowSurrogate);
            }

            return (int)returnValue;
        }

        // returns a negative number on failure
        private static int ReadRuneFromString(string input, int index)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            if ((uint)index >= (uint)input.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();
            }

            // Optimistically assume input is within BMP.

            uint returnValue = input[index];
            if (UnicodeUtility.IsSurrogateCodePoint(returnValue))
            {
                if (!UnicodeUtility.IsHighSurrogateCodePoint(returnValue))
                {
                    return -1;
                }

                // Treat 'returnValue' as the high surrogate.
                //
                // If this becomes a hot code path, we can skip the below bounds check by reading
                // off the end of the string using unsafe code. Since strings are null-terminated,
                // we're guaranteed not to read a valid low surrogate, so we'll fail correctly if
                // the string terminates unexpectedly.

                index++;
                if ((uint)index >= (uint)input.Length)
                {
                    return -1; // not an argument exception - just a "bad data" failure
                }

                uint potentialLowSurrogate = input[index];
                if (!UnicodeUtility.IsLowSurrogateCodePoint(potentialLowSurrogate))
                {
                    return -1;
                }

                returnValue = UnicodeUtility.GetScalarFromUtf16SurrogatePair(returnValue, potentialLowSurrogate);
            }

            return (int)returnValue;
        }

        /// <summary>
        /// Returns a <see cref="string"/> representation of this <see cref="Rune"/> instance.
        /// </summary>
        public override string ToString()
        {
            if (IsBmp)
            {
                return string.CreateFromChar((char)_value);
            }
            else
            {
                UnicodeUtility.GetUtf16SurrogatesFromSupplementaryPlaneScalar(_value, out char high, out char low);
                return string.CreateFromChar(high, low);
            }
        }

        /// <summary>
        /// Attempts to create a <see cref="Rune"/> from the provided input value.
        /// </summary>
        public static bool TryCreate(char ch, out Rune result)
        {
            uint extendedValue = ch;
            if (!UnicodeUtility.IsSurrogateCodePoint(extendedValue))
            {
                result = UnsafeCreate(extendedValue);
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Attempts to create a <see cref="Rune"/> from the provided input value.
        /// </summary>
        public static bool TryCreate(int value, out Rune result) => TryCreate((uint)value, out result);

        /// <summary>
        /// Attempts to create a <see cref="Rune"/> from the provided input value.
        /// </summary>
        [CLSCompliant(false)]
        public static bool TryCreate(uint value, out Rune result)
        {
            if (UnicodeUtility.IsValidUnicodeScalar(value))
            {
                result = UnsafeCreate(value);
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Encodes this <see cref="Rune"/> to a UTF-16 destination buffer.
        /// </summary>
        /// <param name="destination">The buffer to which to write this value as UTF-16.</param>
        /// <param name="charsWritten">
        /// The number of <see cref="char"/>s written to <paramref name="destination"/>,
        /// or 0 if the destination buffer is not large enough to contain the output.</param>
        /// <returns>True if the value was written to the buffer; otherwise, false.</returns>
        /// <remarks>
        /// The <see cref="Utf16SequenceLength"/> property can be queried ahead of time to determine
        /// the required size of the <paramref name="destination"/> buffer.
        /// </remarks>
        public bool TryEncode(Span<char> destination, out int charsWritten)
        {
            if (destination.Length >= 1)
            {
                if (IsBmp)
                {
                    destination[0] = (char)_value;
                    charsWritten = 1;
                    return true;
                }
                else if (destination.Length >= 2)
                {
                    UnicodeUtility.GetUtf16SurrogatesFromSupplementaryPlaneScalar(_value, out destination[0], out destination[1]);
                    charsWritten = 2;
                    return true;
                }
            }

            // Destination buffer not large enough

            charsWritten = default;
            return false;
        }

        /// <summary>
        /// Encodes this <see cref="Rune"/> to a destination buffer as UTF-8 bytes.
        /// </summary>
        /// <param name="destination">The buffer to which to write this value as UTF-8.</param>
        /// <param name="bytesWritten">
        /// The number of <see cref="byte"/>s written to <paramref name="destination"/>,
        /// or 0 if the destination buffer is not large enough to contain the output.</param>
        /// <returns>True if the value was written to the buffer; otherwise, false.</returns>
        /// <remarks>
        /// The <see cref="Utf8SequenceLength"/> property can be queried ahead of time to determine
        /// the required size of the <paramref name="destination"/> buffer.
        /// </remarks>
        // ** This is public so it can be unit tested but isn't yet exposed via the reference assemblies. **
        public bool TryEncodeToUtf8Bytes(Span<byte> destination, out int bytesWritten)
        {
            // TODO: Optimize some of these writes by using BMI2 instructions.

            // The bit patterns below come from the Unicode Standard, Table 3-6.

            if (destination.Length >= 1)
            {
                if (IsAscii)
                {
                    destination[0] = (byte)_value;
                    bytesWritten = 1;
                    return true;
                }

                if (destination.Length >= 2)
                {
                    if (_value <= 0x7FFu)
                    {
                        // Scalar 00000yyy yyxxxxxx -> bytes [ 110yyyyy 10xxxxxx ]
                        destination[0] = (byte)((_value + (0b110u << 11)) >> 6);
                        destination[1] = (byte)((_value & 0x3Fu) + 0x80u);
                        bytesWritten = 2;
                        return true;
                    }

                    if (destination.Length >= 3)
                    {
                        if (_value <= 0xFFFFu)
                        {
                            // Scalar zzzzyyyy yyxxxxxx -> bytes [ 1110zzzz 10yyyyyy 10xxxxxx ]
                            destination[0] = (byte)((_value + (0b1110 << 16)) >> 12);
                            destination[1] = (byte)(((_value & (0x3Fu << 6)) >> 6) + 0x80u);
                            destination[2] = (byte)((_value & 0x3Fu) + 0x80u);
                            bytesWritten = 3;
                            return true;
                        }

                        if (destination.Length >= 4)
                        {
                            // Scalar 000uuuuu zzzzyyyy yyxxxxxx -> bytes [ 11110uuu 10uuzzzz 10yyyyyy 10xxxxxx ]
                            destination[0] = (byte)((_value + (0b11110 << 21)) >> 18);
                            destination[1] = (byte)(((_value & (0x3Fu << 12)) >> 12) + 0x80u);
                            destination[2] = (byte)(((_value & (0x3Fu << 6)) >> 6) + 0x80u);
                            destination[3] = (byte)((_value & 0x3Fu) + 0x80u);
                            bytesWritten = 4;
                            return true;
                        }
                    }
                }
            }

            // Destination buffer not large enough

            bytesWritten = default;
            return false;
        }

        /// <summary>
        /// Attempts to get the <see cref="Rune"/> which begins at index <paramref name="index"/> in
        /// string <paramref name="input"/>.
        /// </summary>
        /// <returns><see langword="true"/> if a scalar value was successfully extracted from the specified index,
        /// <see langword="false"/> if a value could not be extracted due to invalid data.</returns>
        /// <remarks>
        /// Throws only if <paramref name="input"/> is null or <paramref name="index"/> is out of range.
        /// </remarks>
        public static bool TryGetRuneAt(string input, int index, out Rune value)
        {
            int runeValue = ReadRuneFromString(input, index);
            if (runeValue >= 0)
            {
                value = UnsafeCreate((uint)runeValue);
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        // Allows constructing a Unicode scalar value from an arbitrary 32-bit integer without
        // validation. It is the caller's responsibility to have performed manual validation
        // before calling this method. If a Rune instance is forcibly constructed
        // from invalid input, the APIs on this type have undefined behavior, potentially including
        // introducing a security hole in the consuming application.
        //
        // An example of a security hole resulting from an invalid Rune value, which could result
        // in a stack overflow.
        //
        // public int GetMarvin32HashCode(Rune r) {
        //   Span<char> buffer = stackalloc char[r.Utf16SequenceLength];
        //   r.TryEncode(buffer, ...);
        //   return Marvin32.ComputeHash(buffer.AsBytes());
        // }

        /// <summary>
        /// Creates a <see cref="Rune"/> without performing validation on the input.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Rune UnsafeCreate(uint scalarValue) => new Rune(scalarValue, false);

        // These are analogs of APIs on System.Char

        public static double GetNumericValue(Rune value)
        {
            if (value.IsAscii)
            {
                uint baseNum = value._value - '0';
                return (baseNum <= 9) ? (double)baseNum : -1;
            }
            else
            {
                // not an ASCII char; fall back to globalization table
                return CharUnicodeInfo.InternalGetNumericValue(value.Value);
            }
        }

        public static UnicodeCategory GetUnicodeCategory(Rune value)
        {
            if (value.IsAscii)
            {
                return (UnicodeCategory)(AsciiCharInfo[value.Value] & UnicodeCategoryMask);
            }
            else
            {
                return GetUnicodeCategoryNonAscii(value);
            }
        }

        private static UnicodeCategory GetUnicodeCategoryNonAscii(Rune value)
        {
            Debug.Assert(!value.IsAscii, "Shouldn't use this non-optimized code path for ASCII characters.");
            return CharUnicodeInfo.GetUnicodeCategory(value.Value);
        }

        // Returns true iff this Unicode category represents a letter
        private static bool IsCategoryLetter(UnicodeCategory category)
        {
            return UnicodeUtility.IsInRangeInclusive((uint)category, (uint)UnicodeCategory.UppercaseLetter, (uint)UnicodeCategory.OtherLetter);
        }

        // Returns true iff this Unicode category represents a letter or a decimal digit
        private static bool IsCategoryLetterOrDecimalDigit(UnicodeCategory category)
        {
            return UnicodeUtility.IsInRangeInclusive((uint)category, (uint)UnicodeCategory.UppercaseLetter, (uint)UnicodeCategory.OtherLetter)
                || (category == UnicodeCategory.DecimalDigitNumber);
        }

        // Returns true iff this Unicode category represents a number
        private static bool IsCategoryNumber(UnicodeCategory category)
        {
            return UnicodeUtility.IsInRangeInclusive((uint)category, (uint)UnicodeCategory.DecimalDigitNumber, (uint)UnicodeCategory.OtherNumber);
        }

        // Returns true iff this Unicode category represents a punctuation mark
        private static bool IsCategoryPunctuation(UnicodeCategory category)
        {
            return UnicodeUtility.IsInRangeInclusive((uint)category, (uint)UnicodeCategory.ConnectorPunctuation, (uint)UnicodeCategory.OtherPunctuation);
        }

        // Returns true iff this Unicode category represents a separator
        private static bool IsCategorySeparator(UnicodeCategory category)
        {
            return UnicodeUtility.IsInRangeInclusive((uint)category, (uint)UnicodeCategory.SpaceSeparator, (uint)UnicodeCategory.ParagraphSeparator);
        }

        // Returns true iff this Unicode category represents a symbol
        private static bool IsCategorySymbol(UnicodeCategory category)
        {
            return UnicodeUtility.IsInRangeInclusive((uint)category, (uint)UnicodeCategory.MathSymbol, (uint)UnicodeCategory.OtherSymbol);
        }

        public static bool IsControl(Rune value)
        {
            // Per the Unicode stability policy, the set of control characters
            // is forever fixed at [ U+0000..U+001F ], [ U+007F..U+009F ]. No
            // characters will ever be added to the "control characters" group.
            // See http://www.unicode.org/policies/stability_policy.html.

            // Logic below depends on Rune.Value never being -1 (since Rune is a validating type)
            // 00..1F (+1) => 01..20 (&~80) => 01..20
            // 7F..9F (+1) => 80..A0 (&~80) => 00..20

            return (((value._value + 1) & ~0x80u) <= 0x20u);
        }

        public static bool IsDigit(Rune value)
        {
            if (value.IsAscii)
            {
                return UnicodeUtility.IsInRangeInclusive(value._value, '0', '9');
            }
            else
            {
                return (GetUnicodeCategoryNonAscii(value) == UnicodeCategory.DecimalDigitNumber);
            }
        }

        public static bool IsLetter(Rune value)
        {
            if (value.IsAscii)
            {
                return (((value._value - 'A') & ~0x20u) <= (uint)('Z' - 'A')); // [A-Za-z]
            }
            else
            {
                return IsCategoryLetter(GetUnicodeCategoryNonAscii(value));
            }
        }

        public static bool IsLetterOrDigit(Rune value)
        {
            if (value.IsAscii)
            {
                return ((AsciiCharInfo[value.Value] & IsLetterOrDigitFlag) != 0);
            }
            else
            {
                return IsCategoryLetterOrDecimalDigit(GetUnicodeCategoryNonAscii(value));
            }
        }

        public static bool IsLower(Rune value)
        {
            if (value.IsAscii)
            {
                return UnicodeUtility.IsInRangeInclusive(value._value, 'a', 'z');
            }
            else
            {
                return (GetUnicodeCategoryNonAscii(value) == UnicodeCategory.LowercaseLetter);
            }
        }

        public static bool IsNumber(Rune value)
        {
            if (value.IsAscii)
            {
                return UnicodeUtility.IsInRangeInclusive(value._value, '0', '9');
            }
            else
            {
                return IsCategoryNumber(GetUnicodeCategoryNonAscii(value));
            }
        }

        public static bool IsPunctuation(Rune value)
        {
            return IsCategoryPunctuation(GetUnicodeCategory(value));
        }

        public static bool IsSeparator(Rune value)
        {
            return IsCategorySeparator(GetUnicodeCategory(value));
        }

        public static bool IsSymbol(Rune value)
        {
            return IsCategorySymbol(GetUnicodeCategory(value));
        }

        public static bool IsUpper(Rune value)
        {
            if (value.IsAscii)
            {
                return UnicodeUtility.IsInRangeInclusive(value._value, 'A', 'Z');
            }
            else
            {
                return (GetUnicodeCategoryNonAscii(value) == UnicodeCategory.UppercaseLetter);
            }
        }

        public static bool IsWhiteSpace(Rune value)
        {
            if (value.IsAscii)
            {
                return (AsciiCharInfo[value.Value] & IsWhiteSpaceFlag) != 0;
            }

            // U+0085 is special since it's a whitespace character but is in the Control category
            // instead of a normal separator category. No other code point outside the ASCII range
            // has this mismatch.

            if (value._value == 0x0085u)
            {
                return true;
            }

            return IsCategorySeparator(GetUnicodeCategoryNonAscii(value));
        }

        public static Rune ToLower(Rune value, CultureInfo culture)
        {
            if (culture is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);
            }

            // We don't want to special-case ASCII here since the specified culture might handle
            // ASCII characters differently than the invariant culture (e.g., Turkish I). Instead
            // we'll just jump straight to the globalization tables if they're available.

            if (GlobalizationMode.Invariant)
            {
                return ToLowerInvariant(value);
            }

            return ChangeCaseCultureAware(value, culture.TextInfo, toUpper: false);
        }

        public static Rune ToLowerInvariant(Rune value)
        {
            // Handle the most common case (ASCII data) first. Within the common case, we expect
            // that there'll be a mix of lowercase & uppercase chars, so make the conversion branchless.

            if (value.IsAscii)
            {
                // It's ok for us to use the UTF-16 conversion utility for this since the high
                // 16 bits of the value will never be set so will be left unchanged.
                return UnsafeCreate(Utf16Utility.ConvertAllAsciiCharsInUInt32ToLowercase(value._value));
            }

            if (GlobalizationMode.Invariant)
            {
                // If the value isn't ASCII and if the globalization tables aren't available,
                // case changing has no effect.
                return value;
            }

            // Non-ASCII data requires going through the case folding tables.

            return ChangeCaseCultureAware(value, TextInfo.Invariant, toUpper: false);
        }

        public static Rune ToUpper(Rune value, CultureInfo culture)
        {
            if (culture is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);
            }

            // We don't want to special-case ASCII here since the specified culture might handle
            // ASCII characters differently than the invariant culture (e.g., Turkish I). Instead
            // we'll just jump straight to the globalization tables if they're available.

            if (GlobalizationMode.Invariant)
            {
                return ToUpperInvariant(value);
            }

            return ChangeCaseCultureAware(value, culture.TextInfo, toUpper: true);
        }

        public static Rune ToUpperInvariant(Rune value)
        {
            // Handle the most common case (ASCII data) first. Within the common case, we expect
            // that there'll be a mix of lowercase & uppercase chars, so make the conversion branchless.

            if (value.IsAscii)
            {
                // It's ok for us to use the UTF-16 conversion utility for this since the high
                // 16 bits of the value will never be set so will be left unchanged.
                return UnsafeCreate(Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase(value._value));
            }

            if (GlobalizationMode.Invariant)
            {
                // If the value isn't ASCII and if the globalization tables aren't available,
                // case changing has no effect.
                return value;
            }

            // Non-ASCII data requires going through the case folding tables.

            return ChangeCaseCultureAware(value, TextInfo.Invariant, toUpper: true);
        }
    }
}

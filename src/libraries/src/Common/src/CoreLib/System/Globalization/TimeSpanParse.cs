// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////
//
//  Purpose:  Used by TimeSpan to parse a time interval string.
//
//  Standard Format:
//  -=-=-=-=-=-=-=-
//  "c":  Constant format.  [-][d'.']hh':'mm':'ss['.'fffffff]  
//  Not culture sensitive.  Default format (and null/empty format string) map to this format.
//
//  "g":  General format, short:  [-][d':']h':'mm':'ss'.'FFFFFFF  
//  Only print what's needed.  Localized (if you want Invariant, pass in Invariant).
//  The fractional seconds separator is localized, equal to the culture's DecimalSeparator.
//
//  "G":  General format, long:  [-]d':'hh':'mm':'ss'.'fffffff
//  Always print days and 7 fractional digits.  Localized (if you want Invariant, pass in Invariant).
//  The fractional seconds separator is localized, equal to the culture's DecimalSeparator.
//
//  * "TryParseTimeSpan" is the main method for Parse/TryParse
//
//  - TimeSpanTokenizer.GetNextToken() is used to split the input string into number and literal tokens.
//  - TimeSpanRawInfo.ProcessToken() adds the next token into the parsing intermediary state structure
//  - ProcessTerminalState() uses the fully initialized TimeSpanRawInfo to find a legal parse match.
//    The terminal states are attempted as follows:
//    foreach (+InvariantPattern, -InvariantPattern, +LocalizedPattern, -LocalizedPattern) try
//       1 number  => d
//       2 numbers => h:m
//       3 numbers => h:m:s     | d.h:m   | h:m:.f
//       4 numbers => h:m:s.f   | d.h:m:s | d.h:m:.f
//       5 numbers => d.h:m:s.f
//
// Custom Format:
// -=-=-=-=-=-=-=
//
// * "TryParseExactTimeSpan" is the main method for ParseExact/TryParseExact methods
// * "TryParseExactMultipleTimeSpan" is the main method for ParseExact/TryparseExact
//    methods that take a string[] of formats
//
// - For single-letter formats "TryParseTimeSpan" is called (see above)
// - For multi-letter formats "TryParseByFormat" is called
// - TryParseByFormat uses helper methods (ParseExactLiteral, ParseExactDigits, etc)
//   which drive the underlying TimeSpanTokenizer.  However, unlike standard formatting which
//   operates on whole-tokens, ParseExact operates at the character-level.  As such, 
//   TimeSpanTokenizer.NextChar and TimeSpanTokenizer.BackOne() are called directly. 
//
////////////////////////////////////////////////////////////////////////////

using System.Diagnostics;
using System.Text;

namespace System.Globalization
{
    internal static class TimeSpanParse
    {
        private const int MaxFractionDigits = 7;
        private const int MaxDays = 10675199;
        private const int MaxHours = 23;
        private const int MaxMinutes = 59;
        private const int MaxSeconds = 59;
        private const int MaxFraction = 9999999;

        private enum ParseFailureKind : byte
        {
            None = 0,
            ArgumentNull = 1,
            Format = 2,
            FormatWithParameter = 3,
            Overflow = 4,
        }

        [Flags]
        private enum TimeSpanStandardStyles : byte
        {
            // Standard Format Styles
            None = 0x00000000,
            Invariant = 0x00000001, //Allow Invariant Culture
            Localized = 0x00000002, //Allow Localized Culture
            RequireFull = 0x00000004, //Require the input to be in DHMSF format
            Any = Invariant | Localized,
        }

        // TimeSpan Token Types
        private enum TTT : byte
        {
            None = 0,         // None of the TimeSpanToken fields are set
            End = 1,          // '\0'
            Num = 2,          // Number
            Sep = 3,          // literal
            NumOverflow = 4,  // Number that overflowed
        }

        private ref struct TimeSpanToken
        {
            internal TTT _ttt;
            internal int _num;                // Store the number that we are parsing (if any)
            internal int _zeroes;             // Store the number of leading zeroes (if any)
            internal ReadOnlySpan<char> _sep; // Store the literal that we are parsing (if any)

            public TimeSpanToken(TTT type) : this(type, 0, 0, default(ReadOnlySpan<char>)) { }

            public TimeSpanToken(int number) : this(TTT.Num, number, 0, default(ReadOnlySpan<char>)) { }

            public TimeSpanToken(int number, int leadingZeroes) : this(TTT.Num, number, leadingZeroes, default(ReadOnlySpan<char>)) { }

            public TimeSpanToken(TTT type, int number, int leadingZeroes, ReadOnlySpan<char> separator)
            {
                _ttt = type;
                _num = number;
                _zeroes = leadingZeroes;
                _sep = separator;
            }

            public bool IsInvalidFraction()
            {
                Debug.Assert(_ttt == TTT.Num);
                Debug.Assert(_num > -1);

                if (_num > MaxFraction || _zeroes > MaxFractionDigits)
                    return true;

                if (_num == 0 || _zeroes == 0)
                    return false;

                // num > 0 && zeroes > 0 && num <= maxValue && zeroes <= maxPrecision
                return _num >= MaxFraction / Pow10(_zeroes - 1);
            }
        }

        private ref struct TimeSpanTokenizer
        {
            private ReadOnlySpan<char> _value;
            private int _pos;

            internal TimeSpanTokenizer(ReadOnlySpan<char> input) : this(input, 0) { }

            internal TimeSpanTokenizer(ReadOnlySpan<char> input, int startPosition)
            {
                _value = input;
                _pos = startPosition;
            }

            /// <summary>Returns the next token in the input string</summary>
            /// <remarks>Used by the parsing routines that operate on standard-formats.</remarks>
            internal TimeSpanToken GetNextToken()
            {
                // Get the position of the next character to be processed.  If there is no
                // next character, we're at the end.
                int pos = _pos;
                Debug.Assert(pos > -1);
                if (pos >= _value.Length)
                {
                    return new TimeSpanToken(TTT.End);
                }

                // Now retrieve that character. If it's a digit, we're processing a number.
                int num = _value[pos] - '0';
                if ((uint)num <= 9)
                {
                    int zeroes = 0;
                    if (num == 0)
                    {
                        // Read all leading zeroes.
                        zeroes = 1;
                        while (true)
                        {
                            int digit;
                            if (++_pos >= _value.Length || (uint)(digit = _value[_pos] - '0') > 9)
                            {
                                return new TimeSpanToken(TTT.Num, 0, zeroes, default(ReadOnlySpan<char>));
                            }

                            if (digit == 0)
                            {
                                zeroes++;
                                continue;
                            }

                            num = digit;
                            break;
                        }
                    }

                    // Continue to read as long as we're reading digits.
                    while (++_pos < _value.Length)
                    {
                        int digit = _value[_pos] - '0';
                        if ((uint)digit > 9)
                        {
                            break;
                        }

                        num = num * 10 + digit;
                        if ((num & 0xF0000000) != 0)
                        {
                            return new TimeSpanToken(TTT.NumOverflow);
                        }
                    }

                    return new TimeSpanToken(TTT.Num, num, zeroes, default(ReadOnlySpan<char>));
                }

                // Otherwise, we're processing a separator, and we've already processed the first
                // character of it.  Continue processing characters as long as they're not digits.
                int length = 1;
                while (true)
                {
                    if (++_pos >= _value.Length || (uint)(_value[_pos] - '0') <= 9)
                    {
                        break;
                    }
                    length++;
                }

                // Return the separator.
                return new TimeSpanToken(TTT.Sep, 0, 0, _value.Slice(pos, length));
            }

            internal bool EOL => _pos >= (_value.Length - 1);

            internal void BackOne()
            {
                if (_pos > 0) --_pos;
            }

            internal char NextChar
            {
                get
                {
                    int pos = ++_pos;
                    return (uint)pos < (uint)_value.Length ?
                        _value[pos] :
                        (char)0;
                }
            }
        }

        /// <summary>Stores intermediary parsing state for the standard formats.</summary>
        private ref struct TimeSpanRawInfo
        {
            internal TimeSpanFormat.FormatLiterals PositiveInvariant => TimeSpanFormat.PositiveInvariantFormatLiterals;
            internal TimeSpanFormat.FormatLiterals NegativeInvariant => TimeSpanFormat.NegativeInvariantFormatLiterals;

            internal TimeSpanFormat.FormatLiterals PositiveLocalized
            {
                get
                {
                    if (!_posLocInit)
                    {
                        _posLoc = new TimeSpanFormat.FormatLiterals();
                        _posLoc.Init(_fullPosPattern, false);
                        _posLocInit = true;
                    }
                    return _posLoc;
                }
            }

            internal TimeSpanFormat.FormatLiterals NegativeLocalized
            {
                get
                {
                    if (!_negLocInit)
                    {
                        _negLoc = new TimeSpanFormat.FormatLiterals();
                        _negLoc.Init(_fullNegPattern, false);
                        _negLocInit = true;
                    }
                    return _negLoc;
                }
            }

            internal bool FullAppCompatMatch(TimeSpanFormat.FormatLiterals pattern) =>
                _sepCount == 5
                && _numCount == 4
                && StringSpanHelpers.Equals(_literals0, pattern.Start)
                && StringSpanHelpers.Equals(_literals1, pattern.DayHourSep)
                && StringSpanHelpers.Equals(_literals2, pattern.HourMinuteSep)
                && StringSpanHelpers.Equals(_literals3, pattern.AppCompatLiteral)
                && StringSpanHelpers.Equals(_literals4, pattern.End);

            internal bool PartialAppCompatMatch(TimeSpanFormat.FormatLiterals pattern) =>
                _sepCount == 4
                && _numCount == 3
                && StringSpanHelpers.Equals(_literals0, pattern.Start)
                && StringSpanHelpers.Equals(_literals1, pattern.HourMinuteSep)
                && StringSpanHelpers.Equals(_literals2, pattern.AppCompatLiteral)
                && StringSpanHelpers.Equals(_literals3, pattern.End);

            /// <summary>DHMSF (all values matched)</summary>
            internal bool FullMatch(TimeSpanFormat.FormatLiterals pattern) =>
                _sepCount == MaxLiteralTokens
                && _numCount == MaxNumericTokens
                && StringSpanHelpers.Equals(_literals0, pattern.Start)
                && StringSpanHelpers.Equals(_literals1, pattern.DayHourSep)
                && StringSpanHelpers.Equals(_literals2, pattern.HourMinuteSep)
                && StringSpanHelpers.Equals(_literals3, pattern.MinuteSecondSep)
                && StringSpanHelpers.Equals(_literals4, pattern.SecondFractionSep)
                && StringSpanHelpers.Equals(_literals5, pattern.End);

            /// <summary>D (no hours, minutes, seconds, or fractions)</summary>
            internal bool FullDMatch(TimeSpanFormat.FormatLiterals pattern) =>
                _sepCount == 2
                && _numCount == 1
                && StringSpanHelpers.Equals(_literals0, pattern.Start)
                && StringSpanHelpers.Equals(_literals1, pattern.End);

            /// <summary>HM (no days, seconds, or fractions)</summary>
            internal bool FullHMMatch(TimeSpanFormat.FormatLiterals pattern) =>
                _sepCount == 3
                && _numCount == 2
                && StringSpanHelpers.Equals(_literals0, pattern.Start)
                && StringSpanHelpers.Equals(_literals1, pattern.HourMinuteSep)
                && StringSpanHelpers.Equals(_literals2, pattern.End);

            /// <summary>DHM (no seconds or fraction)</summary>
            internal bool FullDHMMatch(TimeSpanFormat.FormatLiterals pattern) =>
                _sepCount == 4
                && _numCount == 3
                && StringSpanHelpers.Equals(_literals0, pattern.Start)
                && StringSpanHelpers.Equals(_literals1, pattern.DayHourSep)
                && StringSpanHelpers.Equals(_literals2, pattern.HourMinuteSep)
                && StringSpanHelpers.Equals(_literals3, pattern.End);

            /// <summary>HMS (no days or fraction)</summary>
            internal bool FullHMSMatch(TimeSpanFormat.FormatLiterals pattern) =>
                _sepCount == 4
                && _numCount == 3
                && StringSpanHelpers.Equals(_literals0, pattern.Start)
                && StringSpanHelpers.Equals(_literals1, pattern.HourMinuteSep)
                && StringSpanHelpers.Equals(_literals2, pattern.MinuteSecondSep)
                && StringSpanHelpers.Equals(_literals3, pattern.End);

            /// <summary>DHMS (no fraction)</summary>
            internal bool FullDHMSMatch(TimeSpanFormat.FormatLiterals pattern) =>
                _sepCount == 5
                && _numCount == 4
                && StringSpanHelpers.Equals(_literals0, pattern.Start)
                && StringSpanHelpers.Equals(_literals1, pattern.DayHourSep)
                && StringSpanHelpers.Equals(_literals2, pattern.HourMinuteSep)
                && StringSpanHelpers.Equals(_literals3, pattern.MinuteSecondSep)
                && StringSpanHelpers.Equals(_literals4, pattern.End);

            /// <summary>HMSF (no days)</summary>
            internal bool FullHMSFMatch(TimeSpanFormat.FormatLiterals pattern) =>
                _sepCount == 5
                && _numCount == 4
                && StringSpanHelpers.Equals(_literals0, pattern.Start)
                && StringSpanHelpers.Equals(_literals1, pattern.HourMinuteSep)
                && StringSpanHelpers.Equals(_literals2, pattern.MinuteSecondSep)
                && StringSpanHelpers.Equals(_literals3, pattern.SecondFractionSep)
                && StringSpanHelpers.Equals(_literals4, pattern.End);

            internal TTT _lastSeenTTT;
            internal int _tokenCount;
            internal int _sepCount;
            internal int _numCount;

            private TimeSpanFormat.FormatLiterals _posLoc;
            private TimeSpanFormat.FormatLiterals _negLoc;
            private bool _posLocInit;
            private bool _negLocInit;
            private string _fullPosPattern;
            private string _fullNegPattern;

            private const int MaxTokens = 11;
            private const int MaxLiteralTokens = 6;
            private const int MaxNumericTokens = 5;

            internal TimeSpanToken _numbers0, _numbers1, _numbers2, _numbers3, _numbers4; // MaxNumbericTokens = 5
            internal ReadOnlySpan<char> _literals0, _literals1, _literals2, _literals3, _literals4, _literals5; // MaxLiteralTokens=6

            internal void Init(DateTimeFormatInfo dtfi)
            {
                Debug.Assert(dtfi != null);

                _lastSeenTTT = TTT.None;
                _tokenCount = 0;
                _sepCount = 0;
                _numCount = 0;

                _fullPosPattern = dtfi.FullTimeSpanPositivePattern;
                _fullNegPattern = dtfi.FullTimeSpanNegativePattern;
                _posLocInit = false;
                _negLocInit = false;
            }

            internal bool ProcessToken(ref TimeSpanToken tok, ref TimeSpanResult result)
            {
                switch (tok._ttt)
                {
                    case TTT.Num:
                        if ((_tokenCount == 0 && !AddSep(default(ReadOnlySpan<char>), ref result)) || !AddNum(tok, ref result))
                        {
                            return false;
                        }
                        break;

                    case TTT.Sep:
                        if (!AddSep(tok._sep, ref result))
                        {
                            return false;
                        }
                        break;

                    case TTT.NumOverflow:
                        return result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge));

                    default:
                        // Some unknown token or a repeat token type in the input
                        return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
                }

                _lastSeenTTT = tok._ttt;
                Debug.Assert(_tokenCount == (_sepCount + _numCount), "tokenCount == (SepCount + NumCount)");
                return true;
            }

            private bool AddSep(ReadOnlySpan<char> sep, ref TimeSpanResult result)
            {
                if (_sepCount >= MaxLiteralTokens || _tokenCount >= MaxTokens)
                {
                    return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
                }

                switch (_sepCount++)
                {
                    case 0: _literals0 = sep; break;
                    case 1: _literals1 = sep; break;
                    case 2: _literals2 = sep; break;
                    case 3: _literals3 = sep; break;
                    case 4: _literals4 = sep; break;
                    default: _literals5 = sep; break;
                }

                _tokenCount++;
                return true;
            }
            private bool AddNum(TimeSpanToken num, ref TimeSpanResult result)
            {
                if (_numCount >= MaxNumericTokens || _tokenCount >= MaxTokens)
                {
                    return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
                }

                switch (_numCount++)
                {
                    case 0: _numbers0 = num; break;
                    case 1: _numbers1 = num; break;
                    case 2: _numbers2 = num; break;
                    case 3: _numbers3 = num; break;
                    default: _numbers4 = num; break;
                }

                _tokenCount++;
                return true;
            }
        }

        /// <summary>Store the result of the parsing.</summary>
        private struct TimeSpanResult
        {
            internal TimeSpan parsedTimeSpan;
            private readonly bool _throwOnFailure;

            internal TimeSpanResult(bool throwOnFailure)
            {
                parsedTimeSpan = default(TimeSpan);
                _throwOnFailure = throwOnFailure;
            }

            internal bool SetFailure(ParseFailureKind kind, string resourceKey, object messageArgument = null, string argumentName = null)
            {
                if (!_throwOnFailure)
                {
                    return false;
                }

                string message = SR.GetResourceString(resourceKey);
                switch (kind)
                {
                    case ParseFailureKind.ArgumentNull:
                        Debug.Assert(argumentName != null);
                        throw new ArgumentNullException(argumentName, message);

                    case ParseFailureKind.FormatWithParameter:
                        throw new FormatException(SR.Format(message, messageArgument));

                    case ParseFailureKind.Overflow:
                        throw new OverflowException(message);

                    default:
                        Debug.Assert(kind == ParseFailureKind.Format, $"Unexpected failure {kind}");
                        throw new FormatException(message);
                }
            }
        }

        internal static long Pow10(int pow)
        {
            switch (pow)
            {
                case 0:  return 1;
                case 1:  return 10;
                case 2:  return 100;
                case 3:  return 1000;
                case 4:  return 10000;
                case 5:  return 100000;
                case 6:  return 1000000;
                case 7:  return 10000000;
                default: return (long)Math.Pow(10, pow);
            }
        }

        private static bool TryTimeToTicks(bool positive, TimeSpanToken days, TimeSpanToken hours, TimeSpanToken minutes, TimeSpanToken seconds, TimeSpanToken fraction, out long result)
        {
            if (days._num > MaxDays ||
                hours._num > MaxHours ||
                minutes._num > MaxMinutes ||
                seconds._num > MaxSeconds ||
                fraction.IsInvalidFraction())
            {
                result = 0;
                return false;
            }

            long ticks = ((long)days._num * 3600 * 24 + (long)hours._num * 3600 + (long)minutes._num * 60 + seconds._num) * 1000;
            if (ticks > InternalGlobalizationHelper.MaxMilliSeconds || ticks < InternalGlobalizationHelper.MinMilliSeconds)
            {
                result = 0;
                return false;
            }

            // Normalize the fraction component
            //
            // string representation => (zeroes,num) => resultant fraction ticks
            // ---------------------    ------------    ------------------------
            // ".9999999"            => (0,9999999)  => 9,999,999 ticks (same as constant maxFraction)
            // ".1"                  => (0,1)        => 1,000,000 ticks
            // ".01"                 => (1,1)        =>   100,000 ticks
            // ".001"                => (2,1)        =>    10,000 ticks
            long f = fraction._num;
            if (f != 0)
            {
                long lowerLimit = InternalGlobalizationHelper.TicksPerTenthSecond;
                if (fraction._zeroes > 0)
                {
                    long divisor = Pow10(fraction._zeroes);
                    lowerLimit = lowerLimit / divisor;
                }

                while (f < lowerLimit)
                {
                    f *= 10;
                }
            }

            result = ticks * TimeSpan.TicksPerMillisecond + f;
            if (positive && result < 0)
            {
                result = 0;
                return false;
            }

            return true;
        }

        internal static TimeSpan Parse(ReadOnlySpan<char> input, IFormatProvider formatProvider)
        {
            var parseResult = new TimeSpanResult(throwOnFailure: true);
            bool success = TryParseTimeSpan(input, TimeSpanStandardStyles.Any, formatProvider, ref parseResult);
            Debug.Assert(success, "Should have thrown on failure");
            return parseResult.parsedTimeSpan;
        }

        internal static bool TryParse(ReadOnlySpan<char> input, IFormatProvider formatProvider, out TimeSpan result)
        {
            var parseResult = new TimeSpanResult(throwOnFailure: false);

            if (TryParseTimeSpan(input, TimeSpanStandardStyles.Any, formatProvider, ref parseResult))
            {
                result = parseResult.parsedTimeSpan;
                return true;
            }

            result = default(TimeSpan);
            return false;
        }

        internal static TimeSpan ParseExact(ReadOnlySpan<char> input, ReadOnlySpan<char> format, IFormatProvider formatProvider, TimeSpanStyles styles)
        {
            var parseResult = new TimeSpanResult(throwOnFailure: true);
            bool success = TryParseExactTimeSpan(input, format, formatProvider, styles, ref parseResult);
            Debug.Assert(success, "Should have thrown on failure");
            return parseResult.parsedTimeSpan;
        }

        internal static bool TryParseExact(ReadOnlySpan<char> input, ReadOnlySpan<char> format, IFormatProvider formatProvider, TimeSpanStyles styles, out TimeSpan result)
        {
            var parseResult = new TimeSpanResult(throwOnFailure: false);

            if (TryParseExactTimeSpan(input, format, formatProvider, styles, ref parseResult))
            {
                result = parseResult.parsedTimeSpan;
                return true;
            }

            result = default(TimeSpan);
            return false;
        }

        internal static TimeSpan ParseExactMultiple(ReadOnlySpan<char> input, string[] formats, IFormatProvider formatProvider, TimeSpanStyles styles)
        {
            var parseResult = new TimeSpanResult(throwOnFailure: true);
            bool success = TryParseExactMultipleTimeSpan(input, formats, formatProvider, styles, ref parseResult);
            Debug.Assert(success, "Should have thrown on failure");
            return parseResult.parsedTimeSpan;
        }

        internal static bool TryParseExactMultiple(ReadOnlySpan<char> input, string[] formats, IFormatProvider formatProvider, TimeSpanStyles styles, out TimeSpan result)
        {
            var parseResult = new TimeSpanResult(throwOnFailure: false);

            if (TryParseExactMultipleTimeSpan(input, formats, formatProvider, styles, ref parseResult))
            {
                result = parseResult.parsedTimeSpan;
                return true;
            }

            result = default(TimeSpan);
            return false;
        }

        /// <summary>Common private Parse method called by both Parse and TryParse.</summary>
        private static bool TryParseTimeSpan(ReadOnlySpan<char> input, TimeSpanStandardStyles style, IFormatProvider formatProvider, ref TimeSpanResult result)
        {
            input = input.Trim();
            if (input.IsEmpty)
            {
                return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
            }

            var tokenizer = new TimeSpanTokenizer(input);

            var raw = new TimeSpanRawInfo();
            raw.Init(DateTimeFormatInfo.GetInstance(formatProvider));

            TimeSpanToken tok = tokenizer.GetNextToken();

            // The following loop will break out when we reach the end of the str or
            // when we can determine that the input is invalid.
            while (tok._ttt != TTT.End)
            {
                if (!raw.ProcessToken(ref tok, ref result))
                {
                    return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
                }
                tok = tokenizer.GetNextToken();
            }
            Debug.Assert(tokenizer.EOL);

            if (!ProcessTerminalState(ref raw, style, ref result))
            {
                return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
            }

            return true;
        }

        /// <summary>
        /// Validate the terminal state of a standard format parse.
        /// Sets result.parsedTimeSpan on success.
        /// Calculates the resultant TimeSpan from the TimeSpanRawInfo.
        /// </summary>
        /// <remarks>
        /// try => +InvariantPattern, -InvariantPattern, +LocalizedPattern, -LocalizedPattern
        /// 1) Verify Start matches
        /// 2) Verify End matches
        /// 3) 1 number  => d
        ///    2 numbers => h:m
        ///    3 numbers => h:m:s | d.h:m | h:m:.f
        ///    4 numbers => h:m:s.f | d.h:m:s | d.h:m:.f
        ///    5 numbers => d.h:m:s.f
        /// </remarks>
        private static bool ProcessTerminalState(ref TimeSpanRawInfo raw, TimeSpanStandardStyles style, ref TimeSpanResult result)
        {
            if (raw._lastSeenTTT == TTT.Num)
            {
                TimeSpanToken tok = new TimeSpanToken();
                tok._ttt = TTT.Sep;
                if (!raw.ProcessToken(ref tok, ref result))
                {
                    return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
                }
            }

            switch (raw._numCount)
            {
                case 1: return ProcessTerminal_D(ref raw, style, ref result);
                case 2: return ProcessTerminal_HM(ref raw, style, ref result);
                case 3: return ProcessTerminal_HM_S_D(ref raw, style, ref result);
                case 4: return ProcessTerminal_HMS_F_D(ref raw, style, ref result);
                case 5: return ProcessTerminal_DHMSF(ref raw, style, ref result);
                default: return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
            }
        }

        /// <summary>Validate the 5-number "Days.Hours:Minutes:Seconds.Fraction" terminal case.</summary>
        private static bool ProcessTerminal_DHMSF(ref TimeSpanRawInfo raw, TimeSpanStandardStyles style, ref TimeSpanResult result)
        {
            if (raw._sepCount != 6)
            {
                return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
            }
            Debug.Assert(raw._numCount == 5);

            bool inv = (style & TimeSpanStandardStyles.Invariant) != 0;
            bool loc = (style & TimeSpanStandardStyles.Localized) != 0;
            bool positive = false;
            bool match = false;

            if (inv)
            {
                if (raw.FullMatch(raw.PositiveInvariant))
                {
                    match = true;
                    positive = true;
                }
                if (!match && raw.FullMatch(raw.NegativeInvariant))
                {
                    match = true;
                    positive = false;
                }
            }

            if (loc)
            {
                if (!match && raw.FullMatch(raw.PositiveLocalized))
                {
                    match = true;
                    positive = true;
                }
                if (!match && raw.FullMatch(raw.NegativeLocalized))
                {
                    match = true;
                    positive = false;
                }
            }

            if (match)
            {
                long ticks;

                if (!TryTimeToTicks(positive, raw._numbers0, raw._numbers1, raw._numbers2, raw._numbers3, raw._numbers4, out ticks))
                {
                    return result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge));
                }

                if (!positive)
                {
                    ticks = -ticks;
                    if (ticks > 0)
                    {
                        return result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge));
                    }
                }

                result.parsedTimeSpan._ticks = ticks;
                return true;
            }

            return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
        }


        /// <summary>
        /// Validate the ambiguous 4-number "Hours:Minutes:Seconds.Fraction", "Days.Hours:Minutes:Seconds",
        /// or "Days.Hours:Minutes:.Fraction" terminal case.
        /// </summary>
        private static bool ProcessTerminal_HMS_F_D(ref TimeSpanRawInfo raw, TimeSpanStandardStyles style, ref TimeSpanResult result)
        {
            if (raw._sepCount != 5 || (style & TimeSpanStandardStyles.RequireFull) != 0)
            {
                return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
            }
            Debug.Assert(raw._numCount == 4);

            bool inv = ((style & TimeSpanStandardStyles.Invariant) != 0);
            bool loc = ((style & TimeSpanStandardStyles.Localized) != 0);

            long ticks = 0;
            bool positive = false, match = false, overflow = false;
            var zero = new TimeSpanToken(0);

            if (inv)
            {
                if (raw.FullHMSFMatch(raw.PositiveInvariant))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, zero, raw._numbers0, raw._numbers1, raw._numbers2, raw._numbers3, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullDHMSMatch(raw.PositiveInvariant))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, raw._numbers0, raw._numbers1, raw._numbers2, raw._numbers3, zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullAppCompatMatch(raw.PositiveInvariant))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, raw._numbers0, raw._numbers1, raw._numbers2, zero, raw._numbers3, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullHMSFMatch(raw.NegativeInvariant))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, zero, raw._numbers0, raw._numbers1, raw._numbers2, raw._numbers3, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullDHMSMatch(raw.NegativeInvariant))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, raw._numbers0, raw._numbers1, raw._numbers2, raw._numbers3, zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullAppCompatMatch(raw.NegativeInvariant))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, raw._numbers0, raw._numbers1, raw._numbers2, zero, raw._numbers3, out ticks);
                    overflow = overflow || !match;
                }
            }

            if (loc)
            {
                if (!match && raw.FullHMSFMatch(raw.PositiveLocalized))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, zero, raw._numbers0, raw._numbers1, raw._numbers2, raw._numbers3, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullDHMSMatch(raw.PositiveLocalized))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, raw._numbers0, raw._numbers1, raw._numbers2, raw._numbers3, zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullAppCompatMatch(raw.PositiveLocalized))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, raw._numbers0, raw._numbers1, raw._numbers2, zero, raw._numbers3, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullHMSFMatch(raw.NegativeLocalized))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, zero, raw._numbers0, raw._numbers1, raw._numbers2, raw._numbers3, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullDHMSMatch(raw.NegativeLocalized))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, raw._numbers0, raw._numbers1, raw._numbers2, raw._numbers3, zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullAppCompatMatch(raw.NegativeLocalized))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, raw._numbers0, raw._numbers1, raw._numbers2, zero, raw._numbers3, out ticks);
                    overflow = overflow || !match;
                }
            }

            if (match)
            {
                if (!positive)
                {
                    ticks = -ticks;
                    if (ticks > 0)
                    {
                        return result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge));
                    }
                }

                result.parsedTimeSpan._ticks = ticks;
                return true;
            }

            return overflow ?
                result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge)) : // we found at least one literal pattern match but the numbers just didn't fit
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan)); // we couldn't find a thing
        }

        /// <summary>Validate the ambiguous 3-number "Hours:Minutes:Seconds", "Days.Hours:Minutes", or "Hours:Minutes:.Fraction" terminal case.</summary>
        private static bool ProcessTerminal_HM_S_D(ref TimeSpanRawInfo raw, TimeSpanStandardStyles style, ref TimeSpanResult result)
        {
            if (raw._sepCount != 4 || (style & TimeSpanStandardStyles.RequireFull) != 0)
            {
                return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
            }
            Debug.Assert(raw._numCount == 3);

            bool inv = ((style & TimeSpanStandardStyles.Invariant) != 0);
            bool loc = ((style & TimeSpanStandardStyles.Localized) != 0);

            bool positive = false, match = false, overflow = false;
            var zero = new TimeSpanToken(0);
            long ticks = 0;

            if (inv)
            {
                if (raw.FullHMSMatch(raw.PositiveInvariant))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, zero, raw._numbers0, raw._numbers1, raw._numbers2, zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullDHMMatch(raw.PositiveInvariant))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, raw._numbers0, raw._numbers1, raw._numbers2, zero, zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.PartialAppCompatMatch(raw.PositiveInvariant))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, zero, raw._numbers0, raw._numbers1, zero, raw._numbers2, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullHMSMatch(raw.NegativeInvariant))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, zero, raw._numbers0, raw._numbers1, raw._numbers2, zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullDHMMatch(raw.NegativeInvariant))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, raw._numbers0, raw._numbers1, raw._numbers2, zero, zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.PartialAppCompatMatch(raw.NegativeInvariant))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, zero, raw._numbers0, raw._numbers1, zero, raw._numbers2, out ticks);
                    overflow = overflow || !match;
                }
            }

            if (loc)
            {
                if (!match && raw.FullHMSMatch(raw.PositiveLocalized))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, zero, raw._numbers0, raw._numbers1, raw._numbers2, zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullDHMMatch(raw.PositiveLocalized))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, raw._numbers0, raw._numbers1, raw._numbers2, zero, zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.PartialAppCompatMatch(raw.PositiveLocalized))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, zero, raw._numbers0, raw._numbers1, zero, raw._numbers2, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullHMSMatch(raw.NegativeLocalized))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, zero, raw._numbers0, raw._numbers1, raw._numbers2, zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullDHMMatch(raw.NegativeLocalized))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, raw._numbers0, raw._numbers1, raw._numbers2, zero, zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.PartialAppCompatMatch(raw.NegativeLocalized))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, zero, raw._numbers0, raw._numbers1, zero, raw._numbers2, out ticks);
                    overflow = overflow || !match;
                }
            }

            if (match)
            {
                if (!positive)
                {
                    ticks = -ticks;
                    if (ticks > 0)
                    {
                        return result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge));
                    }
                }

                result.parsedTimeSpan._ticks = ticks;
                return true;
            }

            return overflow ?
                result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge)) : // we found at least one literal pattern match but the numbers just didn't fit
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan)); // we couldn't find a thing
        }

        /// <summary>Validate the 2-number "Hours:Minutes" terminal case.</summary>
        private static bool ProcessTerminal_HM(ref TimeSpanRawInfo raw, TimeSpanStandardStyles style, ref TimeSpanResult result)
        {
            if (raw._sepCount != 3 || (style & TimeSpanStandardStyles.RequireFull) != 0)
            {
                return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
            }
            Debug.Assert(raw._numCount == 2);

            bool inv = ((style & TimeSpanStandardStyles.Invariant) != 0);
            bool loc = ((style & TimeSpanStandardStyles.Localized) != 0);

            bool positive = false, match = false;

            if (inv)
            {
                if (raw.FullHMMatch(raw.PositiveInvariant))
                {
                    match = true;
                    positive = true;
                }

                if (!match && raw.FullHMMatch(raw.NegativeInvariant))
                {
                    match = true;
                    positive = false;
                }
            }

            if (loc)
            {
                if (!match && raw.FullHMMatch(raw.PositiveLocalized))
                {
                    match = true;
                    positive = true;
                }

                if (!match && raw.FullHMMatch(raw.NegativeLocalized))
                {
                    match = true;
                    positive = false;
                }
            }

            if (match)
            {
                long ticks = 0;
                var zero = new TimeSpanToken(0);

                if (!TryTimeToTicks(positive, zero, raw._numbers0, raw._numbers1, zero, zero, out ticks))
                {
                    return result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge));
                }

                if (!positive)
                {
                    ticks = -ticks;
                    if (ticks > 0)
                    {
                        return result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge));
                    }
                }

                result.parsedTimeSpan._ticks = ticks;
                return true;
            }

            return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
        }

        /// <summary>Validate the 1-number "Days" terminal case.</summary>
        private static bool ProcessTerminal_D(ref TimeSpanRawInfo raw, TimeSpanStandardStyles style, ref TimeSpanResult result)
        {
            if (raw._sepCount != 2 || (style & TimeSpanStandardStyles.RequireFull) != 0)
            {
                return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
            }
            Debug.Assert(raw._numCount == 1);

            bool inv = ((style & TimeSpanStandardStyles.Invariant) != 0);
            bool loc = ((style & TimeSpanStandardStyles.Localized) != 0);

            bool positive = false, match = false;

            if (inv)
            {
                if (raw.FullDMatch(raw.PositiveInvariant))
                {
                    match = true;
                    positive = true;
                }

                if (!match && raw.FullDMatch(raw.NegativeInvariant))
                {
                    match = true;
                    positive = false;
                }
            }

            if (loc)
            {
                if (!match && raw.FullDMatch(raw.PositiveLocalized))
                {
                    match = true;
                    positive = true;
                }

                if (!match && raw.FullDMatch(raw.NegativeLocalized))
                {
                    match = true;
                    positive = false;
                }
            }

            if (match)
            {
                long ticks = 0;
                var zero = new TimeSpanToken(0);

                if (!TryTimeToTicks(positive, raw._numbers0, zero, zero, zero, zero, out ticks))
                {
                    return result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge));
                }

                if (!positive)
                {
                    ticks = -ticks;
                    if (ticks > 0)
                    {
                        return result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge));
                    }
                }

                result.parsedTimeSpan._ticks = ticks;
                return true;
            }

            return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
        }

        /// <summary>Common private ParseExact method called by both ParseExact and TryParseExact.</summary>
        private static bool TryParseExactTimeSpan(ReadOnlySpan<char> input, ReadOnlySpan<char> format, IFormatProvider formatProvider, TimeSpanStyles styles, ref TimeSpanResult result)
        {
            if (format.Length == 0)
            {
                return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadFormatSpecifier));
            }

            if (format.Length == 1)
            {
                switch (format[0])
                {
                    case 'c':
                    case 't':
                    case 'T':
                        return TryParseTimeSpanConstant(input, ref result); // fast path for legacy style TimeSpan formats.

                    case 'g':
                        return TryParseTimeSpan(input, TimeSpanStandardStyles.Localized, formatProvider, ref result);

                    case 'G':
                        return TryParseTimeSpan(input, TimeSpanStandardStyles.Localized | TimeSpanStandardStyles.RequireFull, formatProvider, ref result);

                    default:
                        return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadFormatSpecifier));
                }
            }

            return TryParseByFormat(input, format, styles, ref result);
        }

        /// <summary>Parse the TimeSpan instance using the specified format.  Used by TryParseExactTimeSpan.</summary>
        private static bool TryParseByFormat(ReadOnlySpan<char> input, ReadOnlySpan<char> format, TimeSpanStyles styles, ref TimeSpanResult result)
        {
            bool seenDD = false;      // already processed days?
            bool seenHH = false;      // already processed hours?
            bool seenMM = false;      // already processed minutes?
            bool seenSS = false;      // already processed seconds?
            bool seenFF = false;      // already processed fraction?

            int dd = 0;               // parsed days
            int hh = 0;               // parsed hours
            int mm = 0;               // parsed minutes
            int ss = 0;               // parsed seconds
            int leadingZeroes = 0;    // number of leading zeroes in the parsed fraction
            int ff = 0;               // parsed fraction
            int i = 0;                // format string position
            int tokenLen = 0;         // length of current format token, used to update index 'i'

            var tokenizer = new TimeSpanTokenizer(input, -1);

            while (i < format.Length)
            {
                char ch = format[i];
                int nextFormatChar;
                switch (ch)
                {
                    case 'h':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2 || seenHH || !ParseExactDigits(ref tokenizer, tokenLen, out hh))
                        {
                            return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_InvalidString));
                        }
                        seenHH = true;
                        break;

                    case 'm':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2 || seenMM || !ParseExactDigits(ref tokenizer, tokenLen, out mm))
                        {
                            return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_InvalidString));
                        }
                        seenMM = true;
                        break;

                    case 's':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2 || seenSS || !ParseExactDigits(ref tokenizer, tokenLen, out ss))
                        {
                            return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_InvalidString));
                        }
                        seenSS = true;
                        break;

                    case 'f':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > DateTimeFormat.MaxSecondsFractionDigits || seenFF || !ParseExactDigits(ref tokenizer, tokenLen, tokenLen, out leadingZeroes, out ff))
                        {
                            return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_InvalidString));
                        }
                        seenFF = true;
                        break;

                    case 'F':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > DateTimeFormat.MaxSecondsFractionDigits || seenFF)
                        {
                            return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_InvalidString));
                        }
                        ParseExactDigits(ref tokenizer, tokenLen, tokenLen, out leadingZeroes, out ff);
                        seenFF = true;
                        break;

                    case 'd':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        int tmp = 0;
                        if (tokenLen > 8 || seenDD || !ParseExactDigits(ref tokenizer, (tokenLen < 2) ? 1 : tokenLen, (tokenLen < 2) ? 8 : tokenLen, out tmp, out dd))
                        {
                            return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_InvalidString));
                        }
                        seenDD = true;
                        break;

                    case '\'':
                    case '\"':
                        StringBuilder enquotedString = StringBuilderCache.Acquire();
                        if (!DateTimeParse.TryParseQuoteString(format, i, enquotedString, out tokenLen))
                        {
                            StringBuilderCache.Release(enquotedString);
                            return result.SetFailure(ParseFailureKind.FormatWithParameter, nameof(SR.Format_BadQuote), ch);
                        }
                        if (!ParseExactLiteral(ref tokenizer, enquotedString))
                        {
                            StringBuilderCache.Release(enquotedString);
                            return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_InvalidString));
                        }
                        StringBuilderCache.Release(enquotedString);
                        break;

                    case '%':
                        // Optional format character.
                        // For example, format string "%d" will print day 
                        // Most of the cases, "%" can be ignored.
                        nextFormatChar = DateTimeFormat.ParseNextChar(format, i);

                        // nextFormatChar will be -1 if we already reach the end of the format string.
                        // Besides, we will not allow "%%" appear in the pattern.
                        if (nextFormatChar >= 0 && nextFormatChar != '%')
                        {
                            tokenLen = 1; // skip the '%' and process the format character
                            break;
                        }
                        else
                        {
                            // This means that '%' is at the end of the format string or
                            // "%%" appears in the format string.
                            return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_InvalidString));
                        }

                    case '\\':
                        // Escaped character.  Can be used to insert character into the format string.
                        // For example, "\d" will insert the character 'd' into the string.
                        //
                        nextFormatChar = DateTimeFormat.ParseNextChar(format, i);
                        if (nextFormatChar >= 0 && tokenizer.NextChar == (char)nextFormatChar)
                        {
                            tokenLen = 2;
                        }
                        else
                        {
                            // This means that '\' is at the end of the format string or the literal match failed.
                            return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_InvalidString));
                        }
                        break;

                    default:
                        return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_InvalidString));
                }

                i += tokenLen;
            }


            if (!tokenizer.EOL)
            {
                // the custom format didn't consume the entire input
                return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
            }

            bool positive = (styles & TimeSpanStyles.AssumeNegative) == 0;
            if (TryTimeToTicks(positive, new TimeSpanToken(dd),
                                         new TimeSpanToken(hh),
                                         new TimeSpanToken(mm),
                                         new TimeSpanToken(ss),
                                         new TimeSpanToken(ff, leadingZeroes),
                                         out long ticks))
            {
                if (!positive)
                {
                    ticks = -ticks;
                }

                result.parsedTimeSpan._ticks = ticks;
                return true;
            }
            else
            {
                return result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge));
            }
        }

        private static bool ParseExactDigits(ref TimeSpanTokenizer tokenizer, int minDigitLength, out int result)
        {
            result = 0;
            int zeroes = 0;
            int maxDigitLength = (minDigitLength == 1) ? 2 : minDigitLength;
            return ParseExactDigits(ref tokenizer, minDigitLength, maxDigitLength, out zeroes, out result);
        }

        private static bool ParseExactDigits(ref TimeSpanTokenizer tokenizer, int minDigitLength, int maxDigitLength, out int zeroes, out int result)
        {
            int tmpResult = 0, tmpZeroes = 0;

            int tokenLength = 0;
            while (tokenLength < maxDigitLength)
            {
                char ch = tokenizer.NextChar;
                if (ch < '0' || ch > '9')
                {
                    tokenizer.BackOne();
                    break;
                }

                tmpResult = tmpResult * 10 + (ch - '0');
                if (tmpResult == 0) tmpZeroes++;
                tokenLength++;
            }

            zeroes = tmpZeroes;
            result = tmpResult;
            return tokenLength >= minDigitLength;
        }

        private static bool ParseExactLiteral(ref TimeSpanTokenizer tokenizer, StringBuilder enquotedString)
        {
            for (int i = 0; i < enquotedString.Length; i++)
            {
                if (enquotedString[i] != tokenizer.NextChar)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Parses the "c" (constant) format.  This code is 100% identical to the non-globalized v1.0-v3.5 TimeSpan.Parse() routine
        /// and exists for performance/appcompat with legacy callers who cannot move onto the globalized Parse overloads. 
        /// </summary>
        private static bool TryParseTimeSpanConstant(ReadOnlySpan<char> input, ref TimeSpanResult result) =>
            new StringParser().TryParse(input, ref result);

        private ref struct StringParser
        {
            private ReadOnlySpan<char> _str;
            private char _ch;
            private int _pos;
            private int _len;

            internal void NextChar()
            {
                if (_pos < _len)
                {
                    _pos++;
                }

                _ch = _pos < _len ?
                    _str[_pos] :
                    (char)0;
            }

            internal char NextNonDigit()
            {
                int i = _pos;
                while (i < _len)
                {
                    char ch = _str[i];
                    if (ch < '0' || ch > '9') return ch;
                    i++;
                }

                return (char)0;
            }

            internal bool TryParse(ReadOnlySpan<char> input, ref TimeSpanResult result)
            {
                result.parsedTimeSpan._ticks = 0;

                _str = input;
                _len = input.Length;
                _pos = -1;
                NextChar();
                SkipBlanks();

                bool negative = false;
                if (_ch == '-')
                {
                    negative = true;
                    NextChar();
                }

                long time;
                if (NextNonDigit() == ':')
                {
                    if (!ParseTime(out time, ref result))
                    {
                        return false;
                    };
                }
                else
                {
                    int days;
                    if (!ParseInt((int)(0x7FFFFFFFFFFFFFFFL / TimeSpan.TicksPerDay), out days, ref result))
                    {
                        return false;
                    }

                    time = days * TimeSpan.TicksPerDay;

                    if (_ch == '.')
                    {
                        NextChar();
                        long remainingTime;
                        if (!ParseTime(out remainingTime, ref result))
                        {
                            return false;
                        };
                        time += remainingTime;
                    }
                }

                if (negative)
                {
                    time = -time;
                    // Allow -0 as well
                    if (time > 0)
                    {
                        return result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge));
                    }
                }
                else
                {
                    if (time < 0)
                    {
                        return result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge));
                    }
                }

                SkipBlanks();

                if (_pos < _len)
                {
                    return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
                }

                result.parsedTimeSpan._ticks = time;
                return true;
            }

            internal bool ParseInt(int max, out int i, ref TimeSpanResult result)
            {
                i = 0;
                int p = _pos;
                while (_ch >= '0' && _ch <= '9')
                {
                    if ((i & 0xF0000000) != 0)
                    {
                        return result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge));
                    }

                    i = i * 10 + _ch - '0';
                    if (i < 0)
                    {
                        return result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge));
                    }

                    NextChar();
                }

                if (p == _pos)
                {
                    return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
                }

                if (i > max)
                {
                    return result.SetFailure(ParseFailureKind.Overflow, nameof(SR.Overflow_TimeSpanElementTooLarge));
                }

                return true;
            }

            internal bool ParseTime(out long time, ref TimeSpanResult result)
            {
                time = 0;
                int unit;

                if (!ParseInt(23, out unit, ref result))
                {
                    return false;
                }

                time = unit * TimeSpan.TicksPerHour;
                if (_ch != ':')
                {
                    return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
                }

                NextChar();

                if (!ParseInt(59, out unit, ref result))
                {
                    return false;
                }

                time += unit * TimeSpan.TicksPerMinute;

                if (_ch == ':')
                {
                    NextChar();
                    
                    // allow seconds with the leading zero
                    if (_ch != '.')
                    {
                        if (!ParseInt(59, out unit, ref result))
                        {
                            return false;
                        }
                        time += unit * TimeSpan.TicksPerSecond;
                    }

                    if (_ch == '.')
                    {
                        NextChar();
                        int f = (int)TimeSpan.TicksPerSecond;
                        while (f > 1 && _ch >= '0' && _ch <= '9')
                        {
                            f /= 10;
                            time += (_ch - '0') * f;
                            NextChar();
                        }
                    }
                }

                return true;
            }

            internal void SkipBlanks()
            {
                while (_ch == ' ' || _ch == '\t') NextChar();
            }
        }

        /// <summary>Common private ParseExactMultiple method called by both ParseExactMultiple and TryParseExactMultiple.</summary>
        private static bool TryParseExactMultipleTimeSpan(ReadOnlySpan<char> input, string[] formats, IFormatProvider formatProvider, TimeSpanStyles styles, ref TimeSpanResult result)
        {
            if (formats == null)
            {
                return result.SetFailure(ParseFailureKind.ArgumentNull, nameof(SR.ArgumentNull_String), argumentName: nameof(formats));
            }

            if (input.Length == 0)
            {
                return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
            }

            if (formats.Length == 0)
            {
                return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadFormatSpecifier));
            }

            // Do a loop through the provided formats and see if we can parse succesfully in
            // one of the formats.
            for (int i = 0; i < formats.Length; i++)
            {
                if (formats[i] == null || formats[i].Length == 0)
                {
                    return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadFormatSpecifier));
                }

                // Create a new non-throwing result each time to ensure the runs are independent.
                TimeSpanResult innerResult = new TimeSpanResult(throwOnFailure: false);

                if (TryParseExactTimeSpan(input, formats[i], formatProvider, styles, ref innerResult))
                {
                    result.parsedTimeSpan = innerResult.parsedTimeSpan;
                    return true;
                }
            }

            return result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_BadTimeSpan));
        }
    }
}

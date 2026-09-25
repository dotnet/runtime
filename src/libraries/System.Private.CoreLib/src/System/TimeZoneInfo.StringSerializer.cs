// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        /// <summary>
        /// Used to serialize and deserialize TimeZoneInfo objects based on the custom string serialization format.
        /// </summary>
        private struct StringSerializer
        {
            private enum State
            {
                Escaped = 0,
                NotEscaped = 1,
                StartOfToken = 2,
                EndOfLine = 3
            }

            private readonly string _serializedText;
            private int _currentTokenStartIndex;
            private State _state;

            // the majority of the strings contained in the OS time zones fit in 64 chars
            private const int InitialCapacityForString = 64;
            private const char Esc = '\\';
            private const char Sep = ';';
            private const char Lhs = '[';
            private const char Rhs = ']';
            private const string DateTimeFormat = "MM:dd:yyyy";
            private const string TimeOfDayFormat = "HH:mm:ss.FFF";

            // Marks the start of the optional full-fidelity adjustment rule data that is appended
            // after the separator terminating the adjustment rule list. Older readers stop at that
            // separator and never see this data, so it is backward compatible.
            private const char FullFidelityRulesMarker = '!';

            // Version of the full-fidelity trailer layout. It is written right after the marker so a
            // future revision of the layout can be detected. A reader that does not recognize the
            // version ignores the trailer and falls back to the legacy rules instead of misparsing
            // newer data.
            private const int FullFidelityRulesVersion = 1;

            // Minimum number of separator characters a single full-fidelity rule occupies: one after each
            // of its seven numeric fields plus one after each of its two transitions (both encoded as the
            // one-character "D" form). A rule can only be longer than this, so this is used to bound the
            // rule count against the remaining input length before allocating.
            private const int MinSeparatorsPerFullFidelityRule = 9;

            /// <summary>
            /// Creates the custom serialized string representation of a TimeZoneInfo instance.
            /// </summary>
            public static unsafe string GetSerializedString(TimeZoneInfo zone)
            {
                var serializedText = new ValueStringBuilder(stackalloc char[InitialCapacityForString]);

                //
                // <_id>;<_baseUtcOffset>;<_displayName>;<_standardDisplayName>;<_daylightDispayName>
                //
                SerializeSubstitute(zone.Id, ref serializedText);
                serializedText.Append(Sep);
                serializedText.AppendSpanFormattable(zone.BaseUtcOffset.TotalMinutes, format: default, CultureInfo.InvariantCulture);
                serializedText.Append(Sep);
                SerializeSubstitute(zone.DisplayName, ref serializedText);
                serializedText.Append(Sep);
                SerializeSubstitute(zone.StandardName, ref serializedText);
                serializedText.Append(Sep);
                SerializeSubstitute(zone.DaylightName, ref serializedText);
                serializedText.Append(Sep);

                AdjustmentRule[] rules = zone.GetAdjustmentRules();
                DateTime? previousLegacyEndDate = null;
                foreach (AdjustmentRule rule in rules)
                {
                    // Compute the whole-day, strictly ordered boundaries written to the legacy portion.
                    // The exact boundaries are preserved in the full-fidelity trailer below.
                    GetLegacyRuleDates(rule, ref previousLegacyEndDate, out DateTime legacyDateStart, out DateTime legacyDateEnd);

                    serializedText.Append(Lhs);
                    serializedText.AppendSpanFormattable(legacyDateStart, DateTimeFormat, DateTimeFormatInfo.InvariantInfo);
                    serializedText.Append(Sep);
                    serializedText.AppendSpanFormattable(legacyDateEnd, DateTimeFormat, DateTimeFormatInfo.InvariantInfo);
                    serializedText.Append(Sep);
                    serializedText.AppendSpanFormattable(rule.DaylightDelta.TotalMinutes, format: default, CultureInfo.InvariantCulture);
                    serializedText.Append(Sep);
                    // Serialize the TransitionTime's. The legacy format cannot represent an empty
                    // transition (used by rules that carry no daylight transition) and requires the two
                    // transitions to differ when NoDaylightTransitions is not set, so substitute distinct
                    // parseable placeholders. The exact values are preserved in the full-fidelity trailer.
                    GetLegacyTransitionTimes(rule, out TransitionTime legacyStart, out TransitionTime legacyEnd);
                    SerializeTransitionTime(legacyStart, ref serializedText);
                    serializedText.Append(Sep);
                    SerializeTransitionTime(legacyEnd, ref serializedText);
                    serializedText.Append(Sep);
                    if (rule.BaseUtcOffsetDelta != TimeSpan.Zero || rule.NoDaylightTransitions)
                    {
                        // Serialize it only when BaseUtcOffsetDelta has a value to reduce the impact of adding rule.BaseUtcOffsetDelta.
                        // The legacy format stores this offset in whole minutes and its reader rejects a fractional value, so write a
                        // whole-minute value here. Some Unix rules carry a sub-minute BaseUtcOffsetDelta; its exact value is preserved
                        // in the full-fidelity trailer below, and this whole-minute value keeps the legacy portion parseable for readers
                        // that ignore the trailer.
                        // It is also written (as 0 when absent) whenever the NoDaylightTransitions marker below is emitted, because the
                        // legacy reader distinguishes these two optional fields positionally: it consumes a leading digit as the
                        // BaseUtcOffsetDelta, so without a preceding offset token the '1' marker would be misread as a one-minute offset.
                        serializedText.AppendSpanFormattable(rule.BaseUtcOffsetDelta.Ticks / TimeSpan.TicksPerMinute, format: default, CultureInfo.InvariantCulture);
                        serializedText.Append(Sep);
                    }
                    if (rule.NoDaylightTransitions)
                    {
                        // Emit the NoDaylightTransitions marker so a reader that ignores the full-fidelity trailer (for example an
                        // older runtime) reconstructs a Linux-style rule and treats DateStart/DateEnd as the UTC window. Without it,
                        // such a reader would parse the rule as a Windows-style seasonal rule and interpret the placeholder transitions
                        // as local-time transitions, changing the calculated offsets. The exact rule is still preserved in the trailer.
                        serializedText.Append('1');
                        serializedText.Append(Sep);
                    }
                    serializedText.Append(Rhs);
                }
                serializedText.Append(Sep);

                // The public rules serialized above are a Windows-shaped projection of the internal
                // rules. On Unix that projection is lossy (for example NoDaylightTransitions rules,
                // UTC sub-day boundaries, or multi-year rules that get split). When it cannot reproduce
                // the internal rules exactly, append a full-fidelity copy of the internal rules so the
                // round trip is exact. This is placed after the separator that terminates the rule list,
                // which older readers ignore, so existing serialized strings and Windows output are
                // unchanged.
                AdjustmentRule[]? internalRules = zone._adjustmentRules;
                if (internalRules is not null && RequiresFullFidelityRules(rules, internalRules))
                {
                    SerializeFullFidelityRules(internalRules, ref serializedText);
                }

                return serializedText.ToString();
            }

            /// <summary>
            /// Instantiates a TimeZoneInfo from a custom serialized string.
            /// </summary>
            public static TimeZoneInfo GetDeserializedTimeZoneInfo(string source)
            {
                StringSerializer s = new StringSerializer(source);

                string id = s.GetNextStringValue();
                TimeSpan baseUtcOffset = s.GetNextTimeSpanValue();
                string displayName = s.GetNextStringValue();
                string standardName = s.GetNextStringValue();
                string daylightName = s.GetNextStringValue();
                AdjustmentRule[]? rules = s.GetNextAdjustmentRuleArrayValue();

                // If a full-fidelity copy of the internal rules was appended, use it instead of the
                // legacy (public projection) rules so the round trip is exact.
                rules = s.GetFullFidelityAdjustmentRulesIfPresent(rules);

                try
                {
                    return new TimeZoneInfo(id, baseUtcOffset, displayName, standardName, daylightName, rules, disableDaylightSavingTime: false);
                }
                catch (ArgumentException ex)
                {
                    throw new SerializationException(SR.Serialization_InvalidData, ex);
                }
                catch (InvalidTimeZoneException ex)
                {
                    throw new SerializationException(SR.Serialization_InvalidData, ex);
                }
            }

            private StringSerializer(string str)
            {
                _serializedText = str;
                _currentTokenStartIndex = 0;
                _state = State.StartOfToken;
            }

            /// <summary>
            /// Produces the transitions to write for the legacy portion of a rule. Empty transitions
            /// (Month == 0), used by rules that carry no daylight transition, cannot be represented by
            /// the legacy format because it reconstructs transitions through the TransitionTime factory
            /// methods (which reject a zero month). They are replaced by placeholders. The placeholders
            /// are also forced to differ from each other so the rule passes validation on readers that
            /// do not honor the full-fidelity trailer. The exact values are preserved in that trailer.
            /// </summary>
            private static void GetLegacyTransitionTimes(AdjustmentRule rule, out TransitionTime start, out TransitionTime end)
            {
                start = rule.DaylightTransitionStart.Month != 0 ? rule.DaylightTransitionStart : s_legacyTransitionPlaceholderStart;
                end = rule.DaylightTransitionEnd.Month != 0 ? rule.DaylightTransitionEnd : s_legacyTransitionPlaceholderEnd;

                if (start.Equals(end))
                {
                    // The two transitions must differ (see TimeZoneInfo.AdjustmentRule validation). Pick a
                    // month that is guaranteed to be different from the one already used.
                    end = TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1), start.Month == 12 ? 6 : 12, 1);
                }
            }

            private static readonly TransitionTime s_legacyTransitionPlaceholderStart = TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1), 1, 1);
            private static readonly TransitionTime s_legacyTransitionPlaceholderEnd = TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1), 12, 31);

            /// <summary>
            /// Produces the whole-day, strictly ordered boundaries to write for the legacy portion of a
            /// rule. The legacy format stores only the date component (see <see cref="DateTimeFormat"/>),
            /// so internal rules whose boundaries are sub-day UTC instants can collapse onto the same
            /// calendar day and make consecutive rules overlap. Readers that do not honor the
            /// full-fidelity trailer validate the legacy rules for chronological order, so nudge the start
            /// past the previous rule's end when they would collide. For Windows-shaped rules (already
            /// whole-day and non-overlapping) this is a no-op, keeping the output byte-identical. The exact
            /// boundaries are preserved in the full-fidelity trailer.
            /// </summary>
            private static void GetLegacyRuleDates(AdjustmentRule rule, ref DateTime? previousEndDate, out DateTime startDate, out DateTime endDate)
            {
                startDate = rule.DateStart.Date;
                endDate = rule.DateEnd.Date;

                if (previousEndDate is DateTime previous && startDate <= previous && previous < DateTime.MaxValue.Date)
                {
                    startDate = previous.AddDays(1);
                    if (startDate > endDate)
                    {
                        endDate = startDate;
                    }
                }

                previousEndDate = endDate;
            }

            /// <summary>
            /// Determines whether the internal adjustment rules can be reproduced exactly from the
            /// legacy (public projection) serialization. When they cannot, a full-fidelity copy of the
            /// internal rules is appended to the serialized string.
            /// </summary>
            private static bool RequiresFullFidelityRules(AdjustmentRule[] publicRules, AdjustmentRule[] internalRules)
            {
                if (publicRules.Length != internalRules.Length)
                {
                    return true;
                }

                for (int i = 0; i < internalRules.Length; i++)
                {
                    AdjustmentRule internalRule = internalRules[i];

                    // The legacy format only represents Windows-shaped rules: Unspecified-kind, whole-day
                    // boundaries, whole-minute offsets, real daylight transitions, and no
                    // NoDaylightTransitions marker. Anything else (produced on Unix) loses information when
                    // projected through GetAdjustmentRules().
                    if (internalRule.NoDaylightTransitions ||
                        internalRule.DateStart.Kind == DateTimeKind.Utc ||
                        internalRule.DateEnd.Kind == DateTimeKind.Utc ||
                        internalRule.BaseUtcOffsetDelta.Ticks % TimeSpan.TicksPerMinute != 0 ||
                        !publicRules[i].Equals(internalRule))
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Appends a full-fidelity copy of the internal adjustment rules. It is written after the
            /// separator that terminates the legacy rule list, which older readers ignore. The layout is
            /// the marker, the format version, the rule count, then each rule.
            /// </summary>
            private static void SerializeFullFidelityRules(AdjustmentRule[] rules, ref ValueStringBuilder serializedText)
            {
                serializedText.Append(FullFidelityRulesMarker);
                AppendInt32Value(FullFidelityRulesVersion, ref serializedText);
                AppendInt32Value(rules.Length, ref serializedText);

                foreach (AdjustmentRule rule in rules)
                {
                    AppendInt64Value(rule.DateStart.Ticks, ref serializedText);
                    AppendInt32Value((int)rule.DateStart.Kind, ref serializedText);
                    AppendInt64Value(rule.DateEnd.Ticks, ref serializedText);
                    AppendInt32Value((int)rule.DateEnd.Kind, ref serializedText);
                    AppendInt64Value(rule.DaylightDelta.Ticks, ref serializedText);
                    AppendInt64Value(rule.BaseUtcOffsetDelta.Ticks, ref serializedText);
                    AppendInt32Value(rule.NoDaylightTransitions ? 1 : 0, ref serializedText);
                    SerializeFullFidelityTransitionTime(rule.DaylightTransitionStart, ref serializedText);
                    SerializeFullFidelityTransitionTime(rule.DaylightTransitionEnd, ref serializedText);
                }
            }

            /// <summary>
            /// Serializes a TransitionTime with full fidelity, including empty (default) transitions.
            /// </summary>
            private static void SerializeFullFidelityTransitionTime(TransitionTime transition, ref ValueStringBuilder serializedText)
            {
                if (transition == default)
                {
                    serializedText.Append('D');
                    serializedText.Append(Sep);
                    return;
                }

                if (transition.IsFixedDateRule)
                {
                    serializedText.Append('F');
                    serializedText.Append(Sep);
                    AppendInt64Value(transition.TimeOfDay.Ticks, ref serializedText);
                    AppendInt32Value(transition.Month, ref serializedText);
                    AppendInt32Value(transition.Day, ref serializedText);
                }
                else
                {
                    serializedText.Append('W');
                    serializedText.Append(Sep);
                    AppendInt64Value(transition.TimeOfDay.Ticks, ref serializedText);
                    AppendInt32Value(transition.Month, ref serializedText);
                    AppendInt32Value(transition.Week, ref serializedText);
                    AppendInt32Value((int)transition.DayOfWeek, ref serializedText);
                }
            }

            private static void AppendInt32Value(int value, ref ValueStringBuilder serializedText)
            {
                serializedText.AppendSpanFormattable(value, format: default, CultureInfo.InvariantCulture);
                serializedText.Append(Sep);
            }

            private static void AppendInt64Value(long value, ref ValueStringBuilder serializedText)
            {
                serializedText.AppendSpanFormattable(value, format: default, CultureInfo.InvariantCulture);
                serializedText.Append(Sep);
            }

            /// <summary>
            /// Appends the String to the StringBuilder with all of the reserved chars escaped.
            ///
            /// ";" -> "\;"
            /// "[" -> "\["
            /// "]" -> "\]"
            /// "\" -> "\\"
            /// </summary>
            private static void SerializeSubstitute(string text, ref ValueStringBuilder serializedText)
            {
                foreach (char c in text)
                {
                    if (c == Esc || c == Lhs || c == Rhs || c == Sep)
                    {
                        serializedText.Append('\\');
                    }
                    serializedText.Append(c);
                }
            }

            /// <summary>
            /// Helper method to serialize a TimeZoneInfo.TransitionTime object.
            /// </summary>
            private static void SerializeTransitionTime(TransitionTime time, ref ValueStringBuilder serializedText)
            {
                serializedText.Append(Lhs);
                serializedText.Append(time.IsFixedDateRule ? '1' : '0');
                serializedText.Append(Sep);
                serializedText.AppendSpanFormattable(time.TimeOfDay, TimeOfDayFormat, DateTimeFormatInfo.InvariantInfo);
                serializedText.Append(Sep);
                serializedText.AppendSpanFormattable(time.Month, format: default, CultureInfo.InvariantCulture);
                serializedText.Append(Sep);
                if (time.IsFixedDateRule)
                {
                    serializedText.AppendSpanFormattable(time.Day, format: default, CultureInfo.InvariantCulture);
                    serializedText.Append(Sep);
                }
                else
                {
                    serializedText.AppendSpanFormattable(time.Week, format: default, CultureInfo.InvariantCulture);
                    serializedText.Append(Sep);
                    serializedText.AppendSpanFormattable((int)time.DayOfWeek, format: default, CultureInfo.InvariantCulture);
                    serializedText.Append(Sep);
                }
                serializedText.Append(Rhs);
            }

            /// <summary>
            /// Helper function to determine if the passed in string token is allowed to be preceded by an escape sequence token.
            /// </summary>
            private static void VerifyIsEscapableCharacter(char c)
            {
                if (c != Esc && c != Sep && c != Lhs && c != Rhs)
                {
                    throw new SerializationException(SR.Format(SR.Serialization_InvalidEscapeSequence, c));
                }
            }

            /// <summary>
            /// Helper function that reads past "v.Next" data fields. Receives a "depth" parameter indicating the
            /// current relative nested bracket depth that _currentTokenStartIndex is at. The function ends
            /// successfully when "depth" returns to zero (0).
            /// </summary>
            private void SkipVersionNextDataFields(int depth /* starting depth in the nested brackets ('[', ']')*/)
            {
                if (_currentTokenStartIndex < 0 || _currentTokenStartIndex >= _serializedText.Length)
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }
                State tokenState = State.NotEscaped;

                // walk the serialized text, building up the token as we go...
                for (int i = _currentTokenStartIndex; i < _serializedText.Length; i++)
                {
                    if (tokenState == State.Escaped)
                    {
                        VerifyIsEscapableCharacter(_serializedText[i]);
                        tokenState = State.NotEscaped;
                    }
                    else if (tokenState == State.NotEscaped)
                    {
                        switch (_serializedText[i])
                        {
                            case Esc:
                                tokenState = State.Escaped;
                                break;

                            case Lhs:
                                depth++;
                                break;
                            case Rhs:
                                depth--;
                                if (depth == 0)
                                {
                                    _currentTokenStartIndex = i + 1;
                                    if (_currentTokenStartIndex >= _serializedText.Length)
                                    {
                                        _state = State.EndOfLine;
                                    }
                                    else
                                    {
                                        _state = State.StartOfToken;
                                    }
                                    return;
                                }
                                break;

                            case '\0':
                                // invalid character
                                throw new SerializationException(SR.Serialization_InvalidData);

                            default:
                                break;
                        }
                    }
                }

                throw new SerializationException(SR.Serialization_InvalidData);
            }

            /// <summary>
            /// Helper function that reads a string token from the serialized text. The function
            /// updates <see cref="_currentTokenStartIndex"/> to point to the next token on exit.
            /// Also <see cref="_state"/> is set to either <see cref="State.StartOfToken"/> or
            /// <see cref="State.EndOfLine"/> on exit.
            /// </summary>
            private unsafe string GetNextStringValue()
            {
                // first verify the internal state of the object
                if (_state == State.EndOfLine)
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }
                if (_currentTokenStartIndex < 0 || _currentTokenStartIndex >= _serializedText.Length)
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }
                State tokenState = State.NotEscaped;
                var token = new ValueStringBuilder(stackalloc char[InitialCapacityForString]);

                // walk the serialized text, building up the token as we go...
                for (int i = _currentTokenStartIndex; i < _serializedText.Length; i++)
                {
                    if (tokenState == State.Escaped)
                    {
                        VerifyIsEscapableCharacter(_serializedText[i]);
                        token.Append(_serializedText[i]);
                        tokenState = State.NotEscaped;
                    }
                    else if (tokenState == State.NotEscaped)
                    {
                        switch (_serializedText[i])
                        {
                            case Esc:
                                tokenState = State.Escaped;
                                break;

                            case Lhs:
                                // '[' is an unexpected character
                                throw new SerializationException(SR.Serialization_InvalidData);

                            case Rhs:
                                // ']' is an unexpected character
                                throw new SerializationException(SR.Serialization_InvalidData);

                            case Sep:
                                _currentTokenStartIndex = i + 1;
                                if (_currentTokenStartIndex >= _serializedText.Length)
                                {
                                    _state = State.EndOfLine;
                                }
                                else
                                {
                                    _state = State.StartOfToken;
                                }
                                return token.ToString();

                            case '\0':
                                // invalid character
                                throw new SerializationException(SR.Serialization_InvalidData);

                            default:
                                token.Append(_serializedText[i]);
                                break;
                        }
                    }
                }
                //
                // we are at the end of the line
                //
                if (tokenState == State.Escaped)
                {
                    // we are at the end of the serialized text but we are in an escaped state
                    throw new SerializationException(SR.Format(SR.Serialization_InvalidEscapeSequence, string.Empty));
                }

                throw new SerializationException(SR.Serialization_InvalidData);
            }

            /// <summary>
            /// Helper function to read a DateTime token.
            /// </summary>
            private DateTime GetNextDateTimeValue(string format)
            {
                string token = GetNextStringValue();
                if (!DateTime.TryParseExact(token, format, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out DateTime time))
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }
                return time;
            }

            /// <summary>
            /// Helper function to read a TimeSpan token.
            /// </summary>
            private TimeSpan GetNextTimeSpanValue()
            {
                int token = GetNextInt32Value();
                try
                {
                    return new TimeSpan(hours: 0, minutes: token, seconds: 0);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    throw new SerializationException(SR.Serialization_InvalidData, e);
                }
            }

            /// <summary>
            /// Helper function to read an Int32 token.
            /// </summary>
            private int GetNextInt32Value()
            {
                string token = GetNextStringValue();
                if (!int.TryParse(token, NumberStyles.AllowLeadingSign /* "[sign]digits" */, CultureInfo.InvariantCulture, out int value))
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }
                return value;
            }

            /// <summary>
            /// Helper function to read an Int64 token.
            /// </summary>
            private long GetNextInt64Value()
            {
                string token = GetNextStringValue();
                if (!long.TryParse(token, NumberStyles.AllowLeadingSign /* "[sign]digits" */, CultureInfo.InvariantCulture, out long value))
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }
                return value;
            }

            /// <summary>
            /// Reads the optional full-fidelity adjustment rules that follow the separator terminating
            /// the legacy rule list. When present, they replace the legacy rules so the round trip is
            /// exact; when absent (or unrecognized future data), the legacy rules are returned unchanged.
            /// </summary>
            private AdjustmentRule[]? GetFullFidelityAdjustmentRulesIfPresent(AdjustmentRule[]? legacyRules)
            {
                // The legacy rule array reader stops on the separator that terminates the list without
                // consuming it. If there is nothing after that separator, there is no full-fidelity data.
                if (_state == State.EndOfLine ||
                    _currentTokenStartIndex >= _serializedText.Length ||
                    _serializedText[_currentTokenStartIndex] != Sep)
                {
                    return legacyRules;
                }

                _currentTokenStartIndex++;
                if (_currentTokenStartIndex >= _serializedText.Length ||
                    _serializedText[_currentTokenStartIndex] != FullFidelityRulesMarker)
                {
                    // No marker: either end of string or unknown trailing data from a newer format. Keep
                    // the legacy rules.
                    return legacyRules;
                }

                _currentTokenStartIndex++;
                _state = State.StartOfToken;

                int version = GetNextInt32Value();
                if (version != FullFidelityRulesVersion)
                {
                    // A trailer written by a newer runtime using a layout this reader does not understand.
                    // Ignore it and keep the legacy rules rather than misparsing the newer data.
                    return legacyRules;
                }

                int count = GetNextInt32Value();

                // Guard against a corrupt or malicious count driving an unbounded allocation. Every rule
                // occupies at least MinSeparatorsPerFullFidelityRule characters, so a count larger than the
                // remaining characters can hold cannot be valid.
                if (count <= 0 || count > (_serializedText.Length - _currentTokenStartIndex) / MinSeparatorsPerFullFidelityRule)
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }

                AdjustmentRule[] rules = new AdjustmentRule[count];
                for (int i = 0; i < count; i++)
                {
                    rules[i] = GetNextFullFidelityAdjustmentRuleValue();
                }
                return rules;
            }

            /// <summary>
            /// Reads a single full-fidelity AdjustmentRule.
            /// </summary>
            private AdjustmentRule GetNextFullFidelityAdjustmentRuleValue()
            {
                long dateStartTicks = GetNextInt64Value();
                int dateStartKind = GetNextInt32Value();
                long dateEndTicks = GetNextInt64Value();
                int dateEndKind = GetNextInt32Value();
                long daylightDeltaTicks = GetNextInt64Value();
                long baseUtcOffsetDeltaTicks = GetNextInt64Value();
                int noDaylightTransitions = GetNextInt32Value();
                if (noDaylightTransitions is not (0 or 1))
                {
                    // The writer only emits 0 or 1; reject any other value rather than treating it as true.
                    throw new SerializationException(SR.Serialization_InvalidData);
                }
                TransitionTime daylightStart = GetNextFullFidelityTransitionTimeValue();
                TransitionTime daylightEnd = GetNextFullFidelityTransitionTimeValue();

                try
                {
                    return AdjustmentRule.CreateAdjustmentRule(
                        new DateTime(dateStartTicks, (DateTimeKind)dateStartKind),
                        new DateTime(dateEndTicks, (DateTimeKind)dateEndKind),
                        new TimeSpan(daylightDeltaTicks),
                        daylightStart,
                        daylightEnd,
                        new TimeSpan(baseUtcOffsetDeltaTicks),
                        noDaylightTransitions != 0);
                }
                catch (Exception e) when (e is ArgumentException or OverflowException)
                {
                    // The tick values are untrusted. Out-of-range dates surface as ArgumentException, and
                    // extreme delta values can overflow while CreateAdjustmentRule normalizes them; both
                    // are reported as corrupt serialized data.
                    throw new SerializationException(SR.Serialization_InvalidData, e);
                }
            }

            /// <summary>
            /// Reads a full-fidelity TransitionTime, including empty (default) transitions.
            /// </summary>
            private TransitionTime GetNextFullFidelityTransitionTimeValue()
            {
                string kind = GetNextStringValue();
                try
                {
                    switch (kind)
                    {
                        case "D":
                            return default;

                        case "F":
                            long fixedTicks = GetNextInt64Value();
                            int fixedMonth = GetNextInt32Value();
                            int fixedDay = GetNextInt32Value();
                            return TransitionTime.CreateFixedDateRule(new DateTime(fixedTicks), fixedMonth, fixedDay);

                        case "W":
                            long floatingTicks = GetNextInt64Value();
                            int floatingMonth = GetNextInt32Value();
                            int floatingWeek = GetNextInt32Value();
                            int floatingDayOfWeek = GetNextInt32Value();
                            return TransitionTime.CreateFloatingDateRule(new DateTime(floatingTicks), floatingMonth, floatingWeek, (DayOfWeek)floatingDayOfWeek);

                        default:
                            throw new SerializationException(SR.Serialization_InvalidData);
                    }
                }
                catch (ArgumentException e)
                {
                    throw new SerializationException(SR.Serialization_InvalidData, e);
                }
            }

            /// <summary>
            /// Helper function to read an AdjustmentRule[] token.
            /// </summary>
            private AdjustmentRule[]? GetNextAdjustmentRuleArrayValue()
            {
                List<AdjustmentRule> rules = new List<AdjustmentRule>(1);
                int count = 0;

                // individual AdjustmentRule array elements do not require semicolons
                AdjustmentRule? rule = GetNextAdjustmentRuleValue();
                while (rule != null)
                {
                    rules.Add(rule);
                    count++;

                    rule = GetNextAdjustmentRuleValue();
                }

                // the AdjustmentRule array must end with a separator
                if (_state == State.EndOfLine)
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }
                if (_currentTokenStartIndex < 0 || _currentTokenStartIndex >= _serializedText.Length)
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }

                return count != 0 ? rules.ToArray() : null;
            }

            /// <summary>
            /// Helper function to read an AdjustmentRule token.
            /// </summary>
            private AdjustmentRule? GetNextAdjustmentRuleValue()
            {
                // first verify the internal state of the object
                if (_state == State.EndOfLine)
                {
                    return null;
                }

                if (_currentTokenStartIndex < 0 || _currentTokenStartIndex >= _serializedText.Length)
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }

                // check to see if the very first token we see is the separator
                if (_serializedText[_currentTokenStartIndex] == Sep)
                {
                    return null;
                }

                // verify the current token is a left-hand-side marker ("[")
                if (_serializedText[_currentTokenStartIndex] != Lhs)
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }
                _currentTokenStartIndex++;

                DateTime dateStart = GetNextDateTimeValue(DateTimeFormat);
                DateTime dateEnd = GetNextDateTimeValue(DateTimeFormat);
                TimeSpan daylightDelta = GetNextTimeSpanValue();
                TransitionTime daylightStart = GetNextTransitionTimeValue();
                TransitionTime daylightEnd = GetNextTransitionTimeValue();
                TimeSpan baseUtcOffsetDelta = TimeSpan.Zero;
                int noDaylightTransitions = 0;

                // verify that the string is now at the right-hand-side marker ("]") ...

                if (_state == State.EndOfLine || _currentTokenStartIndex >= _serializedText.Length)
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }

                // Check if we have baseUtcOffsetDelta in the serialized string and then deserialize it
                if (char.IsAsciiDigit(_serializedText[_currentTokenStartIndex]) || _serializedText[_currentTokenStartIndex] is '-' or '+')
                {
                    baseUtcOffsetDelta = GetNextTimeSpanValue();
                }

                // Check if we have NoDaylightTransitions in the serialized string and then deserialize it
                if (_serializedText[_currentTokenStartIndex] is '0' or '1')
                {
                    noDaylightTransitions = GetNextInt32Value();
                }

                if (_state == State.EndOfLine || _currentTokenStartIndex >= _serializedText.Length)
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }

                if (_serializedText[_currentTokenStartIndex] != Rhs)
                {
                    // skip ahead of any "v.Next" data at the end of the AdjustmentRule
                    //
                    // FUTURE: if the serialization format is extended in the future then this
                    // code section will need to be changed to read the new fields rather
                    // than just skipping the data at the end of the [AdjustmentRule].
                    SkipVersionNextDataFields(1);
                }
                else
                {
                    _currentTokenStartIndex++;
                }

                // create the AdjustmentRule from the deserialized fields ...

                AdjustmentRule rule;
                try
                {
                    rule = AdjustmentRule.CreateAdjustmentRule(dateStart, dateEnd, daylightDelta, daylightStart, daylightEnd, baseUtcOffsetDelta, noDaylightTransitions > 0);
                }
                catch (ArgumentException e)
                {
                    throw new SerializationException(SR.Serialization_InvalidData, e);
                }

                // finally set the state to either EndOfLine or StartOfToken for the next caller
                if (_currentTokenStartIndex >= _serializedText.Length)
                {
                    _state = State.EndOfLine;
                }
                else
                {
                    _state = State.StartOfToken;
                }
                return rule;
            }

            /// <summary>
            /// Helper function to read a TransitionTime token.
            /// </summary>
            private TransitionTime GetNextTransitionTimeValue()
            {
                // first verify the internal state of the object

                if (_state == State.EndOfLine ||
                    (_currentTokenStartIndex < _serializedText.Length && _serializedText[_currentTokenStartIndex] == Rhs))
                {
                    //
                    // we are at the end of the line or we are starting at a "]" character
                    //
                    throw new SerializationException(SR.Serialization_InvalidData);
                }

                if (_currentTokenStartIndex < 0 || _currentTokenStartIndex >= _serializedText.Length)
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }

                // verify the current token is a left-hand-side marker ("[")

                if (_serializedText[_currentTokenStartIndex] != Lhs)
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }
                _currentTokenStartIndex++;

                int isFixedDate = GetNextInt32Value();

                if (isFixedDate != 0 && isFixedDate != 1)
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }

                TransitionTime transition;

                DateTime timeOfDay = TimeOnly.FromDateTime(GetNextDateTimeValue(TimeOfDayFormat)).ToDateTime();

                int month = GetNextInt32Value();

                if (isFixedDate == 1)
                {
                    int day = GetNextInt32Value();

                    try
                    {
                        transition = TransitionTime.CreateFixedDateRule(timeOfDay, month, day);
                    }
                    catch (ArgumentException e)
                    {
                        throw new SerializationException(SR.Serialization_InvalidData, e);
                    }
                }
                else
                {
                    int week = GetNextInt32Value();
                    int dayOfWeek = GetNextInt32Value();

                    try
                    {
                        transition = TransitionTime.CreateFloatingDateRule(timeOfDay, month, week, (DayOfWeek)dayOfWeek);
                    }
                    catch (ArgumentException e)
                    {
                        throw new SerializationException(SR.Serialization_InvalidData, e);
                    }
                }

                // verify that the string is now at the right-hand-side marker ("]") ...

                if (_state == State.EndOfLine || _currentTokenStartIndex >= _serializedText.Length)
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }

                if (_serializedText[_currentTokenStartIndex] != Rhs)
                {
                    // skip ahead of any "v.Next" data at the end of the AdjustmentRule
                    //
                    // FUTURE: if the serialization format is extended in the future then this
                    // code section will need to be changed to read the new fields rather
                    // than just skipping the data at the end of the [TransitionTime].
                    SkipVersionNextDataFields(1);
                }
                else
                {
                    _currentTokenStartIndex++;
                }

                // check to see if the string is now at the separator (";") ...
                bool sepFound = false;
                if (_currentTokenStartIndex < _serializedText.Length &&
                    _serializedText[_currentTokenStartIndex] == Sep)
                {
                    // handle the case where we ended on a ";"
                    _currentTokenStartIndex++;
                    sepFound = true;
                }

                if (!sepFound)
                {
                    // we MUST end on a separator
                    throw new SerializationException(SR.Serialization_InvalidData);
                }

                // finally set the state to either EndOfLine or StartOfToken for the next caller
                if (_currentTokenStartIndex >= _serializedText.Length)
                {
                    _state = State.EndOfLine;
                }
                else
                {
                    _state = State.StartOfToken;
                }
                return transition;
            }
        }
    }
}

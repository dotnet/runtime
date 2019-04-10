// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
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

            /// <summary>
            /// Creates the custom serialized string representation of a TimeZoneInfo instance.
            /// </summary>
            public static string GetSerializedString(TimeZoneInfo zone)
            {
                StringBuilder serializedText = StringBuilderCache.Acquire();

                //
                // <_id>;<_baseUtcOffset>;<_displayName>;<_standardDisplayName>;<_daylightDispayName>
                //
                SerializeSubstitute(zone.Id, serializedText);
                serializedText.Append(Sep);
                serializedText.AppendSpanFormattable(zone.BaseUtcOffset.TotalMinutes, format: default, CultureInfo.InvariantCulture);
                serializedText.Append(Sep);
                SerializeSubstitute(zone.DisplayName, serializedText);
                serializedText.Append(Sep);
                SerializeSubstitute(zone.StandardName, serializedText);
                serializedText.Append(Sep);
                SerializeSubstitute(zone.DaylightName, serializedText);
                serializedText.Append(Sep);

                AdjustmentRule[] rules = zone.GetAdjustmentRules();
                foreach (AdjustmentRule rule in rules)
                {
                    serializedText.Append(Lhs);
                    serializedText.AppendSpanFormattable(rule.DateStart, DateTimeFormat, DateTimeFormatInfo.InvariantInfo);
                    serializedText.Append(Sep);
                    serializedText.AppendSpanFormattable(rule.DateEnd, DateTimeFormat, DateTimeFormatInfo.InvariantInfo);
                    serializedText.Append(Sep);
                    serializedText.AppendSpanFormattable(rule.DaylightDelta.TotalMinutes, format: default, CultureInfo.InvariantCulture);
                    serializedText.Append(Sep);
                    // serialize the TransitionTime's
                    SerializeTransitionTime(rule.DaylightTransitionStart, serializedText);
                    serializedText.Append(Sep);
                    SerializeTransitionTime(rule.DaylightTransitionEnd, serializedText);
                    serializedText.Append(Sep);
                    if (rule.BaseUtcOffsetDelta != TimeSpan.Zero)
                    {
                        // Serialize it only when BaseUtcOffsetDelta has a value to reduce the impact of adding rule.BaseUtcOffsetDelta
                        serializedText.AppendSpanFormattable(rule.BaseUtcOffsetDelta.TotalMinutes, format: default, CultureInfo.InvariantCulture);
                        serializedText.Append(Sep);
                    }
                    if (rule.NoDaylightTransitions)
                    {
                        // Serialize it only when NoDaylightTransitions is true to reduce the impact of adding rule.NoDaylightTransitions
                        serializedText.Append('1');
                        serializedText.Append(Sep);
                    }
                    serializedText.Append(Rhs);
                }
                serializedText.Append(Sep);

                return StringBuilderCache.GetStringAndRelease(serializedText);
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
            /// Appends the String to the StringBuilder with all of the reserved chars escaped.
            ///
            /// ";" -> "\;"
            /// "[" -> "\["
            /// "]" -> "\]"
            /// "\" -> "\\"
            /// </summary>
            private static void SerializeSubstitute(string text, StringBuilder serializedText)
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
            private static void SerializeTransitionTime(TransitionTime time, StringBuilder serializedText)
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
            private string GetNextStringValue()
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
                StringBuilder token = StringBuilderCache.Acquire(InitialCapacityForString);

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
                                return StringBuilderCache.GetStringAndRelease(token);

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
                DateTime time;
                if (!DateTime.TryParseExact(token, format, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out time))
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
                int value;
                if (!int.TryParse(token, NumberStyles.AllowLeadingSign /* "[sign]digits" */, CultureInfo.InvariantCulture, out value))
                {
                    throw new SerializationException(SR.Serialization_InvalidData);
                }
                return value;
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
                if ((_serializedText[_currentTokenStartIndex] >= '0' && _serializedText[_currentTokenStartIndex] <= '9') ||
                    _serializedText[_currentTokenStartIndex] == '-' || _serializedText[_currentTokenStartIndex] == '+')
                {
                    baseUtcOffsetDelta = GetNextTimeSpanValue();
                }

                // Check if we have NoDaylightTransitions in the serialized string and then deserialize it
                if ((_serializedText[_currentTokenStartIndex] >= '0' && _serializedText[_currentTokenStartIndex] <= '1'))
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

                DateTime timeOfDay = GetNextDateTimeValue(TimeOfDayFormat);
                timeOfDay = new DateTime(1, 1, 1, timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);

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

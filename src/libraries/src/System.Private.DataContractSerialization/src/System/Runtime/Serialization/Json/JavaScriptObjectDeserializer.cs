// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;


namespace System.Runtime.Serialization.Json
{
#if FEATURE_LEGACYNETCF
    public
#else
    internal
#endif
    class JavaScriptObjectDeserializer
    {
        private const string DateTimePrefix = @"""\/Date(";
        private const int DateTimePrefixLength = 8;

        internal JavaScriptString _s;
        private bool _isDataContract;

#if FEATURE_LEGACYNETCF
        public
#else
        internal
#endif
        object BasicDeserialize()
        {
            object result = this.DeserializeInternal(0);
            if (this._s.GetNextNonEmptyChar() != null)
            {
                throw new SerializationException(SR.Format(SR.ObjectDeserializer_IllegalPrimitive, this._s.ToString()));
            }
            return result;
        }

        internal JavaScriptObjectDeserializer(string input)
            : this(input, true)
        {
        }

#if FEATURE_LEGACYNETCF
        public
#else
        internal
#endif
        JavaScriptObjectDeserializer(string input, bool isDataContract)
        {
            _s = new JavaScriptString(input);
            _isDataContract = isDataContract;
        }

        private object DeserializeInternal(int depth)
        {
            Nullable<Char> c = _s.GetNextNonEmptyChar();
            if (c == null)
            {
                return null;
            }

            _s.MovePrev();

            if (IsNextElementDateTime())
            {
                return DeserializeStringIntoDateTime();
            }

            if (IsNextElementObject(c))
            {
                IDictionary<string, object> dict = DeserializeDictionary(depth);
                return dict;
            }

            if (IsNextElementArray(c))
            {
                return DeserializeList(depth);
            }

            if (IsNextElementString(c))
            {
                return DeserializeString();
            }

            return DeserializePrimitiveObject();
        }

        private IList DeserializeList(int depth)
        {
            IList list = new List<object>();
            Nullable<Char> c = _s.MoveNext();
            if (c != '[')
            {
                throw new SerializationException(_s.GetDebugString(SR.Format(SR.ObjectDeserializer_UnexpectedToken, c, '[')));
            }

            bool expectMore = false;
            while ((c = _s.GetNextNonEmptyChar()) != null && c != ']')
            {
                _s.MovePrev();
                object o = DeserializeInternal(depth);
                list.Add(o);

                expectMore = false;

                c = _s.GetNextNonEmptyChar();
                if (c == ']')
                {
                    break;
                }

                expectMore = true;
                if (c != ',')
                {
                    throw new SerializationException(_s.GetDebugString(SR.Format(SR.ObjectDeserializer_UnexpectedToken, c, ',')));
                }
            }
            if (expectMore)
            {
                throw new SerializationException(_s.GetDebugString(SR.Format(SR.ObjectDeserializer_InvalidArrayExtraComma)));
            }
            if (c != ']')
            {
                throw new SerializationException(_s.GetDebugString(SR.Format(SR.ObjectDeserializer_UnexpectedToken, c, ']')));
            }
            return list;
        }

        private IDictionary<string, object> DeserializeDictionary(int depth)
        {
            IDictionary<string, object> dictionary = null;
            Nullable<Char> c = _s.MoveNext();
            if (c != '{')
            {
                throw new SerializationException(_s.GetDebugString(SR.Format(SR.ObjectDeserializer_UnexpectedToken, c, '{')));
            }

            bool encounteredFirstMember = false;
            while ((c = _s.GetNextNonEmptyChar()) != null)
            {
                _s.MovePrev();

                if (c == ':')
                {
                    throw new SerializationException(_s.GetDebugString(SR.Format(SR.ObjectDeserializer_InvalidMemberName)));
                }

                string memberName = null;
                if (c != '}')
                {
                    memberName = DeserializeMemberName();
                    if (String.IsNullOrEmpty(memberName))
                    {
                        throw new SerializationException
                            (_s.GetDebugString(SR.Format(SR.ObjectDeserializer_InvalidMemberName)));
                    }
                    c = _s.GetNextNonEmptyChar();
                    if (c != ':')
                    {
                        throw new SerializationException(_s.GetDebugString(SR.Format(SR.ObjectDeserializer_UnexpectedToken, c, ':')));
                    }
                }

                if (dictionary == null)
                {
                    dictionary = new Dictionary<string, object>();
                    if (String.IsNullOrEmpty(memberName))
                    {
                        c = _s.GetNextNonEmptyChar();
                        break;
                    }
                }

                object propVal = DeserializeInternal(depth);
                //Ignore the __type attribute when its not the first element in the dictionary
                if (!encounteredFirstMember || (memberName != null && !memberName.Equals(JsonGlobals.ServerTypeString)))
                {
                    if (dictionary.ContainsKey(memberName))
                    {
                        throw new SerializationException(SR.Format(SR.JsonDuplicateMemberInInput, memberName));
                    }

                    dictionary[memberName] = propVal;
                    //Added the first member from the dictionary
                    encounteredFirstMember = true;
                }

                c = _s.GetNextNonEmptyChar();
                if (c == '}')
                {
                    break;
                }

                if (c != ',')
                {
                    throw new SerializationException(_s.GetDebugString(SR.Format(SR.ObjectDeserializer_UnexpectedToken, c, ',')));
                }
            }

            if (c != '}')
            {
                throw new SerializationException(_s.GetDebugString(SR.Format(SR.ObjectDeserializer_UnexpectedToken, c, '}')));
            }

            return dictionary;
        }

        private string DeserializeMemberName()
        {
            Nullable<Char> c = _s.GetNextNonEmptyChar();
            if (c == null)
            {
                return null;
            }

            _s.MovePrev();


            if (IsNextElementString(c))
            {
                return DeserializeString();
            }


            return DeserializePrimitiveToken();
        }

        private object DeserializePrimitiveObject()
        {
            string input = DeserializePrimitiveToken();
            if (input.Equals("null"))
            {
                return null;
            }

            if (input.Equals(Globals.True))
            {
                return true;
            }

            if (input.Equals(Globals.False))
            {
                return false;
            }

            if (input.Equals(JsonGlobals.PositiveInf))
            {
                return float.PositiveInfinity;
            }
            if (input.Equals(JsonGlobals.NegativeInf))
            {
                return float.NegativeInfinity;
            }

            bool hasDecimalPoint = input.IndexOfAny(JsonGlobals.FloatingPointCharacters) >= 0;
            if (!hasDecimalPoint)
            {
                int parseInt;
                if (Int32.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out parseInt))
                {
                    return parseInt;
                }
                long parseLong;
                if (Int64.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out parseLong))
                {
                    return parseLong;
                }

                if (_isDataContract)
                {
                    return JavaScriptObjectDeserializer.ParseJsonNumberAsDoubleOrDecimal(input);
                }
            }
            //Ensure that the number is a valid JSON number.
            object ret = JavaScriptObjectDeserializer.ParseJsonNumberAsDoubleOrDecimal(input);
            if (ret.GetType() == Globals.TypeOfString)
            {
                throw new SerializationException(SR.Format(SR.ObjectDeserializer_IllegalPrimitive, input));
            }


            // return floating point number as string in DataContract case.
            return _isDataContract ? input : ret;
        }

        private string DeserializePrimitiveToken()
        {
            StringBuilder sb = new StringBuilder();
            Nullable<Char> c = null;
            while ((c = _s.MoveNext()) != null)
            {
                if (Char.IsLetterOrDigit(c.Value) || c.Value == '.' ||
                    c.Value == '-' || c.Value == '_' || c.Value == '+')
                {
                    sb.Append(c);
                }
                else
                {
                    _s.MovePrev();
                    break;
                }
            }

            return sb.ToString();
        }

        internal string DeserializeString()
        {
            StringBuilder sb = new StringBuilder();
            bool escapedChar = false;

            Nullable<Char> c = _s.MoveNext();


            Char quoteChar = CheckQuoteChar(c);
            while ((c = _s.MoveNext()) != null)
            {
                if (c == '\\')
                {
                    if (escapedChar)
                    {
                        sb.Append('\\');
                        escapedChar = false;
                    }
                    else
                    {
                        escapedChar = true;
                    }

                    continue;
                }

                if (escapedChar)
                {
                    AppendCharToBuilder(c, sb);
                    escapedChar = false;
                }
                else
                {
                    if (c == quoteChar)
                    {
                        return sb.ToString();
                    }

                    sb.Append(c);
                }
            }

            throw new SerializationException(_s.GetDebugString(SR.Format(SR.ObjectDeserializer_UnterminatedString)));
        }

        private void AppendCharToBuilder(char? c, StringBuilder sb)
        {
            if (c == '"' || c == '\'' || c == '/')
            {
                sb.Append(c);
            }
            else if (c == 'b')
            {
                sb.Append('\b');
            }
            else if (c == 'f')
            {
                sb.Append('\f');
            }
            else if (c == 'n')
            {
                sb.Append('\n');
            }
            else if (c == 'r')
            {
                sb.Append('\r');
            }
            else if (c == 't')
            {
                sb.Append('\t');
            }
            else if (c == 'u')
            {
                sb.Append((char)int.Parse(_s.MoveNext(4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            }
            else
            {
                throw new SerializationException(_s.GetDebugString(SR.Format(SR.ObjectDeserializer_BadEscape)));
            }
        }

        private char CheckQuoteChar(char? c)
        {
            Char quoteChar = '"';
            if (c == '\'')
            {
                quoteChar = c.Value;
            }
            else if (c != '"')
            {
                throw new SerializationException(_s.GetDebugString(SR.Format(SR.ObjectDeserializer_StringNotQuoted)));
            }

            return quoteChar;
        }

        private object DeserializeStringIntoDateTime()
        {
            string dateTimeValue = DeserializeString();
            string ticksvalue = dateTimeValue.Substring(6, dateTimeValue.Length - 8);
            long millisecondsSinceUnixEpoch;
            DateTimeKind dateTimeKind = DateTimeKind.Utc;
            int indexOfTimeZoneOffset = ticksvalue.IndexOf('+', 1);

            if (indexOfTimeZoneOffset == -1)
            {
                indexOfTimeZoneOffset = ticksvalue.IndexOf('-', 1);
            }

            if (indexOfTimeZoneOffset != -1)
            {
                dateTimeKind = DateTimeKind.Local;
                ticksvalue = ticksvalue.Substring(0, indexOfTimeZoneOffset);
            }

            try
            {
                millisecondsSinceUnixEpoch = Int64.Parse(ticksvalue, NumberStyles.Integer, CultureInfo.InvariantCulture);
            }
            catch (ArgumentException exception)
            {
                throw System.ServiceModel.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(ticksvalue, "Int64", exception));
            }
            catch (FormatException exception)
            {
                throw System.ServiceModel.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(ticksvalue, "Int64", exception));
            }
            catch (OverflowException exception)
            {
                throw System.ServiceModel.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(ticksvalue, "Int64", exception));
            }

            // Convert from # milliseconds since epoch to # of 100-nanosecond units, which is what DateTime understands
            long ticks = millisecondsSinceUnixEpoch * 10000 + JsonGlobals.unixEpochTicks;

            try
            {
                DateTime dateTime = new DateTime(ticks, DateTimeKind.Utc);
                switch (dateTimeKind)
                {
                    case DateTimeKind.Local:
                        dateTime = dateTime.ToLocalTime();
                        break;
                    case DateTimeKind.Unspecified:
                        dateTime = DateTime.SpecifyKind(dateTime.ToLocalTime(), DateTimeKind.Unspecified);
                        break;
                    case DateTimeKind.Utc:
                    default:
                        break;
                }

                // This string could be serialized from DateTime or String, keeping both until DataContract information is available
                return Tuple.Create<DateTime, string>(dateTime, dateTimeValue);
            }
            catch (ArgumentException exception)
            {
                throw System.ServiceModel.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(ticksvalue, "DateTime", exception));
            }
        }

        private static bool IsNextElementArray(Nullable<Char> c)
        {
            return c == '[';
        }

        private bool IsNextElementDateTime()
        {
            String next = _s.MoveNext(DateTimePrefixLength);
            if (next != null)
            {
                _s.MovePrev(DateTimePrefixLength);
                return String.Equals(next, DateTimePrefix, StringComparison.Ordinal);
            }

            return false;
        }

        private static bool IsNextElementObject(Nullable<Char> c)
        {
            return c == '{';
        }

        private static bool IsNextElementString(Nullable<Char> c)
        {
            return c == '"' || c == '\'';
        }

        internal static object ParseJsonNumberAsDoubleOrDecimal(string input)
        {
            decimal parseDecimal;
            if (decimal.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out parseDecimal) && parseDecimal != 0)
            {
                return parseDecimal;
            }

            double parseDouble;
            if (Double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out parseDouble))
            {
                return parseDouble;
            }

            return input;
        }
    }
}


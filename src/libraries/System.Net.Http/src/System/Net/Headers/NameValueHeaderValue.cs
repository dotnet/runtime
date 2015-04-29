// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;

namespace System.Net.Http.Headers
{
    // According to the RFC, in places where a "parameter" is required, the value is mandatory 
    // (e.g. Media-Type, Accept). However, we don't introduce a dedicated type for it. So NameValueHeaderValue supports
    // name-only values in addition to name/value pairs.
    public class NameValueHeaderValue : ICloneable
    {
        private static readonly Func<NameValueHeaderValue> s_defaultNameValueCreator = CreateNameValue;

        private string _name;
        private string _value;

        public string Name
        {
            get { return _name; }
        }

        public string Value
        {
            get { return _value; }
            set
            {
                CheckValueFormat(value);
                _value = value;
            }
        }

        internal NameValueHeaderValue()
        {
        }

        public NameValueHeaderValue(string name)
            : this(name, null)
        {
        }

        public NameValueHeaderValue(string name, string value)
        {
            CheckNameValueFormat(name, value);

            _name = name;
            _value = value;
        }

        protected NameValueHeaderValue(NameValueHeaderValue source)
        {
            Contract.Requires(source != null);

            _name = source._name;
            _value = source._value;
        }

        public override int GetHashCode()
        {
            Debug.Assert(_name != null);

            int nameHashCode = _name.ToLowerInvariant().GetHashCode();

            if (!string.IsNullOrEmpty(_value))
            {
                // If we have a quoted-string, then just use the hash code. If we have a token, convert to lowercase 
                // and retrieve the hash code.
                if (_value[0] == '"')
                {
                    return nameHashCode ^ _value.GetHashCode();
                }

                return nameHashCode ^ _value.ToLowerInvariant().GetHashCode();
            }

            return nameHashCode;
        }

        public override bool Equals(object obj)
        {
            NameValueHeaderValue other = obj as NameValueHeaderValue;

            if (other == null)
            {
                return false;
            }

            if (string.Compare(_name, other._name, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }

            // RFC2616: 14.20: unquoted tokens should use case-INsensitive comparison; quoted-strings should use
            // case-sensitive comparison. The RFC doesn't mention how to compare quoted-strings outside the "Expect"
            // header. We treat all quoted-strings the same: case-sensitive comparison. 

            if (string.IsNullOrEmpty(_value))
            {
                return string.IsNullOrEmpty(other._value);
            }

            if (_value[0] == '"')
            {
                // We have a quoted string, so we need to do case-sensitive comparison.
                return (string.CompareOrdinal(_value, other._value) == 0);
            }
            else
            {
                return (string.Compare(_value, other._value, StringComparison.OrdinalIgnoreCase) == 0);
            }
        }

        public static NameValueHeaderValue Parse(string input)
        {
            int index = 0;
            return (NameValueHeaderValue)GenericHeaderParser.SingleValueNameValueParser.ParseValue(
                input, null, ref index);
        }

        public static bool TryParse(string input, out NameValueHeaderValue parsedValue)
        {
            int index = 0;
            object output;
            parsedValue = null;

            if (GenericHeaderParser.SingleValueNameValueParser.TryParseValue(input, null, ref index, out output))
            {
                parsedValue = (NameValueHeaderValue)output;
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(_value))
            {
                return _name + "=" + _value;
            }
            return _name;
        }

        internal static void ToString(ICollection<NameValueHeaderValue> values, char separator, bool leadingSeparator,
            StringBuilder destination)
        {
            Debug.Assert(destination != null);

            if ((values == null) || (values.Count == 0))
            {
                return;
            }

            foreach (var value in values)
            {
                if (leadingSeparator || (destination.Length > 0))
                {
                    destination.Append(separator);
                    destination.Append(' ');
                }
                destination.Append(value.ToString());
            }
        }

        internal static string ToString(ICollection<NameValueHeaderValue> values, char separator, bool leadingSeparator)
        {
            if ((values == null) || (values.Count == 0))
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();

            ToString(values, separator, leadingSeparator, sb);

            return sb.ToString();
        }

        internal static int GetHashCode(ICollection<NameValueHeaderValue> values)
        {
            if ((values == null) || (values.Count == 0))
            {
                return 0;
            }

            int result = 0;
            foreach (var value in values)
            {
                result = result ^ value.GetHashCode();
            }
            return result;
        }

        internal static int GetNameValueLength(string input, int startIndex, out NameValueHeaderValue parsedValue)
        {
            return GetNameValueLength(input, startIndex, s_defaultNameValueCreator, out parsedValue);
        }

        internal static int GetNameValueLength(string input, int startIndex,
            Func<NameValueHeaderValue> nameValueCreator, out NameValueHeaderValue parsedValue)
        {
            Contract.Requires(input != null);
            Contract.Requires(startIndex >= 0);
            Contract.Requires(nameValueCreator != null);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Parse the name, i.e. <name> in name/value string "<name>=<value>". Caller must remove 
            // leading whitespaces.
            int nameLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (nameLength == 0)
            {
                return 0;
            }

            string name = input.Substring(startIndex, nameLength);
            int current = startIndex + nameLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            // Parse the separator between name and value
            if ((current == input.Length) || (input[current] != '='))
            {
                // We only have a name and that's OK. Return.
                parsedValue = nameValueCreator();
                parsedValue._name = name;
                current = current + HttpRuleParser.GetWhitespaceLength(input, current); // skip whitespaces
                return current - startIndex;
            }

            current++; // skip delimiter.
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            // Parse the value, i.e. <value> in name/value string "<name>=<value>"
            int valueLength = GetValueLength(input, current);

            if (valueLength == 0)
            {
                return 0; // We have an invalid value. 
            }

            // Use parameterless ctor to avoid double-parsing of name and value, i.e. skip public ctor validation.
            parsedValue = nameValueCreator();
            parsedValue._name = name;
            parsedValue._value = input.Substring(current, valueLength);
            current = current + valueLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current); // skip whitespaces
            return current - startIndex;
        }

        // Returns the length of a name/value list, separated by 'delimiter'. E.g. "a=b, c=d, e=f" adds 3
        // name/value pairs to 'nameValueCollection' if 'delimiter' equals ','.
        internal static int GetNameValueListLength(string input, int startIndex, char delimiter,
            ICollection<NameValueHeaderValue> nameValueCollection)
        {
            Contract.Requires(nameValueCollection != null);
            Contract.Requires(startIndex >= 0);

            if ((string.IsNullOrEmpty(input)) || (startIndex >= input.Length))
            {
                return 0;
            }

            int current = startIndex + HttpRuleParser.GetWhitespaceLength(input, startIndex);
            while (true)
            {
                NameValueHeaderValue parameter = null;
                int nameValueLength = NameValueHeaderValue.GetNameValueLength(input, current,
                    s_defaultNameValueCreator, out parameter);

                if (nameValueLength == 0)
                {
                    return 0;
                }

                nameValueCollection.Add(parameter);
                current = current + nameValueLength;
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);

                if ((current == input.Length) || (input[current] != delimiter))
                {
                    // We're done and we have at least one valid name/value pair.
                    return current - startIndex;
                }

                // input[current] is 'delimiter'. Skip the delimiter and whitespaces and try to parse again.
                current++; // skip delimiter.
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);
            }
        }

        internal static NameValueHeaderValue Find(ICollection<NameValueHeaderValue> values, string name)
        {
            Contract.Requires((name != null) && (name.Length > 0));

            if ((values == null) || (values.Count == 0))
            {
                return null;
            }

            foreach (var value in values)
            {
                if (string.Compare(value.Name, name, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return value;
                }
            }
            return null;
        }

        internal static int GetValueLength(string input, int startIndex)
        {
            Contract.Requires(input != null);

            if (startIndex >= input.Length)
            {
                return 0;
            }

            int valueLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (valueLength == 0)
            {
                // A value can either be a token or a quoted string. Check if it is a quoted string.
                if (HttpRuleParser.GetQuotedStringLength(input, startIndex, out valueLength) != HttpParseResult.Parsed)
                {
                    // We have an invalid value. Reset the name and return.
                    return 0;
                }
            }
            return valueLength;
        }

        private static void CheckNameValueFormat(string name, string value)
        {
            HeaderUtilities.CheckValidToken(name, "name");
            CheckValueFormat(value);
        }

        private static void CheckValueFormat(string value)
        {
            // Either value is null/empty or a valid token/quoted string
            if (!(string.IsNullOrEmpty(value) || (GetValueLength(value, 0) == value.Length)))
            {
                throw new FormatException(string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, value));
            }
        }

        private static NameValueHeaderValue CreateNameValue()
        {
            return new NameValueHeaderValue();
        }

        // Implement ICloneable explicitly to allow derived types to "override" the implementation.
        object ICloneable.Clone()
        {
            return new NameValueHeaderValue(this);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.Net.Http.Headers
{
#if DEBUG
    [ContractClass(typeof(HttpHeaderParserContract))]
#endif
    internal abstract class HttpHeaderParser
    {
        internal const string DefaultSeparator = ", ";

        private bool _supportsMultipleValues;
        private string _separator;

        public bool SupportsMultipleValues
        {
            get { return _supportsMultipleValues; }
        }

        public string Separator
        {
            get
            {
                Debug.Assert(_supportsMultipleValues);
                return _separator;
            }
        }

        // If ValueType implements Equals() as required, there is no need to provide a comparer. A comparer is needed
        // e.g. if we want to compare strings using case-insensitive comparison.
        public virtual IEqualityComparer Comparer
        {
            get { return null; }
        }

        protected HttpHeaderParser(bool supportsMultipleValues)
        {
            _supportsMultipleValues = supportsMultipleValues;

            if (supportsMultipleValues)
            {
                _separator = DefaultSeparator;
            }
        }

        protected HttpHeaderParser(bool supportsMultipleValues, string separator)
        {
            Debug.Assert(!string.IsNullOrEmpty(separator));

            _supportsMultipleValues = supportsMultipleValues;
            _separator = separator;
        }

        // If a parser supports multiple values, a call to ParseValue/TryParseValue should return a value for 'index'
        // pointing to the next non-whitespace character after a delimiter. E.g. if called with a start index of 0
        // for string "value , second_value", then after the call completes, 'index' must point to 's', i.e. the first
        // non-whitespace after the separator ','.
        public abstract bool TryParseValue(string value, object storeValue, ref int index, out object parsedValue);

        public object ParseValue(string value, object storeValue, ref int index)
        {
            // Index may be value.Length (e.g. both 0). This may be allowed for some headers (e.g. Accept but not
            // allowed by others (e.g. Content-Length). The parser has to decide if this is valid or not.
            Debug.Assert((value == null) || ((index >= 0) && (index <= value.Length)));

            // If a parser returns 'null', it means there was no value, but that's valid (e.g. "Accept: "). The caller
            // can ignore the value.
            object result = null;
            if (!TryParseValue(value, storeValue, ref index, out result))
            {
                throw new FormatException(string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value,
                    value == null ? "<null>" : value.Substring(index)));
            }
            return result;
        }

        // If ValueType is a custom header value type (e.g. NameValueHeaderValue) it already implements ToString() correctly.
        // However for existing types like int, byte[], DateTimeOffset we can't override ToString(). Therefore the 
        // parser provides a ToString() virtual method that can be overridden by derived types to correctly serialize 
        // values (e.g. byte[] to Base64 encoded string).
        public virtual string ToString(object value)
        {
            Debug.Assert(value != null);

            return value.ToString();
        }
    }

#if DEBUG
    [ContractClassFor(typeof(HttpHeaderParser))]
    internal abstract class HttpHeaderParserContract : HttpHeaderParser
    {
        public HttpHeaderParserContract()
            : base(false)
        {
        }

        public override bool TryParseValue(string value, object storeValue, ref int index, out object parsedValue)
        {
            // Index may be value.Length (e.g. both 0). This may be allowed for some headers (e.g. Accept but not
            // allowed by others (e.g. Content-Length). The parser has to decide if this is valid or not.
            Debug.Assert((value == null) || ((index >= 0) && (index <= value.Length)));

            parsedValue = null;
            return false;
        }
    }
#endif
}

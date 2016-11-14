// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Text;

namespace System.Net.Http.Headers
{
    // This type is used to store a collection of headers in 'headerStore':
    // - A header can have multiple values. 
    // - A header can have an associated parser which is able to parse the raw string value into a strongly typed object.
    // - If a header has an associated parser and the provided raw value can't be parsed, the value is considered
    //   invalid. Invalid values are stored if added using TryAddWithoutValidation(). If the value was added using Add(),
    //   Add() will throw FormatException.
    // - Since parsing header values is expensive and users usually only care about a few headers, header values are
    //   lazily initialized.
    //
    // Given the properties above, a header value can have three states:
    // - 'raw': The header value was added using TryAddWithoutValidation() and it wasn't parsed yet.
    // - 'parsed': The header value was successfully parsed. It was either added using Add() where the value was parsed 
    //   immediately, or if added using TryAddWithoutValidation() a user already accessed a property/method triggering the 
    //   value to be parsed.
    // - 'invalid': The header value was parsed, but parsing failed because the value is invalid. Storing invalid values
    //   allows users to still retrieve the value (by calling GetValues()), but it will not be exposed as strongly typed
    //   object. E.g. the client receives a response with the following header: 'Via: 1.1 proxy, invalid'
    //   - HttpHeaders.GetValues() will return "1.1 proxy", "invalid"
    //   - HttpResponseHeaders.Via collection will only contain one ViaHeaderValue object with value "1.1 proxy"
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Justification = "This is not a collection")]
    public abstract class HttpHeaders : IEnumerable<KeyValuePair<string, IEnumerable<string>>>
    {
        private Dictionary<string, HeaderStoreItemInfo> _headerStore;
        private Dictionary<string, HttpHeaderParser> _parserStore;
        private HashSet<string> _invalidHeaders;

        private enum StoreLocation
        {
            Raw,
            Invalid,
            Parsed
        }

        protected HttpHeaders()
        {
        }

        public void Add(string name, string value)
        {
            CheckHeaderName(name);

            // We don't use GetOrCreateHeaderInfo() here, since this would create a new header in the store. If parsing
            // the value then throws, we would have to remove the header from the store again. So just get a 
            // HeaderStoreItemInfo object and try to parse the value. If it works, we'll add the header.
            HeaderStoreItemInfo info;
            bool addToStore;
            PrepareHeaderInfoForAdd(name, out info, out addToStore);

            ParseAndAddValue(name, info, value);

            // If we get here, then the value could be parsed correctly. If we created a new HeaderStoreItemInfo, add
            // it to the store if we added at least one value.
            if (addToStore && (info.ParsedValue != null))
            {
                AddHeaderToStore(name, info);
            }
        }

        public void Add(string name, IEnumerable<string> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }
            CheckHeaderName(name);

            HeaderStoreItemInfo info;
            bool addToStore;
            PrepareHeaderInfoForAdd(name, out info, out addToStore);

            try
            {
                // Note that if the first couple of values are valid followed by an invalid value, the valid values
                // will be added to the store before the exception for the invalid value is thrown.
                foreach (string value in values)
                {
                    ParseAndAddValue(name, info, value);
                }
            }
            finally
            {
                // Even if one of the values was invalid, make sure we add the header for the valid ones. We need to be
                // consistent here: If values get added to an _existing_ header, then all values until the invalid one
                // get added. Same here: If multiple values get added to a _new_ header, make sure the header gets added
                // with the valid values.
                // However, if all values for a _new_ header were invalid, then don't add the header.
                if (addToStore && (info.ParsedValue != null))
                {
                    AddHeaderToStore(name, info);
                }
            }
        }

        public bool TryAddWithoutValidation(string name, string value)
        {
            if (!TryCheckHeaderName(name))
            {
                return false;
            }

            if (value == null)
            {
                // We allow empty header values. (e.g. "My-Header: "). If the user adds multiple null/empty
                // values, we'll just add them to the collection. This will result in delimiter-only values:
                // E.g. adding two null-strings (or empty, or whitespace-only) results in "My-Header: ,".
                value = string.Empty;
            }

            HeaderStoreItemInfo info = GetOrCreateHeaderInfo(name, false);
            AddValue(info, value, StoreLocation.Raw);

            return true;
        }

        public bool TryAddWithoutValidation(string name, IEnumerable<string> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }
            if (!TryCheckHeaderName(name))
            {
                return false;
            }

            HeaderStoreItemInfo info = GetOrCreateHeaderInfo(name, false);
            foreach (string value in values)
            {
                // We allow empty header values. (e.g. "My-Header: "). If the user adds multiple null/empty
                // values, we'll just add them to the collection. This will result in delimiter-only values:
                // E.g. adding two null-strings (or empty, or whitespace-only) results in "My-Header: ,".
                AddValue(info, value ?? string.Empty, StoreLocation.Raw);
            }

            return true;
        }

        public void Clear()
        {
            if (_headerStore != null)
            {
                _headerStore.Clear();
            }
        }

        public bool Remove(string name)
        {
            CheckHeaderName(name);

            if (_headerStore == null)
            {
                return false;
            }

            return _headerStore.Remove(name);
        }

        public IEnumerable<string> GetValues(string name)
        {
            CheckHeaderName(name);

            IEnumerable<string> values;
            if (!TryGetValues(name, out values))
            {
                throw new InvalidOperationException(SR.net_http_headers_not_found);
            }

            return values;
        }

        public bool TryGetValues(string name, out IEnumerable<string> values)
        {
            if (!TryCheckHeaderName(name))
            {
                values = null;
                return false;
            }

            if (_headerStore == null)
            {
                values = null;
                return false;
            }

            HeaderStoreItemInfo info = null;
            if (TryGetAndParseHeaderInfo(name, out info))
            {
                values = GetValuesAsStrings(info);
                return true;
            }

            values = null;
            return false;
        }

        public bool Contains(string name)
        {
            CheckHeaderName(name);

            if (_headerStore == null)
            {
                return false;
            }

            // We can't just call headerStore.ContainsKey() since after parsing the value the header may not exist
            // anymore (if the value contains invalid newline chars, we remove the header). So try to parse the 
            // header value.
            HeaderStoreItemInfo info = null;
            return TryGetAndParseHeaderInfo(name, out info);
        }

        public override string ToString()
        {
            // Return all headers as string similar to: 
            // HeaderName1: Value1, Value2
            // HeaderName2: Value1
            // ...
            StringBuilder sb = new StringBuilder();
            foreach (var header in this)
            {
                sb.Append(header.Key);
                sb.Append(": ");
                sb.Append(this.GetHeaderString(header.Key));
                sb.Append("\r\n");
            }

            return sb.ToString();
        }

        internal IEnumerable<KeyValuePair<string, string>> GetHeaderStrings()
        {
            if (_headerStore == null)
            {
                yield break;
            }

            foreach (var header in _headerStore)
            {
                HeaderStoreItemInfo info = header.Value;

                string stringValue = GetHeaderString(info);

                yield return new KeyValuePair<string, string>(header.Key, stringValue);
            }
        }

        internal string GetHeaderString(string headerName)
        {
            return GetHeaderString(headerName, null);
        }

        internal string GetHeaderString(string headerName, object exclude)
        {
            HeaderStoreItemInfo info;
            if (!TryGetHeaderInfo(headerName, out info))
            {
                return string.Empty;
            }

            return GetHeaderString(info, exclude);
        }

        private string GetHeaderString(HeaderStoreItemInfo info)
        {
            return GetHeaderString(info, null);
        }

        private string GetHeaderString(HeaderStoreItemInfo info, object exclude)
        {
            string stringValue = string.Empty; // returned if values.Length == 0

            string[] values = GetValuesAsStrings(info, exclude);

            if (values.Length == 1)
            {
                stringValue = values[0];
            }
            else
            {
                // Note that if we get multiple values for a header that doesn't support multiple values, we'll
                // just separate the values using a comma (default separator).
                string separator = HttpHeaderParser.DefaultSeparator;
                if ((info.Parser != null) && (info.Parser.SupportsMultipleValues))
                {
                    separator = info.Parser.Separator;
                }
                stringValue = string.Join(separator, values);
            }

            return stringValue;
        }

        #region IEnumerable<KeyValuePair<string, IEnumerable<string>>> Members

        public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator()
        {
            if (_headerStore == null)
            {
                yield break;
            }

            List<string> invalidHeaders = null;

            foreach (var header in _headerStore)
            {
                HeaderStoreItemInfo info = header.Value;

                // Make sure we parse all raw values before returning the result. Note that this has to be
                // done before we calculate the array length (next line): A raw value may contain a list of
                // values.
                if (!ParseRawHeaderValues(header.Key, info, false))
                {
                    // We have an invalid header value (contains invalid newline chars). Mark it as "to-be-deleted"
                    // and skip this header.
                    if (invalidHeaders == null)
                    {
                        invalidHeaders = new List<string>();
                    }
                    invalidHeaders.Add(header.Key);
                }
                else
                {
                    string[] values = GetValuesAsStrings(info);
                    yield return new KeyValuePair<string, IEnumerable<string>>(header.Key, values);
                }
            }

            // While we were enumerating headers, we also parsed header values. If during parsing it turned out that
            // the header value was invalid (contains invalid newline chars), remove the header from the store after
            // completing the enumeration.
            if (invalidHeaders != null)
            {
                Debug.Assert(_headerStore != null);
                foreach (string invalidHeader in invalidHeaders)
                {
                    _headerStore.Remove(invalidHeader);
                }
            }
        }

        #endregion

        #region IEnumerable Members

        Collections.IEnumerator Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        internal void SetConfiguration(Dictionary<string, HttpHeaderParser> parserStore,
            HashSet<string> invalidHeaders)
        {
            Debug.Assert(_parserStore == null, "Parser store was already set.");

            _parserStore = parserStore;
            _invalidHeaders = invalidHeaders;
        }

        internal void AddParsedValue(string name, object value)
        {
            Debug.Assert((name != null) && (name.Length > 0));
            Debug.Assert(HttpRuleParser.GetTokenLength(name, 0) == name.Length);
            Debug.Assert(value != null);

            HeaderStoreItemInfo info = GetOrCreateHeaderInfo(name, true);
            Debug.Assert(info.Parser != null, "Can't add parsed value if there is no parser available.");

            // If the current header has only one value, we can't add another value. The strongly typed property
            // must not call AddParsedValue(), but SetParsedValue(). E.g. for headers like 'Date', 'Host'.
            Debug.Assert(info.CanAddValue, "Header '" + name + "' doesn't support multiple values");

            AddValue(info, value, StoreLocation.Parsed);
        }

        internal void SetParsedValue(string name, object value)
        {
            Debug.Assert((name != null) && (name.Length > 0));
            Debug.Assert(HttpRuleParser.GetTokenLength(name, 0) == name.Length);
            Debug.Assert(value != null);

            // This method will first clear all values. This is used e.g. when setting the 'Date' or 'Host' header.
            // I.e. headers not supporting collections.
            HeaderStoreItemInfo info = GetOrCreateHeaderInfo(name, true);
            Debug.Assert(info.Parser != null, "Can't add parsed value if there is no parser available.");

            info.InvalidValue = null;
            info.ParsedValue = null;
            info.RawValue = null;

            AddValue(info, value, StoreLocation.Parsed);
        }

        internal void SetOrRemoveParsedValue(string name, object value)
        {
            if (value == null)
            {
                Remove(name);
            }
            else
            {
                SetParsedValue(name, value);
            }
        }

        internal bool RemoveParsedValue(string name, object value)
        {
            Debug.Assert((name != null) && (name.Length > 0));
            Debug.Assert(HttpRuleParser.GetTokenLength(name, 0) == name.Length);
            Debug.Assert(value != null);

            if (_headerStore == null)
            {
                return false;
            }

            // If we have a value for this header, then verify if we have a single value. If so, compare that
            // value with 'item'. If we have a list of values, then remove 'item' from the list.
            HeaderStoreItemInfo info = null;
            if (TryGetAndParseHeaderInfo(name, out info))
            {
                Debug.Assert(info.Parser != null, "Can't add parsed value if there is no parser available.");
                Debug.Assert(info.Parser.SupportsMultipleValues,
                    "This method should not be used for single-value headers. Use Remove(string) instead.");

                bool result = false;

                // If there is no entry, just return.
                if (info.ParsedValue == null)
                {
                    return false;
                }

                IEqualityComparer comparer = info.Parser.Comparer;

                List<object> parsedValues = info.ParsedValue as List<object>;
                if (parsedValues == null)
                {
                    Debug.Assert(info.ParsedValue.GetType() == value.GetType(),
                        "Stored value does not have the same type as 'value'.");

                    if (AreEqual(value, info.ParsedValue, comparer))
                    {
                        info.ParsedValue = null;
                        result = true;
                    }
                }
                else
                {
                    foreach (object item in parsedValues)
                    {
                        Debug.Assert(item.GetType() == value.GetType(),
                            "One of the stored values does not have the same type as 'value'.");

                        if (AreEqual(value, item, comparer))
                        {
                            // Remove 'item' rather than 'value', since the 'comparer' may consider two values
                            // equal even though the default obj.Equals() may not (e.g. if 'comparer' does
                            // case-insensitive comparison for strings, but string.Equals() is case-sensitive).
                            result = parsedValues.Remove(item);
                            break;
                        }
                    }

                    // If we removed the last item in a list, remove the list.
                    if (parsedValues.Count == 0)
                    {
                        info.ParsedValue = null;
                    }
                }

                // If there is no value for the header left, remove the header.
                if (info.IsEmpty)
                {
                    bool headerRemoved = Remove(name);
                    Debug.Assert(headerRemoved, "Existing header '" + name + "' couldn't be removed.");
                }

                return result;
            }
            return false;
        }

        internal bool ContainsParsedValue(string name, object value)
        {
            Debug.Assert((name != null) && (name.Length > 0));
            Debug.Assert(HttpRuleParser.GetTokenLength(name, 0) == name.Length);
            Debug.Assert(value != null);

            if (_headerStore == null)
            {
                return false;
            }

            // If we have a value for this header, then verify if we have a single value. If so, compare that
            // value with 'item'. If we have a list of values, then compare each item in the list with 'item'.
            HeaderStoreItemInfo info = null;
            if (TryGetAndParseHeaderInfo(name, out info))
            {
                Debug.Assert(info.Parser != null, "Can't add parsed value if there is no parser available.");
                Debug.Assert(info.Parser.SupportsMultipleValues,
                    "This method should not be used for single-value headers. Use equality comparer instead.");

                // If there is no entry, just return.
                if (info.ParsedValue == null)
                {
                    return false;
                }

                List<object> parsedValues = info.ParsedValue as List<object>;

                IEqualityComparer comparer = info.Parser.Comparer;

                if (parsedValues == null)
                {
                    Debug.Assert(info.ParsedValue.GetType() == value.GetType(),
                        "Stored value does not have the same type as 'value'.");

                    return AreEqual(value, info.ParsedValue, comparer);
                }
                else
                {
                    foreach (object item in parsedValues)
                    {
                        Debug.Assert(item.GetType() == value.GetType(),
                            "One of the stored values does not have the same type as 'value'.");

                        if (AreEqual(value, item, comparer))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }

            return false;
        }

        internal virtual void AddHeaders(HttpHeaders sourceHeaders)
        {
            Debug.Assert(sourceHeaders != null);
            Debug.Assert(_parserStore == sourceHeaders._parserStore,
                "Can only copy headers from an instance with the same header parsers.");

            if (sourceHeaders._headerStore == null)
            {
                return;
            }

            List<string> invalidHeaders = null;

            foreach (var header in sourceHeaders._headerStore)
            {
                // Only add header values if they're not already set on the message. Note that we don't merge 
                // collections: If both the default headers and the message have set some values for a certain
                // header, then we don't try to merge the values.
                if ((_headerStore == null) || (!_headerStore.ContainsKey(header.Key)))
                {
                    HeaderStoreItemInfo sourceInfo = header.Value;

                    // If DefaultRequestHeaders values are copied to multiple messages, it is useful to parse these
                    // default header values only once. This is what we're doing here: By parsing raw headers in 
                    // 'sourceHeaders' before copying values to our header store.
                    if (!sourceHeaders.ParseRawHeaderValues(header.Key, sourceInfo, false))
                    {
                        // If after trying to parse source header values no value is left (i.e. all values contain 
                        // invalid newline chars), mark this header as 'to-be-deleted' and skip to the next header.
                        if (invalidHeaders == null)
                        {
                            invalidHeaders = new List<string>();
                        }

                        invalidHeaders.Add(header.Key);
                    }
                    else
                    {
                        AddHeaderInfo(header.Key, sourceInfo);
                    }
                }
            }

            if (invalidHeaders != null)
            {
                Debug.Assert(sourceHeaders._headerStore != null);
                foreach (string invalidHeader in invalidHeaders)
                {
                    sourceHeaders._headerStore.Remove(invalidHeader);
                }
            }
        }

        private void AddHeaderInfo(string headerName, HeaderStoreItemInfo sourceInfo)
        {
            HeaderStoreItemInfo destinationInfo = CreateAndAddHeaderToStore(headerName);
            Debug.Assert(sourceInfo.Parser == destinationInfo.Parser,
                "Expected same parser on both source and destination header store for header '" + headerName +
                "'.");

            // We have custom header values. The parsed values are strings.
            if (destinationInfo.Parser == null)
            {
                Debug.Assert((sourceInfo.RawValue == null) && (sourceInfo.InvalidValue == null),
                    "No raw or invalid values expected for custom headers.");

                // Custom header values are always stored as string or list of strings.
                destinationInfo.ParsedValue = CloneStringHeaderInfoValues(sourceInfo.ParsedValue);
            }
            else
            {
                // We have a parser, so we have to copy invalid values and clone parsed values.

                // Invalid values are always strings. Strings are immutable. So we only have to clone the 
                // collection (if there is one).
                destinationInfo.InvalidValue = CloneStringHeaderInfoValues(sourceInfo.InvalidValue);

                // Now clone and add parsed values (if any).
                if (sourceInfo.ParsedValue != null)
                {
                    List<object> sourceValues = sourceInfo.ParsedValue as List<object>;
                    if (sourceValues == null)
                    {
                        CloneAndAddValue(destinationInfo, sourceInfo.ParsedValue);
                    }
                    else
                    {
                        foreach (object item in sourceValues)
                        {
                            CloneAndAddValue(destinationInfo, item);
                        }
                    }
                }
            }
        }

        private static void CloneAndAddValue(HeaderStoreItemInfo destinationInfo, object source)
        {
            // We only have one value. Clone it and assign it to the store.
            ICloneable cloneableValue = source as ICloneable;

            if (cloneableValue != null)
            {
                AddValue(destinationInfo, cloneableValue.Clone(), StoreLocation.Parsed);
            }
            else
            {
                // If it doesn't implement ICloneable, it's a value type or an immutable type like String/Uri. 
                AddValue(destinationInfo, source, StoreLocation.Parsed);
            }
        }

        private static object CloneStringHeaderInfoValues(object source)
        {
            if (source == null)
            {
                return null;
            }

            List<object> sourceValues = source as List<object>;
            if (sourceValues == null)
            {
                // If we just have one value, return the reference to the string (strings are immutable so it's OK
                // to use the reference).
                return source;
            }
            else
            {
                // If we have a list of strings, create a new list and copy all strings to the new list.
                return new List<object>(sourceValues);
            }
        }

        private HeaderStoreItemInfo GetOrCreateHeaderInfo(string name, bool parseRawValues)
        {
            Debug.Assert((name != null) && (name.Length > 0));
            Debug.Assert(HttpRuleParser.GetTokenLength(name, 0) == name.Length);
            Contract.Ensures(Contract.Result<HeaderStoreItemInfo>() != null);

            HeaderStoreItemInfo result = null;
            bool found = false;
            if (parseRawValues)
            {
                found = TryGetAndParseHeaderInfo(name, out result);
            }
            else
            {
                found = TryGetHeaderInfo(name, out result);
            }

            if (!found)
            {
                result = CreateAndAddHeaderToStore(name);
            }

            return result;
        }

        private HeaderStoreItemInfo CreateAndAddHeaderToStore(string name)
        {
            // If we don't have the header in the store yet, add it now.
            HeaderStoreItemInfo result = new HeaderStoreItemInfo(GetParser(name));

            AddHeaderToStore(name, result);

            return result;
        }

        private void AddHeaderToStore(string name, HeaderStoreItemInfo info)
        {
            if (_headerStore == null)
            {
                _headerStore = new Dictionary<string, HeaderStoreItemInfo>(
                    StringComparer.OrdinalIgnoreCase);
            }
            _headerStore.Add(name, info);
        }

        private bool TryGetHeaderInfo(string name, out HeaderStoreItemInfo info)
        {
            if (_headerStore == null)
            {
                info = null;
                return false;
            }

            return _headerStore.TryGetValue(name, out info);
        }

        private bool TryGetAndParseHeaderInfo(string name, out HeaderStoreItemInfo info)
        {
            if (TryGetHeaderInfo(name, out info))
            {
                return ParseRawHeaderValues(name, info, true);
            }

            return false;
        }

        private bool ParseRawHeaderValues(string name, HeaderStoreItemInfo info, bool removeEmptyHeader)
        {
            // Prevent multiple threads from parsing the raw value at the same time, or else we would get
            // false duplicates or false nulls.
            lock (info)
            {
                // Unlike TryGetHeaderInfo() this method tries to parse all non-validated header values (if any)
                // before returning to the caller.
                if (info.RawValue != null)
                {
                    List<string> rawValues = info.RawValue as List<string>;

                    if (rawValues == null)
                    {
                        ParseSingleRawHeaderValue(name, info);
                    }
                    else
                    {
                        ParseMultipleRawHeaderValues(name, info, rawValues);
                    }

                    // At this point all values are either in info.ParsedValue, info.InvalidValue, or were removed since they
                    // contain invalid newline chars. Reset RawValue.
                    info.RawValue = null;

                    // During parsing, we removed the value since it contains invalid newline chars. Return false to indicate that
                    // this is an empty header. If the caller specified to remove empty headers, we'll remove the header before
                    // returning.
                    if ((info.InvalidValue == null) && (info.ParsedValue == null))
                    {
                        if (removeEmptyHeader)
                        {
                            // After parsing the raw value, no value is left because all values contain invalid newline 
                            // chars.
                            Debug.Assert(_headerStore != null);
                            _headerStore.Remove(name);
                        }
                        return false;
                    }
                }
            }

            return true;
        }

        private static void ParseMultipleRawHeaderValues(string name, HeaderStoreItemInfo info, List<string> rawValues)
        {
            if (info.Parser == null)
            {
                foreach (string rawValue in rawValues)
                {
                    if (!ContainsInvalidNewLine(rawValue, name))
                    {
                        AddValue(info, rawValue, StoreLocation.Parsed);
                    }
                }
            }
            else
            {
                foreach (string rawValue in rawValues)
                {
                    if (!TryParseAndAddRawHeaderValue(name, info, rawValue, true))
                    {
                        if (NetEventSource.IsEnabled) NetEventSource.Log.HeadersInvalidValue(name, rawValue);
                    }
                }
            }
        }

        private static void ParseSingleRawHeaderValue(string name, HeaderStoreItemInfo info)
        {
            string rawValue = info.RawValue as string;
            Debug.Assert(rawValue != null, "RawValue must either be List<string> or string.");

            if (info.Parser == null)
            {
                if (!ContainsInvalidNewLine(rawValue, name))
                {
                    AddValue(info, rawValue, StoreLocation.Parsed);
                }
            }
            else
            {
                if (!TryParseAndAddRawHeaderValue(name, info, rawValue, true))
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Log.HeadersInvalidValue(name, rawValue);
                }
            }
        }

        // See Add(name, string)
        internal bool TryParseAndAddValue(string name, string value)
        {
            // We don't use GetOrCreateHeaderInfo() here, since this would create a new header in the store. If parsing
            // the value then throws, we would have to remove the header from the store again. So just get a 
            // HeaderStoreItemInfo object and try to parse the value. If it works, we'll add the header.
            HeaderStoreItemInfo info;
            bool addToStore;
            PrepareHeaderInfoForAdd(name, out info, out addToStore);

            bool result = TryParseAndAddRawHeaderValue(name, info, value, false);

            if (result && addToStore && (info.ParsedValue != null))
            {
                // If we get here, then the value could be parsed correctly. If we created a new HeaderStoreItemInfo, add
                // it to the store if we added at least one value.
                AddHeaderToStore(name, info);
            }

            return result;
        }

        // See ParseAndAddValue
        private static bool TryParseAndAddRawHeaderValue(string name, HeaderStoreItemInfo info, string value, bool addWhenInvalid)
        {
            Debug.Assert(info != null);
            Debug.Assert(info.Parser != null);

            // Values are added as 'invalid' if we either can't parse the value OR if we already have a value
            // and the current header doesn't support multiple values: e.g. trying to add a date/time value
            // to the 'Date' header if we already have a date/time value will result in the second value being
            // added to the 'invalid' header values.
            if (!info.CanAddValue)
            {
                if (addWhenInvalid)
                {
                    AddValue(info, value ?? string.Empty, StoreLocation.Invalid);
                }
                return false;
            }

            int index = 0;
            object parsedValue = null;

            if (info.Parser.TryParseValue(value, info.ParsedValue, ref index, out parsedValue))
            {
                // The raw string only represented one value (which was successfully parsed). Add the value and return.
                if ((value == null) || (index == value.Length))
                {
                    if (parsedValue != null)
                    {
                        AddValue(info, parsedValue, StoreLocation.Parsed);
                    }
                    return true;
                }
                Debug.Assert(index < value.Length, "Parser must return an index value within the string length.");

                // If we successfully parsed a value, but there are more left to read, store the results in a temp
                // list. Only when all values are parsed successfully write the list to the store.
                List<object> parsedValues = new List<object>();
                if (parsedValue != null)
                {
                    parsedValues.Add(parsedValue);
                }

                while (index < value.Length)
                {
                    if (info.Parser.TryParseValue(value, info.ParsedValue, ref index, out parsedValue))
                    {
                        if (parsedValue != null)
                        {
                            parsedValues.Add(parsedValue);
                        }
                    }
                    else
                    {
                        if (!ContainsInvalidNewLine(value, name) && addWhenInvalid)
                        {
                            AddValue(info, value, StoreLocation.Invalid);
                        }
                        return false;
                    }
                }

                // All values were parsed correctly. Copy results to the store.
                foreach (object item in parsedValues)
                {
                    AddValue(info, item, StoreLocation.Parsed);
                }
                return true;
            }

            if (!ContainsInvalidNewLine(value, name) && addWhenInvalid)
            {
                AddValue(info, value ?? string.Empty, StoreLocation.Invalid);
            }
            return false;
        }

        private static void AddValue(HeaderStoreItemInfo info, object value, StoreLocation location)
        {
            // Since we have the same pattern for all three store locations (raw, invalid, parsed), we use
            // this helper method to deal with adding values:
            // - if 'null' just set the store property to 'value'
            // - if 'List<T>' append 'value' to the end of the list
            // - if 'T', i.e. we have already a value stored (but no list), create a list, add the stored value
            //   to the list and append 'value' at the end of the newly created list.

            Debug.Assert((info.Parser != null) || ((info.Parser == null) && (value.GetType() == typeof(string))),
                "If no parser is defined, then the value must be string.");

            object currentStoreValue = null;
            switch (location)
            {
                case StoreLocation.Raw:
                    currentStoreValue = info.RawValue;
                    AddValueToStoreValue<string>(info, value, ref currentStoreValue);
                    info.RawValue = currentStoreValue;
                    break;

                case StoreLocation.Invalid:
                    currentStoreValue = info.InvalidValue;
                    AddValueToStoreValue<string>(info, value, ref currentStoreValue);
                    info.InvalidValue = currentStoreValue;
                    break;

                case StoreLocation.Parsed:
                    Debug.Assert((value == null) || (!(value is List<object>)),
                        "Header value types must not derive from List<object> since this type is used internally to store " +
                        "lists of values. So we would not be able to distinguish between a single value and a list of values.");
                    currentStoreValue = info.ParsedValue;
                    AddValueToStoreValue<object>(info, value, ref currentStoreValue);
                    info.ParsedValue = currentStoreValue;
                    break;

                default:
                    Debug.Assert(false, "Unknown StoreLocation value: " + location.ToString());
                    break;
            }
        }

        private static void AddValueToStoreValue<T>(HeaderStoreItemInfo info, object value,
            ref object currentStoreValue) where T : class
        {
            // If there is no value set yet, then add current item as value (we don't create a list
            // if not required). If 'info.Value' is already assigned then make sure 'info.Value' is a
            // List<T> and append 'item' to the list.
            if (currentStoreValue == null)
            {
                currentStoreValue = value;
            }
            else
            {
                List<T> storeValues = currentStoreValue as List<T>;

                if (storeValues == null)
                {
                    storeValues = new List<T>(2);
                    Debug.Assert(value is T);
                    storeValues.Add(currentStoreValue as T);
                    currentStoreValue = storeValues;
                }
                Debug.Assert(value is T);
                storeValues.Add(value as T);
            }
        }

        // Since most of the time we just have 1 value, we don't create a List<object> for one value, but we change
        // the return type to 'object'. The caller has to deal with the return type (object vs. List<object>). This 
        // is to optimize the most common scenario where a header has only one value.
        internal object GetParsedValues(string name)
        {
            Debug.Assert((name != null) && (name.Length > 0));
            Debug.Assert(HttpRuleParser.GetTokenLength(name, 0) == name.Length);

            HeaderStoreItemInfo info = null;

            if (!TryGetAndParseHeaderInfo(name, out info))
            {
                return null;
            }

            return info.ParsedValue;
        }

        private void PrepareHeaderInfoForAdd(string name, out HeaderStoreItemInfo info, out bool addToStore)
        {
            info = null;
            addToStore = false;
            if (!TryGetAndParseHeaderInfo(name, out info))
            {
                info = new HeaderStoreItemInfo(GetParser(name));
                addToStore = true;
            }
        }

        private void ParseAndAddValue(string name, HeaderStoreItemInfo info, string value)
        {
            Debug.Assert(info != null);

            if (info.Parser == null)
            {
                // If we don't have a parser for the header, we consider the value valid if it doesn't contains 
                // invalid newline characters. We add the values as "parsed value". Note that we allow empty values.
                CheckInvalidNewLine(value);
                AddValue(info, value ?? string.Empty, StoreLocation.Parsed);
                return;
            }

            // If the header only supports 1 value, we can add the current value only if there is no
            // value already set.
            if (!info.CanAddValue)
            {
                throw new FormatException(string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_single_value_header, name));
            }

            int index = 0;
            object parsedValue = info.Parser.ParseValue(value, info.ParsedValue, ref index);

            // The raw string only represented one value (which was successfully parsed). Add the value and return.
            // If value is null we still have to first call ParseValue() to allow the parser to decide whether null is
            // a valid value. If it is (i.e. no exception thrown), we set the parsed value (if any) and return.
            if ((value == null) || (index == value.Length))
            {
                // If the returned value is null, then it means the header accepts empty values. I.e. we don't throw
                // but we don't add 'null' to the store either.
                if (parsedValue != null)
                {
                    AddValue(info, parsedValue, StoreLocation.Parsed);
                }
                return;
            }
            Debug.Assert(index < value.Length, "Parser must return an index value within the string length.");

            // If we successfully parsed a value, but there are more left to read, store the results in a temp
            // list. Only when all values are parsed successfully write the list to the store.
            List<object> parsedValues = new List<object>();
            if (parsedValue != null)
            {
                parsedValues.Add(parsedValue);
            }

            while (index < value.Length)
            {
                parsedValue = info.Parser.ParseValue(value, info.ParsedValue, ref index);
                if (parsedValue != null)
                {
                    parsedValues.Add(parsedValue);
                }
            }

            // All values were parsed correctly. Copy results to the store.
            foreach (object item in parsedValues)
            {
                AddValue(info, item, StoreLocation.Parsed);
            }
        }

        private HttpHeaderParser GetParser(string name)
        {
            if (_parserStore == null)
            {
                return null;
            }

            HttpHeaderParser parser = null;
            if (_parserStore.TryGetValue(name, out parser))
            {
                return parser;
            }

            return null;
        }

        private void CheckHeaderName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(SR.net_http_argument_empty_string, nameof(name));
            }

            if (HttpRuleParser.GetTokenLength(name, 0) != name.Length)
            {
                throw new FormatException(SR.net_http_headers_invalid_header_name);
            }

            if ((_invalidHeaders != null) && (_invalidHeaders.Contains(name)))
            {
                throw new InvalidOperationException(SR.net_http_headers_not_allowed_header_name);
            }
        }

        private bool TryCheckHeaderName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (HttpRuleParser.GetTokenLength(name, 0) != name.Length)
            {
                return false;
            }

            if ((_invalidHeaders != null) && (_invalidHeaders.Contains(name)))
            {
                return false;
            }

            return true;
        }

        private static void CheckInvalidNewLine(string value)
        {
            if (value == null)
            {
                return;
            }

            if (HttpRuleParser.ContainsInvalidNewLine(value))
            {
                throw new FormatException(SR.net_http_headers_no_newlines);
            }
        }

        private static bool ContainsInvalidNewLine(string value, string name)
        {
            if (HttpRuleParser.ContainsInvalidNewLine(value))
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, SR.Format(SR.net_http_log_headers_no_newlines, name, value));
                return true;
            }
            return false;
        }

        private static string[] GetValuesAsStrings(HeaderStoreItemInfo info)
        {
            return GetValuesAsStrings(info, null);
        }

        // When doing exclusion comparison, assume raw values have been parsed.
        private static string[] GetValuesAsStrings(HeaderStoreItemInfo info, object exclude)
        {
            Contract.Ensures(Contract.Result<string[]>() != null);

            int length = GetValueCount(info);
            string[] values = new string[length];

            if (length > 0)
            {
                int currentIndex = 0;

                ReadStoreValues<string>(values, info.RawValue, null, null, ref currentIndex);
                ReadStoreValues<object>(values, info.ParsedValue, info.Parser, exclude, ref currentIndex);

                // Set parser parameter to 'null' for invalid values: The invalid values is always a string so we 
                // don't need the parser to "serialize" the value to a string.
                ReadStoreValues<string>(values, info.InvalidValue, null, null, ref currentIndex);

                // The values array may not be full because some values were excluded
                if (currentIndex < length)
                {
                    string[] trimmedValues = new string[currentIndex];
                    Array.Copy(values, 0, trimmedValues, 0, currentIndex);
                    values = trimmedValues;
                }
            }
            return values;
        }

        private static int GetValueCount(HeaderStoreItemInfo info)
        {
            Debug.Assert(info != null);

            int valueCount = 0;
            UpdateValueCount<string>(info.RawValue, ref valueCount);
            UpdateValueCount<string>(info.InvalidValue, ref valueCount);
            UpdateValueCount<object>(info.ParsedValue, ref valueCount);

            return valueCount;
        }

        private static void UpdateValueCount<T>(object valueStore, ref int valueCount)
        {
            if (valueStore == null)
            {
                return;
            }

            List<T> values = valueStore as List<T>;
            if (values != null)
            {
                valueCount += values.Count;
            }
            else
            {
                valueCount++;
            }
        }

        private static void ReadStoreValues<T>(string[] values, object storeValue, HttpHeaderParser parser,
            T exclude, ref int currentIndex)
        {
            Debug.Assert(values != null);

            if (storeValue != null)
            {
                List<T> storeValues = storeValue as List<T>;

                if (storeValues == null)
                {
                    if (ShouldAdd<T>(storeValue, parser, exclude))
                    {
                        values[currentIndex] = parser == null ? storeValue.ToString() : parser.ToString(storeValue);
                        currentIndex++;
                    }
                }
                else
                {
                    foreach (object item in storeValues)
                    {
                        if (ShouldAdd<T>(item, parser, exclude))
                        {
                            values[currentIndex] = parser == null ? item.ToString() : parser.ToString(item);
                            currentIndex++;
                        }
                    }
                }
            }
        }

        private static bool ShouldAdd<T>(object storeValue, HttpHeaderParser parser, T exclude)
        {
            bool add = true;
            if (parser != null && exclude != null)
            {
                if (parser.Comparer != null)
                {
                    add = !parser.Comparer.Equals(exclude, storeValue);
                }
                else
                {
                    add = !exclude.Equals(storeValue);
                }
            }
            return add;
        }

        private bool AreEqual(object value, object storeValue, IEqualityComparer comparer)
        {
            Debug.Assert(value != null);

            if (comparer != null)
            {
                return comparer.Equals(value, storeValue);
            }

            // We don't have a comparer, so use the Equals() method.
            return value.Equals(storeValue);
        }

        #region Private Classes

        private class HeaderStoreItemInfo
        {
            private object _rawValue;
            private object _invalidValue;
            private object _parsedValue;
            private HttpHeaderParser _parser;

            internal object RawValue
            {
                get { return _rawValue; }
                set { _rawValue = value; }
            }

            internal object InvalidValue
            {
                get { return _invalidValue; }
                set { _invalidValue = value; }
            }

            internal object ParsedValue
            {
                get { return _parsedValue; }
                set { _parsedValue = value; }
            }

            internal HttpHeaderParser Parser
            {
                get { return _parser; }
            }

            internal bool CanAddValue
            {
                get
                {
                    Debug.Assert(_parser != null,
                        "There should be no reason to call CanAddValue if there is no parser for the current header.");

                    // If the header only supports one value, and we have already a value set, then we can't add
                    // another value. E.g. the 'Date' header only supports one value. We can't add multiple timestamps
                    // to 'Date'.
                    // So if this is a known header, ask the parser if it supports multiple values and check whether
                    // we already have a (valid or invalid) value.
                    // Note that we ignore the rawValue by purpose: E.g. we are parsing 2 raw values for a header only 
                    // supporting 1 value. When the first value gets parsed, CanAddValue returns true and we add the
                    // parsed value to ParsedValue. When the second value is parsed, CanAddValue returns false, because
                    // we have already a parsed value. 
                    return ((_parser.SupportsMultipleValues) || ((_invalidValue == null) && (_parsedValue == null)));
                }
            }

            internal bool IsEmpty
            {
                get { return ((_rawValue == null) && (_invalidValue == null) && (_parsedValue == null)); }
            }

            internal HeaderStoreItemInfo(HttpHeaderParser parser)
            {
                // Can be null.
                _parser = parser;
            }
        }
        #endregion
    }
}

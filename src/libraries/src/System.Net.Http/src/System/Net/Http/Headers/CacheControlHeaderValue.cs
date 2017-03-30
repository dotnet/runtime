// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Net.Http.Headers
{
    public class CacheControlHeaderValue : ICloneable
    {
        private const string maxAgeString = "max-age";
        private const string maxStaleString = "max-stale";
        private const string minFreshString = "min-fresh";
        private const string mustRevalidateString = "must-revalidate";
        private const string noCacheString = "no-cache";
        private const string noStoreString = "no-store";
        private const string noTransformString = "no-transform";
        private const string onlyIfCachedString = "only-if-cached";
        private const string privateString = "private";
        private const string proxyRevalidateString = "proxy-revalidate";
        private const string publicString = "public";
        private const string sharedMaxAgeString = "s-maxage";

        private static readonly HttpHeaderParser s_nameValueListParser = GenericHeaderParser.MultipleValueNameValueParser;
        private static readonly Action<string> s_checkIsValidToken = CheckIsValidToken;

        private bool _noCache;
        private ObjectCollection<string> _noCacheHeaders;
        private bool _noStore;
        private TimeSpan? _maxAge;
        private TimeSpan? _sharedMaxAge;
        private bool _maxStale;
        private TimeSpan? _maxStaleLimit;
        private TimeSpan? _minFresh;
        private bool _noTransform;
        private bool _onlyIfCached;
        private bool _publicField;
        private bool _privateField;
        private ObjectCollection<string> _privateHeaders;
        private bool _mustRevalidate;
        private bool _proxyRevalidate;
        private ObjectCollection<NameValueHeaderValue> _extensions;

        public bool NoCache
        {
            get { return _noCache; }
            set { _noCache = value; }
        }

        public ICollection<string> NoCacheHeaders
        {
            get
            {
                if (_noCacheHeaders == null)
                {
                    _noCacheHeaders = new ObjectCollection<string>(s_checkIsValidToken);
                }
                return _noCacheHeaders;
            }
        }

        public bool NoStore
        {
            get { return _noStore; }
            set { _noStore = value; }
        }

        public TimeSpan? MaxAge
        {
            get { return _maxAge; }
            set { _maxAge = value; }
        }

        public TimeSpan? SharedMaxAge
        {
            get { return _sharedMaxAge; }
            set { _sharedMaxAge = value; }
        }

        public bool MaxStale
        {
            get { return _maxStale; }
            set { _maxStale = value; }
        }

        public TimeSpan? MaxStaleLimit
        {
            get { return _maxStaleLimit; }
            set { _maxStaleLimit = value; }
        }

        public TimeSpan? MinFresh
        {
            get { return _minFresh; }
            set { _minFresh = value; }
        }

        public bool NoTransform
        {
            get { return _noTransform; }
            set { _noTransform = value; }
        }

        public bool OnlyIfCached
        {
            get { return _onlyIfCached; }
            set { _onlyIfCached = value; }
        }

        public bool Public
        {
            get { return _publicField; }
            set { _publicField = value; }
        }

        public bool Private
        {
            get { return _privateField; }
            set { _privateField = value; }
        }

        public ICollection<string> PrivateHeaders
        {
            get
            {
                if (_privateHeaders == null)
                {
                    _privateHeaders = new ObjectCollection<string>(s_checkIsValidToken);
                }
                return _privateHeaders;
            }
        }

        public bool MustRevalidate
        {
            get { return _mustRevalidate; }
            set { _mustRevalidate = value; }
        }

        public bool ProxyRevalidate
        {
            get { return _proxyRevalidate; }
            set { _proxyRevalidate = value; }
        }

        public ICollection<NameValueHeaderValue> Extensions
        {
            get
            {
                if (_extensions == null)
                {
                    _extensions = new ObjectCollection<NameValueHeaderValue>();
                }
                return _extensions;
            }
        }

        public CacheControlHeaderValue()
        {
        }

        private CacheControlHeaderValue(CacheControlHeaderValue source)
        {
            Debug.Assert(source != null);

            _noCache = source._noCache;
            _noStore = source._noStore;
            _maxAge = source._maxAge;
            _sharedMaxAge = source._sharedMaxAge;
            _maxStale = source._maxStale;
            _maxStaleLimit = source._maxStaleLimit;
            _minFresh = source._minFresh;
            _noTransform = source._noTransform;
            _onlyIfCached = source._onlyIfCached;
            _publicField = source._publicField;
            _privateField = source._privateField;
            _mustRevalidate = source._mustRevalidate;
            _proxyRevalidate = source._proxyRevalidate;

            if (source._noCacheHeaders != null)
            {
                foreach (var noCacheHeader in source._noCacheHeaders)
                {
                    NoCacheHeaders.Add(noCacheHeader);
                }
            }

            if (source._privateHeaders != null)
            {
                foreach (var privateHeader in source._privateHeaders)
                {
                    PrivateHeaders.Add(privateHeader);
                }
            }

            if (source._extensions != null)
            {
                foreach (var extension in source._extensions)
                {
                    Extensions.Add((NameValueHeaderValue)((ICloneable)extension).Clone());
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = StringBuilderCache.Acquire();

            AppendValueIfRequired(sb, _noStore, noStoreString);
            AppendValueIfRequired(sb, _noTransform, noTransformString);
            AppendValueIfRequired(sb, _onlyIfCached, onlyIfCachedString);
            AppendValueIfRequired(sb, _publicField, publicString);
            AppendValueIfRequired(sb, _mustRevalidate, mustRevalidateString);
            AppendValueIfRequired(sb, _proxyRevalidate, proxyRevalidateString);

            if (_noCache)
            {
                AppendValueWithSeparatorIfRequired(sb, noCacheString);
                if ((_noCacheHeaders != null) && (_noCacheHeaders.Count > 0))
                {
                    sb.Append("=\"");
                    AppendValues(sb, _noCacheHeaders);
                    sb.Append('\"');
                }
            }

            if (_maxAge.HasValue)
            {
                AppendValueWithSeparatorIfRequired(sb, maxAgeString);
                sb.Append('=');
                sb.Append(((int)_maxAge.Value.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo));
            }

            if (_sharedMaxAge.HasValue)
            {
                AppendValueWithSeparatorIfRequired(sb, sharedMaxAgeString);
                sb.Append('=');
                sb.Append(((int)_sharedMaxAge.Value.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo));
            }

            if (_maxStale)
            {
                AppendValueWithSeparatorIfRequired(sb, maxStaleString);
                if (_maxStaleLimit.HasValue)
                {
                    sb.Append('=');
                    sb.Append(((int)_maxStaleLimit.Value.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo));
                }
            }

            if (_minFresh.HasValue)
            {
                AppendValueWithSeparatorIfRequired(sb, minFreshString);
                sb.Append('=');
                sb.Append(((int)_minFresh.Value.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo));
            }

            if (_privateField)
            {
                AppendValueWithSeparatorIfRequired(sb, privateString);
                if ((_privateHeaders != null) && (_privateHeaders.Count > 0))
                {
                    sb.Append("=\"");
                    AppendValues(sb, _privateHeaders);
                    sb.Append('\"');
                }
            }

            NameValueHeaderValue.ToString(_extensions, ',', false, sb);

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public override bool Equals(object obj)
        {
            CacheControlHeaderValue other = obj as CacheControlHeaderValue;

            if (other == null)
            {
                return false;
            }

            if ((_noCache != other._noCache) || (_noStore != other._noStore) || (_maxAge != other._maxAge) ||
                (_sharedMaxAge != other._sharedMaxAge) || (_maxStale != other._maxStale) ||
                (_maxStaleLimit != other._maxStaleLimit) || (_minFresh != other._minFresh) ||
                (_noTransform != other._noTransform) || (_onlyIfCached != other._onlyIfCached) ||
                (_publicField != other._publicField) || (_privateField != other._privateField) ||
                (_mustRevalidate != other._mustRevalidate) || (_proxyRevalidate != other._proxyRevalidate))
            {
                return false;
            }

            if (!HeaderUtilities.AreEqualCollections(_noCacheHeaders, other._noCacheHeaders,
                StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!HeaderUtilities.AreEqualCollections(_privateHeaders, other._privateHeaders,
                StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!HeaderUtilities.AreEqualCollections(_extensions, other._extensions))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            // Use a different bit for bool fields: bool.GetHashCode() will return 0 (false) or 1 (true). So we would
            // end up having the same hash code for e.g. two instances where one has only noCache set and the other
            // only noStore.
            int result = _noCache.GetHashCode() ^ (_noStore.GetHashCode() << 1) ^ (_maxStale.GetHashCode() << 2) ^
                (_noTransform.GetHashCode() << 3) ^ (_onlyIfCached.GetHashCode() << 4) ^
                (_publicField.GetHashCode() << 5) ^ (_privateField.GetHashCode() << 6) ^
                (_mustRevalidate.GetHashCode() << 7) ^ (_proxyRevalidate.GetHashCode() << 8);

            // XOR the hashcode of timespan values with different numbers to make sure two instances with the same
            // timespan set on different fields result in different hashcodes.
            result = result ^ (_maxAge.HasValue ? _maxAge.Value.GetHashCode() ^ 1 : 0) ^
                (_sharedMaxAge.HasValue ? _sharedMaxAge.Value.GetHashCode() ^ 2 : 0) ^
                (_maxStaleLimit.HasValue ? _maxStaleLimit.Value.GetHashCode() ^ 4 : 0) ^
                (_minFresh.HasValue ? _minFresh.Value.GetHashCode() ^ 8 : 0);

            if ((_noCacheHeaders != null) && (_noCacheHeaders.Count > 0))
            {
                foreach (var noCacheHeader in _noCacheHeaders)
                {
                    result = result ^ StringComparer.OrdinalIgnoreCase.GetHashCode(noCacheHeader);
                }
            }

            if ((_privateHeaders != null) && (_privateHeaders.Count > 0))
            {
                foreach (var privateHeader in _privateHeaders)
                {
                    result = result ^ StringComparer.OrdinalIgnoreCase.GetHashCode(privateHeader);
                }
            }

            if ((_extensions != null) && (_extensions.Count > 0))
            {
                foreach (var extension in _extensions)
                {
                    result = result ^ extension.GetHashCode();
                }
            }

            return result;
        }

        public static CacheControlHeaderValue Parse(string input)
        {
            int index = 0;
            return (CacheControlHeaderValue)CacheControlHeaderParser.Parser.ParseValue(input, null, ref index);
        }

        public static bool TryParse(string input, out CacheControlHeaderValue parsedValue)
        {
            int index = 0;
            object output;
            parsedValue = null;

            if (CacheControlHeaderParser.Parser.TryParseValue(input, null, ref index, out output))
            {
                parsedValue = (CacheControlHeaderValue)output;
                return true;
            }
            return false;
        }

        internal static int GetCacheControlLength(string input, int startIndex, CacheControlHeaderValue storeValue,
            out CacheControlHeaderValue parsedValue)
        {
            Debug.Assert(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Cache-Control header consists of a list of name/value pairs, where the value is optional. So use an
            // instance of NameValueHeaderParser to parse the string.
            int current = startIndex;
            object nameValue = null;
            List<NameValueHeaderValue> nameValueList = new List<NameValueHeaderValue>();
            while (current < input.Length)
            {
                if (!s_nameValueListParser.TryParseValue(input, null, ref current, out nameValue))
                {
                    return 0;
                }

                nameValueList.Add(nameValue as NameValueHeaderValue);
            }

            // If we get here, we were able to successfully parse the string as list of name/value pairs. Now analyze
            // the name/value pairs.

            // Cache-Control is a header supporting lists of values. However, expose the header as an instance of
            // CacheControlHeaderValue. So if we already have an instance of CacheControlHeaderValue, add the values
            // from this string to the existing instances. 
            CacheControlHeaderValue result = storeValue;
            if (result == null)
            {
                result = new CacheControlHeaderValue();
            }

            if (!TrySetCacheControlValues(result, nameValueList))
            {
                return 0;
            }

            // If we had an existing store value and we just updated that instance, return 'null' to indicate that 
            // we don't have a new instance of CacheControlHeaderValue, but just updated an existing one. This is the
            // case if we have multiple 'Cache-Control' headers set in a request/response message.
            if (storeValue == null)
            {
                parsedValue = result;
            }

            // If we get here we successfully parsed the whole string.
            return input.Length - startIndex;
        }

        private static bool TrySetCacheControlValues(CacheControlHeaderValue cc,
            List<NameValueHeaderValue> nameValueList)
        {
            foreach (NameValueHeaderValue nameValue in nameValueList)
            {
                bool success = true;
                string name = nameValue.Name.ToLowerInvariant();

                switch (name)
                {
                    case noCacheString:
                        success = TrySetOptionalTokenList(nameValue, ref cc._noCache, ref cc._noCacheHeaders);
                        break;

                    case noStoreString:
                        success = TrySetTokenOnlyValue(nameValue, ref cc._noStore);
                        break;

                    case maxAgeString:
                        success = TrySetTimeSpan(nameValue, ref cc._maxAge);
                        break;

                    case maxStaleString:
                        success = ((nameValue.Value == null) || TrySetTimeSpan(nameValue, ref cc._maxStaleLimit));
                        if (success)
                        {
                            cc._maxStale = true;
                        }
                        break;

                    case minFreshString:
                        success = TrySetTimeSpan(nameValue, ref cc._minFresh);
                        break;

                    case noTransformString:
                        success = TrySetTokenOnlyValue(nameValue, ref cc._noTransform);
                        break;

                    case onlyIfCachedString:
                        success = TrySetTokenOnlyValue(nameValue, ref cc._onlyIfCached);
                        break;

                    case publicString:
                        success = TrySetTokenOnlyValue(nameValue, ref cc._publicField);
                        break;

                    case privateString:
                        success = TrySetOptionalTokenList(nameValue, ref cc._privateField, ref cc._privateHeaders);
                        break;

                    case mustRevalidateString:
                        success = TrySetTokenOnlyValue(nameValue, ref cc._mustRevalidate);
                        break;

                    case proxyRevalidateString:
                        success = TrySetTokenOnlyValue(nameValue, ref cc._proxyRevalidate);
                        break;

                    case sharedMaxAgeString:
                        success = TrySetTimeSpan(nameValue, ref cc._sharedMaxAge);
                        break;

                    default:
                        cc.Extensions.Add(nameValue); // success is always true
                        break;
                }

                if (!success)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TrySetTokenOnlyValue(NameValueHeaderValue nameValue, ref bool boolField)
        {
            if (nameValue.Value != null)
            {
                return false;
            }

            boolField = true;
            return true;
        }

        private static bool TrySetOptionalTokenList(NameValueHeaderValue nameValue, ref bool boolField,
            ref ObjectCollection<string> destination)
        {
            Debug.Assert(nameValue != null);

            if (nameValue.Value == null)
            {
                boolField = true;
                return true;
            }

            // We need the string to be at least 3 chars long: 2x quotes and at least 1 character. Also make sure we
            // have a quoted string. Note that NameValueHeaderValue will never have leading/trailing whitespace.
            string valueString = nameValue.Value;
            if ((valueString.Length < 3) || (valueString[0] != '\"') || (valueString[valueString.Length - 1] != '\"'))
            {
                return false;
            }

            // We have a quoted string. Now verify that the string contains a list of valid tokens separated by ','.
            int current = 1; // skip the initial '"' character.
            int maxLength = valueString.Length - 1; // -1 because we don't want to parse the final '"'.
            bool separatorFound = false;
            int originalValueCount = destination == null ? 0 : destination.Count;
            while (current < maxLength)
            {
                current = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(valueString, current, true,
                    out separatorFound);

                if (current == maxLength)
                {
                    break;
                }

                int tokenLength = HttpRuleParser.GetTokenLength(valueString, current);

                if (tokenLength == 0)
                {
                    // We already skipped whitespace and separators. If we don't have a token it must be an invalid
                    // character.
                    return false;
                }

                if (destination == null)
                {
                    destination = new ObjectCollection<string>(s_checkIsValidToken);
                }

                destination.Add(valueString.Substring(current, tokenLength));

                current = current + tokenLength;
            }

            // After parsing a valid token list, we expect to have at least one value
            if ((destination != null) && (destination.Count > originalValueCount))
            {
                boolField = true;
                return true;
            }

            return false;
        }

        private static bool TrySetTimeSpan(NameValueHeaderValue nameValue, ref TimeSpan? timeSpan)
        {
            Debug.Assert(nameValue != null);

            if (nameValue.Value == null)
            {
                return false;
            }

            int seconds;
            if (!HeaderUtilities.TryParseInt32(nameValue.Value, out seconds))
            {
                return false;
            }

            timeSpan = new TimeSpan(0, 0, seconds);

            return true;
        }

        private static void AppendValueIfRequired(StringBuilder sb, bool appendValue, string value)
        {
            if (appendValue)
            {
                AppendValueWithSeparatorIfRequired(sb, value);
            }
        }

        private static void AppendValueWithSeparatorIfRequired(StringBuilder sb, string value)
        {
            if (sb.Length > 0)
            {
                sb.Append(", ");
            }
            sb.Append(value);
        }

        private static void AppendValues(StringBuilder sb, ObjectCollection<string> values)
        {
            bool first = true;
            foreach (string value in values)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }

                sb.Append(value);
            }
        }

        private static void CheckIsValidToken(string item)
        {
            HeaderUtilities.CheckValidToken(item, nameof(item));
        }

        object ICloneable.Clone()
        {
            return new CacheControlHeaderValue(this);
        }
    }
}

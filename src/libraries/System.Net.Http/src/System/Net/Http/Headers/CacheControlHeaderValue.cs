// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

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

        private static readonly GenericHeaderParser s_nameValueListParser = GenericHeaderParser.MultipleValueNameValueParser;

        [Flags]
        private enum Flags : int
        {
            None = 0,
            MaxAgeHasValue = 1 << 0,
            SharedMaxAgeHasValue = 1 << 1,
            MaxStaleLimitHasValue = 1 << 2,
            MinFreshHasValue = 1 << 3,
            NoCache = 1 << 4,
            NoStore = 1 << 5,
            MaxStale = 1 << 6,
            NoTransform = 1 << 7,
            OnlyIfCached = 1 << 8,
            Public = 1 << 9,
            Private = 1 << 10,
            MustRevalidate = 1 << 11,
            ProxyRevalidate = 1 << 12,
        }

        private Flags _flags;
        private TokenObjectCollection? _noCacheHeaders;
        private TimeSpan _maxAge;
        private TimeSpan _sharedMaxAge;
        private TimeSpan _maxStaleLimit;
        private TimeSpan _minFresh;
        private TokenObjectCollection? _privateHeaders;
        private UnvalidatedObjectCollection<NameValueHeaderValue>? _extensions;

        private void SetTimeSpan(ref TimeSpan fieldRef, Flags flag, TimeSpan? value)
        {
            fieldRef = value.GetValueOrDefault();
            SetFlag(flag, value.HasValue);
        }

        private void SetFlag(Flags flag, bool value)
        {
            Debug.Assert(sizeof(Flags) == sizeof(int));

            // This type is not thread-safe, but we do a minimal amount of synchronization to ensure
            // that concurrent modifications of different properties don't interfere with each other.
            if (value)
            {
                Interlocked.Or(ref Unsafe.As<Flags, int>(ref _flags), (int)flag);
            }
            else
            {
                Interlocked.And(ref Unsafe.As<Flags, int>(ref _flags), (int)~flag);
            }
        }

        public bool NoCache
        {
            get => (_flags & Flags.NoCache) != 0;
            set => SetFlag(Flags.NoCache, value);
        }

        public ICollection<string> NoCacheHeaders => _noCacheHeaders ??= new TokenObjectCollection();

        public bool NoStore
        {
            get => (_flags & Flags.NoStore) != 0;
            set => SetFlag(Flags.NoStore, value);
        }

        public TimeSpan? MaxAge
        {
            get => (_flags & Flags.MaxAgeHasValue) == 0 ? null : _maxAge;
            set => SetTimeSpan(ref _maxAge, Flags.MaxAgeHasValue, value);
        }

        public TimeSpan? SharedMaxAge
        {
            get => (_flags & Flags.SharedMaxAgeHasValue) == 0 ? null : _sharedMaxAge;
            set => SetTimeSpan(ref _sharedMaxAge, Flags.SharedMaxAgeHasValue, value);
        }

        public bool MaxStale
        {
            get => (_flags & Flags.MaxStale) != 0;
            set => SetFlag(Flags.MaxStale, value);
        }

        public TimeSpan? MaxStaleLimit
        {
            get => (_flags & Flags.MaxStaleLimitHasValue) == 0 ? null : _maxStaleLimit;
            set => SetTimeSpan(ref _maxStaleLimit, Flags.MaxStaleLimitHasValue, value);
        }

        public TimeSpan? MinFresh
        {
            get => (_flags & Flags.MinFreshHasValue) == 0 ? null : _minFresh;
            set => SetTimeSpan(ref _minFresh, Flags.MinFreshHasValue, value);
        }

        public bool NoTransform
        {
            get => (_flags & Flags.NoTransform) != 0;
            set => SetFlag(Flags.NoTransform, value);
        }

        public bool OnlyIfCached
        {
            get => (_flags & Flags.OnlyIfCached) != 0;
            set => SetFlag(Flags.OnlyIfCached, value);
        }

        public bool Public
        {
            get => (_flags & Flags.Public) != 0;
            set => SetFlag(Flags.Public, value);
        }

        public bool Private
        {
            get => (_flags & Flags.Private) != 0;
            set => SetFlag(Flags.Private, value);
        }

        public ICollection<string> PrivateHeaders => _privateHeaders ??= new TokenObjectCollection();

        public bool MustRevalidate
        {
            get => (_flags & Flags.MustRevalidate) != 0;
            set => SetFlag(Flags.MustRevalidate, value);
        }

        public bool ProxyRevalidate
        {
            get => (_flags & Flags.ProxyRevalidate) != 0;
            set => SetFlag(Flags.ProxyRevalidate, value);
        }

        public ICollection<NameValueHeaderValue> Extensions => _extensions ??= new UnvalidatedObjectCollection<NameValueHeaderValue>();

        public CacheControlHeaderValue()
        {
        }

        private CacheControlHeaderValue(CacheControlHeaderValue source)
        {
            Debug.Assert(source != null);

            _flags = source._flags;
            _maxAge = source._maxAge;
            _sharedMaxAge = source._sharedMaxAge;
            _maxStaleLimit = source._maxStaleLimit;
            _minFresh = source._minFresh;

            if (source._noCacheHeaders != null)
            {
                foreach (string noCacheHeader in source._noCacheHeaders)
                {
                    NoCacheHeaders.Add(noCacheHeader);
                }
            }

            if (source._privateHeaders != null)
            {
                foreach (string privateHeader in source._privateHeaders)
                {
                    PrivateHeaders.Add(privateHeader);
                }
            }

            _extensions = source._extensions.Clone();
        }

        public override string ToString()
        {
            StringBuilder sb = StringBuilderCache.Acquire();

            AppendValueIfRequired(sb, NoStore, noStoreString);
            AppendValueIfRequired(sb, NoTransform, noTransformString);
            AppendValueIfRequired(sb, OnlyIfCached, onlyIfCachedString);
            AppendValueIfRequired(sb, Public, publicString);
            AppendValueIfRequired(sb, MustRevalidate, mustRevalidateString);
            AppendValueIfRequired(sb, ProxyRevalidate, proxyRevalidateString);

            if (NoCache)
            {
                AppendValueWithSeparatorIfRequired(sb, noCacheString);
                if ((_noCacheHeaders != null) && (_noCacheHeaders.Count > 0))
                {
                    sb.Append("=\"");
                    AppendValues(sb, _noCacheHeaders);
                    sb.Append('\"');
                }
            }

            if ((_flags & Flags.MaxAgeHasValue) != 0)
            {
                AppendValueWithSeparatorIfRequired(sb, maxAgeString);
                sb.Append('=');
                int maxAge = (int)_maxAge.TotalSeconds;
                if (maxAge >= 0)
                {
                    sb.Append(maxAge);
                }
                else
                {
                    // In the corner case where the value is negative, ensure it uses
                    // the invariant's negative sign rather than the current culture's.
                    sb.Append(NumberFormatInfo.InvariantInfo, $"{maxAge}");
                }
            }

            if ((_flags & Flags.SharedMaxAgeHasValue) != 0)
            {
                AppendValueWithSeparatorIfRequired(sb, sharedMaxAgeString);
                sb.Append('=');
                int sharedMaxAge = (int)_sharedMaxAge.TotalSeconds;
                if (sharedMaxAge >= 0)
                {
                    sb.Append(sharedMaxAge);
                }
                else
                {
                    // In the corner case where the value is negative, ensure it uses
                    // the invariant's negative sign rather than the current culture's.
                    sb.Append(NumberFormatInfo.InvariantInfo, $"{sharedMaxAge}");
                }
            }

            if (MaxStale)
            {
                AppendValueWithSeparatorIfRequired(sb, maxStaleString);
                if ((_flags & Flags.MaxStaleLimitHasValue) != 0)
                {
                    sb.Append('=');
                    int maxStaleLimit = (int)_maxStaleLimit.TotalSeconds;
                    if (maxStaleLimit >= 0)
                    {
                        sb.Append(maxStaleLimit);
                    }
                    else
                    {
                        // In the corner case where the value is negative, ensure it uses
                        // the invariant's negative sign rather than the current culture's.
                        sb.Append(NumberFormatInfo.InvariantInfo, $"{maxStaleLimit}");
                    }
                }
            }

            if ((_flags & Flags.MinFreshHasValue) != 0)
            {
                AppendValueWithSeparatorIfRequired(sb, minFreshString);
                sb.Append('=');
                int minFresh = (int)_minFresh.TotalSeconds;
                if (minFresh >= 0)
                {
                    sb.Append(minFresh);
                }
                else
                {
                    // In the corner case where the value is negative, ensure it uses
                    // the invariant's negative sign rather than the current culture's.
                    sb.Append(NumberFormatInfo.InvariantInfo, $"{minFresh}");
                }
            }

            if (Private)
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

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is CacheControlHeaderValue other &&
            _flags == other._flags &&
            _maxAge == other._maxAge &&
            _sharedMaxAge == other._sharedMaxAge &&
            _maxStaleLimit == other._maxStaleLimit &&
            _minFresh == other._minFresh &&
            HeaderUtilities.AreEqualCollections(_noCacheHeaders, other._noCacheHeaders, StringComparer.OrdinalIgnoreCase) &&
            HeaderUtilities.AreEqualCollections(_privateHeaders, other._privateHeaders, StringComparer.OrdinalIgnoreCase) &&
            HeaderUtilities.AreEqualCollections(_extensions, other._extensions);

        public override int GetHashCode() =>
            HashCode.Combine(
                _flags,
                _maxAge,
                _sharedMaxAge,
                _maxStaleLimit,
                _minFresh,
                (_noCacheHeaders is null ? 0 : _noCacheHeaders.GetHashCode(StringComparer.OrdinalIgnoreCase)),
                (_privateHeaders is null ? 0 : _privateHeaders.GetHashCode(StringComparer.OrdinalIgnoreCase)),
                NameValueHeaderValue.GetHashCode(_extensions));

        public static CacheControlHeaderValue Parse(string? input)
        {
            int index = 0;
            return (CacheControlHeaderValue)CacheControlHeaderParser.Parser.ParseValue(input, null, ref index) ?? new CacheControlHeaderValue();
        }

        public static bool TryParse(string? input, [NotNullWhen(true)] out CacheControlHeaderValue? parsedValue)
        {
            int index = 0;
            parsedValue = null;

            if (CacheControlHeaderParser.Parser.TryParseValue(input, null, ref index, out object? output))
            {
                parsedValue = (CacheControlHeaderValue?)output ?? new CacheControlHeaderValue();
                return true;
            }
            return false;
        }

        internal static int GetCacheControlLength(string? input, int startIndex, CacheControlHeaderValue? storeValue,
            out CacheControlHeaderValue? parsedValue)
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
            List<NameValueHeaderValue> nameValueList = new List<NameValueHeaderValue>();
            while (current < input.Length)
            {
                if (!s_nameValueListParser.TryParseValue(input, null, ref current, out object? nameValue))
                {
                    return 0;
                }

                Debug.Assert(nameValue is not null);
                nameValueList.Add((NameValueHeaderValue)nameValue);
            }

            // If we get here, we were able to successfully parse the string as list of name/value pairs. Now analyze
            // the name/value pairs.

            // Cache-Control is a header supporting lists of values. However, expose the header as an instance of
            // CacheControlHeaderValue. So if we already have an instance of CacheControlHeaderValue, add the values
            // from this string to the existing instances.
            CacheControlHeaderValue? result = storeValue ?? new CacheControlHeaderValue();

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

        private static bool TrySetCacheControlValues(CacheControlHeaderValue cc, List<NameValueHeaderValue> nameValueList)
        {
            foreach (NameValueHeaderValue nameValue in nameValueList)
            {
                string name = nameValue.Name.ToLowerInvariant();
                string? value = nameValue.Value;

                Flags flagsToSet = Flags.None;
                bool success = value is null;

                switch (name)
                {
                    case noCacheString:
                        flagsToSet = Flags.NoCache;
                        success = TrySetOptionalTokenList(nameValue, ref cc._noCacheHeaders);
                        break;

                    case noStoreString:
                        flagsToSet = Flags.NoStore;
                        break;

                    case maxAgeString:
                        flagsToSet = Flags.MaxAgeHasValue;
                        success = TrySetTimeSpan(value, ref cc._maxAge);
                        break;

                    case maxStaleString:
                        flagsToSet = Flags.MaxStale;
                        if (TrySetTimeSpan(value, ref cc._maxStaleLimit))
                        {
                            success = true;
                            flagsToSet = Flags.MaxStale | Flags.MaxStaleLimitHasValue;
                        }
                        break;

                    case minFreshString:
                        flagsToSet = Flags.MinFreshHasValue;
                        success = TrySetTimeSpan(value, ref cc._minFresh);
                        break;

                    case noTransformString:
                        flagsToSet = Flags.NoTransform;
                        break;

                    case onlyIfCachedString:
                        flagsToSet = Flags.OnlyIfCached;
                        break;

                    case publicString:
                        flagsToSet = Flags.Public;
                        break;

                    case privateString:
                        flagsToSet = Flags.Private;
                        success = TrySetOptionalTokenList(nameValue, ref cc._privateHeaders);
                        break;

                    case mustRevalidateString:
                        flagsToSet = Flags.MustRevalidate;
                        break;

                    case proxyRevalidateString:
                        flagsToSet = Flags.ProxyRevalidate;
                        break;

                    case sharedMaxAgeString:
                        flagsToSet = Flags.SharedMaxAgeHasValue;
                        success = TrySetTimeSpan(value, ref cc._sharedMaxAge);
                        break;

                    default:
                        success = true;
                        cc.Extensions.Add(nameValue);
                        break;
                }

                if (success)
                {
                    cc._flags |= flagsToSet;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TrySetOptionalTokenList(NameValueHeaderValue nameValue, ref TokenObjectCollection? destination)
        {
            Debug.Assert(nameValue != null);

            if (nameValue.Value == null)
            {
                return true;
            }

            // We need the string to be at least 3 chars long: 2x quotes and at least 1 character. Also make sure we
            // have a quoted string. Note that NameValueHeaderValue will never have leading/trailing whitespace.
            string valueString = nameValue.Value;
            if ((valueString.Length < 3) || !valueString.StartsWith('\"') || !valueString.EndsWith('\"'))
            {
                return false;
            }

            // We have a quoted string. Now verify that the string contains a list of valid tokens separated by ','.
            int current = 1; // skip the initial '"' character.
            int maxLength = valueString.Length - 1; // -1 because we don't want to parse the final '"'.
            int originalValueCount = destination == null ? 0 : destination.Count;
            while (current < maxLength)
            {
                current = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(valueString, current, true,
                    out _);

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

                destination ??= new TokenObjectCollection();
                destination.Add(valueString.Substring(current, tokenLength));

                current += tokenLength;
            }

            // After parsing a valid token list, we expect to have at least one value
            if ((destination != null) && (destination.Count > originalValueCount))
            {
                return true;
            }

            return false;
        }

        private static bool TrySetTimeSpan(string? value, ref TimeSpan timeSpan)
        {
            if (value is null || !HeaderUtilities.TryParseInt32(value, out int seconds))
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

        private static void AppendValues(StringBuilder sb, TokenObjectCollection values)
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

        object ICloneable.Clone()
        {
            return new CacheControlHeaderValue(this);
        }

        private sealed class TokenObjectCollection : ObjectCollection<string>
        {
            public override void Validate(string item) => HeaderUtilities.CheckValidToken(item, nameof(item));

            public int GetHashCode(StringComparer comparer)
            {
                int hashcode = 0;

                foreach (string value in this)
                {
                    hashcode ^= comparer.GetHashCode(value);
                }

                return hashcode;
            }
        }
    }
}

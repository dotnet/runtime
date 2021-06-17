// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;

namespace System.Net.Http.Headers
{
    internal sealed class GenericHeaderParser : BaseHeaderParser
    {
        private delegate int GetParsedValueLengthDelegate(string value, int startIndex, out object? parsedValue);

        #region Parser instances

        internal static readonly GenericHeaderParser HostParser = new GenericHeaderParser(false, ParseHost, StringComparer.OrdinalIgnoreCase);
        internal static readonly GenericHeaderParser TokenListParser = new GenericHeaderParser(true, ParseTokenList, StringComparer.OrdinalIgnoreCase);
        internal static readonly GenericHeaderParser SingleValueNameValueWithParametersParser = new GenericHeaderParser(false, NameValueWithParametersHeaderValue.GetNameValueWithParametersLength);
        internal static readonly GenericHeaderParser MultipleValueNameValueWithParametersParser = new GenericHeaderParser(true, NameValueWithParametersHeaderValue.GetNameValueWithParametersLength);
        internal static readonly GenericHeaderParser SingleValueNameValueParser = new GenericHeaderParser(false, ParseNameValue);
        internal static readonly GenericHeaderParser MultipleValueNameValueParser = new GenericHeaderParser(true, ParseNameValue);
        internal static readonly GenericHeaderParser SingleValueParserWithoutValidation = new GenericHeaderParser(false, ParseWithoutValidation);
        internal static readonly GenericHeaderParser SingleValueProductParser = new GenericHeaderParser(false, ParseProduct);
        internal static readonly GenericHeaderParser MultipleValueProductParser = new GenericHeaderParser(true, ParseProduct);
        internal static readonly GenericHeaderParser RangeConditionParser = new GenericHeaderParser(false, RangeConditionHeaderValue.GetRangeConditionLength);
        internal static readonly GenericHeaderParser SingleValueAuthenticationParser = new GenericHeaderParser(false, AuthenticationHeaderValue.GetAuthenticationLength);
        internal static readonly GenericHeaderParser MultipleValueAuthenticationParser = new GenericHeaderParser(true, AuthenticationHeaderValue.GetAuthenticationLength);
        internal static readonly GenericHeaderParser RangeParser = new GenericHeaderParser(false, RangeHeaderValue.GetRangeLength);
        internal static readonly GenericHeaderParser RetryConditionParser = new GenericHeaderParser(false, RetryConditionHeaderValue.GetRetryConditionLength);
        internal static readonly GenericHeaderParser ContentRangeParser = new GenericHeaderParser(false, ContentRangeHeaderValue.GetContentRangeLength);
        internal static readonly GenericHeaderParser ContentDispositionParser = new GenericHeaderParser(false, ContentDispositionHeaderValue.GetDispositionTypeLength);
        internal static readonly GenericHeaderParser SingleValueStringWithQualityParser = new GenericHeaderParser(false, StringWithQualityHeaderValue.GetStringWithQualityLength);
        internal static readonly GenericHeaderParser MultipleValueStringWithQualityParser = new GenericHeaderParser(true, StringWithQualityHeaderValue.GetStringWithQualityLength);
        internal static readonly GenericHeaderParser SingleValueEntityTagParser = new GenericHeaderParser(false, ParseSingleEntityTag);
        internal static readonly GenericHeaderParser MultipleValueEntityTagParser = new GenericHeaderParser(true, ParseMultipleEntityTags);
        internal static readonly GenericHeaderParser SingleValueViaParser = new GenericHeaderParser(false, ViaHeaderValue.GetViaLength);
        internal static readonly GenericHeaderParser MultipleValueViaParser = new GenericHeaderParser(true, ViaHeaderValue.GetViaLength);
        internal static readonly GenericHeaderParser SingleValueWarningParser = new GenericHeaderParser(false, WarningHeaderValue.GetWarningLength);
        internal static readonly GenericHeaderParser MultipleValueWarningParser = new GenericHeaderParser(true, WarningHeaderValue.GetWarningLength);

        #endregion

        private readonly GetParsedValueLengthDelegate _getParsedValueLength;
        private readonly IEqualityComparer? _comparer;

        public override IEqualityComparer? Comparer
        {
            get { return _comparer; }
        }

        private GenericHeaderParser(bool supportsMultipleValues, GetParsedValueLengthDelegate getParsedValueLength)
            : this(supportsMultipleValues, getParsedValueLength, null)
        {
        }

        private GenericHeaderParser(bool supportsMultipleValues, GetParsedValueLengthDelegate getParsedValueLength,
            IEqualityComparer? comparer)
            : base(supportsMultipleValues)
        {
            Debug.Assert(getParsedValueLength != null);

            _getParsedValueLength = getParsedValueLength;
            _comparer = comparer;
        }

        protected override int GetParsedValueLength(string value, int startIndex, object? storeValue,
            out object? parsedValue)
        {
            return _getParsedValueLength(value, startIndex, out parsedValue);
        }

        #region Parse methods

        private static int ParseNameValue(string value, int startIndex, out object? parsedValue)
        {
            int resultLength = NameValueHeaderValue.GetNameValueLength(value, startIndex, out NameValueHeaderValue? temp);

            parsedValue = temp;
            return resultLength;
        }

        private static int ParseProduct(string value, int startIndex, out object? parsedValue)
        {
            int resultLength = ProductHeaderValue.GetProductLength(value, startIndex, out ProductHeaderValue? temp);

            parsedValue = temp;
            return resultLength;
        }

        private static int ParseSingleEntityTag(string value, int startIndex, out object? parsedValue)
        {
            parsedValue = null;

            int resultLength = EntityTagHeaderValue.GetEntityTagLength(value, startIndex, out EntityTagHeaderValue? temp);

            // If we don't allow '*' ("Any") as valid ETag value, return false (e.g. 'ETag' header)
            if (temp == EntityTagHeaderValue.Any)
            {
                return 0;
            }

            parsedValue = temp;
            return resultLength;
        }

        // Note that if multiple ETag values are allowed (e.g. 'If-Match', 'If-None-Match'), according to the RFC
        // the value must either be '*' or a list of ETag values. It's not allowed to have both '*' and a list of
        // ETag values. We're not that strict: We allow both '*' and ETag values in a list. If the server sends such
        // an invalid list, we want to be able to represent it using the corresponding header property.
        private static int ParseMultipleEntityTags(string value, int startIndex, out object? parsedValue)
        {
            int resultLength = EntityTagHeaderValue.GetEntityTagLength(value, startIndex, out EntityTagHeaderValue? temp);

            parsedValue = temp;
            return resultLength;
        }

        /// <summary>
        /// Allows for arbitrary header values without validation (aside from newline, which is always invalid in a header value).
        /// </summary>
        private static int ParseWithoutValidation(string value, int startIndex, out object? parsedValue)
        {
            if (HttpRuleParser.ContainsInvalidNewLine(value, startIndex))
            {
                parsedValue = null;
                return 0;
            }

            string result = value.Substring(startIndex);

            parsedValue = result;
            return result.Length;
        }

        private static int ParseHost(string value, int startIndex, out object? parsedValue)
        {
            int hostLength = HttpRuleParser.GetHostLength(value, startIndex, false, out string? host);

            parsedValue = host;
            return hostLength;
        }

        private static int ParseTokenList(string value, int startIndex, out object parsedValue)
        {
            int resultLength = HttpRuleParser.GetTokenLength(value, startIndex);

            parsedValue = value.Substring(startIndex, resultLength);
            return resultLength;
        }
        #endregion
    }
}

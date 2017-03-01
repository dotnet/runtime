// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace System.Net.Http.Headers
{
    public class MediaTypeHeaderValue : ICloneable
    {
        private const string charSet = "charset";

        private ObjectCollection<NameValueHeaderValue> _parameters;
        private string _mediaType;

        public string CharSet
        {
            get
            {
                NameValueHeaderValue charSetParameter = NameValueHeaderValue.Find(_parameters, charSet);
                if (charSetParameter != null)
                {
                    return charSetParameter.Value;
                }
                return null;
            }
            set
            {
                // We don't prevent a user from setting whitespace-only charsets. Like we can't prevent a user from
                // setting a non-existing charset.
                NameValueHeaderValue charSetParameter = NameValueHeaderValue.Find(_parameters, charSet);
                if (string.IsNullOrEmpty(value))
                {
                    // Remove charset parameter
                    if (charSetParameter != null)
                    {
                        _parameters.Remove(charSetParameter);
                    }
                }
                else
                {
                    if (charSetParameter != null)
                    {
                        charSetParameter.Value = value;
                    }
                    else
                    {
                        Parameters.Add(new NameValueHeaderValue(charSet, value));
                    }
                }
            }
        }

        public ICollection<NameValueHeaderValue> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    _parameters = new ObjectCollection<NameValueHeaderValue>();
                }
                return _parameters;
            }
        }

        public string MediaType
        {
            get { return _mediaType; }
            set
            {
                CheckMediaTypeFormat(value, nameof(value));
                _mediaType = value;
            }
        }

        internal MediaTypeHeaderValue()
        {
            // Used by the parser to create a new instance of this type.
        }

        protected MediaTypeHeaderValue(MediaTypeHeaderValue source)
        {
            Debug.Assert(source != null);

            _mediaType = source._mediaType;

            if (source._parameters != null)
            {
                foreach (var parameter in source._parameters)
                {
                    this.Parameters.Add((NameValueHeaderValue)((ICloneable)parameter).Clone());
                }
            }
        }

        public MediaTypeHeaderValue(string mediaType)
        {
            CheckMediaTypeFormat(mediaType, nameof(mediaType));
            _mediaType = mediaType;
        }

        public override string ToString()
        {
            var sb = StringBuilderCache.Acquire();
            sb.Append(_mediaType);
            NameValueHeaderValue.ToString(_parameters, ';', true, sb);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public override bool Equals(object obj)
        {
            MediaTypeHeaderValue other = obj as MediaTypeHeaderValue;

            if (other == null)
            {
                return false;
            }

            return string.Equals(_mediaType, other._mediaType, StringComparison.OrdinalIgnoreCase) &&
                HeaderUtilities.AreEqualCollections(_parameters, other._parameters);
        }

        public override int GetHashCode()
        {
            // The media-type string is case-insensitive.
            return StringComparer.OrdinalIgnoreCase.GetHashCode(_mediaType) ^ NameValueHeaderValue.GetHashCode(_parameters);
        }

        public static MediaTypeHeaderValue Parse(string input)
        {
            int index = 0;
            return (MediaTypeHeaderValue)MediaTypeHeaderParser.SingleValueParser.ParseValue(input, null, ref index);
        }

        public static bool TryParse(string input, out MediaTypeHeaderValue parsedValue)
        {
            int index = 0;
            object output;
            parsedValue = null;

            if (MediaTypeHeaderParser.SingleValueParser.TryParseValue(input, null, ref index, out output))
            {
                parsedValue = (MediaTypeHeaderValue)output;
                return true;
            }
            return false;
        }

        internal static int GetMediaTypeLength(string input, int startIndex,
            Func<MediaTypeHeaderValue> mediaTypeCreator, out MediaTypeHeaderValue parsedValue)
        {
            Debug.Assert(mediaTypeCreator != null);
            Debug.Assert(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Caller must remove leading whitespace. If not, we'll return 0.
            string mediaType = null;
            int mediaTypeLength = MediaTypeHeaderValue.GetMediaTypeExpressionLength(input, startIndex, out mediaType);

            if (mediaTypeLength == 0)
            {
                return 0;
            }

            int current = startIndex + mediaTypeLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);
            MediaTypeHeaderValue mediaTypeHeader = null;

            // If we're not done and we have a parameter delimiter, then we have a list of parameters.
            if ((current < input.Length) && (input[current] == ';'))
            {
                mediaTypeHeader = mediaTypeCreator();
                mediaTypeHeader._mediaType = mediaType;

                current++; // skip delimiter.
                int parameterLength = NameValueHeaderValue.GetNameValueListLength(input, current, ';',
                    (ObjectCollection<NameValueHeaderValue>)mediaTypeHeader.Parameters);

                if (parameterLength == 0)
                {
                    return 0;
                }

                parsedValue = mediaTypeHeader;
                return current + parameterLength - startIndex;
            }

            // We have a media type without parameters.
            mediaTypeHeader = mediaTypeCreator();
            mediaTypeHeader._mediaType = mediaType;
            parsedValue = mediaTypeHeader;
            return current - startIndex;
        }

        private static int GetMediaTypeExpressionLength(string input, int startIndex, out string mediaType)
        {
            Debug.Assert((input != null) && (input.Length > 0) && (startIndex < input.Length));

            // This method just parses the "type/subtype" string, it does not parse parameters.
            mediaType = null;

            // Parse the type, i.e. <type> in media type string "<type>/<subtype>; param1=value1; param2=value2"
            int typeLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (typeLength == 0)
            {
                return 0;
            }

            int current = startIndex + typeLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            // Parse the separator between type and subtype
            if ((current >= input.Length) || (input[current] != '/'))
            {
                return 0;
            }
            current++; // skip delimiter.
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            // Parse the subtype, i.e. <subtype> in media type string "<type>/<subtype>; param1=value1; param2=value2"
            int subtypeLength = HttpRuleParser.GetTokenLength(input, current);

            if (subtypeLength == 0)
            {
                return 0;
            }

            // If there is no whitespace between <type> and <subtype> in <type>/<subtype> get the media type using
            // one Substring call. Otherwise get substrings for <type> and <subtype> and combine them.
            int mediatTypeLength = current + subtypeLength - startIndex;
            if (typeLength + subtypeLength + 1 == mediatTypeLength)
            {
                mediaType = input.Substring(startIndex, mediatTypeLength);
            }
            else
            {
                mediaType = input.Substring(startIndex, typeLength) + "/" + input.Substring(current, subtypeLength);
            }

            return mediatTypeLength;
        }

        private static void CheckMediaTypeFormat(string mediaType, string parameterName)
        {
            if (string.IsNullOrEmpty(mediaType))
            {
                throw new ArgumentException(SR.net_http_argument_empty_string, parameterName);
            }

            // When adding values using strongly typed objects, no leading/trailing LWS (whitespace) are allowed.
            // Also no LWS between type and subtype are allowed.
            string tempMediaType;
            int mediaTypeLength = GetMediaTypeExpressionLength(mediaType, 0, out tempMediaType);
            if ((mediaTypeLength == 0) || (tempMediaType.Length != mediaType.Length))
            {
                throw new FormatException(string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, mediaType));
            }
        }

        // Implement ICloneable explicitly to allow derived types to "override" the implementation.
        object ICloneable.Clone()
        {
            return new MediaTypeHeaderValue(this);
        }
    }
}

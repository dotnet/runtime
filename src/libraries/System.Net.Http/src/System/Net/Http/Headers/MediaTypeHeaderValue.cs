// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using static System.HexConverter;

namespace System.Net.Http.Headers
{
    /// <summary>Represents a media type used in a Content-Type header as defined in the RFC 2616.</summary>
    /// <remarks>
    /// The <see cref="MediaTypeHeaderValue"/> class provides support for the media type used in a Content-Type header
    /// as defined in RFC 2616 by the IETF. An example of a media-type would be "text/plain; charset=iso-8859-5".
    /// </remarks>
    public class MediaTypeHeaderValue : ICloneable
    {
        /// <summary>The name of the charset header value.</summary>
        private const string CharSetName = "charset";

        /// <summary>The lazily-initialized parameters of the header value.</summary>
        private UnvalidatedObjectCollection<NameValueHeaderValue>? _parameters;
        /// <summary>The media type.</summary>
        private string? _mediaType;

        /// <summary>Gets or sets the character set.</summary>
        /// <value>The character set.</value>
        public string? CharSet
        {
            get => NameValueHeaderValue.Find(_parameters, CharSetName)?.Value;
            set
            {
                // We don't prevent a user from setting whitespace-only charsets. Like we can't prevent a user from
                // setting a non-existing charset.
                NameValueHeaderValue? charSetParameter = NameValueHeaderValue.Find(_parameters, CharSetName);
                if (string.IsNullOrEmpty(value))
                {
                    // Remove charset parameter
                    if (charSetParameter != null)
                    {
                        _parameters!.Remove(charSetParameter);
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
                        Parameters.Add(new NameValueHeaderValue(CharSetName, value));
                    }
                }
            }
        }

        /// <summary>Gets the media-type header value parameters.</summary>
        /// <value>The media-type header value parameters.</value>
        public ICollection<NameValueHeaderValue> Parameters => _parameters ??= new UnvalidatedObjectCollection<NameValueHeaderValue>();

        /// <summary>Gets or sets the media-type header value.</summary>
        /// <value>The media-type header value.</value>
        [DisallowNull]
        public string? MediaType
        {
            get { return _mediaType; }
            set
            {
                CheckMediaTypeFormat(value, nameof(value));
                _mediaType = value;
            }
        }

        /// <summary>Used by the parser to create a new instance of this type.</summary>
        internal MediaTypeHeaderValue()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="MediaTypeHeaderValue"/> class.</summary>
        /// <param name="source">A <see cref="MediaTypeHeaderValue"/> object used to initialize the new instance.</param>
        protected MediaTypeHeaderValue(MediaTypeHeaderValue source)
        {
            Debug.Assert(source != null);

            _mediaType = source._mediaType;
            _parameters = source._parameters.Clone();
        }

        /// <summary>Initializes a new instance of the <see cref="MediaTypeHeaderValue"/> class.</summary>
        /// <param name="mediaType">The source represented as a string to initialize the new instance.</param>
        public MediaTypeHeaderValue(string mediaType)
            : this(mediaType, charSet: null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="MediaTypeHeaderValue"/> class.</summary>
        /// <param name="mediaType">The source represented as a string to initialize the new instance.</param>
        /// <param name="charSet">The value to use for the character set.</param>
        public MediaTypeHeaderValue(string mediaType, string? charSet)
        {
            CheckMediaTypeFormat(mediaType, nameof(mediaType));
            _mediaType = mediaType;

            if (!string.IsNullOrEmpty(charSet))
            {
                CharSet = charSet;
            }
        }

        /// <summary>Returns a string that represents the current <see cref="MediaTypeHeaderValue"/> object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            if (_parameters is null || _parameters.Count == 0)
            {
                return _mediaType ?? string.Empty;
            }

            var sb = StringBuilderCache.Acquire();
            sb.Append(_mediaType);
            NameValueHeaderValue.ToString(_parameters, ';', true, sb);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        /// <summary>Determines whether the specified <see cref="object"/> is equal to the current <see cref="MediaTypeHeaderValue"/> object.</summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><see langword="true"/> if the specified <see cref="object"/> is equal to the current object; otherwise, <see langword="false"/>.</returns>
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is MediaTypeHeaderValue other &&
            string.Equals(_mediaType, other._mediaType, StringComparison.OrdinalIgnoreCase) &&
            HeaderUtilities.AreEqualCollections(_parameters, other._parameters);

        /// <summary>Serves as a hash function for an <see cref="MediaTypeHeaderValue"/> object.</summary>
        /// <returns>A hash code for the current object.</returns>
        /// <remarks>
        /// A hash code is a numeric value that is used to identify an object during equality testing. It can also serve as an index for an object in a collection.
        /// The GetHashCode method is suitable for use in hashing algorithms and data structures such as a hash table.
        /// </remarks>
        public override int GetHashCode()
        {
            // The media-type string is case-insensitive.
            return StringComparer.OrdinalIgnoreCase.GetHashCode(_mediaType!) ^ NameValueHeaderValue.GetHashCode(_parameters);
        }

        /// <summary>Converts a string to an <see cref="MediaTypeHeaderValue"/> instance.</summary>
        /// <param name="input">A string that represents media type header value information.</param>
        /// <returns>A <see cref="MediaTypeHeaderValue"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is a <see langword="null"/> reference.</exception>
        /// <exception cref="FormatException"><parmref name="input"/> is not valid media type header value information.</exception>
        public static MediaTypeHeaderValue Parse(string? input)
        {
            int index = 0;
            return (MediaTypeHeaderValue)MediaTypeHeaderParser.SingleValueParser.ParseValue(input, null, ref index);
        }

        /// <summary>Determines whether a string is valid <see cref="MediaTypeHeaderValue"/> information.</summary>
        /// <param name="input">The string to validate.</param>
        /// <param name="parsedValue">The <see cref="MediaTypeHeaderValue"/> version of the string.</param>
        /// <returns><see langword="true"/> if input is valid <see cref="MediaTypeHeaderValue"/> information; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out MediaTypeHeaderValue? parsedValue)
        {
            int index = 0;
            parsedValue = null;

            if (MediaTypeHeaderParser.SingleValueParser.TryParseValue(input, null, ref index, out object? output))
            {
                parsedValue = (MediaTypeHeaderValue)output!;
                return true;
            }
            return false;
        }

        internal static int GetMediaTypeLength(string? input, int startIndex,
            Func<MediaTypeHeaderValue> mediaTypeCreator, out MediaTypeHeaderValue? parsedValue)
        {
            Debug.Assert(mediaTypeCreator != null);
            Debug.Assert(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Caller must remove leading whitespace. If not, we'll return 0.
            int mediaTypeLength = MediaTypeHeaderValue.GetMediaTypeExpressionLength(input, startIndex, out string? mediaType);

            if (mediaTypeLength == 0)
            {
                return 0;
            }

            int current = startIndex + mediaTypeLength;
            current += HttpRuleParser.GetWhitespaceLength(input, current);
            MediaTypeHeaderValue mediaTypeHeader;

            // If we're not done and we have a parameter delimiter, then we have a list of parameters.
            if ((current < input.Length) && (input[current] == ';'))
            {
                mediaTypeHeader = mediaTypeCreator();
                mediaTypeHeader._mediaType = mediaType;

                current++; // skip delimiter.
                int parameterLength = NameValueHeaderValue.GetNameValueListLength(input, current, ';',
                    (UnvalidatedObjectCollection<NameValueHeaderValue>)mediaTypeHeader.Parameters);

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

        private static int GetMediaTypeExpressionLength(string input, int startIndex, out string? mediaType)
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
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            // Parse the separator between type and subtype
            if ((current >= input.Length) || (input[current] != '/'))
            {
                return 0;
            }
            current++; // skip delimiter.
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            // Parse the subtype, i.e. <subtype> in media type string "<type>/<subtype>; param1=value1; param2=value2"
            int subtypeLength = HttpRuleParser.GetTokenLength(input, current);

            if (subtypeLength == 0)
            {
                return 0;
            }

            // If there is no whitespace between <type> and <subtype> in <type>/<subtype> get the media type using
            // one Substring call. Otherwise get substrings for <type> and <subtype> and combine them.
            int mediaTypeLength = current + subtypeLength - startIndex;
            if (typeLength + subtypeLength + 1 == mediaTypeLength)
            {
                mediaType = input.Substring(startIndex, mediaTypeLength);
            }
            else
            {
                mediaType = string.Concat(input.AsSpan(startIndex, typeLength), "/", input.AsSpan(current, subtypeLength));
            }

            return mediaTypeLength;
        }

        private static void CheckMediaTypeFormat(string mediaType, string parameterName)
        {
            if (string.IsNullOrEmpty(mediaType))
            {
                throw new ArgumentException(SR.net_http_argument_empty_string, parameterName);
            }

            // When adding values using strongly typed objects, no leading/trailing LWS (whitespace) are allowed.
            // Also no LWS between type and subtype are allowed.
            int mediaTypeLength = GetMediaTypeExpressionLength(mediaType, 0, out string? tempMediaType);
            if ((mediaTypeLength == 0) || (tempMediaType!.Length != mediaType.Length))
            {
                throw new FormatException(SR.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, mediaType));
            }
        }

        // Implement ICloneable explicitly to allow derived types to "override" the implementation.
        object ICloneable.Clone()
        {
            return new MediaTypeHeaderValue(this);
        }
    }
}

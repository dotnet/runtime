// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers
{
    public class ContentDispositionHeaderValue : ICloneable
    {
        #region Fields

        private const string fileName = "filename";
        private const string name = "name";
        private const string fileNameStar = "filename*";
        private const string creationDate = "creation-date";
        private const string modificationDate = "modification-date";
        private const string readDate = "read-date";
        private const string size = "size";

        // Use UnvalidatedObjectCollection<T> since we may have multiple parameters with the same name.
        private UnvalidatedObjectCollection<NameValueHeaderValue>? _parameters;
        private string _dispositionType = null!;

        #endregion Fields

        #region Properties

        public string DispositionType
        {
            get { return _dispositionType; }
            set
            {
                CheckDispositionTypeFormat(value, nameof(value));
                _dispositionType = value;
            }
        }

        public ICollection<NameValueHeaderValue> Parameters => _parameters ??= new UnvalidatedObjectCollection<NameValueHeaderValue>();

        public string? Name
        {
            get { return GetName(name); }
            set { SetName(name, value); }
        }

        public string? FileName
        {
            get { return GetName(fileName); }
            set { SetName(fileName, value); }
        }

        public string? FileNameStar
        {
            get { return GetName(fileNameStar); }
            set { SetName(fileNameStar, value); }
        }

        public DateTimeOffset? CreationDate
        {
            get { return GetDate(creationDate); }
            set { SetDate(creationDate, value); }
        }

        public DateTimeOffset? ModificationDate
        {
            get { return GetDate(modificationDate); }
            set { SetDate(modificationDate, value); }
        }

        public DateTimeOffset? ReadDate
        {
            get { return GetDate(readDate); }
            set { SetDate(readDate, value); }
        }

        public long? Size
        {
            get
            {
                NameValueHeaderValue? sizeParameter = NameValueHeaderValue.Find(_parameters, size);
                ulong value;
                if (sizeParameter != null)
                {
                    string? sizeString = sizeParameter.Value;
                    if (ulong.TryParse(sizeString, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                    {
                        return (long)value;
                    }
                }
                return null;
            }
            set
            {
                NameValueHeaderValue? sizeParameter = NameValueHeaderValue.Find(_parameters, size);
                if (value == null)
                {
                    // Remove parameter.
                    if (sizeParameter != null)
                    {
                        _parameters!.Remove(sizeParameter);
                    }
                }
                else if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                else if (sizeParameter != null)
                {
                    sizeParameter.Value = value.Value.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    string sizeString = value.Value.ToString(CultureInfo.InvariantCulture);
                    Parameters.Add(new NameValueHeaderValue(size, sizeString));
                }
            }
        }

        #endregion Properties

        #region Constructors

        internal ContentDispositionHeaderValue()
        {
            // Used by the parser to create a new instance of this type.
        }

        protected ContentDispositionHeaderValue(ContentDispositionHeaderValue source)
        {
            Debug.Assert(source != null);

            _dispositionType = source._dispositionType;
            _parameters = source._parameters.Clone();
        }

        public ContentDispositionHeaderValue(string dispositionType)
        {
            CheckDispositionTypeFormat(dispositionType, nameof(dispositionType));
            _dispositionType = dispositionType;
        }

        #endregion Constructors

        #region Overloads

        public override string ToString()
        {
            StringBuilder sb = StringBuilderCache.Acquire();
            sb.Append(_dispositionType);
            NameValueHeaderValue.ToString(_parameters, ';', true, sb);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            ContentDispositionHeaderValue? other = obj as ContentDispositionHeaderValue;

            if (other == null)
            {
                return false;
            }

            return string.Equals(_dispositionType, other._dispositionType, StringComparison.OrdinalIgnoreCase) &&
                HeaderUtilities.AreEqualCollections(_parameters, other._parameters);
        }

        public override int GetHashCode()
        {
            // The dispositionType string is case-insensitive.
            return StringComparer.OrdinalIgnoreCase.GetHashCode(_dispositionType) ^ NameValueHeaderValue.GetHashCode(_parameters);
        }

        // Implement ICloneable explicitly to allow derived types to "override" the implementation.
        object ICloneable.Clone()
        {
            return new ContentDispositionHeaderValue(this);
        }

        #endregion Overloads

        #region Parsing

        public static ContentDispositionHeaderValue Parse(string? input)
        {
            int index = 0;
            return (ContentDispositionHeaderValue)GenericHeaderParser.ContentDispositionParser.ParseValue(input,
                null, ref index);
        }

        public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out ContentDispositionHeaderValue? parsedValue)
        {
            int index = 0;
            parsedValue = null;

            if (GenericHeaderParser.ContentDispositionParser.TryParseValue(input, null, ref index, out object? output))
            {
                parsedValue = (ContentDispositionHeaderValue)output!;
                return true;
            }
            return false;
        }

        internal static int GetDispositionTypeLength(string? input, int startIndex, out object? parsedValue)
        {
            Debug.Assert(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Caller must remove leading whitespace. If not, we'll return 0.
            string? dispositionType;
            int dispositionTypeLength = GetDispositionTypeExpressionLength(input, startIndex, out dispositionType);

            if (dispositionTypeLength == 0)
            {
                return 0;
            }

            int current = startIndex + dispositionTypeLength;
            current += HttpRuleParser.GetWhitespaceLength(input, current);
            ContentDispositionHeaderValue contentDispositionHeader = new ContentDispositionHeaderValue();
            contentDispositionHeader._dispositionType = dispositionType!;

            // If we're not done and we have a parameter delimiter, then we have a list of parameters.
            if ((current < input.Length) && (input[current] == ';'))
            {
                current++; // Skip delimiter.
                int parameterLength = NameValueHeaderValue.GetNameValueListLength(input, current, ';',
                    (UnvalidatedObjectCollection<NameValueHeaderValue>)contentDispositionHeader.Parameters);

                if (parameterLength == 0)
                {
                    return 0;
                }

                parsedValue = contentDispositionHeader;
                return current + parameterLength - startIndex;
            }

            // We have a ContentDisposition header without parameters.
            parsedValue = contentDispositionHeader;
            return current - startIndex;
        }

        private static int GetDispositionTypeExpressionLength(string input, int startIndex, out string? dispositionType)
        {
            Debug.Assert((input != null) && (input.Length > 0) && (startIndex < input.Length));

            // This method just parses the disposition type string, it does not parse parameters.
            dispositionType = null;

            // Parse the disposition type, i.e. <dispositiontype> in content-disposition string
            // "<dispositiontype>; param1=value1; param2=value2".
            int typeLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (typeLength == 0)
            {
                return 0;
            }

            dispositionType = input.Substring(startIndex, typeLength);
            return typeLength;
        }

        private static void CheckDispositionTypeFormat(string dispositionType, string parameterName)
        {
            if (string.IsNullOrEmpty(dispositionType))
            {
                throw new ArgumentException(SR.net_http_argument_empty_string, parameterName);
            }

            // When adding values using strongly typed objects, no leading/trailing LWS (whitespace) are allowed.
            int dispositionTypeLength = GetDispositionTypeExpressionLength(dispositionType, 0, out string? tempDispositionType);
            if ((dispositionTypeLength == 0) || (tempDispositionType!.Length != dispositionType.Length))
            {
                throw new FormatException(SR.Format(System.Globalization.CultureInfo.InvariantCulture,
                    SR.net_http_headers_invalid_value, dispositionType));
            }
        }

        #endregion Parsing

        #region Helpers

        // Gets a parameter of the given name and attempts to extract a date.
        // Returns null if the parameter is not present or the format is incorrect.
        private DateTimeOffset? GetDate(string parameter)
        {
            NameValueHeaderValue? dateParameter = NameValueHeaderValue.Find(_parameters, parameter);
            DateTimeOffset date;
            if (dateParameter != null)
            {
                ReadOnlySpan<char> dateString = dateParameter.Value;
                // Should have quotes, remove them.
                if (IsQuoted(dateString))
                {
                    dateString = dateString.Slice(1, dateString.Length - 2);
                }
                if (HttpDateParser.TryParse(dateString, out date))
                {
                    return date;
                }
            }
            return null;
        }

        // Add the given parameter to the list. Remove if date is null.
        private void SetDate(string parameter, DateTimeOffset? date)
        {
            NameValueHeaderValue? dateParameter = NameValueHeaderValue.Find(_parameters, parameter);
            if (date == null)
            {
                // Remove parameter.
                if (dateParameter != null)
                {
                    _parameters!.Remove(dateParameter);
                }
            }
            else
            {
                // Must always be quoted.
                string dateString = $"\"{date.GetValueOrDefault():r}\"";
                if (dateParameter != null)
                {
                    dateParameter.Value = dateString;
                }
                else
                {
                    Parameters.Add(new NameValueHeaderValue(parameter, dateString));
                }
            }
        }

        // Gets a parameter of the given name and attempts to decode it if necessary.
        // Returns null if the parameter is not present or the raw value if the encoding is incorrect.
        private string? GetName(string parameter)
        {
            NameValueHeaderValue? nameParameter = NameValueHeaderValue.Find(_parameters, parameter);
            if (nameParameter != null)
            {
                string? result;
                // filename*=utf-8'lang'%7FMyString
                if (parameter.EndsWith('*'))
                {
                    Debug.Assert(nameParameter.Value != null);
                    if (TryDecode5987(nameParameter.Value, out result))
                    {
                        return result;
                    }
                    return null; // Unrecognized encoding.
                }

                // filename="=?utf-8?B?BDFSDFasdfasdc==?="
                if (TryDecodeMime(nameParameter.Value, out result))
                {
                    return result;
                }
                // May not have been encoded.
                return nameParameter.Value;
            }
            return null;
        }

        // Add/update the given parameter in the list, encoding if necessary.
        // Remove if value is null/Empty
        private void SetName(string parameter, string? value)
        {
            NameValueHeaderValue? nameParameter = NameValueHeaderValue.Find(_parameters, parameter);
            if (string.IsNullOrEmpty(value))
            {
                // Remove parameter.
                if (nameParameter != null)
                {
                    _parameters!.Remove(nameParameter);
                }
            }
            else
            {
                string processedValue;
                if (parameter.EndsWith('*'))
                {
                    processedValue = HeaderUtilities.Encode5987(value);
                }
                else
                {
                    processedValue = EncodeAndQuoteMime(value);
                }

                if (nameParameter != null)
                {
                    nameParameter.Value = processedValue;
                }
                else
                {
                    Parameters.Add(new NameValueHeaderValue(parameter, processedValue));
                }
            }
        }

        // Returns input for decoding failures, as the content might not be encoded.
        private static string EncodeAndQuoteMime(string input)
        {
            string result = input;
            bool needsQuotes = false;
            // Remove bounding quotes, they'll get re-added later.
            if (IsQuoted(result))
            {
                result = result.Substring(1, result.Length - 2);
                needsQuotes = true;
            }

            if (result.Contains('"')) // Only bounding quotes are allowed.
            {
                throw new ArgumentException(SR.Format(CultureInfo.InvariantCulture,
                    SR.net_http_headers_invalid_value, input));
            }
            else if (HeaderUtilities.ContainsNonAscii(result))
            {
                needsQuotes = true; // Encoded data must always be quoted, the equals signs are invalid in tokens.
                result = EncodeMime(result); // =?utf-8?B?asdfasdfaesdf?=
            }
            else if (!needsQuotes && HttpRuleParser.GetTokenLength(result, 0) != result.Length)
            {
                needsQuotes = true;
            }

            if (needsQuotes)
            {
                // Re-add quotes "value".
                result = "\"" + result + "\"";
            }
            return result;
        }

        // Returns true if the value starts and ends with a quote.
        private static bool IsQuoted(ReadOnlySpan<char> value)
        {
            return
                value.Length > 1 &&
                value[0] == '"' &&
                value[value.Length - 1] == '"';
        }

        // Encode using MIME encoding.
        private static string EncodeMime(string input)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(input);
            string encodedName = Convert.ToBase64String(buffer);
            return "=?utf-8?B?" + encodedName + "?=";
        }

        // Attempt to decode MIME encoded strings.
        private static bool TryDecodeMime(string? input, [NotNullWhen(true)] out string? output)
        {
            Debug.Assert(input != null);

            output = null;
            string? processedInput = input;
            // Require quotes, min of "=?e?b??="
            if (!IsQuoted(processedInput) || processedInput.Length < 10)
            {
                return false;
            }

            string[] parts = processedInput.Split('?');
            // "=, encodingName, encodingType, encodedData, ="
            if (parts.Length != 5 || parts[0] != "\"=" || parts[4] != "=\"" || parts[2].ToLowerInvariant() != "b")
            {
                // Not encoded.
                // This does not support multi-line encoding.
                // Only base64 encoding is supported, not quoted printable.
                return false;
            }

            try
            {
                Encoding encoding = Encoding.GetEncoding(parts[1]);
                byte[] bytes = Convert.FromBase64String(parts[3]);
                output = encoding.GetString(bytes, 0, bytes.Length);
                return true;
            }
            catch (ArgumentException)
            {
                // Unknown encoding or bad characters.
            }
            catch (FormatException)
            {
                // Bad base64 decoding.
            }
            return false;
        }


        // Attempt to decode using RFC 5987 encoding.
        // encoding'language'my%20string
        private static bool TryDecode5987(string input, out string? output)
        {
            output = null;

            int quoteIndex = input.IndexOf('\'');
            if (quoteIndex == -1)
            {
                return false;
            }

            int lastQuoteIndex = input.LastIndexOf('\'');
            if (quoteIndex == lastQuoteIndex || input.IndexOf('\'', quoteIndex + 1) != lastQuoteIndex)
            {
                return false;
            }

            string encodingString = input.Substring(0, quoteIndex);
            string dataString = input.Substring(lastQuoteIndex + 1);

            StringBuilder decoded = new StringBuilder();
            try
            {
                Encoding encoding = Encoding.GetEncoding(encodingString);

                byte[] unescapedBytes = new byte[dataString.Length];
                int unescapedBytesCount = 0;
                for (int index = 0; index < dataString.Length; index++)
                {
                    if (Uri.IsHexEncoding(dataString, index)) // %FF
                    {
                        // Unescape and cache bytes, multi-byte characters must be decoded all at once.
                        unescapedBytes[unescapedBytesCount++] = (byte)Uri.HexUnescape(dataString, ref index);
                        index--; // HexUnescape did +=3; Offset the for loop's ++
                    }
                    else
                    {
                        if (unescapedBytesCount > 0)
                        {
                            // Decode any previously cached bytes.
                            decoded.Append(encoding.GetString(unescapedBytes, 0, unescapedBytesCount));
                            unescapedBytesCount = 0;
                        }
                        decoded.Append(dataString[index]); // Normal safe character.
                    }
                }

                if (unescapedBytesCount > 0)
                {
                    // Decode any previously cached bytes.
                    decoded.Append(encoding.GetString(unescapedBytes, 0, unescapedBytesCount));
                }
            }
            catch (ArgumentException)
            {
                return false; // Unknown encoding or bad characters.
            }

            output = decoded.ToString();
            return true;
        }
        #endregion Helpers
    }
}

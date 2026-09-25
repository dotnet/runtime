// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mime
{
    internal abstract class MimeBasePart
    {
        internal const string DefaultCharSet = "utf-8";

        protected ContentType? _contentType;
        protected ContentDisposition? _contentDisposition;
        private HeaderCollection? _headers;

        internal MimeBasePart() { }

        internal static bool ShouldUseBase64Encoding(Encoding? encoding) =>
            encoding == Encoding.Unicode || encoding == Encoding.UTF8 || encoding == Encoding.UTF32 || encoding == Encoding.BigEndianUnicode;

        //use when the length of the header is not known or if there is no header
        internal static string EncodeHeaderValue(string value, Encoding encoding, bool base64Encoding) =>
            EncodeHeaderValue(value, encoding, base64Encoding, 0);

        //used when the length of the header name itself is known (i.e. Subject : )
        internal static string EncodeHeaderValue(string value, Encoding? encoding, bool base64Encoding, int headerLength)
        {
            //no need to encode if it's pure ascii
            if (IsAscii(value, false))
            {
                return value;
            }

            encoding ??= Encoding.GetEncoding(DefaultCharSet);

            IEncodableStream stream = EncodedStreamFactory.GetEncoderForHeader(encoding, base64Encoding, headerLength);

            stream.EncodeString(value, encoding);
            return stream.GetEncodedString();
        }

        internal static string DecodeHeaderValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (!value.Contains("=?", StringComparison.Ordinal))
            {
                return value;
            }

            StringBuilder decodedValue = new StringBuilder(value.Length);
            ReadOnlySpan<char> valueSpan = value;
            bool decodedAny = false;
            bool previousTokenWasEncoded = false;
            int current = 0;

            while (current < valueSpan.Length)
            {
                int whitespaceStart = current;
                while (current < valueSpan.Length && IsLinearWhiteSpace(valueSpan[current]))
                {
                    current++;
                }

                int tokenStart = current;
                while (current < valueSpan.Length && !IsLinearWhiteSpace(valueSpan[current]))
                {
                    current++;
                }

                if (tokenStart == current)
                {
                    decodedValue.Append(valueSpan[whitespaceStart..current]);
                    break;
                }

                ReadOnlySpan<char> token = valueSpan[tokenStart..current];
                if (TryDecodeHeaderValue(token, out string decodedToken))
                {
                    if (!previousTokenWasEncoded)
                    {
                        decodedValue.Append(valueSpan[whitespaceStart..tokenStart]);
                    }

                    decodedValue.Append(decodedToken);
                    decodedAny = true;
                    previousTokenWasEncoded = true;
                }
                else
                {
                    decodedValue.Append(valueSpan[whitespaceStart..tokenStart]);
                    decodedValue.Append(token);
                    previousTokenWasEncoded = false;
                }
            }

            return decodedAny ? decodedValue.ToString() : value;
        }

        // Detect the encoding: "=?encoding?BorQ?content?="
        // "=?utf-8?B?RmlsZU5hbWVf55CG0Y3Qq9C60I5jw4TRicKq0YIM0Y1hSsSeTNCy0Klh?="; // 3.5
        // With the addition of folding in 4.0, there may be multiple lines with encoding, only detect the first:
        // "=?utf-8?B?RmlsZU5hbWVf55CG0Y3Qq9C60I5jw4TRicKq0YIM0Y1hSsSeTNCy0Klh?=\r\n =?utf-8?B??=";
        internal static Encoding? DecodeEncoding(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            ReadOnlySpan<char> valueSpan = value;
            int current = 0;

            while (current < valueSpan.Length)
            {
                while (current < valueSpan.Length && IsLinearWhiteSpace(valueSpan[current]))
                {
                    current++;
                }

                int tokenStart = current;
                while (current < valueSpan.Length && !IsLinearWhiteSpace(valueSpan[current]))
                {
                    current++;
                }

                ReadOnlySpan<char> token = valueSpan[tokenStart..current];
                if (TryParseEncodedWord(token, out Range charSet, out _, out _))
                {
                    Encoding? encoding = TryGetEncoding(token[charSet]);
                    if (encoding is not null)
                    {
                        return encoding;
                    }
                }
            }

            return null;
        }

        internal static bool IsFullyEncoded(string value)
        {
            ReadOnlySpan<char> valueSpan = value;
            bool encodedAny = false;
            int current = 0;

            while (current < valueSpan.Length)
            {
                int whitespaceStart = current;
                while (current < valueSpan.Length && IsLinearWhiteSpace(valueSpan[current]))
                {
                    current++;
                }

                if (!IsValidHeaderWhiteSpace(valueSpan[whitespaceStart..current]))
                {
                    return false;
                }

                int tokenStart = current;
                while (current < valueSpan.Length && !IsLinearWhiteSpace(valueSpan[current]))
                {
                    current++;
                }

                if (tokenStart == current)
                {
                    break;
                }

                ReadOnlySpan<char> token = valueSpan[tokenStart..current];
                if (!TryParseEncodedWord(token, out Range charSet, out _, out _) ||
                    TryGetEncoding(token[charSet]) is null ||
                    token.ContainsAny('"', '\\'))
                {
                    return false;
                }

                encodedAny = true;
            }

            return encodedAny;
        }

        private static bool TryDecodeHeaderValue(ReadOnlySpan<char> value, out string decodedValue)
        {
            decodedValue = string.Empty;
            if (!TryParseEncodedWord(value, out Range charSetRange, out bool base64Encoding, out Range encodedTextRange))
            {
                return false;
            }

            Encoding? encoding = TryGetEncoding(value[charSetRange]);
            if (encoding is null)
            {
                return false;
            }

            ReadOnlySpan<char> encodedText = value[encodedTextRange];
            byte[] buffer = new byte[encodedText.Length];
            Encoding.ASCII.GetBytes(encodedText, buffer);
            IEncodableStream stream = EncodedStreamFactory.GetEncoderForHeader(encoding, base64Encoding, 0);
            int decodedLength = stream.DecodeBytes(buffer);
            decodedValue = encoding.GetString(buffer, 0, decodedLength);
            return true;
        }

        private static bool TryParseEncodedWord(
            ReadOnlySpan<char> value,
            out Range charSet,
            out bool base64Encoding,
            out Range encodedText)
        {
            charSet = default;
            base64Encoding = false;
            encodedText = default;

            if (value.Length < 7 || !value.StartsWith("=?") || !value.EndsWith("?="))
            {
                return false;
            }

            foreach (char c in value)
            {
                if (c is < '!' or > '~')
                {
                    return false;
                }
            }

            int charSetEnd = value[2..].IndexOf('?');
            if (charSetEnd <= 0)
            {
                return false;
            }
            charSetEnd += 2;

            int encodingEnd = value[(charSetEnd + 1)..].IndexOf('?');
            if (encodingEnd != 1)
            {
                return false;
            }
            encodingEnd += charSetEnd + 1;

            char encodingIdentifier = value[charSetEnd + 1];
            if (encodingIdentifier is 'B' or 'b')
            {
                base64Encoding = true;
            }
            else if (encodingIdentifier is not ('Q' or 'q'))
            {
                return false;
            }

            ReadOnlySpan<char> encodedTextValue = value[(encodingEnd + 1)..^2];
            if (encodedTextValue.Contains('?'))
            {
                return false;
            }

            charSet = 2..charSetEnd;
            encodedText = (encodingEnd + 1)..^2;
            return true;
        }

        private static Encoding? TryGetEncoding(ReadOnlySpan<char> charSet)
        {
            try
            {
                return Encoding.GetEncoding(charSet.ToString());
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        private static bool IsValidHeaderWhiteSpace(ReadOnlySpan<char> value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] is ' ' or '\t')
                {
                    continue;
                }

                if (value[i] != '\r' ||
                    i + 2 >= value.Length ||
                    value[i + 1] != '\n' ||
                    value[i + 2] is not (' ' or '\t'))
                {
                    return false;
                }

                i += 2;
            }

            return true;
        }

        private static bool IsLinearWhiteSpace(char value) => value is ' ' or '\t' or '\r' or '\n';

        internal static bool IsAscii(string value, bool permitCROrLF)
        {
            ArgumentNullException.ThrowIfNull(value);

            return Ascii.IsValid(value) && (permitCROrLF || !value.AsSpan().ContainsAny('\r', '\n'));
        }

        internal string? ContentID
        {
            get { return Headers[MailHeaderInfo.GetString(MailHeaderID.ContentID)!]; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.ContentID));
                }
                else
                {
                    Headers[MailHeaderInfo.GetString(MailHeaderID.ContentID)] = value;
                }
            }
        }

        internal string? ContentLocation
        {
            get { return Headers[MailHeaderInfo.GetString(MailHeaderID.ContentLocation)!]; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.ContentLocation));
                }
                else
                {
                    Headers[MailHeaderInfo.GetString(MailHeaderID.ContentLocation)] = value;
                }
            }
        }

        internal NameValueCollection Headers
        {
            get
            {
                //persist existing info before returning
                _headers ??= new HeaderCollection();

                _contentType ??= new ContentType();
                _contentType.PersistIfNeeded(_headers, false);

                _contentDisposition?.PersistIfNeeded(_headers, false);

                return _headers;
            }
        }

        internal ContentType ContentType
        {
            get { return _contentType ??= new ContentType(); }
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                _contentType = value;
                _contentType.PersistIfNeeded((HeaderCollection)Headers, true);
            }
        }

        internal void PrepareHeaders(bool allowUnicode)
        {
            _contentType!.PersistIfNeeded((HeaderCollection)Headers, false);
            _headers!.InternalSet(MailHeaderInfo.GetString(MailHeaderID.ContentType)!, _contentType.Encode(allowUnicode));

            if (_contentDisposition != null)
            {
                _contentDisposition.PersistIfNeeded((HeaderCollection)Headers, false);
                _headers.InternalSet(MailHeaderInfo.GetString(MailHeaderID.ContentDisposition)!, _contentDisposition.Encode(allowUnicode));
            }
        }

        internal abstract Task SendAsync<TIOAdapter>(BaseWriter writer, bool allowUnicode, CancellationToken cancellationToken) where TIOAdapter : IReadWriteAdapter;
    }
}

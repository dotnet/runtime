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

        private static readonly char[] s_headerValueSplitChars = new char[] { '\r', '\n', ' ' };

        internal static string DecodeHeaderValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string newValue = string.Empty;

            //split strings, they may be folded.  If they are, decode one at a time and append the results
            string[] substringsToDecode = value.Split(s_headerValueSplitChars, StringSplitOptions.RemoveEmptyEntries);

            foreach (string foldedSubString in substringsToDecode)
            {
                //an encoded string has as specific format in that it must start and end with an
                //'=' char and contains five parts, separated by '?' chars.
                //the first and last part are therefore '=', the second part is the byte encoding (B or Q)
                //the third is the unicode encoding type, and the fourth is encoded message itself.  '?' is not valid inside of
                //an encoded string other than as a separator for these five parts.
                //If this check fails, the string is either not encoded or cannot be decoded by this method
                string[] subStrings = foldedSubString.Split('?');
                if ((subStrings.Length != 5 || subStrings[0] != "=" || subStrings[4] != "="))
                {
                    return value;
                }

                string charSet = subStrings[1];
                bool base64Encoding = (subStrings[2] == "B");
                byte[] buffer = Encoding.ASCII.GetBytes(subStrings[3]);
                int newLength;

                IEncodableStream s = EncodedStreamFactory.GetEncoderForHeader(Encoding.GetEncoding(charSet), base64Encoding, 0);

                newLength = s.DecodeBytes(buffer);

                Encoding encoding = Encoding.GetEncoding(charSet);
                newValue += encoding.GetString(buffer, 0, newLength);
            }
            return newValue;
        }

        // Detect the encoding: "=?charset?BorQ?content?="
        // "=?utf-8?B?RmlsZU5hbWVf55CG0Y3Qq9C60I5jw4TRicKq0YIM0Y1hSsSeTNCy0Klh?="; // 3.5
        // With the addition of folding in 4.0, there may be multiple lines with encoding, only detect the first:
        // "=?utf-8?B?RmlsZU5hbWVf55CG0Y3Qq9C60I5jw4TRicKq0YIM0Y1hSsSeTNCy0Klh?=\r\n =?utf-8?B??=";
        //
        // The entire value must consist of one or more well-formed RFC 2047 encoded-words
        // separated by linear whitespace (folding); otherwise null is returned so callers
        // do not treat attacker-controlled input as pre-encoded and pass it through unquoted.
        internal static Encoding? DecodeEncoding(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            ReadOnlySpan<char> remainder = value;
            ReadOnlySpan<char> firstCharSet = default;

            while (true)
            {
                // An encoded-word has the form "=?charset?encoding?text?=".
                // Minimum possible length is "=?x?Q??=" (8 chars).
                if (remainder.Length < 8 || remainder[0] != '=' || remainder[1] != '?')
                {
                    return null;
                }

                // charset = characters up to the next '?'.
                int charSetLength = remainder.Slice(2).IndexOf('?');
                if (charSetLength <= 0)
                {
                    return null;
                }
                ReadOnlySpan<char> charSet = remainder.Slice(2, charSetLength);

                // Validate charset is an RFC 2047 token (no whitespace, controls, or tspecials).
                if (!IsValidEncodedWordToken(charSet))
                {
                    return null;
                }

                int encodingPos = 2 + charSetLength + 1;
                if (encodingPos + 2 >= remainder.Length || remainder[encodingPos + 1] != '?')
                {
                    return null;
                }
                char encoding = remainder[encodingPos];
                if (encoding is not ('B' or 'b' or 'Q' or 'q'))
                {
                    return null;
                }

                // Encoded text: terminated by "?=", and must not contain '?', whitespace,
                // or any non-printable ASCII (per RFC 2047).
                int dataStart = encodingPos + 2;
                int dataEnd = -1;
                for (int i = dataStart; i < remainder.Length - 1; i++)
                {
                    char c = remainder[i];
                    if (c == '?')
                    {
                        if (remainder[i + 1] == '=')
                        {
                            dataEnd = i;
                            break;
                        }
                        return null;
                    }
                    if (c <= ' ' || c >= 127)
                    {
                        return null;
                    }
                }
                if (dataEnd < 0)
                {
                    return null;
                }

                if (firstCharSet.IsEmpty)
                {
                    firstCharSet = charSet;
                }

                remainder = remainder.Slice(dataEnd + 2);
                if (remainder.IsEmpty)
                {
                    break;
                }

                // Multiple encoded-words must be separated by linear whitespace (folding):
                // an optional CRLF followed by one or more SP/HT, or just SP/HT.
                int wsLength = 0;
                if (remainder.Length >= 2 && remainder[0] == '\r' && remainder[1] == '\n')
                {
                    wsLength = 2;
                }
                int wsEnd = wsLength;
                while (wsEnd < remainder.Length && (remainder[wsEnd] == ' ' || remainder[wsEnd] == '\t'))
                {
                    wsEnd++;
                }
                if (wsEnd == wsLength)
                {
                    return null;
                }
                remainder = remainder.Slice(wsEnd);
                if (remainder.IsEmpty)
                {
                    break;
                }
            }

            return Encoding.GetEncoding(firstCharSet.ToString());
        }

        private static bool IsValidEncodedWordToken(ReadOnlySpan<char> token)
        {
            foreach (char c in token)
            {
                // RFC 2047 token: any CHAR except SPACE, CTLs, and especials
                // (especials = "(" / ")" / "<" / ">" / "@" / "," / ";" / ":" /
                //              "\" / <"> / "/" / "[" / "]" / "?" / "." / "=").
                if (c <= ' ' || c >= 127)
                {
                    return false;
                }
                switch (c)
                {
                    case '(':
                    case ')':
                    case '<':
                    case '>':
                    case '@':
                    case ',':
                    case ';':
                    case ':':
                    case '\\':
                    case '"':
                    case '/':
                    case '[':
                    case ']':
                    case '?':
                    case '.':
                    case '=':
                        return false;
                }
            }
            return true;
        }

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

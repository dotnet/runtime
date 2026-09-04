// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Specialized;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mime
{
    internal abstract class MimeBasePart
    {
        internal const string DefaultCharSet = "utf-8";

        // RFC 2047 encoded-word token: any printable ASCII except SPACE, CTLs, and especials
        // (especials = "(" / ")" / "<" / ">" / "@" / "," / ";" / ":" /
        //              "\" / <"> / "/" / "[" / "]" / "?" / "." / "=").
        private static readonly SearchValues<char> s_encodedWordTokenChars =
            SearchValues.Create("!#$%&'*+-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ^_`abcdefghijklmnopqrstuvwxyz{|}~");

        // Valid characters inside the encoded-text section of an RFC 2047 encoded-word:
        // any printable ASCII except SPACE and '?' (which terminates the section).
        private static readonly SearchValues<char> s_encodedWordDataChars =
            SearchValues.Create("!\"#$%&'()*+,-./0123456789:;<=>@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~");

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

        // Decodes a header value of the form "=?charset?BorQ?content?=", optionally folded across
        // multiple lines:
        // "=?utf-8?B?RmlsZU5hbWVf55CG0Y3Qq9C60I5jw4TRicKq0YIM0Y1hSsSeTNCy0Klh?="; // 3.5
        // With the addition of folding in 4.0, there may be multiple lines with encoding:
        // "=?utf-8?B?RmlsZU5hbWVf55CG0Y3Qq9C60I5jw4TRicKq0YIM0Y1hSsSeTNCy0Klh?=\r\n =?utf-8?B??=";
        //
        // The entire value must consist of one or more well-formed RFC 2047 encoded-words
        // separated by linear whitespace (folding); otherwise the value is returned unchanged
        // with a null Encoding.
        internal static (string Value, Encoding? Encoding) DecodeHeaderValue(string? value)
        {
            const int MaxEncodedWordLength = 75;

            if (string.IsNullOrEmpty(value))
            {
                return (string.Empty, null);
            }

            ReadOnlySpan<char> remainder = value;
            Encoding? firstEncoding = null;
            StringBuilder? decodedValue = null;

            while (true)
            {
                // An encoded-word has the form "=?charset?encoding?text?=".
                // Minimum possible length is "=?x?Q??=" (8 chars).
                if (remainder.Length < 8 || remainder[0] != '=' || remainder[1] != '?')
                {
                    return (value, null);
                }

                // charset = characters up to the next '?'.
                int charSetLength = remainder.Slice(2, Math.Min(remainder.Length - 2, MaxEncodedWordLength)).IndexOf('?');
                if (charSetLength <= 0)
                {
                    return (value, null);
                }
                ReadOnlySpan<char> charSet = remainder.Slice(2, charSetLength);

                // Validate charset is an RFC 2047 token (no whitespace, controls, or especials).
                if (charSet.ContainsAnyExcept(s_encodedWordTokenChars))
                {
                    return (value, null);
                }

                int encodingPos = 2 + charSetLength + 1;
                if (encodingPos + 2 >= remainder.Length || remainder[encodingPos + 1] != '?')
                {
                    return (value, null);
                }
                char encodingChar = remainder[encodingPos];
                bool base64Encoding;
                switch (encodingChar)
                {
                    case 'B' or 'b':
                        base64Encoding = true;
                        break;
                    case 'Q' or 'q':
                        base64Encoding = false;
                        break;
                    default:
                        return (value, null);
                }

                // Encoded text: terminated by "?=", and must not contain whitespace or any
                // non-printable ASCII (per RFC 2047).
                int dataStart = encodingPos + 2;
                int terminator = remainder.Slice(dataStart, Math.Min(remainder.Length - dataStart, MaxEncodedWordLength)).IndexOf("?=");
                if (terminator < 0)
                {
                    return (value, null);
                }
                ReadOnlySpan<char> data = remainder.Slice(dataStart, terminator);
                if (data.ContainsAnyExcept(s_encodedWordDataChars))
                {
                    return (value, null);
                }

                // For Q-encoding, every '=' must be followed by exactly two hex digits.
                // QEncodedStream.DecodeBytes validates the digits themselves, but a '='
                // within the last two characters can never have two digits following it,
                // so reject that shape here rather than passing a truncated escape to the
                // decoder (which is only ever invoked once per encoded-word and would
                // otherwise decode everything up to the truncated escape, silently
                // dropping it, instead of recognizing the whole value as malformed).
                if (!base64Encoding)
                {
                    ReadOnlySpan<char> tail = data.Length > 2 ? data.Slice(data.Length - 2) : data;
                    if (tail.Contains('='))
                    {
                        return (value, null);
                    }
                }

                Encoding wordEncoding = Encoding.GetEncoding(charSet.ToString());
                firstEncoding ??= wordEncoding;

                byte[] buffer = Encoding.ASCII.GetBytes(data.ToString());
                IEncodableStream s = EncodedStreamFactory.GetEncoderForHeader(wordEncoding, base64Encoding, 0);
                int newLength = s.DecodeBytes(buffer);
                (decodedValue ??= new StringBuilder()).Append(wordEncoding.GetString(buffer, 0, newLength));

                if (dataStart + terminator + 2 > MaxEncodedWordLength)
                {
                    return (value, null);
                }

                remainder = remainder.Slice(dataStart + terminator + 2);
                if (remainder.IsEmpty)
                {
                    break;
                }

                // Multiple encoded-words must be separated by linear whitespace (folding):
                // an optional CRLF followed by one or more SP/HT, or just SP/HT.
                bool hasNewLine = remainder.Length >= 2 && remainder[0] == '\r' && remainder[1] == '\n';
                int whiteSpacesLength = hasNewLine ? 2 : 0;
                while (whiteSpacesLength < remainder.Length && (remainder[whiteSpacesLength] == ' ' || remainder[whiteSpacesLength] == '\t'))
                {
                    whiteSpacesLength++;
                }
                // CRLF NOT followed by at least one SP/HT
                if (hasNewLine && whiteSpacesLength == 2)
                {
                    return (value, null);
                }
                remainder = remainder.Slice(whiteSpacesLength);
                if (remainder.IsEmpty)
                {
                    break;
                }
            }

            return (decodedValue!.ToString(), firstEncoding);
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

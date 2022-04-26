// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.Text;
using System.Net.Mail;

namespace System.Net.Mime
{
    internal class MimeBasePart
    {
        internal const string DefaultCharSet = "utf-8";
        private static readonly char[] s_decodeEncodingSplitChars = new char[] { '?', '\r', '\n' };

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
        private static readonly char[] s_questionMarkSplitChars = new char[] { '?' };

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
                string[] subStrings = foldedSubString.Split(s_questionMarkSplitChars);
                if ((subStrings.Length != 5 || subStrings[0] != "=" || subStrings[4] != "="))
                {
                    return value;
                }

                string charSet = subStrings[1];
                bool base64Encoding = (subStrings[2] == "B");
                byte[] buffer = Encoding.ASCII.GetBytes(subStrings[3]);
                int newLength;

                IEncodableStream s = EncodedStreamFactory.GetEncoderForHeader(Encoding.GetEncoding(charSet), base64Encoding, 0);

                newLength = s.DecodeBytes(buffer, 0, buffer.Length);

                Encoding encoding = Encoding.GetEncoding(charSet);
                newValue += encoding.GetString(buffer, 0, newLength);
            }
            return newValue;
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

            string[] subStrings = value.Split(s_decodeEncodingSplitChars);
            if ((subStrings.Length < 5 || subStrings[0] != "=" || subStrings[4] != "="))
            {
                return null;
            }

            string charSet = subStrings[1];
            return Encoding.GetEncoding(charSet);
        }

        internal static bool IsAscii(string value, bool permitCROrLF)
        {
            ArgumentNullException.ThrowIfNull(value);

            foreach (char c in value)
            {
                if (c > 0x7f)
                {
                    return false;
                }
                if (!permitCROrLF && (c == '\r' || c == '\n'))
                {
                    return false;
                }
            }
            return true;
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
                if (_headers == null)
                {
                    _headers = new HeaderCollection();
                }

                if (_contentType == null)
                {
                    _contentType = new ContentType();
                }
                _contentType.PersistIfNeeded(_headers, false);

                if (_contentDisposition != null)
                {
                    _contentDisposition.PersistIfNeeded(_headers, false);
                }

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

        internal virtual void Send(BaseWriter writer, bool allowUnicode)
        {
            throw new NotImplementedException();
        }

        internal virtual IAsyncResult BeginSend(BaseWriter writer, AsyncCallback? callback,
            bool allowUnicode, object? state)
        {
            throw new NotImplementedException();
        }

        internal void EndSend(IAsyncResult asyncResult)
        {
            ArgumentNullException.ThrowIfNull(asyncResult);

            LazyAsyncResult? castedAsyncResult = asyncResult as MimePartAsyncResult;

            if (castedAsyncResult == null || castedAsyncResult.AsyncObject != this)
            {
                throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));
            }

            if (castedAsyncResult.EndCalled)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, nameof(EndSend)));
            }

            castedAsyncResult.InternalWaitForCompletion();
            castedAsyncResult.EndCalled = true;
            if (castedAsyncResult.Result is Exception)
            {
                throw (Exception)castedAsyncResult.Result;
            }
        }

        internal sealed class MimePartAsyncResult : LazyAsyncResult
        {
            internal MimePartAsyncResult(MimeBasePart part, object? state, AsyncCallback? callback) : base(part, state, callback)
            {
            }
        }
    }
}

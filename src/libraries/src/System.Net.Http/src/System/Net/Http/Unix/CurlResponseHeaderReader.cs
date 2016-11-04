// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Net.Http
{
    internal struct CurlResponseHeaderReader
    {
        private const string HttpPrefix = "HTTP/";

        private readonly HeaderBufferSpan _span;

        public CurlResponseHeaderReader(IntPtr buffer, ulong size)
        {
            Debug.Assert(buffer != IntPtr.Zero);
            Debug.Assert(size <= int.MaxValue);

            _span = new HeaderBufferSpan(buffer, (int)size).Trim();
        }

        public bool ReadStatusLine(HttpResponseMessage response)
        {
            if (!_span.StartsWithHttpPrefix())
            {
                return false;
            }

            int index = HttpPrefix.Length;
            int majorVersion = _span.ReadInt(ref index);
            CheckResponseMsgFormat(majorVersion != 0);

            CheckResponseMsgFormat(index < _span.Length && _span[index] == '.');
            index++;

            // Need minor version.
            CheckResponseMsgFormat(index < _span.Length && _span[index] >= '0' && _span[index] <= '9');
            int minorVersion = _span.ReadInt(ref index);

            CheckResponseMsgFormat(_span.SkipSpace(ref index));

            // Parse status code.
            int statusCode = _span.ReadInt(ref index);
            CheckResponseMsgFormat(statusCode >= 100 && statusCode < 1000);

            bool foundSpace = _span.SkipSpace(ref index);
            CheckResponseMsgFormat(index <= _span.Length);
            CheckResponseMsgFormat(foundSpace || index == _span.Length);

            // Set the response HttpVersion.
            response.Version =
                (majorVersion == 1 && minorVersion == 1) ? HttpVersionInternal.Version11 :
                (majorVersion == 1 && minorVersion == 0) ? HttpVersionInternal.Version10 :
                (majorVersion == 2 && minorVersion == 0) ? HttpVersionInternal.Version20 :
                HttpVersionInternal.Unknown;

            response.StatusCode = (HttpStatusCode)statusCode;

            // Try to use a known reason phrase instead of allocating a new string.
            HeaderBufferSpan reasonPhraseSpan = _span.Substring(index);
            string knownReasonPhrase = HttpStatusDescription.Get(response.StatusCode);
            response.ReasonPhrase = reasonPhraseSpan.EqualsOrdinal(knownReasonPhrase) ?
                knownReasonPhrase :
                reasonPhraseSpan.ToString();

            return true;
        }

        public bool ReadHeader(out string headerName, out string headerValue)
        {
            int index = 0;
            while (index < _span.Length && ValidHeaderNameChar(_span[index]))
            {
                index++;
            }

            if (index > 0)
            {
                // For compatability, skip past any whitespace before the colon, even though
                // the RFC suggests there shouldn't be any.
                int headerNameLength = index;
                while (index < _span.Length && IsWhiteSpaceLatin1(_span[index]))
                {
                    index++;
                }

                CheckResponseMsgFormat(index < _span.Length);
                CheckResponseMsgFormat(_span[index] == ':');
                HeaderBufferSpan headerNameSpan = _span.Substring(0, headerNameLength);
                if (!HttpKnownHeaderNames.TryGetHeaderName(_span.Buffer, _span.Length, out headerName))
                {
                    headerName = headerNameSpan.ToString();
                }
                CheckResponseMsgFormat(headerName.Length > 0);

                index++;
                headerValue = _span.Substring(index).Trim().ToString();
                return true;
            }

            headerName = null;
            headerValue = null;
            return false;
        }

        private static void CheckResponseMsgFormat(bool condition)
        {
            if (!condition)
            {
                throw new HttpRequestException(SR.net_http_unix_invalid_response);
            }
        }

        private static bool ValidHeaderNameChar(byte c)
        {
            const string invalidChars = "()<>@,;:\\\"/[]?={}";
            return c > ' ' && invalidChars.IndexOf((char)c) < 0;
        }

        internal static bool IsWhiteSpaceLatin1(byte c)
        {
            // SPACE
            // U+0009 = <control> HORIZONTAL TAB
            // U+000a = <control> LINE FEED
            // U+000b = <control> VERTICAL TAB
            // U+000c = <control> FORM FEED
            // U+000d = <control> CARRIAGE RETURN
            // U+0085 = <control> NEXT LINE
            // U+00a0 = NO-BREAK SPACE
            return c == ' ' || (c >= '\x0009' && c <= '\x000d') || c == '\x00a0' || c == '\x0085';
        }

        private unsafe struct HeaderBufferSpan
        {
            private readonly byte* _pointer;
            public readonly int Length;

            public static readonly HeaderBufferSpan Empty = default(HeaderBufferSpan);

            public HeaderBufferSpan(IntPtr pointer, int length)
                : this((byte*)pointer, length)
            {
            }

            public HeaderBufferSpan(byte* pointer, int length)
            {
                Debug.Assert(pointer != null);
                Debug.Assert(length >= 0);

                _pointer = pointer;
                Length = length;
            }

            public IntPtr Buffer => new IntPtr(_pointer);

            public byte this[int index]
            {
                get
                {
                    Debug.Assert(index >= 0 && index < Length);
                    return _pointer[index];
                }
            }

            public HeaderBufferSpan Trim()
            {
                if (Length == 0)
                {
                    return Empty;
                }

                int index = 0;
                while (index < Length && IsWhiteSpaceLatin1(_pointer[index]))
                {
                    index++;
                }

                int end = Length - 1;
                while (end >= index && IsWhiteSpaceLatin1(_pointer[end]))
                {
                    end--;
                }

                byte* pointer = _pointer + index;
                int length = end - index + 1;

                return new HeaderBufferSpan(pointer, length);
            }

            public bool StartsWithHttpPrefix()
            {
                if (Length < HttpPrefix.Length)
                {
                    return false;
                }

                return
                    (_pointer[0] == 'H' || _pointer[0] == 'h') &&
                    (_pointer[1] == 'T' || _pointer[1] == 't') &&
                    (_pointer[2] == 'T' || _pointer[2] == 't') &&
                    (_pointer[3] == 'P' || _pointer[3] == 'p') &&
                    (_pointer[4] == '/');
            }

            public int ReadInt(ref int index)
            {
                int value = 0;
                for (; index < Length; index++)
                {
                    byte c = _pointer[index];
                    if (c < '0' || c > '9')
                    {
                        break;
                    }

                    value = (value * 10) + (c - '0');
                }

                return value;
            }

            public bool SkipSpace(ref int index)
            {
                bool foundSpace = false;
                for (; index < Length; index++)
                {
                    if (_pointer[index] == ' ' || _pointer[index] == '\t')
                    {
                        foundSpace = true;
                    }
                    else
                    {
                        break;
                    }
                }
                return foundSpace;
            }

            public bool EqualsOrdinal(string value)
            {
                if (value == null)
                {
                    return false;
                }

                if (Length != value.Length)
                {
                    return false;
                }

                for (int i = 0; i < Length; i++)
                {
                    if (_pointer[i] != value[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public HeaderBufferSpan Substring(int startIndex)
            {
                return Substring(startIndex, Length - startIndex);
            }

            public HeaderBufferSpan Substring(int startIndex, int length)
            {
                Debug.Assert(startIndex >= 0);
                Debug.Assert(length >= 0);
                Debug.Assert(startIndex <= Length - length);

                if (length == 0)
                {
                    return Empty;
                }

                if (startIndex == 0 && length == Length)
                {
                    return this;
                }

                return new HeaderBufferSpan(_pointer + startIndex, length);
            }

            public override string ToString()
            {
                return Length == 0 ? string.Empty : HttpRuleParser.DefaultHttpEncoding.GetString(_pointer, Length);
            }
        }
    }
}

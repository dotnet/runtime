// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace System.Net
{
    internal enum CookieToken
    {
        // State types
        Nothing,
        NameValuePair,  // X=Y
        Attribute,      // X
        EndToken,       // ';'
        EndCookie,      // ','
        End,            // EOLN
        Equals,

        // Value types
        Comment,
        CommentUrl,
        CookieName,
        Discard,
        Domain,
        Expires,
        MaxAge,
        Path,
        Port,
        Secure,
        HttpOnly,
        Unknown,
        Version
    }

    // CookieTokenizer
    //
    // Used to split a single or multi-cookie (header) string into individual
    // tokens.
    internal class CookieTokenizer
    {
        private bool _eofCookie;
        private int _index;
        private int _length;
        private string _name;
        private bool _quoted;
        private int _start;
        private CookieToken _token;
        private int _tokenLength;
        private string _tokenStream;
        private string _value;
        private int _cookieStartIndex;
        private int _cookieLength;

        internal CookieTokenizer(string tokenStream)
        {
            _length = tokenStream.Length;
            _tokenStream = tokenStream;
        }

        internal bool EndOfCookie
        {
            get
            {
                return _eofCookie;
            }
            set
            {
                _eofCookie = value;
            }
        }

        internal bool Eof
        {
            get
            {
                return _index >= _length;
            }
        }

        internal string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        internal bool Quoted
        {
            get
            {
                return _quoted;
            }
            set
            {
                _quoted = value;
            }
        }

        internal CookieToken Token
        {
            get
            {
                return _token;
            }
            set
            {
                _token = value;
            }
        }

        internal string Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        // GetCookieString
        //
        // Gets the full string of the cookie
        internal string GetCookieString()
        {
            return _tokenStream.SubstringTrim(_cookieStartIndex, _cookieLength);
        }

        // Extract
        //
        // Extracts the current token
        internal string Extract()
        {
            string tokenString = string.Empty;

            if (_tokenLength != 0)
            {
                tokenString = Quoted ?
                    _tokenStream.Substring(_start, _tokenLength) :
                    _tokenStream.SubstringTrim(_start, _tokenLength);
            }
            return tokenString;
        }

        // FindNext
        //
        // Find the start and length of the next token. The token is terminated
        // by one of:
        //     - end-of-line
        //     - end-of-cookie: unquoted comma separates multiple cookies
        //     - end-of-token: unquoted semi-colon
        //     - end-of-name: unquoted equals
        //
        // Inputs:
        // <argument>  ignoreComma
        //     true if parsing doesn't stop at a comma. This is only true when
        //     we know we're parsing an original cookie that has an expires=
        //     attribute, because the format of the time/date used in expires
        //     is:
        //         Wdy, dd-mmm-yyyy HH:MM:SS GMT
        //
        // <argument>  ignoreEquals
        //     true if parsing doesn't stop at an equals sign. The LHS of the
        //     first equals sign is an attribute name. The next token may
        //     include one or more equals signs. For example:
        //          SESSIONID=ID=MSNx45&q=33
        //
        // Outputs:
        // <member>    _index
        //     incremented to the last position in _tokenStream contained by
        //     the current token
        //
        // <member>    _start
        //     incremented to the start of the current token
        //
        // <member>    _tokenLength
        //     set to the length of the current token
        //
        // Assumes: Nothing
        //
        // Returns:
        // type of CookieToken found:
        //
        //     End         - end of the cookie string
        //     EndCookie   - end of current cookie in (potentially) a
        //                   multi-cookie string
        //     EndToken    - end of name=value pair, or end of an attribute
        //     Equals      - end of name=
        //
        // Throws: Nothing
        internal CookieToken FindNext(bool ignoreComma, bool ignoreEquals)
        {
            _tokenLength = 0;
            _start = _index;
            while ((_index < _length) && Char.IsWhiteSpace(_tokenStream[_index]))
            {
                ++_index;
                ++_start;
            }

            CookieToken token = CookieToken.End;
            int increment = 1;

            if (!Eof)
            {
                if (_tokenStream[_index] == '"')
                {
                    Quoted = true;
                    ++_index;
                    bool quoteOn = false;
                    while (_index < _length)
                    {
                        char currChar = _tokenStream[_index];
                        if (!quoteOn && currChar == '"')
                        {
                            break;
                        }

                        if (quoteOn)
                        {
                            quoteOn = false;
                        }
                        else if (currChar == '\\')
                        {
                            quoteOn = true;
                        }
                        ++_index;
                    }
                    if (_index < _length)
                    {
                        ++_index;
                    }
                    _tokenLength = _index - _start;
                    increment = 0;
                    // If we are here, reset ignoreComma.
                    // In effect, we ignore everything after quoted string until the next delimiter.
                    ignoreComma = false;
                }
                while ((_index < _length)
                       && (_tokenStream[_index] != ';')
                       && (ignoreEquals || (_tokenStream[_index] != '='))
                       && (ignoreComma || (_tokenStream[_index] != ',')))
                {
                    // Fixing 2 things:
                    // 1) ignore day of week in cookie string
                    // 2) revert ignoreComma once meet it, so won't miss the next cookie)
                    if (_tokenStream[_index] == ',')
                    {
                        _start = _index + 1;
                        _tokenLength = -1;
                        ignoreComma = false;
                    }
                    ++_index;
                    _tokenLength += increment;
                }
                if (!Eof)
                {
                    switch (_tokenStream[_index])
                    {
                        case ';':
                            token = CookieToken.EndToken;
                            break;

                        case '=':
                            token = CookieToken.Equals;
                            break;

                        default:
                            _cookieLength = _index - _cookieStartIndex;
                            token = CookieToken.EndCookie;
                            break;
                    }
                    ++_index;
                }
                
                if (Eof)
                {
                    _cookieLength = _index - _cookieStartIndex;
                }
            }
            return token;
        }

        // Next
        //
        // Get the next cookie name/value or attribute
        //
        // Cookies come in the following formats:
        //
        //     1. Version0
        //         Set-Cookie: [<name>][=][<value>]
        //                     [; expires=<date>]
        //                     [; path=<path>]
        //                     [; domain=<domain>]
        //                     [; secure]
        //         Cookie: <name>=<value>
        //
        //         Notes: <name> and/or <value> may be blank
        //                <date> is the RFC 822/1123 date format that
        //                incorporates commas, e.g.
        //                "Wednesday, 09-Nov-99 23:12:40 GMT"
        //
        //     2. RFC 2109
        //         Set-Cookie: 1#{
        //                         <name>=<value>
        //                         [; comment=<comment>]
        //                         [; domain=<domain>]
        //                         [; max-age=<seconds>]
        //                         [; path=<path>]
        //                         [; secure]
        //                         ; Version=<version>
        //                     }
        //         Cookie: $Version=<version>
        //                 1#{
        //                     ; <name>=<value>
        //                     [; path=<path>]
        //                     [; domain=<domain>]
        //                 }
        //
        //     3. RFC 2965
        //         Set-Cookie2: 1#{
        //                         <name>=<value>
        //                         [; comment=<comment>]
        //                         [; commentURL=<comment>]
        //                         [; discard]
        //                         [; domain=<domain>]
        //                         [; max-age=<seconds>]
        //                         [; path=<path>]
        //                         [; ports=<portlist>]
        //                         [; secure]
        //                         ; Version=<version>
        //                      }
        //         Cookie: $Version=<version>
        //                 1#{
        //                     ; <name>=<value>
        //                     [; path=<path>]
        //                     [; domain=<domain>]
        //                     [; port="<port>"]
        //                 }
        //         [Cookie2: $Version=<version>]
        //
        // Inputs:
        // <argument>  first
        //     true if this is the first name/attribute that we have looked for
        //     in the cookie stream
        //
        // Outputs:
        //
        // Assumes:
        // Nothing
        //
        // Returns:
        // type of CookieToken found:
        //
        //     - Attribute
        //         - token was single-value. May be empty. Caller should check
        //           Eof or EndCookie to determine if any more action needs to
        //           be taken
        //
        //     - NameValuePair
        //         - Name and Value are meaningful. Either may be empty
        //
        // Throws:
        // Nothing
        internal CookieToken Next(bool first, bool parseResponseCookies)
        {
            Reset();

            if (first)
            {
                _cookieStartIndex = _index;
                _cookieLength = 0;
            }

            CookieToken terminator = FindNext(false, false);
            if (terminator == CookieToken.EndCookie)
            {
                EndOfCookie = true;
            }

            if ((terminator == CookieToken.End) || (terminator == CookieToken.EndCookie))
            {
                if ((Name = Extract()).Length != 0)
                {
                    Token = TokenFromName(parseResponseCookies);
                    return CookieToken.Attribute;
                }
                return terminator;
            }
            Name = Extract();
            if (first)
            {
                Token = CookieToken.CookieName;
            }
            else
            {
                Token = TokenFromName(parseResponseCookies);
            }
            if (terminator == CookieToken.Equals)
            {
                terminator = FindNext(!first && (Token == CookieToken.Expires), true);
                if (terminator == CookieToken.EndCookie)
                {
                    EndOfCookie = true;
                }
                Value = Extract();
                return CookieToken.NameValuePair;
            }
            else
            {
                return CookieToken.Attribute;
            }
        }

        // Reset
        //
        // Sets this tokenizer up for finding the next name/value pair,
        // attribute, or end-of-{token,cookie,line}.
        internal void Reset()
        {
            _eofCookie = false;
            _name = string.Empty;
            _quoted = false;
            _start = _index;
            _token = CookieToken.Nothing;
            _tokenLength = 0;
            _value = string.Empty;
        }

        private struct RecognizedAttribute
        {
            private string _name;
            private CookieToken _token;

            internal RecognizedAttribute(string name, CookieToken token)
            {
                _name = name;
                _token = token;
            }

            internal CookieToken Token
            {
                get
                {
                    return _token;
                }
            }

            internal bool IsEqualTo(string value)
            {
                return string.Equals(_name, value, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Recognized attributes in order of expected frequency.
        private static readonly RecognizedAttribute[] s_recognizedAttributes = {
            new RecognizedAttribute(CookieFields.PathAttributeName, CookieToken.Path),
            new RecognizedAttribute(CookieFields.MaxAgeAttributeName, CookieToken.MaxAge),
            new RecognizedAttribute(CookieFields.ExpiresAttributeName, CookieToken.Expires),
            new RecognizedAttribute(CookieFields.VersionAttributeName, CookieToken.Version),
            new RecognizedAttribute(CookieFields.DomainAttributeName, CookieToken.Domain),
            new RecognizedAttribute(CookieFields.SecureAttributeName, CookieToken.Secure),
            new RecognizedAttribute(CookieFields.DiscardAttributeName, CookieToken.Discard),
            new RecognizedAttribute(CookieFields.PortAttributeName, CookieToken.Port),
            new RecognizedAttribute(CookieFields.CommentAttributeName, CookieToken.Comment),
            new RecognizedAttribute(CookieFields.CommentUrlAttributeName, CookieToken.CommentUrl),
            new RecognizedAttribute(CookieFields.HttpOnlyAttributeName, CookieToken.HttpOnly),
        };

        private static readonly RecognizedAttribute[] s_recognizedServerAttributes = {
            new RecognizedAttribute('$' + CookieFields.PathAttributeName, CookieToken.Path),
            new RecognizedAttribute('$' + CookieFields.VersionAttributeName, CookieToken.Version),
            new RecognizedAttribute('$' + CookieFields.DomainAttributeName, CookieToken.Domain),
            new RecognizedAttribute('$' + CookieFields.PortAttributeName, CookieToken.Port),
            new RecognizedAttribute('$' + CookieFields.HttpOnlyAttributeName, CookieToken.HttpOnly),
        };

        internal CookieToken TokenFromName(bool parseResponseCookies)
        {
            if (!parseResponseCookies)
            {
                for (int i = 0; i < s_recognizedServerAttributes.Length; ++i)
                {
                    if (s_recognizedServerAttributes[i].IsEqualTo(Name))
                    {
                        return s_recognizedServerAttributes[i].Token;
                    }
                }
            }
            else
            {
                for (int i = 0; i < s_recognizedAttributes.Length; ++i)
                {
                    if (s_recognizedAttributes[i].IsEqualTo(Name))
                    {
                        return s_recognizedAttributes[i].Token;
                    }
                }
            }
            return CookieToken.Unknown;
        }
    }

    // CookieParser
    //
    // Takes a cookie header, makes cookies.
    internal class CookieParser
    {
        private CookieTokenizer _tokenizer;
        private Cookie _savedCookie;

        internal CookieParser(string cookieString)
        {
            _tokenizer = new CookieTokenizer(cookieString);
        }

        // GetString
        //
        // Gets the next cookie string
        internal string GetString()
        {
            bool first = true;

            if (_tokenizer.Eof)
            {
                return null;
            }

            do
            {
                _tokenizer.Next(first, true);
                first = false;
            } while (!_tokenizer.Eof && !_tokenizer.EndOfCookie);

            return _tokenizer.GetCookieString();
        }

#if SYSTEM_NET_PRIMITIVES_DLL
        private static bool InternalSetNameMethod(Cookie cookie, string value)
        {
            return cookie.InternalSetName(value);
        }
#else
        private static Func<Cookie, string, bool> s_internalSetNameMethod;
        private static Func<Cookie, string, bool> InternalSetNameMethod
        {
            get
            {
                if (s_internalSetNameMethod == null)
                {
                    // TODO: #13607
                    // We need to use Cookie.InternalSetName instead of the Cookie.set_Name wrapped in a try catch block, as
                    // Cookie.set_Name keeps the original name if the string is empty or null.
                    // Unfortunately this API is internal so we use reflection to access it. The method is cached for performance reasons.
                    BindingFlags flags = BindingFlags.Instance;
#if uap
                    flags |= BindingFlags.Public;
#else
                    flags |= BindingFlags.NonPublic;
#endif
                    MethodInfo method = typeof(Cookie).GetMethod("InternalSetName", flags);
                    Debug.Assert(method != null, "We need to use an internal method named InternalSetName that is declared on Cookie.");
                    s_internalSetNameMethod = (Func<Cookie, string, bool>)Delegate.CreateDelegate(typeof(Func<Cookie, string, bool>), method);
                }

                return s_internalSetNameMethod;
            }
        }
#endif

        private static FieldInfo s_isQuotedDomainField = null;
        private static FieldInfo IsQuotedDomainField
        {
            get
            {
                if (s_isQuotedDomainField == null)
                {
                    // TODO: #13607
                    BindingFlags flags = BindingFlags.Instance;
#if uap
                    flags |= BindingFlags.Public;
#else
                    flags |= BindingFlags.NonPublic;
#endif
                    FieldInfo field = typeof(Cookie).GetField("IsQuotedDomain", flags);
                    Debug.Assert(field != null, "We need to use an internal field named IsQuotedDomain that is declared on Cookie.");
                    s_isQuotedDomainField = field;
                }

                return s_isQuotedDomainField;
            }
        }

        private static FieldInfo s_isQuotedVersionField = null;
        private static FieldInfo IsQuotedVersionField
        {
            get
            {
                if (s_isQuotedVersionField == null)
                {
                    // TODO: #13607
                    BindingFlags flags = BindingFlags.Instance;
#if uap
                    flags |= BindingFlags.Public;
#else
                    flags |= BindingFlags.NonPublic;
#endif
                    FieldInfo field = typeof(Cookie).GetField("IsQuotedVersion", flags);
                    Debug.Assert(field != null, "We need to use an internal field named IsQuotedVersion that is declared on Cookie.");
                    s_isQuotedVersionField = field;
                }

                return s_isQuotedVersionField;
            }
        }

        // Get
        //
        // Gets the next cookie or null if there are no more cookies.
        internal Cookie Get()
        {
            Cookie cookie = null;

            // Only the first occurrence of an attribute value must be counted.
            bool commentSet = false;
            bool commentUriSet = false;
            bool domainSet = false;
            bool expiresSet = false;
            bool pathSet = false;
            bool portSet = false; // Special case: may have no value in header.
            bool versionSet = false;
            bool secureSet = false;
            bool discardSet = false;

            do
            {
                CookieToken token = _tokenizer.Next(cookie == null, true);
                if (cookie == null && (token == CookieToken.NameValuePair || token == CookieToken.Attribute))
                {
                    cookie = new Cookie();
                    InternalSetNameMethod(cookie, _tokenizer.Name);
                    cookie.Value = _tokenizer.Value;
                }
                else
                {
                    switch (token)
                    {
                        case CookieToken.NameValuePair:
                            switch (_tokenizer.Token)
                            {
                                case CookieToken.Comment:
                                    if (!commentSet)
                                    {
                                        commentSet = true;
                                        cookie.Comment = _tokenizer.Value;
                                    }
                                    break;

                                case CookieToken.CommentUrl:
                                    if (!commentUriSet)
                                    {
                                        commentUriSet = true;
                                        if (Uri.TryCreate(CheckQuoted(_tokenizer.Value), UriKind.Absolute, out Uri parsed))
                                        {
                                            cookie.CommentUri = parsed;
                                        }
                                    }
                                    break;

                                case CookieToken.Domain:
                                    if (!domainSet)
                                    {
                                        domainSet = true;
                                        cookie.Domain = CheckQuoted(_tokenizer.Value);
                                        IsQuotedDomainField.SetValue(cookie, _tokenizer.Quoted);
                                    }
                                    break;

                                case CookieToken.Expires:
                                    if (!expiresSet)
                                    {
                                        expiresSet = true;

                                        if (DateTime.TryParse(CheckQuoted(_tokenizer.Value),
                                            CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime expires))
                                        {
                                            cookie.Expires = expires;
                                        }
                                        else
                                        {
                                            // This cookie will be rejected
                                            InternalSetNameMethod(cookie, string.Empty);
                                        }
                                    }
                                    break;

                                case CookieToken.MaxAge:
                                    if (!expiresSet)
                                    {
                                        expiresSet = true;
                                        if (int.TryParse(CheckQuoted(_tokenizer.Value), out int parsed))
                                        {
                                            cookie.Expires = DateTime.Now.AddSeconds(parsed);
                                        }
                                        else
                                        {
                                            // This cookie will be rejected
                                            InternalSetNameMethod(cookie, string.Empty);
                                        }
                                    }
                                    break;

                                case CookieToken.Path:
                                    if (!pathSet)
                                    {
                                        pathSet = true;
                                        cookie.Path = _tokenizer.Value;
                                    }
                                    break;

                                case CookieToken.Port:
                                    if (!portSet)
                                    {
                                        portSet = true;
                                        try
                                        {
                                            cookie.Port = _tokenizer.Value;
                                        }
                                        catch
                                        {
                                            // This cookie will be rejected
                                            InternalSetNameMethod(cookie, string.Empty);
                                        }
                                    }
                                    break;

                                case CookieToken.Version:
                                    if (!versionSet)
                                    {
                                        versionSet = true;
                                        int parsed;
                                        if (int.TryParse(CheckQuoted(_tokenizer.Value), out parsed))
                                        {
                                            cookie.Version = parsed;
                                            IsQuotedVersionField.SetValue(cookie, _tokenizer.Quoted);
                                        }
                                        else
                                        {
                                            // This cookie will be rejected
                                            InternalSetNameMethod(cookie, string.Empty);
                                        }
                                    }
                                    break;
                            }
                            break;

                        case CookieToken.Attribute:
                            switch (_tokenizer.Token)
                            {
                                case CookieToken.Discard:
                                    if (!discardSet)
                                    {
                                        discardSet = true;
                                        cookie.Discard = true;
                                    }
                                    break;

                                case CookieToken.Secure:
                                    if (!secureSet)
                                    {
                                        secureSet = true;
                                        cookie.Secure = true;
                                    }
                                    break;

                                case CookieToken.HttpOnly:
                                    cookie.HttpOnly = true;
                                    break;

                                case CookieToken.Port:
                                    if (!portSet)
                                    {
                                        portSet = true;
                                        cookie.Port = string.Empty;
                                    }
                                    break;
                            }
                            break;
                    }
                }
            } while (!_tokenizer.Eof && !_tokenizer.EndOfCookie);

            return cookie;
        }

        internal Cookie GetServer()
        {
            Cookie cookie = _savedCookie;
            _savedCookie = null;

            // Only the first occurrence of an attribute value must be counted.
            bool domainSet = false;
            bool pathSet = false;
            bool portSet = false; // Special case: may have no value in header.

            do
            {
                bool first = cookie == null || string.IsNullOrEmpty(cookie.Name);
                CookieToken token = _tokenizer.Next(first, false);

                if (first && (token == CookieToken.NameValuePair || token == CookieToken.Attribute))
                {
                    if (cookie == null)
                    {
                        cookie = new Cookie();
                    }
                    InternalSetNameMethod(cookie, _tokenizer.Name);
                    cookie.Value = _tokenizer.Value;
                }
                else
                {
                    switch (token)
                    {
                        case CookieToken.NameValuePair:
                            switch (_tokenizer.Token)
                            {
                                case CookieToken.Domain:
                                    if (!domainSet)
                                    {
                                        domainSet = true;
                                        cookie.Domain = CheckQuoted(_tokenizer.Value);
                                        IsQuotedDomainField.SetValue(cookie, _tokenizer.Quoted);
                                    }
                                    break;

                                case CookieToken.Path:
                                    if (!pathSet)
                                    {
                                        pathSet = true;
                                        cookie.Path = _tokenizer.Value;
                                    }
                                    break;

                                case CookieToken.Port:
                                    if (!portSet)
                                    {
                                        portSet = true;
                                        try
                                        {
                                            cookie.Port = _tokenizer.Value;
                                        }
                                        catch (CookieException)
                                        {
                                            // This cookie will be rejected
                                            InternalSetNameMethod(cookie, string.Empty);
                                        }
                                    }
                                    break;

                                case CookieToken.Version:
                                    // this is a new cookie, this token is for the next cookie.
                                    _savedCookie = new Cookie();
                                    if (int.TryParse(_tokenizer.Value, out int parsed))
                                    {
                                        _savedCookie.Version = parsed;
                                    }
                                    return cookie;

                                case CookieToken.Unknown:
                                    // this is a new cookie, the token is for the next cookie.
                                    _savedCookie = new Cookie();
                                    InternalSetNameMethod(_savedCookie, _tokenizer.Name);
                                    _savedCookie.Value = _tokenizer.Value;
                                    return cookie;
                            }
                            break;

                        case CookieToken.Attribute:
                            if (_tokenizer.Token == CookieToken.Port && !portSet)
                            {
                                portSet = true;
                                cookie.Port = string.Empty;
                            }
                            break;
                    }
                }
            } while (!_tokenizer.Eof && !_tokenizer.EndOfCookie);
            return cookie;
        }

        internal static string CheckQuoted(string value)
        {
            if (value.Length < 2 || value[0] != '\"' || value[value.Length - 1] != '\"')
                return value;

            return value.Length == 2 ? string.Empty : value.Substring(1, value.Length - 2);
        }
        
        internal bool EndofHeader()
        {
            return _tokenizer.Eof;
        }
    }
}

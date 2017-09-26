// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System
{
    public partial class Uri
    {
        //
        // All public ctors go through here
        //
        private void CreateThis(string uri, bool dontEscape, UriKind uriKind)
        {
            // if (!Enum.IsDefined(typeof(UriKind), uriKind)) -- We currently believe that Enum.IsDefined() is too slow 
            // to be used here.
            if ((int)uriKind < (int)UriKind.RelativeOrAbsolute || (int)uriKind > (int)UriKind.Relative)
            {
                throw new ArgumentException(SR.Format(SR.net_uri_InvalidUriKind, uriKind));
            }

            _string = uri == null ? string.Empty : uri;

            if (dontEscape)
                _flags |= Flags.UserEscaped;

            ParsingError err = ParseScheme(_string, ref _flags, ref _syntax);
            UriFormatException e;

            InitializeUri(err, uriKind, out e);
            if (e != null)
                throw e;
        }

        private void InitializeUri(ParsingError err, UriKind uriKind, out UriFormatException e)
        {
            if (err == ParsingError.None)
            {
                if (IsImplicitFile)
                {
                    // V1 compat
                    // A relative Uri wins over implicit UNC path unless the UNC path is of the form "\\something" and 
                    // uriKind != Absolute
                    // A relative Uri wins over implicit Unix path unless uriKind == Absolute
                    if (NotAny(Flags.DosPath) &&
                        uriKind != UriKind.Absolute &&
                       ((uriKind == UriKind.Relative || (_string.Length >= 2 && (_string[0] != '\\' || _string[1] != '\\')))
                    || (!IsWindowsSystem && InFact(Flags.UnixPath))))
                    {
                        _syntax = null; //make it be relative Uri
                        _flags &= Flags.UserEscaped; // the only flag that makes sense for a relative uri
                        e = null;
                        return;
                        // Otherwise an absolute file Uri wins when it's of the form "\\something"
                    }
                    //
                    // V1 compat issue
                    // We should support relative Uris of the form c:\bla or c:/bla
                    //
                    else if (uriKind == UriKind.Relative && InFact(Flags.DosPath))
                    {
                        _syntax = null; //make it be relative Uri
                        _flags &= Flags.UserEscaped; // the only flag that makes sense for a relative uri
                        e = null;
                        return;
                        // Otherwise an absolute file Uri wins when it's of the form "c:\something"
                    }
                }
            }
            else if (err > ParsingError.LastRelativeUriOkErrIndex)
            {
                //This is a fatal error based solely on scheme name parsing
                _string = null; // make it be invalid Uri
                e = GetException(err);
                return;
            }

            bool hasUnicode = false;

            _iriParsing = (s_IriParsing && ((_syntax == null) || _syntax.InFact(UriSyntaxFlags.AllowIriParsing)));

            if (_iriParsing &&
                (CheckForUnicode(_string) || CheckForEscapedUnreserved(_string)))
            {
                _flags |= Flags.HasUnicode;
                hasUnicode = true;
                // switch internal strings
                _originalUnicodeString = _string; // original string location changed
            }

            if (_syntax != null)
            {
                if (_syntax.IsSimple)
                {
                    if ((err = PrivateParseMinimal()) != ParsingError.None)
                    {
                        if (uriKind != UriKind.Absolute && err <= ParsingError.LastRelativeUriOkErrIndex)
                        {
                            // RFC 3986 Section 5.4.2 - http:(relativeUri) may be considered a valid relative Uri.
                            _syntax = null; // convert to relative uri
                            e = null;
                            _flags &= Flags.UserEscaped; // the only flag that makes sense for a relative uri
                            return;
                        }
                        else
                            e = GetException(err);
                    }
                    else if (uriKind == UriKind.Relative)
                    {
                        // Here we know that we can create an absolute Uri, but the user has requested only a relative one
                        e = GetException(ParsingError.CannotCreateRelative);
                    }
                    else
                        e = null;
                    // will return from here

                    if (_iriParsing && hasUnicode)
                    {
                        // In this scenario we need to parse the whole string 
                        EnsureParseRemaining();
                    }
                }
                else
                {
                    // offer custom parser to create a parsing context
                    _syntax = _syntax.InternalOnNewUri();

                    // in case they won't call us
                    _flags |= Flags.UserDrivenParsing;

                    // Ask a registered type to validate this uri
                    _syntax.InternalValidate(this, out e);

                    if (e != null)
                    {
                        // Can we still take it as a relative Uri?
                        if (uriKind != UriKind.Absolute && err != ParsingError.None
                            && err <= ParsingError.LastRelativeUriOkErrIndex)
                        {
                            _syntax = null; // convert it to relative
                            e = null;
                            _flags &= Flags.UserEscaped; // the only flag that makes sense for a relative uri
                        }
                    }
                    else // e == null
                    {
                        if (err != ParsingError.None || InFact(Flags.ErrorOrParsingRecursion))
                        {
                            // User parser took over on an invalid Uri
                            SetUserDrivenParsing();
                        }
                        else if (uriKind == UriKind.Relative)
                        {
                            // Here we know that custom parser can create an absolute Uri, but the user has requested only a 
                            // relative one
                            e = GetException(ParsingError.CannotCreateRelative);
                        }

                        if (_iriParsing && hasUnicode)
                        {
                            // In this scenario we need to parse the whole string 
                            EnsureParseRemaining();
                        }
                    }
                    // will return from here
                }
            }
            // If we encountered any parsing errors that indicate this may be a relative Uri, 
            // and we'll allow relative Uri's, then create one.
            else if (err != ParsingError.None && uriKind != UriKind.Absolute
                && err <= ParsingError.LastRelativeUriOkErrIndex)
            {
                e = null;
                _flags &= (Flags.UserEscaped | Flags.HasUnicode); // the only flags that makes sense for a relative uri
                if (_iriParsing && hasUnicode)
                {
                    // Iri'ze and then normalize relative uris
                    _string = EscapeUnescapeIri(_originalUnicodeString, 0, _originalUnicodeString.Length,
                                                (UriComponents)0);
                }
            }
            else
            {
                _string = null; // make it be invalid Uri
                e = GetException(err);
            }
        }

        //
        // Unescapes entire string and checks if it has unicode chars
        //
        private bool CheckForUnicode(string data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                char c = data[i];
                if (c == '%')
                {
                    if (i + 2 < data.Length)
                    {
                        if (UriHelper.EscapedAscii(data[i + 1], data[i + 2]) > 0x7F)
                        {
                            return true;
                        }
                        i += 2;
                    }
                }
                else if (c > 0x7F)
                {
                    return true;
                }
            }
            return false;
        }

        // Does this string have any %6A sequences that are 3986 Unreserved characters?  These should be un-escaped.
        private unsafe bool CheckForEscapedUnreserved(string data)
        {
            fixed (char* tempPtr = data)
            {
                for (int i = 0; i < data.Length - 2; ++i)
                {
                    if (tempPtr[i] == '%' && IsHexDigit(tempPtr[i + 1]) && IsHexDigit(tempPtr[i + 2])
                        && tempPtr[i + 1] >= '0' && tempPtr[i + 1] <= '7') // max 0x7F
                    {
                        char ch = UriHelper.EscapedAscii(tempPtr[i + 1], tempPtr[i + 2]);
                        if (ch != c_DummyChar && UriHelper.Is3986Unreserved(ch))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        //
        //  Returns true if the string represents a valid argument to the Uri ctor
        //  If uriKind != AbsoluteUri then certain parsing errors are ignored but Uri usage is limited
        //
        public static bool TryCreate(string uriString, UriKind uriKind, out Uri result)
        {
            if ((object)uriString == null)
            {
                result = null;
                return false;
            }
            UriFormatException e = null;
            result = CreateHelper(uriString, false, uriKind, ref e);
            return (object)e == null && result != null;
        }

        public static bool TryCreate(Uri baseUri, string relativeUri, out Uri result)
        {
            Uri relativeLink;
            if (TryCreate(relativeUri, UriKind.RelativeOrAbsolute, out relativeLink))
            {
                if (!relativeLink.IsAbsoluteUri)
                    return TryCreate(baseUri, relativeLink, out result);

                result = relativeLink;
                return true;
            }
            result = null;
            return false;
        }

        public static bool TryCreate(Uri baseUri, Uri relativeUri, out Uri result)
        {
            result = null;

            if ((object)baseUri == null || (object)relativeUri == null)
                return false;

            if (baseUri.IsNotAbsoluteUri)
                return false;

            UriFormatException e;
            string newUriString = null;

            bool dontEscape;
            if (baseUri.Syntax.IsSimple)
            {
                dontEscape = relativeUri.UserEscaped;
                result = ResolveHelper(baseUri, relativeUri, ref newUriString, ref dontEscape, out e);
            }
            else
            {
                dontEscape = false;
                newUriString = baseUri.Syntax.InternalResolve(baseUri, relativeUri, out e);
            }

            if (e != null)
                return false;

            if ((object)result == null)
                result = CreateHelper(newUriString, dontEscape, UriKind.Absolute, ref e);

            return (object)e == null && result != null && result.IsAbsoluteUri;
        }

        public string GetComponents(UriComponents components, UriFormat format)
        {
            if (((components & UriComponents.SerializationInfoString) != 0) && components != UriComponents.SerializationInfoString)
                throw new ArgumentOutOfRangeException(nameof(components), components, SR.net_uri_NotJustSerialization);

            if ((format & ~UriFormat.SafeUnescaped) != 0)
                throw new ArgumentOutOfRangeException(nameof(format));

            if (IsNotAbsoluteUri)
            {
                if (components == UriComponents.SerializationInfoString)
                    return GetRelativeSerializationString(format);
                else
                    throw new InvalidOperationException(SR.net_uri_NotAbsolute);
            }

            if (Syntax.IsSimple)
                return GetComponentsHelper(components, format);

            return Syntax.InternalGetComponents(this, components, format);
        }

        //
        // This is for languages that do not support == != operators overloading
        //
        // Note that Uri.Equals will get an optimized path but is limited to true/false result only
        //
        public static int Compare(Uri uri1, Uri uri2, UriComponents partsToCompare, UriFormat compareFormat,
            StringComparison comparisonType)
        {
            if ((object)uri1 == null)
            {
                if (uri2 == null)
                    return 0; // Equal
                return -1;    // null < non-null
            }
            if ((object)uri2 == null)
                return 1;     // non-null > null

            // a relative uri is always less than an absolute one
            if (!uri1.IsAbsoluteUri || !uri2.IsAbsoluteUri)
                return uri1.IsAbsoluteUri ? 1 : uri2.IsAbsoluteUri ? -1 : string.Compare(uri1.OriginalString,
                    uri2.OriginalString, comparisonType);

            return string.Compare(
                                    uri1.GetParts(partsToCompare, compareFormat),
                                    uri2.GetParts(partsToCompare, compareFormat),
                                    comparisonType
                                  );
        }

        public bool IsWellFormedOriginalString()
        {
            if (IsNotAbsoluteUri || Syntax.IsSimple)
                return InternalIsWellFormedOriginalString();

            return Syntax.InternalIsWellFormedOriginalString(this);
        }

        public static bool IsWellFormedUriString(string uriString, UriKind uriKind)
        {
            Uri result;

            if (!Uri.TryCreate(uriString, uriKind, out result))
                return false;

            return result.IsWellFormedOriginalString();
        }

        //
        // Internal stuff
        //

        // Returns false if OriginalString value
        // (1) is not correctly escaped as per URI spec excluding intl UNC name case
        // (2) or is an absolute Uri that represents implicit file Uri "c:\dir\file"
        // (3) or is an absolute Uri that misses a slash before path "file://c:/dir/file"
        // (4) or contains unescaped backslashes even if they will be treated
        //     as forward slashes like http:\\host/path\file or file:\\\c:\path
        //
        internal unsafe bool InternalIsWellFormedOriginalString()
        {
            if (UserDrivenParsing)
                throw new InvalidOperationException(SR.Format(SR.net_uri_UserDrivenParsing, this.GetType().ToString()));

            fixed (char* str = _string)
            {
                ushort idx = 0;
                //
                // For a relative Uri we only care about escaping and backslashes
                //
                if (!IsAbsoluteUri)
                {
                    // my:scheme/path?query is not well formed because the colon is ambiguous
                    if (CheckForColonInFirstPathSegment(_string))
                    {
                        return false;
                    }
                    return (CheckCanonical(str, ref idx, (ushort)_string.Length, c_EOL)
                            & (Check.BackslashInPath | Check.EscapedCanonical)) == Check.EscapedCanonical;
                }

                //
                // (2) or is an absolute Uri that represents implicit file Uri "c:\dir\file"
                //
                if (IsImplicitFile)
                    return false;

                //This will get all the offsets, a Host name will be checked separately below
                EnsureParseRemaining();

                Flags nonCanonical = (_flags & (Flags.E_CannotDisplayCanonical | Flags.IriCanonical));
                // User, Path, Query or Fragment may have some non escaped characters
                if (((nonCanonical & Flags.E_CannotDisplayCanonical & (Flags.E_UserNotCanonical | Flags.E_PathNotCanonical |
                                        Flags.E_QueryNotCanonical | Flags.E_FragmentNotCanonical)) != Flags.Zero) &&
                    (!_iriParsing || (_iriParsing &&
                    (((nonCanonical & Flags.E_UserNotCanonical) == 0) || ((nonCanonical & Flags.UserIriCanonical) == 0)) &&
                    (((nonCanonical & Flags.E_PathNotCanonical) == 0) || ((nonCanonical & Flags.PathIriCanonical) == 0)) &&
                    (((nonCanonical & Flags.E_QueryNotCanonical) == 0) || ((nonCanonical & Flags.QueryIriCanonical) == 0)) &&
                    (((nonCanonical & Flags.E_FragmentNotCanonical) == 0) || ((nonCanonical & Flags.FragmentIriCanonical) == 0)))))
                {
                    return false;
                }

                // checking on scheme:\\ or file:////
                if (InFact(Flags.AuthorityFound))
                {
                    idx = (ushort)(_info.Offset.Scheme + _syntax.SchemeName.Length + 2);
                    if (idx >= _info.Offset.User || _string[idx - 1] == '\\' || _string[idx] == '\\')
                        return false;

                    if (InFact(Flags.UncPath | Flags.DosPath))
                    {
                        while (++idx < _info.Offset.User && (_string[idx] == '/' || _string[idx] == '\\'))
                            return false;
                    }
                }


                // (3) or is an absolute Uri that misses a slash before path "file://c:/dir/file"
                // Note that for this check to be more general we assert that if Path is non empty and if it requires a first slash
                // (which looks absent) then the method has to fail.
                // Today it's only possible for a Dos like path, i.e. file://c:/bla would fail below check.
                if (InFact(Flags.FirstSlashAbsent) && _info.Offset.Query > _info.Offset.Path)
                    return false;

                // (4) or contains unescaped backslashes even if they will be treated
                //     as forward slashes like http:\\host/path\file or file:\\\c:\path
                // Note we do not check for Flags.ShouldBeCompressed i.e. allow // /./ and alike as valid
                if (InFact(Flags.BackslashInPath))
                    return false;

                // Capturing a rare case like file:///c|/dir
                if (IsDosPath && _string[_info.Offset.Path + SecuredPathIndex - 1] == '|')
                    return false;

                //
                // May need some real CPU processing to answer the request
                //
                //
                // Check escaping for authority
                //
                // IPv6 hosts cannot be properly validated by CheckCannonical
                if ((_flags & Flags.CanonicalDnsHost) == 0 && HostType != Flags.IPv6HostType)
                {
                    idx = _info.Offset.User;
                    Check result = CheckCanonical(str, ref idx, (ushort)_info.Offset.Path, '/');
                    if (((result & (Check.ReservedFound | Check.BackslashInPath | Check.EscapedCanonical))
                        != Check.EscapedCanonical)
                        && (!_iriParsing || (_iriParsing
                            && ((result & (Check.DisplayCanonical | Check.FoundNonAscii | Check.NotIriCanonical))
                                != (Check.DisplayCanonical | Check.FoundNonAscii)))))
                    {
                        return false;
                    }
                }

                // Want to ensure there are slashes after the scheme
                if ((_flags & (Flags.SchemeNotCanonical | Flags.AuthorityFound))
                    == (Flags.SchemeNotCanonical | Flags.AuthorityFound))
                {
                    idx = (ushort)_syntax.SchemeName.Length;
                    while (str[idx++] != ':') ;
                    if (idx + 1 >= _string.Length || str[idx] != '/' || str[idx + 1] != '/')
                        return false;
                }
            }
            //
            // May be scheme, host, port or path need some canonicalization but still the uri string is found to be a 
            // "well formed" one
            //
            return true;
        }

        public static string UnescapeDataString(string stringToUnescape)
        {
            if ((object)stringToUnescape == null)
                throw new ArgumentNullException(nameof(stringToUnescape));

            if (stringToUnescape.Length == 0)
                return string.Empty;

            unsafe
            {
                fixed (char* pStr = stringToUnescape)
                {
                    int position;
                    for (position = 0; position < stringToUnescape.Length; ++position)
                        if (pStr[position] == '%')
                            break;

                    if (position == stringToUnescape.Length)
                        return stringToUnescape;

                    UnescapeMode unescapeMode = UnescapeMode.Unescape | UnescapeMode.UnescapeAll;
                    position = 0;
                    char[] dest = new char[stringToUnescape.Length];
                    dest = UriHelper.UnescapeString(stringToUnescape, 0, stringToUnescape.Length, dest, ref position,
                        c_DummyChar, c_DummyChar, c_DummyChar, unescapeMode, null, false);
                    return new string(dest, 0, position);
                }
            }
        }

        //
        // Where stringToEscape is intended to be a completely unescaped URI string.
        // This method will escape any character that is not a reserved or unreserved character, including percent signs.
        // Note that EscapeUriString will also do not escape a '#' sign.
        //
        public static string EscapeUriString(string stringToEscape)
        {
            if ((object)stringToEscape == null)
                throw new ArgumentNullException(nameof(stringToEscape));

            if (stringToEscape.Length == 0)
                return string.Empty;

            int position = 0;
            char[] dest = UriHelper.EscapeString(stringToEscape, 0, stringToEscape.Length, null, ref position, true,
                c_DummyChar, c_DummyChar, c_DummyChar);
            if ((object)dest == null)
                return stringToEscape;
            return new string(dest, 0, position);
        }

        //
        // Where stringToEscape is intended to be URI data, but not an entire URI.
        // This method will escape any character that is not an unreserved character, including percent signs.
        //
        public static string EscapeDataString(string stringToEscape)
        {
            if ((object)stringToEscape == null)
                throw new ArgumentNullException(nameof(stringToEscape));

            if (stringToEscape.Length == 0)
                return string.Empty;

            int position = 0;
            char[] dest = UriHelper.EscapeString(stringToEscape, 0, stringToEscape.Length, null, ref position, false,
                c_DummyChar, c_DummyChar, c_DummyChar);
            if (dest == null)
                return stringToEscape;
            return new string(dest, 0, position);
        }

        //
        // Cleans up the specified component according to Iri rules
        // a) Chars allowed by iri in a component are unescaped if found escaped
        // b) Bidi chars are stripped
        //
        // should be called only if IRI parsing is switched on 
        internal unsafe string EscapeUnescapeIri(string input, int start, int end, UriComponents component)
        {
            fixed (char* pInput = input)
            {
                return IriHelper.EscapeUnescapeIri(pInput, start, end, component);
            }
        }

        // Should never be used except by the below method
        private Uri(Flags flags, UriParser uriParser, string uri)
        {
            _flags = flags;
            _syntax = uriParser;
            _string = uri;
        }

        //
        // a Uri.TryCreate() method goes through here.
        //
        internal static Uri CreateHelper(string uriString, bool dontEscape, UriKind uriKind, ref UriFormatException e)
        {
            // if (!Enum.IsDefined(typeof(UriKind), uriKind)) -- We currently believe that Enum.IsDefined() is too slow 
            // to be used here.
            if ((int)uriKind < (int)UriKind.RelativeOrAbsolute || (int)uriKind > (int)UriKind.Relative)
            {
                throw new ArgumentException(SR.Format(SR.net_uri_InvalidUriKind, uriKind));
            }

            UriParser syntax = null;
            Flags flags = Flags.Zero;
            ParsingError err = ParseScheme(uriString, ref flags, ref syntax);

            if (dontEscape)
                flags |= Flags.UserEscaped;

            // We won't use User factory for these errors
            if (err != ParsingError.None)
            {
                // If it looks as a relative Uri, custom factory is ignored
                if (uriKind != UriKind.Absolute && err <= ParsingError.LastRelativeUriOkErrIndex)
                    return new Uri((flags & Flags.UserEscaped), null, uriString);

                return null;
            }

            // Cannot be relative Uri if came here
            Uri result = new Uri(flags, syntax, uriString);

            // Validate instance using ether built in or a user Parser
            try
            {
                result.InitializeUri(err, uriKind, out e);

                if (e == null)
                    return result;

                return null;
            }
            catch (UriFormatException ee)
            {
                Debug.Assert(!syntax.IsSimple, "A UriPraser threw on InitializeAndValidate.");
                e = ee;
                // A precaution since custom Parser should never throw in this case.
                return null;
            }
        }

        //
        // Resolves into either baseUri or relativeUri according to conditions OR if not possible it uses newUriString 
        // to  return combined URI strings from both Uris 
        // otherwise if e != null on output the operation has failed
        //
        internal static Uri ResolveHelper(Uri baseUri, Uri relativeUri, ref string newUriString, ref bool userEscaped,
            out UriFormatException e)
        {
            Debug.Assert(!baseUri.IsNotAbsoluteUri && !baseUri.UserDrivenParsing, "Uri::ResolveHelper()|baseUri is not Absolute or is controlled by User Parser.");

            e = null;
            string relativeStr = string.Empty;

            if ((object)relativeUri != null)
            {
                if (relativeUri.IsAbsoluteUri)
                    return relativeUri;

                relativeStr = relativeUri.OriginalString;
                userEscaped = relativeUri.UserEscaped;
            }
            else
                relativeStr = string.Empty;

            // Here we can assert that passed "relativeUri" is indeed a relative one

            if (relativeStr.Length > 0 && (UriHelper.IsLWS(relativeStr[0]) || UriHelper.IsLWS(relativeStr[relativeStr.Length - 1])))
                relativeStr = relativeStr.Trim(UriHelper.s_WSchars);

            if (relativeStr.Length == 0)
            {
                newUriString = baseUri.GetParts(UriComponents.AbsoluteUri,
                    baseUri.UserEscaped ? UriFormat.UriEscaped : UriFormat.SafeUnescaped);
                return null;
            }

            // Check for a simple fragment in relative part
            if (relativeStr[0] == '#' && !baseUri.IsImplicitFile && baseUri.Syntax.InFact(UriSyntaxFlags.MayHaveFragment))
            {
                newUriString = baseUri.GetParts(UriComponents.AbsoluteUri & ~UriComponents.Fragment,
                    UriFormat.UriEscaped) + relativeStr;
                return null;
            }

            // Check for a simple query in relative part
            if (relativeStr[0] == '?' && !baseUri.IsImplicitFile && baseUri.Syntax.InFact(UriSyntaxFlags.MayHaveQuery))
            {
                newUriString = baseUri.GetParts(UriComponents.AbsoluteUri & ~UriComponents.Query & ~UriComponents.Fragment,
                    UriFormat.UriEscaped) + relativeStr;
                return null;
            }

            // Check on the DOS path in the relative Uri (a special case)
            if (relativeStr.Length >= 3
                && (relativeStr[1] == ':' || relativeStr[1] == '|')
                && UriHelper.IsAsciiLetter(relativeStr[0])
                && (relativeStr[2] == '\\' || relativeStr[2] == '/'))
            {
                if (baseUri.IsImplicitFile)
                {
                    // It could have file:/// prepended to the result but we want to keep it as *Implicit* File Uri
                    newUriString = relativeStr;
                    return null;
                }
                else if (baseUri.Syntax.InFact(UriSyntaxFlags.AllowDOSPath))
                {
                    // The scheme is not changed just the path gets replaced
                    string prefix;
                    if (baseUri.InFact(Flags.AuthorityFound))
                        prefix = baseUri.Syntax.InFact(UriSyntaxFlags.PathIsRooted) ? ":///" : "://";
                    else
                        prefix = baseUri.Syntax.InFact(UriSyntaxFlags.PathIsRooted) ? ":/" : ":";

                    newUriString = baseUri.Scheme + prefix + relativeStr;
                    return null;
                }
                // If we are here then input like "http://host/path/" + "C:\x" will produce the result  http://host/path/c:/x
            }


            ParsingError err = GetCombinedString(baseUri, relativeStr, userEscaped, ref newUriString);

            if (err != ParsingError.None)
            {
                e = GetException(err);
                return null;
            }

            if ((object)newUriString == (object)baseUri._string)
                return baseUri;

            return null;
        }

        private unsafe string GetRelativeSerializationString(UriFormat format)
        {
            if (format == UriFormat.UriEscaped)
            {
                if (_string.Length == 0)
                    return string.Empty;
                int position = 0;
                char[] dest = UriHelper.EscapeString(_string, 0, _string.Length, null, ref position, true,
                    c_DummyChar, c_DummyChar, '%');
                if ((object)dest == null)
                    return _string;
                return new string(dest, 0, position);
            }

            else if (format == UriFormat.Unescaped)
                return UnescapeDataString(_string);

            else if (format == UriFormat.SafeUnescaped)
            {
                if (_string.Length == 0)
                    return string.Empty;

                char[] dest = new char[_string.Length];
                int position = 0;
                dest = UriHelper.UnescapeString(_string, 0, _string.Length, dest, ref position, c_DummyChar,
                    c_DummyChar, c_DummyChar, UnescapeMode.EscapeUnescape, null, false);
                return new string(dest, 0, position);
            }
            else
                throw new ArgumentOutOfRangeException(nameof(format));
        }

        //
        // UriParser helpers methods
        //
        internal string GetComponentsHelper(UriComponents uriComponents, UriFormat uriFormat)
        {
            if (uriComponents == UriComponents.Scheme)
                return _syntax.SchemeName;

            // A serialization info is "almost" the same as AbsoluteUri except for IPv6 + ScopeID hostname case
            if ((uriComponents & UriComponents.SerializationInfoString) != 0)
                uriComponents |= UriComponents.AbsoluteUri;

            //This will get all the offsets, HostString will be created below if needed
            EnsureParseRemaining();

            if ((uriComponents & UriComponents.NormalizedHost) != 0)
            {
                // Down the path we rely on Host to be ON for NormalizedHost
                uriComponents |= UriComponents.Host;
            }

            //Check to see if we need the host/authority string
            if ((uriComponents & UriComponents.Host) != 0)
                EnsureHostString(true);

            //This, single Port request is always processed here
            if (uriComponents == UriComponents.Port || uriComponents == UriComponents.StrongPort)
            {
                if (((_flags & Flags.NotDefaultPort) != 0) || (uriComponents == UriComponents.StrongPort
                    && _syntax.DefaultPort != UriParser.NoDefaultPort))
                {
                    // recreate string from the port value
                    return _info.Offset.PortValue.ToString(CultureInfo.InvariantCulture);
                }
                return string.Empty;
            }

            if ((uriComponents & UriComponents.StrongPort) != 0)
            {
                // Down the path we rely on Port to be ON for StrongPort
                uriComponents |= UriComponents.Port;
            }

            //This request sometime is faster to process here
            if (uriComponents == UriComponents.Host && (uriFormat == UriFormat.UriEscaped
                || ((_flags & (Flags.HostNotCanonical | Flags.E_HostNotCanonical)) == 0)))
            {
                EnsureHostString(false);
                return _info.Host;
            }

            switch (uriFormat)
            {
                case UriFormat.UriEscaped:
                    return GetEscapedParts(uriComponents);

                case V1ToStringUnescape:
                case UriFormat.SafeUnescaped:
                case UriFormat.Unescaped:
                    return GetUnescapedParts(uriComponents, uriFormat);

                default:
                    throw new ArgumentOutOfRangeException(nameof(uriFormat));
            }
        }

        public bool IsBaseOf(Uri uri)
        {
            if ((object)uri == null)
                throw new ArgumentNullException(nameof(uri));

            if (!IsAbsoluteUri)
                return false;

            if (Syntax.IsSimple)
                return IsBaseOfHelper(uri);

            return Syntax.InternalIsBaseOf(this, uri);
        }


        internal bool IsBaseOfHelper(Uri uriLink)
        {
            if (!IsAbsoluteUri || UserDrivenParsing)
                return false;

            if (!uriLink.IsAbsoluteUri)
            {
                //a relative uri could have quite tricky form, it's better to fix it now.
                string newUriString = null;
                UriFormatException e;
                bool dontEscape = false;

                uriLink = ResolveHelper(this, uriLink, ref newUriString, ref dontEscape, out e);
                if (e != null)
                    return false;

                if ((object)uriLink == null)
                    uriLink = CreateHelper(newUriString, dontEscape, UriKind.Absolute, ref e);

                if (e != null)
                    return false;
            }

            if (Syntax.SchemeName != uriLink.Syntax.SchemeName)
                return false;

            // Canonicalize and test for substring match up to the last path slash
            string self = GetParts(UriComponents.AbsoluteUri & ~UriComponents.Fragment, UriFormat.SafeUnescaped);
            string other = uriLink.GetParts(UriComponents.AbsoluteUri & ~UriComponents.Fragment, UriFormat.SafeUnescaped);

            unsafe
            {
                fixed (char* selfPtr = self)
                {
                    fixed (char* otherPtr = other)
                    {
                        return UriHelper.TestForSubPath(selfPtr, (ushort)self.Length, otherPtr, (ushort)other.Length,
                            IsUncOrDosPath || uriLink.IsUncOrDosPath);
                    }
                }
            }
        }

        //
        // Only a ctor time call
        //
        private void CreateThisFromUri(Uri otherUri)
        {
            // Clone the other guy but develop own UriInfo member
            _info = null;

            _flags = otherUri._flags;
            if (InFact(Flags.MinimalUriInfoSet))
            {
                _flags &= ~(Flags.MinimalUriInfoSet | Flags.AllUriInfoSet | Flags.IndexMask);
                // Port / Path offset
                int portIndex = otherUri._info.Offset.Path;
                if (InFact(Flags.NotDefaultPort))
                {
                    // Find the start of the port.  Account for non-canonical ports like :00123
                    while (otherUri._string[portIndex] != ':' && portIndex > otherUri._info.Offset.Host)
                    {
                        portIndex--;
                    }
                    if (otherUri._string[portIndex] != ':')
                    {
                        // Something wrong with the NotDefaultPort flag.  Reset to path index
                        Debug.Assert(false, "Uri failed to locate custom port at index: " + portIndex);
                        portIndex = otherUri._info.Offset.Path;
                    }
                }
                _flags |= (Flags)portIndex; // Port or path
            }

            _syntax = otherUri._syntax;
            _string = otherUri._string;
            _iriParsing = otherUri._iriParsing;
            if (otherUri.OriginalStringSwitched)
            {
                _originalUnicodeString = otherUri._originalUnicodeString;
            }
            if (otherUri.AllowIdn && (otherUri.InFact(Flags.IdnHost) || otherUri.InFact(Flags.UnicodeHost)))
            {
                _dnsSafeHost = otherUri._dnsSafeHost;
            }
        }
    }
}

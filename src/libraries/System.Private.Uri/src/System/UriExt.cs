// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public partial class Uri
    {
        //
        // All public ctors go through here
        //
        [MemberNotNull(nameof(_string))]
        private void CreateThis(string? uri, bool dontEscape, UriKind uriKind, in UriCreationOptions creationOptions = default)
        {
            DebugAssertInCtor();

            if ((int)uriKind < (int)UriKind.RelativeOrAbsolute || (int)uriKind > (int)UriKind.Relative)
            {
                throw new ArgumentException(SR.Format(SR.net_uri_InvalidUriKind, uriKind));
            }

            _string = uri ?? string.Empty;

            Debug.Assert(_originalUnicodeString is null && _info is null && _syntax is null && _flags == Flags.Zero);

            if (dontEscape)
                _flags |= Flags.UserEscaped;

            if (creationOptions.DangerousDisablePathAndQueryCanonicalization)
                _flags |= Flags.DisablePathAndQueryCanonicalization;

            ParsingError err = ParseScheme(_string, ref _flags, ref _syntax!);

            InitializeUri(err, uriKind, out UriFormatException? e);
            if (e != null)
                throw e;
        }

        private void InitializeUri(ParsingError err, UriKind uriKind, out UriFormatException? e)
        {
            DebugAssertInCtor();

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
                    || (!OperatingSystem.IsWindows() && InFact(Flags.UnixPath))))
                    {
                        _syntax = null!; //make it be relative Uri
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
                        _syntax = null!; //make it be relative Uri
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
                _string = null!; // make it be invalid Uri
                e = GetException(err);
                return;
            }

            bool hasUnicode = false;

            if (IriParsing && CheckForUnicodeOrEscapedUnreserved(_string))
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
                            _syntax = null!; // convert to relative uri
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

                    if (hasUnicode)
                    {
                        // In this scenario we need to parse the whole string
                        try
                        {
                            EnsureParseRemaining();
                        }
                        catch (UriFormatException ex)
                        {
                            e = ex;
                            return;
                        }
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
                            _syntax = null!; // convert it to relative
                            e = null;
                            _flags &= Flags.UserEscaped; // the only flag that makes sense for a relative uri
                        }
                    }
                    else // e == null
                    {
                        if (err != ParsingError.None || InFact(Flags.ErrorOrParsingRecursion))
                        {
                            // User parser took over on an invalid Uri
                            // we use = here to clear all parsing flags for a uri that we think is invalid.
                            _flags = Flags.UserDrivenParsing | (_flags & Flags.UserEscaped);
                        }
                        else if (uriKind == UriKind.Relative)
                        {
                            // Here we know that custom parser can create an absolute Uri, but the user has requested only a
                            // relative one
                            e = GetException(ParsingError.CannotCreateRelative);
                        }

                        if (hasUnicode)
                        {
                            // In this scenario we need to parse the whole string
                            try
                            {
                                EnsureParseRemaining();
                            }
                            catch (UriFormatException ex)
                            {
                                e = ex;
                                return;
                            }
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
                if (hasUnicode)
                {
                    // Iri'ze and then normalize relative uris
                    _string = EscapeUnescapeIri(_originalUnicodeString, 0, _originalUnicodeString.Length,
                                                (UriComponents)0);
                    if (_string.Length > ushort.MaxValue)
                    {
                        return;
                    }
                }
            }
            else
            {
                _string = null!; // make it be invalid Uri
                e = GetException(err);
            }
        }

        // Unescapes entire string and checks if it has unicode chars
        // Also checks for sequences that are 3986 Unreserved characters as these should be un-escaped
        private static bool CheckForUnicodeOrEscapedUnreserved(string data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                char c = data[i];
                if (c == '%')
                {
                    if ((uint)(i + 2) < (uint)data.Length)
                    {
                        char value = UriHelper.DecodeHexChars(data[i + 1], data[i + 2]);

                        if (!char.IsAscii(value) || UriHelper.Unreserved.Contains(value))
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

        //
        //  Returns true if the string represents a valid argument to the Uri ctor
        //  If uriKind != AbsoluteUri then certain parsing errors are ignored but Uri usage is limited
        //
        public static bool TryCreate([NotNullWhen(true), StringSyntax(StringSyntaxAttribute.Uri, "uriKind")] string? uriString, UriKind uriKind, [NotNullWhen(true)] out Uri? result)
        {
            if (uriString is null)
            {
                result = null;
                return false;
            }
            UriFormatException? e = null;
            result = CreateHelper(uriString, false, uriKind, ref e);
            result?.DebugSetLeftCtor();
            return e is null && result != null;
        }

        /// <summary>
        /// Creates a new <see cref="Uri"/> using the specified <see cref="string"/> instance and <see cref="UriCreationOptions"/>.
        /// </summary>
        /// <param name="uriString">The string representation of the <see cref="Uri"/>.</param>
        /// <param name="creationOptions">Options that control how the <seealso cref="Uri"/> is created and behaves.</param>
        /// <param name="result">The constructed <see cref="Uri"/>.</param>
        /// <returns><see langword="true"/> if the <see cref="Uri"/> was successfully created; otherwise, <see langword="false"/>.</returns>
        public static bool TryCreate([NotNullWhen(true), StringSyntax(StringSyntaxAttribute.Uri)] string? uriString, in UriCreationOptions creationOptions, [NotNullWhen(true)] out Uri? result)
        {
            if (uriString is null)
            {
                result = null;
                return false;
            }
            UriFormatException? e = null;
            result = CreateHelper(uriString, false, UriKind.Absolute, ref e, in creationOptions);
            result?.DebugSetLeftCtor();
            return e is null && result != null;
        }

        public static bool TryCreate(Uri? baseUri, string? relativeUri, [NotNullWhen(true)] out Uri? result)
        {
            if (TryCreate(relativeUri, UriKind.RelativeOrAbsolute, out Uri? relativeLink))
            {
                if (!relativeLink.IsAbsoluteUri)
                    return TryCreate(baseUri, relativeLink, out result);

                result = relativeLink;
                return true;
            }
            result = null;
            return false;
        }

        public static bool TryCreate(Uri? baseUri, Uri? relativeUri, [NotNullWhen(true)] out Uri? result)
        {
            result = null;

            if (baseUri is null || relativeUri is null)
                return false;

            if (baseUri.IsNotAbsoluteUri)
                return false;

            UriFormatException? e = null;
            string? newUriString = null;

            bool dontEscape;
            if (baseUri.Syntax.IsSimple)
            {
                dontEscape = relativeUri.UserEscaped;
                result = ResolveHelper(baseUri, relativeUri, ref newUriString, ref dontEscape);
            }
            else
            {
                dontEscape = false;
                newUriString = baseUri.Syntax.InternalResolve(baseUri, relativeUri, out e);

                if (e != null)
                    return false;
            }

            result ??= CreateHelper(newUriString!, dontEscape, UriKind.Absolute, ref e);

            result?.DebugSetLeftCtor();
            return e is null && result != null && result.IsAbsoluteUri;
        }

        public string GetComponents(UriComponents components, UriFormat format)
        {
            if (DisablePathAndQueryCanonicalization && (components & (UriComponents.Path | UriComponents.Query)) != 0)
            {
                throw new InvalidOperationException(SR.net_uri_GetComponentsCalledWhenCanonicalizationDisabled);
            }

            return InternalGetComponents(components, format);
        }

        private string InternalGetComponents(UriComponents components, UriFormat format)
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
        public static int Compare(Uri? uri1, Uri? uri2, UriComponents partsToCompare, UriFormat compareFormat,
            StringComparison comparisonType)
        {
            if (uri1 is null)
            {
                if (uri2 is null)
                    return 0; // Equal
                return -1;    // null < non-null
            }

            if (uri2 is null)
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

        public static bool IsWellFormedUriString([NotNullWhen(true), StringSyntax(StringSyntaxAttribute.Uri, "uriKind")] string? uriString, UriKind uriKind)
        {
            Uri? result;

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
                throw new InvalidOperationException(SR.Format(SR.net_uri_UserDrivenParsing, this.GetType()));

            fixed (char* str = _string)
            {
                int idx = 0;
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
                    return (CheckCanonical(str, ref idx, _string.Length, c_EOL)
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

                // Cleanup canonical IRI from nonCanonical
                if ((nonCanonical & (Flags.UserIriCanonical | Flags.PathIriCanonical | Flags.QueryIriCanonical | Flags.FragmentIriCanonical)) != 0)
                {
                    if ((nonCanonical & (Flags.E_UserNotCanonical | Flags.UserIriCanonical)) == (Flags.E_UserNotCanonical | Flags.UserIriCanonical))
                    {
                        nonCanonical &= ~(Flags.E_UserNotCanonical | Flags.UserIriCanonical);
                    }

                    if ((nonCanonical & (Flags.E_PathNotCanonical | Flags.PathIriCanonical)) == (Flags.E_PathNotCanonical | Flags.PathIriCanonical))
                    {
                        nonCanonical &= ~(Flags.E_PathNotCanonical | Flags.PathIriCanonical);
                    }

                    if ((nonCanonical & (Flags.E_QueryNotCanonical | Flags.QueryIriCanonical)) == (Flags.E_QueryNotCanonical | Flags.QueryIriCanonical))
                    {
                        nonCanonical &= ~(Flags.E_QueryNotCanonical | Flags.QueryIriCanonical);
                    }

                    if ((nonCanonical & (Flags.E_FragmentNotCanonical | Flags.FragmentIriCanonical)) == (Flags.E_FragmentNotCanonical | Flags.FragmentIriCanonical))
                    {
                        nonCanonical &= ~(Flags.E_FragmentNotCanonical | Flags.FragmentIriCanonical);
                    }
                }

                // User, Path, Query or Fragment may have some non escaped characters
                if (((nonCanonical & Flags.E_CannotDisplayCanonical & (Flags.E_UserNotCanonical | Flags.E_PathNotCanonical |
                                        Flags.E_QueryNotCanonical | Flags.E_FragmentNotCanonical)) != Flags.Zero))
                {
                    return false;
                }

                // checking on scheme:\\ or file:////
                if (InFact(Flags.AuthorityFound))
                {
                    idx = _info.Offset.Scheme + _syntax.SchemeName.Length + 2;
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
                // IPv6 hosts cannot be properly validated by CheckCanonical
                if ((_flags & Flags.CanonicalDnsHost) == 0 && HostType != Flags.IPv6HostType)
                {
                    idx = _info.Offset.User;
                    Check result = CheckCanonical(str, ref idx, _info.Offset.Path, '/');
                    if (((result & (Check.ReservedFound | Check.BackslashInPath | Check.EscapedCanonical))
                        != Check.EscapedCanonical)
                        && (!IriParsing || (result & (Check.DisplayCanonical | Check.FoundNonAscii | Check.NotIriCanonical))
                                != (Check.DisplayCanonical | Check.FoundNonAscii)))
                    {
                        return false;
                    }
                }

                // Want to ensure there are slashes after the scheme
                if ((_flags & (Flags.SchemeNotCanonical | Flags.AuthorityFound))
                    == (Flags.SchemeNotCanonical | Flags.AuthorityFound))
                {
                    idx = _syntax.SchemeName.Length;
                    while (str[idx++] != ':');
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

        /// <summary>Converts a string to its unescaped representation.</summary>
        /// <param name="stringToUnescape">The string to unescape.</param>
        /// <returns>The unescaped representation of <paramref name="stringToUnescape"/>.</returns>
        public static string UnescapeDataString(string stringToUnescape)
        {
            ArgumentNullException.ThrowIfNull(stringToUnescape);

            return UnescapeDataString(stringToUnescape, stringToUnescape);
        }

        /// <summary>Converts a span to its unescaped representation.</summary>
        /// <param name="charsToUnescape">The span to unescape.</param>
        /// <returns>The unescaped representation of <paramref name="charsToUnescape"/>.</returns>
        public static string UnescapeDataString(ReadOnlySpan<char> charsToUnescape)
        {
            return UnescapeDataString(charsToUnescape, backingString: null);
        }

        private static string UnescapeDataString(ReadOnlySpan<char> charsToUnescape, string? backingString = null)
        {
            Debug.Assert(backingString is null || backingString.Length == charsToUnescape.Length);

            int indexOfFirstToUnescape = charsToUnescape.IndexOf('%');
            if (indexOfFirstToUnescape < 0)
            {
                // Nothing to unescape, just return the original value.
                return backingString ?? charsToUnescape.ToString();
            }

            var vsb = new ValueStringBuilder(stackalloc char[StackallocThreshold]);

            // We may throw for very large inputs (when growing the ValueStringBuilder).
            vsb.EnsureCapacity(charsToUnescape.Length - indexOfFirstToUnescape);

            UriHelper.UnescapeString(
                charsToUnescape.Slice(indexOfFirstToUnescape), ref vsb,
                c_DummyChar, c_DummyChar, c_DummyChar,
                UnescapeMode.Unescape | UnescapeMode.UnescapeAll,
                syntax: null, isQuery: false);

            string result = string.Concat(charsToUnescape.Slice(0, indexOfFirstToUnescape), vsb.AsSpan());
            vsb.Dispose();
            return result;
        }

        /// <summary>Attempts to convert a span to its unescaped representation.</summary>
        /// <param name="charsToUnescape">The span to unescape.</param>
        /// <param name="destination">The output span that contains the unescaped result of the operation.</param>
        /// <param name="charsWritten">When this method returns, contains the number of chars that were written into <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if the <paramref name="destination"/> was large enough to hold the entire result; otherwise, <see langword="false"/>.</returns>
        public static bool TryUnescapeDataString(ReadOnlySpan<char> charsToUnescape, Span<char> destination, out int charsWritten)
        {
            int indexOfFirstToUnescape = charsToUnescape.IndexOf('%');
            if (indexOfFirstToUnescape < 0)
            {
                // Nothing to unescape, just copy the original chars.
                if (charsToUnescape.TryCopyTo(destination))
                {
                    charsWritten = charsToUnescape.Length;
                    return true;
                }

                charsWritten = 0;
                return false;
            }

            // We may throw for very large inputs (when growing the ValueStringBuilder).
            scoped ValueStringBuilder vsb;

            // If the input and destination buffers overlap, we must take care not to overwrite parts of the input before we've processed it.
            // If the buffers start at the same location, we can still use the destination as the output length is strictly <= input length.
            bool overlapped = charsToUnescape.Overlaps(destination) &&
                !Unsafe.AreSame(ref MemoryMarshal.GetReference(charsToUnescape), ref MemoryMarshal.GetReference(destination));

            if (overlapped)
            {
                vsb = new ValueStringBuilder(stackalloc char[StackallocThreshold]);
                vsb.EnsureCapacity(charsToUnescape.Length - indexOfFirstToUnescape);
            }
            else
            {
                vsb = new ValueStringBuilder(destination.Slice(indexOfFirstToUnescape));
            }

            // We may throw for very large inputs (when growing the ValueStringBuilder).
            UriHelper.UnescapeString(
                charsToUnescape.Slice(indexOfFirstToUnescape), ref vsb,
                c_DummyChar, c_DummyChar, c_DummyChar,
                UnescapeMode.Unescape | UnescapeMode.UnescapeAll,
                syntax: null, isQuery: false);

            int newLength = indexOfFirstToUnescape + vsb.Length;
            Debug.Assert(newLength <= charsToUnescape.Length);

            if (destination.Length >= newLength)
            {
                charsToUnescape.Slice(0, indexOfFirstToUnescape).CopyTo(destination);

                if (overlapped)
                {
                    vsb.AsSpan().CopyTo(destination.Slice(indexOfFirstToUnescape));
                    vsb.Dispose();
                }
                else
                {
                    // We are expecting the builder not to grow if the original span was large enough.
                    // This means that we MUST NOT over allocate anywhere in UnescapeString (e.g. append and then decrease the length).
                    Debug.Assert(vsb.RawChars.Overlaps(destination));
                }

                charsWritten = newLength;
                return true;
            }

            vsb.Dispose();
            charsWritten = 0;
            return false;
        }

        // Where stringToEscape is intended to be a completely unescaped URI string.
        // This method will escape any character that is not a reserved or unreserved character, including percent signs.
        [Obsolete(Obsoletions.EscapeUriStringMessage, DiagnosticId = Obsoletions.EscapeUriStringDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static string EscapeUriString(string stringToEscape) =>
            UriHelper.EscapeString(stringToEscape, checkExistingEscaped: false, UriHelper.UnreservedReserved);

        // Where stringToEscape is intended to be URI data, but not an entire URI.
        // This method will escape any character that is not an unreserved character, including percent signs.

        /// <summary>Converts a string to its escaped representation.</summary>
        /// <param name="stringToEscape">The string to escape.</param>
        /// <returns>The escaped representation of <paramref name="stringToEscape"/>.</returns>
        public static string EscapeDataString(string stringToEscape) =>
            UriHelper.EscapeString(stringToEscape, checkExistingEscaped: false, UriHelper.Unreserved);

        /// <summary>Converts a span to its escaped representation.</summary>
        /// <param name="charsToEscape">The span to escape.</param>
        /// <returns>The escaped representation of <paramref name="charsToEscape"/>.</returns>
        public static string EscapeDataString(ReadOnlySpan<char> charsToEscape) =>
            UriHelper.EscapeString(charsToEscape, checkExistingEscaped: false, UriHelper.Unreserved, backingString: null);

        /// <summary>Attempts to convert a span to its escaped representation.</summary>
        /// <param name="charsToEscape">The span to escape.</param>
        /// <param name="destination">The output span that contains the escaped result of the operation.</param>
        /// <param name="charsWritten">When this method returns, contains the number of chars that were written into <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if the <paramref name="destination"/> was large enough to hold the entire result; otherwise, <see langword="false"/>.</returns>
        public static bool TryEscapeDataString(ReadOnlySpan<char> charsToEscape, Span<char> destination, out int charsWritten) =>
            UriHelper.TryEscapeDataString(charsToEscape, destination, out charsWritten);

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
        private Uri(Flags flags, UriParser? uriParser, string uri)
        {
            _flags = flags;
            _syntax = uriParser!;
            _string = uri;

            if (uriParser is null)
            {
                // Relative Uris are fully initialized after the call to this constructor
                // Absolute Uris will be initialized with a call to InitializeUri on the newly created instance
                DebugSetLeftCtor();
            }
        }

        //
        // a Uri.TryCreate() method goes through here.
        //
        internal static Uri? CreateHelper(string uriString, bool dontEscape, UriKind uriKind, ref UriFormatException? e, in UriCreationOptions creationOptions = default)
        {
            if ((int)uriKind < (int)UriKind.RelativeOrAbsolute || (int)uriKind > (int)UriKind.Relative)
            {
                throw new ArgumentException(SR.Format(SR.net_uri_InvalidUriKind, uriKind));
            }

            UriParser? syntax = null;
            Flags flags = Flags.Zero;
            ParsingError err = ParseScheme(uriString, ref flags, ref syntax);

            if (dontEscape)
                flags |= Flags.UserEscaped;

            if (creationOptions.DangerousDisablePathAndQueryCanonicalization)
                flags |= Flags.DisablePathAndQueryCanonicalization;

            // We won't use User factory for these errors
            if (err != ParsingError.None)
            {
                // If it looks as a relative Uri, custom factory is ignored
                if (uriKind != UriKind.Absolute && err <= ParsingError.LastRelativeUriOkErrIndex)
                    return new Uri((flags & Flags.UserEscaped), null, uriString);

                return null;
            }

            // Cannot be relative Uri if came here
            Debug.Assert(syntax != null);
            Uri result = new Uri(flags, syntax, uriString);

            // Validate instance using ether built in or a user Parser
            try
            {
                result.InitializeUri(err, uriKind, out e);

                if (e == null)
                {
                    result.DebugSetLeftCtor();
                    return result;
                }

                return null;
            }
            catch (UriFormatException ee)
            {
                Debug.Assert(!syntax!.IsSimple, "A UriPraser threw on InitializeAndValidate.");
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
        internal static Uri? ResolveHelper(Uri baseUri, Uri? relativeUri, ref string? newUriString, ref bool userEscaped)
        {
            Debug.Assert(!baseUri.IsNotAbsoluteUri && !baseUri.UserDrivenParsing, "Uri::ResolveHelper()|baseUri is not Absolute or is controlled by User Parser.");

            string relativeStr;

            if (relativeUri is not null)
            {
                if (relativeUri.IsAbsoluteUri)
                    return relativeUri;

                relativeStr = relativeUri.OriginalString;
                userEscaped = relativeUri.UserEscaped;
            }
            else
            {
                relativeStr = string.Empty;
            }

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
            if (relativeStr[0] == '#' && !baseUri.IsImplicitFile && baseUri.Syntax!.InFact(UriSyntaxFlags.MayHaveFragment))
            {
                newUriString = baseUri.GetParts(UriComponents.AbsoluteUri & ~UriComponents.Fragment,
                    UriFormat.UriEscaped) + relativeStr;
                return null;
            }

            // Check for a simple query in relative part
            if (relativeStr[0] == '?' && !baseUri.IsImplicitFile && baseUri.Syntax!.InFact(UriSyntaxFlags.MayHaveQuery))
            {
                newUriString = baseUri.GetParts(UriComponents.AbsoluteUri & ~UriComponents.Query & ~UriComponents.Fragment,
                    UriFormat.UriEscaped) + relativeStr;
                return null;
            }

            // Check on the DOS path in the relative Uri (a special case)
            if (relativeStr.Length >= 3
                && (relativeStr[1] == ':' || relativeStr[1] == '|')
                && char.IsAsciiLetter(relativeStr[0])
                && (relativeStr[2] == '\\' || relativeStr[2] == '/'))
            {
                if (baseUri.IsImplicitFile)
                {
                    // It could have file:/// prepended to the result but we want to keep it as *Implicit* File Uri
                    newUriString = relativeStr;
                    return null;
                }
                else if (baseUri.Syntax!.InFact(UriSyntaxFlags.AllowDOSPath))
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

            GetCombinedString(baseUri, relativeStr, userEscaped, ref newUriString);

            if (ReferenceEquals(newUriString, baseUri._string))
                return baseUri;

            return null;
        }

        private unsafe string GetRelativeSerializationString(UriFormat format)
        {
            if (format == UriFormat.UriEscaped)
            {
                return UriHelper.EscapeString(_string, checkExistingEscaped: true, UriHelper.UnreservedReserved);
            }
            else if (format == UriFormat.Unescaped)
            {
                return UnescapeDataString(_string);
            }
            else if (format == UriFormat.SafeUnescaped)
            {
                if (_string.Length == 0)
                    return string.Empty;

                var vsb = new ValueStringBuilder(stackalloc char[StackallocThreshold]);
                UriHelper.UnescapeString(_string, ref vsb, c_DummyChar, c_DummyChar, c_DummyChar, UnescapeMode.EscapeUnescape, null, false);
                return vsb.ToString();
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(format));
            }
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
                return _info.Host!;
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
            ArgumentNullException.ThrowIfNull(uri);

            if (!IsAbsoluteUri)
                return false;

            if (Syntax.IsSimple)
                return IsBaseOfHelper(uri);

            return Syntax.InternalIsBaseOf(this, uri);
        }


        internal bool IsBaseOfHelper(Uri uriLink)
        {
            const UriComponents ComponentsToCompare =
                UriComponents.AbsoluteUri
                & ~UriComponents.Fragment
                & ~UriComponents.UserInfo;

            if (!IsAbsoluteUri || UserDrivenParsing)
                return false;

            if (!uriLink.IsAbsoluteUri)
            {
                //a relative uri could have quite tricky form, it's better to fix it now.
                string? newUriString = null;
                bool dontEscape = false;

                uriLink = ResolveHelper(this, uriLink, ref newUriString, ref dontEscape)!;

                if (uriLink is null)
                {
                    UriFormatException? e = null;

                    uriLink = CreateHelper(newUriString!, dontEscape, UriKind.Absolute, ref e)!;

                    if (e != null)
                        return false;
                }
            }

            if (Syntax.SchemeName != uriLink.Syntax.SchemeName)
                return false;

            // Canonicalize and test for substring match up to the last path slash
            string self = GetParts(ComponentsToCompare, UriFormat.SafeUnescaped);
            string other = uriLink.GetParts(ComponentsToCompare, UriFormat.SafeUnescaped);

            unsafe
            {
                fixed (char* selfPtr = self)
                {
                    fixed (char* otherPtr = other)
                    {
                        return UriHelper.TestForSubPath(selfPtr, self.Length, otherPtr, other.Length,
                            IsUncOrDosPath || uriLink.IsUncOrDosPath);
                    }
                }
            }
        }

        //
        // Only a ctor time call
        //
        [MemberNotNull(nameof(_string))]
        private void CreateThisFromUri(Uri otherUri)
        {
            DebugAssertInCtor();

            // Clone the other URI but develop own UriInfo member
            _info = null!;

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
                        Debug.Fail("Uri failed to locate custom port at index: " + portIndex);
                        portIndex = otherUri._info.Offset.Path;
                    }
                }
                _flags |= (Flags)portIndex; // Port or path
            }

            _syntax = otherUri._syntax;
            _string = otherUri._string;
            _originalUnicodeString = otherUri._originalUnicodeString;
        }
    }
}

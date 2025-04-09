// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace System.Net
{
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public enum CookieVariant
    {
        Unknown,
        Plain,
        Rfc2109,
        Rfc2965,
        Default = Rfc2109
    }

    // Cookie class
    //
    // Adheres to RFC 2965
    //
    // Currently, only represents client-side cookies. The cookie classes know
    // how to parse a set-cookie format string, but not a cookie format string
    // (e.g. "Cookie: $Version=1; name=value; $Path=/foo; $Secure")
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class Cookie
    {
        // NOTE: these two constants must change together.
        internal const int MaxSupportedVersion = 1;
        internal const string MaxSupportedVersionString = "1";

        internal const string SeparatorLiteral = "; ";
        internal const char EqualsLiteral = '=';
        internal const string QuotesLiteral = "\"";
        internal const string SpecialAttributeLiteral = "$";

        internal static readonly char[] PortSplitDelimiters = new char[] { ' ', ',', '\"' };
        // Space (' ') should be reserved as well per RFCs, but major web browsers support it and some web sites use it - so we support it too
        private static readonly SearchValues<char> s_reservedToNameChars = SearchValues.Create("\t\r\n=;,");

        private static readonly SearchValues<char> s_domainChars =
            SearchValues.Create("-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz");

        private string m_comment = string.Empty; // Do not rename (binary serialization)
        private Uri? m_commentUri; // Do not rename (binary serialization)
        private CookieVariant m_cookieVariant = CookieVariant.Plain; // Do not rename (binary serialization)
        private bool m_discard; // Do not rename (binary serialization)
        private string m_domain = string.Empty; // Do not rename (binary serialization)
        private bool m_domain_implicit = true; // Do not rename (binary serialization)
        private DateTime m_expires = DateTime.MinValue; // Do not rename (binary serialization)
        private string m_name = string.Empty; // Do not rename (binary serialization)
        private string m_path = string.Empty; // Do not rename (binary serialization)
        private bool m_path_implicit = true; // Do not rename (binary serialization)
        private string m_port = string.Empty; // Do not rename (binary serialization)
        private bool m_port_implicit = true; // Do not rename (binary serialization)
        private int[]? m_port_list; // Do not rename (binary serialization)
        private bool m_secure; // Do not rename (binary serialization)
        [System.Runtime.Serialization.OptionalField]
        private bool m_httpOnly = false; // Do not rename (binary serialization)
        private DateTime m_timeStamp = DateTime.UtcNow; // Do not rename (binary serialization)
        private string m_value = string.Empty; // Do not rename (binary serialization)
        private int m_version; // Do not rename (binary serialization)

        private string? m_domainKey; // Do not rename (binary serialization)

#pragma warning disable 0649 // set via reflection by CookieParser: https://github.com/dotnet/runtime/issues/19348
        internal bool IsQuotedVersion; // Do not rename (binary serialization)
        internal bool IsQuotedDomain; // Do not rename (binary serialization)
#pragma warning restore 0649

#if DEBUG
        static Cookie()
        {
            Debug.Assert(MaxSupportedVersion.ToString(NumberFormatInfo.InvariantInfo).Equals(MaxSupportedVersionString, StringComparison.Ordinal));
        }
#endif

        // These DynamicDependency attributes are a workaround for https://github.com/dotnet/runtime/issues/19348.
        // HttpListener uses the non-public ToServerString, which isn't used by anything else in this assembly,
        // and which accesses other internals and can't be moved to HttpListener (at least not without incurring
        // functional differences).  However, once we do our initial System.Net.Primitives build and ToServerString
        // survives to it, we no longer want the DynamicDependencyAttribute to remain around, so that ToServerString
        // can be trimmed out if the relevant functionality from HttpListener isn't used when performing whole-app
        // analysis.  As such, when trimming System.Net.Primitives, we build the assembly with ILLinkKeepDepAttributes=false,
        // such that when this assembly is compiled, ToServerString will remain but the DynamicDependency attributes
        // will be removed.  This hack will need to be revisited if anything else in the assembly starts using
        // DynamicDependencyAttribute.
        // https://github.com/mono/linker/issues/802

        [DynamicDependency("ToServerString")]
        public Cookie()
        {
        }

        [DynamicDependency("ToServerString")] // Workaround for https://github.com/dotnet/runtime/issues/19348
        public Cookie(string name, string? value)
        {
            Name = name;
            Value = value;
        }

        public Cookie(string name, string? value, string? path)
            : this(name, value)
        {
            Path = path;
        }

        public Cookie(string name, string? value, string? path, string? domain)
            : this(name, value, path)
        {
            Domain = domain;
        }

        [AllowNull]
        public string Comment
        {
            get
            {
                return m_comment;
            }
            set
            {
                m_comment = value ?? string.Empty;
            }
        }

        public Uri? CommentUri
        {
            get
            {
                return m_commentUri;
            }
            set
            {
                m_commentUri = value;
            }
        }


        public bool HttpOnly
        {
            get
            {
                return m_httpOnly;
            }
            set
            {
                m_httpOnly = value;
            }
        }


        public bool Discard
        {
            get
            {
                return m_discard;
            }
            set
            {
                m_discard = value;
            }
        }

        [AllowNull]
        public string Domain
        {
            get
            {
                return m_domain;
            }
            set
            {
                SetDomainAndKey(value ?? string.Empty);
                m_domain_implicit = false;
            }
        }

        internal bool DomainImplicit
        {
            get
            {
                return m_domain_implicit;
            }
            set
            {
                m_domain_implicit = value;
            }
        }

        public bool Expired
        {
            get
            {
                return (m_expires != DateTime.MinValue) && (m_expires.ToUniversalTime() <= DateTime.UtcNow);
            }
            set
            {
                if (value)
                {
                    m_expires = DateTime.UtcNow;
                }
            }
        }

        public DateTime Expires
        {
            get
            {
                return m_expires;
            }
            set
            {
                m_expires = value;
            }
        }

        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                if (string.IsNullOrEmpty(value) || !InternalSetName(value))
                {
                    throw new CookieException(SR.Format(SR.net_cookie_attribute, "Name", value ?? "<null>"));
                }
            }
        }
        internal bool InternalSetName(string? value)
        {
            if (string.IsNullOrEmpty(value)
                || value.StartsWith('$')
                || value.StartsWith(' ')
                || value.EndsWith(' ')
                || value.AsSpan().ContainsAny(s_reservedToNameChars))
            {
                m_name = string.Empty;
                return false;
            }
            m_name = value;
            return true;
        }

        [AllowNull]
        public string Path
        {
            get
            {
                return m_path;
            }
            set
            {
                m_path = value ?? string.Empty;
                m_path_implicit = false;
            }
        }

        internal bool Plain
        {
            get
            {
                return Variant == CookieVariant.Plain;
            }
        }

        internal Cookie Clone()
        {
            Cookie clonedCookie = new Cookie(m_name, m_value);

            // Copy over all the properties from the original cookie
            if (!m_port_implicit)
            {
                clonedCookie.Port = m_port;
            }
            if (!m_path_implicit)
            {
                clonedCookie.Path = m_path;
            }
            clonedCookie.m_domain = m_domain;

            // If the domain in the original cookie was implicit, we should preserve that property
            clonedCookie.DomainImplicit = m_domain_implicit;
            clonedCookie.m_domainKey = m_domainKey;
            clonedCookie.m_timeStamp = m_timeStamp;
            clonedCookie.Comment = m_comment;
            clonedCookie.CommentUri = m_commentUri;
            clonedCookie.HttpOnly = m_httpOnly;
            clonedCookie.Discard = m_discard;
            clonedCookie.Expires = m_expires;
            clonedCookie.Version = m_version;
            clonedCookie.Secure = m_secure;

            // The variant is set when we set properties like port/version. So,
            // we should copy over the variant from the original cookie after
            // we set all other properties
            clonedCookie.m_cookieVariant = m_cookieVariant;

            return clonedCookie;
        }

        private void SetDomainAndKey(string domain)
        {
            m_domain = domain;
            m_domainKey = CookieComparer.StripLeadingDot(m_domain).ToString().ToLowerInvariant();
        }

        // Implements Domain Matching following RFC 6265 rules with a special handling for single-label domains.
        // The method assumes that 'domain' has been stripped of its optional leading dot and converted to lower case.
        // It checks if the condition defined in https://datatracker.ietf.org/doc/html/rfc6265#section-5.1.3 is met:
        // [rephrased] 'host' domain-matches 'domain' if at least one of the following conditions hold:
        // - 'domain' and 'host' are identical.
        // - All of the following conditions hold:
        //    * 'domain' is a suffix of 'host'
        //    * The last character of 'host' that is not included in 'domain' is a "."
        //    * 'host' is a host name (i.e., not an IP address).
        // Beside the RFC 6265 rules, an extra condition is included for compatibility:
        // in case 'domain' is a single-label domain, an exact match is required.
        // This is to avoid matching top-level domains, for example "test.com" should not match "com",
        // however this does not prevent matching multi-label public suffixes, eg. "co.uk".
        // Note that browsers handle this by validating against the Public Suffix List (https://publicsuffix.org/)
        // which is a behavior under standardization in the latest RFC drafts: https://datatracker.ietf.org/doc/draft-ietf-httpbis-rfc6265bis/
        private static bool HostMatchesDomain(ReadOnlySpan<char> host, ReadOnlySpan<char> domain)
        {
            if (!host.EndsWith(domain, StringComparison.Ordinal))
            {
                return false;
            }

            // The last character of the 'host' that is not included in the domain
            int idxOfSeparator = host.Length - domain.Length - 1;
            if (idxOfSeparator < 0)
            {
                // 'host' and 'domain' are equal
                Debug.Assert(idxOfSeparator == -1);
                return true;
            }

            return host[idxOfSeparator] is '.' // The last character of 'host' that is not included in 'domain' is a "."
                && domain.Contains('.') // In case of single-label domains, there should be an exact match.
                && !IPAddress.IsValid(host); // If host is an IP address, there should be an exact match.
        }

        // According to spec we must assume default values for attributes but still
        // keep in mind that we must not include them into the requests.
        // We also check the validity of all attributes based on the version and variant (read RFC)
        //
        // To work properly this function must be called after cookie construction with
        // default (response) URI.
        internal void VerifyAndSetDefaults(CookieVariant variant, Uri uri)
        {
            string host = uri.Host;
            int port = uri.Port;
            string path = uri.AbsolutePath;

            // Set Variant. If version is zero => reset cookie to Version0 style
            if (Version == 0)
            {
                variant = CookieVariant.Plain;
            }
            else if (Version == 1 && variant == CookieVariant.Unknown)
            {
                // Since we don't expose Variant to an app, set it to Default
                variant = CookieVariant.Default;
            }
            m_cookieVariant = variant;

            // Check the name
            if (string.IsNullOrEmpty(m_name) ||
                m_name.StartsWith('$') ||
                m_name.StartsWith(' ') ||
                m_name.EndsWith(' ') ||
                m_name.AsSpan().ContainsAny(s_reservedToNameChars))
            {
                throw new CookieException(SR.Format(SR.net_cookie_attribute, "Name", m_name ?? "<null>"));
            }

            // Check the value
            if (m_value == null ||
                (!(m_value.Length > 2 && m_value.StartsWith('\"') && m_value.EndsWith('\"')) && m_value.AsSpan().ContainsAny(';', ',')))
            {
                throw new CookieException(SR.Format(SR.net_cookie_attribute, "Value", m_value ?? "<null>"));
            }

            // Check Comment syntax
            if (Comment != null && !(Comment.Length > 2 && Comment.StartsWith('\"') && Comment.EndsWith('\"'))
                && (Comment.AsSpan().ContainsAny(';', ',')))
            {
                throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.CommentAttributeName, Comment));
            }

            // Check Path syntax
            if (Path != null && !(Path.Length > 2 && Path.StartsWith('\"') && Path.EndsWith('\"'))
                && (Path.AsSpan().ContainsAny(';', ',')))
            {
                throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.PathAttributeName, Path));
            }

            // Check/set domain
            //
            // If domain is implicit => assume a) uri is valid, b) just set domain to uri hostname.
            if (m_domain_implicit)
            {
                SetDomainAndKey(host);
            }
            else
            {
                Debug.Assert(m_domainKey is not null);

                if (!IsValidDomainName(m_domainKey) || !HostMatchesDomain(host, m_domainKey))
                {
                    throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.DomainAttributeName, m_domain));
                }
            }

            // Check/Set Path
            if (m_path_implicit)
            {
                // This code assumes that the URI path is always valid and contains at least one '/'.
                switch (m_cookieVariant)
                {
                    case CookieVariant.Plain:
                        // As per RFC6265 5.1.4. (https://tools.ietf.org/html/rfc6265#section-5.1.4):
                        // | 2. If the uri-path is empty or if the first character of the uri-
                        // |    path is not a %x2F ("/") character, output %x2F ("/") and skip
                        // |    the remaining steps.
                        // | 3. If the uri-path contains no more than one %x2F ("/") character,
                        // |    output %x2F ("/") and skip the remaining step.
                        // Note: Normally Uri.AbsolutePath contains at least one "/" after parsing,
                        //       but it's possible construct Uri with an empty path using a custom UriParser
                        int lastSlash;
                        if (!path.StartsWith('/') || (lastSlash = path.LastIndexOf('/')) == 0)
                        {
                            m_path = "/";
                            break;
                        }

                        // | 4. Output the characters of the uri-path from the first character up
                        // |    to, but not including, the right-most %x2F ("/").
                        m_path = path.Substring(0, lastSlash);
                        break;
                    case CookieVariant.Rfc2109:
                        m_path = path.Substring(0, path.LastIndexOf('/')); // May be empty
                        break;

                    case CookieVariant.Rfc2965:
                    default:
                        // NOTE: this code is not resilient against future versions with different 'Path' semantics.
                        m_path = path.Substring(0, path.LastIndexOf('/') + 1);
                        break;
                }
            }

            // Set the default port if Port attribute was present but had no value.
            if (m_port_implicit == false && m_port.Length == 0)
            {
                m_port_list = new int[1] { port };
            }

            if (m_port_implicit == false)
            {
                // Port must match against the one from the uri.
                bool valid = false;
                foreach (int p in m_port_list!)
                {
                    if (p == port)
                    {
                        valid = true;
                        break;
                    }
                }
                if (!valid)
                {
                    throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.PortAttributeName, m_port));
                }
            }
        }

        // Very primitive test to make sure that the name does not have illegal characters
        // as per RFC 952 (relaxed on first char could be a digit and string can have '_').
        private static bool IsValidDomainName(ReadOnlySpan<char> name) =>
            !name.IsEmpty &&
            !name.ContainsAnyExcept(s_domainChars);

        [AllowNull]
        public string Port
        {
            get
            {
                return m_port;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    // "Port" is present but has no value.
                    // Therefore; the effective port value is implicit.
                    m_port_implicit = true;
                    m_port = string.Empty;
                }
                else
                {
                    // "Port" value is present, so we use the provided value rather than an implicit one.
                    m_port_implicit = false;
                    // Parse port list
                    if (!value.StartsWith('\"') || !value.EndsWith('\"'))
                    {
                        throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.PortAttributeName, value));
                    }
                    string[] ports = value.Split(PortSplitDelimiters, StringSplitOptions.RemoveEmptyEntries);
                    int[] parsedPorts = new int[ports.Length];

                    for (int i = 0; i < ports.Length; ++i)
                    {
                        if (!int.TryParse(ports[i], out int port))
                        {
                            throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.PortAttributeName, value));
                        }

                        // valid values for port 0 - 0xFFFF
                        if ((port < 0) || (port > 0xFFFF))
                        {
                            throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.PortAttributeName, value));
                        }

                        parsedPorts[i] = port;
                    }
                    m_port_list = parsedPorts;
                    m_port = value;
                    m_version = MaxSupportedVersion;
                    m_cookieVariant = CookieVariant.Rfc2965;
                }
            }
        }


        internal int[]? PortList
        {
            get
            {
                // PortList will be null if Port Attribute was omitted in the response.
                return m_port_list;
            }
        }

        public bool Secure
        {
            get
            {
                return m_secure;
            }
            set
            {
                m_secure = value;
            }
        }

        public DateTime TimeStamp
        {
            get
            {
                return m_timeStamp;
            }
        }

        [AllowNull]
        public string Value
        {
            get
            {
                return m_value;
            }
            set
            {
                m_value = value ?? string.Empty;
            }
        }

        internal CookieVariant Variant
        {
            get
            {
                return m_cookieVariant;
            }
        }

        // _domainKey member is set internally in VerifySetDefaults().
        // If it is not set then verification function was not called;
        // this should never happen.
        internal string DomainKey
        {
            get
            {
                return m_domain_implicit ? Domain : m_domainKey!;
            }
        }

        public int Version
        {
            get
            {
                return m_version;
            }
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                m_version = value;
                if (value > 0 && m_cookieVariant < CookieVariant.Rfc2109)
                {
                    m_cookieVariant = CookieVariant.Rfc2109;
                }
            }
        }

        public override bool Equals([NotNullWhen(true)] object? comparand)
        {
            return comparand is Cookie other
                    && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(Value, other.Value, StringComparison.Ordinal)
                    && string.Equals(Path, other.Path, StringComparison.Ordinal)
                    && CookieComparer.EqualDomains(Domain, other.Domain)
                    && (Version == other.Version);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(Name),
                StringComparer.Ordinal.GetHashCode(Value),
                StringComparer.Ordinal.GetHashCode(Path),
                StringComparer.OrdinalIgnoreCase.GetHashCode(DomainKey),
                Version);
        }

        public override string ToString()
        {
            StringBuilder sb = StringBuilderCache.Acquire();
            ToString(sb);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        internal void ToString(StringBuilder sb)
        {
            int beforeLength = sb.Length;

            // Add the Cookie version if necessary.
            if (Version != 0)
            {
                sb.Append(SpecialAttributeLiteral + CookieFields.VersionAttributeName + EqualsLiteral); // const strings
                if (IsQuotedVersion) sb.Append('"');
                sb.Append(NumberFormatInfo.InvariantInfo, $"{m_version}");
                if (IsQuotedVersion) sb.Append('"');
                sb.Append(SeparatorLiteral);
            }

            // Add the Cookie Name=Value pair.
            sb.Append(Name).Append(EqualsLiteral).Append(Value);

            if (!Plain)
            {
                // Add the Path if necessary.
                if (!m_path_implicit && m_path.Length > 0)
                {
                    sb.Append(SeparatorLiteral + SpecialAttributeLiteral + CookieFields.PathAttributeName + EqualsLiteral); // const strings
                    sb.Append(m_path);
                }

                // Add the Domain if necessary.
                if (!m_domain_implicit && m_domain.Length > 0)
                {
                    sb.Append(SeparatorLiteral + SpecialAttributeLiteral + CookieFields.DomainAttributeName + EqualsLiteral); // const strings
                    if (IsQuotedDomain) sb.Append('"');
                    sb.Append(m_domain);
                    if (IsQuotedDomain) sb.Append('"');
                }
            }

            // Add the Port if necessary.
            if (!m_port_implicit)
            {
                sb.Append(SeparatorLiteral + SpecialAttributeLiteral + CookieFields.PortAttributeName); // const strings
                if (m_port.Length > 0)
                {
                    sb.Append(EqualsLiteral);
                    sb.Append(m_port);
                }
            }

            // Check to see whether the only thing we added was "=", and if so,
            // remove it so that we leave the StringBuilder unchanged in contents.
            int afterLength = sb.Length;
            if (afterLength == (1 + beforeLength) && sb[beforeLength] == '=')
            {
                sb.Length = beforeLength;
            }
        }

        internal string? ToServerString()
        {
            string result = Name + EqualsLiteral + Value;
            if (m_comment != null && m_comment.Length > 0)
            {
                result += SeparatorLiteral + CookieFields.CommentAttributeName + EqualsLiteral + m_comment;
            }
            if (m_commentUri != null)
            {
                result += SeparatorLiteral + CookieFields.CommentUrlAttributeName + EqualsLiteral + QuotesLiteral + m_commentUri.ToString() + QuotesLiteral;
            }
            if (m_discard)
            {
                result += SeparatorLiteral + CookieFields.DiscardAttributeName;
            }
            if (!m_domain_implicit && m_domain != null && m_domain.Length > 0)
            {
                result += SeparatorLiteral + CookieFields.DomainAttributeName + EqualsLiteral + m_domain;
            }
            if (Expires != DateTime.MinValue)
            {
                int seconds = (int)(Expires.ToUniversalTime() - DateTime.UtcNow).TotalSeconds;
                if (seconds < 0)
                {
                    // This means that the cookie has already expired. Set Max-Age to 0
                    // so that the client will discard the cookie immediately.
                    seconds = 0;
                }
                result += SeparatorLiteral + CookieFields.MaxAgeAttributeName + EqualsLiteral + seconds.ToString(NumberFormatInfo.InvariantInfo);
            }
            if (!m_path_implicit && m_path != null && m_path.Length > 0)
            {
                result += SeparatorLiteral + CookieFields.PathAttributeName + EqualsLiteral + m_path;
            }
            if (!Plain && !m_port_implicit && m_port != null && m_port.Length > 0)
            {
                // QuotesLiteral are included in _port.
                result += SeparatorLiteral + CookieFields.PortAttributeName + EqualsLiteral + m_port;
            }
            if (m_version > 0)
            {
                result += SeparatorLiteral + CookieFields.VersionAttributeName + EqualsLiteral + m_version.ToString(NumberFormatInfo.InvariantInfo);
            }
            return result == "=" ? null : result;
        }
    }
}

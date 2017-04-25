// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Net
{
    internal enum CookieVariant
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
    public sealed class Cookie
    {
        // NOTE: these two constants must change together.
        internal const int MaxSupportedVersion = 1;
        internal const string MaxSupportedVersionString = "1";

        internal const string SeparatorLiteral = "; ";
        internal const string EqualsLiteral = "=";
        internal const string QuotesLiteral = "\"";
        internal const string SpecialAttributeLiteral = "$";

        internal static readonly char[] PortSplitDelimiters = new char[] { ' ', ',', '\"' };
        internal static readonly char[] ReservedToName = new char[] { ' ', '\t', '\r', '\n', '=', ';', ',' };
        internal static readonly char[] ReservedToValue = new char[] { ';', ',' };

        private string _comment = string.Empty;
        private Uri _commentUri = null;
        private CookieVariant _cookieVariant = CookieVariant.Plain;
        private bool _discard = false;
        private string _domain = string.Empty;
        private bool _domainImplicit = true;
        private DateTime _expires = DateTime.MinValue;
        private string _name = string.Empty;
        private string _path = string.Empty;
        private bool _pathImplicit = true;
        private string _port = string.Empty;
        private bool _portImplicit = true;
        private int[] _portList = null;
        private bool _secure = false;
        private bool _httpOnly = false;
        private DateTime _timeStamp = DateTime.Now;
        private string _value = string.Empty;
        private int _version = 0;

        private string _domainKey = string.Empty;
        internal bool IsQuotedVersion = false;
        internal bool IsQuotedDomain = false;

#if DEBUG
        static Cookie()
        {
            Debug.Assert(MaxSupportedVersion.ToString(NumberFormatInfo.InvariantInfo).Equals(MaxSupportedVersionString, StringComparison.Ordinal));
        }
#endif

        public Cookie()
        {
        }

        public Cookie(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public Cookie(string name, string value, string path)
            : this(name, value)
        {
            Path = path;
        }

        public Cookie(string name, string value, string path, string domain)
            : this(name, value, path)
        {
            Domain = domain;
        }

        public string Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                _comment = value ?? string.Empty;
            }
        }

        public Uri CommentUri
        {
            get
            {
                return _commentUri;
            }
            set
            {
                _commentUri = value;
            }
        }


        public bool HttpOnly
        {
            get
            {
                return _httpOnly;
            }
            set
            {
                _httpOnly = value;
            }
        }


        public bool Discard
        {
            get
            {
                return _discard;
            }
            set
            {
                _discard = value;
            }
        }

        public string Domain
        {
            get
            {
                return _domain;
            }
            set
            {
                _domain = value ?? string.Empty;
                _domainImplicit = false;
                _domainKey = string.Empty; // _domainKey will be set when adding this cookie to a container.
            }
        }

        internal bool DomainImplicit
        {
            get
            {
                return _domainImplicit;
            }
            set
            {
                _domainImplicit = value;
            }
        }

        public bool Expired
        {
            get
            {
                return (_expires != DateTime.MinValue) && (_expires.ToLocalTime() <= DateTime.Now);
            }
            set
            {
                if (value == true)
                {
                    _expires = DateTime.Now;
                }
            }
        }

        public DateTime Expires
        {
            get
            {
                return _expires;
            }
            set
            {
                _expires = value;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (String.IsNullOrEmpty(value) || !InternalSetName(value))
                {
                    throw new CookieException(SR.Format(SR.net_cookie_attribute, "Name", value == null ? "<null>" : value));
                }
            }
        }

        internal bool InternalSetName(string value)
        {
            if (String.IsNullOrEmpty(value) || value[0] == '$' || value.IndexOfAny(ReservedToName) != -1)
            {
                _name = string.Empty;
                return false;
            }
            _name = value;
            return true;
        }

        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                _path = value ?? string.Empty;
                _pathImplicit = false;
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
            Cookie clonedCookie = new Cookie(_name, _value);

            // Copy over all the properties from the original cookie
            if (!_portImplicit)
            {
                clonedCookie.Port = _port;
            }
            if (!_pathImplicit)
            {
                clonedCookie.Path = _path;
            }
            clonedCookie.Domain = _domain;

            // If the domain in the original cookie was implicit, we should preserve that property
            clonedCookie.DomainImplicit = _domainImplicit;
            clonedCookie._timeStamp = _timeStamp;
            clonedCookie.Comment = _comment;
            clonedCookie.CommentUri = _commentUri;
            clonedCookie.HttpOnly = _httpOnly;
            clonedCookie.Discard = _discard;
            clonedCookie.Expires = _expires;
            clonedCookie.Version = _version;
            clonedCookie.Secure = _secure;

            // The variant is set when we set properties like port/version. So, 
            // we should copy over the variant from the original cookie after 
            // we set all other properties
            clonedCookie._cookieVariant = _cookieVariant;

            return clonedCookie;
        }

        private static bool IsDomainEqualToHost(string domain, string host)
        {
            // +1 in the host length is to account for the leading dot in domain
            return (host.Length + 1 == domain.Length) &&
                   (string.Compare(host, 0, domain, 1, host.Length, StringComparison.OrdinalIgnoreCase) == 0);
        }

        // According to spec we must assume default values for attributes but still
        // keep in mind that we must not include them into the requests.
        // We also check the validity of all attributes based on the version and variant (read RFC)
        //
        // To work properly this function must be called after cookie construction with
        // default (response) URI AND setDefault == true
        //
        // Afterwards, the function can be called many times with other URIs and
        // setDefault == false to check whether this cookie matches given uri
        internal bool VerifySetDefaults(CookieVariant variant, Uri uri, bool isLocalDomain, string localDomain, bool setDefault, bool shouldThrow)
        {
            string host = uri.Host;
            int port = uri.Port;
            string path = uri.AbsolutePath;
            bool valid = true;

            if (setDefault)
            {
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
                _cookieVariant = variant;
            }

            // Check the name
            if (_name == null || _name.Length == 0 || _name[0] == '$' || _name.IndexOfAny(ReservedToName) != -1)
            {
                if (shouldThrow)
                {
                    throw new CookieException(SR.Format(SR.net_cookie_attribute, "Name", _name == null ? "<null>" : _name));
                }
                return false;
            }

            // Check the value
            if (_value == null ||
                (!(_value.Length > 2 && _value[0] == '\"' && _value[_value.Length - 1] == '\"') && _value.IndexOfAny(ReservedToValue) != -1))
            {
                if (shouldThrow)
                {
                    throw new CookieException(SR.Format(SR.net_cookie_attribute, "Value", _value == null ? "<null>" : _value));
                }
                return false;
            }

            // Check Comment syntax
            if (Comment != null && !(Comment.Length > 2 && Comment[0] == '\"' && Comment[Comment.Length - 1] == '\"')
                && (Comment.IndexOfAny(ReservedToValue) != -1))
            {
                if (shouldThrow)
                {
                    throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.CommentAttributeName, Comment));
                }
                return false;
            }

            // Check Path syntax
            if (Path != null && !(Path.Length > 2 && Path[0] == '\"' && Path[Path.Length - 1] == '\"')
                && (Path.IndexOfAny(ReservedToValue) != -1))
            {
                if (shouldThrow)
                {
                    throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.PathAttributeName, Path));
                }
                return false;
            }

            // Check/set domain
            //
            // If domain is implicit => assume a) uri is valid, b) just set domain to uri hostname.
            if (setDefault && _domainImplicit == true)
            {
                _domain = host;
            }
            else
            {
                if (!_domainImplicit)
                {
                    // Forwarding note: If Uri.Host is of IP address form then the only supported case
                    // is for IMPLICIT domain property of a cookie.
                    // The code below (explicit cookie.Domain value) will try to parse Uri.Host IP string
                    // as a fqdn and reject the cookie.

                    // Aliasing since we might need the KeyValue (but not the original one).
                    string domain = _domain;

                    // Syntax check for Domain charset plus empty string.
                    if (!DomainCharsTest(domain))
                    {
                        if (shouldThrow)
                        {
                            throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.DomainAttributeName, domain == null ? "<null>" : domain));
                        }
                        return false;
                    }

                    // Domain must start with '.' if set explicitly.
                    if (domain[0] != '.')
                    {
                        if (!(variant == CookieVariant.Rfc2965 || variant == CookieVariant.Plain))
                        {
                            if (shouldThrow)
                            {
                                throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.DomainAttributeName, _domain));
                            }
                            return false;
                        }
                        domain = '.' + domain;
                    }

                    int host_dot = host.IndexOf('.');

                    // First quick check is for pushing a cookie into the local domain.
                    if (isLocalDomain && string.Equals(localDomain, domain, StringComparison.OrdinalIgnoreCase))
                    {
                        valid = true;
                    }
                    else if (domain.IndexOf('.', 1, domain.Length - 2) == -1)
                    {
                        // A single label domain is valid only if the domain is exactly the same as the host specified in the URI.
                        if (!IsDomainEqualToHost(domain, host))
                        {
                            valid = false;
                        }
                    }
                    else if (variant == CookieVariant.Plain)
                    {
                        // We distinguish between Version0 cookie and other versions on domain issue.
                        // According to Version0 spec a domain must be just a substring of the hostname.

                        if (!IsDomainEqualToHost(domain, host))
                        {
                            if (host.Length <= domain.Length ||
                                (string.Compare(host, host.Length - domain.Length, domain, 0, domain.Length, StringComparison.OrdinalIgnoreCase) != 0))
                            {
                                valid = false;
                            }
                        }
                    }
                    else if (host_dot == -1 ||
                             domain.Length != host.Length - host_dot ||
                             (string.Compare(host, host_dot, domain, 0, domain.Length, StringComparison.OrdinalIgnoreCase) != 0))
                    {
                        // Starting from the first dot, the host must match the domain.
                        //
                        // For null hosts, the host must match the domain exactly.
                        if (!IsDomainEqualToHost(domain, host))
                        {
                            valid = false;
                        }
                    }

                    if (valid)
                    {
                        _domainKey = domain.ToLowerInvariant();
                    }
                }
                else
                {
                    // For implicitly set domain AND at the set_default == false time
                    // we simply need to match uri.Host against m_domain.
                    if (!string.Equals(host, _domain, StringComparison.OrdinalIgnoreCase))
                    {
                        valid = false;
                    }
                }
                if (!valid)
                {
                    if (shouldThrow)
                    {
                        throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.DomainAttributeName, _domain));
                    }
                    return false;
                }
            }

            // Check/Set Path
            if (setDefault && _pathImplicit == true)
            {
                // This code assumes that the URI path is always valid and contains at least one '/'.
                switch (_cookieVariant)
                {
                    case CookieVariant.Plain:
                        _path = path;
                        break;
                    case CookieVariant.Rfc2109:
                        _path = path.Substring(0, path.LastIndexOf('/')); // May be empty
                        break;

                    case CookieVariant.Rfc2965:
                    default:
                        // NOTE: this code is not resilient against future versions with different 'Path' semantics.
                        _path = path.Substring(0, path.LastIndexOf('/') + 1);
                        break;
                }
            }
            else
            {
                // Check current path (implicit/explicit) against given URI.
                if (!path.StartsWith(CookieParser.CheckQuoted(_path)))
                {
                    if (shouldThrow)
                    {
                        throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.PathAttributeName, _path));
                    }
                    return false;
                }
            }

            // Set the default port if Port attribute was present but had no value.
            if (setDefault && (_portImplicit == false && _port.Length == 0))
            {
                _portList = new int[1] { port };
            }

            if (_portImplicit == false)
            {
                // Port must match against the one from the uri.
                valid = false;
                foreach (int p in _portList)
                {
                    if (p == port)
                    {
                        valid = true;
                        break;
                    }
                }
                if (!valid)
                {
                    if (shouldThrow)
                    {
                        throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.PortAttributeName, _port));
                    }
                    return false;
                }
            }
            return true;
        }

        // Very primitive test to make sure that the name does not have illegal characters
        // as per RFC 952 (relaxed on first char could be a digit and string can have '_').
        private static bool DomainCharsTest(string name)
        {
            if (name == null || name.Length == 0)
            {
                return false;
            }
            for (int i = 0; i < name.Length; ++i)
            {
                char ch = name[i];
                if (!((ch >= '0' && ch <= '9') ||
                      (ch == '.' || ch == '-') ||
                      (ch >= 'a' && ch <= 'z') ||
                      (ch >= 'A' && ch <= 'Z') ||
                      (ch == '_')))
                {
                    return false;
                }
            }
            return true;
        }

        public string Port
        {
            get
            {
                return _port;
            }
            set
            {
                _portImplicit = false;
                if (string.IsNullOrEmpty(value))
                {
                    // "Port" is present but has no value.
                    _port = string.Empty;
                }
                else
                {
                    // Parse port list
                    if (value[0] != '\"' || value[value.Length - 1] != '\"')
                    {
                        throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.PortAttributeName, value));
                    }
                    string[] ports = value.Split(PortSplitDelimiters);

                    List<int> portList = new List<int>();
                    int port;

                    for (int i = 0; i < ports.Length; ++i)
                    {
                        // Skip spaces
                        if (ports[i] != string.Empty)
                        {
                            if (!Int32.TryParse(ports[i], out port))
                            {
                                throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.PortAttributeName, value));
                            }

                            // valid values for port 0 - 0xFFFF
                            if ((port < 0) || (port > 0xFFFF))
                            {
                                throw new CookieException(SR.Format(SR.net_cookie_attribute, CookieFields.PortAttributeName, value));
                            }

                            portList.Add(port);
                        }
                    }
                    _portList = portList.ToArray();
                    _port = value;
                    _version = MaxSupportedVersion;
                    _cookieVariant = CookieVariant.Rfc2965;
                }
            }
        }


        internal int[] PortList
        {
            get
            {
                // PortList will be null if Port Attribute was omitted in the response.
                return _portList;
            }
        }

        public bool Secure
        {
            get
            {
                return _secure;
            }
            set
            {
                _secure = value;
            }
        }

        public DateTime TimeStamp
        {
            get
            {
                return _timeStamp;
            }
        }

        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value ?? string.Empty;
            }
        }

        internal CookieVariant Variant
        {
            get
            {
                return _cookieVariant;
            }
            set
            {
                // Only set by HttpListenerRequest::Cookies_get()
#if !uap
                if (value != CookieVariant.Rfc2965)
                {
                    NetEventSource.Fail(this, $"value != Rfc2965:{value}");
                }
#endif
                _cookieVariant = value;
            }
        }

        // _domainKey member is set internally in VerifySetDefaults().
        // If it is not set then verification function was not called;
        // this should never happen.
        internal string DomainKey
        {
            get
            {
                return _domainImplicit ? Domain : _domainKey;
            }
        }

        public int Version
        {
            get
            {
                return _version;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _version = value;
                if (value > 0 && _cookieVariant < CookieVariant.Rfc2109)
                {
                    _cookieVariant = CookieVariant.Rfc2109;
                }
            }
        }

        public override bool Equals(object comparand)
        {
            Cookie other = comparand as Cookie;

            return other != null
                    && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(Value, other.Value, StringComparison.Ordinal)
                    && string.Equals(Path, other.Path, StringComparison.Ordinal)
                    && string.Equals(Domain, other.Domain, StringComparison.OrdinalIgnoreCase)
                    && (Version == other.Version);
        }

        public override int GetHashCode()
        {
            return (Name + "=" + Value + ";" + Path + "; " + Domain + "; " + Version).GetHashCode();
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
                sb.Append(_version.ToString(NumberFormatInfo.InvariantInfo));
                if (IsQuotedVersion) sb.Append('"');
                sb.Append(SeparatorLiteral);
            }

            // Add the Cookie Name=Value pair.
            sb.Append(Name).Append(EqualsLiteral).Append(Value);

            if (!Plain)
            {
                // Add the Path if necessary.
                if (!_pathImplicit && _path.Length > 0)
                {
                    sb.Append(SeparatorLiteral + SpecialAttributeLiteral + CookieFields.PathAttributeName + EqualsLiteral); // const strings
                    sb.Append(_path);
                }

                // Add the Domain if necessary.
                if (!_domainImplicit && _domain.Length > 0)
                {
                    sb.Append(SeparatorLiteral + SpecialAttributeLiteral + CookieFields.DomainAttributeName + EqualsLiteral); // const strings
                    if (IsQuotedDomain) sb.Append('"');
                    sb.Append(_domain);
                    if (IsQuotedDomain) sb.Append('"');
                }
            }

            // Add the Port if necessary.
            if (!_portImplicit)
            {
                sb.Append(SeparatorLiteral + SpecialAttributeLiteral + CookieFields.PortAttributeName); // const strings
                if (_port.Length > 0)
                {
                    sb.Append(EqualsLiteral);
                    sb.Append(_port);
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

        internal string ToServerString()
        {
            string result = Name + EqualsLiteral + Value;
            if (_comment != null && _comment.Length > 0)
            {
                result += SeparatorLiteral + CookieFields.CommentAttributeName + EqualsLiteral + _comment;
            }
            if (_commentUri != null)
            {
                result += SeparatorLiteral + CookieFields.CommentUrlAttributeName + EqualsLiteral + QuotesLiteral + _commentUri.ToString() + QuotesLiteral;
            }
            if (_discard)
            {
                result += SeparatorLiteral + CookieFields.DiscardAttributeName;
            }
            if (!_domainImplicit && _domain != null && _domain.Length > 0)
            {
                result += SeparatorLiteral + CookieFields.DomainAttributeName + EqualsLiteral + _domain;
            }
            if (Expires != DateTime.MinValue)
            {
                int seconds = (int)(Expires.ToLocalTime() - DateTime.Now).TotalSeconds;
                if (seconds < 0)
                {
                    // This means that the cookie has already expired. Set Max-Age to 0
                    // so that the client will discard the cookie immediately.
                    seconds = 0;
                }
                result += SeparatorLiteral + CookieFields.MaxAgeAttributeName + EqualsLiteral + seconds.ToString(NumberFormatInfo.InvariantInfo);
            }
            if (!_pathImplicit && _path != null && _path.Length > 0)
            {
                result += SeparatorLiteral + CookieFields.PathAttributeName + EqualsLiteral + _path;
            }
            if (!Plain && !_portImplicit && _port != null && _port.Length > 0)
            {
                // QuotesLiteral are included in _port.
                result += SeparatorLiteral + CookieFields.PortAttributeName + EqualsLiteral + _port;
            }
            if (_version > 0)
            {
                result += SeparatorLiteral + CookieFields.VersionAttributeName + EqualsLiteral + _version.ToString(NumberFormatInfo.InvariantInfo);
            }
            return result == EqualsLiteral ? null : result;
        }

#if DEBUG
        internal void Dump()
        {
#if !uap
            if (NetEventSource.IsEnabled)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, 
                                  "Cookie: "        + ToString() + "->\n"
                                + "\tComment    = " + Comment + "\n"
                                + "\tCommentUri = " + CommentUri + "\n"
                                + "\tDiscard    = " + Discard + "\n"
                                + "\tDomain     = " + Domain + "\n"
                                + "\tExpired    = " + Expired + "\n"
                                + "\tExpires    = " + Expires + "\n"
                                + "\tName       = " + Name + "\n"
                                + "\tPath       = " + Path + "\n"
                                + "\tPort       = " + Port + "\n"
                                + "\tSecure     = " + Secure + "\n"
                                + "\tTimeStamp  = " + TimeStamp + "\n"
                                + "\tValue      = " + Value + "\n"
                                + "\tVariant    = " + Variant + "\n"
                                + "\tVersion    = " + Version + "\n"
                                + "\tHttpOnly    = " + HttpOnly + "\n"
                                );
            }
#endif
        }
#endif
    }

    internal static class CookieComparer
    {
        internal static int Compare(Cookie left, Cookie right)
        {
            int result;

            if ((result = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase)) != 0)
            {
                return result;
            }

            if ((result = string.Compare(left.Domain, right.Domain, StringComparison.OrdinalIgnoreCase)) != 0)
            {
                return result;
            }

            // NB: The only path is case sensitive as per spec. However, many Windows applications assume
            //     case-insensitivity.
            if ((result = string.Compare(left.Path, right.Path, StringComparison.Ordinal)) != 0)
            {
                return result;
            }

            // They are equal here even if variants are still different.
            return 0;
        }
    }
}

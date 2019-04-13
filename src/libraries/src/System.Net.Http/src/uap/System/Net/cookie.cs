// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

// The uap #define is used in some source files to indicate we are compiling classes
// directly into the UWP System.Net.Http.dll implementation assembly in order to use internal class
// methods. Internal methods are needed in order to map cookie response headers from the WinRT Windows.Web.Http APIs.
// Windows.Web.Http is used underneath the System.Net.Http classes on .NET Native. Having other similarly
// named classes would normally conflict with the public System.Net namespace classes.
// So, we need to move the classes to a different namespace. Those classes are never
// exposed up to user code so there isn't a problem.  In the future, we might expose some of the internal methods
// as new public APIs and then we can remove this duplicate source code inclusion in the binaries.
#if uap
namespace System.Net.Internal
#else
namespace System.Net
#endif
{
    internal enum CookieVariant
    {
        Unknown,
        Plain,
        Rfc2109,
        Rfc2965,
        Default = Rfc2109
    }

    //
    // Cookie class
    //
    //  Adheres to RFC 2965
    //
    //  Currently, only client-side cookies. The cookie classes know how to
    //  parse a set-cookie format string, but not a cookie format string
    //  (e.g. "Cookie: $Version=1; name=value; $Path=/foo; $Secure")
    //

    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public sealed class Cookie
    {
        internal const int MaxSupportedVersion = 1;
        internal const string CommentAttributeName = "Comment";
        internal const string CommentUrlAttributeName = "CommentURL";
        internal const string DiscardAttributeName = "Discard";
        internal const string DomainAttributeName = "Domain";
        internal const string ExpiresAttributeName = "Expires";
        internal const string MaxAgeAttributeName = "Max-Age";
        internal const string PathAttributeName = "Path";
        internal const string PortAttributeName = "Port";
        internal const string SecureAttributeName = "Secure";
        internal const string VersionAttributeName = "Version";
        internal const string HttpOnlyAttributeName = "HttpOnly";

        internal const string SeparatorLiteral = "; ";
        internal const string EqualsLiteral = "=";
        internal const string QuotesLiteral = "\"";
        internal const string SpecialAttributeLiteral = "$";

        internal static readonly char[] PortSplitDelimiters = new char[] { ' ', ',', '\"' };
        // Space (' ') should be reserved as well per RFCs, but major web browsers support it and some web sites use it - so we support it too
        internal static readonly char[] Reserved2Name = new char[] { '\t', '\r', '\n', '=', ';', ',' };
        internal static readonly char[] Reserved2Value = new char[] { ';', ',' };

        // fields

        string m_comment = string.Empty;
        Uri m_commentUri = null;
        CookieVariant m_cookieVariant = CookieVariant.Plain;
        bool m_discard = false;
        string m_domain = string.Empty;
        bool m_domain_implicit = true;
        DateTime m_expires = DateTime.MinValue;
        string m_name = string.Empty;
        string m_path = string.Empty;
        bool m_path_implicit = true;
        string m_port = string.Empty;
        bool m_port_implicit = true;
        int[] m_port_list = null;
        bool m_secure = false;
        bool m_httpOnly = false;
        DateTime m_timeStamp = DateTime.Now;
        string m_value = string.Empty;
        int m_version = 0;

        string m_domainKey = string.Empty;
        internal bool IsQuotedVersion = false;
        internal bool IsQuotedDomain = false;


        // constructors

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Cookie()
        {
        }


        //public Cookie(string cookie) {
        //    if ((cookie == null) || (cookie == String.Empty)) {
        //        throw new ArgumentException("cookie");
        //    }
        //    Parse(cookie.Trim());
        //    Validate();
        //}

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Cookie(string name, string value)
        {
            Name = name;
            Value = value;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Cookie(string name, string value, string path)
            : this(name, value)
        {
            Path = path;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Cookie(string name, string value, string path, string domain)
            : this(name, value, path)
        {
            Domain = domain;
        }

        // properties

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Comment
        {
            get
            {
                return m_comment;
            }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                m_comment = value;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Uri CommentUri
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


        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
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

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Domain
        {
            get
            {
                return m_domain;
            }
            set
            {
                m_domain = (value == null ? string.Empty : value);
                m_domain_implicit = false;
                m_domainKey = string.Empty; //this will get it value when adding into the Container.
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

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Expired
        {
            get
            {
                return (m_expires != DateTime.MinValue) && (m_expires.ToLocalTime() <= DateTime.Now);
            }
            set
            {
                if (value == true)
                {
                    m_expires = DateTime.Now;
                }
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
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

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
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
                    throw new CookieException(SR.Format(SR.net_cookie_attribute, "Name", value == null ? "<null>" : value));
                }
            }
        }

        internal bool InternalSetName(string value)
        {
            if (string.IsNullOrEmpty(value) || value[0] == '$' || value.IndexOfAny(Reserved2Name) != -1 ||  value[0] == ' ' || value[value.Length - 1] == ' ')
            {
                m_name = string.Empty;
                return false;
            }
            m_name = value;
            return true;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Path
        {
            get
            {
                return m_path;
            }
            set
            {
                m_path = (value == null ? string.Empty : value);
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

            //
            // Copy over all the properties from the original cookie
            //
            if (!m_port_implicit)
            {
                clonedCookie.Port = m_port;
            }
            if (!m_path_implicit)
            {
                clonedCookie.Path = m_path;
            }
            clonedCookie.Domain = m_domain;
            //
            // If the domain in the original cookie was implicit, we should preserve that property
            clonedCookie.DomainImplicit = m_domain_implicit;
            clonedCookie.m_timeStamp = m_timeStamp;
            clonedCookie.Comment = m_comment;
            clonedCookie.CommentUri = m_commentUri;
            clonedCookie.HttpOnly = m_httpOnly;
            clonedCookie.Discard = m_discard;
            clonedCookie.Expires = m_expires;
            clonedCookie.Version = m_version;
            clonedCookie.Secure = m_secure;

            //
            // The variant is set when we set properties like port/version. So, 
            // we should copy over the variant from the original cookie after 
            // we set all other properties
            clonedCookie.m_cookieVariant = m_cookieVariant;

            return clonedCookie;
        }

        private static bool IsDomainEqualToHost(string domain, string host)
        {
            //
            // +1 in the host length is to account for the leading dot in domain
            return (host.Length + 1 == domain.Length) &&
                (string.Compare(host, 0, domain, 1, host.Length, StringComparison.OrdinalIgnoreCase) == 0);
        }

        //
        // According to spec we must assume default values for attributes but still
        // keep in mind that we must not include them into the requests.
        // We also check the validity of all attributes based on the version and variant (read RFC)
        //
        // To work properly this function must be called after cookie construction with
        // default (response) URI AND set_default == true
        //
        // Afterwards, the function can be called many times with other URIs and
        // set_default == false to check whether this cookie matches given uri
        //

        internal bool VerifySetDefaults(CookieVariant variant, Uri uri, bool isLocalDomain, string localDomain, bool set_default, bool isThrow)
        {
            string host = uri.Host;
            int port = uri.Port;
            string path = uri.AbsolutePath;
            bool valid = true;

            if (set_default)
            {
                // Set Variant. If version is zero => reset cookie to Version0 style
                if (Version == 0)
                {
                    variant = CookieVariant.Plain;
                }
                else if (Version == 1 && variant == CookieVariant.Unknown)
                {
                    //since we don't expose Variant to an app, set it to Default
                    variant = CookieVariant.Default;
                }
                m_cookieVariant = variant;
            }

            //Check the name
            if (string.IsNullOrEmpty(m_name) || m_name[0] == '$' || m_name.IndexOfAny(Reserved2Name) != -1 ||  m_name[0] == ' ' || m_name[m_name.Length - 1] == ' ')
            {
                if (isThrow)
                {
                    throw new CookieException(SR.Format(SR.net_cookie_attribute, "Name", m_name == null ? "<null>" : m_name));
                }
                return false;
            }

            //Check the value
            if (m_value == null ||
                (!(m_value.Length > 2 && m_value[0] == '\"' && m_value[m_value.Length - 1] == '\"') && m_value.IndexOfAny(Reserved2Value) != -1))
            {
                if (isThrow)
                {
                    throw new CookieException(SR.Format(SR.net_cookie_attribute, "Value", m_value == null ? "<null>" : m_value));
                }
                return false;
            }

            //Check Comment syntax
            if (Comment != null && !(Comment.Length > 2 && Comment[0] == '\"' && Comment[Comment.Length - 1] == '\"')
                && (Comment.IndexOfAny(Reserved2Value) != -1))
            {
                if (isThrow)
                    throw new CookieException(SR.Format(SR.net_cookie_attribute, CommentAttributeName, Comment));
                return false;
            }

            //Check Path syntax
            if (Path != null && !(Path.Length > 2 && Path[0] == '\"' && Path[Path.Length - 1] == '\"')
                && (Path.IndexOfAny(Reserved2Value) != -1))
            {
                if (isThrow)
                {
                    throw new CookieException(SR.Format(SR.net_cookie_attribute, PathAttributeName, Path));
                }
                return false;
            }

            //Check/set domain
            // if domain is implicit => assume a) uri is valid, b) just set domain to uri hostname
            if (set_default && m_domain_implicit == true)
            {
                m_domain = host;
            }
            else
            {
                if (!m_domain_implicit)
                {
                    // Forwarding note: If Uri.Host is of IP address form then the only supported case
                    // is for IMPLICIT domain property of a cookie.
                    // The below code (explicit cookie.Domain value) will try to parse Uri.Host IP string
                    // as a fqdn and reject the cookie

                    //aliasing since we might need the KeyValue (but not the original one)
                    string domain = m_domain;

                    //Syntax check for Domain charset plus empty string
                    if (!DomainCharsTest(domain))
                    {
                        if (isThrow)
                        {
                            throw new CookieException(SR.Format(SR.net_cookie_attribute, DomainAttributeName, domain == null ? "<null>" : domain));
                        }
                        return false;
                    }

                    //domain must start with '.' if set explicitly
                    if (domain[0] != '.')
                    {
                        if (!(variant == CookieVariant.Rfc2965 || variant == CookieVariant.Plain))
                        {
                            if (isThrow)
                            {
                                throw new CookieException(SR.Format(SR.net_cookie_attribute, DomainAttributeName, m_domain));
                            }
                            return false;
                        }
                        domain = '.' + domain;
                    }

                    int host_dot = host.IndexOf('.');

                    // First quick check is for pushing a cookie into the local domain
                    if (isLocalDomain && string.Equals(localDomain, domain, StringComparison.OrdinalIgnoreCase))
                    {
                        valid = true;
                    }
                    else if (domain.IndexOf('.', 1, domain.Length - 2) == -1)
                    {
                        // A single label domain is valid only if the domain is exactly the same as the host specified in the URI
                        if (!IsDomainEqualToHost(domain, host))
                        {
                            valid = false;
                        }
                    }
                    else if (variant == CookieVariant.Plain)
                    {
                        // We distinguish between Version0 cookie and other versions on domain issue
                        // According to Version0 spec a domain must be just a substring of the hostname

                        if (!IsDomainEqualToHost(domain, host))
                        {
                            if (host.Length <= domain.Length ||
                                string.Compare(host, host.Length - domain.Length, domain, 0, domain.Length, StringComparison.OrdinalIgnoreCase) != 0)
                            {
                                valid = false;
                            }
                        }
                    }
                    else if (host_dot == -1 ||
                             domain.Length != host.Length - host_dot ||
                             string.Compare(host, host_dot, domain, 0, domain.Length, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        //starting the first dot, the host must match the domain

                        // for null hosts, the host must match the domain exactly
                        if (!IsDomainEqualToHost(domain, host))
                        {
                            valid = false;
                        }
                    }

                    if (valid)
                    {
                        m_domainKey = domain.ToLowerInvariant();
                    }
                }
                else
                {
                    // for implicitly set domain AND at the set_default==false time
                    // we simply got to match uri.Host against m_domain
                    if (!string.Equals(host, m_domain, StringComparison.OrdinalIgnoreCase))
                    {
                        valid = false;
                    }
                }
                if (!valid)
                {
                    if (isThrow)
                    {
                        throw new CookieException(SR.Format(SR.net_cookie_attribute, DomainAttributeName, m_domain));
                    }
                    return false;
                }
            }


            //Check/Set Path

            if (set_default && m_path_implicit == true)
            {
                //assuming that uri path is always valid and contains at least one '/'
                switch (m_cookieVariant)
                {
                    case CookieVariant.Plain:
                        m_path = path;
                        break;
                    case CookieVariant.Rfc2109:
                        m_path = path.Substring(0, path.LastIndexOf('/')); //may be empty
                        break;

                    case CookieVariant.Rfc2965:
                    default:                //hope future versions will have same 'Path' semantic?
                        m_path = path.Substring(0, path.LastIndexOf('/') + 1);
                        break;
                }
            }
            else
            {
                //check current path (implicit/explicit) against given uri
                if (!path.StartsWith(CookieParser.CheckQuoted(m_path)))
                {
                    if (isThrow)
                    {
                        throw new CookieException(SR.Format(SR.net_cookie_attribute, PathAttributeName, m_path));
                    }
                    return false;
                }
            }

            // set the default port if Port attribute was present but had no value
            if (set_default && (m_port_implicit == false && m_port.Length == 0))
            {
                m_port_list = new int[1] { port };
            }

            if (m_port_implicit == false)
            {
                // Port must match against the one from the uri
                valid = false;
                foreach (int p in m_port_list)
                {
                    if (p == port)
                    {
                        valid = true;
                        break;
                    }
                }
                if (!valid)
                {
                    if (isThrow)
                    {
                        throw new CookieException(SR.Format(SR.net_cookie_attribute, PortAttributeName, m_port));
                    }
                    return false;
                }
            }
            return true;
        }

        //Very primitive test to make sure that the name does not have illegal characters
        // As per RFC 952 (relaxed on first char could be a digit and string can have '_')
        private static bool DomainCharsTest(string name)
        {
            if (name == null || name.Length == 0)
            {
                return false;
            }
            for (int i = 0; i < name.Length; ++i)
            {
                char ch = name[i];
                if (!(
                      (ch >= '0' && ch <= '9') ||
                      (ch == '.' || ch == '-') ||
                      (ch >= 'a' && ch <= 'z') ||
                      (ch >= 'A' && ch <= 'Z') ||
                      (ch == '_')
                    ))
                    return false;
            }
            return true;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Port
        {
            get
            {
                return m_port;
            }
            set
            {
                m_port_implicit = false;
                if ((value == null || value.Length == 0))
                {
                    //"Port" is present but has no value.
                    m_port = string.Empty;
                }
                else
                {
                    // Parse port list
                    if (value[0] != '\"' || value[value.Length - 1] != '\"')
                    {
                        throw new CookieException(SR.Format(SR.net_cookie_attribute, PortAttributeName, value));
                    }
                    string[] ports = value.Split(PortSplitDelimiters);

                    List<int> portList = new List<int>();
                    int port;

                    for (int i = 0; i < ports.Length; ++i)
                    {
                        // Skip spaces
                        if (ports[i] != string.Empty)
                        {
                            if (!int.TryParse(ports[i], out port))
                                throw new CookieException(SR.Format(SR.net_cookie_attribute, PortAttributeName, value));
                            // valid values for port 0 - 0xFFFF
                            if ((port < 0) || (port > 0xFFFF))
                                throw new CookieException(SR.Format(SR.net_cookie_attribute, PortAttributeName, value));
                            portList.Add(port);
                        }
                    }
                    m_port_list = portList.ToArray();
                    m_port = value;
                    m_version = MaxSupportedVersion;
                    m_cookieVariant = CookieVariant.Rfc2965;
                }
            }
        }


        internal int[] PortList
        {
            get
            {
                //It must be null if Port Attribute was omitted in the response
                return m_port_list;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
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

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public DateTime TimeStamp
        {
            get
            {
                return m_timeStamp;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Value
        {
            get
            {
                return m_value;
            }
            set
            {
                m_value = (value == null ? string.Empty : value);
            }
        }

        internal CookieVariant Variant
        {
            get
            {
                return m_cookieVariant;
            }
            set
            {
                // only set by HttpListenerRequest::Cookies_get()
#if !uap
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"value:{value}");
#endif
                m_cookieVariant = value;
            }
        }

        // m_domainKey member is set internally in VerifySetDefaults()
        // If it is not set then verification function was not called
        // and this should never happen
        internal string DomainKey
        {
            get
            {
                return m_domain_implicit ? Domain : m_domainKey;
            }
        }


        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Version
        {
            get
            {
                return m_version;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                m_version = value;
                if (value > 0 && m_cookieVariant < CookieVariant.Rfc2109)
                {
                    m_cookieVariant = CookieVariant.Rfc2109;
                }
            }
        }

        // methods


        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override bool Equals(object comparand)
        {
            if (!(comparand is Cookie))
            {
                return false;
            }

            Cookie other = (Cookie)comparand;

            return (string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase))
                    && (string.Equals(Value, other.Value, StringComparison.Ordinal))
                    && (string.Equals(Path, other.Path, StringComparison.Ordinal))
                    && (string.Equals(Domain, other.Domain, StringComparison.OrdinalIgnoreCase))
                    && (Version == other.Version)
                    ;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override int GetHashCode()
        {
            //
            //string hashString = Name + "=" + Value + ";" + Path + "; " + Domain + "; " + Version;
            //int hash = 0;
            //
            //foreach (char ch in hashString) {
            //    hash = unchecked(hash << 1 ^ (int)ch);
            //}
            //return hash;
            return (Name + "=" + Value + ";" + Path + "; " + Domain + "; " + Version).GetHashCode();
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override string ToString()
        {
            var sb = new StringBuilder();
            ToString(sb);
            return sb.ToString();
        }

        internal void ToString(StringBuilder sb)
        {
            int beforeLength = sb.Length;

            // Add the Cookie version if necessary.
            if (Version != 0)
            {
                sb.Append(SpecialAttributeLiteral + VersionAttributeName + EqualsLiteral); // const strings
                if (IsQuotedVersion) sb.Append('"');
                sb.Append(m_version.ToString(NumberFormatInfo.InvariantInfo));
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
                    sb.Append(SeparatorLiteral + SpecialAttributeLiteral + PathAttributeName + EqualsLiteral); // const strings
                    sb.Append(m_path);
                }

                // Add the Domain if necessary.
                if (!m_domain_implicit && m_domain.Length > 0)
                {
                    sb.Append(SeparatorLiteral + SpecialAttributeLiteral + DomainAttributeName + EqualsLiteral); // const strings
                    if (IsQuotedDomain) sb.Append('"');
                    sb.Append(m_domain);
                    if (IsQuotedDomain) sb.Append('"');
                }
            }

            // Add the Port if necessary.
            if (!m_port_implicit)
            {
                sb.Append(SeparatorLiteral + SpecialAttributeLiteral + PortAttributeName); // const strings
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

        internal string ToServerString()
        {
            string result = Name + EqualsLiteral + Value;
            if (m_comment != null && m_comment.Length > 0)
            {
                result += SeparatorLiteral + CommentAttributeName + EqualsLiteral + m_comment;
            }
            if (m_commentUri != null)
            {
                result += SeparatorLiteral + CommentUrlAttributeName + EqualsLiteral + QuotesLiteral + m_commentUri.ToString() + QuotesLiteral;
            }
            if (m_discard)
            {
                result += SeparatorLiteral + DiscardAttributeName;
            }
            if (!m_domain_implicit && m_domain != null && m_domain.Length > 0)
            {
                result += SeparatorLiteral + DomainAttributeName + EqualsLiteral + m_domain;
            }
            if (Expires != DateTime.MinValue)
            {
                int seconds = (int)(Expires.ToLocalTime() - DateTime.Now).TotalSeconds;
                if (seconds < 0)
                {
                    // This means that the cookie has already expired. Set Max-Age to 0
                    // so that the client will discard the cookie immediately
                    seconds = 0;
                }
                result += SeparatorLiteral + MaxAgeAttributeName + EqualsLiteral + seconds.ToString(NumberFormatInfo.InvariantInfo);
            }
            if (!m_path_implicit && m_path != null && m_path.Length > 0)
            {
                result += SeparatorLiteral + PathAttributeName + EqualsLiteral + m_path;
            }
            if (!Plain && !m_port_implicit && m_port != null && m_port.Length > 0)
            {
                // QuotesLiteral are included in m_port
                result += SeparatorLiteral + PortAttributeName + EqualsLiteral + m_port;
            }
            if (m_version > 0)
            {
                result += SeparatorLiteral + VersionAttributeName + EqualsLiteral + m_version.ToString(NumberFormatInfo.InvariantInfo);
            }
            return result == EqualsLiteral ? null : result;
        }

        //private void Validate() {
        //    if ((m_name == String.Empty) && (m_value == String.Empty)) {
        //        throw new CookieException();
        //    }
        //    if ((m_name != String.Empty) && (m_name[0] == '$')) {
        //        throw new CookieException();
        //    }
        //}

#if DEBUG
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal void Dump()
        {
#if !uap
            if (NetEventSource.IsEnabled)
                NetEventSource.Info(this, 
                              "Cookie: " + ToString() + "->\n"
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
#endif
        }
#endif
    }

    internal enum CookieToken
    {
        // state types

        Nothing,
        NameValuePair,  // X=Y
        Attribute,      // X
        EndToken,       // ';'
        EndCookie,      // ','
        End,            // EOLN
        Equals,

        // value types

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

    //
    // CookieTokenizer
    //
    //  Used to split a single or multi-cookie (header) string into individual
    //  tokens
    //

    internal class CookieTokenizer
    {
        // fields

        bool m_eofCookie;
        int m_index;
        int m_length;
        string m_name;
        bool m_quoted;
        int m_start;
        CookieToken m_token;
        int m_tokenLength;
        string m_tokenStream;
        string m_value;
        int m_cookieStartIndex;
        int m_cookieLength;

        // constructors

        internal CookieTokenizer(string tokenStream)
        {
            m_length = tokenStream.Length;
            m_tokenStream = tokenStream;
        }

        // properties

        internal bool EndOfCookie
        {
            get
            {
                return m_eofCookie;
            }
            set
            {
                m_eofCookie = value;
            }
        }

        internal bool Eof
        {
            get
            {
                return m_index >= m_length;
            }
        }

        internal string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                m_name = value;
            }
        }

        internal bool Quoted
        {
            get
            {
                return m_quoted;
            }
            set
            {
                m_quoted = value;
            }
        }

        internal CookieToken Token
        {
            get
            {
                return m_token;
            }
            set
            {
                m_token = value;
            }
        }

        internal string Value
        {
            get
            {
                return m_value;
            }
            set
            {
                m_value = value;
            }
        }

        // methods

        //
        // GetCookieString
        //
        //  Gets the full string of the cookie
        //
        
        internal string GetCookieString()
        {
            return m_tokenStream.Substring(m_cookieStartIndex, m_cookieLength).Trim();
        }

        //
        // Extract
        //
        //  extract the current token
        //

        internal string Extract()
        {
            string tokenString = string.Empty;

            if (m_tokenLength != 0)
            {
                tokenString = m_tokenStream.Substring(m_start, m_tokenLength);
                if (!Quoted)
                {
                    tokenString = tokenString.Trim();
                }
            }
            return tokenString;
        }

        //
        // FindNext
        //
        //  Find the start and length of the next token. The token is terminated
        //  by one of:
        //
        //      - end-of-line
        //      - end-of-cookie: unquoted comma separates multiple cookies
        //      - end-of-token: unquoted semi-colon
        //      - end-of-name: unquoted equals
        //
        // Inputs:
        //  <argument>  ignoreComma
        //      true if parsing doesn't stop at a comma. This is only true when
        //      we know we're parsing an original cookie that has an expires=
        //      attribute, because the format of the time/date used in expires
        //      is:
        //          Wdy, dd-mmm-yyyy HH:MM:SS GMT
        //
        //  <argument>  ignoreEquals
        //      true if parsing doesn't stop at an equals sign. The LHS of the
        //      first equals sign is an attribute name. The next token may
        //      include one or more equals signs. E.g.,
        //
        //          SESSIONID=ID=MSNx45&q=33
        //
        // Outputs:
        //  <member>    m_index
        //      incremented to the last position in m_tokenStream contained by
        //      the current token
        //
        //  <member>    m_start
        //      incremented to the start of the current token
        //
        //  <member>    m_tokenLength
        //      set to the length of the current token
        //
        // Assumes:
        //  Nothing
        //
        // Returns:
        //  type of CookieToken found:
        //
        //      End         - end of the cookie string
        //      EndCookie   - end of current cookie in (potentially) a
        //                    multi-cookie string
        //      EndToken    - end of name=value pair, or end of an attribute
        //      Equals      - end of name=
        //
        // Throws:
        //  Nothing
        //

        internal CookieToken FindNext(bool ignoreComma, bool ignoreEquals)
        {
            m_tokenLength = 0;
            m_start = m_index;
            while ((m_index < m_length) && char.IsWhiteSpace(m_tokenStream[m_index]))
            {
                ++m_index;
                ++m_start;
            }

            CookieToken token = CookieToken.End;
            int increment = 1;

            if (!Eof)
            {
                if (m_tokenStream[m_index] == '"')
                {
                    Quoted = true;
                    ++m_index;
                    bool quoteOn = false;
                    while (m_index < m_length)
                    {
                        char currChar = m_tokenStream[m_index];
                        if (!quoteOn && currChar == '"')
                            break;
                        if (quoteOn)
                            quoteOn = false;
                        else if (currChar == '\\')
                            quoteOn = true;
                        ++m_index;
                    }
                    if (m_index < m_length)
                    {
                        ++m_index;
                    }
                    m_tokenLength = m_index - m_start;
                    increment = 0;
                    // if we are here, reset ignoreComma
                    // In effect, we ignore everything after quoted string till next delimiter
                    ignoreComma = false;
                }
                while ((m_index < m_length)
                       && (m_tokenStream[m_index] != ';')
                       && (ignoreEquals || (m_tokenStream[m_index] != '='))
                       && (ignoreComma || (m_tokenStream[m_index] != ',')))
                {
                    // Fixing 2 things:
                    // 1) ignore day of week in cookie string
                    // 2) revert ignoreComma once meet it, so won't miss the next cookie)
                    if (m_tokenStream[m_index] == ',')
                    {
                        m_start = m_index + 1;
                        m_tokenLength = -1;
                        ignoreComma = false;
                    }
                    ++m_index;
                    m_tokenLength += increment;
                }
                if (!Eof)
                {
                    switch (m_tokenStream[m_index])
                    {
                        case ';':
                            token = CookieToken.EndToken;
                            break;

                        case '=':
                            token = CookieToken.Equals;
                            break;

                        default:
                            m_cookieLength = m_index - m_cookieStartIndex;
                            token = CookieToken.EndCookie;
                            break;
                    }
                    ++m_index;
                }
                
                if (Eof)
                {
                    m_cookieLength = m_index - m_cookieStartIndex;
                }
            }
            return token;
        }

        //
        // Next
        //
        //  Get the next cookie name/value or attribute
        //
        //  Cookies come in the following formats:
        //
        //      1. Version0
        //          Set-Cookie: [<name>][=][<value>]
        //                      [; expires=<date>]
        //                      [; path=<path>]
        //                      [; domain=<domain>]
        //                      [; secure]
        //          Cookie: <name>=<value>
        //
        //          Notes: <name> and/or <value> may be blank
        //                 <date> is the RFC 822/1123 date format that
        //                 incorporates commas, e.g.
        //                 "Wednesday, 09-Nov-99 23:12:40 GMT"
        //
        //      2. RFC 2109
        //          Set-Cookie: 1#{
        //                          <name>=<value>
        //                          [; comment=<comment>]
        //                          [; domain=<domain>]
        //                          [; max-age=<seconds>]
        //                          [; path=<path>]
        //                          [; secure]
        //                          ; Version=<version>
        //                      }
        //          Cookie: $Version=<version>
        //                  1#{
        //                      ; <name>=<value>
        //                      [; path=<path>]
        //                      [; domain=<domain>]
        //                  }
        //
        //      3. RFC 2965
        //          Set-Cookie2: 1#{
        //                          <name>=<value>
        //                          [; comment=<comment>]
        //                          [; commentURL=<comment>]
        //                          [; discard]
        //                          [; domain=<domain>]
        //                          [; max-age=<seconds>]
        //                          [; path=<path>]
        //                          [; ports=<portlist>]
        //                          [; secure]
        //                          ; Version=<version>
        //                       }
        //          Cookie: $Version=<version>
        //                  1#{
        //                      ; <name>=<value>
        //                      [; path=<path>]
        //                      [; domain=<domain>]
        //                      [; port="<port>"]
        //                  }
        //          [Cookie2: $Version=<version>]
        //
        // Inputs:
        //  <argument>  first
        //      true if this is the first name/attribute that we have looked for
        //      in the cookie stream
        //
        // Outputs:
        //
        // Assumes:
        //  Nothing
        //
        // Returns:
        //  type of CookieToken found:
        //
        //      - Attribute
        //          - token was single-value. May be empty. Caller should check
        //            Eof or EndCookie to determine if any more action needs to
        //            be taken
        //
        //      - NameValuePair
        //          - Name and Value are meaningful. Either may be empty
        //
        // Throws:
        //  Nothing
        //

        internal CookieToken Next(bool first, bool parseResponseCookies)
        {
            Reset();

            if (first)
            {
                m_cookieStartIndex = m_index;
                m_cookieLength = 0;
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

        //
        // Reset
        //
        //  set up this tokenizer for finding the next name/value pair or
        //  attribute, or end-of-[token, cookie, or line]
        //

        internal void Reset()
        {
            m_eofCookie = false;
            m_name = string.Empty;
            m_quoted = false;
            m_start = m_index;
            m_token = CookieToken.Nothing;
            m_tokenLength = 0;
            m_value = string.Empty;
        }

        private struct RecognizedAttribute
        {
            string m_name;
            CookieToken m_token;

            internal RecognizedAttribute(string name, CookieToken token)
            {
                m_name = name;
                m_token = token;
            }

            internal CookieToken Token
            {
                get
                {
                    return m_token;
                }
            }

            internal bool IsEqualTo(string value)
            {
                return string.Equals(m_name, value, StringComparison.OrdinalIgnoreCase);
            }
        }

        //
        // recognized attributes in order of expected commonality
        //

        static RecognizedAttribute[] RecognizedAttributes = {
            new RecognizedAttribute(Cookie.PathAttributeName, CookieToken.Path),
            new RecognizedAttribute(Cookie.MaxAgeAttributeName, CookieToken.MaxAge),
            new RecognizedAttribute(Cookie.ExpiresAttributeName, CookieToken.Expires),
            new RecognizedAttribute(Cookie.VersionAttributeName, CookieToken.Version),
            new RecognizedAttribute(Cookie.DomainAttributeName, CookieToken.Domain),
            new RecognizedAttribute(Cookie.SecureAttributeName, CookieToken.Secure),
            new RecognizedAttribute(Cookie.DiscardAttributeName, CookieToken.Discard),
            new RecognizedAttribute(Cookie.PortAttributeName, CookieToken.Port),
            new RecognizedAttribute(Cookie.CommentAttributeName, CookieToken.Comment),
            new RecognizedAttribute(Cookie.CommentUrlAttributeName, CookieToken.CommentUrl),
            new RecognizedAttribute(Cookie.HttpOnlyAttributeName, CookieToken.HttpOnly),
        };

        static RecognizedAttribute[] RecognizedServerAttributes = {
            new RecognizedAttribute('$' + Cookie.PathAttributeName, CookieToken.Path),
            new RecognizedAttribute('$' + Cookie.VersionAttributeName, CookieToken.Version),
            new RecognizedAttribute('$' + Cookie.DomainAttributeName, CookieToken.Domain),
            new RecognizedAttribute('$' + Cookie.PortAttributeName, CookieToken.Port),
            new RecognizedAttribute('$' + Cookie.HttpOnlyAttributeName, CookieToken.HttpOnly),
        };

        internal CookieToken TokenFromName(bool parseResponseCookies)
        {
            if (!parseResponseCookies)
            {
                for (int i = 0; i < RecognizedServerAttributes.Length; ++i)
                {
                    if (RecognizedServerAttributes[i].IsEqualTo(Name))
                    {
                        return RecognizedServerAttributes[i].Token;
                    }
                }
            }
            else
            {
                for (int i = 0; i < RecognizedAttributes.Length; ++i)
                {
                    if (RecognizedAttributes[i].IsEqualTo(Name))
                    {
                        return RecognizedAttributes[i].Token;
                    }
                }
            }
            return CookieToken.Unknown;
        }
    }

    //
    // CookieParser
    //
    //  Takes a cookie header, makes cookies
    //

    internal class CookieParser
    {
        // fields

        CookieTokenizer m_tokenizer;
        Cookie m_savedCookie;

        // constructors

        internal CookieParser(string cookieString)
        {
            m_tokenizer = new CookieTokenizer(cookieString);
        }

        // properties

        // methods

        //  Gets the next cookie string
        //
        // Inputs:
        //  Nothing
        //
        // Outputs:
        //  Nothing
        //
        // Assumes:
        //  Nothing
        //
        // Returns:
        //  The string for the cookie
        //
        // Throws:
        //  Nothing
        //

        internal string GetString() {
            bool first = true;

            if (m_tokenizer.Eof)
            {
                return null;
            }

            do {
                m_tokenizer.Next(first, true);
                first = false;
            } while (!m_tokenizer.Eof && !m_tokenizer.EndOfCookie);

            return m_tokenizer.GetCookieString();
        }

        //
        // Get
        //
        //  Gets the next cookie
        //
        // Inputs:
        //  Nothing
        //
        // Outputs:
        //  Nothing
        //
        // Assumes:
        //  Nothing
        //
        // Returns:
        //  new cookie object, or null if there's no more
        //
        // Throws:
        //  Nothing
        //

        internal Cookie Get()
        {
            Cookie cookie = null;

            // only first occurrence of an attribute value must be counted
            bool commentSet = false;
            bool commentUriSet = false;
            bool domainSet = false;
            bool expiresSet = false;
            bool pathSet = false;
            bool portSet = false; //special case as it may have no value in header
            bool versionSet = false;
            bool secureSet = false;
            bool discardSet = false;

            do
            {
                CookieToken token = m_tokenizer.Next(cookie == null, true);
                if (cookie == null && (token == CookieToken.NameValuePair || token == CookieToken.Attribute))
                {
                    cookie = new Cookie();
                    if (cookie.InternalSetName(m_tokenizer.Name) == false)
                    {
                        //will be rejected
                        cookie.InternalSetName(string.Empty);
                    }
                    cookie.Value = m_tokenizer.Value;
                }
                else
                {
                    switch (token)
                    {
                        case CookieToken.NameValuePair:
                            switch (m_tokenizer.Token)
                            {
                                case CookieToken.Comment:
                                    if (!commentSet)
                                    {
                                        commentSet = true;
                                        cookie.Comment = m_tokenizer.Value;
                                    }
                                    break;

                                case CookieToken.CommentUrl:
                                    if (!commentUriSet)
                                    {
                                        commentUriSet = true;
                                        Uri parsed;
                                        if (Uri.TryCreate(CheckQuoted(m_tokenizer.Value), UriKind.Absolute, out parsed))
                                        {
                                            cookie.CommentUri = parsed;
                                        }
                                    }
                                    break;

                                case CookieToken.Domain:
                                    if (!domainSet)
                                    {
                                        domainSet = true;
                                        cookie.Domain = CheckQuoted(m_tokenizer.Value);
                                        cookie.IsQuotedDomain = m_tokenizer.Quoted;
                                    }
                                    break;

                                case CookieToken.Expires:
                                    if (!expiresSet)
                                    {
                                        expiresSet = true;

                                        DateTime expires;
                                        if (DateTime.TryParse(CheckQuoted(m_tokenizer.Value),
                                            CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out expires))
                                        {
                                            cookie.Expires = expires;
                                        }
                                        else
                                        {
                                            //this cookie will be rejected
                                            cookie.InternalSetName(string.Empty);
                                        }
                                    }
                                    break;

                                case CookieToken.MaxAge:
                                    if (!expiresSet)
                                    {
                                        expiresSet = true;
                                        int parsed;
                                        if (int.TryParse(CheckQuoted(m_tokenizer.Value), out parsed))
                                        {
                                            cookie.Expires = DateTime.Now.AddSeconds((double)parsed);
                                        }
                                        else
                                        {
                                            //this cookie will be rejected
                                            cookie.InternalSetName(string.Empty);
                                        }
                                    }
                                    break;

                                case CookieToken.Path:
                                    if (!pathSet)
                                    {
                                        pathSet = true;
                                        cookie.Path = m_tokenizer.Value;
                                    }
                                    break;

                                case CookieToken.Port:
                                    if (!portSet)
                                    {
                                        portSet = true;
                                        try
                                        {
                                            cookie.Port = m_tokenizer.Value;
                                        }
                                        catch
                                        {
                                            //this cookie will be rejected
                                            cookie.InternalSetName(string.Empty);
                                        }
                                    }
                                    break;

                                case CookieToken.Version:
                                    if (!versionSet)
                                    {
                                        versionSet = true;
                                        int parsed;
                                        if (int.TryParse(CheckQuoted(m_tokenizer.Value), out parsed))
                                        {
                                            cookie.Version = parsed;
                                            cookie.IsQuotedVersion = m_tokenizer.Quoted;
                                        }
                                        else
                                        {
                                            //this cookie will be rejected
                                            cookie.InternalSetName(string.Empty);
                                        }
                                    }
                                    break;
                            }
                            break;

                        case CookieToken.Attribute:
                            switch (m_tokenizer.Token)
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
            } while (!m_tokenizer.Eof && !m_tokenizer.EndOfCookie);
            return cookie;
        }

        // twin parsing method, different enough that it's better to split it into
        // a different method
        internal Cookie GetServer()
        {
            Cookie cookie = m_savedCookie;
            m_savedCookie = null;

            // only first occurrence of an attribute value must be counted
            bool domainSet = false;
            bool pathSet = false;
            bool portSet = false; //special case as it may have no value in header

            do
            {
                bool first = cookie == null || cookie.Name == null || cookie.Name.Length == 0;
                CookieToken token = m_tokenizer.Next(first, false);

                if (first && (token == CookieToken.NameValuePair || token == CookieToken.Attribute))
                {
                    if (cookie == null)
                    {
                        cookie = new Cookie();
                    }
                    if (cookie.InternalSetName(m_tokenizer.Name) == false)
                    {
                        //will be rejected
                        cookie.InternalSetName(string.Empty);
                    }
                    cookie.Value = m_tokenizer.Value;
                }
                else
                {
                    switch (token)
                    {
                        case CookieToken.NameValuePair:
                            switch (m_tokenizer.Token)
                            {
                                case CookieToken.Domain:
                                    if (!domainSet)
                                    {
                                        domainSet = true;
                                        cookie.Domain = CheckQuoted(m_tokenizer.Value);
                                        cookie.IsQuotedDomain = m_tokenizer.Quoted;
                                    }
                                    break;

                                case CookieToken.Path:
                                    if (!pathSet)
                                    {
                                        pathSet = true;
                                        cookie.Path = m_tokenizer.Value;
                                    }
                                    break;

                                case CookieToken.Port:
                                    if (!portSet)
                                    {
                                        portSet = true;
                                        try
                                        {
                                            cookie.Port = m_tokenizer.Value;
                                        }
                                        catch (CookieException)
                                        {
                                            //this cookie will be rejected
                                            cookie.InternalSetName(string.Empty);
                                        }
                                    }
                                    break;

                                case CookieToken.Version:
                                    // this is a new cookie, this token is for the next cookie.
                                    m_savedCookie = new Cookie();
                                    int parsed;
                                    if (int.TryParse(m_tokenizer.Value, out parsed))
                                    {
                                        m_savedCookie.Version = parsed;
                                    }
                                    return cookie;

                                case CookieToken.Unknown:
                                    // this is a new cookie, the token is for the next cookie.
                                    m_savedCookie = new Cookie();
                                    if (m_savedCookie.InternalSetName(m_tokenizer.Name) == false)
                                    {
                                        //will be rejected
                                        m_savedCookie.InternalSetName(string.Empty);
                                    }
                                    m_savedCookie.Value = m_tokenizer.Value;
                                    return cookie;
                            }
                            break;

                        case CookieToken.Attribute:
                            switch (m_tokenizer.Token)
                            {
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
            } while (!m_tokenizer.Eof && !m_tokenizer.EndOfCookie);
            return cookie;
        }

        internal static string CheckQuoted(string value)
        {
            if (value.Length < 2 || value[0] != '\"' || value[value.Length - 1] != '\"')
                return value;

            return value.Length == 2 ? string.Empty : value.Substring(1, value.Length - 2);
        }
    }
}

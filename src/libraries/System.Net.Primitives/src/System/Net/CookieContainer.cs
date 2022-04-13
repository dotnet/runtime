// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;

// Relevant cookie specs:
//
// PERSISTENT CLIENT STATE HTTP COOKIES (1996)
// From <http:// web.archive.org/web/20020803110822/http://wp.netscape.com/newsref/std/cookie_spec.html>
//
// RFC2109 HTTP State Management Mechanism (February 1997)
// From <http:// tools.ietf.org/html/rfc2109>
//
// RFC2965 HTTP State Management Mechanism (October 2000)
// From <http:// tools.ietf.org/html/rfc2965>
//
// RFC6265 HTTP State Management Mechanism (April 2011)
// From <http:// tools.ietf.org/html/rfc6265>
//
// The Version attribute of the cookie header is defined and used only in RFC2109 and RFC2965 cookie
// specs and specifies Version=1. The Version attribute is not used in the  Netscape cookie spec
// (considered as Version=0). Nor is it used in the most recent cookie spec, RFC6265, introduced in 2011.
// RFC6265 deprecates all previous cookie specs including the Version attribute.
//
// Cookies without an explicit Domain attribute will only match a potential uri that matches the original
// uri from where the cookie came from.
//
// For explicit Domain attribute in the cookie, the following rules apply:
//
// Version=0 (Netscape, RFC6265) allows the Domain attribute of the cookie to match any tail substring
// of the host uri.
//
// Version=1 related cookie specs only allows the Domain attribute to match the host uri based on a
// more restricted set of rules.
//
// According to RFC2109/RFC2965, the cookie will be rejected for matching if:
// * The value for the Domain attribute contains no embedded dots or does not start with a dot.
// * The value for the request-host does not domain-match the Domain attribute.
// " The request-host is a FQDN (not IP address) and has the form HD, where D is the value of the Domain
//  attribute, and H is a string that contains one or more dots.
//
// Examples:
// * A cookie from request-host y.x.foo.com for Domain=.foo.com would be rejected, because H is y.x
//  and contains a dot.
//
// * A cookie from request-host x.foo.com for Domain=.foo.com would be accepted.
//
// * A cookie with Domain=.com or Domain=.com., will always be rejected, because there is no embedded dot.
//
// * A cookie with Domain=ajax.com will be rejected because the value for Domain does not begin with a dot.

namespace System.Net
{
    internal readonly struct HeaderVariantInfo
    {
        private readonly string _name;
        private readonly CookieVariant _variant;

        internal HeaderVariantInfo(string name, CookieVariant variant)
        {
            _name = name;
            _variant = variant;
        }

        internal string Name
        {
            get
            {
                return _name;
            }
        }

        internal CookieVariant Variant
        {
            get
            {
                return _variant;
            }
        }
    }

    // CookieContainer
    //
    // Manage cookies for a user (implicit). Based on RFC 2965.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class CookieContainer
    {
        public const int DefaultCookieLimit = 300;
        public const int DefaultPerDomainCookieLimit = 20;
        public const int DefaultCookieLengthLimit = 4096;

        private static readonly string s_fqdnMyDomain = CreateFqdnMyDomain();
        private static readonly HeaderVariantInfo[] s_headerInfo = {
            new HeaderVariantInfo(HttpKnownHeaderNames.SetCookie,  CookieVariant.Rfc2109),
            new HeaderVariantInfo(HttpKnownHeaderNames.SetCookie2, CookieVariant.Rfc2965)
        };

        private readonly Hashtable m_domainTable = new Hashtable(); // Do not rename (binary serialization)
        private int m_maxCookieSize = DefaultCookieLengthLimit; // Do not rename (binary serialization)
        private int m_maxCookies = DefaultCookieLimit; // Do not rename (binary serialization)
        private int m_maxCookiesPerDomain = DefaultPerDomainCookieLimit; // Do not rename (binary serialization)
        private int m_count; // Do not rename (binary serialization)
        private readonly string m_fqdnMyDomain = s_fqdnMyDomain; // Do not rename (binary serialization)

        public CookieContainer()
        {
        }

        public CookieContainer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentException(SR.net_toosmall, nameof(capacity));
            }
            m_maxCookies = capacity;
        }

        public CookieContainer(int capacity, int perDomainCapacity, int maxCookieSize) : this(capacity)
        {
            if (perDomainCapacity != int.MaxValue && (perDomainCapacity <= 0 || perDomainCapacity > capacity))
            {
                throw new ArgumentOutOfRangeException(nameof(perDomainCapacity), SR.Format(SR.net_cookie_capacity_range, "PerDomainCapacity", 0, capacity));
            }
            m_maxCookiesPerDomain = perDomainCapacity;
            if (maxCookieSize <= 0)
            {
                throw new ArgumentException(SR.net_toosmall, nameof(maxCookieSize));
            }
            m_maxCookieSize = maxCookieSize;
        }

        private static string CreateFqdnMyDomain()
        {
            string domain = HostInformation.DomainName;
            return domain != null && domain.Length > 1 ?
                '.' + domain :
                string.Empty;
        }

        // NOTE: after shrinking the capacity, Count can become greater than Capacity.
        public int Capacity
        {
            get
            {
                return m_maxCookies;
            }
            set
            {
                if (value <= 0 || (value < m_maxCookiesPerDomain && m_maxCookiesPerDomain != int.MaxValue))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Format(SR.net_cookie_capacity_range, "Capacity", 0, m_maxCookiesPerDomain));
                }
                if (value < m_maxCookies)
                {
                    m_maxCookies = value;
                    AgeCookies(null);
                }
                m_maxCookies = value;
            }
        }

        /// <devdoc>
        ///   <para>Returns the total number of cookies in the container.</para>
        /// </devdoc>
        public int Count
        {
            get
            {
                return m_count;
            }
        }

        public int MaxCookieSize
        {
            get
            {
                return m_maxCookieSize;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                m_maxCookieSize = value;
            }
        }

        /// <devdoc>
        ///   <para>After shrinking domain capacity, each domain will less hold than new domain capacity.</para>
        /// </devdoc>
        public int PerDomainCapacity
        {
            get
            {
                return m_maxCookiesPerDomain;
            }
            set
            {
                if (value <= 0 || (value > m_maxCookies && value != int.MaxValue))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                if (value < m_maxCookiesPerDomain)
                {
                    m_maxCookiesPerDomain = value;
                    AgeCookies(null);
                }
                m_maxCookiesPerDomain = value;
            }
        }

        // This method will construct a faked URI: the Domain property is required for param.
        public void Add(Cookie cookie!!)
        {
            if (cookie.Domain.Length == 0)
            {
                throw new ArgumentException(
                    SR.Format(SR.net_emptystringcall, nameof(cookie) + "." + nameof(cookie.Domain)),
                    nameof(cookie));
            }

            Uri? uri;
            var uriSb = new StringBuilder();

            // We cannot add an invalid cookie into the container.
            // Trying to prepare Uri for the cookie verification.
            uriSb.Append(cookie.Secure ? UriScheme.Https : UriScheme.Http).Append(UriScheme.SchemeDelimiter);

            // If the original cookie has an explicitly set domain, copy it over to the new cookie.
            if (!cookie.DomainImplicit)
            {
                if (cookie.Domain[0] == '.')
                {
                    uriSb.Append('0'); // URI cctor should consume this faked host.
                }
            }
            uriSb.Append(cookie.Domain);


            // Either keep Port as implicit or set it according to original cookie.
            if (cookie.PortList != null)
            {
                uriSb.Append(':').Append(cookie.PortList[0]);
            }

            // Path must be present, set to root by default.
            uriSb.Append(cookie.Path);

            if (!Uri.TryCreate(uriSb.ToString(), UriKind.Absolute, out uri))
                throw new CookieException(SR.Format(SR.net_cookie_attribute, "Domain", cookie.Domain));

            // We don't know cookie verification status, so re-create the cookie and verify it.
            Cookie new_cookie = cookie.Clone();
            new_cookie.VerifySetDefaults(new_cookie.Variant, uri, IsLocalDomain(uri.Host), m_fqdnMyDomain, true, true);

            Add(new_cookie, true);
        }

        // This method is called *only* when cookie verification is done, so unlike with public
        // Add(Cookie cookie) the cookie is in a reasonable condition.
        internal void Add(Cookie cookie, bool throwOnError)
        {
            PathList? pathList;

            if (cookie.Value.Length > m_maxCookieSize)
            {
                if (throwOnError)
                {
                    throw new CookieException(SR.Format(SR.net_cookie_size, cookie, m_maxCookieSize));
                }
                return;
            }

            try
            {
                lock (m_domainTable.SyncRoot)
                {
                    pathList = (PathList?)m_domainTable[cookie.DomainKey];
                    if (pathList == null)
                    {
                        m_domainTable[cookie.DomainKey] = (pathList = new PathList());
                    }
                }
                int domain_count = pathList.GetCookiesCount();

                CookieCollection? cookies;
                lock (pathList.SyncRoot)
                {
                    cookies = (CookieCollection?)pathList[cookie.Path]!;

                    if (cookies == null)
                    {
                        cookies = new CookieCollection();
                        pathList[cookie.Path] = cookies;
                    }
                }

                if (cookie.Expired)
                {
                    // Explicit removal command (Max-Age == 0)
                    lock (cookies)
                    {
                        int idx = cookies.IndexOf(cookie);
                        if (idx != -1)
                        {
                            cookies.RemoveAt(idx);
                            --m_count;
                        }
                    }
                }
                else
                {
                    // This is about real cookie adding, check Capacity first
                    if (domain_count >= m_maxCookiesPerDomain && !AgeCookies(cookie.DomainKey))
                    {
                        return; // Cannot age: reject new cookie
                    }
                    else if (m_count >= m_maxCookies && !AgeCookies(null))
                    {
                        return; // Cannot age: reject new cookie
                    }

                    // About to change the collection.
                    lock (cookies)
                    {
                        m_count += cookies.InternalAdd(cookie, true);
                    }
                }

                // We don't want to cleanup m_domaintable/m_list too often. Add check to avoid overhead.
                if (m_domainTable.Count > m_count || pathList.Count > m_maxCookiesPerDomain)
                {
                    DomainTableCleanup();
                }
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception e)
            {
                if (throwOnError)
                {
                    throw new CookieException(SR.net_container_add_cookie, e);
                }
            }
        }

        // This function, when called, must delete at least one cookie.
        // If there are expired cookies in given scope they are cleaned up.
        // If nothing is found the least used Collection will be found and removed
        // from the container.
        //
        // Also note that expired cookies are also removed during request preparation
        // (this.GetCookies method).
        //
        // Param. 'domain' == null means to age in the whole container.
        private bool AgeCookies(string? domain)
        {
            Debug.Assert(m_maxCookies != 0);
            Debug.Assert(m_maxCookiesPerDomain != 0);

            int removed = 0;
            DateTime oldUsed = DateTime.MaxValue;
            DateTime tempUsed;

            CookieCollection? lruCc = null;
            string? lruDomain;
            string tempDomain;

            PathList pathList;
            int domain_count;
            int itemp = 0;
            float remainingFraction = 1.0F;

            // The container was shrunk, might need additional cleanup for each domain
            if (m_count > m_maxCookies)
            {
                // Means the fraction of the container to be left.
                // Each domain will be cut accordingly.
                remainingFraction = (float)m_maxCookies / (float)m_count;
            }
            lock (m_domainTable.SyncRoot)
            {
                foreach (object item in m_domainTable)
                {
                    DictionaryEntry entry = (DictionaryEntry)item;
                    if (domain == null)
                    {
                        tempDomain = (string)entry.Key;
                        pathList = (PathList)entry.Value!; // Aliasing to trick foreach
                    }
                    else
                    {
                        tempDomain = domain;
                        pathList = (PathList)m_domainTable[domain]!;
                    }

                    domain_count = 0; // Cookies in the domain
                    lock (pathList.SyncRoot)
                    {
                        foreach (CookieCollection? cc in pathList.Values)
                        {
                            Debug.Assert(cc != null);
                            itemp = ExpireCollection(cc);
                            removed += itemp;
                            m_count -= itemp; // Update this container's count
                            domain_count += cc.Count;

                            // We also find the least used cookie collection in ENTIRE container.
                            // We count the collection as LRU only if it holds 1+ elements.
                            if (cc.Count > 0 && (tempUsed = cc.TimeStamp(CookieCollection.Stamp.Check)) < oldUsed)
                            {
                                lruDomain = tempDomain;
                                lruCc = cc;
                                oldUsed = tempUsed;
                            }
                        }
                    }

                    // Check if we have reduced to the limit of the domain by expiration only.
                    int min_count = Math.Min((int)(domain_count * remainingFraction), Math.Min(m_maxCookiesPerDomain, m_maxCookies) - 1);
                    if (domain_count > min_count)
                    {
                        // This case requires sorting all domain collections by timestamp.
                        CookieCollection[] cookies;
                        DateTime[] stamps;
                        lock (pathList.SyncRoot)
                        {
                            cookies = new CookieCollection[pathList.Count];
                            stamps = new DateTime[pathList.Count];
                            foreach (CookieCollection? cc in pathList.Values)
                            {
                                stamps[itemp] = cc!.TimeStamp(CookieCollection.Stamp.Check);
                                cookies[itemp] = cc;
                                ++itemp;
                            }
                        }
                        Array.Sort(stamps, cookies);

                        itemp = 0;
                        for (int i = 0; i < cookies.Length; ++i)
                        {
                            CookieCollection cc = cookies[i];

                            lock (cc)
                            {
                                while (domain_count > min_count && cc.Count > 0)
                                {
                                    cc.RemoveAt(0);
                                    --domain_count;
                                    --m_count;
                                    ++removed;
                                }
                            }
                            if (domain_count <= min_count)
                            {
                                break;
                            }
                        }

                        if (domain_count > min_count && domain != null)
                        {
                            // Cannot complete aging of explicit domain (no cookie adding allowed).
                            return false;
                        }
                    }
                }
            }

            // We have completed aging of the specified domain.
            if (domain != null)
            {
                return true;
            }

            // The rest is for entire container aging.
            // We must get at least one free slot.

            // Don't need to apply LRU if we already cleaned something.
            if (removed != 0)
            {
                return true;
            }

            if (oldUsed == DateTime.MaxValue)
            {
                // Something strange. Either capacity is 0 or all collections are locked with cc.Used.
                return false;
            }

            // Remove oldest cookies from the least used collection.
            lock (lruCc!)
            {
                while (m_count >= m_maxCookies && lruCc.Count > 0)
                {
                    lruCc.RemoveAt(0);
                    --m_count;
                }
            }
            return true;
        }

        private void DomainTableCleanup()
        {
            var removePathList = new List<object>();
            var removeDomainList = new List<string>();

            string currentDomain;
            PathList pathList;

            lock (m_domainTable.SyncRoot)
            {
                // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                IDictionaryEnumerator enumerator = m_domainTable.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    currentDomain = (string)enumerator.Key;
                    pathList = (PathList)enumerator.Value!;

                    lock (pathList.SyncRoot)
                    {
                        IDictionaryEnumerator e = pathList.GetEnumerator();
                        while (e.MoveNext())
                        {
                            CookieCollection cc = (CookieCollection)e.Value!;
                            if (cc.Count == 0)
                            {
                                removePathList.Add(e.Key);
                            }
                        }

                        foreach (var key in removePathList)
                        {
                            pathList.Remove(key);
                        }

                        removePathList.Clear();
                        if (pathList.Count == 0) removeDomainList.Add(currentDomain);
                    }
                }

                foreach (var key in removeDomainList)
                {
                    m_domainTable.Remove(key);
                }
            }
        }

        // Return number of cookies removed from the collection.
        private static int ExpireCollection(CookieCollection cc)
        {
            lock (cc)
            {
                int oldCount = cc.Count;
                int idx = oldCount - 1;

                // Cannot use enumerator as we are going to alter collection.
                while (idx >= 0)
                {
                    Cookie cookie = cc[idx];
                    if (cookie.Expired)
                    {
                        cc.RemoveAt(idx);
                    }
                    --idx;
                }
                return oldCount - cc.Count;
            }
        }

        public void Add(CookieCollection cookies!!)
        {
            foreach (Cookie c in (ICollection<Cookie>)cookies)
            {
                Add(c);
            }
        }

        // This will try (if needed) get the full domain name of the host given the Uri.
        // NEVER call this function from internal methods with 'fqdnRemote' == null.
        // Since this method counts security issue for DNS and hence will slow
        // the performance.
        internal bool IsLocalDomain(string host)
        {
            int dot = host.IndexOf('.');
            if (dot == -1)
            {
                // No choice but to treat it as a host on the local domain.
                // This also covers 'localhost' and 'loopback'.
                return true;
            }

            // Quick test for typical cases: loopback addresses for IPv4 and IPv6.
            if ((host == "127.0.0.1") || (host == "::1") || (host == "0:0:0:0:0:0:0:1"))
            {
                return true;
            }

            // Test domain membership.
            if (string.Compare(m_fqdnMyDomain, 0, host, dot, m_fqdnMyDomain.Length, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }

            // Test for "127.###.###.###" without using regex.
            string[] ipParts = host.Split('.');
            if (ipParts != null && ipParts.Length == 4 && ipParts[0] == "127")
            {
                int i;
                for (i = 1; i < ipParts.Length; i++)
                {
                    string part = ipParts[i];
                    switch (part.Length)
                    {
                        case 3:
                            if (part[2] < '0' || part[2] > '9')
                            {
                                break;
                            }
                            goto case 2;

                        case 2:
                            if (part[1] < '0' || part[1] > '9')
                            {
                                break;
                            }
                            goto case 1;

                        case 1:
                            if (part[0] < '0' || part[0] > '9')
                            {
                                break;
                            }
                            continue;
                    }
                    break;
                }
                if (i == 4)
                {
                    return true;
                }
            }

            return false;
        }

        public void Add(Uri uri!!, Cookie cookie!!)
        {

            Cookie new_cookie = cookie.Clone();
            new_cookie.VerifySetDefaults(new_cookie.Variant, uri, IsLocalDomain(uri.Host), m_fqdnMyDomain, true, true);

            Add(new_cookie, true);
        }

        public void Add(Uri uri!!, CookieCollection cookies!!)
        {
            bool isLocalDomain = IsLocalDomain(uri.Host);
            foreach (Cookie c in cookies)
            {
                Cookie new_cookie = c.Clone();
                new_cookie.VerifySetDefaults(new_cookie.Variant, uri, isLocalDomain, m_fqdnMyDomain, true, true);
                Add(new_cookie, true);
            }
        }

        internal CookieCollection CookieCutter(Uri uri, string? headerName, string setCookieHeader, bool isThrow)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"uri:{uri} headerName:{headerName} setCookieHeader:{setCookieHeader} isThrow:{isThrow}");

            CookieCollection cookies = new CookieCollection();
            CookieVariant variant = CookieVariant.Unknown;
            if (headerName == null)
            {
                variant = CookieVariant.Default;
            }
            else
            {
                for (int i = 0; i < s_headerInfo.Length; ++i)
                {
                    if ((string.Equals(headerName, s_headerInfo[i].Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        variant = s_headerInfo[i].Variant;
                    }
                }
            }

            bool isLocalDomain = IsLocalDomain(uri.Host);
            try
            {
                CookieParser parser = new CookieParser(setCookieHeader);
                do
                {
                    Cookie? cookie = parser.Get();
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"CookieParser returned cookie:{cookie}");

                    if (cookie == null)
                    {
                        if (parser.EndofHeader())
                        {
                            break;
                        }
                        continue;
                    }

                    // Parser marks invalid cookies this way
                    if (string.IsNullOrEmpty(cookie.Name))
                    {
                        if (isThrow)
                        {
                            throw new CookieException(SR.net_cookie_format);
                        }
                        // Otherwise, ignore (reject) cookie
                        continue;
                    }

                    // This will set the default values from the response URI
                    // AND will check for cookie validity
                    if (!cookie.VerifySetDefaults(variant, uri, isLocalDomain, m_fqdnMyDomain, true, isThrow))
                    {
                        continue;
                    }
                    // If many same cookies arrive we collapse them into just one, hence setting
                    // parameter isStrict = true below
                    cookies.InternalAdd(cookie, true);
                } while (true);
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception e)
            {
                if (isThrow)
                {
                    throw new CookieException(SR.Format(SR.net_cookie_parse_header, uri.AbsoluteUri), e);
                }
            }

            int cookiesCount = cookies.Count;
            for (int i = 0; i < cookiesCount; i++)
            {
                Add((Cookie)cookies[i], isThrow);
            }

            return cookies;
        }

        public CookieCollection GetCookies(Uri uri!!)
        {
            return InternalGetCookies(uri) ?? new CookieCollection();
        }

        /// <summary>Gets a <see cref="CookieCollection"/> that contains all of the <see cref="Cookie"/> instances in the container.</summary>
        /// <returns>A <see cref="CookieCollection"/> that contains all of the <see cref="Cookie"/> instances in the container.</returns>
        public CookieCollection GetAllCookies()
        {
            var result = new CookieCollection();

            lock (m_domainTable.SyncRoot)
            {
                IDictionaryEnumerator lists = m_domainTable.GetEnumerator();
                while (lists.MoveNext())
                {
                    PathList list = (PathList)lists.Value!;
                    lock (list.SyncRoot)
                    {
                        IDictionaryEnumerator collections = list.List.GetEnumerator();
                        while (collections.MoveNext())
                        {
                            result.Add((CookieCollection)collections.Value!);
                        }
                    }
                }
            }

            return result;
        }

        internal CookieCollection? InternalGetCookies(Uri uri)
        {
            if (m_count == 0)
            {
                return null;
            }

            bool isSecure = (uri.Scheme == UriScheme.Https || uri.Scheme == UriScheme.Wss);
            int port = uri.Port;
            CookieCollection? cookies = null;

            var domainAttributeMatchAnyCookieVariant = new System.Collections.Generic.List<string>();
            System.Collections.Generic.List<string>? domainAttributeMatchOnlyCookieVariantPlain = null;

            string fqdnRemote = uri.Host;

            // Add initial candidates to match Domain attribute of possible cookies.
            // For these Domains, cookie can have any CookieVariant enum value.
            domainAttributeMatchAnyCookieVariant.Add(fqdnRemote);
            domainAttributeMatchAnyCookieVariant.Add("." + fqdnRemote);

            int dot = fqdnRemote.IndexOf('.');
            if (dot == -1)
            {
                // DNS.resolve may return short names even for other inet domains ;-(
                // We _don't_ know what the exact domain is, so try also grab short hostname cookies.
                // Grab long name from the local domain
                if (m_fqdnMyDomain != null && m_fqdnMyDomain.Length != 0)
                {
                    domainAttributeMatchAnyCookieVariant.Add(fqdnRemote + m_fqdnMyDomain);
                    // Grab the local domain itself
                    domainAttributeMatchAnyCookieVariant.Add(m_fqdnMyDomain);
                }
            }
            else
            {
                // Grab the host domain
                domainAttributeMatchAnyCookieVariant.Add(fqdnRemote.Substring(dot));

                // The following block is only for compatibility with Version0 spec.
                // Still, we'll add only Plain-Variant cookies if found under below keys
                if (fqdnRemote.Length > 2)
                {
                    // We ignore the '.' at the end on the name
                    int last = fqdnRemote.LastIndexOf('.', fqdnRemote.Length - 2);
                    // AND keys with <2 dots inside.
                    if (last > 0)
                    {
                        last = fqdnRemote.LastIndexOf('.', last - 1);
                    }
                    if (last != -1)
                    {
                        while ((dot < last) && (dot = fqdnRemote.IndexOf('.', dot + 1)) != -1)
                        {
                            if (domainAttributeMatchOnlyCookieVariantPlain == null)
                            {
                                domainAttributeMatchOnlyCookieVariantPlain = new System.Collections.Generic.List<string>();
                            }

                            // These candidates can only match CookieVariant.Plain cookies.
                            domainAttributeMatchOnlyCookieVariantPlain.Add(fqdnRemote.Substring(dot));
                        }
                    }
                }
            }

            BuildCookieCollectionFromDomainMatches(uri, isSecure, port, ref cookies, domainAttributeMatchAnyCookieVariant, false);
            if (domainAttributeMatchOnlyCookieVariantPlain != null)
            {
                BuildCookieCollectionFromDomainMatches(uri, isSecure, port, ref cookies, domainAttributeMatchOnlyCookieVariantPlain, true);
            }

            return cookies;
        }

        private void BuildCookieCollectionFromDomainMatches(Uri uri, bool isSecure, int port, ref CookieCollection? cookies, System.Collections.Generic.List<string> domainAttribute, bool matchOnlyPlainCookie)
        {
            for (int i = 0; i < domainAttribute.Count; i++)
            {
                PathList pathList;
                lock (m_domainTable.SyncRoot)
                {
                    pathList = (PathList)m_domainTable[domainAttribute[i]]!;
                    if (pathList == null)
                    {
                        continue;
                    }
                }

                lock (pathList.SyncRoot)
                {
                    SortedList list = pathList.List;
                    int listCount = list.Count;
                    for (int e = 0; e < listCount; e++)
                    {
                        string path = (string)list.GetKey(e);
                        if (PathMatch(uri.AbsolutePath, path))
                        {
                            CookieCollection cc = (CookieCollection)list.GetByIndex(e)!;
                            cc.TimeStamp(CookieCollection.Stamp.Set);
                            MergeUpdateCollections(ref cookies, cc, port, isSecure, matchOnlyPlainCookie);
                        }
                    }
                }

                // Remove unused domain.
                if (pathList.Count == 0)
                {
                    lock (m_domainTable.SyncRoot)
                    {
                        m_domainTable.Remove(domainAttribute[i]);
                    }
                }
            }
        }

        // Implement path-matching according to https://tools.ietf.org/html/rfc6265#section-5.1.4:
        // | A request-path path-matches a given cookie-path if at least one of the following conditions holds:
        // | - The cookie-path and the request-path are identical.
        // | - The cookie-path is a prefix of the request-path, and the last character of the cookie-path is %x2F ("/").
        // | - The cookie-path is a prefix of the request-path, and the first character of the request-path that is not included in the cookie-path is a %x2F ("/") character.
        // The latter conditions are needed to make sure that
        // PathMatch("/fooBar, "/foo") == false
        // but:
        // PathMatch("/foo/bar", "/foo") == true, PathMatch("/foo/bar", "/foo/") == true
        private static bool PathMatch(string requestPath, string cookiePath)
        {
            cookiePath = CookieParser.CheckQuoted(cookiePath);

            if (!requestPath.StartsWith(cookiePath, StringComparison.Ordinal))
                return false;
            return requestPath.Length == cookiePath.Length ||
                   cookiePath.Length > 0 && cookiePath[^1] == '/' ||
                   requestPath[cookiePath.Length] == '/';
        }

        private void MergeUpdateCollections(ref CookieCollection? destination, CookieCollection source, int port, bool isSecure, bool isPlainOnly)
        {
            lock (source)
            {
                // Cannot use foreach as we are going to update 'source'
                for (int idx = 0; idx < source.Count; ++idx)
                {
                    bool to_add = false;

                    Cookie cookie = source[idx];

                    if (cookie.Expired)
                    {
                        // If expired, remove from container and don't add to the destination
                        source.RemoveAt(idx);
                        --m_count;
                        --idx;
                    }
                    else
                    {
                        // Add only if port does match to this request URI
                        // or was not present in the original response.
                        if (isPlainOnly && cookie.Variant != CookieVariant.Plain)
                        {
                            ; // Don't add
                        }
                        else if (cookie.PortList != null)
                        {
                            foreach (int p in cookie.PortList)
                            {
                                if (p == port)
                                {
                                    to_add = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // It was implicit Port, always OK to add.
                            to_add = true;
                        }

                        // Refuse to add a secure cookie into an 'unsecure' destination
                        if (cookie.Secure && !isSecure)
                        {
                            to_add = false;
                        }

                        if (to_add)
                        {
                            // In 'source' are already ordered.
                            // If two same cookies come from different 'source' then they
                            // will follow (not replace) each other.
                            if (destination == null)
                            {
                                destination = new CookieCollection();
                            }
                            destination.InternalAdd(cookie, false);
                        }
                    }
                }
            }
        }

        public string GetCookieHeader(Uri uri!!)
        {
            return GetCookieHeader(uri, out _);
        }

        internal string GetCookieHeader(Uri uri, out string optCookie2)
        {
            CookieCollection? cookies = InternalGetCookies(uri);
            if (cookies == null)
            {
                optCookie2 = string.Empty;
                return string.Empty;
            }

            string delimiter = string.Empty;

            StringBuilder builder = StringBuilderCache.Acquire();
            for (int i = 0; i < cookies.Count; i++)
            {
                builder.Append(delimiter);
                cookies[i].ToString(builder);

                delimiter = "; ";
            }

            optCookie2 = cookies.IsOtherVersionSeen ?
                          (Cookie.SpecialAttributeLiteral +
                           CookieFields.VersionAttributeName +
                           Cookie.EqualsLiteral +
                           Cookie.MaxSupportedVersionString) : string.Empty;

            return StringBuilderCache.GetStringAndRelease(builder);
        }

        public void SetCookies(Uri uri!!, string cookieHeader!!)
        {

            CookieCutter(uri, null, cookieHeader, true); // Will throw on error
        }
    }

    // PathList needs to be public in order to maintain binary serialization compatibility as the System shim
    // needs to have access to type-forward it.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class PathList
    {
        // Usage of PathList depends on it being shallowly immutable;
        // adding any mutable fields to it would result in breaks.
        private readonly SortedList m_list = SortedList.Synchronized(new SortedList(PathListComparer.StaticInstance)); // Do not rename (binary serialization)

        internal int Count => m_list.Count;

        internal int GetCookiesCount()
        {
            int count = 0;
            lock (SyncRoot)
            {
                IList list = m_list.GetValueList();
                int listCount = list.Count;
                for (int i = 0; i < listCount; i++)
                {
                    count += ((CookieCollection)list[i]!).Count;
                }
            }
            return count;
        }

        internal ICollection Values
        {
            get
            {
                return m_list.Values;
            }
        }

        internal object? this[string s]
        {
            get
            {
                lock (SyncRoot)
                {
                    return m_list[s];
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    Debug.Assert(value != null);
                    m_list[s] = value;
                }
            }
        }

        internal IDictionaryEnumerator GetEnumerator()
        {
            lock (SyncRoot)
            {
                return m_list.GetEnumerator();
            }
        }

        internal void Remove(object key)
        {
            lock (SyncRoot)
            {
                m_list.Remove(key);
            }
        }

        internal SortedList List => m_list;

        internal object SyncRoot => m_list.SyncRoot;

        [Serializable]
        [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
        private sealed class PathListComparer : IComparer
        {
            internal static readonly PathListComparer StaticInstance = new PathListComparer();

            int IComparer.Compare(object? ol, object? or)
            {
                string pathLeft = CookieParser.CheckQuoted((string)ol!);
                string pathRight = CookieParser.CheckQuoted((string)or!);
                int ll = pathLeft.Length;
                int lr = pathRight.Length;
                int length = Math.Min(ll, lr);

                for (int i = 0; i < length; ++i)
                {
                    if (pathLeft[i] != pathRight[i])
                    {
                        return pathLeft[i] - pathRight[i];
                    }
                }
                return lr - ll;
            }
        }
    }
}

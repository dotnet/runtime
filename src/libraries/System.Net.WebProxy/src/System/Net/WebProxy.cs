// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace System.Net
{
    public class WebProxy : IWebProxy, ISerializable
    {
        private ArrayList _bypassList;
        private Regex[] _regExBypassList;

        public WebProxy() : this((Uri)null, false, null, null) { }

        public WebProxy(Uri Address) : this(Address, false, null, null) { }

        public WebProxy(Uri Address, bool BypassOnLocal) : this(Address, BypassOnLocal, null, null) { }

        public WebProxy(Uri Address, bool BypassOnLocal, string[] BypassList) : this(Address, BypassOnLocal, BypassList, null) { }

        public WebProxy(Uri Address, bool BypassOnLocal, string[] BypassList, ICredentials Credentials)
        {
            this.Address = Address;
            this.Credentials = Credentials;
            this.BypassProxyOnLocal = BypassOnLocal;
            if (BypassList != null)
            {
                _bypassList = new ArrayList(BypassList);
                UpdateRegExList(true);
            }
        }

        public WebProxy(string Host, int Port)
            : this(new Uri("http://" + Host + ":" + Port.ToString(CultureInfo.InvariantCulture)), false, null, null)
        {
        }

        public WebProxy(string Address)
            : this(CreateProxyUri(Address), false, null, null)
        {
        }

        public WebProxy(string Address, bool BypassOnLocal)
            : this(CreateProxyUri(Address), BypassOnLocal, null, null)
        {
        }

        public WebProxy(string Address, bool BypassOnLocal, string[] BypassList)
            : this(CreateProxyUri(Address), BypassOnLocal, BypassList, null)
        {
        }

        public WebProxy(string Address, bool BypassOnLocal, string[] BypassList, ICredentials Credentials)
            : this(CreateProxyUri(Address), BypassOnLocal, BypassList, Credentials)
        {
        }

        public Uri Address { get; set; }

        public bool BypassProxyOnLocal { get; set; }

        public string[] BypassList
        {
            get { return _bypassList != null ? (string[])_bypassList.ToArray(typeof(string)) : Array.Empty<string>(); }
            set
            {
                _bypassList = new ArrayList(value);
                UpdateRegExList(true);
            }
        }

        public ArrayList BypassArrayList => _bypassList ?? (_bypassList = new ArrayList());

        public ICredentials Credentials { get; set; }

        public bool UseDefaultCredentials
        {
            get { return Credentials == CredentialCache.DefaultCredentials; }
            set { Credentials = value ? CredentialCache.DefaultCredentials : null; }
        }

        public Uri GetProxy(Uri destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            return IsBypassed(destination) ? destination : Address;
        }

        private static Uri CreateProxyUri(string address) =>
            address == null ? null :
            address.IndexOf("://") == -1 ? new Uri("http://" + address) :
            new Uri(address);

        private void UpdateRegExList(bool canThrow)
        {
            Regex[] regExBypassList = null;
            ArrayList bypassList = _bypassList;
            try
            {
                if (bypassList != null && bypassList.Count > 0)
                {
                    regExBypassList = new Regex[bypassList.Count];
                    for (int i = 0; i < bypassList.Count; i++)
                    {
                        regExBypassList[i] = new Regex((string)bypassList[i], RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    }
                }
            }
            catch
            {
                if (!canThrow)
                {
                    _regExBypassList = null;
                    return;
                }
                throw;
            }

            // Update field here, as it could throw earlier in the loop
            _regExBypassList = regExBypassList;
        }

        private bool IsMatchInBypassList(Uri input)
        {
            UpdateRegExList(false);

            if (_regExBypassList != null)
            {
                string matchUriString = input.IsDefaultPort ?
                    input.Scheme + "://" + input.Host :
                    input.Scheme + "://" + input.Host + ":" + input.Port.ToString();

                foreach (Regex r in _regExBypassList)
                {
                    if (r.IsMatch(matchUriString))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsLocal(Uri host)
        {
            if (host.IsLoopback)
            {
                return true;
            }

            string hostString = host.Host;

            IPAddress hostAddress;
            if (IPAddress.TryParse(hostString, out hostAddress))
            {
                return IPAddress.IsLoopback(hostAddress) || IsAddressLocal(hostAddress);
            }

            // No dot?  Local.
            int dot = hostString.IndexOf('.');
            if (dot == -1)
            {
                return true;
            }

            // If it matches the primary domain, it's local.  (Whether or not the hostname matches.)
            string local = "." + IPGlobalProperties.GetIPGlobalProperties().DomainName;
            return
                local.Length == (hostString.Length - dot) &&
                string.Compare(local, 0, hostString, dot, local.Length, StringComparison.OrdinalIgnoreCase) == 0;
        }

        private static bool IsAddressLocal(IPAddress ipAddress)
        {
            // Perf note: The .NET Framework caches this and then uses network change notifications to track
            // whether the set should be recomputed.  We could consider doing the same if this is observed as
            // a bottleneck, but that tracking has its own costs.
            IPAddress[] localAddresses = Dns.GetHostEntryAsync(Dns.GetHostName()).GetAwaiter().GetResult().AddressList; // TODO: Use synchronous GetHostEntry when available
            for (int i = 0; i < localAddresses.Length; i++)
            {
                if (ipAddress.Equals(localAddresses[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsBypassed(Uri host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            return
                Address == null ||
                (BypassProxyOnLocal && IsLocal(host)) ||
                IsMatchInBypassList(host);
        }

        protected WebProxy(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        protected virtual void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        [Obsolete("This method has been deprecated. Please use the proxy selected for you by default. https://go.microsoft.com/fwlink/?linkid=14202")]
        public static WebProxy GetDefaultProxy()
        {
            // The .NET Framework here returns a proxy that fetches IE settings and
            // executes JavaScript to determine the correct proxy.
            throw new PlatformNotSupportedException();
        }
    }
}

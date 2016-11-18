// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net
{
    public class ServicePointManager
    {
        public const int DefaultNonPersistentConnectionLimit = 4;
        public const int DefaultPersistentConnectionLimit = 2;

        private static readonly ConcurrentDictionary<string, WeakReference<ServicePoint>> s_servicePointTable = new ConcurrentDictionary<string, WeakReference<ServicePoint>>();
        private static SecurityProtocolType s_securityProtocolType = SecurityProtocolType.SystemDefault;
        private static int s_connectionLimit = 2;
        private static int s_maxServicePoints = 0;
        private static int s_maxServicePointIdleTime = 100 * 1000;
        private static int s_dnsRefreshTimeout = 2 * 60 * 1000;

        private ServicePointManager() { }

        public static SecurityProtocolType SecurityProtocol
        {
            get { return s_securityProtocolType; }
            set
            {
                ValidateSecurityProtocol(value);
                s_securityProtocolType = value;
            }
        }

        private static void ValidateSecurityProtocol(SecurityProtocolType value)
        {
            SecurityProtocolType allowed = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            if ((value & ~allowed) != 0)
            {
                throw new NotSupportedException(SR.net_securityprotocolnotsupported);
            }
        }

        public static int MaxServicePoints
        {
            get { return s_maxServicePoints; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                s_maxServicePoints = value;
            }
        }

        public static int DefaultConnectionLimit
        {
            get { return s_connectionLimit; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                s_connectionLimit = value;
            }
        }

        public static int MaxServicePointIdleTime
        {
            get { return s_maxServicePointIdleTime; }
            set
            {
                if (value < Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                s_maxServicePointIdleTime = value;
            }
        }

        public static bool UseNagleAlgorithm { get; set; } = true;

        public static bool Expect100Continue { get; set; } = true;

        public static bool EnableDnsRoundRobin { get; set; }

        public static int DnsRefreshTimeout
        {
            get { return s_dnsRefreshTimeout; }
            set { s_dnsRefreshTimeout = Math.Max(-1, value); }
        }

        public static RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }

        public static bool ReusePort { get; set; }

        public static bool CheckCertificateRevocationList { get; set; }

        public static EncryptionPolicy EncryptionPolicy { get; } = EncryptionPolicy.RequireEncryption;

        public static ServicePoint FindServicePoint(Uri address) => FindServicePoint(address, null);

        public static ServicePoint FindServicePoint(string uriString, IWebProxy proxy) => FindServicePoint(new Uri(uriString), proxy);

        public static ServicePoint FindServicePoint(Uri address, IWebProxy proxy)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            // If there's a proxy for this address, get the "real" address.
            bool isProxyServicePoint = ProxyAddressIfNecessary(ref address, proxy);

            // Create a lookup key to find the service point
            string tableKey = MakeQueryString(address, isProxyServicePoint);

            // Get an existing service point or create a new one
            ServicePoint sp; // outside of loop to keep references alive from one iteration to the next
            while (true)
            {
                // The table maps lookup key to a weak reference to a service point.  If the table
                // contains a weak ref for the key and that weak ref points to a valid ServicePoint,
                // simply return it (after updating its last used time).
                WeakReference<ServicePoint> wr;
                if (s_servicePointTable.TryGetValue(tableKey, out wr) && wr.TryGetTarget(out sp))
                {
                    sp.IdleSince = DateTime.Now;
                    return sp;
                }

                // Any time we don't find what we're looking for in the table, take that as an opportunity
                // to scavenge the table looking for entries that have lost their service point and removing them.
                foreach (KeyValuePair<string, WeakReference<ServicePoint>> entry in s_servicePointTable)
                {
                    ServicePoint ignored;
                    if (!entry.Value.TryGetTarget(out ignored))
                    {
                        // We use the IDictionary.Remove method rather than TryRemove as it will only
                        // remove the entry from the table if both the key/value in the pair match.
                        // This avoids a race condition where another thread concurrently sets a new
                        // weak reference value for the same key, and is why when adding the new
                        // service point below, we don't use any weak reference object we may already
                        // have from the initial retrieval above.
                        ((IDictionary<string, WeakReference<ServicePoint>>)s_servicePointTable).Remove(entry);
                    }
                }

                // There wasn't a service point in the table.  Create a new one, and then store
                // it back into the table.  We create a new weak reference object even if we were
                // able to get one above so that when we scavenge the table, we can rely on 
                // weak reference reference equality to know whether we're removing the same
                // weak reference we saw when we enumerated.
                sp = new ServicePoint(address)
                {
                    ConnectionLimit = DefaultConnectionLimit,
                    IdleSince = DateTime.Now,
                    Expect100Continue = Expect100Continue,
                    UseNagleAlgorithm = UseNagleAlgorithm
                };
                s_servicePointTable[tableKey] = new WeakReference<ServicePoint>(sp);

                // It's possible there's a race between two threads both updating the table
                // at the same time.  We don't want to just use GetOrAdd, as with the weak
                // reference we may end up getting back a weak ref that no longer has a target.
                // So we simply loop around again; in all but the most severe of circumstances, the
                // next iteration will find it in the table and return it.
            }
        }

        private static bool ProxyAddressIfNecessary(ref Uri address, IWebProxy proxy)
        {
            if (proxy != null && !address.IsLoopback)
            {
                Uri proxyAddress = proxy.GetProxy(address);
                if (proxyAddress != null)
                {
                    if (proxyAddress.Scheme != Uri.UriSchemeHttp)
                    {
                        throw new NotSupportedException(SR.Format(SR.net_proxyschemenotsupported, address.Scheme));
                    }

                    address = proxyAddress;
                    return true;
                }
            }

            return false;
        }

        private static string MakeQueryString(Uri address) => address.IsDefaultPort ?
            address.Scheme + "://" + address.DnsSafeHost :
            address.Scheme + "://" + address.DnsSafeHost + ":" + address.Port.ToString();

        private static string MakeQueryString(Uri address, bool isProxy)
        {
            string queryString = MakeQueryString(address);
            return isProxy ? queryString + "://proxy" : queryString;
        }

        public static void SetTcpKeepAlive(bool enabled, int keepAliveTime, int keepAliveInterval)
        {
            if (enabled)
            {
                if (keepAliveTime <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(keepAliveTime));
                }
                if (keepAliveInterval <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(keepAliveInterval));
                }
            }
        }
    }
}

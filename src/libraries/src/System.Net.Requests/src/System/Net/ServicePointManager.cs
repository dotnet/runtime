// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net
{
    public class ServicePointManager
    {
        public const int DefaultNonPersistentConnectionLimit = 4;
        public const int DefaultPersistentConnectionLimit = 2;

        private static SecurityProtocolType s_securityProtocolType = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        private static int s_connectionLimit = 10;
        private static int s_maxServicePoints = 0;
        private static int s_maxServicePointIdleTime = 100000;
        private static int s_dnsRefreshTimeout = 120000;

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

        [Obsolete("CertificatePolicy is obsoleted for this type, please use ServerCertificateValidationCallback instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static ICertificatePolicy CertificatePolicy { get; set; } = new DefaultCertPolicy();

        private sealed class DefaultCertPolicy : ICertificatePolicy
        {
            public bool CheckValidationResult(ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem) =>
                certificateProblem == 0;
        }

        public static RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }

        public static bool ReusePort { get; set; }

        public static bool CheckCertificateRevocationList { get; set; }

        public static EncryptionPolicy EncryptionPolicy { get; } = EncryptionPolicy.RequireEncryption;

        public static ServicePoint FindServicePoint(Uri address) => FindServicePoint(address, null);

        public static ServicePoint FindServicePoint(string uriString, IWebProxy proxy) => FindServicePoint(new Uri(uriString), proxy);

        public static ServicePoint FindServicePoint(Uri address, IWebProxy proxy)
        {
            throw new PlatformNotSupportedException();
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

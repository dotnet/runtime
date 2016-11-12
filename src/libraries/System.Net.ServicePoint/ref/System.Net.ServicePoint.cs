// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net
{
    public delegate System.Net.IPEndPoint BindIPEndPoint(System.Net.ServicePoint servicePoint, System.Net.IPEndPoint remoteEndPoint, int retryCount);
    public class ServicePoint
    {
        internal ServicePoint() { }
        public System.Net.BindIPEndPoint BindIPEndPointDelegate { get { throw null; } set { } }
        public int ConnectionLeaseTimeout { get { throw null; } set { } }
        public System.Uri Address { get { throw null; } }
        public int MaxIdleTime { get { throw null; } set { } }
        public bool UseNagleAlgorithm { get { throw null; } set { } }
        public int ReceiveBufferSize { get { throw null; } set { } }
        public bool Expect100Continue { get { throw null; } set { } }
        public System.DateTime IdleSince { get { throw null; } }
        public virtual System.Version ProtocolVersion { get { throw null; } }
        public string ConnectionName { get { throw null; } }
        public bool CloseConnectionGroup(string connectionGroupName) { throw null; }
        public int ConnectionLimit { get { throw null; } set { } }
        public int CurrentConnections { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Certificate Certificate { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Certificate ClientCertificate { get { throw null; } }
        public bool SupportsPipelining { get { throw null; } }
        public void SetTcpKeepAlive(bool enabled, int keepAliveTime, int keepAliveInterval) { throw null; }
    }
    public class ServicePointManager
    {
        public const int DefaultNonPersistentConnectionLimit = 4;
        public const int DefaultPersistentConnectionLimit = 2;
        private ServicePointManager() { }
        public static System.Net.SecurityProtocolType SecurityProtocol { get { throw null; } set { } }
        public static int MaxServicePoints { get { throw null; } set { } }
        public static int DefaultConnectionLimit { get { throw null; } set { } }
        public static int MaxServicePointIdleTime { get { throw null; } set { } }
        public static bool UseNagleAlgorithm { get { throw null; } set { } }
        public static bool Expect100Continue { get { throw null; } set { } }
        public static bool EnableDnsRoundRobin { get { throw null; } set { } }
        public static int DnsRefreshTimeout { get { throw null; } set { } }
        public static System.Net.Security.RemoteCertificateValidationCallback ServerCertificateValidationCallback { get { throw null; } set { } }
        public static bool ReusePort { get { throw null; } set { } }
        public static bool CheckCertificateRevocationList { get { throw null; } set { } }
        public static System.Net.Security.EncryptionPolicy EncryptionPolicy { get { throw null; } }
        public static System.Net.ServicePoint FindServicePoint(System.Uri address) { throw null; }
        public static System.Net.ServicePoint FindServicePoint(string uriString, System.Net.IWebProxy proxy) { throw null; }
        public static System.Net.ServicePoint FindServicePoint(System.Uri address, System.Net.IWebProxy proxy) { throw null; }
        public static void SetTcpKeepAlive(bool enabled, int keepAliveTime, int keepAliveInterval) { throw null; }
    }
    [Flags]
    public enum SecurityProtocolType
    {
#if netcoreapp11
        SystemDefault = 0,
#endif
#pragma warning disable CS0618
        Ssl3 = System.Security.Authentication.SslProtocols.Ssl3,
#pragma warning restore CS0618
        Tls = System.Security.Authentication.SslProtocols.Tls,
        Tls11 = System.Security.Authentication.SslProtocols.Tls11,
        Tls12 = System.Security.Authentication.SslProtocols.Tls12,
    }
}

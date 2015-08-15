// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Net
{
    public delegate System.Net.IPEndPoint BindIPEndPoint(System.Net.ServicePoint servicePoint, System.Net.IPEndPoint remoteEndPoint, int retryCount);
    [System.FlagsAttribute]
    public enum SecurityProtocolType
    {
        Ssl3 = 48,
        Tls = 192,
        Tls11 = 768,
        Tls12 = 3072,
    }
    public partial class ServicePoint
    {
        internal ServicePoint() { }
        public System.Uri Address { get { return default(System.Uri); } }
        public System.Net.BindIPEndPoint BindIPEndPointDelegate { get { return default(System.Net.BindIPEndPoint); } set { } }
        public System.Security.Cryptography.X509Certificates.X509Certificate Certificate { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate); } }
        public System.Security.Cryptography.X509Certificates.X509Certificate ClientCertificate { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate); } }
        public int ConnectionLeaseTimeout { get { return default(int); } set { } }
        public int ConnectionLimit { get { return default(int); } set { } }
        public string ConnectionName { get { return default(string); } }
        public int CurrentConnections { get { return default(int); } }
        public bool Expect100Continue { get { return default(bool); } set { } }
        public System.DateTime IdleSince { get { return default(System.DateTime); } }
        public int MaxIdleTime { get { return default(int); } set { } }
        public virtual System.Version ProtocolVersion { get { return default(System.Version); } }
        public int ReceiveBufferSize { get { return default(int); } set { } }
        public bool SupportsPipelining { get { return default(bool); } }
        public bool UseNagleAlgorithm { get { return default(bool); } set { } }
        public bool CloseConnectionGroup(string connectionGroupName) { return default(bool); }
        public void SetTcpKeepAlive(bool enabled, int keepAliveTime, int keepAliveInterval) { }
    }
    public partial class ServicePointManager
    {
        internal ServicePointManager() { }
        public const int DefaultNonPersistentConnectionLimit = 4;
        public const int DefaultPersistentConnectionLimit = 2;
        public static bool CheckCertificateRevocationList { get { return default(bool); } set { } }
        public static int DefaultConnectionLimit { get { return default(int); } set { } }
        public static int DnsRefreshTimeout { get { return default(int); } set { } }
        public static bool EnableDnsRoundRobin { get { return default(bool); } set { } }
        public static System.Net.Security.EncryptionPolicy EncryptionPolicy { get { return default(System.Net.Security.EncryptionPolicy); } }
        public static bool Expect100Continue { get { return default(bool); } set { } }
        public static int MaxServicePointIdleTime { get { return default(int); } set { } }
        public static int MaxServicePoints { get { return default(int); } set { } }
        public static System.Net.SecurityProtocolType SecurityProtocol { get { return default(System.Net.SecurityProtocolType); } set { } }
        public static System.Net.Security.RemoteCertificateValidationCallback ServerCertificateValidationCallback { get { return default(System.Net.Security.RemoteCertificateValidationCallback); } set { } }
        public static bool UseNagleAlgorithm { get { return default(bool); } set { } }
        public static System.Net.ServicePoint FindServicePoint(string uriString, System.Net.IWebProxy proxy) { return default(System.Net.ServicePoint); }
        public static System.Net.ServicePoint FindServicePoint(System.Uri address) { return default(System.Net.ServicePoint); }
        public static System.Net.ServicePoint FindServicePoint(System.Uri address, System.Net.IWebProxy proxy) { return default(System.Net.ServicePoint); }
        public static void SetTcpKeepAlive(bool enabled, int keepAliveTime, int keepAliveInterval) { }
    }
}

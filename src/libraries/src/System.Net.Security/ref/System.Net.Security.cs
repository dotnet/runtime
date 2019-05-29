// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net.Security
{
    public abstract partial class AuthenticatedStream : System.IO.Stream
    {
        protected AuthenticatedStream(System.IO.Stream innerStream, bool leaveInnerStreamOpen) { }
        protected System.IO.Stream InnerStream { get { throw null; } }
        public abstract bool IsAuthenticated { get; }
        public abstract bool IsEncrypted { get; }
        public abstract bool IsMutuallyAuthenticated { get; }
        public abstract bool IsServer { get; }
        public abstract bool IsSigned { get; }
        public bool LeaveInnerStreamOpen { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public override System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
    }
    public enum EncryptionPolicy
    {
        RequireEncryption = 0,
        AllowNoEncryption = 1,
        NoEncryption = 2,
    }
    public delegate System.Security.Cryptography.X509Certificates.X509Certificate LocalCertificateSelectionCallback(object sender, string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection localCertificates, System.Security.Cryptography.X509Certificates.X509Certificate remoteCertificate, string[] acceptableIssuers);
    public partial class NegotiateStream : System.Net.Security.AuthenticatedStream
    {
        public NegotiateStream(System.IO.Stream innerStream) : base (default(System.IO.Stream), default(bool)) { }
        public NegotiateStream(System.IO.Stream innerStream, bool leaveInnerStreamOpen) : base (default(System.IO.Stream), default(bool)) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanTimeout { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public virtual System.Security.Principal.TokenImpersonationLevel ImpersonationLevel { get { throw null; } }
        public override bool IsAuthenticated { get { throw null; } }
        public override bool IsEncrypted { get { throw null; } }
        public override bool IsMutuallyAuthenticated { get { throw null; } }
        public override bool IsServer { get { throw null; } }
        public override bool IsSigned { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override int ReadTimeout { get { throw null; } set { } }
        public virtual System.Security.Principal.IIdentity RemoteIdentity { get { throw null; } }
        public override int WriteTimeout { get { throw null; } set { } }
        public virtual void AuthenticateAsClient() { }
        public virtual void AuthenticateAsClient(System.Net.NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ChannelBinding binding, string targetName) { }
        public virtual void AuthenticateAsClient(System.Net.NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ChannelBinding binding, string targetName, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel allowedImpersonationLevel) { }
        public virtual void AuthenticateAsClient(System.Net.NetworkCredential credential, string targetName) { }
        public virtual void AuthenticateAsClient(System.Net.NetworkCredential credential, string targetName, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel allowedImpersonationLevel) { }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync() { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(System.Net.NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ChannelBinding binding, string targetName) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(System.Net.NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ChannelBinding binding, string targetName, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel allowedImpersonationLevel) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(System.Net.NetworkCredential credential, string targetName) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(System.Net.NetworkCredential credential, string targetName, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel allowedImpersonationLevel) { throw null; }
        public virtual void AuthenticateAsServer() { }
        public virtual void AuthenticateAsServer(System.Net.NetworkCredential credential, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel requiredImpersonationLevel) { }
        public virtual void AuthenticateAsServer(System.Net.NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy policy, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel requiredImpersonationLevel) { }
        public virtual void AuthenticateAsServer(System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy policy) { }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync() { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(System.Net.NetworkCredential credential, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel requiredImpersonationLevel) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(System.Net.NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy policy, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel requiredImpersonationLevel) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy policy) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsClient(System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsClient(System.Net.NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ChannelBinding binding, string targetName, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsClient(System.Net.NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ChannelBinding binding, string targetName, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel allowedImpersonationLevel, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsClient(System.Net.NetworkCredential credential, string targetName, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsClient(System.Net.NetworkCredential credential, string targetName, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel allowedImpersonationLevel, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsServer(System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsServer(System.Net.NetworkCredential credential, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel requiredImpersonationLevel, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsServer(System.Net.NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy policy, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel requiredImpersonationLevel, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsServer(System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy policy, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public virtual void EndAuthenticateAsClient(System.IAsyncResult asyncResult) { }
        public virtual void EndAuthenticateAsServer(System.IAsyncResult asyncResult) { }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
    }
    public enum ProtectionLevel
    {
        None = 0,
        Sign = 1,
        EncryptAndSign = 2,
    }
    public delegate bool RemoteCertificateValidationCallback(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors);
    public delegate System.Security.Cryptography.X509Certificates.X509Certificate ServerCertificateSelectionCallback(object sender, string hostName);
    public readonly partial struct SslApplicationProtocol : System.IEquatable<System.Net.Security.SslApplicationProtocol>
    {
        private readonly object _dummy;
        public static readonly System.Net.Security.SslApplicationProtocol Http11;
        public static readonly System.Net.Security.SslApplicationProtocol Http2;
        public SslApplicationProtocol(byte[] protocol) { throw null; }
        public SslApplicationProtocol(string protocol) { throw null; }
        public System.ReadOnlyMemory<byte> Protocol { get { throw null; } }
        public bool Equals(System.Net.Security.SslApplicationProtocol other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Net.Security.SslApplicationProtocol left, System.Net.Security.SslApplicationProtocol right) { throw null; }
        public static bool operator !=(System.Net.Security.SslApplicationProtocol left, System.Net.Security.SslApplicationProtocol right) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class SslClientAuthenticationOptions
    {
        public SslClientAuthenticationOptions() { }
        public bool AllowRenegotiation { get { throw null; } set { } }
        public System.Collections.Generic.List<System.Net.Security.SslApplicationProtocol> ApplicationProtocols { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509RevocationMode CertificateRevocationCheckMode { get { throw null; } set { } }
        public System.Net.Security.CipherSuitesPolicy CipherSuitesPolicy { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates { get { throw null; } set { } }
        public System.Security.Authentication.SslProtocols EnabledSslProtocols { get { throw null; } set { } }
        public System.Net.Security.EncryptionPolicy EncryptionPolicy { get { throw null; } set { } }
        public System.Net.Security.LocalCertificateSelectionCallback LocalCertificateSelectionCallback { get { throw null; } set { } }
        public System.Net.Security.RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get { throw null; } set { } }
        public string TargetHost { get { throw null; } set { } }
    }
    public partial class SslServerAuthenticationOptions
    {
        public SslServerAuthenticationOptions() { }
        public bool AllowRenegotiation { get { throw null; } set { } }
        public System.Collections.Generic.List<System.Net.Security.SslApplicationProtocol> ApplicationProtocols { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509RevocationMode CertificateRevocationCheckMode { get { throw null; } set { } }
        public System.Net.Security.CipherSuitesPolicy CipherSuitesPolicy { get { throw null; } set { } }
        public bool ClientCertificateRequired { get { throw null; } set { } }
        public System.Security.Authentication.SslProtocols EnabledSslProtocols { get { throw null; } set { } }
        public System.Net.Security.EncryptionPolicy EncryptionPolicy { get { throw null; } set { } }
        public System.Net.Security.RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509Certificate ServerCertificate { get { throw null; } set { } }
        public System.Net.Security.ServerCertificateSelectionCallback ServerCertificateSelectionCallback { get { throw null; } set { } }
    }
    public partial class SslStream : System.Net.Security.AuthenticatedStream
    {
        public SslStream(System.IO.Stream innerStream) : base (default(System.IO.Stream), default(bool)) { }
        public SslStream(System.IO.Stream innerStream, bool leaveInnerStreamOpen) : base (default(System.IO.Stream), default(bool)) { }
        public SslStream(System.IO.Stream innerStream, bool leaveInnerStreamOpen, System.Net.Security.RemoteCertificateValidationCallback userCertificateValidationCallback) : base (default(System.IO.Stream), default(bool)) { }
        public SslStream(System.IO.Stream innerStream, bool leaveInnerStreamOpen, System.Net.Security.RemoteCertificateValidationCallback userCertificateValidationCallback, System.Net.Security.LocalCertificateSelectionCallback userCertificateSelectionCallback) : base (default(System.IO.Stream), default(bool)) { }
        public SslStream(System.IO.Stream innerStream, bool leaveInnerStreamOpen, System.Net.Security.RemoteCertificateValidationCallback userCertificateValidationCallback, System.Net.Security.LocalCertificateSelectionCallback userCertificateSelectionCallback, System.Net.Security.EncryptionPolicy encryptionPolicy) : base (default(System.IO.Stream), default(bool)) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanTimeout { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public virtual bool CheckCertRevocationStatus { get { throw null; } }
        public virtual System.Security.Authentication.CipherAlgorithmType CipherAlgorithm { get { throw null; } }
        public virtual int CipherStrength { get { throw null; } }
        public virtual System.Security.Authentication.HashAlgorithmType HashAlgorithm { get { throw null; } }
        public virtual int HashStrength { get { throw null; } }
        public override bool IsAuthenticated { get { throw null; } }
        public override bool IsEncrypted { get { throw null; } }
        public override bool IsMutuallyAuthenticated { get { throw null; } }
        public override bool IsServer { get { throw null; } }
        public override bool IsSigned { get { throw null; } }
        public virtual System.Security.Authentication.ExchangeAlgorithmType KeyExchangeAlgorithm { get { throw null; } }
        public virtual int KeyExchangeStrength { get { throw null; } }
        public override long Length { get { throw null; } }
        public virtual System.Security.Cryptography.X509Certificates.X509Certificate LocalCertificate { get { throw null; } }
        public System.Net.Security.SslApplicationProtocol NegotiatedApplicationProtocol { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public virtual System.Net.Security.TlsCipherSuite NegotiatedCipherSuite { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override int ReadTimeout { get { throw null; } set { } }
        public virtual System.Security.Cryptography.X509Certificates.X509Certificate RemoteCertificate { get { throw null; } }
        public virtual System.Security.Authentication.SslProtocols SslProtocol { get { throw null; } }
        public System.Net.TransportContext TransportContext { get { throw null; } }
        public override int WriteTimeout { get { throw null; } set { } }
        public virtual void AuthenticateAsClient(string targetHost) { }
        public virtual void AuthenticateAsClient(string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates, bool checkCertificateRevocation) { }
        public virtual void AuthenticateAsClient(string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates, System.Security.Authentication.SslProtocols enabledSslProtocols, bool checkCertificateRevocation) { }
        public System.Threading.Tasks.Task AuthenticateAsClientAsync(System.Net.Security.SslClientAuthenticationOptions sslClientAuthenticationOptions, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(string targetHost) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates, bool checkCertificateRevocation) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates, System.Security.Authentication.SslProtocols enabledSslProtocols, bool checkCertificateRevocation) { throw null; }
        public virtual void AuthenticateAsServer(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate) { }
        public virtual void AuthenticateAsServer(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation) { }
        public virtual void AuthenticateAsServer(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate, bool clientCertificateRequired, System.Security.Authentication.SslProtocols enabledSslProtocols, bool checkCertificateRevocation) { }
        public System.Threading.Tasks.Task AuthenticateAsServerAsync(System.Net.Security.SslServerAuthenticationOptions sslServerAuthenticationOptions, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate, bool clientCertificateRequired, System.Security.Authentication.SslProtocols enabledSslProtocols, bool checkCertificateRevocation) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsClient(string targetHost, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsClient(string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates, bool checkCertificateRevocation, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsClient(string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates, System.Security.Authentication.SslProtocols enabledSslProtocols, bool checkCertificateRevocation, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsServer(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsServer(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsServer(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate, bool clientCertificateRequired, System.Security.Authentication.SslProtocols enabledSslProtocols, bool checkCertificateRevocation, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public virtual void EndAuthenticateAsClient(System.IAsyncResult asyncResult) { }
        public virtual void EndAuthenticateAsServer(System.IAsyncResult asyncResult) { }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public virtual System.Threading.Tasks.Task ShutdownAsync() { throw null; }
        public void Write(byte[] buffer) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public sealed class CipherSuitesPolicy
    {
        [System.CLSCompliantAttribute(false)]
        public CipherSuitesPolicy(System.Collections.Generic.IEnumerable<System.Net.Security.TlsCipherSuite> allowedCipherSuites) { }
        [System.CLSCompliantAttribute(false)]
        public System.Collections.Generic.IEnumerable<System.Net.Security.TlsCipherSuite> AllowedCipherSuites { get; }
    }
    [System.CLSCompliantAttribute(false)]
    public enum TlsCipherSuite : ushort
    {
        TLS_NULL_WITH_NULL_NULL = (ushort)0,
        TLS_RSA_WITH_NULL_MD5 = (ushort)1,
        TLS_RSA_WITH_NULL_SHA = (ushort)2,
        TLS_RSA_EXPORT_WITH_RC4_40_MD5 = (ushort)3,
        TLS_RSA_WITH_RC4_128_MD5 = (ushort)4,
        TLS_RSA_WITH_RC4_128_SHA = (ushort)5,
        TLS_RSA_EXPORT_WITH_RC2_CBC_40_MD5 = (ushort)6,
        TLS_RSA_WITH_IDEA_CBC_SHA = (ushort)7,
        TLS_RSA_EXPORT_WITH_DES40_CBC_SHA = (ushort)8,
        TLS_RSA_WITH_DES_CBC_SHA = (ushort)9,
        TLS_RSA_WITH_3DES_EDE_CBC_SHA = (ushort)10,
        TLS_DH_DSS_EXPORT_WITH_DES40_CBC_SHA = (ushort)11,
        TLS_DH_DSS_WITH_DES_CBC_SHA = (ushort)12,
        TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA = (ushort)13,
        TLS_DH_RSA_EXPORT_WITH_DES40_CBC_SHA = (ushort)14,
        TLS_DH_RSA_WITH_DES_CBC_SHA = (ushort)15,
        TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA = (ushort)16,
        TLS_DHE_DSS_EXPORT_WITH_DES40_CBC_SHA = (ushort)17,
        TLS_DHE_DSS_WITH_DES_CBC_SHA = (ushort)18,
        TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA = (ushort)19,
        TLS_DHE_RSA_EXPORT_WITH_DES40_CBC_SHA = (ushort)20,
        TLS_DHE_RSA_WITH_DES_CBC_SHA = (ushort)21,
        TLS_DHE_RSA_WITH_3DES_EDE_CBC_SHA = (ushort)22,
        TLS_DH_anon_EXPORT_WITH_RC4_40_MD5 = (ushort)23,
        TLS_DH_anon_WITH_RC4_128_MD5 = (ushort)24,
        TLS_DH_anon_EXPORT_WITH_DES40_CBC_SHA = (ushort)25,
        TLS_DH_anon_WITH_DES_CBC_SHA = (ushort)26,
        TLS_DH_anon_WITH_3DES_EDE_CBC_SHA = (ushort)27,
        TLS_KRB5_WITH_DES_CBC_SHA = (ushort)30,
        TLS_KRB5_WITH_3DES_EDE_CBC_SHA = (ushort)31,
        TLS_KRB5_WITH_RC4_128_SHA = (ushort)32,
        TLS_KRB5_WITH_IDEA_CBC_SHA = (ushort)33,
        TLS_KRB5_WITH_DES_CBC_MD5 = (ushort)34,
        TLS_KRB5_WITH_3DES_EDE_CBC_MD5 = (ushort)35,
        TLS_KRB5_WITH_RC4_128_MD5 = (ushort)36,
        TLS_KRB5_WITH_IDEA_CBC_MD5 = (ushort)37,
        TLS_KRB5_EXPORT_WITH_DES_CBC_40_SHA = (ushort)38,
        TLS_KRB5_EXPORT_WITH_RC2_CBC_40_SHA = (ushort)39,
        TLS_KRB5_EXPORT_WITH_RC4_40_SHA = (ushort)40,
        TLS_KRB5_EXPORT_WITH_DES_CBC_40_MD5 = (ushort)41,
        TLS_KRB5_EXPORT_WITH_RC2_CBC_40_MD5 = (ushort)42,
        TLS_KRB5_EXPORT_WITH_RC4_40_MD5 = (ushort)43,
        TLS_PSK_WITH_NULL_SHA = (ushort)44,
        TLS_DHE_PSK_WITH_NULL_SHA = (ushort)45,
        TLS_RSA_PSK_WITH_NULL_SHA = (ushort)46,
        TLS_RSA_WITH_AES_128_CBC_SHA = (ushort)47,
        TLS_DH_DSS_WITH_AES_128_CBC_SHA = (ushort)48,
        TLS_DH_RSA_WITH_AES_128_CBC_SHA = (ushort)49,
        TLS_DHE_DSS_WITH_AES_128_CBC_SHA = (ushort)50,
        TLS_DHE_RSA_WITH_AES_128_CBC_SHA = (ushort)51,
        TLS_DH_anon_WITH_AES_128_CBC_SHA = (ushort)52,
        TLS_RSA_WITH_AES_256_CBC_SHA = (ushort)53,
        TLS_DH_DSS_WITH_AES_256_CBC_SHA = (ushort)54,
        TLS_DH_RSA_WITH_AES_256_CBC_SHA = (ushort)55,
        TLS_DHE_DSS_WITH_AES_256_CBC_SHA = (ushort)56,
        TLS_DHE_RSA_WITH_AES_256_CBC_SHA = (ushort)57,
        TLS_DH_anon_WITH_AES_256_CBC_SHA = (ushort)58,
        TLS_RSA_WITH_NULL_SHA256 = (ushort)59,
        TLS_RSA_WITH_AES_128_CBC_SHA256 = (ushort)60,
        TLS_RSA_WITH_AES_256_CBC_SHA256 = (ushort)61,
        TLS_DH_DSS_WITH_AES_128_CBC_SHA256 = (ushort)62,
        TLS_DH_RSA_WITH_AES_128_CBC_SHA256 = (ushort)63,
        TLS_DHE_DSS_WITH_AES_128_CBC_SHA256 = (ushort)64,
        TLS_RSA_WITH_CAMELLIA_128_CBC_SHA = (ushort)65,
        TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA = (ushort)66,
        TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA = (ushort)67,
        TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA = (ushort)68,
        TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA = (ushort)69,
        TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA = (ushort)70,
        TLS_DHE_RSA_WITH_AES_128_CBC_SHA256 = (ushort)103,
        TLS_DH_DSS_WITH_AES_256_CBC_SHA256 = (ushort)104,
        TLS_DH_RSA_WITH_AES_256_CBC_SHA256 = (ushort)105,
        TLS_DHE_DSS_WITH_AES_256_CBC_SHA256 = (ushort)106,
        TLS_DHE_RSA_WITH_AES_256_CBC_SHA256 = (ushort)107,
        TLS_DH_anon_WITH_AES_128_CBC_SHA256 = (ushort)108,
        TLS_DH_anon_WITH_AES_256_CBC_SHA256 = (ushort)109,
        TLS_RSA_WITH_CAMELLIA_256_CBC_SHA = (ushort)132,
        TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA = (ushort)133,
        TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA = (ushort)134,
        TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA = (ushort)135,
        TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA = (ushort)136,
        TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA = (ushort)137,
        TLS_PSK_WITH_RC4_128_SHA = (ushort)138,
        TLS_PSK_WITH_3DES_EDE_CBC_SHA = (ushort)139,
        TLS_PSK_WITH_AES_128_CBC_SHA = (ushort)140,
        TLS_PSK_WITH_AES_256_CBC_SHA = (ushort)141,
        TLS_DHE_PSK_WITH_RC4_128_SHA = (ushort)142,
        TLS_DHE_PSK_WITH_3DES_EDE_CBC_SHA = (ushort)143,
        TLS_DHE_PSK_WITH_AES_128_CBC_SHA = (ushort)144,
        TLS_DHE_PSK_WITH_AES_256_CBC_SHA = (ushort)145,
        TLS_RSA_PSK_WITH_RC4_128_SHA = (ushort)146,
        TLS_RSA_PSK_WITH_3DES_EDE_CBC_SHA = (ushort)147,
        TLS_RSA_PSK_WITH_AES_128_CBC_SHA = (ushort)148,
        TLS_RSA_PSK_WITH_AES_256_CBC_SHA = (ushort)149,
        TLS_RSA_WITH_SEED_CBC_SHA = (ushort)150,
        TLS_DH_DSS_WITH_SEED_CBC_SHA = (ushort)151,
        TLS_DH_RSA_WITH_SEED_CBC_SHA = (ushort)152,
        TLS_DHE_DSS_WITH_SEED_CBC_SHA = (ushort)153,
        TLS_DHE_RSA_WITH_SEED_CBC_SHA = (ushort)154,
        TLS_DH_anon_WITH_SEED_CBC_SHA = (ushort)155,
        TLS_RSA_WITH_AES_128_GCM_SHA256 = (ushort)156,
        TLS_RSA_WITH_AES_256_GCM_SHA384 = (ushort)157,
        TLS_DHE_RSA_WITH_AES_128_GCM_SHA256 = (ushort)158,
        TLS_DHE_RSA_WITH_AES_256_GCM_SHA384 = (ushort)159,
        TLS_DH_RSA_WITH_AES_128_GCM_SHA256 = (ushort)160,
        TLS_DH_RSA_WITH_AES_256_GCM_SHA384 = (ushort)161,
        TLS_DHE_DSS_WITH_AES_128_GCM_SHA256 = (ushort)162,
        TLS_DHE_DSS_WITH_AES_256_GCM_SHA384 = (ushort)163,
        TLS_DH_DSS_WITH_AES_128_GCM_SHA256 = (ushort)164,
        TLS_DH_DSS_WITH_AES_256_GCM_SHA384 = (ushort)165,
        TLS_DH_anon_WITH_AES_128_GCM_SHA256 = (ushort)166,
        TLS_DH_anon_WITH_AES_256_GCM_SHA384 = (ushort)167,
        TLS_PSK_WITH_AES_128_GCM_SHA256 = (ushort)168,
        TLS_PSK_WITH_AES_256_GCM_SHA384 = (ushort)169,
        TLS_DHE_PSK_WITH_AES_128_GCM_SHA256 = (ushort)170,
        TLS_DHE_PSK_WITH_AES_256_GCM_SHA384 = (ushort)171,
        TLS_RSA_PSK_WITH_AES_128_GCM_SHA256 = (ushort)172,
        TLS_RSA_PSK_WITH_AES_256_GCM_SHA384 = (ushort)173,
        TLS_PSK_WITH_AES_128_CBC_SHA256 = (ushort)174,
        TLS_PSK_WITH_AES_256_CBC_SHA384 = (ushort)175,
        TLS_PSK_WITH_NULL_SHA256 = (ushort)176,
        TLS_PSK_WITH_NULL_SHA384 = (ushort)177,
        TLS_DHE_PSK_WITH_AES_128_CBC_SHA256 = (ushort)178,
        TLS_DHE_PSK_WITH_AES_256_CBC_SHA384 = (ushort)179,
        TLS_DHE_PSK_WITH_NULL_SHA256 = (ushort)180,
        TLS_DHE_PSK_WITH_NULL_SHA384 = (ushort)181,
        TLS_RSA_PSK_WITH_AES_128_CBC_SHA256 = (ushort)182,
        TLS_RSA_PSK_WITH_AES_256_CBC_SHA384 = (ushort)183,
        TLS_RSA_PSK_WITH_NULL_SHA256 = (ushort)184,
        TLS_RSA_PSK_WITH_NULL_SHA384 = (ushort)185,
        TLS_RSA_WITH_CAMELLIA_128_CBC_SHA256 = (ushort)186,
        TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA256 = (ushort)187,
        TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA256 = (ushort)188,
        TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA256 = (ushort)189,
        TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA256 = (ushort)190,
        TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA256 = (ushort)191,
        TLS_RSA_WITH_CAMELLIA_256_CBC_SHA256 = (ushort)192,
        TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA256 = (ushort)193,
        TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA256 = (ushort)194,
        TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA256 = (ushort)195,
        TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA256 = (ushort)196,
        TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA256 = (ushort)197,
        TLS_AES_128_GCM_SHA256 = (ushort)4865,
        TLS_AES_256_GCM_SHA384 = (ushort)4866,
        TLS_CHACHA20_POLY1305_SHA256 = (ushort)4867,
        TLS_AES_128_CCM_SHA256 = (ushort)4868,
        TLS_AES_128_CCM_8_SHA256 = (ushort)4869,
        TLS_ECDH_ECDSA_WITH_NULL_SHA = (ushort)49153,
        TLS_ECDH_ECDSA_WITH_RC4_128_SHA = (ushort)49154,
        TLS_ECDH_ECDSA_WITH_3DES_EDE_CBC_SHA = (ushort)49155,
        TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA = (ushort)49156,
        TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA = (ushort)49157,
        TLS_ECDHE_ECDSA_WITH_NULL_SHA = (ushort)49158,
        TLS_ECDHE_ECDSA_WITH_RC4_128_SHA = (ushort)49159,
        TLS_ECDHE_ECDSA_WITH_3DES_EDE_CBC_SHA = (ushort)49160,
        TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA = (ushort)49161,
        TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA = (ushort)49162,
        TLS_ECDH_RSA_WITH_NULL_SHA = (ushort)49163,
        TLS_ECDH_RSA_WITH_RC4_128_SHA = (ushort)49164,
        TLS_ECDH_RSA_WITH_3DES_EDE_CBC_SHA = (ushort)49165,
        TLS_ECDH_RSA_WITH_AES_128_CBC_SHA = (ushort)49166,
        TLS_ECDH_RSA_WITH_AES_256_CBC_SHA = (ushort)49167,
        TLS_ECDHE_RSA_WITH_NULL_SHA = (ushort)49168,
        TLS_ECDHE_RSA_WITH_RC4_128_SHA = (ushort)49169,
        TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA = (ushort)49170,
        TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA = (ushort)49171,
        TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA = (ushort)49172,
        TLS_ECDH_anon_WITH_NULL_SHA = (ushort)49173,
        TLS_ECDH_anon_WITH_RC4_128_SHA = (ushort)49174,
        TLS_ECDH_anon_WITH_3DES_EDE_CBC_SHA = (ushort)49175,
        TLS_ECDH_anon_WITH_AES_128_CBC_SHA = (ushort)49176,
        TLS_ECDH_anon_WITH_AES_256_CBC_SHA = (ushort)49177,
        TLS_SRP_SHA_WITH_3DES_EDE_CBC_SHA = (ushort)49178,
        TLS_SRP_SHA_RSA_WITH_3DES_EDE_CBC_SHA = (ushort)49179,
        TLS_SRP_SHA_DSS_WITH_3DES_EDE_CBC_SHA = (ushort)49180,
        TLS_SRP_SHA_WITH_AES_128_CBC_SHA = (ushort)49181,
        TLS_SRP_SHA_RSA_WITH_AES_128_CBC_SHA = (ushort)49182,
        TLS_SRP_SHA_DSS_WITH_AES_128_CBC_SHA = (ushort)49183,
        TLS_SRP_SHA_WITH_AES_256_CBC_SHA = (ushort)49184,
        TLS_SRP_SHA_RSA_WITH_AES_256_CBC_SHA = (ushort)49185,
        TLS_SRP_SHA_DSS_WITH_AES_256_CBC_SHA = (ushort)49186,
        TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256 = (ushort)49187,
        TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384 = (ushort)49188,
        TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256 = (ushort)49189,
        TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384 = (ushort)49190,
        TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256 = (ushort)49191,
        TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384 = (ushort)49192,
        TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256 = (ushort)49193,
        TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384 = (ushort)49194,
        TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256 = (ushort)49195,
        TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384 = (ushort)49196,
        TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256 = (ushort)49197,
        TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384 = (ushort)49198,
        TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256 = (ushort)49199,
        TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384 = (ushort)49200,
        TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256 = (ushort)49201,
        TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384 = (ushort)49202,
        TLS_ECDHE_PSK_WITH_RC4_128_SHA = (ushort)49203,
        TLS_ECDHE_PSK_WITH_3DES_EDE_CBC_SHA = (ushort)49204,
        TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA = (ushort)49205,
        TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA = (ushort)49206,
        TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA256 = (ushort)49207,
        TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA384 = (ushort)49208,
        TLS_ECDHE_PSK_WITH_NULL_SHA = (ushort)49209,
        TLS_ECDHE_PSK_WITH_NULL_SHA256 = (ushort)49210,
        TLS_ECDHE_PSK_WITH_NULL_SHA384 = (ushort)49211,
        TLS_RSA_WITH_ARIA_128_CBC_SHA256 = (ushort)49212,
        TLS_RSA_WITH_ARIA_256_CBC_SHA384 = (ushort)49213,
        TLS_DH_DSS_WITH_ARIA_128_CBC_SHA256 = (ushort)49214,
        TLS_DH_DSS_WITH_ARIA_256_CBC_SHA384 = (ushort)49215,
        TLS_DH_RSA_WITH_ARIA_128_CBC_SHA256 = (ushort)49216,
        TLS_DH_RSA_WITH_ARIA_256_CBC_SHA384 = (ushort)49217,
        TLS_DHE_DSS_WITH_ARIA_128_CBC_SHA256 = (ushort)49218,
        TLS_DHE_DSS_WITH_ARIA_256_CBC_SHA384 = (ushort)49219,
        TLS_DHE_RSA_WITH_ARIA_128_CBC_SHA256 = (ushort)49220,
        TLS_DHE_RSA_WITH_ARIA_256_CBC_SHA384 = (ushort)49221,
        TLS_DH_anon_WITH_ARIA_128_CBC_SHA256 = (ushort)49222,
        TLS_DH_anon_WITH_ARIA_256_CBC_SHA384 = (ushort)49223,
        TLS_ECDHE_ECDSA_WITH_ARIA_128_CBC_SHA256 = (ushort)49224,
        TLS_ECDHE_ECDSA_WITH_ARIA_256_CBC_SHA384 = (ushort)49225,
        TLS_ECDH_ECDSA_WITH_ARIA_128_CBC_SHA256 = (ushort)49226,
        TLS_ECDH_ECDSA_WITH_ARIA_256_CBC_SHA384 = (ushort)49227,
        TLS_ECDHE_RSA_WITH_ARIA_128_CBC_SHA256 = (ushort)49228,
        TLS_ECDHE_RSA_WITH_ARIA_256_CBC_SHA384 = (ushort)49229,
        TLS_ECDH_RSA_WITH_ARIA_128_CBC_SHA256 = (ushort)49230,
        TLS_ECDH_RSA_WITH_ARIA_256_CBC_SHA384 = (ushort)49231,
        TLS_RSA_WITH_ARIA_128_GCM_SHA256 = (ushort)49232,
        TLS_RSA_WITH_ARIA_256_GCM_SHA384 = (ushort)49233,
        TLS_DHE_RSA_WITH_ARIA_128_GCM_SHA256 = (ushort)49234,
        TLS_DHE_RSA_WITH_ARIA_256_GCM_SHA384 = (ushort)49235,
        TLS_DH_RSA_WITH_ARIA_128_GCM_SHA256 = (ushort)49236,
        TLS_DH_RSA_WITH_ARIA_256_GCM_SHA384 = (ushort)49237,
        TLS_DHE_DSS_WITH_ARIA_128_GCM_SHA256 = (ushort)49238,
        TLS_DHE_DSS_WITH_ARIA_256_GCM_SHA384 = (ushort)49239,
        TLS_DH_DSS_WITH_ARIA_128_GCM_SHA256 = (ushort)49240,
        TLS_DH_DSS_WITH_ARIA_256_GCM_SHA384 = (ushort)49241,
        TLS_DH_anon_WITH_ARIA_128_GCM_SHA256 = (ushort)49242,
        TLS_DH_anon_WITH_ARIA_256_GCM_SHA384 = (ushort)49243,
        TLS_ECDHE_ECDSA_WITH_ARIA_128_GCM_SHA256 = (ushort)49244,
        TLS_ECDHE_ECDSA_WITH_ARIA_256_GCM_SHA384 = (ushort)49245,
        TLS_ECDH_ECDSA_WITH_ARIA_128_GCM_SHA256 = (ushort)49246,
        TLS_ECDH_ECDSA_WITH_ARIA_256_GCM_SHA384 = (ushort)49247,
        TLS_ECDHE_RSA_WITH_ARIA_128_GCM_SHA256 = (ushort)49248,
        TLS_ECDHE_RSA_WITH_ARIA_256_GCM_SHA384 = (ushort)49249,
        TLS_ECDH_RSA_WITH_ARIA_128_GCM_SHA256 = (ushort)49250,
        TLS_ECDH_RSA_WITH_ARIA_256_GCM_SHA384 = (ushort)49251,
        TLS_PSK_WITH_ARIA_128_CBC_SHA256 = (ushort)49252,
        TLS_PSK_WITH_ARIA_256_CBC_SHA384 = (ushort)49253,
        TLS_DHE_PSK_WITH_ARIA_128_CBC_SHA256 = (ushort)49254,
        TLS_DHE_PSK_WITH_ARIA_256_CBC_SHA384 = (ushort)49255,
        TLS_RSA_PSK_WITH_ARIA_128_CBC_SHA256 = (ushort)49256,
        TLS_RSA_PSK_WITH_ARIA_256_CBC_SHA384 = (ushort)49257,
        TLS_PSK_WITH_ARIA_128_GCM_SHA256 = (ushort)49258,
        TLS_PSK_WITH_ARIA_256_GCM_SHA384 = (ushort)49259,
        TLS_DHE_PSK_WITH_ARIA_128_GCM_SHA256 = (ushort)49260,
        TLS_DHE_PSK_WITH_ARIA_256_GCM_SHA384 = (ushort)49261,
        TLS_RSA_PSK_WITH_ARIA_128_GCM_SHA256 = (ushort)49262,
        TLS_RSA_PSK_WITH_ARIA_256_GCM_SHA384 = (ushort)49263,
        TLS_ECDHE_PSK_WITH_ARIA_128_CBC_SHA256 = (ushort)49264,
        TLS_ECDHE_PSK_WITH_ARIA_256_CBC_SHA384 = (ushort)49265,
        TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_CBC_SHA256 = (ushort)49266,
        TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_CBC_SHA384 = (ushort)49267,
        TLS_ECDH_ECDSA_WITH_CAMELLIA_128_CBC_SHA256 = (ushort)49268,
        TLS_ECDH_ECDSA_WITH_CAMELLIA_256_CBC_SHA384 = (ushort)49269,
        TLS_ECDHE_RSA_WITH_CAMELLIA_128_CBC_SHA256 = (ushort)49270,
        TLS_ECDHE_RSA_WITH_CAMELLIA_256_CBC_SHA384 = (ushort)49271,
        TLS_ECDH_RSA_WITH_CAMELLIA_128_CBC_SHA256 = (ushort)49272,
        TLS_ECDH_RSA_WITH_CAMELLIA_256_CBC_SHA384 = (ushort)49273,
        TLS_RSA_WITH_CAMELLIA_128_GCM_SHA256 = (ushort)49274,
        TLS_RSA_WITH_CAMELLIA_256_GCM_SHA384 = (ushort)49275,
        TLS_DHE_RSA_WITH_CAMELLIA_128_GCM_SHA256 = (ushort)49276,
        TLS_DHE_RSA_WITH_CAMELLIA_256_GCM_SHA384 = (ushort)49277,
        TLS_DH_RSA_WITH_CAMELLIA_128_GCM_SHA256 = (ushort)49278,
        TLS_DH_RSA_WITH_CAMELLIA_256_GCM_SHA384 = (ushort)49279,
        TLS_DHE_DSS_WITH_CAMELLIA_128_GCM_SHA256 = (ushort)49280,
        TLS_DHE_DSS_WITH_CAMELLIA_256_GCM_SHA384 = (ushort)49281,
        TLS_DH_DSS_WITH_CAMELLIA_128_GCM_SHA256 = (ushort)49282,
        TLS_DH_DSS_WITH_CAMELLIA_256_GCM_SHA384 = (ushort)49283,
        TLS_DH_anon_WITH_CAMELLIA_128_GCM_SHA256 = (ushort)49284,
        TLS_DH_anon_WITH_CAMELLIA_256_GCM_SHA384 = (ushort)49285,
        TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_GCM_SHA256 = (ushort)49286,
        TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_GCM_SHA384 = (ushort)49287,
        TLS_ECDH_ECDSA_WITH_CAMELLIA_128_GCM_SHA256 = (ushort)49288,
        TLS_ECDH_ECDSA_WITH_CAMELLIA_256_GCM_SHA384 = (ushort)49289,
        TLS_ECDHE_RSA_WITH_CAMELLIA_128_GCM_SHA256 = (ushort)49290,
        TLS_ECDHE_RSA_WITH_CAMELLIA_256_GCM_SHA384 = (ushort)49291,
        TLS_ECDH_RSA_WITH_CAMELLIA_128_GCM_SHA256 = (ushort)49292,
        TLS_ECDH_RSA_WITH_CAMELLIA_256_GCM_SHA384 = (ushort)49293,
        TLS_PSK_WITH_CAMELLIA_128_GCM_SHA256 = (ushort)49294,
        TLS_PSK_WITH_CAMELLIA_256_GCM_SHA384 = (ushort)49295,
        TLS_DHE_PSK_WITH_CAMELLIA_128_GCM_SHA256 = (ushort)49296,
        TLS_DHE_PSK_WITH_CAMELLIA_256_GCM_SHA384 = (ushort)49297,
        TLS_RSA_PSK_WITH_CAMELLIA_128_GCM_SHA256 = (ushort)49298,
        TLS_RSA_PSK_WITH_CAMELLIA_256_GCM_SHA384 = (ushort)49299,
        TLS_PSK_WITH_CAMELLIA_128_CBC_SHA256 = (ushort)49300,
        TLS_PSK_WITH_CAMELLIA_256_CBC_SHA384 = (ushort)49301,
        TLS_DHE_PSK_WITH_CAMELLIA_128_CBC_SHA256 = (ushort)49302,
        TLS_DHE_PSK_WITH_CAMELLIA_256_CBC_SHA384 = (ushort)49303,
        TLS_RSA_PSK_WITH_CAMELLIA_128_CBC_SHA256 = (ushort)49304,
        TLS_RSA_PSK_WITH_CAMELLIA_256_CBC_SHA384 = (ushort)49305,
        TLS_ECDHE_PSK_WITH_CAMELLIA_128_CBC_SHA256 = (ushort)49306,
        TLS_ECDHE_PSK_WITH_CAMELLIA_256_CBC_SHA384 = (ushort)49307,
        TLS_RSA_WITH_AES_128_CCM = (ushort)49308,
        TLS_RSA_WITH_AES_256_CCM = (ushort)49309,
        TLS_DHE_RSA_WITH_AES_128_CCM = (ushort)49310,
        TLS_DHE_RSA_WITH_AES_256_CCM = (ushort)49311,
        TLS_RSA_WITH_AES_128_CCM_8 = (ushort)49312,
        TLS_RSA_WITH_AES_256_CCM_8 = (ushort)49313,
        TLS_DHE_RSA_WITH_AES_128_CCM_8 = (ushort)49314,
        TLS_DHE_RSA_WITH_AES_256_CCM_8 = (ushort)49315,
        TLS_PSK_WITH_AES_128_CCM = (ushort)49316,
        TLS_PSK_WITH_AES_256_CCM = (ushort)49317,
        TLS_DHE_PSK_WITH_AES_128_CCM = (ushort)49318,
        TLS_DHE_PSK_WITH_AES_256_CCM = (ushort)49319,
        TLS_PSK_WITH_AES_128_CCM_8 = (ushort)49320,
        TLS_PSK_WITH_AES_256_CCM_8 = (ushort)49321,
        TLS_PSK_DHE_WITH_AES_128_CCM_8 = (ushort)49322,
        TLS_PSK_DHE_WITH_AES_256_CCM_8 = (ushort)49323,
        TLS_ECDHE_ECDSA_WITH_AES_128_CCM = (ushort)49324,
        TLS_ECDHE_ECDSA_WITH_AES_256_CCM = (ushort)49325,
        TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8 = (ushort)49326,
        TLS_ECDHE_ECDSA_WITH_AES_256_CCM_8 = (ushort)49327,
        TLS_ECCPWD_WITH_AES_128_GCM_SHA256 = (ushort)49328,
        TLS_ECCPWD_WITH_AES_256_GCM_SHA384 = (ushort)49329,
        TLS_ECCPWD_WITH_AES_128_CCM_SHA256 = (ushort)49330,
        TLS_ECCPWD_WITH_AES_256_CCM_SHA384 = (ushort)49331,
        TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256 = (ushort)52392,
        TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256 = (ushort)52393,
        TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256 = (ushort)52394,
        TLS_PSK_WITH_CHACHA20_POLY1305_SHA256 = (ushort)52395,
        TLS_ECDHE_PSK_WITH_CHACHA20_POLY1305_SHA256 = (ushort)52396,
        TLS_DHE_PSK_WITH_CHACHA20_POLY1305_SHA256 = (ushort)52397,
        TLS_RSA_PSK_WITH_CHACHA20_POLY1305_SHA256 = (ushort)52398,
        TLS_ECDHE_PSK_WITH_AES_128_GCM_SHA256 = (ushort)53249,
        TLS_ECDHE_PSK_WITH_AES_256_GCM_SHA384 = (ushort)53250,
        TLS_ECDHE_PSK_WITH_AES_128_CCM_8_SHA256 = (ushort)53251,
        TLS_ECDHE_PSK_WITH_AES_128_CCM_SHA256 = (ushort)53253,
    }
}
namespace System.Security.Authentication
{
    public partial class AuthenticationException : System.SystemException
    {
        public AuthenticationException() { }
        protected AuthenticationException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public AuthenticationException(string message) { }
        public AuthenticationException(string message, System.Exception innerException) { }
    }
    public partial class InvalidCredentialException : System.Security.Authentication.AuthenticationException
    {
        public InvalidCredentialException() { }
        protected InvalidCredentialException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public InvalidCredentialException(string message) { }
        public InvalidCredentialException(string message, System.Exception innerException) { }
    }
}
namespace System.Security.Authentication.ExtendedProtection
{
    public partial class ExtendedProtectionPolicy : System.Runtime.Serialization.ISerializable
    {
        protected ExtendedProtectionPolicy(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ExtendedProtectionPolicy(System.Security.Authentication.ExtendedProtection.PolicyEnforcement policyEnforcement) { }
        public ExtendedProtectionPolicy(System.Security.Authentication.ExtendedProtection.PolicyEnforcement policyEnforcement, System.Security.Authentication.ExtendedProtection.ChannelBinding customChannelBinding) { }
        public ExtendedProtectionPolicy(System.Security.Authentication.ExtendedProtection.PolicyEnforcement policyEnforcement, System.Security.Authentication.ExtendedProtection.ProtectionScenario protectionScenario, System.Collections.ICollection customServiceNames) { }
        public ExtendedProtectionPolicy(System.Security.Authentication.ExtendedProtection.PolicyEnforcement policyEnforcement, System.Security.Authentication.ExtendedProtection.ProtectionScenario protectionScenario, System.Security.Authentication.ExtendedProtection.ServiceNameCollection customServiceNames) { }
        public System.Security.Authentication.ExtendedProtection.ChannelBinding CustomChannelBinding { get { throw null; } }
        public System.Security.Authentication.ExtendedProtection.ServiceNameCollection CustomServiceNames { get { throw null; } }
        public static bool OSSupportsExtendedProtection { get { throw null; } }
        public System.Security.Authentication.ExtendedProtection.PolicyEnforcement PolicyEnforcement { get { throw null; } }
        public System.Security.Authentication.ExtendedProtection.ProtectionScenario ProtectionScenario { get { throw null; } }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
    }
    public enum PolicyEnforcement
    {
        Never = 0,
        WhenSupported = 1,
        Always = 2,
    }
    public enum ProtectionScenario
    {
        TransportSelected = 0,
        TrustedProxy = 1,
    }
    public partial class ServiceNameCollection : System.Collections.ReadOnlyCollectionBase
    {
        public ServiceNameCollection(System.Collections.ICollection items) { }
        public bool Contains(string searchServiceName) { throw null; }
        public System.Security.Authentication.ExtendedProtection.ServiceNameCollection Merge(System.Collections.IEnumerable serviceNames) { throw null; }
        public System.Security.Authentication.ExtendedProtection.ServiceNameCollection Merge(string serviceName) { throw null; }
    }
}

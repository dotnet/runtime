// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Net.Security
{
    public abstract class AuthenticatedStream : System.IO.Stream
    {
        protected AuthenticatedStream(System.IO.Stream innerStream, bool leaveInnerStreamOpen) { }
        public abstract bool IsAuthenticated { get; }
        public abstract bool IsEncrypted { get; }
        public abstract bool IsMutuallyAuthenticated { get; }
        public abstract bool IsServer { get; }
        public abstract bool IsSigned { get; }
        public bool LeaveInnerStreamOpen { get { return default(bool); } }
        protected System.IO.Stream InnerStream { get { return default(System.IO.Stream); } }
        protected override void Dispose(bool disposing) { }
    }
    public enum EncryptionPolicy
    {
        AllowNoEncryption = 1,
        NoEncryption = 2,
        RequireEncryption = 0,
    }
    public delegate System.Security.Cryptography.X509Certificates.X509Certificate LocalCertificateSelectionCallback(object sender, string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection localCertificates, System.Security.Cryptography.X509Certificates.X509Certificate remoteCertificate, string[] acceptableIssuers);
    public partial class NegotiateStream : AuthenticatedStream
    {
        public NegotiateStream(System.IO.Stream innerStream) : base(innerStream, false) { }
        public NegotiateStream(System.IO.Stream innerStream, bool leaveInnerStreamOpen) : base(innerStream, leaveInnerStreamOpen) { }
        public override bool CanRead { get { return default(bool); } }
        public override bool CanSeek { get { return default(bool); } }
        public override bool CanTimeout { get { return default(bool); } }
        public override bool CanWrite { get { return default(bool); } }
        public virtual System.Security.Principal.TokenImpersonationLevel ImpersonationLevel { get { return default(System.Security.Principal.TokenImpersonationLevel); } }
        public override bool IsAuthenticated { get { return default(bool); } }
        public override bool IsEncrypted { get { return default(bool); } }
        public override bool IsMutuallyAuthenticated { get { return default(bool); } }
        public override bool IsServer { get { return default(bool); } }
        public override bool IsSigned { get { return default(bool); } }
        public override long Length { get { return default(long); } }
        public override long Position { get { return default(long); } set { } }
        public override int ReadTimeout { get { return default(int); } set { } }
        public virtual System.Security.Principal.IIdentity RemoteIdentity { get { return default(System.Security.Principal.IIdentity); } }
        public override int WriteTimeout { get { return default(int); } set { } }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync() { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(NetworkCredential credential, string targetName) { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ChannelBinding binding, string targetName) { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(NetworkCredential credential, string targetName, ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel allowedImpersonationLevel) { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ChannelBinding binding, string targetName, ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel allowedImpersonationLevel) { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync() { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy policy) { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel requiredImpersonationLevel) { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy policy, ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel requiredImpersonationLevel) { return default(System.Threading.Tasks.Task); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) { return default(int); }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { return default(long); }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
    }
    public enum ProtectionLevel
    {
        None = 0,
        Sign = 1,
        EncryptAndSign = 2
    }
    public delegate bool RemoteCertificateValidationCallback(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors);
    public partial class SslStream : AuthenticatedStream
    {
        public SslStream(System.IO.Stream innerStream) : base(innerStream, false) { }
        public SslStream(System.IO.Stream innerStream, bool leaveInnerStreamOpen) : base(innerStream, leaveInnerStreamOpen) { }
        public SslStream(System.IO.Stream innerStream, bool leaveInnerStreamOpen, System.Net.Security.RemoteCertificateValidationCallback userCertificateValidationCallback) : base(innerStream, leaveInnerStreamOpen) { }
        public SslStream(System.IO.Stream innerStream, bool leaveInnerStreamOpen, System.Net.Security.RemoteCertificateValidationCallback userCertificateValidationCallback, System.Net.Security.LocalCertificateSelectionCallback userCertificateSelectionCallback) : base(innerStream, leaveInnerStreamOpen) { }
        public SslStream(System.IO.Stream innerStream, bool leaveInnerStreamOpen, System.Net.Security.RemoteCertificateValidationCallback userCertificateValidationCallback, System.Net.Security.LocalCertificateSelectionCallback userCertificateSelectionCallback, System.Net.Security.EncryptionPolicy encryptionPolicy) : base(innerStream, leaveInnerStreamOpen) { }
        public override bool CanRead { get { return default(bool); } }
        public override bool CanSeek { get { return default(bool); } }
        public override bool CanTimeout { get { return default(bool); } }
        public override bool CanWrite { get { return default(bool); } }
        public virtual bool CheckCertRevocationStatus { get { return default(bool); } }
        public virtual System.Security.Authentication.CipherAlgorithmType CipherAlgorithm { get { return default(System.Security.Authentication.CipherAlgorithmType); } }
        public virtual int CipherStrength { get { return default(int); } }
        public virtual System.Security.Authentication.HashAlgorithmType HashAlgorithm { get { return default(System.Security.Authentication.HashAlgorithmType); } }
        public virtual int HashStrength { get { return default(int); } }
        public override bool IsAuthenticated { get { return default(bool); } }
        public override bool IsEncrypted { get { return default(bool); } }
        public override bool IsMutuallyAuthenticated { get { return default(bool); } }
        public override bool IsServer { get { return default(bool); } }
        public override bool IsSigned { get { return default(bool); } }
        public virtual System.Security.Authentication.ExchangeAlgorithmType KeyExchangeAlgorithm { get { return default(System.Security.Authentication.ExchangeAlgorithmType); } }
        public virtual int KeyExchangeStrength { get { return default(int); } }
        public override long Length { get { return default(long); } }
        public virtual System.Security.Cryptography.X509Certificates.X509Certificate LocalCertificate { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate); } }
        public override long Position { get { return default(long); } set { } }
        public override int ReadTimeout { get { return default(int); } set { } }
        public virtual System.Security.Cryptography.X509Certificates.X509Certificate RemoteCertificate { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate); } }
        public virtual System.Security.Authentication.SslProtocols SslProtocol { get { return default(System.Security.Authentication.SslProtocols); } }
        public System.Net.TransportContext TransportContext { get { return default(System.Net.TransportContext); } }
        public override int WriteTimeout { get { return default(int); } set { } }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(string targetHost) { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates, System.Security.Authentication.SslProtocols enabledSslProtocols, bool checkCertificateRevocation) { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate) { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate, bool clientCertificateRequired, System.Security.Authentication.SslProtocols enabledSslProtocols, bool checkCertificateRevocation) { return default(System.Threading.Tasks.Task); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) { return default(int); }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { return default(long); }
        public override void SetLength(long value) { }
        public void Write(byte[] buffer) { }
        public override void Write(byte[] buffer, int offset, int count) { }
    }
}
namespace System.Security.Authentication
{
    public partial class AuthenticationException : System.Exception
    {
        public AuthenticationException() { }
        public AuthenticationException(string message) { }
        public AuthenticationException(string message, System.Exception innerException) { }
    }
}
namespace System.Security.Authentication.ExtendedProtection
{
    public partial class ExtendedProtectionPolicy
    {
        public ExtendedProtectionPolicy(System.Security.Authentication.ExtendedProtection.PolicyEnforcement policyEnforcement) { }
        public ExtendedProtectionPolicy(System.Security.Authentication.ExtendedProtection.PolicyEnforcement policyEnforcement, System.Security.Authentication.ExtendedProtection.ChannelBinding customChannelBinding) { }
        public ExtendedProtectionPolicy(System.Security.Authentication.ExtendedProtection.PolicyEnforcement policyEnforcement, System.Security.Authentication.ExtendedProtection.ProtectionScenario protectionScenario, System.Collections.ICollection customServiceNames) { }
        public ExtendedProtectionPolicy(System.Security.Authentication.ExtendedProtection.PolicyEnforcement policyEnforcement, System.Security.Authentication.ExtendedProtection.ProtectionScenario protectionScenario, System.Security.Authentication.ExtendedProtection.ServiceNameCollection customServiceNames) { }
        public System.Security.Authentication.ExtendedProtection.ChannelBinding CustomChannelBinding { get { return default(System.Security.Authentication.ExtendedProtection.ChannelBinding); } }
        public System.Security.Authentication.ExtendedProtection.ServiceNameCollection CustomServiceNames { get { return default(System.Security.Authentication.ExtendedProtection.ServiceNameCollection); } }
        public static bool OSSupportsExtendedProtection { get { return default(bool); } }
        public System.Security.Authentication.ExtendedProtection.PolicyEnforcement PolicyEnforcement { get { return default(System.Security.Authentication.ExtendedProtection.PolicyEnforcement); } }
        public System.Security.Authentication.ExtendedProtection.ProtectionScenario ProtectionScenario { get { return default(System.Security.Authentication.ExtendedProtection.ProtectionScenario); } }
        public override string ToString() { return default(string); }
    }
    public enum PolicyEnforcement
    {
        Always = 2,
        Never = 0,
        WhenSupported = 1,
    }
    public enum ProtectionScenario
    {
        TransportSelected = 0,
        TrustedProxy = 1,
    }
    public partial class ServiceNameCollection
    {
        public ServiceNameCollection(System.Collections.ICollection items) { }
        public bool Contains(string searchServiceName) { return default(bool); }
        public System.Security.Authentication.ExtendedProtection.ServiceNameCollection Merge(System.Collections.IEnumerable serviceNames) { return default(System.Security.Authentication.ExtendedProtection.ServiceNameCollection); }
        public System.Security.Authentication.ExtendedProtection.ServiceNameCollection Merge(string serviceName) { return default(System.Security.Authentication.ExtendedProtection.ServiceNameCollection); }
    }
}

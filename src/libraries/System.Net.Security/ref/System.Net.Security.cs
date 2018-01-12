// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

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
        public bool LeaveInnerStreamOpen { get { throw null; } }
        protected System.IO.Stream InnerStream { get { throw null; } }
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
        public virtual void AuthenticateAsClient(System.Net.NetworkCredential credential, string targetName) { }
        public virtual void AuthenticateAsClient(System.Net.NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ChannelBinding binding, string targetName) { }
        public virtual void AuthenticateAsClient(System.Net.NetworkCredential credential, string targetName, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel allowedImpersonationLevel) { }
        public virtual void AuthenticateAsClient(System.Net.NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ChannelBinding binding, string targetName, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel allowedImpersonationLevel) { }
        public virtual void AuthenticateAsServer() { }
        public virtual void AuthenticateAsServer(System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy policy) { }
        public virtual void AuthenticateAsServer(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel requiredImpersonationLevel) { }
        public virtual void AuthenticateAsServer(NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy policy, ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel requiredImpersonationLevel) { }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync() { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(NetworkCredential credential, string targetName) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ChannelBinding binding, string targetName) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(NetworkCredential credential, string targetName, ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel allowedImpersonationLevel) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ChannelBinding binding, string targetName, ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel allowedImpersonationLevel) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync() { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy policy) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel requiredImpersonationLevel) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy policy, ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel requiredImpersonationLevel) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsClient(System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsClient(System.Net.NetworkCredential credential, string targetName, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsClient(System.Net.NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ChannelBinding binding, string targetName, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsClient(System.Net.NetworkCredential credential, string targetName, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel allowedImpersonationLevel, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsClient(System.Net.NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ChannelBinding binding, string targetName, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel allowedImpersonationLevel, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsServer(AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsServer(System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy policy, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual IAsyncResult BeginAuthenticateAsServer(System.Net.NetworkCredential credential, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel requiredImpersonationLevel, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual IAsyncResult BeginAuthenticateAsServer(System.Net.NetworkCredential credential, System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy policy, System.Net.Security.ProtectionLevel requiredProtectionLevel, System.Security.Principal.TokenImpersonationLevel requiredImpersonationLevel, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState) { throw null; }
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState) { throw null; }
        protected override void Dispose(bool disposing) { }
        public virtual void EndAuthenticateAsClient(System.IAsyncResult asyncResult) { }
        public virtual void EndAuthenticateAsServer(System.IAsyncResult asyncResult) { }
        public override int EndRead(IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(IAsyncResult asyncResult) { }
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
        EncryptAndSign = 2
    }
    public delegate bool RemoteCertificateValidationCallback(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors);
    public class SslServerAuthenticationOptions
    {
        public bool AllowRenegotiation { get { throw null;  } set { } }
        public X509Certificate ServerCertificate { get { throw null;  } set { } }
        public bool ClientCertificateRequired { get { throw null;  } set { } }
        public SslProtocols EnabledSslProtocols { get { throw null;  } set { } }
        public X509RevocationMode CertificateRevocationCheckMode { get { throw null;  } set { } }
        public List<SslApplicationProtocol> ApplicationProtocols { get { throw null; } set { } }
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get { throw null;  } set { } }
        public EncryptionPolicy EncryptionPolicy { get { throw null;  } set { } }
    }
    public partial class SslClientAuthenticationOptions
    {
        public bool AllowRenegotiation { get { throw null;  } set { } }
        public string TargetHost { get { throw null; } set { } }
        public X509CertificateCollection ClientCertificates { get { throw null; } set { } }
        public LocalCertificateSelectionCallback LocalCertificateSelectionCallback { get { throw null; } set { } }
        public SslProtocols EnabledSslProtocols { get { throw null; } set { } }
        public X509RevocationMode CertificateRevocationCheckMode { get { throw null; } set { } }
        public List<SslApplicationProtocol> ApplicationProtocols { get { throw null; } set { } }
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get { throw null; } set { } }
        public EncryptionPolicy EncryptionPolicy { get { throw null; } set { } }
    }
    public readonly partial struct SslApplicationProtocol : IEquatable<SslApplicationProtocol>
    {
        private readonly object _dummy;

        public static readonly SslApplicationProtocol Http2;
        public static readonly SslApplicationProtocol Http11;

        public SslApplicationProtocol(byte[] protocol) { throw null; }
        public SslApplicationProtocol(string protocol) { throw null; }

        public ReadOnlyMemory<byte> Protocol { get { throw null; } }

        public bool Equals(SslApplicationProtocol other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
        public static bool operator ==(SslApplicationProtocol left, SslApplicationProtocol right) { throw null; }
        public static bool operator !=(SslApplicationProtocol left, SslApplicationProtocol right) { throw null; }
    }
    public partial class SslStream : AuthenticatedStream
    {
        public SslStream(System.IO.Stream innerStream) : base(innerStream, false) { }
        public SslStream(System.IO.Stream innerStream, bool leaveInnerStreamOpen) : base(innerStream, leaveInnerStreamOpen) { }
        public SslStream(System.IO.Stream innerStream, bool leaveInnerStreamOpen, System.Net.Security.RemoteCertificateValidationCallback userCertificateValidationCallback) : base(innerStream, leaveInnerStreamOpen) { }
        public SslStream(System.IO.Stream innerStream, bool leaveInnerStreamOpen, System.Net.Security.RemoteCertificateValidationCallback userCertificateValidationCallback, System.Net.Security.LocalCertificateSelectionCallback userCertificateSelectionCallback) : base(innerStream, leaveInnerStreamOpen) { }
        public SslStream(System.IO.Stream innerStream, bool leaveInnerStreamOpen, System.Net.Security.RemoteCertificateValidationCallback userCertificateValidationCallback, System.Net.Security.LocalCertificateSelectionCallback userCertificateSelectionCallback, System.Net.Security.EncryptionPolicy encryptionPolicy) : base(innerStream, leaveInnerStreamOpen) { }
        public SslApplicationProtocol NegotiatedApplicationProtocol { get { throw null; } }
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
        public override long Position { get { throw null; } set { } }
        public override int ReadTimeout { get { throw null; } set { } }
        public virtual System.Security.Cryptography.X509Certificates.X509Certificate RemoteCertificate { get { throw null; } }
        public virtual System.Security.Authentication.SslProtocols SslProtocol { get { throw null; } }
        public System.Net.TransportContext TransportContext { get { throw null; } }
        public override int WriteTimeout { get { throw null; } set { } }
        public virtual void AuthenticateAsClient(string targetHost) { }
        public virtual void AuthenticateAsClient(string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates, System.Security.Authentication.SslProtocols enabledSslProtocols, bool checkCertificateRevocation) { }
        public virtual void AuthenticateAsClient(string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates, bool checkCertificateRevocation) { }
        public virtual void AuthenticateAsServer(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate) { }
        public virtual void AuthenticateAsServer(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate, bool clientCertificateRequired, System.Security.Authentication.SslProtocols enabledSslProtocols, bool checkCertificateRevocation) { }
        public virtual void AuthenticateAsServer(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation) { }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(string targetHost) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates, System.Security.Authentication.SslProtocols enabledSslProtocols, bool checkCertificateRevocation) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsClientAsync(string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates, bool checkCertificateRevocation) { throw null; }
        public Task AuthenticateAsClientAsync(SslClientAuthenticationOptions sslClientAuthenticationOptions, CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate, bool clientCertificateRequired, System.Security.Authentication.SslProtocols enabledSslProtocols, bool checkCertificateRevocation) { throw null; }
        public virtual System.Threading.Tasks.Task AuthenticateAsServerAsync(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation) { throw null; }
        public Task AuthenticateAsServerAsync(SslServerAuthenticationOptions sslClientAuthenticationOptions, CancellationToken cancellationToken) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsClient(string targetHost, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsClient(string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates, System.Security.Authentication.SslProtocols enabledSslProtocols, bool checkCertificateRevocation, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsClient(string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates, bool checkCertificateRevocation, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsServer(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsServer(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate, bool clientCertificateRequired, System.Security.Authentication.SslProtocols enabledSslProtocols, bool checkCertificateRevocation, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public virtual System.IAsyncResult BeginAuthenticateAsServer(System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation, System.AsyncCallback asyncCallback, object asyncState) { throw null; }
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState) { throw null; }
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState) { throw null; }
        protected override void Dispose(bool disposing) { }
        public virtual void EndAuthenticateAsClient(IAsyncResult asyncResult) { }
        public virtual void EndAuthenticateAsServer(IAsyncResult asyncResult) { }
        public override int EndRead(IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public virtual System.Threading.Tasks.Task ShutdownAsync() { throw null; }
        public void Write(byte[] buffer) { }
        public override void Write(byte[] buffer, int offset, int count) { }
    }
}
namespace System.Security.Authentication
{
    public partial class AuthenticationException : System.SystemException
    {
        public AuthenticationException() { }
        public AuthenticationException(string message) { }
        public AuthenticationException(string message, System.Exception innerException) { }
        protected AuthenticationException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public partial class InvalidCredentialException : System.Security.Authentication.AuthenticationException
    {
        public InvalidCredentialException() { }
        public InvalidCredentialException(string message) { }
        public InvalidCredentialException(string message, System.Exception innerException) { }
        protected InvalidCredentialException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
}
namespace System.Security.Authentication.ExtendedProtection
{
    public partial class ExtendedProtectionPolicy : System.Runtime.Serialization.ISerializable
    {
        public ExtendedProtectionPolicy(System.Security.Authentication.ExtendedProtection.PolicyEnforcement policyEnforcement) { }
        public ExtendedProtectionPolicy(System.Security.Authentication.ExtendedProtection.PolicyEnforcement policyEnforcement, System.Security.Authentication.ExtendedProtection.ChannelBinding customChannelBinding) { }
        public ExtendedProtectionPolicy(System.Security.Authentication.ExtendedProtection.PolicyEnforcement policyEnforcement, System.Security.Authentication.ExtendedProtection.ProtectionScenario protectionScenario, System.Collections.ICollection customServiceNames) { }
        public ExtendedProtectionPolicy(System.Security.Authentication.ExtendedProtection.PolicyEnforcement policyEnforcement, System.Security.Authentication.ExtendedProtection.ProtectionScenario protectionScenario, System.Security.Authentication.ExtendedProtection.ServiceNameCollection customServiceNames) { }
        protected ExtendedProtectionPolicy(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
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
        Always = 2,
        Never = 0,
        WhenSupported = 1,
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

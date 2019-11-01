// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.DirectoryServices.Protocols
{
    public partial class AddRequest : System.DirectoryServices.Protocols.DirectoryRequest
    {
        public AddRequest() { }
        public AddRequest(string distinguishedName, params System.DirectoryServices.Protocols.DirectoryAttribute[] attributes) { }
        public AddRequest(string distinguishedName, string objectClass) { }
        public System.DirectoryServices.Protocols.DirectoryAttributeCollection Attributes { get { throw null; } }
        public string DistinguishedName { get { throw null; } set { } }
    }
    public partial class AddResponse : System.DirectoryServices.Protocols.DirectoryResponse
    {
        internal AddResponse() { }
    }
    public partial class AsqRequestControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public AsqRequestControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public AsqRequestControl(string attributeName) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public string AttributeName { get { throw null; } set { } }
        public override byte[] GetValue() { throw null; }
    }
    public partial class AsqResponseControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        internal AsqResponseControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public System.DirectoryServices.Protocols.ResultCode Result { get { throw null; } }
    }
    public enum AuthType
    {
        Anonymous = 0,
        Basic = 1,
        Negotiate = 2,
        Ntlm = 3,
        Digest = 4,
        Sicily = 5,
        Dpa = 6,
        Msn = 7,
        External = 8,
        Kerberos = 9,
    }
    public partial class BerConversionException : System.DirectoryServices.Protocols.DirectoryException
    {
        public BerConversionException() { }
        protected BerConversionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public BerConversionException(string message) { }
        public BerConversionException(string message, System.Exception inner) { }
    }
    public static partial class BerConverter
    {
        public static object[] Decode(string format, byte[] value) { throw null; }
        public static byte[] Encode(string format, params object[] value) { throw null; }
    }
    public partial class CompareRequest : System.DirectoryServices.Protocols.DirectoryRequest
    {
        public CompareRequest() { }
        public CompareRequest(string distinguishedName, System.DirectoryServices.Protocols.DirectoryAttribute assertion) { }
        public CompareRequest(string distinguishedName, string attributeName, byte[] value) { }
        public CompareRequest(string distinguishedName, string attributeName, string value) { }
        public CompareRequest(string distinguishedName, string attributeName, System.Uri value) { }
        public System.DirectoryServices.Protocols.DirectoryAttribute Assertion { get { throw null; } }
        public string DistinguishedName { get { throw null; } set { } }
    }
    public partial class CompareResponse : System.DirectoryServices.Protocols.DirectoryResponse
    {
        internal CompareResponse() { }
    }
    public partial class CrossDomainMoveControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public CrossDomainMoveControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public CrossDomainMoveControl(string targetDomainController) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public string TargetDomainController { get { throw null; } set { } }
        public override byte[] GetValue() { throw null; }
    }
    public partial class DeleteRequest : System.DirectoryServices.Protocols.DirectoryRequest
    {
        public DeleteRequest() { }
        public DeleteRequest(string distinguishedName) { }
        public string DistinguishedName { get { throw null; } set { } }
    }
    public partial class DeleteResponse : System.DirectoryServices.Protocols.DirectoryResponse
    {
        internal DeleteResponse() { }
    }
    public enum DereferenceAlias
    {
        Never = 0,
        InSearching = 1,
        FindingBaseObject = 2,
        Always = 3,
    }
    public delegate void DereferenceConnectionCallback(System.DirectoryServices.Protocols.LdapConnection primaryConnection, System.DirectoryServices.Protocols.LdapConnection connectionToDereference);
    public partial class DirectoryAttribute : System.Collections.CollectionBase
    {
        public DirectoryAttribute() { }
        public DirectoryAttribute(string name, byte[] value) { }
        public DirectoryAttribute(string name, params object[] values) { }
        public DirectoryAttribute(string name, string value) { }
        public DirectoryAttribute(string name, System.Uri value) { }
        public object this[int index] { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
        public int Add(byte[] value) { throw null; }
        public int Add(string value) { throw null; }
        public int Add(System.Uri value) { throw null; }
        public void AddRange(object[] values) { }
        public bool Contains(object value) { throw null; }
        public void CopyTo(object[] array, int index) { }
        public object[] GetValues(System.Type valuesType) { throw null; }
        public int IndexOf(object value) { throw null; }
        public void Insert(int index, byte[] value) { }
        public void Insert(int index, string value) { }
        public void Insert(int index, System.Uri value) { }
        protected override void OnValidate(object value) { }
        public void Remove(object value) { }
    }
    public partial class DirectoryAttributeCollection : System.Collections.CollectionBase
    {
        public DirectoryAttributeCollection() { }
        public System.DirectoryServices.Protocols.DirectoryAttribute this[int index] { get { throw null; } set { } }
        public int Add(System.DirectoryServices.Protocols.DirectoryAttribute attribute) { throw null; }
        public void AddRange(System.DirectoryServices.Protocols.DirectoryAttributeCollection attributeCollection) { }
        public void AddRange(System.DirectoryServices.Protocols.DirectoryAttribute[] attributes) { }
        public bool Contains(System.DirectoryServices.Protocols.DirectoryAttribute value) { throw null; }
        public void CopyTo(System.DirectoryServices.Protocols.DirectoryAttribute[] array, int index) { }
        public int IndexOf(System.DirectoryServices.Protocols.DirectoryAttribute value) { throw null; }
        public void Insert(int index, System.DirectoryServices.Protocols.DirectoryAttribute value) { }
        protected override void OnValidate(object value) { }
        public void Remove(System.DirectoryServices.Protocols.DirectoryAttribute value) { }
    }
    public partial class DirectoryAttributeModification : System.DirectoryServices.Protocols.DirectoryAttribute
    {
        public DirectoryAttributeModification() { }
        public System.DirectoryServices.Protocols.DirectoryAttributeOperation Operation { get { throw null; } set { } }
    }
    public partial class DirectoryAttributeModificationCollection : System.Collections.CollectionBase
    {
        public DirectoryAttributeModificationCollection() { }
        public System.DirectoryServices.Protocols.DirectoryAttributeModification this[int index] { get { throw null; } set { } }
        public int Add(System.DirectoryServices.Protocols.DirectoryAttributeModification attribute) { throw null; }
        public void AddRange(System.DirectoryServices.Protocols.DirectoryAttributeModificationCollection attributeCollection) { }
        public void AddRange(System.DirectoryServices.Protocols.DirectoryAttributeModification[] attributes) { }
        public bool Contains(System.DirectoryServices.Protocols.DirectoryAttributeModification value) { throw null; }
        public void CopyTo(System.DirectoryServices.Protocols.DirectoryAttributeModification[] array, int index) { }
        public int IndexOf(System.DirectoryServices.Protocols.DirectoryAttributeModification value) { throw null; }
        public void Insert(int index, System.DirectoryServices.Protocols.DirectoryAttributeModification value) { }
        protected override void OnValidate(object value) { }
        public void Remove(System.DirectoryServices.Protocols.DirectoryAttributeModification value) { }
    }
    public enum DirectoryAttributeOperation
    {
        Add = 0,
        Delete = 1,
        Replace = 2,
    }
    public abstract partial class DirectoryConnection
    {
        protected DirectoryConnection() { }
        public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates { get { throw null; } }
        public virtual System.Net.NetworkCredential Credential { set { } }
        public virtual System.DirectoryServices.Protocols.DirectoryIdentifier Directory { get { throw null; } }
        public virtual System.TimeSpan Timeout { get { throw null; } set { } }
        public abstract System.DirectoryServices.Protocols.DirectoryResponse SendRequest(System.DirectoryServices.Protocols.DirectoryRequest request);
    }
    public partial class DirectoryControl
    {
        public DirectoryControl(string type, byte[] value, bool isCritical, bool serverSide) { }
        public bool IsCritical { get { throw null; } set { } }
        public bool ServerSide { get { throw null; } set { } }
        public string Type { get { throw null; } }
        public virtual byte[] GetValue() { throw null; }
    }
    public partial class DirectoryControlCollection : System.Collections.CollectionBase
    {
        public DirectoryControlCollection() { }
        public System.DirectoryServices.Protocols.DirectoryControl this[int index] { get { throw null; } set { } }
        public int Add(System.DirectoryServices.Protocols.DirectoryControl control) { throw null; }
        public void AddRange(System.DirectoryServices.Protocols.DirectoryControlCollection controlCollection) { }
        public void AddRange(System.DirectoryServices.Protocols.DirectoryControl[] controls) { }
        public bool Contains(System.DirectoryServices.Protocols.DirectoryControl value) { throw null; }
        public void CopyTo(System.DirectoryServices.Protocols.DirectoryControl[] array, int index) { }
        public int IndexOf(System.DirectoryServices.Protocols.DirectoryControl value) { throw null; }
        public void Insert(int index, System.DirectoryServices.Protocols.DirectoryControl value) { }
        protected override void OnValidate(object value) { }
        public void Remove(System.DirectoryServices.Protocols.DirectoryControl value) { }
    }
    public partial class DirectoryException : System.Exception
    {
        public DirectoryException() { }
        protected DirectoryException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public DirectoryException(string message) { }
        public DirectoryException(string message, System.Exception inner) { }
    }
    public abstract partial class DirectoryIdentifier
    {
        protected DirectoryIdentifier() { }
    }
    public partial class DirectoryNotificationControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public DirectoryNotificationControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
    }
    public abstract partial class DirectoryOperation
    {
        protected DirectoryOperation() { }
    }
    public partial class DirectoryOperationException : System.DirectoryServices.Protocols.DirectoryException, System.Runtime.Serialization.ISerializable
    {
        public DirectoryOperationException() { }
        public DirectoryOperationException(System.DirectoryServices.Protocols.DirectoryResponse response) { }
        public DirectoryOperationException(System.DirectoryServices.Protocols.DirectoryResponse response, string message) { }
        public DirectoryOperationException(System.DirectoryServices.Protocols.DirectoryResponse response, string message, System.Exception inner) { }
        protected DirectoryOperationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public DirectoryOperationException(string message) { }
        public DirectoryOperationException(string message, System.Exception inner) { }
        public System.DirectoryServices.Protocols.DirectoryResponse Response { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public abstract partial class DirectoryRequest : System.DirectoryServices.Protocols.DirectoryOperation
    {
        internal DirectoryRequest() { }
        public System.DirectoryServices.Protocols.DirectoryControlCollection Controls { get { throw null; } }
        public string RequestId { get { throw null; } set { } }
    }
    public abstract partial class DirectoryResponse : System.DirectoryServices.Protocols.DirectoryOperation
    {
        internal DirectoryResponse() { }
        public virtual System.DirectoryServices.Protocols.DirectoryControl[] Controls { get { throw null; } }
        public virtual string ErrorMessage { get { throw null; } }
        public virtual string MatchedDN { get { throw null; } }
        public virtual System.Uri[] Referral { get { throw null; } }
        public string RequestId { get { throw null; } }
        public virtual System.DirectoryServices.Protocols.ResultCode ResultCode { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum DirectorySynchronizationOptions : long
    {
        None = (long)0,
        ObjectSecurity = (long)1,
        ParentsFirst = (long)2048,
        PublicDataOnly = (long)8192,
        IncrementalValues = (long)2147483648,
    }
    public partial class DirSyncRequestControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public DirSyncRequestControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public DirSyncRequestControl(byte[] cookie) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public DirSyncRequestControl(byte[] cookie, System.DirectoryServices.Protocols.DirectorySynchronizationOptions option) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public DirSyncRequestControl(byte[] cookie, System.DirectoryServices.Protocols.DirectorySynchronizationOptions option, int attributeCount) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public int AttributeCount { get { throw null; } set { } }
        public byte[] Cookie { get { throw null; } set { } }
        public System.DirectoryServices.Protocols.DirectorySynchronizationOptions Option { get { throw null; } set { } }
        public override byte[] GetValue() { throw null; }
    }
    public partial class DirSyncResponseControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        internal DirSyncResponseControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public byte[] Cookie { get { throw null; } }
        public bool MoreData { get { throw null; } }
        public int ResultSize { get { throw null; } }
    }
    public partial class DomainScopeControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public DomainScopeControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
    }
    public partial class DsmlAuthRequest : System.DirectoryServices.Protocols.DirectoryRequest
    {
        public DsmlAuthRequest() { }
        public DsmlAuthRequest(string principal) { }
        public string Principal { get { throw null; } set { } }
    }
    public partial class ExtendedDNControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public ExtendedDNControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public ExtendedDNControl(System.DirectoryServices.Protocols.ExtendedDNFlag flag) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public System.DirectoryServices.Protocols.ExtendedDNFlag Flag { get { throw null; } set { } }
        public override byte[] GetValue() { throw null; }
    }
    public enum ExtendedDNFlag
    {
        HexString = 0,
        StandardString = 1,
    }
    public partial class ExtendedRequest : System.DirectoryServices.Protocols.DirectoryRequest
    {
        public ExtendedRequest() { }
        public ExtendedRequest(string requestName) { }
        public ExtendedRequest(string requestName, byte[] requestValue) { }
        public string RequestName { get { throw null; } set { } }
        public byte[] RequestValue { get { throw null; } set { } }
    }
    public partial class ExtendedResponse : System.DirectoryServices.Protocols.DirectoryResponse
    {
        internal ExtendedResponse() { }
        public string ResponseName { get { throw null; } }
        public byte[] ResponseValue { get { throw null; } }
    }
    public partial class LazyCommitControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public LazyCommitControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
    }
    public partial class LdapConnection : System.DirectoryServices.Protocols.DirectoryConnection, System.IDisposable
    {
        public LdapConnection(System.DirectoryServices.Protocols.LdapDirectoryIdentifier identifier) { }
        public LdapConnection(System.DirectoryServices.Protocols.LdapDirectoryIdentifier identifier, System.Net.NetworkCredential credential) { }
        public LdapConnection(System.DirectoryServices.Protocols.LdapDirectoryIdentifier identifier, System.Net.NetworkCredential credential, System.DirectoryServices.Protocols.AuthType authType) { }
        public LdapConnection(string server) { }
        public System.DirectoryServices.Protocols.AuthType AuthType { get { throw null; } set { } }
        public bool AutoBind { get { throw null; } set { } }
        public override System.Net.NetworkCredential Credential { set { } }
        public System.DirectoryServices.Protocols.LdapSessionOptions SessionOptions { get { throw null; } }
        public override System.TimeSpan Timeout { get { throw null; } set { } }
        public void Abort(System.IAsyncResult asyncResult) { }
        public System.IAsyncResult BeginSendRequest(System.DirectoryServices.Protocols.DirectoryRequest request, System.DirectoryServices.Protocols.PartialResultProcessing partialMode, System.AsyncCallback callback, object state) { throw null; }
        public System.IAsyncResult BeginSendRequest(System.DirectoryServices.Protocols.DirectoryRequest request, System.TimeSpan requestTimeout, System.DirectoryServices.Protocols.PartialResultProcessing partialMode, System.AsyncCallback callback, object state) { throw null; }
        public void Bind() { }
        public void Bind(System.Net.NetworkCredential newCredential) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.DirectoryServices.Protocols.DirectoryResponse EndSendRequest(System.IAsyncResult asyncResult) { throw null; }
        ~LdapConnection() { }
        public System.DirectoryServices.Protocols.PartialResultsCollection GetPartialResults(System.IAsyncResult asyncResult) { throw null; }
        public override System.DirectoryServices.Protocols.DirectoryResponse SendRequest(System.DirectoryServices.Protocols.DirectoryRequest request) { throw null; }
        public System.DirectoryServices.Protocols.DirectoryResponse SendRequest(System.DirectoryServices.Protocols.DirectoryRequest request, System.TimeSpan requestTimeout) { throw null; }
    }
    public partial class LdapDirectoryIdentifier : System.DirectoryServices.Protocols.DirectoryIdentifier
    {
        public LdapDirectoryIdentifier(string server) { }
        public LdapDirectoryIdentifier(string server, bool fullyQualifiedDnsHostName, bool connectionless) { }
        public LdapDirectoryIdentifier(string server, int portNumber) { }
        public LdapDirectoryIdentifier(string server, int portNumber, bool fullyQualifiedDnsHostName, bool connectionless) { }
        public LdapDirectoryIdentifier(string[] servers, bool fullyQualifiedDnsHostName, bool connectionless) { }
        public LdapDirectoryIdentifier(string[] servers, int portNumber, bool fullyQualifiedDnsHostName, bool connectionless) { }
        public bool Connectionless { get { throw null; } }
        public bool FullyQualifiedDnsHostName { get { throw null; } }
        public int PortNumber { get { throw null; } }
        public string[] Servers { get { throw null; } }
    }
    public partial class LdapException : System.DirectoryServices.Protocols.DirectoryException, System.Runtime.Serialization.ISerializable
    {
        public LdapException() { }
        public LdapException(int errorCode) { }
        public LdapException(int errorCode, string message) { }
        public LdapException(int errorCode, string message, System.Exception inner) { }
        public LdapException(int errorCode, string message, string serverErrorMessage) { }
        protected LdapException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public LdapException(string message) { }
        public LdapException(string message, System.Exception inner) { }
        public int ErrorCode { get { throw null; } }
        public System.DirectoryServices.Protocols.PartialResultsCollection PartialResults { get { throw null; } }
        public string ServerErrorMessage { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public partial class LdapSessionOptions
    {
        internal LdapSessionOptions() { }
        public bool AutoReconnect { get { throw null; } set { } }
        public string DomainName { get { throw null; } set { } }
        public string HostName { get { throw null; } set { } }
        public bool HostReachable { get { throw null; } }
        public System.DirectoryServices.Protocols.LocatorFlags LocatorFlag { get { throw null; } set { } }
        public System.TimeSpan PingKeepAliveTimeout { get { throw null; } set { } }
        public int PingLimit { get { throw null; } set { } }
        public System.TimeSpan PingWaitTimeout { get { throw null; } set { } }
        public int ProtocolVersion { get { throw null; } set { } }
        public System.DirectoryServices.Protocols.QueryClientCertificateCallback QueryClientCertificate { get { throw null; } set { } }
        public System.DirectoryServices.Protocols.ReferralCallback ReferralCallback { get { throw null; } set { } }
        public System.DirectoryServices.Protocols.ReferralChasingOptions ReferralChasing { get { throw null; } set { } }
        public int ReferralHopLimit { get { throw null; } set { } }
        public bool RootDseCache { get { throw null; } set { } }
        public string SaslMethod { get { throw null; } set { } }
        public bool Sealing { get { throw null; } set { } }
        public bool SecureSocketLayer { get { throw null; } set { } }
        public object SecurityContext { get { throw null; } }
        public System.TimeSpan SendTimeout { get { throw null; } set { } }
        public bool Signing { get { throw null; } set { } }
        public System.DirectoryServices.Protocols.SecurityPackageContextConnectionInformation SslInformation { get { throw null; } }
        public int SspiFlag { get { throw null; } set { } }
        public bool TcpKeepAlive { get { throw null; } set { } }
        public System.DirectoryServices.Protocols.VerifyServerCertificateCallback VerifyServerCertificate { get { throw null; } set { } }
        public void FastConcurrentBind() { }
        public void StartTransportLayerSecurity(System.DirectoryServices.Protocols.DirectoryControlCollection controls) { }
        public void StopTransportLayerSecurity() { }
    }
    [System.FlagsAttribute]
    public enum LocatorFlags : long
    {
        None = (long)0,
        ForceRediscovery = (long)1,
        DirectoryServicesRequired = (long)16,
        DirectoryServicesPreferred = (long)32,
        GCRequired = (long)64,
        PdcRequired = (long)128,
        IPRequired = (long)512,
        KdcRequired = (long)1024,
        TimeServerRequired = (long)2048,
        WriteableRequired = (long)4096,
        GoodTimeServerPreferred = (long)8192,
        AvoidSelf = (long)16384,
        OnlyLdapNeeded = (long)32768,
        IsFlatName = (long)65536,
        IsDnsName = (long)131072,
        ReturnDnsName = (long)1073741824,
        ReturnFlatName = (long)2147483648,
    }
    public partial class ModifyDNRequest : System.DirectoryServices.Protocols.DirectoryRequest
    {
        public ModifyDNRequest() { }
        public ModifyDNRequest(string distinguishedName, string newParentDistinguishedName, string newName) { }
        public bool DeleteOldRdn { get { throw null; } set { } }
        public string DistinguishedName { get { throw null; } set { } }
        public string NewName { get { throw null; } set { } }
        public string NewParentDistinguishedName { get { throw null; } set { } }
    }
    public partial class ModifyDNResponse : System.DirectoryServices.Protocols.DirectoryResponse
    {
        internal ModifyDNResponse() { }
    }
    public partial class ModifyRequest : System.DirectoryServices.Protocols.DirectoryRequest
    {
        public ModifyRequest() { }
        public ModifyRequest(string distinguishedName, params System.DirectoryServices.Protocols.DirectoryAttributeModification[] modifications) { }
        public ModifyRequest(string distinguishedName, System.DirectoryServices.Protocols.DirectoryAttributeOperation operation, string attributeName, params object[] values) { }
        public string DistinguishedName { get { throw null; } set { } }
        public System.DirectoryServices.Protocols.DirectoryAttributeModificationCollection Modifications { get { throw null; } }
    }
    public partial class ModifyResponse : System.DirectoryServices.Protocols.DirectoryResponse
    {
        internal ModifyResponse() { }
    }
    public delegate bool NotifyOfNewConnectionCallback(System.DirectoryServices.Protocols.LdapConnection primaryConnection, System.DirectoryServices.Protocols.LdapConnection referralFromConnection, string newDistinguishedName, System.DirectoryServices.Protocols.LdapDirectoryIdentifier identifier, System.DirectoryServices.Protocols.LdapConnection newConnection, System.Net.NetworkCredential credential, long currentUserToken, int errorCodeFromBind);
    public partial class PageResultRequestControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public PageResultRequestControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public PageResultRequestControl(byte[] cookie) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public PageResultRequestControl(int pageSize) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public byte[] Cookie { get { throw null; } set { } }
        public int PageSize { get { throw null; } set { } }
        public override byte[] GetValue() { throw null; }
    }
    public partial class PageResultResponseControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        internal PageResultResponseControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public byte[] Cookie { get { throw null; } }
        public int TotalCount { get { throw null; } }
    }
    public enum PartialResultProcessing
    {
        NoPartialResultSupport = 0,
        ReturnPartialResults = 1,
        ReturnPartialResultsAndNotifyCallback = 2,
    }
    public partial class PartialResultsCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal PartialResultsCollection() { }
        public object this[int index] { get { throw null; } }
        public bool Contains(object value) { throw null; }
        public void CopyTo(object[] values, int index) { }
        public int IndexOf(object value) { throw null; }
    }
    public partial class PermissiveModifyControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public PermissiveModifyControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
    }
    public delegate System.Security.Cryptography.X509Certificates.X509Certificate QueryClientCertificateCallback(System.DirectoryServices.Protocols.LdapConnection connection, byte[][] trustedCAs);
    public delegate System.DirectoryServices.Protocols.LdapConnection QueryForConnectionCallback(System.DirectoryServices.Protocols.LdapConnection primaryConnection, System.DirectoryServices.Protocols.LdapConnection referralFromConnection, string newDistinguishedName, System.DirectoryServices.Protocols.LdapDirectoryIdentifier identifier, System.Net.NetworkCredential credential, long currentUserToken);
    public partial class QuotaControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public QuotaControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public QuotaControl(System.Security.Principal.SecurityIdentifier querySid) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public System.Security.Principal.SecurityIdentifier QuerySid { get { throw null; } set { } }
        public override byte[] GetValue() { throw null; }
    }
    public sealed partial class ReferralCallback
    {
        public ReferralCallback() { }
        public System.DirectoryServices.Protocols.DereferenceConnectionCallback DereferenceConnection { get { throw null; } set { } }
        public System.DirectoryServices.Protocols.NotifyOfNewConnectionCallback NotifyNewConnection { get { throw null; } set { } }
        public System.DirectoryServices.Protocols.QueryForConnectionCallback QueryForConnection { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum ReferralChasingOptions
    {
        None = 0,
        Subordinate = 32,
        External = 64,
        All = 96,
    }
    public enum ResultCode
    {
        Success = 0,
        OperationsError = 1,
        ProtocolError = 2,
        TimeLimitExceeded = 3,
        SizeLimitExceeded = 4,
        CompareFalse = 5,
        CompareTrue = 6,
        AuthMethodNotSupported = 7,
        StrongAuthRequired = 8,
        ReferralV2 = 9,
        Referral = 10,
        AdminLimitExceeded = 11,
        UnavailableCriticalExtension = 12,
        ConfidentialityRequired = 13,
        SaslBindInProgress = 14,
        NoSuchAttribute = 16,
        UndefinedAttributeType = 17,
        InappropriateMatching = 18,
        ConstraintViolation = 19,
        AttributeOrValueExists = 20,
        InvalidAttributeSyntax = 21,
        NoSuchObject = 32,
        AliasProblem = 33,
        InvalidDNSyntax = 34,
        AliasDereferencingProblem = 36,
        InappropriateAuthentication = 48,
        InsufficientAccessRights = 50,
        Busy = 51,
        Unavailable = 52,
        UnwillingToPerform = 53,
        LoopDetect = 54,
        SortControlMissing = 60,
        OffsetRangeError = 61,
        NamingViolation = 64,
        ObjectClassViolation = 65,
        NotAllowedOnNonLeaf = 66,
        NotAllowedOnRdn = 67,
        EntryAlreadyExists = 68,
        ObjectClassModificationsProhibited = 69,
        ResultsTooLarge = 70,
        AffectsMultipleDsas = 71,
        VirtualListViewError = 76,
        Other = 80,
    }
    public enum SearchOption
    {
        DomainScope = 1,
        PhantomRoot = 2,
    }
    public partial class SearchOptionsControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public SearchOptionsControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public SearchOptionsControl(System.DirectoryServices.Protocols.SearchOption flags) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public System.DirectoryServices.Protocols.SearchOption SearchOption { get { throw null; } set { } }
        public override byte[] GetValue() { throw null; }
    }
    public partial class SearchRequest : System.DirectoryServices.Protocols.DirectoryRequest
    {
        public SearchRequest() { }
        public SearchRequest(string distinguishedName, string ldapFilter, System.DirectoryServices.Protocols.SearchScope searchScope, params string[] attributeList) { }
        public System.DirectoryServices.Protocols.DereferenceAlias Aliases { get { throw null; } set { } }
        public System.Collections.Specialized.StringCollection Attributes { get { throw null; } }
        public string DistinguishedName { get { throw null; } set { } }
        public object Filter { get { throw null; } set { } }
        public System.DirectoryServices.Protocols.SearchScope Scope { get { throw null; } set { } }
        public int SizeLimit { get { throw null; } set { } }
        public System.TimeSpan TimeLimit { get { throw null; } set { } }
        public bool TypesOnly { get { throw null; } set { } }
    }
    public partial class SearchResponse : System.DirectoryServices.Protocols.DirectoryResponse
    {
        internal SearchResponse() { }
        public override System.DirectoryServices.Protocols.DirectoryControl[] Controls { get { throw null; } }
        public System.DirectoryServices.Protocols.SearchResultEntryCollection Entries { get { throw null; } }
        public override string ErrorMessage { get { throw null; } }
        public override string MatchedDN { get { throw null; } }
        public System.DirectoryServices.Protocols.SearchResultReferenceCollection References { get { throw null; } }
        public override System.Uri[] Referral { get { throw null; } }
        public override System.DirectoryServices.Protocols.ResultCode ResultCode { get { throw null; } }
    }
    public partial class SearchResultAttributeCollection : System.Collections.DictionaryBase
    {
        internal SearchResultAttributeCollection() { }
        public System.Collections.ICollection AttributeNames { get { throw null; } }
        public System.DirectoryServices.Protocols.DirectoryAttribute this[string attributeName] { get { throw null; } }
        public System.Collections.ICollection Values { get { throw null; } }
        public bool Contains(string attributeName) { throw null; }
        public void CopyTo(System.DirectoryServices.Protocols.DirectoryAttribute[] array, int index) { }
    }
    public partial class SearchResultEntry
    {
        internal SearchResultEntry() { }
        public System.DirectoryServices.Protocols.SearchResultAttributeCollection Attributes { get { throw null; } }
        public System.DirectoryServices.Protocols.DirectoryControl[] Controls { get { throw null; } }
        public string DistinguishedName { get { throw null; } }
    }
    public partial class SearchResultEntryCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal SearchResultEntryCollection() { }
        public System.DirectoryServices.Protocols.SearchResultEntry this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.Protocols.SearchResultEntry value) { throw null; }
        public void CopyTo(System.DirectoryServices.Protocols.SearchResultEntry[] values, int index) { }
        public int IndexOf(System.DirectoryServices.Protocols.SearchResultEntry value) { throw null; }
    }
    public partial class SearchResultReference
    {
        internal SearchResultReference() { }
        public System.DirectoryServices.Protocols.DirectoryControl[] Controls { get { throw null; } }
        public System.Uri[] Reference { get { throw null; } }
    }
    public partial class SearchResultReferenceCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal SearchResultReferenceCollection() { }
        public System.DirectoryServices.Protocols.SearchResultReference this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.Protocols.SearchResultReference value) { throw null; }
        public void CopyTo(System.DirectoryServices.Protocols.SearchResultReference[] values, int index) { }
        public int IndexOf(System.DirectoryServices.Protocols.SearchResultReference value) { throw null; }
    }
    public enum SearchScope
    {
        Base = 0,
        OneLevel = 1,
        Subtree = 2,
    }
    public partial class SecurityDescriptorFlagControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public SecurityDescriptorFlagControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public SecurityDescriptorFlagControl(System.DirectoryServices.Protocols.SecurityMasks masks) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public System.DirectoryServices.Protocols.SecurityMasks SecurityMasks { get { throw null; } set { } }
        public override byte[] GetValue() { throw null; }
    }
    [System.FlagsAttribute]
    public enum SecurityMasks
    {
        None = 0,
        Owner = 1,
        Group = 2,
        Dacl = 4,
        Sacl = 8,
    }
    public partial class SecurityPackageContextConnectionInformation
    {
        internal SecurityPackageContextConnectionInformation() { }
        public System.Security.Authentication.CipherAlgorithmType AlgorithmIdentifier { get { throw null; } }
        public int CipherStrength { get { throw null; } }
        public int ExchangeStrength { get { throw null; } }
        public System.Security.Authentication.HashAlgorithmType Hash { get { throw null; } }
        public int HashStrength { get { throw null; } }
        public int KeyExchangeAlgorithm { get { throw null; } }
        public System.DirectoryServices.Protocols.SecurityProtocol Protocol { get { throw null; } }
    }
    public enum SecurityProtocol
    {
        Pct1Server = 1,
        Pct1Client = 2,
        Ssl2Server = 4,
        Ssl2Client = 8,
        Ssl3Server = 16,
        Ssl3Client = 32,
        Tls1Server = 64,
        Tls1Client = 128,
    }
    public partial class ShowDeletedControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public ShowDeletedControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
    }
    public partial class SortKey
    {
        public SortKey() { }
        public SortKey(string attributeName, string matchingRule, bool reverseOrder) { }
        public string AttributeName { get { throw null; } set { } }
        public string MatchingRule { get { throw null; } set { } }
        public bool ReverseOrder { get { throw null; } set { } }
    }
    public partial class SortRequestControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public SortRequestControl(params System.DirectoryServices.Protocols.SortKey[] sortKeys) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public SortRequestControl(string attributeName, bool reverseOrder) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public SortRequestControl(string attributeName, string matchingRule, bool reverseOrder) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public System.DirectoryServices.Protocols.SortKey[] SortKeys { get { throw null; } set { } }
        public override byte[] GetValue() { throw null; }
    }
    public partial class SortResponseControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        internal SortResponseControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public string AttributeName { get { throw null; } }
        public System.DirectoryServices.Protocols.ResultCode Result { get { throw null; } }
    }
    public partial class TlsOperationException : System.DirectoryServices.Protocols.DirectoryOperationException
    {
        public TlsOperationException() { }
        public TlsOperationException(System.DirectoryServices.Protocols.DirectoryResponse response) { }
        public TlsOperationException(System.DirectoryServices.Protocols.DirectoryResponse response, string message) { }
        public TlsOperationException(System.DirectoryServices.Protocols.DirectoryResponse response, string message, System.Exception inner) { }
        protected TlsOperationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public TlsOperationException(string message) { }
        public TlsOperationException(string message, System.Exception inner) { }
    }
    public partial class TreeDeleteControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public TreeDeleteControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
    }
    public partial class VerifyNameControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public VerifyNameControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public VerifyNameControl(string serverName) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public VerifyNameControl(string serverName, int flag) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public int Flag { get { throw null; } set { } }
        public string ServerName { get { throw null; } set { } }
        public override byte[] GetValue() { throw null; }
    }
    public delegate bool VerifyServerCertificateCallback(System.DirectoryServices.Protocols.LdapConnection connection, System.Security.Cryptography.X509Certificates.X509Certificate certificate);
    public partial class VlvRequestControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        public VlvRequestControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public VlvRequestControl(int beforeCount, int afterCount, byte[] target) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public VlvRequestControl(int beforeCount, int afterCount, int offset) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public VlvRequestControl(int beforeCount, int afterCount, string target) : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public int AfterCount { get { throw null; } set { } }
        public int BeforeCount { get { throw null; } set { } }
        public byte[] ContextId { get { throw null; } set { } }
        public int EstimateCount { get { throw null; } set { } }
        public int Offset { get { throw null; } set { } }
        public byte[] Target { get { throw null; } set { } }
        public override byte[] GetValue() { throw null; }
    }
    public partial class VlvResponseControl : System.DirectoryServices.Protocols.DirectoryControl
    {
        internal VlvResponseControl() : base (default(string), default(byte[]), default(bool), default(bool)) { }
        public int ContentCount { get { throw null; } }
        public byte[] ContextId { get { throw null; } }
        public System.DirectoryServices.Protocols.ResultCode Result { get { throw null; } }
        public int TargetPosition { get { throw null; } }
    }
}

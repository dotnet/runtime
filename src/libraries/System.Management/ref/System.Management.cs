// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Management
{
    public enum AuthenticationLevel
    {
        Unchanged = -1,
        Default = 0,
        None = 1,
        Connect = 2,
        Call = 3,
        Packet = 4,
        PacketIntegrity = 5,
        PacketPrivacy = 6,
    }
    public enum CimType
    {
        None = 0,
        SInt16 = 2,
        SInt32 = 3,
        Real32 = 4,
        Real64 = 5,
        String = 8,
        Boolean = 11,
        Object = 13,
        SInt8 = 16,
        UInt8 = 17,
        UInt16 = 18,
        UInt32 = 19,
        SInt64 = 20,
        UInt64 = 21,
        DateTime = 101,
        Reference = 102,
        Char16 = 103,
    }
    public enum CodeLanguage
    {
        CSharp = 0,
        JScript = 1,
        VB = 2,
        VJSharp = 3,
        Mcpp = 4,
    }
    [System.FlagsAttribute]
    public enum ComparisonSettings
    {
        IncludeAll = 0,
        IgnoreQualifiers = 1,
        IgnoreObjectSource = 2,
        IgnoreDefaultValues = 4,
        IgnoreClass = 8,
        IgnoreCase = 16,
        IgnoreFlavor = 32,
    }
    public partial class CompletedEventArgs : System.Management.ManagementEventArgs
    {
        internal CompletedEventArgs() { }
        public System.Management.ManagementStatus Status { get { throw null; } }
        public System.Management.ManagementBaseObject StatusObject { get { throw null; } }
    }
    public delegate void CompletedEventHandler(object sender, System.Management.CompletedEventArgs e);
    public partial class ConnectionOptions : System.Management.ManagementOptions
    {
        public ConnectionOptions() { }
        public ConnectionOptions(string locale, string username, System.Security.SecureString password, string authority, System.Management.ImpersonationLevel impersonation, System.Management.AuthenticationLevel authentication, bool enablePrivileges, System.Management.ManagementNamedValueCollection context, System.TimeSpan timeout) { }
        public ConnectionOptions(string locale, string username, string password, string authority, System.Management.ImpersonationLevel impersonation, System.Management.AuthenticationLevel authentication, bool enablePrivileges, System.Management.ManagementNamedValueCollection context, System.TimeSpan timeout) { }
        public System.Management.AuthenticationLevel Authentication { get { throw null; } set { } }
        public string Authority { get { throw null; } set { } }
        public bool EnablePrivileges { get { throw null; } set { } }
        public System.Management.ImpersonationLevel Impersonation { get { throw null; } set { } }
        public string Locale { get { throw null; } set { } }
        public string Password { set { } }
        public System.Security.SecureString SecurePassword { set { } }
        public string Username { get { throw null; } set { } }
        public override object Clone() { throw null; }
    }
    public partial class DeleteOptions : System.Management.ManagementOptions
    {
        public DeleteOptions() { }
        public DeleteOptions(System.Management.ManagementNamedValueCollection context, System.TimeSpan timeout) { }
        public override object Clone() { throw null; }
    }
    public partial class EnumerationOptions : System.Management.ManagementOptions
    {
        public EnumerationOptions() { }
        public EnumerationOptions(System.Management.ManagementNamedValueCollection context, System.TimeSpan timeout, int blockSize, bool rewindable, bool returnImmediatley, bool useAmendedQualifiers, bool ensureLocatable, bool prototypeOnly, bool directRead, bool enumerateDeep) { }
        public int BlockSize { get { throw null; } set { } }
        public bool DirectRead { get { throw null; } set { } }
        public bool EnsureLocatable { get { throw null; } set { } }
        public bool EnumerateDeep { get { throw null; } set { } }
        public bool PrototypeOnly { get { throw null; } set { } }
        public bool ReturnImmediately { get { throw null; } set { } }
        public bool Rewindable { get { throw null; } set { } }
        public bool UseAmendedQualifiers { get { throw null; } set { } }
        public override object Clone() { throw null; }
    }
    public partial class EventArrivedEventArgs : System.Management.ManagementEventArgs
    {
        internal EventArrivedEventArgs() { }
        public System.Management.ManagementBaseObject NewEvent { get { throw null; } }
    }
    public delegate void EventArrivedEventHandler(object sender, System.Management.EventArrivedEventArgs e);
    public partial class EventQuery : System.Management.ManagementQuery
    {
        public EventQuery() { }
        public EventQuery(string query) { }
        public EventQuery(string language, string query) { }
        public override object Clone() { throw null; }
    }
    public partial class EventWatcherOptions : System.Management.ManagementOptions
    {
        public EventWatcherOptions() { }
        public EventWatcherOptions(System.Management.ManagementNamedValueCollection context, System.TimeSpan timeout, int blockSize) { }
        public int BlockSize { get { throw null; } set { } }
        public override object Clone() { throw null; }
    }
    public enum ImpersonationLevel
    {
        Default = 0,
        Anonymous = 1,
        Identify = 2,
        Impersonate = 3,
        Delegate = 4,
    }
    public partial class InvokeMethodOptions : System.Management.ManagementOptions
    {
        public InvokeMethodOptions() { }
        public InvokeMethodOptions(System.Management.ManagementNamedValueCollection context, System.TimeSpan timeout) { }
        public override object Clone() { throw null; }
    }
    [System.ComponentModel.ToolboxItemAttribute(false)]
    public partial class ManagementBaseObject : System.ComponentModel.Component, System.ICloneable, System.Runtime.Serialization.ISerializable
    {
        private protected ManagementBaseObject() { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected ManagementBaseObject(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public virtual System.Management.ManagementPath ClassPath { get { throw null; } }
        public object this[string propertyName] { get { throw null; } set { } }
        public virtual System.Management.PropertyDataCollection Properties { get { throw null; } }
        public virtual System.Management.QualifierDataCollection Qualifiers { get { throw null; } }
        public virtual System.Management.PropertyDataCollection SystemProperties { get { throw null; } }
        public virtual object Clone() { throw null; }
        public bool CompareTo(System.Management.ManagementBaseObject otherObject, System.Management.ComparisonSettings settings) { throw null; }
        public new void Dispose() { }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        protected virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public object GetPropertyQualifierValue(string propertyName, string qualifierName) { throw null; }
        public object GetPropertyValue(string propertyName) { throw null; }
        public object GetQualifierValue(string qualifierName) { throw null; }
        public string GetText(System.Management.TextFormat format) { throw null; }
        public static explicit operator System.IntPtr (System.Management.ManagementBaseObject managementObject) { throw null; }
        public void SetPropertyQualifierValue(string propertyName, string qualifierName, object qualifierValue) { }
        public void SetPropertyValue(string propertyName, object propertyValue) { }
        public void SetQualifierValue(string qualifierName, object qualifierValue) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class ManagementClass : System.Management.ManagementObject
    {
        public ManagementClass() { }
        public ManagementClass(System.Management.ManagementPath path) { }
        public ManagementClass(System.Management.ManagementPath path, System.Management.ObjectGetOptions options) { }
        public ManagementClass(System.Management.ManagementScope scope, System.Management.ManagementPath path, System.Management.ObjectGetOptions options) { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected ManagementClass(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ManagementClass(string path) { }
        public ManagementClass(string path, System.Management.ObjectGetOptions options) { }
        public ManagementClass(string scope, string path, System.Management.ObjectGetOptions options) { }
        public System.Collections.Specialized.StringCollection Derivation { get { throw null; } }
        public System.Management.MethodDataCollection Methods { get { throw null; } }
        public override System.Management.ManagementPath Path { get { throw null; } set { } }
        public override object Clone() { throw null; }
        public System.Management.ManagementObject CreateInstance() { throw null; }
        public System.Management.ManagementClass Derive(string newClassName) { throw null; }
        public System.Management.ManagementObjectCollection GetInstances() { throw null; }
        public System.Management.ManagementObjectCollection GetInstances(System.Management.EnumerationOptions options) { throw null; }
        public void GetInstances(System.Management.ManagementOperationObserver watcher) { }
        public void GetInstances(System.Management.ManagementOperationObserver watcher, System.Management.EnumerationOptions options) { }
        protected override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public System.Management.ManagementObjectCollection GetRelatedClasses() { throw null; }
        public void GetRelatedClasses(System.Management.ManagementOperationObserver watcher) { }
        public void GetRelatedClasses(System.Management.ManagementOperationObserver watcher, string relatedClass) { }
        public void GetRelatedClasses(System.Management.ManagementOperationObserver watcher, string relatedClass, string relationshipClass, string relationshipQualifier, string relatedQualifier, string relatedRole, string thisRole, System.Management.EnumerationOptions options) { }
        public System.Management.ManagementObjectCollection GetRelatedClasses(string relatedClass) { throw null; }
        public System.Management.ManagementObjectCollection GetRelatedClasses(string relatedClass, string relationshipClass, string relationshipQualifier, string relatedQualifier, string relatedRole, string thisRole, System.Management.EnumerationOptions options) { throw null; }
        public System.Management.ManagementObjectCollection GetRelationshipClasses() { throw null; }
        public void GetRelationshipClasses(System.Management.ManagementOperationObserver watcher) { }
        public void GetRelationshipClasses(System.Management.ManagementOperationObserver watcher, string relationshipClass) { }
        public void GetRelationshipClasses(System.Management.ManagementOperationObserver watcher, string relationshipClass, string relationshipQualifier, string thisRole, System.Management.EnumerationOptions options) { }
        public System.Management.ManagementObjectCollection GetRelationshipClasses(string relationshipClass) { throw null; }
        public System.Management.ManagementObjectCollection GetRelationshipClasses(string relationshipClass, string relationshipQualifier, string thisRole, System.Management.EnumerationOptions options) { throw null; }
        public System.CodeDom.CodeTypeDeclaration GetStronglyTypedClassCode(bool includeSystemClassInClassDef, bool systemPropertyClass) { throw null; }
        public bool GetStronglyTypedClassCode(System.Management.CodeLanguage lang, string filePath, string classNamespace) { throw null; }
        public System.Management.ManagementObjectCollection GetSubclasses() { throw null; }
        public System.Management.ManagementObjectCollection GetSubclasses(System.Management.EnumerationOptions options) { throw null; }
        public void GetSubclasses(System.Management.ManagementOperationObserver watcher) { }
        public void GetSubclasses(System.Management.ManagementOperationObserver watcher, System.Management.EnumerationOptions options) { }
    }
    public sealed partial class ManagementDateTimeConverter
    {
        internal ManagementDateTimeConverter() { }
        public static System.DateTime ToDateTime(string dmtfDate) { throw null; }
        public static string ToDmtfDateTime(System.DateTime date) { throw null; }
        public static string ToDmtfTimeInterval(System.TimeSpan timespan) { throw null; }
        public static System.TimeSpan ToTimeSpan(string dmtfTimespan) { throw null; }
    }
    public abstract partial class ManagementEventArgs : System.EventArgs
    {
        internal ManagementEventArgs() { }
        public object Context { get { throw null; } }
    }
    [System.ComponentModel.ToolboxItemAttribute(false)]
    public partial class ManagementEventWatcher : System.ComponentModel.Component
    {
        public ManagementEventWatcher() { }
        public ManagementEventWatcher(System.Management.EventQuery query) { }
        public ManagementEventWatcher(System.Management.ManagementScope scope, System.Management.EventQuery query) { }
        public ManagementEventWatcher(System.Management.ManagementScope scope, System.Management.EventQuery query, System.Management.EventWatcherOptions options) { }
        public ManagementEventWatcher(string query) { }
        public ManagementEventWatcher(string scope, string query) { }
        public ManagementEventWatcher(string scope, string query, System.Management.EventWatcherOptions options) { }
        public System.Management.EventWatcherOptions Options { get { throw null; } set { } }
        public System.Management.EventQuery Query { get { throw null; } set { } }
        public System.Management.ManagementScope Scope { get { throw null; } set { } }
        public event System.Management.EventArrivedEventHandler EventArrived { add { } remove { } }
        public event System.Management.StoppedEventHandler Stopped { add { } remove { } }
        ~ManagementEventWatcher() { }
        public void Start() { }
        public void Stop() { }
        public System.Management.ManagementBaseObject WaitForNextEvent() { throw null; }
    }
    public partial class ManagementException : System.SystemException
    {
        public ManagementException() { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected ManagementException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ManagementException(string message) { }
        public ManagementException(string message, System.Exception innerException) { }
        public System.Management.ManagementStatus ErrorCode { get { throw null; } }
        public System.Management.ManagementBaseObject ErrorInformation { get { throw null; } }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class ManagementNamedValueCollection : System.Collections.Specialized.NameObjectCollectionBase
    {
        public ManagementNamedValueCollection() { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected ManagementNamedValueCollection(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public object this[string name] { get { throw null; } }
        public void Add(string name, object value) { }
        public System.Management.ManagementNamedValueCollection Clone() { throw null; }
        public void Remove(string name) { }
        public void RemoveAll() { }
    }
    public partial class ManagementObject : System.Management.ManagementBaseObject, System.ICloneable
    {
        public ManagementObject() { }
        public ManagementObject(System.Management.ManagementPath path) { }
        public ManagementObject(System.Management.ManagementPath path, System.Management.ObjectGetOptions options) { }
        public ManagementObject(System.Management.ManagementScope scope, System.Management.ManagementPath path, System.Management.ObjectGetOptions options) { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected ManagementObject(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ManagementObject(string path) { }
        public ManagementObject(string path, System.Management.ObjectGetOptions options) { }
        public ManagementObject(string scopeString, string pathString, System.Management.ObjectGetOptions options) { }
        public override System.Management.ManagementPath ClassPath { get { throw null; } }
        public System.Management.ObjectGetOptions Options { get { throw null; } set { } }
        public virtual System.Management.ManagementPath Path { get { throw null; } set { } }
        public System.Management.ManagementScope Scope { get { throw null; } set { } }
        public override object Clone() { throw null; }
        public void CopyTo(System.Management.ManagementOperationObserver watcher, System.Management.ManagementPath path) { }
        public void CopyTo(System.Management.ManagementOperationObserver watcher, System.Management.ManagementPath path, System.Management.PutOptions options) { }
        public void CopyTo(System.Management.ManagementOperationObserver watcher, string path) { }
        public void CopyTo(System.Management.ManagementOperationObserver watcher, string path, System.Management.PutOptions options) { }
        public System.Management.ManagementPath CopyTo(System.Management.ManagementPath path) { throw null; }
        public System.Management.ManagementPath CopyTo(System.Management.ManagementPath path, System.Management.PutOptions options) { throw null; }
        public System.Management.ManagementPath CopyTo(string path) { throw null; }
        public System.Management.ManagementPath CopyTo(string path, System.Management.PutOptions options) { throw null; }
        public void Delete() { }
        public void Delete(System.Management.DeleteOptions options) { }
        public void Delete(System.Management.ManagementOperationObserver watcher) { }
        public void Delete(System.Management.ManagementOperationObserver watcher, System.Management.DeleteOptions options) { }
        public new void Dispose() { }
        public void Get() { }
        public void Get(System.Management.ManagementOperationObserver watcher) { }
        public System.Management.ManagementBaseObject GetMethodParameters(string methodName) { throw null; }
        protected override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public System.Management.ManagementObjectCollection GetRelated() { throw null; }
        public void GetRelated(System.Management.ManagementOperationObserver watcher) { }
        public void GetRelated(System.Management.ManagementOperationObserver watcher, string relatedClass) { }
        public void GetRelated(System.Management.ManagementOperationObserver watcher, string relatedClass, string relationshipClass, string relationshipQualifier, string relatedQualifier, string relatedRole, string thisRole, bool classDefinitionsOnly, System.Management.EnumerationOptions options) { }
        public System.Management.ManagementObjectCollection GetRelated(string relatedClass) { throw null; }
        public System.Management.ManagementObjectCollection GetRelated(string relatedClass, string relationshipClass, string relationshipQualifier, string relatedQualifier, string relatedRole, string thisRole, bool classDefinitionsOnly, System.Management.EnumerationOptions options) { throw null; }
        public System.Management.ManagementObjectCollection GetRelationships() { throw null; }
        public void GetRelationships(System.Management.ManagementOperationObserver watcher) { }
        public void GetRelationships(System.Management.ManagementOperationObserver watcher, string relationshipClass) { }
        public void GetRelationships(System.Management.ManagementOperationObserver watcher, string relationshipClass, string relationshipQualifier, string thisRole, bool classDefinitionsOnly, System.Management.EnumerationOptions options) { }
        public System.Management.ManagementObjectCollection GetRelationships(string relationshipClass) { throw null; }
        public System.Management.ManagementObjectCollection GetRelationships(string relationshipClass, string relationshipQualifier, string thisRole, bool classDefinitionsOnly, System.Management.EnumerationOptions options) { throw null; }
        public void InvokeMethod(System.Management.ManagementOperationObserver watcher, string methodName, System.Management.ManagementBaseObject inParameters, System.Management.InvokeMethodOptions options) { }
        public void InvokeMethod(System.Management.ManagementOperationObserver watcher, string methodName, object[] args) { }
        public System.Management.ManagementBaseObject InvokeMethod(string methodName, System.Management.ManagementBaseObject inParameters, System.Management.InvokeMethodOptions options) { throw null; }
        public object InvokeMethod(string methodName, object[] args) { throw null; }
        public System.Management.ManagementPath Put() { throw null; }
        public void Put(System.Management.ManagementOperationObserver watcher) { }
        public void Put(System.Management.ManagementOperationObserver watcher, System.Management.PutOptions options) { }
        public System.Management.ManagementPath Put(System.Management.PutOptions options) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class ManagementObjectCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.IDisposable
    {
        internal ManagementObjectCollection() { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Management.ManagementBaseObject[] objectCollection, int index) { }
        public void Dispose() { }
        ~ManagementObjectCollection() { }
        public System.Management.ManagementObjectCollection.ManagementObjectEnumerator GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial class ManagementObjectEnumerator : System.Collections.IEnumerator, System.IDisposable
        {
            internal ManagementObjectEnumerator() { }
            public System.Management.ManagementBaseObject Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            ~ManagementObjectEnumerator() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    [System.ComponentModel.ToolboxItemAttribute(false)]
    public partial class ManagementObjectSearcher : System.ComponentModel.Component
    {
        public ManagementObjectSearcher() { }
        public ManagementObjectSearcher(System.Management.ManagementScope scope, System.Management.ObjectQuery query) { }
        public ManagementObjectSearcher(System.Management.ManagementScope scope, System.Management.ObjectQuery query, System.Management.EnumerationOptions options) { }
        public ManagementObjectSearcher(System.Management.ObjectQuery query) { }
        public ManagementObjectSearcher(string queryString) { }
        public ManagementObjectSearcher(string scope, string queryString) { }
        public ManagementObjectSearcher(string scope, string queryString, System.Management.EnumerationOptions options) { }
        public System.Management.EnumerationOptions Options { get { throw null; } set { } }
        public System.Management.ObjectQuery Query { get { throw null; } set { } }
        public System.Management.ManagementScope Scope { get { throw null; } set { } }
        public System.Management.ManagementObjectCollection Get() { throw null; }
        public void Get(System.Management.ManagementOperationObserver watcher) { }
    }
    public partial class ManagementOperationObserver
    {
        public ManagementOperationObserver() { }
        public event System.Management.CompletedEventHandler Completed { add { } remove { } }
        public event System.Management.ObjectPutEventHandler ObjectPut { add { } remove { } }
        public event System.Management.ObjectReadyEventHandler ObjectReady { add { } remove { } }
        public event System.Management.ProgressEventHandler Progress { add { } remove { } }
        public void Cancel() { }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public abstract partial class ManagementOptions : System.ICloneable
    {
        internal ManagementOptions() { }
        public static readonly System.TimeSpan InfiniteTimeout;
        public System.Management.ManagementNamedValueCollection Context { get { throw null; } set { } }
        public System.TimeSpan Timeout { get { throw null; } set { } }
        public abstract object Clone();
    }
    public partial class ManagementPath : System.ICloneable
    {
        public ManagementPath() { }
        public ManagementPath(string path) { }
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public string ClassName { get { throw null; } set { } }
        public static System.Management.ManagementPath DefaultPath { get { throw null; } set { } }
        public bool IsClass { get { throw null; } }
        public bool IsInstance { get { throw null; } }
        public bool IsSingleton { get { throw null; } }
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public string NamespacePath { get { throw null; } set { } }
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public string Path { get { throw null; } set { } }
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public string RelativePath { get { throw null; } set { } }
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public string Server { get { throw null; } set { } }
        public System.Management.ManagementPath Clone() { throw null; }
        public void SetAsClass() { }
        public void SetAsSingleton() { }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
    }
    public abstract partial class ManagementQuery : System.ICloneable
    {
        internal ManagementQuery() { }
        public virtual string QueryLanguage { get { throw null; } set { } }
        public virtual string QueryString { get { throw null; } set { } }
        public abstract object Clone();
        protected internal virtual void ParseQuery(string query) { }
    }
    public partial class ManagementScope : System.ICloneable
    {
        public ManagementScope() { }
        public ManagementScope(System.Management.ManagementPath path) { }
        public ManagementScope(System.Management.ManagementPath path, System.Management.ConnectionOptions options) { }
        public ManagementScope(string path) { }
        public ManagementScope(string path, System.Management.ConnectionOptions options) { }
        public bool IsConnected { get { throw null; } }
        public System.Management.ConnectionOptions Options { get { throw null; } set { } }
        public System.Management.ManagementPath Path { get { throw null; } set { } }
        public System.Management.ManagementScope Clone() { throw null; }
        public void Connect() { }
        object System.ICloneable.Clone() { throw null; }
    }
    public enum ManagementStatus
    {
        Failed = -2147217407,
        NotFound = -2147217406,
        AccessDenied = -2147217405,
        ProviderFailure = -2147217404,
        TypeMismatch = -2147217403,
        OutOfMemory = -2147217402,
        InvalidContext = -2147217401,
        InvalidParameter = -2147217400,
        NotAvailable = -2147217399,
        CriticalError = -2147217398,
        InvalidStream = -2147217397,
        NotSupported = -2147217396,
        InvalidSuperclass = -2147217395,
        InvalidNamespace = -2147217394,
        InvalidObject = -2147217393,
        InvalidClass = -2147217392,
        ProviderNotFound = -2147217391,
        InvalidProviderRegistration = -2147217390,
        ProviderLoadFailure = -2147217389,
        InitializationFailure = -2147217388,
        TransportFailure = -2147217387,
        InvalidOperation = -2147217386,
        InvalidQuery = -2147217385,
        InvalidQueryType = -2147217384,
        AlreadyExists = -2147217383,
        OverrideNotAllowed = -2147217382,
        PropagatedQualifier = -2147217381,
        PropagatedProperty = -2147217380,
        Unexpected = -2147217379,
        IllegalOperation = -2147217378,
        CannotBeKey = -2147217377,
        IncompleteClass = -2147217376,
        InvalidSyntax = -2147217375,
        NondecoratedObject = -2147217374,
        ReadOnly = -2147217373,
        ProviderNotCapable = -2147217372,
        ClassHasChildren = -2147217371,
        ClassHasInstances = -2147217370,
        QueryNotImplemented = -2147217369,
        IllegalNull = -2147217368,
        InvalidQualifierType = -2147217367,
        InvalidPropertyType = -2147217366,
        ValueOutOfRange = -2147217365,
        CannotBeSingleton = -2147217364,
        InvalidCimType = -2147217363,
        InvalidMethod = -2147217362,
        InvalidMethodParameters = -2147217361,
        SystemProperty = -2147217360,
        InvalidProperty = -2147217359,
        CallCanceled = -2147217358,
        ShuttingDown = -2147217357,
        PropagatedMethod = -2147217356,
        UnsupportedParameter = -2147217355,
        MissingParameterID = -2147217354,
        InvalidParameterID = -2147217353,
        NonconsecutiveParameterIDs = -2147217352,
        ParameterIDOnRetval = -2147217351,
        InvalidObjectPath = -2147217350,
        OutOfDiskSpace = -2147217349,
        BufferTooSmall = -2147217348,
        UnsupportedPutExtension = -2147217347,
        UnknownObjectType = -2147217346,
        UnknownPacketType = -2147217345,
        MarshalVersionMismatch = -2147217344,
        MarshalInvalidSignature = -2147217343,
        InvalidQualifier = -2147217342,
        InvalidDuplicateParameter = -2147217341,
        TooMuchData = -2147217340,
        ServerTooBusy = -2147217339,
        InvalidFlavor = -2147217338,
        CircularReference = -2147217337,
        UnsupportedClassUpdate = -2147217336,
        CannotChangeKeyInheritance = -2147217335,
        CannotChangeIndexInheritance = -2147217328,
        TooManyProperties = -2147217327,
        UpdateTypeMismatch = -2147217326,
        UpdateOverrideNotAllowed = -2147217325,
        UpdatePropagatedMethod = -2147217324,
        MethodNotImplemented = -2147217323,
        MethodDisabled = -2147217322,
        RefresherBusy = -2147217321,
        UnparsableQuery = -2147217320,
        NotEventClass = -2147217319,
        MissingGroupWithin = -2147217318,
        MissingAggregationList = -2147217317,
        PropertyNotAnObject = -2147217316,
        AggregatingByObject = -2147217315,
        UninterpretableProviderQuery = -2147217313,
        BackupRestoreWinmgmtRunning = -2147217312,
        QueueOverflow = -2147217311,
        PrivilegeNotHeld = -2147217310,
        InvalidOperator = -2147217309,
        LocalCredentials = -2147217308,
        CannotBeAbstract = -2147217307,
        AmendedObject = -2147217306,
        ClientTooSlow = -2147217305,
        RegistrationTooBroad = -2147213311,
        RegistrationTooPrecise = -2147213310,
        NoError = 0,
        False = 1,
        ResetToDefault = 262146,
        Different = 262147,
        Timedout = 262148,
        NoMoreData = 262149,
        OperationCanceled = 262150,
        Pending = 262151,
        DuplicateObjects = 262152,
        PartialResults = 262160,
    }
    public partial class MethodData
    {
        internal MethodData() { }
        public System.Management.ManagementBaseObject InParameters { get { throw null; } }
        public string Name { get { throw null; } }
        public string Origin { get { throw null; } }
        public System.Management.ManagementBaseObject OutParameters { get { throw null; } }
        public System.Management.QualifierDataCollection Qualifiers { get { throw null; } }
    }
    public partial class MethodDataCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal MethodDataCollection() { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public virtual System.Management.MethodData this[string methodName] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public virtual void Add(string methodName) { }
        public virtual void Add(string methodName, System.Management.ManagementBaseObject inParameters, System.Management.ManagementBaseObject outParameters) { }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Management.MethodData[] methodArray, int index) { }
        public System.Management.MethodDataCollection.MethodDataEnumerator GetEnumerator() { throw null; }
        public virtual void Remove(string methodName) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial class MethodDataEnumerator : System.Collections.IEnumerator
        {
            internal MethodDataEnumerator() { }
            public System.Management.MethodData Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public partial class ObjectGetOptions : System.Management.ManagementOptions
    {
        public ObjectGetOptions() { }
        public ObjectGetOptions(System.Management.ManagementNamedValueCollection context) { }
        public ObjectGetOptions(System.Management.ManagementNamedValueCollection context, System.TimeSpan timeout, bool useAmendedQualifiers) { }
        public bool UseAmendedQualifiers { get { throw null; } set { } }
        public override object Clone() { throw null; }
    }
    public partial class ObjectPutEventArgs : System.Management.ManagementEventArgs
    {
        internal ObjectPutEventArgs() { }
        public System.Management.ManagementPath Path { get { throw null; } }
    }
    public delegate void ObjectPutEventHandler(object sender, System.Management.ObjectPutEventArgs e);
    public partial class ObjectQuery : System.Management.ManagementQuery
    {
        public ObjectQuery() { }
        public ObjectQuery(string query) { }
        public ObjectQuery(string language, string query) { }
        public override object Clone() { throw null; }
    }
    public partial class ObjectReadyEventArgs : System.Management.ManagementEventArgs
    {
        internal ObjectReadyEventArgs() { }
        public System.Management.ManagementBaseObject NewObject { get { throw null; } }
    }
    public delegate void ObjectReadyEventHandler(object sender, System.Management.ObjectReadyEventArgs e);
    public partial class ProgressEventArgs : System.Management.ManagementEventArgs
    {
        internal ProgressEventArgs() { }
        public int Current { get { throw null; } }
        public string Message { get { throw null; } }
        public int UpperBound { get { throw null; } }
    }
    public delegate void ProgressEventHandler(object sender, System.Management.ProgressEventArgs e);
    public partial class PropertyData
    {
        internal PropertyData() { }
        public bool IsArray { get { throw null; } }
        public bool IsLocal { get { throw null; } }
        public string Name { get { throw null; } }
        public string Origin { get { throw null; } }
        public System.Management.QualifierDataCollection Qualifiers { get { throw null; } }
        public System.Management.CimType Type { get { throw null; } }
        public object Value { get { throw null; } set { } }
    }
    public partial class PropertyDataCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal PropertyDataCollection() { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public virtual System.Management.PropertyData this[string propertyName] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public void Add(string propertyName, System.Management.CimType propertyType, bool isArray) { }
        public virtual void Add(string propertyName, object propertyValue) { }
        public void Add(string propertyName, object propertyValue, System.Management.CimType propertyType) { }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Management.PropertyData[] propertyArray, int index) { }
        public System.Management.PropertyDataCollection.PropertyDataEnumerator GetEnumerator() { throw null; }
        public virtual void Remove(string propertyName) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial class PropertyDataEnumerator : System.Collections.IEnumerator
        {
            internal PropertyDataEnumerator() { }
            public System.Management.PropertyData Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public partial class PutOptions : System.Management.ManagementOptions
    {
        public PutOptions() { }
        public PutOptions(System.Management.ManagementNamedValueCollection context) { }
        public PutOptions(System.Management.ManagementNamedValueCollection context, System.TimeSpan timeout, bool useAmendedQualifiers, System.Management.PutType putType) { }
        public System.Management.PutType Type { get { throw null; } set { } }
        public bool UseAmendedQualifiers { get { throw null; } set { } }
        public override object Clone() { throw null; }
    }
    public enum PutType
    {
        None = 0,
        UpdateOnly = 1,
        CreateOnly = 2,
        UpdateOrCreate = 3,
    }
    public partial class QualifierData
    {
        internal QualifierData() { }
        public bool IsAmended { get { throw null; } set { } }
        public bool IsLocal { get { throw null; } }
        public bool IsOverridable { get { throw null; } set { } }
        public string Name { get { throw null; } }
        public bool PropagatesToInstance { get { throw null; } set { } }
        public bool PropagatesToSubclass { get { throw null; } set { } }
        public object Value { get { throw null; } set { } }
    }
    public partial class QualifierDataCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal QualifierDataCollection() { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public virtual System.Management.QualifierData this[string qualifierName] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public virtual void Add(string qualifierName, object qualifierValue) { }
        public virtual void Add(string qualifierName, object qualifierValue, bool isAmended, bool propagatesToInstance, bool propagatesToSubclass, bool isOverridable) { }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Management.QualifierData[] qualifierArray, int index) { }
        public System.Management.QualifierDataCollection.QualifierDataEnumerator GetEnumerator() { throw null; }
        public virtual void Remove(string qualifierName) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial class QualifierDataEnumerator : System.Collections.IEnumerator
        {
            internal QualifierDataEnumerator() { }
            public System.Management.QualifierData Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public partial class RelatedObjectQuery : System.Management.WqlObjectQuery
    {
        public RelatedObjectQuery() { }
        public RelatedObjectQuery(bool isSchemaQuery, string sourceObject, string relatedClass, string relationshipClass, string relatedQualifier, string relationshipQualifier, string relatedRole, string thisRole) { }
        public RelatedObjectQuery(string queryOrSourceObject) { }
        public RelatedObjectQuery(string sourceObject, string relatedClass) { }
        public RelatedObjectQuery(string sourceObject, string relatedClass, string relationshipClass, string relatedQualifier, string relationshipQualifier, string relatedRole, string thisRole, bool classDefinitionsOnly) { }
        public bool ClassDefinitionsOnly { get { throw null; } set { } }
        public bool IsSchemaQuery { get { throw null; } set { } }
        public string RelatedClass { get { throw null; } set { } }
        public string RelatedQualifier { get { throw null; } set { } }
        public string RelatedRole { get { throw null; } set { } }
        public string RelationshipClass { get { throw null; } set { } }
        public string RelationshipQualifier { get { throw null; } set { } }
        public string SourceObject { get { throw null; } set { } }
        public string ThisRole { get { throw null; } set { } }
        protected internal void BuildQuery() { }
        public override object Clone() { throw null; }
        protected internal override void ParseQuery(string query) { }
    }
    public partial class RelationshipQuery : System.Management.WqlObjectQuery
    {
        public RelationshipQuery() { }
        public RelationshipQuery(bool isSchemaQuery, string sourceObject, string relationshipClass, string relationshipQualifier, string thisRole) { }
        public RelationshipQuery(string queryOrSourceObject) { }
        public RelationshipQuery(string sourceObject, string relationshipClass) { }
        public RelationshipQuery(string sourceObject, string relationshipClass, string relationshipQualifier, string thisRole, bool classDefinitionsOnly) { }
        public bool ClassDefinitionsOnly { get { throw null; } set { } }
        public bool IsSchemaQuery { get { throw null; } set { } }
        public string RelationshipClass { get { throw null; } set { } }
        public string RelationshipQualifier { get { throw null; } set { } }
        public string SourceObject { get { throw null; } set { } }
        public string ThisRole { get { throw null; } set { } }
        protected internal void BuildQuery() { }
        public override object Clone() { throw null; }
        protected internal override void ParseQuery(string query) { }
    }
    public partial class SelectQuery : System.Management.WqlObjectQuery
    {
        public SelectQuery() { }
        public SelectQuery(bool isSchemaQuery, string condition) { }
        public SelectQuery(string queryOrClassName) { }
        public SelectQuery(string className, string condition) { }
        public SelectQuery(string className, string condition, string[] selectedProperties) { }
        public string ClassName { get { throw null; } set { } }
        public string Condition { get { throw null; } set { } }
        public bool IsSchemaQuery { get { throw null; } set { } }
        public override string QueryString { get { throw null; } set { } }
        public System.Collections.Specialized.StringCollection SelectedProperties { get { throw null; } set { } }
        protected internal void BuildQuery() { }
        public override object Clone() { throw null; }
        protected internal override void ParseQuery(string query) { }
    }
    public partial class StoppedEventArgs : System.Management.ManagementEventArgs
    {
        internal StoppedEventArgs() { }
        public System.Management.ManagementStatus Status { get { throw null; } }
    }
    public delegate void StoppedEventHandler(object sender, System.Management.StoppedEventArgs e);
    public enum TextFormat
    {
        Mof = 0,
        CimDtd20 = 1,
        WmiDtd20 = 2,
    }
    public partial class WqlEventQuery : System.Management.EventQuery
    {
        public WqlEventQuery() { }
        public WqlEventQuery(string queryOrEventClassName) { }
        public WqlEventQuery(string eventClassName, string condition) { }
        public WqlEventQuery(string eventClassName, string condition, System.TimeSpan groupWithinInterval) { }
        public WqlEventQuery(string eventClassName, string condition, System.TimeSpan groupWithinInterval, string[] groupByPropertyList) { }
        public WqlEventQuery(string eventClassName, System.TimeSpan withinInterval) { }
        public WqlEventQuery(string eventClassName, System.TimeSpan withinInterval, string condition) { }
        public WqlEventQuery(string eventClassName, System.TimeSpan withinInterval, string condition, System.TimeSpan groupWithinInterval, string[] groupByPropertyList, string havingCondition) { }
        public string Condition { get { throw null; } set { } }
        public string EventClassName { get { throw null; } set { } }
        public System.Collections.Specialized.StringCollection GroupByPropertyList { get { throw null; } set { } }
        public System.TimeSpan GroupWithinInterval { get { throw null; } set { } }
        public string HavingCondition { get { throw null; } set { } }
        public override string QueryLanguage { get { throw null; } }
        public override string QueryString { get { throw null; } set { } }
        public System.TimeSpan WithinInterval { get { throw null; } set { } }
        protected internal void BuildQuery() { }
        public override object Clone() { throw null; }
        protected internal override void ParseQuery(string query) { }
    }
    public partial class WqlObjectQuery : System.Management.ObjectQuery
    {
        public WqlObjectQuery() { }
        public WqlObjectQuery(string query) { }
        public override string QueryLanguage { get { throw null; } }
        public override object Clone() { throw null; }
    }
}

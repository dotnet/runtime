// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Management
{
    public enum AuthenticationLevel
    {
        Call = 3,
        Connect = 2,
        Default = 0,
        None = 1,
        Packet = 4,
        PacketIntegrity = 5,
        PacketPrivacy = 6,
        Unchanged = -1,
    }
    public enum CimType
    {
        Boolean = 11,
        Char16 = 103,
        DateTime = 101,
        None = 0,
        Object = 13,
        Real32 = 4,
        Real64 = 5,
        Reference = 102,
        SInt16 = 2,
        SInt32 = 3,
        SInt64 = 20,
        SInt8 = 16,
        String = 8,
        UInt16 = 18,
        UInt32 = 19,
        UInt64 = 21,
        UInt8 = 17,
    }
    public enum CodeLanguage
    {
        CSharp = 0,
        JScript = 1,
        Mcpp = 4,
        VB = 2,
        VJSharp = 3,
    }
    [System.FlagsAttribute]
    public enum ComparisonSettings
    {
        IgnoreCase = 16,
        IgnoreClass = 8,
        IgnoreDefaultValues = 4,
        IgnoreFlavor = 32,
        IgnoreObjectSource = 2,
        IgnoreQualifiers = 1,
        IncludeAll = 0,
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
        Anonymous = 1,
        Default = 0,
        Delegate = 4,
        Identify = 2,
        Impersonate = 3,
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
        protected ManagementException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ManagementException(string message) { }
        public ManagementException(string message, System.Exception innerException) { }
        public System.Management.ManagementStatus ErrorCode { get { throw null; } }
        public System.Management.ManagementBaseObject ErrorInformation { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class ManagementNamedValueCollection : System.Collections.Specialized.NameObjectCollectionBase
    {
        public ManagementNamedValueCollection() { }
        protected ManagementNamedValueCollection(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public object this[string name] { get { throw null; } }
        public void Add(string name, object value) { }
        public System.Management.ManagementNamedValueCollection Clone() { throw null; }
        public void Remove(string name) { }
        public void RemoveAll() { }
    }
    public partial class ManagementObject : System.Management.ManagementBaseObject, System.ICloneable
    {
        public ManagementObject() : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
        public ManagementObject(System.Management.ManagementPath path) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
        public ManagementObject(System.Management.ManagementPath path, System.Management.ObjectGetOptions options) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
        public ManagementObject(System.Management.ManagementScope scope, System.Management.ManagementPath path, System.Management.ObjectGetOptions options) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
        protected ManagementObject(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
        public ManagementObject(string path) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
        public ManagementObject(string path, System.Management.ObjectGetOptions options) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
        public ManagementObject(string scopeString, string pathString, System.Management.ObjectGetOptions options) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
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
        [System.ComponentModel.RefreshPropertiesAttribute((System.ComponentModel.RefreshProperties)(1))]
        public string ClassName { get { throw null; } set { } }
        public static System.Management.ManagementPath DefaultPath { get { throw null; } set { } }
        public bool IsClass { get { throw null; } }
        public bool IsInstance { get { throw null; } }
        public bool IsSingleton { get { throw null; } }
        [System.ComponentModel.RefreshPropertiesAttribute((System.ComponentModel.RefreshProperties)(1))]
        public string NamespacePath { get { throw null; } set { } }
        [System.ComponentModel.RefreshPropertiesAttribute((System.ComponentModel.RefreshProperties)(1))]
        public string Path { get { throw null; } set { } }
        [System.ComponentModel.RefreshPropertiesAttribute((System.ComponentModel.RefreshProperties)(1))]
        public string RelativePath { get { throw null; } set { } }
        [System.ComponentModel.RefreshPropertiesAttribute((System.ComponentModel.RefreshProperties)(1))]
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
        AccessDenied = -2147217405,
        AggregatingByObject = -2147217315,
        AlreadyExists = -2147217383,
        AmendedObject = -2147217306,
        BackupRestoreWinmgmtRunning = -2147217312,
        BufferTooSmall = -2147217348,
        CallCanceled = -2147217358,
        CannotBeAbstract = -2147217307,
        CannotBeKey = -2147217377,
        CannotBeSingleton = -2147217364,
        CannotChangeIndexInheritance = -2147217328,
        CannotChangeKeyInheritance = -2147217335,
        CircularReference = -2147217337,
        ClassHasChildren = -2147217371,
        ClassHasInstances = -2147217370,
        ClientTooSlow = -2147217305,
        CriticalError = -2147217398,
        Different = 262147,
        DuplicateObjects = 262152,
        Failed = -2147217407,
        False = 1,
        IllegalNull = -2147217368,
        IllegalOperation = -2147217378,
        IncompleteClass = -2147217376,
        InitializationFailure = -2147217388,
        InvalidCimType = -2147217363,
        InvalidClass = -2147217392,
        InvalidContext = -2147217401,
        InvalidDuplicateParameter = -2147217341,
        InvalidFlavor = -2147217338,
        InvalidMethod = -2147217362,
        InvalidMethodParameters = -2147217361,
        InvalidNamespace = -2147217394,
        InvalidObject = -2147217393,
        InvalidObjectPath = -2147217350,
        InvalidOperation = -2147217386,
        InvalidOperator = -2147217309,
        InvalidParameter = -2147217400,
        InvalidParameterID = -2147217353,
        InvalidProperty = -2147217359,
        InvalidPropertyType = -2147217366,
        InvalidProviderRegistration = -2147217390,
        InvalidQualifier = -2147217342,
        InvalidQualifierType = -2147217367,
        InvalidQuery = -2147217385,
        InvalidQueryType = -2147217384,
        InvalidStream = -2147217397,
        InvalidSuperclass = -2147217395,
        InvalidSyntax = -2147217375,
        LocalCredentials = -2147217308,
        MarshalInvalidSignature = -2147217343,
        MarshalVersionMismatch = -2147217344,
        MethodDisabled = -2147217322,
        MethodNotImplemented = -2147217323,
        MissingAggregationList = -2147217317,
        MissingGroupWithin = -2147217318,
        MissingParameterID = -2147217354,
        NoError = 0,
        NoMoreData = 262149,
        NonconsecutiveParameterIDs = -2147217352,
        NondecoratedObject = -2147217374,
        NotAvailable = -2147217399,
        NotEventClass = -2147217319,
        NotFound = -2147217406,
        NotSupported = -2147217396,
        OperationCanceled = 262150,
        OutOfDiskSpace = -2147217349,
        OutOfMemory = -2147217402,
        OverrideNotAllowed = -2147217382,
        ParameterIDOnRetval = -2147217351,
        PartialResults = 262160,
        Pending = 262151,
        PrivilegeNotHeld = -2147217310,
        PropagatedMethod = -2147217356,
        PropagatedProperty = -2147217380,
        PropagatedQualifier = -2147217381,
        PropertyNotAnObject = -2147217316,
        ProviderFailure = -2147217404,
        ProviderLoadFailure = -2147217389,
        ProviderNotCapable = -2147217372,
        ProviderNotFound = -2147217391,
        QueryNotImplemented = -2147217369,
        QueueOverflow = -2147217311,
        ReadOnly = -2147217373,
        RefresherBusy = -2147217321,
        RegistrationTooBroad = -2147213311,
        RegistrationTooPrecise = -2147213310,
        ResetToDefault = 262146,
        ServerTooBusy = -2147217339,
        ShuttingDown = -2147217357,
        SystemProperty = -2147217360,
        Timedout = 262148,
        TooManyProperties = -2147217327,
        TooMuchData = -2147217340,
        TransportFailure = -2147217387,
        TypeMismatch = -2147217403,
        Unexpected = -2147217379,
        UninterpretableProviderQuery = -2147217313,
        UnknownObjectType = -2147217346,
        UnknownPacketType = -2147217345,
        UnparsableQuery = -2147217320,
        UnsupportedClassUpdate = -2147217336,
        UnsupportedParameter = -2147217355,
        UnsupportedPutExtension = -2147217347,
        UpdateOverrideNotAllowed = -2147217325,
        UpdatePropagatedMethod = -2147217324,
        UpdateTypeMismatch = -2147217326,
        ValueOutOfRange = -2147217365,
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
        CreateOnly = 2,
        None = 0,
        UpdateOnly = 1,
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
        CimDtd20 = 1,
        Mof = 0,
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

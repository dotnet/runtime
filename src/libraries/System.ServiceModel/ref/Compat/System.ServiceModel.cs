// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Generic
{
    public partial class KeyedByTypeCollection<TItem> : System.Collections.ObjectModel.KeyedCollection<System.Type, TItem>
    {
        public KeyedByTypeCollection() { }
        public KeyedByTypeCollection(System.Collections.Generic.IEnumerable<TItem> items) { }
        public T Find<T>() { throw null; }
        public System.Collections.ObjectModel.Collection<T> FindAll<T>() { throw null; }
        protected override System.Type GetKeyForItem(TItem item) { throw null; }
        protected override void InsertItem(int index, TItem kind) { }
        public T Remove<T>() { throw null; }
        public System.Collections.ObjectModel.Collection<T> RemoveAll<T>() { throw null; }
        protected override void SetItem(int index, TItem kind) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public partial class SynchronizedCollection<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public SynchronizedCollection() { }
        public SynchronizedCollection(object syncRoot) { }
        public SynchronizedCollection(object syncRoot, params T[] list) { }
        public SynchronizedCollection(object syncRoot, System.Collections.Generic.IEnumerable<T> list) { }
        public SynchronizedCollection(object syncRoot, System.Collections.Generic.List<T> list, bool makeCopy) { }
        public int Count { get { throw null; } }
        public T this[int index] { get { throw null; } set { } }
        protected System.Collections.Generic.List<T> Items { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void Add(T item) { }
        public void Clear() { }
        protected virtual void ClearItems() { }
        public bool Contains(T item) { throw null; }
        public void CopyTo(T[] array, int index) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
        public int IndexOf(T item) { throw null; }
        public void Insert(int index, T item) { }
        protected virtual void InsertItem(int index, T item) { }
        public bool Remove(T item) { throw null; }
        public void RemoveAt(int index) { }
        protected virtual void RemoveItem(int index) { }
        protected virtual void SetItem(int index, T item) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public abstract partial class SynchronizedKeyedCollection<K, T> : System.Collections.Generic.SynchronizedCollection<T>
    {
        protected SynchronizedKeyedCollection() { }
        protected SynchronizedKeyedCollection(object syncRoot) { }
        protected SynchronizedKeyedCollection(object syncRoot, System.Collections.Generic.IEqualityComparer<K> comparer) { }
        protected SynchronizedKeyedCollection(object syncRoot, System.Collections.Generic.IEqualityComparer<K> comparer, int capacity) { }
        protected System.Collections.Generic.IDictionary<K, T> Dictionary { get { throw null; } }
        public T this[K key] { get { throw null; } }
        protected void ChangeItemKey(T item, K newKey) { }
        protected override void ClearItems() { }
        public bool Contains(K key) { throw null; }
        protected abstract K GetKeyForItem(T item);
        protected override void InsertItem(int index, T item) { }
        public bool Remove(K key) { throw null; }
        protected override void RemoveItem(int index) { }
        protected override void SetItem(int index, T item) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public partial class SynchronizedReadOnlyCollection<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public SynchronizedReadOnlyCollection() { }
        public SynchronizedReadOnlyCollection(object sync_root) { }
        public SynchronizedReadOnlyCollection(object sync_root, params T[] list) { }
        public SynchronizedReadOnlyCollection(object sync_root, System.Collections.Generic.IEnumerable<T> list) { }
        public SynchronizedReadOnlyCollection(object sync_root, System.Collections.Generic.List<T> list, bool make_copy) { }
        public int Count { get { throw null; } }
        public T this[int index] { get { throw null; } }
        protected System.Collections.Generic.IList<T> Items { get { throw null; } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { throw null; } }
        T System.Collections.Generic.IList<T>.this[int index] { get { throw null; } set { } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public bool Contains(T value) { throw null; }
        public void CopyTo(T[] array, int index) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
        public int IndexOf(T value) { throw null; }
        void System.Collections.Generic.ICollection<T>.Add(T value) { }
        void System.Collections.Generic.ICollection<T>.Clear() { }
        bool System.Collections.Generic.ICollection<T>.Remove(T value) { throw null; }
        void System.Collections.Generic.IList<T>.Insert(int index, T value) { }
        void System.Collections.Generic.IList<T>.RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
        void System.Collections.IList.RemoveAt(int index) { }
    }
}
namespace System.ServiceModel
{
    public partial class ActionNotSupportedException : System.ServiceModel.CommunicationException
    {
        public ActionNotSupportedException() { }
        protected ActionNotSupportedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ActionNotSupportedException(string msg) { }
        public ActionNotSupportedException(string msg, System.Exception inner) { }
    }
    public enum AddressFilterMode
    {
        Any = 2,
        Exact = 0,
        Prefix = 1,
    }
    public enum AuditLevel
    {
        Failure = 2,
        None = 0,
        Success = 1,
        SuccessOrFailure = 3,
    }
    public enum AuditLogLocation
    {
        Application = 1,
        Default = 0,
        Security = 2,
    }
    public partial class BasicHttpBinding : System.ServiceModel.HttpBindingBase, System.ServiceModel.Channels.IBindingRuntimePreferences
    {
        public BasicHttpBinding() { }
        public BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode securityMode) { }
        public System.ServiceModel.WSMessageEncoding MessageEncoding { get { throw null; } set { } }
        public override string Scheme { get { throw null; } }
        public System.ServiceModel.BasicHttpSecurity Security { get { throw null; } set { } }
        public override System.ServiceModel.Channels.BindingElementCollection CreateBindingElements() { throw null; }
    }
    public enum BasicHttpMessageCredentialType
    {
        Certificate = 1,
        UserName = 0,
    }
    public sealed partial class BasicHttpMessageSecurity
    {
        internal BasicHttpMessageSecurity() { }
        public System.ServiceModel.BasicHttpMessageCredentialType ClientCredentialType { get { throw null; } set { } }
    }
    public partial class BasicHttpsBinding : System.ServiceModel.HttpBindingBase, System.ServiceModel.Channels.IBindingRuntimePreferences
    {
        public BasicHttpsBinding() { }
        public BasicHttpsBinding(System.ServiceModel.BasicHttpsSecurityMode securityMode) { }
        public System.ServiceModel.WSMessageEncoding MessageEncoding { get { throw null; } set { } }
        public override string Scheme { get { throw null; } }
        public System.ServiceModel.BasicHttpsSecurity Security { get { throw null; } }
        public override System.ServiceModel.Channels.BindingElementCollection CreateBindingElements() { throw null; }
    }
    public sealed partial class BasicHttpSecurity
    {
        public BasicHttpSecurity() { }
        public System.ServiceModel.BasicHttpMessageSecurity Message { get { throw null; } set { } }
        public System.ServiceModel.BasicHttpSecurityMode Mode { get { throw null; } set { } }
        public System.ServiceModel.HttpTransportSecurity Transport { get { throw null; } set { } }
    }
    public enum BasicHttpSecurityMode
    {
        Message = 2,
        None = 0,
        Transport = 1,
        TransportCredentialOnly = 4,
        TransportWithMessageCredential = 3,
    }
    public sealed partial class BasicHttpsSecurity
    {
        public BasicHttpsSecurity() { }
        public System.ServiceModel.BasicHttpMessageSecurity Message { get { throw null; } }
        public System.ServiceModel.BasicHttpsSecurityMode Mode { get { throw null; } set { } }
        public System.ServiceModel.HttpTransportSecurity Transport { get { throw null; } }
    }
    public enum BasicHttpsSecurityMode
    {
        Transport = 0,
        TransportWithMessageCredential = 1,
    }
    public abstract partial class ChannelFactory : System.ServiceModel.Channels.CommunicationObject, System.IDisposable, System.ServiceModel.Channels.IChannelFactory, System.ServiceModel.ICommunicationObject
    {
        protected ChannelFactory() { }
        public System.ServiceModel.Description.ClientCredentials Credentials { get { throw null; } }
        protected internal override System.TimeSpan DefaultCloseTimeout { get { throw null; } }
        protected internal override System.TimeSpan DefaultOpenTimeout { get { throw null; } }
        public System.ServiceModel.Description.ServiceEndpoint Endpoint { get { throw null; } }
        protected virtual void ApplyConfiguration(string endpointConfig) { }
        protected abstract System.ServiceModel.Description.ServiceEndpoint CreateDescription();
        protected virtual System.ServiceModel.Channels.IChannelFactory CreateFactory() { throw null; }
        protected void EnsureOpened() { }
        public T GetProperty<T>() where T : class { throw null; }
        protected void InitializeEndpoint(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) { }
        protected void InitializeEndpoint(System.ServiceModel.Description.ServiceEndpoint endpoint) { }
        protected void InitializeEndpoint(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) { }
        protected override void OnAbort() { }
        protected override System.IAsyncResult OnBeginClose(System.TimeSpan timeout, System.AsyncCallback callback, object state) { throw null; }
        protected override System.IAsyncResult OnBeginOpen(System.TimeSpan timeout, System.AsyncCallback callback, object state) { throw null; }
        protected override void OnClose(System.TimeSpan timeout) { }
        protected override void OnEndClose(System.IAsyncResult result) { }
        protected override void OnEndOpen(System.IAsyncResult result) { }
        protected override void OnOpen(System.TimeSpan timeout) { }
        protected override void OnOpened() { }
        protected override void OnOpening() { }
        void System.IDisposable.Dispose() { }
    }
    public partial class ChannelFactory<TChannel> : System.ServiceModel.ChannelFactory, System.ServiceModel.Channels.IChannelFactory, System.ServiceModel.Channels.IChannelFactory<TChannel>, System.ServiceModel.ICommunicationObject
    {
        public ChannelFactory() { }
        public ChannelFactory(System.ServiceModel.Channels.Binding binding) { }
        public ChannelFactory(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) { }
        public ChannelFactory(System.ServiceModel.Channels.Binding binding, string remoteAddress) { }
        public ChannelFactory(System.ServiceModel.Description.ServiceEndpoint endpoint) { }
        public ChannelFactory(string endpointConfigurationName) { }
        public ChannelFactory(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) { }
        protected ChannelFactory(System.Type type) { }
        public TChannel CreateChannel() { throw null; }
        public static TChannel CreateChannel(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress address) { throw null; }
        public static TChannel CreateChannel(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress address, System.Uri via) { throw null; }
        public TChannel CreateChannel(System.ServiceModel.EndpointAddress address) { throw null; }
        public virtual TChannel CreateChannel(System.ServiceModel.EndpointAddress address, System.Uri via) { throw null; }
        protected static TChannel CreateChannel(string endpointConfigurationName) { throw null; }
        protected override System.ServiceModel.Description.ServiceEndpoint CreateDescription() { throw null; }
    }
    public abstract partial class ClientBase<TChannel> : System.IDisposable, System.ServiceModel.ICommunicationObject where TChannel : class
    {
        protected ClientBase() { }
        protected ClientBase(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) { }
        protected ClientBase(System.ServiceModel.Description.ServiceEndpoint endpoint) { }
        protected ClientBase(System.ServiceModel.InstanceContext instance) { }
        protected ClientBase(System.ServiceModel.InstanceContext instance, System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) { }
        protected ClientBase(System.ServiceModel.InstanceContext instance, System.ServiceModel.Description.ServiceEndpoint endpoint) { }
        protected ClientBase(System.ServiceModel.InstanceContext instance, string endpointConfigurationName) { }
        protected ClientBase(System.ServiceModel.InstanceContext instance, string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) { }
        protected ClientBase(System.ServiceModel.InstanceContext instance, string endpointConfigurationName, string remoteAddress) { }
        protected ClientBase(string endpointConfigurationName) { }
        protected ClientBase(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) { }
        protected ClientBase(string endpointConfigurationName, string remoteAddress) { }
        protected TChannel Channel { get { throw null; } }
        public System.ServiceModel.ChannelFactory<TChannel> ChannelFactory { get { throw null; } }
        public System.ServiceModel.Description.ClientCredentials ClientCredentials { get { throw null; } }
        public System.ServiceModel.Description.ServiceEndpoint Endpoint { get { throw null; } }
        public System.ServiceModel.IClientChannel InnerChannel { get { throw null; } }
        public System.ServiceModel.CommunicationState State { get { throw null; } }
        event System.EventHandler System.ServiceModel.ICommunicationObject.Closed { add { } remove { } }
        event System.EventHandler System.ServiceModel.ICommunicationObject.Closing { add { } remove { } }
        event System.EventHandler System.ServiceModel.ICommunicationObject.Faulted { add { } remove { } }
        event System.EventHandler System.ServiceModel.ICommunicationObject.Opened { add { } remove { } }
        event System.EventHandler System.ServiceModel.ICommunicationObject.Opening { add { } remove { } }
        public void Abort() { }
        public void Close() { }
        protected virtual TChannel CreateChannel() { throw null; }
        public void DisplayInitializationUI() { }
        protected T GetDefaultValueForInitialization<T>() { throw null; }
        protected void InvokeAsync(System.ServiceModel.ClientBase<TChannel>.BeginOperationDelegate beginOperationDelegate, object[] inValues, System.ServiceModel.ClientBase<TChannel>.EndOperationDelegate endOperationDelegate, System.Threading.SendOrPostCallback operationCompletedCallback, object userState) { }
        public void Open() { }
        void System.IDisposable.Dispose() { }
        System.IAsyncResult System.ServiceModel.ICommunicationObject.BeginClose(System.AsyncCallback callback, object state) { throw null; }
        System.IAsyncResult System.ServiceModel.ICommunicationObject.BeginClose(System.TimeSpan timeout, System.AsyncCallback callback, object state) { throw null; }
        System.IAsyncResult System.ServiceModel.ICommunicationObject.BeginOpen(System.AsyncCallback callback, object state) { throw null; }
        System.IAsyncResult System.ServiceModel.ICommunicationObject.BeginOpen(System.TimeSpan timeout, System.AsyncCallback callback, object state) { throw null; }
        void System.ServiceModel.ICommunicationObject.Close(System.TimeSpan timeout) { }
        void System.ServiceModel.ICommunicationObject.EndClose(System.IAsyncResult result) { }
        void System.ServiceModel.ICommunicationObject.EndOpen(System.IAsyncResult result) { }
        void System.ServiceModel.ICommunicationObject.Open(System.TimeSpan timeout) { }
        protected delegate System.IAsyncResult BeginOperationDelegate(object[] inValues, System.AsyncCallback asyncCallback, object state);
        protected internal partial class ChannelBase<T> : System.IDisposable, System.ServiceModel.Channels.IChannel, System.ServiceModel.Channels.IOutputChannel, System.ServiceModel.Channels.IRequestChannel, System.ServiceModel.IClientChannel, System.ServiceModel.ICommunicationObject, System.ServiceModel.IContextChannel, System.ServiceModel.IExtensibleObject<System.ServiceModel.IContextChannel> where T : class
        {
            protected ChannelBase(System.ServiceModel.ClientBase<T> client) { }
            public bool AllowInitializationUI { get { throw null; } set { } }
                    public bool AllowOutputBatching { get { throw null; } set { } }
            public bool DidInteractiveInitialization { get { throw null; } }
                    public System.ServiceModel.Channels.IInputSession InputSession { get { throw null; } }
            public System.ServiceModel.EndpointAddress LocalAddress { get { throw null; } }
                    public System.TimeSpan OperationTimeout { get { throw null; } set { } }
                    public System.ServiceModel.Channels.IOutputSession OutputSession { get { throw null; } }
            public System.ServiceModel.EndpointAddress RemoteAddress { get { throw null; } }
                    public string SessionId { get { throw null; } }
            System.ServiceModel.EndpointAddress System.ServiceModel.Channels.IRequestChannel.RemoteAddress { get { throw null; } }
            System.Uri System.ServiceModel.Channels.IRequestChannel.Via { get { throw null; } }
            System.ServiceModel.CommunicationState System.ServiceModel.ICommunicationObject.State { get { throw null; } }
            System.ServiceModel.IExtensionCollection<System.ServiceModel.IContextChannel> System.ServiceModel.IExtensibleObject<System.ServiceModel.IContextChannel>.Extensions { get { throw null; } }
            public System.Uri Via { get { throw null; } }
            event System.EventHandler System.ServiceModel.ICommunicationObject.Closed { add { } remove { } }
            event System.EventHandler System.ServiceModel.ICommunicationObject.Closing { add { } remove { } }
            event System.EventHandler System.ServiceModel.ICommunicationObject.Faulted { add { } remove { } }
            event System.EventHandler System.ServiceModel.ICommunicationObject.Opened { add { } remove { } }
            event System.EventHandler System.ServiceModel.ICommunicationObject.Opening { add { } remove { } }
            public event System.EventHandler<System.ServiceModel.UnknownMessageReceivedEventArgs> UnknownMessageReceived { add { } remove { } }
            public System.IAsyncResult BeginDisplayInitializationUI(System.AsyncCallback callback, object state) { throw null; }
            protected System.IAsyncResult BeginInvoke(string methodName, object[] args, System.AsyncCallback callback, object state) { throw null; }
            public void DisplayInitializationUI() { }
            public void Dispose() { }
            public void EndDisplayInitializationUI(System.IAsyncResult result) { }
            protected object EndInvoke(string methodName, object[] args, System.IAsyncResult result) { throw null; }
            protected object Invoke(string methodName, object[] args) { throw null; }
            TProperty System.ServiceModel.Channels.IChannel.GetProperty<TProperty>() { throw null; }
            System.IAsyncResult System.ServiceModel.Channels.IOutputChannel.BeginSend(System.ServiceModel.Channels.Message message, System.AsyncCallback callback, object state) { throw null; }
            System.IAsyncResult System.ServiceModel.Channels.IOutputChannel.BeginSend(System.ServiceModel.Channels.Message message, System.TimeSpan timeout, System.AsyncCallback callback, object state) { throw null; }
            void System.ServiceModel.Channels.IOutputChannel.EndSend(System.IAsyncResult result) { }
            void System.ServiceModel.Channels.IOutputChannel.Send(System.ServiceModel.Channels.Message message) { }
            void System.ServiceModel.Channels.IOutputChannel.Send(System.ServiceModel.Channels.Message message, System.TimeSpan timeout) { }
            System.IAsyncResult System.ServiceModel.Channels.IRequestChannel.BeginRequest(System.ServiceModel.Channels.Message message, System.AsyncCallback callback, object state) { throw null; }
            System.IAsyncResult System.ServiceModel.Channels.IRequestChannel.BeginRequest(System.ServiceModel.Channels.Message message, System.TimeSpan timeout, System.AsyncCallback callback, object state) { throw null; }
            System.ServiceModel.Channels.Message System.ServiceModel.Channels.IRequestChannel.EndRequest(System.IAsyncResult result) { throw null; }
            System.ServiceModel.Channels.Message System.ServiceModel.Channels.IRequestChannel.Request(System.ServiceModel.Channels.Message message) { throw null; }
            System.ServiceModel.Channels.Message System.ServiceModel.Channels.IRequestChannel.Request(System.ServiceModel.Channels.Message message, System.TimeSpan timeout) { throw null; }
            void System.ServiceModel.ICommunicationObject.Abort() { }
            System.IAsyncResult System.ServiceModel.ICommunicationObject.BeginClose(System.AsyncCallback callback, object state) { throw null; }
            System.IAsyncResult System.ServiceModel.ICommunicationObject.BeginClose(System.TimeSpan timeout, System.AsyncCallback callback, object state) { throw null; }
            System.IAsyncResult System.ServiceModel.ICommunicationObject.BeginOpen(System.AsyncCallback callback, object state) { throw null; }
            System.IAsyncResult System.ServiceModel.ICommunicationObject.BeginOpen(System.TimeSpan timeout, System.AsyncCallback callback, object state) { throw null; }
            void System.ServiceModel.ICommunicationObject.Close() { }
            void System.ServiceModel.ICommunicationObject.Close(System.TimeSpan timeout) { }
            void System.ServiceModel.ICommunicationObject.EndClose(System.IAsyncResult result) { }
            void System.ServiceModel.ICommunicationObject.EndOpen(System.IAsyncResult result) { }
            void System.ServiceModel.ICommunicationObject.Open() { }
            void System.ServiceModel.ICommunicationObject.Open(System.TimeSpan timeout) { }
        }
        protected delegate object[] EndOperationDelegate(System.IAsyncResult result);
        protected partial class InvokeAsyncCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
        {
            internal InvokeAsyncCompletedEventArgs() { }
            public object[] Results { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        }
    }
    public partial class CommunicationException : System.SystemException
    {
        public CommunicationException() { }
        protected CommunicationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public CommunicationException(string msg) { }
        public CommunicationException(string msg, System.Exception inner) { }
    }
    public partial class CommunicationObjectAbortedException : System.ServiceModel.CommunicationException
    {
        public CommunicationObjectAbortedException() { }
        protected CommunicationObjectAbortedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public CommunicationObjectAbortedException(string msg) { }
        public CommunicationObjectAbortedException(string msg, System.Exception inner) { }
    }
    public partial class CommunicationObjectFaultedException : System.ServiceModel.CommunicationException
    {
        public CommunicationObjectFaultedException() { }
        protected CommunicationObjectFaultedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public CommunicationObjectFaultedException(string msg) { }
        public CommunicationObjectFaultedException(string msg, System.Exception inner) { }
    }
    public enum CommunicationState
    {
        Closed = 4,
        Closing = 3,
        Created = 0,
        Faulted = 5,
        Opened = 2,
        Opening = 1,
    }
    public enum ConcurrencyMode
    {
        Multiple = 2,
        Reentrant = 1,
        Single = 0,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1092), Inherited=false)]
    public sealed partial class DataContractFormatAttribute : System.Attribute
    {
        public DataContractFormatAttribute() { }
        public System.ServiceModel.OperationFormatStyle Style { get { throw null; } set { } }
    }
    public enum DeadLetterQueue
    {
        Custom = 2,
        None = 0,
        System = 1,
    }
    public partial class EndpointAddress
    {
        public EndpointAddress(string uri) { }
        public EndpointAddress(System.Uri uri, params System.ServiceModel.Channels.AddressHeader[] headers) { }
        public EndpointAddress(System.Uri uri, System.ServiceModel.EndpointIdentity identity, params System.ServiceModel.Channels.AddressHeader[] headers) { }
        public EndpointAddress(System.Uri uri, System.ServiceModel.EndpointIdentity identity, System.ServiceModel.Channels.AddressHeaderCollection headers) { }
        public EndpointAddress(System.Uri uri, System.ServiceModel.EndpointIdentity identity, System.ServiceModel.Channels.AddressHeaderCollection headers, System.Xml.XmlDictionaryReader metadataReader, System.Xml.XmlDictionaryReader extensionReader) { }
        public static System.Uri AnonymousUri { get { throw null; } }
        public System.ServiceModel.Channels.AddressHeaderCollection Headers { get { throw null; } }
        public System.ServiceModel.EndpointIdentity Identity { get { throw null; } }
        public bool IsAnonymous { get { throw null; } }
        public bool IsNone { get { throw null; } }
        public static System.Uri NoneUri { get { throw null; } }
        public System.Uri Uri { get { throw null; } }
            public void ApplyTo(System.ServiceModel.Channels.Message message) { }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.Xml.XmlDictionaryReader GetReaderAtExtensions() { throw null; }
        public System.Xml.XmlDictionaryReader GetReaderAtMetadata() { throw null; }
        public static bool operator ==(System.ServiceModel.EndpointAddress address1, System.ServiceModel.EndpointAddress address2) { throw null; }
        public static bool operator !=(System.ServiceModel.EndpointAddress address1, System.ServiceModel.EndpointAddress address2) { throw null; }
        public static System.ServiceModel.EndpointAddress ReadFrom(System.ServiceModel.Channels.AddressingVersion addressingVersion, System.Xml.XmlDictionaryReader reader) { throw null; }
        public static System.ServiceModel.EndpointAddress ReadFrom(System.ServiceModel.Channels.AddressingVersion addressingVersion, System.Xml.XmlDictionaryReader reader, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString ns) { throw null; }
        public static System.ServiceModel.EndpointAddress ReadFrom(System.ServiceModel.Channels.AddressingVersion addressingVersion, System.Xml.XmlReader reader) { throw null; }
        public static System.ServiceModel.EndpointAddress ReadFrom(System.ServiceModel.Channels.AddressingVersion addressingVersion, System.Xml.XmlReader reader, string localName, string ns) { throw null; }
        public static System.ServiceModel.EndpointAddress ReadFrom(System.Xml.XmlDictionaryReader reader) { throw null; }
        public static System.ServiceModel.EndpointAddress ReadFrom(System.Xml.XmlDictionaryReader reader, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString ns) { throw null; }
        public override string ToString() { throw null; }
        public void WriteContentsTo(System.ServiceModel.Channels.AddressingVersion addressingVersion, System.Xml.XmlDictionaryWriter writer) { }
        public void WriteContentsTo(System.ServiceModel.Channels.AddressingVersion addressingVersion, System.Xml.XmlWriter writer) { }
        public void WriteTo(System.ServiceModel.Channels.AddressingVersion addressingVersion, System.Xml.XmlDictionaryWriter writer) { }
        public void WriteTo(System.ServiceModel.Channels.AddressingVersion addressingVersion, System.Xml.XmlDictionaryWriter writer, System.Xml.XmlDictionaryString localname, System.Xml.XmlDictionaryString ns) { }
        public void WriteTo(System.ServiceModel.Channels.AddressingVersion addressingVersion, System.Xml.XmlWriter writer) { }
        public void WriteTo(System.ServiceModel.Channels.AddressingVersion addressingVersion, System.Xml.XmlWriter writer, string localname, string ns) { }
    }
    public partial class EndpointAddressBuilder
    {
        public EndpointAddressBuilder() { }
        public EndpointAddressBuilder(System.ServiceModel.EndpointAddress address) { }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Channels.AddressHeader> Headers { get { throw null; } }
        public System.Uri Uri { get { throw null; } set { } }
        public System.ServiceModel.EndpointAddress ToEndpointAddress() { throw null; }
    }
    public partial class EndpointIdentity
    {
        public EndpointIdentity() { }
    }
    public partial class EndpointNotFoundException : System.ServiceModel.CommunicationException
    {
        public EndpointNotFoundException() { }
        protected EndpointNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public EndpointNotFoundException(string msg) { }
        public EndpointNotFoundException(string msg, System.Exception inner) { }
    }
    public sealed partial class EnvelopeVersion
    {
        internal EnvelopeVersion() { }
        public string NextDestinationActorValue { get { throw null; } }
        public static System.ServiceModel.EnvelopeVersion None { get { throw null; } }
        public static System.ServiceModel.EnvelopeVersion Soap11 { get { throw null; } }
        public static System.ServiceModel.EnvelopeVersion Soap12 { get { throw null; } }
        public string[] GetUltimateDestinationActorValues() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Runtime.Serialization.DataContractAttribute]
    public partial class ExceptionDetail
    {
        public ExceptionDetail(System.Exception exception) { }
        [System.Runtime.Serialization.DataMemberAttribute]
        public string HelpLink { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [System.Runtime.Serialization.DataMemberAttribute]
        public System.ServiceModel.ExceptionDetail InnerException { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [System.Runtime.Serialization.DataMemberAttribute]
        public string Message { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [System.Runtime.Serialization.DataMemberAttribute]
        public string StackTrace { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [System.Runtime.Serialization.DataMemberAttribute]
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override string ToString() { throw null; }
    }
    public sealed partial class ExtensionCollection<T> : System.Collections.Generic.SynchronizedCollection<System.ServiceModel.IExtension<T>>, System.Collections.Generic.ICollection<System.ServiceModel.IExtension<T>>, System.Collections.Generic.IEnumerable<System.ServiceModel.IExtension<T>>, System.Collections.IEnumerable, System.ServiceModel.IExtensionCollection<T> where T : System.ServiceModel.IExtensibleObject<T>
    {
        public ExtensionCollection(T owner) { }
        public ExtensionCollection(T owner, object syncRoot) { }
        bool System.Collections.Generic.ICollection<System.ServiceModel.IExtension<T>>.IsReadOnly { get { throw null; } }
            protected override void ClearItems() { }
        public E Find<E>() { throw null; }
        public System.Collections.ObjectModel.Collection<E> FindAll<E>() { throw null; }
            protected override void InsertItem(int index, System.ServiceModel.IExtension<T> item) { }
            protected override void RemoveItem(int index) { }
            protected override void SetItem(int index, System.ServiceModel.IExtension<T> item) { }
    }
    public partial class FaultCode
    {
        public FaultCode(string name) { }
        public FaultCode(string name, System.ServiceModel.FaultCode subcode) { }
        public FaultCode(string name, string ns) { }
        public FaultCode(string name, string ns, System.ServiceModel.FaultCode subcode) { }
        public bool IsPredefinedFault { get { throw null; } }
        public bool IsReceiverFault { get { throw null; } }
        public bool IsSenderFault { get { throw null; } }
        public string Name { get { throw null; } }
        public string Namespace { get { throw null; } }
        public System.ServiceModel.FaultCode SubCode { get { throw null; } }
        public static System.ServiceModel.FaultCode CreateReceiverFaultCode(System.ServiceModel.FaultCode subcode) { throw null; }
        public static System.ServiceModel.FaultCode CreateReceiverFaultCode(string name, string ns) { throw null; }
        public static System.ServiceModel.FaultCode CreateSenderFaultCode(System.ServiceModel.FaultCode subcode) { throw null; }
        public static System.ServiceModel.FaultCode CreateSenderFaultCode(string name, string ns) { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false, AllowMultiple=true)]
    public sealed partial class FaultContractAttribute : System.Attribute
    {
        public FaultContractAttribute(System.Type detailType) { }
        public string Action { get { throw null; } set { } }
        public System.Type DetailType { get { throw null; } }
        public bool HasProtectionLevel { get { throw null; } }
        public string Name { get { throw null; } set { } }
        public string Namespace { get { throw null; } set { } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { throw null; } set { } }
    }
    public partial class FaultException : System.ServiceModel.CommunicationException
    {
        public FaultException() { }
            protected FaultException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
            public FaultException(System.ServiceModel.Channels.MessageFault fault) { }
        public FaultException(System.ServiceModel.Channels.MessageFault fault, string action) { }
            public FaultException(System.ServiceModel.FaultReason reason) { }
        public FaultException(System.ServiceModel.FaultReason reason, System.ServiceModel.FaultCode code) { }
        public FaultException(System.ServiceModel.FaultReason reason, System.ServiceModel.FaultCode code, string action) { }
        public FaultException(string msg) { }
        public FaultException(string msg, System.ServiceModel.FaultCode code) { }
        public FaultException(string reason, System.ServiceModel.FaultCode code, string action) { }
        public string Action { get { throw null; } }
        public System.ServiceModel.FaultCode Code { get { throw null; } }
        public override string Message { get { throw null; } }
        public System.ServiceModel.FaultReason Reason { get { throw null; } }
            public static System.ServiceModel.FaultException CreateFault(System.ServiceModel.Channels.MessageFault messageFault, string action, params System.Type[] faultDetailTypes) { throw null; }
            public static System.ServiceModel.FaultException CreateFault(System.ServiceModel.Channels.MessageFault fault, params System.Type[] details) { throw null; }
        public virtual System.ServiceModel.Channels.MessageFault CreateMessageFault() { throw null; }
            public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class FaultException<TDetail> : System.ServiceModel.FaultException
    {
        public FaultException(TDetail detail) { }
        public FaultException(TDetail detail, System.ServiceModel.FaultReason reason) { }
        public FaultException(TDetail detail, System.ServiceModel.FaultReason reason, System.ServiceModel.FaultCode code) { }
        public FaultException(TDetail detail, System.ServiceModel.FaultReason reason, System.ServiceModel.FaultCode code, string action) { }
        public FaultException(TDetail detail, string reason) { }
        public FaultException(TDetail detail, string reason, System.ServiceModel.FaultCode code) { }
        public FaultException(TDetail detail, string reason, System.ServiceModel.FaultCode code, string action) { }
            protected FaultException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public TDetail Detail { get { throw null; } }
        public override System.ServiceModel.Channels.MessageFault CreateMessageFault() { throw null; }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
    }
    public partial class FaultReason
    {
        public FaultReason(System.Collections.Generic.IEnumerable<System.ServiceModel.FaultReasonText> translations) { }
        public FaultReason(System.ServiceModel.FaultReasonText translation) { }
        public FaultReason(string text) { }
        public System.Collections.Generic.SynchronizedReadOnlyCollection<System.ServiceModel.FaultReasonText> Translations { get { throw null; } }
        public System.ServiceModel.FaultReasonText GetMatchingTranslation() { throw null; }
        public System.ServiceModel.FaultReasonText GetMatchingTranslation(System.Globalization.CultureInfo cultureInfo) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class FaultReasonText
    {
        public FaultReasonText(string text) { }
        public FaultReasonText(string text, System.Globalization.CultureInfo cultureInfo) { }
        public FaultReasonText(string text, string xmlLang) { }
        public string Text { get { throw null; } }
        public string XmlLang { get { throw null; } }
        public bool Matches(System.Globalization.CultureInfo cultureInfo) { throw null; }
    }
    public enum HostNameComparisonMode
    {
        Exact = 1,
        StrongWildcard = 0,
        WeakWildcard = 2,
    }
    public abstract partial class HttpBindingBase : System.ServiceModel.Channels.Binding, System.ServiceModel.Channels.IBindingRuntimePreferences
    {
        protected HttpBindingBase() { }
        public bool AllowCookies { get { throw null; } set { } }
        public bool BypassProxyOnLocal { get { throw null; } set { } }
        public System.ServiceModel.EnvelopeVersion EnvelopeVersion { get { throw null; } }
        public System.ServiceModel.HostNameComparisonMode HostNameComparisonMode { get { throw null; } set { } }
        public long MaxBufferPoolSize { get { throw null; } set { } }
        public int MaxBufferSize { get { throw null; } set { } }
        public long MaxReceivedMessageSize { get { throw null; } set { } }
        public System.Uri ProxyAddress { get { throw null; } set { } }
        public System.Xml.XmlDictionaryReaderQuotas ReaderQuotas { get { throw null; } set { } }
        public abstract override string Scheme { get; }
        bool System.ServiceModel.Channels.IBindingRuntimePreferences.ReceiveSynchronously { get { throw null; } }
        public System.Text.Encoding TextEncoding { get { throw null; } set { } }
        public System.ServiceModel.TransferMode TransferMode { get { throw null; } set { } }
        public bool UseDefaultWebProxy { get { throw null; } set { } }
        public abstract override System.ServiceModel.Channels.BindingElementCollection CreateBindingElements();
    }
    public enum HttpClientCredentialType
    {
        Basic = 1,
        Certificate = 5,
        Digest = 2,
        InheritedFromHost = 6,
        None = 0,
        Ntlm = 3,
        Windows = 4,
    }
    public enum HttpProxyCredentialType
    {
        Basic = 1,
        Digest = 2,
        None = 0,
        Ntlm = 3,
        Windows = 4,
    }
    public sealed partial class HttpTransportSecurity
    {
        public HttpTransportSecurity() { }
        public System.ServiceModel.HttpClientCredentialType ClientCredentialType { get { throw null; } set { } }
            public System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy ExtendedProtectionPolicy { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.ServiceModel.HttpProxyCredentialType ProxyCredentialType { get { throw null; } set { } }
        public string Realm { get { throw null; } set { } }
    }
    public partial interface IClientChannel : System.IDisposable, System.ServiceModel.Channels.IChannel, System.ServiceModel.ICommunicationObject, System.ServiceModel.IContextChannel, System.ServiceModel.IExtensibleObject<System.ServiceModel.IContextChannel>
    {
        bool AllowInitializationUI { get; set; }
        bool DidInteractiveInitialization { get; }
        System.Uri Via { get; }
        event System.EventHandler<System.ServiceModel.UnknownMessageReceivedEventArgs> UnknownMessageReceived;
        System.IAsyncResult BeginDisplayInitializationUI(System.AsyncCallback callback, object state);
        void DisplayInitializationUI();
        void EndDisplayInitializationUI(System.IAsyncResult result);
    }
    public partial interface ICommunicationObject
    {
        System.ServiceModel.CommunicationState State { get; }
        event System.EventHandler Closed;
        event System.EventHandler Closing;
        event System.EventHandler Faulted;
        event System.EventHandler Opened;
        event System.EventHandler Opening;
        void Abort();
        System.IAsyncResult BeginClose(System.AsyncCallback callback, object state);
        System.IAsyncResult BeginClose(System.TimeSpan timeout, System.AsyncCallback callback, object state);
        System.IAsyncResult BeginOpen(System.AsyncCallback callback, object state);
        System.IAsyncResult BeginOpen(System.TimeSpan timeout, System.AsyncCallback callback, object state);
        void Close();
        void Close(System.TimeSpan timeout);
        void EndClose(System.IAsyncResult result);
        void EndOpen(System.IAsyncResult result);
        void Open();
        void Open(System.TimeSpan timeout);
    }
    public partial interface IContextChannel : System.ServiceModel.Channels.IChannel, System.ServiceModel.ICommunicationObject, System.ServiceModel.IExtensibleObject<System.ServiceModel.IContextChannel>
    {
        bool AllowOutputBatching { get; set; }
        System.ServiceModel.Channels.IInputSession InputSession { get; }
        System.ServiceModel.EndpointAddress LocalAddress { get; }
        System.TimeSpan OperationTimeout { get; set; }
        System.ServiceModel.Channels.IOutputSession OutputSession { get; }
        System.ServiceModel.EndpointAddress RemoteAddress { get; }
        string SessionId { get; }
    }
    public partial interface IDefaultCommunicationTimeouts
    {
        System.TimeSpan CloseTimeout { get; }
        System.TimeSpan OpenTimeout { get; }
        System.TimeSpan ReceiveTimeout { get; }
        System.TimeSpan SendTimeout { get; }
    }
    public partial interface IExtensibleObject<T> where T : System.ServiceModel.IExtensibleObject<T>
    {
        System.ServiceModel.IExtensionCollection<T> Extensions { get; }
    }
    public partial interface IExtension<T> where T : System.ServiceModel.IExtensibleObject<T>
    {
        void Attach(T owner);
        void Detach(T owner);
    }
    public partial interface IExtensionCollection<T> : System.Collections.Generic.ICollection<System.ServiceModel.IExtension<T>>, System.Collections.Generic.IEnumerable<System.ServiceModel.IExtension<T>>, System.Collections.IEnumerable where T : System.ServiceModel.IExtensibleObject<T>
    {
        E Find<E>();
        System.Collections.ObjectModel.Collection<E> FindAll<E>();
    }
    public enum ImpersonationOption
    {
        Allowed = 1,
        NotAllowed = 0,
        Required = 2,
    }
    public partial class InstanceContext
    {
        public InstanceContext(object dummy) { }
    }
    public enum InstanceContextMode
    {
        PerCall = 1,
        PerSession = 0,
        Single = 2,
    }
    public partial class InvalidMessageContractException : System.SystemException
    {
        public InvalidMessageContractException() { }
        protected InvalidMessageContractException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public InvalidMessageContractException(string msg) { }
        public InvalidMessageContractException(string msg, System.Exception inner) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(384), Inherited=false)]
    public partial class MessageBodyMemberAttribute : System.ServiceModel.MessageContractMemberAttribute
    {
        public MessageBodyMemberAttribute() { }
        public int Order { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(12))]
    public sealed partial class MessageContractAttribute : System.Attribute
    {
        public MessageContractAttribute() { }
        public bool HasProtectionLevel { get { throw null; } }
        public bool IsWrapped { get { throw null; } set { } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { throw null; } set { } }
        public string WrapperName { get { throw null; } set { } }
        public string WrapperNamespace { get { throw null; } set { } }
    }
    public abstract partial class MessageContractMemberAttribute : System.Attribute
    {
        protected MessageContractMemberAttribute() { }
        public bool HasProtectionLevel { get { throw null; } }
        public string Name { get { throw null; } set { } }
        public string Namespace { get { throw null; } set { } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { throw null; } set { } }
    }
    public enum MessageCredentialType
    {
        Certificate = 3,
        IssuedToken = 4,
        None = 0,
        UserName = 2,
        Windows = 1,
    }
    public partial class MessageHeader<T>
    {
        public MessageHeader() { }
        public MessageHeader(T content) { }
        public MessageHeader(T content, bool must_understand, string actor, bool relay) { }
        public string Actor { get { throw null; } set { } }
        public T Content { get { throw null; } set { } }
        public bool MustUnderstand { get { throw null; } set { } }
        public bool Relay { get { throw null; } set { } }
        public System.ServiceModel.Channels.MessageHeader GetUntypedHeader(string name, string ns) { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(384), Inherited=false)]
    public sealed partial class MessageHeaderArrayAttribute : System.ServiceModel.MessageHeaderAttribute
    {
        public MessageHeaderArrayAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(384), Inherited=false)]
    public partial class MessageHeaderAttribute : System.ServiceModel.MessageContractMemberAttribute
    {
        public MessageHeaderAttribute() { }
        public string Actor { get { throw null; } set { } }
        public bool MustUnderstand { get { throw null; } set { } }
        public bool Relay { get { throw null; } set { } }
    }
    public partial class MessageHeaderException : System.ServiceModel.ProtocolException
    {
        public MessageHeaderException() { }
        protected MessageHeaderException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public MessageHeaderException(string msg) { }
        public MessageHeaderException(string message, bool isDuplicate) { }
        public MessageHeaderException(string msg, System.Exception inner) { }
        public MessageHeaderException(string message, string headerName, string ns) { }
        public MessageHeaderException(string message, string headerName, string ns, bool isDuplicate) { }
        public MessageHeaderException(string message, string headerName, string ns, bool isDuplicate, System.Exception innerException) { }
        public MessageHeaderException(string message, string headerName, string ns, System.Exception innerException) { }
        public string HeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string HeaderNamespace { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool IsDuplicate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10240), Inherited=false)]
    public sealed partial class MessageParameterAttribute : System.Attribute
    {
        public MessageParameterAttribute() { }
        public string Name { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(384), Inherited=false)]
    public sealed partial class MessagePropertyAttribute : System.Attribute
    {
        public MessagePropertyAttribute() { }
        public string Name { get { throw null; } set { } }
    }
    public enum MsmqAuthenticationMode
    {
        Certificate = 2,
        None = 0,
        WindowsDomain = 1,
    }
    public enum MsmqEncryptionAlgorithm
    {
        Aes = 1,
        RC4Stream = 0,
    }
    public enum MsmqSecureHashAlgorithm
    {
        MD5 = 0,
        Sha1 = 1,
        Sha256 = 2,
        Sha512 = 3,
    }
    public partial class NetHttpBinding : System.ServiceModel.HttpBindingBase
    {
        public NetHttpBinding() { }
        public NetHttpBinding(System.ServiceModel.BasicHttpSecurityMode securityMode) { }
        public NetHttpBinding(System.ServiceModel.BasicHttpSecurityMode securityMode, bool reliableSessionEnabled) { }
        public NetHttpBinding(string configurationName) { }
        public System.ServiceModel.NetHttpMessageEncoding MessageEncoding { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.ServiceModel.OptionalReliableSession ReliableSession { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override string Scheme { get { throw null; } }
        public System.ServiceModel.BasicHttpSecurity Security { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.ServiceModel.Channels.WebSocketTransportSettings WebSocketSettings { get { throw null; } }
        public override System.ServiceModel.Channels.BindingElementCollection CreateBindingElements() { throw null; }
        public bool ShouldSerializeReliableSession() { throw null; }
        public bool ShouldSerializeSecurity() { throw null; }
    }
    public enum NetHttpMessageEncoding
    {
        Binary = 0,
        Mtom = 2,
        Text = 1,
    }
    public enum NetMsmqSecurityMode
    {
        Both = 3,
        Message = 2,
        None = 0,
        Transport = 1,
    }
    public enum NetNamedPipeSecurityMode
    {
        None = 0,
        Transport = 1,
    }
    public sealed partial class OperationContext : System.ServiceModel.IExtensibleObject<System.ServiceModel.OperationContext>
    {
        public OperationContext(System.ServiceModel.IContextChannel channel) { }
        public System.ServiceModel.IContextChannel Channel { get { throw null; } }
        public static System.ServiceModel.OperationContext Current { get { throw null; } set { } }
        public System.ServiceModel.IExtensionCollection<System.ServiceModel.OperationContext> Extensions { get { throw null; } }
        public System.ServiceModel.Channels.MessageHeaders IncomingMessageHeaders { get { throw null; } }
        public System.ServiceModel.Channels.MessageProperties IncomingMessageProperties { get { throw null; } }
        public System.ServiceModel.Channels.MessageVersion IncomingMessageVersion { get { throw null; } }
            public System.ServiceModel.InstanceContext InstanceContext { get { throw null; } }
        public bool IsUserContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.ServiceModel.Channels.MessageHeaders OutgoingMessageHeaders { get { throw null; } }
        public System.ServiceModel.Channels.MessageProperties OutgoingMessageProperties { get { throw null; } }
        public System.ServiceModel.Channels.RequestContext RequestContext { get { throw null; } set { } }
        public string SessionId { get { throw null; } }
        public event System.EventHandler OperationCompleted { add { } remove { } }
    }
    public sealed partial class OperationContextScope : System.IDisposable
    {
        public OperationContextScope(System.ServiceModel.IContextChannel channel) { }
        public OperationContextScope(System.ServiceModel.OperationContext context) { }
        public void Dispose() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64))]
    public sealed partial class OperationContractAttribute : System.Attribute
    {
        public OperationContractAttribute() { }
        public string Action { get { throw null; } set { } }
        public bool AsyncPattern { get { throw null; } set { } }
        public bool HasProtectionLevel { get { throw null; } }
        public bool IsInitiating { get { throw null; } set { } }
        public bool IsOneWay { get { throw null; } set { } }
        public bool IsTerminating { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { throw null; } set { } }
        public string ReplyAction { get { throw null; } set { } }
    }
    public enum OperationFormatStyle
    {
        Document = 0,
        Rpc = 1,
    }
    public enum OperationFormatUse
    {
        Encoded = 1,
        Literal = 0,
    }
    public partial class OptionalReliableSession : System.ServiceModel.ReliableSession
    {
        public OptionalReliableSession() { }
        public OptionalReliableSession(System.ServiceModel.Channels.ReliableSessionBindingElement binding) { }
        public bool Enabled { get { throw null; } set { } }
    }
    public partial class PeerHopCountAttribute
    {
        public PeerHopCountAttribute() { }
    }
    public enum PeerMessageOrigination
    {
        Local = 0,
        Remote = 1,
    }
    public enum PeerMessagePropagation
    {
        Local = 1,
        LocalAndRemote = 3,
        None = 0,
        Remote = 2,
    }
    public enum PeerTransportCredentialType
    {
        Certificate = 1,
        Password = 0,
    }
    public partial class ProtocolException : System.ServiceModel.CommunicationException
    {
        public ProtocolException() { }
        protected ProtocolException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ProtocolException(string msg) { }
        public ProtocolException(string msg, System.Exception inner) { }
    }
    public enum QueuedDeliveryRequirementsMode
    {
        Allowed = 0,
        NotAllowed = 2,
        Required = 1,
    }
    public enum QueueTransferProtocol
    {
        Native = 0,
        Srmp = 1,
        SrmpSecure = 2,
    }
    public partial class QuotaExceededException : System.SystemException
    {
        public QuotaExceededException() { }
        protected QuotaExceededException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public QuotaExceededException(string msg) { }
        public QuotaExceededException(string msg, System.Exception inner) { }
    }
    public enum ReceiveErrorHandling
    {
        Drop = 1,
        Fault = 0,
        Move = 3,
        Reject = 2,
    }
    public enum ReleaseInstanceMode
    {
        AfterCall = 2,
        BeforeAndAfterCall = 3,
        BeforeCall = 1,
        None = 0,
    }
    public abstract partial class ReliableMessagingVersion
    {
        protected ReliableMessagingVersion() { }
        public static System.ServiceModel.ReliableMessagingVersion Default { get { throw null; } }
        public static System.ServiceModel.ReliableMessagingVersion WSReliableMessaging11 { get { throw null; } }
        public static System.ServiceModel.ReliableMessagingVersion WSReliableMessagingFebruary2005 { get { throw null; } }
    }
    public partial class ReliableSession
    {
        public ReliableSession() { }
        public ReliableSession(System.ServiceModel.Channels.ReliableSessionBindingElement binding) { }
        public System.TimeSpan InactivityTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Ordered { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public enum SecurityMode
    {
        Message = 2,
        None = 0,
        Transport = 1,
        TransportWithMessageCredential = 3,
    }
    public partial class ServerTooBusyException : System.ServiceModel.CommunicationException
    {
        public ServerTooBusyException() { }
        protected ServerTooBusyException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ServerTooBusyException(string msg) { }
        public ServerTooBusyException(string msg, System.Exception inner) { }
    }
    public partial class ServiceActivationException : System.ServiceModel.CommunicationException
    {
        public ServiceActivationException() { }
        protected ServiceActivationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ServiceActivationException(string msg) { }
        public ServiceActivationException(string msg, System.Exception inner) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1028), Inherited=false)]
    public sealed partial class ServiceContractAttribute : System.Attribute
    {
        public ServiceContractAttribute() { }
        public System.Type CallbackContract { get { throw null; } set { } }
        public string ConfigurationName { get { throw null; } set { } }
        public bool HasProtectionLevel { get { throw null; } }
        public string Name { get { throw null; } set { } }
        public string Namespace { get { throw null; } set { } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { throw null; } set { } }
        public System.ServiceModel.SessionMode SessionMode { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1092), AllowMultiple=true, Inherited=true)]
    public sealed partial class ServiceKnownTypeAttribute : System.Attribute
    {
        public ServiceKnownTypeAttribute(string methodName) { }
        public ServiceKnownTypeAttribute(string methodName, System.Type declaringType) { }
        public ServiceKnownTypeAttribute(System.Type type) { }
        public System.Type DeclaringType { get { throw null; } }
        public string MethodName { get { throw null; } }
        public System.Type Type { get { throw null; } }
    }
    public enum SessionMode
    {
        Allowed = 0,
        NotAllowed = 2,
        Required = 1,
    }
    public enum TcpClientCredentialType
    {
        Certificate = 2,
        None = 0,
        Windows = 1,
    }
    public enum TransactionFlowOption
    {
        Allowed = 1,
        Mandatory = 2,
        NotAllowed = 0,
    }
    public enum TransferMode
    {
        Buffered = 0,
        Streamed = 1,
        StreamedRequest = 2,
        StreamedResponse = 3,
    }
    public sealed partial class UnknownMessageReceivedEventArgs : System.EventArgs
    {
        internal UnknownMessageReceivedEventArgs() { }
        public System.ServiceModel.Channels.Message Message { get { throw null; } }
    }
    public partial class UriSchemeKeyedCollection : System.Collections.Generic.SynchronizedKeyedCollection<string, System.Uri>
    {
        public UriSchemeKeyedCollection(params System.Uri[] uris) { }
        protected override string GetKeyForItem(System.Uri item) { throw null; }
        protected override void InsertItem(int index, System.Uri item) { }
        protected override void SetItem(int index, System.Uri item) { }
    }
    public enum WSDualHttpSecurityMode
    {
        Message = 1,
        None = 0,
    }
    public enum WSFederationHttpSecurityMode
    {
        Message = 1,
        None = 0,
        TransportWithMessageCredential = 2,
    }
    public enum WSMessageEncoding
    {
        Mtom = 1,
        Text = 0,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1092), Inherited=false)]
    public sealed partial class XmlSerializerFormatAttribute : System.Attribute
    {
        public XmlSerializerFormatAttribute() { }
        public System.ServiceModel.OperationFormatStyle Style { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public bool SupportFaults { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.ServiceModel.OperationFormatUse Use { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace System.ServiceModel.Activation
{
    public enum AspNetCompatibilityRequirementsMode
    {
        Allowed = 1,
        NotAllowed = 0,
        Required = 2,
    }
}
namespace System.ServiceModel.Activation.Configuration
{
    public partial class Dummy
    {
        public Dummy() { }
    }
}
namespace System.ServiceModel.Channels
{
    public abstract partial class AddressHeader
    {
        protected AddressHeader() { }
        public abstract string Name { get; }
        public abstract string Namespace { get; }
        public static System.ServiceModel.Channels.AddressHeader CreateAddressHeader(object value) { throw null; }
        public static System.ServiceModel.Channels.AddressHeader CreateAddressHeader(object value, System.Runtime.Serialization.XmlObjectSerializer formatter) { throw null; }
        public static System.ServiceModel.Channels.AddressHeader CreateAddressHeader(string name, string ns, object value) { throw null; }
        public static System.ServiceModel.Channels.AddressHeader CreateAddressHeader(string name, string ns, object value, System.Runtime.Serialization.XmlObjectSerializer formatter) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public virtual System.Xml.XmlDictionaryReader GetAddressHeaderReader() { throw null; }
        public override int GetHashCode() { throw null; }
        public T GetValue<T>() { throw null; }
        public T GetValue<T>(System.Runtime.Serialization.XmlObjectSerializer formatter) { throw null; }
        protected abstract void OnWriteAddressHeaderContents(System.Xml.XmlDictionaryWriter writer);
        protected virtual void OnWriteStartAddressHeader(System.Xml.XmlDictionaryWriter writer) { }
        public System.ServiceModel.Channels.MessageHeader ToMessageHeader() { throw null; }
        public void WriteAddressHeader(System.Xml.XmlDictionaryWriter writer) { }
        public void WriteAddressHeader(System.Xml.XmlWriter writer) { }
        public void WriteAddressHeaderContents(System.Xml.XmlDictionaryWriter writer) { }
        public void WriteStartAddressHeader(System.Xml.XmlDictionaryWriter writer) { }
    }
    public sealed partial class AddressHeaderCollection : System.Collections.ObjectModel.ReadOnlyCollection<System.ServiceModel.Channels.AddressHeader>
    {
        public AddressHeaderCollection() : base (default(System.Collections.Generic.IList<System.ServiceModel.Channels.AddressHeader>)) { }
        public AddressHeaderCollection(System.Collections.Generic.IEnumerable<System.ServiceModel.Channels.AddressHeader> headers) : base (default(System.Collections.Generic.IList<System.ServiceModel.Channels.AddressHeader>)) { }
        public void AddHeadersTo(System.ServiceModel.Channels.Message message) { }
        public System.ServiceModel.Channels.AddressHeader[] FindAll(string name, string ns) { throw null; }
        public System.ServiceModel.Channels.AddressHeader FindHeader(string name, string ns) { throw null; }
    }
    public sealed partial class AddressingVersion
    {
        internal AddressingVersion() { }
        public static System.ServiceModel.Channels.AddressingVersion None { get { throw null; } }
        public static System.ServiceModel.Channels.AddressingVersion WSAddressing10 { get { throw null; } }
        public static System.ServiceModel.Channels.AddressingVersion WSAddressingAugust2004 { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public sealed partial class BinaryMessageEncodingBindingElement : System.ServiceModel.Channels.MessageEncodingBindingElement, System.ServiceModel.Description.IPolicyExportExtension, System.ServiceModel.Description.IWsdlExportExtension
    {
        public BinaryMessageEncodingBindingElement() { }
            public System.ServiceModel.Channels.CompressionFormat CompressionFormat { get { throw null; } set { } }
        public int MaxReadPoolSize { get { throw null; } set { } }
        public int MaxSessionSize { get { throw null; } set { } }
        public int MaxWritePoolSize { get { throw null; } set { } }
        public override System.ServiceModel.Channels.MessageVersion MessageVersion { get { throw null; } set { } }
        public System.Xml.XmlDictionaryReaderQuotas ReaderQuotas { get { throw null; } set { } }
        public override System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { throw null; }
        public override System.ServiceModel.Channels.BindingElement Clone() { throw null; }
        public override System.ServiceModel.Channels.MessageEncoderFactory CreateMessageEncoderFactory() { throw null; }
        public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) { throw null; }
    }
    public abstract partial class Binding : System.ServiceModel.IDefaultCommunicationTimeouts
    {
        protected Binding() { }
        protected Binding(string name, string ns) { }
        public System.TimeSpan CloseTimeout { get { throw null; } set { } }
        public System.ServiceModel.Channels.MessageVersion MessageVersion { get { throw null; } }
        public string Name { get { throw null; } set { } }
        public string Namespace { get { throw null; } set { } }
        public System.TimeSpan OpenTimeout { get { throw null; } set { } }
        public System.TimeSpan ReceiveTimeout { get { throw null; } set { } }
        public abstract string Scheme { get; }
        public System.TimeSpan SendTimeout { get { throw null; } set { } }
        public System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(params object[] parameters) { throw null; }
        public virtual System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingParameterCollection parameters) { throw null; }
        public bool CanBuildChannelFactory<TChannel>(params object[] parameters) { throw null; }
        public virtual bool CanBuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingParameterCollection parameters) { throw null; }
        public abstract System.ServiceModel.Channels.BindingElementCollection CreateBindingElements();
        public T GetProperty<T>(System.ServiceModel.Channels.BindingParameterCollection parameters) where T : class { throw null; }
    }
    public partial class BindingContext
    {
        public BindingContext(System.ServiceModel.Channels.CustomBinding binding, System.ServiceModel.Channels.BindingParameterCollection parms) { }
        public BindingContext(System.ServiceModel.Channels.CustomBinding binding, System.ServiceModel.Channels.BindingParameterCollection parameters, System.Uri listenUriBaseAddress, string listenUriRelativeAddress, System.ServiceModel.Description.ListenUriMode listenUriMode) { }
        public System.ServiceModel.Channels.CustomBinding Binding { get { throw null; } }
        public System.ServiceModel.Channels.BindingParameterCollection BindingParameters { get { throw null; } }
        public System.Uri ListenUriBaseAddress { get { throw null; } set { } }
        public System.ServiceModel.Description.ListenUriMode ListenUriMode { get { throw null; } set { } }
        public string ListenUriRelativeAddress { get { throw null; } set { } }
        public System.ServiceModel.Channels.BindingElementCollection RemainingBindingElements { get { throw null; } }
        public System.ServiceModel.Channels.IChannelFactory<TChannel> BuildInnerChannelFactory<TChannel>() { throw null; }
        public bool CanBuildInnerChannelFactory<TChannel>() { throw null; }
        public System.ServiceModel.Channels.BindingContext Clone() { throw null; }
        public T GetInnerProperty<T>() where T : class { throw null; }
    }
    public abstract partial class BindingElement
    {
        protected BindingElement() { }
            protected BindingElement(System.ServiceModel.Channels.BindingElement other) { }
        public virtual System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { throw null; }
        public virtual bool CanBuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { throw null; }
        public abstract System.ServiceModel.Channels.BindingElement Clone();
        public abstract T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) where T : class;
    }
    public partial class BindingElementCollection : System.Collections.ObjectModel.Collection<System.ServiceModel.Channels.BindingElement>
    {
        public BindingElementCollection() { }
        public BindingElementCollection(System.Collections.Generic.IEnumerable<System.ServiceModel.Channels.BindingElement> bindings) { }
        public BindingElementCollection(System.ServiceModel.Channels.BindingElement[] bindings) { }
        public void AddRange(params System.ServiceModel.Channels.BindingElement[] elements) { }
        public System.ServiceModel.Channels.BindingElementCollection Clone() { throw null; }
        public bool Contains(System.Type bindingElementType) { throw null; }
        public T Find<T>() { throw null; }
        public System.Collections.ObjectModel.Collection<T> FindAll<T>() { throw null; }
        protected override void InsertItem(int index, System.ServiceModel.Channels.BindingElement item) { }
        public T Remove<T>() { throw null; }
        public System.Collections.ObjectModel.Collection<T> RemoveAll<T>() { throw null; }
        protected override void SetItem(int index, System.ServiceModel.Channels.BindingElement item) { }
    }
    public partial class BindingParameterCollection : System.Collections.Generic.KeyedByTypeCollection<object>
    {
        public BindingParameterCollection() { }
        protected override System.Type GetKeyForItem(object item) { throw null; }
        protected override void InsertItem(int index, object item) { }
        protected override void SetItem(int index, object item) { }
    }
    public abstract partial class BodyWriter
    {
        protected BodyWriter(bool isBuffered) { }
        public bool IsBuffered { get { throw null; } }
        public System.ServiceModel.Channels.BodyWriter CreateBufferedCopy(int maxBufferSize) { throw null; }
        protected virtual System.ServiceModel.Channels.BodyWriter OnCreateBufferedCopy(int maxBufferSize) { throw null; }
        protected abstract void OnWriteBodyContents(System.Xml.XmlDictionaryWriter writer);
        public void WriteBodyContents(System.Xml.XmlDictionaryWriter writer) { }
    }
    public abstract partial class BufferManager
    {
        protected BufferManager() { }
        public abstract void Clear();
        public static System.ServiceModel.Channels.BufferManager CreateBufferManager(long maxBufferPoolSize, int maxBufferSize) { throw null; }
        public abstract void ReturnBuffer(byte[] buffer);
        public abstract byte[] TakeBuffer(int bufferSize);
    }
    public abstract partial class ChannelBase : System.ServiceModel.Channels.CommunicationObject, System.ServiceModel.Channels.IChannel, System.ServiceModel.ICommunicationObject, System.ServiceModel.IDefaultCommunicationTimeouts
    {
        protected ChannelBase(System.ServiceModel.Channels.ChannelManagerBase manager) { }
        protected internal override System.TimeSpan DefaultCloseTimeout { get { throw null; } }
        protected internal override System.TimeSpan DefaultOpenTimeout { get { throw null; } }
        protected internal System.TimeSpan DefaultReceiveTimeout { get { throw null; } }
        protected internal System.TimeSpan DefaultSendTimeout { get { throw null; } }
        protected System.ServiceModel.Channels.ChannelManagerBase Manager { get { throw null; } }
        System.TimeSpan System.ServiceModel.IDefaultCommunicationTimeouts.CloseTimeout { get { throw null; } }
        System.TimeSpan System.ServiceModel.IDefaultCommunicationTimeouts.OpenTimeout { get { throw null; } }
        System.TimeSpan System.ServiceModel.IDefaultCommunicationTimeouts.ReceiveTimeout { get { throw null; } }
        System.TimeSpan System.ServiceModel.IDefaultCommunicationTimeouts.SendTimeout { get { throw null; } }
        public virtual T GetProperty<T>() where T : class { throw null; }
        protected override void OnClosed() { }
    }
    public abstract partial class ChannelFactoryBase : System.ServiceModel.Channels.ChannelManagerBase, System.ServiceModel.Channels.IChannelFactory, System.ServiceModel.ICommunicationObject
    {
        protected ChannelFactoryBase() { }
        protected ChannelFactoryBase(System.ServiceModel.IDefaultCommunicationTimeouts timeouts) { }
        protected internal override System.TimeSpan DefaultCloseTimeout { get { throw null; } }
        protected internal override System.TimeSpan DefaultOpenTimeout { get { throw null; } }
        protected internal override System.TimeSpan DefaultReceiveTimeout { get { throw null; } }
        protected internal override System.TimeSpan DefaultSendTimeout { get { throw null; } }
        public virtual T GetProperty<T>() where T : class { throw null; }
        protected override void OnAbort() { }
        protected override System.IAsyncResult OnBeginClose(System.TimeSpan timeout, System.AsyncCallback callback, object state) { throw null; }
        protected override void OnClose(System.TimeSpan timeout) { }
        protected override void OnEndClose(System.IAsyncResult result) { }
    }
    public abstract partial class ChannelFactoryBase<TChannel> : System.ServiceModel.Channels.ChannelFactoryBase, System.ServiceModel.Channels.IChannelFactory, System.ServiceModel.Channels.IChannelFactory<TChannel>, System.ServiceModel.ICommunicationObject
    {
        protected ChannelFactoryBase() { }
        protected ChannelFactoryBase(System.ServiceModel.IDefaultCommunicationTimeouts timeouts) { }
        public TChannel CreateChannel(System.ServiceModel.EndpointAddress remoteAddress) { throw null; }
        public TChannel CreateChannel(System.ServiceModel.EndpointAddress remoteAddress, System.Uri via) { throw null; }
        protected override void OnAbort() { }
        protected override System.IAsyncResult OnBeginClose(System.TimeSpan timeout, System.AsyncCallback callback, object state) { throw null; }
        protected override void OnClose(System.TimeSpan timeout) { }
        protected abstract TChannel OnCreateChannel(System.ServiceModel.EndpointAddress remoteAddress, System.Uri via);
        protected override void OnEndClose(System.IAsyncResult result) { }
        protected void ValidateCreateChannel() { }
    }
    public abstract partial class ChannelManagerBase : System.ServiceModel.Channels.CommunicationObject, System.ServiceModel.ICommunicationObject, System.ServiceModel.IDefaultCommunicationTimeouts
    {
        protected ChannelManagerBase() { }
        protected internal abstract System.TimeSpan DefaultReceiveTimeout { get; }
        protected internal abstract System.TimeSpan DefaultSendTimeout { get; }
        System.TimeSpan System.ServiceModel.IDefaultCommunicationTimeouts.CloseTimeout { get { throw null; } }
        System.TimeSpan System.ServiceModel.IDefaultCommunicationTimeouts.OpenTimeout { get { throw null; } }
        System.TimeSpan System.ServiceModel.IDefaultCommunicationTimeouts.ReceiveTimeout { get { throw null; } }
        System.TimeSpan System.ServiceModel.IDefaultCommunicationTimeouts.SendTimeout { get { throw null; } }
    }
    public partial class ChannelParameterCollection : System.Collections.ObjectModel.Collection<object>
    {
        public ChannelParameterCollection() { }
        public ChannelParameterCollection(System.ServiceModel.Channels.IChannel channel) { }
        protected virtual System.ServiceModel.Channels.IChannel Channel { get { throw null; } }
        protected override void ClearItems() { }
        protected override void InsertItem(int index, object item) { }
        public void PropagateChannelParameters(System.ServiceModel.Channels.IChannel innerChannel) { }
        protected override void RemoveItem(int index) { }
        protected override void SetItem(int index, object item) { }
    }
    public partial class ChannelPoolSettings
    {
            public ChannelPoolSettings() { }
            public System.TimeSpan IdleTimeout { get { throw null; } set { } }
            public System.TimeSpan LeaseTimeout { get { throw null; } set { } }
            public int MaxOutboundChannelsPerEndpoint { get { throw null; } set { } }
    }
    public abstract partial class CommunicationObject : System.ServiceModel.ICommunicationObject
    {
        protected CommunicationObject() { }
        protected CommunicationObject(object mutex) { }
        protected internal abstract System.TimeSpan DefaultCloseTimeout { get; }
        protected internal abstract System.TimeSpan DefaultOpenTimeout { get; }
        protected bool IsDisposed { get { throw null; } }
        public System.ServiceModel.CommunicationState State { get { throw null; } }
        protected object ThisLock { get { throw null; } }
        public event System.EventHandler Closed { add { } remove { } }
        public event System.EventHandler Closing { add { } remove { } }
        public event System.EventHandler Faulted { add { } remove { } }
        public event System.EventHandler Opened { add { } remove { } }
        public event System.EventHandler Opening { add { } remove { } }
        public void Abort() { }
        public System.IAsyncResult BeginClose(System.AsyncCallback callback, object state) { throw null; }
        public System.IAsyncResult BeginClose(System.TimeSpan timeout, System.AsyncCallback callback, object state) { throw null; }
        public System.IAsyncResult BeginOpen(System.AsyncCallback callback, object state) { throw null; }
        public System.IAsyncResult BeginOpen(System.TimeSpan timeout, System.AsyncCallback callback, object state) { throw null; }
        public void Close() { }
        public void Close(System.TimeSpan timeout) { }
        public void EndClose(System.IAsyncResult result) { }
        public void EndOpen(System.IAsyncResult result) { }
        protected void Fault() { }
        protected virtual System.Type GetCommunicationObjectType() { throw null; }
        protected abstract void OnAbort();
        protected abstract System.IAsyncResult OnBeginClose(System.TimeSpan timeout, System.AsyncCallback callback, object state);
        protected abstract System.IAsyncResult OnBeginOpen(System.TimeSpan timeout, System.AsyncCallback callback, object state);
        protected abstract void OnClose(System.TimeSpan timeout);
        protected virtual void OnClosed() { }
        protected virtual void OnClosing() { }
        protected abstract void OnEndClose(System.IAsyncResult result);
        protected abstract void OnEndOpen(System.IAsyncResult result);
        protected virtual void OnFaulted() { }
        protected abstract void OnOpen(System.TimeSpan timeout);
        protected virtual void OnOpened() { }
        protected virtual void OnOpening() { }
        public void Open() { }
        public void Open(System.TimeSpan timeout) { }
        protected void ThrowIfDisposed() { }
        protected void ThrowIfDisposedOrImmutable() { }
        protected void ThrowIfDisposedOrNotOpen() { }
    }
    public enum CompressionFormat
    {
        Deflate = 2,
        GZip = 1,
        None = 0,
    }
    public partial class CustomBinding : System.ServiceModel.Channels.Binding
    {
        public CustomBinding() { }
        public CustomBinding(System.Collections.Generic.IEnumerable<System.ServiceModel.Channels.BindingElement> bindingElements) { }
        public CustomBinding(System.ServiceModel.Channels.Binding binding) { }
        public CustomBinding(params System.ServiceModel.Channels.BindingElement[] binding) { }
        public CustomBinding(string name) { }
        public CustomBinding(string name, string ns, params System.ServiceModel.Channels.BindingElement[] binding) { }
        public System.ServiceModel.Channels.BindingElementCollection Elements { get { throw null; } }
        public override string Scheme { get { throw null; } }
        public override System.ServiceModel.Channels.BindingElementCollection CreateBindingElements() { throw null; }
    }
    public abstract partial class FaultConverter
    {
        protected FaultConverter() { }
        public static System.ServiceModel.Channels.FaultConverter GetDefaultFaultConverter(System.ServiceModel.Channels.MessageVersion version) { throw null; }
            protected abstract bool OnTryCreateException(System.ServiceModel.Channels.Message message, System.ServiceModel.Channels.MessageFault fault, out System.Exception error);
            protected abstract bool OnTryCreateFaultMessage(System.Exception error, out System.ServiceModel.Channels.Message message);
        public bool TryCreateException(System.ServiceModel.Channels.Message message, System.ServiceModel.Channels.MessageFault fault, out System.Exception error) { throw null; }
        public bool TryCreateFaultMessage(System.Exception error, out System.ServiceModel.Channels.Message message) { throw null; }
    }
    [System.ObsoleteAttribute("Use AllowCookies.")]
    public partial class HttpCookieContainerBindingElement : System.ServiceModel.Channels.BindingElement
    {
        public HttpCookieContainerBindingElement() { }
        protected HttpCookieContainerBindingElement(System.ServiceModel.Channels.HttpCookieContainerBindingElement elementToBeCloned) { }
        public override System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { throw null; }
        public override System.ServiceModel.Channels.BindingElement Clone() { throw null; }
        public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) { throw null; }
    }
    public sealed partial class HttpRequestMessageProperty : System.ServiceModel.Channels.IMessageProperty
    {
        public HttpRequestMessageProperty() { }
        public System.Net.WebHeaderCollection Headers { get { throw null; } }
        public string Method { get { throw null; } set { } }
        public static string Name { get { throw null; } }
        public string QueryString { get { throw null; } set { } }
        public bool SuppressEntityBody { get { throw null; } set { } }
        System.ServiceModel.Channels.IMessageProperty System.ServiceModel.Channels.IMessageProperty.CreateCopy() { throw null; }
    }
    public sealed partial class HttpResponseMessageProperty : System.ServiceModel.Channels.IMessageProperty
    {
        public HttpResponseMessageProperty() { }
        public System.Net.WebHeaderCollection Headers { get { throw null; } }
        public static string Name { get { throw null; } }
        public System.Net.HttpStatusCode StatusCode { get { throw null; } set { } }
        public string StatusDescription { get { throw null; } set { } }
        public bool SuppressEntityBody { get { throw null; } set { } }
        System.ServiceModel.Channels.IMessageProperty System.ServiceModel.Channels.IMessageProperty.CreateCopy() { throw null; }
    }
    public partial class HttpsTransportBindingElement : System.ServiceModel.Channels.HttpTransportBindingElement, System.ServiceModel.Channels.ITransportTokenAssertionProvider, System.ServiceModel.Description.IPolicyExportExtension, System.ServiceModel.Description.IWsdlExportExtension
    {
        public HttpsTransportBindingElement() { }
        protected HttpsTransportBindingElement(System.ServiceModel.Channels.HttpsTransportBindingElement other) { }
        public bool RequireClientCertificate { get { throw null; } set { } }
        public override string Scheme { get { throw null; } }
        public override System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { throw null; }
        public override System.ServiceModel.Channels.BindingElement Clone() { throw null; }
        public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) { throw null; }
    }
    public partial class HttpTransportBindingElement : System.ServiceModel.Channels.TransportBindingElement, System.ServiceModel.Description.IPolicyExportExtension, System.ServiceModel.Description.IWsdlExportExtension
    {
        public HttpTransportBindingElement() { }
        protected HttpTransportBindingElement(System.ServiceModel.Channels.HttpTransportBindingElement other) { }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool AllowCookies { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Net.AuthenticationSchemes)(32768))]
        public System.Net.AuthenticationSchemes AuthenticationScheme { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool BypassProxyOnLocal { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
            public bool DecompressionEnabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy ExtendedProtectionPolicy { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [System.ComponentModel.DefaultValueAttribute((System.ServiceModel.HostNameComparisonMode)(0))]
        public System.ServiceModel.HostNameComparisonMode HostNameComparisonMode { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool KeepAliveEnabled { get { throw null; } set { } }
            [System.ObsoleteAttribute("Use ExtendedProtectionPolicy")]
        public object LegacyExtendedProtectionPolicy { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [System.ComponentModel.DefaultValueAttribute(65536)]
        public int MaxBufferSize { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.ComponentModel.TypeConverterAttribute(typeof(System.UriTypeConverter))]
        public System.Uri ProxyAddress { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Net.AuthenticationSchemes)(32768))]
        public System.Net.AuthenticationSchemes ProxyAuthenticationScheme { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        public string Realm { get { throw null; } set { } }
        public override string Scheme { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute((System.ServiceModel.TransferMode)(0))]
        public System.ServiceModel.TransferMode TransferMode { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool UnsafeConnectionNtlmAuthentication { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool UseDefaultWebProxy { get { throw null; } set { } }
        public System.ServiceModel.Channels.WebSocketTransportSettings WebSocketSettings { get { throw null; } set { } }
        public override System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { throw null; }
        public override bool CanBuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { throw null; }
        public override System.ServiceModel.Channels.BindingElement Clone() { throw null; }
        public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) { throw null; }
    }
    public partial interface IBindingDeliveryCapabilities
    {
        bool AssuresOrderedDelivery { get; }
        bool QueuedDelivery { get; }
    }
    public partial interface IBindingRuntimePreferences
    {
        bool ReceiveSynchronously { get; }
    }
    public partial interface IChannel : System.ServiceModel.ICommunicationObject
    {
        T GetProperty<T>() where T : class;
    }
    public partial interface IChannelFactory : System.ServiceModel.ICommunicationObject
    {
        T GetProperty<T>() where T : class;
    }
    public partial interface IChannelFactory<TChannel> : System.ServiceModel.Channels.IChannelFactory, System.ServiceModel.ICommunicationObject
    {
        TChannel CreateChannel(System.ServiceModel.EndpointAddress to);
        TChannel CreateChannel(System.ServiceModel.EndpointAddress to, System.Uri via);
    }
    public partial interface IDuplexChannel : System.ServiceModel.Channels.IChannel, System.ServiceModel.Channels.IInputChannel, System.ServiceModel.Channels.IOutputChannel, System.ServiceModel.ICommunicationObject
    {
    }
    public partial interface IDuplexSession : System.ServiceModel.Channels.IInputSession, System.ServiceModel.Channels.IOutputSession, System.ServiceModel.Channels.ISession
    {
        System.IAsyncResult BeginCloseOutputSession(System.AsyncCallback callback, object state);
        System.IAsyncResult BeginCloseOutputSession(System.TimeSpan timeout, System.AsyncCallback callback, object state);
        void CloseOutputSession();
        void CloseOutputSession(System.TimeSpan timeout);
        void EndCloseOutputSession(System.IAsyncResult result);
    }
    public partial interface IDuplexSessionChannel : System.ServiceModel.Channels.IChannel, System.ServiceModel.Channels.IDuplexChannel, System.ServiceModel.Channels.IInputChannel, System.ServiceModel.Channels.IOutputChannel, System.ServiceModel.Channels.ISessionChannel<System.ServiceModel.Channels.IDuplexSession>, System.ServiceModel.ICommunicationObject
    {
    }
    public partial interface IHttpCookieContainerManager
    {
        System.Net.CookieContainer CookieContainer { get; set; }
    }
    public partial interface IInputChannel : System.ServiceModel.Channels.IChannel, System.ServiceModel.ICommunicationObject
    {
        System.ServiceModel.EndpointAddress LocalAddress { get; }
        System.IAsyncResult BeginReceive(System.AsyncCallback callback, object state);
        System.IAsyncResult BeginReceive(System.TimeSpan timeout, System.AsyncCallback callback, object state);
        System.IAsyncResult BeginTryReceive(System.TimeSpan timeout, System.AsyncCallback callback, object state);
        System.IAsyncResult BeginWaitForMessage(System.TimeSpan timeout, System.AsyncCallback callback, object state);
        System.ServiceModel.Channels.Message EndReceive(System.IAsyncResult result);
        bool EndTryReceive(System.IAsyncResult result, out System.ServiceModel.Channels.Message message);
        bool EndWaitForMessage(System.IAsyncResult result);
        System.ServiceModel.Channels.Message Receive();
        System.ServiceModel.Channels.Message Receive(System.TimeSpan timeout);
        bool TryReceive(System.TimeSpan timeout, out System.ServiceModel.Channels.Message message);
        bool WaitForMessage(System.TimeSpan timeout);
    }
    public partial interface IInputSession : System.ServiceModel.Channels.ISession
    {
    }
    public partial interface IInputSessionChannel : System.ServiceModel.Channels.IChannel, System.ServiceModel.Channels.IInputChannel, System.ServiceModel.Channels.ISessionChannel<System.ServiceModel.Channels.IInputSession>, System.ServiceModel.ICommunicationObject
    {
    }
    public partial interface IMessageProperty
    {
        System.ServiceModel.Channels.IMessageProperty CreateCopy();
    }
    public partial interface IOutputChannel : System.ServiceModel.Channels.IChannel, System.ServiceModel.ICommunicationObject
    {
        System.ServiceModel.EndpointAddress RemoteAddress { get; }
        System.Uri Via { get; }
        System.IAsyncResult BeginSend(System.ServiceModel.Channels.Message message, System.AsyncCallback callback, object state);
        System.IAsyncResult BeginSend(System.ServiceModel.Channels.Message message, System.TimeSpan timeout, System.AsyncCallback callback, object state);
        void EndSend(System.IAsyncResult result);
        void Send(System.ServiceModel.Channels.Message message);
        void Send(System.ServiceModel.Channels.Message message, System.TimeSpan timeout);
    }
    public partial interface IOutputSession : System.ServiceModel.Channels.ISession
    {
    }
    public partial interface IOutputSessionChannel : System.ServiceModel.Channels.IChannel, System.ServiceModel.Channels.IOutputChannel, System.ServiceModel.Channels.ISessionChannel<System.ServiceModel.Channels.IOutputSession>, System.ServiceModel.ICommunicationObject
    {
    }
    public partial interface IRequestChannel : System.ServiceModel.Channels.IChannel, System.ServiceModel.ICommunicationObject
    {
        System.ServiceModel.EndpointAddress RemoteAddress { get; }
        System.Uri Via { get; }
        System.IAsyncResult BeginRequest(System.ServiceModel.Channels.Message message, System.AsyncCallback callback, object state);
        System.IAsyncResult BeginRequest(System.ServiceModel.Channels.Message message, System.TimeSpan timeout, System.AsyncCallback callback, object state);
        System.ServiceModel.Channels.Message EndRequest(System.IAsyncResult result);
        System.ServiceModel.Channels.Message Request(System.ServiceModel.Channels.Message message);
        System.ServiceModel.Channels.Message Request(System.ServiceModel.Channels.Message message, System.TimeSpan timeout);
    }
    public partial interface IRequestSessionChannel : System.ServiceModel.Channels.IChannel, System.ServiceModel.Channels.IRequestChannel, System.ServiceModel.Channels.ISessionChannel<System.ServiceModel.Channels.IOutputSession>, System.ServiceModel.ICommunicationObject
    {
    }
    public partial interface ISecurityCapabilities
    {
        System.Net.Security.ProtectionLevel SupportedRequestProtectionLevel { get; }
        System.Net.Security.ProtectionLevel SupportedResponseProtectionLevel { get; }
        bool SupportsClientAuthentication { get; }
        bool SupportsClientWindowsIdentity { get; }
        bool SupportsServerAuthentication { get; }
    }
    public partial interface ISession
    {
        string Id { get; }
    }
    public partial interface ISessionChannel<TSession> where TSession : System.ServiceModel.Channels.ISession
    {
        TSession Session { get; }
    }
    public partial interface ITransportTokenAssertionProvider
    {
    }
    public sealed partial class LocalClientSecuritySettings
    {
        public LocalClientSecuritySettings() { }
        public System.TimeSpan MaxClockSkew { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan ReplayWindow { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan TimestampValidityDuration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.ServiceModel.Channels.LocalClientSecuritySettings Clone() { throw null; }
    }
    public abstract partial class Message : System.IDisposable
    {
        protected Message() { }
        public abstract System.ServiceModel.Channels.MessageHeaders Headers { get; }
        protected bool IsDisposed { get { throw null; } }
        public virtual bool IsEmpty { get { throw null; } }
        public virtual bool IsFault { get { throw null; } }
        public abstract System.ServiceModel.Channels.MessageProperties Properties { get; }
        public System.ServiceModel.Channels.MessageState State { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public abstract System.ServiceModel.Channels.MessageVersion Version { get; }
        public void Close() { }
        public System.ServiceModel.Channels.MessageBuffer CreateBufferedCopy(int maxBufferSize) { throw null; }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, System.ServiceModel.Channels.MessageFault fault, string action) { throw null; }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, System.ServiceModel.FaultCode code, string reason, object detail, string action) { throw null; }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, System.ServiceModel.FaultCode code, string reason, string action) { throw null; }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, string action) { throw null; }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, string action, object body) { throw null; }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, string action, object body, System.Runtime.Serialization.XmlObjectSerializer xmlFormatter) { throw null; }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, string action, System.ServiceModel.Channels.BodyWriter body) { throw null; }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, string action, System.Xml.XmlDictionaryReader body) { throw null; }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, string action, System.Xml.XmlReader body) { throw null; }
        public static System.ServiceModel.Channels.Message CreateMessage(System.Xml.XmlDictionaryReader envelopeReader, int maxSizeOfHeaders, System.ServiceModel.Channels.MessageVersion version) { throw null; }
        public static System.ServiceModel.Channels.Message CreateMessage(System.Xml.XmlReader envelopeReader, int maxSizeOfHeaders, System.ServiceModel.Channels.MessageVersion version) { throw null; }
        public T GetBody<T>() { throw null; }
        public T GetBody<T>(System.Runtime.Serialization.XmlObjectSerializer xmlFormatter) { throw null; }
        public string GetBodyAttribute(string localName, string ns) { throw null; }
        public System.Xml.XmlDictionaryReader GetReaderAtBodyContents() { throw null; }
        protected virtual void OnBodyToString(System.Xml.XmlDictionaryWriter writer) { }
        protected virtual void OnClose() { }
        protected virtual System.ServiceModel.Channels.MessageBuffer OnCreateBufferedCopy(int maxBufferSize) { throw null; }
        protected virtual T OnGetBody<T>(System.Xml.XmlDictionaryReader reader) { throw null; }
        protected virtual string OnGetBodyAttribute(string localName, string ns) { throw null; }
        protected virtual System.Xml.XmlDictionaryReader OnGetReaderAtBodyContents() { throw null; }
        protected abstract void OnWriteBodyContents(System.Xml.XmlDictionaryWriter writer);
        protected virtual void OnWriteMessage(System.Xml.XmlDictionaryWriter writer) { }
        protected virtual void OnWriteStartBody(System.Xml.XmlDictionaryWriter writer) { }
        protected virtual void OnWriteStartEnvelope(System.Xml.XmlDictionaryWriter writer) { }
        protected virtual void OnWriteStartHeaders(System.Xml.XmlDictionaryWriter writer) { }
        void System.IDisposable.Dispose() { }
        public override string ToString() { throw null; }
        public void WriteBody(System.Xml.XmlDictionaryWriter writer) { }
        public void WriteBody(System.Xml.XmlWriter writer) { }
        public void WriteBodyContents(System.Xml.XmlDictionaryWriter writer) { }
        public void WriteMessage(System.Xml.XmlDictionaryWriter writer) { }
        public void WriteMessage(System.Xml.XmlWriter writer) { }
        public void WriteStartBody(System.Xml.XmlDictionaryWriter writer) { }
        public void WriteStartBody(System.Xml.XmlWriter writer) { }
        public void WriteStartEnvelope(System.Xml.XmlDictionaryWriter writer) { }
    }
    public abstract partial class MessageBuffer : System.IDisposable
    {
        protected MessageBuffer() { }
        public abstract int BufferSize { get; }
        public virtual string MessageContentType { get { throw null; } }
        public abstract void Close();
        public abstract System.ServiceModel.Channels.Message CreateMessage();
        void System.IDisposable.Dispose() { }
        public virtual void WriteMessage(System.IO.Stream stream) { }
    }
    public abstract partial class MessageEncoder
    {
        protected MessageEncoder() { }
        public abstract string ContentType { get; }
        public abstract string MediaType { get; }
        public abstract System.ServiceModel.Channels.MessageVersion MessageVersion { get; }
            public virtual T GetProperty<T>() where T : class { throw null; }
        public virtual bool IsContentTypeSupported(string contentType) { throw null; }
        public System.ServiceModel.Channels.Message ReadMessage(System.ArraySegment<byte> buffer, System.ServiceModel.Channels.BufferManager bufferManager) { throw null; }
        public abstract System.ServiceModel.Channels.Message ReadMessage(System.ArraySegment<byte> buffer, System.ServiceModel.Channels.BufferManager bufferManager, string contentType);
        public System.ServiceModel.Channels.Message ReadMessage(System.IO.Stream stream, int maxSizeOfHeaders) { throw null; }
        public abstract System.ServiceModel.Channels.Message ReadMessage(System.IO.Stream stream, int maxSizeOfHeaders, string contentType);
        public override string ToString() { throw null; }
        public System.ArraySegment<byte> WriteMessage(System.ServiceModel.Channels.Message message, int maxMessageSize, System.ServiceModel.Channels.BufferManager bufferManager) { throw null; }
        public abstract System.ArraySegment<byte> WriteMessage(System.ServiceModel.Channels.Message message, int maxMessageSize, System.ServiceModel.Channels.BufferManager bufferManager, int messageOffset);
        public abstract void WriteMessage(System.ServiceModel.Channels.Message message, System.IO.Stream stream);
    }
    public abstract partial class MessageEncoderFactory
    {
        protected MessageEncoderFactory() { }
        public abstract System.ServiceModel.Channels.MessageEncoder Encoder { get; }
        public abstract System.ServiceModel.Channels.MessageVersion MessageVersion { get; }
        public virtual System.ServiceModel.Channels.MessageEncoder CreateSessionEncoder() { throw null; }
    }
    public abstract partial class MessageEncodingBindingElement : System.ServiceModel.Channels.BindingElement
    {
        public MessageEncodingBindingElement() { }
            public MessageEncodingBindingElement(System.ServiceModel.Channels.MessageEncodingBindingElement source) { }
        public abstract System.ServiceModel.Channels.MessageVersion MessageVersion { get; set; }
        public abstract System.ServiceModel.Channels.MessageEncoderFactory CreateMessageEncoderFactory();
        public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext ctx) { throw null; }
    }
    public abstract partial class MessageFault
    {
        protected MessageFault() { }
        public virtual string Actor { get { throw null; } }
        public abstract System.ServiceModel.FaultCode Code { get; }
        public abstract bool HasDetail { get; }
        public virtual string Node { get { throw null; } }
        public abstract System.ServiceModel.FaultReason Reason { get; }
        public static System.ServiceModel.Channels.MessageFault CreateFault(System.ServiceModel.Channels.Message message, int maxBufferSize) { throw null; }
        public static System.ServiceModel.Channels.MessageFault CreateFault(System.ServiceModel.FaultCode code, System.ServiceModel.FaultReason reason) { throw null; }
        public static System.ServiceModel.Channels.MessageFault CreateFault(System.ServiceModel.FaultCode code, System.ServiceModel.FaultReason reason, object detail) { throw null; }
        public static System.ServiceModel.Channels.MessageFault CreateFault(System.ServiceModel.FaultCode code, System.ServiceModel.FaultReason reason, object detail, System.Runtime.Serialization.XmlObjectSerializer formatter) { throw null; }
        public static System.ServiceModel.Channels.MessageFault CreateFault(System.ServiceModel.FaultCode code, System.ServiceModel.FaultReason reason, object detail, System.Runtime.Serialization.XmlObjectSerializer formatter, string actor) { throw null; }
        public static System.ServiceModel.Channels.MessageFault CreateFault(System.ServiceModel.FaultCode code, System.ServiceModel.FaultReason reason, object detail, System.Runtime.Serialization.XmlObjectSerializer formatter, string actor, string node) { throw null; }
        public static System.ServiceModel.Channels.MessageFault CreateFault(System.ServiceModel.FaultCode code, string reason) { throw null; }
        public T GetDetail<T>() { throw null; }
        public T GetDetail<T>(System.Runtime.Serialization.XmlObjectSerializer formatter) { throw null; }
        public System.Xml.XmlDictionaryReader GetReaderAtDetailContents() { throw null; }
        protected virtual System.Xml.XmlDictionaryReader OnGetReaderAtDetailContents() { throw null; }
        protected virtual void OnWriteDetail(System.Xml.XmlDictionaryWriter writer, System.ServiceModel.EnvelopeVersion version) { }
        protected abstract void OnWriteDetailContents(System.Xml.XmlDictionaryWriter writer);
        protected virtual void OnWriteStartDetail(System.Xml.XmlDictionaryWriter writer, System.ServiceModel.EnvelopeVersion version) { }
        public void WriteTo(System.Xml.XmlDictionaryWriter writer, System.ServiceModel.EnvelopeVersion version) { }
        public void WriteTo(System.Xml.XmlWriter writer, System.ServiceModel.EnvelopeVersion version) { }
    }
    public abstract partial class MessageHeader : System.ServiceModel.Channels.MessageHeaderInfo
    {
        protected MessageHeader() { }
        public override string Actor { get { throw null; } }
        public override bool IsReferenceParameter { get { throw null; } }
        public override bool MustUnderstand { get { throw null; } }
        public override bool Relay { get { throw null; } }
        public static System.ServiceModel.Channels.MessageHeader CreateHeader(string name, string ns, object value) { throw null; }
        public static System.ServiceModel.Channels.MessageHeader CreateHeader(string name, string ns, object value, bool must_understand) { throw null; }
        public static System.ServiceModel.Channels.MessageHeader CreateHeader(string name, string ns, object value, bool must_understand, string actor) { throw null; }
        public static System.ServiceModel.Channels.MessageHeader CreateHeader(string name, string ns, object value, bool must_understand, string actor, bool relay) { throw null; }
        public static System.ServiceModel.Channels.MessageHeader CreateHeader(string name, string ns, object value, System.Runtime.Serialization.XmlObjectSerializer formatter) { throw null; }
        public static System.ServiceModel.Channels.MessageHeader CreateHeader(string name, string ns, object value, System.Runtime.Serialization.XmlObjectSerializer formatter, bool must_understand) { throw null; }
        public static System.ServiceModel.Channels.MessageHeader CreateHeader(string name, string ns, object value, System.Runtime.Serialization.XmlObjectSerializer formatter, bool must_understand, string actor) { throw null; }
        public static System.ServiceModel.Channels.MessageHeader CreateHeader(string name, string ns, object value, System.Runtime.Serialization.XmlObjectSerializer formatter, bool must_understand, string actor, bool relay) { throw null; }
        public virtual bool IsMessageVersionSupported(System.ServiceModel.Channels.MessageVersion version) { throw null; }
        protected abstract void OnWriteHeaderContents(System.Xml.XmlDictionaryWriter writer, System.ServiceModel.Channels.MessageVersion version);
        protected virtual void OnWriteStartHeader(System.Xml.XmlDictionaryWriter writer, System.ServiceModel.Channels.MessageVersion version) { }
        public override string ToString() { throw null; }
        public void WriteHeader(System.Xml.XmlDictionaryWriter writer, System.ServiceModel.Channels.MessageVersion version) { }
        public void WriteHeader(System.Xml.XmlWriter writer, System.ServiceModel.Channels.MessageVersion version) { }
        protected void WriteHeaderAttributes(System.Xml.XmlDictionaryWriter writer, System.ServiceModel.Channels.MessageVersion version) { }
        public void WriteHeaderContents(System.Xml.XmlDictionaryWriter writer, System.ServiceModel.Channels.MessageVersion version) { }
        public void WriteStartHeader(System.Xml.XmlDictionaryWriter writer, System.ServiceModel.Channels.MessageVersion version) { }
    }
    public abstract partial class MessageHeaderInfo
    {
        protected MessageHeaderInfo() { }
        public abstract string Actor { get; }
        public abstract bool IsReferenceParameter { get; }
        public abstract bool MustUnderstand { get; }
        public abstract string Name { get; }
        public abstract string Namespace { get; }
        public abstract bool Relay { get; }
    }
    public sealed partial class MessageHeaders : System.Collections.Generic.IEnumerable<System.ServiceModel.Channels.MessageHeaderInfo>, System.Collections.IEnumerable
    {
        public MessageHeaders(System.ServiceModel.Channels.MessageHeaders headers) { }
        public MessageHeaders(System.ServiceModel.Channels.MessageVersion version) { }
        public MessageHeaders(System.ServiceModel.Channels.MessageVersion version, int capacity) { }
        public string Action { get { throw null; } set { } }
        public int Count { get { throw null; } }
        public System.ServiceModel.EndpointAddress FaultTo { get { throw null; } set { } }
        public System.ServiceModel.EndpointAddress From { get { throw null; } set { } }
        public System.ServiceModel.Channels.MessageHeaderInfo this[int index] { get { throw null; } }
        public System.Xml.UniqueId MessageId { get { throw null; } set { } }
        public System.ServiceModel.Channels.MessageVersion MessageVersion { get { throw null; } }
        public System.Xml.UniqueId RelatesTo { get { throw null; } set { } }
        public System.ServiceModel.EndpointAddress ReplyTo { get { throw null; } set { } }
        public System.Uri To { get { throw null; } set { } }
            public System.ServiceModel.Channels.UnderstoodHeaders UnderstoodHeaders { get { throw null; } }
        public void Add(System.ServiceModel.Channels.MessageHeader header) { }
        public void Clear() { }
        public void CopyHeaderFrom(System.ServiceModel.Channels.Message m, int index) { }
        public void CopyHeaderFrom(System.ServiceModel.Channels.MessageHeaders headers, int index) { }
        public void CopyHeadersFrom(System.ServiceModel.Channels.Message m) { }
        public void CopyHeadersFrom(System.ServiceModel.Channels.MessageHeaders headers) { }
        public void CopyTo(System.ServiceModel.Channels.MessageHeaderInfo[] dst, int index) { }
        public int FindHeader(string name, string ns) { throw null; }
        public int FindHeader(string name, string ns, params string[] actors) { throw null; }
        public System.Collections.Generic.IEnumerator<System.ServiceModel.Channels.MessageHeaderInfo> GetEnumerator() { throw null; }
        public T GetHeader<T>(int index) { throw null; }
        public T GetHeader<T>(int index, System.Runtime.Serialization.XmlObjectSerializer serializer) { throw null; }
        public T GetHeader<T>(string name, string ns) { throw null; }
        public T GetHeader<T>(string name, string ns, System.Runtime.Serialization.XmlObjectSerializer serializer) { throw null; }
        public T GetHeader<T>(string name, string ns, params string[] actors) { throw null; }
        public System.Xml.XmlDictionaryReader GetReaderAtHeader(int index) { throw null; }
        public bool HaveMandatoryHeadersBeenUnderstood() { throw null; }
        public bool HaveMandatoryHeadersBeenUnderstood(params string[] actors) { throw null; }
        public void Insert(int index, System.ServiceModel.Channels.MessageHeader header) { }
        public void RemoveAll(string name, string ns) { }
        public void RemoveAt(int index) { }
        public void SetAction(System.Xml.XmlDictionaryString action) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public void WriteHeader(int index, System.Xml.XmlDictionaryWriter writer) { }
        public void WriteHeader(int index, System.Xml.XmlWriter writer) { }
        public void WriteHeaderContents(int index, System.Xml.XmlDictionaryWriter writer) { }
        public void WriteHeaderContents(int index, System.Xml.XmlWriter writer) { }
        public void WriteStartHeader(int index, System.Xml.XmlDictionaryWriter writer) { }
        public void WriteStartHeader(int index, System.Xml.XmlWriter writer) { }
    }
    public sealed partial class MessageProperties : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IDictionary<string, object>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.IEnumerable, System.IDisposable
    {
        public MessageProperties() { }
        public MessageProperties(System.ServiceModel.Channels.MessageProperties properties) { }
        public bool AllowOutputBatching { get { throw null; } set { } }
        public int Count { get { throw null; } }
        public System.ServiceModel.Channels.MessageEncoder Encoder { get { throw null; } set { } }
        public bool IsFixedSize { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public object this[string name] { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<string> Keys { get { throw null; } }
        public System.Collections.Generic.ICollection<object> Values { get { throw null; } }
        public System.Uri Via { get { throw null; } set { } }
        public void Add(string name, object property) { }
        public void Clear() { }
        public bool ContainsKey(string name) { throw null; }
        public void CopyProperties(System.ServiceModel.Channels.MessageProperties properties) { }
        public void Dispose() { }
        public bool Remove(string name) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string,object>>.Add(System.Collections.Generic.KeyValuePair<string, object> pair) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string,object>>.Contains(System.Collections.Generic.KeyValuePair<string, object> pair) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string,object>>.CopyTo(System.Collections.Generic.KeyValuePair<string, object>[] array, int index) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string,object>>.Remove(System.Collections.Generic.KeyValuePair<string, object> pair) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string,object>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValue(string name, out object value) { throw null; }
    }
    public enum MessageState
    {
        Closed = 4,
        Copied = 3,
        Created = 0,
        Read = 1,
        Written = 2,
    }
    public sealed partial class MessageVersion
    {
        internal MessageVersion() { }
        public System.ServiceModel.Channels.AddressingVersion Addressing { get { throw null; } }
        public static System.ServiceModel.Channels.MessageVersion Default { get { throw null; } }
        public System.ServiceModel.EnvelopeVersion Envelope { get { throw null; } }
        public static System.ServiceModel.Channels.MessageVersion None { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static System.ServiceModel.Channels.MessageVersion Soap11 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static System.ServiceModel.Channels.MessageVersion Soap11WSAddressing10 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static System.ServiceModel.Channels.MessageVersion Soap11WSAddressingAugust2004 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static System.ServiceModel.Channels.MessageVersion Soap12 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static System.ServiceModel.Channels.MessageVersion Soap12WSAddressing10 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static System.ServiceModel.Channels.MessageVersion Soap12WSAddressingAugust2004 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static System.ServiceModel.Channels.MessageVersion CreateVersion(System.ServiceModel.EnvelopeVersion envelope_version) { throw null; }
        public static System.ServiceModel.Channels.MessageVersion CreateVersion(System.ServiceModel.EnvelopeVersion envelope_version, System.ServiceModel.Channels.AddressingVersion addressing_version) { throw null; }
        public override bool Equals(object value) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class PrivacyNoticeBindingElementImporter
    {
        public PrivacyNoticeBindingElementImporter() { }
    }
    public sealed partial class ReliableSessionBindingElement : System.ServiceModel.Channels.BindingElement
    {
        public ReliableSessionBindingElement() { }
        public ReliableSessionBindingElement(bool ordered) { }
            public System.TimeSpan AcknowledgementInterval { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public bool FlowControlEnabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public System.TimeSpan InactivityTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public int MaxPendingChannels { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public int MaxRetryCount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public int MaxTransferWindowSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public bool Ordered { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public System.ServiceModel.ReliableMessagingVersion ReliableMessagingVersion { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public override System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { throw null; }
            public override bool CanBuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { throw null; }
        public override System.ServiceModel.Channels.BindingElement Clone() { throw null; }
        public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) { throw null; }
    }
    public abstract partial class RequestContext : System.IDisposable
    {
        protected RequestContext() { }
        public abstract System.ServiceModel.Channels.Message RequestMessage { get; }
        public abstract void Abort();
        public abstract System.IAsyncResult BeginReply(System.ServiceModel.Channels.Message message, System.AsyncCallback callback, object state);
        public abstract System.IAsyncResult BeginReply(System.ServiceModel.Channels.Message message, System.TimeSpan timeout, System.AsyncCallback callback, object state);
        public abstract void Close();
        public abstract void Close(System.TimeSpan timeout);
        protected virtual void Dispose(bool disposing) { }
        public abstract void EndReply(System.IAsyncResult result);
        public abstract void Reply(System.ServiceModel.Channels.Message message);
        public abstract void Reply(System.ServiceModel.Channels.Message message, System.TimeSpan timeout);
        void System.IDisposable.Dispose() { }
    }
    public abstract partial class SecurityBindingElement : System.ServiceModel.Channels.BindingElement
    {
        internal SecurityBindingElement() { }
        public bool IncludeTimestamp { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.ServiceModel.Channels.LocalClientSecuritySettings LocalClientSettings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { throw null; }
        protected abstract System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactoryCore<TChannel>(System.ServiceModel.Channels.BindingContext context);
        public override bool CanBuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { throw null; }
            public static System.ServiceModel.Channels.TransportSecurityBindingElement CreateUserNameOverTransportBindingElement() { throw null; }
            public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) { throw null; }
    }
    public enum SecurityHeaderLayout
    {
        Lax = 1,
        LaxTimestampFirst = 2,
        LaxTimestampLast = 3,
        Strict = 0,
    }
    public enum SupportedAddressingMode
    {
        Anonymous = 0,
        Mixed = 2,
        NonAnonymous = 1,
    }
    public sealed partial class TextMessageEncodingBindingElement : System.ServiceModel.Channels.MessageEncodingBindingElement, System.ServiceModel.Description.IPolicyExportExtension, System.ServiceModel.Description.IWsdlExportExtension
    {
        public TextMessageEncodingBindingElement() { }
        public TextMessageEncodingBindingElement(System.ServiceModel.Channels.MessageVersion messageVersion, System.Text.Encoding writeEncoding) { }
        public int MaxReadPoolSize { get { throw null; } set { } }
        public int MaxWritePoolSize { get { throw null; } set { } }
        public override System.ServiceModel.Channels.MessageVersion MessageVersion { get { throw null; } set { } }
        public System.Xml.XmlDictionaryReaderQuotas ReaderQuotas { get { throw null; } set { } }
        public System.Text.Encoding WriteEncoding { get { throw null; } set { } }
        public override System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { throw null; }
        public override System.ServiceModel.Channels.BindingElement Clone() { throw null; }
        public override System.ServiceModel.Channels.MessageEncoderFactory CreateMessageEncoderFactory() { throw null; }
            public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) { throw null; }
    }
    public enum TransferSession
    {
        None = 0,
        Ordered = 1,
        Unordered = 2,
    }
    public abstract partial class TransportBindingElement : System.ServiceModel.Channels.BindingElement
    {
        protected TransportBindingElement() { }
        protected TransportBindingElement(System.ServiceModel.Channels.TransportBindingElement other) { }
        public bool ManualAddressing { get { throw null; } set { } }
        public virtual long MaxBufferPoolSize { get { throw null; } set { } }
        public virtual long MaxReceivedMessageSize { get { throw null; } set { } }
        public abstract string Scheme { get; }
        public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) { throw null; }
    }
    public sealed partial class TransportSecurityBindingElement : System.ServiceModel.Channels.SecurityBindingElement
    {
        public TransportSecurityBindingElement() { }
            protected override System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactoryCore<TChannel>(System.ServiceModel.Channels.BindingContext context) { throw null; }
        public override System.ServiceModel.Channels.BindingElement Clone() { throw null; }
    }
    public sealed partial class UnderstoodHeaders : System.Collections.Generic.IEnumerable<System.ServiceModel.Channels.MessageHeaderInfo>, System.Collections.IEnumerable
    {
        internal UnderstoodHeaders() { }
        public void Add(System.ServiceModel.Channels.MessageHeaderInfo header) { }
        public bool Contains(System.ServiceModel.Channels.MessageHeaderInfo header) { throw null; }
        public System.Collections.Generic.IEnumerator<System.ServiceModel.Channels.MessageHeaderInfo> GetEnumerator() { throw null; }
        public void Remove(System.ServiceModel.Channels.MessageHeaderInfo header) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public static partial class UrlUtility
    {
        public static string UrlDecode(string s, System.Text.Encoding e) { throw null; }
        public static string UrlEncode(string s, System.Text.Encoding e) { throw null; }
    }
    public partial class UseManagedPresentationBindingElementImporter
    {
        public UseManagedPresentationBindingElementImporter() { }
    }
    public sealed partial class WebSocketTransportSettings : System.IEquatable<System.ServiceModel.Channels.WebSocketTransportSettings>
    {
        public const string BinaryEncoderTransferModeHeader = null;
        public const string BinaryMessageReceivedAction = "http://schemas.microsoft.com/2011/02/websockets/onbinarymessage";
        public const string ConnectionOpenedAction = null;
        public const string SoapContentTypeHeader = null;
        public const string TextMessageReceivedAction = "http://schemas.microsoft.com/2011/02/websockets/ontextmessage";
        public WebSocketTransportSettings() { }
        public bool CreateNotificationOnConnection { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool DisablePayloadMasking { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan KeepAliveInterval { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int MaxPendingConnections { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int ReceiveBufferSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int SendBufferSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SubProtocol { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.ServiceModel.Channels.WebSocketTransportUsage TransportUsage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Equals(System.ServiceModel.Channels.WebSocketTransportSettings other) { throw null; }
    }
    public enum WebSocketTransportUsage
    {
        Always = 1,
        Never = 2,
        WhenDuplex = 0,
    }
    public partial class XmlSerializerImportOptions
    {
        public XmlSerializerImportOptions() { }
    }
}
namespace System.ServiceModel.Description
{
    public partial class ClientCredentials : System.ServiceModel.Description.IEndpointBehavior
    {
        public ClientCredentials() { }
            protected ClientCredentials(System.ServiceModel.Description.ClientCredentials source) { }
        public System.ServiceModel.Security.UserNamePasswordClientCredential UserName { get { throw null; } }
            public virtual void ApplyClientBehavior(System.ServiceModel.Description.ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.ClientRuntime behavior) { }
        public System.ServiceModel.Description.ClientCredentials Clone() { throw null; }
        protected virtual System.ServiceModel.Description.ClientCredentials CloneCore() { throw null; }
        void System.ServiceModel.Description.IEndpointBehavior.AddBindingParameters(System.ServiceModel.Description.ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection parameters) { }
        void System.ServiceModel.Description.IEndpointBehavior.ApplyDispatchBehavior(System.ServiceModel.Description.ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.EndpointDispatcher dispatcher) { }
        void System.ServiceModel.Description.IEndpointBehavior.Validate(System.ServiceModel.Description.ServiceEndpoint endpoint) { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Name={name}, Namespace={ns}, ContractType={contractType}")]
    public partial class ContractDescription
    {
        public ContractDescription(string name) { }
        public ContractDescription(string name, string ns) { }
        public System.Collections.Generic.KeyedByTypeCollection<System.ServiceModel.Description.IContractBehavior> Behaviors { get { throw null; } }
        public System.Type CallbackContractType { get { throw null; } set { } }
        public string ConfigurationName { get { throw null; } set { } }
            public System.Collections.ObjectModel.KeyedCollection<System.Type, System.ServiceModel.Description.IContractBehavior> ContractBehaviors { get { throw null; } }
        public System.Type ContractType { get { throw null; } set { } }
        public bool HasProtectionLevel { get { throw null; } }
        public string Name { get { throw null; } set { } }
        public string Namespace { get { throw null; } set { } }
        public System.ServiceModel.Description.OperationDescriptionCollection Operations { get { throw null; } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { throw null; } set { } }
        public System.ServiceModel.SessionMode SessionMode { get { throw null; } set { } }
        public static System.ServiceModel.Description.ContractDescription GetContract(System.Type contractType) { throw null; }
        public static System.ServiceModel.Description.ContractDescription GetContract(System.Type contractType, object serviceImplementation) { throw null; }
        public static System.ServiceModel.Description.ContractDescription GetContract(System.Type contractType, System.Type serviceType) { throw null; }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Description.ContractDescription> GetInheritedContracts() { throw null; }
    }
    public partial class DataContractSerializerOperationBehavior : System.ServiceModel.Description.IOperationBehavior, System.ServiceModel.Description.IWsdlExportExtension
    {
        public DataContractSerializerOperationBehavior(System.ServiceModel.Description.OperationDescription operation) { }
        public DataContractSerializerOperationBehavior(System.ServiceModel.Description.OperationDescription operation, System.ServiceModel.DataContractFormatAttribute dataContractFormatAttribute) { }
        public System.ServiceModel.DataContractFormatAttribute DataContractFormatAttribute { get { throw null; } }
        public System.Runtime.Serialization.DataContractResolver DataContractResolver { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IgnoreExtensionDataObject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int MaxItemsInObjectGraph { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Runtime.Serialization.XmlObjectSerializer CreateSerializer(System.Type type, string name, string ns, System.Collections.Generic.IList<System.Type> knownTypes) { throw null; }
        public virtual System.Runtime.Serialization.XmlObjectSerializer CreateSerializer(System.Type type, System.Xml.XmlDictionaryString name, System.Xml.XmlDictionaryString ns, System.Collections.Generic.IList<System.Type> knownTypes) { throw null; }
        void System.ServiceModel.Description.IOperationBehavior.AddBindingParameters(System.ServiceModel.Description.OperationDescription description, System.ServiceModel.Channels.BindingParameterCollection parameters) { }
        void System.ServiceModel.Description.IOperationBehavior.ApplyClientBehavior(System.ServiceModel.Description.OperationDescription description, System.ServiceModel.Dispatcher.ClientOperation proxy) { }
        void System.ServiceModel.Description.IOperationBehavior.ApplyDispatchBehavior(System.ServiceModel.Description.OperationDescription description, System.ServiceModel.Dispatcher.DispatchOperation dispatch) { }
        void System.ServiceModel.Description.IOperationBehavior.Validate(System.ServiceModel.Description.OperationDescription description) { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Name={name}, Action={action}, DetailType={detailType}")]
    public partial class FaultDescription
    {
        public FaultDescription(string action) { }
        public string Action { get { throw null; } }
        public System.Type DetailType { get { throw null; } set { } }
        public bool HasProtectionLevel { get { throw null; } }
        public string Name { get { throw null; } set { } }
        public string Namespace { get { throw null; } set { } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { throw null; } set { } }
    }
    public partial class FaultDescriptionCollection : System.Collections.ObjectModel.Collection<System.ServiceModel.Description.FaultDescription>
    {
        internal FaultDescriptionCollection() { }
        public System.ServiceModel.Description.FaultDescription Find(string name) { throw null; }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Description.FaultDescription> FindAll(string name) { throw null; }
    }
    public partial interface IContractBehavior
    {
        void AddBindingParameters(System.ServiceModel.Description.ContractDescription description, System.ServiceModel.Description.ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection parameters);
        void ApplyClientBehavior(System.ServiceModel.Description.ContractDescription description, System.ServiceModel.Description.ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.ClientRuntime proxy);
        void ApplyDispatchBehavior(System.ServiceModel.Description.ContractDescription description, System.ServiceModel.Description.ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.DispatchRuntime dispatch);
        void Validate(System.ServiceModel.Description.ContractDescription description, System.ServiceModel.Description.ServiceEndpoint endpoint);
    }
    public partial interface IEndpointBehavior
    {
        void AddBindingParameters(System.ServiceModel.Description.ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection parameters);
        void ApplyClientBehavior(System.ServiceModel.Description.ServiceEndpoint serviceEndpoint, System.ServiceModel.Dispatcher.ClientRuntime behavior);
        void ApplyDispatchBehavior(System.ServiceModel.Description.ServiceEndpoint serviceEndpoint, System.ServiceModel.Dispatcher.EndpointDispatcher dispatcher);
        void Validate(System.ServiceModel.Description.ServiceEndpoint serviceEndpoint);
    }
    public partial interface IOperationBehavior
    {
        void AddBindingParameters(System.ServiceModel.Description.OperationDescription description, System.ServiceModel.Channels.BindingParameterCollection parameters);
        void ApplyClientBehavior(System.ServiceModel.Description.OperationDescription description, System.ServiceModel.Dispatcher.ClientOperation proxy);
        void ApplyDispatchBehavior(System.ServiceModel.Description.OperationDescription description, System.ServiceModel.Dispatcher.DispatchOperation dispatch);
        void Validate(System.ServiceModel.Description.OperationDescription description);
    }
    public partial interface IPolicyExportExtension
    {
    }
    public partial interface IPolicyImportExtension
    {
    }
    public partial interface IWsdlExportExtension
    {
    }
    public partial interface IWsdlImportExtension
    {
    }
    public enum ListenUriMode
    {
        Explicit = 0,
        Unique = 1,
    }
    public partial class MessageBodyDescription
    {
        public MessageBodyDescription() { }
        public System.ServiceModel.Description.MessagePartDescriptionCollection Parts { get { throw null; } }
        public System.ServiceModel.Description.MessagePartDescription ReturnValue { get { throw null; } set { } }
        public string WrapperName { get { throw null; } set { } }
        public string WrapperNamespace { get { throw null; } set { } }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Action={action}, Direction={direction}, MessageType={messageType}")]
    public partial class MessageDescription
    {
        public MessageDescription(string action, System.ServiceModel.Description.MessageDirection direction) { }
        public string Action { get { throw null; } }
        public System.ServiceModel.Description.MessageBodyDescription Body { get { throw null; } }
        public System.ServiceModel.Description.MessageDirection Direction { get { throw null; } }
        public bool HasProtectionLevel { get { throw null; } }
        public System.ServiceModel.Description.MessageHeaderDescriptionCollection Headers { get { throw null; } }
        public System.Type MessageType { get { throw null; } set { } }
        public System.ServiceModel.Description.MessagePropertyDescriptionCollection Properties { get { throw null; } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { throw null; } set { } }
    }
    public partial class MessageDescriptionCollection : System.Collections.ObjectModel.Collection<System.ServiceModel.Description.MessageDescription>
    {
        internal MessageDescriptionCollection() { }
        public System.ServiceModel.Description.MessageDescription Find(string action) { throw null; }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Description.MessageDescription> FindAll(string action) { throw null; }
    }
    public enum MessageDirection
    {
        Input = 0,
        Output = 1,
    }
    public partial class MessageHeaderDescription : System.ServiceModel.Description.MessagePartDescription
    {
        public MessageHeaderDescription(string name, string ns) : base (default(string), default(string)) { }
        public string Actor { get { throw null; } set { } }
        public bool MustUnderstand { get { throw null; } set { } }
        public bool Relay { get { throw null; } set { } }
        public bool TypedHeader { get { throw null; } set { } }
    }
    public partial class MessageHeaderDescriptionCollection : System.Collections.ObjectModel.KeyedCollection<System.Xml.XmlQualifiedName, System.ServiceModel.Description.MessageHeaderDescription>
    {
        internal MessageHeaderDescriptionCollection() { }
        protected override System.Xml.XmlQualifiedName GetKeyForItem(System.ServiceModel.Description.MessageHeaderDescription item) { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Name={name}, Namespace={ns}, Type={Type}, Index={index}}")]
    public partial class MessagePartDescription
    {
        public MessagePartDescription(string name, string ns) { }
        public bool HasProtectionLevel { get { throw null; } }
        public int Index { get { throw null; } set { } }
        public System.Reflection.MemberInfo MemberInfo { get { throw null; } set { } }
        public bool Multiple { get { throw null; } set { } }
        public string Name { get { throw null; } }
        public string Namespace { get { throw null; } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { throw null; } set { } }
        public System.Type Type { get { throw null; } set { } }
    }
    public partial class MessagePartDescriptionCollection : System.Collections.ObjectModel.KeyedCollection<System.Xml.XmlQualifiedName, System.ServiceModel.Description.MessagePartDescription>
    {
        internal MessagePartDescriptionCollection() { }
        protected override System.Xml.XmlQualifiedName GetKeyForItem(System.ServiceModel.Description.MessagePartDescription item) { throw null; }
    }
    public partial class MessagePropertyDescription : System.ServiceModel.Description.MessagePartDescription
    {
        public MessagePropertyDescription(string name) : base (default(string), default(string)) { }
    }
    public partial class MessagePropertyDescriptionCollection : System.Collections.ObjectModel.KeyedCollection<string, System.ServiceModel.Description.MessagePropertyDescription>
    {
        internal MessagePropertyDescriptionCollection() { }
        protected override string GetKeyForItem(System.ServiceModel.Description.MessagePropertyDescription item) { throw null; }
    }
    public enum MetadataExchangeClientMode
    {
        HttpGet = 1,
        MetadataExchange = 0,
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Name={name}, IsInitiating={isInitiating}, IsTerminating={isTerminating}")]
    public partial class OperationDescription
    {
        public OperationDescription(string name, System.ServiceModel.Description.ContractDescription declaringContract) { }
        public System.Reflection.MethodInfo BeginMethod { get { throw null; } set { } }
        public System.Collections.Generic.KeyedByTypeCollection<System.ServiceModel.Description.IOperationBehavior> Behaviors { get { throw null; } }
        public System.ServiceModel.Description.ContractDescription DeclaringContract { get { throw null; } set { } }
        public System.Reflection.MethodInfo EndMethod { get { throw null; } set { } }
        public System.ServiceModel.Description.FaultDescriptionCollection Faults { get { throw null; } }
        public bool HasProtectionLevel { get { throw null; } }
        public bool IsInitiating { get { throw null; } set { } }
        public bool IsOneWay { get { throw null; } }
        public bool IsTerminating { get { throw null; } set { } }
        public System.Collections.ObjectModel.Collection<System.Type> KnownTypes { get { throw null; } }
        public System.ServiceModel.Description.MessageDescriptionCollection Messages { get { throw null; } }
        public string Name { get { throw null; } }
            public System.Collections.ObjectModel.KeyedCollection<System.Type, System.ServiceModel.Description.IOperationBehavior> OperationBehaviors { get { throw null; } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { throw null; } set { } }
        public System.Reflection.MethodInfo SyncMethod { get { throw null; } set { } }
            public System.Reflection.MethodInfo TaskMethod { get { throw null; } set { } }
    }
    public partial class OperationDescriptionCollection : System.Collections.ObjectModel.Collection<System.ServiceModel.Description.OperationDescription>
    {
        internal OperationDescriptionCollection() { }
        public System.ServiceModel.Description.OperationDescription Find(string name) { throw null; }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Description.OperationDescription> FindAll(string name) { throw null; }
        protected override void InsertItem(int index, System.ServiceModel.Description.OperationDescription item) { }
        protected override void SetItem(int index, System.ServiceModel.Description.OperationDescription item) { }
    }
    public enum PrincipalPermissionMode
    {
        Custom = 3,
        None = 0,
        UseAspNetRoles = 2,
        UseWindowsGroups = 1,
    }
    [System.FlagsAttribute]
    public enum ServiceContractGenerationOptions
    {
        AsynchronousMethods = 1,
        ChannelInterface = 2,
        ClientClass = 8,
        EventBasedAsynchronousMethods = 32,
        InternalTypes = 4,
        None = 0,
        TypedMessages = 16,
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Address={address}")]
    [System.Diagnostics.DebuggerDisplayAttribute("Name={name}")]
    public partial class ServiceEndpoint
    {
        public ServiceEndpoint(System.ServiceModel.Description.ContractDescription contract) { }
        public ServiceEndpoint(System.ServiceModel.Description.ContractDescription contract, System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress address) { }
        public System.ServiceModel.EndpointAddress Address { get { throw null; } set { } }
        public System.Collections.Generic.KeyedByTypeCollection<System.ServiceModel.Description.IEndpointBehavior> Behaviors { get { throw null; } }
        public System.ServiceModel.Channels.Binding Binding { get { throw null; } set { } }
        public System.ServiceModel.Description.ContractDescription Contract { get { throw null; } set { } }
            public System.Collections.ObjectModel.KeyedCollection<System.Type, System.ServiceModel.Description.IEndpointBehavior> EndpointBehaviors { get { throw null; } }
        public bool IsSystemEndpoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Uri ListenUri { get { throw null; } set { } }
        public System.ServiceModel.Description.ListenUriMode ListenUriMode { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
    }
    public partial class XmlSerializerOperationBehavior : System.ServiceModel.Description.IOperationBehavior
    {
        public XmlSerializerOperationBehavior(System.ServiceModel.Description.OperationDescription operation) { }
        public XmlSerializerOperationBehavior(System.ServiceModel.Description.OperationDescription operation, System.ServiceModel.XmlSerializerFormatAttribute format) { }
        public System.ServiceModel.XmlSerializerFormatAttribute XmlSerializerFormatAttribute { get { throw null; } }
            public System.Collections.ObjectModel.Collection<System.Xml.Serialization.XmlMapping> GetXmlMappings() { throw null; }
        void System.ServiceModel.Description.IOperationBehavior.AddBindingParameters(System.ServiceModel.Description.OperationDescription description, System.ServiceModel.Channels.BindingParameterCollection parameters) { }
        void System.ServiceModel.Description.IOperationBehavior.ApplyClientBehavior(System.ServiceModel.Description.OperationDescription description, System.ServiceModel.Dispatcher.ClientOperation proxy) { }
        void System.ServiceModel.Description.IOperationBehavior.ApplyDispatchBehavior(System.ServiceModel.Description.OperationDescription description, System.ServiceModel.Dispatcher.DispatchOperation dispatch) { }
        void System.ServiceModel.Description.IOperationBehavior.Validate(System.ServiceModel.Description.OperationDescription description) { }
    }
}
namespace System.ServiceModel.Dispatcher
{
    public sealed partial class ClientOperation
    {
        public ClientOperation(System.ServiceModel.Dispatcher.ClientRuntime parent, string name, string action) { }
        public ClientOperation(System.ServiceModel.Dispatcher.ClientRuntime parent, string name, string action, string replyAction) { }
        public string Action { get { throw null; } }
        public System.Reflection.MethodInfo BeginMethod { get { throw null; } set { } }
            public System.Collections.Generic.ICollection<System.ServiceModel.Dispatcher.IParameterInspector> ClientParameterInspectors { get { throw null; } }
        public bool DeserializeReply { get { throw null; } set { } }
        public System.Reflection.MethodInfo EndMethod { get { throw null; } set { } }
        public System.Collections.Generic.SynchronizedCollection<System.ServiceModel.Dispatcher.FaultContractInfo> FaultContractInfos { get { throw null; } }
        public System.ServiceModel.Dispatcher.IClientMessageFormatter Formatter { get { throw null; } set { } }
        public bool IsInitiating { get { throw null; } set { } }
        public bool IsOneWay { get { throw null; } set { } }
        public bool IsTerminating { get { throw null; } set { } }
        public string Name { get { throw null; } }
        public System.Collections.Generic.SynchronizedCollection<System.ServiceModel.Dispatcher.IParameterInspector> ParameterInspectors { get { throw null; } }
        public System.ServiceModel.Dispatcher.ClientRuntime Parent { get { throw null; } }
        public string ReplyAction { get { throw null; } }
        public bool SerializeRequest { get { throw null; } set { } }
        public System.Reflection.MethodInfo SyncMethod { get { throw null; } set { } }
            public System.Reflection.MethodInfo TaskMethod { get { throw null; } set { } }
            public System.Type TaskTResult { get { throw null; } set { } }
    }
    public sealed partial class ClientRuntime
    {
        internal ClientRuntime() { }
        public System.Type CallbackClientType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.SynchronizedCollection<System.ServiceModel.Dispatcher.IChannelInitializer> ChannelInitializers { get { throw null; } }
            public System.Collections.Generic.ICollection<System.ServiceModel.Dispatcher.IClientMessageInspector> ClientMessageInspectors { get { throw null; } }
            public System.Collections.Generic.ICollection<System.ServiceModel.Dispatcher.ClientOperation> ClientOperations { get { throw null; } }
        public System.Type ContractClientType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ContractName { get { throw null; } }
        public string ContractNamespace { get { throw null; } }
        public System.Collections.Generic.SynchronizedCollection<System.ServiceModel.Dispatcher.IInteractiveChannelInitializer> InteractiveChannelInitializers { get { throw null; } }
        public bool ManualAddressing { get { throw null; } set { } }
        public int MaxFaultSize { get { throw null; } set { } }
        public System.Collections.Generic.SynchronizedCollection<System.ServiceModel.Dispatcher.IClientMessageInspector> MessageInspectors { get { throw null; } }
            public bool MessageVersionNoneFaultsEnabled { get { throw null; } set { } }
        public System.Collections.ObjectModel.KeyedCollection<string, System.ServiceModel.Dispatcher.ClientOperation> Operations { get { throw null; } }
        public System.ServiceModel.Dispatcher.IClientOperationSelector OperationSelector { get { throw null; } set { } }
            public System.ServiceModel.Dispatcher.ClientOperation UnhandledClientOperation { get { throw null; } }
        public bool ValidateMustUnderstand { get { throw null; } set { } }
        public System.Uri Via { get { throw null; } set { } }
    }
    public sealed partial class DispatchOperation
    {
        public DispatchOperation(System.ServiceModel.Dispatcher.DispatchRuntime parent, string name, string action) { }
        public DispatchOperation(System.ServiceModel.Dispatcher.DispatchRuntime parent, string name, string action, string replyAction) { }
        public string Action { get { throw null; } }
        public bool AutoDisposeParameters { get { throw null; } set { } }
        public bool DeserializeRequest { get { throw null; } set { } }
        public System.ServiceModel.Dispatcher.IDispatchMessageFormatter Formatter { get { throw null; } set { } }
        public bool IsOneWay { get { throw null; } }
        public string Name { get { throw null; } }
        public System.ServiceModel.Dispatcher.DispatchRuntime Parent { get { throw null; } }
        public bool SerializeReply { get { throw null; } set { } }
    }
    public sealed partial class DispatchRuntime
    {
        internal DispatchRuntime() { }
        public System.ServiceModel.Dispatcher.DispatchOperation UnhandledDispatchOperation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public sealed partial class EndpointDispatcher
    {
        internal EndpointDispatcher() { }
    }
    public partial class FaultContractInfo
    {
        public FaultContractInfo(string action, System.Type detail) { }
        public string Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Type Detail { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial interface IChannelInitializer
    {
        void Initialize(System.ServiceModel.IClientChannel channel);
    }
    public partial interface IClientMessageFormatter
    {
        object DeserializeReply(System.ServiceModel.Channels.Message message, object[] paremeters);
        System.ServiceModel.Channels.Message SerializeRequest(System.ServiceModel.Channels.MessageVersion version, object[] inputs);
    }
    public partial interface IClientMessageInspector
    {
        void AfterReceiveReply(ref System.ServiceModel.Channels.Message message, object correlationState);
        object BeforeSendRequest(ref System.ServiceModel.Channels.Message message, System.ServiceModel.IClientChannel channel);
    }
    public partial interface IClientOperationSelector
    {
        bool AreParametersRequiredForSelection { get; }
        string SelectOperation(System.Reflection.MethodBase method, object[] parameters);
    }
    public partial interface IDispatchMessageFormatter
    {
        void DeserializeRequest(System.ServiceModel.Channels.Message message, object[] parameters);
        System.ServiceModel.Channels.Message SerializeReply(System.ServiceModel.Channels.MessageVersion version, object[] parameters, object result);
    }
    public partial interface IInteractiveChannelInitializer
    {
        System.IAsyncResult BeginDisplayInitializationUI(System.ServiceModel.IClientChannel channel, System.AsyncCallback callback, object state);
        void EndDisplayInitializationUI(System.IAsyncResult result);
    }
    public partial interface IParameterInspector
    {
        void AfterCall(string operationName, object[] outputs, object returnValue, object correlationState);
        object BeforeCall(string operationName, object[] inputs);
    }
}
namespace System.ServiceModel.MsmqIntegration
{
    public enum MsmqIntegrationSecurityMode
    {
        None = 0,
        Transport = 1,
    }
    public enum MsmqMessageSerializationFormat
    {
        ActiveX = 2,
        Binary = 1,
        ByteArray = 3,
        Stream = 4,
        Xml = 0,
    }
}
namespace System.ServiceModel.Security
{
    public sealed partial class HttpDigestClientCredential
    {
        internal HttpDigestClientCredential() { }
        public System.Security.Principal.TokenImpersonationLevel AllowedImpersonationLevel { get { throw null; } set { } }
        public System.Net.NetworkCredential ClientCredential { get { throw null; } set { } }
    }
    public partial class MessageSecurityException : System.ServiceModel.CommunicationException
    {
        public MessageSecurityException() { }
        protected MessageSecurityException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public MessageSecurityException(string message) { }
        public MessageSecurityException(string message, System.Exception innerException) { }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext ctx) { }
    }
    public partial class SecurityAccessDeniedException : System.ServiceModel.CommunicationException
    {
        public SecurityAccessDeniedException() { }
        protected SecurityAccessDeniedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public SecurityAccessDeniedException(string message) { }
        public SecurityAccessDeniedException(string message, System.Exception innerException) { }
    }
    public sealed partial class UserNamePasswordClientCredential
    {
        internal UserNamePasswordClientCredential() { }
        public string Password { get { throw null; } set { } }
        public string UserName { get { throw null; } set { } }
    }
    public enum UserNamePasswordValidationMode
    {
        Custom = 2,
        MembershipProvider = 1,
        Windows = 0,
    }
    public sealed partial class WindowsClientCredential
    {
        internal WindowsClientCredential() { }
        public System.Security.Principal.TokenImpersonationLevel AllowedImpersonationLevel { get { throw null; } set { } }
        public bool AllowNtlm { get { throw null; } set { } }
            public System.Net.NetworkCredential ClientCredential { get { throw null; } set { } }
    }
}
namespace System.ServiceModel.Security.Tokens
{
    public enum SecurityTokenInclusionMode
    {
        AlwaysToInitiator = 3,
        AlwaysToRecipient = 0,
        Never = 1,
        Once = 2,
    }
    public enum X509KeyIdentifierClauseType
    {
        Any = 0,
        IssuerSerial = 2,
        RawDataKeyIdentifier = 4,
        SubjectKeyIdentifier = 3,
        Thumbprint = 1,
    }
}

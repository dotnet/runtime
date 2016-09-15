// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Generic
{
    public partial class KeyedByTypeCollection<TItem> : System.Collections.ObjectModel.KeyedCollection<System.Type, TItem>
    {
        public KeyedByTypeCollection() { }
        public KeyedByTypeCollection(System.Collections.Generic.IEnumerable<TItem> items) { }
        public T Find<T>() { return default(T); }
        public System.Collections.ObjectModel.Collection<T> FindAll<T>() { return default(System.Collections.ObjectModel.Collection<T>); }
        protected override System.Type GetKeyForItem(TItem item) { return default(System.Type); }
        protected override void InsertItem(int index, TItem kind) { }
        public T Remove<T>() { return default(T); }
        public System.Collections.ObjectModel.Collection<T> RemoveAll<T>() { return default(System.Collections.ObjectModel.Collection<T>); }
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
        public int Count { get { return default(int); } }
        public T this[int index] { get { return default(T); } set { } }
        protected System.Collections.Generic.List<T> Items { get { return default(System.Collections.Generic.List<T>); } }
        public object SyncRoot { get { return default(object); } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { return default(bool); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        bool System.Collections.IList.IsFixedSize { get { return default(bool); } }
        bool System.Collections.IList.IsReadOnly { get { return default(bool); } }
        object System.Collections.IList.this[int index] { get { return default(object); } set { } }
        public void Add(T item) { }
        public void Clear() { }
        protected virtual void ClearItems() { }
        public bool Contains(T item) { return default(bool); }
        public void CopyTo(T[] array, int index) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        public int IndexOf(T item) { return default(int); }
        public void Insert(int index, T item) { }
        protected virtual void InsertItem(int index, T item) { }
        public bool Remove(T item) { return default(bool); }
        public void RemoveAt(int index) { }
        protected virtual void RemoveItem(int index) { }
        protected virtual void SetItem(int index, T item) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        int System.Collections.IList.Add(object value) { return default(int); }
        bool System.Collections.IList.Contains(object value) { return default(bool); }
        int System.Collections.IList.IndexOf(object value) { return default(int); }
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
        protected System.Collections.Generic.IDictionary<K, T> Dictionary { get { return default(System.Collections.Generic.IDictionary<K, T>); } }
        public T this[K key] { get { return default(T); } }
        protected void ChangeItemKey(T item, K newKey) { }
        protected override void ClearItems() { }
        public bool Contains(K key) { return default(bool); }
        protected abstract K GetKeyForItem(T item);
        protected override void InsertItem(int index, T item) { }
        public bool Remove(K key) { return default(bool); }
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
        public int Count { get { return default(int); } }
        public T this[int index] { get { return default(T); } }
        protected System.Collections.Generic.IList<T> Items { get { return default(System.Collections.Generic.IList<T>); } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { return default(bool); } }
        T System.Collections.Generic.IList<T>.this[int index] { get { return default(T); } set { } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        bool System.Collections.IList.IsFixedSize { get { return default(bool); } }
        bool System.Collections.IList.IsReadOnly { get { return default(bool); } }
        object System.Collections.IList.this[int index] { get { return default(object); } set { } }
        public bool Contains(T value) { return default(bool); }
        public void CopyTo(T[] array, int index) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        public int IndexOf(T value) { return default(int); }
        void System.Collections.Generic.ICollection<T>.Add(T value) { }
        void System.Collections.Generic.ICollection<T>.Clear() { }
        bool System.Collections.Generic.ICollection<T>.Remove(T value) { return default(bool); }
        void System.Collections.Generic.IList<T>.Insert(int index, T value) { }
        void System.Collections.Generic.IList<T>.RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        int System.Collections.IList.Add(object value) { return default(int); }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { return default(bool); }
        int System.Collections.IList.IndexOf(object value) { return default(int); }
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
        public System.ServiceModel.WSMessageEncoding MessageEncoding { get { return default(System.ServiceModel.WSMessageEncoding); } set { } }
        public override string Scheme { get { return default(string); } }
        public System.ServiceModel.BasicHttpSecurity Security { get { return default(System.ServiceModel.BasicHttpSecurity); } set { } }
        public override System.ServiceModel.Channels.BindingElementCollection CreateBindingElements() { return default(System.ServiceModel.Channels.BindingElementCollection); }
    }
    public enum BasicHttpMessageCredentialType
    {
        Certificate = 1,
        UserName = 0,
    }
    public sealed partial class BasicHttpMessageSecurity
    {
        internal BasicHttpMessageSecurity() { }
        public System.ServiceModel.BasicHttpMessageCredentialType ClientCredentialType { get { return default(System.ServiceModel.BasicHttpMessageCredentialType); } set { } }
    }
    public partial class BasicHttpsBinding : System.ServiceModel.HttpBindingBase, System.ServiceModel.Channels.IBindingRuntimePreferences
    {
        public BasicHttpsBinding() { }
        public BasicHttpsBinding(System.ServiceModel.BasicHttpsSecurityMode securityMode) { }
        public System.ServiceModel.WSMessageEncoding MessageEncoding { get { return default(System.ServiceModel.WSMessageEncoding); } set { } }
        public override string Scheme { get { return default(string); } }
        public System.ServiceModel.BasicHttpsSecurity Security { get { return default(System.ServiceModel.BasicHttpsSecurity); } }
        public override System.ServiceModel.Channels.BindingElementCollection CreateBindingElements() { return default(System.ServiceModel.Channels.BindingElementCollection); }
    }
    public sealed partial class BasicHttpSecurity
    {
        public BasicHttpSecurity() { }
        public System.ServiceModel.BasicHttpMessageSecurity Message { get { return default(System.ServiceModel.BasicHttpMessageSecurity); } set { } }
        public System.ServiceModel.BasicHttpSecurityMode Mode { get { return default(System.ServiceModel.BasicHttpSecurityMode); } set { } }
        public System.ServiceModel.HttpTransportSecurity Transport { get { return default(System.ServiceModel.HttpTransportSecurity); } set { } }
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
        public System.ServiceModel.BasicHttpMessageSecurity Message { get { return default(System.ServiceModel.BasicHttpMessageSecurity); } }
        public System.ServiceModel.BasicHttpsSecurityMode Mode { get { return default(System.ServiceModel.BasicHttpsSecurityMode); } set { } }
        public System.ServiceModel.HttpTransportSecurity Transport { get { return default(System.ServiceModel.HttpTransportSecurity); } }
    }
    public enum BasicHttpsSecurityMode
    {
        Transport = 0,
        TransportWithMessageCredential = 1,
    }
    public abstract partial class ChannelFactory : System.ServiceModel.Channels.CommunicationObject, System.IDisposable, System.ServiceModel.Channels.IChannelFactory, System.ServiceModel.ICommunicationObject
    {
        protected ChannelFactory() { }
        public System.ServiceModel.Description.ClientCredentials Credentials { get { return default(System.ServiceModel.Description.ClientCredentials); } }
        protected internal override System.TimeSpan DefaultCloseTimeout { get { return default(System.TimeSpan); } }
        protected internal override System.TimeSpan DefaultOpenTimeout { get { return default(System.TimeSpan); } }
        public System.ServiceModel.Description.ServiceEndpoint Endpoint { get { return default(System.ServiceModel.Description.ServiceEndpoint); } }
        protected virtual void ApplyConfiguration(string endpointConfig) { }
        protected abstract System.ServiceModel.Description.ServiceEndpoint CreateDescription();
        protected virtual System.ServiceModel.Channels.IChannelFactory CreateFactory() { return default(System.ServiceModel.Channels.IChannelFactory); }
        protected void EnsureOpened() { }
        public T GetProperty<T>() where T : class { return default(T); }
        protected void InitializeEndpoint(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) { }
        protected void InitializeEndpoint(System.ServiceModel.Description.ServiceEndpoint endpoint) { }
        protected void InitializeEndpoint(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) { }
        protected override void OnAbort() { }
        protected override System.IAsyncResult OnBeginClose(System.TimeSpan timeout, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        protected override System.IAsyncResult OnBeginOpen(System.TimeSpan timeout, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
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
        public TChannel CreateChannel() { return default(TChannel); }
        public static TChannel CreateChannel(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress address) { return default(TChannel); }
        public static TChannel CreateChannel(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress address, System.Uri via) { return default(TChannel); }
        public TChannel CreateChannel(System.ServiceModel.EndpointAddress address) { return default(TChannel); }
        public virtual TChannel CreateChannel(System.ServiceModel.EndpointAddress address, System.Uri via) { return default(TChannel); }
        protected static TChannel CreateChannel(string endpointConfigurationName) { return default(TChannel); }
        protected override System.ServiceModel.Description.ServiceEndpoint CreateDescription() { return default(System.ServiceModel.Description.ServiceEndpoint); }
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
        protected TChannel Channel { get { return default(TChannel); } }
        public System.ServiceModel.ChannelFactory<TChannel> ChannelFactory { get { return default(System.ServiceModel.ChannelFactory<TChannel>); } }
        public System.ServiceModel.Description.ClientCredentials ClientCredentials { get { return default(System.ServiceModel.Description.ClientCredentials); } }
        public System.ServiceModel.Description.ServiceEndpoint Endpoint { get { return default(System.ServiceModel.Description.ServiceEndpoint); } }
        public System.ServiceModel.IClientChannel InnerChannel { get { return default(System.ServiceModel.IClientChannel); } }
        public System.ServiceModel.CommunicationState State { get { return default(System.ServiceModel.CommunicationState); } }
        event System.EventHandler System.ServiceModel.ICommunicationObject.Closed { add { } remove { } }
        event System.EventHandler System.ServiceModel.ICommunicationObject.Closing { add { } remove { } }
        event System.EventHandler System.ServiceModel.ICommunicationObject.Faulted { add { } remove { } }
        event System.EventHandler System.ServiceModel.ICommunicationObject.Opened { add { } remove { } }
        event System.EventHandler System.ServiceModel.ICommunicationObject.Opening { add { } remove { } }
        public void Abort() { }
        public void Close() { }
        protected virtual TChannel CreateChannel() { return default(TChannel); }
        public void DisplayInitializationUI() { }
        protected T GetDefaultValueForInitialization<T>() { return default(T); }
        protected void InvokeAsync(System.ServiceModel.ClientBase<TChannel>.BeginOperationDelegate beginOperationDelegate, object[] inValues, System.ServiceModel.ClientBase<TChannel>.EndOperationDelegate endOperationDelegate, System.Threading.SendOrPostCallback operationCompletedCallback, object userState) { }
        public void Open() { }
        void System.IDisposable.Dispose() { }
        System.IAsyncResult System.ServiceModel.ICommunicationObject.BeginClose(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        System.IAsyncResult System.ServiceModel.ICommunicationObject.BeginClose(System.TimeSpan timeout, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        System.IAsyncResult System.ServiceModel.ICommunicationObject.BeginOpen(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        System.IAsyncResult System.ServiceModel.ICommunicationObject.BeginOpen(System.TimeSpan timeout, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        void System.ServiceModel.ICommunicationObject.Close(System.TimeSpan timeout) { }
        void System.ServiceModel.ICommunicationObject.EndClose(System.IAsyncResult result) { }
        void System.ServiceModel.ICommunicationObject.EndOpen(System.IAsyncResult result) { }
        void System.ServiceModel.ICommunicationObject.Open(System.TimeSpan timeout) { }
        protected delegate System.IAsyncResult BeginOperationDelegate(object[] inValues, System.AsyncCallback asyncCallback, object state);
        protected internal partial class ChannelBase<T> : System.IDisposable, System.ServiceModel.Channels.IChannel, System.ServiceModel.Channels.IOutputChannel, System.ServiceModel.Channels.IRequestChannel, System.ServiceModel.IClientChannel, System.ServiceModel.ICommunicationObject, System.ServiceModel.IContextChannel, System.ServiceModel.IExtensibleObject<System.ServiceModel.IContextChannel> where T : class
        {
            protected ChannelBase(System.ServiceModel.ClientBase<T> client) { }
            public bool AllowInitializationUI { get { return default(bool); } set { } }
                    public bool AllowOutputBatching { get { return default(bool); } set { } }
            public bool DidInteractiveInitialization { get { return default(bool); } }
                    public System.ServiceModel.Channels.IInputSession InputSession { get { return default(System.ServiceModel.Channels.IInputSession); } }
            public System.ServiceModel.EndpointAddress LocalAddress { get { return default(System.ServiceModel.EndpointAddress); } }
                    public System.TimeSpan OperationTimeout { get { return default(System.TimeSpan); } set { } }
                    public System.ServiceModel.Channels.IOutputSession OutputSession { get { return default(System.ServiceModel.Channels.IOutputSession); } }
            public System.ServiceModel.EndpointAddress RemoteAddress { get { return default(System.ServiceModel.EndpointAddress); } }
                    public string SessionId { get { return default(string); } }
            System.ServiceModel.EndpointAddress System.ServiceModel.Channels.IRequestChannel.RemoteAddress { get { return default(System.ServiceModel.EndpointAddress); } }
            System.Uri System.ServiceModel.Channels.IRequestChannel.Via { get { return default(System.Uri); } }
            System.ServiceModel.CommunicationState System.ServiceModel.ICommunicationObject.State { get { return default(System.ServiceModel.CommunicationState); } }
            System.ServiceModel.IExtensionCollection<System.ServiceModel.IContextChannel> System.ServiceModel.IExtensibleObject<System.ServiceModel.IContextChannel>.Extensions { get { return default(System.ServiceModel.IExtensionCollection<System.ServiceModel.IContextChannel>); } }
            public System.Uri Via { get { return default(System.Uri); } }
            event System.EventHandler System.ServiceModel.ICommunicationObject.Closed { add { } remove { } }
            event System.EventHandler System.ServiceModel.ICommunicationObject.Closing { add { } remove { } }
            event System.EventHandler System.ServiceModel.ICommunicationObject.Faulted { add { } remove { } }
            event System.EventHandler System.ServiceModel.ICommunicationObject.Opened { add { } remove { } }
            event System.EventHandler System.ServiceModel.ICommunicationObject.Opening { add { } remove { } }
            public event System.EventHandler<System.ServiceModel.UnknownMessageReceivedEventArgs> UnknownMessageReceived { add { } remove { } }
            public System.IAsyncResult BeginDisplayInitializationUI(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
            protected System.IAsyncResult BeginInvoke(string methodName, object[] args, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
            public void DisplayInitializationUI() { }
            public void Dispose() { }
            public void EndDisplayInitializationUI(System.IAsyncResult result) { }
            protected object EndInvoke(string methodName, object[] args, System.IAsyncResult result) { return default(object); }
            protected object Invoke(string methodName, object[] args) { return default(object); }
            TProperty System.ServiceModel.Channels.IChannel.GetProperty<TProperty>() { return default(TProperty); }
            System.IAsyncResult System.ServiceModel.Channels.IOutputChannel.BeginSend(System.ServiceModel.Channels.Message message, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
            System.IAsyncResult System.ServiceModel.Channels.IOutputChannel.BeginSend(System.ServiceModel.Channels.Message message, System.TimeSpan timeout, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
            void System.ServiceModel.Channels.IOutputChannel.EndSend(System.IAsyncResult result) { }
            void System.ServiceModel.Channels.IOutputChannel.Send(System.ServiceModel.Channels.Message message) { }
            void System.ServiceModel.Channels.IOutputChannel.Send(System.ServiceModel.Channels.Message message, System.TimeSpan timeout) { }
            System.IAsyncResult System.ServiceModel.Channels.IRequestChannel.BeginRequest(System.ServiceModel.Channels.Message message, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
            System.IAsyncResult System.ServiceModel.Channels.IRequestChannel.BeginRequest(System.ServiceModel.Channels.Message message, System.TimeSpan timeout, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
            System.ServiceModel.Channels.Message System.ServiceModel.Channels.IRequestChannel.EndRequest(System.IAsyncResult result) { return default(System.ServiceModel.Channels.Message); }
            System.ServiceModel.Channels.Message System.ServiceModel.Channels.IRequestChannel.Request(System.ServiceModel.Channels.Message message) { return default(System.ServiceModel.Channels.Message); }
            System.ServiceModel.Channels.Message System.ServiceModel.Channels.IRequestChannel.Request(System.ServiceModel.Channels.Message message, System.TimeSpan timeout) { return default(System.ServiceModel.Channels.Message); }
            void System.ServiceModel.ICommunicationObject.Abort() { }
            System.IAsyncResult System.ServiceModel.ICommunicationObject.BeginClose(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
            System.IAsyncResult System.ServiceModel.ICommunicationObject.BeginClose(System.TimeSpan timeout, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
            System.IAsyncResult System.ServiceModel.ICommunicationObject.BeginOpen(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
            System.IAsyncResult System.ServiceModel.ICommunicationObject.BeginOpen(System.TimeSpan timeout, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
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
            public object[] Results { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(object[]); } }
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
        public System.ServiceModel.OperationFormatStyle Style { get { return default(System.ServiceModel.OperationFormatStyle); } set { } }
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
        public static System.Uri AnonymousUri { get { return default(System.Uri); } }
        public System.ServiceModel.Channels.AddressHeaderCollection Headers { get { return default(System.ServiceModel.Channels.AddressHeaderCollection); } }
        public System.ServiceModel.EndpointIdentity Identity { get { return default(System.ServiceModel.EndpointIdentity); } }
        public bool IsAnonymous { get { return default(bool); } }
        public bool IsNone { get { return default(bool); } }
        public static System.Uri NoneUri { get { return default(System.Uri); } }
        public System.Uri Uri { get { return default(System.Uri); } }
            public void ApplyTo(System.ServiceModel.Channels.Message message) { }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public System.Xml.XmlDictionaryReader GetReaderAtExtensions() { return default(System.Xml.XmlDictionaryReader); }
        public System.Xml.XmlDictionaryReader GetReaderAtMetadata() { return default(System.Xml.XmlDictionaryReader); }
        public static bool operator ==(System.ServiceModel.EndpointAddress address1, System.ServiceModel.EndpointAddress address2) { return default(bool); }
        public static bool operator !=(System.ServiceModel.EndpointAddress address1, System.ServiceModel.EndpointAddress address2) { return default(bool); }
        public static System.ServiceModel.EndpointAddress ReadFrom(System.ServiceModel.Channels.AddressingVersion addressingVersion, System.Xml.XmlDictionaryReader reader) { return default(System.ServiceModel.EndpointAddress); }
        public static System.ServiceModel.EndpointAddress ReadFrom(System.ServiceModel.Channels.AddressingVersion addressingVersion, System.Xml.XmlDictionaryReader reader, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString ns) { return default(System.ServiceModel.EndpointAddress); }
        public static System.ServiceModel.EndpointAddress ReadFrom(System.ServiceModel.Channels.AddressingVersion addressingVersion, System.Xml.XmlReader reader) { return default(System.ServiceModel.EndpointAddress); }
        public static System.ServiceModel.EndpointAddress ReadFrom(System.ServiceModel.Channels.AddressingVersion addressingVersion, System.Xml.XmlReader reader, string localName, string ns) { return default(System.ServiceModel.EndpointAddress); }
        public static System.ServiceModel.EndpointAddress ReadFrom(System.Xml.XmlDictionaryReader reader) { return default(System.ServiceModel.EndpointAddress); }
        public static System.ServiceModel.EndpointAddress ReadFrom(System.Xml.XmlDictionaryReader reader, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString ns) { return default(System.ServiceModel.EndpointAddress); }
        public override string ToString() { return default(string); }
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
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Channels.AddressHeader> Headers { get { return default(System.Collections.ObjectModel.Collection<System.ServiceModel.Channels.AddressHeader>); } }
        public System.Uri Uri { get { return default(System.Uri); } set { } }
        public System.ServiceModel.EndpointAddress ToEndpointAddress() { return default(System.ServiceModel.EndpointAddress); }
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
        public string NextDestinationActorValue { get { return default(string); } }
        public static System.ServiceModel.EnvelopeVersion None { get { return default(System.ServiceModel.EnvelopeVersion); } }
        public static System.ServiceModel.EnvelopeVersion Soap11 { get { return default(System.ServiceModel.EnvelopeVersion); } }
        public static System.ServiceModel.EnvelopeVersion Soap12 { get { return default(System.ServiceModel.EnvelopeVersion); } }
        public string[] GetUltimateDestinationActorValues() { return default(string[]); }
        public override string ToString() { return default(string); }
    }
    [System.Runtime.Serialization.DataContractAttribute]
    public partial class ExceptionDetail
    {
        public ExceptionDetail(System.Exception exception) { }
        [System.Runtime.Serialization.DataMemberAttribute]
        public string HelpLink { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [System.Runtime.Serialization.DataMemberAttribute]
        public System.ServiceModel.ExceptionDetail InnerException { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.ExceptionDetail); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [System.Runtime.Serialization.DataMemberAttribute]
        public string Message { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [System.Runtime.Serialization.DataMemberAttribute]
        public string StackTrace { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [System.Runtime.Serialization.DataMemberAttribute]
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override string ToString() { return default(string); }
    }
    public sealed partial class ExtensionCollection<T> : System.Collections.Generic.SynchronizedCollection<System.ServiceModel.IExtension<T>>, System.Collections.Generic.ICollection<System.ServiceModel.IExtension<T>>, System.Collections.Generic.IEnumerable<System.ServiceModel.IExtension<T>>, System.Collections.IEnumerable, System.ServiceModel.IExtensionCollection<T> where T : System.ServiceModel.IExtensibleObject<T>
    {
        public ExtensionCollection(T owner) { }
        public ExtensionCollection(T owner, object syncRoot) { }
        bool System.Collections.Generic.ICollection<System.ServiceModel.IExtension<T>>.IsReadOnly { get { return default(bool); } }
            protected override void ClearItems() { }
        public E Find<E>() { return default(E); }
        public System.Collections.ObjectModel.Collection<E> FindAll<E>() { return default(System.Collections.ObjectModel.Collection<E>); }
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
        public bool IsPredefinedFault { get { return default(bool); } }
        public bool IsReceiverFault { get { return default(bool); } }
        public bool IsSenderFault { get { return default(bool); } }
        public string Name { get { return default(string); } }
        public string Namespace { get { return default(string); } }
        public System.ServiceModel.FaultCode SubCode { get { return default(System.ServiceModel.FaultCode); } }
        public static System.ServiceModel.FaultCode CreateReceiverFaultCode(System.ServiceModel.FaultCode subcode) { return default(System.ServiceModel.FaultCode); }
        public static System.ServiceModel.FaultCode CreateReceiverFaultCode(string name, string ns) { return default(System.ServiceModel.FaultCode); }
        public static System.ServiceModel.FaultCode CreateSenderFaultCode(System.ServiceModel.FaultCode subcode) { return default(System.ServiceModel.FaultCode); }
        public static System.ServiceModel.FaultCode CreateSenderFaultCode(string name, string ns) { return default(System.ServiceModel.FaultCode); }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false, AllowMultiple=true)]
    public sealed partial class FaultContractAttribute : System.Attribute
    {
        public FaultContractAttribute(System.Type detailType) { }
        public string Action { get { return default(string); } set { } }
        public System.Type DetailType { get { return default(System.Type); } }
        public bool HasProtectionLevel { get { return default(bool); } }
        public string Name { get { return default(string); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { return default(System.Net.Security.ProtectionLevel); } set { } }
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
        public string Action { get { return default(string); } }
        public System.ServiceModel.FaultCode Code { get { return default(System.ServiceModel.FaultCode); } }
        public override string Message { get { return default(string); } }
        public System.ServiceModel.FaultReason Reason { get { return default(System.ServiceModel.FaultReason); } }
            public static System.ServiceModel.FaultException CreateFault(System.ServiceModel.Channels.MessageFault messageFault, string action, params System.Type[] faultDetailTypes) { return default(System.ServiceModel.FaultException); }
            public static System.ServiceModel.FaultException CreateFault(System.ServiceModel.Channels.MessageFault fault, params System.Type[] details) { return default(System.ServiceModel.FaultException); }
        public virtual System.ServiceModel.Channels.MessageFault CreateMessageFault() { return default(System.ServiceModel.Channels.MessageFault); }
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
        public TDetail Detail { get { return default(TDetail); } }
        public override System.ServiceModel.Channels.MessageFault CreateMessageFault() { return default(System.ServiceModel.Channels.MessageFault); }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { return default(string); }
    }
    public partial class FaultReason
    {
        public FaultReason(System.Collections.Generic.IEnumerable<System.ServiceModel.FaultReasonText> translations) { }
        public FaultReason(System.ServiceModel.FaultReasonText translation) { }
        public FaultReason(string text) { }
        public System.Collections.Generic.SynchronizedReadOnlyCollection<System.ServiceModel.FaultReasonText> Translations { get { return default(System.Collections.Generic.SynchronizedReadOnlyCollection<System.ServiceModel.FaultReasonText>); } }
        public System.ServiceModel.FaultReasonText GetMatchingTranslation() { return default(System.ServiceModel.FaultReasonText); }
        public System.ServiceModel.FaultReasonText GetMatchingTranslation(System.Globalization.CultureInfo cultureInfo) { return default(System.ServiceModel.FaultReasonText); }
        public override string ToString() { return default(string); }
    }
    public partial class FaultReasonText
    {
        public FaultReasonText(string text) { }
        public FaultReasonText(string text, System.Globalization.CultureInfo cultureInfo) { }
        public FaultReasonText(string text, string xmlLang) { }
        public string Text { get { return default(string); } }
        public string XmlLang { get { return default(string); } }
        public bool Matches(System.Globalization.CultureInfo cultureInfo) { return default(bool); }
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
        public bool AllowCookies { get { return default(bool); } set { } }
        public bool BypassProxyOnLocal { get { return default(bool); } set { } }
        public System.ServiceModel.EnvelopeVersion EnvelopeVersion { get { return default(System.ServiceModel.EnvelopeVersion); } }
        public System.ServiceModel.HostNameComparisonMode HostNameComparisonMode { get { return default(System.ServiceModel.HostNameComparisonMode); } set { } }
        public long MaxBufferPoolSize { get { return default(long); } set { } }
        public int MaxBufferSize { get { return default(int); } set { } }
        public long MaxReceivedMessageSize { get { return default(long); } set { } }
        public System.Uri ProxyAddress { get { return default(System.Uri); } set { } }
        public System.Xml.XmlDictionaryReaderQuotas ReaderQuotas { get { return default(System.Xml.XmlDictionaryReaderQuotas); } set { } }
        public abstract override string Scheme { get; }
        bool System.ServiceModel.Channels.IBindingRuntimePreferences.ReceiveSynchronously { get { return default(bool); } }
        public System.Text.Encoding TextEncoding { get { return default(System.Text.Encoding); } set { } }
        public System.ServiceModel.TransferMode TransferMode { get { return default(System.ServiceModel.TransferMode); } set { } }
        public bool UseDefaultWebProxy { get { return default(bool); } set { } }
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
        public System.ServiceModel.HttpClientCredentialType ClientCredentialType { get { return default(System.ServiceModel.HttpClientCredentialType); } set { } }
            public System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy ExtendedProtectionPolicy { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.ServiceModel.HttpProxyCredentialType ProxyCredentialType { get { return default(System.ServiceModel.HttpProxyCredentialType); } set { } }
        public string Realm { get { return default(string); } set { } }
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
        public int Order { get { return default(int); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(12))]
    public sealed partial class MessageContractAttribute : System.Attribute
    {
        public MessageContractAttribute() { }
        public bool HasProtectionLevel { get { return default(bool); } }
        public bool IsWrapped { get { return default(bool); } set { } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { return default(System.Net.Security.ProtectionLevel); } set { } }
        public string WrapperName { get { return default(string); } set { } }
        public string WrapperNamespace { get { return default(string); } set { } }
    }
    public abstract partial class MessageContractMemberAttribute : System.Attribute
    {
        protected MessageContractMemberAttribute() { }
        public bool HasProtectionLevel { get { return default(bool); } }
        public string Name { get { return default(string); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { return default(System.Net.Security.ProtectionLevel); } set { } }
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
        public string Actor { get { return default(string); } set { } }
        public T Content { get { return default(T); } set { } }
        public bool MustUnderstand { get { return default(bool); } set { } }
        public bool Relay { get { return default(bool); } set { } }
        public System.ServiceModel.Channels.MessageHeader GetUntypedHeader(string name, string ns) { return default(System.ServiceModel.Channels.MessageHeader); }
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
        public string Actor { get { return default(string); } set { } }
        public bool MustUnderstand { get { return default(bool); } set { } }
        public bool Relay { get { return default(bool); } set { } }
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
        public string HeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
        public string HeaderNamespace { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
        public bool IsDuplicate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10240), Inherited=false)]
    public sealed partial class MessageParameterAttribute : System.Attribute
    {
        public MessageParameterAttribute() { }
        public string Name { get { return default(string); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(384), Inherited=false)]
    public sealed partial class MessagePropertyAttribute : System.Attribute
    {
        public MessagePropertyAttribute() { }
        public string Name { get { return default(string); } set { } }
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
        public System.ServiceModel.NetHttpMessageEncoding MessageEncoding { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.NetHttpMessageEncoding); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.ServiceModel.OptionalReliableSession ReliableSession { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.OptionalReliableSession); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override string Scheme { get { return default(string); } }
        public System.ServiceModel.BasicHttpSecurity Security { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.BasicHttpSecurity); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.ServiceModel.Channels.WebSocketTransportSettings WebSocketSettings { get { return default(System.ServiceModel.Channels.WebSocketTransportSettings); } }
        public override System.ServiceModel.Channels.BindingElementCollection CreateBindingElements() { return default(System.ServiceModel.Channels.BindingElementCollection); }
        public bool ShouldSerializeReliableSession() { return default(bool); }
        public bool ShouldSerializeSecurity() { return default(bool); }
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
        public System.ServiceModel.IContextChannel Channel { get { return default(System.ServiceModel.IContextChannel); } }
        public static System.ServiceModel.OperationContext Current { get { return default(System.ServiceModel.OperationContext); } set { } }
        public System.ServiceModel.IExtensionCollection<System.ServiceModel.OperationContext> Extensions { get { return default(System.ServiceModel.IExtensionCollection<System.ServiceModel.OperationContext>); } }
        public System.ServiceModel.Channels.MessageHeaders IncomingMessageHeaders { get { return default(System.ServiceModel.Channels.MessageHeaders); } }
        public System.ServiceModel.Channels.MessageProperties IncomingMessageProperties { get { return default(System.ServiceModel.Channels.MessageProperties); } }
        public System.ServiceModel.Channels.MessageVersion IncomingMessageVersion { get { return default(System.ServiceModel.Channels.MessageVersion); } }
            public System.ServiceModel.InstanceContext InstanceContext { get { return default(System.ServiceModel.InstanceContext); } }
        public bool IsUserContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
        public System.ServiceModel.Channels.MessageHeaders OutgoingMessageHeaders { get { return default(System.ServiceModel.Channels.MessageHeaders); } }
        public System.ServiceModel.Channels.MessageProperties OutgoingMessageProperties { get { return default(System.ServiceModel.Channels.MessageProperties); } }
        public System.ServiceModel.Channels.RequestContext RequestContext { get { return default(System.ServiceModel.Channels.RequestContext); } set { } }
        public string SessionId { get { return default(string); } }
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
        public string Action { get { return default(string); } set { } }
        public bool AsyncPattern { get { return default(bool); } set { } }
        public bool HasProtectionLevel { get { return default(bool); } }
        public bool IsInitiating { get { return default(bool); } set { } }
        public bool IsOneWay { get { return default(bool); } set { } }
        public bool IsTerminating { get { return default(bool); } set { } }
        public string Name { get { return default(string); } set { } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { return default(System.Net.Security.ProtectionLevel); } set { } }
        public string ReplyAction { get { return default(string); } set { } }
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
        public bool Enabled { get { return default(bool); } set { } }
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
        public static System.ServiceModel.ReliableMessagingVersion Default { get { return default(System.ServiceModel.ReliableMessagingVersion); } }
        public static System.ServiceModel.ReliableMessagingVersion WSReliableMessaging11 { get { return default(System.ServiceModel.ReliableMessagingVersion); } }
        public static System.ServiceModel.ReliableMessagingVersion WSReliableMessagingFebruary2005 { get { return default(System.ServiceModel.ReliableMessagingVersion); } }
    }
    public partial class ReliableSession
    {
        public ReliableSession() { }
        public ReliableSession(System.ServiceModel.Channels.ReliableSessionBindingElement binding) { }
        public System.TimeSpan InactivityTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.TimeSpan); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Ordered { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
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
        public System.Type CallbackContract { get { return default(System.Type); } set { } }
        public string ConfigurationName { get { return default(string); } set { } }
        public bool HasProtectionLevel { get { return default(bool); } }
        public string Name { get { return default(string); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { return default(System.Net.Security.ProtectionLevel); } set { } }
        public System.ServiceModel.SessionMode SessionMode { get { return default(System.ServiceModel.SessionMode); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1092), AllowMultiple=true, Inherited=true)]
    public sealed partial class ServiceKnownTypeAttribute : System.Attribute
    {
        public ServiceKnownTypeAttribute(string methodName) { }
        public ServiceKnownTypeAttribute(string methodName, System.Type declaringType) { }
        public ServiceKnownTypeAttribute(System.Type type) { }
        public System.Type DeclaringType { get { return default(System.Type); } }
        public string MethodName { get { return default(string); } }
        public System.Type Type { get { return default(System.Type); } }
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
        public System.ServiceModel.Channels.Message Message { get { return default(System.ServiceModel.Channels.Message); } }
    }
    public partial class UriSchemeKeyedCollection : System.Collections.Generic.SynchronizedKeyedCollection<string, System.Uri>
    {
        public UriSchemeKeyedCollection(params System.Uri[] uris) { }
        protected override string GetKeyForItem(System.Uri item) { return default(string); }
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
        public System.ServiceModel.OperationFormatStyle Style { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.OperationFormatStyle); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public bool SupportFaults { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.ServiceModel.OperationFormatUse Use { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.OperationFormatUse); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
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
        public static System.ServiceModel.Channels.AddressHeader CreateAddressHeader(object value) { return default(System.ServiceModel.Channels.AddressHeader); }
        public static System.ServiceModel.Channels.AddressHeader CreateAddressHeader(object value, System.Runtime.Serialization.XmlObjectSerializer formatter) { return default(System.ServiceModel.Channels.AddressHeader); }
        public static System.ServiceModel.Channels.AddressHeader CreateAddressHeader(string name, string ns, object value) { return default(System.ServiceModel.Channels.AddressHeader); }
        public static System.ServiceModel.Channels.AddressHeader CreateAddressHeader(string name, string ns, object value, System.Runtime.Serialization.XmlObjectSerializer formatter) { return default(System.ServiceModel.Channels.AddressHeader); }
        public override bool Equals(object obj) { return default(bool); }
        public virtual System.Xml.XmlDictionaryReader GetAddressHeaderReader() { return default(System.Xml.XmlDictionaryReader); }
        public override int GetHashCode() { return default(int); }
        public T GetValue<T>() { return default(T); }
        public T GetValue<T>(System.Runtime.Serialization.XmlObjectSerializer formatter) { return default(T); }
        protected abstract void OnWriteAddressHeaderContents(System.Xml.XmlDictionaryWriter writer);
        protected virtual void OnWriteStartAddressHeader(System.Xml.XmlDictionaryWriter writer) { }
        public System.ServiceModel.Channels.MessageHeader ToMessageHeader() { return default(System.ServiceModel.Channels.MessageHeader); }
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
        public System.ServiceModel.Channels.AddressHeader[] FindAll(string name, string ns) { return default(System.ServiceModel.Channels.AddressHeader[]); }
        public System.ServiceModel.Channels.AddressHeader FindHeader(string name, string ns) { return default(System.ServiceModel.Channels.AddressHeader); }
    }
    public sealed partial class AddressingVersion
    {
        internal AddressingVersion() { }
        public static System.ServiceModel.Channels.AddressingVersion None { get { return default(System.ServiceModel.Channels.AddressingVersion); } }
        public static System.ServiceModel.Channels.AddressingVersion WSAddressing10 { get { return default(System.ServiceModel.Channels.AddressingVersion); } }
        public static System.ServiceModel.Channels.AddressingVersion WSAddressingAugust2004 { get { return default(System.ServiceModel.Channels.AddressingVersion); } }
        public override string ToString() { return default(string); }
    }
    public sealed partial class BinaryMessageEncodingBindingElement : System.ServiceModel.Channels.MessageEncodingBindingElement, System.ServiceModel.Description.IPolicyExportExtension, System.ServiceModel.Description.IWsdlExportExtension
    {
        public BinaryMessageEncodingBindingElement() { }
            public System.ServiceModel.Channels.CompressionFormat CompressionFormat { get { return default(System.ServiceModel.Channels.CompressionFormat); } set { } }
        public int MaxReadPoolSize { get { return default(int); } set { } }
        public int MaxSessionSize { get { return default(int); } set { } }
        public int MaxWritePoolSize { get { return default(int); } set { } }
        public override System.ServiceModel.Channels.MessageVersion MessageVersion { get { return default(System.ServiceModel.Channels.MessageVersion); } set { } }
        public System.Xml.XmlDictionaryReaderQuotas ReaderQuotas { get { return default(System.Xml.XmlDictionaryReaderQuotas); } set { } }
        public override System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { return default(System.ServiceModel.Channels.IChannelFactory<TChannel>); }
        public override System.ServiceModel.Channels.BindingElement Clone() { return default(System.ServiceModel.Channels.BindingElement); }
        public override System.ServiceModel.Channels.MessageEncoderFactory CreateMessageEncoderFactory() { return default(System.ServiceModel.Channels.MessageEncoderFactory); }
        public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) { return default(T); }
    }
    public abstract partial class Binding : System.ServiceModel.IDefaultCommunicationTimeouts
    {
        protected Binding() { }
        protected Binding(string name, string ns) { }
        public System.TimeSpan CloseTimeout { get { return default(System.TimeSpan); } set { } }
        public System.ServiceModel.Channels.MessageVersion MessageVersion { get { return default(System.ServiceModel.Channels.MessageVersion); } }
        public string Name { get { return default(string); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public System.TimeSpan OpenTimeout { get { return default(System.TimeSpan); } set { } }
        public System.TimeSpan ReceiveTimeout { get { return default(System.TimeSpan); } set { } }
        public abstract string Scheme { get; }
        public System.TimeSpan SendTimeout { get { return default(System.TimeSpan); } set { } }
        public System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(params object[] parameters) { return default(System.ServiceModel.Channels.IChannelFactory<TChannel>); }
        public virtual System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingParameterCollection parameters) { return default(System.ServiceModel.Channels.IChannelFactory<TChannel>); }
        public bool CanBuildChannelFactory<TChannel>(params object[] parameters) { return default(bool); }
        public virtual bool CanBuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingParameterCollection parameters) { return default(bool); }
        public abstract System.ServiceModel.Channels.BindingElementCollection CreateBindingElements();
        public T GetProperty<T>(System.ServiceModel.Channels.BindingParameterCollection parameters) where T : class { return default(T); }
    }
    public partial class BindingContext
    {
        public BindingContext(System.ServiceModel.Channels.CustomBinding binding, System.ServiceModel.Channels.BindingParameterCollection parms) { }
        public BindingContext(System.ServiceModel.Channels.CustomBinding binding, System.ServiceModel.Channels.BindingParameterCollection parameters, System.Uri listenUriBaseAddress, string listenUriRelativeAddress, System.ServiceModel.Description.ListenUriMode listenUriMode) { }
        public System.ServiceModel.Channels.CustomBinding Binding { get { return default(System.ServiceModel.Channels.CustomBinding); } }
        public System.ServiceModel.Channels.BindingParameterCollection BindingParameters { get { return default(System.ServiceModel.Channels.BindingParameterCollection); } }
        public System.Uri ListenUriBaseAddress { get { return default(System.Uri); } set { } }
        public System.ServiceModel.Description.ListenUriMode ListenUriMode { get { return default(System.ServiceModel.Description.ListenUriMode); } set { } }
        public string ListenUriRelativeAddress { get { return default(string); } set { } }
        public System.ServiceModel.Channels.BindingElementCollection RemainingBindingElements { get { return default(System.ServiceModel.Channels.BindingElementCollection); } }
        public System.ServiceModel.Channels.IChannelFactory<TChannel> BuildInnerChannelFactory<TChannel>() { return default(System.ServiceModel.Channels.IChannelFactory<TChannel>); }
        public bool CanBuildInnerChannelFactory<TChannel>() { return default(bool); }
        public System.ServiceModel.Channels.BindingContext Clone() { return default(System.ServiceModel.Channels.BindingContext); }
        public T GetInnerProperty<T>() where T : class { return default(T); }
    }
    public abstract partial class BindingElement
    {
        protected BindingElement() { }
            protected BindingElement(System.ServiceModel.Channels.BindingElement other) { }
        public virtual System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { return default(System.ServiceModel.Channels.IChannelFactory<TChannel>); }
        public virtual bool CanBuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { return default(bool); }
        public abstract System.ServiceModel.Channels.BindingElement Clone();
        public abstract T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) where T : class;
    }
    public partial class BindingElementCollection : System.Collections.ObjectModel.Collection<System.ServiceModel.Channels.BindingElement>
    {
        public BindingElementCollection() { }
        public BindingElementCollection(System.Collections.Generic.IEnumerable<System.ServiceModel.Channels.BindingElement> bindings) { }
        public BindingElementCollection(System.ServiceModel.Channels.BindingElement[] bindings) { }
        public void AddRange(params System.ServiceModel.Channels.BindingElement[] elements) { }
        public System.ServiceModel.Channels.BindingElementCollection Clone() { return default(System.ServiceModel.Channels.BindingElementCollection); }
        public bool Contains(System.Type bindingElementType) { return default(bool); }
        public T Find<T>() { return default(T); }
        public System.Collections.ObjectModel.Collection<T> FindAll<T>() { return default(System.Collections.ObjectModel.Collection<T>); }
        protected override void InsertItem(int index, System.ServiceModel.Channels.BindingElement item) { }
        public T Remove<T>() { return default(T); }
        public System.Collections.ObjectModel.Collection<T> RemoveAll<T>() { return default(System.Collections.ObjectModel.Collection<T>); }
        protected override void SetItem(int index, System.ServiceModel.Channels.BindingElement item) { }
    }
    public partial class BindingParameterCollection : System.Collections.Generic.KeyedByTypeCollection<object>
    {
        public BindingParameterCollection() { }
        protected override System.Type GetKeyForItem(object item) { return default(System.Type); }
        protected override void InsertItem(int index, object item) { }
        protected override void SetItem(int index, object item) { }
    }
    public abstract partial class BodyWriter
    {
        protected BodyWriter(bool isBuffered) { }
        public bool IsBuffered { get { return default(bool); } }
        public System.ServiceModel.Channels.BodyWriter CreateBufferedCopy(int maxBufferSize) { return default(System.ServiceModel.Channels.BodyWriter); }
        protected virtual System.ServiceModel.Channels.BodyWriter OnCreateBufferedCopy(int maxBufferSize) { return default(System.ServiceModel.Channels.BodyWriter); }
        protected abstract void OnWriteBodyContents(System.Xml.XmlDictionaryWriter writer);
        public void WriteBodyContents(System.Xml.XmlDictionaryWriter writer) { }
    }
    public abstract partial class BufferManager
    {
        protected BufferManager() { }
        public abstract void Clear();
        public static System.ServiceModel.Channels.BufferManager CreateBufferManager(long maxBufferPoolSize, int maxBufferSize) { return default(System.ServiceModel.Channels.BufferManager); }
        public abstract void ReturnBuffer(byte[] buffer);
        public abstract byte[] TakeBuffer(int bufferSize);
    }
    public abstract partial class ChannelBase : System.ServiceModel.Channels.CommunicationObject, System.ServiceModel.Channels.IChannel, System.ServiceModel.ICommunicationObject, System.ServiceModel.IDefaultCommunicationTimeouts
    {
        protected ChannelBase(System.ServiceModel.Channels.ChannelManagerBase manager) { }
        protected internal override System.TimeSpan DefaultCloseTimeout { get { return default(System.TimeSpan); } }
        protected internal override System.TimeSpan DefaultOpenTimeout { get { return default(System.TimeSpan); } }
        protected internal System.TimeSpan DefaultReceiveTimeout { get { return default(System.TimeSpan); } }
        protected internal System.TimeSpan DefaultSendTimeout { get { return default(System.TimeSpan); } }
        protected System.ServiceModel.Channels.ChannelManagerBase Manager { get { return default(System.ServiceModel.Channels.ChannelManagerBase); } }
        System.TimeSpan System.ServiceModel.IDefaultCommunicationTimeouts.CloseTimeout { get { return default(System.TimeSpan); } }
        System.TimeSpan System.ServiceModel.IDefaultCommunicationTimeouts.OpenTimeout { get { return default(System.TimeSpan); } }
        System.TimeSpan System.ServiceModel.IDefaultCommunicationTimeouts.ReceiveTimeout { get { return default(System.TimeSpan); } }
        System.TimeSpan System.ServiceModel.IDefaultCommunicationTimeouts.SendTimeout { get { return default(System.TimeSpan); } }
        public virtual T GetProperty<T>() where T : class { return default(T); }
        protected override void OnClosed() { }
    }
    public abstract partial class ChannelFactoryBase : System.ServiceModel.Channels.ChannelManagerBase, System.ServiceModel.Channels.IChannelFactory, System.ServiceModel.ICommunicationObject
    {
        protected ChannelFactoryBase() { }
        protected ChannelFactoryBase(System.ServiceModel.IDefaultCommunicationTimeouts timeouts) { }
        protected internal override System.TimeSpan DefaultCloseTimeout { get { return default(System.TimeSpan); } }
        protected internal override System.TimeSpan DefaultOpenTimeout { get { return default(System.TimeSpan); } }
        protected internal override System.TimeSpan DefaultReceiveTimeout { get { return default(System.TimeSpan); } }
        protected internal override System.TimeSpan DefaultSendTimeout { get { return default(System.TimeSpan); } }
        public virtual T GetProperty<T>() where T : class { return default(T); }
        protected override void OnAbort() { }
        protected override System.IAsyncResult OnBeginClose(System.TimeSpan timeout, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        protected override void OnClose(System.TimeSpan timeout) { }
        protected override void OnEndClose(System.IAsyncResult result) { }
    }
    public abstract partial class ChannelFactoryBase<TChannel> : System.ServiceModel.Channels.ChannelFactoryBase, System.ServiceModel.Channels.IChannelFactory, System.ServiceModel.Channels.IChannelFactory<TChannel>, System.ServiceModel.ICommunicationObject
    {
        protected ChannelFactoryBase() { }
        protected ChannelFactoryBase(System.ServiceModel.IDefaultCommunicationTimeouts timeouts) { }
        public TChannel CreateChannel(System.ServiceModel.EndpointAddress remoteAddress) { return default(TChannel); }
        public TChannel CreateChannel(System.ServiceModel.EndpointAddress remoteAddress, System.Uri via) { return default(TChannel); }
        protected override void OnAbort() { }
        protected override System.IAsyncResult OnBeginClose(System.TimeSpan timeout, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
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
        System.TimeSpan System.ServiceModel.IDefaultCommunicationTimeouts.CloseTimeout { get { return default(System.TimeSpan); } }
        System.TimeSpan System.ServiceModel.IDefaultCommunicationTimeouts.OpenTimeout { get { return default(System.TimeSpan); } }
        System.TimeSpan System.ServiceModel.IDefaultCommunicationTimeouts.ReceiveTimeout { get { return default(System.TimeSpan); } }
        System.TimeSpan System.ServiceModel.IDefaultCommunicationTimeouts.SendTimeout { get { return default(System.TimeSpan); } }
    }
    public partial class ChannelParameterCollection : System.Collections.ObjectModel.Collection<object>
    {
        public ChannelParameterCollection() { }
        public ChannelParameterCollection(System.ServiceModel.Channels.IChannel channel) { }
        protected virtual System.ServiceModel.Channels.IChannel Channel { get { return default(System.ServiceModel.Channels.IChannel); } }
        protected override void ClearItems() { }
        protected override void InsertItem(int index, object item) { }
        public void PropagateChannelParameters(System.ServiceModel.Channels.IChannel innerChannel) { }
        protected override void RemoveItem(int index) { }
        protected override void SetItem(int index, object item) { }
    }
    public partial class ChannelPoolSettings
    {
            public ChannelPoolSettings() { }
            public System.TimeSpan IdleTimeout { get { return default(System.TimeSpan); } set { } }
            public System.TimeSpan LeaseTimeout { get { return default(System.TimeSpan); } set { } }
            public int MaxOutboundChannelsPerEndpoint { get { return default(int); } set { } }
    }
    public abstract partial class CommunicationObject : System.ServiceModel.ICommunicationObject
    {
        protected CommunicationObject() { }
        protected CommunicationObject(object mutex) { }
        protected internal abstract System.TimeSpan DefaultCloseTimeout { get; }
        protected internal abstract System.TimeSpan DefaultOpenTimeout { get; }
        protected bool IsDisposed { get { return default(bool); } }
        public System.ServiceModel.CommunicationState State { get { return default(System.ServiceModel.CommunicationState); } }
        protected object ThisLock { get { return default(object); } }
        public event System.EventHandler Closed { add { } remove { } }
        public event System.EventHandler Closing { add { } remove { } }
        public event System.EventHandler Faulted { add { } remove { } }
        public event System.EventHandler Opened { add { } remove { } }
        public event System.EventHandler Opening { add { } remove { } }
        public void Abort() { }
        public System.IAsyncResult BeginClose(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public System.IAsyncResult BeginClose(System.TimeSpan timeout, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public System.IAsyncResult BeginOpen(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public System.IAsyncResult BeginOpen(System.TimeSpan timeout, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public void Close() { }
        public void Close(System.TimeSpan timeout) { }
        public void EndClose(System.IAsyncResult result) { }
        public void EndOpen(System.IAsyncResult result) { }
        protected void Fault() { }
        protected virtual System.Type GetCommunicationObjectType() { return default(System.Type); }
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
        public System.ServiceModel.Channels.BindingElementCollection Elements { get { return default(System.ServiceModel.Channels.BindingElementCollection); } }
        public override string Scheme { get { return default(string); } }
        public override System.ServiceModel.Channels.BindingElementCollection CreateBindingElements() { return default(System.ServiceModel.Channels.BindingElementCollection); }
    }
    public abstract partial class FaultConverter
    {
        protected FaultConverter() { }
        public static System.ServiceModel.Channels.FaultConverter GetDefaultFaultConverter(System.ServiceModel.Channels.MessageVersion version) { return default(System.ServiceModel.Channels.FaultConverter); }
            protected abstract bool OnTryCreateException(System.ServiceModel.Channels.Message message, System.ServiceModel.Channels.MessageFault fault, out System.Exception error);
            protected abstract bool OnTryCreateFaultMessage(System.Exception error, out System.ServiceModel.Channels.Message message);
        public bool TryCreateException(System.ServiceModel.Channels.Message message, System.ServiceModel.Channels.MessageFault fault, out System.Exception error) { error = default(System.Exception); return default(bool); }
        public bool TryCreateFaultMessage(System.Exception error, out System.ServiceModel.Channels.Message message) { message = default(System.ServiceModel.Channels.Message); return default(bool); }
    }
    [System.ObsoleteAttribute("Use AllowCookies.")]
    public partial class HttpCookieContainerBindingElement : System.ServiceModel.Channels.BindingElement
    {
        public HttpCookieContainerBindingElement() { }
        protected HttpCookieContainerBindingElement(System.ServiceModel.Channels.HttpCookieContainerBindingElement elementToBeCloned) { }
        public override System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { return default(System.ServiceModel.Channels.IChannelFactory<TChannel>); }
        public override System.ServiceModel.Channels.BindingElement Clone() { return default(System.ServiceModel.Channels.BindingElement); }
        public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) { return default(T); }
    }
    public sealed partial class HttpRequestMessageProperty : System.ServiceModel.Channels.IMessageProperty
    {
        public HttpRequestMessageProperty() { }
        public System.Net.WebHeaderCollection Headers { get { return default(System.Net.WebHeaderCollection); } }
        public string Method { get { return default(string); } set { } }
        public static string Name { get { return default(string); } }
        public string QueryString { get { return default(string); } set { } }
        public bool SuppressEntityBody { get { return default(bool); } set { } }
        System.ServiceModel.Channels.IMessageProperty System.ServiceModel.Channels.IMessageProperty.CreateCopy() { return default(System.ServiceModel.Channels.IMessageProperty); }
    }
    public sealed partial class HttpResponseMessageProperty : System.ServiceModel.Channels.IMessageProperty
    {
        public HttpResponseMessageProperty() { }
        public System.Net.WebHeaderCollection Headers { get { return default(System.Net.WebHeaderCollection); } }
        public static string Name { get { return default(string); } }
        public System.Net.HttpStatusCode StatusCode { get { return default(System.Net.HttpStatusCode); } set { } }
        public string StatusDescription { get { return default(string); } set { } }
        public bool SuppressEntityBody { get { return default(bool); } set { } }
        System.ServiceModel.Channels.IMessageProperty System.ServiceModel.Channels.IMessageProperty.CreateCopy() { return default(System.ServiceModel.Channels.IMessageProperty); }
    }
    public partial class HttpsTransportBindingElement : System.ServiceModel.Channels.HttpTransportBindingElement, System.ServiceModel.Channels.ITransportTokenAssertionProvider, System.ServiceModel.Description.IPolicyExportExtension, System.ServiceModel.Description.IWsdlExportExtension
    {
        public HttpsTransportBindingElement() { }
        protected HttpsTransportBindingElement(System.ServiceModel.Channels.HttpsTransportBindingElement other) { }
        public bool RequireClientCertificate { get { return default(bool); } set { } }
        public override string Scheme { get { return default(string); } }
        public override System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { return default(System.ServiceModel.Channels.IChannelFactory<TChannel>); }
        public override System.ServiceModel.Channels.BindingElement Clone() { return default(System.ServiceModel.Channels.BindingElement); }
        public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) { return default(T); }
    }
    public partial class HttpTransportBindingElement : System.ServiceModel.Channels.TransportBindingElement, System.ServiceModel.Description.IPolicyExportExtension, System.ServiceModel.Description.IWsdlExportExtension
    {
        public HttpTransportBindingElement() { }
        protected HttpTransportBindingElement(System.ServiceModel.Channels.HttpTransportBindingElement other) { }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool AllowCookies { get { return default(bool); } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Net.AuthenticationSchemes)(32768))]
        public System.Net.AuthenticationSchemes AuthenticationScheme { get { return default(System.Net.AuthenticationSchemes); } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool BypassProxyOnLocal { get { return default(bool); } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
            public bool DecompressionEnabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy ExtendedProtectionPolicy { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [System.ComponentModel.DefaultValueAttribute((System.ServiceModel.HostNameComparisonMode)(0))]
        public System.ServiceModel.HostNameComparisonMode HostNameComparisonMode { get { return default(System.ServiceModel.HostNameComparisonMode); } set { } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool KeepAliveEnabled { get { return default(bool); } set { } }
            [System.ObsoleteAttribute("Use ExtendedProtectionPolicy")]
        public object LegacyExtendedProtectionPolicy { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(object); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [System.ComponentModel.DefaultValueAttribute(65536)]
        public int MaxBufferSize { get { return default(int); } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.ComponentModel.TypeConverterAttribute(typeof(System.UriTypeConverter))]
        public System.Uri ProxyAddress { get { return default(System.Uri); } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Net.AuthenticationSchemes)(32768))]
        public System.Net.AuthenticationSchemes ProxyAuthenticationScheme { get { return default(System.Net.AuthenticationSchemes); } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        public string Realm { get { return default(string); } set { } }
        public override string Scheme { get { return default(string); } }
        [System.ComponentModel.DefaultValueAttribute((System.ServiceModel.TransferMode)(0))]
        public System.ServiceModel.TransferMode TransferMode { get { return default(System.ServiceModel.TransferMode); } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool UnsafeConnectionNtlmAuthentication { get { return default(bool); } set { } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool UseDefaultWebProxy { get { return default(bool); } set { } }
        public System.ServiceModel.Channels.WebSocketTransportSettings WebSocketSettings { get { return default(System.ServiceModel.Channels.WebSocketTransportSettings); } set { } }
        public override System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { return default(System.ServiceModel.Channels.IChannelFactory<TChannel>); }
        public override bool CanBuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { return default(bool); }
        public override System.ServiceModel.Channels.BindingElement Clone() { return default(System.ServiceModel.Channels.BindingElement); }
        public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) { return default(T); }
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
        public System.TimeSpan MaxClockSkew { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.TimeSpan); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan ReplayWindow { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.TimeSpan); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan TimestampValidityDuration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.TimeSpan); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.ServiceModel.Channels.LocalClientSecuritySettings Clone() { return default(System.ServiceModel.Channels.LocalClientSecuritySettings); }
    }
    public abstract partial class Message : System.IDisposable
    {
        protected Message() { }
        public abstract System.ServiceModel.Channels.MessageHeaders Headers { get; }
        protected bool IsDisposed { get { return default(bool); } }
        public virtual bool IsEmpty { get { return default(bool); } }
        public virtual bool IsFault { get { return default(bool); } }
        public abstract System.ServiceModel.Channels.MessageProperties Properties { get; }
        public System.ServiceModel.Channels.MessageState State { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.Channels.MessageState); } }
        public abstract System.ServiceModel.Channels.MessageVersion Version { get; }
        public void Close() { }
        public System.ServiceModel.Channels.MessageBuffer CreateBufferedCopy(int maxBufferSize) { return default(System.ServiceModel.Channels.MessageBuffer); }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, System.ServiceModel.Channels.MessageFault fault, string action) { return default(System.ServiceModel.Channels.Message); }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, System.ServiceModel.FaultCode code, string reason, object detail, string action) { return default(System.ServiceModel.Channels.Message); }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, System.ServiceModel.FaultCode code, string reason, string action) { return default(System.ServiceModel.Channels.Message); }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, string action) { return default(System.ServiceModel.Channels.Message); }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, string action, object body) { return default(System.ServiceModel.Channels.Message); }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, string action, object body, System.Runtime.Serialization.XmlObjectSerializer xmlFormatter) { return default(System.ServiceModel.Channels.Message); }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, string action, System.ServiceModel.Channels.BodyWriter body) { return default(System.ServiceModel.Channels.Message); }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, string action, System.Xml.XmlDictionaryReader body) { return default(System.ServiceModel.Channels.Message); }
        public static System.ServiceModel.Channels.Message CreateMessage(System.ServiceModel.Channels.MessageVersion version, string action, System.Xml.XmlReader body) { return default(System.ServiceModel.Channels.Message); }
        public static System.ServiceModel.Channels.Message CreateMessage(System.Xml.XmlDictionaryReader envelopeReader, int maxSizeOfHeaders, System.ServiceModel.Channels.MessageVersion version) { return default(System.ServiceModel.Channels.Message); }
        public static System.ServiceModel.Channels.Message CreateMessage(System.Xml.XmlReader envelopeReader, int maxSizeOfHeaders, System.ServiceModel.Channels.MessageVersion version) { return default(System.ServiceModel.Channels.Message); }
        public T GetBody<T>() { return default(T); }
        public T GetBody<T>(System.Runtime.Serialization.XmlObjectSerializer xmlFormatter) { return default(T); }
        public string GetBodyAttribute(string localName, string ns) { return default(string); }
        public System.Xml.XmlDictionaryReader GetReaderAtBodyContents() { return default(System.Xml.XmlDictionaryReader); }
        protected virtual void OnBodyToString(System.Xml.XmlDictionaryWriter writer) { }
        protected virtual void OnClose() { }
        protected virtual System.ServiceModel.Channels.MessageBuffer OnCreateBufferedCopy(int maxBufferSize) { return default(System.ServiceModel.Channels.MessageBuffer); }
        protected virtual T OnGetBody<T>(System.Xml.XmlDictionaryReader reader) { return default(T); }
        protected virtual string OnGetBodyAttribute(string localName, string ns) { return default(string); }
        protected virtual System.Xml.XmlDictionaryReader OnGetReaderAtBodyContents() { return default(System.Xml.XmlDictionaryReader); }
        protected abstract void OnWriteBodyContents(System.Xml.XmlDictionaryWriter writer);
        protected virtual void OnWriteMessage(System.Xml.XmlDictionaryWriter writer) { }
        protected virtual void OnWriteStartBody(System.Xml.XmlDictionaryWriter writer) { }
        protected virtual void OnWriteStartEnvelope(System.Xml.XmlDictionaryWriter writer) { }
        protected virtual void OnWriteStartHeaders(System.Xml.XmlDictionaryWriter writer) { }
        void System.IDisposable.Dispose() { }
        public override string ToString() { return default(string); }
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
        public virtual string MessageContentType { get { return default(string); } }
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
            public virtual T GetProperty<T>() where T : class { return default(T); }
        public virtual bool IsContentTypeSupported(string contentType) { return default(bool); }
        public System.ServiceModel.Channels.Message ReadMessage(System.ArraySegment<byte> buffer, System.ServiceModel.Channels.BufferManager bufferManager) { return default(System.ServiceModel.Channels.Message); }
        public abstract System.ServiceModel.Channels.Message ReadMessage(System.ArraySegment<byte> buffer, System.ServiceModel.Channels.BufferManager bufferManager, string contentType);
        public System.ServiceModel.Channels.Message ReadMessage(System.IO.Stream stream, int maxSizeOfHeaders) { return default(System.ServiceModel.Channels.Message); }
        public abstract System.ServiceModel.Channels.Message ReadMessage(System.IO.Stream stream, int maxSizeOfHeaders, string contentType);
        public override string ToString() { return default(string); }
        public System.ArraySegment<byte> WriteMessage(System.ServiceModel.Channels.Message message, int maxMessageSize, System.ServiceModel.Channels.BufferManager bufferManager) { return default(System.ArraySegment<byte>); }
        public abstract System.ArraySegment<byte> WriteMessage(System.ServiceModel.Channels.Message message, int maxMessageSize, System.ServiceModel.Channels.BufferManager bufferManager, int messageOffset);
        public abstract void WriteMessage(System.ServiceModel.Channels.Message message, System.IO.Stream stream);
    }
    public abstract partial class MessageEncoderFactory
    {
        protected MessageEncoderFactory() { }
        public abstract System.ServiceModel.Channels.MessageEncoder Encoder { get; }
        public abstract System.ServiceModel.Channels.MessageVersion MessageVersion { get; }
        public virtual System.ServiceModel.Channels.MessageEncoder CreateSessionEncoder() { return default(System.ServiceModel.Channels.MessageEncoder); }
    }
    public abstract partial class MessageEncodingBindingElement : System.ServiceModel.Channels.BindingElement
    {
        public MessageEncodingBindingElement() { }
            public MessageEncodingBindingElement(System.ServiceModel.Channels.MessageEncodingBindingElement source) { }
        public abstract System.ServiceModel.Channels.MessageVersion MessageVersion { get; set; }
        public abstract System.ServiceModel.Channels.MessageEncoderFactory CreateMessageEncoderFactory();
        public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext ctx) { return default(T); }
    }
    public abstract partial class MessageFault
    {
        protected MessageFault() { }
        public virtual string Actor { get { return default(string); } }
        public abstract System.ServiceModel.FaultCode Code { get; }
        public abstract bool HasDetail { get; }
        public virtual string Node { get { return default(string); } }
        public abstract System.ServiceModel.FaultReason Reason { get; }
        public static System.ServiceModel.Channels.MessageFault CreateFault(System.ServiceModel.Channels.Message message, int maxBufferSize) { return default(System.ServiceModel.Channels.MessageFault); }
        public static System.ServiceModel.Channels.MessageFault CreateFault(System.ServiceModel.FaultCode code, System.ServiceModel.FaultReason reason) { return default(System.ServiceModel.Channels.MessageFault); }
        public static System.ServiceModel.Channels.MessageFault CreateFault(System.ServiceModel.FaultCode code, System.ServiceModel.FaultReason reason, object detail) { return default(System.ServiceModel.Channels.MessageFault); }
        public static System.ServiceModel.Channels.MessageFault CreateFault(System.ServiceModel.FaultCode code, System.ServiceModel.FaultReason reason, object detail, System.Runtime.Serialization.XmlObjectSerializer formatter) { return default(System.ServiceModel.Channels.MessageFault); }
        public static System.ServiceModel.Channels.MessageFault CreateFault(System.ServiceModel.FaultCode code, System.ServiceModel.FaultReason reason, object detail, System.Runtime.Serialization.XmlObjectSerializer formatter, string actor) { return default(System.ServiceModel.Channels.MessageFault); }
        public static System.ServiceModel.Channels.MessageFault CreateFault(System.ServiceModel.FaultCode code, System.ServiceModel.FaultReason reason, object detail, System.Runtime.Serialization.XmlObjectSerializer formatter, string actor, string node) { return default(System.ServiceModel.Channels.MessageFault); }
        public static System.ServiceModel.Channels.MessageFault CreateFault(System.ServiceModel.FaultCode code, string reason) { return default(System.ServiceModel.Channels.MessageFault); }
        public T GetDetail<T>() { return default(T); }
        public T GetDetail<T>(System.Runtime.Serialization.XmlObjectSerializer formatter) { return default(T); }
        public System.Xml.XmlDictionaryReader GetReaderAtDetailContents() { return default(System.Xml.XmlDictionaryReader); }
        protected virtual System.Xml.XmlDictionaryReader OnGetReaderAtDetailContents() { return default(System.Xml.XmlDictionaryReader); }
        protected virtual void OnWriteDetail(System.Xml.XmlDictionaryWriter writer, System.ServiceModel.EnvelopeVersion version) { }
        protected abstract void OnWriteDetailContents(System.Xml.XmlDictionaryWriter writer);
        protected virtual void OnWriteStartDetail(System.Xml.XmlDictionaryWriter writer, System.ServiceModel.EnvelopeVersion version) { }
        public void WriteTo(System.Xml.XmlDictionaryWriter writer, System.ServiceModel.EnvelopeVersion version) { }
        public void WriteTo(System.Xml.XmlWriter writer, System.ServiceModel.EnvelopeVersion version) { }
    }
    public abstract partial class MessageHeader : System.ServiceModel.Channels.MessageHeaderInfo
    {
        protected MessageHeader() { }
        public override string Actor { get { return default(string); } }
        public override bool IsReferenceParameter { get { return default(bool); } }
        public override bool MustUnderstand { get { return default(bool); } }
        public override bool Relay { get { return default(bool); } }
        public static System.ServiceModel.Channels.MessageHeader CreateHeader(string name, string ns, object value) { return default(System.ServiceModel.Channels.MessageHeader); }
        public static System.ServiceModel.Channels.MessageHeader CreateHeader(string name, string ns, object value, bool must_understand) { return default(System.ServiceModel.Channels.MessageHeader); }
        public static System.ServiceModel.Channels.MessageHeader CreateHeader(string name, string ns, object value, bool must_understand, string actor) { return default(System.ServiceModel.Channels.MessageHeader); }
        public static System.ServiceModel.Channels.MessageHeader CreateHeader(string name, string ns, object value, bool must_understand, string actor, bool relay) { return default(System.ServiceModel.Channels.MessageHeader); }
        public static System.ServiceModel.Channels.MessageHeader CreateHeader(string name, string ns, object value, System.Runtime.Serialization.XmlObjectSerializer formatter) { return default(System.ServiceModel.Channels.MessageHeader); }
        public static System.ServiceModel.Channels.MessageHeader CreateHeader(string name, string ns, object value, System.Runtime.Serialization.XmlObjectSerializer formatter, bool must_understand) { return default(System.ServiceModel.Channels.MessageHeader); }
        public static System.ServiceModel.Channels.MessageHeader CreateHeader(string name, string ns, object value, System.Runtime.Serialization.XmlObjectSerializer formatter, bool must_understand, string actor) { return default(System.ServiceModel.Channels.MessageHeader); }
        public static System.ServiceModel.Channels.MessageHeader CreateHeader(string name, string ns, object value, System.Runtime.Serialization.XmlObjectSerializer formatter, bool must_understand, string actor, bool relay) { return default(System.ServiceModel.Channels.MessageHeader); }
        public virtual bool IsMessageVersionSupported(System.ServiceModel.Channels.MessageVersion version) { return default(bool); }
        protected abstract void OnWriteHeaderContents(System.Xml.XmlDictionaryWriter writer, System.ServiceModel.Channels.MessageVersion version);
        protected virtual void OnWriteStartHeader(System.Xml.XmlDictionaryWriter writer, System.ServiceModel.Channels.MessageVersion version) { }
        public override string ToString() { return default(string); }
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
        public string Action { get { return default(string); } set { } }
        public int Count { get { return default(int); } }
        public System.ServiceModel.EndpointAddress FaultTo { get { return default(System.ServiceModel.EndpointAddress); } set { } }
        public System.ServiceModel.EndpointAddress From { get { return default(System.ServiceModel.EndpointAddress); } set { } }
        public System.ServiceModel.Channels.MessageHeaderInfo this[int index] { get { return default(System.ServiceModel.Channels.MessageHeaderInfo); } }
        public System.Xml.UniqueId MessageId { get { return default(System.Xml.UniqueId); } set { } }
        public System.ServiceModel.Channels.MessageVersion MessageVersion { get { return default(System.ServiceModel.Channels.MessageVersion); } }
        public System.Xml.UniqueId RelatesTo { get { return default(System.Xml.UniqueId); } set { } }
        public System.ServiceModel.EndpointAddress ReplyTo { get { return default(System.ServiceModel.EndpointAddress); } set { } }
        public System.Uri To { get { return default(System.Uri); } set { } }
            public System.ServiceModel.Channels.UnderstoodHeaders UnderstoodHeaders { get { return default(System.ServiceModel.Channels.UnderstoodHeaders); } }
        public void Add(System.ServiceModel.Channels.MessageHeader header) { }
        public void Clear() { }
        public void CopyHeaderFrom(System.ServiceModel.Channels.Message m, int index) { }
        public void CopyHeaderFrom(System.ServiceModel.Channels.MessageHeaders headers, int index) { }
        public void CopyHeadersFrom(System.ServiceModel.Channels.Message m) { }
        public void CopyHeadersFrom(System.ServiceModel.Channels.MessageHeaders headers) { }
        public void CopyTo(System.ServiceModel.Channels.MessageHeaderInfo[] dst, int index) { }
        public int FindHeader(string name, string ns) { return default(int); }
        public int FindHeader(string name, string ns, params string[] actors) { return default(int); }
        public System.Collections.Generic.IEnumerator<System.ServiceModel.Channels.MessageHeaderInfo> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.ServiceModel.Channels.MessageHeaderInfo>); }
        public T GetHeader<T>(int index) { return default(T); }
        public T GetHeader<T>(int index, System.Runtime.Serialization.XmlObjectSerializer serializer) { return default(T); }
        public T GetHeader<T>(string name, string ns) { return default(T); }
        public T GetHeader<T>(string name, string ns, System.Runtime.Serialization.XmlObjectSerializer serializer) { return default(T); }
        public T GetHeader<T>(string name, string ns, params string[] actors) { return default(T); }
        public System.Xml.XmlDictionaryReader GetReaderAtHeader(int index) { return default(System.Xml.XmlDictionaryReader); }
        public bool HaveMandatoryHeadersBeenUnderstood() { return default(bool); }
        public bool HaveMandatoryHeadersBeenUnderstood(params string[] actors) { return default(bool); }
        public void Insert(int index, System.ServiceModel.Channels.MessageHeader header) { }
        public void RemoveAll(string name, string ns) { }
        public void RemoveAt(int index) { }
        public void SetAction(System.Xml.XmlDictionaryString action) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
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
        public bool AllowOutputBatching { get { return default(bool); } set { } }
        public int Count { get { return default(int); } }
        public System.ServiceModel.Channels.MessageEncoder Encoder { get { return default(System.ServiceModel.Channels.MessageEncoder); } set { } }
        public bool IsFixedSize { get { return default(bool); } }
        public bool IsReadOnly { get { return default(bool); } }
        public object this[string name] { get { return default(object); } set { } }
        public System.Collections.Generic.ICollection<string> Keys { get { return default(System.Collections.Generic.ICollection<string>); } }
        public System.Collections.Generic.ICollection<object> Values { get { return default(System.Collections.Generic.ICollection<object>); } }
        public System.Uri Via { get { return default(System.Uri); } set { } }
        public void Add(string name, object property) { }
        public void Clear() { }
        public bool ContainsKey(string name) { return default(bool); }
        public void CopyProperties(System.ServiceModel.Channels.MessageProperties properties) { }
        public void Dispose() { }
        public bool Remove(string name) { return default(bool); }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string,object>>.Add(System.Collections.Generic.KeyValuePair<string, object> pair) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string,object>>.Contains(System.Collections.Generic.KeyValuePair<string, object> pair) { return default(bool); }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string,object>>.CopyTo(System.Collections.Generic.KeyValuePair<string, object>[] array, int index) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string,object>>.Remove(System.Collections.Generic.KeyValuePair<string, object> pair) { return default(bool); }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string,object>>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>>); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public bool TryGetValue(string name, out object value) { value = default(object); return default(bool); }
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
        public System.ServiceModel.Channels.AddressingVersion Addressing { get { return default(System.ServiceModel.Channels.AddressingVersion); } }
        public static System.ServiceModel.Channels.MessageVersion Default { get { return default(System.ServiceModel.Channels.MessageVersion); } }
        public System.ServiceModel.EnvelopeVersion Envelope { get { return default(System.ServiceModel.EnvelopeVersion); } }
        public static System.ServiceModel.Channels.MessageVersion None { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.Channels.MessageVersion); } }
        public static System.ServiceModel.Channels.MessageVersion Soap11 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.Channels.MessageVersion); } }
        public static System.ServiceModel.Channels.MessageVersion Soap11WSAddressing10 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.Channels.MessageVersion); } }
        public static System.ServiceModel.Channels.MessageVersion Soap11WSAddressingAugust2004 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.Channels.MessageVersion); } }
        public static System.ServiceModel.Channels.MessageVersion Soap12 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.Channels.MessageVersion); } }
        public static System.ServiceModel.Channels.MessageVersion Soap12WSAddressing10 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.Channels.MessageVersion); } }
        public static System.ServiceModel.Channels.MessageVersion Soap12WSAddressingAugust2004 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.Channels.MessageVersion); } }
        public static System.ServiceModel.Channels.MessageVersion CreateVersion(System.ServiceModel.EnvelopeVersion envelope_version) { return default(System.ServiceModel.Channels.MessageVersion); }
        public static System.ServiceModel.Channels.MessageVersion CreateVersion(System.ServiceModel.EnvelopeVersion envelope_version, System.ServiceModel.Channels.AddressingVersion addressing_version) { return default(System.ServiceModel.Channels.MessageVersion); }
        public override bool Equals(object value) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public override string ToString() { return default(string); }
    }
    public partial class PrivacyNoticeBindingElementImporter
    {
        public PrivacyNoticeBindingElementImporter() { }
    }
    public sealed partial class ReliableSessionBindingElement : System.ServiceModel.Channels.BindingElement
    {
        public ReliableSessionBindingElement() { }
        public ReliableSessionBindingElement(bool ordered) { }
            public System.TimeSpan AcknowledgementInterval { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.TimeSpan); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public bool FlowControlEnabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public System.TimeSpan InactivityTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.TimeSpan); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public int MaxPendingChannels { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public int MaxRetryCount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public int MaxTransferWindowSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public bool Ordered { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public System.ServiceModel.ReliableMessagingVersion ReliableMessagingVersion { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.ReliableMessagingVersion); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public override System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { return default(System.ServiceModel.Channels.IChannelFactory<TChannel>); }
            public override bool CanBuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { return default(bool); }
        public override System.ServiceModel.Channels.BindingElement Clone() { return default(System.ServiceModel.Channels.BindingElement); }
        public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) { return default(T); }
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
        public bool IncludeTimestamp { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.ServiceModel.Channels.LocalClientSecuritySettings LocalClientSettings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.Channels.LocalClientSecuritySettings); } }
        public override System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { return default(System.ServiceModel.Channels.IChannelFactory<TChannel>); }
        protected abstract System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactoryCore<TChannel>(System.ServiceModel.Channels.BindingContext context);
        public override bool CanBuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { return default(bool); }
            public static System.ServiceModel.Channels.TransportSecurityBindingElement CreateUserNameOverTransportBindingElement() { return default(System.ServiceModel.Channels.TransportSecurityBindingElement); }
            public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) { return default(T); }
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
        public int MaxReadPoolSize { get { return default(int); } set { } }
        public int MaxWritePoolSize { get { return default(int); } set { } }
        public override System.ServiceModel.Channels.MessageVersion MessageVersion { get { return default(System.ServiceModel.Channels.MessageVersion); } set { } }
        public System.Xml.XmlDictionaryReaderQuotas ReaderQuotas { get { return default(System.Xml.XmlDictionaryReaderQuotas); } set { } }
        public System.Text.Encoding WriteEncoding { get { return default(System.Text.Encoding); } set { } }
        public override System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactory<TChannel>(System.ServiceModel.Channels.BindingContext context) { return default(System.ServiceModel.Channels.IChannelFactory<TChannel>); }
        public override System.ServiceModel.Channels.BindingElement Clone() { return default(System.ServiceModel.Channels.BindingElement); }
        public override System.ServiceModel.Channels.MessageEncoderFactory CreateMessageEncoderFactory() { return default(System.ServiceModel.Channels.MessageEncoderFactory); }
            public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) { return default(T); }
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
        public bool ManualAddressing { get { return default(bool); } set { } }
        public virtual long MaxBufferPoolSize { get { return default(long); } set { } }
        public virtual long MaxReceivedMessageSize { get { return default(long); } set { } }
        public abstract string Scheme { get; }
        public override T GetProperty<T>(System.ServiceModel.Channels.BindingContext context) { return default(T); }
    }
    public sealed partial class TransportSecurityBindingElement : System.ServiceModel.Channels.SecurityBindingElement
    {
        public TransportSecurityBindingElement() { }
            protected override System.ServiceModel.Channels.IChannelFactory<TChannel> BuildChannelFactoryCore<TChannel>(System.ServiceModel.Channels.BindingContext context) { return default(System.ServiceModel.Channels.IChannelFactory<TChannel>); }
        public override System.ServiceModel.Channels.BindingElement Clone() { return default(System.ServiceModel.Channels.BindingElement); }
    }
    public sealed partial class UnderstoodHeaders : System.Collections.Generic.IEnumerable<System.ServiceModel.Channels.MessageHeaderInfo>, System.Collections.IEnumerable
    {
        internal UnderstoodHeaders() { }
        public void Add(System.ServiceModel.Channels.MessageHeaderInfo header) { }
        public bool Contains(System.ServiceModel.Channels.MessageHeaderInfo header) { return default(bool); }
        public System.Collections.Generic.IEnumerator<System.ServiceModel.Channels.MessageHeaderInfo> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.ServiceModel.Channels.MessageHeaderInfo>); }
        public void Remove(System.ServiceModel.Channels.MessageHeaderInfo header) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public static partial class UrlUtility
    {
        public static string UrlDecode(string s, System.Text.Encoding e) { return default(string); }
        public static string UrlEncode(string s, System.Text.Encoding e) { return default(string); }
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
        public bool CreateNotificationOnConnection { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool DisablePayloadMasking { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan KeepAliveInterval { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.TimeSpan); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int MaxPendingConnections { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int ReceiveBufferSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int SendBufferSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SubProtocol { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.ServiceModel.Channels.WebSocketTransportUsage TransportUsage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.Channels.WebSocketTransportUsage); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Equals(System.ServiceModel.Channels.WebSocketTransportSettings other) { return default(bool); }
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
        public System.ServiceModel.Security.UserNamePasswordClientCredential UserName { get { return default(System.ServiceModel.Security.UserNamePasswordClientCredential); } }
            public virtual void ApplyClientBehavior(System.ServiceModel.Description.ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.ClientRuntime behavior) { }
        public System.ServiceModel.Description.ClientCredentials Clone() { return default(System.ServiceModel.Description.ClientCredentials); }
        protected virtual System.ServiceModel.Description.ClientCredentials CloneCore() { return default(System.ServiceModel.Description.ClientCredentials); }
        void System.ServiceModel.Description.IEndpointBehavior.AddBindingParameters(System.ServiceModel.Description.ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection parameters) { }
        void System.ServiceModel.Description.IEndpointBehavior.ApplyDispatchBehavior(System.ServiceModel.Description.ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.EndpointDispatcher dispatcher) { }
        void System.ServiceModel.Description.IEndpointBehavior.Validate(System.ServiceModel.Description.ServiceEndpoint endpoint) { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Name={name}, Namespace={ns}, ContractType={contractType}")]
    public partial class ContractDescription
    {
        public ContractDescription(string name) { }
        public ContractDescription(string name, string ns) { }
        public System.Collections.Generic.KeyedByTypeCollection<System.ServiceModel.Description.IContractBehavior> Behaviors { get { return default(System.Collections.Generic.KeyedByTypeCollection<System.ServiceModel.Description.IContractBehavior>); } }
        public System.Type CallbackContractType { get { return default(System.Type); } set { } }
        public string ConfigurationName { get { return default(string); } set { } }
            public System.Collections.ObjectModel.KeyedCollection<System.Type, System.ServiceModel.Description.IContractBehavior> ContractBehaviors { get { return default(System.Collections.ObjectModel.KeyedCollection<System.Type, System.ServiceModel.Description.IContractBehavior>); } }
        public System.Type ContractType { get { return default(System.Type); } set { } }
        public bool HasProtectionLevel { get { return default(bool); } }
        public string Name { get { return default(string); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public System.ServiceModel.Description.OperationDescriptionCollection Operations { get { return default(System.ServiceModel.Description.OperationDescriptionCollection); } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { return default(System.Net.Security.ProtectionLevel); } set { } }
        public System.ServiceModel.SessionMode SessionMode { get { return default(System.ServiceModel.SessionMode); } set { } }
        public static System.ServiceModel.Description.ContractDescription GetContract(System.Type contractType) { return default(System.ServiceModel.Description.ContractDescription); }
        public static System.ServiceModel.Description.ContractDescription GetContract(System.Type contractType, object serviceImplementation) { return default(System.ServiceModel.Description.ContractDescription); }
        public static System.ServiceModel.Description.ContractDescription GetContract(System.Type contractType, System.Type serviceType) { return default(System.ServiceModel.Description.ContractDescription); }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Description.ContractDescription> GetInheritedContracts() { return default(System.Collections.ObjectModel.Collection<System.ServiceModel.Description.ContractDescription>); }
    }
    public partial class DataContractSerializerOperationBehavior : System.ServiceModel.Description.IOperationBehavior, System.ServiceModel.Description.IWsdlExportExtension
    {
        public DataContractSerializerOperationBehavior(System.ServiceModel.Description.OperationDescription operation) { }
        public DataContractSerializerOperationBehavior(System.ServiceModel.Description.OperationDescription operation, System.ServiceModel.DataContractFormatAttribute dataContractFormatAttribute) { }
        public System.ServiceModel.DataContractFormatAttribute DataContractFormatAttribute { get { return default(System.ServiceModel.DataContractFormatAttribute); } }
        public System.Runtime.Serialization.DataContractResolver DataContractResolver { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Runtime.Serialization.DataContractResolver); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IgnoreExtensionDataObject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int MaxItemsInObjectGraph { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Runtime.Serialization.XmlObjectSerializer CreateSerializer(System.Type type, string name, string ns, System.Collections.Generic.IList<System.Type> knownTypes) { return default(System.Runtime.Serialization.XmlObjectSerializer); }
        public virtual System.Runtime.Serialization.XmlObjectSerializer CreateSerializer(System.Type type, System.Xml.XmlDictionaryString name, System.Xml.XmlDictionaryString ns, System.Collections.Generic.IList<System.Type> knownTypes) { return default(System.Runtime.Serialization.XmlObjectSerializer); }
        void System.ServiceModel.Description.IOperationBehavior.AddBindingParameters(System.ServiceModel.Description.OperationDescription description, System.ServiceModel.Channels.BindingParameterCollection parameters) { }
        void System.ServiceModel.Description.IOperationBehavior.ApplyClientBehavior(System.ServiceModel.Description.OperationDescription description, System.ServiceModel.Dispatcher.ClientOperation proxy) { }
        void System.ServiceModel.Description.IOperationBehavior.ApplyDispatchBehavior(System.ServiceModel.Description.OperationDescription description, System.ServiceModel.Dispatcher.DispatchOperation dispatch) { }
        void System.ServiceModel.Description.IOperationBehavior.Validate(System.ServiceModel.Description.OperationDescription description) { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Name={name}, Action={action}, DetailType={detailType}")]
    public partial class FaultDescription
    {
        public FaultDescription(string action) { }
        public string Action { get { return default(string); } }
        public System.Type DetailType { get { return default(System.Type); } set { } }
        public bool HasProtectionLevel { get { return default(bool); } }
        public string Name { get { return default(string); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { return default(System.Net.Security.ProtectionLevel); } set { } }
    }
    public partial class FaultDescriptionCollection : System.Collections.ObjectModel.Collection<System.ServiceModel.Description.FaultDescription>
    {
        internal FaultDescriptionCollection() { }
        public System.ServiceModel.Description.FaultDescription Find(string name) { return default(System.ServiceModel.Description.FaultDescription); }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Description.FaultDescription> FindAll(string name) { return default(System.Collections.ObjectModel.Collection<System.ServiceModel.Description.FaultDescription>); }
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
        public System.ServiceModel.Description.MessagePartDescriptionCollection Parts { get { return default(System.ServiceModel.Description.MessagePartDescriptionCollection); } }
        public System.ServiceModel.Description.MessagePartDescription ReturnValue { get { return default(System.ServiceModel.Description.MessagePartDescription); } set { } }
        public string WrapperName { get { return default(string); } set { } }
        public string WrapperNamespace { get { return default(string); } set { } }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Action={action}, Direction={direction}, MessageType={messageType}")]
    public partial class MessageDescription
    {
        public MessageDescription(string action, System.ServiceModel.Description.MessageDirection direction) { }
        public string Action { get { return default(string); } }
        public System.ServiceModel.Description.MessageBodyDescription Body { get { return default(System.ServiceModel.Description.MessageBodyDescription); } }
        public System.ServiceModel.Description.MessageDirection Direction { get { return default(System.ServiceModel.Description.MessageDirection); } }
        public bool HasProtectionLevel { get { return default(bool); } }
        public System.ServiceModel.Description.MessageHeaderDescriptionCollection Headers { get { return default(System.ServiceModel.Description.MessageHeaderDescriptionCollection); } }
        public System.Type MessageType { get { return default(System.Type); } set { } }
        public System.ServiceModel.Description.MessagePropertyDescriptionCollection Properties { get { return default(System.ServiceModel.Description.MessagePropertyDescriptionCollection); } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { return default(System.Net.Security.ProtectionLevel); } set { } }
    }
    public partial class MessageDescriptionCollection : System.Collections.ObjectModel.Collection<System.ServiceModel.Description.MessageDescription>
    {
        internal MessageDescriptionCollection() { }
        public System.ServiceModel.Description.MessageDescription Find(string action) { return default(System.ServiceModel.Description.MessageDescription); }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Description.MessageDescription> FindAll(string action) { return default(System.Collections.ObjectModel.Collection<System.ServiceModel.Description.MessageDescription>); }
    }
    public enum MessageDirection
    {
        Input = 0,
        Output = 1,
    }
    public partial class MessageHeaderDescription : System.ServiceModel.Description.MessagePartDescription
    {
        public MessageHeaderDescription(string name, string ns) : base (default(string), default(string)) { }
        public string Actor { get { return default(string); } set { } }
        public bool MustUnderstand { get { return default(bool); } set { } }
        public bool Relay { get { return default(bool); } set { } }
        public bool TypedHeader { get { return default(bool); } set { } }
    }
    public partial class MessageHeaderDescriptionCollection : System.Collections.ObjectModel.KeyedCollection<System.Xml.XmlQualifiedName, System.ServiceModel.Description.MessageHeaderDescription>
    {
        internal MessageHeaderDescriptionCollection() { }
        protected override System.Xml.XmlQualifiedName GetKeyForItem(System.ServiceModel.Description.MessageHeaderDescription item) { return default(System.Xml.XmlQualifiedName); }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Name={name}, Namespace={ns}, Type={Type}, Index={index}}")]
    public partial class MessagePartDescription
    {
        public MessagePartDescription(string name, string ns) { }
        public bool HasProtectionLevel { get { return default(bool); } }
        public int Index { get { return default(int); } set { } }
        public System.Reflection.MemberInfo MemberInfo { get { return default(System.Reflection.MemberInfo); } set { } }
        public bool Multiple { get { return default(bool); } set { } }
        public string Name { get { return default(string); } }
        public string Namespace { get { return default(string); } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { return default(System.Net.Security.ProtectionLevel); } set { } }
        public System.Type Type { get { return default(System.Type); } set { } }
    }
    public partial class MessagePartDescriptionCollection : System.Collections.ObjectModel.KeyedCollection<System.Xml.XmlQualifiedName, System.ServiceModel.Description.MessagePartDescription>
    {
        internal MessagePartDescriptionCollection() { }
        protected override System.Xml.XmlQualifiedName GetKeyForItem(System.ServiceModel.Description.MessagePartDescription item) { return default(System.Xml.XmlQualifiedName); }
    }
    public partial class MessagePropertyDescription : System.ServiceModel.Description.MessagePartDescription
    {
        public MessagePropertyDescription(string name) : base (default(string), default(string)) { }
    }
    public partial class MessagePropertyDescriptionCollection : System.Collections.ObjectModel.KeyedCollection<string, System.ServiceModel.Description.MessagePropertyDescription>
    {
        internal MessagePropertyDescriptionCollection() { }
        protected override string GetKeyForItem(System.ServiceModel.Description.MessagePropertyDescription item) { return default(string); }
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
        public System.Reflection.MethodInfo BeginMethod { get { return default(System.Reflection.MethodInfo); } set { } }
        public System.Collections.Generic.KeyedByTypeCollection<System.ServiceModel.Description.IOperationBehavior> Behaviors { get { return default(System.Collections.Generic.KeyedByTypeCollection<System.ServiceModel.Description.IOperationBehavior>); } }
        public System.ServiceModel.Description.ContractDescription DeclaringContract { get { return default(System.ServiceModel.Description.ContractDescription); } set { } }
        public System.Reflection.MethodInfo EndMethod { get { return default(System.Reflection.MethodInfo); } set { } }
        public System.ServiceModel.Description.FaultDescriptionCollection Faults { get { return default(System.ServiceModel.Description.FaultDescriptionCollection); } }
        public bool HasProtectionLevel { get { return default(bool); } }
        public bool IsInitiating { get { return default(bool); } set { } }
        public bool IsOneWay { get { return default(bool); } }
        public bool IsTerminating { get { return default(bool); } set { } }
        public System.Collections.ObjectModel.Collection<System.Type> KnownTypes { get { return default(System.Collections.ObjectModel.Collection<System.Type>); } }
        public System.ServiceModel.Description.MessageDescriptionCollection Messages { get { return default(System.ServiceModel.Description.MessageDescriptionCollection); } }
        public string Name { get { return default(string); } }
            public System.Collections.ObjectModel.KeyedCollection<System.Type, System.ServiceModel.Description.IOperationBehavior> OperationBehaviors { get { return default(System.Collections.ObjectModel.KeyedCollection<System.Type, System.ServiceModel.Description.IOperationBehavior>); } }
        public System.Net.Security.ProtectionLevel ProtectionLevel { get { return default(System.Net.Security.ProtectionLevel); } set { } }
        public System.Reflection.MethodInfo SyncMethod { get { return default(System.Reflection.MethodInfo); } set { } }
            public System.Reflection.MethodInfo TaskMethod { get { return default(System.Reflection.MethodInfo); } set { } }
    }
    public partial class OperationDescriptionCollection : System.Collections.ObjectModel.Collection<System.ServiceModel.Description.OperationDescription>
    {
        internal OperationDescriptionCollection() { }
        public System.ServiceModel.Description.OperationDescription Find(string name) { return default(System.ServiceModel.Description.OperationDescription); }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Description.OperationDescription> FindAll(string name) { return default(System.Collections.ObjectModel.Collection<System.ServiceModel.Description.OperationDescription>); }
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
        public System.ServiceModel.EndpointAddress Address { get { return default(System.ServiceModel.EndpointAddress); } set { } }
        public System.Collections.Generic.KeyedByTypeCollection<System.ServiceModel.Description.IEndpointBehavior> Behaviors { get { return default(System.Collections.Generic.KeyedByTypeCollection<System.ServiceModel.Description.IEndpointBehavior>); } }
        public System.ServiceModel.Channels.Binding Binding { get { return default(System.ServiceModel.Channels.Binding); } set { } }
        public System.ServiceModel.Description.ContractDescription Contract { get { return default(System.ServiceModel.Description.ContractDescription); } set { } }
            public System.Collections.ObjectModel.KeyedCollection<System.Type, System.ServiceModel.Description.IEndpointBehavior> EndpointBehaviors { get { return default(System.Collections.ObjectModel.KeyedCollection<System.Type, System.ServiceModel.Description.IEndpointBehavior>); } }
        public bool IsSystemEndpoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Uri ListenUri { get { return default(System.Uri); } set { } }
        public System.ServiceModel.Description.ListenUriMode ListenUriMode { get { return default(System.ServiceModel.Description.ListenUriMode); } set { } }
        public string Name { get { return default(string); } set { } }
    }
    public partial class XmlSerializerOperationBehavior : System.ServiceModel.Description.IOperationBehavior
    {
        public XmlSerializerOperationBehavior(System.ServiceModel.Description.OperationDescription operation) { }
        public XmlSerializerOperationBehavior(System.ServiceModel.Description.OperationDescription operation, System.ServiceModel.XmlSerializerFormatAttribute format) { }
        public System.ServiceModel.XmlSerializerFormatAttribute XmlSerializerFormatAttribute { get { return default(System.ServiceModel.XmlSerializerFormatAttribute); } }
            public System.Collections.ObjectModel.Collection<System.Xml.Serialization.XmlMapping> GetXmlMappings() { return default(System.Collections.ObjectModel.Collection<System.Xml.Serialization.XmlMapping>); }
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
        public string Action { get { return default(string); } }
        public System.Reflection.MethodInfo BeginMethod { get { return default(System.Reflection.MethodInfo); } set { } }
            public System.Collections.Generic.ICollection<System.ServiceModel.Dispatcher.IParameterInspector> ClientParameterInspectors { get { return default(System.Collections.Generic.ICollection<System.ServiceModel.Dispatcher.IParameterInspector>); } }
        public bool DeserializeReply { get { return default(bool); } set { } }
        public System.Reflection.MethodInfo EndMethod { get { return default(System.Reflection.MethodInfo); } set { } }
        public System.Collections.Generic.SynchronizedCollection<System.ServiceModel.Dispatcher.FaultContractInfo> FaultContractInfos { get { return default(System.Collections.Generic.SynchronizedCollection<System.ServiceModel.Dispatcher.FaultContractInfo>); } }
        public System.ServiceModel.Dispatcher.IClientMessageFormatter Formatter { get { return default(System.ServiceModel.Dispatcher.IClientMessageFormatter); } set { } }
        public bool IsInitiating { get { return default(bool); } set { } }
        public bool IsOneWay { get { return default(bool); } set { } }
        public bool IsTerminating { get { return default(bool); } set { } }
        public string Name { get { return default(string); } }
        public System.Collections.Generic.SynchronizedCollection<System.ServiceModel.Dispatcher.IParameterInspector> ParameterInspectors { get { return default(System.Collections.Generic.SynchronizedCollection<System.ServiceModel.Dispatcher.IParameterInspector>); } }
        public System.ServiceModel.Dispatcher.ClientRuntime Parent { get { return default(System.ServiceModel.Dispatcher.ClientRuntime); } }
        public string ReplyAction { get { return default(string); } }
        public bool SerializeRequest { get { return default(bool); } set { } }
        public System.Reflection.MethodInfo SyncMethod { get { return default(System.Reflection.MethodInfo); } set { } }
            public System.Reflection.MethodInfo TaskMethod { get { return default(System.Reflection.MethodInfo); } set { } }
            public System.Type TaskTResult { get { return default(System.Type); } set { } }
    }
    public sealed partial class ClientRuntime
    {
        internal ClientRuntime() { }
        public System.Type CallbackClientType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Type); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.SynchronizedCollection<System.ServiceModel.Dispatcher.IChannelInitializer> ChannelInitializers { get { return default(System.Collections.Generic.SynchronizedCollection<System.ServiceModel.Dispatcher.IChannelInitializer>); } }
            public System.Collections.Generic.ICollection<System.ServiceModel.Dispatcher.IClientMessageInspector> ClientMessageInspectors { get { return default(System.Collections.Generic.ICollection<System.ServiceModel.Dispatcher.IClientMessageInspector>); } }
            public System.Collections.Generic.ICollection<System.ServiceModel.Dispatcher.ClientOperation> ClientOperations { get { return default(System.Collections.Generic.ICollection<System.ServiceModel.Dispatcher.ClientOperation>); } }
        public System.Type ContractClientType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Type); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ContractName { get { return default(string); } }
        public string ContractNamespace { get { return default(string); } }
        public System.Collections.Generic.SynchronizedCollection<System.ServiceModel.Dispatcher.IInteractiveChannelInitializer> InteractiveChannelInitializers { get { return default(System.Collections.Generic.SynchronizedCollection<System.ServiceModel.Dispatcher.IInteractiveChannelInitializer>); } }
        public bool ManualAddressing { get { return default(bool); } set { } }
        public int MaxFaultSize { get { return default(int); } set { } }
        public System.Collections.Generic.SynchronizedCollection<System.ServiceModel.Dispatcher.IClientMessageInspector> MessageInspectors { get { return default(System.Collections.Generic.SynchronizedCollection<System.ServiceModel.Dispatcher.IClientMessageInspector>); } }
            public bool MessageVersionNoneFaultsEnabled { get { return default(bool); } set { } }
        public System.Collections.ObjectModel.KeyedCollection<string, System.ServiceModel.Dispatcher.ClientOperation> Operations { get { return default(System.Collections.ObjectModel.KeyedCollection<string, System.ServiceModel.Dispatcher.ClientOperation>); } }
        public System.ServiceModel.Dispatcher.IClientOperationSelector OperationSelector { get { return default(System.ServiceModel.Dispatcher.IClientOperationSelector); } set { } }
            public System.ServiceModel.Dispatcher.ClientOperation UnhandledClientOperation { get { return default(System.ServiceModel.Dispatcher.ClientOperation); } }
        public bool ValidateMustUnderstand { get { return default(bool); } set { } }
        public System.Uri Via { get { return default(System.Uri); } set { } }
    }
    public sealed partial class DispatchOperation
    {
        public DispatchOperation(System.ServiceModel.Dispatcher.DispatchRuntime parent, string name, string action) { }
        public DispatchOperation(System.ServiceModel.Dispatcher.DispatchRuntime parent, string name, string action, string replyAction) { }
        public string Action { get { return default(string); } }
        public bool AutoDisposeParameters { get { return default(bool); } set { } }
        public bool DeserializeRequest { get { return default(bool); } set { } }
        public System.ServiceModel.Dispatcher.IDispatchMessageFormatter Formatter { get { return default(System.ServiceModel.Dispatcher.IDispatchMessageFormatter); } set { } }
        public bool IsOneWay { get { return default(bool); } }
        public string Name { get { return default(string); } }
        public System.ServiceModel.Dispatcher.DispatchRuntime Parent { get { return default(System.ServiceModel.Dispatcher.DispatchRuntime); } }
        public bool SerializeReply { get { return default(bool); } set { } }
    }
    public sealed partial class DispatchRuntime
    {
        internal DispatchRuntime() { }
        public System.ServiceModel.Dispatcher.DispatchOperation UnhandledDispatchOperation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.ServiceModel.Dispatcher.DispatchOperation); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public sealed partial class EndpointDispatcher
    {
        internal EndpointDispatcher() { }
    }
    public partial class FaultContractInfo
    {
        public FaultContractInfo(string action, System.Type detail) { }
        public string Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
        public System.Type Detail { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Type); } }
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
        public System.Security.Principal.TokenImpersonationLevel AllowedImpersonationLevel { get { return default(System.Security.Principal.TokenImpersonationLevel); } set { } }
        public System.Net.NetworkCredential ClientCredential { get { return default(System.Net.NetworkCredential); } set { } }
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
        public string Password { get { return default(string); } set { } }
        public string UserName { get { return default(string); } set { } }
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
        public System.Security.Principal.TokenImpersonationLevel AllowedImpersonationLevel { get { return default(System.Security.Principal.TokenImpersonationLevel); } set { } }
        public bool AllowNtlm { get { return default(bool); } set { } }
            public System.Net.NetworkCredential ClientCredential { get { return default(System.Net.NetworkCredential); } set { } }
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

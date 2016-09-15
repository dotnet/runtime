// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.Services.Client
{
    public sealed partial class ChangeOperationResponse : System.Data.Services.Client.OperationResponse
    {
        internal ChangeOperationResponse() { }
        public System.Data.Services.Client.Descriptor Descriptor { get { return default(System.Data.Services.Client.Descriptor); } }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{Message}")]
    public sealed partial class DataServiceClientException : System.InvalidOperationException
    {
        public DataServiceClientException() { }
        /*
         * Had to comment this out since csc.exe would throw an error saying that protected members can't exist in sealed classes.
        protected DataServiceClientException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext context) { }
        */
        public DataServiceClientException(string message) { }
        public DataServiceClientException(string message, System.Exception innerException) { }
        public DataServiceClientException(string message, System.Exception innerException, int statusCode) { }
        public DataServiceClientException(string message, int statusCode) { }
        public int StatusCode { get { return default(int); } }
        [System.Security.SecurityCriticalAttribute]
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class DataServiceCollection<T> : System.Collections.ObjectModel.ObservableCollection<T>
    {
        public DataServiceCollection() { }
        public DataServiceCollection(System.Collections.Generic.IEnumerable<T> items) { }
        public DataServiceCollection(System.Collections.Generic.IEnumerable<T> items, System.Data.Services.Client.TrackingMode trackingMode) { }
        public DataServiceCollection(System.Collections.Generic.IEnumerable<T> items, System.Data.Services.Client.TrackingMode trackingMode, string entitySetName, System.Func<System.Data.Services.Client.EntityChangedParams, bool> entityChangedCallback, System.Func<System.Data.Services.Client.EntityCollectionChangedParams, bool> collectionChangedCallback) { }
        public DataServiceCollection(System.Data.Services.Client.DataServiceContext context) { }
        public DataServiceCollection(System.Data.Services.Client.DataServiceContext context, System.Collections.Generic.IEnumerable<T> items, System.Data.Services.Client.TrackingMode trackingMode, string entitySetName, System.Func<System.Data.Services.Client.EntityChangedParams, bool> entityChangedCallback, System.Func<System.Data.Services.Client.EntityCollectionChangedParams, bool> collectionChangedCallback) { }
        public DataServiceCollection(System.Data.Services.Client.DataServiceContext context, string entitySetName, System.Func<System.Data.Services.Client.EntityChangedParams, bool> entityChangedCallback, System.Func<System.Data.Services.Client.EntityCollectionChangedParams, bool> collectionChangedCallback) { }
        public System.Data.Services.Client.DataServiceQueryContinuation<T> Continuation { get { return default(System.Data.Services.Client.DataServiceQueryContinuation<T>); } set { } }
        public void Clear(bool stopTracking) { }
        public void Detach() { }
        protected override void InsertItem(int index, T item) { }
        public void Load(T item) { }
        public void Load(System.Collections.Generic.IEnumerable<T> items) { }
    }
    public partial class DataServiceContext
    {
        public DataServiceContext(System.Uri serviceRoot) { }
        public bool ApplyingChanges { get { return default(bool); } }
        public System.Uri BaseUri { get { return default(System.Uri); } }
        public System.Net.ICredentials Credentials { get { return default(System.Net.ICredentials); } set { } }
        public string DataNamespace { get { return default(string); } set { } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Data.Services.Client.EntityDescriptor> Entities { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Data.Services.Client.EntityDescriptor>); } }
        public bool IgnoreMissingProperties { get { return default(bool); } set { } }
        public bool IgnoreResourceNotFoundException { get { return default(bool); } set { } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Data.Services.Client.LinkDescriptor> Links { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Data.Services.Client.LinkDescriptor>); } }
        public System.Data.Services.Client.MergeOption MergeOption { get { return default(System.Data.Services.Client.MergeOption); } set { } }
        public System.Func<System.Type, string> ResolveName { get { return default(System.Func<System.Type, string>); } set { } }
        public System.Func<string, System.Type> ResolveType { get { return default(System.Func<string, System.Type>); } set { } }
        public System.Data.Services.Client.SaveChangesOptions SaveChangesDefaultOptions { get { return default(System.Data.Services.Client.SaveChangesOptions); } set { } }
        public int Timeout { get { return default(int); } set { } }
        public System.Uri TypeScheme { get { return default(System.Uri); } set { } }
        public bool UsePostTunneling { get { return default(bool); } set { } }
        public event System.EventHandler<System.Data.Services.Client.ReadingWritingEntityEventArgs> ReadingEntity { add { } remove { } }
        public event System.EventHandler<System.Data.Services.Client.SendingRequestEventArgs> SendingRequest { add { } remove { } }
        public event System.EventHandler<System.Data.Services.Client.ReadingWritingEntityEventArgs> WritingEntity { add { } remove { } }
        public void AddLink(object source, string sourceProperty, object target) { }
        public void AddObject(string entitySetName, object entity) { }
        public void AddRelatedObject(object source, string sourceProperty, object target) { }
        public void AttachLink(object source, string sourceProperty, object target) { }
        public void AttachTo(string entitySetName, object entity) { }
        public void AttachTo(string entitySetName, object entity, string etag) { }
        public System.IAsyncResult BeginExecute<T>(System.Data.Services.Client.DataServiceQueryContinuation<T> continuation, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public System.IAsyncResult BeginExecute<TElement>(System.Uri requestUri, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public System.IAsyncResult BeginExecuteBatch(System.AsyncCallback callback, object state, params System.Data.Services.Client.DataServiceRequest[] queries) { return default(System.IAsyncResult); }
        public System.IAsyncResult BeginGetReadStream(object entity, System.Data.Services.Client.DataServiceRequestArgs args, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public System.IAsyncResult BeginLoadProperty(object entity, string propertyName, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public System.IAsyncResult BeginLoadProperty(object entity, string propertyName, System.Data.Services.Client.DataServiceQueryContinuation continuation, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public System.IAsyncResult BeginLoadProperty(object entity, string propertyName, System.Uri nextLinkUri, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public System.IAsyncResult BeginSaveChanges(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public System.IAsyncResult BeginSaveChanges(System.Data.Services.Client.SaveChangesOptions options, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public void CancelRequest(System.IAsyncResult asyncResult) { }
        public System.Data.Services.Client.DataServiceQuery<T> CreateQuery<T>(string entitySetName) { return default(System.Data.Services.Client.DataServiceQuery<T>); }
        public void DeleteLink(object source, string sourceProperty, object target) { }
        public void DeleteObject(object entity) { }
        public bool Detach(object entity) { return default(bool); }
        public bool DetachLink(object source, string sourceProperty, object target) { return default(bool); }
        public System.Collections.Generic.IEnumerable<TElement> EndExecute<TElement>(System.IAsyncResult asyncResult) { return default(System.Collections.Generic.IEnumerable<TElement>); }
        public System.Data.Services.Client.DataServiceResponse EndExecuteBatch(System.IAsyncResult asyncResult) { return default(System.Data.Services.Client.DataServiceResponse); }
        public System.Data.Services.Client.DataServiceStreamResponse EndGetReadStream(System.IAsyncResult asyncResult) { return default(System.Data.Services.Client.DataServiceStreamResponse); }
        public System.Data.Services.Client.QueryOperationResponse EndLoadProperty(System.IAsyncResult asyncResult) { return default(System.Data.Services.Client.QueryOperationResponse); }
        public System.Data.Services.Client.DataServiceResponse EndSaveChanges(System.IAsyncResult asyncResult) { return default(System.Data.Services.Client.DataServiceResponse); }
        public System.Data.Services.Client.QueryOperationResponse<T> Execute<T>(System.Data.Services.Client.DataServiceQueryContinuation<T> continuation) { return default(System.Data.Services.Client.QueryOperationResponse<T>); }
        public System.Collections.Generic.IEnumerable<TElement> Execute<TElement>(System.Uri requestUri) { return default(System.Collections.Generic.IEnumerable<TElement>); }
        public System.Data.Services.Client.DataServiceResponse ExecuteBatch(params System.Data.Services.Client.DataServiceRequest[] queries) { return default(System.Data.Services.Client.DataServiceResponse); }
        public System.Data.Services.Client.EntityDescriptor GetEntityDescriptor(object entity) { return default(System.Data.Services.Client.EntityDescriptor); }
        public System.Data.Services.Client.LinkDescriptor GetLinkDescriptor(object source, string sourceProperty, object target) { return default(System.Data.Services.Client.LinkDescriptor); }
        public System.Uri GetMetadataUri() { return default(System.Uri); }
        public System.Data.Services.Client.DataServiceStreamResponse GetReadStream(object entity) { return default(System.Data.Services.Client.DataServiceStreamResponse); }
        public System.Data.Services.Client.DataServiceStreamResponse GetReadStream(object entity, System.Data.Services.Client.DataServiceRequestArgs args) { return default(System.Data.Services.Client.DataServiceStreamResponse); }
        public System.Data.Services.Client.DataServiceStreamResponse GetReadStream(object entity, string acceptContentType) { return default(System.Data.Services.Client.DataServiceStreamResponse); }
        public System.Uri GetReadStreamUri(object entity) { return default(System.Uri); }
        public System.Data.Services.Client.QueryOperationResponse LoadProperty(object entity, string propertyName) { return default(System.Data.Services.Client.QueryOperationResponse); }
        public System.Data.Services.Client.QueryOperationResponse LoadProperty(object entity, string propertyName, System.Data.Services.Client.DataServiceQueryContinuation continuation) { return default(System.Data.Services.Client.QueryOperationResponse); }
        public System.Data.Services.Client.QueryOperationResponse LoadProperty(object entity, string propertyName, System.Uri nextLinkUri) { return default(System.Data.Services.Client.QueryOperationResponse); }
        public System.Data.Services.Client.QueryOperationResponse<T> LoadProperty<T>(object entity, string propertyName, System.Data.Services.Client.DataServiceQueryContinuation<T> continuation) { return default(System.Data.Services.Client.QueryOperationResponse<T>); }
        public System.Data.Services.Client.DataServiceResponse SaveChanges() { return default(System.Data.Services.Client.DataServiceResponse); }
        public System.Data.Services.Client.DataServiceResponse SaveChanges(System.Data.Services.Client.SaveChangesOptions options) { return default(System.Data.Services.Client.DataServiceResponse); }
        public void SetLink(object source, string sourceProperty, object target) { }
        public void SetSaveStream(object entity, System.IO.Stream stream, bool closeStream, System.Data.Services.Client.DataServiceRequestArgs args) { }
        public void SetSaveStream(object entity, System.IO.Stream stream, bool closeStream, string contentType, string slug) { }
        public bool TryGetEntity<TEntity>(System.Uri identity, out TEntity entity) where TEntity : class { entity = default(TEntity); return default(bool); }
        public bool TryGetUri(object entity, out System.Uri identity) { identity = default(System.Uri); return default(bool); }
        public void UpdateObject(object entity) { }
    }
    public abstract partial class DataServiceQuery : System.Data.Services.Client.DataServiceRequest, System.Collections.IEnumerable, System.Linq.IQueryable
    {
        internal DataServiceQuery() { }
        public abstract System.Linq.Expressions.Expression Expression { get; }
        public abstract System.Linq.IQueryProvider Provider { get; }
        public System.IAsyncResult BeginExecute(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public System.Collections.IEnumerable EndExecute(System.IAsyncResult asyncResult) { return default(System.Collections.IEnumerable); }
        public System.Collections.IEnumerable Execute() { return default(System.Collections.IEnumerable); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public partial class DataServiceQuery<TElement> : System.Data.Services.Client.DataServiceQuery, System.Collections.Generic.IEnumerable<TElement>, System.Collections.IEnumerable, System.Linq.IQueryable, System.Linq.IQueryable<TElement>
    {
        internal DataServiceQuery() { }
        public override System.Type ElementType { get { return default(System.Type); } }
        public override System.Linq.Expressions.Expression Expression { get { return default(System.Linq.Expressions.Expression); } }
        public override System.Linq.IQueryProvider Provider { get { return default(System.Linq.IQueryProvider); } }
        public override System.Uri RequestUri { get { return default(System.Uri); } }
        public System.Data.Services.Client.DataServiceQuery<TElement> AddQueryOption(string name, object value) { return default(System.Data.Services.Client.DataServiceQuery<TElement>); }
        public new System.IAsyncResult BeginExecute(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public new System.Collections.Generic.IEnumerable<TElement> EndExecute(System.IAsyncResult asyncResult) { return default(System.Collections.Generic.IEnumerable<TElement>); }
        public new System.Collections.Generic.IEnumerable<TElement> Execute() { return default(System.Collections.Generic.IEnumerable<TElement>); }
        public System.Data.Services.Client.DataServiceQuery<TElement> Expand(string path) { return default(System.Data.Services.Client.DataServiceQuery<TElement>); }
        public System.Collections.Generic.IEnumerator<TElement> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<TElement>); }
        public System.Data.Services.Client.DataServiceQuery<TElement> IncludeTotalCount() { return default(System.Data.Services.Client.DataServiceQuery<TElement>); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public override string ToString() { return default(string); }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{NextLinkUri}")]
    public abstract partial class DataServiceQueryContinuation
    {
        internal DataServiceQueryContinuation() { }
        public System.Uri NextLinkUri { get { return default(System.Uri); } }
        public override string ToString() { return default(string); }
    }
    public sealed partial class DataServiceQueryContinuation<T> : System.Data.Services.Client.DataServiceQueryContinuation
    {
        internal DataServiceQueryContinuation() { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{Message}")]
    public sealed partial class DataServiceQueryException : System.InvalidOperationException
    {
        public DataServiceQueryException() { }
        /*
         * Had to comment this out since csc.exe would throw an error saying that protected members can't exist in sealed classes.
        protected DataServiceQueryException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        */
        public DataServiceQueryException(string message) { }
        public DataServiceQueryException(string message, System.Exception innerException) { }
        public DataServiceQueryException(string message, System.Exception innerException, System.Data.Services.Client.QueryOperationResponse response) { }
        public System.Data.Services.Client.QueryOperationResponse Response { get { return default(System.Data.Services.Client.QueryOperationResponse); } }
    }
    public abstract partial class DataServiceRequest
    {
        internal DataServiceRequest() { }
        public abstract System.Type ElementType { get; }
        public abstract System.Uri RequestUri { get; }
        public override string ToString() { return default(string); }
    }
    public sealed partial class DataServiceRequest<TElement> : System.Data.Services.Client.DataServiceRequest
    {
        public DataServiceRequest(System.Uri requestUri) { }
        public override System.Type ElementType { get { return default(System.Type); } }
        public override System.Uri RequestUri { get { return default(System.Uri); } }
    }
    public partial class DataServiceRequestArgs
    {
        public DataServiceRequestArgs() { }
        public string AcceptContentType { get { return default(string); } set { } }
        public string ContentType { get { return default(string); } set { } }
        public System.Collections.Generic.Dictionary<string, string> Headers { get { return default(System.Collections.Generic.Dictionary<string, string>); } }
        public string Slug { get { return default(string); } set { } }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{Message}")]
    public sealed partial class DataServiceRequestException : System.InvalidOperationException
    {
        public DataServiceRequestException() { }
        /*
         * Had to comment this out since csc.exe would throw an error saying that protected members can't exist in sealed classes.
        protected DataServiceRequestException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        */
        public DataServiceRequestException(string message) { }
        public DataServiceRequestException(string message, System.Exception innerException) { }
        public DataServiceRequestException(string message, System.Exception innerException, System.Data.Services.Client.DataServiceResponse response) { }
        public System.Data.Services.Client.DataServiceResponse Response { get { return default(System.Data.Services.Client.DataServiceResponse); } }
    }
    public sealed partial class DataServiceResponse : System.Collections.Generic.IEnumerable<System.Data.Services.Client.OperationResponse>, System.Collections.IEnumerable
    {
        internal DataServiceResponse() { }
        public System.Collections.Generic.IDictionary<string, string> BatchHeaders { get { return default(System.Collections.Generic.IDictionary<string, string>); } }
        public int BatchStatusCode { get { return default(int); } }
        public bool IsBatchResponse { get { return default(bool); } }
        public System.Collections.Generic.IEnumerator<System.Data.Services.Client.OperationResponse> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Data.Services.Client.OperationResponse>); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public sealed partial class DataServiceStreamResponse : System.IDisposable
    {
        internal DataServiceStreamResponse() { }
        public string ContentDisposition { get { return default(string); } }
        public string ContentType { get { return default(string); } }
        public System.Collections.Generic.Dictionary<string, string> Headers { get { return default(System.Collections.Generic.Dictionary<string, string>); } }
        public System.IO.Stream Stream { get { return default(System.IO.Stream); } }
        public void Dispose() { }
    }
    public abstract partial class Descriptor
    {
        internal Descriptor() { }
        public System.Data.Services.Client.EntityStates State { get { return default(System.Data.Services.Client.EntityStates); } }
    }
    public sealed partial class EntityChangedParams
    {
        internal EntityChangedParams() { }
        public System.Data.Services.Client.DataServiceContext Context { get { return default(System.Data.Services.Client.DataServiceContext); } }
        public object Entity { get { return default(object); } }
        public string PropertyName { get { return default(string); } }
        public object PropertyValue { get { return default(object); } }
        public string SourceEntitySet { get { return default(string); } }
        public string TargetEntitySet { get { return default(string); } }
    }
    public sealed partial class EntityCollectionChangedParams
    {
        internal EntityCollectionChangedParams() { }
        public System.Collections.Specialized.NotifyCollectionChangedAction Action { get { return default(System.Collections.Specialized.NotifyCollectionChangedAction); } }
        public System.Collections.ICollection Collection { get { return default(System.Collections.ICollection); } }
        public System.Data.Services.Client.DataServiceContext Context { get { return default(System.Data.Services.Client.DataServiceContext); } }
        public string PropertyName { get { return default(string); } }
        public object SourceEntity { get { return default(object); } }
        public string SourceEntitySet { get { return default(string); } }
        public object TargetEntity { get { return default(object); } }
        public string TargetEntitySet { get { return default(string); } }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("State = {state}, Uri = {editLink}, Element = {entity.GetType().ToString()}")]
    public sealed partial class EntityDescriptor : System.Data.Services.Client.Descriptor
    {
        internal EntityDescriptor() { }
        public System.Uri EditLink { get { return default(System.Uri); } }
        public System.Uri EditStreamUri { get { return default(System.Uri); } }
        public object Entity { get { return default(object); } }
        public string ETag { get { return default(string); } }
        public string Identity { get { return default(string); } }
        public System.Data.Services.Client.EntityDescriptor ParentForInsert { get { return default(System.Data.Services.Client.EntityDescriptor); } }
        public string ParentPropertyForInsert { get { return default(string); } }
        public System.Uri ReadStreamUri { get { return default(System.Uri); } }
        public System.Uri SelfLink { get { return default(System.Uri); } }
        public string ServerTypeName { get { return default(string); } }
        public string StreamETag { get { return default(string); } }
    }
    [System.FlagsAttribute]
    public enum EntityStates
    {
        Added = 4,
        Deleted = 8,
        Detached = 1,
        Modified = 16,
        Unchanged = 2,
    }
    [System.Diagnostics.DebuggerDisplayAttribute("State = {state}")]
    public sealed partial class LinkDescriptor : System.Data.Services.Client.Descriptor
    {
        internal LinkDescriptor() { }
        public object Source { get { return default(object); } }
        public string SourceProperty { get { return default(string); } }
        public object Target { get { return default(object); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), AllowMultiple=false, Inherited=true)]
    public sealed partial class MediaEntryAttribute : System.Attribute
    {
        public MediaEntryAttribute(string mediaMemberName) { }
        public string MediaMemberName { get { return default(string); } }
    }
    public enum MergeOption
    {
        AppendOnly = 0,
        NoTracking = 3,
        OverwriteChanges = 1,
        PreserveChanges = 2,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), AllowMultiple=false, Inherited=true)]
    public sealed partial class MimeTypePropertyAttribute : System.Attribute
    {
        public MimeTypePropertyAttribute(string dataPropertyName, string mimeTypePropertyName) { }
        public string DataPropertyName { get { return default(string); } }
        public string MimeTypePropertyName { get { return default(string); } }
    }
    public abstract partial class OperationResponse
    {
        internal OperationResponse() { }
        public System.Exception Error { get { return default(System.Exception); } set { } }
        public System.Collections.Generic.IDictionary<string, string> Headers { get { return default(System.Collections.Generic.IDictionary<string, string>); } }
        public int StatusCode { get { return default(int); } }
    }
    public partial class QueryOperationResponse : System.Data.Services.Client.OperationResponse, System.Collections.IEnumerable
    {
        internal QueryOperationResponse() { }
        public System.Data.Services.Client.DataServiceRequest Query { get { return default(System.Data.Services.Client.DataServiceRequest); } }
        public virtual long TotalCount { get { return default(long); } }
        public System.Data.Services.Client.DataServiceQueryContinuation GetContinuation() { return default(System.Data.Services.Client.DataServiceQueryContinuation); }
        public System.Data.Services.Client.DataServiceQueryContinuation GetContinuation(System.Collections.IEnumerable collection) { return default(System.Data.Services.Client.DataServiceQueryContinuation); }
        public System.Data.Services.Client.DataServiceQueryContinuation<T> GetContinuation<T>(System.Collections.Generic.IEnumerable<T> collection) { return default(System.Data.Services.Client.DataServiceQueryContinuation<T>); }
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public sealed partial class QueryOperationResponse<T> : System.Data.Services.Client.QueryOperationResponse, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable
    {
        internal QueryOperationResponse() { }
        public override long TotalCount { get { return default(long); } }
        public new System.Data.Services.Client.DataServiceQueryContinuation<T> GetContinuation() { return default(System.Data.Services.Client.DataServiceQueryContinuation<T>); }
        public new System.Collections.Generic.IEnumerator<T> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
    }
    public sealed partial class ReadingWritingEntityEventArgs : System.EventArgs
    {
        internal ReadingWritingEntityEventArgs() { }
        public System.Xml.Linq.XElement Data { [System.Diagnostics.DebuggerStepThroughAttribute]get { return default(System.Xml.Linq.XElement); } }
        public object Entity { get { return default(object); } }
    }
    [System.FlagsAttribute]
    public enum SaveChangesOptions
    {
        Batch = 1,
        ContinueOnError = 2,
        None = 0,
        ReplaceOnUpdate = 4,
    }
    public partial class SendingRequestEventArgs : System.EventArgs
    {
        internal SendingRequestEventArgs() { }
        public System.Net.WebRequest Request { get { return default(System.Net.WebRequest); } set { } }
        public System.Net.WebHeaderCollection RequestHeaders { get { return default(System.Net.WebHeaderCollection); } }
    }
    public enum TrackingMode
    {
        AutoChangeTracking = 1,
        None = 0,
    }
}
namespace System.Data.Services.Common
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), AllowMultiple=false)]
    public sealed partial class DataServiceEntityAttribute : System.Attribute
    {
        public DataServiceEntityAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), AllowMultiple=false)]
    public sealed partial class DataServiceKeyAttribute : System.Attribute
    {
        public DataServiceKeyAttribute(string keyName) { }
        public DataServiceKeyAttribute(params string[] keyNames) { }
        public System.Collections.ObjectModel.ReadOnlyCollection<string> KeyNames { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } }
    }
    public enum DataServiceProtocolVersion
    {
        V1 = 0,
        V2 = 1,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), AllowMultiple=true, Inherited=true)]
    public sealed partial class EntityPropertyMappingAttribute : System.Attribute
    {
        public EntityPropertyMappingAttribute(string sourcePath, System.Data.Services.Common.SyndicationItemProperty targetSyndicationItem, System.Data.Services.Common.SyndicationTextContentKind targetTextContentKind, bool keepInContent) { }
        public EntityPropertyMappingAttribute(string sourcePath, string targetPath, string targetNamespacePrefix, string targetNamespaceUri, bool keepInContent) { }
        public bool KeepInContent { get { return default(bool); } }
        public string SourcePath { get { return default(string); } }
        public string TargetNamespacePrefix { get { return default(string); } }
        public string TargetNamespaceUri { get { return default(string); } }
        public string TargetPath { get { return default(string); } }
        public System.Data.Services.Common.SyndicationItemProperty TargetSyndicationItem { get { return default(System.Data.Services.Common.SyndicationItemProperty); } }
        public System.Data.Services.Common.SyndicationTextContentKind TargetTextContentKind { get { return default(System.Data.Services.Common.SyndicationTextContentKind); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), AllowMultiple=false)]
    public sealed partial class EntitySetAttribute : System.Attribute
    {
        public EntitySetAttribute(string entitySet) { }
        public string EntitySet { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), Inherited=true, AllowMultiple=false)]
    public sealed partial class HasStreamAttribute : System.Attribute
    {
        public HasStreamAttribute() { }
    }
    public enum SyndicationItemProperty
    {
        AuthorEmail = 1,
        AuthorName = 2,
        AuthorUri = 3,
        ContributorEmail = 4,
        ContributorName = 5,
        ContributorUri = 6,
        CustomProperty = 0,
        Published = 8,
        Rights = 9,
        Summary = 10,
        Title = 11,
        Updated = 7,
    }
    public enum SyndicationTextContentKind
    {
        Html = 1,
        Plaintext = 0,
        Xhtml = 2,
    }
}

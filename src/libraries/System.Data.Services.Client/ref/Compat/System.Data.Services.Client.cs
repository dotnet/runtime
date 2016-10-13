// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.Services.Client
{
    public sealed partial class ChangeOperationResponse : System.Data.Services.Client.OperationResponse
    {
        internal ChangeOperationResponse() { }
        public System.Data.Services.Client.Descriptor Descriptor { get { throw null; } }
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
        public int StatusCode { get { throw null; } }
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
        public System.Data.Services.Client.DataServiceQueryContinuation<T> Continuation { get { throw null; } set { } }
        public void Clear(bool stopTracking) { }
        public void Detach() { }
        protected override void InsertItem(int index, T item) { }
        public void Load(T item) { }
        public void Load(System.Collections.Generic.IEnumerable<T> items) { }
    }
    public partial class DataServiceContext
    {
        public DataServiceContext(System.Uri serviceRoot) { }
        public bool ApplyingChanges { get { throw null; } }
        public System.Uri BaseUri { get { throw null; } }
        public System.Net.ICredentials Credentials { get { throw null; } set { } }
        public string DataNamespace { get { throw null; } set { } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Data.Services.Client.EntityDescriptor> Entities { get { throw null; } }
        public bool IgnoreMissingProperties { get { throw null; } set { } }
        public bool IgnoreResourceNotFoundException { get { throw null; } set { } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Data.Services.Client.LinkDescriptor> Links { get { throw null; } }
        public System.Data.Services.Client.MergeOption MergeOption { get { throw null; } set { } }
        public System.Func<System.Type, string> ResolveName { get { throw null; } set { } }
        public System.Func<string, System.Type> ResolveType { get { throw null; } set { } }
        public System.Data.Services.Client.SaveChangesOptions SaveChangesDefaultOptions { get { throw null; } set { } }
        public int Timeout { get { throw null; } set { } }
        public System.Uri TypeScheme { get { throw null; } set { } }
        public bool UsePostTunneling { get { throw null; } set { } }
        public event System.EventHandler<System.Data.Services.Client.ReadingWritingEntityEventArgs> ReadingEntity { add { } remove { } }
        public event System.EventHandler<System.Data.Services.Client.SendingRequestEventArgs> SendingRequest { add { } remove { } }
        public event System.EventHandler<System.Data.Services.Client.ReadingWritingEntityEventArgs> WritingEntity { add { } remove { } }
        public void AddLink(object source, string sourceProperty, object target) { }
        public void AddObject(string entitySetName, object entity) { }
        public void AddRelatedObject(object source, string sourceProperty, object target) { }
        public void AttachLink(object source, string sourceProperty, object target) { }
        public void AttachTo(string entitySetName, object entity) { }
        public void AttachTo(string entitySetName, object entity, string etag) { }
        public System.IAsyncResult BeginExecute<T>(System.Data.Services.Client.DataServiceQueryContinuation<T> continuation, System.AsyncCallback callback, object state) { throw null; }
        public System.IAsyncResult BeginExecute<TElement>(System.Uri requestUri, System.AsyncCallback callback, object state) { throw null; }
        public System.IAsyncResult BeginExecuteBatch(System.AsyncCallback callback, object state, params System.Data.Services.Client.DataServiceRequest[] queries) { throw null; }
        public System.IAsyncResult BeginGetReadStream(object entity, System.Data.Services.Client.DataServiceRequestArgs args, System.AsyncCallback callback, object state) { throw null; }
        public System.IAsyncResult BeginLoadProperty(object entity, string propertyName, System.AsyncCallback callback, object state) { throw null; }
        public System.IAsyncResult BeginLoadProperty(object entity, string propertyName, System.Data.Services.Client.DataServiceQueryContinuation continuation, System.AsyncCallback callback, object state) { throw null; }
        public System.IAsyncResult BeginLoadProperty(object entity, string propertyName, System.Uri nextLinkUri, System.AsyncCallback callback, object state) { throw null; }
        public System.IAsyncResult BeginSaveChanges(System.AsyncCallback callback, object state) { throw null; }
        public System.IAsyncResult BeginSaveChanges(System.Data.Services.Client.SaveChangesOptions options, System.AsyncCallback callback, object state) { throw null; }
        public void CancelRequest(System.IAsyncResult asyncResult) { }
        public System.Data.Services.Client.DataServiceQuery<T> CreateQuery<T>(string entitySetName) { throw null; }
        public void DeleteLink(object source, string sourceProperty, object target) { }
        public void DeleteObject(object entity) { }
        public bool Detach(object entity) { throw null; }
        public bool DetachLink(object source, string sourceProperty, object target) { throw null; }
        public System.Collections.Generic.IEnumerable<TElement> EndExecute<TElement>(System.IAsyncResult asyncResult) { throw null; }
        public System.Data.Services.Client.DataServiceResponse EndExecuteBatch(System.IAsyncResult asyncResult) { throw null; }
        public System.Data.Services.Client.DataServiceStreamResponse EndGetReadStream(System.IAsyncResult asyncResult) { throw null; }
        public System.Data.Services.Client.QueryOperationResponse EndLoadProperty(System.IAsyncResult asyncResult) { throw null; }
        public System.Data.Services.Client.DataServiceResponse EndSaveChanges(System.IAsyncResult asyncResult) { throw null; }
        public System.Data.Services.Client.QueryOperationResponse<T> Execute<T>(System.Data.Services.Client.DataServiceQueryContinuation<T> continuation) { throw null; }
        public System.Collections.Generic.IEnumerable<TElement> Execute<TElement>(System.Uri requestUri) { throw null; }
        public System.Data.Services.Client.DataServiceResponse ExecuteBatch(params System.Data.Services.Client.DataServiceRequest[] queries) { throw null; }
        public System.Data.Services.Client.EntityDescriptor GetEntityDescriptor(object entity) { throw null; }
        public System.Data.Services.Client.LinkDescriptor GetLinkDescriptor(object source, string sourceProperty, object target) { throw null; }
        public System.Uri GetMetadataUri() { throw null; }
        public System.Data.Services.Client.DataServiceStreamResponse GetReadStream(object entity) { throw null; }
        public System.Data.Services.Client.DataServiceStreamResponse GetReadStream(object entity, System.Data.Services.Client.DataServiceRequestArgs args) { throw null; }
        public System.Data.Services.Client.DataServiceStreamResponse GetReadStream(object entity, string acceptContentType) { throw null; }
        public System.Uri GetReadStreamUri(object entity) { throw null; }
        public System.Data.Services.Client.QueryOperationResponse LoadProperty(object entity, string propertyName) { throw null; }
        public System.Data.Services.Client.QueryOperationResponse LoadProperty(object entity, string propertyName, System.Data.Services.Client.DataServiceQueryContinuation continuation) { throw null; }
        public System.Data.Services.Client.QueryOperationResponse LoadProperty(object entity, string propertyName, System.Uri nextLinkUri) { throw null; }
        public System.Data.Services.Client.QueryOperationResponse<T> LoadProperty<T>(object entity, string propertyName, System.Data.Services.Client.DataServiceQueryContinuation<T> continuation) { throw null; }
        public System.Data.Services.Client.DataServiceResponse SaveChanges() { throw null; }
        public System.Data.Services.Client.DataServiceResponse SaveChanges(System.Data.Services.Client.SaveChangesOptions options) { throw null; }
        public void SetLink(object source, string sourceProperty, object target) { }
        public void SetSaveStream(object entity, System.IO.Stream stream, bool closeStream, System.Data.Services.Client.DataServiceRequestArgs args) { }
        public void SetSaveStream(object entity, System.IO.Stream stream, bool closeStream, string contentType, string slug) { }
        public bool TryGetEntity<TEntity>(System.Uri identity, out TEntity entity) where TEntity : class { throw null; }
        public bool TryGetUri(object entity, out System.Uri identity) { throw null; }
        public void UpdateObject(object entity) { }
    }
    public abstract partial class DataServiceQuery : System.Data.Services.Client.DataServiceRequest, System.Collections.IEnumerable, System.Linq.IQueryable
    {
        internal DataServiceQuery() { }
        public abstract System.Linq.Expressions.Expression Expression { get; }
        public abstract System.Linq.IQueryProvider Provider { get; }
        public System.IAsyncResult BeginExecute(System.AsyncCallback callback, object state) { throw null; }
        public System.Collections.IEnumerable EndExecute(System.IAsyncResult asyncResult) { throw null; }
        public System.Collections.IEnumerable Execute() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class DataServiceQuery<TElement> : System.Data.Services.Client.DataServiceQuery, System.Collections.Generic.IEnumerable<TElement>, System.Collections.IEnumerable, System.Linq.IQueryable, System.Linq.IQueryable<TElement>
    {
        internal DataServiceQuery() { }
        public override System.Type ElementType { get { throw null; } }
        public override System.Linq.Expressions.Expression Expression { get { throw null; } }
        public override System.Linq.IQueryProvider Provider { get { throw null; } }
        public override System.Uri RequestUri { get { throw null; } }
        public System.Data.Services.Client.DataServiceQuery<TElement> AddQueryOption(string name, object value) { throw null; }
        public new System.IAsyncResult BeginExecute(System.AsyncCallback callback, object state) { throw null; }
        public new System.Collections.Generic.IEnumerable<TElement> EndExecute(System.IAsyncResult asyncResult) { throw null; }
        public new System.Collections.Generic.IEnumerable<TElement> Execute() { throw null; }
        public System.Data.Services.Client.DataServiceQuery<TElement> Expand(string path) { throw null; }
        public System.Collections.Generic.IEnumerator<TElement> GetEnumerator() { throw null; }
        public System.Data.Services.Client.DataServiceQuery<TElement> IncludeTotalCount() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{NextLinkUri}")]
    public abstract partial class DataServiceQueryContinuation
    {
        internal DataServiceQueryContinuation() { }
        public System.Uri NextLinkUri { get { throw null; } }
        public override string ToString() { throw null; }
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
        public System.Data.Services.Client.QueryOperationResponse Response { get { throw null; } }
    }
    public abstract partial class DataServiceRequest
    {
        internal DataServiceRequest() { }
        public abstract System.Type ElementType { get; }
        public abstract System.Uri RequestUri { get; }
        public override string ToString() { throw null; }
    }
    public sealed partial class DataServiceRequest<TElement> : System.Data.Services.Client.DataServiceRequest
    {
        public DataServiceRequest(System.Uri requestUri) { }
        public override System.Type ElementType { get { throw null; } }
        public override System.Uri RequestUri { get { throw null; } }
    }
    public partial class DataServiceRequestArgs
    {
        public DataServiceRequestArgs() { }
        public string AcceptContentType { get { throw null; } set { } }
        public string ContentType { get { throw null; } set { } }
        public System.Collections.Generic.Dictionary<string, string> Headers { get { throw null; } }
        public string Slug { get { throw null; } set { } }
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
        public System.Data.Services.Client.DataServiceResponse Response { get { throw null; } }
    }
    public sealed partial class DataServiceResponse : System.Collections.Generic.IEnumerable<System.Data.Services.Client.OperationResponse>, System.Collections.IEnumerable
    {
        internal DataServiceResponse() { }
        public System.Collections.Generic.IDictionary<string, string> BatchHeaders { get { throw null; } }
        public int BatchStatusCode { get { throw null; } }
        public bool IsBatchResponse { get { throw null; } }
        public System.Collections.Generic.IEnumerator<System.Data.Services.Client.OperationResponse> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class DataServiceStreamResponse : System.IDisposable
    {
        internal DataServiceStreamResponse() { }
        public string ContentDisposition { get { throw null; } }
        public string ContentType { get { throw null; } }
        public System.Collections.Generic.Dictionary<string, string> Headers { get { throw null; } }
        public System.IO.Stream Stream { get { throw null; } }
        public void Dispose() { }
    }
    public abstract partial class Descriptor
    {
        internal Descriptor() { }
        public System.Data.Services.Client.EntityStates State { get { throw null; } }
    }
    public sealed partial class EntityChangedParams
    {
        internal EntityChangedParams() { }
        public System.Data.Services.Client.DataServiceContext Context { get { throw null; } }
        public object Entity { get { throw null; } }
        public string PropertyName { get { throw null; } }
        public object PropertyValue { get { throw null; } }
        public string SourceEntitySet { get { throw null; } }
        public string TargetEntitySet { get { throw null; } }
    }
    public sealed partial class EntityCollectionChangedParams
    {
        internal EntityCollectionChangedParams() { }
        public System.Collections.Specialized.NotifyCollectionChangedAction Action { get { throw null; } }
        public System.Collections.ICollection Collection { get { throw null; } }
        public System.Data.Services.Client.DataServiceContext Context { get { throw null; } }
        public string PropertyName { get { throw null; } }
        public object SourceEntity { get { throw null; } }
        public string SourceEntitySet { get { throw null; } }
        public object TargetEntity { get { throw null; } }
        public string TargetEntitySet { get { throw null; } }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("State = {state}, Uri = {editLink}, Element = {entity.GetType().ToString()}")]
    public sealed partial class EntityDescriptor : System.Data.Services.Client.Descriptor
    {
        internal EntityDescriptor() { }
        public System.Uri EditLink { get { throw null; } }
        public System.Uri EditStreamUri { get { throw null; } }
        public object Entity { get { throw null; } }
        public string ETag { get { throw null; } }
        public string Identity { get { throw null; } }
        public System.Data.Services.Client.EntityDescriptor ParentForInsert { get { throw null; } }
        public string ParentPropertyForInsert { get { throw null; } }
        public System.Uri ReadStreamUri { get { throw null; } }
        public System.Uri SelfLink { get { throw null; } }
        public string ServerTypeName { get { throw null; } }
        public string StreamETag { get { throw null; } }
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
        public object Source { get { throw null; } }
        public string SourceProperty { get { throw null; } }
        public object Target { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), AllowMultiple=false, Inherited=true)]
    public sealed partial class MediaEntryAttribute : System.Attribute
    {
        public MediaEntryAttribute(string mediaMemberName) { }
        public string MediaMemberName { get { throw null; } }
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
        public string DataPropertyName { get { throw null; } }
        public string MimeTypePropertyName { get { throw null; } }
    }
    public abstract partial class OperationResponse
    {
        internal OperationResponse() { }
        public System.Exception Error { get { throw null; } set { } }
        public System.Collections.Generic.IDictionary<string, string> Headers { get { throw null; } }
        public int StatusCode { get { throw null; } }
    }
    public partial class QueryOperationResponse : System.Data.Services.Client.OperationResponse, System.Collections.IEnumerable
    {
        internal QueryOperationResponse() { }
        public System.Data.Services.Client.DataServiceRequest Query { get { throw null; } }
        public virtual long TotalCount { get { throw null; } }
        public System.Data.Services.Client.DataServiceQueryContinuation GetContinuation() { throw null; }
        public System.Data.Services.Client.DataServiceQueryContinuation GetContinuation(System.Collections.IEnumerable collection) { throw null; }
        public System.Data.Services.Client.DataServiceQueryContinuation<T> GetContinuation<T>(System.Collections.Generic.IEnumerable<T> collection) { throw null; }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
    }
    public sealed partial class QueryOperationResponse<T> : System.Data.Services.Client.QueryOperationResponse, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable
    {
        internal QueryOperationResponse() { }
        public override long TotalCount { get { throw null; } }
        public new System.Data.Services.Client.DataServiceQueryContinuation<T> GetContinuation() { throw null; }
        public new System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
    }
    public sealed partial class ReadingWritingEntityEventArgs : System.EventArgs
    {
        internal ReadingWritingEntityEventArgs() { }
        public System.Xml.Linq.XElement Data { [System.Diagnostics.DebuggerStepThroughAttribute]get { throw null; } }
        public object Entity { get { throw null; } }
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
        public System.Net.WebRequest Request { get { throw null; } set { } }
        public System.Net.WebHeaderCollection RequestHeaders { get { throw null; } }
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
        public System.Collections.ObjectModel.ReadOnlyCollection<string> KeyNames { get { throw null; } }
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
        public bool KeepInContent { get { throw null; } }
        public string SourcePath { get { throw null; } }
        public string TargetNamespacePrefix { get { throw null; } }
        public string TargetNamespaceUri { get { throw null; } }
        public string TargetPath { get { throw null; } }
        public System.Data.Services.Common.SyndicationItemProperty TargetSyndicationItem { get { throw null; } }
        public System.Data.Services.Common.SyndicationTextContentKind TargetTextContentKind { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), AllowMultiple=false)]
    public sealed partial class EntitySetAttribute : System.Attribute
    {
        public EntitySetAttribute(string entitySet) { }
        public string EntitySet { get { throw null; } }
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Net.Http
{
    public partial class ByteArrayContent : System.Net.Http.HttpContent
    {
        public ByteArrayContent(byte[] content) { }
        public ByteArrayContent(byte[] content, int offset, int count) { }
        protected override System.Threading.Tasks.Task<System.IO.Stream> CreateContentReadStreamAsync() { return default(System.Threading.Tasks.Task<System.IO.Stream>); }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext context) { return default(System.Threading.Tasks.Task); }
        protected internal override bool TryComputeLength(out long length) { length = default(long); return default(bool); }
    }
    public enum ClientCertificateOption
    {
        Automatic = 1,
        Manual = 0,
    }
    public abstract partial class DelegatingHandler : System.Net.Http.HttpMessageHandler
    {
        protected DelegatingHandler() { }
        protected DelegatingHandler(System.Net.Http.HttpMessageHandler innerHandler) { }
        public System.Net.Http.HttpMessageHandler InnerHandler { get { return default(System.Net.Http.HttpMessageHandler); } set { } }
        protected override void Dispose(bool disposing) { }
        protected internal override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
    }
    public partial class FormUrlEncodedContent : System.Net.Http.ByteArrayContent
    {
        public FormUrlEncodedContent(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> nameValueCollection) : base(default(byte[])) { }
    }
    public partial class HttpClient : System.Net.Http.HttpMessageInvoker
    {
        public HttpClient() : base(default(System.Net.Http.HttpMessageHandler)) { }
        public HttpClient(System.Net.Http.HttpMessageHandler handler) : base(default(System.Net.Http.HttpMessageHandler)) { }
        public HttpClient(System.Net.Http.HttpMessageHandler handler, bool disposeHandler) : base(default(System.Net.Http.HttpMessageHandler)) { }
        public System.Uri BaseAddress { get { return default(System.Uri); } set { } }
        public System.Net.Http.Headers.HttpRequestHeaders DefaultRequestHeaders { get { return default(System.Net.Http.Headers.HttpRequestHeaders); } }
        public long MaxResponseContentBufferSize { get { return default(long); } set { } }
        public System.TimeSpan Timeout { get { return default(System.TimeSpan); } set { } }
        public void CancelPendingRequests() { }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> DeleteAsync(string requestUri) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> DeleteAsync(string requestUri, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> DeleteAsync(System.Uri requestUri) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> DeleteAsync(System.Uri requestUri, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        protected override void Dispose(bool disposing) { }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync(string requestUri) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync(string requestUri, System.Net.Http.HttpCompletionOption completionOption) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync(string requestUri, System.Net.Http.HttpCompletionOption completionOption, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync(string requestUri, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync(System.Uri requestUri) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync(System.Uri requestUri, System.Net.Http.HttpCompletionOption completionOption) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync(System.Uri requestUri, System.Net.Http.HttpCompletionOption completionOption, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync(System.Uri requestUri, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<byte[]> GetByteArrayAsync(string requestUri) { return default(System.Threading.Tasks.Task<byte[]>); }
        public System.Threading.Tasks.Task<byte[]> GetByteArrayAsync(System.Uri requestUri) { return default(System.Threading.Tasks.Task<byte[]>); }
        public System.Threading.Tasks.Task<System.IO.Stream> GetStreamAsync(string requestUri) { return default(System.Threading.Tasks.Task<System.IO.Stream>); }
        public System.Threading.Tasks.Task<System.IO.Stream> GetStreamAsync(System.Uri requestUri) { return default(System.Threading.Tasks.Task<System.IO.Stream>); }
        public System.Threading.Tasks.Task<string> GetStringAsync(string requestUri) { return default(System.Threading.Tasks.Task<string>); }
        public System.Threading.Tasks.Task<string> GetStringAsync(System.Uri requestUri) { return default(System.Threading.Tasks.Task<string>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PostAsync(string requestUri, System.Net.Http.HttpContent content) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PostAsync(string requestUri, System.Net.Http.HttpContent content, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PostAsync(System.Uri requestUri, System.Net.Http.HttpContent content) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PostAsync(System.Uri requestUri, System.Net.Http.HttpContent content, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PutAsync(string requestUri, System.Net.Http.HttpContent content) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PutAsync(string requestUri, System.Net.Http.HttpContent content, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PutAsync(System.Uri requestUri, System.Net.Http.HttpContent content) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PutAsync(System.Uri requestUri, System.Net.Http.HttpContent content, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Net.Http.HttpCompletionOption completionOption) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Net.Http.HttpCompletionOption completionOption, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
        public override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
    }
    public partial class HttpClientHandler : System.Net.Http.HttpMessageHandler
    {
        public HttpClientHandler() { }
        public bool AllowAutoRedirect { get { return default(bool); } set { } }
        public System.Net.DecompressionMethods AutomaticDecompression { get { return default(System.Net.DecompressionMethods); } set { } }
        public System.Net.Http.ClientCertificateOption ClientCertificateOptions { get { return default(System.Net.Http.ClientCertificateOption); } set { } }
        public System.Net.CookieContainer CookieContainer { get { return default(System.Net.CookieContainer); } set { } }
        public System.Net.ICredentials Credentials { get { return default(System.Net.ICredentials); } set { } }
        public int MaxAutomaticRedirections { get { return default(int); } set { } }
        public long MaxRequestContentBufferSize { get { return default(long); } set { } }
        public bool PreAuthenticate { get { return default(bool); } set { } }
        public System.Net.IWebProxy Proxy { get { return default(System.Net.IWebProxy); } set { } }
        public virtual bool SupportsAutomaticDecompression { get { return default(bool); } }
        public virtual bool SupportsProxy { get { return default(bool); } }
        public virtual bool SupportsRedirectConfiguration { get { return default(bool); } }
        public bool UseCookies { get { return default(bool); } set { } }
        public bool UseDefaultCredentials { get { return default(bool); } set { } }
        public bool UseProxy { get { return default(bool); } set { } }
        protected override void Dispose(bool disposing) { }
        protected internal override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
    }
    public enum HttpCompletionOption
    {
        ResponseContentRead = 0,
        ResponseHeadersRead = 1,
    }
    public abstract partial class HttpContent : System.IDisposable
    {
        protected HttpContent() { }
        public System.Net.Http.Headers.HttpContentHeaders Headers { get { return default(System.Net.Http.Headers.HttpContentHeaders); } }
        public System.Threading.Tasks.Task CopyToAsync(System.IO.Stream stream) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task CopyToAsync(System.IO.Stream stream, System.Net.TransportContext context) { return default(System.Threading.Tasks.Task); }
        protected virtual System.Threading.Tasks.Task<System.IO.Stream> CreateContentReadStreamAsync() { return default(System.Threading.Tasks.Task<System.IO.Stream>); }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.Threading.Tasks.Task LoadIntoBufferAsync() { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task LoadIntoBufferAsync(long maxBufferSize) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task<byte[]> ReadAsByteArrayAsync() { return default(System.Threading.Tasks.Task<byte[]>); }
        public System.Threading.Tasks.Task<System.IO.Stream> ReadAsStreamAsync() { return default(System.Threading.Tasks.Task<System.IO.Stream>); }
        public System.Threading.Tasks.Task<string> ReadAsStringAsync() { return default(System.Threading.Tasks.Task<string>); }
        protected abstract System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext context);
        protected internal abstract bool TryComputeLength(out long length);
    }
    public abstract partial class HttpMessageHandler : System.IDisposable
    {
        protected HttpMessageHandler() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected internal abstract System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken);
    }
    public partial class HttpMessageInvoker : System.IDisposable
    {
        public HttpMessageInvoker(System.Net.Http.HttpMessageHandler handler) { }
        public HttpMessageInvoker(System.Net.Http.HttpMessageHandler handler, bool disposeHandler) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
    }
    public partial class HttpMethod : System.IEquatable<System.Net.Http.HttpMethod>
    {
        public HttpMethod(string method) { }
        public static System.Net.Http.HttpMethod Delete { get { return default(System.Net.Http.HttpMethod); } }
        public static System.Net.Http.HttpMethod Get { get { return default(System.Net.Http.HttpMethod); } }
        public static System.Net.Http.HttpMethod Head { get { return default(System.Net.Http.HttpMethod); } }
        public string Method { get { return default(string); } }
        public static System.Net.Http.HttpMethod Options { get { return default(System.Net.Http.HttpMethod); } }
        public static System.Net.Http.HttpMethod Post { get { return default(System.Net.Http.HttpMethod); } }
        public static System.Net.Http.HttpMethod Put { get { return default(System.Net.Http.HttpMethod); } }
        public static System.Net.Http.HttpMethod Trace { get { return default(System.Net.Http.HttpMethod); } }
        public bool Equals(System.Net.Http.HttpMethod other) { return default(bool); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Net.Http.HttpMethod left, System.Net.Http.HttpMethod right) { return default(bool); }
        public static bool operator !=(System.Net.Http.HttpMethod left, System.Net.Http.HttpMethod right) { return default(bool); }
        public override string ToString() { return default(string); }
    }
    public partial class HttpRequestException : System.Exception
    {
        public HttpRequestException() { }
        public HttpRequestException(string message) { }
        public HttpRequestException(string message, System.Exception inner) { }
    }
    public partial class HttpRequestMessage : System.IDisposable
    {
        public HttpRequestMessage() { }
        public HttpRequestMessage(System.Net.Http.HttpMethod method, string requestUri) { }
        public HttpRequestMessage(System.Net.Http.HttpMethod method, System.Uri requestUri) { }
        public System.Net.Http.HttpContent Content { get { return default(System.Net.Http.HttpContent); } set { } }
        public System.Net.Http.Headers.HttpRequestHeaders Headers { get { return default(System.Net.Http.Headers.HttpRequestHeaders); } }
        public System.Net.Http.HttpMethod Method { get { return default(System.Net.Http.HttpMethod); } set { } }
        public System.Collections.Generic.IDictionary<string, object> Properties { get { return default(System.Collections.Generic.IDictionary<string, object>); } }
        public System.Uri RequestUri { get { return default(System.Uri); } set { } }
        public System.Version Version { get { return default(System.Version); } set { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public override string ToString() { return default(string); }
    }
    public partial class HttpResponseMessage : System.IDisposable
    {
        public HttpResponseMessage() { }
        public HttpResponseMessage(System.Net.HttpStatusCode statusCode) { }
        public System.Net.Http.HttpContent Content { get { return default(System.Net.Http.HttpContent); } set { } }
        public System.Net.Http.Headers.HttpResponseHeaders Headers { get { return default(System.Net.Http.Headers.HttpResponseHeaders); } }
        public bool IsSuccessStatusCode { get { return default(bool); } }
        public string ReasonPhrase { get { return default(string); } set { } }
        public System.Net.Http.HttpRequestMessage RequestMessage { get { return default(System.Net.Http.HttpRequestMessage); } set { } }
        public System.Net.HttpStatusCode StatusCode { get { return default(System.Net.HttpStatusCode); } set { } }
        public System.Version Version { get { return default(System.Version); } set { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.Net.Http.HttpResponseMessage EnsureSuccessStatusCode() { return default(System.Net.Http.HttpResponseMessage); }
        public override string ToString() { return default(string); }
    }
    public abstract partial class MessageProcessingHandler : System.Net.Http.DelegatingHandler
    {
        protected MessageProcessingHandler() { }
        protected MessageProcessingHandler(System.Net.Http.HttpMessageHandler innerHandler) { }
        protected abstract System.Net.Http.HttpRequestMessage ProcessRequest(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken);
        protected abstract System.Net.Http.HttpResponseMessage ProcessResponse(System.Net.Http.HttpResponseMessage response, System.Threading.CancellationToken cancellationToken);
        protected internal sealed override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>); }
    }
    public partial class MultipartContent : System.Net.Http.HttpContent, System.Collections.Generic.IEnumerable<System.Net.Http.HttpContent>, System.Collections.IEnumerable
    {
        public MultipartContent() { }
        public MultipartContent(string subtype) { }
        public MultipartContent(string subtype, string boundary) { }
        public virtual void Add(System.Net.Http.HttpContent content) { }
        protected override void Dispose(bool disposing) { }
        public System.Collections.Generic.IEnumerator<System.Net.Http.HttpContent> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Net.Http.HttpContent>); }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext context) { return default(System.Threading.Tasks.Task); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        protected internal override bool TryComputeLength(out long length) { length = default(long); return default(bool); }
    }
    public partial class MultipartFormDataContent : System.Net.Http.MultipartContent
    {
        public MultipartFormDataContent() { }
        public MultipartFormDataContent(string boundary) { }
        public override void Add(System.Net.Http.HttpContent content) { }
        public void Add(System.Net.Http.HttpContent content, string name) { }
        public void Add(System.Net.Http.HttpContent content, string name, string fileName) { }
    }
    public partial class StreamContent : System.Net.Http.HttpContent
    {
        public StreamContent(System.IO.Stream content) { }
        public StreamContent(System.IO.Stream content, int bufferSize) { }
        protected override System.Threading.Tasks.Task<System.IO.Stream> CreateContentReadStreamAsync() { return default(System.Threading.Tasks.Task<System.IO.Stream>); }
        protected override void Dispose(bool disposing) { }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext context) { return default(System.Threading.Tasks.Task); }
        protected internal override bool TryComputeLength(out long length) { length = default(long); return default(bool); }
    }
    public partial class StringContent : System.Net.Http.ByteArrayContent
    {
        public StringContent(string content) : base(default(byte[])) { }
        public StringContent(string content, System.Text.Encoding encoding) : base(default(byte[])) { }
        public StringContent(string content, System.Text.Encoding encoding, string mediaType) : base(default(byte[])) { }
    }
}
namespace System.Net.Http.Headers
{
    public partial class AuthenticationHeaderValue
    {
        public AuthenticationHeaderValue(string scheme) { }
        public AuthenticationHeaderValue(string scheme, string parameter) { }
        public string Parameter { get { return default(string); } }
        public string Scheme { get { return default(string); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Net.Http.Headers.AuthenticationHeaderValue Parse(string input) { return default(System.Net.Http.Headers.AuthenticationHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.AuthenticationHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.AuthenticationHeaderValue); return default(bool); }
    }
    public partial class CacheControlHeaderValue
    {
        public CacheControlHeaderValue() { }
        public System.Collections.Generic.ICollection<System.Net.Http.Headers.NameValueHeaderValue> Extensions { get { return default(System.Collections.Generic.ICollection<System.Net.Http.Headers.NameValueHeaderValue>); } }
        public System.Nullable<System.TimeSpan> MaxAge { get { return default(System.Nullable<System.TimeSpan>); } set { } }
        public bool MaxStale { get { return default(bool); } set { } }
        public System.Nullable<System.TimeSpan> MaxStaleLimit { get { return default(System.Nullable<System.TimeSpan>); } set { } }
        public System.Nullable<System.TimeSpan> MinFresh { get { return default(System.Nullable<System.TimeSpan>); } set { } }
        public bool MustRevalidate { get { return default(bool); } set { } }
        public bool NoCache { get { return default(bool); } set { } }
        public System.Collections.Generic.ICollection<string> NoCacheHeaders { get { return default(System.Collections.Generic.ICollection<string>); } }
        public bool NoStore { get { return default(bool); } set { } }
        public bool NoTransform { get { return default(bool); } set { } }
        public bool OnlyIfCached { get { return default(bool); } set { } }
        public bool Private { get { return default(bool); } set { } }
        public System.Collections.Generic.ICollection<string> PrivateHeaders { get { return default(System.Collections.Generic.ICollection<string>); } }
        public bool ProxyRevalidate { get { return default(bool); } set { } }
        public bool Public { get { return default(bool); } set { } }
        public System.Nullable<System.TimeSpan> SharedMaxAge { get { return default(System.Nullable<System.TimeSpan>); } set { } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Net.Http.Headers.CacheControlHeaderValue Parse(string input) { return default(System.Net.Http.Headers.CacheControlHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.CacheControlHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.CacheControlHeaderValue); return default(bool); }
    }
    public partial class ContentDispositionHeaderValue
    {
        protected ContentDispositionHeaderValue(System.Net.Http.Headers.ContentDispositionHeaderValue source) { }
        public ContentDispositionHeaderValue(string dispositionType) { }
        public System.Nullable<System.DateTimeOffset> CreationDate { get { return default(System.Nullable<System.DateTimeOffset>); } set { } }
        public string DispositionType { get { return default(string); } set { } }
        public string FileName { get { return default(string); } set { } }
        public string FileNameStar { get { return default(string); } set { } }
        public System.Nullable<System.DateTimeOffset> ModificationDate { get { return default(System.Nullable<System.DateTimeOffset>); } set { } }
        public string Name { get { return default(string); } set { } }
        public System.Collections.Generic.ICollection<System.Net.Http.Headers.NameValueHeaderValue> Parameters { get { return default(System.Collections.Generic.ICollection<System.Net.Http.Headers.NameValueHeaderValue>); } }
        public System.Nullable<System.DateTimeOffset> ReadDate { get { return default(System.Nullable<System.DateTimeOffset>); } set { } }
        public System.Nullable<long> Size { get { return default(System.Nullable<long>); } set { } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Net.Http.Headers.ContentDispositionHeaderValue Parse(string input) { return default(System.Net.Http.Headers.ContentDispositionHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.ContentDispositionHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.ContentDispositionHeaderValue); return default(bool); }
    }
    public partial class ContentRangeHeaderValue
    {
        public ContentRangeHeaderValue(long length) { }
        public ContentRangeHeaderValue(long from, long to) { }
        public ContentRangeHeaderValue(long from, long to, long length) { }
        public System.Nullable<long> From { get { return default(System.Nullable<long>); } }
        public bool HasLength { get { return default(bool); } }
        public bool HasRange { get { return default(bool); } }
        public System.Nullable<long> Length { get { return default(System.Nullable<long>); } }
        public System.Nullable<long> To { get { return default(System.Nullable<long>); } }
        public string Unit { get { return default(string); } set { } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Net.Http.Headers.ContentRangeHeaderValue Parse(string input) { return default(System.Net.Http.Headers.ContentRangeHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.ContentRangeHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.ContentRangeHeaderValue); return default(bool); }
    }
    public partial class EntityTagHeaderValue
    {
        public EntityTagHeaderValue(string tag) { }
        public EntityTagHeaderValue(string tag, bool isWeak) { }
        public static System.Net.Http.Headers.EntityTagHeaderValue Any { get { return default(System.Net.Http.Headers.EntityTagHeaderValue); } }
        public bool IsWeak { get { return default(bool); } }
        public string Tag { get { return default(string); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Net.Http.Headers.EntityTagHeaderValue Parse(string input) { return default(System.Net.Http.Headers.EntityTagHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.EntityTagHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.EntityTagHeaderValue); return default(bool); }
    }
    public sealed partial class HttpContentHeaders : System.Net.Http.Headers.HttpHeaders
    {
        internal HttpContentHeaders() { }
        public System.Collections.Generic.ICollection<string> Allow { get { return default(System.Collections.Generic.ICollection<string>); } }
        public System.Net.Http.Headers.ContentDispositionHeaderValue ContentDisposition { get { return default(System.Net.Http.Headers.ContentDispositionHeaderValue); } set { } }
        public System.Collections.Generic.ICollection<string> ContentEncoding { get { return default(System.Collections.Generic.ICollection<string>); } }
        public System.Collections.Generic.ICollection<string> ContentLanguage { get { return default(System.Collections.Generic.ICollection<string>); } }
        public System.Nullable<long> ContentLength { get { return default(System.Nullable<long>); } set { } }
        public System.Uri ContentLocation { get { return default(System.Uri); } set { } }
        public byte[] ContentMD5 { get { return default(byte[]); } set { } }
        public System.Net.Http.Headers.ContentRangeHeaderValue ContentRange { get { return default(System.Net.Http.Headers.ContentRangeHeaderValue); } set { } }
        public System.Net.Http.Headers.MediaTypeHeaderValue ContentType { get { return default(System.Net.Http.Headers.MediaTypeHeaderValue); } set { } }
        public System.Nullable<System.DateTimeOffset> Expires { get { return default(System.Nullable<System.DateTimeOffset>); } set { } }
        public System.Nullable<System.DateTimeOffset> LastModified { get { return default(System.Nullable<System.DateTimeOffset>); } set { } }
    }
    public abstract partial class HttpHeaders : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<string>>>, System.Collections.IEnumerable
    {
        protected HttpHeaders() { }
        public void Add(string name, System.Collections.Generic.IEnumerable<string> values) { }
        public void Add(string name, string value) { }
        public void Clear() { }
        public bool Contains(string name) { return default(bool); }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<string>>> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<string>>>); }
        public System.Collections.Generic.IEnumerable<string> GetValues(string name) { return default(System.Collections.Generic.IEnumerable<string>); }
        public bool Remove(string name) { return default(bool); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public override string ToString() { return default(string); }
        public bool TryAddWithoutValidation(string name, System.Collections.Generic.IEnumerable<string> values) { return default(bool); }
        public bool TryAddWithoutValidation(string name, string value) { return default(bool); }
        public bool TryGetValues(string name, out System.Collections.Generic.IEnumerable<string> values) { values = default(System.Collections.Generic.IEnumerable<string>); return default(bool); }
    }
    public sealed partial class HttpHeaderValueCollection<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable where T : class
    {
        internal HttpHeaderValueCollection() { }
        public int Count { get { return default(int); } }
        public bool IsReadOnly { get { return default(bool); } }
        public void Add(T item) { }
        public void Clear() { }
        public bool Contains(T item) { return default(bool); }
        public void CopyTo(T[] array, int arrayIndex) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        public void ParseAdd(string input) { }
        public bool Remove(T item) { return default(bool); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public override string ToString() { return default(string); }
        public bool TryParseAdd(string input) { return default(bool); }
    }
    public sealed partial class HttpRequestHeaders : System.Net.Http.Headers.HttpHeaders
    {
        internal HttpRequestHeaders() { }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.MediaTypeWithQualityHeaderValue> Accept { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.MediaTypeWithQualityHeaderValue>); } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.StringWithQualityHeaderValue> AcceptCharset { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.StringWithQualityHeaderValue>); } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.StringWithQualityHeaderValue> AcceptEncoding { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.StringWithQualityHeaderValue>); } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.StringWithQualityHeaderValue> AcceptLanguage { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.StringWithQualityHeaderValue>); } }
        public System.Net.Http.Headers.AuthenticationHeaderValue Authorization { get { return default(System.Net.Http.Headers.AuthenticationHeaderValue); } set { } }
        public System.Net.Http.Headers.CacheControlHeaderValue CacheControl { get { return default(System.Net.Http.Headers.CacheControlHeaderValue); } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<string> Connection { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<string>); } }
        public System.Nullable<bool> ConnectionClose { get { return default(System.Nullable<bool>); } set { } }
        public System.Nullable<System.DateTimeOffset> Date { get { return default(System.Nullable<System.DateTimeOffset>); } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.NameValueWithParametersHeaderValue> Expect { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.NameValueWithParametersHeaderValue>); } }
        public System.Nullable<bool> ExpectContinue { get { return default(System.Nullable<bool>); } set { } }
        public string From { get { return default(string); } set { } }
        public string Host { get { return default(string); } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.EntityTagHeaderValue> IfMatch { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.EntityTagHeaderValue>); } }
        public System.Nullable<System.DateTimeOffset> IfModifiedSince { get { return default(System.Nullable<System.DateTimeOffset>); } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.EntityTagHeaderValue> IfNoneMatch { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.EntityTagHeaderValue>); } }
        public System.Net.Http.Headers.RangeConditionHeaderValue IfRange { get { return default(System.Net.Http.Headers.RangeConditionHeaderValue); } set { } }
        public System.Nullable<System.DateTimeOffset> IfUnmodifiedSince { get { return default(System.Nullable<System.DateTimeOffset>); } set { } }
        public System.Nullable<int> MaxForwards { get { return default(System.Nullable<int>); } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.NameValueHeaderValue> Pragma { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.NameValueHeaderValue>); } }
        public System.Net.Http.Headers.AuthenticationHeaderValue ProxyAuthorization { get { return default(System.Net.Http.Headers.AuthenticationHeaderValue); } set { } }
        public System.Net.Http.Headers.RangeHeaderValue Range { get { return default(System.Net.Http.Headers.RangeHeaderValue); } set { } }
        public System.Uri Referrer { get { return default(System.Uri); } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.TransferCodingWithQualityHeaderValue> TE { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.TransferCodingWithQualityHeaderValue>); } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<string> Trailer { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<string>); } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.TransferCodingHeaderValue> TransferEncoding { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.TransferCodingHeaderValue>); } }
        public System.Nullable<bool> TransferEncodingChunked { get { return default(System.Nullable<bool>); } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ProductHeaderValue> Upgrade { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ProductHeaderValue>); } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ProductInfoHeaderValue> UserAgent { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ProductInfoHeaderValue>); } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ViaHeaderValue> Via { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ViaHeaderValue>); } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.WarningHeaderValue> Warning { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.WarningHeaderValue>); } }
    }
    public sealed partial class HttpResponseHeaders : System.Net.Http.Headers.HttpHeaders
    {
        internal HttpResponseHeaders() { }
        public System.Net.Http.Headers.HttpHeaderValueCollection<string> AcceptRanges { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<string>); } }
        public System.Nullable<System.TimeSpan> Age { get { return default(System.Nullable<System.TimeSpan>); } set { } }
        public System.Net.Http.Headers.CacheControlHeaderValue CacheControl { get { return default(System.Net.Http.Headers.CacheControlHeaderValue); } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<string> Connection { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<string>); } }
        public System.Nullable<bool> ConnectionClose { get { return default(System.Nullable<bool>); } set { } }
        public System.Nullable<System.DateTimeOffset> Date { get { return default(System.Nullable<System.DateTimeOffset>); } set { } }
        public System.Net.Http.Headers.EntityTagHeaderValue ETag { get { return default(System.Net.Http.Headers.EntityTagHeaderValue); } set { } }
        public System.Uri Location { get { return default(System.Uri); } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.NameValueHeaderValue> Pragma { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.NameValueHeaderValue>); } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.AuthenticationHeaderValue> ProxyAuthenticate { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.AuthenticationHeaderValue>); } }
        public System.Net.Http.Headers.RetryConditionHeaderValue RetryAfter { get { return default(System.Net.Http.Headers.RetryConditionHeaderValue); } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ProductInfoHeaderValue> Server { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ProductInfoHeaderValue>); } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<string> Trailer { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<string>); } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.TransferCodingHeaderValue> TransferEncoding { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.TransferCodingHeaderValue>); } }
        public System.Nullable<bool> TransferEncodingChunked { get { return default(System.Nullable<bool>); } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ProductHeaderValue> Upgrade { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ProductHeaderValue>); } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<string> Vary { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<string>); } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ViaHeaderValue> Via { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ViaHeaderValue>); } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.WarningHeaderValue> Warning { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.WarningHeaderValue>); } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.AuthenticationHeaderValue> WwwAuthenticate { get { return default(System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.AuthenticationHeaderValue>); } }
    }
    public partial class MediaTypeHeaderValue
    {
        protected MediaTypeHeaderValue(System.Net.Http.Headers.MediaTypeHeaderValue source) { }
        public MediaTypeHeaderValue(string mediaType) { }
        public string CharSet { get { return default(string); } set { } }
        public string MediaType { get { return default(string); } set { } }
        public System.Collections.Generic.ICollection<System.Net.Http.Headers.NameValueHeaderValue> Parameters { get { return default(System.Collections.Generic.ICollection<System.Net.Http.Headers.NameValueHeaderValue>); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Net.Http.Headers.MediaTypeHeaderValue Parse(string input) { return default(System.Net.Http.Headers.MediaTypeHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.MediaTypeHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.MediaTypeHeaderValue); return default(bool); }
    }
    public sealed partial class MediaTypeWithQualityHeaderValue : System.Net.Http.Headers.MediaTypeHeaderValue
    {
        public MediaTypeWithQualityHeaderValue(string mediaType) : base(default(System.Net.Http.Headers.MediaTypeHeaderValue)) { }
        public MediaTypeWithQualityHeaderValue(string mediaType, double quality) : base(default(System.Net.Http.Headers.MediaTypeHeaderValue)) { }
        public System.Nullable<double> Quality { get { return default(System.Nullable<double>); } set { } }
        public static new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue Parse(string input) { return default(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue); }
        public static bool TryParse(string input, out System.Net.Http.Headers.MediaTypeWithQualityHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue); return default(bool); }
    }
    public partial class NameValueHeaderValue
    {
        protected NameValueHeaderValue(System.Net.Http.Headers.NameValueHeaderValue source) { }
        public NameValueHeaderValue(string name) { }
        public NameValueHeaderValue(string name, string value) { }
        public string Name { get { return default(string); } }
        public string Value { get { return default(string); } set { } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Net.Http.Headers.NameValueHeaderValue Parse(string input) { return default(System.Net.Http.Headers.NameValueHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.NameValueHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.NameValueHeaderValue); return default(bool); }
    }
    public partial class NameValueWithParametersHeaderValue : System.Net.Http.Headers.NameValueHeaderValue
    {
        protected NameValueWithParametersHeaderValue(System.Net.Http.Headers.NameValueWithParametersHeaderValue source) : base(default(string)) { }
        public NameValueWithParametersHeaderValue(string name) : base(default(string)) { }
        public NameValueWithParametersHeaderValue(string name, string value) : base(default(string)) { }
        public System.Collections.Generic.ICollection<System.Net.Http.Headers.NameValueHeaderValue> Parameters { get { return default(System.Collections.Generic.ICollection<System.Net.Http.Headers.NameValueHeaderValue>); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static new System.Net.Http.Headers.NameValueWithParametersHeaderValue Parse(string input) { return default(System.Net.Http.Headers.NameValueWithParametersHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.NameValueWithParametersHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.NameValueWithParametersHeaderValue); return default(bool); }
    }
    public partial class ProductHeaderValue
    {
        public ProductHeaderValue(string name) { }
        public ProductHeaderValue(string name, string version) { }
        public string Name { get { return default(string); } }
        public string Version { get { return default(string); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Net.Http.Headers.ProductHeaderValue Parse(string input) { return default(System.Net.Http.Headers.ProductHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.ProductHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.ProductHeaderValue); return default(bool); }
    }
    public partial class ProductInfoHeaderValue
    {
        public ProductInfoHeaderValue(System.Net.Http.Headers.ProductHeaderValue product) { }
        public ProductInfoHeaderValue(string comment) { }
        public ProductInfoHeaderValue(string productName, string productVersion) { }
        public string Comment { get { return default(string); } }
        public System.Net.Http.Headers.ProductHeaderValue Product { get { return default(System.Net.Http.Headers.ProductHeaderValue); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Net.Http.Headers.ProductInfoHeaderValue Parse(string input) { return default(System.Net.Http.Headers.ProductInfoHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.ProductInfoHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.ProductInfoHeaderValue); return default(bool); }
    }
    public partial class RangeConditionHeaderValue
    {
        public RangeConditionHeaderValue(System.DateTimeOffset date) { }
        public RangeConditionHeaderValue(System.Net.Http.Headers.EntityTagHeaderValue entityTag) { }
        public RangeConditionHeaderValue(string entityTag) { }
        public System.Nullable<System.DateTimeOffset> Date { get { return default(System.Nullable<System.DateTimeOffset>); } }
        public System.Net.Http.Headers.EntityTagHeaderValue EntityTag { get { return default(System.Net.Http.Headers.EntityTagHeaderValue); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Net.Http.Headers.RangeConditionHeaderValue Parse(string input) { return default(System.Net.Http.Headers.RangeConditionHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.RangeConditionHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.RangeConditionHeaderValue); return default(bool); }
    }
    public partial class RangeHeaderValue
    {
        public RangeHeaderValue() { }
        public RangeHeaderValue(System.Nullable<long> from, System.Nullable<long> to) { }
        public System.Collections.Generic.ICollection<System.Net.Http.Headers.RangeItemHeaderValue> Ranges { get { return default(System.Collections.Generic.ICollection<System.Net.Http.Headers.RangeItemHeaderValue>); } }
        public string Unit { get { return default(string); } set { } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Net.Http.Headers.RangeHeaderValue Parse(string input) { return default(System.Net.Http.Headers.RangeHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.RangeHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.RangeHeaderValue); return default(bool); }
    }
    public partial class RangeItemHeaderValue
    {
        public RangeItemHeaderValue(System.Nullable<long> from, System.Nullable<long> to) { }
        public System.Nullable<long> From { get { return default(System.Nullable<long>); } }
        public System.Nullable<long> To { get { return default(System.Nullable<long>); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public override string ToString() { return default(string); }
    }
    public partial class RetryConditionHeaderValue
    {
        public RetryConditionHeaderValue(System.DateTimeOffset date) { }
        public RetryConditionHeaderValue(System.TimeSpan delta) { }
        public System.Nullable<System.DateTimeOffset> Date { get { return default(System.Nullable<System.DateTimeOffset>); } }
        public System.Nullable<System.TimeSpan> Delta { get { return default(System.Nullable<System.TimeSpan>); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Net.Http.Headers.RetryConditionHeaderValue Parse(string input) { return default(System.Net.Http.Headers.RetryConditionHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.RetryConditionHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.RetryConditionHeaderValue); return default(bool); }
    }
    public partial class StringWithQualityHeaderValue
    {
        public StringWithQualityHeaderValue(string value) { }
        public StringWithQualityHeaderValue(string value, double quality) { }
        public System.Nullable<double> Quality { get { return default(System.Nullable<double>); } }
        public string Value { get { return default(string); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Net.Http.Headers.StringWithQualityHeaderValue Parse(string input) { return default(System.Net.Http.Headers.StringWithQualityHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.StringWithQualityHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.StringWithQualityHeaderValue); return default(bool); }
    }
    public partial class TransferCodingHeaderValue
    {
        protected TransferCodingHeaderValue(System.Net.Http.Headers.TransferCodingHeaderValue source) { }
        public TransferCodingHeaderValue(string value) { }
        public System.Collections.Generic.ICollection<System.Net.Http.Headers.NameValueHeaderValue> Parameters { get { return default(System.Collections.Generic.ICollection<System.Net.Http.Headers.NameValueHeaderValue>); } }
        public string Value { get { return default(string); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Net.Http.Headers.TransferCodingHeaderValue Parse(string input) { return default(System.Net.Http.Headers.TransferCodingHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.TransferCodingHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.TransferCodingHeaderValue); return default(bool); }
    }
    public sealed partial class TransferCodingWithQualityHeaderValue : System.Net.Http.Headers.TransferCodingHeaderValue
    {
        public TransferCodingWithQualityHeaderValue(string value) : base(default(System.Net.Http.Headers.TransferCodingHeaderValue)) { }
        public TransferCodingWithQualityHeaderValue(string value, double quality) : base(default(System.Net.Http.Headers.TransferCodingHeaderValue)) { }
        public System.Nullable<double> Quality { get { return default(System.Nullable<double>); } set { } }
        public static new System.Net.Http.Headers.TransferCodingWithQualityHeaderValue Parse(string input) { return default(System.Net.Http.Headers.TransferCodingWithQualityHeaderValue); }
        public static bool TryParse(string input, out System.Net.Http.Headers.TransferCodingWithQualityHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.TransferCodingWithQualityHeaderValue); return default(bool); }
    }
    public partial class ViaHeaderValue
    {
        public ViaHeaderValue(string protocolVersion, string receivedBy) { }
        public ViaHeaderValue(string protocolVersion, string receivedBy, string protocolName) { }
        public ViaHeaderValue(string protocolVersion, string receivedBy, string protocolName, string comment) { }
        public string Comment { get { return default(string); } }
        public string ProtocolName { get { return default(string); } }
        public string ProtocolVersion { get { return default(string); } }
        public string ReceivedBy { get { return default(string); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Net.Http.Headers.ViaHeaderValue Parse(string input) { return default(System.Net.Http.Headers.ViaHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.ViaHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.ViaHeaderValue); return default(bool); }
    }
    public partial class WarningHeaderValue
    {
        public WarningHeaderValue(int code, string agent, string text) { }
        public WarningHeaderValue(int code, string agent, string text, System.DateTimeOffset date) { }
        public string Agent { get { return default(string); } }
        public int Code { get { return default(int); } }
        public System.Nullable<System.DateTimeOffset> Date { get { return default(System.Nullable<System.DateTimeOffset>); } }
        public string Text { get { return default(string); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Net.Http.Headers.WarningHeaderValue Parse(string input) { return default(System.Net.Http.Headers.WarningHeaderValue); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string input, out System.Net.Http.Headers.WarningHeaderValue parsedValue) { parsedValue = default(System.Net.Http.Headers.WarningHeaderValue); return default(bool); }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    public partial class ByteArrayContent : System.Net.Http.HttpContent
    {
        public ByteArrayContent(byte[] content) { }
        public ByteArrayContent(byte[] content, int offset, int count) { }
        protected override System.IO.Stream CreateContentReadStream(System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override System.Threading.Tasks.Task<System.IO.Stream> CreateContentReadStreamAsync() { throw null; }
        protected override void SerializeToStream(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context) { throw null; }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected internal override bool TryComputeLength(out long length) { throw null; }
    }
    public enum ClientCertificateOption
    {
        Manual = 0,
        Automatic = 1,
    }
    public abstract partial class DelegatingHandler : System.Net.Http.HttpMessageHandler
    {
        protected DelegatingHandler() { }
        protected DelegatingHandler(System.Net.Http.HttpMessageHandler innerHandler) { }
        [System.Diagnostics.CodeAnalysis.DisallowNullAttribute]
        public System.Net.Http.HttpMessageHandler? InnerHandler { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected internal override System.Net.Http.HttpResponseMessage Send(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected internal override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class FormUrlEncodedContent : System.Net.Http.ByteArrayContent
    {
        public FormUrlEncodedContent(
            System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<
                #nullable disable
                string, string
                #nullable restore
            >> nameValueCollection) : base (default(byte[])) { }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public delegate System.Text.Encoding? HeaderEncodingSelector<TContext>(string headerName, TContext context);
    public partial class HttpClient : System.Net.Http.HttpMessageInvoker
    {
        public HttpClient() : base (default(System.Net.Http.HttpMessageHandler)) { }
        public HttpClient(System.Net.Http.HttpMessageHandler handler) : base (default(System.Net.Http.HttpMessageHandler)) { }
        public HttpClient(System.Net.Http.HttpMessageHandler handler, bool disposeHandler) : base (default(System.Net.Http.HttpMessageHandler)) { }
        public System.Uri? BaseAddress { get { throw null; } set { } }
        public static System.Net.IWebProxy DefaultProxy { get { throw null; } set { } }
        public System.Net.Http.Headers.HttpRequestHeaders DefaultRequestHeaders { get { throw null; } }
        public System.Version DefaultRequestVersion { get { throw null; } set { } }
        public System.Net.Http.HttpVersionPolicy DefaultVersionPolicy { get { throw null; } set { } }
        public long MaxResponseContentBufferSize { get { throw null; } set { } }
        public System.TimeSpan Timeout { get { throw null; } set { } }
        public void CancelPendingRequests() { }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> DeleteAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> DeleteAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> DeleteAsync(System.Uri? requestUri) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> DeleteAsync(System.Uri? requestUri, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override void Dispose(bool disposing) { }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri, System.Net.Http.HttpCompletionOption completionOption) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri, System.Net.Http.HttpCompletionOption completionOption, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync(System.Uri? requestUri) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync(System.Uri? requestUri, System.Net.Http.HttpCompletionOption completionOption) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync(System.Uri? requestUri, System.Net.Http.HttpCompletionOption completionOption, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> GetAsync(System.Uri? requestUri, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<byte[]> GetByteArrayAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri) { throw null; }
        public System.Threading.Tasks.Task<byte[]> GetByteArrayAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<byte[]> GetByteArrayAsync(System.Uri? requestUri) { throw null; }
        public System.Threading.Tasks.Task<byte[]> GetByteArrayAsync(System.Uri? requestUri, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<System.IO.Stream> GetStreamAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri) { throw null; }
        public System.Threading.Tasks.Task<System.IO.Stream> GetStreamAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<System.IO.Stream> GetStreamAsync(System.Uri? requestUri) { throw null; }
        public System.Threading.Tasks.Task<System.IO.Stream> GetStreamAsync(System.Uri? requestUri, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<string> GetStringAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri) { throw null; }
        public System.Threading.Tasks.Task<string> GetStringAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<string> GetStringAsync(System.Uri? requestUri) { throw null; }
        public System.Threading.Tasks.Task<string> GetStringAsync(System.Uri? requestUri, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PatchAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri, System.Net.Http.HttpContent? content) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PatchAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri, System.Net.Http.HttpContent? content, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PatchAsync(System.Uri? requestUri, System.Net.Http.HttpContent? content) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PatchAsync(System.Uri? requestUri, System.Net.Http.HttpContent? content, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PostAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri, System.Net.Http.HttpContent? content) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PostAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri, System.Net.Http.HttpContent? content, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PostAsync(System.Uri? requestUri, System.Net.Http.HttpContent? content) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PostAsync(System.Uri? requestUri, System.Net.Http.HttpContent? content, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PutAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri, System.Net.Http.HttpContent? content) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PutAsync([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri, System.Net.Http.HttpContent? content, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PutAsync(System.Uri? requestUri, System.Net.Http.HttpContent? content) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PutAsync(System.Uri? requestUri, System.Net.Http.HttpContent? content, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public System.Net.Http.HttpResponseMessage Send(System.Net.Http.HttpRequestMessage request) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public System.Net.Http.HttpResponseMessage Send(System.Net.Http.HttpRequestMessage request, System.Net.Http.HttpCompletionOption completionOption) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public System.Net.Http.HttpResponseMessage Send(System.Net.Http.HttpRequestMessage request, System.Net.Http.HttpCompletionOption completionOption, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public override System.Net.Http.HttpResponseMessage Send(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Net.Http.HttpCompletionOption completionOption) { throw null; }
        public System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Net.Http.HttpCompletionOption completionOption, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class HttpClientHandler : System.Net.Http.HttpMessageHandler
    {
        public HttpClientHandler() { }
        public bool AllowAutoRedirect { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public System.Net.DecompressionMethods AutomaticDecompression { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public bool CheckCertificateRevocationList { get { throw null; } set { } }
        public System.Net.Http.ClientCertificateOption ClientCertificateOptions { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates { get { throw null; } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public System.Net.CookieContainer CookieContainer { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public System.Net.ICredentials? Credentials { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Func<System.Net.Http.HttpRequestMessage, System.Security.Cryptography.X509Certificates.X509Certificate2?, System.Security.Cryptography.X509Certificates.X509Chain?, System.Net.Security.SslPolicyErrors, bool> DangerousAcceptAnyServerCertificateValidator { get { throw null; } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public System.Net.ICredentials? DefaultProxyCredentials { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public int MaxAutomaticRedirections { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public int MaxConnectionsPerServer { get { throw null; } set { } }
        public long MaxRequestContentBufferSize { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public int MaxResponseHeadersLength { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public bool PreAuthenticate { get { throw null; } set { } }
        public System.Collections.Generic.IDictionary<string, object?> Properties { get { throw null; } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public System.Net.IWebProxy? Proxy { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public System.Func<System.Net.Http.HttpRequestMessage, System.Security.Cryptography.X509Certificates.X509Certificate2?, System.Security.Cryptography.X509Certificates.X509Chain?, System.Net.Security.SslPolicyErrors, bool>? ServerCertificateCustomValidationCallback { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public System.Security.Authentication.SslProtocols SslProtocols { get { throw null; } set { } }
        public virtual bool SupportsAutomaticDecompression { get { throw null; } }
        public virtual bool SupportsProxy { get { throw null; } }
        public virtual bool SupportsRedirectConfiguration { get { throw null; } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public bool UseCookies { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public bool UseDefaultCredentials { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public bool UseProxy { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        //
        // Attributes are commented out due to https://github.com/dotnet/arcade/issues/7585
        // API compat will fail until this is fixed
        //
        //[System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        //[System.Runtime.Versioning.UnsupportedOSPlatformAttributeUnsupportedOSPlatform("ios")]
        //[System.Runtime.Versioning.UnsupportedOSPlatformAttributeUnsupportedOSPlatform("tvos")]
        protected internal override System.Net.Http.HttpResponseMessage Send(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected internal override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public enum HttpCompletionOption
    {
        ResponseContentRead = 0,
        ResponseHeadersRead = 1,
    }
    public abstract partial class HttpContent : System.IDisposable
    {
        protected HttpContent() { }
        public System.Net.Http.Headers.HttpContentHeaders Headers { get { throw null; } }
        public void CopyTo(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { }
        public System.Threading.Tasks.Task CopyToAsync(System.IO.Stream stream) { throw null; }
        public System.Threading.Tasks.Task CopyToAsync(System.IO.Stream stream, System.Net.TransportContext? context) { throw null; }
        public System.Threading.Tasks.Task CopyToAsync(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task CopyToAsync(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected virtual System.IO.Stream CreateContentReadStream(System.Threading.CancellationToken cancellationToken) { throw null; }
        protected virtual System.Threading.Tasks.Task<System.IO.Stream> CreateContentReadStreamAsync() { throw null; }
        protected virtual System.Threading.Tasks.Task<System.IO.Stream> CreateContentReadStreamAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.Threading.Tasks.Task LoadIntoBufferAsync() { throw null; }
        public System.Threading.Tasks.Task LoadIntoBufferAsync(long maxBufferSize) { throw null; }
        public System.Threading.Tasks.Task<byte[]> ReadAsByteArrayAsync() { throw null; }
        public System.Threading.Tasks.Task<byte[]> ReadAsByteArrayAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.IO.Stream ReadAsStream() { throw null; }
        public System.IO.Stream ReadAsStream(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<System.IO.Stream> ReadAsStreamAsync() { throw null; }
        public System.Threading.Tasks.Task<System.IO.Stream> ReadAsStreamAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<string> ReadAsStringAsync() { throw null; }
        public System.Threading.Tasks.Task<string> ReadAsStringAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        protected virtual void SerializeToStream(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { }
        protected abstract System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context);
        protected virtual System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected internal abstract bool TryComputeLength(out long length);
    }
    public abstract partial class HttpMessageHandler : System.IDisposable
    {
        protected HttpMessageHandler() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected internal virtual System.Net.Http.HttpResponseMessage Send(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected internal abstract System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken);
    }
    public partial class HttpMessageInvoker : System.IDisposable
    {
        public HttpMessageInvoker(System.Net.Http.HttpMessageHandler handler) { }
        public HttpMessageInvoker(System.Net.Http.HttpMessageHandler handler, bool disposeHandler) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public virtual System.Net.Http.HttpResponseMessage Send(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class HttpMethod : System.IEquatable<System.Net.Http.HttpMethod>
    {
        public HttpMethod(string method) { }
        public static System.Net.Http.HttpMethod Delete { get { throw null; } }
        public static System.Net.Http.HttpMethod Get { get { throw null; } }
        public static System.Net.Http.HttpMethod Head { get { throw null; } }
        public string Method { get { throw null; } }
        public static System.Net.Http.HttpMethod Options { get { throw null; } }
        public static System.Net.Http.HttpMethod Patch { get { throw null; } }
        public static System.Net.Http.HttpMethod Post { get { throw null; } }
        public static System.Net.Http.HttpMethod Put { get { throw null; } }
        public static System.Net.Http.HttpMethod Trace { get { throw null; } }
        public bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] System.Net.Http.HttpMethod? other) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Net.Http.HttpMethod? left, System.Net.Http.HttpMethod? right) { throw null; }
        public static bool operator !=(System.Net.Http.HttpMethod? left, System.Net.Http.HttpMethod? right) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class HttpRequestException : System.Exception
    {
        public HttpRequestException() { }
        public HttpRequestException(string? message) { }
        public HttpRequestException(string? message, System.Exception? inner) { }
        public HttpRequestException(string? message, System.Exception? inner, System.Net.HttpStatusCode? statusCode) { }
        public System.Net.HttpStatusCode? StatusCode { get { throw null; } }
    }
    public partial class HttpRequestMessage : System.IDisposable
    {
        public HttpRequestMessage() { }
        public HttpRequestMessage(System.Net.Http.HttpMethod method, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string? requestUri) { }
        public HttpRequestMessage(System.Net.Http.HttpMethod method, System.Uri? requestUri) { }
        public System.Net.Http.HttpContent? Content { get { throw null; } set { } }
        public System.Net.Http.Headers.HttpRequestHeaders Headers { get { throw null; } }
        public System.Net.Http.HttpMethod Method { get { throw null; } set { } }
        [System.ObsoleteAttribute("HttpRequestMessage.Properties has been deprecated. Use Options instead.")]
        public System.Collections.Generic.IDictionary<string, object?> Properties { get { throw null; } }
        public HttpRequestOptions Options { get { throw null; } }
        public System.Uri? RequestUri { get { throw null; } set { } }
        public System.Version Version { get { throw null; } set { } }
        public System.Net.Http.HttpVersionPolicy VersionPolicy { get { throw null; } set { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public override string ToString() { throw null; }
    }

    public readonly struct HttpRequestOptionsKey<TValue>
    {
        public HttpRequestOptionsKey(string key) {}
        public string Key { get { throw null; } }
    }

    public sealed class HttpRequestOptions : System.Collections.Generic.IDictionary<string, object?>
    {
        void System.Collections.Generic.IDictionary<string, object?>.Add(string key, object? value) { throw null; }
        System.Collections.Generic.ICollection<string> System.Collections.Generic.IDictionary<string, object?>.Keys { get { throw null; } }
        System.Collections.Generic.ICollection<object?> System.Collections.Generic.IDictionary<string, object?>.Values { get { throw null; } }
        bool System.Collections.Generic.IDictionary<string, object?>.Remove(string key) { throw null; }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object?>>.Remove(System.Collections.Generic.KeyValuePair<string, object?> item) { throw null; }
        bool System.Collections.Generic.IDictionary<string, object?>.TryGetValue(string key, out object? value) { throw null; }
        object? System.Collections.Generic.IDictionary<string, object?>.this[string key] { get { throw null; } set { } }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object?>>.Add(System.Collections.Generic.KeyValuePair<string, object?> item) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object?>>.Clear() { throw null; }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object?>>.Contains(System.Collections.Generic.KeyValuePair<string, object?> item) { throw null; }
        bool System.Collections.Generic.IDictionary<string, object?>.ContainsKey(string key) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object?>>.CopyTo(System.Collections.Generic.KeyValuePair<string, object?>[] array, int arrayIndex) { throw null; }
        int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object?>>.Count { get { throw null; } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object?>>.IsReadOnly { get { throw null; } }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object?>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValue<TValue>(HttpRequestOptionsKey<TValue> key, [MaybeNullWhen(false)] out TValue value) { throw null; }
        public void Set<TValue>(HttpRequestOptionsKey<TValue> key, TValue value) { throw null; }
    }

    public partial class HttpResponseMessage : System.IDisposable
    {
        public HttpResponseMessage() { }
        public HttpResponseMessage(System.Net.HttpStatusCode statusCode) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public System.Net.Http.HttpContent Content { get { throw null; } set { } }
        public System.Net.Http.Headers.HttpResponseHeaders Headers { get { throw null; } }
        public bool IsSuccessStatusCode { get { throw null; } }
        public string? ReasonPhrase { get { throw null; } set { } }
        public System.Net.Http.HttpRequestMessage? RequestMessage { get { throw null; } set { } }
        public System.Net.HttpStatusCode StatusCode { get { throw null; } set { } }
        public System.Net.Http.Headers.HttpResponseHeaders TrailingHeaders { get { throw null; } }
        public System.Version Version { get { throw null; } set { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.Net.Http.HttpResponseMessage EnsureSuccessStatusCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public enum HttpVersionPolicy
    {
        RequestVersionOrLower = 0,
        RequestVersionOrHigher = 1,
        RequestVersionExact = 2,
    }
    public abstract partial class MessageProcessingHandler : System.Net.Http.DelegatingHandler
    {
        protected MessageProcessingHandler() { }
        protected MessageProcessingHandler(System.Net.Http.HttpMessageHandler innerHandler) { }
        protected abstract System.Net.Http.HttpRequestMessage ProcessRequest(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken);
        protected abstract System.Net.Http.HttpResponseMessage ProcessResponse(System.Net.Http.HttpResponseMessage response, System.Threading.CancellationToken cancellationToken);
        protected internal sealed override System.Net.Http.HttpResponseMessage Send(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected internal sealed override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class MultipartContent : System.Net.Http.HttpContent, System.Collections.Generic.IEnumerable<System.Net.Http.HttpContent>, System.Collections.IEnumerable
    {
        public MultipartContent() { }
        public MultipartContent(string subtype) { }
        public MultipartContent(string subtype, string boundary) { }
        public System.Net.Http.HeaderEncodingSelector<System.Net.Http.HttpContent>? HeaderEncodingSelector { get { throw null; } set { } }
        public virtual void Add(System.Net.Http.HttpContent content) { }
        protected override System.IO.Stream CreateContentReadStream(System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override System.Threading.Tasks.Task<System.IO.Stream> CreateContentReadStreamAsync() { throw null; }
        protected override System.Threading.Tasks.Task<System.IO.Stream> CreateContentReadStreamAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override void Dispose(bool disposing) { }
        public System.Collections.Generic.IEnumerator<System.Net.Http.HttpContent> GetEnumerator() { throw null; }
        protected override void SerializeToStream(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context) { throw null; }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        protected internal override bool TryComputeLength(out long length) { throw null; }
    }
    public partial class MultipartFormDataContent : System.Net.Http.MultipartContent
    {
        public MultipartFormDataContent() { }
        public MultipartFormDataContent(string boundary) { }
        public override void Add(System.Net.Http.HttpContent content) { }
        public void Add(System.Net.Http.HttpContent content, string name) { }
        public void Add(System.Net.Http.HttpContent content, string name, string fileName) { }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public sealed partial class ReadOnlyMemoryContent : System.Net.Http.HttpContent
    {
        public ReadOnlyMemoryContent(System.ReadOnlyMemory<byte> content) { }
        protected override System.IO.Stream CreateContentReadStream(System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override System.Threading.Tasks.Task<System.IO.Stream> CreateContentReadStreamAsync() { throw null; }
        protected override void SerializeToStream(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context) { throw null; }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected internal override bool TryComputeLength(out long length) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public sealed partial class SocketsHttpHandler : System.Net.Http.HttpMessageHandler
    {
        public SocketsHttpHandler() { }
        public int InitialHttp2StreamWindowSize { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformGuardAttribute("browser")]
        public static bool IsSupported { get { throw null; } }
        public bool AllowAutoRedirect { get { throw null; } set { } }
        public System.Net.DecompressionMethods AutomaticDecompression { get { throw null; } set { } }
        public System.TimeSpan ConnectTimeout { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public System.Net.CookieContainer CookieContainer { get { throw null; } set { } }
        public System.Net.ICredentials? Credentials { get { throw null; } set { } }
        public System.Net.ICredentials? DefaultProxyCredentials { get { throw null; } set { } }
        public System.TimeSpan Expect100ContinueTimeout { get { throw null; } set { } }
        public System.TimeSpan KeepAlivePingDelay { get { throw null; } set { } }
        public System.TimeSpan KeepAlivePingTimeout { get { throw null; } set { } }
        public HttpKeepAlivePingPolicy KeepAlivePingPolicy { get { throw null; } set { } }
        public int MaxAutomaticRedirections { get { throw null; } set { } }
        public int MaxConnectionsPerServer { get { throw null; } set { } }
        public int MaxResponseDrainSize { get { throw null; } set { } }
        public int MaxResponseHeadersLength { get { throw null; } set { } }
        public System.TimeSpan PooledConnectionIdleTimeout { get { throw null; } set { } }
        public System.TimeSpan PooledConnectionLifetime { get { throw null; } set { } }
        public bool PreAuthenticate { get { throw null; } set { } }
        public System.Collections.Generic.IDictionary<string, object?> Properties { get { throw null; } }
        public System.Net.IWebProxy? Proxy { get { throw null; } set { } }
        public System.Net.Http.HeaderEncodingSelector<System.Net.Http.HttpRequestMessage>? RequestHeaderEncodingSelector { get { throw null; } set { } }
        public System.TimeSpan ResponseDrainTimeout { get { throw null; } set { } }
        public System.Net.Http.HeaderEncodingSelector<System.Net.Http.HttpRequestMessage>? ResponseHeaderEncodingSelector { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public System.Net.Security.SslClientAuthenticationOptions SslOptions { get { throw null; } set { } }
        public bool UseCookies { get { throw null; } set { } }
        public bool UseProxy { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected internal override System.Net.Http.HttpResponseMessage Send(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected internal override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
        public bool EnableMultipleHttp2Connections { get { throw null; } set { } }
        public Func<SocketsHttpConnectionContext, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.IO.Stream>>? ConnectCallback { get { throw null; } set { } }
        public Func<SocketsHttpPlaintextStreamFilterContext, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.IO.Stream>>? PlaintextStreamFilter { get { throw null; } set { } }
        [System.CLSCompliantAttribute(false)]
        public System.Diagnostics.DistributedContextPropagator? ActivityHeadersPropagator { get { throw null; } set { } }
    }
    public sealed class SocketsHttpConnectionContext
    {
        internal SocketsHttpConnectionContext() { }
        public DnsEndPoint DnsEndPoint { get { throw null; } }
        public HttpRequestMessage InitialRequestMessage { get { throw null; } }
    }
    public sealed class SocketsHttpPlaintextStreamFilterContext
    {
        internal SocketsHttpPlaintextStreamFilterContext() { }
        public System.IO.Stream PlaintextStream { get { throw null; } }
        public Version NegotiatedHttpVersion { get { throw null; } }
        public HttpRequestMessage InitialRequestMessage { get { throw null; } }
    }
    public enum HttpKeepAlivePingPolicy
    {
        WithActiveRequests,
        Always
    }
    public partial class StreamContent : System.Net.Http.HttpContent
    {
        public StreamContent(System.IO.Stream content) { }
        public StreamContent(System.IO.Stream content, int bufferSize) { }
        protected override System.IO.Stream CreateContentReadStream(System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override System.Threading.Tasks.Task<System.IO.Stream> CreateContentReadStreamAsync() { throw null; }
        protected override void Dispose(bool disposing) { }
        protected override void SerializeToStream(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context) { throw null; }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected internal override bool TryComputeLength(out long length) { throw null; }
    }
    public partial class StringContent : System.Net.Http.ByteArrayContent
    {
        public StringContent(string content) : base (default(byte[])) { }
        public StringContent(string content, System.Net.Http.Headers.MediaTypeHeaderValue mediaType) : base (default(byte[])) { }
        public StringContent(string content, System.Text.Encoding? encoding) : base (default(byte[])) { }
        public StringContent(string content, System.Text.Encoding? encoding, System.Net.Http.Headers.MediaTypeHeaderValue mediaType) : base (default(byte[])) { }
        public StringContent(string content, System.Text.Encoding? encoding, string mediaType) : base (default(byte[])) { }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
namespace System.Net.Http.Headers
{
    public partial class AuthenticationHeaderValue : System.ICloneable
    {
        public AuthenticationHeaderValue(string scheme) { }
        public AuthenticationHeaderValue(string scheme, string? parameter) { }
        public string? Parameter { get { throw null; } }
        public string Scheme { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.Http.Headers.AuthenticationHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.AuthenticationHeaderValue? parsedValue) { throw null; }
    }
    public partial class CacheControlHeaderValue : System.ICloneable
    {
        public CacheControlHeaderValue() { }
        public System.Collections.Generic.ICollection<System.Net.Http.Headers.NameValueHeaderValue> Extensions { get { throw null; } }
        public System.TimeSpan? MaxAge { get { throw null; } set { } }
        public bool MaxStale { get { throw null; } set { } }
        public System.TimeSpan? MaxStaleLimit { get { throw null; } set { } }
        public System.TimeSpan? MinFresh { get { throw null; } set { } }
        public bool MustRevalidate { get { throw null; } set { } }
        public bool NoCache { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<string> NoCacheHeaders { get { throw null; } }
        public bool NoStore { get { throw null; } set { } }
        public bool NoTransform { get { throw null; } set { } }
        public bool OnlyIfCached { get { throw null; } set { } }
        public bool Private { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<string> PrivateHeaders { get { throw null; } }
        public bool ProxyRevalidate { get { throw null; } set { } }
        public bool Public { get { throw null; } set { } }
        public System.TimeSpan? SharedMaxAge { get { throw null; } set { } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.Http.Headers.CacheControlHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse(string? input, out System.Net.Http.Headers.CacheControlHeaderValue? parsedValue) { throw null; }
    }
    public partial class ContentDispositionHeaderValue : System.ICloneable
    {
        protected ContentDispositionHeaderValue(System.Net.Http.Headers.ContentDispositionHeaderValue source) { }
        public ContentDispositionHeaderValue(string dispositionType) { }
        public System.DateTimeOffset? CreationDate { get { throw null; } set { } }
        public string DispositionType { get { throw null; } set { } }
        public string? FileName { get { throw null; } set { } }
        public string? FileNameStar { get { throw null; } set { } }
        public System.DateTimeOffset? ModificationDate { get { throw null; } set { } }
        public string? Name { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<System.Net.Http.Headers.NameValueHeaderValue> Parameters { get { throw null; } }
        public System.DateTimeOffset? ReadDate { get { throw null; } set { } }
        public long? Size { get { throw null; } set { } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.Http.Headers.ContentDispositionHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.ContentDispositionHeaderValue? parsedValue) { throw null; }
    }
    public partial class ContentRangeHeaderValue : System.ICloneable
    {
        public ContentRangeHeaderValue(long length) { }
        public ContentRangeHeaderValue(long from, long to) { }
        public ContentRangeHeaderValue(long from, long to, long length) { }
        public long? From { get { throw null; } }
        public bool HasLength { get { throw null; } }
        public bool HasRange { get { throw null; } }
        public long? Length { get { throw null; } }
        public long? To { get { throw null; } }
        public string Unit { get { throw null; } set { } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.Http.Headers.ContentRangeHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.ContentRangeHeaderValue? parsedValue) { throw null; }
    }
    public partial class EntityTagHeaderValue : System.ICloneable
    {
        public EntityTagHeaderValue(string tag) { }
        public EntityTagHeaderValue(string tag, bool isWeak) { }
        public static System.Net.Http.Headers.EntityTagHeaderValue Any { get { throw null; } }
        public bool IsWeak { get { throw null; } }
        public string Tag { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.Http.Headers.EntityTagHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.EntityTagHeaderValue? parsedValue) { throw null; }
    }
    public readonly partial struct HeaderStringValues : System.Collections.Generic.IEnumerable<string>, System.Collections.Generic.IReadOnlyCollection<string>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Net.Http.Headers.HeaderStringValues.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<string> System.Collections.Generic.IEnumerable<string>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public override string ToString() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<string>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public string Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
    public sealed partial class HttpContentHeaders : System.Net.Http.Headers.HttpHeaders
    {
        internal HttpContentHeaders() { }
        public System.Collections.Generic.ICollection<string> Allow { get { throw null; } }
        public System.Net.Http.Headers.ContentDispositionHeaderValue? ContentDisposition { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<string> ContentEncoding { get { throw null; } }
        public System.Collections.Generic.ICollection<string> ContentLanguage { get { throw null; } }
        public long? ContentLength { get { throw null; } set { } }
        public System.Uri? ContentLocation { get { throw null; } set { } }
        public byte[]? ContentMD5 { get { throw null; } set { } }
        public System.Net.Http.Headers.ContentRangeHeaderValue? ContentRange { get { throw null; } set { } }
        public System.Net.Http.Headers.MediaTypeHeaderValue? ContentType { get { throw null; } set { } }
        public System.DateTimeOffset? Expires { get { throw null; } set { } }
        public System.DateTimeOffset? LastModified { get { throw null; } set { } }
    }
    public abstract partial class HttpHeaders : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<string>>>, System.Collections.IEnumerable
    {
        protected HttpHeaders() { }
        public System.Net.Http.Headers.HttpHeadersNonValidated NonValidated { get { throw null; } }
        public void Add(string name, System.Collections.Generic.IEnumerable<string?> values) { }
        public void Add(string name, string? value) { }
        public void Clear() { }
        public bool Contains(string name) { throw null; }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<string>>> GetEnumerator() { throw null; }
        public System.Collections.Generic.IEnumerable<string> GetValues(string name) { throw null; }
        public bool Remove(string name) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public override string ToString() { throw null; }
        public bool TryAddWithoutValidation(string name, System.Collections.Generic.IEnumerable<string?> values) { throw null; }
        public bool TryAddWithoutValidation(string name, string? value) { throw null; }
        public bool TryGetValues(string name, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Collections.Generic.IEnumerable<string>? values) { throw null; }
    }
    public readonly partial struct HttpHeadersNonValidated : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Net.Http.Headers.HeaderStringValues>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, System.Net.Http.Headers.HeaderStringValues>>, System.Collections.Generic.IReadOnlyDictionary<string, System.Net.Http.Headers.HeaderStringValues>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Net.Http.Headers.HeaderStringValues this[string headerName] { get { throw null; } }
        System.Collections.Generic.IEnumerable<string> System.Collections.Generic.IReadOnlyDictionary<string, System.Net.Http.Headers.HeaderStringValues>.Keys { get { throw null; } }
        System.Collections.Generic.IEnumerable<System.Net.Http.Headers.HeaderStringValues> System.Collections.Generic.IReadOnlyDictionary<string, System.Net.Http.Headers.HeaderStringValues>.Values { get { throw null; } }
        public bool Contains(string headerName) { throw null; }
        bool System.Collections.Generic.IReadOnlyDictionary<string, System.Net.Http.Headers.HeaderStringValues>.ContainsKey(string key) { throw null; }
        public System.Net.Http.Headers.HttpHeadersNonValidated.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, System.Net.Http.Headers.HeaderStringValues>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Net.Http.Headers.HeaderStringValues>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValues(string headerName, out System.Net.Http.Headers.HeaderStringValues values) { throw null; }
        bool System.Collections.Generic.IReadOnlyDictionary<string, System.Net.Http.Headers.HeaderStringValues>.TryGetValue(string key, out System.Net.Http.Headers.HeaderStringValues value) { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, System.Net.Http.Headers.HeaderStringValues>>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Collections.Generic.KeyValuePair<string, System.Net.Http.Headers.HeaderStringValues> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
    public sealed partial class HttpHeaderValueCollection<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable where T : class
    {
        internal HttpHeaderValueCollection() { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public void Add(T item) { }
        public void Clear() { }
        public bool Contains(T item) { throw null; }
        public void CopyTo(T[] array, int arrayIndex) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
        public void ParseAdd(string? input) { }
        public bool Remove(T item) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public override string ToString() { throw null; }
        public bool TryParseAdd(string? input) { throw null; }
    }
    public sealed partial class HttpRequestHeaders : System.Net.Http.Headers.HttpHeaders
    {
        internal HttpRequestHeaders() { }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.MediaTypeWithQualityHeaderValue> Accept { get { throw null; } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.StringWithQualityHeaderValue> AcceptCharset { get { throw null; } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.StringWithQualityHeaderValue> AcceptEncoding { get { throw null; } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.StringWithQualityHeaderValue> AcceptLanguage { get { throw null; } }
        public System.Net.Http.Headers.AuthenticationHeaderValue? Authorization { get { throw null; } set { } }
        public System.Net.Http.Headers.CacheControlHeaderValue? CacheControl { get { throw null; } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<string> Connection { get { throw null; } }
        public bool? ConnectionClose { get { throw null; } set { } }
        public System.DateTimeOffset? Date { get { throw null; } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.NameValueWithParametersHeaderValue> Expect { get { throw null; } }
        public bool? ExpectContinue { get { throw null; } set { } }
        public string? From { get { throw null; } set { } }
        public string? Host { get { throw null; } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.EntityTagHeaderValue> IfMatch { get { throw null; } }
        public System.DateTimeOffset? IfModifiedSince { get { throw null; } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.EntityTagHeaderValue> IfNoneMatch { get { throw null; } }
        public System.Net.Http.Headers.RangeConditionHeaderValue? IfRange { get { throw null; } set { } }
        public System.DateTimeOffset? IfUnmodifiedSince { get { throw null; } set { } }
        public int? MaxForwards { get { throw null; } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.NameValueHeaderValue> Pragma { get { throw null; } }
        public System.Net.Http.Headers.AuthenticationHeaderValue? ProxyAuthorization { get { throw null; } set { } }
        public System.Net.Http.Headers.RangeHeaderValue? Range { get { throw null; } set { } }
        public System.Uri? Referrer { get { throw null; } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.TransferCodingWithQualityHeaderValue> TE { get { throw null; } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<string> Trailer { get { throw null; } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.TransferCodingHeaderValue> TransferEncoding { get { throw null; } }
        public bool? TransferEncodingChunked { get { throw null; } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ProductHeaderValue> Upgrade { get { throw null; } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ProductInfoHeaderValue> UserAgent { get { throw null; } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ViaHeaderValue> Via { get { throw null; } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.WarningHeaderValue> Warning { get { throw null; } }
    }
    public sealed partial class HttpResponseHeaders : System.Net.Http.Headers.HttpHeaders
    {
        internal HttpResponseHeaders() { }
        public System.Net.Http.Headers.HttpHeaderValueCollection<string> AcceptRanges { get { throw null; } }
        public System.TimeSpan? Age { get { throw null; } set { } }
        public System.Net.Http.Headers.CacheControlHeaderValue? CacheControl { get { throw null; } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<string> Connection { get { throw null; } }
        public bool? ConnectionClose { get { throw null; } set { } }
        public System.DateTimeOffset? Date { get { throw null; } set { } }
        public System.Net.Http.Headers.EntityTagHeaderValue? ETag { get { throw null; } set { } }
        public System.Uri? Location { get { throw null; } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.NameValueHeaderValue> Pragma { get { throw null; } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.AuthenticationHeaderValue> ProxyAuthenticate { get { throw null; } }
        public System.Net.Http.Headers.RetryConditionHeaderValue? RetryAfter { get { throw null; } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ProductInfoHeaderValue> Server { get { throw null; } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<string> Trailer { get { throw null; } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.TransferCodingHeaderValue> TransferEncoding { get { throw null; } }
        public bool? TransferEncodingChunked { get { throw null; } set { } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ProductHeaderValue> Upgrade { get { throw null; } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<string> Vary { get { throw null; } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.ViaHeaderValue> Via { get { throw null; } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.WarningHeaderValue> Warning { get { throw null; } }
        public System.Net.Http.Headers.HttpHeaderValueCollection<System.Net.Http.Headers.AuthenticationHeaderValue> WwwAuthenticate { get { throw null; } }
    }
    public partial class MediaTypeHeaderValue : System.ICloneable
    {
        protected MediaTypeHeaderValue(System.Net.Http.Headers.MediaTypeHeaderValue source) { }
        public MediaTypeHeaderValue(string mediaType) { }
        public MediaTypeHeaderValue(string mediaType, string? charSet) { }
        public string? CharSet { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.DisallowNullAttribute]
        public string? MediaType { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<System.Net.Http.Headers.NameValueHeaderValue> Parameters { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.Http.Headers.MediaTypeHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.MediaTypeHeaderValue? parsedValue) { throw null; }
    }
    public sealed partial class MediaTypeWithQualityHeaderValue : System.Net.Http.Headers.MediaTypeHeaderValue, System.ICloneable
    {
        public MediaTypeWithQualityHeaderValue(string mediaType) : base (default(System.Net.Http.Headers.MediaTypeHeaderValue)) { }
        public MediaTypeWithQualityHeaderValue(string mediaType, double quality) : base (default(System.Net.Http.Headers.MediaTypeHeaderValue)) { }
        public double? Quality { get { throw null; } set { } }
        public static new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.MediaTypeWithQualityHeaderValue? parsedValue) { throw null; }
    }
    public partial class NameValueHeaderValue : System.ICloneable
    {
        protected NameValueHeaderValue(System.Net.Http.Headers.NameValueHeaderValue source) { }
        public NameValueHeaderValue(string name) { }
        public NameValueHeaderValue(string name, string? value) { }
        public string Name { get { throw null; } }
        public string? Value { get { throw null; } set { } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.Http.Headers.NameValueHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.NameValueHeaderValue? parsedValue) { throw null; }
    }
    public partial class NameValueWithParametersHeaderValue : System.Net.Http.Headers.NameValueHeaderValue, System.ICloneable
    {
        protected NameValueWithParametersHeaderValue(System.Net.Http.Headers.NameValueWithParametersHeaderValue source) : base (default(string)) { }
        public NameValueWithParametersHeaderValue(string name) : base (default(string)) { }
        public NameValueWithParametersHeaderValue(string name, string? value) : base (default(string)) { }
        public System.Collections.Generic.ICollection<System.Net.Http.Headers.NameValueHeaderValue> Parameters { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static new System.Net.Http.Headers.NameValueWithParametersHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.NameValueWithParametersHeaderValue? parsedValue) { throw null; }
    }
    public partial class ProductHeaderValue : System.ICloneable
    {
        public ProductHeaderValue(string name) { }
        public ProductHeaderValue(string name, string? version) { }
        public string Name { get { throw null; } }
        public string? Version { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.Http.Headers.ProductHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.ProductHeaderValue? parsedValue) { throw null; }
    }
    public partial class ProductInfoHeaderValue : System.ICloneable
    {
        public ProductInfoHeaderValue(System.Net.Http.Headers.ProductHeaderValue product) { }
        public ProductInfoHeaderValue(string comment) { }
        public ProductInfoHeaderValue(string productName, string? productVersion) { }
        public string? Comment { get { throw null; } }
        public System.Net.Http.Headers.ProductHeaderValue? Product { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.Http.Headers.ProductInfoHeaderValue Parse(string input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.ProductInfoHeaderValue? parsedValue) { throw null; }
    }
    public partial class RangeConditionHeaderValue : System.ICloneable
    {
        public RangeConditionHeaderValue(System.DateTimeOffset date) { }
        public RangeConditionHeaderValue(System.Net.Http.Headers.EntityTagHeaderValue entityTag) { }
        public RangeConditionHeaderValue(string entityTag) { }
        public System.DateTimeOffset? Date { get { throw null; } }
        public System.Net.Http.Headers.EntityTagHeaderValue? EntityTag { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.Http.Headers.RangeConditionHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.RangeConditionHeaderValue? parsedValue) { throw null; }
    }
    public partial class RangeHeaderValue : System.ICloneable
    {
        public RangeHeaderValue() { }
        public RangeHeaderValue(long? from, long? to) { }
        public System.Collections.Generic.ICollection<System.Net.Http.Headers.RangeItemHeaderValue> Ranges { get { throw null; } }
        public string Unit { get { throw null; } set { } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.Http.Headers.RangeHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.RangeHeaderValue? parsedValue) { throw null; }
    }
    public partial class RangeItemHeaderValue : System.ICloneable
    {
        public RangeItemHeaderValue(long? from, long? to) { }
        public long? From { get { throw null; } }
        public long? To { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class RetryConditionHeaderValue : System.ICloneable
    {
        public RetryConditionHeaderValue(System.DateTimeOffset date) { }
        public RetryConditionHeaderValue(System.TimeSpan delta) { }
        public System.DateTimeOffset? Date { get { throw null; } }
        public System.TimeSpan? Delta { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.Http.Headers.RetryConditionHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.RetryConditionHeaderValue? parsedValue) { throw null; }
    }
    public partial class StringWithQualityHeaderValue : System.ICloneable
    {
        public StringWithQualityHeaderValue(string value) { }
        public StringWithQualityHeaderValue(string value, double quality) { }
        public double? Quality { get { throw null; } }
        public string Value { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.Http.Headers.StringWithQualityHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.StringWithQualityHeaderValue? parsedValue) { throw null; }
    }
    public partial class TransferCodingHeaderValue : System.ICloneable
    {
        protected TransferCodingHeaderValue(System.Net.Http.Headers.TransferCodingHeaderValue source) { }
        public TransferCodingHeaderValue(string value) { }
        public System.Collections.Generic.ICollection<System.Net.Http.Headers.NameValueHeaderValue> Parameters { get { throw null; } }
        public string Value { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.Http.Headers.TransferCodingHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.TransferCodingHeaderValue? parsedValue) { throw null; }
    }
    public sealed partial class TransferCodingWithQualityHeaderValue : System.Net.Http.Headers.TransferCodingHeaderValue, System.ICloneable
    {
        public TransferCodingWithQualityHeaderValue(string value) : base (default(System.Net.Http.Headers.TransferCodingHeaderValue)) { }
        public TransferCodingWithQualityHeaderValue(string value, double quality) : base (default(System.Net.Http.Headers.TransferCodingHeaderValue)) { }
        public double? Quality { get { throw null; } set { } }
        public static new System.Net.Http.Headers.TransferCodingWithQualityHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.TransferCodingWithQualityHeaderValue? parsedValue) { throw null; }
    }
    public partial class ViaHeaderValue : System.ICloneable
    {
        public ViaHeaderValue(string protocolVersion, string receivedBy) { }
        public ViaHeaderValue(string protocolVersion, string receivedBy, string? protocolName) { }
        public ViaHeaderValue(string protocolVersion, string receivedBy, string? protocolName, string? comment) { }
        public string? Comment { get { throw null; } }
        public string? ProtocolName { get { throw null; } }
        public string ProtocolVersion { get { throw null; } }
        public string ReceivedBy { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.Http.Headers.ViaHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.ViaHeaderValue? parsedValue) { throw null; }
    }
    public partial class WarningHeaderValue : System.ICloneable
    {
        public WarningHeaderValue(int code, string agent, string text) { }
        public WarningHeaderValue(int code, string agent, string text, System.DateTimeOffset date) { }
        public string Agent { get { throw null; } }
        public int Code { get { throw null; } }
        public System.DateTimeOffset? Date { get { throw null; } }
        public string Text { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.Http.Headers.WarningHeaderValue Parse(string? input) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? input, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.Http.Headers.WarningHeaderValue? parsedValue) { throw null; }
    }
}

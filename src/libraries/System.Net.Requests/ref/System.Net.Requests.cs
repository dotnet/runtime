// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Net
{
    public partial class HttpWebRequest : System.Net.WebRequest
    {
        internal HttpWebRequest() { }
        public string Accept { get { return default(string); } set { } }
        public virtual bool AllowReadStreamBuffering { get { return default(bool); } set { } }
        public override string ContentType { get { return default(string); } set { } }
        public int ContinueTimeout { get { return default(int); } set { } }
        public virtual System.Net.CookieContainer CookieContainer { get { return default(System.Net.CookieContainer); } set { } }
        public override System.Net.ICredentials Credentials { get { return default(System.Net.ICredentials); } set { } }
        public virtual bool HaveResponse { get { return default(bool); } }
        public override System.Net.WebHeaderCollection Headers { get { return default(System.Net.WebHeaderCollection); } set { } }
        public override string Method { get { return default(string); } set { } }
        public override System.Uri RequestUri { get { return default(System.Uri); } }
        public virtual bool SupportsCookieContainer { get { return default(bool); } }
        public override bool UseDefaultCredentials { get { return default(bool); } set { } }
        public override void Abort() { }
        public override System.IAsyncResult BeginGetRequestStream(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public override System.IAsyncResult BeginGetResponse(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public override System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult) { return default(System.IO.Stream); }
        public override System.Net.WebResponse EndGetResponse(System.IAsyncResult asyncResult) { return default(System.Net.WebResponse); }
    }
    public partial class HttpWebResponse : System.Net.WebResponse
    {
        internal HttpWebResponse() { }
        public override long ContentLength { get { return default(long); } }
        public override string ContentType { get { return default(string); } }
        public virtual System.Net.CookieCollection Cookies { get { return default(System.Net.CookieCollection); } }
        public override System.Net.WebHeaderCollection Headers { get { return default(System.Net.WebHeaderCollection); } }
        public virtual string Method { get { return default(string); } }
        public override System.Uri ResponseUri { get { return default(System.Uri); } }
        public virtual System.Net.HttpStatusCode StatusCode { get { return default(System.Net.HttpStatusCode); } }
        public virtual string StatusDescription { get { return default(string); } }
        public override bool SupportsHeaders { get { return default(bool); } }
        protected override void Dispose(bool disposing) { }
        public override System.IO.Stream GetResponseStream() { return default(System.IO.Stream); }
    }
    public partial interface IWebRequestCreate
    {
        System.Net.WebRequest Create(System.Uri uri);
    }
    public partial class ProtocolViolationException : System.InvalidOperationException
    {
        public ProtocolViolationException() { }
        public ProtocolViolationException(string message) { }
    }
    public partial class WebException : System.InvalidOperationException
    {
        public WebException() { }
        public WebException(string message) { }
        public WebException(string message, System.Exception innerException) { }
        public WebException(string message, System.Exception innerException, System.Net.WebExceptionStatus status, System.Net.WebResponse response) { }
        public WebException(string message, System.Net.WebExceptionStatus status) { }
        public System.Net.WebResponse Response { get { return default(System.Net.WebResponse); } }
        public System.Net.WebExceptionStatus Status { get { return default(System.Net.WebExceptionStatus); } }
    }
    public enum WebExceptionStatus
    {
        CacheEntryNotFound = 18,
        ConnectFailure = 2,
        ConnectionClosed = 8,
        KeepAliveFailure = 12,
        MessageLengthLimitExceeded = 17,
        NameResolutionFailure = 1,
        Pending = 13,
        PipelineFailure = 5,
        ProtocolError = 7,
        ProxyNameResolutionFailure = 15,
        ReceiveFailure = 3,
        RequestCanceled = 6,
        RequestProhibitedByCachePolicy = 19,
        RequestProhibitedByProxy = 20,
        SecureChannelFailure = 10,
        SendFailure = 4,
        ServerProtocolViolation = 11,
        Success = 0,
        Timeout = 14,
        TrustFailure = 9,
        UnknownError = 16,
    }
    public abstract partial class WebRequest
    {
        protected WebRequest() { }
        public abstract string ContentType { get; set; }
        public virtual System.Net.ICredentials Credentials { get { return default(System.Net.ICredentials); } set { } }
        public static System.Net.IWebProxy DefaultWebProxy { get { return default(System.Net.IWebProxy); } set { } }
        public abstract System.Net.WebHeaderCollection Headers { get; set; }
        public abstract string Method { get; set; }
        public virtual System.Net.IWebProxy Proxy { get { return default(System.Net.IWebProxy); } set { } }
        public abstract System.Uri RequestUri { get; }
        public virtual bool UseDefaultCredentials { get { return default(bool); } set { } }
        public abstract void Abort();
        public abstract System.IAsyncResult BeginGetRequestStream(System.AsyncCallback callback, object state);
        public abstract System.IAsyncResult BeginGetResponse(System.AsyncCallback callback, object state);
        public static System.Net.WebRequest Create(string requestUriString) { return default(System.Net.WebRequest); }
        public static System.Net.WebRequest Create(System.Uri requestUri) { return default(System.Net.WebRequest); }
        public static System.Net.HttpWebRequest CreateHttp(string requestUriString) { return default(System.Net.HttpWebRequest); }
        public static System.Net.HttpWebRequest CreateHttp(System.Uri requestUri) { return default(System.Net.HttpWebRequest); }
        public abstract System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult);
        public abstract System.Net.WebResponse EndGetResponse(System.IAsyncResult asyncResult);
        public virtual System.Threading.Tasks.Task<System.IO.Stream> GetRequestStreamAsync() { return default(System.Threading.Tasks.Task<System.IO.Stream>); }
        public virtual System.Threading.Tasks.Task<System.Net.WebResponse> GetResponseAsync() { return default(System.Threading.Tasks.Task<System.Net.WebResponse>); }
        public static bool RegisterPrefix(string prefix, System.Net.IWebRequestCreate creator) { return default(bool); }
    }
    public abstract partial class WebResponse : System.IDisposable
    {
        protected WebResponse() { }
        public abstract long ContentLength { get; }
        public abstract string ContentType { get; }
        public virtual System.Net.WebHeaderCollection Headers { get { return default(System.Net.WebHeaderCollection); } }
        public abstract System.Uri ResponseUri { get; }
        public virtual bool SupportsHeaders { get { return default(bool); } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract System.IO.Stream GetResponseStream();
    }
}

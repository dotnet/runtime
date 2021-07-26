// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Cache;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public class WebClient : Component
    {
        private const int DefaultCopyBufferLength = 8192;
        private const int DefaultDownloadBufferLength = 65536;
        private const string DefaultUploadFileContentType = "application/octet-stream";
        private const string UploadFileContentType = "multipart/form-data";
        private const string UploadValuesContentType = "application/x-www-form-urlencoded";

        private Uri? _baseAddress;
        private ICredentials? _credentials;
        private WebHeaderCollection? _headers;
        private NameValueCollection? _requestParameters;
        private WebResponse? _webResponse;
        private WebRequest? _webRequest;
        private Encoding _encoding = Encoding.Default;
        private string? _method;
        private long _contentLength = -1;
        private bool _initWebClientAsync;
        private bool _canceled;
        private ProgressData? _progress;
        private IWebProxy? _proxy;
        private bool _proxySet;
        private int _callNesting; // > 0 if we're in a Read/Write call
        private AsyncOperation? _asyncOp;

        private SendOrPostCallback? _downloadDataOperationCompleted;
        private SendOrPostCallback? _openReadOperationCompleted;
        private SendOrPostCallback? _openWriteOperationCompleted;
        private SendOrPostCallback? _downloadStringOperationCompleted;
        private SendOrPostCallback? _downloadFileOperationCompleted;
        private SendOrPostCallback? _uploadStringOperationCompleted;
        private SendOrPostCallback? _uploadDataOperationCompleted;
        private SendOrPostCallback? _uploadFileOperationCompleted;
        private SendOrPostCallback? _uploadValuesOperationCompleted;
        private SendOrPostCallback? _reportDownloadProgressChanged;
        private SendOrPostCallback? _reportUploadProgressChanged;

        [Obsolete(Obsoletions.WebRequestMessage, DiagnosticId = Obsoletions.WebRequestDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public WebClient()
        {
            // We don't know if derived types need finalizing, but WebClient doesn't.
            if (GetType() == typeof(WebClient))
            {
                GC.SuppressFinalize(this);
            }
        }

        public event DownloadStringCompletedEventHandler? DownloadStringCompleted;
        public event DownloadDataCompletedEventHandler? DownloadDataCompleted;
        public event AsyncCompletedEventHandler? DownloadFileCompleted;
        public event UploadStringCompletedEventHandler? UploadStringCompleted;
        public event UploadDataCompletedEventHandler? UploadDataCompleted;
        public event UploadFileCompletedEventHandler? UploadFileCompleted;
        public event UploadValuesCompletedEventHandler? UploadValuesCompleted;
        public event OpenReadCompletedEventHandler? OpenReadCompleted;
        public event OpenWriteCompletedEventHandler? OpenWriteCompleted;
        public event DownloadProgressChangedEventHandler? DownloadProgressChanged;
        public event UploadProgressChangedEventHandler? UploadProgressChanged;

        protected virtual void OnDownloadStringCompleted(DownloadStringCompletedEventArgs e) => DownloadStringCompleted?.Invoke(this, e);
        protected virtual void OnDownloadDataCompleted(DownloadDataCompletedEventArgs e) => DownloadDataCompleted?.Invoke(this, e);
        protected virtual void OnDownloadFileCompleted(AsyncCompletedEventArgs e) => DownloadFileCompleted?.Invoke(this, e);
        protected virtual void OnDownloadProgressChanged(DownloadProgressChangedEventArgs e) => DownloadProgressChanged?.Invoke(this, e);
        protected virtual void OnUploadStringCompleted(UploadStringCompletedEventArgs e) => UploadStringCompleted?.Invoke(this, e);
        protected virtual void OnUploadDataCompleted(UploadDataCompletedEventArgs e) => UploadDataCompleted?.Invoke(this, e);
        protected virtual void OnUploadFileCompleted(UploadFileCompletedEventArgs e) => UploadFileCompleted?.Invoke(this, e);
        protected virtual void OnUploadValuesCompleted(UploadValuesCompletedEventArgs e) => UploadValuesCompleted?.Invoke(this, e);
        protected virtual void OnUploadProgressChanged(UploadProgressChangedEventArgs e) => UploadProgressChanged?.Invoke(this, e);
        protected virtual void OnOpenReadCompleted(OpenReadCompletedEventArgs e) => OpenReadCompleted?.Invoke(this, e);
        protected virtual void OnOpenWriteCompleted(OpenWriteCompletedEventArgs e) => OpenWriteCompleted?.Invoke(this, e);

        private void StartOperation()
        {
            if (Interlocked.Increment(ref _callNesting) > 1)
            {
                EndOperation();
                throw new NotSupportedException(SR.net_webclient_no_concurrent_io_allowed);
            }

            _contentLength = -1;
            _webResponse = null;
            _webRequest = null;
            _method = null;
            _canceled = false;

            _progress?.Reset();
        }

        private AsyncOperation StartAsyncOperation(object? userToken)
        {
            if (!_initWebClientAsync)
            {
                // Set up the async delegates

                _openReadOperationCompleted = arg => OnOpenReadCompleted((OpenReadCompletedEventArgs)arg!);
                _openWriteOperationCompleted = arg => OnOpenWriteCompleted((OpenWriteCompletedEventArgs)arg!);

                _downloadStringOperationCompleted = arg => OnDownloadStringCompleted((DownloadStringCompletedEventArgs)arg!);
                _downloadDataOperationCompleted = arg => OnDownloadDataCompleted((DownloadDataCompletedEventArgs)arg!);
                _downloadFileOperationCompleted = arg => OnDownloadFileCompleted((AsyncCompletedEventArgs)arg!);

                _uploadStringOperationCompleted = arg => OnUploadStringCompleted((UploadStringCompletedEventArgs)arg!);
                _uploadDataOperationCompleted = arg => OnUploadDataCompleted((UploadDataCompletedEventArgs)arg!);
                _uploadFileOperationCompleted = arg => OnUploadFileCompleted((UploadFileCompletedEventArgs)arg!);
                _uploadValuesOperationCompleted = arg => OnUploadValuesCompleted((UploadValuesCompletedEventArgs)arg!);

                _reportDownloadProgressChanged = arg => OnDownloadProgressChanged((DownloadProgressChangedEventArgs)arg!);
                _reportUploadProgressChanged = arg => OnUploadProgressChanged((UploadProgressChangedEventArgs)arg!);

                _progress = new ProgressData();
                _initWebClientAsync = true;
            }

            AsyncOperation asyncOp = AsyncOperationManager.CreateOperation(userToken);

            StartOperation();
            _asyncOp = asyncOp;

            return asyncOp;
        }

        private void EndOperation() => Interlocked.Decrement(ref _callNesting);

        public Encoding Encoding
        {
            get { return _encoding; }
            set
            {
                ArgumentNullException.ThrowIfNull(value, nameof(value));
                _encoding = value;
            }
        }

        [AllowNull]
        public string BaseAddress
        {
            get { return _baseAddress != null ? _baseAddress.ToString() : string.Empty; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _baseAddress = null;
                }
                else
                {
                    try
                    {
                        _baseAddress = new Uri(value);
                    }
                    catch (UriFormatException e)
                    {
                        throw new ArgumentException(SR.net_webclient_invalid_baseaddress, nameof(value), e);
                    }
                }
            }
        }

        public ICredentials? Credentials
        {
            get { return _credentials; }
            set { _credentials = value; }
        }

        public bool UseDefaultCredentials
        {
            get { return _credentials == CredentialCache.DefaultCredentials; }
            set { _credentials = value ? CredentialCache.DefaultCredentials : null; }
        }

        [AllowNull]
        public WebHeaderCollection Headers
        {
            get { return _headers ??= new WebHeaderCollection(); }
            set { _headers = value; }
        }

        [AllowNull]
        public NameValueCollection QueryString
        {
            get { return _requestParameters ??= new NameValueCollection(); }
            set { _requestParameters = value; }
        }

        public WebHeaderCollection? ResponseHeaders => _webResponse?.Headers;

        public IWebProxy? Proxy
        {
            get { return _proxySet ? _proxy : WebRequest.DefaultWebProxy; }
            set
            {
                _proxy = value;
                _proxySet = true;
            }
        }

        public RequestCachePolicy? CachePolicy { get; set; }

        public bool IsBusy => Volatile.Read(ref _callNesting) > 0;

        protected virtual WebRequest GetWebRequest(Uri address)
        {
#pragma warning disable SYSLIB0014 // WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.
            WebRequest request = WebRequest.Create(address);
#pragma warning restore SYSLIB0014

            CopyHeadersTo(request);

            if (Credentials != null)
            {
                request.Credentials = Credentials;
            }

            if (_method != null)
            {
                request.Method = _method;
            }

            if (_contentLength != -1)
            {
                request.ContentLength = _contentLength;
            }

            if (_proxySet)
            {
                request.Proxy = _proxy;
            }

            if (CachePolicy != null)
            {
                request.CachePolicy = CachePolicy;
            }

            return request;
        }

        protected virtual WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = request.GetResponse();
            _webResponse = response;
            return response;
        }

        protected virtual WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            WebResponse response = request.EndGetResponse(result);
            _webResponse = response;
            return response;
        }

        private async Task<WebResponse> GetWebResponseTaskAsync(WebRequest request)
        {
            // We would like to simply await request.GetResponseAsync(), but WebClient exposes
            // a protected member GetWebResponse(WebRequest, IAsyncResult) that derived instances expect to
            // be used to get the response, and it needs to be passed the IAsyncResult that was returned
            // from WebRequest.BeginGetResponse.
            var awaitable = new BeginEndAwaitableAdapter();
            request.BeginGetResponse(BeginEndAwaitableAdapter.Callback, awaitable);
            return GetWebResponse(request, await awaitable);
        }

        public byte[] DownloadData(string address) =>
            DownloadData(GetUri(address));

        public byte[] DownloadData(Uri address)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));

            StartOperation();
            try
            {
                WebRequest request;
                return DownloadDataInternal(address, out request);
            }
            finally
            {
                EndOperation();
            }
        }

        private byte[] DownloadDataInternal(Uri address, out WebRequest request)
        {
            WebRequest? tmpRequest = null;
            byte[] result;

            try
            {
                tmpRequest = _webRequest = GetWebRequest(GetUri(address));
                result = DownloadBits(tmpRequest, new ChunkedMemoryStream())!;
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                AbortRequest(tmpRequest);
                if (e is WebException || e is SecurityException) throw;
                throw new WebException(SR.net_webclient, e);
            }

            request = tmpRequest;
            return result;
        }

        public void DownloadFile(string address, string fileName) =>
            DownloadFile(GetUri(address), fileName);

        public void DownloadFile(Uri address, string fileName)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));
            ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));

            WebRequest? request = null;
            FileStream? fs = null;
            bool succeeded = false;
            StartOperation();
            try
            {
                fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                request = _webRequest = GetWebRequest(GetUri(address));
                DownloadBits(request, fs);
                succeeded = true;
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                AbortRequest(request);
                if (e is WebException || e is SecurityException) throw;
                throw new WebException(SR.net_webclient, e);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    if (!succeeded)
                    {
                        File.Delete(fileName);
                    }
                }
                EndOperation();
            }
        }

        public Stream OpenRead(string address) =>
            OpenRead(GetUri(address));

        public Stream OpenRead(Uri address)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));

            WebRequest? request = null;
            StartOperation();
            try
            {
                request = _webRequest = GetWebRequest(GetUri(address));
                WebResponse response = _webResponse = GetWebResponse(request);
                return response.GetResponseStream();
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                AbortRequest(request);
                if (e is WebException || e is SecurityException) throw;
                throw new WebException(SR.net_webclient, e);
            }
            finally
            {
                EndOperation();
            }
        }

        public Stream OpenWrite(string address) =>
            OpenWrite(GetUri(address), null);

        public Stream OpenWrite(Uri address) =>
            OpenWrite(address, null);

        public Stream OpenWrite(string address, string? method) =>
            OpenWrite(GetUri(address), method);

        public Stream OpenWrite(Uri address, string? method)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));
            if (method == null)
            {
                method = MapToDefaultMethod(address);
            }

            WebRequest? request = null;
            StartOperation();
            try
            {
                _method = method;
                request = _webRequest = GetWebRequest(GetUri(address));
                return new WebClientWriteStream(
                    request.GetRequestStream(),
                    request,
                    this);
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                AbortRequest(request);
                if (e is WebException || e is SecurityException) throw;
                throw new WebException(SR.net_webclient, e);
            }
            finally
            {
                EndOperation();
            }
        }

        public byte[] UploadData(string address, byte[] data) =>
            UploadData(GetUri(address), null, data);

        public byte[] UploadData(Uri address, byte[] data) =>
            UploadData(address, null, data);

        public byte[] UploadData(string address, string? method, byte[] data) =>
            UploadData(GetUri(address), method, data);

        public byte[] UploadData(Uri address, string? method, byte[] data)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));
            ArgumentNullException.ThrowIfNull(data, nameof(data));
            if (method == null)
            {
                method = MapToDefaultMethod(address);
            }

            StartOperation();
            try
            {
                WebRequest request;
                return UploadDataInternal(address, method, data, out request);
            }
            finally
            {
                EndOperation();
            }
        }

        private byte[] UploadDataInternal(Uri address, string method, byte[] data, out WebRequest request)
        {
            WebRequest? tmpRequest = null;
            byte[] result;

            try
            {
                _method = method;
                _contentLength = data.Length;
                tmpRequest = _webRequest = GetWebRequest(GetUri(address));
                result = UploadBits(tmpRequest, null, data, 0, null, null);
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                AbortRequest(tmpRequest);
                if (e is WebException || e is SecurityException) throw;
                throw new WebException(SR.net_webclient, e);
            }

            request = tmpRequest;
            return result;
        }

        private void OpenFileInternal(
            bool needsHeaderAndBoundary, string fileName,
            out FileStream fs, out byte[] buffer, ref byte[]? formHeaderBytes, ref byte[]? boundaryBytes)
        {
            fileName = Path.GetFullPath(fileName);

            WebHeaderCollection headers = Headers;
            string? contentType = headers[HttpKnownHeaderNames.ContentType];

            if (contentType == null)
            {
                contentType = DefaultUploadFileContentType;
            }
            else if (contentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
            {
                throw new WebException(SR.net_webclient_Multipart);
            }

            fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            int buffSize = DefaultCopyBufferLength;
            _contentLength = -1;

            if (string.Equals(_method, "POST", StringComparison.Ordinal))
            {
                if (needsHeaderAndBoundary)
                {
                    string boundary = $"---------------------{DateTime.Now.Ticks:x}";

                    headers[HttpKnownHeaderNames.ContentType] = UploadFileContentType + "; boundary=" + boundary;

                    string formHeader =
                        "--" + boundary + "\r\n" +
                        "Content-Disposition: form-data; name=\"file\"; filename=\"" + Path.GetFileName(fileName) + "\"\r\n" + // TODO: Should the filename path be encoded?
                        "Content-Type: " + contentType + "\r\n" +
                        "\r\n";
                    formHeaderBytes = Encoding.UTF8.GetBytes(formHeader);
                    boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                }
                else
                {
                    formHeaderBytes = Array.Empty<byte>();
                    boundaryBytes = Array.Empty<byte>();
                }

                if (fs.CanSeek)
                {
                    _contentLength = fs.Length + formHeaderBytes.Length + boundaryBytes.Length;
                    buffSize = (int)Math.Min(DefaultCopyBufferLength, fs.Length);
                }
            }
            else
            {
                headers[HttpKnownHeaderNames.ContentType] = contentType;

                formHeaderBytes = null;
                boundaryBytes = null;

                if (fs.CanSeek)
                {
                    _contentLength = fs.Length;
                    buffSize = (int)Math.Min(DefaultCopyBufferLength, fs.Length);
                }
            }

            buffer = new byte[buffSize];
        }

        public byte[] UploadFile(string address, string fileName) =>
            UploadFile(GetUri(address), fileName);

        public byte[] UploadFile(Uri address, string fileName) =>
            UploadFile(address, null, fileName);

        public byte[] UploadFile(string address, string? method, string fileName) =>
            UploadFile(GetUri(address), method, fileName);

        public byte[] UploadFile(Uri address, string? method, string fileName)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));
            ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));
            if (method == null)
            {
                method = MapToDefaultMethod(address);
            }

            FileStream? fs = null;
            WebRequest? request = null;
            StartOperation();
            try
            {
                _method = method;
                byte[]? formHeaderBytes = null, boundaryBytes = null, buffer;
                Uri uri = GetUri(address);
                bool needsHeaderAndBoundary = (uri.Scheme != Uri.UriSchemeFile);
                OpenFileInternal(needsHeaderAndBoundary, fileName, out fs, out buffer, ref formHeaderBytes, ref boundaryBytes);
                request = _webRequest = GetWebRequest(uri);
                return UploadBits(request, fs, buffer, 0, formHeaderBytes, boundaryBytes);
            }
            catch (Exception e)
            {
                fs?.Close();
                if (e is OutOfMemoryException) throw;
                AbortRequest(request);
                if (e is WebException || e is SecurityException) throw;
                throw new WebException(SR.net_webclient, e);
            }
            finally
            {
                EndOperation();
            }
        }

        private byte[] GetValuesToUpload(NameValueCollection data)
        {
            WebHeaderCollection headers = Headers;

            string? contentType = headers[HttpKnownHeaderNames.ContentType];
            if (contentType != null && !string.Equals(contentType, UploadValuesContentType, StringComparison.OrdinalIgnoreCase))
            {
                throw new WebException(SR.net_webclient_ContentType);
            }

            headers[HttpKnownHeaderNames.ContentType] = UploadValuesContentType;

            string delimiter = string.Empty;
            var values = new StringBuilder();
            foreach (string? name in data.AllKeys)
            {
                values.Append(delimiter);
                values.Append(UrlEncode(name));
                values.Append('=');
                values.Append(UrlEncode(data[name]));
                delimiter = "&";
            }

            byte[] buffer = Encoding.ASCII.GetBytes(values.ToString());
            _contentLength = buffer.Length;
            return buffer;
        }

        public byte[] UploadValues(string address, NameValueCollection data) =>
            UploadValues(GetUri(address), null, data);

        public byte[] UploadValues(Uri address, NameValueCollection data) =>
            UploadValues(address, null, data);

        public byte[] UploadValues(string address, string? method, NameValueCollection data) =>
            UploadValues(GetUri(address), method, data);

        public byte[] UploadValues(Uri address, string? method, NameValueCollection data)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));
            ArgumentNullException.ThrowIfNull(data, nameof(data));
            if (method == null)
            {
                method = MapToDefaultMethod(address);
            }

            WebRequest? request = null;
            StartOperation();
            try
            {
                byte[] buffer = GetValuesToUpload(data);
                _method = method;
                request = _webRequest = GetWebRequest(GetUri(address));
                return UploadBits(request, null, buffer, 0, null, null);
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                AbortRequest(request);
                if (e is WebException || e is SecurityException) throw;
                throw new WebException(SR.net_webclient, e);
            }
            finally
            {
                EndOperation();
            }
        }

        public string UploadString(string address, string data) =>
            UploadString(GetUri(address), null, data);

        public string UploadString(Uri address, string data) =>
            UploadString(address, null, data);

        public string UploadString(string address, string? method, string data) =>
            UploadString(GetUri(address), method, data);

        public string UploadString(Uri address, string? method, string data)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));
            ArgumentNullException.ThrowIfNull(data, nameof(data));
            if (method == null)
            {
                method = MapToDefaultMethod(address);
            }

            StartOperation();
            try
            {
                WebRequest request;
                byte[] requestData = Encoding.GetBytes(data);
                byte[] responseData = UploadDataInternal(address, method, requestData, out request);
                return GetStringUsingEncoding(request, responseData);
            }
            finally
            {
                EndOperation();
            }
        }

        public string DownloadString(string address) =>
            DownloadString(GetUri(address));

        public string DownloadString(Uri address)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));

            StartOperation();
            try
            {
                WebRequest request;
                byte[] data = DownloadDataInternal(address, out request);
                return GetStringUsingEncoding(request, data);
            }
            finally
            {
                EndOperation();
            }
        }

        private static void AbortRequest(WebRequest? request)
        {
            try { request?.Abort(); }
            catch (Exception exception) when (!(exception is OutOfMemoryException)) { }
        }

        private void CopyHeadersTo(WebRequest request)
        {
            if (_headers == null)
            {
                return;
            }

            var hwr = request as HttpWebRequest;
            if (hwr == null)
            {
                return;
            }

            string? accept = _headers[HttpKnownHeaderNames.Accept];
            string? connection = _headers[HttpKnownHeaderNames.Connection];
            string? contentType = _headers[HttpKnownHeaderNames.ContentType];
            string? expect = _headers[HttpKnownHeaderNames.Expect];
            string? referrer = _headers[HttpKnownHeaderNames.Referer];
            string? userAgent = _headers[HttpKnownHeaderNames.UserAgent];
            string? host = _headers[HttpKnownHeaderNames.Host];

            _headers.Remove(HttpKnownHeaderNames.Accept);
            _headers.Remove(HttpKnownHeaderNames.Connection);
            _headers.Remove(HttpKnownHeaderNames.ContentType);
            _headers.Remove(HttpKnownHeaderNames.Expect);
            _headers.Remove(HttpKnownHeaderNames.Referer);
            _headers.Remove(HttpKnownHeaderNames.UserAgent);
            _headers.Remove(HttpKnownHeaderNames.Host);

            request.Headers = _headers;

            if (!string.IsNullOrEmpty(accept))
            {
                hwr.Accept = accept;
            }

            if (!string.IsNullOrEmpty(connection))
            {
                hwr.Connection = connection;
            }

            if (!string.IsNullOrEmpty(contentType))
            {
                hwr.ContentType = contentType;
            }

            if (!string.IsNullOrEmpty(expect))
            {
                hwr.Expect = expect;
            }

            if (!string.IsNullOrEmpty(referrer))
            {
                hwr.Referer = referrer;
            }

            if (!string.IsNullOrEmpty(userAgent))
            {
                hwr.UserAgent = userAgent;
            }

            if (!string.IsNullOrEmpty(host))
            {
                hwr.Host = host;
            }
        }

        private Uri GetUri(string address)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));

            Uri? uri;
            if (_baseAddress != null)
            {
                if (!Uri.TryCreate(_baseAddress, address, out uri))
                {
                    return new Uri(Path.GetFullPath(address));
                }
            }
            else if (!Uri.TryCreate(address, UriKind.Absolute, out uri))
            {
                return new Uri(Path.GetFullPath(address));
            }

            return GetUri(uri);
        }

        private Uri GetUri(Uri address)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));

            Uri? uri = address;

            if (!address.IsAbsoluteUri && _baseAddress != null && !Uri.TryCreate(_baseAddress, address, out uri))
            {
                return address;
            }

            if (string.IsNullOrEmpty(uri.Query) && _requestParameters != null)
            {
                var sb = new StringBuilder();

                string delimiter = string.Empty;
                for (int i = 0; i < _requestParameters.Count; ++i)
                {
                    sb.Append(delimiter).Append(_requestParameters.AllKeys[i]).Append('=').Append(_requestParameters[i]);
                    delimiter = "&";
                }

                uri = new UriBuilder(uri) { Query = sb.ToString() }.Uri;
            }

            return uri;
        }

        private byte[]? DownloadBits(WebRequest request, Stream writeStream)
        {
            try
            {
                WebResponse response = _webResponse = GetWebResponse(request);

                long contentLength = response.ContentLength;
                byte[] copyBuffer = new byte[contentLength == -1 || contentLength > DefaultDownloadBufferLength ? DefaultDownloadBufferLength : contentLength];

                if (writeStream is ChunkedMemoryStream)
                {
                    if (contentLength > int.MaxValue)
                    {
                        throw new WebException(SR.net_webstatus_MessageLengthLimitExceeded, WebExceptionStatus.MessageLengthLimitExceeded);
                    }
                    writeStream.SetLength(copyBuffer.Length);
                }

                using (Stream readStream = response.GetResponseStream())
                {
                    if (readStream != null)
                    {
                        int bytesRead;
                        while ((bytesRead = readStream.Read(copyBuffer, 0, copyBuffer.Length)) != 0)
                        {
                            writeStream.Write(copyBuffer, 0, bytesRead);
                        }
                    }
                }

                return (writeStream as ChunkedMemoryStream)?.ToArray();
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                writeStream?.Close();
                AbortRequest(request);
                if (e is WebException || e is SecurityException) throw;
                throw new WebException(SR.net_webclient, e);
            }
        }

        private async void DownloadBitsAsync(
            WebRequest request, Stream writeStream,
            AsyncOperation asyncOp, Action<byte[]?, Exception?, AsyncOperation> completionDelegate)
        {
            Debug.Assert(_progress != null, "ProgressData should have been initialized");
            Debug.Assert(asyncOp != null);

            Exception? exception = null;
            try
            {
                WebResponse response = _webResponse = await GetWebResponseTaskAsync(request).ConfigureAwait(false);

                long contentLength = response.ContentLength;
                byte[] copyBuffer = new byte[contentLength == -1 || contentLength > DefaultDownloadBufferLength ? DefaultDownloadBufferLength : contentLength];

                if (writeStream is ChunkedMemoryStream)
                {
                    if (contentLength > int.MaxValue)
                    {
                        throw new WebException(SR.net_webstatus_MessageLengthLimitExceeded, WebExceptionStatus.MessageLengthLimitExceeded);
                    }
                    writeStream.SetLength(copyBuffer.Length);
                }

                if (contentLength >= 0)
                {
                    _progress.TotalBytesToReceive = contentLength;
                }

                using (writeStream)
                using (Stream readStream = response.GetResponseStream())
                {
                    if (readStream != null)
                    {
                        while (true)
                        {
                            int bytesRead = await readStream.ReadAsync(new Memory<byte>(copyBuffer)).ConfigureAwait(false);
                            if (bytesRead == 0)
                            {
                                break;
                            }

                            _progress.BytesReceived += bytesRead;
                            if (_progress.BytesReceived != _progress.TotalBytesToReceive)
                            {
                                PostProgressChanged(asyncOp, _progress);
                            }

                            await writeStream.WriteAsync(new ReadOnlyMemory<byte>(copyBuffer, 0, bytesRead)).ConfigureAwait(false);
                        }
                    }

                    if (_progress.TotalBytesToReceive < 0)
                    {
                        _progress.TotalBytesToReceive = _progress.BytesReceived;
                    }
                    PostProgressChanged(asyncOp, _progress);
                }

                completionDelegate((writeStream as ChunkedMemoryStream)?.ToArray(), null, asyncOp);
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                exception = GetExceptionToPropagate(e);
                AbortRequest(request);
                writeStream?.Close();
            }
            finally
            {
                if (exception != null)
                {
                    completionDelegate(null, exception, asyncOp);
                }
            }
        }

        private byte[] UploadBits(
            WebRequest request, Stream? readStream, byte[] buffer, int chunkSize,
            byte[]? header, byte[]? footer)
        {
            try
            {
                if (request.RequestUri.Scheme == Uri.UriSchemeFile)
                {
                    header = footer = null;
                }

                using (Stream writeStream = request.GetRequestStream())
                {
                    if (header != null)
                    {
                        writeStream.Write(header, 0, header.Length);
                    }

                    if (readStream != null)
                    {
                        using (readStream)
                        {
                            while (true)
                            {
                                int bytesRead = readStream.Read(buffer, 0, buffer.Length);
                                if (bytesRead <= 0)
                                    break;
                                writeStream.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                    else
                    {
                        for (int pos = 0; pos < buffer.Length;)
                        {
                            int toWrite = buffer.Length - pos;
                            if (chunkSize != 0 && toWrite > chunkSize)
                            {
                                toWrite = chunkSize;
                            }
                            writeStream.Write(buffer, pos, toWrite);
                            pos += toWrite;
                        }
                    }

                    if (footer != null)
                    {
                        writeStream.Write(footer, 0, footer.Length);
                    }
                }

                return DownloadBits(request, new ChunkedMemoryStream())!;
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                AbortRequest(request);
                if (e is WebException || e is SecurityException) throw;
                throw new WebException(SR.net_webclient, e);
            }
        }

        private async void UploadBitsAsync(
            WebRequest request, Stream? readStream, byte[] buffer, int chunkSize,
            byte[]? header, byte[]? footer,
            AsyncOperation asyncOp, Action<byte[]?, Exception?, AsyncOperation> completionDelegate)
        {
            Debug.Assert(asyncOp != null);
            Debug.Assert(_progress != null, "ProgressData should have been initialized");
            _progress.HasUploadPhase = true;

            Exception? exception = null;
            try
            {
                if (request.RequestUri.Scheme == Uri.UriSchemeFile)
                {
                    header = footer = null;
                }

                using (Stream writeStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
                {
                    if (header != null)
                    {
                        await writeStream.WriteAsync(new ReadOnlyMemory<byte>(header)).ConfigureAwait(false);
                        _progress.BytesSent += header.Length;
                        PostProgressChanged(asyncOp, _progress);
                    }

                    if (readStream != null)
                    {
                        using (readStream)
                        {
                            while (true)
                            {
                                int bytesRead = await readStream.ReadAsync(new Memory<byte>(buffer)).ConfigureAwait(false);
                                if (bytesRead <= 0) break;
                                await writeStream.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead)).ConfigureAwait(false);

                                _progress.BytesSent += bytesRead;
                                PostProgressChanged(asyncOp, _progress);
                            }
                        }
                    }
                    else
                    {
                        for (int pos = 0; pos < buffer.Length;)
                        {
                            int toWrite = buffer.Length - pos;
                            if (chunkSize != 0 && toWrite > chunkSize)
                            {
                                toWrite = chunkSize;
                            }
                            await writeStream.WriteAsync(new ReadOnlyMemory<byte>(buffer, pos, toWrite)).ConfigureAwait(false);
                            pos += toWrite;
                            _progress.BytesSent += toWrite;
                            PostProgressChanged(asyncOp, _progress);
                        }
                    }

                    if (footer != null)
                    {
                        await writeStream.WriteAsync(new ReadOnlyMemory<byte>(footer)).ConfigureAwait(false);
                        _progress.BytesSent += footer.Length;
                        PostProgressChanged(asyncOp, _progress);
                    }
                }

                DownloadBitsAsync(request, new ChunkedMemoryStream(), asyncOp, completionDelegate);
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                exception = GetExceptionToPropagate(e);
                AbortRequest(request);
            }
            finally
            {
                if (exception != null)
                {
                    completionDelegate(null, exception, asyncOp);
                }
            }
        }

        private static bool ByteArrayHasPrefix(byte[] prefix, byte[] byteArray)
        {
            if (prefix == null || byteArray == null || prefix.Length > byteArray.Length)
            {
                return false;
            }

            for (int i = 0; i < prefix.Length; i++)
            {
                if (prefix[i] != byteArray[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static readonly char[] s_parseContentTypeSeparators = new char[] { ';', '=', ' ' };
        private static readonly Encoding[] s_knownEncodings = { Encoding.UTF8, Encoding.UTF32, Encoding.Unicode, Encoding.BigEndianUnicode };

        private string GetStringUsingEncoding(WebRequest request, byte[] data)
        {
            Encoding? enc = null;
            int bomLengthInData = -1;

            // Figure out encoding by first checking for encoding string in Content-Type HTTP header
            // This can throw NotImplementedException if the derived class of WebRequest doesn't support it.
            string? contentType;
            try
            {
                contentType = request.ContentType;
            }
            catch (Exception e) when (e is NotImplementedException || e is NotSupportedException) // some types do this
            {
                contentType = null;
            }

            if (contentType != null)
            {
                contentType = contentType.ToLowerInvariant();
                string[] parsedList = contentType.Split(s_parseContentTypeSeparators);
                bool nextItem = false;
                foreach (string item in parsedList)
                {
                    if (item == "charset")
                    {
                        nextItem = true;
                    }
                    else if (nextItem)
                    {
                        try
                        {
                            enc = Encoding.GetEncoding(item);
                        }
                        catch (ArgumentException)
                        {
                            // Eat ArgumentException here.
                            // We'll assume that Content-Type encoding might have been garbled and wasn't present at all.
                            break;
                        }
                        // Unexpected exceptions are thrown back to caller
                    }
                }
            }

            // If no content encoding listed in the ContentType HTTP header, or no Content-Type header present, then
            // check for a byte-order-mark (BOM) in the data to figure out encoding.
            if (enc == null)
            {
                // UTF32 must be tested before Unicode because it's BOM is the same but longer.
                Encoding[] encodings = s_knownEncodings;
                for (int i = 0; i < encodings.Length; i++)
                {
                    byte[] preamble = encodings[i].GetPreamble();
                    if (ByteArrayHasPrefix(preamble, data))
                    {
                        enc = encodings[i];
                        bomLengthInData = preamble.Length;
                        break;
                    }
                }
            }

            // Do we have an encoding guess?  If not, use default.
            if (enc == null)
            {
                enc = Encoding;
            }

            // Calculate BOM length based on encoding guess.  Then check for it in the data.
            if (bomLengthInData == -1)
            {
                byte[] preamble = enc.GetPreamble();
                bomLengthInData = ByteArrayHasPrefix(preamble, data) ? preamble.Length : 0;
            }

            // Convert byte array to string stripping off any BOM before calling Format().
            // This is required since Format() doesn't handle stripping off BOM.
            return enc.GetString(data, bomLengthInData, data.Length - bomLengthInData);
        }

        private string MapToDefaultMethod(Uri address)
        {
            Uri uri = !address.IsAbsoluteUri && _baseAddress != null ?
                new Uri(_baseAddress, address) :
                address;

            return string.Equals(uri.Scheme, Uri.UriSchemeFtp, StringComparison.Ordinal) ?
                "STOR" :
                "POST";
        }

        [return: NotNullIfNotNull("str")]
        private static string? UrlEncode(string? str)
        {
            if (str == null)
                return null;
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return Encoding.ASCII.GetString(UrlEncodeBytesToBytesInternal(bytes, 0, bytes.Length, false));
        }

        private static byte[] UrlEncodeBytesToBytesInternal(byte[] bytes, int offset, int count, bool alwaysCreateReturnValue)
        {
            int cSpaces = 0;
            int cUnsafe = 0;

            // Count them first.
            for (int i = 0; i < count; i++)
            {
                char ch = (char)bytes[offset + i];

                if (ch == ' ')
                {
                    cSpaces++;
                }
                else if (!IsSafe(ch))
                {
                    cUnsafe++;
                }
            }

            // If nothing to expand.
            if (!alwaysCreateReturnValue && cSpaces == 0 && cUnsafe == 0)
                return bytes;

            // Expand not 'safe' characters into %XX, spaces to +.
            byte[] expandedBytes = new byte[count + cUnsafe * 2];
            int pos = 0;

            for (int i = 0; i < count; i++)
            {
                byte b = bytes[offset + i];
                char ch = (char)b;

                if (IsSafe(ch))
                {
                    expandedBytes[pos++] = b;
                }
                else if (ch == ' ')
                {
                    expandedBytes[pos++] = (byte)'+';
                }
                else
                {
                    expandedBytes[pos++] = (byte)'%';
                    expandedBytes[pos++] = (byte)HexConverter.ToCharLower(b >> 4);
                    expandedBytes[pos++] = (byte)HexConverter.ToCharLower(b);
                }
            }

            return expandedBytes;
        }

        private static bool IsSafe(char ch)
        {
            if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9')
            {
                return true;
            }

            switch (ch)
            {
                case '-':
                case '_':
                case '.':
                case '!':
                case '*':
                case '\'':
                case '(':
                case ')':
                    return true;
            }

            return false;
        }

        private void InvokeOperationCompleted(AsyncOperation asyncOp, SendOrPostCallback callback, AsyncCompletedEventArgs eventArgs)
        {
            if (Interlocked.CompareExchange(ref _asyncOp, null, asyncOp) == asyncOp)
            {
                EndOperation();
                asyncOp.PostOperationCompleted(callback, eventArgs);
            }
        }

        public void OpenReadAsync(Uri address) =>
            OpenReadAsync(address, null);

        public void OpenReadAsync(Uri address, object? userToken)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));

            AsyncOperation asyncOp = StartAsyncOperation(userToken);
            try
            {
                WebRequest request = _webRequest = GetWebRequest(GetUri(address));
                request.BeginGetResponse(iar =>
                {
                    Stream? stream = null;
                    Exception? exception = null;
                    try
                    {
                        WebResponse response = _webResponse = GetWebResponse(request, iar);
                        stream = response.GetResponseStream();
                    }
                    catch (Exception e) when (!(e is OutOfMemoryException))
                    {
                        exception = GetExceptionToPropagate(e);
                    }

                    InvokeOperationCompleted(asyncOp, _openReadOperationCompleted!, new OpenReadCompletedEventArgs(stream, exception, _canceled, asyncOp.UserSuppliedState));
                }, null);
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                InvokeOperationCompleted(asyncOp, _openReadOperationCompleted!,
                    new OpenReadCompletedEventArgs(null, GetExceptionToPropagate(e), _canceled, asyncOp.UserSuppliedState));
            }
        }

        public void OpenWriteAsync(Uri address) =>
            OpenWriteAsync(address, null, null);

        public void OpenWriteAsync(Uri address, string? method) =>
            OpenWriteAsync(address, method, null);

        public void OpenWriteAsync(Uri address, string? method, object? userToken)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));
            if (method == null)
            {
                method = MapToDefaultMethod(address);
            }

            AsyncOperation asyncOp = StartAsyncOperation(userToken);
            try
            {
                _method = method;
                WebRequest request = _webRequest = GetWebRequest(GetUri(address));
                request.BeginGetRequestStream(iar =>
                {
                    WebClientWriteStream? stream = null;
                    Exception? exception = null;

                    try
                    {
                        stream = new WebClientWriteStream(request.EndGetRequestStream(iar), request, this);
                    }
                    catch (Exception e) when (!(e is OutOfMemoryException))
                    {
                        exception = GetExceptionToPropagate(e);
                    }

                    InvokeOperationCompleted(asyncOp, _openWriteOperationCompleted!, new OpenWriteCompletedEventArgs(stream, exception, _canceled, asyncOp.UserSuppliedState));
                }, null);
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                var eventArgs = new OpenWriteCompletedEventArgs(null, GetExceptionToPropagate(e), _canceled, asyncOp.UserSuppliedState);
                InvokeOperationCompleted(asyncOp, _openWriteOperationCompleted!, eventArgs);
            }
        }

        private void DownloadStringAsyncCallback(byte[]? returnBytes, Exception? exception, object state)
        {
            AsyncOperation asyncOp = (AsyncOperation)state;
            string? stringData = null;
            try
            {
                if (returnBytes != null)
                {
                    stringData = GetStringUsingEncoding(_webRequest!, returnBytes);
                }
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                exception = GetExceptionToPropagate(e);
            }

            var eventArgs = new DownloadStringCompletedEventArgs(stringData, exception, _canceled, asyncOp.UserSuppliedState);
            InvokeOperationCompleted(asyncOp, _downloadStringOperationCompleted!, eventArgs);
        }


        public void DownloadStringAsync(Uri address) =>
            DownloadStringAsync(address, null);

        public void DownloadStringAsync(Uri address, object? userToken)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));

            AsyncOperation asyncOp = StartAsyncOperation(userToken);
            try
            {
                WebRequest request = _webRequest = GetWebRequest(GetUri(address));
                DownloadBitsAsync(request, new ChunkedMemoryStream(), asyncOp, DownloadStringAsyncCallback);
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                DownloadStringAsyncCallback(null, GetExceptionToPropagate(e), asyncOp);
            }
        }

        private void DownloadDataAsyncCallback(byte[]? returnBytes, Exception? exception, object state)
        {
            AsyncOperation asyncOp = (AsyncOperation)state;
            DownloadDataCompletedEventArgs eventArgs = new DownloadDataCompletedEventArgs(returnBytes, exception, _canceled, asyncOp.UserSuppliedState!);
            InvokeOperationCompleted(asyncOp, _downloadDataOperationCompleted!, eventArgs);
        }

        public void DownloadDataAsync(Uri address) =>
            DownloadDataAsync(address, null);

        public void DownloadDataAsync(Uri address, object? userToken)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));

            AsyncOperation asyncOp = StartAsyncOperation(userToken);
            try
            {
                WebRequest request = _webRequest = GetWebRequest(GetUri(address));
                DownloadBitsAsync(request, new ChunkedMemoryStream(), asyncOp, DownloadDataAsyncCallback);
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                DownloadDataAsyncCallback(null, GetExceptionToPropagate(e), asyncOp);
            }
        }

        private void DownloadFileAsyncCallback(byte[]? returnBytes, Exception? exception, object state)
        {
            AsyncOperation asyncOp = (AsyncOperation)state;
            AsyncCompletedEventArgs eventArgs = new AsyncCompletedEventArgs(exception, _canceled, asyncOp.UserSuppliedState);
            InvokeOperationCompleted(asyncOp, _downloadFileOperationCompleted!, eventArgs);
        }

        public void DownloadFileAsync(Uri address, string fileName) =>
            DownloadFileAsync(address, fileName, null);

        public void DownloadFileAsync(Uri address, string fileName, object? userToken)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));
            ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));

            FileStream? fs = null;
            AsyncOperation asyncOp = StartAsyncOperation(userToken);
            try
            {
                fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                WebRequest request = _webRequest = GetWebRequest(GetUri(address));
                DownloadBitsAsync(request, fs, asyncOp, DownloadFileAsyncCallback);
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                fs?.Close();
                DownloadFileAsyncCallback(null, GetExceptionToPropagate(e), asyncOp);
            }
        }

        public void UploadStringAsync(Uri address, string data) =>
            UploadStringAsync(address, null, data, null);

        public void UploadStringAsync(Uri address, string? method, string data) =>
            UploadStringAsync(address, method, data, null);

        public void UploadStringAsync(Uri address, string? method, string data, object? userToken)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));
            ArgumentNullException.ThrowIfNull(data, nameof(data));
            if (method == null)
            {
                method = MapToDefaultMethod(address);
            }

            AsyncOperation asyncOp = StartAsyncOperation(userToken);
            try
            {
                byte[] requestData = Encoding.GetBytes(data);
                _method = method;
                _contentLength = requestData.Length;
                WebRequest request = _webRequest = GetWebRequest(GetUri(address));

                UploadBitsAsync(
                    request, null, requestData, 0, null, null, asyncOp,
                    (bytesResult, error, uploadAsyncOp) =>
                    {
                        string? stringResult = null;
                        if (error == null && bytesResult != null)
                        {
                            try
                            {
                                stringResult = GetStringUsingEncoding(_webRequest, bytesResult);
                            }
                            catch (Exception e) when (!(e is OutOfMemoryException))
                            {
                                error = GetExceptionToPropagate(e);
                            }
                        }

                        InvokeOperationCompleted(uploadAsyncOp, _uploadStringOperationCompleted!,
                            new UploadStringCompletedEventArgs(stringResult, error, _canceled, uploadAsyncOp.UserSuppliedState));
                    });
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                var eventArgs = new UploadStringCompletedEventArgs(null, GetExceptionToPropagate(e), _canceled, asyncOp.UserSuppliedState);
                InvokeOperationCompleted(asyncOp, _uploadStringOperationCompleted!, eventArgs);
            }
        }

        public void UploadDataAsync(Uri address, byte[] data) =>
            UploadDataAsync(address, null, data, null);

        public void UploadDataAsync(Uri address, string? method, byte[] data) =>
            UploadDataAsync(address, method, data, null);

        public void UploadDataAsync(Uri address, string? method, byte[] data, object? userToken)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));
            ArgumentNullException.ThrowIfNull(data, nameof(data));
            if (method == null)
            {
                method = MapToDefaultMethod(address);
            }

            AsyncOperation asyncOp = StartAsyncOperation(userToken);
            try
            {
                _method = method;
                _contentLength = data.Length;
                WebRequest request = _webRequest = GetWebRequest(GetUri(address));

                int chunkSize = 0;
                if (UploadProgressChanged != null)
                {
                    // If ProgressCallback is requested, we should send the buffer in chunks
                    chunkSize = (int)Math.Min((long)DefaultCopyBufferLength, data.Length);
                }

                UploadBitsAsync(
                    request, null, data, chunkSize, null, null, asyncOp,
                    (result, error, uploadAsyncOp) =>
                        InvokeOperationCompleted(asyncOp, _uploadDataOperationCompleted!, new UploadDataCompletedEventArgs(result, error, _canceled, uploadAsyncOp.UserSuppliedState)));
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                var eventArgs = new UploadDataCompletedEventArgs(null, GetExceptionToPropagate(e), _canceled, asyncOp.UserSuppliedState);
                InvokeOperationCompleted(asyncOp, _uploadDataOperationCompleted!, eventArgs);
            }
        }

        public void UploadFileAsync(Uri address, string fileName) =>
            UploadFileAsync(address, null, fileName, null);

        public void UploadFileAsync(Uri address, string? method, string fileName) =>
            UploadFileAsync(address, method, fileName, null);

        public void UploadFileAsync(Uri address, string? method, string fileName, object? userToken)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));
            ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));
            if (method == null)
            {
                method = MapToDefaultMethod(address);
            }

            FileStream? fs = null;
            AsyncOperation asyncOp = StartAsyncOperation(userToken);
            try
            {
                _method = method;
                byte[]? formHeaderBytes = null, boundaryBytes = null, buffer = null;
                Uri uri = GetUri(address);
                bool needsHeaderAndBoundary = (uri.Scheme != Uri.UriSchemeFile);
                OpenFileInternal(needsHeaderAndBoundary, fileName, out fs, out buffer, ref formHeaderBytes, ref boundaryBytes);
                WebRequest request = _webRequest = GetWebRequest(uri);

                UploadBitsAsync(
                    request, fs, buffer, 0, formHeaderBytes, boundaryBytes, asyncOp,
                    (result, error, uploadAsyncOp) =>
                        InvokeOperationCompleted(asyncOp, _uploadFileOperationCompleted!, new UploadFileCompletedEventArgs(result, error, _canceled, uploadAsyncOp.UserSuppliedState)));
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                fs?.Close();
                var eventArgs = new UploadFileCompletedEventArgs(null, GetExceptionToPropagate(e), _canceled, asyncOp.UserSuppliedState);
                InvokeOperationCompleted(asyncOp, _uploadFileOperationCompleted!, eventArgs);
            }
        }

        public void UploadValuesAsync(Uri address, NameValueCollection data) =>
            UploadValuesAsync(address, null, data, null);

        public void UploadValuesAsync(Uri address, string? method, NameValueCollection data) =>
            UploadValuesAsync(address, method, data, null);

        public void UploadValuesAsync(Uri address, string? method, NameValueCollection data, object? userToken)
        {
            ArgumentNullException.ThrowIfNull(address, nameof(address));
            ArgumentNullException.ThrowIfNull(data, nameof(data));
            if (method == null)
            {
                method = MapToDefaultMethod(address);
            }

            AsyncOperation asyncOp = StartAsyncOperation(userToken);
            try
            {
                byte[] buffer = GetValuesToUpload(data);
                _method = method;
                WebRequest request = _webRequest = GetWebRequest(GetUri(address));

                int chunkSize = 0;
                if (UploadProgressChanged != null)
                {
                    // If ProgressCallback is requested, we should send the buffer in chunks
                    chunkSize = (int)Math.Min((long)DefaultCopyBufferLength, buffer.Length);
                }

                UploadBitsAsync(
                    request, null, buffer, chunkSize, null, null, asyncOp,
                    (result, error, uploadAsyncOp) =>
                        InvokeOperationCompleted(asyncOp, _uploadValuesOperationCompleted!, new UploadValuesCompletedEventArgs(result, error, _canceled, uploadAsyncOp.UserSuppliedState)));
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                var eventArgs = new UploadValuesCompletedEventArgs(null, GetExceptionToPropagate(e), _canceled, asyncOp.UserSuppliedState!);
                InvokeOperationCompleted(asyncOp, _uploadValuesOperationCompleted!, eventArgs);
            }
        }

        private static Exception GetExceptionToPropagate(Exception e) =>
            e is WebException || e is SecurityException ? e : new WebException(SR.net_webclient, e);

        public void CancelAsync()
        {
            WebRequest? request = _webRequest;
            _canceled = true;
            AbortRequest(request);
        }

        public Task<string> DownloadStringTaskAsync(string address) =>
            DownloadStringTaskAsync(GetUri(address));

        public Task<string> DownloadStringTaskAsync(Uri address)
        {
            // Create the task to be returned
            var tcs = new TaskCompletionSource<string>(address);

            DownloadStringCompletedEventHandler? handler = null;
            handler = (sender, e) => HandleCompletion(tcs, e, (args) => args.Result, handler, (webClient, completion) => webClient.DownloadStringCompleted -= completion);
            DownloadStringCompleted += handler;

            // Start the async operation.
            try { DownloadStringAsync(address, tcs); }
            catch
            {
                DownloadStringCompleted -= handler;
                throw;
            }

            // Return the task that represents the async operation
            return tcs.Task;
        }

        public Task<Stream> OpenReadTaskAsync(string address) =>
            OpenReadTaskAsync(GetUri(address));

        public Task<Stream> OpenReadTaskAsync(Uri address)
        {
            // Create the task to be returned
            var tcs = new TaskCompletionSource<Stream>(address);

            // Setup the callback event handler
            OpenReadCompletedEventHandler? handler = null;
            handler = (sender, e) => HandleCompletion(tcs, e, (args) => args.Result, handler, (webClient, completion) => webClient.OpenReadCompleted -= completion);
            OpenReadCompleted += handler;

            // Start the async operation.
            try { OpenReadAsync(address, tcs); }
            catch
            {
                OpenReadCompleted -= handler;
                throw;
            }

            // Return the task that represents the async operation
            return tcs.Task;
        }

        public Task<Stream> OpenWriteTaskAsync(string address) =>
            OpenWriteTaskAsync(GetUri(address), null);

        public Task<Stream> OpenWriteTaskAsync(Uri address) =>
            OpenWriteTaskAsync(address, null);

        public Task<Stream> OpenWriteTaskAsync(string address, string? method) =>
            OpenWriteTaskAsync(GetUri(address), method);

        public Task<Stream> OpenWriteTaskAsync(Uri address, string? method)
        {
            // Create the task to be returned
            var tcs = new TaskCompletionSource<Stream>(address);

            // Setup the callback event handler
            OpenWriteCompletedEventHandler? handler = null;
            handler = (sender, e) => HandleCompletion(tcs, e, (args) => args.Result, handler, (webClient, completion) => webClient.OpenWriteCompleted -= completion);
            OpenWriteCompleted += handler;

            // Start the async operation.
            try { OpenWriteAsync(address, method, tcs); }
            catch
            {
                OpenWriteCompleted -= handler;
                throw;
            }

            // Return the task that represents the async operation
            return tcs.Task;
        }

        public Task<string> UploadStringTaskAsync(string address, string data) =>
            UploadStringTaskAsync(address, null, data);

        public Task<string> UploadStringTaskAsync(Uri address, string data) =>
            UploadStringTaskAsync(address, null, data);

        public Task<string> UploadStringTaskAsync(string address, string? method, string data) =>
            UploadStringTaskAsync(GetUri(address), method, data);

        public Task<string> UploadStringTaskAsync(Uri address, string? method, string data)
        {
            // Create the task to be returned
            var tcs = new TaskCompletionSource<string>(address);

            // Setup the callback event handler
            UploadStringCompletedEventHandler? handler = null;
            handler = (sender, e) => HandleCompletion(tcs, e, (args) => args.Result, handler, (webClient, completion) => webClient.UploadStringCompleted -= completion);
            UploadStringCompleted += handler;

            // Start the async operation.
            try { UploadStringAsync(address, method, data, tcs); }
            catch
            {
                UploadStringCompleted -= handler;
                throw;
            }

            // Return the task that represents the async operation
            return tcs.Task;
        }

        public Task<byte[]> DownloadDataTaskAsync(string address) =>
            DownloadDataTaskAsync(GetUri(address));

        public Task<byte[]> DownloadDataTaskAsync(Uri address)
        {
            // Create the task to be returned
            var tcs = new TaskCompletionSource<byte[]>(address);

            // Setup the callback event handler
            DownloadDataCompletedEventHandler? handler = null;
            handler = (sender, e) => HandleCompletion(tcs, e, (args) => args.Result, handler, (webClient, completion) => webClient.DownloadDataCompleted -= completion);
            DownloadDataCompleted += handler;

            // Start the async operation.
            try { DownloadDataAsync(address, tcs); }
            catch
            {
                DownloadDataCompleted -= handler;
                throw;
            }

            // Return the task that represents the async operation
            return tcs.Task;
        }

        public Task DownloadFileTaskAsync(string address, string fileName) =>
            DownloadFileTaskAsync(GetUri(address), fileName);

        public Task DownloadFileTaskAsync(Uri address, string fileName)
        {
            // Create the task to be returned
            var tcs = new TaskCompletionSource<object?>(address);

            // Setup the callback event handler
            AsyncCompletedEventHandler? handler = null;
            handler = (sender, e) => HandleCompletion(tcs, e, (args) => null, handler, (webClient, completion) => webClient.DownloadFileCompleted -= completion);
            DownloadFileCompleted += handler;

            // Start the async operation.
            try { DownloadFileAsync(address, fileName, tcs); }
            catch
            {
                DownloadFileCompleted -= handler;
                throw;
            }

            // Return the task that represents the async operation
            return tcs.Task;
        }

        public Task<byte[]> UploadDataTaskAsync(string address, byte[] data) =>
            UploadDataTaskAsync(GetUri(address), null, data);

        public Task<byte[]> UploadDataTaskAsync(Uri address, byte[] data) =>
            UploadDataTaskAsync(address, null, data);

        public Task<byte[]> UploadDataTaskAsync(string address, string? method, byte[] data) =>
            UploadDataTaskAsync(GetUri(address), method, data);

        public Task<byte[]> UploadDataTaskAsync(Uri address, string? method, byte[] data)
        {
            // Create the task to be returned
            var tcs = new TaskCompletionSource<byte[]>(address);

            // Setup the callback event handler
            UploadDataCompletedEventHandler? handler = null;
            handler = (sender, e) => HandleCompletion(tcs, e, (args) => args.Result, handler, (webClient, completion) => webClient.UploadDataCompleted -= completion);
            UploadDataCompleted += handler;

            // Start the async operation.
            try { UploadDataAsync(address, method, data, tcs); }
            catch
            {
                UploadDataCompleted -= handler;
                throw;
            }

            // Return the task that represents the async operation
            return tcs.Task;
        }

        public Task<byte[]> UploadFileTaskAsync(string address, string fileName) =>
            UploadFileTaskAsync(GetUri(address), null, fileName);

        public Task<byte[]> UploadFileTaskAsync(Uri address, string fileName) =>
            UploadFileTaskAsync(address, null, fileName);

        public Task<byte[]> UploadFileTaskAsync(string address, string? method, string fileName) =>
            UploadFileTaskAsync(GetUri(address), method, fileName);

        public Task<byte[]> UploadFileTaskAsync(Uri address, string? method, string fileName)
        {
            // Create the task to be returned
            var tcs = new TaskCompletionSource<byte[]>(address);

            // Setup the callback event handler
            UploadFileCompletedEventHandler? handler = null;
            handler = (sender, e) => HandleCompletion(tcs, e, (args) => args.Result, handler, (webClient, completion) => webClient.UploadFileCompleted -= completion);
            UploadFileCompleted += handler;

            // Start the async operation.
            try { UploadFileAsync(address, method, fileName, tcs); }
            catch
            {
                UploadFileCompleted -= handler;
                throw;
            }

            // Return the task that represents the async operation
            return tcs.Task;
        }

        public Task<byte[]> UploadValuesTaskAsync(string address, NameValueCollection data) =>
            UploadValuesTaskAsync(GetUri(address), null, data);

        public Task<byte[]> UploadValuesTaskAsync(string address, string? method, NameValueCollection data) =>
            UploadValuesTaskAsync(GetUri(address), method, data);

        public Task<byte[]> UploadValuesTaskAsync(Uri address, NameValueCollection data) =>
            UploadValuesTaskAsync(address, null, data);

        public Task<byte[]> UploadValuesTaskAsync(Uri address, string? method, NameValueCollection data)
        {
            // Create the task to be returned
            var tcs = new TaskCompletionSource<byte[]>(address);

            // Setup the callback event handler
            UploadValuesCompletedEventHandler? handler = null;
            handler = (sender, e) => HandleCompletion(tcs, e, (args) => args.Result, handler, (webClient, completion) => webClient.UploadValuesCompleted -= completion);
            UploadValuesCompleted += handler;

            // Start the async operation.
            try { UploadValuesAsync(address, method, data, tcs); }
            catch
            {
                UploadValuesCompleted -= handler;
                throw;
            }

            // Return the task that represents the async operation
            return tcs.Task;
        }

        private void HandleCompletion<TAsyncCompletedEventArgs, TCompletionDelegate, T>(TaskCompletionSource<T> tcs, TAsyncCompletedEventArgs e, Func<TAsyncCompletedEventArgs, T> getResult, TCompletionDelegate handler, Action<WebClient, TCompletionDelegate> unregisterHandler)
            where TAsyncCompletedEventArgs : AsyncCompletedEventArgs
        {
            if (e.UserState == tcs)
            {
                try { unregisterHandler(this, handler); }
                finally
                {
                    if (e.Error != null) tcs.TrySetException(e.Error);
                    else if (e.Cancelled) tcs.TrySetCanceled();
                    else tcs.TrySetResult(getResult(e));
                }
            }
        }

        private void PostProgressChanged(AsyncOperation asyncOp, ProgressData progress)
        {
            if (asyncOp != null && (progress.BytesSent > 0 || progress.BytesReceived > 0))
            {
                int progressPercentage;
                if (progress.HasUploadPhase)
                {
                    if (UploadProgressChanged != null)
                    {
                        progressPercentage = progress.TotalBytesToReceive < 0 && progress.BytesReceived == 0 ?
                            progress.TotalBytesToSend < 0 ? 0 : progress.TotalBytesToSend == 0 ? 50 : (int)((50 * progress.BytesSent) / progress.TotalBytesToSend) :
                            progress.TotalBytesToSend < 0 ? 50 : progress.TotalBytesToReceive == 0 ? 100 : (int)((50 * progress.BytesReceived) / progress.TotalBytesToReceive + 50);
                        asyncOp.Post(_reportUploadProgressChanged!, new UploadProgressChangedEventArgs(progressPercentage, asyncOp.UserSuppliedState!, progress.BytesSent, progress.TotalBytesToSend, progress.BytesReceived, progress.TotalBytesToReceive));
                    }
                }
                else if (DownloadProgressChanged != null)
                {
                    progressPercentage = progress.TotalBytesToReceive < 0 ? 0 : progress.TotalBytesToReceive == 0 ? 100 : (int)((100 * progress.BytesReceived) / progress.TotalBytesToReceive);
                    asyncOp.Post(_reportDownloadProgressChanged!, new DownloadProgressChangedEventArgs(progressPercentage, asyncOp.UserSuppliedState!, progress.BytesReceived, progress.TotalBytesToReceive));
                }
            }
        }

        #region Supporting Types
        private sealed class ProgressData
        {
            internal long BytesSent;
            internal long TotalBytesToSend = -1;
            internal long BytesReceived;
            internal long TotalBytesToReceive = -1;
            internal bool HasUploadPhase;

            internal void Reset()
            {
                BytesSent = 0;
                TotalBytesToSend = -1;
                BytesReceived = 0;
                TotalBytesToReceive = -1;
                HasUploadPhase = false;
            }
        }

        private sealed class WebClientWriteStream : DelegatingStream
        {
            private readonly WebRequest _request;
            private readonly WebClient _webClient;

            public WebClientWriteStream(Stream stream, WebRequest request, WebClient webClient) : base(stream)
            {
                _request = request;
                _webClient = webClient;
            }

            protected override void Dispose(bool disposing)
            {
                try
                {
                    if (disposing)
                    {
                        _webClient.GetWebResponse(_request).Dispose();
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
        #endregion

        #region Obsolete designer support
        //introduced to support design-time loading of System.Windows.dll

        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool AllowReadStreamBuffering { get; set; }

        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool AllowWriteStreamBuffering { get; set; }

        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public event WriteStreamClosedEventHandler? WriteStreamClosed { add { } remove { } }

        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected virtual void OnWriteStreamClosed(WriteStreamClosedEventArgs e) { }
        #endregion
    }

    #region Delegates and supporting *CompletedEventArgs classes used by event-based async code
    public delegate void OpenReadCompletedEventHandler(object sender, OpenReadCompletedEventArgs e);
    public delegate void OpenWriteCompletedEventHandler(object sender, OpenWriteCompletedEventArgs e);
    public delegate void DownloadStringCompletedEventHandler(object sender, DownloadStringCompletedEventArgs e);
    public delegate void DownloadDataCompletedEventHandler(object sender, DownloadDataCompletedEventArgs e);
    public delegate void UploadStringCompletedEventHandler(object sender, UploadStringCompletedEventArgs e);
    public delegate void UploadDataCompletedEventHandler(object sender, UploadDataCompletedEventArgs e);
    public delegate void UploadFileCompletedEventHandler(object sender, UploadFileCompletedEventArgs e);
    public delegate void UploadValuesCompletedEventHandler(object sender, UploadValuesCompletedEventArgs e);
    public delegate void DownloadProgressChangedEventHandler(object sender, DownloadProgressChangedEventArgs e);
    public delegate void UploadProgressChangedEventHandler(object sender, UploadProgressChangedEventArgs e);
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void WriteStreamClosedEventHandler(object sender, WriteStreamClosedEventArgs e);

    public class OpenReadCompletedEventArgs : AsyncCompletedEventArgs
    {
        private readonly Stream? _result;

        internal OpenReadCompletedEventArgs(Stream? result, Exception? exception, bool cancelled, object? userToken) : base(exception, cancelled, userToken)
        {
            _result = result;
        }

        public Stream Result
        {
            get
            {
                RaiseExceptionIfNecessary();
                return _result!;
            }
        }
    }

    public class OpenWriteCompletedEventArgs : AsyncCompletedEventArgs
    {
        private readonly Stream? _result;

        internal OpenWriteCompletedEventArgs(Stream? result, Exception? exception, bool cancelled, object? userToken) : base(exception, cancelled, userToken)
        {
            _result = result;
        }

        public Stream Result
        {
            get
            {
                RaiseExceptionIfNecessary();
                return _result!;
            }
        }
    }

    public class DownloadStringCompletedEventArgs : AsyncCompletedEventArgs
    {
        private readonly string? _result;

        internal DownloadStringCompletedEventArgs(string? result, Exception? exception, bool cancelled, object? userToken) : base(exception, cancelled, userToken)
        {
            _result = result;
        }

        public string Result
        {
            get
            {
                RaiseExceptionIfNecessary();
                return _result!;
            }
        }

    }

    public class DownloadDataCompletedEventArgs : AsyncCompletedEventArgs
    {
        private readonly byte[]? _result;

        internal DownloadDataCompletedEventArgs(byte[]? result, Exception? exception, bool cancelled, object? userToken) : base(exception, cancelled, userToken)
        {
            _result = result;
        }

        public byte[] Result
        {
            get
            {
                RaiseExceptionIfNecessary();
                return _result!;
            }
        }
    }

    public class UploadStringCompletedEventArgs : AsyncCompletedEventArgs
    {
        private readonly string? _result;

        internal UploadStringCompletedEventArgs(string? result, Exception? exception, bool cancelled, object? userToken) : base(exception, cancelled, userToken)
        {
            _result = result;
        }

        public string Result
        {
            get
            {
                RaiseExceptionIfNecessary();
                return _result!;
            }
        }
    }

    public class UploadDataCompletedEventArgs : AsyncCompletedEventArgs
    {
        private readonly byte[]? _result;

        internal UploadDataCompletedEventArgs(byte[]? result, Exception? exception, bool cancelled, object? userToken) : base(exception, cancelled, userToken)
        {
            _result = result;
        }

        public byte[] Result
        {
            get
            {
                RaiseExceptionIfNecessary();
                return _result!;
            }
        }
    }

    public class UploadFileCompletedEventArgs : AsyncCompletedEventArgs
    {
        private readonly byte[]? _result;

        internal UploadFileCompletedEventArgs(byte[]? result, Exception? exception, bool cancelled, object? userToken) : base(exception, cancelled, userToken)
        {
            _result = result;
        }

        public byte[] Result
        {
            get
            {
                RaiseExceptionIfNecessary();
                return _result!;
            }
        }
    }

    public class UploadValuesCompletedEventArgs : AsyncCompletedEventArgs
    {
        private readonly byte[]? _result;

        internal UploadValuesCompletedEventArgs(byte[]? result, Exception? exception, bool cancelled, object? userToken) : base(exception, cancelled, userToken)
        {
            _result = result;
        }

        public byte[] Result
        {
            get
            {
                RaiseExceptionIfNecessary();
                return _result!;
            }
        }
    }

    public class DownloadProgressChangedEventArgs : ProgressChangedEventArgs
    {
        internal DownloadProgressChangedEventArgs(int progressPercentage, object? userToken, long bytesReceived, long totalBytesToReceive) :
            base(progressPercentage, userToken)
        {
            BytesReceived = bytesReceived;
            TotalBytesToReceive = totalBytesToReceive;
        }

        public long BytesReceived { get; }
        public long TotalBytesToReceive { get; }
    }


    public class UploadProgressChangedEventArgs : ProgressChangedEventArgs
    {
        internal UploadProgressChangedEventArgs(int progressPercentage, object? userToken, long bytesSent, long totalBytesToSend, long bytesReceived, long totalBytesToReceive) :
            base(progressPercentage, userToken)
        {
            BytesReceived = bytesReceived;
            TotalBytesToReceive = totalBytesToReceive;
            BytesSent = bytesSent;
            TotalBytesToSend = totalBytesToSend;
        }

        public long BytesReceived { get; }
        public long TotalBytesToReceive { get; }
        public long BytesSent { get; }
        public long TotalBytesToSend { get; }
    }


    [EditorBrowsable(EditorBrowsableState.Never)]
    public class WriteStreamClosedEventArgs : EventArgs
    {
        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public WriteStreamClosedEventArgs() { }

        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Exception? Error { get { return null; } }
    }
    #endregion
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace System.Net
{
    public class FileWebRequest : WebRequest, ISerializable
    {
        private readonly WebHeaderCollection _headers = new WebHeaderCollection();
        private string _method = WebRequestMethods.File.DownloadFile;
        private FileAccess _fileAccess = FileAccess.Read;
        private ManualResetEventSlim _blockReaderUntilRequestStreamDisposed;
        private WebResponse _response;
        private WebFileStream _stream;
        private Uri _uri;
        private long _contentLength;
        private int _timeout = DefaultTimeoutMilliseconds;
        private bool _readPending;
        private bool _writePending;
        private bool _writing;
        private bool _syncHint;
        private int _aborted;

        internal FileWebRequest(Uri uri)
        {
            if (uri.Scheme != (object)Uri.UriSchemeFile)
            {
                throw new ArgumentOutOfRangeException(nameof(uri));
            }

            _uri = uri;
        }

        [Obsolete("Serialization is obsoleted for this type. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected FileWebRequest(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext) =>
            GetObjectData(serializationInfo, streamingContext);

        protected override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        internal bool Aborted => _aborted != 0;

        public override string ConnectionGroupName { get; set; }

        public override long ContentLength
        {
            get { return _contentLength; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(SR.net_clsmall, nameof(value));
                }
                _contentLength = value;
            }
        }

        public override string ContentType
        {
            get { return _headers["Content-Type"]; }
            set { _headers["Content-Type"] = value; }
        }

        public override ICredentials Credentials { get; set; }

        public override WebHeaderCollection Headers => _headers;

        public override string Method
        {
            get { return _method; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(SR.net_badmethod, nameof(value));
                }
                _method = value;
            }
        }

        public override bool PreAuthenticate { get; set; }

        public override IWebProxy Proxy { get; set; }

        public override int Timeout
        {
            get { return _timeout; }
            set
            {
                if (value < 0 && value != System.Threading.Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.net_io_timeout_use_ge_zero);
                }
                _timeout = value;
            }
        }

        public override Uri RequestUri => _uri;

        private static Exception CreateRequestAbortedException() =>
            new WebException(SR.Format(SR.net_requestaborted, WebExceptionStatus.RequestCanceled), WebExceptionStatus.RequestCanceled);

        private void CheckAndMarkAsyncGetRequestStreamPending()
        {
            if (Aborted)
            {
                throw CreateRequestAbortedException();
            }

            if (string.Equals(_method, "GET", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_method, "HEAD", StringComparison.OrdinalIgnoreCase))
            {
                throw new ProtocolViolationException(SR.net_nouploadonget);
            }

            if (_response != null)
            {
                throw new InvalidOperationException(SR.net_reqsubmitted);
            }

            lock (this)
            {
                if (_writePending)
                {
                    throw new InvalidOperationException(SR.net_repcall);
                }
                _writePending = true;
            }
        }

        private Stream CreateWriteStream()
        {
            try
            {
                if (_stream == null)
                {
                    _stream = new WebFileStream(this, _uri.LocalPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    _fileAccess = FileAccess.Write;
                    _writing = true;
                }
                return _stream;
            }
            catch (Exception e) { throw new WebException(e.Message, e); }
        }

        public override IAsyncResult BeginGetRequestStream(AsyncCallback callback, object state)
        {
            CheckAndMarkAsyncGetRequestStreamPending();
            Task<Stream> t = Task.Factory.StartNew(s => ((FileWebRequest)s).CreateWriteStream(),
                this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            return TaskToApm.Begin(t, callback, state);
        }

        public override Task<Stream> GetRequestStreamAsync()
        {
            CheckAndMarkAsyncGetRequestStreamPending();
            return Task.Factory.StartNew(s =>
            {
                FileWebRequest thisRef = (FileWebRequest)s;
                Stream writeStream = thisRef.CreateWriteStream();
                thisRef._writePending = false;
                return writeStream;
            }, this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        private void CheckAndMarkAsyncGetResponsePending()
        {
            if (Aborted)
            {
                throw CreateRequestAbortedException();
            }

            lock (this)
            {
                if (_readPending)
                {
                    throw new InvalidOperationException(SR.net_repcall);
                }
                _readPending = true;
            }
        }

        private WebResponse CreateResponse()
        {
            if (_writePending || _writing)
            {
                lock (this)
                {
                    if (_writePending || _writing)
                    {
                        _blockReaderUntilRequestStreamDisposed = new ManualResetEventSlim();
                    }
                }
            }
            _blockReaderUntilRequestStreamDisposed?.Wait();

            try
            {
                return _response ?? (_response = new FileWebResponse(this, _uri, _fileAccess, !_syncHint));
            }
            catch (Exception e)
            {
                throw new WebException(e.Message, e);
            }
        }

        public override IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
        {
            CheckAndMarkAsyncGetResponsePending();
            Task<WebResponse> t = Task.Factory.StartNew(s => ((FileWebRequest)s).CreateResponse(),
                 this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            return TaskToApm.Begin(t, callback, state);
        }

        public override Task<WebResponse> GetResponseAsync()
        {
            CheckAndMarkAsyncGetResponsePending();
            return Task.Factory.StartNew(s =>
            {
                var thisRef = (FileWebRequest)s;
                WebResponse response = thisRef.CreateResponse();
                _readPending = false;
                return response;
            }, this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public override Stream EndGetRequestStream(IAsyncResult asyncResult)
        {
            Stream stream = TaskToApm.End<Stream>(asyncResult);
            _writePending = false;
            return stream;
        }

        public override WebResponse EndGetResponse(IAsyncResult asyncResult)
        {
            WebResponse response = TaskToApm.End<WebResponse>(asyncResult);
            _readPending = false;
            return response;
        }

        public override Stream GetRequestStream()
        {
            IAsyncResult result = BeginGetRequestStream(null, null);

            if (Timeout != Threading.Timeout.Infinite &&
                !result.IsCompleted &&
                (!result.AsyncWaitHandle.WaitOne(Timeout, false) || !result.IsCompleted))
            {
                _stream?.Close();
                throw new WebException(SR.net_webstatus_Timeout, WebExceptionStatus.Timeout);
            }

            return EndGetRequestStream(result);
        }

        public override WebResponse GetResponse()
        {
            _syncHint = true;
            IAsyncResult result = BeginGetResponse(null, null);

            if (Timeout != Threading.Timeout.Infinite &&
                !result.IsCompleted &&
                (!result.AsyncWaitHandle.WaitOne(Timeout, false) || !result.IsCompleted))
            {
                _response?.Close();
                throw new WebException(SR.net_webstatus_Timeout, WebExceptionStatus.Timeout);
            }

            return EndGetResponse(result);
        }

        internal void UnblockReader()
        {
            lock (this) { _blockReaderUntilRequestStreamDisposed?.Set(); }
            _writing = false;
        }

        public override bool UseDefaultCredentials
        {
            get { throw new NotSupportedException(SR.net_PropertyNotSupportedException); }
            set { throw new NotSupportedException(SR.net_PropertyNotSupportedException); }
        }

        public override void Abort()
        {
            if (Interlocked.Increment(ref _aborted) == 1)
            {
                _stream?.Abort();
                _response?.Close();
            }
        }
    }

    internal sealed class FileWebRequestCreator : IWebRequestCreate
    {
        public WebRequest Create(Uri uri) => new FileWebRequest(uri);
    }

    internal sealed class WebFileStream : FileStream
    {
        private readonly FileWebRequest _request;

        public WebFileStream(FileWebRequest request, string path, FileMode mode, FileAccess access, FileShare sharing) :
            base(path, mode, access, sharing)
        {
            _request = request;
        }

        public WebFileStream(FileWebRequest request, string path, FileMode mode, FileAccess access, FileShare sharing, int length, bool async) :
            base(path, mode, access, sharing, length, async)
        {
            _request = request;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    _request?.UnblockReader();
                }
            }
            finally { base.Dispose(disposing); }
        }

        internal void Abort() => SafeFileHandle.Close();

        public override int Read(byte[] buffer, int offset, int size)
        {
            CheckAborted();
            try
            {
                return base.Read(buffer, offset, size);
            }
            catch
            {
                CheckAborted();
                throw;
            }
        }

        public override void Write(byte[] buffer, int offset, int size)
        {
            CheckAborted();
            try
            {
                base.Write(buffer, offset, size);
            }
            catch
            {
                CheckAborted();
                throw;
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            CheckAborted();
            try
            {
                return base.BeginRead(buffer, offset, size, callback, state);
            }
            catch
            {
                CheckAborted();
                throw;
            }
        }

        public override int EndRead(IAsyncResult ar)
        {
            try
            {
                return base.EndRead(ar);
            }
            catch
            {
                CheckAborted();
                throw;
            }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            CheckAborted();
            try
            {
                return base.BeginWrite(buffer, offset, size, callback, state);
            }
            catch
            {
                CheckAborted();
                throw;
            }
        }

        public override void EndWrite(IAsyncResult ar)
        {
            try
            {
                base.EndWrite(ar);
            }
            catch
            {
                CheckAborted();
                throw;
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckAborted();
            try
            {
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            }
            catch
            {
                CheckAborted();
                throw;
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckAborted();
            try
            {
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            }
            catch
            {
                CheckAborted();
                throw;
            }
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            CheckAborted();
            try
            {
                return base.CopyToAsync(destination, bufferSize, cancellationToken);
            }
            catch
            {
                CheckAborted();
                throw;
            }
        }

        private void CheckAborted()
        {
            if (_request.Aborted)
            {
                throw new WebException(SR.Format(SR.net_requestaborted, WebExceptionStatus.RequestCanceled), WebExceptionStatus.RequestCanceled);
            }
        }
    }
}

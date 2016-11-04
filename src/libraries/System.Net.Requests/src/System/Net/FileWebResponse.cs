// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.Serialization;
using System.Globalization;

namespace System.Net
{
    [Serializable]
    public class FileWebResponse : WebResponse, ISerializable
    {
        private const int DefaultFileStreamBufferSize = 8192;
        private const string DefaultFileContentType = "application/octet-stream";

        private readonly long _contentLength;
        private readonly FileAccess _fileAccess;
        private readonly WebHeaderCollection _headers;
        private readonly Uri _uri;

        private Stream _stream;
        private bool _closed;

        internal FileWebResponse(FileWebRequest request, Uri uri, FileAccess access, bool useAsync)
        {
            try
            {
                _fileAccess = access;
                if (access == FileAccess.Write)
                {
                    _stream = Stream.Null;
                }
                else
                {
                    _stream = new WebFileStream(request, uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultFileStreamBufferSize, useAsync);
                    _contentLength = _stream.Length;
                }
                _headers = new WebHeaderCollection();
                _headers[HttpKnownHeaderNames.ContentLength] = _contentLength.ToString(NumberFormatInfo.InvariantInfo);
                _headers[HttpKnownHeaderNames.ContentType] = DefaultFileContentType;
                _uri = uri;
            }
            catch (Exception e)
            {
                throw new WebException(e.Message, e, WebExceptionStatus.ConnectFailure, null);
            }
        }

        [Obsolete("Serialization is obsoleted for this type. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected FileWebResponse(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
            _headers = (WebHeaderCollection)serializationInfo.GetValue("headers", typeof(WebHeaderCollection));
            _uri = (Uri)serializationInfo.GetValue("uri", typeof(Uri));
            _contentLength = serializationInfo.GetInt64("contentLength");
            _fileAccess = (FileAccess)serializationInfo.GetInt32("fileAccess");
        }

        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            GetObjectData(serializationInfo, streamingContext);
        }

        protected override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            serializationInfo.AddValue("headers", _headers, typeof(WebHeaderCollection));
            serializationInfo.AddValue("uri", _uri, typeof(Uri));
            serializationInfo.AddValue("contentLength", _contentLength);
            serializationInfo.AddValue("fileAccess", _fileAccess);
            base.GetObjectData(serializationInfo, streamingContext);
        }

        public override long ContentLength
        {
            get
            {
                CheckDisposed();
                return _contentLength;
            }
        }

        public override string ContentType
        {
            get
            {
                CheckDisposed();
                return DefaultFileContentType;
            }
        }

        public override WebHeaderCollection Headers
        {
            get
            {
                CheckDisposed();
                return _headers;
            }
        }

        public override bool SupportsHeaders => true;

        public override Uri ResponseUri
        {
            get
            {
                CheckDisposed();
                return _uri;
            }
        }

        private void CheckDisposed()
        {
            if (_closed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        public override void Close()
        {
            if (!_closed)
            {
                _closed = true;

                Stream chkStream = _stream;
                if (chkStream != null)
                {
                    chkStream.Close();
                    _stream = null;
                }
            }
        }

        public override Stream GetResponseStream()
        {
            CheckDisposed();
            return _stream;
        }
    }
}

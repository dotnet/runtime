// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    using System;
    using System.IO;
    using System.Security;
    using System.Collections;
    using System.Net;
    using System.Net.Cache;
    using System.Runtime.Versioning;
    using System.Net.Http;

    //
    // XmlDownloadManager
    //
    internal partial class XmlDownloadManager
    {
        private Hashtable _connections;

        internal Stream GetStream(Uri uri, ICredentials credentials, IWebProxy proxy,
            RequestCachePolicy cachePolicy)
        {
            if (uri.Scheme == "file")
            {
                return new FileStream(uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1);
            }
            else
            {
                return GetNonFileStream(uri, credentials, proxy, cachePolicy);
            }
        }

        private Stream GetNonFileStream(Uri uri, ICredentials credentials, IWebProxy proxy,
            RequestCachePolicy cachePolicy)
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, uri);

            lock (this)
            {
                if (_connections == null)
                {
                    _connections = new Hashtable();
                }
            }

            HttpResponseMessage resp = client.SendAsync(req).GetAwaiter().GetResult();

            Stream respStream = new MemoryStream();
            resp.Content.CopyToAsync(respStream);
            return respStream;
        }

        internal void Remove(string host)
        {
            lock (this)
            {
                OpenedHost openedHost = (OpenedHost)_connections[host];
                if (openedHost != null)
                {
                    if (--openedHost.nonCachedConnectionsCount == 0)
                    {
                        _connections.Remove(host);
                    }
                }
            }
        }
    }

    //
    // OpenedHost
    //
    internal class OpenedHost
    {
        internal int nonCachedConnectionsCount;
    }

    //
    // XmlRegisteredNonCachedStream
    //
    internal class XmlRegisteredNonCachedStream : Stream
    {
        protected Stream stream;
        private XmlDownloadManager _downloadManager;
        private string _host;

        internal XmlRegisteredNonCachedStream(Stream stream, XmlDownloadManager downloadManager, string host)
        {
            this.stream = stream;
            _downloadManager = downloadManager;
            _host = host;
        }

        ~XmlRegisteredNonCachedStream()
        {
            if (_downloadManager != null)
            {
                _downloadManager.Remove(_host);
            }
            stream = null;
            // The base class, Stream, provides its own finalizer
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && stream != null)
                {
                    if (_downloadManager != null)
                    {
                        _downloadManager.Remove(_host);
                    }
                    stream.Dispose();
                }
                stream = null;
                GC.SuppressFinalize(this); // do not call finalizer
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        //
        // Stream
        //
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return stream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return stream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return stream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            stream.EndWrite(asyncResult);
        }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            return stream.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            stream.WriteByte(value);
        }

        public override Boolean CanRead
        {
            get { return stream.CanRead; }
        }

        public override Boolean CanSeek
        {
            get { return stream.CanSeek; }
        }

        public override Boolean CanWrite
        {
            get { return stream.CanWrite; }
        }

        public override long Length
        {
            get { return stream.Length; }
        }

        public override long Position
        {
            get { return stream.Position; }
            set { stream.Position = value; }
        }
    }

    //
    // XmlCachedStream
    //
    internal class XmlCachedStream : MemoryStream
    {
        private const int MoveBufferSize = 4096;

        private Uri _uri;

        internal XmlCachedStream(Uri uri, Stream stream)
            : base()
        {
            _uri = uri;

            try
            {
                byte[] bytes = new byte[MoveBufferSize];
                int read = 0;
                while ((read = stream.Read(bytes, 0, MoveBufferSize)) > 0)
                {
                    this.Write(bytes, 0, read);
                }
                base.Position = 0;
            }
            finally
            {
                stream.Dispose();
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace System.Net.Tests
{
    // Utilities for generating URL prefixes for HttpListener
    internal class HttpListenerFactory : IDisposable
    {
        private readonly HttpListener _processPrefixListener;
        private readonly Exception _processPrefixException;
        private readonly string _processPrefix;
        public const string Hostname = "localhost";
        private readonly string _path;
        private readonly int _port;

        internal HttpListenerFactory()
        {
            // Find a URL prefix that is not in use on this machine *and* uses a port that's not in use.
            // Once we find this prefix, keep a listener on it for the duration of the process, so other processes
            // can't steal it.

            Guid processGuid = Guid.NewGuid();
            _path = processGuid.ToString("N");

            for (int port = 1024; port <= IPEndPoint.MaxPort; port++)
            {
                string prefix = $"http://{Hostname}:{port}/{_path}/";

                var listener = new HttpListener();
                try
                {
                    listener.Prefixes.Add(prefix);
                    listener.Start();

                    _processPrefixListener = listener;
                    _processPrefix = prefix;
                    _port = port;
                    break;
                }
                catch (Exception e)
                {
                    // can't use this prefix
                    listener.Close();

                    // Remember the exception for later
                    _processPrefixException = e;

                    // If this is not an HttpListenerException or SocketException, something very wrong has happened, and there's no point
                    // in trying again.
                    if (!(e is HttpListenerException) && !(e is SocketException))
                        break;
                }
            }

            // At this point, either we've reserved a prefix, or we've tried everything and failed.  If we failed,
            // we've saved the exception for later.  We'll defer actually *throwing* the exception until a test
            // asks for the prefix, because dealing with a type initialization exception is not nice in xunit.
        }

        public int Port
        {
            get
            {
                if (_port == 0)
                {
                    throw new Exception("Could not reserve a port for HttpListener", _processPrefixException);
                }

                return _port;
            }
        }

        public string ListeningUrl
        {
            get
            {
                if (_processPrefix == null)
                {
                    throw new Exception("Could not reserve a port for HttpListener", _processPrefixException);
                }

                return _processPrefix;
            }
        }

        public string Path => _path;

        public HttpListener GetListener() => _processPrefixListener;

        public void Dispose() => _processPrefixListener.Close();

        public Socket GetConnectedSocket()
        {
            // Some platforms or distributions require IPv6 sockets if the OS supports IPv6. Others (e.g. Ubunutu) don't.
            try
            {
                AddressFamily addressFamily = Socket.OSSupportsIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
                Socket socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(Hostname, _port);
                return socket;
            }
            catch
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(Hostname, _port);
                return socket;
            }            
        }

        public byte[] GetContent(string requestType, string query, string text, IEnumerable<string> headers, bool headerOnly)
        {
            Uri listeningUri = new Uri(ListeningUrl);
            string rawUrl = listeningUri.PathAndQuery;
            if (query != null)
            {
                rawUrl += query;
            }

            string content = $"{requestType} {rawUrl} HTTP/1.1\r\nHost: {listeningUri.Host}\r\n";
            if (text != null)
            {
                content += $"Content-Length: {text.Length}\r\n";
            }
            foreach (string header in headers ?? Enumerable.Empty<string>())
            {
                content += header + "\r\n";
            }
            content += "\r\n";

            if (!headerOnly && text != null)
            {
                content += text;
            }

            return Encoding.UTF8.GetBytes(content);
        }

        public byte[] GetContent(string requestType, string text, bool headerOnly)
        {
            return GetContent(requestType, query: null, text: text, headers: null, headerOnly: headerOnly);
        }
    }

    public static class RequestTypes
    {
        public const string POST = "POST";
    }
}

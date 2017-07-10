// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Net.Tests
{
    // Utilities for generating URL prefixes for HttpListener
    public class HttpListenerFactory : IDisposable
    {
        const int StartPort = 1025;
        const int MaxStartAttempts = IPEndPoint.MaxPort - StartPort + 1;
        private static readonly object s_nextPortLock = new object();
        private static int s_nextPort = StartPort;

        private readonly HttpListener _processPrefixListener;
        private readonly Exception _processPrefixException;
        private readonly string _processPrefix;
        private readonly string _hostname;
        private readonly string _path;
        private readonly int _port;

        internal HttpListenerFactory(string hostname = "localhost", string path = null)
        {
            // Find a URL prefix that is not in use on this machine *and* uses a port that's not in use.
            // Once we find this prefix, keep a listener on it for the duration of the process, so other processes
            // can't steal it.
            _hostname = hostname;
            _path = path ?? Guid.NewGuid().ToString("N");
            string pathComponent = string.IsNullOrEmpty(_path) ? _path : $"{_path}/";

            for (int attempt = 0; attempt < MaxStartAttempts; attempt++)
            {
                int port = GetNextPort();
                string prefix = $"http://{hostname}:{port}/{pathComponent}";

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

                    if (e is HttpListenerException listenerException)
                    {
                        // If we can't access the host (e.g. if it is '+' or '*' and the current user is the administrator)
                        // then throw.
                        const int ERROR_ACCESS_DENIED = 5;
                        if (listenerException.ErrorCode == ERROR_ACCESS_DENIED && (hostname == "*" || hostname == "+"))
                        {
                            throw new InvalidOperationException($"Access denied for host {hostname}");
                        }
                    }
                    else if (!(e is SocketException))
                    {
                        // If this is not an HttpListenerException or SocketException, something very wrong has happened, and there's no point
                        // in trying again.
                        break;
                    }
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

        public string Hostname => _hostname;
        public string Path => _path;

        private static bool? s_supportsWildcards;
        public static bool SupportsWildcards
        {
            get
            {
                if (!s_supportsWildcards.HasValue)
                {
                    try
                    {
                        using (new HttpListenerFactory("*"))
                        {
                            s_supportsWildcards = true;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        s_supportsWildcards = false;
                    }
                }

                return s_supportsWildcards.Value;
            }
        }

        public HttpListener GetListener() => _processPrefixListener ?? throw new Exception("Could not reserve a port for HttpListener", _processPrefixException);

        public void Dispose() => _processPrefixListener?.Close();

        public Socket GetConnectedSocket()
        {
            // Some platforms or distributions require IPv6 sockets if the OS supports IPv6. Others (e.g. Ubuntu) don't.
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

        public byte[] GetContent(string httpVersion, string requestType, string query, string text, IEnumerable<string> headers, bool headerOnly)
        {
            headers = headers ?? Enumerable.Empty<string>();

            Uri listeningUri = new Uri(ListeningUrl);
            string rawUrl = listeningUri.PathAndQuery;
            if (query != null)
            {
                rawUrl += query;
            }

            string content = $"{requestType} {rawUrl} HTTP/{httpVersion}\r\n";
            if (!headers.Any(header => header.ToLower().StartsWith("host:")))
            {
                content += $"Host: { listeningUri.Host}\r\n";
            }
            if (text != null && !headers.Any(header => header.ToLower().StartsWith("content-length:")))
            {
                content += $"Content-Length: {text.Length}\r\n";
            }
            foreach (string header in headers)
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
            return GetContent("1.1", requestType, query: null, text: text, headers: null, headerOnly: headerOnly);
        }

        private static int GetNextPort()
        {
            lock (s_nextPortLock)
            {
                int port = s_nextPort++;
                if (s_nextPort > IPEndPoint.MaxPort)
                {
                    s_nextPort = StartPort;
                }
                return port;
            }
        }
    }

    public static class RequestTypes
    {
        public const string POST = "POST";
    }
}

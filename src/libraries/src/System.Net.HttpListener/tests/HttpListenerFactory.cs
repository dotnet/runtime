// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;

namespace System.Net.Tests
{
    // Utilities for generating URL prefixes for HttpListener
    internal class HttpListenerFactory : IDisposable
    {
        private readonly HttpListener _processPrefixListener;
        private readonly Exception _processPrefixException;
        private readonly string _processPrefix;

        internal HttpListenerFactory()
        {
            // Find a URL prefix that is not in use on this machine *and* uses a port that's not in use.
            // Once we find this prefix, keep a listener on it for the duration of the process, so other processes
            // can't steal it.

            Guid processGuid = Guid.NewGuid();

            for (int port = 1024; port <= IPEndPoint.MaxPort; port++)
            {
                string prefix = $"http://localhost:{port}/{processGuid:N}/";

                var listener = new HttpListener();
                try
                {
                    listener.Prefixes.Add(prefix);
                    listener.Start();

                    _processPrefixListener = listener;
                    _processPrefix = prefix;
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

        public HttpListener GetListener() => _processPrefixListener;

        public void Dispose() => _processPrefixListener.Close();
    }
}

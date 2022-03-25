// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Net.HttpEndPointListener
//
// Author:
//  Gonzalo Paniagua Javier (gonzalo.mono@gmail.com)
//
// Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
// Copyright (c) 2012 Xamarin, Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net
{
    internal sealed class HttpEndPointListener
    {
        private readonly HttpListener _listener;
        private readonly IPEndPoint _endpoint;
        private readonly Socket _socket;
        private readonly Dictionary<HttpConnection, HttpConnection> _unregisteredConnections;
        private Dictionary<ListenerPrefix, HttpListener> _prefixes;
        private List<ListenerPrefix>? _unhandledPrefixes; // host = '*'
        private List<ListenerPrefix>? _allPrefixes;       // host = '+'
        private X509Certificate? _cert;
        private bool _secure;

        public HttpEndPointListener(HttpListener listener, IPAddress addr, int port, bool secure)
        {
            _listener = listener;

            if (secure)
            {
                _secure = secure;
                _cert = HttpListener.LoadCertificateAndKey (addr, port);
            }

            _endpoint = new IPEndPoint(addr, port);
            _socket = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(_endpoint);
            _socket.Listen(500);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.UserToken = this;
            args.Completed += OnAccept;
            Accept(args);

            _prefixes = new Dictionary<ListenerPrefix, HttpListener>();
            _unregisteredConnections = new Dictionary<HttpConnection, HttpConnection>();
        }

        internal HttpListener Listener
        {
            get { return _listener; }
        }

        private void Accept(SocketAsyncEventArgs e)
        {
            e.AcceptSocket = null;
            bool asyn;
            try
            {
                asyn = _socket.AcceptAsync(e);
            }
            catch (ObjectDisposedException)
            {
                // Once the listener starts running, it kicks off an async accept,
                // and each subsequent accept initiates the next async accept.  At
                // point if the listener is torn down, the socket will be disposed
                // and the AcceptAsync on the socket can fail with an ODE.  Far from
                // ideal, but for now just eat such exceptions.
                return;
            }
            if (!asyn)
            {
                ProcessAccept(e);
            }
        }

        private static void ProcessAccept(SocketAsyncEventArgs args)
        {
            HttpEndPointListener epl = (HttpEndPointListener)args.UserToken!;

            Socket? accepted = args.SocketError == SocketError.Success ? args.AcceptSocket : null;
            epl.Accept(args);

            if (accepted == null)
                return;

            if (epl._secure && epl._cert == null)
            {
                accepted.Close();
                return;
            }

            HttpConnection conn;
            try
            {
                conn = new HttpConnection(accepted, epl, epl._secure, epl._cert!);
            }
            catch
            {
                accepted.Close();
                return;
            }

            lock (epl._unregisteredConnections)
            {
                epl._unregisteredConnections[conn] = conn;
            }
            conn.BeginReadRequest();
        }

        private static void OnAccept(object? sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        internal void RemoveConnection(HttpConnection conn)
        {
            lock (_unregisteredConnections)
            {
                _unregisteredConnections.Remove(conn);
            }
        }

        public bool BindContext(HttpListenerContext context)
        {
            HttpListenerRequest req = context.Request;
            ListenerPrefix? prefix;
            HttpListener? listener = SearchListener(req.Url, out prefix);
            if (listener == null)
                return false;

            context._listener = listener;
            context.Connection.Prefix = prefix;
            return true;
        }

        public static void UnbindContext(HttpListenerContext context)
        {
            if (context == null || context.Request == null)
                return;

            context._listener!.UnregisterContext(context);
        }

        private HttpListener? SearchListener(Uri? uri, out ListenerPrefix? prefix)
        {
            prefix = null;
            if (uri == null)
                return null;

            string host = uri.Host;
            int port = uri.Port;
            string path = WebUtility.UrlDecode(uri.AbsolutePath);
            string pathSlash = path[path.Length - 1] == '/' ? path : path + "/";

            HttpListener? bestMatch = null;
            int bestLength = -1;

            if (host != null && host != "")
            {
                Dictionary<ListenerPrefix, HttpListener> localPrefixes = _prefixes;
                foreach (ListenerPrefix p in localPrefixes.Keys)
                {
                    string ppath = p.Path!;
                    if (ppath.Length < bestLength)
                        continue;

                    if (p.Host != host || p.Port != port)
                        continue;

                    if (path.StartsWith(ppath, StringComparison.Ordinal) || pathSlash.StartsWith(ppath, StringComparison.Ordinal))
                    {
                        bestLength = ppath.Length;
                        bestMatch = localPrefixes[p];
                        prefix = p;
                    }
                }
                if (bestLength != -1)
                    return bestMatch;
            }

            List<ListenerPrefix>? list = _unhandledPrefixes;
            bestMatch = MatchFromList(host, path, list, out prefix);

            if (path != pathSlash && bestMatch == null)
                bestMatch = MatchFromList(host, pathSlash, list, out prefix);

            if (bestMatch != null)
                return bestMatch;

            list = _allPrefixes;
            bestMatch = MatchFromList(host, path, list, out prefix);

            if (path != pathSlash && bestMatch == null)
                bestMatch = MatchFromList(host, pathSlash, list, out prefix);

            if (bestMatch != null)
                return bestMatch;

            return null;
        }

        private static HttpListener? MatchFromList(string? host, string path, List<ListenerPrefix>? list, out ListenerPrefix? prefix)
        {
            prefix = null;
            if (list == null)
                return null;

            HttpListener? bestMatch = null;
            int bestLength = -1;

            foreach (ListenerPrefix p in list)
            {
                string ppath = p.Path!;
                if (ppath.Length < bestLength)
                    continue;

                if (path.StartsWith(ppath, StringComparison.Ordinal))
                {
                    bestLength = ppath.Length;
                    bestMatch = p._listener;
                    prefix = p;
                }
            }

            return bestMatch;
        }

        private static void AddSpecial(List<ListenerPrefix> list, ListenerPrefix prefix)
        {
            if (list == null)
                return;

            foreach (ListenerPrefix p in list)
            {
                if (p.Path == prefix.Path)
                    throw new HttpListenerException((int)HttpStatusCode.BadRequest, SR.Format(SR.net_listener_already, prefix));
            }
            list.Add(prefix);
        }

        private static bool RemoveSpecial(List<ListenerPrefix> list, ListenerPrefix prefix)
        {
            if (list == null)
                return false;

            int c = list.Count;
            for (int i = 0; i < c; i++)
            {
                ListenerPrefix p = list[i];
                if (p.Path == prefix.Path)
                {
                    list.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        private void CheckIfRemove()
        {
            if (_prefixes.Count > 0)
                return;

            List<ListenerPrefix>? list = _unhandledPrefixes;
            if (list != null && list.Count > 0)
                return;

            list = _allPrefixes;
            if (list != null && list.Count > 0)
                return;

            HttpEndPointManager.RemoveEndPoint(this, _endpoint);
        }

        public void Close()
        {
            _socket.Close();
            lock (_unregisteredConnections)
            {
                // Clone the list because RemoveConnection can be called from Close
                var connections = new List<HttpConnection>(_unregisteredConnections.Keys);

                foreach (HttpConnection c in connections)
                    c.Close(true);
                _unregisteredConnections.Clear();
            }
        }

        public void AddPrefix(ListenerPrefix prefix, HttpListener listener)
        {
            List<ListenerPrefix>? current;
            List<ListenerPrefix> future;
            if (prefix.Host == "*")
            {
                do
                {
                    current = _unhandledPrefixes;
                    future = current != null ? new List<ListenerPrefix>(current) : new List<ListenerPrefix>();
                    prefix._listener = listener;
                    AddSpecial(future, prefix);
                } while (Interlocked.CompareExchange(ref _unhandledPrefixes, future, current) != current);
                return;
            }

            if (prefix.Host == "+")
            {
                do
                {
                    current = _allPrefixes;
                    future = current != null ? new List<ListenerPrefix>(current) : new List<ListenerPrefix>();
                    prefix._listener = listener;
                    AddSpecial(future, prefix);
                } while (Interlocked.CompareExchange(ref _allPrefixes, future, current) != current);
                return;
            }

            Dictionary<ListenerPrefix, HttpListener> prefs, p2;
            do
            {
                prefs = _prefixes;
                if (prefs.ContainsKey(prefix))
                {
                    throw new HttpListenerException((int)HttpStatusCode.BadRequest, SR.Format(SR.net_listener_already, prefix));
                }
                p2 = new Dictionary<ListenerPrefix, HttpListener>(prefs);
                p2[prefix] = listener;
            } while (Interlocked.CompareExchange(ref _prefixes, p2, prefs) != prefs);
        }

        public void RemovePrefix(ListenerPrefix prefix, HttpListener listener)
        {
            List<ListenerPrefix>? current;
            List<ListenerPrefix> future;
            if (prefix.Host == "*")
            {
                do
                {
                    current = _unhandledPrefixes;
                    future = current != null ? new List<ListenerPrefix>(current) : new List<ListenerPrefix>();
                    if (!RemoveSpecial(future, prefix))
                        break; // Prefix not found
                } while (Interlocked.CompareExchange(ref _unhandledPrefixes, future, current) != current);

                CheckIfRemove();
                return;
            }

            if (prefix.Host == "+")
            {
                do
                {
                    current = _allPrefixes;
                    future = current != null ? new List<ListenerPrefix>(current) : new List<ListenerPrefix>();
                    if (!RemoveSpecial(future, prefix))
                        break; // Prefix not found
                } while (Interlocked.CompareExchange(ref _allPrefixes, future, current) != current);
                CheckIfRemove();
                return;
            }

            Dictionary<ListenerPrefix, HttpListener> prefs, p2;
            do
            {
                prefs = _prefixes;
                if (!prefs.ContainsKey(prefix))
                    break;

                p2 = new Dictionary<ListenerPrefix, HttpListener>(prefs);
                p2.Remove(prefix);
            } while (Interlocked.CompareExchange(ref _prefixes, p2, prefs) != prefs);
            CheckIfRemove();
        }
    }
}

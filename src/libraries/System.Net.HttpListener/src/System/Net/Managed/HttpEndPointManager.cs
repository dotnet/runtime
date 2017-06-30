// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Net.HttpEndPointManager
//
// Author:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
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

using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;

namespace System.Net
{
    internal sealed class HttpEndPointManager
    {
        private static Dictionary<IPAddress, Dictionary<int, HttpEndPointListener>> s_ipEndPoints = new Dictionary<IPAddress, Dictionary<int, HttpEndPointListener>>();

        private HttpEndPointManager()
        {
        }

        public static void AddListener(HttpListener listener)
        {
            List<string> added = new List<string>();
            try
            {
                lock ((s_ipEndPoints as ICollection).SyncRoot)
                {
                    foreach (string prefix in listener.Prefixes)
                    {
                        AddPrefixInternal(prefix, listener);
                        added.Add(prefix);
                    }
                }
            }
            catch
            {
                foreach (string prefix in added)
                {
                    RemovePrefix(prefix, listener);
                }
                throw;
            }
        }

        public static void AddPrefix(string prefix, HttpListener listener)
        {
            lock ((s_ipEndPoints as ICollection).SyncRoot)
            {
                AddPrefixInternal(prefix, listener);
            }
        }

        private static void AddPrefixInternal(string p, HttpListener listener)
        {
            int start = p.IndexOf(':') + 3;
            int colon = p.IndexOf(':', start);
            if (colon != -1)
            {
                // root can't be -1 here, since we've already checked for ending '/' in ListenerPrefix.
                int root = p.IndexOf('/', colon, p.Length - colon);
                string portString = p.Substring(colon + 1, root - colon - 1);
                
                int port;
                if (!int.TryParse(portString, out port) || port <= 0 || port >= 65536)
                {
                    throw new HttpListenerException((int)HttpStatusCode.BadRequest, SR.net_invalid_port);
                }
            }

            ListenerPrefix lp = new ListenerPrefix(p);
            if (lp.Host != "*" && lp.Host != "+" && Uri.CheckHostName(lp.Host) == UriHostNameType.Unknown)
                throw new HttpListenerException((int)HttpStatusCode.BadRequest, SR.net_listener_host);

            if (lp.Path.IndexOf('%') != -1)
                throw new HttpListenerException((int)HttpStatusCode.BadRequest, SR.net_invalid_path);

            if (lp.Path.IndexOf("//", StringComparison.Ordinal) != -1)
                throw new HttpListenerException((int)HttpStatusCode.BadRequest, SR.net_invalid_path);

            // listens on all the interfaces if host name cannot be parsed by IPAddress.
            HttpEndPointListener epl = GetEPListener(lp.Host, lp.Port, listener, lp.Secure, out bool alreadyExists);
            if (alreadyExists)
            {
                throw new HttpListenerException(98, SR.Format(SR.net_listener_already, p));
            }
            epl.AddPrefix(lp, listener);
        }

        private static HttpEndPointListener GetEPListener(string host, int port, HttpListener listener, bool secure, out bool alreadyExists)
        {
            alreadyExists = false;

            IPAddress addr;
            if (host == "*")
                addr = IPAddress.Any;
            else if (IPAddress.TryParse(host, out addr) == false)
            {
                try
                {
                    IPHostEntry iphost = Dns.GetHostEntry(host);
                    if (iphost != null)
                        addr = iphost.AddressList[0];
                    else
                        addr = IPAddress.Any;
                }
                catch
                {
                    addr = IPAddress.Any;
                }
            }
            Dictionary<int, HttpEndPointListener> p = null;
            if (s_ipEndPoints.ContainsKey(addr))
            {
                p = s_ipEndPoints[addr];
            }
            else
            {
                p = new Dictionary<int, HttpEndPointListener>();
                s_ipEndPoints[addr] = p;
            }

            HttpEndPointListener epl = null;
            if (p.ContainsKey(port))
            {
                alreadyExists = true;
                epl = p[port];
            }
            else
            {
                try
                {
                    epl = new HttpEndPointListener(listener, addr, port, secure);
                }
                catch (SocketException ex)
                {
                    throw new HttpListenerException(ex.ErrorCode, ex.Message);
                }
                p[port] = epl;
            }

            return epl;
        }

        public static void RemoveEndPoint(HttpEndPointListener epl, IPEndPoint ep)
        {
            lock ((s_ipEndPoints as ICollection).SyncRoot)
            {
                Dictionary<int, HttpEndPointListener> p = null;
                p = s_ipEndPoints[ep.Address];
                p.Remove(ep.Port);
                if (p.Count == 0)
                {
                    s_ipEndPoints.Remove(ep.Address);
                }
                epl.Close();
            }
        }

        public static void RemoveListener(HttpListener listener)
        {
            lock ((s_ipEndPoints as ICollection).SyncRoot)
            {
                foreach (string prefix in listener.Prefixes)
                {
                    RemovePrefixInternal(prefix, listener);
                }
            }
        }

        public static void RemovePrefix(string prefix, HttpListener listener)
        {
            lock ((s_ipEndPoints as ICollection).SyncRoot)
            {
                RemovePrefixInternal(prefix, listener);
            }
        }

        private static void RemovePrefixInternal(string prefix, HttpListener listener)
        {
            ListenerPrefix lp = new ListenerPrefix(prefix);
            if (lp.Path.IndexOf('%') != -1)
                return;

            if (lp.Path.IndexOf("//", StringComparison.Ordinal) != -1)
                return;

            HttpEndPointListener epl = GetEPListener(lp.Host, lp.Port, listener, lp.Secure, out bool ignored);
            epl.RemovePrefix(lp, listener);
        }
    }
}

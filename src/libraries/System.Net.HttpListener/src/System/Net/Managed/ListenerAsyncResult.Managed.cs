// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Net.ListenerAsyncResult
//
// Authors:
//  Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (c) 2005 Ximian, Inc (http://www.ximian.com)
//

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

using System.Runtime.ExceptionServices;
using System.Threading;

namespace System.Net
{
    internal sealed class ListenerAsyncResult : IAsyncResult
    {
        private ManualResetEvent? _handle;
        private bool _synch;
        private bool _completed;
        private AsyncCallback? _cb;
        private object? _state;
        private Exception? _exception;
        private HttpListenerContext? _context;
        private object _locker = new object();
        private ListenerAsyncResult? _forward;
        internal readonly HttpListener _parent;
        internal bool _endCalled;
        internal bool _inGet;

        public ListenerAsyncResult(HttpListener parent, AsyncCallback? cb, object? state)
        {
            _parent = parent;
            _cb = cb;
            _state = state;
        }

        internal void Complete(Exception exc)
        {
            if (_forward != null)
            {
                _forward.Complete(exc);
                return;
            }
            _exception = exc;
            if (_inGet && (exc is ObjectDisposedException))
                _exception = new HttpListenerException((int)HttpStatusCode.InternalServerError, SR.net_listener_close);
            lock (_locker)
            {
                _completed = true;
                if (_handle != null)
                    _handle.Set();

                if (_cb != null)
                    ThreadPool.UnsafeQueueUserWorkItem(s_invokeCB, this);
            }
        }

        private static WaitCallback s_invokeCB = new WaitCallback(InvokeCallback!);
        private static void InvokeCallback(object o)
        {
            ListenerAsyncResult ares = (ListenerAsyncResult)o!;
            if (ares._forward != null)
            {
                InvokeCallback(ares._forward);
                return;
            }
            try
            {
                ares._cb!(ares);
            }
            catch
            {
            }
        }

        internal void Complete(HttpListenerContext context)
        {
            Complete(context, false);
        }

        internal void Complete(HttpListenerContext context, bool synch)
        {
            if (_forward != null)
            {
                _forward.Complete(context, synch);
                return;
            }
            _synch = synch;
            _context = context;
            lock (_locker)
            {
                bool authFailure = false;
                try
                {
                    context.AuthenticationSchemes = context._listener!.SelectAuthenticationScheme(context);
                }
                catch (OutOfMemoryException oom)
                {
                    context.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                    _exception = oom;
                }
                catch
                {
                    authFailure = true;
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }

                if (context.AuthenticationSchemes != AuthenticationSchemes.None &&
                    (context.AuthenticationSchemes & AuthenticationSchemes.Anonymous) != AuthenticationSchemes.Anonymous &&
                    (context.AuthenticationSchemes & AuthenticationSchemes.Basic) != AuthenticationSchemes.Basic)
                {
                    authFailure = true;
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                }
                else if (context.AuthenticationSchemes == AuthenticationSchemes.Basic)
                {
                    HttpStatusCode errorCode = HttpStatusCode.Unauthorized;
                    string? authHeader = context.Request.Headers["Authorization"];
                    if (authHeader == null ||
                        !HttpListenerContext.IsBasicHeader(authHeader) ||
                        authHeader.Length < AuthenticationTypes.Basic.Length + 2 ||
                        !HttpListenerContext.TryParseBasicAuth(authHeader.Substring(AuthenticationTypes.Basic.Length + 1), out errorCode, out _, out _))
                    {
                        authFailure = true;
                        context.Response.StatusCode = (int)errorCode;
                        if (errorCode == HttpStatusCode.Unauthorized)
                        {
                            context.Response.Headers["WWW-Authenticate"] = context.AuthenticationSchemes + " realm=\"" + context._listener!.Realm + "\"";
                        }
                    }
                }

                if (authFailure)
                {
                    context.Response.OutputStream.Close();
                    IAsyncResult ares = context._listener!.BeginGetContext(_cb, _state);
                    _forward = (ListenerAsyncResult)ares;
                    lock (_forward._locker)
                    {
                        if (_handle != null)
                            _forward._handle = _handle;
                    }
                    ListenerAsyncResult next = _forward;
                    for (int i = 0; next._forward != null; i++)
                    {
                        if (i > 20)
                            Complete(new HttpListenerException((int)HttpStatusCode.Unauthorized, SR.net_listener_auth_errors));
                        next = next._forward;
                    }
                }
                else
                {
                    _completed = true;
                    _synch = false;

                    if (_handle != null)
                        _handle.Set();

                    if (_cb != null)
                        ThreadPool.UnsafeQueueUserWorkItem(s_invokeCB, this);
                }
            }
        }

        internal HttpListenerContext? GetContext()
        {
            if (_forward != null)
            {
                return _forward.GetContext();
            }

            if (_exception != null)
            {
                ExceptionDispatchInfo.Throw(_exception);
            }

            return _context;
        }

        public object? AsyncState
        {
            get
            {
                if (_forward != null)
                    return _forward.AsyncState;
                return _state;
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (_forward != null)
                    return _forward.AsyncWaitHandle;

                lock (_locker)
                {
                    if (_handle == null)
                        _handle = new ManualResetEvent(_completed);
                }

                return _handle;
            }
        }

        public bool CompletedSynchronously
        {
            get
            {
                if (_forward != null)
                    return _forward.CompletedSynchronously;
                return _synch;
            }
        }

        public bool IsCompleted
        {
            get
            {
                if (_forward != null)
                    return _forward.IsCompleted;

                lock (_locker)
                {
                    return _completed;
                }
            }
        }
    }
}

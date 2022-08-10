// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;

namespace System.Net
{
    public sealed unsafe partial class HttpListener
    {
        public static bool IsSupported => true;

        private Dictionary<HttpListenerContext, HttpListenerContext> _listenerContexts = new Dictionary<HttpListenerContext, HttpListenerContext>();
        private List<HttpListenerContext> _contextQueue = new List<HttpListenerContext>();
        private List<ListenerAsyncResult> _asyncWaitQueue = new List<ListenerAsyncResult>();
        private Dictionary<HttpConnection, HttpConnection> _connections = new Dictionary<HttpConnection, HttpConnection>();
        private bool _unsafeConnectionNtlmAuthentication;

        public HttpListenerTimeoutManager TimeoutManager
        {
            get
            {
                CheckDisposed();
                return _timeoutManager;
            }
        }

        private void AddPrefixCore(string uriPrefix) => HttpEndPointManager.AddPrefix(uriPrefix, this);

        private void RemovePrefixCore(string uriPrefix) => HttpEndPointManager.RemovePrefix(uriPrefix, this);

        public void Start()
        {
            lock (_internalLock)
            {
                try
                {
                    CheckDisposed();
                    if (_state == State.Started)
                        return;

                    HttpEndPointManager.AddListener(this);

                    _state = State.Started;
                }
                catch (Exception exception)
                {
                    _state = State.Closed;
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"Start {exception}");
                    throw;
                }
            }
        }

        public bool UnsafeConnectionNtlmAuthentication
        {
            // NTLM isn't currently supported, so this is a nop anyway and we can just roundtrip the value
            get => _unsafeConnectionNtlmAuthentication;
            set
            {
                CheckDisposed();
                _unsafeConnectionNtlmAuthentication = value;
            }
        }

        public void Stop()
        {

            lock (_internalLock)
            {
                try
                {
                    CheckDisposed();
                    if (_state == State.Stopped)
                    {
                        return;
                    }

                    Close(false);
                }
                catch (Exception exception)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"Stop {exception}");
                    throw;
                }
                finally
                {
                    _state = State.Stopped;
                }
            }
        }

        public void Abort()
        {

            lock (_internalLock)
            {
                try
                {
                    if (_state == State.Closed)
                    {
                        return;
                    }

                    // Just detach and free resources. Don't call Stop (which may throw).
                    if (_state == State.Started)
                    {
                        Close(true);
                    }
                }
                catch (Exception exception)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"Abort {exception}");
                    throw;
                }
                finally
                {
                    _state = State.Closed;
                }
            }
        }

        private void Dispose()
        {

            lock (_internalLock)
            {
                try
                {
                    if (_state == State.Closed)
                    {
                        return;
                    }

                    Close(true);
                }
                catch (Exception exception)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"Dispose {exception}");
                    throw;
                }
                finally
                {
                    _state = State.Closed;
                }
            }
        }

        private void Close(bool force)
        {
            CheckDisposed();
            HttpEndPointManager.RemoveListener(this);
            Cleanup(force);
        }

        internal void UnregisterContext(HttpListenerContext context)
        {
            lock ((_listenerContexts as ICollection).SyncRoot)
            {
                _listenerContexts.Remove(context);
            }
            lock ((_contextQueue as ICollection).SyncRoot)
            {
                int idx = _contextQueue.IndexOf(context);
                if (idx >= 0)
                    _contextQueue.RemoveAt(idx);
            }
        }

        internal void AddConnection(HttpConnection cnc)
        {
            lock ((_connections as ICollection).SyncRoot)
            {
                _connections[cnc] = cnc;
            }
        }

        internal void RemoveConnection(HttpConnection cnc)
        {
            lock ((_connections as ICollection).SyncRoot)
            {
                _connections.Remove(cnc);
            }
        }

        internal void RegisterContext(HttpListenerContext context)
        {
            lock ((_listenerContexts as ICollection).SyncRoot)
            {
                _listenerContexts[context] = context;
            }

            ListenerAsyncResult? ares = null;
            lock ((_asyncWaitQueue as ICollection).SyncRoot)
            {
                if (_asyncWaitQueue.Count == 0)
                {
                    lock ((_contextQueue as ICollection).SyncRoot)
                        _contextQueue.Add(context);
                }
                else
                {
                    ares = _asyncWaitQueue[0];
                    _asyncWaitQueue.RemoveAt(0);
                }
            }

            ares?.Complete(context);
        }

        private void Cleanup(bool close_existing)
        {
            lock ((_listenerContexts as ICollection).SyncRoot)
            {
                if (close_existing)
                {
                    // Need to copy this since closing will call UnregisterContext
                    ICollection keys = _listenerContexts.Keys;
                    var all = new HttpListenerContext[keys.Count];
                    keys.CopyTo(all, 0);
                    _listenerContexts.Clear();
                    for (int i = all.Length - 1; i >= 0; i--)
                        all[i].Connection.Close(true);
                }

                lock ((_connections as ICollection).SyncRoot)
                {
                    ICollection keys = _connections.Keys;
                    var conns = new HttpConnection[keys.Count];
                    keys.CopyTo(conns, 0);
                    _connections.Clear();
                    for (int i = conns.Length - 1; i >= 0; i--)
                        conns[i].Close(true);
                }
                lock ((_contextQueue as ICollection).SyncRoot)
                {
                    var ctxs = (HttpListenerContext[])_contextQueue.ToArray();
                    _contextQueue.Clear();
                    for (int i = ctxs.Length - 1; i >= 0; i--)
                        ctxs[i].Connection.Close(true);
                }

                lock ((_asyncWaitQueue as ICollection).SyncRoot)
                {
                    Exception exc = new ObjectDisposedException("listener");
                    foreach (ListenerAsyncResult ares in _asyncWaitQueue)
                    {
                        ares.Complete(exc);
                    }
                    _asyncWaitQueue.Clear();
                }
            }
        }

        private HttpListenerContext? GetContextFromQueue()
        {
            lock ((_contextQueue as ICollection).SyncRoot)
            {
                if (_contextQueue.Count == 0)
                {
                    return null;
                }

                HttpListenerContext context = _contextQueue[0];
                _contextQueue.RemoveAt(0);

                return context;
            }
        }

        public IAsyncResult BeginGetContext(AsyncCallback? callback, object? state)
        {
            CheckDisposed();
            if (_state != State.Started)
            {
                throw new InvalidOperationException(SR.Format(SR.net_listener_mustcall, "Start()"));
            }

            ListenerAsyncResult ares = new ListenerAsyncResult(this, callback, state);

            // lock wait_queue early to avoid race conditions
            lock ((_asyncWaitQueue as ICollection).SyncRoot)
            {
                lock ((_contextQueue as ICollection).SyncRoot)
                {
                    HttpListenerContext? ctx = GetContextFromQueue();
                    if (ctx != null)
                    {
                        ares.Complete(ctx, true);
                        return ares;
                    }
                }

                _asyncWaitQueue.Add(ares);
            }

            return ares;
        }

        public HttpListenerContext EndGetContext(IAsyncResult asyncResult)
        {
            CheckDisposed();
            ArgumentNullException.ThrowIfNull(asyncResult);

            ListenerAsyncResult? ares = asyncResult as ListenerAsyncResult;
            if (ares == null || !ReferenceEquals(this, ares._parent))
            {
                throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));
            }
            if (ares._endCalled)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, nameof(EndGetContext)));
            }

            ares._endCalled = true;

            if (!ares.IsCompleted)
                ares.AsyncWaitHandle.WaitOne();

            lock ((_asyncWaitQueue as ICollection).SyncRoot)
            {
                int idx = _asyncWaitQueue.IndexOf(ares);
                if (idx >= 0)
                    _asyncWaitQueue.RemoveAt(idx);
            }

            HttpListenerContext context = ares.GetContext()!;
            context.ParseAuthentication(context.AuthenticationSchemes);
            return context;
        }

        internal AuthenticationSchemes SelectAuthenticationScheme(HttpListenerContext context)
        {
            return AuthenticationSchemeSelectorDelegate != null ? AuthenticationSchemeSelectorDelegate(context.Request) : _authenticationScheme;
        }

        public HttpListenerContext GetContext()
        {
            CheckDisposed();
            if (_state == State.Stopped)
            {
                throw new InvalidOperationException(SR.Format(SR.net_listener_mustcall, "Start()"));
            }
            if (_prefixes.Count == 0)
            {
                throw new InvalidOperationException(SR.Format(SR.net_listener_mustcall, "AddPrefix()"));
            }

            ListenerAsyncResult ares = (ListenerAsyncResult)BeginGetContext(null, null);
            ares._inGet = true;

            return EndGetContext(ares);
        }
    }
}

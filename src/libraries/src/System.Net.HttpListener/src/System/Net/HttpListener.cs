// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Security.Authentication.ExtendedProtection;
using System.Threading.Tasks;

namespace System.Net
{
    public sealed unsafe partial class HttpListener : IDisposable
    {
        public delegate ExtendedProtectionPolicy ExtendedProtectionSelector(HttpListenerRequest request);

        private readonly object _internalLock;
        private volatile State _state; // _state is set only within lock blocks, but often read outside locks. 
        private HttpListenerPrefixCollection _prefixes;
        internal Hashtable _uriPrefixes = new Hashtable();
        private bool _ignoreWriteExceptions;
        private ServiceNameStore _defaultServiceNames;
        private HttpListenerTimeoutManager _timeoutManager;
        private ExtendedProtectionPolicy _extendedProtectionPolicy;
        private AuthenticationSchemeSelector _authenticationDelegate;
        private AuthenticationSchemes _authenticationScheme = AuthenticationSchemes.Anonymous;
        private ExtendedProtectionSelector _extendedProtectionSelectorDelegate;
        private string _realm;

        internal ICollection PrefixCollection => _uriPrefixes.Keys;

        public HttpListener()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            _state = State.Stopped;
            _internalLock = new object();
            _defaultServiceNames = new ServiceNameStore();

            _timeoutManager = new HttpListenerTimeoutManager(this);
            _prefixes = new HttpListenerPrefixCollection(this);

            // default: no CBT checks on any platform (appcompat reasons); applies also to PolicyEnforcement 
            // config element
            _extendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        public AuthenticationSchemeSelector AuthenticationSchemeSelectorDelegate
        {
            get
            {
                return _authenticationDelegate;
            }
            set
            {
                CheckDisposed();
                _authenticationDelegate = value;
            }
        }

        public ExtendedProtectionSelector ExtendedProtectionSelectorDelegate
        {
            get
            {
                return _extendedProtectionSelectorDelegate;
            }
            set
            {
                CheckDisposed();
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _extendedProtectionSelectorDelegate = value;
            }
        }

        public AuthenticationSchemes AuthenticationSchemes
        {
            get
            {
                return _authenticationScheme;
            }
            set
            {
                CheckDisposed();
                _authenticationScheme = value;
            }
        }

        public ExtendedProtectionPolicy ExtendedProtectionPolicy
        {
            get
            {
                return _extendedProtectionPolicy;
            }
            set
            {
                CheckDisposed();
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.CustomChannelBinding != null)
                {
                    throw new ArgumentException(SR.net_listener_cannot_set_custom_cbt, nameof(value));
                }

                _extendedProtectionPolicy = value;
            }
        }

        public ServiceNameCollection DefaultServiceNames
        {
            get
            {
                return _defaultServiceNames.ServiceNames;
            }
        }

        public string Realm
        {
            get
            {
                return _realm;
            }
            set
            {
                CheckDisposed();
                _realm = value;
            }
        }

        public bool IsListening => _state == State.Started;

        public bool IgnoreWriteExceptions
        {
            get
            {
                return _ignoreWriteExceptions;
            }
            set
            {
                CheckDisposed();
                _ignoreWriteExceptions = value;
            }
        }

        public Task<HttpListenerContext> GetContextAsync()
        {
            return Task.Factory.FromAsync(
                (callback, state) => ((HttpListener)state).BeginGetContext(callback, state),
                iar => ((HttpListener)iar.AsyncState).EndGetContext(iar),
                this);
        }

        public void Close()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, nameof(Close));
            try
            {
                if (NetEventSource.IsEnabled) NetEventSource.Info("HttpListenerRequest::Close()");
                ((IDisposable)this).Dispose();
            }
            catch (Exception exception)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"Close {exception}");
                throw;
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }

        internal void CheckDisposed()
        {
            if (_state == State.Closed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        private enum State
        {
            Stopped,
            Started,
            Closed,
        }

        void IDisposable.Dispose()
        {
            Dispose();
        }
    }
}

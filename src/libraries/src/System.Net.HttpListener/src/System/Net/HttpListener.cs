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
        private readonly HttpListenerPrefixCollection _prefixes;
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
            get => _authenticationDelegate;
            set
            {
                CheckDisposed();
                _authenticationDelegate = value;
            }
        }

        public ExtendedProtectionSelector ExtendedProtectionSelectorDelegate
        {
            get => _extendedProtectionSelectorDelegate;
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
            get => _authenticationScheme;
            set
            {
                CheckDisposed();
                _authenticationScheme = value;
            }
        }

        public ExtendedProtectionPolicy ExtendedProtectionPolicy
        {
            get => _extendedProtectionPolicy;
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

        public ServiceNameCollection DefaultServiceNames => _defaultServiceNames.ServiceNames;

        public HttpListenerPrefixCollection Prefixes
        {
            get
            {
                if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
                CheckDisposed();
                if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
                return _prefixes;
            }
        }

        internal void AddPrefix(string uriPrefix)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, $"uriPrefix:{uriPrefix}");
            string registeredPrefix = null;
            try
            {
                if (uriPrefix == null)
                {
                    throw new ArgumentNullException(nameof(uriPrefix));
                }
                CheckDisposed();
                int i;
                if (string.Compare(uriPrefix, 0, "http://", 0, 7, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    i = 7;
                }
                else if (string.Compare(uriPrefix, 0, "https://", 0, 8, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    i = 8;
                }
                else
                {
                    throw new ArgumentException(SR.net_listener_scheme, nameof(uriPrefix));
                }
                bool inSquareBrakets = false;
                int j = i;
                while (j < uriPrefix.Length && uriPrefix[j] != '/' && (uriPrefix[j] != ':' || inSquareBrakets))
                {
                    if (uriPrefix[j] == '[')
                    {
                        if (inSquareBrakets)
                        {
                            j = i;
                            break;
                        }
                        inSquareBrakets = true;
                    }
                    if (inSquareBrakets && uriPrefix[j] == ']')
                    {
                        inSquareBrakets = false;
                    }
                    j++;
                }
                if (i == j)
                {
                    throw new ArgumentException(SR.net_listener_host, nameof(uriPrefix));
                }
                if (uriPrefix[uriPrefix.Length - 1] != '/')
                {
                    throw new ArgumentException(SR.net_listener_slash, nameof(uriPrefix));
                }
                registeredPrefix = uriPrefix[j] == ':' ? String.Copy(uriPrefix) : uriPrefix.Substring(0, j) + (i == 7 ? ":80" : ":443") + uriPrefix.Substring(j);
                fixed (char* pChar = registeredPrefix)
                {
                    i = 0;
                    while (pChar[i] != ':')
                    {
                        pChar[i] = (char)CaseInsensitiveAscii.AsciiToLower[(byte)pChar[i]];
                        i++;
                    }
                }
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"mapped uriPrefix: {uriPrefix} to registeredPrefix: {registeredPrefix}");
                if (_state == State.Started)
                {
                    AddPrefixCore(registeredPrefix);
                }
                _uriPrefixes[uriPrefix] = registeredPrefix;
                _defaultServiceNames.Add(uriPrefix);
            }
            catch (Exception exception)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, exception);
                throw;
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this, $"prefix: {registeredPrefix}");
            }
        }
        
        internal bool ContainsPrefix(string uriPrefix) => _uriPrefixes.Contains(uriPrefix);

        internal bool RemovePrefix(string uriPrefix)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, $"uriPrefix: {uriPrefix}");
            try
            {
                CheckDisposed();
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"uriPrefix: {uriPrefix}");
                if (uriPrefix == null)
                {
                    throw new ArgumentNullException(nameof(uriPrefix));
                }

                if (!_uriPrefixes.Contains(uriPrefix))
                {
                    return false;
                }

                if (_state == State.Started)
                {
                    RemovePrefixCore((string)_uriPrefixes[uriPrefix]);
                }

                _uriPrefixes.Remove(uriPrefix);
                _defaultServiceNames.Remove(uriPrefix);
            }
            catch (Exception exception)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, exception);
                throw;
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this, $"uriPrefix: {uriPrefix}");
            }
            return true;
        }

        internal void RemoveAll(bool clear)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            try
            {
                CheckDisposed();
                // go through the uri list and unregister for each one of them
                if (_uriPrefixes.Count > 0)
                {
                    if (_state == State.Started)
                    {
                        foreach (string registeredPrefix in _uriPrefixes.Values)
                        {
                            RemovePrefixCore(registeredPrefix);
                        }
                    }

                    if (clear)
                    {
                        _uriPrefixes.Clear();
                        _defaultServiceNames.Clear();
                    }
                }
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }

        public string Realm
        {
            get => _realm;
            set
            {
                CheckDisposed();
                _realm = value;
            }
        }

        public bool IsListening => _state == State.Started;

        public bool IgnoreWriteExceptions
        {
            get => _ignoreWriteExceptions;
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
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private enum State
        {
            Stopped,
            Started,
            Closed,
        }

        void IDisposable.Dispose() => Dispose();
    }
}

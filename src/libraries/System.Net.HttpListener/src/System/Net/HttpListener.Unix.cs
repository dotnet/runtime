// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Security.Authentication.ExtendedProtection;
using System.Threading.Tasks;

namespace System.Net
{
    // TODO: #13187
    public sealed unsafe partial class HttpListener
    {
        public static bool IsSupported
        {
            get
            {
                return false;
            }
        }

        public HttpListener()
        {
            throw new PlatformNotSupportedException();
        }

        internal void CheckDisposed()
        {
            throw new PlatformNotSupportedException();
        }

        internal ICollection PrefixCollection
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        internal void AddPrefix(string uriPrefix)
        {
            throw new PlatformNotSupportedException();
        }

        internal bool ContainsPrefix(string uriPrefix)
        {
            throw new PlatformNotSupportedException();
        }

        internal bool RemovePrefix(string uriPrefix)
        {
            throw new PlatformNotSupportedException();
        }

        internal void RemoveAll(bool clear)
        {
            throw new PlatformNotSupportedException();
        }

        public AuthenticationSchemeSelector AuthenticationSchemeSelectorDelegate
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public ExtendedProtectionSelector ExtendedProtectionSelectorDelegate
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public AuthenticationSchemes AuthenticationSchemes
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public ExtendedProtectionPolicy ExtendedProtectionPolicy
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public ServiceNameCollection DefaultServiceNames
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public string Realm
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public HttpListenerTimeoutManager TimeoutManager
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public bool IsListening
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public bool IgnoreWriteExceptions
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public bool UnsafeConnectionNtlmAuthentication
        {
            get
            {
                throw new PlatformNotSupportedException();
            }

            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public HttpListenerPrefixCollection Prefixes
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public void Start()
        {
            throw new PlatformNotSupportedException();
        }

        public void Stop()
        {
            throw new PlatformNotSupportedException();
        }

        public void Abort()
        {
            throw new PlatformNotSupportedException();
        }

        private void Dispose(bool disposing)
        {
            throw new NotImplementedException();
        }

        public HttpListenerContext GetContext()
        {
            throw new PlatformNotSupportedException();
        }

        public IAsyncResult BeginGetContext(AsyncCallback callback, object state)
        {
            throw new PlatformNotSupportedException();
        }

        public HttpListenerContext EndGetContext(IAsyncResult asyncResult)
        {
            throw new PlatformNotSupportedException();
        }

        public Task<HttpListenerContext> GetContextAsync()
        {
            throw new PlatformNotSupportedException();
        }

        private void Dispose() { }
    }
}
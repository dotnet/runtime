// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    [Obsolete("This class has been deprecated. Please use WebRequest.DefaultWebProxy instead to access and set the global default proxy. Use 'null' instead of GetEmptyWebProxy. https://go.microsoft.com/fwlink/?linkid=14202")]
    public class GlobalProxySelection
    {
        // This defers to WebRequest.DefaultWebProxy, but returns EmptyWebProxy instead of null.
        public static IWebProxy Select
        {
            get
            {
                IWebProxy proxy = WebRequest.DefaultWebProxy;
                if (proxy == null)
                {
                    proxy = GetEmptyWebProxy();
                }
                return proxy;
            }

            set
            {
                WebRequest.DefaultWebProxy = value;
            }
        }

        public static IWebProxy GetEmptyWebProxy()
        {
            return new EmptyWebProxy();
        }

        private sealed class EmptyWebProxy : IWebProxy
        {
            private ICredentials _credentials;

            public EmptyWebProxy()
            {
            }

            public Uri GetProxy(Uri uri)
            {
                return uri;
            }

            public bool IsBypassed(Uri uri)
            {
                return true; // no proxy, always bypasses
            }

            public ICredentials Credentials
            {
                get
                {
                    return _credentials;
                }
                set
                {
                    _credentials = value; // doesn't do anything, but doesn't break contract either
                }
            }
        }
    }
}

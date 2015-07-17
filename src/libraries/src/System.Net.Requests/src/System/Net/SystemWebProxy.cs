// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Net
{
    // Singleton proxy object representing the System proxy
    // that is determined by per-user registry settings.  These
    // are the proxy settings that Internet Explorer and Wininet
    // use to look up a suitable proxy server. This object is the
    // initial default value of WebRequest.DefaultWebProxy.
    //
    // This object is only used as a sentinel to provide backward
    // compatibility for developers using the WebRequest.DefaultWebProxy
    // object. Developers use the DefaultWebProxy object either explicitly
    // by referencing it or implicitly when expecting .NET Desktop behaviors
    // when used with HttpWebRequest or HttpClientHandler.
    internal class SystemWebProxy : IWebProxy
    {
        private static IWebProxy s_systemWebProxy = null;
        private static bool s_systemWebProxyInitialized = false;
        private static object s_lockObject = new object();

        private ICredentials _credentials = null;

        private SystemWebProxy()
        {
        }

        public static IWebProxy Get()
        {
            if (!s_systemWebProxyInitialized)
            {
                lock (s_lockObject)
                {
                    if (!s_systemWebProxyInitialized)
                    {
                        s_systemWebProxy = new SystemWebProxy();
                        s_systemWebProxyInitialized = true;
                    }
                }
            }

            return s_systemWebProxy;
        }

        public ICredentials Credentials
        {
            get
            {
                return _credentials;
            }
            set
            {
                _credentials = value;
            }
        }

        // This is a sentinel object and can't support the GetProxy or IsBypassed
        // methods directly. Our .NET Core and .NET Native code will handle this exception
        // and call into WinInet/WinHttp as appropriate to use the system proxy.
        public Uri GetProxy(Uri destination)
        {
            throw new PlatformNotSupportedException();
        }

        public bool IsBypassed(Uri host)
        {
            throw new PlatformNotSupportedException();
        }
    }
}

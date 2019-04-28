// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    internal static class SystemProxyInfo
    {
        // On Windows we get default proxy configuration from either environment variables or the Windows system proxy.
        public static IWebProxy ConstructSystemProxy()
        {
            if (!HttpEnvironmentProxy.TryCreate(out IWebProxy proxy))
            {
                HttpSystemProxy.TryCreate(out proxy);
            }

            return proxy;
        }
    }
}

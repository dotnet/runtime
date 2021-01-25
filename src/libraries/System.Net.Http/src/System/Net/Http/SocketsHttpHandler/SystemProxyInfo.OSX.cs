// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    internal static partial class SystemProxyInfo
    {
        // On OSX we get default proxy configuration from either environment variables or the OSX system proxy.
        public static IWebProxy ConstructSystemProxy()
        {
            return HttpEnvironmentProxy.TryCreate(out IWebProxy? proxy) ? proxy : new MacProxy();
        }
    }
}

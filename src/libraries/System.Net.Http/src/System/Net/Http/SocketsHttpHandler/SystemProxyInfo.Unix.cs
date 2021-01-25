// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    internal static partial class SystemProxyInfo
    {
        // On Unix (except for OSX) we get default proxy configuration from environment variables. If the
        // environment variables are not defined, we return an IWebProxy object that effectively is
        // the "no proxy" object.
        public static IWebProxy ConstructSystemProxy()
        {
            return HttpEnvironmentProxy.TryCreate(out IWebProxy? proxy) ? proxy : new HttpNoProxy();
        }
    }
}

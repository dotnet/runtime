// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    internal static class SystemProxyInfo
    {
        // On Unix (except for OSX) we get default proxy configuration from environment variables.
        public static IWebProxy ConstructSystemProxy()
        {
            return HttpEnvironmentProxy.TryCreate(out IWebProxy proxy) ? proxy : null;
        }
    }
}

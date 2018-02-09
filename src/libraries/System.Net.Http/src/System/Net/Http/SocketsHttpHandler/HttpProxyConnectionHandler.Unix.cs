// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    internal sealed partial class HttpProxyConnectionHandler : HttpMessageHandler
    {
        // On Unix we get default proxy configuration from environment variables
        private static IWebProxy ConstructSystemProxy()
        {
            return HttpEnvironmentProxy.TryToCreate();
        }
    }
}


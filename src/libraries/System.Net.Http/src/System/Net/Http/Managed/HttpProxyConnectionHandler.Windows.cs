// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    internal sealed partial class HttpProxyConnectionHandler : HttpMessageHandler
    {
        private static IWebProxy ConstructSystemProxy()
        {
            // Windows normally does not use environment but it has system
            // configuration. That has not been implemented yet.
            // TODO #23150: windows portion
            return null;
        }
    }
}


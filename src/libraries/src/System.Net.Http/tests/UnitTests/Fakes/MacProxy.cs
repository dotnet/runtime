// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    internal sealed class MacProxy : IWebProxy
    {
        public ICredentials Credentials
        {
            get => throw NotImplemented.ByDesignWithMessage("Mac Proxy not implemented");
            set => throw NotImplemented.ByDesignWithMessage("Mac Proxy not implemented");
        }

        public Uri GetProxy(Uri targetUri)
        {
            throw NotImplemented.ByDesignWithMessage("Mac Proxy not implemented");
        }

        public bool IsBypassed(Uri targetUri)
        {
            throw NotImplemented.ByDesignWithMessage("Mac Proxy not implemented");
        }
    }
}

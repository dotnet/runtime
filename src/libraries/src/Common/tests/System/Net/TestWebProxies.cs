// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Test.Common
{
    internal sealed class UseSpecifiedUriWebProxy : IWebProxy
    {
        private readonly Uri _uri;
        private readonly bool _bypass;

        public UseSpecifiedUriWebProxy(Uri uri, ICredentials credentials = null, bool bypass = false)
        {
            _uri = uri;
            _bypass = bypass;
            Credentials = credentials;
        }

        public ICredentials Credentials { get; set; }
        public Uri GetProxy(Uri destination) => _uri;
        public bool IsBypassed(Uri host) => _bypass;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Net
{
    [Obsolete("This class has been deprecated. Please use WebRequest.DefaultWebProxy instead to access and set the global default proxy. Use 'null' instead of GetEmptyWebProxy. https://go.microsoft.com/fwlink/?linkid=14202")]
    public class GlobalProxySelection
    {
        // This defers to WebRequest.DefaultWebProxy, but returns EmptyWebProxy instead of null.
        [AllowNull]
        public static IWebProxy Select
        {
            get => WebRequest.DefaultWebProxy ?? GetEmptyWebProxy();
            set => WebRequest.DefaultWebProxy = value;
        }

        public static IWebProxy GetEmptyWebProxy() => new EmptyWebProxy();

        private sealed class EmptyWebProxy : IWebProxy
        {
            private ICredentials? _credentials;

            public EmptyWebProxy() { }

            public Uri GetProxy(Uri uri) => uri;

            public bool IsBypassed(Uri uri) => true; // no proxy, always bypasses

            public ICredentials? Credentials
            {
                get => _credentials;
                set => _credentials = value; // doesn't do anything, but doesn't break contract either
            }
        }
    }
}

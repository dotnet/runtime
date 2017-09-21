// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Net.Http.Functional.Tests
{
    public partial class HttpClientHandler_SslProtocols_Test
    {
        private static bool BackendSupportsSslConfiguration => true;

        private static bool SSLv3DisabledByDefault => true;
    }
}

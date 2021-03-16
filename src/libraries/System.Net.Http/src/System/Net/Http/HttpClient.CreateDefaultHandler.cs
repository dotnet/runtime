// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    public partial class HttpClient
    {
        private static HttpMessageHandler CreateDefaultHandler()
        {
            return new HttpClientHandler();
        }
    }
}

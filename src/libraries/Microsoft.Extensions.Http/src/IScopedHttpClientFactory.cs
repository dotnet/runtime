// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    public interface IScopedHttpClientFactory
    {
        HttpClient CreateClient(string name);
    }
}

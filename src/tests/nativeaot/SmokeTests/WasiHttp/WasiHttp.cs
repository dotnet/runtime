// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;

using HttpClient client = new HttpClient();
try
{
    // Nothing listens on port 1, so a request that reaches wasi:http/outgoing-handler fails to connect.
    using HttpResponseMessage response = await client.GetAsync("http://127.0.0.1:1/");
    Console.WriteLine($"Unexpected response: {(int)response.StatusCode}");
    return 1;
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Expected {nameof(HttpRequestException)}: {ex.Message}");
    return 100;
}

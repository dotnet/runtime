// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

var handler = new SocketsHttpHandler();
handler.SslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true;

var client = new HttpClient(handler);

var urls = new[]
{
    "https://self-signed.badssl.com",
    "https://wrong.host.badssl.com",
    "https://microsoft.com",
};

var allSucceeded = true;
foreach (var url in urls)
{
    var response = await client.GetAsync(url);
    Console.WriteLine($"{url} -> {response.StatusCode}");

    allSucceeded &= response.IsSuccessStatusCode;
}

return allSucceeded ? 42 : 1;

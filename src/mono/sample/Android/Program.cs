// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

var handler = new SocketsHttpHandler();
handler.SslOptions.RemoteCertificateValidationCallback =
    (sender, certificate, chain, errors) =>
    {
        Console.WriteLine("Validation callback called.");
        Console.WriteLine($"  sender: {sender}");
        Console.WriteLine($"  certificate: {certificate}");
        Console.WriteLine($"  chain: {chain}");
        Console.WriteLine($"  errors: {errors}");

        var ret = true;
        Console.WriteLine($"Returning {ret}");
        return ret;
    };

var client = new HttpClient(handler);
var responseB = await client.GetAsync("https://self-signed.badssl.com");
Console.WriteLine(responseB);

return 42;

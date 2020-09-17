// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks; 
using System.Net; 
using System.Net.Http;

public class AsyncStepClass
{
    static HttpClient client = new HttpClient();
    public static async Task TestAsyncStepOut()
    {
        await TestAsyncStepOut2("foobar");
    }

    public static async Task<int> TestAsyncStepOut2(string some)
    {
        var resp = await client.GetAsync("http://localhost:9400/debugger-driver.html");
        Console.WriteLine($"resp: {resp}"); /// BP at this line

        return 10;
    }
}
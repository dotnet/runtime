// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading.Tasks;

var client = new HttpClient();

try
{
    var response = await client.GetAsync("https://microsoft.com");
    Console.WriteLine(response); // logcat
}
catch (Exception e)
{
    Console.WriteLine(e);
    return 1;
}

return 42;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Net;
using System.Net.Http;

try
{
    _ = new HttpClient().GetStringAsync("https://bing.com").Result;
}
catch { }

try
{
    var a = Assembly.Load("System.Net.Quic");
    bool hasTypes = false;
    foreach (var t in a.GetTypes())
    {
        Console.WriteLine(t);
        hasTypes = true;
    }

    if (!hasTypes)
        return 100;
}
catch (FileNotFoundException)
{
    return 100;
}

return -1;

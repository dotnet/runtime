// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using Microsoft.AspNetCore.Http.Connections;

namespace BlazorHosted.Client;

public static class Helper
{
    public static string GetValue(NameValueCollection parameters, string key)
    {
        var values = parameters.GetValues(key);
        if (values == null || values.Length == 0)
        {
            throw new Exception($"Parameter '{key}' is required in the query string");
        }
        if (values.Length > 1)
        {
            throw new Exception($"Parameter '{key}' should be unique in the query string");
        }
        return values[0];
    }

    public static HttpTransportType StringToTransportType(string transport)
    {
        switch (transport.ToLowerInvariant())
        {
            case "longpolling":
                return HttpTransportType.LongPolling;
            case "websockets":
                return HttpTransportType.WebSockets;
            default:
                throw new Exception($"{transport} is invalid transport type");
        }
    }

    public static void TestOutputWriteLine(string message)
    {
        Console.WriteLine("TestOutput -> " + message);
    }
}

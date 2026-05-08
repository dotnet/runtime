// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Threading.Tasks;

class Program
{
    static async Task<int> Main(string[] args)
    {
        using HttpClient client = new();

        // send a request, but ignore its result
        try
        {
            await client.GetAsync("https://www.microsoft.com");
        }
        catch { }

        Type metricsHandler = GetHttpType("System.Net.Http.Metrics.MetricsHandler");

        // MetricsHandler should have been trimmed
        if (metricsHandler is not null)
        {
            return -1;
        }

        Type socketsHttpHandlerMetrics = GetHttpType("System.Net.Http.Metrics.SocketsHttpHandlerMetrics");

        // SocketsHttpHandlerMetrics should have been trimmed
        if (socketsHttpHandlerMetrics is not null)
        {
            return -2;
        }

        Type connectionMetrics = GetHttpType("System.Net.Http.Metrics.ConnectionMetrics");

        // ConnectionMetrics should have been trimmed
        if (connectionMetrics is not null)
        {
            return -3;
        }

        Type sharedMeter = GetHttpType("System.Net.Http.Metrics.SharedMeter");

        // SharedMeter should have been trimmed
        if (sharedMeter is not null)
        {
            return -4;
        }

        return 100;
    }

    // The intention of this method is to ensure the trimmer doesn't preserve the Type.
    private static Type GetHttpType(string name) =>
        typeof(HttpClient).Assembly.GetType(name, throwOnError: false);
}

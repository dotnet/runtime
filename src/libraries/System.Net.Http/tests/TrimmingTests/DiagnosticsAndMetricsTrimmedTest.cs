// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
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

        var trimmedTypes = new List<(Assembly,string,bool)>();
        trimmedTypes.Add((typeof(HttpClient).Assembly,"System.Diagnostics.Metrics.Meter", true));
        // TODO trimmedTypes.Add((typeof(Object).Assembly,"System.Diagnostics.Tracing.ActivityTracker", true));

        trimmedTypes.Add((typeof(ActivityStatusCode).Assembly,"System.Diagnostics.Metrics.MetricsEventSource", true));
        trimmedTypes.Add((typeof(ActivityStatusCode).Assembly,"System.Diagnostics.DiagnosticSourceEventSource", true));

        trimmedTypes.Add((typeof(ActivityStatusCode).Assembly,"System.Diagnostics.Metrics.AggregationManager", true));
        trimmedTypes.Add((typeof(ActivityStatusCode).Assembly,"System.Diagnostics.Metrics.RuntimeMetrics", true));
        trimmedTypes.Add((typeof(ActivityStatusCode).Assembly,"System.Diagnostics.ActivityListener", true));
        trimmedTypes.Add((typeof(ActivityStatusCode).Assembly,"System.Diagnostics.DiagnosticSource", true));
        trimmedTypes.Add((typeof(ActivityStatusCode).Assembly,"System.Diagnostics.DiagnosticListener", true));
        trimmedTypes.Add((typeof(ActivityStatusCode).Assembly,"System.Diagnostics.HttpHandlerDiagnosticListener", true));

        trimmedTypes.Add((typeof(ActivityStatusCode).Assembly,"System.Diagnostics.DiagLinkedList ", false));
        trimmedTypes.Add((typeof(ActivityStatusCode).Assembly,"System.Diagnostics.ActivitySource", false));
        trimmedTypes.Add((typeof(ActivityStatusCode).Assembly,"System.Diagnostics.Activity", false));

        if(!OperatingSystem.IsBrowser())
        {
            // TODO trimmedTypes.Add((typeof(Socket).Assembly,"System.Net.Sockets.SocketsTelemetry", true));
            // TODO trimmedTypes.Add((typeof(Dns).Assembly,"System.Net.NameResolutionTelemetry", true));
            // TODO trimmedTypes.Add((typeof(SslStream).Assembly,"System.Net.Security.NetSecurityTelemetry", true));
            // TODO trimmedTypes.Add((typeof(Object).Assembly,"System.Diagnostics.Tracing.EventSource", true));
        }

        var ok = 100;
        for (var i=0; i < trimmedTypes.Count; i++)
        {
            var (assembly,typeName,trim) = trimmedTypes[i];
            // The intention of this method is to ensure the trimmer doesn't preserve the Type.
            Type type = assembly.GetType(typeName, throwOnError: false);
            bool isTrimmed = (type == null);
            bool expectTrimmed=trim;
            if (expectTrimmed != isTrimmed)
            {
                ok = 0-i;
                Console.WriteLine($"Type {typeName} is isTrimmed:{isTrimmed} expectTrimmed:{expectTrimmed}.");
            }
        }
        return ok;
    }
}

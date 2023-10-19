// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Sockets;

namespace System.Net
{
    internal static class NameResolutionMetrics
    {
        private static readonly Meter s_meter = new("System.Net.NameResolution");

        private static readonly Histogram<double> s_lookupDuration = s_meter.CreateHistogram<double>(
            name: "dns.lookup.duration",
            unit: "s",
            description: "Measures the time taken to perform a DNS lookup.");

        public static bool IsEnabled() => s_lookupDuration.Enabled;

        public static void AfterResolution(TimeSpan duration, string hostName, Exception? exception)
        {
            var hostNameTag = KeyValuePair.Create("dns.question.name", (object?)hostName);

            if (exception is null)
            {
                s_lookupDuration.Record(duration.TotalSeconds, hostNameTag);
            }
            else
            {
                var errorTypeTag = KeyValuePair.Create("error.type", (object?)GetErrorType(exception));
                s_lookupDuration.Record(duration.TotalSeconds, hostNameTag, errorTypeTag);
            }
        }

        private static string GetErrorType(Exception exception) => (exception as SocketException)?.SocketErrorCode switch
        {
            SocketError.HostNotFound => "host_not_found",
            SocketError.TryAgain => "try_again",
            SocketError.AddressFamilyNotSupported => "address_family_not_supported",
            SocketError.NoRecovery => "no_recovery",

            _ => exception.GetType().FullName!
        };
    }
}

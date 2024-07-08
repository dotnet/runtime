// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Net.Http
{
    internal static class ConnectionSetupDiagnostics
    {
        private static readonly ActivitySource s_connectionActivitySource = new ActivitySource(DiagnosticsHandlerLoggingStrings.ConnectionNamespace);
        private static readonly ActivitySource s_waitForConnectionActivitySource = new ActivitySource(DiagnosticsHandlerLoggingStrings.WaitForConnectionNamespace);

        public static Activity? StartConnectionSetupActivity(bool isSecure, HttpAuthority authority)
        {
            Activity? activity = null;
            if (s_connectionActivitySource.HasListeners())
            {
                // Connection activities should be new roots and not parented under whatever
                // request happens to be in progress when the connection is started.
                Activity.Current = null;
                activity = s_connectionActivitySource.StartActivity(DiagnosticsHandlerLoggingStrings.ConnectionSetupActivityName, ActivityKind.Client);
            }

            if (activity is not null)
            {
                activity.DisplayName = $"HTTP connection_setup {authority.HostValue}:{authority.Port}";
                if (activity.IsAllDataRequested)
                {
                    activity.SetTag("server.address", authority.HostValue);
                    activity.SetTag("server.port", authority.Port);
                    activity.SetTag("url.scheme", isSecure ? "https" : "http");
                }
            }

            return activity;
        }

        public static void StopConnectionSetupActivity(Activity activity, IPEndPoint? remoteEndPoint)
        {
            Debug.Assert(activity is not null);
            if (activity.IsAllDataRequested && remoteEndPoint is not null)
            {
                activity.SetTag("network.peer.address", remoteEndPoint.Address.ToString());
            }
            activity.Stop();
        }

        public static void AbortActivity(Activity? activity, Exception exception)
        {
            Debug.Assert(exception is not null);
            if (activity is null) return;
            activity.SetStatus(ActivityStatusCode.Error);

            if (activity.IsAllDataRequested)
            {
                DiagnosticsHelper.TryGetErrorType(null, exception, out string? errorType);
                Debug.Assert(errorType is not null, "DiagnosticsHelper.TryGetErrorType() should succeed whenever an exception is provided.");
                activity.SetTag("error.type", errorType);
            }
            activity.Stop();
        }

        public static Activity? StartWaitForConnectionActivity(HttpAuthority authority)
        {
            Activity? activity = s_waitForConnectionActivitySource.StartActivity(DiagnosticsHandlerLoggingStrings.WaitForConnectionActivityName);
            if (activity is not null)
            {
                activity.DisplayName = $"wait_for_connection {authority.HostValue}:{authority.Port}";
            }

            return activity;
        }

        public static void StopWaitForConnectionActivity(Activity waitForConnectionActivity, HttpConnectionBase? connection)
        {
            Debug.Assert(waitForConnectionActivity is not null);
            if (waitForConnectionActivity.IsAllDataRequested && connection?.ConnectionSetupActivity is Activity connectionSetupActivity)
            {
                waitForConnectionActivity.AddLink(new ActivityLink(connectionSetupActivity.Context));
            }
            waitForConnectionActivity.Stop();
        }
    }
}

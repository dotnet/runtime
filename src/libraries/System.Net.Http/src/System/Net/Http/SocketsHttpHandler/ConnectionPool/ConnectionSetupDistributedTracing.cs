// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Net.Http
{
    // Implements distributed tracing logic for managing the "HTTP connection_setup" and "HTTP wait_for_connection" Activities.
    internal static class ConnectionSetupDistributedTracing
    {
        private static readonly ActivitySource s_connectionsActivitySource = new ActivitySource(DiagnosticsHandlerLoggingStrings.ConnectionsNamespace);

        public static Activity? StartConnectionSetupActivity(bool isSecure, HttpAuthority authority)
        {
            Activity? activity = null;
            if (s_connectionsActivitySource.HasListeners())
            {
                // Connection activities should be new roots and not parented under whatever
                // request happens to be in progress when the connection is started.
                Activity.Current = null;
                activity = s_connectionsActivitySource.StartActivity(DiagnosticsHandlerLoggingStrings.ConnectionSetupActivityName);
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

        public static void StopConnectionSetupActivity(Activity activity, Exception? exception, IPEndPoint? remoteEndPoint)
        {
            Debug.Assert(activity is not null);
            if (exception is not null)
            {
                ReportError(activity, exception);
            }
            else
            {
                if (activity.IsAllDataRequested && remoteEndPoint is not null)
                {
                    activity.SetTag("network.peer.address", remoteEndPoint.Address.ToString());
                }
            }

            activity.Stop();
        }

        public static void ReportError(Activity? activity, Exception exception)
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
        }

        public static Activity? StartWaitForConnectionActivity(HttpAuthority authority)
        {
            Activity? activity = s_connectionsActivitySource.StartActivity(DiagnosticsHandlerLoggingStrings.WaitForConnectionActivityName);
            if (activity is not null)
            {
                activity.DisplayName = $"HTTP wait_for_connection {authority.HostValue}:{authority.Port}";
            }

            return activity;
        }

        public static void AddConnectionLinkToRequestActivity(Activity connectionSetupActivity)
        {
            Debug.Assert(connectionSetupActivity is not null);

            // We only support links for request activities created by the "System.Net.Http" ActivitySource.
            if (DiagnosticsHandler.s_activitySource.HasListeners())
            {
                Activity? requestActivity = Activity.Current;
                if (requestActivity?.Source == DiagnosticsHandler.s_activitySource)
                {
                    requestActivity.AddLink(new ActivityLink(connectionSetupActivity.Context));
                }
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    /// <summary>
    /// Exposes process-wide settings for handlers.
    /// </summary>
    internal static class GlobalHttpSettings
    {
        internal static class DiagnosticsHandler
        {
            public static bool EnableActivityPropagation { get; } = RuntimeSettingParser.QueryRuntimeSettingSwitch(
                "System.Net.Http.EnableActivityPropagation",
                "DOTNET_SYSTEM_NET_HTTP_ENABLEACTIVITYPROPAGATION",
                true);
        }

#if !BROWSER
        internal static class SocketsHttpHandler
        {
            // Default to allowing HTTP/2, but enable that to be overridden by an
            // AppContext switch, or by an environment variable being set to false/0.
            public static bool AllowHttp2 { get; } = RuntimeSettingParser.QueryRuntimeSettingSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2Support",
                "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2SUPPORT",
                true);

            // Default to allowing draft HTTP/3, but enable that to be overridden
            // by an AppContext switch, or by an environment variable being set to false/0.
            public static bool AllowDraftHttp3 { get; } = RuntimeSettingParser.QueryRuntimeSettingSwitch(
                "System.Net.SocketsHttpHandler.Http3DraftSupport",
                "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP3DRAFTSUPPORT",
                true);

            // Switch to disable the HTTP/2 dynamic window scaling algorithm. Enabled by default.
            public static bool DisableDynamicHttp2WindowSizing { get; } = RuntimeSettingParser.QueryRuntimeSettingSwitch(
                "System.Net.SocketsHttpHandler.Http2FlowControl.DisableDynamicWindowSizing",
                "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2FLOWCONTROL_DISABLEDYNAMICWINDOWSIZING",
                false);

            // The maximum size of the HTTP/2 stream receive window. Defaults to 16 MB.
            public static int MaxHttp2StreamWindowSize { get; } = GetMaxHttp2StreamWindowSize();

            // Defaults to 1.0. Higher values result in shorter window, but slower downloads.
            public static double Http2StreamWindowScaleThresholdMultiplier { get; } = GetHttp2StreamWindowScaleThresholdMultiplier();

            private static int GetMaxHttp2StreamWindowSize()
            {
                int value = RuntimeSettingParser.ParseInt32EnvironmentVariableValue(
                    "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_FLOWCONTROL_MAXSTREAMWINDOWSIZE",
                    HttpHandlerDefaults.DefaultHttp2MaxStreamWindowSize);

                // Disallow small values:
                if (value < Http2Connection.DefaultInitialWindowSize)
                {
                    value = Http2Connection.DefaultInitialWindowSize;
                }
                return value;
            }

            private static double GetHttp2StreamWindowScaleThresholdMultiplier()
            {
                double value = RuntimeSettingParser.ParseDoubleEnvironmentVariableValue(
                    "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_FLOWCONTROL_STREAMWINDOWSCALETHRESHOLDMULTIPLIER",
                    HttpHandlerDefaults.DefaultHttp2StreamWindowScaleThresholdMultiplier);

                // Disallow negative values:
                if (value < 0)
                {
                    value = HttpHandlerDefaults.DefaultHttp2StreamWindowScaleThresholdMultiplier;
                }
                return value;
            }
        }
#endif
    }
}

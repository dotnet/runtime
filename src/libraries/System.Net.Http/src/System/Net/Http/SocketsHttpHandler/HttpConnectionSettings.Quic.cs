// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    internal sealed partial class HttpConnectionSettings
    {
        private const string Http3DraftSupportEnvironmentVariableSettingName = "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP3DRAFTSUPPORT";
        private const string Http3DraftSupportAppCtxSettingName = "System.Net.SocketsHttpHandler.Http3DraftSupport";

        private static bool AllowDraftHttp3
        {
            get
            {
                // Default to allowing draft HTTP/3, but enable that to be overridden
                // by an AppContext switch, or by an environment variable being set to false/0.

                // First check for the AppContext switch, giving it priority over the environment variable.
                if (AppContext.TryGetSwitch(Http3DraftSupportAppCtxSettingName, out bool allowHttp3))
                {
                    return allowHttp3;
                }

                // AppContext switch wasn't used. Check the environment variable.
                string? envVar = Environment.GetEnvironmentVariable(Http3DraftSupportEnvironmentVariableSettingName);
                if (envVar != null && (envVar.Equals("false", StringComparison.OrdinalIgnoreCase) || envVar.Equals("0")))
                {
                    // Disallow HTTP/3 protocol for HTTP endpoints.
                    return false;
                }

                // Default to allow.
                return true;
            }
        }

        private byte[]? _http3SettingsFrame;
        internal byte[] Http3SettingsFrame => _http3SettingsFrame ??= Http3Connection.BuildSettingsFrame(this);
    }
}

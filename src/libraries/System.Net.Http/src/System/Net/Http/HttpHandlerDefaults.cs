// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Net.Http
{
    /// <summary>
    /// Additional default values used used only in this assembly.
    /// </summary>
    internal static partial class HttpHandlerDefaults
    {
        public static readonly int DefaultMaxConnectionsPerServer = GlobalHttpSettings.SocketsHttpHandler.MaxConnectionsPerServer;

        public static readonly TimeSpan DefaultKeepAlivePingTimeout = TimeSpan.FromSeconds(20);
        public static readonly TimeSpan DefaultKeepAlivePingDelay = Timeout.InfiniteTimeSpan;
        public const HttpKeepAlivePingPolicy DefaultKeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always;

        // This is the default value for SocketsHttpHandler.InitialHttp2StreamWindowSize,
        // which defines the value we communicate in stream SETTINGS frames.
        // Should not be confused with Http2Connection.DefaultInitialWindowSize, which defines the RFC default.
        // Unlike that value, DefaultInitialHttp2StreamWindowSize might be changed in the future.
        public const int DefaultInitialHttp2StreamWindowSize = 65535;

        // This is the default value for SocketsHttpHandler.InitialHttp2MaxConcurrentStreams.
        // It defines how many concurrent streams a new HTTP/2 connection may use before it
        // observes the server's SETTINGS_MAX_CONCURRENT_STREAMS value.
        // 100 is the lowest limit servers are recommended to advertise by
        // https://www.rfc-editor.org/rfc/rfc9113.html#section-6.5.2 ("It is recommended that this
        // value be no smaller than 100, so as to not unnecessarily limit parallelism"), which makes
        // it a safe assumption for servers we haven't talked to yet.
        public const int DefaultInitialHttp2MaxConcurrentStreams = 100;
    }
}

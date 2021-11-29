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
        public static readonly TimeSpan DefaultKeepAlivePingTimeout = TimeSpan.FromSeconds(20);
        public static readonly TimeSpan DefaultKeepAlivePingDelay = Timeout.InfiniteTimeSpan;
        public const HttpKeepAlivePingPolicy DefaultKeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always;

        // This is the default value for SocketsHttpHandler.InitialHttp2StreamWindowSize,
        // which defines the value we communicate in stream SETTINGS frames.
        // Should not be confused with Http2Connection.DefaultInitialWindowSize, which defines the RFC default.
        // Unlike that value, DefaultInitialHttp2StreamWindowSize might be changed in the future.
        public const int DefaultInitialHttp2StreamWindowSize = 65535;
    }
}

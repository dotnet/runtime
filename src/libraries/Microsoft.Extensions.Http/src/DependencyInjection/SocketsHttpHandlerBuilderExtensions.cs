// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SocketsHttpHandlerBuilderExtensions
    {
        [UnsupportedOSPlatform("browser")]
        public static ISocketsHttpHandlerBuilder Configure(this ISocketsHttpHandlerBuilder builder, Action<SocketsHttpHandler, IServiceProvider> configure)
        {
            builder.Services.Configure<HttpClientFactoryOptions>(builder.Name, options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(b =>
                {
                    if (b.PrimaryHandler is not SocketsHttpHandler socketsHttpHandler)
                    {
                        string message = SR.Format(SR.SocketsHttpHandlerBuilder_PrimaryHandlerIsInvalid, nameof(b.PrimaryHandler), typeof(SocketsHttpHandler).FullName, Environment.NewLine, b.PrimaryHandler?.ToString() ?? "(null)");
                        throw new InvalidOperationException(message);
                    }

                    configure(socketsHttpHandler, b.Services);
                });
            });
            return builder;
        }

        [UnsupportedOSPlatform("browser")]
        public static ISocketsHttpHandlerBuilder Configure(this ISocketsHttpHandlerBuilder builder, IConfigurationSection configurationSection)
        {
            SocketsHttpHandlerConfiguration parsedConfig = SocketsHttpHandlerConfiguration.ParseFromConfig(configurationSection);
            return Configure(builder, (handler, _) => FillFromConfig(handler, parsedConfig));
        }

        [UnsupportedOSPlatform("browser")]
        private static void FillFromConfig(SocketsHttpHandler handler, SocketsHttpHandlerConfiguration parsedConfig)
        {
            if(parsedConfig.PooledConnectionIdleTimeout is not null)
            {
                handler.PooledConnectionIdleTimeout = parsedConfig.PooledConnectionIdleTimeout.Value;
            }

            if(parsedConfig.PooledConnectionLifetime is not null)
            {
                handler.PooledConnectionLifetime = parsedConfig.PooledConnectionLifetime.Value;
            }

            if(parsedConfig.PreAuthenticate is not null)
            {
                handler.PreAuthenticate = parsedConfig.PreAuthenticate.Value;
            }

            if(parsedConfig.ResponseDrainTimeout is not null)
            {
                handler.ResponseDrainTimeout = parsedConfig.ResponseDrainTimeout.Value;
            }

            if(parsedConfig.UseCookies is not null)
            {
                handler.UseCookies = parsedConfig.UseCookies.Value;
            }

            if(parsedConfig.UseProxy is not null)
            {
                handler.UseProxy = parsedConfig.UseProxy.Value;
            }

            if(parsedConfig.EnableMultipleHttp2Connections is not null)
            {
                handler.EnableMultipleHttp2Connections = parsedConfig.EnableMultipleHttp2Connections.Value;
            }

            if(parsedConfig.MaxResponseHeadersLength is not null)
            {
                handler.MaxResponseHeadersLength = parsedConfig.MaxResponseHeadersLength.Value;
            }

            if(parsedConfig.MaxResponseDrainSize is not null)
            {
                handler.MaxResponseDrainSize = parsedConfig.MaxResponseDrainSize.Value;
            }

            if(parsedConfig.MaxConnectionsPerServer is not null)
            {
                handler.MaxConnectionsPerServer = parsedConfig.MaxConnectionsPerServer.Value;
            }

            if(parsedConfig.MaxAutomaticRedirections is not null)
            {
                handler.MaxAutomaticRedirections = parsedConfig.MaxAutomaticRedirections.Value;
            }

            if(parsedConfig.InitialHttp2StreamWindowSize is not null)
            {
                handler.InitialHttp2StreamWindowSize = parsedConfig.InitialHttp2StreamWindowSize.Value;
            }

            if(parsedConfig.AllowAutoRedirect is not null)
            {
                handler.AllowAutoRedirect = parsedConfig.AllowAutoRedirect.Value;
            }

            if(parsedConfig.AutomaticDecompression is not null)
            {
                handler.AutomaticDecompression = parsedConfig.AutomaticDecompression.Value;
            }

            if(parsedConfig.ConnectTimeout is not null)
            {
                handler.ConnectTimeout = parsedConfig.ConnectTimeout.Value;
            }

            if(parsedConfig.Expect100ContinueTimeout is not null)
            {
                handler.Expect100ContinueTimeout = parsedConfig.Expect100ContinueTimeout.Value;
            }

            if(parsedConfig.KeepAlivePingDelay is not null)
            {
                handler.KeepAlivePingDelay = parsedConfig.KeepAlivePingDelay.Value;
            }

            if(parsedConfig.KeepAlivePingTimeout is not null)
            {
                handler.KeepAlivePingTimeout = parsedConfig.KeepAlivePingTimeout.Value;
            }

            if(parsedConfig.KeepAlivePingPolicy is not null)
            {
                handler.KeepAlivePingPolicy = parsedConfig.KeepAlivePingPolicy.Value;
            }
        }

        private record struct SocketsHttpHandlerConfiguration
        {
            public TimeSpan? PooledConnectionIdleTimeout { get; set; }
            public TimeSpan? PooledConnectionLifetime { get; set; }
            public bool? PreAuthenticate { get; set; }
            public TimeSpan? ResponseDrainTimeout { get; set; }
            public bool? UseCookies { get; set; }
            public bool? UseProxy { get; set; }
            public bool? EnableMultipleHttp2Connections { get; set; }
            public int? MaxResponseHeadersLength { get; set; }
            public int? MaxResponseDrainSize { get; set; }
            public int? MaxConnectionsPerServer { get; set; }
            public int? MaxAutomaticRedirections { get; set; }
            public int? InitialHttp2StreamWindowSize { get; set; }
            public bool? AllowAutoRedirect { get; set; }
            public DecompressionMethods? AutomaticDecompression { get; set; }
            public TimeSpan? ConnectTimeout { get; set; }
            public TimeSpan? Expect100ContinueTimeout { get; set; }
            public TimeSpan? KeepAlivePingDelay { get; set; }
            public TimeSpan? KeepAlivePingTimeout { get; set; }
            public HttpKeepAlivePingPolicy? KeepAlivePingPolicy { get; set; }

            [UnsupportedOSPlatform("browser")]
            public static SocketsHttpHandlerConfiguration ParseFromConfig(IConfigurationSection config)
            {
                return new SocketsHttpHandlerConfiguration()
                {
                    PooledConnectionIdleTimeout = ParseTimeSpan(config[nameof(SocketsHttpHandler.PooledConnectionIdleTimeout)]),
                    PooledConnectionLifetime = ParseTimeSpan(config[nameof(SocketsHttpHandler.PooledConnectionLifetime)]),
                    PreAuthenticate = ParseBool(config[nameof(SocketsHttpHandler.PreAuthenticate)]),
                    ResponseDrainTimeout = ParseTimeSpan(config[nameof(SocketsHttpHandler.ResponseDrainTimeout)]),
                    UseCookies = ParseBool(config[nameof(SocketsHttpHandler.UseCookies)]),
                    UseProxy = ParseBool(config[nameof(SocketsHttpHandler.UseProxy)]),
                    EnableMultipleHttp2Connections = ParseBool(config[nameof(SocketsHttpHandler.EnableMultipleHttp2Connections)]),
                    MaxResponseHeadersLength = ParseInt(config[nameof(SocketsHttpHandler.MaxResponseHeadersLength)]),
                    MaxResponseDrainSize = ParseInt(config[nameof(SocketsHttpHandler.MaxResponseDrainSize)]),
                    MaxConnectionsPerServer = ParseInt(config[nameof(SocketsHttpHandler.MaxConnectionsPerServer)]),
                    MaxAutomaticRedirections = ParseInt(config[nameof(SocketsHttpHandler.MaxAutomaticRedirections)]),
                    InitialHttp2StreamWindowSize = ParseInt(config[nameof(SocketsHttpHandler.InitialHttp2StreamWindowSize)]),
                    AllowAutoRedirect = ParseBool(config[nameof(SocketsHttpHandler.AllowAutoRedirect)]),
                    AutomaticDecompression = ParseDecompressionMethods(config[nameof(SocketsHttpHandler.AutomaticDecompression)]),
                    ConnectTimeout = ParseTimeSpan(config[nameof(SocketsHttpHandler.ConnectTimeout)]),
                    Expect100ContinueTimeout = ParseTimeSpan(config[nameof(SocketsHttpHandler.Expect100ContinueTimeout)]),
                    KeepAlivePingDelay = ParseTimeSpan(config[nameof(SocketsHttpHandler.KeepAlivePingDelay)]),
                    KeepAlivePingTimeout = ParseTimeSpan(config[nameof(SocketsHttpHandler.KeepAlivePingTimeout)]),
                    KeepAlivePingPolicy = ParseHttpKeepAlivePingPolicy(config[nameof(SocketsHttpHandler.KeepAlivePingPolicy)])
                };
            }
        }

        private static DecompressionMethods? ParseDecompressionMethods(string? decompressionMethods)
            => Enum.TryParse<DecompressionMethods>(decompressionMethods, ignoreCase: true, out var result)
                ? result
                : null;

        private static HttpKeepAlivePingPolicy? ParseHttpKeepAlivePingPolicy(string? httpKeepAlivePingPolicy)
            => Enum.TryParse<HttpKeepAlivePingPolicy>(httpKeepAlivePingPolicy, ignoreCase: true, out var result)
                ? result
                : null;

        private static bool? ParseBool(string? boolString) => bool.TryParse(boolString, out var result) ? result : null;

        private static int? ParseInt(string? intString) => int.TryParse(intString, out var result) ? result : null;

        private static TimeSpan? ParseTimeSpan(string? timeSpanString) => TimeSpan.TryParse(timeSpanString, out var result) ? result : null;
    }
}
#endif

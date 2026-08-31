// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET
using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to configure <see cref="SocketsHttpHandler"/> for a named
    /// <see cref="System.Net.Http.HttpClient"/> instances returned by <see cref="IHttpClientFactory"/>.
    /// </summary>
    public static class SocketsHttpHandlerBuilderExtensions
    {
        /// <summary>
        /// Adds a delegate that will be used to configure the primary <see cref="SocketsHttpHandler"/> for a
        /// named <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="builder">The <see cref="ISocketsHttpHandlerBuilder"/>.</param>
        /// <param name="configure">A delegate that is used to modify a <see cref="SocketsHttpHandler"/>.</param>
        /// <returns>An <see cref="ISocketsHttpHandlerBuilder"/> that can be used to configure the handler.</returns>
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

        /// <summary>
        /// Uses <see cref="IConfiguration"/> to configure the primary <see cref="SocketsHttpHandler"/> for a
        /// named <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="builder">The <see cref="ISocketsHttpHandlerBuilder"/>.</param>
        /// <param name="configuration">Configuration containing properties of <see cref="SocketsHttpHandler"/>.</param>
        /// <returns>An <see cref="ISocketsHttpHandlerBuilder"/> that can be used to configure the handler.</returns>
        /// <remarks>
        /// <para>
        /// Only simple (of type `bool`, `int`, <see cref="Enum"/> or <see cref="TimeSpan"/>) properties of <see cref="SocketsHttpHandler"/> will be parsed.
        /// </para>
        /// <para>
        /// All unmatched properties in <see cref="IConfiguration"/> will be ignored.
        /// </para>
        /// </remarks>
        [UnsupportedOSPlatform("browser")]
        public static ISocketsHttpHandlerBuilder Configure(this ISocketsHttpHandlerBuilder builder, IConfiguration configuration)
        {
            SocketsHttpHandlerConfiguration parsedConfig = ParseSocketsHttpHandlerConfiguration(configuration);
            return Configure(builder, (handler, _) => FillFromConfig(handler, parsedConfig));
        }

        [UnsupportedOSPlatform("browser")]
        private static void FillFromConfig(SocketsHttpHandler handler, in SocketsHttpHandlerConfiguration config)
        {
            if (config.PooledConnectionIdleTimeout is not null)
            {
                handler.PooledConnectionIdleTimeout = config.PooledConnectionIdleTimeout.Value;
            }

            if (config.PooledConnectionLifetime is not null)
            {
                handler.PooledConnectionLifetime = config.PooledConnectionLifetime.Value;
            }

            if (config.PreAuthenticate is not null)
            {
                handler.PreAuthenticate = config.PreAuthenticate.Value;
            }

            if (config.ResponseDrainTimeout is not null)
            {
                handler.ResponseDrainTimeout = config.ResponseDrainTimeout.Value;
            }

            if (config.UseCookies is not null)
            {
                handler.UseCookies = config.UseCookies.Value;
            }

            if (config.UseProxy is not null)
            {
                handler.UseProxy = config.UseProxy.Value;
            }

            if (config.EnableMultipleHttp2Connections is not null)
            {
                handler.EnableMultipleHttp2Connections = config.EnableMultipleHttp2Connections.Value;
            }

            if (config.MaxResponseHeadersLength is not null)
            {
                handler.MaxResponseHeadersLength = config.MaxResponseHeadersLength.Value;
            }

            if (config.MaxResponseDrainSize is not null)
            {
                handler.MaxResponseDrainSize = config.MaxResponseDrainSize.Value;
            }

            if (config.MaxConnectionsPerServer is not null)
            {
                handler.MaxConnectionsPerServer = config.MaxConnectionsPerServer.Value;
            }

            if (config.MaxAutomaticRedirections is not null)
            {
                handler.MaxAutomaticRedirections = config.MaxAutomaticRedirections.Value;
            }

            if (config.InitialHttp2StreamWindowSize is not null)
            {
                handler.InitialHttp2StreamWindowSize = config.InitialHttp2StreamWindowSize.Value;
            }

            if (config.AllowAutoRedirect is not null)
            {
                handler.AllowAutoRedirect = config.AllowAutoRedirect.Value;
            }

            if (config.AutomaticDecompression is not null)
            {
                handler.AutomaticDecompression = config.AutomaticDecompression.Value;
            }

            if (config.ConnectTimeout is not null)
            {
                handler.ConnectTimeout = config.ConnectTimeout.Value;
            }

            if (config.Expect100ContinueTimeout is not null)
            {
                handler.Expect100ContinueTimeout = config.Expect100ContinueTimeout.Value;
            }

            if (config.KeepAlivePingDelay is not null)
            {
                handler.KeepAlivePingDelay = config.KeepAlivePingDelay.Value;
            }

            if (config.KeepAlivePingTimeout is not null)
            {
                handler.KeepAlivePingTimeout = config.KeepAlivePingTimeout.Value;
            }

            if (config.KeepAlivePingPolicy is not null)
            {
                handler.KeepAlivePingPolicy = config.KeepAlivePingPolicy.Value;
            }
        }

        private readonly record struct SocketsHttpHandlerConfiguration
        {
            public TimeSpan? PooledConnectionIdleTimeout { get; init; }
            public TimeSpan? PooledConnectionLifetime { get; init; }
            public bool? PreAuthenticate { get; init; }
            public TimeSpan? ResponseDrainTimeout { get; init; }
            public bool? UseCookies { get; init; }
            public bool? UseProxy { get; init; }
            public bool? EnableMultipleHttp2Connections { get; init; }
            public int? MaxResponseHeadersLength { get; init; }
            public int? MaxResponseDrainSize { get; init; }
            public int? MaxConnectionsPerServer { get; init; }
            public int? MaxAutomaticRedirections { get; init; }
            public int? InitialHttp2StreamWindowSize { get; init; }
            public bool? AllowAutoRedirect { get; init; }
            public DecompressionMethods? AutomaticDecompression { get; init; }
            public TimeSpan? ConnectTimeout { get; init; }
            public TimeSpan? Expect100ContinueTimeout { get; init; }
            public TimeSpan? KeepAlivePingDelay { get; init; }
            public TimeSpan? KeepAlivePingTimeout { get; init; }
            public HttpKeepAlivePingPolicy? KeepAlivePingPolicy { get; init; }
        }

        [UnsupportedOSPlatform("browser")]
        private static SocketsHttpHandlerConfiguration ParseSocketsHttpHandlerConfiguration(IConfiguration config)
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
                AutomaticDecompression = ParseEnum<DecompressionMethods>(config[nameof(SocketsHttpHandler.AutomaticDecompression)]),
                ConnectTimeout = ParseTimeSpan(config[nameof(SocketsHttpHandler.ConnectTimeout)]),
                Expect100ContinueTimeout = ParseTimeSpan(config[nameof(SocketsHttpHandler.Expect100ContinueTimeout)]),
                KeepAlivePingDelay = ParseTimeSpan(config[nameof(SocketsHttpHandler.KeepAlivePingDelay)]),
                KeepAlivePingTimeout = ParseTimeSpan(config[nameof(SocketsHttpHandler.KeepAlivePingTimeout)]),
                KeepAlivePingPolicy = ParseEnum<HttpKeepAlivePingPolicy>(config[nameof(SocketsHttpHandler.KeepAlivePingPolicy)])
            };
        }

        private static TEnum? ParseEnum<TEnum>(string? enumString) where TEnum : struct
            => Enum.TryParse<TEnum>(enumString, ignoreCase: true, out var result) ? result : null;

        private static bool? ParseBool(string? boolString) => bool.TryParse(boolString, out var result) ? result : null;
        private static int? ParseInt(string? intString) => int.TryParse(intString, out var result) ? result : null;
        private static TimeSpan? ParseTimeSpan(string? timeSpanString) => TimeSpan.TryParse(timeSpanString, out var result) ? result : null;
    }
}
#endif

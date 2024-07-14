// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HttpClientBuilderExtensions
    {
        /// <summary>
        /// Adds a delegate that will be used to create an additional logger for a named <see cref="System.Net.Http.HttpClient"/>. The custom logger would be invoked
        /// from a dedicated logging DelegatingHandler on every request of the corresponding named <see cref="System.Net.Http.HttpClient"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <param name="httpClientLoggerFactory">A delegate that is used to create a custom logger. The logger should implement
        /// <see cref="IHttpClientLogger"/> or <see cref="IHttpClientAsyncLogger"/>.</param>
        /// <param name="wrapHandlersPipeline">Whether the logging handler with the custom logger would be added to the top
        /// or to the bottom of the additional handlers chains.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// If the <see paramref="wrapHandlersPipeline"/> is `true`, <see cref="IHttpClientLogger.LogRequestStart"/> and
        /// <see cref="IHttpClientAsyncLogger.LogRequestStartAsync"/> would be executed before all
        /// other additional handlers in the chain. <see cref="IHttpClientLogger.LogRequestStop"/> and
        /// <see cref="IHttpClientAsyncLogger.LogRequestStopAsync"/> would be executed after all
        /// other additional handlers, essentially wrapping the whole pipeline.
        /// </para>
        /// <para>
        /// If the <see paramref="wrapHandlersPipeline"/> is `false`, <see cref="IHttpClientLogger.LogRequestStart"/> and
        /// <see cref="IHttpClientAsyncLogger.LogRequestStartAsync"/> would be executed after all
        /// other additional handlers in the chain, right before the primary handler. <see cref="IHttpClientLogger.LogRequestStop"/> and
        /// <see cref="IHttpClientAsyncLogger.LogRequestStopAsync"/> would be executed before all
        /// other additional handlers, right after the primary handler.
        /// </para>
        /// <para>
        /// The <see cref="IServiceProvider"/> argument provided to <paramref name="httpClientLoggerFactory"/> will be
        /// a reference to a scoped service provider that shares the lifetime of the handler chain being constructed.
        /// </para>
        /// <para>
        /// If <see cref="AddLogger"/> is called multiple times, multiple loggers would be added. If <see cref="RemoveAllLoggers"/> was
        /// not called before calling <see cref="AddLogger"/>, then new logger would be added in addition to the default ones.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddLogger(this IHttpClientBuilder builder, Func<IServiceProvider, IHttpClientLogger> httpClientLoggerFactory, bool wrapHandlersPipeline = false)
        {
            ThrowHelper.ThrowIfNull(builder);
            ThrowHelper.ThrowIfNull(httpClientLoggerFactory);

            builder.Services.Configure<HttpClientFactoryOptions>(builder.Name, options =>
            {
                options.LoggingBuilderActions.Add(b =>
                {
                    IHttpClientLogger httpClientLogger = httpClientLoggerFactory(b.Services);
                    HttpClientLoggerHandler handler = new HttpClientLoggerHandler(httpClientLogger);

                    if (wrapHandlersPipeline)
                    {
                        b.AdditionalHandlers.Insert(0, handler);
                    }
                    else
                    {
                        b.AdditionalHandlers.Add(handler);
                    }
                });
            });

            return builder;
        }

        /// <summary>
        /// Adds a delegate that will be used to create an additional logger for a named <see cref="System.Net.Http.HttpClient"/>. The custom logger would be invoked
        /// from a dedicated logging DelegatingHandler on every request of the corresponding named <see cref="System.Net.Http.HttpClient"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <param name="wrapHandlersPipeline">Whether the logging handler with the custom logger would be added to the top
        /// or to the bottom of the additional handlers chains.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <typeparam name="TLogger">
        /// The service type of the custom logger as it was registered in DI. The logger should implement <see cref="IHttpClientLogger"/>
        /// or <see cref="IHttpClientAsyncLogger"/>.
        /// </typeparam>
        /// <remarks>
        /// <para>
        /// If the <see paramref="wrapHandlersPipeline"/> is `true`, <see cref="IHttpClientLogger.LogRequestStart"/> and
        /// <see cref="IHttpClientAsyncLogger.LogRequestStartAsync"/> would be executed before all
        /// other additional handlers in the chain. <see cref="IHttpClientLogger.LogRequestStop"/> and
        /// <see cref="IHttpClientAsyncLogger.LogRequestStopAsync"/> would be executed after all
        /// other additional handlers, essentially wrapping the whole pipeline.
        /// </para>
        /// <para>
        /// If the <see paramref="wrapHandlersPipeline"/> is `false`, <see cref="IHttpClientLogger.LogRequestStart"/> and
        /// <see cref="IHttpClientAsyncLogger.LogRequestStartAsync"/> would be executed after all
        /// other additional handlers in the chain, right before the primary handler. <see cref="IHttpClientLogger.LogRequestStop"/> and
        /// <see cref="IHttpClientAsyncLogger.LogRequestStopAsync"/> would be executed before all
        /// other additional handlers, right after the primary handler.
        /// </para>
        /// <para>
        /// The <typeparamref name="TLogger"/> will be resolved from a scoped service provider that shares
        /// the lifetime of the handler chain being constructed.
        /// </para>
        /// <para>
        /// If <see cref="AddLogger{TLogger}"/> is called multiple times, multiple loggers would be added. If <see cref="RemoveAllLoggers"/> was
        /// not called before calling <see cref="AddLogger{TLogger}"/>, then new logger would be added in addition to the default ones.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddLogger<TLogger>(this IHttpClientBuilder builder, bool wrapHandlersPipeline = false)
            where TLogger : IHttpClientLogger
        {
            ThrowHelper.ThrowIfNull(builder);

            builder.Services.Configure<HttpClientFactoryOptions>(builder.Name, options =>
            {
                options.LoggingBuilderActions.Add(b =>
                {
                    IHttpClientLogger httpClientLogger = TryGetKeyedService<TLogger>(b.Services, b.Name!)
                        ?? b.Services.GetRequiredService<TLogger>();

                    HttpClientLoggerHandler handler = new HttpClientLoggerHandler(httpClientLogger);

                    if (wrapHandlersPipeline)
                    {
                        b.AdditionalHandlers.Insert(0, handler);
                    }
                    else
                    {
                        b.AdditionalHandlers.Add(handler);
                    }
                });
            });

            return builder;
        }

        /// <summary>
        /// Removes all previously added loggers for a named <see cref="System.Net.Http.HttpClient"/>, including default ones.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        public static IHttpClientBuilder RemoveAllLoggers(this IHttpClientBuilder builder)
        {
            ThrowHelper.ThrowIfNull(builder);

            builder.Services.Configure<HttpClientFactoryOptions>(builder.Name, options =>
            {
                options.LoggingBuilderActions.Clear();
                options.SuppressDefaultLogging = true;
            });

            return builder;
        }

        /// <summary>
        /// Adds back the default logging for a named <see cref="System.Net.Http.HttpClient"/>, if it was removed previously by calling <see cref="RemoveAllLoggers"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        public static IHttpClientBuilder AddDefaultLogger(this IHttpClientBuilder builder)
        {
            ThrowHelper.ThrowIfNull(builder);

            builder.Services.Configure<HttpClientFactoryOptions>(builder.Name, options => options.SuppressDefaultLogging = false);
            return builder;
        }
    }
}

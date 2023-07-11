// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HttpClientBuilderExtensions
    {
        public static IHttpClientBuilder AddLogger(this IHttpClientBuilder builder, Func<IServiceProvider, IHttpClientLogger> httpClientLoggerFactory, bool wrapHandlersPipeline = false)
        {
            ThrowHelper.ThrowIfNull(builder);

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

        public static IHttpClientBuilder AddLogger<TLogger>(this IHttpClientBuilder builder, bool wrapHandlersPipeline = false)
            where TLogger : IHttpClientLogger
        {
            ThrowHelper.ThrowIfNull(builder);

            return AddLogger(builder, services => services.GetRequiredService<TLogger>(), wrapHandlersPipeline);
        }

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

        public static IHttpClientBuilder AddDefaultLogger(this IHttpClientBuilder builder)
        {
            ThrowHelper.ThrowIfNull(builder);

            builder.Services.Configure<HttpClientFactoryOptions>(builder.Name, options => options.SuppressDefaultLogging = false);
            return builder;
        }
    }
}

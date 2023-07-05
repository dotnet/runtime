// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpClientLoggingBuilderExtensions
    {
        public static IHttpClientLoggingBuilder AddDefaultLogger(this IHttpClientLoggingBuilder builder)
        {
            ThrowHelper.ThrowIfNull(builder);

            builder.Services.Configure<HttpClientFactoryOptions>(builder.Name, options => options.SuppressDefaultLogging = false);
            return builder;
        }

        public static IHttpClientLoggingBuilder AddLogger<TLogger>(this IHttpClientLoggingBuilder builder, bool wrapHandlersPipeline = false)
            where TLogger : IHttpClientLogger
        {
            ThrowHelper.ThrowIfNull(builder);

            return builder.AddLogger(services => services.GetRequiredService<TLogger>(), wrapHandlersPipeline);
        }
    }
}

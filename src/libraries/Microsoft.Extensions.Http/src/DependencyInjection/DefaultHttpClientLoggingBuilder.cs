// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class DefaultHttpClientLoggingBuilder : IHttpClientLoggingBuilder
    {
        public DefaultHttpClientLoggingBuilder(IServiceCollection services, string name)
        {
            Services = services;
            Name = name;
        }

        public string Name { get; }

        public IServiceCollection Services { get; }

        public IHttpClientLoggingBuilder AddLogger(Func<IServiceProvider, IHttpClientLogger> httpClientLoggerFactory, bool wrapHandlersPipeline = false)
        {
            Services.Configure<HttpClientFactoryOptions>(Name, options =>
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

            return this;
        }

        public IHttpClientLoggingBuilder RemoveAllLoggers()
        {
            Services.Configure<HttpClientFactoryOptions>(Name, options =>
            {
                options.LoggingBuilderActions.Clear();
                options.SuppressDefaultLogging = true;
            });

            return this;
        }
    }
}

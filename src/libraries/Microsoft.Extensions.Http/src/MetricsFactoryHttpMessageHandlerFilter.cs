// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET
using System;
using System.Diagnostics.Metrics;
using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    internal sealed class MetricsFactoryHttpMessageHandlerFilter : IHttpMessageHandlerBuilderFilter
    {
        private readonly IMeterFactory _meterFactory;

        public MetricsFactoryHttpMessageHandlerFilter(IMeterFactory meterFactory)
        {
            ArgumentNullException.ThrowIfNull(meterFactory);

            _meterFactory = meterFactory;
        }

        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        {
            ArgumentNullException.ThrowIfNull(next);

            return (builder) =>
            {
                // Run other configuration first, we want to decorate.
                next(builder);

                if (builder.PrimaryHandler is HttpClientHandler httpClientHandler)
                {
                    // Don't overwrite factory if one is already set.
                    httpClientHandler.MeterFactory ??= _meterFactory;
                }
                else if (!OperatingSystem.IsBrowser() && !OperatingSystem.IsWasi() && builder.PrimaryHandler is SocketsHttpHandler socketsHttpHandler)
                {
                    // Don't overwrite factory if one is already set.
                    socketsHttpHandler.MeterFactory ??= _meterFactory;
                }
            };
        }
    }
}
#endif

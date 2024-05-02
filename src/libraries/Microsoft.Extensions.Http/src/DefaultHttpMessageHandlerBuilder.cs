// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http
{
    internal sealed class DefaultHttpMessageHandlerBuilder : HttpMessageHandlerBuilder
    {
        private HttpMessageHandler? _primaryHandler;
        private string? _name;

        public DefaultHttpMessageHandlerBuilder(IServiceProvider services)
        {
            Services = services;
        }

        [DisallowNull]
        public override string? Name
        {
            get => _name;
            set
            {
                ThrowHelper.ThrowIfNull(value);
                _name = value;
            }
        }

        public override HttpMessageHandler PrimaryHandler
        {
            get => _primaryHandler ??= CreatePrimaryHandler();
            set => _primaryHandler = value;
        }

        public override IList<DelegatingHandler> AdditionalHandlers { get; } = new List<DelegatingHandler>();

        public override IServiceProvider Services { get; }

        public override HttpMessageHandler Build()
        {
            if (PrimaryHandler == null)
            {
                string message = SR.Format(SR.HttpMessageHandlerBuilder_PrimaryHandlerIsNull, nameof(PrimaryHandler));
                throw new InvalidOperationException(message);
            }

            return CreateHandlerPipeline(PrimaryHandler, AdditionalHandlers);
        }

#pragma warning disable CA1822, CA1859 // Mark members as static, Use concrete types when possible for improved performance
        private HttpMessageHandler CreatePrimaryHandler()
#pragma warning restore CA1822, CA1859
        {
#if NET
            // On platforms where SocketsHttpHandler is supported, HttpClientHandler is a thin wrapper
            // around it. By using SocketsHttpHandler directly, we can avoid the overhead of the wrapper,
            // but more importantly, we can configure it to limit the lifetime of its pooled connections
            // to match the requested lifetime of the handler itself. That way, if/when someone holds on
            // to a resulting HttpClient for a prolonged period of time, it'll still benefit from connection
            // recycling, and without needing to tear down and reconstitute the rest of the handler pipeline.
            if (SocketsHttpHandler.IsSupported)
            {
                SocketsHttpHandler handler = new();

                if (Services.GetService<IOptionsMonitor<HttpClientFactoryOptions>>() is IOptionsMonitor<HttpClientFactoryOptions> optionsMonitor)
                {
                    TimeSpan lifetime = optionsMonitor.Get(_name).HandlerLifetime;
                    if (lifetime >= TimeSpan.Zero)
                    {
                        handler.PooledConnectionLifetime = lifetime;
                    }
                }

                return handler;
            }
#endif

            return new HttpClientHandler();
        }
    }
}

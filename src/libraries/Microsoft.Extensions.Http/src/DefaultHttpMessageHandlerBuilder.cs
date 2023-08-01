// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    internal sealed class DefaultHttpMessageHandlerBuilder : HttpMessageHandlerBuilder
    {
        public DefaultHttpMessageHandlerBuilder(IServiceProvider services, IMeterFactory meterFactory)
        {
            Services = services;
            _meterFactory = meterFactory;
        }

        private readonly IMeterFactory _meterFactory;
        private string? _name;

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

        public override HttpMessageHandler PrimaryHandler { get; set; } = new HttpClientHandler();

        public override IList<DelegatingHandler> AdditionalHandlers { get; } = new List<DelegatingHandler>();

        public override IServiceProvider Services { get; }

        public override HttpMessageHandler Build()
        {
            if (PrimaryHandler == null)
            {
                string message = SR.Format(SR.HttpMessageHandlerBuilder_PrimaryHandlerIsNull, nameof(PrimaryHandler));
                throw new InvalidOperationException(message);
            }

#if NET8_0_OR_GREATER
            // The MeterFactory property is available on handlers in .NET 8 or later.
            if (PrimaryHandler is HttpClientHandler httpClientHandler)
            {
                httpClientHandler.MeterFactory = _meterFactory;
            }
            else if (!OperatingSystem.IsBrowser() && PrimaryHandler is SocketsHttpHandler socketsHttpHandler)
            {
                socketsHttpHandler.MeterFactory = _meterFactory;
            }
#endif

            return CreateHandlerPipeline(PrimaryHandler, AdditionalHandlers);
        }
    }
}

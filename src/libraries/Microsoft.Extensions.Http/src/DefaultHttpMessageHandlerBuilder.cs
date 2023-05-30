// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Microsoft.Extensions.Http
{
    internal sealed class DefaultHttpMessageHandlerBuilder : HttpMessageHandlerBuilder
    {
        private const string MeterName = "System.Net.Http";

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

            if (PrimaryHandler is HttpClientHandler httpClientHandler)
            {
                _ = _meterFactory.Create(MeterName);

                // TODO: Waiting for HttpClientHandler.Meter API.
                // httpClientHandler.Meter = _meterFactory.Create(MeterName);
            }
#if NET8_0_OR_GREATER
            else if (PrimaryHandler is SocketsHttpHandler socketsHttpHandler)
            {
                _ = _meterFactory.Create(MeterName);

                // TODO: Waiting for SocketsHttpHandler.Meter API.
                //socketsHttpHandler.Meter = _meterFactory.Create(MeterName);
            }
#endif

            return CreateHandlerPipeline(PrimaryHandler, AdditionalHandlers);
        }
    }
}

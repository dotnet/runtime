// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    internal sealed class DefaultHttpMessageHandlerBuilder : HttpMessageHandlerBuilder
    {
        public DefaultHttpMessageHandlerBuilder(IServiceProvider services)
        {
            Services = services;
        }

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

        private HttpMessageHandler? _primaryHandler;
        internal bool PrimaryHandlerIsSet { get; private set;}
        public override HttpMessageHandler PrimaryHandler
        {
            get
            {
                if (_primaryHandler is null && !PrimaryHandlerIsSet)
                {
                    _primaryHandler =
#if NET5_0_OR_GREATER
                        SocketsHttpHandler.IsSupported
                            // There's a lot of erroneous usecases when a client gets captured by a singleton.
                            // This should make the default case better by preventing the loss of DNS changes.
                            ? new SocketsHttpHandler() { PooledConnectionLifetime = HttpClientFactoryOptions.DefaultHandlerLifetime }
                            : new HttpClientHandler();
#else
                        new HttpClientHandler();
#endif
                }
                return _primaryHandler!;
            }

            set
            {
                _primaryHandler = value;
                PrimaryHandlerIsSet = true;
            }
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
    }
}

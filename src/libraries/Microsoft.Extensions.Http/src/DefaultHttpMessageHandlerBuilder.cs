// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    internal class DefaultHttpMessageHandlerBuilder : HttpMessageHandlerBuilder
    {
        public DefaultHttpMessageHandlerBuilder(IServiceProvider services)
        {
            Services = services;
        }

        private string _name;

        public override string Name
        {
            get => _name;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _name = value;
            }
        }

        private bool _primaryHandlerExposed;
        internal override bool PrimaryHandlerExposed => _primaryHandlerExposed;

        private HttpMessageHandler _primaryHandler;
        public override HttpMessageHandler PrimaryHandler
        {
            get
            {
                if (_primaryHandler == null && !_primaryHandlerExposed)
                {
                    _primaryHandler = new HttpClientHandler(); // Backward-compatibility
                }
                _primaryHandlerExposed = true; // Someone accessed PrimaryHandler. Its properties might be changed.
                return _primaryHandler;
            }
            set
            {
                _primaryHandler = value;
                _primaryHandlerExposed = true;
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

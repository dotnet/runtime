// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class DefaultHttpClientBuilder : IHttpClientBuilder
    {
        public DefaultHttpClientBuilder(IServiceCollection services, string? name)
        {
            // We want to return the same default builder instance for each call.
            // This is required because the service collection wrapper has state (last added position) that we want to maintain.
            var tracker = (DefaultHttpClientConfigurationTracker?)services.Single(sd => sd.ServiceType == typeof(DefaultHttpClientConfigurationTracker)).ImplementationInstance;
            Debug.Assert(tracker != null);

            Services = new DefaultHttpClientBuilderServiceCollection(services, name == null, tracker);
            Name = name!;
        }

        public string Name { get; }

        public IServiceCollection Services { get; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Microsoft.Extensions.Hosting.Internal
{
    internal class MetricsHostedService : IHostedService
    {
        private readonly IMetricsSubscriptionManager _manager;

        public MetricsHostedService(IMetricsSubscriptionManager manager)
        {
            _manager = manager;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _manager.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

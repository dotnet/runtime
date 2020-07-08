// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting.Tests.Fakes
{
    public class FakeHostLifetime : IHostLifetime
    {
        public int StartCount { get; internal set; }
        public int StopCount { get; internal set; }

        public Action<CancellationToken> StartAction { get; set; }
        public Action StopAction { get; set; }
        
        public Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            StartCount++;
            StartAction?.Invoke(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCount++;
            StopAction?.Invoke();
            return Task.CompletedTask;
        }
    }
}

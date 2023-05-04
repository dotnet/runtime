// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting.Unit.Tests;

internal class DelegateHostedService : IHostedService, IDisposable
{
    private readonly Action _started;
    private readonly Action _stopping;
    private readonly Action _disposing;

    public DelegateHostedService(Action started, Action stopping, Action disposing)
    {
        _started = started;
        _stopping = stopping;
        _disposing = disposing;
    }

    public Task StartAsync(CancellationToken token)
    {
        StartDate = DateTimeOffset.Now;
        _started();
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken token)
    {
        StopDate = DateTimeOffset.Now;
        _stopping();
        return Task.CompletedTask;
    }

    public void Dispose() => _disposing();

    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset StopDate { get; private set; }
}

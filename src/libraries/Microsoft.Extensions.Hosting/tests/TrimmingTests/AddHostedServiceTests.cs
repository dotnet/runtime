// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static int Main(string[] args)
    {
        IServiceCollection descriptors = new ServiceCollection();
        descriptors.AddHostedService<MyHost>();

        ServiceProvider provider = descriptors.BuildServiceProvider();

        foreach (IHostedService h in provider.GetServices<IHostedService>())
        {
            if (!(h is MyHost))
            {
                return -1;
            }
        }

        return 100;
    }

    private class MyHost : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

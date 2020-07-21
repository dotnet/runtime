// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

class Program
{
    static int Main(string[] args)
    {
        IServiceCollection descriptors = new ServiceCollection();
        descriptors.AddTransient<ServiceA>();
        descriptors.AddScoped(typeof(ServiceB));
        descriptors.AddSingleton(typeof(IServiceC), typeof(ServiceC));

        ServiceProvider provider = descriptors.BuildServiceProvider();

        if (provider.GetService<ServiceA>() is null ||
            provider.GetService<ServiceB>() is null ||
            provider.GetService<IServiceC>() is null)
        {
            return -1;
        }

        return 100;
    }

    private class ServiceA { }
    private class ServiceB { }
    private interface IServiceC { }
    private class ServiceC : IServiceC { }
}

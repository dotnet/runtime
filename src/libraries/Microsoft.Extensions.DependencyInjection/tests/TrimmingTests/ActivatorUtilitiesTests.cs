// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static int Main(string[] args)
    {
        ServiceProvider provider = new ServiceCollection().BuildServiceProvider();

        ObjectFactory factory = ActivatorUtilities.CreateFactory(typeof(ServiceA), Array.Empty<Type>());
        ServiceA serviceA = factory(provider, null) as ServiceA;
        ServiceB serviceB = ActivatorUtilities.CreateInstance(provider, typeof(ServiceB)) as ServiceB;
        ServiceC serviceC = ActivatorUtilities.CreateInstance<ServiceC>(provider);
        ServiceD serviceD = ActivatorUtilities.GetServiceOrCreateInstance(provider, typeof(ServiceD)) as ServiceD;
        ServiceE serviceE = ActivatorUtilities.GetServiceOrCreateInstance<ServiceE>(provider);

        if (serviceA is null ||
            serviceB is null ||
            serviceC is null ||
            serviceD is null ||
            serviceE is null)
        {
            return -1;
        }

        return 100;
    }

    private class ServiceA { }
    private class ServiceB { }
    private class ServiceC { }
    private class ServiceD { }
    private class ServiceE { }
}

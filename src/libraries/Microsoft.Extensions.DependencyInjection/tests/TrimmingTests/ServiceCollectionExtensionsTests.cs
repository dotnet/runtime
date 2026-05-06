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

        descriptors.AddTransient<IServiceD, ServiceD>();
        descriptors.AddTransient<IServiceE, ServiceE>();
        descriptors.AddTransient(typeof(IServiceF), typeof(ServiceF));

        descriptors.AddTransient<IServiceG, ServiceG>();
        descriptors.AddTransient(typeof(Logger<>));

        descriptors.AddTransient<IServiceH, ServiceH1>();
        descriptors.AddTransient<IServiceH, ServiceH2>();

        ServiceProvider provider = descriptors.BuildServiceProvider();

        if (provider.GetService<ServiceA>() is null ||
            provider.GetService<ServiceB>() is null ||
            provider.GetService<IServiceC>() is null ||
            provider.GetService<IServiceF>()?.ServiceE?.ServiceD is null ||
            provider.GetService<Logger<IServiceG>>() is null)
        {
            return -1;
        }

        int hServices = 0;
        foreach (IServiceH h in provider.GetServices<IServiceH>())
        {
            hServices++;
            if (h == null)
            {
                return -1;
            }
        }

        if (hServices != 2)
        {
            return -1;
        }

        return 100;
    }

    private class ServiceA { }
    private class ServiceB { }
    private interface IServiceC { }
    private class ServiceC : IServiceC { }

    public interface IServiceD { }
    public interface IServiceE { IServiceD ServiceD { get; } }
    public interface IServiceF { IServiceE ServiceE { get; } }
    public class ServiceD : IServiceD
    {
    }
    public class ServiceE : IServiceE
    {
        public IServiceD ServiceD { get; }
        public ServiceE(IServiceD d) { ServiceD = d; }
    }
    public class ServiceF : IServiceF
    {
        public IServiceE ServiceE { get; }
        public ServiceF(IServiceE e) { ServiceE = e; }
    }

    public class Logger<T> { }
    public interface IServiceG { }
    public class ServiceG : IServiceG { }

    public interface IServiceH { }
    public class ServiceH1 : IServiceH { }
    public class ServiceH2 : IServiceH { }
}

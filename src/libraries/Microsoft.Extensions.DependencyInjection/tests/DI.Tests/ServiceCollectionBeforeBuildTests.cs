// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection;

public class ServiceCollectionBeforeBuildTests
{
    [Fact]
    public void TestBeforeBuildHookRunsAfterOtherActions()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.OnBeforeBuild += Decorate;
        serviceCollection.AddSingleton<IInterface, Implementation>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var @interface = serviceProvider.GetRequiredService<IInterface>();

        Assert.IsType<Decorator>(@interface);

        string? foo = @interface.Foo();

        Assert.Equal("Bar!", foo);
    }

    private void Decorate(IServiceCollection serviceCollection)
    {
        var implType = serviceCollection.First(x => x.ServiceType == typeof(IInterface)).ImplementationType;
        serviceCollection.RemoveAll<IInterface>();
        serviceCollection.AddSingleton<IInterface>(sp =>
        {
            IInterface originalImplementation = (IInterface)ActivatorUtilities.CreateInstance(sp, implType);
            return new Decorator(originalImplementation);
        });
    }

    private interface IInterface
    {
        string Foo();
    }

    private class Implementation : IInterface
    {
        public string Foo() => "Bar";
    }

    private class Decorator : IInterface
    {
        private readonly IInterface _interface;

        public Decorator(IInterface @interface)
        {
            _interface = @interface;
        }
        public string Foo() => _interface.Foo() + "!";
    }
}

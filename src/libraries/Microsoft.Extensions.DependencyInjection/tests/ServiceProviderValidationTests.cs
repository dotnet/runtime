// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class ServiceProviderValidationTests
    {
        [Fact]
        public void GetService_Throws_WhenScopedIsInjectedIntoSingleton()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IFoo, Foo>();
            serviceCollection.AddScoped<IBar, Bar>();
            var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);

            // Act + Assert
            var exception = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService(typeof(IFoo)));
            Assert.Equal($"Cannot consume scoped service '{typeof(IBar)}' from singleton '{typeof(IFoo)}'.", exception.Message);
        }

        [Fact]
        public void GetService_Throws_WhenScopedIsInjectedIntoSingletonThroughTransient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IFoo, Foo>();
            serviceCollection.AddTransient<IBar, Bar2>();
            serviceCollection.AddScoped<IBaz, Baz>();
            var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);

            // Act + Assert
            var exception = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService(typeof(IFoo)));
            Assert.Equal($"Cannot consume scoped service '{typeof(IBaz)}' from singleton '{typeof(IFoo)}'.", exception.Message);
        }

        [Fact]
        public void GetService_Throws_WhenScopedIsInjectedIntoSingletonThroughSingleton()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IFoo, Foo>();
            serviceCollection.AddSingleton<IBar, Bar2>();
            serviceCollection.AddScoped<IBaz, Baz>();
            var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);

            // Act + Assert
            var exception = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService(typeof(IFoo)));
            Assert.Equal($"Cannot consume scoped service '{typeof(IBaz)}' from singleton '{typeof(IBar)}'.", exception.Message);
        }

        [Fact]
        public void GetService_Throws_WhenScopedIsInjectedIntoSingletonThroughSingletonAndScopedWhileInScope()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddScoped<IFoo, Foo>();
            serviceCollection.AddSingleton<IBar, Bar2>();
            serviceCollection.AddScoped<IBaz, Baz>();
            var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
            var scope = serviceProvider.CreateScope();

            // Act + Assert
            var exception = Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetService(typeof(IFoo)));
            Assert.Equal($"Cannot consume scoped service '{typeof(IBaz)}' from singleton '{typeof(IBar)}'.", exception.Message);
        }

        [Fact]
        public void GetService_Throws_WhenGetServiceForScopedServiceIsCalledOnRoot()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IBar, Bar>();
            var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);

            // Act + Assert
            var exception = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService(typeof(IBar)));
            Assert.Equal($"Cannot resolve scoped service '{typeof(IBar)}' from root provider.", exception.Message);
        }

        [Fact]
        public void GetService_Throws_WhenGetServiceForScopedServiceIsCalledOnRootViaTransient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<IFoo, Foo>();
            serviceCollection.AddScoped<IBar, Bar>();
            var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);

            // Act + Assert
            var exception = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService(typeof(IFoo)));
            Assert.Equal($"Cannot resolve '{typeof(IFoo)}' from root provider because it requires scoped service '{typeof(IBar)}'.", exception.Message);
        }

        [Fact]
        public void GetService_DoesNotThrow_WhenScopeFactoryIsInjectedIntoSingleton()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IBoo, Boo>();
            var serviceProvider = serviceCollection.BuildServiceProvider(true);

            // Act + Assert
            var result = serviceProvider.GetService(typeof(IBoo));
            Assert.NotNull(result);
        }

        [Fact]
        public void BuildServiceProvider_ValidateOnBuild_ThrowsForUnresolvableServices()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<IFoo, Foo>();
            serviceCollection.AddTransient<IBaz, BazRecursive>();

            // Act + Assert
            var aggregateException = Assert.Throws<AggregateException>(() => serviceCollection.BuildServiceProvider(new ServiceProviderOptions() { ValidateOnBuild = true }));
            Assert.StartsWith("Some services are not able to be constructed", aggregateException.Message);
            Assert.Equal(2, aggregateException.InnerExceptions.Count);
            Assert.Equal("Error while validating the service descriptor 'ServiceType: Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+IFoo Lifetime: Transient ImplementationType: Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+Foo': " +
                         "Unable to resolve service for type 'Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+IBar' while attempting to activate" +
                         " 'Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+Foo'.",
                aggregateException.InnerExceptions[0].Message);

            Assert.Equal("Error while validating the service descriptor 'ServiceType: Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+IBaz Lifetime: Transient ImplementationType: Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+BazRecursive': " +
                         "A circular dependency was detected for the service of type 'Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+IBaz'." + Environment.NewLine +
                         "Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+IBaz(Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+BazRecursive) ->" +
                         " Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+IBaz",
                aggregateException.InnerExceptions[1].Message);
        }

        [Fact]
        public void BuildServiceProvider_ValidateOnBuild_SkipsOpenGenerics()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient(typeof(IFakeOpenGenericService<>), typeof(FakeOpenGenericService<>));

            // Act + Assert
            serviceCollection.BuildServiceProvider(new ServiceProviderOptions() { ValidateOnBuild = true });
        }

        [Fact]
        public void BuildServiceProvider_ValidateOnBuild_ValidatesAllDescriptors()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<IBaz, BazRecursive>();
            serviceCollection.AddTransient<IBaz, Baz>();

            // Act + Assert
            var aggregateException = Assert.Throws<AggregateException>(() => serviceCollection.BuildServiceProvider(new ServiceProviderOptions() { ValidateOnBuild = true }));
            Assert.StartsWith("Some services are not able to be constructed", aggregateException.Message);
            Assert.Single(aggregateException.InnerExceptions);

            Assert.Equal("Error while validating the service descriptor 'ServiceType: Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+IBaz Lifetime: Transient ImplementationType: Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+BazRecursive': " +
                         "A circular dependency was detected for the service of type 'Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+IBaz'." + Environment.NewLine +
                         "Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+IBaz(Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+BazRecursive) ->" +
                         " Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+IBaz",
                aggregateException.InnerExceptions[0].Message);
        }

        [Fact]
        public void BuildServiceProvider_ValidateOnBuild_ThrowsWhenImplementationIsNotAssignableToService()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient(typeof(IBaz), typeof(Boo));
            serviceCollection.AddSingleton(typeof(IFoo), new object());

            // Act + Assert
            var aggregateException = Assert.Throws<AggregateException>(() => serviceCollection.BuildServiceProvider(new ServiceProviderOptions() { ValidateOnBuild = true }));
            Assert.StartsWith("Some services are not able to be constructed", aggregateException.Message);
            Assert.Equal(2, aggregateException.InnerExceptions.Count);

            Assert.Equal("Error while validating the service descriptor 'ServiceType: Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+IBaz Lifetime: Transient ImplementationType: Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+Boo': " +
                         "Implementation type 'Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+Boo' can't be converted to service type 'Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+IBaz'",
                         aggregateException.InnerExceptions[0].Message);

            Assert.Equal("Error while validating the service descriptor 'ServiceType: Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+IFoo Lifetime: Singleton ImplementationInstance: System.Object': " +
                         "Constant value of type 'System.Object' can't be converted to service type 'Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderValidationTests+IFoo'",
                         aggregateException.InnerExceptions[1].Message);
        }

        private interface IFoo
        {
        }

        private class Foo : IFoo
        {
            public Foo(IBar bar)
            {
            }
        }

        private interface IBar
        {
        }

        private class Bar : IBar
        {
        }

        private class Bar2 : IBar
        {
            public Bar2(IBaz baz)
            {
            }
        }

        private interface IBaz
        {
        }

        private class Baz : IBaz
        {
        }

        private class BazRecursive : IBaz
        {
            public BazRecursive(IBaz baz)
            {
            }
        }

        private interface IBoo
        {
        }

        private class Boo : IBoo
        {
            public Boo(IServiceScopeFactory scopeFactory)
            {
            }
        }
    }
}

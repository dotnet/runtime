// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public async void GetService_Throws_WhenGetServiceForScopedServiceIsCalledOnRoot_IL_Replacement()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IBar, Bar>();
            var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);

            // Act + Assert
            using (var scope = serviceProvider.CreateScope())
            {
                // Switch to an emit-based version which is triggered in the background after 2 calls to GetService.
                scope.ServiceProvider.GetRequiredService(typeof(IBar));
                scope.ServiceProvider.GetRequiredService(typeof(IBar));

                // Give the background thread time to generate the emit version.
                await Task.Delay(100);

                // Ensure the emit-based version has the correct scope checks.
                var exception = Assert.Throws<InvalidOperationException>(serviceProvider.GetRequiredService<IBar>);
                Assert.Equal($"Cannot resolve scoped service '{typeof(IBar)}' from root provider.", exception.Message);
            }
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetService_DoesNotThrow_WhenGetServiceForPolymorphicServiceIsCalledOnRoot_AndTheLastOneIsNotScoped(bool validateOnBuild)
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IBar, Bar>();
            serviceCollection.AddTransient<IBar, Bar3>();
            using var serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = validateOnBuild
            });

            // Act
            var actual = serviceProvider.GetService<IBar>();

            // Assert
            Assert.IsType<Bar3>(actual);
        }

        [Fact]
        public void ScopeValidation_ShouldBeAbleToDistingushGenericCollections_WhenGetServiceIsCalledOnRoot()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<IBar, Bar>();
            serviceCollection.AddScoped<IBar, Bar3>();

            serviceCollection.AddTransient<IBaz, Baz>();
            serviceCollection.AddTransient<IBaz, Baz2>();

            // Act
            using var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
            Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService<IEnumerable<IBar>>());
            var actual = serviceProvider.GetService<IEnumerable<IBaz>>();

            // Assert
            Assert.IsType<Baz>(actual.First());
            Assert.IsType<Baz2>(actual.Last());
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
        public void GetService_DoesNotThrow_WhenGetServiceForServiceWithMultipleImplementationScopesWhereLastIsNotScoped()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IBar, Bar>();
            serviceCollection.AddSingleton<IBar, Bar2>();
            serviceCollection.AddSingleton<IBaz, Baz>();
            var serviceProvider = serviceCollection.BuildServiceProvider(true);


            // Act + Assert
            var exception = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService(typeof(IEnumerable<IBar>)));
            Assert.Equal($"Cannot resolve scoped service '{typeof(IEnumerable<IBar>)}' from root provider.", exception.Message);

            var result = serviceProvider.GetService(typeof(IBar));
            Assert.NotNull(result);
        }


        [Fact]
        public void GetService_Throws_WhenGetServiceForServiceWithMultipleImplementationScopesWhereLastIsScoped()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IBar, Bar>();
            serviceCollection.AddScoped<IBar, Bar2>();
            serviceCollection.AddSingleton<IBaz, Baz>();
            var serviceProvider = serviceCollection.BuildServiceProvider(true);


            // Act + Assert
            var exception = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService(typeof(IEnumerable<IBar>)));
            Assert.Equal($"Cannot resolve scoped service '{typeof(IEnumerable<IBar>)}' from root provider.", exception.Message);

            exception = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService(typeof(IBar)));
            Assert.Equal($"Cannot resolve scoped service '{typeof(IBar)}' from root provider.", exception.Message);
        }

        [Fact]
        public void GetService_DoesNotThrow_WhenGetServiceForNonScopedImplementationWithMultipleImplementationScopesWhereLastIsScoped()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IBar, Bar>();
            serviceCollection.AddSingleton<Bar>();
            serviceCollection.AddScoped<IBar, Bar2>();
            serviceCollection.AddSingleton<IBaz, Baz>();
            var serviceProvider = serviceCollection.BuildServiceProvider(true);


            // Act + Assert
            var exception = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService(typeof(IEnumerable<IBar>)));
            Assert.Equal($"Cannot resolve scoped service '{typeof(IEnumerable<IBar>)}' from root provider.", exception.Message);

            var result = serviceProvider.GetService(typeof(Bar));
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

        private class Bar3 : IBar
        {
        }

        private interface IBaz
        {
        }

        private class Baz : IBaz
        {
        }

        private class Baz2 : IBaz
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

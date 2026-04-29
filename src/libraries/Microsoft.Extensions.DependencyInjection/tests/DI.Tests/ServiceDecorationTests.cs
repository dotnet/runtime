// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class ServiceDecorationTests
    {
        [Fact]
        public void Decorate_TypeBased_WrapsService()
        {
            var services = new ServiceCollection();
            services.AddTransient<IService, InnerService>();
            services.Decorate<IService, DecoratorService>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<IService>();

            var decorator = Assert.IsType<DecoratorService>(service);
            Assert.IsType<InnerService>(decorator.Inner);
        }

        [Fact]
        public void Decorate_FactoryBased_WrapsService()
        {
            var services = new ServiceCollection();
            services.AddTransient<IService, InnerService>();
            services.Decorate<IService>((inner, sp) => new DecoratorService(inner));

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<IService>();

            var decorator = Assert.IsType<DecoratorService>(service);
            Assert.IsType<InnerService>(decorator.Inner);
        }

        [Fact]
        public void Decorate_MultipleDecorators_AppliedInFIFOOrder()
        {
            var services = new ServiceCollection();
            services.AddTransient<IService, InnerService>();
            services.Decorate<IService, DecoratorService>();
            services.Decorate<IService, OuterDecoratorService>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<IService>();

            var outer = Assert.IsType<OuterDecoratorService>(service);
            var inner = Assert.IsType<DecoratorService>(outer.Inner);
            Assert.IsType<InnerService>(inner.Inner);
        }

        [Fact]
        public void Decorate_DeeplyNested_ThreeLevels()
        {
            var services = new ServiceCollection();
            services.AddTransient<IService, InnerService>();
            services.Decorate<IService, DecoratorService>();
            services.Decorate<IService, DecoratorWithLogger>();
            services.AddSingleton<ILogger, Logger>();
            services.Decorate<IService, OuterDecoratorService>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<IService>();

            var outer = Assert.IsType<OuterDecoratorService>(service);
            var middle = Assert.IsType<DecoratorWithLogger>(outer.Inner);
            Assert.IsType<Logger>(middle.Logger);
            var inner = Assert.IsType<DecoratorService>(middle.Inner);
            Assert.IsType<InnerService>(inner.Inner);
        }

        [Fact]
        public void Decorate_TransientDecoratorOnScopedService()
        {
            var services = new ServiceCollection();
            services.AddScoped<IService, InnerService>();
            services.Decorate<IService, DecoratorService>();

            var provider = services.BuildServiceProvider();
            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var service1a = scope1.ServiceProvider.GetRequiredService<IService>();
            var service1b = scope1.ServiceProvider.GetRequiredService<IService>();
            var service2 = scope2.ServiceProvider.GetRequiredService<IService>();

            // Same decorated instance within a scope
            Assert.Same(service1a, service1b);
            // Different across scopes
            Assert.NotSame(service1a, service2);
            // Both are decorated
            Assert.IsType<DecoratorService>(service1a);
            Assert.IsType<DecoratorService>(service2);
        }

        [Fact]
        public void Decorate_SingletonInner_TransientOuter_SameInnerDifferentDecorator()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IService, InnerService>();
            services.Decorate<IService, DecoratorService>();

            var provider = services.BuildServiceProvider();
            var service1 = provider.GetRequiredService<IService>();
            var service2 = provider.GetRequiredService<IService>();

            // Singleton: same decorated instance
            Assert.Same(service1, service2);
            var d1 = Assert.IsType<DecoratorService>(service1);
            var d2 = Assert.IsType<DecoratorService>(service2);
            Assert.Same(d1.Inner, d2.Inner);
        }

        [Fact]
        public void Decorate_NoMatchingService_IsIgnoredAtResolution()
        {
            var services = new ServiceCollection();
            services.Decorate<IService, DecoratorService>();

            // Decoration with no matching service is silently ignored
            var provider = services.BuildServiceProvider();
            Assert.Null(provider.GetService<IService>());
        }

        [Fact]
        public void Decorate_Singleton_ReturnsSameDecoratedInstance()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IService, InnerService>();
            services.Decorate<IService, DecoratorService>();

            var provider = services.BuildServiceProvider();
            var service1 = provider.GetRequiredService<IService>();
            var service2 = provider.GetRequiredService<IService>();

            Assert.Same(service1, service2);
            Assert.IsType<DecoratorService>(service1);
        }

        [Fact]
        public void Decorate_Transient_ReturnsDifferentInstances()
        {
            var services = new ServiceCollection();
            services.AddTransient<IService, InnerService>();
            services.Decorate<IService, DecoratorService>();

            var provider = services.BuildServiceProvider();
            var service1 = provider.GetRequiredService<IService>();
            var service2 = provider.GetRequiredService<IService>();

            Assert.NotSame(service1, service2);
        }

        [Fact]
        public void Decorate_Scoped_ReturnsSameInstanceWithinScope()
        {
            var services = new ServiceCollection();
            services.AddScoped<IService, InnerService>();
            services.Decorate<IService, DecoratorService>();

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var service1 = scope.ServiceProvider.GetRequiredService<IService>();
            var service2 = scope.ServiceProvider.GetRequiredService<IService>();

            Assert.Same(service1, service2);
            Assert.IsType<DecoratorService>(service1);
        }

        [Fact]
        public void Decorate_IEnumerable_DecoratesEachItem()
        {
            var services = new ServiceCollection();
            services.AddTransient<IService, InnerService>();
            services.AddTransient<IService, AnotherInnerService>();
            services.Decorate<IService, DecoratorService>();

            var provider = services.BuildServiceProvider();
            var all = provider.GetServices<IService>().ToList();

            Assert.Equal(2, all.Count);
            Assert.All(all, s => Assert.IsType<DecoratorService>(s));
        }

        [Fact]
        public void Decorate_OpenGeneric_WrapsService()
        {
            var services = new ServiceCollection();
            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            services.Decorate(typeof(IRepository<>), typeof(CachingRepository<>));

            var provider = services.BuildServiceProvider();
            var repo = provider.GetRequiredService<IRepository<string>>();

            var decorator = Assert.IsType<CachingRepository<string>>(repo);
            Assert.IsType<Repository<string>>(decorator.Inner);
        }

        [Fact]
        public void Decorate_DecoratorWithAdditionalDependencies()
        {
            var services = new ServiceCollection();
            services.AddTransient<IService, InnerService>();
            services.AddSingleton<ILogger, Logger>();
            services.Decorate<IService, DecoratorWithLogger>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<IService>();

            var decorator = Assert.IsType<DecoratorWithLogger>(service);
            Assert.IsType<InnerService>(decorator.Inner);
            Assert.IsType<Logger>(decorator.Logger);
        }

        [Fact]
        public void DecorateKeyed_WrapsKeyedService()
        {
            var services = new ServiceCollection();
            services.AddKeyedTransient<IService, InnerService>("key1");
            services.DecorateKeyed<IService, DecoratorService>("key1");

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredKeyedService<IService>("key1");

            var decorator = Assert.IsType<DecoratorService>(service);
            Assert.IsType<InnerService>(decorator.Inner);
        }

        [Fact]
        public void MaterializeDecorations_ClosedType_ProducesWorkingDescriptors()
        {
            var services = new ServiceCollection();
            services.AddTransient<IService, InnerService>();
            services.Decorate<IService, DecoratorService>();

            Assert.Single(services.Decorations);

            DecorationMaterializer.Materialize(services, services.Decorations);
            services.Decorations.Clear();

            Assert.Empty(services.Decorations);

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<IService>();

            var decorator = Assert.IsType<DecoratorService>(service);
            Assert.IsType<InnerService>(decorator.Inner);
        }

        [Fact]
        public void MaterializeDecorations_OpenGeneric_Throws()
        {
            var services = new ServiceCollection();
            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            services.Decorate(typeof(IRepository<>), typeof(CachingRepository<>));

            Assert.Throws<InvalidOperationException>(() =>
                DecorationMaterializer.Materialize(services, services.Decorations));
        }

        [Fact]
        public void Decorate_ExistingInstance_WrapsInstance()
        {
            var instance = new InnerService();
            var services = new ServiceCollection();
            services.AddSingleton<IService>(instance);
            services.Decorate<IService, DecoratorService>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<IService>();

            var decorator = Assert.IsType<DecoratorService>(service);
            Assert.Same(instance, decorator.Inner);
        }

        [Fact]
        public void Decorate_FactoryRegistration_WrapsFactory()
        {
            var services = new ServiceCollection();
            services.AddTransient<IService>(sp => new InnerService());
            services.Decorate<IService, DecoratorService>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<IService>();

            var decorator = Assert.IsType<DecoratorService>(service);
            Assert.IsType<InnerService>(decorator.Inner);
        }

        [Fact]
        public void Decorate_DisposableDecoratorAndInner_BothDisposed()
        {
            var services = new ServiceCollection();
            services.AddTransient<IService, DisposableInnerService>();
            services.Decorate<IService, DisposableDecoratorService>();

            DisposableInnerService inner;
            DisposableDecoratorService decorator;

            using (var provider = services.BuildServiceProvider())
            {
                using var scope = provider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IService>();
                decorator = Assert.IsType<DisposableDecoratorService>(service);
                inner = Assert.IsType<DisposableInnerService>(decorator.Inner);
            }

            Assert.True(decorator.Disposed);
            Assert.True(inner.Disposed);
        }

        [Fact]
        public void Decorate_ConcreteType_WrapsService()
        {
            var services = new ServiceCollection();
            services.AddTransient<ConcreteService>();
            services.Decorate<ConcreteService, ConcreteServiceDecorator>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<ConcreteService>();

            var decorator = Assert.IsType<ConcreteServiceDecorator>(service);
            Assert.NotNull(decorator.Inner);
        }

        [Fact]
        public void Decorate_ServiceRegisteredAfterDecoration_StillApplies()
        {
            var services = new ServiceCollection();
            services.Decorate<IService, DecoratorService>();
            services.AddTransient<IService, InnerService>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<IService>();

            var decorator = Assert.IsType<DecoratorService>(service);
            Assert.IsType<InnerService>(decorator.Inner);
        }

        [Fact]
        public void Decorate_OpenGenericWithConstraints_AppliesWhenConstraintsMet()
        {
            var services = new ServiceCollection();
            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            services.Decorate(typeof(IRepository<>), typeof(ConstrainedCachingRepository<>));

            var provider = services.BuildServiceProvider();

            // string is a class — constraint met, decoration applied
            var stringRepo = provider.GetRequiredService<IRepository<string>>();
            Assert.IsType<ConstrainedCachingRepository<string>>(stringRepo);

            // int is a value type — constraint not met, decoration skipped
            var intRepo = provider.GetRequiredService<IRepository<int>>();
            Assert.IsType<Repository<int>>(intRepo);
        }

        [Fact]
        public void ValidateOnBuild_InvalidDecorator_Throws()
        {
            var services = new ServiceCollection();
            services.AddTransient<IService, InnerService>();
            services.Decorate<IService, InvalidDecoratorService>();

            var ex = Assert.Throws<AggregateException>(() =>
                services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true }));
            Assert.NotEmpty(ex.InnerExceptions);
        }

        [Theory]
        [InlineData(ServiceProviderMode.Runtime)]
        [InlineData(ServiceProviderMode.Expressions)]
        [InlineData(ServiceProviderMode.ILEmit)]
        [InlineData(ServiceProviderMode.Dynamic)]
        private void Decorate_TypeBased_AllEngines(ServiceProviderMode mode)
        {
            var services = new ServiceCollection();
            services.AddTransient<IService, InnerService>();
            services.Decorate<IService, DecoratorService>();

            var provider = services.BuildServiceProvider(mode);
            var service = provider.GetRequiredService<IService>();

            var decorator = Assert.IsType<DecoratorService>(service);
            Assert.IsType<InnerService>(decorator.Inner);
        }

        [Theory]
        [InlineData(ServiceProviderMode.Runtime)]
        [InlineData(ServiceProviderMode.Expressions)]
        [InlineData(ServiceProviderMode.ILEmit)]
        [InlineData(ServiceProviderMode.Dynamic)]
        private void Decorate_FactoryBased_AllEngines(ServiceProviderMode mode)
        {
            var services = new ServiceCollection();
            services.AddTransient<IService, InnerService>();
            services.Decorate<IService>((inner, sp) => new DecoratorService(inner));

            var provider = services.BuildServiceProvider(mode);
            var service = provider.GetRequiredService<IService>();

            var decorator = Assert.IsType<DecoratorService>(service);
            Assert.IsType<InnerService>(decorator.Inner);
        }

        [Theory]
        [InlineData(ServiceProviderMode.Runtime)]
        [InlineData(ServiceProviderMode.Expressions)]
        [InlineData(ServiceProviderMode.ILEmit)]
        [InlineData(ServiceProviderMode.Dynamic)]
        private void Decorate_MultipleDecorators_AllEngines(ServiceProviderMode mode)
        {
            var services = new ServiceCollection();
            services.AddTransient<IService, InnerService>();
            services.Decorate<IService, DecoratorService>();
            services.Decorate<IService, OuterDecoratorService>();

            var provider = services.BuildServiceProvider(mode);
            var service = provider.GetRequiredService<IService>();

            var outer = Assert.IsType<OuterDecoratorService>(service);
            var inner = Assert.IsType<DecoratorService>(outer.Inner);
            Assert.IsType<InnerService>(inner.Inner);
        }

        // --- Test types ---

        public interface IService { }

        public interface ILogger { }

        public class InnerService : IService { }

        public class AnotherInnerService : IService { }

        public class DecoratorService : IService
        {
            public IService Inner { get; }

            public DecoratorService(IService inner)
            {
                Inner = inner;
            }
        }

        public class OuterDecoratorService : IService
        {
            public IService Inner { get; }

            public OuterDecoratorService(IService inner)
            {
                Inner = inner;
            }
        }

        public class DecoratorWithLogger : IService
        {
            public IService Inner { get; }
            public ILogger Logger { get; }

            public DecoratorWithLogger(IService inner, ILogger logger)
            {
                Inner = inner;
                Logger = logger;
            }
        }

        public class Logger : ILogger { }

        public interface IRepository<T> { }

        public class Repository<T> : IRepository<T> { }

        public class CachingRepository<T> : IRepository<T>
        {
            public IRepository<T> Inner { get; }

            public CachingRepository(IRepository<T> inner)
            {
                Inner = inner;
            }
        }
        public class ConstrainedCachingRepository<T> : IRepository<T> where T : class
        {
            public IRepository<T> Inner { get; }

            public ConstrainedCachingRepository(IRepository<T> inner)
            {
                Inner = inner;
            }
        }

        public class ConcreteService { }

        public class ConcreteServiceDecorator : ConcreteService
        {
            public ConcreteService Inner { get; }
            public ConcreteServiceDecorator(ConcreteService inner) => Inner = inner;
        }

        public class DisposableInnerService : IService, IDisposable
        {
            public bool Disposed { get; private set; }
            public void Dispose() => Disposed = true;
        }

        public class DisposableDecoratorService : IService, IDisposable
        {
            public IService Inner { get; }
            public bool Disposed { get; private set; }

            public DisposableDecoratorService(IService inner) => Inner = inner;
            public void Dispose() => Disposed = true;
        }

        // Decorator without a constructor accepting IService — invalid
        public class InvalidDecoratorService : IService
        {
            public InvalidDecoratorService(string notAService) { }
        }
    }
}

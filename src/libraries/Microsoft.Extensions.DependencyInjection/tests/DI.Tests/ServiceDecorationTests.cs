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
        public void Decorate_ThrowsWhenNoMatchingService()
        {
            var services = new ServiceCollection();

            Assert.Throws<InvalidOperationException>(() =>
                services.Decorate<IService, DecoratorService>());
        }

        [Fact]
        public void TryDecorate_DoesNotThrowWhenNoMatchingService()
        {
            var services = new ServiceCollection();
            services.TryDecorate<IService, DecoratorService>();

            // Should not throw, decoration is simply ignored
            var provider = services.BuildServiceProvider();
        }

        [Fact]
        public void Decorate_Singleton_ReturnssSameDecoratedInstance()
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
    }
}

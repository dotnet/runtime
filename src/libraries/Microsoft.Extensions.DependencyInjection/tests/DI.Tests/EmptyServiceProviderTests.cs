// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class EmptyServiceProviderTests
    {
        private static EmptyServiceProvider Empty => EmptyServiceProvider.Instance;

        private static readonly Type[] s_serviceTypes =
        [
            typeof(IServiceProvider),
            typeof(IServiceScopeFactory),
            typeof(IServiceProviderIsService),
            typeof(IServiceProviderIsKeyedService),
            typeof(IKeyedServiceProvider),
            typeof(IServiceScope),
            typeof(IFoo),
            typeof(object),
            typeof(IEnumerable<IFoo>),
            typeof(IEnumerable<int>),
            typeof(IEnumerable<>),
            typeof(List<IFoo>),
        ];

        private static readonly object?[] s_serviceKeys = [null, "key", 42, KeyedService.AnyKey];

        public static TheoryData<Type> ServiceTypes() => new(s_serviceTypes);

        public static TheoryData<Type, object?> ServiceTypeKeyCombinations()
        {
            var data = new TheoryData<Type, object?>();
            foreach (Type serviceType in s_serviceTypes)
            {
                foreach (object? serviceKey in s_serviceKeys)
                {
                    data.Add(serviceType, serviceKey);
                }
            }

            return data;
        }

        private static IKeyedServiceProvider CreateRealProvider() =>
            new ServiceCollection().BuildServiceProvider();

        // Reduces the result of a service-resolution call to a token that can be compared between
        // the real ServiceProvider and EmptyServiceProvider, ignoring the concrete instances each returns.
        private static string Outcome(Func<object> resolve)
        {
            object result;
            try
            {
                result = resolve();
            }
            catch (Exception ex)
            {
                return "throw:" + ex.GetType().FullName;
            }

            return result switch
            {
                null => "null",
                System.Collections.IEnumerable enumerable => "enumerable:" + enumerable.Cast<object>().Count(),
                _ => "instance",
            };
        }

        [Theory]
        [InlineData(typeof(IServiceProvider))]
        [InlineData(typeof(IServiceProviderIsService))]
        [InlineData(typeof(IServiceProviderIsKeyedService))]
        public void GetService_BuiltInServices(Type serviceType)
        {
            Assert.NotNull(((IServiceProvider)Empty).GetService(serviceType));
        }

        [Fact]
        public void GetService_ScopeFactory_ReturnsScopeFactoryForEmptyProvider()
        {
            object service = ((IServiceProvider)Empty).GetService(typeof(IServiceScopeFactory));
            IServiceScopeFactory scopeFactory = Assert.IsAssignableFrom<IServiceScopeFactory>(service);

            using IServiceScope scope = scopeFactory.CreateScope();
            Assert.Same(Empty, scope.ServiceProvider);
        }

        [Fact]
        public void GetKeyedService_NullKey_MatchesGetService()
        {
            Assert.NotNull(((IKeyedServiceProvider)Empty).GetKeyedService(typeof(IServiceProvider), serviceKey: null));
            Assert.Null(((IKeyedServiceProvider)Empty).GetKeyedService(typeof(IFoo), serviceKey: null));
        }

        [Theory]
        [MemberData(nameof(ServiceTypes))]
        public void GetService_MatchesBuiltServiceProvider(Type serviceType)
        {
            IServiceProvider real = CreateRealProvider();

            string expected = Outcome(() => real.GetService(serviceType));
            string actual = Outcome(() => ((IServiceProvider)Empty).GetService(serviceType));

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(ServiceTypeKeyCombinations))]
        public void GetKeyedService_MatchesBuiltServiceProvider(Type serviceType, object? serviceKey)
        {
            IKeyedServiceProvider real = CreateRealProvider();

            string expected = Outcome(() => real.GetKeyedService(serviceType, serviceKey));
            string actual = Outcome(() => ((IKeyedServiceProvider)Empty).GetKeyedService(serviceType, serviceKey));

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(ServiceTypeKeyCombinations))]
        public void GetRequiredKeyedService_MatchesBuiltServiceProvider(Type serviceType, object? serviceKey)
        {
            IKeyedServiceProvider real = CreateRealProvider();

            string expected = Outcome(() => real.GetRequiredKeyedService(serviceType, serviceKey));
            string actual = Outcome(() => ((IKeyedServiceProvider)Empty).GetRequiredKeyedService(serviceType, serviceKey));

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(ServiceTypes))]
        public void IsService_MatchesBuiltServiceProvider(Type serviceType)
        {
            IServiceProvider real = CreateRealProvider();
            var realIsService = (IServiceProviderIsService)real.GetService(typeof(IServiceProviderIsService));

            bool expected = realIsService.IsService(serviceType);
            bool actual = ((IServiceProviderIsService)Empty).IsService(serviceType);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(ServiceTypeKeyCombinations))]
        public void IsKeyedService_MatchesBuiltServiceProvider(Type serviceType, object? serviceKey)
        {
            IKeyedServiceProvider real = CreateRealProvider();
            var realIsKeyedService = (IServiceProviderIsKeyedService)real.GetService(typeof(IServiceProviderIsKeyedService));

            bool expected = realIsKeyedService.IsKeyedService(serviceType, serviceKey);
            bool actual = ((IServiceProviderIsKeyedService)Empty).IsKeyedService(serviceType, serviceKey);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetService_NullServiceType_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ((IServiceProvider)Empty).GetService(null));
        }

        [Fact]
        public void IsService_NullServiceType_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ((IServiceProviderIsService)Empty).IsService(null));
        }

        [Fact]
        public void IsKeyedService_NullServiceType_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ((IServiceProviderIsKeyedService)Empty).IsKeyedService(null, "key"));
        }

        private interface IFoo
        {
        }
    }
}

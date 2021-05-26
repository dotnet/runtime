// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class CallSiteTests
    {
        public static IEnumerable<object[]> TestServiceDescriptors(ServiceLifetime lifetime)
        {
            Func<object, object, bool> compare;

            if (lifetime == ServiceLifetime.Transient)
            {
                // Expect service references to be different for transient descriptors
                compare = (service1, service2) => service1 != service2;
            }
            else
            {
                // Expect service references to be the same for singleton and scoped descriptors
                compare = (service1, service2) => service1 == service2;
            }

            // Implementation Type Descriptor
            yield return new object[]
            {
                new[] { new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), lifetime) },
                typeof(IFakeService),
                compare,
            };
            // Closed Generic Descriptor
            yield return new object[]
            {
                new[] { new ServiceDescriptor(typeof(IFakeOpenGenericService<PocoClass>), typeof(FakeService), lifetime) },
                typeof(IFakeOpenGenericService<PocoClass>),
                compare,
            };
            // Open Generic Descriptor
            yield return new object[]
            {
                new[]
                {
                    new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), lifetime),
                    new ServiceDescriptor(typeof(IFakeOpenGenericService<>), typeof(FakeOpenGenericService<>), lifetime),
                },
                typeof(IFakeOpenGenericService<IFakeService>),
                compare,
            };
            // Factory Descriptor
            yield return new object[]
            {
                new[] { new ServiceDescriptor(typeof(IFakeService), _ => new FakeService(), lifetime) },
                typeof(IFakeService),
                compare,
            };

            if (lifetime == ServiceLifetime.Singleton)
            {
                // Instance Descriptor
                yield return new object[]
                {
                   new[] { new ServiceDescriptor(typeof(IFakeService), new FakeService()) },
                   typeof(IFakeService),
                   compare,
                };
            }
        }

        [Theory]
        [MemberData(nameof(TestServiceDescriptors), ServiceLifetime.Singleton)]
        [MemberData(nameof(TestServiceDescriptors), ServiceLifetime.Scoped)]
        [MemberData(nameof(TestServiceDescriptors), ServiceLifetime.Transient)]
        public void BuiltExpressionWillReturnResolvedServiceWhenAppropriate(
            ServiceDescriptor[] descriptors, Type serviceType, Func<object, object, bool> compare)
        {
            var provider = new ServiceProvider(descriptors, ServiceProviderOptions.Default);

            var callSite = provider.CallSiteFactory.GetCallSite(serviceType, new CallSiteChain());
            var collectionCallSite = provider.CallSiteFactory.GetCallSite(typeof(IEnumerable<>).MakeGenericType(serviceType), new CallSiteChain());

            var compiledCallSite = CompileCallSite(callSite, provider);
            var compiledCollectionCallSite = CompileCallSite(collectionCallSite, provider);

            using var scope = (ServiceProviderEngineScope)provider.CreateScope();

            var service1 = Invoke(callSite, scope);
            var service2 = compiledCallSite(scope);
            var serviceEnumerator = ((IEnumerable)compiledCollectionCallSite(scope)).GetEnumerator();

            Assert.NotNull(service1);
            Assert.True(compare(service1, service2));

            // Service can be IEnumerable resolved. The IEnumerable should have exactly one element.
            Assert.True(serviceEnumerator.MoveNext());
            Assert.True(compare(service1, serviceEnumerator.Current));
            Assert.False(serviceEnumerator.MoveNext());
        }

        [Fact]
        public void BuiltExpressionCanResolveNestedScopedService()
        {
            var descriptors = new ServiceCollection();
            descriptors.AddScoped<ServiceA>();
            descriptors.AddScoped<ServiceB>();
            descriptors.AddScoped<ServiceC>();

            var provider = new ServiceProvider(descriptors, ServiceProviderOptions.Default);
            var callSite = provider.CallSiteFactory.GetCallSite(typeof(ServiceC), new CallSiteChain());
            var compiledCallSite = CompileCallSite(callSite, provider);

            using var scope = (ServiceProviderEngineScope)provider.CreateScope();

            var serviceC = (ServiceC)compiledCallSite(scope);

            Assert.NotNull(serviceC.ServiceB.ServiceA);
            Assert.Equal(serviceC, Invoke(callSite, scope));
        }

        [Theory]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Transient)]
        [InlineData(ServiceLifetime.Singleton)]
        public void BuildExpressionAddsDisposableCaptureForDisposableServices(ServiceLifetime lifetime)
        {
            IServiceCollection descriptors = new ServiceCollection();
            descriptors.Add(ServiceDescriptor.Describe(typeof(ServiceA), typeof(DisposableServiceA), lifetime));
            descriptors.Add(ServiceDescriptor.Describe(typeof(ServiceB), typeof(DisposableServiceB), lifetime));
            descriptors.Add(ServiceDescriptor.Describe(typeof(ServiceC), typeof(DisposableServiceC), lifetime));

            var disposables = new List<object>();
            var provider = new ServiceProvider(descriptors, ServiceProviderOptions.Default);
            provider.Root._captureDisposableCallback = obj =>
            {
                disposables.Add(obj);
            };
            var callSite = provider.CallSiteFactory.GetCallSite(typeof(ServiceC), new CallSiteChain());
            var compiledCallSite = CompileCallSite(callSite, provider);

            var serviceC = (DisposableServiceC)compiledCallSite(provider.Root);

            Assert.Equal(3, disposables.Count);
        }

        [Theory]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Transient)]
        [InlineData(ServiceLifetime.Singleton)]
        public void BuildExpressionAddsDisposableCaptureForDisposableFactoryServices(ServiceLifetime lifetime)
        {
            IServiceCollection descriptors = new ServiceCollection();
            descriptors.Add(ServiceDescriptor.Describe(typeof(ServiceA), typeof(DisposableServiceA), lifetime));
            descriptors.Add(ServiceDescriptor.Describe(typeof(ServiceB), typeof(DisposableServiceB), lifetime));
            descriptors.Add(ServiceDescriptor.Describe(
                typeof(ServiceC), p => new DisposableServiceC(p.GetService<ServiceB>()), lifetime));

            var disposables = new List<object>();
            var provider = new ServiceProvider(descriptors, ServiceProviderOptions.Default);
            provider.Root._captureDisposableCallback = obj =>
            {
                disposables.Add(obj);
            };
            var callSite = provider.CallSiteFactory.GetCallSite(typeof(ServiceC), new CallSiteChain());
            var compiledCallSite = CompileCallSite(callSite, provider);

            var serviceC = (DisposableServiceC)compiledCallSite(provider.Root);

            Assert.Equal(3, disposables.Count);
        }

        [Theory]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Transient)]
        // We are not testing singleton here because singleton resolutions always got through
        // runtime resolver and there is no sense to eliminating call from there
        public void BuildExpressionElidesDisposableCaptureForNonDisposableServices(ServiceLifetime lifetime)
        {
            IServiceCollection descriptors = new ServiceCollection();
            descriptors.Add(ServiceDescriptor.Describe(typeof(ServiceA), typeof(ServiceA), lifetime));
            descriptors.Add(ServiceDescriptor.Describe(typeof(ServiceB), typeof(ServiceB), lifetime));
            descriptors.Add(ServiceDescriptor.Describe(typeof(ServiceC), typeof(ServiceC), lifetime));

            descriptors.AddScoped<ServiceB>();
            descriptors.AddTransient<ServiceC>();

            var disposables = new List<object>();
            var provider = new ServiceProvider(descriptors, ServiceProviderOptions.Default);
            provider.Root._captureDisposableCallback = obj =>
            {
                disposables.Add(obj);
            };
            var callSite = provider.CallSiteFactory.GetCallSite(typeof(ServiceC), new CallSiteChain());
            var compiledCallSite = CompileCallSite(callSite, provider);

            var serviceC = (ServiceC)compiledCallSite(provider.Root);

            Assert.Empty(disposables);
        }

        [Theory]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Transient)]
        // We are not testing singleton here because singleton resolutions always got through
        // runtime resolver and there is no sense to eliminating call from there
        public void BuildExpressionElidesDisposableCaptureForEnumerableServices(ServiceLifetime lifetime)
        {
            IServiceCollection descriptors = new ServiceCollection();
            descriptors.Add(ServiceDescriptor.Describe(typeof(ServiceA), typeof(ServiceA), lifetime));
            descriptors.Add(ServiceDescriptor.Describe(typeof(ServiceD), typeof(ServiceD), lifetime));

            var disposables = new List<object>();
            var provider = new ServiceProvider(descriptors, ServiceProviderOptions.Default);
            provider.Root._captureDisposableCallback = obj =>
            {
                disposables.Add(obj);
            };
            var callSite = provider.CallSiteFactory.GetCallSite(typeof(ServiceD), new CallSiteChain());
            var compiledCallSite = CompileCallSite(callSite, provider);

            var serviceD = (ServiceD)compiledCallSite(provider.Root);

            Assert.Empty(disposables);
        }

        [Fact]
        public void BuiltExpressionRethrowsOriginalExceptionFromConstructor()
        {
            var descriptors = new ServiceCollection();
            descriptors.AddTransient<ClassWithThrowingEmptyCtor>();
            descriptors.AddTransient<ClassWithThrowingCtor>();
            descriptors.AddTransient<IFakeService, FakeService>();

            var provider = new ServiceProvider(descriptors, ServiceProviderOptions.Default);

            var callSite1 = provider.CallSiteFactory.GetCallSite(typeof(ClassWithThrowingEmptyCtor), new CallSiteChain());
            var compiledCallSite1 = CompileCallSite(callSite1, provider);

            var callSite2 = provider.CallSiteFactory.GetCallSite(typeof(ClassWithThrowingCtor), new CallSiteChain());
            var compiledCallSite2 = CompileCallSite(callSite2, provider);

            var ex1 = Assert.Throws<Exception>(() => compiledCallSite1(provider.Root));
            Assert.Equal(nameof(ClassWithThrowingEmptyCtor), ex1.Message);

            var ex2 = Assert.Throws<Exception>(() => compiledCallSite2(provider.Root));
            Assert.Equal(nameof(ClassWithThrowingCtor), ex2.Message);
        }

        [Fact]
        public void DoesNotThrowWhenServiceIsUsedAsEnumerableAndNotInOneCallSite()
        {
            var descriptors = new ServiceCollection();
            descriptors.AddTransient<ServiceA>();
            descriptors.AddTransient<ServiceD>();
            descriptors.AddTransient<ServiceE>();

            var provider = new ServiceProvider(descriptors, ServiceProviderOptions.Default);

            var callSite1 = provider.CallSiteFactory.GetCallSite(typeof(ServiceE), new CallSiteChain());
            var compileCallSite = CompileCallSite(callSite1, provider);

            Assert.NotNull(compileCallSite);
        }

        [Theory]
        [InlineData(ServiceProviderMode.Default)]
        [InlineData(ServiceProviderMode.Dynamic)]
        [InlineData(ServiceProviderMode.Runtime)]
        [InlineData(ServiceProviderMode.Expressions)]
        [InlineData(ServiceProviderMode.ILEmit)]
        private void NoServiceCallsite_DefaultValueNull_DoesNotThrow(ServiceProviderMode mode)
        {
            var descriptors = new ServiceCollection();
            descriptors.AddTransient<ServiceG>();

            var provider = descriptors.BuildServiceProvider(mode);
            ServiceF instance = ActivatorUtilities.CreateInstance<ServiceF>(provider);

            Assert.NotNull(instance);
        }

        private interface IServiceG
        {
        }

        private class ServiceG
        {
            public ServiceG(IServiceG service = null) { }
        }

        private class ServiceF
        {
            public ServiceF(ServiceG service) { }
        }

        private class ServiceD
        {
            public ServiceD(IEnumerable<ServiceA> services)
            {

            }
        }

        private class ServiceA
        {
        }

        private class ServiceB
        {
            public ServiceB(ServiceA serviceA)
            {
                ServiceA = serviceA;
            }

            public ServiceA ServiceA { get; set; }
        }

        private class ServiceC
        {
            public ServiceC(ServiceB serviceB)
            {
                ServiceB = serviceB;
            }

            public ServiceB ServiceB { get; set; }
        }

        private class ServiceE
        {
            public ServiceE(ServiceD serviceD, ServiceA serviceA)
            {
                ServiceD = serviceD;
                ServiceA = serviceA;
            }

            public ServiceD ServiceD { get; set; }

            public ServiceA ServiceA { get; set; }
        }

        private class DisposableServiceA : ServiceA, IDisposable
        {
            public void Dispose()
            {
            }
        }

        private class DisposableServiceB : ServiceB, IDisposable
        {
            public DisposableServiceB(ServiceA serviceA)
                : base(serviceA)
            {
            }

            public void Dispose()
            {
            }
        }

        private class DisposableServiceC : ServiceC, IDisposable
        {
            public DisposableServiceC(ServiceB serviceB)
                : base(serviceB)
            {
            }

            public void Dispose()
            {
            }
        }

        private static object Invoke(ServiceCallSite callSite, ServiceProviderEngineScope scope)
        {
            return CallSiteRuntimeResolver.Instance.Resolve(callSite, scope);
        }

        private static Func<ServiceProviderEngineScope, object> CompileCallSite(ServiceCallSite callSite, ServiceProvider provider)
        {
            return new ExpressionResolverBuilder(provider).Build(callSite);
        }
    }
}

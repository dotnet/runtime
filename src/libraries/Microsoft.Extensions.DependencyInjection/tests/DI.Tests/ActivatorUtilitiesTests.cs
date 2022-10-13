// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
using static Microsoft.Extensions.DependencyInjection.Tests.AsyncServiceScopeTests;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class ActivatorUtilitiesTests
    {
        [Fact]
        public void CreateInstance_ClassWithABCS_UsesTheLongestAvailableConstructor()
        {
            var services = new ServiceCollection();
            services.AddScoped<B>();
            services.AddScoped<S>();
            using var provider = services.BuildServiceProvider();
            var a = new A();
            var c = new C();

            var instance = ActivatorUtilities.CreateInstance<ClassWithABCS>(provider, a, c);

            Assert.NotNull(instance.B);
            Assert.NotNull(instance.S);
            Assert.Same(a, instance.A);
            Assert.Same(c, instance.C);
        }

        [Fact]
        public void CreateInstance_OneCtor_IsRegistered_CreatesInstanceSuccessfully()
        {
            var services = new ServiceCollection();
            services.AddScoped<A>();
            using var provider = services.BuildServiceProvider();

            var instance = ActivatorUtilities.CreateInstance<ClassWithA>(provider);
            Assert.NotNull(instance.A);
        }

        [Fact]
        public void CreateInstance_BadlyConfiguredIServiceProviderIsService_ProperlyCreatesUnambiguousInstance()
        {
            var serviceCollection = new ServiceCollection()
                .AddScoped<A>();
            var fakeServiceProvider = new FakeServiceProvider();
            fakeServiceProvider.Populate(serviceCollection);
            fakeServiceProvider.Build();

            var instance = ActivatorUtilities.CreateInstance<ClassWithA>(fakeServiceProvider);

            Assert.NotNull(instance);
            Assert.True(fakeServiceProvider.FakeServiceProviderIsService.IsServiceGotCalled);
        }

        [Theory]
        [InlineData(typeof(ABCS1))]
        [InlineData(typeof(ABCS2))]
        [InlineData(typeof(ABCS3))]
        public void CreateInstance_DifferentOrders_CreatesInstanceSuccessfully(Type type)
        {
            var services = new ServiceCollection();
            services.AddScoped<S>();
            using var provider = services.BuildServiceProvider();
            B b = new B();
            C c = new C();

            var instance = (ABCS)ActivatorUtilities.CreateInstance(provider, type, b, c);
            Assert.Null(instance.A);
            Assert.Same(b, instance.B);
            Assert.Same(c, instance.C);
            Assert.NotNull(instance.S);
        }

        [Fact]
        public void CreateInstance_NullInstance_HandlesBadInputWithInvalidOperationException()
        {
            var serviceCollection = new ServiceCollection()
                .AddScoped<S>();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            B? nullB = null;

            Assert.Throws<InvalidOperationException>(() =>
                ActivatorUtilities.CreateInstance<ABCS1>(serviceProvider, nullB!, new C()));
        }

        [Fact]
        public void TypeActivatorThrowsOnNullProvider()
        {
            Assert.Throws<ArgumentNullException>(() => ActivatorUtilities.CreateInstance<ClassWithABCS>(null, "hello"));
        }

        [Fact]
        public void CreateInstance_ClassWithABCS_UsesTheLongestAvailableConstructor_ParameterOrderDoesntMatter()
        {
            var services = new ServiceCollection();
            services.AddScoped<B>();
            services.AddScoped<S>();
            using var provider = services.BuildServiceProvider();
            var a = new A();
            var c = new C();

            var instance = ActivatorUtilities.CreateInstance<ClassWithABCS>(provider, c, a);

            Assert.NotNull(instance.B);
            Assert.NotNull(instance.S);
            Assert.Same(a, instance.A);
            Assert.Same(c, instance.C);
        }

        [Theory]
        [InlineData(typeof(ClassWithABC_DefaultConstructorFirst))]
        [InlineData(typeof(ClassWithABC_DefaultConstructorLast))]
        public void CreateInstance_ClassWithABC_ChoosesDefaultConstructorNoMatterCtorOrder(Type instanceType)
        {
            var services = new ServiceCollection();
            using var provider = services.BuildServiceProvider();

            var instance = ActivatorUtilities.CreateInstance(provider, instanceType) as ClassWithABC;

            Assert.NotNull(instance);
            Assert.Null(instance.A);
            Assert.Null(instance.B);
        }

        [Fact]
        public void CreateInstance_ClassWithABCS_BNotRegistered_UsesLongestPossibleCtorTakingAllRegisteredAndPassedParameters()
        {
            var services = new ServiceCollection();
            services.AddScoped<S>();
            using var provider = services.BuildServiceProvider();
            var a = new A();
            var c = new C();

            var instance = ActivatorUtilities.CreateInstance<ClassWithABCS>(provider, c, a);

            Assert.Same(a, instance.A);
            Assert.Same(c, instance.C);
            Assert.NotNull(instance.S);
            Assert.Null(instance.B);
        }

        [Theory]
        [InlineData(typeof(ClassWithABC_FirstConstructorWithAttribute))]
        [InlineData(typeof(ClassWithABC_LastConstructorWithAttribute))]
        public void CreateInstance_ClassWithABC_ConstructorWithAttribute_PicksCtorWithAttr_NoMatterDefinitionOrder(Type instanceType)
        {
            var services = new ServiceCollection();
            var a = new A();
            services.AddSingleton(a);
            using var provider = services.BuildServiceProvider();

            var instance = (ClassWithABC)ActivatorUtilities.CreateInstance(provider, instanceType, new B(), new C());

            Assert.Same(a, instance.A);
        }

        [Fact]
        public void CreateInstance_ClassWithABC_MultipleCtorsWithSameLength_ThrowsAmbiguous()
        {
            string message = $"Multiple constructors for type '{typeof(ClassWithABC_MultipleCtorsWithSameLength)}' were found with length 1.";
            var services = new ServiceCollection();
            var a = new A();
            var b = new B();
            services.AddSingleton(a);
            services.AddSingleton(b);
            using var provider = services.BuildServiceProvider();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                ActivatorUtilities.CreateInstance<ClassWithABC_MultipleCtorsWithSameLength>(provider));
            Assert.Equal(message, exception.Message);
        }
    }

    internal class A { }
    internal class B { }
    internal class C { }
    internal class S { }

    internal class ClassWithABCS : ClassWithABC
    {
        public S S { get; }
        public ClassWithABCS(A a, B b, C c, S s) : base (a, b, c) { S = s; }
        public ClassWithABCS(A a, C c, S s) : this (a, null, c, s) { }
    }

    internal class ClassWithABC_FirstConstructorWithAttribute : ClassWithABC
    {
        [ActivatorUtilitiesConstructor]
        public ClassWithABC_FirstConstructorWithAttribute(A a, B b, C c) : base(a, b, c) { }
        public ClassWithABC_FirstConstructorWithAttribute(B b, C c) : this(null, b, c) { }
    }

    internal class ClassWithABC_LastConstructorWithAttribute : ClassWithABC
    {
        public ClassWithABC_LastConstructorWithAttribute(B b, C c) : this(null, b, c) {  }
        [ActivatorUtilitiesConstructor]
        public ClassWithABC_LastConstructorWithAttribute(A a, B b, C c) : base(a, b , c) { }
    }

    internal class FakeServiceProvider : IServiceProvider
    {
        private IServiceProvider _inner;
        private IServiceCollection _services;
        public IServiceCollection Services => _services;
        public FakeIServiceProviderIsService FakeServiceProviderIsService { get; set; } = new FakeIServiceProviderIsService();

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceProviderIsService))
            {
                return FakeServiceProviderIsService;
            }

            return _inner.GetService(serviceType);
        }

        public void Populate(IServiceCollection services)
        {
            _services = services;
            _services.AddSingleton<FakeServiceProvider>(this);
            _services.AddSingleton<IServiceProviderIsService>((p) => (IServiceProviderIsService)FakeServiceProviderIsService);
        }

        public void Build()
        {
            _inner = _services.BuildServiceProvider();
        }
    }

    internal class FakeIServiceProviderIsService : IServiceProviderIsService
    {
        public FakeIServiceProviderIsService() { }
        public bool IsServiceGotCalled { get; set; }
        public bool IsService(Type serviceType) { IsServiceGotCalled = true; return false; }
    }

    internal class ClassWithA
    {
        public A A { get; }
        public ClassWithA(A a)
        {
            A = a;
        }
    }

    internal class ABCS
    {
        public A A { get; }
        public B B { get; }
        public C C { get; }
        public S S { get; }

        public ABCS(A a, B b, C c)
        {
            A = a;
            B = b;
            C = c;
        }

        public ABCS(B b, C c, S s)
        {
            B = b;
            C = c;
            S = s;
        }
    }

    internal class ABCS1 : ABCS
    {
        public ABCS1(A a, B b, C c) : base(a, b, c) { }
        public ABCS1(B b, C c, S s) : base(b, c, s) { }
    }

    internal class ABCS2 : ABCS
    {
        public ABCS2(B b, A a, C c) : base(a, b, c) { }
        public ABCS2(B b, S s, C c) : base(b, c, s) { }
    }

    internal class ABCS3 : ABCS
    {
        public ABCS3(B b, S s, C c) : base(b, c, s) { }
        public ABCS3(A a, B b, C c) : base(a, b, c) { }
    }

    internal class ClassWithABC
    {
        public A A { get; }
        public B B { get; }
        public C C { get; }

        public ClassWithABC() { }

        public ClassWithABC(A a)
        {
            A = a;
        }

        public ClassWithABC(A a, B b)
        {
            A = a;
            B = b;
        }

        public ClassWithABC(A a, B b, C c)
        {
            A = a;
            B = b;
            C = c;
        }
    }

    internal class ClassWithABC_MultipleCtorsWithSameLength : ClassWithABC
    {
        public ClassWithABC_MultipleCtorsWithSameLength() : base() { }
        public ClassWithABC_MultipleCtorsWithSameLength(A a) : base(a, null) { }
        public ClassWithABC_MultipleCtorsWithSameLength(B b) : base(null, b) { }
    }

    internal class ClassWithABC_DefaultConstructorFirst : ClassWithABC
    {
        public ClassWithABC_DefaultConstructorFirst() : base() { }
        public ClassWithABC_DefaultConstructorFirst(A a) : base(a) { }
        public ClassWithABC_DefaultConstructorFirst(A a, B b) : base (a, b) { }
        public ClassWithABC_DefaultConstructorFirst(A a, B b, C c) : base (a, b, c) { }
    }

    internal class ClassWithABC_DefaultConstructorLast : ClassWithABC
    {
        public ClassWithABC_DefaultConstructorLast(A a, B b, C c) : base (a, b, c) { }
        public ClassWithABC_DefaultConstructorLast(A a, B b) : base (a, b) { }
        public ClassWithABC_DefaultConstructorLast(A a) : base(a) { }
        public ClassWithABC_DefaultConstructorLast() : base() { }
    }
}

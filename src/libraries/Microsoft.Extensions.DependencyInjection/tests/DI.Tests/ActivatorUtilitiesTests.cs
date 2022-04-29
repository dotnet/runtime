// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class ActivatorUtilitiesTests
    {
        [Fact]
        public void ShouldUseLongestAvailableConstructorOnlyIfConstructorsHaveTheSamePriority()
        {
            var services = new ServiceCollection();
            services.AddScoped<B>();
            services.AddScoped<S>();
            using var provider = services.BuildServiceProvider();
            var a = new A();
            var c = new C();

            var instance = ActivatorUtilities.CreateInstance<Creatable>(provider, a, c);

            Assert.Null(instance.B);
        }

        [Theory]
        [InlineData(typeof(DefaultConstructorFirst))]
        [InlineData(typeof(DefaultConstructorLast))]
        public void ChoosesDefaultConstructorNoMatterOrder(Type instanceType)
        {
            var services = new ServiceCollection();
            using var provider = services.BuildServiceProvider();

            var instance = ActivatorUtilities.CreateInstance(provider, instanceType);

            Assert.NotNull(instance);
        }

        [Fact]
        public void ShouldFixIssue_46132()
        {
            var services = new ServiceCollection();
            services.AddScoped<S>();
            using var provider = services.BuildServiceProvider();
            var a = new A();
            var c = new C();

            var instance = ActivatorUtilities.CreateInstance<Creatable>(
                provider, c, a);

            Assert.NotNull(instance);
            Assert.Same(a, instance.A);
            Assert.Same(c, instance.C);
            Assert.NotNull(instance.S);
            Assert.Null(instance.B);
        }

        [Theory]
        [InlineData(typeof(FakeValidationResult))]
        [InlineData(typeof(FakeValidationResultOps))]
        public void ShouldFixIssue_42339(Type instanceType)
        {
            var data = new Dictionary<string, object>();
            var serviceProvider = new FakeServiceProvider(t =>
            {
                if (t == typeof(FakeValidationStatus)) return FakeValidationStatus.Invalid;
                throw new NotImplementedException();
            });

            var instance = (IFakeValidationResult)ActivatorUtilities
                .CreateInstance(serviceProvider, instanceType, "description", data);

            Assert.Equal(FakeValidationStatus.Invalid, instance.Status);
        }

        public static TheoryData<Type> ActivatorUtilitiesData
        {
            get => new TheoryData<Type>
            {
                typeof(FakeValidationResult),
                typeof(FakeValidationResultOps)
            };
        }
    }

    internal class A { }
    internal class B { }
    internal class C { }
    internal class S { }

    internal class Creatable
    {
        public A A { get; }
        public B B { get; }
        public C C { get; }
        public S S { get; }
        public Creatable(A a, B b, C c, S s)
        {
            A = a;
            B = b;
            C = c;
            S = s;
        }

        public Creatable(A a, C c, S s) : this (a, null, c, s) { }
    }

    internal class FakeServiceProvider : IServiceProvider
    {
        private readonly Func<Type, object?> _factory;

        public FakeServiceProvider(Func<Type, object?> factory)
        {
            _factory = factory;
        }

        public object? GetService(Type serviceType) => _factory(serviceType);
    }

    internal interface IFakeValidationResult
    {
        FakeValidationStatus Status { get; }
        string Description { get; }
        IReadOnlyDictionary<string, object> Data { get; }
    }

    internal class FakeValidationResult : IFakeValidationResult
    {
        [ActivatorUtilitiesConstructor]
        public FakeValidationResult(FakeValidationStatus status, string description,
           IReadOnlyDictionary<string, object> data)
        {
            Status = status;
            Description = description;
            Data = data;
        }

        public FakeValidationResult(string description, IReadOnlyDictionary<string, object> data)
            : this(FakeValidationStatus.Valid, description, data) { }

        public FakeValidationStatus Status { get; }
        public string Description { get; }
        public IReadOnlyDictionary<string, object> Data { get; }
    }

    internal class FakeValidationResultOps : IFakeValidationResult
    {
        public FakeValidationResultOps(string description, IReadOnlyDictionary<string, object> data)
            : this(FakeValidationStatus.Valid, description, data) { }

        [ActivatorUtilitiesConstructor]
        public FakeValidationResultOps(FakeValidationStatus status, string description,
           IReadOnlyDictionary<string, object> data)
        {
            Status = status;
            Description = description;
            Data = data;
        }

        public FakeValidationStatus Status { get; }
        public string Description { get; }
        public IReadOnlyDictionary<string, object> Data { get; }
    }

    internal enum FakeValidationStatus
    {
        Valid,
        Invalid
    }

    internal class DefaultConstructorFirst
    {
        public A A { get; }
        public B B { get; }

        public DefaultConstructorFirst() { }

        public DefaultConstructorFirst(A a)
        {
            A = a;
        }

        public DefaultConstructorFirst(A a, B b)
        {
            A = a;
            B = b;
        }
    }

    internal class DefaultConstructorLast
    {
        public A A { get; }
        public B B { get; }

        public DefaultConstructorLast(A a, B b)
        {
            A = a;
            B = b;
        }

        public DefaultConstructorLast(A a)
        {
            A = a;
        }

        public DefaultConstructorLast() { }
    }
}

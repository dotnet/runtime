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
        public void ShouldTryToUseAllAvailableConstructorsBeforeThrowingActivationException()
        {   // https://github.com/dotnet/runtime/issues/46132
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
        [InlineData(typeof(IClassWithAttribute.FirstConstructorWithAttribute))]
        [InlineData(typeof(IClassWithAttribute.LastConstructorWithAttribute))]
        public void ConstructorWithAttributeShouldHaveTheHighestPriorityNoMatterOrderDefinition(Type instanceType)
        {   // https://github.com/dotnet/runtime/issues/42339
            var services = new ServiceCollection();
            var a = new A();
            services.AddSingleton(a);
            using var provider = services.BuildServiceProvider();

            var instance = (IClassWithAttribute)ActivatorUtilities
                .CreateInstance(provider, instanceType, new B(), new C());

            Assert.Same(a, instance.A);
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

    internal interface IClassWithAttribute
    {
        public A A { get; }
        public B B { get; }
        public C C { get; }

        public class FirstConstructorWithAttribute : IClassWithAttribute
        {
            public A A { get; }
            public B B { get; }
            public C C { get; }

            [ActivatorUtilitiesConstructor]
            public FirstConstructorWithAttribute(A a, B b, C c)
            {
                A = a;
                B = b;
                C = c;
            }

            public FirstConstructorWithAttribute(B b, C c) : this(null, b, c) {  }
        }

        public class LastConstructorWithAttribute : IClassWithAttribute
        {
            public A A { get; }
            public B B { get; }
            public C C { get; }

            public LastConstructorWithAttribute(B b, C c) : this(null, b, c) {  }


            [ActivatorUtilitiesConstructor]
            public LastConstructorWithAttribute(A a, B b, C c)
            {
                A = a;
                B = b;
                C = c;
            }
        }
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection.Tests.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class CircularDependencyTests
    {
        [Fact]
        public void SelfCircularDependency()
        {
            var expectedMessage = "A circular dependency was detected for the service of type " +
                                  "'Microsoft.Extensions.DependencyInjection.Tests.Fakes.SelfCircularDependency'." +
                                  Environment.NewLine +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.SelfCircularDependency -> " +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.SelfCircularDependency";

            var serviceProvider = new ServiceCollection()
                .AddTransient<SelfCircularDependency>()
                .BuildServiceProvider();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<SelfCircularDependency>());

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void SelfCircularDependencyInEnumerable()
        {
            var expectedMessage = "A circular dependency was detected for the service of type " +
                                  "'Microsoft.Extensions.DependencyInjection.Tests.Fakes.SelfCircularDependency'." +
                                  Environment.NewLine +
                                  "System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyInjection.Tests.Fakes.SelfCircularDependency> -> " +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.SelfCircularDependency -> " +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.SelfCircularDependency";

            var serviceProvider = new ServiceCollection()
                .AddTransient<SelfCircularDependency>()
                .BuildServiceProvider();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<IEnumerable<SelfCircularDependency>>());

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void SelfCircularDependencyGenericDirect()
        {
            var expectedMessage = "A circular dependency was detected for the service of type " +
                                  "'Microsoft.Extensions.DependencyInjection.Tests.Fakes.SelfCircularDependencyGeneric<string>'." +
                                  Environment.NewLine +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.SelfCircularDependencyGeneric<string> -> " +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.SelfCircularDependencyGeneric<string>";

            var serviceProvider = new ServiceCollection()
                .AddTransient<SelfCircularDependencyGeneric<string>>()
                .BuildServiceProvider();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<SelfCircularDependencyGeneric<string>>());

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void SelfCircularDependencyGenericIndirect()
        {
            var expectedMessage = "A circular dependency was detected for the service of type " +
                                  "'Microsoft.Extensions.DependencyInjection.Tests.Fakes.SelfCircularDependencyGeneric<string>'." +
                                  Environment.NewLine +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.SelfCircularDependencyGeneric<int> -> " +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.SelfCircularDependencyGeneric<string> -> " +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.SelfCircularDependencyGeneric<string>";

            var serviceProvider = new ServiceCollection()
                .AddTransient<SelfCircularDependencyGeneric<int>>()
                .AddTransient<SelfCircularDependencyGeneric<string>>()
                .BuildServiceProvider();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<SelfCircularDependencyGeneric<int>>());

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void NoCircularDependencyGeneric()
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton(new SelfCircularDependencyGeneric<string>())
                .AddTransient<SelfCircularDependencyGeneric<int>>()
                .BuildServiceProvider();

            // This will not throw because we are creating an instance of the first time
            // using the parameterless constructor which has no circular dependency
            var resolvedService = serviceProvider.GetRequiredService<SelfCircularDependencyGeneric<int>>();
            Assert.NotNull(resolvedService);
        }

        [Fact]
        public void SelfCircularDependencyWithInterface()
        {
            var expectedMessage = "A circular dependency was detected for the service of type " +
                                  "'Microsoft.Extensions.DependencyInjection.Tests.Fakes.ISelfCircularDependencyWithInterface'." +
                                  Environment.NewLine +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.SelfCircularDependencyWithInterface -> " +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.ISelfCircularDependencyWithInterface" +
                                  "(Microsoft.Extensions.DependencyInjection.Tests.Fakes.SelfCircularDependencyWithInterface) -> " +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.ISelfCircularDependencyWithInterface";

            var serviceProvider = new ServiceCollection()
                .AddTransient<ISelfCircularDependencyWithInterface, SelfCircularDependencyWithInterface>()
                .AddTransient<SelfCircularDependencyWithInterface>()
                .BuildServiceProvider();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<SelfCircularDependencyWithInterface>());

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void DirectCircularDependency()
        {
            var expectedMessage = "A circular dependency was detected for the service of type " +
                                  "'Microsoft.Extensions.DependencyInjection.Tests.Fakes.DirectCircularDependencyA'." +
                                  Environment.NewLine +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.DirectCircularDependencyA -> " +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.DirectCircularDependencyB -> " +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.DirectCircularDependencyA";

            var serviceProvider = new ServiceCollection()
                .AddSingleton<DirectCircularDependencyA>()
                .AddSingleton<DirectCircularDependencyB>()
                .BuildServiceProvider();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<DirectCircularDependencyA>());

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void IndirectCircularDependency()
        {
            var expectedMessage = "A circular dependency was detected for the service of type " +
                                  "'Microsoft.Extensions.DependencyInjection.Tests.Fakes.IndirectCircularDependencyA'." +
                                  Environment.NewLine +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.IndirectCircularDependencyA -> " +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.IndirectCircularDependencyB -> " +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.IndirectCircularDependencyC -> " +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.IndirectCircularDependencyA";

            var serviceProvider = new ServiceCollection()
                .AddSingleton<IndirectCircularDependencyA>()
                .AddTransient<IndirectCircularDependencyB>()
                .AddTransient<IndirectCircularDependencyC>()
                .BuildServiceProvider();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<IndirectCircularDependencyA>());

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void NoCircularDependencySameTypeMultipleTimes()
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<NoCircularDependencySameTypeMultipleTimesA>()
                .AddTransient<NoCircularDependencySameTypeMultipleTimesB>()
                .AddTransient<NoCircularDependencySameTypeMultipleTimesC>()
                .BuildServiceProvider();

            var resolvedService = serviceProvider.GetRequiredService<NoCircularDependencySameTypeMultipleTimesA>();
            Assert.NotNull(resolvedService);
        }

        [Fact]
        public void DependencyOnCircularDependency()
        {
            var expectedMessage = "A circular dependency was detected for the service of type " +
                                  "'Microsoft.Extensions.DependencyInjection.Tests.Fakes.DirectCircularDependencyA'." +
                                  Environment.NewLine +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.DependencyOnCircularDependency -> " +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.DirectCircularDependencyA -> " +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.DirectCircularDependencyB -> " +
                                  "Microsoft.Extensions.DependencyInjection.Tests.Fakes.DirectCircularDependencyA";

            var serviceProvider = new ServiceCollection()
                .AddTransient<DependencyOnCircularDependency>()
                .AddTransient<DirectCircularDependencyA>()
                .AddTransient<DirectCircularDependencyB>()
                .BuildServiceProvider();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<DependencyOnCircularDependency>());

            Assert.Equal(expectedMessage, exception.Message);
        }
    }
}

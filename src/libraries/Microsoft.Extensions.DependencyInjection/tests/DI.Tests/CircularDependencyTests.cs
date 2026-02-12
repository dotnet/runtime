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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FactoryCircularDependency_DetectedWithValidateOnBuild(bool validateOnBuild)
        {
            // This test demonstrates circular dependency through factory functions
            // Service A has constructor dependency on Service B
            // Service B is registered with a factory that requests Service A from IServiceProvider
            // This creates a circular dependency: A -> B -> A

            var services = new ServiceCollection();
            services.AddSingleton<FactoryCircularDependencyA>();
            services.AddSingleton<FactoryCircularDependencyB>(sp => 
            {
                // Factory tries to resolve FactoryCircularDependencyA, creating a circle
                var a = sp.GetRequiredService<FactoryCircularDependencyA>();
                return new FactoryCircularDependencyB();
            });

            if (validateOnBuild)
            {
                // With ValidateOnBuild = true, the circular dependency should be detected at build time
                var exception = Assert.Throws<AggregateException>(() =>
                    services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true }));

                Assert.Contains("circular dependency", exception.ToString(), StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // Without ValidateOnBuild, the circular dependency causes a deadlock or stack overflow at resolution time
                // This test demonstrates the issue - it would hang/deadlock without the fix
                var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = false });

                // This will either throw InvalidOperationException (good) or deadlock/timeout (bad - issue #88390)
                Assert.ThrowsAny<Exception>(() =>
                {
                    var a = serviceProvider.GetRequiredService<FactoryCircularDependencyA>();
                });
            }
        }
    }
}

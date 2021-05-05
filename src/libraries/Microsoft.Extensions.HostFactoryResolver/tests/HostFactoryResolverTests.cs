// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MockHostTypes;
using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class HostFactoryResolverTests
    {
        [Fact]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BuildWebHostPatternTestSite.Program))]
        public void BuildWebHostPattern_CanFindWebHost()
        {
            var factory = HostFactoryResolver.ResolveWebHostFactory<IWebHost>(typeof(BuildWebHostPatternTestSite.Program).Assembly);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IWebHost>(factory(Array.Empty<string>()));
        }

        [Fact]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BuildWebHostPatternTestSite.Program))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IWebHost))]
        public void BuildWebHostPattern_CanFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(BuildWebHostPatternTestSite.Program).Assembly);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IServiceProvider>(factory(Array.Empty<string>()));
        }

        [Fact]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BuildWebHostInvalidSignature.Program))]
        public void BuildWebHostPattern__Invalid_CantFindWebHost()
        {
            var factory = HostFactoryResolver.ResolveWebHostFactory<IWebHost>(typeof(BuildWebHostInvalidSignature.Program).Assembly);

            Assert.Null(factory);
        }

        [Fact]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BuildWebHostInvalidSignature.Program))]
        public void BuildWebHostPattern__Invalid_CantFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(BuildWebHostInvalidSignature.Program).Assembly);

            Assert.Null(factory);
        }

        [Fact]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateWebHostBuilderPatternTestSite.Program))]
        public void CreateWebHostBuilderPattern_CanFindWebHostBuilder()
        {
            var factory = HostFactoryResolver.ResolveWebHostBuilderFactory<IWebHostBuilder>(typeof(CreateWebHostBuilderPatternTestSite.Program).Assembly);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IWebHostBuilder>(factory(Array.Empty<string>()));
        }

        [Fact]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateWebHostBuilderPatternTestSite.Program))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IWebHost))]
        public void CreateWebHostBuilderPattern_CanFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(CreateWebHostBuilderPatternTestSite.Program).Assembly);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IServiceProvider>(factory(Array.Empty<string>()));
        }

        [Fact]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateWebHostBuilderInvalidSignature.Program))]
        public void CreateWebHostBuilderPattern__Invalid_CantFindWebHostBuilder()
        {
            var factory = HostFactoryResolver.ResolveWebHostBuilderFactory<IWebHostBuilder>(typeof(CreateWebHostBuilderInvalidSignature.Program).Assembly);

            Assert.Null(factory);
        }

        [Fact]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateWebHostBuilderInvalidSignature.Program))]
        public void CreateWebHostBuilderPattern__InvalidReturnType_CanFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(CreateWebHostBuilderInvalidSignature.Program).Assembly);

            Assert.NotNull(factory);
            Assert.Null(factory(Array.Empty<string>()));
        }

        [Fact]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateHostBuilderPatternTestSite.Program))]
        public void CreateHostBuilderPattern_CanFindHostBuilder()
        {
            var factory = HostFactoryResolver.ResolveHostBuilderFactory<IHostBuilder>(typeof(CreateHostBuilderPatternTestSite.Program).Assembly);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IHostBuilder>(factory(Array.Empty<string>()));
        }

        [Fact]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateHostBuilderPatternTestSite.Program))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Host))]
        public void CreateHostBuilderPattern_CanFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(CreateHostBuilderPatternTestSite.Program).Assembly);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IServiceProvider>(factory(Array.Empty<string>()));
        }

        [Fact]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateHostBuilderInvalidSignature.Program))]
        public void CreateHostBuilderPattern__Invalid_CantFindHostBuilder()
        {
            var factory = HostFactoryResolver.ResolveHostBuilderFactory<IHostBuilder>(typeof(CreateHostBuilderInvalidSignature.Program).Assembly);

            Assert.Null(factory);
        }

        [Fact]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateHostBuilderInvalidSignature.Program))]
        public void CreateHostBuilderPattern__Invalid_CantFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(CreateHostBuilderInvalidSignature.Program).Assembly);

            Assert.Null(factory);
        }
    }
}

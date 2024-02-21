// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using MockHostTypes;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class HostFactoryResolverTests
    {
        private static readonly TimeSpan s_WaitTimeout = TimeSpan.FromSeconds(20);

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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BuildWebHostInvalidSignature.Program))]
        public void BuildWebHostPattern__Invalid_CantFindWebHost()
        {
            var factory = HostFactoryResolver.ResolveWebHostFactory<IWebHost>(typeof(BuildWebHostInvalidSignature.Program).Assembly);

            Assert.Null(factory);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BuildWebHostInvalidSignature.Program))]
        public void BuildWebHostPattern__Invalid_CantFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(BuildWebHostInvalidSignature.Program).Assembly);

            Assert.NotNull(factory);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateWebHostBuilderPatternTestSite.Program))]
        public void CreateWebHostBuilderPattern_CanFindWebHostBuilder()
        {
            var factory = HostFactoryResolver.ResolveWebHostBuilderFactory<IWebHostBuilder>(typeof(CreateWebHostBuilderPatternTestSite.Program).Assembly);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IWebHostBuilder>(factory(Array.Empty<string>()));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateWebHostBuilderPatternTestSite.Program))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IWebHost))]
        public void CreateWebHostBuilderPattern_CanFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(CreateWebHostBuilderPatternTestSite.Program).Assembly);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IServiceProvider>(factory(Array.Empty<string>()));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateWebHostBuilderInvalidSignature.Program))]
        public void CreateWebHostBuilderPattern__Invalid_CantFindWebHostBuilder()
        {
            var factory = HostFactoryResolver.ResolveWebHostBuilderFactory<IWebHostBuilder>(typeof(CreateWebHostBuilderInvalidSignature.Program).Assembly);

            Assert.Null(factory);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateWebHostBuilderInvalidSignature.Program))]
        public void CreateWebHostBuilderPattern__InvalidReturnType_CanFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(CreateWebHostBuilderInvalidSignature.Program).Assembly);

            Assert.NotNull(factory);
            Assert.Null(factory(Array.Empty<string>()));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateHostBuilderPatternTestSite.Program))]
        public void CreateHostBuilderPattern_CanFindHostBuilder()
        {
            var factory = HostFactoryResolver.ResolveHostBuilderFactory<IHostBuilder>(typeof(CreateHostBuilderPatternTestSite.Program).Assembly);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IHostBuilder>(factory(Array.Empty<string>()));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateHostBuilderPatternTestSite.Program))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Host))]
        public void CreateHostBuilderPattern_CanFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(CreateHostBuilderPatternTestSite.Program).Assembly);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IServiceProvider>(factory(Array.Empty<string>()));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateHostBuilderInvalidSignature.Program))]
        public void CreateHostBuilderPattern__Invalid_CantFindHostBuilder()
        {
            var factory = HostFactoryResolver.ResolveHostBuilderFactory<IHostBuilder>(typeof(CreateHostBuilderInvalidSignature.Program).Assembly);

            Assert.Null(factory);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateHostBuilderInvalidSignature.Program))]
        public void CreateHostBuilderPattern__Invalid_CantFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(CreateHostBuilderInvalidSignature.Program).Assembly, s_WaitTimeout);

            Assert.NotNull(factory);
            Assert.Throws<InvalidOperationException>(() => factory(Array.Empty<string>()));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NoSpecialEntryPointPattern.Program))]
        public void NoSpecialEntryPointPattern()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(NoSpecialEntryPointPattern.Program).Assembly, s_WaitTimeout);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IServiceProvider>(factory(Array.Empty<string>()));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NoSpecialEntryPointPattern.Program))]
        public void NoSpecialEntryPointPatternHostBuilderConfigureHostBuilderCallbackIsCalled()
        {
            bool called = false;
            void ConfigureHostBuilder(object hostBuilder)
            {
                Assert.IsAssignableFrom<IHostBuilder>(hostBuilder);
                called = true;
            }

            var factory = HostFactoryResolver.ResolveHostFactory(typeof(NoSpecialEntryPointPattern.Program).Assembly, waitTimeout: s_WaitTimeout, configureHostBuilder: ConfigureHostBuilder);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IHost>(factory(Array.Empty<string>()));
            Assert.True(called);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NoSpecialEntryPointPattern.Program))]
        public void NoSpecialEntryPointPatternBuildsThenThrowsCallsEntryPointCompletedCallback()
        {
            var wait = new ManualResetEventSlim(false);
            Exception? entryPointException = null;
            void EntryPointCompleted(Exception? exception)
            {
                entryPointException = exception;
                wait.Set();
            }

            var factory = HostFactoryResolver.ResolveHostFactory(typeof(NoSpecialEntryPointPattern.Program).Assembly, waitTimeout: s_WaitTimeout, stopApplication: false, entrypointCompleted: EntryPointCompleted);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IHost>(factory(Array.Empty<string>()));
            Assert.True(wait.Wait(s_WaitTimeout));
            Assert.Null(entryPointException);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NoSpecialEntryPointPatternBuildsThenThrows.Program))]
        public void NoSpecialEntryPointPatternBuildsThenThrowsCallsEntryPointCompletedCallbackWithException()
        {
            var wait = new ManualResetEventSlim(false);
            Exception? entryPointException = null;
            void EntryPointCompleted(Exception? exception)
            {
                entryPointException = exception;
                wait.Set();
            }

            var factory = HostFactoryResolver.ResolveHostFactory(typeof(NoSpecialEntryPointPatternBuildsThenThrows.Program).Assembly, waitTimeout: s_WaitTimeout, stopApplication: false, entrypointCompleted: EntryPointCompleted);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IHost>(factory(Array.Empty<string>()));
            Assert.True(wait.Wait(s_WaitTimeout));
            Assert.NotNull(entryPointException);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NoSpecialEntryPointPatternThrows.Program))]
        public void NoSpecialEntryPointPatternThrows()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(NoSpecialEntryPointPatternThrows.Program).Assembly, s_WaitTimeout);

            Assert.NotNull(factory);
            Assert.Throws<Exception>(() => factory(Array.Empty<string>()));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NoSpecialEntryPointPatternExits.Program))]
        public void NoSpecialEntryPointPatternExits()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(NoSpecialEntryPointPatternExits.Program).Assembly, s_WaitTimeout);

            Assert.NotNull(factory);
            Assert.Throws<InvalidOperationException>(() => factory(Array.Empty<string>()));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NoSpecialEntryPointPatternHangs.Program))]
        public void NoSpecialEntryPointPatternHangs()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(NoSpecialEntryPointPatternHangs.Program).Assembly, s_WaitTimeout);

            Assert.NotNull(factory);
            Assert.Throws<InvalidOperationException>(() => factory(Array.Empty<string>()));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NoSpecialEntryPointPatternMainNoArgs.Program))]
        public void NoSpecialEntryPointPatternMainNoArgs()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(NoSpecialEntryPointPatternMainNoArgs.Program).Assembly, s_WaitTimeout);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IServiceProvider>(factory(Array.Empty<string>()));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Program", "TopLevelStatements")]
        public void TopLevelStatements()
        {
            var assembly = Assembly.Load("TopLevelStatements");
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(assembly, s_WaitTimeout);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IServiceProvider>(factory(Array.Empty<string>()));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Program", "TopLevelStatementsTestsTimeout")]
        public void TopLevelStatementsTestsTimeout()
        {
            var assembly = Assembly.Load("TopLevelStatementsTestsTimeout");
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(assembly, s_WaitTimeout);

            Assert.NotNull(factory);
            Assert.Throws<InvalidOperationException>(() => factory(Array.Empty<string>()));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Program", "ApplicationNameSetFromArgument")]
        public void ApplicationNameSetFromArgument()
        {
            Assembly assembly = Assembly.Load("ApplicationNameSetFromArgument");
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(assembly, s_WaitTimeout);
            IServiceProvider? serviceProvider = factory(Array.Empty<string>());

            var configuration = (IConfiguration)serviceProvider.GetService(typeof(IConfiguration));
            Assert.Contains("ApplicationNameSetFromArgument", configuration["applicationName"]);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NoSpecialEntryPointPattern.Program))]
        public void NoSpecialEntryPointPatternCanRunInParallel()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(NoSpecialEntryPointPattern.Program).Assembly, s_WaitTimeout);
            Assert.NotNull(factory);

            var tasks = new Task<IServiceProvider>[30];
            int index = 0;
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[index++] = Task.Run(() => factory(Array.Empty<string>()));
            }

            Task.WaitAll(tasks);

            foreach (var t in tasks)
            {
                Assert.IsAssignableFrom<IServiceProvider>(t.Result);
            }
        }
    }
}

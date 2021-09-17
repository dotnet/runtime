// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public abstract class RidAssetResolutionBase : ComponentDependencyResolutionBase
    {
        protected SharedTestState SharedState { get; }

        protected RidAssetResolutionBase(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        protected abstract void RunTest(
            Action<NetCoreAppBuilder.RuntimeLibraryBuilder> assetsCustomizer,
            string rid,
            string includedAssemblyPaths,
            string excludedAssemblyPaths,
            string includedNativeLibraryPaths,
            string excludedNativeLibraryPaths,
            Action<NetCoreAppBuilder> appCustomizer = null);

        [Theory]
        [InlineData("win", "win/WindowsAssembly.dll", "linux/LinuxAssembly.dll")]
        [InlineData("win10-x64", "win/WindowsAssembly.dll", "linux/LinuxAssembly.dll")]
        [InlineData("linux-x64", "linux/LinuxAssembly.dll", "win/WindowsAssembly.dll")]
        public void RidSpecificAssembly(string rid, string includedPath, string excludedPath)
        {
            RunTest(
                p => p
                    .WithAssemblyGroup("win", g => g.WithAsset("win/WindowsAssembly.dll"))
                    .WithAssemblyGroup("linux", g => g.WithAsset("linux/LinuxAssembly.dll")),
                rid, includedPath, excludedPath, null, null);
        }

        [Theory]
        [InlineData("win", "win", "linux")]
        [InlineData("win10-x64", "win", "linux")]
        [InlineData("linux-x64", "linux", "win")]
        public void RidSpecificNativeLibrary(string rid, string includedPath, string excludedPath)
        {
            RunTest(
                p => p
                    .WithNativeLibraryGroup("win", g => g.WithAsset("win/WindowsNativeLibrary.dll"))
                    .WithNativeLibraryGroup("linux", g => g.WithAsset("linux/LinuxNativeLibrary.so")),
                rid, null, null, includedPath, excludedPath);
        }

        [Theory]
        [InlineData("win10-x64", "win-x64/ManagedWin64.dll")]
        [InlineData("win10-x86", "win/ManagedWin.dll")]
        [InlineData("linux-x64", "any/ManagedAny.dll")]
        public void MostSpecificRidAssemblySelected(string rid, string expectedPath)
        {
            RunTest(
                p => p
                    .WithAssemblyGroup("any", g => g.WithAsset("any/ManagedAny.dll"))
                    .WithAssemblyGroup("win", g => g.WithAsset("win/ManagedWin.dll"))
                    .WithAssemblyGroup("win-x64", g => g.WithAsset("win-x64/ManagedWin64.dll")),
                rid, expectedPath, null, null, null);
        }

        [Theory]
        [InlineData("win10-x64", "win-x64")]
        [InlineData("win10-x86", "win")]
        [InlineData("linux-x64", "any")]
        public void MostSpecificRidNativeLibrarySelected(string rid, string expectedPath)
        {
            RunTest(
                p => p
                    .WithNativeLibraryGroup("any", g => g.WithAsset("any/NativeAny.dll"))
                    .WithNativeLibraryGroup("win", g => g.WithAsset("win/NativeWin.dll"))
                    .WithNativeLibraryGroup("win-x64", g => g.WithAsset("win-x64/NativeWin64.dll")),
                rid, null, null, expectedPath, null);
        }

        [Theory]
        [InlineData("win10-x64", "win/ManagedWin.dll", "native/win-x64")]
        [InlineData("win10-x86", "win/ManagedWin.dll", "native/win-x86")]
        [InlineData("linux-x64", "any/ManagedAny.dll", "native/linux")]
        public void MostSpecificRidAssemblySelectedPerType(string rid, string expectedAssemblyPath, string expectedNativePath)
        {
            RunTest(
                p => p
                    .WithAssemblyGroup("any", g => g.WithAsset("any/ManagedAny.dll"))
                    .WithAssemblyGroup("win", g => g.WithAsset("win/ManagedWin.dll"))
                    .WithNativeLibraryGroup("win-x64", g => g.WithAsset("native/win-x64/n.dll"))
                    .WithNativeLibraryGroup("win-x86", g => g.WithAsset("native/win-x86/n.dll"))
                    .WithNativeLibraryGroup("linux", g => g.WithAsset("native/linux/n.so")),
                rid, expectedAssemblyPath, null, expectedNativePath, null);
        }

        [Theory]
        // For "win" RIDs the DependencyLib which is RID-agnostic will not be included, 
        // since there are other assembly (runtime) assets with more specific RID match.
        [InlineData("win10-x64", "win/ManagedWin.dll;win/AnotherWin.dll", "native/win10-x64;native/win10-x64-2")]
        [InlineData("win10-x86", "win/ManagedWin.dll;win/AnotherWin.dll", "native/win-x86")]
        // For "linux" on the other hand the DependencyLib will be resolved because there are
        // no RID-specific assembly assets available.
        [InlineData("linux-x64", "", "native/linux")]
        public void MostSpecificRidAssemblySelectedPerTypeMultipleAssets(string rid, string expectedAssemblyPath, string expectedNativePath)
        {
            // Skip the component on self-contained app case as that won't work and our simple checks will be broken
            // in this complex test case (the PortableLib and PortableLib2 will always resolve, even in this broken case).
            if (GetType() == typeof(PortableComponentOnSelfContainedAppRidAssetResolution))
            {
                return;
            }

            RunTest(
                assetsCustomizer: null,
                appCustomizer: b => b
                    .WithPackage("ridSpecificLib", "1.0.0", p => p
                        .WithAssemblyGroup(null, g => g.WithAsset("DependencyLib.dll"))
                        .WithAssemblyGroup("win", g => g.WithAsset("win/ManagedWin.dll"))
                        .WithAssemblyGroup("win", g => g.WithAsset("win/AnotherWin.dll"))
                        .WithNativeLibraryGroup("win10-x64", g => g.WithAsset("native/win10-x64/n1.dll"))
                        .WithNativeLibraryGroup("win10-x64", g => g.WithAsset("native/win10-x64/n2.dll"))
                        .WithNativeLibraryGroup("win10-x64", g => g.WithAsset("native/win10-x64-2/n3.dll"))
                        .WithNativeLibraryGroup("win-x86", g => g.WithAsset("native/win-x86/n1.dll"))
                        .WithNativeLibraryGroup("win-x86", g => g.WithAsset("native/win-x86/n2.dll"))
                        .WithNativeLibraryGroup("linux", g => g.WithAsset("native/linux/n.so")))
                    .WithPackage("ridAgnosticLib", "2.0.0", p => p
                        .WithAssemblyGroup(null, g => g.WithAsset("PortableLib.dll").WithAsset("PortableLib2.dll"))),
                rid: rid,
                // The PortableLib an PortableLib2 are from a separate package which has no RID specific assets, 
                // so the RID-agnostic assets are always included
                includedAssemblyPaths: expectedAssemblyPath + ";PortableLib.dll;PortableLib2.dll", excludedAssemblyPaths: null,
                includedNativeLibraryPaths: expectedNativePath, excludedNativeLibraryPaths: null);
        }

        public class SharedTestState : ComponentSharedTestStateBase
        {
            public SharedTestState() : base()
            {
            }
        }
    }

    // Run the tests on a framework dependent app
    public class PortableAppRidAssetResolution :
        RidAssetResolutionBase,
        IClassFixture<RidAssetResolutionBase.SharedTestState>
    {
        public PortableAppRidAssetResolution(SharedTestState sharedState)
            : base(sharedState)
        {
        }

        protected override void RunTest(
            Action<NetCoreAppBuilder.RuntimeLibraryBuilder> assetsCustomizer,
            string rid,
            string includedAssemblyPaths,
            string excludedAssemblyPaths,
            string includedNativeLibraryPaths,
            string excludedNativeLibraryPaths,
            Action<NetCoreAppBuilder> appCustomizer)
        {
            using (TestApp app = NetCoreAppBuilder.PortableForNETCoreApp(SharedState.FrameworkReferenceApp)
                .WithProject(p => { p.WithAssemblyGroup(null, g => g.WithMainAssembly()); assetsCustomizer?.Invoke(p); })
                .WithCustomizer(appCustomizer)
                .Build())
            {
                SharedState.DotNetWithNetCoreApp.Exec(app.AppDll)
                    .EnableTracingAndCaptureOutputs()
                    .RuntimeId(rid)
                    .Execute()
                    .Should().Pass()
                    .And.HaveResolvedAssembly(includedAssemblyPaths, app)
                    .And.NotHaveResolvedAssembly(excludedAssemblyPaths, app)
                    .And.HaveResolvedNativeLibraryPath(includedNativeLibraryPaths, app)
                    .And.NotHaveResolvedNativeLibraryPath(excludedNativeLibraryPaths, app);
            }
        }
    }

    // Run the tests on a portable component hosted by a framework dependent app
    public class PortableComponentOnFrameworkDependentAppRidAssetResolution :
        RidAssetResolutionBase,
        IClassFixture<RidAssetResolutionBase.SharedTestState>
    {
        public PortableComponentOnFrameworkDependentAppRidAssetResolution(SharedTestState sharedState)
            : base(sharedState)
        {
        }

        protected override void RunTest(
            Action<NetCoreAppBuilder.RuntimeLibraryBuilder> assetsCustomizer,
            string rid,
            string includedAssemblyPaths,
            string excludedAssemblyPaths,
            string includedNativeLibraryPaths,
            string excludedNativeLibraryPaths,
            Action<NetCoreAppBuilder> appCustomizer)
        {
            var component = SharedState.CreateComponentWithNoDependencies(b => b
                .WithPackage("NativeDependency", "1.0.0", p => assetsCustomizer?.Invoke(p))
                .WithCustomizer(appCustomizer));

            SharedState.RunComponentResolutionTest(component, command => command
                .RuntimeId(rid))
                .Should().Pass()
                .And.HaveSuccessfullyResolvedComponentDependencies()
                .And.HaveResolvedComponentDependencyAssembly(includedAssemblyPaths, component)
                .And.NotHaveResolvedComponentDependencyAssembly(excludedAssemblyPaths, component)
                .And.HaveResolvedComponentDependencyNativeLibraryPath(includedNativeLibraryPaths, component)
                .And.NotHaveResolvedComponentDependencyNativeLibraryPath(excludedNativeLibraryPaths, component);
        }
    }

    // Run the tests on a portable component hosted by a self-contained app
    // This is testing the currently shipping scenario where SDK does not generate RID fallback graph for self-contained apps
    public class PortableComponentOnSelfContainedAppRidAssetResolution :
        RidAssetResolutionBase,
        IClassFixture<PortableComponentOnSelfContainedAppRidAssetResolution.ComponentSharedTestState>
    {
        private ComponentSharedTestState ComponentSharedState { get; }

        public PortableComponentOnSelfContainedAppRidAssetResolution(ComponentSharedTestState sharedState)
            : base(sharedState)
        {
            ComponentSharedState = sharedState;
        }

        protected override void RunTest(
            Action<NetCoreAppBuilder.RuntimeLibraryBuilder> assetsCustomizer,
            string rid,
            string includedAssemblyPaths,
            string excludedAssemblyPaths,
            string includedNativeLibraryPaths,
            string excludedNativeLibraryPaths,
            Action<NetCoreAppBuilder> appCustomizer)
        {
            var component = SharedState.CreateComponentWithNoDependencies(b => b
                .WithPackage("NativeDependency", "1.0.0", p => assetsCustomizer?.Invoke(p))
                .WithCustomizer(appCustomizer));

            string assemblyPaths = includedAssemblyPaths ?? "";
            if (excludedAssemblyPaths != null)
            {
                assemblyPaths = assemblyPaths.Length == 0 ? (";" + excludedAssemblyPaths) : excludedAssemblyPaths;
            }

            string nativeLibrarypaths = includedNativeLibraryPaths ?? "";
            if (excludedNativeLibraryPaths != null)
            {
                nativeLibrarypaths = nativeLibrarypaths.Length == 0 ? (";" + excludedNativeLibraryPaths) : excludedNativeLibraryPaths;
            }

            // Self-contained apps don't have any RID fallback graph, so currently there's no way to resolve native dependencies
            // from portable components - as we have no way of knowing how to follow RID fallback logic.
            SharedState.RunComponentResolutionTest(component.AppDll, ComponentSharedState.HostApp, ComponentSharedState.HostApp.Location, command => command
                .RuntimeId(rid))
                .Should().Pass()
                .And.HaveSuccessfullyResolvedComponentDependencies()
                .And.NotHaveResolvedComponentDependencyAssembly(assemblyPaths, component)
                .And.NotHaveResolvedComponentDependencyNativeLibraryPath(nativeLibrarypaths, component);
        }

        public class ComponentSharedTestState : SharedTestState
        {
            public TestApp HostApp { get; }

            public ComponentSharedTestState()
            {
                HostApp = CreateSelfContainedAppWithMockCoreClr("ComponentHostSelfContainedApp", "1.0.0");
            }
        }
    }

    // Run the tests on a portable component hosted by a self-contained app which does have a RID fallback graph
    // This is testing the scenario after SDK starts generating RID fallback graph even for self-contained apps 
    //   - https://github.com/dotnet/sdk/issues/3361
    public class PortableComponentOnSelfContainedAppRidAssetResolutionWithRidFallbackGraph :
        RidAssetResolutionBase,
        IClassFixture<PortableComponentOnSelfContainedAppRidAssetResolutionWithRidFallbackGraph.ComponentSharedTestState>
    {
        private ComponentSharedTestState ComponentSharedState { get; }

        public PortableComponentOnSelfContainedAppRidAssetResolutionWithRidFallbackGraph(ComponentSharedTestState sharedState)
            : base(sharedState)
        {
            ComponentSharedState = sharedState;
        }

        protected override void RunTest(
            Action<NetCoreAppBuilder.RuntimeLibraryBuilder> assetsCustomizer,
            string rid,
            string includedAssemblyPaths,
            string excludedAssemblyPaths,
            string includedNativeLibraryPaths,
            string excludedNativeLibraryPaths,
            Action<NetCoreAppBuilder> appCustomizer)
        {
            var component = SharedState.CreateComponentWithNoDependencies(b => b
                .WithPackage("NativeDependency", "1.0.0", p => assetsCustomizer?.Invoke(p))
                .WithCustomizer(appCustomizer));

            SharedState.RunComponentResolutionTest(component.AppDll, ComponentSharedState.HostApp, ComponentSharedState.HostApp.Location, command => command
                .RuntimeId(rid))
                .Should().Pass()
                .And.HaveSuccessfullyResolvedComponentDependencies()
                .And.HaveResolvedComponentDependencyAssembly(includedAssemblyPaths, component)
                .And.NotHaveResolvedComponentDependencyAssembly(excludedAssemblyPaths, component)
                .And.HaveResolvedComponentDependencyNativeLibraryPath(includedNativeLibraryPaths, component)
                .And.NotHaveResolvedComponentDependencyNativeLibraryPath(excludedNativeLibraryPaths, component);
        }

        public class ComponentSharedTestState : SharedTestState
        {
            public TestApp HostApp { get; }

            public ComponentSharedTestState()
            {
                HostApp = CreateSelfContainedAppWithMockCoreClr(
                    "ComponentHostSelfContainedApp", 
                    "1.0.0",
                    b => b.WithStandardRuntimeFallbacks());
            }
        }
    }
}

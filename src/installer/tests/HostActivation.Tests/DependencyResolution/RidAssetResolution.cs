// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Build;
using Microsoft.Extensions.DependencyModel;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public abstract class RidAssetResolutionBase : ComponentDependencyResolutionBase
    {
        protected abstract void RunTest(
            Action<NetCoreAppBuilder.RuntimeLibraryBuilder> assetsCustomizer,
            string rid,
            string includedAssemblyPaths,
            string excludedAssemblyPaths,
            string includedNativeLibraryPaths,
            string excludedNativeLibraryPaths,
            bool hasRuntimeFallbacks = true,
            Action<NetCoreAppBuilder> appCustomizer = null);

        protected const string UnknownRid = "unknown-rid";

        private const string LinuxAssembly = "linux/LinuxAssembly.dll";
        private const string MacOSAssembly = "osx/MacOSAssembly.dll";
        private const string WindowsAssembly = "win/WindowsAssembly.dll";

        private void RidSpecificAssemblyImpl(string rid, string includedPath, string excludedPath, bool hasRuntimeFallbacks)
        {
            RunTest(
                p => p
                    .WithAssemblyGroup("win", g => g.WithAsset(WindowsAssembly))
                    .WithAssemblyGroup("linux", g => g.WithAsset(LinuxAssembly))
                    .WithAssemblyGroup("osx", g => g.WithAsset(MacOSAssembly)),
                rid, includedPath, excludedPath, null, null, hasRuntimeFallbacks);
        }

        [Theory]
        [InlineData("win", WindowsAssembly, $"{LinuxAssembly};{MacOSAssembly}")]
        [InlineData("win10-x64", WindowsAssembly, $"{LinuxAssembly};{MacOSAssembly}")]
        [InlineData("linux-x64", LinuxAssembly, $"{MacOSAssembly};{WindowsAssembly}")]
        [InlineData("osx-x64", MacOSAssembly, $"{LinuxAssembly};{WindowsAssembly}")]
        public void RidSpecificAssembly(string rid, string includedPath, string excludedPath)
        {
            RidSpecificAssemblyImpl(rid, includedPath, excludedPath, hasRuntimeFallbacks: true);
        }

        [Theory]
        // RID is computed at run-time
        [InlineData(null, true)]
        [InlineData(null, false)]
        // RID is from a compile-time fallback
        [InlineData(UnknownRid, true)]
        [InlineData(UnknownRid, false)]
        public void RidSpecificAssembly_CurrentRid(string rid, bool hasRuntimeFallbacks)
        {
            // Host relies on the fallback graph to resolve any RID-specific assets that don't exactly match
            // the current RID, so everything remains excluded without the fallback graph currently
            string includedPath = null;
            string excludedPath = $"{LinuxAssembly};{MacOSAssembly};{WindowsAssembly}";
            if (hasRuntimeFallbacks)
            {
                // Host should resolve to the RID corresponding to the platform on which it is running
                if (OperatingSystem.IsLinux())
                {
                    includedPath = LinuxAssembly;
                    excludedPath = $"{MacOSAssembly};{WindowsAssembly}";
                }
                else if (OperatingSystem.IsMacOS())
                {
                    includedPath = MacOSAssembly;
                    excludedPath = $"{LinuxAssembly};{WindowsAssembly}";
                }
                else if (OperatingSystem.IsWindows())
                {
                    includedPath = WindowsAssembly;
                    excludedPath = $"{LinuxAssembly};{MacOSAssembly}";
                }
                else
                {
                    includedPath = null;
                    excludedPath = $"{LinuxAssembly};{MacOSAssembly};{WindowsAssembly}";
                }
            }

            RidSpecificAssemblyImpl(rid, includedPath, excludedPath, hasRuntimeFallbacks);
        }

        private void RidSpecificNativeLibraryImpl(string rid, string includedPath, string excludedPath, bool hasRuntimeFallbacks)
        {
            RunTest(
                p => p
                    .WithNativeLibraryGroup("win", g => g.WithAsset("win/WindowsNativeLibrary.dll"))
                    .WithNativeLibraryGroup("linux", g => g.WithAsset("linux/LinuxNativeLibrary.so"))
                    .WithNativeLibraryGroup("osx", g => g.WithAsset("osx/MacOSNativeLibrary.dylib")),
                rid, null, null, includedPath, excludedPath, hasRuntimeFallbacks);
        }

        [Theory]
        [InlineData("win", "win", "linux;osx")]
        [InlineData("win10-x64", "win", "linux;osx")]
        [InlineData("linux-x64", "linux", "osx;win")]
        [InlineData("osx-x64", "osx", "linux;win")]
        public void RidSpecificNativeLibrary(string rid, string includedPath, string excludedPath)
        {
            RidSpecificNativeLibraryImpl(rid, includedPath, excludedPath, hasRuntimeFallbacks: true);
        }

        [Theory]
        // RID is computed at run-time
        [InlineData(null, true)]
        [InlineData(null, false)]
        // RID is from a compile-time fallback
        [InlineData(UnknownRid, true)]
        [InlineData(UnknownRid, false)]
        public void RidSpecificNativeLibrary_CurrentRid(string rid, bool hasRuntimeFallbacks)
        {
            // Host relies on the fallback graph to resolve any RID-specific assets that don't exactly match
            // the current RID, so everything remains excluded without the fallback graph currently
            string includedPath = null;
            string excludedPath = "linux;osx;win";
            if (hasRuntimeFallbacks)
            {
                // Host should resolve to the RID corresponding to the platform on which it is running
                if (OperatingSystem.IsLinux())
                {
                    includedPath = "linux";
                    excludedPath = "osx;win";
                }
                else if (OperatingSystem.IsMacOS())
                {
                    includedPath = "osx";
                    excludedPath = "linux;win";
                }
                else if (OperatingSystem.IsWindows())
                {
                    includedPath = "win";
                    excludedPath = "linux;osx";
                }
            }

            RidSpecificNativeLibraryImpl(rid, includedPath, excludedPath, hasRuntimeFallbacks);
        }

        [Theory]
        [InlineData("win10-x64", "win-x64/ManagedWin64.dll")]
        [InlineData("win10-x86", "win/ManagedWin.dll")]
        [InlineData("linux-x64", "any/ManagedAny.dll")]
        public void MostSpecificRidAssemblySelected(string rid, string expectedPath, bool hasRuntimeFallbacks = true)
        {
            RunTest(
                p => p
                    .WithAssemblyGroup("any", g => g.WithAsset("any/ManagedAny.dll"))
                    .WithAssemblyGroup("win", g => g.WithAsset("win/ManagedWin.dll"))
                    .WithAssemblyGroup("win-x64", g => g.WithAsset("win-x64/ManagedWin64.dll")),
                rid, expectedPath, null, null, null);
        }


        private static string CurrentRid = RepoDirectoriesProvider.Default.BuildRID;
        private static string CurrentRidAsset = $"{CurrentRid}/{CurrentRid}Asset.dll";

        // Strip the -<arch> from the RID to get the OS
        private static string CurrentOS = CurrentRid[..^(RepoDirectoriesProvider.Default.BuildArchitecture.Length + 1)];
        private static string CurrentOSAsset = $"{CurrentOS}/{CurrentOS}Asset.dll";

        // Append a different architecture - arm64 if current architecture is x64, otherwise x64
        private static string DifferentArch = $"{CurrentOS}-{(RepoDirectoriesProvider.Default.BuildArchitecture == "x64" ? "arm64" : "x64")}";
        private static string DifferentArchAsset = $"{DifferentArch}/{DifferentArch}Asset.dll";

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void MostSpecificRidAssemblySelected_ComputedRid(bool includeCurrentArch, bool hasRuntimeFallbacks)
        {
            // Host relies on the fallback graph to resolve any RID-specific assets that don't exactly match
            // the current RID, so the OS-only asset remains excluded without the fallback graph currently
            string includedPath = null;
            string excludedPath = $"{CurrentOSAsset};{DifferentArchAsset}";
            if (hasRuntimeFallbacks)
            {
                if (includeCurrentArch)
                {
                    includedPath = CurrentRidAsset;
                }
                else
                {
                    includedPath = CurrentOSAsset;
                    excludedPath = DifferentArchAsset;
                }
            }

            RunTest(p =>
                {
                    p.WithAssemblyGroup(CurrentOS, g => g.WithAsset(CurrentOSAsset));
                    if (includeCurrentArch)
                    {
                        p.WithAssemblyGroup(CurrentRid, g => g.WithAsset(CurrentRidAsset));
                    }
                },
                rid: null, // RID is computed at run-time
                includedPath, excludedPath, null, null, hasRuntimeFallbacks);
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
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void MostSpecificRidNativeLibrarySelected_ComputedRid(bool includeCurrentArch, bool hasRuntimeFallbacks)
        {
            // Host relies on the fallback graph to resolve any RID-specific assets that don't exactly match
            // the current RID, so the OS-only asset remains excluded without the fallback graph currently
            string includedPath = null;
            string excludedPath = $"{CurrentOS}/;{DifferentArch}/";
            if (hasRuntimeFallbacks)
            {
                if (includeCurrentArch)
                {
                    includedPath = $"{CurrentRid}/";
                }
                else
                {
                    includedPath = $"{CurrentOS}/";
                    excludedPath = $"{DifferentArch}/";
                }
            }

            RunTest(p =>
                {
                    p.WithNativeLibraryGroup(CurrentOS, g => g.WithAsset(CurrentOSAsset));
                    if (includeCurrentArch)
                    {
                        p.WithNativeLibraryGroup(CurrentRid, g => g.WithAsset(CurrentRidAsset));
                    }
                },
                rid: null, // RID is computed at run-time
                null, null, includedPath, excludedPath, hasRuntimeFallbacks);
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

        protected static void UseFallbacksFromBuiltDotNet(NetCoreAppBuilder builder)
        {
            IReadOnlyList<RuntimeFallbacks> fallbacks;
            string depsJson = Path.Combine(new DotNetCli(RepoDirectoriesProvider.Default.BuiltDotnet).GreatestVersionSharedFxPath, $"{Constants.MicrosoftNETCoreApp}.deps.json");
            using (FileStream fileStream = File.Open(depsJson, FileMode.Open))
            using (DependencyContextJsonReader reader = new DependencyContextJsonReader())
            {
                fallbacks = reader.Read(fileStream).RuntimeGraph;
            }

            builder.RuntimeFallbacks.Clear();
            foreach (RuntimeFallbacks fallback in fallbacks)
            {
                builder.WithRuntimeFallbacks(fallback.Runtime, fallback.Fallbacks.ToArray());
            }
        }

        public class AppSharedTestState : ComponentSharedTestStateBase
        {
            public DotNetCli DotNetWithNetCoreApp_RuntimeFallbacks { get; }
            public DotNetCli DotNetWithNetCoreApp_NoRuntimeFallbacks { get; }

            public AppSharedTestState() : base()
            {
                DotNetWithNetCoreApp_RuntimeFallbacks = DotNet("WithNetCoreApp_RuntimeFallbacks")
                    .AddMicrosoftNETCoreAppFrameworkMockCoreClr("4.0.0", UseFallbacksFromBuiltDotNet)
                    .Build();

                DotNetWithNetCoreApp_NoRuntimeFallbacks = DotNet("WithNetCoreApp_NoRuntimeFallbacks")
                    .AddMicrosoftNETCoreAppFrameworkMockCoreClr("4.0.0", b => b.RuntimeFallbacks.Clear())
                    .Build();
            }
        }
    }

    // Run the tests on a framework dependent app
    public class PortableAppRidAssetResolution :
        RidAssetResolutionBase,
        IClassFixture<RidAssetResolutionBase.AppSharedTestState>
    {
        private AppSharedTestState SharedState { get; }

        public PortableAppRidAssetResolution(AppSharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        protected override void RunTest(
            Action<NetCoreAppBuilder.RuntimeLibraryBuilder> assetsCustomizer,
            string rid,
            string includedAssemblyPaths,
            string excludedAssemblyPaths,
            string includedNativeLibraryPaths,
            string excludedNativeLibraryPaths,
            bool hasRuntimeFallbacks,
            Action<NetCoreAppBuilder> appCustomizer)
        {
            using (TestApp app = NetCoreAppBuilder.PortableForNETCoreApp(SharedState.FrameworkReferenceApp)
                .WithProject(p => { p.WithAssemblyGroup(null, g => g.WithMainAssembly()); assetsCustomizer?.Invoke(p); })
                .WithCustomizer(appCustomizer)
                .Build())
            {
                DotNetCli dotnet;
                if (hasRuntimeFallbacks)
                {
                    // Use the fallbacks from the product when testing the computed RID
                    dotnet = rid == null ? SharedState.DotNetWithNetCoreApp_RuntimeFallbacks : SharedState.DotNetWithNetCoreApp;
                }
                else
                {
                    dotnet = SharedState.DotNetWithNetCoreApp_NoRuntimeFallbacks;
                }

                dotnet.Exec(app.AppDll)
                    .EnableTracingAndCaptureOutputs()
                    .RuntimeId(rid)
                    .Execute()
                    .Should().Pass()
                    .And.HaveResolvedAssembly(includedAssemblyPaths, app)
                    .And.NotHaveResolvedAssembly(excludedAssemblyPaths, app)
                    .And.HaveResolvedNativeLibraryPath(includedNativeLibraryPaths, app)
                    .And.NotHaveResolvedNativeLibraryPath(excludedNativeLibraryPaths, app)
                    .And.HaveUsedFallbackRid(rid == UnknownRid || !hasRuntimeFallbacks)
                    .And.HaveUsedFrameworkProbe(dotnet.GreatestVersionSharedFxPath, level: 1);
            }
        }
    }

    // Run the tests on a portable component hosted by a framework dependent app
    public class PortableComponentOnFrameworkDependentAppRidAssetResolution :
        RidAssetResolutionBase,
        IClassFixture<RidAssetResolutionBase.AppSharedTestState>
    {
        private AppSharedTestState SharedState { get; }

        public PortableComponentOnFrameworkDependentAppRidAssetResolution(AppSharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        protected override void RunTest(
            Action<NetCoreAppBuilder.RuntimeLibraryBuilder> assetsCustomizer,
            string rid,
            string includedAssemblyPaths,
            string excludedAssemblyPaths,
            string includedNativeLibraryPaths,
            string excludedNativeLibraryPaths,
            bool hasRuntimeFallbacks,
            Action<NetCoreAppBuilder> appCustomizer)
        {
            var component = SharedState.CreateComponentWithNoDependencies(b => b
                .WithPackage("NativeDependency", "1.0.0", p => assetsCustomizer?.Invoke(p))
                .WithCustomizer(appCustomizer));

            DotNetCli dotnet;
            if (hasRuntimeFallbacks)
            {
                // Use the fallbacks from the product when testing the computed RID
                dotnet = rid == null ? SharedState.DotNetWithNetCoreApp_RuntimeFallbacks : SharedState.DotNetWithNetCoreApp;
            }
            else
            {
                dotnet = SharedState.DotNetWithNetCoreApp_NoRuntimeFallbacks;
            }

            SharedState.RunComponentResolutionTest(component.AppDll, SharedState.FrameworkReferenceApp, dotnet.GreatestVersionHostFxrPath, command => command
                .RuntimeId(rid))
                .Should().Pass()
                .And.HaveSuccessfullyResolvedComponentDependencies()
                .And.HaveResolvedComponentDependencyAssembly(includedAssemblyPaths, component)
                .And.NotHaveResolvedComponentDependencyAssembly(excludedAssemblyPaths, component)
                .And.HaveResolvedComponentDependencyNativeLibraryPath(includedNativeLibraryPaths, component)
                .And.NotHaveResolvedComponentDependencyNativeLibraryPath(excludedNativeLibraryPaths, component)
                .And.HaveUsedFallbackRid(rid == UnknownRid || !hasRuntimeFallbacks)
                .And.NotHaveUsedFrameworkProbe(dotnet.GreatestVersionSharedFxPath);
        }
    }

    // Run the tests on a portable component hosted by a self-contained app
    public class PortableComponentOnSelfContainedAppRidAssetResolution :
        RidAssetResolutionBase,
        IClassFixture<PortableComponentOnSelfContainedAppRidAssetResolution.ComponentSharedTestState>
    {
        private ComponentSharedTestState SharedState { get; }

        public PortableComponentOnSelfContainedAppRidAssetResolution(ComponentSharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        protected override void RunTest(
            Action<NetCoreAppBuilder.RuntimeLibraryBuilder> assetsCustomizer,
            string rid,
            string includedAssemblyPaths,
            string excludedAssemblyPaths,
            string includedNativeLibraryPaths,
            string excludedNativeLibraryPaths,
            bool hasRuntimeFallbacks,
            Action<NetCoreAppBuilder> appCustomizer)
        {
            var component = SharedState.CreateComponentWithNoDependencies(b => b
                .WithPackage("NativeDependency", "1.0.0", p => assetsCustomizer?.Invoke(p))
                .WithCustomizer(appCustomizer));

            TestApp app;
            if (hasRuntimeFallbacks)
            {
                // Use the fallbacks from the product when testing the computed RID
                app = rid == null ? SharedState.HostApp_RuntimeFallbacks : SharedState.HostApp;
            }
            else
            {
                app = SharedState.HostApp_NoRuntimeFallbacks;
            }

            SharedState.RunComponentResolutionTest(component.AppDll, app, app.Location, command => command
                .RuntimeId(rid))
                .Should().Pass()
                .And.HaveSuccessfullyResolvedComponentDependencies()
                .And.HaveResolvedComponentDependencyAssembly(includedAssemblyPaths, component)
                .And.NotHaveResolvedComponentDependencyAssembly(excludedAssemblyPaths, component)
                .And.HaveResolvedComponentDependencyNativeLibraryPath(includedNativeLibraryPaths, component)
                .And.NotHaveResolvedComponentDependencyNativeLibraryPath(excludedNativeLibraryPaths, component)
                .And.HaveUsedFallbackRid(rid == UnknownRid || !hasRuntimeFallbacks);
        }

        public class ComponentSharedTestState : ComponentSharedTestStateBase
        {
            public TestApp HostApp { get; }
            public TestApp HostApp_RuntimeFallbacks { get; }
            public TestApp HostApp_NoRuntimeFallbacks { get; }

            public ComponentSharedTestState()
            {
                HostApp = CreateSelfContainedAppWithMockCoreClr(
                    "ComponentHostSelfContainedApp",
                    b => b.WithStandardRuntimeFallbacks());

                HostApp_RuntimeFallbacks = CreateSelfContainedAppWithMockCoreClr(
                    "ComponentHostSelfContainedApp_RuntimeFallbacks",
                    UseFallbacksFromBuiltDotNet);

                HostApp_NoRuntimeFallbacks = CreateSelfContainedAppWithMockCoreClr(
                    "ComponentHostSelfContainedApp_NoRuntimeFallbacks",
                    b => b.RuntimeFallbacks.Clear());
            }
        }
    }
}

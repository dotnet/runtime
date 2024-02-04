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
        private static Version UseRidGraphDisabledVersion = new Version(8, 0);
        public class TestSetup
        {
            // Explicit RID (environment variable) to set when running the test
            // Value of null indicates unset (default)
            public string? Rid { get; init; }

            // Represents the configuration (System.Runtime.Loader.UseRidGraph) for whether to read the RID graph
            // Value of null indicates unset (default setting)
            public bool? UseRidGraph { get; init; }

            // Whether or not the root deps file has a RID graph
            public bool HasRidGraph { get; init; }

            // Expected behaviour of the test based on above settings
            public bool ShouldUseRidGraph => UseRidGraph == true;

            public bool? ShouldUseFallbackRid
            {
                get
                {
                    if (!ShouldUseRidGraph)
                        return false;

                    if (Rid == UnknownRid || !HasRidGraph)
                        return true;

                    // We use the product RID graph for testing (for cases with a RID graph). If the test is running
                    // on a platform that isn't in that RID graph, we may end up with the fallback even when the RID
                    // graph is used and RID is not unknown. Value of null indicates this state.
                    return null;
                }
            }

            public override string ToString() => $"""
                {nameof(Rid)}: {(Rid ?? "<null>")}
                {nameof(UseRidGraph)}: {(UseRidGraph.HasValue ? UseRidGraph : "<null>")}
                {nameof(HasRidGraph)}: {HasRidGraph}
                [computed]
                  {nameof(ShouldUseRidGraph)}: {ShouldUseRidGraph}
                  {nameof(ShouldUseFallbackRid)}: {ShouldUseFallbackRid}
                """;
        };

        public class ResolvedPaths
        {
            public string IncludedAssemblyPaths { get; init; }
            public string ExcludedAssemblyPaths { get; init; }
            public string IncludedNativeLibraryPaths { get; init; }
            public string ExcludedNativeLibraryPaths { get; init; }
        }

        protected abstract void RunTest(
            Action<NetCoreAppBuilder.RuntimeLibraryBuilder> assetsCustomizer,
            TestSetup setup,
            ResolvedPaths expected,
            Action<NetCoreAppBuilder> appCustomizer = null);

        protected TestApp UpdateAppConfigForTest(TestApp app, TestSetup setup, bool copyOnUpdate)
        {
            if (!setup.UseRidGraph.HasValue)
                return app;

            if (copyOnUpdate)
                app = app.Copy();

            RuntimeConfig config = RuntimeConfig.FromFile(app.RuntimeConfigJson);

            if (setup.UseRidGraph.HasValue)
                config.WithProperty("System.Runtime.Loader.UseRidGraph", setup.UseRidGraph.ToString());

            config.Save();

            return app;
        }

        // The fallback RID is a compile-time define for the host. On Windows, it is always win10 and on
        // other platforms, it matches the build RID (non-portable for source-builds, portable otherwise)
        private static string FallbackRid = OperatingSystem.IsWindows()
            ? $"win10-{TestContext.BuildArchitecture}"
            : TestContext.BuildRID;

        protected const string UnknownRid = "unknown-rid";

        private const string LinuxAssembly = "linux/LinuxAssembly.dll";
        private const string MacOSAssembly = "osx/MacOSAssembly.dll";
        private const string WindowsAssembly = "win/WindowsAssembly.dll";

        private static (string included, string excluded) GetExpectedPathsforCurrentRid(string linux, string macos, string windows)
        {
            if (OperatingSystem.IsLinux())
                return (linux, $"{macos};{windows}");

            if (OperatingSystem.IsMacOS())
                return (macos, $"{linux};{windows}");

            if (OperatingSystem.IsWindows())
                return (windows, $"{linux};{macos}");

            return (null, $"{linux};{macos};{windows}");
        }

        private void RidSpecificAssemblyImpl(TestSetup setup, string includedPath, string excludedPath)
        {
            RunTest(
                p => p
                    .WithAssemblyGroup("win", g => g.WithAsset(WindowsAssembly))
                    .WithAssemblyGroup("linux", g => g.WithAsset(LinuxAssembly))
                    .WithAssemblyGroup("osx", g => g.WithAsset(MacOSAssembly)),
                setup,
                new ResolvedPaths() { IncludedAssemblyPaths = includedPath, ExcludedAssemblyPaths = excludedPath });
        }

        [Theory]
        [InlineData("win", WindowsAssembly, $"{LinuxAssembly};{MacOSAssembly}")]
        [InlineData("win10-x64", WindowsAssembly, $"{LinuxAssembly};{MacOSAssembly}")]
        [InlineData("linux-x64", LinuxAssembly, $"{MacOSAssembly};{WindowsAssembly}")]
        [InlineData("osx-x64", MacOSAssembly, $"{LinuxAssembly};{WindowsAssembly}")]
        public void RidSpecificAssembly_RidGraph(string rid, string includedPath, string excludedPath)
        {
            RidSpecificAssemblyImpl(
                new TestSetup() { Rid = rid, HasRidGraph = true, UseRidGraph = true },
                includedPath,
                excludedPath);
        }

        [Theory]
        // RID is computed at run-time
        [InlineData(null, true, true)]
        [InlineData(null, true, false)]
        [InlineData(null, true, null)]
        [InlineData(null, false, true)]
        [InlineData(null, false, false)]
        [InlineData(null, false, null)]
        // RID is from a compile-time fallback when using the RID graph
        [InlineData(UnknownRid, true, true)]
        [InlineData(UnknownRid, true, false)]
        [InlineData(UnknownRid, true, null)]
        [InlineData(UnknownRid, false, true)]
        [InlineData(UnknownRid, false, false)]
        [InlineData(UnknownRid, false, null)]
        public void RidSpecificAssembly_CurrentRid(string rid, bool hasRuntimeFallbacks, bool? useRidGraph)
        {
            // When not using the RID graph, the host uses the target OS for which it was built to determine applicable
            // RIDs that apply, so it can find both the arch-specific and the OS-only assets.
            // When using the RID graph, the host uses it to resolve any RID-specific assets that don't exactly match
            // the current RID, so the OS-only asset remains excluded if there are no fallbacks.
            string includedPath = null;
            string excludedPath = $"{LinuxAssembly};{MacOSAssembly};{WindowsAssembly}";
            if (useRidGraph != true || hasRuntimeFallbacks)
            {
                // Host should resolve to the RID corresponding to the platform on which it is running
                (includedPath, excludedPath) = GetExpectedPathsforCurrentRid(LinuxAssembly, MacOSAssembly, WindowsAssembly);
            }

            RidSpecificAssemblyImpl(
                new TestSetup() { Rid = rid, HasRidGraph = hasRuntimeFallbacks, UseRidGraph = useRidGraph },
                includedPath,
                excludedPath);
        }

        [Fact]
        public void RidSpecificAssembly_FallbackRid()
        {
            // When there is no RID graph and the host is configured to use the RID graph, it should still be able to
            // resolve an exact match to the fallback RID
            string assetPath = $"{FallbackRid}/{FallbackRid}Asset";
            RunTest(
                p => p.WithAssemblyGroup(FallbackRid, g => g.WithAsset(assetPath)),
                new TestSetup() { Rid = null, HasRidGraph = false, UseRidGraph = true },
                new ResolvedPaths() { IncludedAssemblyPaths = assetPath });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void RidSpecificAssembly_UnknownRid(bool? useRidGraph)
        {
            string assetPath = $"{UnknownRid}/{UnknownRid}Asset";

            string includedPath;
            string excludedPath;
            if (useRidGraph == true)
            {
                // Host won't resolve the unknown RID asset
                includedPath = null;
                excludedPath = assetPath;
            }
            else
            {
                // Host should resolve to the specified RID
                includedPath = assetPath;
                excludedPath = null;
            }

            RunTest(
                p => p.WithAssemblyGroup(UnknownRid, g => g.WithAsset(assetPath)),
                new TestSetup() { Rid = UnknownRid, UseRidGraph = useRidGraph },
                new ResolvedPaths() { IncludedAssemblyPaths = includedPath, ExcludedAssemblyPaths = excludedPath });
        }

        private void RidSpecificNativeLibraryImpl(TestSetup setup, string includedPath, string excludedPath)
        {
            RunTest(
                p => p
                    .WithNativeLibraryGroup("win", g => g.WithAsset("win/WindowsNativeLibrary.dll"))
                    .WithNativeLibraryGroup("linux", g => g.WithAsset("linux/LinuxNativeLibrary.so"))
                    .WithNativeLibraryGroup("osx", g => g.WithAsset("osx/MacOSNativeLibrary.dylib")),
                setup,
                new ResolvedPaths() { IncludedNativeLibraryPaths = includedPath, ExcludedNativeLibraryPaths = excludedPath });
        }

        [Theory]
        [InlineData("win", "win", "linux;osx")]
        [InlineData("win10-x64", "win", "linux;osx")]
        [InlineData("linux-x64", "linux", "osx;win")]
        [InlineData("osx-x64", "osx", "linux;win")]
        public void RidSpecificNativeLibrary_RidGraph(string rid, string includedPath, string excludedPath)
        {
            RidSpecificNativeLibraryImpl(
                new TestSetup() { Rid = rid, HasRidGraph = true, UseRidGraph = true },
                includedPath, excludedPath);
        }

        [Theory]
        // RID is computed at run-time
        [InlineData(null, true, true)]
        [InlineData(null, true, false)]
        [InlineData(null, true, null)]
        [InlineData(null, false, true)]
        [InlineData(null, false, false)]
        [InlineData(null, false, null)]
        // RID is from a compile-time fallback when using the RID graph
        [InlineData(UnknownRid, true, true)]
        [InlineData(UnknownRid, true, false)]
        [InlineData(UnknownRid, true, null)]
        [InlineData(UnknownRid, false, true)]
        [InlineData(UnknownRid, false, false)]
        [InlineData(UnknownRid, false, null)]
        public void RidSpecificNativeLibrary_CurrentRid(string rid, bool hasRuntimeFallbacks, bool? useRidGraph)
        {
            // When not using the RID graph, the host uses the target OS for which it was built to determine applicable
            // RIDs that apply, so it can find both the arch-specific and the OS-only assets.
            // When using the RID graph, the host uses it to resolve any RID-specific assets that don't exactly match
            // the current RID, so the OS-only asset remains excluded if there are no fallbacks.
            string includedPath = null;
            string excludedPath = "linux;osx;win";
            if (useRidGraph != true || hasRuntimeFallbacks)
            {
                // Host should resolve to the RID corresponding to the platform on which it is running
                (includedPath, excludedPath) = GetExpectedPathsforCurrentRid("linux/", "osx/", "win/");
            }

            RidSpecificNativeLibraryImpl(
                new TestSetup() { Rid = rid, HasRidGraph = hasRuntimeFallbacks, UseRidGraph = useRidGraph },
                includedPath,
                excludedPath);
        }

        [Fact]
        public void RidSpecificNativeLibrary_FallbackRid()
        {
            // When there is no RID graph and the host is configured to use the RID graph, it should still be able to
            // resolve an exact match to the fallback RID
            string assetPath = $"{FallbackRid}/{FallbackRid}Asset";
            RunTest(
                p => p.WithNativeLibraryGroup(FallbackRid, g => g.WithAsset(assetPath)),
                new TestSetup() { Rid = null, HasRidGraph = false, UseRidGraph = true },
                new ResolvedPaths() { IncludedNativeLibraryPaths = $"{FallbackRid}/" });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void RidSpecificNativeLibrary_UnknownRid(bool? useRidGraph)
        {
            string assetPath = $"{UnknownRid}/{UnknownRid}Asset";

            string includedPath;
            string excludedPath;
            if (useRidGraph == true)
            {
                // Host won't resolve the unknown RID asset
                includedPath = null;
                excludedPath = $"{UnknownRid}/";
            }
            else
            {
                // Host should resolve to the specified RID
                includedPath = $"{UnknownRid}/";
                excludedPath = null;
            }

            RunTest(
                p => p.WithNativeLibraryGroup(UnknownRid, g => g.WithAsset(assetPath)),
                new TestSetup() { Rid = UnknownRid, UseRidGraph = useRidGraph },
                new ResolvedPaths() { IncludedNativeLibraryPaths = includedPath, ExcludedNativeLibraryPaths = excludedPath });
        }

        [Theory]
        [InlineData("win10-x64", "win-x64/ManagedWin64.dll")]
        [InlineData("win10-x86", "win/ManagedWin.dll")]
        [InlineData("linux-x64", "any/ManagedAny.dll")]
        public void MostSpecificRidAssemblySelected_RidGraph(string rid, string expectedPath)
        {
            RunTest(
                p => p
                    .WithAssemblyGroup("any", g => g.WithAsset("any/ManagedAny.dll"))
                    .WithAssemblyGroup("win", g => g.WithAsset("win/ManagedWin.dll"))
                    .WithAssemblyGroup("win-x64", g => g.WithAsset("win-x64/ManagedWin64.dll")),
                new TestSetup() { Rid = rid, HasRidGraph = true, UseRidGraph = true },
                new ResolvedPaths() { IncludedAssemblyPaths = expectedPath });
        }

        // The build RID from the test context should match the build RID of the host under test
        private static string CurrentRid = TestContext.BuildRID;
        private static string CurrentRidAsset = $"{CurrentRid}/{CurrentRid}Asset.dll";

        // Strip the -<arch> from the RID to get the OS
        private static string CurrentOS = CurrentRid[..^(TestContext.BuildArchitecture.Length + 1)];
        private static string CurrentOSAsset = $"{CurrentOS}/{CurrentOS}Asset.dll";

        // Append a different architecture - arm64 if current architecture is x64, otherwise x64
        private static string DifferentArch = $"{CurrentOS}-{(TestContext.BuildArchitecture == "x64" ? "arm64" : "x64")}";
        private static string DifferentArchAsset = $"{DifferentArch}/{DifferentArch}Asset.dll";

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, true, null)]
        [InlineData(true, false, true)]
        [InlineData(true, false, false)]
        [InlineData(true, false, null)]
        [InlineData(false, true, true)]
        [InlineData(false, true, false)]
        [InlineData(false, true, null)]
        [InlineData(false, false, true)]
        [InlineData(false, false, false)]
        [InlineData(false, false, null)]
        public void MostSpecificRidAssemblySelected(bool includeCurrentArch, bool hasRuntimeFallbacks, bool? useRidGraph)
        {
            // When not using the RID graph, the host uses the target OS for which it was built to determine applicable
            // RIDs that apply, so it can find both the arch-specific and the OS-only assets.
            // When using the RID graph, the host uses it to resolve any RID-specific assets that don't exactly match
            // the current RID, so the OS-only asset remains excluded if there are no fallbacks.
            string includedPath = null;
            string excludedPath = $"{CurrentOSAsset};{DifferentArchAsset}";
            if (useRidGraph != true || hasRuntimeFallbacks)
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
                    p.WithAssemblyGroup(DifferentArch, g => g.WithAsset(DifferentArchAsset));
                    if (includeCurrentArch)
                    {
                        p.WithAssemblyGroup(CurrentRid, g => g.WithAsset(CurrentRidAsset));
                    }
                },
                // RID is computed at run-time
                new TestSetup() { Rid = null, HasRidGraph = hasRuntimeFallbacks, UseRidGraph = useRidGraph },
                new ResolvedPaths() { IncludedAssemblyPaths = includedPath, ExcludedAssemblyPaths = excludedPath });
        }

        [Theory]
        [InlineData("win10-x64", "win-x64")]
        [InlineData("win10-x86", "win")]
        [InlineData("linux-x64", "any")]
        public void MostSpecificRidNativeLibrarySelected_RidGraph(string rid, string expectedPath)
        {
            RunTest(
                p => p
                    .WithNativeLibraryGroup("any", g => g.WithAsset("any/NativeAny.dll"))
                    .WithNativeLibraryGroup("win", g => g.WithAsset("win/NativeWin.dll"))
                    .WithNativeLibraryGroup("win-x64", g => g.WithAsset("win-x64/NativeWin64.dll")),
                new TestSetup() { Rid = rid, HasRidGraph = true, UseRidGraph = true },
                new ResolvedPaths() { IncludedNativeLibraryPaths = expectedPath });
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, true, null)]
        [InlineData(true, false, true)]
        [InlineData(true, false, false)]
        [InlineData(true, false, null)]
        [InlineData(false, true, true)]
        [InlineData(false, true, false)]
        [InlineData(false, true, null)]
        [InlineData(false, false, true)]
        [InlineData(false, false, false)]
        [InlineData(false, false, null)]
        public void MostSpecificRidNativeLibrarySelected(bool includeCurrentArch, bool hasRuntimeFallbacks, bool? useRidGraph)
        {
            // When not using the RID graph, the host uses the target OS for which it was built to determine applicable
            // RIDs that apply, so it can find both the arch-specific and the OS-only assets.
            // When using the RID graph, the host uses it to resolve any RID-specific assets that don't exactly match
            // the current RID, so the OS-only asset remains excluded if there are no fallbacks.
            string includedPath = null;
            string excludedPath = $"{CurrentOS}/;{DifferentArch}/";
            if (useRidGraph != true || hasRuntimeFallbacks)
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
                    p.WithNativeLibraryGroup(DifferentArch, g => g.WithAsset(DifferentArchAsset));
                    if (includeCurrentArch)
                    {
                        p.WithNativeLibraryGroup(CurrentRid, g => g.WithAsset(CurrentRidAsset));
                    }
                },
                // RID is computed at run-time
                new TestSetup() { Rid = null, HasRidGraph = hasRuntimeFallbacks, UseRidGraph = useRidGraph },
                new ResolvedPaths() { IncludedNativeLibraryPaths = includedPath, ExcludedNativeLibraryPaths = excludedPath });
        }

        [Theory]
        [InlineData("win10-x64", "win/ManagedWin.dll", "native/win-x64")]
        [InlineData("win10-x86", "win/ManagedWin.dll", "native/win-x86")]
        [InlineData("linux-x64", "any/ManagedAny.dll", "native/linux")]
        public void MostSpecificRidAssemblySelectedPerType_RidGraph(string rid, string expectedAssemblyPath, string expectedNativePath)
        {
            RunTest(
                p => p
                    .WithAssemblyGroup("any", g => g.WithAsset("any/ManagedAny.dll"))
                    .WithAssemblyGroup("win", g => g.WithAsset("win/ManagedWin.dll"))
                    .WithNativeLibraryGroup("win-x64", g => g.WithAsset("native/win-x64/n.dll"))
                    .WithNativeLibraryGroup("win-x86", g => g.WithAsset("native/win-x86/n.dll"))
                    .WithNativeLibraryGroup("linux", g => g.WithAsset("native/linux/n.so")),
                new TestSetup() { Rid = rid, HasRidGraph = true, UseRidGraph = true },
                new ResolvedPaths() { IncludedAssemblyPaths = expectedAssemblyPath, IncludedNativeLibraryPaths = expectedNativePath });
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(true, null)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        [InlineData(false, null)]
        public void MostSpecificRidAssemblySelectedPerType(bool hasRuntimeFallbacks, bool? useRidGraph)
        {
            // When not using the RID graph, the host uses the target OS for which it was built to determine applicable
            // RIDs that apply, so it can find both the arch-specific and the OS-only assets.
            // When using the RID graph, the host uses it to resolve any RID-specific assets that don't exactly match
            // the current RID, so the OS-only asset remains excluded if there are no fallbacks.
            string includedAssemblyPath = null;
            string excludedAssemblyPath = CurrentOSAsset;
            string includedNativePath = null;
            string excludedNativePath = $"native/{CurrentOS}/;native/{CurrentRidAsset}/";
            if (useRidGraph != true || hasRuntimeFallbacks)
            {
                includedAssemblyPath = CurrentOSAsset;
                excludedAssemblyPath = null;
                includedNativePath = $"native/{CurrentRid}/";
                excludedNativePath = $"native/{CurrentOS}/";
            }

            RunTest(
                p => p
                    .WithAssemblyGroup(CurrentOS, g => g.WithAsset(CurrentOSAsset))
                    .WithNativeLibraryGroup(CurrentOS, g => g.WithAsset($"native/{CurrentOSAsset}"))
                    .WithNativeLibraryGroup(CurrentRid, g => g.WithAsset($"native/{CurrentRidAsset}")),
                // RID is computed at run-time
                new TestSetup() { Rid = null, HasRidGraph = hasRuntimeFallbacks, UseRidGraph = useRidGraph },
                new ResolvedPaths()
                {
                    IncludedAssemblyPaths = includedAssemblyPath, ExcludedAssemblyPaths = excludedAssemblyPath,
                    IncludedNativeLibraryPaths = includedNativePath, ExcludedNativeLibraryPaths = excludedNativePath
                });
        }

        [Theory]
        // For "win" RIDs the DependencyLib which is RID-agnostic will not be included,
        // since there are other assembly (runtime) assets with more specific RID match.
        [InlineData("win10-x64", "win/ManagedWin.dll;win/AnotherWin.dll", "native/win10-x64;native/win10-x64-2")]
        [InlineData("win10-x86", "win/ManagedWin.dll;win/AnotherWin.dll", "native/win-x86")]
        // For "linux" on the other hand the DependencyLib will be resolved because there are
        // no RID-specific assembly assets available.
        [InlineData("linux-x64", "DependencyLib.dll", "native/linux")]
        public void MostSpecificRidAssemblySelectedPerTypeMultipleAssets_RidGraph(string rid, string expectedAssemblyPath, string expectedNativePath)
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
                setup: new TestSetup() { Rid = rid, HasRidGraph = true, UseRidGraph = true },
                // PortableLib and PortableLib2 are from a separate package which has no RID specific assets,
                // so the RID-agnostic assets are always included
                expected: new ResolvedPaths()
                {
                    IncludedAssemblyPaths = expectedAssemblyPath + ";PortableLib.dll;PortableLib2.dll",
                    IncludedNativeLibraryPaths = expectedNativePath
                });
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(true, null)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        [InlineData(false, null)]
        public void MostSpecificRidAssemblySelectedPerTypeMultipleAssets(bool hasRuntimeFallbacks, bool? useRidGraph)
        {
            // When not using the RID graph, the host uses the target OS for which it was built to determine applicable
            // RIDs that apply, so it can find both the arch-specific and the OS-only assets.
            // When using the RID graph, the host uses it to resolve any RID-specific assets that don't exactly match
            // the current RID, so the OS-only asset remains excluded if there are no fallbacks.
            string suffix = ".2";
            string includedAssemblyPath = "ridSpecificLib.dll";
            string excludedAssemblyPath = $"{CurrentOSAsset};{CurrentOS}/{CurrentOS}Asset{suffix}.dll";
            string includedNativePath = null;
            if (useRidGraph != true || hasRuntimeFallbacks)
            {
                includedAssemblyPath = $"{CurrentOSAsset};{CurrentOS}/{CurrentOS}Asset{suffix}.dll";
                excludedAssemblyPath = "ridSpecificLib.dll";
                includedNativePath = $"native/{CurrentRid}/;native/{CurrentRid}{suffix}/;native{suffix}/{CurrentRid}/";
            }

            RunTest(
                assetsCustomizer: null,
                appCustomizer: b => b
                    .WithPackage("ridSpecificLib", "1.0.0", p => p
                        .WithAssemblyGroup(null, g => g.WithAsset("ridSpecificLib.dll"))
                        .WithAssemblyGroup(CurrentOS, g => g.WithAsset(CurrentOSAsset))
                        .WithAssemblyGroup(CurrentOS, g => g.WithAsset($"{CurrentOS}/{CurrentOS}Asset{suffix}.dll"))
                        .WithNativeLibraryGroup(CurrentOS, g => g.WithAsset($"native/{CurrentOSAsset}"))
                        .WithNativeLibraryGroup(CurrentRid, g => g.WithAsset($"native/{CurrentRidAsset}"))
                        .WithNativeLibraryGroup(CurrentRid, g => g.WithAsset($"native/{CurrentRid}{suffix}/Asset"))
                        .WithNativeLibraryGroup(CurrentRid, g => g.WithAsset($"native{suffix}/{CurrentRidAsset}")))
                    .WithPackage("noRidMatch", "1.0.0", p => p
                        .WithAssemblyGroup(null, g => g.WithAsset("noRidMatch.dll"))
                        .WithAssemblyGroup(DifferentArch, g => g.WithAsset(DifferentArchAsset))
                        .WithNativeLibraryGroup(null, g => g.WithAsset($"noRidMatch"))
                        .WithNativeLibraryGroup(DifferentArch, g => g.WithAsset($"native/{DifferentArchAsset}")))
                    .WithPackage("ridAgnosticLib", "1.0.0", p => p
                        .WithAssemblyGroup(null, g => g.WithAsset("PortableLib.dll").WithAsset("PortableLib2.dll"))),
                setup: new TestSetup() { Rid = null, HasRidGraph = hasRuntimeFallbacks, UseRidGraph = useRidGraph },
                // PortableLib and PortableLib2 are from a separate package which has no RID specific assets,
                // so the RID-agnostic assets are always included. noRidMatch is from a package where none of
                // RID-specific assets match, so the RID-agnostic asset is included.
                expected: new ResolvedPaths()
                {
                    IncludedAssemblyPaths = $"{includedAssemblyPath};noRidMatch.dll;PortableLib.dll;PortableLib2.dll",
                    ExcludedAssemblyPaths = $"{excludedAssemblyPath};{DifferentArchAsset}",
                    IncludedNativeLibraryPaths = $"{includedNativePath};/",
                    ExcludedNativeLibraryPaths = $"native/{CurrentOS}/;native/{DifferentArch}/"
                });
        }

        protected static void UseFallbacksFromBuiltDotNet(NetCoreAppBuilder builder)
        {
            IReadOnlyList<RuntimeFallbacks> fallbacks;
            string depsJson = Path.Combine(TestContext.BuiltDotNet.GreatestVersionSharedFxPath, $"{Constants.MicrosoftNETCoreApp}.deps.json");
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
            TestSetup setup,
            ResolvedPaths expected,
            Action<NetCoreAppBuilder> appCustomizer)
        {
            using (TestApp app = NetCoreAppBuilder.PortableForNETCoreApp(SharedState.FrameworkReferenceApp)
                .WithProject(p => { p.WithAssemblyGroup(null, g => g.WithMainAssembly()); assetsCustomizer?.Invoke(p); })
                .WithCustomizer(appCustomizer)
                .Build())
            {
                DotNetCli dotnet;
                if (setup.HasRidGraph)
                {
                    // Use the fallbacks from the product when testing the computed RID
                    dotnet = setup.Rid == null ? SharedState.DotNetWithNetCoreApp_RuntimeFallbacks : SharedState.DotNetWithNetCoreApp;
                }
                else
                {
                    dotnet = SharedState.DotNetWithNetCoreApp_NoRuntimeFallbacks;
                }

                UpdateAppConfigForTest(app, setup, copyOnUpdate: false);

                var result = dotnet.Exec(app.AppDll)
                    .EnableTracingAndCaptureOutputs()
                    .RuntimeId(setup.Rid)
                    .Execute();
                result.Should().Pass()
                    .And.HaveResolvedAssembly(expected.IncludedAssemblyPaths, app)
                    .And.NotHaveResolvedAssembly(expected.ExcludedAssemblyPaths, app)
                    .And.HaveResolvedNativeLibraryPath(expected.IncludedNativeLibraryPaths, app)
                    .And.NotHaveResolvedNativeLibraryPath(expected.ExcludedNativeLibraryPaths, app)
                    .And.HaveReadRidGraph(setup.ShouldUseRidGraph)
                    .And.HaveUsedFrameworkProbe(dotnet.GreatestVersionSharedFxPath, level: 1);

                if (setup.ShouldUseFallbackRid.HasValue)
                    result.Should().HaveUsedFallbackRid(setup.ShouldUseFallbackRid.Value);
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
            TestSetup setup,
            ResolvedPaths expected,
            Action<NetCoreAppBuilder> appCustomizer)
        {
            var component = SharedState.CreateComponentWithNoDependencies(b => b
                .WithPackage("NativeDependency", "1.0.0", p => assetsCustomizer?.Invoke(p))
                .WithCustomizer(appCustomizer));

            DotNetCli dotnet;
            if (setup.HasRidGraph)
            {
                // Use the fallbacks from the product when testing the computed RID
                dotnet = setup.Rid == null ? SharedState.DotNetWithNetCoreApp_RuntimeFallbacks : SharedState.DotNetWithNetCoreApp;
            }
            else
            {
                dotnet = SharedState.DotNetWithNetCoreApp_NoRuntimeFallbacks;
            }

            TestApp app = UpdateAppConfigForTest(SharedState.FrameworkReferenceApp, setup, copyOnUpdate: true);

            var result = SharedState.RunComponentResolutionTest(component.AppDll, app, dotnet.GreatestVersionHostFxrPath, command => command
                .RuntimeId(setup.Rid));
            result.Should().Pass()
                .And.HaveSuccessfullyResolvedComponentDependencies()
                .And.HaveResolvedComponentDependencyAssembly(expected.IncludedAssemblyPaths, component)
                .And.NotHaveResolvedComponentDependencyAssembly(expected.ExcludedAssemblyPaths, component)
                .And.HaveResolvedComponentDependencyNativeLibraryPath(expected.IncludedNativeLibraryPaths, component)
                .And.NotHaveResolvedComponentDependencyNativeLibraryPath(expected.ExcludedNativeLibraryPaths, component)
                .And.HaveReadRidGraph(setup.ShouldUseRidGraph)
                .And.NotHaveUsedFrameworkProbe(dotnet.GreatestVersionSharedFxPath);

            if (setup.ShouldUseFallbackRid.HasValue)
                    result.Should().HaveUsedFallbackRid(setup.ShouldUseFallbackRid.Value);
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
            TestSetup setup,
            ResolvedPaths expected,
            Action<NetCoreAppBuilder> appCustomizer)
        {
            var component = SharedState.CreateComponentWithNoDependencies(b => b
                .WithPackage("NativeDependency", "1.0.0", p => assetsCustomizer?.Invoke(p))
                .WithCustomizer(appCustomizer));

            TestApp app;
            if (setup.HasRidGraph)
            {
                // Use the fallbacks from the product when testing the computed RID
                app = setup.Rid == null ? SharedState.HostApp_RuntimeFallbacks : SharedState.HostApp;
            }
            else
            {
                app = SharedState.HostApp_NoRuntimeFallbacks;
            }

            app = UpdateAppConfigForTest(app, setup, copyOnUpdate: true);

            var result = SharedState.RunComponentResolutionTest(component.AppDll, app, app.Location, command => command
                .RuntimeId(setup.Rid));
            result.Should().Pass()
                .And.HaveSuccessfullyResolvedComponentDependencies()
                .And.HaveResolvedComponentDependencyAssembly(expected.IncludedAssemblyPaths, component)
                .And.NotHaveResolvedComponentDependencyAssembly(expected.ExcludedAssemblyPaths, component)
                .And.HaveResolvedComponentDependencyNativeLibraryPath(expected.IncludedNativeLibraryPaths, component)
                .And.NotHaveResolvedComponentDependencyNativeLibraryPath(expected.ExcludedNativeLibraryPaths, component)
                .And.HaveReadRidGraph(setup.ShouldUseRidGraph);

            if (setup.ShouldUseFallbackRid.HasValue)
                result.Should().HaveUsedFallbackRid(setup.ShouldUseFallbackRid.Value);
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

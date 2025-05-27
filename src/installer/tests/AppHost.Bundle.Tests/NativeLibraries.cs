// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class NativeLibraries : IClassFixture<NativeLibraries.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public NativeLibraries(NativeLibraries.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void PInvoke_FrameworkDependent() => PInvoke(false, false);

        [Fact]
        public void PInvoke_FrameworkDependent_BundleNative() => PInvoke(false, true);

        [Fact]
        public void PInvoke_SelfContained() => PInvoke(true, false);

        [Fact]
        public void PInvoke_SelfContained_BundleNative() => PInvoke(true, true);

        private void PInvoke(bool selfContained, bool bundleNative)
        {
            string app = (selfContained, bundleNative) switch
            {
                (true, true) => sharedTestState.SelfContainedApp_BundleNative,
                (true, false) => sharedTestState.SelfContainedApp,
                (false, true) => sharedTestState.FrameworkDependentApp_BundleNative,
                (false, false) => sharedTestState.FrameworkDependentApp
            };

            Command.Create(app, "load_native_library_pinvoke")
                .CaptureStdErr()
                .CaptureStdOut()
                .DotNetRoot(selfContained ? null : TestContext.BuiltDotNet.BinPath)
                .Execute()
                .Should().Pass()
                .And.CallPInvoke(null, true)
                .And.CallPInvoke(DllImportSearchPath.AssemblyDirectory, true)
                // Single-file always looks in application directory, even when only System32 is specified
                .And.CallPInvoke(DllImportSearchPath.System32, true);
        }

        [Fact]
        public void TryLoad_FrameworkDependent() => TryLoad(false, false);

        [Fact]
        public void TryLoad_FrameworkDependent_BundleNative() => TryLoad(false, true);

        [Fact]
        public void TryLoad_SelfContained() => TryLoad(true, false);

        [Fact]
        public void TryLoad_SelfContained_BundleNative() => TryLoad(true, true);

        private void TryLoad(bool selfContained, bool bundleNative)
        {
            string app = (selfContained, bundleNative) switch
            {
                (true, true) => sharedTestState.SelfContainedApp_BundleNative,
                (true, false) => sharedTestState.SelfContainedApp,
                (false, true) => sharedTestState.FrameworkDependentApp_BundleNative,
                (false, false) => sharedTestState.FrameworkDependentApp
            };

            Command.Create(app, "load_native_library_api")
                .CaptureStdErr()
                .CaptureStdOut()
                .DotNetRoot(selfContained ? null : TestContext.BuiltDotNet.BinPath)
                .Execute()
                .Should().Pass()
                .And.TryLoadLibrary(null, true)
                .And.TryLoadLibrary(DllImportSearchPath.AssemblyDirectory, true)
                // Single-file always looks in application directory, even when only System32 is specified
                .And.TryLoadLibrary(DllImportSearchPath.System32, true);
        }

        internal static string GetLibraryName(DllImportSearchPath? flags)
        {
            string name = Path.GetFileNameWithoutExtension(Binaries.HostPolicy.MockName);
            return flags switch
            {
                DllImportSearchPath.AssemblyDirectory => $"{name}-{nameof(DllImportSearchPath.AssemblyDirectory)}",
                DllImportSearchPath.System32 => $"{name}-{nameof(DllImportSearchPath.System32)}",
                _ => name
            };
        }

        public class SharedTestState : IDisposable
        {
            public string FrameworkDependentApp { get; }
            public string SelfContainedApp { get; }

            public string FrameworkDependentApp_BundleNative { get; }
            public string SelfContainedApp_BundleNative { get; }

            private SingleFileTestApp _frameworkDependentApp;
            private SingleFileTestApp _selfContainedApp;

            public SharedTestState()
            {
                _frameworkDependentApp = SingleFileTestApp.CreateFrameworkDependent("HelloWorld");
                _selfContainedApp = SingleFileTestApp.CreateSelfContained("HelloWorld");

                // Copy over mockhostpolicy - the app will try to load this
                string[] names = [GetLibraryName(null), GetLibraryName(DllImportSearchPath.AssemblyDirectory), GetLibraryName(DllImportSearchPath.System32)];
                foreach (string name in names)
                {
                    string fileName = $"{name}{Path.GetExtension(Binaries.HostPolicy.MockName)}";
                    File.Copy(Binaries.HostPolicy.MockPath, Path.Combine(_frameworkDependentApp.NonBundledLocation, fileName));
                    File.Copy(Binaries.HostPolicy.MockPath, Path.Combine(_selfContainedApp.NonBundledLocation, fileName));
                }

                FrameworkDependentApp = _frameworkDependentApp.Bundle();
                SelfContainedApp = _selfContainedApp.Bundle();

                FrameworkDependentApp_BundleNative = _frameworkDependentApp.Bundle(BundleOptions.BundleNativeBinaries);
                SelfContainedApp_BundleNative = _selfContainedApp.Bundle(BundleOptions.BundleNativeBinaries);
            }

            public void Dispose()
            {
                _frameworkDependentApp.Dispose();
                _selfContainedApp.Dispose();
            }
        }
    }

    public static class NativeLibrariesResultExtensions
    {
        public static FluentAssertions.AndConstraint<CommandResultAssertions> CallPInvoke(this CommandResultAssertions assertion, DllImportSearchPath? flags, bool success)
        {
            var constraint = assertion.HaveStdOutContaining($"Loading {NativeLibraries.GetLibraryName(flags)} via P/Invoke (flags: {(flags.HasValue ? flags : "default")}) {(success ? "succeeded" : "failed")}");
            if (!success)
                constraint = constraint.And.HaveStdOutContaining(typeof(DllNotFoundException).FullName);

            return constraint;
        }

        public static FluentAssertions.AndConstraint<CommandResultAssertions> TryLoadLibrary(this CommandResultAssertions assertion, DllImportSearchPath? flags, bool success)
        {
            return assertion.HaveStdOutContaining($"Loading {NativeLibraries.GetLibraryName(flags)} via NativeLibrary API (flags: {(flags.HasValue ? flags : "default")}) {(success ? "succeeded" : "failed")}");
        }
    }
}

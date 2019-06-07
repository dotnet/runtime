// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public class PortableAppRidAssetResolution : 
        DependencyResolutionBase,
        IClassFixture<PortableAppRidAssetResolution.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        public PortableAppRidAssetResolution(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        [Theory]
        [InlineData("win", "win/WindowsAssembly.dll", "linux/LinuxAssembly.dll")]
        [InlineData("win10-x64", "win/WindowsAssembly.dll", "linux/LinuxAssembly.dll")]
        [InlineData("linux", "linux/LinuxAssembly.dll", "win/WindowsAssembly.dll")]
        public void RidSpecificAssembly(string rid, string includedPath, string excludedPath)
        {
            using (TestApp app = NetCoreAppBuilder.PortableForNETCoreApp(SharedState.FrameworkReferenceApp)
                .WithProject(p => p
                    .WithAssemblyGroup(null, g => g.WithMainAssembly())
                    .WithAssemblyGroup("win", g => g.WithAsset("win/WindowsAssembly.dll"))
                    .WithAssemblyGroup("linux", g => g.WithAsset("linux/LinuxAssembly.dll")))
                .Build())
            {
                SharedState.DotNetWithNetCoreApp.Exec(app.AppDll)
                    .EnableTracingAndCaptureOutputs()
                    .RuntimeId(rid)
                    .Execute()
                    .Should().Pass()
                    .And.HaveResolvedAssembly(includedPath, app)
                    .And.NotHaveResolvedAssembly(excludedPath, app);
            }
        }

        [Theory]
        [InlineData("win", "win", "linux")]
        [InlineData("win10-x64", "win", "linux")]
        [InlineData("linux", "linux", "win")]
        public void RidSpecificNativeLibrary(string rid, string includedPath, string excludedPath)
        {
            using (TestApp app = NetCoreAppBuilder.PortableForNETCoreApp(SharedState.FrameworkReferenceApp)
                .WithProject(p => p
                    .WithAssemblyGroup(null, g => g.WithMainAssembly())
                    .WithNativeLibraryGroup("win", g => g.WithAsset("win/WindowsNativeLibrary.dll"))
                    .WithNativeLibraryGroup("linux", g => g.WithAsset("linux/LinuxNativeLibrary.so")))
                .Build())
            {
                SharedState.DotNetWithNetCoreApp.Exec(app.AppDll)
                    .EnableTracingAndCaptureOutputs()
                    .RuntimeId(rid)
                    .Execute()
                    .Should().Pass()
                    .And.HaveResolvedNativeLibraryPath(includedPath, app)
                    .And.NotHaveResolvedNativeLibraryPath(excludedPath, app);
            }
        }

        [Theory]
        [InlineData("win10-x64", "win-x64/ManagedWin64.dll")]
        [InlineData("win10-x86", "win/ManagedWin.dll")]
        [InlineData("linux", "any/ManagedAny.dll")]
        public void MostSpecificRidAssemblySelected(string rid, string expectedPath)
        {
            using (TestApp app = NetCoreAppBuilder.PortableForNETCoreApp(SharedState.FrameworkReferenceApp)
                .WithProject(p => p
                    .WithAssemblyGroup(null, g => g.WithMainAssembly())
                    .WithAssemblyGroup("any", g => g.WithAsset("any/ManagedAny.dll"))
                    .WithAssemblyGroup("win", g => g.WithAsset("win/ManagedWin.dll"))
                    .WithAssemblyGroup("win-x64", g => g.WithAsset("win-x64/ManagedWin64.dll")))
                .Build())
            {
                SharedState.DotNetWithNetCoreApp.Exec(app.AppDll)
                    .EnableTracingAndCaptureOutputs()
                    .RuntimeId(rid)
                    .Execute()
                    .Should().Pass()
                    .And.HaveResolvedAssembly(expectedPath, app);
            }
        }

        [Theory]
        [InlineData("win10-x64", "win-x64")]
        [InlineData("win10-x86", "win")]
        [InlineData("linux", "any")]
        public void MostSpecificRidNativeLibrarySelected(string rid, string expectedPath)
        {
            using (TestApp app = NetCoreAppBuilder.PortableForNETCoreApp(SharedState.FrameworkReferenceApp)
                .WithProject(p => p
                    .WithAssemblyGroup(null, g => g.WithMainAssembly())
                    .WithNativeLibraryGroup("any", g => g.WithAsset("any/NativeAny.dll"))
                    .WithNativeLibraryGroup("win", g => g.WithAsset("win/NativeWin.dll"))
                    .WithNativeLibraryGroup("win-x64", g => g.WithAsset("win-x64/NativeWin64.dll")))
                .Build())
            {
                SharedState.DotNetWithNetCoreApp.Exec(app.AppDll)
                    .EnableTracingAndCaptureOutputs()
                    .RuntimeId(rid)
                    .Execute()
                    .Should().Pass()
                    .And.HaveResolvedNativeLibraryPath(expectedPath, app);
            }
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp FrameworkReferenceApp { get; }

            public DotNetCli DotNetWithNetCoreApp { get; }

            public SharedTestState() : base("DependencyResolution")
            {
                DotNetWithNetCoreApp = DotNet("WithNetCoreApp")
                    .AddMicrosoftNETCoreAppFrameworkMockCoreClr("4.0.0")
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp(MicrosoftNETCoreApp, "4.0.0");
            }
        }
    }
}

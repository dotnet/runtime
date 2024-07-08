// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

using FluentAssertions;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.AppHost;
using Xunit;

namespace HostActivation.Tests
{
    public class InvalidHost : IClassFixture<InvalidHost.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public InvalidHost(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void AppHost_NotBound()
        {
            CommandResult result = Command.Create(sharedTestState.UnboundAppHost)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(expectedToFail: true);

            result.Should().Fail()
                .And.HaveStdErrContaining("This executable is not bound to a managed DLL to execute.");

            int exitCode = result.ExitCode;
            const int AppHostExeNotBoundFailure = unchecked((int)0x80008095);
            if (OperatingSystem.IsWindows())
            {
                exitCode.Should().Be(AppHostExeNotBoundFailure);
            }
            else
            {
                // Some Unix flavors filter exit code to ubyte.
                (exitCode & 0xFF).Should().Be(AppHostExeNotBoundFailure & 0xFF);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // GUI app host is only supported on Windows.
        public void AppHost_NotBound_GUI()
        {
            Command.Create(sharedTestState.UnboundAppHostGUI)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining("This executable is not bound to a managed DLL to execute.");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // GUI app host is only supported on Windows.
        public void AppHost_NotBound_GUI_TraceFile()
        {
            string traceFilePath;
            Command.Create(sharedTestState.UnboundAppHostGUI)
                .EnableHostTracingToFile(out traceFilePath)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Fail()
                .And.FileExists(traceFilePath)
                .And.FileContains(traceFilePath, "This executable is not bound to a managed DLL to execute.")
                .And.HaveStdErrContaining("This executable is not bound to a managed DLL to execute.");

            FileUtils.DeleteFileIfPossible(traceFilePath);
        }

        [Fact]
        public void DotNet_Renamed()
        {
            CommandResult result = Command.Create(sharedTestState.RenamedDotNet)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(expectedToFail: true);

            result.Should().Fail()
                .And.HaveStdErrContaining($"Error: cannot execute dotnet when renamed to {Path.GetFileNameWithoutExtension(sharedTestState.RenamedDotNet)}");

            int exitCode = result.ExitCode;
            const int EntryPointFailure = unchecked((int)0x80008084);
            if (OperatingSystem.IsWindows())
            {
                exitCode.Should().Be(EntryPointFailure);
            }
            else
            {
                // Some Unix flavors filter exit code to ubyte.
                (exitCode & 0xFF).Should().Be(EntryPointFailure & 0xFF);
            }
        }

        public class SharedTestState : IDisposable
        {
            public TestArtifact BaseDirectory { get; }

            public string RenamedDotNet { get;  }
            public string UnboundAppHost { get; }
            public string UnboundAppHostGUI { get;  }

            public SharedTestState()
            {
                BaseDirectory = TestArtifact.Create(nameof(InvalidHost));
                Directory.CreateDirectory(BaseDirectory.Location);

                RenamedDotNet = Path.Combine(BaseDirectory.Location, Binaries.GetExeFileNameForCurrentPlatform("renamed"));
                File.Copy(Binaries.DotNet.FilePath, RenamedDotNet);

                UnboundAppHost = Path.Combine(BaseDirectory.Location, Binaries.GetExeFileNameForCurrentPlatform("unbound"));
                File.Copy(Binaries.AppHost.FilePath, UnboundAppHost);

                if (OperatingSystem.IsWindows())
                {
                    // Mark the apphost as GUI, but don't bind it to anything - this will cause it to fail
                    UnboundAppHostGUI = Path.Combine(BaseDirectory.Location, Binaries.GetExeFileNameForCurrentPlatform("unboundgui"));
                    File.Copy(Binaries.AppHost.FilePath, UnboundAppHostGUI);
                    PEUtils.SetWindowsGraphicalUserInterfaceBit(UnboundAppHostGUI);
                }
            }

            public void Dispose()
            {
                BaseDirectory.Dispose();
            }
        }
    }
}


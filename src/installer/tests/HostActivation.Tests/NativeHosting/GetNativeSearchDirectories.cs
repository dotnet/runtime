// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public class GetNativeSearchDirectories : IClassFixture<GetNativeSearchDirectories.SharedTestState>
    {
        public class Scenario
        {
            public const string GetForCommandLine = "get_for_command_line";
        }

        private const string GetNativeSearchDirectoriesArg = "get_native_search_directories";

        private readonly SharedTestState sharedState;

        public GetNativeSearchDirectories(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BasicApp(bool hasDepsJson)
        {
            // Original app will dispose of its copies
            var app = hasDepsJson ? sharedState.App : sharedState.App.Copy();

            string[] args =
            {
                GetNativeSearchDirectoriesArg,
                Scenario.GetForCommandLine,
                sharedState.HostFxrPath,
                sharedState.DotNet.DotnetExecutablePath,
                app.AppDll
            };

            if (!hasDepsJson)
                File.Delete(app.DepsJson);

            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNet.BinPath)
                .Execute();

            // App directory is always added to native search directories if there is no deps.json.
            // Otherwise, only directories with native assets are added.
            string expectedSearchDirectories = hasDepsJson
                ? string.Empty
                : $"{app.Location}{Path.DirectorySeparatorChar}{Path.PathSeparator}";

            // Microsoft.NETCore.App framework directory
            expectedSearchDirectories += $"{Path.Combine(sharedState.DotNet.SharedFxPath, SharedTestState.NetCoreAppVersion)}{Path.DirectorySeparatorChar}{Path.PathSeparator}";

            result.Should().Pass()
                .And.HaveStdOutContaining($"Native search directories: '{expectedSearchDirectories}'");
        }

        [Fact]
        public void Invalid_NullBufferWithNonZeroSize()
        {
            string[] args =
            {
                GetNativeSearchDirectoriesArg,
                Scenario.GetForCommandLine,
                sharedState.HostFxrPath,
                "test_NullBufferWithNonZeroSize"
            };

            sharedState.CreateNativeHostCommand(args, sharedState.DotNet.BinPath)
                .Execute()
                .Should().Fail()
                .And.HaveStdOutContaining($"get_native_search_directories (null, 1) returned: 0x{Constants.ErrorCode.InvalidArgFailure:x}")
                .And.HaveStdOutContaining("buffer_size: 0")
                .And.HaveStdOutContaining("hostfxr reported errors:")
                .And.HaveStdOutContaining("hostfxr_get_native_search_directories received an invalid argument.");
        }

        [Fact]
        public void Invalid_NonNullBufferWithNegativeSize()
        {
            string[] args =
            {
                GetNativeSearchDirectoriesArg,
                Scenario.GetForCommandLine,
                sharedState.HostFxrPath,
                "test_NonNullBufferWithNegativeSize"
            };

            sharedState.CreateNativeHostCommand(args, sharedState.DotNet.BinPath)
                .Execute()
                .Should().Fail()
                .And.HaveStdOutContaining($"get_native_search_directories (temp_buffer, -1) returned: 0x{Constants.ErrorCode.InvalidArgFailure:x}")
                .And.HaveStdOutContaining("buffer_size: 0")
                .And.HaveStdOutContaining("hostfxr reported errors:")
                .And.HaveStdOutContaining("hostfxr_get_native_search_directories received an invalid argument.");
        }

        // This test also validates that hostfxr_set_error_writer propagates the custom writer
        // to the hostpolicy.dll for the duration of those calls.
        [Fact]
        public void WithInvalidDepsJson()
        {
            var app = sharedState.App.Copy();
            string[] args =
            {
                GetNativeSearchDirectoriesArg,
                Scenario.GetForCommandLine,
                sharedState.HostFxrPath,
                sharedState.DotNet.DotnetExecutablePath,
                app.AppDll
            };

            string depsJsonFile = app.DepsJson;
            File.WriteAllText(depsJsonFile, "{");

            sharedState.CreateNativeHostCommand(args, sharedState.DotNet.BinPath)
                .Execute()
                .Should().Fail()
                .And.HaveStdOutContaining($"get_native_search_directories (null,0) returned unexpected error code 0x{Constants.ErrorCode.ResolverInitFailure:x} expected HostApiBufferTooSmall (0x80008098).")
                .And.HaveStdOutContaining("buffer_size: 0")
                .And.HaveStdOutContaining("hostfxr reported errors:")
                .And.HaveStdOutContaining($"A JSON parsing exception occurred in [{depsJsonFile}], offset 1 (line 1, column 2): Missing a name for object member.")
                .And.HaveStdOutContaining($"Error initializing the dependency resolver: An error occurred while parsing: {depsJsonFile}");
        }

        [Fact]
        public void CliCommand()
        {
            string[] args =
            {
                GetNativeSearchDirectoriesArg,
                Scenario.GetForCommandLine,
                sharedState.HostFxrPath,
                sharedState.DotNet.DotnetExecutablePath,
                "build"
            };

            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNet.BinPath)
                .Execute();

            result.Should().Fail()
                .And.HaveStdOutContaining($"get_native_search_directories (null,0) returned unexpected error code 0x{Constants.ErrorCode.AppArgNotRunnable:x} expected HostApiBufferTooSmall (0x80008098).")
                .And.HaveStdOutContaining("buffer_size: 0")
                .And.HaveStdErrContaining("Application 'build' is not a managed executable.");
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string HostFxrPath { get; }
            public DotNetCli DotNet { get; }
            public TestApp App { get; }

            public const string NetCoreAppVersion = "2.2.0";

            public SharedTestState()
            {
                DotNet = new DotNetBuilder(BaseDirectory, TestContext.BuiltDotNet.BinPath, "mockRuntime")
                    .AddMicrosoftNETCoreAppFrameworkMockCoreClr(NetCoreAppVersion)
                    .Build();

                HostFxrPath = DotNet.GreatestVersionHostFxrFilePath;

                App = new TestApp(Path.Combine(BaseDirectory, "app"));
                App.PopulateFrameworkDependent(Constants.MicrosoftNETCoreApp, NetCoreAppVersion);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (App != null)
                        App.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}

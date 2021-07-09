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

        [Fact]
        public void BasicApp()
        {
            string[] args =
            {
                GetNativeSearchDirectoriesArg,
                Scenario.GetForCommandLine,
                sharedState.HostFxrPath,
                sharedState.DotNet.DotnetExecutablePath,
                sharedState.AppPath
            };

            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.DotNet.BinPath)
                .Execute();

            string pathSuffix = Path.DirectorySeparatorChar.ToString();
            string expectedSearchDirectories =
                Path.GetDirectoryName(sharedState.AppPath) + pathSuffix + Path.PathSeparator +
                Path.Combine(sharedState.DotNet.BinPath, "shared", "Microsoft.NETCore.App", SharedTestState.NetCoreAppVersion) + pathSuffix + Path.PathSeparator;
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
            string[] args =
            {
                GetNativeSearchDirectoriesArg,
                Scenario.GetForCommandLine,
                sharedState.HostFxrPath,
                sharedState.DotNet.DotnetExecutablePath,
                sharedState.AppPath
            };

            string depsJsonFile = Path.Combine(sharedState.AppDirectory, "App.deps.json");
            try
            {
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
            finally
            {
                FileUtils.DeleteFileIfPossible(depsJsonFile);
            }
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
            public string AppDirectory { get; }
            public string AppPath { get; }

            public const string NetCoreAppVersion = "2.2.0";

            public SharedTestState()
            {
                DotNet = new DotNetBuilder(BaseDirectory, Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish"), "mockRuntime")
                    .AddMicrosoftNETCoreAppFrameworkMockCoreClr(NetCoreAppVersion)
                    .Build();

                HostFxrPath = Path.Combine(
                    DotNet.GreatestVersionHostFxrPath,
                    RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostfxr"));

                AppDirectory = Path.Combine(BaseDirectory, "app");
                Directory.CreateDirectory(AppDirectory);
                AppPath = Path.Combine(AppDirectory, "App.dll");
                File.WriteAllText(AppPath, string.Empty);

                RuntimeConfig.FromFile(Path.Combine(AppDirectory, "App.runtimeconfig.json"))
                    .WithFramework(new RuntimeConfig.Framework(Constants.MicrosoftNETCoreApp, NetCoreAppVersion))
                    .Save();
            }
        }
    }
}

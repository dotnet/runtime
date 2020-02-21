using System.IO;
using Microsoft.DotNet.Cli.Build;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public class HostNoContextExports
    {
        public class Scenario
        {
            public const string GetHostFxrRuntimePropertyValue = "get_prop_value";
            public const string GetHostFxrRuntimeProperties = "get_properties";
        }

        private const string GetHostFxrExportsNoContext = "get_hostfxr_exports_no_context";

        private readonly SharedTestState sharedState;

        public HostNoContextExports()
        {
            sharedState = new SharedTestState();
        }

        [Fact]
        public void GetRuntimeProperty()
        {
            string[] args =
            {
                GetHostFxrExportsNoContext,
                Scenario.GetHostFxrRuntimePropertyValue,
                sharedState.HostFxrPath
            };

            sharedState.CreateNativeHostCommand(args, sharedState.DotNet.BinPath)
                .Execute()
                .Should().Pass();
        }

        [Fact]
        public void GetRuntimeProperties()
        {
            string[] args =
            {
                GetHostFxrExportsNoContext,
                Scenario.GetHostFxrRuntimeProperties,
                sharedState.HostFxrPath
            };

            sharedState.CreateNativeHostCommand(args, sharedState.DotNet.BinPath)
                .Execute()
                .Should().Pass();
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

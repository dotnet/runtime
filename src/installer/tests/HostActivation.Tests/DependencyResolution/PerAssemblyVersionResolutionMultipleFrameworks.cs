// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public abstract class PerAssemblyVersionResolutionMultipleFrameworksBase :
        ComponentDependencyResolutionBase,
        IClassFixture<PerAssemblyVersionResolutionBase.SharedTestState>
    {
        protected readonly SharedTestState SharedState;

        public PerAssemblyVersionResolutionMultipleFrameworksBase(SharedTestState fixture)
        {
            SharedState = fixture;
        }

        protected const string HighWare = "HighWare";
        protected const string TestVersionsPackage = "Test.Versions.Package";

        // The test framework above has 4 assemblies in it each with different set of assembly and file versions.
        //                                     NetCoreApp        HighWare
        // - TestAssemblyWithNoVersions:       null   , null     null   , null
        // - TestAssemblyWithAssemblyVersion:  2.1.1.1, null     2.1.1.2, null
        // - TestAssemblyWithFileVersion:      null   , 3.2.2.2  null   , 3.2.2.2
        // - TestAssemblyWithBothVersions:     2.1.1.1, 3.2.2.2  2.1.1.0, 3.2.2.0
        private const string TestAssemblyWithNoVersions = "Test.Assembly.NoVersions";
        private const string TestAssemblyWithAssemblyVersion = "Test.Assembly.AssemblyVersion";
        private const string TestAssemblyWithFileVersion = "Test.Assembly.FileVersion";
        private const string TestAssemblyWithBothVersions = "Test.Assembly.BothVersions";

        [Theory]
        [InlineData(TestAssemblyWithBothVersions, null, null, MicrosoftNETCoreApp)] // NetCoreApp has higher version than HighWare
        [InlineData(TestAssemblyWithBothVersions, "1.0.0.0", "1.0.0.0", MicrosoftNETCoreApp)]
        [InlineData(TestAssemblyWithBothVersions, "3.0.0.0", "4.0.0.0", null)]  // App has higher version than any framework
        [InlineData(TestAssemblyWithBothVersions, "2.1.1.1", "3.3.0.0", null)]  // App has higher file version
        [InlineData(TestAssemblyWithBothVersions, "2.1.1.1", "3.2.2.2", MicrosoftNETCoreApp)]  // Lower level framework always wins on equality (this is intentional)
        [InlineData(TestAssemblyWithBothVersions, null, "4.0.0.0", MicrosoftNETCoreApp)] // The one with version wins
        [InlineData(TestAssemblyWithBothVersions, null, "2.0.0.0", MicrosoftNETCoreApp)] // The one with version wins
        [InlineData(TestAssemblyWithBothVersions, "3.0.0.0", null, null)]
        [InlineData(TestAssemblyWithBothVersions, "2.1.1.1", null, MicrosoftNETCoreApp)]
        [InlineData(TestAssemblyWithNoVersions, null, null, MicrosoftNETCoreApp)] // No versions are treated as equal (so lower one wins)
        [InlineData(TestAssemblyWithNoVersions, "1.0.0.0", null, null)] // The one with version wins
        [InlineData(TestAssemblyWithNoVersions, "1.0.0.0", "1.0.0.0", null)] // The one with version wins
        [InlineData(TestAssemblyWithNoVersions, null, "1.0.0.0", null)] // The one with version wins
        [InlineData(TestAssemblyWithAssemblyVersion, null, null, HighWare)] // Highware has higher version than NetCoreApp
        [InlineData(TestAssemblyWithAssemblyVersion, "1.0.0.0", null, HighWare)]
        [InlineData(TestAssemblyWithAssemblyVersion, null, "1.0.0.0", HighWare)]
        [InlineData(TestAssemblyWithAssemblyVersion, "3.0.0.0", "1.0.0.0", null)] // App has higher version than any framework
        [InlineData(TestAssemblyWithAssemblyVersion, "2.1.1.2", null, HighWare)] // Both are exactly the same, so lower level wins
        [InlineData(TestAssemblyWithAssemblyVersion, "2.1.1.2", "1.0.0.0", null)]
        [InlineData(TestAssemblyWithFileVersion, null, null, MicrosoftNETCoreApp)] // Frameworks both have the same version - lower one wins
        [InlineData(TestAssemblyWithFileVersion, "1.0.0.0", null, null)] // App has assembly version, no framework has it - so app wins
        [InlineData(TestAssemblyWithFileVersion, null, "1.0.0.0", MicrosoftNETCoreApp)]
        [InlineData(TestAssemblyWithFileVersion, null, "4.0.0.0", null)] // App has higher version than either framework
        [InlineData(TestAssemblyWithFileVersion, null, "3.2.2.2", MicrosoftNETCoreApp)] // Exactly equal - lower one wins
        public void AppWithSameAssemblyAsFramework(string testAssemblyName, string appAsmVersion, string appFileVersion, string frameWorkWins)
        {
            RunTest(null, testAssemblyName, appAsmVersion, appFileVersion, frameWorkWins);
        }

        [Theory]
        [InlineData("1.1.1")]  // Exact match - no roll forward
        [InlineData("1.1.0")]  // Patch roll forward
        [InlineData("1.0.0")]  // Minor
        [InlineData("0.0.0")]  // Major
        public void AppWithExactlySameAssemblyAsFrameworkWithRollForward(string frameworkReferenceVersion)
        {
            RunTest(
                runtimeConfig => runtimeConfig
                    .WithFramework(HighWare, frameworkReferenceVersion)
                    .WithRollForward(Constants.RollForwardSetting.Major),
                TestAssemblyWithBothVersions, "2.1.1.1", "3.2.2.2", MicrosoftNETCoreApp);
        }

        protected abstract void RunTest(Action<RuntimeConfig> runtimeConfigCustomizer, string testAssemblyName, string appAsmVersion, string appFileVersion, string frameWorkWins);

        public class SharedTestState : ComponentSharedTestStateBase
        {
            public string HighWarePath => Path.Combine(DotNetWithNetCoreApp.BinPath, "shared", HighWare, "1.1.1");

            public SharedTestState()
            {
            }

            protected override TestApp CreateTestFrameworkReferenceApp() => CreateFrameworkReferenceApp(HighWare, "1.1.1");

            protected override void CustomizeDotNetWithNetCoreAppMicrosoftNETCoreApp(NetCoreAppBuilder builder)
            {
                builder
                    .WithPackage(TestVersionsPackage, "1.1.1", b => b
                        .WithAssemblyGroup(null, g => g
                            .WithAsset(TestAssemblyWithNoVersions + ".dll")
                            .WithAsset(TestAssemblyWithAssemblyVersion + ".dll", rf => rf.WithVersion("2.1.1.1", null))
                            .WithAsset(TestAssemblyWithFileVersion + ".dll", rf => rf.WithVersion(null, "3.2.2.2"))
                            .WithAsset(TestAssemblyWithBothVersions + ".dll", rf => rf.WithVersion("2.1.1.1", "3.2.2.2"))));
            }

            protected override void CustomizeDotNetWithNetCoreApp(DotNetBuilder builder)
            {
                builder.AddFramework(
                    HighWare,
                    "1.1.1",
                    runtimeConfig => runtimeConfig.WithFramework(MicrosoftNETCoreApp, "4.0.0"),
                    path => NetCoreAppBuilder.ForNETCoreApp(HighWare, RepoDirectoriesProvider.Default.TargetRID)
                        .WithProject(HighWare, "1.1.1", p => p
                            .WithAssemblyGroup(null, g => g
                            .WithAsset(TestAssemblyWithNoVersions + ".dll")
                            .WithAsset(TestAssemblyWithAssemblyVersion + ".dll", rf => rf.WithVersion("2.1.1.2", null))
                            .WithAsset(TestAssemblyWithFileVersion + ".dll", rf => rf.WithVersion(null, "3.2.2.2"))
                            .WithAsset(TestAssemblyWithBothVersions + ".dll", rf => rf.WithVersion("2.1.1.0", "3.2.2.0"))))
                        .Build(new TestApp(path, HighWare)));
            }

            public TestApp CreateTestFrameworkReferenceApp(Action<NetCoreAppBuilder> customizer)
            {
                TestApp testApp = FrameworkReferenceApp.Copy();
                NetCoreAppBuilder builder = NetCoreAppBuilder.PortableForNETCoreApp(testApp);
                builder.WithProject(p => p
                    .WithAssemblyGroup(null, g => g.WithMainAssembly()));
                customizer(builder);
                return builder.Build(testApp);
            }
        }
    }

    public class AppPerAssemblyVersionResolutionMultipleFrameworks :
        PerAssemblyVersionResolutionMultipleFrameworksBase,
        IClassFixture<PerAssemblyVersionResolutionMultipleFrameworksBase.SharedTestState>
    {
        public AppPerAssemblyVersionResolutionMultipleFrameworks(SharedTestState sharedState)
            : base(sharedState)
        {
        }

        protected override void RunTest(Action<RuntimeConfig> runtimeConfigCustomizer, string testAssemblyName, string appAsmVersion, string appFileVersion, string frameworkWins)
        {
            var app = SharedState.CreateTestFrameworkReferenceApp(b => b
                .WithPackage(TestVersionsPackage, "1.0.0", lib => lib
                    .WithAssemblyGroup(null, g => g
                        .WithAsset(testAssemblyName + ".dll", rf => rf
                            .WithVersion(appAsmVersion, appFileVersion)))));
            if (runtimeConfigCustomizer is not null)
            {
                var runtimeConfig = new RuntimeConfig(app.RuntimeConfigJson);
                runtimeConfigCustomizer(runtimeConfig);
                runtimeConfig.Save();
            }

            string expectedBaseLocation = frameworkWins switch
            {
                MicrosoftNETCoreApp => SharedState.DotNetWithNetCoreApp.GreatestVersionSharedFxPath,
                HighWare => SharedState.HighWarePath,
                _ => app.Location,
            };
            string expectedTestAssemblyPath = Path.Combine(expectedBaseLocation, testAssemblyName + ".dll");

            SharedState.DotNetWithNetCoreApp.Exec(app.AppDll)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveResolvedAssembly(expectedTestAssemblyPath)
                .And.HaveUsedFrameworkProbe(SharedState.HighWarePath, level: 1)
                .And.HaveUsedFrameworkProbe(SharedState.DotNetWithNetCoreApp.GreatestVersionSharedFxPath, level: 2);
        }
    }

    public class ComponentPerAssemblyVersionResolutionMultipleFrameworks :
        PerAssemblyVersionResolutionMultipleFrameworksBase,
        IClassFixture<PerAssemblyVersionResolutionMultipleFrameworksBase.SharedTestState>
    {
        public ComponentPerAssemblyVersionResolutionMultipleFrameworks(SharedTestState sharedState)
            : base(sharedState)
        {
        }

        protected override void RunTest(Action<RuntimeConfig> runtimeConfigCustomizer, string testAssemblyName, string appAsmVersion, string appFileVersion, string frameworkWins)
        {
            var component = SharedState.CreateComponentWithNoDependencies(b => b
                .WithPackage(TestVersionsPackage, "1.0.0", lib => lib
                    .WithAssemblyGroup(null, g => g
                        .WithAsset(testAssemblyName + ".dll", rf => rf
                            .WithVersion(appAsmVersion, appFileVersion)))));
            if (runtimeConfigCustomizer is not null)
            {
                var runtimeConfig = RuntimeConfig.FromFile(component.RuntimeConfigJson);
                runtimeConfigCustomizer(runtimeConfig);
                runtimeConfig.Save();
            }

            // For component dependency resolution, frameworks are not considered, so the assembly from the component always wins
            string expectedTestAssemblyPath = Path.Combine(component.Location, testAssemblyName + ".dll");

            SharedState.RunComponentResolutionTest(component)
                .Should().Pass()
                .And.HaveSuccessfullyResolvedComponentDependencies()
                .And.HaveResolvedComponentDependencyAssembly($"{component.AppDll};{expectedTestAssemblyPath}")
                .And.NotHaveUsedFrameworkProbe(SharedState.HighWarePath)
                .And.NotHaveUsedFrameworkProbe(SharedState.DotNetWithNetCoreApp.GreatestVersionSharedFxPath);
        }
    }
}

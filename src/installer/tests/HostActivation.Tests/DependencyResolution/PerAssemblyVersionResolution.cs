// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public abstract class PerAssemblyVersionResolutionBase :
        ComponentDependencyResolutionBase,
        IClassFixture<PerAssemblyVersionResolutionBase.SharedTestState>
    {
        protected readonly SharedTestState SharedState;

        public PerAssemblyVersionResolutionBase(SharedTestState fixture)
        {
            SharedState = fixture;
        }

        protected const string TestVersionsPackage = "Test.Versions.Package";

        // The test framework above has 4 assemblies in it each with different set of assembly and file versions.
        // The version values are always (if present)
        // - assembly version: 2.1.1.1
        // - file version:     3.2.2.2
        private const string TestAssemblyWithNoVersions = "Test.Assembly.NoVersions";
        private const string TestAssemblyWithAssemblyVersion = "Test.Assembly.AssemblyVersion";
        private const string TestAssemblyWithFileVersion = "Test.Assembly.FileVersion";
        private const string TestAssemblyWithBothVersions = "Test.Assembly.BothVersions";

        [Theory]
        [InlineData(TestAssemblyWithBothVersions, null, null, false)]
        [InlineData(TestAssemblyWithBothVersions, "1.0.0.0", "1.0.0.0", false)]
        [InlineData(TestAssemblyWithBothVersions, "3.0.0.0", "4.0.0.0", true)]
        [InlineData(TestAssemblyWithBothVersions, "2.1.1.1", "1.0.0.0", false)]
        [InlineData(TestAssemblyWithBothVersions, "2.1.1.1", "3.3.0.0", true)]
        [InlineData(TestAssemblyWithBothVersions, "2.1.1.1", "3.2.2.2", false)] // Lower level framework always wins on equality (this is intentional)
        [InlineData(TestAssemblyWithBothVersions, null, "4.0.0.0", false)] // The one with version wins
        [InlineData(TestAssemblyWithBothVersions, null, "2.0.0.0", false)] // The one with version wins
        [InlineData(TestAssemblyWithBothVersions, "3.0.0.0", null, true)]
        [InlineData(TestAssemblyWithBothVersions, "2.1.1.1", null, false)]
        [InlineData(TestAssemblyWithNoVersions, null, null, false)] // No versions are treated as equal (so lower one wins)
        [InlineData(TestAssemblyWithNoVersions, "1.0.0.0", null, true)]
        [InlineData(TestAssemblyWithNoVersions, "1.0.0.0", "1.0.0.0", true)]
        [InlineData(TestAssemblyWithNoVersions, null, "1.0.0.0", true)]
        [InlineData(TestAssemblyWithAssemblyVersion, null, null, false)]
        [InlineData(TestAssemblyWithAssemblyVersion, "1.0.0.0", null, false)]
        [InlineData(TestAssemblyWithAssemblyVersion, null, "1.0.0.0", false)]
        [InlineData(TestAssemblyWithAssemblyVersion, "3.0.0.0", "1.0.0.0", true)]
        [InlineData(TestAssemblyWithAssemblyVersion, "2.1.1.1", null, false)]
        [InlineData(TestAssemblyWithAssemblyVersion, "2.1.1.1", "1.0.0.0", true)]
        [InlineData(TestAssemblyWithFileVersion, null, null, false)]
        [InlineData(TestAssemblyWithFileVersion, "1.0.0.0", null, true)]
        [InlineData(TestAssemblyWithFileVersion, null, "1.0.0.0", false)]
        [InlineData(TestAssemblyWithFileVersion, null, "4.0.0.0", true)]
        [InlineData(TestAssemblyWithFileVersion, null, "3.2.2.2", false)]
        public void AppWithSameAssemblyAsFramework(string testAssemblyName, string appAsmVersion, string appFileVersion, bool appWins)
        {
            RunTest(testAssemblyName, appAsmVersion, appFileVersion, appWins);
        }

        protected abstract void RunTest(string testAssemblyName, string appAsmVersion, string appFileVersion, bool appWins);

        public class SharedTestState : ComponentSharedTestStateBase
        {
            public SharedTestState()
            {
            }

            protected override void CustomizeDotNetWithNetCoreAppMicrosoftNETCoreApp(NetCoreAppBuilder builder)
            {
                builder
                    .WithPackage(TestVersionsPackage, "1.0.0", b => b
                        .WithAssemblyGroup(null, g => g
                            .WithAsset(TestAssemblyWithNoVersions + ".dll")
                            .WithAsset(TestAssemblyWithAssemblyVersion + ".dll", rf => rf.WithVersion("2.1.1.1", null))
                            .WithAsset(TestAssemblyWithFileVersion + ".dll", rf => rf.WithVersion(null, "3.2.2.2"))
                            .WithAsset(TestAssemblyWithBothVersions + ".dll", rf => rf.WithVersion("2.1.1.1", "3.2.2.2"))));
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

    public class AppPerAssemblyVersionResolution :
        PerAssemblyVersionResolutionBase,
        IClassFixture<PerAssemblyVersionResolutionBase.SharedTestState>
    {
        public AppPerAssemblyVersionResolution(SharedTestState sharedState)
            : base(sharedState)
        {
        }

        protected override void RunTest(string testAssemblyName, string appAsmVersion, string appFileVersion, bool appWins)
        {
            var app = SharedState.CreateTestFrameworkReferenceApp(b => b
                .WithPackage(TestVersionsPackage, "1.0.0", lib => lib
                    .WithAssemblyGroup(null, g => g
                        .WithAsset(testAssemblyName + ".dll", rf => rf
                            .WithVersion(appAsmVersion, appFileVersion)))));

            string expectedTestAssemblyPath =
                Path.Combine(appWins ? app.Location : SharedState.DotNetWithNetCoreApp.GreatestVersionSharedFxPath, testAssemblyName + ".dll");

            SharedState.DotNetWithNetCoreApp.Exec(app.AppDll)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveResolvedAssembly(expectedTestAssemblyPath)
                .And.HaveUsedFrameworkProbe(SharedState.DotNetWithNetCoreApp.GreatestVersionSharedFxPath, level: 1);
        }
    }

    public class AdditionalDepsPerAssemblyVersionResolution :
        PerAssemblyVersionResolutionBase,
        IClassFixture<PerAssemblyVersionResolutionBase.SharedTestState>
    {
        public AdditionalDepsPerAssemblyVersionResolution(SharedTestState sharedState)
            : base(sharedState)
        {
        }

        protected override void RunTest(string testAssemblyName, string appAsmVersion, string appFileVersion, bool appWins)
        {
            using (TestApp additionalDependency = TestApp.CreateEmpty("additionalDeps"))
            {
                // Additional deps are treated as part of app dependencies.
                // The result for whether the app wins should be the same whether the dependency is
                // specified via additional deps or by the app itself.
                NetCoreAppBuilder builder = NetCoreAppBuilder.PortableForNETCoreApp(additionalDependency)
                    .WithPackage(TestVersionsPackage, "1.0.0", lib => lib
                        .WithAssemblyGroup(null, g => g
                            .WithAsset(testAssemblyName + ".dll", rf => rf
                                .WithVersion(appAsmVersion, appFileVersion))));
                builder.Build(additionalDependency);

                TestApp app = SharedState.FrameworkReferenceApp.Copy();
                string appTestAssemblyPath = Path.Combine(app.Location, $"{testAssemblyName}.dll");
                File.WriteAllText(Path.Combine(app.Location, $"{testAssemblyName}.dll"), null);

                string expectedTestAssemblyPath = appWins
                    ? appTestAssemblyPath
                    : Path.Combine(SharedState.DotNetWithNetCoreApp.GreatestVersionSharedFxPath, $"{testAssemblyName}.dll");
                SharedState.DotNetWithNetCoreApp.Exec(Constants.AdditionalDeps.CommandLineArgument, additionalDependency.DepsJson, app.AppDll)
                    .EnableTracingAndCaptureOutputs()
                    .Execute()
                    .Should().Pass()
                    .And.HaveUsedAdditionalDeps(additionalDependency.DepsJson)
                    .And.HaveResolvedAssembly(expectedTestAssemblyPath)
                    .And.HaveUsedFrameworkProbe(SharedState.DotNetWithNetCoreApp.GreatestVersionSharedFxPath, level: 1);
            }
        }
    }

    public class ComponentPerAssemblyVersionResolution :
        PerAssemblyVersionResolutionBase,
        IClassFixture<PerAssemblyVersionResolutionBase.SharedTestState>
    {
        public ComponentPerAssemblyVersionResolution(SharedTestState sharedState)
            : base(sharedState)
        {
        }

        protected override void RunTest(string testAssemblyName, string appAsmVersion, string appFileVersion, bool appWins)
        {
            var component = SharedState.CreateComponentWithNoDependencies(b => b
                .WithPackage(TestVersionsPackage, "1.0.0", lib => lib
                    .WithAssemblyGroup(null, g => g
                        .WithAsset(testAssemblyName + ".dll", rf => rf
                            .WithVersion(appAsmVersion, appFileVersion)))));

            // For component dependency resolution, frameworks are not considered, so the assembly from the component always wins
            string expectedTestAssemblyPath = Path.Combine(component.Location, testAssemblyName + ".dll");

            SharedState.RunComponentResolutionTest(component)
                .Should().Pass()
                .And.HaveSuccessfullyResolvedComponentDependencies()
                .And.HaveResolvedComponentDependencyAssembly($"{component.AppDll};{expectedTestAssemblyPath}");
        }
    }
}

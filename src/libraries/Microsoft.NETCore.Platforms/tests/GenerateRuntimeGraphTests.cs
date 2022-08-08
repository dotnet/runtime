// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.RuntimeModel;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.NETCore.Platforms.BuildTasks.Tests
{
    // MSBuild engine is not compatible with single file
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
    public class GenerateRuntimeGraphTests
    {
        private Log _log;
        private TestBuildEngine _engine;

        private string defaultRootPath = (PlatformDetection.IsiOS || PlatformDetection.IstvOS) ? Path.GetTempPath() : string.Empty;
        private string defaultRuntimeFile = "runtime.json";

        public GenerateRuntimeGraphTests(ITestOutputHelper output)
        {
            _log = new Log(output);
            _engine = new TestBuildEngine(_log);

            if (PlatformDetection.IsiOS || PlatformDetection.IstvOS)
            {
                var runtimeJsonPath = Path.Combine(defaultRootPath, defaultRuntimeFile);
                File.Copy(defaultRuntimeFile, runtimeJsonPath, true);

                defaultRuntimeFile = runtimeJsonPath;
            }
        }

        private static ITaskItem[] DefaultRuntimeGroupItems { get; } = GetDefaultRuntimeGroupItems();

        private static ITaskItem[] GetDefaultRuntimeGroupItems()
        {
            Project runtimeGroupProps = new Project("runtimeGroups.props");

            ITaskItem[] runtimeGroups = runtimeGroupProps.GetItems("RuntimeGroupWithQualifiers")
                                                 .Select(i => CreateItem(i)).ToArray();

            Assert.NotEmpty(runtimeGroups);

            return runtimeGroups;
        }

        private static ITaskItem CreateItem(ProjectItem projectItem)
        {
            TaskItem item = new TaskItem(projectItem.EvaluatedInclude);
            foreach (var metadatum in projectItem.Metadata)
            {
                item.SetMetadata(metadatum.Name, metadatum.EvaluatedValue);
            }
            return item;
        }

        [Fact]
        public void CanCreateRuntimeGraph()
        {
            // will generate and compare to existing file.
            GenerateRuntimeGraph task = new GenerateRuntimeGraph()
            {
                BuildEngine = _engine,
                RuntimeGroups = DefaultRuntimeGroupItems,
                RuntimeJson = defaultRuntimeFile,
                UpdateRuntimeFiles = false
            };
            task.Execute();

            _log.AssertNoErrorsOrWarnings();
        }


        [Fact]
        public void CanIgnoreExistingInferRids()
        {
            // will generate and compare to existing file.
            GenerateRuntimeGraph task = new GenerateRuntimeGraph()
            {
                BuildEngine = _engine,
                RuntimeGroups = DefaultRuntimeGroupItems,
                RuntimeJson = defaultRuntimeFile,
                AdditionalRuntimeIdentifiers = new[] { "rhel.9-x64", "centos.9-arm64", "win-x64" },
                UpdateRuntimeFiles = false
            };

            _log.Reset();
            task.Execute();
            _log.AssertNoErrorsOrWarnings();
        }

        /// <summary>
        /// Runs GenerateRuntimeGraph task specifying AdditionalRuntimeIdentifiers then asserts that the
        /// generated runtime.json has the expected additions (and no more).
        /// </summary>
        /// <param name="additionalRIDs">additional RIDs</param>
        /// <param name="expectedAdditions">entries that are expected to be added to the RuntimeGraph</param>
        /// <param name="additionalRIDParent">parent to use when adding a new RID</param>
        /// <param name="runtimeFilePrefix">a unique prefix to use for the generated </param>
        private void AssertRuntimeGraphAdditions(string[] additionalRIDs, RuntimeDescription[] expectedAdditions, string additionalRIDParent = null, [CallerMemberName] string runtimeFilePrefix = null)
        {
            string runtimeFile = Path.Combine(defaultRootPath, runtimeFilePrefix + ".runtime.json");

            GenerateRuntimeGraph task = new GenerateRuntimeGraph()
            {
                BuildEngine = _engine,
                RuntimeGroups = DefaultRuntimeGroupItems,
                RuntimeJson = runtimeFile,
                AdditionalRuntimeIdentifiers = additionalRIDs,
                AdditionalRuntimeIdentifierParent = additionalRIDParent,
                UpdateRuntimeFiles = true
            };

            _log.Reset();
            task.Execute();
            _log.AssertNoErrorsOrWarnings();

            RuntimeGraph expected = RuntimeGraph.Merge(
                JsonRuntimeFormat.ReadRuntimeGraph(defaultRuntimeFile),
                new RuntimeGraph(expectedAdditions));

            RuntimeGraph actual = JsonRuntimeFormat.ReadRuntimeGraph(runtimeFile);

            // Should this assert fail, it's helpful to diff defaultRuntimeFile and runtimeFile to see the additions.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanAddVersionsToExistingGroups()
        {
            var additionalRIDs = new[] { "ubuntu.22.04-arm64" };
            var expectedAdditions = new[]
            {
                new RuntimeDescription("ubuntu.22.04", new[] { "ubuntu" }),
                new RuntimeDescription("ubuntu.22.04-x64", new[] { "ubuntu.22.04", "ubuntu-x64" }),
                new RuntimeDescription("ubuntu.22.04-x86", new[] { "ubuntu.22.04", "ubuntu-x86" }),
                new RuntimeDescription("ubuntu.22.04-arm", new[] { "ubuntu.22.04", "ubuntu-arm" }),
                new RuntimeDescription("ubuntu.22.04-arm64", new[] { "ubuntu.22.04", "ubuntu-arm64" })
            };

            AssertRuntimeGraphAdditions(additionalRIDs, expectedAdditions);
        }

        [Fact]
        public void CanAddParentVersionsToExistingGroups()
        {
            var additionalRIDs = new[] { "centos.9.2-arm64" };
            var expectedAdditions = new[]
            {
                new RuntimeDescription("centos.9.2", new[] { "centos", "rhel.9.2" }),
                new RuntimeDescription("centos.9.2-x64", new[] { "centos.9.2", "centos-x64", "rhel.9.2-x64" }),
                new RuntimeDescription("centos.9.2-arm64", new[] { "centos.9.2", "centos-arm64", "rhel.9.2-arm64" }),

                // rhel RIDs are implicitly created since centos imports versioned RHEL RIDs
                new RuntimeDescription("rhel.9.2", new[] { "rhel.9" }),
                new RuntimeDescription("rhel.9.2-x64", new[] { "rhel.9.2", "rhel.9-x64" }),
                new RuntimeDescription("rhel.9.2-arm64", new[] { "rhel.9.2", "rhel.9-arm64" })
            };

            AssertRuntimeGraphAdditions(additionalRIDs, expectedAdditions);
        }

        [Fact]
        public void CanAddMajorVersionsToExistingGroups()
        {

            var additionalRIDs = new[] { "rhel.10-x64" };
            var expectedAdditions = new[]
            {
                // Note that rhel doesn't treat major versions as compatible, however we do since it's closest and we don't represent this policy in the RuntimeGroups explicitly.
                // We could add a rule that wouldn't insert a new major version if we see existing groups are split by major version.
                new RuntimeDescription("rhel.10", new[] { "rhel.9" }),
                new RuntimeDescription("rhel.10-x64", new[] { "rhel.10", "rhel.9-x64" }),
                new RuntimeDescription("rhel.10-arm64", new[] { "rhel.10", "rhel.9-arm64" })
            };

            AssertRuntimeGraphAdditions(additionalRIDs, expectedAdditions);
        }

        [Fact]
        public void CanAddArchitectureToExistingGroups()
        {
            var additionalRIDs = new[] { "win10-x128" };
            var expectedAdditions = new[]
            {
                new RuntimeDescription("win10-x128", new[] { "win10", "win81-x128" }),
                new RuntimeDescription("win10-x128-aot", new[] { "win10-aot", "win10-x128", "win10", "win81-x128-aot" }),
                new RuntimeDescription("win81-x128-aot", new[] { "win81-aot", "win81-x128", "win81", "win8-x128-aot" }),
                new RuntimeDescription("win81-x128", new[] { "win81", "win8-x128" }),
                new RuntimeDescription("win8-x128-aot", new[] { "win8-aot", "win8-x128", "win8", "win7-x128-aot" }),
                new RuntimeDescription("win8-x128", new[] { "win8", "win7-x128" }),
                new RuntimeDescription("win7-x128-aot", new[] { "win7-aot", "win7-x128", "win7", "win-x128-aot" }),
                new RuntimeDescription("win7-x128", new[] { "win7", "win-x128" }),
                new RuntimeDescription("win-x128-aot", new[] { "win-aot", "win-x128" }),
                new RuntimeDescription("win-x128", new[] { "win" })
            };

            AssertRuntimeGraphAdditions(additionalRIDs, expectedAdditions);
        }


        [Fact]
        public void CanAddArchitectureAndVersionToExistingGroups()
        {
            var additionalRIDs = new[] { "osx.12-powerpc" };
            var expectedAdditions = new[]
            {
                new RuntimeDescription("osx.12-powerpc", new[] { "osx.12", "osx.11.0-powerpc" }),
                new RuntimeDescription("osx.12-arm64", new[] { "osx.12", "osx.11.0-arm64" }),
                new RuntimeDescription("osx.12-x64", new[] { "osx.12", "osx.11.0-x64" }),
                new RuntimeDescription("osx.12", new[] { "osx.11.0" }),
                // our RID model doesn't give priority to architecture, so the new architecture is applied to all past versions
                new RuntimeDescription("osx.11.0-powerpc", new[] { "osx.11.0", "osx.10.16-powerpc" }),
                new RuntimeDescription("osx.10.16-powerpc", new[] { "osx.10.16", "osx.10.15-powerpc" }),
                new RuntimeDescription("osx.10.15-powerpc", new[] { "osx.10.15", "osx.10.14-powerpc" }),
                new RuntimeDescription("osx.10.14-powerpc", new[] { "osx.10.14", "osx.10.13-powerpc" }),
                new RuntimeDescription("osx.10.13-powerpc", new[] { "osx.10.13", "osx.10.12-powerpc" }),
                new RuntimeDescription("osx.10.12-powerpc", new[] { "osx.10.12", "osx.10.11-powerpc" }),
                new RuntimeDescription("osx.10.11-powerpc", new[] { "osx.10.11", "osx.10.10-powerpc" }),
                new RuntimeDescription("osx.10.10-powerpc", new[] { "osx.10.10", "osx-powerpc" }),
                new RuntimeDescription("unix-powerpc", new[] { "unix" }),
                new RuntimeDescription("osx-powerpc", new[] { "osx", "unix-powerpc" }),
            };

            AssertRuntimeGraphAdditions(additionalRIDs, expectedAdditions);
        }

        [Fact]
        public void CanAddNewGroups()
        {
            var additionalRIDs = new[] { "yolinux.42.0-quantum" };
            var expectedAdditions = new[]
            {
                new RuntimeDescription("unix-quantum", new[] { "unix" }),
                new RuntimeDescription("linux-quantum", new[] { "linux", "unix-quantum" }),
                new RuntimeDescription("linux-musl-quantum", new[] { "linux-musl", "linux-quantum" }),
                new RuntimeDescription("yolinux", new[] { "linux-musl" }),
                new RuntimeDescription("yolinux-quantum", new[] { "yolinux", "linux-musl-quantum" }),
                new RuntimeDescription("yolinux.42.0", new[] { "yolinux" }),
                new RuntimeDescription("yolinux.42.0-quantum", new[] { "yolinux.42.0", "yolinux-quantum" })
            };

            AssertRuntimeGraphAdditions(additionalRIDs, expectedAdditions, "linux-musl");
        }

    }
}

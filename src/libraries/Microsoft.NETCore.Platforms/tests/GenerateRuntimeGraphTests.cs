// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.NETCore.Platforms.BuildTasks.Tests
{
    public class GenerateRuntimeGraphTests
    {
        private Log _log;
        private TestBuildEngine _engine;

        public GenerateRuntimeGraphTests(ITestOutputHelper output)
        {
            _log = new Log(output);
            _engine = new TestBuildEngine(_log);
        }

        [Fact]
        public void CanCreateRuntimeGraph()
        {
            string runtimeFile = "runtime.json";

            Project runtimeGroupProps = new Project("runtimeGroups.props");

            ITaskItem[] runtimeGroups = runtimeGroupProps.GetItems("RuntimeGroupWithQualifiers")
                                                 .Select(i => CreateItem(i)).ToArray();

            Assert.NotEmpty(runtimeGroups);

            // will generate and compare to existing file.
            GenerateRuntimeGraph task = new GenerateRuntimeGraph()
            {
                BuildEngine = _engine,
                RuntimeGroups = runtimeGroups,
                RuntimeJson = runtimeFile
            };
            task.Execute();

            _log.AssertNoErrorsOrWarnings();
        }

        private static ITaskItem CreateItem(ProjectItem projectItem)
        {
            TaskItem item = new TaskItem(projectItem.EvaluatedInclude);
            foreach(var metadatum in projectItem.Metadata)
            {
                item.SetMetadata(metadatum.Name, metadatum.EvaluatedValue);
            }
            return item;
        }
    }
}

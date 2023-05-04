// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Moq;
using Xunit;

namespace Microsoft.NET.Sdk.WebAssembly.Tests;

public class ComputeBuildAssetsTest
{
    [Fact]
    public void Execute_FixesReferencesTo()
    {
        // Arrange
        var taskInstance = new ComputeWasmBuildAssets
        {
            Candidates = new[]
            {
                new TaskItem(Path.Combine("x:", "refassembly", "file.dll"), new Dictionary<string, string>()
                {
                    ["OriginalItemSpec"] = Path.Combine("x:", "MyRefProject", "bin", "Debug", "net6.0", "MyRefProject.dll"),
                    ["ReferenceSourceTarget"] = "ProjectReference",
                }),
                new TaskItem(Path.Combine("x:", "MyRefProject", "bin", "Debug", "net6.0", "MyRefProject.dll"), new Dictionary<string, string>()
                {
                    ["OriginalItemSpec"] = Path.Combine("x:", "MyRefProject", "bin", "Debug", "net6.0", "MyRefProject.dll"),
                    ["ReferenceSourceTarget"] = "ProjectReference",
                }),
            },
            ProjectAssembly = new[]
            {
                new TaskItem(Path.Combine("x:", "MyProject", "bin", "Debug", "MyProject.dll"), new Dictionary<string, object>
                {
                    ["OriginalItemSpec"] = Path.Combine("x:", "MyProject", "bin", "Debug", "MyProject.dll"),
                })
            },
            ProjectDebugSymbols = new[]
            {
                new TaskItem(Path.Combine("x:", "MyProject", "bin", "Debug", "MyProject.pdb"), new Dictionary<string, object>
                {
                    ["OriginalItemSpec"] = Path.Combine("x:", "MyProject", "bin", "Debug", "MyProject.pdb"),
                })
            },
            SatelliteAssemblies = Array.Empty<ITaskItem>(),
            ProjectSatelliteAssemblies = Array.Empty<ITaskItem>(),
            BuildEngine = Mock.Of<IBuildEngine>(),
        };

        // Act
        taskInstance.Execute();

        // Assert
        Assert.Collection(
            taskInstance.AssetCandidates,
            item =>
            {
                Assert.Equal(Path.Combine("x:", "refassembly", "file.dll"), item.ItemSpec);
                Assert.Equal(Path.Combine("x:", "refassembly", "file.dll"), item.GetMetadata("OriginalItemSpec"));
            },
            item =>
            {
                Assert.Equal(Path.Combine("x:", "MyRefProject", "bin", "Debug", "net6.0", "MyRefProject.dll"), item.ItemSpec);
                Assert.Equal(Path.Combine("x:", "MyRefProject", "bin", "Debug", "net6.0", "MyRefProject.dll"), item.GetMetadata("OriginalItemSpec"));
            },
            item =>
            {
                Assert.Equal(Path.Combine("x:", "MyProject", "bin", "Debug", "MyProject.dll"), item.ItemSpec);
                Assert.Equal(Path.Combine("x:", "MyProject", "bin", "Debug", "MyProject.dll"), item.GetMetadata("OriginalItemSpec"));
            },
            item =>
            {
                Assert.Equal(Path.Combine("x:", "MyProject", "bin", "Debug", "MyProject.pdb"), item.ItemSpec);
                Assert.Empty(item.GetMetadata("OriginalItemSpec"));
            });
    }
}

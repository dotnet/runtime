// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection.PortableExecutable;
using ILCompiler.Win32Resources;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel;
using Xunit;

namespace AppHost.Bundle.Tests;

public class ResourceUpdaterTests
{
    RepoDirectoriesProvider RepoDirectories = new RepoDirectoriesProvider();

    [Fact]
    void ResourceUpdaterAddResource()
    {
        using var memoryStream = new MemoryStream(File.ReadAllBytes(Path.Combine(RepoDirectories.HostArtifacts, "comhost.dll")));

        using (var updater = new ResourceUpdater(memoryStream, true))
        {
            updater.AddResource("Test Resource"u8.ToArray(), "testType", 0);
            updater.Update();
        }

        memoryStream.Seek(0, SeekOrigin.Begin);

        using (var reader = new PEReader(memoryStream, PEStreamOptions.LeaveOpen))
        {
            var resourceReader = new ResourceData(reader);
            byte[]? testType = resourceReader.FindResource(0, "testType", 0);
            Assert.Equal("Test Resource"u8.ToArray(), testType);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
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
        var peBuilder = new ManagedPEBuilder(
            PEHeaderBuilder.CreateExecutableHeader(),
            new MetadataRootBuilder(new MetadataBuilder()),
            ilStream: new BlobBuilder());
        var peImageBuilder = new BlobBuilder();
        peBuilder.Serialize(peImageBuilder);
        using var memoryStream = new MemoryStream(peImageBuilder.ToArray());

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

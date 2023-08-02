// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.NET.HostModel;
using Microsoft.NET.HostModel.Win32Resources;
using Xunit;

namespace Microsoft.NET.HostModel.Tests;

public class ResourceUpdaterTests
{
    private MemoryStream CreateTestPEFileWithoutRsrc()
    {
        var peBuilder = new ManagedPEBuilder(
            PEHeaderBuilder.CreateExecutableHeader(),
            new MetadataRootBuilder(new MetadataBuilder()),
            ilStream: new BlobBuilder());
        var peImageBuilder = new BlobBuilder();
        peBuilder.Serialize(peImageBuilder);
        var memoryStream = new MemoryStream();
        memoryStream.Write(peImageBuilder.ToArray());
        memoryStream.Seek(0, SeekOrigin.Begin);

        memoryStream.Seek(0, SeekOrigin.Begin);

        return memoryStream;
    }

    private MemoryStream GetCurrentAssemblyMemoryStream()
    {
        var memoryStream = new MemoryStream();
        memoryStream.Write(File.ReadAllBytes(Assembly.GetExecutingAssembly().Location));
        memoryStream.Seek(0, SeekOrigin.Begin);

        memoryStream.Seek(0, SeekOrigin.Begin);

        return memoryStream;
    }

    [Fact]
    void ResourceUpdaterAddResourceToPEWithoutRsrc()
    {
        using var memoryStream = CreateTestPEFileWithoutRsrc();

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

    [Fact]
    void ResourceUpdaterAddResourceToExistingRsrc()
    {
        using var memoryStream = GetCurrentAssemblyMemoryStream();

        using (var updater = new ResourceUpdater(memoryStream, true))
        {
            updater.AddResource("OtherResource"u8.ToArray(), "testType2", 0);
            updater.Update();
        }

        memoryStream.Seek(0, SeekOrigin.Begin);

        using (var reader = new PEReader(memoryStream, PEStreamOptions.LeaveOpen))
        {
            var resourceReader = new ResourceData(reader);
            byte[]? testType = resourceReader.FindResource(0, "testType2", 0);
            Assert.Equal("OtherResource"u8.ToArray(), testType);
        }
    }
}

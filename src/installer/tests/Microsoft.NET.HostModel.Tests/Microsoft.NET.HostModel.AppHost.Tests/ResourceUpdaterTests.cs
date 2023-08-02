// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

        return memoryStream;
    }

    private MemoryStream GetCurrentAssemblyMemoryStream()
    {
        var memoryStream = new MemoryStream();
        memoryStream.Write(File.ReadAllBytes(Assembly.GetExecutingAssembly().Location));
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

    [Fact]
    void ResourceUpdaterAddResourceIdType()
    {
        using var memoryStream = GetCurrentAssemblyMemoryStream();
        const ushort IdTestType = 100;

        using (var updater = new ResourceUpdater(memoryStream, true))
        {
            updater.AddResource("OtherResource"u8.ToArray(), IdTestType, 0);
            updater.Update();
        }

        memoryStream.Seek(0, SeekOrigin.Begin);

        using (var reader = new PEReader(memoryStream, PEStreamOptions.LeaveOpen))
        {
            var resourceReader = new ResourceData(reader);
            byte[]? testType = resourceReader.FindResource(0, IdTestType, 0);
            Assert.Equal("OtherResource"u8.ToArray(), testType);
        }
    }

    [Fact]
    void ResourceUpdaterAddResourceTwo()
    {
        using var memoryStream = GetCurrentAssemblyMemoryStream();

        using (var updater = new ResourceUpdater(memoryStream, true))
        {
            updater.AddResource("Test Resource"u8.ToArray(), "testType", 0);
            updater.AddResource("Other Resource"u8.ToArray(), "testType", 1);
            updater.Update();
        }

        memoryStream.Seek(0, SeekOrigin.Begin);

        using (var reader = new PEReader(memoryStream, PEStreamOptions.LeaveOpen))
        {
            var resourceReader = new ResourceData(reader);
            byte[]? name0 = resourceReader.FindResource(0, "testType", 0);
            byte[]? name1 = resourceReader.FindResource(1, "testType", 0);
            Assert.Equal("Test Resource"u8.ToArray(), name0);
            Assert.Equal("Other Resource"u8.ToArray(), name1);
        }
    }

    [Fact]
    void AddResourcesFromPEImage()
    {
        using var memoryStream = GetCurrentAssemblyMemoryStream();

        using (var updater = new ResourceUpdater(memoryStream, true))
        {
            updater.AddResourcesFromPEImage(Assembly.GetExecutingAssembly().Location);
            updater.Update();
        }

        memoryStream.Seek(0, SeekOrigin.Begin);

        using (var modified = new PEReader(memoryStream, PEStreamOptions.LeaveOpen))
        using (var assembly = new PEReader(File.Open(Assembly.GetExecutingAssembly().Location, FileMode.Open)))
        {
            var modifiedReader = new ResourceData(modified);
            var assemblyReader = new ResourceData(assembly);
            foreach ((object nameObj, object typeObj, ushort language, byte[] data) in assemblyReader.GetAllResources())
            {
                byte[]? found;
                switch (nameObj, typeObj)
                {
                    case (ushort name, ushort type):
                        found = modifiedReader.FindResource(name, type, language);
                        break;
                    case (ushort name, string type):
                        found = modifiedReader.FindResource(name, type, language);
                        break;
                    case (string name, ushort type):
                        found = modifiedReader.FindResource(name, type, language);
                        break;
                    case (string name, string type):
                        found = modifiedReader.FindResource(name, type, language);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                Assert.Equal(data, found);
            }
        }
    }
}

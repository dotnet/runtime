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
    class TempFile : IDisposable
    {
        public readonly FileStream Stream;
        private readonly string _path;

        public TempFile()
        {
            _path = Path.GetTempFileName();
            Stream = new FileStream(_path, FileMode.Open);
        }

        public void Dispose()
        {
            Stream.Close();
            File.Delete(_path);
        }
    }

    private TempFile CreateTestPEFileWithoutRsrc()
    {
        var peBuilder = new ManagedPEBuilder(
            PEHeaderBuilder.CreateExecutableHeader(),
            new MetadataRootBuilder(new MetadataBuilder()),
            ilStream: new BlobBuilder());
        var peImageBuilder = new BlobBuilder();
        peBuilder.Serialize(peImageBuilder);
        var tempFile = new TempFile();
        tempFile.Stream.Write(peImageBuilder.ToArray());
        tempFile.Stream.Seek(0, SeekOrigin.Begin);

        return tempFile;
    }

    private TempFile GetCurrentAssemblyMemoryStream()
    {
        var tempFile = new TempFile();
        tempFile.Stream.Write(File.ReadAllBytes(Assembly.GetExecutingAssembly().Location));
        tempFile.Stream.Seek(0, SeekOrigin.Begin);

        return tempFile;
    }

    [Fact]
    void AddResource_AddToPEWithoutRsrc()
    {
        using var tempFile = CreateTestPEFileWithoutRsrc();

        using (var updater = new ResourceUpdater(tempFile.Stream, true))
        {
            updater.AddResource("Test Resource"u8.ToArray(), "testType", 0);
            updater.Update();
        }

        tempFile.Stream.Seek(0, SeekOrigin.Begin);

        using (var reader = new PEReader(tempFile.Stream, PEStreamOptions.LeaveOpen))
        {
            var resourceReader = new ResourceData(reader);
            byte[]? testType = resourceReader.FindResource(0, "testType", 0);
            Assert.Equal("Test Resource"u8.ToArray(), testType);
        }
    }

    [Fact]
    void AddResource_AddToExistingRsrc()
    {
        using var tempFile = GetCurrentAssemblyMemoryStream();

        using (var updater = new ResourceUpdater(tempFile.Stream, true))
        {
            updater.AddResource("OtherResource"u8.ToArray(), "testType2", 0);
            updater.Update();
        }

        tempFile.Stream.Seek(0, SeekOrigin.Begin);

        using (var reader = new PEReader(tempFile.Stream, PEStreamOptions.LeaveOpen))
        {
            var resourceReader = new ResourceData(reader);
            byte[]? testType = resourceReader.FindResource(0, "testType2", 0);
            Assert.Equal("OtherResource"u8.ToArray(), testType);
        }
    }

    [Fact]
    void AddResource_AddResourceWithIdType()
    {
        using var tempFile = GetCurrentAssemblyMemoryStream();
        const ushort IdTestType = 100;

        using (var updater = new ResourceUpdater(tempFile.Stream, true))
        {
            updater.AddResource("OtherResource"u8.ToArray(), IdTestType, 0);
            updater.Update();
        }

        tempFile.Stream.Seek(0, SeekOrigin.Begin);

        using (var reader = new PEReader(tempFile.Stream, PEStreamOptions.LeaveOpen))
        {
            var resourceReader = new ResourceData(reader);
            byte[]? testType = resourceReader.FindResource(0, IdTestType, 0);
            Assert.Equal("OtherResource"u8.ToArray(), testType);
        }
    }

    [Fact]
    void AddResource_AddTwoSameStringTypeWithDifferName()
    {
        using var tempFile = GetCurrentAssemblyMemoryStream();

        using (var updater = new ResourceUpdater(tempFile.Stream, true))
        {
            updater.AddResource("Test Resource"u8.ToArray(), "testType", 0);
            updater.AddResource("Other Resource"u8.ToArray(), "testType", 1);
            updater.Update();
        }

        tempFile.Stream.Seek(0, SeekOrigin.Begin);

        using (var reader = new PEReader(tempFile.Stream, PEStreamOptions.LeaveOpen))
        {
            var resourceReader = new ResourceData(reader);
            byte[]? name0 = resourceReader.FindResource(0, "testType", 0);
            byte[]? name1 = resourceReader.FindResource(1, "testType", 0);
            Assert.Equal("Test Resource"u8.ToArray(), name0);
            Assert.Equal("Other Resource"u8.ToArray(), name1);
        }
    }

    [Fact]
    void AddResource_AddTwoSameUShortTypeWithDifferName()
    {
        using var tempFile = GetCurrentAssemblyMemoryStream();

        using (var updater = new ResourceUpdater(tempFile.Stream, true))
        {
            updater.AddResource("Test Resource"u8.ToArray(), 11, 0);
            updater.AddResource("Other Resource"u8.ToArray(), 11, 1);
            updater.Update();
        }

        tempFile.Stream.Seek(0, SeekOrigin.Begin);

        using (var reader = new PEReader(tempFile.Stream, PEStreamOptions.LeaveOpen))
        {
            var resourceReader = new ResourceData(reader);
            byte[]? name0 = resourceReader.FindResource(0, 11, 0);
            byte[]? name1 = resourceReader.FindResource(1, 11, 0);
            Assert.Equal("Test Resource"u8.ToArray(), name0);
            Assert.Equal("Other Resource"u8.ToArray(), name1);
        }
    }

    [Fact]
    void AddResourcesFromPEImage()
    {
        using var tempFile = CreateTestPEFileWithoutRsrc();

        using (var updater = new ResourceUpdater(tempFile.Stream, true))
        {
            updater.AddResourcesFromPEImage(Assembly.GetExecutingAssembly().Location);
            updater.Update();
        }

        tempFile.Stream.Seek(0, SeekOrigin.Begin);

        using (var modified = new PEReader(tempFile.Stream, PEStreamOptions.LeaveOpen))
        using (var assembly = new PEReader(File.Open(Assembly.GetExecutingAssembly().Location, FileMode.Open, FileAccess.Read, FileShare.Read)))
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
                        found = null;
                        Assert.Fail("name or type is not string nor ushort.");
                        break;
                }
                Assert.Equal(data, found);
            }
        }
    }
}

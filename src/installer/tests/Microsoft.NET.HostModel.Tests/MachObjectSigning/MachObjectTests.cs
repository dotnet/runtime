// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.CoreSetup;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.MachO;
using Microsoft.NET.HostModel.MachO.CodeSign.Tests;
using Xunit;

namespace Microsoft.NET.HostModel.Tests;

public class MachObjectTests
{
    ITestOutputHelper output;
    public MachObjectTests(ITestOutputHelper output)
    {
        this.output = output;
    }
    [Theory]
    [MemberData(nameof(GetTestFilePaths), nameof(StreamAndMemoryMappedFileAreTheSame))]
    public void StreamAndMemoryMappedFileAreTheSame(string filePath, TestArtifact _)
    {
        MachObjectFile streamMachOFile;
        MachObjectFile memoryMappedMachOFile;
        using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            streamMachOFile = MachObjectFile.Create(new StreamBasedMachOFile(stream));

            using (MemoryMappedFile memoryMappedFile = MemoryMappedFile.CreateFromFile(stream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true))
            using (MemoryMappedViewAccessor memoryMappedViewAccessor = memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
            {
                memoryMappedMachOFile = MachObjectFile.Create(new MemoryMappedMachOViewAccessor(memoryMappedViewAccessor));
            }
        }
        MachObjectFile.AssertEquivalent(streamMachOFile, memoryMappedMachOFile);
    }


    [Theory]
    [MemberData(nameof(GetTestFilePaths), nameof(RoundTripMachObjectFileIsTheSame))]
    void RoundTripMachObjectFileIsTheSame(string filePath, TestArtifact _)
    {
        var backupFilePath = filePath + ".bak";
        File.Copy(filePath, backupFilePath);
        using (var mmap = MemoryMappedFile.CreateFromFile(filePath))
        using (var accessor = mmap.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite))
        {
            var machFile = new MemoryMappedMachOViewAccessor(accessor);
            var machObjectFile = MachObjectFile.Create(machFile);
            machObjectFile.Write(machFile);
            var rewrittenMachFile = MachObjectFile.Create(machFile);
            MachObjectFile.AssertEquivalent(machObjectFile, rewrittenMachFile);
        }
        using (FileStream original = new FileStream(backupFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (FileStream written = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Assert.Equal(original.Length, written.Length);
            byte[] originalBuffer = new byte[4096];
            byte[] writtenBuffer = new byte[4096];
            while (true)
            {
                int bytesReadOriginal = original.Read(originalBuffer, 0, originalBuffer.Length);
                int bytesReadWritten = written.Read(writtenBuffer, 0, writtenBuffer.Length);
                Assert.Equal(bytesReadOriginal, bytesReadWritten);

                if (bytesReadOriginal == 0)
                    break;

                Assert.True(originalBuffer.SequenceEqual(writtenBuffer));
            }
        }
    }

    static readonly ImmutableArray<string> liveBuiltHosts = ImmutableArray.Create(Binaries.AppHost.FilePath, Binaries.SingleFileHost.FilePath);
    public static Object[][] GetTestFilePaths(string testArtifactName)
    {
        List<object[]> arguments = [];
        List<(string Name, FileInfo File)> testData = TestData.MachObjects.GetAll().ToList();
        foreach ((string name, FileInfo file) in testData)
        {
            var testArtifact = TestArtifact.Create(testArtifactName + "-" + name);
            string newFilePath = Path.Combine(testArtifact.Location, name);
            File.Copy(file.FullName, newFilePath, true);
            arguments.Add([newFilePath, testArtifact]);
        }

        // If we're on mac, we can use the live built binaries to test against
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            foreach (var filePath in liveBuiltHosts)
            {
                string fileName = Path.GetFileName(filePath);
                var testArtifact = TestArtifact.Create(testArtifactName + "-" + fileName);
                string testFilePath = Path.Combine(testArtifact.Location, fileName);
                File.Copy(filePath, testFilePath);
                arguments.Add([testFilePath, testArtifact]);
            }
        }

        return arguments.ToArray();
    }

    [Fact]
    public void CanParseCodesignOutput()
    {
        var parsed = CodesignOutputInfo.ParseFromCodeSignOutput(CodesignOutputInfo.SampleCodesignOutput);
        Assert.NotNull(parsed);
        output.WriteLine(parsed.ToString());
        var expected = new CodesignOutputInfo
        {
            Identifier = "singlefilehost-5555494409d4df688bf436b291061028f736b11c",
            CodeDirectoryFlags = CodeDirectoryFlags.Adhoc,
            CodeDirectoryVersion = CodeDirectoryVersion.SupportsExecSegment,
            ExecutableSegmentBase = 0,
            ExecutableSegmentLimit = 8949760,
            ExecutableSegmentFlags = ExecutableSegmentFlags.MainBinary,
            SpecialSlotHashes = [
                    [0x4d, 0x8d, 0x4b, 0x9e, 0x41, 0x16, 0xe8, 0xed, 0xd9, 0x96, 0x17, 0x6b, 0x55, 0x53, 0x46, 0x3a, 0xcb, 0x64, 0x28, 0x7b, 0xb6, 0x35, 0xe7, 0xf1, 0x41, 0x15, 0x55, 0x29, 0xe2, 0x04, 0x57, 0xbc],
                    [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
                    [0xcc, 0xa8, 0xaf, 0xe7, 0x24, 0x25, 0x46, 0x3c, 0x13, 0xb8, 0x13, 0xda, 0x9a, 0xe4, 0x68, 0xae, 0x3b, 0x5f, 0xe2, 0x0f, 0xe5, 0xfe, 0x1d, 0x3f, 0x34, 0x30, 0x2b, 0xa2, 0xf1, 0x57, 0x22, 0xf2],
                    [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
                    [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
                    [0x98, 0x79, 0x20, 0x90, 0x4e, 0xab, 0x65, 0x0e, 0x75, 0x78, 0x8c, 0x05, 0x4a, 0xa0, 0xb0, 0x52, 0x4e, 0x6a, 0x80, 0xbf, 0xc7, 0x1a, 0xa3, 0x2d, 0xf8, 0xd2, 0x37, 0xa6, 0x17, 0x43, 0xf9, 0x86],
                    [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]
                ],
            CodeHashes = [
                    [0x20, 0x04, 0x29, 0x93, 0x66, 0x56, 0x11, 0xbf, 0x5d, 0x01, 0xd3, 0x5a, 0x46, 0x09, 0x2c, 0x2d, 0x43, 0xa0, 0x78, 0x83, 0xf3, 0x12, 0x47, 0xa0, 0x3b, 0x56, 0x00, 0xc3, 0x01, 0xf5, 0xc0, 0x39],
                    [0xa9, 0x7f, 0xad, 0x07, 0xcc, 0x9d, 0x6e, 0xab, 0xad, 0x27, 0xa7, 0x7e, 0x32, 0xb6, 0x9c, 0x3d, 0xa5, 0x93, 0x72, 0xfa, 0x79, 0x87, 0xa1, 0x3c, 0x2b, 0x8d, 0x23, 0xf3, 0x78, 0x38, 0x04, 0x76],
                    [0xad, 0x7f, 0xac, 0xb2, 0x58, 0x6f, 0xc6, 0xe9, 0x66, 0xc0, 0x04, 0xd7, 0xd1, 0xd1, 0x6b, 0x02, 0x4f, 0x58, 0x05, 0xff, 0x7c, 0xb4, 0x7c, 0x7a, 0x85, 0xda, 0xbd, 0x8b, 0x48, 0x89, 0x2c, 0xa7],
                    [0xad, 0x7f, 0xac, 0xb2, 0x58, 0x6f, 0xc6, 0xe9, 0x66, 0xc0, 0x04, 0xd7, 0xd1, 0xd1, 0x6b, 0x02, 0x4f, 0x58, 0x05, 0xff, 0x7c, 0xb4, 0x7c, 0x7a, 0x85, 0xda, 0xbd, 0x8b, 0x48, 0x89, 0x2c, 0xa7],
                    [0xad, 0x7f, 0xac, 0xb2, 0x58, 0x6f, 0xc6, 0xe9, 0x66, 0xc0, 0x04, 0xd7, 0xd1, 0xd1, 0x6b, 0x02, 0x4f, 0x58, 0x05, 0xff, 0x7c, 0xb4, 0x7c, 0x7a, 0x85, 0xda, 0xbd, 0x8b, 0x48, 0x89, 0x2c, 0xa7],
                    [0xb3, 0xd2, 0x30, 0x34, 0x0a, 0xa5, 0xed, 0x09, 0xc7, 0x88, 0xc3, 0x90, 0x81, 0xc2, 0x07, 0xa7, 0x43, 0x0b, 0x83, 0xd2, 0x2c, 0x94, 0x89, 0xd8, 0x4d, 0x4e, 0xde, 0x3e, 0xd3, 0x20, 0xf4, 0x7b],
                    [0x82, 0x5b, 0x7a, 0xa1, 0x61, 0x70, 0xa9, 0xb7, 0x39, 0xa4, 0x68, 0x9b, 0xa8, 0x87, 0x83, 0x91, 0xbc, 0xae, 0x87, 0xef, 0xd6, 0x3e, 0x3b, 0x17, 0x47, 0x38, 0xc3, 0x82, 0x02, 0x00, 0x31, 0xc1],
                    [0xe3, 0x60, 0x15, 0x9e, 0xe0, 0xad, 0xae, 0xba, 0x5a, 0xc5, 0xf5, 0x62, 0xc4, 0x5e, 0xc5, 0x51, 0xdb, 0xe8, 0xb7, 0x3f, 0xbc, 0x85, 0x8b, 0xec, 0xa2, 0x98, 0x61, 0x03, 0x12, 0xdf, 0x33, 0xb3],
                    [0x20, 0x58, 0x5e, 0xf0, 0xbc, 0x02, 0x87, 0xc5, 0xb7, 0xa9, 0xb5, 0x4f, 0x26, 0x69, 0x70, 0x4c, 0xdc, 0x31, 0xce, 0xa7, 0xd7, 0xb1, 0x70, 0x2b, 0x33, 0x6f, 0xcf, 0x93, 0xa9, 0xf0, 0x1c, 0xa2],
                    [0x41, 0x4a, 0xe6, 0x56, 0x3e, 0x58, 0x81, 0xb2, 0x15, 0xa0, 0x8b, 0xb3, 0x3f, 0xc5, 0x39, 0xfb, 0x0c, 0x90, 0xc3, 0xa5, 0x53, 0x2f, 0x6e, 0x15, 0xa7, 0x26, 0xed, 0x6c, 0xdc, 0x25, 0x55, 0x50],
                    [0xb6, 0x72, 0xb6, 0x67, 0xeb, 0x31, 0xb4, 0x8d, 0x02, 0x7b, 0xd5, 0xf1, 0xcf, 0x75, 0xba, 0xd5, 0xa8, 0x55, 0x2b, 0x4d, 0x6b, 0x64, 0x9c, 0xbd, 0xae, 0x35, 0x69, 0x91, 0x52, 0xfb, 0x8a, 0x1b]
                ],
        };
        Assert.Equal(expected, parsed);
    }

    // test all the binaries compared to codesinginfo from codesign output
    [Theory]
    [PlatformSpecific(TestPlatforms.OSX)]
    [MemberData(nameof(GetTestFilePaths), nameof(EmbeddedSignatureBlobMatchesCodesignInfo))]
    public void EmbeddedSignatureBlobMatchesCodesignInfo(string filePath, TestArtifact _)
    {
        if (!SigningTests.IsSigned(filePath))
        {
            return;
        }
        MachObjectFile machObjectFile;
        EmbeddedSignatureBlob? embeddedSignatureBlob;
        using (var memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read))
        using (var memoryMappedViewAccessor = memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
        {
            machObjectFile = MachObjectFile.Create(new MemoryMappedMachOViewAccessor(memoryMappedViewAccessor));
            Assert.True(machObjectFile.HasSignature, "Expected MachObjectFile to have a signature");
            embeddedSignatureBlob = machObjectFile.EmbeddedSignatureBlob;
            Assert.NotNull(embeddedSignatureBlob);
        }

        var (exitcode, stderr) = Codesign.Run("--display --verbose=6", filePath);
#pragma warning disable CS0162
        if (exitcode != 0)
        {
            output.WriteLine($"Codesign command failed with exit code {exitcode}: {stderr}");
            Assert.Fail("Codesign command failed");
        }
        output.WriteLine($"Codesign output for {filePath}:\n{stderr}");
        CodesignOutputInfo codesignInfo = CodesignOutputInfo.ParseFromCodeSignOutput(stderr);
        output.WriteLine($"Comparing {filePath} to codesign info: {codesignInfo}");
        output.WriteLine($"specialSlotHashes: {string.Join(", ", codesignInfo.SpecialSlotHashes.Select(h => BitConverter.ToString(h).Replace("-", "")))}");
        output.WriteLine($"machObjectFile specialSlotHashes: {string.Join(", ", embeddedSignatureBlob.CodeDirectoryBlob.SpecialSlotHashes.Select(h => BitConverter.ToString(h.ToArray()).Replace("-", "")))}");
        AssertEqual(codesignInfo, embeddedSignatureBlob);
    }

    static void AssertEqual(CodesignOutputInfo csi, EmbeddedSignatureBlob b)
    {
        Assert.True(csi.Identifier == b.CodeDirectoryBlob.Identifier, "Identifiers do not match");
        Assert.True(csi.CodeDirectoryFlags == b.CodeDirectoryBlob.Flags, "CodeDirectoryFlags do not match");
        Assert.True(csi.CodeDirectoryVersion == b.CodeDirectoryBlob.Version, "CodeDirectoryVersion do not match");
        Assert.True(csi.ExecutableSegmentBase == b.CodeDirectoryBlob.ExecutableSegmentBase, "ExecutableSegmentBase do not match");
        Assert.True(csi.ExecutableSegmentLimit == b.CodeDirectoryBlob.ExecutableSegmentLimit, "ExecutableSegmentLimit do not match");
        Assert.True(csi.ExecutableSegmentFlags == b.CodeDirectoryBlob.ExecutableSegmentFlags, "ExecutableSegmentFlags do not match");

        AssertEqual(csi.SpecialSlotHashes, b.CodeDirectoryBlob.SpecialSlotHashes);
        AssertEqual(csi.CodeHashes, b.CodeDirectoryBlob.CodeHashes);

        static void AssertEqual(byte[][] hashes1, IReadOnlyList<IReadOnlyList<byte>> hashes2)
        {
            Assert.Equal(hashes1.Length, hashes2.Count);

            for (int i = 0; i < hashes1.Length; i++)
            {
                Assert.Equal(hashes1[i].Length, hashes2[i].Count);

                for (int j = 0; j < hashes1[i].Length; j++)
                {
                    Assert.Equal(hashes1[i][j], hashes2[i][j]);
                }
            }
        }
    }
}

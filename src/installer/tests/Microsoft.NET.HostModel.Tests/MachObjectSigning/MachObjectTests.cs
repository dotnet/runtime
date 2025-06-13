

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.MachO;
using Xunit;

public class MachObjectTests
{
    private readonly List<TestArtifact> _testArtifacts = new();

    [Theory]
    [MemberData(nameof(GetTestFilePaths), nameof(StreamAndMemoryMappedFileAreTheSame))]
    public void StreamAndMemoryMappedFileAreTheSame(string filePath, TestArtifact testArtifact)
    {
        using var testArtifactLocation = testArtifact;
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
    void RoundTripMachObjectFileIsTheSame(string filePath, TestArtifact testArtifact)
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

                Assert.True(originalBuffer.Take(bytesReadOriginal).SequenceEqual(writtenBuffer.Take(bytesReadWritten)));
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

    public void Dispose()
    {
        foreach (var artifact in _testArtifacts)
        {
            try
            {
                artifact.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to dispose test artifact: {ex.Message}");
            }
        }
        _testArtifacts.Clear();
    }
}

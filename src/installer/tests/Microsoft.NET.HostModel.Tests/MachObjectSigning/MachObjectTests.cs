

using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using Microsoft.NET.HostModel.MachO;
using Xunit;

public class MachObjectTests
{
    [Theory]
    [MemberData(nameof(MachObjects))]
    public void StreamAndMemoryMappedFileAreTheSame(string fileName, FileInfo file)
    {
        MachObjectFile streamMachOFile;
        MachObjectFile memoryMappedMachOFile;
        using (FileStream stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
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

    private static IEnumerable<object[]> MachObjects()
    {
        return TestData.MachObjects.GetAll()
            .Select(f => new object[] { f.Name, f.File });
    }
}

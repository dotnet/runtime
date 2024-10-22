
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal class TestData
{
    internal class MachObjects
    {
        internal static List<(string Name, Stream Data)> GetAll()
        {
            return GetRecursiveFiles(new DirectoryInfo("MachO"))
                .Select(f => (f.UniqueName, (Stream)f.File.Open(FileMode.Open, FileAccess.Read)))
                .ToList();
        }

        internal static IEnumerable<(FileInfo File, string UniqueName)> GetRecursiveFiles(DirectoryInfo dir, string prefix = "")
        {
            var files = dir.GetFiles().Select(f => (f, $"{prefix}{dir.Name}-{f.Name}"));
            var recursiveFiles = dir.GetDirectories().SelectMany(sd => GetRecursiveFiles(sd, $"{prefix}{dir.Name}-"));
            return files.Concat(recursiveFiles);
        }

        internal static (string Name, Stream Data) Get(string name)
        {
            return (name, new FileStream(Path.Combine("MachO", name), FileMode.Open, FileAccess.Read));
        }
    }
}

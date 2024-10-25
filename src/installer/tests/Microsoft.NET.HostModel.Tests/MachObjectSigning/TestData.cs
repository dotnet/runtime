
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal class TestData
{
    internal class MachObjects
    {
        internal static List<(string Name, FileInfo Data)> GetAll()
        {
            return GetRecursiveFiles(new DirectoryInfo("MachO"))
                .Select(f => (f.UniqueName, f.File))
                .ToList();
        }

        private static IEnumerable<(FileInfo File, string UniqueName)> GetRecursiveFiles(DirectoryInfo dir, string prefix = "")
        {
            var files = dir.GetFiles().Select(f => (f, $"{prefix}{dir.Name}-{f.Name}"));
            var recursiveFiles = dir.GetDirectories().SelectMany(sd => GetRecursiveFiles(sd, $"{prefix}{dir.Name}-"));
            return files.Concat(recursiveFiles);
        }

        internal static (string Name, FileInfo File) GetSingle()
        {
            var file = GetRecursiveFiles(new DirectoryInfo("MachO")).First();
            return (file.File.Name, file.File);
        }
    }
}

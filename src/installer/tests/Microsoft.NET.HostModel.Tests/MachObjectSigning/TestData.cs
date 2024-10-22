
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
            return new DirectoryInfo("MachO")
                .GetFiles()
                .Select(f => (f.Name, (Stream)f.Open(FileMode.Open, FileAccess.Read)))
                .ToList();
        }

        internal static (string Name, Stream Data) Get(string name)
        {
            return (name, new FileStream(Path.Combine("MachO", name), FileMode.Open, FileAccess.Read));
        }
    }
}

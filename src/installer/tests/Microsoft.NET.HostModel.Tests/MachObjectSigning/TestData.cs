
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

internal class TestData
{
    internal class MachObjects
    {
        internal static IEnumerable<(string Name, FileInfo File)> GetAll()
        {
            return GetRecursiveFiles(new DirectoryInfo("MachO"))
                .Select(f => (f.UniqueName, f.File));
        }

        // Gets a FileInfo and a unique name for each binary test Mach-O file.
        // Test data is located at ./MachO/<arch>/<filename> (using the clang/llvm arch names)
        // Unique name is <arch>-<filename>
        private static IEnumerable<(FileInfo File, string UniqueName)> GetRecursiveFiles(DirectoryInfo dir, string prefix = "")
        {
            var files = dir.GetFiles().Select(f => (f, $"{prefix}{dir.Name}-{f.Name}".ToLowerInvariant()));
            var recursiveFiles = dir.GetDirectories().SelectMany(sd => GetRecursiveFiles(sd, $"{prefix}{dir.Name}-"));
            return files.Concat(recursiveFiles);
        }

        static readonly string s_currentArchitectureLlvmString = RuntimeInformation.ProcessArchitecture switch {
            Architecture.X64 => "x86_64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException()
        };
        internal static IEnumerable<(string Name, FileInfo File)> GetRunnable()
        {
            return GetAll().Where(f => f.Name.Contains(s_currentArchitectureLlvmString));
        }
    }
}

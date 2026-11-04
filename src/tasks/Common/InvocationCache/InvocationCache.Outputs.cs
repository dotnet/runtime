// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.NET.Build.Tasks;

internal sealed partial class InvocationCache
{
    internal void ValidateOutputs(InvocationOutputs outputs)
    {
        StringComparison comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        string cachePrefix = CacheDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (outputs.DirectoryPath is not null)
        {
            string outputPrefix = outputs.DirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (cachePrefix.StartsWith(outputPrefix, comparison) || outputPrefix.StartsWith(cachePrefix, comparison))
                throw new ArgumentException("The cache and output directories must not contain each other.", nameof(outputs));
        }
        else
        {
            var paths = new HashSet<string>(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            foreach (string file in outputs.Files)
            {
                string filePrefix = file.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                foreach (string existing in paths)
                {
                    string existingPrefix = existing.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    if (existing.StartsWith(filePrefix, comparison) || file.StartsWith(existingPrefix, comparison))
                        throw new ArgumentException("Declared output files must not contain each other.", nameof(outputs));
                }
                if (!paths.Add(file) || file.StartsWith(cachePrefix, comparison) ||
                    cachePrefix.StartsWith(filePrefix, comparison))
                    throw new ArgumentException("Cache outputs must be distinct files outside the cache.", nameof(outputs));
            }
        }
    }

    private static List<string> GetFileNames(InvocationOutputs outputs)
    {
        var files = new List<string>();
        if (outputs.DirectoryPath is not null)
        {
            Collect(outputs.DirectoryPath, "");
            files.Sort(StringComparer.Ordinal);
        }
        else
        {
            for (int i = 0; i < outputs.Files.Length; i++)
                files.Add(i.ToString(CultureInfo.InvariantCulture));
        }
        return files;

        void Collect(string directory, string relativeDirectory)
        {
            foreach (string path in Directory.EnumerateFileSystemEntries(directory))
            {
                string relativePath = Path.Combine(relativeDirectory, Path.GetFileName(path));
                if (Directory.Exists(path))
                    Collect(path, relativePath);
                else
                    files.Add(relativePath);
            }
        }
    }

    private static string GetOutputPath(InvocationOutputs outputs, string name, int index) =>
        outputs.DirectoryPath is not null ? Path.Combine(outputs.DirectoryPath, name) : outputs.Files[index];

    private static void DeleteOutputFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (DirectoryNotFoundException)
        {
            // A missing parent already satisfies the declared file's absence.
        }
    }
}

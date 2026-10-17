// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.NET.Build.Tasks;

internal readonly struct InvocationCacheFile
{
    internal string Path { get; }
    internal bool Required { get; }

    internal InvocationCacheFile(string path, bool required = true)
    {
        Path = path;
        Required = required;
    }
}

internal sealed class InvocationCacheOutputs
{
    internal string? DirectoryPath { get; }
    private readonly InvocationCacheFile[] _files;

    private InvocationCacheOutputs(string? directoryPath, InvocationCacheFile[] files)
    {
        DirectoryPath = directoryPath;
        _files = files;
    }

    internal static InvocationCacheOutputs FromDirectory(string path) =>
        new InvocationCacheOutputs(System.IO.Path.GetFullPath(path), Array.Empty<InvocationCacheFile>());

    internal static InvocationCacheOutputs FromFiles(params InvocationCacheFile[] files)
    {
        var normalized = new InvocationCacheFile[files.Length];
        for (int i = 0; i < files.Length; i++)
            normalized[i] = new InvocationCacheFile(System.IO.Path.GetFullPath(files[i].Path), files[i].Required);

        return new InvocationCacheOutputs(null, normalized);
    }

    internal void Validate(string cacheDirectory)
    {
        StringComparison comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        string cachePrefix = cacheDirectory.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
        if (DirectoryPath is not null)
        {
            string outputPrefix = DirectoryPath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
            if (cachePrefix.StartsWith(outputPrefix, comparison) || outputPrefix.StartsWith(cachePrefix, comparison))
                throw new ArgumentException("The cache and output directories must not contain each other.", "outputDirectory");
        }
        else
        {
            var paths = new HashSet<string>(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            foreach (InvocationCacheFile file in _files)
            {
                string filePrefix = file.Path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
                foreach (string existing in paths)
                {
                    string existingPrefix = existing.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
                    if (existing.StartsWith(filePrefix, comparison) || file.Path.StartsWith(existingPrefix, comparison))
                        throw new ArgumentException("Declared output files must not contain each other.", nameof(cacheDirectory));
                }
                if (!paths.Add(file.Path) || file.Path.StartsWith(cachePrefix, comparison) ||
                    cachePrefix.StartsWith(filePrefix, comparison))
                    throw new ArgumentException("Cache outputs must be distinct files outside the cache.", nameof(cacheDirectory));
            }
        }
    }

    internal List<string> GetFileNames()
    {
        var files = new List<string>();
        if (DirectoryPath is not null)
        {
            Collect(DirectoryPath, "");
            files.Sort(StringComparer.Ordinal);
        }
        else
        {
            for (int i = 0; i < _files.Length; i++)
                files.Add(i.ToString(CultureInfo.InvariantCulture));
        }
        return files;

        void Collect(string directory, string relativeDirectory)
        {
            foreach (string path in Directory.EnumerateFileSystemEntries(directory))
            {
                string relativePath = System.IO.Path.Combine(relativeDirectory, System.IO.Path.GetFileName(path));
                if (Directory.Exists(path))
                    Collect(path, relativePath);
                else
                    files.Add(relativePath);
            }
        }
    }

    internal int FileCount => _files.Length;

    internal string GetPath(string name, int index) =>
        DirectoryPath is not null ? System.IO.Path.Combine(DirectoryPath, name) : _files[index].Path;

    internal bool IsRequired(int index) => DirectoryPath is not null || _files[index].Required;

    internal void Clean()
    {
        if (DirectoryPath is not null)
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
            Directory.CreateDirectory(DirectoryPath);
        }
        else
        {
            foreach (InvocationCacheFile file in _files)
                DeleteFile(file.Path);
        }
    }

    internal static void DeleteFile(string path)
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

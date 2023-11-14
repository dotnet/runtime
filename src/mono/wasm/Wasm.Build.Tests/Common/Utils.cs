// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;
using Xunit.Abstractions;

internal static class Utils
{
    public static void DirectoryCopy(string sourceDirName, string destDirName, Func<string, bool>? predicate=null, bool copySubDirs=true, bool silent=false, ITestOutputHelper? testOutput = null)
    {
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        DirectoryInfo[] dirs = dir.GetDirectories();

        // If the destination directory doesn't exist, create it.
        Directory.CreateDirectory(destDirName);

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string fullPath = file.ToString();
            if (predicate != null && !predicate(fullPath))
            {
                 if (!silent)
                     testOutput?.WriteLine($"Skipping {fullPath}");
                continue;
            }

            string tempPath = Path.Combine(destDirName, file.Name);
             if (!silent)
                 testOutput?.WriteLine($"Copying {fullPath} to {tempPath}");
            file.CopyTo(tempPath, false);
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, predicate, copySubDirs, silent);
            }
        }
    }

    public static string GZipCompress(string fileSelected)
    {
        FileInfo fileToCompress = new FileInfo(fileSelected);
        string compressedFileName = fileToCompress.FullName + ".gz";

        using FileStream originalFileStream = fileToCompress.OpenRead();
        using FileStream compressedFileStream = File.Create(compressedFileName);
        using GZipStream compressionStream = new(compressedFileStream, CompressionMode.Compress);
        originalFileStream.CopyTo(compressionStream);

        return compressedFileName;
    }
}

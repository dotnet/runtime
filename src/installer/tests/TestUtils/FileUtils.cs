// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public static class FileUtils
    {
        public static void EnsureFileDirectoryExists(string filePath)
        {
            EnsureDirectoryExists(Path.GetDirectoryName(filePath));
        }

        public static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static void CreateEmptyFile(string filePath)
        {
            EnsureFileDirectoryExists(filePath);
            File.WriteAllText(filePath, string.Empty);
        }

        public static void DeleteFileIfPossible(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (System.IO.IOException)
            {
            }
        }

        /// <summary>
        /// Copies a file into a directory.
        ///
        /// This is a drop-in replacement for File.Copy usages that rely on non-Windows platforms
        /// allowing a directory as a target path. This behavior was corrected in CoreFX:
        /// https://github.com/dotnet/runtime/issues/29204
        /// </summary>
        public static void CopyIntoDirectory(string filePath, string directoryPath)
        {
            File.Copy(
                filePath,
                Path.Combine(directoryPath, Path.GetFileName(filePath)));
        }
    }
}

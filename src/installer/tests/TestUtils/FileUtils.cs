// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class FileUtils
    {
        public static void EnsureFileDirectoryExists(string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
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
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal static partial class FindTransforms
    {
        internal static DirectoryInfo AsDirectoryInfo(ref RawFindData findData)
        {
            string fileName = new string(findData.FileName);
            return new DirectoryInfo(PathHelpers.CombineNoChecks(findData.Directory, fileName), fileName, ref findData);
        }

        internal static FileInfo AsFileInfo(ref RawFindData findData)
        {
            string fileName = new string(findData.FileName);
            return new FileInfo(PathHelpers.CombineNoChecks(findData.Directory, fileName), fileName, ref findData);
        }

        internal static FileSystemInfo AsFileSystemInfo(ref RawFindData findData)
        {
            string fileName = new string(findData.FileName);
            string fullPath = PathHelpers.CombineNoChecks(findData.Directory, fileName);

            return (findData.Attributes & FileAttributes.Directory) != 0
                ? (FileSystemInfo)new DirectoryInfo(fullPath, fileName, ref findData)
                : (FileSystemInfo)new FileInfo(fullPath, fileName, ref findData);
        }

        /// <summary>
        /// Returns the full path for find results, based off of the initially provided path.
        /// </summary>
        internal static string AsUserFullPath(ref RawFindData findData)
        {
            ReadOnlySpan<char> subdirectory = findData.Directory.AsReadOnlySpan().Slice(findData.OriginalDirectory.Length);
            return PathHelpers.CombineNoChecks(findData.OriginalUserDirectory, subdirectory, findData.FileName);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Extensions.FileProviders.Physical
{
    internal static class FileSystemInfoHelper
    {
        public static bool IsExcluded(FileSystemInfo fileSystemInfo, ExclusionFilters filters)
        {
            if (filters == ExclusionFilters.None)
            {
                return false;
            }
            else if (fileSystemInfo.Name.StartsWith(".", StringComparison.Ordinal) && (filters & ExclusionFilters.DotPrefixed) != 0)
            {
                return true;
            }
            else if (fileSystemInfo.Exists &&
                (((fileSystemInfo.Attributes & FileAttributes.Hidden) != 0 && (filters & ExclusionFilters.Hidden) != 0) ||
                 ((fileSystemInfo.Attributes & FileAttributes.System) != 0 && (filters & ExclusionFilters.System) != 0)))
            {
                return true;
            }

            return false;
        }

        public static FileInfo ResolveFileLinkTarget(string filePath)
#if NETCOREAPP
            => ResolveFileLinkTarget(new FileInfo(filePath));
#else
            => null;
#endif

        public static FileInfo ResolveFileLinkTarget(FileInfo fileInfo)
        {
#if NETCOREAPP
            if (fileInfo.Exists && fileInfo.LinkTarget != null)
            {
                FileSystemInfo targetInfo = fileInfo.ResolveLinkTarget(returnFinalTarget: true);
                if (targetInfo.Exists)
                {
                    return (FileInfo)targetInfo;
                }
            }
#endif

            return null;
        }
    }
}

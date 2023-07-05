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

        public static DateTime? GetFileLinkTargetLastWriteTimeUtc(string filePath)
        {
#if NETCOREAPP
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                return GetFileLinkTargetLastWriteTimeUtc(fileInfo);
            }
#endif
            return null;
        }

        // If file is a link and link target exists, return target's LastWriteTimeUtc.
        // If file is a link, and link target does not exists, return DateTime.MinValue
        //   since the link's LastWriteTimeUtc doesn't convey anything for this scenario.
        // If file is not a link, return null to inform the caller that file is not a link.
        public static DateTime? GetFileLinkTargetLastWriteTimeUtc(FileInfo fileInfo)
        {
#if NETCOREAPP
            Debug.Assert(fileInfo.Exists);
            if (fileInfo.LinkTarget != null)
            {
                try
                {
                    FileSystemInfo? targetInfo = fileInfo.ResolveLinkTarget(returnFinalTarget: true);
                    if (targetInfo != null && targetInfo.Exists)
                    {
                        return targetInfo.LastWriteTimeUtc;
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // The target or the link ceased to exist between LinkTarget and ResolveLinkTarget.
                }

                return DateTime.MinValue;
            }
#endif

            return null;
        }
    }
}

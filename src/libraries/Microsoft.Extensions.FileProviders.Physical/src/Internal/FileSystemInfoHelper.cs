// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.FileProviders.Physical
{
    internal static class FileSystemInfoHelper
    {
        public static bool IsExcluded(FileSystemInfo fileSystemInfo, ExclusionFilters filters) =>
            FindMatchingExclusionFilter(fileSystemInfo, filters) != ExclusionFilters.None;

        public static ExclusionFilters FindMatchingExclusionFilter(FileSystemInfo fileSystemInfo, ExclusionFilters filters)
        {
            if (filters == ExclusionFilters.None)
            {
                return ExclusionFilters.None;
            }

            if (fileSystemInfo.Name.StartsWith(".", StringComparison.Ordinal) && (filters & ExclusionFilters.DotPrefixed) != 0)
            {
                return ExclusionFilters.DotPrefixed;
            }

            if (fileSystemInfo.Exists)
            {
                if ((fileSystemInfo.Attributes & FileAttributes.Hidden) != 0 &&
                    (filters & ExclusionFilters.Hidden) != 0)
                {
                    return ExclusionFilters.Hidden;
                }

                if ((fileSystemInfo.Attributes & FileAttributes.System) != 0 &&
                    (filters & ExclusionFilters.System) != 0)
                {
                    return ExclusionFilters.System;
                }
            }

            return ExclusionFilters.None;
        }
    }
}

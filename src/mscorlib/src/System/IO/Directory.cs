// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Exposes routines for enumerating through a 
** directory.
**
**          April 11,2000
**
===========================================================*/

using System.Collections.Generic;
using System.Security;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System.IO
{
    internal static class Directory
    {
        // Private class that holds search data that is passed around 
        // in the heap based stack recursion
        internal sealed class SearchData
        {
            public SearchData(String fullPath, String userPath, SearchOption searchOption)
            {
                Debug.Assert(fullPath != null && fullPath.Length > 0);
                Debug.Assert(userPath != null && userPath.Length > 0);
                Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

                this.fullPath = fullPath;
                this.userPath = userPath;
                this.searchOption = searchOption;
            }

            public readonly string fullPath;     // Fully qualified search path excluding the search criteria in the end (ex, c:\temp\bar\foo)
            public readonly string userPath;     // User specified path (ex, bar\foo)
            public readonly SearchOption searchOption;
        }

#if PLATFORM_UNIX
        public static IEnumerable<String> EnumerateFiles(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);

            return InternalEnumerateFiles(path, searchPattern, searchOption);
        }

        private static IEnumerable<String> InternalEnumerateFiles(String path, String searchPattern, SearchOption searchOption)
        {
            Debug.Assert(path != null);
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return EnumerateFileSystemNames(path, searchPattern, searchOption, true, false);
        }

        private static IEnumerable<String> EnumerateFileSystemNames(String path, String searchPattern, SearchOption searchOption,
                                                            bool includeFiles, bool includeDirs)
        {
            Debug.Assert(path != null);
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return FileSystemEnumerableFactory.CreateFileNameIterator(path, path, searchPattern,
                                                                        includeFiles, includeDirs, searchOption, true);
        }
#endif // PLATFORM_UNIX        
    }
}


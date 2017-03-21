// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//  URLString
//
//
//  Implementation of membership condition for zones
//

namespace System.Security.Util
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Runtime.Serialization;
    using System.Globalization;
    using System.Text;
    using System.IO;
    using System.Diagnostics.Contracts;

    internal static class URLString
    {
        internal static string PreProcessForExtendedPathRemoval(bool checkPathLength, string url, bool isFileUrl)
        {
            bool isUncShare = false;
            return PreProcessForExtendedPathRemoval(checkPathLength: checkPathLength, url: url, isFileUrl: isFileUrl, isUncShare: ref isUncShare);
        }

        // Keeping this signature to avoid reflection breaks
        private static string PreProcessForExtendedPathRemoval(string url, bool isFileUrl, ref bool isUncShare)
        {
            return PreProcessForExtendedPathRemoval(checkPathLength: true, url: url, isFileUrl: isFileUrl, isUncShare: ref isUncShare);
        }

        private static string PreProcessForExtendedPathRemoval(bool checkPathLength, string url, bool isFileUrl, ref bool isUncShare)
        {
            // This is the modified URL that we will return
            StringBuilder modifiedUrl = new StringBuilder(url);

            // ITEM 1 - remove extended path characters.
            {
                // Keep track of where we are in both the comparison and altered strings.
                int curCmpIdx = 0;
                int curModIdx = 0;

                // If all the '\' have already been converted to '/', just check for //?/ or //./
                if ((url.Length - curCmpIdx) >= 4 &&
                    (String.Compare(url, curCmpIdx, "//?/", 0, 4, StringComparison.OrdinalIgnoreCase) == 0 ||
                     String.Compare(url, curCmpIdx, "//./", 0, 4, StringComparison.OrdinalIgnoreCase) == 0))
                {
                    modifiedUrl.Remove(curModIdx, 4);
                    curCmpIdx += 4;
                }
                else
                {
                    if (isFileUrl)
                    {
                        // We need to handle an indefinite number of leading front slashes for file URLs since we could
                        // get something like:
                        //      file://\\?\
                        //      file:/\\?\
                        //      file:\\?\
                        //      etc...
                        while (url[curCmpIdx] == '/')
                        {
                            curCmpIdx++;
                            curModIdx++;
                        }
                    }

                    // Remove the extended path characters
                    if ((url.Length - curCmpIdx) >= 4 &&
                        (String.Compare(url, curCmpIdx, "\\\\?\\", 0, 4, StringComparison.OrdinalIgnoreCase) == 0 ||
                         String.Compare(url, curCmpIdx, "\\\\?/", 0, 4, StringComparison.OrdinalIgnoreCase) == 0 ||
                         String.Compare(url, curCmpIdx, "\\\\.\\", 0, 4, StringComparison.OrdinalIgnoreCase) == 0 ||
                         String.Compare(url, curCmpIdx, "\\\\./", 0, 4, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        modifiedUrl.Remove(curModIdx, 4);
                        curCmpIdx += 4;
                    }
                }
            }

            // ITEM 2 - convert all slashes to forward slashes, and strip leading slashes.
            if (isFileUrl)
            {
                int slashCount = 0;
                bool seenFirstBackslash = false;

                while (slashCount < modifiedUrl.Length && (modifiedUrl[slashCount] == '/' || modifiedUrl[slashCount] == '\\'))
                {
                    // Look for sets of consecutive backslashes. We can't just look for these at the start
                    // of the string, since file:// might come first.  Instead, once we see the first \, look
                    // for a second one following it.
                    if (!seenFirstBackslash && modifiedUrl[slashCount] == '\\')
                    {
                        seenFirstBackslash = true;
                        if (slashCount + 1 < modifiedUrl.Length && modifiedUrl[slashCount + 1] == '\\')
                            isUncShare = true;
                    }

                    slashCount++;
                }

                modifiedUrl.Remove(0, slashCount);
                modifiedUrl.Replace('\\', '/');
            }

            // ITEM 3 - If the path is greater than or equal (due to terminating NULL in windows) MAX_PATH, we throw.
            if (checkPathLength)
            {
                // This needs to be a separate method to avoid hitting the static constructor on AppContextSwitches
                CheckPathTooLong(modifiedUrl);
            }

            // Create the result string from the StringBuilder
            return modifiedUrl.ToString();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CheckPathTooLong(StringBuilder path)
        {
            if (path.Length >= (
#if PLATFORM_UNIX
                Interop.Sys.MaxPath))
#else
                PathInternal.MaxLongPath))
#endif
            {
                throw new PathTooLongException(SR.IO_PathTooLong);
            }
        }
    }
}

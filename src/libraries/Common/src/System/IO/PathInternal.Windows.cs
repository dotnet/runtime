// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;

namespace System.IO
{
    /// <summary>
    ///     Contains internal path helpers that are shared between many projects.
    /// </summary>
    internal static class PathInternal
    {
        /// <summary>
        ///     Returns a value indicating if the given path contains invalid characters (", &lt;, &gt;, | 
        ///     NUL, or any ASCII char whose integer representation is in the range of 1 through 31), 
        ///     optionally checking for ? and *.
        /// </summary>
        internal static bool HasIllegalCharacters(string path, bool checkAdditional = false)
        {
            // See: http://msdn.microsoft.com/en-us/library/windows/desktop/aa365247(v=vs.85).aspx

            Contract.Requires(path != null);

            foreach (char c in path)
            {
                // Note: Same as Path.InvalidPathChars, unrolled here for performance
                if (c == '\"' || c == '<' || c == '>' || c == '|' || c < 32)
                    return true;

                // used when path cannot contain search strings.
                if (checkAdditional &&
                    (c == '?' || c == '*'))
                    return true;
            }

            return false;
        }
    }
}
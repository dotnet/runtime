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
        ///     Returns a value indicating if the given path contains invalid characters.
        /// </summary>
        internal static bool HasIllegalCharacters(string path, bool checkAdditional = false)
        {
            Contract.Requires(path != null);

            foreach (char c in path)
            {
                // Same as Path.InvalidPathChars, unrolled here for performance
                if (c == '\0')
                    return true;

                // checkAdditional is meant to check for additional characters 
                // permitted in a search path but disallowed in a normal path.
                // In Windows this is * and ?,but Unix permits such characters
                // in the filename so checkAdditional is ignored.
            }

            return false;
        }
    }
}
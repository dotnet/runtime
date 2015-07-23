// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;

namespace System.IO
{
    /// <summary>Contains internal path helpers that are shared between many projects.</summary>
    internal static partial class PathInternal
    {
        /// <summary>
        /// Checks for invalid path characters in the given path.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Thrown if the path is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if the path has invalid characters.</exception>
        /// <param name="path">The path to check for invalid characters.</param>
        /// <param name="checkAdditional">Set to true to check for characters that are only valid in specific contexts (such as search characters on Windows).</param>
        internal static void CheckInvalidPathChars(string path, bool checkAdditional = false)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (PathInternal.HasIllegalCharacters(path, checkAdditional))
                throw new ArgumentException(SR.Argument_InvalidPathChars, "path");
        }
    }
}

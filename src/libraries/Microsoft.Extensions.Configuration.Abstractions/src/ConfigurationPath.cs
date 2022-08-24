// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Utility methods and constants for manipulating Configuration paths
    /// </summary>
    public static class ConfigurationPath
    {
        /// <summary>
        /// The delimiter ":" used to separate individual keys in a path.
        /// </summary>
        public static readonly string KeyDelimiter = ":";

        /// <summary>
        /// Combines path segments into one path.
        /// </summary>
        /// <param name="pathSegments">The path segments to combine.</param>
        /// <returns>The combined path.</returns>
        public static string Combine(params string[] pathSegments)
        {
            ThrowHelper.ThrowIfNull(pathSegments);

            return string.Join(KeyDelimiter, pathSegments);
        }

        /// <summary>
        /// Combines path segments into one path.
        /// </summary>
        /// <param name="pathSegments">The path segments to combine.</param>
        /// <returns>The combined path.</returns>
        public static string Combine(IEnumerable<string> pathSegments)
        {
            ThrowHelper.ThrowIfNull(pathSegments);

            return string.Join(KeyDelimiter, pathSegments);
        }

        /// <summary>
        /// Extracts the last path segment from the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The last path segment of the path.</returns>
        [return: NotNullIfNotNull(nameof(path))]
        public static string? GetSectionKey(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            int lastDelimiterIndex = path.LastIndexOf(KeyDelimiter, StringComparison.OrdinalIgnoreCase);
            return lastDelimiterIndex == -1 ? path : path.Substring(lastDelimiterIndex + 1);
        }

        /// <summary>
        /// Extracts the path corresponding to the parent node for a given path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The original path minus the last individual segment found in it. Null if the original path corresponds to a top level node.</returns>
        public static string? GetParentPath(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            int lastDelimiterIndex = path.LastIndexOf(KeyDelimiter, StringComparison.OrdinalIgnoreCase);
            return lastDelimiterIndex == -1 ? null : path.Substring(0, lastDelimiterIndex);
        }
    }
}

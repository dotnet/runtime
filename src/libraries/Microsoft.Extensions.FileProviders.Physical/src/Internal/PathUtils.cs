// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders.Physical.Internal
{
    internal static class PathUtils
    {
        private static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars()
            .Where(c => c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar).ToArray();

        private static readonly char[] _invalidFilterChars = _invalidFileNameChars
            .Where(c => c != '*' && c != '|' && c != '?').ToArray();

        private static readonly char[] _pathSeparators = new[]
            {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar};

        internal static bool HasInvalidPathChars(string path)
        {
            return path.IndexOfAny(_invalidFileNameChars) != -1;
        }

        internal static bool HasInvalidFilterChars(string path)
        {
            return path.IndexOfAny(_invalidFilterChars) != -1;
        }

        internal static string EnsureTrailingSlash(string path)
        {
            if (!string.IsNullOrEmpty(path) &&
                path[path.Length - 1] != Path.DirectorySeparatorChar)
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }

        internal static bool PathNavigatesAboveRoot(string path)
        {
            var tokenizer = new StringTokenizer(path, _pathSeparators);
            int depth = 0;

            foreach (StringSegment segment in tokenizer)
            {
                if (segment.Equals(".") || segment.Equals(""))
                {
                    continue;
                }
                else if (segment.Equals(".."))
                {
                    depth--;

                    if (depth == -1)
                    {
                        return true;
                    }
                }
                else
                {
                    depth++;
                }
            }

            return false;
        }
    }
}

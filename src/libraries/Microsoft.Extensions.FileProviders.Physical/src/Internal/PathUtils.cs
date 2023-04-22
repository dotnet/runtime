// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders.Physical.Internal
{
    internal static class PathUtils
    {
        private static char[] GetInvalidFileNameChars() => Path.GetInvalidFileNameChars()
            .Where(c => c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar).ToArray();

        private static char[] GetInvalidFilterChars() => GetInvalidFileNameChars()
            .Where(c => c != '*' && c != '|' && c != '?').ToArray();

#if NET8_0_OR_GREATER
        private static readonly IndexOfAnyValues<char> _invalidFileNameChars = IndexOfAnyValues.Create(GetInvalidFileNameChars());
        private static readonly IndexOfAnyValues<char> _invalidFilterChars = IndexOfAnyValues.Create(GetInvalidFilterChars());
#else
        private static readonly char[] _invalidFileNameChars = GetInvalidFileNameChars();
        private static readonly char[] _invalidFilterChars = GetInvalidFilterChars();
#endif

        private static readonly char[] _pathSeparators = new[]
            {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar};

        internal static bool HasInvalidPathChars(string path)
        {
            return path.AsSpan().IndexOfAny(_invalidFileNameChars) >= 0;
        }

        internal static bool HasInvalidFilterChars(string path)
        {
            return path.AsSpan().IndexOfAny(_invalidFilterChars) >= 0;
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

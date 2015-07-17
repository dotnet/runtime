// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;

namespace System.IO
{
    /// <summary>Contains internal path helpers that are shared between many projects.</summary>
    internal static partial class PathInternal
    {
        internal const string LongPathPrefix = @"\\?\";
        internal const string UNCPathPrefix = @"\\";
        internal const string UNCLongPathPrefixToInsert = @"?\UNC\";
        internal const string UNCLongPathPrefix = @"\\?\UNC\";

        internal static readonly char[] InvalidPathChars =
        {
            '\"', '<', '>', '|', '\0',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31
        };

        internal static readonly char[] InvalidPathCharsWithAdditionalChecks = // This is used by HasIllegalCharacters
        {
            '\"', '<', '>', '|', '\0',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31, '*', '?'
        };

        /// <summary>
        /// Returns a value indicating if the given path contains invalid characters (", &lt;, &gt;, | 
        /// NUL, or any ASCII char whose integer representation is in the range of 1 through 31), 
        /// optionally checking for ? and *.
        /// </summary>
        internal static bool HasIllegalCharacters(string path, bool checkAdditional = false)
        {
            // See: http://msdn.microsoft.com/en-us/library/windows/desktop/aa365247(v=vs.85).aspx

            Contract.Requires(path != null);

            // Question mark is a normal part of extended path syntax (\\?\)
            int startIndex = path.StartsWith(LongPathPrefix, StringComparison.Ordinal) ? LongPathPrefix.Length : 0;
            return path.IndexOfAny(checkAdditional ? InvalidPathCharsWithAdditionalChecks : InvalidPathChars, startIndex) >= 0;
        }

        /// <summary>
        /// Gets the length of the root of the path (drive, share, etc.).
        /// </summary>
        internal static int GetRootLength(string path)
        {
            CheckInvalidPathChars(path);

            int i = 0;
            int length = path.Length;
            int volumeSeparatorLength = 2;  // Length to the colon "C:"
            int uncRootLength = 2;          // Length to the start of the server name "\\"

            bool extendedSyntax = path.StartsWith(LongPathPrefix, StringComparison.Ordinal);
            bool extendedUncSyntax = extendedSyntax && path.StartsWith(UNCLongPathPrefix, StringComparison.Ordinal);
            if (extendedSyntax)
            {
                // Shift the position we look for the root from to account for the extended prefix
                if (extendedUncSyntax)
                {
                    // "C:" -> "\\?\C:"
                    uncRootLength = UNCLongPathPrefix.Length;
                }
                else
                {
                    // "\\" -> "\\?\UNC\"
                    volumeSeparatorLength += LongPathPrefix.Length;
                }
            }

            if ((!extendedSyntax || extendedUncSyntax) && length > 0 && IsDirectorySeparator(path[0]))
            {
                // UNC or simple rooted path (e.g. "\foo", NOT "\\?\C:\foo")

                i = 1; //  Drive rooted (\foo) is one character
                if (extendedUncSyntax || (length > 1 && (IsDirectorySeparator(path[1]))))
                {
                    // UNC (\\?\UNC\ or \\), scan past the next two directory separators at most
                    // (e.g. to \\?\UNC\Server\Share or \\Server\Share\)
                    i = uncRootLength;
                    int n = 2; // Maximum separators to skip
                    while (i < length && (!IsDirectorySeparator(path[i]) || --n > 0)) i++;
                }
            }
            else if (length >= volumeSeparatorLength && path[volumeSeparatorLength - 1] == Path.VolumeSeparatorChar)
            {
                // Path is at least longer than where we expect a colon, and has a colon (\\?\A:, A:)
                // If the colon is followed by a directory separator, move past it
                i = volumeSeparatorLength;
                if (length >= volumeSeparatorLength + 1 && (IsDirectorySeparator(path[volumeSeparatorLength]))) i++;
            }
            return i;
        }

        internal static bool IsDirectorySeparator(char c)
        {
            return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
        }
    }
}

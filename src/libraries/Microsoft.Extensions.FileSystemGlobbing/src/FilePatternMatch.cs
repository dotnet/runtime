// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.FileSystemGlobbing
{
    /// <summary>
    /// Represents a file that was matched by searching using a globbing pattern
    /// </summary>
    public struct FilePatternMatch : IEquatable<FilePatternMatch>
    {
        /// <summary>
        /// The path to the file matched
        /// </summary>
        /// <remarks>
        /// If the matcher searched for "**/*.cs" using "src/Project" as the directory base and the pattern matcher found
        /// "src/Project/Interfaces/IFile.cs", then Stem = "Interfaces/IFile.cs" and Path = "src/Project/Interfaces/IFile.cs".
        /// </remarks>
        public string Path { get; }

        /// <summary>
        /// The subpath to the matched file under the base directory searched
        /// </summary>
        /// <remarks>
        /// If the matcher searched for "**/*.cs" using "src/Project" as the directory base and the pattern matcher found
        /// "src/Project/Interfaces/IFile.cs",
        /// then Stem = "Interfaces/IFile.cs" and Path = "src/Project/Interfaces/IFile.cs".
        /// </remarks>
        public string Stem { get; }

        /// <summary>
        /// Initializes new instance of <see cref="FilePatternMatch" />
        /// </summary>
        /// <param name="path">The path to the matched file</param>
        /// <param name="stem">The stem</param>
        public FilePatternMatch(string path, string stem)
        {
            Path = path;
            Stem = stem;
        }

        /// <summary>
        /// Determines if the specified match is equivalent to the current match using a case-insensitive comparison.
        /// </summary>
        /// <param name="other">The other match to be compared</param>
        /// <returns>True if <see cref="Path" /> and <see cref="Stem" /> are equal using case-insensitive comparison</returns>
        public bool Equals(FilePatternMatch other)
        {
            return string.Equals(other.Path, Path, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(other.Stem, Stem, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines if the specified object is equivalent to the current match using a case-insensitive comparison.
        /// </summary>
        /// <param name="obj">The object to be compared</param>
        /// <returns>True when <see cref="Equals(FilePatternMatch)" /></returns>
        public override bool Equals(object obj)
        {
            return Equals((FilePatternMatch) obj);
        }

        /// <summary>
        /// Gets a hash for the file pattern match.
        /// </summary>
        /// <returns>Some number</returns>
        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(Path, StringComparer.OrdinalIgnoreCase);
            hashCodeCombiner.Add(Stem, StringComparer.OrdinalIgnoreCase);

            return hashCodeCombiner;
        }
    }
}
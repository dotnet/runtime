// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics.Hashing;

namespace Microsoft.Extensions.FileSystemGlobbing
{
    /// <summary>
    /// Represents a file that was matched by searching using a globbing pattern
    /// </summary>
    public struct FilePatternMatch : IEquatable<FilePatternMatch>
    {
        /// <summary>
        /// The path to the file matched, relative to the beginning of the matching search pattern.
        /// </summary>
        /// <remarks>
        /// If the matcher searched for "src/Project/**/*.cs" and the pattern matcher found "src/Project/Interfaces/IFile.cs",
        /// then <see cref="Stem" /> = "Interfaces/IFile.cs" and <see cref="Path" /> = "src/Project/Interfaces/IFile.cs".
        /// </remarks>
        public string Path { get; }

        /// <summary>
        /// The subpath to the file matched, relative to the first wildcard in the matching search pattern.
        /// </summary>
        /// <remarks>
        /// If the matcher searched for "src/Project/**/*.cs" and the pattern matcher found "src/Project/Interfaces/IFile.cs",
        /// then <see cref="Stem" /> = "Interfaces/IFile.cs" and <see cref="Path" /> = "src/Project/Interfaces/IFile.cs".
        /// </remarks>
        public string? Stem { get; }

        /// <summary>
        /// Initializes new instance of <see cref="FilePatternMatch" />
        /// </summary>
        /// <param name="path">The path to the file matched, relative to the beginning of the matching search pattern.</param>
        /// <param name="stem">The subpath to the file matched, relative to the first wildcard in the matching search pattern.</param>
        public FilePatternMatch(string path, string? stem)
        {
            ThrowHelper.ThrowIfNull(path);

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
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is FilePatternMatch match && Equals(match);

        /// <summary>
        /// Gets a hash for the file pattern match.
        /// </summary>
        /// <returns>Some number</returns>
        public override int GetHashCode() =>
            HashHelpers.Combine(GetHashCode(Path), GetHashCode(Stem));

        private static int GetHashCode(string? value) =>
            value != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(value) : 0;
    }
}

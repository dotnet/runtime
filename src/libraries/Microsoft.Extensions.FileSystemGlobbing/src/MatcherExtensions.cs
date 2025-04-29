// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Microsoft.Extensions.FileSystemGlobbing
{
    public static class MatcherExtensions
    {
        /// <summary>
        /// Adds multiple exclude patterns to <see cref="Matcher" />. <seealso cref="Matcher.AddExclude(string)" />
        /// </summary>
        /// <param name="matcher">The matcher to which the exclude patterns are added</param>
        /// <param name="excludePatternsGroups">A list of globbing patterns</param>
        public static void AddExcludePatterns(this Matcher matcher, params IEnumerable<string>[] excludePatternsGroups)
        {
            foreach (IEnumerable<string> group in excludePatternsGroups)
            {
                foreach (string pattern in group)
                {
                    matcher.AddExclude(pattern);
                }
            }
        }

        /// <summary>
        /// Adds multiple patterns to include in <see cref="Matcher" />. See <seealso cref="Matcher.AddInclude(string)" />
        /// </summary>
        /// <param name="matcher">The matcher to which the include patterns are added</param>
        /// <param name="includePatternsGroups">A list of globbing patterns</param>
        public static void AddIncludePatterns(this Matcher matcher, params IEnumerable<string>[] includePatternsGroups)
        {
            foreach (IEnumerable<string> group in includePatternsGroups)
            {
                foreach (string pattern in group)
                {
                    matcher.AddInclude(pattern);
                }
            }
        }

        /// <summary>
        /// Searches the directory specified for all files matching patterns added to this instance of <see cref="Matcher" />
        /// </summary>
        /// <param name="matcher">The matcher</param>
        /// <param name="directoryPath">The root directory for the search</param>
        /// <returns>Absolute file paths of all files matched. Empty enumerable if no files matched given patterns.</returns>
        public static IEnumerable<string> GetResultsInFullPath(this Matcher matcher, string directoryPath)
        {
            PatternMatchingResult patternMatchingResult = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(directoryPath)));
            if (!patternMatchingResult.HasMatches)
            {
                return Array.Empty<string>();
            }

            IEnumerable<FilePatternMatch> matches = patternMatchingResult.Files;
            List<string> result = matches is ICollection matchCollection ? new(matchCollection.Count) : new();
            foreach (FilePatternMatch match in matches)
            {
                result.Add(Path.GetFullPath(Path.Combine(directoryPath, match.Path)));
            }
            return result;
        }

        /// <summary>
        /// Matches the file passed in with the patterns in the matcher without going to disk.
        /// </summary>
        /// <param name="matcher">The matcher that holds the patterns and pattern matching type.</param>
        /// <param name="file">The file to run the matcher against.</param>
        /// <returns>The match results.</returns>
        public static PatternMatchingResult Match(this Matcher matcher, string file)
        {
            return Match(matcher, Directory.GetCurrentDirectory(), new List<string> { file });
        }

        /// <summary>
        /// Matches the file passed in with the patterns in the matcher without going to disk.
        /// </summary>
        /// <param name="matcher">The matcher that holds the patterns and pattern matching type.</param>
        /// <param name="rootDir">The root directory for the matcher to match the file from.</param>
        /// <param name="file">The file to run the matcher against.</param>
        /// <returns>The match results.</returns>
        public static PatternMatchingResult Match(this Matcher matcher, string rootDir, string file)
        {
            return Match(matcher, rootDir, new List<string> { file });
        }

        /// <summary>
        /// Matches the files passed in with the patterns in the matcher without going to disk.
        /// </summary>
        /// <param name="matcher">The matcher that holds the patterns and pattern matching type.</param>
        /// <param name="files">The files to run the matcher against.</param>
        /// <returns>The match results.</returns>
        public static PatternMatchingResult Match(this Matcher matcher, IEnumerable<string>? files)
        {
            return Match(matcher, Directory.GetCurrentDirectory(), files);
        }

        /// <summary>
        /// Matches the files passed in with the patterns in the matcher without going to disk.
        /// </summary>
        /// <param name="matcher">The matcher that holds the patterns and pattern matching type.</param>
        /// <param name="rootDir">The root directory for the matcher to match the files from.</param>
        /// <param name="files">The files to run the matcher against.</param>
        /// <returns>The match results.</returns>
        public static PatternMatchingResult Match(this Matcher matcher, string rootDir, IEnumerable<string>? files)
        {
            ThrowHelper.ThrowIfNull(matcher);

            return matcher.Execute(new InMemoryDirectoryInfo(rootDir, files));
        }
    }
}

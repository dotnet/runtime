// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;

namespace System.IO.Enumeration
{
    internal static class FileSystemEnumerableFactory
    {
        // Filter modes for file system enumeration
        private enum FileSystemEntryType
        {
            All,
            Files,
            Directories
        }

        /// <summary>
        /// Validates the directory and expression strings to check that they have no invalid characters, any special DOS wildcard characters in Win32 in the expression get replaced with their proper escaped representation, and if the expression string begins with a directory name, the directory name is moved and appended at the end of the directory string.
        /// </summary>
        /// <param name="directory">A reference to a directory string that we will be checking for normalization.</param>
        /// <param name="expression">A reference to a expression string that we will be checking for normalization.</param>
        /// <param name="matchType">The kind of matching we want to check in the expression. If the value is Win32, we will replace special DOS wild characters to their safely escaped representation. This replacement does not affect the normalization status of the expression.</param>
        /// <returns><cref langword="false" /> if the directory reference string get modified inside this function due to the expression beginning with a directory name. <cref langword="true" /> if the directory reference string was not modified.</returns>
        /// <exception cref="ArgumentException">
        /// The expression is a rooted path.
        /// -or-
        /// The directory or the expression reference strings contain a null character.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The match type is out of the range of the valid MatchType enum values.
        /// </exception>
        internal static bool NormalizeInputs(ref string directory, ref string expression, MatchType matchType)
        {
            if (Path.IsPathRooted(expression))
                throw new ArgumentException(SR.Arg_Path2IsRooted, nameof(expression));

            if (expression.Contains('\0'))
                throw new ArgumentException(SR.Argument_NullCharInPath, expression);

            if (directory.Contains('\0'))
                throw new ArgumentException(SR.Argument_NullCharInPath, directory);

            // We always allowed breaking the passed ref directory and filter to be separated
            // any way the user wanted. Looking for "C:\foo\*.cs" could be passed as "C:\" and
            // "foo\*.cs" or "C:\foo" and "*.cs", for example. As such we need to combine and
            // split the inputs if the expression contains a directory separator.
            //
            // We also allowed for expression to be "foo\" which would translate to "foo\*".

            ReadOnlySpan<char> directoryName = Path.GetDirectoryName(expression.AsSpan());

            bool isDirectoryModified = true;

            if (directoryName.Length != 0)
            {
                // Need to fix up the input paths
                directory = Path.Join(directory.AsSpan(), directoryName);
                expression = expression.Substring(directoryName.Length + 1);

                isDirectoryModified = false;
            }

            switch (matchType)
            {
                case MatchType.Win32:
                    if (expression == "*")
                    {
                        // Most common case
                        break;
                    }
                    else if (string.IsNullOrEmpty(expression) || expression == "." || expression == "*.*")
                    {
                        // Historically we always treated "." as "*"
                        expression = "*";
                    }
                    else
                    {
                        // These all have special meaning in DOS name matching. '\' is the escaping character (which conveniently
                        // is the directory separator and cannot be part of any path segment in Windows). The other three are the
                        // special case wildcards that we'll convert some * and ? into. They're also valid as filenames on Unix,
                        // which is not true in Windows and as such we'll escape any that occur on the input string.
                        if (Path.DirectorySeparatorChar != '\\')
                        {
                            expression = FileSystemName.EscapeExpression(expression);
                        }

                        // Need to convert the expression to match Win32 behavior
                        expression = FileSystemName.TranslateWin32Expression(expression);
                    }
                    break;
                case MatchType.Simple:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(matchType));
            }

            return isDirectoryModified;
        }

        /// <summary>
        /// Returns a predicate that checks whether a file entry matches the given expression and entry type.
        /// The predicate is optimized based on the pattern type (e.g., StartsWith, EndsWith, Contains).
        /// </summary>
        private static FileSystemEnumerable<T>.FindPredicate GetPredicate<T>(string expression, EnumerationOptions options, FileSystemEntryType entryType)
        {
            bool ignoreCase = (options.MatchCasing == MatchCasing.PlatformDefault && !PathInternal.IsCaseSensitive)
                || options.MatchCasing == MatchCasing.CaseInsensitive;

            StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            bool useExtendedWildcards = options.MatchType == MatchType.Win32;

            // Determine which wildcards to check for (extended wildcards include DOS special characters)
            SearchValues<char> wildcards = useExtendedWildcards ? FileSystemName.s_extendedWildcards : FileSystemName.s_simpleWildcards;

            // Check for special patterns that can be optimized
            if (expression == "*")
            {
                // Match all - only need to check entry type
                return entryType switch
                {
                    FileSystemEntryType.Files => (ref FileSystemEntry entry) => !entry.IsDirectory,
                    FileSystemEntryType.Directories => (ref FileSystemEntry entry) => entry.IsDirectory,
                    _ => (ref FileSystemEntry entry) => true
                };
            }

            // Check for literal pattern (no wildcards and no escape characters) - use simple Equals
            // The backslash is an escape character that needs to be processed by the full matcher
            if (!expression.AsSpan().ContainsAny(wildcards) && !expression.Contains('\\'))
            {
                return entryType switch
                {
                    FileSystemEntryType.Files => (ref FileSystemEntry entry) => !entry.IsDirectory && entry.FileName.Equals(expression, comparison),
                    FileSystemEntryType.Directories => (ref FileSystemEntry entry) => entry.IsDirectory && entry.FileName.Equals(expression, comparison),
                    _ => (ref FileSystemEntry entry) => entry.FileName.Equals(expression, comparison)
                };
            }

            if (expression.Length > 1)
            {
                bool startsWithStar = expression[0] == '*';
                bool endsWithStar = expression[^1] == '*';

                switch ((startsWithStar, endsWithStar))
                {
                    case (true, true):
                    {
                        // Pattern: *literal* (Contains)
                        ReadOnlySpan<char> middle = expression.AsSpan(1, expression.Length - 2);
                        if (!middle.ContainsAny(wildcards))
                        {
                            return entryType switch
                            {
                                FileSystemEntryType.Files => (ref FileSystemEntry entry) => !entry.IsDirectory && entry.FileName.Contains(expression.AsSpan(1, expression.Length - 2), comparison),
                                FileSystemEntryType.Directories => (ref FileSystemEntry entry) => entry.IsDirectory && entry.FileName.Contains(expression.AsSpan(1, expression.Length - 2), comparison),
                                _ => (ref FileSystemEntry entry) => entry.FileName.Contains(expression.AsSpan(1, expression.Length - 2), comparison)
                            };
                        }
                        break;
                    }

                    case (true, false):
                    {
                        // Pattern: *literal (EndsWith)
                        ReadOnlySpan<char> suffix = expression.AsSpan(1);
                        if (!suffix.ContainsAny(wildcards))
                        {
                            return entryType switch
                            {
                                FileSystemEntryType.Files => (ref FileSystemEntry entry) => !entry.IsDirectory && entry.FileName.EndsWith(expression.AsSpan(1), comparison),
                                FileSystemEntryType.Directories => (ref FileSystemEntry entry) => entry.IsDirectory && entry.FileName.EndsWith(expression.AsSpan(1), comparison),
                                _ => (ref FileSystemEntry entry) => entry.FileName.EndsWith(expression.AsSpan(1), comparison)
                            };
                        }
                        break;
                    }

                    case (false, true):
                    {
                        // Pattern: literal* (StartsWith)
                        ReadOnlySpan<char> prefix = expression.AsSpan(0, expression.Length - 1);
                        if (!prefix.ContainsAny(wildcards))
                        {
                            return entryType switch
                            {
                                FileSystemEntryType.Files => (ref FileSystemEntry entry) => !entry.IsDirectory && entry.FileName.StartsWith(expression.AsSpan(0, expression.Length - 1), comparison),
                                FileSystemEntryType.Directories => (ref FileSystemEntry entry) => entry.IsDirectory && entry.FileName.StartsWith(expression.AsSpan(0, expression.Length - 1), comparison),
                                _ => (ref FileSystemEntry entry) => entry.FileName.StartsWith(expression.AsSpan(0, expression.Length - 1), comparison)
                            };
                        }
                        break;
                    }

                    case (false, false):
                    {
                        // Check for prefix*suffix pattern
                        int starIndex = expression.IndexOf('*');
                        if (starIndex > 0)
                        {
                            // Pattern: prefix*suffix (StartsWith + EndsWith)
                            ReadOnlySpan<char> prefix = expression.AsSpan(0, starIndex);
                            ReadOnlySpan<char> suffix = expression.AsSpan(starIndex + 1);
                            if (!prefix.ContainsAny(wildcards) && !suffix.ContainsAny(wildcards))
                            {
                                int prefixLength = starIndex;
                                int suffixLength = expression.Length - starIndex - 1;
                                int minLength = prefixLength + suffixLength;
                                return entryType switch
                                {
                                    FileSystemEntryType.Files => (ref FileSystemEntry entry) =>
                                        !entry.IsDirectory &&
                                        entry.FileName.Length >= minLength &&
                                        entry.FileName.StartsWith(expression.AsSpan(0, prefixLength), comparison) &&
                                        entry.FileName.EndsWith(expression.AsSpan(prefixLength + 1), comparison),
                                    FileSystemEntryType.Directories => (ref FileSystemEntry entry) =>
                                        entry.IsDirectory &&
                                        entry.FileName.Length >= minLength &&
                                        entry.FileName.StartsWith(expression.AsSpan(0, prefixLength), comparison) &&
                                        entry.FileName.EndsWith(expression.AsSpan(prefixLength + 1), comparison),
                                    _ => (ref FileSystemEntry entry) =>
                                        entry.FileName.Length >= minLength &&
                                        entry.FileName.StartsWith(expression.AsSpan(0, prefixLength), comparison) &&
                                        entry.FileName.EndsWith(expression.AsSpan(prefixLength + 1), comparison)
                                };
                            }
                        }
                        break;
                    }
                }
            }

            // Fall back to the full pattern matching algorithm
            return (useExtendedWildcards, entryType) switch
            {
                (true, FileSystemEntryType.Files) => (ref FileSystemEntry entry) => !entry.IsDirectory && FileSystemName.MatchesWin32Expression(expression, entry.FileName, ignoreCase),
                (true, FileSystemEntryType.Directories) => (ref FileSystemEntry entry) => entry.IsDirectory && FileSystemName.MatchesWin32Expression(expression, entry.FileName, ignoreCase),
                (true, _) => (ref FileSystemEntry entry) => FileSystemName.MatchesWin32Expression(expression, entry.FileName, ignoreCase),
                (false, FileSystemEntryType.Files) => (ref FileSystemEntry entry) => !entry.IsDirectory && FileSystemName.MatchesSimpleExpression(expression, entry.FileName, ignoreCase),
                (false, FileSystemEntryType.Directories) => (ref FileSystemEntry entry) => entry.IsDirectory && FileSystemName.MatchesSimpleExpression(expression, entry.FileName, ignoreCase),
                (false, _) => (ref FileSystemEntry entry) => FileSystemName.MatchesSimpleExpression(expression, entry.FileName, ignoreCase)
            };
        }

        internal static IEnumerable<string> UserFiles(string directory,
            string expression,
            EnumerationOptions options)
        {
            return new FileSystemEnumerable<string>(
                directory,
                (ref FileSystemEntry entry) => entry.ToSpecifiedFullPath(),
                options,
                isNormalized: false,
                expression)
            {
                ShouldIncludePredicate = GetPredicate<string>(expression, options, FileSystemEntryType.Files)
            };
        }

        internal static IEnumerable<string> UserDirectories(string directory,
            string expression,
            EnumerationOptions options)
        {
            return new FileSystemEnumerable<string>(
                directory,
                (ref FileSystemEntry entry) => entry.ToSpecifiedFullPath(),
                options,
                isNormalized: false,
                expression)
            {
                ShouldIncludePredicate = GetPredicate<string>(expression, options, FileSystemEntryType.Directories)
            };
        }

        internal static IEnumerable<string> UserEntries(string directory,
            string expression,
            EnumerationOptions options)
        {
            return new FileSystemEnumerable<string>(
                directory,
                (ref FileSystemEntry entry) => entry.ToSpecifiedFullPath(),
                options,
                isNormalized: false,
                expression)
            {
                ShouldIncludePredicate = GetPredicate<string>(expression, options, FileSystemEntryType.All)
            };
        }

        internal static IEnumerable<FileInfo> FileInfos(
            string directory,
            string expression,
            EnumerationOptions options,
            bool isNormalized)
        {
            return new FileSystemEnumerable<FileInfo>(
               directory,
               (ref FileSystemEntry entry) => (FileInfo)entry.ToFileSystemInfo(),
               options,
               isNormalized,
               expression)
            {
                ShouldIncludePredicate = GetPredicate<FileInfo>(expression, options, FileSystemEntryType.Files)
            };
        }

        internal static IEnumerable<DirectoryInfo> DirectoryInfos(
            string directory,
            string expression,
            EnumerationOptions options,
            bool isNormalized)
        {
            return new FileSystemEnumerable<DirectoryInfo>(
               directory,
               (ref FileSystemEntry entry) => (DirectoryInfo)entry.ToFileSystemInfo(),
               options,
               isNormalized,
               expression)
            {
                ShouldIncludePredicate = GetPredicate<DirectoryInfo>(expression, options, FileSystemEntryType.Directories)
            };
        }

        internal static IEnumerable<FileSystemInfo> FileSystemInfos(
            string directory,
            string expression,
            EnumerationOptions options,
            bool isNormalized)
        {
            return new FileSystemEnumerable<FileSystemInfo>(
               directory,
               (ref FileSystemEntry entry) => entry.ToFileSystemInfo(),
               options,
               isNormalized,
               expression)
            {
                ShouldIncludePredicate = GetPredicate<FileSystemInfo>(expression, options, FileSystemEntryType.All)
            };
        }
    }
}

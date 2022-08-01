// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace System.IO.Enumeration
{
    internal static class FileSystemEnumerableFactory
    {
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
                throw new ArgumentException(SR.Argument_InvalidPathChars, expression);

            if (directory.Contains('\0'))
                throw new ArgumentException(SR.Argument_InvalidPathChars, directory);

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
                        if (Path.DirectorySeparatorChar != '\\' && expression.AsSpan().IndexOfAny(@"\""<>") >= 0)
                        {
                            // Backslash isn't the default separator, need to escape (e.g. Unix)
                            expression = expression.Replace("\\", "\\\\");

                            // Also need to escape the other special wild characters ('"', '<', and '>')
                            expression = expression.Replace("\"", "\\\"");
                            expression = expression.Replace(">", "\\>");
                            expression = expression.Replace("<", "\\<");
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

        private static bool MatchesPattern(string expression, ReadOnlySpan<char> name, EnumerationOptions options)
        {
            bool ignoreCase = (options.MatchCasing == MatchCasing.PlatformDefault && !PathInternal.IsCaseSensitive)
                || options.MatchCasing == MatchCasing.CaseInsensitive;

            return options.MatchType switch
            {
                MatchType.Simple => FileSystemName.MatchesSimpleExpression(expression.AsSpan(), name, ignoreCase),
                MatchType.Win32 => FileSystemName.MatchesWin32Expression(expression.AsSpan(), name, ignoreCase),
                _ => throw new ArgumentOutOfRangeException(nameof(options)),
            };
        }

        internal static IEnumerable<string> UserFiles(string directory,
            string expression,
            EnumerationOptions options)
        {
            return new FileSystemEnumerable<string>(
                directory,
                (ref FileSystemEntry entry) => entry.ToSpecifiedFullPath(),
                options)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                    !entry.IsDirectory && MatchesPattern(expression, entry.FileName, options)
            };
        }

        internal static IEnumerable<string> UserDirectories(string directory,
            string expression,
            EnumerationOptions options)
        {
            return new FileSystemEnumerable<string>(
                directory,
                (ref FileSystemEntry entry) => entry.ToSpecifiedFullPath(),
                options)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                    entry.IsDirectory && MatchesPattern(expression, entry.FileName, options)
            };
        }

        internal static IEnumerable<string> UserEntries(string directory,
            string expression,
            EnumerationOptions options)
        {
            return new FileSystemEnumerable<string>(
                directory,
                (ref FileSystemEntry entry) => entry.ToSpecifiedFullPath(),
                options)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                    MatchesPattern(expression, entry.FileName, options)
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
               isNormalized)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                    !entry.IsDirectory && MatchesPattern(expression, entry.FileName, options)
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
               isNormalized)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                    entry.IsDirectory && MatchesPattern(expression, entry.FileName, options)
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
               isNormalized)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                    MatchesPattern(expression, entry.FileName, options)
            };
        }
    }
}

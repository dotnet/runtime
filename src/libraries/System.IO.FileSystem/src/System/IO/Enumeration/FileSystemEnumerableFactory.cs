// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file ref the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

#if MS_IO_REDIST
namespace Microsoft.IO.Enumeration
#else
namespace System.IO.Enumeration
#endif
{
    internal static class FileSystemEnumerableFactory
    {
        // These all have special meaning in DOS name matching. '\' is the escaping character (which conveniently
        // is the directory separator and cannot be part of any path segment in Windows). The other three are the
        // special case wildcards that we'll convert some * and ? into. They're also valid as filenames on Unix,
        // which is not true in Windows and as such we'll escape any that occur on the input string.
        private readonly static char[] s_unixEscapeChars = { '\\', '"', '<', '>' };

        internal static void NormalizeInputs(ref string directory, ref string expression, EnumerationOptions options)
        {
            if (Path.IsPathRooted(expression))
                throw new ArgumentException(SR.Arg_Path2IsRooted, nameof(expression));

            // We always allowed breaking the passed ref directory and filter to be separated
            // any way the user wanted. Looking for "C:\foo\*.cs" could be passed as "C:\" and
            // "foo\*.cs" or "C:\foo" and "*.cs", for example. As such we need to combine and
            // split the inputs if the expression contains a directory separator.
            //
            // We also allowed for expression to be "foo\" which would translate to "foo\*".

            ReadOnlySpan<char> directoryName = Path.GetDirectoryName(expression.AsSpan());

            if (directoryName.Length != 0)
            {
                // Need to fix up the input paths
                directory = Path.Join(directory.AsSpan(), directoryName);
                expression = expression.Substring(directoryName.Length + 1);
            }

            switch (options.MatchType)
            {
                case MatchType.Win32:
                    if (string.IsNullOrEmpty(expression) || expression == "." || expression == "*.*")
                    {
                        // Historically we always treated "." as "*"
                        expression = "*";
                    }
                    else
                    {
                        if (Path.DirectorySeparatorChar != '\\' && expression.IndexOfAny(s_unixEscapeChars) != -1)
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
                    throw new ArgumentOutOfRangeException(nameof(options));
            }
        }

        private static bool MatchesPattern(string expression, ReadOnlySpan<char> name, EnumerationOptions options)
        {
            bool ignoreCase = (options.MatchCasing == MatchCasing.PlatformDefault && !PathInternal.IsCaseSensitive)
                || options.MatchCasing == MatchCasing.CaseInsensitive;

            switch (options.MatchType)
            {
                case MatchType.Simple:
                    return FileSystemName.MatchesSimpleExpression(expression.AsSpan(), name, ignoreCase);
                case MatchType.Win32:
                    return FileSystemName.MatchesWin32Expression(expression.AsSpan(), name, ignoreCase);
                default:
                    throw new ArgumentOutOfRangeException(nameof(options));
            }
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
            EnumerationOptions options)
        {
             return new FileSystemEnumerable<FileInfo>(
                directory,
                (ref FileSystemEntry entry) => (FileInfo)entry.ToFileSystemInfo(),
                options)
             {
                 ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                     !entry.IsDirectory && MatchesPattern(expression, entry.FileName, options)
             };
        }

        internal static IEnumerable<DirectoryInfo> DirectoryInfos(
            string directory,
            string expression,
            EnumerationOptions options)
        {
            return new FileSystemEnumerable<DirectoryInfo>(
               directory,
               (ref FileSystemEntry entry) => (DirectoryInfo)entry.ToFileSystemInfo(),
               options)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                    entry.IsDirectory && MatchesPattern(expression, entry.FileName, options)
            };
        }

        internal static IEnumerable<FileSystemInfo> FileSystemInfos(
            string directory,
            string expression,
            EnumerationOptions options)
        {
            return new FileSystemEnumerable<FileSystemInfo>(
               directory,
               (ref FileSystemEntry entry) => entry.ToFileSystemInfo(),
               options)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                    MatchesPattern(expression, entry.FileName, options)
            };
        }
    }
}

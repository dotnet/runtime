// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal static class FindEnumerableFactory
    {
        internal static void NormalizeInputs(ref string directory, ref string expression)
        {
            if (PathHelpers.IsPathRooted(expression))
                throw new ArgumentException(SR.Arg_Path2IsRooted, nameof(expression));

            // We always allowed breaking the passed in directory and filter to be separated
            // any way the user wanted. Looking for "C:\foo\*.cs" could be passed as "C:\" and
            // "foo\*.cs" or "C:\foo" and "*.cs", for example. As such we need to combine and
            // split the inputs if the expression contains a directory separator.
            //
            // We also allowed for expression to be "foo\" which would translate to "foo\*".

            ReadOnlySpan<char> directoryName = PathHelpers.GetDirectoryNameNoChecks(expression.AsReadOnlySpan());

            if (directoryName.Length != 0)
            {
                // Need to fix up the input paths
                directory = PathHelpers.CombineNoChecks(directory, directoryName);
                expression = expression.Substring(directoryName.Length + 1);
            }

            // Historically we always treated "." as "*"
            if (string.IsNullOrEmpty(expression) || expression == "." || expression == "*.*")
                expression = "*";
        }

        internal static FindEnumerable<string, string> UserFiles(string directory,
            string expression = "*",
            bool recursive = false)
        {
            return new FindEnumerable<string, string>(
                directory,
                (ref RawFindData findData) => FindTransforms.AsUserFullPath(ref findData),
                (ref RawFindData findData, string expr) =>
                {
                    return FindPredicates.NotDotOrDotDot(ref findData)
                        && !FindPredicates.IsDirectory(ref findData)
                        && DosMatcher.MatchPattern(expr, findData.FileName, ignoreCase: true);
                },
                DosMatcher.TranslateExpression(expression),
                recursive);
        }

        internal static FindEnumerable<string, string> UserDirectories(string directory,
            string expression = "*",
            bool recursive = false)
        {
            return new FindEnumerable<string, string>(
                directory,
                (ref RawFindData findData) => FindTransforms.AsUserFullPath(ref findData),
                (ref RawFindData findData, string expr) =>
                {
                    return FindPredicates.NotDotOrDotDot(ref findData)
                        && FindPredicates.IsDirectory(ref findData)
                        && DosMatcher.MatchPattern(expr, findData.FileName, ignoreCase: true);
                },
                DosMatcher.TranslateExpression(expression),
                recursive);
        }

        internal static FindEnumerable<string, string> UserEntries(string directory,
            string expression = "*",
            bool recursive = false)
        {
            return new FindEnumerable<string, string>(
                directory,
                (ref RawFindData findData) => FindTransforms.AsUserFullPath(ref findData),
                (ref RawFindData findData, string expr) =>
                {
                    return FindPredicates.NotDotOrDotDot(ref findData)
                        && DosMatcher.MatchPattern(expr, findData.FileName, ignoreCase: true);
                },
                DosMatcher.TranslateExpression(expression),
                recursive);
        }

        internal static FindEnumerable<FileInfo, string> FileInfos(
            string directory,
            string expression = "*",
            bool recursive = false)
        {
             return new FindEnumerable<FileInfo, string>(
                directory,
                (ref RawFindData findData) => FindTransforms.AsFileInfo(ref findData),
                (ref RawFindData findData, string expr) =>
                {
                    return FindPredicates.NotDotOrDotDot(ref findData)
                        && !FindPredicates.IsDirectory(ref findData)
                        && DosMatcher.MatchPattern(expr, findData.FileName, ignoreCase: true);
                },
                DosMatcher.TranslateExpression(expression),
                recursive);
        }

        internal static FindEnumerable<DirectoryInfo, string> DirectoryInfos(
            string directory,
            string expression = "*",
            bool recursive = false)
        {
            return new FindEnumerable<DirectoryInfo, string>(
               directory,
               (ref RawFindData findData) => FindTransforms.AsDirectoryInfo(ref findData),
               (ref RawFindData findData, string expr) =>
               {
                   return FindPredicates.NotDotOrDotDot(ref findData)
                       && FindPredicates.IsDirectory(ref findData)
                       && DosMatcher.MatchPattern(expr, findData.FileName, ignoreCase: true);
               },
               DosMatcher.TranslateExpression(expression),
               recursive);
        }

        internal static FindEnumerable<FileSystemInfo, string> FileSystemInfos(
            string directory,
            string expression = "*",
            bool recursive = false)
        {
            return new FindEnumerable<FileSystemInfo, string>(
               directory,
               (ref RawFindData findData) => FindTransforms.AsFileSystemInfo(ref findData),
               (ref RawFindData findData, string expr) =>
               {
                   return FindPredicates.NotDotOrDotDot(ref findData)
                       && DosMatcher.MatchPattern(expr, findData.FileName, ignoreCase: true);
               },
               DosMatcher.TranslateExpression(expression),
               recursive);
        }
    }
}

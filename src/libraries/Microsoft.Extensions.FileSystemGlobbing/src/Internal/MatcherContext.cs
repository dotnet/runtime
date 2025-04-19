// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Microsoft.Extensions.FileSystemGlobbing.Util;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal
{
    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class MatcherContext
    {
        private readonly DirectoryInfoBase _root;
        private readonly List<IPatternContext> _includePatternContexts = [];
        private readonly List<IPatternContext> _excludePatternContexts = [];
        private readonly List<(bool IsInclude, IPatternContext Context)> _orderedContexts = [];
        private readonly List<FilePatternMatch> _files;

        private readonly HashSet<string> _declaredLiteralFolderSegmentInString;
        private readonly HashSet<LiteralPathSegment> _declaredLiteralFileSegments = new HashSet<LiteralPathSegment>();

        private bool _declaredParentPathSegment;
        private bool _declaredWildcardPathSegment;
        private bool _preserveFilterOrder;

        private readonly StringComparison _comparisonType;

        public MatcherContext(IEnumerable<IPattern> includePatterns, IEnumerable<IPattern> excludePatterns, DirectoryInfoBase directoryInfo, StringComparison comparison)
            : this(directoryInfo, comparison)
        {
            foreach (var pattern in includePatterns)
                _includePatternContexts.Add(pattern.CreatePatternContextForInclude());

            foreach (var pattern in excludePatterns)
                _excludePatternContexts.Add(pattern.CreatePatternContextForExclude());
        }

        internal MatcherContext(List<(bool IsInclude, IPattern Pattern)> orderedPatterns, DirectoryInfoBase directoryInfo, StringComparison comparison)
            : this(directoryInfo, comparison)
        {
            foreach (var x in orderedPatterns)
            {
                IPatternContext context = x.IsInclude
                    ? x.Pattern.CreatePatternContextForInclude()
                    : x.Pattern.CreatePatternContextForExclude();

                _orderedContexts.Add((x.IsInclude, context));
            }

            _preserveFilterOrder = true;
        }

        private MatcherContext(DirectoryInfoBase directoryInfo, StringComparison comparison)
        {
            _root = directoryInfo;
            _files = [];
            _comparisonType = comparison;

            _declaredLiteralFolderSegmentInString = new HashSet<string>(StringComparisonHelper.GetStringComparer(comparison));
        }

        public PatternMatchingResult Execute()
        {
            _files.Clear();

            if (_preserveFilterOrder)
                MatchOrdered(_root, parentRelativePath: null);
            else
                Match(_root, parentRelativePath: null);

            return new PatternMatchingResult(_files, _files.Count > 0);
        }

        private void Match(DirectoryInfoBase directory, string? parentRelativePath)
        {
            // Request all the including and excluding patterns to push current directory onto their status stack.
            PushDirectory(directory);
            Declare();

            var entities = new List<FileSystemInfoBase?>();
            if (_declaredWildcardPathSegment || _declaredLiteralFileSegments.Count != 0)
            {
                entities.AddRange(directory.EnumerateFileSystemInfos());
            }
            else
            {
                IEnumerable<FileSystemInfoBase> candidates = directory.EnumerateFileSystemInfos();
                foreach (FileSystemInfoBase candidate in candidates)
                {
                    if (candidate is DirectoryInfoBase &&
                        _declaredLiteralFolderSegmentInString.Contains(candidate.Name))
                    {
                        entities.Add(candidate);
                    }
                }
            }

            if (_declaredParentPathSegment)
            {
                entities.Add(directory.GetDirectory(".."));
            }

            // collect files and sub directories
            var subDirectories = new List<DirectoryInfoBase>();
            foreach (FileSystemInfoBase? entity in entities)
            {
                if (entity is FileInfoBase fileInfo)
                {
                    PatternTestResult result = MatchPatternContexts(fileInfo, (pattern, file) => pattern.Test(file));
                    if (result.IsSuccessful)
                    {
                        _files.Add(new FilePatternMatch(
                            path: CombinePath(parentRelativePath, fileInfo.Name),
                            stem: result.Stem));
                    }

                    continue;
                }

                if (entity is DirectoryInfoBase directoryInfo)
                {
                    if (MatchPatternContextsDirectory(directoryInfo, (pattern, dir) => pattern.Test(dir)))
                    {
                        subDirectories.Add(directoryInfo);
                    }

                    continue;
                }
            }

            // Matches the sub directories recursively
            foreach (DirectoryInfoBase subDir in subDirectories)
            {
                string relativePath = CombinePath(parentRelativePath, subDir.Name);

                Match(subDir, relativePath);
            }

            // Request all the including and excluding patterns to pop their status stack.
            PopDirectory();

            void Declare()
            {
                _declaredLiteralFileSegments.Clear();
                _declaredParentPathSegment = false;
                _declaredWildcardPathSegment = false;

                foreach (IPatternContext include in _includePatternContexts)
                {
                    include.Declare(DeclareInclude);
                }
            }

            // Used to adapt Test(DirectoryInfoBase) for the below overload
            bool MatchPatternContextsDirectory<TFileInfoBase>(TFileInfoBase fileinfo, Func<IPatternContext, TFileInfoBase, bool> test)
            {
                return MatchPatternContexts(
                    fileinfo,
                    (ctx, file) =>
                    {
                        if (test(ctx, file))
                        {
                            return PatternTestResult.Success(stem: string.Empty);
                        }
                        else
                        {
                            return PatternTestResult.Failed;
                        }
                    }).IsSuccessful;
            }

            PatternTestResult MatchPatternContexts<TFileInfoBase>(TFileInfoBase fileinfo, Func<IPatternContext, TFileInfoBase, PatternTestResult> test)
            {
                PatternTestResult result = PatternTestResult.Failed;

                // If the given file/directory matches any including pattern, continues to next step.
                foreach (IPatternContext context in _includePatternContexts)
                {
                    PatternTestResult localResult = test(context, fileinfo);
                    if (localResult.IsSuccessful)
                    {
                        result = localResult;
                        break;
                    }
                }

                // If the given file/directory doesn't match any of the including pattern, returns false.
                if (!result.IsSuccessful)
                {
                    return PatternTestResult.Failed;
                }

                // If the given file/directory matches any excluding pattern, returns false.
                foreach (IPatternContext context in _excludePatternContexts)
                {
                    if (test(context, fileinfo).IsSuccessful)
                    {
                        return PatternTestResult.Failed;
                    }
                }

                return result;
            }

            void PopDirectory()
            {
                foreach (IPatternContext context in _excludePatternContexts)
                {
                    context.PopDirectory();
                }

                foreach (IPatternContext context in _includePatternContexts)
                {
                    context.PopDirectory();
                }
            }

            void PushDirectory(DirectoryInfoBase directory)
            {
                foreach (IPatternContext context in _includePatternContexts)
                {
                    context.PushDirectory(directory);
                }

                foreach (IPatternContext context in _excludePatternContexts)
                {
                    context.PushDirectory(directory);
                }
            }
        }

        private void MatchOrdered(DirectoryInfoBase directory, string? parentRelativePath)
        {
            // Request all the including and excluding patterns to push current directory onto their status stack.
            PushDirectory(directory);
            Declare();

            var entities = new List<FileSystemInfoBase?>();
            if (_declaredWildcardPathSegment || _declaredLiteralFileSegments.Count != 0)
            {
                entities.AddRange(directory.EnumerateFileSystemInfos());
            }
            else
            {
                IEnumerable<FileSystemInfoBase> candidates = directory.EnumerateFileSystemInfos();
                foreach (FileSystemInfoBase candidate in candidates)
                {
                    if (candidate is DirectoryInfoBase &&
                        _declaredLiteralFolderSegmentInString.Contains(candidate.Name))
                    {
                        entities.Add(candidate);
                    }
                }
            }

            if (_declaredParentPathSegment)
            {
                entities.Add(directory.GetDirectory(".."));
            }

            var subDirectories = new List<DirectoryInfoBase>();

            foreach (var entity in entities)
            {
                if (entity is FileInfoBase fileInfo)
                {
                    PatternTestResult result = MatchWithOrder(fileInfo);
                    if (result.IsSuccessful)
                    {
                        _files.Add(new FilePatternMatch(
                            path: CombinePath(parentRelativePath, fileInfo.Name),
                            stem: result.Stem));
                    }

                    continue;
                }

                if (entity is DirectoryInfoBase dirInfo)
                {
                    bool result = false;
                    foreach (var (isInclude, context) in _orderedContexts)
                    {
                        if ((result != isInclude) && context.Test(dirInfo))
                        {
                            result = isInclude;
                        }
                    }

                    if (result)
                    {
                        subDirectories.Add(dirInfo);
                    }

                    continue;
                }
            }

            // Matches the sub directories recursively
            foreach (DirectoryInfoBase subDir in subDirectories)
            {
                string relativePath = CombinePath(parentRelativePath, subDir.Name);

                MatchOrdered(subDir, relativePath);
            }

            // Request all the including and excluding patterns to pop their status stack.
            PopDirectory();


            PatternTestResult MatchWithOrder(FileInfoBase fileinfo)
            {
                PatternTestResult result = PatternTestResult.Failed;

                foreach ((bool isInclude, IPatternContext context) in _orderedContexts)
                {
                    if (result.IsSuccessful != isInclude)
                    {
                        var testResult = context.Test(fileinfo);
                        if (testResult.IsSuccessful)
                        {
                            result = isInclude ? testResult : PatternTestResult.Failed;
                        }
                    }
                }

                return result;
            }

            void PushDirectory(DirectoryInfoBase directory)
            {
                foreach (var (_, context) in _orderedContexts)
                {
                    context.PushDirectory(directory);
                }
            }

            void PopDirectory()
            {
                foreach (var (_, context) in _orderedContexts)
                {
                    context.PopDirectory();
                }
            }

            void Declare()
            {
                _declaredLiteralFileSegments.Clear();
                _declaredParentPathSegment = false;
                _declaredWildcardPathSegment = false;

                foreach ((bool isInclude, IPatternContext context) in _orderedContexts)
                {
                    if (isInclude)
                    {
                        context.Declare(DeclareInclude);
                    }
                }
            }
        }

        private void DeclareInclude(IPathSegment patternSegment, bool isLastSegment)
        {
            if (patternSegment is LiteralPathSegment literalSegment)
            {
                if (isLastSegment)
                {
                    _declaredLiteralFileSegments.Add(literalSegment);
                }
                else
                {
                    _declaredLiteralFolderSegmentInString.Add(literalSegment.Value);
                }
            }
            else if (patternSegment is ParentPathSegment)
            {
                _declaredParentPathSegment = true;
            }
            else if (patternSegment is WildcardPathSegment)
            {
                _declaredWildcardPathSegment = true;
            }
        }

        internal static string CombinePath(string? left, string right)
        {
            if (string.IsNullOrEmpty(left))
            {
                return right;
            }
            else
            {
                return $"{left}/{right}";
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IPatternContext[] _includePatternContexts;
        private readonly IPatternContext[] _excludePatternContexts;
        private readonly List<FilePatternMatch> _files;

        private readonly HashSet<string> _declaredLiteralFolderSegmentInString;
        private readonly HashSet<LiteralPathSegment> _declaredLiteralFileSegments = new HashSet<LiteralPathSegment>();

        private bool _declaredParentPathSegment;
        private bool _declaredWildcardPathSegment;

        private readonly StringComparison _comparisonType;

        public MatcherContext(
            IEnumerable<IPattern> includePatterns,
            IEnumerable<IPattern> excludePatterns,
            DirectoryInfoBase directoryInfo,
            StringComparison comparison)
        {
            _root = directoryInfo;
            _files = new List<FilePatternMatch>();
            _comparisonType = comparison;

            _includePatternContexts = includePatterns.Select(pattern => pattern.CreatePatternContextForInclude()).ToArray();
            _excludePatternContexts = excludePatterns.Select(pattern => pattern.CreatePatternContextForExclude()).ToArray();

            _declaredLiteralFolderSegmentInString = new HashSet<string>(StringComparisonHelper.GetStringComparer(comparison));
        }

        public PatternMatchingResult Execute()
        {
            _files.Clear();

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
                    if (MatchPatternContexts(directoryInfo, (pattern, dir) => pattern.Test(dir)))
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
        }

        private void Declare()
        {
            _declaredLiteralFileSegments.Clear();
            _declaredParentPathSegment = false;
            _declaredWildcardPathSegment = false;

            foreach (IPatternContext include in _includePatternContexts)
            {
                include.Declare(DeclareInclude);
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

        // Used to adapt Test(DirectoryInfoBase) for the below overload
        private bool MatchPatternContexts<TFileInfoBase>(TFileInfoBase fileinfo, Func<IPatternContext, TFileInfoBase, bool> test)
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

        private PatternTestResult MatchPatternContexts<TFileInfoBase>(TFileInfoBase fileinfo, Func<IPatternContext, TFileInfoBase, PatternTestResult> test)
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

        private void PopDirectory()
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

        private void PushDirectory(DirectoryInfoBase directory)
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
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Microsoft.Extensions.FileSystemGlobbing.Util;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal
{
    public class MatcherContext
    {
        private readonly DirectoryInfoBase _root;
        private readonly List<IPatternContext> _includePatternContexts;
        private readonly List<IPatternContext> _excludePatternContexts;
        private readonly List<FilePatternMatch> _files;

        private readonly HashSet<string> _declaredLiteralFolderSegmentInString;
        private readonly HashSet<LiteralPathSegment> _declaredLiteralFolderSegments = new HashSet<LiteralPathSegment>();
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

            _includePatternContexts = includePatterns.Select(pattern => pattern.CreatePatternContextForInclude()).ToList();
            _excludePatternContexts = excludePatterns.Select(pattern => pattern.CreatePatternContextForExclude()).ToList();

            _declaredLiteralFolderSegmentInString = new HashSet<string>(StringComparisonHelper.GetStringComparer(comparison));
        }

        public PatternMatchingResult Execute()
        {
            _files.Clear();

            Match(_root, parentRelativePath: null);

            return new PatternMatchingResult(_files, _files.Count > 0);
        }

        private void Match(DirectoryInfoBase directory, string parentRelativePath)
        {
            // Request all the including and excluding patterns to push current directory onto their status stack.
            PushDirectory(directory);
            Declare();

            var entities = new List<FileSystemInfoBase>();
            if (_declaredWildcardPathSegment || _declaredLiteralFileSegments.Any())
            {
                entities.AddRange(directory.EnumerateFileSystemInfos());
            }
            else
            {
                var candidates = directory.EnumerateFileSystemInfos().OfType<DirectoryInfoBase>();
                foreach (var candidate in candidates)
                {
                    if (_declaredLiteralFolderSegmentInString.Contains(candidate.Name))
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
            foreach (var entity in entities)
            {
                var fileInfo = entity as FileInfoBase;
                if (fileInfo != null)
                {
                    var result = MatchPatternContexts(fileInfo, (pattern, file) => pattern.Test(file));
                    if (result.IsSuccessful)
                    {
                        _files.Add(new FilePatternMatch(
                            path: CombinePath(parentRelativePath, fileInfo.Name),
                            stem: result.Stem));
                    }

                    continue;
                }

                var directoryInfo = entity as DirectoryInfoBase;
                if (directoryInfo != null)
                {
                    if (MatchPatternContexts(directoryInfo, (pattern, dir) => pattern.Test(dir)))
                    {
                        subDirectories.Add(directoryInfo);
                    }

                    continue;
                }
            }

            // Matches the sub directories recursively
            foreach (var subDir in subDirectories)
            {
                var relativePath = CombinePath(parentRelativePath, subDir.Name);

                Match(subDir, relativePath);
            }

            // Request all the including and excluding patterns to pop their status stack.
            PopDirectory();
        }

        private void Declare()
        {
            _declaredLiteralFileSegments.Clear();
            _declaredLiteralFolderSegments.Clear();
            _declaredParentPathSegment = false;
            _declaredWildcardPathSegment = false;

            foreach (var include in _includePatternContexts)
            {
                include.Declare(DeclareInclude);
            }
        }

        private void DeclareInclude(IPathSegment patternSegment, bool isLastSegment)
        {
            var literalSegment = patternSegment as LiteralPathSegment;
            if (literalSegment != null)
            {
                if (isLastSegment)
                {
                    _declaredLiteralFileSegments.Add(literalSegment);
                }
                else
                {
                    _declaredLiteralFolderSegments.Add(literalSegment);
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

        internal static string CombinePath(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
            {
                return right;
            }
            else
            {
                return string.Format("{0}/{1}", left, right);
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
            var result = PatternTestResult.Failed;

            // If the given file/directory matches any including pattern, continues to next step.
            foreach (var context in _includePatternContexts)
            {
                var localResult = test(context, fileinfo);
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
            foreach (var context in _excludePatternContexts)
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
            foreach (var context in _excludePatternContexts)
            {
                context.PopDirectory();
            }

            foreach (var context in _includePatternContexts)
            {
                context.PopDirectory();
            }
        }

        private void PushDirectory(DirectoryInfoBase directory)
        {
            foreach (var context in _includePatternContexts)
            {
                context.PushDirectory(directory);
            }

            foreach (var context in _excludePatternContexts)
            {
                context.PushDirectory(directory);
            }
        }
    }
}

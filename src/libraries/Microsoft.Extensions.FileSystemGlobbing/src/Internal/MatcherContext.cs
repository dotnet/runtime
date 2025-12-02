// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts;
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
        private readonly IPatternContext _patternContext;
        private readonly List<FilePatternMatch> _files;

        private readonly HashSet<string> _declaredLiteralFolderSegmentInString;
        private readonly HashSet<LiteralPathSegment> _declaredLiteralFileSegments = new HashSet<LiteralPathSegment>();

        private bool _declaredParentPathSegment;
        private bool _declaredWildcardPathSegment;

        public MatcherContext(IEnumerable<IPattern> includePatterns, IEnumerable<IPattern> excludePatterns, DirectoryInfoBase directoryInfo, StringComparison comparison)
        {
            _root = directoryInfo;
            _files = [];
            _declaredLiteralFolderSegmentInString = new HashSet<string>(StringComparisonHelper.GetStringComparer(comparison));

            IPatternContext[] includePatternContexts = includePatterns.Select(pattern => pattern.CreatePatternContextForInclude()).ToArray();
            IPatternContext[] excludePatternContexts = excludePatterns.Select(pattern => pattern.CreatePatternContextForExclude()).ToArray();

            _patternContext = new IncludesFirstCompositePatternContext(includePatternContexts, excludePatternContexts);
        }

        internal MatcherContext(List<IncludeOrExcludeValue<IPattern>> orderedPatterns, DirectoryInfoBase directoryInfo, StringComparison comparison)
        {
            _root = directoryInfo;
            _files = [];
            _declaredLiteralFolderSegmentInString = new HashSet<string>(StringComparisonHelper.GetStringComparer(comparison));

            IncludeOrExcludeValue<IPatternContext>[] includeOrExcludePatternContexts = orderedPatterns
                .Select(item => new IncludeOrExcludeValue<IPatternContext>
                {
                    Value = item.IsInclude ? item.Value.CreatePatternContextForInclude() : item.Value.CreatePatternContextForExclude(),
                    IsInclude = item.IsInclude
                })
                .ToArray();

            _patternContext = new PreserveOrderCompositePatternContext(includeOrExcludePatternContexts);
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
            _patternContext.PushDirectory(directory);
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
                    PatternTestResult result = _patternContext.Test(fileInfo);
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
                    if (_patternContext.Test(directoryInfo))
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
            _patternContext.PopDirectory();
        }

        private void Declare()
        {
            _declaredLiteralFileSegments.Clear();
            _declaredParentPathSegment = false;
            _declaredWildcardPathSegment = false;

            _patternContext.Declare(DeclareInclude);
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Enumeration;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts;
using Microsoft.Extensions.FileSystemGlobbing.Util;
using Touki.Io;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal
{
    internal sealed class ToukiMatcherPlan
    {
        private ToukiMatcherPlan(
            ToukiPattern[] includePatterns,
            ToukiPattern[] excludePatterns,
            DirectDirectoryExclusion[]? directDirectoryExclusions,
            ExclusionWinsPatternMatcher exclusionWinsMatcher)
        {
            IncludePatterns = includePatterns;
            ExcludePatterns = excludePatterns;
            DirectDirectoryExclusions = directDirectoryExclusions;
            ExclusionWinsMatcher = exclusionWinsMatcher;
        }

        private ToukiMatcherPlan(
            IncludeOrExcludeValue<ToukiPattern>[] orderedPatterns,
            OrderedPatternMatcher orderedMatcher)
        {
            OrderedPatterns = orderedPatterns;
            OrderedMatcher = orderedMatcher;
        }

        public ToukiPattern[]? IncludePatterns { get; }

        public ToukiPattern[]? ExcludePatterns { get; }

        public IncludeOrExcludeValue<ToukiPattern>[]? OrderedPatterns { get; }

        public DirectDirectoryExclusion[]? DirectDirectoryExclusions { get; }

        public ExclusionWinsPatternMatcher? ExclusionWinsMatcher { get; }

        public OrderedPatternMatcher? OrderedMatcher { get; }

        public static bool TryCreate(
            IReadOnlyList<IPattern> includePatterns,
            IReadOnlyList<IPattern> excludePatterns,
            StringComparison comparison,
            [NotNullWhen(true)] out ToukiMatcherPlan? plan)
        {
            if (!TryGetPatterns(includePatterns, comparison, out ToukiPattern[]? includes)
                || includes.Length == 0
                || !TryGetPatterns(excludePatterns, comparison, out ToukiPattern[]? excludes))
            {
                plan = null;
                return false;
            }

            var matcher = new ExclusionWinsPatternMatcher(includes, excludes);

            DirectDirectoryExclusion[]? directDirectoryExclusions =
                TryCreateDirectDirectoryExclusions(excludes, comparison, out DirectDirectoryExclusion[]? direct)
                    ? direct
                    : null;

            plan = new ToukiMatcherPlan(includes, excludes, directDirectoryExclusions, matcher);
            return true;
        }

        public static bool TryCreate(
            IReadOnlyList<IncludeOrExcludeValue<IPattern>> orderedPatterns,
            StringComparison comparison,
            [NotNullWhen(true)] out ToukiMatcherPlan? plan)
        {
            var patterns = new IncludeOrExcludeValue<ToukiPattern>[orderedPatterns.Count];
            bool hasInclude = false;

            for (int index = 0; index < orderedPatterns.Count; index++)
            {
                IncludeOrExcludeValue<IPattern> item = orderedPatterns[index];
                ToukiPattern pattern = ToukiPattern.Create(item.Value, comparison);
                if (!pattern.TryCompile())
                {
                    plan = null;
                    return false;
                }

                patterns[index] = new IncludeOrExcludeValue<ToukiPattern>
                {
                    IsInclude = item.IsInclude,
                    Value = pattern
                };

                if (item.IsInclude)
                {
                    hasInclude = true;
                }
            }

            if (!hasInclude)
            {
                plan = null;
                return false;
            }

            plan = new ToukiMatcherPlan(patterns, new OrderedPatternMatcher(patterns));
            return true;
        }

        public PatternMatchingResult Execute(DirectoryInfoBase directoryInfo, StringComparison comparison) =>
            new ToukiMatcherContext(this, directoryInfo, comparison).Execute();

        private static bool TryGetPatterns(
            IReadOnlyList<IPattern> patterns,
            StringComparison comparison,
            [NotNullWhen(true)] out ToukiPattern[]? toukiPatterns)
        {
            var result = new ToukiPattern[patterns.Count];
            for (int index = 0; index < patterns.Count; index++)
            {
                IPattern item = patterns[index];
                ToukiPattern pattern = ToukiPattern.Create(item, comparison);
                if (!pattern.TryCompile())
                {
                    toukiPatterns = null;
                    return false;
                }

                result[index] = pattern;
            }

            toukiPatterns = result;
            return true;
        }

        private static bool TryCreateDirectDirectoryExclusions(
            ToukiPattern[] excludes,
            StringComparison comparison,
            [NotNullWhen(true)] out DirectDirectoryExclusion[]? directExcludes)
        {
            if (excludes.Length == 0)
            {
                directExcludes = [];
                return true;
            }

            var result = new DirectDirectoryExclusion[excludes.Length];
            for (int index = 0; index < excludes.Length; index++)
            {
                if (!DirectDirectoryExclusion.TryCreate(excludes[index], comparison, out result[index]))
                {
                    directExcludes = null;
                    return false;
                }
            }

            directExcludes = result;
            return true;
        }
    }

    internal readonly struct DirectDirectoryExclusion
    {
        private readonly ToukiPattern? _fileNamePattern;
        private readonly string? _literalPath;
        private readonly StringComparison _comparison;
        private readonly bool _matchAll;
        private readonly bool _pruneCandidate;

        private DirectDirectoryExclusion(ToukiPattern fileNamePattern)
        {
            _fileNamePattern = fileNamePattern;
        }

        private DirectDirectoryExclusion(
            string literalPath,
            StringComparison comparison,
            bool matchAll,
            bool pruneCandidate)
        {
            _literalPath = literalPath;
            _comparison = comparison;
            _matchAll = matchAll;
            _pruneCandidate = pruneCandidate;
        }

        public bool Matches(ReadOnlySpan<char> directoryPrefix, ReadOnlySpan<char> directoryName)
        {
            if (_fileNamePattern is not null)
            {
                return _fileNamePattern.MatchesFile(directoryPrefix, directoryName);
            }

            if (_matchAll)
            {
                return true;
            }

            if (!_pruneCandidate)
            {
                return false;
            }

            ReadOnlySpan<char> literalPath = _literalPath;
            return directoryPrefix.Length + directoryName.Length == literalPath.Length
                && directoryPrefix.Equals(literalPath[..directoryPrefix.Length], _comparison)
                && directoryName.Equals(literalPath[directoryPrefix.Length..], _comparison);
        }

        public bool PrunesDescendants(ReadOnlySpan<char> directoryPrefix)
        {
            if (_fileNamePattern is not null || _matchAll || _pruneCandidate)
            {
                return false;
            }

            ReadOnlySpan<char> literalPath = _literalPath;
            return directoryPrefix.Length == literalPath.Length + 1
                && directoryPrefix[^1] == '/'
                && directoryPrefix[..^1].Equals(literalPath, _comparison);
        }

        public static bool TryCreate(
            ToukiPattern pattern,
            StringComparison comparison,
            out DirectDirectoryExclusion directExclude)
        {
            if (pattern.LegacyPattern is IRaggedPattern
                {
                    StartsWith.Count: 0,
                    Contains.Count: 0,
                    EndsWith.Count: 1,
                    Segments.Count: > 1
                } fileNamePattern
                && fileNamePattern.Segments[0] is RecursiveWildcardSegment)
            {
                directExclude = new DirectDirectoryExclusion(pattern);
                return true;
            }

            if (pattern.LegacyPattern is IRaggedPattern
                {
                    Contains.Count: 0,
                    EndsWith.Count: 0,
                    Segments.Count: > 0
                } recursivePattern
                && recursivePattern.Segments[^1] is RecursiveWildcardSegment)
            {
                string[] literalSegments = new string[recursivePattern.StartsWith.Count];
                for (int index = 0; index < literalSegments.Length; index++)
                {
                    if (recursivePattern.StartsWith[index] is not LiteralPathSegment literal)
                    {
                        directExclude = default;
                        return false;
                    }

                    literalSegments[index] = literal.Value;
                }

                directExclude = new DirectDirectoryExclusion(
                    string.Join('/', literalSegments),
                    comparison,
                    matchAll: literalSegments.Length == 0,
                    pruneCandidate: literalSegments.Length <= 1);
                return true;
            }

            directExclude = default;
            return false;
        }
    }

    internal sealed class ToukiMatcherContext
    {
        private readonly DirectoryInfoBase _root;
        private readonly IPatternContext? _directoryContext;
        private readonly ExclusionWinsPatternMatcher? _exclusionWinsMatcher;
        private readonly OrderedPatternMatcher? _orderedMatcher;
        private readonly DirectDirectoryExclusion[]? _directDirectoryExclusions;
        private readonly bool _hasParentPathSegment;
        private readonly List<FilePatternMatch> _files = [];
        private readonly StringComparer _comparer;
        private HashSet<string>? _declaredLiteralFolderSegments;
        private bool _declaredLiteralFileSegment;
        private bool _declaredParentPathSegment;
        private bool _declaredWildcardPathSegment;

        public ToukiMatcherContext(
            ToukiMatcherPlan plan,
            DirectoryInfoBase directoryInfo,
            StringComparison comparison)
        {
            _root = directoryInfo;
            _comparer = StringComparisonHelper.GetStringComparer(comparison);
            _exclusionWinsMatcher = plan.ExclusionWinsMatcher;
            _orderedMatcher = plan.OrderedMatcher;
            _directDirectoryExclusions = plan.DirectDirectoryExclusions;
            _hasParentPathSegment = HasParentPathSegment(plan);

            if (directoryInfo.GetType() == typeof(DirectoryInfoWrapper)
                && !_hasParentPathSegment
                && CanTraverseAllDirectories(plan))
            {
                _directoryContext = null;
            }
            else if (plan.OrderedPatterns is { } orderedPatterns)
            {
                var contexts = new IncludeOrExcludeValue<IPatternContext>[orderedPatterns.Length];
                for (int index = 0; index < orderedPatterns.Length; index++)
                {
                    IncludeOrExcludeValue<ToukiPattern> item = orderedPatterns[index];
                    contexts[index] = new IncludeOrExcludeValue<IPatternContext>
                    {
                        IsInclude = item.IsInclude,
                        Value = CreateDirectoryPatternContext(item.Value, item.IsInclude)
                    };
                }

                _directoryContext = new PreserveOrderCompositePatternContext(contexts);
            }
            else
            {
                ToukiPattern[] includePatterns = plan.IncludePatterns!;
                var includeContexts = new IPatternContext[includePatterns.Length];
                for (int index = 0; index < includePatterns.Length; index++)
                {
                    includeContexts[index] = CreateDirectoryPatternContext(
                        includePatterns[index],
                        isInclude: true);
                }

                ToukiPattern[] excludePatterns = plan.ExcludePatterns!;
                var excludeContexts = new IPatternContext[excludePatterns.Length];
                for (int index = 0; index < excludePatterns.Length; index++)
                {
                    excludeContexts[index] = CreateDirectoryPatternContext(
                        excludePatterns[index],
                        isInclude: false);
                }

                _directoryContext = new IncludesFirstCompositePatternContext(includeContexts, excludeContexts);
            }
        }

        public PatternMatchingResult Execute()
        {
            Match(_root, parentRelativePath: null);
            return new PatternMatchingResult(_files, _files.Count > 0);
        }

        private void Match(DirectoryInfoBase directory, string? parentRelativePath)
        {
            if (directory.GetType() == typeof(DirectoryInfoWrapper))
            {
                var wrapper = (DirectoryInfoWrapper)directory;
                if (_hasParentPathSegment)
                {
                    Match(wrapper, parentRelativePath);
                }
                else
                {
                    DirectoryInfo wrappedDirectory = wrapper.DirectoryInfo;
                    if (_directoryContext is null)
                    {
                        wrappedDirectory.Refresh();
                        if (wrappedDirectory.Exists)
                        {
                            int rootPrefixLength = wrappedDirectory.FullName.Length
                                + (Path.EndsInDirectorySeparator(wrappedDirectory.FullName) ? 0 : 1);
                            MatchFileSystemPath(wrappedDirectory.FullName, rootPrefixLength);
                        }
                    }
                    else
                    {
                        MatchFileSystemDirectory(wrapper, wrappedDirectory.FullName);
                    }
                }

                return;
            }

            _directoryContext!.PushDirectory(directory);
            Declare();

            var entities = new List<FileSystemInfoBase?>();
            if (_declaredWildcardPathSegment || _declaredLiteralFileSegment)
            {
                entities.AddRange(directory.EnumerateFileSystemInfos());
            }
            else
            {
                foreach (FileSystemInfoBase candidate in directory.EnumerateFileSystemInfos())
                {
                    if (candidate is DirectoryInfoBase
                        && _declaredLiteralFolderSegments?.Contains(candidate.Name) == true)
                    {
                        entities.Add(candidate);
                    }
                }
            }

            if (_declaredParentPathSegment)
            {
                entities.Add(directory.GetDirectory(".."));
            }

            string directoryPrefix = parentRelativePath is null ? string.Empty : parentRelativePath + "/";
            var subDirectories = new List<DirectoryInfoBase>();
            foreach (FileSystemInfoBase? entity in entities)
            {
                if (entity is FileInfoBase fileInfo)
                {
                    PatternTestResult match = TestFile(directoryPrefix, fileInfo.Name);
                    if (match.IsSuccessful)
                    {
                        _files.Add(new FilePatternMatch(
                            MatcherContext.CombinePath(parentRelativePath, fileInfo.Name),
                            match.Stem));
                    }
                }
                else if (entity is DirectoryInfoBase directoryInfo && _directoryContext.Test(directoryInfo))
                {
                    subDirectories.Add(directoryInfo);
                }
            }

            foreach (DirectoryInfoBase subDirectory in subDirectories)
            {
                Match(subDirectory, MatcherContext.CombinePath(parentRelativePath, subDirectory.Name));
            }

            _directoryContext.PopDirectory();
        }

        private void MatchFileSystemDirectory(
            DirectoryInfoBase directory,
            string physicalPath)
        {
            _directoryContext!.PushDirectory(directory);
            Declare();
            bool enumerateFiles = _declaredWildcardPathSegment || _declaredLiteralFileSegment;

            FileSystemDirectoryInfo? firstSubDirectory = null;

            bool exists;
            if (directory is DirectoryInfoWrapper wrapper)
            {
                wrapper.DirectoryInfo.Refresh();
                exists = wrapper.DirectoryInfo.Exists;
            }
            else
            {
                exists = Directory.Exists(physicalPath);
            }

            if (exists)
            {
                WrapperFileSystemEnumerator? enumerator = null;
                try
                {
                    enumerator = new WrapperFileSystemEnumerator(
                        this,
                        physicalPath,
                        directory,
                        enumerateFiles);
                }
                catch (DirectoryNotFoundException)
                {
                }

                if (enumerator is not null)
                {
                    using (enumerator)
                    {
                        while (enumerator.MoveNext())
                        {
                        }
                    }

                    firstSubDirectory = enumerator.FirstSubDirectory;
                }
            }

            for (FileSystemDirectoryInfo? subDirectory = firstSubDirectory;
                subDirectory is not null;
                subDirectory = subDirectory.Next)
            {
                MatchFileSystemDirectory(
                    subDirectory,
                    subDirectory.FullName);
            }

            _directoryContext.PopDirectory();
        }

        private void MatchFileSystemPath(string physicalPath, int rootPrefixLength)
        {
            string[]? subDirectories = null;
            int subDirectoryCount = 0;
            WrapperFileSystemEnumerator? enumerator = null;
            try
            {
                enumerator = new WrapperFileSystemEnumerator(this, physicalPath, rootPrefixLength);
            }
            catch (DirectoryNotFoundException)
            {
            }

            if (enumerator is not null)
            {
                using (enumerator)
                {
                    while (enumerator.MoveNext())
                    {
                    }

                    enumerator.TakePathSubDirectories(out subDirectories, out subDirectoryCount);
                }
            }

            if (subDirectories is null)
            {
                return;
            }

            try
            {
                for (int index = 0; index < subDirectoryCount; index++)
                {
                    MatchFileSystemPath(subDirectories[index], rootPrefixLength);
                }
            }
            finally
            {
                ArrayPool<string>.Shared.Return(subDirectories, clearArray: true);
            }
        }

        private void Match(DirectoryInfoWrapper directory, string? parentRelativePath)
        {
            _directoryContext!.PushDirectory(directory);
            Declare();

            bool enumerateFiles = _declaredWildcardPathSegment || _declaredLiteralFileSegment;
            string directoryPrefix = parentRelativePath is null ? string.Empty : parentRelativePath + "/";
            var subDirectories = new List<DirectoryInfoWrapper>();

            DirectoryInfo wrappedDirectory = directory.DirectoryInfo;
            wrappedDirectory.Refresh();
            if (wrappedDirectory.Exists)
            {
                IEnumerable<FileSystemInfo> fileSystemInfos;
                try
                {
                    fileSystemInfos = wrappedDirectory.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
                }
                catch (DirectoryNotFoundException)
                {
                    fileSystemInfos = [];
                }

                foreach (FileSystemInfo fileSystemInfo in fileSystemInfos)
                {
                    if (fileSystemInfo is DirectoryInfo childDirectory)
                    {
                        if (enumerateFiles || _declaredLiteralFolderSegments?.Contains(childDirectory.Name) == true)
                        {
                            var childWrapper = new DirectoryInfoWrapper(childDirectory);
                            if (_directoryContext.Test(childWrapper))
                            {
                                subDirectories.Add(childWrapper);
                            }
                        }
                    }
                    else if (enumerateFiles)
                    {
                        PatternTestResult match = TestFile(directoryPrefix, fileSystemInfo.Name);
                        if (match.IsSuccessful)
                        {
                            _files.Add(new FilePatternMatch(
                                MatcherContext.CombinePath(parentRelativePath, fileSystemInfo.Name),
                                match.Stem));
                        }
                    }
                }
            }

            if (_declaredParentPathSegment
                && directory.GetDirectory("..") is DirectoryInfoWrapper parentDirectory
                && _directoryContext.Test(parentDirectory))
            {
                subDirectories.Add(parentDirectory);
            }

            foreach (DirectoryInfoWrapper subDirectory in subDirectories)
            {
                Match(subDirectory, MatcherContext.CombinePath(parentRelativePath, subDirectory.Name));
            }

            _directoryContext.PopDirectory();
        }

        private PatternTestResult TestFile(string directoryPrefix, string fileName)
        {
            if (!TryMatchFile(directoryPrefix, fileName, out ToukiPattern? pattern))
            {
                return PatternTestResult.Failed;
            }

            return PatternTestResult.Success(pattern.CalculateStem(directoryPrefix, fileName));
        }

        private bool TryMatchFile(
            ReadOnlySpan<char> directoryPrefix,
            ReadOnlySpan<char> fileName,
            [NotNullWhen(true)] out ToukiPattern? pattern)
        {
            ToukiPattern? matchingInclude;
            bool matched = _orderedMatcher is not null
                ? _orderedMatcher.TryMatchFile(directoryPrefix, fileName, out matchingInclude)
                : _exclusionWinsMatcher!.TryMatchFile(directoryPrefix, fileName, out matchingInclude);

            pattern = matchingInclude;
            return matched;
        }

        private bool IsDirectoryExcluded(
            ReadOnlySpan<char> directoryPrefix,
            ReadOnlySpan<char> directoryName)
        {
            foreach (DirectDirectoryExclusion exclude in _directDirectoryExclusions!)
            {
                if (exclude.Matches(directoryPrefix, directoryName))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldPruneSubDirectories(ReadOnlySpan<char> directoryPrefix)
        {
            foreach (DirectDirectoryExclusion exclude in _directDirectoryExclusions!)
            {
                if (exclude.PrunesDescendants(directoryPrefix))
                {
                    return true;
                }
            }

            return false;
        }

        private void Declare()
        {
            _declaredLiteralFileSegment = false;
            _declaredLiteralFolderSegments?.Clear();
            _declaredParentPathSegment = false;
            _declaredWildcardPathSegment = false;
            _directoryContext!.Declare(DeclareInclude);
        }

        private void DeclareInclude(IPathSegment patternSegment, bool isLastSegment)
        {
            if (patternSegment is LiteralPathSegment literalSegment)
            {
                if (isLastSegment)
                {
                    _declaredLiteralFileSegment = true;
                }
                else
                {
                    (_declaredLiteralFolderSegments ??= new HashSet<string>(_comparer)).Add(literalSegment.Value);
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

        private static bool HasParentPathSegment(ToukiMatcherPlan plan)
        {
            if (plan.OrderedPatterns is { } orderedPatterns)
            {
                foreach (IncludeOrExcludeValue<ToukiPattern> item in orderedPatterns)
                {
                    if (HasParentPathSegment(item.Value.LegacyPattern))
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (ToukiPattern pattern in plan.IncludePatterns!)
            {
                if (HasParentPathSegment(pattern.LegacyPattern))
                {
                    return true;
                }
            }

            foreach (ToukiPattern pattern in plan.ExcludePatterns!)
            {
                if (HasParentPathSegment(pattern.LegacyPattern))
                {
                    return true;
                }
            }

            return false;
        }

        private static IPatternContext CreateDirectoryPatternContext(
            ToukiPattern pattern,
            bool isInclude)
        {
            IPatternContext context = isInclude
                ? pattern.LegacyPattern.CreatePatternContextForInclude()
                : pattern.LegacyPattern.CreatePatternContextForExclude();

            if (context is PatternContextLinear linearContext)
            {
                linearContext.TrackStem = false;
            }
            else if (context is PatternContextRagged raggedContext)
            {
                raggedContext.TrackStem = false;
            }

            return context;
        }

        private static bool CanTraverseAllDirectories(ToukiMatcherPlan plan)
        {
            if (plan.OrderedPatterns is not null || plan.DirectDirectoryExclusions is null)
            {
                return false;
            }

            foreach (ToukiPattern pattern in plan.IncludePatterns!)
            {
                if (pattern.LegacyPattern is IRaggedPattern { StartsWith.Count: 0 })
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasParentPathSegment(IPattern pattern)
        {
            IList<IPathSegment> segments = pattern switch
            {
                ILinearPattern linearPattern => linearPattern.Segments,
                IRaggedPattern raggedPattern => raggedPattern.Segments,
                _ => []
            };

            foreach (IPathSegment segment in segments)
            {
                if (segment is ParentPathSegment)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class WrapperFileSystemEnumerator : FileSystemEnumerator<bool>
        {
            private static readonly EnumerationOptions s_options = new()
            {
                AttributesToSkip = 0,
                IgnoreInaccessible = false,
                MatchType = MatchType.Win32
            };

            private readonly ToukiMatcherContext _context;
            private readonly DirectoryInfoBase _directory;
            private readonly bool _enumerateFiles;
            private char[]? _directoryPrefixBuffer;
            private readonly int _directoryPrefixLength;
            private FileSystemDirectoryInfo? _lastSubDirectory;
            private string[]? _pathSubDirectories;
            private int _pathSubDirectoryCount;
            private readonly bool _pruneSubDirectories;

            public WrapperFileSystemEnumerator(
                ToukiMatcherContext context,
                string directory,
                DirectoryInfoBase directoryInfo,
                bool enumerateFiles)
                : base(directory, s_options)
            {
                _context = context;
                _directory = directoryInfo;
                _enumerateFiles = enumerateFiles;

                if (directoryInfo is FileSystemDirectoryInfo fileSystemDirectory)
                {
                    _directoryPrefixLength = fileSystemDirectory.RelativePrefixLength;
                    _directoryPrefixBuffer = ArrayPool<char>.Shared.Rent(_directoryPrefixLength);
                    BuildDirectoryPrefix(
                        fileSystemDirectory,
                        _directoryPrefixBuffer.AsSpan(0, _directoryPrefixLength));
                }
            }

            public WrapperFileSystemEnumerator(
                ToukiMatcherContext context,
                string directory,
                int rootPrefixLength)
                : base(directory, s_options)
            {
                _context = context;
                _directory = null!;
                _enumerateFiles = true;

                if (directory.Length >= rootPrefixLength)
                {
                    ReadOnlySpan<char> relativePath = directory.AsSpan(rootPrefixLength);
                    _directoryPrefixLength = relativePath.Length + (relativePath.IsEmpty ? 0 : 1);
                    if (_directoryPrefixLength != 0)
                    {
                        _directoryPrefixBuffer = ArrayPool<char>.Shared.Rent(_directoryPrefixLength);
                        BuildDirectoryPrefix(
                            relativePath,
                            _directoryPrefixBuffer.AsSpan(0, _directoryPrefixLength));
                    }
                }

                _pruneSubDirectories = _context.ShouldPruneSubDirectories(
                    _directoryPrefixBuffer.AsSpan(0, _directoryPrefixLength));
            }

            public FileSystemDirectoryInfo? FirstSubDirectory { get; private set; }

            public void TakePathSubDirectories(
                out string[]? subDirectories,
                out int count)
            {
                subDirectories = _pathSubDirectories;
                count = _pathSubDirectoryCount;
                _pathSubDirectories = null;
                _pathSubDirectoryCount = 0;
            }

            protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
            {
                if (entry.IsDirectory)
                {
                    if (_directory is null)
                    {
                        if (_pruneSubDirectories)
                        {
                            return false;
                        }

                        ReadOnlySpan<char> directoryPrefix = _directoryPrefixBuffer.AsSpan(0, _directoryPrefixLength);
                        if (_context.IsDirectoryExcluded(directoryPrefix, entry.FileName))
                        {
                            return false;
                        }

                        if (_pathSubDirectories is null)
                        {
                            _pathSubDirectories = ArrayPool<string>.Shared.Rent(4);
                        }
                        else if (_pathSubDirectoryCount == _pathSubDirectories.Length)
                        {
                            string[] replacement = ArrayPool<string>.Shared.Rent(_pathSubDirectories.Length * 2);
                            Array.Copy(_pathSubDirectories, replacement, _pathSubDirectoryCount);
                            ArrayPool<string>.Shared.Return(_pathSubDirectories, clearArray: true);
                            _pathSubDirectories = replacement;
                        }

                        _pathSubDirectories[_pathSubDirectoryCount++] = entry.ToFullPath();
                        return false;
                    }

                    string name = entry.FileName.ToString();
                    if (_enumerateFiles || _context._declaredLiteralFolderSegments?.Contains(name) == true)
                    {
                        var directory = new FileSystemDirectoryInfo(entry.ToFullPath(), name, _directory);
                        if (_context._directoryContext!.Test(directory))
                        {
                            if (_lastSubDirectory is null)
                            {
                                FirstSubDirectory = directory;
                            }
                            else
                            {
                                _lastSubDirectory.Next = directory;
                            }

                            _lastSubDirectory = directory;
                        }
                    }
                }
                else if (_enumerateFiles)
                {
                    ReadOnlySpan<char> directoryPrefix = _directoryPrefixBuffer.AsSpan(0, _directoryPrefixLength);
                    if (_context.TryMatchFile(directoryPrefix, entry.FileName, out ToukiPattern? pattern))
                    {
                        string path = directoryPrefix.Length == 0
                            ? entry.FileName.ToString()
                            : string.Concat(directoryPrefix, entry.FileName);
                        _context._files.Add(new FilePatternMatch(
                            path,
                            pattern.CalculateStem(directoryPrefix, entry.FileName, path)));
                    }
                }

                return false;
            }

            protected override bool TransformEntry(ref FileSystemEntry entry) => false;

            protected override void Dispose(bool disposing)
            {
                if (_directoryPrefixBuffer is { } buffer)
                {
                    _directoryPrefixBuffer = null;
                    ArrayPool<char>.Shared.Return(buffer);
                }

                if (_pathSubDirectories is { } subDirectories)
                {
                    _pathSubDirectories = null;
                    _pathSubDirectoryCount = 0;
                    ArrayPool<string>.Shared.Return(subDirectories, clearArray: true);
                }

                base.Dispose(disposing);
            }

            private static void BuildDirectoryPrefix(
                FileSystemDirectoryInfo directory,
                Span<char> destination)
            {
                int offset = destination.Length;
                for (FileSystemDirectoryInfo? current = directory;
                    current is not null;
                    current = current.ParentDirectory as FileSystemDirectoryInfo)
                {
                    destination[--offset] = '/';
                    offset -= current.Name.Length;
                    current.Name.CopyTo(destination[offset..]);
                }

                Debug.Assert(offset == 0);
            }

            private static void BuildDirectoryPrefix(
                ReadOnlySpan<char> relativePath,
                Span<char> destination)
            {
                for (int index = 0; index < relativePath.Length; index++)
                {
                    char value = relativePath[index];
                    destination[index] = value == Path.DirectorySeparatorChar
                        || value == Path.AltDirectorySeparatorChar
                            ? '/'
                            : value;
                }

                destination[^1] = '/';
            }
        }

        private sealed class FileSystemDirectoryInfo : DirectoryInfoBase
        {
            public FileSystemDirectoryInfo(
                string fullName,
                string name,
                DirectoryInfoBase parentDirectory)
            {
                FullName = fullName;
                Name = name;
                ParentDirectory = parentDirectory;
                RelativePrefixLength = name.Length + 1
                    + ((parentDirectory as FileSystemDirectoryInfo)?.RelativePrefixLength ?? 0);
            }

            public override string FullName { get; }

            public override string Name { get; }

            public override DirectoryInfoBase ParentDirectory { get; }

            public FileSystemDirectoryInfo? Next { get; set; }

            public int RelativePrefixLength { get; }

            public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos() =>
                throw new NotSupportedException();

            public override DirectoryInfoBase GetDirectory(string path) =>
                throw new NotSupportedException();

            public override FileInfoBase GetFile(string path) =>
                throw new NotSupportedException();
        }
    }
}

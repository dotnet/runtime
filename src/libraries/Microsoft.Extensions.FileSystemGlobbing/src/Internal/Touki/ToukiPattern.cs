// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Touki.Io.Globbing;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal
{
    internal sealed class ToukiPattern
    {
        private readonly IPattern _legacyPattern;
        private readonly bool _ignoreCase;
        private readonly int _stemStartSegment;
        private GlobStrategy? _strategy;
        private bool _compileAttempted;

        private ToukiPattern(IPattern legacyPattern, bool ignoreCase)
        {
            _legacyPattern = legacyPattern;
            _ignoreCase = ignoreCase;
            _stemStartSegment = GetStemStartSegment(legacyPattern);
        }

        public static ToukiPattern Create(IPattern legacyPattern, StringComparison comparisonType)
        {
            Debug.Assert(comparisonType is StringComparison.Ordinal or StringComparison.OrdinalIgnoreCase);

            return new ToukiPattern(legacyPattern, comparisonType == StringComparison.OrdinalIgnoreCase);
        }

        public IPattern LegacyPattern => _legacyPattern;

        public bool MatchesFile(ReadOnlySpan<char> currentDirectory, ReadOnlySpan<char> fileName) =>
            (_strategy ?? GetStrategy()) is { } strategy
            && strategy.MatchCore(currentDirectory, fileName);

        public bool TryCompile() => GetStrategy() is not null;

        public string CalculateStem(ReadOnlySpan<char> directoryPrefix, string fileName)
            => CalculateStemCore(directoryPrefix, fileName, fileName, fullPath: null);

        public string CalculateStem(
            ReadOnlySpan<char> directoryPrefix,
            ReadOnlySpan<char> fileName,
            string fullPath)
            => CalculateStemCore(directoryPrefix, fileName, materializedFileName: null, fullPath);

        private string CalculateStemCore(
            ReadOnlySpan<char> directoryPrefix,
            ReadOnlySpan<char> fileName,
            string? materializedFileName,
            string? fullPath)
        {
            if (_stemStartSegment == int.MaxValue)
            {
                return directoryPrefix.IsEmpty && fullPath is not null
                    ? fullPath
                    : materializedFileName ?? fileName.ToString();
            }

            int offset = 0;
            for (int segment = 0; segment < _stemStartSegment; segment++)
            {
                int separator = directoryPrefix[offset..].IndexOf('/');
                if (separator < 0)
                {
                    return materializedFileName ?? fileName.ToString();
                }

                offset += separator + 1;
            }

            if (offset == 0 && fullPath is not null)
            {
                return fullPath;
            }

            return offset == directoryPrefix.Length
                ? materializedFileName ?? fileName.ToString()
                : string.Concat(directoryPrefix[offset..], fileName);
        }

        private GlobStrategy? GetStrategy()
        {
            if (Volatile.Read(ref _compileAttempted))
            {
                return _strategy;
            }

            lock (this)
            {
                if (!_compileAttempted)
                {
                    GlobCompiler.TryCompile(
                        GetCompiledPattern(_legacyPattern),
                        _ignoreCase,
                        out _strategy);
                    Volatile.Write(ref _compileAttempted, true);
                }
            }

            return _strategy;
        }

        private static int GetStemStartSegment(IPattern pattern)
        {
            if (pattern is IRaggedPattern raggedPattern)
            {
                return raggedPattern.StartsWith.Count;
            }

            if (pattern is ILinearPattern linearPattern)
            {
                for (int index = 0; index < linearPattern.Segments.Count; index++)
                {
                    if (linearPattern.Segments[index].CanProduceStem)
                    {
                        return index;
                    }
                }
            }

            return int.MaxValue;
        }

        private static string GetCompiledPattern(IPattern pattern)
        {
            IList<IPathSegment> segments = pattern switch
            {
                ILinearPattern linearPattern => linearPattern.Segments,
                IRaggedPattern raggedPattern => raggedPattern.Segments,
                _ => throw new InvalidOperationException()
            };

            using ValueStringBuilder builder = new(stackalloc char[256]);
            bool previousWasRecursive = false;
            int emittedSegments = 0;
            for (int index = 0; index < segments.Count; index++)
            {
                IPathSegment segment = segments[index];
                bool isRecursive = segment is RecursiveWildcardSegment;
                if (isRecursive && previousWasRecursive)
                {
                    continue;
                }

                if (isRecursive && emittedSegments != 0 && IsTrailingRecursiveRun(segments, index))
                {
                    builder.Append("/*");
                    emittedSegments++;
                }

                if (emittedSegments != 0)
                {
                    builder.Append('/');
                }

                switch (segment)
                {
                    case LiteralPathSegment literal:
                        builder.Append(literal.Value);
                        break;
                    case ParentPathSegment:
                        builder.Append("..");
                        break;
                    case RecursiveWildcardSegment:
                        builder.Append("**");
                        break;
                    case WildcardPathSegment wildcard:
                        builder.Append(wildcard.BeginsWith);
                        builder.Append('*');
                        foreach (string contains in wildcard.Contains)
                        {
                            builder.Append(contains);
                            builder.Append('*');
                        }
                        builder.Append(wildcard.EndsWith);
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                emittedSegments++;
                previousWasRecursive = isRecursive;
            }

            return builder.ToString();

            static bool IsTrailingRecursiveRun(IList<IPathSegment> segments, int start)
            {
                for (int index = start + 1; index < segments.Count; index++)
                {
                    if (segments[index] is not RecursiveWildcardSegment)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }

}

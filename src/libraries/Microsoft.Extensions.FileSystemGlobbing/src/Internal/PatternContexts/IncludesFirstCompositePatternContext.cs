// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts
{
    internal sealed class IncludesFirstCompositePatternContext : CompositePatternContext
    {
        private readonly IPatternContext[] _includePatternContexts;
        private readonly IPatternContext[] _excludePatternContexts;

        internal IncludesFirstCompositePatternContext(IPatternContext[] includePatternContexts, IPatternContext[] excludePatternContexts)
        {
            _includePatternContexts = includePatternContexts;
            _excludePatternContexts = excludePatternContexts;
        }

        public override void Declare(Action<IPathSegment, bool> onDeclare)
        {
            foreach (IPatternContext include in _includePatternContexts)
            {
                include.Declare(onDeclare);
            }
        }

        protected internal override PatternTestResult MatchPatternContexts<TFileInfoBase>(TFileInfoBase fileInfo, Func<IPatternContext, TFileInfoBase, PatternTestResult> test)
        {
            PatternTestResult result = PatternTestResult.Failed;

            // If the given file/directory matches any including pattern, continues to next step.
            foreach (IPatternContext context in _includePatternContexts)
            {
                PatternTestResult localResult = test(context, fileInfo);
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
                if (test(context, fileInfo).IsSuccessful)
                {
                    return PatternTestResult.Failed;
                }
            }

            return result;
        }

        public override void PopDirectory()
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

        public override void PushDirectory(DirectoryInfoBase directory)
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

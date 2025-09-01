// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts
{
    internal sealed class PreserveOrderCompositePatternContext : CompositePatternContext
    {
        private readonly IncludeOrExcludeValue<IPatternContext>[] _includeOrExcludePatternContexts;

        internal PreserveOrderCompositePatternContext(IncludeOrExcludeValue<IPatternContext>[] includeOrExcludePatternContexts) =>
            _includeOrExcludePatternContexts = includeOrExcludePatternContexts;

        public override void Declare(Action<IPathSegment, bool> onDeclare)
        {
            foreach (IncludeOrExcludeValue<IPatternContext> context in _includeOrExcludePatternContexts)
            {
                if (context.IsInclude)
                {
                    context.Value.Declare(onDeclare);
                }
            }
        }

        protected internal override PatternTestResult MatchPatternContexts<TFileInfoBase>(TFileInfoBase fileInfo, Func<IPatternContext, TFileInfoBase, PatternTestResult> test)
        {
            PatternTestResult result = PatternTestResult.Failed;

            foreach (IncludeOrExcludeValue<IPatternContext> context in _includeOrExcludePatternContexts)
            {
                // If the file is currently a match and the pattern is exclude, then test it to determine
                // if we should unmatch. And if the file is currently not a match and the pattern is include,
                // then test it to determine if we should match.
                if (result.IsSuccessful != context.IsInclude)
                {
                    PatternTestResult localResult = test(context.Value, fileInfo);
                    if (localResult.IsSuccessful)
                    {
                        result = context.IsInclude ? localResult : PatternTestResult.Failed;
                    }
                }
            }

            return result;
        }

        public override void PopDirectory()
        {
            foreach (IncludeOrExcludeValue<IPatternContext> context in _includeOrExcludePatternContexts)
            {
                context.Value.PopDirectory();
            }
        }

        public override void PushDirectory(DirectoryInfoBase directory)
        {
            foreach (IncludeOrExcludeValue<IPatternContext> context in _includeOrExcludePatternContexts)
            {
                context.Value.PushDirectory(directory);
            }
        }
    }
}

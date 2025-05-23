// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts
{
    internal abstract class CompositePatternContext : IPatternContext
    {
        public abstract void Declare(Action<IPathSegment, bool> onDeclare);
        public abstract void PopDirectory();
        public abstract void PushDirectory(DirectoryInfoBase directory);

        protected internal abstract PatternTestResult MatchPatternContexts<TFileInfoBase>(
            TFileInfoBase fileInfo,
            Func<IPatternContext, TFileInfoBase, PatternTestResult> test);

        public bool Test(DirectoryInfoBase directory) =>
            MatchPatternContexts(directory,
                static (context, dir) =>
                    context.Test(dir) ? PatternTestResult.Success(stem: string.Empty) : PatternTestResult.Failed).IsSuccessful;

        public PatternTestResult Test(FileInfoBase file) =>
            MatchPatternContexts(file, static (context, fileInfo) => context.Test(fileInfo));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Touki.Io.Globbing;

internal sealed class RootFileNameStrategy : GlobStrategy
{
    private readonly GlobStrategy _fileMatcher;

    public RootFileNameStrategy(GlobStrategy fileMatcher)
        : base(ignoreCase: false)
    {
        _fileMatcher = fileMatcher;
    }

    internal override bool MatchCore(ReadOnlySpan<char> directoryPrefix, ReadOnlySpan<char> fileName) =>
        directoryPrefix.IsEmpty && _fileMatcher.MatchCore(default, fileName);

}

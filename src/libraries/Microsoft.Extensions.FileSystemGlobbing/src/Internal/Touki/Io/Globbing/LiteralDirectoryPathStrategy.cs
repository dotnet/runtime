// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Touki.Io.Globbing;

internal sealed class LiteralDirectoryPathStrategy : GlobStrategy
{
    private readonly string _directoryPath;
    private readonly GlobStrategy _fileMatcher;

    public LiteralDirectoryPathStrategy(string directoryPath, GlobStrategy fileMatcher, bool ignoreCase)
        : base(ignoreCase)
    {
        _directoryPath = directoryPath;
        _fileMatcher = fileMatcher;
    }

    internal override bool MatchCore(ReadOnlySpan<char> directoryPrefix, ReadOnlySpan<char> fileName) =>
        directoryPrefix.Equals(
            _directoryPath,
            IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
        && _fileMatcher.MatchCore(default, fileName);

}

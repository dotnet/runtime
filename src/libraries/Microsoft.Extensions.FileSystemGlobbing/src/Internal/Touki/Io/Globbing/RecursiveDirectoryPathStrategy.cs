// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Touki.Io.Globbing;

internal sealed class RecursiveDirectoryPathStrategy : GlobStrategy
{
    private readonly string _directoryPath;

    public RecursiveDirectoryPathStrategy(string directoryPath, bool ignoreCase)
        : base(ignoreCase)
    {
        _directoryPath = directoryPath + '/';
    }

    internal override bool MatchCore(ReadOnlySpan<char> directoryPrefix, ReadOnlySpan<char> fileName)
    {
        StringComparison comparison = IgnoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        ReadOnlySpan<char> remaining = directoryPrefix;

        while (true)
        {
            int match = remaining.IndexOf(_directoryPath, comparison);
            if (match < 0)
            {
                return false;
            }

            if (match == 0 || remaining[match - 1] == '/')
            {
                return true;
            }

            remaining = remaining[(match + 1)..];
        }
    }
}

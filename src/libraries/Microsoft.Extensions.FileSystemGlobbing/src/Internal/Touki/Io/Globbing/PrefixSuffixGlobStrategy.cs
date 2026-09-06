// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2025 Jeremy W Kuhne
// SPDX-License-Identifier: MIT
// See LICENSE file in the project root for full license information

namespace Touki.Io.Globbing;

// <summary>
//  Matches inputs of the form <c>prefix*suffix</c> -- a single <c>*</c> bracketed by
//  literal runs.
// </summary>
internal sealed class PrefixSuffixGlobStrategy : GlobStrategy
{
    private readonly string _prefix;
    private readonly string _suffix;

    public PrefixSuffixGlobStrategy(string prefix, string suffix, bool ignoreCase)
        : base(ignoreCase)
    {
        _prefix = prefix;
        _suffix = suffix;
    }

    // <inheritdoc/>
    internal override bool MatchCore(
        ReadOnlySpan<char> directoryPrefix,
        ReadOnlySpan<char> fileName)
    {
        // PrefixSuffixGlobStrategy is only chosen for path-unaware dialects; the
        // directory prefix is always empty by construction.
        Debug.Assert(directoryPrefix.IsEmpty);
        ReadOnlySpan<char> prefix = _prefix.AsSpan();
        ReadOnlySpan<char> suffix = _suffix.AsSpan();
        if (fileName.Length < prefix.Length + suffix.Length)
        {
            return false;
        }

        // The prefix is taken from the pattern and is not a wildcard, so it enforces the
        // leading-dot rule by literal compare.
        return IgnoreCase
            ? fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            : fileName.StartsWith(prefix) && fileName.EndsWith(suffix);
    }
}

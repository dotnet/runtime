// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2025 Jeremy W Kuhne
// SPDX-License-Identifier: MIT
// See LICENSE file in the project root for full license information

namespace Touki.Io.Globbing;

// <summary>
//  Matches inputs that end with a fixed literal suffix (pattern of the form <c>*suffix</c>).
// </summary>
internal sealed class SuffixGlobStrategy : GlobStrategy
{
    private readonly string _suffix;

    public SuffixGlobStrategy(string suffix, bool ignoreCase)
        : base(ignoreCase)
    {
        _suffix = suffix;
    }

    // <inheritdoc/>
    internal override bool MatchCore(
        ReadOnlySpan<char> directoryPrefix,
        ReadOnlySpan<char> fileName)
    {
        // SuffixGlobStrategy is only chosen for path-unaware dialects; the directory
        // prefix is always empty by construction.
        Debug.Assert(directoryPrefix.IsEmpty);
        ReadOnlySpan<char> suffix = _suffix.AsSpan();

        return IgnoreCase
            ? fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            : fileName.EndsWith(suffix);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2025 Jeremy W Kuhne
// SPDX-License-Identifier: MIT
// See LICENSE file in the project root for full license information

namespace Touki.Io.Globbing;

// <summary>
//  Matches inputs that start with a fixed literal prefix (pattern of the form <c>prefix*</c>).
// </summary>
internal sealed class PrefixGlobStrategy : GlobStrategy
{
    private readonly string _prefix;

    public PrefixGlobStrategy(string prefix, bool ignoreCase)
        : base(ignoreCase)
    {
        _prefix = prefix;
    }

    // <inheritdoc/>
    internal override bool MatchCore(
        ReadOnlySpan<char> directoryPrefix,
        ReadOnlySpan<char> fileName)
    {
        // PrefixGlobStrategy is only chosen for path-unaware dialects; the directory
        // prefix is always empty by construction. The prefix is taken from the
        // pattern; a literal '.' in the prefix correctly enforces the POSIX
        // leading-dot rule by itself, so no extra check is required.
        Debug.Assert(directoryPrefix.IsEmpty);
        ReadOnlySpan<char> prefix = _prefix.AsSpan();
        return IgnoreCase
            ? fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            : fileName.StartsWith(prefix);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2025 Jeremy W Kuhne
// SPDX-License-Identifier: MIT
// See LICENSE file in the project root for full license information

namespace Touki.Io.Globbing;

// <summary>
//  Matches inputs that contain a fixed literal substring (pattern of the form <c>*needle*</c>).
// </summary>
internal sealed class ContainsGlobStrategy : GlobStrategy
{
    private readonly string _needle;

    public ContainsGlobStrategy(string needle, bool ignoreCase)
        : base(ignoreCase)
    {
        _needle = needle;
    }

    // <inheritdoc/>
    internal override bool MatchCore(
        ReadOnlySpan<char> directoryPrefix,
        ReadOnlySpan<char> fileName)
    {
        // ContainsGlobStrategy is only chosen for path-unaware dialects; the directory
        // prefix is always empty by construction.
        Debug.Assert(directoryPrefix.IsEmpty);
        ReadOnlySpan<char> needle = _needle.AsSpan();

        return IgnoreCase
            ? fileName.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0
            : fileName.IndexOf(needle) >= 0;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2025 Jeremy W Kuhne
// SPDX-License-Identifier: MIT
// See LICENSE file in the project root for full license information

namespace Touki.Io.Globbing;

// <summary>
//  Specialized matcher for the canonical <c>**/&lt;segment&gt;</c>
//  shape (e.g. <c>**/*.cs</c>, <c>**/file.cs</c>, <c>**/foo*</c>) where the trailing
//  segment contains no separators. The matcher delegates to an inner path-unaware
//  segment matcher that already knows the cheapest implementation for the segment
//  shape (literal / prefix / suffix / contains / prefix+suffix / any), bypassing the
//  bytecode interpreter entirely. The two-span override ignores the directory prefix,
//  so the per-file hot path is one delegated match against the file name span.
// </summary>
internal sealed class GlobStarFileNameStrategy : GlobStrategy
{
    private readonly GlobStrategy _segmentMatcher;

    public GlobStarFileNameStrategy(GlobStrategy segmentMatcher)
        : base(ignoreCase: false)
    {
        _segmentMatcher = segmentMatcher;
    }

    // <inheritdoc/>
    // <remarks>
    //  <para>
    //   The directory prefix in <paramref name="directoryPrefix"/> is irrelevant for
    //   <c>**/</c> globstar patterns: zero or more
    //   directory segments are matched by the leading globstar, and the trailing
    //   segment matches against the file name alone. The wrapper delegates straight
    //   to the inner segment matcher with <paramref name="fileName"/>, bypassing the
    //   bytecode interpreter and any path concatenation on the per-file hot path.
    //  </para>
    // </remarks>
    internal override bool MatchCore(
        ReadOnlySpan<char> directoryPrefix,
        ReadOnlySpan<char> fileName)
        => _segmentMatcher.MatchCore(default, fileName);

}

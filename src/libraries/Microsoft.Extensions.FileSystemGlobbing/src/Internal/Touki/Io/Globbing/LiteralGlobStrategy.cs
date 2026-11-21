// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2025 Jeremy W Kuhne
// SPDX-License-Identifier: MIT
// See LICENSE file in the project root for full license information

namespace Touki.Io.Globbing;

// <summary>
//  Matches inputs that are byte-for-byte equal to a fixed literal.
// </summary>
internal sealed class LiteralGlobStrategy : GlobStrategy
{
    private readonly string _literal;

    public LiteralGlobStrategy(string literal, bool ignoreCase)
        : base(ignoreCase)
    {
        _literal = literal;
    }

    // <inheritdoc/>
    // <remarks>
    //  <para>
    //   Compares the logical concatenation <paramref name="directoryPrefix"/> +
    //   <paramref name="fileName"/> against the stored literal without copying either
    //   span. The literal is split at <c>directoryPrefix.Length</c> so each half stays
    //   contiguous on its source span and the inner comparisons use the same vectorized
    //   routines as the path-unaware fast paths. Path-unaware matchers receive an
    //   empty <paramref name="directoryPrefix"/> and the implementation collapses to a
    //   single full-span equality. When <paramref name="directoryPrefix"/> is
    //   non-empty the caller has appended <see cref="GlobStrategy.Separator"/> at
    //   the end, so the split at <c>directoryPrefix.Length</c> aligns exactly with
    //   the literal's directory / file-name boundary.
    //  </para>
    // </remarks>
    internal override bool MatchCore(
        ReadOnlySpan<char> directoryPrefix,
        ReadOnlySpan<char> fileName)
    {
        int total = directoryPrefix.Length + fileName.Length;
        if (total != _literal.Length)
        {
            return false;
        }

        ReadOnlySpan<char> literal = _literal.AsSpan();
        ReadOnlySpan<char> literalPrefix = literal[..directoryPrefix.Length];
        ReadOnlySpan<char> literalFileName = literal[directoryPrefix.Length..];

        return IgnoreCase
            ? directoryPrefix.Equals(literalPrefix, StringComparison.OrdinalIgnoreCase)
                && fileName.Equals(literalFileName, StringComparison.OrdinalIgnoreCase)
            : directoryPrefix.SequenceEqual(literalPrefix)
                && fileName.SequenceEqual(literalFileName);
    }

}

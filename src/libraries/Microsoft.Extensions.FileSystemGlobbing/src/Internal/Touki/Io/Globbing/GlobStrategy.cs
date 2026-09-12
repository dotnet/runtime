// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2025 Jeremy W Kuhne
// SPDX-License-Identifier: MIT
// See LICENSE file in the project root for full license information

namespace Touki.Io.Globbing;

// <summary>
//  Immutable, root-independent FSG matching strategy.
// </summary>
// <remarks>
//  <para>
//   Strategies hold no per-enumeration state and can be matched concurrently.
//  </para>
// </remarks>
internal abstract class GlobStrategy
{
    private protected GlobStrategy(bool ignoreCase)
    {
        IgnoreCase = ignoreCase;
    }

    // <summary>
    //  The case-fold rule the strategy dispatches to when comparing characters.
    // </summary>
    internal bool IgnoreCase { get; }

    // <summary>
    //  The path separator character for path-aware matching, or <c>'\0'</c> when the
    //  dialect is path-unaware.
    // </summary>
    protected const char Separator = '/';

    // <summary>
    //  Tests whether the logical concatenation
    //  <paramref name="directoryPrefix"/> + <paramref name="fileName"/> matches the
    //  compiled pattern. When <paramref name="directoryPrefix"/> is non-empty it
    //  ends with <see cref="Separator"/>.
    // </summary>
    internal abstract bool MatchCore(
        ReadOnlySpan<char> directoryPrefix,
        ReadOnlySpan<char> fileName);

}

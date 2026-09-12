// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2025 Jeremy W Kuhne
// SPDX-License-Identifier: MIT
// See LICENSE file in the project root for full license information

namespace Touki.Io.Globbing;

// <summary>
//  Matches any input. Used for a single-segment <c>*</c> and whole-pattern
//  recursive <c>**</c> after the factory has applied the appropriate root scope.
// </summary>
internal sealed class AnyGlobStrategy : GlobStrategy
{
    public AnyGlobStrategy()
        : base(ignoreCase: false)
    {
    }

    // <inheritdoc/>
    internal override bool MatchCore(
        ReadOnlySpan<char> directoryPrefix,
        ReadOnlySpan<char> fileName)
        => true;
}

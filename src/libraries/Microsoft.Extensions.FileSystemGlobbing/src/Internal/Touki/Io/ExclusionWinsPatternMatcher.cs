// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2025 Jeremy W Kuhne
// SPDX-License-Identifier: MIT
// See LICENSE file in the project root for full license information

namespace Touki.Io;

using Microsoft.Extensions.FileSystemGlobbing.Internal;

internal sealed class ExclusionWinsPatternMatcher
{
    private readonly ToukiPattern[] _includes;
    private readonly ToukiPattern[] _excludes;

    public ExclusionWinsPatternMatcher(ToukiPattern[] includes, ToukiPattern[] excludes)
    {
        _includes = includes;
        _excludes = excludes;
    }

    internal bool TryMatchFile(
        ReadOnlySpan<char> currentDirectory,
        ReadOnlySpan<char> fileName,
        [NotNullWhen(true)] out ToukiPattern? matchingInclude)
    {
        matchingInclude = null;
        foreach (ToukiPattern matcher in _includes)
        {
            if (matcher.MatchesFile(currentDirectory, fileName))
            {
                matchingInclude = matcher;
                break;
            }
        }

        if (matchingInclude is null)
        {
            return false;
        }

        foreach (ToukiPattern matcher in _excludes)
        {
            if (matcher.MatchesFile(currentDirectory, fileName))
            {
                matchingInclude = null;
                return false;
            }
        }

        return true;
    }

}

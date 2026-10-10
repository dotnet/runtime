// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2025 Jeremy W Kuhne
// SPDX-License-Identifier: MIT
// See LICENSE file in the project root for full license information

namespace Touki.Io;

using Microsoft.Extensions.FileSystemGlobbing.Internal;

internal sealed class OrderedPatternMatcher
{
    private readonly IncludeOrExcludeValue<ToukiPattern>[] _rules;

    public OrderedPatternMatcher(IncludeOrExcludeValue<ToukiPattern>[] rules)
    {
        _rules = rules;
    }

    internal bool TryMatchFile(
        ReadOnlySpan<char> currentDirectory,
        ReadOnlySpan<char> fileName,
        [NotNullWhen(true)] out ToukiPattern? matchingInclude)
    {
        bool included = false;
        matchingInclude = null;

        foreach (IncludeOrExcludeValue<ToukiPattern> rule in _rules)
        {
            bool ruleIncludes = rule.IsInclude;
            if (included == ruleIncludes)
            {
                continue;
            }

            if (rule.Value.MatchesFile(currentDirectory, fileName))
            {
                included = ruleIncludes;
                matchingInclude = ruleIncludes ? rule.Value : null;
            }
        }

        return included && matchingInclude is not null;
    }

}

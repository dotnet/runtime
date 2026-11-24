// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ILLink.CodeFix.UnsafeContext
{
    /// <summary>
    /// One alternative for a region: either an edit, or a refinement that replaces the region with finer regions
    /// (e.g. one per statement of a merged range, or the header and bodies of a compound statement).
    /// </summary>
    internal sealed class RegionCandidate
    {
        private RegionCandidate(UnsafeContextEdit? edit, Func<UnsafeContextRegion, IEnumerable<UnsafeContextRegion>>? refine)
        {
            Edit = edit;
            Refine = refine;
        }

        public UnsafeContextEdit? Edit { get; }

        public Func<UnsafeContextRegion, IEnumerable<UnsafeContextRegion>>? Refine { get; }

        public static RegionCandidate ForEdit(UnsafeContextEdit edit) => new(edit, null);

        public static RegionCandidate ForRefinement(Func<UnsafeContextRegion, IEnumerable<UnsafeContextRegion>> refine) => new(null, refine);
    }

    /// <summary>
    /// A group of operations covered by one context, with its candidates in order of preference. Validation advances
    /// a failing region to its next candidate; a refined region whose children run out of candidates falls back to
    /// the candidates its parent has after the refinement.
    /// </summary>
    internal sealed class UnsafeContextRegion(ImmutableArray<UnsafeTarget> targets, ImmutableArray<RegionCandidate> candidates, UnsafeContextRegion? parent)
    {
        private int _index;

        public ImmutableArray<UnsafeTarget> Targets { get; } = targets;

        public UnsafeContextRegion? Parent { get; } = parent;

        /// <summary>Marks the nodes created by this region's edit, to find them again in the validated tree.</summary>
        public SyntaxAnnotation Annotation { get; } = new("UnsafeContextRegion");

        public RegionCandidate? Current => _index < candidates.Length ? candidates[_index] : null;

        public UnsafeContextEdit? CurrentEdit => Current?.Edit;

        public bool HasNext => _index + 1 < candidates.Length;

        /// <summary>Moves to the next candidate; returns <see langword="false"/> when none is left.</summary>
        public bool MoveNext() => ++_index < candidates.Length;

        public bool IsDescendantOf(UnsafeContextRegion region)
        {
            for (UnsafeContextRegion? current = Parent; current is not null; current = current.Parent)
            {
                if (current == region)
                    return true;
            }

            return false;
        }
    }
}

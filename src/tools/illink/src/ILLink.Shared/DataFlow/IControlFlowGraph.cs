// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.DataFlow
{
	public enum RegionKind
	{
		Try,
		Catch,
		Filter,
		Finally
	}

	public interface IRegion<TRegion> : IEquatable<TRegion>
	{
		RegionKind Kind { get; }
	}

	public interface IControlFlowGraph<TBlock, TRegion>
		where TBlock : IEquatable<TBlock>
		where TRegion : IRegion<TRegion>
	{

		public readonly struct Predecessor
		{
			public readonly TBlock Block;
			public readonly ImmutableArray<TRegion> FinallyRegions;
			public Predecessor (TBlock block, ImmutableArray<TRegion> finallyRegions)
			{
				(Block, FinallyRegions) = (block, finallyRegions);
			}
		}

		IEnumerable<TBlock> Blocks { get; }

		TBlock Entry { get; }

		// This does not include predecessor edges for exceptional control flow into
		// catch regions or finally regions. It also doesn't include edges for non-exceptional
		// control flow from try -> finally or from catch -> finally.
		IEnumerable<Predecessor> GetPredecessors (TBlock block);

		bool TryGetEnclosingTryOrCatchOrFilter (TBlock block, [NotNullWhen (true)] out TRegion? tryOrCatchOrFilterRegion);

		bool TryGetEnclosingTryOrCatchOrFilter (TRegion region, [NotNullWhen (true)] out TRegion? tryOrCatchOrFilterRegion);

		bool TryGetEnclosingFinally (TBlock block, [NotNullWhen (true)] out TRegion? region);

		TRegion GetCorrespondingTry (TRegion cathOrFilterOrFinallyRegion);

		IEnumerable<TRegion> GetPreviousFilters (TRegion catchOrFilterRegion);

		bool HasFilter (TRegion catchRegion);

		TBlock FirstBlock (TRegion region);

		TBlock LastBlock (TRegion region);
	}
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace ILLink.Shared.DataFlow
{
	public enum RegionKind
	{
		Try,
		Catch,
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

		bool TryGetEnclosingTryOrCatch (TBlock block, [NotNullWhen (true)] out TRegion? tryOrCatchRegion);

		bool TryGetEnclosingTryOrCatch (TRegion region, [NotNullWhen (true)] out TRegion? tryOrCatchRegion);

		bool TryGetEnclosingFinally (TBlock block, [NotNullWhen (true)] out TRegion? region);

		TRegion GetCorrespondingTry (TRegion cathOrFinallyRegion);

		TBlock FirstBlock (TRegion region);

		TBlock LastBlock (TRegion region);
	}
}
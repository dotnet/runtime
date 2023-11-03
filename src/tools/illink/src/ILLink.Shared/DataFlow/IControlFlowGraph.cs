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

	public enum ConditionKind
	{
		None,
		WhenFalse,
		WhenTrue,
	}

	public interface IRegion<TRegion> : IEquatable<TRegion>
	{
		RegionKind Kind { get; }
	}

	public interface IBlock<TBlock> : IEquatable<TBlock>
	{
		ConditionKind ConditionKind { get; }
	}

	public interface IControlFlowGraph<TBlock, TRegion>
		where TBlock : struct, IBlock<TBlock>
		where TRegion : IRegion<TRegion>
	{

		// Represents an edge in the control flow graph, from a source to a destination basic block.
		public readonly struct ControlFlowBranch : IEquatable<ControlFlowBranch>
		{
			public readonly TBlock Source;
			private readonly TBlock? Destination;

			// The finally regions exited when control flows through this edge.
			// For example:
			//
			// try {
			//     try {
			//         Source();
			//     }
			//     finally {}
			// } finally {}
			// Target();
			//
			// There will be an edge in the CRFG from the block that calls
			// Source() to the block that calls Target(), which exits both
			// finally regions.
			public readonly ImmutableArray<TRegion> FinallyRegions;
			public readonly bool IsConditional;
			public ControlFlowBranch (TBlock source, TBlock? destination, ImmutableArray<TRegion> finallyRegions, bool isConditional)
			{
				Source = source;
				Destination = destination;
				FinallyRegions = finallyRegions;
				IsConditional = isConditional;
			}

			public bool Equals (ControlFlowBranch other)
			{
				if (!Source.Equals (other.Source))
					return false;

				if (IsConditional != other.IsConditional)
					return false;

				if (Destination == null)
					return other.Destination == null;

				return Destination.Equals (other.Destination);
			}

			public override bool Equals (object? obj)
			{
				return obj is ControlFlowBranch other && Equals (other);
			}

			public override int GetHashCode ()
			{
				return HashUtils.Combine (
					Source.GetHashCode (),
					Destination?.GetHashCode () ?? typeof (ControlFlowBranch).GetHashCode (),
					IsConditional.GetHashCode ());
			}
		}

		IEnumerable<TBlock> Blocks { get; }

		TBlock Entry { get; }

		// This does not include predecessor edges for exceptional control flow into
		// catch regions or finally regions. It also doesn't include edges for non-exceptional
		// control flow from try -> finally or from catch -> finally.
		IEnumerable<ControlFlowBranch> GetPredecessors (TBlock block);

		ControlFlowBranch? GetConditionalSuccessor (TBlock block);

		ControlFlowBranch? GetFallThroughSuccessor (TBlock block);

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

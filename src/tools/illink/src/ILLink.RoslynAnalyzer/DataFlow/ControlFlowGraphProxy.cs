// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;

using ControlFlowBranch = ILLink.Shared.DataFlow.IControlFlowGraph<
	ILLink.RoslynAnalyzer.DataFlow.BlockProxy,
	ILLink.RoslynAnalyzer.DataFlow.RegionProxy
>.ControlFlowBranch;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	// Blocks should be usable as keys of a dictionary.
	// The record equality implementation will check for reference equality
	// on the underlying BasicBlock, so uses of this class should not expect
	// any kind of value equality for different block instances. In practice
	// this should be fine as long as we consistently use block instances from
	// a single ControlFlowGraph.
	public readonly record struct BlockProxy (BasicBlock Block) : IBlock<BlockProxy>
	{
		public override string ToString ()
		{
			return base.ToString () + $"[{Block.Ordinal}]";
		}

		public ConditionKind ConditionKind => (ConditionKind) Block.ConditionKind;
	}

	public readonly record struct RegionProxy (ControlFlowRegion Region) : IRegion<RegionProxy>
	{
		public RegionKind Kind => Region.Kind switch {
			ControlFlowRegionKind.Try => RegionKind.Try,
			ControlFlowRegionKind.Catch => RegionKind.Catch,
			ControlFlowRegionKind.Filter => RegionKind.Filter,
			ControlFlowRegionKind.Finally => RegionKind.Finally,
			_ => throw new InvalidOperationException ()
		};
	}

	public readonly record struct ControlFlowGraphProxy (ControlFlowGraph ControlFlowGraph) : IControlFlowGraph<BlockProxy, RegionProxy>
	{
		public IEnumerable<BlockProxy> Blocks {
			get {
				foreach (var block in ControlFlowGraph.Blocks)
					yield return new BlockProxy (block);
			}
		}

		public BlockProxy Entry => new BlockProxy (ControlFlowGraph.EntryBlock ());

		public static ControlFlowBranch? CreateProxyBranch (Microsoft.CodeAnalysis.FlowAnalysis.ControlFlowBranch? branch)
		{
			if (branch == null)
				return null;

			var finallyRegions = ImmutableArray.CreateBuilder<RegionProxy> ();
			foreach (var region in branch.FinallyRegions) {
				Debug.Assert (region != null);
				if (region == null)
					continue;
				finallyRegions.Add (new RegionProxy (region));
			}

			// Destination might be null in a 'throw' branch.
			return new ControlFlowBranch (
				new BlockProxy (branch.Source),
				branch.Destination == null ? null : new BlockProxy (branch.Destination),
				finallyRegions.ToImmutable (),
				branch.IsConditionalSuccessor);
		}

		// This is implemented by getting predecessors of the underlying Roslyn BasicBlock.
		// This is fine as long as the blocks come from the correct control-flow graph.
		public IEnumerable<ControlFlowBranch> GetPredecessors (BlockProxy block)
		{
			foreach (var predecessor in block.Block.Predecessors) {
				if (CreateProxyBranch (predecessor) is ControlFlowBranch branch)
					yield return branch;
			}
		}

		public ControlFlowBranch? GetConditionalSuccessor (BlockProxy block) => CreateProxyBranch (block.Block.ConditionalSuccessor);

		public ControlFlowBranch? GetFallThroughSuccessor (BlockProxy block) => CreateProxyBranch (block.Block.FallThroughSuccessor);

		public bool TryGetEnclosingTryOrCatchOrFilter (BlockProxy block, out RegionProxy tryOrCatchOrFilterRegion)
		{
			return TryGetTryOrCatchOrFilter (block.Block.EnclosingRegion, out tryOrCatchOrFilterRegion);
		}

		public bool TryGetEnclosingTryOrCatchOrFilter (RegionProxy regionProxy, out RegionProxy tryOrCatchOrFilterRegion)
		{
			return TryGetTryOrCatchOrFilter (regionProxy.Region.EnclosingRegion, out tryOrCatchOrFilterRegion);
		}

		static bool TryGetTryOrCatchOrFilter (ControlFlowRegion? region, out RegionProxy tryOrCatchOrFilterRegion)
		{
			tryOrCatchOrFilterRegion = default;
			// The check for ControlFlowRegionKind.Root prevents us from walking out to regions that
			// contain code outside of the current control flow graph.
			while (region != null && region.Kind != ControlFlowRegionKind.Root) {
				if (region.Kind is ControlFlowRegionKind.Try or ControlFlowRegionKind.Catch or ControlFlowRegionKind.Filter) {
					tryOrCatchOrFilterRegion = new RegionProxy (region);
					return true;
				}
				region = region.EnclosingRegion;
			}
			return false;
		}

		public bool TryGetEnclosingFinally (BlockProxy block, out RegionProxy catchRegion)
		{
			catchRegion = default;
			ControlFlowRegion? region = block.Block.EnclosingRegion;
			// The check for ControlFlowRegionKind.Root prevents us from walking out to regions that
			// contain code outside of the current control flow graph.
			while (region != null && region.Kind != ControlFlowRegionKind.Root) {
				if (region.Kind == ControlFlowRegionKind.Finally) {
					catchRegion = new RegionProxy (region);
					return true;
				}
				region = region.EnclosingRegion;
			}
			return false;
		}

		public RegionProxy GetCorrespondingTry (RegionProxy catchOrFilterOrFinallyRegion)
		{
			if (catchOrFilterOrFinallyRegion.Region.Kind is not (ControlFlowRegionKind.Catch or ControlFlowRegionKind.Filter or ControlFlowRegionKind.Finally))
				throw new ArgumentException ("Must be a catch, filter, or finally region: {}", nameof (catchOrFilterOrFinallyRegion));

			// Finally -> TryAndFinally
			// Catch -> TryAndCatch or FilterAndHandler
			// Filter -> FilterAndHandler
			var enclosingRegion = catchOrFilterOrFinallyRegion.Region.EnclosingRegion!;
			// FilterAndHandler -> TryAndCatch
			if (enclosingRegion.Kind == ControlFlowRegionKind.FilterAndHandler) {
				enclosingRegion = enclosingRegion.EnclosingRegion!;
				Debug.Assert (enclosingRegion.Kind == ControlFlowRegionKind.TryAndCatch);
			}

			// For TryAndFinally or TryAndCatch, get the Try region.
			foreach (var nested in enclosingRegion.NestedRegions) {
				// Note that for try+catch+finally, the try corresponding to the finally will not be the same as
				// the try corresponding to the catch, because Roslyn represents this region hierarchy the same as
				// a try+catch nested inside the try block of a try+finally.
				if (nested.Kind == ControlFlowRegionKind.Try)
					return new (nested);
			}
			throw new InvalidOperationException ();
		}

		public IEnumerable<RegionProxy> GetPreviousFilters (RegionProxy catchOrFilterRegion)
		{
			var region = catchOrFilterRegion.Region;
			if (region.Kind is not (ControlFlowRegionKind.Catch or ControlFlowRegionKind.Filter))
				throw new ArgumentException ("Must be a catch or filter region: {}", nameof (catchOrFilterRegion));

			// Should not be called for a catch block that already has a filter.
			if (region.Kind is ControlFlowRegionKind.Catch && region.EnclosingRegion!.Kind is ControlFlowRegionKind.FilterAndHandler)
				throw new ArgumentException ("Must not be a catch block with filter: {}", nameof (catchOrFilterRegion));

			var tryRegion = GetCorrespondingTry (catchOrFilterRegion);
			// The enclosing region is part of a TryAndCatch region, which has
			// a Try and multiple Catch or FilterAndHandler regions.
			foreach (var nested in tryRegion.Region.EnclosingRegion!.NestedRegions) {
				ControlFlowRegion? catchOrFilter = null;
				switch (nested.Kind) {
				case ControlFlowRegionKind.Catch:
					catchOrFilter = nested;
					break;
				case ControlFlowRegionKind.FilterAndHandler:
					// Get Filter region from the FilterAndHandler
					foreach (var filter in nested.NestedRegions) {
						if (filter.Kind == ControlFlowRegionKind.Filter) {
							catchOrFilter = filter;
							break;
						}
					}
					// In case there is no filter region, just skip this one.
					if (catchOrFilter == null)
						continue;
					break;
				default:
					continue;
				}

				// When we reach this one, we are done searching.
				if (catchOrFilter.Equals (region))
					yield break;

				// If the previous region is a filter region, yield it.
				if (catchOrFilter.Kind == ControlFlowRegionKind.Filter)
					yield return new (catchOrFilter);
			}
			throw new InvalidOperationException ();
		}

		public bool HasFilter (RegionProxy catchRegion)
		{
			if (catchRegion.Region.Kind is not ControlFlowRegionKind.Catch)
				throw new ArgumentException ("Must be a catch region: {}", nameof (catchRegion));

			return catchRegion.Region.EnclosingRegion!.Kind == ControlFlowRegionKind.FilterAndHandler;
		}

		public BlockProxy FirstBlock (RegionProxy region) =>
			new BlockProxy (ControlFlowGraph.Blocks[region.Region.FirstBlockOrdinal]);

		public BlockProxy LastBlock (RegionProxy region) =>
			new BlockProxy (ControlFlowGraph.Blocks[region.Region.LastBlockOrdinal]);
	}
}

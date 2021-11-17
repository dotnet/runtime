// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILLink.Shared.DataFlow
{
	// A generic implementation of a forward dataflow analysis. Forward means that it flows facts
	// across code in the order of execution, starting from the beginning of a method,
	// and merging values from predecessors.
	public abstract class ForwardDataFlowAnalysis<TValue, TLattice, TBlock, TControlFlowGraph, TTransfer>
		where TValue : IEquatable<TValue>
		where TLattice : ILattice<TValue>
		where TBlock : IEquatable<TBlock>
		where TControlFlowGraph : IControlFlowGraph<TBlock>
		where TTransfer : ITransfer<TBlock, TValue, TLattice>
	{
		// This just runs a dataflow algorithm until convergence. It doesn't cache any results,
		// allowing each particular kind of analysis to decide what is worth saving.
		public static void Fixpoint (TControlFlowGraph cfg, TLattice lattice, TTransfer transfer)
		{
			// Initialize output of each block to the Top value of the lattice
			DefaultValueDictionary<TBlock, TValue> blockOutput = new (lattice.Top);

			// For now, the actual dataflow algorithm is the simplest possible version.
			// It is written to be obviously correct, but has not been optimized for performance
			// at all. As written it will almost always perform unnecessary passes over the entire
			// control flow graph. The core abstractions shouldn't need to change even when we write
			// an optimized version of this algorithm - ideally any optimizations will be generic,
			// not specific to a particular analysis.

			bool changed = true;
			while (changed) {
				changed = false;
				foreach (var block in cfg.Blocks) {
					if (block.Equals (cfg.Entry))
						continue;

					// Meet over predecessors to get the new value at the start of this block.
					TValue blockState = lattice.Top;
					foreach (var predecessor in cfg.GetPredecessors (block))
						blockState = lattice.Meet (blockState, blockOutput.Get (predecessor));

					// Apply transfer function to the input to compute the output state after this block.
					// This mutates the block state in place.
					transfer.Transfer (block, blockState);

					if (!blockOutput.Get (block).Equals (blockState))
						changed = true;

					blockOutput.Set (block, blockState);
				}
			}
		}
	}
}
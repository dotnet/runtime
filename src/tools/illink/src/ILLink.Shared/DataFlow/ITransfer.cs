// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILLink.Shared.DataFlow
{
	// ITransfer represents the transfer functions for a dataflow analysis.
	// The transfer functions compute the effects of an operation on the set of facts
	// tracked by a dataflow analysis. This simulates the execution of the operation
	// on the domain of abstract values tracked in the dataflow analysis.

	// TValue is the type of the information tracked in a dataflow analysis at each program point.
	// TLattice is the type of the lattice formed by these values.

	// TOperation is a type representing the operations that the transfer function
	// "simulates". It isn't constrained by the interface, but is typically a basic block,
	// where the transfer functions are defined in terms of transfer functions for individual
	// operations in the block.

	// TLattice isn't typically used in the implementation except to provide the "Top" value.
	// This expresses the conceptual constraint that the transferred values are part of a lattice.
	public interface ITransfer<TOperation, TValue, TState, TLattice>
		where TValue : struct, IEquatable<TValue>
		where TState : class, IDataFlowState<TValue, TLattice>
		where TLattice : ILattice<TValue>
	{
		// Transfer should mutate the input value to reflect the effect of
		// computing this operation. When using value types, ensure that
		// any modifications to the values are observable by the caller (consider
		// using readonly structs to prevent the implementation from making changes
		// that won't be reflected in the caller).
		void Transfer (TOperation operation, TState state);
	}
}
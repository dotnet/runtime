// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.DataFlow
{
	// ILattice represents a lattice (technically a semilattice) of values.
	// A semilattice is a set of values along with a meet operation (or greatest lower bound).
	// The meet operation imposes a partial order on the values: a <= b iff Meet(a, b) == a.

	// In a dataflow analysis, the Meet operation is used to combine the tracked facts when
	// there are multiple control flow paths that reach the same program point (or in a backwards
	// analysis, to combine the tracked facts from multiple control flow paths out of a program point).

	// The interface constraint on TValue ensures that trying to instantiate
	// ILattice over a nullable type will produce a warning or error.

	// The lattice might be better represented as an interface describing individual lattice values
	// (as opposed to describing the lattice structure as ILattice does), with Top being a static
	// virtual interface method. This would avoid the need to pass around multiple generic arguments
	// (TValue and TLattice). However, we can't use static virtual interface methods in the analyzer
	// so the lattice instance provides the Top value.
	public interface ILattice<TValue> where TValue : IEquatable<TValue>
	{
		// We require that the lattice has a "Top" or maximum element.
		// Top is >= a for every element a of the lattice.
		// Top is an identity for Meet: Meet(a, Top) = a.

		// The typical use in a dataflow analysis is for Top to represent the "unknown" initial state
		// with the least possible information about the analysis.
		public TValue Top { get; }

		// The Meet operation is associative, commutative, and idempotent.
		// This is used in dataflow analysis to iteratively Meet the tracked facts from different control
		// flow paths until the analysis converges to the most specific set of tracked facts.
		public TValue Meet (TValue left, TValue right);
	}
}

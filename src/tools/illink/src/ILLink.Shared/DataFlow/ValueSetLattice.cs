// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILLink.Shared.DataFlow
{
	// A lattice over ValueSets where the Meet operation is just set union.
	public readonly struct ValueSetLattice<TValue> : ILattice<ValueSet<TValue>>
		where TValue : IEquatable<TValue>
	{
		public ValueSet<TValue> Top => default;

		public ValueSet<TValue> Meet (ValueSet<TValue> left, ValueSet<TValue> right) => ValueSet<TValue>.Meet (left, right);
	}
}
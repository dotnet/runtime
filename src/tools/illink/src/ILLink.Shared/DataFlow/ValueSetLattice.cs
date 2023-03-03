// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

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

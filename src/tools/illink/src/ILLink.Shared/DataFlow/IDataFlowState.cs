// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.DataFlow
{
	public sealed class Box<T> where T : struct
	{
		public Box (T value) => Value = value;
		public T Value { get; set; }

	}

	public interface IDataFlowState<TValue, TValueLattice>
		where TValue : struct, IEquatable<TValue>
		where TValueLattice : ILattice<TValue>
	{
		TValue Current { get; set; }
		Box<TValue>? Exception { get; set; }
		TValueLattice Lattice { get; init; }
	}
}

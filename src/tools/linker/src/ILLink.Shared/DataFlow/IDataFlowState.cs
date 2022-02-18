// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

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
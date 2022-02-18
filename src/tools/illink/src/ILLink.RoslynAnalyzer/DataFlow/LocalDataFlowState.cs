// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using ILLink.Shared.DataFlow;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	public class LocalDataFlowState<TValue, TValueLattice>
		: IDataFlowState<LocalState<TValue>, LocalStateLattice<TValue, TValueLattice>>
		where TValue : struct, IEquatable<TValue>
		where TValueLattice : ILattice<TValue>
	{
		LocalState<TValue> current;
		public LocalState<TValue> Current {
			get => current;
			set => current = value;
		}

		public Box<LocalState<TValue>>? Exception { get; set; }

		public LocalStateLattice<TValue, TValueLattice> Lattice { get; init; }

		public void Set (LocalKey key, TValue value)
		{
			current.Set (key, value);
			if (Exception != null)
				// TODO: optimize this to not meet the whole value, but just modify one value without copying.
				Exception.Value = Lattice.Meet (Exception.Value, current);
		}

		public TValue Get (LocalKey key) => current.Get (key);
	}
}
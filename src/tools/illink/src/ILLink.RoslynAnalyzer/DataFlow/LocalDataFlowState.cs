// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

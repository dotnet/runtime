// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILLink.Shared.DataFlow;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	public class LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice>
		: IDataFlowState<LocalContextState<TValue, TContext>, LocalContextLattice<TValue, TContext, TValueLattice, TContextLattice>>
		where TValue : struct, IEquatable<TValue>
		where TContext : struct, IEquatable<TContext>
		where TValueLattice : ILattice<TValue>
		where TContextLattice : ILattice<TContext>
	{
		LocalContextState<TValue, TContext> current;
		public LocalContextState<TValue, TContext> Current {
			get => current;
			set => current = value;
		}

		public Box<LocalContextState<TValue, TContext>>? Exception { get; set; }

		public LocalContextLattice<TValue, TContext, TValueLattice, TContextLattice> Lattice { get; init; }

		public void Set (LocalKey key, TValue value)
		{
			current.LocalState.Set (key, value);
			if (Exception != null)
				// TODO: optimize this to not meet the whole value, but just modify one value without copying.
				Exception.Value = Lattice.Meet (Exception.Value, current);
		}

		public TValue Get (LocalKey key) => current.LocalState.Get (key);
	}
}

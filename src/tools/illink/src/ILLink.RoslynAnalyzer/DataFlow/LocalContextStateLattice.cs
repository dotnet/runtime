// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILLink.Shared;
using ILLink.Shared.DataFlow;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	public struct LocalContextState<TValue, TContext> : IEquatable<LocalContextState<TValue, TContext>>
		where TValue : IEquatable<TValue>
		where TContext : IEquatable<TContext>
	{
		public LocalState<TValue> LocalState;
		public TContext Context;

		public LocalContextState (LocalState<TValue> localState, TContext context)
		{
			LocalState = localState;
			Context = context;
		}

		public bool Equals (LocalContextState<TValue, TContext> other) =>
			LocalState.Equals (other.LocalState) && Context.Equals (other.Context);

		public override bool Equals (object? obj) => obj is LocalContextState<TValue, TContext> other && Equals (other);
		public override int GetHashCode () => HashUtils.Combine (LocalState, Context);
	}

	public readonly struct LocalContextLattice<TValue, TContext, TValueLattice, TContextLattice> : ILattice<LocalContextState<TValue, TContext>>
		where TValue : struct, IEquatable<TValue>
		where TContext : struct, IEquatable<TContext>
		where TValueLattice : ILattice<TValue>
		where TContextLattice : ILattice<TContext>
	{
		public readonly LocalStateLattice<TValue, TValueLattice> LocalStateLattice;
		public readonly TContextLattice ContextLattice;

		public LocalContextLattice (LocalStateLattice<TValue, TValueLattice> localStateLattice, TContextLattice contextLattice)
		{
			LocalStateLattice = localStateLattice;
			ContextLattice = contextLattice;
		}

		public LocalContextState<TValue, TContext> Top { get; }

		public LocalContextState<TValue, TContext> Meet (LocalContextState<TValue, TContext> left, LocalContextState<TValue, TContext> right)
		{
			return new LocalContextState<TValue, TContext> {
				LocalState = LocalStateLattice.Meet (left.LocalState, right.LocalState),
				Context = ContextLattice.Meet (left.Context, right.Context)
			};
		}
	}
}

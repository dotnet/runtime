// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILLink.Shared;
using ILLink.Shared.DataFlow;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	// A lattice value that holds both a local state, and a context
	public struct LocalStateAndContext<TValue, TContext> : IEquatable<LocalStateAndContext<TValue, TContext>>
		where TValue : IEquatable<TValue>
		where TContext : IEquatable<TContext>
	{
		public LocalState<TValue> LocalState;
		public TContext Context;

		public LocalStateAndContext (LocalState<TValue> localState, TContext context)
		{
			LocalState = localState;
			Context = context;
		}

		public bool Equals (LocalStateAndContext<TValue, TContext> other) =>
			LocalState.Equals (other.LocalState) && Context.Equals (other.Context);

		public override bool Equals (object? obj) => obj is LocalStateAndContext<TValue, TContext> other && Equals (other);
		public override int GetHashCode () => HashUtils.Combine (LocalState, Context);
	}

	public readonly struct LocalStateAndContextLattice<TValue, TContext, TValueLattice, TContextLattice> : ILattice<LocalStateAndContext<TValue, TContext>>
		where TValue : struct, IEquatable<TValue>
		where TContext : struct, IEquatable<TContext>
		where TValueLattice : ILattice<TValue>
		where TContextLattice : ILattice<TContext>
	{
		public readonly LocalStateLattice<TValue, TValueLattice> LocalStateLattice;
		public readonly TContextLattice ContextLattice;

		public LocalStateAndContextLattice (LocalStateLattice<TValue, TValueLattice> localStateLattice, TContextLattice contextLattice)
		{
			LocalStateLattice = localStateLattice;
			ContextLattice = contextLattice;
			Top = new (LocalStateLattice.Top, ContextLattice.Top);
		}

		public LocalStateAndContext<TValue, TContext> Top { get; }

		public LocalStateAndContext<TValue, TContext> Meet (LocalStateAndContext<TValue, TContext> left, LocalStateAndContext<TValue, TContext> right)
		{
			return new LocalStateAndContext<TValue, TContext> {
				LocalState = LocalStateLattice.Meet (left.LocalState, right.LocalState),
				Context = ContextLattice.Meet (left.Context, right.Context)
			};
		}
	}
}

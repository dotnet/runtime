// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILLink.Shared;
using ILLink.Shared.DataFlow;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	// A lattice value that holds both a local state, and a context
	public struct LocalStateAndContext<TValue, TContext, TConditionValue> : IEquatable<LocalStateAndContext<TValue, TContext, TConditionValue>>
		where TValue : IEquatable<TValue>
		where TContext : IEquatable<TContext>
		where TConditionValue : struct
	{
		public LocalState<TValue> LocalState;
		public TContext Context;
		public TConditionValue ReturnValue;

		public LocalStateAndContext (LocalState<TValue> localState, TContext context, TConditionValue returnValue)
		{
			LocalState = localState;
			Context = context;
			ReturnValue = returnValue;
		}

		public bool Equals (LocalStateAndContext<TValue, TContext, TConditionValue> other) =>
			LocalState.Equals (other.LocalState) && Context.Equals (other.Context) && ReturnValue.Equals (other.ReturnValue);

		public override bool Equals (object? obj) => obj is LocalStateAndContext<TValue, TContext, TConditionValue> other && Equals (other);
		public override int GetHashCode () => HashUtils.Combine (LocalState, Context, ReturnValue);
	}

	public readonly struct LocalStateAndContextLattice<TValue, TContext, TValueLattice, TContextLattice, TConditionValue, TConditionLattice> : ILattice<LocalStateAndContext<TValue, TContext, TConditionValue>>
		where TValue : struct, IEquatable<TValue>
		where TContext : struct, IEquatable<TContext>
		where TValueLattice : ILattice<TValue>
		where TContextLattice : ILattice<TContext>
		where TConditionValue : struct, IEquatable<TConditionValue>
		where TConditionLattice : ILattice<TConditionValue>
	{
		public readonly LocalStateLattice<TValue, TValueLattice> LocalStateLattice;
		public readonly TContextLattice ContextLattice;
		public readonly TConditionLattice ConditionLattice;

		public LocalStateAndContextLattice (LocalStateLattice<TValue, TValueLattice> localStateLattice, TContextLattice contextLattice, TConditionLattice conditionLattice)
		{
			LocalStateLattice = localStateLattice;
			ContextLattice = contextLattice;
			ConditionLattice = conditionLattice;
		}

		public LocalStateAndContext<TValue, TContext, TConditionValue> Top { get; }

		public LocalStateAndContext<TValue, TContext, TConditionValue> Meet (LocalStateAndContext<TValue, TContext, TConditionValue> left, LocalStateAndContext<TValue, TContext, TConditionValue> right)
		{
			return new LocalStateAndContext<TValue, TContext, TConditionValue> {
				LocalState = LocalStateLattice.Meet (left.LocalState, right.LocalState),
				Context = ContextLattice.Meet (left.Context, right.Context)
			};
		}
	}
}

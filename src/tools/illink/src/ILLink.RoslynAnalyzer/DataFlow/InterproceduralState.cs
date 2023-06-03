// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.DataFlow;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	// Tracks the set of methods which get analyzed together during interprocedural analysis,
	// and the possible states of hoisted locals in state machine methods and lambdas/local functions.
	public struct InterproceduralState<TValue, TValueLattice> : IEquatable<InterproceduralState<TValue, TValueLattice>>
		where TValue : struct, IEquatable<TValue>
		where TValueLattice : ILattice<TValue>
	{
		public ValueSet<MethodBodyValue> Methods;

		// The HoistedLocals dictionary has a default value of MaybeLattice.Top (effectively null),
		// for any local that has not been discovered to be captured by a nested function.
		// Once we discover that a local is captured, it gets the value TValueLattice.Top
		// (in our case the "empty" MultiValue), and from then on reading/writing the local will use this
		// dictionary instead of the per-method dictionary.
		public DefaultValueDictionary<LocalKey, Maybe<TValue>> HoistedLocals;

		readonly InterproceduralStateLattice<TValue, TValueLattice> lattice;

		public InterproceduralState (
			ValueSet<MethodBodyValue> methods,
			DefaultValueDictionary<LocalKey, Maybe<TValue>> hoistedLocals,
			InterproceduralStateLattice<TValue, TValueLattice> lattice)
		{
			Methods = methods;
			HoistedLocals = hoistedLocals;
			this.lattice = lattice;
		}

		public bool Equals (InterproceduralState<TValue, TValueLattice> other)
			=> Methods.Equals (other.Methods) && HoistedLocals.Equals (other.HoistedLocals);

		public override bool Equals (object obj)
			=> obj is InterproceduralState<TValue, TValueLattice> inst && Equals (inst);

		public override int GetHashCode ()
			=> throw new NotImplementedException ();

		public InterproceduralState<TValue, TValueLattice> Clone ()
			=> new (Methods.DeepCopy (),
			HoistedLocals.Clone (), lattice);

		public void TrackMethod (MethodBodyValue method)
		{
			var methodsList = new List<MethodBodyValue> (Methods);
			methodsList.Add (method);
			Methods = new ValueSet<MethodBodyValue> (methodsList);
		}

		public void TrackHoistedLocal (LocalKey key)
		{
			var existingValue = HoistedLocals.Get (key);
			if (existingValue.MaybeValue != null)
				return; // Already tracked

			HoistedLocals.Set (key, new Maybe<TValue> (lattice.HoistedLocalLattice.ValueLattice.ValueLattice.Top));
		}

		public bool TrySetHoistedLocal (LocalKey key, TValue value)
		{
			var existingValue = HoistedLocals.Get (key);
			if (existingValue.MaybeValue == null)
				return false;

			// For hoisted locals, we track the entire set of assigned values seen
			// in the closure of a method, so setting a hoisted local value meets
			// it with any existing value.
			HoistedLocals.Set (key,
				lattice.HoistedLocalLattice.ValueLattice.Meet (
					existingValue, new (value)));
			return true;
		}

		public bool TryGetHoistedLocal (LocalKey key, [NotNullWhen (true)] out TValue? value)
			=> (value = HoistedLocals.Get (key).MaybeValue) != null;
	}

	public struct InterproceduralStateLattice<TValue, TValueLattice> : ILattice<InterproceduralState<TValue, TValueLattice>>
		where TValue : struct, IEquatable<TValue>
		where TValueLattice : ILattice<TValue>
	{
		public readonly ValueSetLattice<MethodBodyValue> MethodLattice;

		public readonly DictionaryLattice<LocalKey, Maybe<TValue>, MaybeLattice<TValue, TValueLattice>> HoistedLocalLattice;

		public InterproceduralStateLattice (
			ValueSetLattice<MethodBodyValue> methodLattice,
			DictionaryLattice<LocalKey, Maybe<TValue>, MaybeLattice<TValue, TValueLattice>> hoistedLocalLattice
		)
		{
			MethodLattice = methodLattice;
			HoistedLocalLattice = hoistedLocalLattice;
		}

		public InterproceduralState<TValue, TValueLattice> Top => new (MethodLattice.Top,
			HoistedLocalLattice.Top, this);

		public InterproceduralState<TValue, TValueLattice> Meet (InterproceduralState<TValue, TValueLattice> left, InterproceduralState<TValue, TValueLattice> right)
			=> new (
				MethodLattice.Meet (left.Methods, right.Methods),
				HoistedLocalLattice.Meet (left.HoistedLocals, right.HoistedLocals),
				this);
	}
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILLink.Shared;
using ILLink.Shared.DataFlow;
using Mono.Cecil;
using Mono.Cecil.Cil;
using HoistedLocalState = ILLink.Shared.DataFlow.DefaultValueDictionary<
	Mono.Linker.Dataflow.HoistedLocalKey,
	ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>>;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
{
	// Tracks the set of methods which get analyzer together during interprocedural analysis,
	// and the possible states of hoisted locals in state machine methods and lambdas/local functions.
	struct InterproceduralState : IEquatable<InterproceduralState>
	{
		public ValueSet<MethodIL> MethodBodies;
		public HoistedLocalState HoistedLocals;
		readonly InterproceduralStateLattice lattice;

		public InterproceduralState (ValueSet<MethodIL> methodBodies, HoistedLocalState hoistedLocals, InterproceduralStateLattice lattice)
			=> (MethodBodies, HoistedLocals, this.lattice) = (methodBodies, hoistedLocals, lattice);

		public bool Equals (InterproceduralState other)
			=> MethodBodies.Equals (other.MethodBodies) && HoistedLocals.Equals (other.HoistedLocals);

		public override bool Equals (object? obj)
			=> obj is InterproceduralState state && Equals (state);

		public override int GetHashCode () => HashUtils.Combine (MethodBodies.GetHashCode (), HoistedLocals.GetHashCode ());

		public InterproceduralState Clone ()
			=> new (MethodBodies.DeepCopy (), HoistedLocals.Clone (), lattice);

		public void TrackMethod (MethodDefinition method)
		{
			if (method.Body is not MethodBody methodBody)
				return;

			TrackMethod (methodBody);
		}

		public void TrackMethod (MethodBody methodBody)
		{
			TrackMethod (lattice.Context.GetMethodIL (methodBody));
		}

		public void TrackMethod (MethodIL methodIL)
		{
			// Work around the fact that ValueSet is readonly
			var methodsList = new List<MethodIL> (MethodBodies);
			methodsList.Add (methodIL);

			// For state machine methods, also scan the state machine members.
			// Simplification: assume that all generated methods of the state machine type are
			// reached at the point where the state machine method is reached.
			if (CompilerGeneratedState.TryGetStateMachineType (methodIL.Method, out TypeDefinition? stateMachineType)) {
				foreach (var stateMachineMethod in stateMachineType.Methods) {
					Debug.Assert (!CompilerGeneratedNames.IsLambdaOrLocalFunction (stateMachineMethod.Name));
					if (stateMachineMethod.Body is MethodBody stateMachineMethodBody)
						methodsList.Add (lattice.Context.GetMethodIL (stateMachineMethodBody));
				}
			}

			MethodBodies = new ValueSet<MethodIL> (methodsList);
		}

		public void SetHoistedLocal (HoistedLocalKey key, MultiValue value)
		{
			// For hoisted locals, we track the entire set of assigned values seen
			// in the closure of a method, so setting a hoisted local value meets
			// it with any existing value.
			HoistedLocals.Set (key,
				lattice.HoistedLocalsLattice.ValueLattice.Meet (
					HoistedLocals.Get (key), value));
		}

		public MultiValue GetHoistedLocal (HoistedLocalKey key)
			=> HoistedLocals.Get (key);
	}

	readonly struct InterproceduralStateLattice : ILattice<InterproceduralState>
	{
		public readonly ValueSetLattice<MethodIL> MethodBodyLattice;
		public readonly DictionaryLattice<HoistedLocalKey, MultiValue, ValueSetLattice<SingleValue>> HoistedLocalsLattice;
		public readonly LinkContext Context;

		public InterproceduralStateLattice (
			ValueSetLattice<MethodIL> methodBodyLattice,
			DictionaryLattice<HoistedLocalKey, MultiValue, ValueSetLattice<SingleValue>> hoistedLocalsLattice,
			LinkContext context)
			=> (MethodBodyLattice, HoistedLocalsLattice, Context) = (methodBodyLattice, hoistedLocalsLattice, context);

		public InterproceduralState Top => new InterproceduralState (MethodBodyLattice.Top, HoistedLocalsLattice.Top, this);

		public InterproceduralState Meet (InterproceduralState left, InterproceduralState right)
			=> new (
				MethodBodyLattice.Meet (left.MethodBodies, right.MethodBodies),
				HoistedLocalsLattice.Meet (left.HoistedLocals, right.HoistedLocals),
				this);
	}
}

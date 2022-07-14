// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using Mono.Cecil;
using Mono.Cecil.Cil;
using HoistedLocalState = ILLink.Shared.DataFlow.DefaultValueDictionary<
	Mono.Linker.Dataflow.HoistedLocalKey,
	ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>>;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
{
	// Wrapper that implements IEquatable for MethodBody.
	readonly record struct MethodBodyValue (MethodBody MethodBody);

	// Tracks the set of methods which get analyzer together during interprocedural analysis,
	// and the possible states of hoisted locals in state machine methods and lambdas/local functions.
	struct InterproceduralState : IEquatable<InterproceduralState>
	{
		public ValueSet<MethodBodyValue> MethodBodies;
		public HoistedLocalState HoistedLocals;
		readonly InterproceduralStateLattice lattice;

		public InterproceduralState (ValueSet<MethodBodyValue> methodBodies, HoistedLocalState hoistedLocals, InterproceduralStateLattice lattice)
			=> (MethodBodies, HoistedLocals, this.lattice) = (methodBodies, hoistedLocals, lattice);

		public bool Equals (InterproceduralState other)
			=> MethodBodies.Equals (other.MethodBodies) && HoistedLocals.Equals (other.HoistedLocals);

		public InterproceduralState Clone ()
			=> new (MethodBodies.Clone (), HoistedLocals.Clone (), lattice);

		public void TrackMethod (MethodDefinition method)
		{
			if (method.Body is not MethodBody methodBody)
				return;

			TrackMethod (methodBody);
		}

		public void TrackMethod (MethodBody methodBody)
		{
			// Work around the fact that ValueSet is readonly
			var methodsList = new List<MethodBodyValue> (MethodBodies);
			methodsList.Add (new MethodBodyValue (methodBody));

			// For state machine methods, also scan the state machine members.
			// Simplification: assume that all generated methods of the state machine type are
			// reached at the point where the state machine method is reached.
			if (CompilerGeneratedState.TryGetStateMachineType (methodBody.Method, out TypeDefinition? stateMachineType)) {
				foreach (var stateMachineMethod in stateMachineType.Methods) {
					Debug.Assert (!CompilerGeneratedNames.IsLambdaOrLocalFunction (stateMachineMethod.Name));
					if (stateMachineMethod.Body is MethodBody stateMachineMethodBody)
						methodsList.Add (new MethodBodyValue (stateMachineMethodBody));
				}
			}

			MethodBodies = new ValueSet<MethodBodyValue> (methodsList);
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

	struct InterproceduralStateLattice : ILattice<InterproceduralState>
	{
		public readonly ValueSetLattice<MethodBodyValue> MethodBodyLattice;
		public readonly DictionaryLattice<HoistedLocalKey, MultiValue, ValueSetLattice<SingleValue>> HoistedLocalsLattice;

		public InterproceduralStateLattice (
			ValueSetLattice<MethodBodyValue> methodBodyLattice,
			DictionaryLattice<HoistedLocalKey, MultiValue, ValueSetLattice<SingleValue>> hoistedLocalsLattice)
			=> (MethodBodyLattice, HoistedLocalsLattice) = (methodBodyLattice, hoistedLocalsLattice);

		public InterproceduralState Top => new InterproceduralState (MethodBodyLattice.Top, HoistedLocalsLattice.Top, this);

		public InterproceduralState Meet (InterproceduralState left, InterproceduralState right)
			=> new (
				MethodBodyLattice.Meet (left.MethodBodies, right.MethodBodies),
				HoistedLocalsLattice.Meet (left.HoistedLocals, right.HoistedLocals),
				this);
	}
}
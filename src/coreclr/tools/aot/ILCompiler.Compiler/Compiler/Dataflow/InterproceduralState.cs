// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using ILLink.Shared.DataFlow;
using Internal.IL;
using Internal.TypeSystem;

using HoistedLocalState = ILLink.Shared.DataFlow.DefaultValueDictionary<
    ILCompiler.Dataflow.HoistedLocalKey,
    ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>>;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

#nullable enable

namespace ILCompiler.Dataflow
{
    // Wrapper that implements IEquatable for MethodBody.
    internal readonly record struct MethodBodyValue(MethodIL MethodBody) : IEquatable<MethodBodyValue>
    {
        bool IEquatable<MethodBodyValue>.Equals(ILCompiler.Dataflow.MethodBodyValue other)
            => other.MethodBody.OwningMethod == MethodBody.OwningMethod;

        public override int GetHashCode() => MethodBody.OwningMethod.GetHashCode();
    }

    // Tracks the set of methods which get analyzer together during interprocedural analysis,
    // and the possible states of hoisted locals in state machine methods and lambdas/local functions.
#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T>
    internal struct InterproceduralState : IEquatable<InterproceduralState>
#pragma warning restore CA1067 // Override Object.Equals(object) when implementing IEquatable<T>
    {
        private readonly ILProvider _ilProvider;
        public ValueSet<MethodBodyValue> MethodBodies;
        public HoistedLocalState HoistedLocals;
        private readonly InterproceduralStateLattice lattice;

        public InterproceduralState(ILProvider ilProvider, ValueSet<MethodBodyValue> methodBodies, HoistedLocalState hoistedLocals, InterproceduralStateLattice lattice)
            => (_ilProvider, MethodBodies, HoistedLocals, this.lattice) = (ilProvider, methodBodies, hoistedLocals, lattice);

        public bool Equals(InterproceduralState other)
            => MethodBodies.Equals(other.MethodBodies) && HoistedLocals.Equals(other.HoistedLocals);

        public InterproceduralState Clone()
            => new(_ilProvider, MethodBodies.Clone(), HoistedLocals.Clone(), lattice);

        public void TrackMethod(MethodDesc method)
        {
            if (!TryGetMethodBody(method, out MethodIL? methodBody))
                return;

            TrackMethod(methodBody);
        }

        public void TrackMethod(MethodIL methodBody)
        {
            Debug.Assert(methodBody.GetMethodILDefinition() == methodBody);
            methodBody = GetInstantiatedMethodIL(methodBody);

            // Work around the fact that ValueSet is readonly
            var methodsList = new List<MethodBodyValue>(MethodBodies);
            methodsList.Add(new MethodBodyValue(methodBody));

            // For state machine methods, also scan the state machine members.
            // Simplification: assume that all generated methods of the state machine type are
            // reached at the point where the state machine method is reached.
            if (CompilerGeneratedState.TryGetStateMachineType(methodBody.OwningMethod, out MetadataType? stateMachineType))
            {
                foreach (var stateMachineMethod in stateMachineType.GetMethods())
                {
                    Debug.Assert(!CompilerGeneratedNames.IsLambdaOrLocalFunction(stateMachineMethod.Name));
                    if (TryGetMethodBody(stateMachineMethod, out MethodIL? stateMachineMethodBody))
                    {
                        stateMachineMethodBody = GetInstantiatedMethodIL(stateMachineMethodBody);
                        methodsList.Add(new MethodBodyValue(stateMachineMethodBody));
                    }
                }
            }

            MethodBodies = new ValueSet<MethodBodyValue>(methodsList);

            static MethodIL GetInstantiatedMethodIL(MethodIL methodIL)
            {
                if (methodIL.OwningMethod.HasInstantiation || methodIL.OwningMethod.OwningType.HasInstantiation)
                {
                    // We instantiate the body over the generic parameters.
                    //
                    // This will transform references like "call Foo<!0>.Method(!0 arg)" into
                    // "call Foo<T>.Method(T arg)". We do this to avoid getting confused about what
                    // context the generic variables refer to - in the above example, we would see
                    // two !0's - one refers to the generic parameter of the type that owns the method with
                    // the call, but the other one (in the signature of "Method") actually refers to
                    // the generic parameter of Foo.
                    //
                    // If we don't do this translation, retrieving the signature of the called method
                    // would attempt to do bogus substitutions.
                    //
                    // By doing the following transformation, we ensure we don't see the generic variables
                    // that need to be bound to the context of the currently analyzed method.
                    methodIL = new InstantiatedMethodIL(methodIL.OwningMethod, methodIL);
                }

                return methodIL;
            }
        }

        public void SetHoistedLocal(HoistedLocalKey key, MultiValue value)
        {
            // For hoisted locals, we track the entire set of assigned values seen
            // in the closure of a method, so setting a hoisted local value meets
            // it with any existing value.
            HoistedLocals.Set(key,
                lattice.HoistedLocalsLattice.ValueLattice.Meet(
                    HoistedLocals.Get(key), value));
        }

        public MultiValue GetHoistedLocal(HoistedLocalKey key)
            => HoistedLocals.Get(key);

        private bool TryGetMethodBody(MethodDesc method, [NotNullWhen(true)] out MethodIL? methodBody)
        {
            methodBody = null;

            if (method.IsPInvoke)
                return false;

            MethodIL methodIL = _ilProvider.GetMethodIL(method);
            if (methodIL == null)
                return false;

            methodBody = methodIL;
            return true;
        }
    }

    internal struct InterproceduralStateLattice : ILattice<InterproceduralState>
    {
        private readonly ILProvider _ilProvider;
        public readonly ValueSetLattice<MethodBodyValue> MethodBodyLattice;
        public readonly DictionaryLattice<HoistedLocalKey, MultiValue, ValueSetLattice<SingleValue>> HoistedLocalsLattice;

        public InterproceduralStateLattice(
            ILProvider ilProvider,
            ValueSetLattice<MethodBodyValue> methodBodyLattice,
            DictionaryLattice<HoistedLocalKey, MultiValue, ValueSetLattice<SingleValue>> hoistedLocalsLattice)
            => (_ilProvider, MethodBodyLattice, HoistedLocalsLattice) = (ilProvider, methodBodyLattice, hoistedLocalsLattice);

        public InterproceduralState Top => new InterproceduralState(_ilProvider, MethodBodyLattice.Top, HoistedLocalsLattice.Top, this);

        public InterproceduralState Meet(InterproceduralState left, InterproceduralState right)
            => new(
                _ilProvider,
                MethodBodyLattice.Meet(left.MethodBodies, right.MethodBodies),
                HoistedLocalsLattice.Meet(left.HoistedLocals, right.HoistedLocals),
                this);
    }
}

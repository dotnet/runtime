// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace ILLink.RoslynAnalyzer.DataFlow
{
    public class LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice>
        : IDataFlowState<LocalStateAndContext<TValue, TContext>, LocalStateAndContextLattice<TValue, TContext, TValueLattice, TContextLattice>>
        where TValue : struct, IEquatable<TValue>, IDeepCopyValue<TValue>, IValueWithMutableState
        where TContext : struct, IEquatable<TContext>
        where TValueLattice : ILattice<TValue>
        where TContextLattice : ILattice<TContext>
    {
        private LocalStateAndContext<TValue, TContext> current;
        private Box<LocalStateAndContext<TValue, TContext>>? _exception;
        private bool _ownsExceptionState;
        private bool _mutableLocalsInitialized;
        private HashSet<LocalKey>? _mutableLocals;
        public LocalStateAndContext<TValue, TContext> Current
        {
            get => current;
            set
            {
                current = value;
                _ownsExceptionState = false;
                _mutableLocalsInitialized = false;
                _mutableLocals = null;
            }
        }

        public Box<LocalStateAndContext<TValue, TContext>>? Exception
        {
            get => _exception;
            set
            {
                _exception = value;
                _ownsExceptionState = false;
                _mutableLocalsInitialized = false;
                _mutableLocals = null;
            }
        }

        public LocalStateAndContextLattice<TValue, TContext, TValueLattice, TContextLattice> Lattice { get; init; }

        internal void RemoveLocals(ControlFlowRegion region)
        {
            Debug.Assert(!_ownsExceptionState && !_mutableLocalsInitialized);
            TValue top = Lattice.LocalStateLattice.Lattice.ValueLattice.Top;
            foreach (ILocalSymbol local in region.Locals)
                current.LocalState.Set(new LocalKey(local), top);
            foreach (CaptureId capture in region.CaptureIds)
            {
                current.LocalState.Set(new LocalKey(capture), top);
                current.LocalState.CapturedReferences.Set(capture, default);
            }
        }

        public void Set(LocalKey key, TValue value)
        {
            current.LocalState.Set(key, value);
            if (_exception is null)
                return;

            if (!_ownsExceptionState)
            {
                // The solver retains the pre-transfer value for convergence comparisons.
                _exception.Value = Lattice.Meet(_exception.Value, current);
                _ownsExceptionState = true;
                return;
            }

            if (!_mutableLocalsInitialized)
            {
                foreach (var local in current.LocalState.Dictionary)
                {
                    if (local.Value.HasMutableState)
                        (_mutableLocals ??= new HashSet<LocalKey>()).Add(local.Key);
                }
                _mutableLocalsInitialized = true;
            }
            else if (value.HasMutableState)
            {
                (_mutableLocals ??= new HashSet<LocalKey>()).Add(key);
            }
            else
            {
                _mutableLocals?.Remove(key);
            }

            var exceptionValue = _exception.Value;
            var valueLattice = Lattice.LocalStateLattice.Lattice.ValueLattice;
            exceptionValue.LocalState.Set(key, valueLattice.Meet(exceptionValue.LocalState.Get(key), value));
            // Array values can change through aliases without another assignment to their local.
            if (_mutableLocals is not null)
            {
                foreach (LocalKey mutableLocal in _mutableLocals)
                {
                    if (!mutableLocal.Equals(key))
                        exceptionValue.LocalState.Set(mutableLocal, valueLattice.Meet(exceptionValue.LocalState.Get(mutableLocal), current.LocalState.Get(mutableLocal)));
                }
            }
            // Captured deconstruction receivers can contain the same mutable array values.
            exceptionValue.LocalState.CapturedTargetValues = Lattice.LocalStateLattice.CapturedTargetValueLattice.Meet(
                exceptionValue.LocalState.CapturedTargetValues, current.LocalState.CapturedTargetValues);
            _exception.Value = exceptionValue;
        }

        public TValue Get(LocalKey key) => current.LocalState.Get(key);

        public LocalStateAndContext<TValue, TContext> CreateBranchOutput(bool canTransferOwnership)
        {
            if (!canTransferOwnership)
                return Lattice.Meet(Lattice.Top, current);

            // Maps are owned, but their values can still alias hoisted arrays. Detach those
            // payloads before transferring the maps, without rebuilding all immutable entries.
            KeyValuePair<LocalKey, TValue>? firstLocal = null;
            List<KeyValuePair<LocalKey, TValue>>? otherLocals = null;
            foreach (var local in current.LocalState.Dictionary)
            {
                if (local.Value.HasMutableState)
                {
                    var copy = new KeyValuePair<LocalKey, TValue>(local.Key, local.Value.DeepCopy());
                    if (firstLocal is null)
                        firstLocal = copy;
                    else
                        (otherLocals ??= new()).Add(copy);
                }
            }
            if (firstLocal is { } firstLocalCopy)
                current.LocalState.Set(firstLocalCopy.Key, firstLocalCopy.Value);
            if (otherLocals is not null)
            {
                foreach (var copy in otherLocals)
                    current.LocalState.Set(copy.Key, copy.Value);
            }

            KeyValuePair<CapturedTargetKey, CapturedTargetValue<TValue>>? firstTarget = null;
            List<KeyValuePair<CapturedTargetKey, CapturedTargetValue<TValue>>>? otherTargets = null;
            foreach (var captured in current.LocalState.CapturedTargetValues)
            {
                if (captured.Value.HasValue && captured.Value.Value.HasMutableState)
                {
                    var copy = new KeyValuePair<CapturedTargetKey, CapturedTargetValue<TValue>>(
                        captured.Key, new CapturedTargetValue<TValue>(captured.Value.Value.DeepCopy()));
                    if (firstTarget is null)
                        firstTarget = copy;
                    else
                        (otherTargets ??= new()).Add(copy);
                }
            }
            if (firstTarget is { } firstTargetCopy)
                current.LocalState.CapturedTargetValues.Set(firstTargetCopy.Key, firstTargetCopy.Value);
            if (otherTargets is not null)
            {
                foreach (var copy in otherTargets)
                    current.LocalState.CapturedTargetValues.Set(copy.Key, copy.Value);
            }

            return new LocalStateAndContext<TValue, TContext>(
                current.LocalState,
                Lattice.ContextLattice.Meet(Lattice.ContextLattice.Top, current.Context));
        }
    }
}

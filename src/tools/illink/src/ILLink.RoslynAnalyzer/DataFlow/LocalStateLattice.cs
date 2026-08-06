// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace ILLink.RoslynAnalyzer.DataFlow
{
    public readonly struct LocalKey : IEquatable<LocalKey>
    {
        private readonly ILocalSymbol? Local;

        private readonly CaptureId? CaptureId;

        public LocalKey(ILocalSymbol symbol) => (Local, CaptureId) = (symbol, null);

        public LocalKey(CaptureId captureId) => (Local, CaptureId) = (null, captureId);

        public bool Equals(LocalKey other) => SymbolEqualityComparer.Default.Equals(Local, other.Local) &&
            (CaptureId?.Equals(other.CaptureId) ?? other.CaptureId == null);

        public override bool Equals(object obj)
            => obj is LocalKey inst && Equals(inst);

        public override int GetHashCode()
            => CaptureId is null ? SymbolEqualityComparer.Default.GetHashCode(Local) : CaptureId.GetHashCode();

        public override string ToString()
        {
            if (Local != null)
                return Local.ToString();
            return $"capture {CaptureId.GetHashCode()}";
        }
    }

    public readonly struct CapturedTargetKey : IEquatable<CapturedTargetKey>
    {
        private readonly IOperation Operation;

        public CapturedTargetKey(IOperation operation) => Operation = operation;

        public bool Equals(CapturedTargetKey other) => Operation == other.Operation;

        public override bool Equals(object obj)
            => obj is CapturedTargetKey inst && Equals(inst);

        public override int GetHashCode() => Operation.GetHashCode();
    }

    public readonly struct CapturedTargetValue<TValue> : IEquatable<CapturedTargetValue<TValue>>, IDeepCopyValue<CapturedTargetValue<TValue>>
        where TValue : IEquatable<TValue>
    {
        public readonly bool HasValue;

        public readonly TValue Value;

        public CapturedTargetValue(TValue value) => (HasValue, Value) = (true, value);

        public bool Equals(CapturedTargetValue<TValue> other) =>
            HasValue == other.HasValue &&
            (!HasValue || EqualityComparer<TValue>.Default.Equals(Value, other.Value));

        public override bool Equals(object obj)
            => obj is CapturedTargetValue<TValue> inst && Equals(inst);

        public override int GetHashCode() => HasValue ? EqualityComparer<TValue>.Default.GetHashCode(Value) : 0;

        public CapturedTargetValue<TValue> DeepCopy() =>
            HasValue
                ? new CapturedTargetValue<TValue>(
                    Value is IDeepCopyValue<TValue> copyValue ? copyValue.DeepCopy() : Value)
                : default;
    }

    public readonly struct CapturedTargetValueLattice<TValue, TValueLattice> : ILattice<CapturedTargetValue<TValue>>
        where TValue : IEquatable<TValue>
        where TValueLattice : ILattice<TValue>
    {
        private readonly TValueLattice _valueLattice;

        public CapturedTargetValueLattice(TValueLattice valueLattice) => _valueLattice = valueLattice;

        public CapturedTargetValue<TValue> Top => default;

        public CapturedTargetValue<TValue> Meet(CapturedTargetValue<TValue> left, CapturedTargetValue<TValue> right)
        {
            if (!left.HasValue)
                return right.DeepCopy();
            if (!right.HasValue)
                return left.DeepCopy();
            return new CapturedTargetValue<TValue>(_valueLattice.Meet(left.Value, right.Value));
        }
    }

    public struct LocalState<TValue> : IEquatable<LocalState<TValue>>
        where TValue : IEquatable<TValue>
    {
        public DefaultValueDictionary<LocalKey, TValue> Dictionary;

        // Stores any operations which are captured by reference in a FlowCaptureOperation.
        // Only stores captures which are assigned through. Captures of the values of operations
        // are tracked as part of the dictionary of values, keyed by LocalKey.
        public DefaultValueDictionary<CaptureId, ValueSet<CapturedReferenceValue>> CapturedReferences;

        // Stores target receiver and index values evaluated by deconstruction l-value captures.
        public DefaultValueDictionary<CapturedTargetKey, CapturedTargetValue<TValue>> CapturedTargetValues;

        public LocalState(
            DefaultValueDictionary<LocalKey, TValue> dictionary,
            DefaultValueDictionary<CaptureId, ValueSet<CapturedReferenceValue>> capturedReferences,
            DefaultValueDictionary<CapturedTargetKey, CapturedTargetValue<TValue>> capturedTargetValues)
        {
            Dictionary = dictionary;
            CapturedReferences = capturedReferences;
            CapturedTargetValues = capturedTargetValues;
        }

        public LocalState(DefaultValueDictionary<LocalKey, TValue> dictionary)
            : this(
                dictionary,
                new DefaultValueDictionary<CaptureId, ValueSet<CapturedReferenceValue>>(default(ValueSet<CapturedReferenceValue>)),
                new DefaultValueDictionary<CapturedTargetKey, CapturedTargetValue<TValue>>(default(CapturedTargetValue<TValue>)))
        {
        }

        public bool Equals(LocalState<TValue> other) =>
            Dictionary.Equals(other.Dictionary) &&
            CapturedReferences.Equals(other.CapturedReferences) &&
            CapturedTargetValues.Equals(other.CapturedTargetValues);

        public override bool Equals(object obj)
            => obj is LocalState<TValue> inst && Equals(inst);

        public TValue Get(LocalKey key) => Dictionary.Get(key);

        // Local dataflow states are mutable and should never be used as dictionary keys.
        public override int GetHashCode()
            => throw new NotImplementedException();

        public void Set(LocalKey key, TValue value) => Dictionary.Set(key, value);

        public override string ToString() => Dictionary.ToString();
    }

    // Wrapper struct exists purely to substitute a concrete LocalKey for TKey of DictionaryLattice
    public readonly struct LocalStateLattice<TValue, TValueLattice> : ILattice<LocalState<TValue>>
        where TValue : struct, IEquatable<TValue>
        where TValueLattice : ILattice<TValue>
    {
        public readonly DictionaryLattice<LocalKey, TValue, TValueLattice> Lattice;
        public readonly DictionaryLattice<CaptureId, ValueSet<CapturedReferenceValue>, ValueSetLattice<CapturedReferenceValue>> CapturedReferenceLattice;
        public readonly DictionaryLattice<CapturedTargetKey, CapturedTargetValue<TValue>, CapturedTargetValueLattice<TValue, TValueLattice>> CapturedTargetValueLattice;

        public LocalStateLattice(TValueLattice valueLattice)
        {
            Lattice = new DictionaryLattice<LocalKey, TValue, TValueLattice>(valueLattice);
            CapturedReferenceLattice = new DictionaryLattice<CaptureId, ValueSet<CapturedReferenceValue>, ValueSetLattice<CapturedReferenceValue>>(default(ValueSetLattice<CapturedReferenceValue>));
            CapturedTargetValueLattice = new DictionaryLattice<CapturedTargetKey, CapturedTargetValue<TValue>, CapturedTargetValueLattice<TValue, TValueLattice>>(
                new CapturedTargetValueLattice<TValue, TValueLattice>(valueLattice));
            Top = new(Lattice.Top);
        }

        public LocalState<TValue> Top { get; }

        public LocalState<TValue> Meet(LocalState<TValue> left, LocalState<TValue> right)
        {
            var dictionary = Lattice.Meet(left.Dictionary, right.Dictionary);
            var capturedProperties = CapturedReferenceLattice.Meet(left.CapturedReferences, right.CapturedReferences);
            var capturedTargetValues = CapturedTargetValueLattice.Meet(left.CapturedTargetValues, right.CapturedTargetValues);
            return new LocalState<TValue>(dictionary, capturedProperties, capturedTargetValues);
        }
    }
}

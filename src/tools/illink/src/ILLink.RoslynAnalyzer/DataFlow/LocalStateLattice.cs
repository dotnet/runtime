// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace ILLink.RoslynAnalyzer.DataFlow
{
    public enum LocalValueKind
    {
        Top,
        // Bottom for this lattice: merging it with any value remains Unknown.
        Unknown,
        Scalar,
        Tuple
    }

    public readonly struct LocalValue<TValue> : IEquatable<LocalValue<TValue>>, IDeepCopyValue<LocalValue<TValue>>
        where TValue : IEquatable<TValue>
    {
        public LocalValueKind Kind { get; }

        public TValue ScalarValue { get; }

        public ImmutableArray<LocalValue<TValue>> Elements { get; }

        public static LocalValue<TValue> Top => new(LocalValueKind.Top);

        public static LocalValue<TValue> Unknown => new(LocalValueKind.Unknown);

        private LocalValue(LocalValueKind kind)
        {
            Kind = kind;
            ScalarValue = default!;
            Elements = default;
        }

        public LocalValue(TValue value)
        {
            Kind = LocalValueKind.Scalar;
            ScalarValue = value;
            Elements = default;
        }

        public LocalValue(ImmutableArray<LocalValue<TValue>> elements)
        {
            Kind = LocalValueKind.Tuple;
            ScalarValue = default!;
            Elements = elements;
        }

        public bool Equals(LocalValue<TValue> other)
        {
            if (Kind != other.Kind)
                return false;

            if (Kind is LocalValueKind.Top or LocalValueKind.Unknown)
                return true;
            if (Kind == LocalValueKind.Scalar)
                return EqualityComparer<TValue>.Default.Equals(ScalarValue, other.ScalarValue);
            if (Elements.Length != other.Elements.Length)
                return false;

            for (int i = 0; i < Elements.Length; i++)
            {
                if (!Elements[i].Equals(other.Elements[i]))
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj) => obj is LocalValue<TValue> other && Equals(other);

        public override int GetHashCode()
        {
            int hashCode = (int)Kind;
            if (Kind == LocalValueKind.Scalar)
            {
                hashCode = unchecked((hashCode * 31) + EqualityComparer<TValue>.Default.GetHashCode(ScalarValue));
            }
            else if (Kind == LocalValueKind.Tuple)
            {
                foreach (LocalValue<TValue> element in Elements)
                    hashCode = unchecked((hashCode * 31) + element.GetHashCode());
            }

            return hashCode;
        }

        public LocalValue<TValue> DeepCopy()
        {
            if (Kind == LocalValueKind.Top)
                return Top;

            if (Kind == LocalValueKind.Unknown)
                return Unknown;

            if (Kind == LocalValueKind.Scalar)
            {
                return new LocalValue<TValue>(
                    ScalarValue is IDeepCopyValue<TValue> copyValue ? copyValue.DeepCopy() : ScalarValue);
            }

            var elements = ImmutableArray.CreateBuilder<LocalValue<TValue>>(Elements.Length);
            foreach (LocalValue<TValue> element in Elements)
                elements.Add(element.DeepCopy());

            return new LocalValue<TValue>(elements.MoveToImmutable());
        }

        public TValue GetScalarValueOrTop(TValue topValue) =>
            Kind == LocalValueKind.Scalar ? ScalarValue : topValue;
    }

    public readonly struct LocalValueLattice<TValue, TValueLattice> : ILattice<LocalValue<TValue>>
        where TValue : struct, IEquatable<TValue>
        where TValueLattice : ILattice<TValue>
    {
        private readonly TValueLattice _valueLattice;

        public LocalValueLattice(TValueLattice valueLattice) => _valueLattice = valueLattice;

        public LocalValue<TValue> Top => LocalValue<TValue>.Top;

        public LocalValue<TValue> Meet(LocalValue<TValue> left, LocalValue<TValue> right)
        {
            if (left.Kind == LocalValueKind.Top)
                return right.DeepCopy();
            if (right.Kind == LocalValueKind.Top)
                return left.DeepCopy();

            if (left.Kind == LocalValueKind.Unknown || right.Kind == LocalValueKind.Unknown)
                return LocalValue<TValue>.Unknown;

            if (left.Kind == LocalValueKind.Scalar && right.Kind == LocalValueKind.Scalar)
                return new LocalValue<TValue>(_valueLattice.Meet(left.ScalarValue, right.ScalarValue));

            if (left.Kind != LocalValueKind.Tuple ||
                right.Kind != LocalValueKind.Tuple ||
                left.Elements.Length != right.Elements.Length)
            {
                return LocalValue<TValue>.Unknown;
            }

            var elements = ImmutableArray.CreateBuilder<LocalValue<TValue>>(left.Elements.Length);
            for (int i = 0; i < left.Elements.Length; i++)
                elements.Add(Meet(left.Elements[i], right.Elements[i]));

            return new LocalValue<TValue>(elements.MoveToImmutable());
        }
    }

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
        public DefaultValueDictionary<LocalKey, LocalValue<TValue>> Dictionary;

        // Stores any operations which are captured by reference in a FlowCaptureOperation.
        // Only stores captures which are assigned through. Captures of the values of operations
        // are tracked as part of the dictionary of values, keyed by LocalKey.
        public DefaultValueDictionary<CaptureId, ValueSet<CapturedReferenceValue>> CapturedReferences;

        // Stores target receiver and index values evaluated by deconstruction l-value captures.
        public DefaultValueDictionary<CapturedTargetKey, CapturedTargetValue<TValue>> CapturedTargetValues;

        public LocalState(
            DefaultValueDictionary<LocalKey, LocalValue<TValue>> dictionary,
            DefaultValueDictionary<CaptureId, ValueSet<CapturedReferenceValue>> capturedReferences,
            DefaultValueDictionary<CapturedTargetKey, CapturedTargetValue<TValue>> capturedTargetValues)
        {
            Dictionary = dictionary;
            CapturedReferences = capturedReferences;
            CapturedTargetValues = capturedTargetValues;
        }

        public LocalState(DefaultValueDictionary<LocalKey, LocalValue<TValue>> dictionary)
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

        public LocalValue<TValue> Get(LocalKey key) => Dictionary.Get(key);

        // Local dataflow states are mutable and should never be used as dictionary keys.
        public override int GetHashCode()
            => throw new NotImplementedException();

        public void Set(LocalKey key, LocalValue<TValue> value) => Dictionary.Set(key, value);

        public override string ToString() => Dictionary.ToString();
    }

    // Wrapper struct exists purely to substitute a concrete LocalKey for TKey of DictionaryLattice
    public readonly struct LocalStateLattice<TValue, TValueLattice> : ILattice<LocalState<TValue>>
        where TValue : struct, IEquatable<TValue>
        where TValueLattice : ILattice<TValue>
    {
        public readonly DictionaryLattice<LocalKey, LocalValue<TValue>, LocalValueLattice<TValue, TValueLattice>> Lattice;
        public readonly DictionaryLattice<CaptureId, ValueSet<CapturedReferenceValue>, ValueSetLattice<CapturedReferenceValue>> CapturedReferenceLattice;
        public readonly DictionaryLattice<CapturedTargetKey, CapturedTargetValue<TValue>, CapturedTargetValueLattice<TValue, TValueLattice>> CapturedTargetValueLattice;
        public readonly TValueLattice ValueLattice;

        public LocalStateLattice(TValueLattice valueLattice)
        {
            Lattice = new DictionaryLattice<LocalKey, LocalValue<TValue>, LocalValueLattice<TValue, TValueLattice>>(
                new LocalValueLattice<TValue, TValueLattice>(valueLattice));
            CapturedReferenceLattice = new DictionaryLattice<CaptureId, ValueSet<CapturedReferenceValue>, ValueSetLattice<CapturedReferenceValue>>(default(ValueSetLattice<CapturedReferenceValue>));
            CapturedTargetValueLattice = new DictionaryLattice<CapturedTargetKey, CapturedTargetValue<TValue>, CapturedTargetValueLattice<TValue, TValueLattice>>(
                new CapturedTargetValueLattice<TValue, TValueLattice>(valueLattice));
            ValueLattice = valueLattice;
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

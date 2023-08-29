// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// A union of Diagnostic and T, representing either a value of type T or a Diagnostic.
    /// Useful helper functions exist in <see cref="IncrementalValuesProviderExtensions"/> and <see cref="IncrementalGeneratorInitializationContextExtensions"/>.
    /// </summary>
    public abstract record DiagnosticOr<T>
    {
        public abstract bool HasValue { get; }

        public abstract bool HasDiagnostic { get; }

        /// <summary>
        /// Throws <see cref="InvalidOperationException"/> if IsValue is false
        /// </summary>
        public abstract T Value { get; }

        /// <summary>
        /// Throws <see cref="InvalidOperationException"/> if IsDiagnostic is false
        /// </summary>
        public abstract ImmutableArray<DiagnosticInfo> Diagnostics { get; }

        private sealed record Diag : DiagnosticOr<T>
        {
            private readonly SequenceEqualImmutableArray<DiagnosticInfo> _diagnostics;
            internal Diag(ImmutableArray<DiagnosticInfo> diagnostics) => _diagnostics = diagnostics.ToSequenceEqual();
            public override bool HasValue => false;
            public override bool HasDiagnostic => true;
            public override T Value => throw new InvalidOperationException();
            public override ImmutableArray<DiagnosticInfo> Diagnostics => _diagnostics.Array;
        }

        private sealed record Val : DiagnosticOr<T>
        {
            private readonly T _value;
            internal Val(T value) => _value = value;
            public override bool HasValue => true;
            public override bool HasDiagnostic => false;
            public override T Value => _value;
            public override ImmutableArray<DiagnosticInfo> Diagnostics => throw new InvalidOperationException();
        }

        private sealed record ValueAndDiagnostic : DiagnosticOr<T>
        {
            private readonly T _value;
            private readonly SequenceEqualImmutableArray<DiagnosticInfo> _diagnostics;
            internal ValueAndDiagnostic(T value, ImmutableArray<DiagnosticInfo> diagnostics) => (_value, _diagnostics) = (value, diagnostics.ToSequenceEqual());
            public override bool HasValue => true;
            public override bool HasDiagnostic => true;
            public override T Value => _value;
            public override ImmutableArray<DiagnosticInfo> Diagnostics => _diagnostics.Array;
        }

        /// <summary>
        /// Adds a diagnostic to the <see cref="DiagnosticOr{T}.Diagnostics"/> property
        /// </summary>
        public DiagnosticOr<T> AddDiagnostic(DiagnosticInfo diagnostic) => this switch
        {
            Diag d => new Diag(d.Diagnostics.Add(diagnostic)),
            Val v => new ValueAndDiagnostic(v.Value, ImmutableArray.Create(diagnostic)),
            ValueAndDiagnostic vad => new ValueAndDiagnostic(vad.Value, vad.Diagnostics.Add(diagnostic)),
            _ => throw new UnreachableException()
        };

        /// <summary>
        /// Creates a new <see cref="DiagnosticOr{T}"/> with the <see cref="DiagnosticOr{T}.Value"/> set to <paramref name="value"/>
        /// </summary>
        public DiagnosticOr<T> WithValue(T value) => this switch
        {
            Diag d => new ValueAndDiagnostic(value, d.Diagnostics),
            Val => new Val(value),
            ValueAndDiagnostic vad => new ValueAndDiagnostic(value, vad.Diagnostics),
            _ => throw new UnreachableException()
        };

        /// <summary>
        /// Create a Diagnostic variant
        /// </summary>
        public static DiagnosticOr<T> From(DiagnosticInfo diagnostic)
        {
            return new Diag(ImmutableArray.Create(diagnostic));
        }

        /// <summary>
        /// Create a Value variant
        /// </summary>
        public static DiagnosticOr<T> From(T value)
        {
            Debug.Assert(value is not null);
            return new Val(value);
        }

        /// <summary>
        /// Create a ValueAndDiagnostic variant
        /// </summary>
        public static DiagnosticOr<T> From(T value, params DiagnosticInfo[] diagnostics)
        {
            Debug.Assert(value is not null);
            Debug.Assert(diagnostics is not null);
            return new ValueAndDiagnostic(value, ImmutableArray.Create(diagnostics));
        }
    }

    public static class DiagnosticOrTHelperExtensions
    {
        /// <summary>
        /// Splits the elements of <paramref name="provider"/> into a values provider and a diagnostics provider.
        /// </summary>
        public static (IncrementalValuesProvider<T>, IncrementalValuesProvider<DiagnosticInfo>) Split<T>(this IncrementalValuesProvider<DiagnosticOr<T>> provider)
        {
            var values = provider.Where(x => x.HasValue).Select(static (x, ct) => x.Value);
            var diagnostics = provider.Where(x => x.HasDiagnostic).SelectMany(static (x, ct) => x.Diagnostics);
            return (values, diagnostics);
        }

        /// <summary>
        /// Splits the inner arrays of <paramref name="provider"/> into values and diagnostics.
        /// </summary>
        public static (IncrementalValuesProvider<SequenceEqualImmutableArray<T>>, IncrementalValuesProvider<DiagnosticInfo>) SplitArrays<T>(this IncrementalValuesProvider<SequenceEqualImmutableArray<DiagnosticOr<T>>> provider)
        {
            var values = provider.Select((arr, ct) => arr.Where(x => x.HasValue).Select((x, ct) => x.Value).ToSequenceEqualImmutableArray());
            var diagnostics = provider.SelectMany((arr, ct) => arr.Where(x => x.HasDiagnostic).SelectMany((x, ct) => x.Diagnostics));
            return (values, diagnostics);
        }

        /// <summary>
        /// Splits the elements of <paramref name="provider"/> into groups depending on whether Item1 is a value or a Diagnostic.
        /// </summary>
        public static (IncrementalValuesProvider<(T, T2)>, IncrementalValuesProvider<DiagnosticInfo>) Split<T, T2>(this IncrementalValuesProvider<(DiagnosticOr<T>, T2)> provider)
        {
            var values = provider.Where(x => x.Item1.HasValue).Select(static (x, ct) => (x.Item1.Value, x.Item2));
            var diagnostics = provider.Where(x => x.Item1.HasDiagnostic).SelectMany(static (x, ct) => x.Item1.Diagnostics);
            return (values, diagnostics);
        }

        /// <summary>
        /// Filters the <see cref="IncrementalValuesProvider{TValue}"/> by whether or not the is a <see cref="Diagnostic"/>, reports the diagnostics, and returns the values.
        /// </summary>
        public static IncrementalValuesProvider<T> FilterAndReportDiagnostics<T>(this IncrementalGeneratorInitializationContext ctx, IncrementalValuesProvider<DiagnosticOr<T>> diagnosticOrValues)
        {
            var (values, diagnostics) = diagnosticOrValues.Split();
            ctx.RegisterDiagnostics(diagnostics);
            return values;
        }

        /// <summary>
        /// Filters both <see cref="IncrementalValuesProvider{TValue}"/> by whether or not the value in <paramref name="diagnosticOrValues"/> is a <see cref="Diagnostic"/>, reports the diagnostics, and returns the values.
        /// </summary>
        public static (IncrementalValuesProvider<T>, IncrementalValuesProvider<T2>) FilterAndReportDiagnostics<T, T2>(
            this IncrementalGeneratorInitializationContext ctx,
            IncrementalValuesProvider<DiagnosticOr<T>> diagnosticOrValues,
            IncrementalValuesProvider<T2> associatedValues)
        {
            var (values, diagnostics) = diagnosticOrValues.Zip(associatedValues).Split();
            ctx.RegisterDiagnostics(diagnostics);
            return values.Split();
        }

        /// <summary>
        /// Filters each inner <see cref="IEnumerable{T}"/> of <see cref="DiagnosticOr{T}"/> by whether the elements are <see cref="Diagnostic"/>s, reports the diagnostics, and returns the values.
        /// </summary>
        public static IncrementalValuesProvider<SequenceEqualImmutableArray<T>> FilterAndReportDiagnostics<T>(
            this IncrementalGeneratorInitializationContext ctx,
            IncrementalValuesProvider<SequenceEqualImmutableArray<DiagnosticOr<T>>> diagnosticOrValues)
        {
            var (values, diagnostics) = diagnosticOrValues.SplitArrays<T>();
            ctx.RegisterDiagnostics(diagnostics);
            return values;
        }
    }
}

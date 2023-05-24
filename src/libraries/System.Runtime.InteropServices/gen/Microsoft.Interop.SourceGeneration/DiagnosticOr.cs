// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    /// <summary>
    /// A union of Diagnostic and T, representing either a value of type T or a Diagnostic.
    /// Useful helper functions exist in <see cref="IncrementalValuesProviderExtensions"/> and <see cref="IncrementalGeneratorInitializationContextExtensions"/>.
    /// </summary>
    public abstract record DiagnosticOr<T>
    {
        public abstract bool IsValue { get; }

        public bool IsDiagnostic => !IsValue;

        /// <summary>
        /// Throws <see cref="InvalidOperationException"/> if IsValue is false
        /// </summary>
        public abstract T Value { get; }

        /// <summary>
        /// Throws <see cref="InvalidOperationException"/> if IsDiagnostic is false
        /// </summary>
        public abstract Diagnostic Diagnostic { get; }

        private sealed record Diag : DiagnosticOr<T>
        {
            private readonly Diagnostic _diagnostic;
            internal Diag(Diagnostic diagnostic) => _diagnostic = diagnostic;
            public override bool IsValue => false;
            public override T Value => throw new InvalidOperationException();
            public override Diagnostic Diagnostic => _diagnostic;
        }

        private sealed record Val : DiagnosticOr<T>
        {
            private readonly T _value;
            internal Val(T value) => _value = value;
            public override bool IsValue => true;
            public override T Value => _value;
            public override Diagnostic Diagnostic => throw new InvalidOperationException();
        }

        /// <summary>
        /// Create a Diagnostic variant
        /// </summary>
        public static DiagnosticOr<T> From(Diagnostic diagnostic)
        {
            Debug.Assert(diagnostic is not null);
            return new Diag(diagnostic);
        }

        /// <summary>
        /// Create a Value variant
        /// </summary>
        public static DiagnosticOr<T> From(T value)
        {
            Debug.Assert(value is not null);
            return new Val(value);
        }
    }

    public static class DiagnosticOrTHelperExtensions
    {
        /// <summary>
        /// Splits the elements of <paramref name="provider"/> into a "values" provider and a "diagnositics" provider.
        /// </summary>
        public static (IncrementalValuesProvider<T>, IncrementalValuesProvider<Diagnostic>) Split<T>(this IncrementalValuesProvider<DiagnosticOr<T>> provider)
        {
            var values = provider.Where(x => x.IsValue).Select(static (x, ct) => x.Value);
            var diagnostics = provider.Where(x => x.IsDiagnostic).Select(static (x, ct) => x.Diagnostic);
            return (values, diagnostics);
        }

        /// <summary>
        /// Splits the inner arrays of <paramref name="provider"/> into values and diagnostics.
        /// </summary>
        public static (IncrementalValuesProvider<SequenceEqualImmutableArray<T>>, IncrementalValuesProvider<Diagnostic>) SplitArrays<T>(this IncrementalValuesProvider<SequenceEqualImmutableArray<DiagnosticOr<T>>> provider)
        {
            var values = provider.Select((arr, ct) => arr.Where(x => x.IsValue).Select((x, ct) => x.Value).ToSequenceEqualImmutableArray());
            var diagnostics = provider.SelectMany((arr, ct) => arr.Where(x => x.IsDiagnostic).Select((x, ct) => x.Diagnostic));
            return (values, diagnostics);
        }

        /// <summary>
        /// Splits the elements of <paramref name="provider"/> into groups depending on whether Item1 is a value or a Diagnostic.
        /// </summary>
        public static (IncrementalValuesProvider<(T, T2)>, IncrementalValuesProvider<Diagnostic>) Split<T, T2>(this IncrementalValuesProvider<(DiagnosticOr<T>, T2)> provider)
        {
            var values = provider.Where(x => x.Item1.IsValue).Select(static (x, ct) => (x.Item1.Value, x.Item2));
            var diagnostics = provider.Where(x => !x.Item1.IsValue).Select(static (x, ct) => x.Item1.Diagnostic);
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

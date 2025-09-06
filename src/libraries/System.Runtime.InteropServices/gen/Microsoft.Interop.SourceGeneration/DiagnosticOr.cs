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
        /// Filters the <see cref="IncrementalValuesProvider{TValue}"/> by whether or not the is a <see cref="Diagnostic"/>, reports the diagnostics, and returns the values.
        /// </summary>
        public static IncrementalValuesProvider<T> FilterAndReportDiagnostics<T>(this IncrementalGeneratorInitializationContext ctx, IncrementalValuesProvider<DiagnosticOr<T>> diagnosticOrValues)
        {
            var (values, diagnostics) = diagnosticOrValues.Split();
            ctx.RegisterDiagnostics(diagnostics);
            return values;
        }
    }
}

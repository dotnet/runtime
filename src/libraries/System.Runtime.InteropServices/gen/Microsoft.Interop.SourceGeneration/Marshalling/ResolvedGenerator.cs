// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Interop
{
    public record struct ResolvedGenerator([property: MemberNotNullWhen(true, nameof(ResolvedGenerator.IsResolved), nameof(ResolvedGenerator.IsResolvedWithoutErrors))] IMarshallingGenerator? Generator, ImmutableArray<GeneratorDiagnostic> Diagnostics)
    {
        private static readonly Forwarder s_forwarder = new();

        private bool? _resolvedWithoutErrors;

        public bool IsResolvedWithoutErrors => _resolvedWithoutErrors ??= IsResolved && Diagnostics.All(d => !d.IsFatal);

        public readonly bool IsResolved => Generator is not null;

        public static ResolvedGenerator Resolved(IMarshallingGenerator generator)
        {
            return new(generator, ImmutableArray<GeneratorDiagnostic>.Empty);
        }

        public static ResolvedGenerator NotSupported(GeneratorDiagnostic.NotSupported notSupportedDiagnostic)
        {
            return new(s_forwarder, ImmutableArray.Create<GeneratorDiagnostic>(notSupportedDiagnostic));
        }

        public static ResolvedGenerator ResolvedWithDiagnostics(IMarshallingGenerator generator, ImmutableArray<GeneratorDiagnostic> diagnostics)
        {
            return new(generator, diagnostics);
        }

        public static ResolvedGenerator UnresolvedGenerator { get; } = new(null, ImmutableArray<GeneratorDiagnostic>.Empty);
    }
}

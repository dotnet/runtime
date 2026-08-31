// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Interop
{
    public record struct ResolvedGenerator([property: MemberNotNullWhen(true, nameof(ResolvedGenerator.IsResolved), nameof(ResolvedGenerator.IsResolvedWithoutErrors))] IBoundMarshallingGenerator? Generator, ImmutableArray<GeneratorDiagnostic> Diagnostics)
    {
        private static readonly Forwarder s_forwarder = new();

        private bool? _resolvedWithoutErrors;

        public bool IsResolvedWithoutErrors => _resolvedWithoutErrors ??= IsResolved && Diagnostics.All(d => !d.IsFatal);

        public readonly bool IsResolved => Generator is not null;

        public static ResolvedGenerator Resolved(IBoundMarshallingGenerator generator)
        {
            return new(generator, ImmutableArray<GeneratorDiagnostic>.Empty);
        }

        public static ResolvedGenerator NotSupported(TypePositionInfo info, StubCodeContext context, GeneratorDiagnostic.NotSupported notSupportedDiagnostic)
        {
            return new(s_forwarder.Bind(info, context), ImmutableArray.Create<GeneratorDiagnostic>(notSupportedDiagnostic));
        }

        public static ResolvedGenerator ResolvedWithDiagnostics(IBoundMarshallingGenerator generator, ImmutableArray<GeneratorDiagnostic> diagnostics)
        {
            return new(generator, diagnostics);
        }

        public static ResolvedGenerator UnresolvedGenerator { get; } = new(null, ImmutableArray<GeneratorDiagnostic>.Empty);
    }
}

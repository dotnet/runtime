// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.Interop
{
    public record struct ResolvedGenerator(IMarshallingGenerator Generator, ImmutableArray<GeneratorDiagnostic> Diagnostics)
    {
        private static readonly Forwarder s_forwarder = new();

        private bool? _resolvedSuccessfully;

        public bool ResolvedSuccessfully => _resolvedSuccessfully ??= Diagnostics.All(d => !d.IsFatal);

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
    }
}

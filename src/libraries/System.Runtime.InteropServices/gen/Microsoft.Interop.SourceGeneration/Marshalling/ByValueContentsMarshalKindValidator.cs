// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Interop
{
    /// <summary>
    /// An <see cref="IMarshallingGeneratorResolver"/> implementation that wraps an inner <see cref="IMarshallingGeneratorResolver"/> instance and validates that the <see cref="TypePositionInfo.ByValueContentsMarshalKind"/> on the provided <see cref="TypePositionInfo"/> is valid in the current marshalling scenario.
    /// </summary>
    public sealed class ByValueContentsMarshalKindValidator : IMarshallingGeneratorResolver
    {
        private static readonly Forwarder s_forwarder = new();

        private readonly IMarshallingGeneratorResolver _inner;

        public ByValueContentsMarshalKindValidator(IMarshallingGeneratorResolver inner)
        {
            _inner = inner;
        }

        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            ResolvedGenerator generator = _inner.Create(info, context);
            return generator.IsResolvedWithoutErrors ? ValidateByValueMarshalKind(info, context, generator) : generator;
        }

        private static ResolvedGenerator ValidateByValueMarshalKind(TypePositionInfo info, StubCodeContext context, ResolvedGenerator generator)
        {
            if (generator.Generator is Forwarder)
            {
                // Forwarder allows everything since it just forwards to a P/Invoke.
                // The Default marshal kind is always valid.
                return generator;
            }

            var support = generator.Generator.SupportsByValueMarshalKind(info.ByValueContentsMarshalKind, info, context, out GeneratorDiagnostic? diagnostic);
            Debug.Assert(support == ByValueMarshalKindSupport.Supported || diagnostic is not null);
            return support switch
            {
                ByValueMarshalKindSupport.Supported => generator,
                ByValueMarshalKindSupport.NotSupported => ResolvedGenerator.ResolvedWithDiagnostics(s_forwarder, generator.Diagnostics.Add(diagnostic!)),
                ByValueMarshalKindSupport.Unnecessary => generator with { Diagnostics = generator.Diagnostics.Add(diagnostic!) },
                ByValueMarshalKindSupport.NotRecommended => generator with { Diagnostics = generator.Diagnostics.Add(diagnostic!) },
                _ => throw new UnreachableException()
            };
        }
    }
}

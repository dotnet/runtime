// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Interop
{
    /// <summary>
    /// An <see cref="IMarshallingGeneratorFactory"/> implementation that wraps an inner <see cref="IMarshallingGeneratorFactory"/> instance and validates that the <see cref="TypePositionInfo.ByValueContentsMarshalKind"/> on the provided <see cref="TypePositionInfo"/> is valid in the current marshalling scenario.
    /// </summary>
    public class ByValueContentsMarshalKindValidator : IMarshallingGeneratorFactory
    {
        private readonly IMarshallingGeneratorFactory _inner;

        public ByValueContentsMarshalKindValidator(IMarshallingGeneratorFactory inner)
        {
            _inner = inner;
        }

        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            ResolvedGenerator generator = _inner.Create(info, context);
            return generator.ResolvedSuccessfully ? ValidateByValueMarshalKind(info, context, generator) : generator;
        }

        private static ResolvedGenerator ValidateByValueMarshalKind(TypePositionInfo info, StubCodeContext context, ResolvedGenerator generator)
        {
            if (generator.Generator is Forwarder)
            {
                // Forwarder allows everything since it just forwards to a P/Invoke.
                return generator;
            }

            if (info.IsByRef && info.ByValueContentsMarshalKind != ByValueContentsMarshalKind.Default)
            {
                return generator with
                {
                    Diagnostics = generator.Diagnostics.Add(new GeneratorDiagnostic.NotSupported(info, context)
                    {
                        NotSupportedDetails = SR.InOutAttributeByRefNotSupported
                    })
                };
            }
            else if (info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.In)
            {
                return generator with
                {
                    Diagnostics = generator.Diagnostics.Add(new GeneratorDiagnostic.NotSupported(info, context)
                    {
                        NotSupportedDetails = SR.InAttributeNotSupportedWithoutOut
                    })
                };
            }
            else if (info.ByValueContentsMarshalKind != ByValueContentsMarshalKind.Default
                && !generator.Generator.SupportsByValueMarshalKind(info.ByValueContentsMarshalKind, context))
            {
                return generator with
                {
                    Diagnostics = generator.Diagnostics.Add(new GeneratorDiagnostic.NotSupported(info, context)
                    {
                        NotSupportedDetails = SR.InOutAttributeMarshalerNotSupported
                    })
                };
            }
            return generator;
        }
    }
}

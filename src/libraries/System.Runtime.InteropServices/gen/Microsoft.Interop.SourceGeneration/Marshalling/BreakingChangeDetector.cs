// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// An <see cref="IMarshallingGeneratorResolver"/> that adds diagnostics to warn users about breaking changes in the interop generators,
    /// whether from built-in to source-generated interop or between versions of interop source-generation.
    /// </summary>
    public sealed class BreakingChangeDetector(IMarshallingGeneratorResolver inner) : IMarshallingGeneratorResolver
    {
        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            ResolvedGenerator gen = inner.Create(info, context);
            if (!gen.IsResolvedWithoutErrors)
            {
                return gen;
            }

            // Breaking change: [MarshalAs(UnmanagedType.Struct)] in object in unmanaged-to-managed scenarios will not respect VT_BYREF.
            if (info is { RefKind: RefKind.In, MarshallingAttributeInfo: NativeMarshallingAttributeInfo(ManagedTypeInfo(_, TypeNames.ComVariantMarshaller), _) }
                && context.Direction == MarshalDirection.UnmanagedToManaged)
            {
                gen = ResolvedGenerator.ResolvedWithDiagnostics(
                    gen.Generator,
                    gen.Diagnostics.Add(
                        new GeneratorDiagnostic.NotRecommended(info, context)
                        {
                            Details = SR.InVariantShouldBeRef
                        }));
            }

            return gen;
        }
    }
}

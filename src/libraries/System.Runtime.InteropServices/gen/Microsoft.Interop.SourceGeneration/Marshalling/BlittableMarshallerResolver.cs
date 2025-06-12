// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.Interop
{
    /// <summary>
    /// Resolves a <see cref="TypePositionInfo"/> with <see cref="UnmanagedBlittableMarshallingInfo"/> based on the target compilation's
    /// runtime marshalling support and the type's blittability.
    /// </summary>
    /// <param name="runtimeMarshallingDisabled">Whether or not the target compilation has the 'DisableRuntimeMarshallingAttribute' applied.</param>
    public sealed class BlittableMarshallerResolver(bool runtimeMarshallingDisabled) : IMarshallingGeneratorResolver
    {
        private static readonly ImmutableDictionary<string, string> AddDisableRuntimeMarshallingAttributeProperties =
            ImmutableDictionary<string, string>.Empty.Add(GeneratorDiagnosticProperties.AddDisableRuntimeMarshallingAttribute, GeneratorDiagnosticProperties.AddDisableRuntimeMarshallingAttribute);

        private static readonly BlittableMarshaller s_blittable = new BlittableMarshaller();

        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            if (info.MarshallingAttributeInfo is UnmanagedBlittableMarshallingInfo blittableInfo)
            {
                if (runtimeMarshallingDisabled || blittableInfo.IsStrictlyBlittable)
                {
                    return ResolvedGenerator.Resolved(s_blittable.Bind(info, context));
                }

                return ResolvedGenerator.NotSupported(
                    info,
                    context, new GeneratorDiagnostic.NotSupported(info)
                    {
                        NotSupportedDetails = SR.RuntimeMarshallingMustBeDisabled,
                        DiagnosticProperties = AddDisableRuntimeMarshallingAttributeProperties
                    });
            }

            return ResolvedGenerator.UnresolvedGenerator;
        }
    }
}

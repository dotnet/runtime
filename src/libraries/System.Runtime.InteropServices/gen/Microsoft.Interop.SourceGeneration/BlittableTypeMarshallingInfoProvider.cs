// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// Marshalling information provider for unmanaged types that may be blittable.
    /// </summary>
    public sealed class BlittableTypeMarshallingInfoProvider : ITypeBasedMarshallingInfoProvider
    {
        private readonly Compilation _compilation;

        public BlittableTypeMarshallingInfoProvider(Compilation compilation)
        {
            _compilation = compilation;
        }

        public bool CanProvideMarshallingInfoForType(ITypeSymbol type) => type is INamedTypeSymbol { IsUnmanagedType: true } unmanagedType
                && unmanagedType.IsConsideredBlittable();
        public MarshallingInfo GetMarshallingInfo(ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            if (type.TypeKind is TypeKind.Enum or TypeKind.Pointer or TypeKind.FunctionPointer
                || type.SpecialType.IsAlwaysBlittable())
            {
                // Treat primitive types and enums as having no marshalling info.
                // They are supported in configurations where runtime marshalling is enabled.
                return NoMarshallingInfo.Instance;
            }
            else if (_compilation.GetTypeByMetadataName(TypeNames.System_Runtime_CompilerServices_DisableRuntimeMarshallingAttribute) is null)
            {
                // If runtime marshalling cannot be disabled, then treat this as a "missing support" scenario so we can gracefully fall back to using the forwarder downlevel.
                return new MissingSupportMarshallingInfo();
            }
            else
            {
                return new UnmanagedBlittableMarshallingInfo(type.IsStrictlyBlittableInContext(_compilation));
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// The type of the element is a SafeHandle-derived type with no marshalling attributes.
    /// </summary>
    public sealed record SafeHandleMarshallingInfo(bool AccessibleDefaultConstructor, bool IsAbstract) : MarshallingInfo;

    /// <summary>
    /// This class supports generating marshalling info for SafeHandle-derived types.
    /// </summary>
    public sealed class SafeHandleMarshallingInfoProvider : ITypeBasedMarshallingInfoProvider
    {
        private readonly Compilation _compilation;
        private readonly ITypeSymbol _containingScope;

        public SafeHandleMarshallingInfoProvider(Compilation compilation, ITypeSymbol containingScope)
        {
            _compilation = compilation;
            _containingScope = containingScope;
        }

        public bool CanProvideMarshallingInfoForType(ITypeSymbol type)
        {
            // Check for an implicit SafeHandle conversion.
            // The SafeHandle type might not be defined if we're using one of the test CoreLib implementations used for NativeAOT.
            ITypeSymbol? safeHandleType = _compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_SafeHandle);
            if (safeHandleType is not null)
            {
                CodeAnalysis.Operations.CommonConversion conversion = _compilation.ClassifyCommonConversion(type, safeHandleType);
                if (conversion.Exists
                    && conversion.IsImplicit
                    && (conversion.IsReference || conversion.IsIdentity))
                {
                    return true;
                }
            }
            return false;
        }

        public MarshallingInfo GetMarshallingInfo(ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            bool hasAccessibleDefaultConstructor = false;
            if (type is INamedTypeSymbol named && !named.IsAbstract && named.InstanceConstructors.Length > 0)
            {
                foreach (IMethodSymbol ctor in named.InstanceConstructors)
                {
                    if (ctor.Parameters.Length == 0)
                    {
                        hasAccessibleDefaultConstructor = _compilation.IsSymbolAccessibleWithin(ctor, _containingScope);
                        break;
                    }
                }
            }
            return new SafeHandleMarshallingInfo(hasAccessibleDefaultConstructor, type.IsAbstract);
        }
    }
}

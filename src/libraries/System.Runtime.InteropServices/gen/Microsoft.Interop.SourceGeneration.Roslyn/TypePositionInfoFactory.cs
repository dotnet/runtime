// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop;

/// <summary>Converts signature symbols into positional marshalling information.</summary>
public static class TypePositionInfoFactory
{
    extension(TypePositionInfo)
    {
        /// <summary>Creates positional information for a managed parameter.</summary>
        /// <param name="paramSymbol">The parameter symbol.</param>
        /// <param name="marshallingInfo">The parsed marshalling information.</param>
        /// <param name="compilation">The compilation containing the parameter.</param>
        /// <returns>The positional information, without assigned managed or native indexes.</returns>
        public static TypePositionInfo CreateForParameter(IParameterSymbol paramSymbol, MarshallingInfo marshallingInfo, Compilation compilation)
        {
            return new TypePositionInfo(ManagedTypeInfo.CreateTypeInfoForTypeSymbol(paramSymbol.Type), marshallingInfo)
            {
                InstanceIdentifier = CodeWriterHelpers.EscapeIdentifier(paramSymbol.Name),
                RefKind = paramSymbol.RefKind.ToInteropRefKind(),
                ByValueContentsMarshalKind = GetByValueContentsMarshalKind(paramSymbol.GetAttributes(), compilation),
                ScopedKind = paramSymbol.ScopedKind.ToInteropScopedKind(),
                IsExplicitThis = ((ParameterSyntax?)paramSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax())?.Modifiers.Any(SyntaxKind.ThisKeyword) ?? false
            };
        }

        /// <summary>Gets the diagnostic location for a signature element.</summary>
        /// <param name="info">The positional information.</param>
        /// <param name="methodSymbol">The containing method.</param>
        /// <returns>The corresponding diagnostic location.</returns>
        public static Location GetLocation(TypePositionInfo info, IMethodSymbol methodSymbol)
        {
            if (info.ManagedIndex is TypePositionInfo.UnsetIndex)
                return Location.None;

            if (info.ManagedIndex is TypePositionInfo.ReturnIndex or TypePositionInfo.ErrorIndex)
                return methodSymbol.Locations[0];

            return methodSymbol.Parameters[info.ManagedIndex].Locations[0];
        }
    }

    private static ByValueContentsMarshalKind GetByValueContentsMarshalKind(IEnumerable<AttributeData> attributes, Compilation compilation)
    {
        INamedTypeSymbol outAttributeType = compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_OutAttribute)!;
        INamedTypeSymbol inAttributeType = compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_InAttribute)!;

        ByValueContentsMarshalKind marshalKind = ByValueContentsMarshalKind.Default;

        foreach (AttributeData attr in attributes)
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, outAttributeType))
            {
                marshalKind |= ByValueContentsMarshalKind.Out;
            }
            else if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, inAttributeType))
            {
                marshalKind |= ByValueContentsMarshalKind.In;
            }
        }

        return marshalKind;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    internal static class CustomMarshallingInfoHelper
    {
        public static MarshallingInfo CreateNativeMarshallingInfo(
            ITypeSymbol type,
            INamedTypeSymbol entryPointType,
            AttributeData attrData,
            UseSiteAttributeProvider useSiteAttributeProvider,
            GetMarshallingInfoCallback getMarshallingInfoCallback,
            int indirectionDepth,
            CountInfo parsedCountInfo,
            IGeneratorDiagnostics diagnostics,
            Compilation compilation)
        {
            if (!ManualTypeMarshallingHelper.HasEntryPointMarshallerAttribute(entryPointType))
            {
                return NoMarshallingInfo.Instance;
            }

            if (!(entryPointType.IsStatic && entryPointType.TypeKind == TypeKind.Class)
                && entryPointType.TypeKind != TypeKind.Struct)
            {
                diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(SR.MarshallerTypeMustBeStaticClassOrStruct), entryPointType.ToDisplayString(), type.ToDisplayString());
                return NoMarshallingInfo.Instance;
            }

            ManagedTypeInfo entryPointTypeInfo = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(entryPointType);

            bool isLinearCollectionMarshalling = ManualTypeMarshallingHelper.IsLinearCollectionEntryPoint(entryPointType);
            if (isLinearCollectionMarshalling)
            {
                // Update the entry point type with the type arguments based on the managed type
                if (type is IArrayTypeSymbol arrayManagedType)
                {
                    // Generally, we require linear collection marshallers to have "arity of managed type + 1" arity.
                    // However, arrays aren't "generic" over their element type as they're generics, but we want to treat the element type
                    // as a generic type parameter. As a result, we require an arity of 2 for array marshallers, 1 for the array element type,
                    // and 1 for the native element type (the required additional type parameter for linear collection marshallers).
                    if (entryPointType.Arity != 2)
                    {
                        diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(SR.MarshallerEntryPointTypeMustMatchArity), entryPointType.ToDisplayString(), type.ToDisplayString());
                        return NoMarshallingInfo.Instance;
                    }

                    entryPointType = entryPointType.ConstructedFrom.Construct(
                        arrayManagedType.ElementType,
                        entryPointType.TypeArguments.Last());
                }
                else if (type is INamedTypeSymbol namedManagedCollectionType && entryPointType.IsUnboundGenericType)
                {
                    if (!ManualTypeMarshallingHelper.TryResolveEntryPointType(
                        namedManagedCollectionType,
                        entryPointType,
                        isLinearCollectionMarshalling,
                        (type, entryPointType) => diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(SR.MarshallerEntryPointTypeMustMatchArity), entryPointType.ToDisplayString(), type.ToDisplayString()),
                        out ITypeSymbol resolvedEntryPointType))
                    {
                        return NoMarshallingInfo.Instance;
                    }

                    entryPointType = (INamedTypeSymbol)resolvedEntryPointType;
                }
                else
                {
                    diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(SR.MarshallerEntryPointTypeMustMatchArity), entryPointType.ToDisplayString(), type.ToDisplayString());
                    return NoMarshallingInfo.Instance;
                }

                Func<ITypeSymbol, MarshallingInfo> getMarshallingInfoForElement = (ITypeSymbol elementType) => getMarshallingInfoCallback(elementType, useSiteAttributeProvider, indirectionDepth + 1);
                if (ManualTypeMarshallingHelper.TryGetLinearCollectionMarshallersFromEntryType(entryPointType, type, compilation, getMarshallingInfoForElement, out CustomTypeMarshallers? collectionMarshallers))
                {
                    return new NativeLinearCollectionMarshallingInfo(
                        entryPointTypeInfo,
                        collectionMarshallers.Value,
                        parsedCountInfo,
                        ManagedTypeInfo.CreateTypeInfoForTypeSymbol(entryPointType.TypeParameters.Last()));
                }
                return NoMarshallingInfo.Instance;
            }

            if (type is INamedTypeSymbol namedManagedType && entryPointType.IsUnboundGenericType)
            {
                if (!ManualTypeMarshallingHelper.TryResolveEntryPointType(
                    namedManagedType,
                    entryPointType,
                    isLinearCollectionMarshalling,
                    (type, entryPointType) => diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(SR.MarshallerEntryPointTypeMustMatchArity), entryPointType.ToDisplayString(), type.ToDisplayString()),
                    out ITypeSymbol resolvedEntryPointType))
                {
                    return NoMarshallingInfo.Instance;
                }

                entryPointType = (INamedTypeSymbol)resolvedEntryPointType;
            }

            if (ManualTypeMarshallingHelper.TryGetValueMarshallersFromEntryType(entryPointType, type, compilation, out CustomTypeMarshallers? marshallers))
            {
                return new NativeMarshallingAttributeInfo(entryPointTypeInfo, marshallers.Value);
            }
            return NoMarshallingInfo.Instance;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public static class CustomMarshallingInfoHelper
    {
        public static MarshallingInfo CreateNativeMarshallingInfo(
            ITypeSymbol type,
            INamedTypeSymbol entryPointType,
            AttributeData attrData,
            UseSiteAttributeProvider useSiteAttributeProvider,
            GetMarshallingInfoCallback getMarshallingInfoCallback,
            bool isMarshalUsingAttribute,
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
                    if (entryPointType.Arity != 2)
                    {
                        diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(SR.MarshallerEntryPointTypeMustMatchArity), entryPointType.ToDisplayString(), type.ToDisplayString());
                        return NoMarshallingInfo.Instance;
                    }

                    entryPointType = entryPointType.ConstructedFrom.Construct(
                        arrayManagedType.ElementType,
                        entryPointType.TypeArguments.Last());
                }
                else if (type is INamedTypeSymbol namedManagedType)
                {
                    // Entry point type for linear collection marshalling must have the arity of the managed type + 1
                    // for the element unmanaged type placeholder
                    if (entryPointType.Arity != namedManagedType.Arity + 1)
                    {
                        diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(SR.MarshallerEntryPointTypeMustMatchArity), entryPointType.ToDisplayString(), type.ToDisplayString());
                        return NoMarshallingInfo.Instance;
                    }

                    entryPointType = entryPointType.ConstructedFrom.Construct(
                        namedManagedType.TypeArguments.Add(entryPointType.TypeArguments.Last()).ToArray());
                }
                else
                {
                    diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(SR.MarshallerEntryPointTypeMustMatchArity), entryPointType.ToDisplayString(), type.ToDisplayString());
                    return NoMarshallingInfo.Instance;
                }

                Func<ITypeSymbol, MarshallingInfo> getMarshallingInfoForElement = (ITypeSymbol elementType) => getMarshallingInfoCallback(elementType, useSiteAttributeProvider, indirectionDepth + 1);
                if (ManualTypeMarshallingHelper.TryGetLinearCollectionMarshallersFromEntryType(entryPointType, type, compilation, getMarshallingInfoForElement, out CustomTypeMarshallers? marshallers))
                {
                    return new NativeLinearCollectionMarshallingInfo(
                        entryPointTypeInfo,
                        marshallers.Value,
                        parsedCountInfo,
                        ManagedTypeInfo.CreateTypeInfoForTypeSymbol(entryPointType.TypeParameters.Last()));
                }
            }
            else if (ManualTypeMarshallingHelper.TryGetValueMarshallersFromEntryType(entryPointType, type, compilation, out CustomTypeMarshallers? marshallers))
            {
                return new NativeMarshallingAttributeInfo(entryPointTypeInfo, marshallers.Value);
            }
            return NoMarshallingInfo.Instance;
        }
    }
}

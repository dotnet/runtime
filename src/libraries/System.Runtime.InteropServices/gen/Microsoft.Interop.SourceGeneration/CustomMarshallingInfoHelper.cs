// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Microsoft.Interop
{
    public static class CustomMarshallingInfoHelper
    {
        internal static MarshallingInfo CreateNativeMarshallingInfo(
            ITypeSymbol type,
            INamedTypeSymbol entryPointType,
            AttributeData attrData,
            UseSiteAttributeProvider useSiteAttributeProvider,
            GetMarshallingInfoCallback getMarshallingInfoCallback,
            int indirectionDepth,
            CountInfo parsedCountInfo,
            GeneratorDiagnosticsBag diagnostics,
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

        /// <summary>
        /// Creates a <see cref="MarshallingInfo"/> for the given managed type and marshaller type in the given compilation.
        /// This marshalling info is independent of any specific marshalling context or signature element.
        /// </summary>
        /// <param name="type">The managed type of the element for which to generate marshalling info.</param>
        /// <param name="entryPointType">The type of the marshaller entry point.</param>
        /// <param name="attrData">The attribute data for attribute that provided the marshalling info (used for reporting diagnostics).</param>
        /// <param name="compilation">The compilation in which the marshalling info is being generated.</param>
        /// <param name="diagnostics">The diagnostics sink to report diagnostics to.</param>
        /// <returns>The marshalling info for the given managed type and marshaller entrypoint type, or <see cref="NoMarshallingInfo.Instance" /> if the marshaller requires use-site information.</returns>
        /// <remarks>
        /// This method cannot generate marshalling info for any marshallers that require use-site information like count information or marshalling
        /// information for an element-indirection-level greater than 0.
        /// </remarks>
        /// <example>
        /// This method can be used to generate marshalling info for exceptions. Exception marshalling does not have any use-site information and
        /// is not in the signature. As a result, this method can be used to generate the marshalling info for an exception marshaller.
        /// <code>
        /// var exceptionMarshallingInfo = CustomMarshallingInfoForNonSignatureElement.Create(compilation.GetTypeByMetadataName(TypeNames.System_Exception), compilation.GetTypeByMetadataName(TypeNames.DefaultExceptionMarshaller), triggeringAttribute, compilation, diagnostics);
        /// </code>
        /// </example>
        public static MarshallingInfo CreateNativeMarshallingInfoForNonSignatureElement(
            ITypeSymbol type,
            INamedTypeSymbol entryPointType,
            AttributeData attrData,
            Compilation compilation,
            GeneratorDiagnosticsBag diagnostics)
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

            if (ManualTypeMarshallingHelper.IsLinearCollectionEntryPoint(entryPointType))
            {
                // We can't provide collection marshalling info like count information for non-signature elements,
                // so we disallow linear collection marshallers here.
                // TODO: Add diagnostic
                return NoMarshallingInfo.Instance;
            }

            if (ManualTypeMarshallingHelper.TryGetValueMarshallersFromEntryType(entryPointType, type, compilation, out CustomTypeMarshallers? marshallers))
            {
                return new NativeMarshallingAttributeInfo(entryPointTypeInfo, marshallers.Value);
            }

            return NoMarshallingInfo.Instance;
        }

        public static MarshallingInfo CreateMarshallingInfoByMarshallerTypeName(
            Compilation compilation,
            ITypeSymbol type,
            string marshallerName)
        {
            INamedTypeSymbol? marshallerType = compilation.GetBestTypeByMetadataName(marshallerName);
            if (marshallerType is null)
                return new MissingSupportMarshallingInfo();

            if (ManualTypeMarshallingHelper.HasEntryPointMarshallerAttribute(marshallerType))
            {
                if (ManualTypeMarshallingHelper.TryGetValueMarshallersFromEntryType(marshallerType, type, compilation, out CustomTypeMarshallers? marshallers))
                {
                    return new NativeMarshallingAttributeInfo(
                        EntryPointType: ManagedTypeInfo.CreateTypeInfoForTypeSymbol(marshallerType),
                        Marshallers: marshallers.Value);
                }
            }

            return new MissingSupportMarshallingInfo();
        }
    }
}

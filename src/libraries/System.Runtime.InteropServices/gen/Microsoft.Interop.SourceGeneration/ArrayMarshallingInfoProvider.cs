// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// Marshalling information provider for single-dimensional zero-based array types using the <c>System.Runtime.InteropServices.Marshalling.ArrayMarshaller</c> and <c>System.Runtime.InteropServices.Marshalling.PointerArrayMarshaller</c>
    /// built-in types.
    /// </summary>
    public sealed class ArrayMarshallingInfoProvider : ITypeBasedMarshallingInfoProvider
    {
        private readonly Compilation _compilation;

        public ArrayMarshallingInfoProvider(Compilation compilation)
        {
            _compilation = compilation;
        }

        public bool CanProvideMarshallingInfoForType(ITypeSymbol type) => type is IArrayTypeSymbol { IsSZArray: true };

        public MarshallingInfo GetMarshallingInfo(ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            CountInfo countInfo = NoCountInfo.Instance;
            if (useSiteAttributes.TryGetUseSiteAttributeInfo(indirectionDepth, out UseSiteAttributeData useSiteInfo))
            {
                countInfo = useSiteInfo.CountInfo;
            }

            ITypeSymbol elementType = ((IArrayTypeSymbol)type).ElementType;
            return CreateArrayMarshallingInfo(_compilation, type, elementType, countInfo, marshallingInfoCallback(elementType, useSiteAttributes, indirectionDepth + 1));
        }

        public static MarshallingInfo CreateArrayMarshallingInfo(
            Compilation compilation,
            ITypeSymbol managedType,
            ITypeSymbol elementType,
            CountInfo countInfo,
            MarshallingInfo elementMarshallingInfo)
        {
            ITypeSymbol typeArgumentToInsert = elementType;
            INamedTypeSymbol? arrayMarshaller;
            if (elementType is IPointerTypeSymbol { PointedAtType: ITypeSymbol pointedAt })
            {
                arrayMarshaller = compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_PointerArrayMarshaller_Metadata);
                typeArgumentToInsert = pointedAt;
            }
            else
            {
                arrayMarshaller = compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_ArrayMarshaller_Metadata);
            }

            if (arrayMarshaller is null)
            {
                // If the array marshaler type is not available, then we cannot marshal arrays but indicate it is missing.
                return new MissingSupportCollectionMarshallingInfo(countInfo, elementMarshallingInfo);
            }

            if (ManualTypeMarshallingHelper.HasEntryPointMarshallerAttribute(arrayMarshaller)
                && ManualTypeMarshallingHelper.IsLinearCollectionEntryPoint(arrayMarshaller))
            {
                arrayMarshaller = arrayMarshaller.Construct(
                    typeArgumentToInsert,
                    arrayMarshaller.TypeArguments.Last());

                Func<ITypeSymbol, MarshallingInfo> getMarshallingInfoForElement = (ITypeSymbol elementType) => elementMarshallingInfo;
                if (ManualTypeMarshallingHelper.TryGetLinearCollectionMarshallersFromEntryType(arrayMarshaller, managedType, compilation, getMarshallingInfoForElement, out CustomTypeMarshallers? marshallers))
                {
                    return new NativeLinearCollectionMarshallingInfo(
                        ManagedTypeInfo.CreateTypeInfoForTypeSymbol(arrayMarshaller),
                        marshallers.Value,
                        countInfo,
                        ManagedTypeInfo.CreateTypeInfoForTypeSymbol(arrayMarshaller.TypeParameters.Last()));
                }
            }

            Debug.WriteLine("Default marshallers for arrays should be a valid shape.");
            return NoMarshallingInfo.Instance;
        }
    }
}

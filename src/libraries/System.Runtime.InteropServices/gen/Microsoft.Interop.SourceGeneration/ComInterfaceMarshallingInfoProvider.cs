// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// This class supports generating marshalling info for types with the <c>System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute</c> attribute.
    /// </summary>
    public class ComInterfaceMarshallingInfoProvider : IMarshallingInfoAttributeParser
    {
        private readonly Compilation _compilation;

        public ComInterfaceMarshallingInfoProvider(Compilation compilation)
        {
            _compilation = compilation;
        }

        public bool CanParseAttributeType(INamedTypeSymbol attributeType) => attributeType.ToDisplayString() == TypeNames.GeneratedComInterfaceAttribute;

        public MarshallingInfo? ParseAttribute(AttributeData attributeData, ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            return CreateComInterfaceMarshallingInfo(_compilation, type);
        }

        public static MarshallingInfo CreateComInterfaceMarshallingInfo(
            Compilation compilation,
            ITypeSymbol interfaceType,
            TypePositionInfo? iidParameterIndexInfo = null)
        {
            INamedTypeSymbol? comInterfaceMarshaller = compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_Marshalling_ComInterfaceMarshaller_Metadata);
            if (comInterfaceMarshaller is null)
                return NoMarshallingInfo.Instance;

            comInterfaceMarshaller = comInterfaceMarshaller.Construct(interfaceType);

            if (ManualTypeMarshallingHelper.HasEntryPointMarshallerAttribute(comInterfaceMarshaller))
            {
                if (ManualTypeMarshallingHelper.TryGetValueMarshallersFromEntryType(comInterfaceMarshaller, interfaceType, compilation, out CustomTypeMarshallers? marshallers))
                {
                    // Extract the ComInterfaceOptions from the GeneratedComInterface attribute on the interface type
                    // to determine which marshalling directions are supported. This is stored as a set of boolean
                    // flags on the resulting marshalling info so that shared consumers (such as
                    // AttributedMarshallingModelGeneratorResolver) can validate direction without depending on the
                    // ComInterfaceOptions enum type.
                    GetComInterfaceWrapperSupport(interfaceType, out bool supportsManagedObjectWrapper, out bool supportsComObjectWrapper);

                    if (iidParameterIndexInfo is not null)
                    {
                        return new IidParameterIndexNativeMarshallingInfo(
                            EntryPointType: ManagedTypeInfo.CreateTypeInfoForTypeSymbol(comInterfaceMarshaller),
                            Marshallers: marshallers.Value,
                            IidParameterIndexInfo: iidParameterIndexInfo)
                        {
                            SupportsManagedToUnmanagedMarshalling = supportsManagedObjectWrapper,
                            SupportsUnmanagedToManagedMarshalling = supportsComObjectWrapper,
                        };
                    }

                    return new NativeMarshallingAttributeInfo(
                        EntryPointType: ManagedTypeInfo.CreateTypeInfoForTypeSymbol(comInterfaceMarshaller),
                        Marshallers: marshallers.Value)
                    {
                        SupportsManagedToUnmanagedMarshalling = supportsManagedObjectWrapper,
                        SupportsUnmanagedToManagedMarshalling = supportsComObjectWrapper,
                    };
                }
            }

            return NoMarshallingInfo.Instance;
        }

        // Values from ComInterfaceOptions in
        // src/libraries/System.Runtime.InteropServices/src/System/Runtime/InteropServices/Marshalling/ComInterfaceOptions.cs.
        // Duplicated here to avoid a shared-project dependency on the ComInterfaceGenerator-defined enum type.
        private const int ManagedObjectWrapperOption = 0x1;
        private const int ComObjectWrapperOption = 0x2;

        private static void GetComInterfaceWrapperSupport(ITypeSymbol interfaceType, out bool supportsManagedObjectWrapper, out bool supportsComObjectWrapper)
        {
            // Default when the attribute is absent or the Options property is not set matches
            // GeneratedComInterfaceCompilationData.Options: both wrappers are generated.
            supportsManagedObjectWrapper = true;
            supportsComObjectWrapper = true;

            foreach (AttributeData attr in interfaceType.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() != TypeNames.GeneratedComInterfaceAttribute)
                {
                    continue;
                }

                foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "Options" && namedArg.Value.Value is int options)
                    {
                        supportsManagedObjectWrapper = (options & ManagedObjectWrapperOption) != 0;
                        supportsComObjectWrapper = (options & ComObjectWrapperOption) != 0;
                    }
                }

                break;
            }
        }
    }
}

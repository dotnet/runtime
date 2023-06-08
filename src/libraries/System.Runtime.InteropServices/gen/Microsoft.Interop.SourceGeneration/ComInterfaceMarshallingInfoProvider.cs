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
            ITypeSymbol interfaceType)
        {
            INamedTypeSymbol? comInterfaceMarshaller = compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_Marshalling_ComInterfaceMarshaller_Metadata);
            if (comInterfaceMarshaller is null)
                return new MissingSupportMarshallingInfo();

            comInterfaceMarshaller = comInterfaceMarshaller.Construct(interfaceType);

            if (ManualTypeMarshallingHelper.HasEntryPointMarshallerAttribute(comInterfaceMarshaller))
            {
                if (ManualTypeMarshallingHelper.TryGetValueMarshallersFromEntryType(comInterfaceMarshaller, interfaceType, compilation, out CustomTypeMarshallers? marshallers))
                {
                    return new NativeMarshallingAttributeInfo(
                        EntryPointType: ManagedTypeInfo.CreateTypeInfoForTypeSymbol(comInterfaceMarshaller),
                        Marshallers: marshallers.Value);
                }
            }

            return new MissingSupportMarshallingInfo();
        }
    }
}

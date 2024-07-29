// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public static class CustomAttributeExtensions
    {
        public static CustomAttributeValue<TypeDesc>? GetDecodedCustomAttribute(this PropertyPseudoDesc prop, string attributeNamespace, string attributeName)
        {
            var ecmaType = prop.OwningType as EcmaType;
            var metadataReader = ecmaType.MetadataReader;

            var attributeHandle = metadataReader.GetCustomAttributeHandle(prop.GetCustomAttributes,
                attributeNamespace, attributeName);

            if (attributeHandle.IsNil)
                return null;

            return metadataReader.GetCustomAttribute(attributeHandle).DecodeValue(new CustomAttributeTypeProvider(ecmaType.EcmaModule));
        }

        public static IEnumerable<CustomAttributeValue<TypeDesc>> GetDecodedCustomAttributes(this PropertyPseudoDesc prop, string attributeNamespace, string attributeName)
        {
            var ecmaType = prop.OwningType as EcmaType;
            var metadataReader = ecmaType.MetadataReader;

            var attributeHandles = prop.GetCustomAttributes;
            foreach (var attributeHandle in attributeHandles)
            {
                if (MetadataExtensions.IsEqualCustomAttributeName(attributeHandle, metadataReader, attributeNamespace, attributeName))
                {
                    yield return metadataReader.GetCustomAttribute(attributeHandle).DecodeValue(new CustomAttributeTypeProvider(ecmaType.EcmaModule));
                }
            }
        }

        public static CustomAttributeValue<TypeDesc>? GetDecodedCustomAttribute(this EventPseudoDesc @event, string attributeNamespace, string attributeName)
        {
            var ecmaType = @event.OwningType as EcmaType;
            var metadataReader = ecmaType.MetadataReader;

            var attributeHandle = metadataReader.GetCustomAttributeHandle(@event.GetCustomAttributes,
                attributeNamespace, attributeName);

            if (attributeHandle.IsNil)
                return null;

            return metadataReader.GetCustomAttribute(attributeHandle).DecodeValue(new CustomAttributeTypeProvider(ecmaType.EcmaModule));
        }

        public static IEnumerable<CustomAttributeValue<TypeDesc>> GetDecodedCustomAttributes(this EventPseudoDesc @event, string attributeNamespace, string attributeName)
        {
            var ecmaType = @event.OwningType as EcmaType;
            var metadataReader = ecmaType.MetadataReader;

            var attributeHandles = @event.GetCustomAttributes;
            foreach (var attributeHandle in attributeHandles)
            {
                if (MetadataExtensions.IsEqualCustomAttributeName(attributeHandle, metadataReader, attributeNamespace, attributeName))
                {
                    yield return metadataReader.GetCustomAttribute(attributeHandle).DecodeValue(new CustomAttributeTypeProvider(ecmaType.EcmaModule));
                }
            }
        }

        public static IEnumerable<CustomAttributeValue<TypeDesc>> GetDecodedCustomAttributesForModule(this EcmaModule module, string attributeNamespace, string attributeName)
        {
            var metadataReader = module.MetadataReader;

            var attributeHandles = metadataReader.GetModuleDefinition().GetCustomAttributes();
            foreach (var attributeHandle in attributeHandles)
            {
                if (MetadataExtensions.IsEqualCustomAttributeName(attributeHandle, metadataReader, attributeNamespace, attributeName))
                {
                    yield return metadataReader.GetCustomAttribute(attributeHandle).DecodeValue(new CustomAttributeTypeProvider(module));
                }
            }
        }
    }
}

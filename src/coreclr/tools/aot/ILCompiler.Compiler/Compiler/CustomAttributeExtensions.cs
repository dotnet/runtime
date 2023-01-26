// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public static class CustomAttributeExtensions
    {
        public static IEnumerable<CustomAttributeValue<TypeDesc>> GetDecodedCustomAttributes(TypeSystemEntity entity, string attributeNamespace, string attributeName)
        {
            switch (entity)
            {
                case MethodDesc method:
                    var ecmaMethod = method.GetTypicalMethodDefinition() as EcmaMethod;
                    if (ecmaMethod == null)
                        return Enumerable.Empty<CustomAttributeValue<TypeDesc>>();
                    return ecmaMethod.GetDecodedCustomAttributes(attributeNamespace, attributeName);
                case MetadataType type:
                    var ecmaType = type.GetTypeDefinition() as EcmaType;
                    if (ecmaType == null)
                        return Enumerable.Empty<CustomAttributeValue<TypeDesc>>();
                    return ecmaType.GetDecodedCustomAttributes(attributeNamespace, attributeName);
                case FieldDesc field:
                    var ecmaField = field.GetTypicalFieldDefinition() as EcmaField;
                    if (ecmaField == null)
                        return Enumerable.Empty<CustomAttributeValue<TypeDesc>>();
                    return ecmaField.GetDecodedCustomAttributes(attributeNamespace, attributeName);
                case PropertyPseudoDesc property:
                    return property.GetDecodedCustomAttributes(attributeNamespace, attributeName);
                case EventPseudoDesc @event:
                    return @event.GetDecodedCustomAttributes(attributeNamespace, attributeName);
                case ModuleDesc module:
                    if (module is not EcmaAssembly ecmaAssembly)
                        return Enumerable.Empty<CustomAttributeValue<TypeDesc>>();
                    return ecmaAssembly.GetDecodedModuleCustomAttributes(attributeNamespace, attributeName)
                        .Concat(ecmaAssembly.GetDecodedCustomAttributes(attributeNamespace, attributeName));
                default:
                    Debug.Fail("Trying to operate with unsupported TypeSystemEntity " + entity.GetType().ToString());
                    return Enumerable.Empty<CustomAttributeValue<TypeDesc>>();
            }
        }

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

        public static IEnumerable<CustomAttributeValue<TypeDesc>> GetDecodedModuleCustomAttributes(this EcmaModule module, string attributeNamespace, string attributeName)
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

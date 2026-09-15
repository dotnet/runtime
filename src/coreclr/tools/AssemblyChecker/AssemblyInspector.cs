// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace AssemblyChecker
{
    internal static class AssemblyInspector
    {
        internal static bool IsDebug(string path)
        {
            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using PEReader peReader = new(stream);
            MetadataReader reader = peReader.GetMetadataReader();
            AssemblyDefinition assembly = reader.GetAssemblyDefinition();

            foreach (CustomAttributeHandle attributeHandle in assembly.GetCustomAttributes())
            {
                CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);
                if (!IsDebuggableAttribute(reader, attribute))
                {
                    continue;
                }

                if (!HasSupportedConstructorSignature(reader, attribute.Constructor))
                {
                    throw new BadImageFormatException("DebuggableAttribute has an unsupported constructor signature.");
                }

                CustomAttributeValue<AttributeType> value = attribute.DecodeValue(AttributeTypeProvider.Instance);
                if (value.FixedArguments.Length == 1)
                {
                    if (value.FixedArguments[0].Type != AttributeType.DebuggingModes ||
                        value.FixedArguments[0].Value is not int modes)
                    {
                        throw new BadImageFormatException("DebuggableAttribute has an invalid debugging modes argument.");
                    }

                    if (((DebuggableAttribute.DebuggingModes)modes & DebuggableAttribute.DebuggingModes.DisableOptimizations) != 0)
                    {
                        return true;
                    }
                }
                else if (value.FixedArguments.Length == 2)
                {
                    if (value.FixedArguments[0].Type != AttributeType.Boolean ||
                        value.FixedArguments[1].Type != AttributeType.Boolean ||
                        value.FixedArguments[1].Value is not bool optimizationsDisabled)
                    {
                        throw new BadImageFormatException("DebuggableAttribute has invalid Boolean arguments.");
                    }

                    if (optimizationsDisabled)
                    {
                        return true;
                    }
                }
                else
                {
                    throw new BadImageFormatException("DebuggableAttribute has an unsupported constructor.");
                }
            }

            return false;
        }

        private static bool IsDebuggableAttribute(MetadataReader reader, CustomAttribute attribute)
        {
            EntityHandle attributeType;

            switch (attribute.Constructor.Kind)
            {
                case HandleKind.MemberReference:
                    MemberReference reference = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                    if (!reader.StringComparer.Equals(reference.Name, ".ctor"))
                    {
                        return false;
                    }

                    attributeType = reference.Parent;
                    break;

                case HandleKind.MethodDefinition:
                    MethodDefinition constructor = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
                    if (!reader.StringComparer.Equals(constructor.Name, ".ctor"))
                    {
                        return false;
                    }

                    attributeType = constructor.GetDeclaringType();
                    break;

                default:
                    return false;
            }

            return IsDebuggableAttributeType(reader, attributeType);
        }

        private static bool HasSupportedConstructorSignature(MetadataReader reader, EntityHandle constructor)
        {
            BlobHandle signature = constructor.Kind switch
            {
                HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)constructor).Signature,
                HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)constructor).Signature,
                _ => default,
            };

            BlobReader signatureReader = reader.GetBlobReader(signature);
            SignatureHeader header = signatureReader.ReadSignatureHeader();
            if (header.Kind != SignatureKind.Method || !header.IsInstance || header.IsGeneric || header.HasExplicitThis)
            {
                return false;
            }

            int parameterCount = signatureReader.ReadCompressedInteger();
            if (signatureReader.ReadSignatureTypeCode() != SignatureTypeCode.Void)
            {
                return false;
            }

            bool supported = parameterCount switch
            {
                1 => signatureReader.ReadCompressedInteger() == (int)SignatureTypeKind.ValueType
                    && IsDebuggingModesType(reader, signatureReader.ReadTypeHandle()),
                2 => signatureReader.ReadSignatureTypeCode() == SignatureTypeCode.Boolean
                    && signatureReader.ReadSignatureTypeCode() == SignatureTypeCode.Boolean,
                _ => false,
            };

            return supported && signatureReader.RemainingBytes == 0;
        }

        private static bool IsDebuggableAttributeType(MetadataReader reader, EntityHandle handle)
        {
            StringHandle typeNamespace;
            StringHandle typeName;

            if (handle.Kind == HandleKind.TypeReference)
            {
                TypeReference type = reader.GetTypeReference((TypeReferenceHandle)handle);
                typeNamespace = type.Namespace;
                typeName = type.Name;
            }
            else if (handle.Kind == HandleKind.TypeDefinition)
            {
                TypeDefinition type = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
                typeNamespace = type.Namespace;
                typeName = type.Name;
            }
            else
            {
                return false;
            }

            return reader.StringComparer.Equals(typeNamespace, "System.Diagnostics")
                && reader.StringComparer.Equals(typeName, nameof(DebuggableAttribute));
        }

        private static bool IsDebuggingModesType(MetadataReader reader, EntityHandle handle)
        {
            if (handle.Kind == HandleKind.TypeReference)
            {
                TypeReference type = reader.GetTypeReference((TypeReferenceHandle)handle);
                return reader.StringComparer.Equals(type.Name, nameof(DebuggableAttribute.DebuggingModes))
                    && IsDebuggableAttributeType(reader, type.ResolutionScope);
            }

            if (handle.Kind == HandleKind.TypeDefinition)
            {
                TypeDefinition type = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
                return reader.StringComparer.Equals(type.Name, nameof(DebuggableAttribute.DebuggingModes))
                    && IsDebuggableAttributeType(reader, type.GetDeclaringType());
            }

            return false;
        }

        private enum AttributeType
        {
            Boolean,
            DebuggingModes,
            Other,
            SystemType,
        }

        private sealed class AttributeTypeProvider : ICustomAttributeTypeProvider<AttributeType>
        {
            internal static readonly AttributeTypeProvider Instance = new();

            public AttributeType GetPrimitiveType(PrimitiveTypeCode typeCode) =>
                typeCode == PrimitiveTypeCode.Boolean ? AttributeType.Boolean : AttributeType.Other;

            public AttributeType GetSystemType() => AttributeType.SystemType;

            public AttributeType GetSZArrayType(AttributeType elementType) => AttributeType.Other;

            public AttributeType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                return IsDebuggingModesType(reader, handle)
                    ? AttributeType.DebuggingModes
                    : AttributeType.Other;
            }

            public AttributeType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                return IsDebuggingModesType(reader, handle)
                    ? AttributeType.DebuggingModes
                    : AttributeType.Other;
            }

            public AttributeType GetTypeFromSerializedName(string name) => AttributeType.Other;

            public PrimitiveTypeCode GetUnderlyingEnumType(AttributeType type)
            {
                if (type != AttributeType.DebuggingModes)
                {
                    throw new BadImageFormatException("DebuggableAttribute has an invalid enum argument.");
                }

                return PrimitiveTypeCode.Int32;
            }

            public bool IsSystemType(AttributeType type) => type == AttributeType.SystemType;
        }
    }
}

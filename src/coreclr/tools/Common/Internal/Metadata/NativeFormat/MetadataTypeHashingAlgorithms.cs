// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Debug = System.Diagnostics.Debug;
using HashCodeBuilder = Internal.VersionResilientHashCode.HashCodeBuilder;
using TypeAttributes = System.Reflection.TypeAttributes;
using TypeHashingAlgorithms = Internal.NativeFormat.TypeHashingAlgorithms;

namespace Internal.Metadata.NativeFormat
{
    internal static class MetadataTypeHashingAlgorithms
    {
        private static void AppendNamespaceHashCode(ref HashCodeBuilder builder, NamespaceDefinitionHandle namespaceDefHandle, MetadataReader reader, bool appendDot)
        {
            NamespaceDefinition namespaceDefinition = reader.GetNamespaceDefinition(namespaceDefHandle);

            Handle parentHandle = namespaceDefinition.ParentScopeOrNamespace;
            HandleType parentHandleType = parentHandle.HandleType;
            if (parentHandleType == HandleType.NamespaceDefinition)
            {
                AppendNamespaceHashCode(ref builder, parentHandle.ToNamespaceDefinitionHandle(reader), reader, appendDot: true);
                ReadOnlySpan<byte> namespaceNamePart = reader.ReadStringAsBytes(namespaceDefinition.Name);
                builder.Append(namespaceNamePart);
                if (appendDot)
                    builder.Append("."u8);
            }
            else
            {
                Debug.Assert(parentHandleType == HandleType.ScopeDefinition);
                Debug.Assert(string.IsNullOrEmpty(reader.GetString(namespaceDefinition.Name)), "Root namespace with a name?");
            }
        }

        private static void AppendNamespaceHashCode(ref HashCodeBuilder builder, NamespaceReferenceHandle namespaceRefHandle, MetadataReader reader, bool appendDot)
        {
            NamespaceReference namespaceReference = reader.GetNamespaceReference(namespaceRefHandle);

            Handle parentHandle = namespaceReference.ParentScopeOrNamespace;
            HandleType parentHandleType = parentHandle.HandleType;
            if (parentHandleType == HandleType.NamespaceReference)
            {
                AppendNamespaceHashCode(ref builder, parentHandle.ToNamespaceReferenceHandle(reader), reader, appendDot: true);
                ReadOnlySpan<byte> namespaceNamePart = reader.ReadStringAsBytes(namespaceReference.Name);
                builder.Append(namespaceNamePart);
                if (appendDot)
                    builder.Append("."u8);
            }
            else
            {
                Debug.Assert(parentHandleType == HandleType.ScopeReference);
                Debug.Assert(string.IsNullOrEmpty(reader.GetString(namespaceReference.Name)), "Root namespace with a name?");
            }
        }

        public static int ComputeHashCode(this TypeDefinitionHandle typeDefHandle, MetadataReader reader)
        {
            TypeDefinition typeDef = reader.GetTypeDefinition(typeDefHandle);

            HashCodeBuilder builder = new HashCodeBuilder(""u8);
            AppendNamespaceHashCode(ref builder, typeDef.NamespaceDefinition, reader, appendDot: false);
            int nameHashCode = VersionResilientHashCode.NameHashCode(reader.ReadStringAsBytes(typeDef.Name));

            int hashCode = VersionResilientHashCode.NameHashCode(builder.ToHashCode(), nameHashCode);

            if (typeDef.Flags.IsNested())
            {
                int enclosingTypeHashCode = typeDef.EnclosingType.ComputeHashCode(reader);
                return VersionResilientHashCode.NestedTypeHashCode(enclosingTypeHashCode, hashCode);
            }

            return hashCode;
        }

        public static int ComputeHashCode(this TypeReferenceHandle typeRefHandle, MetadataReader reader)
        {
            TypeReference typeRef = reader.GetTypeReference(typeRefHandle);

            HashCodeBuilder builder = new HashCodeBuilder(""u8);
            AppendNamespaceHashCode(ref builder, typeRef.ParentNamespaceOrType.ToNamespaceReferenceHandle(reader), reader, appendDot: false);
            int nameHashCode = VersionResilientHashCode.NameHashCode(reader.ReadStringAsBytes(typeRef.TypeName));

            int hashCode = VersionResilientHashCode.NameHashCode(builder.ToHashCode(), nameHashCode);

            if (typeRef.ParentNamespaceOrType.HandleType == HandleType.TypeReference)
            {
                int enclosingTypeHashCode = typeRef.ParentNamespaceOrType.ToTypeReferenceHandle(reader).ComputeHashCode(reader);
                return VersionResilientHashCode.NestedTypeHashCode(enclosingTypeHashCode, hashCode);
            }

            return hashCode;
        }

        // This mask is the fastest way to check if a type is nested from its flags,
        // but it should not be added to the BCL enum as its semantics can be misleading.
        // Consider, for example, that (NestedFamANDAssem & NestedMask) == NestedFamORAssem.
        // Only comparison of the masked value to 0 is meaningful, which is different from
        // the other masks in the enum.
        private const TypeAttributes NestedMask = (TypeAttributes)0x00000006;

        private static bool IsNested(this TypeAttributes flags)
        {
            return (flags & NestedMask) != 0;
        }
    }
}

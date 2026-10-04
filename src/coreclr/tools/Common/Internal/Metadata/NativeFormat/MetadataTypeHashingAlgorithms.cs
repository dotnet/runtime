// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Debug = System.Diagnostics.Debug;
using HashCodeBuilder = Internal.VersionResilientHashCode.HashCodeBuilder;
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

            Handle namespaceOrEnclosingType = typeDef.NamespaceOrEnclosingType;
            if (namespaceOrEnclosingType.HandleType == HandleType.NamespaceDefinition)
            {
                AppendNamespaceHashCode(ref builder, namespaceOrEnclosingType.ToNamespaceDefinitionHandle(reader), reader, appendDot: false);
            }

            int nameHashCode = VersionResilientHashCode.NameHashCode(reader.ReadStringAsBytes(typeDef.Name));

            int hashCode = VersionResilientHashCode.NameHashCode(builder.ToHashCode(), nameHashCode);

            if (namespaceOrEnclosingType.HandleType == HandleType.TypeDefinition)
            {
                int enclosingTypeHashCode = namespaceOrEnclosingType.ToTypeDefinitionHandle(reader).ComputeHashCode(reader);
                return VersionResilientHashCode.NestedTypeHashCode(enclosingTypeHashCode, hashCode);
            }

            return hashCode;
        }

        public static int ComputeHashCode(this TypeReferenceHandle typeRefHandle, MetadataReader reader)
        {
            TypeReference typeRef = reader.GetTypeReference(typeRefHandle);

            HashCodeBuilder builder = new HashCodeBuilder(""u8);
            AppendNamespaceHashCode(ref builder, typeRef.NamespaceOrEnclosingType.ToNamespaceReferenceHandle(reader), reader, appendDot: false);
            int nameHashCode = VersionResilientHashCode.NameHashCode(reader.ReadStringAsBytes(typeRef.TypeName));

            int hashCode = VersionResilientHashCode.NameHashCode(builder.ToHashCode(), nameHashCode);

            if (typeRef.NamespaceOrEnclosingType.HandleType == HandleType.TypeReference)
            {
                int enclosingTypeHashCode = typeRef.NamespaceOrEnclosingType.ToTypeReferenceHandle(reader).ComputeHashCode(reader);
                return VersionResilientHashCode.NestedTypeHashCode(enclosingTypeHashCode, hashCode);
            }

            return hashCode;
        }

    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using TypeAttributes = System.Reflection.TypeAttributes;
using Debug = System.Diagnostics.Debug;
using HashCodeBuilder = Internal.NativeFormat.TypeHashingAlgorithms.HashCodeBuilder;
using TypeHashingAlgorithms = Internal.NativeFormat.TypeHashingAlgorithms;

namespace Internal.Metadata.NativeFormat
{
    internal static class MetadataTypeHashingAlgorithms
    {
        private static void AppendNamespaceHashCode(ref HashCodeBuilder builder, NamespaceDefinitionHandle namespaceDefHandle, MetadataReader reader)
        {
            NamespaceDefinition namespaceDefinition = reader.GetNamespaceDefinition(namespaceDefHandle);

            Handle parentHandle = namespaceDefinition.ParentScopeOrNamespace;
            HandleType parentHandleType = parentHandle.HandleType;
            if (parentHandleType == HandleType.NamespaceDefinition)
            {
                AppendNamespaceHashCode(ref builder, parentHandle.ToNamespaceDefinitionHandle(reader), reader);
                string namespaceNamePart = reader.GetString(namespaceDefinition.Name);
                builder.Append(namespaceNamePart);
                builder.Append(".");
            }
            else
            {
                Debug.Assert(parentHandleType == HandleType.ScopeDefinition);
                Debug.Assert(string.IsNullOrEmpty(reader.GetString(namespaceDefinition.Name)), "Root namespace with a name?");
            }
        }

        private static void AppendNamespaceHashCode(ref HashCodeBuilder builder, NamespaceReferenceHandle namespaceRefHandle, MetadataReader reader)
        {
            NamespaceReference namespaceReference = reader.GetNamespaceReference(namespaceRefHandle);

            Handle parentHandle = namespaceReference.ParentScopeOrNamespace;
            HandleType parentHandleType = parentHandle.HandleType;
            if (parentHandleType == HandleType.NamespaceReference)
            {
                AppendNamespaceHashCode(ref builder, parentHandle.ToNamespaceReferenceHandle(reader), reader);
                string namespaceNamePart = reader.GetString(namespaceReference.Name);
                builder.Append(namespaceNamePart);
                builder.Append(".");
            }
            else
            {
                Debug.Assert(parentHandleType == HandleType.ScopeReference);
                Debug.Assert(string.IsNullOrEmpty(reader.GetString(namespaceReference.Name)), "Root namespace with a name?");
            }
        }

        public static int ComputeHashCode(this TypeDefinitionHandle typeDefHandle, MetadataReader reader)
        {
            HashCodeBuilder builder = new HashCodeBuilder("");

            TypeDefinition typeDef = reader.GetTypeDefinition(typeDefHandle);
            bool isNested = typeDef.Flags.IsNested();
            if (!isNested)
            {
                AppendNamespaceHashCode(ref builder, typeDef.NamespaceDefinition, reader);
            }

            string typeName = reader.GetString(typeDef.Name);
            builder.Append(typeName);

            if (isNested)
            {
                int enclosingTypeHashCode = typeDef.EnclosingType.ComputeHashCode(reader);
                return TypeHashingAlgorithms.ComputeNestedTypeHashCode(enclosingTypeHashCode, builder.ToHashCode());
            }

            return builder.ToHashCode();
        }

        public static int ComputeHashCode(this TypeReferenceHandle typeRefHandle, MetadataReader reader)
        {
            HashCodeBuilder builder = new HashCodeBuilder("");

            TypeReference typeRef = reader.GetTypeReference(typeRefHandle);
            HandleType parentHandleType = typeRef.ParentNamespaceOrType.HandleType;
            bool isNested = parentHandleType == HandleType.TypeReference;
            if (!isNested)
            {
                Debug.Assert(parentHandleType == HandleType.NamespaceReference);
                AppendNamespaceHashCode(ref builder, typeRef.ParentNamespaceOrType.ToNamespaceReferenceHandle(reader), reader);
            }

            string typeName = reader.GetString(typeRef.TypeName);
            builder.Append(typeName);

            if (isNested)
            {
                int enclosingTypeHashCode = typeRef.ParentNamespaceOrType.ToTypeReferenceHandle(reader).ComputeHashCode(reader);
                return TypeHashingAlgorithms.ComputeNestedTypeHashCode(enclosingTypeHashCode, builder.ToHashCode());
            }

            return builder.ToHashCode();
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

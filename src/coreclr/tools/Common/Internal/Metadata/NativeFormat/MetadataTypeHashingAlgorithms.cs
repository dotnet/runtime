// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.Metadata.NativeFormat
{
    internal static class MetadataTypeHashingAlgorithms
    {
        public static int ComputeHashCode(this TypeDefinitionHandle typeDefHandle, MetadataReader reader)
        {
            TypeDefinition typeDef = reader.GetTypeDefinition(typeDefHandle);

            int namespaceHashCode = 0;

            Handle namespaceOrEnclosingType = typeDef.NamespaceOrEnclosingType;
            if (namespaceOrEnclosingType.HandleType == HandleType.NamespaceDefinition)
            {
                NamespaceDefinition namespaceDefinition = reader.GetNamespaceDefinition(namespaceOrEnclosingType.ToNamespaceDefinitionHandle(reader));
                namespaceHashCode = VersionResilientHashCode.NameHashCode(reader.ReadStringAsBytes(namespaceDefinition.Name));
            }

            int nameHashCode = VersionResilientHashCode.NameHashCode(reader.ReadStringAsBytes(typeDef.Name));

            int hashCode = VersionResilientHashCode.NameHashCode(namespaceHashCode, nameHashCode);

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

            int namespaceHashCode = 0;
            Handle namespaceOrEnclosingType = typeRef.NamespaceOrEnclosingType;
            if (namespaceOrEnclosingType.HandleType == HandleType.NamespaceReference)
            {
                NamespaceReference namespaceReference = reader.GetNamespaceReference(namespaceOrEnclosingType.ToNamespaceReferenceHandle(reader));
                namespaceHashCode = VersionResilientHashCode.NameHashCode(reader.ReadStringAsBytes(namespaceReference.Name));
            }
            int nameHashCode = VersionResilientHashCode.NameHashCode(reader.ReadStringAsBytes(typeRef.TypeName));

            int hashCode = VersionResilientHashCode.NameHashCode(namespaceHashCode, nameHashCode);

            if (namespaceOrEnclosingType.HandleType == HandleType.TypeReference)
            {
                int enclosingTypeHashCode = namespaceOrEnclosingType.ToTypeReferenceHandle(reader).ComputeHashCode(reader);
                return VersionResilientHashCode.NestedTypeHashCode(enclosingTypeHashCode, hashCode);
            }

            return hashCode;
        }

    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Emit
{
    // This static helper class adds common entities to a MetadataBuilder.
    internal static class MetadataHelper
    {
        internal static AssemblyReferenceHandle AddAssemblyReference(Assembly assembly, MetadataBuilder metadata)
        {
            AssemblyName assemblyName = assembly.GetName();

            return AddAssemblyReference(metadata, assemblyName.Name!, assemblyName.Version, assemblyName.CultureName,
                assemblyName.GetPublicKeyToken(), assemblyName.Flags, assemblyName.ContentType);
        }

        internal static AssemblyReferenceHandle AddAssemblyReference(MetadataBuilder metadata, string name, Version? version,
            string? culture, byte[]? publicKeyToken, AssemblyNameFlags flags, AssemblyContentType contentType)
        {
            return metadata.AddAssemblyReference(
                name: metadata.GetOrAddString(name),
                version: version ?? new Version(0, 0, 0, 0),
                culture: (culture == null) ? default : metadata.GetOrAddString(value: culture),
                publicKeyOrToken: (publicKeyToken == null) ? default : metadata.GetOrAddBlob(publicKeyToken), // reference has token, not full public key
                flags: (AssemblyFlags)((int)contentType << 9) | ((flags & AssemblyNameFlags.Retargetable) != 0 ? AssemblyFlags.Retargetable : 0),
                hashValue: default); // .file directive assemblies not supported, no need to handle this value.
        }

        internal static TypeDefinitionHandle AddTypeDefinition(MetadataBuilder metadata, TypeBuilderImpl typeBuilder, EntityHandle baseType, int methodToken, int fieldToken)
        {
            // Add type metadata
            return metadata.AddTypeDefinition(
                attributes: typeBuilder.Attributes,
                @namespace: (typeBuilder.Namespace == null) ? default : metadata.GetOrAddString(typeBuilder.Namespace),
                name: metadata.GetOrAddString(typeBuilder.Name),
                baseType: baseType,
                fieldList: MetadataTokens.FieldDefinitionHandle(fieldToken),
                methodList: MetadataTokens.MethodDefinitionHandle(methodToken));
        }

        internal static TypeReferenceHandle AddTypeReference(MetadataBuilder metadata, Type type, AssemblyReferenceHandle parent)
        {
            return AddTypeReference(metadata, parent, type.Name, type.Namespace);
        }

        internal static TypeReferenceHandle AddTypeReference(MetadataBuilder metadata, AssemblyReferenceHandle parent, string name, string? nameSpace)
        {
            return metadata.AddTypeReference(
                resolutionScope: parent,
                @namespace: (nameSpace == null) ? default : metadata.GetOrAddString(nameSpace),
                name: metadata.GetOrAddString(name)
                );
        }

        internal static MethodDefinitionHandle AddMethodDefinition(MetadataBuilder metadata, MethodBuilderImpl methodBuilder, BlobBuilder methodSignatureBlob)
        {
            return metadata.AddMethodDefinition(
                attributes: methodBuilder.Attributes,
                implAttributes: MethodImplAttributes.IL,
                name: metadata.GetOrAddString(methodBuilder.Name),
                signature: metadata.GetOrAddBlob(methodSignatureBlob),
                bodyOffset: -1, // No body supported yet
                parameterList: MetadataTokens.ParameterHandle(1)
                );
        }

        internal static FieldDefinitionHandle AddFieldDefinition(MetadataBuilder metadata, FieldInfo field, BlobBuilder fieldSignatureBlob)
        {
            return metadata.AddFieldDefinition(
                attributes: field.Attributes,
                name: metadata.GetOrAddString(field.Name),
                signature: metadata.GetOrAddBlob(fieldSignatureBlob));
        }
    }
}

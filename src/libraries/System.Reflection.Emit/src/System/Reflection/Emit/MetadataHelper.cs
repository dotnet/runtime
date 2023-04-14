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
            string? culture, byte[]? publicKeyToken, AssemblyNameFlags flags, AssemblyContentType contentType) =>
            metadata.AddAssemblyReference(
                name: metadata.GetOrAddString(name),
                version: version ?? new Version(0, 0, 0, 0),
                culture: (culture == null) ? default : metadata.GetOrAddString(value: culture),
                publicKeyOrToken: (publicKeyToken == null) ? default : metadata.GetOrAddBlob(publicKeyToken), // reference has token, not full public key
                flags: (AssemblyFlags)((int)contentType << 9) | ((flags & AssemblyNameFlags.Retargetable) != 0 ? AssemblyFlags.Retargetable : 0),
                hashValue: default); // .file directive assemblies not supported, no need to handle this value.

        internal static TypeDefinitionHandle AddTypeDefinition(MetadataBuilder metadata, TypeBuilderImpl typeBuilder,
            EntityHandle baseType, int methodToken, int fieldToken) =>
            metadata.AddTypeDefinition(
                attributes: typeBuilder.Attributes,
                @namespace: (typeBuilder.Namespace == null) ? default : metadata.GetOrAddString(typeBuilder.Namespace),
                name: metadata.GetOrAddString(typeBuilder.Name),
                baseType: baseType,
                fieldList: MetadataTokens.FieldDefinitionHandle(fieldToken),
                methodList: MetadataTokens.MethodDefinitionHandle(methodToken)
                );

        internal static TypeReferenceHandle AddTypeReference(MetadataBuilder metadata, Type type, AssemblyReferenceHandle parent) =>
            metadata.AddTypeReference(
                resolutionScope: parent,
                @namespace: (type.Namespace == null) ? default : metadata.GetOrAddString(type.Namespace),
                name: metadata.GetOrAddString(type.Name)
                );

        internal static MethodDefinitionHandle AddMethodDefinition(MetadataBuilder metadata, MethodBuilderImpl methodBuilder, BlobBuilder methodSignatureBlob) =>
            metadata.AddMethodDefinition(
                attributes: methodBuilder.Attributes,
                implAttributes: methodBuilder.GetMethodImplementationFlags(),
                name: metadata.GetOrAddString(methodBuilder.Name),
                signature: metadata.GetOrAddBlob(methodSignatureBlob),
                bodyOffset: -1, // No body supported yet
                parameterList: MetadataTokens.ParameterHandle(1)
                );

        internal static FieldDefinitionHandle AddFieldDefinition(MetadataBuilder metadata, FieldInfo field, BlobBuilder fieldSignatureBlob) =>
            metadata.AddFieldDefinition(
                attributes: field.Attributes,
                name: metadata.GetOrAddString(field.Name),
                signature: metadata.GetOrAddBlob(fieldSignatureBlob)
                );

        internal static MemberReferenceHandle AddConstructorReference(ModuleBuilderImpl moduleBuilder, MetadataBuilder metadata, TypeReferenceHandle parent, MethodBase method)
        {
            var blob = MetadataSignatureHelper.ConstructorSignatureEncoder(method.GetParameters(), moduleBuilder);
            return metadata.AddMemberReference(
                    parent: parent,
                    name: metadata.GetOrAddString(method.Name),
                    signature: metadata.GetOrAddBlob(blob)
                    );
        }

        internal static void AddMethodImport(MetadataBuilder metadata, MethodDefinitionHandle methodHandle,
            string name, MethodImportAttributes attributes, ModuleReferenceHandle moduleHandle) =>
            metadata.AddMethodImport(
                method: methodHandle,
                attributes: attributes,
                name: metadata.GetOrAddString(name),
                module: moduleHandle
                );

        internal static ModuleReferenceHandle AddModuleReference(MetadataBuilder metadata, string moduleName) =>
            metadata.AddModuleReference(moduleName: metadata.GetOrAddString(moduleName));

        internal static void AddFieldLayout(MetadataBuilder metadata, FieldDefinitionHandle fieldHandle, int offset) =>
            metadata.AddFieldLayout(field: fieldHandle, offset: offset);
    }
}

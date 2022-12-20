// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;

using global::Internal.Metadata.NativeFormat;

namespace Internal.Runtime.TypeLoader
{
    internal static class MetadataNameExtensions
    {
        public static string GetFullName(this Handle handle, MetadataReader reader)
        {
            switch (handle.HandleType)
            {
                case HandleType.TypeDefinition:
                    return handle.ToTypeDefinitionHandle(reader).GetFullName(reader);
                case HandleType.TypeReference:
                    return handle.ToTypeReferenceHandle(reader).GetFullName(reader);

                case HandleType.NamespaceDefinition:
                    return handle.ToNamespaceDefinitionHandle(reader).GetFullName(reader);
                case HandleType.NamespaceReference:
                    return handle.ToNamespaceReferenceHandle(reader).GetFullName(reader);

                case HandleType.TypeSpecification:
                    return handle.ToTypeSpecificationHandle(reader).GetFullName(reader);
                case HandleType.TypeInstantiationSignature:
                    return handle.ToTypeInstantiationSignatureHandle(reader).GetFullName(reader);

                case HandleType.ArraySignature:
                    return handle.ToArraySignatureHandle(reader).GetFullName(reader);
                case HandleType.SZArraySignature:
                    return handle.ToSZArraySignatureHandle(reader).GetFullName(reader);

                case HandleType.PointerSignature:
                    return handle.ToPointerSignatureHandle(reader).GetFullName(reader);
                case HandleType.ByReferenceSignature:
                    return handle.ToByReferenceSignatureHandle(reader).GetFullName(reader);

                case HandleType.ScopeDefinition:
                    return handle.ToScopeDefinitionHandle(reader).GetFullName(reader);
                case HandleType.ScopeReference:
                    return handle.ToScopeReferenceHandle(reader).GetFullName(reader);
            }
            return null;
        }

        public static string GetFullName(this ByReferenceSignatureHandle handle, MetadataReader reader)
        {
            var result = handle.GetByReferenceSignature(reader).Type.GetFullName(reader);
            if (result == null) return null;
            return result + "&";
        }

        public static string GetFullName(this PointerSignatureHandle handle, MetadataReader reader)
        {
            var result = handle.GetPointerSignature(reader).Type.GetFullName(reader);
            if (result == null) return null;
            return result + "*";
        }

        public static string GetFullName(this ArraySignatureHandle handle, MetadataReader reader)
        {
            ArraySignature array = handle.GetArraySignature(reader);
            var result = array.ElementType.GetFullName(reader);
            if (result == null) return null;
            return result + "[" + (new string(',', array.Rank - 1)) + "]";
        }

        public static string GetFullName(this SZArraySignatureHandle handle, MetadataReader reader)
        {
            var result = handle.GetSZArraySignature(reader).ElementType.GetFullName(reader);
            if (result == null) return null;
            return result + "[]";
        }

        public static string GetFullName(this TypeSpecificationHandle typeSpecHandle, MetadataReader reader)
        {
            var typeSpec = typeSpecHandle.GetTypeSpecification(reader);

            if (typeSpec.Signature.IsNull(reader))
                return null;

            return typeSpec.Signature.GetFullName(reader);
        }

        public static string GetFullName(this TypeInstantiationSignatureHandle typeInstSigHandle, MetadataReader reader)
        {
            var typeInstSig = typeInstSigHandle.GetTypeInstantiationSignature(reader);

            if (typeInstSig.GenericType.IsNull(reader))
                return null;

            var name = typeInstSig.GenericType.GetFullName(reader);
            if (name == null)
                return null;

            var index = 0;
            string argsString = null;
            foreach (var argHandle in typeInstSig.GenericTypeArguments)
            {
                if (index > 0) argsString += ",";
                var argName = argHandle.GetFullName(reader);
                if (argName == null) return name;
                argsString += argName;
                index++;
            }
            return name + "<" + argsString + ">";
        }

        public static void GetFullName(this TypeDefinitionHandle typeDefHandle, MetadataReader reader, out string name, out string enclosing, out string nspace)
        {
            var typeDef = typeDefHandle.GetTypeDefinition(reader);

            Debug.Assert(!typeDef.Name.IsNull(reader));

            name = typeDef.Name.GetConstantStringValue(reader).Value;
            enclosing = typeDef.EnclosingType.IsNull(reader) ? null : typeDef.EnclosingType.GetFullName(reader);
            nspace = typeDef.NamespaceDefinition.IsNull(reader) ? null : typeDef.NamespaceDefinition.GetFullName(reader);
        }

        public static string GetFullName(this TypeDefinitionHandle typeDefHandle, MetadataReader reader)
        {
            string name;
            string enclosing;
            string nspace;
            typeDefHandle.GetFullName(reader, out name, out enclosing, out nspace);

            if (enclosing != null && name != null)
                return enclosing + "+" + name;
            else if (nspace != null && name != null)
                return nspace + "." + name;

            return name;
        }

        public static string GetContainingModuleName(this TypeDefinitionHandle typeDefHandle, MetadataReader reader)
        {
            var typeDef = typeDefHandle.GetTypeDefinition(reader);

            Handle currentHandle = !typeDef.EnclosingType.IsNull(reader) ? (Handle)typeDef.EnclosingType : (Handle)typeDef.NamespaceDefinition;
            Debug.Assert(!currentHandle.IsNull(reader));

            while (!currentHandle.IsNull(reader))
            {
                switch (currentHandle.HandleType)
                {
                    case HandleType.TypeDefinition:
                        typeDef = currentHandle.ToTypeDefinitionHandle(reader).GetTypeDefinition(reader);
                        currentHandle = !typeDef.EnclosingType.IsNull(reader) ? (Handle)typeDef.EnclosingType : (Handle)typeDef.NamespaceDefinition;
                        break;

                    case HandleType.NamespaceDefinition:
                        currentHandle = currentHandle.ToNamespaceDefinitionHandle(reader).GetNamespaceDefinition(reader).ParentScopeOrNamespace;
                        break;

                    case HandleType.ScopeDefinition:
                        return currentHandle.GetFullName(reader);

                    default:
                        return "?";
                }
            }

            return "?";
        }
        public static string GetFullName(this NamespaceDefinitionHandle namespaceHandle, MetadataReader reader)
        {
            var nspace = namespaceHandle.GetNamespaceDefinition(reader);

            if (nspace.Name.IsNull(reader))
                return null;

            var name = nspace.Name.GetConstantStringValue(reader).Value;
            var containingNamespace = nspace.ParentScopeOrNamespace.IsNull(reader) ? null : nspace.ParentScopeOrNamespace.GetFullName(reader);

            if (containingNamespace != null)
                return containingNamespace + "." + name;

            return name;
        }

        public static void GetFullName(this TypeReferenceHandle typeRefHandle, MetadataReader reader, out string name, out string enclosing, out string nspace)
        {
            var typeRef = typeRefHandle.GetTypeReference(reader);

            Debug.Assert(!typeRef.TypeName.IsNull(reader));

            name = typeRef.TypeName.GetConstantStringValue(reader).Value;
            enclosing = typeRef.ParentNamespaceOrType.HandleType == HandleType.TypeReference ? typeRef.ParentNamespaceOrType.GetFullName(reader) : null;
            nspace = typeRef.ParentNamespaceOrType.HandleType == HandleType.NamespaceReference ? typeRef.ParentNamespaceOrType.GetFullName(reader) : null;
        }

        public static string GetFullName(this TypeReferenceHandle typeRefHandle, MetadataReader reader)
        {
            string name;
            string enclosing;
            string nspace;
            typeRefHandle.GetFullName(reader, out name, out enclosing, out nspace);

            if (enclosing != null && name != null)
                return enclosing + "+" + name;
            else if (nspace != null && name != null)
                return nspace + "." + name;

            return name;
        }

        public static string GetContainingModuleName(this TypeReferenceHandle typeRefHandle, MetadataReader reader)
        {
            var typeRef = typeRefHandle.GetTypeReference(reader);

            Handle currentHandle = typeRef.ParentNamespaceOrType;
            Debug.Assert(!currentHandle.IsNull(reader));

            while (!currentHandle.IsNull(reader))
            {
                switch (currentHandle.HandleType)
                {
                    case HandleType.TypeReference:
                    case HandleType.NamespaceReference:
                        currentHandle = typeRef.ParentNamespaceOrType;
                        break;

                    case HandleType.ScopeReference:
                        return currentHandle.GetFullName(reader);

                    default:
                        return "?";
                }
            }

            return "?";
        }

        public static string GetFullName(this NamespaceReferenceHandle namespaceHandle, MetadataReader reader)
        {
            var nspace = namespaceHandle.GetNamespaceReference(reader);

            if (nspace.Name.IsNull(reader))
                return null;

            var name = nspace.Name.GetConstantStringValue(reader).Value;
            var containingNamespace = nspace.ParentScopeOrNamespace.IsNull(reader) ? null : nspace.ParentScopeOrNamespace.GetFullName(reader);

            if (containingNamespace != null)
                return containingNamespace + "." + name;

            return name;
        }

        public static string GetFullName(this ScopeDefinitionHandle scopeDefHandle, MetadataReader reader)
        {
            var scopeDef = scopeDefHandle.GetScopeDefinition(reader);

            Debug.Assert(!scopeDef.Name.IsNull(reader));

            var assemblyName = new AssemblyName
            {
                Name = scopeDef.Name.GetConstantStringValue(reader).Value,
                CultureName = scopeDef.Culture.IsNull(reader) ? null : scopeDef.Culture.GetConstantStringValue(reader).Value,
                Version = new Version(scopeDef.MajorVersion, scopeDef.MinorVersion, scopeDef.BuildNumber, scopeDef.RevisionNumber)
            };

            if (scopeDef.PublicKey.Count > 0)
            {
                var pkt = new byte[scopeDef.PublicKey.Count];
                int index = 0;
                foreach (var b in scopeDef.PublicKey)
                    pkt[index++] = b;
                assemblyName.SetPublicKeyToken(pkt);
            }
            else
            {
                assemblyName.SetPublicKeyToken(Array.Empty<byte>());
            }

            return assemblyName.FullName;
        }

        public static string GetFullName(this ScopeReferenceHandle scopeRefHandle, MetadataReader reader)
        {
            var scopeRef = scopeRefHandle.GetScopeReference(reader);

            Debug.Assert(!scopeRef.Name.IsNull(reader));

            var assemblyName = new AssemblyName
            {
                Name = scopeRef.Name.GetConstantStringValue(reader).Value,
                CultureName = scopeRef.Culture.IsNull(reader) ? null : scopeRef.Culture.GetConstantStringValue(reader).Value,
                Version = new Version(scopeRef.MajorVersion, scopeRef.MinorVersion, scopeRef.BuildNumber, scopeRef.RevisionNumber)
            };

            if (scopeRef.PublicKeyOrToken.Count > 0)
            {
                var pkt = new byte[scopeRef.PublicKeyOrToken.Count];
                int index = 0;
                foreach (var b in scopeRef.PublicKeyOrToken)
                    pkt[index++] = b;
                assemblyName.SetPublicKeyToken(pkt);
            }
            else
            {
                assemblyName.SetPublicKeyToken(Array.Empty<byte>());
            }

            return assemblyName.FullName;
        }
    }
}

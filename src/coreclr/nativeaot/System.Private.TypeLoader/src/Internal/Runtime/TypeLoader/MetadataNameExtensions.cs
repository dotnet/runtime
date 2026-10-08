// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::Internal.Metadata.NativeFormat;

using Debug = System.Diagnostics.Debug;

namespace Internal.Runtime.TypeLoader
{
    internal static class MetadataNameExtensions
    {
        private static string GetFullName(this Handle handle, MetadataReader reader)
        {
            return handle.HandleType switch
            {
                HandleType.TypeDefinition => handle.ToTypeDefinitionHandle(reader).GetFullName(reader),
                HandleType.NamespaceDefinition => handle.ToNamespaceDefinitionHandle(reader).GetFullName(reader),
                HandleType.ScopeDefinition => handle.ToScopeDefinitionHandle(reader).GetFullName(reader),
                _ => null,
            };
        }

        public static void GetFullName(this TypeDefinitionHandle typeDefHandle, MetadataReader reader, out string name, out string enclosing, out string nspace)
        {
            var typeDef = typeDefHandle.GetTypeDefinition(reader);

            Debug.Assert(!typeDef.Name.IsNil);

            name = typeDef.Name.GetConstantStringValue(reader).Value;
            Handle parent = typeDef.NamespaceOrEnclosingType;
            enclosing = parent.HandleType == HandleType.TypeDefinition ? parent.ToTypeDefinitionHandle(reader).GetFullName(reader) : null;
            nspace = parent.HandleType == HandleType.NamespaceDefinition ? parent.ToNamespaceDefinitionHandle(reader).GetFullName(reader) : null;
        }

        public static string GetFullName(this TypeDefinitionHandle typeDefHandle, MetadataReader reader)
        {
            string name;
            string enclosing;
            string nspace;
            typeDefHandle.GetFullName(reader, out name, out enclosing, out nspace);

            if (enclosing is not null)
                return enclosing + "+" + name;
            else if (nspace is not null)
                return nspace + "." + name;

            return name;
        }

        public static string GetContainingModuleName(this TypeDefinitionHandle typeDefHandle, MetadataReader reader)
        {
            var typeDef = typeDefHandle.GetTypeDefinition(reader);

            Handle currentHandle = typeDef.NamespaceOrEnclosingType;
            Debug.Assert(!currentHandle.IsNil);

            while (!currentHandle.IsNil)
            {
                switch (currentHandle.HandleType)
                {
                    case HandleType.TypeDefinition:
                        typeDef = currentHandle.ToTypeDefinitionHandle(reader).GetTypeDefinition(reader);
                        currentHandle = typeDef.NamespaceOrEnclosingType;
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

        private static string GetFullName(this NamespaceDefinitionHandle namespaceHandle, MetadataReader reader)
        {
            var nspace = namespaceHandle.GetNamespaceDefinition(reader);

            if (nspace.Name.IsNil)
                return null;

            var name = nspace.Name.GetConstantStringValue(reader).Value;
            var containingNamespace = nspace.ParentScopeOrNamespace.IsNil ? null : nspace.ParentScopeOrNamespace.GetFullName(reader);

            if (containingNamespace is not null)
                return containingNamespace + "." + name;

            return name;
        }

        private static string GetFullName(this ScopeDefinitionHandle scopeDefHandle, MetadataReader reader)
        {
            var scopeDef = scopeDefHandle.GetScopeDefinition(reader);
            return scopeDef.Name.GetConstantStringValue(reader).Value;
        }
    }
}

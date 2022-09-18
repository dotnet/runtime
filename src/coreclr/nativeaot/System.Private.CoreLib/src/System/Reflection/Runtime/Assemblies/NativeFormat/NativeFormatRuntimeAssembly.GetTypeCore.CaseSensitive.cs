// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;

using Internal.Reflection.Core;
using Internal.Metadata.NativeFormat;

namespace System.Reflection.Runtime.Assemblies.NativeFormat
{
    internal partial class NativeFormatRuntimeAssembly
    {
        internal sealed override RuntimeTypeInfo UncachedGetTypeCoreCaseSensitive(string fullName)
        {
            string[] parts = fullName.Split('.');
            int numNamespaceParts = parts.Length - 1;
            string[] namespaceParts = new string[numNamespaceParts];
            for (int i = 0; i < numNamespaceParts; i++)
                namespaceParts[numNamespaceParts - i - 1] = parts[i];
            string name = parts[numNamespaceParts];

            foreach (QScopeDefinition scopeDefinition in AllScopes)
            {
                MetadataReader reader = scopeDefinition.Reader;
                ScopeDefinitionHandle scopeDefinitionHandle = scopeDefinition.Handle;

                NamespaceDefinition namespaceDefinition;
                if (!TryResolveNamespaceDefinitionCaseSensitive(reader, namespaceParts, scopeDefinitionHandle, out namespaceDefinition))
                    continue;

                // We've successfully drilled down the namespace chain. Now look for a top-level type matching the type name.
                TypeDefinitionHandleCollection candidateTypes = namespaceDefinition.TypeDefinitions;
                foreach (TypeDefinitionHandle candidateType in candidateTypes)
                {
                    TypeDefinition typeDefinition = candidateType.GetTypeDefinition(reader);
                    if (typeDefinition.Name.StringEquals(name, reader))
                        return candidateType.ResolveTypeDefinition(reader);
                }

                // No match found in this assembly - see if there's a matching type forwarder.
                TypeForwarderHandleCollection candidateTypeForwarders = namespaceDefinition.TypeForwarders;
                foreach (TypeForwarderHandle typeForwarderHandle in candidateTypeForwarders)
                {
                    TypeForwarder typeForwarder = typeForwarderHandle.GetTypeForwarder(reader);
                    if (typeForwarder.Name.StringEquals(name, reader))
                    {
                        RuntimeAssemblyName redirectedAssemblyName = typeForwarder.Scope.ToRuntimeAssemblyName(reader);
                        RuntimeAssemblyInfo redirectedAssembly = RuntimeAssemblyInfo.GetRuntimeAssemblyIfExists(redirectedAssemblyName);
                        if (redirectedAssembly == null)
                            return null;
                        return redirectedAssembly.GetTypeCoreCaseSensitive(fullName);
                    }
                }
            }

            return null;
        }

        private static bool TryResolveNamespaceDefinitionCaseSensitive(MetadataReader reader, string[] namespaceParts, ScopeDefinitionHandle scopeDefinitionHandle, out NamespaceDefinition namespaceDefinition)
        {
            namespaceDefinition = scopeDefinitionHandle.GetScopeDefinition(reader).RootNamespaceDefinition.GetNamespaceDefinition(reader);
            NamespaceDefinitionHandleCollection candidates = namespaceDefinition.NamespaceDefinitions;
            int idx = namespaceParts.Length;
            while (idx-- != 0)
            {
                // Each iteration finds a match for one segment of the namespace chain.
                string expected = namespaceParts[idx];
                bool foundMatch = false;
                foreach (NamespaceDefinitionHandle candidate in candidates)
                {
                    namespaceDefinition = candidate.GetNamespaceDefinition(reader);
                    if (namespaceDefinition.Name.StringOrNullEquals(expected, reader))
                    {
                        // Found a match for this segment of the namespace chain. Move on to the next level.
                        foundMatch = true;
                        candidates = namespaceDefinition.NamespaceDefinitions;
                        break;
                    }
                }

                if (!foundMatch)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

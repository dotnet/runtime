// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;

using Internal;
using Internal.Metadata.NativeFormat;
using Internal.Reflection.Core;

namespace System.Reflection.Runtime.Assemblies.NativeFormat
{
    internal partial class NativeFormatRuntimeAssembly
    {
        internal sealed override RuntimeTypeInfo UncachedGetTypeCoreCaseSensitive(string fullName)
        {
            int separator = fullName.LastIndexOf('.');
            ReadOnlySpan<char> namespaceName = separator < 0 ? default : fullName.AsSpan(0, separator);
            ReadOnlySpan<char> name = fullName.AsSpan(separator + 1);
            int hashCode = VersionResilientHashCode.NameHashCode(
                VersionResilientHashCode.NameHashCode(namespaceName),
                VersionResilientHashCode.NameHashCode(name));

            MetadataReader reader = Scope.Reader;
            ScopeDefinition scopeDefinition = Scope.ScopeDefinition;

            foreach (TypeDefinitionHandle candidateType in scopeDefinition.TypeDefinitions.Lookup(hashCode))
            {
                TypeDefinition typeDefinition = candidateType.GetTypeDefinition(reader);
                if (typeDefinition.Name.StringEquals(name, reader))
                {
                    NamespaceDefinition namespaceDefinition = typeDefinition.NamespaceOrEnclosingType.ToNamespaceDefinitionHandle(reader).GetNamespaceDefinition(reader);
                    if (namespaceDefinition.Name.IsNil != (separator < 0))
                        continue;
                    if (separator < 0 || namespaceDefinition.Name.StringEquals(namespaceName, reader))
                        return candidateType.ResolveTypeDefinition(reader);
                }
            }

            // No match found in this assembly - see if there's a matching type forwarder.
            foreach (TypeForwarderHandle typeForwarderHandle in scopeDefinition.TypeForwarders.Lookup(hashCode))
            {
                TypeForwarder typeForwarder = typeForwarderHandle.GetTypeForwarder(reader);
                if (typeForwarder.Name.StringEquals(name, reader))
                {
                    NamespaceReference namespaceReference = typeForwarder.Namespace.GetNamespaceReference(reader);
                    if (namespaceReference.Name.IsNil != (separator < 0))
                        continue;
                    if (separator >= 0 && !namespaceReference.Name.StringEquals(namespaceName, reader))
                        continue;

                    RuntimeAssemblyName redirectedAssemblyName = namespaceReference.Scope.ToRuntimeAssemblyName(reader);
                    RuntimeAssemblyInfo redirectedAssembly = RuntimeAssemblyInfo.GetRuntimeAssemblyIfExists(redirectedAssemblyName);
                    if (redirectedAssembly is null)
                        return null;
                    return redirectedAssembly.GetTypeCoreCaseSensitive(fullName);
                }
            }

            return null;
        }
    }
}

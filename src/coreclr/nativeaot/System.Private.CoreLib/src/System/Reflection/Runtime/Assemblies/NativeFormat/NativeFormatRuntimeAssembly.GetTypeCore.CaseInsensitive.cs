// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;

using Internal.Metadata.NativeFormat;

namespace System.Reflection.Runtime.Assemblies.NativeFormat
{
    internal partial class NativeFormatRuntimeAssembly
    {
        internal sealed override RuntimeTypeInfo GetTypeCoreCaseInsensitive(string fullName)
        {
            Dictionary<string, QHandle> dict = CaseInsensitiveTypeDictionary;
            if (!dict.TryGetValue(fullName, out QHandle qualifiedHandle))
            {
                return null;
            }

            MetadataReader reader = qualifiedHandle.Reader;
            Handle typeDefOrForwarderHandle = qualifiedHandle.Handle;

            HandleType handleType = typeDefOrForwarderHandle.HandleType;
            switch (handleType)
            {
                case HandleType.TypeDefinition:
                    {
                        TypeDefinitionHandle typeDefinitionHandle = typeDefOrForwarderHandle.ToTypeDefinitionHandle(reader);
                        return typeDefinitionHandle.ResolveTypeDefinition(reader);
                    }
                case HandleType.TypeForwarder:
                    {
                        TypeForwarder typeForwarder = typeDefOrForwarderHandle.ToTypeForwarderHandle(reader).GetTypeForwarder(reader);
                        ScopeReferenceHandle destinationScope = typeForwarder.Scope;
                        RuntimeAssemblyName destinationAssemblyName = destinationScope.ToRuntimeAssemblyName(reader);
                        RuntimeAssemblyInfo destinationAssembly = RuntimeAssemblyInfo.GetRuntimeAssemblyIfExists(destinationAssemblyName);
                        if (destinationAssembly == null)
                            return null;
                        return destinationAssembly.GetTypeCoreCaseInsensitive(fullName);
                    }
                default:
                    throw new InvalidOperationException();
            }
        }

        private Dictionary<string, QHandle> CaseInsensitiveTypeDictionary
        {
            get
            {
                return _lazyCaseInsensitiveTypeDictionary ??= CreateCaseInsensitiveTypeDictionary();
            }
        }

        private Dictionary<string, QHandle> CreateCaseInsensitiveTypeDictionary()
        {
            //
            // Collect all of the *non-nested* types and type-forwards.
            //
            //   The keys are full typenames in lower-cased form.
            //   The value is a tuple containing either a TypeDefinitionHandle or TypeForwarderHandle and the associated Reader
            //      for that handle.
            //
            // We do not store nested types here. The container type is resolved and chosen first, then the nested type chosen from
            // that. If we chose the wrong container type and fail the match as a result, that's too bad. (The desktop CLR has the
            // same issue.)
            //

            Dictionary<string, QHandle> dict = new Dictionary<string, QHandle>(StringComparer.OrdinalIgnoreCase);

            MetadataReader reader = Scope.Reader;
            ScopeDefinition scopeDefinition = Scope.ScopeDefinition;
            IEnumerable<NamespaceDefinitionHandle> topLevelNamespaceHandles = new NamespaceDefinitionHandle[] { scopeDefinition.RootNamespaceDefinition };
            IEnumerable<NamespaceDefinitionHandle> allNamespaceHandles = reader.GetTransitiveNamespaces(topLevelNamespaceHandles);
            foreach (NamespaceDefinitionHandle namespaceHandle in allNamespaceHandles)
            {
                string ns = namespaceHandle.ToNamespaceName(reader);
                if (ns.Length != 0)
                    ns += ".";
                ns = ns.ToLowerInvariant();

                NamespaceDefinition namespaceDefinition = namespaceHandle.GetNamespaceDefinition(reader);
                foreach (TypeDefinitionHandle typeDefinitionHandle in namespaceDefinition.TypeDefinitions)
                {
                    string fullName = ns + typeDefinitionHandle.GetTypeDefinition(reader).Name.GetString(reader).ToLowerInvariant();
                    dict.TryAdd(fullName, new QHandle(reader, typeDefinitionHandle));
                }

                foreach (TypeForwarderHandle typeForwarderHandle in namespaceDefinition.TypeForwarders)
                {
                    string fullName = ns + typeForwarderHandle.GetTypeForwarder(reader).Name.GetString(reader).ToLowerInvariant();
                    dict.TryAdd(fullName, new QHandle(reader, typeForwarderHandle));
                }
            }

            return dict;
        }

        private volatile Dictionary<string, QHandle> _lazyCaseInsensitiveTypeDictionary;
    }
}

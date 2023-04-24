// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;

using Internal.Metadata.NativeFormat;
using Internal.Reflection.Core;

namespace System.Reflection.Runtime.Assemblies.NativeFormat
{
    internal partial class NativeFormatRuntimeAssembly
    {
        internal sealed override RuntimeTypeInfo GetTypeCoreCaseInsensitive(string fullName)
        {
            LowLevelDictionary<string, QHandle> dict = CaseInsensitiveTypeDictionary;
            QHandle qualifiedHandle;
            if (!dict.TryGetValue(fullName.ToLowerInvariant(), out qualifiedHandle))
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

        private LowLevelDictionary<string, QHandle> CaseInsensitiveTypeDictionary
        {
            get
            {
                return _lazyCaseInsensitiveTypeDictionary ??= CreateCaseInsensitiveTypeDictionary();
            }
        }

        private LowLevelDictionary<string, QHandle> CreateCaseInsensitiveTypeDictionary()
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

            LowLevelDictionary<string, QHandle> dict = new LowLevelDictionary<string, QHandle>();

            foreach (QScopeDefinition scope in AllScopes)
            {
                MetadataReader reader = scope.Reader;
                ScopeDefinition scopeDefinition = scope.ScopeDefinition;
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
                        if (!dict.TryGetValue(fullName, out _))
                        {
                            dict.Add(fullName, new QHandle(reader, typeDefinitionHandle));
                        }
                    }

                    foreach (TypeForwarderHandle typeForwarderHandle in namespaceDefinition.TypeForwarders)
                    {
                        string fullName = ns + typeForwarderHandle.GetTypeForwarder(reader).Name.GetString(reader).ToLowerInvariant();
                        if (!dict.TryGetValue(fullName, out _))
                        {
                            dict.Add(fullName, new QHandle(reader, typeForwarderHandle));
                        }
                    }
                }
            }

            return dict;
        }

        private volatile LowLevelDictionary<string, QHandle> _lazyCaseInsensitiveTypeDictionary;
    }
}

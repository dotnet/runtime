// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.Metadata
{
    internal partial class Transform<TPolicy>
    {
        private Dictionary<NamespaceKey, NamespaceDefinition> _namespaceDefs = new Dictionary<NamespaceKey, NamespaceDefinition>();

        private NamespaceDefinition HandleNamespaceDefinition(Cts.ModuleDesc parentScope, string namespaceString)
        {
            Debug.Assert(namespaceString != null);

            NamespaceDefinition result;
            NamespaceKey key = new NamespaceKey(parentScope, namespaceString);
            if (_namespaceDefs.TryGetValue(key, out result))
            {
                return result;
            }

            if (namespaceString.Length == 0)
            {
                var rootNamespace = new NamespaceDefinition
                {
                    Name = null,
                };
                _namespaceDefs.Add(key, rootNamespace);
                ScopeDefinition rootScope = HandleScopeDefinition(parentScope);
                rootScope.RootNamespaceDefinition = rootNamespace;
                rootNamespace.ParentScopeOrNamespace = rootScope;
                return rootNamespace;
            }

            string currentNamespaceName = string.Empty;
            NamespaceDefinition currentNamespace = HandleNamespaceDefinition(parentScope, currentNamespaceName);
            foreach (var segment in namespaceString.Split('.'))
            {
                string nextNamespaceName = currentNamespaceName;
                if (nextNamespaceName.Length > 0)
                    nextNamespaceName += '.';
                nextNamespaceName += segment;
                NamespaceDefinition nextNamespace;
                key = new NamespaceKey(parentScope, nextNamespaceName);
                if (!_namespaceDefs.TryGetValue(key, out nextNamespace))
                {
                    nextNamespace = new NamespaceDefinition
                    {
                        Name = HandleString(segment.Length == 0 ? null : segment),
                        ParentScopeOrNamespace = currentNamespace
                    };

                    _namespaceDefs.Add(key, nextNamespace);
                    currentNamespace.NamespaceDefinitions.Add(nextNamespace);
                }
                currentNamespace = nextNamespace;
                currentNamespaceName = nextNamespaceName;
            }

            return currentNamespace;
        }

        private Dictionary<NamespaceKey, NamespaceReference> _namespaceRefs = new Dictionary<NamespaceKey, NamespaceReference>();

        private NamespaceReference HandleNamespaceReference(Cts.ModuleDesc parentScope, string namespaceString)
        {
            // The format represents root namespace as a namespace with null name, in contrast with ECMA-335
            if (namespaceString.Length == 0)
                namespaceString = null;

            NamespaceReference result;
            NamespaceKey key = new NamespaceKey(parentScope, namespaceString);
            if (_namespaceRefs.TryGetValue(key, out result))
            {
                return result;
            }

            ScopeReference scope = HandleScopeReference(parentScope);
            NamespaceReference rootNamespace;
            key = new NamespaceKey(parentScope, null);
            if (!_namespaceRefs.TryGetValue(key, out rootNamespace))
            {
                rootNamespace = new NamespaceReference
                {
                    Name = null,
                    ParentScopeOrNamespace = scope,
                };
                _namespaceRefs.Add(key, rootNamespace);
            }

            if (namespaceString == null)
                return rootNamespace;

            NamespaceReference currentNamespace = rootNamespace;
            string currentNamespaceName = string.Empty;
            foreach (var segment in namespaceString.Split('.'))
            {
                string nextNamespaceName = currentNamespaceName;
                if (nextNamespaceName.Length > 0)
                    nextNamespaceName += '.';
                nextNamespaceName += segment;
                NamespaceReference nextNamespace;
                key = new NamespaceKey(parentScope, nextNamespaceName);
                if (!_namespaceRefs.TryGetValue(key, out nextNamespace))
                {
                    nextNamespace = new NamespaceReference
                    {
                        Name = HandleString(segment.Length == 0 ? null : segment),
                        ParentScopeOrNamespace = currentNamespace
                    };

                    _namespaceRefs.Add(key, nextNamespace);
                }
                currentNamespace = nextNamespace;
                currentNamespaceName = nextNamespaceName;
            }

            return currentNamespace;
        }
    }

    internal struct NamespaceKey : IEquatable<NamespaceKey>
    {
        public readonly Cts.ModuleDesc Module;
        public readonly string Namespace;

        public NamespaceKey(Cts.ModuleDesc module, string namespaceName)
        {
            Module = module;
            Namespace = namespaceName;
        }

        public bool Equals(NamespaceKey other)
        {
            return Module == other.Module && Namespace == other.Namespace;
        }

        public override bool Equals(object obj)
        {
            if (obj is NamespaceKey)
                return Equals((NamespaceKey)obj);
            return false;
        }

        public override int GetHashCode()
        {
            return Namespace != null ? Namespace.GetHashCode() : 0;
        }
    }
}

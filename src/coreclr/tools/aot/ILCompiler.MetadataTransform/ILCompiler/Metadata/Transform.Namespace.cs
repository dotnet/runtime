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
            Debug.Assert(namespaceString is not null);

            NamespaceDefinition result;
            NamespaceKey key = new NamespaceKey(parentScope, namespaceString);
            if (_namespaceDefs.TryGetValue(key, out result))
            {
                return result;
            }

            result = new NamespaceDefinition
            {
                Name = HandleString(namespaceString.Length == 0 ? null : namespaceString),
            };
            _namespaceDefs.Add(key, result);
            result.Scope = HandleScopeDefinition(parentScope);

            return result;
        }

        private Dictionary<NamespaceKey, NamespaceReference> _namespaceRefs = new Dictionary<NamespaceKey, NamespaceReference>();

        private NamespaceReference HandleNamespaceReference(Cts.ModuleDesc parentScope, string namespaceString)
        {
            NamespaceKey key = new NamespaceKey(parentScope, namespaceString);
            if (_namespaceRefs.TryGetValue(key, out NamespaceReference result))
            {
                return result;
            }

            result = new NamespaceReference
            {
                Scope = HandleScopeReference(parentScope),
                Name = HandleString(namespaceString.Length == 0 ? null : namespaceString),
            };
            _namespaceRefs.Add(key, result);

            return result;
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

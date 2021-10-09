// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace System.Text.Json.Reflection
{
    internal class AssemblyWrapper : Assembly
    {
        private readonly MetadataLoadContextInternal _metadataLoadContext;

        public AssemblyWrapper(IAssemblySymbol assembly, MetadataLoadContextInternal metadataLoadContext)
        {
            Symbol = assembly;
            _metadataLoadContext = metadataLoadContext;
        }

        internal IAssemblySymbol Symbol { get; }

        public override string FullName => Symbol.Identity.Name;

        public override Type[] GetExportedTypes()
        {
            return GetTypes();
        }

        public override Type[] GetTypes()
        {
            var types = new List<Type>();
            var stack = new Stack<INamespaceSymbol>();
            stack.Push(Symbol.GlobalNamespace);
            while (stack.Count > 0)
            {
                INamespaceSymbol current = stack.Pop();

                foreach (INamedTypeSymbol type in current.GetTypeMembers())
                {
                    types.Add(type.AsType(_metadataLoadContext));
                }

                foreach (INamespaceSymbol ns in current.GetNamespaceMembers())
                {
                    stack.Push(ns);
                }
            }
            return types.ToArray();
        }

        public override Type GetType(string name)
        {
            return Symbol.GetTypeByMetadataName(name)!.AsType(_metadataLoadContext);
        }
    }
}

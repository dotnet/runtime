// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335
{
    internal sealed class NamespaceData
    {
        public readonly StringHandle Name;
        public readonly string FullName;
        public readonly NamespaceDefinitionHandle Parent;
        public readonly ImmutableArray<NamespaceDefinitionHandle> NamespaceDefinitions;
        public readonly ImmutableArray<TypeDefinitionHandle> TypeDefinitions;
        public readonly ImmutableArray<ExportedTypeHandle> ExportedTypes;

        public NamespaceData(
            StringHandle name,
            string fullName,
            NamespaceDefinitionHandle parent,
            ImmutableArray<NamespaceDefinitionHandle> namespaceDefinitions,
            ImmutableArray<TypeDefinitionHandle> typeDefinitions,
            ImmutableArray<ExportedTypeHandle> exportedTypes)
        {
            this.Name = name;
            this.FullName = fullName;
            this.Parent = parent;
            this.NamespaceDefinitions = namespaceDefinitions;
            this.TypeDefinitions = typeDefinitions;
            this.ExportedTypes = exportedTypes;
        }
    }
}

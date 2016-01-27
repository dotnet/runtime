// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public struct NamespaceDefinition
    {
        private NamespaceData _data;

        internal NamespaceDefinition(NamespaceData data)
        {
            Debug.Assert(data != null);
            _data = data;
        }

        /// <summary>
        /// Gets the unqualified name of the NamespaceDefinition.
        /// </summary>
        public StringHandle Name
        {
            get { return _data.Name; }
        }

        /// <summary>
        /// Gets the parent namespace.
        /// </summary>
        public NamespaceDefinitionHandle Parent
        {
            get { return _data.Parent; }
        }

        /// <summary>
        /// Gets the namespace definitions that are direct children of the current
        /// namespace definition. 
        /// 
        /// System.Collections and System.Linq are direct children of System. 
        /// System.Collections.Generic is a direct child of System.Collections.
        /// System.Collections.Generic is *not* a direct child of System.
        /// </summary>
        public ImmutableArray<NamespaceDefinitionHandle> NamespaceDefinitions
        {
            get { return _data.NamespaceDefinitions; }
        }

        /// <summary>
        /// Gets all type definitions that reside directly in a namespace.
        /// </summary>
        public ImmutableArray<TypeDefinitionHandle> TypeDefinitions
        {
            get { return _data.TypeDefinitions; }
        }

        /// <summary>
        /// Gets all exported types that reside directly in a namespace.
        /// </summary>
        public ImmutableArray<ExportedTypeHandle> ExportedTypes
        {
            get { return _data.ExportedTypes; }
        }
    }
}

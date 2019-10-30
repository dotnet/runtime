// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public readonly struct ImportDefinition
    {
        public ImportDefinitionKind Kind { get; }
        public BlobHandle Alias { get; }
        public AssemblyReferenceHandle TargetAssembly { get; }

        public BlobHandle TargetNamespace => (BlobHandle)_typeOrNamespace;
        public EntityHandle TargetType => (EntityHandle)_typeOrNamespace;
        private readonly Handle _typeOrNamespace;

        internal ImportDefinition(
            ImportDefinitionKind kind,
            BlobHandle alias = default(BlobHandle),
            AssemblyReferenceHandle assembly = default(AssemblyReferenceHandle),
            Handle typeOrNamespace = default(Handle))
        {
            Debug.Assert(
                typeOrNamespace.IsNil ||
                typeOrNamespace.Kind == HandleKind.Blob ||
                typeOrNamespace.Kind == HandleKind.TypeDefinition ||
                typeOrNamespace.Kind == HandleKind.TypeReference ||
                typeOrNamespace.Kind == HandleKind.TypeSpecification);

            Kind = kind;
            Alias = alias;
            TargetAssembly = assembly;
            _typeOrNamespace = typeOrNamespace;
        }
    }
}

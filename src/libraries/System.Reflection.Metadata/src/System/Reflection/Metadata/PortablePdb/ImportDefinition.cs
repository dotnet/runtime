// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct ImportDefinition
    {
        private readonly ImportDefinitionKind _kind;
        private readonly BlobHandle _alias;
        private readonly AssemblyReferenceHandle _assembly;
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

            _kind = kind;
            _alias = alias;
            _assembly = assembly;
            _typeOrNamespace = typeOrNamespace;
        }

        public ImportDefinitionKind Kind { get { return _kind; } }
        public BlobHandle Alias { get { return _alias; } }
        public AssemblyReferenceHandle TargetAssembly { get { return _assembly; } }
        public BlobHandle TargetNamespace { get { return (BlobHandle)_typeOrNamespace; } }
        public EntityHandle TargetType { get { return (EntityHandle)_typeOrNamespace; } }
    }
}

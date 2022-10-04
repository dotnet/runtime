// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Internal.TypeSystem.Ecma
{
    // Pluggable file that adds PDB handling functionality to EcmaAssembly
    public partial class EcmaAssembly
    {
        internal EcmaAssembly(TypeSystemContext context, PEReader peReader, MetadataReader metadataReader, PdbSymbolReader pdbReader, IModuleResolver customModuleResolver)
            : base(context, peReader, metadataReader, containingAssembly: null, pdbReader, customModuleResolver)
        {
            _assemblyDefinition = metadataReader.GetAssemblyDefinition();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Internal.TypeSystem.Ecma
{
    // Pluggable file that adds PDB handling functionality to EcmaAssembly
    partial class EcmaAssembly
    {
        internal EcmaAssembly(TypeSystemContext context, PEReader peReader, MetadataReader metadataReader, PdbSymbolReader pdbReader)
            : base(context, peReader, metadataReader, containingAssembly: null, pdbReader)
        {
            _assemblyDefinition = metadataReader.GetAssemblyDefinition();
        }
    }
}

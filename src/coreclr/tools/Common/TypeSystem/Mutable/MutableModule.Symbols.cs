// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Internal.TypeSystem.Ecma
{
    // Pluggable file that adds PDB handling functionality to EcmaModule
    partial class MutableModule
    {
        public PdbSymbolReader PdbReader => null;
    }
}

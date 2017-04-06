// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // AssemblyQualifiedNamespaceSymbol
    //
    // Parented by an NamespaceSymbol. Represents an NamespaceSymbol within an aid (assembly/alias id).
    // The name is a form of the aid.
    // ----------------------------------------------------------------------------

    internal sealed class AssemblyQualifiedNamespaceSymbol : ParentSymbol
    {
        public NamespaceSymbol GetNS()
        {
            return parent.AsNamespaceSymbol();
        }
    }
}

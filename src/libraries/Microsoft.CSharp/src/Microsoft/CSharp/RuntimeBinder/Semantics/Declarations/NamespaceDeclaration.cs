// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // NamespaceDeclaration
    //
    // NamespaceDeclaration - a symbol representing a declaration
    // of a namspace in the source. 
    //
    // firstChild/firstChild->nextChild enumerates the 
    // NSDECLs and AGGDECLs declared within this declaration.
    //
    // parent is the containing namespace declaration.
    //
    // Bag() is the namespace corresponding to this declaration.
    //
    // DeclNext() is the next declaration for the same namespace.
    // ----------------------------------------------------------------------------

    internal sealed class NamespaceDeclaration : Declaration
    {
        public NamespaceSymbol Bag()
        {
            return bag.AsNamespaceSymbol();
        }

        public NamespaceSymbol NameSpace()
        {
            return bag.AsNamespaceSymbol();
        }
    }
}

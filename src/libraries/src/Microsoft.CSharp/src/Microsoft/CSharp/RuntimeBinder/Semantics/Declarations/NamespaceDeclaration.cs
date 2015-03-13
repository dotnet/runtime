// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    internal class NamespaceDeclaration : Declaration
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

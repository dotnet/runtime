// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // Declaration
    //
    // Base class for NamespaceDeclaration and AggregateDeclaration. Parent is another DECL.
    // Children are DECLs.
    // ----------------------------------------------------------------------------

    internal class Declaration : ParentSymbol
    {
        public NamespaceOrAggregateSymbol bag;
        public Declaration declNext;
    }
}

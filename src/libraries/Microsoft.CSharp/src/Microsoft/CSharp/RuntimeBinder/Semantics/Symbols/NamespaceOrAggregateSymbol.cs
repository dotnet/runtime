// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // NamespaceOrAggregateSymbol
    //
    // Base class for NamespaceSymbol and AggregateSymbol. Bags have DECLSYMs.
    // Parent is another BAG. Children are other BAGs, members, type vars, etc.
    // ----------------------------------------------------------------------------

    internal abstract class NamespaceOrAggregateSymbol : ParentSymbol
    {
        private AggregateDeclaration _declFirst;
        private AggregateDeclaration _declLast;

        // ----------------------------------------------------------------------------
        // NamespaceOrAggregateSymbol
        // ----------------------------------------------------------------------------

        // Compare to ParentSymbol::AddToChildList
        public void AddDecl(AggregateDeclaration decl)
        {
            Debug.Assert(decl != null);
            Debug.Assert(this is AggregateSymbol);
            Debug.Assert(decl is AggregateDeclaration);

            // If parent is set it should be set to us!
            Debug.Assert(decl.bag == null || decl.bag == this);
            // There shouldn't be a declNext.
            Debug.Assert(decl.declNext == null);

            if (_declLast == null)
            {
                Debug.Assert(_declFirst == null);
                _declFirst = _declLast = decl;
            }
            else
            {
                _declLast.declNext = decl;
                _declLast = decl;

#if DEBUG
                // Validate our chain.
                AggregateDeclaration pdecl;
                for (pdecl = _declFirst; pdecl?.declNext != null; pdecl = pdecl.declNext)
                { }
                Debug.Assert(pdecl == null || (pdecl == _declLast && pdecl.declNext == null));
#endif
            }

            decl.declNext = null;
            decl.bag = this;
        }
    }
}

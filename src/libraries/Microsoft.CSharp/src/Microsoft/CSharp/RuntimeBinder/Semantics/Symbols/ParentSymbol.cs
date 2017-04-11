// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // ParentSymbol - a symbol that can contain other symbols as children.
    //
    // ----------------------------------------------------------------------------

    internal abstract class ParentSymbol : Symbol
    {
        public Symbol firstChild;       // List of all children of this symbol
        private Symbol _lastChild;

        // This adds the sym to the child list but doesn't associate it
        // in the symbol table.

        public void AddToChildList(Symbol sym)
        {
            Debug.Assert(sym != null /*&& this != null */);

            // If parent is set it should be set to us!
            Debug.Assert(sym.parent == null || sym.parent == this);
            // There shouldn't be a nextChild.
            Debug.Assert(sym.nextChild == null);

            if (_lastChild == null)
            {
                Debug.Assert(firstChild == null);
                firstChild = _lastChild = sym;
            }
            else
            {
                _lastChild.nextChild = sym;
                _lastChild = sym;
                sym.nextChild = null;

#if DEBUG
                // Validate our chain.
                Symbol psym;
                int count = 400; // Limited the length of chain that we'll run - so debug perf isn't too bad.
                for (psym = this.firstChild; psym?.nextChild != null && --count > 0;)
                    psym = psym.nextChild;
                Debug.Assert(_lastChild == psym || count == 0);
#endif
            }

            sym.parent = this;
        }
    }
}

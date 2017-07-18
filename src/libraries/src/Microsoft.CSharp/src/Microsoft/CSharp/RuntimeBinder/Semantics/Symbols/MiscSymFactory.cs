// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class MiscSymFactory : SymFactoryBase
    {
        // Constructor.

        public MiscSymFactory(SYMTBL symtable)
            : base(symtable, false)
        {
        }

        public Scope CreateScope(Scope parent)
        {
            Scope sym = newBasicSym(SYMKIND.SK_Scope, null, parent).AsScope();
            if (parent != null)
            {
                sym.nestingOrder = parent.nestingOrder + 1;
            }

            return sym;
        }

        public IndexerSymbol CreateIndexer(Name name, ParentSymbol parent, Name realName)
        {
            IndexerSymbol sym = (IndexerSymbol)newBasicSym(SYMKIND.SK_IndexerSymbol, name, parent);
            sym.setKind(SYMKIND.SK_PropertySymbol);
            sym.isOperator = true;

            Debug.Assert(sym != null);
            return sym;
        }
    }
}

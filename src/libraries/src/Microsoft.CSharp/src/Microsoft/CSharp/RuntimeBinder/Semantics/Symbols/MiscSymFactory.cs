// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;
using mdAssemblyRef = Microsoft.CSharp.RuntimeBinder.Semantics.mdToken;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class MiscSymFactory : SymFactoryBase
    {
        // Constructor.

        public MiscSymFactory(SYMTBL symtable)
            : base(symtable, null)
        {
        }

        // Files
        public InputFile CreateMDInfile(Name name, mdAssemblyRef idLocalAssembly)
        {
            InputFile sym = new InputFile();
            sym.isSource = false;
            return sym;
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

        public IndexerSymbol CreateIndexer(Name name, ParentSymbol parent, Name realName, AggregateDeclaration declaration)
        {
            IndexerSymbol sym = (IndexerSymbol)newBasicSym(SYMKIND.SK_IndexerSymbol, name, parent);
            sym.setKind(SYMKIND.SK_PropertySymbol);
            sym.isOperator = true;
            sym.declaration = declaration;

            Debug.Assert(sym != null);
            return sym;
        }
    }
}
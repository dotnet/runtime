// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // This class is the base class for the different symbol factories: the
    // SymFactory, TypeFactory and MiscSymFactory. This provides a common
    // way of creating syms that all three classes use.

    internal abstract class SymFactoryBase
    {
        // Members.
        private readonly SYMTBL m_pSymTable;
        protected Symbol newBasicSym(
            SYMKIND kind,
            Name name,
            ParentSymbol parent)
        {
            Symbol sym;
            switch (kind)
            {
                case SYMKIND.SK_NamespaceSymbol:
                    sym = new NamespaceSymbol();
                    sym.name = name;
                    break;
                case SYMKIND.SK_AssemblyQualifiedNamespaceSymbol:
                    sym = new AssemblyQualifiedNamespaceSymbol();
                    sym.name = name;
                    break;
                case SYMKIND.SK_AggregateSymbol:
                    sym = new AggregateSymbol();
                    sym.name = name;
                    break;
                case SYMKIND.SK_AggregateDeclaration:
                    sym = new AggregateDeclaration();
                    sym.name = name;
                    break;
                case SYMKIND.SK_TypeParameterSymbol:
                    sym = new TypeParameterSymbol();
                    sym.name = name;
                    break;
                case SYMKIND.SK_FieldSymbol:
                    sym = new FieldSymbol();
                    sym.name = name;
                    break;
                case SYMKIND.SK_LocalVariableSymbol:
                    sym = new LocalVariableSymbol();
                    sym.name = name;
                    break;
                case SYMKIND.SK_MethodSymbol:
                    sym = new MethodSymbol();
                    sym.name = name;
                    break;
                case SYMKIND.SK_PropertySymbol:
                    sym = new PropertySymbol();
                    sym.name = name;
                    break;
                case SYMKIND.SK_EventSymbol:
                    sym = new EventSymbol();
                    sym.name = name;
                    break;
                case SYMKIND.SK_Scope:
                    sym = new Scope();
                    sym.name = name;
                    break;
                case SYMKIND.SK_IndexerSymbol:
                    sym = new IndexerSymbol();
                    sym.name = name;
                    break;
                default:
                    throw Error.InternalCompilerError();
            }

            sym.setKind(kind);

            if (parent != null)
            {
                // Set the parent element of the child symbol.
                parent.AddToChildList(sym);
                m_pSymTable.InsertChild(parent, sym);
            }

            return (sym);
        }

        // This class should never be created on its own.
        protected SymFactoryBase(SYMTBL symtable)
        {
            m_pSymTable = symtable;
        }
    }
}

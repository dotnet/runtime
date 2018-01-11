// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class SymFactory
    {
        private readonly SYMTBL _symbolTable;

        public SymFactory(SYMTBL symtable)
        {
            _symbolTable = symtable;
        }

        private Symbol NewBasicSymbol(
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
                _symbolTable.InsertChild(parent, sym);
            }

            return (sym);
        }

        // Namespace
        public NamespaceSymbol CreateNamespace(Name name, NamespaceSymbol parent)
        {
            NamespaceSymbol sym = (NamespaceSymbol)NewBasicSymbol(SYMKIND.SK_NamespaceSymbol, name, parent);
            sym.SetAccess(ACCESS.ACC_PUBLIC);

            return (sym);
        }

        /////////////////////////////////////////////////////////////////////////////////
        public AggregateSymbol CreateAggregate(Name name, NamespaceOrAggregateSymbol parent, TypeManager typeManager)
        {
            if (name == null || parent == null || typeManager == null)
            {
                throw Error.InternalCompilerError();
            }

            AggregateSymbol sym = (AggregateSymbol)NewBasicSymbol(SYMKIND.SK_AggregateSymbol, name, parent);
            sym.name = name;
            sym.SetTypeManager(typeManager);
            sym.SetSealed(false);
            sym.SetAccess(ACCESS.ACC_UNKNOWN);
            sym.SetIfaces(null);
            sym.SetIfacesAll(null);
            sym.SetTypeVars(null);

            return sym;
        }

        public AggregateDeclaration CreateAggregateDecl(AggregateSymbol agg, AggregateDeclaration declOuter)
        {
            Debug.Assert(agg != null);
            //Debug.Assert(declOuter == null || declOuter.Bag() == agg.Parent);

            // DECLSYMs are not parented like named symbols.
            AggregateDeclaration sym = NewBasicSymbol(SYMKIND.SK_AggregateDeclaration, agg.name, null) as AggregateDeclaration;

            declOuter?.AddToChildList(sym);
            agg.AddDecl(sym);

            Debug.Assert(sym != null);
            return (sym);
        }

        // Members of aggs
        public FieldSymbol CreateMemberVar(Name name, AggregateSymbol parent)
        {
            Debug.Assert(name != null);

            FieldSymbol sym = NewBasicSymbol(SYMKIND.SK_FieldSymbol, name, parent) as FieldSymbol;

            Debug.Assert(sym != null);
            return (sym);
        }

        public LocalVariableSymbol CreateLocalVar(Name name, Scope parent, CType type)
        {
            LocalVariableSymbol sym = (LocalVariableSymbol)NewBasicSymbol(SYMKIND.SK_LocalVariableSymbol, name, parent);
            sym.SetType(type);
            sym.SetAccess(ACCESS.ACC_UNKNOWN);    // required for Symbol::hasExternalAccess which is used by refactoring
            sym.wrap = null;

            return sym;
        }

        public MethodSymbol CreateMethod(Name name, AggregateSymbol parent) => 
            NewBasicSymbol(SYMKIND.SK_MethodSymbol, name, parent) as MethodSymbol;

        public PropertySymbol CreateProperty(Name name, AggregateSymbol parent)
        {
            PropertySymbol sym = NewBasicSymbol(SYMKIND.SK_PropertySymbol, name, parent) as PropertySymbol;
            Debug.Assert(sym != null);
            return (sym);
        }

        public EventSymbol CreateEvent(Name name, AggregateSymbol parent)
        {
            EventSymbol sym = NewBasicSymbol(SYMKIND.SK_EventSymbol, name, parent) as EventSymbol;

            Debug.Assert(sym != null);
            return (sym);
        }

        public TypeParameterSymbol CreateMethodTypeParameter(Name pName, MethodSymbol pParent, int index, int indexTotal)
        {
            TypeParameterSymbol pResult = (TypeParameterSymbol)NewBasicSymbol(SYMKIND.SK_TypeParameterSymbol, pName, pParent);
            pResult.SetIndexInOwnParameters(index);
            pResult.SetIndexInTotalParameters(indexTotal);

            pResult.SetIsMethodTypeParameter(true);
            pResult.SetAccess(ACCESS.ACC_PRIVATE); // Always private - not accessible anywhere except their own type.

            return pResult;
        }

        public TypeParameterSymbol CreateClassTypeParameter(Name pName, AggregateSymbol pParent, int index, int indexTotal)
        {
            TypeParameterSymbol pResult = (TypeParameterSymbol)NewBasicSymbol(SYMKIND.SK_TypeParameterSymbol, pName, pParent);
            pResult.SetIndexInOwnParameters(index);
            pResult.SetIndexInTotalParameters(indexTotal);

            pResult.SetIsMethodTypeParameter(false);
            pResult.SetAccess(ACCESS.ACC_PRIVATE); // Always private - not accessible anywhere except their own type.

            return pResult;
        }

        public Scope CreateScope() => (Scope)NewBasicSymbol(SYMKIND.SK_Scope, null, null);

        public IndexerSymbol CreateIndexer(Name name, ParentSymbol parent, Name realName)
        {
            IndexerSymbol sym = (IndexerSymbol)NewBasicSymbol(SYMKIND.SK_IndexerSymbol, name, parent);
            sym.setKind(SYMKIND.SK_PropertySymbol);
            sym.isOperator = true;

            Debug.Assert(sym != null);
            return sym;
        }
    }
}

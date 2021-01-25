// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal static class SymFactory
    {
        private static Symbol NewBasicSymbol(
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
                SymbolStore.InsertChild(parent, sym);
            }

            return sym;
        }

        // Namespace
        public static NamespaceSymbol CreateNamespace(Name name, NamespaceSymbol parent)
        {
            NamespaceSymbol sym = (NamespaceSymbol)NewBasicSymbol(SYMKIND.SK_NamespaceSymbol, name, parent);
            sym.SetAccess(ACCESS.ACC_PUBLIC);

            return sym;
        }

        /////////////////////////////////////////////////////////////////////////////////
        public static AggregateSymbol CreateAggregate(Name name, NamespaceOrAggregateSymbol parent)
        {
            Debug.Assert(name != null);
            Debug.Assert(parent != null);

            AggregateSymbol sym = (AggregateSymbol)NewBasicSymbol(SYMKIND.SK_AggregateSymbol, name, parent);
            sym.name = name;
            sym.SetSealed(false);
            sym.SetAccess(ACCESS.ACC_UNKNOWN);
            sym.SetIfaces(null);
            sym.SetIfacesAll(null);
            sym.SetTypeVars(null);

            return sym;
        }

        // Members of aggs
        public static FieldSymbol CreateMemberVar(Name name, AggregateSymbol parent)
        {
            Debug.Assert(name != null);

            FieldSymbol sym = NewBasicSymbol(SYMKIND.SK_FieldSymbol, name, parent) as FieldSymbol;

            Debug.Assert(sym != null);
            return sym;
        }

        public static LocalVariableSymbol CreateLocalVar(Name name, Scope parent, CType type)
        {
            LocalVariableSymbol sym = (LocalVariableSymbol)NewBasicSymbol(SYMKIND.SK_LocalVariableSymbol, name, parent);
            sym.SetType(type);
            sym.SetAccess(ACCESS.ACC_UNKNOWN);    // required for Symbol::hasExternalAccess which is used by refactoring
            sym.wrap = null;

            return sym;
        }

        public static MethodSymbol CreateMethod(Name name, AggregateSymbol parent) =>
            NewBasicSymbol(SYMKIND.SK_MethodSymbol, name, parent) as MethodSymbol;

        public static PropertySymbol CreateProperty(Name name, AggregateSymbol parent)
        {
            PropertySymbol sym = NewBasicSymbol(SYMKIND.SK_PropertySymbol, name, parent) as PropertySymbol;
            Debug.Assert(sym != null);
            return sym;
        }

        public static EventSymbol CreateEvent(Name name, AggregateSymbol parent)
        {
            EventSymbol sym = NewBasicSymbol(SYMKIND.SK_EventSymbol, name, parent) as EventSymbol;

            Debug.Assert(sym != null);
            return sym;
        }

        public static TypeParameterSymbol CreateMethodTypeParameter(Name pName, MethodSymbol pParent, int index, int indexTotal)
        {
            TypeParameterSymbol pResult = (TypeParameterSymbol)NewBasicSymbol(SYMKIND.SK_TypeParameterSymbol, pName, pParent);
            pResult.SetIndexInOwnParameters(index);
            pResult.SetIndexInTotalParameters(indexTotal);

            pResult.SetIsMethodTypeParameter(true);
            pResult.SetAccess(ACCESS.ACC_PRIVATE); // Always private - not accessible anywhere except their own type.

            return pResult;
        }

        public static TypeParameterSymbol CreateClassTypeParameter(Name pName, AggregateSymbol pParent, int index, int indexTotal)
        {
            TypeParameterSymbol pResult = (TypeParameterSymbol)NewBasicSymbol(SYMKIND.SK_TypeParameterSymbol, pName, pParent);
            pResult.SetIndexInOwnParameters(index);
            pResult.SetIndexInTotalParameters(indexTotal);

            pResult.SetIsMethodTypeParameter(false);
            pResult.SetAccess(ACCESS.ACC_PRIVATE); // Always private - not accessible anywhere except their own type.

            return pResult;
        }

        public static Scope CreateScope() => (Scope)NewBasicSymbol(SYMKIND.SK_Scope, null, null);

        public static IndexerSymbol CreateIndexer(Name name, ParentSymbol parent)
        {
            IndexerSymbol sym = (IndexerSymbol)NewBasicSymbol(SYMKIND.SK_IndexerSymbol, name, parent);
            sym.setKind(SYMKIND.SK_PropertySymbol);
            sym.isOperator = true;

            Debug.Assert(sym != null);
            return sym;
        }
    }
}

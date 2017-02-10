// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class SymFactory : SymFactoryBase
    {
        public SymFactory(
            SYMTBL symtable,
            NameManager namemgr) :
            base(symtable, namemgr)
        {
        }

        // Namespace
        public NamespaceSymbol CreateNamespace(Name name, NamespaceSymbol parent)
        {
            NamespaceSymbol sym = newBasicSym(SYMKIND.SK_NamespaceSymbol, name, parent).AsNamespaceSymbol();
            sym.SetAccess(ACCESS.ACC_PUBLIC);

            return (sym);
        }

        public AssemblyQualifiedNamespaceSymbol CreateNamespaceAid(Name name, ParentSymbol parent, KAID assemblyID)
        {
            Debug.Assert(name != null);

            AssemblyQualifiedNamespaceSymbol sym = newBasicSym(SYMKIND.SK_AssemblyQualifiedNamespaceSymbol, name, parent).AsAssemblyQualifiedNamespaceSymbol();

            Debug.Assert(sym != null);
            return sym;
        }

        /////////////////////////////////////////////////////////////////////////////////
        public AggregateSymbol CreateAggregate(Name name, NamespaceOrAggregateSymbol parent, InputFile infile, TypeManager typeManager)
        {
            if (name == null || parent == null || infile == null || typeManager == null)
            {
                throw Error.InternalCompilerError();
            }

            AggregateSymbol sym = null;
            if (infile.GetAssemblyID() == KAID.kaidUnresolved)
            {
                // Unresolved aggs need extra storage.
                sym = CreateUnresolvedAggregate(name, parent, typeManager);
            }
            else
            {
                sym = newBasicSym(SYMKIND.SK_AggregateSymbol, name, parent).AsAggregateSymbol();
                sym.name = name;
                sym.SetTypeManager(typeManager);
                sym.SetSealed(false);
                sym.SetAccess(ACCESS.ACC_UNKNOWN);
                sym.initBogus();
                sym.SetIfaces(null);
                sym.SetIfacesAll(null);
                sym.SetTypeVars(null);
            }

            sym.InitFromInfile(infile);
            return sym;
        }

        public AggregateDeclaration CreateAggregateDecl(AggregateSymbol agg, Declaration declOuter)
        {
            Debug.Assert(agg != null);
            //Debug.Assert(declOuter == null || declOuter.Bag() == agg.Parent);

            // DECLSYMs are not parented like named symbols.
            AggregateDeclaration sym = newBasicSym(SYMKIND.SK_AggregateDeclaration, agg.name, null).AsAggregateDeclaration();

            if (declOuter != null)
            {
                declOuter.AddToChildList(sym);
            }
            agg.AddDecl(sym);

            Debug.Assert(sym != null);
            return (sym);
        }

        private AggregateSymbol CreateUnresolvedAggregate(Name name, ParentSymbol parent, TypeManager typeManager)
        {
            Debug.Assert(name != null);

            Symbol sym = newBasicSym(SYMKIND.SK_UnresolvedAggregateSymbol, name, parent);
            AggregateSymbol AggregateSymbol = null;

            // Unresolved Aggs need extra storage, but are still considered Aggs.

            sym.setKind(SYMKIND.SK_AggregateSymbol);
            AggregateSymbol = sym.AsAggregateSymbol();
            AggregateSymbol.SetTypeManager(typeManager);

            Debug.Assert(AggregateSymbol != null);
            return (AggregateSymbol);
        }

        // Members of aggs
        public FieldSymbol CreateMemberVar(Name name, ParentSymbol parent, AggregateDeclaration declaration, int iIteratorLocal)
        {
            Debug.Assert(name != null);

            FieldSymbol sym = newBasicSym(SYMKIND.SK_FieldSymbol, name, parent).AsFieldSymbol();
            sym.declaration = declaration;

            Debug.Assert(sym != null);
            return (sym);
        }

        public LocalVariableSymbol CreateLocalVar(Name name, ParentSymbol parent, CType type)
        {
            LocalVariableSymbol sym = newBasicSym(SYMKIND.SK_LocalVariableSymbol, name, parent).AsLocalVariableSymbol();
            sym.SetType(type);
            sym.SetAccess(ACCESS.ACC_UNKNOWN);    // required for Symbol::hasExternalAccess which is used by refactoring
            sym.wrap = null;

            return sym;
        }

        public MethodSymbol CreateMethod(Name name, ParentSymbol parent, AggregateDeclaration declaration)
        {
            MethodSymbol sym = newBasicSym(SYMKIND.SK_MethodSymbol, name, parent).AsMethodSymbol();
            sym.declaration = declaration;

            return sym;
        }

        public PropertySymbol CreateProperty(Name name, ParentSymbol parent, AggregateDeclaration declaration)
        {
            PropertySymbol sym = newBasicSym(SYMKIND.SK_PropertySymbol, name, parent).AsPropertySymbol();
            Debug.Assert(sym != null);
            sym.declaration = declaration;
            return (sym);
        }

        public EventSymbol CreateEvent(Name name, ParentSymbol parent, AggregateDeclaration declaration)
        {
            EventSymbol sym = newBasicSym(SYMKIND.SK_EventSymbol, name, parent).AsEventSymbol();
            sym.declaration = declaration;

            Debug.Assert(sym != null);
            return (sym);
        }

        public TypeParameterSymbol CreateMethodTypeParameter(Name pName, MethodSymbol pParent, int index, int indexTotal)
        {
            TypeParameterSymbol pResult = newBasicSym(SYMKIND.SK_TypeParameterSymbol, pName, pParent).AsTypeParameterSymbol();
            pResult.SetIndexInOwnParameters(index);
            pResult.SetIndexInTotalParameters(indexTotal);

            pResult.SetIsMethodTypeParameter(true);
            pResult.SetAccess(ACCESS.ACC_PRIVATE); // Always private - not accessible anywhere except their own type.

            return pResult;
        }

        public TypeParameterSymbol CreateClassTypeParameter(Name pName, AggregateSymbol pParent, int index, int indexTotal)
        {
            TypeParameterSymbol pResult = newBasicSym(SYMKIND.SK_TypeParameterSymbol, pName, pParent).AsTypeParameterSymbol();
            pResult.SetIndexInOwnParameters(index);
            pResult.SetIndexInTotalParameters(indexTotal);

            pResult.SetIsMethodTypeParameter(false);
            pResult.SetAccess(ACCESS.ACC_PRIVATE); // Always private - not accessible anywhere except their own type.

            return pResult;
        }
    }
}

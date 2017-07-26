// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /*
     * Define the different access levels that symbols can have.
     */
    internal enum ACCESS
    {
        ACC_UNKNOWN,     // Not yet determined.
        ACC_PRIVATE,
        ACC_INTERNAL,
        ACC_PROTECTED,
        ACC_INTERNALPROTECTED,   // internal OR protected
        ACC_PUBLIC
    }

    // The kinds of aggregates.
    internal enum AggKindEnum
    {
        Unknown,

        Class,
        Delegate,
        Interface,
        Struct,
        Enum,

        Lim
    }

    /////////////////////////////////////////////////////////////////////////////////

    // Special constraints.
    internal enum SpecCons
    {
        None = 0x00,

        New = 0x01,
        Ref = 0x02,
        Val = 0x04
    }

    // ----------------------------------------------------------------------------
    //
    // Symbol - the base symbol.
    //
    // ----------------------------------------------------------------------------

    internal abstract class Symbol
    {
        private SYMKIND _kind;     // the symbol kind
        private bool _isBogus;     // can't be used in our language -- unsupported type(s)
        private bool _checkedBogus; // Have we checked a method args/return for bogus types
        private ACCESS _access;    // access level

        // If this is true, then we had an error the first time so do not give an error the second time.

        public Name name;         // name of the symbol
        public ParentSymbol parent;  // parent of the symbol
        public Symbol nextChild;     // next child of this parent
        public Symbol nextSameName;  // next child of this parent with same name.


        public ACCESS GetAccess()
        {
            Debug.Assert(_access != ACCESS.ACC_UNKNOWN);
            return _access;
        }

        public void SetAccess(ACCESS access)
        {
            _access = access;
        }

        public SYMKIND getKind()
        {
            return _kind;
        }

        public void setKind(SYMKIND kind)
        {
            _kind = kind;
        }

        public symbmask_t mask()
        {
            return (symbmask_t)(1 << (int)_kind);
        }

        public bool checkBogus()
        {
            Debug.Assert(_checkedBogus);
            return _isBogus;
        } // if this Debug.Assert fires then call COMPILER_BASE::CheckBogus() instead

        public bool getBogus()
        {
            return _isBogus;
        }

        public bool hasBogus()
        {
            return _checkedBogus;
        }

        public void setBogus(bool isBogus)
        {
            _isBogus = isBogus;
            _checkedBogus = true;
        }

        public void initBogus()
        {
            _isBogus = false;
            _checkedBogus = false;
        }

        public bool computeCurrentBogusState()
        {
            if (hasBogus())
            {
                return checkBogus();
            }

            bool fBogus = false;

            switch (getKind())
            {
                case SYMKIND.SK_PropertySymbol:
                case SYMKIND.SK_MethodSymbol:
                    {
                        MethodOrPropertySymbol meth = (MethodOrPropertySymbol)this;

                        if (meth.RetType != null)
                        {
                            fBogus = meth.RetType.computeCurrentBogusState();
                        }
                        if (meth.Params != null)
                        {
                            for (int i = 0; !fBogus && i < meth.Params.Count; i++)
                            {
                                fBogus |= meth.Params[i].computeCurrentBogusState();
                            }
                        }
                    }
                    break;

                /*
                case SYMKIND.SK_ParameterModifierType:
                case SYMKIND.SK_OptionalModifierType:
                case SYMKIND.SK_PointerType:
                case SYMKIND.SK_ArrayType:
                case SYMKIND.SK_NullableType:
                case SYMKIND.SK_PinnedType:
                    if (this.AsType().GetBaseOrParameterOrElementType() != null)
                    {
                        fBogus = this.AsType().GetBaseOrParameterOrElementType().computeCurrentBogusState();
                    }
                    break;
                    */

                case SYMKIND.SK_EventSymbol:
                    CType evType = ((EventSymbol)this).type;
                    if (evType != null)
                    {
                        fBogus = evType.computeCurrentBogusState();
                    }
                    break;

                case SYMKIND.SK_FieldSymbol:
                    var fiType = ((FieldSymbol)this).GetType();
                    if (fiType != null)
                    {
                        fBogus = fiType.computeCurrentBogusState();
                    }
                    break;

                /*
                case SYMKIND.SK_ErrorType:
                    this.setBogus(false);
                    break;

                case SYMKIND.SK_AggregateType:
                    fBogus = this.AsAggregateType().getAggregate().computeCurrentBogusState();
                    for (int i = 0; !fBogus && i < this.AsAggregateType().GetTypeArgsAll().size; i++)
                    {
                        fBogus |= this.AsAggregateType().GetTypeArgsAll()[i].computeCurrentBogusState();
                    }
                    break;
                 */

                case SYMKIND.SK_TypeParameterSymbol:
                case SYMKIND.SK_LocalVariableSymbol:
                    setBogus(false);
                    break;

                case SYMKIND.SK_AggregateSymbol:
                    fBogus = hasBogus() && checkBogus();
                    break;

                default:
                    Debug.Assert(false, "CheckBogus with invalid Symbol kind");
                    setBogus(false);
                    break;
            }

            if (fBogus)
            {
                // Only set this if at least 1 declared thing is bogus
                setBogus(fBogus);
            }

            return hasBogus() && checkBogus();
        }

        public CType getType()
        {
            if (this is MethodOrPropertySymbol methProp)
            {
                return methProp.RetType;
            }

            if (this is FieldSymbol field)
            {
                return field.GetType();
            }

            if (this is EventSymbol ev)
            {
                return ev.type;
            }

            return null;
        }

        public bool isStatic
        {
            get
            {
                if (this is FieldSymbol field)
                {
                    return field.isStatic;
                }

                if (this is EventSymbol ev)
                {
                    return ev.isStatic;
                }

                if (this is MethodOrPropertySymbol methProp)
                {
                    return methProp.isStatic;
                }

                return this is AggregateSymbol;
            }
        }

        private Assembly GetAssembly()
        {
            switch (_kind)
            {
                case SYMKIND.SK_MethodSymbol:
                case SYMKIND.SK_PropertySymbol:
                case SYMKIND.SK_FieldSymbol:
                case SYMKIND.SK_EventSymbol:
                case SYMKIND.SK_TypeParameterSymbol:
                    return ((AggregateSymbol)parent).AssociatedAssembly;

                case SYMKIND.SK_AggregateDeclaration:
                    return ((AggregateDeclaration)this).GetAssembly();
                case SYMKIND.SK_AggregateSymbol:
                    return ((AggregateSymbol)this).AssociatedAssembly;
                default:
                    // Should never call this with any other kind.
                    Debug.Assert(false, "GetAssemblyID called on bad sym kind");
                    return null;
            }
        }

        /*
         * returns the assembly id for the declaration of this symbol
         */
        private bool InternalsVisibleTo(Assembly assembly)
        {
            switch (_kind)
            {
                case SYMKIND.SK_MethodSymbol:
                case SYMKIND.SK_PropertySymbol:
                case SYMKIND.SK_FieldSymbol:
                case SYMKIND.SK_EventSymbol:
                case SYMKIND.SK_TypeParameterSymbol:
                    return ((AggregateSymbol)parent).InternalsVisibleTo(assembly);

                case SYMKIND.SK_AggregateDeclaration:
                    return ((AggregateDeclaration)this).Agg().InternalsVisibleTo(assembly);
                case SYMKIND.SK_AggregateSymbol:
                    return ((AggregateSymbol)this).InternalsVisibleTo(assembly);
                default:
                    // Should never call this with any other kind.
                    Debug.Assert(false, "InternalsVisibleTo called on bad sym kind");
                    return false;
            }
        }

        public bool SameAssemOrFriend(Symbol sym)
        {
            Assembly assem = GetAssembly();
            return assem == sym.GetAssembly() || sym.InternalsVisibleTo(assem);
        }

        /* Returns if the symbol is virtual. */
        public bool IsVirtual()
        {
            switch (_kind)
            {
                case SYMKIND.SK_MethodSymbol:
                    return ((MethodSymbol)this).isVirtual;

                case SYMKIND.SK_EventSymbol:
                    MethodSymbol methAdd = ((EventSymbol)this).methAdd;
                    return methAdd != null && methAdd.isVirtual;

                case SYMKIND.SK_PropertySymbol:
                    PropertySymbol prop = ((PropertySymbol)this);
                    MethodSymbol meth = prop.methGet ?? prop.methSet;
                    return meth != null && meth.isVirtual;

                default:
                    return false;
            }
        }

        public bool IsOverride()
        {
            switch (_kind)
            {
                case SYMKIND.SK_MethodSymbol:
                case SYMKIND.SK_PropertySymbol:
                    return ((MethodOrPropertySymbol)this).isOverride;
                case SYMKIND.SK_EventSymbol:
                    return ((EventSymbol)this).isOverride;
                default:
                    return false;
            }
        }

        public bool IsHideByName()
        {
            switch (_kind)
            {
                case SYMKIND.SK_MethodSymbol:
                case SYMKIND.SK_PropertySymbol:
                    return ((MethodOrPropertySymbol)this).isHideByName;
                case SYMKIND.SK_EventSymbol:
                    MethodSymbol methAdd = ((EventSymbol)this).methAdd;
                    return methAdd != null && methAdd.isHideByName;
                default:
                    return true;
            }
        }

        // Returns the virtual that this sym overrides (if IsOverride() is true), null otherwise.
        public Symbol SymBaseVirtual()
        {
            return (this as MethodOrPropertySymbol)?.swtSlot.Sym;
        }

        /*
         * returns true if this symbol is a normal symbol visible to the user
         */
        public bool isUserCallable()
        {
            return !(this is MethodSymbol methSym) || methSym.isUserCallable();
        }
    }
}

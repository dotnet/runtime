// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /*
    // ===========================================================================
       Defines structs that package an aggregate member together with
       generic type argument information.
    // ===========================================================================*/
    /******************************************************************************
        SymWithType and its cousins. These package an aggregate member (field,
        prop, event, or meth) together with the particular instantiation of the
        aggregate (the AggregateType).
     
        The default constructor does nothing so these are not safe to use
        uninitialized. Note that when they are used as member of an EXPR they
        are automatically zero filled by newExpr.
    ******************************************************************************/
    internal class SymWithType
    {
        private AggregateType _ats;
        private Symbol _sym;

        public SymWithType()
        {
        }

        public SymWithType(Symbol sym, AggregateType ats)
        {
            Set(sym, ats);
        }

        public virtual void Clear()
        {
            _sym = null;
            _ats = null;
        }

        public AggregateType Ats
        {
            get { return _ats; }
        }

        public Symbol Sym
        {
            get { return _sym; }
        }

        public new AggregateType GetType()
        {
            // This conflicts with object.GetType.  Turn every usage of this
            // into a get on Ats.
            return Ats;
        }

        public static bool operator ==(SymWithType swt1, SymWithType swt2)
        {
            if (ReferenceEquals(swt1, swt2))
            {
                return true;
            }
            else if (ReferenceEquals(swt1, null))
            {
                return swt2._sym == null;
            }
            else if (ReferenceEquals(swt2, null))
            {
                return swt1._sym == null;
            }
            return swt1.Sym == swt2.Sym && swt1.Ats == swt2.Ats;
        }

        public static bool operator !=(SymWithType swt1, SymWithType swt2) => !(swt1 == swt2);

        [ExcludeFromCodeCoverage] // == overload should always be the method called.
        public override bool Equals(object obj)
        {
            Debug.Fail("Sub-optimal equality called. Check if this is correct.");
            SymWithType other = obj as SymWithType;
            if (other == null) return false;
            return Sym == other.Sym && Ats == other.Ats;
        }

        [ExcludeFromCodeCoverage] // Never used as a key.
        public override int GetHashCode()
        {
            Debug.Fail("If using this as a key, implement IEquatable<SymWithType>");
            return (Sym?.GetHashCode() ?? 0) + (Ats?.GetHashCode() ?? 0);
        }

        // The SymWithType is considered NULL iff the Symbol is NULL.
        public static implicit operator bool (SymWithType swt)
        {
            return swt != null;
        }

        // These assert that the Symbol is of the correct type.
        public MethodOrPropertySymbol MethProp()
        {
            return Sym as MethodOrPropertySymbol;
        }

        public MethodSymbol Meth()
        {
            return Sym as MethodSymbol;
        }

        public PropertySymbol Prop()
        {
            return Sym as PropertySymbol;
        }

        public FieldSymbol Field()
        {
            return Sym as FieldSymbol;
        }

        public EventSymbol Event()
        {
            return Sym as EventSymbol;
        }

        public void Set(Symbol sym, AggregateType ats)
        {
            if (sym == null)
                ats = null;
            Debug.Assert(ats == null || sym.parent == ats.getAggregate());
            _sym = sym;
            _ats = ats;
        }
    }

    internal class MethPropWithType : SymWithType
    {
        public MethPropWithType()
        {
        }

        public MethPropWithType(MethodOrPropertySymbol mps, AggregateType ats)
        {
            Set(mps, ats);
        }
    }

    internal sealed class MethWithType : MethPropWithType
    {
        public MethWithType()
        {
        }

        public MethWithType(MethodSymbol meth, AggregateType ats)
        {
            Set(meth, ats);
        }
    }

    internal sealed class PropWithType : MethPropWithType
    {
        public PropWithType(PropertySymbol prop, AggregateType ats)
        {
            Set(prop, ats);
        }

        public PropWithType(SymWithType swt)
        {
            Set(swt.Sym as PropertySymbol, swt.Ats);
        }
    }

    internal sealed class EventWithType : SymWithType
    {
        public EventWithType(EventSymbol @event, AggregateType ats)
        {
            Set(@event, ats);
        }
    }

    internal sealed class FieldWithType : SymWithType
    {
        public FieldWithType(FieldSymbol field, AggregateType ats)
        {
            Set(field, ats);
        }
    }

    /******************************************************************************
        MethPropWithInst and MethWithInst. These extend MethPropWithType with
        the method type arguments. Properties will never have type args, but
        methods and properties share a lot of code so it's convenient to allow
        both here.
     
        The default constructor does nothing so these are not safe to use
        uninitialized. Note that when they are used as member of an EXPR they
        are automatically zero filled by newExpr.
    ******************************************************************************/

    internal class MethPropWithInst : MethPropWithType
    {
        public TypeArray TypeArgs { get; private set; }

        public MethPropWithInst()
        {
            Set(null, null, null);
        }

        public MethPropWithInst(MethodOrPropertySymbol mps, AggregateType ats)
            : this(mps, ats, null)
        {
        }

        public MethPropWithInst(MethodOrPropertySymbol mps, AggregateType ats, TypeArray typeArgs)
        {
            Set(mps, ats, typeArgs);
        }

        public override void Clear()
        {
            base.Clear();
            TypeArgs = null;
        }

        public void Set(MethodOrPropertySymbol mps, AggregateType ats, TypeArray typeArgs)
        {
            if (mps == null)
            {
                ats = null;
                typeArgs = null;
            }
            Debug.Assert(ats == null || mps != null && mps.getClass() == ats.getAggregate());
            base.Set(mps, ats);
            TypeArgs = typeArgs;
        }
    }

    internal sealed class MethWithInst : MethPropWithInst
    {
        public MethWithInst(MethodSymbol meth, AggregateType ats)
            : this(meth, ats, null)
        {
        }
        public MethWithInst(MethodSymbol meth, AggregateType ats, TypeArray typeArgs)
        {
            Set(meth, ats, typeArgs);
        }
        public MethWithInst(MethPropWithInst mpwi)
        {
            Set(mpwi.Sym as MethodSymbol, mpwi.Ats, mpwi.TypeArgs);
        }
    }
}

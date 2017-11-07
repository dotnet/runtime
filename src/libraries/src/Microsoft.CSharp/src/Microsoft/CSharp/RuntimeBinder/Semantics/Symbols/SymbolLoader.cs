// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class SymbolLoader
    {
        private PredefinedMembers PredefinedMembers { get; }
        private GlobalSymbolContext GlobalSymbolContext { get; }
        public ErrorHandling ErrorContext { get; }
        public SymbolTable RuntimeBinderSymbolTable { get; private set; }

        public SymbolLoader()
        {
            GlobalSymbolContext globalSymbols = new GlobalSymbolContext();
            PredefinedMembers = new PredefinedMembers(this);
            ErrorContext = new ErrorHandling(globalSymbols);
            GlobalSymbolContext = globalSymbols;
            Debug.Assert(GlobalSymbolContext != null);
        }

        public ErrorHandling GetErrorContext()
        {
            return ErrorContext;
        }

        public GlobalSymbolContext GetGlobalSymbolContext()
        {
            return GlobalSymbolContext;
        }

        public MethodSymbol LookupInvokeMeth(AggregateSymbol pAggDel)
        {
            Debug.Assert(pAggDel.AggKind() == AggKindEnum.Delegate);
            for (Symbol pSym = LookupAggMember(NameManager.GetPredefinedName(PredefinedName.PN_INVOKE), pAggDel, symbmask_t.MASK_ALL);
                 pSym != null;
                 pSym = LookupNextSym(pSym, pAggDel, symbmask_t.MASK_ALL))
            {
                if (pSym is MethodSymbol meth && meth.isInvoke())
                {
                    return meth;
                }
            }
            return null;
        }

        public PredefinedTypes GetPredefindTypes()
        {
            return GlobalSymbolContext.GetPredefTypes();
        }

        public TypeManager GetTypeManager()
        {
            return TypeManager;
        }

        public TypeManager TypeManager
        {
            get { return GlobalSymbolContext.TypeManager; }
        }

        public PredefinedMembers getPredefinedMembers()
        {
            return PredefinedMembers;
        }

        public BSYMMGR getBSymmgr()
        {
            return GlobalSymbolContext.GetGlobalSymbols();
        }

        public SymFactory GetGlobalSymbolFactory()
        {
            return GlobalSymbolContext.GetGlobalSymbolFactory();
        }

        public AggregateSymbol GetPredefAgg(PredefinedType pt) => GetTypeManager().GetPredefAgg(pt);

        public AggregateType GetPredefindType(PredefinedType pt) => GetPredefAgg(pt).getThisType();

        public Symbol LookupAggMember(Name name, AggregateSymbol agg, symbmask_t mask)
        {
            return getBSymmgr().LookupAggMember(name, agg, mask);
        }

        public static Symbol LookupNextSym(Symbol sym, ParentSymbol parent, symbmask_t kindmask)
            => BSYMMGR.LookupNextSym(sym, parent, kindmask);

        // It would be nice to make this a virtual method on typeSym.
        public AggregateType GetAggTypeSym(CType typeSym)
        {
            Debug.Assert(typeSym != null);
            Debug.Assert(typeSym is AggregateType ||
                   typeSym is ArrayType ||
                   typeSym is NullableType);

            switch (typeSym.GetTypeKind())
            {
                case TypeKind.TK_AggregateType:
                    return (AggregateType)typeSym;
                case TypeKind.TK_ArrayType:
                    return GetPredefindType(PredefinedType.PT_ARRAY);
                case TypeKind.TK_NullableType:
                    return ((NullableType)typeSym).GetAts();
            }
            Debug.Assert(false, "Bad typeSym!");
            return null;
        }

        private static bool IsBaseInterface(AggregateType atsDer, AggregateType pBase)
        {
            Debug.Assert(atsDer != null);
            Debug.Assert(pBase != null);
            if (pBase.isInterfaceType())
            {
                while (atsDer != null)
                {
                    TypeArray ifacesAll = atsDer.GetIfacesAll();
                    for (int i = 0; i < ifacesAll.Count; i++)
                    {
                        if (AreTypesEqualForConversion(ifacesAll[i], pBase))
                        {
                            return true;
                        }
                    }

                    atsDer = atsDer.GetBaseClass();
                }
            }

            return false;
        }

        public static bool IsBaseClassOfClass(CType pDerived, CType pBase)
        {
            Debug.Assert(pDerived != null);
            Debug.Assert(pBase != null);

            // This checks to see whether derived is a class, and if so, 
            // if base is a base class of derived.
            if (!pDerived.isClassType())
            {
                return false;
            }
            return IsBaseClass(pDerived, pBase);
        }

        private static bool IsBaseClass(CType pDerived, CType pBase)
        {
            Debug.Assert(pDerived != null);
            Debug.Assert(pBase != null);
            // A base class has got to be a class. The derived type might be a struct.

            if (!(pBase is AggregateType atsBase && atsBase.isClassType()))
            {
                return false;
            }
            if (pDerived is NullableType derivedNub)
            {
                pDerived = derivedNub.GetAts();
            }

            if (!(pDerived is AggregateType atsDer))
            {
                return false;
            }

            AggregateType atsCur = atsDer.GetBaseClass();
            while (atsCur != null)
            {
                if (atsCur == atsBase)
                {
                    return true;
                }
                atsCur = atsCur.GetBaseClass();
            }
            return false;
        }

        private bool HasCovariantArrayConversion(ArrayType pSource, ArrayType pDest)
        {
            Debug.Assert(pSource != null);
            Debug.Assert(pDest != null);
            // * S and T differ only in element type. In other words, S and T have the same number of dimensions.
            // * Both SE and TE are reference types.
            // * An implicit reference conversion exists from SE to TE.
            return (pSource.rank == pDest.rank) && pSource.IsSZArray == pDest.IsSZArray &&
                HasImplicitReferenceConversion(pSource.GetElementType(), pDest.GetElementType());
        }

        public bool HasIdentityOrImplicitReferenceConversion(CType pSource, CType pDest)
        {
            Debug.Assert(pSource != null);
            Debug.Assert(pDest != null);

            if (AreTypesEqualForConversion(pSource, pDest))
            {
                return true;
            }
            return HasImplicitReferenceConversion(pSource, pDest);
        }

        private static bool AreTypesEqualForConversion(CType pType1, CType pType2) => pType1.Equals(pType2);

        private bool HasArrayConversionToInterface(ArrayType pSource, CType pDest)
        {
            Debug.Assert(pSource != null);
            Debug.Assert(pDest != null);
            if (!pSource.IsSZArray)
            {
                return false;
            }
            if (!pDest.isInterfaceType())
            {
                return false;
            }

            // * From a single-dimensional array type S[] to IList<T> or IReadOnlyList<T> and their base
            //   interfaces, provided that there is an implicit identity or reference
            //   conversion from S to T.

            // We only have six interfaces to check. IList<T>, IReadOnlyList<T> and their bases:
            // * The base interface of IList<T> is ICollection<T>.
            // * The base interface of ICollection<T> is IEnumerable<T>.
            // * The base interface of IEnumerable<T> is IEnumerable.
            // * The base interface of IReadOnlyList<T> is IReadOnlyCollection<T>.
            // * The base interface of IReadOnlyCollection<T> is IEnumerable<T>.

            if (pDest.isPredefType(PredefinedType.PT_IENUMERABLE))
            {
                return true;
            }

            AggregateType atsDest = (AggregateType)pDest;
            AggregateSymbol aggDest = atsDest.getAggregate();
            if (!aggDest.isPredefAgg(PredefinedType.PT_G_ILIST) &&
                !aggDest.isPredefAgg(PredefinedType.PT_G_ICOLLECTION) &&
                !aggDest.isPredefAgg(PredefinedType.PT_G_IENUMERABLE) &&
                !aggDest.isPredefAgg(PredefinedType.PT_G_IREADONLYCOLLECTION) &&
                !aggDest.isPredefAgg(PredefinedType.PT_G_IREADONLYLIST))
            {
                return false;
            }

            Debug.Assert(atsDest.GetTypeArgsAll().Count == 1);

            CType pSourceElement = pSource.GetElementType();
            CType pDestTypeArgument = atsDest.GetTypeArgsAll()[0];
            return HasIdentityOrImplicitReferenceConversion(pSourceElement, pDestTypeArgument);
        }

        private bool HasImplicitReferenceConversion(CType pSource, CType pDest)
        {
            Debug.Assert(pSource != null);
            Debug.Assert(pDest != null);
            Debug.Assert(!(pSource is TypeParameterType));

            // The implicit reference conversions are:
            // * From any reference type to Object.
            if (pSource.IsRefType() && pDest.isPredefType(PredefinedType.PT_OBJECT))
            {
                return true;
            }

            if (pSource is AggregateType aggSource)
            {
                if (pDest is AggregateType aggDest)
                {
                    switch (aggSource.GetOwningAggregate().AggKind())
                    {
                        case AggKindEnum.Class:
                            switch (aggDest.GetOwningAggregate().AggKind())
                            {
                                case AggKindEnum.Class:
                                    // * From any class type S to any class type T provided S is derived from T.
                                    return IsBaseClass(aggSource, aggDest);

                                case AggKindEnum.Interface:
                                    // ORIGINAL RULES:
                                    //    // * From any class type S to any interface type T provided S implements T.
                                    //    if (pSource.isClassType() && pDest.isInterfaceType() && IsBaseInterface(pSource, pDest))
                                    //    {
                                    //        return true;
                                    //    }
                                    //    // * from any interface type S to any interface type T, provided S is derived from T.
                                    //    if (pSource.isInterfaceType() && pDest.isInterfaceType() && IsBaseInterface(pSource, pDest))
                                    //    {
                                    //        return true;
                                    //    }

                                    // VARIANCE EXTENSIONS:
                                    // * From any class type S to any interface type T provided S implements an interface
                                    //   convertible to T.
                                    // * From any interface type S to any interface type T provided S implements an interface
                                    //   convertible to T.
                                    // * From any interface type S to any interface type T provided S is not T and S is 
                                    //   an interface convertible to T.

                                    return HasAnyBaseInterfaceConversion(aggSource, aggDest);
                            }

                            break;

                        case AggKindEnum.Interface:
                            if (aggDest.isInterfaceType())
                            {
                                return HasAnyBaseInterfaceConversion(aggSource, aggDest)
                                       || HasInterfaceConversion(aggSource, aggDest);
                            }

                            break;

                        case AggKindEnum.Delegate:
                            // * From any delegate type to System.Delegate
                            // 
                            // SPEC OMISSION:
                            // 
                            // The spec should actually say
                            //
                            // * From any delegate type to System.Delegate 
                            // * From any delegate type to System.MulticastDelegate
                            // * From any delegate type to any interface implemented by System.MulticastDelegate
                            if (aggDest.isPredefType(PredefinedType.PT_MULTIDEL)
                                || aggDest.isPredefType(PredefinedType.PT_DELEGATE) || IsBaseInterface(
                                    GetPredefindType(PredefinedType.PT_MULTIDEL), aggDest))
                            {
                                return true;
                            }

                            // VARIANCE EXTENSION:
                            // * From any delegate type S to a delegate type T provided S is not T and
                            //   S is a delegate convertible to T
                            return pDest.isDelegateType() && HasDelegateConversion(aggSource, aggDest);
                    }
                }
            }
            else if (pSource is ArrayType arrSource)
            {
                // * From an array type S with an element type SE to an array type T with element type TE
                //   provided that all of the following are true:
                //   * S and T differ only in element type. In other words, S and T have the same number of dimensions.
                //   * Both SE and TE are reference types.
                //   * An implicit reference conversion exists from SE to TE.
                if (pDest is ArrayType arrDest)
                {
                    return HasCovariantArrayConversion(arrSource, arrDest);
                }

                if (pDest is AggregateType aggDest)
                {
                    // * From any array type to System.Array or any interface implemented by System.Array.
                    if (aggDest.isPredefType(PredefinedType.PT_ARRAY)
                        || IsBaseInterface(GetPredefindType(PredefinedType.PT_ARRAY), aggDest))
                    {
                        return true;
                    }

                    // * From a single-dimensional array type S[] to IList<T> and its base
                    //   interfaces, provided that there is an implicit identity or reference
                    //   conversion from S to T.
                    return HasArrayConversionToInterface(arrSource, pDest);
                }
            }
            else if (pSource is NullType)
            {
                // * From the null literal to any reference type
                // NOTE: We extend the specification here. The C# 3.0 spec does not describe
                // a "null type". Rather, it says that the null literal is typeless, and is
                // convertible to any reference or nullable type. However, the C# 2.0 and 3.0
                // implementations have a "null type" which some expressions other than the
                // null literal may have. (For example, (null??null), which is also an
                // extension to the specification.)
                return pDest.IsRefType() || pDest is NullableType;
            }

            return false;
        }

        private bool HasAnyBaseInterfaceConversion(CType pDerived, CType pBase)
        {
            if (!pBase.isInterfaceType())
            {
                return false;
            }
            if (!(pDerived is AggregateType atsDer))
            {
                return false;
            }

            AggregateType atsBase = (AggregateType)pBase;
            while (atsDer != null)
            {
                foreach (AggregateType iface in atsDer.GetIfacesAll().Items)
                {
                    if (HasInterfaceConversion(iface, atsBase))
                    {
                        return true;
                    }
                }

                atsDer = atsDer.GetBaseClass();
            }

            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // The rules for variant interface and delegate conversions are the same:
        //
        // An interface/delegate type S is convertible to an interface/delegate type T 
        // if and only if T is U<S1, ... Sn> and T is U<T1, ... Tn> such that for all
        // parameters of U:
        //
        // * if the ith parameter of U is invariant then Si is exactly equal to Ti.
        // * if the ith parameter of U is covariant then either Si is exactly equal
        //   to Ti, or there is an implicit reference conversion from Si to Ti.
        // * if the ith parameter of U is contravariant then either Si is exactly
        //   equal to Ti, or there is an implicit reference conversion from Ti to Si.

        private bool HasInterfaceConversion(AggregateType pSource, AggregateType pDest)
        {
            Debug.Assert(pSource != null && pSource.isInterfaceType());
            Debug.Assert(pDest != null && pDest.isInterfaceType());
            return HasVariantConversion(pSource, pDest);
        }

        //////////////////////////////////////////////////////////////////////////////

        private bool HasDelegateConversion(AggregateType pSource, AggregateType pDest)
        {
            Debug.Assert(pSource != null && pSource.isDelegateType());
            Debug.Assert(pDest != null && pDest.isDelegateType());
            return HasVariantConversion(pSource, pDest);
        }

        //////////////////////////////////////////////////////////////////////////////

        private bool HasVariantConversion(AggregateType pSource, AggregateType pDest)
        {
            Debug.Assert(pSource != null);
            Debug.Assert(pDest != null);
            if (pSource == pDest)
            {
                return true;
            }
            AggregateSymbol pAggSym = pSource.getAggregate();
            if (pAggSym != pDest.getAggregate())
            {
                return false;
            }

            TypeArray pTypeParams = pAggSym.GetTypeVarsAll();
            TypeArray pSourceArgs = pSource.GetTypeArgsAll();
            TypeArray pDestArgs = pDest.GetTypeArgsAll();

            Debug.Assert(pTypeParams.Count == pSourceArgs.Count);
            Debug.Assert(pTypeParams.Count == pDestArgs.Count);

            for (int iParam = 0; iParam < pTypeParams.Count; ++iParam)
            {
                CType pSourceArg = pSourceArgs[iParam];
                CType pDestArg = pDestArgs[iParam];
                // If they're identical then this one is automatically good, so skip it.
                if (pSourceArg == pDestArg)
                {
                    continue;
                }
                TypeParameterType pParam = (TypeParameterType)pTypeParams[iParam];
                if (pParam.Invariant)
                {
                    return false;
                }
                if (pParam.Covariant)
                {
                    if (!HasImplicitReferenceConversion(pSourceArg, pDestArg))
                    {
                        return false;
                    }
                }
                if (pParam.Contravariant)
                {
                    if (!HasImplicitReferenceConversion(pDestArg, pSourceArg))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool HasImplicitBoxingConversion(CType pSource, CType pDest)
        {
            Debug.Assert(pSource != null);
            Debug.Assert(pDest != null);
            Debug.Assert(!(pSource is TypeParameterType));

            // The rest of the boxing conversions only operate when going from a value type
            // to a reference type.

            if (!pDest.IsRefType())
            {
                return false;
            }

            // A boxing conversion exists from a nullable type to a reference type
            // if and only if a boxing conversion exists from the underlying type.
            if (pSource is NullableType nubSource)
            {
                pSource = nubSource.UnderlyingType; // pSource.IsValType() known to be true.
            }
            else if (!pSource.IsValType())
            {
                return false;
            }


            // A boxing conversion exists from any non-nullable value type to object,
            // to System.ValueType, and to any interface type implemented by the
            // non-nullable value type.  Furthermore, an enum type can be converted
            // to the type System.Enum.

            // We set the base class of the structs to System.ValueType, System.Enum, etc,
            // so we can just check here.

            return IsBaseClass(pSource, pDest) || HasAnyBaseInterfaceConversion(pSource, pDest);
        }

        public bool HasBaseConversion(CType pSource, CType pDest)
        {
            // By a "base conversion" we mean:
            //
            // * an identity conversion
            // * an implicit reference conversion
            // * an implicit boxing conversion
            // * an implicit type parameter conversion
            //
            // In other words, these are conversions that can be made to a base
            // class, base interface or co/contravariant type without any change in
            // representation other than boxing.  A conversion from, say, int to double, 
            // is NOT a "base conversion", because representation is changed.  A conversion
            // from, say, lambda to expression tree is not a "base conversion" because 
            // do not have a type.
            //
            // The existence of a base conversion depends solely upon the source and
            // destination types, not the source expression.
            //
            // This notion is not found in the spec but it is useful in the implementation.

            if (pSource is AggregateType && pDest.isPredefType(PredefinedType.PT_OBJECT))
            {
                // If we are going from any aggregate type (class, struct, interface, enum or delegate)
                // to object, we immediately return true. This may seem like a mere optimization --
                // after all, if we have an aggregate then we have some kind of implicit conversion
                // to object.
                //
                // However, it is not a mere optimization; this introduces a control flow change
                // in error reporting scenarios for unresolved type forwarders. If a type forwarder
                // cannot be resolved then the resulting type symbol will be an aggregate, but
                // we will not be able to classify it into class, struct, etc.
                //
                // We know that we will have an error in this case; we do not wish to compound
                // that error by giving a spurious "you cannot convert this thing to object"
                // error, which, after all, will go away when the type forwarding problem is
                // fixed.
                return true;
            }

            return HasIdentityOrImplicitReferenceConversion(pSource, pDest) || HasImplicitBoxingConversion(pSource, pDest);
        }

        public static bool IsBaseAggregate(AggregateSymbol derived, AggregateSymbol @base)
        {
            Debug.Assert(!derived.IsEnum() && !@base.IsEnum());

            if (derived == @base)
                return true;      // identity.

            // refactoring error tolerance:  structs and delegates can be base classes in error scenarios so
            // we cannot filter on whether or not the base is marked as sealed.

            if (@base.IsInterface())
            {
                // Search the direct and indirect interfaces via ifacesAll, going up the base chain...

                while (derived != null)
                {
                    foreach (AggregateType iface in derived.GetIfacesAll().Items)
                    {
                        if (iface.getAggregate() == @base)
                            return true;
                    }
                    derived = derived.GetBaseAgg();
                }

                return false;
            }

            // base is a class. Just go up the base class chain to look for it.

            while (derived.GetBaseClass() != null)
            {
                derived = derived.GetBaseClass().getAggregate();
                if (derived == @base)
                    return true;
            }
            return false;
        }

        internal void SetSymbolTable(SymbolTable symbolTable)
        {
            RuntimeBinderSymbolTable = symbolTable;
        }
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // Encapsulates all logic about convertibility between types.
    //
    // WARNING: These methods do not precisely match the spec.
    // WARNING: For example most also return true for identity conversions,
    // WARNING: FExpRefConv includes all Implicit and Explicit reference conversions.

    internal static class CConversions
    {
        // WARNING: These methods do not precisely match the spec.
        // WARNING: For example most also return true for identity conversions,
        // WARNING: FExpRefConv includes all Implicit and Explicit reference conversions.

        /***************************************************************************************************
            Determine whether there is an implicit reference conversion from typeSrc to typeDst. This is
            when the source is a reference type and the destination is a base type of the source. Note
            that typeDst.IsRefType() may still return false (when both are type parameters).
        ***************************************************************************************************/
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public static bool FImpRefConv(CType typeSrc, CType typeDst) =>
            typeSrc.IsReferenceType && SymbolLoader.HasIdentityOrImplicitReferenceConversion(typeSrc, typeDst);

        /***************************************************************************************************
            Determine whether there is an explicit or implicit reference conversion (or identity conversion)
            from typeSrc to typeDst. This is when:

         13.2.3 Explicit reference conversions

        The explicit reference conversions are:
        *   From object to any reference-type.
        *   From any class-type S to any class-type T, provided S is a base class of T.
        *   From any class-type S to any interface-type T, provided S is not sealed and provided S does not implement T.
        *   From any interface-type S to any class-type T, provided T is not sealed or provided T implements S.
        *   From any interface-type S to any interface-type T, provided S is not derived from T.
        *   From an array-type S with an element type SE to an array-type T with an element type TE, provided all of the following are true:
            o   S and T differ only in element type. (In other words, S and T have the same number of dimensions.)
            o   An explicit reference conversion exists from SE to TE.
        *   From System.Array and the interfaces it implements, to any array-type.
        *   From System.Delegate and the interfaces it implements, to any delegate-type.
        *   From a one-dimensional array-type S[] to System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyList<T> and their base interfaces, provided there is an explicit reference conversion from S to T.
        *   From a generic delegate type S to generic delegate type  T, provided all of the follow are true:
            o Both types are constructed generic types of the same generic delegate type, D<X1,... Xk>.That is,
              S is D<S1,... Sk> and T is D<T1,... Tk>.
            o S is not compatible with or identical to T.
            o If type parameter Xi is declared to be invariant then Si must be identical to Ti.
            o If type parameter Xi is declared to be covariant ("out") then Si must be convertible
              to Ti via an identify conversion,  implicit reference conversion, or explicit reference conversion.
            o If type parameter Xi is declared to be contravariant ("in") then either Si must be identical to Ti,
               or Si and Ti must both be reference types.
        *   From System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyList<T> and their base interfaces to a one-dimensional array-type S[], provided there is an implicit or explicit reference conversion from S[] to System.Collections.Generic.IList<T> or System.Collections.Generic.IReadOnlyList<T>. This is precisely when either S and T are the same type or there is an implicit or explicit reference conversion from S to T.

        For a type-parameter T that is known to be a reference type (25.7), the following explicit reference conversions exist:
        *   From the effective base class C of T to T and from any base class of C to T.
        *   From any interface-type to T.
        *   From T to any interface-type I provided there isn't already an implicit reference conversion from T to I.
        *   From a type-parameter U to T provided that T depends on U (25.7). [Note: Since T is known to be a reference type, within the scope of T, the run-time type of U will always be a reference type, even if U is not known to be a reference type at compile-time. end note]

            * Both src and dst are reference types and there is a builtin explicit conversion from
              src to dst.
            * Or src is a reference type and dst is a base type of src (in which case the conversion is
              implicit as well).
            * Or dst is a reference type and src is a base type of dst.

            The latter two cases can happen with type variables even though the other type variable is not
            a reference type.
        ***************************************************************************************************/
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public static bool FExpRefConv(CType typeSrc, CType typeDst)
        {
            Debug.Assert(typeSrc != null);
            Debug.Assert(typeDst != null);
            if (typeSrc.IsReferenceType && typeDst.IsReferenceType)
            {
                // is there an implicit reference conversion in either direction?
                // this handles the bulk of the cases ...
                if (SymbolLoader.HasIdentityOrImplicitReferenceConversion(typeSrc, typeDst) ||
                    SymbolLoader.HasIdentityOrImplicitReferenceConversion(typeDst, typeSrc))
                {
                    return true;
                }

                // For a type-parameter T that is known to be a reference type (25.7), the following explicit reference conversions exist:
                // *    From any interface-type to T.
                // *    From T to any interface-type I provided there isn't already an implicit reference conversion from T to I.
                if (typeSrc.IsInterfaceType && typeDst is TypeParameterType || typeSrc is TypeParameterType && typeDst.IsInterfaceType)
                {
                    return true;
                }

                // * From any class-type S to any interface-type T, provided S is not sealed
                // * From any interface-type S to any class-type T, provided T is not sealed
                // * From any interface-type S to any interface-type T, provided S is not derived from T.
                if (typeSrc is AggregateType atSrc && typeDst is AggregateType atDst)
                {
                    AggregateSymbol aggSrc = atSrc.OwningAggregate;
                    AggregateSymbol aggDest = atDst.OwningAggregate;

                    if ((aggSrc.IsClass() && !aggSrc.IsSealed() && aggDest.IsInterface()) ||
                        (aggSrc.IsInterface() && aggDest.IsClass() && !aggDest.IsSealed()) ||
                        (aggSrc.IsInterface() && aggDest.IsInterface()))
                    {
                        return true;
                    }
                }

                if (typeSrc is ArrayType arrSrc)
                {
                    // *    From an array-type S with an element type SE to an array-type T with an element type TE, provided all of the following are true:
                    //     o    S and T differ only in element type. (In other words, S and T have the same number of dimensions.)
                    //     o    An explicit reference conversion exists from SE to TE.
                    if (typeDst is ArrayType arrDst)
                    {
                        return arrSrc.Rank == arrDst.Rank
                               && arrSrc.IsSZArray == arrDst.IsSZArray
                               && FExpRefConv(arrSrc.ElementType, arrDst.ElementType);
                    }

                    // *    From a one-dimensional array-type S[] to System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyList<T>
                    //      and their base interfaces, provided there is an explicit reference conversion from S to T.
                    if (!arrSrc.IsSZArray || !typeDst.IsInterfaceType)
                    {
                        return false;
                    }

                    AggregateType aggDst = (AggregateType)typeDst;
                    TypeArray typeArgsAll = aggDst.TypeArgsAll;

                    if (typeArgsAll.Count != 1)
                    {
                        return false;
                    }

                    AggregateSymbol aggIList = SymbolLoader.GetPredefAgg(PredefinedType.PT_G_ILIST);
                    AggregateSymbol aggIReadOnlyList = SymbolLoader.GetPredefAgg(PredefinedType.PT_G_IREADONLYLIST);

                    if ((aggIList == null ||
                        !SymbolLoader.IsBaseAggregate(aggIList, aggDst.OwningAggregate)) &&
                        (aggIReadOnlyList == null ||
                        !SymbolLoader.IsBaseAggregate(aggIReadOnlyList, aggDst.OwningAggregate)))
                    {
                        return false;
                    }

                    return FExpRefConv(arrSrc.ElementType, typeArgsAll[0]);
                }

                if (typeDst is ArrayType arrayDest && typeSrc is AggregateType aggtypeSrc)
                {
                    // * From System.Array and the interfaces it implements, to any array-type.
                    if (SymbolLoader.HasIdentityOrImplicitReferenceConversion(SymbolLoader.GetPredefindType(PredefinedType.PT_ARRAY), typeSrc))
                    {
                        return true;
                    }

                    // *    From System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyList<T> and their base interfaces to a
                    //      one-dimensional array-type S[], provided there is an implicit or explicit reference conversion from S[] to
                    //      System.Collections.Generic.IList<T> or System.Collections.Generic.IReadOnlyList<T>. This is precisely when either S and T
                    //      are the same type or there is an implicit or explicit reference conversion from S to T.
                    if (!arrayDest.IsSZArray || !typeSrc.IsInterfaceType || aggtypeSrc.TypeArgsAll.Count != 1)
                    {
                        return false;
                    }

                    AggregateSymbol aggIList = SymbolLoader.GetPredefAgg(PredefinedType.PT_G_ILIST);
                    AggregateSymbol aggIReadOnlyList = SymbolLoader.GetPredefAgg(PredefinedType.PT_G_IREADONLYLIST);

                    if ((aggIList == null ||
                        !SymbolLoader.IsBaseAggregate(aggIList, aggtypeSrc.OwningAggregate)) &&
                        (aggIReadOnlyList == null ||
                        !SymbolLoader.IsBaseAggregate(aggIReadOnlyList, aggtypeSrc.OwningAggregate)))
                    {
                        return false;
                    }

                    CType typeArr = arrayDest.ElementType;
                    CType typeLst = aggtypeSrc.TypeArgsAll[0];

                    Debug.Assert(!(typeArr is MethodGroupType));
                    return typeArr == typeLst || FExpRefConv(typeArr, typeLst);
                }
                if (HasGenericDelegateExplicitReferenceConversion(typeSrc, typeDst))
                {
                    return true;
                }
            }
            else if (typeSrc.IsReferenceType)
            {
                // conversion of T . U, where T : class, U
                // .. these constraints implies where U : class
                return SymbolLoader.HasIdentityOrImplicitReferenceConversion(typeSrc, typeDst);
            }
            else if (typeDst.IsReferenceType)
            {
                // conversion of T . U, where U : class, T
                // .. these constraints implies where T : class
                return SymbolLoader.HasIdentityOrImplicitReferenceConversion(typeDst, typeSrc);
            }
            return false;
        }
        /***************************************************************************************************

         There exists an explicit conversion ...
         * From a generic delegate type S to generic delegate type T, provided all of the follow are true:
            o Both types are constructed generic types of the same generic delegate type, D<X1,... Xk>.That is,
              S is D<S1,... Sk> and T is D<T1,... Tk>.
            o S is not compatible with or identical to T.
            o If type parameter Xi is declared to be invariant then Si must be identical to Ti.
            o If type parameter Xi is declared to be covariant ("out") then Si must be convertible
              to Ti via an identify conversion,  implicit reference conversion, or explicit reference conversion.
            o If type parameter Xi is declared to be contravariant ("in") then either Si must be identical to Ti,
              or Si and Ti must both be reference types.
        ***************************************************************************************************/
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public static bool HasGenericDelegateExplicitReferenceConversion(CType source, CType target) =>
            target is AggregateType aggTarget && HasGenericDelegateExplicitReferenceConversion(source, aggTarget);

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public static bool HasGenericDelegateExplicitReferenceConversion(CType pSource, AggregateType pTarget)
        {
            if (!(pSource is AggregateType aggSrc) ||
                !aggSrc.IsDelegateType ||
                !pTarget.IsDelegateType ||
                aggSrc.OwningAggregate != pTarget.OwningAggregate ||
                SymbolLoader.HasIdentityOrImplicitReferenceConversion(aggSrc, pTarget))
            {
                return false;
            }

            TypeArray pTypeParams = aggSrc.OwningAggregate.GetTypeVarsAll();
            TypeArray pSourceArgs = aggSrc.TypeArgsAll;
            TypeArray pTargetArgs = pTarget.TypeArgsAll;

            Debug.Assert(pTypeParams.Count == pSourceArgs.Count);
            Debug.Assert(pTypeParams.Count == pTargetArgs.Count);

            for (int iParam = 0; iParam < pTypeParams.Count; ++iParam)
            {
                CType pSourceArg = pSourceArgs[iParam];
                CType pTargetArg = pTargetArgs[iParam];

                // If they're identical then this one is automatically good, so skip it.
                // If we have an error type, then we're in some fault tolerance. Let it through.
                if (pSourceArg == pTargetArg)
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
                    if (!FExpRefConv(pSourceArg, pTargetArg))
                    {
                        return false;
                    }
                }
                else if (pParam.Contravariant)
                {
                    if (!pSourceArg.IsReferenceType || !pTargetArg.IsReferenceType)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /***************************************************************************************************
            13.1.1 Identity conversion

            An identity conversion converts from any type to the same type. This conversion exists only
            such that an entity that already has a required type can be said to be convertible to that type.

            Always returns false if the types are error, anonymous method, or method group
        ***************************************************************************************************/

        /***************************************************************************************************
            Determines whether there is a boxing conversion from typeSrc to typeDst

        13.1.5 Boxing conversions

        A boxing conversion permits any non-nullable-value-type to be implicitly converted to the type
        object or System.ValueType or to any interface-type implemented by the non-nullable-value-type,
        and any enum type to be implicitly converted to System.Enum as well. ... An enum can be boxed to
        the type System.Enum, since that is the direct base class for all enums (21.4). A struct or enum
        can be boxed to the type System.ValueType, since that is the direct base class for all
        structs (18.3.2) and a base class for all enums.

        A nullable-type has a boxing conversion to the same set of types to which the nullable-type's
        underlying type has boxing conversions.

        For a type-parameter T that is not known to be a reference type (25.7), the following conversions
        involving T are considered to be boxing conversions at compile-time. At run-time, if T is a value
        type, the conversion is executed as a boxing conversion. At run-time, if T is a reference type,
        the conversion is executed as an implicit reference conversion or identity conversion.
        *   From T to its effective base class C, from T to any base class of C, and from T to any
            interface implemented by C. [Note: C will be one of the types System.Object, System.ValueType,
            or System.Enum (otherwise T would be known to be a reference type and 13.1.4 would apply
            instead of this clause). end note]
        *   From T to an interface-type I in T's effective interface set and from T to any base
            interface of I.
        ***************************************************************************************************/

        /***************************************************************************************************
            Determines whether there is a wrapping conversion from typeSrc to typeDst

        13.7 Conversions involving nullable types

        The following terms are used in the subsequent sections:
        *   The term wrapping denotes the process of packaging a value, of type T, in an instance of type T?.
            A value x of type T is wrapped to type T? by evaluating the expression new T?(x).
        ***************************************************************************************************/
        public static bool FWrappingConv(CType typeSrc, CType typeDst) => typeDst is NullableType nubDst && typeSrc == nubDst.UnderlyingType;

        /***************************************************************************************************
            Determines whether there is a unwrapping conversion from typeSrc to typeDst

        13.7 Conversions involving nullable types

        The following terms are used in the subsequent sections:
        *   The term unwrapping denotes the process of obtaining the value, of type T, contained in an
            instance of type T?. A value x of type T? is unwrapped to type T by evaluating the expression
            x.Value. Attempting to unwrap a null instance causes a System.InvalidOperationException to be
            thrown.

        ***************************************************************************************************/
        public static bool FUnwrappingConv(CType typeSrc, CType typeDst)
        {
            return FWrappingConv(typeDst, typeSrc);
        }
    }
}

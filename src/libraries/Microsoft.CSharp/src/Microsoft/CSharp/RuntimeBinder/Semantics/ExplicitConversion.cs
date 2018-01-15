// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed partial class ExpressionBinder
    {
        // ----------------------------------------------------------------------------
        // BindExplicitConversion
        // ----------------------------------------------------------------------------

        private sealed class ExplicitConversion
        {
            private readonly ExpressionBinder _binder;
            private Expr _exprSrc;
            private readonly CType _typeSrc;
            private readonly CType _typeDest;

            // This is for lambda error reporting. The reason we have this is because we 
            // store errors for lambda conversions, and then we don't bind the conversion
            // again to report errors. Consider the following case:
            //
            // int? x = () => null;
            //
            // When we try to convert the lambda to the nullable type int?, we first 
            // attempt the conversion to int. If that fails, then we know there is no
            // conversion to int?, since int is a predef type. We then look for UserDefined
            // conversions, and fail. When we report the errors, we ask the lambda for its
            // conversion errors. But since we attempted its conversion to int and not int?, 
            // we report the wrong error. This field is to keep track of the right type 
            // to report the error on, so that when the lambda conversion fails, it reports
            // errors on the correct type.

            private Expr _exprDest;
            private readonly bool _needsExprDest;
            private readonly CONVERTTYPE _flags;

            // ----------------------------------------------------------------------------
            // BindExplicitConversion
            // ----------------------------------------------------------------------------

            public ExplicitConversion(ExpressionBinder binder, Expr exprSrc, CType typeSrc, CType typeDest, bool needsExprDest, CONVERTTYPE flags)
            {
                _binder = binder;
                _exprSrc = exprSrc;
                _typeSrc = typeSrc;
                _typeDest = typeDest;
                _needsExprDest = needsExprDest;
                _flags = flags;
                _exprDest = null;
            }
            public Expr ExprDest { get { return _exprDest; } }
            /*
             * BindExplicitConversion
             *
             * This is a complex routine with complex parameter. Generally, this should
             * be called through one of the helper methods that insulates you
             * from the complexity of the interface. This routine handles all the logic
             * associated with explicit conversions.
             *
             * Note that this function calls BindImplicitConversion first, so the main
             * logic is only concerned with conversions that can be made explicitly, but
             * not implicitly.
             */
            public bool Bind()
            {
                // To test for a standard conversion, call canConvert(exprSrc, typeDest, STANDARDANDCONVERTTYPE.NOUDC) and
                // canConvert(typeDest, typeSrc, STANDARDANDCONVERTTYPE.NOUDC).
                Debug.Assert((_flags & CONVERTTYPE.STANDARD) == 0);

                // 13.2 Explicit conversions
                // 
                // The following conversions are classified as explicit conversions: 
                // 
                // * All implicit conversions
                // * Explicit numeric conversions
                // * Explicit enumeration conversions
                // * Explicit reference conversions
                // * Explicit interface conversions
                // * Unboxing conversions
                // * Explicit type parameter conversions
                // * User-defined explicit conversions
                // * Explicit nullable conversions
                // * Lifted user-defined explicit conversions
                //
                // Explicit conversions can occur in cast expressions (14.6.6).
                //
                // The explicit conversions that are not implicit conversions are conversions that cannot be
                // proven always to succeed, conversions that are known possibly to lose information, and
                // conversions across domains of types sufficiently different to merit explicit notation.

                // The set of explicit conversions includes all implicit conversions.

                // Don't try user-defined conversions now because we'll try them again later.
                if (_binder.BindImplicitConversion(_exprSrc, _typeSrc, _typeDest, _needsExprDest, out _exprDest, _flags | CONVERTTYPE.ISEXPLICIT))
                {
                    return true;
                }

                if (_typeSrc == null || _typeDest == null || _typeDest is MethodGroupType)
                {
                    return false;
                }

                if (_typeDest is NullableType)
                {
                    // This is handled completely by BindImplicitConversion.
                    return false;
                }

                if (_typeSrc is NullableType)
                {
                    return bindExplicitConversionFromNub();
                }

                if (bindExplicitConversionFromArrayToIList())
                {
                    return true;
                }

                // if we were casting an integral constant to another constant type,
                // then, if the constant were in range, then the above call would have succeeded.

                // But it failed, and so we know that the constant is not in range

                switch (_typeDest.GetTypeKind())
                {
                    default:
                        Debug.Fail($"Bad type kind: {_typeDest.GetTypeKind()}");
                        return false;

                    case TypeKind.TK_VoidType:
                        return false; // Can't convert to a method group or anon method.

                    case TypeKind.TK_NullType:
                        return false;  // Can never convert TO the null type.

                    case TypeKind.TK_ArrayType:
                        if (bindExplicitConversionToArray((ArrayType)_typeDest))
                        {
                            return true;
                        }

                        break;

                    case TypeKind.TK_PointerType:
                        if (bindExplicitConversionToPointer())
                        {
                            return true;
                        }

                        break;

                    case TypeKind.TK_AggregateType:
                        {
                            AggCastResult result = bindExplicitConversionToAggregate(_typeDest as AggregateType);

                            if (result == AggCastResult.Success)
                            {
                                return true;
                            }

                            if (result == AggCastResult.Abort)
                            {
                                return false;
                            }

                            break;
                        }
                }

                // No built-in conversion was found. Maybe a user-defined conversion?
                if (0 == (_flags & CONVERTTYPE.NOUDC))
                {
                    return _binder.bindUserDefinedConversion(_exprSrc, _typeSrc, _typeDest, _needsExprDest, out _exprDest, false);
                }

                return false;
            }

            private bool bindExplicitConversionFromNub()
            {
                Debug.Assert(_typeSrc != null);
                Debug.Assert(_typeDest != null);

                // If S and T are value types and there is a builtin conversion from S => T then there is an
                // explicit conversion from S? => T that throws on null.
                if (_typeDest.IsValType() && _binder.BindExplicitConversion(null, _typeSrc.StripNubs(), _typeDest, _flags | CONVERTTYPE.NOUDC))
                {
                    if (_needsExprDest)
                    {
                        Expr valueSrc = _exprSrc;
                        if (valueSrc.Type is NullableType)
                        {
                            valueSrc = _binder.BindNubValue(valueSrc);
                        }

                        Debug.Assert(valueSrc.Type == _typeSrc.StripNubs());
                        if (!_binder.BindExplicitConversion(valueSrc, valueSrc.Type, _typeDest, _needsExprDest, out _exprDest, _flags | CONVERTTYPE.NOUDC))
                        {
                            Debug.Fail("BindExplicitConversion failed unexpectedly");
                            return false;
                        }
                        if (_exprDest is ExprUserDefinedConversion udc)
                        {
                            udc.Argument = _exprSrc;
                        }
                    }
                    return true;
                }

                if ((_flags & CONVERTTYPE.NOUDC) == 0)
                {
                    return _binder.bindUserDefinedConversion(_exprSrc, _typeSrc, _typeDest, _needsExprDest, out _exprDest, false);
                }
                return false;
            }

            private bool bindExplicitConversionFromArrayToIList()
            {
                // 13.2.2
                //
                // The explicit reference conversions are:
                //
                // * From a one-dimensional array-type S[] to System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyList<T> and
                //   their base interfaces, provided there is an explicit reference conversion from S to T.

                Debug.Assert(_typeSrc != null);
                Debug.Assert(_typeDest != null);

                if (!(_typeSrc is ArrayType arrSrc) || !arrSrc.IsSZArray || !(_typeDest is AggregateType aggDest)
                    || !aggDest.isInterfaceType() || aggDest.GetTypeArgsAll().Count != 1)
                {
                    return false;
                }

                AggregateSymbol aggIList = GetSymbolLoader().GetPredefAgg(PredefinedType.PT_G_ILIST);
                AggregateSymbol aggIReadOnlyList = GetSymbolLoader().GetPredefAgg(PredefinedType.PT_G_IREADONLYLIST);

                if ((aggIList == null ||
                    !SymbolLoader.IsBaseAggregate(aggIList, aggDest.getAggregate())) &&
                    (aggIReadOnlyList == null ||
                    !SymbolLoader.IsBaseAggregate(aggIReadOnlyList, aggDest.getAggregate())))
                {
                    return false;
                }

                CType typeArr = arrSrc.GetElementType();
                CType typeLst = aggDest.GetTypeArgsAll()[0];

                if (!CConversions.FExpRefConv(GetSymbolLoader(), typeArr, typeLst))
                {
                    return false;
                }

                if (_needsExprDest)
                    _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest, EXPRFLAG.EXF_REFCHECK);
                return true;
            }

            private bool bindExplicitConversionFromIListToArray(ArrayType arrayDest)
            {
                // 13.2.2
                //
                // The explicit reference conversions are:
                //
                // * From System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyList<T> and their base interfaces 
                //   to a one-dimensional array-type S[], provided there is an implicit or explicit reference conversion from
                //   S[] to System.Collections.Generic.IList<T> or System.Collections.Generic.IReadOnlyList<T>. This is precisely when either S and T
                //   are the same type or there is an implicit or explicit reference conversion from S to T.

                if (!arrayDest.IsSZArray || !(_typeSrc is AggregateType aggSrc) || !aggSrc.isInterfaceType() ||
                    aggSrc.GetTypeArgsAll().Count != 1)
                {
                    return false;
                }

                AggregateSymbol aggIList = GetSymbolLoader().GetPredefAgg(PredefinedType.PT_G_ILIST);
                AggregateSymbol aggIReadOnlyList = GetSymbolLoader().GetPredefAgg(PredefinedType.PT_G_IREADONLYLIST);

                if ((aggIList == null ||
                    !SymbolLoader.IsBaseAggregate(aggIList, aggSrc.getAggregate())) &&
                    (aggIReadOnlyList == null ||
                    !SymbolLoader.IsBaseAggregate(aggIReadOnlyList, aggSrc.getAggregate())))
                {
                    return false;
                }

                CType typeArr = arrayDest.GetElementType();
                CType typeLst = aggSrc.GetTypeArgsAll()[0];

                Debug.Assert(!(typeArr is MethodGroupType));
                if (typeArr != typeLst && !CConversions.FExpRefConv(GetSymbolLoader(), typeArr, typeLst))
                {
                    return false;
                }
                if (_needsExprDest)
                    _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest, EXPRFLAG.EXF_REFCHECK);
                return true;
            }

            private bool bindExplicitConversionFromArrayToArray(ArrayType arraySrc, ArrayType arrayDest)
            {
                // 13.2.2
                //
                // The explicit reference conversions are:
                //
                // * From an array-type S with an element type SE to an array-type T with an element type
                //   TE, provided all of the following are true:
                //
                //   * S and T differ only in element type. (In other words, S and T have the same number
                //     of dimensions.)
                //
                //   * An explicit reference conversion exists from SE to TE.

                if (arraySrc.rank != arrayDest.rank || arraySrc.IsSZArray != arrayDest.IsSZArray)
                {
                    return false;  // Ranks do not match.
                }

                if (CConversions.FExpRefConv(GetSymbolLoader(), arraySrc.GetElementType(), arrayDest.GetElementType()))
                {
                    if (_needsExprDest)
                        _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest, EXPRFLAG.EXF_REFCHECK);
                    return true;
                }

                return false;
            }

            private bool bindExplicitConversionToArray(ArrayType arrayDest)
            {
                Debug.Assert(_typeSrc != null);
                Debug.Assert(arrayDest != null);

                if (_typeSrc is ArrayType arrSrc)
                {
                    return bindExplicitConversionFromArrayToArray(arrSrc, arrayDest);
                }

                if (bindExplicitConversionFromIListToArray(arrayDest))
                {
                    return true;
                }

                // 13.2.2
                //
                // The explicit reference conversions are:
                //
                // * From System.Array and the interfaces it implements, to any array-type.

                if (_binder.canConvert(_binder.GetPredefindType(PredefinedType.PT_ARRAY), _typeSrc, CONVERTTYPE.NOUDC))
                {
                    if (_needsExprDest)
                        _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest, EXPRFLAG.EXF_REFCHECK);
                    return true;
                }
                return false;
            }

            private bool bindExplicitConversionToPointer()
            {
                // 27.4 Pointer conversions
                //
                // in an unsafe context, the set of available explicit conversions (13.2) is extended to
                // include the following explicit pointer conversions:
                //
                // * From any pointer-type to any other pointer-type.
                // * From sbyte, byte, short, ushort, int, uint, long, or ulong to any pointer-type.

                if (_typeSrc is PointerType || _typeSrc.fundType() <= FUNDTYPE.FT_LASTINTEGRAL && _typeSrc.isNumericType())
                {
                    if (_needsExprDest)
                        _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest);
                    return true;
                }
                return false;
            }

            // 13.2.2 Explicit enumeration conversions
            //
            // The explicit enumeration conversions are:
            //
            // * From sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, or
            //   decimal to any enum-type.
            //
            // * From any enum-type to sbyte, byte, short, ushort, int, uint, long, ulong, char,
            //   float, double, or decimal.
            //
            // * From any enum-type to any other enum-type.
            //
            // * An explicit enumeration conversion between two types is processed by treating any
            //   participating enum-type as the underlying type of that enum-type, and then performing
            //   an implicit or explicit numeric conversion between the resulting types.

            private AggCastResult bindExplicitConversionFromEnumToAggregate(AggregateType aggTypeDest)
            {
                Debug.Assert(_typeSrc != null);
                Debug.Assert(aggTypeDest != null);

                if (!_typeSrc.isEnumType())
                {
                    return AggCastResult.Failure;
                }

                AggregateSymbol aggDest = aggTypeDest.getAggregate();
                if (aggDest.isPredefAgg(PredefinedType.PT_DECIMAL))
                {
                    return bindExplicitConversionFromEnumToDecimal(aggTypeDest);
                }


                if (!aggDest.getThisType().isNumericType() &&
                    !aggDest.IsEnum() &&
                    !(aggDest.IsPredefined() && aggDest.GetPredefType() == PredefinedType.PT_CHAR))
                {
                    return AggCastResult.Failure;
                }

                if (_exprSrc.GetConst() != null)
                {
                    ConstCastResult result = _binder.bindConstantCast(_exprSrc, _typeDest, _needsExprDest, out _exprDest, true);
                    if (result == ConstCastResult.Success)
                    {
                        return AggCastResult.Success;
                    }
                    else if (result == ConstCastResult.CheckFailure)
                    {
                        return AggCastResult.Abort;
                    }
                }

                if (_needsExprDest)
                    _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest);
                return AggCastResult.Success;
            }

            private AggCastResult bindExplicitConversionFromDecimalToEnum(AggregateType aggTypeDest)
            {
                Debug.Assert(_typeSrc != null);
                Debug.Assert(_typeSrc.isPredefType(PredefinedType.PT_DECIMAL));

                // There is an explicit conversion from decimal to all integral types.
                if (_exprSrc.GetConst() != null)
                {
                    // Fold the constant cast if possible.
                    ConstCastResult result = _binder.bindConstantCast(_exprSrc, _typeDest, _needsExprDest, out _exprDest, true);
                    if (result == ConstCastResult.Success)
                    {
                        return AggCastResult.Success;  // else, don't fold and use a regular cast, below.
                    }
                    if (result == ConstCastResult.CheckFailure && 0 == (_flags & CONVERTTYPE.CHECKOVERFLOW))
                    {
                        return AggCastResult.Abort;
                    }
                }

                // All casts from decimal to integer types are bound as user-defined conversions.

                bool bIsConversionOK = true;
                if (_needsExprDest)
                {
                    // According the language, this is a standard conversion, but it is implemented
                    // through a user-defined conversion. Because it's a standard conversion, we don't
                    // test the CONVERTTYPE.NOUDC flag here.
                    CType underlyingType = aggTypeDest.underlyingType();
                    bIsConversionOK = _binder.bindUserDefinedConversion(_exprSrc, _typeSrc, underlyingType, _needsExprDest, out _exprDest, false);

                    if (bIsConversionOK)
                    {
                        // upcast to the Enum type
                        _binder.bindSimpleCast(_exprDest, _typeDest, out _exprDest);
                    }
                }
                return bIsConversionOK ? AggCastResult.Success : AggCastResult.Failure;
            }

            private AggCastResult bindExplicitConversionFromEnumToDecimal(AggregateType aggTypeDest)
            {
                Debug.Assert(_typeSrc != null);
                Debug.Assert(aggTypeDest != null);
                Debug.Assert(aggTypeDest.isPredefType(PredefinedType.PT_DECIMAL));

                AggregateType underlyingType = _typeSrc.underlyingType() as AggregateType;

                // Need to first cast the source expr to its underlying type.

                Expr exprCast;

                if (_exprSrc == null)
                {
                    exprCast = null;
                }
                else
                {
                    _binder.bindSimpleCast(_exprSrc, underlyingType, out exprCast);
                }

                // There is always an implicit conversion from any integral type to decimal.

                if (exprCast.GetConst() != null)
                {
                    // Fold the constant cast if possible.
                    ConstCastResult result = _binder.bindConstantCast(exprCast, _typeDest, _needsExprDest, out _exprDest, true);
                    if (result == ConstCastResult.Success)
                    {
                        return AggCastResult.Success;  // else, don't fold and use a regular cast, below.
                    }
                    if (result == ConstCastResult.CheckFailure && 0 == (_flags & CONVERTTYPE.CHECKOVERFLOW))
                    {
                        return AggCastResult.Abort;
                    }
                }

                // Conversions from integral types to decimal are always bound as a user-defined conversion.

                if (_needsExprDest)
                {
                    // According the language, this is a standard conversion, but it is implemented
                    // through a user-defined conversion. Because it's a standard conversion, we don't
                    // test the CONVERTTYPE.NOUDC flag here.

                    bool ok = _binder.bindUserDefinedConversion(exprCast, underlyingType, aggTypeDest, _needsExprDest, out _exprDest, false);
                    Debug.Assert(ok);
                }

                return AggCastResult.Success;
            }

            private AggCastResult bindExplicitConversionToEnum(AggregateType aggTypeDest)
            {
                Debug.Assert(_typeSrc != null);
                Debug.Assert(aggTypeDest != null);

                AggregateSymbol aggDest = aggTypeDest.getAggregate();
                if (!aggDest.IsEnum())
                {
                    return AggCastResult.Failure;
                }

                if (_typeSrc.isPredefType(PredefinedType.PT_DECIMAL))
                {
                    return bindExplicitConversionFromDecimalToEnum(aggTypeDest);
                }

                if (_typeSrc.isNumericType() || (_typeSrc.isPredefined() && _typeSrc.getPredefType() == PredefinedType.PT_CHAR))
                {
                    // Transform constant to constant.
                    if (_exprSrc.GetConst() != null)
                    {
                        ConstCastResult result = _binder.bindConstantCast(_exprSrc, _typeDest, _needsExprDest, out _exprDest, true);
                        if (result == ConstCastResult.Success)
                        {
                            return AggCastResult.Success;
                        }
                        if (result == ConstCastResult.CheckFailure)
                        {
                            return AggCastResult.Abort;
                        }
                    }
                    if (_needsExprDest)
                        _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest);
                    return AggCastResult.Success;
                }
                else if (_typeSrc.isPredefined() &&
                         (_typeSrc.isPredefType(PredefinedType.PT_OBJECT) || _typeSrc.isPredefType(PredefinedType.PT_VALUE) || _typeSrc.isPredefType(PredefinedType.PT_ENUM)))
                {
                    if (_needsExprDest)
                        _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest, EXPRFLAG.EXF_UNBOX);
                    return AggCastResult.Success;
                }
                return AggCastResult.Failure;
            }

            private AggCastResult bindExplicitConversionBetweenSimpleTypes(AggregateType aggTypeDest)
            {
                // 13.2.1
                //
                // Because the explicit conversions include all implicit and explicit numeric conversions,
                // it is always possible to convert from any numeric-type to any other numeric-type using
                // a cast expression (14.6.6).

                Debug.Assert(_typeSrc != null);
                Debug.Assert(aggTypeDest != null);

                if (!_typeSrc.isSimpleType() || !aggTypeDest.isSimpleType())
                {
                    return AggCastResult.Failure;
                }

                AggregateSymbol aggDest = aggTypeDest.getAggregate();

                Debug.Assert(_typeSrc.isPredefined() && aggDest.IsPredefined());

                PredefinedType ptSrc = _typeSrc.getPredefType();
                PredefinedType ptDest = aggDest.GetPredefType();

                Debug.Assert((int)ptSrc < NUM_SIMPLE_TYPES && (int)ptDest < NUM_SIMPLE_TYPES);

                ConvKind convertKind = GetConvKind(ptSrc, ptDest);
                // Identity and implicit conversions should already have been handled.
                Debug.Assert(convertKind != ConvKind.Implicit);
                Debug.Assert(convertKind != ConvKind.Identity);

                if (convertKind != ConvKind.Explicit)
                {
                    return AggCastResult.Failure;
                }

                if (_exprSrc.GetConst() != null)
                {
                    // Fold the constant cast if possible.
                    ConstCastResult result = _binder.bindConstantCast(_exprSrc, _typeDest, _needsExprDest, out _exprDest, true);
                    if (result == ConstCastResult.Success)
                    {
                        return AggCastResult.Success;  // else, don't fold and use a regular cast, below.
                    }
                    if (result == ConstCastResult.CheckFailure && 0 == (_flags & CONVERTTYPE.CHECKOVERFLOW))
                    {
                        return AggCastResult.Abort;
                    }
                }

                bool bConversionOk = true;
                if (_needsExprDest)
                {
                    // Explicit conversions involving decimals are bound as user-defined conversions.
                    if (isUserDefinedConversion(ptSrc, ptDest))
                    {
                        // According the language, this is a standard conversion, but it is implemented
                        // through a user-defined conversion. Because it's a standard conversion, we don't
                        // test the CONVERTTYPE.NOUDC flag here.
                        bConversionOk = _binder.bindUserDefinedConversion(_exprSrc, _typeSrc, aggTypeDest, _needsExprDest, out _exprDest, false);
                    }
                    else
                    {
                        _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest, (_flags & CONVERTTYPE.CHECKOVERFLOW) != 0 ? EXPRFLAG.EXF_CHECKOVERFLOW : 0);
                    }
                }
                return bConversionOk ? AggCastResult.Success : AggCastResult.Failure;
            }

            private AggCastResult bindExplicitConversionBetweenAggregates(AggregateType aggTypeDest)
            {
                // 13.2.3 
                //
                // The explicit reference conversions are:
                //
                // * From object to any reference-type.
                // * From any class-type S to any class-type T, provided S is a base class of T.
                // * From any class-type S to any interface-type T, provided S is not sealed and
                //   provided S does not implement T.
                // * From any interface-type S to any class-type T, provided T is not sealed or provided
                //   T implements S.
                // * From any interface-type S to any interface-type T, provided S is not derived from T.

                Debug.Assert(_typeSrc != null);
                Debug.Assert(aggTypeDest != null);

                if (!(_typeSrc is AggregateType atSrc))
                {
                    return AggCastResult.Failure;
                }

                AggregateSymbol aggSrc = atSrc.getAggregate();
                AggregateSymbol aggDest = aggTypeDest.getAggregate();

                if (GetSymbolLoader().HasBaseConversion(aggTypeDest, atSrc))
                {
                    if (_needsExprDest)
                    {
                        if (aggDest.IsValueType() && aggSrc.getThisType().fundType() == FUNDTYPE.FT_REF)
                        {
                            _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest, EXPRFLAG.EXF_UNBOX);
                        }
                        else
                        {
                            _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest, EXPRFLAG.EXF_REFCHECK | (_exprSrc?.Flags & EXPRFLAG.EXF_CANTBENULL ?? 0));
                        }
                    }
                    return AggCastResult.Success;
                }

                if ((aggSrc.IsClass() && !aggSrc.IsSealed() && aggDest.IsInterface()) ||
                    (aggSrc.IsInterface() && aggDest.IsClass() && !aggDest.IsSealed()) ||
                    (aggSrc.IsInterface() && aggDest.IsInterface()) ||
                    CConversions.HasGenericDelegateExplicitReferenceConversion(GetSymbolLoader(), _typeSrc, aggTypeDest))
                {
                    if (_needsExprDest)
                        _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest, EXPRFLAG.EXF_REFCHECK | (_exprSrc?.Flags & EXPRFLAG.EXF_CANTBENULL ?? 0));
                    return AggCastResult.Success;
                }
                return AggCastResult.Failure;
            }

            private AggCastResult bindExplicitConversionFromPointerToInt(AggregateType aggTypeDest)
            {
                // 27.4 Pointer conversions
                // in an unsafe context, the set of available explicit conversions (13.2) is extended to include
                // the following explicit pointer conversions:
                //
                // * From any pointer-type to sbyte, byte, short, ushort, int, uint, long, or ulong.

                if (!(_typeSrc is PointerType) || aggTypeDest.fundType() > FUNDTYPE.FT_LASTINTEGRAL || !aggTypeDest.isNumericType())
                {
                    return AggCastResult.Failure;
                }
                if (_needsExprDest)
                    _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest);
                return AggCastResult.Success;
            }

            private AggCastResult bindExplicitConversionToAggregate(AggregateType aggTypeDest)
            {
                Debug.Assert(_typeSrc != null);
                Debug.Assert(aggTypeDest != null);

                AggCastResult result = bindExplicitConversionFromEnumToAggregate(aggTypeDest);
                if (result != AggCastResult.Failure)
                {
                    return result;
                }

                result = bindExplicitConversionToEnum(aggTypeDest);
                if (result != AggCastResult.Failure)
                {
                    return result;
                }

                result = bindExplicitConversionBetweenSimpleTypes(aggTypeDest);
                if (result != AggCastResult.Failure)
                {
                    return result;
                }

                result = bindExplicitConversionBetweenAggregates(aggTypeDest);
                if (result != AggCastResult.Failure)
                {
                    return result;
                }

                result = bindExplicitConversionFromPointerToInt(aggTypeDest);
                if (result != AggCastResult.Failure)
                {
                    return result;
                }

                if (_typeSrc is VoidType)
                {
                    // No conversion is allowed to or from a void type (user defined or otherwise)
                    // This is most likely the result of a failed anonymous method or member group conversion
                    return AggCastResult.Abort;
                }

                return AggCastResult.Failure;
            }

            private SymbolLoader GetSymbolLoader()
            {
                return _binder.GetSymbolLoader();
            }

            private ExprFactory GetExprFactory()
            {
                return _binder.GetExprFactory();
            }
        }
    }
}

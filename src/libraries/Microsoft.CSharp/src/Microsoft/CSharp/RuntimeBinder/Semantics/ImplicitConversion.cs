// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal readonly partial struct ExpressionBinder
    {
        // ----------------------------------------------------------------------------
        // BindImplicitConversion
        // ----------------------------------------------------------------------------

        private sealed class ImplicitConversion
        {
            public ImplicitConversion(ExpressionBinder binder, Expr exprSrc, CType typeSrc, CType typeDest, bool needsExprDest, CONVERTTYPE flags)
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
            private Expr _exprDest;
            private readonly ExpressionBinder _binder;
            private readonly Expr _exprSrc;
            private readonly CType _typeSrc;
            private readonly CType _typeDest;
            private readonly bool _needsExprDest;
            private CONVERTTYPE _flags;

            /*
             * BindImplicitConversion
             *
             * This is a complex routine with complex parameters. Generally, this should
             * be called through one of the helper methods that insulates you
             * from the complexity of the interface. This routine handles all the logic
             * associated with implicit conversions.
             *
             * exprSrc - the expression being converted. Can be null if only type conversion
             *           info is being supplied.
             * typeSrc - type of the source
             * typeDest - type of the destination
             * exprDest - returns an expression of the src converted to the dest. If null, we
             *            only care about whether the conversion can be attempted, not the
             *            expression tree.
             * flags    - flags possibly customizing the conversions allowed. E.g., can suppress
             *            user-defined conversions.
             *
             * returns true if the conversion can be made, false if not.
             */
            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            public bool Bind()
            {
                // 13.1 Implicit conversions
                //
                // The following conversions are classified as implicit conversions:
                //
                // *   Identity conversions
                // *   Implicit numeric conversions
                // *   Implicit enumeration conversions
                // *   Implicit reference conversions
                // *   Boxing conversions
                // *   Implicit type parameter conversions
                // *   Implicit constant expression conversions
                // *   User-defined implicit conversions
                // *   Implicit conversions from an anonymous method expression to a compatible delegate type
                // *   Implicit conversion from a method group to a compatible delegate type
                // *   Conversions from the null type (11.2.7) to any nullable type
                // *   Implicit nullable conversions
                // *   Lifted user-defined implicit conversions
                //
                // Implicit conversions can occur in a variety of situations, including function member invocations
                // (14.4.3), cast expressions (14.6.6), and assignments (14.14).

                // Can't convert to or from the error type.
                if (_typeSrc == null || _typeDest == null || _typeDest is MethodGroupType)
                {
                    return false;
                }

                Debug.Assert(_typeSrc != null && _typeDest != null);            // types must be supplied.
                Debug.Assert(_exprSrc == null || _typeSrc == _exprSrc.Type);    // type of source should be correct if source supplied
                Debug.Assert(!_needsExprDest || _exprSrc != null);           // need source expr to create dest expr

                switch (_typeDest.TypeKind)
                {
                    case TypeKind.TK_NullType:
                        // Can only convert to the null type if src is null.
                        if (!(_typeSrc is NullType))
                        {
                            return false;
                        }
                        if (_needsExprDest)
                        {
                            _exprDest = _exprSrc;
                        }
                        return true;
                    case TypeKind.TK_ArgumentListType:
                        return _typeSrc == _typeDest;
                    case TypeKind.TK_VoidType:
                        return false;
                    default:
                        break;
                }

                // 13.1.1 Identity conversion
                //
                // An identity conversion converts from any type to the same type. This conversion exists only
                // such that an entity that already has a required type can be said to be convertible to that type.

                if (_typeSrc == _typeDest &&
                    ((_flags & CONVERTTYPE.ISEXPLICIT) == 0 || (!_typeSrc.IsPredefType(PredefinedType.PT_FLOAT) && !_typeSrc.IsPredefType(PredefinedType.PT_DOUBLE))))
                {
                    if (_needsExprDest)
                    {
                        _exprDest = _exprSrc;
                    }
                    return true;
                }

                if (_typeDest is NullableType nubDest)
                {
                    return BindNubConversion(nubDest);
                }

                if (_typeSrc is NullableType nubSrc)
                {
                    return bindImplicitConversionFromNullable(nubSrc);
                }

                if ((_flags & CONVERTTYPE.ISEXPLICIT) != 0)
                {
                    _flags |= CONVERTTYPE.NOUDC;
                }

                // Get the fundamental types of destination.
                FUNDTYPE ftDest = _typeDest.FundamentalType;
                Debug.Assert(ftDest != FUNDTYPE.FT_NONE || _typeDest is ParameterModifierType);

                switch (_typeSrc.TypeKind)
                {
                    default:
                        Debug.Fail($"Bad type symbol kind: {_typeSrc.TypeKind}");
                        break;
                    case TypeKind.TK_VoidType:
                    case TypeKind.TK_ParameterModifierType:
                    case TypeKind.TK_ArgumentListType:
                        return false;
                    case TypeKind.TK_NullType:
                        if (bindImplicitConversionFromNull())
                        {
                            return true;
                        }
                        // If not, try user defined implicit conversions.
                        break;
                    case TypeKind.TK_ArrayType:
                        if (bindImplicitConversionFromArray())
                        {
                            return true;
                        }
                        // If not, try user defined implicit conversions.
                        break;
                    case TypeKind.TK_PointerType:
                        if (bindImplicitConversionFromPointer())
                        {
                            return true;
                        }
                        // If not, try user defined implicit conversions.
                        break;

                    case TypeKind.TK_AggregateType:
                        if (bindImplicitConversionFromAgg(_typeSrc as AggregateType))
                        {
                            return true;
                        }
                        // If not, try user defined implicit conversions.
                        break;
                }

                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                // RUNTIME BINDER ONLY CHANGE
                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                //
                // Every incoming dynamic operand should be implicitly convertible
                // to any type that it is an instance of.
                object srcRuntimeObject = _exprSrc?.RuntimeObject;
                if (srcRuntimeObject != null
                    && _typeDest.AssociatedSystemType.IsInstanceOfType(srcRuntimeObject)
                    && CSemanticChecker.CheckTypeAccess(_typeDest, _binder.Context.ContextForMemberLookup))
                {
                    if (_needsExprDest)
                    {
                        _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest, _exprSrc.Flags & EXPRFLAG.EXF_CANTBENULL);
                    }
                    return true;
                }

                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                // END RUNTIME BINDER ONLY CHANGE
                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                // 13.1.8 User-defined implicit conversions
                //
                // A user-defined implicit conversion consists of an optional standard implicit conversion,
                // followed by execution of a user-defined implicit conversion operator, followed by another
                // optional standard implicit conversion. The exact rules for evaluating user-defined
                // conversions are described in 13.4.3.

                if (0 == (_flags & CONVERTTYPE.NOUDC))
                {
                    return _binder.bindUserDefinedConversion(_exprSrc, _typeSrc, _typeDest, _needsExprDest, out _exprDest, true);
                }

                // No conversion was found.

                return false;
            }


            /***************************************************************************************************
                Called by BindImplicitConversion when the destination type is Nullable<T>. The following
                conversions are handled by this method:

                * For S in { object, ValueType, interfaces implemented by underlying type} there is an explicit
                  unboxing conversion S => T?
                * System.Enum => T? there is an unboxing conversion if T is an enum type
                * null => T? implemented as default(T?)

                * Implicit T?* => T?+ implemented by either wrapping or calling GetValueOrDefault the
                  appropriate number of times.
                * If imp/exp S => T then imp/exp S => T?+ implemented by converting to T then wrapping the
                  appropriate number of times.
                * If imp/exp S => T then imp/exp S?+ => T?+ implemented by calling GetValueOrDefault (m-1) times
                  then calling HasValue, producing a null if it returns false, otherwise calling Value,
                  converting to T then wrapping the appropriate number of times.

                The 3 rules above can be summarized with the following recursive rules:

                * If imp/exp S => T? then imp/exp S? => T? implemented as
                  qs.HasValue ? (T?)(qs.Value) : default(T?)
                * If imp/exp S => T then imp/exp S => T? implemented as new T?((T)s)

                This method also handles calling bindUserDefinedConversion. This method does NOT handle
                the following conversions:

                * Implicit boxing conversion from S? to { object, ValueType, Enum, ifaces implemented by S }. (Handled by BindImplicitConversion.)
                * If imp/exp S => T then explicit S?+ => T implemented by calling Value the appropriate number
                  of times. (Handled by BindExplicitConversion.)

                The recursive equivalent is:

                * If imp/exp S => T and T is not nullable then explicit S? => T implemented as qs.Value

                Some nullable conversion are NOT standard conversions. In particular, if S => T is implicit
                then S? => T is not standard. Similarly if S => T is not implicit then S => T? is not standard.
            ***************************************************************************************************/
            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private bool BindNubConversion(NullableType nubDst)
            {
                // This code assumes that STANDARD and ISEXPLICIT are never both set.
                // bindUserDefinedConversion should ensure this!
                Debug.Assert(0 != (~_flags & (CONVERTTYPE.STANDARD | CONVERTTYPE.ISEXPLICIT)));
                Debug.Assert(_exprSrc == null || _exprSrc.Type == _typeSrc);
                Debug.Assert(!_needsExprDest || _exprSrc != null);
                Debug.Assert(_typeSrc != nubDst); // BindImplicitConversion should have taken care of this already.
                AggregateType atsDst = nubDst.GetAts();

                // Check for the unboxing conversion. This takes precedence over the wrapping conversions.
                if (SymbolLoader.HasBaseConversion(nubDst.UnderlyingType, _typeSrc) && !CConversions.FWrappingConv(_typeSrc, nubDst))
                {
                    // These should be different! Fix the caller if typeSrc is an AggregateType of Nullable.
                    Debug.Assert(atsDst != _typeSrc);

                    // typeSrc is a base type of the destination nullable type so there is an explicit
                    // unboxing conversion.
                    if (0 == (_flags & CONVERTTYPE.ISEXPLICIT))
                    {
                        return false;
                    }

                    if (_needsExprDest)
                    {
                        _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest, EXPRFLAG.EXF_UNBOX);
                    }
                    return true;
                }

                bool dstWasNullable;
                bool srcWasNullable;
                CType typeDstBase = nubDst.StripNubs(out dstWasNullable);
                CType typeSrcBase = _typeSrc.StripNubs(out srcWasNullable);
                ConversionFunc pfn = (_flags & CONVERTTYPE.ISEXPLICIT) != 0 ?
                    (ConversionFunc)_binder.BindExplicitConversion :
                    (ConversionFunc)_binder.BindImplicitConversion;

                if (!srcWasNullable)
                {
                    Debug.Assert(_typeSrc == typeSrcBase);

                    // The null type can be implicitly converted to T? as the default value.
                    if (_typeSrc is NullType)
                    {
                        // If we have the constant null, generate it as a default value of T?.  If we have
                        // some wacky expression which has been determined to be always null, like (null??null)
                        // keep it in its expression form and transform it in the nullable rewrite pass.
                        if (_needsExprDest)
                        {
                            _exprDest = _exprSrc is ExprConstant
                                ? ExprFactory.CreateZeroInit(nubDst)
                                : ExprFactory.CreateCast(_typeDest, _exprSrc);
                        }
                        return true;
                    }

                    Expr exprTmp = _exprSrc;

                    // If there is an implicit/explicit S => T then there is an implicit/explicit S => T?
                    if (_typeSrc == typeDstBase || pfn(_exprSrc, _typeSrc, typeDstBase, _needsExprDest, out exprTmp, _flags | CONVERTTYPE.NOUDC))
                    {
                        if (_needsExprDest)
                        {
                            ExprUserDefinedConversion exprUDC = exprTmp as ExprUserDefinedConversion;
                            if (exprUDC != null)
                            {
                                exprTmp = exprUDC.UserDefinedCall;
                            }

                            if (dstWasNullable)
                            {
                                ExprCall call = BindNubNew(exprTmp);
                                exprTmp = call;
                                call.NullableCallLiftKind = NullableCallLiftKind.NullableConversionConstructor;
                            }

                            if (exprUDC != null)
                            {
                                exprUDC.UserDefinedCall = exprTmp;
                                exprTmp = exprUDC;
                            }

                            Debug.Assert(exprTmp.Type == nubDst);
                            _exprDest = exprTmp;
                        }
                        return true;
                    }

                    // No builtin conversion. Maybe there is a user defined conversion....
                    return 0 == (_flags & CONVERTTYPE.NOUDC) && _binder.bindUserDefinedConversion(_exprSrc, _typeSrc, nubDst, _needsExprDest, out _exprDest, 0 == (_flags & CONVERTTYPE.ISEXPLICIT));
                }

                // Both are Nullable so there is only a conversion if there is a conversion between the base types.
                // That is, if there is an implicit/explicit S => T then there is an implicit/explicit S?+ => T?+.
                if (typeSrcBase != typeDstBase && !pfn(null, typeSrcBase, typeDstBase, false, out _exprDest, _flags | CONVERTTYPE.NOUDC))
                {
                    // No builtin conversion. Maybe there is a user defined conversion....
                    return 0 == (_flags & CONVERTTYPE.NOUDC) && _binder.bindUserDefinedConversion(_exprSrc, _typeSrc, nubDst, _needsExprDest, out _exprDest, 0 == (_flags & CONVERTTYPE.ISEXPLICIT));
                }

                if (_needsExprDest)
                {
                    MethWithInst mwi = new MethWithInst(null, null);
                    ExprMemberGroup pMemGroup = ExprFactory.CreateMemGroup(null, mwi);
                    ExprCall exprDst = ExprFactory.CreateCall(0, nubDst, _exprSrc, pMemGroup, null);

                    // Here we want to first check whether or not the conversions work on the base types.

                    Expr arg1 = _binder.mustCast(_exprSrc, typeSrcBase);
                    bool convertible = (_flags & CONVERTTYPE.ISEXPLICIT) != 0
                        ? _binder.BindExplicitConversion(
                            arg1, arg1.Type, typeDstBase, out arg1, _flags | CONVERTTYPE.NOUDC)
                        : _binder.BindImplicitConversion(
                            arg1, arg1.Type, typeDstBase, out arg1, _flags | CONVERTTYPE.NOUDC);

                    if (!convertible)
                    {
                        Debug.Fail("bind(Im|Ex)plicitConversion failed unexpectedly");
                        return false;
                    }

                    exprDst.CastOfNonLiftedResultToLiftedType = _binder.mustCast(arg1, nubDst, 0);
                    exprDst.NullableCallLiftKind = NullableCallLiftKind.NullableConversion;
                    exprDst.PConversions = exprDst.CastOfNonLiftedResultToLiftedType;
                    _exprDest = exprDst;
                }

                return true;
            }

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private bool bindImplicitConversionFromNull()
            {
                // null type can be implicitly converted to any reference type or pointer type or type
                // variable with reference-type constraint.

                FUNDTYPE ftDest = _typeDest.FundamentalType;
                if (ftDest != FUNDTYPE.FT_REF && ftDest != FUNDTYPE.FT_PTR &&
                    // null is convertible to System.Nullable<T>.
                    !_typeDest.IsPredefType(PredefinedType.PT_G_OPTIONAL))
                {
                    return false;
                }
                if (_needsExprDest)
                {
                    // If the conversion argument is a constant null then return a ZEROINIT.
                    // Otherwise, bind this as a cast to the destination type. In a later
                    // rewrite pass we will rewrite the cast as SEQ(side effects, ZEROINIT).
                    _exprDest = _exprSrc is ExprConstant
                        ? ExprFactory.CreateZeroInit(_typeDest)
                        : ExprFactory.CreateCast(_typeDest, _exprSrc);
                }
                return true;
            }

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private bool bindImplicitConversionFromNullable(NullableType nubSrc)
            {
                // We can convert T? using a boxing conversion, we can convert it to ValueType, and
                // we can convert it to any interface implemented by T.
                //
                // 13.1.5 Boxing Conversions
                //
                // A nullable-type has a boxing conversion to the same set of types to which the nullable-type's
                // underlying type has boxing conversions. A boxing conversion applied to a value of a nullable-type
                // proceeds as follows:
                //
                // *   If the HasValue property of the nullable value evaluates to false, then the result of the
                //     boxing conversion is the null reference of the appropriate type.
                //
                // Otherwise, the result is obtained by boxing the result of evaluating the Value property on
                // the nullable value.

                AggregateType atsNub = nubSrc.GetAts();
                if (atsNub == _typeDest)
                {
                    if (_needsExprDest)
                    {
                        _exprDest = _exprSrc;
                    }
                    return true;
                }
                if (SymbolLoader.HasBaseConversion(nubSrc.UnderlyingType, _typeDest) && !CConversions.FUnwrappingConv(nubSrc, _typeDest))
                {
                    if (_needsExprDest)
                    {
                        _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest, EXPRFLAG.EXF_BOX);
                        if (!_typeDest.IsPredefType(PredefinedType.PT_OBJECT))
                        {
                            // The base type of a nullable is always a non-nullable value type,
                            // therefore so is typeDest unless typeDest is PT_OBJECT. In this case the conversion
                            // needs to be unboxed. We only need this if we actually will use the result.
                            _binder.bindSimpleCast(_exprDest, _typeDest, out _exprDest, EXPRFLAG.EXF_FORCE_UNBOX);
                        }
                    }
                    return true;
                }
                return 0 == (_flags & CONVERTTYPE.NOUDC) && _binder.bindUserDefinedConversion(_exprSrc, nubSrc, _typeDest, _needsExprDest, out _exprDest, true);
            }

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private bool bindImplicitConversionFromArray()
            {
                // 13.1.4
                //
                // The implicit reference conversions are:
                //
                // *   From an array-type S with an element type SE to an array-type T with an element
                //     type TE, provided all of the following are true:
                //     *   S and T differ only in element type. In other words, S and T have the same number of dimensions.
                //     *   An implicit reference conversion exists from SE to TE.
                // *   From a one-dimensional array-type S[] to System.Collections.Generic.IList<S>,
                //     System.Collections.Generic.IReadOnlyList<S> and their base interfaces
                // *   From a one-dimensional array-type S[] to System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyList<T>
                //     and their base interfaces, provided there is an implicit reference conversion from S to T.
                // *   From any array-type to System.Array.
                // *   From any array-type to any interface implemented by System.Array.

                if (!SymbolLoader.HasBaseConversion(_typeSrc, _typeDest))
                {
                    return false;
                }

                EXPRFLAG grfex = 0;
                // The above if checks for dest==Array, object or an interface the array implements,
                // including IList<T>, ICollection<T>, IEnumerable<T>, IReadOnlyList<T>, IReadOnlyCollection<T>
                // and the non-generic versions.

                if ((_typeDest is ArrayType ||
                     (_typeDest is AggregateType aggDest && aggDest.IsInterfaceType &&
                      aggDest.TypeArgsAll.Count == 1 &&
                      (aggDest.TypeArgsAll[0] != ((ArrayType)_typeSrc).ElementType ||
                       0 != (_flags & CONVERTTYPE.FORCECAST))))
                    &&
                    (0 != (_flags & CONVERTTYPE.FORCECAST) ||
                     TypeManager.TypeContainsTyVars(_typeSrc, null) ||
                     TypeManager.TypeContainsTyVars(_typeDest, null)))
                {
                    grfex = EXPRFLAG.EXF_REFCHECK;
                }
                if (_needsExprDest)
                {
                    _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest, grfex);
                }
                return true;
            }

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private bool bindImplicitConversionFromPointer()
            {
                // 27.4 Pointer conversions
                //
                // In an unsafe context, the set of available implicit conversions (13.1) is extended to include
                // the following implicit pointer conversions:
                //
                // * From any pointer-type to the type void*.

                if (_typeDest is PointerType ptDest && ptDest.ReferentType == VoidType.Instance)
                {
                    if (_needsExprDest)
                    {
                        _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest);
                    }

                    return true;
                }

                return false;
            }

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private bool bindImplicitConversionFromAgg(AggregateType aggTypeSrc)
            {
                // GENERICS: The case for constructed types is very similar to types with
                // no parameters. The parameters are irrelevant for most of the conversions
                // below. They could be relevant if we had user-defined conversions on
                // generic types.

                AggregateSymbol aggSrc = aggTypeSrc.OwningAggregate;
                if (aggSrc.IsEnum())
                {
                    return bindImplicitConversionFromEnum(aggTypeSrc);
                }

                if (_typeDest.IsEnumType)
                {
                    if (bindImplicitConversionToEnum(aggTypeSrc))
                    {
                        return true;
                    }
                    // Even though enum is sealed, a class can derive from enum in LAF scenarios --
                    // continue testing for derived to base conversions below.
                }
                else if (aggSrc.getThisType().IsSimpleType && _typeDest.IsSimpleType)
                {
                    if (bindImplicitConversionBetweenSimpleTypes(aggTypeSrc))
                    {
                        return true;
                    }
                    // No simple conversion -- continue testing for derived to base conversions below.
                }

                return bindImplicitConversionToBase(aggTypeSrc);
            }

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private bool bindImplicitConversionToBase(AggregateType pSource)
            {
                // 13.1.4 Implicit reference conversions
                //
                // *   From any reference-type to object.
                // *   From any class-type S to any class-type T, provided S is derived from T.
                // *   From any class-type S to any interface-type T, provided S implements T.
                // *   From any interface-type S to any interface-type T, provided S is derived from T.
                // *   From any delegate-type to System.Delegate.
                // *   From any delegate-type to System.ICloneable.

                if (!(_typeDest is AggregateType) || !SymbolLoader.HasBaseConversion(pSource, _typeDest))
                {
                    return false;
                }
                EXPRFLAG flags = 0x00;
                if (pSource.OwningAggregate.IsStruct() && _typeDest.FundamentalType == FUNDTYPE.FT_REF)
                {
                    flags = EXPRFLAG.EXF_BOX | EXPRFLAG.EXF_CANTBENULL;
                }
                else if (_exprSrc != null)
                {
                    flags = _exprSrc.Flags & EXPRFLAG.EXF_CANTBENULL;
                }
                if (_needsExprDest)
                    _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest, flags);
                return true;
            }

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private bool bindImplicitConversionFromEnum(AggregateType aggTypeSrc)
            {
                // 13.1.5 Boxing conversions
                //
                // A boxing conversion permits any non-nullable-value-type to be implicitly converted to the type
                // object or System.ValueType or to any interface-type implemented by the value-type, and any enum
                // type to be implicitly converted to System.Enum as well. Boxing a value of a
                // non-nullable-value-type consists of allocating an object instance and copying the value-type
                // value into that instance. An enum can be boxed to the type System.Enum, since that is the direct
                // base class for all enums (21.4). A struct or enum can be boxed to the type System.ValueType,
                // since that is the direct base class for all structs (18.3.2) and a base class for all enums.

                if (_typeDest is AggregateType aggDest && SymbolLoader.HasBaseConversion(aggTypeSrc, aggDest))
                {
                    if (_needsExprDest)
                        _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest, EXPRFLAG.EXF_BOX | EXPRFLAG.EXF_CANTBENULL);
                    return true;
                }
                return false;
            }

            private bool bindImplicitConversionToEnum(AggregateType aggTypeSrc)
            {
                // The spec states:
                // *****************
                // 13.1.3 Implicit enumeration conversions
                //
                // An implicit enumeration conversion permits the decimal-integer-literal 0 to be converted to any
                // enum-type.
                // *****************
                // However, we actually allow any constant zero, not just the integer literal zero, to be converted
                // to enum.  The reason for this is for backwards compatibility with a premature optimization
                // that used to be in the binding layer.  We would optimize away expressions such as 0 | blah to be
                // just 0, but not erase the "is literal" bit.  This meant that expression such as 0 | 0 | E.X
                // would succeed rather than correctly producing an error pointing out that 0 | 0 is not a literal
                // zero and therefore does not convert to any enum.
                //
                // We have removed the premature optimization but want old code to continue to compile. Rather than
                // try to emulate the somewhat complex behaviour of the previous optimizer, it is easier to simply
                // say that any compile time constant zero is convertible to any enum.  This means unfortunately
                // expressions such as (7-7) * 12 are convertible to enum, but frankly, that's better than having
                // some terribly complex rule about what constitutes a legal zero and what doesn't.

                // Note: Don't use GetConst here since the conversion only applies to bona-fide compile time constants.
                if (
                    aggTypeSrc.OwningAggregate.GetPredefType() != PredefinedType.PT_BOOL &&
                    _exprSrc != null &&
                    _exprSrc.IsZero() &&
                    _exprSrc.Type.IsNumericType &&
                    /*(exprSrc.flags & EXF_LITERALCONST) &&*/
                    0 == (_flags & CONVERTTYPE.STANDARD))
                {
                    // NOTE: This allows conversions from uint, long, ulong, float, double, and hexadecimal int
                    // NOTE: This is for backwards compatibility with Everett

                    // This is another place where we lose Expr fidelity. We shouldn't fold this
                    // into a constant here - we should move this to a later pass.
                    if (_needsExprDest)
                    {
                        _exprDest = ExprFactory.CreateConstant(_typeDest, ConstVal.GetDefaultValue(_typeDest.ConstValKind));
                    }
                    return true;
                }
                return false;
            }

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private bool bindImplicitConversionBetweenSimpleTypes(AggregateType aggTypeSrc)
            {
                AggregateSymbol aggSrc = aggTypeSrc.OwningAggregate;
                Debug.Assert(aggSrc.getThisType().IsSimpleType);
                Debug.Assert(_typeDest.IsSimpleType);

                Debug.Assert(aggSrc.IsPredefined() && _typeDest.IsPredefined);
                PredefinedType ptSrc = aggSrc.GetPredefType();
                PredefinedType ptDest = _typeDest.PredefinedType;
                ConvKind convertKind;

                Debug.Assert((int)ptSrc < NUM_SIMPLE_TYPES && (int)ptDest < NUM_SIMPLE_TYPES);

                // 13.1.7 Implicit constant expression conversions
                //
                // An implicit constant expression conversion permits the following conversions:
                // *   A constant-expression (14.16) of type int can be converted to type sbyte,  byte,  short,
                //     ushort,  uint, or ulong, provided the value of the constant-expression is within the range
                //     of the destination type.
                // *   A constant-expression of type long can be converted to type ulong, provided the value of
                //     the constant-expression is not negative.
                // Note: Don't use GetConst here since the conversion only applies to bona-fide compile time constants.
                if (_exprSrc is ExprConstant constant &&
                    ((ptSrc == PredefinedType.PT_INT && ptDest != PredefinedType.PT_BOOL && ptDest != PredefinedType.PT_CHAR) ||
                    (ptSrc == PredefinedType.PT_LONG && ptDest == PredefinedType.PT_ULONG)) &&
                    isConstantInRange(constant, _typeDest))
                {
                    // Special case (CLR 6.1.6): if integral constant is in range, the conversion is a legal implicit conversion.
                    convertKind = ConvKind.Implicit;
                }
                else if (ptSrc == ptDest)
                {
                    // Special case: precision limiting casts to float or double
                    Debug.Assert(ptSrc == PredefinedType.PT_FLOAT || ptSrc == PredefinedType.PT_DOUBLE);
                    Debug.Assert(0 != (_flags & CONVERTTYPE.ISEXPLICIT));
                    convertKind = ConvKind.Implicit;
                }
                else
                {
                    convertKind = GetConvKind(ptSrc, ptDest);
                    Debug.Assert(convertKind != ConvKind.Identity);
                    // identity conversion should have been handled at first.
                }

                if (convertKind != ConvKind.Implicit)
                {
                    return false;
                }

                // An implicit conversion exists. Do the conversion.
                if (_exprSrc.GetConst() != null)
                {
                    // Fold the constant cast if possible.
                    ConstCastResult result = _binder.bindConstantCast(_exprSrc, _typeDest, _needsExprDest, out _exprDest, false);
                    if (result == ConstCastResult.Success)
                    {
                        return true;  // else, don't fold and use a regular cast, below.
                    }
                }

                if (isUserDefinedConversion(ptSrc, ptDest))
                {
                    if (!_needsExprDest)
                    {
                        return true;
                    }
                    // According the language, this is a standard conversion, but it is implemented
                    // through a user-defined conversion. Because it's a standard conversion, we don't
                    // test the NOUDC flag here.
                    return _binder.bindUserDefinedConversion(_exprSrc, aggTypeSrc, _typeDest, _needsExprDest, out _exprDest, true);
                }
                if (_needsExprDest)
                    _binder.bindSimpleCast(_exprSrc, _typeDest, out _exprDest);
                return true;
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal readonly partial struct ExpressionBinder
    {
        /*
            These are the predefined binary operator signatures

                (object,    object)     :                   == !=
                (string,    string)     :                   == !=
                (string,    string)     :       +
                (string,    object)     :       +
                (object,    string)     :       +

                (int,       int)        :  / % + - << >>   == != < > <= >=&| ^
                (uint,      uint)       :  / % + -         == != < > <= >=&| ^
                (long,      long)       :  / % + -         == != < > <= >=&| ^
                (ulong,     ulong)      :  / % + -         == != < > <= >=&| ^
                (uint,      int)        :           << >>
                (long,      int)        :           << >>
                (ulong,     int)        :           << >>

                (float,     float)      :  / % + -         == != < > <= >=
                (double,    double)     :  / % + -         == != < > <= >=
                (decimal,   decimal)    :  / % + -         == != < > <= >=

                (bool,      bool)       :                   == !=          &| ^ && ||

                (Sys.Del,   Sys.Del)    :                   == !=

                // Below here the types cannot be represented entirely by a PREDEFTYPE.
                (delegate,  delegate)   :       + -         == !=

                (enum,      enum)       :         -         == != < > <= >=&| ^
                (enum,      under)      :       + -
                (under,     enum)       :       +

                (ptr,       ptr)        :         -     Not callable through dynamic
                (ptr,       int)        :       + -     Not callable through dynamic
                (ptr,       uint)       :       + -     Not callable through dynamic
                (ptr,       long)       :       + -     Not callable through dynamic
                (ptr,       ulong)      :       + -     Not callable through dynamic
                (int,       ptr)        :       +       Not callable through dynamic
                (uint,      ptr)        :       +       Not callable through dynamic
                (long,      ptr)        :       +       Not callable through dynamic
                (ulong,     ptr)        :       +       Not callable through dynamic

                (void,     void)      :                   == != < > <= >=

            There are the predefined unary operator signatures:

                int     : + -   ~
                uint    : +     ~
                long    : + -   ~
                ulong   : +     ~

                float   : + -
                double  : + -
                decimal : + -

                bool    :     !

                // Below here the types cannot be represented entirely by a PREDEFTYPE.
                enum    :       ~
                ptr     :

            Note that pointer operators cannot be lifted over nullable and are not callable through dynamic
        */

        // BinOpBindMethod and UnaOpBindMethod are method pointer arrays to dispatch the appropriate operator binder.
        // Method pointers must be in the order of the corresponding enums. We check this when the full signature is set.
        // When the binding method is looked up in these arrays we ASSERT
        // if the array is out of bounds of the corresponding array.
        private static readonly BinOpSig[] s_binopSignatures =
        {
            new BinOpSig (PredefinedType.PT_INT,        PredefinedType.PT_INT,      BinOpMask.Integer,  8, BindIntBinOp,            OpSigFlags.Value,       BinOpFuncKind.IntBinOp      ),
            new BinOpSig (PredefinedType.PT_UINT,       PredefinedType.PT_UINT,     BinOpMask.Integer,  7, BindIntBinOp,            OpSigFlags.Value,       BinOpFuncKind.IntBinOp      ),
            new BinOpSig (PredefinedType.PT_LONG,       PredefinedType.PT_LONG,     BinOpMask.Integer,  6, BindIntBinOp,            OpSigFlags.Value,       BinOpFuncKind.IntBinOp      ),
            new BinOpSig (PredefinedType.PT_ULONG,      PredefinedType.PT_ULONG,    BinOpMask.Integer,  5, BindIntBinOp,            OpSigFlags.Value,       BinOpFuncKind.IntBinOp      ),
            /* ERROR */
            new BinOpSig (PredefinedType.PT_ULONG,      PredefinedType.PT_LONG,     BinOpMask.Integer,  4, null,                    OpSigFlags.Value,       BinOpFuncKind.None          ),
            /* ERROR */
            new BinOpSig (PredefinedType.PT_LONG,       PredefinedType.PT_ULONG,    BinOpMask.Integer,  3, null,                    OpSigFlags.Value,       BinOpFuncKind.None          ),
            new BinOpSig (PredefinedType.PT_FLOAT,      PredefinedType.PT_FLOAT,    BinOpMask.Real,     1, BindRealBinOp,           OpSigFlags.Value,       BinOpFuncKind.RealBinOp     ),
            new BinOpSig (PredefinedType.PT_DOUBLE,     PredefinedType.PT_DOUBLE,   BinOpMask.Real,     0, BindRealBinOp,           OpSigFlags.Value,       BinOpFuncKind.RealBinOp     ),
            new BinOpSig (PredefinedType.PT_DECIMAL,    PredefinedType.PT_DECIMAL,  BinOpMask.Real,     0, BindDecBinOp,            OpSigFlags.Value,       BinOpFuncKind.DecBinOp      ),
            new BinOpSig (PredefinedType.PT_STRING,     PredefinedType.PT_STRING,   BinOpMask.Equal,    0, BindStrCmpOp,            OpSigFlags.Reference,   BinOpFuncKind.StrCmpOp      ),
            new BinOpSig (PredefinedType.PT_STRING,     PredefinedType.PT_STRING,   BinOpMask.Add,      2, BindStrBinOp,            OpSigFlags.Reference,   BinOpFuncKind.StrBinOp      ),
            new BinOpSig (PredefinedType.PT_STRING,     PredefinedType.PT_OBJECT,   BinOpMask.Add,      1, BindStrBinOp,            OpSigFlags.Reference,   BinOpFuncKind.StrBinOp      ),
            new BinOpSig (PredefinedType.PT_OBJECT,     PredefinedType.PT_STRING,   BinOpMask.Add,      0, BindStrBinOp,            OpSigFlags.Reference,   BinOpFuncKind.StrBinOp      ),
            new BinOpSig (PredefinedType.PT_INT,        PredefinedType.PT_INT,      BinOpMask.Shift,    3, BindShiftOp,             OpSigFlags.Value,       BinOpFuncKind.ShiftOp       ),
            new BinOpSig (PredefinedType.PT_UINT,       PredefinedType.PT_INT,      BinOpMask.Shift,    2, BindShiftOp,             OpSigFlags.Value,       BinOpFuncKind.ShiftOp       ),
            new BinOpSig (PredefinedType.PT_LONG,       PredefinedType.PT_INT,      BinOpMask.Shift,    1, BindShiftOp,             OpSigFlags.Value,       BinOpFuncKind.ShiftOp       ),
            new BinOpSig (PredefinedType.PT_ULONG,      PredefinedType.PT_INT,      BinOpMask.Shift,    0, BindShiftOp,             OpSigFlags.Value,       BinOpFuncKind.ShiftOp       ),
            new BinOpSig (PredefinedType.PT_BOOL,       PredefinedType.PT_BOOL,     BinOpMask.BoolNorm, 0, BindBoolBinOp,           OpSigFlags.Value,       BinOpFuncKind.BoolBinOp     ),
            // Make boolean logical operators liftable so that they don't give funny short circuiting semantics.
            // This is for DDBugs 677075.
            new BinOpSig (PredefinedType.PT_BOOL,       PredefinedType.PT_BOOL,     BinOpMask.Logical,  0, BindBoolBinOp,           OpSigFlags.BoolBit,     BinOpFuncKind.BoolBinOp     ),
            new BinOpSig (PredefinedType.PT_BOOL,       PredefinedType.PT_BOOL,     BinOpMask.Bitwise,  0, BindLiftedBoolBitwiseOp, OpSigFlags.BoolBit,     BinOpFuncKind.BoolBitwiseOp ),
        };

        // We want unary minus to bind to "operator -(ulong)" and then we
        // produce an error (since there is no pfn). We can't let - bind to a floating point type,
        // since they lose precision. See the language spec.

        // Increment and decrement operators are special.
        private static readonly UnaOpSig[] s_rguos =
        {
            new UnaOpSig( PredefinedType.PT_INT,        UnaOpMask.Signed,   7, BindIntUnaOp,    UnaOpFuncKind.IntUnaOp  ),
            new UnaOpSig( PredefinedType.PT_UINT,       UnaOpMask.Unsigned, 6, BindIntUnaOp,    UnaOpFuncKind.IntUnaOp  ),
            new UnaOpSig( PredefinedType.PT_LONG,       UnaOpMask.Signed,   5, BindIntUnaOp,    UnaOpFuncKind.IntUnaOp  ),
            new UnaOpSig( PredefinedType.PT_ULONG,      UnaOpMask.Unsigned, 4, BindIntUnaOp,    UnaOpFuncKind.IntUnaOp  ),
            /* ERROR */
            new UnaOpSig( PredefinedType.PT_ULONG,      UnaOpMask.Minus,    3, null,            UnaOpFuncKind.None      ),
            new UnaOpSig( PredefinedType.PT_FLOAT,      UnaOpMask.Real,     1, BindRealUnaOp,   UnaOpFuncKind.RealUnaOp ),
            new UnaOpSig( PredefinedType.PT_DOUBLE,     UnaOpMask.Real,     0, BindRealUnaOp,   UnaOpFuncKind.RealUnaOp ),
            new UnaOpSig( PredefinedType.PT_DECIMAL,    UnaOpMask.Real,     0, BindDecUnaOp,    UnaOpFuncKind.DecUnaOp  ),
            new UnaOpSig( PredefinedType.PT_BOOL,       UnaOpMask.Bool,     0, BindBoolUnaOp,   UnaOpFuncKind.BoolUnaOp ),
            new UnaOpSig( PredefinedType.PT_INT,        UnaOpMask.IncDec,   6, null,            UnaOpFuncKind.None      ),
            new UnaOpSig( PredefinedType.PT_UINT,       UnaOpMask.IncDec,   5, null,            UnaOpFuncKind.None      ),
            new UnaOpSig( PredefinedType.PT_LONG,       UnaOpMask.IncDec,   4, null,            UnaOpFuncKind.None      ),
            new UnaOpSig( PredefinedType.PT_ULONG,      UnaOpMask.IncDec,   3, null,            UnaOpFuncKind.None      ),
            new UnaOpSig( PredefinedType.PT_FLOAT,      UnaOpMask.IncDec,   1, null,            UnaOpFuncKind.None      ),
            new UnaOpSig( PredefinedType.PT_DOUBLE,     UnaOpMask.IncDec,   0, null,            UnaOpFuncKind.None      ),
            new UnaOpSig( PredefinedType.PT_DECIMAL,    UnaOpMask.IncDec,   0, null,            UnaOpFuncKind.None      ),
        };

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private ExprBinOp BindUserDefinedBinOp(ExpressionKind ek, BinOpArgInfo info)
        {
            MethPropWithInst pmpwi;
            if (info.pt1 <= PredefinedType.PT_ULONG && info.pt2 <= PredefinedType.PT_ULONG)
            {
                return null;
            }

            Expr expr;
            if (info.binopKind == BinOpKind.Logical)
            {
                // Logical operators cannot be overloaded, but use the bitwise overloads.
                ExprCall call = BindUDBinop(
                    ek - ExpressionKind.LogicalAnd + ExpressionKind.BitwiseAnd, info.arg1, info.arg2, true, out pmpwi);
                if (call == null)
                {
                    return null;
                }

                expr = BindUserBoolOp(ek, call);
            }
            else
            {
                expr = BindUDBinop(ek, info.arg1, info.arg2, false, out pmpwi);
            }

            if (expr == null)
            {
                return null;
            }

            return ExprFactory.CreateUserDefinedBinop(ek, expr.Type, info.arg1, info.arg2, expr, pmpwi);
        }

        // Adds special signatures to the candidate list.  If we find an exact match
        // then it will be the last item on the list and we return true.
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private bool GetSpecialBinopSignatures(List<BinOpFullSig> prgbofs, BinOpArgInfo info)
        {
            Debug.Assert(prgbofs != null);
            if (info.pt1 <= PredefinedType.PT_ULONG && info.pt2 <= PredefinedType.PT_ULONG)
            {
                return false;
            }
            return GetDelBinOpSigs(prgbofs, info) ||
                   GetEnumBinOpSigs(prgbofs, info) ||
                   GetRefEqualSigs(prgbofs, info);
        }

        // Adds standard and lifted signatures to the candidate list.  If we find an exact match
        // then it will be the last item on the list and we return true.

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private bool GetStandardAndLiftedBinopSignatures(List<BinOpFullSig> rgbofs, BinOpArgInfo info)
        {
            Debug.Assert(rgbofs != null);

            int ibosMinLift = 0;
            for (int ibos = 0; ibos < s_binopSignatures.Length; ibos++)
            {
                BinOpSig bos = s_binopSignatures[ibos];
                if ((bos.mask & info.mask) == 0)
                {
                    continue;
                }

                CType typeSig1 = GetPredefindType(bos.pt1);
                CType typeSig2 = GetPredefindType(bos.pt2);
                if (typeSig1 == null || typeSig2 == null)
                    continue;

                ConvKind cv1 = GetConvKind(info.pt1, bos.pt1);
                ConvKind cv2 = GetConvKind(info.pt2, bos.pt2);
                LiftFlags grflt = LiftFlags.None;

                switch (cv1)
                {
                    default:
                        Debug.Fail("Shouldn't happen!");
                        continue;

                    case ConvKind.None:
                        continue;
                    case ConvKind.Explicit:
                        if (!(info.arg1 is ExprConstant constant))
                        {
                            continue;
                        }
                        // Need to try to convert.

                        if (canConvert(constant, typeSig1))
                        {
                            break;
                        }

                        if (ibos < ibosMinLift || !bos.CanLift())
                        {
                            continue;
                        }

                        Debug.Assert(typeSig1.IsValueType);

                        typeSig1 = TypeManager.GetNullable(typeSig1);
                        if (!canConvert(constant, typeSig1))
                        {
                            continue;
                        }
                        switch (GetConvKind(info.ptRaw1, bos.pt1))
                        {
                            default:
                                grflt |= LiftFlags.Convert1;
                                break;
                            case ConvKind.Implicit:
                            case ConvKind.Identity:
                                grflt |= LiftFlags.Lift1;
                                break;
                        }
                        break;
                    case ConvKind.Unknown:
                        if (canConvert(info.arg1, typeSig1))
                        {
                            break;
                        }
                        if (ibos < ibosMinLift || !bos.CanLift())
                        {
                            continue;
                        }
                        Debug.Assert(typeSig1.IsValueType);

                        typeSig1 = TypeManager.GetNullable(typeSig1);
                        if (!canConvert(info.arg1, typeSig1))
                        {
                            continue;
                        }
                        switch (GetConvKind(info.ptRaw1, bos.pt1))
                        {
                            default:
                                grflt |= LiftFlags.Convert1;
                                break;
                            case ConvKind.Implicit:
                            case ConvKind.Identity:
                                grflt |= LiftFlags.Lift1;
                                break;
                        }
                        break;
                    case ConvKind.Implicit:
                        break;
                    case ConvKind.Identity:
                        if (cv2 == ConvKind.Identity)
                        {
                            BinOpFullSig newsig = new BinOpFullSig(this, bos);
                            if (newsig.Type1() != null && newsig.Type2() != null)
                            {
                                // Exact match.
                                rgbofs.Add(newsig);
                                return true;
                            }
                        }
                        break;
                }

                switch (cv2)
                {
                    default:
                        Debug.Fail("Shouldn't happen!");
                        continue;
                    case ConvKind.None:
                        continue;
                    case ConvKind.Explicit:
                        if (!(info.arg2 is ExprConstant constant))
                        {
                            continue;
                        }

                        // Need to try to convert.
                        if (canConvert(constant, typeSig2))
                        {
                            break;
                        }

                        if (ibos < ibosMinLift || !bos.CanLift())
                        {
                            continue;
                        }
                        Debug.Assert(typeSig2.IsValueType);

                        typeSig2 = TypeManager.GetNullable(typeSig2);
                        if (!canConvert(constant, typeSig2))
                        {
                            continue;
                        }

                        switch (GetConvKind(info.ptRaw2, bos.pt2))
                        {
                            default:
                                grflt |= LiftFlags.Convert2;
                                break;
                            case ConvKind.Implicit:
                            case ConvKind.Identity:
                                grflt |= LiftFlags.Lift2;
                                break;
                        }
                        break;
                    case ConvKind.Unknown:
                        if (canConvert(info.arg2, typeSig2))
                        {
                            break;
                        }
                        if (ibos < ibosMinLift || !bos.CanLift())
                        {
                            continue;
                        }
                        Debug.Assert(typeSig2.IsValueType);

                        typeSig2 = TypeManager.GetNullable(typeSig2);
                        if (!canConvert(info.arg2, typeSig2))
                        {
                            continue;
                        }
                        switch (GetConvKind(info.ptRaw2, bos.pt2))
                        {
                            default:
                                grflt |= LiftFlags.Convert2;
                                break;
                            case ConvKind.Implicit:
                            case ConvKind.Identity:
                                grflt |= LiftFlags.Lift2;
                                break;
                        }
                        break;
                    case ConvKind.Identity:
                    case ConvKind.Implicit:
                        break;
                }

                if (grflt != LiftFlags.None)
                {
                    // We have a lifted signature.
                    rgbofs.Add(new BinOpFullSig(typeSig1, typeSig2, bos.pfn, bos.grfos, grflt, bos.fnkind));

                    // NOTE: Can't skip any if we use a lifted signature because the
                    // type might convert to int? and to long (but not to int) in which
                    // case we should get an ambiguity. But we can skip the lifted ones....
                    ibosMinLift = ibos + bos.cbosSkip + 1;
                }
                else
                {
                    // Record it as applicable and skip accordingly.
                    rgbofs.Add(new BinOpFullSig(this, bos));
                    ibos += bos.cbosSkip;
                }
            }
            return false;
        }

        // Returns the index of the best match, or -1 if there is no best match.
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private int FindBestSignatureInList(
                List<BinOpFullSig> binopSignatures,
                BinOpArgInfo info)
        {
            Debug.Assert(binopSignatures != null);

            if (binopSignatures.Count == 1)
            {
                return 0;
            }

            int bestSignature = 0;
            int index;
            // Try to find a candidate for the best.
            for (index = 1; index < binopSignatures.Count; index++)
            {
                if (bestSignature < 0)
                {
                    bestSignature = index;
                }
                else
                {
                    int nT = WhichBofsIsBetter(binopSignatures[bestSignature], binopSignatures[index], info.type1, info.type2);
                    if (nT == 0)
                    {
                        bestSignature = -1;
                    }
                    else if (nT > 0)
                    {
                        bestSignature = index;
                    }
                }
            }

            if (bestSignature == -1)
            {
                return -1;
            }

            // Verify that the candidate really is not worse than all others.
            // Do we need to loop over the whole list here, or just
            // from 0 . bestSignature - 1?
            for (index = 0; index < binopSignatures.Count; index++)
            {
                if (index == bestSignature)
                {
                    continue;
                }
                if (WhichBofsIsBetter(binopSignatures[bestSignature], binopSignatures[index], info.type1, info.type2) >= 0)
                {
                    return -1;
                }
            }
            return bestSignature;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static ExprBinOp BindNullEqualityComparison(ExpressionKind ek, BinOpArgInfo info)
        {
            Expr arg1 = info.arg1;
            Expr arg2 = info.arg2;
            if (info.binopKind == BinOpKind.Equal)
            {
                CType typeBool = GetPredefindType(PredefinedType.PT_BOOL);
                ExprBinOp exprRes = null;
                if (info.type1 is NullableType && info.type2 is NullType)
                {
                    arg2 = ExprFactory.CreateZeroInit(info.type1);
                    exprRes = ExprFactory.CreateBinop(ek, typeBool, arg1, arg2);
                }
                if (info.type1 is NullType && info.type2 is NullableType)
                {
                    arg1 = ExprFactory.CreateZeroInit(info.type2);
                    exprRes = ExprFactory.CreateBinop(ek, typeBool, arg1, arg2);
                }
                if (exprRes != null)
                {
                    exprRes.IsLifted = true;
                    return exprRes;
                }
            }

            throw BadOperatorTypesError(info.arg1, info.arg2);
        }

        /*
            This handles binding binary operators by first checking for user defined operators, then
            applying overload resolution to the predefined operators. It handles lifting over nullable.
        */

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public Expr BindStandardBinop(ExpressionKind ek, Expr arg1, Expr arg2)
        {
            Debug.Assert(arg1 != null);
            Debug.Assert(arg2 != null);

            (BinOpKind kind, EXPRFLAG flags) = GetBinopKindAndFlags(ek);
            BinOpArgInfo info = new BinOpArgInfo(arg1, arg2)
            {
                binopKind = kind
            };

            info.mask = (BinOpMask)(1 << (int)info.binopKind);

            List<BinOpFullSig> binopSignatures = new List<BinOpFullSig>();

            // First check if this is a user defined binop. If it is, return it.
            ExprBinOp exprUD = BindUserDefinedBinOp(ek, info);
            if (exprUD != null)
            {
                return exprUD;
            }

            // Get the special binop signatures. If successful, the special binop signature will be
            // the last item in the array of signatures that we give it.

            bool exactMatch = GetSpecialBinopSignatures(binopSignatures, info);
            if (!exactMatch)
            {
                // No match, try to get standard and lifted binop signatures.

                exactMatch = GetStandardAndLiftedBinopSignatures(binopSignatures, info);
            }

            // If we have an exact match in either the special binop signatures or the standard/lifted binop
            // signatures, then we set our best match. Otherwise, we check if we had any signatures at all.
            // If we didn't, then its possible where we have x == null, where x is nullable, so try to bind
            // the null equality comparison. Otherwise, we had some ambiguity - we have a match, but its not exact.

            int bestBinopSignature;
            if (exactMatch)
            {
                Debug.Assert(binopSignatures.Count > 0);
                bestBinopSignature = binopSignatures.Count - 1;
            }
            else if (binopSignatures.Count == 0)
            {
                // If we got no matches then it's possible that we're in the case
                // x == null, where x is nullable.
                return BindNullEqualityComparison(ek, info);
            }
            else
            {
                // We had some matches, try to find the best one. FindBestSignatureInList returns < 0 if
                // we don't have a best one, otherwise it returns the index of the best one in our list that
                // we give it.

                bestBinopSignature = FindBestSignatureInList(binopSignatures, info);
                if (bestBinopSignature < 0)
                {
                    // Ambiguous.

                    throw AmbiguousOperatorError(arg1, arg2);
                }
            }

            // If we're here, we should have a binop signature that exactly matches.

            Debug.Assert(bestBinopSignature < binopSignatures.Count);

            // We've found the one to use, so lets go and bind it.

            return BindStandardBinopCore(info, binopSignatures[bestBinopSignature], ek, flags);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private Expr BindStandardBinopCore(BinOpArgInfo info, BinOpFullSig bofs, ExpressionKind ek, EXPRFLAG flags)
        {
            if (bofs.pfn == null)
            {
                throw BadOperatorTypesError(info.arg1, info.arg2);
            }

            if (!bofs.isLifted() || !bofs.AutoLift())
            {
                Expr expr1 = info.arg1;
                Expr expr2 = info.arg2;
                if (bofs.ConvertOperandsBeforeBinding())
                {
                    expr1 = mustConvert(expr1, bofs.Type1());
                    expr2 = mustConvert(expr2, bofs.Type2());
                }

                if (bofs.fnkind == BinOpFuncKind.BoolBitwiseOp)
                {
                    return BindBoolBitwiseOp(ek, flags, expr1, expr2);
                }

                return bofs.pfn(this, ek, flags, expr1, expr2);
            }
            Debug.Assert(bofs.fnkind != BinOpFuncKind.BoolBitwiseOp);
            if (IsEnumArithmeticBinOp(ek, info))
            {
                Expr expr1 = info.arg1;
                Expr expr2 = info.arg2;
                if (bofs.ConvertOperandsBeforeBinding())
                {
                    expr1 = mustConvert(expr1, bofs.Type1());
                    expr2 = mustConvert(expr2, bofs.Type2());
                }

                return BindLiftedEnumArithmeticBinOp(ek, flags, expr1, expr2);
            }

            return BindLiftedStandardBinOp(info, bofs, ek, flags);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private ExprBinOp BindLiftedStandardBinOp(BinOpArgInfo info, BinOpFullSig bofs, ExpressionKind ek, EXPRFLAG flags)
        {
            Debug.Assert(bofs.Type1() is NullableType || bofs.Type2() is NullableType);

            Expr arg1 = info.arg1;
            Expr arg2 = info.arg2;

            // We want to get the base types of the arguments and attempt to bind the non-lifted form of the
            // method so that we error report (ie divide by zero etc), and then we store in the resulting
            // binop that we have a lifted operator.

            Expr nonLiftedResult = null;

            LiftArgument(arg1, bofs.Type1(), bofs.ConvertFirst(), out Expr pArgument1, out Expr nonLiftedArg1);
            LiftArgument(arg2, bofs.Type2(), bofs.ConvertSecond(), out Expr pArgument2, out Expr nonLiftedArg2);

            // Now call the non-lifted method to generate errors, and stash the result.
            if (!nonLiftedArg1.isNull() && !nonLiftedArg2.isNull())
            {
                // Only compute the method if theres no nulls. If there are, we'll special case it
                // later, since operations with a null operand are null.
                nonLiftedResult = bofs.pfn(this, ek, flags, nonLiftedArg1, nonLiftedArg2);
            }

            // Check if we have a comparison. If so, set the result type to bool.
            CType resultType;
            if (info.binopKind == BinOpKind.Compare || info.binopKind == BinOpKind.Equal)
            {
                resultType = GetPredefindType(PredefinedType.PT_BOOL);
            }
            else
            {
                resultType = bofs.fnkind == BinOpFuncKind.EnumBinOp
                    ? GetEnumBinOpType(ek, nonLiftedArg1.Type, nonLiftedArg2.Type, out _)
                    : pArgument1.Type;

                if (!(resultType is NullableType))
                {
                    resultType = TypeManager.GetNullable(resultType);
                }
            }

            ExprBinOp exprRes = ExprFactory.CreateBinop(ek, resultType, pArgument1, pArgument2);
            mustCast(nonLiftedResult, resultType, 0);
            exprRes.IsLifted = true;
            exprRes.Flags |= flags;
            Debug.Assert((exprRes.Flags & EXPRFLAG.EXF_LVALUE) == 0);

            return exprRes;
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private void LiftArgument(Expr pArgument, CType pParameterType, bool bConvertBeforeLift,
                                            out Expr ppLiftedArgument, out Expr ppNonLiftedArgument)
        {
            Expr pLiftedArgument = mustConvert(pArgument, pParameterType);
            if (pLiftedArgument != pArgument)
            {
                MarkAsIntermediateConversion(pLiftedArgument);
            }

            Expr pNonLiftedArgument = pArgument;
            if (pParameterType is NullableType paramNub)
            {
                if (pNonLiftedArgument.isNull())
                {
                    pNonLiftedArgument = mustCast(pNonLiftedArgument, pParameterType);
                }
                pNonLiftedArgument = mustCast(pNonLiftedArgument, paramNub.UnderlyingType);
                if (bConvertBeforeLift)
                {
                    MarkAsIntermediateConversion(pNonLiftedArgument);
                }
            }
            else
            {
                pNonLiftedArgument = pLiftedArgument;
            }
            ppLiftedArgument = pLiftedArgument;
            ppNonLiftedArgument = pNonLiftedArgument;
        }

        /*
            Get the special signatures when at least one of the args is a delegate instance.
            Returns true iff an exact signature match is found.
        */
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private bool GetDelBinOpSigs(List<BinOpFullSig> prgbofs, BinOpArgInfo info)
        {
            if (!info.ValidForDelegate() || !info.type1.IsDelegateType && !info.type2.IsDelegateType)
            {
                return false;
            }

            // No conversions needed. Determine the lifting. This is the common case.
            if (info.type1 == info.type2)
            {
                prgbofs.Add(new BinOpFullSig(info.type1, info.type2, BindDelBinOp, OpSigFlags.Reference, LiftFlags.None, BinOpFuncKind.DelBinOp));
                return true;
            }

            // Now, for each delegate type, if both arguments convert to that delegate type, that is a candidate
            // for this binary operator. It's possible that we add two candidates, in which case they will compete
            // in overload resolution. Or we could add no candidates.

            bool t1tot2 = info.type2.IsDelegateType && canConvert(info.arg1, info.type2);
            bool t2tot1 = info.type1.IsDelegateType && canConvert(info.arg2, info.type1);

            if (t1tot2)
            {
                prgbofs.Add(new BinOpFullSig(info.type2, info.type2, BindDelBinOp, OpSigFlags.Reference, LiftFlags.None, BinOpFuncKind.DelBinOp));
            }

            if (t2tot1)
            {
                prgbofs.Add(new BinOpFullSig(info.type1, info.type1, BindDelBinOp, OpSigFlags.Reference, LiftFlags.None, BinOpFuncKind.DelBinOp));
            }

            // Might be ambiguous so return false.
            return false;
        }

        /*
            Utility method to determine whether arg1 is convertible to typeDst, either in a regular
            scenario or lifted scenario. Sets pgrflt, ptypeSig1 and ptypeSig2 accordingly.
        */
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private bool CanConvertArg1(BinOpArgInfo info, CType typeDst, out LiftFlags pgrflt,
                                      out CType ptypeSig1, out CType ptypeSig2)
        {
            ptypeSig1 = null;
            ptypeSig2 = null;
            Debug.Assert(!(typeDst is NullableType));

            if (canConvert(info.arg1, typeDst))
                pgrflt = LiftFlags.None;
            else
            {
                pgrflt = LiftFlags.None;
                typeDst = TypeManager.GetNullable(typeDst);
                if (!canConvert(info.arg1, typeDst))
                    return false;
                pgrflt = LiftFlags.Convert1;
            }
            ptypeSig1 = typeDst;

            if (info.type2 is NullableType)
            {
                pgrflt |= LiftFlags.Lift2;
                ptypeSig2 = TypeManager.GetNullable(info.typeRaw2);
            }
            else
                ptypeSig2 = info.typeRaw2;

            return true;
        }


        /*
            Same as CanConvertArg1 but with the indices interchanged!
        */
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private bool CanConvertArg2(BinOpArgInfo info, CType typeDst, out LiftFlags pgrflt,
                                      out CType ptypeSig1, out CType ptypeSig2)
        {
            Debug.Assert(!(typeDst is NullableType));
            ptypeSig1 = null;
            ptypeSig2 = null;

            if (canConvert(info.arg2, typeDst))
                pgrflt = LiftFlags.None;
            else
            {
                pgrflt = LiftFlags.None;
                typeDst = TypeManager.GetNullable(typeDst);
                if (!canConvert(info.arg2, typeDst))
                    return false;
                pgrflt = LiftFlags.Convert2;
            }
            ptypeSig2 = typeDst;

            if (info.type1 is NullableType)
            {
                pgrflt |= LiftFlags.Lift1;
                ptypeSig1 = TypeManager.GetNullable(info.typeRaw1);
            }
            else
                ptypeSig1 = info.typeRaw1;

            return true;
        }


        /*
            Record the appropriate binary operator full signature from the given BinOpArgInfo. This assumes
            that any NullableType valued args should be lifted.
        */
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static void RecordBinOpSigFromArgs(List<BinOpFullSig> prgbofs, BinOpArgInfo info)
        {
            LiftFlags grflt = LiftFlags.None;
            CType typeSig1;
            CType typeSig2;

            if (info.type1 != info.typeRaw1)
            {
                Debug.Assert(info.type1 is NullableType);
                grflt |= LiftFlags.Lift1;
                typeSig1 = TypeManager.GetNullable(info.typeRaw1);
            }
            else
            {
                typeSig1 = info.typeRaw1;
            }

            if (info.type2 != info.typeRaw2)
            {
                Debug.Assert(info.type2 is NullableType);
                grflt |= LiftFlags.Lift2;
                typeSig2 = TypeManager.GetNullable(info.typeRaw2);
            }
            else
            {
                typeSig2 = info.typeRaw2;
            }

            prgbofs.Add(new BinOpFullSig(typeSig1, typeSig2, BindEnumBinOp, OpSigFlags.Value, grflt, BinOpFuncKind.EnumBinOp));
        }

        /*
            Get the special signatures when at least one of the args is an enum.  Return true if
            we find an exact match.
        */
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private bool GetEnumBinOpSigs(List<BinOpFullSig> prgbofs, BinOpArgInfo info)
        {
            if (!info.typeRaw1.IsEnumType && !info.typeRaw2.IsEnumType)
            {
                return false;
            }

            // (enum,      enum)       :         -         == != < > <= >=&| ^
            // (enum,      under)      :       + -
            // (under,     enum)       :       +
            CType typeSig1 = null;
            CType typeSig2 = null;
            LiftFlags grflt = LiftFlags.None;

            // Look for the no conversions cases. Still need to determine the lifting. These are the common case.
            if (info.typeRaw1 == info.typeRaw2)
            {
                if (!info.ValidForEnum())
                {
                    return false;
                }

                RecordBinOpSigFromArgs(prgbofs, info);
                return true;
            }

            bool isValidForEnum = info.typeRaw1.IsEnumType
                ? info.typeRaw2 == info.typeRaw1.UnderlyingEnumType && info.ValidForEnumAndUnderlyingType()
                : info.typeRaw1 == info.typeRaw2.UnderlyingEnumType && info.ValidForUnderlyingTypeAndEnum();

            if (isValidForEnum)
            {
                RecordBinOpSigFromArgs(prgbofs, info);
                return true;
            }

            // Now deal with the conversion cases. Since there are no conversions from enum types to other
            // enum types we never need to do both cases.

            if (info.typeRaw1.IsEnumType)
            {
                isValidForEnum = info.ValidForEnum() && CanConvertArg2(info, info.typeRaw1, out grflt, out typeSig1, out typeSig2) ||
                    info.ValidForEnumAndUnderlyingType() && CanConvertArg2(info, info.typeRaw1.UnderlyingEnumType, out grflt, out typeSig1, out typeSig2);
            }
            else
            {
                isValidForEnum = info.ValidForEnum() && CanConvertArg1(info, info.typeRaw2, out grflt, out typeSig1, out typeSig2) ||
                    info.ValidForEnumAndUnderlyingType() && CanConvertArg1(info, info.typeRaw2.UnderlyingEnumType, out grflt, out typeSig1, out typeSig2);
            }

            if (isValidForEnum)
            {
                prgbofs.Add(new BinOpFullSig(typeSig1, typeSig2, BindEnumBinOp, OpSigFlags.Value, grflt, BinOpFuncKind.EnumBinOp));
            }
            return false;
        }

        private static bool IsEnumArithmeticBinOp(ExpressionKind ek, BinOpArgInfo info) =>
            ek switch
            {
                ExpressionKind.Add => info.typeRaw1.IsEnumType ^ info.typeRaw2.IsEnumType,
                ExpressionKind.Subtract => info.typeRaw1.IsEnumType | info.typeRaw2.IsEnumType,
                _ => false,
            };


        /*
            See if standard reference equality applies. Make sure not to return true if another == operator
            may be applicable and better (or ambiguous)! This also handles == on System.Delegate, since
            it has special rules as well.
        */
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private bool GetRefEqualSigs(List<BinOpFullSig> prgbofs, BinOpArgInfo info)
        {
            if (info.mask != BinOpMask.Equal)
            {
                return false;
            }

            if (info.type1 != info.typeRaw1 || info.type2 != info.typeRaw2)
            {
                return false;
            }

            bool fRet = false;
            CType type1 = info.type1;
            CType type2 = info.type2;
            CType typeObj = GetPredefindType(PredefinedType.PT_OBJECT);
            CType typeCls = null;

            if (type1 is NullType && type2 is NullType)
            {
                typeCls = typeObj;
                fRet = true;
            }
            else
            {

                // Check for: operator ==(System.Delegate, System.Delegate).
                CType typeDel = GetPredefindType(PredefinedType.PT_DELEGATE);
                if (canConvert(info.arg1, typeDel) && canConvert(info.arg2, typeDel) && !type1.IsDelegateType && !type2.IsDelegateType)
                {
                    prgbofs.Add(
                        new BinOpFullSig(
                            typeDel, typeDel, BindDelBinOp, OpSigFlags.Convert, LiftFlags.None,
                            BinOpFuncKind.DelBinOp));
                }

                // The reference type equality operators only handle reference types.
                Debug.Assert(type1.FundamentalType != FUNDTYPE.FT_VAR);
                if (type1.FundamentalType != FUNDTYPE.FT_REF)
                {
                    return false;
                }

                if (type2 is NullType)
                {
                    fRet = true;

                    // We don't need to determine the actual best type since we're
                    // returning true - indicating that we've found the best operator.
                    typeCls = typeObj;
                }
                else
                {
                    Debug.Assert(type2.FundamentalType != FUNDTYPE.FT_VAR);
                    if (type2.FundamentalType != FUNDTYPE.FT_REF)
                    {
                        return false;
                    }

                    if (type1 is NullType)
                    {
                        fRet = true;

                        // We don't need to determine the actual best type since we're
                        // returning true - indicating that we've found the best operator.
                        typeCls = typeObj;
                    }
                    else
                    {
                        if (!canCast(type1, type2, CONVERTTYPE.NOUDC) && !canCast(type2, type1, CONVERTTYPE.NOUDC))
                            return false;

                        if (type1.IsInterfaceType || type1.IsPredefType(PredefinedType.PT_STRING)
                            || SymbolLoader.HasBaseConversion(type1, typeDel))
                            type1 = typeObj;
                        else if (type1 is ArrayType)
                            type1 = GetPredefindType(PredefinedType.PT_ARRAY);
                        else if (!type1.IsClassType)
                            return false;

                        if (type2.IsInterfaceType || type2.IsPredefType(PredefinedType.PT_STRING)
                            || SymbolLoader.HasBaseConversion(type2, typeDel))
                            type2 = typeObj;
                        else if (type2 is ArrayType)
                            type2 = GetPredefindType(PredefinedType.PT_ARRAY);
                        else if (!type2.IsClassType)
                            return false;

                        Debug.Assert(
                            type1.IsClassType && !type1.IsPredefType(PredefinedType.PT_STRING)
                            && !type1.IsPredefType(PredefinedType.PT_DELEGATE));
                        Debug.Assert(
                            type2.IsClassType && !type2.IsPredefType(PredefinedType.PT_STRING)
                            && !type2.IsPredefType(PredefinedType.PT_DELEGATE));

                        if (SymbolLoader.HasBaseConversion(type2, type1))
                            typeCls = type1;
                        else if (SymbolLoader.HasBaseConversion(type1, type2))
                            typeCls = type2;

                    }
                }
            }

            prgbofs.Add(new BinOpFullSig(typeCls, typeCls, BindRefCmpOp, OpSigFlags.None, LiftFlags.None, BinOpFuncKind.RefCmpOp));
            return fRet;
        }

        /*
            Determine which BinOpSig is better for overload resolution.
            Better means: at least as good in all Params, and better in at least one param.

            Better w/r to a param means:
            1) same type as argument
            2) implicit conversion from this one's param type to the other's param type
            Because of user defined conversion operators this relation is not transitive.

            Returns negative if ibos1 is better, positive if ibos2 is better, 0 if neither.
        */

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private int WhichBofsIsBetter(BinOpFullSig bofs1, BinOpFullSig bofs2, CType type1, CType type2)
        {
            BetterType bt1;
            BetterType bt2;

            if (bofs1.FPreDef() && bofs2.FPreDef())
            {
                // Faster to compare predefs.
                bt1 = WhichTypeIsBetter(bofs1.pt1, bofs2.pt1, type1);
                bt2 = WhichTypeIsBetter(bofs1.pt2, bofs2.pt2, type2);
            }
            else
            {
                bt1 = WhichTypeIsBetter(bofs1.Type1(), bofs2.Type1(), type1);
                bt2 = WhichTypeIsBetter(bofs1.Type2(), bofs2.Type2(), type2);
            }

            Debug.Assert(Enum.IsDefined(typeof(BetterType), bt1));
            Debug.Assert(Enum.IsDefined(typeof(BetterType), bt2));
            int res = bt1 switch
            {
                BetterType.Left => -1,
                BetterType.Right => 1,
                _ => 0,
            };
            switch (bt2)
            {
                case BetterType.Left:
                    res--;
                    break;

                case BetterType.Right:
                    res++;
                    break;
            }

            return res;
        }


        /////////////////////////////////////////////////////////////////////////////////
        // Bind a standard unary operator. Takes care of user defined operators, predefined operators
        // and lifting over nullable.

        private static (ExpressionKind, UnaOpKind, EXPRFLAG) CalculateExprAndUnaryOpKinds(OperatorKind op, bool bChecked)
        {
            ExpressionKind ek;
            UnaOpKind uok;
            EXPRFLAG flags = 0;
            switch (op)
            {
                case OperatorKind.OP_UPLUS:
                    uok = UnaOpKind.Plus;
                    ek = ExpressionKind.UnaryPlus;
                    break;

                case OperatorKind.OP_NEG:
                    if (bChecked)
                    {
                        flags = EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    uok = UnaOpKind.Minus;
                    ek = ExpressionKind.Negate;
                    break;

                case OperatorKind.OP_BITNOT:
                    uok = UnaOpKind.Tilde;
                    ek = ExpressionKind.BitwiseNot;
                    break;

                case OperatorKind.OP_LOGNOT:
                    uok = UnaOpKind.Bang;
                    ek = ExpressionKind.LogicalNot;
                    break;

                case OperatorKind.OP_POSTINC:
                    flags = EXPRFLAG.EXF_ISPOSTOP;
                    if (bChecked)
                    {
                        flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    uok = UnaOpKind.IncDec;
                    ek = ExpressionKind.Add;
                    break;

                case OperatorKind.OP_PREINC:
                    if (bChecked)
                    {
                        flags = EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    uok = UnaOpKind.IncDec;
                    ek = ExpressionKind.Add;
                    break;

                case OperatorKind.OP_POSTDEC:
                    flags = EXPRFLAG.EXF_ISPOSTOP;
                    if (bChecked)
                    {
                        flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    uok = UnaOpKind.IncDec;
                    ek = ExpressionKind.Subtract;
                    break;

                case OperatorKind.OP_PREDEC:
                    if (bChecked)
                    {
                        flags = EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    uok = UnaOpKind.IncDec;
                    ek = ExpressionKind.Subtract;
                    break;

                default:
                    Debug.Fail($"Bad op: {op}");
                    throw Error.InternalCompilerError();
            }

            return (ek, uok, flags);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public Expr BindStandardUnaryOperator(OperatorKind op, Expr pArgument)
        {
            Debug.Assert(pArgument != null);

            CType type = pArgument.Type;
            Debug.Assert(type != null);
            if (type is NullableType nub)
            {
                CType nonNub = nub.UnderlyingType;
                if (nonNub.IsEnumType)
                {
                    PredefinedType ptOp = nonNub.FundamentalType switch
                    {
                        FUNDTYPE.FT_U4 => PredefinedType.PT_UINT,
                        FUNDTYPE.FT_I8 => PredefinedType.PT_LONG,
                        FUNDTYPE.FT_U8 => PredefinedType.PT_ULONG,
                        _ => PredefinedType.PT_INT, // Promote all smaller types to int.
                    };
                    return mustCast(
                        BindStandardUnaryOperator(
                            op, mustCast(pArgument, TypeManager.GetNullable(GetPredefindType(ptOp)))), nub);
                }
            }

            (ExpressionKind ek, UnaOpKind unaryOpKind, EXPRFLAG flags) =
                CalculateExprAndUnaryOpKinds(op, Context.Checked);

            UnaOpMask unaryOpMask = (UnaOpMask)(1 << (int)unaryOpKind);

            List<UnaOpFullSig> pSignatures = new List<UnaOpFullSig>();

            UnaryOperatorSignatureFindResult eResultOfSignatureFind = PopulateSignatureList(pArgument, unaryOpKind, unaryOpMask, ek, flags, pSignatures, out Expr pResult);

            // nBestSignature is a 0-based index.
            int nBestSignature = pSignatures.Count - 1;

            if (eResultOfSignatureFind == UnaryOperatorSignatureFindResult.Return)
            {
                Debug.Assert(pResult != null);
                return pResult;
            }
            else if (eResultOfSignatureFind != UnaryOperatorSignatureFindResult.Match)
            {
                // If we didn't find a best match while populating, try to find while doing
                // applicability testing.
                if (!FindApplicableSignatures(
                            pArgument,
                            unaryOpMask,
                            pSignatures))
                {
                    if (pSignatures.Count == 0)
                    {
                        throw BadOperatorTypesError(pArgument, null);
                    }

                    nBestSignature = 0;
                    // If we couldn't find exactly one, then we need to do some betterness testing.
                    if (pSignatures.Count != 1)
                    {
                        // Determine which is best.
                        for (int iuofs = 1; iuofs < pSignatures.Count; iuofs++)
                        {
                            if (nBestSignature < 0)
                            {
                                nBestSignature = iuofs;
                            }
                            else
                            {
                                int nT = WhichUofsIsBetter(pSignatures[nBestSignature], pSignatures[iuofs], type);
                                if (nT == 0)
                                {
                                    nBestSignature = -1;
                                }
                                else if (nT > 0)
                                {
                                    nBestSignature = iuofs;
                                }
                            }
                        }
                        if (nBestSignature < 0)
                        {
                            // Ambiguous.
                            throw AmbiguousOperatorError(pArgument, null);
                        }

                        // Verify that our answer works.
                        for (int iuofs = 0; iuofs < pSignatures.Count; iuofs++)
                        {
                            if (iuofs == nBestSignature)
                            {
                                continue;
                            }
                            if (WhichUofsIsBetter(pSignatures[nBestSignature], pSignatures[iuofs], type) >= 0)
                            {
                                throw AmbiguousOperatorError(pArgument, null);
                            }
                        }
                    }
                }
                else
                {
                    nBestSignature = pSignatures.Count - 1;
                }
            }

            Debug.Assert(nBestSignature < pSignatures.Count);

            UnaOpFullSig uofs = pSignatures[nBestSignature];

            if (uofs.pfn == null)
            {
                if (unaryOpKind == UnaOpKind.IncDec)
                {
                    return BindIncOp(ek, flags, pArgument, uofs);
                }

                throw BadOperatorTypesError(pArgument, null);
            }

            if (uofs.isLifted())
            {
                return BindLiftedStandardUnop(ek, flags, pArgument, uofs);
            }

            if (pArgument is ExprConstant)
            {
                // Wrap the constant in an identity cast, to force the later casts to not be optimised out.
                // The ExpressionTreeRewriter will remove this again.
                pArgument = ExprFactory.CreateCast(pArgument.Type, pArgument);
            }

            // Try the conversion - if it fails, do a cast without user defined casts.
            Expr arg =
                tryConvert(pArgument, uofs.GetType()) ??
                mustCast(pArgument, uofs.GetType(), CONVERTTYPE.NOUDC);

            return uofs.pfn(this, ek, flags, arg);
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private UnaryOperatorSignatureFindResult PopulateSignatureList(Expr pArgument, UnaOpKind unaryOpKind, UnaOpMask unaryOpMask, ExpressionKind exprKind, EXPRFLAG flags, List<UnaOpFullSig> pSignatures, out Expr ppResult)
        {
            // We should have already checked argument != null and argument.type != null.
            Debug.Assert(pArgument != null);
            Debug.Assert(pArgument.Type != null);

            ppResult = null;
            CType pArgumentType = pArgument.Type;
            CType pRawType = pArgumentType.StripNubs();
            PredefinedType ptRaw = pRawType.IsPredefined ? pRawType.PredefinedType : PredefinedType.PT_COUNT;

            // Find all applicable operator signatures.
            // First check for special ones (enum, ptr) and check for user defined ops.

            if (ptRaw > PredefinedType.PT_ULONG)
            {
                // Enum types are special in that they carry a set of "predefined" operators (~ and inc/dec).
                if (pRawType.IsEnumType)
                {
                    // Nullable enums are dealt with already.
                    Debug.Assert(pRawType == pArgumentType);
                    Debug.Assert(pArgumentType is AggregateType);
                    if ((unaryOpMask & (UnaOpMask.Tilde | UnaOpMask.IncDec)) != 0)
                    {
                        // We have an exact match.
                        if (unaryOpKind == UnaOpKind.Tilde)
                        {
                            pSignatures.Add(new UnaOpFullSig(
                                    ((AggregateType)pArgumentType).OwningAggregate.GetUnderlyingType(),
                                    BindEnumUnaOp,
                                    LiftFlags.None,
                                    UnaOpFuncKind.EnumUnaOp));
                        }
                        else
                        {
                            // For enums, we want to add the signature as the underlying type so that we'll
                            // perform the conversions to and from the enum type.
                            pSignatures.Add(new UnaOpFullSig(
                                    ((AggregateType)pArgumentType).OwningAggregate.GetUnderlyingType(),
                                    null,
                                    LiftFlags.None,
                                    UnaOpFuncKind.None));
                        }

                        return UnaryOperatorSignatureFindResult.Match;
                    }
                }
                else if (unaryOpKind == UnaOpKind.IncDec)
                {
                    Debug.Assert(!(pArgumentType is PointerType));

                    // Check for user defined inc/dec
                    ExprMultiGet exprGet = ExprFactory.CreateMultiGet(0, pArgumentType, null);

                    Expr exprVal = bindUDUnop((ExpressionKind)(exprKind - ExpressionKind.Add + ExpressionKind.Inc), exprGet);
                    if (exprVal != null)
                    {
                        if (exprVal.Type != null && exprVal.Type != pArgumentType)
                        {
                            exprVal = mustConvert(exprVal, pArgumentType);
                        }

                        Debug.Assert(pArgument != null);
                        ExprMulti exprMulti = ExprFactory.CreateMulti(EXPRFLAG.EXF_ASSGOP | flags, pArgumentType, pArgument, exprVal);
                        exprGet.OptionalMulti = exprMulti;

                        // Check whether Lvalue can be assigned.
                        CheckLvalue(pArgument, CheckLvalueKind.Increment);
                        ppResult = exprMulti;
                        return UnaryOperatorSignatureFindResult.Return;
                    }
                    // Try for a predefined increment operator.
                }
                else
                {
                    // Check for user defined.
                    Expr expr = bindUDUnop(exprKind, pArgument);
                    if (expr != null)
                    {
                        ppResult = expr;
                        return UnaryOperatorSignatureFindResult.Return;
                    }
                }
            }

            return UnaryOperatorSignatureFindResult.Continue;
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private bool FindApplicableSignatures(
                Expr pArgument,
                UnaOpMask unaryOpMask,
                List<UnaOpFullSig> pSignatures)
        {
            // All callers should already assert this to be the case.
            Debug.Assert(pArgument != null);
            Debug.Assert(pArgument.Type != null);

            long iuosMinLift = 0;

            CType pArgumentType = pArgument.Type;
            CType pRawType = pArgumentType.StripNubs();
            PredefinedType pt = pArgumentType.IsPredefined ? pArgumentType.PredefinedType : PredefinedType.PT_COUNT;
            PredefinedType ptRaw = pRawType.IsPredefined ? pRawType.PredefinedType : PredefinedType.PT_COUNT;

            for (int index = 0; index < s_rguos.Length; index++)
            {
                UnaOpSig uos = s_rguos[index];
                if ((uos.grfuom & unaryOpMask) == 0)
                {
                    continue;
                }

                ConvKind cv = GetConvKind(pt, s_rguos[index].pt);
                CType typeSig = null;

                switch (cv)
                {
                    default:
                        Debug.Fail("Shouldn't happen!");
                        continue;

                    case ConvKind.None:
                        continue;

                    case ConvKind.Explicit:
                        if (!(pArgument is ExprConstant))
                        {
                            continue;
                        }

                        if (canConvert(pArgument, typeSig = GetPredefindType(uos.pt)))
                        {
                            break;
                        }
                        if (index < iuosMinLift)
                        {
                            continue;
                        }
                        typeSig = TypeManager.GetNullable(typeSig);
                        if (!canConvert(pArgument, typeSig))
                        {
                            continue;
                        }
                        break;

                    case ConvKind.Unknown:
                        if (canConvert(pArgument, typeSig = GetPredefindType(uos.pt)))
                        {
                            break;
                        }
                        if (index < iuosMinLift)
                        {
                            continue;
                        }
                        typeSig = TypeManager.GetNullable(typeSig);
                        if (!canConvert(pArgument, typeSig))
                        {
                            continue;
                        }
                        break;

                    case ConvKind.Implicit:
                        break;

                    case ConvKind.Identity:
                        {
                            UnaOpFullSig result = new UnaOpFullSig(this, uos);
                            if (result.GetType() != null)
                            {
                                pSignatures.Add(result);
                                return true;
                            }
                        }
                        break;
                }

                if (typeSig is NullableType)
                {
                    // Need to use a lifted signature.
                    LiftFlags grflt = LiftFlags.None;

                    switch (GetConvKind(ptRaw, uos.pt))
                    {
                        default:
                            grflt |= LiftFlags.Convert1;
                            break;
                        case ConvKind.Implicit:
                        case ConvKind.Identity:
                            grflt |= LiftFlags.Lift1;
                            break;
                    }

                    pSignatures.Add(new UnaOpFullSig(typeSig, uos.pfn, grflt, uos.fnkind));

                    // NOTE: Can't skip any if we use the lifted signature because the
                    // type might convert to int? and to long (but not to int) in which
                    // case we should get an ambiguity. But we can skip the lifted ones....
                    iuosMinLift = index + uos.cuosSkip + 1;
                }
                else
                {
                    // Record it as applicable and skip accordingly.
                    UnaOpFullSig newResult = new UnaOpFullSig(this, uos);
                    if (newResult.GetType() != null)
                    {
                        pSignatures.Add(newResult);
                    }
                    index += uos.cuosSkip;
                }
            }
            return false;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private ExprOperator BindLiftedStandardUnop(ExpressionKind ek, EXPRFLAG flags, Expr arg, UnaOpFullSig uofs)
        {
            NullableType type = uofs.GetType() as NullableType;
            Debug.Assert(arg?.Type != null);
            if (arg.Type is NullType)
            {
                throw BadOperatorTypesError(arg, null);
            }

            LiftArgument(arg, uofs.GetType(), uofs.Convert(), out Expr pArgument, out Expr nonLiftedArg);

            // Now call the function with the non lifted arguments to report errors.
            Expr nonLiftedResult = uofs.pfn(this, ek, flags, nonLiftedArg);
            ExprUnaryOp exprRes = ExprFactory.CreateUnaryOp(ek, type, pArgument);
            mustCast(nonLiftedResult, type, 0);
            exprRes.Flags |= flags;

            Debug.Assert((exprRes.Flags & EXPRFLAG.EXF_LVALUE) == 0);
            return exprRes;
        }

        /*
            Determine which UnaOpSig is better for overload resolution.
            Returns negative if iuos1 is better, positive if iuos2 is better, 0 if neither.
        */
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private int WhichUofsIsBetter(UnaOpFullSig uofs1, UnaOpFullSig uofs2, CType typeArg)
        {
            BetterType bt;

            if (uofs1.FPreDef() && uofs2.FPreDef())
            {
                // Faster to compare predefs.
                bt = WhichTypeIsBetter(uofs1.pt, uofs2.pt, typeArg);
            }
            else
            {
                bt = WhichTypeIsBetter(uofs1.GetType(), uofs2.GetType(), typeArg);
            }

            Debug.Assert(Enum.IsDefined(typeof(BetterType), bt));
            return bt switch
            {
                BetterType.Left => -1,
                BetterType.Right => +1,
                _ => 0,
            };
        }

        /*
            Handles standard binary integer based operators.
        */
        private static ExprOperator BindIntBinOp(ExpressionBinder binder, ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(arg1.Type.IsPredefined && arg2.Type.IsPredefined && arg1.Type.PredefinedType == arg2.Type.PredefinedType);
            return binder.BindIntOp(ek, flags, arg1, arg2, arg1.Type.PredefinedType);
        }


        /*
            Handles standard unary integer based operators.
        */
        private static ExprOperator BindIntUnaOp(ExpressionBinder binder, ExpressionKind ek, EXPRFLAG flags, Expr arg)
        {
            Debug.Assert(arg.Type.IsPredefined);
            return binder.BindIntOp(ek, flags, arg, null, arg.Type.PredefinedType);
        }


        /*
            Handles standard binary floating point (float, double) based operators.
        */
        private static ExprOperator BindRealBinOp(ExpressionBinder binder, ExpressionKind ek, EXPRFLAG _, Expr arg1, Expr arg2)
        {
            Debug.Assert(arg1.Type.IsPredefined && arg2.Type.IsPredefined && arg1.Type.PredefinedType == arg2.Type.PredefinedType);
            return BindFloatOp(ek, arg1, arg2);
        }


        /*
            Handles standard unary floating point (float, double) based operators.
        */
        private static ExprOperator BindRealUnaOp(ExpressionBinder binder, ExpressionKind ek, EXPRFLAG _, Expr arg)
        {
            Debug.Assert(arg.Type.IsPredefined);
            return BindFloatOp(ek, arg, null);
        }


        /*
            Handles standard increment and decrement operators.
        */
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private Expr BindIncOp(ExpressionKind ek, EXPRFLAG flags, Expr arg, UnaOpFullSig uofs)
        {
            Debug.Assert(ek == ExpressionKind.Add || ek == ExpressionKind.Subtract);

            CheckLvalue(arg, CheckLvalueKind.Increment);
            CType typeRaw = uofs.GetType().StripNubs();

            FUNDTYPE ft = typeRaw.FundamentalType;
            if (ft == FUNDTYPE.FT_R8 || ft == FUNDTYPE.FT_R4)
            {
                flags &= ~EXPRFLAG.EXF_CHECKOVERFLOW;
            }

            if (uofs.isLifted())
            {
                return BindLiftedIncOp(ek, flags, arg, uofs);
            }
            else
            {
                return BindNonliftedIncOp(ek, flags, arg, uofs);
            }
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private Expr BindIncOpCore(ExpressionKind ek, EXPRFLAG flags, Expr exprVal, CType type)
        {
            Debug.Assert(ek == ExpressionKind.Add || ek == ExpressionKind.Subtract);
            ConstVal cv;

            if (type.IsEnumType && type.FundamentalType > FUNDTYPE.FT_LASTINTEGRAL)
            {
                // This is an error case when enum derives from an illegal type. Just treat it as an int.
                type = GetPredefindType(PredefinedType.PT_INT);
            }

            Debug.Assert(type.FundamentalType != FUNDTYPE.FT_PTR); // Can't have a pointer.
            switch (type.FundamentalType)
            {
                default:
                    Debug.Assert(type.IsPredefType(PredefinedType.PT_DECIMAL));
                    PREDEFMETH predefMeth;
                    if (ek == ExpressionKind.Add)
                    {
                        ek = ExpressionKind.DecimalInc;
                        predefMeth = PREDEFMETH.PM_DECIMAL_OPINCREMENT;
                    }
                    else
                    {
                        ek = ExpressionKind.DecimalDec;
                        predefMeth = PREDEFMETH.PM_DECIMAL_OPDECREMENT;
                    }

                    return CreateUnaryOpForPredefMethodCall(ek, predefMeth, type, exprVal);

                case FUNDTYPE.FT_I1:
                case FUNDTYPE.FT_I2:
                case FUNDTYPE.FT_U1:
                case FUNDTYPE.FT_U2:
                    type = GetPredefindType(PredefinedType.PT_INT);
                    cv = ConstVal.Get(1);
                    break;

                case FUNDTYPE.FT_I4:
                case FUNDTYPE.FT_U4:
                    cv = ConstVal.Get(1);
                    break;

                case FUNDTYPE.FT_I8:
                case FUNDTYPE.FT_U8:
                    cv = ConstVal.Get((long)1);
                    break;

                case FUNDTYPE.FT_R4:
                case FUNDTYPE.FT_R8:
                    cv = ConstVal.Get(1.0);
                    break;
            }

            return LScalar(ek, flags, exprVal, type, cv, type);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private Expr LScalar(ExpressionKind ek, EXPRFLAG flags, Expr exprVal, CType type, ConstVal cv, CType typeTmp)
        {
            CType typeOne = type;
            if (typeOne.IsEnumType)
            {
                typeOne = typeOne.UnderlyingEnumType;
            }

            ExprBinOp pExprResult = ExprFactory.CreateBinop(ek, typeTmp, exprVal, ExprFactory.CreateConstant(typeOne, cv));
            pExprResult.Flags |= flags;
            return typeTmp != type ? mustCast(pExprResult, type, CONVERTTYPE.NOUDC) : pExprResult;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private ExprMulti BindNonliftedIncOp(ExpressionKind ek, EXPRFLAG flags, Expr arg, UnaOpFullSig uofs)
        {
            Debug.Assert(ek == ExpressionKind.Add || ek == ExpressionKind.Subtract);
            Debug.Assert(!uofs.isLifted());

            Debug.Assert(arg != null);
            ExprMultiGet exprGet = ExprFactory.CreateMultiGet(EXPRFLAG.EXF_ASSGOP, arg.Type, null);
            Expr exprVal = exprGet;
            CType type = uofs.GetType();
            Debug.Assert(!(type is NullableType));

            // These used to be converts, but we're making them casts now - this is because
            // we need to remove the ability to call inc(sbyte) etc for all types smaller than int.
            // Note however, that this will give us different error messages on compile time versus runtime
            // for checked increments.
            //
            // Also, we changed it so that we now generate the cast to and from enum for enum increments.
            exprVal = mustCast(exprVal, type);
            exprVal = BindIncOpCore(ek, flags, exprVal, type);
            Expr op = mustCast(exprVal, arg.Type, CONVERTTYPE.NOUDC);

            ExprMulti exprMulti = ExprFactory.CreateMulti(EXPRFLAG.EXF_ASSGOP | flags, arg.Type, arg, op);
            exprGet.OptionalMulti = exprMulti;
            return exprMulti;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private ExprMulti BindLiftedIncOp(ExpressionKind ek, EXPRFLAG flags, Expr arg, UnaOpFullSig uofs)
        {
            Debug.Assert(ek == ExpressionKind.Add || ek == ExpressionKind.Subtract);
            Debug.Assert(uofs.isLifted());

            NullableType type = uofs.GetType() as NullableType;
            Debug.Assert(arg != null);

            ExprMultiGet exprGet = ExprFactory.CreateMultiGet(EXPRFLAG.EXF_ASSGOP, arg.Type, null);
            Expr exprVal = exprGet;
            Expr nonLiftedArg = exprVal;

            // We want to give the lifted argument as the binop, but use the non-lifted argument as the
            // argument of the call.
            //Debug.Assert(uofs.LiftArg() || type.IsValType());
            nonLiftedArg = mustCast(nonLiftedArg, type.UnderlyingType);
            Expr nonLiftedResult = BindIncOpCore(ek, flags, nonLiftedArg, type.UnderlyingType);
            exprVal = mustCast(exprVal, type);
            ExprUnaryOp exprRes = ExprFactory.CreateUnaryOp((ek == ExpressionKind.Add) ? ExpressionKind.Inc : ExpressionKind.Dec, arg.Type/* type */, exprVal);
            mustCast(mustCast(nonLiftedResult, type), arg.Type);
            exprRes.Flags |= flags;

            ExprMulti exprMulti = ExprFactory.CreateMulti(EXPRFLAG.EXF_ASSGOP | flags, arg.Type, arg, exprRes);
            exprGet.OptionalMulti = exprMulti;
            return exprMulti;
        }

        /*
            Handles standard binary decimal based operators.
            This function is called twice by the EE for every binary operator it evaluates
            Here is how it works.
        */
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "All types used here are builtin and will not be trimmed.")]
        private static ExprBinOp BindDecBinOp(ExpressionBinder _, ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(arg1.Type.IsPredefType(PredefinedType.PT_DECIMAL) && arg2.Type.IsPredefType(PredefinedType.PT_DECIMAL));

            CType typeDec = GetPredefindType(PredefinedType.PT_DECIMAL);
            Debug.Assert(typeDec != null);

            CType typeRet;

            switch (ek)
            {
                default:
                    Debug.Fail($"Bad kind: {ek}");
                    typeRet = null;
                    break;
                case ExpressionKind.Add:
                case ExpressionKind.Subtract:
                case ExpressionKind.Multiply:
                case ExpressionKind.Divide:
                case ExpressionKind.Modulo:
                    typeRet = typeDec;
                    break;
                case ExpressionKind.LessThan:
                case ExpressionKind.LessThanOrEqual:
                case ExpressionKind.GreaterThan:
                case ExpressionKind.GreaterThanOrEqual:
                case ExpressionKind.Eq:
                case ExpressionKind.NotEq:
                    typeRet = GetPredefindType(PredefinedType.PT_BOOL);
                    break;
            }

            return ExprFactory.CreateBinop(ek, typeRet, arg1, arg2);
        }


        /*
            Handles standard unary decimal based operators.
        */

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "All types used here are builtin and will not be trimmed.")]
        private static ExprUnaryOp BindDecUnaOp(ExpressionBinder _, ExpressionKind ek, EXPRFLAG flags, Expr arg)
        {
            Debug.Assert(arg.Type.IsPredefType(PredefinedType.PT_DECIMAL));
            Debug.Assert(ek == ExpressionKind.Negate || ek == ExpressionKind.UnaryPlus);

            CType typeDec = GetPredefindType(PredefinedType.PT_DECIMAL);
            Debug.Assert(typeDec != null);

            if (ek == ExpressionKind.Negate)
            {
                PREDEFMETH predefMeth = PREDEFMETH.PM_DECIMAL_OPUNARYMINUS;
                return CreateUnaryOpForPredefMethodCall(ExpressionKind.DecimalNegate, predefMeth, typeDec, arg);
            }
            return ExprFactory.CreateUnaryOp(ExpressionKind.UnaryPlus, typeDec, arg);
        }


        /*
            Handles string concatenation.
        */
        private static Expr BindStrBinOp(ExpressionBinder _, ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(ek == ExpressionKind.Add);
            Debug.Assert(arg1.Type.IsPredefType(PredefinedType.PT_STRING) || arg2.Type.IsPredefType(PredefinedType.PT_STRING));
            return BindStringConcat(arg1, arg2);
        }


        /*
            Bind a shift operator: <<, >>. These can have integer or long first operands,
            and second operand must be int.
        */
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "All types used here are builtin and will not be trimmed.")]
        private static ExprBinOp BindShiftOp(ExpressionBinder _, ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(ek == ExpressionKind.LeftShirt || ek == ExpressionKind.RightShift);
            Debug.Assert(arg1.Type.IsPredefined);
            Debug.Assert(arg2.Type.IsPredefType(PredefinedType.PT_INT));

            PredefinedType ptOp = arg1.Type.PredefinedType;
            Debug.Assert(ptOp == PredefinedType.PT_INT || ptOp == PredefinedType.PT_UINT || ptOp == PredefinedType.PT_LONG || ptOp == PredefinedType.PT_ULONG);

            return ExprFactory.CreateBinop(ek, arg1.Type, arg1, arg2);
        }

        /*
            Bind a bool binary operator: ==, !=, &&, ||, , |, ^. If both operands are constant, the
            result will be a constant also.
        */
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "All types used here are builtin and will not be trimmed.")]
        private static ExprBinOp BindBoolBinOp(ExpressionBinder _, ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(arg1 != null);
            Debug.Assert(arg2 != null);
            Debug.Assert(arg1.Type.IsPredefType(PredefinedType.PT_BOOL) || arg1.Type is NullableType argNubType1 && argNubType1.UnderlyingType.IsPredefType(PredefinedType.PT_BOOL));
            Debug.Assert(arg2.Type.IsPredefType(PredefinedType.PT_BOOL) || arg2.Type is NullableType argNubType2 && argNubType2.UnderlyingType.IsPredefType(PredefinedType.PT_BOOL));

            return ExprFactory.CreateBinop(ek, GetPredefindType(PredefinedType.PT_BOOL), arg1, arg2);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private ExprOperator BindBoolBitwiseOp(ExpressionKind ek, EXPRFLAG flags, Expr expr1, Expr expr2)
        {
            Debug.Assert(ek == ExpressionKind.BitwiseAnd || ek == ExpressionKind.BitwiseOr);
            Debug.Assert(expr1.Type.IsPredefType(PredefinedType.PT_BOOL) || expr1.Type is NullableType expNubType1 && expNubType1.UnderlyingType.IsPredefType(PredefinedType.PT_BOOL));
            Debug.Assert(expr2.Type.IsPredefType(PredefinedType.PT_BOOL) || expr2.Type is NullableType expNubType2 && expNubType2.UnderlyingType.IsPredefType(PredefinedType.PT_BOOL));

            if (expr1.Type is NullableType || expr2.Type is NullableType)
            {
                CType typeBool = GetPredefindType(PredefinedType.PT_BOOL);
                CType typeRes = TypeManager.GetNullable(typeBool);

                // Get the non-lifted result.
                Expr nonLiftedArg1 = StripNullableConstructor(expr1);
                Expr nonLiftedArg2 = StripNullableConstructor(expr2);
                Expr nonLiftedResult = null;

                if (!(nonLiftedArg1.Type is NullableType) && !(nonLiftedArg2.Type is NullableType))
                {
                    nonLiftedResult = BindBoolBinOp(this, ek, flags, nonLiftedArg1, nonLiftedArg2);
                }

                // Make the binop and set that its lifted.
                ExprBinOp exprRes = ExprFactory.CreateBinop(ek, typeRes, expr1, expr2);
                if (nonLiftedResult != null)
                {
                    // Bitwise operators can have null non-lifted results if we have a nub sym somewhere.
                    mustCast(nonLiftedResult, typeRes, 0);
                }
                exprRes.IsLifted = true;
                exprRes.Flags |= flags;
                Debug.Assert((exprRes.Flags & EXPRFLAG.EXF_LVALUE) == 0);
                return exprRes;
            }

            return BindBoolBinOp(this, ek, flags, expr1, expr2);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "All types used here are builtin and will not be trimmed.")]
        private static Expr BindLiftedBoolBitwiseOp(ExpressionBinder _, ExpressionKind ek, EXPRFLAG flags, Expr expr1, Expr expr2) => null;

        /*
            Handles boolean unary operator (!).
        */
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "All types used here are builtin and will not be trimmed.")]
        private static Expr BindBoolUnaOp(ExpressionBinder _, ExpressionKind ek, EXPRFLAG flags, Expr arg)
        {
            Debug.Assert(arg.Type.IsPredefType(PredefinedType.PT_BOOL));
            Debug.Assert(ek == ExpressionKind.LogicalNot);

            // Get the result type and operand type.
            CType typeBool = GetPredefindType(PredefinedType.PT_BOOL);

            // Determine if arg has a constant value.
            // Strip off EXPRKIND.EK_SEQUENCE for constant checking.

            Expr argConst = arg.GetConst();

            if (argConst == null)
                return ExprFactory.CreateUnaryOp(ExpressionKind.LogicalNot, typeBool, arg);

            return ExprFactory.CreateConstant(typeBool, ConstVal.Get(((ExprConstant)argConst).Val.Int32Val == 0));
        }


        /*
            Handles string equality.
        */
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "All types used here are builtin and will not be trimmed.")]
        private static ExprBinOp BindStrCmpOp(ExpressionBinder _, ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(ek == ExpressionKind.Eq || ek == ExpressionKind.NotEq);
            Debug.Assert(arg1.Type.IsPredefType(PredefinedType.PT_STRING) && arg2.Type.IsPredefType(PredefinedType.PT_STRING));

            // Get the predefined method for string comparison, and then stash it in the Expr so we can
            // transform it later.

            PREDEFMETH predefMeth = ek == ExpressionKind.Eq ? PREDEFMETH.PM_STRING_OPEQUALITY : PREDEFMETH.PM_STRING_OPINEQUALITY;
            ek = ek == ExpressionKind.Eq ? ExpressionKind.StringEq : ExpressionKind.StringNotEq;
            return CreateBinopForPredefMethodCall(ek, predefMeth, GetPredefindType(PredefinedType.PT_BOOL), arg1, arg2);
        }


        /*
            Handles reference equality operators. Type variables come through here.
        */
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static ExprBinOp BindRefCmpOp(ExpressionBinder binder, ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(ek == ExpressionKind.Eq || ek == ExpressionKind.NotEq);

            // Must box type variables for the verifier.
            arg1 = binder.mustConvert(arg1, GetPredefindType(PredefinedType.PT_OBJECT), CONVERTTYPE.NOUDC);
            arg2 = binder.mustConvert(arg2, GetPredefindType(PredefinedType.PT_OBJECT), CONVERTTYPE.NOUDC);

            return ExprFactory.CreateBinop(ek, GetPredefindType(PredefinedType.PT_BOOL), arg1, arg2);
        }


        /*
            Handles delegate binary operators.
        */
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static Expr BindDelBinOp(ExpressionBinder _, ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(ek == ExpressionKind.Add || ek == ExpressionKind.Subtract || ek == ExpressionKind.Eq || ek == ExpressionKind.NotEq);
            Debug.Assert(arg1.Type == arg2.Type && (arg1.Type.IsDelegateType || arg1.Type.IsPredefType(PredefinedType.PT_DELEGATE)));

            PREDEFMETH predefMeth = (PREDEFMETH)0;
            CType RetType = null;
            switch (ek)
            {
                case ExpressionKind.Add:
                    predefMeth = PREDEFMETH.PM_DELEGATE_COMBINE;
                    RetType = arg1.Type;
                    ek = ExpressionKind.DelegateAdd;
                    break;

                case ExpressionKind.Subtract:
                    predefMeth = PREDEFMETH.PM_DELEGATE_REMOVE;
                    RetType = arg1.Type;
                    ek = ExpressionKind.DelegateSubtract;
                    break;

                case ExpressionKind.Eq:
                    predefMeth = PREDEFMETH.PM_DELEGATE_OPEQUALITY;
                    RetType = GetPredefindType(PredefinedType.PT_BOOL);
                    ek = ExpressionKind.DelegateEq;
                    break;

                case ExpressionKind.NotEq:
                    predefMeth = PREDEFMETH.PM_DELEGATE_OPINEQUALITY;
                    RetType = GetPredefindType(PredefinedType.PT_BOOL);
                    ek = ExpressionKind.DelegateNotEq;
                    break;
            }
            return CreateBinopForPredefMethodCall(ek, predefMeth, RetType, arg1, arg2);
        }


        /*
            Handles enum binary operators.
        */
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static Expr BindEnumBinOp(ExpressionBinder binder, ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            AggregateType typeDst = GetEnumBinOpType(ek, arg1.Type, arg2.Type, out AggregateType typeEnum);

            Debug.Assert(typeEnum != null);

            PredefinedType ptOp = typeEnum.FundamentalType switch
            {
                FUNDTYPE.FT_U4 => PredefinedType.PT_UINT,
                FUNDTYPE.FT_I8 => PredefinedType.PT_LONG,
                FUNDTYPE.FT_U8 => PredefinedType.PT_ULONG,
                _ => PredefinedType.PT_INT, // Promote all smaller types to int.
            };
            CType typeOp = GetPredefindType(ptOp);
            arg1 = binder.mustCast(arg1, typeOp, CONVERTTYPE.NOUDC);
            arg2 = binder.mustCast(arg2, typeOp, CONVERTTYPE.NOUDC);

            Expr exprRes = binder.BindIntOp(ek, flags, arg1, arg2, ptOp);

            if (exprRes.Type != typeDst)
            {
                Debug.Assert(!typeDst.IsPredefType(PredefinedType.PT_BOOL));
                exprRes = binder.mustCast(exprRes, typeDst, CONVERTTYPE.NOUDC);
            }

            return exprRes;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private Expr BindLiftedEnumArithmeticBinOp(ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(ek == ExpressionKind.Add || ek == ExpressionKind.Subtract);
            CType nonNullableType1 = arg1.Type is NullableType arg1NubType ? arg1NubType.UnderlyingType : arg1.Type;
            CType nonNullableType2 = arg2.Type is NullableType arg2NubType ? arg2NubType.UnderlyingType : arg2.Type;
            if (nonNullableType1 is NullType)
            {
                nonNullableType1 = nonNullableType2.UnderlyingEnumType;
            }
            else if (nonNullableType2 is NullType)
            {
                nonNullableType2 = nonNullableType1.UnderlyingEnumType;
            }

            NullableType typeDst = TypeManager.GetNullable(GetEnumBinOpType(ek, nonNullableType1, nonNullableType2, out AggregateType typeEnum));

            Debug.Assert(typeEnum != null);

            PredefinedType ptOp = typeEnum.FundamentalType switch
            {
                FUNDTYPE.FT_U4 => PredefinedType.PT_UINT,
                FUNDTYPE.FT_I8 => PredefinedType.PT_LONG,
                FUNDTYPE.FT_U8 => PredefinedType.PT_ULONG,
                _ => PredefinedType.PT_INT, // Promote all smaller types to int.
            };
            NullableType typeOp = TypeManager.GetNullable(GetPredefindType(ptOp));
            arg1 = mustCast(arg1, typeOp, CONVERTTYPE.NOUDC);
            arg2 = mustCast(arg2, typeOp, CONVERTTYPE.NOUDC);

            ExprBinOp exprRes = ExprFactory.CreateBinop(ek, typeOp, arg1, arg2);
            exprRes.IsLifted = true;
            exprRes.Flags |= flags;
            Debug.Assert((exprRes.Flags & EXPRFLAG.EXF_LVALUE) == 0);

            if (exprRes.Type != typeDst)
            {
                return mustCast(exprRes, typeDst, CONVERTTYPE.NOUDC);
            }

            return exprRes;
        }


        /*
            Handles enum unary operator (~).
        */
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static Expr BindEnumUnaOp(ExpressionBinder binder, ExpressionKind ek, EXPRFLAG flags, Expr arg)
        {
            Debug.Assert(ek == ExpressionKind.BitwiseNot);
            Debug.Assert((ExprCast)arg != null);
            Debug.Assert(((ExprCast)arg).Argument.Type.IsEnumType);
            CType typeEnum = ((ExprCast)arg).Argument.Type;

            PredefinedType ptOp = typeEnum.FundamentalType switch
            {
                FUNDTYPE.FT_U4 => PredefinedType.PT_UINT,
                FUNDTYPE.FT_I8 => PredefinedType.PT_LONG,
                FUNDTYPE.FT_U8 => PredefinedType.PT_ULONG,
                _ => PredefinedType.PT_INT, // Promote all smaller types to int.
            };
            CType typeOp = GetPredefindType(ptOp);
            arg = binder.mustCast(arg, typeOp, CONVERTTYPE.NOUDC);

            Expr exprRes = binder.BindIntOp(ek, flags, arg, null, ptOp);
            return binder.MustCastInUncheckedContext(exprRes, typeEnum, CONVERTTYPE.NOUDC);
        }

        /*
            Given a binary operator EXPRKIND, get the BinOpKind and flags.
        */
        private (BinOpKind, EXPRFLAG) GetBinopKindAndFlags(ExpressionKind ek)
        {
            BinOpKind pBinopKind;
            EXPRFLAG flags = 0;
            switch (ek)
            {
                case ExpressionKind.Add:
                    if (Context.Checked)
                    {
                        flags = EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    pBinopKind = BinOpKind.Add;
                    break;
                case ExpressionKind.Subtract:
                    if (Context.Checked)
                    {
                        flags = EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    pBinopKind = BinOpKind.Sub;
                    break;
                case ExpressionKind.Divide:
                case ExpressionKind.Modulo:
                    // EXPRKIND.EK_DIV and EXPRKIND.EK_MOD need to be treated special for hasSideEffects,
                    // hence the EXPRFLAG.EXF_ASSGOP. Yes, this is a hack.
                    flags = EXPRFLAG.EXF_ASSGOP;
                    if (Context.Checked)
                    {
                        flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    pBinopKind = BinOpKind.Mul;
                    break;
                case ExpressionKind.Multiply:
                    if (Context.Checked)
                    {
                        flags = EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    pBinopKind = BinOpKind.Mul;
                    break;
                case ExpressionKind.BitwiseAnd:
                case ExpressionKind.BitwiseOr:
                    pBinopKind = BinOpKind.Bitwise;
                    break;
                case ExpressionKind.BitwiseExclusiveOr:
                    pBinopKind = BinOpKind.BitXor;
                    break;
                case ExpressionKind.LeftShirt:
                case ExpressionKind.RightShift:
                    pBinopKind = BinOpKind.Shift;
                    break;
                case ExpressionKind.LogicalOr:
                case ExpressionKind.LogicalAnd:
                    pBinopKind = BinOpKind.Logical;
                    break;
                case ExpressionKind.LessThan:
                case ExpressionKind.LessThanOrEqual:
                case ExpressionKind.GreaterThan:
                case ExpressionKind.GreaterThanOrEqual:
                    pBinopKind = BinOpKind.Compare;
                    break;
                case ExpressionKind.Eq:
                case ExpressionKind.NotEq:
                    pBinopKind = BinOpKind.Equal;
                    break;
                default:
                    Debug.Fail($"Bad ek: {ek}");
                    throw Error.InternalCompilerError();
            }

            return (pBinopKind, flags);
        }

        /*
            Convert an expression involving I4, U4, I8 or U8 operands. The operands are
            assumed to be already converted to the correct types.
        */
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "All types used here are builtin and will not be trimmed.")]
        private ExprOperator BindIntOp(ExpressionKind kind, EXPRFLAG flags, Expr op1, Expr op2, PredefinedType ptOp)
        {
            //Debug.Assert(kind.isRelational() || kind.isArithmetic() || kind.isBitwise());
            Debug.Assert(ptOp == PredefinedType.PT_INT || ptOp == PredefinedType.PT_UINT || ptOp == PredefinedType.PT_LONG || ptOp == PredefinedType.PT_ULONG);
            CType typeOp = GetPredefindType(ptOp);
            Debug.Assert(typeOp != null);
            Debug.Assert(op1 != null && op1.Type == typeOp);
            Debug.Assert(op2 == null || op2.Type == typeOp);
            Debug.Assert((op2 == null) == (kind == ExpressionKind.Negate || kind == ExpressionKind.UnaryPlus || kind == ExpressionKind.BitwiseNot));

            if (kind == ExpressionKind.Negate)
            {
                return BindIntegerNeg(flags, op1, ptOp);
            }

            CType typeDest = kind.IsRelational() ? GetPredefindType(PredefinedType.PT_BOOL) : typeOp;

            ExprOperator exprRes = ExprFactory.CreateOperator(kind, typeDest, op1, op2);
            exprRes.Flags |= flags;
            Debug.Assert((exprRes.Flags & EXPRFLAG.EXF_LVALUE) == 0);
            return exprRes;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "All types used here are builtin and will not be trimmed.")]
        private ExprOperator BindIntegerNeg(EXPRFLAG flags, Expr op, PredefinedType ptOp)
        {
            // 14.6.2 Unary minus operator
            // For an operation of the form -x, unary operator overload resolution (14.2.3) is applied to select
            // a specific operator implementation. The operand is converted to the parameter type of the selected
            // operator, and the type of the result is the return type of the operator. The predefined negation
            // operators are:
            //
            //  Integer negation:
            //
            //   int operator -(int x);
            //   long operator -(long x);
            //
            // The result is computed by subtracting x from zero. In a checked context, if the value of x is the
            // smallest int or long (-2^31 or -2^63, respectively), a System.OverflowException is thrown. In an
            //  unchecked context, if the value of x is the smallest int or long, the result is that same value
            // and the overflow is not reported.
            //
            // If the operand of the negation operator is of type uint, it is converted to type long, and the
            // type of the result is long. An exception is the rule that permits the int value -2147483648 (-2^31)
            // to be written as a decimal integer literal (9.4.4.2).
            //
            //  Negation of ulong is an error:
            //
            //   void operator -(ulong x);
            //
            // Selection of this operator by unary operator overload resolution (14.2.3) always results in a
            // compile-time error. Consequently, if the operand of the negation operator is of type ulong, a
            // compile-time error occurs. An exception is the rule that permits the long value
            // -9223372036854775808 (-2^63) to be written as a decimal integer literal (9.4.4.2).


            Debug.Assert(ptOp == PredefinedType.PT_INT || ptOp == PredefinedType.PT_UINT || ptOp == PredefinedType.PT_LONG || ptOp == PredefinedType.PT_ULONG);
            CType typeOp = GetPredefindType(ptOp);
            Debug.Assert(typeOp != null);
            Debug.Assert(op != null && op.Type == typeOp);

            if (ptOp == PredefinedType.PT_ULONG)
            {
                throw BadOperatorTypesError(op, null);
            }

            if (ptOp == PredefinedType.PT_UINT && op.Type.FundamentalType == FUNDTYPE.FT_U4)
            {
                op = mustConvertCore(op, GetPredefindType(PredefinedType.PT_LONG), CONVERTTYPE.NOUDC);
            }

            ExprOperator exprRes = ExprFactory.CreateNeg(flags, op);
            Debug.Assert(0 == (exprRes.Flags & EXPRFLAG.EXF_LVALUE));
            return exprRes;
        }

        /*
          Bind an float/double operator: +, -, , /, %, <, >, <=, >=, ==, !=. If both operations are constants, the result
          will be a constant also. op2 can be null for a unary operator. The operands are assumed
          to be already converted to the correct type.
         */

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "All types used here are builtin and will not be trimmed.")]
        private static ExprOperator BindFloatOp(ExpressionKind kind, Expr op1, Expr op2)
        {
            //Debug.Assert(kind.isRelational() || kind.isArithmetic());
            Debug.Assert(op2 == null || op1.Type == op2.Type);
            Debug.Assert(op1.Type.IsPredefType(PredefinedType.PT_FLOAT) || op1.Type.IsPredefType(PredefinedType.PT_DOUBLE));

            // Allocate the result expression.
            CType typeDest = kind.IsRelational() ? GetPredefindType(PredefinedType.PT_BOOL) : op1.Type;

            ExprOperator exprRes = ExprFactory.CreateOperator(kind, typeDest, op1, op2);
            exprRes.Flags &= ~EXPRFLAG.EXF_CHECKOVERFLOW;

            return exprRes;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "All types used here are builtin and will not be trimmed.")]
        private static ExprConcat BindStringConcat(Expr op1, Expr op2)
        {
            // If the concatenation consists solely of two constants then we must
            // realize the concatenation into a single constant node at this time.
            // Why?  Because we have to know whether
            //
            //  string x = "c" + "d";
            //
            // is legal or not.  We also need to be able to determine during flow
            // checking that
            //
            // switch("a" + "b"){ case "a": ++foo; break; }
            //
            // contains unreachable code.
            //
            // However we can defer further merging of concatenation trees until
            // the optimization pass after flow checking.

            Debug.Assert(op1 != null);
            Debug.Assert(op2 != null);
            return ExprFactory.CreateConcat(op1, op2);
        }

        /*
          Report an ambiguous operator types error.
         */
        private static RuntimeBinderException AmbiguousOperatorError(Expr op1, Expr op2)
        {
            Debug.Assert(op1 != null);

            // This is exactly the same "hack" that BadOperatorError uses. The first operand contains the
            // name of the operator in its errorString.
            string strOp = op1.ErrorString;

            // Bad arg types - report error to user.
            return op2 != null
                ? ErrorHandling.Error(ErrorCode.ERR_AmbigBinaryOps, strOp, op1.Type, op2.Type)
                : ErrorHandling.Error(ErrorCode.ERR_AmbigUnaryOp, strOp, op1.Type);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private Expr BindUserBoolOp(ExpressionKind kind, ExprCall pCall)
        {
            Debug.Assert(pCall != null);
            Debug.Assert(pCall.MethWithInst.Meth() != null);
            Debug.Assert(pCall.OptionalArguments != null);
            Debug.Assert(kind == ExpressionKind.LogicalAnd || kind == ExpressionKind.LogicalOr);

            CType typeRet = pCall.Type;

            Debug.Assert(pCall.MethWithInst.Meth().Params.Count == 2);
            if (!TypeManager.SubstEqualTypes(typeRet, pCall.MethWithInst.Meth().Params[0], typeRet) ||
                !TypeManager.SubstEqualTypes(typeRet, pCall.MethWithInst.Meth().Params[1], typeRet))
            {
                throw ErrorHandling.Error(ErrorCode.ERR_BadBoolOp, pCall.MethWithInst);
            }

            ExprList list = (ExprList)pCall.OptionalArguments;
            Debug.Assert(list != null);

            Expr pExpr = list.OptionalElement;
            ExprWrap pExprWrap = WrapShortLivedExpression(pExpr);
            list.OptionalElement = pExprWrap;

            // Reflection load the true and false methods.
            SymbolTable.PopulateSymbolTableWithName(SpecialNames.CLR_True, null, pExprWrap.Type.AssociatedSystemType);
            SymbolTable.PopulateSymbolTableWithName(SpecialNames.CLR_False, null, pExprWrap.Type.AssociatedSystemType);

            Expr pCallT = bindUDUnop(ExpressionKind.True, pExprWrap);
            Expr pCallF = bindUDUnop(ExpressionKind.False, pExprWrap);

            if (pCallT == null || pCallF == null)
            {
                throw ErrorHandling.Error(ErrorCode.ERR_MustHaveOpTF, typeRet);
            }

            pCallT = mustConvert(pCallT, GetPredefindType(PredefinedType.PT_BOOL));
            pCallF = mustConvert(pCallF, GetPredefindType(PredefinedType.PT_BOOL));
            return ExprFactory.CreateUserLogOp(typeRet, kind == ExpressionKind.LogicalAnd ? pCallF : pCallT, pCall);
        }

        private static AggregateType GetUserDefinedBinopArgumentType(CType type)
        {
            if (type is NullableType nt)
            {
                type = nt.UnderlyingType;
            }

            if (type is AggregateType ats && (ats.IsClassType || ats.IsStructType) && !ats.OwningAggregate.IsSkipUDOps())
            {
                return ats;
            }

            return null;
        }

        private static int GetUserDefinedBinopArgumentTypes(CType type1, CType type2, AggregateType[] rgats)
        {
            int cats = 0;
            rgats[0] = GetUserDefinedBinopArgumentType(type1);
            if (rgats[0] != null)
            {
                ++cats;
            }
            rgats[cats] = GetUserDefinedBinopArgumentType(type2);
            if (rgats[cats] != null)
            {
                ++cats;
            }
            if (cats == 2 && rgats[0] == rgats[1])
            {
                // Common case: they're the same.
                cats = 1;
            }
            return cats;
        }

        private static bool UserDefinedBinaryOperatorCanBeLifted(ExpressionKind ek, MethodSymbol method, AggregateType ats,
            TypeArray Params)
        {
            if (!Params[0].IsNonNullableValueType || !Params[1].IsNonNullableValueType)
            {
                return false;
            }

            CType typeRet = TypeManager.SubstType(method.RetType, ats);
            if (!typeRet.IsNonNullableValueType)
            {
                return false;
            }

            switch (ek)
            {
                case ExpressionKind.Eq:
                case ExpressionKind.NotEq:
                    if (!typeRet.IsPredefType(PredefinedType.PT_BOOL))
                    {
                        return false;
                    }
                    if (Params[0] != Params[1])
                    {
                        return false;
                    }
                    return true;
                case ExpressionKind.GreaterThan:
                case ExpressionKind.GreaterThanOrEqual:
                case ExpressionKind.LessThan:
                case ExpressionKind.LessThanOrEqual:
                    if (!typeRet.IsPredefType(PredefinedType.PT_BOOL))
                    {
                        return false;
                    }
                    return true;
                default:
                    return true;
            }
        }

        // If the operator is applicable in either its regular or lifted forms,
        // add it to the candidate set and return true, otherwise return false.
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private bool UserDefinedBinaryOperatorIsApplicable(List<CandidateFunctionMember> candidateList,
            ExpressionKind ek, MethodSymbol method, AggregateType ats, Expr arg1, Expr arg2, bool fDontLift)
        {
            if (!method.isOperator || method.Params.Count != 2)
            {
                return false;
            }
            Debug.Assert(method.typeVars.Count == 0);
            TypeArray paramsCur = TypeManager.SubstTypeArray(method.Params, ats);
            if (canConvert(arg1, paramsCur[0]) && canConvert(arg2, paramsCur[1]))
            {
                candidateList.Add(new CandidateFunctionMember(
                    new MethPropWithInst(method, ats, TypeArray.Empty),
                    paramsCur,
                    0, // No lifted arguments
                    false));
                return true;
            }
            if (fDontLift || !UserDefinedBinaryOperatorCanBeLifted(ek, method, ats, paramsCur))
            {
                return false;
            }
            CType[] rgtype = new CType[2];
            rgtype[0] = TypeManager.GetNullable(paramsCur[0]);
            rgtype[1] = TypeManager.GetNullable(paramsCur[1]);
            if (!canConvert(arg1, rgtype[0]) || !canConvert(arg2, rgtype[1]))
            {
                return false;
            }
            candidateList.Add(new CandidateFunctionMember(
                new MethPropWithInst(method, ats, TypeArray.Empty),
                TypeArray.Allocate(rgtype),
                2, // two lifted arguments
                false));
            return true;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private bool GetApplicableUserDefinedBinaryOperatorCandidates(
            List<CandidateFunctionMember> candidateList, ExpressionKind ek, AggregateType type,
            Expr arg1, Expr arg2, bool fDontLift)
        {
            Name name = ExpressionKindName(ek);
            Debug.Assert(name != null);
            bool foundSome = false;
            for (MethodSymbol methCur = SymbolLoader.LookupAggMember(name, type.OwningAggregate, symbmask_t.MASK_MethodSymbol) as MethodSymbol;
                methCur != null;
                methCur = methCur.LookupNext(symbmask_t.MASK_MethodSymbol) as MethodSymbol)
            {
                if (UserDefinedBinaryOperatorIsApplicable(candidateList, ek, methCur, type, arg1, arg2, fDontLift))
                {
                    foundSome = true;
                }
            }
            return foundSome;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private AggregateType GetApplicableUserDefinedBinaryOperatorCandidatesInBaseTypes(
            List<CandidateFunctionMember> candidateList, ExpressionKind ek, AggregateType type,
            Expr arg1, Expr arg2, bool fDontLift, AggregateType atsStop)
        {
            for (AggregateType atsCur = type; atsCur != null && atsCur != atsStop; atsCur = atsCur.BaseClass)
            {
                if (GetApplicableUserDefinedBinaryOperatorCandidates(candidateList, ek, atsCur, arg1, arg2, fDontLift))
                {
                    return atsCur;
                }
            }

            return null;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private ExprCall BindUDBinop(ExpressionKind ek, Expr arg1, Expr arg2, bool fDontLift, out MethPropWithInst ppmpwi)
        {
            List<CandidateFunctionMember> methFirst = new List<CandidateFunctionMember>();

            ppmpwi = null;

            AggregateType[] rgats = { null, null };
            int cats = GetUserDefinedBinopArgumentTypes(arg1.Type, arg2.Type, rgats);
            if (cats == 0)
            {
                return null;
            }
            else if (cats == 1)
            {
                GetApplicableUserDefinedBinaryOperatorCandidatesInBaseTypes(methFirst, ek,
                    rgats[0], arg1, arg2, fDontLift, null);
            }
            else
            {
                Debug.Assert(cats == 2);
                AggregateType atsStop = GetApplicableUserDefinedBinaryOperatorCandidatesInBaseTypes(methFirst, ek,
                    rgats[0], arg1, arg2, fDontLift, null);
                GetApplicableUserDefinedBinaryOperatorCandidatesInBaseTypes(methFirst, ek,
                    rgats[1], arg1, arg2, fDontLift, atsStop);
            }
            if (methFirst.IsEmpty())
            {
                return null;
            }

            ExprList args = ExprFactory.CreateList(arg1, arg2);
            ArgInfos info = new ArgInfos();
            info.carg = 2;
            FillInArgInfoFromArgList(info, args);
            CandidateFunctionMember pmethAmbig1;
            CandidateFunctionMember pmethAmbig2;
            CandidateFunctionMember pmethBest = FindBestMethod(methFirst, null, info, out pmethAmbig1, out pmethAmbig2);

            if (pmethBest == null)
            {
                // No winner, so its an ambiguous call...
                throw ErrorHandling.Error(ErrorCode.ERR_AmbigCall, pmethAmbig1.mpwi, pmethAmbig2.mpwi);
            }

            ppmpwi = pmethBest.mpwi;

            if (pmethBest.ctypeLift != 0)
            {
                Debug.Assert(pmethBest.ctypeLift == 2);

                return BindLiftedUDBinop(ek, arg1, arg2, pmethBest.@params, pmethBest.mpwi);
            }

            CType typeRetRaw = TypeManager.SubstType(pmethBest.mpwi.Meth().RetType, pmethBest.mpwi.GetType());

            return BindUDBinopCall(arg1, arg2, pmethBest.@params, typeRetRaw, pmethBest.mpwi);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private ExprCall BindUDBinopCall(Expr arg1, Expr arg2, TypeArray Params, CType typeRet, MethPropWithInst mpwi)
        {
            arg1 = mustConvert(arg1, Params[0]);
            arg2 = mustConvert(arg2, Params[1]);
            ExprList args = ExprFactory.CreateList(arg1, arg2);

            CheckUnsafe(arg1.Type); // added to the binder so we don't bind to pointer ops
            CheckUnsafe(arg2.Type); // added to the binder so we don't bind to pointer ops
            CheckUnsafe(typeRet); // added to the binder so we don't bind to pointer ops


            ExprMemberGroup pMemGroup = ExprFactory.CreateMemGroup(null, mpwi);
            ExprCall call = ExprFactory.CreateCall(0, typeRet, args, pMemGroup, null);
            call.MethWithInst = new MethWithInst(mpwi);
            verifyMethodArgs(call, mpwi.GetType());
            return call;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private ExprCall BindLiftedUDBinop(ExpressionKind ek, Expr arg1, Expr arg2, TypeArray Params, MethPropWithInst mpwi)
        {
            Expr exprVal1 = arg1;
            Expr exprVal2 = arg2;
            CType typeRet;
            CType typeRetRaw = TypeManager.SubstType(mpwi.Meth().RetType, mpwi.GetType());

            // This is a lifted user defined operator.  We know that both arguments
            // go to the nullable formal parameter types, and that at least one
            // of the arguments does not go to the non-nullable formal parameter type.
            // (If both went to the non-nullable types then we would not be lifting.)
            // We also know that the non-nullable type of the argument goes to the
            // non-nullable type of formal parameter.  However, if it does so only via
            // a user-defined conversion then we should bind the conversion from the
            // argument to the nullable formal parameter type first, before we then
            // do the cast for the non-nullable call.

            TypeArray paramsRaw = TypeManager.SubstTypeArray(mpwi.Meth().Params, mpwi.GetType());
            Debug.Assert(Params != paramsRaw);
            Debug.Assert(paramsRaw[0] == Params[0].BaseOrParameterOrElementType);
            Debug.Assert(paramsRaw[1] == Params[1].BaseOrParameterOrElementType);

            if (!canConvert(arg1.Type.StripNubs(), paramsRaw[0], CONVERTTYPE.NOUDC))
            {
                exprVal1 = mustConvert(arg1, Params[0]);
            }
            if (!canConvert(arg2.Type.StripNubs(), paramsRaw[1], CONVERTTYPE.NOUDC))
            {
                exprVal2 = mustConvert(arg2, Params[1]);
            }
            Expr nonLiftedArg1 = mustCast(exprVal1, paramsRaw[0]);
            Expr nonLiftedArg2 = mustCast(exprVal2, paramsRaw[1]);
            switch (ek)
            {
                default:
                    typeRet = TypeManager.GetNullable(typeRetRaw);
                    break;
                case ExpressionKind.Eq:
                case ExpressionKind.NotEq:
                    Debug.Assert(paramsRaw[0] == paramsRaw[1]);
                    Debug.Assert(typeRetRaw.IsPredefType(PredefinedType.PT_BOOL));
                    // These ones don't lift the return type. Instead, if either side is null, the result is false.
                    typeRet = typeRetRaw;
                    break;
                case ExpressionKind.GreaterThan:
                case ExpressionKind.GreaterThanOrEqual:
                case ExpressionKind.LessThan:
                case ExpressionKind.LessThanOrEqual:
                    Debug.Assert(typeRetRaw.IsPredefType(PredefinedType.PT_BOOL));
                    // These ones don't lift the return type. Instead, if either side is null, the result is false.
                    typeRet = typeRetRaw;
                    break;
            }

            // Now get the result for the pre-lifted call.

            Debug.Assert(!(ek == ExpressionKind.Eq || ek == ExpressionKind.NotEq) || nonLiftedArg1.Type == nonLiftedArg2.Type);

            ExprCall nonLiftedResult = BindUDBinopCall(nonLiftedArg1, nonLiftedArg2, paramsRaw, typeRetRaw, mpwi);

            ExprList args = ExprFactory.CreateList(exprVal1, exprVal2);
            ExprMemberGroup pMemGroup = ExprFactory.CreateMemGroup(null, mpwi);
            ExprCall call = ExprFactory.CreateCall(0, typeRet, args, pMemGroup, null);
            call.MethWithInst = new MethWithInst(mpwi);

            call.NullableCallLiftKind = ek switch
            {
                ExpressionKind.Eq => NullableCallLiftKind.EqualityOperator,

                ExpressionKind.NotEq => NullableCallLiftKind.InequalityOperator,

                _ => NullableCallLiftKind.Operator,
            };
            call.CastOfNonLiftedResultToLiftedType = mustCast(nonLiftedResult, typeRet, 0);
            return call;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static AggregateType GetEnumBinOpType(ExpressionKind ek, CType argType1, CType argType2, out AggregateType ppEnumType)
        {
            Debug.Assert(argType1.IsEnumType || argType2.IsEnumType);

            AggregateType type1 = argType1 as AggregateType;
            AggregateType type2 = argType2 as AggregateType;

            AggregateType typeEnum = type1.IsEnumType ? type1 : type2;

            Debug.Assert(type1 == typeEnum || type1 == typeEnum.UnderlyingEnumType);
            Debug.Assert(type2 == typeEnum || type2 == typeEnum.UnderlyingEnumType);

            AggregateType typeDst = typeEnum;

            switch (ek)
            {
                case ExpressionKind.BitwiseAnd:
                case ExpressionKind.BitwiseOr:
                case ExpressionKind.BitwiseExclusiveOr:
                    Debug.Assert(type1 == type2);
                    break;

                case ExpressionKind.Add:
                    Debug.Assert(type1 != type2);
                    break;

                case ExpressionKind.Subtract:
                    if (type1 == type2)
                        typeDst = typeEnum.UnderlyingEnumType;
                    break;

                default:
                    Debug.Assert(ek.IsRelational());
                    typeDst = GetPredefindType(PredefinedType.PT_BOOL);
                    break;
            }

            ppEnumType = typeEnum;
            return typeDst;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static ExprBinOp CreateBinopForPredefMethodCall(ExpressionKind ek, PREDEFMETH predefMeth, CType RetType, Expr arg1, Expr arg2)
        {
            MethodSymbol methSym = PredefinedMembers.GetMethod(predefMeth);
            Debug.Assert(methSym != null);
            ExprBinOp binop = ExprFactory.CreateBinop(ek, RetType, arg1, arg2);

            // Set the predefined method to call.
            AggregateSymbol agg = methSym.getClass();
            AggregateType callingType = TypeManager.GetAggregate(agg, TypeArray.Empty);
            binop.PredefinedMethodToCall = new MethWithInst(methSym, callingType, null);
            binop.UserDefinedCallMethod = binop.PredefinedMethodToCall;
            return binop;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static ExprUnaryOp CreateUnaryOpForPredefMethodCall(ExpressionKind ek, PREDEFMETH predefMeth, CType pRetType, Expr pArg)
        {
            MethodSymbol methSym = PredefinedMembers.GetMethod(predefMeth);
            Debug.Assert(methSym != null);
            ExprUnaryOp pUnaryOp = ExprFactory.CreateUnaryOp(ek, pRetType, pArg);

            // Set the predefined method to call.
            AggregateSymbol pAgg = methSym.getClass();
            AggregateType pCallingType = TypeManager.GetAggregate(pAgg, TypeArray.Empty);
            pUnaryOp.PredefinedMethodToCall = new MethWithInst(methSym, pCallingType, null);
            pUnaryOp.UserDefinedCallMethod = pUnaryOp.PredefinedMethodToCall;
            return pUnaryOp;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed partial class ExpressionBinder
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
         
                (ptr,       ptr)        :         -
                (ptr,       int)        :       + -
                (ptr,       uint)       :       + -
                (ptr,       long)       :       + -
                (ptr,       ulong)      :       + -
                (int,       ptr)        :       +
                (uint,      ptr)        :       +
                (long,      ptr)        :       +
                (ulong,     ptr)        :       +
         
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
         
            Note that pointer operators cannot be lifted over nullable.
        */

        // BinOpBindMethod and UnaOpBindMethod are method pointer arrays to dispatch the appropriate operator binder.
        // Method pointers must be in the order of the corresponding enums. We check this when the full signature is set. 
        // When the binding method is looked up in these arrays we ASSERT
        // if the array is out of bounds of the corresponding array.

        private readonly BinOpSig[] g_binopSignatures;

        // We want unary minus to bind to "operator -(ulong)" and then we
        // produce an error (since there is no pfn). We can't let - bind to a floating point type,
        // since they lose precision. See the language spec.

        // Increment and decrement operators are special.

        private readonly UnaOpSig[] g_rguos;

        private Expr bindUserDefinedBinOp(ExpressionKind ek, BinOpArgInfo info)
        {
            MethPropWithInst pmpwi = null;
            if (info.pt1 <= PredefinedType.PT_ULONG && info.pt2 <= PredefinedType.PT_ULONG)
            {
                return null;
            }

            Expr expr = null;

            switch (info.binopKind)
            {
                case BinOpKind.Logical:
                    {
                        // Logical operators cannot be overloaded, but use the bitwise overloads.
                        ExprCall call = BindUDBinop((ExpressionKind)(ek - ExpressionKind.EK_LOGAND + ExpressionKind.EK_BITAND), info.arg1, info.arg2, true, out pmpwi);
                        if (call != null)
                        {
                            if (call.IsOK)
                            {
                                expr = BindUserBoolOp(ek, call);
                            }
                            else
                            {
                                expr = call;
                            }
                        }
                        break;
                    }
                default:
                    expr = BindUDBinop(ek, info.arg1, info.arg2, false, out pmpwi);
                    break;
            }

            if (expr == null)
            {
                return null;
            }

            return GetExprFactory().CreateUserDefinedBinop(ek, expr.Type, info.arg1, info.arg2, expr, pmpwi);
        }

        // Adds special signatures to the candidate list.  If we find an exact match
        // then it will be the last item on the list and we return true.
        private bool GetSpecialBinopSignatures(List<BinOpFullSig> prgbofs, BinOpArgInfo info)
        {
            Debug.Assert(prgbofs != null);
            if (info.pt1 <= PredefinedType.PT_ULONG && info.pt2 <= PredefinedType.PT_ULONG)
            {
                return false;
            }
            return GetDelBinOpSigs(prgbofs, info) ||
                   GetEnumBinOpSigs(prgbofs, info) ||
                   GetPtrBinOpSigs(prgbofs, info) ||
                   GetRefEqualSigs(prgbofs, info);
        }

        // Adds standard and lifted signatures to the candidate list.  If we find an exact match
        // then it will be the last item on the list and we return true.

        private bool GetStandardAndLiftedBinopSignatures(List<BinOpFullSig> rgbofs, BinOpArgInfo info)
        {
            Debug.Assert(rgbofs != null);

            int ibos;
            int ibosMinLift;

            ibosMinLift = GetSymbolLoader().FCanLift() ? 0 : g_binopSignatures.Length;
            for (ibos = 0; ibos < g_binopSignatures.Length; ibos++)
            {
                BinOpSig bos = g_binopSignatures[ibos];
                if ((bos.mask & info.mask) == 0)
                {
                    continue;
                }

                CType typeSig1 = GetOptPDT(bos.pt1, PredefinedTypes.isRequired(bos.pt1));
                CType typeSig2 = GetOptPDT(bos.pt2, PredefinedTypes.isRequired(bos.pt2));
                if (typeSig1 == null || typeSig2 == null)
                    continue;

                ConvKind cv1 = GetConvKind(info.pt1, bos.pt1);
                ConvKind cv2 = GetConvKind(info.pt2, bos.pt2);
                LiftFlags grflt = LiftFlags.None;

                switch (cv1)
                {
                    default:
                        VSFAIL("Shouldn't happen!");
                        continue;

                    case ConvKind.None:
                        continue;
                    case ConvKind.Explicit:
                        if (!info.arg1.isCONSTANT_OK())
                        {
                            continue;
                        }
                        // Need to try to convert.
                        if (canConvert(info.arg1, typeSig1))
                        {
                            break;
                        }
                        if (ibos < ibosMinLift || !bos.CanLift())
                        {
                            continue;
                        }
                        Debug.Assert(typeSig1.IsValType());

                        typeSig1 = GetSymbolLoader().GetTypeManager().GetNullable(typeSig1);
                        if (!canConvert(info.arg1, typeSig1))
                        {
                            continue;
                        }
                        switch (GetConvKind(info.ptRaw1, bos.pt1))
                        {
                            default:
                                grflt = grflt | LiftFlags.Convert1;
                                break;
                            case ConvKind.Implicit:
                            case ConvKind.Identity:
                                grflt = grflt | LiftFlags.Lift1;
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
                        Debug.Assert(typeSig1.IsValType());

                        typeSig1 = GetSymbolLoader().GetTypeManager().GetNullable(typeSig1);
                        if (!canConvert(info.arg1, typeSig1))
                        {
                            continue;
                        }
                        switch (GetConvKind(info.ptRaw1, bos.pt1))
                        {
                            default:
                                grflt = grflt | LiftFlags.Convert1;
                                break;
                            case ConvKind.Implicit:
                            case ConvKind.Identity:
                                grflt = grflt | LiftFlags.Lift1;
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
                        VSFAIL("Shouldn't happen!");
                        continue;
                    case ConvKind.None:
                        continue;
                    case ConvKind.Explicit:
                        if (!info.arg2.isCONSTANT_OK())
                        {
                            continue;
                        }
                        // Need to try to convert.
                        if (canConvert(info.arg2, typeSig2))
                        {
                            break;
                        }
                        if (ibos < ibosMinLift || !bos.CanLift())
                        {
                            continue;
                        }
                        Debug.Assert(typeSig2.IsValType());

                        typeSig2 = GetSymbolLoader().GetTypeManager().GetNullable(typeSig2);
                        if (!canConvert(info.arg2, typeSig2))
                        {
                            continue;
                        }
                        switch (GetConvKind(info.ptRaw2, bos.pt2))
                        {
                            default:
                                grflt = grflt | LiftFlags.Convert2;
                                break;
                            case ConvKind.Implicit:
                            case ConvKind.Identity:
                                grflt = grflt | LiftFlags.Lift2;
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
                        Debug.Assert(typeSig2.IsValType());

                        typeSig2 = GetSymbolLoader().GetTypeManager().GetNullable(typeSig2);
                        if (!canConvert(info.arg2, typeSig2))
                        {
                            continue;
                        }
                        switch (GetConvKind(info.ptRaw2, bos.pt2))
                        {
                            default:
                                grflt = grflt | LiftFlags.Convert2;
                                break;
                            case ConvKind.Implicit:
                            case ConvKind.Identity:
                                grflt = grflt | LiftFlags.Lift2;
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

        private ExprBinOp bindNullEqualityComparison(ExpressionKind ek, BinOpArgInfo info)
        {
            Expr arg1 = info.arg1;
            Expr arg2 = info.arg2;
            if (info.binopKind == BinOpKind.Equal)
            {
                CType typeBool = GetReqPDT(PredefinedType.PT_BOOL);
                ExprBinOp exprRes = null;
                if (info.type1.IsNullableType() && info.type2.IsNullType())
                {
                    arg2 = GetExprFactory().CreateZeroInit(info.type1);
                    exprRes = GetExprFactory().CreateBinop(ek, typeBool, arg1, arg2);
                }
                if (info.type1.IsNullType() && info.type2.IsNullableType())
                {
                    arg1 = GetExprFactory().CreateZeroInit(info.type2);
                    exprRes = GetExprFactory().CreateBinop(ek, typeBool, arg1, arg2);
                }
                if (exprRes != null)
                {
                    exprRes.IsLifted = true;
                    return exprRes;
                }
            }
            Expr pExpr = BadOperatorTypesError(ek, info.arg1, info.arg2, GetTypes().GetErrorSym());
            pExpr.AssertIsBin();
            return (ExprBinOp)pExpr;
        }

        /*
            This handles binding binary operators by first checking for user defined operators, then
            applying overload resolution to the predefined operators. It handles lifting over nullable.
        */
        public Expr BindStandardBinop(ExpressionKind ek, Expr arg1, Expr arg2)
        {
            Debug.Assert(arg1 != null);
            Debug.Assert(arg2 != null);

            EXPRFLAG flags = 0;

            BinOpArgInfo info = new BinOpArgInfo(arg1, arg2);
            if (!GetBinopKindAndFlags(ek, out info.binopKind, out flags))
            {
                // If we don't get the BinopKind and the flags, then we must have had some bad operator types.

                return BadOperatorTypesError(ek, arg1, arg2);
            }

            info.mask = (BinOpMask)(1 << (int)info.binopKind);

            List<BinOpFullSig> binopSignatures = new List<BinOpFullSig>();
            int bestBinopSignature = -1;

            // First check if this is a user defined binop. If it is, return it.
            Expr exprUD = bindUserDefinedBinOp(ek, info);
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

            if (exactMatch)
            {
                Debug.Assert(binopSignatures.Count > 0);
                bestBinopSignature = binopSignatures.Count - 1;
            }
            else if (binopSignatures.Count == 0)
            {
                // If we got no matches then it's possible that we're in the case
                // x == null, where x is nullable.
                return bindNullEqualityComparison(ek, info);
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

                    return ambiguousOperatorError(ek, arg1, arg2);
                }
            }

            // If we're here, we should have a binop signature that exactly matches.

            Debug.Assert(bestBinopSignature < binopSignatures.Count);

            // We've found the one to use, so lets go and bind it.

            return BindStandardBinopCore(info, binopSignatures[bestBinopSignature], ek, flags);
        }

        private Expr BindStandardBinopCore(BinOpArgInfo info, BinOpFullSig bofs, ExpressionKind ek, EXPRFLAG flags)
        {
            if (bofs.pfn == null)
            {
                return BadOperatorTypesError(ek, info.arg1, info.arg2);
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
                    return BindBoolBitwiseOp(ek, flags, expr1, expr2, bofs);
                }
                return bofs.pfn(ek, flags, expr1, expr2);
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
        private Expr BindLiftedStandardBinOp(BinOpArgInfo info, BinOpFullSig bofs, ExpressionKind ek, EXPRFLAG flags)
        {
            Debug.Assert(bofs.Type1().IsNullableType() || bofs.Type2().IsNullableType());

            Expr arg1 = info.arg1;
            Expr arg2 = info.arg2;

            // We want to get the base types of the arguments and attempt to bind the non-lifted form of the
            // method so that we error report (ie divide by zero etc), and then we store in the resulting
            // binop that we have a lifted operator.

            Expr pArgument1 = null;
            Expr pArgument2 = null;
            Expr nonLiftedArg1 = null;
            Expr nonLiftedArg2 = null;
            Expr nonLiftedResult = null;
            CType resultType = null;

            LiftArgument(arg1, bofs.Type1(), bofs.ConvertFirst(), out pArgument1, out nonLiftedArg1);
            LiftArgument(arg2, bofs.Type2(), bofs.ConvertSecond(), out pArgument2, out nonLiftedArg2);

            // Now call the non-lifted method to generate errors, and stash the result.
            if (!nonLiftedArg1.isNull() && !nonLiftedArg2.isNull())
            {
                // Only compute the method if theres no nulls. If there are, we'll special case it
                // later, since operations with a null operand are null.
                nonLiftedResult = bofs.pfn(ek, flags, nonLiftedArg1, nonLiftedArg2);
            }

            // Check if we have a comparison. If so, set the result type to bool.
            if (info.binopKind == BinOpKind.Compare || info.binopKind == BinOpKind.Equal)
            {
                resultType = GetReqPDT(PredefinedType.PT_BOOL);
            }
            else
            {
                if (bofs.fnkind == BinOpFuncKind.EnumBinOp)
                {
                    AggregateType enumType;
                    resultType = GetEnumBinOpType(ek, nonLiftedArg1.Type, nonLiftedArg2.Type, out enumType);
                }
                else
                {
                    resultType = pArgument1.Type;
                }
                resultType = resultType.IsNullableType() ? resultType : GetSymbolLoader().GetTypeManager().GetNullable(resultType);
            }

            ExprBinOp exprRes = GetExprFactory().CreateBinop(ek, resultType, pArgument1, pArgument2);
            mustCast(nonLiftedResult, resultType, 0);
            exprRes.IsLifted = true;
            exprRes.Flags |= flags;
            Debug.Assert((exprRes.Flags & EXPRFLAG.EXF_LVALUE) == 0);

            return exprRes;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private void LiftArgument(Expr pArgument, CType pParameterType, bool bConvertBeforeLift,
                                            out Expr ppLiftedArgument, out Expr ppNonLiftedArgument)
        {
            Expr pLiftedArgument = mustConvert(pArgument, pParameterType);
            if (pLiftedArgument != pArgument)
            {
                MarkAsIntermediateConversion(pLiftedArgument);
            }

            Expr pNonLiftedArgument = pArgument;
            if (pParameterType.IsNullableType())
            {
                if (pNonLiftedArgument.isNull())
                {
                    pNonLiftedArgument = mustCast(pNonLiftedArgument, pParameterType);
                }
                pNonLiftedArgument = mustCast(pNonLiftedArgument, pParameterType.AsNullableType().GetUnderlyingType());
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
        private bool GetDelBinOpSigs(List<BinOpFullSig> prgbofs, BinOpArgInfo info)
        {
            if (!info.ValidForDelegate())
            {
                return false;
            }
            if (!info.type1.isDelegateType() && !info.type2.isDelegateType())
            {
                return false;
            }

            // Don't allow comparison with an anonymous method or lambda. It's just too weird.
            if (((info.mask & BinOpMask.Equal) != 0) && (info.type1.IsBoundLambdaType() || info.type2.IsBoundLambdaType()))
                return false;

            // No conversions needed. Determine the lifting. This is the common case.
            if (info.type1 == info.type2)
            {
                prgbofs.Add(new BinOpFullSig(info.type1, info.type2, BindDelBinOp, OpSigFlags.Reference, LiftFlags.None, BinOpFuncKind.DelBinOp));
                return true;
            }

            // Now, for each delegate type, if both arguments convert to that delegate type, that is a candidate
            // for this binary operator. It's possible that we add two candidates, in which case they will compete
            // in overload resolution. Or we could add no candidates.

            bool t1tot2 = info.type2.isDelegateType() && canConvert(info.arg1, info.type2);
            bool t2tot1 = info.type1.isDelegateType() && canConvert(info.arg2, info.type1);

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
        private bool CanConvertArg1(BinOpArgInfo info, CType typeDst, out LiftFlags pgrflt,
                                      out CType ptypeSig1, out CType ptypeSig2)
        {
            ptypeSig1 = null;
            ptypeSig2 = null;
            Debug.Assert(!typeDst.IsNullableType());

            if (canConvert(info.arg1, typeDst))
                pgrflt = LiftFlags.None;
            else
            {
                pgrflt = LiftFlags.None;
                if (!GetSymbolLoader().FCanLift())
                    return false;
                typeDst = GetSymbolLoader().GetTypeManager().GetNullable(typeDst);
                if (!canConvert(info.arg1, typeDst))
                    return false;
                pgrflt = LiftFlags.Convert1;
            }
            ptypeSig1 = typeDst;

            if (info.type2.IsNullableType())
            {
                pgrflt = pgrflt | LiftFlags.Lift2;
                ptypeSig2 = GetSymbolLoader().GetTypeManager().GetNullable(info.typeRaw2);
            }
            else
                ptypeSig2 = info.typeRaw2;

            return true;
        }


        /*
            Same as CanConvertArg1 but with the indices interchanged!
        */
        private bool CanConvertArg2(BinOpArgInfo info, CType typeDst, out LiftFlags pgrflt,
                                      out CType ptypeSig1, out CType ptypeSig2)
        {
            Debug.Assert(!typeDst.IsNullableType());
            ptypeSig1 = null;
            ptypeSig2 = null;

            if (canConvert(info.arg2, typeDst))
                pgrflt = LiftFlags.None;
            else
            {
                pgrflt = LiftFlags.None;
                if (!GetSymbolLoader().FCanLift())
                    return false;
                typeDst = GetSymbolLoader().GetTypeManager().GetNullable(typeDst);
                if (!canConvert(info.arg2, typeDst))
                    return false;
                pgrflt = LiftFlags.Convert2;
            }
            ptypeSig2 = typeDst;

            if (info.type1.IsNullableType())
            {
                pgrflt = pgrflt | LiftFlags.Lift1;
                ptypeSig1 = GetSymbolLoader().GetTypeManager().GetNullable(info.typeRaw1);
            }
            else
                ptypeSig1 = info.typeRaw1;

            return true;
        }


        /*
            Record the appropriate binary operator full signature from the given BinOpArgInfo. This assumes
            that any NullableType valued args should be lifted.
        */
        private void RecordBinOpSigFromArgs(List<BinOpFullSig> prgbofs, BinOpArgInfo info)
        {
            LiftFlags grflt = LiftFlags.None;
            CType typeSig1;
            CType typeSig2;

            if (info.type1 != info.typeRaw1)
            {
                Debug.Assert(info.type1.IsNullableType());
                grflt = grflt | LiftFlags.Lift1;
                typeSig1 = GetSymbolLoader().GetTypeManager().GetNullable(info.typeRaw1);
            }
            else
                typeSig1 = info.typeRaw1;

            if (info.type2 != info.typeRaw2)
            {
                Debug.Assert(info.type2.IsNullableType());
                grflt = grflt | LiftFlags.Lift2;
                typeSig2 = GetSymbolLoader().GetTypeManager().GetNullable(info.typeRaw2);
            }
            else
                typeSig2 = info.typeRaw2;

            prgbofs.Add(new BinOpFullSig(typeSig1, typeSig2, BindEnumBinOp, OpSigFlags.Value, grflt, BinOpFuncKind.EnumBinOp));
        }

        /*
            Get the special signatures when at least one of the args is an enum.  Return true if
            we find an exact match.
        */
        private bool GetEnumBinOpSigs(List<BinOpFullSig> prgbofs, BinOpArgInfo info)
        {
            if (!info.typeRaw1.isEnumType() && !info.typeRaw2.isEnumType())
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

            bool isValidForEnum;

            if (info.typeRaw1.isEnumType())
            {
                isValidForEnum = (info.typeRaw2 == info.typeRaw1.underlyingEnumType() && info.ValidForEnumAndUnderlyingType());
            }
            else
            {
                isValidForEnum = (info.typeRaw1 == info.typeRaw2.underlyingEnumType() && info.ValidForUnderlyingTypeAndEnum());
            }

            if (isValidForEnum)
            {
                RecordBinOpSigFromArgs(prgbofs, info);
                return true;
            }

            // Now deal with the conversion cases. Since there are no conversions from enum types to other
            // enum types we never need to do both cases.

            if (info.typeRaw1.isEnumType())
            {
                isValidForEnum = info.ValidForEnum() && CanConvertArg2(info, info.typeRaw1, out grflt, out typeSig1, out typeSig2) ||
                    info.ValidForEnumAndUnderlyingType() && CanConvertArg2(info, info.typeRaw1.underlyingEnumType(), out grflt, out typeSig1, out typeSig2);
            }
            else
            {
                isValidForEnum = info.ValidForEnum() && CanConvertArg1(info, info.typeRaw2, out grflt, out typeSig1, out typeSig2) ||
                    info.ValidForEnumAndUnderlyingType() && CanConvertArg1(info, info.typeRaw2.underlyingEnumType(), out grflt, out typeSig1, out typeSig2);
            }

            if (isValidForEnum)
            {
                prgbofs.Add(new BinOpFullSig(typeSig1, typeSig2, BindEnumBinOp, OpSigFlags.Value, grflt, BinOpFuncKind.EnumBinOp));
            }
            return false;
        }

        private bool IsEnumArithmeticBinOp(ExpressionKind ek, BinOpArgInfo info)
        {
            switch (ek)
            {
                case ExpressionKind.EK_ADD:
                    return info.typeRaw1.isEnumType() ^ info.typeRaw2.isEnumType();
                case ExpressionKind.EK_SUB:
                    return info.typeRaw1.isEnumType() | info.typeRaw2.isEnumType();
            }

            return false;
        }


        /*
            Get the special signatures when at least one of the args is a pointer. Since pointers can't be
            type arguments, a nullable pointer is illegal, so no sense trying to lift any of these.
         
            NOTE: We don't filter out bad operators on void pointers since BindPtrBinOp gives better
            error messages than the operator overload resolution does.
        */
        private bool GetPtrBinOpSigs(List<BinOpFullSig> prgbofs, BinOpArgInfo info)
        {
            if (!info.type1.IsPointerType() && !info.type2.IsPointerType())
            {
                return false;
            }

            // (ptr,       ptr)        :         -
            // (ptr,       int)        :       + -
            // (ptr,       uint)       :       + -
            // (ptr,       long)       :       + -
            // (ptr,       ulong)      :       + -
            // (int,       ptr)        :       +
            // (uint,      ptr)        :       +
            // (long,      ptr)        :       +
            // (ulong,     ptr)        :       +
            // (void,     void)      :                   == != < > <= >=

            // Check the common case first.
            if (info.type1.IsPointerType() && info.type2.IsPointerType())
            {
                if (info.ValidForVoidPointer())
                {
                    prgbofs.Add(new BinOpFullSig(info.type1, info.type2, BindPtrCmpOp, OpSigFlags.None, LiftFlags.None, BinOpFuncKind.PtrCmpOp));
                    return true;
                }
                if (info.type1 == info.type2 && info.ValidForPointer())
                {
                    prgbofs.Add(new BinOpFullSig(info.type1, info.type2, BindPtrBinOp, OpSigFlags.None, LiftFlags.None, BinOpFuncKind.PtrBinOp));
                    return true;
                }
                return false;
            }

            CType typeT;

            if (info.type1.IsPointerType())
            {
                if (info.type2.IsNullType())
                {
                    if (!info.ValidForVoidPointer())
                    {
                        return false;
                    }
                    prgbofs.Add(new BinOpFullSig(info.type1, info.type1, BindPtrCmpOp, OpSigFlags.Convert, LiftFlags.None, BinOpFuncKind.PtrCmpOp));
                    return true;
                }
                if (!info.ValidForPointerAndNumber())
                {
                    return false;
                }

                for (uint i = 0; i < s_rgptIntOp.Length; i++)
                {
                    if (canConvert(info.arg2, typeT = GetReqPDT(s_rgptIntOp[i])))
                    {
                        prgbofs.Add(new BinOpFullSig(info.type1, typeT, BindPtrBinOp, OpSigFlags.Convert, LiftFlags.None, BinOpFuncKind.PtrBinOp));
                        return true;
                    }
                }
                return false;
            }

            Debug.Assert(info.type2.IsPointerType());
            if (info.type1.IsNullType())
            {
                if (!info.ValidForVoidPointer())
                {
                    return false;
                }
                prgbofs.Add(new BinOpFullSig(info.type2, info.type2, BindPtrCmpOp, OpSigFlags.Convert, LiftFlags.None, BinOpFuncKind.PtrCmpOp));
                return true;
            }
            if (!info.ValidForNumberAndPointer())
            {
                return false;
            }

            for (uint i = 0; i < s_rgptIntOp.Length; i++)
            {
                if (canConvert(info.arg1, typeT = GetReqPDT(s_rgptIntOp[i])))
                {
                    prgbofs.Add(new BinOpFullSig(typeT, info.type2, BindPtrBinOp, OpSigFlags.Convert, LiftFlags.None, BinOpFuncKind.PtrBinOp));
                    return true;
                }
            }
            return false;
        }


        /*
            See if standard reference equality applies. Make sure not to return true if another == operator
            may be applicable and better (or ambiguous)! This also handles == on System.Delegate, since
            it has special rules as well.
        */
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
            CType typeObj = GetReqPDT(PredefinedType.PT_OBJECT);
            CType typeCls = null;

            if (type1.IsNullType() && type2.IsNullType())
            {
                typeCls = typeObj;
                fRet = true;
                goto LRecord;
            }

            // Check for: operator ==(System.Delegate, System.Delegate).
            CType typeDel = GetReqPDT(PredefinedType.PT_DELEGATE);

            if (canConvert(info.arg1, typeDel) && canConvert(info.arg2, typeDel) &&
                !type1.isDelegateType() && !type2.isDelegateType())
            {
                prgbofs.Add(new BinOpFullSig(typeDel, typeDel, BindDelBinOp, OpSigFlags.Convert, LiftFlags.None, BinOpFuncKind.DelBinOp));
            }

            // The reference type equality operators only handle reference types.
            FUNDTYPE ft1 = type1.fundType();
            FUNDTYPE ft2 = type2.fundType();

            switch (ft1)
            {
                default:
                    return false;
                case FUNDTYPE.FT_REF:
                    break;
                case FUNDTYPE.FT_VAR:
                    if (type1.AsTypeParameterType().IsValueType() || (!type1.AsTypeParameterType().IsReferenceType() && !type2.IsNullType()))
                        return false;
                    type1 = type1.AsTypeParameterType().GetEffectiveBaseClass();
                    break;
            }
            if (type2.IsNullType())
            {
                fRet = true;
                // We don't need to determine the actual best type since we're
                // returning true - indicating that we've found the best operator.
                typeCls = typeObj;
                goto LRecord;
            }

            switch (ft2)
            {
                default:
                    return false;
                case FUNDTYPE.FT_REF:
                    break;
                case FUNDTYPE.FT_VAR:
                    if (type2.AsTypeParameterType().IsValueType() || (!type2.AsTypeParameterType().IsReferenceType() && !type1.IsNullType()))
                        return false;
                    type2 = type2.AsTypeParameterType().GetEffectiveBaseClass();
                    break;
            }
            if (type1.IsNullType())
            {
                fRet = true;
                // We don't need to determine the actual best type since we're
                // returning true - indicating that we've found the best operator.
                typeCls = typeObj;
                goto LRecord;
            }

            if (!canCast(type1, type2, CONVERTTYPE.NOUDC) && !canCast(type2, type1, CONVERTTYPE.NOUDC))
                return false;

            if (type1.isInterfaceType() || type1.isPredefType(PredefinedType.PT_STRING) || GetSymbolLoader().HasBaseConversion(type1, typeDel))
                type1 = typeObj;
            else if (type1.IsArrayType())
                type1 = GetReqPDT(PredefinedType.PT_ARRAY);
            else if (!type1.isClassType())
                return false;

            if (type2.isInterfaceType() || type2.isPredefType(PredefinedType.PT_STRING) || GetSymbolLoader().HasBaseConversion(type2, typeDel))
                type2 = typeObj;
            else if (type2.IsArrayType())
                type2 = GetReqPDT(PredefinedType.PT_ARRAY);
            else if (!type2.isClassType())
                return false;

            Debug.Assert(type1.isClassType() && !type1.isPredefType(PredefinedType.PT_STRING) && !type1.isPredefType(PredefinedType.PT_DELEGATE));
            Debug.Assert(type2.isClassType() && !type2.isPredefType(PredefinedType.PT_STRING) && !type2.isPredefType(PredefinedType.PT_DELEGATE));

            if (GetSymbolLoader().HasBaseConversion(type2, type1))
                typeCls = type1;
            else if (GetSymbolLoader().HasBaseConversion(type1, type2))
                typeCls = type2;

            LRecord:
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

            int res = 0;

            switch (bt1)
            {
                default:
                    VSFAIL("Shouldn't happen");
                    break;
                case BetterType.Same:
                case BetterType.Neither:
                    break;
                case BetterType.Left:
                    res--;
                    break;
                case BetterType.Right:
                    res++;
                    break;
            }

            switch (bt2)
            {
                default:
                    VSFAIL("Shouldn't happen");
                    break;
                case BetterType.Same:
                case BetterType.Neither:
                    break;
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

        private static bool CalculateExprAndUnaryOpKinds(
                OperatorKind op,
                bool bChecked,
                out /*out*/ ExpressionKind ek,
                out /*out*/ UnaOpKind uok,
                out /*out*/ EXPRFLAG flags)
        {
            flags = 0;
            ek = 0;
            uok = 0;
            switch (op)
            {
                case OperatorKind.OP_UPLUS:
                    uok = UnaOpKind.Plus;
                    ek = ExpressionKind.EK_UPLUS;
                    break;

                case OperatorKind.OP_NEG:
                    if (bChecked)
                    {
                        flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    uok = UnaOpKind.Minus;
                    ek = ExpressionKind.EK_NEG;
                    break;

                case OperatorKind.OP_BITNOT:
                    uok = UnaOpKind.Tilde;
                    ek = ExpressionKind.EK_BITNOT;
                    break;

                case OperatorKind.OP_LOGNOT:
                    uok = UnaOpKind.Bang;
                    ek = ExpressionKind.EK_LOGNOT;
                    break;

                case OperatorKind.OP_POSTINC:
                    flags |= EXPRFLAG.EXF_ISPOSTOP;
                    if (bChecked)
                    {
                        flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    uok = UnaOpKind.IncDec;
                    ek = ExpressionKind.EK_ADD;
                    break;

                case OperatorKind.OP_PREINC:
                    if (bChecked)
                    {
                        flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    uok = UnaOpKind.IncDec;
                    ek = ExpressionKind.EK_ADD;
                    break;

                case OperatorKind.OP_POSTDEC:
                    flags |= EXPRFLAG.EXF_ISPOSTOP;
                    if (bChecked)
                    {
                        flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    uok = UnaOpKind.IncDec;
                    ek = ExpressionKind.EK_SUB;
                    break;

                case OperatorKind.OP_PREDEC:
                    if (bChecked)
                    {
                        flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    uok = UnaOpKind.IncDec;
                    ek = ExpressionKind.EK_SUB;
                    break;

                default:
                    VSFAIL("Bad op");
                    return false;
            }
            return true;
        }

        public Expr BindStandardUnaryOperator(OperatorKind op, Expr pArgument)
        {
            RETAILVERIFY(pArgument != null);

            ExpressionKind ek;
            UnaOpKind unaryOpKind;
            EXPRFLAG flags;

            if (pArgument.Type == null ||
                !CalculateExprAndUnaryOpKinds(
                           op,
                           Context.CheckedNormal,
                           out ek/*out*/,
                           out unaryOpKind/*out*/,
                           out flags/*out*/))
            {
                return BadOperatorTypesError(ExpressionKind.EK_UNARYOP, pArgument, null);
            }

            UnaOpMask unaryOpMask = (UnaOpMask)(1 << (int)unaryOpKind);
            CType type = pArgument.Type;

            List<UnaOpFullSig> pSignatures = new List<UnaOpFullSig>();

            Expr pResult = null;
            UnaryOperatorSignatureFindResult eResultOfSignatureFind = PopulateSignatureList(pArgument, unaryOpKind, unaryOpMask, ek, flags, pSignatures, out pResult);

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
                        return BadOperatorTypesError(ek, pArgument, null);
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
                            return ambiguousOperatorError(ek, pArgument, null);
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
                                return ambiguousOperatorError(ek, pArgument, null);
                            }
                        }
                    }
                }
                else
                {
                    nBestSignature = pSignatures.Count - 1;
                }
            }

            RETAILVERIFY(nBestSignature < pSignatures.Count);

            UnaOpFullSig uofs = pSignatures[nBestSignature];

            if (uofs.pfn == null)
            {
                if (unaryOpKind == UnaOpKind.IncDec)
                {
                    return BindIncOp(ek, flags, pArgument, uofs);
                }
                return BadOperatorTypesError(ek, pArgument, null);
            }

            if (uofs.isLifted())
            {
                return BindLiftedStandardUnop(ek, flags, pArgument, uofs);
            }

            // Try the conversion - if it fails, do a cast without user defined casts.
            Expr arg = tryConvert(pArgument, uofs.GetType());
            if (arg == null)
            {
                arg = mustCast(pArgument, uofs.GetType(), CONVERTTYPE.NOUDC);
            }
            return uofs.pfn(ek, flags, arg);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private UnaryOperatorSignatureFindResult PopulateSignatureList(Expr pArgument, UnaOpKind unaryOpKind, UnaOpMask unaryOpMask, ExpressionKind exprKind, EXPRFLAG flags, List<UnaOpFullSig> pSignatures, out Expr ppResult)
        {
            // We should have already checked argument != null and argument.type != null.
            Debug.Assert(pArgument != null);
            Debug.Assert(pArgument.Type != null);

            ppResult = null;
            CType pArgumentType = pArgument.Type;
            CType pRawType = pArgumentType.StripNubs();
            PredefinedType ptRaw = pRawType.isPredefined() ? pRawType.getPredefType() : PredefinedType.PT_COUNT;

            // Find all applicable operator signatures.
            // First check for special ones (enum, ptr) and check for user defined ops.

            if (ptRaw > PredefinedType.PT_ULONG)
            {
                // Enum types are special in that they carry a set of "predefined" operators (~ and inc/dec).
                if (pRawType.isEnumType())
                {
                    if ((unaryOpMask & (UnaOpMask.Tilde | UnaOpMask.IncDec)) != 0)
                    {
                        // We have an exact match.
                        LiftFlags liftFlags = LiftFlags.None;
                        CType typeSig = pArgumentType;

                        if (typeSig.IsNullableType())
                        {
                            if (typeSig.AsNullableType().GetUnderlyingType() != pRawType)
                            {
                                typeSig = GetSymbolLoader().GetTypeManager().GetNullable(pRawType);
                            }
                            liftFlags = LiftFlags.Lift1;
                        }
                        if (unaryOpKind == UnaOpKind.Tilde)
                        {
                            pSignatures.Add(new UnaOpFullSig(
                                    typeSig.getAggregate().GetUnderlyingType(),
                                    BindEnumUnaOp,
                                    liftFlags,
                                    UnaOpFuncKind.EnumUnaOp));
                        }
                        else
                        {
                            // For enums, we want to add the signature as the underlying type so that we'll
                            // perform the conversions to and from the enum type.
                            pSignatures.Add(new UnaOpFullSig(
                                    typeSig.getAggregate().GetUnderlyingType(),
                                    null,
                                    liftFlags,
                                    UnaOpFuncKind.None));
                        }
                        return UnaryOperatorSignatureFindResult.Match;
                    }
                }
                else if (unaryOpKind == UnaOpKind.IncDec)
                {
                    // Check for pointers
                    if (pArgumentType.IsPointerType())
                    {
                        pSignatures.Add(new UnaOpFullSig(
                                pArgumentType,
                                null,
                                LiftFlags.None,
                                UnaOpFuncKind.None));
                        return UnaryOperatorSignatureFindResult.Match;
                    }

                    // Check for user defined inc/dec
#if !CSEE
                    ExprMultiGet exprGet = GetExprFactory().CreateMultiGet(0, pArgumentType, null);
#else // CSEE

                    Expr exprGet = pArgument;
#endif // CSEE

                    Expr exprVal = bindUDUnop((ExpressionKind)(exprKind - ExpressionKind.EK_ADD + ExpressionKind.EK_INC), exprGet);
                    if (exprVal != null)
                    {
                        if (exprVal.Type != null && !exprVal.Type.IsErrorType() && exprVal.Type != pArgumentType)
                        {
                            exprVal = mustConvert(exprVal, pArgumentType);
                        }

                        Debug.Assert(pArgument != null);
                        ExprMulti exprMulti = GetExprFactory().CreateMulti(EXPRFLAG.EXF_ASSGOP | flags, pArgumentType, pArgument, exprVal);
#if ! CSEE
                        exprGet.OptionalMulti = exprMulti;
#endif // !CSEE

                        // Check whether Lvalue can be assigned. checkLvalue may return true 
                        // despite reporting an error. 
                        if (!checkLvalue(pArgument, CheckLvalueKind.Increment))
                        {
                            // This seems like it can never be reached - exprVal is only valid if 
                            // we have a UDUnop, and in order for checkLValue to return false, either the 
                            // arg has to not be OK, in which case we shouldn't get here, or we have an 
                            // AnonMeth, Lambda, or Constant, all of which cannot have UDUnops defined for them. 
                            exprMulti.SetError();
                        }
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

        private bool FindApplicableSignatures(
                Expr pArgument,
                UnaOpMask unaryOpMask,
                List<UnaOpFullSig> pSignatures)
        {
            // All callers should already assert this to be the case.
            Debug.Assert(pArgument != null);
            Debug.Assert(pArgument.Type != null);

            long iuosMinLift = GetSymbolLoader().FCanLift() ? 0 : g_rguos.Length;

            CType pArgumentType = pArgument.Type;
            CType pRawType = pArgumentType.StripNubs();
            PredefinedType pt = pArgumentType.isPredefined() ? pArgumentType.getPredefType() : PredefinedType.PT_COUNT;
            PredefinedType ptRaw = pRawType.isPredefined() ? pRawType.getPredefType() : PredefinedType.PT_COUNT;

            for (int index = 0; index < g_rguos.Length; index++)
            {
                UnaOpSig uos = g_rguos[index];
                if ((uos.grfuom & unaryOpMask) == 0)
                {
                    continue;
                }

                ConvKind cv = GetConvKind(pt, g_rguos[index].pt);
                CType typeSig = null;

                switch (cv)
                {
                    default:
                        VSFAIL("Shouldn't happen!");
                        continue;

                    case ConvKind.None:
                        continue;

                    case ConvKind.Explicit:
                        if (!pArgument.isCONSTANT_OK())
                        {
                            continue;
                        }
                        if (canConvert(pArgument, typeSig = GetOptPDT(uos.pt)))
                        {
                            break;
                        }
                        if (index < iuosMinLift)
                        {
                            continue;
                        }
                        typeSig = GetSymbolLoader().GetTypeManager().GetNullable(typeSig);
                        if (!canConvert(pArgument, typeSig))
                        {
                            continue;
                        }
                        break;

                    case ConvKind.Unknown:
                        if (canConvert(pArgument, typeSig = GetOptPDT(uos.pt)))
                        {
                            break;
                        }
                        if (index < iuosMinLift)
                        {
                            continue;
                        }
                        typeSig = GetSymbolLoader().GetTypeManager().GetNullable(typeSig);
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

                if (typeSig != null && typeSig.IsNullableType())
                {
                    // Need to use a lifted signature.
                    LiftFlags grflt = LiftFlags.None;

                    switch (GetConvKind(ptRaw, uos.pt))
                    {
                        default:
                            grflt = grflt | LiftFlags.Convert1;
                            break;
                        case ConvKind.Implicit:
                        case ConvKind.Identity:
                            grflt = grflt | LiftFlags.Lift1;
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

        private Expr BindLiftedStandardUnop(ExpressionKind ek, EXPRFLAG flags, Expr arg, UnaOpFullSig uofs)
        {
            NullableType type = uofs.GetType().AsNullableType();
            Debug.Assert(arg?.Type != null);
            if (arg.Type.IsNullType())
            {
                return BadOperatorTypesError(ek, arg, null, type);
            }

            Expr pArgument = null;
            Expr nonLiftedArg = null;

            LiftArgument(arg, uofs.GetType(), uofs.Convert(), out pArgument, out nonLiftedArg);

            // Now call the function with the non lifted arguments to report errors.
            Expr nonLiftedResult = uofs.pfn(ek, flags, nonLiftedArg);
            ExprUnaryOp exprRes = GetExprFactory().CreateUnaryOp(ek, type, pArgument);
            mustCast(nonLiftedResult, type, 0);
            exprRes.Flags |= flags;

            Debug.Assert((exprRes.Flags & EXPRFLAG.EXF_LVALUE) == 0);
            return exprRes;
        }

        /*
            Determine which UnaOpSig is better for overload resolution.
            Returns negative if iuos1 is better, positive if iuos2 is better, 0 if neither.
        */
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

            switch (bt)
            {
                default:
                    VSFAIL("Shouldn't happen");
                    return 0;
                case BetterType.Same:
                case BetterType.Neither:
                    return 0;
                case BetterType.Left:
                    return -1;
                case BetterType.Right:
                    return +1;
            }
        }

        /*
            Handles standard binary integer based operators.
        */
        private Expr BindIntBinOp(ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(arg1.Type.isPredefined() && arg2.Type.isPredefined() && arg1.Type.getPredefType() == arg2.Type.getPredefType());
            return BindIntOp(ek, flags, arg1, arg2, arg1.Type.getPredefType());
        }


        /*
            Handles standard unary integer based operators.
        */
        private Expr BindIntUnaOp(ExpressionKind ek, EXPRFLAG flags, Expr arg)
        {
            Debug.Assert(arg.Type.isPredefined());
            return BindIntOp(ek, flags, arg, null, arg.Type.getPredefType());
        }


        /*
            Handles standard binary floating point (float, double) based operators.
        */
        private Expr BindRealBinOp(ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(arg1.Type.isPredefined() && arg2.Type.isPredefined() && arg1.Type.getPredefType() == arg2.Type.getPredefType());
            return bindFloatOp(ek, flags, arg1, arg2);
        }


        /*
            Handles standard unary floating point (float, double) based operators.
        */
        private Expr BindRealUnaOp(ExpressionKind ek, EXPRFLAG flags, Expr arg)
        {
            Debug.Assert(arg.Type.isPredefined());
            return bindFloatOp(ek, flags, arg, null);
        }


        /*
            Handles standard increment and decrement operators.
        */
        private Expr BindIncOp(ExpressionKind ek, EXPRFLAG flags, Expr arg, UnaOpFullSig uofs)
        {
            Debug.Assert(ek == ExpressionKind.EK_ADD || ek == ExpressionKind.EK_SUB);
            if (!checkLvalue(arg, CheckLvalueKind.Increment))
            {
                Expr rval = GetExprFactory().CreateBinop(ek, arg.Type, arg, null);
                rval.SetError();
                return rval;
            }

            CType typeRaw = uofs.GetType().StripNubs();

            FUNDTYPE ft = typeRaw.fundType();
            if (ft == FUNDTYPE.FT_R8 || ft == FUNDTYPE.FT_R4)
            {
                flags = ~EXPRFLAG.EXF_CHECKOVERFLOW;
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

        private Expr BindIncOpCore(ExpressionKind ek, EXPRFLAG flags, Expr exprVal, CType type)
        {
            Debug.Assert(ek == ExpressionKind.EK_ADD || ek == ExpressionKind.EK_SUB);
            ConstVal cv;
            Expr pExprResult = null;

            if (type.isEnumType() && type.fundType() > FUNDTYPE.FT_LASTINTEGRAL)
            {
                // This is an error case when enum derives from an illegal type. Just treat it as an int.
                type = GetReqPDT(PredefinedType.PT_INT);
            }

            FUNDTYPE ft = type.fundType();
            CType typeTmp = type;

            switch (ft)
            {
                default:
                    {
                        Debug.Assert(type.isPredefType(PredefinedType.PT_DECIMAL));
                        ek = ek == ExpressionKind.EK_ADD ? ExpressionKind.EK_DECIMALINC : ExpressionKind.EK_DECIMALDEC;
                        PREDEFMETH predefMeth = ek == ExpressionKind.EK_DECIMALINC ? PREDEFMETH.PM_DECIMAL_OPINCREMENT : PREDEFMETH.PM_DECIMAL_OPDECREMENT;
                        pExprResult = CreateUnaryOpForPredefMethodCall(ek, predefMeth, type, exprVal);
                    }
                    break;
                case FUNDTYPE.FT_PTR:
                    cv = ConstVal.Get(1);
                    pExprResult = BindPtrBinOp(ek, flags, exprVal, GetExprFactory().CreateConstant(GetReqPDT(PredefinedType.PT_INT), cv));
                    break;
                case FUNDTYPE.FT_I1:
                case FUNDTYPE.FT_I2:
                case FUNDTYPE.FT_U1:
                case FUNDTYPE.FT_U2:
                    typeTmp = GetReqPDT(PredefinedType.PT_INT);
                    cv = ConstVal.Get(1);
                    pExprResult = LScalar(ek, flags, exprVal, type, cv, pExprResult, typeTmp);
                    break;
                case FUNDTYPE.FT_I4:
                case FUNDTYPE.FT_U4:
                    cv = ConstVal.Get(1);
                    pExprResult = LScalar(ek, flags, exprVal, type, cv, pExprResult, typeTmp);
                    break;
                case FUNDTYPE.FT_I8:
                case FUNDTYPE.FT_U8:
                    cv = ConstVal.Get((long)1);
                    pExprResult = LScalar(ek, flags, exprVal, type, cv, pExprResult, typeTmp);
                    break;
                case FUNDTYPE.FT_R4:
                case FUNDTYPE.FT_R8:
                    cv = ConstVal.Get(1.0);
                    pExprResult = LScalar(ek, flags, exprVal, type, cv, pExprResult, typeTmp);
                    break;
            }
            Debug.Assert(pExprResult != null);
            Debug.Assert(!pExprResult.Type.IsNullableType());
            return pExprResult;
        }

        private Expr LScalar(ExpressionKind ek, EXPRFLAG flags, Expr exprVal, CType type, ConstVal cv, Expr pExprResult, CType typeTmp)
        {
            CType typeOne = type;
            if (typeOne.isEnumType())
            {
                typeOne = typeOne.underlyingEnumType();
            }
            pExprResult = GetExprFactory().CreateBinop(ek, typeTmp, exprVal, GetExprFactory().CreateConstant(typeOne, cv));
            pExprResult.Flags |= flags;
            if (typeTmp != type)
            {
                pExprResult = mustCast(pExprResult, type, CONVERTTYPE.NOUDC);
            }
            return pExprResult;
        }

        private ExprMulti BindNonliftedIncOp(ExpressionKind ek, EXPRFLAG flags, Expr arg, UnaOpFullSig uofs)
        {
            Debug.Assert(ek == ExpressionKind.EK_ADD || ek == ExpressionKind.EK_SUB);
            Debug.Assert(!uofs.isLifted());

            Debug.Assert(arg != null);
#if ! CSEE
            ExprMultiGet exprGet = GetExprFactory().CreateMultiGet(EXPRFLAG.EXF_ASSGOP, arg.Type, null);
            Expr exprVal = exprGet;
#else
            Expr exprVal = arg;
#endif

            CType type = uofs.GetType();
            Debug.Assert(!type.IsNullableType());

            // These used to be converts, but we're making them casts now - this is because
            // we need to remove the ability to call inc(sbyte) etc for all types smaller than int. 
            // Note however, that this will give us different error messages on compile time versus runtime
            // for checked increments.
            //
            // Also, we changed it so that we now generate the cast to and from enum for enum increments.
            exprVal = mustCast(exprVal, type);
            exprVal = BindIncOpCore(ek, flags, exprVal, type);
            Expr op = mustCast(exprVal, arg.Type, CONVERTTYPE.NOUDC);

            ExprMulti exprMulti = GetExprFactory().CreateMulti(EXPRFLAG.EXF_ASSGOP | flags, arg.Type, arg, op);

#if ! CSEE
            exprGet.OptionalMulti = exprMulti;
#endif
            return exprMulti;
        }

        private ExprMulti BindLiftedIncOp(ExpressionKind ek, EXPRFLAG flags, Expr arg, UnaOpFullSig uofs)
        {
            Debug.Assert(ek == ExpressionKind.EK_ADD || ek == ExpressionKind.EK_SUB);
            Debug.Assert(uofs.isLifted());

            NullableType type = uofs.GetType().AsNullableType();
            Debug.Assert(arg != null);

#if ! CSEE
            ExprMultiGet exprGet = GetExprFactory().CreateMultiGet(EXPRFLAG.EXF_ASSGOP, arg.Type, null);
            Expr exprVal = exprGet;
#else
            Expr exprVal = arg;
#endif

            Expr nonLiftedResult = null;
            Expr nonLiftedArg = exprVal;

            // We want to give the lifted argument as the binop, but use the non-lifted argument as the 
            // argument of the call.
            //Debug.Assert(uofs.LiftArg() || type.IsValType());
            nonLiftedArg = mustCast(nonLiftedArg, type.GetUnderlyingType());
            nonLiftedResult = BindIncOpCore(ek, flags, nonLiftedArg, type.GetUnderlyingType());
            exprVal = mustCast(exprVal, type);
            ExprUnaryOp exprRes = GetExprFactory().CreateUnaryOp((ek == ExpressionKind.EK_ADD) ? ExpressionKind.EK_INC : ExpressionKind.EK_DEC, arg.Type/* type */, exprVal);
            mustCast(mustCast(nonLiftedResult, type), arg.Type);
            exprRes.Flags |= flags;

            ExprMulti exprMulti = GetExprFactory().CreateMulti(EXPRFLAG.EXF_ASSGOP | flags, arg.Type, arg, exprRes);

#if ! CSEE
            exprGet.OptionalMulti = exprMulti;
#endif
            return exprMulti;
        }

        /*
            Handles standard binary decimal based operators.
            This function is called twice by the EE for every binary operator it evaluates
            Here is how it works.
        */
        private Expr BindDecBinOp(ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(arg1.Type.isPredefType(PredefinedType.PT_DECIMAL) && arg2.Type.isPredefType(PredefinedType.PT_DECIMAL));

            CType typeDec = GetOptPDT(PredefinedType.PT_DECIMAL);
            Debug.Assert(typeDec != null);

            CType typeRet;

            switch (ek)
            {
                default:
                    VSFAIL("Bad kind");
                    typeRet = null;
                    break;
                case ExpressionKind.EK_ADD:
                case ExpressionKind.EK_SUB:
                case ExpressionKind.EK_MUL:
                case ExpressionKind.EK_DIV:
                case ExpressionKind.EK_MOD:
                    typeRet = typeDec;
                    break;
                case ExpressionKind.EK_LT:
                case ExpressionKind.EK_LE:
                case ExpressionKind.EK_GT:
                case ExpressionKind.EK_GE:
                case ExpressionKind.EK_EQ:
                case ExpressionKind.EK_NE:
                    typeRet = GetReqPDT(PredefinedType.PT_BOOL);
                    break;
            }

#if CSEE
            // In the EE, we want to emit an EXPRBINOP with the
            // right EK so that when we evalsync we can just do the work ourselves instead of
            // delegating to method calls.

            return GetExprFactory().CreateBinop(tree, ek, typeRet, arg1, arg2);

#endif // CSEE

            return GetExprFactory().CreateBinop(ek, typeRet, arg1, arg2);
        }


        /*
            Handles standard unary decimal based operators.
        */
        private Expr BindDecUnaOp(ExpressionKind ek, EXPRFLAG flags, Expr arg)
        {
            Debug.Assert(arg.Type.isPredefType(PredefinedType.PT_DECIMAL));
            Debug.Assert(ek == ExpressionKind.EK_NEG || ek == ExpressionKind.EK_UPLUS);

            CType typeDec = GetOptPDT(PredefinedType.PT_DECIMAL);
            Debug.Assert(typeDec != null);

            if (ek == ExpressionKind.EK_NEG)
            {
                PREDEFMETH predefMeth = PREDEFMETH.PM_DECIMAL_OPUNARYMINUS;
                return CreateUnaryOpForPredefMethodCall(ExpressionKind.EK_DECIMALNEG, predefMeth, typeDec, arg);
            }
            return GetExprFactory().CreateUnaryOp(ExpressionKind.EK_UPLUS, typeDec, arg);
        }


        /*
            Handles string concatenation.
        */
        private Expr BindStrBinOp(ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(ek == ExpressionKind.EK_ADD);
            Debug.Assert(arg1.Type.isPredefType(PredefinedType.PT_STRING) || arg2.Type.isPredefType(PredefinedType.PT_STRING));
            return bindStringConcat(arg1, arg2);
        }


        /*
            Bind a shift operator: <<, >>. These can have integer or long first operands,
            and second operand must be int.
        */
        private Expr BindShiftOp(ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(ek == ExpressionKind.EK_LSHIFT || ek == ExpressionKind.EK_RSHIFT);
            Debug.Assert(arg1.Type.isPredefined());
            Debug.Assert(arg2.Type.isPredefType(PredefinedType.PT_INT));

            PredefinedType ptOp = arg1.Type.getPredefType();
            Debug.Assert(ptOp == PredefinedType.PT_INT || ptOp == PredefinedType.PT_UINT || ptOp == PredefinedType.PT_LONG || ptOp == PredefinedType.PT_ULONG);

            return GetExprFactory().CreateBinop(ek, arg1.Type, arg1, arg2);
        }

        /*
            Bind a bool binary operator: ==, !=, &&, ||, , |, ^. If both operands are constant, the
            result will be a constant also.
        */
        private Expr BindBoolBinOp(ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(arg1 != null);
            Debug.Assert(arg2 != null);
            Debug.Assert(arg1.Type.isPredefType(PredefinedType.PT_BOOL) || (arg1.Type.IsNullableType() && arg2.Type.AsNullableType().GetUnderlyingType().isPredefType(PredefinedType.PT_BOOL)));
            Debug.Assert(arg2.Type.isPredefType(PredefinedType.PT_BOOL) || (arg2.Type.IsNullableType() && arg2.Type.AsNullableType().GetUnderlyingType().isPredefType(PredefinedType.PT_BOOL)));

            Expr exprRes = GetExprFactory().CreateBinop(ek, GetReqPDT(PredefinedType.PT_BOOL), arg1, arg2);

            return exprRes;
        }

        private Expr BindBoolBitwiseOp(ExpressionKind ek, EXPRFLAG flags, Expr expr1, Expr expr2, BinOpFullSig bofs)
        {
            Debug.Assert(ek == ExpressionKind.EK_BITAND || ek == ExpressionKind.EK_BITOR);
            Debug.Assert(expr1.Type.isPredefType(PredefinedType.PT_BOOL) || expr1.Type.IsNullableType() && expr1.Type.AsNullableType().GetUnderlyingType().isPredefType(PredefinedType.PT_BOOL));
            Debug.Assert(expr2.Type.isPredefType(PredefinedType.PT_BOOL) || expr2.Type.IsNullableType() && expr2.Type.AsNullableType().GetUnderlyingType().isPredefType(PredefinedType.PT_BOOL));

            if (expr1.Type.IsNullableType() || expr2.Type.IsNullableType())
            {
                CType typeBool = GetReqPDT(PredefinedType.PT_BOOL);
                CType typeRes = GetSymbolLoader().GetTypeManager().GetNullable(typeBool);

                // Get the non-lifted result.
                Expr nonLiftedArg1 = CNullable.StripNullableConstructor(expr1);
                Expr nonLiftedArg2 = CNullable.StripNullableConstructor(expr2);
                Expr nonLiftedResult = null;

                if (!nonLiftedArg1.Type.IsNullableType() && !nonLiftedArg2.Type.IsNullableType())
                {
                    nonLiftedResult = BindBoolBinOp(ek, flags, nonLiftedArg1, nonLiftedArg2);
                }

                // Make the binop and set that its lifted.
                ExprBinOp exprRes = GetExprFactory().CreateBinop(ek, typeRes, expr1, expr2);
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
            return BindBoolBinOp(ek, flags, expr1, expr2);
        }

        private Expr BindLiftedBoolBitwiseOp(ExpressionKind ek, EXPRFLAG flags, Expr expr1, Expr expr2)
        {
            return null;
        }


        /*
            Handles boolean unary operator (!).
        */
        private Expr BindBoolUnaOp(ExpressionKind ek, EXPRFLAG flags, Expr arg)
        {
            Debug.Assert(arg.Type.isPredefType(PredefinedType.PT_BOOL));
            Debug.Assert(ek == ExpressionKind.EK_LOGNOT);

            // Get the result type and operand type.
            CType typeBool = GetReqPDT(PredefinedType.PT_BOOL);

            // Determine if arg has a constant value.
            // Strip off EXPRKIND.EK_SEQUENCE for constant checking.

            Expr argConst = arg.GetConst();

            if (argConst == null)
                return GetExprFactory().CreateUnaryOp(ExpressionKind.EK_LOGNOT, typeBool, arg);

            return GetExprFactory().CreateConstant(typeBool, ConstVal.Get(((ExprConstant)argConst).Val.Int32Val == 0));
        }


        /*
            Handles string equality.
        */
        private Expr BindStrCmpOp(ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(ek == ExpressionKind.EK_EQ || ek == ExpressionKind.EK_NE);
            Debug.Assert(arg1.Type.isPredefType(PredefinedType.PT_STRING) && arg2.Type.isPredefType(PredefinedType.PT_STRING));

            // Get the predefined method for string comparison, and then stash it in the Expr so we can 
            // transform it later.

            PREDEFMETH predefMeth = ek == ExpressionKind.EK_EQ ? PREDEFMETH.PM_STRING_OPEQUALITY : PREDEFMETH.PM_STRING_OPINEQUALITY;
            ek = ek == ExpressionKind.EK_EQ ? ExpressionKind.EK_STRINGEQ : ExpressionKind.EK_STRINGNE;
            return CreateBinopForPredefMethodCall(ek, predefMeth, GetReqPDT(PredefinedType.PT_BOOL), arg1, arg2);
        }


        /*
            Handles reference equality operators. Type variables come through here.
        */
        private Expr BindRefCmpOp(ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(ek == ExpressionKind.EK_EQ || ek == ExpressionKind.EK_NE);

            // Must box type variables for the verifier.
            arg1 = mustConvert(arg1, GetReqPDT(PredefinedType.PT_OBJECT), CONVERTTYPE.NOUDC);
            arg2 = mustConvert(arg2, GetReqPDT(PredefinedType.PT_OBJECT), CONVERTTYPE.NOUDC);

            return GetExprFactory().CreateBinop(ek, GetReqPDT(PredefinedType.PT_BOOL), arg1, arg2);
        }


        /*
            Handles delegate binary operators.
        */
        private Expr BindDelBinOp(ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(ek == ExpressionKind.EK_ADD || ek == ExpressionKind.EK_SUB || ek == ExpressionKind.EK_EQ || ek == ExpressionKind.EK_NE);
            Debug.Assert(arg1.Type == arg2.Type && (arg1.Type.isDelegateType() || arg1.Type.isPredefType(PredefinedType.PT_DELEGATE)));

            PREDEFMETH predefMeth = (PREDEFMETH)0;
            CType RetType = null;
            switch (ek)
            {
                case ExpressionKind.EK_ADD:
                    predefMeth = PREDEFMETH.PM_DELEGATE_COMBINE;
                    RetType = arg1.Type;
                    ek = ExpressionKind.EK_DELEGATEADD;
                    break;

                case ExpressionKind.EK_SUB:
                    predefMeth = PREDEFMETH.PM_DELEGATE_REMOVE;
                    RetType = arg1.Type;
                    ek = ExpressionKind.EK_DELEGATESUB;
                    break;

                case ExpressionKind.EK_EQ:
                    predefMeth = PREDEFMETH.PM_DELEGATE_OPEQUALITY;
                    RetType = GetReqPDT(PredefinedType.PT_BOOL);
                    ek = ExpressionKind.EK_DELEGATEEQ;
                    break;

                case ExpressionKind.EK_NE:
                    predefMeth = PREDEFMETH.PM_DELEGATE_OPINEQUALITY;
                    RetType = GetReqPDT(PredefinedType.PT_BOOL);
                    ek = ExpressionKind.EK_DELEGATENE;
                    break;
            }
            return CreateBinopForPredefMethodCall(ek, predefMeth, RetType, arg1, arg2);
        }


        /*
            Handles enum binary operators.
        */
        private Expr BindEnumBinOp(ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            AggregateType typeEnum = null;
            AggregateType typeDst = GetEnumBinOpType(ek, arg1.Type, arg2.Type, out typeEnum);

            Debug.Assert(typeEnum != null);
            PredefinedType ptOp;

            switch (typeEnum.fundType())
            {
                default:
                    // Promote all smaller types to int.
                    ptOp = PredefinedType.PT_INT;
                    break;
                case FUNDTYPE.FT_U4:
                    ptOp = PredefinedType.PT_UINT;
                    break;
                case FUNDTYPE.FT_I8:
                    ptOp = PredefinedType.PT_LONG;
                    break;
                case FUNDTYPE.FT_U8:
                    ptOp = PredefinedType.PT_ULONG;
                    break;
            }

            CType typeOp = GetReqPDT(ptOp);
            arg1 = mustCast(arg1, typeOp, CONVERTTYPE.NOUDC);
            arg2 = mustCast(arg2, typeOp, CONVERTTYPE.NOUDC);

            Expr exprRes = BindIntOp(ek, flags, arg1, arg2, ptOp);

            if (!exprRes.IsOK)
            {
                return exprRes;
            }

            if (exprRes.Type != typeDst)
            {
                Debug.Assert(!typeDst.isPredefType(PredefinedType.PT_BOOL));
                exprRes = mustCast(exprRes, typeDst, CONVERTTYPE.NOUDC);
            }

            return exprRes;
        }

        private Expr BindLiftedEnumArithmeticBinOp(ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            Debug.Assert(ek == ExpressionKind.EK_ADD || ek == ExpressionKind.EK_SUB);
            CType nonNullableType1 = arg1.Type.IsNullableType() ? arg1.Type.AsNullableType().UnderlyingType : arg1.Type;
            CType nonNullableType2 = arg2.Type.IsNullableType() ? arg2.Type.AsNullableType().UnderlyingType : arg2.Type;
            if (nonNullableType1.IsNullType())
            {
                nonNullableType1 = nonNullableType2.underlyingEnumType();
            }
            else if (nonNullableType2.IsNullType())
            {
                nonNullableType2 = nonNullableType1.underlyingEnumType();
            }

            NullableType typeDst = GetTypes().GetNullable(GetEnumBinOpType(ek, nonNullableType1, nonNullableType2, out AggregateType typeEnum));

            Debug.Assert(typeEnum != null);
            PredefinedType ptOp;

            switch (typeEnum.fundType())
            {
                default:
                    // Promote all smaller types to int.
                    ptOp = PredefinedType.PT_INT;
                    break;
                case FUNDTYPE.FT_U4:
                    ptOp = PredefinedType.PT_UINT;
                    break;
                case FUNDTYPE.FT_I8:
                    ptOp = PredefinedType.PT_LONG;
                    break;
                case FUNDTYPE.FT_U8:
                    ptOp = PredefinedType.PT_ULONG;
                    break;
            }

            NullableType typeOp = GetTypes().GetNullable(GetReqPDT(ptOp));
            arg1 = mustCast(arg1, typeOp, CONVERTTYPE.NOUDC);
            arg2 = mustCast(arg2, typeOp, CONVERTTYPE.NOUDC);

            ExprBinOp exprRes = GetExprFactory().CreateBinop(ek, typeOp, arg1, arg2);
            exprRes.IsLifted = true;
            exprRes.Flags |= flags;
            Debug.Assert((exprRes.Flags & EXPRFLAG.EXF_LVALUE) == 0);

            if (!exprRes.IsOK)
            {
                return exprRes;
            }

            if (exprRes.Type != typeDst)
            {
                return mustCast(exprRes, typeDst, CONVERTTYPE.NOUDC);
            }

            return exprRes;
        }


        /*
            Handles enum unary operator (~).
        */
        private Expr BindEnumUnaOp(ExpressionKind ek, EXPRFLAG flags, Expr arg)
        {
            Debug.Assert(ek == ExpressionKind.EK_BITNOT);
            Debug.Assert((ExprCast)arg != null);
            Debug.Assert(((ExprCast)arg).Argument.Type.isEnumType());

            PredefinedType ptOp;
            CType typeEnum = ((ExprCast)arg).Argument.Type;

            switch (typeEnum.fundType())
            {
                default:
                    // Promote all smaller types to int.
                    ptOp = PredefinedType.PT_INT;
                    break;
                case FUNDTYPE.FT_U4:
                    ptOp = PredefinedType.PT_UINT;
                    break;
                case FUNDTYPE.FT_I8:
                    ptOp = PredefinedType.PT_LONG;
                    break;
                case FUNDTYPE.FT_U8:
                    ptOp = PredefinedType.PT_ULONG;
                    break;
            }

            CType typeOp = GetReqPDT(ptOp);
            arg = mustCast(arg, typeOp, CONVERTTYPE.NOUDC);

            Expr exprRes = BindIntOp(ek, flags, arg, null, ptOp);

            if (!exprRes.IsOK)
            {
                return exprRes;
            }

            return mustCastInUncheckedContext(exprRes, typeEnum, CONVERTTYPE.NOUDC);
        }


        /*
            Handles pointer binary operators (+ and -).
        */
        private Expr BindPtrBinOp(ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            return null;
        }


        /*
            Handles pointer comparison operators.
        */
        private Expr BindPtrCmpOp(ExpressionKind ek, EXPRFLAG flags, Expr arg1, Expr arg2)
        {
            return null;
        }


        /*
            Given a binary operator EXPRKIND, get the BinOpKind and flags.
        */
        private bool GetBinopKindAndFlags(ExpressionKind ek, out BinOpKind pBinopKind, out EXPRFLAG flags)
        {
            flags = 0;
            switch (ek)
            {
                case ExpressionKind.EK_ADD:
                    if (Context.CheckedNormal)
                    {
                        flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    pBinopKind = BinOpKind.Add;
                    break;
                case ExpressionKind.EK_SUB:
                    if (Context.CheckedNormal)
                    {
                        flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    pBinopKind = BinOpKind.Sub;
                    break;
                case ExpressionKind.EK_DIV:
                case ExpressionKind.EK_MOD:
                    // EXPRKIND.EK_DIV and EXPRKIND.EK_MOD need to be treated special for hasSideEffects, 
                    // hence the EXPRFLAG.EXF_ASSGOP. Yes, this is a hack.
                    flags |= EXPRFLAG.EXF_ASSGOP;
                    if (Context.CheckedNormal)
                    {
                        flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    pBinopKind = BinOpKind.Mul;
                    break;
                case ExpressionKind.EK_MUL:
                    if (Context.CheckedNormal)
                    {
                        flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
                    }
                    pBinopKind = BinOpKind.Mul;
                    break;
                case ExpressionKind.EK_BITAND:
                case ExpressionKind.EK_BITOR:
                    pBinopKind = BinOpKind.Bitwise;
                    break;
                case ExpressionKind.EK_BITXOR:
                    pBinopKind = BinOpKind.BitXor;
                    break;
                case ExpressionKind.EK_LSHIFT:
                case ExpressionKind.EK_RSHIFT:
                    pBinopKind = BinOpKind.Shift;
                    break;
                case ExpressionKind.EK_LOGOR:
                case ExpressionKind.EK_LOGAND:
                    pBinopKind = BinOpKind.Logical;
                    break;
                case ExpressionKind.EK_LT:
                case ExpressionKind.EK_LE:
                case ExpressionKind.EK_GT:
                case ExpressionKind.EK_GE:
                    pBinopKind = BinOpKind.Compare;
                    break;
                case ExpressionKind.EK_EQ:
                case ExpressionKind.EK_NE:
                    pBinopKind = BinOpKind.Equal;
                    break;
                default:
                    VSFAIL("Bad ek");
                    pBinopKind = BinOpKind.Add;
                    return false;
            }
            return true;
        }

        /*
            Convert an expression involving I4, U4, I8 or U8 operands. The operands are
            assumed to be already converted to the correct types.
        */
        private Expr BindIntOp(ExpressionKind kind, EXPRFLAG flags, Expr op1, Expr op2, PredefinedType ptOp)
        {
            //Debug.Assert(kind.isRelational() || kind.isArithmetic() || kind.isBitwise());
            Debug.Assert(ptOp == PredefinedType.PT_INT || ptOp == PredefinedType.PT_UINT || ptOp == PredefinedType.PT_LONG || ptOp == PredefinedType.PT_ULONG);
            CType typeOp = GetReqPDT(ptOp);
            Debug.Assert(typeOp != null);
            Debug.Assert(op1 != null && op1.Type == typeOp);
            Debug.Assert(op2 == null || op2.Type == typeOp);
            Debug.Assert((op2 == null) == (kind == ExpressionKind.EK_NEG || kind == ExpressionKind.EK_UPLUS || kind == ExpressionKind.EK_BITNOT));

            if (kind == ExpressionKind.EK_NEG)
            {
                return BindIntegerNeg(flags, op1, ptOp);
            }

            CType typeDest = kind.isRelational() ? GetReqPDT(PredefinedType.PT_BOOL) : typeOp;

            Expr exprRes = GetExprFactory().CreateOperator(kind, typeDest, op1, op2);
            exprRes.Flags |= flags;
            Debug.Assert((exprRes.Flags & EXPRFLAG.EXF_LVALUE) == 0);
            return exprRes;
        }

        private Expr BindIntegerNeg(EXPRFLAG flags, Expr op, PredefinedType ptOp)
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
            CType typeOp = GetReqPDT(ptOp);
            Debug.Assert(typeOp != null);
            Debug.Assert(op != null && op.Type == typeOp);

            if (ptOp == PredefinedType.PT_ULONG)
            {
                return BadOperatorTypesError(ExpressionKind.EK_NEG, op, null);
            }

            if (ptOp == PredefinedType.PT_UINT && op.Type.fundType() == FUNDTYPE.FT_U4)
            {
                ExprClass exprObj = GetExprFactory().MakeClass(GetReqPDT(PredefinedType.PT_LONG));
                op = mustConvertCore(op, exprObj, CONVERTTYPE.NOUDC);
            }

            Expr exprRes = GetExprFactory().CreateNeg(flags, op);
            Debug.Assert(0 == (exprRes.Flags & EXPRFLAG.EXF_LVALUE));
            return exprRes;
        }

        /*
          Bind an float/double operator: +, -, , /, %, <, >, <=, >=, ==, !=. If both operations are constants, the result
          will be a constant also. op2 can be null for a unary operator. The operands are assumed
          to be already converted to the correct type.
         */
        private Expr bindFloatOp(ExpressionKind kind, EXPRFLAG flags, Expr op1, Expr op2)
        {
            //Debug.Assert(kind.isRelational() || kind.isArithmetic());
            Debug.Assert(op2 == null || op1.Type == op2.Type);
            Debug.Assert(op1.Type.isPredefType(PredefinedType.PT_FLOAT) || op1.Type.isPredefType(PredefinedType.PT_DOUBLE));

            // Allocate the result expression.
            CType typeDest = kind.isRelational() ? GetReqPDT(PredefinedType.PT_BOOL) : op1.Type;

            Expr exprRes = GetExprFactory().CreateOperator(kind, typeDest, op1, op2);
            flags = ~EXPRFLAG.EXF_CHECKOVERFLOW;
            exprRes.Flags |= flags;

            return exprRes;
        }

        private Expr bindStringConcat(Expr op1, Expr op2)
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
            return GetExprFactory().CreateConcat(op1, op2);
        }

        /*
          Report an ambiguous operator types error.
         */
        private Expr ambiguousOperatorError(ExpressionKind ek, Expr op1, Expr op2)
        {
            RETAILVERIFY(op1 != null);

            // This is exactly the same "hack" that BadOperatorError uses. The first operand contains the
            // name of the operator in its errorString.
            string strOp = op1.ErrorString;

            // Bad arg types - report error to user.
            if (op2 != null)
            {
                GetErrorContext().Error(ErrorCode.ERR_AmbigBinaryOps, strOp, op1.Type, op2.Type);
            }
            else
            {
                GetErrorContext().Error(ErrorCode.ERR_AmbigUnaryOp, strOp, op1.Type);
            }

            Expr rval = GetExprFactory().CreateOperator(ek, null, op1, op2);
            rval.SetError();
            return rval;
        }

        private Expr BindUserBoolOp(ExpressionKind kind, ExprCall pCall)
        {
            RETAILVERIFY(pCall != null);
            RETAILVERIFY(pCall.MethWithInst.Meth() != null);
            RETAILVERIFY(pCall.OptionalArguments != null);
            Debug.Assert(kind == ExpressionKind.EK_LOGAND || kind == ExpressionKind.EK_LOGOR);

            CType typeRet = pCall.Type;

            Debug.Assert(pCall.MethWithInst.Meth().Params.Count == 2);
            if (!GetTypes().SubstEqualTypes(typeRet, pCall.MethWithInst.Meth().Params[0], typeRet) ||
                !GetTypes().SubstEqualTypes(typeRet, pCall.MethWithInst.Meth().Params[1], typeRet))
            {
                MethWithInst mwi = new MethWithInst(null, null);
                ExprMemberGroup pMemGroup = GetExprFactory().CreateMemGroup(null, mwi);
                ExprCall pCallTF = GetExprFactory().CreateCall(0, null, null, pMemGroup, null);
                pCallTF.SetError();
                GetErrorContext().Error(ErrorCode.ERR_BadBoolOp, pCall.MethWithInst);
                return GetExprFactory().CreateUserLogOpError(typeRet, pCallTF, pCall);
            }

            ExprList list = (ExprList)pCall.OptionalArguments;
            Debug.Assert(list != null);

            Expr pExpr = list.OptionalElement;
            ExprWrap pExprWrap = WrapShortLivedExpression(pExpr);
            list.OptionalElement = pExprWrap;

            // Reflection load the true and false methods.
            SymbolLoader.RuntimeBinderSymbolTable.PopulateSymbolTableWithName(SpecialNames.CLR_True, null, pExprWrap.Type.AssociatedSystemType);
            SymbolLoader.RuntimeBinderSymbolTable.PopulateSymbolTableWithName(SpecialNames.CLR_False, null, pExprWrap.Type.AssociatedSystemType);

            Expr pCallT = bindUDUnop(ExpressionKind.EK_TRUE, pExprWrap);
            Expr pCallF = bindUDUnop(ExpressionKind.EK_FALSE, pExprWrap);

            if (pCallT == null || pCallF == null)
            {
                Expr pCallTorF = pCallT != null ? pCallT : pCallF;
                if (pCallTorF == null)
                {
                    MethWithInst mwi = new MethWithInst(null, null);
                    ExprMemberGroup pMemGroup = GetExprFactory().CreateMemGroup(null, mwi);
                    pCallTorF = GetExprFactory().CreateCall(0, null, pExprWrap, pMemGroup, null);
                    pCall.SetError();
                }
                GetErrorContext().Error(ErrorCode.ERR_MustHaveOpTF, typeRet);
                return GetExprFactory().CreateUserLogOpError(typeRet, pCallTorF, pCall);
            }
            pCallT = mustConvert(pCallT, GetReqPDT(PredefinedType.PT_BOOL));
            pCallF = mustConvert(pCallF, GetReqPDT(PredefinedType.PT_BOOL));
            return GetExprFactory().CreateUserLogOp(typeRet, kind == ExpressionKind.EK_LOGAND ? pCallF : pCallT, pCall);
        }

        private AggregateType GetUserDefinedBinopArgumentType(CType type)
        {
            for (; ;)
            {
                switch (type.GetTypeKind())
                {
                    case TypeKind.TK_NullableType:
                        type = type.StripNubs();
                        break;
                    case TypeKind.TK_TypeParameterType:
                        type = type.AsTypeParameterType().GetEffectiveBaseClass();
                        break;
                    case TypeKind.TK_AggregateType:
                        if ((type.isClassType() || type.isStructType()) && !type.AsAggregateType().getAggregate().IsSkipUDOps())
                        {
                            return type.AsAggregateType();
                        }
                        return null;
                    default:
                        return null;
                }
            }
        }

        private int GetUserDefinedBinopArgumentTypes(CType type1, CType type2, AggregateType[] rgats)
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

        private bool UserDefinedBinaryOperatorCanBeLifted(ExpressionKind ek, MethodSymbol method, AggregateType ats,
            TypeArray Params)
        {
            if (!Params[0].IsNonNubValType())
            {
                return false;
            }
            if (!Params[1].IsNonNubValType())
            {
                return false;
            }
            CType typeRet = GetTypes().SubstType(method.RetType, ats);
            if (!typeRet.IsNonNubValType())
            {
                return false;
            }
            switch (ek)
            {
                case ExpressionKind.EK_EQ:
                case ExpressionKind.EK_NE:
                    if (!typeRet.isPredefType(PredefinedType.PT_BOOL))
                    {
                        return false;
                    }
                    if (Params[0] != Params[1])
                    {
                        return false;
                    }
                    return true;
                case ExpressionKind.EK_GT:
                case ExpressionKind.EK_GE:
                case ExpressionKind.EK_LT:
                case ExpressionKind.EK_LE:
                    if (!typeRet.isPredefType(PredefinedType.PT_BOOL))
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
        private bool UserDefinedBinaryOperatorIsApplicable(List<CandidateFunctionMember> candidateList,
            ExpressionKind ek, MethodSymbol method, AggregateType ats, Expr arg1, Expr arg2, bool fDontLift)
        {
            if (!method.isOperator || method.Params.Count != 2)
            {
                return false;
            }
            Debug.Assert(method.typeVars.Count == 0);
            TypeArray paramsCur = GetTypes().SubstTypeArray(method.Params, ats);
            if (canConvert(arg1, paramsCur[0]) && canConvert(arg2, paramsCur[1]))
            {
                candidateList.Add(new CandidateFunctionMember(
                    new MethPropWithInst(method, ats, BSYMMGR.EmptyTypeArray()),
                    paramsCur,
                    0, // No lifted arguments
                    false));
                return true;
            }
            if (fDontLift || !GetSymbolLoader().FCanLift() ||
                !UserDefinedBinaryOperatorCanBeLifted(ek, method, ats, paramsCur))
            {
                return false;
            }
            CType[] rgtype = new CType[2];
            rgtype[0] = GetTypes().GetNullable(paramsCur[0]);
            rgtype[1] = GetTypes().GetNullable(paramsCur[1]);
            if (!canConvert(arg1, rgtype[0]) || !canConvert(arg2, rgtype[1]))
            {
                return false;
            }
            candidateList.Add(new CandidateFunctionMember(
                new MethPropWithInst(method, ats, BSYMMGR.EmptyTypeArray()),
                GetGlobalSymbols().AllocParams(2, rgtype),
                2, // two lifted arguments
                false));
            return true;
        }

        private bool GetApplicableUserDefinedBinaryOperatorCandidates(
            List<CandidateFunctionMember> candidateList, ExpressionKind ek, AggregateType type,
            Expr arg1, Expr arg2, bool fDontLift)
        {
            Name name = ekName(ek);
            Debug.Assert(name != null);
            bool foundSome = false;
            for (MethodSymbol methCur = GetSymbolLoader().LookupAggMember(name, type.getAggregate(), symbmask_t.MASK_MethodSymbol).AsMethodSymbol();
                methCur != null;
                methCur = GetSymbolLoader().LookupNextSym(methCur, type.getAggregate(), symbmask_t.MASK_MethodSymbol).AsMethodSymbol())
            {
                if (UserDefinedBinaryOperatorIsApplicable(candidateList, ek, methCur, type, arg1, arg2, fDontLift))
                {
                    foundSome = true;
                }
            }
            return foundSome;
        }

        private AggregateType GetApplicableUserDefinedBinaryOperatorCandidatesInBaseTypes(
            List<CandidateFunctionMember> candidateList, ExpressionKind ek, AggregateType type,
            Expr arg1, Expr arg2, bool fDontLift, AggregateType atsStop)
        {
            for (AggregateType atsCur = type; atsCur != null && atsCur != atsStop; atsCur = atsCur.GetBaseClass())
            {
                if (GetApplicableUserDefinedBinaryOperatorCandidates(candidateList, ek, atsCur, arg1, arg2, fDontLift))
                {
                    return atsCur;
                }
            }
            return null;
        }

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

            ExprList args = GetExprFactory().CreateList(arg1, arg2);
            ArgInfos info = new ArgInfos();
            info.carg = 2;
            FillInArgInfoFromArgList(info, args);
            CandidateFunctionMember pmethAmbig1;
            CandidateFunctionMember pmethAmbig2;
            CandidateFunctionMember pmethBest = FindBestMethod(methFirst, null, info, out pmethAmbig1, out pmethAmbig2);

            if (pmethBest == null)
            {
                // No winner, so its an ambiguous call...
                GetErrorContext().Error(ErrorCode.ERR_AmbigCall, pmethAmbig1.mpwi, pmethAmbig2.mpwi);

                ExprMemberGroup pMemGroup = GetExprFactory().CreateMemGroup(null, pmethAmbig1.mpwi);
                ExprCall rval = GetExprFactory().CreateCall(0, null, GetExprFactory().CreateList(arg1, arg2), pMemGroup, null);
                rval.SetError();
                return rval;
            }

            if (GetSemanticChecker().CheckBogus(pmethBest.mpwi.Meth()))
            {
                GetErrorContext().ErrorRef(ErrorCode.ERR_BindToBogus, pmethBest.mpwi);

                ExprMemberGroup pMemGroup = GetExprFactory().CreateMemGroup(null, pmethBest.mpwi);
                ExprCall rval = GetExprFactory().CreateCall(0, null, GetExprFactory().CreateList(arg1, arg2), pMemGroup, null);
                rval.SetError();
                return rval;
            }

            ppmpwi = pmethBest.mpwi;

            if (pmethBest.ctypeLift != 0)
            {
                Debug.Assert(pmethBest.ctypeLift == 2);

                return BindLiftedUDBinop(ek, arg1, arg2, pmethBest.@params, pmethBest.mpwi);
            }

            CType typeRetRaw = GetTypes().SubstType(pmethBest.mpwi.Meth().RetType, pmethBest.mpwi.GetType());

            return BindUDBinopCall(arg1, arg2, pmethBest.@params, typeRetRaw, pmethBest.mpwi);
        }

        private ExprCall BindUDBinopCall(Expr arg1, Expr arg2, TypeArray Params, CType typeRet, MethPropWithInst mpwi)
        {
            arg1 = mustConvert(arg1, Params[0]);
            arg2 = mustConvert(arg2, Params[1]);
            ExprList args = GetExprFactory().CreateList(arg1, arg2);

            checkUnsafe(arg1.Type); // added to the binder so we don't bind to pointer ops
            checkUnsafe(arg2.Type); // added to the binder so we don't bind to pointer ops
            checkUnsafe(typeRet); // added to the binder so we don't bind to pointer ops


            ExprMemberGroup pMemGroup = GetExprFactory().CreateMemGroup(null, mpwi);
            ExprCall call = GetExprFactory().CreateCall(0, typeRet, args, pMemGroup, null);
            call.MethWithInst = new MethWithInst(mpwi);
            verifyMethodArgs(call, mpwi.GetType());
            return call;
        }

        private ExprCall BindLiftedUDBinop(ExpressionKind ek, Expr arg1, Expr arg2, TypeArray Params, MethPropWithInst mpwi)
        {
            Expr exprVal1 = arg1;
            Expr exprVal2 = arg2;
            CType typeRet;
            CType typeRetRaw = GetTypes().SubstType(mpwi.Meth().RetType, mpwi.GetType());

            // This is a lifted user defined operator.  We know that both arguments
            // go to the nullable formal parameter types, and that at least one
            // of the arguments does not go to the non-nullable formal parameter type.
            // (If both went to the non-nullable types then we would not be lifting.)
            // We also know that the non-nullable type of the argument goes to the
            // non-nullable type of formal parameter.  However, if it does so only via
            // a user-defined conversion then we should bind the conversion from the
            // argument to the nullable formal parameter type first, before we then
            // do the cast for the non-nullable call.

            TypeArray paramsRaw = GetTypes().SubstTypeArray(mpwi.Meth().Params, mpwi.GetType());
            Debug.Assert(Params != paramsRaw);
            Debug.Assert(paramsRaw[0] == Params[0].GetBaseOrParameterOrElementType());
            Debug.Assert(paramsRaw[1] == Params[1].GetBaseOrParameterOrElementType());

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
                    typeRet = GetTypes().GetNullable(typeRetRaw);
                    break;
                case ExpressionKind.EK_EQ:
                case ExpressionKind.EK_NE:
                    Debug.Assert(paramsRaw[0] == paramsRaw[1]);
                    Debug.Assert(typeRetRaw.isPredefType(PredefinedType.PT_BOOL));
                    // These ones don't lift the return type. Instead, if either side is null, the result is false.
                    typeRet = typeRetRaw;
                    break;
                case ExpressionKind.EK_GT:
                case ExpressionKind.EK_GE:
                case ExpressionKind.EK_LT:
                case ExpressionKind.EK_LE:
                    Debug.Assert(typeRetRaw.isPredefType(PredefinedType.PT_BOOL));
                    // These ones don't lift the return type. Instead, if either side is null, the result is false.
                    typeRet = typeRetRaw;
                    break;
            }

            // Now get the result for the pre-lifted call.

            Debug.Assert(!(ek == ExpressionKind.EK_EQ || ek == ExpressionKind.EK_NE) || nonLiftedArg1.Type == nonLiftedArg2.Type);

            ExprCall nonLiftedResult = BindUDBinopCall(nonLiftedArg1, nonLiftedArg2, paramsRaw, typeRetRaw, mpwi);

            ExprList args = GetExprFactory().CreateList(exprVal1, exprVal2);
            ExprMemberGroup pMemGroup = GetExprFactory().CreateMemGroup(null, mpwi);
            ExprCall call = GetExprFactory().CreateCall(0, typeRet, args, pMemGroup, null);
            call.MethWithInst = new MethWithInst(mpwi);

            switch (ek)
            {
                case ExpressionKind.EK_EQ:
                    call.NullableCallLiftKind = NullableCallLiftKind.EqualityOperator;
                    break;

                case ExpressionKind.EK_NE:
                    call.NullableCallLiftKind = NullableCallLiftKind.InequalityOperator;
                    break;

                default:
                    call.NullableCallLiftKind = NullableCallLiftKind.Operator;
                    break;
            }

            call.CastOfNonLiftedResultToLiftedType = mustCast(nonLiftedResult, typeRet, 0);
            return call;
        }

        private AggregateType GetEnumBinOpType(ExpressionKind ek, CType argType1, CType argType2, out AggregateType ppEnumType)
        {
            Debug.Assert(argType1.isEnumType() || argType2.isEnumType());

            AggregateType type1 = argType1.AsAggregateType();
            AggregateType type2 = argType2.AsAggregateType();

            AggregateType typeEnum = type1.isEnumType() ? type1 : type2;

            Debug.Assert(type1 == typeEnum || type1 == typeEnum.underlyingEnumType());
            Debug.Assert(type2 == typeEnum || type2 == typeEnum.underlyingEnumType());

            AggregateType typeDst = typeEnum;

            switch (ek)
            {
                case ExpressionKind.EK_BITAND:
                case ExpressionKind.EK_BITOR:
                case ExpressionKind.EK_BITXOR:
                    Debug.Assert(type1 == type2);
                    break;

                case ExpressionKind.EK_ADD:
                    Debug.Assert(type1 != type2);
                    break;

                case ExpressionKind.EK_SUB:
                    if (type1 == type2)
                        typeDst = typeEnum.underlyingEnumType();
                    break;

                default:
                    Debug.Assert(ek.isRelational());
                    typeDst = GetReqPDT(PredefinedType.PT_BOOL);
                    break;
            }

            ppEnumType = typeEnum;
            return typeDst;
        }

        private ExprBinOp CreateBinopForPredefMethodCall(ExpressionKind ek, PREDEFMETH predefMeth, CType RetType, Expr arg1, Expr arg2)
        {
            MethodSymbol methSym = GetSymbolLoader().getPredefinedMembers().GetMethod(predefMeth);
            ExprBinOp binop = GetExprFactory().CreateBinop(ek, RetType, arg1, arg2);

            // Set the predefined method to call.
            if (methSym != null)
            {
                AggregateSymbol agg = methSym.getClass();
                AggregateType callingType = GetTypes().GetAggregate(agg, BSYMMGR.EmptyTypeArray());
                binop.PredefinedMethodToCall = new MethWithInst(methSym, callingType, null);
                binop.UserDefinedCallMethod = binop.PredefinedMethodToCall;
            }
            else
            {
                // Couldn't find it.
                binop.SetError();
            }
            return binop;
        }

        private ExprUnaryOp CreateUnaryOpForPredefMethodCall(ExpressionKind ek, PREDEFMETH predefMeth, CType pRetType, Expr pArg)
        {
            MethodSymbol methSym = GetSymbolLoader().getPredefinedMembers().GetMethod(predefMeth);
            ExprUnaryOp pUnaryOp = GetExprFactory().CreateUnaryOp(ek, pRetType, pArg);

            // Set the predefined method to call.
            if (methSym != null)
            {
                AggregateSymbol pAgg = methSym.getClass();
                AggregateType pCallingType = GetTypes().GetAggregate(pAgg, BSYMMGR.EmptyTypeArray());
                pUnaryOp.PredefinedMethodToCall = new MethWithInst(methSym, pCallingType, null);
                pUnaryOp.UserDefinedCallMethod = pUnaryOp.PredefinedMethodToCall;
            }
            else
            {
                pUnaryOp.SetError();
            }
            return pUnaryOp;
        }
    }
}

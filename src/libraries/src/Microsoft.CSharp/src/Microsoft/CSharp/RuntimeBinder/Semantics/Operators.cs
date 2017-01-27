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

        private EXPR bindUserDefinedBinOp(ExpressionKind ek, BinOpArgInfo info)
        {
            MethPropWithInst pmpwi = null;
            if (info.pt1 <= PredefinedType.PT_ULONG && info.pt2 <= PredefinedType.PT_ULONG)
            {
                return null;
            }

            EXPR expr = null;

            switch (info.binopKind)
            {
                case BinOpKind.Logical:
                    {
                        // Logical operators cannot be overloaded, but use the bitwise overloads.
                        EXPRCALL call = BindUDBinop((ExpressionKind)(ek - ExpressionKind.EK_LOGAND + ExpressionKind.EK_BITAND), info.arg1, info.arg2, true, out pmpwi);
                        if (call != null)
                        {
                            if (call.isOK())
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

            return GetExprFactory().CreateUserDefinedBinop(ek, expr.type, info.arg1, info.arg2, expr, pmpwi);
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

        private EXPRBINOP bindNullEqualityComparison(ExpressionKind ek, BinOpArgInfo info)
        {
            EXPR arg1 = info.arg1;
            EXPR arg2 = info.arg2;
            if (info.binopKind == BinOpKind.Equal)
            {
                CType typeBool = GetReqPDT(PredefinedType.PT_BOOL);
                EXPRBINOP exprRes = null;
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
                    exprRes.isLifted = true;
                    return exprRes;
                }
            }
            EXPR pExpr = BadOperatorTypesError(ek, info.arg1, info.arg2, GetTypes().GetErrorSym());
            Debug.Assert(pExpr.isBIN());
            return pExpr.asBIN();
        }

        /*
            This handles binding binary operators by first checking for user defined operators, then
            applying overload resolution to the predefined operators. It handles lifting over nullable.
        */
        public EXPR BindStandardBinop(ExpressionKind ek, EXPR arg1, EXPR arg2)
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
            EXPR exprUD = bindUserDefinedBinOp(ek, info);
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

        private EXPR BindStandardBinopCore(BinOpArgInfo info, BinOpFullSig bofs, ExpressionKind ek, EXPRFLAG flags)
        {
            if (bofs.pfn == null)
            {
                return BadOperatorTypesError(ek, info.arg1, info.arg2);
            }

            if (!bofs.isLifted() || !bofs.AutoLift())
            {
                EXPR expr1 = info.arg1;
                EXPR expr2 = info.arg2;
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
            return BindLiftedStandardBinOp(info, bofs, ek, flags);
        }
        private EXPR BindLiftedStandardBinOp(BinOpArgInfo info, BinOpFullSig bofs, ExpressionKind ek, EXPRFLAG flags)
        {
            Debug.Assert(bofs.Type1().IsNullableType() || bofs.Type2().IsNullableType());

            EXPR arg1 = info.arg1;
            EXPR arg2 = info.arg2;

            // We want to get the base types of the arguments and attempt to bind the non-lifted form of the
            // method so that we error report (ie divide by zero etc), and then we store in the resulting
            // binop that we have a lifted operator.

            EXPR pArgument1 = null;
            EXPR pArgument2 = null;
            EXPR nonLiftedArg1 = null;
            EXPR nonLiftedArg2 = null;
            EXPR nonLiftedResult = null;
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
                    resultType = GetEnumBinOpType(ek, nonLiftedArg1.type, nonLiftedArg2.type, out enumType);
                }
                else
                {
                    resultType = pArgument1.type;
                }
                resultType = resultType.IsNullableType() ? resultType : GetSymbolLoader().GetTypeManager().GetNullable(resultType);
            }

            EXPRBINOP exprRes = GetExprFactory().CreateBinop(ek, resultType, pArgument1, pArgument2);
            mustCast(nonLiftedResult, resultType, 0);
            exprRes.isLifted = true;
            exprRes.flags |= flags;
            Debug.Assert((exprRes.flags & EXPRFLAG.EXF_LVALUE) == 0);

            return exprRes;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private void LiftArgument(EXPR pArgument, CType pParameterType, bool bConvertBeforeLift,
                                            out EXPR ppLiftedArgument, out EXPR ppNonLiftedArgument)
        {
            EXPR pLiftedArgument = mustConvert(pArgument, pParameterType);
            if (pLiftedArgument != pArgument)
            {
                MarkAsIntermediateConversion(pLiftedArgument);
            }

            EXPR pNonLiftedArgument = pArgument;
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

        public EXPR BindStandardUnaryOperator(OperatorKind op, EXPR pArgument)
        {
            RETAILVERIFY(pArgument != null);

            ExpressionKind ek;
            UnaOpKind unaryOpKind;
            EXPRFLAG flags;

            if (pArgument.type == null ||
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
            CType type = pArgument.type;

            List<UnaOpFullSig> pSignatures = new List<UnaOpFullSig>();

            EXPR pResult = null;
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
            EXPR arg = tryConvert(pArgument, uofs.GetType());
            if (arg == null)
            {
                arg = mustCast(pArgument, uofs.GetType(), CONVERTTYPE.NOUDC);
            }
            return uofs.pfn(ek, flags, arg);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private UnaryOperatorSignatureFindResult PopulateSignatureList(EXPR pArgument, UnaOpKind unaryOpKind, UnaOpMask unaryOpMask, ExpressionKind exprKind, EXPRFLAG flags, List<UnaOpFullSig> pSignatures, out EXPR ppResult)
        {
            // We should have already checked argument != null and argument.type != null.
            Debug.Assert(pArgument != null);
            Debug.Assert(pArgument.type != null);

            ppResult = null;
            CType pArgumentType = pArgument.type;
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
                    EXPRMULTIGET exprGet = GetExprFactory().CreateMultiGet(0, pArgumentType, null);
#else // CSEE

                    EXPR exprGet = pArgument;
#endif // CSEE

                    EXPR exprVal = bindUDUnop((ExpressionKind)(exprKind - ExpressionKind.EK_ADD + ExpressionKind.EK_INC), exprGet);
                    if (exprVal != null)
                    {
                        if (exprVal.type != null && !exprVal.type.IsErrorType() && exprVal.type != pArgumentType)
                        {
                            exprVal = mustConvert(exprVal, pArgumentType);
                        }

                        Debug.Assert(pArgument != null);
                        EXPRMULTI exprMulti = GetExprFactory().CreateMulti(EXPRFLAG.EXF_ASSGOP | flags, pArgumentType, pArgument, exprVal);
#if ! CSEE
                        exprGet.SetOptionalMulti(exprMulti);
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
                    EXPR expr = bindUDUnop(exprKind, pArgument);
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
                EXPR pArgument,
                UnaOpMask unaryOpMask,
                List<UnaOpFullSig> pSignatures)
        {
            // All callers should already assert this to be the case.
            Debug.Assert(pArgument != null);
            Debug.Assert(pArgument.type != null);

            long iuosMinLift = GetSymbolLoader().FCanLift() ? 0 : g_rguos.Length;

            CType pArgumentType = pArgument.type;
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

        private EXPR BindLiftedStandardUnop(ExpressionKind ek, EXPRFLAG flags, EXPR arg, UnaOpFullSig uofs)
        {
            NullableType type = uofs.GetType().AsNullableType();
            Debug.Assert(arg != null && arg.type != null);
            if (arg.type.IsNullType())
            {
                return BadOperatorTypesError(ek, arg, null, type);
            }

            EXPR pArgument = null;
            EXPR nonLiftedArg = null;

            LiftArgument(arg, uofs.GetType(), uofs.Convert(), out pArgument, out nonLiftedArg);

            // Now call the function with the non lifted arguments to report errors.
            EXPR nonLiftedResult = uofs.pfn(ek, flags, nonLiftedArg);
            EXPRUNARYOP exprRes = GetExprFactory().CreateUnaryOp(ek, type, pArgument);
            mustCast(nonLiftedResult, type, 0);
            exprRes.flags |= flags;

            Debug.Assert((exprRes.flags & EXPRFLAG.EXF_LVALUE) == 0);
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
        private EXPR BindIntBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
        {
            Debug.Assert(arg1.type.isPredefined() && arg2.type.isPredefined() && arg1.type.getPredefType() == arg2.type.getPredefType());
            return BindIntOp(ek, flags, arg1, arg2, arg1.type.getPredefType());
        }


        /*
            Handles standard unary integer based operators.
        */
        private EXPR BindIntUnaOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg)
        {
            Debug.Assert(arg.type.isPredefined());
            return BindIntOp(ek, flags, arg, null, arg.type.getPredefType());
        }


        /*
            Handles standard binary floating point (float, double) based operators.
        */
        private EXPR BindRealBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
        {
            Debug.Assert(arg1.type.isPredefined() && arg2.type.isPredefined() && arg1.type.getPredefType() == arg2.type.getPredefType());
            return bindFloatOp(ek, flags, arg1, arg2);
        }


        /*
            Handles standard unary floating point (float, double) based operators.
        */
        private EXPR BindRealUnaOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg)
        {
            Debug.Assert(arg.type.isPredefined());
            return bindFloatOp(ek, flags, arg, null);
        }


        /*
            Handles standard increment and decrement operators.
        */
        private EXPR BindIncOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg, UnaOpFullSig uofs)
        {
            Debug.Assert(ek == ExpressionKind.EK_ADD || ek == ExpressionKind.EK_SUB);
            if (!checkLvalue(arg, CheckLvalueKind.Increment))
            {
                EXPR rval = GetExprFactory().CreateBinop(ek, arg.type, arg, null);
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

        private EXPR BindIncOpCore(ExpressionKind ek, EXPRFLAG flags, EXPR exprVal, CType type)
        {
            Debug.Assert(ek == ExpressionKind.EK_ADD || ek == ExpressionKind.EK_SUB);
            CONSTVAL cv = new CONSTVAL();
            EXPR pExprResult = null;

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
                    cv.iVal = 1;
                    pExprResult = BindPtrBinOp(ek, flags, exprVal, GetExprFactory().CreateConstant(GetReqPDT(PredefinedType.PT_INT), cv));
                    break;
                case FUNDTYPE.FT_I1:
                case FUNDTYPE.FT_I2:
                case FUNDTYPE.FT_U1:
                case FUNDTYPE.FT_U2:
                    typeTmp = GetReqPDT(PredefinedType.PT_INT);
                    cv.iVal = 1;
                    pExprResult = LScalar(ek, flags, exprVal, type, cv, pExprResult, typeTmp);
                    break;
                case FUNDTYPE.FT_I4:
                case FUNDTYPE.FT_U4:
                    cv.iVal = 1;
                    pExprResult = LScalar(ek, flags, exprVal, type, cv, pExprResult, typeTmp);
                    break;
                case FUNDTYPE.FT_I8:
                case FUNDTYPE.FT_U8:
                    cv = GetExprConstants().Create((long)1);
                    pExprResult = LScalar(ek, flags, exprVal, type, cv, pExprResult, typeTmp);
                    break;
                case FUNDTYPE.FT_R4:
                case FUNDTYPE.FT_R8:
                    cv = GetExprConstants().Create(1.0);
                    pExprResult = LScalar(ek, flags, exprVal, type, cv, pExprResult, typeTmp);
                    break;
            }
            Debug.Assert(pExprResult != null);
            Debug.Assert(!pExprResult.type.IsNullableType());
            return pExprResult;
        }

        private EXPR LScalar(ExpressionKind ek, EXPRFLAG flags, EXPR exprVal, CType type, CONSTVAL cv, EXPR pExprResult, CType typeTmp)
        {
            CType typeOne = type;
            if (typeOne.isEnumType())
            {
                typeOne = typeOne.underlyingEnumType();
            }
            pExprResult = GetExprFactory().CreateBinop(ek, typeTmp, exprVal, GetExprFactory().CreateConstant(typeOne, cv));
            pExprResult.flags |= flags;
            if (typeTmp != type)
            {
                pExprResult = mustCast(pExprResult, type, CONVERTTYPE.NOUDC);
            }
            return pExprResult;
        }

        private EXPRMULTI BindNonliftedIncOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg, UnaOpFullSig uofs)
        {
            Debug.Assert(ek == ExpressionKind.EK_ADD || ek == ExpressionKind.EK_SUB);
            Debug.Assert(!uofs.isLifted());

            Debug.Assert(arg != null);
#if ! CSEE
            EXPRMULTIGET exprGet = GetExprFactory().CreateMultiGet(EXPRFLAG.EXF_ASSGOP, arg.type, null);
            EXPR exprVal = exprGet;
#else
            EXPR exprVal = arg;
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
            EXPR op = mustCast(exprVal, arg.type, CONVERTTYPE.NOUDC);

            EXPRMULTI exprMulti = GetExprFactory().CreateMulti(EXPRFLAG.EXF_ASSGOP | flags, arg.type, arg, op);

#if ! CSEE
            exprGet.SetOptionalMulti(exprMulti);
#endif
            return exprMulti;
        }

        private EXPRMULTI BindLiftedIncOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg, UnaOpFullSig uofs)
        {
            Debug.Assert(ek == ExpressionKind.EK_ADD || ek == ExpressionKind.EK_SUB);
            Debug.Assert(uofs.isLifted());

            NullableType type = uofs.GetType().AsNullableType();
            Debug.Assert(arg != null);

#if ! CSEE
            EXPRMULTIGET exprGet = GetExprFactory().CreateMultiGet(EXPRFLAG.EXF_ASSGOP, arg.type, null);
            EXPR exprVal = exprGet;
#else
            EXPR exprVal = arg;
#endif

            EXPR nonLiftedResult = null;
            EXPR nonLiftedArg = exprVal;

            // We want to give the lifted argument as the binop, but use the non-lifted argument as the 
            // argument of the call.
            //Debug.Assert(uofs.LiftArg() || type.IsValType());
            nonLiftedArg = mustCast(nonLiftedArg, type.GetUnderlyingType());
            nonLiftedResult = BindIncOpCore(ek, flags, nonLiftedArg, type.GetUnderlyingType());
            exprVal = mustCast(exprVal, type);
            EXPRUNARYOP exprRes = GetExprFactory().CreateUnaryOp((ek == ExpressionKind.EK_ADD) ? ExpressionKind.EK_INC : ExpressionKind.EK_DEC, arg.type/* type */, exprVal);
            mustCast(mustCast(nonLiftedResult, type), arg.type);
            exprRes.flags |= flags;

            EXPRMULTI exprMulti = GetExprFactory().CreateMulti(EXPRFLAG.EXF_ASSGOP | flags, arg.type, arg, exprRes);

#if ! CSEE
            exprGet.SetOptionalMulti(exprMulti);
#endif
            return exprMulti;
        }

        /*
            Handles standard binary decimal based operators.
            This function is called twice by the EE for every binary operator it evaluates
            Here is how it works.
        1.  The EE on finding an Expr asks the Expression binder to bind it. 
        2.  At this time the expression binder just creates a new binopexpr and returns it to the EE,
        the EE then uses the runtimesystem to find if any of the arguments of the expr can be evaluated to constants.
        3.  If so it creates new arguments and expr, aliases the original expr to the new one and passes
        it new expr to Expressionbinder to be bound. 
        4.  This time the expression binder realizes that the 2 arguments are constants and tries to fold them.
        If the folding is successful the value is used by the EE (and we have avoided a funceval)
        5.  if the constant binding fails, then the Expression binders returns the same exp as it would have 
        created for the compile case ( we func eval the same function as what would be executed at runtime).
        */
        private EXPR BindDecBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
        {
            Debug.Assert(arg1.type.isPredefType(PredefinedType.PT_DECIMAL) && arg2.type.isPredefType(PredefinedType.PT_DECIMAL));

            CType typeDec = GetOptPDT(PredefinedType.PT_DECIMAL);
            Debug.Assert(typeDec != null);

            EXPR argConst1 = arg1.GetConst();
            EXPR argConst2 = arg2.GetConst();

            CType typeRet = null;

            switch (ek)
            {
                default:
                    VSFAIL("Bad kind");
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
            // In the EE, even if we don't have two constants, we want to emit an EXPRBINOP with the
            // right EK so that when we evalsync we can just do the work ourselves instead of
            // delegating to method calls.

            if (!argConst1 || !argConst2)
            {
                // We don't have 2 constants, so just emit an EXPRBINOP...
                return GetExprFactory().CreateBinop(tree, ek, typeRet, arg1, arg2);
            }
            else
            {
                goto LBothConst;
            }

        LUserDefined:

#endif // CSEE

#if !CSEE
            if (argConst2 != null && argConst1 != null)
            {
                goto LBothConst;
            }
#endif

            // At this point, for the compiler we don't want to optimize the binop just yet. Maintain the correct tree until
            // the arithmetic optimizer pass.
            return GetExprFactory().CreateBinop(ek, typeRet, arg1, arg2);

        LBothConst:
            decimal dec1;
            decimal dec2;
            decimal decRes = 0;
            bool fRes = false;
            bool fBool = false;

            dec1 = argConst1.asCONSTANT().getVal().decVal;
            dec2 = argConst2.asCONSTANT().getVal().decVal;

            // Do the operation.
            switch (ek)
            {
                case ExpressionKind.EK_ADD:
                    decRes = dec1 + dec2;
                    break;
                case ExpressionKind.EK_SUB:
                    decRes = dec1 - dec2;
                    break;
                case ExpressionKind.EK_MUL:
                    decRes = dec1 * dec2;
                    break;
                case ExpressionKind.EK_DIV:
                    if (dec2 == 0)
                    {
                        GetErrorContext().Error(ErrorCode.ERR_IntDivByZero);
                        EXPR rval = GetExprFactory().CreateBinop(ek, typeDec, arg1, arg2);
                        rval.SetError();
                        return rval;
                    }

                    decRes = dec1 / dec2;
                    break;

                case ExpressionKind.EK_MOD:
                    {
                        /* n % d = n - d  truncate(n/d) */
                        decimal decDiv;

                        if (dec2 == 0)
                        {
                            GetErrorContext().Error(ErrorCode.ERR_IntDivByZero);
                            EXPR rval = GetExprFactory().CreateBinop(ek, typeDec, arg1, arg2);
                            rval.SetError();
                            return rval;
                        }

                        decDiv = dec1 % dec2;
                        break;
                    }

                default:
                    fBool = true;

                    switch (ek)
                    {
                        default:
                            VSFAIL("Bad ek");
                            break;
                        case ExpressionKind.EK_EQ:
                            fRes = dec1 == dec2;
                            break;
                        case ExpressionKind.EK_NE:
                            fRes = dec1 != dec2;
                            break;
                        case ExpressionKind.EK_LE:
                            fRes = dec1 <= dec2;
                            break;
                        case ExpressionKind.EK_LT:
                            fRes = dec1 < dec2;
                            break;
                        case ExpressionKind.EK_GE:
                            fRes = dec1 >= dec2;
                            break;
                        case ExpressionKind.EK_GT:
                            fRes = dec1 > dec2;
                            break;
                    }
                    break;
            }

            // Allocate the result node.
            CONSTVAL cv;
            EXPR exprRes;

            if (fBool)
            {
                cv = ConstValFactory.GetBool(fRes);
                exprRes = GetExprFactory().CreateConstant(GetReqPDT(PredefinedType.PT_BOOL), cv);
            }
            else
            {
                cv = GetExprConstants().Create(decRes);
                exprRes = GetExprFactory().CreateConstant(typeDec, cv);
            }

            return exprRes;
        }


        /*
            Handles standard unary decimal based operators.
        */
        private EXPR BindDecUnaOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg)
        {
            Debug.Assert(arg.type.isPredefType(PredefinedType.PT_DECIMAL));
            Debug.Assert(ek == ExpressionKind.EK_NEG || ek == ExpressionKind.EK_UPLUS);

            CType typeDec = GetOptPDT(PredefinedType.PT_DECIMAL);
            Debug.Assert(typeDec != null);
            ek = ek == ExpressionKind.EK_NEG ? ExpressionKind.EK_DECIMALNEG : ExpressionKind.EK_UPLUS;

            // We want to fold if the argument is constant. Otherwise, keep the regular op.
            EXPR argConst = arg.GetConst();
            if (argConst == null) // Non-constant.
            {
                if (ek == ExpressionKind.EK_DECIMALNEG)
                {
                    PREDEFMETH predefMeth = PREDEFMETH.PM_DECIMAL_OPUNARYMINUS;
                    return CreateUnaryOpForPredefMethodCall(ek, predefMeth, typeDec, arg);
                }
                return GetExprFactory().CreateUnaryOp(ek, typeDec, arg);
            }

            // If its a uplus, just return it.
            if (ek == ExpressionKind.EK_UPLUS)
            {
                return arg;
            }

            decimal dec = argConst.asCONSTANT().getVal().decVal;
            dec = dec * -1;

            // Allocate the result node.
            CONSTVAL cv = GetExprConstants().Create(dec);

            EXPR exprRes = GetExprFactory().CreateConstant(typeDec, cv);

            return exprRes;
        }


        /*
            Handles string concatenation.
        */
        private EXPR BindStrBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
        {
            Debug.Assert(ek == ExpressionKind.EK_ADD);
            Debug.Assert(arg1.type.isPredefType(PredefinedType.PT_STRING) || arg2.type.isPredefType(PredefinedType.PT_STRING));
            return bindStringConcat(arg1, arg2);
        }


        /*
            Bind a shift operator: <<, >>. These can have integer or long first operands,
            and second operand must be int.
        */
        private EXPR BindShiftOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
        {
            Debug.Assert(ek == ExpressionKind.EK_LSHIFT || ek == ExpressionKind.EK_RSHIFT);
            Debug.Assert(arg1.type.isPredefined());
            Debug.Assert(arg2.type.isPredefType(PredefinedType.PT_INT));

            PredefinedType ptOp = arg1.type.getPredefType();
            Debug.Assert(ptOp == PredefinedType.PT_INT || ptOp == PredefinedType.PT_UINT || ptOp == PredefinedType.PT_LONG || ptOp == PredefinedType.PT_ULONG);

            // We want to check up front if we have two constants, because constant folding is supposed to
            // happen in the initial binding pass.
            EXPR argConst1 = arg1.GetConst();
            EXPR argConst2 = arg2.GetConst();

            if (argConst1 == null || argConst2 == null) // One or more aren't constants, so don't fold anything.
            {
                return GetExprFactory().CreateBinop(ek, arg1.type, arg1, arg2);
            }

            // Both constants, so fold them.
            CONSTVAL cv = new CONSTVAL();
            int cbit = (ptOp == PredefinedType.PT_LONG || ptOp == PredefinedType.PT_ULONG) ? 0x3f : 0x1f;
            cv.iVal = argConst2.asCONSTANT().getVal().iVal & cbit;
            cbit = cv.iVal;

            // Fill in the CONSTVAL.
            if (ptOp == PredefinedType.PT_LONG || ptOp == PredefinedType.PT_ULONG)
            {
                Debug.Assert(0 <= cbit && cbit < 0x40);
                ulong u1 = argConst1.asCONSTANT().getVal().ulongVal;
                ulong uval;

                switch (ek)
                {
                    case ExpressionKind.EK_LSHIFT:
                        uval = u1 << cbit;
                        break;
                    case ExpressionKind.EK_RSHIFT:
                        uval = (ptOp == PredefinedType.PT_LONG) ? (ulong)((long)u1 >> cbit) : (u1 >> cbit);
                        break;
                    default:
                        VSFAIL("Unknown op");
                        uval = 0;
                        break;
                }
                cv = GetExprConstants().Create(uval);
            }
            else
            {
                Debug.Assert(0 <= cbit && cbit < 0x20);
                uint u1 = argConst1.asCONSTANT().getVal().uiVal;

                switch (ek)
                {
                    case ExpressionKind.EK_LSHIFT:
                        cv.uiVal = u1 << cbit;
                        break;
                    case ExpressionKind.EK_RSHIFT:
                        cv.uiVal = (ptOp == PredefinedType.PT_INT) ? (uint)((int)u1 >> cbit) : (u1 >> cbit);
                        break;
                    default:
                        VSFAIL("Unknown op");
                        cv.uiVal = 0;
                        break;
                }
            }

            EXPR exprRes = GetExprFactory().CreateConstant(GetReqPDT(ptOp), cv);
            return exprRes;
        }

        /*
            Bind a bool binary operator: ==, !=, &&, ||, , |, ^. If both operands are constant, the
            result will be a constant also.
        */
        private EXPR BindBoolBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
        {
            Debug.Assert(arg1 != null);
            Debug.Assert(arg2 != null);
            Debug.Assert(arg1.type.isPredefType(PredefinedType.PT_BOOL) || (arg1.type.IsNullableType() && arg2.type.AsNullableType().GetUnderlyingType().isPredefType(PredefinedType.PT_BOOL)));
            Debug.Assert(arg2.type.isPredefType(PredefinedType.PT_BOOL) || (arg2.type.IsNullableType() && arg2.type.AsNullableType().GetUnderlyingType().isPredefType(PredefinedType.PT_BOOL)));

            EXPR exprRes = GetExprFactory().CreateBinop(ek, GetReqPDT(PredefinedType.PT_BOOL), arg1, arg2);

            return exprRes;
        }

        private EXPR BindBoolBitwiseOp(ExpressionKind ek, EXPRFLAG flags, EXPR expr1, EXPR expr2, BinOpFullSig bofs)
        {
            Debug.Assert(ek == ExpressionKind.EK_BITAND || ek == ExpressionKind.EK_BITOR);
            Debug.Assert(expr1.type.isPredefType(PredefinedType.PT_BOOL) || expr1.type.IsNullableType() && expr1.type.AsNullableType().GetUnderlyingType().isPredefType(PredefinedType.PT_BOOL));
            Debug.Assert(expr2.type.isPredefType(PredefinedType.PT_BOOL) || expr2.type.IsNullableType() && expr2.type.AsNullableType().GetUnderlyingType().isPredefType(PredefinedType.PT_BOOL));

            if (expr1.type.IsNullableType() || expr2.type.IsNullableType())
            {
                CType typeBool = GetReqPDT(PredefinedType.PT_BOOL);
                CType typeRes = GetSymbolLoader().GetTypeManager().GetNullable(typeBool);

                // Get the non-lifted result.
                EXPR nonLiftedArg1 = CNullable.StripNullableConstructor(expr1);
                EXPR nonLiftedArg2 = CNullable.StripNullableConstructor(expr2);
                EXPR nonLiftedResult = null;

                if (!nonLiftedArg1.type.IsNullableType() && !nonLiftedArg2.type.IsNullableType())
                {
                    nonLiftedResult = BindBoolBinOp(ek, flags, nonLiftedArg1, nonLiftedArg2);
                }

                // Make the binop and set that its lifted.
                EXPRBINOP exprRes = GetExprFactory().CreateBinop(ek, typeRes, expr1, expr2);
                if (nonLiftedResult != null)
                {
                    // Bitwise operators can have null non-lifted results if we have a nub sym somewhere.
                    mustCast(nonLiftedResult, typeRes, 0);
                }
                exprRes.isLifted = true;
                exprRes.flags |= flags;
                Debug.Assert((exprRes.flags & EXPRFLAG.EXF_LVALUE) == 0);
                return exprRes;
            }
            return BindBoolBinOp(ek, flags, expr1, expr2);
        }

        private EXPR BindLiftedBoolBitwiseOp(ExpressionKind ek, EXPRFLAG flags, EXPR expr1, EXPR expr2)
        {
            return null;
        }


        /*
            Handles boolean unary operator (!).
        */
        private EXPR BindBoolUnaOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg)
        {
            Debug.Assert(arg.type.isPredefType(PredefinedType.PT_BOOL));
            Debug.Assert(ek == ExpressionKind.EK_LOGNOT);

            // Get the result type and operand type.
            CType typeBool = GetReqPDT(PredefinedType.PT_BOOL);

            // Determine if arg has a constant value.
            // Strip off EXPRKIND.EK_SEQUENCE for constant checking.

            EXPR argConst = arg.GetConst();

            if (argConst == null)
                return GetExprFactory().CreateUnaryOp(ExpressionKind.EK_LOGNOT, typeBool, arg);

            bool fRes = argConst.asCONSTANT().getVal().iVal != 0;
            EXPR rval = GetExprFactory().CreateConstant(typeBool, ConstValFactory.GetBool(!fRes));

            return rval;
        }


        /*
            Handles string equality.
        */
        private EXPR BindStrCmpOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
        {
            Debug.Assert(ek == ExpressionKind.EK_EQ || ek == ExpressionKind.EK_NE);
            Debug.Assert(arg1.type.isPredefType(PredefinedType.PT_STRING) && arg2.type.isPredefType(PredefinedType.PT_STRING));

            // Get the predefined method for string comparison, and then stash it in the EXPR so we can 
            // transform it later.

            PREDEFMETH predefMeth = ek == ExpressionKind.EK_EQ ? PREDEFMETH.PM_STRING_OPEQUALITY : PREDEFMETH.PM_STRING_OPINEQUALITY;
            ek = ek == ExpressionKind.EK_EQ ? ExpressionKind.EK_STRINGEQ : ExpressionKind.EK_STRINGNE;
            return CreateBinopForPredefMethodCall(ek, predefMeth, GetReqPDT(PredefinedType.PT_BOOL), arg1, arg2);
        }


        /*
            Handles reference equality operators. Type variables come through here.
        */
        private EXPR BindRefCmpOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
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
        private EXPR BindDelBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
        {
            Debug.Assert(ek == ExpressionKind.EK_ADD || ek == ExpressionKind.EK_SUB || ek == ExpressionKind.EK_EQ || ek == ExpressionKind.EK_NE);
            Debug.Assert(arg1.type == arg2.type && (arg1.type.isDelegateType() || arg1.type.isPredefType(PredefinedType.PT_DELEGATE)));

            PREDEFMETH predefMeth = (PREDEFMETH)0;
            CType RetType = null;
            switch (ek)
            {
                case ExpressionKind.EK_ADD:
                    predefMeth = PREDEFMETH.PM_DELEGATE_COMBINE;
                    RetType = arg1.type;
                    ek = ExpressionKind.EK_DELEGATEADD;
                    break;

                case ExpressionKind.EK_SUB:
                    predefMeth = PREDEFMETH.PM_DELEGATE_REMOVE;
                    RetType = arg1.type;
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
        private EXPR BindEnumBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
        {
            AggregateType typeEnum = null;
            AggregateType typeDst = GetEnumBinOpType(ek, arg1.type, arg2.type, out typeEnum);

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

            EXPR exprRes = BindIntOp(ek, flags, arg1, arg2, ptOp);

            if (!exprRes.isOK())
            {
                return exprRes;
            }

            if (exprRes.type != typeDst)
            {
                Debug.Assert(!typeDst.isPredefType(PredefinedType.PT_BOOL));
                exprRes = mustCast(exprRes, typeDst, CONVERTTYPE.NOUDC);
            }

            return exprRes;
        }


        /*
            Handles enum unary operator (~).
        */
        private EXPR BindEnumUnaOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg)
        {
            Debug.Assert(ek == ExpressionKind.EK_BITNOT);
            Debug.Assert(arg.isCAST());
            Debug.Assert(arg.asCAST().GetArgument().type.isEnumType());

            PredefinedType ptOp;
            CType typeEnum = arg.asCAST().GetArgument().type;

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

            EXPR exprRes = BindIntOp(ek, flags, arg, null, ptOp);

            if (!exprRes.isOK())
            {
                return exprRes;
            }

            return mustCastInUncheckedContext(exprRes, typeEnum, CONVERTTYPE.NOUDC);
        }


        /*
            Handles pointer binary operators (+ and -).
        */
        private EXPR BindPtrBinOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
        {
            return null;
        }


        /*
            Handles pointer comparison operators.
        */
        private EXPR BindPtrCmpOp(ExpressionKind ek, EXPRFLAG flags, EXPR arg1, EXPR arg2)
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

        private static bool isDivByZero(ExpressionKind kind, EXPR op2)
        {
            return false;
        }

        private EXPR FoldIntegerConstants(ExpressionKind kind, EXPRFLAG flags, EXPR op1, EXPR op2, PredefinedType ptOp)
        {
            //Debug.Assert(kind.isRelational() || kind.isArithmetic() || kind.isBitwise());
            Debug.Assert(ptOp == PredefinedType.PT_INT || ptOp == PredefinedType.PT_UINT || ptOp == PredefinedType.PT_LONG || ptOp == PredefinedType.PT_ULONG);
            CType typeOp = GetReqPDT(ptOp);
            Debug.Assert(typeOp != null);
            Debug.Assert(op1 != null && op1.type == typeOp);
            Debug.Assert(op2 == null || op2.type == typeOp);
            Debug.Assert((op2 == null) == (kind == ExpressionKind.EK_NEG || kind == ExpressionKind.EK_UPLUS || kind == ExpressionKind.EK_BITNOT));

            EXPRCONSTANT opConst1 = op1.GetConst().asCONSTANT();
            EXPRCONSTANT opConst2 = (op2 != null) ? op2.GetConst().asCONSTANT() : null;

            // Fold operation if both args are constant.
            if (opConst1 != null && (op2 == null || opConst2 != null))
            {
                if (ptOp == PredefinedType.PT_LONG || ptOp == PredefinedType.PT_ULONG)
                {
                    return FoldConstI8Op(kind, op1, opConst1, op2, opConst2, ptOp);
                }
                else
                {
                    return FoldConstI4Op(kind, op1, opConst1, op2, opConst2, ptOp);
                }
            }

            return null;
        }


        /*
            Convert and constant fold an expression involving I4, U4, I8 or U8 operands. The operands are
            assumed to be already converted to the correct types.
        */
        private EXPR BindIntOp(ExpressionKind kind, EXPRFLAG flags, EXPR op1, EXPR op2, PredefinedType ptOp)
        {
            //Debug.Assert(kind.isRelational() || kind.isArithmetic() || kind.isBitwise());
            Debug.Assert(ptOp == PredefinedType.PT_INT || ptOp == PredefinedType.PT_UINT || ptOp == PredefinedType.PT_LONG || ptOp == PredefinedType.PT_ULONG);
            CType typeOp = GetReqPDT(ptOp);
            Debug.Assert(typeOp != null);
            Debug.Assert(op1 != null && op1.type == typeOp);
            Debug.Assert(op2 == null || op2.type == typeOp);
            Debug.Assert((op2 == null) == (kind == ExpressionKind.EK_NEG || kind == ExpressionKind.EK_UPLUS || kind == ExpressionKind.EK_BITNOT));

            if (isDivByZero(kind, op2))
            {
                GetErrorContext().Error(ErrorCode.ERR_IntDivByZero);
                EXPR rval = GetExprFactory().CreateBinop(kind, typeOp, op1, op2);
                rval.SetError();
                return rval;
            }

            // This optimization CANNOT be moved to a later pass.  See comments in
            // FoldIntegerConstants.
            EXPR exprFolded = FoldIntegerConstants(kind, flags, op1, op2, ptOp);
            if (exprFolded != null)
            {
                return exprFolded;
            }

            if (kind == ExpressionKind.EK_NEG)
            {
                return BindIntegerNeg(flags, op1, ptOp);
            }

            CType typeDest = kind.isRelational() ? GetReqPDT(PredefinedType.PT_BOOL) : typeOp;

            EXPR exprRes = GetExprFactory().CreateOperator(kind, typeDest, op1, op2);
            exprRes.flags |= flags;
            Debug.Assert((exprRes.flags & EXPRFLAG.EXF_LVALUE) == 0);
            return exprRes;
        }

        private EXPR BindIntegerNeg(EXPRFLAG flags, EXPR op, PredefinedType ptOp)
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
            Debug.Assert(op != null && op.type == typeOp);

            if (ptOp == PredefinedType.PT_ULONG)
            {
                return BadOperatorTypesError(ExpressionKind.EK_NEG, op, null);
            }

            if (ptOp == PredefinedType.PT_UINT && op.type.fundType() == FUNDTYPE.FT_U4)
            {
                EXPRCLASS exprObj = GetExprFactory().MakeClass(GetReqPDT(PredefinedType.PT_LONG));
                op = mustConvertCore(op, exprObj, CONVERTTYPE.NOUDC);
            }

            EXPR exprRes = GetExprFactory().CreateNeg(flags, op);
            Debug.Assert(0 == (exprRes.flags & EXPRFLAG.EXF_LVALUE));
            return exprRes;
        }

        private EXPR FoldConstI4Op(ExpressionKind kind, EXPR op1, EXPRCONSTANT opConst1, EXPR op2, EXPRCONSTANT opConst2, PredefinedType ptOp)
        {
            Debug.Assert(ptOp == PredefinedType.PT_INT || ptOp == PredefinedType.PT_UINT);
            Debug.Assert(opConst1.isCONSTANT_OK());
            Debug.Assert(op1.type.isPredefType(ptOp) && op1.type == opConst1.type);
            Debug.Assert(op2 == null && opConst2 == null ||
                   op2 != null && opConst2 != null && opConst2.isCONSTANT_OK() && op1.type == op2.type && op1.type == opConst2.type);

            bool fSigned = (ptOp == PredefinedType.PT_INT);

            // Get the operands
            uint u1 = opConst1.asCONSTANT().getVal().uiVal;
            uint u2 = opConst2 != null ? opConst2.asCONSTANT().getVal().uiVal : 0;
            uint uRes;

            // The code below doesn't work if uint isn't 4 bytes!
            Debug.Assert(sizeof(uint) == 4);

            // The sign bit.
            uint uSign = 0x80000000;

            // Do the operation.
            switch (kind)
            {
                case ExpressionKind.EK_ADD:
                    uRes = u1 + u2;
                    // For signed, we want either sign(u1) != sign(u2) or sign(u1) == sign(uRes).
                    // For unsigned, the result should be at least as big as either operand (if it's bigger than
                    // one, it will be bigger than the other as well).
                    if (fSigned)
                    {
                        EnsureChecked(0 != (((u1 ^ u2) | (u1 ^ uRes ^ uSign)) & uSign));
                    }
                    else
                    {
                        EnsureChecked(uRes >= u1);
                    }
                    break;

                case ExpressionKind.EK_SUB:
                    uRes = u1 - u2;
                    // For signed, we want either sign(u1) == sign(u2) or sign(u1) == sign(uRes).
                    // For unsigned, the result should be no bigger than the first operand.
                    if (fSigned)
                    {
                        EnsureChecked(0 != (((u1 ^ u2 ^ uSign) | (u1 ^ uRes ^ uSign)) & uSign));
                    }
                    else
                    {
                        EnsureChecked(uRes <= u1);
                    }
                    break;

                case ExpressionKind.EK_MUL:
                    // Multiply mod 2^32 doesn't depend on signed vs unsigned.
                    uRes = u1 * u2;
                    // Note that divide depends on signed-ness.
                    // For signed, the first check detects 0x80000000 / 0xFFFFFFFF == 0x80000000.
                    // This test needs to come first to avoid an integer overflow exception - yes we get this
                    // in native code.
                    if (u1 == 0 || u2 == 0)
                    {
                        break;
                    }

                    if (fSigned)
                    {
                        EnsureChecked((u2 != uRes || u1 == 1) && (int)uRes / (int)u1 == (int)u2);
                    }
                    else
                    {
                        EnsureChecked(uRes / u1 == u2);
                    }
                    break;

                case ExpressionKind.EK_DIV:
                    Debug.Assert(u2 != 0); // Caller should have handled this.
                    if (!fSigned)
                    {
                        uRes = u1 / u2;
                    }
                    else if (u2 != 0)
                    {
                        uRes = (uint)((int)u1 / (int)u2);
                    }
                    else
                    {
                        uRes = (uint)-(int)u1;
                        EnsureChecked(u1 != uSign);
                    }
                    break;

                case ExpressionKind.EK_MOD:
                    Debug.Assert(u2 != 0); // Caller should have handled this.
                    if (!fSigned)
                    {
                        uRes = u1 % u2;
                    }
                    else if (u2 != 0)
                    {
                        uRes = (uint)((int)u1 % (int)u2);
                    }
                    else
                    {
                        uRes = 0;
                    }
                    break;

                case ExpressionKind.EK_NEG:
                    if (!fSigned)
                    {
                        // Special case: a unary minus promotes a uint to a long
                        CONSTVAL cv = GetExprConstants().Create(-(long)u1);
                        EXPRCONSTANT foldedConst = GetExprFactory().CreateConstant(GetReqPDT(PredefinedType.PT_LONG), cv);

                        return foldedConst;
                    }

                    uRes = (uint)-(int)u1;
                    EnsureChecked(u1 != uSign);
                    break;

                case ExpressionKind.EK_UPLUS:
                    uRes = u1;
                    break;
                case ExpressionKind.EK_BITAND:
                    uRes = u1 & u2;
                    break;
                case ExpressionKind.EK_BITOR:
                    uRes = u1 | u2;
                    break;
                case ExpressionKind.EK_BITXOR:
                    uRes = u1 ^ u2;
                    break;
                case ExpressionKind.EK_BITNOT:
                    uRes = ~u1;
                    break;
                case ExpressionKind.EK_EQ:
                    uRes = (uint)((u1 == u2) ? 1 : 0);
                    break;
                case ExpressionKind.EK_NE:
                    uRes = (uint)((u1 != u2) ? 1 : 0);
                    break;
                case ExpressionKind.EK_LE:
                    uRes = (uint)((fSigned ? (int)u1 <= (int)u2 : u1 <= u2) ? 1 : 0);
                    break;
                case ExpressionKind.EK_LT:
                    uRes = (uint)((fSigned ? (int)u1 < (int)u2 : u1 < u2) ? 1 : 0);
                    break;
                case ExpressionKind.EK_GE:
                    uRes = (uint)((fSigned ? (int)u1 >= (int)u2 : u1 >= u2) ? 1 : 0);
                    break;
                case ExpressionKind.EK_GT:
                    uRes = (uint)((fSigned ? (int)u1 > (int)u2 : u1 > u2) ? 1 : 0);
                    break;
                default:
                    VSFAIL("Unknown op");
                    uRes = 0;
                    break;
            }

            CType typeDest = GetOptPDT(kind.isRelational() ? PredefinedType.PT_BOOL : ptOp);
            Debug.Assert(typeDest != null);

            // Allocate the result node.
            EXPR exprRes = GetExprFactory().CreateConstant(typeDest, ConstValFactory.GetUInt(uRes));

            return exprRes;
        }

        private void EnsureChecked(bool b)
        {
            if (!b && Context.CheckedConstant)
            {
                GetErrorContext().Error(ErrorCode.ERR_CheckedOverflow);
            }
        }

        private EXPR FoldConstI8Op(ExpressionKind kind, EXPR op1, EXPRCONSTANT opConst1, EXPR op2, EXPRCONSTANT opConst2, PredefinedType ptOp)
        {
            Debug.Assert(ptOp == PredefinedType.PT_LONG || ptOp == PredefinedType.PT_ULONG);
            Debug.Assert(opConst1.isCONSTANT_OK());
            Debug.Assert(op1.type.isPredefType(ptOp) && op1.type == opConst1.type);
            Debug.Assert(op2 == null && opConst2 == null ||
                   op2 != null && opConst2 != null && opConst2.isCONSTANT_OK() && op1.type == op2.type && op1.type == opConst2.type);

            bool fSigned = (ptOp == PredefinedType.PT_LONG);
            bool fRes = false;
            // Allocate the result node.
            CType typeDest;
            CONSTVAL cv = new CONSTVAL();


            if (fSigned)
            {
                // long.
                long u1 = opConst1.asCONSTANT().getVal().longVal;
                long u2 = opConst2 != null ? opConst2.asCONSTANT().getVal().longVal : 0;
                long uRes = 0;
                switch (kind)
                {
                    case ExpressionKind.EK_ADD:
                        uRes = u1 + u2;
                        break;

                    case ExpressionKind.EK_SUB:
                        uRes = u1 - u2;
                        break;

                    case ExpressionKind.EK_MUL:
                        uRes = u1 * u2;
                        break;

                    case ExpressionKind.EK_DIV:
                        Debug.Assert(u2 != 0); // Caller should have handled this.
                        uRes = u1 / u2;
                        break;

                    case ExpressionKind.EK_MOD:
                        Debug.Assert(u2 != 0); // Caller should have handled this.
                        uRes = u1 % u2;
                        break;

                    case ExpressionKind.EK_NEG:
                        uRes = -u1;
                        break;

                    case ExpressionKind.EK_UPLUS:
                        uRes = u1;
                        break;
                    case ExpressionKind.EK_BITAND:
                        uRes = u1 & u2;
                        break;
                    case ExpressionKind.EK_BITOR:
                        uRes = u1 | u2;
                        break;
                    case ExpressionKind.EK_BITXOR:
                        uRes = u1 ^ u2;
                        break;
                    case ExpressionKind.EK_BITNOT:
                        uRes = ~u1;
                        break;
                    case ExpressionKind.EK_EQ:
                        fRes = (u1 == u2);
                        break;
                    case ExpressionKind.EK_NE:
                        fRes = (u1 != u2);
                        break;
                    case ExpressionKind.EK_LE:
                        fRes = u1 <= u2;
                        break;
                    case ExpressionKind.EK_LT:
                        fRes = u1 < u2;
                        break;
                    case ExpressionKind.EK_GE:
                        fRes = u1 >= u2;
                        break;
                    case ExpressionKind.EK_GT:
                        fRes = u1 > u2;
                        break;
                    default:
                        VSFAIL("Unknown op");
                        uRes = 0;
                        break;
                }

                if (kind.isRelational())
                {
                    cv.iVal = fRes ? 1 : 0;
                    typeDest = GetReqPDT(PredefinedType.PT_BOOL);
                }
                else
                {
                    cv = GetExprConstants().Create(uRes);
                    typeDest = GetOptPDT(ptOp);
                    Debug.Assert(typeDest != null);
                }
            }
            else
            {
                // ulong.
                // Get the operands
                ulong u1 = opConst1.asCONSTANT().getVal().ulongVal;
                ulong u2 = opConst2 != null ? opConst2.asCONSTANT().getVal().ulongVal : 0;
                ulong uRes = 0;

                // Do the operation.
                switch (kind)
                {
                    case ExpressionKind.EK_ADD:
                        uRes = u1 + u2;
                        break;

                    case ExpressionKind.EK_SUB:
                        uRes = u1 - u2;
                        break;

                    case ExpressionKind.EK_MUL:
                        uRes = u1 * u2;
                        break;

                    case ExpressionKind.EK_DIV:
                        Debug.Assert(u2 != 0); // Caller should have handled this.
                        uRes = u1 / u2;
                        break;

                    case ExpressionKind.EK_MOD:
                        Debug.Assert(u2 != 0); // Caller should have handled this.
                        uRes = u1 % u2;
                        break;

                    case ExpressionKind.EK_NEG:
                        // You can't do this!
                        return BadOperatorTypesError(kind, op1, op2);

                    case ExpressionKind.EK_UPLUS:
                        uRes = u1;
                        break;
                    case ExpressionKind.EK_BITAND:
                        uRes = u1 & u2;
                        break;
                    case ExpressionKind.EK_BITOR:
                        uRes = u1 | u2;
                        break;
                    case ExpressionKind.EK_BITXOR:
                        uRes = u1 ^ u2;
                        break;
                    case ExpressionKind.EK_BITNOT:
                        uRes = ~u1;
                        break;
                    case ExpressionKind.EK_EQ:
                        fRes = (u1 == u2);
                        break;
                    case ExpressionKind.EK_NE:
                        fRes = (u1 != u2);
                        break;
                    case ExpressionKind.EK_LE:
                        fRes = u1 <= u2;
                        break;
                    case ExpressionKind.EK_LT:
                        fRes = u1 < u2;
                        break;
                    case ExpressionKind.EK_GE:
                        fRes = u1 >= u2;
                        break;
                    case ExpressionKind.EK_GT:
                        fRes = u1 > u2;
                        break;
                    default:
                        VSFAIL("Unknown op");
                        uRes = 0;
                        break;
                }

                if (kind.isRelational())
                {
                    cv.iVal = fRes ? 1 : 0;
                    typeDest = GetReqPDT(PredefinedType.PT_BOOL);
                }
                else
                {
                    cv = GetExprConstants().Create(uRes);
                    typeDest = GetOptPDT(ptOp);
                    Debug.Assert(typeDest != null);
                }
            }


            // Allocate the result node.
            EXPR exprRes = GetExprFactory().CreateConstant(typeDest, cv);

            return exprRes;
        }

        /*
          Bind an float/double operator: +, -, , /, %, <, >, <=, >=, ==, !=. If both operations are constants, the result
          will be a constant also. op2 can be null for a unary operator. The operands are assumed
          to be already converted to the correct type.
         */
        private EXPR bindFloatOp(ExpressionKind kind, EXPRFLAG flags, EXPR op1, EXPR op2)
        {
            //Debug.Assert(kind.isRelational() || kind.isArithmetic());
            Debug.Assert(op2 == null || op1.type == op2.type);
            Debug.Assert(op1.type.isPredefType(PredefinedType.PT_FLOAT) || op1.type.isPredefType(PredefinedType.PT_DOUBLE));

            EXPR exprRes;
            EXPR opConst1 = op1.GetConst();
            EXPR opConst2 = op2 != null ? op2.GetConst() : null;

            // Check for constants and fold them.
            if (opConst1 != null && (op2 == null || opConst2 != null))
            {
                // Get the operands
                double d1 = opConst1.asCONSTANT().getVal().doubleVal;
                double d2 = opConst2 != null ? opConst2.asCONSTANT().getVal().doubleVal : 0.0;
                double result = 0;      // if isBoolResult is false
                bool result_b = false;  // if isBoolResult is true

                // Do the operation.
                switch (kind)
                {
                    case ExpressionKind.EK_ADD:
                        result = d1 + d2;
                        break;
                    case ExpressionKind.EK_SUB:
                        result = d1 - d2;
                        break;
                    case ExpressionKind.EK_MUL:
                        result = d1 * d2;
                        break;
                    case ExpressionKind.EK_DIV:
                        result = d1 / d2;
                        break;
                    case ExpressionKind.EK_NEG:
                        result = -d1;
                        break;
                    case ExpressionKind.EK_UPLUS:
                        result = d1;
                        break;
                    case ExpressionKind.EK_MOD:
                        result = d1 % d2;
                        break;
                    case ExpressionKind.EK_EQ:
                        result_b = (d1 == d2);
                        break;
                    case ExpressionKind.EK_NE:
                        result_b = (d1 != d2);
                        break;
                    case ExpressionKind.EK_LE:
                        result_b = (d1 <= d2);
                        break;
                    case ExpressionKind.EK_LT:
                        result_b = (d1 < d2);
                        break;
                    case ExpressionKind.EK_GE:
                        result_b = (d1 >= d2);
                        break;
                    case ExpressionKind.EK_GT:
                        result_b = (d1 > d2);
                        break;
                    default:
                        Debug.Assert(false);
                        result = 0.0;  // unexpected operation.
                        break;
                }

                CType typeDest;
                CONSTVAL cv = new CONSTVAL();

                // Allocate the result node.
                if (kind.isRelational())
                {
                    cv.iVal = result_b ? 1 : 0;
                    typeDest = GetReqPDT(PredefinedType.PT_BOOL);
                }
                else
                {
                    // NaN has some implementation defined bits that differ between platforms.
                    // Normalize it to produce identical images across all platforms
                    /*
                     * How do we get here?
                    if (_isnan(result))
                    {
                        cv = ConstValFactory.GetNan();
                    }
                    else
                    {
                     * */
                    cv = GetExprConstants().Create(result);

                    typeDest = op1.type;
                }
                exprRes = GetExprFactory().CreateConstant(typeDest, cv);
            }
            else
            {
                // Allocate the result expression.
                CType typeDest = kind.isRelational() ? GetReqPDT(PredefinedType.PT_BOOL) : op1.type;

                exprRes = GetExprFactory().CreateOperator(kind, typeDest, op1, op2);
                flags = ~EXPRFLAG.EXF_CHECKOVERFLOW;
                exprRes.flags |= flags;
            }

            return exprRes;
        }

        private EXPR bindStringConcat(EXPR op1, EXPR op2)
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
        private EXPR ambiguousOperatorError(ExpressionKind ek, EXPR op1, EXPR op2)
        {
            RETAILVERIFY(op1 != null);

            // This is exactly the same "hack" that BadOperatorError uses. The first operand contains the
            // name of the operator in its errorString.
            string strOp = op1.errorString;

            // Bad arg types - report error to user.
            if (op2 != null)
            {
                GetErrorContext().Error(ErrorCode.ERR_AmbigBinaryOps, strOp, op1.type, op2.type);
            }
            else
            {
                GetErrorContext().Error(ErrorCode.ERR_AmbigUnaryOp, strOp, op1.type);
            }

            EXPR rval = GetExprFactory().CreateOperator(ek, null, op1, op2);
            rval.SetError();
            return rval;
        }

        private EXPR BindUserBoolOp(ExpressionKind kind, EXPRCALL pCall)
        {
            RETAILVERIFY(pCall != null);
            RETAILVERIFY(pCall.mwi.Meth() != null);
            RETAILVERIFY(pCall.GetOptionalArguments() != null);
            Debug.Assert(kind == ExpressionKind.EK_LOGAND || kind == ExpressionKind.EK_LOGOR);

            CType typeRet = pCall.type;

            Debug.Assert(pCall.mwi.Meth().Params.size == 2);
            if (!GetTypes().SubstEqualTypes(typeRet, pCall.mwi.Meth().Params.Item(0), typeRet) ||
                !GetTypes().SubstEqualTypes(typeRet, pCall.mwi.Meth().Params.Item(1), typeRet))
            {
                MethWithInst mwi = new MethWithInst(null, null);
                EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(null, mwi);
                EXPRCALL pCallTF = GetExprFactory().CreateCall(0, null, null, pMemGroup, null);
                pCallTF.SetError();
                GetErrorContext().Error(ErrorCode.ERR_BadBoolOp, pCall.mwi);
                return GetExprFactory().CreateUserLogOpError(typeRet, pCallTF, pCall);
            }

            Debug.Assert(pCall.GetOptionalArguments().isLIST());

            EXPR pExpr = pCall.GetOptionalArguments().asLIST().GetOptionalElement();
            EXPR pExprWrap = WrapShortLivedExpression(pExpr);
            pCall.GetOptionalArguments().asLIST().SetOptionalElement(pExprWrap);

            // Reflection load the true and false methods.
            SymbolLoader.RuntimeBinderSymbolTable.PopulateSymbolTableWithName(SpecialNames.CLR_True, null, pExprWrap.type.AssociatedSystemType);
            SymbolLoader.RuntimeBinderSymbolTable.PopulateSymbolTableWithName(SpecialNames.CLR_False, null, pExprWrap.type.AssociatedSystemType);

            EXPR pCallT = bindUDUnop(ExpressionKind.EK_TRUE, pExprWrap);
            EXPR pCallF = bindUDUnop(ExpressionKind.EK_FALSE, pExprWrap);

            if (pCallT == null || pCallF == null)
            {
                EXPR pCallTorF = pCallT != null ? pCallT : pCallF;
                if (pCallTorF == null)
                {
                    MethWithInst mwi = new MethWithInst(null, null);
                    EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(null, mwi);
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
            if (!Params.Item(0).IsNonNubValType())
            {
                return false;
            }
            if (!Params.Item(1).IsNonNubValType())
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
                    if (Params.Item(0) != Params.Item(1))
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
            ExpressionKind ek, MethodSymbol method, AggregateType ats, EXPR arg1, EXPR arg2, bool fDontLift)
        {
            if (!method.isOperator || method.Params.size != 2)
            {
                return false;
            }
            Debug.Assert(method.typeVars.size == 0);
            TypeArray paramsCur = GetTypes().SubstTypeArray(method.Params, ats);
            if (canConvert(arg1, paramsCur.Item(0)) && canConvert(arg2, paramsCur.Item(1)))
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
            rgtype[0] = GetTypes().GetNullable(paramsCur.Item(0));
            rgtype[1] = GetTypes().GetNullable(paramsCur.Item(1));
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
            EXPR arg1, EXPR arg2, bool fDontLift)
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
            EXPR arg1, EXPR arg2, bool fDontLift, AggregateType atsStop)
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

        private EXPRCALL BindUDBinop(ExpressionKind ek, EXPR arg1, EXPR arg2, bool fDontLift, out MethPropWithInst ppmpwi)
        {
            List<CandidateFunctionMember> methFirst = new List<CandidateFunctionMember>();

            ppmpwi = null;

            AggregateType[] rgats = { null, null };
            int cats = GetUserDefinedBinopArgumentTypes(arg1.type, arg2.type, rgats);
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

            EXPRLIST args = GetExprFactory().CreateList(arg1, arg2);
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

                EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(null, pmethAmbig1.mpwi);
                EXPRCALL rval = GetExprFactory().CreateCall(0, null, GetExprFactory().CreateList(arg1, arg2), pMemGroup, null);
                rval.SetError();
                return rval;
            }

            if (GetSemanticChecker().CheckBogus(pmethBest.mpwi.Meth()))
            {
                GetErrorContext().ErrorRef(ErrorCode.ERR_BindToBogus, pmethBest.mpwi);

                EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(null, pmethBest.mpwi);
                EXPRCALL rval = GetExprFactory().CreateCall(0, null, GetExprFactory().CreateList(arg1, arg2), pMemGroup, null);
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

        private EXPRCALL BindUDBinopCall(EXPR arg1, EXPR arg2, TypeArray Params, CType typeRet, MethPropWithInst mpwi)
        {
            arg1 = mustConvert(arg1, Params.Item(0));
            arg2 = mustConvert(arg2, Params.Item(1));
            EXPRLIST args = GetExprFactory().CreateList(arg1, arg2);

            checkUnsafe(arg1.type); // added to the binder so we don't bind to pointer ops
            checkUnsafe(arg2.type); // added to the binder so we don't bind to pointer ops
            checkUnsafe(typeRet); // added to the binder so we don't bind to pointer ops


            EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(null, mpwi);
            EXPRCALL call = GetExprFactory().CreateCall(0, typeRet, args, pMemGroup, null);
            call.mwi = new MethWithInst(mpwi);
            verifyMethodArgs(call, mpwi.GetType());
            return call;
        }

        private EXPRCALL BindLiftedUDBinop(ExpressionKind ek, EXPR arg1, EXPR arg2, TypeArray Params, MethPropWithInst mpwi)
        {
            EXPR exprVal1 = arg1;
            EXPR exprVal2 = arg2;
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
            Debug.Assert(paramsRaw.Item(0) == Params.Item(0).GetBaseOrParameterOrElementType());
            Debug.Assert(paramsRaw.Item(1) == Params.Item(1).GetBaseOrParameterOrElementType());

            if (!canConvert(arg1.type.StripNubs(), paramsRaw.Item(0), CONVERTTYPE.NOUDC))
            {
                exprVal1 = mustConvert(arg1, Params.Item(0));
            }
            if (!canConvert(arg2.type.StripNubs(), paramsRaw.Item(1), CONVERTTYPE.NOUDC))
            {
                exprVal2 = mustConvert(arg2, Params.Item(1));
            }
            EXPR nonLiftedArg1 = mustCast(exprVal1, paramsRaw.Item(0));
            EXPR nonLiftedArg2 = mustCast(exprVal2, paramsRaw.Item(1));
            switch (ek)
            {
                default:
                    typeRet = GetTypes().GetNullable(typeRetRaw);
                    break;
                case ExpressionKind.EK_EQ:
                case ExpressionKind.EK_NE:
                    Debug.Assert(paramsRaw.Item(0) == paramsRaw.Item(1));
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

            Debug.Assert(!(ek == ExpressionKind.EK_EQ || ek == ExpressionKind.EK_NE) || nonLiftedArg1.type == nonLiftedArg2.type);

            EXPRCALL nonLiftedResult = BindUDBinopCall(nonLiftedArg1, nonLiftedArg2, paramsRaw, typeRetRaw, mpwi);

            EXPRLIST args = GetExprFactory().CreateList(exprVal1, exprVal2);
            EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(null, mpwi);
            EXPRCALL call = GetExprFactory().CreateCall(0, typeRet, args, pMemGroup, null);
            call.mwi = new MethWithInst(mpwi);

            switch (ek)
            {
                case ExpressionKind.EK_EQ:
                    call.nubLiftKind = NullableCallLiftKind.EqualityOperator;
                    break;

                case ExpressionKind.EK_NE:
                    call.nubLiftKind = NullableCallLiftKind.InequalityOperator;
                    break;

                default:
                    call.nubLiftKind = NullableCallLiftKind.Operator;
                    break;
            }

            call.castOfNonLiftedResultToLiftedType = mustCast(nonLiftedResult, typeRet, 0);
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

        private EXPRBINOP CreateBinopForPredefMethodCall(ExpressionKind ek, PREDEFMETH predefMeth, CType RetType, EXPR arg1, EXPR arg2)
        {
            MethodSymbol methSym = GetSymbolLoader().getPredefinedMembers().GetMethod(predefMeth);
            EXPRBINOP binop = GetExprFactory().CreateBinop(ek, RetType, arg1, arg2);

            // Set the predefined method to call.
            if (methSym != null)
            {
                AggregateSymbol agg = methSym.getClass();
                AggregateType callingType = GetTypes().GetAggregate(agg, BSYMMGR.EmptyTypeArray());
                binop.predefinedMethodToCall = new MethWithInst(methSym, callingType, null);
                binop.SetUserDefinedCallMethod(binop.predefinedMethodToCall);
            }
            else
            {
                // Couldn't find it.
                binop.SetError();
            }
            return binop;
        }

        private EXPRUNARYOP CreateUnaryOpForPredefMethodCall(ExpressionKind ek, PREDEFMETH predefMeth, CType pRetType, EXPR pArg)
        {
            MethodSymbol methSym = GetSymbolLoader().getPredefinedMembers().GetMethod(predefMeth);
            EXPRUNARYOP pUnaryOp = GetExprFactory().CreateUnaryOp(ek, pRetType, pArg);

            // Set the predefined method to call.
            if (methSym != null)
            {
                AggregateSymbol pAgg = methSym.getClass();
                AggregateType pCallingType = GetTypes().GetAggregate(pAgg, BSYMMGR.EmptyTypeArray());
                pUnaryOp.predefinedMethodToCall = new MethWithInst(methSym, pCallingType, null);
                pUnaryOp.UserDefinedCallMethod = pUnaryOp.predefinedMethodToCall;
            }
            else
            {
                pUnaryOp.SetError();
            }
            return pUnaryOp;
        }
    }
}

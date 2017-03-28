// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal static class EXPRExtensions
    {
        public static Expr Map(this Expr expr, ExprFactory factory, Func<Expr, Expr> f)
        {
            Debug.Assert(f != null);
            Debug.Assert(factory != null);

            if (expr == null)
                return f(expr);

            Expr result = null;
            Expr tail = null;
            foreach (Expr item in expr.ToEnumerable())
            {
                Expr mappedItem = f(item);
                factory.AppendItemToList(mappedItem, ref result, ref tail);
            }
            return result;
        }

        public static IEnumerable<Expr> ToEnumerable(this Expr expr)
        {
            Expr exprCur = expr;
            while (exprCur != null)
            {
                if (exprCur.isLIST())
                {
                    yield return exprCur.asLIST().OptionalElement;
                    exprCur = exprCur.asLIST().OptionalNextListNode;
                }
                else
                {
                    yield return exprCur;
                    yield break;
                }
            }
        }
        public static bool isSTMT(this Expr expr)
        {
            return (expr == null) ? false : expr.Kind < ExpressionKind.EK_StmtLim;
        }
        public static ExprStatement asSTMT(this Expr expr)
        {
            Debug.Assert(expr == null || expr.Kind < ExpressionKind.EK_StmtLim);
            return (ExprStatement)expr;
        }
        public static bool isBIN(this Expr expr)
        {
            return (expr == null) ? false : (expr.Kind >= ExpressionKind.EK_TypeLim) &&
                (0 != (expr.Flags & EXPRFLAG.EXF_BINOP));
        }
        public static bool isUnaryOperator(this Expr expr)
        {
            if (expr != null)
            {
                switch (expr.Kind)
                {
                    case ExpressionKind.EK_UNARYOP:
                    case ExpressionKind.EK_TRUE:
                    case ExpressionKind.EK_FALSE:
                    case ExpressionKind.EK_INC:
                    case ExpressionKind.EK_DEC:
                    case ExpressionKind.EK_LOGNOT:
                    case ExpressionKind.EK_NEG:
                    case ExpressionKind.EK_UPLUS:
                    case ExpressionKind.EK_BITNOT:
                    case ExpressionKind.EK_ADDR:
                    case ExpressionKind.EK_DECIMALNEG:
                    case ExpressionKind.EK_DECIMALINC:
                    case ExpressionKind.EK_DECIMALDEC:
                        return true;
                    default:
                        break;
                }
            }
            return false;
        }

        public static bool isLvalue(this Expr expr)
        {
            return (expr == null) ? false : 0 != (expr.Flags & EXPRFLAG.EXF_LVALUE);
        }
        public static bool isChecked(this Expr expr)
        {
            return (expr == null) ? false : 0 != (expr.Flags & EXPRFLAG.EXF_CHECKOVERFLOW);
        }
        public static ExprBinOp asBIN(this Expr expr)
        {
            Debug.Assert(expr == null || 0 != (expr.Flags & EXPRFLAG.EXF_BINOP));
            return (ExprBinOp)expr;
        }
        public static ExprUnaryOp asUnaryOperator(this Expr expr)
        {
            Debug.Assert(expr == null || expr.isUnaryOperator());
            return (ExprUnaryOp)expr;
        }
        public static bool isANYLOCAL(this Expr expr)
        {
            return (expr == null) ? false : expr.Kind == ExpressionKind.EK_LOCAL || expr.Kind == ExpressionKind.EK_THISPOINTER;
        }
        public static ExprLocal asANYLOCAL(this Expr expr)
        {
            Debug.Assert(expr == null || expr.isANYLOCAL());
            return (ExprLocal)expr;
        }
        public static bool isANYLOCAL_OK(this Expr expr)
        {
            return expr.isANYLOCAL() && expr.IsOK;
        }
        public static bool isNull(this Expr expr)
        {
            return expr.isCONSTANT_OK() && (expr.Type.fundType() == FUNDTYPE.FT_REF) && expr.asCONSTANT().Val.IsNullRef;
        }

        public static bool isZero(this Expr expr)
        {
            return (expr.isCONSTANT_OK()) && (expr.asCONSTANT().IsZero);
        }

        private static Expr GetSeqVal(this Expr expr)
        {
            // Scan through EK_SEQUENCE and EK_SEQREV exprs to get the real value.
            if (expr == null)
                return null;

            Expr exprVal = expr;
            for (; ;)
            {
                switch (exprVal.Kind)
                {
                    default:
                        return exprVal;
                    case ExpressionKind.EK_SEQUENCE:
                        exprVal = exprVal.asBIN().OptionalRightChild;
                        break;
                    case ExpressionKind.EK_SEQREV:
                        exprVal = exprVal.asBIN().OptionalLeftChild;
                        break;
                }
            }
        }

        /***************************************************************************************************
        Determine whether this expr has a constant value (EK_CONSTANT or EK_ZEROINIT), possibly with
        side effects (via EK_SEQUENCE or EK_SEQREV). Returns NULL if not, or the constant expr if so.
        The returned Expr will always be an EK_CONSTANT or EK_ZEROINIT.
        ***************************************************************************************************/
        public static Expr GetConst(this Expr expr)
        {
            Expr exprVal = expr.GetSeqVal();
            if (null == exprVal || !exprVal.isCONSTANT_OK() && exprVal.Kind != ExpressionKind.EK_ZEROINIT)
                return null;
            return exprVal;
        }

        private static void RETAILVERIFY(bool f)
        {
            if (!f)
                Debug.Assert(false, "Panic!");
        }

        public static ExprReturn asRETURN(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_RETURN); return (ExprReturn)expr; }
        public static ExprBinOp asBINOP(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_BINOP); return (ExprBinOp)expr; }
        public static ExprList asLIST(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_LIST); return (ExprList)expr; }
        public static ExprArrayIndex asARRAYINDEX(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_ARRAYINDEX); return (ExprArrayIndex)expr; }
        public static ExprCall asCALL(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_CALL); return (ExprCall)expr; }
        public static ExprEvent asEVENT(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_EVENT); return (ExprEvent)expr; }
        public static ExprField asFIELD(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_FIELD); return (ExprField)expr; }
        public static ExprConstant asCONSTANT(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_CONSTANT); return (ExprConstant)expr; }
        public static ExprFuncPtr asFUNCPTR(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_FUNCPTR); return (ExprFuncPtr)expr; }
        public static ExprProperty asPROP(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_PROP); return (ExprProperty)expr; }
        public static ExprWrap asWRAP(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_WRAP); return (ExprWrap)expr; }
        public static ExprArrayInit asARRINIT(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_ARRINIT); return (ExprArrayInit)expr; }
        public static ExprCast asCAST(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_CAST); return (ExprCast)expr; }
        public static ExprUserDefinedConversion asUSERDEFINEDCONVERSION(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_USERDEFINEDCONVERSION); return (ExprUserDefinedConversion)expr; }
        public static ExprTypeOf asTYPEOF(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_TYPEOF); return (ExprTypeOf)expr; }
        public static ExprZeroInit asZEROINIT(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_ZEROINIT); return (ExprZeroInit)expr; }
        public static ExprUserLogicalOp asUSERLOGOP(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_USERLOGOP); return (ExprUserLogicalOp)expr; }
        public static ExprMemberGroup asMEMGRP(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_MEMGRP); return (ExprMemberGroup)expr; }
        public static ExprFieldInfo asFIELDINFO(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_FIELDINFO); return (ExprFieldInfo)expr; }
        public static ExprMethodInfo asMETHODINFO(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_METHODINFO); return (ExprMethodInfo)expr; }
        public static ExprPropertyInfo asPropertyInfo(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_PROPERTYINFO); return (ExprPropertyInfo)expr; }
        public static ExprNamedArgumentSpecification asNamedArgumentSpecification(this Expr expr) { RETAILVERIFY(expr == null || expr.Kind == ExpressionKind.EK_NamedArgumentSpecification); return (ExprNamedArgumentSpecification)expr; }

        public static bool isCONSTANT_OK(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_CONSTANT && expr.IsOK); }
        public static bool isRETURN(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_RETURN); }
        public static bool isLIST(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_LIST); }
        public static bool isARRAYINDEX(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_ARRAYINDEX); }
        public static bool isCALL(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_CALL); }
        public static bool isFIELD(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_FIELD); }
        public static bool isCONSTANT(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_CONSTANT); }
        public static bool isCLASS(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_CLASS); }
        public static bool isPROP(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_PROP); }
        public static bool isWRAP(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_WRAP); }
        public static bool isARRINIT(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_ARRINIT); }
        public static bool isCAST(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_CAST); }
        public static bool isUSERDEFINEDCONVERSION(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_USERDEFINEDCONVERSION); }
        public static bool isTYPEOF(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_TYPEOF); }
        public static bool isZEROINIT(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_ZEROINIT); }
        public static bool isMEMGRP(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_MEMGRP); }
        public static bool isBOUNDLAMBDA(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_BOUNDLAMBDA); }
        public static bool isUNBOUNDLAMBDA(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_UNBOUNDLAMBDA); }
        public static bool isMETHODINFO(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_METHODINFO); }
        public static bool isNamedArgumentSpecification(this Expr expr) { return (expr == null) ? false : (expr.Kind == ExpressionKind.EK_NamedArgumentSpecification); }
    }
}

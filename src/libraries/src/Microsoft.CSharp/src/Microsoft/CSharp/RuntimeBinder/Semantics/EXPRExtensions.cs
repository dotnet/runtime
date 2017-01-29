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
        public static EXPR Map(this EXPR expr, ExprFactory factory, Func<EXPR, EXPR> f)
        {
            Debug.Assert(f != null);
            Debug.Assert(factory != null);

            if (expr == null)
                return f(expr);

            EXPR result = null;
            EXPR tail = null;
            foreach (EXPR item in expr.ToEnumerable())
            {
                EXPR mappedItem = f(item);
                factory.AppendItemToList(mappedItem, ref result, ref tail);
            }
            return result;
        }

        public static IEnumerable<EXPR> ToEnumerable(this EXPR expr)
        {
            EXPR exprCur = expr;
            while (exprCur != null)
            {
                if (exprCur.isLIST())
                {
                    yield return exprCur.asLIST().GetOptionalElement();
                    exprCur = exprCur.asLIST().GetOptionalNextListNode();
                }
                else
                {
                    yield return exprCur;
                    yield break;
                }
            }
        }
        public static bool isSTMT(this EXPR expr)
        {
            return (expr == null) ? false : expr.kind < ExpressionKind.EK_StmtLim;
        }
        public static EXPRSTMT asSTMT(this EXPR expr)
        {
            Debug.Assert(expr == null || expr.kind < ExpressionKind.EK_StmtLim);
            return (EXPRSTMT)expr;
        }
        public static bool isBIN(this EXPR expr)
        {
            return (expr == null) ? false : (expr.kind >= ExpressionKind.EK_TypeLim) &&
                (0 != (expr.flags & EXPRFLAG.EXF_BINOP));
        }
        public static bool isUnaryOperator(this EXPR expr)
        {
            if (expr != null)
            {
                switch (expr.kind)
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

        public static bool isLvalue(this EXPR expr)
        {
            return (expr == null) ? false : 0 != (expr.flags & EXPRFLAG.EXF_LVALUE);
        }
        public static bool isChecked(this EXPR expr)
        {
            return (expr == null) ? false : 0 != (expr.flags & EXPRFLAG.EXF_CHECKOVERFLOW);
        }
        public static EXPRBINOP asBIN(this EXPR expr)
        {
            Debug.Assert(expr == null || 0 != (expr.flags & EXPRFLAG.EXF_BINOP));
            return (EXPRBINOP)expr;
        }
        public static EXPRUNARYOP asUnaryOperator(this EXPR expr)
        {
            Debug.Assert(expr == null || expr.isUnaryOperator());
            return (EXPRUNARYOP)expr;
        }
        public static bool isANYLOCAL(this EXPR expr)
        {
            return (expr == null) ? false : expr.kind == ExpressionKind.EK_LOCAL || expr.kind == ExpressionKind.EK_THISPOINTER;
        }
        public static EXPRLOCAL asANYLOCAL(this EXPR expr)
        {
            Debug.Assert(expr == null || expr.isANYLOCAL());
            return (EXPRLOCAL)expr;
        }
        public static bool isANYLOCAL_OK(this EXPR expr)
        {
            return expr.isANYLOCAL() && expr.isOK();
        }
        public static bool isNull(this EXPR expr)
        {
            return expr.isCONSTANT_OK() && (expr.type.fundType() == FUNDTYPE.FT_REF) && expr.asCONSTANT().Val.IsNullRef();
        }

        public static bool isZero(this EXPR expr)
        {
            return (expr.isCONSTANT_OK()) && (expr.asCONSTANT().isZero());
        }

        private static EXPR GetSeqVal(this EXPR expr)
        {
            // Scan through EK_SEQUENCE and EK_SEQREV exprs to get the real value.
            if (expr == null)
                return null;

            EXPR exprVal = expr;
            for (; ;)
            {
                switch (exprVal.kind)
                {
                    default:
                        return exprVal;
                    case ExpressionKind.EK_SEQUENCE:
                        exprVal = exprVal.asBIN().GetOptionalRightChild();
                        break;
                    case ExpressionKind.EK_SEQREV:
                        exprVal = exprVal.asBIN().GetOptionalLeftChild();
                        break;
                }
            }
        }

        /***************************************************************************************************
        Determine whether this expr has a constant value (EK_CONSTANT or EK_ZEROINIT), possibly with
        side effects (via EK_SEQUENCE or EK_SEQREV). Returns NULL if not, or the constant expr if so.
        The returned EXPR will always be an EK_CONSTANT or EK_ZEROINIT.
        ***************************************************************************************************/
        public static EXPR GetConst(this EXPR expr)
        {
            EXPR exprVal = expr.GetSeqVal();
            if (null == exprVal || !exprVal.isCONSTANT_OK() && exprVal.kind != ExpressionKind.EK_ZEROINIT)
                return null;
            return exprVal;
        }

        private static void RETAILVERIFY(bool f)
        {
            if (!f)
                Debug.Assert(false, "Panic!");
        }

        public static EXPRRETURN asRETURN(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_RETURN); return (EXPRRETURN)expr; }
        public static EXPRBINOP asBINOP(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_BINOP); return (EXPRBINOP)expr; }
        public static EXPRLIST asLIST(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_LIST); return (EXPRLIST)expr; }
        public static EXPRARRAYINDEX asARRAYINDEX(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_ARRAYINDEX); return (EXPRARRAYINDEX)expr; }
        public static EXPRCALL asCALL(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_CALL); return (EXPRCALL)expr; }
        public static EXPREVENT asEVENT(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_EVENT); return (EXPREVENT)expr; }
        public static EXPRFIELD asFIELD(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_FIELD); return (EXPRFIELD)expr; }
        public static EXPRCONSTANT asCONSTANT(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_CONSTANT); return (EXPRCONSTANT)expr; }
        public static EXPRFUNCPTR asFUNCPTR(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_FUNCPTR); return (EXPRFUNCPTR)expr; }
        public static EXPRPROP asPROP(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_PROP); return (EXPRPROP)expr; }
        public static EXPRWRAP asWRAP(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_WRAP); return (EXPRWRAP)expr; }
        public static EXPRARRINIT asARRINIT(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_ARRINIT); return (EXPRARRINIT)expr; }
        public static EXPRCAST asCAST(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_CAST); return (EXPRCAST)expr; }
        public static EXPRUSERDEFINEDCONVERSION asUSERDEFINEDCONVERSION(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_USERDEFINEDCONVERSION); return (EXPRUSERDEFINEDCONVERSION)expr; }
        public static EXPRTYPEOF asTYPEOF(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_TYPEOF); return (EXPRTYPEOF)expr; }
        public static EXPRZEROINIT asZEROINIT(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_ZEROINIT); return (EXPRZEROINIT)expr; }
        public static EXPRUSERLOGOP asUSERLOGOP(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_USERLOGOP); return (EXPRUSERLOGOP)expr; }
        public static EXPRMEMGRP asMEMGRP(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_MEMGRP); return (EXPRMEMGRP)expr; }
        public static EXPRFIELDINFO asFIELDINFO(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_FIELDINFO); return (EXPRFIELDINFO)expr; }
        public static EXPRMETHODINFO asMETHODINFO(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_METHODINFO); return (EXPRMETHODINFO)expr; }
        public static EXPRPropertyInfo asPropertyInfo(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_PROPERTYINFO); return (EXPRPropertyInfo)expr; }
        public static EXPRNamedArgumentSpecification asNamedArgumentSpecification(this EXPR expr) { RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_NamedArgumentSpecification); return (EXPRNamedArgumentSpecification)expr; }

        public static bool isCONSTANT_OK(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_CONSTANT && expr.isOK()); }
        public static bool isRETURN(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_RETURN); }
        public static bool isLIST(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_LIST); }
        public static bool isARRAYINDEX(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_ARRAYINDEX); }
        public static bool isCALL(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_CALL); }
        public static bool isFIELD(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_FIELD); }
        public static bool isCONSTANT(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_CONSTANT); }
        public static bool isCLASS(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_CLASS); }
        public static bool isPROP(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_PROP); }
        public static bool isWRAP(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_WRAP); }
        public static bool isARRINIT(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_ARRINIT); }
        public static bool isCAST(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_CAST); }
        public static bool isUSERDEFINEDCONVERSION(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_USERDEFINEDCONVERSION); }
        public static bool isTYPEOF(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_TYPEOF); }
        public static bool isZEROINIT(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_ZEROINIT); }
        public static bool isMEMGRP(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_MEMGRP); }
        public static bool isBOUNDLAMBDA(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_BOUNDLAMBDA); }
        public static bool isUNBOUNDLAMBDA(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_UNBOUNDLAMBDA); }
        public static bool isMETHODINFO(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_METHODINFO); }
        public static bool isNamedArgumentSpecification(this EXPR expr) { return (expr == null) ? false : (expr.kind == ExpressionKind.EK_NamedArgumentSpecification); }
    }
}

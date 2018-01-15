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
                if (exprCur is ExprList list)
                {
                    yield return list.OptionalElement;
                    exprCur = list.OptionalNextListNode;
                }
                else
                {
                    yield return exprCur;
                    yield break;
                }
            }
        }

        [Conditional("DEBUG")]
        public static void AssertIsBin(this Expr expr)
        {
            Debug.Assert(expr?.Kind >= ExpressionKind.TypeLimit && 0 != (expr.Flags & EXPRFLAG.EXF_BINOP));
        }
        public static bool isLvalue(this Expr expr)
        {
            return (expr == null) ? false : 0 != (expr.Flags & EXPRFLAG.EXF_LVALUE);
        }
        public static bool isChecked(this Expr expr)
        {
            return (expr == null) ? false : 0 != (expr.Flags & EXPRFLAG.EXF_CHECKOVERFLOW);
        }

        public static bool isNull(this Expr expr)
            => expr is ExprConstant constant && expr.Type.fundType() == FUNDTYPE.FT_REF && constant.Val.IsNullRef;

        public static bool IsZero(this Expr expr) => expr is ExprConstant constant && constant.IsZero;

        private static Expr GetSeqVal(this Expr expr)
        {
            // Scan through EK_SEQUENCE and EK_SEQREV exprs to get the real value.
            if (expr == null)
            {
                return null;
            }

            Expr exprVal = expr;
            while (exprVal.Kind == ExpressionKind.Sequence)
            {
                exprVal = ((ExprBinOp)exprVal).OptionalRightChild;
            }

            return exprVal;
        }

        /***************************************************************************************************
        Determine whether this expr has a constant value (EK_CONSTANT or EK_ZEROINIT), possibly with
        side effects (via EK_SEQUENCE or EK_SEQREV). Returns NULL if not, or the constant expr if so.
        The returned Expr will always be an EK_CONSTANT or EK_ZEROINIT.
        ***************************************************************************************************/
        public static Expr GetConst(this Expr expr)
        {
            Expr exprVal = expr.GetSeqVal();
            switch (exprVal?.Kind)
            {
                case ExpressionKind.Constant:
                case ExpressionKind.ZeroInit:
                    return exprVal;
            }

            return null;
        }
    }
}

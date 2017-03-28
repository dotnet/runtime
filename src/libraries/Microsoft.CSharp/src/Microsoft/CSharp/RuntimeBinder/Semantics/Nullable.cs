// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Errors;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class CNullable
    {
        private readonly SymbolLoader _pSymbolLoader;
        private readonly ExprFactory _exprFactory;
        private readonly ErrorHandling _pErrorContext;

        private SymbolLoader GetSymbolLoader()
        {
            return _pSymbolLoader;
        }
        private ExprFactory GetExprFactory()
        {
            return _exprFactory;
        }
        private ErrorHandling GetErrorContext()
        {
            return _pErrorContext;
        }
        private static bool IsNullableConstructor(Expr expr)
        {
            Debug.Assert(expr != null);

            if (!expr.isCALL())
            {
                return false;
            }

            ExprCall pCall = expr.asCALL();
            if (pCall.MemberGroup.OptionalObject != null)
            {
                return false;
            }

            MethodSymbol meth = pCall.MethWithInst.Meth();
            if (meth == null)
            {
                return false;
            }
            return meth.IsNullableConstructor();
        }
        public static Expr StripNullableConstructor(Expr pExpr)
        {
            while (IsNullableConstructor(pExpr))
            {
                Debug.Assert(pExpr.isCALL());
                pExpr = pExpr.asCALL().OptionalArguments;
                Debug.Assert(pExpr != null && !pExpr.isLIST());
            }
            return pExpr;
        }

        // Value
        public Expr BindValue(Expr exprSrc)
        {
            Debug.Assert(exprSrc != null && exprSrc.Type.IsNullableType());

            // For new T?(x), the answer is x.
            if (IsNullableConstructor(exprSrc))
            {
                Debug.Assert(exprSrc.asCALL().OptionalArguments != null && !exprSrc.asCALL().OptionalArguments.isLIST());
                return exprSrc.asCALL().OptionalArguments;
            }

            CType typeBase = exprSrc.Type.AsNullableType().GetUnderlyingType();
            AggregateType ats = exprSrc.Type.AsNullableType().GetAts(GetErrorContext());
            if (ats == null)
            {
                ExprProperty rval = GetExprFactory().CreateProperty(typeBase, exprSrc);
                rval.SetError();
                return rval;
            }

            PropertySymbol prop = GetSymbolLoader().getBSymmgr().propNubValue;
            if (prop == null)
            {
                prop = GetSymbolLoader().getPredefinedMembers().GetProperty(PREDEFPROP.PP_G_OPTIONAL_VALUE);
                GetSymbolLoader().getBSymmgr().propNubValue = prop;
            }

            PropWithType pwt = new PropWithType(prop, ats);
            MethWithType mwt = new MethWithType(prop?.methGet, ats);
            MethPropWithInst mpwi = new MethPropWithInst(prop, ats);
            ExprMemberGroup pMemGroup = GetExprFactory().CreateMemGroup(exprSrc, mpwi);
            ExprProperty exprRes = GetExprFactory().CreateProperty(typeBase, null, null, pMemGroup, pwt, mwt, null);

            if (prop == null)
            {
                exprRes.SetError();
            }

            return exprRes;
        }

        public ExprCall BindNew(Expr pExprSrc)
        {
            Debug.Assert(pExprSrc != null);

            NullableType pNubSourceType = GetSymbolLoader().GetTypeManager().GetNullable(pExprSrc.Type);

            AggregateType pSourceType = pNubSourceType.GetAts(GetErrorContext());
            if (pSourceType == null)
            {
                MethWithInst mwi = new MethWithInst(null, null);
                ExprMemberGroup pMemGroup = GetExprFactory().CreateMemGroup(pExprSrc, mwi);
                ExprCall rval = GetExprFactory().CreateCall(0, pNubSourceType, null, pMemGroup, null);
                rval.SetError();
                return rval;
            }

            MethodSymbol meth = GetSymbolLoader().getBSymmgr().methNubCtor;
            if (meth == null)
            {
                meth = GetSymbolLoader().getPredefinedMembers().GetMethod(PREDEFMETH.PM_G_OPTIONAL_CTOR);
                GetSymbolLoader().getBSymmgr().methNubCtor = meth;
            }

            MethWithInst methwithinst = new MethWithInst(meth, pSourceType, BSYMMGR.EmptyTypeArray());
            ExprMemberGroup memgroup = GetExprFactory().CreateMemGroup(null, methwithinst);
            ExprCall pExprRes = GetExprFactory().CreateCall(EXPRFLAG.EXF_NEWOBJCALL | EXPRFLAG.EXF_CANTBENULL, pNubSourceType, pExprSrc, memgroup, methwithinst);

            if (meth == null)
            {
                pExprRes.SetError();
            }

            return pExprRes;
        }
        public CNullable(SymbolLoader symbolLoader, ErrorHandling errorContext, ExprFactory exprFactory)
        {
            _pSymbolLoader = symbolLoader;
            _pErrorContext = errorContext;
            _exprFactory = exprFactory;
        }
    }

    internal sealed partial class ExpressionBinder
    {
        // Create an expr for exprSrc.Value where exprSrc.type is a NullableType.
        private Expr BindNubValue(Expr exprSrc)
        {
            return m_nullable.BindValue(exprSrc);
        }

        // Create an expr for new T?(exprSrc) where T is exprSrc.type.
        private ExprCall BindNubNew(Expr exprSrc)
        {
            return m_nullable.BindNew(exprSrc);
        }
    }
}

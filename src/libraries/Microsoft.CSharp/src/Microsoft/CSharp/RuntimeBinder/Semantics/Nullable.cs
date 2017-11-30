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

        private static bool IsNullableConstructor(Expr expr, out ExprCall call)
        {
            Debug.Assert(expr != null);

            if (expr is ExprCall pCall && pCall.MemberGroup.OptionalObject == null
                && (pCall.MethWithInst?.Meth().IsNullableConstructor() ?? false))
            {
                call = pCall;
                return true;
            }

            call = null;
            return false;
        }

        public static Expr StripNullableConstructor(Expr pExpr)
        {
            while (IsNullableConstructor(pExpr, out ExprCall call))
            {
                pExpr = call.OptionalArguments;
                Debug.Assert(pExpr != null && !(pExpr is ExprList));
            }

            return pExpr;
        }

        // Value
        public Expr BindValue(Expr exprSrc)
        {
            Debug.Assert(exprSrc != null && exprSrc.Type is NullableType);

            // For new T?(x), the answer is x.
            if (IsNullableConstructor(exprSrc, out ExprCall call))
            {
                var args = call.OptionalArguments;
                Debug.Assert(args != null && !(args is ExprList));
                return args;
            }

            NullableType nubSrc = (NullableType)exprSrc.Type;
            CType typeBase = nubSrc.GetUnderlyingType();
            AggregateType ats = nubSrc.GetAts();
            PropertySymbol prop = GetSymbolLoader().getBSymmgr().propNubValue;
            if (prop == null)
            {
                prop = GetSymbolLoader().getPredefinedMembers().GetProperty(PREDEFPROP.PP_G_OPTIONAL_VALUE);
                Debug.Assert(prop != null);
                GetSymbolLoader().getBSymmgr().propNubValue = prop;
            }

            PropWithType pwt = new PropWithType(prop, ats);
            MethPropWithInst mpwi = new MethPropWithInst(prop, ats);
            ExprMemberGroup pMemGroup = GetExprFactory().CreateMemGroup(exprSrc, mpwi);
            return GetExprFactory().CreateProperty(typeBase, null, null, pMemGroup, pwt, null);
        }

        public ExprCall BindNew(Expr pExprSrc)
        {
            Debug.Assert(pExprSrc != null);

            NullableType pNubSourceType = GetSymbolLoader().GetTypeManager().GetNullable(pExprSrc.Type);

            AggregateType pSourceType = pNubSourceType.GetAts();
            MethodSymbol meth = GetSymbolLoader().getBSymmgr().methNubCtor;
            if (meth == null)
            {
                meth = GetSymbolLoader().getPredefinedMembers().GetMethod(PREDEFMETH.PM_G_OPTIONAL_CTOR);
                Debug.Assert(meth != null);
                GetSymbolLoader().getBSymmgr().methNubCtor = meth;
            }

            MethWithInst methwithinst = new MethWithInst(meth, pSourceType, BSYMMGR.EmptyTypeArray());
            ExprMemberGroup memgroup = GetExprFactory().CreateMemGroup(null, methwithinst);
            return GetExprFactory().CreateCall(EXPRFLAG.EXF_NEWOBJCALL | EXPRFLAG.EXF_CANTBENULL, pNubSourceType, pExprSrc, memgroup, methwithinst);
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

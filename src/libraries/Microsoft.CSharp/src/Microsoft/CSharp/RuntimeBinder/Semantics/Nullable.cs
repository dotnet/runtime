// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal readonly partial struct ExpressionBinder
    {
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

        private static Expr StripNullableConstructor(Expr pExpr)
        {
            while (IsNullableConstructor(pExpr, out ExprCall call))
            {
                pExpr = call.OptionalArguments;
                Debug.Assert(pExpr != null && !(pExpr is ExprList));
            }

            return pExpr;
        }

        // Create an expr for exprSrc.Value where exprSrc.type is a NullableType.
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static Expr BindNubValue(Expr exprSrc)
        {
            Debug.Assert(exprSrc != null && exprSrc.Type is NullableType);

            // For new T?(x), the answer is x.
            if (IsNullableConstructor(exprSrc, out ExprCall call))
            {
                Expr args = call.OptionalArguments;
                Debug.Assert(args != null && !(args is ExprList));
                return args;
            }

            NullableType nubSrc = (NullableType)exprSrc.Type;
            CType typeBase = nubSrc.UnderlyingType;
            AggregateType ats = nubSrc.GetAts();
            PropertySymbol prop = PredefinedMembers.GetProperty(PREDEFPROP.PP_G_OPTIONAL_VALUE);
            PropWithType pwt = new PropWithType(prop, ats);
            MethPropWithInst mpwi = new MethPropWithInst(prop, ats);
            ExprMemberGroup pMemGroup = ExprFactory.CreateMemGroup(exprSrc, mpwi);
            return ExprFactory.CreateProperty(typeBase, null, null, pMemGroup, pwt, null);
        }

        // Create an expr for new T?(exprSrc) where T is exprSrc.type.
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static ExprCall BindNubNew(Expr exprSrc)
        {
            Debug.Assert(exprSrc != null);

            NullableType pNubSourceType = TypeManager.GetNullable(exprSrc.Type);
            AggregateType pSourceType = pNubSourceType.GetAts();
            MethodSymbol meth = PredefinedMembers.GetMethod(PREDEFMETH.PM_G_OPTIONAL_CTOR);
            MethWithInst methwithinst = new MethWithInst(meth, pSourceType, TypeArray.Empty);
            ExprMemberGroup memgroup = ExprFactory.CreateMemGroup(null, methwithinst);
            return ExprFactory.CreateCall(EXPRFLAG.EXF_NEWOBJCALL | EXPRFLAG.EXF_CANTBENULL, pNubSourceType, exprSrc, memgroup, methwithinst);
        }
    }
}

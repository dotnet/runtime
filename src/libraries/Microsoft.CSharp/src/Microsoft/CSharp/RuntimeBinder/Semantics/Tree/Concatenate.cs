// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprConcat : ExprWithType
    {
        public ExprConcat(Expr first, Expr second)
            : base(ExpressionKind.Concat, TypeFromOperands(first, second))
        {
            Debug.Assert(first?.Type != null);
            Debug.Assert(second?.Type != null);
            Debug.Assert(first.Type.IsPredefType(PredefinedType.PT_STRING) || second.Type.IsPredefType(PredefinedType.PT_STRING));
            FirstArgument = first;
            SecondArgument = second;
        }

        private static CType TypeFromOperands(Expr first, Expr second)
        {
            CType type = first.Type;
            if (type.IsPredefType(PredefinedType.PT_STRING))
            {
                return type;
            }

            Debug.Assert(second.Type.IsPredefType(PredefinedType.PT_STRING));
            return second.Type;
        }

        public Expr FirstArgument { get; set; }

        public Expr SecondArgument { get; set; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprUnaryOp : ExprOperator
    {
        public ExprUnaryOp(ExpressionKind kind, CType type, Expr operand)
            : base(kind, type)
        {
            Debug.Assert(kind.IsUnaryOperator());
            Debug.Assert(operand != null);
            Child = operand;
        }

        public ExprUnaryOp(ExpressionKind kind, CType type, Expr operand, Expr call, MethPropWithInst userMethod)
            : base(kind, type, call, userMethod)
        {
            Debug.Assert(kind.IsUnaryOperator());
            Debug.Assert(operand != null);
            Debug.Assert(type != null);
            Debug.Assert(call != null);
            Debug.Assert(userMethod != null);
            Child = operand;
        }

        public Expr Child { get; set; }
    }
}

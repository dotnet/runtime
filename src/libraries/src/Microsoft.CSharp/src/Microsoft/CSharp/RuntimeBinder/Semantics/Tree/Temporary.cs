// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprWrap : Expr
    {
        public ExprWrap(Expr expression)
            : base(ExpressionKind.Wrap)
        {
            OptionalExpression = expression;
            Type = expression?.Type;
            Flags = EXPRFLAG.EXF_LVALUE;
        }

        public Expr OptionalExpression { get; }
    }
}

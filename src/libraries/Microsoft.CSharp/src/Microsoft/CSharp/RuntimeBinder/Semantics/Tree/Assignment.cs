// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprAssignment : Expr
    {
        private Expr _lhs;

        public ExprAssignment(Expr lhs, Expr rhs)
            : base(ExpressionKind.Assignment)
        {
            LHS = lhs;
            RHS = rhs;
            Flags = EXPRFLAG.EXF_ASSGOP;
        }

        public Expr LHS
        {
            get => _lhs;
            set => Type = (_lhs = value).Type;
        }

        public Expr RHS { get; set; }
    }
}

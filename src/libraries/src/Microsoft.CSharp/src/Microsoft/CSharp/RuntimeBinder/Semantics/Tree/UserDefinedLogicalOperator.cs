// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprUserLogicalOp : ExprWithType
    {
        public ExprUserLogicalOp(CType type)
            : base(ExpressionKind.UserLogicalOp, type)
        {
        }

        public Expr TrueFalseCall { get; set; }

        public ExprCall OperatorCall { get; set; }

        public Expr FirstOperandToExamine { get; set; }
    }
}

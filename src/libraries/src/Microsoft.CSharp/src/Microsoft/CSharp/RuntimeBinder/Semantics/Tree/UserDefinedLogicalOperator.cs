// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprUserLogicalOp : ExprWithType
    {
        public ExprUserLogicalOp(CType type, Expr trueFalseCall, ExprCall operatorCall)
            : base(ExpressionKind.UserLogicalOp, type)
        {
            Debug.Assert(trueFalseCall != null);
            Debug.Assert((operatorCall?.OptionalArguments as ExprList)?.OptionalElement != null);
            Flags = EXPRFLAG.EXF_ASSGOP;
            TrueFalseCall = trueFalseCall;
            OperatorCall = operatorCall;
            Expr leftChild = ((ExprList)operatorCall.OptionalArguments).OptionalElement;
            // In the EE case, we don't create WRAPEXPRs.
            FirstOperandToExamine = leftChild is ExprWrap wrap ? wrap.OptionalExpression : leftChild;
            Debug.Assert(FirstOperandToExamine != null);
        }

        public Expr TrueFalseCall { get; set; }

        public ExprCall OperatorCall { get; set; }

        public Expr FirstOperandToExamine { get; set; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprBinOp : ExprOperator
    {
        public ExprBinOp(ExpressionKind kind, CType type)
            : base(kind, type)
        {
            Debug.Assert(kind > ExpressionKind.TypeLimit);
        }

        public Expr OptionalLeftChild { get; set; }

        public Expr OptionalRightChild { get; set; }

        public bool IsLifted { get; set; }

        public void SetAssignment()
        {
            Flags |= EXPRFLAG.EXF_ASSGOP;
        }
    }
}

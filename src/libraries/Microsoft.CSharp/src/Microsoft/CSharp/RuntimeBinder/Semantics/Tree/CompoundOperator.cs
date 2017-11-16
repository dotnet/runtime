// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprMultiGet : ExprWithType
    {
        public ExprMultiGet(CType type, EXPRFLAG flags, ExprMulti multi)
            : base(ExpressionKind.MultiGet, type)
        {
            Debug.Assert((flags & ~EXPRFLAG.EXF_MASK_ANY) == 0);
            Flags = flags;
            OptionalMulti = multi;
        }

        public ExprMulti OptionalMulti { get; set; }
    }

    internal sealed class ExprMulti : ExprWithType
    {
        public ExprMulti(CType type, EXPRFLAG flags, Expr left, Expr op)
            : base(ExpressionKind.Multi, type)
        {
            Debug.Assert(left != null);
            Debug.Assert(op != null);
            Flags = flags;
            Left = left;
            Operator = op;
        }

        public Expr Left { get; set; }

        public Expr Operator { get; set; }
    }
}

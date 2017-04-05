// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprMultiGet : ExprWithType
    {
        public ExprMultiGet(CType type)
            : base(ExpressionKind.MultiGet, type)
        {
        }

        public ExprMulti OptionalMulti { get; set; }
    }

    internal sealed class ExprMulti : ExprWithType
    {
        public ExprMulti(CType type)
            : base(ExpressionKind.Multi, type)
        {
        }

        public Expr Left { get; set; }

        public Expr Operator { get; set; }
    }
}

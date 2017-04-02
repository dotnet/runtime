// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprMultiGet : ExprWithType
    {
        public ExprMultiGet(CType type)
            : base(type)
        {
        }

        public ExprMulti OptionalMulti { get; set; }

        public override ExpressionKind Kind => ExpressionKind.EK_MULTIGET;
    }

    internal sealed class ExprMulti : ExprWithType
    {
        public ExprMulti(CType type)
            : base(type)
        {
        }

        public Expr Left { get; set; }

        public Expr Operator { get; set; }

        public override ExpressionKind Kind => ExpressionKind.EK_MULTI;
    }
}

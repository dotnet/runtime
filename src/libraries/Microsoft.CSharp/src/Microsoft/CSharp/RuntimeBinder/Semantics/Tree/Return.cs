// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprReturn : ExprStatement, IExprWithObject
    {
        public ExprReturn()
            : base(ExpressionKind.Return)
        {
        }

        // Return object is optional because of void returns.
        public Expr OptionalObject { get; set; }
    }
}

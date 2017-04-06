// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprNamedArgumentSpecification : Expr
    {
        public ExprNamedArgumentSpecification()
            : base(ExpressionKind.NamedArgumentSpecification)
        {
        }

        public Name Name { get; set; }

        public Expr Value { get; set; }

        public override CType Type => Value.Type;
    }
}

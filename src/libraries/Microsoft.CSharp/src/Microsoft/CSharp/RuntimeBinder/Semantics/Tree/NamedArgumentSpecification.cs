// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprNamedArgumentSpecification : Expr
    {
        private Expr _value;

        public ExprNamedArgumentSpecification(Name name, Expr value)
            : base(ExpressionKind.NamedArgumentSpecification)
        {
            Name = name;
            Value = value;
        }

        public Name Name { get; }

        public Expr Value
        {
            get => _value;
            set => Type = (_value = value).Type;
        }
    }
}

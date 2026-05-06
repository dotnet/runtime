// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal abstract class ExprWithArgs : ExprWithType
    {
        protected ExprWithArgs(ExpressionKind kind, CType type)
            : base(kind, type)
        {
        }

        public ExprMemberGroup MemberGroup { get; set; }

        public Expr OptionalArguments { get; set; }

        public abstract SymWithType GetSymWithType();
    }
}

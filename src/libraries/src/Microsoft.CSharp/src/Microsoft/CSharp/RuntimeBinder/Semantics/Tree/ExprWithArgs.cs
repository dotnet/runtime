// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal abstract class ExprWithArgs : ExprWithType, IExprWithObject
    {
        protected ExprWithArgs(ExpressionKind kind, CType type)
            : base(kind, type)
        {
        }

        public ExprMemberGroup MemberGroup { get; set; }

        public Expr OptionalObject
        {
            get => MemberGroup.OptionalObject;
            set => MemberGroup.OptionalObject = value;
        }

        public Expr OptionalArguments { get; set; }

        public abstract SymWithType GetSymWithType();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprCall : Expr
    {
        public Expr OptionalArguments { get; set; }

        public ExprMemberGroup MemberGroup { get; set; }

        public MethWithInst MethWithInst { get; set; }

        public PREDEFMETH PredefinedMethod { get; set; }

        public NullableCallLiftKind NullableCallLiftKind { get; set; }

        public Expr PConversions { get; set; }

        public Expr CastOfNonLiftedResultToLiftedType { get; set; }
    }
}

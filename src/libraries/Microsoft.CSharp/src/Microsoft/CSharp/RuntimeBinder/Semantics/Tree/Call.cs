// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprCall : ExprWithArgs
    {
        public ExprCall(CType type, EXPRFLAG flags, Expr arguments, ExprMemberGroup member, MethWithInst method)
            : base(ExpressionKind.Call, type)
        {
            Debug.Assert(
                (flags & ~(EXPRFLAG.EXF_NEWOBJCALL | EXPRFLAG.EXF_CONSTRAINED
                           | EXPRFLAG.EXF_NEWSTRUCTASSG | EXPRFLAG.EXF_IMPLICITSTRUCTASSG | EXPRFLAG.EXF_MASK_ANY)) == 0);
            Flags = flags;
            OptionalArguments = arguments;
            MemberGroup = member;
            NullableCallLiftKind = NullableCallLiftKind.NotLifted;
            MethWithInst = method;
        }

        public MethWithInst MethWithInst { get; set; }

        public PREDEFMETH PredefinedMethod { get; set; } = PREDEFMETH.PM_COUNT;

        public NullableCallLiftKind NullableCallLiftKind { get; set; }

        public Expr PConversions { get; set; }

        public Expr CastOfNonLiftedResultToLiftedType { get; set; }

        public override SymWithType GetSymWithType() => MethWithInst;
    }
}

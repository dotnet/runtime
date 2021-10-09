// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprCast : ExprWithType
    {
        public ExprCast(EXPRFLAG flags, CType type, Expr argument)
            : base(ExpressionKind.Cast, type)
        {
            Debug.Assert(argument != null);
            Debug.Assert((flags & ~(EXPRFLAG.EXF_CAST_ALL | EXPRFLAG.EXF_MASK_ANY)) == 0);
            Argument = argument;
            Flags = flags;
        }

        public Expr Argument { get; set; }

        public bool IsBoxingCast => (Flags & (EXPRFLAG.EXF_BOX | EXPRFLAG.EXF_FORCE_BOX)) != 0;

        public override object Object
        {
            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            get
            {
                Expr arg = Argument;
                while (arg is ExprCast castArg)
                {
                    arg = castArg.Argument;
                }

                return arg.Object;
            }
        }
    }
}

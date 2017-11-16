// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprCast : Expr
    {
        private ExprClass _destinationType;

        public ExprCast(EXPRFLAG flags, ExprClass destinationType, Expr argument)
            : base(ExpressionKind.Cast)
        {
            Debug.Assert(argument != null);
            Debug.Assert(destinationType != null);
            Debug.Assert((flags & ~(EXPRFLAG.EXF_CAST_ALL | EXPRFLAG.EXF_MASK_ANY)) == 0);
            Argument = argument;
            Flags = flags;
            DestinationType = destinationType;
        }

        public Expr Argument { get; set; }

        public ExprClass DestinationType
        {
            get => _destinationType;
            set => Type = (_destinationType = value).Type;
        }

        public bool IsBoxingCast => (Flags & (EXPRFLAG.EXF_BOX | EXPRFLAG.EXF_FORCE_BOX)) != 0;
    }
}

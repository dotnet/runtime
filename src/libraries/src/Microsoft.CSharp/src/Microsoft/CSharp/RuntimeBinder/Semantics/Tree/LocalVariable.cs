// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprLocal : Expr
    {
        public ExprLocal(LocalVariableSymbol local)
            : base(ExpressionKind.Local)
        {
            Flags = EXPRFLAG.EXF_LVALUE;
            Local = local;
        }

        public LocalVariableSymbol Local { get; }

        public override CType Type => Local?.GetType();
    }
}

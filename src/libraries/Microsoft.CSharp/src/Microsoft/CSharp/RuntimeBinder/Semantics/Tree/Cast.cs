// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprCast : Expr
    {
        public Expr Argument { get; set; }

        public ExprTypeOrNamespace DestinationType { get; set; }

        public bool IsBoxingCast => (Flags & (EXPRFLAG.EXF_BOX | EXPRFLAG.EXF_FORCE_BOX)) != 0;
    }
}

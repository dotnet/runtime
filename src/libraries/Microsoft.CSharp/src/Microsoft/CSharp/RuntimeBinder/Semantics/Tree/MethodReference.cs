// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprFuncPtr : ExprWithType, IExprWithObject
    {
        public ExprFuncPtr(CType type, EXPRFLAG flags, Expr optionalObject, MethWithInst method) 
            : base(ExpressionKind.FunctionPointer, type)
        {
            Debug.Assert(flags == 0);
            Flags = flags;
            OptionalObject = optionalObject;
            MethWithInst = new MethWithInst(method);
        }

        public MethWithInst MethWithInst { get; }

        public Expr OptionalObject { get; set; }
    }
}

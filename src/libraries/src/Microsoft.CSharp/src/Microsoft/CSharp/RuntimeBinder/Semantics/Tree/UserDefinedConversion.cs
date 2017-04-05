// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprUserDefinedConversion : Expr
    {
        public ExprUserDefinedConversion()
            : base(ExpressionKind.UserDefinedConversion)
        {
            
        }
        public Expr Argument { get; set; }

        public Expr UserDefinedCall { get; set; }

        public override CType Type => UserDefinedCall.Type;

        public MethWithInst UserDefinedCallMethod { get; set; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprUserDefinedConversion : Expr
    {
        private Expr _userDefinedCall;

        public ExprUserDefinedConversion(Expr argument, Expr call, MethWithInst method)
            : base(ExpressionKind.UserDefinedConversion)
        {
            Debug.Assert(argument != null);
            Debug.Assert(call != null);
            Argument = argument;
            UserDefinedCall = call;
            UserDefinedCallMethod = method;
        }

        public Expr Argument { get; set; }

        public Expr UserDefinedCall
        {
            get => _userDefinedCall;
            set => Type = (_userDefinedCall = value).Type;
        }

        public MethWithInst UserDefinedCallMethod { get; }
    }
}

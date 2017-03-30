// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprBoundLambda : Expr
    {
        private Scope _argumentScope;

        public ExprBlock OptionalBody { get; set; }

        public AggregateType DelegateType => Type as AggregateType;

        // The scope containing the names of the parameters
        // The scope that will hold this anonymous function. This starts off as the outer scope and is then
        // ratcheted down to the correct scope after the containing method is fully bound.
        public Scope ArgumentScope
        {
            get
            {
                Debug.Assert(_argumentScope != null);
                return _argumentScope;
            }
            set
            {
                Debug.Assert(value != null);
                _argumentScope = value;
            }
        }
    }
}

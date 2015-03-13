// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRBOUNDLAMBDA : EXPR
    {
        public EXPRBLOCK OptionalBody;
        private Scope _argumentScope;            // The scope containing the names of the parameters
        // The scope that will hold this anonymous function. This starts off as the outer scope and is then
        // ratcheted down to the correct scope after the containing method is fully bound.

        public void Initialize(Scope argScope)
        {
            Debug.Assert(argScope != null);
            _argumentScope = argScope;
        }

        public AggregateType DelegateType()
        {
            return type.AsAggregateType();
        }

        public Scope ArgumentScope()
        {
            Debug.Assert(_argumentScope != null);
            return _argumentScope;
        }
    }
}

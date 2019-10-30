// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class LocalVariableSymbol : VariableSymbol
    {
        // To do expression tree rewriting we need to keep a map between a
        // local in an expression tree and the result of a ParameterExpression
        // creation.  We really ought to build a table to do the mapping in the
        // rewriter, but in the interests of expediency I've just put the mapping here
        // for now.

        public ExprWrap wrap;

        public void SetType(CType pType)
        {
            type = pType;
        }

        public new CType GetType()
        {
            return type;
        }
    }
}

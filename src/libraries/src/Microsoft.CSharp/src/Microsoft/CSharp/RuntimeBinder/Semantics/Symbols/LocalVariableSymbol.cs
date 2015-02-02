// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class LocalVariableSymbol : VariableSymbol
    {
        // To do expression tree rewriting we need to keep a map between a
        // local in an expression tree and the result of a ParameterExpression
        // creation.  We really ought to build a table to do the mapping in the
        // rewriter, but in the interests of expediency I've just put the mapping here
        // for now.

        public EXPRWRAP wrap;

        public bool isThis;           // Is this the one and only <this> pointer?
        // movedToField should have iIteratorLocal set appropriately
        public bool fUsedInAnonMeth;   // Set if the local is ever used in an anon method

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

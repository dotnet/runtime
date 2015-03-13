// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /*************************************************************************************************
        This wraps the type arguments for a class. It contains the TypeArray which is
        associated with the AggregateType for the instantiation of the class. 
    *************************************************************************************************/

    internal class EXPRTYPEARGUMENTS : EXPR
    {
        public EXPR OptionalElements;
        public EXPR GetOptionalElements() { return OptionalElements; }
        public void SetOptionalElements(EXPR value) { OptionalElements = value; }
    }
}

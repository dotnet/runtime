// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /*************************************************************************************************
        This wraps the type arguments for a class. It contains the TypeArray which is
        associated with the AggregateType for the instantiation of the class. 
    *************************************************************************************************/

    internal sealed class EXPRTYPEARGUMENTS : EXPR
    {
        private EXPR OptionalElements;
        public EXPR GetOptionalElements() { return OptionalElements; }
        public void SetOptionalElements(EXPR value) { OptionalElements = value; }
    }
}

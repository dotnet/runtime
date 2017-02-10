// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class EXPRARRINIT : EXPR
    {
        private EXPR _OptionalArguments;
        public EXPR GetOptionalArguments() { return _OptionalArguments; }
        public void SetOptionalArguments(EXPR value) { _OptionalArguments = value; }

        private EXPR _OptionalArgumentDimensions;
        public EXPR GetOptionalArgumentDimensions() { return _OptionalArgumentDimensions; }
        public void SetOptionalArgumentDimensions(EXPR value) { _OptionalArgumentDimensions = value; }

        // The EXPRs bound as the size of the array.
        public int[] dimSizes;
        public int dimSize;
        public bool GeneratedForParamArray;
    }
}

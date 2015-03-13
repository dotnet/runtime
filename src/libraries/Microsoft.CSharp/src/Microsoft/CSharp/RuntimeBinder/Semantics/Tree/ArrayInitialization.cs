// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRARRINIT : EXPR
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

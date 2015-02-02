// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRFUNCPTR : EXPR
    {
        public MethWithInst mwi;
        public EXPR OptionalObject;
        public void SetOptionalObject(EXPR value) { OptionalObject = value; }
    }
}

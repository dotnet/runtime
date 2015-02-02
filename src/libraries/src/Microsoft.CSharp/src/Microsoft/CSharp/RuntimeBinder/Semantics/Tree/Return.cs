// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//

//

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRRETURN : EXPRSTMT
    {
        // Return object is optional because of void returns.
        public EXPR OptionalObject;
        public EXPR GetOptionalObject() { return OptionalObject; }
        public void SetOptionalObject(EXPR value) { OptionalObject = value; }
    }
}

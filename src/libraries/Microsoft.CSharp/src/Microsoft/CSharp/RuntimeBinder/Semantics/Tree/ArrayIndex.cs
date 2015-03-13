// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRARRAYINDEX : EXPR
    {
        private EXPR _Array;
        public EXPR GetArray() { return _Array; }
        public void SetArray(EXPR value) { _Array = value; }

        private EXPR _Index;
        public EXPR GetIndex() { return _Index; }
        public void SetIndex(EXPR value) { _Index = value; }
    }
}

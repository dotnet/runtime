// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class EXPRARRAYINDEX : EXPR
    {
        private EXPR _Array;
        public EXPR GetArray() { return _Array; }
        public void SetArray(EXPR value) { _Array = value; }

        private EXPR _Index;
        public EXPR GetIndex() { return _Index; }
        public void SetIndex(EXPR value) { _Index = value; }
    }
}

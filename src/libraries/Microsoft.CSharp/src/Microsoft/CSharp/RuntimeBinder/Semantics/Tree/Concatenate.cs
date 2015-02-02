// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRCONCAT : EXPR
    {
        public EXPR FirstArgument;
        public EXPR GetFirstArgument() { return FirstArgument; }
        public void SetFirstArgument(EXPR value) { FirstArgument = value; }
        public EXPR SecondArgument;
        public EXPR GetSecondArgument() { return SecondArgument; }
        public void SetSecondArgument(EXPR value) { SecondArgument = value; }
    }
}

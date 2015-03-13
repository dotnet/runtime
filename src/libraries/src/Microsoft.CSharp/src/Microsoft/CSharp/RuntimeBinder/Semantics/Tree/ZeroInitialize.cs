// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRZEROINIT : EXPR
    {
        public EXPR OptionalArgument;
        public EXPR OptionalConstructorCall;
        public bool IsConstructor;
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRNamedArgumentSpecification : EXPR
    {
        public Microsoft.CSharp.RuntimeBinder.Syntax.Name Name;
        public EXPR Value;
    }
}

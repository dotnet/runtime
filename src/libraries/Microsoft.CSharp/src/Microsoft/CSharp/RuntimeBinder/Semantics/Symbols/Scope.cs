// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class Scope : ParentSymbol
    {
        public uint nestingOrder;  // the nesting order of this scopes. outermost == 0
    }
}

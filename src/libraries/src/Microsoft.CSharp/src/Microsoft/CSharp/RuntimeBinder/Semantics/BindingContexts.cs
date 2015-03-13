// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    // This file contains classes that create a new statement binding context
    // from the current one, but push on some new state.
    // ----------------------------------------------------------------------------

    internal class CheckedContext : BindingContext
    {
        public static CheckedContext CreateInstance(
            BindingContext parentCtx,
            bool checkedNormal,
            bool checkedConstant)
        {
            return new CheckedContext(parentCtx, checkedNormal, checkedConstant);
        }

        protected CheckedContext(
                BindingContext parentCtx,
                bool checkedNormal,
                bool checkedConstant
                )
            : base(parentCtx)
        {
            CheckedConstant = checkedConstant;
            CheckedNormal = checkedNormal;
        }
    }
}


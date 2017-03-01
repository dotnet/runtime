// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    // This file contains classes that create a new statement binding context
    // from the current one, but push on some new state.
    // ----------------------------------------------------------------------------

    internal sealed class CheckedContext : BindingContext
    {
        public static CheckedContext CreateInstance(
            BindingContext parentCtx,
            bool checkedNormal,
            bool checkedConstant)
        {
            return new CheckedContext(parentCtx, checkedNormal, checkedConstant);
        }

        private CheckedContext(
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


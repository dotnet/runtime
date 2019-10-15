// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    // This is the interface for the BindingContext, which is
    // consumed by the StatementBinder.
    // ----------------------------------------------------------------------------

    internal readonly struct BindingContext
    {
        public BindingContext(AggregateSymbol context, bool isChecked)
        {
            ContextForMemberLookup = context;
            Checked = isChecked;
        }

        public BindingContext(BindingContext parent)
        {
            // We copy the context object, but leave checking false.
            ContextForMemberLookup = parent.ContextForMemberLookup;
            Checked = false;
        }

        public AggregateSymbol ContextForMemberLookup { get; }

        public bool Checked { get; }
    }
}

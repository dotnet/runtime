// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // UnresolvedAggregateSymbol
    //
    // A fabricated AggregateSymbol to represent an imported type that we couldn't resolve.
    // Used for error reporting.
    // In the EE this is used as a place holder until the real AggregateSymbol is created.
    //
    // ----------------------------------------------------------------------------

    internal class UnresolvedAggregateSymbol : AggregateSymbol
    {
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    internal sealed class UnresolvedAggregateSymbol : AggregateSymbol
    {
    }
}

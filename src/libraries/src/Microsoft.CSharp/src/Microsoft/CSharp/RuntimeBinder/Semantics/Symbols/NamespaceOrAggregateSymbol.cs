// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // NamespaceOrAggregateSymbol
    //
    // Base class for NamespaceSymbol and AggregateSymbol. Bags have DECLSYMs.
    // Parent is another BAG. Children are other BAGs, members, type vars, etc.
    // ----------------------------------------------------------------------------

    internal abstract class NamespaceOrAggregateSymbol : ParentSymbol
    {
    }
}

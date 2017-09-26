// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // AggregateDeclaration
    //
    // AggregateDeclaration - represents a declaration of an aggregate type. With partial classes,
    // an aggregate type might be declared in multiple places.  This symbol represents
    // on of the declarations.
    //
    // parent is the containing Declaration.
    // ----------------------------------------------------------------------------

    // Either a ClassNode or a DelegateNode
    internal sealed class AggregateDeclaration : ParentSymbol
    {
        public NamespaceOrAggregateSymbol bag;

        public AggregateDeclaration declNext;

        public AggregateSymbol Agg()
        {
            return bag as AggregateSymbol;
        }

        public Assembly GetAssembly()
        {
            return Agg().AssociatedAssembly;
        }
    }
}

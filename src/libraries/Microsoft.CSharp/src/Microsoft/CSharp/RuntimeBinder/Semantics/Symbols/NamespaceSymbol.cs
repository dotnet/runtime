// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // NamespaceSymbol
    //
    // Namespaces, Namespace Declarations, and their members.
    //
    //
    // The parent, child, nextChild relationships are overloaded for namespaces.
    // The cause of all of this is that a namespace can be declared in multiple
    // places. This would not be a problem except that the using clauses(which
    // effect symbol lookup) are related to the namespace declaration not the
    // namespace itself. The result is that each namespace needs lists of all of
    // its declarations, and its members. Each namespace declaration needs a list
    // the declarations and types declared within it. Each member of a namespace
    // needs to access both the namespace it is contained in and the namespace
    // declaration it is contained in.
    //
    //
    // NamespaceSymbol - a symbol representing a name space. 
    // parent is the containing namespace.
    // ----------------------------------------------------------------------------

    internal sealed class NamespaceSymbol : NamespaceOrAggregateSymbol
    {
        /// <summary>The "root" (unnamed) namespace.</summary>
        public static readonly NamespaceSymbol Root = GetRootNamespaceSymbol();

        private static NamespaceSymbol GetRootNamespaceSymbol()
        {
            NamespaceSymbol root = new NamespaceSymbol
            {
                name = NameManager.GetPredefinedName(PredefinedName.PN_VOID)
            };

            root.setKind(SYMKIND.SK_NamespaceSymbol);
            return root;
        }
    }
}

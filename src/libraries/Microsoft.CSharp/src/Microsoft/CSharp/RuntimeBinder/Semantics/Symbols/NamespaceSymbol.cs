// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

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
        // Which assemblies and extern aliases contain this namespace.
        private HashSet<KAID> _bsetFilter;

        public NamespaceSymbol()
        {
            _bsetFilter = new HashSet<KAID>();
        }

        public bool InAlias(KAID aid)
        {
            Debug.Assert(0 <= aid);
            return _bsetFilter.Contains(aid);
        }

        public void DeclAdded(NamespaceDeclaration decl)
        {
            Debug.Assert(decl.Bag() == this);
            //Debug.Assert(this.pdeclAttach == &decl.declNext);

            InputFile infile = decl.getInputFile();

            if (infile.isSource)
            {
                _bsetFilter.Add(KAID.kaidGlobal);
                _bsetFilter.Add(KAID.kaidThisAssembly);
            }
            else
            {
                infile.UnionAliasFilter(ref _bsetFilter);
            }
        }

        public void AddAid(KAID aid)
        {
            if (aid == KAID.kaidThisAssembly)
            {
                _bsetFilter.Add(KAID.kaidGlobal);
            }
            _bsetFilter.Add(aid);
        }
    }
}

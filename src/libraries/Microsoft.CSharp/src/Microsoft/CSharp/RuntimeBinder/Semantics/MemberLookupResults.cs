// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    // This class encapsulates the results of member lookup, allowing the consumers
    // to get at the inaccessible symbols, bogus symbols, and validly bound symbols.
    // ----------------------------------------------------------------------------

    internal partial class CMemberLookupResults
    {
        private TypeArray ContainingTypes { get; }// Types that contain the member we're looking for.

        private readonly Name _pName; // The name that we're looking for.

        public CMemberLookupResults()
        {
            _pName = null;
            ContainingTypes = null;
        }

        public CMemberLookupResults(
                TypeArray containingTypes,
                Name name)
        {
            _pName = name;
            ContainingTypes = containingTypes;
            if (ContainingTypes == null)
            {
                ContainingTypes = BSYMMGR.EmptyTypeArray();
            }
        }

        public CMethodIterator GetMethodIterator(
            CSemanticChecker pChecker, SymbolLoader pSymLoader, CType pObject, CType pQualifyingType, Declaration pContext, bool allowBogusAndInaccessible, bool allowExtensionMethods, int arity, EXPRFLAG flags, symbmask_t mask)
        {
            Debug.Assert(pSymLoader != null);
            CMethodIterator iterator = new CMethodIterator(pChecker, pSymLoader, _pName, ContainingTypes, pObject, pQualifyingType, pContext, allowBogusAndInaccessible, allowExtensionMethods, arity, flags, mask);
            return iterator;
        }

        public partial class CMethodIterator
        {
        }
    }
}

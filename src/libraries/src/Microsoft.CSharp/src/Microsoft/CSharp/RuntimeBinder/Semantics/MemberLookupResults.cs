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

        public CMemberLookupResults(
                TypeArray containingTypes,
                Name name)
        {
            Debug.Assert(containingTypes != null);
            Debug.Assert(containingTypes.Count != 0);
            _pName = name;
            ContainingTypes = containingTypes;
        }

        public CMethodIterator GetMethodIterator(
            CSemanticChecker pChecker, SymbolLoader pSymLoader, CType pQualifyingType, AggregateDeclaration pContext, int arity, EXPRFLAG flags, symbmask_t mask, ArgInfos nonTrailingNamedArguments)
        {
            Debug.Assert(pSymLoader != null);
            CMethodIterator iterator = new CMethodIterator(pChecker, pSymLoader, _pName, ContainingTypes, pQualifyingType, pContext, arity, flags, mask, nonTrailingNamedArguments);
            return iterator;
        }

        public partial class CMethodIterator
        {
        }
    }
}

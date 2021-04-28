// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    // This class encapsulates the results of member lookup, allowing the consumers
    // to get at the inaccessible symbols, bogus symbols, and validly bound symbols.
    // ----------------------------------------------------------------------------

    internal sealed partial class CMemberLookupResults
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
            CType qualifyingType, AggregateSymbol context, int arity, EXPRFLAG flags, symbmask_t mask, ArgInfos nonTrailingNamedArguments) =>
            new CMethodIterator(_pName, ContainingTypes, qualifyingType, context, arity, flags, mask, nonTrailingNamedArguments);
    }
}

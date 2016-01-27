// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// QueryAggregationOptions.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

namespace System.Linq.Parallel
{
    /// <summary>
    /// An enum to specify whether an aggregate operator is associative, commutative,
    /// neither, or both. This influences query analysis and execution: associative
    /// aggregations can run in parallel, whereas non-associative cannot; non-commutative
    /// aggregations must be run over data in input-order. 
    /// </summary>
    [Flags]
    internal enum QueryAggregationOptions
    {
        None = 0,
        Associative = 1,
        Commutative = 2,
        AssociativeCommutative = (Associative | Commutative) // For convenience.
        // If you change the members, make sure you update IsDefinedQueryAggregationOptions() below.
    }

    internal static class QueryAggregationOptionsExtensions
    {
        // This helper is a workaround for the fact that Enum.Defined() does not work on non-public enums.
        // There is a custom attribute in System.Reflection.Metadata.Controls that would make it work
        // but we don't want to introduce a dependency on that contract just to support two asserts.
        public static bool IsValidQueryAggregationOption(this QueryAggregationOptions value)
        {
            return value == QueryAggregationOptions.None
                || value == QueryAggregationOptions.Associative
                || value == QueryAggregationOptions.Commutative
                || value == QueryAggregationOptions.AssociativeCommutative;
        }
    }
}

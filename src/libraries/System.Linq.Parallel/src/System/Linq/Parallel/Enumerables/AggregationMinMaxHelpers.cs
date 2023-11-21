// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// AggregationMinMaxHelpers.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Parallel;

namespace System.Linq
{
    internal static class AggregationMinMaxHelpers<T>
    {
        //-----------------------------------------------------------------------------------
        // Helper method to find the minimum or maximum element in the source.
        //

        private static T? Reduce(IEnumerable<T> source, int sign)
        {
            Debug.Assert(source != null);
            Debug.Assert(sign == -1 || sign == 1);

            Func<Pair<bool, T>, T, Pair<bool, T>> intermediateReduce = MakeIntermediateReduceFunction(sign);
            Func<Pair<bool, T>, Pair<bool, T>, Pair<bool, T>> finalReduce = MakeFinalReduceFunction(sign);
            Func<Pair<bool, T>, T> resultSelector = MakeResultSelectorFunction();

            AssociativeAggregationOperator<T, Pair<bool, T>, T> aggregation =
                new AssociativeAggregationOperator<T, Pair<bool, T>, T>(source, new Pair<bool, T>(false, default!), null,
                                                                        true, intermediateReduce, finalReduce, resultSelector, default(T) != null, QueryAggregationOptions.AssociativeCommutative);

            return aggregation.Aggregate();
        }

        //-----------------------------------------------------------------------------------
        // Helper method to find the minimum element in the source.
        //

        internal static T? ReduceMin(IEnumerable<T> source)
        {
            return Reduce(source, -1);
        }

        //-----------------------------------------------------------------------------------
        // Helper method to find the maximum element in the source.
        //

        internal static T? ReduceMax(IEnumerable<T> source)
        {
            return Reduce(source, 1);
        }

        //-----------------------------------------------------------------------------------
        // These methods are used to generate delegates to perform the comparisons.
        //

        private static Func<Pair<bool, T>, T, Pair<bool, T>> MakeIntermediateReduceFunction(int sign)
        {
            Comparer<T> comparer = Util.GetDefaultComparer<T>();

            // Note that we capture the 'sign' argument and 'comparer' local, and therefore the C#
            // compiler will transform this into an instance-based delegate, incurring an extra (hidden)
            // object allocation.
            return delegate (Pair<bool, T> accumulator, T element)
                       {
                           // If this is the first element, or the sign of the result of comparing the element with
                           // the existing accumulated result is equal to the sign requested by the function factory,
                           // we will return a new pair that contains the current element as the best item.  We will
                           // ignore null elements (for reference and nullable types) in the input stream.
                           if ((default(T) != null || element != null) &&
                               (!accumulator.First || Util.Sign(comparer.Compare(element, accumulator.Second)) == sign))
                           {
                               return new Pair<bool, T>(true, element);
                           }

                           // Otherwise, just return the current accumulator result.
                           return accumulator;
                       };
        }

        private static Func<Pair<bool, T>, Pair<bool, T>, Pair<bool, T>> MakeFinalReduceFunction(int sign)
        {
            Comparer<T> comparer = Util.GetDefaultComparer<T>();

            // Note that we capture the 'sign' argument and 'comparer' local, and therefore the C#
            // compiler will transform this into an instance-based delegate, incurring an extra (hidden)
            // object allocation.
            return delegate (Pair<bool, T> accumulator, Pair<bool, T> element)
                       {
                           // If the intermediate reduction is empty, we will ignore it. Otherwise, if this is the
                           // first element, or the sign of the result of comparing the element with the existing
                           // accumulated result is equal to the sign requested by the function factory, we will
                           // return a new pair that contains the current element as the best item.
                           if (element.First &&
                               (!accumulator.First || Util.Sign(comparer.Compare(element.Second, accumulator.Second)) == sign))
                           {
                               Debug.Assert(default(T) != null || element.Second != null, "nulls unexpected in final reduce");
                               return new Pair<bool, T>(true, element.Second);
                           }

                           // Otherwise, just return the current accumulator result.
                           return accumulator;
                       };
        }

        private static Func<Pair<bool, T>, T> MakeResultSelectorFunction()
        {
            // If we saw at least one element in the source stream, the right pair element will contain
            // the element we're looking for -- so we return that. In the case of non-nullable value
            // types, the aggregation API will have thrown an exception before calling us for
            // empty sequences.  Else, we will just return the element, which may be null for other types.
            return delegate (Pair<bool, T> accumulator)
                       {
                           Debug.Assert(accumulator.First || default(T) == null,
                                           "for non-null types we expect an exception to be thrown before getting here");
                           return accumulator.Second;
                       };
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ParallelEnumerable.cs
//
// The standard IEnumerable-based LINQ-to-Objects query provider. This class basically
// mirrors the System.Linq.Enumerable class, but (1) takes as input a special "parallel
// enumerable" data type and (2) uses an alternative implementation of the operators.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Linq.Parallel;
using System.Collections.Concurrent;
using System.Collections;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    //-----------------------------------------------------------------------------------
    // Languages like C# and VB that support query comprehensions translate queries
    // into calls to a query provider which creates executable representations of the
    // query. The LINQ-to-Objects provider is implemented as a static class with an
    // extension method per-query operator; when invoked, these return enumerable
    // objects that implement the querying behavior.
    //
    // We have a new sequence class for two reasons:
    //
    //     (1) Developers can opt in to parallel query execution piecemeal, by using
    //         a special AsParallel API to wrap the data source.
    //     (2) Parallel LINQ uses a new representation for queries when compared to LINQ,
    //         which we must return from the new sequence operator implementations.
    //
    // Comments and documentation will be somewhat light in this file. Please refer
    // to the "official" Standard Query Operators specification for details on each API:
    // http://download.microsoft.com/download/5/8/6/5868081c-68aa-40de-9a45-a3803d8134b8/Standard_Query_Operators.doc
    //
    // Notes:
    //     The Standard Query Operators herein should be semantically equivalent to
    //     the specification linked to above. In some cases, we offer operators that
    //     aren't available in the sequential LINQ library; in each case, we will note
    //     why this is needed.
    //

    /// <summary>
    /// Provides a set of methods for querying objects that implement
    /// ParallelQuery{TSource}.  This is the parallel equivalent of
    /// <see cref="System.Linq.Enumerable"/>.
    /// </summary>
    public static class ParallelEnumerable
    {
        // We pass this string constant to an attribute constructor. Unfortunately, we cannot access resources from
        // an attribute constructor, so we have to store this string in source code.
        private const string RIGHT_SOURCE_NOT_PARALLEL_STR =
            "The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than "
            + "System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method "
            + "to convert the right data source to System.Linq.ParallelQuery<T>.";

        //-----------------------------------------------------------------------------------
        // Converts any IEnumerable<TSource> into something that can be the target of parallel
        // query execution.
        //
        // Arguments:
        //     source              - the enumerable data source
        //     options             - query analysis options to override the defaults
        //     degreeOfParallelism - the DOP to use instead of the system default, if any
        //
        // Notes:
        //     If the argument is already a parallel enumerable, such as a query operator,
        //     no new objects are allocated. Otherwise, a very simple wrapper is instantiated
        //     that exposes the IEnumerable as a ParallelQuery.
        //

        /// <summary>
        /// Enables parallelization of a query.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}"/>
        /// to convert to a <see cref="System.Linq.ParallelQuery{T}"/>.</param>
        /// <returns>The source as a <see cref="System.Linq.ParallelQuery{T}"/> to bind to
        /// ParallelEnumerable extension methods.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> AsParallel<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ParallelEnumerableWrapper<TSource>(source);
        }

        /// <summary>
        /// Enables parallelization of a query, as sourced by a partitioner
        /// responsible for splitting the input sequence into partitions.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A partitioner over the input sequence.</param>
        /// <returns>The <paramref name="source"/> as a ParallelQuery to bind to ParallelEnumerable extension methods.</returns>
        /// <remarks>
        /// The source partitioner's GetOrderedPartitions method is used when ordering is enabled,
        /// whereas the partitioner's GetPartitions is used if ordering is not enabled (the default).
        /// The source partitioner's GetDynamicPartitions and GetDynamicOrderedPartitions are not used.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> AsParallel<TSource>(this Partitioner<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new PartitionerQueryOperator<TSource>(source);
        }


        /// <summary>
        /// Enables treatment of a data source as if it was ordered, overriding the default of unordered.
        /// AsOrdered may only be invoked on sequences returned by AsParallel, ParallelEnumerable.Range,
        /// and ParallelEnumerable.Repeat.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The input sequence.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if <paramref name="source"/> is not one of AsParallel, ParallelEnumerable.Range, or ParallelEnumerable.Repeat.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <remarks>
        /// A natural tension exists between performance and preserving order in parallel processing. By default,
        /// a parallelized query behaves as if the ordering of the results is arbitrary
        /// unless AsOrdered is applied or there is an explicit OrderBy operator in the query.
        /// </remarks>
        /// <returns>The source sequence which will maintain ordering in the query.</returns>
        public static ParallelQuery<TSource> AsOrdered<TSource>(this ParallelQuery<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (!(source is ParallelEnumerableWrapper<TSource> || source is IParallelPartitionable<TSource>))
            {
                if (source is PartitionerQueryOperator<TSource>  partitionerOp)
                {
                    if (!partitionerOp.Orderable)
                    {
                        throw new InvalidOperationException(SR.ParallelQuery_PartitionerNotOrderable);
                    }
                }
                else
                {
                    throw new InvalidOperationException(SR.ParallelQuery_InvalidAsOrderedCall);
                }
            }

            return new OrderingQueryOperator<TSource>(QueryOperator<TSource>.AsQueryOperator(source), true);
        }

        /// <summary>
        /// Enables treatment of a data source as if it was ordered, overriding the default of unordered.
        /// AsOrdered may only be invoked on sequences returned by AsParallel, ParallelEnumerable.Range,
        /// and ParallelEnumerable.Repeat.
        /// </summary>
        /// <param name="source">The input sequence.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <paramref name="source"/> is not one of AsParallel, ParallelEnumerable.Range, or ParallelEnumerable.Repeat.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <remarks>
        /// A natural tension exists between performance and preserving order in parallel processing. By default,
        /// a parallelized query behaves as if the ordering of the results is arbitrary unless AsOrdered
        /// is applied or there is an explicit OrderBy operator in the query.
        /// </remarks>
        /// <returns>The source sequence which will maintain ordering in the query.</returns>
        public static ParallelQuery AsOrdered(this ParallelQuery source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            ParallelEnumerableWrapper? wrapper = source as ParallelEnumerableWrapper;
            if (wrapper == null)
            {
                throw new InvalidOperationException(SR.ParallelQuery_InvalidNonGenericAsOrderedCall);
            }

            return new OrderingQueryOperator<object?>(QueryOperator<object?>.AsQueryOperator(wrapper), true);
        }

        /// <summary>
        /// Allows an intermediate query to be treated as if no ordering is implied among the elements.
        /// </summary>
        /// <remarks>
        /// AsUnordered may provide
        /// performance benefits when ordering is not required in a portion of a query.
        /// </remarks>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The input sequence.</param>
        /// <returns>The source sequence with arbitrary order.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> AsUnordered<TSource>(this ParallelQuery<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new OrderingQueryOperator<TSource>(QueryOperator<TSource>.AsQueryOperator(source), false);
        }

        /// <summary>
        /// Enables parallelization of a query.
        /// </summary>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}"/> to convert
        /// to a <see cref="System.Linq.ParallelQuery{T}"/>.</param>
        /// <returns>
        /// The source as a ParallelQuery to bind to
        /// ParallelEnumerable extension methods.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery AsParallel(this IEnumerable source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return new ParallelEnumerableWrapper(source);
        }


        //-----------------------------------------------------------------------------------
        // Converts a parallel enumerable into something that forces sequential execution.
        //
        // Arguments:
        //     source - the parallel enumerable data source
        //

        /// <summary>
        /// Converts a <see cref="ParallelQuery{T}"/> into an
        /// <see cref="System.Collections.Generic.IEnumerable{T}"/> to force sequential
        /// evaluation of the query.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A <see cref="ParallelQuery{T}"/> to convert to an <see cref="System.Collections.Generic.IEnumerable{T}"/>.</param>
        /// <returns>The source as an <see cref="System.Collections.Generic.IEnumerable{T}"/>
        /// to bind to sequential extension methods.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static IEnumerable<TSource> AsSequential<TSource>(this ParallelQuery<TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // Ditch the wrapper, if there is one.
            if (source is ParallelEnumerableWrapper<TSource> wrapper)
            {
                return wrapper.WrappedEnumerable;
            }
            else
            {
                return source;
            }
        }

        /// <summary>
        /// Sets the degree of parallelism to use in a query. Degree of parallelism is the maximum number of concurrently
        /// executing tasks that will be used to process the query.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A ParallelQuery on which to set the limit on the degrees of parallelism.</param>
        /// <param name="degreeOfParallelism">The degree of parallelism for the query.</param>
        /// <returns>ParallelQuery representing the same query as source, with the limit on the degrees of parallelism set.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// WithDegreeOfParallelism is used multiple times in the query.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="degreeOfParallelism"/> is less than 1 or greater than 512.
        /// </exception>
        public static ParallelQuery<TSource> WithDegreeOfParallelism<TSource>(this ParallelQuery<TSource> source, int degreeOfParallelism)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (degreeOfParallelism < 1 || degreeOfParallelism > Scheduling.MAX_SUPPORTED_DOP)
            {
                throw new ArgumentOutOfRangeException(nameof(degreeOfParallelism));
            }

            QuerySettings settings = QuerySettings.Empty;
            settings.DegreeOfParallelism = degreeOfParallelism;

            return new QueryExecutionOption<TSource>(
                QueryOperator<TSource>.AsQueryOperator(source), settings);
        }

        /// <summary>
        /// Sets the <see cref="System.Threading.CancellationToken"/> to associate with the query.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A ParallelQuery on which to set the option.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>ParallelQuery representing the same query as source, but with the <seealso cref="System.Threading.CancellationToken"/>
        /// registered.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// WithCancellation is used multiple times in the query.
        /// </exception>
        public static ParallelQuery<TSource> WithCancellation<TSource>(this ParallelQuery<TSource> source, CancellationToken cancellationToken)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            QuerySettings settings = QuerySettings.Empty;
            settings.CancellationState = new CancellationState(cancellationToken);

            return new QueryExecutionOption<TSource>(
                QueryOperator<TSource>.AsQueryOperator(source), settings);
        }

        /// <summary>
        /// Sets the execution mode of the query.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A ParallelQuery on which to set the option.</param>
        /// <param name="executionMode">The mode in which to execute the query.</param>
        /// <returns>ParallelQuery representing the same query as source, but with the
        /// <seealso cref="System.Linq.ParallelExecutionMode"/> registered.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// <paramref name="executionMode"/> is not a valid <see cref="System.Linq.ParallelExecutionMode"/> value.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// WithExecutionMode is used multiple times in the query.
        /// </exception>
        public static ParallelQuery<TSource> WithExecutionMode<TSource>(this ParallelQuery<TSource> source, ParallelExecutionMode executionMode)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (executionMode != ParallelExecutionMode.Default && executionMode != ParallelExecutionMode.ForceParallelism)
            {
                throw new ArgumentException(SR.ParallelEnumerable_WithQueryExecutionMode_InvalidMode);
            }

            QuerySettings settings = QuerySettings.Empty;
            settings.ExecutionMode = executionMode;

            return new QueryExecutionOption<TSource>(
                QueryOperator<TSource>.AsQueryOperator(source), settings);
        }

        /// <summary>
        /// Sets the merge options for this query, which specify how the query will buffer output.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A ParallelQuery on which to set the option.</param>
        /// <param name="mergeOptions">The merge options to set for this query.</param>
        /// <returns>ParallelQuery representing the same query as source, but with the
        /// <seealso cref="ParallelMergeOptions"/> registered.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// <paramref name="mergeOptions"/> is not a valid <see cref="System.Linq.ParallelMergeOptions"/> value.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// WithMergeOptions is used multiple times in the query.
        /// </exception>
        public static ParallelQuery<TSource> WithMergeOptions<TSource>(this ParallelQuery<TSource> source, ParallelMergeOptions mergeOptions)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (mergeOptions != ParallelMergeOptions.Default
                && mergeOptions != ParallelMergeOptions.AutoBuffered
                && mergeOptions != ParallelMergeOptions.NotBuffered
                && mergeOptions != ParallelMergeOptions.FullyBuffered)
            {
                throw new ArgumentException(SR.ParallelEnumerable_WithMergeOptions_InvalidOptions);
            }

            QuerySettings settings = QuerySettings.Empty;
            settings.MergeOptions = mergeOptions;

            return new QueryExecutionOption<TSource>(
                QueryOperator<TSource>.AsQueryOperator(source), settings);
        }

        //-----------------------------------------------------------------------------------
        // Range generates a sequence of numbers that can be used as input to a query.
        //

        /// <summary>
        /// Generates a parallel sequence of integral numbers within a specified range.
        /// </summary>
        /// <param name="start">The value of the first integer in the sequence.</param>
        /// <param name="count">The number of sequential integers to generate.</param>
        /// <returns>An <b>IEnumerable&lt;Int32&gt;</b> in C# or <B>IEnumerable(Of Int32)</B> in
        /// Visual Basic that contains a range of sequential integral numbers.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="count"/> is less than 0
        /// -or-
        /// <paramref name="start"/> + <paramref name="count"/> - 1 is larger than <see cref="int.MaxValue"/>.
        /// </exception>
        public static ParallelQuery<int> Range(int start, int count)
        {
            if (count < 0 || (count > 0 && int.MaxValue - (count - 1) < start)) throw new ArgumentOutOfRangeException(nameof(count));
            return new RangeEnumerable(start, count);
        }

        //-----------------------------------------------------------------------------------
        // Repeat just generates a sequence of size 'count' containing 'element'.
        //

        /// <summary>
        /// Generates a parallel sequence that contains one repeated value.
        /// </summary>
        /// <typeparam name="TResult">The type of the value to be repeated in the result sequence.</typeparam>
        /// <param name="element">The value to be repeated.</param>
        /// <param name="count">The number of times to repeat the value in the generated sequence.</param>
        /// <returns>A sequence that contains a repeated value.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="count"/> is less than 0.
        /// </exception>
        public static ParallelQuery<TResult> Repeat<TResult>(TResult element, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            return new RepeatEnumerable<TResult>(element, count);
        }

        //-----------------------------------------------------------------------------------
        // Returns an always-empty sequence.
        //

        /// <summary>
        /// Returns an empty ParallelQuery{TResult} that has the specified type argument.
        /// </summary>
        /// <typeparam name="TResult">The type to assign to the type parameter of the returned
        /// generic sequence.</typeparam>
        /// <returns>An empty sequence whose type argument is <typeparamref name="TResult"/>.</returns>
        public static ParallelQuery<TResult> Empty<TResult>()
        {
            return System.Linq.Parallel.EmptyEnumerable<TResult>.Instance;
        }

        //-----------------------------------------------------------------------------------
        // A new query operator that allows an arbitrary user-specified "action" to be
        // tacked on to the query tree. The action will be invoked for every element in the
        // underlying data source, avoiding a costly final merge in the query's execution,
        // which can lead to much better scalability. The caveat is that these occur in
        // parallel, so the user providing an action must take care to eliminate shared state
        // accesses or to synchronize as appropriate.
        //
        // Arguments:
        //     source  - the data source over which the actions will be invoked
        //     action  - a delegate representing the per-element action to be invoked
        //
        // Notes:
        //     Neither source nor action may be null, otherwise this method throws.
        //

        /// <summary>
        /// Invokes in parallel the specified action for each element in the <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// This is an efficient way to process the output from a parallelized query because it does
        /// not require a merge step at the end.  However, order of execution is non-deterministic.
        /// </remarks>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The <see cref="ParallelQuery{T}"/> whose elements will be processed by
        /// <paramref name="action"/>.</param>
        /// <param name="action">An Action to invoke on each element.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="action"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static void ForAll<TSource>(this ParallelQuery<TSource> source, Action<TSource> action)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));

            // We just instantiate the forall operator and invoke it synchronously on this thread.
            // By the time it returns, the entire query has been executed and the actions run..
            new ForAllOperator<TSource>(source, action).RunSynchronously();
        }



        /*===================================================================================
         * BASIC OPERATORS
         *===================================================================================*/

        //-----------------------------------------------------------------------------------
        // Where is an operator that filters any elements from the data source for which the
        // user-supplied predicate returns false.
        //

        /// <summary>
        /// Filters in parallel a sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A sequence that contains elements from the input sequence that satisfy
        /// the condition.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> Where<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return new WhereQueryOperator<TSource>(source, predicate);
        }

        /// <summary>
        /// Filters in parallel a sequence of values based on a predicate. Each element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> Where<TSource>(this ParallelQuery<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return new IndexedWhereQueryOperator<TSource>(source, predicate);
        }

        //-----------------------------------------------------------------------------------
        // Select merely maps a selector delegate over each element in the data source.
        //


        /// <summary>
        /// Projects in parallel each element of a sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of elements returned by <b>selector</b>.</typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>A sequence whose elements are the result of invoking the transform function on each
        /// element of <paramref name="source"/>.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> Select<TSource, TResult>(
            this ParallelQuery<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return new SelectQueryOperator<TSource, TResult>(source, selector);
        }

        /// <summary>
        /// Projects in parallel each element of a sequence into a new form by incorporating the element's index.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of elements returned by <b>selector</b>.</typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>A sequence whose elements are the result of invoking the transform function on each
        /// element of <paramref name="source"/>.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> Select<TSource, TResult>(
             this ParallelQuery<TSource> source, Func<TSource, int, TResult> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return new IndexedSelectQueryOperator<TSource, TResult>(source, selector);
        }

        //-----------------------------------------------------------------------------------
        // Zip combines an outer and inner data source into a single output data stream.
        //

        /// <summary>
        /// Merges in parallel two sequences by using the specified predicate function.
        /// </summary>
        /// <typeparam name="TFirst">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TSecond">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TResult">The type of the return elements.</typeparam>
        /// <param name="first">The first sequence to zip.</param>
        /// <param name="second">The second sequence to zip.</param>
        /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
        /// <returns>
        /// A sequence that has elements of type <typeparamref name="TResult"/> that are obtained by performing
        /// resultSelector pairwise on two sequences. If the sequence lengths are unequal, this truncates
        /// to the length of the shorter sequence.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="first"/> or <paramref name="second"/> or <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> Zip<TFirst, TSecond, TResult>(
            this ParallelQuery<TFirst> first, ParallelQuery<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return new ZipQueryOperator<TFirst, TSecond, TResult>(first, second, resultSelector);
        }

        /// <summary>
        /// This Zip overload should never be called.
        /// This method is marked as obsolete and always throws
        /// <see cref="System.NotSupportedException"/> when invoked.
        /// </summary>
        /// <typeparam name="TFirst">This type parameter is not used.</typeparam>
        /// <typeparam name="TSecond">This type parameter is not used.</typeparam>
        /// <typeparam name="TResult">This type parameter is not used.</typeparam>
        /// <param name="first">This parameter is not used.</param>
        /// <param name="second">This parameter is not used.</param>
        /// <param name="resultSelector">This parameter is not used.</param>
        /// <returns>This overload always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <exception cref="System.NotSupportedException">The exception that occurs when this method is called.</exception>
        /// <remarks>
        /// This overload exists to disallow usage of Zip with a left data source of type
        /// <see cref="System.Linq.ParallelQuery{TFirst}"/> and a right data source of type <see cref="System.Collections.Generic.IEnumerable{TSecond}"/>.
        /// Otherwise, the Zip operator would appear to be bind to the parallel implementation, but would in reality bind to the sequential implementation.
        /// </remarks>
        [Obsolete(RIGHT_SOURCE_NOT_PARALLEL_STR)]
        public static ParallelQuery<TResult> Zip<TFirst, TSecond, TResult>(
            this ParallelQuery<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
        {
            throw new NotSupportedException(SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
        }

        //-----------------------------------------------------------------------------------
        // Join is an inner join operator, i.e. elements from outer with no inner matches
        // will yield no results in the output data stream.
        //

        /// <summary>
        /// Correlates in parallel the elements of two sequences based on matching keys.
        /// The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of
        /// the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of
        /// the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
        /// <returns>A sequence that has elements of type <typeparamref name="TResult"/> that are obtained by performing
        /// an inner join on two sequences.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="outer"/> or <paramref name="inner"/> or <paramref name="outerKeySelector"/> or
        /// <paramref name="innerKeySelector"/> or <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> Join<TOuter, TInner, TKey, TResult>(
            this ParallelQuery<TOuter> outer, ParallelQuery<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
        {
            return Join<TOuter, TInner, TKey, TResult>(
                outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);
        }

        /// <summary>
        /// This Join overload should never be called.
        /// This method is marked as obsolete and always throws <see cref="System.NotSupportedException"/> when invoked.
        /// </summary>
        /// <typeparam name="TOuter">This type parameter is not used.</typeparam>
        /// <typeparam name="TInner">This type parameter is not used.</typeparam>
        /// <typeparam name="TKey">This type parameter is not used.</typeparam>
        /// <typeparam name="TResult">This type parameter is not used.</typeparam>
        /// <param name="outer">This parameter is not used.</param>
        /// <param name="inner">This parameter is not used.</param>
        /// <param name="outerKeySelector">This parameter is not used.</param>
        /// <param name="innerKeySelector">This parameter is not used.</param>
        /// <param name="resultSelector">This parameter is not used.</param>
        /// <returns>This overload always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <exception cref="System.NotSupportedException">The exception that occurs when this method is called.</exception>
        /// <remarks>
        /// This overload exists to disallow usage Join with a left data source of type
        /// <see cref="System.Linq.ParallelQuery{TOuter}"/> and a right data source of type <see cref="System.Collections.Generic.IEnumerable{TInner}"/>.
        /// Otherwise, the Join operator would appear to be binding to the parallel implementation, but would in reality bind to the sequential implementation.
        /// </remarks>
        [Obsolete(RIGHT_SOURCE_NOT_PARALLEL_STR)]
        public static ParallelQuery<TResult> Join<TOuter, TInner, TKey, TResult>(
            this ParallelQuery<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
        {
            throw new NotSupportedException(SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
        }

        /// <summary>
        /// Correlates in parallel the elements of two sequences based on matching keys.
        /// A specified IEqualityComparer{T} is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element
        /// of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element
        /// of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
        /// <param name="comparer">An IEqualityComparer&lt;(Of &lt;(T&gt;)&gt;) to hash and compare keys.</param>
        /// <returns>A sequence that has elements of type <typeparamref name="TResult"/> that are obtained by performing
        /// an inner join on two sequences.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="outer"/> or <paramref name="inner"/> or <paramref name="outerKeySelector"/> or
        /// <paramref name="innerKeySelector"/> or <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> Join<TOuter, TInner, TKey, TResult>(
            this ParallelQuery<TOuter> outer, ParallelQuery<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            if (outer == null) throw new ArgumentNullException(nameof(outer));
            if (inner == null) throw new ArgumentNullException(nameof(inner));
            if (outerKeySelector == null) throw new ArgumentNullException(nameof(outerKeySelector));
            if (innerKeySelector == null) throw new ArgumentNullException(nameof(innerKeySelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return new JoinQueryOperator<TOuter, TInner, TKey, TResult>(
                outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
        }

        /// <summary>
        /// This Join overload should never be called.
        /// This method is marked as obsolete and always throws <see cref="System.NotSupportedException"/> when invoked.
        /// </summary>
        /// <typeparam name="TOuter">This type parameter is not used.</typeparam>
        /// <typeparam name="TInner">This type parameter is not used.</typeparam>
        /// <typeparam name="TKey">This type parameter is not used.</typeparam>
        /// <typeparam name="TResult">This type parameter is not used.</typeparam>
        /// <param name="outer">This parameter is not used.</param>
        /// <param name="inner">This parameter is not used.</param>
        /// <param name="outerKeySelector">This parameter is not used.</param>
        /// <param name="innerKeySelector">This parameter is not used.</param>
        /// <param name="resultSelector">This parameter is not used.</param>
        /// <param name="comparer">This parameter is not used.</param>
        /// <returns>This overload always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <exception cref="System.NotSupportedException">The exception that occurs when this method is called.</exception>
        /// <remarks>
        /// This overload exists to disallow usage of Join with a left data source of type
        /// <see cref="System.Linq.ParallelQuery{TOuter}"/> and a right data source of type <see cref="System.Collections.Generic.IEnumerable{TInner}"/>.
        /// Otherwise, the Join operator would appear to be binding to the parallel implementation, but would in reality bind to the sequential implementation.
        /// </remarks>
        [Obsolete(RIGHT_SOURCE_NOT_PARALLEL_STR)]
        public static ParallelQuery<TResult> Join<TOuter, TInner, TKey, TResult>(
            this ParallelQuery<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            throw new NotSupportedException(SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
        }

        //-----------------------------------------------------------------------------------
        // GroupJoin is an outer join operator, i.e. elements from outer with no inner matches
        // will yield results (empty lists) in the output data stream.
        //

        /// <summary>
        /// Correlates in parallel the elements of two sequences based on equality of keys and groups the results.
        /// The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element
        /// of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element
        /// of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from an element from
        /// the first sequence and a collection of matching elements from the second sequence.</param>
        /// <returns>A sequence that has elements of type <typeparamref name="TResult"/> that are obtained by performing
        /// a grouped join on two sequences.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="outer"/> or <paramref name="inner"/> or <paramref name="outerKeySelector"/> or
        /// <paramref name="innerKeySelector"/> or <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
            this ParallelQuery<TOuter> outer, ParallelQuery<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, TResult> resultSelector)
        {
            return GroupJoin<TOuter, TInner, TKey, TResult>(
                outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);
        }

        /// <summary>
        /// This GroupJoin overload should never be called.
        /// This method is marked as obsolete and always throws <see cref="System.NotSupportedException"/> when called.
        /// </summary>
        /// <typeparam name="TOuter">This type parameter is not used.</typeparam>
        /// <typeparam name="TInner">This type parameter is not used.</typeparam>
        /// <typeparam name="TKey">This type parameter is not used.</typeparam>
        /// <typeparam name="TResult">This type parameter is not used.</typeparam>
        /// <param name="outer">This parameter is not used.</param>
        /// <param name="inner">This parameter is not used.</param>
        /// <param name="outerKeySelector">This parameter is not used.</param>
        /// <param name="innerKeySelector">This parameter is not used.</param>
        /// <param name="resultSelector">This parameter is not used.</param>
        /// <returns>This overload always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <exception cref="System.NotSupportedException">The exception that occurs when this method is called.</exception>
        /// <remarks>
        /// This overload exists to disallow usage of GroupJoin with a left data source of type
        /// <see cref="System.Linq.ParallelQuery{TOuter}"/> and a right data source of type <see cref="System.Collections.Generic.IEnumerable{TInner}"/>.
        /// Otherwise, the GroupJoin operator would appear to be binding to the parallel implementation,
        /// but would in reality bind to the sequential implementation.
        ///</remarks>
        [Obsolete(RIGHT_SOURCE_NOT_PARALLEL_STR)]
        public static ParallelQuery<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
            this ParallelQuery<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, TResult> resultSelector)
        {
            throw new NotSupportedException(SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
        }

        /// <summary>
        /// Correlates in parallel the elements of two sequences based on key equality and groups the results.
        /// A specified IEqualityComparer{T} is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element
        /// of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element
        /// of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from an element from
        /// the first sequence and a collection of matching elements from the second sequence.</param>
        /// <param name="comparer">An IEqualityComparer&lt;(Of &lt;(T&gt;)&gt;) to hash and compare keys.</param>
        /// <returns>A sequence that has elements of type <typeparamref name="TResult"/> that are obtained by performing
        /// a grouped join on two sequences.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="outer"/> or <paramref name="inner"/> or <paramref name="outerKeySelector"/> or
        /// <paramref name="innerKeySelector"/> or <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
            this ParallelQuery<TOuter> outer, ParallelQuery<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            if (outer == null) throw new ArgumentNullException(nameof(outer));
            if (inner == null) throw new ArgumentNullException(nameof(inner));
            if (outerKeySelector == null) throw new ArgumentNullException(nameof(outerKeySelector));
            if (innerKeySelector == null) throw new ArgumentNullException(nameof(innerKeySelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return new GroupJoinQueryOperator<TOuter, TInner, TKey, TResult>(outer, inner,
                outerKeySelector, innerKeySelector, resultSelector, comparer);
        }

        /// <summary>
        /// This GroupJoin overload should never be called.
        /// This method is marked as obsolete and always throws <see cref="System.NotSupportedException"/> when called.
        /// </summary>
        /// <typeparam name="TOuter">This type parameter is not used.</typeparam>
        /// <typeparam name="TInner">This type parameter is not used.</typeparam>
        /// <typeparam name="TKey">This type parameter is not used.</typeparam>
        /// <typeparam name="TResult">This type parameter is not used.</typeparam>
        /// <param name="outer">This parameter is not used.</param>
        /// <param name="inner">This parameter is not used.</param>
        /// <param name="outerKeySelector">This parameter is not used.</param>
        /// <param name="innerKeySelector">This parameter is not used.</param>
        /// <param name="resultSelector">This parameter is not used.</param>
        /// <param name="comparer">This parameter is not used.</param>
        /// <returns>This overload always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <exception cref="System.NotSupportedException">The exception that occurs when this method is called.</exception>
        /// <remarks>
        /// This overload exists to disallow usage of GroupJoin with a left data source of type
        /// <see cref="System.Linq.ParallelQuery{TOuter}"/> and a right data source of type <see cref="System.Collections.Generic.IEnumerable{TInner}"/>.
        /// Otherwise, the GroupJoin operator would appear to be binding to the parallel implementation,
        /// but would in reality bind to the sequential implementation.
        /// </remarks>
        [Obsolete(RIGHT_SOURCE_NOT_PARALLEL_STR)]
        public static ParallelQuery<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
            this ParallelQuery<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            throw new NotSupportedException(SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
        }

        //-----------------------------------------------------------------------------------
        // SelectMany is a kind of nested loops join. For each element in the outer data
        // source, we enumerate each element in the inner data source, yielding the result
        // with some kind of selection routine. A few different flavors are supported.
        //

        /// <summary>
        /// Projects in parallel each element of a sequence to an IEnumerable{T}
        /// and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by <B>selector</B>.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>A sequence whose elements are the result of invoking the one-to-many transform
        /// function on each element of the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> SelectMany<TSource, TResult>(
            this ParallelQuery<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return new SelectManyQueryOperator<TSource, TResult, TResult>(source, selector, null, null);
        }

        /// <summary>
        /// Projects in parallel each element of a sequence to an IEnumerable{T}, and flattens the resulting
        /// sequences into one sequence. The index of each source element is used in the projected form of
        /// that element.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by <B>selector</B>.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>A sequence whose elements are the result of invoking the one-to-many transform
        /// function on each element of the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> SelectMany<TSource, TResult>(
             this ParallelQuery<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return new SelectManyQueryOperator<TSource, TResult, TResult>(source, null, selector, null);
        }

        /// <summary>
        /// Projects each element of a sequence to an IEnumerable{T},
        /// flattens the resulting sequences into one sequence, and invokes a result selector
        /// function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="collectionSelector">A transform function to apply to each source element;
        /// the second parameter of the function represents the index of the source element.</param>
        /// <param name="resultSelector">A function to create a result element from an element from
        /// the first sequence and a collection of matching elements from the second sequence.</param>
        /// <returns>A sequence whose elements are the result of invoking the one-to-many transform
        /// function <paramref name="collectionSelector"/> on each element of <paramref name="source"/> and then mapping
        /// each of those sequence elements and their corresponding source element to a result element.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="collectionSelector"/> or
        /// <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> SelectMany<TSource, TCollection, TResult>(
            this ParallelQuery<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (collectionSelector == null) throw new ArgumentNullException(nameof(collectionSelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return new SelectManyQueryOperator<TSource, TCollection, TResult>(source, collectionSelector, null, resultSelector);
        }

        /// <summary>
        /// Projects each element of a sequence to an IEnumerable{T}, flattens the resulting
        /// sequences into one sequence, and invokes a result selector function on each element
        /// therein. The index of each source element is used in the intermediate projected
        /// form of that element.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by
        /// <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of elements to return.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="collectionSelector">A transform function to apply to each source element;
        /// the second parameter of the function represents the index of the source element.</param>
        /// <param name="resultSelector">A function to create a result element from an element from
        /// the first sequence and a collection of matching elements from the second sequence.</param>
        /// <returns>
        /// A sequence whose elements are the result of invoking the one-to-many transform
        /// function <paramref name="collectionSelector"/> on each element of <paramref name="source"/> and then mapping
        /// each of those sequence elements and their corresponding source element to a
        /// result element.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="collectionSelector"/> or
        /// <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> SelectMany<TSource, TCollection, TResult>(
            this ParallelQuery<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (collectionSelector == null) throw new ArgumentNullException(nameof(collectionSelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return new SelectManyQueryOperator<TSource, TCollection, TResult>(source, null, collectionSelector, resultSelector);
        }

        //-----------------------------------------------------------------------------------
        // OrderBy and ThenBy establish an ordering among elements, using user-specified key
        // selection and key comparison routines. There are also descending sort variants.
        //

        /// <summary>
        /// Sorts in parallel the elements of a sequence in ascending order according to a key.
        /// </summary>
        /// <remarks>
        /// In contrast to the sequential implementation, this is not a stable sort.
        /// To achieve a stable sort, change a query of the form:
        /// <code>var ordered = source.OrderBy((e) => e.k);</code>
        /// to instead be formed as:
        /// <code>var ordered = source.Select((e,i) => new { E=e, I=i }).OrderBy((v) => v.i).Select((v) => v.e);</code>
        /// </remarks>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An OrderedParallelQuery{TSource} whose elements are sorted
        /// according to a key.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static OrderedParallelQuery<TSource> OrderBy<TSource, TKey>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            return new OrderedParallelQuery<TSource>(
                new SortQueryOperator<TSource, TKey>(source, keySelector, null, false));
        }

        /// <summary>
        /// Sorts in parallel the elements of a sequence in ascending order by using a specified comparer.
        /// </summary>
        /// <remarks>
        /// In contrast to the sequential implementation, this is not a stable sort.
        /// See the remarks for OrderBy(ParallelQuery{TSource}, Func{TSource,TKey}) for
        /// an approach to implementing a stable sort.
        /// </remarks>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">An IComparer{TKey} to compare keys.</param>
        /// <returns>An OrderedParallelQuery{TSource} whose elements are sorted according
        /// to a key.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static OrderedParallelQuery<TSource> OrderBy<TSource, TKey>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            return new OrderedParallelQuery<TSource>(
                new SortQueryOperator<TSource, TKey>(source, keySelector, comparer, false));
        }

        /// <summary>
        /// Sorts in parallel the elements of a sequence in descending order according to a key.
        /// </summary>
        /// <remarks>
        /// In contrast to the sequential implementation, this is not a stable sort.
        /// See the remarks for OrderBy(ParallelQuery{TSource}, Func{TSource,TKey}) for
        /// an approach to implementing a stable sort.
        /// </remarks>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An OrderedParallelQuery{TSource} whose elements are sorted
        /// descending according to a key.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static OrderedParallelQuery<TSource> OrderByDescending<TSource, TKey>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            return new OrderedParallelQuery<TSource>(new SortQueryOperator<TSource, TKey>(source, keySelector, null, true));
        }

        /// <summary>
        /// Sorts the elements of a sequence in descending order by using a specified comparer.
        /// </summary>
        /// <remarks>
        /// In contrast to the sequential implementation, this is not a stable sort.
        /// See the remarks for OrderBy(ParallelQuery{TSource}, Func{TSource,TKey}) for
        /// an approach to implementing a stable sort.
        /// </remarks>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">An IComparer{TKey} to compare keys.</param>
        /// <returns>An OrderedParallelQuery{TSource} whose elements are sorted descending
        /// according to a key.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static OrderedParallelQuery<TSource> OrderByDescending<TSource, TKey>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            return new OrderedParallelQuery<TSource>(
                new SortQueryOperator<TSource, TKey>(source, keySelector, comparer, true));
        }

        /// <summary>
        /// Performs in parallel a subsequent ordering of the elements in a sequence
        /// in ascending order according to a key.
        /// </summary>
        /// <remarks>
        /// In contrast to the sequential implementation, this is not a stable sort.
        /// See the remarks for OrderBy(ParallelQuery{TSource}, Func{TSource,TKey}) for
        /// an approach to implementing a stable sort.
        /// </remarks>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An OrderedParallelQuery{TSource} than
        /// contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An OrderedParallelQuery{TSource} whose elements are
        /// sorted according to a key.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>

        public static OrderedParallelQuery<TSource> ThenBy<TSource, TKey>(
            this OrderedParallelQuery<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            return new OrderedParallelQuery<TSource>(
                (QueryOperator<TSource>)source.OrderedEnumerable.CreateOrderedEnumerable<TKey>(keySelector, null, false));
        }
        /// <summary>
        /// Performs in parallel a subsequent ordering of the elements in a sequence in
        /// ascending order by using a specified comparer.
        /// </summary>
        /// <remarks>
        /// In contrast to the sequential implementation, this is not a stable sort.
        /// See the remarks for OrderBy(ParallelQuery{TSource}, Func{TSource,TKey}) for
        /// an approach to implementing a stable sort.
        /// </remarks>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An OrderedParallelQuery{TSource} that contains
        /// elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">An IComparer{TKey} to compare keys.</param>
        /// <returns>An OrderedParallelQuery{TSource} whose elements are sorted
        /// according to a key.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        ///

        public static OrderedParallelQuery<TSource> ThenBy<TSource, TKey>(
            this OrderedParallelQuery<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            return new OrderedParallelQuery<TSource>(
                (QueryOperator<TSource>)source.OrderedEnumerable.CreateOrderedEnumerable<TKey>(keySelector, comparer, false));
        }
        /// <summary>
        /// Performs in parallel a subsequent ordering of the elements in a sequence in
        /// descending order, according to a key.
        /// </summary>
        /// <remarks>
        /// In contrast to the sequential implementation, this is not a stable sort.
        /// See the remarks for OrderBy(ParallelQuery{TSource}, Func{TSource,TKey}) for
        /// an approach to implementing a stable sort.
        /// </remarks>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An OrderedParallelQuery{TSource} than contains
        /// elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An OrderedParallelQuery{TSource} whose elements are sorted
        /// descending according to a key.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        ///

        public static OrderedParallelQuery<TSource> ThenByDescending<TSource, TKey>(
            this OrderedParallelQuery<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            return new OrderedParallelQuery<TSource>(
                (QueryOperator<TSource>)source.OrderedEnumerable.CreateOrderedEnumerable<TKey>(keySelector, null, true));
        }
        /// <summary>
        /// Performs in parallel a subsequent ordering of the elements in a sequence in descending
        /// order by using a specified comparer.
        /// </summary>
        /// <remarks>
        /// In contrast to the sequential implementation, this is not a stable sort.
        /// See the remarks for OrderBy(ParallelQuery{TSource}, Func{TSource,TKey}) for
        /// an approach to implementing a stable sort.
        /// </remarks>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An OrderedParallelQuery{TSource} than contains
        /// elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">An IComparer{TKey} to compare keys.</param>
        /// <returns>An OrderedParallelQuery{TSource} whose elements are sorted
        /// descending according to a key.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        ///

        public static OrderedParallelQuery<TSource> ThenByDescending<TSource, TKey>(
            this OrderedParallelQuery<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            return new OrderedParallelQuery<TSource>(
                (QueryOperator<TSource>)source.OrderedEnumerable.CreateOrderedEnumerable<TKey>(keySelector, comparer, true));
        }

        //-----------------------------------------------------------------------------------
        // A GroupBy operation groups inputs based on a key-selection routine, yielding a
        // one-to-many value of key-to-elements to the consumer.
        //

        /// <summary>
        /// Groups in parallel the elements of a sequence according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence whose elements to group.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>A collection of elements of type IGrouping{TKey, TElement}, where each element represents a
        /// group and its key.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/>
        /// is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector)
        {
            return GroupBy<TSource, TKey>(source, keySelector, null);
        }

        /// <summary>
        /// Groups in parallel the elements of a sequence according to a specified key selector function and compares the keys by using a specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence whose elements to group.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <returns>A collection of elements of type IGrouping{TKey, TElement}, where each element represents a
        /// group and its key.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            return new GroupByQueryOperator<TSource, TKey, TSource>(source, keySelector, null, comparer);
        }

        /// <summary>
        /// Groups in parallel the elements of a sequence according to a specified key selector function and
        /// projects the elements for each group by using a specified function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the elements in each
        /// IGrouping{TKey, TElement}.</typeparam>
        /// <param name="source">A sequence whose elements to group.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an
        /// IGrouping{Key, TElement}.</param>
        /// <returns>A collection of elements of type IGrouping{TKey, TElement}, where each element represents a
        /// group and its key.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or
        /// <paramref name="elementSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            return GroupBy<TSource, TKey, TElement>(source, keySelector, elementSelector, null);
        }

        /// <summary>
        /// Groups in parallel the elements of a sequence according to a key selector function.
        /// The keys are compared by using a comparer and each group's elements are projected by
        /// using a specified function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the elements in each
        /// IGrouping{TKey, TElement}.</typeparam>
        /// <param name="source">A sequence whose elements to group.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an
        /// IGrouping{Key, TElement}.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <returns>A collection of elements of type IGrouping{TKey, TElement}, where each element represents a
        /// group and its key.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or
        /// <paramref name="elementSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector == null) throw new ArgumentNullException(nameof(elementSelector));

            return new GroupByQueryOperator<TSource, TKey, TElement>(source, keySelector, elementSelector, comparer);
        }

        //
        // @PERF: We implement the GroupBy overloads that accept a resultSelector using a GroupBy followed by a Select. This
        // adds some extra overhead, perhaps the most significant of which is an extra delegate invocation per element.
        //
        // One possible solution is to create two different versions of the GroupByOperator class, where one has a TResult
        // generic type and the other does not. Since this results in code duplication, we will avoid doing that for now.
        //
        // Another possible solution is to only have the more general GroupByOperator. Unfortunately, implementing the less
        // general overload (TResult == TElement) using the more general overload would likely result in unnecessary boxing
        // and unboxing of each processed element in the cases where TResult is a value type, so that solution comes with
        // a significant cost, too.
        //

        /// <summary>
        /// Groups in parallel the elements of a sequence according to a specified
        /// key selector function and creates a result value from each group and its key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector"/>.</typeparam>
        /// <param name="source">A sequence whose elements to group.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <returns>A collection of elements of type <typeparamref name="TResult"/> where each element represents a
        /// projection over a group and its key.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or
        /// <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> GroupBy<TSource, TKey, TResult>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector)

        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return source.GroupBy<TSource, TKey>(keySelector)
                .Select<IGrouping<TKey, TSource>, TResult>(delegate (IGrouping<TKey, TSource> grouping) { return resultSelector(grouping.Key, grouping); });
        }

        /// <summary>
        /// Groups in parallel the elements of a sequence according to a specified key selector function
        /// and creates a result value from each group and its key. The keys are compared
        /// by using a specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector"/>.</typeparam>
        /// <param name="source">A sequence whose elements to group.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <returns>A collection of elements of type <typeparamref name="TResult"/> where each element represents a
        /// projection over a group and its key.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or
        /// <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> GroupBy<TSource, TKey, TResult>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return source.GroupBy<TSource, TKey>(keySelector, comparer).Select<IGrouping<TKey, TSource>, TResult>(
                delegate (IGrouping<TKey, TSource> grouping) { return resultSelector(grouping.Key, grouping); });
        }

        /// <summary>
        /// Groups in parallel the elements of a sequence according to a specified key
        /// selector function and creates a result value from each group and its key.
        /// The elements of each group are projected by using a specified function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the elements in each
        /// IGrouping{TKey, TElement}.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector"/>.</typeparam>
        /// <param name="source">A sequence whose elements to group.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an
        /// IGrouping{Key, TElement}.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <returns>A collection of elements of type <typeparamref name="TResult"/> where each element represents a
        /// projection over a group and its key.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or
        /// <paramref name="elementSelector"/> or <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> GroupBy<TSource, TKey, TElement, TResult>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return source.GroupBy<TSource, TKey, TElement>(keySelector, elementSelector)
                .Select<IGrouping<TKey, TElement>, TResult>(delegate (IGrouping<TKey, TElement> grouping) { return resultSelector(grouping.Key, grouping); });
        }

        /// <summary>
        /// Groups the elements of a sequence according to a specified key selector function and
        /// creates a result value from each group and its key. Key values are compared by using a
        /// specified comparer, and the elements of each group are projected by using a specified function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the elements in each
        /// IGrouping{TKey, TElement}.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector"/>.</typeparam>
        /// <param name="source">A sequence whose elements to group.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an
        /// IGrouping{Key, TElement}.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <returns>A collection of elements of type <typeparamref name="TResult"/> where each element represents a
        /// projection over a group and its key.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or
        /// <paramref name="elementSelector"/> or <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> GroupBy<TSource, TKey, TElement, TResult>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return source.GroupBy<TSource, TKey, TElement>(keySelector, elementSelector, comparer)
                .Select<IGrouping<TKey, TElement>, TResult>(delegate (IGrouping<TKey, TElement> grouping) { return resultSelector(grouping.Key, grouping); });
        }

        /*===================================================================================
         * AGGREGATION OPERATORS
         *===================================================================================*/

        //-----------------------------------------------------------------------------------
        // Internal helper method that constructs an aggregation query operator and performs
        // the actual execution/reduction before returning the result.
        //
        // Arguments:
        //     source  - the data source over which aggregation is performed
        //     reduce  - the binary reduction operator
        //     options - whether the operator is associative, commutative, both, or neither
        //
        // Return Value:
        //     The result of aggregation.
        //
        private static T PerformAggregation<T>(this ParallelQuery<T> source,
            Func<T, T, T> reduce, T seed, bool seedIsSpecified, bool throwIfEmpty, QueryAggregationOptions options)
        {
            Debug.Assert(source != null);
            Debug.Assert(reduce != null);
            Debug.Assert(options.IsValidQueryAggregationOption(), "enum is out of range");

            AssociativeAggregationOperator<T, T, T> op = new AssociativeAggregationOperator<T, T, T>(
                source, seed, null, seedIsSpecified, reduce, reduce, delegate (T obj) { return obj; }, throwIfEmpty, options);
            return op.Aggregate();
        }

        /// <summary>
        /// Run an aggregation sequentially. If the user-provided reduction function throws an exception, wrap
        /// it with an AggregateException.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="seed"></param>
        /// <param name="seedIsSpecified">
        /// if true, use the seed provided in the method argument
        /// if false, use the first element of the sequence as the seed instead
        /// </param>
        /// <param name="func"></param>
        private static TAccumulate PerformSequentialAggregation<TSource, TAccumulate>(
            this ParallelQuery<TSource> source, TAccumulate seed, bool seedIsSpecified, Func<TAccumulate, TSource, TAccumulate> func)
        {
            Debug.Assert(source != null);
            Debug.Assert(func != null);
            Debug.Assert(seedIsSpecified || typeof(TSource) == typeof(TAccumulate));

            using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            {
                TAccumulate acc;
                if (seedIsSpecified)
                {
                    acc = seed;
                }
                else
                {
                    // Take the first element as the seed
                    if (!enumerator.MoveNext())
                    {
                        throw new InvalidOperationException(SR.NoElements);
                    }

                    acc = (TAccumulate)(object)enumerator.Current!;
                }

                while (enumerator.MoveNext())
                {
                    TSource elem = enumerator.Current;

                    // If the user delegate throws an exception, wrap it with an AggregateException
                    try
                    {
                        acc = func(acc, elem);
                    }
                    catch (Exception e)
                    {
                        throw new AggregateException(e);
                    }
                }

                return acc;
            }
        }

        //-----------------------------------------------------------------------------------
        // General purpose aggregation operators, allowing pluggable binary prefix operations.
        //

        /// <summary>
        /// Applies in parallel an accumulator function over a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence to aggregate over.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <returns>The final accumulator value.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="func"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource Aggregate<TSource>(
            this ParallelQuery<TSource> source, Func<TSource, TSource, TSource> func)
        {
            return Aggregate<TSource>(source, func, QueryAggregationOptions.AssociativeCommutative);
        }

        internal static TSource Aggregate<TSource>(
            this ParallelQuery<TSource> source, Func<TSource, TSource, TSource> func, QueryAggregationOptions options)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (func == null) throw new ArgumentNullException(nameof(func));
            if ((~(QueryAggregationOptions.Associative | QueryAggregationOptions.Commutative) & options) != 0) throw new ArgumentOutOfRangeException(nameof(options));

            if ((options & QueryAggregationOptions.Associative) != QueryAggregationOptions.Associative)
            {
                // Non associative aggregations must be run sequentially.  We run the query in parallel
                // and then perform the reduction over the resulting list.
                return source.PerformSequentialAggregation(default!, false, func);
            }
            else
            {
                // If associative, we can run this aggregation in parallel. The logic of the aggregation
                // operator depends on whether the operator is commutative, so we also pass that information
                // down to the query planning/execution engine.
                return source.PerformAggregation<TSource>(func, default!, false, true, options);
            }
        }

        /// <summary>
        /// Applies in parallel an accumulator function over a sequence.
        /// The specified seed value is used as the initial accumulator value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <param name="source">A sequence to aggregate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <returns>The final accumulator value.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="func"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TAccumulate Aggregate<TSource, TAccumulate>(
            this ParallelQuery<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func)
        {
            return Aggregate<TSource, TAccumulate>(source, seed, func, QueryAggregationOptions.AssociativeCommutative);
        }

        internal static TAccumulate Aggregate<TSource, TAccumulate>(
            this ParallelQuery<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func, QueryAggregationOptions options)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (func == null) throw new ArgumentNullException(nameof(func));
            if ((~(QueryAggregationOptions.Associative | QueryAggregationOptions.Commutative) & options) != 0) throw new ArgumentOutOfRangeException(nameof(options));

            return source.PerformSequentialAggregation(seed, true, func);
        }

        /// <summary>
        /// Applies in parallel an accumulator function over a sequence. The specified
        /// seed value is used as the initial accumulator value, and the specified
        /// function is used to select the result value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <typeparam name="TResult">The type of the resulting value.</typeparam>
        /// <param name="source">A sequence to aggregate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value
        /// into the result value.</param>
        /// <returns>The transformed final accumulator value.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="func"/> or <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TResult Aggregate<TSource, TAccumulate, TResult>(
            this ParallelQuery<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func,
            Func<TAccumulate, TResult> resultSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (func == null) throw new ArgumentNullException(nameof(func));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            TAccumulate acc = source.PerformSequentialAggregation(seed, true, func);
            try
            {
                return resultSelector(acc);
            }
            catch (Exception e)
            {
                throw new AggregateException(e);
            }
        }

        /// <summary>
        /// Applies in parallel an accumulator function over a sequence. This overload is not
        /// available in the sequential implementation.
        /// </summary>
        /// <remarks>
        /// This overload is specific to processing a parallelized query. A parallelized query may
        /// partition the data source sequence into several sub-sequences (partitions).
        /// The <paramref name="updateAccumulatorFunc"/> is invoked on each element within partitions.
        /// Each partition then yields a single accumulated result. The <paramref name="combineAccumulatorsFunc"/>
        /// is then invoked on the results of each partition to yield a single element. This element is then
        /// transformed by the <paramref name="resultSelector"/> function.
        /// </remarks>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <typeparam name="TResult">The type of the resulting value.</typeparam>
        /// <param name="source">A sequence to aggregate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="updateAccumulatorFunc">
        /// An accumulator function to be invoked on each element in a partition.
        /// </param>
        /// <param name="combineAccumulatorsFunc">
        /// An accumulator function to be invoked on the yielded element from each partition.
        /// </param>
        /// <param name="resultSelector">
        /// A function to transform the final accumulator value into the result value.
        /// </param>
        /// <returns>The transformed final accumulator value.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="updateAccumulatorFunc"/>
        /// or <paramref name="combineAccumulatorsFunc"/> or <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TResult Aggregate<TSource, TAccumulate, TResult>(
            this ParallelQuery<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> updateAccumulatorFunc,
            Func<TAccumulate, TAccumulate, TAccumulate> combineAccumulatorsFunc, Func<TAccumulate, TResult> resultSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (updateAccumulatorFunc == null) throw new ArgumentNullException(nameof(updateAccumulatorFunc));
            if (combineAccumulatorsFunc == null) throw new ArgumentNullException(nameof(combineAccumulatorsFunc));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return new AssociativeAggregationOperator<TSource, TAccumulate, TResult>(
                source, seed, null, true, updateAccumulatorFunc, combineAccumulatorsFunc, resultSelector,
                false, QueryAggregationOptions.AssociativeCommutative).Aggregate();
        }

        /// <summary>
        /// Applies in parallel an accumulator function over a sequence.  This overload is not
        /// available in the sequential implementation.
        /// </summary>
        /// <remarks>
        /// This overload is specific to parallelized queries. A parallelized query may partition the data source sequence
        /// into several sub-sequences (partitions). The <paramref name="updateAccumulatorFunc"/> is invoked
        /// on each element within partitions. Each partition then yields a single accumulated result.
        /// The <paramref name="combineAccumulatorsFunc"/>
        /// is then invoked on the results of each partition to yield a single element. This element is then
        /// transformed by the <paramref name="resultSelector"/> function.
        /// </remarks>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <typeparam name="TResult">The type of the resulting value.</typeparam>
        /// <param name="source">A sequence to aggregate over.</param>
        /// <param name="seedFactory">
        /// A function that returns the initial accumulator value.
        /// </param>
        /// <param name="updateAccumulatorFunc">
        /// An accumulator function to be invoked on each element in a partition.
        /// </param>
        /// <param name="combineAccumulatorsFunc">
        /// An accumulator function to be invoked on the yielded element from each partition.
        /// </param>
        /// <param name="resultSelector">
        /// A function to transform the final accumulator value into the result value.
        /// </param>
        /// <returns>The transformed final accumulator value.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="seedFactory"/> or <paramref name="updateAccumulatorFunc"/>
        /// or <paramref name="combineAccumulatorsFunc"/> or <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TResult Aggregate<TSource, TAccumulate, TResult>(
            this ParallelQuery<TSource> source,
            Func<TAccumulate> seedFactory,
            Func<TAccumulate, TSource, TAccumulate> updateAccumulatorFunc,
            Func<TAccumulate, TAccumulate, TAccumulate> combineAccumulatorsFunc,
            Func<TAccumulate, TResult> resultSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (seedFactory == null) throw new ArgumentNullException(nameof(seedFactory));
            if (updateAccumulatorFunc == null) throw new ArgumentNullException(nameof(updateAccumulatorFunc));
            if (combineAccumulatorsFunc == null) throw new ArgumentNullException(nameof(combineAccumulatorsFunc));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return new AssociativeAggregationOperator<TSource, TAccumulate, TResult>(
                source, default!, seedFactory, true, updateAccumulatorFunc, combineAccumulatorsFunc, resultSelector,
                false, QueryAggregationOptions.AssociativeCommutative).Aggregate();
        }


        //-----------------------------------------------------------------------------------
        // Count and LongCount reductions.
        //

        /// <summary>
        /// Returns the number of elements in a parallel sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence that contains elements to be counted.</param>
        /// <returns>The number of elements in the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The number of elements in source is larger than <see cref="int.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static int Count<TSource>(this ParallelQuery<TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // If the data source is a collection, we can just return the count right away.
            if (source is ParallelEnumerableWrapper<TSource> sourceAsWrapper)
            {
                if (sourceAsWrapper.WrappedEnumerable is ICollection<TSource> sourceAsCollection)
                {
                    return sourceAsCollection.Count;
                }
            }

            // Otherwise, enumerate the whole thing and aggregate a count.
            checked
            {
                return new CountAggregationOperator<TSource>(source).Aggregate();
            }
        }

        /// <summary>
        /// Returns a number that represents how many elements in the specified
        /// parallel sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence that contains elements to be counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// A number that represents how many elements in the sequence satisfy the condition
        /// in the predicate function.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The number of elements in source is larger than <see cref="int.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static int Count<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            // Construct a where operator to filter out non-matching elements, and then aggregate.
            checked
            {
                return new CountAggregationOperator<TSource>(Where<TSource>(source, predicate)).Aggregate();
            }
        }

        /// <summary>
        /// Returns an Int64 that represents the total number of elements in a parallel sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence that contains elements to be counted.</param>
        /// <returns>The number of elements in the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The number of elements in source is larger than <see cref="long.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static long LongCount<TSource>(this ParallelQuery<TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // If the data source is a collection, we can just return the count right away.
            if (source is ParallelEnumerableWrapper<TSource> sourceAsWrapper)
            {
                if (sourceAsWrapper.WrappedEnumerable is ICollection<TSource> sourceAsCollection)
                {
                    return sourceAsCollection.Count;
                }
            }

            // Otherwise, enumerate the whole thing and aggregate a count.
            return new LongCountAggregationOperator<TSource>(source).Aggregate();
        }

        /// <summary>
        /// Returns an Int64 that represents how many elements in a parallel sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence that contains elements to be counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// A number that represents how many elements in the sequence satisfy the condition
        /// in the predicate function.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The number of elements in source is larger than <see cref="long.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static long LongCount<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            // Construct a where operator to filter out non-matching elements, and then aggregate.
            return new LongCountAggregationOperator<TSource>(Where<TSource>(source, predicate)).Aggregate();
        }

        //-----------------------------------------------------------------------------------
        // Sum aggregations.
        //

        /// <summary>
        /// Computes in parallel the sum of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum is larger than <see cref="int.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static int Sum(this ParallelQuery<int> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new IntSumAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the sum of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum is larger than <see cref="int.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static int? Sum(this ParallelQuery<int?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableIntSumAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the sum of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum is larger than <see cref="long.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static long Sum(this ParallelQuery<long> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new LongSumAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the sum of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum is larger than <see cref="long.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static long? Sum(this ParallelQuery<long?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableLongSumAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the sum of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static float Sum(this ParallelQuery<float> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new FloatSumAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the sum of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static float? Sum(this ParallelQuery<float?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableFloatSumAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the sum of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double Sum(this ParallelQuery<double> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new DoubleSumAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the sum of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double? Sum(this ParallelQuery<double?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableDoubleSumAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the sum of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum is larger than <see cref="decimal.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static decimal Sum(this ParallelQuery<decimal> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new DecimalSumAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the sum of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum is larger than <see cref="decimal.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static decimal? Sum(this ParallelQuery<decimal?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableDecimalSumAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the sum of the sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum is larger than <see cref="int.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static int Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, int> selector)
        {
            return source.Select(selector).Sum();
        }

        /// <summary>
        /// Computes in parallel the sum of the sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum is larger than <see cref="int.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static int? Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, int?> selector)
        {
            return source.Select(selector).Sum();
        }

        /// <summary>
        /// Computes in parallel the sum of the sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum is larger than <see cref="long.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static long Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, long> selector)
        {
            return source.Select(selector).Sum();
        }

        /// <summary>
        /// Computes in parallel the sum of the sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum is larger than <see cref="long.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static long? Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, long?> selector)
        {
            return source.Select(selector).Sum();
        }

        /// <summary>
        /// Computes in parallel the sum of the sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static float Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, float> selector)
        {
            return source.Select(selector).Sum();
        }

        /// <summary>
        /// Computes in parallel the sum of the sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static float? Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, float?> selector)
        {
            return source.Select(selector).Sum();
        }

        /// <summary>
        /// Computes in parallel the sum of the sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, double> selector)
        {
            return source.Select(selector).Sum();
        }

        /// <summary>
        /// Computes in parallel the sum of the sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double? Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, double?> selector)
        {
            return source.Select(selector).Sum();
        }

        /// <summary>
        /// Computes in parallel the sum of the sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum is larger than <see cref="decimal.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static decimal Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, decimal> selector)
        {
            return source.Select(selector).Sum();
        }

        /// <summary>
        /// Computes in parallel the sum of the sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum is larger than <see cref="decimal.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static decimal? Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, decimal?> selector)
        {
            return source.Select(selector).Sum();
        }

        //-----------------------------------------------------------------------------------
        // Helper methods used for Min/Max aggregations below. This class can create a whole
        // bunch of type-specific delegates that are passed to the general aggregation
        // infrastructure. All comparisons are performed using the Comparer<T>.Default
        // comparator. LINQ doesn't offer a way to override this, so neither do we.
        //
        // @PERF: we'll eventually want inlined primitive providers that use IL instructions
        //    for comparison instead of the Comparer<T>.CompareTo method.
        //

        //-----------------------------------------------------------------------------------
        // Min aggregations.
        //

        /// <summary>
        /// Returns the minimum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static int Min(this ParallelQuery<int> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new IntMinMaxAggregationOperator(source, -1).Aggregate();
        }

        /// <summary>
        /// Returns the minimum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static int? Min(this ParallelQuery<int?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableIntMinMaxAggregationOperator(source, -1).Aggregate();
        }

        /// <summary>
        /// Returns the minimum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static long Min(this ParallelQuery<long> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new LongMinMaxAggregationOperator(source, -1).Aggregate();
        }

        /// <summary>
        /// Returns the minimum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static long? Min(this ParallelQuery<long?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableLongMinMaxAggregationOperator(source, -1).Aggregate();
        }

        /// <summary>
        /// Returns the minimum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static float Min(this ParallelQuery<float> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new FloatMinMaxAggregationOperator(source, -1).Aggregate();
        }

        /// <summary>
        /// Returns the minimum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static float? Min(this ParallelQuery<float?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableFloatMinMaxAggregationOperator(source, -1).Aggregate();
        }

        /// <summary>
        /// Returns the minimum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double Min(this ParallelQuery<double> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new DoubleMinMaxAggregationOperator(source, -1).Aggregate();
        }

        /// <summary>
        /// Returns the minimum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double? Min(this ParallelQuery<double?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableDoubleMinMaxAggregationOperator(source, -1).Aggregate();
        }

        /// <summary>
        /// Returns the minimum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static decimal Min(this ParallelQuery<decimal> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new DecimalMinMaxAggregationOperator(source, -1).Aggregate();
        }

        /// <summary>
        /// Returns the minimum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static decimal? Min(this ParallelQuery<decimal?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableDecimalMinMaxAggregationOperator(source, -1).Aggregate();
        }

        /// <summary>
        /// Returns the minimum value in a parallel sequence of values.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements and <typeparamref name="TSource"/> is a non-nullable value type.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource? Min<TSource>(this ParallelQuery<TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return AggregationMinMaxHelpers<TSource>.ReduceMin(source);
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the minimum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static int Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, int> selector)
        {
            return source.Select<TSource, int>(selector).Min<int>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the minimum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static int? Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, int?> selector)
        {
            return source.Select<TSource, int?>(selector).Min<int?>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the minimum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static long Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, long> selector)
        {
            return source.Select<TSource, long>(selector).Min<long>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the minimum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static long? Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, long?> selector)
        {
            return source.Select<TSource, long?>(selector).Min<long?>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the minimum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static float Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, float> selector)
        {
            return source.Select<TSource, float>(selector).Min<float>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the minimum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static float? Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, float?> selector)
        {
            return source.Select<TSource, float?>(selector).Min<float?>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the minimum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, double> selector)
        {
            return source.Select<TSource, double>(selector).Min<double>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the minimum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double? Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, double?> selector)
        {
            return source.Select<TSource, double?>(selector).Min<double?>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the minimum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static decimal Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, decimal> selector)
        {
            return source.Select<TSource, decimal>(selector).Min<decimal>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the minimum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static decimal? Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, decimal?> selector)
        {
            return source.Select<TSource, decimal?>(selector).Min<decimal?>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the minimum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements and <typeparamref name="TResult"/> is a non-nullable value type.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TResult? Min<TSource, TResult>(this ParallelQuery<TSource> source, Func<TSource, TResult> selector)
        {
            return source.Select<TSource, TResult>(selector).Min<TResult>();
        }

        //-----------------------------------------------------------------------------------
        // Max aggregations.
        //

        /// <summary>
        /// Returns the maximum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static int Max(this ParallelQuery<int> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new IntMinMaxAggregationOperator(source, 1).Aggregate();
        }

        /// <summary>
        /// Returns the maximum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static int? Max(this ParallelQuery<int?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableIntMinMaxAggregationOperator(source, 1).Aggregate();
        }

        /// <summary>
        /// Returns the maximum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static long Max(this ParallelQuery<long> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new LongMinMaxAggregationOperator(source, 1).Aggregate();
        }

        /// <summary>
        /// Returns the maximum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static long? Max(this ParallelQuery<long?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableLongMinMaxAggregationOperator(source, 1).Aggregate();
        }

        /// <summary>
        /// Returns the maximum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static float Max(this ParallelQuery<float> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new FloatMinMaxAggregationOperator(source, 1).Aggregate();
        }

        /// <summary>
        /// Returns the maximum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static float? Max(this ParallelQuery<float?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableFloatMinMaxAggregationOperator(source, 1).Aggregate();
        }

        /// <summary>
        /// Returns the maximum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double Max(this ParallelQuery<double> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new DoubleMinMaxAggregationOperator(source, 1).Aggregate();
        }

        /// <summary>
        /// Returns the maximum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double? Max(this ParallelQuery<double?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableDoubleMinMaxAggregationOperator(source, 1).Aggregate();
        }

        /// <summary>
        /// Returns the maximum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static decimal Max(this ParallelQuery<decimal> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new DecimalMinMaxAggregationOperator(source, 1).Aggregate();
        }

        /// <summary>
        /// Returns the maximum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static decimal? Max(this ParallelQuery<decimal?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableDecimalMinMaxAggregationOperator(source, 1).Aggregate();
        }

        /// <summary>
        /// Returns the maximum value in a parallel sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements and <typeparam name="TSource"/> is a non-nullable value type.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource? Max<TSource>(this ParallelQuery<TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return AggregationMinMaxHelpers<TSource>.ReduceMax(source);
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the maximum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static int Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, int> selector)
        {
            return source.Select<TSource, int>(selector).Max<int>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the maximum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static int? Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, int?> selector)
        {
            return source.Select<TSource, int?>(selector).Max<int?>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the maximum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static long Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, long> selector)
        {
            return source.Select<TSource, long>(selector).Max<long>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the maximum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static long? Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, long?> selector)
        {
            return source.Select<TSource, long?>(selector).Max<long?>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the maximum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static float Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, float> selector)
        {
            return source.Select<TSource, float>(selector).Max<float>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the maximum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static float? Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, float?> selector)
        {
            return source.Select<TSource, float?>(selector).Max<float?>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the maximum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, double> selector)
        {
            return source.Select<TSource, double>(selector).Max<double>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the maximum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double? Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, double?> selector)
        {
            return source.Select<TSource, double?>(selector).Max<double?>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the maximum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static decimal Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, decimal> selector)
        {
            return source.Select<TSource, decimal>(selector).Max<decimal>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the maximum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static decimal? Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, decimal?> selector)
        {
            return source.Select<TSource, decimal?>(selector).Max<decimal?>();
        }

        /// <summary>
        /// Invokes in parallel a transform function on each element of a
        /// sequence and returns the maximum value.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements and <typeparamref name="TResult"/> is a non-nullable value type.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TResult? Max<TSource, TResult>(this ParallelQuery<TSource> source, Func<TSource, TResult> selector)
        {
            return source.Select<TSource, TResult>(selector).Max<TResult>();
        }

        //-----------------------------------------------------------------------------------
        // Average aggregations.
        //

        /// <summary>
        /// Computes in parallel the average of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum or count of the elements in the sequence is larger than <see cref="int.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double Average(this ParallelQuery<int> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new IntAverageAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum or count of the elements in the sequence is larger than <see cref="int.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double? Average(this ParallelQuery<int?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableIntAverageAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum or count of the elements in the sequence is larger than <see cref="int.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double Average(this ParallelQuery<long> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new LongAverageAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum or count of the elements in the sequence is larger than <see cref="int.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double? Average(this ParallelQuery<long?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableLongAverageAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static float Average(this ParallelQuery<float> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new FloatAverageAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static float? Average(this ParallelQuery<float?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableFloatAverageAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double Average(this ParallelQuery<double> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new DoubleAverageAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double? Average(this ParallelQuery<double?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableDoubleAverageAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static decimal Average(this ParallelQuery<decimal> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new DecimalAverageAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static decimal? Average(this ParallelQuery<decimal?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NullableDecimalAverageAggregationOperator(source).Aggregate();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum or count of the elements in the sequence is larger than <see cref="int.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, int> selector)
        {
            return source.Select(selector).Average();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum or count of the elements in the sequence is larger than <see cref="int.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double? Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, int?> selector)
        {
            return source.Select(selector).Average();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum or count of the elements in the sequence is larger than <see cref="int.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, long> selector)
        {
            return source.Select(selector).Average();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// The sum or count of the elements in the sequence is larger than <see cref="long.MaxValue"/>.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double? Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, long?> selector)
        {
            return source.Select(selector).Average();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static float Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, float> selector)
        {
            return source.Select(selector).Average();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static float? Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, float?> selector)
        {
            return source.Select(selector).Average();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, double> selector)
        {
            return source.Select(selector).Average();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static double? Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, double?> selector)
        {
            return source.Select(selector).Average();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static decimal Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, decimal> selector)
        {
            return source.Select(selector).Average();
        }

        /// <summary>
        /// Computes in parallel the average of a sequence of values that are obtained
        /// by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static decimal? Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, decimal?> selector)
        {
            return source.Select(selector).Average();
        }

        //-----------------------------------------------------------------------------------
        // Any returns true if there exists an element for which the predicate returns true.
        //

        /// <summary>
        /// Determines in parallel whether any element of a sequence satisfies a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An IEnumerable whose elements to apply the predicate to.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// true if any elements in the source sequence pass the test in the specified predicate; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static bool Any<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return new AnyAllSearchOperator<TSource>(source, true, predicate).Aggregate();
        }

        /// <summary>
        /// Determines whether a parallel sequence contains any elements.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The IEnumerable to check for emptiness.</param>
        /// <returns>true if the source sequence contains any elements; otherwise, false.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static bool Any<TSource>(this ParallelQuery<TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Any(source, x => true);
        }

        //-----------------------------------------------------------------------------------
        // All returns false if there exists an element for which the predicate returns false.
        //

        /// <summary>
        /// Determines in parallel whether all elements of a sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence whose elements to apply the predicate to.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// true if all elements in the source sequence pass the test in the specified predicate; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static bool All<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return new AnyAllSearchOperator<TSource>(source, false, predicate).Aggregate();
        }

        //-----------------------------------------------------------------------------------
        // Contains returns true if the specified value was found in the data source.
        //

        /// <summary>
        /// Determines in parallel whether a sequence contains a specified element
        /// by using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence in which to locate a value.</param>
        /// <param name="value">The value to locate in the sequence.</param>
        /// <returns>
        /// true if the source sequence contains an element that has the specified value; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static bool Contains<TSource>(this ParallelQuery<TSource> source, TSource value)
        {
            return Contains(source, value, null);
        }

        /// <summary>
        /// Determines in parallel whether a sequence contains a specified element by using a
        /// specified IEqualityComparer{T}.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence in which to locate a value.</param>
        /// <param name="value">The value to locate in the sequence.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>
        /// true if the source sequence contains an element that has the specified value; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static bool Contains<TSource>(this ParallelQuery<TSource> source, TSource value, IEqualityComparer<TSource>? comparer)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // @PERF: there are many simple optimizations we can make for collection types with known sizes.

            return new ContainsSearchOperator<TSource>(source, value, comparer).Aggregate();
        }

        /*===================================================================================
         * TOP (TAKE, SKIP) OPERATORS
         *===================================================================================*/

        //-----------------------------------------------------------------------------------
        // Take will take the first [0..count) contiguous elements from the input.
        //

        /// <summary>
        /// Returns a specified number of contiguous elements from the start of a parallel sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="count">The number of elements to return.</param>
        /// <returns>
        /// A sequence that contains the specified number of elements from the start of the input sequence.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> Take<TSource>(this ParallelQuery<TSource> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (count > 0)
            {
                return new TakeOrSkipQueryOperator<TSource>(source, count, true);
            }
            else
            {
                return ParallelEnumerable.Empty<TSource>();
            }
        }

        //-----------------------------------------------------------------------------------
        // TakeWhile will take the first [0..N) contiguous elements, where N is the smallest
        // index of an element for which the predicate yields false.
        //

        /// <summary>
        /// Returns elements from a parallel sequence as long as a specified condition is true.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// A sequence that contains the elements from the input sequence that occur before
        /// the element at which the test no longer passes.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> TakeWhile<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return new TakeOrSkipWhileQueryOperator<TSource>(source, predicate, null, true);
        }

        /// <summary>
        /// Returns elements from a parallel sequence as long as a specified condition is true.
        /// The element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="predicate">
        /// A function to test each source element for a condition; the second parameter of the
        /// function represents the index of the source element.
        /// </param>
        /// <returns>
        /// A sequence that contains elements from the input sequence that occur before
        /// the element at which the test no longer passes.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> TakeWhile<TSource>(this ParallelQuery<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return new TakeOrSkipWhileQueryOperator<TSource>(source, null, predicate, true);
        }

        //-----------------------------------------------------------------------------------
        // Skip will take the last [count..M) contiguous elements from the input, where M is
        // the size of the input.
        //

        /// <summary>
        /// Bypasses a specified number of elements in a parallel sequence and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>
        /// A sequence that contains the elements that occur after the specified index in the input sequence.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> Skip<TSource>(this ParallelQuery<TSource> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // If the count is 0 (or less) we just return the whole stream.
            if (count <= 0)
            {
                return source;
            }

            return new TakeOrSkipQueryOperator<TSource>(source, count, false);
        }

        //-----------------------------------------------------------------------------------
        // SkipWhile will take the last [N..M) contiguous elements, where N is the smallest
        // index of an element for which the predicate yields false, and M is the size of
        // the input data source.
        //

        /// <summary>
        /// Bypasses elements in a parallel sequence as long as a specified
        /// condition is true and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A sequence that contains the elements from the input sequence starting at
        /// the first element in the linear series that does not pass the test specified by
        /// <B>predicate</B>.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> SkipWhile<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return new TakeOrSkipWhileQueryOperator<TSource>(source, predicate, null, false);
        }

        /// <summary>
        /// Bypasses elements in a parallel sequence as long as a specified condition is true and
        /// then returns the remaining elements. The element's index is used in the logic of
        /// the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="predicate">
        /// A function to test each source element for a condition; the
        /// second parameter of the function represents the index of the source element.
        /// </param>
        /// <returns>
        /// A sequence that contains the elements from the input sequence starting at the
        /// first element in the linear series that does not pass the test specified by
        /// <B>predicate</B>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> SkipWhile<TSource>(this ParallelQuery<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return new TakeOrSkipWhileQueryOperator<TSource>(source, null, predicate, false);
        }

        /*===================================================================================
         * SET OPERATORS
         *===================================================================================*/

        //-----------------------------------------------------------------------------------
        // Appends the second data source to the first, preserving order in the process.
        //

        /// <summary>
        /// Concatenates two parallel sequences.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="second">The sequence to concatenate to the first sequence.</param>
        /// <returns>A sequence that contains the concatenated elements of the two input sequences.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="first"/> or <paramref name="second"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> Concat<TSource>(this ParallelQuery<TSource> first, ParallelQuery<TSource> second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            return new ConcatQueryOperator<TSource>(first, second);
        }

        /// <summary>
        /// This Concat overload should never be called.
        /// This method is marked as obsolete and always throws <see cref="System.NotSupportedException"/> when called.
        /// </summary>
        /// <typeparam name="TSource">This type parameter is not used.</typeparam>
        /// <param name="first">This parameter is not used.</param>
        /// <param name="second">This parameter is not used.</param>
        /// <returns>This overload always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <exception cref="System.NotSupportedException">The exception that occurs when this method is called.</exception>
        /// <remarks>
        /// This overload exists to disallow usage of Concat with a left data source of type
        /// <see cref="System.Linq.ParallelQuery{TSource}"/> and a right data source of type <see cref="System.Collections.Generic.IEnumerable{TSource}"/>.
        /// Otherwise, the Concat operator would appear to be binding to the parallel implementation,
        /// but would in reality bind to the sequential implementation.
        /// </remarks>
        [Obsolete(RIGHT_SOURCE_NOT_PARALLEL_STR)]
        public static ParallelQuery<TSource> Concat<TSource>(this ParallelQuery<TSource> first, IEnumerable<TSource> second)
        {
            throw new NotSupportedException(SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
        }

        //-----------------------------------------------------------------------------------
        // Compares two input streams pairwise for equality.
        //

        /// <summary>
        /// Determines whether two parallel sequences are equal by comparing the elements by using
        /// the default equality comparer for their type.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">A sequence to compare to <b>second</b>.</param>
        /// <param name="second">A sequence to compare to the first input sequence.</param>
        /// <returns>
        /// true if the two source sequences are of equal length and their corresponding elements
        /// are equal according to the default equality comparer for their type; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="first"/> or <paramref name="second"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static bool SequenceEqual<TSource>(this ParallelQuery<TSource> first, ParallelQuery<TSource> second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            return SequenceEqual<TSource>(first, second, null);
        }

        /// <summary>
        /// This SequenceEqual overload should never be called.
        /// This method is marked as obsolete and always throws <see cref="System.NotSupportedException"/> when called.
        /// </summary>
        /// <typeparam name="TSource">This type parameter is not used.</typeparam>
        /// <param name="first">This parameter is not used.</param>
        /// <param name="second">This parameter is not used.</param>
        /// <returns>This overload always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <exception cref="System.NotSupportedException">Thrown every time this method is called.</exception>
        /// <remarks>
        /// This overload exists to disallow usage of SequenceEqual with a left data source of type
        /// <see cref="System.Linq.ParallelQuery{TSource}"/> and a right data source of type <see cref="System.Collections.Generic.IEnumerable{TSource}"/>.
        /// Otherwise, the SequenceEqual operator would appear to be binding to the parallel implementation,
        /// but would in reality bind to the sequential implementation.
        /// </remarks>
        [Obsolete(RIGHT_SOURCE_NOT_PARALLEL_STR)]
        public static bool SequenceEqual<TSource>(this ParallelQuery<TSource> first, IEnumerable<TSource> second)
        {
            throw new NotSupportedException(SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
        }

        /// <summary>
        /// Determines whether two parallel sequences are equal by comparing their elements by
        /// using a specified IEqualityComparer{T}.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">A sequence to compare to <paramref name="second"/>.</param>
        /// <param name="second">A sequence to compare to the first input sequence.</param>
        /// <param name="comparer">An IEqualityComparer&lt;(Of &lt;(T&gt;)&gt;) to use to compare elements.</param>
        /// <returns>
        /// true if the two source sequences are of equal length and their corresponding
        /// elements are equal according to the default equality comparer for their type; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="first"/> or <paramref name="second"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static bool SequenceEqual<TSource>(this ParallelQuery<TSource> first, ParallelQuery<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            // If comparer is null, use the default one
            comparer = comparer ?? EqualityComparer<TSource>.Default;

            QueryOperator<TSource> leftOp = QueryOperator<TSource>.AsQueryOperator(first);
            QueryOperator<TSource> rightOp = QueryOperator<TSource>.AsQueryOperator(second);

            // We use a fully-qualified type name for Shared here to prevent the conflict between System.Linq.Parallel.Shared<>
            // and System.Threading.Shared<> in the 3.5 legacy build.
            QuerySettings settings = leftOp.SpecifiedQuerySettings.Merge(rightOp.SpecifiedQuerySettings)
                .WithDefaults()
                .WithPerExecutionSettings(new CancellationTokenSource(), new System.Linq.Parallel.Shared<bool>(false));

            // If first.GetEnumerator throws an exception, we don't want to wrap it with an AggregateException.
            IEnumerator<TSource> e1 = first.GetEnumerator();
            try
            {
                // If second.GetEnumerator throws an exception, we don't want to wrap it with an AggregateException.
                IEnumerator<TSource> e2 = second.GetEnumerator();
                try
                {
                    while (e1.MoveNext())
                    {
                        if (!(e2.MoveNext() && comparer.Equals(e1.Current, e2.Current))) return false;
                    }
                    if (e2.MoveNext()) return false;
                }
                catch (Exception ex)
                {
                    ExceptionAggregator.ThrowOCEorAggregateException(ex, settings.CancellationState);
                }
                finally
                {
                    DisposeEnumerator<TSource>(e2, settings.CancellationState);
                }
            }
            finally
            {
                DisposeEnumerator<TSource>(e1, settings.CancellationState);
            }

            return true;
        }

        /// <summary>
        /// A helper method for SequenceEqual to dispose an enumerator. If an exception is thrown by the disposal,
        /// it gets wrapped into an AggregateException, unless it is an OCE with the query's CancellationToken.
        /// </summary>
        private static void DisposeEnumerator<TSource>(IEnumerator<TSource> e, CancellationState cancelState)
        {
            try
            {
                e.Dispose();
            }
            catch (Exception ex)
            {
                ExceptionAggregator.ThrowOCEorAggregateException(ex, cancelState);
            }
        }

        /// <summary>
        /// This SequenceEqual overload should never be called.
        /// This method is marked as obsolete and always throws <see cref="System.NotSupportedException"/> when called.
        /// </summary>
        /// <typeparam name="TSource">This type parameter is not used.</typeparam>
        /// <param name="first">This parameter is not used.</param>
        /// <param name="second">This parameter is not used.</param>
        /// <param name="comparer">This parameter is not used.</param>
        /// <returns>This overload always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <exception cref="System.NotSupportedException">Thrown every time this method is called.</exception>
        /// <remarks>
        /// This overload exists to disallow usage of SequenceEqual with a left data source of type
        /// <see cref="System.Linq.ParallelQuery{TSource}"/> and a right data source of type <see cref="System.Collections.Generic.IEnumerable{TSource}"/>.
        /// Otherwise, the SequenceEqual operator would appear to be binding to the parallel implementation,
        /// but would in reality bind to sequential implementation.
        /// </remarks>
        [Obsolete(RIGHT_SOURCE_NOT_PARALLEL_STR)]
        public static bool SequenceEqual<TSource>(this ParallelQuery<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            throw new NotSupportedException(SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
        }

        //-----------------------------------------------------------------------------------
        // Calculates the distinct set of elements in the single input data source.
        //

        /// <summary>
        /// Returns distinct elements from a parallel sequence by using the
        /// default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <returns>A sequence that contains distinct elements from the source sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> Distinct<TSource>(
            this ParallelQuery<TSource> source)
        {
            return Distinct(source, null);
        }

        /// <summary>
        /// Returns distinct elements from a parallel sequence by using a specified
        /// IEqualityComparer{T} to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="comparer">An IEqualityComparer&lt;(Of &lt;(T&gt;)&gt;) to compare values.</param>
        /// <returns>A sequence that contains distinct elements from the source sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> Distinct<TSource>(
            this ParallelQuery<TSource> source, IEqualityComparer<TSource>? comparer)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return new DistinctQueryOperator<TSource>(source, comparer);
        }

        //-----------------------------------------------------------------------------------
        // Calculates the union between the first and second data sources.
        //

        /// <summary>
        /// Produces the set union of two parallel sequences by using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">A sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">A sequence whose distinct elements form the second set for the union.</param>
        /// <returns>A sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="first"/> or <paramref name="second"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> Union<TSource>(
            this ParallelQuery<TSource> first, ParallelQuery<TSource> second)
        {
            return Union(first, second, null);
        }

        /// <summary>
        /// This Union overload should never be called.
        /// This method is marked as obsolete and always throws <see cref="System.NotSupportedException"/> when called.
        /// </summary>
        /// <typeparam name="TSource">This type parameter is not used.</typeparam>
        /// <param name="first">This parameter is not used.</param>
        /// <param name="second">This parameter is not used.</param>
        /// <returns>This overload always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <exception cref="System.NotSupportedException">The exception that occurs when this method is called.</exception>
        /// <remarks>
        /// This overload exists to disallow usage of Union with a left data source of type
        /// <see cref="System.Linq.ParallelQuery{TSource}"/> and a right data source of type <see cref="System.Collections.Generic.IEnumerable{TSource}"/>.
        /// Otherwise, the Union operator would appear to be binding to the parallel implementation,
        /// but would in reality bind to sequential implementation.
        /// </remarks>
        [Obsolete(RIGHT_SOURCE_NOT_PARALLEL_STR)]
        public static ParallelQuery<TSource> Union<TSource>(
            this ParallelQuery<TSource> first, IEnumerable<TSource> second)
        {
            throw new NotSupportedException(SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
        }

        /// <summary>
        /// Produces the set union of two parallel sequences by using a specified IEqualityComparer{T}.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">A sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">A sequence whose distinct elements form the second set for the union.</param>
        /// <param name="comparer">An IEqualityComparer&lt;(Of &lt;(T&gt;)&gt;) to compare values.</param>
        /// <returns>A sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="first"/> or <paramref name="second"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> Union<TSource>(
            this ParallelQuery<TSource> first, ParallelQuery<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return new UnionQueryOperator<TSource>(first, second, comparer);
        }

        /// <summary>
        /// This Union overload should never be called.
        /// This method is marked as obsolete and always throws <see cref="System.NotSupportedException"/> when called.
        /// </summary>
        /// <typeparam name="TSource">This type parameter is not used.</typeparam>
        /// <param name="first">This parameter is not used.</param>
        /// <param name="second">This parameter is not used.</param>
        /// <param name="comparer">This parameter is not used.</param>
        /// <returns>This overload always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <exception cref="System.NotSupportedException">The exception that occurs when this method is called.</exception>
        /// <remarks>
        /// This overload exists to disallow usage of Union with a left data source of type
        /// <see cref="System.Linq.ParallelQuery{TSource}"/> and a right data source of type <see cref="System.Collections.Generic.IEnumerable{TSource}"/>.
        /// Otherwise, the Union operator would appear to be binding to the parallel implementation,
        /// but would in reality bind to the sequential implementation.
        /// </remarks>
        [Obsolete(RIGHT_SOURCE_NOT_PARALLEL_STR)]
        public static ParallelQuery<TSource> Union<TSource>(
            this ParallelQuery<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            throw new NotSupportedException(SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
        }

        //-----------------------------------------------------------------------------------
        // Calculates the intersection between the first and second data sources.
        //

        /// <summary>
        /// Produces the set intersection of two parallel sequences by using the
        /// default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first"
        /// >A sequence whose distinct elements that also appear in <paramref name="second"/> will be returned.
        /// </param>
        /// <param name="second">
        /// A sequence whose distinct elements that also appear in the first sequence will be returned.
        /// </param>
        /// <returns>A sequence that contains the elements that form the set intersection of two sequences.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="first"/> or <paramref name="second"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> Intersect<TSource>(
            this ParallelQuery<TSource> first, ParallelQuery<TSource> second)
        {
            return Intersect(first, second, null);
        }

        /// <summary>
        /// This Intersect overload should never be called.
        /// This method is marked as obsolete and always throws <see cref="System.NotSupportedException"/> when called.
        /// </summary>
        /// <typeparam name="TSource">This type parameter is not used.</typeparam>
        /// <param name="first">This parameter is not used.</param>
        /// <param name="second">This parameter is not used.</param>
        /// <returns>This overload always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <exception cref="System.NotSupportedException">The exception that occurs when this method is called.</exception>
        /// <remarks>
        /// This overload exists to disallow usage of Intersect with a left data source of type
        /// <see cref="System.Linq.ParallelQuery{TSource}"/> and a right data source of type <see cref="System.Collections.Generic.IEnumerable{TSource}"/>.
        /// Otherwise, the Intersect operator would appear to be binding to the parallel implementation,
        /// but would in reality bind to the sequential implementation.
        /// </remarks>
        [Obsolete(RIGHT_SOURCE_NOT_PARALLEL_STR)]
        public static ParallelQuery<TSource> Intersect<TSource>(
            this ParallelQuery<TSource> first, IEnumerable<TSource> second)
        {
            throw new NotSupportedException(SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
        }

        /// <summary>
        /// Produces the set intersection of two parallel sequences by using
        /// the specified IEqualityComparer{T} to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">
        /// A sequence whose distinct elements that also appear in <paramref name="second"/> will be returned.
        /// </param>
        /// <param name="second">
        /// A sequence whose distinct elements that also appear in the first sequence will be returned.
        /// </param>
        /// <param name="comparer">An IEqualityComparer&lt;(Of &lt;(T&gt;)&gt;) to compare values.</param>
        /// <returns>A sequence that contains the elements that form the set intersection of two sequences.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="first"/> or <paramref name="second"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> Intersect<TSource>(
            this ParallelQuery<TSource> first, ParallelQuery<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return new IntersectQueryOperator<TSource>(first, second, comparer);
        }

        /// <summary>
        /// This Intersect overload should never be called.
        /// This method is marked as obsolete and always throws <see cref="System.NotSupportedException"/> when called.
        /// </summary>
        /// <typeparam name="TSource">This type parameter is not used.</typeparam>
        /// <param name="first">This parameter is not used.</param>
        /// <param name="second">This parameter is not used.</param>
        /// <param name="comparer">This parameter is not used.</param>
        /// <returns>This overload always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <exception cref="System.NotSupportedException">The exception that occurs when this method is called.</exception>
        /// <remarks>
        /// This overload exists to disallow usage of Intersect with a left data source of type
        /// <see cref="System.Linq.ParallelQuery{TSource}"/> and a right data source of type <see cref="System.Collections.Generic.IEnumerable{TSource}"/>.
        /// Otherwise, the Intersect operator would appear to be binding to the parallel implementation,
        /// but would in reality bind to the sequential implementation.
        /// </remarks>
        [Obsolete(RIGHT_SOURCE_NOT_PARALLEL_STR)]
        public static ParallelQuery<TSource> Intersect<TSource>(
            this ParallelQuery<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            throw new NotSupportedException(SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
        }

        //-----------------------------------------------------------------------------------
        // Calculates the relative complement of the first and second data sources, that is,
        // the elements in first that are not found in second.
        //

        /// <summary>
        /// Produces the set difference of two parallel sequences by using
        /// the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">
        /// A sequence whose elements that are not also in <paramref name="second"/> will be returned.
        /// </param>
        /// <param name="second">
        /// A sequence whose elements that also occur in the first sequence will cause those
        /// elements to be removed from the returned sequence.
        /// </param>
        /// <returns>A sequence that contains the set difference of the elements of two sequences.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="first"/> or <paramref name="second"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> Except<TSource>(
            this ParallelQuery<TSource> first, ParallelQuery<TSource> second)
        {
            return Except(first, second, null);
        }

        /// <summary>
        /// This Except overload should never be called.
        /// This method is marked as obsolete and always throws <see cref="System.NotSupportedException"/> when called.
        /// </summary>
        /// <typeparam name="TSource">This type parameter is not used.</typeparam>
        /// <param name="first">This parameter is not used.</param>
        /// <param name="second">This parameter is not used.</param>
        /// <returns>This overload always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <exception cref="System.NotSupportedException">The exception that occurs when this method is called.</exception>
        /// <remarks>
        /// This overload exists to disallow usage of Except with a left data source of type
        /// <see cref="System.Linq.ParallelQuery{TSource}"/> and a right data source of type <see cref="System.Collections.Generic.IEnumerable{TSource}"/>.
        /// Otherwise, the Except operator would appear to be binding to the parallel implementation,
        /// but would in reality bind to the sequential implementation.
        /// </remarks>
        [Obsolete(RIGHT_SOURCE_NOT_PARALLEL_STR)]
        public static ParallelQuery<TSource> Except<TSource>(
            this ParallelQuery<TSource> first, IEnumerable<TSource> second)
        {
            throw new NotSupportedException(SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
        }

        /// <summary>
        /// Produces the set difference of two parallel sequences by using the
        /// specified IEqualityComparer{T} to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">A sequence whose elements that are not also in <paramref name="second"/> will be returned.</param>
        /// <param name="second">
        /// A sequence whose elements that also occur in the first sequence will cause those elements
        /// to be removed from the returned sequence.
        /// </param>
        /// <param name="comparer">An IEqualityComparer&lt;(Of &lt;(T&gt;)&gt;) to compare values.</param>
        /// <returns>A sequence that contains the set difference of the elements of two sequences.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="first"/> or <paramref name="second"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> Except<TSource>(
            this ParallelQuery<TSource> first, ParallelQuery<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return new ExceptQueryOperator<TSource>(first, second, comparer);
        }

        /// <summary>
        /// This Except overload should never be called.
        /// This method is marked as obsolete and always throws <see cref="System.NotSupportedException"/> when called.
        /// </summary>
        /// <typeparam name="TSource">This type parameter is not used.</typeparam>
        /// <param name="first">This parameter is not used.</param>
        /// <param name="second">This parameter is not used.</param>
        /// <param name="comparer">This parameter is not used.</param>
        /// <returns>This overload always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <exception cref="System.NotSupportedException">The exception that occurs when this method is called.</exception>
        /// <remarks>
        /// This overload exists to disallow usage of Except with a left data source of type
        /// <see cref="System.Linq.ParallelQuery{TSource}"/> and a right data source of type <see cref="System.Collections.Generic.IEnumerable{TSource}"/>.
        /// Otherwise, the Except operator would appear to be binding to the parallel implementation,
        /// but would in reality bind to the sequential implementation.
        /// </remarks>
        [Obsolete(RIGHT_SOURCE_NOT_PARALLEL_STR)]
        public static ParallelQuery<TSource> Except<TSource>(
            this ParallelQuery<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            throw new NotSupportedException(SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
        }

        /*===================================================================================
         * DATA TYPE CONVERSION OPERATORS
         *===================================================================================*/

        //-----------------------------------------------------------------------------------
        // For compatibility with LINQ. Changes the static type to be less specific if needed.
        //

        /// <summary>
        /// Converts a <see cref="ParallelQuery{T}"/> into an
        /// <see cref="System.Collections.Generic.IEnumerable{T}"/> to force sequential
        /// evaluation of the query.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to type as <see cref="System.Collections.Generic.IEnumerable{T}"/>.</param>
        /// <returns>The input sequence types as <see cref="System.Collections.Generic.IEnumerable{T}"/>.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static IEnumerable<TSource> AsEnumerable<TSource>(this ParallelQuery<TSource> source)
        {
            return AsSequential(source);
        }

        //-----------------------------------------------------------------------------------
        // Simply generates a single-dimensional array containing the elements from the
        // provided enumerable object.
        //

        /// <summary>
        /// Creates an array from a ParallelQuery{T}.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence to create an array from.</param>
        /// <returns>An array that contains the elements from the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource[] ToArray<TSource>(this ParallelQuery<TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (source is QueryOperator<TSource> asOperator)
            {
                return asOperator.ExecuteAndGetResultsAsArray();
            }

            return ToList<TSource>(source).ToArray<TSource>();
        }

        //-----------------------------------------------------------------------------------
        // The ToList method is similar to the ToArray methods above, except that they return
        // List<TSource> objects. An overload is provided to specify the length, if desired.
        //

        /// <summary>
        /// Creates a List{T} from an ParallelQuery{T}.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence to create a List&lt;(Of &lt;(T&gt;)&gt;) from.</param>
        /// <returns>A List&lt;(Of &lt;(T&gt;)&gt;) that contains elements from the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static List<TSource> ToList<TSource>(this ParallelQuery<TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // Allocate a growable list (optionally passing the length as the initial size).
            List<TSource> list = new List<TSource>();
            IEnumerator<TSource> input;

            if (source is QueryOperator<TSource> asOperator)
            {
                if (asOperator.OrdinalIndexState == OrdinalIndexState.Indexable && asOperator.OutputOrdered)
                {
                    // If the query is indexable and the output is ordered, we will use the array-based merge.
                    // That way, we avoid the ordering overhead. Due to limitations of the List<> class, the
                    // most efficient solution seems to be to first dump all results into the array, and then
                    // copy them over into a List<>.
                    //
                    // The issue is that we cannot efficiently construct a List<> with a fixed size. We can
                    // construct a List<> with a fixed *capacity*, but we still need to call Add() N times
                    // in order to be able to index into the List<>.
                    return new List<TSource>(ToArray<TSource>(source));
                }

                // We will enumerate the list w/out pipelining.

                // @PERF: there are likely some cases, e.g. for very large data sets,
                //     where we want to use pipelining for this operation. It can reduce memory
                //     usage since, as we enumerate w/ pipelining off, we're already accumulating
                //     results into a buffer. As a matter of fact, there's probably a way we can
                //     just directly use that buffer below instead of creating a new list.

                input = asOperator.GetEnumerator(ParallelMergeOptions.FullyBuffered);
            }
            else
            {
                input = source.GetEnumerator();
            }

            // Now, accumulate the results into a dynamically sized list, stopping if we reach
            // the (optionally specified) maximum length.
            Debug.Assert(input != null);
            using (input)
            {
                while (input.MoveNext())
                {
                    list.Add(input.Current);
                }
            }

            return list;
        }

        //-----------------------------------------------------------------------------------
        // ToDictionary constructs a dictionary from an instance of ParallelQuery.
        // Each element in the enumerable is converted to a (key,value) pair using a pair
        // of lambda expressions specified by the caller. Different elements must produce
        // different keys or else ArgumentException is thrown.
        //

        /// <summary>
        /// Creates a Dictionary{TKey,TValue} from a ParallelQuery{T} according to
        /// a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence to create a Dictionary&lt;(Of &lt;(TKey, TValue&gt;)&gt;) from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>A Dictionary&lt;(Of &lt;(TKey, TValue&gt;)&gt;) that contains keys and values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// <paramref name="keySelector"/> produces a key that is a null reference (Nothing in Visual Basic).
        /// -or-
        /// <paramref name="keySelector"/> produces duplicate keys for two elements.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector) where TKey : notnull
        {
            return ToDictionary(source, keySelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Creates a Dictionary{TKey,TValue} from a ParallelQuery{T} according to a
        /// specified key selector function and key comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence to create a Dictionary&lt;(Of &lt;(TKey, TValue&gt;)&gt;) from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An IEqualityComparer&lt;(Of &lt;(T&gt;)&gt;) to compare keys.</param>
        /// <returns>A Dictionary&lt;(Of &lt;(TKey, TValue&gt;)&gt;) that contains keys and values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// <paramref name="keySelector"/> produces a key that is a null reference (Nothing in Visual Basic).
        /// -or-
        /// <paramref name="keySelector"/> produces duplicate keys for two elements.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            // comparer may be null. In that case, the Dictionary constructor will use the default comparer.
            Dictionary<TKey, TSource> result = new Dictionary<TKey, TSource>(comparer);

            QueryOperator<TSource>? op = source as QueryOperator<TSource>;
            IEnumerator<TSource> input = (op == null) ? source.GetEnumerator() : op.GetEnumerator(ParallelMergeOptions.FullyBuffered, true);

            using (input)
            {
                while (input.MoveNext())
                {
                    TKey key;
                    TSource val = input.Current;
                    try
                    {
                        key = keySelector(val);
                        result.Add(key, val);
                    }
                    catch (Exception ex)
                    {
                        throw new AggregateException(ex);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a Dictionary{TKey,TValue} from a ParallelQuery{T} according to specified
        /// key selector and element selector functions.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">A sequence to create a Dictionary&lt;(Of &lt;(TKey, TValue&gt;)&gt;) from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">
        /// A transform function to produce a result element value from each element.
        /// </param>
        /// <returns>
        /// A Dictionary&lt;(Of &lt;(TKey, TValue&gt;)&gt;) that contains values of type <typeparamref name="TElement"/>
        /// selected from the input sequence
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or <paramref name="elementSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// <paramref name="keySelector"/> produces a key that is a null reference (Nothing in Visual Basic).
        /// -or-
        /// <paramref name="keySelector"/> produces duplicate keys for two elements.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) where TKey : notnull
        {
            return ToDictionary(source, keySelector, elementSelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Creates a Dictionary{TKey,TValue from a ParallelQuery{T} according to a
        /// specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">A sequence to create a Dictionary&lt;(Of &lt;(TKey, TValue&gt;)&gt;) from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element
        /// value from each element.</param>
        /// <param name="comparer">An IEqualityComparer&lt;(Of &lt;(T&gt;)&gt;) to compare keys.</param>
        /// <returns>
        /// A Dictionary&lt;(Of &lt;(TKey, TValue&gt;)&gt;) that contains values of type <typeparamref name="TElement"/>
        /// selected from the input sequence
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or <paramref name="elementSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// <paramref name="keySelector"/> produces a key that is a null reference (Nothing in Visual Basic).
        /// -or-
        /// <paramref name="keySelector"/> produces duplicate keys for two elements.
        /// -or-
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector == null) throw new ArgumentNullException(nameof(elementSelector));

            // comparer may be null. In that case, the Dictionary constructor will use the default comparer.
            Dictionary<TKey, TElement> result = new Dictionary<TKey, TElement>(comparer);

            QueryOperator<TSource>? op = source as QueryOperator<TSource>;
            IEnumerator<TSource> input = (op == null) ? source.GetEnumerator() : op.GetEnumerator(ParallelMergeOptions.FullyBuffered, true);

            using (input)
            {
                while (input.MoveNext())
                {
                    TSource src = input.Current;

                    try
                    {
                        result.Add(keySelector(src), elementSelector(src));
                    }
                    catch (Exception ex)
                    {
                        throw new AggregateException(ex);
                    }
                }
            }

            return result;
        }

        //-----------------------------------------------------------------------------------
        // ToLookup constructs a lookup from an instance of ParallelQuery.
        // Each element in the enumerable is converted to a (key,value) pair using a pair
        // of lambda expressions specified by the caller. Multiple elements are allowed
        // to produce the same key.
        //

        /// <summary>
        /// Creates an ILookup{TKey,T} from a ParallelQuery{T} according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The sequence to create a Lookup&lt;(Of &lt;(TKey, TElement&gt;)&gt;) from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>A Lookup&lt;(Of &lt;(TKey, TElement&gt;)&gt;) that contains keys and values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector) where TKey: notnull
        {
            return ToLookup(source, keySelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Creates an ILookup{TKey,T} from a ParallelQuery{T} according to a specified
        /// key selector function and key comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The sequence to create a Lookup&lt;(Of &lt;(TKey, TElement&gt;)&gt;) from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An IEqualityComparer&lt;(Of &lt;(T&gt;)&gt;) to compare keys.</param>
        /// <returns>A Lookup&lt;(Of &lt;(TKey, TElement&gt;)&gt;) that contains keys and values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer) where TKey: notnull
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            // comparer may be null, in which case we use the default comparer.
            comparer = comparer ?? EqualityComparer<TKey>.Default;

            ParallelQuery<IGrouping<TKey, TSource>> groupings = source.GroupBy(keySelector, comparer);

            Parallel.Lookup<TKey, TSource> lookup = new Parallel.Lookup<TKey, TSource>(comparer);

            Debug.Assert(groupings is QueryOperator<IGrouping<TKey, TSource>>);
            QueryOperator<IGrouping<TKey, TSource>>? op = groupings as QueryOperator<IGrouping<TKey, TSource>>;

            IEnumerator<IGrouping<TKey, TSource>> input = (op == null) ? groupings.GetEnumerator() : op.GetEnumerator(ParallelMergeOptions.FullyBuffered);

            using (input)
            {
                while (input.MoveNext())
                {
                    lookup.Add(input.Current);
                }
            }

            return lookup;
        }

        /// <summary>
        /// Creates an ILookup{TKey,TElement} from a ParallelQuery{T} according to specified
        /// key selector and element selector functions.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">The sequence to create a Lookup&lt;(Of &lt;(TKey, TElement&gt;)&gt;) from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">
        /// A transform function to produce a result element value from each element.
        /// </param>
        /// <returns>
        /// A Lookup&lt;(Of &lt;(TKey, TElement&gt;)&gt;) that contains values of type TElement
        /// selected from the input sequence.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or <paramref name="elementSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) where TKey : notnull
        {
            return ToLookup(source, keySelector, elementSelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Creates an ILookup{TKey,TElement} from a ParallelQuery{T} according to
        /// a specified key selector function, a comparer and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">The sequence to create a Lookup&lt;(Of &lt;(TKey, TElement&gt;)&gt;) from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">
        /// A transform function to produce a result element value from each element.
        /// </param>
        /// <param name="comparer">An IEqualityComparer&lt;(Of &lt;(T&gt;)&gt;) to compare keys.</param>
        /// <returns>
        /// A Lookup&lt;(Of &lt;(TKey, TElement&gt;)&gt;) that contains values of type TElement selected
        /// from the input sequence.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or <paramref name="elementSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(
            this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector == null) throw new ArgumentNullException(nameof(elementSelector));

            // comparer may be null, in which case we use the default comparer.
            comparer = comparer ?? EqualityComparer<TKey>.Default;

            ParallelQuery<IGrouping<TKey, TElement>> groupings = source.GroupBy(keySelector, elementSelector, comparer);

            Parallel.Lookup<TKey, TElement> lookup = new Parallel.Lookup<TKey, TElement>(comparer);

            Debug.Assert(groupings is QueryOperator<IGrouping<TKey, TElement>>);
            QueryOperator<IGrouping<TKey, TElement>>? op = groupings as QueryOperator<IGrouping<TKey, TElement>>;

            IEnumerator<IGrouping<TKey, TElement>> input = (op == null) ? groupings.GetEnumerator() : op.GetEnumerator(ParallelMergeOptions.FullyBuffered);

            using (input)
            {
                while (input.MoveNext())
                {
                    lookup.Add(input.Current);
                }
            }

            return lookup;
        }

        /*===================================================================================
         * MISCELLANEOUS OPERATORS
         *===================================================================================*/

        //-----------------------------------------------------------------------------------
        // Reverses the input.
        //

        /// <summary>
        /// Inverts the order of the elements in a parallel sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to reverse.</param>
        /// <returns>A sequence whose elements correspond to those of the input sequence in reverse order.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> Reverse<TSource>(this ParallelQuery<TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new ReverseQueryOperator<TSource>(source);
        }

        //-----------------------------------------------------------------------------------
        // Both OfType and Cast convert a weakly typed stream to a strongly typed one:
        // the difference is that OfType filters out elements that aren't of the given type,
        // while Cast forces the cast, possibly resulting in InvalidCastExceptions.
        //

        /// <summary>
        /// Filters the elements of a ParallelQuery based on a specified type.
        /// </summary>
        /// <typeparam name="TResult">The type to filter the elements of the sequence on.</typeparam>
        /// <param name="source">The sequence whose elements to filter.</param>
        /// <returns>A sequence that contains elements from the input sequence of type <typeparamref name="TResult"/>.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> OfType<TResult>(this ParallelQuery source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return source.OfType<TResult>();
        }

        /// <summary>
        /// Converts the elements of a weakly-typed ParallelQuery to the specified stronger type.
        /// </summary>
        /// <typeparam name="TResult">The stronger type to convert the elements of <paramref name="source"/> to.</typeparam>
        /// <param name="source">The sequence that contains the weakly typed elements to be converted.</param>
        /// <returns>
        /// A sequence that contains each weakly-type element of the source sequence converted to the specified stronger type.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TResult> Cast<TResult>(this ParallelQuery source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return source.Cast<TResult>();
        }

        //-----------------------------------------------------------------------------------
        // Helper method used by First, FirstOrDefault, Last, LastOrDefault, Single, and
        // SingleOrDefault below.  This takes a query operator, gets the first item (and
        // either checks or asserts there is at most one item in the source), and returns it.
        // If there are no elements, the method either throws an exception or, if
        // defaultIfEmpty is true, returns a default value.
        //
        // Arguments:
        //     queryOp        - the query operator to enumerate (for the single element)
        //     throwIfTwo     - whether to throw an exception (true) or assert (false) that
        //                      there is no more than one element in the source
        //     defaultIfEmpty - whether to return a default value (true) or throw an
        //                      exception if the output of the query operator is empty
        //
        private static TSource? GetOneWithPossibleDefault<TSource>(
            QueryOperator<TSource> queryOp, bool throwIfTwo, bool defaultIfEmpty)
        {
            Debug.Assert(queryOp != null, "expected query operator");

            using (IEnumerator<TSource> e = queryOp.GetEnumerator(ParallelMergeOptions.FullyBuffered))
            {
                if (e.MoveNext())
                {
                    TSource current = e.Current;

                    // Some operators need to do a runtime, retail check for more than one element.
                    // Others can simply assert that there was only one.
                    if (throwIfTwo)
                    {
                        if (e.MoveNext())
                        {
                            throw new InvalidOperationException(SR.MoreThanOneMatch);
                        }
                    }
                    else
                    {
                        Debug.Assert(!e.MoveNext(), "expected only a single element");
                    }

                    return current;
                }
            }

            if (defaultIfEmpty)
            {
                return default;
            }
            else
            {
                throw new InvalidOperationException(SR.NoElements);
            }
        }

        //-----------------------------------------------------------------------------------
        // First simply returns the first element from the data source; if the predicate
        // overload is used, the first element satisfying the predicate is returned.
        // An exception is thrown for empty data sources. Alternatively, the FirstOrDefault
        // method can be used which returns default(T) if empty (or no elements satisfy the
        // predicate).
        //

        /// <summary>
        /// Returns the first element of a parallel sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <returns>The first element in the specified sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource First<TSource>(this ParallelQuery<TSource> source)
        {
            // @PERF: optimize for seekable data sources.  E.g. if an array, we can
            //     seek directly to the 0th element.
            if (source == null) throw new ArgumentNullException(nameof(source));

            FirstQueryOperator<TSource> queryOp = new FirstQueryOperator<TSource>(source, null);

            // If in conservative mode and a premature merge would be inserted by the First operator,
            // run the whole query sequentially.
            QuerySettings settings = queryOp.SpecifiedQuerySettings.WithDefaults();
            if (queryOp.LimitsParallelism && settings.ExecutionMode != ParallelExecutionMode.ForceParallelism)
            {
                IEnumerable<TSource> childAsSequential = queryOp.Child.AsSequentialQuery(settings.CancellationState.ExternalCancellationToken);
                IEnumerable<TSource> childWithCancelChecks = CancellableEnumerable.Wrap(childAsSequential, settings.CancellationState.ExternalCancellationToken);
                return ExceptionAggregator.WrapEnumerable(childWithCancelChecks, settings.CancellationState)
                    .First();
            }

            return GetOneWithPossibleDefault(queryOp, false, false)!;
        }

        /// <summary>
        /// Returns the first element in a parallel sequence that satisfies a specified condition.
        /// </summary>
        /// <remarks>There's a temporary difference from LINQ to Objects, this does not throw
        /// ArgumentNullException when the predicate is null.</remarks>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>The first element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// No element in <paramref name="source"/> satisfies the condition in <paramref name="predicate"/>.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource First<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            FirstQueryOperator<TSource> queryOp = new FirstQueryOperator<TSource>(source, predicate);

            // If in conservative mode and a premature merge would be inserted by the First operator,
            // run the whole query sequentially.
            QuerySettings settings = queryOp.SpecifiedQuerySettings.WithDefaults();
            if (queryOp.LimitsParallelism && settings.ExecutionMode != ParallelExecutionMode.ForceParallelism)
            {
                IEnumerable<TSource> childAsSequential = queryOp.Child.AsSequentialQuery(settings.CancellationState.ExternalCancellationToken);
                IEnumerable<TSource> childWithCancelChecks = CancellableEnumerable.Wrap(childAsSequential, settings.CancellationState.ExternalCancellationToken);
                return ExceptionAggregator.WrapEnumerable(childWithCancelChecks, settings.CancellationState)
                    .First(ExceptionAggregator.WrapFunc<TSource, bool>(predicate, settings.CancellationState));
            }

            return GetOneWithPossibleDefault(queryOp, false, false)!;
        }

        /// <summary>
        /// Returns the first element of a parallel sequence, or a default value if the
        /// sequence contains no elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <returns>
        /// default(<B>TSource</B>) if <paramref name="source"/> is empty; otherwise, the first element in <paramref name="source"/>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource? FirstOrDefault<TSource>(this ParallelQuery<TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // @PERF: optimize for seekable data sources.  E.g. if an array, we can
            //     seek directly to the 0th element.
            FirstQueryOperator<TSource> queryOp = new FirstQueryOperator<TSource>(source, null);

            // If in conservative mode and a premature merge would be inserted by the First operator,
            // run the whole query sequentially.
            QuerySettings settings = queryOp.SpecifiedQuerySettings.WithDefaults();
            if (queryOp.LimitsParallelism
                && settings.ExecutionMode != ParallelExecutionMode.ForceParallelism)
            {
                IEnumerable<TSource> childAsSequential = queryOp.Child.AsSequentialQuery(settings.CancellationState.ExternalCancellationToken);
                IEnumerable<TSource> childWithCancelChecks = CancellableEnumerable.Wrap(childAsSequential, settings.CancellationState.ExternalCancellationToken);
                return ExceptionAggregator.WrapEnumerable(childWithCancelChecks,
                    settings.CancellationState).FirstOrDefault();
            }

            return GetOneWithPossibleDefault(queryOp, false, true);
        }

        /// <summary>
        /// Returns the first element of the parallel sequence that satisfies a condition or a
        /// default value if no such element is found.
        /// </summary>
        /// <remarks>There's a temporary difference from LINQ to Objects, this does not throw
        /// ArgumentNullException when the predicate is null.</remarks>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// default(<B>TSource</B>) if <paramref name="source"/> is empty or if no element passes the test
        /// specified by <B>predicate</B>; otherwise, the first element in <paramref name="source"/> that
        /// passes the test specified by <B>predicate</B>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource? FirstOrDefault<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            FirstQueryOperator<TSource> queryOp = new FirstQueryOperator<TSource>(source, predicate);

            // If in conservative mode and a premature merge would be inserted by the First operator,
            // run the whole query sequentially.
            QuerySettings settings = queryOp.SpecifiedQuerySettings.WithDefaults();
            if (queryOp.LimitsParallelism
                && settings.ExecutionMode != ParallelExecutionMode.ForceParallelism)
            {
                IEnumerable<TSource> childAsSequential = queryOp.Child.AsSequentialQuery(settings.CancellationState.ExternalCancellationToken);
                IEnumerable<TSource> childWithCancelChecks = CancellableEnumerable.Wrap(childAsSequential, settings.CancellationState.ExternalCancellationToken);
                return ExceptionAggregator.WrapEnumerable(
                    childWithCancelChecks, settings.CancellationState)
                    .FirstOrDefault(ExceptionAggregator.WrapFunc<TSource, bool>(predicate, settings.CancellationState));
            }

            return GetOneWithPossibleDefault(queryOp, false, true);
        }

        //-----------------------------------------------------------------------------------
        // Last simply returns the last element from the data source; if the predicate
        // overload is used, the last element satisfying the predicate is returned.
        // An exception is thrown for empty data sources. Alternatively, the LastOrDefault
        // method can be used which returns default(T) if empty (or no elements satisfy the
        // predicate).
        //

        /// <summary>
        /// Returns the last element of a parallel sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the last element from.</param>
        /// <returns>The value at the last position in the source sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="source"/> contains no elements.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource Last<TSource>(this ParallelQuery<TSource> source)
        {
            // @PERF: optimize for seekable data sources.  E.g. if an array, we can
            //     seek directly to the last element.
            if (source == null) throw new ArgumentNullException(nameof(source));

            LastQueryOperator<TSource> queryOp = new LastQueryOperator<TSource>(source, null);

            // If in conservative mode and a premature merge would be inserted by the First operator,
            // run the whole query sequentially.
            QuerySettings settings = queryOp.SpecifiedQuerySettings.WithDefaults();
            if (queryOp.LimitsParallelism
                && settings.ExecutionMode != ParallelExecutionMode.ForceParallelism)
            {
                IEnumerable<TSource> childAsSequential = queryOp.Child.AsSequentialQuery(settings.CancellationState.ExternalCancellationToken);
                IEnumerable<TSource> childWithCancelChecks = CancellableEnumerable.Wrap(childAsSequential, settings.CancellationState.ExternalCancellationToken);
                return ExceptionAggregator.WrapEnumerable(childWithCancelChecks, settings.CancellationState).Last();
            }

            return GetOneWithPossibleDefault(queryOp, false, false)!;
        }

        /// <summary>
        /// Returns the last element of a parallel sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// The last element in the sequence that passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// No element in <paramref name="source"/> satisfies the condition in <paramref name="predicate"/>.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource Last<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            LastQueryOperator<TSource> queryOp = new LastQueryOperator<TSource>(source, predicate);

            // If in conservative mode and a premature merge would be inserted by the First operator,
            // run the whole query sequentially.
            QuerySettings settings = queryOp.SpecifiedQuerySettings.WithDefaults();
            if (queryOp.LimitsParallelism
                && settings.ExecutionMode != ParallelExecutionMode.ForceParallelism)
            {
                IEnumerable<TSource> childAsSequential = queryOp.Child.AsSequentialQuery(settings.CancellationState.ExternalCancellationToken);
                IEnumerable<TSource> childWithCancelChecks = CancellableEnumerable.Wrap(childAsSequential, settings.CancellationState.ExternalCancellationToken);
                return ExceptionAggregator.WrapEnumerable(
                    childWithCancelChecks, settings.CancellationState)
                    .Last(ExceptionAggregator.WrapFunc<TSource, bool>(predicate, settings.CancellationState));
            }

            return GetOneWithPossibleDefault(queryOp, false, false)!;
        }

        /// <summary>
        /// Returns the last element of a parallel sequence, or a default value if the
        /// sequence contains no elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return an element from.</param>
        /// <returns>
        /// default(<typeparamref name="TSource"/>) if the source sequence is empty; otherwise, the last element in the sequence.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource? LastOrDefault<TSource>(this ParallelQuery<TSource> source)
        {
            // @PERF: optimize for seekable data sources.  E.g. if an array, we can
            //     seek directly to the last element.
            if (source == null) throw new ArgumentNullException(nameof(source));

            LastQueryOperator<TSource> queryOp = new LastQueryOperator<TSource>(source, null);

            // If in conservative mode and a premature merge would be inserted by the First operator,
            // run the whole query sequentially.
            QuerySettings settings = queryOp.SpecifiedQuerySettings.WithDefaults();
            if (queryOp.LimitsParallelism
                && settings.ExecutionMode != ParallelExecutionMode.ForceParallelism)
            {
                IEnumerable<TSource> childAsSequential = queryOp.Child.AsSequentialQuery(settings.CancellationState.ExternalCancellationToken);
                IEnumerable<TSource> childWithCancelChecks = CancellableEnumerable.Wrap(childAsSequential, settings.CancellationState.ExternalCancellationToken);
                return ExceptionAggregator.WrapEnumerable(childWithCancelChecks, settings.CancellationState).LastOrDefault();
            }

            return GetOneWithPossibleDefault(queryOp, false, true);
        }

        /// <summary>
        /// Returns the last element of a parallel sequence that satisfies a condition, or
        /// a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// default(<typeparamref name="TSource"/>) if the sequence is empty or if no elements pass the test in the
        /// predicate function; otherwise, the last element that passes the test in the predicate function.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource? LastOrDefault<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            LastQueryOperator<TSource> queryOp = new LastQueryOperator<TSource>(source, predicate);

            // If in conservative mode and a premature merge would be inserted by the First operator,
            // run the whole query sequentially.
            QuerySettings settings = queryOp.SpecifiedQuerySettings.WithDefaults();
            if (queryOp.LimitsParallelism
                && settings.ExecutionMode != ParallelExecutionMode.ForceParallelism)
            {
                IEnumerable<TSource> childAsSequential = queryOp.Child.AsSequentialQuery(settings.CancellationState.ExternalCancellationToken);
                IEnumerable<TSource> childWithCancelChecks = CancellableEnumerable.Wrap(childAsSequential, settings.CancellationState.ExternalCancellationToken);
                return ExceptionAggregator.WrapEnumerable(
                    childWithCancelChecks, settings.CancellationState)
                    .LastOrDefault(ExceptionAggregator.WrapFunc<TSource, bool>(predicate, settings.CancellationState));
            }

            return GetOneWithPossibleDefault(queryOp, false, true);
        }

        //-----------------------------------------------------------------------------------
        // Single yields the single element matching the optional predicate, or throws an
        // exception if there is zero or more than one match. SingleOrDefault is similar
        // except that it returns the default value in this condition.
        //

        /// <summary>
        /// Returns the only element of a parallel sequence, and throws an exception if there is not
        /// exactly one element in the sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <returns>The single element of the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// The input sequence contains more than one element. -or- The input sequence is empty.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource Single<TSource>(this ParallelQuery<TSource> source)
        {
            // @PERF: optimize for ICollection-typed data sources, i.e. we can just
            //     check the Count property and avoid costly fork/join/synchronization.
            if (source == null) throw new ArgumentNullException(nameof(source));

            return GetOneWithPossibleDefault(new SingleQueryOperator<TSource>(source, null), true, false)!;
        }

        /// <summary>
        /// Returns the only element of a parallel sequence that satisfies a specified condition,
        /// and throws an exception if more than one such element exists.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <returns>The single element of the input sequence that satisfies a condition.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// No element satisfies the condition in <paramref name="predicate"/>. -or- More than one element satisfies the condition in <paramref name="predicate"/>.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource Single<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return GetOneWithPossibleDefault(new SingleQueryOperator<TSource>(source, predicate), true, false)!;
        }

        /// <summary>
        /// Returns the only element of a parallel sequence, or a default value if the sequence is
        /// empty; this method throws an exception if there is more than one element in the sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <returns>
        /// The single element of the input sequence, or default(<typeparamref name="TSource"/>) if the
        /// sequence contains no elements.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource? SingleOrDefault<TSource>(this ParallelQuery<TSource> source)
        {
            // @PERF: optimize for ICollection-typed data sources, i.e. we can just
            //     check the Count property and avoid costly fork/join/synchronization.
            if (source == null) throw new ArgumentNullException(nameof(source));

            return GetOneWithPossibleDefault(new SingleQueryOperator<TSource>(source, null), true, true);
        }

        /// <summary>
        /// Returns the only element of a parallel sequence that satisfies a specified condition
        /// or a default value if no such element exists; this method throws an exception
        /// if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <returns>
        /// The single element of the input sequence that satisfies the condition, or
        /// default(<typeparamref name="TSource"/>) if no such element is found.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource? SingleOrDefault<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return GetOneWithPossibleDefault(new SingleQueryOperator<TSource>(source, predicate), true, true);
        }

        //-----------------------------------------------------------------------------------
        // DefaultIfEmpty yields the data source, unmodified, if it is non-empty. Otherwise,
        // it yields a single occurrence of the default value.
        //

        /// <summary>
        /// Returns the elements of the specified parallel sequence or the type parameter's
        /// default value in a singleton collection if the sequence is empty.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return a default value for if it is empty.</param>
        /// <returns>
        /// A sequence that contains default(<B>TSource</B>) if <paramref name="source"/> is empty; otherwise, <paramref name="source"/>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource?> DefaultIfEmpty<TSource>(this ParallelQuery<TSource> source)
        {
            return DefaultIfEmpty<TSource>(source, default!)!;
        }

        /// <summary>
        /// Returns the elements of the specified parallel sequence or the specified value
        /// in a singleton collection if the sequence is empty.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the specified value for if it is empty.</param>
        /// <param name="defaultValue">The value to return if the sequence is empty.</param>
        /// <returns>
        /// A sequence that contains <B>defaultValue</B> if <paramref name="source"/> is empty; otherwise, <paramref name="source"/>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static ParallelQuery<TSource> DefaultIfEmpty<TSource>(this ParallelQuery<TSource> source, TSource defaultValue)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new DefaultIfEmptyQueryOperator<TSource>(source, defaultValue);
        }

        //-----------------------------------------------------------------------------------
        // ElementAt yields an element at a specific index.  If the data source doesn't
        // contain such an element, an exception is thrown.  Alternatively, ElementAtOrDefault
        // will return a default value if the given index is invalid.
        //

        /// <summary>
        /// Returns the element at a specified index in a parallel sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence to return an element from.</param>
        /// <param name="index">The zero-based index of the element to retrieve.</param>
        /// <returns>The element at the specified position in the source sequence.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="index"/> is less than 0 or greater than or equal to the number of elements in <paramref name="source"/>.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource ElementAt<TSource>(this ParallelQuery<TSource> source, int index)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

            // @PERF: there are obvious optimization opportunities for indexable data sources,
            //          since we can just seek to the element requested.

            ElementAtQueryOperator<TSource> op = new ElementAtQueryOperator<TSource>(source, index);

            TSource result;
            if (op.Aggregate(out result!, false))
            {
                return result;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        /// <summary>
        /// Returns the element at a specified index in a parallel sequence or a default value if the
        /// index is out of range.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence to return an element from.</param>
        /// <param name="index">The zero-based index of the element to retrieve.</param>
        /// <returns>
        /// default(<B>TSource</B>) if the index is outside the bounds of the source sequence;
        /// otherwise, the element at the specified position in the source sequence.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="source"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// One or more exceptions occurred during the evaluation of the query.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// The query was canceled.
        /// </exception>
        public static TSource? ElementAtOrDefault<TSource>(this ParallelQuery<TSource> source, int index)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // @PERF: there are obvious optimization opportunities for indexable data sources,
            //          since we can just seek to the element requested.

            if (index >= 0)
            {
                ElementAtQueryOperator<TSource> op = new ElementAtQueryOperator<TSource>(source, index);

                TSource result;
                if (op.Aggregate(out result!, true))
                {
                    return result;
                }
            }

            return default;
        }
    }
}

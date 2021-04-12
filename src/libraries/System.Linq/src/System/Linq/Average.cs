// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    /// <summary>Provides a set of <see langword="static" /> (<see langword="Shared" /> in Visual Basic) methods for querying objects that implement <see cref="System.Collections.Generic.IEnumerable{T}" />.</summary>
    /// <remarks>The methods in this class provide an implementation of the standard query operators for querying data sources that implement <see cref="System.Collections.Generic.IEnumerable{T}" />. The standard query operators are general purpose methods that follow the LINQ pattern and enable you to express traversal, filter, and projection operations over data in any .NET-based programming language.
    /// The majority of the methods in this class are defined as extension methods that extend <see cref="System.Collections.Generic.IEnumerable{T}" />. This means they can be called like an instance method on any object that implements <see cref="System.Collections.Generic.IEnumerable{T}" />.
    /// Methods that are used in a query that returns a sequence of values do not consume the target data until the query object is enumerated. This is known as deferred execution. Methods that are used in a query that returns a singleton value execute and consume the target data immediately.</remarks>
    /// <related type="Article" href="https://msdn.microsoft.com/library/24cda21e-8af8-4632-b519-c404a839b9b2">Standard Query Operators Overview</related>
    /// <related type="Article" href="/dotnet/csharp/programming-guide/classes-and-structs/extension-methods">Extension Methods (C# Programming Guide)</related>
    /// <related type="Article" href="/dotnet/visual-basic/programming-guide/language-features/procedures/extension-methods">Extension Methods (Visual Basic)</related>
    public static partial class Enumerable
    {
        /// <summary>Computes the average of a sequence of <see cref="int" /> values.</summary>
        /// <param name="source">A sequence of <see cref="int" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.Average" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.Average(System.Collections.Generic.IEnumerable{int})" /> to calculate an average.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet8":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet8":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double Average(this IEnumerable<int> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<int> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                long sum = e.Current;
                long count = 1;
                checked
                {
                    while (e.MoveNext())
                    {
                        sum += e.Current;
                        ++count;
                    }
                }

                return (double)sum / count;
            }
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="int" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="int" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the source sequence is empty or contains only values that are <see langword="null" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum of the elements in the sequence is larger than <see cref="long.MaxValue" />.</exception>
        /// <remarks>In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.Average" />.</remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double? Average(this IEnumerable<int?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<int?> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    int? v = e.Current;
                    if (v.HasValue)
                    {
                        long sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = e.Current;
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }

                        return (double)sum / count;
                    }
                }
            }

            return null;
        }

        /// <summary>Computes the average of a sequence of <see cref="long" /> values.</summary>
        /// <param name="source">A sequence of <see cref="long" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.Average" />.</remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double Average(this IEnumerable<long> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<long> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                long sum = e.Current;
                long count = 1;
                checked
                {
                    while (e.MoveNext())
                    {
                        sum += e.Current;
                        ++count;
                    }
                }

                return (double)sum / count;
            }
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="long" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="long" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the source sequence is empty or contains only values that are <see langword="null" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum of the elements in the sequence is larger than <see cref="long.MaxValue" />.</exception>
        /// <remarks>In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.Average" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.Average(System.Collections.Generic.IEnumerable{System.Nullable{long}})" /> to calculate an average.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet12":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet12":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double? Average(this IEnumerable<long?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<long?> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    long? v = e.Current;
                    if (v.HasValue)
                    {
                        long sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = e.Current;
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }

                        return (double)sum / count;
                    }
                }
            }

            return null;
        }

        /// <summary>Computes the average of a sequence of <see cref="float" /> values.</summary>
        /// <param name="source">A sequence of <see cref="float" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.Average" />.</remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static float Average(this IEnumerable<float> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<float> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                double sum = e.Current;
                long count = 1;
                while (e.MoveNext())
                {
                    sum += e.Current;
                    ++count;
                }

                return (float)(sum / count);
            }
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="float" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="float" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the source sequence is empty or contains only values that are <see langword="null" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.Average" />.</remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static float? Average(this IEnumerable<float?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<float?> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    float? v = e.Current;
                    if (v.HasValue)
                    {
                        double sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = e.Current;
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }

                        return (float)(sum / count);
                    }
                }
            }

            return null;
        }

        /// <summary>Computes the average of a sequence of <see cref="double" /> values.</summary>
        /// <param name="source">A sequence of <see cref="double" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>If the sum of the elements is too large to represent as a <see cref="double" />, this method returns positive or negative infinity.
        /// In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.Average" />.</remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double Average(this IEnumerable<double> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<double> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                double sum = e.Current;
                long count = 1;
                while (e.MoveNext())
                {
                    // There is an opportunity to short-circuit here, in that if e.Current is
                    // ever NaN then the result will always be NaN. Assuming that this case is
                    // rare enough that not checking is the better approach generally.
                    sum += e.Current;
                    ++count;
                }

                return sum / count;
            }
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="double" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="double" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the source sequence is empty or contains only values that are <see langword="null" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>If the sum of the elements is too large to represent as a <see cref="double" />, this method returns positive or negative infinity.
        /// In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.Average" />.</remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double? Average(this IEnumerable<double?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<double?> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    double? v = e.Current;
                    if (v.HasValue)
                    {
                        double sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = e.Current;
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }

                        return sum / count;
                    }
                }
            }

            return null;
        }

        /// <summary>Computes the average of a sequence of <see cref="decimal" /> values.</summary>
        /// <param name="source">A sequence of <see cref="decimal" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.Average" />.</remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static decimal Average(this IEnumerable<decimal> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<decimal> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                decimal sum = e.Current;
                long count = 1;
                while (e.MoveNext())
                {
                    sum += e.Current;
                    ++count;
                }

                return sum / count;
            }
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="decimal" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="decimal" /> values to calculate the average of.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the source sequence is empty or contains only values that are <see langword="null" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum of the elements in the sequence is larger than <see cref="decimal.MaxValue" />.</exception>
        /// <remarks>In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.Average" />.</remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static decimal? Average(this IEnumerable<decimal?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<decimal?> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    decimal? v = e.Current;
                    if (v.HasValue)
                    {
                        decimal sum = v.GetValueOrDefault();
                        long count = 1;
                        while (e.MoveNext())
                        {
                            v = e.Current;
                            if (v.HasValue)
                            {
                                sum += v.GetValueOrDefault();
                                ++count;
                            }
                        }

                        return sum / count;
                    }
                }
            }

            return null;
        }

        /// <summary>Computes the average of a sequence of <see cref="int" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <exception cref="System.OverflowException">The sum of the elements in the sequence is larger than <see cref="long.MaxValue" />.</exception>
        /// <remarks>In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.Average" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.Average{T}(System.Collections.Generic.IEnumerable{T},System.Func{T,int})" /> to calculate an average.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet18":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet18":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                long sum = selector(e.Current);
                long count = 1;
                checked
                {
                    while (e.MoveNext())
                    {
                        sum += selector(e.Current);
                        ++count;
                    }
                }

                return (double)sum / count;
            }
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="int" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the source sequence is empty or contains only values that are <see langword="null" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum of the elements in the sequence is larger than <see cref="long.MaxValue" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <xref:System.Linq.Enumerable.Average%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Average%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int32%7D%29> to calculate an average.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet18":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet18":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    int? v = selector(e.Current);
                    if (v.HasValue)
                    {
                        long sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = selector(e.Current);
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }

                        return (double)sum / count;
                    }
                }
            }

            return null;
        }

        /// <summary>Computes the average of a sequence of <see cref="long" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <exception cref="System.OverflowException">The sum of the elements in the sequence is larger than <see cref="long.MaxValue" />.</exception>
        /// <remarks>In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.Average" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.Average{T}(System.Collections.Generic.IEnumerable{T},System.Func{T,long})" /> to calculate an average.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet16":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet16":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                long sum = selector(e.Current);
                long count = 1;
                checked
                {
                    while (e.MoveNext())
                    {
                        sum += selector(e.Current);
                        ++count;
                    }
                }

                return (double)sum / count;
            }
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="long" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the source sequence is empty or contains only values that are <see langword="null" />.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <xref:System.Linq.Enumerable.Average%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Average%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int64%7D%29> to calculate an average.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet16":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet16":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    long? v = selector(e.Current);
                    if (v.HasValue)
                    {
                        long sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = selector(e.Current);
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }

                        return (double)sum / count;
                    }
                }
            }

            return null;
        }

        /// <summary>Computes the average of a sequence of <see cref="float" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <xref:System.Linq.Enumerable.Average%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Average%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int32%7D%29> to calculate an average.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet18":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet18":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static float Average<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                double sum = selector(e.Current);
                long count = 1;
                while (e.MoveNext())
                {
                    sum += selector(e.Current);
                    ++count;
                }

                return (float)(sum / count);
            }
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="float" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the source sequence is empty or contains only values that are <see langword="null" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <xref:System.Linq.Enumerable.Average%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Average%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int32%7D%29> to calculate an average.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet18":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet18":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static float? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    float? v = selector(e.Current);
                    if (v.HasValue)
                    {
                        double sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = selector(e.Current);
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }

                        return (float)(sum / count);
                    }
                }
            }

            return null;
        }

        /// <summary>Computes the average of a sequence of <see cref="double" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <xref:System.Linq.Enumerable.Average%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Average%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int64%7D%29> to calculate an average.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet16":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet16":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                double sum = selector(e.Current);
                long count = 1;
                while (e.MoveNext())
                {
                    // There is an opportunity to short-circuit here, in that if e.Current is
                    // ever NaN then the result will always be NaN. Assuming that this case is
                    // rare enough that not checking is the better approach generally.
                    sum += selector(e.Current);
                    ++count;
                }

                return sum / count;
            }
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="double" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the source sequence is empty or contains only values that are <see langword="null" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <xref:System.Linq.Enumerable.Average%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Average%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int64%7D%29> to calculate an average.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet16":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet16":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    double? v = selector(e.Current);
                    if (v.HasValue)
                    {
                        double sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = selector(e.Current);
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }

                        return sum / count;
                    }
                }
            }

            return null;
        }

        /// <summary>Computes the average of a sequence of <see cref="decimal" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values that are used to calculate an average.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <exception cref="System.OverflowException">The sum of the elements in the sequence is larger than <see cref="decimal.MaxValue" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <xref:System.Linq.Enumerable.Average%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Average%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int64%7D%29> to calculate an average.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet16":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet16":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static decimal Average<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                decimal sum = selector(e.Current);
                long count = 1;
                while (e.MoveNext())
                {
                    sum += selector(e.Current);
                    ++count;
                }

                return sum / count;
            }
        }

        /// <summary>Computes the average of a sequence of nullable <see cref="decimal" /> values that are obtained by invoking a transform function on each element of the input sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The average of the sequence of values, or <see langword="null" /> if the source sequence is empty or contains only values that are <see langword="null" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The sum of the elements in the sequence is larger than <see cref="decimal.MaxValue" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// In Visual Basic query expression syntax, an `Aggregate Into Average()` clause translates to an invocation of <xref:System.Linq.Enumerable.Average%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Average%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int64%7D%29> to calculate an average.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet16":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet16":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static decimal? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    decimal? v = selector(e.Current);
                    if (v.HasValue)
                    {
                        decimal sum = v.GetValueOrDefault();
                        long count = 1;
                        while (e.MoveNext())
                        {
                            v = selector(e.Current);
                            if (v.HasValue)
                            {
                                sum += v.GetValueOrDefault();
                                ++count;
                            }
                        }

                        return sum / count;
                    }
                }
            }

            return null;
        }
    }
}

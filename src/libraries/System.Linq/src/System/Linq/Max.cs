// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>Returns the maximum value in a sequence of <see cref="int" /> values.</summary>
        /// <param name="source">A sequence of <see cref="int" /> values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>
        /// <para>The <see cref="Max(IEnumerable{int})" /> method uses the <see cref="int" /> implementation of <see cref="System.IComparable{T}" /> to compare values.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <see cref="O:Enumerable.Max" />.</para>
        /// </remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static int Max(this IEnumerable<int> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            int value;
            using (IEnumerator<int> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = e.Current;
                while (e.MoveNext())
                {
                    int x = e.Current;
                    if (x > value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        /// <summary>Returns the maximum value in a sequence of nullable <see cref="int" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="int" /> values to determine the maximum value of.</param>
        /// <returns>A value of type <c>Nullable&lt;Int32&gt;</c> in C# or <c>Nullable(Of Int32)</c> in Visual Basic that corresponds to the maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="Max(IEnumerable{System.Nullable{int}})" /> method uses the <see cref="int" /> implementation of <see cref="System.IComparable{T}" /> to compare values.</para>
        /// <para>If the source sequence is empty or contains only values that are <see langword="null" />, this function returns <see langword="null" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <see cref="O:Enumerable.Max" />.</para>
        /// </remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static int? Max(this IEnumerable<int?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            int? value = null;
            using (IEnumerator<int?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = e.Current;
                }
                while (!value.HasValue);

                int valueVal = value.GetValueOrDefault();
                if (valueVal >= 0)
                {
                    // We can fast-path this case where we know HasValue will
                    // never affect the outcome, without constantly checking
                    // if we're in such a state. Similar fast-paths could
                    // be done for other cases, but as all-positive
                    // or mostly-positive integer values are quite common in real-world
                    // uses, it's only been done in this direction for int? and long?.
                    while (e.MoveNext())
                    {
                        int? cur = e.Current;
                        int x = cur.GetValueOrDefault();
                        if (x > valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                    }
                }
                else
                {
                    while (e.MoveNext())
                    {
                        int? cur = e.Current;
                        int x = cur.GetValueOrDefault();

                        // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                        // unless nulls either never happen or always happen.
                        if (cur.HasValue & x > valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                    }
                }
            }

            return value;
        }

        /// <summary>Returns the maximum value in a sequence of <see cref="long" /> values.</summary>
        /// <param name="source">A sequence of <see cref="long" /> values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>
        /// <para>The <see cref="Max(IEnumerable{long})" /> method uses the <see cref="long" /> implementation of <see cref="System.IComparable{T}" /> to compare values.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <see cref="O:Enumerable.Max" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="Max(IEnumerable{long})" /> to determine the maximum value in a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet52":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet52":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static long Max(this IEnumerable<long> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            long value;
            using (IEnumerator<long> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = e.Current;
                while (e.MoveNext())
                {
                    long x = e.Current;
                    if (x > value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        /// <summary>Returns the maximum value in a sequence of nullable <see cref="long" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="long" /> values to determine the maximum value of.</param>
        /// <returns>A value of type <c>Nullable&lt;Int64&gt;</c> in C# or <c>Nullable(Of Int64)</c> in Visual Basic that corresponds to the maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="Max(IEnumerable{System.Nullable{long}})" /> method uses the <see cref="long" /> implementation of <see cref="System.IComparable{T}" /> to compare values.</para>
        /// <para>If the source sequence is empty or contains only values that are <see langword="null" />, this function returns <see langword="null" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <see cref="O:Enumerable.Max" />.</para>
        /// </remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static long? Max(this IEnumerable<long?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            long? value = null;
            using (IEnumerator<long?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = e.Current;
                }
                while (!value.HasValue);

                long valueVal = value.GetValueOrDefault();
                if (valueVal >= 0)
                {
                    while (e.MoveNext())
                    {
                        long? cur = e.Current;
                        long x = cur.GetValueOrDefault();
                        if (x > valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                    }
                }
                else
                {
                    while (e.MoveNext())
                    {
                        long? cur = e.Current;
                        long x = cur.GetValueOrDefault();

                        // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                        // unless nulls either never happen or always happen.
                        if (cur.HasValue & x > valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                    }
                }
            }

            return value;
        }

        /// <summary>Returns the maximum value in a sequence of <see cref="double" /> values.</summary>
        /// <param name="source">A sequence of <see cref="double" /> values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>
        /// <para>The <see cref="Max(IEnumerable{double})" /> method uses the <see cref="double" /> implementation of <see cref="System.IComparable{T}" /> to compare values.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <see cref="O:Enumerable.Max" />.</para>
        /// </remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double Max(this IEnumerable<double> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            double value;
            using (IEnumerator<double> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = e.Current;

                // As described in a comment on Min(this IEnumerable<double>) NaN is ordered
                // less than all other values. We need to do explicit checks to ensure this, but
                // once we've found a value that is not NaN we need no longer worry about it,
                // so first loop until such a value is found (or not, as the case may be).
                while (double.IsNaN(value))
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = e.Current;
                }

                while (e.MoveNext())
                {
                    double x = e.Current;
                    if (x > value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        /// <summary>Returns the maximum value in a sequence of nullable <see cref="double" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="double" /> values to determine the maximum value of.</param>
        /// <returns>A value of type <c>Nullable&lt;Double&gt;</c> in C# or <c>Nullable(Of Double)</c> in Visual Basic that corresponds to the maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="Max(IEnumerable{System.Nullable{double}})" /> method uses the <see cref="double" /> implementation of <see cref="System.IComparable{T}" /> to compare values.</para>
        /// <para>If the source sequence is empty or contains only values that are <see langword="null" />, this function returns <see langword="null" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <see cref="O:Enumerable.Max" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="Max(IEnumerable{System.Nullable{double}})" /> to determine the maximum value in a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet54":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet54":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double? Max(this IEnumerable<double?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            double? value = null;
            using (IEnumerator<double?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = e.Current;
                }
                while (!value.HasValue);

                double valueVal = value.GetValueOrDefault();
                while (double.IsNaN(valueVal))
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    double? cur = e.Current;
                    if (cur.HasValue)
                    {
                        valueVal = (value = cur).GetValueOrDefault();
                    }
                }

                while (e.MoveNext())
                {
                    double? cur = e.Current;
                    double x = cur.GetValueOrDefault();

                    // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                    // unless nulls either never happen or always happen.
                    if (cur.HasValue & x > valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        /// <summary>Returns the maximum value in a sequence of <see cref="float" /> values.</summary>
        /// <param name="source">A sequence of <see cref="float" /> values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>
        /// <para>The <see cref="Max(IEnumerable{float})" /> method uses the <see cref="float" /> implementation of <see cref="System.IComparable{T}" /> to compare values.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <see cref="O:Enumerable.Max" />.</para>
        /// </remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static float Max(this IEnumerable<float> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            float value;
            using (IEnumerator<float> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = e.Current;
                while (float.IsNaN(value))
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = e.Current;
                }

                while (e.MoveNext())
                {
                    float x = e.Current;
                    if (x > value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        /// <summary>Returns the maximum value in a sequence of nullable <see cref="float" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="float" /> values to determine the maximum value of.</param>
        /// <returns>A value of type <c>Nullable&lt;Single&gt;</c> in C# or <c>Nullable(Of Single)</c> in Visual Basic that corresponds to the maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="Max(IEnumerable{System.Nullable{float}})" /> method uses the <see cref="float" /> implementation of <see cref="System.IComparable{T}" /> to compare values.</para>
        /// <para>If the source sequence is empty or contains only values that are <see langword="null" />, this function returns <see langword="null" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <see cref="O:Enumerable.Max" />.</para>
        /// </remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static float? Max(this IEnumerable<float?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            float? value = null;
            using (IEnumerator<float?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = e.Current;
                }
                while (!value.HasValue);

                float valueVal = value.GetValueOrDefault();
                while (float.IsNaN(valueVal))
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    float? cur = e.Current;
                    if (cur.HasValue)
                    {
                        valueVal = (value = cur).GetValueOrDefault();
                    }
                }

                while (e.MoveNext())
                {
                    float? cur = e.Current;
                    float x = cur.GetValueOrDefault();

                    // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                    // unless nulls either never happen or always happen.
                    if (cur.HasValue & x > valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        /// <summary>Returns the maximum value in a sequence of <see cref="decimal" /> values.</summary>
        /// <param name="source">A sequence of <see cref="decimal" /> values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>
        /// <para>The <see cref="Max(IEnumerable{decimal})" /> method uses the <see cref="decimal" /> implementation of <see cref="System.IComparable{T}" /> to compare values.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <see cref="O:Enumerable.Max" />.</para>
        /// </remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static decimal Max(this IEnumerable<decimal> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            decimal value;
            using (IEnumerator<decimal> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = e.Current;
                while (e.MoveNext())
                {
                    decimal x = e.Current;
                    if (x > value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        /// <summary>Returns the maximum value in a sequence of nullable <see cref="decimal" /> values.</summary>
        /// <param name="source">A sequence of nullable <see cref="decimal" /> values to determine the maximum value of.</param>
        /// <returns>A value of type <c>Nullable&lt;Decimal&gt;</c> in C# or <c>Nullable(Of Decimal)</c> in Visual Basic that corresponds to the maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>The <see cref="Max(IEnumerable{System.Nullable{decimal}})" /> method uses the <see cref="decimal" /> implementation of <see cref="System.IComparable{T}" /> to compare values.</para>
        /// <para>If the source sequence is empty or contains only values that are <see langword="null" />, this function returns <see langword="null" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <see cref="O:Enumerable.Max" />.</para>
        /// </remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static decimal? Max(this IEnumerable<decimal?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            decimal? value = null;
            using (IEnumerator<decimal?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = e.Current;
                }
                while (!value.HasValue);

                decimal valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    decimal? cur = e.Current;
                    decimal x = cur.GetValueOrDefault();
                    if (cur.HasValue && x > valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        /// <summary>Returns the maximum value in a generic sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">No object in <paramref name="source" /> implements the <see cref="System.IComparable" /> or <see cref="System.IComparable{T}" /> interface.</exception>
        /// <remarks>
        /// <para>If type <typeparamref name="TSource" /> implements <see cref="System.IComparable{T}" />, the <see cref="Max{T}(IEnumerable{T})" /> method uses that implementation to compare values. Otherwise, if type <typeparamref name="TSource" /> implements <see cref="System.IComparable" />, that implementation is used to compare values.</para>
        /// <para>If <typeparamref name="TSource" /> is a reference type and the source sequence is empty or contains only values that are <see langword="null" />, this method returns <see langword="null" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <see cref="O:Enumerable.Max" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="Max{T}(IEnumerable{T})" /> to determine the maximum value in a sequence of <see cref="System.IComparable{T}" /> objects.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet57":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet57":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static TSource? Max<TSource>(this IEnumerable<TSource> source) => Max(source, comparer: null);
        public static TSource? Max<TSource>(this IEnumerable<TSource> source, IComparer<TSource>? comparer)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            comparer ??= Comparer<TSource>.Default;

            TSource? value = default;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (value == null)
                {
                    do
                    {
                        if (!e.MoveNext())
                        {
                            return value;
                        }

                        value = e.Current;
                    }
                    while (value == null);

                    while (e.MoveNext())
                    {
                        TSource next = e.Current;
                        if (next != null && comparer.Compare(next, value) > 0)
                        {
                            value = next;
                        }
                    }
                }
                else
                {
                    if (!e.MoveNext())
                    {
                        ThrowHelper.ThrowNoElementsException();
                    }

                    value = e.Current;
                    if (comparer == Comparer<TSource>.Default)
                    {
                        while (e.MoveNext())
                        {
                            TSource next = e.Current;
                            if (Comparer<TSource>.Default.Compare(next, value) > 0)
                            {
                                value = next;
                            }
                        }
                    }
                    else
                    {
                        while (e.MoveNext())
                        {
                            TSource next = e.Current;
                            if (comparer.Compare(next, value) > 0)
                            {
                                value = next;
                            }
                        }
                    }
                }
            }

            return value;
        }

        public static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) => MaxBy(source, keySelector, null);
        public static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (keySelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            comparer ??= Comparer<TKey>.Default;

            TKey? key = default;
            TSource? value = default;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (key == null)
                {
                    do
                    {
                        if (!e.MoveNext())
                        {
                            return value;
                        }

                        value = e.Current;
                        key = keySelector(value);
                    }
                    while (key == null);

                    while (e.MoveNext())
                    {
                        TSource nextValue = e.Current;
                        TKey nextKey = keySelector(nextValue);
                        if (nextKey != null && comparer.Compare(nextKey, key) > 0)
                        {
                            key = nextKey;
                            value = nextValue;
                        }
                    }
                }
                else
                {
                    if (!e.MoveNext())
                    {
                        ThrowHelper.ThrowNoElementsException();
                    }

                    value = e.Current;
                    key = keySelector(value);
                    if (comparer == Comparer<TSource>.Default)
                    {
                        while (e.MoveNext())
                        {
                            TSource nextValue = e.Current;
                            TKey nextKey = keySelector(nextValue);
                            if (Comparer<TKey>.Default.Compare(nextKey, key) > 0)
                            {
                                key = nextKey;
                                value = nextValue;
                            }
                        }
                    }
                    else
                    {
                        while (e.MoveNext())
                        {
                            TSource nextValue = e.Current;
                            TKey nextKey = keySelector(nextValue);
                            if (comparer.Compare(nextKey, key) > 0)
                            {
                                key = nextKey;
                                value = nextValue;
                            }
                        }
                    }
                }
            }

            return value;
        }

        /// <summary>Invokes a transform function on each element of a sequence and returns the maximum <see cref="int" /> value.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks>
        /// <para>The <see cref="Max{T}(IEnumerable{T},Func{T,int})" /> method uses the <see cref="int" /> implementation of <see cref="System.IComparable{T}" /> to compare values.</para>
        /// <para>You can apply this method to a sequence of arbitrary values if you provide a function, <paramref name="selector" />, that projects the members of <paramref name="source" /> into a numeric type, specifically <see cref="int" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <see cref="O:Enumerable.Max" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="Max{T}(IEnumerable{T},Func{T,int})" /> to determine the maximum value in a sequence of projected values.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet58":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet58":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static int Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            int value;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = selector(e.Current);
                while (e.MoveNext())
                {
                    int x = selector(e.Current);
                    if (x > value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        /// <summary>Invokes a transform function on each element of a sequence and returns the maximum nullable <see cref="int" /> value.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The value of type <c>Nullable&lt;Int32&gt;</c> in C# or <c>Nullable(Of Int32)</c> in Visual Basic that corresponds to the maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Int32%7D%7D%29> method uses the <xref:System.Int32> implementation of <xref:System.IComparable%601> to compare values.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically `Nullable<Int32>` in C# or `Nullable(Of Int32)` in Visual Basic.
        /// In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <xref:System.Linq.Enumerable.Max%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int32%7D%29> to determine the maximum value in a sequence of projected values.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet58":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet58":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static int? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            int? value = null;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = selector(e.Current);
                }
                while (!value.HasValue);

                int valueVal = value.GetValueOrDefault();
                if (valueVal >= 0)
                {
                    // We can fast-path this case where we know HasValue will
                    // never affect the outcome, without constantly checking
                    // if we're in such a state. Similar fast-paths could
                    // be done for other cases, but as all-positive
                    // or mostly-positive integer values are quite common in real-world
                    // uses, it's only been done in this direction for int? and long?.
                    while (e.MoveNext())
                    {
                        int? cur = selector(e.Current);
                        int x = cur.GetValueOrDefault();
                        if (x > valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                    }
                }
                else
                {
                    while (e.MoveNext())
                    {
                        int? cur = selector(e.Current);
                        int x = cur.GetValueOrDefault();

                        // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                        // unless nulls either never happen or always happen.
                        if (cur.HasValue & x > valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                    }
                }
            }

            return value;
        }

        /// <summary>Invokes a transform function on each element of a sequence and returns the maximum <see cref="long" /> value.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int64%7D%29> method uses the <xref:System.Int64> implementation of <xref:System.IComparable%601> to compare values.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically <xref:System.Int64>.
        /// In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <xref:System.Linq.Enumerable.Max%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int32%7D%29> to determine the maximum value in a sequence of projected values.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet58":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet58":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static long Max<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            long value;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = selector(e.Current);
                while (e.MoveNext())
                {
                    long x = selector(e.Current);
                    if (x > value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        /// <summary>Invokes a transform function on each element of a sequence and returns the maximum nullable <see cref="long" /> value.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The value of type <c>Nullable&lt;Int64&gt;</c> in C# or <c>Nullable(Of Int64)</c> in Visual Basic that corresponds to the maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Int64%7D%7D%29> method uses the <xref:System.Int64> implementation of <xref:System.IComparable%601> to compare values.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically `Nullable<Int64>` in C# or `Nullable(Of Int64)` in Visual Basic.
        /// In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <xref:System.Linq.Enumerable.Max%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int32%7D%29> to determine the maximum value in a sequence of projected values.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet58":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet58":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static long? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            long? value = null;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = selector(e.Current);
                }
                while (!value.HasValue);

                long valueVal = value.GetValueOrDefault();
                if (valueVal >= 0)
                {
                    while (e.MoveNext())
                    {
                        long? cur = selector(e.Current);
                        long x = cur.GetValueOrDefault();
                        if (x > valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                    }
                }
                else
                {
                    while (e.MoveNext())
                    {
                        long? cur = selector(e.Current);
                        long x = cur.GetValueOrDefault();

                        // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                        // unless nulls either never happen or always happen.
                        if (cur.HasValue & x > valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                    }
                }
            }

            return value;
        }

        /// <summary>Invokes a transform function on each element of a sequence and returns the maximum <see cref="float" /> value.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Single%7D%29> method uses the <xref:System.Single> implementation of <xref:System.IComparable%601> to compare values.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically <xref:System.Single>.
        /// In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <xref:System.Linq.Enumerable.Max%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int32%7D%29> to determine the maximum value in a sequence of projected values.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet58":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet58":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static float Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            float value;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = selector(e.Current);
                while (float.IsNaN(value))
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = selector(e.Current);
                }

                while (e.MoveNext())
                {
                    float x = selector(e.Current);
                    if (x > value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        /// <summary>Invokes a transform function on each element of a sequence and returns the maximum nullable <see cref="float" /> value.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The value of type <c>Nullable&lt;Single&gt;</c> in C# or <c>Nullable(Of Single)</c> in Visual Basic that corresponds to the maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Single%7D%7D%29> method uses the <xref:System.Single> implementation of <xref:System.IComparable%601> to compare values.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically `Nullable<Single>` in C# or `Nullable(Of Single)` in Visual Basic.
        /// In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <xref:System.Linq.Enumerable.Max%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int32%7D%29> to determine the maximum value in a sequence of projected values.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet58":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet58":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static float? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            float? value = null;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = selector(e.Current);
                }
                while (!value.HasValue);

                float valueVal = value.GetValueOrDefault();
                while (float.IsNaN(valueVal))
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    float? cur = selector(e.Current);
                    if (cur.HasValue)
                    {
                        valueVal = (value = cur).GetValueOrDefault();
                    }
                }

                while (e.MoveNext())
                {
                    float? cur = selector(e.Current);
                    float x = cur.GetValueOrDefault();

                    // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                    // unless nulls either never happen or always happen.
                    if (cur.HasValue & x > valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        /// <summary>Invokes a transform function on each element of a sequence and returns the maximum <see cref="double" /> value.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Double%7D%29> method uses the <xref:System.Double> implementation of <xref:System.IComparable%601> to compare values.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically <xref:System.Double>.
        /// In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <xref:System.Linq.Enumerable.Max%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int32%7D%29> to determine the maximum value in a sequence of projected values.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet58":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet58":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            double value;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = selector(e.Current);

                // As described in a comment on Min(this IEnumerable<double>) NaN is ordered
                // less than all other values. We need to do explicit checks to ensure this, but
                // once we've found a value that is not NaN we need no longer worry about it,
                // so first loop until such a value is found (or not, as the case may be).
                while (double.IsNaN(value))
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = selector(e.Current);
                }

                while (e.MoveNext())
                {
                    double x = selector(e.Current);
                    if (x > value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        /// <summary>Invokes a transform function on each element of a sequence and returns the maximum nullable <see cref="double" /> value.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The value of type <c>Nullable&lt;Double&gt;</c> in C# or <c>Nullable(Of Double)</c> in Visual Basic that corresponds to the maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Double%7D%7D%29> method uses the <xref:System.Double> implementation of <xref:System.IComparable%601> to compare values.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically `Nullable<Double>` in C# or `Nullable(Of Double)` in Visual Basic.
        /// In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <xref:System.Linq.Enumerable.Max%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int32%7D%29> to determine the maximum value in a sequence of projected values.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet58":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet58":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static double? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            double? value = null;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = selector(e.Current);
                }
                while (!value.HasValue);

                double valueVal = value.GetValueOrDefault();
                while (double.IsNaN(valueVal))
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    double? cur = selector(e.Current);
                    if (cur.HasValue)
                    {
                        valueVal = (value = cur).GetValueOrDefault();
                    }
                }

                while (e.MoveNext())
                {
                    double? cur = selector(e.Current);
                    double x = cur.GetValueOrDefault();

                    // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                    // unless nulls either never happen or always happen.
                    if (cur.HasValue & x > valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        /// <summary>Invokes a transform function on each element of a sequence and returns the maximum <see cref="decimal" /> value.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Decimal%7D%29> method uses the <xref:System.Decimal> implementation of <xref:System.IComparable%601> to compare values.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically <xref:System.Decimal>.
        /// In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <xref:System.Linq.Enumerable.Max%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int32%7D%29> to determine the maximum value in a sequence of projected values.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet58":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet58":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static decimal Max<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            decimal value;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = selector(e.Current);
                while (e.MoveNext())
                {
                    decimal x = selector(e.Current);
                    if (x > value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        /// <summary>Invokes a transform function on each element of a sequence and returns the maximum nullable <see cref="decimal" /> value.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The value of type <c>Nullable&lt;Decimal&gt;</c> in C# or <c>Nullable(Of Decimal)</c> in Visual Basic that corresponds to the maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Nullable%7BSystem.Decimal%7D%7D%29> method uses the <xref:System.Decimal> implementation of <xref:System.IComparable%601> to compare values.
        /// You can apply this method to a sequence of arbitrary values if you provide a function, `selector`, that projects the members of `source` into a numeric type, specifically `Nullable<Decimal>` in C# or `Nullable(Of Decimal)` in Visual Basic.
        /// In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <xref:System.Linq.Enumerable.Max%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int32%7D%29> to determine the maximum value in a sequence of projected values.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet58":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet58":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static decimal? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            decimal? value = null;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = selector(e.Current);
                }
                while (!value.HasValue);

                decimal valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    decimal? cur = selector(e.Current);
                    decimal x = cur.GetValueOrDefault();
                    if (cur.HasValue && x > valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        /// <summary>Invokes a transform function on each element of a generic sequence and returns the maximum resulting value.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// If type `TResult` implements <xref:System.IComparable%601>, this method uses that implementation to compare values. Otherwise, if type `TResult` implements <xref:System.IComparable>, that implementation is used to compare values.
        /// In Visual Basic query expression syntax, an `Aggregate Into Max()` clause translates to an invocation of <xref:System.Linq.Enumerable.Max%2A>.
        /// ## Examples
        /// The following code example demonstrates how to use <xref:System.Linq.Enumerable.Max%60%601%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2CSystem.Int32%7D%29> to determine the maximum value in a sequence of projected values.
        /// [!INCLUDE[sqo_diff_overload_example_func](~/includes/sqo-diff-overload-example-func-md.md)]
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet58":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet58":::
        /// ]]></format></remarks>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static TResult? Max<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            TResult? value = default;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (value == null)
                {
                    do
                    {
                        if (!e.MoveNext())
                        {
                            return value;
                        }

                        value = selector(e.Current);
                    }
                    while (value == null);

                    Comparer<TResult> comparer = Comparer<TResult>.Default;
                    while (e.MoveNext())
                    {
                        TResult x = selector(e.Current);
                        if (x != null && comparer.Compare(x, value) > 0)
                        {
                            value = x;
                        }
                    }
                }
                else
                {
                    if (!e.MoveNext())
                    {
                        ThrowHelper.ThrowNoElementsException();
                    }

                    value = selector(e.Current);
                    while (e.MoveNext())
                    {
                        TResult x = selector(e.Current);
                        if (Comparer<TResult>.Default.Compare(x, value) > 0)
                        {
                            value = x;
                        }
                    }
                }
            }

            return value;
        }
    }
}
